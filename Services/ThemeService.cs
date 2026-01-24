using System;
using System.Drawing;

namespace PingMonitor.Services
{
    public static class ThemeService
    {
        public static bool IsDarkMode { get; private set; } = false;

        public static event Action OnThemeChanged;

        public static void SetMode(bool isDark)
        {
            IsDarkMode = isDark;
            OnThemeChanged?.Invoke();
        }

        // Core Palette
        private static Color Slate900 => Color.FromArgb(15, 23, 42); 
        private static Color Slate800 => Color.FromArgb(30, 41, 59); 
        private static Color Slate700 => Color.FromArgb(51, 65, 85); 
        private static Color Slate100 => Color.FromArgb(241, 245, 249);
        private static Color Slate200 => Color.FromArgb(226, 232, 240);
        private static Color PureWhite => Color.White;
        private static Color Slate50 => Color.FromArgb(248, 250, 252);

        // Dynamic Semantic Colors
        public static Color BrandColor => Slate900; 
        
        // Backgrounds
        public static Color Background => IsDarkMode ? Slate800 : PureWhite;
        public static Color Surface => IsDarkMode ? Slate800 : PureWhite;
        
        public static Color SidebarBg => IsDarkMode ? Slate900 : Slate100;
        public static Color SidebarText => IsDarkMode ? Slate100 : Slate900;
        
        public static Color HeaderBg => IsDarkMode ? Slate800 : PureWhite;
        public static Color HeaderText => IsDarkMode ? Slate100 : Slate900;
        
        // Legacy/Alias needed for build
        public static Color SurfaceAlt => IsDarkMode ? BrandColor : Slate50; 
        public static Color BrandColorLight => IsDarkMode ? Slate700 : Slate200;

        // Text
        public static Color TextMain => IsDarkMode ? Slate100 : Slate900;
        public static Color TextMuted => IsDarkMode ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);
        
        // UI Elements
        public static Color Border => IsDarkMode ? Slate700 : Slate200;
        public static Color Selection => Color.FromArgb(59, 130, 246);
        
        // Status
        public static Color OnlineBg => IsDarkMode ? Color.FromArgb(6, 78, 59) : Color.FromArgb(209, 250, 229);
        public static Color OnlineText => IsDarkMode ? Color.FromArgb(110, 231, 183) : Color.FromArgb(6, 95, 70);
        
        public static Color OfflineBg => IsDarkMode ? Color.FromArgb(127, 29, 29) : Color.FromArgb(254, 226, 226);
        public static Color OfflineText => IsDarkMode ? Color.FromArgb(252, 165, 165) : Color.FromArgb(153, 27, 27);

        // Buttons (Static)
        public static Color BtnPrimary => Color.FromArgb(37, 99, 235);        // Royal Blue
        public static Color BtnSuccess => Color.FromArgb(16, 185, 129);       // Green
        public static Color BtnDanger => Color.FromArgb(239, 68, 68);         // Red
        public static Color BtnWarning => Color.FromArgb(245, 158, 11);       // Amber
        public static Color BtnSecondary => Color.FromArgb(107, 114, 128);    // Gray
        public static Color BtnPing => Color.FromArgb(6, 182, 212);           // Cyan
    }
}
