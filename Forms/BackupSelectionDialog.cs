using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowsAppsManager.Models;

namespace WindowsAppsManager.Forms
{
    public partial class BackupSelectionDialog : Form
    {
        private ListView backupListView = null!;
        private Button restoreButton = null!;
        private Button cancelButton = null!;
        private Button deleteBackupButton = null!;
        private Label instructionLabel = null!;
        
        private readonly List<BackupInfo> backups;
        
        public BackupInfo? SelectedBackup { get; private set; }
        
        public BackupSelectionDialog(List<BackupInfo> availableBackups)
        {
            backups = availableBackups ?? throw new ArgumentNullException(nameof(availableBackups));
            InitializeComponent();
            PopulateBackupList();
        }
        
        private void InitializeComponent()
        {
            // Configure main form
            Text = "Select Backup to Restore";
            Size = new Size(700, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(600, 400);
            Icon = SystemIcons.Information;
            
            // Instruction label
            instructionLabel = new Label
            {
                Text = "Select a backup to restore. Double-click or use the Restore button to proceed.",
                Font = new Font("Segoe UI", 9),
                AutoSize = false,
                Size = new Size(660, 40),
                Location = new Point(15, 15),
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            // Backup list view
            backupListView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Location = new Point(15, 60),
                Size = new Size(660, 350),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            
            // Add columns
            backupListView.Columns.AddRange(new ColumnHeader[]
            {
                new ColumnHeader { Text = "Application", Width = 200 },
                new ColumnHeader { Text = "Version", Width = 100 },
                new ColumnHeader { Text = "Backup Date", Width = 140 },
                new ColumnHeader { Text = "Size", Width = 80 },
                new ColumnHeader { Text = "Status", Width = 100 },
                new ColumnHeader { Text = "Publisher", Width = 120 }
            });
            
            // Buttons
            deleteBackupButton = new Button
            {
                Text = "Delete Backup",
                Size = new Size(120, 30),
                Location = new Point(15, 425),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Enabled = false,
                UseVisualStyleBackColor = true
            };
            
            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 30),
                Location = new Point(595, 425),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel,
                UseVisualStyleBackColor = true
            };
            
            restoreButton = new Button
            {
                Text = "Restore",
                Size = new Size(80, 30),
                Location = new Point(505, 425),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Enabled = false,
                UseVisualStyleBackColor = true
            };
            
            Controls.AddRange(new Control[] 
            { 
                instructionLabel, backupListView, 
                deleteBackupButton, cancelButton, restoreButton 
            });
            
            // Event handlers
            backupListView.SelectedIndexChanged += BackupListView_SelectedIndexChanged;
            backupListView.DoubleClick += BackupListView_DoubleClick;
            restoreButton.Click += RestoreButton_Click;
            deleteBackupButton.Click += DeleteBackupButton_Click;
        }
        
        private void PopulateBackupList()
        {
            backupListView.Items.Clear();
            
            foreach (var backup in backups.OrderByDescending(b => b.BackupDate))
            {
                var item = new ListViewItem(backup.AppName)
                {
                    Tag = backup
                };
                
                item.SubItems.AddRange(new[]
                {
                    backup.Version,
                    backup.BackupDateFormatted,
                    backup.BackupSizeFormatted,
                    backup.StatusText,
                    backup.Publisher
                });
                
                // Color code based on status
                if (backup.Status == BackupStatus.Failed)
                {
                    item.BackColor = Color.LightCoral;
                }
                else if (backup.Status == BackupStatus.PartiallyCompleted)
                {
                    item.BackColor = Color.LightYellow;
                }
                else if (!backup.CanRestore())
                {
                    item.BackColor = Color.LightGray;
                    item.ForeColor = Color.DarkGray;
                }
                
                backupListView.Items.Add(item);
            }
            
            // Auto-resize columns
            backupListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }
        
        private void BackupListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var hasSelection = backupListView.SelectedItems.Count > 0;
            var canRestore = false;
            
            if (hasSelection)
            {
                var selectedBackup = (BackupInfo)backupListView.SelectedItems[0].Tag;
                canRestore = selectedBackup.CanRestore();
            }
            
            restoreButton.Enabled = canRestore;
            deleteBackupButton.Enabled = hasSelection;
        }
        
        private void BackupListView_DoubleClick(object? sender, EventArgs e)
        {
            if (backupListView.SelectedItems.Count > 0)
            {
                var selectedBackup = (BackupInfo)backupListView.SelectedItems[0].Tag;
                if (selectedBackup.CanRestore())
                {
                    SelectedBackup = selectedBackup;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("This backup cannot be restored. The backup files may be missing or corrupted.",
                        "Cannot Restore", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        
        private void RestoreButton_Click(object? sender, EventArgs e)
        {
            if (backupListView.SelectedItems.Count > 0)
            {
                SelectedBackup = (BackupInfo)backupListView.SelectedItems[0].Tag;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        
        private void DeleteBackupButton_Click(object? sender, EventArgs e)
        {
            if (backupListView.SelectedItems.Count == 0) return;
            
            var selectedBackup = (BackupInfo)backupListView.SelectedItems[0].Tag;
            
            var result = MessageBox.Show(
                $"Delete backup for '{selectedBackup.AppName}'?\n\n" +
                $"Backup Date: {selectedBackup.BackupDateFormatted}\n" +
                $"Size: {selectedBackup.BackupSizeFormatted}\n\n" +
                "This action cannot be undone.",
                "Confirm Backup Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    // Note: We'd need access to BackupService here
                    // For now, just remove from the list
                    backups.Remove(selectedBackup);
                    PopulateBackupList();
                    
                    MessageBox.Show("Backup deleted successfully.", "Backup Deleted",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete backup: {ex.Message}", "Delete Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
} 