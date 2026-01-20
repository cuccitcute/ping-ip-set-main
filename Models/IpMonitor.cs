using System;

namespace PingMonitor.Models
{
    // ============================================
    // Model: IP Monitor
    // ============================================
    public class IpMonitor
    {
        public int Id { get; set; }
        public string DeviceGroup { get; set; } = "";       // Nhóm thiết bị (Điện thoại, Máy tính, Router, Camera...)
        public string DeviceName { get; set; } = "";        // Tên thiết bị (Samsung S23, Laptop Dell...)
        public string ImagePath { get; set; } = "";         // Đường dẫn hình ảnh
        public string IpAddress { get; set; } = "";         // Địa chỉ IP
        public string MacAddress { get; set; } = "";        // Địa chỉ MAC
        public string Location { get; set; } = "";          // Vị trí
        public string Status { get; set; } = "Pending";     // Trạng thái
        public long Latency { get; set; } = 0;              // Độ trễ (ms)
        public DateTime LastCheckTime { get; set; } = DateTime.Now;    // Thời gian kiểm tra
        public DateTime? LastOfflineTime { get; set; } = null;         // Thời gian Offline
        public string Serial { get; set; } = "";            // Serial/IMEI
        public string User { get; set; } = "";              // Người dùng/Bộ phận
        public DateTime CreatedAt { get; set; } = DateTime.Now;        // Ngày tạo
        public int ConsecutiveFailures { get; set; } = 0;              // Số lần fail liên tiếp
        private string _previousStatus = "";

        public void UpdateStatus(bool isOnline, long latency)
        {
            string newStatus = isOnline ? "Online" : "Offline";
            if (!string.IsNullOrEmpty(_previousStatus) && _previousStatus != newStatus)
            {
                if (newStatus == "Offline")
                    LastOfflineTime = DateTime.Now;
            }
            _previousStatus = Status;
            Status = newStatus;
            Latency = isOnline ? latency : 0;
            LastCheckTime = DateTime.Now;
        }
    }
}
