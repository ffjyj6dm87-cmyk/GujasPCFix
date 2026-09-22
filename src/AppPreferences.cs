using Microsoft.Win32;

namespace GujasPCFix
{
    internal static class AppPreferences
    {
        private const string KeyPath = @"Software\GujasPCFix";
        public static bool ShowSplash = true;
        public static bool AutoRecommended;
        public static bool ConfirmAdvanced = true;
        public static bool ShowResults = true;

        public static void Load()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath))
                {
                    if (key == null) return;
                    ShowSplash = Read(key, "ShowSplash", true);
                    AutoRecommended = Read(key, "AutoRecommended", false);
                    ConfirmAdvanced = Read(key, "ConfirmAdvanced", true);
                    ShowResults = Read(key, "ShowResults", true);
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath))
                {
                    if (key == null) return;
                    key.SetValue("ShowSplash", ShowSplash ? 1 : 0, RegistryValueKind.DWord);
                    key.SetValue("AutoRecommended", AutoRecommended ? 1 : 0, RegistryValueKind.DWord);
                    key.SetValue("ConfirmAdvanced", ConfirmAdvanced ? 1 : 0, RegistryValueKind.DWord);
                    key.SetValue("ShowResults", ShowResults ? 1 : 0, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        private static bool Read(RegistryKey key, string name, bool fallback)
        {
            object value = key.GetValue(name);
            return value == null ? fallback : System.Convert.ToInt32(value) != 0;
        }
    }
}
