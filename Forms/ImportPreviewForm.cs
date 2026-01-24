using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace PingMonitor.Forms
{
    public class ImportPreviewForm : Form
    {
        public DataTable ResultData { get; private set; } 
        private DataTable _originalData;
        private DataGridView dgvPreview;
        private Label lblStatus;
        private Button btnImport;
        private Button btnCancel;
        private CheckBox chkFirstRowHeader;
        private Panel panelTop;
        private Panel panelBottom;

        public ImportPreviewForm(DataTable data, string fileName)
        {
            _originalData = data;
            ResultData = data.Copy(); 
            InitializeComponent(fileName);
            
            Services.ThemeService.OnThemeChanged += ApplyTheme;
            this.FormClosed += (s, e) => Services.ThemeService.OnThemeChanged -= ApplyTheme;
            ApplyTheme();
            
            LoadData();
        }

        private void ApplyTheme()
        {
            var isDark = Services.ThemeService.IsDarkMode;
            this.BackColor = Services.ThemeService.Background;
            
            if (panelTop != null) panelTop.BackColor = Services.ThemeService.Background;
            if (panelBottom != null) panelBottom.BackColor = Services.ThemeService.Surface;

            if (lblStatus != null) lblStatus.ForeColor = Services.ThemeService.TextMain;
            if (chkFirstRowHeader != null) chkFirstRowHeader.ForeColor = Services.ThemeService.TextMain;

            if (dgvPreview != null)
            {
                dgvPreview.BackgroundColor = Services.ThemeService.Background;
                dgvPreview.DefaultCellStyle.BackColor = Services.ThemeService.Surface;
                dgvPreview.DefaultCellStyle.ForeColor = Services.ThemeService.TextMain;
                dgvPreview.DefaultCellStyle.SelectionBackColor = Services.ThemeService.Selection;
                dgvPreview.DefaultCellStyle.SelectionForeColor = Color.White;
                
                dgvPreview.ColumnHeadersDefaultCellStyle.BackColor = Services.ThemeService.BrandColor;
                dgvPreview.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            }
            
            if (btnCancel != null)
            {
                btnCancel.BackColor = Services.ThemeService.BtnSecondary;
                btnCancel.ForeColor = Color.White;
            }
            if (btnImport != null)
            {
                btnImport.BackColor = Services.ThemeService.BtnSuccess;
                btnImport.ForeColor = Color.White;
            }
        }

        private void InitializeComponent(string fileName)
        {
            this.Text = $"Preview Import - {fileName}";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);

            // ======================
            // TOP PANEL
            // ======================
            panelTop = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(15, 10, 15, 10) };
            
            lblStatus = new Label 
            { 
                Text = $"Tìm thấy {_originalData?.Rows.Count ?? 0} hàng.", 
                AutoSize = true, 
                Location = new Point(15, 15),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            panelTop.Controls.Add(lblStatus);

            chkFirstRowHeader = new CheckBox
            {
                Text = "Dòng đầu tiên là tiêu đề",
                AutoSize = true,
                Location = new Point(300, 16),
                Checked = true, 
                Cursor = Cursors.Hand
            };
            chkFirstRowHeader.CheckedChanged += (s, e) => ReloadGrid();
            panelTop.Controls.Add(chkFirstRowHeader);

            this.Controls.Add(panelTop);

            // ======================
            // BOTTOM PANEL (BUTTONS)
            // ======================
            panelBottom = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 60, 
                Padding = new Padding(10)
            };
            
            // Flow layout cho buttons để đảm bảo luôn hiển thị
            var flowButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 5, 10, 5)
            };

            btnCancel = new Button 
            { 
                Text = "Hủy bỏ", 
                Size = new Size(100, 35), 
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(10, 0, 0, 0)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            flowButtons.Controls.Add(btnCancel);

            btnImport = new Button 
            { 
                Text = "Xác nhận Import", 
                Size = new Size(140, 35), 
                BackColor = Services.ThemeService.BtnSuccess, 
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnImport.FlatAppearance.BorderSize = 0;
            btnImport.Click += BtnImport_Click;
            flowButtons.Controls.Add(btnImport);

            panelBottom.Controls.Add(flowButtons);
            this.Controls.Add(panelBottom);

            // ======================
            // GRID
            // ======================
            dgvPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            
            // Style grid (Initial defaults, will be partly overridden by ApplyTheme)
            dgvPreview.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { 
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(5)
            };
            dgvPreview.DefaultCellStyle = new DataGridViewCellStyle { 
                Padding = new Padding(5)
            };
            dgvPreview.EnableHeadersVisualStyles = false;

            this.Controls.Add(dgvPreview);
            
            // Đảm bảo Grid nằm giữa Top và Bottom
            dgvPreview.BringToFront(); 
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            // Xử lý dữ liệu cuối cùng trước khi trả về
            ResultData = _originalData.Copy();

            if (chkFirstRowHeader.Checked && ResultData.Rows.Count > 0)
            {
                var headerRow = ResultData.Rows[0];
                for (int i = 0; i < ResultData.Columns.Count; i++)
                {
                    string headerVal = headerRow[i]?.ToString().Trim();
                    if (!string.IsNullOrEmpty(headerVal))
                    {
                        if (ResultData.Columns.Contains(headerVal) && ResultData.Columns[headerVal].Ordinal != i)
                            headerVal += $"_{i}";
                        try { ResultData.Columns[i].ColumnName = headerVal; } catch {}
                    }
                }
                ResultData.Rows.RemoveAt(0);
            }
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void LoadData()
        {
            ReloadGrid();
        }

        private void ReloadGrid()
        {
            if (_originalData == null) return;

            DataTable displayTable = _originalData.Copy();
            
            if (chkFirstRowHeader.Checked && displayTable.Rows.Count > 0)
            {
                var headerRow = displayTable.Rows[0];
                for (int i = 0; i < displayTable.Columns.Count; i++)
                {
                    string headerVal = headerRow[i]?.ToString().Trim();
                    if (!string.IsNullOrEmpty(headerVal))
                    {
                        if (displayTable.Columns.Contains(headerVal) && displayTable.Columns[headerVal].Ordinal != i)
                            headerVal += $"_{i}";
                        try { displayTable.Columns[i].ColumnName = headerVal; } catch {}
                    }
                }
                displayTable.Rows.RemoveAt(0);
            }

            dgvPreview.DataSource = displayTable;
            lblStatus.Text = $"Hiển thị {displayTable.Rows.Count} hàng dữ liệu.";
        }
    }
}
