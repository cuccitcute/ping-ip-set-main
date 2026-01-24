using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PingMonitor.Models;
using PingMonitor.Services;
using PingMonitor.Helpers;

using PingMonitor.Forms;

namespace PingMonitor
{
    public partial class MainForm
    {
        // ============================================
        // UI FIELDS
        // ============================================
        private DataGridView dgvMonitor;
        private SplitContainer splitContainer;
        private Panel panelSidebar;
        private Panel panelTopBar; // Promoted from local
        private Panel panelToolbar; 
        private Label lblSidebarTitle;
        private Button btnToggleSidebar;
        private ListBox lbUsers;
        private Label lblTotal;
        private Label lblOnline;
        private Label lblOffline;
        private Button btnSettings;
        private Label btnLogText; // Changed to Label/Link style
        private Button btnAdd;
        // private Button btnRemove; // Removed
        // private Button btnClearAll; // Removed
        // private Button btnPingNow; // Removed
        private Button btnImport;
        private Button btnExport;
        private TextBox txtSearch;
        private Panel panelSearch; // Use this class field!
        private Label lblSearchPlaceholder; // Promote
        private Label lblSearchIcon; // Promote
        private FlowLayoutPanel panelStats; // Promoted from local
        private ContextMenuStrip contextMenu;
        private Label lblDbInfo;
        private ProgressBar progressBar;
        private System.Windows.Forms.Timer refreshTimer;
        private System.Windows.Forms.Timer uiTimer;
        private Button btnClearFilter;
        private Label lblAuthor;
        private Label lblTitle;
        private Label lblSubLabel;
        private Label lblTotalTitle;
        private Label lblOnlineTitle;
        private Label lblOfflineTitle;


        private Label lblDateTime;
        // private ComboBox cboLanguage; // Removed
        private int currentLanguageIndex = 0;
        private int unreadLogCount = 0;
        private int sortColumnIndex = -1;
        private bool sortAscending = true;

        // ============================================
        // THEME PROXIES (Redirect to ThemeService)
        // ============================================
        private Color COLOR_BACKGROUND => ThemeService.Background;
        private Color COLOR_SURFACE => ThemeService.Surface;
        private Color COLOR_PRIMARY => ThemeService.BtnPrimary;
        private Color COLOR_ONLINE_BG => ThemeService.OnlineBg;
        private Color COLOR_ONLINE_TEXT => ThemeService.OnlineText;
        private Color COLOR_OFFLINE_BG => ThemeService.OfflineBg;
        private Color COLOR_OFFLINE_TEXT => ThemeService.OfflineText;
        private Color COLOR_TEXT_MAIN => ThemeService.TextMain;
        private Color COLOR_TEXT_MUTED => ThemeService.TextMuted;
        private Color COLOR_BORDER => ThemeService.Border;
        private Color COLOR_HEADER => ThemeService.SurfaceAlt;
        private Color COLOR_SURFACE_ALT => ThemeService.SurfaceAlt;
        private Color COLOR_SELECTION => ThemeService.Selection;
        private Color COLOR_BTN_ADD => ThemeService.BtnPrimary;
        private Color COLOR_BTN_DELETE => ThemeService.BtnDanger;
        private Color COLOR_BTN_PING => ThemeService.BtnSuccess;
        private Color COLOR_BTN_WARNING => ThemeService.BtnWarning;
        private Color COLOR_BTN_GRAY => ThemeService.BtnSecondary;

        // ============================================
        // INITIALIZATION
        // ============================================
        private void InitializeUI()
        {
            this.Text = _loc.Get("AppTitle");
            this.Size = new Size(1350, 800);
            this.MinimumSize = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = COLOR_BACKGROUND;
            this.Font = new Font("Segoe UI", 9F);

            InitializeTopBar();

            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                IsSplitterFixed = false,
                SplitterWidth = 5,
                Orientation = Orientation.Vertical,
                BackColor = COLOR_BACKGROUND 
            };
            this.Controls.Add(splitContainer);
            this.Controls.SetChildIndex(splitContainer, 0);
            this.Shown += (s, e) => { splitContainer.SplitterDistance = 180; };

            panelSidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ThemeService.BrandColor, // Dark Navy Sidebar
                Padding = new Padding(10)
            };
            splitContainer.Panel1.Controls.Add(panelSidebar);
            
            var panelContent = new Panel { Dock = DockStyle.Fill, BackColor = COLOR_BACKGROUND };
            splitContainer.Panel2.Controls.Add(panelContent);

            InitializeCombinedToolbar(panelContent); 
            InitializeDataGridView(panelContent);    

            InitializeContextMenu();
            InitializeKeyboardShortcuts();
            
            InitializeSidebar();
            
            // Apply initial theme
            ApplyTheme();
            
            // Wire Resize event for responsive sidebar
            this.Resize += Form_Resize;
        }

    

        private void InitializeTopBar()
        {
            // Use Centralized Brand Color
            var headerColor = ThemeService.HeaderBg; // Use HeaderBg
            
            panelTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = headerColor, 
                Padding = new Padding(20, 0, 20, 0)
            };
            this.Controls.Add(panelTopBar);

            var border = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(30, 41, 59) }; // Slate 800
            panelTopBar.Controls.Add(border);

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
                panelTopBar.Controls.Add(picIcon);
            }

            lblTitle = new Label
            {
                Text = _loc.Get("Title"),
                ForeColor = ThemeService.HeaderText, // Dynamic Header Text
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(60, 12)
            };
            panelTopBar.Controls.Add(lblTitle);

            lblSubLabel = new Label
            {
                Text = _loc.Get("SubTitle"),
                ForeColor = ThemeService.TextMuted, // Dynamic Muted Text
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(64, 42)
            };
            panelTopBar.Controls.Add(lblSubLabel);
            
            lblDateTime = new Label
            {
                Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                ForeColor = ThemeService.TextMuted, // Dynamic Muted Text
                Font = new Font("Consolas", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(panelTopBar.Width - 200, 12), // Moved up slightly
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            panelTopBar.Controls.Add(lblDateTime);

            // Log Link
            btnLogText = new Label
            {
                Text = "Log",
                ForeColor = ThemeService.HeaderText,
                Font = new Font("Segoe UI", 9F, FontStyle.Underline),
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(panelTopBar.Width - 60, 32), // Below Time
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnLogText.Click += BtnViewLogs_Click;
            panelTopBar.Controls.Add(btnLogText);

            // === NEW SEARCH BAR (CENTERED) ===
            panelSearch = new Panel // Use class field
            {
                Height = 35, // Taller pill shape
                Width = 400,
                BackColor = Color.Transparent, 
                Anchor = AnchorStyles.Top 
            };
            // Initial centering
            void CenterSearch()
            {
                if(panelTopBar != null && panelSearch != null)
                    panelSearch.Location = new Point((panelTopBar.Width - panelSearch.Width) / 2, (panelTopBar.Height - panelSearch.Height) / 2);
            }
            CenterSearch();
            this.Resize += (s, e) => CenterSearch();

            panelTopBar.Controls.Add(panelSearch);
            
            panelSearch.Tag = "HeaderSearch";

            // Color Strategy: Use dynamic Theme properties in Paint/ApplyTheme
            // Color pillColor = ThemeService.BrandColorLight; // Removing local var to force dynamic usage

            // Custom Paint for Rounded Background
            panelSearch.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Use BrandColorLight (Slate200/Slate700) for the pill background
                using (var brush = new SolidBrush(ThemeService.BrandColorLight)) 
                {
                    var rect = new Rectangle(0, 0, panelSearch.Width - 1, panelSearch.Height - 1);
                    var path = Helpers.DrawingHelper.GetRoundedPath(rect, 16);
                    e.Graphics.FillPath(brush, path);
                }
            };
            
            // Search Icon (Left)
            lblSearchIcon = new Label
            {
                Text = Services.IconHelper.Search, 
                Font = new Font(Services.IconHelper.FontName, 12F),
                ForeColor = ThemeService.TextMuted, // Dynamic Icon Color
                AutoSize = true,
                BackColor = ThemeService.BrandColorLight, // Initial dynamic color (updated in ApplyTheme)
                Location = new Point(10, 8),
                Cursor = Cursors.IBeam
            };
            panelSearch.Controls.Add(lblSearchIcon);

            // Search Text Box
            txtSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = ThemeService.BrandColorLight, // Initial dynamic color
                ForeColor = ThemeService.IsDarkMode ? Color.White : ThemeService.TextMain, // Initial Text Color
                Font = new Font("Segoe UI", 10F),
                Location = new Point(35, 8),
                Width = 330
            };

            txtSearch.TextChanged += (s, e) => {
                lblSearchPlaceholder.Visible = string.IsNullOrEmpty(txtSearch.Text);
                // Show/Hide Clear Button
                var btnClear = panelSearch.Controls.OfType<Label>().FirstOrDefault(c => c.Tag?.ToString() == "Clear");
                if (btnClear != null) btnClear.Visible = !string.IsNullOrEmpty(txtSearch.Text);
                
                FilterGridBySearch(txtSearch.Text.Trim().ToLower());
            };
            panelSearch.Controls.Add(txtSearch);

            // Placeholder
            lblSearchPlaceholder = new Label
            {
                Text = "Tìm kiếm IP, người dùng...",
                ForeColor = ThemeService.TextMuted, // Dynamic Muted Text
                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                AutoSize = true,
                BackColor = ThemeService.BrandColorLight,
                Location = new Point(35, 8),
                Cursor = Cursors.IBeam
            };
            lblSearchPlaceholder.Click += (s, e) => txtSearch.Focus();
            panelSearch.Controls.Add(lblSearchPlaceholder);
            lblSearchPlaceholder.BringToFront(); 
            
            lblSearchIcon.Click += (s, e) => txtSearch.Focus();
            
            // Clear Button (Right)
            var btnClearSearch = new Label
            {
                Text = "\uE711", // Segoe MDL2 'X'
                Font = new Font("Segoe MDL2 Assets", 10F), 
                ForeColor = ThemeService.IsDarkMode ? Color.White : ThemeService.TextMain,
                AutoSize = true,
                BackColor = ThemeService.BrandColorLight,
                Location = new Point(panelSearch.Width - 30, 9),
                Cursor = Cursors.Hand,
                Visible = false,
                Tag = "Clear"
            };
            btnClearSearch.Click += (s, e) => { txtSearch.Text = ""; txtSearch.Focus(); };
            panelSearch.Controls.Add(btnClearSearch);
            btnClearSearch.BringToFront();

            this.Tag = panelSearch; 
        }

        private void InitializeCombinedToolbar(Control parent)
        {
            panelToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = COLOR_BACKGROUND,
                Padding = new Padding(10, 5, 10, 5)
            };
            parent.Controls.Add(panelToolbar);

            // Left Flow Panel
            var flowLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, // Fill the space not taken by Right panel
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                BackColor = Color.Transparent, // Fix: Ensure transparency so panelToolbar color shows
                Padding = new Padding(0, 10, 0, 0) // Vertical center alignment approx
            };
            
            // Right Flow Panel
            var flowRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent, // Fix: Ensure transparency
                Padding = new Padding(0, 10, 5, 0)
            };
            panelToolbar.Controls.Add(flowRight);
            panelToolbar.Controls.Add(flowLeft); // Add Left second regarding Z-order/Dock? No, Dock Right first, then Fill works.

            // Helper to configure Pill/Rounded buttons
            void ConfigurePillButton(Button btn, Color bgColor, string icon, Func<int> badgeProvider = null)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
                btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
                btn.BackColor = Color.Transparent;
                btn.Cursor = Cursors.Hand;
                btn.Text = ""; // Hide default text

                btn.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    var rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
                    using (var brush = new SolidBrush(bgColor))
                    using (var path = PingMonitor.Helpers.DrawingHelper.GetRoundedPath(rect, 4))
                    {
                        g.FillPath(brush, path);
                    }

                    var iconFont = new Font(Services.IconHelper.FontName, 10F);
                    string text = (string)btn.Tag;
                    var textSize = g.MeasureString(text, btn.Font);
                    int contentWidth = 20 + (int)textSize.Width;
                    int startX = (btn.Width - contentWidth) / 2;
                    int startY = (btn.Height - (int)Math.Max(textSize.Height, 20)) / 2;

                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.DrawString(icon, iconFont, brush, startX, startY + 4); 
                        g.DrawString(text, btn.Font, brush, startX + 22, startY + 4);
                    }

                    if (badgeProvider != null)
                    {
                        int count = badgeProvider();
                        if (count > 0)
                        {
                            string countText = count > 99 ? "99+" : count.ToString();
                            int size = 18;
                            Rectangle badgeRect = new Rectangle(btn.Width - size - 4, -2, size, size);
                            using (var brush = new SolidBrush(Color.Red)) g.FillEllipse(brush, badgeRect);
                            using (var brush = new SolidBrush(Color.White))
                            {
                                var bf = new Font("Segoe UI", 7F, FontStyle.Bold);
                                var ts = g.MeasureString(countText, bf);
                                g.DrawString(countText, bf, brush, badgeRect.X + (badgeRect.Width - ts.Width) / 2, badgeRect.Y + (badgeRect.Height - ts.Height) / 2 + 1);
                            }
                        }
                    }
                };
            }

            int h = 36;

            // 1. Toggle Sidebar
            btnToggleSidebar = CreateButton("", ThemeService.BtnSecondary, Point.Empty, new Size(35, h), IconHelper.GlobalNavButton);
            btnToggleSidebar.Click += (s, e) => { splitContainer.Panel1Collapsed = !splitContainer.Panel1Collapsed; };
            btnToggleSidebar.BackColor = Color.Transparent;
            btnToggleSidebar.ForeColor = ThemeService.TextMuted;
            btnToggleSidebar.Margin = new Padding(0, 0, 10, 0);
            flowLeft.Controls.Add(btnToggleSidebar);

        
            // 2. Add Device
            string addText = _loc.Get("Add");
            btnAdd = new Button
            {
                Size = new Size(130, h),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Tag = addText,
                Margin = new Padding(0, 0, 10, 0)
            };
            ConfigurePillButton(btnAdd, ThemeService.BtnPrimary, IconHelper.Add);
            btnAdd.Click += BtnAdd_Click;
            flowLeft.Controls.Add(btnAdd);

             // 2. Import CSV (Ghost Style)
            string importText = _loc.Get("Import");
            btnImport = new Button // Use class field
            {
                Text = "",
                Size = new Size(110, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = ThemeService.TextMain,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Tag = importText,
                Margin = new Padding(0, 0, 10, 0)
            };
            btnImport.FlatAppearance.BorderSize = 0;
            // Ghost Hover Effect
            // Initial set - will be updated by ApplyTheme
            btnImport.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 255, 255, 255);
            btnImport.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 255, 255, 255);
            
            btnImport.Paint += (s, e) => {
                 var g = e.Graphics;
                 g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                 g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                 var iconFont = new Font(Services.IconHelper.FontName, 10F);
                 var text = btnImport.Tag?.ToString() ?? "";
                 
                 using (var brush = new SolidBrush(btnImport.ForeColor))
                 {
                    // Draw Icon
                    g.DrawString(Services.IconHelper.Import, iconFont, brush, 0, 9);
                    // Draw Text
                    g.DrawString(text, btnImport.Font, brush, 25, 9);
                 }
            };
            btnImport.Click += BtnImport_Click;
            flowLeft.Controls.Add(btnImport);

            // === RIGHT PANEL (Already defined above) ===
            // Adding Export button to flowRight

            // Export
            btnExport = new Button { Size = new Size(100, h), Tag = _loc.Get("Export"), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(5, 0, 5, 0) };
            ConfigurePillButton(btnExport, ThemeService.BtnSuccess, IconHelper.Export);
            btnExport.Click += BtnExport_Click;
            flowRight.Controls.Add(btnExport);

            // Settings
            btnSettings = new Button { Size = new Size(100, h), Tag = _loc.Get("Settings"), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 0) };
            ConfigurePillButton(btnSettings, ThemeService.BtnSecondary, IconHelper.Settings);
            btnSettings.Click += BtnSettings_Click;
            flowRight.Controls.Add(btnSettings);
        }

        private void ApplyTheme()
        {
            this.BackColor = COLOR_BACKGROUND;
            if (splitContainer != null) {
                splitContainer.BackColor = COLOR_BACKGROUND;
                // Sidebar: Always Dark Navy (BrandColor)
            // Sidebar
            if (panelSidebar != null)
            {
                panelSidebar.BackColor = ThemeService.SidebarBg;
            }
            if (lbUsers != null)
            {
                lbUsers.BackColor = ThemeService.SidebarBg;
                lbUsers.ForeColor = ThemeService.SidebarText;
            }
            
            }
            
            // Toolbar
            if (panelToolbar != null) panelToolbar.BackColor = COLOR_BACKGROUND;

            // Add full recursive update if needed, for now just key panels
            if (dgvMonitor != null)
            {
                dgvMonitor.BackgroundColor = COLOR_BACKGROUND; 
                dgvMonitor.GridColor = COLOR_BORDER;
                dgvMonitor.DefaultCellStyle.BackColor = COLOR_SURFACE; 
                dgvMonitor.DefaultCellStyle.ForeColor = COLOR_TEXT_MAIN; 
                dgvMonitor.DefaultCellStyle.SelectionBackColor = ThemeService.Selection; 
                dgvMonitor.DefaultCellStyle.SelectionForeColor = Color.White;
                
                // Header
                dgvMonitor.ColumnHeadersDefaultCellStyle.BackColor = ThemeService.IsDarkMode ? ThemeService.BrandColor : ThemeService.BrandColorLight; 
                dgvMonitor.ColumnHeadersDefaultCellStyle.ForeColor = ThemeService.IsDarkMode ? Color.White : ThemeService.TextMain;
                dgvMonitor.EnableHeadersVisualStyles = false;
            }
            // Update labels
            if (lblTitle != null) lblTitle.ForeColor = ThemeService.HeaderText;
            if (lblSubLabel != null) lblSubLabel.ForeColor = ThemeService.TextMuted;
            if (btnImport != null) 
            {
                btnImport.ForeColor = ThemeService.TextMain;
                // Fix Hover visibility in Light Mode (White on White is invisible)
                var hoverColor = ThemeService.IsDarkMode ? Color.FromArgb(10, 255, 255, 255) : Color.FromArgb(10, 0, 0, 0);
                var downColor = ThemeService.IsDarkMode ? Color.FromArgb(30, 255, 255, 255) : Color.FromArgb(30, 0, 0, 0);
                
                btnImport.FlatAppearance.MouseOverBackColor = hoverColor;
                btnImport.FlatAppearance.MouseDownBackColor = downColor;

                // Force repaint to fix any potential stuck graphics
                btnImport.Invalidate(); 
            }
            
            // Search Bar Updates
            if (panelSearch != null)
            {
                 panelSearch.Invalidate(); // Repaint pill background
                 var pillColor = ThemeService.BrandColorLight;
                 
                 if (txtSearch != null)
                 {
                     txtSearch.BackColor = pillColor;
                     txtSearch.ForeColor = ThemeService.IsDarkMode ? Color.White : ThemeService.TextMain;
                 }
                 if (lblSearchIcon != null)
                 {
                     lblSearchIcon.BackColor = pillColor;
                     lblSearchIcon.ForeColor = ThemeService.TextMuted;
                 }
                 if (lblSearchPlaceholder != null)
                 {
                     lblSearchPlaceholder.BackColor = pillColor;
                     lblSearchPlaceholder.ForeColor = ThemeService.TextMuted;
                 }
                 // Fix Clear Button (find by Tag)
                 var btnClear = panelSearch.Controls.OfType<Label>().FirstOrDefault(c => c.Tag?.ToString() == "Clear");
                 if (btnClear != null)
                 {
                     btnClear.BackColor = pillColor;
                     btnClear.ForeColor = ThemeService.IsDarkMode ? Color.White : ThemeService.TextMain;
                 }
            }
            
            // Header Panel
            if (panelTopBar != null) panelTopBar.BackColor = ThemeService.HeaderBg;
            
            // Force redraw of Sidebar ListBox
            if (lbUsers != null) lbUsers.Invalidate();
            
            // Stats Bar
            if (panelStats != null)
            {
                panelStats.BackColor = ThemeService.Background;
                foreach(Control c in panelStats.Controls)
                {
                    if (c is Panel p)
                    {
                        p.BackColor = ThemeService.Surface; // Update stat box background
                        // Bar color (control 0) doesn't change usually (Primary/Ping/Delete) or maybe it should?
                        // Labels are handled below via fields
                    }
                }
            }
            if (lblTotalTitle != null) lblTotalTitle.ForeColor = ThemeService.TextMuted;
            if (lblOnlineTitle != null) lblOnlineTitle.ForeColor = ThemeService.TextMuted;
            if (lblOfflineTitle != null) lblOfflineTitle.ForeColor = ThemeService.TextMuted;
            
            if (lblTotal != null) lblTotal.ForeColor = ThemeService.TextMain;
            if (lblOnline != null) lblOnline.ForeColor = ThemeService.OnlineText;
            if (lblOffline != null) lblOffline.ForeColor = ThemeService.OfflineText;

            // DataGridView Alternating Rows & Refresh
            if (dgvMonitor != null)
            {
                dgvMonitor.BackgroundColor = COLOR_BACKGROUND;
                dgvMonitor.GridColor = ThemeService.Border;
                
                // Explicitly set colors without relying on proxies if possible to ensure correctness
                var surfaceColor = ThemeService.IsDarkMode ? Color.FromArgb(30, 41, 59) : Color.White; // Slate800 / White
                var altColor = ThemeService.IsDarkMode ? Color.FromArgb(15, 23, 42) : Color.FromArgb(248, 250, 252); // Slate900 / Slate50
                var textColor = ThemeService.IsDarkMode ? Color.FromArgb(241, 245, 249) : Color.FromArgb(15, 23, 42); // Slate100 / Slate900

                dgvMonitor.DefaultCellStyle.BackColor = surfaceColor;
                dgvMonitor.DefaultCellStyle.ForeColor = textColor;
                dgvMonitor.DefaultCellStyle.SelectionBackColor = ThemeService.Selection;
                dgvMonitor.DefaultCellStyle.SelectionForeColor = Color.White;
                
                dgvMonitor.AlternatingRowsDefaultCellStyle.BackColor = altColor;
                dgvMonitor.AlternatingRowsDefaultCellStyle.ForeColor = textColor;
                dgvMonitor.AlternatingRowsDefaultCellStyle.SelectionBackColor = ThemeService.Selection;
                dgvMonitor.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

                // Header matches HeaderBg (Dark: Slate800, Light: White)
                dgvMonitor.ColumnHeadersDefaultCellStyle.BackColor = ThemeService.HeaderBg; 
                dgvMonitor.ColumnHeadersDefaultCellStyle.ForeColor = ThemeService.HeaderText;
                
                dgvMonitor.Refresh();
            }

            this.Invalidate(true);
        }

        private void InitializeStatsBar(Control parent)
        {
            panelStats = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 70, // Keep height
                BackColor = COLOR_BACKGROUND,
                Padding = new Padding(10, 8, 10, 5),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            parent.Controls.Add(panelStats);

            // Helper to create stat box
            Panel CreateStatBox(string title, string value, Color barColor, Color valueColor)
            {
                var p = new Panel { Width = 160, Height = 50, BackColor = COLOR_SURFACE, Margin = new Padding(0, 0, 15, 0) };
                p.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 4, BackColor = barColor });
                
                var lblT = new Label { Text = title, ForeColor = COLOR_TEXT_MUTED, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Location = new Point(12, 6), AutoSize = true };
                p.Controls.Add(lblT);
                
                var lblV = new Label { Text = value, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = valueColor, Location = new Point(10, 20), AutoSize = true };
                p.Controls.Add(lblV);
                
                return p;
            }

            // Total
            var pTotal = CreateStatBox(_loc.Get("Total"), "0", COLOR_PRIMARY, COLOR_TEXT_MAIN);
            lblTotalTitle = pTotal.Controls[1] as Label;
            lblTotal = pTotal.Controls[2] as Label;
            panelStats.Controls.Add(pTotal);

            // Online
            var pOnline = CreateStatBox(_loc.Get("Online"), "0", COLOR_BTN_PING, COLOR_ONLINE_TEXT);
            lblOnlineTitle = pOnline.Controls[1] as Label;
            lblOnline = pOnline.Controls[2] as Label;
            panelStats.Controls.Add(pOnline);

            // Offline
            var pOffline = CreateStatBox(_loc.Get("Offline"), "0", COLOR_BTN_DELETE, COLOR_OFFLINE_TEXT);
            lblOfflineTitle = pOffline.Controls[1] as Label;
            lblOffline = pOffline.Controls[2] as Label;
            panelStats.Controls.Add(pOffline);
        }

        private void InitializeDataGridView(Control parent)
        {
            dgvMonitor = new DataGridView
            {
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.None,
                BackgroundColor = COLOR_BACKGROUND, GridColor = COLOR_BORDER,
                RowHeadersVisible = false, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, ReadOnly = false, // ENABLE EDITING
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,

                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                MultiSelect = true, Font = new Font("Segoe UI", 9.5F),
                RowTemplate = { Height = 45 }, ScrollBars = ScrollBars.Both,
                AllowUserToResizeColumns = true,
                AllowUserToResizeRows = false,
                EnableHeadersVisualStyles = false
            };

            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, dgvMonitor, new object[] { true });

            dgvMonitor.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = COLOR_PRIMARY, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(0)
            };
            dgvMonitor.ColumnHeadersHeight = 45;
            dgvMonitor.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgvMonitor.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = COLOR_SURFACE, 
                ForeColor = COLOR_TEXT_MAIN,
                SelectionBackColor = COLOR_SELECTION, 
                SelectionForeColor = Color.Black, // Always black for selection on light blue
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(0),
                Font = new Font("Segoe UI", 9F)
            };

            dgvMonitor.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = COLOR_SURFACE_ALT 
            };

            dgvMonitor.CellFormatting += DgvMonitor_CellFormatting;
            dgvMonitor.CellPainting += DgvMonitor_CellPainting;
            dgvMonitor.CellDoubleClick += DgvMonitor_CellDoubleClick;
            dgvMonitor.ColumnHeaderMouseClick += DgvMonitor_ColumnHeaderMouseClick;

            // Columns
            string[] h = _loc.GetGridHeaders();
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = h[0], AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells, ReadOnly = true });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDeviceGroup", HeaderText = h[1], Width = 100, ReadOnly = true });
            
            // Dropdown: Device Name (Fill) - Enable sorting
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDeviceName", HeaderText = h[2], MinimumWidth = 100, DataPropertyName = "DeviceName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 150, ReadOnly = true });
            
            // Dropdown: Image (Text based)
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colImage", HeaderText = h[3], Width = 100, DataPropertyName = "ImagePath", ReadOnly = true });
            
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIpAddress", HeaderText = h[4], MinimumWidth = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 100, ReadOnly = true });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMacAddress", HeaderText = h[5], Width = 110, ReadOnly = true });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSerial", HeaderText = h[6], Width = 130, ReadOnly = true });
            
            // Dropdown: User - Enable sorting
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUser", HeaderText = h[7], Width = 130, DataPropertyName = "User", ReadOnly = true });
            
            // Dropdown: Location
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLocation", HeaderText = h[8], Width = 110, DataPropertyName = "Location", ReadOnly = true });
            
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = h[9], Width = 90, ReadOnly = true }); // Fixed width for manual resize
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLatency", HeaderText = h[10], AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells, ReadOnly = true });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLastCheck", HeaderText = h[11], Width = 135, ReadOnly = true });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLastOffline", HeaderText = h[12], Width = 135, ReadOnly = true });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOfflineDays", HeaderText = h[13], AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells, ReadOnly = true });
            dgvMonitor.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCreated", HeaderText = h[14], Width = 135, ReadOnly = true });

            parent.Controls.Add(dgvMonitor);
            dgvMonitor.BringToFront();
            
            // Event for Auto-Save
            dgvMonitor.CellEndEdit += DgvMonitor_CellEndEdit;
            dgvMonitor.DataError += (s, e) => { e.ThrowException = false; };
            
            // ComboBox commit logic removed as columns are now Text
            // dgvMonitor.CurrentCellDirtyStateChanged += ...

            progressBar = new ProgressBar
            {
                Size = new Size(120, 18),
                Style = ProgressBarStyle.Continuous,
                Visible = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            parent.Controls.Add(progressBar);
            dgvMonitor.Resize += (s, e) => {
                progressBar.Location = new Point(
                    dgvMonitor.Right - progressBar.Width - 25,
                    dgvMonitor.Bottom - progressBar.Height - 10
                );
            };
            progressBar.BringToFront();
        }

        private void InitializeSidebar()
        {
            // No header - cleaner design
            lbUsers = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F),
                ItemHeight = 50, // Two-line layout
                BackColor = ThemeService.SidebarBg, // Use correct Sidebar Bg
                ForeColor = ThemeService.SidebarText,
                Cursor = Cursors.Hand,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            lbUsers.DrawItem += LbUsers_DrawItem;
            lbUsers.SelectedIndexChanged += LbUsers_SelectedIndexChanged;
            
            panelSidebar.Controls.Add(lbUsers);

            /* User requested to hide this section
            var panelFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BackColor = ThemeService.BrandColor,
                Padding = new Padding(5)
            };

            var pbLogo = new PictureBox
            {
                Name = "pbLogo",
                Dock = DockStyle.Top, Height = 40,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = ThemeService.BrandColor
            };
            try {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo_company.png");
                if (File.Exists(logoPath)) pbLogo.Image = Image.FromFile(logoPath);
            } catch { }
            panelFooter.Controls.Add(pbLogo);

            lblAuthor = new Label
            {
                Text = _loc.Get("Author"),
                ForeColor = Color.FromArgb(148, 163, 184), // Slate 400
                Font = new Font("Segoe UI", 8F),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelFooter.Controls.Add(lblAuthor);
            lblAuthor.BringToFront();
            
            lblAuthor.BringToFront();
            
            panelSidebar.Controls.Add(panelFooter);
            */ 
        }

        private Button CreateButton(string text, Color bgColor, Point location, Size size, string icon = null)
        {
            var btn = new Button
            {
                Text = string.IsNullOrEmpty(icon) ? text : "", // Hide text if custom painting used
                Size = size, Location = location,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.White, BackColor = bgColor,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                Tag = text // Store text for tooltip or reference
            };
            btn.FlatAppearance.BorderSize = 0;

            if (!string.IsNullOrEmpty(icon))
            {
                btn.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    
                    var iconFont = new Font(Services.IconHelper.FontName, 10F);
                    var textFont = btn.Font;
                    
                    var iconSize = g.MeasureString(icon, iconFont);
                    var textSize = string.IsNullOrEmpty(text) ? SizeF.Empty : g.MeasureString(text, textFont);
                    
                    // Add small horizontal gap if both exist
                    float gap = (!string.IsNullOrEmpty(text)) ? 5 : 0;
                    float contentWidth = iconSize.Width + gap + textSize.Width;
                    
                    float startX = (btn.Width - contentWidth) / 2;
                    // Fix vertical centering: icon might be taller than text
                    float maxHeight = Math.Max(iconSize.Height, textSize.Height);
                    float startY = (btn.Height - maxHeight) / 2;

                    using (var brush = new SolidBrush(btn.ForeColor))
                    {
                        // Draw Icon
                        // Adjust Y slightly for icon font baseline if needed, but centering usually works
                        g.DrawString(icon, iconFont, brush, startX, startY + (maxHeight - iconSize.Height)/2);
                        
                        // Draw Text
                        if (!string.IsNullOrEmpty(text))
                        {
                            g.DrawString(text, textFont, brush, startX + iconSize.Width + gap, startY + (maxHeight - textSize.Height)/2 + 1);
                        }
                    }
                    iconFont.Dispose();
                };
            }
            return btn;
        }

        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("📋 Copy IP", null, (s, e) => CopySelectedIp());
            contextMenu.Items.Add("✏️ Edit", null, (s, e) => EditSelectedDevice());
            contextMenu.Items.Add("🔄 Ping", null, (s, e) => PingSelectedDevice());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("🗑️ Delete", null, (s, e) => BtnRemove_Click(null, null));
            dgvMonitor.ContextMenuStrip = contextMenu;
        }
        
        private void InitializeKeyboardShortcuts()
        {
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
        }

        private void ApplyLanguage(int index)
        {
            currentLanguageIndex = index;
            _loc.SetLanguage(index);
            
            this.Text = _loc.Get("AppTitle");
            if (lblTitle != null) lblTitle.Text = _loc.Get("Title");
            if (lblSubLabel != null) lblSubLabel.Text = _loc.Get("SubTitle");
            if (lblSidebarTitle != null) lblSidebarTitle.Text = _loc.Get("FilterTitle");
            if (btnClearFilter != null) btnClearFilter.Text = _loc.Get("ClearFilter");
            if (lblTotalTitle != null) lblTotalTitle.Text = _loc.Get("Total");
            if (lblOnlineTitle != null) lblOnlineTitle.Text = _loc.Get("Online");
            if (lblOfflineTitle != null) lblOfflineTitle.Text = _loc.Get("Offline");
            if (lblSearchPlaceholder != null) lblSearchPlaceholder.Text = _loc.Get("SearchPlaceholder");
            
            if (btnAdd != null) btnAdd.Text = _loc.Get("Add");
            // if (btnRemove != null) btnRemove.Text = _loc.Get("Remove");
            // if (btnClearAll != null) btnClearAll.Text = _loc.Get("ClearAll");
            // if (btnPingNow != null) btnPingNow.Text = _loc.Get("PingNow");
            // btnViewLogs and btnExportLog are removed from toolbar
            // Ensure Text is empty for custom painted buttons to prevent double drawing
            if (btnImport != null) { btnImport.Tag = _loc.Get("Import"); btnImport.Text = ""; btnImport.Invalidate(); }
            if (btnExport != null) btnExport.Text = _loc.Get("Export");
            if (btnSettings != null) btnSettings.Text = _loc.Get("Settings");
            if (lblAuthor != null) lblAuthor.Text = _loc.Get("Author");
            
            // Grid Headers
            if (dgvMonitor != null)
            {
                string[] h = _loc.GetGridHeaders();
                for(int i=0; i<h.Length && i<dgvMonitor.Columns.Count; i++)
                    dgvMonitor.Columns[i].HeaderText = h[i];
            }
            
            UpdateSidebar();
        }
        
        // ... (Include LbUsers_DrawItem, CboLanguage_DrawItem, DgvMonitor_CellPainting, etc.) ...
        // For brevity in this turn, I will assume I can continue appending in next turn or I will hit limit.
        // It's safer to put as much as possible.
        
        private void LbUsers_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            
            // Background
            // Use SidebarBg for unselected, Selection (or BrandColorLight) for selected
            // But usually SidebarBg is the base.
            Color bgColor = isSelected ? ThemeService.Selection : ThemeService.SidebarBg;
            // If Selection is Blue, we use it. If user preferred BrandColorLight (Slate700) for selection, keep it.
            // Let's stick closer to the theme system: Selection = Blue. 
            // Or better: Sidebar in Dark Mode usually highlights with a slightly lighter slate.
            Color selectedColor = ThemeService.IsDarkMode ? ThemeService.BrandColorLight : Color.FromArgb(203, 213, 225); // Slate 300
            
            using (var brush = new SolidBrush(isSelected ? selectedColor : ThemeService.SidebarBg))
                e.Graphics.FillRectangle(brush, e.Bounds);
            
            // Bottom border - Optional, maybe remove or match theme
            using (var pen = new Pen(ThemeService.Border)) 
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            string rawText = lbUsers.Items[e.Index].ToString();
            string userName = rawText.Contains("(") ? rawText.Substring(0, rawText.LastIndexOf("(")).Trim() : rawText;
            
            List<IpMonitor> filtered;
            
            // Fix for "Empty" user filtering
            if (string.IsNullOrEmpty(userName) || userName == "All" || userName == "Tất cả" || userName == "모두")
            {
                filtered = ipList;
            }
            else if (userName == _loc.Get("Empty"))
            {
                // Correctly filter for empty/null users when "Empty" (Chưa gán) is selected
                filtered = ipList.Where(ip => string.IsNullOrWhiteSpace(ip.User)).ToList();
            }
            else
            {
                filtered = ipList.Where(ip => ip.User == userName).ToList();
            }
                
            int total = filtered.Count;
            int online = filtered.Count(x => x.Status == "Online");
            int offline = filtered.Count(x => x.Status == "Offline" || x.Status == "Pending");

            // Line 1: Display name (bold, larger)
            string displayName = string.IsNullOrEmpty(userName) ? _loc.Get("All") : userName;
            using (var brush = new SolidBrush(ThemeService.SidebarText)) // Dynamic Text Color
                e.Graphics.DrawString(displayName, new Font("Segoe UI", 10F, FontStyle.Bold), brush, e.Bounds.X + 12, e.Bounds.Y + 6);

            // Line 2: Badges
            int badgeY = e.Bounds.Y + 28;
            int badgeX = e.Bounds.X + 12;
            
            // Total Label + Count
            string totalLabel = _loc.Get("Total");
            string totalText = $"{totalLabel}: {total}";
            
            // Total Badge: Slate 700 BG (BrandColorLight), White Text
            // Fix: Use dynamic text color for badge (White on Dark, Dark on Light)
            var badgeTextCol = ThemeService.IsDarkMode ? Color.White : ThemeService.BrandColor;
            DrawBadge(e.Graphics, totalText, ThemeService.BrandColorLight, badgeTextCol, badgeX, badgeY);
            
            // Adjust X
            var totalWidth = e.Graphics.MeasureString(totalText, new Font("Segoe UI", 8F)).Width + 15;
            badgeX += (int)totalWidth;

            // Online badge (Dark Green BG, Light Green Text)
            // Hardcoded Dark Mode colors because Sidebar is always Dark
            var onlineBg = Color.FromArgb(6, 78, 59);
            var onlineFg = Color.FromArgb(110, 231, 183);
            DrawBadge(e.Graphics, online.ToString(), onlineBg, onlineFg, badgeX, badgeY, Services.IconHelper.CheckMark);
            badgeX += 50; 
            
            // Offline badge (Dark Red BG, Light Red Text)
            var offlineBg = Color.FromArgb(127, 29, 29);
            var offlineFg = Color.FromArgb(252, 165, 165);
            DrawBadge(e.Graphics, offline.ToString(), offlineBg, offlineFg, badgeX, badgeY, Services.IconHelper.StatusErrorFull);
        }


        private void DrawBadge(Graphics g, string text, Color bg, Color fg, int x, int y, string icon = null)
        {
            var textFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            var textSize = g.MeasureString(text, textFont);
            
            int width = (int)textSize.Width + 10;
            int height = Math.Max((int)textSize.Height, 20) + 2; // Min height
            
            Font iconFont = null;
            int iconWidth = 0;
            
            if (!string.IsNullOrEmpty(icon))
            {
                iconFont = new Font(Services.IconHelper.FontName, 10F);
                var iconSize = g.MeasureString(icon, iconFont);
                iconWidth = (int)iconSize.Width;
                width += iconWidth + 5; // Add width for icon + spacing
                if (iconSize.Height > height) height = (int)iconSize.Height + 2;
            }

            var rect = new Rectangle(x, y, width, height);
            using (var brush = new SolidBrush(bg)) g.FillRectangle(brush, rect);
            using (var brush = new SolidBrush(fg))
            {
                int currentX = x + 5;
                if (!string.IsNullOrEmpty(icon) && iconFont != null)
                {
                    // Draw Icon
                    g.DrawString(icon, iconFont, brush, currentX, y + (height - iconFont.Height) / 2);
                    currentX += iconWidth + 2;
                }
                // Draw Text
                g.DrawString(text, textFont, brush, currentX, y + (height - textFont.Height) / 2);
            }
            if (iconFont != null) iconFont.Dispose();
        }
        
        private void CboLanguage_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var combo = sender as ComboBox;
            e.DrawBackground();
            // Flag logic simplified
             using (var brush = new SolidBrush(combo.ForeColor))
                e.Graphics.DrawString(combo.Items[e.Index].ToString(), combo.Font, brush, new Point(e.Bounds.X + 5, e.Bounds.Y + 2));
        }

        private void DgvMonitor_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
             e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Border);
             if (dgvMonitor.Columns[e.ColumnIndex].Name == "colStatus" && e.Value != null)
             {
                string status = e.Value.ToString();
                Color backColor = status == "Online" ? ThemeService.BtnSuccess : ThemeService.BtnDanger;
                if (status == "Pending") backColor = Color.Gray;
                
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var rect = e.CellBounds;
                rect.Inflate(-15, -10);
                using (var brush = new SolidBrush(backColor)) g.FillRectangle(brush, rect); // Simplified pill
                TextRenderer.DrawText(g, status.ToUpper(), new Font("Segoe UI", 8F, FontStyle.Bold), rect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
             }
             using (var p = new Pen(dgvMonitor.GridColor, 1))
                e.Graphics.DrawLine(p, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
             e.Handled = true;
        }
        
         private void DgvMonitor_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            // User requested Group color to match Row color, so no custom coloring needed.
        }
        
        private Color GetColorFromString(string text)
        {
             if (string.IsNullOrEmpty(text)) return Color.White;
             int hash = Math.Abs(text.GetHashCode());
             // Bright/Neon colors for Dark Theme Text
             Color[] colors = { 
                 Color.FromArgb(56, 189, 248), // Light Blue
                 Color.FromArgb(74, 222, 128), // Light Green
                 Color.FromArgb(250, 204, 21), // Yellow
                 Color.FromArgb(248, 113, 113), // Light Red
                 Color.FromArgb(192, 132, 252), // Violet
                 Color.FromArgb(251, 146, 60)   // Orange
             };
             return colors[hash % colors.Length];
        }

        private void InitializeTimer()
        {
            refreshTimer = new System.Windows.Forms.Timer { Interval = settings.RefreshIntervalSeconds * 1000 };
            refreshTimer.Tick += async (s, e) => await RefreshAllPings();
            if (settings.AutoRefresh) refreshTimer.Start();

            uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            uiTimer.Tick += (s, e) => { if (lblDateTime != null) lblDateTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"); };
            uiTimer.Start();
        }

        private void UpdateSidebar()
        {
            string selectedUser = null;
            if (lbUsers.SelectedItem != null)
            {
                string s = lbUsers.SelectedItem.ToString();
                if (s.Contains("(")) 
                    selectedUser = s.Substring(0, s.LastIndexOf("(")).Trim();
            }

            var userCounts = ipList
                .GroupBy(ip => string.IsNullOrWhiteSpace(ip.User) ? _loc.Get("Empty") : ip.User)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderBy(x => x.Name == _loc.Get("Empty") ? 0 : 1)
                .ThenBy(x => x.Name)
                .ToList();

            lbUsers.Items.Clear();
            lbUsers.Items.Add($"{_loc.Get("All")} ({ipList.Count})");
            
            foreach (var u in userCounts)
            {
                lbUsers.Items.Add($"{u.Name} ({u.Count})");
            }

            lbUsers.SelectedIndexChanged -= LbUsers_SelectedIndexChanged;
            if (selectedUser != null)
            {
                bool found = false;
                for (int i = 0; i < lbUsers.Items.Count; i++)
                {
                    string item = lbUsers.Items[i].ToString();
                    string name = item.Contains("(") ? item.Substring(0, item.LastIndexOf("(")).Trim() : item;
                    if (string.Equals(name, selectedUser, StringComparison.OrdinalIgnoreCase))
                    {
                        lbUsers.SelectedIndex = i;
                        found = true;
                        break;
                    }
                }
                if (!found && lbUsers.Items.Count > 0) lbUsers.SelectedIndex = 0;
            }
            else if (lbUsers.Items.Count > 0) lbUsers.SelectedIndex = 0;
            
            lbUsers.SelectedIndexChanged += LbUsers_SelectedIndexChanged;
            
            UpdateClearFilterButtonState();
            
            // Force redraw to update badge counts
            lbUsers.Invalidate();
        }

        private void LbUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateClearFilterButtonState();
            if (lbUsers.SelectedItem == null) return;
            string selected = lbUsers.SelectedItem.ToString();
            string userFilter = "";
            if (selected.Contains("(")) userFilter = selected.Substring(0, selected.LastIndexOf("(")).Trim();
            if (userFilter == _loc.Get("All")) userFilter = "";
            FilterGrid(userFilter);
        }

        private void UpdateClearFilterButtonState()
        {
            if (btnClearFilter == null) return;
            bool isFiltering = lbUsers.SelectedIndex > 0; 
            if (isFiltering)
            {
                btnClearFilter.Enabled = true;
                btnClearFilter.BackColor = ThemeService.BtnDanger; 
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
            RefreshDropdownSources();
            dgvMonitor.Rows.Clear();
            int idx = 1;
            var filteredList = string.IsNullOrEmpty(user) 
                ? ipList 
                : (user == _loc.Get("Empty") 
                    ? ipList.Where(ip => string.IsNullOrWhiteSpace(ip.User)).ToList() 
                    : ipList.Where(ip => ip.User.Equals(user, StringComparison.OrdinalIgnoreCase)).ToList());

            foreach (var ip in filteredList) AddRowToGrid(idx++, ip);
            UpdateStatusLabel();
        }

        private void FilterGridBySearch(string query)
        {
            RefreshDropdownSources();
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

            foreach (var ip in filteredList) AddRowToGrid(idx++, ip);
            UpdateStatusLabel();
        }

        private void AddRowToGrid(int idx, IpMonitor ip)
        {
             Image img = null;
             if (!string.IsNullOrEmpty(ip.ImagePath) && File.Exists(ip.ImagePath))
             {
                 try { img = Image.FromFile(ip.ImagePath); } catch { img = null; }
             }
             string offlineDays = "-";
             if (ip.Status == "Offline" && ip.LastOfflineTime.HasValue)
             {
                 offlineDays = FormatOfflineDuration(DateTime.Now - ip.LastOfflineTime.Value);
             }
             dgvMonitor.Rows.Add(
                idx,
                string.IsNullOrEmpty(ip.DeviceGroup) ? _loc.Get("Empty") : ip.DeviceGroup, 
                ip.DeviceName, ip.ImagePath, // Changed from img (Image) to ImagePath (String)
                ip.IpAddress, ip.MacAddress, ip.Serial, ip.User, ip.Location,
                ip.Status, ip.Latency > 0 ? $"{ip.Latency}ms" : "-",
                ip.LastCheckTime.ToString("dd/MM/yyyy HH:mm:ss"),
                ip.LastOfflineTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-",
                offlineDays,
                ip.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss")
            );
        }

        private string FormatOfflineDuration(TimeSpan span)
        {
            if (span.TotalDays >= 1) return $"{span.TotalDays:0.#} " + _loc.Get("Time_Day");
            if (span.TotalHours >= 1) return $"{span.TotalHours:0.#} " + _loc.Get("Time_Hour");
            return $"{span.TotalMinutes:0} " + _loc.Get("Time_Min");
        }

        private void DgvMonitor_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == 0 || e.ColumnIndex == 3) return;
            if (sortColumnIndex == e.ColumnIndex) sortAscending = !sortAscending;
            else { sortColumnIndex = e.ColumnIndex; sortAscending = true; }

            switch (e.ColumnIndex)
            {
                case 1: ipList = sortAscending ? ipList.OrderBy(x => x.DeviceGroup).ToList() : ipList.OrderByDescending(x => x.DeviceGroup).ToList(); break;
                case 2: ipList = sortAscending ? ipList.OrderBy(x => x.DeviceName).ToList() : ipList.OrderByDescending(x => x.DeviceName).ToList(); break;
                case 4: ipList = sortAscending ? ipList.OrderBy(x => x.IpAddress).ToList() : ipList.OrderByDescending(x => x.IpAddress).ToList(); break;
                case 5: ipList = sortAscending ? ipList.OrderBy(x => x.MacAddress).ToList() : ipList.OrderByDescending(x => x.MacAddress).ToList(); break;
                case 7: ipList = sortAscending ? ipList.OrderBy(x => x.User).ToList() : ipList.OrderByDescending(x => x.User).ToList(); break;
                case 9: ipList = sortAscending ? ipList.OrderBy(x => x.Status).ToList() : ipList.OrderByDescending(x => x.Status).ToList(); break;
                case 10: ipList = sortAscending ? ipList.OrderBy(x => x.Latency).ToList() : ipList.OrderByDescending(x => x.Latency).ToList(); break;
                case 14: ipList = sortAscending ? ipList.OrderBy(x => x.CreatedAt).ToList() : ipList.OrderByDescending(x => x.CreatedAt).ToList(); break;
                default: ipList = sortAscending ? ipList.OrderBy(x => x.Id).ToList() : ipList.OrderByDescending(x => x.Id).ToList(); break;
            }
            UpdateDataGridView();
            // Only set glyph on sortable columns
            foreach (DataGridViewColumn col in dgvMonitor.Columns)
            {
                if (col.SortMode != DataGridViewColumnSortMode.NotSortable)
                    col.HeaderCell.SortGlyphDirection = SortOrder.None;
            }
            if (dgvMonitor.Columns[e.ColumnIndex].SortMode != DataGridViewColumnSortMode.NotSortable)
                dgvMonitor.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = sortAscending ? SortOrder.Ascending : SortOrder.Descending;
        }

        private void DgvMonitor_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var ip = dgvMonitor.Rows[e.RowIndex].Cells["colIpAddress"].Value?.ToString();
            if (!string.IsNullOrEmpty(ip)) ShowEditDeviceForm(ip);
        }

        private void CopySelectedIp()
        {
            if (dgvMonitor.SelectedRows.Count > 0)
            {
                var ip = dgvMonitor.SelectedRows[0].Cells["colIpAddress"].Value?.ToString();
                if (!string.IsNullOrEmpty(ip)) { Clipboard.SetText(ip); MessageBox.Show($"Copied: {ip}"); }
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
                        using var ping = new System.Net.NetworkInformation.Ping();
                        var reply = await ping.SendPingAsync(monitor.IpAddress, settings.PingTimeoutMs);
                        monitor.UpdateStatus(reply.Status == System.Net.NetworkInformation.IPStatus.Success, reply.RoundtripTime);
                        db.UpdateIpMonitor(monitor);
                        UpdateDataGridView();
                    }
                    catch { }
                }
            }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            using (var form = new SettingsForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    settings = db.GetSettings();
                    ThemeService.SetMode(settings.IsDarkMode);
                    InitializeTimer();
                    ApplyTheme();
                    ApplyLanguage(settings.Language);
                    FilterGrid("");
                }
            }
        }

        private void BtnViewLogs_Click(object sender, EventArgs e)
        {
            using (var form = new LogViewerForm())
            {
                form.ShowDialog();
                unreadLogCount = 0;
                // No invalidation needed for simple label
            }
        }


        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.N) { BtnAdd_Click(null, null); e.Handled = true; }
            else if (e.KeyCode == Keys.Delete) { BtnRemove_Click(null, null); e.Handled = true; }
            else if (e.KeyCode == Keys.F5) { _ = RefreshAllPings(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.F) { txtSearch?.Focus(); e.Handled = true; }
        }
        
        private void UpdateStatusLabel()
        {
            int online = ipList.Count(x => x.Status == "Online");
            int offline = ipList.Count(x => x.Status == "Offline" || x.Status == "Pending");
            if (lblOnline != null) lblOnline.Text = $"{online}";
            if (lblOffline != null) lblOffline.Text = $"{offline}";
            if (lblTotal != null) lblTotal.Text = $"{ipList.Count}";
        }
        
        private void ResetGridViewLayout()
        {
             if (dgvMonitor == null) return;
             dgvMonitor.RowTemplate.Height = 45;
             foreach (DataGridViewRow row in dgvMonitor.Rows) row.Height = 45;
             ipList = ipList.OrderBy(x => x.Id).ToList();
             UpdateDataGridView();
        }
        
        private void UpdateDataGridView()
        {
             // Simple re-filter based on current state
             string user = null;
             if (lbUsers != null && lbUsers.SelectedIndex > 0)
             {
                 string s = lbUsers.SelectedItem.ToString();
                 if (s.Contains("(")) user = s.Substring(0, s.LastIndexOf("(")).Trim();
                 if (user == _loc.Get("All")) user = null;
             }
             if (txtSearch != null && !string.IsNullOrEmpty(txtSearch.Text)) FilterGridBySearch(txtSearch.Text);
             else FilterGrid(user);
        }

        private async void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var form = new Form())
            {
                form.Text = _loc.Get("Title_Add");
                form.Size = new Size(500, 420);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.BackColor = ThemeService.IsDarkMode ? ThemeService.Background : ThemeService.SidebarBg; // Contrast
                form.MaximizeBox = false;

                int y = 20;
                int inputX = 140;
                int inputWidth = 320;

                form.Controls.Add(new Label { Text = _loc.Get("Lbl_Group"), ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var cboGroup = new ComboBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, DropDownStyle = ComboBoxStyle.DropDown };
                cboGroup.Items.AddRange(new[] { "Điện thoại", "Máy tính", "Laptop", "Camera IP", "Khác" });
                form.Controls.Add(cboGroup);
                y += 35;

                form.Controls.Add(new Label { Text = _loc.Get("Lbl_Name"), ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtName = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN };
                form.Controls.Add(txtName);
                y += 35;

                form.Controls.Add(new Label { Text = _loc.Get("Lbl_Image"), ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtImage = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth - 80, 25), Font = new Font("Segoe UI", 9F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, ReadOnly = true };
                form.Controls.Add(txtImage);
                var btnBrowse = CreateButton(_loc.Get("Btn_Browse"), Color.FromArgb(80, 80, 80), new Point(inputX + inputWidth - 70, y), new Size(70, 25));
                btnBrowse.Click += (s, ev) => { using (var ofd = new OpenFileDialog()) { ofd.Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"; if (ofd.ShowDialog() == DialogResult.OK) txtImage.Text = ofd.FileName; } };
                form.Controls.Add(btnBrowse);
                y += 35;

                form.Controls.Add(new Label { Text = _loc.Get("Lbl_IP"), ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtIp = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Consolas", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN };
                form.Controls.Add(txtIp);
                y += 35;

                form.Controls.Add(new Label { Text = _loc.Get("Lbl_MAC"), ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtMac = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Consolas", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN };
                form.Controls.Add(txtMac);
                y += 35;

                form.Controls.Add(new Label { Text = _loc.Get("Lbl_Serial"), ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtSerial = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN };
                form.Controls.Add(txtSerial);
                y += 35;

                form.Controls.Add(new Label { Text = _loc.Get("Lbl_User"), ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtUser = new ComboBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, DropDownStyle = ComboBoxStyle.DropDown };
                txtUser.Items.AddRange(ipList.Select(x => x.User).Where(u => !string.IsNullOrEmpty(u)).Distinct().OrderBy(u => u).ToArray());
                form.Controls.Add(txtUser);
                y += 35;

                form.Controls.Add(new Label { Text = _loc.Get("Lbl_Location"), ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });
                var txtLoc = new ComboBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, DropDownStyle = ComboBoxStyle.DropDown };
                txtLoc.Items.AddRange(ipList.Select(x => x.Location).Where(l => !string.IsNullOrEmpty(l)).Distinct().OrderBy(l => l).ToArray());
                form.Controls.Add(txtLoc);
                y += 50;

                var btnSave = CreateButton(_loc.Get("Add"), ThemeService.BtnSuccess, new Point(140, y), new Size(120, 35), IconHelper.Save);
                var btnCancel = CreateButton(_loc.Get("Btn_Cancel"), ThemeService.BtnDanger, new Point(280, y), new Size(100, 35), IconHelper.Cancel);

                bool saved = false;
                btnSave.Click += (s, ev) =>
                {
                    string ip = txtIp.Text.Trim();
                    if (!ValidateIpAddress(ip, out _)) { MessageBox.Show(_loc.Get("Msg_IpInvalid")); txtIp.Focus(); return; }
                    if (ipList.Any(x => x.IpAddress.Equals(ip, StringComparison.OrdinalIgnoreCase))) { MessageBox.Show(_loc.Get("Msg_IpExists")); return; }

                    var monitor = new IpMonitor
                    {
                        DeviceGroup = cboGroup.Text.Trim(), DeviceName = txtName.Text.Trim(), ImagePath = txtImage.Text.Trim(),
                        IpAddress = ip, MacAddress = txtMac.Text.Trim(), Serial = txtSerial.Text.Trim(), User = txtUser.Text.Trim(), Location = txtLoc.Text.Trim()
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
                if (saved) { UpdateDataGridView(); UpdateSidebar(); await RefreshAllPings(); }
            }
        }

        private void ShowEditDeviceForm(string currentIp)
        {
            var ip = ipList.FirstOrDefault(x => x.IpAddress == currentIp);
            if (ip == null) return;
            using (var form = new Form())
            {
                form.Text = $"{_loc.Get("Title_Edit")}: {ip.DeviceName} ({ip.IpAddress})";
                form.Size = new Size(500, 420);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.BackColor = ThemeService.IsDarkMode ? ThemeService.Background : ThemeService.SidebarBg; // Contrast
                form.MaximizeBox = false;

                int y = 20; int inputX = 140; int inputWidth = 320;
                void AddLabel(string text) => form.Controls.Add(new Label { Text = text, ForeColor = COLOR_TEXT_MAIN, Location = new Point(20, y + 3), AutoSize = true });

                AddLabel(_loc.Get("Lbl_Group"));
                var cboGroup = new ComboBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, DropDownStyle = ComboBoxStyle.DropDown, Text = ip.DeviceGroup };
                cboGroup.Items.AddRange(new[] { "Điện thoại", "Máy tính", "Laptop", "Camera IP", "Khác" });
                form.Controls.Add(cboGroup); y += 35;

                AddLabel(_loc.Get("Lbl_Name"));
                var txtName = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, Text = ip.DeviceName };
                form.Controls.Add(txtName); y += 35;

                AddLabel(_loc.Get("Lbl_Image"));
                var txtImage = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth - 80, 25), Font = new Font("Segoe UI", 9F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, ReadOnly = true, Text = ip.ImagePath };
                form.Controls.Add(txtImage);
                var btnBrowse = CreateButton("...", Color.FromArgb(80, 80, 80), new Point(inputX + inputWidth - 70, y), new Size(70, 25));
                btnBrowse.Click += (s, ev) => { using (var ofd = new OpenFileDialog()) { ofd.Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"; if (ofd.ShowDialog() == DialogResult.OK) txtImage.Text = ofd.FileName; } };
                form.Controls.Add(btnBrowse); y += 35;

                AddLabel(_loc.Get("Lbl_IP"));
                var txtIp = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Consolas", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, ReadOnly = true, Text = ip.IpAddress };
                form.Controls.Add(txtIp); y += 35;

                AddLabel(_loc.Get("Lbl_MAC"));
                var txtMac = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Consolas", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, Text = ip.MacAddress };
                form.Controls.Add(txtMac); y += 35;

                AddLabel(_loc.Get("Lbl_Serial"));
                var txtSerial = new TextBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, Text = ip.Serial };
                form.Controls.Add(txtSerial); y += 35;

                AddLabel("User:");
                var txtUser = new ComboBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, Text = ip.User, DropDownStyle = ComboBoxStyle.DropDown };
                // Populate distinct users
                txtUser.Items.AddRange(ipList.Select(x => x.User).Where(u => !string.IsNullOrEmpty(u)).Distinct().OrderBy(u => u).ToArray());
                form.Controls.Add(txtUser); y += 35;

                AddLabel(_loc.Get("Lbl_Location"));
                var txtLoc = new ComboBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 25), Font = new Font("Segoe UI", 10F), BackColor = ThemeService.IsDarkMode ? ThemeService.Background : Color.White, ForeColor = COLOR_TEXT_MAIN, Text = ip.Location, DropDownStyle = ComboBoxStyle.DropDown };
                // Populate distinct locations
                txtLoc.Items.AddRange(ipList.Select(x => x.Location).Where(l => !string.IsNullOrEmpty(l)).Distinct().OrderBy(l => l).ToArray());
                form.Controls.Add(txtLoc); y += 45;

                var btnSave = CreateButton(_loc.Get("Btn_Update"), ThemeService.BtnSuccess, new Point(140, y), new Size(100, 35), IconHelper.Save);
                btnSave.Click += (s, ev) => {
                    ip.DeviceGroup = cboGroup.Text.Trim(); ip.DeviceName = txtName.Text.Trim(); ip.ImagePath = txtImage.Text.Trim();
                    ip.MacAddress = txtMac.Text.Trim(); ip.Serial = txtSerial.Text.Trim(); ip.User = txtUser.Text.Trim(); ip.Location = txtLoc.Text.Trim();
                    db.UpdateIpMonitor(ip); UpdateDataGridView(); form.Close();
                };
                form.Controls.Add(btnSave);
                var btnCancel = CreateButton(_loc.Get("Btn_Cancel"), ThemeService.BtnDanger, new Point(260, y), new Size(100, 35), IconHelper.Cancel);
                btnCancel.Click += (s, ev) => form.Close();
                form.Controls.Add(btnCancel);
                form.AcceptButton = btnSave;
                form.ShowDialog(this);
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            int count = dgvMonitor.SelectedRows.Count;
            if (count == 0 || MessageBox.Show($"Delete {count} items?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            foreach (DataGridViewRow row in dgvMonitor.SelectedRows)
            {
                var ip = row.Cells["colIpAddress"].Value?.ToString();
                if (!string.IsNullOrEmpty(ip)) {
                     var m = ipList.FirstOrDefault(x => x.IpAddress == ip);
                     if(m!=null) { db.DeleteIpMonitor(ip); ipList.Remove(m); AddLog(ip, "", "Removed", "Manual", m.DeviceName, m.User); }
                }
            }
            UpdateDataGridView();
        }

        private void BtnClearAll_Click(object sender, EventArgs e)
        {
            if (ipList.Count > 0 && MessageBox.Show("Delete ALL?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                db.DeleteAllIpMonitors(); ipList.Clear(); UpdateDataGridView();
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
             using (var dialog = new OpenFileDialog { Filter = "Excel Files (*.xlsx)|*.xlsx" }) {
                if (dialog.ShowDialog() == DialogResult.OK) {
                    try {
                        var data = ExcelHelper.ReadExcelToDataTable(dialog.FileName);
                        if (data.Rows.Count > 0) using (var form = new ImportPreviewForm(data, Path.GetFileName(dialog.FileName))) { if (form.ShowDialog(this) == DialogResult.OK) ImportFromDataTable(form.ResultData); }
                    } catch (Exception ex) { MessageBox.Show(ex.Message); }
                }
             }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
             if (ipList.Count == 0) return;
             using (var dialog = new SaveFileDialog { Filter = "Excel Files (*.xlsx)|*.xlsx", FileName = $"PingMonitor_{DateTime.Now:yyyyMMdd}.xlsx" }) {
                 if (dialog.ShowDialog() == DialogResult.OK) {
                     try { ExportToExcel(dialog.FileName); MessageBox.Show("Done!"); } catch (Exception ex) { MessageBox.Show(ex.Message); }
                 }
             }
        }



        private void TxtIpAddress_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter) { BtnAdd_Click(sender, e); e.Handled = true; }
        }

        private void RefreshGridRowValues()
        {
            if (dgvMonitor.Rows.Count == 0) return;
            var ipMap = ipList.ToDictionary(x => x.IpAddress);
            foreach (DataGridViewRow row in dgvMonitor.Rows)
            {
                var ipAddr = row.Cells["colIpAddress"].Value?.ToString();
                if (string.IsNullOrEmpty(ipAddr) || !ipMap.ContainsKey(ipAddr)) continue;

                var ip = ipMap[ipAddr];
                UpdateCellIfChanged(row.Cells["colStatus"], ip.Status);
                UpdateCellIfChanged(row.Cells["colLatency"], ip.Latency > 0 ? $"{ip.Latency}ms" : "-");
                UpdateCellIfChanged(row.Cells["colLastCheck"], ip.LastCheckTime.ToString("dd/MM/yyyy HH:mm:ss"));
                UpdateCellIfChanged(row.Cells["colLastOffline"], ip.LastOfflineTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "-");

                string offlineDays = "-";
                if (ip.Status == "Offline" && ip.LastOfflineTime.HasValue) offlineDays = FormatOfflineDuration(DateTime.Now - ip.LastOfflineTime.Value);
                UpdateCellIfChanged(row.Cells["colOfflineDays"], offlineDays);
            }
            UpdateStatusLabel();
        }

        private void UpdateCellIfChanged(DataGridViewCell cell, string newValue)
        {
             if (cell.Value == null || cell.Value.ToString() != newValue) cell.Value = newValue;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
             // Log shutdown? Handled in MainForm or logic?
             // AddLog is in MainForm.cs. So we can call it.
             // But AddLog is private?
             // Main partial can access private members.
             // However, AddLog is defining in MainForm.cs.
             // We'll call it. THIS IS FINE.
             // But wait, if AddLog is private in Part 1, Part 2 can access it? Yes.
             // AddLog("System", "", "Shutdown", "App closed");
             // refreshTimer check
             refreshTimer?.Stop();
             refreshTimer?.Dispose();
             base.OnFormClosing(e);
        }

        private void InitializeComponent() { components = new System.ComponentModel.Container(); AutoScaleMode = AutoScaleMode.Font; }
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && components != null) components.Dispose(); base.Dispose(disposing); }
        private void RefreshDropdownSources()
        {
            if (dgvMonitor == null) return;
            
            // Helper to populate column
            void Populate(string colName, IEnumerable<string> items)
            {
                if (dgvMonitor.Columns[colName] is DataGridViewComboBoxColumn combo)
                {
                    var distinct = items.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
                    combo.DataSource = distinct;
                }
            }

            Populate("colDeviceName", ipList.Select(x => x.DeviceName));
            Populate("colImage", ipList.Select(x => x.ImagePath));
            Populate("colUser", ipList.Select(x => x.User));
            Populate("colLocation", ipList.Select(x => x.Location));
        }

        private void DgvMonitor_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                var row = dgvMonitor.Rows[e.RowIndex];
                // Assuming ipList order matches grid index for initial load, BUT filtering breaks this.
                // Better to get the object from the Row itself if bound, or find it by IP.
                // Since this grid is manually built via AddRowToGrid (likely), we used row.Tag?
                // Checking AddRowToGrid... It's not visible here, but I usually create rows manually.
                
                string ipAddr = row.Cells["colIpAddress"].Value?.ToString();
                var ip = ipList.FirstOrDefault(x => x.IpAddress == ipAddr);
                
                if (ip != null)
                {
                    bool changed = false;
                    string colName = dgvMonitor.Columns[e.ColumnIndex].Name;
                    var newVal = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";

                    if (colName == "colDeviceName" && ip.DeviceName != newVal) { ip.DeviceName = newVal; changed = true; }
                    if (colName == "colImage" && ip.ImagePath != newVal) { ip.ImagePath = newVal; changed = true; }
                    if (colName == "colUser" && ip.User != newVal) { ip.User = newVal; changed = true; }
                    if (colName == "colLocation" && ip.Location != newVal) { ip.Location = newVal; changed = true; }

                    if (changed)
                    {
                        db.UpdateIpMonitor(ip);
                        UpdateSidebar(); // Refresh sidebar when user data changes
                        // Refresh dropdowns to include new value if it was a custom one? 
                        // Actually ComboBoxColumn prevents custom values unless DropDownStyle is changed.
                        // For now we only select existing.
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail or log
                Console.WriteLine(ex.Message);
            }
        }
        private void Form_Resize(object sender, EventArgs e)
        {
             if (splitContainer == null) return;
             
            if (this.Width < 800)
            {
                if (!splitContainer.Panel1Collapsed) splitContainer.Panel1Collapsed = true;
            }
            
            // Re-center Header Search Bar
            if (this.Tag is Panel searchPanel && searchPanel.Tag?.ToString() == "HeaderSearch" && searchPanel.Parent != null)
            {
                searchPanel.Location = new Point((searchPanel.Parent.Width - searchPanel.Width) / 2, (searchPanel.Parent.Height - searchPanel.Height) / 2);
            }
        }
    }
}
