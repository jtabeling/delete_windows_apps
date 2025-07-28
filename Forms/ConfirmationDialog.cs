using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowsAppsManager.Models;

namespace WindowsAppsManager.Forms
{
    public partial class ConfirmationDialog : Form
    {
        private readonly List<WindowsApp> appsToDelete;
        private CheckBox createBackupCheckBox = null!;
        private CheckBox confirmDeletionCheckBox = null!;
        private CheckBox understandRisksCheckBox = null!;
        private Button deleteButton = null!;
        private Button cancelButton = null!;
        private RichTextBox warningTextBox = null!;
        private Label totalSizeLabel = null!;
        private Panel warningPanel = null!;
        
        public bool CreateBackup => createBackupCheckBox.Checked;
        public bool ConfirmedDeletion { get; private set; }
        
        public ConfirmationDialog(List<WindowsApp> apps)
        {
            appsToDelete = apps ?? throw new ArgumentNullException(nameof(apps));
            InitializeComponent();
            SetupDialog();
        }
        
        private void InitializeComponent()
        {
            // Configure main form
            Text = "Confirm App Deletion";
            Size = new Size(600, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Icon = SystemIcons.Warning;
            
            // Create controls
            CreateControls();
            LayoutControls();
            SetupEventHandlers();
        }
        
        private void CreateControls()
        {
            // Warning panel
            warningPanel = new Panel
            {
                BackColor = Color.LightYellow,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };
            
            // Warning text
            warningTextBox = new RichTextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.LightYellow,
                Font = new Font("Segoe UI", 9),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            
            // Total size label
            totalSizeLabel = new Label
            {
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                AutoSize = true
            };
            
            // Checkboxes
            createBackupCheckBox = new CheckBox
            {
                Text = "Create backup before deletion (Recommended)",
                Checked = true,
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };
            
            confirmDeletionCheckBox = new CheckBox
            {
                Text = "I understand that this action cannot be undone without a backup",
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            
            understandRisksCheckBox = new CheckBox
            {
                Text = "I understand the risks and want to proceed",
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.DarkRed
            };
            
            // Buttons
            deleteButton = new Button
            {
                Text = "Delete Applications",
                Size = new Size(150, 35),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Enabled = false,
                UseVisualStyleBackColor = false
            };
            
            cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                DialogResult = DialogResult.Cancel,
                UseVisualStyleBackColor = true
            };
            
            Controls.AddRange(new Control[] 
            { 
                warningPanel, totalSizeLabel, createBackupCheckBox, 
                confirmDeletionCheckBox, understandRisksCheckBox,
                deleteButton, cancelButton 
            });
            
            warningPanel.Controls.Add(warningTextBox);
        }
        
        private void LayoutControls()
        {
            var margin = 15;
            var y = margin;
            
            // Warning panel
            warningPanel.Location = new Point(margin, y);
            warningPanel.Size = new Size(Width - 2 * margin, 180);
            
            warningTextBox.Dock = DockStyle.Fill;
            
            y += warningPanel.Height + margin;
            
            // Total size label
            totalSizeLabel.Location = new Point(margin, y);
            y += totalSizeLabel.Height + margin;
            
            // Checkboxes
            createBackupCheckBox.Location = new Point(margin, y);
            y += createBackupCheckBox.Height + 10;
            
            confirmDeletionCheckBox.Location = new Point(margin, y);
            y += confirmDeletionCheckBox.Height + 10;
            
            understandRisksCheckBox.Location = new Point(margin, y);
            y += understandRisksCheckBox.Height + margin + 10;
            
            // Buttons
            cancelButton.Location = new Point(Width - cancelButton.Width - margin, y);
            deleteButton.Location = new Point(cancelButton.Left - deleteButton.Width - 10, y);
        }
        
        private void SetupEventHandlers()
        {
            confirmDeletionCheckBox.CheckedChanged += UpdateDeleteButtonState;
            understandRisksCheckBox.CheckedChanged += UpdateDeleteButtonState;
            deleteButton.Click += DeleteButton_Click;
            
            // Make the form non-resizable
            Resize += (s, e) => LayoutControls();
        }
        
        private void SetupDialog()
        {
            var totalSize = appsToDelete.Sum(app => app.SizeInBytes);
            var protectedApps = appsToDelete.Where(app => app.IsProtected).ToList();
            var systemApps = appsToDelete.Where(app => app.IsSystemApp && !app.IsProtected).ToList();
            var userApps = appsToDelete.Where(app => !app.IsSystemApp && !app.IsProtected).ToList();
            
            // Update total size label
            totalSizeLabel.Text = $"Total size to be freed: {FormatBytes(totalSize)} from {appsToDelete.Count} application(s)";
            
            // Build warning text
            var warningText = BuildWarningText(protectedApps, systemApps, userApps);
            warningTextBox.Rtf = warningText;
            
            // Adjust dialog based on risk level
            AdjustDialogForRiskLevel(protectedApps.Count > 0, systemApps.Count > 0);
        }
        
        private string BuildWarningText(List<WindowsApp> protectedApps, List<WindowsApp> systemApps, List<WindowsApp> userApps)
        {
            var rtf = @"{\rtf1\ansi\deff0 {\fonttbl {\f0 Segoe UI;}}";
            
            rtf += @"\f0\fs18 ";
            
            if (protectedApps.Count > 0)
            {
                rtf += @"\cf2\b CRITICAL WARNING:\b0\cf0\par ";
                rtf += $"You are about to delete {protectedApps.Count} PROTECTED system application(s):\\par\\par";
                
                foreach (var app in protectedApps)
                {
                    rtf += @"\bullet " + EscapeRtf(app.GetDisplayName()) + @"\par ";
                }
                
                rtf += @"\par \cf2\b Deleting these apps may cause system instability or prevent Windows from functioning properly!\b0\cf0\par\par ";
            }
            
            if (systemApps.Count > 0)
            {
                rtf += @"\cf3\b WARNING:\b0\cf0\par ";
                rtf += $"You are about to delete {systemApps.Count} system application(s):\\par\\par";
                
                foreach (var app in systemApps)
                {
                    rtf += @"\bullet " + EscapeRtf(app.GetDisplayName()) + @"\par ";
                }
                
                rtf += @"\par \cf3\b These apps are part of Windows and may affect system functionality.\b0\cf0\par\par ";
            }
            
            if (userApps.Count > 0)
            {
                rtf += @"\cf4\b INFO:\b0\cf0\par ";
                rtf += $"You are about to delete {userApps.Count} user application(s):\\par\\par";
                
                foreach (var app in userApps.Take(5))
                {
                    rtf += @"\bullet " + EscapeRtf(app.GetDisplayName()) + @"\par ";
                }
                
                if (userApps.Count > 5)
                {
                    rtf += $@"\bullet ... and {userApps.Count - 5} more applications\par ";
                }
                
                rtf += @"\par ";
            }
            
            rtf += @"\cf1\b IMPORTANT:\b0\cf0\par ";
            rtf += @"- This action will permanently delete the selected applications and their data\par ";
            rtf += @"- Registry entries associated with these apps will also be removed\par ";
            rtf += @"- Creating a backup is strongly recommended for recovery\par ";
            rtf += @"- Some apps may be reinstalled automatically by Windows\par ";
            
            rtf += "}";
            
            return rtf;
        }
        
        private string EscapeRtf(string text)
        {
            return text.Replace(@"\", @"\\").Replace("{", @"\{").Replace("}", @"\}");
        }
        
        private void AdjustDialogForRiskLevel(bool hasProtectedApps, bool hasSystemApps)
        {
            if (hasProtectedApps)
            {
                // Critical risk - make it harder to proceed
                warningPanel.BackColor = Color.MistyRose;
                warningTextBox.BackColor = Color.MistyRose;
                deleteButton.BackColor = Color.DarkRed;
                understandRisksCheckBox.Text = "I understand the CRITICAL RISKS and still want to proceed";
                understandRisksCheckBox.ForeColor = Color.DarkRed;
                understandRisksCheckBox.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            }
            else if (hasSystemApps)
            {
                // Medium risk
                warningPanel.BackColor = Color.LightYellow;
                warningTextBox.BackColor = Color.LightYellow;
                deleteButton.BackColor = Color.DarkOrange;
                understandRisksCheckBox.Text = "I understand the risks and want to proceed";
            }
            else
            {
                // Low risk
                warningPanel.BackColor = Color.LightBlue;
                warningTextBox.BackColor = Color.LightBlue;
                deleteButton.BackColor = Color.SteelBlue;
                understandRisksCheckBox.Text = "I want to proceed with the deletion";
                understandRisksCheckBox.ForeColor = Color.DarkBlue;
            }
        }
        
        private void UpdateDeleteButtonState(object? sender, EventArgs e)
        {
            deleteButton.Enabled = confirmDeletionCheckBox.Checked && understandRisksCheckBox.Checked;
        }
        
        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            ConfirmedDeletion = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        
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
    }
} 