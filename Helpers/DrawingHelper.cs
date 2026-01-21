using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PingMonitor.Helpers
{
    public static class DrawingHelper
    {
        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float r = radius;
            float d = r * 2;
            
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            
            return path;
        }
    }
}
