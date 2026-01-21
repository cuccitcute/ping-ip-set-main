using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text;
using PingMonitor.Helpers;
using PingMonitor.Services;

namespace PingMonitor.Forms
{
    public class LogViewerForm : Form
    {
        private DataGridView dgvLogs;
        private DatabaseHelper db;
        private LocalizationService _loc;

        public LogViewerForm()
        {
            db = new DatabaseHelper();
            _loc = new LocalizationService();
            var settings = db.GetSettings();
            _loc.SetLanguage(settings.Language);

            InitializeUI();
            LoadLogs();
        }

        private void InitializeUI()
        {
            this.Text = _loc.Get("ViewLogs");
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ThemeService.Background;

            // DataGridView for logs
            dgvLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ThemeService.Background,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9F)
            };

            dgvLogs.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = ThemeService.BtnPrimary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            dgvLogs.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = ThemeService.Surface,
                ForeColor = ThemeService.TextMain,
                SelectionBackColor = ThemeService.Selection,
                SelectionForeColor = Color.Black
            };

            // Define columns
            dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTimestamp", HeaderText = "Timestamp", Width = 150 });
            dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIpAddress", HeaderText = "IP Address", Width = 120 });
            dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLocation", HeaderText = "Location", Width = 100 });
            dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEvent", HeaderText = "Event", Width = 80 });
            dgvLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDetails", HeaderText = "Details", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            this.Controls.Add(dgvLogs);

            // Close button
            var btnClose = new Button
            {
                Text = _loc.Get("Btn_Cancel"),
                Size = new Size(100, 35),
                Location = new Point(this.ClientSize.Width - 120, this.ClientSize.Height - 50),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                BackColor = ThemeService.BtnSecondary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
            btnClose.BringToFront();

            // Export button (Bottom Left)
            var btnExport = new Button
            {
                Text = _loc.Get("ExportLogs") ?? "Export Logs",
                Size = new Size(100, 35),
                Location = new Point(20, this.ClientSize.Height - 50),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = ThemeService.BtnSuccess,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnExport.Click += (s, e) => ExportLogs();
            this.Controls.Add(btnExport);
            btnExport.BringToFront();
        }

        private void ExportLogs()
        {
            try
            {
                var logs = db.GetLogs(50000);
                if (logs.Count == 0) { MessageBox.Show("No logs to export."); return; }
                using (var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"PingLog_{DateTime.Now:yyyyMMdd}.csv" })
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Timestamp,IP Address,Location,Event,Details");
                        foreach (var log in logs) sb.AppendLine($"\"{log.Timestamp}\",\"{log.IpAddress}\",\"{log.Location}\",\"{log.Event}\",\"{log.Details}\"");
                        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show("Export successful!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLogs()
        {
            try
            {
                var logs = db.GetLogs(1000); // Get last 1000 logs
                dgvLogs.Rows.Clear();
                foreach (var log in logs)
                {
                    dgvLogs.Rows.Add(
                        log.Timestamp,
                        log.IpAddress,
                        log.Location,
                        log.Event,
                        log.Details
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
