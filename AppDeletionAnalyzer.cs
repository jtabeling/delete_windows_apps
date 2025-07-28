using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WindowsAppsManager
{
    /// <summary>
    /// Analyzes why specific Windows apps cannot be deleted using Remove-AppxPackage
    /// </summary>
    public class AppDeletionAnalyzer
    {
        public static async Task AnalyzeApp(string packageName)
        {
            Console.WriteLine("🔍 Windows App Deletion Analysis");
            Console.WriteLine("=" + new string('=', 60));
            Console.WriteLine($"🎯 Analyzing: {packageName}");
            Console.WriteLine("");

            // Get detailed package information
            await GetPackageDetails(packageName);
            
            // Check if it's a system/protected app
            await CheckSystemProtection(packageName);
            
            // Check dependencies
            await CheckDependencies(packageName);
            
            // Check installation type
            await CheckInstallationType(packageName);
            
            // Test actual removal simulation
            await TestRemovalSimulation(packageName);
            
            // Provide specific recommendations
            await ProvideRecommendations(packageName);
        }

        private static async Task GetPackageDetails(string packageName)
        {
            Console.WriteLine("1️⃣ PACKAGE DETAILS");
            Console.WriteLine(new string('-', 30));
            
            var command = $@"
                Get-AppxPackage | Where-Object {{$_.PackageFullName -eq '{packageName}'}} | 
                Select-Object Name, Publisher, Version, Architecture, ResourceId, IsFramework, IsBundle, 
                             IsResourcePackage, IsStub, SignatureKind, InstallLocation, 
                             @{{Name='SystemApp';Expression={{$_.NonRemovable}}}},
                             @{{Name='IsProvisioned';Expression={{(Get-AppxProvisionedPackage -Online | Where-Object {{$_.PackageName -eq $_.Name}}) -ne $null}}}} |
                Format-List
            ";
            
            await ExecutePowerShellCommand(command, "Package Details");
        }

        private static async Task CheckSystemProtection(string packageName)
        {
            Console.WriteLine("\n2️⃣ SYSTEM PROTECTION CHECK");
            Console.WriteLine(new string('-', 30));
            
            // Check if it's marked as NonRemovable
            var command = $@"
                $pkg = Get-AppxPackage | Where-Object {{$_.PackageFullName -eq '{packageName}'}}
                if ($pkg) {{
                    Write-Output ""Non-Removable: $($pkg.NonRemovable)""
                    Write-Output ""Signature Kind: $($pkg.SignatureKind)""
                    Write-Output ""Is Framework: $($pkg.IsFramework)""
                    
                    # Check if it's a critical system component
                    $criticalApps = @('Microsoft.WindowsStore', 'Microsoft.Windows.Settings', 
                                    'Microsoft.VCLibs', 'Microsoft.NET.Native', 'Microsoft.Windows.SecHealthUI')
                    $isCritical = $criticalApps | ForEach-Object {{ $pkg.Name -like ""*$_*"" }} | Where-Object {{ $_ -eq $true }}
                    Write-Output ""Is Critical System Component: $($isCritical -ne $null)""
                }} else {{
                    Write-Output ""Package not found""
                }}
            ";
            
            await ExecutePowerShellCommand(command, "System Protection");
        }

        private static async Task CheckDependencies(string packageName)
        {
            Console.WriteLine("\n3️⃣ DEPENDENCY ANALYSIS");
            Console.WriteLine(new string('-', 30));
            
            var command = $@"
                # Find apps that depend on this package
                $targetPkg = Get-AppxPackage | Where-Object {{$_.PackageFullName -eq '{packageName}'}}
                if ($targetPkg) {{
                    $dependentApps = Get-AppxPackage | Where-Object {{ 
                        $_.Dependencies -contains $targetPkg.PackageFullName -or
                        $_.Dependencies.Name -contains $targetPkg.Name
                    }}
                    
                    if ($dependentApps) {{
                        Write-Output ""⚠️  DEPENDENT APPS FOUND:""
                        $dependentApps | ForEach-Object {{ Write-Output ""  - $($_.Name)"" }}
                        Write-Output """"
                        Write-Output ""⚠️  WARNING: Removing this app may break the dependent apps above!""
                    }} else {{
                        Write-Output ""✅ No dependent apps found - safe to remove""
                    }}
                    
                    # Check what this app depends on
                    if ($targetPkg.Dependencies) {{
                        Write-Output """"
                        Write-Output ""📦 This app depends on:""
                        $targetPkg.Dependencies | ForEach-Object {{ Write-Output ""  - $_"" }}
                    }}
                }} else {{
                    Write-Output ""Package not found""
                }}
            ";
            
            await ExecutePowerShellCommand(command, "Dependencies");
        }

        private static async Task CheckInstallationType(string packageName)
        {
            Console.WriteLine("\n4️⃣ INSTALLATION TYPE");
            Console.WriteLine(new string('-', 30));
            
            var command = $@"
                # Check if it's provisioned (installed for all users)
                $provisioned = Get-AppxProvisionedPackage -Online | Where-Object {{ $_.PackageName -like ""*{packageName.Split('_')[0]}*"" }}
                if ($provisioned) {{
                    Write-Output ""📦 PROVISIONED PACKAGE (installed for all future users)""
                    Write-Output ""   Package Name: $($provisioned.PackageName)""
                    Write-Output ""   Display Name: $($provisioned.DisplayName)""
                    Write-Output ""   ⚠️  Requires: Remove-AppxProvisionedPackage -Online""
                }} else {{
                    Write-Output ""✅ Not a provisioned package""
                }}
                
                # Check installation location
                $pkg = Get-AppxPackage | Where-Object {{$_.PackageFullName -eq '{packageName}'}}
                if ($pkg -and $pkg.InstallLocation) {{
                    Write-Output """"
                    Write-Output ""📁 Install Location: $($pkg.InstallLocation)""
                    if ($pkg.InstallLocation -like ""*Program Files*"" -or $pkg.InstallLocation -like ""*WindowsApps*"") {{
                        Write-Output ""   ⚠️  System-level installation detected""
                    }}
                }}
            ";
            
            await ExecutePowerShellCommand(command, "Installation Type");
        }

        private static async Task TestRemovalSimulation(string packageName)
        {
            Console.WriteLine("\n5️⃣ REMOVAL SIMULATION TEST");
            Console.WriteLine(new string('-', 30));
            
            // Test different removal methods without actually removing
            var methods = new[]
            {
                ("Standard", $"Remove-AppxPackage -Package '{packageName}' -WhatIf"),
                ("All Users", $"Remove-AppxPackage -Package '{packageName}' -AllUsers -WhatIf"),
                ("Provisioned", $@"
                    $prov = Get-AppxProvisionedPackage -Online | Where-Object {{ $_.PackageName -like ""*{packageName.Split('_')[0]}*"" }}
                    if ($prov) {{ 
                        Write-Output ""Would remove provisioned package: $($prov.PackageName)""
                    }} else {{ 
                        Write-Output ""No provisioned package found""
                    }}
                ")
            };

            foreach (var (name, command) in methods)
            {
                Console.WriteLine($"\n🔹 Testing {name} Method:");
                await ExecutePowerShellCommand(command, $"{name} Test", showErrors: true);
            }
        }

        private static async Task ProvideRecommendations(string packageName)
        {
            Console.WriteLine("\n6️⃣ RECOMMENDATIONS");
            Console.WriteLine(new string('-', 30));
            
            var appBaseName = packageName.Split('_')[0];
            
            // Check for known problematic apps
            var protectedApps = new[]
            {
                ("Microsoft.WindowsStore", "❌ CANNOT BE REMOVED - Core system component"),
                ("Microsoft.Windows.Settings", "❌ CANNOT BE REMOVED - System settings app"),
                ("Microsoft.VCLibs", "❌ DO NOT REMOVE - Required by many apps"),
                ("Microsoft.NET.Native", "❌ DO NOT REMOVE - .NET runtime dependency"),
                ("Microsoft.Windows.SecHealthUI", "❌ CANNOT BE REMOVED - Windows Security"),
                ("Microsoft.WindowsCalculator", "✅ Can usually be removed"),
                ("Microsoft.BingWeather", "✅ Can usually be removed"),
                ("Microsoft.ZuneMusic", "✅ Can usually be removed"),
                ("Microsoft.ZuneVideo", "✅ Can usually be removed")
            };

            foreach (var (name, recommendation) in protectedApps)
            {
                if (appBaseName.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"🎯 Identified App Type: {name}");
                    Console.WriteLine($"   {recommendation}");
                    
                    if (recommendation.StartsWith("❌"))
                    {
                        Console.WriteLine("\n💡 ALTERNATIVE SOLUTIONS:");
                        Console.WriteLine("   • Disable the app instead of removing it");
                        Console.WriteLine("   • Use Group Policy to hide it from users");
                        Console.WriteLine("   • Use Windows Settings > Apps to try removal");
                        Console.WriteLine("   • Create a custom Windows image without these apps");
                    }
                    return;
                }
            }

            // Generic recommendations
            Console.WriteLine("💡 GENERAL SOLUTIONS TO TRY:");
            Console.WriteLine("");
            Console.WriteLine("1️⃣ Try All-Users Removal:");
            Console.WriteLine($"   Remove-AppxPackage -Package '{packageName}' -AllUsers");
            Console.WriteLine("");
            Console.WriteLine("2️⃣ Remove Provisioned Package (if applicable):");
            Console.WriteLine($"   Get-AppxProvisionedPackage -Online | Where {{$_.PackageName -like '*{appBaseName}*'}} | Remove-AppxProvisionedPackage -Online");
            Console.WriteLine("");
            Console.WriteLine("3️⃣ Use DISM (for stubborn system apps):");
            Console.WriteLine($"   DISM /Online /Remove-ProvisionedAppxPackage /PackageName:{packageName}");
            Console.WriteLine("");
            Console.WriteLine("4️⃣ Group Policy (Enterprise):");
            Console.WriteLine("   Computer Configuration > Administrative Templates > Windows Components > App Package Deployment");
            Console.WriteLine("");
            Console.WriteLine("⚠️  WARNING: Some apps are intentionally protected by Microsoft");
            Console.WriteLine("    Removing critical system components can break Windows functionality!");
        }

        private static async Task ExecutePowerShellCommand(string command, string title, bool showErrors = false)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"{command.Replace("\"", "\\\"")}\"",
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
                            Console.WriteLine(output.Trim());
                        }

                        if (showErrors && !string.IsNullOrWhiteSpace(error))
                        {
                            Console.WriteLine($"❌ Error: {error.Trim()}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to execute {title}: {ex.Message}");
            }
        }

        public static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: AppDeletionAnalyzer.exe <PackageFullName>");
                Console.WriteLine("Example: AppDeletionAnalyzer.exe Microsoft.BingWeather_8wekyb3d8bbwe");
                return;
            }

            await AnalyzeApp(args[0]);
            
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("Analysis complete! Use the recommendations above to resolve deletion issues.");
        }
    }
} 