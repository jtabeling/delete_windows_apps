using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using WindowsAppsManager.Models;
using WindowsAppsManager.Utils;

namespace WindowsAppsManager.Services
{

public class AppService
{
    private readonly string windowsAppsPath;
    
    public AppService()
    {
        windowsAppsPath = PermissionHelper.GetWindowsAppsPath();
    }
    
    /// <summary>
    /// Gets all installed Windows apps from the WindowsApps folder
    /// </summary>
    /// <returns>List of WindowsApp objects</returns>
    public List<WindowsApp> GetInstalledApps()
    {
        var apps = new List<WindowsApp>();
        
        try
        {
            if (!Directory.Exists(windowsAppsPath))
            {
                throw new DirectoryNotFoundException($"WindowsApps folder not found: {windowsAppsPath}");
            }
            
            var directories = Directory.GetDirectories(windowsAppsPath);
            
                         Console.WriteLine($"Found {directories.Length} directories in WindowsApps folder");
             
             foreach (var directory in directories)
             {
                 try
                 {
                     var app = ProcessAppDirectory(directory);
                     if (app != null)
                     {
                         apps.Add(app);
                         Console.WriteLine($"Successfully processed: {app.GetDisplayName()}");
                     }
                 }
                 catch (Exception ex)
                 {
                     // Log error but continue processing other apps
                     Console.WriteLine($"Error processing {directory}: {ex.Message}");
                 }
             }
             
             Console.WriteLine($"Successfully processed {apps.Count} apps");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error accessing WindowsApps folder: {ex.Message}", ex);
        }
        
        return apps.OrderBy(app => app.GetDisplayName()).ToList();
    }
    
    /// <summary>
    /// Processes a single app directory and extracts app information
    /// </summary>
    /// <param name="directoryPath">Path to the app directory</param>
    /// <returns>WindowsApp object or null if processing fails</returns>
    private WindowsApp? ProcessAppDirectory(string directoryPath)
    {
        try
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            var app = new WindowsApp
            {
                FolderPath = directoryPath,
                PackageName = dirInfo.Name
            };
            
            // Extract basic info from folder name
            ParsePackageName(app);
            
            // Get folder size and file counts
            CalculateFolderSize(app);
            
            // Get install date
            app.InstallDate = dirInfo.CreationTime;
            
            // Try to get detailed info from manifest
            TryGetManifestInfo(app);
            
            // Determine if system or protected app
            DetermineAppType(app);
            
            return app;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing directory {directoryPath}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Parses the package name to extract app information
    /// </summary>
    /// <param name="app">WindowsApp to update</param>
    private void ParsePackageName(WindowsApp app)
    {
        try
        {
            // Package names typically follow: PublisherName.AppName_Version_Architecture_PublisherId
            var parts = app.PackageName.Split('_');
            
            if (parts.Length >= 2)
            {
                // First part usually contains publisher and app name
                var namePart = parts[0];
                var dotIndex = namePart.IndexOf('.');
                
                if (dotIndex > 0)
                {
                    app.Publisher = namePart.Substring(0, dotIndex);
                    app.Name = namePart.Substring(dotIndex + 1);
                }
                else
                {
                    app.Name = namePart;
                }
                
                // Second part is usually version
                if (parts.Length > 1)
                {
                    app.Version = parts[1];
                }
            }
            else
            {
                app.Name = app.PackageName;
            }
        }
        catch
        {
            app.Name = app.PackageName;
        }
    }
    
    /// <summary>
    /// Calculates the total size of the app folder
    /// </summary>
    /// <param name="app">WindowsApp to update</param>
    private void CalculateFolderSize(WindowsApp app)
    {
        try
        {
            var dirInfo = new DirectoryInfo(app.FolderPath);
            
            // Get all files recursively
            var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            var directories = dirInfo.GetDirectories("*", SearchOption.AllDirectories);
            
            app.SizeInBytes = files.Sum(file => 
            {
                try
                {
                    return file.Length;
                }
                catch
                {
                    return 0;
                }
            });
            
            app.FileCount = files.Length;
            app.FolderCount = directories.Length;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating size for {app.FolderPath}: {ex.Message}");
            app.SizeInBytes = 0;
            app.FileCount = 0;
            app.FolderCount = 0;
        }
    }
    
         /// <summary>
     /// Tries to get detailed information from the app manifest
     /// </summary>
     /// <param name="app">WindowsApp to update</param>
     private void TryGetManifestInfo(WindowsApp app)
     {
         try
         {
             // Look for AppxManifest.xml file
             var manifestPath = Path.Combine(app.FolderPath, "AppxManifest.xml");
             
             if (File.Exists(manifestPath))
             {
                 // Parse XML manifest for detailed app information
                 var manifestContent = File.ReadAllText(manifestPath);
                 
                 // Try to extract display name from various possible locations
                 ExtractFromManifest(manifestContent, "DisplayName=\"", value => 
                 {
                     if (!string.IsNullOrEmpty(value) && !value.StartsWith("ms-resource:"))
                     {
                         app.Name = value;
                     }
                 });
                 
                 // Also try uap:DisplayName
                 ExtractFromManifest(manifestContent, "uap:DisplayName=\"", value => 
                 {
                     if (!string.IsNullOrEmpty(value) && !value.StartsWith("ms-resource:"))
                     {
                         app.Name = value;
                     }
                 });
                 
                 // Try to extract publisher display name
                 ExtractFromManifest(manifestContent, "PublisherDisplayName=\"", value => 
                 {
                     if (!string.IsNullOrEmpty(value) && !value.StartsWith("ms-resource:"))
                     {
                         app.Publisher = value;
                     }
                 });
                 
                 // Try to extract version from Identity element
                 ExtractFromManifest(manifestContent, "Version=\"", value => 
                 {
                     if (!string.IsNullOrEmpty(value))
                     {
                         app.Version = value;
                     }
                 });
                 
                 // Try to extract Publisher from Identity element if PublisherDisplayName wasn't found
                 if (string.IsNullOrEmpty(app.Publisher) || app.Publisher == app.PackageName.Split('_').FirstOrDefault())
                 {
                     ExtractFromManifest(manifestContent, "Publisher=\"", value => 
                     {
                         if (!string.IsNullOrEmpty(value))
                         {
                             // Clean up the publisher name (remove CN= prefix if present)
                             var cleanPublisher = value.Replace("CN=", "").Replace("O=", "").Split(',').FirstOrDefault()?.Trim();
                             if (!string.IsNullOrEmpty(cleanPublisher))
                             {
                                 app.Publisher = cleanPublisher;
                             }
                         }
                     });
                 }
             }
         }
         catch (Exception ex)
         {
             Console.WriteLine($"Error reading manifest for {app.PackageName}: {ex.Message}");
         }
     }
    
         /// <summary>
     /// Simple helper to extract values from manifest XML
     /// </summary>
     private void ExtractFromManifest(string manifestContent, string searchPattern, Action<string> setValue)
     {
         try
         {
             var startIndex = manifestContent.IndexOf(searchPattern);
             
             if (startIndex >= 0)
             {
                 startIndex += searchPattern.Length;
                 var endIndex = manifestContent.IndexOf("\"", startIndex);
                 
                 if (endIndex > startIndex)
                 {
                     var value = manifestContent.Substring(startIndex, endIndex - startIndex);
                     setValue(value);
                 }
             }
         }
         catch
         {
             // Ignore parsing errors
         }
     }
    
    /// <summary>
    /// Determines if the app is a system app or protected
    /// </summary>
    /// <param name="app">WindowsApp to update</param>
    private void DetermineAppType(WindowsApp app)
    {
        // Check if it's a Microsoft system app
        var microsoftPublishers = new[]
        {
            "Microsoft",
            "Microsoft Corporation",
            "CN=Microsoft Corporation"
        };
        
        app.IsSystemApp = microsoftPublishers.Any(pub => 
            app.Publisher.Contains(pub, StringComparison.OrdinalIgnoreCase)) ||
            app.PackageName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase);
        
        // Check if it's a protected system component
        var protectedPackages = new[]
        {
            "Microsoft.Windows.Cortana",
            "Microsoft.Windows.ShellExperienceHost",
            "Microsoft.Windows.StartMenuExperienceHost",
            "Microsoft.VCLibs",
            "Microsoft.NET.Native",
            "Microsoft.UI.Xaml"
        };
        
        app.IsProtected = protectedPackages.Any(pkg => 
            app.PackageName.StartsWith(pkg, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Gets detailed information about a specific app
    /// </summary>
    /// <param name="app">WindowsApp to get details for</param>
    /// <returns>Updated WindowsApp with additional details</returns>
    public WindowsApp GetAppDetails(WindowsApp app)
    {
        try
        {
            // Get dependencies by checking other apps that might reference this one
            app.Dependencies = GetAppDependencies(app.PackageName);
            
            return app;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting app details for {app.PackageName}: {ex.Message}");
            return app;
        }
    }
    
    /// <summary>
    /// Gets dependencies for a specific app
    /// </summary>
    /// <param name="packageName">Package name to check dependencies for</param>
    /// <returns>List of dependent package names</returns>
    private List<string> GetAppDependencies(string packageName)
    {
        var dependencies = new List<string>();
        
        try
        {
            // This is a simplified implementation
            // Full implementation would use PowerShell or WMI to query package dependencies
            // For now, return empty list
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting dependencies for {packageName}: {ex.Message}");
        }
        
                 return dependencies;
     }
 }
} 