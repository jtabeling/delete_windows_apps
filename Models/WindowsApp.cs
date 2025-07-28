using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsAppsManager.Models
{

public class WindowsApp
{
    public string Name { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public DateTime InstallDate { get; set; }
    public int FileCount { get; set; }
    public int FolderCount { get; set; }
    public bool IsSystemApp { get; set; }
    public bool IsProtected { get; set; }
    public List<string> Dependencies { get; set; } = new List<string>();
    
    // Display properties
    public string SizeFormatted => FormatBytes(SizeInBytes);
    public string InstallDateFormatted => InstallDate.ToString("yyyy-MM-dd");
    public string StatusText => IsProtected ? "Protected" : IsSystemApp ? "System" : "User";
    
    /// <summary>
    /// Formats bytes into human-readable string
    /// </summary>
    /// <param name="bytes">Number of bytes</param>
    /// <returns>Formatted string (e.g., "1.5 GB")</returns>
    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;
        
        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }
        
        return $"{size:N2} {suffixes[suffixIndex]}";
    }
    
    /// <summary>
    /// Gets the app display name for UI
    /// </summary>
    /// <returns>Formatted display name</returns>
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(Name))
            return Name;
        
        // Fallback to package name if display name is not available
        if (!string.IsNullOrEmpty(PackageName))
        {
            // Try to extract a readable name from package name
            var parts = PackageName.Split('_');
            return parts.Length > 0 ? parts[0] : PackageName;
        }
        
        return "Unknown App";
    }
    
    /// <summary>
    /// Checks if this app can be safely deleted
    /// </summary>
    /// <returns>True if safe to delete, false otherwise</returns>
    public bool IsSafeToDelete()
    {
        // Protected apps should not be deleted
        if (IsProtected) return false;
        
        // Check for critical system components
        var criticalPackages = new[]
        {
            "Microsoft.Windows",
            "Microsoft.VCLibs",
            "Microsoft.NET.Native",
            "Microsoft.UI.Xaml"
        };
        
        return !criticalPackages.Any(critical => 
            PackageName.StartsWith(critical, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Gets warning message if app deletion might be risky
    /// </summary>
    /// <returns>Warning message or empty string if safe</returns>
    public string GetDeletionWarning()
    {
        if (IsProtected)
            return "This is a protected system application. Deletion may cause system instability.";
        
        if (IsSystemApp)
            return "This is a system application. Deletion may affect system functionality.";
        
        if (Dependencies.Count > 0)
            return $"This app has {Dependencies.Count} dependencies. Other apps may be affected.";
        
                 return string.Empty;
     }
 }
} 