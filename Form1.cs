using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PingMonitor.Helpers;
using PingMonitor.Models;
using PingMonitor.Forms;

using PingMonitor.Services;

namespace PingMonitor
{
    // ============================================
    // Main Form
    // ============================================
    public partial class Form1 : Form
    {
        // ============================================
        // MODERN THEME PALETTE
        // ============================================
        private readonly Color COLOR_BACKGROUND = Color.FromArgb(243, 244, 246);   // #F3F4F6 (Light Gray)
        private readonly Color COLOR_SURFACE = Color.FromArgb(255, 255, 255);      // #FFFFFF (White)
        private readonly Color COLOR_PRIMARY = Color.FromArgb(37, 99, 235);        // #2563EB (Royal Blue)
        
        // Status Colors
        private readonly Color COLOR_ONLINE_BG = Color.FromArgb(209, 250, 229);    // #D1FAE5 (Light Emerald)
        private readonly Color COLOR_ONLINE_TEXT = Color.FromArgb(6, 95, 70);      // #065F46 (Dark Emerald)
        private readonly Color COLOR_OFFLINE_BG = Color.FromArgb(254, 226, 226);   // #FEE2E2 (Light Red)
        private readonly Color COLOR_OFFLINE_TEXT = Color.FromArgb(153, 27, 27);     // #991B1B (Dark Red)
        
        // Text Colors
        private readonly Color COLOR_TEXT_MAIN = Color.FromArgb(17, 24, 39);       // #111827 (Gray 900)
        private readonly Color COLOR_TEXT_MUTED = Color.FromArgb(107, 114, 128);   // #6B7280 (Gray 500)
        
        // UI Elements
        private readonly Color COLOR_BORDER = Color.FromArgb(229, 231, 235);       // #E5E7EB
        private readonly Color COLOR_HEADER = Color.FromArgb(249, 250, 251);       // #F9FAFB
        private readonly Color COLOR_SURFACE_ALT = Color.FromArgb(249, 250, 251);      // #F9FAFB
        private readonly Color COLOR_SELECTION = Color.FromArgb(219, 234, 254);    // #DBEAFE (Blue 100)

        // Action Buttons (keep some specific for recognition but muted)
        private readonly Color COLOR_BTN_ADD = Color.FromArgb(59, 130, 246);       // Blue 500
        private readonly Color COLOR_BTN_DELETE = Color.FromArgb(239, 68, 68);     // Red 500
        private readonly Color COLOR_BTN_PING = Color.FromArgb(16, 185, 129);      // Green 500
        private readonly Color COLOR_BTN_WARNING = Color.FromArgb(245, 158, 11);   // Amber 500
        private readonly Color COLOR_BTN_GRAY = Color.FromArgb(158, 158, 158);       // Xám pastel

        // ========== CONTROLS ==========
        private DataGridView dgvMonitor;
        private SplitContainer splitContainer;
        private Panel panelSidebar;
        private Panel panelToolbar; 

        // Sidebar
        private Label lblSidebarTitle;
        private Button btnToggleSidebar;
        private ListBox lbUsers;

        // Stats Labels
        private Label lblTotal;
        private Label lblOnline;
        private Label lblOffline;
        
        // Buttons
        private Button btnSettings;
        private Button btnViewLogs;
        private Button btnExportLog;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnClearAll;
        private Button btnPingNow;
        private Button btnImport;
        private Button btnExport;

        private CheckBox chkAutoRefresh;
        
        // Search & Filter
        private TextBox txtSearch;
        private Panel panelSearch;
        
        // Translatable Strings for Sidebar
        private string strAll = "Tất cả";
        private string strEmptyGroup = "Trống";
        
        // Context Menu
        private ContextMenuStrip contextMenu;
        
        // Misc
        private Label lblDbInfo;
        private ProgressBar progressBar;
        private System.Windows.Forms.Timer refreshTimer;
        private Button btnClearFilter;
        private Label lblAuthor;
        private Label lblTitle;
        private Label lblSubLabel;
        private Label lblTotalTitle;
        private Label lblOnlineTitle;
        private Label lblOfflineTitle;
        private Label lblSearchPlaceholder;
        
        // Translatable Labels
        private ComboBox cboLanguage;
        private int currentLanguageIndex = 0; // 0: VN, 1: EN, 2: KR

        // Notifications
        private int unreadLogCount = 0;

        // Sorting
        private int sortColumnIndex = -1;
        private bool sortAscending = true;

        // ========== DATA ==========
        private List<IpMonitor> ipList = new List<IpMonitor>();
        private bool isRefreshing = false;
        private AppSettings settings;
        private DatabaseHelper db;
        private PingService pingService;

        public Form1()
        {
            InitializeComponent();
            
            // Khởi tạo Database
            db = new DatabaseHelper();
            pingService = new PingService();
            
            // Load settings từ DB
            settings = db.GetSettings();
            
            // Load IP list từ DB
            ipList = db.GetAllIpMonitors();
            
            // Set Form Icon
            var iconPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "icon.png");
            if (File.Exists(iconPath))
            {
                using (var bmp = new Bitmap(iconPath))
                    this.Icon = Icon.FromHandle(bmp.GetHicon());
            }
            
            InitializeUI();
            InitializeTimer();
            
            // Cập nhật giao diện
            FilterGrid("");
            
            // Log startup
            AddLog("System", "", "Started", "Ứng dụng khởi động");
        }

        // ============================================
        // VALIDATE IP
        // ============================================
        private bool ValidateIpAddress(string input, out string errorMessage)
        {
            errorMessage = "";
            if (string.IsNullOrWhiteSpace(input))
            {
                errorMessage = "Địa chỉ IP không được!";
                return false;
            }

            input = input.Trim();
            if (input.Length > 255)
            {
                errorMessage = "Địa chỉ IP quá dài!";
                return false;
            }

            string ipv4Pattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            if (Regex.IsMatch(input, ipv4Pattern)) return true;

            if (IPAddress.TryParse(input, out IPAddress ipAddr))
                if (ipAddr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    return true;

            string hostnamePattern = @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)*[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?$";
            if (Regex.IsMatch(input, hostnamePattern)) return true;

            errorMessage = "Định dạng IP/Hostname không hợp lệ!";
            return false;
        }

        // ============================================
        // ADD LOG (Lưu vào DB)
        // ============================================
        private void AddLog(string ip, string location, string eventType, string details = "", string deviceName = "", string user = "")
        {
            db.AddLog(new LogEntry
            {
                Timestamp = DateTime.Now,
                IpAddress = ip,
                Location = location,
                Event = eventType,
                Details = details,
                DeviceName = deviceName,
                User = user
            });

            // Notification Logic
            System.Threading.Interlocked.Increment(ref unreadLogCount);
            if (btnViewLogs != null && !btnViewLogs.IsDisposed) 
            {
                // Ensure UI update on main thread if needed, though Invalidate is safe
                if (btnViewLogs.InvokeRequired)
                    btnViewLogs.BeginInvoke(new Action(() => btnViewLogs.Invalidate()));
                else
                    btnViewLogs.Invalidate();
            }
        }

        // ============================================
        // KHỞI TẠO GIAO DIỆN
        // ============================================
        private void InitializeUI()
        {
            this.Text = "Ping Monitor V2.0 [Mr Hiệu - Srtech Sub]";
            this.Size = new Size(1350, 800);
            this.MinimumSize = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = COLOR_BACKGROUND;
            this.Font = new Font("Segoe UI", 9F);

            // 1. Top Bar (Title) - Luôn nằm trên cùng
            InitializeTopBar();

            // 2. Main Content (Sidebar + Content)
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                IsSplitterFixed = false,
                SplitterWidth = 5,
                Orientation = Orientation.Vertical
            };
            this.Controls.Add(splitContainer);
            this.Controls.SetChildIndex(splitContainer, 0);
            // Set splitter distance when form is shown (after layout is complete)
            this.Shown += (s, e) => { splitContainer.SplitterDistance = 180; };

            // Panel Sidebar
            panelSidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            splitContainer.Panel1.Controls.Add(panelSidebar);
            
            // Sidebar Controls
            // InitializeSidebar(); // Moved to end


            // Panel Content (Stats + Toolbar + Grid)
            var panelContent = new Panel { Dock = DockStyle.Fill };
            splitContainer.Panel2.Controls.Add(panelContent);

            // Initialize content components
            InitializeCombinedToolbar(panelContent); 
            InitializeStatsBar(panelContent);        
            InitializeDataGridView(panelContent);    
            InitializeContextMenu();
            InitializeKeyboardShortcuts();
            
            // Initialize Sidebar last (depends on grid logic potentially)
            InitializeSidebar();
        }

        private void InitializeCombinedToolbar(Control parent)
        {
            // ========== HÀNG 3: ALL CONTROLS (Buttons + Search + Actions) ==========
            panelToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = COLOR_BACKGROUND
            };
            parent.Controls.Add(panelToolbar);

            int x = 10, y = 6;

            // LEFT: Thêm, Xóa, Xóa tất cả, Ping
            btnAdd = CreateButton("Thêm thiết bị", COLOR_PRIMARY, new Point(x, y), new Size(95, 26));
            btnAdd.Click += BtnAdd_Click;
            panelToolbar.Controls.Add(btnAdd);
            x += 100;

            btnRemove = CreateButton("Xóa", COLOR_BTN_DELETE, new Point(x, y), new Size(45, 26));
            btnRemove.Click += BtnRemove_Click;
            panelToolbar.Controls.Add(btnRemove);
            x += 50;

            btnClearAll = CreateButton("Xóa tất cả", Color.FromArgb(180, 80, 80), new Point(x, y), new Size(70, 26));
            btnClearAll.Click += BtnClearAll_Click;
            panelToolbar.Controls.Add(btnClearAll);
            x += 75;

            btnPingNow = CreateButton("Ping!", COLOR_BTN_PING, new Point(x, y), new Size(50, 26));
            btnPingNow.Click += async (s, e) => await RefreshAllPings();
            panelToolbar.Controls.Add(btnPingNow);
            x += 55;

            // Reset View Button
            var btnResetView = CreateButton("↺", Color.Gray, new Point(x, y), new Size(30, 26));
            btnResetView.Click += (s, e) => ResetGridViewLayout();
            // Tooltip for reset
            var tipReset = new ToolTip();
            tipReset.SetToolTip(btnResetView, "Khôi phục hiển thị (Cột, Hàng, Sắp xếp)");
            panelToolbar.Controls.Add(btnResetView);
            x += 35;

            // MIDDLE: Search box
            var searchContainer = new Panel
            {
                Width = 280,
                Height = 28, // Tăng height chút
                BackColor = COLOR_SURFACE,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(x, y + 2) // Căn chỉnh Y để thẳng hàng
            };
            panelToolbar.Controls.Add(searchContainer);

            var lblIcon = new Label { Text = "🔍", Font = new Font("Segoe UI", 8F), AutoSize = true, Location = new Point(3, 3), BackColor = COLOR_SURFACE };
            searchContainer.Controls.Add(lblIcon);

            lblSearchPlaceholder = new Label
            {
                Text = "Tìm kiếm IP, Tên thiết bị, MAC, IMEI,...",
                ForeColor = Color.Silver, // Màu xám nhạt
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                AutoSize = true,
                Location = new Point(22, 4),
                BackColor = COLOR_SURFACE,
                Cursor = Cursors.IBeam
            };
            lblSearchPlaceholder.Click += (s, e) => txtSearch.Focus();
            searchContainer.Controls.Add(lblSearchPlaceholder);

            txtSearch = new TextBox
            {
                Location = new Point(22, 2),
                Size = new Size(230, 20), // Reduced width to make room for X button
                Font = new Font("Segoe UI", 8F),
                BackColor = COLOR_SURFACE,
                ForeColor = COLOR_TEXT_MAIN,
                BorderStyle = BorderStyle.None
            };
            txtSearch.TextChanged += (s, e) => {
                lblSearchPlaceholder.Visible = string.IsNullOrEmpty(txtSearch.Text);
                if (searchContainer.Controls.ContainsKey("lblClear"))
                    searchContainer.Controls["lblClear"].Visible = !string.IsNullOrEmpty(txtSearch.Text);
                TxtSearch_TextChanged(s, e);
            };
            
            // Clear Search Button (X)
            var lblClear = new Label
            {
                Name = "lblClear",
                Text = "✖",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Visible = false,
                Location = new Point(255, 5), // Right side
                BackColor = COLOR_SURFACE
            };
            lblClear.Click += (s, e) => { txtSearch.Text = ""; txtSearch.Focus(); };
            searchContainer.Controls.Add(lblClear);
            lblClear.BringToFront();
            txtSearch.GotFocus += (s, e) => lblSearchPlaceholder.Visible = false;
            txtSearch.LostFocus += (s, e) => lblSearchPlaceholder.Visible = string.IsNullOrEmpty(txtSearch.Text);
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { txtSearch.Clear(); e.Handled = true; } };
            searchContainer.Controls.Add(txtSearch);
            txtSearch.BringToFront();

            // RIGHT: FlowLayoutPanel for action buttons
            var flowRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 2, 5, 0)
            };
            panelToolbar.Controls.Add(flowRight);

            chkAutoRefresh = new CheckBox
            {
                Text = "Tự động", ForeColor = COLOR_TEXT_MAIN,
                Font = new Font("Segoe UI", 7.5F),
                Checked = settings.AutoRefresh, AutoSize = true,
                Margin = new Padding(3, 8, 3, 0), Cursor = Cursors.Hand
            };
            chkAutoRefresh.CheckedChanged += (s, e) => {
                settings.AutoRefresh = chkAutoRefresh.Checked;
                db.SaveSettings(settings);
                if (settings.AutoRefresh) refreshTimer.Start(); else refreshTimer.Stop();
            };
            flowRight.Controls.Add(chkAutoRefresh);

            btnImport = CreateButton("Nhập Excel", COLOR_BTN_WARNING, Point.Empty, new Size(90, 28));
            btnImport.Click += BtnImport_Click;
            btnImport.Margin = new Padding(2, 4, 2, 0);
            flowRight.Controls.Add(btnImport);

            btnExport = CreateButton("Xuất Excel", Color.FromArgb(46, 125, 50), Point.Empty, new Size(90, 28));
            btnExport.Click += BtnExport_Click;
            btnExport.Margin = new Padding(2, 4, 2, 0);
            flowRight.Controls.Add(btnExport);

            btnViewLogs = CreateButton("Xem Logs", Color.FromArgb(100, 116, 139), Point.Empty, new Size(100, 28));
            btnViewLogs.Click += BtnViewLogs_Click;
            btnViewLogs.Margin = new Padding(2, 4, 2, 0);
            // Notification Badge Paint
            btnViewLogs.Paint += (s, ev) => 
            {
                if (unreadLogCount > 0)
                {
                    var g = ev.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    
                    // Count Text
                    string countText = unreadLogCount > 99 ? "99+" : unreadLogCount.ToString();
                    
                    // Badge Circle
                    int size = 18;
                    int badgeX = btnViewLogs.Width - size - 4;
                    int badgeY = 2; // Top right
                    Rectangle rect = new Rectangle(badgeX, badgeY, size, size);
                    
                    using (var brush = new SolidBrush(Color.Red)) g.FillEllipse(brush, rect);
                    using (var brush = new SolidBrush(Color.White))
                    {
                        var font = new Font("Segoe UI", 8F, FontStyle.Bold);
                        var textSize = g.MeasureString(countText, font);
                        // Center text
                        float tx = rect.X + (rect.Width - textSize.Width) / 2;
                        float ty = rect.Y + (rect.Height - textSize.Height) / 2;
                        g.DrawString(countText, font, brush, tx, ty);
                    }

                    // Bell Icon (Symbol) - Optional, vẽ 1 icon chuông nhỏ màu đỏ bên cạnh text? 
                    // Do button text đã có, vẽ đè lên sẽ rối. Badge đỏ là đủ chuẩn notification.
                    // Nhưng user request "icon hình chuông đỏ". Vẽ thêm ký tự 🔔 màu đỏ?
                    // g.DrawString("🔔", new Font("Segoe UI", 10F), Brushes.Red, 5, 5); // hơi chật.
                    // Badge là đủ tốt rồi.
                }
            };
            flowRight.Controls.Add(btnViewLogs);

            btnExportLog = CreateButton("Xuất Logs", Color.FromArgb(139, 92, 246), Point.Empty, new Size(100, 28));
            btnExportLog.Click += BtnExportLog_Click;
            btnExportLog.Margin = new Padding(2, 4, 2, 0);
            flowRight.Controls.Add(btnExportLog);

            btnSettings = CreateButton("⚙ Cài đặt", Color.FromArgb(71, 85, 105), Point.Empty, new Size(95, 28));
            btnSettings.Click += BtnSettings_Click;
            btnSettings.Margin = new Padding(2, 4, 2, 0);
            flowRight.Controls.Add(btnSettings);
        }

        private void ApplyLanguage(int index)
        {
            // 0: VN, 1: EN, 2: KR
            if (index < 0 || index > 2) return;
            currentLanguageIndex = index;

            // Window Title
            string[] tWindowTitle = { "Ping Monitor V2.0 [Mr Hiệu - Srtech Sub]", "Ping Monitor V2.0 [Mr Hiệu - Srtech Sub]", "핑 모니터 V2.0 [Mr Hiệu - Srtech Sub]" };
            this.Text = tWindowTitle[index];

            // Arrays for simple mapping
            string[] tTitle = { "Ping Monitor", "Ping Monitor", "핑 모니터" };
            string[] tSub = { "Phần mềm theo dõi IP thiết bị", "IP Device Monitoring Software", "IP 장치 모니터링 소프트웨어" };
            
            // Sidebar
            string[] tSidebar = { "LỌC NGƯỜI DÙNG", "USER FILTER", "사용자 필터" };
            string[] tClearFilter = { "Hủy lọc", "Clear Filter", "필터 지우기" };
            strAll = index == 0 ? "Tất cả" : (index == 1 ? "All" : "모두");
            strEmptyGroup = index == 0 ? "Trống" : (index == 1 ? "Empty" : "비어 있음");
            
            // Stats
            string[] tTotal = { "TỔNG THIẾT BỊ", "TOTAL DEVICES", "전체 장치" };
            string[] tOnline = { "ONLINE", "ONLINE", "온라인" };
            string[] tOffline = { "OFFLINE", "OFFLINE", "오프라인" };
            
            // Search
            string[] tSearch = { "Tìm kiếm IP, Tên thiết bị, MAC, IMEI,...", "Search IP, Device Name, MAC, IMEI...", "IP 검색, 장치 이름, MAC, IMEI..." };
            
            // Buttons
            string[] tAdd = { "Thêm thiết bị", "Add Device", "장치 추가" };
            string[] tRemove = { "Xóa", "Delete", "삭제" };
            string[] tClear = { "Xóa tất cả", "Clear All", "모두 삭제" };
            string[] tPing = { "Ping!", "Ping!", "핑!" };
            string[] tViewLogs = { "Xem Logs", "View Logs", "로그 보기" };
            string[] tExportLog = { "Xuất Logs", "Export Logs", "로그 내보내기" };
            string[] tImport = { "Nhập Excel", "Import Excel", "엑셀 가져오기" };
            string[] tExport = { "Xuất Excel", "Export Excel", "엑셀 내보내기" };
            string[] tSettings = { "⚙ Cài đặt", "⚙ Settings", "⚙ 설정" };
         
            // Apply Key Labels
            if (lblTitle != null) lblTitle.Text = tTitle[index];
            if (lblSubLabel != null) lblSubLabel.Text = tSub[index];
            if (lblSidebarTitle != null) lblSidebarTitle.Text = tSidebar[index];
            if (lblTotalTitle != null) lblTotalTitle.Text = tTotal[index];
            if (lblOnlineTitle != null) lblOnlineTitle.Text = tOnline[index];
            if (lblOfflineTitle != null) lblOfflineTitle.Text = tOffline[index];
            if (lblSearchPlaceholder != null) lblSearchPlaceholder.Text = tSearch[index];
            
            // Apply Buttons
            if (btnAdd != null) btnAdd.Text = tAdd[index];
            if (btnRemove != null) btnRemove.Text = tRemove[index];
            if (btnClearAll != null) btnClearAll.Text = tClear[index];
            if (btnPingNow != null) btnPingNow.Text = tPing[index];
            if (btnViewLogs != null) btnViewLogs.Text = tViewLogs[index];
            if (btnImport != null) btnImport.Text = tImport[index];
            if (btnExport != null) btnExport.Text = tExport[index];
            if (btnExportLog != null) btnExportLog.Text = tExportLog[index];
            if (btnSettings != null) btnSettings.Text = tSettings[index];
            if (btnSettings != null) btnSettings.Text = tSettings[index];
            if (btnClearFilter != null) btnClearFilter.Text = tClearFilter[index];

            // Footer Text
            string[] tAuthor = {
                "Phiên bản V2.1\nViết bởi Phạm Huy Hiệu\nBộ phận Thiết bị SUB - SR TECH",
                "Version V2.1\nWritten by Phạm Huy Hiệu\nEQM SUB-SR",
                "버전 V2.1\n작성자 Phạm Huy Hiệu\nEQM SUB-SR"
            };
            if (lblAuthor != null) lblAuthor.Text = tAuthor[index];

            // Auto-refresh checkbox
            string[] tAutoRefresh = { "Tự động", "Auto", "자동" };
            if (chkAutoRefresh != null) chkAutoRefresh.Text = tAutoRefresh[index];

            // Grid Headers
            if (dgvMonitor != null)
            {
                string[][] headers = {
                    new[] { "#", "Nhóm", "Tên thiết bị", "Hình ảnh", "IP", "MAC", "Serial/IMEI", "Người dùng", "Vị trí", "Trạng thái", "Ping", "Time check", "Time Offline", "Đếm", "Ngày tạo" },
                    new[] { "#", "Group", "Device Name", "Image", "IP", "MAC", "Serial/IMEI", "User", "Location", "Status", "Ping", "Last Check", "Last Offline", "Count", "Created" },
                    new[] { "#", "그룹", "장치 이름", "이미지", "IP", "MAC", "시리얼/IMEI", "사용자", "위치", "상태", "핑", "최근 확인", "오프라인 시간", "카운트", "생성일" }
                };
                
                string[] h = headers[index];
                if (dgvMonitor.Columns.Contains("colIndex")) dgvMonitor.Columns["colIndex"].HeaderText = h[0];
                if (dgvMonitor.Columns.Contains("colDeviceGroup")) dgvMonitor.Columns["colDeviceGroup"].HeaderText = h[1];
                if (dgvMonitor.Columns.Contains("colDeviceName")) dgvMonitor.Columns["colDeviceName"].HeaderText = h[2];
                if (dgvMonitor.Columns.Contains("colImage")) dgvMonitor.Columns["colImage"].HeaderText = h[3];
                if (dgvMonitor.Columns.Contains("colIpAddress")) dgvMonitor.Columns["colIpAddress"].HeaderText = h[4];
                if (dgvMonitor.Columns.Contains("colMacAddress")) dgvMonitor.Columns["colMacAddress"].HeaderText = h[5];
                if (dgvMonitor.Columns.Contains("colSerial")) dgvMonitor.Columns["colSerial"].HeaderText = h[6];
                if (dgvMonitor.Columns.Contains("colUser")) dgvMonitor.Columns["colUser"].HeaderText = h[7];
                if (dgvMonitor.Columns.Contains("colLocation")) dgvMonitor.Columns["colLocation"].HeaderText = h[8];
                if (dgvMonitor.Columns.Contains("colStatus")) dgvMonitor.Columns["colStatus"].HeaderText = h[9];
                if (dgvMonitor.Columns.Contains("colLatency")) dgvMonitor.Columns["colLatency"].HeaderText = h[10];
                if (dgvMonitor.Columns.Contains("colLastCheck")) dgvMonitor.Columns["colLastCheck"].HeaderText = h[11];
                if (dgvMonitor.Columns.Contains("colLastOffline")) dgvMonitor.Columns["colLastOffline"].HeaderText = h[12];
                if (dgvMonitor.Columns.Contains("colOfflineDays")) dgvMonitor.Columns["colOfflineDays"].HeaderText = h[13];
                if (dgvMonitor.Columns.Contains("colCreated")) dgvMonitor.Columns["colCreated"].HeaderText = h[14];
            }
            
            UpdateSidebar();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string query = txtSearch.Text.Trim().ToLower();
            FilterGridBySearch(query);
        }

        private void FilterGridBySearch(string query)
        {
            dgvMonitor.Rows.Clear();
            int idx = 1;

            var filteredList = string.IsNullOrEmpty(query)
                ? ipList
                : ipList.Where(ip =>
                    ip.IpAddress.ToLower().Contains(query) ||
                    ip.DeviceName.ToLower().Contains(query) ||
                    ip.Location.ToLower().Contains(query) ||
                    ip.User.ToLower().Contains(query) ||
                    ip.DeviceGroup.ToLower().Contains(query) ||
                    ip.Serial.ToLower().Contains(query) ||
                    ip.Status.ToLower().Contains(query)
                ).ToList();

            foreach (var ip in filteredList)
            {
                Image img = null;
                if (!string.IsNullOrEmpty(ip.ImagePath) && File.Exists(ip.ImagePath))
                {
                    try { img = Image.FromFile(ip.ImagePath); } catch { img = null; }
                }

                // Calculate offline days
                string offlineDays = "-";
                if (ip.Status == "Offline" && ip.LastOfflineTime.HasValue)
                {
                    offlineDays = FormatOfflineDuration(DateTime.Now - ip.LastOfflineTime.Value);
                }

                dgvMonitor.Rows.Add(
                    idx++,
                    ip.DeviceGroup, ip.DeviceName, img,
                    ip.IpAddress, ip.MacAddress,
                    ip.Serial, ip.User, ip.Location,
                    ip.Status, ip.Latency > 0 ? $"{ip.Latency}ms" : "-",
                    ip.LastCheckTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    ip.LastOfflineTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-",
                    offlineDays,
                    ip.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
                );
            }
        }

        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("📋 Sao chép IP", null, (s, e) => CopySelectedIp());
            contextMenu.Items.Add("✏️ Sửa thiết bị", null, (s, e) => EditSelectedDevice());
            contextMenu.Items.Add("🔄 Ping ngay", null, (s, e) => PingSelectedDevice());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("🗑️ Xóa thiết bị", null, (s, e) => BtnRemove_Click(null, null));
            
            dgvMonitor.ContextMenuStrip = contextMenu;
        }

        private void CopySelectedIp()
        {
            if (dgvMonitor.SelectedRows.Count > 0)
            {
                var ip = dgvMonitor.SelectedRows[0].Cells["colIpAddress"].Value?.ToString();
                if (!string.IsNullOrEmpty(ip))
                {
                    Clipboard.SetText(ip);
                    MessageBox.Show($"Đã sao chép: {ip}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void EditSelectedDevice()
        {
            if (dgvMonitor.SelectedRows.Count > 0)
            {
                var ip = dgvMonitor.SelectedRows[0].Cells["colIpAddress"].Value?.ToString();
                if (!string.IsNullOrEmpty(ip)) ShowEditDeviceForm(ip);
            }
        }

        private async void PingSelectedDevice()
        {
            if (dgvMonitor.SelectedRows.Count > 0)
            {
                var ipAddr = dgvMonitor.SelectedRows[0].Cells["colIpAddress"].Value?.ToString();
                var monitor = ipList.FirstOrDefault(x => x.IpAddress == ipAddr);
                if (monitor != null)
                {
                    try
                    {
                        using var ping = new Ping();
                        var reply = await ping.SendPingAsync(monitor.IpAddress, settings.PingTimeoutMs);
                        monitor.UpdateStatus(reply.Status == IPStatus.Success, reply.RoundtripTime);
                        db.UpdateIpMonitor(monitor);
                        UpdateDataGridView();
                    }
                    catch { }
                }
            }
        }

        private void InitializeKeyboardShortcuts()
        {
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.N) { BtnAdd_Click(null, null); e.Handled = true; }
            else if (e.KeyCode == Keys.Delete) { BtnRemove_Click(null, null); e.Handled = true; }
            else if (e.KeyCode == Keys.F5) { _ = RefreshAllPings(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.F) { txtSearch?.Focus(); e.Handled = true; }
        }

        private void InitializeSidebar()
        {
            // Header Sidebar
            var panelHeader = new Panel { Dock = DockStyle.Top, Height = 55 }; // Tăng height
            
            lblSidebarTitle = new Label
            {
                Text = "LỌC NGƯỜI DÙNG",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = COLOR_PRIMARY,
                AutoSize = true,
                Location = new Point(10, 18) // Căn chỉnh lại vị trí
            };
            panelHeader.Controls.Add(lblSidebarTitle);

            btnClearFilter = new Button
            {
                Text = "Hủy lọc",
                Size = new Size(80, 26),
                Location = new Point(145, 14),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Default,
                BackColor = Color.LightGray,
                ForeColor = Color.DarkGray,
                Enabled = false
            };
            btnClearFilter.FlatAppearance.BorderSize = 0;
            
            // Bo tròn
            btnClearFilter.Paint += (s, ev) => 
            {
                var btn = (Button)s;
                var g = ev.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    int r = 12;
                    var rect = btn.ClientRectangle;
                    path.AddArc(rect.X, rect.Y, r, r, 180, 90);
                    path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                    path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                    path.CloseFigure();
                    btn.Region = new Region(path);
                }
            };

            btnClearFilter.Click += (s, e) => { if (lbUsers.Items.Count > 0) lbUsers.SelectedIndex = 0; };
            panelHeader.Controls.Add(btnClearFilter);

            btnToggleSidebar = CreateButton("<<", Color.Transparent, new Point(200, 5), new Size(30, 30));
            btnToggleSidebar.ForeColor = COLOR_TEXT_MUTED;
            btnToggleSidebar.FlatStyle = FlatStyle.Flat;
            btnToggleSidebar.FlatAppearance.BorderSize = 0;
            btnToggleSidebar.Cursor = Cursors.Hand;
            btnToggleSidebar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnToggleSidebar.Click += (s, e) => 
            {
                splitContainer.Panel1Collapsed = !splitContainer.Panel1Collapsed;
            };
            panelHeader.Controls.Add(btnToggleSidebar);
            
            panelSidebar.Controls.Add(panelHeader);

            // List Users
            lbUsers = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F),
                ItemHeight = 60,
                BackColor = Color.White,
                ForeColor = COLOR_TEXT_MAIN,
                Cursor = Cursors.Hand,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            lbUsers.DrawItem += LbUsers_DrawItem;
            lbUsers.SelectedIndexChanged += LbUsers_SelectedIndexChanged;
            panelSidebar.Controls.Add(lbUsers);

            // Footer - Author Info
            // Footer - Author Info
            var panelFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100, // Increased height for logo
                BackColor = COLOR_SURFACE,
                Padding = new Padding(5)
            };

            // Logo
            var pbLogo = new PictureBox
            {
                Name = "pbLogo",
                Dock = DockStyle.Top,
                Height = 40,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = COLOR_SURFACE
            };
            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo_company.png");
                if (File.Exists(logoPath)) pbLogo.Image = Image.FromFile(logoPath);
            }
            catch { }
            panelFooter.Controls.Add(pbLogo);

            // Author Label
            lblAuthor = new Label
            {
                Text = "Phiên bản V2.0\nViết bởi Phạm Huy Hiệu\nBộ phận Thiết bị SUB - SR TECH",
                ForeColor = COLOR_TEXT_MUTED,
                Font = new Font("Segoe UI", 8F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter // Center align looks better with logo
            };
            panelFooter.Controls.Add(lblAuthor);
            lblAuthor.BringToFront(); // Ensure text is below logo (Dock Fill takes remaining space)
            
            panelSidebar.Controls.Add(panelFooter);

            UpdateSidebar();
        }

        private void LbUsers_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            
            e.DrawBackground();
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            
            using (var brush = new SolidBrush(isSelected ? COLOR_SELECTION : COLOR_SURFACE))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            
            // Draw separator line
            using (var pen = new Pen(COLOR_BORDER))
            {
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            string rawText = lbUsers.Items[e.Index].ToString();
            // ... (rest of parsing logic) ...
            string userName = rawText;
            if (userName.Contains("(")) userName = userName.Substring(0, userName.LastIndexOf("(")).Trim();
            
            // Calculate stats
            var filtered = string.IsNullOrEmpty(userName) || userName == "Tất cả"
                ? ipList 
                : ipList.Where(ip => ip.User == userName).ToList();
                
            int total = filtered.Count;
            int online = filtered.Count(x => x.Status == "Online");
            int offline = filtered.Count(x => x.Status == "Offline" || x.Status == "Pending");

            // Draw Name
            string displayName = string.IsNullOrEmpty(userName) ? "Tất cả" : userName;
            var nameFont = new Font("Segoe UI", 11F, FontStyle.Bold);
            using (var brush = new SolidBrush(isSelected ? COLOR_PRIMARY : COLOR_TEXT_MAIN))
            {
                e.Graphics.DrawString(displayName, nameFont, brush, e.Bounds.X + 10, e.Bounds.Y + 5);
            }

            // Draw Stats
            int x = e.Bounds.X + 10;
            int y = e.Bounds.Y + 30;
            
            DrawBadge(e.Graphics, $"Tổng: {total}", COLOR_BORDER, COLOR_TEXT_MAIN, x, y);
            x += 70;
            DrawBadge(e.Graphics, $"{online}", COLOR_ONLINE_BG, COLOR_ONLINE_TEXT, x, y); // Online count
            x += 35;
            DrawBadge(e.Graphics, $"{offline}", COLOR_OFFLINE_BG, COLOR_OFFLINE_TEXT, x, y);   // Offline count
            
            e.DrawFocusRectangle();
        }

        private void DrawBadge(Graphics g, string text, Color bg, Color fg, int x, int y)
        {
            var font = new Font("Segoe UI", 8F);
            var size = g.MeasureString(text, font);
            var rect = new Rectangle(x, y, (int)size.Width + 10, (int)size.Height + 2);
            
            using (var brush = new SolidBrush(bg)) g.FillRectangle(brush, rect);
            using (var brush = new SolidBrush(fg)) g.DrawString(text, font, brush, x + 5, y + 1);
        }

        private void LbUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateClearFilterButtonState();
            
            if (lbUsers.SelectedItem == null) return;
            string selected = lbUsers.SelectedItem.ToString();
            
            // Parse User name from "Name (Count)"
            string userFilter = "";
            if (selected.Contains("("))
            {
                userFilter = selected.Substring(0, selected.LastIndexOf("(")).Trim();
            }

            if (userFilter == "Tất cả") userFilter = "";
            
            FilterGrid(userFilter);
        }

        private void UpdateClearFilterButtonState()
        {
            if (btnClearFilter == null) return;
            
            bool isFiltering = lbUsers.SelectedIndex > 0; // 0 == "Tất cả"
            
            if (isFiltering)
            {
                btnClearFilter.Enabled = true;
                btnClearFilter.BackColor = Color.Crimson; 
                btnClearFilter.ForeColor = Color.White;
                btnClearFilter.Cursor = Cursors.Hand;
            }
            else
            {
                btnClearFilter.Enabled = false;
                btnClearFilter.BackColor = Color.LightGray;
                btnClearFilter.ForeColor = Color.DarkGray;
                btnClearFilter.Cursor = Cursors.Default;
            }
        }

        private void FilterGrid(string user)
        {
            dgvMonitor.Rows.Clear();
            int idx = 1;

            var filteredList = string.IsNullOrEmpty(user) 
                ? ipList 
                : (user == "Trống" 
                    ? ipList.Where(ip => string.IsNullOrWhiteSpace(ip.User)).ToList() 
                    : ipList.Where(ip => ip.User.Equals(user, StringComparison.OrdinalIgnoreCase)).ToList());

            foreach (var ip in filteredList)
            {
                Image img = null;
                if (!string.IsNullOrEmpty(ip.ImagePath) && File.Exists(ip.ImagePath))
                {
                    try { img = Image.FromFile(ip.ImagePath); } catch { img = null; }
                }

                // Calculate offline days
                string offlineDays = "-";
                if (ip.Status == "Offline" && ip.LastOfflineTime.HasValue)
                {
                    var span = DateTime.Now - ip.LastOfflineTime.Value;
                    if (span.TotalDays >= 1) offlineDays = $"{span.TotalDays:0.#} ngày";
                    else if (span.TotalHours >= 1) offlineDays = $"{span.TotalHours:0.#} giờ";
                    else offlineDays = $"{span.TotalMinutes:0} phút";
                }

                dgvMonitor.Rows.Add(
                    idx++,
                    string.IsNullOrEmpty(ip.DeviceGroup) ? "Chưa phân loại" : ip.DeviceGroup, 
                    ip.DeviceName, img,
                    ip.IpAddress, ip.MacAddress,
                    ip.Serial, ip.User, ip.Location,
                    ip.Status, ip.Latency > 0 ? $"{ip.Latency}ms" : "-",
                    ip.LastCheckTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    ip.LastOfflineTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-",
                    offlineDays,
                    ip.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
                );
            }
        }

        private void UpdateSidebar()
        {
            // Save selected user
            string selectedUser = null;
            if (lbUsers.SelectedItem != null)
            {
                string s = lbUsers.SelectedItem.ToString();
                if (s.Contains("(")) 
                    selectedUser = s.Substring(0, s.LastIndexOf("(")).Trim();
            }

            // Get unique users and counts
            var userCounts = ipList
                .GroupBy(ip => string.IsNullOrWhiteSpace(ip.User) ? "Trống" : ip.User)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderBy(x => x.Name == "Trống" ? 0 : 1)
                .ThenBy(x => x.Name)
                .ToList();

            lbUsers.Items.Clear();
            lbUsers.Items.Add($"Tất cả ({ipList.Count})");
            
            foreach (var u in userCounts)
            {
                lbUsers.Items.Add($"{u.Name} ({u.Count})");
            }

            // Restore selection without triggering event loop
            lbUsers.SelectedIndexChanged -= LbUsers_SelectedIndexChanged;
            if (selectedUser != null)
            {
                for (int i = 0; i < lbUsers.Items.Count; i++)
                {
                    string item = lbUsers.Items[i].ToString();
                    string name = item;
                    if (item.Contains("(")) 
                        name = item.Substring(0, item.LastIndexOf("(")).Trim();
                    
                    if (string.Equals(name, selectedUser, StringComparison.OrdinalIgnoreCase))
                    {
                        lbUsers.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                if (lbUsers.Items.Count > 0) lbUsers.SelectedIndex = 0; // Default Select All if nothing selected
            }
            lbUsers.SelectedIndexChanged += LbUsers_SelectedIndexChanged;
        }
        private void InitializeTopBar()
        {
            // ========== HÀNG 1: HEADER (Minimalist) ==========
            var panelTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = COLOR_SURFACE,
                Padding = new Padding(20, 0, 20, 0)
            };
            this.Controls.Add(panelTitle);

            // Separator line
            var border = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = COLOR_BORDER };
            panelTitle.Controls.Add(border);

            // Icon before title
            var iconPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "icon.png");
            if (File.Exists(iconPath))
            {
                var picIcon = new PictureBox
                {
                    Size = new Size(40, 40),
                    Location = new Point(15, 10),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = Image.FromFile(iconPath)
                };
                panelTitle.Controls.Add(picIcon);
            }

            var lblTitle = new Label
            {
                Text = "Ping Monitor",
                ForeColor = COLOR_PRIMARY,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(60, 12) // Shifted right for icon
            };
            this.lblTitle = lblTitle; // Assign to field
            panelTitle.Controls.Add(lblTitle);

            // Subtitle or version
            var lblSub = new Label
            {
                Text = "Phần mềm theo dõi IP thiết bị",
                ForeColor = COLOR_TEXT_MUTED,
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(64, 42) // Aligned with title
            };
            this.lblSubLabel = lblSub; // Assign to field
            panelTitle.Controls.Add(lblSub);

            // Language Label
            var lblLang = new Label
            {
                Text = "Language 🌐",
                ForeColor = COLOR_TEXT_MUTED,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(panelTitle.Width - 215, 21),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            panelTitle.Controls.Add(lblLang);

            // Language Switcher
            cboLanguage = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Width = 140,
                Height = 25,
                BackColor = COLOR_BACKGROUND,
                ForeColor = COLOR_TEXT_MAIN,
                Cursor = Cursors.Hand,
                DrawMode = DrawMode.OwnerDrawFixed, // Custom Draw
                ItemHeight = 20
            };
            cboLanguage.Items.AddRange(new[] { "Tiếng Việt", "English", "한국어" });
            cboLanguage.SelectedIndex = 0; // VN default
            cboLanguage.Location = new Point(panelTitle.Width - 145, 18);
            cboLanguage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cboLanguage.SelectedIndexChanged += (s, e) => ApplyLanguage(cboLanguage.SelectedIndex);
            cboLanguage.DrawItem += CboLanguage_DrawItem;
            panelTitle.Controls.Add(cboLanguage);
        }

        private void CboLanguage_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var combo = sender as ComboBox;
            
            e.DrawBackground();

            // Draw Flag
            string flagFile = "";
            if (e.Index == 0) flagFile = "flag_vn.png";
            else if (e.Index == 1) flagFile = "flag_us.png";
            else if (e.Index == 2) flagFile = "flag_kr.png";

            string path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Resources", flagFile);
            if (File.Exists(path))
            {
                using (var img = Image.FromFile(path))
                {
                    e.Graphics.DrawImage(img, new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 2, 24, 16));
                }
            }

            // Draw Text
            using (var brush = new SolidBrush(combo.ForeColor))
            {
                e.Graphics.DrawString(combo.Items[e.Index].ToString(), combo.Font, brush, new Point(e.Bounds.X + 30, e.Bounds.Y + 2));
            }
            // e.DrawFocusRectangle(); // Optional
        }

        private void InitializeStatsBar(Control parent)
        {
            // ========== HÀNG 1: STATS + TOOLBAR (Merged) ==========
            var panelStats = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = COLOR_BACKGROUND,
                Padding = new Padding(10, 5, 10, 5)
            };
            parent.Controls.Add(panelStats);

            // LEFT: Stats Cards
            int x = 10;
            
            // Total card
            var pTotal = new Panel { Width = 180, Height = 55, Location = new Point(x, 8), BackColor = COLOR_SURFACE };
            pTotal.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 4, BackColor = COLOR_PRIMARY });
            lblTotalTitle = new Label { Text = "TỔNG THIẾT BỊ", ForeColor = COLOR_TEXT_MUTED, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Location = new Point(12, 6), AutoSize = true };
            pTotal.Controls.Add(lblTotalTitle);
            lblTotal = new Label { Text = "0", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = COLOR_TEXT_MAIN, Location = new Point(12, 24), AutoSize = true };
            pTotal.Controls.Add(lblTotal);
            panelStats.Controls.Add(pTotal);
            x += 190;

            // Online card
            var pOnline = new Panel { Width = 150, Height = 55, Location = new Point(x, 8), BackColor = COLOR_SURFACE };
            pOnline.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 4, BackColor = COLOR_BTN_PING }); // Green
            lblOnlineTitle = new Label { Text = "ONLINE", ForeColor = COLOR_TEXT_MUTED, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Location = new Point(12, 6), AutoSize = true };
            pOnline.Controls.Add(lblOnlineTitle);
            lblOnline = new Label { Text = "0", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = COLOR_ONLINE_TEXT, Location = new Point(12, 24), AutoSize = true };
            pOnline.Controls.Add(lblOnline);
            panelStats.Controls.Add(pOnline);
            x += 160;

            // Offline card
            var pOffline = new Panel { Width = 150, Height = 55, Location = new Point(x, 8), BackColor = COLOR_SURFACE };
            pOffline.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 4, BackColor = COLOR_BTN_DELETE }); // Red
            lblOfflineTitle = new Label { Text = "OFFLINE", ForeColor = COLOR_TEXT_MUTED, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Location = new Point(12, 6), AutoSize = true };
            pOffline.Controls.Add(lblOfflineTitle);
            lblOffline = new Label { Text = "0", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = COLOR_OFFLINE_TEXT, Location = new Point(12, 24), AutoSize = true };
            pOffline.Controls.Add(lblOffline);
            panelStats.Controls.Add(pOffline);
        }

        private Button CreateButton(string text, Color bgColor, Point location, Size size)
        {
            var btn = new Button
            {
                Text = text, Size = size, Location = location,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.White, BackColor = bgColor,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bgColor, 0.2f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(bgColor, 0.1f);
            return btn;
        }

        // ============================================
        // DATAGRIDVIEW
        // ============================================
        private void InitializeDataGridView(Control parent)
        {
            dgvMonitor = new DataGridView
            {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.None,
                BackgroundColor = COLOR_BACKGROUND, GridColor = COLOR_BORDER,
                RowHeadersVisible = false, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                MultiSelect = true, Font = new Font("Segoe UI", 9.5F),
                RowTemplate = { Height = 45 }, ScrollBars = ScrollBars.Both, // Taller rows
                AllowUserToResizeColumns = true,
                AllowUserToResizeRows = false, // Disable row resizing
                EnableHeadersVisualStyles = false
            };

            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, dgvMonitor, new object[] { true });

            // Header Style
            dgvMonitor.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = COLOR_PRIMARY, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(0)
            };
            dgvMonitor.ColumnHeadersHeight = 45;
            dgvMonitor.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvMonitor.EnableHeadersVisualStyles = false; // Use custom header style

            // Row Style
            dgvMonitor.GridColor = Color.FromArgb(220, 220, 220); // Màu viền xám rõ ràng
            dgvMonitor.BackgroundColor = Color.White;
            dgvMonitor.CellBorderStyle = DataGridViewCellBorderStyle.Single; // Kẻ bảng đầy đủ (dọc + ngang)
            dgvMonitor.RowTemplate.Height = 35; 
            
            dgvMonitor.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White, 
                ForeColor = Color.Black,
                SelectionBackColor = Color.FromArgb(200, 230, 255), 
                SelectionForeColor = Color.Black,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(0),
                Font = new Font("Segoe UI", 9F)
            };

            dgvMonitor.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(250, 250, 250) // Xen kẽ xám rất nhạt để dễ dò dòng
            };
            
            // Fix lỗi header bị trắng khi sort hoặc click
            dgvMonitor.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgvMonitor.ColumnHeadersDefaultCellStyle.BackColor;
            dgvMonitor.ColumnHeadersDefaultCellStyle.SelectionForeColor = dgvMonitor.ColumnHeadersDefaultCellStyle.ForeColor;

            dgvMonitor.CellFormatting += DgvMonitor_CellFormatting;

            // Custom painting for Status Status
            dgvMonitor.CellPainting += DgvMonitor_CellPainting;

            // Columns setup
            // Columns setup
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = "#", Width = 50 });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDeviceGroup", HeaderText = "Nhóm", Width = 100 });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDeviceName", HeaderText = "Tên thiết bị", Width = 150 });
            dgvMonitor.Columns.Add(new DataGridViewImageColumn { Name = "colImage", HeaderText = "Hình ảnh", Width = 60, ImageLayout = DataGridViewImageCellLayout.Zoom });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIpAddress", HeaderText = "IP", Width = 100 });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMacAddress", HeaderText = "MAC", Width = 110 });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSerial", HeaderText = "Serial/IMEI", Width = 130 });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUser", HeaderText = "Người dùng", Width = 130 });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLocation", HeaderText = "Vị trí", Width = 110 });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Trạng thái", Width = 120 }); // Wider for pill
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLatency", HeaderText = "Ping", Width = 50 });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLastCheck", HeaderText = "Time check", Width = 150 });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLastOffline", HeaderText = "Time Offline", Width = 150 });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOfflineDays", HeaderText = "Đếm", Width = 80 });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCreated", HeaderText = "Ngày tạo", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            dgvMonitor.CellDoubleClick += DgvMonitor_CellDoubleClick;
            dgvMonitor.ColumnHeaderMouseClick += DgvMonitor_ColumnHeaderMouseClick;

            parent.Controls.Add(dgvMonitor);
            dgvMonitor.BringToFront();

            // Progress bar - bottom right of grid
            progressBar = new ProgressBar
            {
                Size = new Size(120, 18),
                Style = ProgressBarStyle.Continuous,
                Visible = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            parent.Controls.Add(progressBar);
            // Position at bottom-right
            dgvMonitor.Resize += (s, e) => {
                progressBar.Location = new Point(
                    dgvMonitor.Right - progressBar.Width - 25,
                    dgvMonitor.Bottom - progressBar.Height - 10
                );
            };
            progressBar.BringToFront();
        }

        private void DgvMonitor_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Don't sort the # column or Image column
            if (e.ColumnIndex == 0 || e.ColumnIndex == 3) return;

            // Toggle sort direction if same column clicked
            if (sortColumnIndex == e.ColumnIndex)
                sortAscending = !sortAscending;
            else
            {
                sortColumnIndex = e.ColumnIndex;
                sortAscending = true;
            }

            // Sort ipList based on column
            switch (e.ColumnIndex)
            {
                case 1: ipList = sortAscending ? ipList.OrderBy(x => x.DeviceGroup).ToList() : ipList.OrderByDescending(x => x.DeviceGroup).ToList(); break;
                case 2: ipList = sortAscending ? ipList.OrderBy(x => x.DeviceName).ToList() : ipList.OrderByDescending(x => x.DeviceName).ToList(); break;
                case 4: ipList = sortAscending ? ipList.OrderBy(x => x.IpAddress).ToList() : ipList.OrderByDescending(x => x.IpAddress).ToList(); break;
                case 5: ipList = sortAscending ? ipList.OrderBy(x => x.MacAddress).ToList() : ipList.OrderByDescending(x => x.MacAddress).ToList(); break;
                case 6: ipList = sortAscending ? ipList.OrderBy(x => x.Serial).ToList() : ipList.OrderByDescending(x => x.Serial).ToList(); break;
                case 7: ipList = sortAscending ? ipList.OrderBy(x => x.User).ToList() : ipList.OrderByDescending(x => x.User).ToList(); break;
                case 8: ipList = sortAscending ? ipList.OrderBy(x => x.Location).ToList() : ipList.OrderByDescending(x => x.Location).ToList(); break;
                case 9: ipList = sortAscending ? ipList.OrderBy(x => x.Status).ToList() : ipList.OrderByDescending(x => x.Status).ToList(); break;
                case 10: ipList = sortAscending ? ipList.OrderBy(x => x.Latency).ToList() : ipList.OrderByDescending(x => x.Latency).ToList(); break;
                case 11: ipList = sortAscending ? ipList.OrderBy(x => x.LastCheckTime).ToList() : ipList.OrderByDescending(x => x.LastCheckTime).ToList(); break;
                case 12: ipList = sortAscending ? ipList.OrderBy(x => x.LastOfflineTime).ToList() : ipList.OrderByDescending(x => x.LastOfflineTime).ToList(); break;
                case 13: ipList = sortAscending ? ipList.OrderBy(x => x.CreatedAt).ToList() : ipList.OrderByDescending(x => x.CreatedAt).ToList(); break;
            }

            // Refresh grid
            UpdateDataGridView();

            // Update header to show sort direction
            foreach (DataGridViewColumn col in dgvMonitor.Columns)
            {
                col.HeaderCell.SortGlyphDirection = SortOrder.None;
            }
            dgvMonitor.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = sortAscending ? SortOrder.Ascending : SortOrder.Descending;
        }

        private void DgvMonitor_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // 1. Draw Cell Background & Content (Standard) but EXCLUDING border if we want to force it
            e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Border);

            // 2. Custom Drawing for Status (Pill)
            if (dgvMonitor.Columns[e.ColumnIndex].Name == "colStatus" && e.Value != null)
            {
                // We typically need to redraw the background for the pill area or rely on step 1
                // For safety, let's just draw the pill on top of whatever standard paint did.

                string status = e.Value.ToString();
                bool isOnline = status == "Online";
                
                Color backColor = isOnline ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
                Color textColor = Color.White;

                // Adjust for Pending
                if (status == "Pending") { backColor = Color.Gray; }

                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                var rect = e.CellBounds;
                rect.Inflate(-15, -10); // Shrink

                using (var brush = new SolidBrush(backColor))
                {
                    // Draw Rounded Rect manually or via helper
                     g.FillEllipse(brush, new Rectangle(rect.X, rect.Y, rect.Height, rect.Height));
                     g.FillEllipse(brush, new Rectangle(rect.Right - rect.Height, rect.Y, rect.Height, rect.Height));
                     g.FillRectangle(brush, new Rectangle(rect.X + rect.Height / 2, rect.Y, rect.Width - rect.Height, rect.Height));
                }

                TextRenderer.DrawText(g, status.ToUpper(), 
                    new Font("Segoe UI", 8F, FontStyle.Bold), 
                    rect, textColor, 
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            // 3. FORCE DRAW BOTTOM BORDER (To fix "blur" issue on selection)
            using (var p = new Pen(dgvMonitor.GridColor, 1))
            {
                e.Graphics.DrawLine(p, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }

            e.Handled = true; // We handled everything (Paint called manually above)
        }


        private void DgvMonitor_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var ip = dgvMonitor.Rows[e.RowIndex].Cells["colIpAddress"].Value?.ToString();
            if (!string.IsNullOrEmpty(ip)) ShowEditDeviceForm(ip);
        }


        // Helper sinh màu pastel từ string
        private Color GetColorFromString(string text)
        {
            if (string.IsNullOrEmpty(text)) return Color.White;
            string lower = text.ToLower().Trim();

            // Mapping màu cố định cho các nhóm thiết bị (Theo yêu cầu)
            if (lower.Contains("điện thoại") || lower.Contains("phone") || lower.Contains("mobile"))
                return Color.FromArgb(255, 228, 225); // MistyRose
            
            if (lower.Contains("máy tính") || lower.Contains("pc") || lower.Contains("desktop"))
                return Color.FromArgb(224, 255, 255); // LightCyan
            
            if (lower.Contains("laptop") || lower.Contains("notebook"))
                return Color.FromArgb(176, 224, 230); // PowderBlue
            
            if (lower.Contains("router") || lower.Contains("gateway") || lower.Contains("modem"))
                return Color.FromArgb(152, 251, 152); // PaleGreen

            if (lower.Contains("switch") || lower.Contains("hub"))
                return Color.FromArgb(144, 238, 144); // LightGreen

            if (lower.Contains("camera") || lower.Contains("cctv") || lower.Contains("cam"))
                return Color.FromArgb(230, 230, 250); // Lavender

            if (lower.Contains("server") || lower.Contains("máy chủ"))
                return Color.FromArgb(255, 228, 196); // Bisque

            if (lower.Contains("printer") || lower.Contains("máy in"))
                return Color.FromArgb(255, 250, 205); // LemonChiffon

            if (lower.Contains("wifi") || lower.Contains("access point") || lower.Contains("ap"))
                return Color.FromArgb(224, 255, 255); // Azure

            if (lower.Contains("chưa phân loại") || lower.Contains("khác") || lower.Contains("other"))
                return Color.FromArgb(245, 245, 245); // WhiteSmoke

            // Fallback: Hash nhẹ nhàng
            int hash = Math.Abs(text.GetHashCode());
            Color[] colors = new Color[] 
            {
                Color.FromArgb(245, 245, 245), 
                Color.FromArgb(255, 245, 238),
                Color.FromArgb(240, 255, 240) 
            };
            return colors[hash % colors.Length];
        }

        private void DgvMonitor_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // Tô màu cột Nhóm
            if (dgvMonitor.Columns[e.ColumnIndex].Name == "colDeviceGroup" && e.Value != null)
            {
                string groupName = e.Value.ToString();
                e.CellStyle.BackColor = GetColorFromString(groupName);
                e.CellStyle.SelectionBackColor = e.CellStyle.BackColor; // Giữ nguyên màu nền khi select để tránh lẫn màu
                e.CellStyle.SelectionForeColor = Color.Black;
            }
        }

        private void InitializeTimer()
        {
            refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = settings.RefreshIntervalSeconds * 1000
            };
            refreshTimer.Tick += async (s, e) => await RefreshAllPings();
            if (settings.AutoRefresh) refreshTimer.Start();
        }

        // ============================================
        // HELPER: OFFLINE DURATION FORMAT
        // ============================================
        private string FormatOfflineDuration(TimeSpan span)
        {
            int lang = currentLanguageIndex;
            string[] tMinutes = { "phút", "minutes", "분" };
            string[] tHours = { "giờ", "hours", "시간" };
            string[] tDays = { "ngày", "days", "일" };

            if (span.TotalDays >= 1) return $"{span.TotalDays:0.#} {tDays[lang]}";
            if (span.TotalHours >= 1) return $"{span.TotalHours:0.#} {tHours[lang]}";
            return $"{span.TotalMinutes:0} {tMinutes[lang]}";
        }

        // ============================================
        // SETTINGS
        // ============================================
        private void BtnSettings_Click(object sender, EventArgs e)
        {
            // Translations for Settings form
            int lang = currentLanguageIndex;
            string[] tFormTitle = { "⚙ Cài đặt", "⚙ Settings", "⚙ 설정" };
            string[] tRefreshLabel = { "Refresh interval (giây):", "Refresh interval (seconds):", "새로고침 간격 (초):" };
            string[] tTimeoutLabel = { "Ping timeout (ms):", "Ping timeout (ms):", "핑 타임아웃 (ms):" };
            string[] tConcurrentLabel = { "Số ping đồng thời:", "Concurrent pings:", "동시 핑 수:" };
            string[] tRetryLabel = { "Số lần thử ping (Retry):", "Ping retry count:", "핑 재시도 횟수:" };
            string[] tOfflineThresholdLabel = { "Ngưỡng mất kết nối (lần):", "Offline threshold (count):", "오프라인 임계값 (회):" };
            string[] tResetBtn = { "Khôi phục mặc định", "Reset to Default", "기본값 복원" };
            string[] tSaveBtn = { "Lưu", "Save", "저장" };
            string[] tCancelBtn = { "Hủy", "Cancel", "취소" };
            string[] tResetConfirm = { "Khôi phục các thông số về mặc định?\n(Refresh=60s, Timeout=1500ms, Concurrent=50, Retry=10, Threshold=5)", 
                                        "Reset all settings to default?\n(Refresh=60s, Timeout=1500ms, Concurrent=50, Retry=10, Threshold=5)", 
                                        "모든 설정을 기본값으로 복원하시겠습니까?\n(Refresh=60s, Timeout=1500ms, Concurrent=50, Retry=10, Threshold=5)" };
            string[] tConfirmTitle = { "Xác nhận", "Confirm", "확인" };
            string[] tSavedMsg = { "Đã lưu cài đặt!", "Settings saved!", "설정이 저장되었습니다!" };
            string[] tInfoTitle = { "Thông báo", "Information", "알림" };

            using (var form = new Form())
            {
                form.Text = tFormTitle[lang];
                form.Size = new Size(420, 480);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.BackColor = COLOR_BACKGROUND;

                int y = 20;

                form.Controls.Add(new Label { Text = tRefreshLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y), AutoSize = true });
                y += 25;
                var numRefresh = new NumericUpDown { Location = new Point(20, y), Size = new Size(360, 30), Minimum = 1, Maximum = 300, Value = settings.RefreshIntervalSeconds, BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN };
                form.Controls.Add(numRefresh);
                y += 40;

                form.Controls.Add(new Label { Text = tTimeoutLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y), AutoSize = true });
                y += 25;
                var numTimeout = new NumericUpDown { Location = new Point(20, y), Size = new Size(360, 30), Minimum = 100, Maximum = 10000, Value = settings.PingTimeoutMs, Increment = 100, BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN };
                form.Controls.Add(numTimeout);
                y += 40;

                form.Controls.Add(new Label { Text = tConcurrentLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y), AutoSize = true });
                y += 25;
                var numConcurrent = new NumericUpDown { Location = new Point(20, y), Size = new Size(360, 30), Minimum = 1, Maximum = 500, Value = settings.MaxConcurrentPings, BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN };
                form.Controls.Add(numConcurrent);
                y += 40;

                form.Controls.Add(new Label { Text = tRetryLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y), AutoSize = true });
                y += 25;
                var numRetry = new NumericUpDown { Location = new Point(20, y), Size = new Size(360, 30), Minimum = 1, Maximum = 20, Value = settings.PingRetryCount, BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN };
                form.Controls.Add(numRetry);
                y += 40;

                form.Controls.Add(new Label { Text = tOfflineThresholdLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y), AutoSize = true });
                y += 25;
                var numThreshold = new NumericUpDown { Location = new Point(20, y), Size = new Size(360, 30), Minimum = 1, Maximum = 20, Value = settings.OfflineThreshold, BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN };
                form.Controls.Add(numThreshold);
                y += 50;

                // Reset Button
                var btnReset = CreateButton(tResetBtn[lang], Color.IndianRed, new Point(20, y), new Size(135, 35));
                btnReset.Click += (s, ev) => 
                {
                    if (MessageBox.Show(tResetConfirm[lang], tConfirmTitle[lang], MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        numRefresh.Value = 60;
                        numTimeout.Value = 1500;
                        numConcurrent.Value = 50;
                        numRetry.Value = 10;
                        numThreshold.Value = 5;
                    }
                };
                form.Controls.Add(btnReset);

                var btnSave = CreateButton(tSaveBtn[lang], COLOR_PRIMARY, new Point(165, y), new Size(100, 35));
                btnSave.Click += (s, ev) =>
                {
                    settings.RefreshIntervalSeconds = (int)numRefresh.Value;
                    settings.PingTimeoutMs = (int)numTimeout.Value;
                    settings.MaxConcurrentPings = (int)numConcurrent.Value;
                    settings.PingRetryCount = (int)numRetry.Value;
                    settings.OfflineThreshold = (int)numThreshold.Value;
                    db.SaveSettings(settings);
                    refreshTimer.Interval = settings.RefreshIntervalSeconds * 1000;
                    MessageBox.Show(tSavedMsg[lang], tInfoTitle[lang]);
                    form.Close();
                };
                form.Controls.Add(btnSave);

                var btnCancel = CreateButton(tCancelBtn[lang], Color.FromArgb(80, 80, 80), new Point(275, y), new Size(100, 35));
                btnCancel.Click += (s, ev) => form.Close();
                form.Controls.Add(btnCancel);

                form.ShowDialog(this);
            }
        }

        // ============================================
        // VIEW LOGS
        // ============================================
        private void BtnViewLogs_Click(object sender, EventArgs e)
        {
            // Reset Notification
            unreadLogCount = 0;
            if (btnViewLogs != null) btnViewLogs.Invalidate();

            // Translations for Log Viewer
            int lang = currentLanguageIndex;
            string[] tFormTitle = { "Log ({0} dòng gần nhất)", "Log ({0} recent entries)", "로그 (최근 {0}개)" };
            string[] tColTime = { "Thời gian", "Time", "시간" };
            string[] tColDeviceName = { "Tên thiết bị", "Device Name", "장치 이름" };
            string[] tColIp = { "IP", "IP", "IP" };
            string[] tColUser = { "Người dùng", "User", "사용자" };
            string[] tColLoc = { "Vị trí", "Location", "위치" };
            string[] tColEvent = { "Sự kiện", "Event", "이벤트" };
            string[] tColDetails = { "Chi tiết", "Details", "상세" };
            string[] tClearBtn = { "Xóa tất cả Log", "Clear All Logs", "모든 로그 삭제" };
            string[] tClearConfirm = { "Xóa tất cả log?", "Delete all logs?", "모든 로그를 삭제하시겠습니까?" };
            string[] tConfirmTitle = { "Xác nhận", "Confirm", "확인" };

            var logs = db.GetLogs(500);

            using (var form = new Form())
            {
                form.Text = string.Format(tFormTitle[lang], logs.Count);
                form.Size = new Size(900, 600);
                form.StartPosition = FormStartPosition.CenterParent;
                form.BackColor = COLOR_BACKGROUND;

                var dgv = new DataGridView
                {
                    Dock = DockStyle.Fill, BorderStyle = BorderStyle.None,
                    BackgroundColor = COLOR_BACKGROUND, GridColor = Color.FromArgb(55, 55, 55),
                    RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true,
                    Font = new Font("Segoe UI", 9F), RowTemplate = { Height = 28 }
                };

                dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = COLOR_HEADER, ForeColor = COLOR_TEXT_MAIN,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                dgv.DefaultCellStyle = new DataGridViewCellStyle { BackColor = COLOR_SURFACE, ForeColor = COLOR_TEXT_MAIN };
                dgv.EnableHeadersVisualStyles = false;

                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTime", HeaderText = tColTime[lang], Width = 140 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDeviceName", HeaderText = tColDeviceName[lang], Width = 120 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIp", HeaderText = tColIp[lang], Width = 110 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUser", HeaderText = tColUser[lang], Width = 100 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLoc", HeaderText = tColLoc[lang], Width = 100 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEvent", HeaderText = tColEvent[lang], Width = 80 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDetails", HeaderText = tColDetails[lang], AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

                foreach (var log in logs)
                {
                    dgv.Rows.Add(
                        log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        log.DeviceName,
                        log.IpAddress, 
                        log.User,
                        log.Location, 
                        log.Event, 
                        log.Details
                    );
                }

                form.Controls.Add(dgv);

                var btnClear = CreateButton(tClearBtn[lang], COLOR_BTN_DELETE, new Point(10, 10), new Size(130, 30));
                btnClear.Click += (s, ev) =>
                {
                    if (MessageBox.Show(tClearConfirm[lang], tConfirmTitle[lang], MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        db.ClearLogs();
                        dgv.Rows.Clear();
                    }
                };

                var panel = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = COLOR_BACKGROUND };
                panel.Controls.Add(btnClear);
                form.Controls.Add(panel);

                form.ShowDialog(this);
            }
        }

        // ============================================
        // THÊM THIẾT BỊ - Dialog Form
        // ============================================
        private async void BtnAdd_Click(object sender, EventArgs e)
        {
            // Translations for Add Device form
            int lang = currentLanguageIndex;
            string[] tFormTitle = { "Thêm thiết bị mới", "Add New Device", "새 장치 추가" };
            string[] tGroupLabel = { "Nhóm thiết bị:", "Device Group:", "장치 그룹:" };
            string[] tNameLabel = { "Tên thiết bị:", "Device Name:", "장치 이름:" };
            string[] tImageLabel = { "Hình ảnh:", "Image:", "이미지:" };
            string[] tIpLabel = { "IP (*):", "IP (*):", "IP (*):" };
            string[] tMacLabel = { "MAC:", "MAC:", "MAC:" };
            string[] tSerialLabel = { "Serial/IMEI:", "Serial/IMEI:", "시리얼/IMEI:" };
            string[] tUserLabel = { "Người dùng:", "User:", "사용자:" };
            string[] tLocLabel = { "Vị trí:", "Location:", "위치:" };
            string[] tBrowseBtn = { "Chọn...", "Browse...", "찾아보기..." };
            string[] tSaveBtn = { "Lưu thiết bị", "Save Device", "장치 저장" };
            string[] tCancelBtn = { "Hủy", "Cancel", "취소" };
            string[] tIpError = { "Định dạng IP/Hostname không hợp lệ!", "Invalid IP/Hostname format!", "IP/호스트 이름 형식이 잘못되었습니다!" };
            string[] tIpExists = { "IP đã tồn tại!", "IP already exists!", "IP가 이미 존재합니다!" };
            string[] tErrorTitle = { "Lỗi", "Error", "오류" };
            string[] tInfoTitle = { "Thông báo", "Information", "알림" };

            using (var form = new Form())
            {
                form.Text = tFormTitle[lang];
                form.Size = new Size(500, 420);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.BackColor = COLOR_BACKGROUND;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                int y = 20;
                int inputX = 140;
                int inputWidth = 320;

                // Nhóm thiết bị
                form.Controls.Add(new Label { Text = tGroupLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var cboGroup = new ComboBox
                {
                    Location = new Point(inputX, y), Size = new Size(inputWidth, 25),
                    Font = new Font("Segoe UI", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN,
                    DropDownStyle = ComboBoxStyle.DropDown
                };
                cboGroup.Items.AddRange(new[] { "Điện thoại", "Máy tính", "Laptop", "Camera IP", "Khác" });
                form.Controls.Add(cboGroup);
                y += 35;

                // Tên thiết bị
                form.Controls.Add(new Label { Text = tNameLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtName = new TextBox
                {
                    Location = new Point(inputX, y), Size = new Size(inputWidth, 25),
                    Font = new Font("Segoe UI", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN
                };
                form.Controls.Add(txtName);
                y += 35;

                // Hình ảnh
                form.Controls.Add(new Label { Text = tImageLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtImage = new TextBox
                {
                    Location = new Point(inputX, y), Size = new Size(inputWidth - 80, 25),
                    Font = new Font("Segoe UI", 9F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN, ReadOnly = true
                };
                form.Controls.Add(txtImage);
                var btnBrowse = CreateButton(tBrowseBtn[lang], Color.FromArgb(80, 80, 80), new Point(inputX + inputWidth - 70, y), new Size(70, 25));
                btnBrowse.Click += (s, ev) =>
                {
                    using (var ofd = new OpenFileDialog())
                    {
                        ofd.Filter = "Images (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp";
                        if (ofd.ShowDialog() == DialogResult.OK)
                            txtImage.Text = ofd.FileName;
                    }
                };
                form.Controls.Add(btnBrowse);
                y += 35;

                // IP Address (bắt buộc)
                form.Controls.Add(new Label { Text = tIpLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtIp = new TextBox
                {
                    Location = new Point(inputX, y), Size = new Size(inputWidth, 25),
                    Font = new Font("Consolas", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN
                };
                form.Controls.Add(txtIp);
                y += 35;

                // MAC Address
                form.Controls.Add(new Label { Text = tMacLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtMac = new TextBox
                {
                    Location = new Point(inputX, y), Size = new Size(inputWidth, 25),
                    Font = new Font("Consolas", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN
                };
                txtMac.TextChanged += (s, ev) =>
                {
                    // Auto-format MAC address
                    string text = txtMac.Text.Replace(":", "").Replace("-", "").ToUpper();
                    if (text.Length > 12) text = text.Substring(0, 12);
                    if (text.Length > 0 && text.All(c => "0123456789ABCDEF".Contains(c)))
                    {
                        var formatted = string.Join(":", Enumerable.Range(0, text.Length / 2).Select(i => text.Substring(i * 2, 2)));
                        if (text.Length % 2 == 1) formatted += (formatted.Length > 0 ? ":" : "") + text.Last();
                        if (formatted != txtMac.Text)
                        {
                            int pos = txtMac.SelectionStart;
                            txtMac.Text = formatted;
                            txtMac.SelectionStart = Math.Min(pos + 1, formatted.Length);
                        }
                    }
                };
                form.Controls.Add(txtMac);
                y += 35;

                // Serial/IMEI
                form.Controls.Add(new Label { Text = tSerialLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtSerial = new TextBox
                {
                    Location = new Point(inputX, y), Size = new Size(inputWidth, 25),
                    Font = new Font("Segoe UI", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN
                };
                form.Controls.Add(txtSerial);
                y += 35;

                // User
                form.Controls.Add(new Label { Text = tUserLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtUser = new TextBox
                {
                    Location = new Point(inputX, y), Size = new Size(inputWidth, 25),
                    Font = new Font("Segoe UI", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN
                };
                form.Controls.Add(txtUser);
                y += 35;

                // Vị trí
                form.Controls.Add(new Label { Text = tLocLabel[lang], ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtLoc = new TextBox
                {
                    Location = new Point(inputX, y), Size = new Size(inputWidth, 25),
                    Font = new Font("Segoe UI", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN
                };
                form.Controls.Add(txtLoc);
                y += 50;

                // Buttons
                var btnSave = CreateButton(tSaveBtn[lang], COLOR_PRIMARY, new Point(140, y), new Size(120, 35));
                var btnCancel = CreateButton(tCancelBtn[lang], Color.FromArgb(80, 80, 80), new Point(280, y), new Size(100, 35));

                bool saved = false;
                btnSave.Click += (s, ev) =>
                {
                    string ip = txtIp.Text.Trim();
                    if (!ValidateIpAddress(ip, out string error))
                    {
                        MessageBox.Show(tIpError[lang], tErrorTitle[lang]); txtIp.Focus(); return;
                    }
                    if (ipList.Any(x => x.IpAddress.Equals(ip, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show(tIpExists[lang], tInfoTitle[lang]); return;
                    }

                    var monitor = new IpMonitor
                    {
                        DeviceGroup = cboGroup.Text.Trim(),
                        DeviceName = txtName.Text.Trim(),
                        ImagePath = txtImage.Text.Trim(),
                        IpAddress = ip,
                        MacAddress = txtMac.Text.Trim(),
                        Serial = txtSerial.Text.Trim(),
                        User = txtUser.Text.Trim(),
                        Location = txtLoc.Text.Trim()
                    };
                    ipList.Add(monitor);
                    db.AddIpMonitor(monitor);
                    AddLog(ip, monitor.Location, "Added", "Thêm thủ công", monitor.DeviceName, monitor.User);
                    saved = true;
                    form.Close();
                };
                btnCancel.Click += (s, ev) => form.Close();

                form.Controls.Add(btnSave);
                form.Controls.Add(btnCancel);
                form.AcceptButton = btnSave;

                form.ShowDialog(this);

                if (saved)
                {
                    UpdateDataGridView();
                    await RefreshAllPings();
                }
            }
        }

        // ============================================
        // EDIT DEVICE FORM
        // ============================================
        private void ShowEditDeviceForm(string currentIp)
        {
            var ip = ipList.FirstOrDefault(x => x.IpAddress == currentIp);
            if (ip == null) return;

            using (var form = new Form())
            {
                form.Text = $"Chỉnh sửa: {ip.DeviceName} ({ip.IpAddress})";
                form.Size = new Size(500, 420);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.BackColor = COLOR_BACKGROUND;
                form.MaximizeBox = false;

                int y = 20;
                int inputX = 140;
                int inputWidth = 320;
                
                void AddLabel(string text) => form.Controls.Add(new Label { Text = text, ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });

                // Group
                AddLabel("Nhóm thiết bị:");
                var cboGroup = new ComboBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN, DropDownStyle = ComboBoxStyle.DropDown };
                cboGroup.Items.AddRange(new[] { "Điện thoại", "Máy tính", "Laptop", "Camera IP", "Khác" });
                cboGroup.Text = ip.DeviceGroup;
                form.Controls.Add(cboGroup);
                y += 35;

                // Name
                AddLabel("Tên thiết bị:");
                var txtName = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN, Text = ip.DeviceName };
                form.Controls.Add(txtName);
                y += 35;

                // Image
                AddLabel("Hình ảnh:");
                var txtImage = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth - 80, 25), Font = new Font("Segoe UI", 9F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN, ReadOnly = true, Text = ip.ImagePath };
                form.Controls.Add(txtImage);
                var btnBrowse = CreateButton("...", Color.FromArgb(80, 80, 80), new Point(inputX + inputWidth - 70, y), new Size(70, 25));
                btnBrowse.Click += (s, ev) => { using (var ofd = new OpenFileDialog()) { ofd.Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"; if (ofd.ShowDialog() == DialogResult.OK) txtImage.Text = ofd.FileName; } };
                form.Controls.Add(btnBrowse);
                y += 35;

                // IP (ReadOnly)
                AddLabel("IP (*):");
                var txtIp = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Consolas", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN, ReadOnly = true, Text = ip.IpAddress };
                form.Controls.Add(txtIp);
                y += 35;

                // MAC
                AddLabel("MAC:");
                var txtMac = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Consolas", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN, Text = ip.MacAddress };
                form.Controls.Add(txtMac);
                y += 35;

                // Serial
                AddLabel("Serial/IMEI:");
                var txtSerial = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN, Text = ip.Serial };
                form.Controls.Add(txtSerial);
                y += 35;

                // User
                AddLabel("Người dùng:");
                var txtUser = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN, Text = ip.User };
                form.Controls.Add(txtUser);
                y += 35;

                // Location
                AddLabel("Vị trí:");
                var txtLoc = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = COLOR_BACKGROUND, ForeColor = COLOR_TEXT_MAIN, Text = ip.Location };
                form.Controls.Add(txtLoc);
                y += 45;

                var btnSave = CreateButton("Cập nhật", COLOR_PRIMARY, new Point(140, y), new Size(100, 35));
                btnSave.Click += (s, ev) => {
                    ip.DeviceGroup = cboGroup.Text.Trim();
                    ip.DeviceName = txtName.Text.Trim();
                    ip.ImagePath = txtImage.Text.Trim();
                    ip.MacAddress = txtMac.Text.Trim();
                    ip.Serial = txtSerial.Text.Trim();
                    ip.User = txtUser.Text.Trim();
                    ip.Location = txtLoc.Text.Trim();
                    db.UpdateIpMonitor(ip);
                    UpdateDataGridView();
                    form.Close();
                };
                form.Controls.Add(btnSave);
                
                var btnCancel = CreateButton("Hủy", Color.FromArgb(80, 80, 80), new Point(260, y), new Size(100, 35));
                btnCancel.Click += (s, ev) => form.Close();
                form.Controls.Add(btnCancel);

                form.AcceptButton = btnSave;
                form.ShowDialog(this);
            }
        }

        // ============================================
        // XÓA IP
        // ============================================
        private void BtnRemove_Click(object sender, EventArgs e)
        {
            int count = dgvMonitor.SelectedRows.Count;
            if (count == 0) { MessageBox.Show("Chọn IP cần xóa!"); return; }

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa {count} thiết bị đang chọn không?", 
                                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            foreach (DataGridViewRow row in dgvMonitor.SelectedRows)
            {
                var ip = row.Cells["colIpAddress"].Value?.ToString();
                if (!string.IsNullOrEmpty(ip))
                {
                    var monitor = ipList.FirstOrDefault(x => x.IpAddress == ip);
                    if (monitor != null)
                    {
                        AddLog(ip, monitor.Location, "Removed", "Xóa thủ công", monitor.DeviceName, monitor.User);
                        db.DeleteIpMonitor(ip);
                        ipList.Remove(monitor);
                    }
                }
            }
            UpdateDataGridView();
        }

        // ============================================
        // XÓA TẤT CẢ
        // ============================================
        private void BtnClearAll_Click(object sender, EventArgs e)
        {
            if (ipList.Count == 0) return;
            if (MessageBox.Show($"Xóa tất cả {ipList.Count} IP?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                AddLog("System", "", "ClearAll", $"Xóa tất cả {ipList.Count} IP");
                db.DeleteAllIpMonitors();
                ipList.Clear();
                UpdateDataGridView();
            }
        }

        // ============================================
        // IMPORT EXCEL
        // ============================================
        private void BtnImport_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                dialog.Title = "Chọn file Excel để import";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var data = ExcelHelper.ReadExcelToDataTable(dialog.FileName);
                        if (data.Rows.Count == 0)
                        {
                            MessageBox.Show("File Excel không có dữ liệu!", "Thông báo");
                            return;
                        }

                        using (var previewForm = new ImportPreviewForm(data, Path.GetFileName(dialog.FileName)))
                        {
                            if (previewForm.ShowDialog(this) == DialogResult.OK)
                            {
                                ImportFromDataTable(previewForm.ResultData);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi import:\n{ex.Message}", "Lỗi");
                    }
                }
            }
        }

        private void ImportFromDataTable(System.Data.DataTable data)
        {
            int added = 0, skipped = 0, invalid = 0;
            string[] headers = { "ip", "ipaddress", "address", "location", "vitri" };

            // Tìm index của cột IP
            int ipColIdx = -1;
            int locColIdx = -1;
            int nameColIdx = -1;
            int groupColIdx = -1;
            int serialColIdx = -1;
            int userColIdx = -1;

            // Nếu hàng đầu tiên là header (đã được xử lý trong preview form), DataTable columns sẽ có tên đúng
            // Hoặc chúng ta duyệt các cột để tìm tên
            for (int i = 0; i < data.Columns.Count; i++)
            {
                string colName = data.Columns[i].ColumnName.ToLower();
                if (colName.Contains("ip") || colName.Contains("address")) ipColIdx = i;
                else if (colName.Contains("vi tri") || colName.Contains("location")) locColIdx = i;
                else if (colName.Contains("ten") || colName.Contains("name") || colName.Contains("device")) nameColIdx = i;
                else if (colName.Contains("nhom") || colName.Contains("group")) groupColIdx = i;
                else if (colName.Contains("serial") || colName.Contains("imei")) serialColIdx = i;
                else if (colName.Contains("nguoi") || colName.Contains("user")) userColIdx = i;
            }

            // Nếu không tìm thấy cột IP, thử cột đầu tiên
            if (ipColIdx == -1) ipColIdx = 0;

            foreach (System.Data.DataRow row in data.Rows)
            {
                string ip = row[ipColIdx]?.ToString().Trim();
                if (string.IsNullOrEmpty(ip) || headers.Any(h => ip.ToLower().Contains(h))) continue; // Skip header-like rows if any

                if (!ValidateIpAddress(ip, out _)) 
                { 
                    invalid++; 
                    continue; 
                }

                if (ipList.Any(x => x.IpAddress.Equals(ip, StringComparison.OrdinalIgnoreCase))) 
                { 
                    skipped++; 
                    continue; 
                }

                var monitor = new IpMonitor 
                { 
                    IpAddress = ip,
                    Location = locColIdx != -1 ? row[locColIdx]?.ToString().Trim() : "",
                    DeviceName = nameColIdx != -1 ? row[nameColIdx]?.ToString().Trim() : "",
                    DeviceGroup = groupColIdx != -1 ? row[groupColIdx]?.ToString().Trim() : "",
                    Serial = serialColIdx != -1 ? row[serialColIdx]?.ToString().Trim() : "",
                    User = userColIdx != -1 ? row[userColIdx]?.ToString().Trim() : ""
                };

                ipList.Add(monitor);
                db.AddIpMonitor(monitor);
                AddLog(ip, (locColIdx != -1 ? row[locColIdx]?.ToString().Trim() : ""), "Added", "Import Excel", monitor.DeviceName, monitor.User);
                added++;
            }

            UpdateDataGridView();
            UpdateSidebar(); // Update sidebar immediately
            UpdateStatusLabel(); // Update stats immediately
            MessageBox.Show($"Import Excel xong!\n\nThêm: {added} | Bỏ qua: {skipped} | Lỗi: {invalid}", "Kết quả");
            _ = RefreshAllPings();
        }

        // ============================================
        // EXPORT EXCEL
        // ============================================
        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (ipList.Count == 0) { MessageBox.Show("Khong co du lieu!"); return; }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                dialog.FileName = $"PingMonitor_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                dialog.Title = "Luu file Excel";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ExportToExcel(dialog.FileName);
                        MessageBox.Show($"Da export {ipList.Count} thiet bi!\n\nFile: {dialog.FileName}", "Thanh cong");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Loi export:\n{ex.Message}", "Loi");
                    }
                }
            }
        }

        private void ExportToExcel(string filePath)
        {
            // Tạo file Excel (.xlsx) đơn giản
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.Combine(tempDir, "_rels"));
            Directory.CreateDirectory(Path.Combine(tempDir, "xl"));
            Directory.CreateDirectory(Path.Combine(tempDir, "xl", "_rels"));
            Directory.CreateDirectory(Path.Combine(tempDir, "xl", "worksheets"));

            // [Content_Types].xml
            File.WriteAllText(Path.Combine(tempDir, "[Content_Types].xml"), @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
<Default Extension=""xml"" ContentType=""application/xml""/>
<Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
<Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
<Override PartName=""/xl/sharedStrings.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml""/>
</Types>");

            // _rels/.rels
            File.WriteAllText(Path.Combine(tempDir, "_rels", ".rels"), @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");

            // xl/workbook.xml
            File.WriteAllText(Path.Combine(tempDir, "xl", "workbook.xml"), @"<?xml version=""1.0"" encoding=""UTF-8""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
<sheets><sheet name=""Devices"" sheetId=""1"" r:id=""rId1""/></sheets>
</workbook>");

            // xl/_rels/workbook.xml.rels
            File.WriteAllText(Path.Combine(tempDir, "xl", "_rels", "workbook.xml.rels"), @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
<Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"" Target=""sharedStrings.xml""/>
</Relationships>");

            // Tạo danh sách strings và sheet data
            var strings = new List<string>();
            var rows = new StringBuilder();

            // Header row
            string[] headers = { "STT", "Nhom thiet bi", "Ten thiet bi", "Hinh anh", "IP", "MAC", "Serial/IMEI", "Nguoi dung", "Vi tri", "Trang thai", "Do tre (ms)", "Thoi gian kiem tra", "Thoi gian Offline", "Ngay tao" };
            rows.Append("<row r=\"1\">");
            for (int i = 0; i < headers.Length; i++)
            {
                int strIdx = strings.Count;
                strings.Add(headers[i]);
                rows.Append($"<c r=\"{(char)('A' + i)}1\" t=\"s\"><v>{strIdx}</v></c>");
            }
            rows.Append("</row>");

            // Data rows
            int rowNum = 2;
            foreach (var ip in ipList)
            {
                rows.Append($"<row r=\"{rowNum}\">");
                
                // STT (number)
                rows.Append($"<c r=\"A{rowNum}\"><v>{rowNum - 1}</v></c>");
                
                // String columns
                string[] values = {
                    ip.DeviceGroup, ip.DeviceName, ip.ImagePath, ip.IpAddress, ip.MacAddress,
                    ip.Serial, ip.User, ip.Location, ip.Status, ip.Latency.ToString(),
                    ip.LastCheckTime.ToString("dd/MM/yyyy HH:mm:ss"),
                    ip.LastOfflineTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-",
                    ip.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
                };
                
                for (int i = 0; i < values.Length; i++)
                {
                    int strIdx = strings.Count;
                    strings.Add(values[i] ?? "");
                    rows.Append($"<c r=\"{(char)('B' + i)}{rowNum}\" t=\"s\"><v>{strIdx}</v></c>");
                }
                
                rows.Append("</row>");
                rowNum++;
            }

            // xl/worksheets/sheet1.xml
            File.WriteAllText(Path.Combine(tempDir, "xl", "worksheets", "sheet1.xml"), 
                $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
<sheetData>{rows}</sheetData>
</worksheet>");

            // xl/sharedStrings.xml
            var ssXml = new StringBuilder(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<sst xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");
            foreach (var s in strings)
            {
                ssXml.Append($"<si><t>{System.Security.SecurityElement.Escape(s)}</t></si>");
            }
            ssXml.Append("</sst>");
            File.WriteAllText(Path.Combine(tempDir, "xl", "sharedStrings.xml"), ssXml.ToString());

            // Nén thành file xlsx
            if (File.Exists(filePath)) File.Delete(filePath);
            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, filePath);
            Directory.Delete(tempDir, true);
        }

        private void BtnExportLog_Click(object sender, EventArgs e)
        {
            var logs = db.GetLogs(50000);
            if (logs.Count == 0) { MessageBox.Show("Khong co log!"); return; }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV (*.csv)|*.csv";
                dialog.FileName = $"PingLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Timestamp,IP Address,Location,Event,Details");
                    foreach (var log in logs)
                    {
                        sb.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.IpAddress}\",\"{log.Location}\",\"{log.Event}\",\"{log.Details}\"");
                    }
                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Da export {logs.Count} dong log!", "Thanh cong");
                }
            }
        }

        // ============================================
        // KEYPRESS
        // ============================================
        private void TxtIpAddress_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter) { BtnAdd_Click(sender, e); e.Handled = true; }
        }

        // ============================================
        // UPDATE DATAGRIDVIEW
        // ============================================
        private void UpdateDataGridView()
        {
            // 1. Save current state
            string selectedIp = null;
            if (dgvMonitor.SelectedRows.Count > 0)
            {
                selectedIp = dgvMonitor.SelectedRows[0].Cells["colIpAddress"].Value?.ToString();
            }
            // FirstDisplayedScrollingRowIndex can be invalid if rows clear
            int scrollIndex = dgvMonitor.FirstDisplayedScrollingRowIndex;

            // Get current filters
            string searchQuery = txtSearch?.Text.Trim().ToLower() ?? "";
            string userFilter = "";
            
            if (lbUsers?.SelectedItem != null)
            {
                string selected = lbUsers.SelectedItem.ToString();
                if (selected.Contains("("))
                    userFilter = selected.Substring(0, selected.LastIndexOf("(")).Trim();
                if (userFilter == "Tất cả") userFilter = "";
            }

            // Apply filters
            var filteredList = ipList.AsEnumerable();
            
            // User filter
            if (!string.IsNullOrEmpty(userFilter))
            {
                filteredList = filteredList.Where(ip => 
                    ip.User.Equals(userFilter, StringComparison.OrdinalIgnoreCase) ||
                    (userFilter == "Chưa phân loại" && string.IsNullOrEmpty(ip.User)));
            }
            
            // Search filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                filteredList = filteredList.Where(ip =>
                    ip.IpAddress.ToLower().Contains(searchQuery) ||
                    ip.DeviceName.ToLower().Contains(searchQuery) ||
                    ip.Location.ToLower().Contains(searchQuery) ||
                    ip.User.ToLower().Contains(searchQuery) ||
                    ip.DeviceGroup.ToLower().Contains(searchQuery) ||
                    ip.Serial.ToLower().Contains(searchQuery) ||
                    ip.Status.ToLower().Contains(searchQuery));
            }

            // Populate grid
            dgvMonitor.Rows.Clear();
            int idx = 1;
            foreach (var ip in filteredList)
            {
                // Load ảnh nếu có
                Image img = null;
                if (!string.IsNullOrEmpty(ip.ImagePath) && File.Exists(ip.ImagePath))
                {
                    try { img = Image.FromFile(ip.ImagePath); }
                    catch { img = null; }
                }

                // Calculate offline days
                string offlineDays = "-";
                if (ip.Status == "Offline" && ip.LastOfflineTime.HasValue)
                {
                    var span = DateTime.Now - ip.LastOfflineTime.Value;
                    if (span.TotalDays >= 1) offlineDays = $"{span.TotalDays:0.#} ngày";
                    else if (span.TotalHours >= 1) offlineDays = $"{span.TotalHours:0.#} giờ";
                    else offlineDays = $"{span.TotalMinutes:0} phút";
                }

                dgvMonitor.Rows.Add(
                    idx++,                                                      // STT
                    string.IsNullOrEmpty(ip.DeviceGroup) ? "Chưa phân loại" : ip.DeviceGroup, // Nhóm thiết bị
                    ip.DeviceName,                                              // Tên thiết bị
                    img,                                                        // Hình ảnh
                    ip.IpAddress,                                               // IP
                    ip.MacAddress,                                              // MAC
                    ip.Serial,                                                  // Serial
                    ip.User,                                                    // User
                    ip.Location,                                                // Vị trí
                    ip.Status,                                                  // Trạng thái
                    ip.Latency > 0 ? $"{ip.Latency}ms" : "-",                   // Độ trễ
                    ip.LastCheckTime.ToString("dd/MM/yyyy HH:mm:ss"),           // Thời gian kiểm tra
                    ip.LastOfflineTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-", // Thời gian Offline
                    offlineDays,                                                // Đếm (Offline duration)
                    ip.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")                // Ngày tạo
                );
            }
            
            UpdateStatusLabel();
            UpdateSidebar();

            // 2. Restore state
            if (dgvMonitor.Rows.Count > 0)
            {
                // Restore selection
                if (!string.IsNullOrEmpty(selectedIp))
                {
                    dgvMonitor.ClearSelection();
                    bool found = false;
                    foreach (DataGridViewRow row in dgvMonitor.Rows)
                    {
                        if (row.Cells["colIpAddress"].Value?.ToString() == selectedIp)
                        {
                            row.Selected = true;
                            // Set CurrentCell to ensure selection is active and visible logic works
                            if (row.Cells.Count > 0) dgvMonitor.CurrentCell = row.Cells[0]; 
                            found = true;
                            break;
                        }
                    }
                    if (!found && dgvMonitor.Rows.Count > 0) dgvMonitor.ClearSelection();
                }
                else
                {
                    dgvMonitor.ClearSelection();
                }

                // Restore scroll
                if (scrollIndex >= 0 && scrollIndex < dgvMonitor.Rows.Count)
                {
                    dgvMonitor.FirstDisplayedScrollingRowIndex = scrollIndex;
                }
            }
        }

        private void UpdateStatusLabel()
        {
            int online = ipList.Count(x => x.Status == "Online");
            int offline = ipList.Count(x => x.Status == "Offline" || x.Status == "Pending");
            
            // Just numbers, the card titles explain what they are
            if (lblOnline != null) lblOnline.Text = $"{online}";
            if (lblOffline != null) lblOffline.Text = $"{offline}";
            if (lblTotal != null) lblTotal.Text = $"{ipList.Count}";
        }

        // ============================================
        // PING ĐA LUỒNG
        // ============================================
        private async Task RefreshAllPings()
        {
            if (isRefreshing || ipList.Count == 0) return;

            isRefreshing = true;
            progressBar.Visible = true;
            progressBar.Maximum = ipList.Count;
            progressBar.Value = 0;

            try
            {
                using (var semaphore = new SemaphoreSlim(settings.MaxConcurrentPings))
                {
                    int completed = 0;
                    var prevStates = ipList.ToDictionary(x => x.IpAddress, x => x.Status);

                    var tasks = ipList.Select(async ip =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            // Call Service
                            var (isSuccess, avgLatency) = await pingService.PingHostAsync(ip.IpAddress, settings.PingTimeoutMs, settings.PingRetryCount);

                            // STABILITY ALGORITHM:
                            // Only mark Offline if N consecutive failures occur.
                            // Mark Online immediately if 1 success occurs.
                            if (isSuccess)
                            {
                                ip.ConsecutiveFailures = 0;
                                ip.UpdateStatus(true, avgLatency);
                            }
                            else
                            {
                                ip.ConsecutiveFailures++;
                                if (ip.ConsecutiveFailures >= settings.OfflineThreshold)
                                {
                                    ip.UpdateStatus(false, 0);
                                }
                                else
                                {
                                    // Transient failure, keep current status but update check time
                                    ip.LastCheckTime = DateTime.Now;
                                }
                            }
                        }
                        catch 
                        { 
                            ip.ConsecutiveFailures++;
                            if (ip.ConsecutiveFailures >= settings.OfflineThreshold)
                                ip.UpdateStatus(false, 0);
                        }
                        finally
                        {
                            semaphore.Release();
                            Interlocked.Increment(ref completed);
                            this.BeginInvoke(new Action(() =>
                            {
                                if (completed <= progressBar.Maximum) progressBar.Value = completed;
                            }));
                        }
                    }).ToArray();

                    await Task.WhenAll(tasks);

                    // Log thay đổi và lưu vào DB
                    foreach (var ip in ipList)
                    {
                        if (prevStates.TryGetValue(ip.IpAddress, out string prev))
                        {
                            if (prev != ip.Status && prev != "Pending")
                            {
                                AddLog(ip.IpAddress, ip.Location, ip.Status, $"{prev} -> {ip.Status}", ip.DeviceName, ip.User);
                            }
                        }
                    }
                    // Batch Update DB
                    db.UpdateAllIpMonitorStatuses(ipList);
                }

                // Smooth UI Update
                if (this.InvokeRequired) this.Invoke(new Action(RefreshGridRowValues));
                else RefreshGridRowValues();
            }
            finally
            {
                isRefreshing = false;
                progressBar.Visible = false;
            }
        }

        private void RefreshGridRowValues()
        {
            if (dgvMonitor.Rows.Count == 0) return;

            // Simple map for speed if needed, but simple linear scan match is safer if sort changed? 
            // Actually, rows have IPs.
            // But Map is O(N).
            var ipMap = ipList.ToDictionary(x => x.IpAddress);

            foreach (DataGridViewRow row in dgvMonitor.Rows)
            {
                var ipAddr = row.Cells["colIpAddress"].Value?.ToString();
                if (string.IsNullOrEmpty(ipAddr) || !ipMap.ContainsKey(ipAddr)) continue;

                var ip = ipMap[ipAddr];
                
                // Update cells if value changed
                UpdateCellIfChanged(row.Cells["colStatus"], ip.Status);
                UpdateCellIfChanged(row.Cells["colLatency"], ip.Latency > 0 ? $"{ip.Latency}ms" : "-");
                UpdateCellIfChanged(row.Cells["colLastCheck"], ip.LastCheckTime.ToString("dd/MM/yyyy HH:mm:ss"));
                UpdateCellIfChanged(row.Cells["colLastOffline"], ip.LastOfflineTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-");

                string offlineDays = "";
                if (ip.Status == "Offline" && ip.LastOfflineTime.HasValue)
                {
                    offlineDays = FormatOfflineDuration(DateTime.Now - ip.LastOfflineTime.Value);
                }
                UpdateCellIfChanged(row.Cells["colOfflineDays"], offlineDays);
            }
            UpdateStatusLabel();
        }

        private void UpdateCellIfChanged(DataGridViewCell cell, string newValue)
        {
            if (cell.Value == null || cell.Value.ToString() != newValue) cell.Value = newValue;
        }

        private void ResetGridViewLayout()
        {
            // Reset Column Widths
            dgvMonitor.Columns["colIndex"].Width = 50;
            dgvMonitor.Columns["colDeviceGroup"].Width = 100;
            dgvMonitor.Columns["colDeviceName"].Width = 150;
            dgvMonitor.Columns["colImage"].Width = 60;
            dgvMonitor.Columns["colIpAddress"].Width = 100;
            dgvMonitor.Columns["colMacAddress"].Width = 110;
            dgvMonitor.Columns["colSerial"].Width = 130;
            dgvMonitor.Columns["colUser"].Width = 130;
            dgvMonitor.Columns["colLocation"].Width = 110;
            dgvMonitor.Columns["colStatus"].Width = 120;
            dgvMonitor.Columns["colLatency"].Width = 50;
            dgvMonitor.Columns["colLastCheck"].Width = 150;
            dgvMonitor.Columns["colLastOffline"].Width = 150;
            dgvMonitor.Columns["colOfflineDays"].Width = 80;
            
            // Reset Row Heights
            dgvMonitor.RowTemplate.Height = 45;
            foreach (DataGridViewRow row in dgvMonitor.Rows) row.Height = 45;

            // Reset Sort
            sortColumnIndex = -1;
            sortAscending = true;
            foreach (DataGridViewColumn col in dgvMonitor.Columns) col.HeaderCell.SortGlyphDirection = SortOrder.None;

            // Re-order list to default (by Id)
            ipList = ipList.OrderBy(x => x.Id).ToList();
            
            // Refresh with current filter
            UpdateDataGridView();
        }

        // ============================================
        // CLEANUP
        // ============================================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            AddLog("System", "", "Shutdown", "Ung dung dong");
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            base.OnFormClosing(e);
        }

        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            AutoScaleMode = AutoScaleMode.Font;
        }
    }
}
