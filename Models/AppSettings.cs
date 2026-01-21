namespace PingMonitor.Models
{
    // ============================================
    // Model: Settings
    // ============================================
    public class AppSettings
    {
        public int RefreshIntervalSeconds { get; set; } = 60;
        public int PingTimeoutMs { get; set; } = 1500;
        public int MaxConcurrentPings { get; set; } = 50;
        public int PingRetryCount { get; set; } = 10;
        public int OfflineThreshold { get; set; } = 5;
        public bool AutoRefresh { get; set; } = true;
        
        // New: UI Preferences
        public int Language { get; set; } = 0; // 0: VN, 1: EN, 2: KR
        public bool IsDarkMode { get; set; } = false;
        public string ColumnWidths { get; set; } = "";
    }
}
