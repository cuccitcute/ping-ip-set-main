using System;

namespace PingMonitor.Models
{
    // ============================================
    // Model: Log Entry
    // ============================================
    public class LogEntry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
        public string Location { get; set; }
        public string Event { get; set; }
        public string Details { get; set; }
        public string DeviceName { get; set; }
        public string User { get; set; }
    }
}
