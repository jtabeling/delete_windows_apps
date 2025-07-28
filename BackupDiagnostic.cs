using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WindowsAppsManager.Models;
using WindowsAppsManager.Services;

namespace WindowsAppsManager
{
    /// <summary>
    /// Diagnostic tool to identify backup crash locations
    /// </summary>
    public class BackupDiagnostic
    {
        private static string logPath = "backup_diagnostic.log";
        private static object logLock = new object();
        
        public static async Task RunDiagnostic(string appFolderPath)
        {
            File.WriteAllText(logPath, $"Backup Diagnostic Started: {DateTime.Now}\n");
            
            try
            {
                Log("=== BACKUP DIAGNOSTIC TOOL ===");
                Log($"Testing backup of: {appFolderPath}");
                
                if (!Directory.Exists(appFolderPath))
                {
                    Log($"ERROR: Directory does not exist: {appFolderPath}");
                    return;
                }
                
                // Create test app object
                var testApp = new WindowsApp
                {
                    FolderPath = appFolderPath,
                    Name = "Test App",
                    PackageName = "TestApp"
                };
                
                // Create backup service
                var backupService = new BackupService();
                
                // Create progress handler with detailed logging
                var progress = new Progress<string>(message =>
                {
                    Log($"PROGRESS: {message}");
                    Console.WriteLine($"PROGRESS: {message}");
                });
                
                Log("Starting backup operation...");
                
                try
                {
                    var backup = await backupService.CreateBackupAsync(testApp, progress);
                    Log($"SUCCESS: Backup completed! Files: {backup.BackedUpFiles.Count}, Size: {backup.BackupSizeBytes} bytes");
                }
                catch (Exception ex)
                {
                    Log($"BACKUP FAILED: {ex.GetType().Name}: {ex.Message}");
                    Log($"Stack Trace: {ex.StackTrace}");
                    
                    if (ex.InnerException != null)
                    {
                        Log($"Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                        Log($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"DIAGNOSTIC TOOL ERROR: {ex.Message}");
                Log($"Stack Trace: {ex.StackTrace}");
            }
            
            Log($"Diagnostic completed: {DateTime.Now}");
            Console.WriteLine($"Diagnostic log saved to: {Path.GetFullPath(logPath)}");
        }
        
        private static void Log(string message)
        {
            lock (logLock)
            {
                try
                {
                    File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
                }
                catch
                {
                    // If we can't log, just continue
                }
            }
        }
        
        // Note: Main method removed to avoid conflicts with Program.cs
        // To use this diagnostic tool, call RunDiagnostic() from other code
    }
} 