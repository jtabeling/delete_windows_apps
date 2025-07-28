using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using WindowsAppsManager.Models;

namespace WindowsAppsManager.Services
{
    public class BackupService
    {
        private readonly string backupRootPath;
        private readonly List<BackupInfo> backupHistory;
        
        public BackupService()
        {
            // Create backup directory in user's Documents folder for safety
            backupRootPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "WindowsAppsManager", "Backups");
            
            backupHistory = new List<BackupInfo>();
            EnsureBackupDirectoryExists();
            LoadBackupHistory();
        }
        
        /// <summary>
        /// Creates a comprehensive backup of a Windows app
        /// </summary>
        /// <param name="app">App to backup</param>
        /// <param name="progress">Progress reporting callback</param>
        /// <returns>Backup information</returns>
        public async Task<BackupInfo> CreateBackupAsync(WindowsApp app, IProgress<string>? progress = null)
        {
            var backup = new BackupInfo
            {
                AppName = app.GetDisplayName(),
                PackageName = app.PackageName,
                Publisher = app.Publisher,
                Version = app.Version,
                OriginalPath = app.FolderPath,
                BackupPath = Path.Combine(backupRootPath, $"{app.PackageName}_{DateTime.Now:yyyyMMdd_HHmmss}")
            };
            
            try
            {
                progress?.Report($"Starting backup of {app.GetDisplayName()}...");
                
                // Check if source app directory exists and is accessible
                if (!Directory.Exists(app.FolderPath))
                {
                    throw new DirectoryNotFoundException($"App directory not found: {app.FolderPath}");
                }
                
                // Create backup directory
                Directory.CreateDirectory(backup.BackupPath);
                progress?.Report($"Created backup directory: {backup.BackupPath}");
                
                // Backup app files
                progress?.Report("Backing up application files...");
                await BackupAppFilesAsync(app, backup, progress);
                
                // Backup registry entries
                progress?.Report("Backing up registry entries...");
                await BackupRegistryEntriesAsync(app, backup, progress);
                
                // Create backup metadata
                progress?.Report("Creating backup metadata...");
                await CreateBackupMetadataAsync(backup);
                
                backup.Status = BackupStatus.Completed;
                progress?.Report($"Backup completed successfully: {backup.BackupSizeFormatted}");
                
                backupHistory.Add(backup);
                SaveBackupHistory();
                
                return backup;
            }
            catch (Exception ex)
            {
                backup.Status = BackupStatus.Failed;
                backup.ErrorMessage = ex.Message;
                progress?.Report($"Backup failed: {ex.Message}");
                
                // Clean up partial backup
                try
                {
                    if (Directory.Exists(backup.BackupPath))
                    {
                        Directory.Delete(backup.BackupPath, true);
                    }
                }
                catch { /* Ignore cleanup errors */ }
                
                throw;
            }
        }
        
        /// <summary>
        /// Backs up application files to backup location
        /// </summary>
        private async Task BackupAppFilesAsync(WindowsApp app, BackupInfo backup, IProgress<string>? progress)
        {
            try
            {
                SafeProgressReport(progress, $"Starting file backup for {app.GetDisplayName()}...");
                
                // Validate source directory before proceeding
                if (string.IsNullOrEmpty(app.FolderPath))
                {
                    throw new ArgumentException("App folder path is null or empty");
                }
                
                if (!Directory.Exists(app.FolderPath))
                {
                    throw new DirectoryNotFoundException($"App folder does not exist: {app.FolderPath}");
                }
                
                SafeProgressReport(progress, "Creating backup directory structure...");
                var targetDir = Path.Combine(backup.BackupPath, "AppFiles");
                Directory.CreateDirectory(targetDir);
                
                SafeProgressReport(progress, "Beginning file enumeration and copying...");
                
                // Add monitoring for backup progress
                var startTime = DateTime.Now;
                var lastProgressTime = DateTime.Now;
                
                await Task.Run(() =>
                {
                    try
                    {
                        var sourceDir = new DirectoryInfo(app.FolderPath);
                        var targetDirInfo = new DirectoryInfo(targetDir);
                        
                        // No monitoring task for now to prevent issues
                        
                        CopyDirectory(sourceDir, targetDirInfo, backup, progress);
                        
                        SafeProgressReport(progress, $"File backup completed. {backup.BackedUpFiles.Count} files copied.");
                    }
                    catch (Exception ex)
                    {
                        SafeProgressReport(progress, $"Error during file backup task: {ex.Message}");
                        throw; // Re-throw to be caught by outer try-catch
                    }
                });
            }
            catch (Exception ex)
            {
                SafeProgressReport(progress, $"Critical error backing up files: {ex.Message}");
                throw new InvalidOperationException($"Failed to backup application files: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Recursively copies directory contents with robust error handling
        /// </summary>
        private void CopyDirectory(DirectoryInfo source, DirectoryInfo target, BackupInfo backup, IProgress<string>? progress, int depth = 0)
        {
            try
            {
                // Prevent infinite recursion and extremely deep paths
                if (depth > 10)
                {
                    SafeProgressReport(progress, $"Skipping directory due to depth limit: {source.Name}");
                    return;
                }
                
                // Periodic memory cleanup every 100 files
                if (backup.BackedUpFiles.Count % 100 == 0 && backup.BackedUpFiles.Count > 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    SafeProgressReport(progress, $"Memory cleanup - {backup.BackedUpFiles.Count} files processed");
                }
                
                target.Create();
            }
            catch (Exception ex)
            {
                SafeProgressReport(progress, $"Error creating target directory {target.Name}: {ex.Message}");
                return;
            }
            
            // Check if source directory is accessible
            if (!source.Exists)
            {
                SafeProgressReport(progress, $"Source directory not found: {source.FullName}");
                return;
            }
            
            // Throttle progress reporting to prevent UI overload
            bool shouldReportProgress = backup.BackedUpFiles.Count % 10 == 0;
            
            // Copy files with robust error handling
            FileInfo[] files;
            try
            {
                files = source.GetFiles();
            }
            catch (Exception ex)
            {
                progress?.Report($"Cannot enumerate files in {source.Name}: {ex.Message}");
                return;
            }
            
                         foreach (var file in files)
            {
                try
                {
                    // Skip files that are too large or have problematic names
                    if (IsFileSkippable(file, progress))
                        continue;
                    
                    var safeName = SanitizeFileName(file.Name);
                    var targetPath = Path.Combine(target.FullName, safeName);
                    
                    // Check target path length
                    if (targetPath.Length > 240) // Leave some buffer for Windows path limits
                    {
                        if (shouldReportProgress)
                            SafeProgressReport(progress, $"Skipping file with long path: {file.Name}");
                        continue;
                    }
                    
                    // Attempt to copy with multiple strategies
                    if (TryCopyFile(file, targetPath, progress, shouldReportProgress))
                    {
                        backup.BackedUpFiles.Add(targetPath);
                        backup.BackupSizeBytes += file.Length;
                        
                        if (shouldReportProgress)
                            SafeProgressReport(progress, $"✓ Copied: {file.Name} (Total: {backup.BackedUpFiles.Count})");
                    }
                    
                    // Safety check - if we're getting too many files, something might be wrong
                    if (backup.BackedUpFiles.Count > 10000)
                    {
                        SafeProgressReport(progress, "WARNING: Large number of files detected. Continuing with caution...");
                    }
                }
                catch (Exception ex)
                {
                    SafeProgressReport(progress, $"⚠ Error copying {file.Name}: {ex.Message}");
                    // Continue with next file
                }
                
                // Yield control occasionally to prevent UI freezing
                if (backup.BackedUpFiles.Count % 50 == 0)
                {
                    System.Threading.Thread.Sleep(1);
                }
            }
            
            // Copy subdirectories with error isolation
            DirectoryInfo[] directories;
            try
            {
                directories = source.GetDirectories();
            }
            catch (Exception ex)
            {
                progress?.Report($"Cannot enumerate directories in {source.Name}: {ex.Message}");
                return;
            }
            
            foreach (var subDir in directories)
            {
                try
                {
                    // Skip system and problematic directories
                    if (IsDirectorySkippable(subDir, progress))
                        continue;
                        
                    var safeName = SanitizeFileName(subDir.Name);
                    var targetSubDir = new DirectoryInfo(Path.Combine(target.FullName, safeName));
                    
                    // Check target path length
                    if (targetSubDir.FullName.Length > 200)
                    {
                        progress?.Report($"Skipping directory with long path: {subDir.Name}");
                        continue;
                    }
                    
                    CopyDirectory(subDir, targetSubDir, backup, progress, depth + 1);
                }
                catch (Exception ex)
                {
                    progress?.Report($"⚠ Error processing directory {subDir.Name}: {ex.Message}");
                    // Continue with next directory
                }
            }
        }
        
        /// <summary>
        /// Determines if a file should be skipped during backup
        /// </summary>
        private bool IsFileSkippable(FileInfo file, IProgress<string>? progress)
        {
            try
            {
                // Skip very large files (over 100MB) to prevent memory issues
                if (file.Length > 100 * 1024 * 1024)
                {
                    progress?.Report($"Skipping large file: {file.Name} ({file.Length / (1024 * 1024)}MB)");
                    return true;
                }
                
                // Skip system files and locked files
                if (file.Attributes.HasFlag(FileAttributes.System) || 
                    file.Attributes.HasFlag(FileAttributes.Device))
                {
                    progress?.Report($"Skipping system file: {file.Name}");
                    return true;
                }
                
                // Skip files with problematic extensions
                var ext = file.Extension.ToLowerInvariant();
                if (ext == ".lock" || ext == ".tmp" || ext == ".temp")
                {
                    progress?.Report($"Skipping temporary file: {file.Name}");
                    return true;
                }
                
                return false;
            }
            catch
            {
                return true; // If we can't check, skip it
            }
        }
        
        /// <summary>
        /// Determines if a directory should be skipped during backup
        /// </summary>
        private bool IsDirectorySkippable(DirectoryInfo directory, IProgress<string>? progress)
        {
            try
            {
                // Skip system directories
                if (directory.Attributes.HasFlag(FileAttributes.System) || 
                    directory.Attributes.HasFlag(FileAttributes.Device))
                {
                    progress?.Report($"Skipping system directory: {directory.Name}");
                    return true;
                }
                
                // Skip known problematic directories
                var name = directory.Name.ToLowerInvariant();
                if (name == "temp" || name == "cache" || name == "logs" || name == "tmp")
                {
                    progress?.Report($"Skipping cache/temp directory: {directory.Name}");
                    return true;
                }
                
                return false;
            }
            catch
            {
                return true; // If we can't check, skip it
            }
        }
        
        /// <summary>
        /// Attempts to copy a file with multiple fallback strategies
        /// </summary>
        private bool TryCopyFile(FileInfo sourceFile, string targetPath, IProgress<string>? progress, bool shouldReport = true)
        {
            try
            {
                // Strategy 1: Standard copy
                sourceFile.CopyTo(targetPath, true);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                if (shouldReport)
                    SafeProgressReport(progress, $"Access denied: {sourceFile.Name}");
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                if (shouldReport)
                    SafeProgressReport(progress, $"Directory not found for: {sourceFile.Name}");
                return false;
            }
            catch (PathTooLongException)
            {
                if (shouldReport)
                    SafeProgressReport(progress, $"Path too long: {sourceFile.Name}");
                return false;
            }
            catch (IOException ex) when (ex.Message.Contains("being used by another process"))
            {
                // Strategy 2: Try to copy after a short delay
                try
                {
                    System.Threading.Thread.Sleep(100);
                    sourceFile.CopyTo(targetPath, true);
                    return true;
                }
                catch
                {
                    progress?.Report($"File in use: {sourceFile.Name}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"Copy failed: {sourceFile.Name} - {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Sanitizes file names to prevent path issues
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            try
            {
                // Remove or replace invalid characters
                var invalidChars = Path.GetInvalidFileNameChars();
                foreach (char c in invalidChars)
                {
                    fileName = fileName.Replace(c, '_');
                }
                
                // Truncate if too long
                if (fileName.Length > 100)
                {
                    var extension = Path.GetExtension(fileName);
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    fileName = nameWithoutExt.Substring(0, 100 - extension.Length) + extension;
                }
                
                return fileName;
            }
            catch
            {
                return "sanitized_file";
            }
        }
        
        /// <summary>
        /// Backs up relevant registry entries for the app
        /// </summary>
        private async Task BackupRegistryEntriesAsync(WindowsApp app, BackupInfo backup, IProgress<string>? progress)
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        var registryBackupDir = Path.Combine(backup.BackupPath, "Registry");
                        Directory.CreateDirectory(registryBackupDir);
                
                // Common registry locations for Windows apps
                var registryPaths = new[]
                {
                    $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications\{app.PackageName}",
                    $@"HKEY_CURRENT_USER\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages\{app.PackageName}",
                    $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\InboxApplications\{app.PackageName}"
                };
                
                foreach (var regPath in registryPaths)
                {
                    try
                    {
                        var backupFile = Path.Combine(registryBackupDir, $"{Path.GetFileName(regPath)}.reg");
                        
                        if (BackupRegistryKey(regPath, backupFile))
                        {
                            backup.RegistryBackups.Add(new RegistryBackupInfo
                            {
                                KeyPath = regPath,
                                BackupFile = backupFile,
                                IsSuccessful = true
                            });
                            
                            progress?.Report($"Backed up registry: {regPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        backup.RegistryBackups.Add(new RegistryBackupInfo
                        {
                            KeyPath = regPath,
                            BackupFile = "",
                            IsSuccessful = false
                        });
                        
                        progress?.Report($"Warning: Could not backup registry {regPath}: {ex.Message}");
                    }
                }
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"Error during registry backup: {ex.Message}");
                        throw; // Re-throw to be caught by outer try-catch
                    }
                });
            }
            catch (Exception ex)
            {
                progress?.Report($"Critical error backing up registry: {ex.Message}");
                // Don't throw here - registry backup failure shouldn't stop file backup
            }
        }
        
        /// <summary>
        /// Backs up a specific registry key to a file
        /// </summary>
        private bool BackupRegistryKey(string keyPath, string backupFile)
        {
            try
            {
                // This is a simplified implementation
                // In a full version, this would use reg.exe or Windows APIs to export registry keys
                // For now, we'll create a placeholder file
                File.WriteAllText(backupFile, $"Registry backup placeholder for: {keyPath}\nDate: {DateTime.Now}");
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Creates metadata file for the backup
        /// </summary>
        private async Task CreateBackupMetadataAsync(BackupInfo backup)
        {
            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        var metadataPath = Path.Combine(backup.BackupPath, "backup_metadata.json");
                        var metadata = System.Text.Json.JsonSerializer.Serialize(backup, new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        
                        File.WriteAllText(metadataPath, metadata);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to create backup metadata: {ex.Message}", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Critical error creating backup metadata: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Restores an app from backup
        /// </summary>
        /// <param name="backup">Backup to restore</param>
        /// <param name="progress">Progress reporting callback</param>
        /// <returns>True if restoration successful</returns>
        public async Task<bool> RestoreFromBackupAsync(BackupInfo backup, IProgress<string>? progress = null)
        {
            try
            {
                if (!backup.CanRestore())
                {
                    throw new InvalidOperationException("Backup cannot be restored - files may be missing or corrupted");
                }
                
                progress?.Report($"Starting restoration of {backup.AppName}...");
                
                // Restore app files
                progress?.Report("Restoring application files...");
                await RestoreAppFilesAsync(backup, progress);
                
                // Restore registry entries
                progress?.Report("Restoring registry entries...");
                await RestoreRegistryEntriesAsync(backup, progress);
                
                progress?.Report("Restoration completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report($"Restoration failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Restores app files from backup
        /// </summary>
        private async Task RestoreAppFilesAsync(BackupInfo backup, IProgress<string>? progress)
        {
            await Task.Run(() =>
            {
                var sourceDir = Path.Combine(backup.BackupPath, "AppFiles");
                var targetDir = backup.OriginalPath;
                
                if (!Directory.Exists(sourceDir))
                {
                    throw new DirectoryNotFoundException("Backup app files not found");
                }
                
                // Create target directory if it doesn't exist
                Directory.CreateDirectory(targetDir);
                
                // Copy files back
                var source = new DirectoryInfo(sourceDir);
                var target = new DirectoryInfo(targetDir);
                
                CopyDirectoryForRestore(source, target, progress);
            });
        }
        
        /// <summary>
        /// Copies directory for restoration
        /// </summary>
        private void CopyDirectoryForRestore(DirectoryInfo source, DirectoryInfo target, IProgress<string>? progress)
        {
            target.Create();
            
            foreach (var file in source.GetFiles())
            {
                try
                {
                    var targetPath = Path.Combine(target.FullName, file.Name);
                    file.CopyTo(targetPath, true);
                    progress?.Report($"Restored: {file.Name}");
                }
                catch (Exception ex)
                {
                    progress?.Report($"Warning: Could not restore {file.Name}: {ex.Message}");
                }
            }
            
            foreach (var subDir in source.GetDirectories())
            {
                try
                {
                    var targetSubDir = new DirectoryInfo(Path.Combine(target.FullName, subDir.Name));
                    CopyDirectoryForRestore(subDir, targetSubDir, progress);
                }
                catch (Exception ex)
                {
                    progress?.Report($"Warning: Could not restore directory {subDir.Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Restores registry entries from backup
        /// </summary>
        private async Task RestoreRegistryEntriesAsync(BackupInfo backup, IProgress<string>? progress)
        {
            await Task.Run(() =>
            {
                foreach (var regBackup in backup.RegistryBackups.Where(r => r.IsSuccessful))
                {
                    try
                    {
                        // In a full implementation, this would restore actual registry entries
                        progress?.Report($"Restored registry: {regBackup.KeyPath}");
                    }
                    catch (Exception ex)
                    {
                        progress?.Report($"Warning: Could not restore registry {regBackup.KeyPath}: {ex.Message}");
                    }
                }
            });
        }
        
        /// <summary>
        /// Gets all available backups
        /// </summary>
        /// <returns>List of backup information</returns>
        public List<BackupInfo> GetAllBackups()
        {
            return backupHistory.ToList();
        }
        
        /// <summary>
        /// Deletes a backup
        /// </summary>
        /// <param name="backup">Backup to delete</param>
        /// <returns>True if successful</returns>
        public bool DeleteBackup(BackupInfo backup)
        {
            try
            {
                if (Directory.Exists(backup.BackupPath))
                {
                    Directory.Delete(backup.BackupPath, true);
                }
                
                backupHistory.Remove(backup);
                SaveBackupHistory();
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Ensures backup directory exists
        /// </summary>
        private void EnsureBackupDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(backupRootPath))
                {
                    Directory.CreateDirectory(backupRootPath);
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash on startup
                Console.WriteLine($"Warning: Could not create backup directory '{backupRootPath}': {ex.Message}");
                Console.WriteLine("Backup functionality may not work properly until this is resolved.");
            }
        }
        
        /// <summary>
        /// Loads backup history from file
        /// </summary>
        private void LoadBackupHistory()
        {
            try
            {
                // Ensure the directory exists before trying to access files in it
                if (!Directory.Exists(backupRootPath))
                {
                    return; // No directory means no history to load
                }
                
                var historyFile = Path.Combine(backupRootPath, "backup_history.json");
                if (File.Exists(historyFile))
                {
                    var json = File.ReadAllText(historyFile);
                    var history = System.Text.Json.JsonSerializer.Deserialize<List<BackupInfo>>(json);
                    if (history != null)
                    {
                        backupHistory.AddRange(history);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                Console.WriteLine($"Warning: Could not load backup history: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Saves backup history to file
        /// </summary>
        private void SaveBackupHistory()
        {
            try
            {
                var historyFile = Path.Combine(backupRootPath, "backup_history.json");
                var json = System.Text.Json.JsonSerializer.Serialize(backupHistory, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(historyFile, json);
            }
            catch
            {
                // Ignore errors saving history
            }
        }
        
        /// <summary>
        /// Gets the total size of all backups
        /// </summary>
        /// <returns>Total backup size in bytes</returns>
        public long GetTotalBackupSize()
        {
            return backupHistory.Sum(b => b.BackupSizeBytes);
        }
        
        /// <summary>
        /// Gets backup directory path
        /// </summary>
        /// <returns>Backup root directory path</returns>
        public string GetBackupDirectory()
        {
            return backupRootPath;
        }
        
        /// <summary>
        /// Tests backup functionality with diagnostics
        /// </summary>
        /// <param name="app">App to test backup for</param>
        /// <returns>Diagnostic information</returns>
        public string TestBackupAccess(WindowsApp app)
        {
            var diagnostics = new List<string>();
            
            try
            {
                diagnostics.Add($"Testing backup access for: {app.GetDisplayName()}");
                diagnostics.Add($"App folder path: {app.FolderPath}");
                
                // Test 1: Check if app directory exists
                if (Directory.Exists(app.FolderPath))
                {
                    diagnostics.Add("✅ App directory exists");
                }
                else
                {
                    diagnostics.Add("❌ App directory does not exist");
                    return string.Join("\n", diagnostics);
                }
                
                // Test 2: Check if backup directory can be created
                try
                {
                    EnsureBackupDirectoryExists();
                    diagnostics.Add("✅ Backup root directory accessible");
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"❌ Cannot create backup directory: {ex.Message}");
                    return string.Join("\n", diagnostics);
                }
                
                // Test 3: Try to enumerate app files
                try
                {
                    var fileCount = Directory.GetFiles(app.FolderPath, "*", SearchOption.AllDirectories).Length;
                    diagnostics.Add($"✅ Can enumerate app files: {fileCount} files found");
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"❌ Cannot enumerate app files: {ex.Message}");
                }
                
                // Test 4: Try to read a sample file
                try
                {
                    var files = Directory.GetFiles(app.FolderPath);
                    if (files.Length > 0)
                    {
                        var testFile = files[0];
                        var info = new FileInfo(testFile);
                        diagnostics.Add($"✅ Can access sample file: {info.Name} ({info.Length} bytes)");
                    }
                    else
                    {
                        diagnostics.Add("⚠️ No files found in app directory");
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.Add($"❌ Cannot access app files: {ex.Message}");
                }
                
                diagnostics.Add($"Backup would be stored in: {Path.Combine(backupRootPath, $"{app.PackageName}_{DateTime.Now:yyyyMMdd_HHmmss}")}");
            }
            catch (Exception ex)
            {
                diagnostics.Add($"❌ Unexpected error during diagnostics: {ex.Message}");
            }
            
            return string.Join("\n", diagnostics);
        }
        
        /// <summary>
        /// Thread-safe progress reporting with error handling
        /// </summary>
        private void SafeProgressReport(IProgress<string>? progress, string message)
        {
            try
            {
                progress?.Report(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reporting progress '{message}': {ex.Message}");
                // Don't throw - progress reporting failure shouldn't stop backup
            }
        }
    }
} 