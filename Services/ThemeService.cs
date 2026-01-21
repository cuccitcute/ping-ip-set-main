using System.Drawing;

namespace PingMonitor.Services
{
    public static class ThemeService
    {
        public static bool IsDarkMode { get; private set; } = false;

        public static void SetMode(bool isDark)
        {
            IsDarkMode = isDark;
        }

        // Palette
        // Dark Navy (Slate 900) as requested for Main Brand Color
        public static Color BrandColor => Color.FromArgb(15, 23, 42); 
        public static Color BrandColorLight => Color.FromArgb(51, 65, 85); // Slate 700 for accents
        private static Color BrandSurface => Color.FromArgb(30, 41, 59); // Slate 800 for containers/table

        // Mapping "Light Mode" to Dark Navy as the default look
        public static Color Background => BrandSurface; // #1e293b (Content is Lighter than Sidebar)
        public static Color Surface => BrandSurface;  // #1e293b (Table Rows match Content BG)
        public static Color SurfaceAlt => BrandColor; // #0f172a
        
        public static Color TextMain => Color.FromArgb(241, 245, 249); // Slate 100 (White-ish)
        public static Color TextMuted => Color.FromArgb(148, 163, 184); // Slate 400
        
        public static Color Border => Color.FromArgb(51, 65, 85); // Slate 700
        public static Color Selection => Color.FromArgb(59, 130, 246); // Blue 500
        
        // Status
        public static Color OnlineBg => Color.FromArgb(6, 78, 59);      // Green 900
        public static Color OnlineText => Color.FromArgb(110, 231, 183); // Green 300
        
        public static Color OfflineBg => Color.FromArgb(127, 29, 29);   // Red 900
        public static Color OfflineText => Color.FromArgb(252, 165, 165); // Red 300

        // Buttons (Static)
        public static Color BtnPrimary => Color.FromArgb(37, 99, 235);        // Royal Blue
        public static Color BtnSuccess => Color.FromArgb(16, 185, 129);       // Green
        public static Color BtnDanger => Color.FromArgb(239, 68, 68);         // Red
        public static Color BtnWarning => Color.FromArgb(245, 158, 11);       // Amber
        public static Color BtnSecondary => Color.FromArgb(107, 114, 128);    // Gray
    }
}
