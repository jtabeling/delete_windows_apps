using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WindowsAppsManager
{
    /// <summary>
    /// Standalone diagnostic tool for analyzing app deletion issues
    /// </summary>
    public class DeletionDiagnostic
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🔍 Windows App Deletion Diagnostic Tool");
            Console.WriteLine("=" + new string('=', 50));
            
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: DeletionDiagnostic.exe <AppPackageName>");
                Console.WriteLine("Example: DeletionDiagnostic.exe Microsoft.BingWeather_8wekyb3d8bbwe");
                return;
            }
            
            string packageName = args[0];
            await DiagnoseAppDeletion(packageName);
        }
        
        public static async Task DiagnoseAppDeletion(string packageName)
        {
            Console.WriteLine($"🎯 Analyzing: {packageName}");
            Console.WriteLine("");
            
            // 1. Check if package exists in PowerShell
            Console.WriteLine("1️⃣ Checking PowerShell package visibility...");
            bool packageExists = await CheckPackageExists(packageName);
            Console.WriteLine($"   Result: {(packageExists ? "✅ Found" : "❌ Not found")}");
            
            if (!packageExists)
            {
                Console.WriteLine("   💡 App may already be deleted or corrupted in package database");
            }
            
            // 2. Check physical folder
            Console.WriteLine("\n2️⃣ Checking physical app folder...");
            string folderPath = await GetAppFolderPath(packageName);
            bool folderExists = !string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath);
            Console.WriteLine($"   Path: {folderPath ?? "Not found"}");
            Console.WriteLine($"   Exists: {(folderExists ? "✅ Yes" : "❌ No")}");
            
            // 3. Check running processes
            Console.WriteLine("\n3️⃣ Checking for running processes...");
            var processes = GetRelatedProcesses(packageName, folderPath);
            if (processes.Any())
            {
                Console.WriteLine($"   ⚠️  Found {processes.Count} running process(es):");
                foreach (var process in processes)
                {
                    Console.WriteLine($"      - {process.ProcessName} (PID: {process.Id})");
                }
            }
            else
            {
                Console.WriteLine("   ✅ No running processes found");
            }
            
            // 4. Test folder permissions
            if (folderExists)
            {
                Console.WriteLine("\n4️⃣ Testing folder permissions...");
                bool canWrite = TestFolderPermissions(folderPath);
                Console.WriteLine($"   Write Access: {(canWrite ? "✅ Yes" : "❌ No")}");
            }
            
            // 5. Check PowerShell execution policy
            Console.WriteLine("\n5️⃣ Checking PowerShell execution policy...");
            string policy = await GetExecutionPolicy();
            Console.WriteLine($"   Policy: {policy}");
            bool policyOk = policy != "Restricted";
            Console.WriteLine($"   PowerShell Deletion Allowed: {(policyOk ? "✅ Yes" : "❌ No")}");
            
            // 6. Test PowerShell deletion command
            if (packageExists && policyOk)
            {
                Console.WriteLine("\n6️⃣ Testing PowerShell deletion command...");
                await TestPowerShellDeletion(packageName);
            }
            
            // 7. Summary and recommendations
            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("📋 SUMMARY & RECOMMENDATIONS");
            Console.WriteLine(new string('=', 50));
            
            if (!packageExists && !folderExists)
            {
                Console.WriteLine("✅ App appears to be already deleted");
            }
            else if (packageExists && !folderExists)
            {
                Console.WriteLine("⚠️  Package registration exists but no folder - run 'wsreset' to refresh");
            }
            else if (!packageExists && folderExists)
            {
                Console.WriteLine("⚠️  Folder exists but package not registered - manual cleanup needed");
            }
            else
            {
                Console.WriteLine("❌ App deletion issues detected:");
                
                if (processes.Any())
                    Console.WriteLine("   • Close running processes first");
                    
                if (!policyOk)
                    Console.WriteLine("   • Fix PowerShell execution policy");
                    
                if (folderExists && !TestFolderPermissions(folderPath))
                    Console.WriteLine("   • Run as Administrator for folder access");
            }
        }
        
        private static async Task<bool> CheckPackageExists(string packageName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Get-AppxPackage | Where-Object {{$_.PackageFullName -eq '{packageName}'}} | Measure-Object | Select-Object -ExpandProperty Count\"",
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
                        return int.TryParse(output.Trim(), out int count) && count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error checking package: {ex.Message}");
            }
            return false;
        }
        
        private static async Task<string> GetAppFolderPath(string packageName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Get-AppxPackage | Where-Object {{$_.PackageFullName -eq '{packageName}'}} | Select-Object -ExpandProperty InstallLocation\"",
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
                        return output.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error getting folder path: {ex.Message}");
            }
            return "";
        }
        
        private static System.Collections.Generic.List<Process> GetRelatedProcesses(string packageName, string folderPath)
        {
            var relatedProcesses = new System.Collections.Generic.List<Process>();
            
            try
            {
                var allProcesses = Process.GetProcesses();
                
                foreach (var process in allProcesses)
                {
                    try
                    {
                        if (process.HasExited) continue;
                        
                        // Check by package name
                        if (process.ProcessName.Contains(packageName.Split('_')[0], StringComparison.OrdinalIgnoreCase))
                        {
                            relatedProcesses.Add(process);
                            continue;
                        }
                        
                        // Check by folder path
                        if (!string.IsNullOrEmpty(folderPath))
                        {
                            try
                            {
                                var processPath = process.MainModule?.FileName;
                                if (!string.IsNullOrEmpty(processPath) && 
                                    processPath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    relatedProcesses.Add(process);
                                }
                            }
                            catch
                            {
                                // Access denied to process info - skip
                            }
                        }
                    }
                    catch
                    {
                        // Skip processes we can't access
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error checking processes: {ex.Message}");
            }
            
            return relatedProcesses;
        }
        
        private static bool TestFolderPermissions(string folderPath)
        {
            try
            {
                var testFile = Path.Combine(folderPath, "test_write_access.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private static async Task<string> GetExecutionPolicy()
        {
            try
            {
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
                        var output = await process.StandardOutput.ReadToEndAsync();
                        return output.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error checking execution policy: {ex.Message}");
            }
            return "Unknown";
        }
        
        private static async Task TestPowerShellDeletion(string packageName)
        {
            try
            {
                Console.WriteLine("   Testing Remove-AppxPackage command...");
                
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Write-Host 'Would execute: Remove-AppxPackage -Package {packageName} -Confirm:$false'\"",
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
                        Console.WriteLine($"   Command Test: ✅ {output.Trim()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ PowerShell command test failed: {ex.Message}");
            }
        }
    }
} 