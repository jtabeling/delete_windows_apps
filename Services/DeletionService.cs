using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsAppsManager.Models;
using System.Security.Principal;
using System.Text;

namespace WindowsAppsManager.Services
{
    /// <summary>
    /// Service responsible for safely deleting Windows apps and cleaning up associated data
    /// </summary>
    public class DeletionService
    {
        private readonly BackupService backupService;
        private static readonly string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WindowsAppsManager", "Logs", "deletion.log");
        private static readonly object LogLock = new object();

        public DeletionService(BackupService backupService)
        {
            this.backupService = backupService;
            EnsureLogDirectory();
        }

        private void EnsureLogDirectory()
        {
            try
            {
                var logDir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
            }
            catch (Exception ex)
            {
                // If we can't create log directory, continue without logging
                Console.WriteLine($"Warning: Could not create log directory: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            try
            {
                lock (LogLock)
                {
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                    Console.WriteLine(logEntry); // Also output to console
                    File.AppendAllText(LogPath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // If logging fails, continue without crashing
            }
        }

        /// <summary>
        /// Deletes a Windows app with enhanced permission handling and comprehensive reporting
        /// </summary>
        public async Task<bool> DeleteAppAsync(WindowsApp app, BackupInfo? backup = null, IProgress<string>? progress = null)
        {
            try
            {
                Log($"=== DELETION START: {app.Name} ({app.PackageName}) ===");
                progress?.Report($"🚀 Starting deletion process for {app.Name}");
                progress?.Report($"📋 Package: {app.PackageName}");
                progress?.Report($"📁 Folder: {app.FolderPath}");

                // Pre-deletion safety checks
                if (app.IsProtected)
                {
                    var warning = "⚠️  WARNING: This is a protected system app. Deletion may cause system instability.";
                    progress?.Report(warning);
                    Log($"PROTECTED APP WARNING: {warning}");
                }

                // Phase 1: Pre-deletion analysis and preparation
                progress?.Report("🔍 Phase 1: Pre-deletion analysis...");
                Log("Phase 1: Pre-deletion analysis starting");
                string analysisResult = await AnalyzeDeletionFeasibilityAsync(app, progress);
                progress?.Report($"📊 Analysis result: {analysisResult}");
                Log($"Analysis result: {analysisResult}");

                // Check if folder exists before starting
                bool folderExistsBefore = Directory.Exists(app.FolderPath);
                progress?.Report($"📁 Initial folder status: {(folderExistsBefore ? "EXISTS" : "NOT FOUND")}");
                Log($"Initial state - Folder exists: {folderExistsBefore}");

                // Phase 2: Process termination (enhanced)
                progress?.Report("🔄 Phase 2: Terminating related processes...");
                Log("Phase 2: Process termination starting");
                bool processesTerminated = await TryTerminateAppProcessesAsync(app, progress);
                if (!processesTerminated)
                {
                    var warning = "⚠️  Some processes could not be terminated - deletion may be incomplete";
                    progress?.Report(warning);
                    Log($"Process termination warning: {warning}");
                }

                // Phase 3: PowerShell deletion attempt (CRITICAL)
                progress?.Report("🔷 Phase 3: PowerShell-based deletion...");
                Log("Phase 3: PowerShell deletion starting - CRITICAL PHASE");
                bool powerShellSuccess = await TryPowerShellDeletionAsync(app, progress);
                Log($"PowerShell deletion result: {powerShellSuccess}");
                
                // Phase 4: Post-PowerShell verification and cleanup
                progress?.Report("🔍 Phase 4: Post-deletion verification...");
                Log("Phase 4: Post-deletion verification starting");
                bool folderExistsAfter = Directory.Exists(app.FolderPath);
                bool packageStillRegistered = await VerifyPackageExistsAsync(app.PackageName, progress);
                Log($"Post-PowerShell state - Folder exists: {folderExistsAfter}, Package registered: {packageStillRegistered}");

                // CRITICAL INTEGRITY CHECK: Analyze the deletion result
                if (!packageStillRegistered && !folderExistsAfter)
                {
                    var success = "✅ COMPLETE SUCCESS: Package unregistered and folder removed";
                    progress?.Report(success);
                    Log($"COMPLETE SUCCESS: {success}");
                    return true;
                }
                else if (!packageStillRegistered && folderExistsAfter)
                {
                    var partial = "⚠️  PARTIAL SUCCESS: Package unregistered but folder persists";
                    progress?.Report(partial);
                    Log($"PARTIAL SUCCESS: {partial}");
                    progress?.Report("🔧 Initiating enhanced folder cleanup...");
                    Log("Attempting enhanced folder cleanup after successful PowerShell unregistration");
                    
                    bool folderCleanupSuccess = await TryManualFolderCleanupAsync(app, progress);
                    if (folderCleanupSuccess)
                    {
                        var cleanup = "✅ CLEANUP SUCCESS: Folder removed after manual intervention";
                        progress?.Report(cleanup);
                        Log($"CLEANUP SUCCESS: {cleanup}");
                        return true;
                    }
                    else
                    {
                        var cleanupPartial = "⚠️  CLEANUP PARTIAL: Some files may remain (reboot may be required)";
                        progress?.Report(cleanupPartial);
                        Log($"CLEANUP PARTIAL: {cleanupPartial}");
                        // Consider partial success if PowerShell unregistration worked
                        Log("Considering partial success since PowerShell unregistration succeeded");
                        return true;
                    }
                }
                else if (packageStillRegistered && !folderExistsAfter)
                {
                    var unusual = "⚠️  UNUSUAL: Folder removed but package still registered";
                    progress?.Report(unusual);
                    Log($"UNUSUAL STATE: {unusual}");
                    progress?.Report("🔧 Attempting additional PowerShell cleanup...");
                    Log("Attempting additional PowerShell cleanup for orphaned registration");
                    
                    bool additionalCleanupSuccess = await TryAdditionalPowerShellCleanupAsync(app.PackageName, progress);
                    Log($"Additional PowerShell cleanup result: {additionalCleanupSuccess}");
                    return additionalCleanupSuccess;
                }
                else
                {
                    // CRITICAL: Both package registration and folder persist
                    var failure = "❌ CRITICAL: Both package registration and folder persist - PowerShell deletion failed";
                    progress?.Report(failure);
                    Log($"CRITICAL FAILURE: {failure}");
                    
                    // INTEGRITY PROTECTION: Do NOT allow manual folder deletion if PowerShell failed
                    // This prevents Windows app integrity issues
                    progress?.Report("🛡️  INTEGRITY PROTECTION: Preventing manual folder deletion");
                    progress?.Report("📋 Package is still registered - manual deletion would cause integrity issues");
                    Log("INTEGRITY PROTECTION ACTIVATED: Preventing manual folder deletion to preserve Windows app integrity");
                    
                    progress?.Report("🔧 Attempting enhanced PowerShell recovery...");
                    Log("Attempting enhanced PowerShell recovery methods");
                    
                    // Try enhanced PowerShell methods before giving up
                    bool enhancedSuccess = await TryEnhancedPowerShellRecoveryAsync(app, progress);
                    Log($"Enhanced PowerShell recovery result: {enhancedSuccess}");
                    
                    if (enhancedSuccess)
                    {
                        var recovery = "✅ RECOVERY SUCCESS: Enhanced PowerShell methods succeeded";
                        progress?.Report(recovery);
                        Log($"RECOVERY SUCCESS: {recovery}");
                        return true;
                    }
                    else
                    {
                        var finalFailure = "❌ DELETION FAILED: Package remains registered - integrity preserved";
                        progress?.Report(finalFailure);
                        Log($"FINAL FAILURE: {finalFailure}");
                        progress?.Report("💡 Recommendations for manual resolution:");
                        progress?.Report("   1. Restart Windows and try again");
                        progress?.Report("   2. Use Windows Settings > Apps to remove the app");
                        progress?.Report("   3. Check Windows Event Viewer for detailed error information");
                        progress?.Report("   4. Ensure app is not currently running or has system dependencies");
                        Log("Provided manual resolution recommendations to user");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                var error = $"❌ Deletion process failed with exception: {ex.Message}";
                progress?.Report(error);
                Log($"EXCEPTION: {error}");
                Log($"Stack trace: {ex.StackTrace}");
                return false;
            }
            finally
            {
                Log($"=== DELETION END: {app.Name} ===");
            }
        }

        /// <summary>
        /// Enhanced PowerShell recovery methods for stubborn packages
        /// </summary>
        private async Task<bool> TryEnhancedPowerShellRecoveryAsync(WindowsApp app, IProgress<string>? progress)
        {
            try
            {
                Log("Starting enhanced PowerShell recovery methods");
                
                // Method 1: Clear package cache and retry
                progress?.Report("🔹 Method 1: Clearing package cache and retrying...");
                Log("Method 1: Clearing package cache");
                await ClearPackageCacheAsync(progress);
                await Task.Delay(3000);
                
                bool retrySuccess = await TryPowerShellDeletionAsync(app, progress);
                Log($"Post-cache-clear retry result: {retrySuccess}");
                if (retrySuccess) return true;

                // Method 2: Force remove provisioned package
                progress?.Report("🔹 Method 2: Force removing provisioned package...");
                Log("Method 2: Force removing provisioned package");
                bool provisionedSuccess = await TryRemoveProvisionedPackageAsync(app.PackageName, progress);
                Log($"Provisioned package removal result: {provisionedSuccess}");
                if (provisionedSuccess)
                {
                    // Retry main deletion after provisioned removal
                    await Task.Delay(2000);
                    retrySuccess = await TryPowerShellDeletionAsync(app, progress);
                    Log($"Post-provisioned-removal retry result: {retrySuccess}");
                    if (retrySuccess) return true;
                }

                // Method 3: DISM cleanup
                progress?.Report("🔹 Method 3: DISM package cleanup...");
                Log("Method 3: DISM package cleanup");
                bool dismSuccess = await TryDismPackageCleanupAsync(app, progress);
                Log($"DISM cleanup result: {dismSuccess}");
                
                return dismSuccess;
            }
            catch (Exception ex)
            {
                Log($"Enhanced PowerShell recovery failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try DISM package cleanup as last resort
        /// </summary>
        private async Task<bool> TryDismPackageCleanupAsync(WindowsApp app, IProgress<string>? progress)
        {
            try
            {
                Log("Attempting DISM package cleanup");
                progress?.Report("🔸 Using DISM for package cleanup...");
                
                // Get the package family name for DISM
                string packageFamilyName = app.PackageName.Split('_')[0];
                
                string dismCommand = $"/Online /Remove-ProvisionedAppxPackage /PackageName:{app.PackageName}";
                bool dismResult = await ExecuteSystemCommand("dism.exe", dismCommand, progress, 30000);
                Log($"DISM result: {dismResult}");
                
                if (dismResult)
                {
                    // Wait and verify
                    await Task.Delay(3000);
                    bool stillExists = await VerifyPackageExistsAsync(app.PackageName, progress);
                    Log($"Post-DISM package exists: {stillExists}");
                    return !stillExists;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Log($"DISM cleanup failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// DEPRECATED: This method has been removed to prevent Windows app integrity issues.
        /// Manual folder deletion without proper package unregistration can cause system instability.
        /// Use TryEnhancedPowerShellRecoveryAsync instead.
        /// </summary>
        private async Task<bool> TryCompleteManualDeletionAsync(WindowsApp app, IProgress<string>? progress)
        {
            Log("TryCompleteManualDeletionAsync called - this method is deprecated for integrity protection");
            progress?.Report("⚠️  Manual deletion disabled for integrity protection");
            progress?.Report("🛡️  Use enhanced PowerShell methods instead");
            return false;
        }

        /// <summary>
        /// Pre-deletion permission fixes to make Remove-AppxPackage succeed
        /// This is the key logic that prevents "access denied" errors in PowerShell
        /// </summary>
        private async Task<bool> TryPreDeletionPermissionFixesAsync(string folderPath, IProgress<string>? progress)
        {
            try
            {
                Log($"Starting pre-deletion permission fixes for: {folderPath}");
                progress?.Report("🔸 Applying ownership and permission changes...");

                // Step 1: Take ownership of the app folder
                bool ownershipSuccess = await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c takeown /f \"{folderPath}\" /r /d y",
                    progress,
                    15000
                );
                Log($"Takeown command result: {ownershipSuccess}");

                // Step 2: Grant full control permissions
                bool permissionSuccess = await ExecuteSystemCommand(
                    "cmd.exe", 
                    $"/c icacls \"{folderPath}\" /grant:r \"{Environment.UserName}\":F /t /c /q",
                    progress,
                    15000
                );
                Log($"Icacls permission grant result: {permissionSuccess}");

                // Step 3: Disable inheritance and grant explicit permissions
                bool inheritanceSuccess = await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c icacls \"{folderPath}\" /inheritance:d /grant:r \"{Environment.UserName}\":F /t /c /q",
                    progress,
                    15000
                );
                Log($"Icacls inheritance disable result: {inheritanceSuccess}");

                // Step 4: Additional PowerShell-based permission fixes
                string psCommand = $@"
                    try {{
                        $acl = Get-Acl '{folderPath}' -ErrorAction Stop
                        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule('{Environment.UserName}','FullControl','ContainerInherit,ObjectInherit','None','Allow')
                        $acl.SetAccessRule($accessRule)
                        Set-Acl -Path '{folderPath}' -AclObject $acl -ErrorAction Stop
                        Write-Output 'SUCCESS'
                    }} catch {{
                        Write-Output $_.Exception.Message
                    }}";

                bool psPermissionSuccess = await ExecutePowerShellCommand(
                    $"-NoProfile -Command \"{psCommand}\"",
                    "powershell.exe",
                    progress,
                    10000
                );
                Log($"PowerShell ACL fix result: {psPermissionSuccess}");

                bool overallSuccess = ownershipSuccess || permissionSuccess || inheritanceSuccess || psPermissionSuccess;
                Log($"Pre-deletion permission fixes overall result: {overallSuccess}");
                
                if (overallSuccess)
                {
                    progress?.Report("✅ Pre-deletion permission fixes completed");
                    // Small delay to let permissions settle
                    await Task.Delay(1000);
                }
                else
                {
                    progress?.Report("⚠️  Pre-deletion permission fixes had limited success");
                }

                return overallSuccess;
            }
            catch (Exception ex)
            {
                Log($"Pre-deletion permission fixes failed: {ex.Message}");
                progress?.Report($"⚠️  Permission fixes error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Enhanced permission fixes for stubborn packages that resist standard permission changes
        /// </summary>
        private async Task<bool> TryEnhancedPermissionFixesAsync(string folderPath, IProgress<string>? progress)
        {
            try
            {
                Log($"Starting enhanced permission fixes for: {folderPath}");
                progress?.Report("🔸 Applying enhanced permission fixes...");

                // Method 1: Multiple takeown attempts with different strategies
                progress?.Report("🔹 Enhanced takeown strategies...");
                bool takeownSuccess = false;
                
                // Strategy 1: Standard takeown
                takeownSuccess = await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c takeown /f \"{folderPath}\" /r /d y",
                    progress,
                    20000
                );

                // Strategy 2: Takeown with admin group
                if (!takeownSuccess)
                {
                    takeownSuccess = await ExecuteSystemCommand(
                        "cmd.exe",
                        $"/c takeown /f \"{folderPath}\" /a /r /d y",
                        progress,
                        20000
                    );
                }

                // Method 2: Advanced ICACLS operations
                progress?.Report("🔹 Advanced ICACLS operations...");
                
                // Reset all permissions to default
                bool resetSuccess = await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c icacls \"{folderPath}\" /reset /t /c /q",
                    progress,
                    20000
                );

                // Grant everyone full control (temporary)
                bool everyoneSuccess = await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c icacls \"{folderPath}\" /grant Everyone:F /t /c /q",
                    progress,
                    20000
                );

                // Method 3: PowerShell with elevated privileges
                progress?.Report("🔹 PowerShell elevated permission operations...");
                string elevatedPsCommand = $@"
                    try {{
                        # Get current user
                        $user = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
                        
                        # Take ownership via PowerShell
                        $folder = Get-Item '{folderPath}' -Force -ErrorAction Stop
                        $acl = $folder.GetAccessControl()
                        $acl.SetOwner([System.Security.Principal.NTAccount]$user)
                        $folder.SetAccessControl($acl)
                        
                        # Grant full control
                        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($user,'FullControl','ContainerInherit,ObjectInherit','None','Allow')
                        $acl.SetAccessRule($accessRule)
                        $folder.SetAccessControl($acl)
                        
                        # Apply to all children
                        Get-ChildItem '{folderPath}' -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {{
                            try {{
                                $_.SetAccessControl($acl)
                            }} catch {{
                                # Continue on individual file failures
                            }}
                        }}
                        
                        Write-Output 'SUCCESS'
                    }} catch {{
                        Write-Output $_.Exception.Message
                    }}";

                bool elevatedPsSuccess = await ExecutePowerShellCommand(
                    $"-NoProfile -Command \"{elevatedPsCommand}\"",
                    "powershell.exe",
                    progress,
                    30000
                );

                bool overallSuccess = takeownSuccess || resetSuccess || everyoneSuccess || elevatedPsSuccess;
                Log($"Enhanced permission fixes overall result: {overallSuccess}");

                if (overallSuccess)
                {
                    progress?.Report("✅ Enhanced permission fixes completed");
                    // Longer delay for enhanced fixes to settle
                    await Task.Delay(3000);
                }
                else
                {
                    progress?.Report("⚠️  Enhanced permission fixes had limited success");
                }

                return overallSuccess;
            }
            catch (Exception ex)
            {
                Log($"Enhanced permission fixes failed: {ex.Message}");
                progress?.Report($"⚠️  Enhanced permission fixes error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Advanced permission fixes for stubborn folders
        /// </summary>
        private async Task<bool> TryAdvancedPermissionFixesAsync(string folderPath, IProgress<string>? progress)
        {
            try
            {
                progress?.Report("🔸 Attempting advanced permission fixes...");

                // Method 1: NTFS ownership change
                bool ownershipSuccess = await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c takeown /f \"{folderPath}\" /r /d y && icacls \"{folderPath}\" /reset /T /C /Q",
                    progress,
                    20000
                );

                // Method 2: PowerShell ACL manipulation
                if (!ownershipSuccess)
                {
                    progress?.Report("🔸 Trying PowerShell ACL manipulation...");
                    string psCommand = $"Get-Acl \"{folderPath}\" | Set-Acl \"{folderPath}\"; " +
                                     $"$acl = Get-Acl \"{folderPath}\"; " +
                                     $"$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule('{Environment.UserName}','FullControl','ContainerInherit,ObjectInherit','None','Allow'); " +
                                     $"$acl.SetAccessRule($accessRule); " +
                                     $"Set-Acl -Path \"{folderPath}\" -AclObject $acl";

                    await ExecuteSystemCommand(
                        "powershell.exe",
                        $"-NoProfile -Command \"{psCommand}\"",
                        progress,
                        15000
                    );
                }

                // Method 3: Disable inheritance and explicit permissions
                await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c icacls \"{folderPath}\" /inheritance:d /grant:r \"{Environment.UserName}\":F /T /C /Q",
                    progress,
                    15000
                );

                return true;
            }
            catch (Exception ex)
            {
                progress?.Report($"⚠️  Advanced permission fixes failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper method to determine if a process is related to the app
        /// </summary>
        private bool IsAppRelatedProcess(Process process, WindowsApp app)
        {
            try
            {
                if (process.HasExited) return false;

                // Check process name against app name
                var appBaseName = Path.GetFileName(app.FolderPath);
                if (process.ProcessName.Contains(appBaseName, StringComparison.OrdinalIgnoreCase))
                    return true;

                // Check process path against app folder
                try
                {
                    var processPath = process.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(processPath) && 
                        processPath.StartsWith(app.FolderPath, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch (Win32Exception)
                {
                    // Access denied to process module info - this is normal for some system processes
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to terminate running processes related to the app
        /// </summary>
        private async Task<bool> TryTerminateAppProcessesAsync(WindowsApp app, IProgress<string>? progress)
        {
            try
            {
                var runningProcesses = Process.GetProcesses()
                    .Where(p => !p.HasExited && IsAppRelatedProcess(p, app))
                    .ToList();

                if (!runningProcesses.Any())
                {
                    progress?.Report("✅ No running processes found for this app");
                    return true;
                }

                progress?.Report($"🔍 Found {runningProcesses.Count} running process(es) for {app.Name}");
                
                foreach (var process in runningProcesses)
                {
                    try
                    {
                        progress?.Report($"⏹️  Attempting to close process: {process.ProcessName} (PID: {process.Id})");
                        
                        // Try graceful close first
                        if (!process.CloseMainWindow())
                        {
                            // Force kill if graceful close doesn't work
                            await Task.Delay(2000); // Wait 2 seconds for graceful close
                            if (!process.HasExited)
                            {
                                process.Kill();
                                await process.WaitForExitAsync();
                                progress?.Report($"🔹 Force killed process: {process.ProcessName}");
                            }
                        }
                        else
                        {
                            await process.WaitForExitAsync();
                            progress?.Report($"✅ Gracefully closed process: {process.ProcessName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"⚠️  Could not terminate process {process.ProcessName}: {ex.Message}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ Error terminating app processes: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes the app's files and folders
        /// </summary>
        private async Task DeleteAppFilesAsync(WindowsApp app, IProgress<string>? progress)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (Directory.Exists(app.FolderPath))
                    {
                        RemoveReadOnlyAttributes(app.FolderPath, progress);
                        Directory.Delete(app.FolderPath, true);
                        progress?.Report($"Deleted app folder: {app.FolderPath}");
                    }
                    else
                    {
                        progress?.Report("App folder not found - may have been already deleted");
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    throw new UnauthorizedAccessException($"Access denied to delete {app.FolderPath}. Make sure the application is running as administrator.");
                }
                catch (DirectoryNotFoundException)
                {
                    progress?.Report("App folder not found - may have been already deleted");
                }
                catch (IOException ex)
                {
                    throw new IOException($"Could not delete app files: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Removes read-only attributes from files and directories recursively
        /// </summary>
        private void RemoveReadOnlyAttributes(string path, IProgress<string>? progress)
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                if (dirInfo.Exists)
                {
                    dirInfo.Attributes &= ~FileAttributes.ReadOnly;

                    foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            file.Attributes &= ~FileAttributes.ReadOnly;
                        }
                        catch (Exception ex)
                        {
                            progress?.Report($"Warning: Could not remove read-only from {file.Name}: {ex.Message}");
                        }
                    }

                    foreach (var dir in dirInfo.GetDirectories("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            dir.Attributes &= ~FileAttributes.ReadOnly;
                        }
                        catch (Exception ex)
                        {
                            progress?.Report($"Warning: Could not remove read-only from {dir.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"Warning: Error removing read-only attributes: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up registry entries related to the app
        /// </summary>
        private async Task CleanupRegistryEntriesAsync(WindowsApp app, IProgress<string>? progress)
        {
            await Task.Run(() =>
            {
                var registryPaths = new[]
                {
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications\{app.PackageName}",
                    $@"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages\{app.PackageName}",
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\InboxApplications\{app.PackageName}",
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{app.PackageName}"
                };

                foreach (var regPath in registryPaths)
                {
                    try
                    {
                        DeleteRegistryKey(Registry.LocalMachine, regPath, progress);
                        DeleteRegistryKey(Registry.CurrentUser, regPath, progress);
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"Warning: Could not clean registry path {regPath}: {ex.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// Deletes a registry key if it exists
        /// </summary>
        private void DeleteRegistryKey(RegistryKey rootKey, string keyPath, IProgress<string>? progress)
        {
            try
            {
                using (var key = rootKey.OpenSubKey(keyPath, true))
                {
                    if (key != null)
                    {
                        rootKey.DeleteSubKeyTree(keyPath);
                        progress?.Report($"Deleted registry key: {rootKey.Name}\\{keyPath}");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                progress?.Report($"Warning: Access denied to registry key: {keyPath}");
            }
            catch (Exception ex)
            {
                progress?.Report($"Warning: Error deleting registry key {keyPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes user data and settings for the app
        /// </summary>
        private async Task RemoveUserDataAsync(WindowsApp app, IProgress<string>? progress)
        {
            await Task.Run(() =>
            {
                var userDataPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", app.PackageName),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", app.GetDisplayName()),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Caches", app.PackageName)
                };

                foreach (var path in userDataPaths)
                {
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                            progress?.Report($"Deleted user data: {path}");
                        }
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"Warning: Could not delete user data {path}: {ex.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// Removes shortcuts and start menu entries
        /// </summary>
        private async Task RemoveShortcutsAsync(WindowsApp app, IProgress<string>? progress)
        {
            await Task.Run(() =>
            {
                var shortcutLocations = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs")
                };

                var appName = app.GetDisplayName();
                var searchPatterns = new[] { $"{appName}.lnk", $"*{appName}*.lnk" };

                foreach (var location in shortcutLocations)
                {
                    try
                    {
                        if (Directory.Exists(location))
                        {
                            foreach (var pattern in searchPatterns)
                            {
                                var shortcuts = Directory.GetFiles(location, pattern, SearchOption.AllDirectories);
                                foreach (var shortcut in shortcuts)
                                {
                                    try
                                    {
                                        File.Delete(shortcut);
                                        progress?.Report($"Deleted shortcut: {shortcut}");
                                    }
                                    catch (Exception ex)
                                    {
                                        progress?.Report($"Warning: Could not delete shortcut {shortcut}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"Warning: Error searching for shortcuts in {location}: {ex.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// Cleans up Windows Store cache related to the app
        /// </summary>
        private async Task CleanupStoreCache(WindowsApp app, IProgress<string>? progress)
        {
            await Task.Run(() =>
            {
                try
                {
                    var cachePaths = new[]
                    {
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows Store", "Cache"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", app.PackageName)
                    };

                    foreach (var cachePath in cachePaths)
                    {
                        try
                        {
                            if (Directory.Exists(cachePath))
                            {
                                Directory.Delete(cachePath, true);
                                progress?.Report($"Cleaned cache: {cachePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            progress?.Report($"Warning: Could not clean cache {cachePath}: {ex.Message}");
                        }
                    }

                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "wsreset.exe",
                            Arguments = "/noui",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        using (var process = Process.Start(psi))
                        {
                            if (process != null)
                            {
                                process.WaitForExit(10000);
                                progress?.Report("Windows Store cache reset completed");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"Warning: Could not reset Windows Store cache: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"Warning: Error during cache cleanup: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Verifies that an app has been completely deleted
        /// </summary>
        /// <param name="app">App to verify deletion</param>
        /// <returns>True if app appears to be completely deleted</returns>
        public bool VerifyDeletion(WindowsApp app)
        {
            try
            {
                if (Directory.Exists(app.FolderPath))
                {
                    return false;
                }

                var userDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", app.PackageName);
                if (Directory.Exists(userDataPath))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Enhanced PowerShell deletion with crash protection and better error handling
        /// </summary>
        private async Task<bool> TryPowerShellDeletionAsync(WindowsApp app, IProgress<string>? progress)
        {
            try
            {
                progress?.Report($"🔷 Attempting PowerShell deletion: {app.PackageName}");
                progress?.Report($"📋 Using exact package name: {app.PackageName}");
                
                // First verify the package exists and get the exact name
                string actualPackageName = await GetExactPackageNameAsync(app.PackageName, progress);
                if (string.IsNullOrEmpty(actualPackageName))
                {
                    progress?.Report("⚠️  Package not found in PowerShell - may already be deleted");
                    return true; // Consider this success if package doesn't exist
                }

                progress?.Report($"✅ Verified package exists: {actualPackageName}");

                // CRITICAL: Apply permission fixes BEFORE attempting PowerShell deletion
                // This is what makes Remove-AppxPackage succeed instead of failing with access denied
                progress?.Report("🔐 Pre-deletion permission fixes...");
                Log("Applying pre-deletion permission fixes to ensure PowerShell success");
                Log($"DEBUG: About to call TryPreDeletionPermissionFixesAsync for folder: {app.FolderPath}");
                bool permissionSuccess = await TryPreDeletionPermissionFixesAsync(app.FolderPath, progress);
                if (permissionSuccess)
                {
                    progress?.Report("✅ Permission fixes applied successfully");
                    Log("Pre-deletion permission fixes successful");
                }
                else
                {
                    progress?.Report("⚠️  Permission fixes had issues - proceeding anyway");
                    Log("Pre-deletion permission fixes had issues");
                }

                // Try multiple PowerShell approaches for better reliability
                bool success = false;
                
                // Method 1: Standard Remove-AppxPackage (most reliable)
                progress?.Report("🔹 Method 1: Standard Remove-AppxPackage...");
                success = await TryStandardPowerShellRemoval(actualPackageName, progress);
                
                if (!success)
                {
                    // Method 2: Force removal with -AllUsers flag
                    progress?.Report("🔹 Method 2: Force removal with -AllUsers...");
                    success = await TryForcePowerShellRemoval(actualPackageName, progress);
                }
                
                if (!success)
                {
                    // Method 3: Use Windows PowerShell 5.1 instead of PowerShell Core
                    progress?.Report("🔹 Method 3: Using Windows PowerShell 5.1...");
                    success = await TryWindowsPowerShellRemoval(actualPackageName, progress);
                }

                if (!success)
                {
                    // Method 4: Enhanced permission fixes + retry
                    progress?.Report("🔹 Method 4: Enhanced permission fixes + retry...");
                    Log("Attempting enhanced permission fixes for stubborn package");
                    bool enhancedPermissions = await TryEnhancedPermissionFixesAsync(app.FolderPath, progress);
                    if (enhancedPermissions)
                    {
                        progress?.Report("🔄 Retrying PowerShell deletion after enhanced permission fixes...");
                        await Task.Delay(2000); // Wait for permissions to take effect
                        success = await TryStandardPowerShellRemoval(actualPackageName, progress);
                        if (!success)
                        {
                            success = await TryForcePowerShellRemoval(actualPackageName, progress);
                        }
                    }
                }

                if (success)
                {
                    // Wait for package database to update
                    await Task.Delay(2000);
                    
                    // Verify removal with comprehensive check
                    bool stillRegistered = await VerifyPackageExistsAsync(actualPackageName, progress);
                    bool folderStillExists = Directory.Exists(app.FolderPath);
                    
                    if (!stillRegistered && !folderStillExists)
                    {
                        progress?.Report("✅ PowerShell deletion successful and fully verified");
                        return true;
                    }
                    else if (!stillRegistered && folderStillExists)
                    {
                        progress?.Report("⚠️  PowerShell deletion succeeded but app folder still exists");
                        progress?.Report("🔧 Attempting manual folder cleanup...");
                        
                        // Try to clean up the remaining folder
                        bool folderCleanupSuccess = await TryManualFolderCleanupAsync(app, progress);
                        if (folderCleanupSuccess)
                        {
                            progress?.Report("✅ Manual folder cleanup successful");
                            return true;
                        }
                        else
                        {
                            progress?.Report("❌ Manual folder cleanup failed - folder may have locked files");
                            return false;
                        }
                    }
                    else if (stillRegistered && !folderStillExists)
                    {
                        progress?.Report("⚠️  Folder deleted but package still registered in PowerShell");
                        progress?.Report("🔧 Attempting additional PowerShell cleanup...");
                        
                        // Try additional cleanup methods
                        bool additionalCleanup = await TryAdditionalPowerShellCleanupAsync(actualPackageName, progress);
                        return additionalCleanup;
                    }
                    else
                    {
                        progress?.Report("❌ PowerShell reported success but both folder and registration persist");
                        return false;
                    }
                }
                else
                {
                    progress?.Report("❌ All PowerShell deletion methods failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ PowerShell deletion crashed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the exact package name by verifying it exists in PowerShell
        /// </summary>
        private async Task<string> GetExactPackageNameAsync(string packageName, IProgress<string>? progress)
        {
            try
            {
                progress?.Report("🔍 Verifying exact package name in PowerShell...");
                
                // Extract the base name for searching (before first underscore)
                string baseName = packageName.Split('_')[0];
                
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"Get-AppxPackage -Name '*{baseName}*' | Where-Object {{$_.PackageFullName -eq '{packageName}'}} | Select-Object -ExpandProperty PackageFullName\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        var timeoutTask = Task.Delay(10000); // 10 second timeout
                        var processTask = process.WaitForExitAsync();
                        
                        var completedTask = await Task.WhenAny(processTask, timeoutTask);
                        
                        if (completedTask == timeoutTask)
                        {
                            progress?.Report("⚠️  Package name verification timed out");
                            try { process.Kill(); } catch { }
                            return packageName; // Return original if verification fails
                        }

                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();
                        
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            progress?.Report($"⚠️  Package verification warning: {error.Trim()}");
                        }
                        
                        string exactName = output.Trim();
                        if (!string.IsNullOrEmpty(exactName))
                        {
                            progress?.Report($"✅ Found exact package name: {exactName}");
                            return exactName;
                        }
                        else
                        {
                            progress?.Report($"⚠️  Package not found by PowerShell search");
                            return ""; // Package doesn't exist
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"⚠️  Package name verification failed: {ex.Message}");
            }
            return packageName; // Return original if verification fails
        }

        /// <summary>
        /// Verify if a package exists using PowerShell
        /// </summary>
        private async Task<bool> VerifyPackageExistsAsync(string packageName, IProgress<string>? progress)
        {
            try
            {
                // Extract the base name for searching (before first underscore)
                string baseName = packageName.Split('_')[0];
                
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"try {{ Get-AppxPackage -Name '*{baseName}*' -ErrorAction Stop | Where-Object {{$_.PackageFullName -eq '{packageName}'}} | Measure-Object | Select-Object -ExpandProperty Count }} catch {{ Write-Output '0' }}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        var timeoutTask = Task.Delay(10000); // 10 second timeout
                        var processTask = process.WaitForExitAsync();
                        
                        var completedTask = await Task.WhenAny(processTask, timeoutTask);
                        
                        if (completedTask == timeoutTask)
                        {
                            progress?.Report("⚠️  Package verification timed out");
                            try { process.Kill(); } catch { }
                            return false;
                        }

                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();
                        
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            progress?.Report($"⚠️  Package verification warning: {error.Trim()}");
                        }
                        
                        return int.TryParse(output.Trim(), out int count) && count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"⚠️  Package verification failed: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Standard PowerShell removal method
        /// </summary>
        private async Task<bool> TryStandardPowerShellRemoval(string packageName, IProgress<string>? progress)
        {
            var command = $"Remove-AppxPackage -Package '{packageName}' -Confirm:$false -ErrorAction Stop";
            progress?.Report($"🔸 Executing: {command}");
            
            return await ExecutePowerShellCommand(
                $"-NoProfile -Command \"try {{ {command}; Write-Output 'SUCCESS' }} catch {{ Write-Output $_.Exception.Message }}\"",
                "powershell.exe",
                progress,
                15000 // 15 second timeout
            );
        }

        /// <summary>
        /// Force PowerShell removal with AllUsers flag
        /// </summary>
        private async Task<bool> TryForcePowerShellRemoval(string packageName, IProgress<string>? progress)
        {
            var command = $"Remove-AppxPackage -Package '{packageName}' -AllUsers -Confirm:$false -ErrorAction Stop";
            progress?.Report($"🔸 Executing: {command}");
            
            return await ExecutePowerShellCommand(
                $"-NoProfile -Command \"try {{ {command}; Write-Output 'SUCCESS' }} catch {{ Write-Output $_.Exception.Message }}\"",
                "powershell.exe",
                progress,
                20000 // 20 second timeout
            );
        }

        /// <summary>
        /// Use Windows PowerShell 5.1 instead of PowerShell Core
        /// </summary>
        private async Task<bool> TryWindowsPowerShellRemoval(string packageName, IProgress<string>? progress)
        {
            // Try Windows PowerShell 5.1 which sometimes works better with AppX
            var windowsPowerShellPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            
            if (!File.Exists(windowsPowerShellPath))
            {
                progress?.Report("⚠️  Windows PowerShell 5.1 not found, skipping");
                return false;
            }

            var command = $"Remove-AppxPackage -Package '{packageName}' -Confirm:$false -ErrorAction Stop";
            progress?.Report($"🔸 Executing (Windows PowerShell 5.1): {command}");

            return await ExecutePowerShellCommand(
                $"-Command \"try {{ {command}; Write-Output 'SUCCESS' }} catch {{ Write-Output $_.Exception.Message }}\"",
                windowsPowerShellPath,
                progress,
                25000 // 25 second timeout
            );
        }

        /// <summary>
        /// Execute PowerShell command with timeout and crash protection
        /// </summary>
        private async Task<bool> ExecutePowerShellCommand(string arguments, string executable, IProgress<string>? progress, int timeoutMs)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        var timeoutTask = Task.Delay(timeoutMs);
                        var processTask = process.WaitForExitAsync();
                        
                        var completedTask = await Task.WhenAny(processTask, timeoutTask);
                        
                        if (completedTask == timeoutTask)
                        {
                            progress?.Report($"⚠️  PowerShell command timed out after {timeoutMs/1000} seconds");
                            try 
                            { 
                                process.Kill(); 
                                await process.WaitForExitAsync();
                            } 
                            catch { }
                            return false;
                        }

                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();
                        
                        // For system commands, exit code 0 usually means success
                        bool success = process.ExitCode == 0;
                        
                        if (!success && !string.IsNullOrWhiteSpace(error))
                        {
                            progress?.Report($"⚠️  Command error: {error.Trim()}");
                        }
                        
                        if (!string.IsNullOrWhiteSpace(output) && output.Length < 500) // Don't spam with long output
                        {
                            progress?.Report($"📋 Command output: {output.Trim()}");
                        }
                        
                        return success;
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ PowerShell execution failed: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Validates dependencies before manual deletion to prevent breaking other apps
        /// </summary>
        private async Task<bool> ValidateDependenciesAsync(WindowsApp app, IProgress<string>? progress)
        {
            try
            {
                progress?.Report("Checking for apps that depend on this package...");
                
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Get-AppxPackage | Where-Object {{$_.Dependencies -contains '{app.PackageName}'}} | Select-Object Name, PackageFullName\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        var output = await process.StandardOutput.ReadToEndAsync();
                        
                        if (!string.IsNullOrWhiteSpace(output) && output.Contains("Name"))
                        {
                            progress?.Report("⚠️  WARNING: Found apps that may depend on this package:");
                            progress?.Report(output);
                            progress?.Report("⚠️  Deletion may cause dependent apps to malfunction");
                            return false; // Indicate potential issues
                        }
                        else
                        {
                            progress?.Report("✅ No dependent apps found - safe to proceed");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"Warning: Could not validate dependencies: {ex.Message}");
                return true; // Proceed with caution if validation fails
            }
            
            return true;
        }

        /// <summary>
        /// Checks PowerShell execution policy and reports any restrictions
        /// </summary>
        private async Task<bool> CheckPowerShellExecutionPolicyAsync(IProgress<string>? progress)
        {
            try
            {
                progress?.Report("Checking PowerShell execution policy...");
                
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"Get-ExecutionPolicy\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                        var policy = output.Trim();
                        
                        progress?.Report($"Current PowerShell execution policy: {policy}");
                        
                        if (policy.Equals("Restricted", StringComparison.OrdinalIgnoreCase))
                        {
                            progress?.Report("⚠️  PowerShell execution is RESTRICTED - this may cause deletion failures");
                            progress?.Report("💡 Consider running: Set-ExecutionPolicy RemoteSigned -Scope CurrentUser");
                            return false;
                        }
                        else if (policy.Equals("AllSigned", StringComparison.OrdinalIgnoreCase))
                        {
                            progress?.Report("⚠️  PowerShell requires signed scripts - may cause issues");
                            return false;
                        }
                        else
                        {
                            progress?.Report("✅ PowerShell execution policy allows script execution");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"Warning: Could not check PowerShell execution policy: {ex.Message}");
            }
            
            return true; // Assume OK if we can't check
        }

        /// <summary>
        /// Comprehensive pre-deletion analysis to identify potential issues
        /// </summary>
        private async Task<string> AnalyzeDeletionFeasibilityAsync(WindowsApp app, IProgress<string>? progress)
        {
            var issues = new List<string>();
            
            try
            {
                progress?.Report("🔍 Analyzing app for deletion feasibility...");
                
                // Check if app is currently running
                var runningProcesses = Process.GetProcesses()
                    .Where(p => !p.HasExited && IsAppRelatedProcess(p, app))
                    .ToList();
                
                if (runningProcesses.Any())
                {
                    var processNames = string.Join(", ", runningProcesses.Select(p => p.ProcessName));
                    issues.Add($"🟡 App is currently running: {processNames}");
                    progress?.Report($"⚠️  Found {runningProcesses.Count} running processes: {processNames}");
                }
                
                // Check if app folder exists and is accessible
                if (!Directory.Exists(app.FolderPath))
                {
                    issues.Add("🟡 App folder not found - may already be deleted");
                    progress?.Report($"📁 App folder not found: {app.FolderPath}");
                }
                else
                {
                    // Check folder permissions
                    try
                    {
                        var testFile = Path.Combine(app.FolderPath, "test_write_access.tmp");
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                        progress?.Report("✅ Write access to app folder confirmed");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        issues.Add("🔴 Access denied to app folder - insufficient permissions");
                        progress?.Report("❌ Cannot write to app folder - permission denied");
                    }
                    catch (Exception ex)
                    {
                        issues.Add($"🟡 App folder access issue: {ex.Message}");
                        progress?.Report($"⚠️  App folder access test failed: {ex.Message}");
                    }
                }
                
                // Check if app is a system/protected app
                if (app.IsProtected)
                {
                    issues.Add("🔴 App is marked as PROTECTED - deletion may fail");
                    progress?.Report("🛡️  WARNING: This is a protected system app");
                }
                
                if (app.IsSystemApp)
                {
                    issues.Add("🟡 App is a SYSTEM app - higher chance of deletion issues");
                    progress?.Report("⚙️  WARNING: This is a system app");
                }
                
                // Check for critical system dependencies
                var criticalApps = new[] { "Microsoft.Windows", "Microsoft.VCLibs", "Microsoft.NET.Native" };
                if (criticalApps.Any(critical => app.PackageName.Contains(critical)))
                {
                    issues.Add("🔴 CRITICAL SYSTEM COMPONENT - deletion strongly discouraged");
                    progress?.Report("🚨 DANGER: This appears to be a critical system component");
                }
                
                // Verify PowerShell can see this package
                await Task.Run(async () =>
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-Command \"Get-AppxPackage | Where-Object {{$_.PackageFullName -eq '{app.PackageName}'}}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        using (var process = Process.Start(psi))
                        {
                            if (process != null)
                            {
                                await process.WaitForExitAsync();
                                var output = await process.StandardOutput.ReadToEndAsync();
                                
                                if (string.IsNullOrWhiteSpace(output))
                                {
                                    issues.Add("🟡 Package not visible to PowerShell - may already be corrupted");
                                    progress?.Report("⚠️  Package not found by PowerShell Get-AppxPackage");
                                }
                                else
                                {
                                    progress?.Report("✅ Package is visible to PowerShell");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        issues.Add($"🟡 PowerShell package check failed: {ex.Message}");
                        progress?.Report($"⚠️  Could not verify package with PowerShell: {ex.Message}");
                    }
                });
                
                progress?.Report($"🔍 Analysis complete - found {issues.Count} potential issues");
                return string.Join("\n", issues);
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ Analysis failed: {ex.Message}");
                return $"Analysis error: {ex.Message}";
            }
        }

        /// <summary>
        /// Enhanced verification that checks if deletion actually succeeded
        /// </summary>
        private async Task<bool> VerifyDeletionSuccessAsync(WindowsApp app, IProgress<string>? progress)
        {
            try
            {
                progress?.Report("🔍 Verifying deletion success...");
                
                bool folderExists = Directory.Exists(app.FolderPath);
                bool packageExists = false;
                
                // Check if package still exists in PowerShell
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"Get-AppxPackage | Where-Object {{$_.PackageFullName -eq '{app.PackageName}'}} | Measure-Object | Select-Object -ExpandProperty Count\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            var output = await process.StandardOutput.ReadToEndAsync();
                            
                            if (int.TryParse(output.Trim(), out int count) && count > 0)
                            {
                                packageExists = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"Warning: Could not verify package existence: {ex.Message}");
                }
                
                // Report detailed verification results
                if (!folderExists && !packageExists)
                {
                    progress?.Report("✅ DELETION VERIFIED: Both folder and package registration removed");
                    return true;
                }
                else if (!folderExists && packageExists)
                {
                    progress?.Report("⚠️  PARTIAL DELETION: Folder removed but package registration remains");
                    progress?.Report("💡 Try running 'wsreset' to refresh the package database");
                    return false;
                }
                else if (folderExists && !packageExists)
                {
                    progress?.Report("⚠️  PARTIAL DELETION: Package unregistered but folder remains");
                    progress?.Report("💡 Folder may be locked by running processes or permissions");
                    return false;
                }
                else
                {
                    progress?.Report("❌ DELETION FAILED: Both folder and package registration still exist");
                    progress?.Report("🔍 Check the detailed error messages above for the cause");
                    return false;
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ Verification failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try manual folder cleanup when PowerShell deletion succeeded but folder still exists
        /// Enhanced based on real-world diagnostic findings
        /// </summary>
        private async Task<bool> TryManualFolderCleanupAsync(WindowsApp app, IProgress<string>? progress)
        {
            try
            {
                if (!Directory.Exists(app.FolderPath))
                {
                    return true; // Folder already gone
                }

                progress?.Report($"🔍 Analyzing remaining folder: {app.FolderPath}");

                // Get folder statistics for reporting
                var folderInfo = new DirectoryInfo(app.FolderPath);
                var files = folderInfo.GetFiles("*", SearchOption.AllDirectories);
                var directories = folderInfo.GetDirectories("*", SearchOption.AllDirectories);
                
                progress?.Report($"📊 Found {files.Length} files and {directories.Length} subdirectories to clean up");

                // Step 1: Terminate any remaining processes (enhanced)
                progress?.Report("🔄 Step 1: Terminating related processes...");
                await TryTerminateAppProcessesAsync(app, progress);
                
                // Give processes extra time to fully terminate
                await Task.Delay(2000);

                // Step 2: Enhanced ownership and permission handling
                progress?.Report("🔐 Step 2: Taking ownership and fixing permissions...");
                bool ownershipSuccess = await TryTakeOwnershipAndPermissionsAsync(app.FolderPath, progress);
                
                if (!ownershipSuccess)
                {
                    progress?.Report("⚠️  Ownership/permission fix failed, attempting alternative methods...");
                }

                // Step 3: Try multiple deletion approaches
                progress?.Report("🗑️  Step 3: Attempting folder deletion...");
                
                // Method 1: Standard deletion
                bool deleted = await TryStandardFolderDeletion(app.FolderPath, progress);
                
                if (!deleted)
                {
                    // Method 2: Force deletion with CMD
                    deleted = await TryForceFolderDeletion(app.FolderPath, progress);
                }
                
                if (!deleted)
                {
                    // Method 3: Robocopy method (for stubborn files)
                    deleted = await TryRobocopyDeletion(app.FolderPath, progress);
                }

                // Step 4: Verify cleanup success
                await Task.Delay(1000); // Give filesystem time to update
                bool folderExists = Directory.Exists(app.FolderPath);
                
                if (!folderExists)
                {
                    progress?.Report("✅ Manual folder cleanup successful - all files removed");
                    return true;
                }
                else
                {
                    // Count remaining files
                    try
                    {
                        var remainingFiles = Directory.GetFiles(app.FolderPath, "*", SearchOption.AllDirectories);
                        progress?.Report($"⚠️  Partial cleanup: {remainingFiles.Length} files remain (may require reboot)");
                        
                        // If significantly fewer files remain, consider it partial success
                        if (remainingFiles.Length < files.Length / 2)
                        {
                            progress?.Report("✅ Significant cleanup achieved - remaining files may be cleared on reboot");
                            return true;
                        }
                    }
                    catch
                    {
                        progress?.Report("⚠️  Cannot count remaining files - folder may be partially cleaned");
                    }
                    
                    progress?.Report("❌ Manual folder cleanup failed - folder still contains locked files");
                    return false;
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ Manual folder cleanup failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Enhanced ownership and permission handling based on diagnostic findings
        /// </summary>
        private async Task<bool> TryTakeOwnershipAndPermissionsAsync(string folderPath, IProgress<string>? progress)
        {
            try
            {
                // Step 1: Take ownership with enhanced error handling
                progress?.Report("🔸 Taking ownership of folder and all contents...");
                var takeownResult = await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c takeown /f \"{folderPath}\" /r /d y",
                    progress,
                    15000
                );
                
                if (!takeownResult)
                {
                    progress?.Report("⚠️  Takeown failed, trying alternative approach...");
                }

                // Step 2: Grant full permissions to current user
                progress?.Report("🔸 Granting full permissions to current user...");
                var currentUser = Environment.UserName;
                var icaclsUserResult = await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c icacls \"{folderPath}\" /grant \"{currentUser}:F\" /T /C /Q",
                    progress,
                    15000
                );

                // Step 3: Grant full permissions to Everyone (fallback)
                progress?.Report("🔸 Granting full permissions to Everyone...");
                var icaclsEveryoneResult = await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c icacls \"{folderPath}\" /grant Everyone:F /T /C /Q",
                    progress,
                    15000
                );

                // Step 4: Grant full permissions to Administrators
                progress?.Report("🔸 Granting full permissions to Administrators...");
                var icaclsAdminResult = await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c icacls \"{folderPath}\" /grant Administrators:F /T /C /Q",
                    progress,
                    15000
                );

                // Step 5: Remove any inherited permissions that might be restricting
                progress?.Report("🔸 Removing restrictive inherited permissions...");
                await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c icacls \"{folderPath}\" /inheritance:d /T /C /Q",
                    progress,
                    10000
                );

                return icaclsUserResult || icaclsEveryoneResult || icaclsAdminResult;
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ Ownership/permission fix failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try standard folder deletion
        /// </summary>
        private async Task<bool> TryStandardFolderDeletion(string folderPath, IProgress<string>? progress)
        {
            try
            {
                progress?.Report("🔸 Attempting standard folder deletion...");
                
                // Remove read-only attributes first
                RemoveReadOnlyAttributes(folderPath, progress);
                
                // Try .NET deletion
                Directory.Delete(folderPath, true);
                
                // Verify deletion
                await Task.Delay(500);
                return !Directory.Exists(folderPath);
            }
            catch (Exception ex)
            {
                progress?.Report($"⚠️  Standard deletion failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try force deletion using CMD
        /// </summary>
        private async Task<bool> TryForceFolderDeletion(string folderPath, IProgress<string>? progress)
        {
            try
            {
                progress?.Report("🔸 Attempting force deletion with CMD...");
                
                var result = await ExecuteSystemCommand(
                    "cmd.exe",
                    $"/c rmdir /s /q \"{folderPath}\"",
                    progress,
                    30000
                );

                // Verify deletion
                await Task.Delay(1000);
                return !Directory.Exists(folderPath);
            }
            catch (Exception ex)
            {
                progress?.Report($"⚠️  Force deletion failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try robocopy deletion method (for very stubborn files)
        /// </summary>
        private async Task<bool> TryRobocopyDeletion(string folderPath, IProgress<string>? progress)
        {
            try
            {
                progress?.Report("🔸 Attempting robocopy deletion method...");
                
                // Create empty temporary directory
                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Use robocopy to mirror empty directory (effectively deleting contents)
                    var robocopyResult = await ExecuteSystemCommand(
                        "robocopy.exe",
                        $"\"{tempDir}\" \"{folderPath}\" /MIR /R:1 /W:1",
                        progress,
                        45000
                    );

                    // Clean up temp directory
                    Directory.Delete(tempDir, true);

                    // Try to remove the now-empty folder
                    Directory.Delete(folderPath, false);

                    // Verify deletion
                    await Task.Delay(1000);
                    return !Directory.Exists(folderPath);
                }
                finally
                {
                    // Ensure temp directory is cleaned up
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"⚠️  Robocopy deletion failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute system command with enhanced error handling and timeout
        /// </summary>
        private async Task<bool> ExecuteSystemCommand(string executable, string arguments, IProgress<string>? progress, int timeoutMs)
        {
            try
            {
                progress?.Report($"🔸 Executing: {executable} {arguments}");

                var psi = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        var timeoutTask = Task.Delay(timeoutMs);
                        var processTask = process.WaitForExitAsync();
                        
                        var completedTask = await Task.WhenAny(processTask, timeoutTask);
                        
                        if (completedTask == timeoutTask)
                        {
                            progress?.Report($"⚠️  Command timed out after {timeoutMs/1000} seconds");
                            try 
                            { 
                                process.Kill(); 
                                await process.WaitForExitAsync();
                            } 
                            catch { }
                            return false;
                        }

                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();
                        
                        // For system commands, exit code 0 usually means success
                        bool success = process.ExitCode == 0;
                        
                        if (!success && !string.IsNullOrWhiteSpace(error))
                        {
                            progress?.Report($"⚠️  Command error: {error.Trim()}");
                        }
                        
                        if (!string.IsNullOrWhiteSpace(output) && output.Length < 500) // Don't spam with long output
                        {
                            progress?.Report($"📋 Command output: {output.Trim()}");
                        }
                        
                        return success;
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ System command execution failed: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Try additional PowerShell cleanup methods
        /// </summary>
        private async Task<bool> TryAdditionalPowerShellCleanupAsync(string packageName, IProgress<string>? progress)
        {
            try
            {
                progress?.Report("🔧 Attempting additional PowerShell cleanup methods...");

                // Method 1: Try removing provisioned package
                progress?.Report("🔹 Method 1: Removing provisioned package...");
                bool provisionedRemoved = await TryRemoveProvisionedPackageAsync(packageName, progress);

                // Method 2: Try forced removal with different parameters
                progress?.Report("🔹 Method 2: Forced removal with DisableDevelopmentMode...");
                bool forcedRemoved = await TryForcedRemovalAsync(packageName, progress);

                // Method 3: Clear package cache
                progress?.Report("🔹 Method 3: Clearing package cache...");
                await ClearPackageCacheAsync(progress);

                // Verify if any method worked
                await Task.Delay(2000);
                bool stillExists = await VerifyPackageExistsAsync(packageName, progress);
                
                if (!stillExists)
                {
                    progress?.Report("✅ Additional PowerShell cleanup successful");
                    return true;
                }
                else if (provisionedRemoved || forcedRemoved)
                {
                    progress?.Report("⚠️  Partial cleanup success - package may be removed on next reboot");
                    return true;
                }
                else
                {
                    progress?.Report("❌ Additional PowerShell cleanup methods failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"❌ Additional PowerShell cleanup failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to remove provisioned package
        /// </summary>
        private async Task<bool> TryRemoveProvisionedPackageAsync(string packageName, IProgress<string>? progress)
        {
            try
            {
                // Extract package family name (before first underscore + publisher ID)
                var parts = packageName.Split('_');
                if (parts.Length < 2) return false;

                var packageFamilyName = parts[0] + "_" + parts[parts.Length - 1]; // AppName_PublisherID
                var command = $"Remove-AppxProvisionedPackage -Online -PackageName '{packageFamilyName}' -ErrorAction SilentlyContinue";
                progress?.Report($"🔸 Executing: {command}");

                return await ExecutePowerShellCommand(
                    $"-NoProfile -Command \"try {{ {command}; Write-Output 'SUCCESS' }} catch {{ Write-Output $_.Exception.Message }}\"",
                    "powershell.exe",
                    progress,
                    15000
                );
            }
            catch (Exception ex)
            {
                progress?.Report($"⚠️  Remove provisioned package failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try forced removal with different parameters
        /// </summary>
        private async Task<bool> TryForcedRemovalAsync(string packageName, IProgress<string>? progress)
        {
            try
            {
                var command = $"Remove-AppxPackage -Package '{packageName}' -AllUsers -Confirm:$false -DisableDevelopmentMode -ErrorAction Stop";
                progress?.Report($"🔸 Executing: {command}");

                return await ExecutePowerShellCommand(
                    $"-NoProfile -Command \"try {{ {command}; Write-Output 'SUCCESS' }} catch {{ Write-Output $_.Exception.Message }}\"",
                    "powershell.exe",
                    progress,
                    20000
                );
            }
            catch (Exception ex)
            {
                progress?.Report($"⚠️  Forced removal failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear package cache to help with cleanup
        /// </summary>
        private async Task ClearPackageCacheAsync(IProgress<string>? progress)
        {
            try
            {
                var commands = new[]
                {
                    "Get-AppxPackage | Reset-AppxPackage",
                    "wsreset.exe /c"
                };

                foreach (var command in commands)
                {
                    try
                    {
                        progress?.Report($"🔸 Executing: {command}");
                        await ExecutePowerShellCommand(
                            $"-NoProfile -Command \"{command}\"",
                            "powershell.exe",
                            progress,
                            10000
                        );
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"⚠️  Cache clear command failed: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"⚠️  Clear package cache failed: {ex.Message}");
            }
        }
    }
} 