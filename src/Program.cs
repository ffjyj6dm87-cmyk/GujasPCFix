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
            using (WelcomeForm welcome = new WelcomeForm())
            {
                welcome.ShowDialog();
            }
            Application.Run(new MainForm());
        }
    }
}
