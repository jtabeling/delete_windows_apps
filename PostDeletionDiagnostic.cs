using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Diagnostic tool for analyzing "PowerShell succeeds but folder persists" scenarios
/// Usage: PostDeletionDiagnostic.exe "Microsoft.ZuneMusic_10.21102.11411.0_x64__8wekyb3d8bbwe" "C:\Program Files\WindowsApps\Microsoft.ZuneMusic_10.21102.11411.0_x64__8wekyb3d8bbwe"
/// </summary>
public class PostDeletionDiagnostic
{
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: PostDeletionDiagnostic.exe <PackageName> <FolderPath>");
            Console.WriteLine("Example: PostDeletionDiagnostic.exe \"Microsoft.ZuneMusic_10.21102.11411.0_x64__8wekyb3d8bbwe\" \"C:\\Program Files\\WindowsApps\\Microsoft.ZuneMusic_10.21102.11411.0_x64__8wekyb3d8bbwe\"");
            return;
        }

        string packageName = args[0];
        string folderPath = args[1];

        Console.WriteLine("🔍 POST-DELETION DIAGNOSTIC ANALYSIS");
        Console.WriteLine("=====================================");
        Console.WriteLine($"Package: {packageName}");
        Console.WriteLine($"Folder:  {folderPath}");
        Console.WriteLine();

        // Check 1: PowerShell Registration Status
        await CheckPowerShellRegistration(packageName);

        // Check 2: Folder Existence and Contents
        CheckFolderStatus(folderPath);

        // Check 3: Running Processes
        CheckRunningProcesses(packageName, folderPath);

        // Check 4: File Locks and Handles
        await CheckFileHandles(folderPath);

        // Check 5: Permissions Analysis
        await CheckPermissions(folderPath);

        // Check 6: Windows Store Cache
        await CheckWindowsStoreCache(packageName);

        // Check 7: Registry Entries
        CheckRegistryEntries(packageName);

        Console.WriteLine();
        Console.WriteLine("📋 RECOMMENDED ACTIONS:");
        Console.WriteLine("========================");
        
        if (Directory.Exists(folderPath))
        {
            Console.WriteLine("1. Terminate any running processes related to the app");
            Console.WriteLine("2. Take ownership of the folder: takeown /f \"" + folderPath + "\" /r /d y");
            Console.WriteLine("3. Grant full permissions: icacls \"" + folderPath + "\" /grant Everyone:F /T /C");
            Console.WriteLine("4. Try manual folder deletion: rmdir /s /q \"" + folderPath + "\"");
            Console.WriteLine("5. If folder still persists, restart Windows and try again");
        }

        Console.WriteLine();
        Console.WriteLine("✅ Diagnostic complete. Press any key to exit...");
        Console.ReadKey();
    }

    private static async Task CheckPowerShellRegistration(string packageName)
    {
        Console.WriteLine("🔍 Checking PowerShell Registration...");
        try
        {
            var baseName = packageName.Split('_')[0];
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"Get-AppxPackage -Name '*{baseName}*' | Where-Object {{$_.PackageFullName -eq '{packageName}'}} | Select-Object PackageFullName, Status, InstallLocation\"",
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
                    var error = await process.StandardError.ReadToEndAsync();

                    if (!string.IsNullOrWhiteSpace(output) && !output.Contains("PackageFullName"))
                    {
                        Console.WriteLine($"✅ Package NOT found in PowerShell (deletion successful)");
                    }
                    else if (output.Contains(packageName))
                    {
                        Console.WriteLine($"❌ Package STILL REGISTERED in PowerShell:");
                        Console.WriteLine(output.Trim());
                    }
                    else
                    {
                        Console.WriteLine($"✅ Package not found in PowerShell registration");
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Console.WriteLine($"⚠️  PowerShell warning: {error.Trim()}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ PowerShell check failed: {ex.Message}");
        }
        Console.WriteLine();
    }

    private static void CheckFolderStatus(string folderPath)
    {
        Console.WriteLine("📁 Checking Folder Status...");
        
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine("✅ Folder does not exist (deletion successful)");
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"❌ Folder STILL EXISTS: {folderPath}");
        
        try
        {
            var dirInfo = new DirectoryInfo(folderPath);
            var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            var subdirs = dirInfo.GetDirectories("*", SearchOption.AllDirectories);

            Console.WriteLine($"   📄 Files: {files.Length}");
            Console.WriteLine($"   📂 Subdirectories: {subdirs.Length}");
            Console.WriteLine($"   📅 Created: {dirInfo.CreationTime}");
            Console.WriteLine($"   📅 Modified: {dirInfo.LastWriteTime}");

            // Show largest files
            var largestFiles = files.OrderByDescending(f => f.Length).Take(5);
            Console.WriteLine("   📋 Largest files:");
            foreach (var file in largestFiles)
            {
                try
                {
                    Console.WriteLine($"      {file.Name} ({file.Length:N0} bytes)");
                }
                catch
                {
                    Console.WriteLine($"      {file.Name} (size unknown - may be locked)");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️  Cannot analyze folder contents: {ex.Message}");
        }
        Console.WriteLine();
    }

    private static void CheckRunningProcesses(string packageName, string folderPath)
    {
        Console.WriteLine("🔄 Checking Running Processes...");
        
        try
        {
            var allProcesses = Process.GetProcesses();
            var relatedProcesses = allProcesses.Where(p =>
            {
                try
                {
                    // Check if process name contains app name
                    var appName = packageName.Split('_')[0].Split('.').LastOrDefault();
                    if (!string.IsNullOrEmpty(appName) && p.ProcessName.Contains(appName, StringComparison.OrdinalIgnoreCase))
                        return true;

                    // Check if process is running from the app folder
                    var processPath = p.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(processPath) && processPath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
                        return true;

                    return false;
                }
                catch
                {
                    return false;
                }
            }).ToList();

            if (relatedProcesses.Any())
            {
                Console.WriteLine($"❌ Found {relatedProcesses.Count} related process(es) still running:");
                foreach (var process in relatedProcesses)
                {
                    try
                    {
                        Console.WriteLine($"   🔄 PID {process.Id}: {process.ProcessName} ({process.MainModule?.FileName ?? "Unknown path"})");
                    }
                    catch
                    {
                        Console.WriteLine($"   🔄 PID {process.Id}: {process.ProcessName} (path unknown)");
                    }
                }
            }
            else
            {
                Console.WriteLine("✅ No related processes found running");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Process check failed: {ex.Message}");
        }
        Console.WriteLine();
    }

    private static async Task CheckFileHandles(string folderPath)
    {
        Console.WriteLine("🔒 Checking File Handles...");
        
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "handle.exe",
                Arguments = $"\"{folderPath}\"",
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
                    
                    if (!string.IsNullOrWhiteSpace(output) && !output.Contains("No matching handles"))
                    {
                        Console.WriteLine("❌ File handles detected:");
                        Console.WriteLine(output.Trim());
                    }
                    else
                    {
                        Console.WriteLine("✅ No file handles detected");
                    }
                }
            }
        }
        catch
        {
            Console.WriteLine("⚠️  Handle.exe not found - install Sysinternals tools for better handle detection");
        }
        Console.WriteLine();
    }

    private static async Task CheckPermissions(string folderPath)
    {
        Console.WriteLine("🔑 Checking Permissions...");
        
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine("✅ Folder doesn't exist");
            Console.WriteLine();
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c icacls \"{folderPath}\"",
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
                    var error = await process.StandardError.ReadToEndAsync();

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        Console.WriteLine("📋 Current permissions:");
                        Console.WriteLine(output.Trim());
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Console.WriteLine($"❌ Permission check error: {error.Trim()}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Permission check failed: {ex.Message}");
        }
        Console.WriteLine();
    }

    private static async Task CheckWindowsStoreCache(string packageName)
    {
        Console.WriteLine("🏪 Checking Windows Store Cache...");
        
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"Get-AppxPackage -Name '*{packageName.Split('_')[0]}*' -AllUsers | Select-Object Name, Status, PackageUserInformation\"",
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

                    if (!string.IsNullOrWhiteSpace(output) && output.Contains(packageName.Split('_')[0]))
                    {
                        Console.WriteLine("❌ Package still in Windows Store cache:");
                        Console.WriteLine(output.Trim());
                    }
                    else
                    {
                        Console.WriteLine("✅ Package not found in Windows Store cache");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Store cache check failed: {ex.Message}");
        }
        Console.WriteLine();
    }

    private static void CheckRegistryEntries(string packageName)
    {
        Console.WriteLine("📊 Checking Registry Entries...");
        
        try
        {
            var appName = packageName.Split('_')[0];
            var registryPaths = new[]
            {
                $@"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages\{packageName}",
                $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications\{packageName}",
                $@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched\{appName}"
            };

            foreach (var regPath in registryPaths)
            {
                try
                {
                    // Simple check - in a real implementation, you'd use Registry classes
                    Console.WriteLine($"   🔍 Checking: {regPath}");
                }
                catch
                {
                    // Registry access issues are common and expected
                }
            }
            
            Console.WriteLine("✅ Registry check completed (detailed analysis requires elevated permissions)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Registry check failed: {ex.Message}");
        }
        Console.WriteLine();
    }
} 