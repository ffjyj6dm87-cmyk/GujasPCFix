using System;
using System.Drawing;
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
        private readonly TextBox _log = MakeMultiline();
        private readonly GlassButton _scanButton = new GlassButton();
        private readonly GlassButton _boostButton = new GlassButton();
        private readonly GlassButton _tuneButton = new GlassButton();
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
            _pageOptimize.Controls.Add(Heading("Optimize", 24, 20, 26));
            _pageOptimize.Controls.Add(Sub("Choose the Windows passes to run. Nested items in the menu map to this list.", 26, 62));
            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Location = new Point(20, 100);
            flow.Size = new Size(790, 430);
            flow.BackColor = Color.Transparent;
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            flow.Controls.Add(_cleanTemp);
            flow.Controls.Add(_recycle);
            flow.Controls.Add(_dns);
            flow.Controls.Add(_thumbs);
            flow.Controls.Add(_power);
            flow.Controls.Add(_game);
            flow.Controls.Add(_gpuSched);
            flow.Controls.Add(_netTune);
            flow.Controls.Add(_visuals);
            _pageOptimize.Controls.Add(flow);
            _boostButton.Text = "Run optimization";
            _boostButton.Emphasized = true;
            _boostButton.Size = new Size(220, 50);
            _boostButton.Location = new Point(24, 640);
            _boostButton.Click += delegate { RunBoost(); };
            _pageOptimize.Controls.Add(_boostButton);
        }

        private void BuildTuner()
        {
            _pageTuner.Controls.Add(Heading("Component tuner", 24, 20, 26));
            _pageTuner.Controls.Add(Sub("CPU and GPU names drive a profile for this hardware. Edit them if detection is wrong.", 26, 62));
            _pageTuner.Controls.Add(SmallLabel("CPU", 26, 110));
            _cpuBox.Location = new Point(26, 132);
            _cpuBox.Size = new Size(500, 30);
            _pageTuner.Controls.Add(_cpuBox);
            _pageTuner.Controls.Add(SmallLabel("GPU", 26, 178));
            _gpuBox.Location = new Point(26, 200);
            _gpuBox.Size = new Size(500, 30);
            _pageTuner.Controls.Add(_gpuBox);
            _tuneButton.Text = "Compile profile";
            _tuneButton.Size = new Size(200, 44);
            _tuneButton.Location = new Point(26, 250);
            _tuneButton.Click += delegate { ApplyTuner(); };
            _pageTuner.Controls.Add(_tuneButton);
            _tuneTips.Location = new Point(26, 316);
            _tuneTips.Size = new Size(790, 300);
            _tuneTips.Text = "Scan fills these fields. Compile a profile, then open Optimize and run it.";
            _pageTuner.Controls.Add(_tuneTips);
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
            TuneProfile profile = HardwareAdvisor.Recommend(_cpuBox.Text, _gpuBox.Text, _ramGb);
            _cleanTemp.Checked = profile.CleanTemp;
            _recycle.Checked = profile.EmptyRecycleBin;
            _dns.Checked = profile.FlushDns;
            _power.Checked = profile.HighPerformancePower;
            _game.Checked = profile.GameMode;
            _visuals.Checked = profile.VisualPerformance;
            _gpuSched.Checked = profile.GpuScheduling;
            _netTune.Checked = profile.NetworkTuning;
            _thumbs.Checked = profile.Thumbnails;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(profile.Summary);
            if (profile.Tips != null)
            {
                for (int i = 0; i < profile.Tips.Length; i++)
                {
                    sb.Append("• ");
                    sb.AppendLine(profile.Tips[i]);
                }
            }
            _tuneTips.Text = sb.ToString();
            AppendLog("Component profile applied. Open Optimize to run it.");
            SyncNav(AppPage.Optimize);
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
            BoostOptions options = new BoostOptions();
            options.CleanTemp = _cleanTemp.Checked;
            options.EmptyRecycleBin = _recycle.Checked;
            options.FlushDns = _dns.Checked;
            options.HighPerformancePower = _power.Checked;
            options.GameMode = _game.Checked;
            options.VisualPerformance = _visuals.Checked;
            options.GpuScheduling = _gpuSched.Checked;
            options.NetworkTuning = _netTune.Checked;
            options.Thumbnails = _thumbs.Checked;
            if (!options.CleanTemp && !options.EmptyRecycleBin && !options.FlushDns &&
                !options.HighPerformancePower && !options.GameMode && !options.VisualPerformance &&
                !options.GpuScheduling && !options.NetworkTuning && !options.Thumbnails)
            {
                AppendLog("Select at least one option.");
                return;
            }

            SetBusy(true);
            AppendLog("==== optimization pass " + DateTime.Now.ToString("HH:mm:ss") + " ====");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    BoostResult result = PcFixEngine.Boost(options, delegate(string msg)
                    {
                        BeginInvoke(new Action(delegate { AppendLog(msg); }));
                    });
                    BeginInvoke(new Action(delegate
                    {
                        for (int i = 0; i < result.Warnings.Count; i++)
                            AppendLog("Warning: " + result.Warnings[i]);
                        AppendLog("Freed " + PcFixEngine.FormatBytes(result.BytesFreed) + " across " + result.FilesRemoved + " files.");
                        SetBusy(false);
                        RunScan();
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
