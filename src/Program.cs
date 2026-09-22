using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace GujasPCFix
{
    internal static class Program
    {
        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint period);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint period);

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            timeBeginPeriod(1);
            try
            {
                using (WelcomeForm welcome = new WelcomeForm())
                {
                    welcome.ShowDialog();
                }
                Application.Run(new MainForm());
            }
            finally
            {
                timeEndPeriod(1);
            }
        }
    }
}
