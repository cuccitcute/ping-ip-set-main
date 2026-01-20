using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace PingMonitor.Forms
{
    public class ImportPreviewForm : Form
    {
        public DataTable ResultData { get; private set; } // Dữ liệu đã xử lý để trả về
        private DataTable _originalData;
        private DataGridView dgvPreview;
        private Label lblStatus;
        private Button btnImport;
        private Button btnCancel;
        private CheckBox chkFirstRowHeader;

        public ImportPreviewForm(DataTable data, string fileName)
        {
            _originalData = data;
            ResultData = data.Copy(); // Mặc định là copy
            InitializeComponent(fileName);
            LoadData();
        }

        private void InitializeComponent(string fileName)
        {
            this.Text = $"Preview Import - {fileName}";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(240, 240, 245);
            this.Font = new Font("Segoe UI", 9F);

            // ======================
            // TOP PANEL
            // ======================
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(15, 10, 15, 10) };
            
            lblStatus = new Label 
            { 
                Text = $"Tìm thấy {_originalData?.Rows.Count ?? 0} hàng.", 
                AutoSize = true, 
                Location = new Point(15, 15),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41)
            };
            panelTop.Controls.Add(lblStatus);

            chkFirstRowHeader = new CheckBox
            {
                Text = "Dòng đầu tiên là tiêu đề",
                AutoSize = true,
                Location = new Point(300, 16),
                Checked = true, // Default to true
                ForeColor = Color.FromArgb(33, 37, 41),
                Cursor = Cursors.Hand
            };
            chkFirstRowHeader.CheckedChanged += (s, e) => ReloadGrid();
            panelTop.Controls.Add(chkFirstRowHeader);

            this.Controls.Add(panelTop);

            // ======================
            // BOTTOM PANEL (BUTTONS)
            // ======================
            var panelBottom = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 60, 
                BackColor = Color.White, // Nền trắng cho nổi bật
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
                BackColor = Color.FromArgb(108, 117, 125), 
                ForeColor = Color.White,
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
                BackColor = Color.FromArgb(46, 125, 50), 
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                // DialogResult = DialogResult.OK, // Xử lý thủ công để process data trước
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
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            
            // Style grid
            dgvPreview.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { 
                BackColor = Color.FromArgb(230, 232, 235), 
                ForeColor = Color.Black, 
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(5)
            };
            dgvPreview.DefaultCellStyle = new DataGridViewCellStyle { 
                Padding = new Padding(5),
                SelectionBackColor = Color.FromArgb(179, 229, 252),
                SelectionForeColor = Color.Black
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
