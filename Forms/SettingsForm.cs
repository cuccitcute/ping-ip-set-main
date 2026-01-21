using System;
using System.Drawing;
using System.Windows.Forms;
using PingMonitor.Models;
using PingMonitor.Services;
using PingMonitor.Helpers;

namespace PingMonitor.Forms
{
    public class SettingsForm : Form
    {
        private AppSettings settings;
        private DatabaseHelper db;
        private LocalizationService _loc;

        // Controls
        private ComboBox cboLanguage;
        private CheckBox chkDarkMode;
        private CheckBox chkAutoRefresh;
        private NumericUpDown numInterval;
        private NumericUpDown numTimeout;
        private NumericUpDown numMaxConcurrent;
        private NumericUpDown numRetry;
        private NumericUpDown numOfflineThreshold;

        public SettingsForm()
        {
            db = new DatabaseHelper();
            settings = db.GetSettings();
            _loc = new LocalizationService();
            _loc.SetLanguage(settings.Language);

            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = _loc.Get("Settings");
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = ThemeService.Background;
            this.ForeColor = ThemeService.TextMain;

            int padding = 20;
            int y = 20;

            // Group: General
            var grpGeneral = CreateGroup(_loc.Get("Settings_General"), new Point(padding, y), new Size(440, 130));
            this.Controls.Add(grpGeneral);

            int gy = 30;
            int lx = 20;
            int cx = 200;

            // Language
            grpGeneral.Controls.Add(new Label { Text = _loc.Get("Lbl_Language"), Location = new Point(lx, gy), AutoSize = true, ForeColor = ThemeService.TextMain });
            cboLanguage = new ComboBox 
            { 
                Location = new Point(cx, gy - 3), 
                Size = new Size(150, 25), 
                DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = ThemeService.Surface, 
                ForeColor = ThemeService.TextMain,
                FlatStyle = FlatStyle.Popup // Better visibility
            };
            cboLanguage.Items.AddRange(new[] { "Tiếng Việt", "English", "한국어" });
            cboLanguage.SelectedIndex = settings.Language >= 0 && settings.Language <= 2 ? settings.Language : 0;
            grpGeneral.Controls.Add(cboLanguage);
            cboLanguage.BringToFront(); // Ensure it's on top
            gy += 35;

            // Dark Mode
            chkDarkMode = new CheckBox { Text = _loc.Get("Lbl_DarkMode"), Location = new Point(lx, gy), AutoSize = true, Checked = settings.IsDarkMode, ForeColor = ThemeService.TextMain };
            grpGeneral.Controls.Add(chkDarkMode);
            gy += 30;

            // Auto Refresh
            chkAutoRefresh = new CheckBox { Text = _loc.Get("Lbl_AutoRefresh"), Location = new Point(lx, gy), AutoSize = true, Checked = settings.AutoRefresh, ForeColor = ThemeService.TextMain };
            grpGeneral.Controls.Add(chkAutoRefresh);

            y += 140;

            // Group: Ping Config
            var grpPing = CreateGroup(_loc.Get("Settings_PingConfig"), new Point(padding, y), new Size(440, 210));
            this.Controls.Add(grpPing);
            
            gy = 30;

            // Interval
            CreateNumericRow(grpPing, _loc.Get("Lbl_Interval"), ref numInterval, settings.RefreshIntervalSeconds, 5, 3600, lx, cx, ref gy);
            
            // Timeout
            CreateNumericRow(grpPing, _loc.Get("Lbl_Timeout"), ref numTimeout, settings.PingTimeoutMs, 100, 10000, lx, cx, ref gy);

            // Max Concurrent
            CreateNumericRow(grpPing, _loc.Get("Lbl_MaxConcurrent"), ref numMaxConcurrent, settings.MaxConcurrentPings, 1, 200, lx, cx, ref gy);

            // Retry Count
            CreateNumericRow(grpPing, _loc.Get("Lbl_Retry"), ref numRetry, settings.PingRetryCount, 0, 10, lx, cx, ref gy);

            // Offline Threshold
            CreateNumericRow(grpPing, _loc.Get("Lbl_OfflineThreshold"), ref numOfflineThreshold, settings.OfflineThreshold, 1, 100, lx, cx, ref gy);

            // Buttons
            var btnSave = new Button { Text = _loc.Get("Btn_Save"), DialogResult = DialogResult.OK, Location = new Point(240, 380), Size = new Size(110, 35), BackColor = ThemeService.BtnSuccess, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            var btnCancel = new Button { Text = _loc.Get("Btn_Cancel"), DialogResult = DialogResult.Cancel, Location = new Point(360, 380), Size = new Size(110, 35), BackColor = ThemeService.BtnDanger, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            this.Controls.Add(btnCancel);
        }

        private GroupBox CreateGroup(string text, Point loc, Size size)
        {
            return new GroupBox 
            { 
                Text = text, Location = loc, Size = size, 
                ForeColor = ThemeService.BtnPrimary, 
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
        }

        private void CreateNumericRow(GroupBox parent, string label, ref NumericUpDown num, int val, int min, int max, int lx, int cx, ref int y)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(lx, y), AutoSize = true, ForeColor = ThemeService.TextMain, Font = new Font("Segoe UI", 9F, FontStyle.Regular) });
            num = new NumericUpDown { Location = new Point(cx, y - 3), Size = new Size(100, 25), Minimum = min, Maximum = max, Value = Math.Max(min, Math.Min(max, val)), BackColor = ThemeService.Surface, ForeColor = ThemeService.TextMain };
            parent.Controls.Add(num);
            y += 35;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            settings.Language = cboLanguage.SelectedIndex;
            settings.IsDarkMode = chkDarkMode.Checked;
            settings.AutoRefresh = chkAutoRefresh.Checked;
            settings.RefreshIntervalSeconds = (int)numInterval.Value;
            settings.PingTimeoutMs = (int)numTimeout.Value;
            settings.MaxConcurrentPings = (int)numMaxConcurrent.Value;
            settings.PingRetryCount = (int)numRetry.Value;
            settings.OfflineThreshold = (int)numOfflineThreshold.Value;

            db.SaveSettings(settings);
        }
    }
}
