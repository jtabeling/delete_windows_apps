using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsAppsManager.Forms
{
    public partial class ProgressForm : Form
    {
        private Label titleLabel = null!;
        private Label statusLabel = null!;
        private ProgressBar progressBar = null!;
        
        public ProgressForm(string title, string initialStatus)
        {
            InitializeComponent();
            titleLabel.Text = title;
            statusLabel.Text = initialStatus;
        }
        
        private void InitializeComponent()
        {
            // Configure main form
            Text = "Operation in Progress";
            Size = new Size(450, 150);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            TopMost = true;
            
            // Title label
            titleLabel = new Label
            {
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                AutoSize = true,
                Location = new Point(15, 15)
            };
            
            // Status label
            statusLabel = new Label
            {
                Font = new Font("Segoe UI", 9),
                AutoSize = false,
                Size = new Size(410, 40),
                Location = new Point(15, 45),
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            // Progress bar
            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 50,
                Size = new Size(410, 23),
                Location = new Point(15, 90)
            };
            
            Controls.AddRange(new Control[] { titleLabel, statusLabel, progressBar });
        }
        
        public void UpdateStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), status);
                return;
            }
            
            statusLabel.Text = status;
            Application.DoEvents();
        }
        
        public void SetProgress(int value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(SetProgress), value);
                return;
            }
            
            if (progressBar.Style != ProgressBarStyle.Blocks)
            {
                progressBar.Style = ProgressBarStyle.Blocks;
            }
            
            progressBar.Value = Math.Max(0, Math.Min(100, value));
            Application.DoEvents();
        }
        
        public void SetIndeterminate(bool indeterminate)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(SetIndeterminate), indeterminate);
                return;
            }
            
            progressBar.Style = indeterminate ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
            Application.DoEvents();
        }
    }
} 