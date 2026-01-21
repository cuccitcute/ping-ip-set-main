namespace PingMonitor.Services
{
    public static class IconHelper
    {
        // Font Name - Fallback to MDL2 for Win10/11 compatibility
        public const string FontName = "Segoe MDL2 Assets";

        // Icons (Map to Unicode Private Use Area)
        public const string CheckMark = "\uE909";       // 🌍 World/Globe (Online)
        public const string StatusErrorFull = "\uE709"; // ✈ Airplane (Offline)
        public const string Add = "\uE710";
        public const string Delete = "\uE74D";
        public const string Edit = "\uE70F";
        public const string Settings = "\uE713";
        public const string Refresh = "\uE72C";
        public const string Search = "\uE721";
        public const string GlobalNavButton = "\uE700"; // Hamburger
        public const string Save = "\uE74E";
        public const string Cancel = "\uE711";
        
        // Additional icons that might be useful
        public const string More = "\uE712";
        public const string Filter = "\uE71C";
        public const string Play = "\uE768";
        public const string Stop = "\uE71A";
        public const string Import = "\uE8B6";
        public const string Export = "\uEDE1";
        public const string Info = "\uE946";
        public const string Log = "\uE81C";
    }
}
