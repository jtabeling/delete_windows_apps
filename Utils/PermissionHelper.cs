using System;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace WindowsAppsManager.Utils
{

public static class PermissionHelper
{
    private const string WindowsAppsPath = @"C:\Program Files\WindowsApps";

    /// <summary>
    /// Checks if the current process is running with administrator privileges
    /// </summary>
    /// <returns>True if running as administrator, false otherwise</returns>
    public static bool IsRunningAsAdministrator()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the WindowsApps folder can be accessed
    /// </summary>
    /// <returns>True if folder can be accessed, false otherwise</returns>
    public static bool CanAccessWindowsAppsFolder()
    {
        try
        {
            // Check if folder exists
            if (!Directory.Exists(WindowsAppsPath))
                return false;

            // Try to enumerate the directory (requires proper permissions)
            Directory.EnumerateDirectories(WindowsAppsPath).Take(1).ToList();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the WindowsApps folder path
    /// </summary>
    /// <returns>Path to WindowsApps folder</returns>
    public static string GetWindowsAppsPath()
    {
        return WindowsAppsPath;
    }

    /// <summary>
    /// Attempts to take ownership of a directory (requires administrator privileges)
    /// </summary>
    /// <param name="directoryPath">Path to the directory</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool TakeOwnership(string directoryPath)
    {
        try
        {
            // This is a placeholder for more complex ownership operations
            // In a full implementation, this would use Windows APIs to change ownership
            return Directory.Exists(directoryPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a specific directory can be deleted
    /// </summary>
    /// <param name="directoryPath">Path to check</param>
    /// <returns>True if can be deleted, false otherwise</returns>
    public static bool CanDeleteDirectory(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
                return false;

            // Check if we can get directory info and list contents
            DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
            dirInfo.GetFiles().Take(1).ToList();
            dirInfo.GetDirectories().Take(1).ToList();
            
            return true;
        }
        catch
        {
            return false;
                 }
     }
 }
} 