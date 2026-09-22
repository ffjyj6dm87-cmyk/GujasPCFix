using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GujasPCFix
{
    // The reference design is a real interactive interface, not a screenshot overlay.
    internal sealed class ReferenceMainForm : Form
    {
        internal static readonly Color Background = Color.FromArgb(7, 15, 23);
        internal static readonly Color Surface = Color.FromArgb(12, 23, 35);
        internal static readonly Color Blue = Color.FromArgb(36, 143, 255);
        internal static readonly Color Muted = Color.FromArgb(171, 190, 215);
        readonly Panel sidebar = new Panel(), content = new Panel();
        readonly List<TweakDefinition> catalog = TweakCatalog.Create();
        readonly List<GameProfile> profiles = GameProfiles.Create();
        readonly List<Button> nav = new List<Button>();
        readonly List<string> activity = new List<string>();
        readonly HashSet<string> selected = new HashSet<string>();
        readonly Image covers, tower;
        ScanResult scan;
        bool busy;
        string page = "Dashboard", lastAction = "No changes this session";
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int m, int w, int l);

        public ReferenceMainForm()
        {
            Text = "GujasPCFix 2.1 • Reference Edition";
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(1040, 700);
            MinimumSize = new Size(1040, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Background;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            covers = LoadArt("GujasPCFix.games.png");
            tower = LoadArt("GujasPCFix.dashboard.png");
            sidebar.SetBounds(0, 0, 194, 700);
            sidebar.BackColor = Color.FromArgb(9, 19, 28);
            sidebar.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            Controls.Add(sidebar);
            Label bolt = LabelAt(sidebar, "ϟ", 22, 19, 33, Color.FromArgb(40, 142, 255), true);
            LabelAt(sidebar, "GujasPCFix", 57, 33, 12, Color.White, true);
            LabelAt(sidebar, "Optimize · Clean · Perform", 58, 56, 6.8f, Muted, false);
            string[] names = { "Dashboard", "Optimize", "Clean", "Game Tweaks", "System Tools", "Startup Apps", "Privacy", "Performance", "Settings" };
            string[] glyphs = { "⌂", "◉", "♜", "◈", "⚒", "↗", "◇", "▥", "⚙" };
            for (int i = 0; i < names.Length; i++)
            {
                string target = names[i];
                Button b = ButtonAt(sidebar, glyphs[i] + "   " + target, 13, 97 + i * 43, 168, 39, false, delegate { Navigate(target); });
                b.TextAlign = ContentAlignment.MiddleLeft; b.Padding = new Padding(10, 0, 0, 0); b.Tag = target;
                nav.Add(b);
            }
            Panel foot = Card(sidebar, 15, 555, 164, 124);
            LabelAt(foot, "⊞", 12, 16, 24, Blue, true);
            LabelAt(foot, "Windows PC", 48, 22, 8, Color.White, false);
            LabelAt(foot, "Local system tools", 48, 42, 7, Color.FromArgb(61, 220, 153), false);
            LabelAt(foot, "Version 2.1 · Reference", 12, 75, 7.5f, Muted, false);
            LabelAt(foot, "Made for a faster tomorrow.", 12, 95, 7, Muted, false);
            content.SetBounds(194, 35, 846, 665); content.AutoScroll = true; content.BackColor = Background;
            content.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(content);
            Panel title = new Panel { BackColor = Background, Bounds = new Rectangle(194, 0, 846, 34), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            title.MouseDown += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 2, 0); } };
            Controls.Add(title);
            ButtonAt(title, "−", 727, 0, 38, 30, false, delegate { WindowState = FormWindowState.Minimized; });
            ButtonAt(title, "□", 765, 0, 38, 30, false, delegate { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; });
            ButtonAt(title, "×", 803, 0, 38, 30, false, delegate { Close(); });
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) { e.Cancel = true; MessageBox.Show(this, "Wait for the current operation to finish before closing.", "Operation running"); } };
            Navigate("Dashboard");
            Shown += async delegate { await ScanAsync(); };
        }

        static Image LoadArt(string name)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            { if (stream == null) return null; using (Image image = Image.FromStream(stream)) return new Bitmap(image); }
        }
        protected override void Dispose(bool disposing)
        { if (disposing) { if (covers != null) covers.Dispose(); if (tower != null) tower.Dispose(); } base.Dispose(disposing); }

        void Navigate(string name)
        {
            page = name;
            foreach (Button b in nav) b.BackColor = (string)b.Tag == name ? Color.FromArgb(22, 43, 91) : sidebar.BackColor;
            while (content.Controls.Count > 0) content.Controls[0].Dispose();
            content.AutoScrollPosition = Point.Empty;
            if (name == "Dashboard") Dashboard();
            else if (name == "Game Tweaks") Games();
            else if (name == "Optimize") TweakPage("Optimize", catalog.Where(t => !IsGame(t) && t.Category != "Maintenance").ToList(), null);
            else if (name == "Clean") TweakPage("Clean", catalog.Where(t => t.Category == "Maintenance").ToList(), null);
            else if (name == "Privacy") TweakPage("Privacy", catalog.Where(t => t.Category == "Background").ToList(), null);
            else if (name == "Settings") SettingsPage();
            else if (name == "Startup Apps") StartupPage();
            else SystemPage(name);
        }
        static bool IsGame(TweakDefinition t)
        { return t.Category == "Gaming" || t.Id.StartsWith("games-") || t.Id == "net-throttle" || t.Id == "system-response"; }

        void Header(string title, string subtitle)
        { LabelAt(content, title, 19, 0, 20, Color.White, true); LabelAt(content, subtitle, 20, 36, 9.5f, Muted, false); }

        void Dashboard()
        {
            Header("Dashboard", "Your PC. Faster. Smarter. Cleaner.");
            Panel hero = Card(content, 19, 68, 506, 248);
            if (tower != null)
            {
                PictureBox art = new PictureBox { Bounds = new Rectangle(265, 1, 239, 246), BackColor = Background };
                art.Paint += delegate(object sender, PaintEventArgs e) { e.Graphics.DrawImage(tower, art.ClientRectangle, new Rectangle(490, 104, 227, 243), GraphicsUnit.Pixel); };
                hero.Controls.Add(art);
            }
            LabelAt(hero, "Your PC.\nReady for more.", 22, 24, 19, Color.White, true);
            LabelAt(hero, "Review your settings for a cleaner\nand smoother experience.", 23, 99, 9, Muted, false);
            ButtonAt(hero, "ϟ   Review optimization   →", 23, 161, 232, 43, true, delegate { Navigate("Optimize"); });
            Panel overview = Card(content, 538, 68, 284, 248);
            LabelAt(overview, "System Overview", 20, 18, 10, Color.White, true);
            LabelAt(overview, scan == null ? "Scanning" : "Snapshot", 211, 19, 7.5f, Color.FromArgb(58, 216, 151), false);
            MetricRow(overview, "▣", "CPU", scan == null ? "Reading hardware…" : scan.CpuName, 54);
            MetricRow(overview, "▤", "GPU", scan == null ? "Reading hardware…" : scan.GpuName, 102);
            MetricRow(overview, "▥", "RAM", scan == null ? "Waiting for scan" : PcFixEngine.FormatBytes(scan.TotalRamBytes - scan.AvailRamBytes) + " / " + PcFixEngine.FormatBytes(scan.TotalRamBytes), 150);
            MetricRow(overview, "▱", "Storage", scan == null || scan.Drives.Count == 0 ? "Waiting for scan" : PcFixEngine.FormatBytes(scan.Drives[0].FreeBytes) + " free", 198);
            string[] titles = { "Optimize", "Clean", "Startup Apps", "Privacy" };
            string[] icons = { "ϟ", "♜", "↗", "◇" };
            string[] copy = { "Improve responsiveness.\nReview every change.", "Review temporary files\nand maintenance tasks.", "Manage startup programs\nin Windows settings.", "Review background activity\nand Windows suggestions." };
            string[] actions = { "Optimize Now", "Start Cleaning", "Manage", "Review" };
            for (int i = 0; i < 4; i++)
            {
                string dest = titles[i]; Panel c = Card(content, 19 + i * 205, 330, 191, 153);
                LabelAt(c, icons[i], 15, 11, 23, Blue, true); LabelAt(c, titles[i], 63, 24, 9, Color.White, true);
                LabelAt(c, copy[i], 17, 64, 8.5f, Muted, false);
                ButtonAt(c, actions[i] + "   →", 16, 109, 159, 32, false, delegate { Navigate(dest); });
            }
            Panel storage = Card(content, 19, 496, 261, 140);
            LabelAt(storage, "▱   Storage Usage", 18, 17, 9, Color.White, true);
            string disk = scan == null || scan.Drives.Count == 0 ? "Scan required" : PcFixEngine.FormatBytes(scan.Drives[0].FreeBytes) + " free of " + PcFixEngine.FormatBytes(scan.Drives[0].TotalBytes);
            LabelAt(storage, disk, 18, 52, 8, Muted, false);
            ButtonAt(storage, "Clean Storage   →", 17, 92, 226, 32, false, delegate { Navigate("Clean"); });
            Panel perf = Card(content, 292, 496, 249, 140);
            LabelAt(perf, "▥   Performance", 18, 17, 9, Color.White, true);
            LabelAt(perf, lastAction, 18, 52, 8, Muted, false);
            ButtonAt(perf, "Game Tweaks   →", 17, 92, 215, 32, false, delegate { Navigate("Game Tweaks"); });
            Panel health = Card(content, 553, 496, 269, 140);
            LabelAt(health, "♡   System Status", 18, 17, 9, Color.White, true);
            LabelAt(health, scan == null ? "Hardware scan pending" : "Hardware scan complete", 19, 52, 8.5f, Muted, false);
            LabelAt(health, "Driver and security health not tested.", 19, 76, 8, Muted, false);
            ButtonAt(health, "Refresh scan", 17, 99, 232, 29, false, async delegate { await ScanAsync(); });
        }

        void MetricRow(Control parent, string icon, string title, string value, int y)
        {
            LabelAt(parent, icon, 22, y, 21, Blue, true);
            LabelAt(parent, title, 70, y + 1, 8, Color.White, true);
            Label l = LabelAt(parent, value, 70, y + 19, 7.5f, Muted, false);
            l.AutoSize = false; l.Size = new Size(202, 24); l.AutoEllipsis = true;
        }

        void Games()
        {
            LabelAt(content, "BOOST GAMES. HIGHER FPS. SMOOTHER EXPERIENCE.", 20, 6, 6.5f, Muted, false);
            LabelAt(content, "◈  Game", 20, 29, 24, Color.White, true);
            LabelAt(content, "Tweaks", 201, 29, 24, Blue, true);
            LabelAt(content, "Review game-specific profiles and Windows settings before applying changes.", 20, 79, 9, Muted, false);
            TextBox search = new TextBox { Bounds = new Rectangle(573, 11, 248, 25), BackColor = Surface, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, AccessibleName = "Search games" };
            content.Controls.Add(search);
            Panel hint = Card(content, 573, 48, 248, 58);
            LabelAt(hint, "◉", 13, 10, 24, Blue, true);
            LabelAt(hint, "Your games. Your settings.", 63, 8, 8, Color.White, true);
            LabelAt(hint, "Select a profile to review its tweaks.", 63, 29, 7, Muted, false);
            string[] order = { "Far Cry 6", "Rainbow Six Siege", "Fortnite", "Minecraft Java", "Counter-Strike 2" };
            var gameCards = new List<Control>();
            for (int i = 0; i < order.Length; i++)
            {
                GameProfile profile = profiles.First(p => p.Name == order[i]);
                Panel card = Card(content, 19 + i * 163, 120, 154, 286); card.Tag = profile.Name;
                int sourceX = new[] { 212, 374, 536, 698, 861 }[i];
                PictureBox cover = new PictureBox { Bounds = new Rectangle(1, 1, 152, 178), BackColor = Surface, Cursor = Cursors.Hand, AccessibleName = profile.Name };
                cover.Paint += delegate(object sender, PaintEventArgs e) { if (covers != null) e.Graphics.DrawImage(covers, cover.ClientRectangle, new Rectangle(sourceX, 156, 151, 177), GraphicsUnit.Pixel); };
                cover.Click += delegate { OpenGame(profile); }; card.Controls.Add(cover);
                Label name = LabelAt(card, profile.Name.Replace(" Java", ""), 3, 189, 8.8f, Color.White, true);
                name.AutoSize = false; name.Size = new Size(148, 21); name.TextAlign = ContentAlignment.MiddleCenter;
                Label count = LabelAt(card, profile.TweakIds.Distinct().Count(id => catalog.Any(t => t.Id == id && t.DisabledReason == null || t.Id == id && t.SettingsUri != null)) + " settings to review", 3, 216, 7.5f, Muted, false);
                count.AutoSize = false; count.Size = new Size(148, 18); count.TextAlign = ContentAlignment.MiddleCenter;
                ButtonAt(card, "Review tweaks   →", 10, 242, 134, 33, true, delegate { OpenGame(profile); }); gameCards.Add(card);
            }
            search.TextChanged += delegate { foreach (Control c in gameCards) c.Visible = ((string)c.Tag).IndexOf(search.Text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0; };
            string[] cats = { "Game Services", "GPU Settings", "Game Mode", "Reduce Latency" };
            string[] desc = { "Capture and background\nrecording settings.", "GPU scheduling and\ndisplay preferences.", "Windows Game Mode\nand game recognition.", "Network and multimedia\nscheduling settings." };
            for (int i = 0; i < 4; i++)
            {
                string category = cats[i]; Panel c = Card(content, 19 + i * 205, 425, 193, 86);
                LabelAt(c, new[] { "⚙", "▣", "▤", "◷" }[i], 14, 17, 22, Muted, true);
                LabelAt(c, category, 56, 20, 8, Color.White, true);
                LabelAt(c, desc[i], 56, 43, 7, Muted, false);
                c.Cursor = Cursors.Hand; c.Click += delegate { OpenGameCategory(category); };
                foreach (Control child in c.Controls) child.Click += delegate { OpenGameCategory(category); };
            }
            Panel tip = Card(content, 19, 525, 803, 66);
            LabelAt(tip, "★", 18, 14, 23, Color.FromArgb(255, 210, 68), true);
            LabelAt(tip, "Pro Tip", 58, 13, 8.5f, Color.White, true);
            LabelAt(tip, "Change one setting at a time and compare frame times. Results depend on your hardware.", 58, 34, 7.5f, Muted, false);
        }

        static string GameCategory(TweakDefinition t)
        {
            if (t.Id == "hags" || t.Id == "vrr" || t.Id == "fullscreen" || t.Id == "games-gpu-priority") return "GPU Settings";
            if (t.Id == "game-mode" || t.Id == "game-mode-allow") return "Game Mode";
            if (t.Category == "Network") return "Reduce Latency";
            if (t.Category == "Maintenance") return "Storage & Cache";
            return "Game Services";
        }
        void OpenGame(GameProfile profile)
        {
            ClearContent();
            TweakPage(profile.Name, catalog.Where(t => profile.TweakIds.Contains(t.Id)).ToList(), profile);
        }
        void OpenGameCategory(string category)
        {
            ClearContent(); TweakPage(category, catalog.Where(t => (IsGame(t) || profiles.Any(p => p.TweakIds.Contains(t.Id))) && GameCategory(t) == category).ToList(), null);
        }
        void ClearContent() { while (content.Controls.Count > 0) content.Controls[0].Dispose(); }

        void TweakPage(string title, List<TweakDefinition> tweaks, GameProfile profile)
        {
            selected.Clear();
            Header(title, profile == null ? "Select individual settings. Review the description and confirm before applying." : "Windows changes apply system-wide. In-game recommendations are manual settings.");
            ButtonAt(content, "← Game Tweaks", 675, 4, 147, 30, false, delegate { Navigate("Game Tweaks"); });
            ComboBox category = new ComboBox { Bounds = new Rectangle(20, 76, 233, 28), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Surface, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            category.Items.Add("All categories");
            Func<TweakDefinition, string> group = t => page == "Game Tweaks" ? GameCategory(t) : t.Category;
            foreach (string c in tweaks.Select(group).Distinct()) category.Items.Add(c);
            content.Controls.Add(category);
            Label selection = LabelAt(content, "0 selected", 277, 80, 9, Muted, false);
            FlowLayoutPanel list = new FlowLayoutPanel { Bounds = new Rectangle(20, 122, profile == null ? 802 : 514, 442), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Background };
            content.Controls.Add(list);
            Action render = delegate
            {
                while (list.Controls.Count > 0) list.Controls[0].Dispose();
                foreach (TweakDefinition t in tweaks.Where(t => category.SelectedIndex == 0 || group(t) == (string)category.SelectedItem))
                {
                    TweakDefinition item = t;
                    Panel row = new Panel { Width = list.Width - 25, Height = 109, BackColor = Surface, Margin = new Padding(0, 0, 0, 8) };
                    bool automatic = string.IsNullOrEmpty(item.DisabledReason) && string.IsNullOrEmpty(item.SettingsUri);
                    CheckBox box = new CheckBox { Text = item.Name, Bounds = new Rectangle(12, 8, row.Width - 24, 24), Checked = selected.Contains(item.Id), ForeColor = Color.White, Enabled = automatic };
                    box.CheckedChanged += delegate { if (box.Checked) selected.Add(item.Id); else selected.Remove(item.Id); selection.Text = selected.Count + " selected"; };
                    row.Controls.Add(box);
                    Label detail = LabelAt(row, item.Description, 31, 34, 8, Muted, false); detail.AutoSize = false; detail.Size = new Size(row.Width - 45, 43);
                    string state = !automatic ? (item.SettingsUri == null ? "Unavailable: no verified effect" : "Manual · Windows control") : group(item) + (item.RestartRequired ? " · Restart" : "") + (string.IsNullOrEmpty(item.Command) ? " · Backed up" : " · Cannot undo");
                    LabelAt(row, state, 31, 83, 7, Blue, false);
                    if (item.SettingsUri != null) ButtonAt(row, "Open settings →", row.Width - 140, 79, 128, 25, false, delegate { OpenWindowsSetting(item.SettingsUri); });
                    list.Controls.Add(row);
                }
            };
            category.SelectedIndexChanged += delegate { render(); }; category.SelectedIndex = 0;
            if (profile != null)
            {
                Panel guide = Card(content, 548, 122, 274, 442);
                LabelAt(guide, "In-game settings", 17, 15, 12, Color.White, true);
                LabelAt(guide, "Manual recommendations", 18, 43, 8, Blue, false);
                TextBox text = new TextBox { Bounds = new Rectangle(18, 74, 237, 349), Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Vertical, BackColor = Surface, ForeColor = Muted };
                text.Text = string.Join(Environment.NewLine + Environment.NewLine, profile.Settings.Select(s => "• " + s)) + Environment.NewLine + Environment.NewLine + "Use settings available in your installed game version. Benchmark before and after changes.";
                guide.Controls.Add(text);
            }
            ButtonAt(content, "Apply selected", 20, 582, 180, 39, true, async delegate { await Execute(tweaks.Where(t => selected.Contains(t.Id)).ToList(), false); });
            ButtonAt(content, "Restore saved values", 213, 582, 180, 39, false, async delegate { await Execute(tweaks.Where(t => selected.Contains(t.Id) && string.IsNullOrEmpty(t.Command)).ToList(), true); });
            LabelAt(content, "Automatic registry changes save and verify original values. Manual changes stay in Windows Settings.", 20, 633, 8, Muted, false);
        }

        async Task Execute(List<TweakDefinition> tweaks, bool restore)
        {
            if (busy) return;
            if (tweaks.Count == 0) { MessageBox.Show(this, "Select at least one supported setting.", "Nothing selected"); return; }
            string message = (restore ? "Restore saved original values for " : "Apply ") + tweaks.Count + " selected settings?\n\n" + string.Join("\n", tweaks.Select(t => t.Name));
            if (tweaks.Any(t => !string.IsNullOrEmpty(t.Command))) message += "\n\nMaintenance and network commands cannot be undone. Cleanup may permanently remove temporary files.";
            if (MessageBox.Show(this, message, "Review changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            busy = true; content.Enabled = false; sidebar.Enabled = false;
            try
            {
                TweakRunResult result = await Task.Run(() => restore ? TweakExecutor.Restore(tweaks, null) : TweakExecutor.Apply(tweaks, null));
                lastAction = result.Applied + " changes at " + DateTime.Now.ToString("HH:mm");
                activity.Add(lastAction); activity.AddRange(result.Log); activity.AddRange(result.Errors);
                string summary = result.Applied + " of " + tweaks.Count + " settings completed.";
                if (result.Errors.Count > 0) summary += "\n\n" + string.Join("\n", result.Errors);
                if (tweaks.Any(t => t.RestartRequired)) summary += "\n\nSome settings require restarting Windows.";
                MessageBox.Show(this, summary, "Results", MessageBoxButtons.OK, result.Errors.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Operation failed"); }
            finally { busy = false; content.Enabled = true; sidebar.Enabled = true; }
        }

        async Task ScanAsync()
        {
            if (busy) return; busy = true;
            try { scan = await Task.Run(() => PcFixEngine.Scan(delegate { })); }
            catch (Exception ex) { activity.Add("Scan failed: " + ex.Message); }
            finally { busy = false; if (!IsDisposed && (page == "Dashboard" || page == "System Tools" || page == "Performance")) Navigate(page); }
        }
        void SystemPage(string title)
        {
            Header(title, "Local hardware snapshot and operation history. No fabricated health scores.");
            string detail = scan == null ? "No completed scan yet." : "CPU: " + scan.CpuName + "\r\nGPU: " + scan.GpuName + "\r\nRAM: " + PcFixEngine.FormatBytes(scan.TotalRamBytes) + "\r\nWindows: " + scan.OsCaption + "\r\nPower plan: " + scan.PowerPlan;
            TextBox text = new TextBox { Bounds = new Rectangle(20, 85, 802, 430), Multiline = true, ReadOnly = true, BackColor = Surface, ForeColor = Muted, BorderStyle = BorderStyle.FixedSingle, ScrollBars = ScrollBars.Vertical, Text = detail + "\r\n\r\nActivity\r\n" + string.Join("\r\n", activity) };
            content.Controls.Add(text);
            ButtonAt(content, "Refresh scan", 20, 538, 170, 38, true, async delegate { await ScanAsync(); });
            ButtonAt(content, "Restore settings", 205, 538, 170, 38, false, delegate { ClearContent(); TweakPage("Restore settings", catalog.Where(t => string.IsNullOrEmpty(t.Command)).ToList(), null); });
        }
        void StartupPage()
        {
            Header("Startup Apps", "Manage enabled startup applications using the Windows startup controls.");
            Panel c = Card(content, 20, 86, 802, 170);
            LabelAt(c, "Choose which apps start with Windows", 23, 22, 15, Color.White, true);
            LabelAt(c, "Windows shows the actual app state and startup impact. No applications are disabled automatically.", 24, 65, 9, Muted, false);
            ButtonAt(c, "Open Startup settings   →", 24, 105, 250, 38, true, delegate { try { Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(this, ex.Message); } });
        }
        void OpenWindowsSetting(string target)
        {
            try
            {
                if (target == "explorer-options") Process.Start(new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, "control.exe"), "folders") { UseShellExecute = true });
                else Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show(this, "Windows could not open this settings page: " + ex.Message); }
        }
        void SettingsPage()
        {
            Header("Settings", "GujasPCFix 2.1 · Reference Edition");
            Panel c = Card(content, 20, 85, 802, 220);
            CheckBox splash = new CheckBox { Text = "Show translucent welcome screen", Bounds = new Rectangle(23, 27, 540, 30), Checked = AppPreferences.ShowSplash, ForeColor = Color.White };
            c.Controls.Add(splash);
            LabelAt(c, "All changes require confirmation. Game recommendations do not edit game files.", 24, 83, 9, Muted, false);
            ButtonAt(c, "Save settings", 24, 139, 180, 38, true, delegate { try { AppPreferences.ShowSplash = splash.Checked; AppPreferences.Save(); MessageBox.Show(this, "Settings saved."); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Settings not saved"); } });
        }

        static Label LabelAt(Control parent, string text, int x, int y, float size, Color color, bool bold)
        {
            Label l = new Label { Text = text, AutoSize = true, Location = new Point(x, y), ForeColor = color, BackColor = Color.Transparent, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular) };
            parent.Controls.Add(l); return l;
        }
        static Panel Card(Control parent, int x, int y, int w, int h)
        {
            Panel p = new ReferenceCard { Bounds = new Rectangle(x, y, w, h) }; parent.Controls.Add(p); return p;
        }
        static Button ButtonAt(Control parent, string text, int x, int y, int w, int h, bool primary, EventHandler action)
        {
            Button b = new ReferenceButton { Text = text, Bounds = new Rectangle(x, y, w, h), Primary = primary, BackColor = Surface, ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8.5f), FlatStyle = FlatStyle.Flat };
            b.FlatAppearance.BorderSize = 0; b.Click += action; parent.Controls.Add(b); return b;
        }
    }

    internal sealed class ReferenceCard : Panel
    {
        public ReferenceCard() { DoubleBuffered = true; BackColor = ReferenceMainForm.Surface; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Glass.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 11))
            using (Pen border = new Pen(Color.FromArgb(38, 63, 88))) e.Graphics.DrawPath(border, path);
        }
    }
    internal sealed class ReferenceButton : Button
    {
        public bool Primary;
        bool hover;
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent == null ? BackColor : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color top = Primary ? Color.FromArgb(24, 104, 250) : (hover ? Color.FromArgb(29, 54, 88) : BackColor);
            using (GraphicsPath path = Glass.RoundRect(r, 8))
            using (LinearGradientBrush brush = new LinearGradientBrush(r, top, Primary ? Color.FromArgb(20, 65, 206) : top, 90f))
            using (Pen border = new Pen(Primary ? Color.FromArgb(66, 141, 255) : Color.FromArgb(31, 57, 87)))
            { e.Graphics.FillPath(brush, path); e.Graphics.DrawPath(border, path); }
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            flags |= TextAlign == ContentAlignment.MiddleLeft ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter;
            Rectangle textRect = r; textRect.Inflate(-8, 0);
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect, Enabled ? ForeColor : Color.Gray, flags);
        }
    }
}
