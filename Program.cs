using System;
using System.Windows.Forms;

namespace PingMonitor
{
    // ============================================
    // ENTRY POINT
    // ============================================
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
