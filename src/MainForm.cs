using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace GujasPCFix
{
    internal sealed class MainForm : Form, IGlassHost
    {
        private static readonly Color Fg = Color.FromArgb(250, 250, 252);
        private static readonly Color Muted = Color.FromArgb(176, 176, 182);
        private Bitmap _frost;

        public Image Frost
        {
            get { return _frost; }
        }

        private readonly IconRail _rail = new IconRail();
        private readonly MenuPane _menu = new MenuPane();
        private readonly GlassPanel _content = new GlassPanel();
        private readonly Panel _pageHome = MakePage();
        private readonly Panel _pageOptimize = MakePage();
        private readonly Panel _pageTuner = MakePage();
        private readonly Panel _pageActivity = MakePage();

        private readonly Label _cpuValue = MakeValue();
        private readonly Label _gpuValue = MakeValue();
        private readonly Label _ramValue = MakeValue();
        private readonly Label _diskValue = MakeValue();
        private readonly Label _junkValue = MakeValue();
        private readonly Label _osValue = MakeValue();
        private readonly Label _powerValue = MakeMuted();
        private readonly CheckBox _cleanTemp = MakeCheck("Clean temporary files", true);
        private readonly CheckBox _recycle = MakeCheck("Empty Recycle Bin", true);
        private readonly CheckBox _dns = MakeCheck("Flush DNS cache", true);
        private readonly CheckBox _power = MakeCheck("High performance power plan", true);
        private readonly CheckBox _game = MakeCheck("Enable Game Mode", true);
        private readonly CheckBox _visuals = MakeCheck("Best-performance visual effects", false);
        private readonly CheckBox _thumbs = MakeCheck("Clear thumbnail cache", true);
        private readonly CheckBox _gpuSched = MakeCheck("GPU hardware scheduling", true);
        private readonly CheckBox _netTune = MakeCheck("Game network profile", true);
        private readonly TextBox _cpuBox = MakeField();
        private readonly TextBox _gpuBox = MakeField();
        private readonly TextBox _tuneTips = MakeMultiline();
        private readonly ComboBox _gameProfile = new ComboBox();
        private readonly List<GameProfile> _gameProfiles = GameProfiles.Create();
        private readonly TextBox _log = MakeMultiline();
        private readonly GlassButton _scanButton = new GlassButton();
        private readonly GlassButton _boostButton = new GlassButton();
        private readonly GlassButton _restoreButton = new GlassButton();
        private readonly GlassButton _recommendedButton = new GlassButton();
        private readonly GlassButton _tuneButton = new GlassButton();
        private readonly TextBox _tweakSearch = MakeField();
        private readonly ComboBox _tweakCategory = new ComboBox();
        private readonly CheckedListBox _tweakList = new CheckedListBox();
        private readonly Label _tweakDescription = MakeMuted();
        private readonly List<TweakDefinition> _tweaks = TweakCatalog.Create();
        private bool _busy;
        private int _ramGb;
        private bool _filledHardware;

        public MainForm()
        {
            Text = "Gujas PC Fix";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1280, 780);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 10.5f);
            ForeColor = Fg;
            DoubleBuffered = true;
            BackColor = Color.Black;
            LoadBackdrop();

            _rail.Location = new Point(18, 24);
            _rail.Size = new Size(68, 732);
            _rail.PageChanged += delegate { SyncNav(_rail.Page); };

            _menu.Location = new Point(98, 24);
            _menu.Size = new Size(300, 732);
            _menu.PageChanged += delegate { SyncNav(_menu.Page); };

            _content.Location = new Point(414, 24);
            _content.Size = new Size(848, 732);

            BuildHome();
            BuildOptimize();
            BuildTuner();
            BuildActivity();

            _content.Controls.Add(_pageActivity);
            _content.Controls.Add(_pageTuner);
            _content.Controls.Add(_pageOptimize);
            _content.Controls.Add(_pageHome);

            Controls.Add(_rail);
            Controls.Add(_menu);
            Controls.Add(_content);

            ShowPage(AppPage.Home);
            Shown += delegate { RunScan(); };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        private void SyncNav(AppPage page)
        {
            _rail.Page = page;
            _menu.Page = page;
            _rail.Invalidate();
            _menu.Invalidate();
            ShowPage(page);
        }

        private void ShowPage(AppPage page)
        {
            _pageHome.Visible = page == AppPage.Home;
            _pageOptimize.Visible = page == AppPage.Optimize;
            _pageTuner.Visible = page == AppPage.Tuner;
            _pageActivity.Visible = page == AppPage.Activity;
        }

        private void LoadBackdrop()
        {
            Theme.LoadWallpaper();
            BackgroundImage = Theme.Wallpaper;
            BackgroundImageLayout = ImageLayout.Stretch;
            if (_frost != null) _frost.Dispose();
            _frost = Theme.MakeFrost(ClientSize.Width, ClientSize.Height);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_frost != null) _frost.Dispose();
            base.OnFormClosed(e);
        }

        private void BuildHome()
        {
            _pageHome.Controls.Add(Heading("Gujas PC Fix", 24, 20, 28));
            _pageHome.Controls.Add(Sub("Overview of this machine. Scan, then Optimize when you are ready.", 26, 62));
            _pageHome.Controls.Add(StatCard("Processor", _cpuValue, 24, 100));
            _pageHome.Controls.Add(StatCard("Graphics", _gpuValue, 292, 100));
            _pageHome.Controls.Add(StatCard("Memory", _ramValue, 560, 100));
            _pageHome.Controls.Add(StatCard("Storage", _diskValue, 24, 250));
            _pageHome.Controls.Add(StatCard("Reclaimable", _junkValue, 292, 250));
            _pageHome.Controls.Add(StatCard("Windows", _osValue, 560, 250));
            _powerValue.Location = new Point(28, 410);
            _pageHome.Controls.Add(_powerValue);
            _scanButton.Text = "Scan system";
            _scanButton.Size = new Size(168, 48);
            _scanButton.Location = new Point(24, 640);
            _scanButton.Click += delegate { RunScan(); };
            _pageHome.Controls.Add(_scanButton);
        }

        private void BuildOptimize()
        {
            _pageOptimize.Controls.Add(Heading("Performance Tweaks", 24, 16, 26));
            _pageOptimize.Controls.Add(Sub("60 transparent tweaks. Select only what you want, every registry change can be restored.", 26, 56));

            _tweakSearch.Location = new Point(24, 88);
            _tweakSearch.Size = new Size(330, 30);
            _tweakSearch.TextChanged += delegate { RefreshTweakList(); };
            _pageOptimize.Controls.Add(_tweakSearch);

            _tweakCategory.Location = new Point(370, 88);
            _tweakCategory.Size = new Size(190, 30);
            _tweakCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            _tweakCategory.BackColor = Color.FromArgb(18, 18, 20);
            _tweakCategory.ForeColor = Fg;
            _tweakCategory.Items.Add("All categories");
            foreach (string category in _tweaks.Select(x => x.Category).Distinct()) _tweakCategory.Items.Add(category);
            _tweakCategory.SelectedIndex = 0;
            _tweakCategory.SelectedIndexChanged += delegate { RefreshTweakList(); };
            _pageOptimize.Controls.Add(_tweakCategory);

            _recommendedButton.Text = "Recommended";
            _recommendedButton.Size = new Size(160, 38);
            _recommendedButton.Location = new Point(574, 84);
            _recommendedButton.Click += delegate { SelectRecommended(); };
            _pageOptimize.Controls.Add(_recommendedButton);

            _tweakList.Location = new Point(24, 132);
            _tweakList.Size = new Size(790, 398);
            _tweakList.BackColor = Color.FromArgb(12, 12, 14);
            _tweakList.ForeColor = Fg;
            _tweakList.BorderStyle = BorderStyle.None;
            _tweakList.CheckOnClick = true;
            _tweakList.Font = new Font("Segoe UI", 10.5f);
            _tweakList.ItemCheck += delegate(object sender, ItemCheckEventArgs e)
            {
                BeginInvoke(new Action(UpdateTweakSummary));
            };
            _tweakList.SelectedIndexChanged += delegate
            {
                TweakDefinition item = _tweakList.SelectedItem as TweakDefinition;
                _tweakDescription.Text = item == null ? "Select a tweak to read what it changes." : item.Description + (item.RestartRequired ? "  Restart required." : "");
            };
            _pageOptimize.Controls.Add(_tweakList);

            _tweakDescription.Location = new Point(28, 542);
            _tweakDescription.MaximumSize = new Size(780, 46);
            _pageOptimize.Controls.Add(_tweakDescription);

            _boostButton.Text = "Apply selected";
            _boostButton.Emphasized = true;
            _boostButton.Size = new Size(190, 48);
            _boostButton.Location = new Point(24, 630);
            _boostButton.Click += delegate { RunBoost(); };
            _pageOptimize.Controls.Add(_boostButton);

            _restoreButton.Text = "Restore defaults";
            _restoreButton.Size = new Size(190, 48);
            _restoreButton.Location = new Point(228, 630);
            _restoreButton.Click += delegate { RestoreTweaks(); };
            _pageOptimize.Controls.Add(_restoreButton);
            RefreshTweakList();
        }

        private void RefreshTweakList()
        {
            if (_tweakCategory.SelectedIndex < 0) return;
            HashSet<string> selected = new HashSet<string>();
            foreach (object value in _tweakList.CheckedItems)
            {
                TweakDefinition old = value as TweakDefinition;
                if (old != null) selected.Add(old.Id);
            }
            string query = _tweakSearch.Text.Trim();
            string category = _tweakCategory.SelectedItem == null ? "All categories" : _tweakCategory.SelectedItem.ToString();
            _tweakList.BeginUpdate();
            _tweakList.Items.Clear();
            foreach (TweakDefinition tweak in _tweaks)
            {
                if (category != "All categories" && tweak.Category != category) continue;
                if (query.Length > 0 && tweak.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 && tweak.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                _tweakList.Items.Add(tweak, selected.Contains(tweak.Id));
            }
            _tweakList.EndUpdate();
            UpdateTweakSummary();
        }

        private void SelectRecommended()
        {
            for (int i = 0; i < _tweakList.Items.Count; i++)
            {
                TweakDefinition tweak = (TweakDefinition)_tweakList.Items[i];
                _tweakList.SetItemChecked(i, tweak.Recommended);
            }
            UpdateTweakSummary();
        }

        private void UpdateTweakSummary()
        {
            int restart = 0;
            foreach (object value in _tweakList.CheckedItems) if (((TweakDefinition)value).RestartRequired) restart++;
            _recommendedButton.Text = _tweakList.CheckedItems.Count + " selected" + (restart > 0 ? "  • " + restart + " restart" : "");
        }

        private void BuildTuner()
        {
            _pageTuner.Controls.Add(Heading("Games Tweaker", 24, 20, 26));
            _pageTuner.Controls.Add(Sub("Choose a game to load safe Windows tweaks and tested in-game recommendations.", 26, 62));
            _pageTuner.Controls.Add(SmallLabel("Game profile", 26, 110));
            _gameProfile.Location = new Point(26, 136);
            _gameProfile.Size = new Size(360, 32);
            _gameProfile.DropDownStyle = ComboBoxStyle.DropDownList;
            _gameProfile.BackColor = Color.FromArgb(18,18,20);
            _gameProfile.ForeColor = Fg;
            foreach (GameProfile profile in _gameProfiles) _gameProfile.Items.Add(profile);
            _gameProfile.SelectedIndexChanged += delegate { PreviewGameProfile(); };
            _pageTuner.Controls.Add(_gameProfile);
            _tuneButton.Text = "Load game tweaks";
            _tuneButton.Size = new Size(200, 44);
            _tuneButton.Location = new Point(410, 130);
            _tuneButton.Click += delegate { ApplyTuner(); };
            _pageTuner.Controls.Add(_tuneButton);
            _tuneTips.Location = new Point(26, 200);
            _tuneTips.Size = new Size(790, 410);
            _tuneTips.Text = "Select Counter-Strike 2, Fortnite, Rainbow Six Siege, Minecraft or Far Cry 6.";
            _pageTuner.Controls.Add(_tuneTips);
            _gameProfile.SelectedIndex = 0;
        }

        private void BuildActivity()
        {
            _pageActivity.Controls.Add(Heading("Activity", 24, 20, 26));
            _pageActivity.Controls.Add(Sub("Technical log of each pass so you can see what actually ran.", 26, 62));
            _log.Location = new Point(24, 100);
            _log.Size = new Size(792, 580);
            _pageActivity.Controls.Add(_log);
        }

        private static Panel MakePage()
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = Color.Transparent;
            return p;
        }

        private static Label Heading(string text, int x, int y, float size)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI Light", size);
            label.ForeColor = Fg;
            label.BackColor = Color.Transparent;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            return label;
        }

        private static Label Sub(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI", 10.5f);
            label.ForeColor = Muted;
            label.BackColor = Color.Transparent;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            return label;
        }

        private Control StatCard(string caption, Label value, int x, int y)
        {
            GlassPanel card = new GlassPanel();
            card.Location = new Point(x, y);
            card.Size = new Size(256, 132);
            Label cap = new Label();
            cap.Text = caption;
            cap.Font = new Font("Segoe UI Semibold", 9f);
            cap.ForeColor = Color.FromArgb(200, 200, 206);
            cap.BackColor = Color.Transparent;
            cap.AutoSize = true;
            cap.Location = new Point(16, 14);
            value.Location = new Point(16, 44);
            value.MaximumSize = new Size(224, 70);
            value.BackColor = Color.Transparent;
            card.Controls.Add(cap);
            card.Controls.Add(value);
            return card;
        }

        private void ApplyTuner()
        {
            GameProfile profile = _gameProfile.SelectedItem as GameProfile;
            if (profile == null) return;
            _tweakSearch.Text = "";
            _tweakCategory.SelectedIndex = 0;
            HashSet<string> ids = new HashSet<string>(profile.TweakIds);
            for (int i = 0; i < _tweakList.Items.Count; i++)
            {
                TweakDefinition tweak = (TweakDefinition)_tweakList.Items[i];
                _tweakList.SetItemChecked(i, ids.Contains(tweak.Id));
            }
            AppendLog(profile.Name + " profile loaded with " + ids.Count + " Windows tweaks.");
            SyncNav(AppPage.Optimize);
        }

        private void PreviewGameProfile()
        {
            GameProfile profile = _gameProfile.SelectedItem as GameProfile;
            if (profile == null) return;
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            string install = profile.FindInstall();
            text.AppendLine(install == null ? "Status: game was not found in a standard folder" : "Detected: " + install);
            text.AppendLine();
            text.AppendLine("Recommended game settings");
            foreach (string setting in profile.Settings) text.AppendLine("• " + setting);
            text.AppendLine();
            text.AppendLine("Launch/profile guidance");
            text.AppendLine(profile.LaunchOptions);
            text.AppendLine();
            text.AppendLine("The Load game tweaks button selects " + profile.TweakIds.Length + " matching Windows tweaks. You review them before applying anything.");
            _tuneTips.Text = text.ToString();
        }

        private void RunScan()
        {
            if (_busy) return;
            SetBusy(true);
            AppendLog("Scanning this PC...");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    ScanResult scan = PcFixEngine.Scan(delegate(string msg)
                    {
                        BeginInvoke(new Action(delegate { AppendLog(msg); }));
                    });
                    BeginInvoke(new Action(delegate
                    {
                        ApplyScan(scan);
                        AppendLog("Scan complete. Reclaimable junk: " + PcFixEngine.FormatBytes(scan.TempBytes));
                        SetBusy(false);
                    }));
                }
                catch (Exception ex)
                {
                    BeginInvoke(new Action(delegate
                    {
                        AppendLog("Scan failed: " + ex.Message);
                        SetBusy(false);
                    }));
                }
            });
        }

        private void RunBoost()
        {
            if (_busy) return;
            List<TweakDefinition> selected = new List<TweakDefinition>();
            foreach (object value in _tweakList.CheckedItems) selected.Add((TweakDefinition)value);
            if (selected.Count == 0) { AppendLog("Select at least one tweak."); return; }

            SetBusy(true);
            AppendLog("==== applying " + selected.Count + " tweaks " + DateTime.Now.ToString("HH:mm:ss") + " ====");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    TweakRunResult result = TweakExecutor.Apply(selected, delegate(string msg)
                    {
                        BeginInvoke(new Action(delegate { AppendLog(msg); }));
                    });
                    BeginInvoke(new Action(delegate
                    {
                        for (int i = 0; i < result.Errors.Count; i++) AppendLog("Could not apply: " + result.Errors[i]);
                        AppendLog("Completed. " + result.Applied + " of " + selected.Count + " tweaks applied.");
                        SetBusy(false);
                        SyncNav(AppPage.Activity);
                    }));
                }
                catch (Exception ex)
                {
                    BeginInvoke(new Action(delegate
                    {
                        AppendLog("Boost failed: " + ex.Message);
                        SetBusy(false);
                    }));
                }
            });
        }

        private void RestoreTweaks()
        {
            if (_busy) return;
            List<TweakDefinition> selected = new List<TweakDefinition>();
            foreach (object value in _tweakList.CheckedItems) selected.Add((TweakDefinition)value);
            if (selected.Count == 0) { AppendLog("Select the tweaks you want to restore."); return; }
            DialogResult answer = MessageBox.Show("Restore the Windows defaults for " + selected.Count + " selected tweaks?", "Gujas PC Fix", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
            SetBusy(true);
            ThreadPool.QueueUserWorkItem(delegate
            {
                TweakRunResult result = TweakExecutor.Restore(selected, delegate(string msg) { BeginInvoke(new Action(delegate { AppendLog(msg); })); });
                BeginInvoke(new Action(delegate
                {
                    for (int i = 0; i < result.Errors.Count; i++) AppendLog("Could not restore: " + result.Errors[i]);
                    AppendLog("Restored " + result.Applied + " registry tweaks.");
                    SetBusy(false);
                    SyncNav(AppPage.Activity);
                }));
            });
        }

        private void ApplyScan(ScanResult scan)
        {
            _cpuValue.Text = Truncate(scan.CpuName, 32) + Environment.NewLine + scan.LogicalCores + " logical cores";
            _gpuValue.Text = Truncate(scan.GpuName, 32);
            _ramValue.Text = scan.MemoryLoad + "% in use" + Environment.NewLine +
                             PcFixEngine.FormatBytes(scan.AvailRamBytes) + " free / " +
                             PcFixEngine.FormatBytes(scan.TotalRamBytes);
            _ramGb = (int)(scan.TotalRamBytes / (1024UL * 1024UL * 1024UL));
            if (scan.Drives.Count > 0)
            {
                DriveSnapshot d = scan.Drives[0];
                _diskValue.Text = d.Name + "  " + PcFixEngine.FormatBytes(d.FreeBytes) + " free" + Environment.NewLine +
                                  PcFixEngine.FormatBytes(d.TotalBytes) + " capacity";
            }
            else
            {
                _diskValue.Text = "No fixed disk";
            }
            _junkValue.Text = PcFixEngine.FormatBytes(scan.TempBytes) + Environment.NewLine + "temp / cache estimate";
            _osValue.Text = Truncate(scan.OsCaption, 32) + Environment.NewLine + "plan  " + scan.PowerPlan;
            _powerValue.Text = "Active power plan: " + scan.PowerPlan;

            if (!_filledHardware)
            {
                _cpuBox.Text = scan.CpuName;
                _gpuBox.Text = scan.GpuName;
                _filledHardware = true;
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            _scanButton.Enabled = !busy;
            _boostButton.Enabled = !busy;
            _restoreButton.Enabled = !busy;
            _recommendedButton.Enabled = !busy;
            _tuneButton.Enabled = !busy;
            UseWaitCursor = busy;
        }

        private void AppendLog(string text)
        {
            _log.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text + Environment.NewLine);
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max) return value;
            return value.Substring(0, max - 1) + "...";
        }

        private static Label SmallLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = Muted;
            label.BackColor = Color.Transparent;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            return label;
        }

        private static Label MakeValue()
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Font = new Font("Segoe UI Semibold", 12f);
            label.ForeColor = Fg;
            label.BackColor = Color.Transparent;
            label.Text = "...";
            return label;
        }

        private static Label MakeMuted()
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Font = new Font("Segoe UI", 9.5f);
            label.ForeColor = Muted;
            label.BackColor = Color.Transparent;
            label.Text = "Active power plan: ...";
            return label;
        }

        private static CheckBox MakeCheck(string text, bool on)
        {
            CheckBox box = new CheckBox();
            box.Text = text;
            box.Checked = on;
            box.AutoSize = true;
            box.ForeColor = Fg;
            box.BackColor = Color.Transparent;
            box.Margin = new Padding(8, 8, 24, 8);
            return box;
        }

        private static TextBox MakeField()
        {
            TextBox box = new TextBox();
            box.BorderStyle = BorderStyle.FixedSingle;
            box.BackColor = Color.FromArgb(18, 18, 20);
            box.ForeColor = Fg;
            box.Font = new Font("Segoe UI", 10.5f);
            return box;
        }

        private static TextBox MakeMultiline()
        {
            TextBox box = new TextBox();
            box.Multiline = true;
            box.ReadOnly = true;
            box.ScrollBars = ScrollBars.Vertical;
            box.BorderStyle = BorderStyle.None;
            box.BackColor = Color.FromArgb(12, 12, 14);
            box.ForeColor = Fg;
            box.Font = new Font("Segoe UI", 9.5f);
            return box;
        }
    }
}
