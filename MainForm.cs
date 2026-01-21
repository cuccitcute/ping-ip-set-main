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
using PingMonitor.Services;

namespace PingMonitor
{
    public partial class MainForm : Form
    {
        private LocalizationService _loc;
        
        // ========== DATA ==========
        private List<IpMonitor> ipList = new List<IpMonitor>();
        private bool isRefreshing = false;
        private AppSettings settings;
        private DatabaseHelper db;
        private PingService pingService;

        public MainForm()
        {
            // Initialize Services
            db = new DatabaseHelper();
            pingService = new PingService();
            _loc = new LocalizationService();
            settings = db.GetSettings(); 
            
            Application.EnableVisualStyles(); // Ensure styles needed? usually in Program.cs
            
            InitializeComponent(); 
            ThemeService.SetMode(settings.IsDarkMode);

            // Load Data
            ipList = db.GetAllIpMonitors();

            InitializeUI();
            InitializeTimer();
            
            try
            {
                var iconPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "icon.png");
                if (File.Exists(iconPath))
                {
                    using (var bmp = new Bitmap(iconPath))
                        this.Icon = Icon.FromHandle(bmp.GetHicon());
                }
            } catch {}
            
            FilterGrid("");
            UpdateSidebar(); // Initialize sidebar with user list
            AddLog("System", "", "Started", "Started");

            // PERFORMANCE FIX: Trigger initial refresh AFTER UI is shown
            this.Shown += async (s, e) => {
                UpdateSidebar(); // Ensure sidebar is populated before ping
                await RefreshAllPings();
            };
        }

        // ============================================
        // VALIDATE IP
        // ============================================
        private bool ValidateIpAddress(string input, out string errorMessage)
        {
            errorMessage = "";
            if (string.IsNullOrWhiteSpace(input))
            {
                errorMessage = "Invalid IP!";
                return false;
            }

            input = input.Trim();
            if (input.Length > 255)
            {
                errorMessage = "IP too long!";
                return false;
            }

            string ipv4Pattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            if (Regex.IsMatch(input, ipv4Pattern)) return true;

            if (IPAddress.TryParse(input, out IPAddress ipAddr))
                if (ipAddr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    return true;

            string hostnamePattern = @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)*[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?$";
            if (Regex.IsMatch(input, hostnamePattern)) return true;

            errorMessage = "Invalid Format!";
            return false;
        }

        // ============================================
        // ADD LOG
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

            // Notification Logic (UI update handled in UI part via partial access if needed, or we invoke btnViewLogs)
            System.Threading.Interlocked.Increment(ref unreadLogCount);
            // Badge removed as per request
            // if (btnFloatingLog != null && !btnFloatingLog.IsDisposed) ...
        }

        // ============================================
        // LOGIC: IMPORT
        // ============================================
        private void ImportFromDataTable(System.Data.DataTable data)
        {
            int added = 0, skipped = 0, invalid = 0;
            string[] headers = { "ip", "ipaddress", "address", "location", "vitri" };

            int ipColIdx = -1, locColIdx = -1, nameColIdx = -1, groupColIdx = -1, serialColIdx = -1, userColIdx = -1;

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

            if (ipColIdx == -1) ipColIdx = 0;

            foreach (System.Data.DataRow row in data.Rows)
            {
                string ip = row[ipColIdx]?.ToString().Trim();
                if (string.IsNullOrEmpty(ip) || headers.Any(h => ip.ToLower().Contains(h))) continue;

                if (!ValidateIpAddress(ip, out _)) { invalid++; continue; }
                if (ipList.Any(x => x.IpAddress.Equals(ip, StringComparison.OrdinalIgnoreCase))) { skipped++; continue; }

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
            UpdateSidebar();
            UpdateStatusLabel();
            MessageBox.Show($"Import Complete!\n\nAdded: {added} | Skipped: {skipped} | Invalid: {invalid}", "Result");
            _ = RefreshAllPings();
        }

        // ============================================
        // LOGIC: EXPORT
        // ============================================
        private void ExportToExcel(string filePath)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.Combine(tempDir, "_rels"));
            Directory.CreateDirectory(Path.Combine(tempDir, "xl"));
            Directory.CreateDirectory(Path.Combine(tempDir, "xl", "_rels"));
            Directory.CreateDirectory(Path.Combine(tempDir, "xl", "worksheets"));

            File.WriteAllText(Path.Combine(tempDir, "[Content_Types].xml"), @"<?xml version=""1.0"" encoding=""UTF-8""?><Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types""><Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/><Default Extension=""xml"" ContentType=""application/xml""/><Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/><Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/><Override PartName=""/xl/sharedStrings.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml""/></Types>");
            File.WriteAllText(Path.Combine(tempDir, "_rels", ".rels"), @"<?xml version=""1.0"" encoding=""UTF-8""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/></Relationships>");
            File.WriteAllText(Path.Combine(tempDir, "xl", "workbook.xml"), @"<?xml version=""1.0"" encoding=""UTF-8""?><workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships""><sheets><sheet name=""Devices"" sheetId=""1"" r:id=""rId1""/></sheets></workbook>");
            File.WriteAllText(Path.Combine(tempDir, "xl", "_rels", "workbook.xml.rels"), @"<?xml version=""1.0"" encoding=""UTF-8""?><Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships""><Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/><Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"" Target=""sharedStrings.xml""/></Relationships>");

            var strings = new List<string>();
            var rows = new StringBuilder();

            string[] headers = { "STT", "Nhom thiet bi", "Ten thiet bi", "Hinh anh", "IP", "MAC", "Serial/IMEI", "Nguoi dung", "Vi tri", "Trang thai", "Do tre (ms)", "Thoi gian kiem tra", "Thoi gian Offline", "Ngay tao" };
            rows.Append("<row r=\"1\">");
            for (int i = 0; i < headers.Length; i++)
            {
                int strIdx = strings.Count;
                strings.Add(headers[i]);
                rows.Append($"<c r=\"{(char)('A' + i)}1\" t=\"s\"><v>{strIdx}</v></c>");
            }
            rows.Append("</row>");

            int rowNum = 2;
            foreach (var ip in ipList)
            {
                rows.Append($"<row r=\"{rowNum}\">");
                rows.Append($"<c r=\"A{rowNum}\"><v>{rowNum - 1}</v></c>");
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

            File.WriteAllText(Path.Combine(tempDir, "xl", "worksheets", "sheet1.xml"), $@"<?xml version=""1.0"" encoding=""UTF-8""?><worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""><sheetData>{rows}</sheetData></worksheet>");
            var ssXml = new StringBuilder(@"<?xml version=""1.0"" encoding=""UTF-8""?><sst xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");
            foreach (var s in strings) ssXml.Append($"<si><t>{System.Security.SecurityElement.Escape(s)}</t></si>");
            ssXml.Append("</sst>");
            File.WriteAllText(Path.Combine(tempDir, "xl", "sharedStrings.xml"), ssXml.ToString());

            if (File.Exists(filePath)) File.Delete(filePath);
            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, filePath);
            Directory.Delete(tempDir, true);
        }

        // ============================================
        // LOGIC: PING SERVICE
        // ============================================
        private async Task RefreshAllPings()
        {
            if (isRefreshing || ipList.Count == 0) return;
            isRefreshing = true;
            if (progressBar != null) { progressBar.Visible = true; progressBar.Maximum = ipList.Count; progressBar.Value = 0; }

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
                            var (isSuccess, avgLatency) = await pingService.PingHostAsync(ip.IpAddress, settings.PingTimeoutMs, settings.PingRetryCount);
                            if (isSuccess) { ip.ConsecutiveFailures = 0; ip.UpdateStatus(true, avgLatency); }
                            else { ip.ConsecutiveFailures++; if (ip.ConsecutiveFailures >= settings.OfflineThreshold) ip.UpdateStatus(false, 0); }
                        }
                        catch { ip.ConsecutiveFailures++; if (ip.ConsecutiveFailures >= settings.OfflineThreshold) ip.UpdateStatus(false, 0); }
                        finally
                        {
                            semaphore.Release();
                            Interlocked.Increment(ref completed);
                            if (progressBar != null) this.BeginInvoke(new Action(() => { if (completed <= progressBar.Maximum) progressBar.Value = completed; }));
                        }
                    }).ToArray();

                    await Task.WhenAll(tasks);

                    foreach (var ip in ipList)
                    {
                        if (prevStates.TryGetValue(ip.IpAddress, out string prev))
                        {
                            if (prev != ip.Status && prev != "Pending") AddLog(ip.IpAddress, ip.Location, ip.Status, $"{prev} -> {ip.Status}", ip.DeviceName, ip.User);
                        }
                    }
                    db.UpdateAllIpMonitorStatuses(ipList);
                }
                if (this.InvokeRequired) this.Invoke(new Action(() => { RefreshGridRowValues(); UpdateSidebar(); }));
                else { RefreshGridRowValues(); UpdateSidebar(); }
            }
            finally
            {
                isRefreshing = false;
                if (progressBar != null) progressBar.Visible = false;
            }
        }
    }
}
