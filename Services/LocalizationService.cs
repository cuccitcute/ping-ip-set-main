using System.Collections.Generic;

namespace PingMonitor.Services
{
    public class LocalizationService
    {
        // 0: VN, 1: EN, 2: KR
        private int _currentLang = 0;

        private readonly Dictionary<string, string[]> _dict = new Dictionary<string, string[]>
        {
            // General
            { "AppTitle", new[] { "Ping Monitor V2.0 [Mr Hiệu - Srtech Sub]", "Ping Monitor V2.0 [Mr Hiệu - Srtech Sub]", "핑 모니터 V2.0 [Mr Hiệu - Srtech Sub]" } },
            { "Title", new[] { "Ping Monitor", "Ping Monitor", "핑 모니터" } },
            { "SubTitle", new[] { "Phần mềm theo dõi IP thiết bị", "IP Device Monitoring Software", "IP 장치 모니터링 소프트웨어" } },
            
            // Sidebar
            { "FilterTitle", new[] { "LỌC NGƯỜI DÙNG", "USER FILTER", "사용자 필터" } },
            { "ClearFilter", new[] { "Hủy lọc", "Clear Filter", "필터 지우기" } },
            { "All", new[] { "Tất cả", "All", "모두" } },
            { "Empty", new[] { "Trống", "Empty", "비어 있음" } },
            
            // Stats
            { "Total", new[] { "TỔNG", "TOTAL", "전체" } },
            { "Online", new[] { "ONLINE", "ONLINE", "온라인" } },
            { "Offline", new[] { "OFFLINE", "OFFLINE", "오프라인" } },
            
            // Interaction
            { "SearchPlaceholder", new[] { "Tìm kiếm IP, Tên thiết bị, MAC, IMEI,...", "Search IP, Device Name, MAC, IMEI...", "IP 검색, 장치 이름, MAC, IMEI..." } },
            { "Add", new[] { "Thêm thiết bị", "Add Device", "장치 추가" } },
            { "Remove", new[] { "Xóa", "Delete", "삭제" } },
            { "ClearAll", new[] { "Xóa tất cả", "Clear All", "모두 삭제" } },
            { "PingNow", new[] { "Ping!", "Ping!", "핑!" } },
            { "ViewLogs", new[] { "Xem Logs", "View Logs", "로그 보기" } },
            { "ExportLogs", new[] { "Xuất Logs", "Export Logs", "로그 내보내기" } },
            { "Import", new[] { "Nhập Excel", "Import Excel", "엑셀 가져오기" } },
            { "Export", new[] { "Xuất Excel", "Export Excel", "엑셀 내보내기" } },
            { "Settings", new[] { "Cài đặt", "Settings", "설정" } },
            { "Auto", new[] { "Tự động", "Auto", "자동" } },

            // Footer
            // { "Author", new[] { 
            //     "Phiên bản V2.1\nViết bởi Phạm Huy Hiệu\nBộ phận Thiết bị SUB - SR TECH", 
            //     "Version V2.1\nWritten by Phạm Huy Hiệu\nEQM SUB-SR", 
            //     "버전 V2.1\n작성자 Phạm Huy Hiệu\nEQM SUB-SR" } },
            
            // Messages
            { "MsgCopy", new[] { "Đã sao chép:", "Copied:", "복사됨:" } },
            { "MsgInfo", new[] { "Thông báo", "Info", "알림" } },
            
            // Settings Form
            { "Settings_General", new[] { "Chung", "General", "일반" } },
            { "Settings_PingConfig", new[] { "Cấu hình Ping", "Ping Config", "Ping 구성" } },
            { "Lbl_Language", new[] { "Ngôn ngữ:", "Language:", "언어:" } },
            { "Lbl_DarkMode", new[] { "Chế độ tối:", "Dark Mode:", "다크 모드:" } },
            { "Lbl_AutoRefresh", new[] { "Tự động chạy:", "Auto Start:", "자동 시작:" } },
            { "Lbl_Interval", new[] { "Chu kỳ Ping (giây):", "Interval (sec):", "간격 (초):" } },
            { "Lbl_Timeout", new[] { "Timeout (ms):", "Timeout (ms):", "시간 초과 (ms):" } },
            { "Lbl_MaxConcurrent", new[] { "Luồng tối đa:", "Max Threads:", "최대 스레드:" } },
            { "Lbl_Retry", new[] { "Số lần thử:", "Retry Count:", "재시도 횟수:" } },
            { "Lbl_OfflineThreshold", new[] { "Ngưỡng Offline:", "Offline Threshold:", "오프라인 임계값:" } },
            { "Btn_Save", new[] { "Lưu cài đặt", "Save Settings", "설정 저장" } },
            { "Btn_Cancel", new[] { "Hủy bỏ", "Cancel", "취소" } },
            { "Btn_Update", new[] { "Cập nhật", "Update", "업데이트" } },
            
            // Time Labels
            { "Time_Day", new[] { "ngày", "d", "일" } },
            { "Time_Hour", new[] { "giờ", "h", "시" } },
            { "Time_Min", new[] { "phút", "m", "분" } },
            { "Time_Sec", new[] { "giây", "s", "초" } },

            // Add/Edit Form
            { "Lbl_Group", new[] { "Nhóm thiết bị:", "Device Group:", "장치 그룹:" } },
            { "Lbl_Name", new[] { "Tên thiết bị:", "Device Name:", "장치 이름:" } },
            { "Lbl_Image", new[] { "Hình ảnh:", "Image Path:", "이미지 경로:" } },
            { "Lbl_IP", new[] { "Địa chỉ IP (*):", "IP Address (*):", "IP 주소 (*):" } },
            { "Lbl_MAC", new[] { "Địa chỉ MAC:", "MAC Address:", "MAC 주소:" } },
            { "Lbl_Serial", new[] { "Serial/IMEI:", "Serial/IMEI:", "시리얼/IMEI:" } },
            { "Lbl_User", new[] { "Người sử dụng:", "User:", "사용자:" } },
            { "Lbl_Location", new[] { "Vị trí/Khu vực:", "Location:", "위치:" } },
            { "Btn_Browse", new[] { "Chọn ảnh...", "Browse...", "찾아보기..." } },
            { "Title_Add", new[] { "Thêm thiết bị mới", "Add New Device", "새 장치 추가" } },
            { "Title_Edit", new[] { "Chỉnh sửa thiết bị", "Edit Device", "장치 편집" } },
            { "Msg_IpExists", new[] { "IP này đã tồn tại trong danh sách!", "This IP already exists!", "이 IP는 이미 존재합니다!" } },
            { "Msg_IpInvalid", new[] { "Địa chỉ IP không hợp lệ!", "Invalid IP Address!", "유효하지 않은 IP 주소입니다!" } }
        };

        private readonly string[][] _gridHeaders = {
            new[] { "#", "Nhóm", "Tên thiết bị", "Hình ảnh", "IP", "MAC", "Serial/IMEI", "Người dùng", "Vị trí", "Trạng thái", "Ping", "Time check", "Time Offline", "Đếm", "Ngày tạo" },
            new[] { "#", "Group", "Device Name", "Image", "IP", "MAC", "Serial/IMEI", "User", "Location", "Status", "Ping", "Last Check", "Last Offline", "Count", "Created" },
            new[] { "#", "그룹", "장치 이름", "이미지", "IP", "MAC", "시리얼/IMEI", "사용자", "위치", "상태", "핑", "최근 확인", "오프라인 시간", "카운트", "생성일" }
        };

        public void SetLanguage(int langIndex)
        {
            if (langIndex >= 0 && langIndex <= 2) _currentLang = langIndex;
        }

        public string Get(string key)
        {
            if (_dict.ContainsKey(key))
            {
                var vals = _dict[key];
                if (_currentLang < vals.Length) return vals[_currentLang];
            }
            return key;
        }

        public string[] GetGridHeaders() => _gridHeaders[_currentLang];
    }
}
