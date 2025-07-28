using System;
using System.Security.Principal;
using System.Windows.Forms;
using WindowsAppsManager.Forms;
using WindowsAppsManager.Utils;

namespace WindowsAppsManager
{

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Set up global exception handlers
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += Application_ThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
                 // Configure application defaults
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            // Check if running as administrator
            if (!PermissionHelper.IsRunningAsAdministrator())
            {
                MessageBox.Show(
                    "WindowsApps Manager requires administrator privileges to access the WindowsApps folder.\n\n" +
                    "Please run the application as an administrator.",
                    "Administrator Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Verify critical permissions
            if (!PermissionHelper.CanAccessWindowsAppsFolder())
            {
                MessageBox.Show(
                    "Unable to access the WindowsApps folder. This may be due to:\n\n" +
                    "• Insufficient permissions\n" +
                    "• Folder does not exist\n" +
                    "• System security restrictions\n\n" +
                    "Please ensure you're running as administrator and try again.",
                    "Folder Access Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            // Start the main application
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An unexpected error occurred during startup:\n\n{ex.Message}",
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
                 }
     }
     
     private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
     {
         MessageBox.Show(
             $"An unhandled error occurred:\n\n{e.Exception.Message}\n\nDetails:\n{e.Exception}",
             "Application Error",
             MessageBoxButtons.OK,
             MessageBoxIcon.Error);
     }
     
     private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
     {
         MessageBox.Show(
             $"A critical unhandled error occurred:\n\n{e.ExceptionObject}",
             "Critical Error",
             MessageBoxButtons.OK,
             MessageBoxIcon.Error);
     }
 }
} 