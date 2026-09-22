using System;
using System.Windows.Forms;

namespace GujasPCFix
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppPreferences.Load();
            if (AppPreferences.ShowSplash)
            {
                using (WelcomeForm welcome = new WelcomeForm())
                {
                    welcome.ShowDialog();
                }
            }
            Application.Run(new ReferenceMainForm());
        }
    }
}
