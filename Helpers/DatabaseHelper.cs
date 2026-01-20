using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using PingMonitor.Models;

namespace PingMonitor.Helpers
{
    // ============================================
    // DATABASE HELPER
    // ============================================
    public class DatabaseHelper
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        public DatabaseHelper()
        {
            // Database file trong thư mục ứng dụng
            string appFolder = Path.GetDirectoryName(Application.ExecutablePath);
            _dbPath = Path.Combine(appFolder, "pingmonitor.db");
            _connectionString = $"Data Source={_dbPath}";
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Tạo bảng IpMonitors với cấu trúc mới
                var cmdIp = connection.CreateCommand();
                cmdIp.CommandText = @"
                    CREATE TABLE IF NOT EXISTS IpMonitors (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DeviceGroup TEXT DEFAULT '',
                        DeviceName TEXT DEFAULT '',
                        ImagePath TEXT DEFAULT '',
                        IpAddress TEXT NOT NULL UNIQUE,
                        MacAddress TEXT DEFAULT '',
                        Location TEXT DEFAULT '',
                        Status TEXT DEFAULT 'Pending',
                        Latency INTEGER DEFAULT 0,
                        LastCheckTime TEXT,
                        LastOfflineTime TEXT,
                        Serial TEXT DEFAULT '',
                        User TEXT DEFAULT '',
                        CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                    );";
                cmdIp.ExecuteNonQuery();

                // Migrate: thêm cột mới nếu bảng đã tồn tại
                var cmdMigrate = connection.CreateCommand();
                cmdMigrate.CommandText = "PRAGMA table_info(IpMonitors)";
                var columns = new List<string>();
                using (var reader = cmdMigrate.ExecuteReader())
                {
                    while (reader.Read()) columns.Add(reader.GetString(1));
                }
                
                // Thêm tất cả các cột mới nếu chưa có
                string[] newColumns = { "DeviceGroup", "DeviceName", "ImagePath", "MacAddress", "LastOfflineTime", "Serial", "User" };
                foreach (var col in newColumns)
                {
                    if (!columns.Contains(col))
                    {
                        try
                        {
                            var addCol = connection.CreateCommand();
                            addCol.CommandText = $"ALTER TABLE IpMonitors ADD COLUMN {col} TEXT DEFAULT ''";
                            addCol.ExecuteNonQuery();
                        }
                        catch { /* Column might already exist */ }
                    }
                }

                // Tạo bảng Logs
                var cmdLog = connection.CreateCommand();
                cmdLog.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Logs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL,
                        IpAddress TEXT,
                        Location TEXT,
                        Event TEXT,
                        Details TEXT,
                        DeviceName TEXT DEFAULT '',
                        User TEXT DEFAULT ''
                    );";
                cmdLog.ExecuteNonQuery();

                // Migrate Logs: thêm cột mới nếu bảng đã tồn tại
                var cmdLogMigrate = connection.CreateCommand();
                cmdLogMigrate.CommandText = "PRAGMA table_info(Logs)";
                var logColumns = new List<string>();
                using (var reader = cmdLogMigrate.ExecuteReader())
                {
                    while (reader.Read()) logColumns.Add(reader.GetString(1));
                }
                
                string[] newLogColumns = { "DeviceName", "User" };
                foreach (var col in newLogColumns)
                {
                    if (!logColumns.Contains(col))
                    {
                        try
                        {
                            var addCol = connection.CreateCommand();
                            addCol.CommandText = $"ALTER TABLE Logs ADD COLUMN {col} TEXT DEFAULT ''";
                            addCol.ExecuteNonQuery();
                        }
                        catch { }
                    }
                }

                // Tạo bảng Settings
                var cmdSettings = connection.CreateCommand();
                cmdSettings.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT
                    );";
                cmdSettings.ExecuteNonQuery();
            }
        }

        // ========== IP MONITORS ==========
        public List<IpMonitor> GetAllIpMonitors()
        {
            var list = new List<IpMonitor>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id, DeviceGroup, DeviceName, ImagePath, IpAddress, MacAddress, Location, Status, Latency, LastCheckTime, LastOfflineTime, Serial, User, CreatedAt FROM IpMonitors ORDER BY Id";
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new IpMonitor
                        {
                            Id = reader.GetInt32(0),
                            DeviceGroup = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            DeviceName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            ImagePath = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            IpAddress = reader.GetString(4),
                            MacAddress = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Location = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            Status = reader.IsDBNull(7) ? "Pending" : reader.GetString(7),
                            Latency = reader.IsDBNull(8) ? 0 : reader.GetInt64(8),
                            LastCheckTime = reader.IsDBNull(9) ? DateTime.Now : DateTime.Parse(reader.GetString(9)),
                            LastOfflineTime = reader.IsDBNull(10) ? null : (DateTime?)DateTime.Parse(reader.GetString(10)),
                            Serial = reader.IsDBNull(11) ? "" : reader.GetString(11),
                            User = reader.IsDBNull(12) ? "" : reader.GetString(12),
                            CreatedAt = reader.IsDBNull(13) ? DateTime.Now : DateTime.Parse(reader.GetString(13))
                        });
                    }
                }
            }
            return list;
        }

        public void AddIpMonitor(IpMonitor ip)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO IpMonitors (DeviceGroup, DeviceName, ImagePath, IpAddress, MacAddress, Location, Status, Latency, LastCheckTime, Serial, User, CreatedAt)
                    VALUES ($group, $name, $image, $ip, $mac, $loc, $status, $lat, $check, $serial, $user, $created)";
                cmd.Parameters.AddWithValue("$group", ip.DeviceGroup ?? "");
                cmd.Parameters.AddWithValue("$name", ip.DeviceName ?? "");
                cmd.Parameters.AddWithValue("$image", ip.ImagePath ?? "");
                cmd.Parameters.AddWithValue("$ip", ip.IpAddress);
                cmd.Parameters.AddWithValue("$mac", ip.MacAddress ?? "");
                cmd.Parameters.AddWithValue("$loc", ip.Location ?? "");
                cmd.Parameters.AddWithValue("$status", ip.Status);
                cmd.Parameters.AddWithValue("$lat", ip.Latency);
                cmd.Parameters.AddWithValue("$check", ip.LastCheckTime.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$serial", ip.Serial ?? "");
                cmd.Parameters.AddWithValue("$user", ip.User ?? "");
                cmd.Parameters.AddWithValue("$created", ip.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();

                // Lấy ID vừa insert
                cmd.CommandText = "SELECT last_insert_rowid()";
                ip.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateIpMonitor(IpMonitor ip)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    UPDATE IpMonitors SET 
                        DeviceGroup = $group,
                        DeviceName = $name,
                        ImagePath = $image,
                        MacAddress = $mac,
                        Location = $loc, 
                        Status = $status, 
                        Latency = $lat, 
                        LastCheckTime = $check,
                        LastOfflineTime = $offline,
                        Serial = $serial,
                        User = $user
                    WHERE IpAddress = $ip";
                cmd.Parameters.AddWithValue("$ip", ip.IpAddress);
                cmd.Parameters.AddWithValue("$group", ip.DeviceGroup ?? "");
                cmd.Parameters.AddWithValue("$name", ip.DeviceName ?? "");
                cmd.Parameters.AddWithValue("$image", ip.ImagePath ?? "");
                cmd.Parameters.AddWithValue("$mac", ip.MacAddress ?? "");
                cmd.Parameters.AddWithValue("$loc", ip.Location ?? "");
                cmd.Parameters.AddWithValue("$status", ip.Status);
                cmd.Parameters.AddWithValue("$lat", ip.Latency);
                cmd.Parameters.AddWithValue("$check", ip.LastCheckTime.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$offline", ip.LastOfflineTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("$serial", ip.Serial ?? "");
                cmd.Parameters.AddWithValue("$user", ip.User ?? "");
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateAllIpMonitorStatuses(IList<IpMonitor> monitors)
        {
            if (monitors == null || monitors.Count == 0) return;
            
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    // Only update fields changed by Ping
                    cmd.CommandText = @"UPDATE IpMonitors SET Status=$s, Latency=$l, LastCheckTime=$c, LastOfflineTime=$o WHERE IpAddress=$ip";
                    
                    var pIp = cmd.Parameters.Add("$ip", SqliteType.Text);
                    var pStatus = cmd.Parameters.Add("$s", SqliteType.Text);
                    var pLat = cmd.Parameters.Add("$l", SqliteType.Integer);
                    var pCheck = cmd.Parameters.Add("$c", SqliteType.Text);
                    var pOffline = cmd.Parameters.Add("$o", SqliteType.Text);

                    foreach (var ip in monitors)
                    {
                        pIp.Value = ip.IpAddress;
                        pStatus.Value = ip.Status;
                        pLat.Value = ip.Latency;
                        pCheck.Value = ip.LastCheckTime.ToString("yyyy-MM-dd HH:mm:ss");
                        pOffline.Value = ip.LastOfflineTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value;
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
        }

        public void DeleteIpMonitor(string ipAddress)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM IpMonitors WHERE IpAddress = $ip";
                cmd.Parameters.AddWithValue("$ip", ipAddress);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteAllIpMonitors()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM IpMonitors";
                cmd.ExecuteNonQuery();
            }
        }

        // ========== LOGS ==========
        public void AddLog(LogEntry log)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Logs (Timestamp, IpAddress, Location, Event, Details, DeviceName, User)
                    VALUES ($ts, $ip, $loc, $event, $details, $name, $user)";
                cmd.Parameters.AddWithValue("$ts", log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$ip", log.IpAddress ?? "");
                cmd.Parameters.AddWithValue("$loc", log.Location ?? "");
                cmd.Parameters.AddWithValue("$event", log.Event ?? "");
                cmd.Parameters.AddWithValue("$details", log.Details ?? "");
                cmd.Parameters.AddWithValue("$name", log.DeviceName ?? "");
                cmd.Parameters.AddWithValue("$user", log.User ?? "");
                cmd.ExecuteNonQuery();
            }

            // Giới hạn log (xóa cũ nếu > 50000)
            CleanupOldLogs();
        }

        private void CleanupOldLogs()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    DELETE FROM Logs WHERE Id NOT IN (
                        SELECT Id FROM Logs ORDER BY Id DESC LIMIT 50000
                    )";
                cmd.ExecuteNonQuery();
            }
        }

        public List<LogEntry> GetLogs(int limit = 1000)
        {
            var list = new List<LogEntry>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = $"SELECT Id, Timestamp, IpAddress, Location, Event, Details, DeviceName, User FROM Logs ORDER BY Id DESC LIMIT {limit}";
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new LogEntry
                        {
                            Id = reader.GetInt32(0),
                            Timestamp = DateTime.Parse(reader.GetString(1)),
                            IpAddress = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Location = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            Event = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Details = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            DeviceName = !reader.IsDBNull(6) ? reader.GetString(6) : "",
                            User = !reader.IsDBNull(7) ? reader.GetString(7) : ""
                        });
                    }
                }
            }
            return list;
        }

        public void ClearLogs()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Logs";
                cmd.ExecuteNonQuery();
            }
        }

        // ========== SETTINGS ==========
        public AppSettings GetSettings()
        {
            var settings = new AppSettings();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Key, Value FROM Settings";
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string key = reader.GetString(0);
                        string value = reader.GetString(1);
                        
                        switch (key)
                        {
                            case "RefreshIntervalSeconds":
                                if (int.TryParse(value, out int interval))
                                    settings.RefreshIntervalSeconds = interval;
                                break;
                            case "PingTimeoutMs":
                                if (int.TryParse(value, out int timeout))
                                    settings.PingTimeoutMs = timeout;
                                break;
                            case "MaxConcurrentPings":
                                if (int.TryParse(value, out int concurrent))
                                    settings.MaxConcurrentPings = concurrent;
                                break;
                            case "AutoRefresh":
                                settings.AutoRefresh = value == "1" || value.ToLower() == "true";
                                break;
                            case "PingRetryCount":
                                if (int.TryParse(value, out int retry))
                                    settings.PingRetryCount = retry;
                                break;
                            case "OfflineThreshold":
                                if (int.TryParse(value, out int threshold))
                                    settings.OfflineThreshold = threshold;
                                break;
                        }
                    }
                }
            }
            return settings;
        }

        public void SaveSettings(AppSettings settings)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                
                SaveSetting(connection, "RefreshIntervalSeconds", settings.RefreshIntervalSeconds.ToString());
                SaveSetting(connection, "PingTimeoutMs", settings.PingTimeoutMs.ToString());
                SaveSetting(connection, "MaxConcurrentPings", settings.MaxConcurrentPings.ToString());
                SaveSetting(connection, "PingRetryCount", settings.PingRetryCount.ToString());
                SaveSetting(connection, "OfflineThreshold", settings.OfflineThreshold.ToString());
                SaveSetting(connection, "AutoRefresh", settings.AutoRefresh ? "1" : "0");
            }
        }

        private void SaveSetting(SqliteConnection connection, string key, string value)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES ($key, $value)";
            cmd.Parameters.AddWithValue("$key", key);
            cmd.Parameters.AddWithValue("$value", value);
            cmd.ExecuteNonQuery();
        }

        public string GetDatabasePath() => _dbPath;
    }
}
