using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsAppsManager.Models;
using WindowsAppsManager.Services;
using WindowsAppsManager.Utils;

namespace WindowsAppsManager.Forms
{

public partial class MainForm : Form
{
         private DataGridView appsGrid = null!;
     private TextBox searchBox = null!;
     private ComboBox sortComboBox = null!;
     private Button refreshButton = null!;
     private Button deleteButton = null!;
     private Button backupButton = null!;
     private StatusStrip statusStrip = null!;
     private ToolStripStatusLabel statusLabel = null!;
     private ToolStripProgressBar progressBar = null!;
     private MenuStrip menuStrip = null!;
     private ToolStrip toolStrip = null!;
    
    private List<WindowsApp> allApps;
    private List<WindowsApp> filteredApps;
    private readonly AppService appService;
    private readonly BackupService backupService;
    private readonly DeletionService deletionService;
    
    public MainForm()
    {
        appService = new AppService();
        backupService = new BackupService();
        deletionService = new DeletionService(backupService);
        allApps = new List<WindowsApp>();
        filteredApps = new List<WindowsApp>();
        
        InitializeComponent();
        SetupEventHandlers();
        LoadAppsAsync();
    }
    
    private void InitializeComponent()
    {
        SuspendLayout();
        
        // Configure main form
        Text = "WindowsApps Manager";
        Size = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(800, 600);
        Icon = SystemIcons.Application;
        
        // Create menu strip
        CreateMenuStrip();
        
        // Create tool strip
        CreateToolStrip();
        
        // Create main content area
        CreateMainContent();
        
        // Create status strip
        CreateStatusStrip();
        
        ResumeLayout();
    }
    
    private void CreateMenuStrip()
    {
        menuStrip = new MenuStrip();
        
        // File menu
        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add("&Refresh", null, (s, e) => LoadAppsAsync());
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("E&xit", null, (s, e) => Close());
        
        // Tools menu
        var toolsMenu = new ToolStripMenuItem("&Tools");
        toolsMenu.DropDownItems.Add("&Backup All Apps", null, (s, e) => BackupAllApps());
        toolsMenu.DropDownItems.Add("&Restore from Backup", null, (s, e) => RestoreFromBackup());
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add("&Test Backup Access", null, (s, e) => TestBackupAccess());
        toolsMenu.DropDownItems.Add("Test &Single Backup", null, (s, e) => TestSingleBackup());
        toolsMenu.DropDownItems.Add("Test &Isolated Backup", null, (s, e) => TestIsolatedBackup());
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add("&Settings", null, (s, e) => ShowSettings());
        
        // Help menu
        var helpMenu = new ToolStripMenuItem("&Help");
        helpMenu.DropDownItems.Add("&About", null, (s, e) => ShowAbout());
        
        menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, toolsMenu, helpMenu });
        
        MainMenuStrip = menuStrip;
        Controls.Add(menuStrip);
    }
    
    private void CreateToolStrip()
    {
        toolStrip = new ToolStrip();
        toolStrip.ImageScalingSize = new Size(24, 24);
        
        // Refresh button
        refreshButton = new Button 
        { 
            Text = "Refresh", 
            Size = new Size(80, 25),
            UseVisualStyleBackColor = true
        };
        var refreshHost = new ToolStripControlHost(refreshButton);
        
        // Search box
        var searchLabel = new ToolStripLabel("Search:");
        searchBox = new TextBox 
        { 
            Size = new Size(200, 25),
            PlaceholderText = "Search apps..."
        };
        var searchHost = new ToolStripControlHost(searchBox);
        
        // Sort combo box
        var sortLabel = new ToolStripLabel("Sort by:");
        sortComboBox = new ComboBox 
        { 
            Size = new Size(120, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        sortComboBox.Items.AddRange(new[] { "Name", "Size", "Install Date", "Publisher", "Status" });
        sortComboBox.SelectedIndex = 0;
        var sortHost = new ToolStripControlHost(sortComboBox);
        
        // Action buttons
        deleteButton = new Button 
        { 
            Text = "Delete Selected", 
            Size = new Size(120, 25),
            UseVisualStyleBackColor = true,
            Enabled = false
        };
        var deleteHost = new ToolStripControlHost(deleteButton);
        
        backupButton = new Button 
        { 
            Text = "Backup Selected", 
            Size = new Size(120, 25),
            UseVisualStyleBackColor = true,
            Enabled = false
        };
        var backupHost = new ToolStripControlHost(backupButton);
        
        toolStrip.Items.AddRange(new ToolStripItem[] 
        { 
            refreshHost,
            new ToolStripSeparator(),
            searchLabel,
            searchHost,
            new ToolStripSeparator(),
            sortLabel,
            sortHost,
            new ToolStripSeparator(),
            deleteHost,
            backupHost
        });
        
        Controls.Add(toolStrip);
    }
    
    private void CreateMainContent()
    {
                 // Create and configure DataGridView
         appsGrid = new DataGridView
         {
             Dock = DockStyle.Fill,
             AllowUserToAddRows = false,
             AllowUserToDeleteRows = false,
             ReadOnly = false,
             SelectionMode = DataGridViewSelectionMode.FullRowSelect,
             MultiSelect = true,
             AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
             RowHeadersVisible = false,
             BackgroundColor = Color.White,
             BorderStyle = BorderStyle.None
         };
         
         // Add columns
         appsGrid.Columns.AddRange(new DataGridViewColumn[]
         {
             new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "", Width = 40, Frozen = true, ReadOnly = false },
             new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Application Name", Width = 200, ReadOnly = true },
             new DataGridViewTextBoxColumn { Name = "Publisher", HeaderText = "Publisher", Width = 150, ReadOnly = true },
             new DataGridViewTextBoxColumn { Name = "Version", HeaderText = "Version", Width = 100, ReadOnly = true },
             new DataGridViewTextBoxColumn { Name = "Size", HeaderText = "Size", Width = 80, ReadOnly = true },
             new DataGridViewTextBoxColumn { Name = "InstallDate", HeaderText = "Install Date", Width = 100, ReadOnly = true },
             new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 80, ReadOnly = true },
             new DataGridViewTextBoxColumn { Name = "FileCount", HeaderText = "Files", Width = 70, ReadOnly = true },
             new DataGridViewTextBoxColumn { Name = "FolderPath", HeaderText = "Path", Width = 300, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true }
         });
        
                 // Style the grid
         appsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.LightGray;
         appsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
         appsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
         
         // Create context menu for right-click actions
         var contextMenu = new ContextMenuStrip();
         contextMenu.Items.Add("View Details", null, (s, e) => ShowAppDetails());
         contextMenu.Items.Add("Open Folder", null, (s, e) => OpenAppFolder());
         contextMenu.Items.Add("-"); // Separator
         contextMenu.Items.Add("Select All", null, (s, e) => SelectAllApps());
         contextMenu.Items.Add("Deselect All", null, (s, e) => DeselectAllApps());
         appsGrid.ContextMenuStrip = contextMenu;
         
         Controls.Add(appsGrid);
    }
    
    private void CreateStatusStrip()
    {
        statusStrip = new StatusStrip();
        
        statusLabel = new ToolStripStatusLabel("Ready")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        
        progressBar = new ToolStripProgressBar
        {
            Visible = false,
            Size = new Size(200, 16)
        };
        
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, progressBar });
        Controls.Add(statusStrip);
    }
    
         private void SetupEventHandlers()
     {
         refreshButton.Click += (s, e) => LoadAppsAsync();
         deleteButton.Click += (s, e) => DeleteSelectedApps();
         backupButton.Click += (s, e) => BackupSelectedApps();
         searchBox.TextChanged += (s, e) => FilterApps();
         sortComboBox.SelectedIndexChanged += (s, e) => SortApps();
         appsGrid.SelectionChanged += AppsGrid_SelectionChanged;
         appsGrid.CellValueChanged += AppsGrid_CellValueChanged;
         appsGrid.CurrentCellDirtyStateChanged += AppsGrid_CurrentCellDirtyStateChanged;
         appsGrid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ShowAppDetails(); };
     }
    
         private async void LoadAppsAsync()
     {
         try
         {
             statusLabel.Text = "Loading applications...";
             progressBar.Visible = true;
             refreshButton.Enabled = false;
             
             // Check permissions first
             if (!PermissionHelper.IsRunningAsAdministrator())
             {
                 MessageBox.Show("Administrator privileges required to access WindowsApps folder.", 
                     "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                 return;
             }
             
             if (!PermissionHelper.CanAccessWindowsAppsFolder())
             {
                 MessageBox.Show("Cannot access WindowsApps folder. Please ensure you have administrator privileges and the folder exists.", 
                     "Access Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 return;
             }
             
             statusLabel.Text = "Scanning WindowsApps folder...";
             allApps = await Task.Run(() => appService.GetInstalledApps());
             filteredApps = new List<WindowsApp>(allApps);
             
             SortApps();
             UpdateGrid();
             statusLabel.Text = $"Loaded {allApps.Count} applications";
         }
         catch (UnauthorizedAccessException ex)
         {
             MessageBox.Show($"Access denied to WindowsApps folder:\n{ex.Message}\n\nPlease run as administrator.", 
                 "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
             statusLabel.Text = "Access denied - run as administrator";
         }
         catch (DirectoryNotFoundException ex)
         {
             MessageBox.Show($"WindowsApps folder not found:\n{ex.Message}", 
                 "Folder Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
             statusLabel.Text = "WindowsApps folder not found";
         }
         catch (Exception ex)
         {
             MessageBox.Show($"Error loading applications:\n{ex.Message}\n\nDetails: {ex.StackTrace}", 
                 "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
             statusLabel.Text = "Error loading applications";
         }
         finally
         {
             progressBar.Visible = false;
             refreshButton.Enabled = true;
         }
     }
    
    private void UpdateGrid()
    {
        appsGrid.Rows.Clear();
        
        foreach (var app in filteredApps)
        {
            var row = new DataGridViewRow();
            row.CreateCells(appsGrid);
            row.Cells[0].Value = false; // Selected checkbox
            row.Cells[1].Value = app.GetDisplayName();
            row.Cells[2].Value = app.Publisher;
            row.Cells[3].Value = app.Version;
            row.Cells[4].Value = app.SizeFormatted;
            row.Cells[5].Value = app.InstallDateFormatted;
            row.Cells[6].Value = app.StatusText;
            row.Cells[7].Value = app.FileCount;
            row.Cells[8].Value = app.FolderPath;
            row.Tag = app;
            
            // Color code based on status
            if (app.IsProtected)
                row.DefaultCellStyle.BackColor = Color.LightCoral;
            else if (app.IsSystemApp)
                row.DefaultCellStyle.BackColor = Color.LightYellow;
            
            appsGrid.Rows.Add(row);
        }
    }
    
    private void FilterApps()
    {
        var searchTerm = searchBox.Text.ToLower();
        
        filteredApps = string.IsNullOrEmpty(searchTerm) 
            ? new List<WindowsApp>(allApps)
            : allApps.Where(app => 
                app.GetDisplayName().ToLower().Contains(searchTerm) ||
                app.Publisher.ToLower().Contains(searchTerm) ||
                app.PackageName.ToLower().Contains(searchTerm)).ToList();
        
        SortApps();
        UpdateGrid();
    }
    
    private void SortApps()
    {
        var sortBy = sortComboBox.SelectedItem?.ToString() ?? "Name";
        
        filteredApps = sortBy switch
        {
            "Name" => filteredApps.OrderBy(app => app.GetDisplayName()).ToList(),
            "Size" => filteredApps.OrderByDescending(app => app.SizeInBytes).ToList(),
            "Install Date" => filteredApps.OrderByDescending(app => app.InstallDate).ToList(),
            "Publisher" => filteredApps.OrderBy(app => app.Publisher).ToList(),
            "Status" => filteredApps.OrderBy(app => app.StatusText).ToList(),
            _ => filteredApps.OrderBy(app => app.GetDisplayName()).ToList()
        };
    }
    
         private void AppsGrid_SelectionChanged(object? sender, EventArgs e)
     {
         UpdateSelectionStatus();
     }
     
     private void AppsGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
     {
         if (e.ColumnIndex == 0) // Selected column
         {
             UpdateSelectionStatus();
         }
     }
     
     private void AppsGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
     {
         if (appsGrid.IsCurrentCellDirty && appsGrid.CurrentCell.ColumnIndex == 0)
         {
             appsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
         }
     }
     
     private void UpdateSelectionStatus()
     {
         var selectedCount = GetSelectedApps().Count;
         deleteButton.Enabled = selectedCount > 0;
         backupButton.Enabled = selectedCount > 0;
         
         statusLabel.Text = selectedCount > 0 
             ? $"{selectedCount} app(s) selected" 
             : $"{filteredApps.Count} applications displayed";
     }
    
    private List<WindowsApp> GetSelectedApps()
    {
        var selectedApps = new List<WindowsApp>();
        
        foreach (DataGridViewRow row in appsGrid.Rows)
        {
            if (row.Cells[0].Value is true && row.Tag is WindowsApp app)
            {
                selectedApps.Add(app);
            }
        }
        
        return selectedApps;
    }
    
    private async void BackupSelectedApps()
    {
        var selectedApps = GetSelectedApps();
        if (selectedApps.Count == 0)
        {
            MessageBox.Show("Please select applications to backup.", "No Selection", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        var result = MessageBox.Show(
            $"Create backup for {selectedApps.Count} selected application(s)?\n\n" +
            $"Backups will be stored in:\n{backupService.GetBackupDirectory()}",
            "Confirm Backup",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        
        if (result != DialogResult.Yes) return;
        
        await PerformBackupAsync(selectedApps);
    }
    
    private async Task PerformBackupAsync(List<WindowsApp> apps)
    {
        var progressForm = new ProgressForm("Creating Backups", "Preparing backup process...");
        progressForm.Show(this);
        
        IProgress<string> progress = new Progress<string>(message => 
        {
            try
            {
                progressForm.UpdateStatus(message);
                statusLabel.Text = message;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in backup progress callback: {ex.Message}");
                // Don't re-throw to avoid cascading exceptions
            }
        });
        
        progressBar.Visible = true;
        progressBar.Style = ProgressBarStyle.Marquee;
        
        try
        {
            var successCount = 0;
            var failCount = 0;
            
            foreach (var app in apps)
            {
                try
                {
                    progress?.Report($"Backing up {app.GetDisplayName()}...");
                    
                    // Wrap the backup call in additional exception handling
                    try
                    {
                        await backupService.CreateBackupAsync(app, progress);
                        successCount++;
                        progress?.Report($"✅ Successfully backed up {app.GetDisplayName()}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        throw new InvalidOperationException($"Access denied: {ex.Message}. Try running as administrator.", ex);
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        throw new InvalidOperationException($"App directory not found: {ex.Message}", ex);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Backup failed: {ex.Message}", ex);
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    var errorMsg = $"❌ Failed to backup {app.GetDisplayName()}: {ex.Message}";
                    
                    try
                    {
                        progress?.Report(errorMsg);
                    }
                    catch (Exception progressEx)
                    {
                        Console.WriteLine($"Error reporting progress: {progressEx.Message}");
                    }
                    
                    // Log the full exception details for debugging
                    Console.WriteLine($"Backup error details: {ex}");
                    
                    await Task.Delay(2000); // Give user time to read the error
                }
            }
            
            progressForm.Close();
            
            var message = $"Backup completed!\n\n" +
                         $"Successfully backed up: {successCount} apps\n";
            
            if (failCount > 0)
            {
                message += $"Failed to backup: {failCount} apps\n";
            }
            
            message += $"\nBackups are stored in:\n{backupService.GetBackupDirectory()}";
            
            MessageBox.Show(message, "Backup Complete", 
                MessageBoxButtons.OK, 
                failCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            
            statusLabel.Text = $"Backup completed: {successCount} successful, {failCount} failed";
        }
        catch (Exception ex)
        {
            progressForm.Close();
            MessageBox.Show($"Backup process failed: {ex.Message}", "Backup Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "Backup failed";
        }
        finally
        {
            progressBar.Visible = false;
            progressBar.Style = ProgressBarStyle.Blocks;
        }
    }
    
    private void DeleteSelectedApps()
    {
        var selectedApps = GetSelectedApps();
        if (selectedApps.Count == 0)
        {
            MessageBox.Show("Please select applications to delete.", "No Selection", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        // Show comprehensive confirmation dialog
        using var confirmDialog = new ConfirmationDialog(selectedApps);
        if (confirmDialog.ShowDialog(this) != DialogResult.OK || !confirmDialog.ConfirmedDeletion)
        {
            return;
        }
        
        // Check if backup should be created
        if (confirmDialog.CreateBackup)
        {
            MessageBox.Show("Backup will be created before deletion proceeds.\n\n" +
                           "Please wait while the backup process completes.",
                           "Creating Backup First",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Information);
            
            PerformBackupAndDeleteAsync(selectedApps);
        }
        else
        {
            // Final warning for no backup
            var finalConfirm = MessageBox.Show(
                "⚠️ FINAL WARNING ⚠️\n\n" +
                "You chose NOT to create a backup.\n" +
                "Deleted applications CANNOT be recovered!\n\n" +
                "Are you absolutely sure you want to proceed?",
                "No Backup - Final Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            
            if (finalConfirm == DialogResult.Yes)
            {
                PerformDeletionAsync(selectedApps);
            }
        }
    }
    
    private async Task PerformBackupAndDeleteAsync(List<WindowsApp> apps)
    {
        try
        {
            // First create backups
            await PerformBackupAsync(apps);
            
            // Then ask if user wants to proceed with deletion
            var proceedWithDeletion = MessageBox.Show(
                "Backup completed successfully.\n\n" +
                "Do you want to proceed with deleting the applications now?",
                "Proceed with Deletion?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            
            if (proceedWithDeletion == DialogResult.Yes)
            {
                await PerformDeletionAsync(apps);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Backup failed: {ex.Message}\n\nDeletion cancelled for safety.", 
                "Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    private async Task PerformDeletionAsync(List<WindowsApp> apps)
    {
        var progressForm = new ProgressForm("Deleting Applications", "Preparing for deletion...");
        progressForm.Show(this);
        
        IProgress<string> progress = new Progress<string>(message => 
        {
            progressForm.UpdateStatus(message);
            statusLabel.Text = message;
        });
        
        var successCount = 0;
        var failureCount = 0;
        var deletionResults = new List<(WindowsApp app, bool success, string error)>();
        
        try
        {
            foreach (var app in apps)
            {
                try
                {
                    progress?.Report($"Deleting {app.GetDisplayName()} ({successCount + failureCount + 1}/{apps.Count})...");
                    
                    // Find the associated backup if it was created
                    BackupInfo? backup = null;
                    var allBackups = backupService.GetAllBackups();
                    backup = allBackups.FirstOrDefault(b => b.PackageName == app.PackageName);
                    
                    // Perform the deletion
                    var success = await deletionService.DeleteAppAsync(app, backup, progress);
                    
                    if (success)
                    {
                        successCount++;
                        deletionResults.Add((app, true, ""));
                        
                        // Verify deletion
                        if (deletionService.VerifyDeletion(app))
                        {
                            progress?.Report($"Successfully deleted and verified: {app.GetDisplayName()}");
                        }
                        else
                        {
                            progress?.Report($"Warning: Deletion incomplete for {app.GetDisplayName()}");
                        }
                    }
                    else
                    {
                        failureCount++;
                        deletionResults.Add((app, false, "Deletion failed"));
                        progress?.Report($"Failed to delete: {app.GetDisplayName()}");
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    deletionResults.Add((app, false, ex.Message));
                    progress?.Report($"Error deleting {app.GetDisplayName()}: {ex.Message}");
                }
                
                // Small delay between deletions to prevent system overload
                await Task.Delay(500);
            }
            
            progressForm.Close();
            
            // Show completion summary
            var summaryMessage = $"Deletion Complete!\n\n";
            summaryMessage += $"Successfully deleted: {successCount} apps\n";
            
            if (failureCount > 0)
            {
                summaryMessage += $"Failed to delete: {failureCount} apps\n\n";
                summaryMessage += "Failed applications:\n";
                
                foreach (var result in deletionResults.Where(r => !r.success))
                {
                    summaryMessage += $"• {result.app.GetDisplayName()}: {result.error}\n";
                }
                
                summaryMessage += "\nCheck if these apps are currently running or require special permissions.";
            }
            
            var icon = failureCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
            MessageBox.Show(summaryMessage, "Deletion Results", MessageBoxButtons.OK, icon);
            
            // Refresh the app list to remove successfully deleted apps
            LoadAppsAsync();
            
            statusLabel.Text = $"Deletion complete: {successCount} successful, {failureCount} failed";
        }
        catch (Exception ex)
        {
            progressForm.Close();
            MessageBox.Show($"Critical error during deletion process: {ex.Message}", 
                "Deletion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            
            statusLabel.Text = "Deletion process failed";
        }
    }
    
    private void BackupAllApps()
    {
        if (allApps.Count == 0)
        {
            MessageBox.Show("No applications available to backup.", "No Applications", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        var result = MessageBox.Show(
            $"Create backup for ALL {allApps.Count} applications?\n\n" +
            $"This may take a significant amount of time and disk space.\n" +
            $"Backups will be stored in:\n{backupService.GetBackupDirectory()}",
            "Backup All Applications",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        
        if (result == DialogResult.Yes)
        {
            Task.Run(async () => await PerformBackupAsync(allApps));
        }
    }
    
    private void RestoreFromBackup()
    {
        var backups = backupService.GetAllBackups();
        if (backups.Count == 0)
        {
            MessageBox.Show("No backups available for restoration.", "No Backups", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        // Show backup selection dialog
        using var backupDialog = new BackupSelectionDialog(backups);
        if (backupDialog.ShowDialog(this) == DialogResult.OK && backupDialog.SelectedBackup != null)
        {
            PerformRestoreAsync(backupDialog.SelectedBackup);
        }
    }
    
    private async void PerformRestoreAsync(BackupInfo backup)
    {
        var result = MessageBox.Show(
            $"Restore application from backup?\n\n" +
            $"App: {backup.AppName}\n" +
            $"Backup Date: {backup.BackupDateFormatted}\n" +
            $"Size: {backup.BackupSizeFormatted}\n\n" +
            $"This will restore the application to its original location.",
            "Confirm Restore",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        
        if (result != DialogResult.Yes) return;
        
        var progressForm = new ProgressForm("Restoring Application", $"Preparing to restore {backup.AppName}...");
        progressForm.Show(this);
        
        IProgress<string> progress = new Progress<string>(message => 
        {
            progressForm.UpdateStatus(message);
            statusLabel.Text = message;
        });
        
        try
        {
            var success = await backupService.RestoreFromBackupAsync(backup, progress);
            progressForm.Close();
            
            if (success)
            {
                MessageBox.Show($"Application '{backup.AppName}' restored successfully!", 
                    "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Refresh the app list to show restored app
                LoadAppsAsync();
            }
            else
            {
                MessageBox.Show($"Failed to restore application '{backup.AppName}'.", 
                    "Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            progressForm.Close();
            MessageBox.Show($"Restore failed: {ex.Message}", "Restore Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    private void TestBackupAccess()
    {
        if (appsGrid.CurrentRow?.Tag is WindowsApp app)
        {
            var diagnostics = backupService.TestBackupAccess(app);
            MessageBox.Show(diagnostics, $"Backup Diagnostics - {app.GetDisplayName()}", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("Please select an application first.", "No Selection", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    
    private async void TestSingleBackup()
    {
        if (appsGrid.CurrentRow?.Tag is WindowsApp app)
        {
            var progressForm = new ProgressForm("Testing Backup", $"Testing backup for {app.GetDisplayName()}...");
            progressForm.Show(this);
            
            IProgress<string> progress = new Progress<string>(message => 
            {
                try
                {
                    progressForm.UpdateStatus(message);
                    Console.WriteLine($"Backup progress: {message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Progress error: {ex.Message}");
                }
            });
            
            try
            {
                progress?.Report("Starting test backup...");
                var backup = await backupService.CreateBackupAsync(app, progress);
                progressForm.Close();
                
                MessageBox.Show($"Test backup completed successfully!\n\nBackup path: {backup.BackupPath}\nSize: {backup.BackupSizeFormatted}", 
                    "Test Backup Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                progressForm.Close();
                MessageBox.Show($"Test backup failed:\n\n{ex.Message}\n\nFull details:\n{ex}", 
                    "Test Backup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            MessageBox.Show("Please select an application first.", "No Selection", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    
    private async void TestIsolatedBackup()
    {
        if (appsGrid.CurrentRow?.Tag is WindowsApp app)
        {
            var result = MessageBox.Show($"Test isolated backup for {app.GetDisplayName()}?\n\nThis will run without UI updates to isolate any threading issues.", 
                "Isolated Backup Test", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes) return;
            
            try
            {
                statusLabel.Text = "Running isolated backup test...";
                
                // Run backup completely isolated from UI
                await Task.Run(async () =>
                {
                    try
                    {
                        Console.WriteLine($"Starting isolated backup test for: {app.GetDisplayName()}");
                        
                        // Create a simple console-only progress reporter
                        IProgress<string> consoleProgress = new Progress<string>(message =>
                        {
                            Console.WriteLine($"Backup: {message}");
                        });
                        
                        var backup = await backupService.CreateBackupAsync(app, consoleProgress);
                        
                        Console.WriteLine($"Isolated backup completed successfully!");
                        Console.WriteLine($"Backup path: {backup.BackupPath}");
                        Console.WriteLine($"Files backed up: {backup.BackedUpFiles.Count}");
                        Console.WriteLine($"Size: {backup.BackupSizeFormatted}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Isolated backup failed: {ex}");
                        throw;
                    }
                });
                
                MessageBox.Show("Isolated backup test completed successfully!\nCheck console output for details.", 
                    "Test Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                statusLabel.Text = "Isolated backup test completed";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Isolated backup test failed:\n\n{ex.Message}\n\nCheck console for full details.", 
                    "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Isolated backup test failed";
                Console.WriteLine($"Full exception details: {ex}");
            }
        }
        else
        {
            MessageBox.Show("Please select an application first.", "No Selection", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    
    private void ShowSettings()
    {
        // This will be implemented in Phase 5
        MessageBox.Show("Settings will be implemented in Phase 5", "Not Implemented", 
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    
         private void ShowAbout()
     {
         MessageBox.Show(
             "WindowsApps Manager v1.0\n\n" +
             "A tool for managing Windows Store applications\n" +
             "and freeing up disk space.\n\n" +
             "Requires administrator privileges to access\n" +
             "the WindowsApps folder.",
             "About WindowsApps Manager",
             MessageBoxButtons.OK,
             MessageBoxIcon.Information);
     }
     
     private void ShowAppDetails()
     {
         if (appsGrid.CurrentRow?.Tag is WindowsApp app)
         {
             var details = $"Application Details:\n\n" +
                          $"Name: {app.GetDisplayName()}\n" +
                          $"Publisher: {app.Publisher}\n" +
                          $"Version: {app.Version}\n" +
                          $"Package Name: {app.PackageName}\n" +
                          $"Size: {app.SizeFormatted}\n" +
                          $"Files: {app.FileCount:N0}\n" +
                          $"Folders: {app.FolderCount:N0}\n" +
                          $"Install Date: {app.InstallDateFormatted}\n" +
                          $"Status: {app.StatusText}\n" +
                          $"Path: {app.FolderPath}\n\n" +
                          $"Safety Check: {(app.IsSafeToDelete() ? "Safe to delete" : "Use caution")}\n";
             
             var warning = app.GetDeletionWarning();
             if (!string.IsNullOrEmpty(warning))
             {
                 details += $"Warning: {warning}";
             }
             
             MessageBox.Show(details, $"Details - {app.GetDisplayName()}", 
                 MessageBoxButtons.OK, MessageBoxIcon.Information);
         }
     }
     
     private void OpenAppFolder()
     {
         if (appsGrid.CurrentRow?.Tag is WindowsApp app)
         {
             try
             {
                 System.Diagnostics.Process.Start("explorer.exe", app.FolderPath);
             }
             catch (Exception ex)
             {
                 MessageBox.Show($"Could not open folder:\n{ex.Message}", "Error", 
                     MessageBoxButtons.OK, MessageBoxIcon.Error);
             }
         }
     }
     
     private void SelectAllApps()
     {
         foreach (DataGridViewRow row in appsGrid.Rows)
         {
             row.Cells[0].Value = true;
         }
         UpdateSelectionStatus();
     }
     
     private void DeselectAllApps()
     {
         foreach (DataGridViewRow row in appsGrid.Rows)
         {
             row.Cells[0].Value = false;
         }
         UpdateSelectionStatus();
     }
 }
} 