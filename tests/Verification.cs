using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GujasPCFix
{
    internal static class Verification
    {
        static int assertions;
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); assertions++; Console.WriteLine("PASS " + message); }
        [STAThread]
        static int Main()
        {
            try
            {
                var catalog = TweakCatalog.Create();
                Check(catalog.Count == 60, "60 catalog entries");
                Check(catalog.Select(t => t.Id).Distinct().Count() == 60, "Unique tweak identifiers");
                foreach (var profile in GameProfiles.Create())
                    foreach (string id in profile.TweakIds) Check(catalog.Any(t => t.Id == id), profile.Name + " links to " + id);
                foreach (var t in catalog)
                {
                    Check(!string.IsNullOrWhiteSpace(t.Description) && !string.IsNullOrWhiteSpace(t.Evidence), t.Id + " has description and evidence or review route");
                    if (t.DisabledReason != null)
                        Check(TweakExecutor.Apply(new[] { t }, null).Applied == 0, t.Id + " cannot run an unverified or manual operation");
                }
                foreach (string id in new[] { "games-gpu-priority", "games-sf-priority" })
                    Check(catalog.Single(t => t.Id == id).SettingsUri == null && catalog.Single(t => t.Id == id).DisabledReason != null, id + " blocked as ineffective");

                // Only isolated test registry values are changed. No real system tweaks run.
                string token = "Test-" + Guid.NewGuid().ToString("N");
                string path = @"Software\GujasPCFix\Verification\" + token;
                TweakDefinition test = new TweakDefinition { Id = token, Name = "Test value", Hive = RegistryHive.CurrentUser, Path = path, ValueName = "Setting", Value = 7, Kind = RegistryValueKind.DWord };
                try
                {
                    Check(TweakExecutor.Restore(new[] { test }, null).Errors.Count == 1, "Restore without backup rejected");
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(path)) key.SetValue("Setting", "original", RegistryValueKind.String);
                    Check(TweakExecutor.Apply(new[] { test }, null).Applied == 1, "Apply verifies written value");
                    Check(TweakExecutor.Apply(new[] { test }, null).Applied == 1, "Repeated apply preserves original backup");
                    Check(TweakExecutor.Restore(new[] { test }, null).Applied == 1, "Restore succeeds");
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path))
                        Check(Equals(key.GetValue("Setting"), "original") && key.GetValueKind("Setting") == RegistryValueKind.String, "Original type and value restored");
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path, true)) key.DeleteValue("Setting");
                    Check(TweakExecutor.Apply(new[] { test }, null).Applied == 1, "Absent value applied");
                    Check(TweakExecutor.Restore(new[] { test }, null).Applied == 1, "Absent value restored");
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path)) Check(key.GetValue("Setting") == null, "Original absence preserved");
                    TweakDefinition failed = new TweakDefinition { Id = token + "-command", Name = "Failure probe", Command = "cmd.exe|/c exit 7" };
                    Check(TweakExecutor.Apply(new[] { failed }, null).Errors.Count == 1, "Nonzero process exit is an error, including cmd.exe");
                }
                finally
                {
                    Registry.CurrentUser.DeleteSubKeyTree(path, false);
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\GujasPCFix\Backups\" + token, false);
                }

                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                using (ReferenceMainForm form = new ReferenceMainForm(false))
                {
                    form.Show(); Application.DoEvents();
                    MethodInfo navigate = typeof(ReferenceMainForm).GetMethod("Navigate", BindingFlags.Instance | BindingFlags.NonPublic);
                    foreach (string page in new[] { "Dashboard", "Game Tweaks", "Optimize", "Clean", "System Tools", "Startup Apps", "Privacy", "Performance", "Settings" })
                    {
                        navigate.Invoke(form, new object[] { page }); form.PerformLayout(); Application.DoEvents();
                        using (Bitmap image = new Bitmap(form.Width, form.Height))
                        { form.DrawToBitmap(image, form.ClientRectangle);
                          Check(image.GetPixel(70, 45).ToArgb() != image.GetPixel(1030, 690).ToArgb(), page + " contains rendered controls");
                          image.Save(Path.Combine("qa", page.Replace(" ", "-") + ".png")); }
                        Check(true, page + " builds and renders");
                    }
                    navigate.Invoke(form, new object[] { "Game Tweaks" });
                    MethodInfo game = typeof(ReferenceMainForm).GetMethod("OpenGame", BindingFlags.Instance | BindingFlags.NonPublic);
                    foreach (GameProfile profile in GameProfiles.Create())
                    { game.Invoke(form, new object[] { profile }); Check(true, profile.Name + " detail page builds"); }
                }
                File.WriteAllLines("qa/tweak-audit.tsv", new[] { "ID\tMODE\tDESCRIPTION\tEVIDENCE" }.Concat(catalog.Select(t => t.Id + "\t" + (t.SettingsUri != null ? "Manual Windows control" : t.DisabledReason != null ? "Blocked" : "Automatic, hardware-dependent") + "\t" + t.Description + "\t" + t.Evidence)));
                Console.WriteLine(assertions + " checks passed. No actual performance tweaks were applied to the runner.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
