using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GujasPCFix
{
    internal sealed class MainForm : Form, IGlassHost
    {
        private static readonly Color Bg=Color.FromArgb(8,13,24), Card=Color.FromArgb(18,25,42), CardAlt=Color.FromArgb(22,30,49), Edge=Color.FromArgb(39,50,74), Accent=Color.FromArgb(124,92,255), Fg=Color.FromArgb(244,247,252), Muted=Color.FromArgb(139,151,174), Green=Color.FromArgb(52,211,153);
        public Image Frost { get { return null; } }

        private readonly MenuPane _menu=new MenuPane();
        private readonly Panel _content=new Panel(), _pageHome=MakePage(), _pageOptimize=MakePage(), _pageTuner=MakePage(), _pageSystem=MakePage(), _pageRestore=MakePage(), _pageSettings=MakePage();
        private readonly Label _cpuValue=MakeValue(), _gpuValue=MakeValue(), _ramValue=MakeValue(), _diskValue=MakeValue(), _junkValue=MakeValue(), _osValue=MakeValue();
        private readonly Label _powerValue=MakeMuted(), _scanStatus=MakeMuted(), _restoreStatus=MakeMuted(), _tweakDescription=MakeMuted(), _selectionStatus=MakeMuted();
        private readonly TextBox _systemDetails=MakeMultiline(), _log=MakeMultiline(), _tweakSearch=MakeField();
        private readonly ComboBox _tweakCategory=new ComboBox();
        private readonly CheckedListBox _tweakList=new CheckedListBox();
        private readonly List<GameProfile> _gameProfiles=GameProfiles.Create();
        private readonly List<TweakDefinition> _tweaks=TweakCatalog.Create();
        private readonly GlassButton _scanButton=new GlassButton(), _quickButton=new GlassButton(), _boostButton=new GlassButton(), _restoreButton=new GlassButton(), _recommendedButton=new GlassButton(), _restoreAllButton=new GlassButton(), _saveSettingsButton=new GlassButton();
        private readonly CheckBox _showSplash=MakeCheck("Show startup screen",true), _autoRecommended=MakeCheck("Select recommended tweaks on launch",false), _confirmAdvanced=MakeCheck("Confirm advanced system tweaks",true), _showResults=MakeCheck("Show result summary after changes",true);
        private bool _busy;

        public MainForm()
        {
            Text="Gujas PC Fix 2.0"; StartPosition=FormStartPosition.CenterScreen; ClientSize=new Size(1380,840); MinimumSize=new Size(1280,780); FormBorderStyle=FormBorderStyle.FixedSingle; MaximizeBox=false; Font=new Font("Segoe UI",10f); ForeColor=Fg; BackColor=Bg; DoubleBuffered=true;
            _menu.Location=new Point(16,16); _menu.Size=new Size(238,808); _menu.PageChanged+=delegate { ShowPage(_menu.Page); };
            _content.Location=new Point(270,16); _content.Size=new Size(1094,808); _content.BackColor=Bg;
            BuildHome(); BuildOptimize(); BuildTuner(); BuildSystemInfo(); BuildRestore(); BuildSettings();
            _content.Controls.Add(_pageSettings); _content.Controls.Add(_pageRestore); _content.Controls.Add(_pageSystem); _content.Controls.Add(_pageTuner); _content.Controls.Add(_pageOptimize); _content.Controls.Add(_pageHome);
            Controls.Add(_menu); Controls.Add(_content); LoadPreferences(); ShowPage(AppPage.Home); Shown+=delegate { RunScan(); };
        }

        protected override CreateParams CreateParams { get { CreateParams cp=base.CreateParams; cp.ExStyle|=0x02000000; return cp; } }
        private void ShowPage(AppPage page) { _menu.Page=page; _menu.Invalidate(); _pageHome.Visible=page==AppPage.Home; _pageOptimize.Visible=page==AppPage.Optimize; _pageTuner.Visible=page==AppPage.Tuner; _pageSystem.Visible=page==AppPage.SystemInfo; _pageRestore.Visible=page==AppPage.Restore; _pageSettings.Visible=page==AppPage.Settings; }
        private void Go(AppPage page) { ShowPage(page); }

        private void BuildHome()
        {
            AddPageTitle(_pageHome,"Dashboard","A clear overview of your PC, performance status and quick actions.");
            Panel hero=FlatCard(24,92,1046,142,Accent);
            hero.Controls.Add(Heading("Your PC is ready to optimize",28,22,23,FontStyle.Bold)); hero.Controls.Add(Sub("Scan hardware, review safe recommendations and apply only the changes you choose.",30,61));
            _scanStatus.Text="System scan is starting..."; _scanStatus.Location=new Point(30,94); hero.Controls.Add(_scanStatus);
            _quickButton.Text="Quick optimize"; _quickButton.Emphasized=true; _quickButton.Size=new Size(174,46); _quickButton.Location=new Point(842,48); _quickButton.Click+=delegate { SelectRecommended(); Go(AppPage.Optimize); }; hero.Controls.Add(_quickButton); _pageHome.Controls.Add(hero);
            _pageHome.Controls.Add(StatCard("PROCESSOR",_cpuValue,24,254,Color.FromArgb(96,165,250))); _pageHome.Controls.Add(StatCard("GRAPHICS",_gpuValue,380,254,Color.FromArgb(167,139,250))); _pageHome.Controls.Add(StatCard("MEMORY",_ramValue,736,254,Green));
            _pageHome.Controls.Add(StatCard("STORAGE",_diskValue,24,416,Color.FromArgb(251,191,36))); _pageHome.Controls.Add(StatCard("RECLAIMABLE",_junkValue,380,416,Color.FromArgb(244,114,182))); _pageHome.Controls.Add(StatCard("WINDOWS",_osValue,736,416,Color.FromArgb(34,211,238)));
            Panel footer=FlatCard(24,590,1046,142,Green); footer.Controls.Add(Heading("System health",24,18,14,FontStyle.Bold)); _powerValue.Location=new Point(24,52); footer.Controls.Add(_powerValue); footer.Controls.Add(Sub("Recommended tweaks are conservative and every registry change has a restore value.",24,82));
            _scanButton.Text="Refresh scan"; _scanButton.Size=new Size(154,42); _scanButton.Location=new Point(860,48); _scanButton.Click+=delegate { RunScan(); }; footer.Controls.Add(_scanButton); _pageHome.Controls.Add(footer);
        }

        private void BuildOptimize()
        {
            AddPageTitle(_pageOptimize,"PC Tweaks","60 transparent optimizations, grouped by purpose and reversible where applicable.");
            _pageOptimize.Controls.Add(InfoChip("60 TWEAKS",24,86,Accent)); _pageOptimize.Controls.Add(InfoChip("6 CATEGORIES",160,86,Color.FromArgb(34,211,238))); _pageOptimize.Controls.Add(InfoChip("RESTORE READY",324,86,Green));
            _tweakSearch.Location=new Point(24,132); _tweakSearch.Size=new Size(376,30); _tweakSearch.TextChanged+=delegate { RefreshTweakList(); }; _pageOptimize.Controls.Add(_tweakSearch);
            _tweakCategory.Location=new Point(416,132); _tweakCategory.Size=new Size(210,30); _tweakCategory.DropDownStyle=ComboBoxStyle.DropDownList; _tweakCategory.FlatStyle=FlatStyle.Flat; _tweakCategory.BackColor=CardAlt; _tweakCategory.ForeColor=Fg; _tweakCategory.Items.Add("All categories"); foreach(string category in _tweaks.Select(x=>x.Category).Distinct()) _tweakCategory.Items.Add(category); _tweakCategory.SelectedIndex=0; _tweakCategory.SelectedIndexChanged+=delegate { RefreshTweakList(); }; _pageOptimize.Controls.Add(_tweakCategory);
            _recommendedButton.Text="Select recommended"; _recommendedButton.Size=new Size(190,38); _recommendedButton.Location=new Point(644,127); _recommendedButton.Click+=delegate { SelectRecommended(); }; _pageOptimize.Controls.Add(_recommendedButton);
            _tweakList.Location=new Point(24,180); _tweakList.Size=new Size(810,438); _tweakList.BackColor=Card; _tweakList.ForeColor=Fg; _tweakList.BorderStyle=BorderStyle.FixedSingle; _tweakList.CheckOnClick=true; _tweakList.Font=new Font("Segoe UI",10.25f); _tweakList.ItemHeight=28;
            _tweakList.ItemCheck+=delegate { BeginInvoke(new Action(UpdateTweakSummary)); }; _tweakList.SelectedIndexChanged+=delegate { TweakDefinition item=_tweakList.SelectedItem as TweakDefinition; _tweakDescription.Text=item==null?"Select a tweak to see exactly what it changes.":item.Description+(item.RestartRequired?"  Restart required.":""); }; _pageOptimize.Controls.Add(_tweakList);
            Panel side=FlatCard(850,180,220,438,Accent); side.Controls.Add(Heading("Selection",18,18,14,FontStyle.Bold)); _selectionStatus.Location=new Point(18,54); _selectionStatus.MaximumSize=new Size(182,80); side.Controls.Add(_selectionStatus); side.Controls.Add(Sub("Recommended",18,130)); side.Controls.Add(Metric(_tweaks.Count(x=>x.Recommended).ToString(),18,153)); side.Controls.Add(Sub("Advanced",18,212)); side.Controls.Add(Metric(_tweaks.Count(x=>x.Risk==TweakRisk.Advanced).ToString(),18,235)); side.Controls.Add(Sub("Restart items",18,294)); side.Controls.Add(Metric(_tweaks.Count(x=>x.RestartRequired).ToString(),18,317)); _pageOptimize.Controls.Add(side);
            _tweakDescription.Location=new Point(28,632); _tweakDescription.MaximumSize=new Size(800,44); _pageOptimize.Controls.Add(_tweakDescription);
            _boostButton.Text="Apply selected"; _boostButton.Emphasized=true; _boostButton.Size=new Size(190,48); _boostButton.Location=new Point(24,698); _boostButton.Click+=delegate { RunBoost(); }; _pageOptimize.Controls.Add(_boostButton);
            _restoreButton.Text="Restore selected"; _restoreButton.Size=new Size(190,48); _restoreButton.Location=new Point(228,698); _restoreButton.Click+=delegate { RestoreTweaks(false); }; _pageOptimize.Controls.Add(_restoreButton); RefreshTweakList();
        }

        private void BuildTuner()
        {
            AddPageTitle(_pageTuner,"Games Tweaker","Dedicated profiles with relevant Windows and in-game recommendations for each title."); int[] xs={24,548};
            for(int i=0;i<_gameProfiles.Count;i++) _pageTuner.Controls.Add(GameCard(_gameProfiles[i],xs[i%2],98+(i/2)*204,i));
        }

        private Control GameCard(GameProfile profile,int x,int y,int index)
        {
            Color[] colors={Color.FromArgb(96,165,250),Color.FromArgb(139,92,246),Color.FromArgb(34,211,238),Green,Color.FromArgb(251,146,60)}; Panel card=FlatCard(x,y,500,184,colors[index]);
            Label badge=new Label(); badge.Text=profile.Name.Substring(0,1); badge.TextAlign=ContentAlignment.MiddleCenter; badge.Font=new Font("Segoe UI",18f,FontStyle.Bold); badge.ForeColor=Color.White; badge.BackColor=colors[index]; badge.Location=new Point(20,20); badge.Size=new Size(48,48); card.Controls.Add(badge);
            card.Controls.Add(Heading(profile.Name,82,18,15,FontStyle.Bold)); string found=profile.FindInstall(); Label status=Sub(found==null?"Not detected automatically":"Installed and detected",84,49); status.ForeColor=found==null?Muted:Green; card.Controls.Add(status);
            Label detail=Sub(ProfileSummary(profile.Name),22,82); detail.MaximumSize=new Size(452,42); card.Controls.Add(detail); Label count=SmallLabel(profile.TweakIds.Length+" matching tweaks",22,139); card.Controls.Add(count);
            GlassButton load=new GlassButton(); load.Text="Open profile"; load.Size=new Size(142,38); load.Location=new Point(336,128); load.Click+=delegate { ShowGameProfile(profile); }; card.Controls.Add(load); return card;
        }

        private static string ProfileSummary(string name) { if(name.IndexOf("Counter",StringComparison.OrdinalIgnoreCase)>=0)return "Competitive FPS, lower input latency and clean fullscreen behavior."; if(name=="Fortnite")return "Rendering mode, graphics choices and stable performance profiles."; if(name.IndexOf("Rainbow",StringComparison.OrdinalIgnoreCase)>=0)return "Competitive graphics, frame-rate and latency recommendations."; if(name.IndexOf("Minecraft",StringComparison.OrdinalIgnoreCase)>=0)return "Java memory, render distance and practical mod guidance."; return "Graphics quality, resolution scaling and stable FPS settings."; }
        private void ShowGameProfile(GameProfile profile) { StringBuilder text=new StringBuilder(); string install=profile.FindInstall(); text.AppendLine(install==null?"Game was not found in a standard install folder.":"Detected: "+install); text.AppendLine(); text.AppendLine("RECOMMENDED GAME SETTINGS"); foreach(string setting in profile.Settings) text.AppendLine("• "+setting); text.AppendLine(); text.AppendLine("LAUNCH AND PROFILE GUIDANCE"); text.AppendLine(profile.LaunchOptions); text.AppendLine(); text.AppendLine("Loading this profile selects "+profile.TweakIds.Length+" matching Windows tweaks. Nothing is applied until you review and confirm."); if(MessageBox.Show(text.ToString(),profile.Name+" profile",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)==DialogResult.OK) LoadGameTweaks(profile); }
        private void LoadGameTweaks(GameProfile profile) { _tweakSearch.Text=""; _tweakCategory.SelectedIndex=0; HashSet<string> ids=new HashSet<string>(profile.TweakIds); for(int i=0;i<_tweakList.Items.Count;i++){TweakDefinition tweak=(TweakDefinition)_tweakList.Items[i]; _tweakList.SetItemChecked(i,ids.Contains(tweak.Id));} AppendLog(profile.Name+" profile loaded with "+ids.Count+" matching tweaks."); Go(AppPage.Optimize); }

        private void BuildSystemInfo()
        {
            AddPageTitle(_pageSystem,"System Info","Hardware and Windows details collected locally from this computer."); Panel overview=FlatCard(24,96,1046,118,Color.FromArgb(34,211,238)); overview.Controls.Add(Heading("Live hardware overview",24,18,16,FontStyle.Bold)); overview.Controls.Add(Sub("Refresh after hardware, driver or power-plan changes.",26,52)); GlassButton refresh=new GlassButton(); refresh.Text="Refresh information"; refresh.Size=new Size(178,42); refresh.Location=new Point(840,38); refresh.Click+=delegate { RunScan(); }; overview.Controls.Add(refresh); _pageSystem.Controls.Add(overview); _systemDetails.Location=new Point(24,234); _systemDetails.Size=new Size(1046,522); _systemDetails.Font=new Font("Consolas",10.5f); _pageSystem.Controls.Add(_systemDetails);
        }

        private void BuildRestore()
        {
            AddPageTitle(_pageRestore,"Restore","Undo registry optimizations and return supported Windows settings to their defaults."); Panel safe=FlatCard(24,100,1046,164,Green); safe.Controls.Add(Heading("Built-in restore values",26,22,18,FontStyle.Bold)); Label info=Sub("Registry-based tweaks include a defined default value. Command-based maintenance tasks do not need restoring.",28,61); info.MaximumSize=new Size(700,50); safe.Controls.Add(info); _restoreAllButton.Text="Restore all supported"; _restoreAllButton.Emphasized=true; _restoreAllButton.Size=new Size(206,46); _restoreAllButton.Location=new Point(808,54); _restoreAllButton.Click+=delegate { RestoreTweaks(true); }; safe.Controls.Add(_restoreAllButton); _pageRestore.Controls.Add(safe);
            Panel how=FlatCard(24,286,514,254,Accent); how.Controls.Add(Heading("How restore works",24,20,16,FontStyle.Bold)); AddStep(how,"1","Choose Restore selected from PC Tweaks, or restore all here.",72); AddStep(how,"2","Confirm the exact scope before Windows is changed.",126); AddStep(how,"3","Restart Windows only when a restored item requires it.",180); _pageRestore.Controls.Add(how);
            Panel activity=FlatCard(556,286,514,438,Color.FromArgb(96,165,250)); activity.Controls.Add(Heading("Activity log",22,18,16,FontStyle.Bold)); _log.Location=new Point(22,56); _log.Size=new Size(470,356); activity.Controls.Add(_log); _pageRestore.Controls.Add(activity); _restoreStatus.Text="No restore action has been run in this session."; _restoreStatus.Location=new Point(32,568); _restoreStatus.MaximumSize=new Size(476,90); _pageRestore.Controls.Add(_restoreStatus);
        }
        private void AddStep(Control parent,string number,string text,int y) { Label n=new Label(); n.Text=number; n.TextAlign=ContentAlignment.MiddleCenter; n.Font=new Font("Segoe UI",10f,FontStyle.Bold); n.ForeColor=Color.White; n.BackColor=Accent; n.Location=new Point(24,y); n.Size=new Size(32,32); parent.Controls.Add(n); Label copy=Sub(text,72,y+5); copy.MaximumSize=new Size(400,42); parent.Controls.Add(copy); }

        private void BuildSettings()
        {
            AddPageTitle(_pageSettings,"Settings","Choose how Gujas PC Fix starts and how confirmations are handled."); Panel preferences=FlatCard(24,100,654,364,Accent); preferences.Controls.Add(Heading("Application preferences",26,22,17,FontStyle.Bold)); PlaceCheck(preferences,_showSplash,26,76,"Keep the branded startup screen when the app opens."); PlaceCheck(preferences,_autoRecommended,26,142,"Prepare the safe recommended set after startup."); PlaceCheck(preferences,_confirmAdvanced,26,208,"Ask before applying machine-wide registry settings."); PlaceCheck(preferences,_showResults,26,274,"Display a clear summary when apply or restore finishes."); _pageSettings.Controls.Add(preferences);
            Panel about=FlatCard(700,100,370,364,Color.FromArgb(34,211,238)); about.Controls.Add(Heading("Gujas PC Fix",26,24,19,FontStyle.Bold)); about.Controls.Add(Sub("Version 2.0",28,63)); about.Controls.Add(Sub("Windows performance control center",28,102)); about.Controls.Add(Sub("60 PC tweaks",28,156)); about.Controls.Add(Sub("5 dedicated game profiles",28,190)); about.Controls.Add(Sub("Local system scan and restore",28,224)); Label admin=Sub(IsAdministrator()?"Administrator access: active":"Administrator access: required for advanced tweaks",28,286); admin.ForeColor=IsAdministrator()?Green:Color.FromArgb(251,191,36); admin.MaximumSize=new Size(310,44); about.Controls.Add(admin); _pageSettings.Controls.Add(about);
            _saveSettingsButton.Text="Save settings"; _saveSettingsButton.Emphasized=true; _saveSettingsButton.Size=new Size(180,46); _saveSettingsButton.Location=new Point(24,492); _saveSettingsButton.Click+=delegate { SavePreferences(); }; _pageSettings.Controls.Add(_saveSettingsButton);
        }
        private static bool IsAdministrator() { try { System.Security.Principal.WindowsIdentity identity=System.Security.Principal.WindowsIdentity.GetCurrent(); return new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator); } catch { return false; } }
        private void PlaceCheck(Control parent,CheckBox box,int x,int y,string description) { box.Location=new Point(x,y); parent.Controls.Add(box); parent.Controls.Add(Sub(description,x+24,y+28)); }
        private void LoadPreferences() { AppPreferences.Load(); _showSplash.Checked=AppPreferences.ShowSplash; _autoRecommended.Checked=AppPreferences.AutoRecommended; _confirmAdvanced.Checked=AppPreferences.ConfirmAdvanced; _showResults.Checked=AppPreferences.ShowResults; if(_autoRecommended.Checked) SelectRecommended(); }
        private void SavePreferences() { AppPreferences.ShowSplash=_showSplash.Checked; AppPreferences.AutoRecommended=_autoRecommended.Checked; AppPreferences.ConfirmAdvanced=_confirmAdvanced.Checked; AppPreferences.ShowResults=_showResults.Checked; AppPreferences.Save(); MessageBox.Show("Settings saved.","Gujas PC Fix",MessageBoxButtons.OK,MessageBoxIcon.Information); }

        private void RefreshTweakList()
        {
            if(_tweakCategory.SelectedIndex<0)return; HashSet<string> selected=new HashSet<string>(); foreach(object value in _tweakList.CheckedItems)selected.Add(((TweakDefinition)value).Id); string query=_tweakSearch.Text.Trim(), category=_tweakCategory.SelectedItem==null?"All categories":_tweakCategory.SelectedItem.ToString(); _tweakList.BeginUpdate(); _tweakList.Items.Clear(); foreach(TweakDefinition tweak in _tweaks){if(category!="All categories"&&tweak.Category!=category)continue; if(query.Length>0&&tweak.Name.IndexOf(query,StringComparison.OrdinalIgnoreCase)<0&&tweak.Description.IndexOf(query,StringComparison.OrdinalIgnoreCase)<0)continue; _tweakList.Items.Add(tweak,selected.Contains(tweak.Id));} _tweakList.EndUpdate(); UpdateTweakSummary();
        }
        private void SelectRecommended() { for(int i=0;i<_tweakList.Items.Count;i++)_tweakList.SetItemChecked(i,((TweakDefinition)_tweakList.Items[i]).Recommended); UpdateTweakSummary(); }
        private void UpdateTweakSummary() { int restart=0; foreach(object value in _tweakList.CheckedItems)if(((TweakDefinition)value).RestartRequired)restart++; _selectionStatus.Text=_tweakList.CheckedItems.Count+" selected"+Environment.NewLine+(restart>0?restart+" require restart":"No restart items"); }

        private void RunScan()
        {
            if(_busy)return; SetBusy(true); _scanStatus.Text="Scanning hardware and storage..."; AppendLog("Scanning this PC..."); ThreadPool.QueueUserWorkItem(delegate { try { ScanResult scan=PcFixEngine.Scan(delegate(string msg){BeginInvoke(new Action(delegate{AppendLog(msg);}));}); BeginInvoke(new Action(delegate{ApplyScan(scan); AppendLog("Scan complete. Reclaimable: "+PcFixEngine.FormatBytes(scan.TempBytes)); SetBusy(false);})); } catch(Exception ex){BeginInvoke(new Action(delegate{_scanStatus.Text="Scan could not finish"; AppendLog("Scan failed: "+ex.Message); SetBusy(false);}));} });
        }
        private void RunBoost()
        {
            if(_busy)return; List<TweakDefinition> selected=CheckedTweaks(false); if(selected.Count==0){MessageBox.Show("Select at least one tweak first.","Gujas PC Fix",MessageBoxButtons.OK,MessageBoxIcon.Information);return;} if(AppPreferences.ConfirmAdvanced&&selected.Any(x=>x.Risk==TweakRisk.Advanced)&&MessageBox.Show("This selection includes machine-wide advanced tweaks and may require administrator access. Continue?","Confirm advanced tweaks",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return; SetBusy(true); AppendLog("Applying "+selected.Count+" selected tweaks..."); ThreadPool.QueueUserWorkItem(delegate{TweakRunResult result=TweakExecutor.Apply(selected,delegate(string msg){BeginInvoke(new Action(delegate{AppendLog(msg);}));}); BeginInvoke(new Action(delegate{foreach(string error in result.Errors)AppendLog("Could not apply: "+error); string summary=result.Applied+" of "+selected.Count+" tweaks applied"+(result.Errors.Count>0?". "+result.Errors.Count+" need attention.":" successfully."); AppendLog(summary); SetBusy(false); if(AppPreferences.ShowResults)MessageBox.Show(summary,"Optimization complete",MessageBoxButtons.OK,result.Errors.Count==0?MessageBoxIcon.Information:MessageBoxIcon.Warning);}));});
        }
        private List<TweakDefinition> CheckedTweaks(bool all) { if(all)return _tweaks.Where(x=>string.IsNullOrEmpty(x.Command)).ToList(); List<TweakDefinition> list=new List<TweakDefinition>(); foreach(object value in _tweakList.CheckedItems)list.Add((TweakDefinition)value); return list; }
        private void RestoreTweaks(bool all)
        {
            if(_busy)return; List<TweakDefinition> selected=CheckedTweaks(all).Where(x=>string.IsNullOrEmpty(x.Command)).ToList(); if(selected.Count==0){MessageBox.Show("Select at least one registry tweak to restore.","Gujas PC Fix",MessageBoxButtons.OK,MessageBoxIcon.Information);return;} if(MessageBox.Show("Restore Windows defaults for "+selected.Count+" supported tweaks?","Confirm restore",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return; SetBusy(true); AppendLog("Restoring "+selected.Count+" tweaks..."); ThreadPool.QueueUserWorkItem(delegate{TweakRunResult result=TweakExecutor.Restore(selected,delegate(string msg){BeginInvoke(new Action(delegate{AppendLog(msg);}));}); BeginInvoke(new Action(delegate{foreach(string error in result.Errors)AppendLog("Could not restore: "+error); _restoreStatus.Text="Last restore: "+result.Applied+" values restored at "+DateTime.Now.ToString("HH:mm")+"."; SetBusy(false); Go(AppPage.Restore); if(AppPreferences.ShowResults)MessageBox.Show(_restoreStatus.Text,"Restore complete",MessageBoxButtons.OK,result.Errors.Count==0?MessageBoxIcon.Information:MessageBoxIcon.Warning);}));});
        }

        private void ApplyScan(ScanResult scan)
        {
            _cpuValue.Text=Truncate(scan.CpuName,38)+Environment.NewLine+scan.LogicalCores+" logical cores"; _gpuValue.Text=Truncate(scan.GpuName,38); _ramValue.Text=scan.MemoryLoad+"% in use"+Environment.NewLine+PcFixEngine.FormatBytes(scan.AvailRamBytes)+" available";
            if(scan.Drives.Count>0){DriveSnapshot d=scan.Drives[0]; _diskValue.Text=d.Name+"  "+PcFixEngine.FormatBytes(d.FreeBytes)+" free"+Environment.NewLine+PcFixEngine.FormatBytes(d.TotalBytes)+" total";}else _diskValue.Text="No fixed disk detected";
            _junkValue.Text=PcFixEngine.FormatBytes(scan.TempBytes)+Environment.NewLine+"temporary data"; _osValue.Text=Truncate(scan.OsCaption,38)+Environment.NewLine+scan.PowerPlan; _powerValue.Text="Active power plan: "+scan.PowerPlan; _scanStatus.Text="Scan complete  •  "+scan.LogicalCores+" logical cores  •  "+PcFixEngine.FormatBytes(scan.TotalRamBytes)+" RAM";
            StringBuilder dtext=new StringBuilder(); dtext.AppendLine("SYSTEM"); dtext.AppendLine("  Operating system     "+scan.OsCaption); dtext.AppendLine("  Runtime              "+scan.OsName); dtext.AppendLine("  Power plan           "+scan.PowerPlan); dtext.AppendLine(); dtext.AppendLine("PROCESSOR"); dtext.AppendLine("  Model                "+scan.CpuName); dtext.AppendLine("  Logical cores        "+scan.LogicalCores); dtext.AppendLine(); dtext.AppendLine("GRAPHICS"); dtext.AppendLine("  Adapter              "+scan.GpuName); dtext.AppendLine(); dtext.AppendLine("MEMORY"); dtext.AppendLine("  Installed            "+PcFixEngine.FormatBytes(scan.TotalRamBytes)); dtext.AppendLine("  Available            "+PcFixEngine.FormatBytes(scan.AvailRamBytes)); dtext.AppendLine("  Current load         "+scan.MemoryLoad+"%"); dtext.AppendLine(); dtext.AppendLine("STORAGE"); foreach(DriveSnapshot d in scan.Drives)dtext.AppendLine("  "+d.Name.PadRight(20)+PcFixEngine.FormatBytes(d.FreeBytes)+" free of "+PcFixEngine.FormatBytes(d.TotalBytes)); dtext.AppendLine(); dtext.AppendLine("MAINTENANCE"); dtext.AppendLine("  Reclaimable estimate "+PcFixEngine.FormatBytes(scan.TempBytes)); _systemDetails.Text=dtext.ToString();
        }

        private void SetBusy(bool busy) { _busy=busy; _scanButton.Enabled=!busy; _quickButton.Enabled=!busy; _boostButton.Enabled=!busy; _restoreButton.Enabled=!busy; _recommendedButton.Enabled=!busy; _restoreAllButton.Enabled=!busy; UseWaitCursor=busy; }
        private void AppendLog(string text) { _log.AppendText("["+DateTime.Now.ToString("HH:mm:ss")+"] "+text+Environment.NewLine); }
        private static Panel MakePage(){Panel p=new Panel();p.Dock=DockStyle.Fill;p.BackColor=Bg;return p;}
        private void AddPageTitle(Control page,string title,string subtitle){page.Controls.Add(Heading(title,24,18,28,FontStyle.Bold));page.Controls.Add(Sub(subtitle,26,62));}
        private static Panel FlatCard(int x,int y,int width,int height,Color stripe){Panel p=new Panel();p.Location=new Point(x,y);p.Size=new Size(width,height);p.BackColor=Card;p.Padding=new Padding(5,0,0,0);p.Paint+=delegate(object sender,PaintEventArgs e){using(SolidBrush b=new SolidBrush(stripe))e.Graphics.FillRectangle(b,0,0,5,p.Height);using(Pen pen=new Pen(Edge))e.Graphics.DrawRectangle(pen,5,0,p.Width-6,p.Height-1);};return p;}
        private Control StatCard(string caption,Label value,int x,int y,Color accent){Panel card=FlatCard(x,y,334,142,accent);Label cap=SmallLabel(caption,20,18);cap.Font=new Font("Segoe UI Semibold",8.5f);cap.ForeColor=accent;value.Location=new Point(20,50);value.MaximumSize=new Size(294,76);card.Controls.Add(cap);card.Controls.Add(value);return card;}
        private static Control InfoChip(string text,int x,int y,Color color){Label l=new Label();l.Text=text;l.TextAlign=ContentAlignment.MiddleCenter;l.Font=new Font("Segoe UI Semibold",8.5f);l.ForeColor=color;l.BackColor=CardAlt;l.Location=new Point(x,y);l.Size=new Size(text.Length*9+28,28);return l;}
        private static Label Heading(string text,int x,int y,float size,FontStyle style){Label l=new Label();l.Text=text;l.Font=new Font("Segoe UI",size,style);l.ForeColor=Fg;l.BackColor=Color.Transparent;l.AutoSize=true;l.Location=new Point(x,y);return l;}
        private static Label Sub(string text,int x,int y){Label l=new Label();l.Text=text;l.Font=new Font("Segoe UI",10f);l.ForeColor=Muted;l.BackColor=Color.Transparent;l.AutoSize=true;l.Location=new Point(x,y);return l;}
        private static Label SmallLabel(string text,int x,int y){Label l=Sub(text,x,y);l.Font=new Font("Segoe UI",9f);return l;}
        private static Label Metric(string text,int x,int y){Label l=Heading(text,x,y,25,FontStyle.Bold);l.ForeColor=Fg;return l;}
        private static Label MakeValue(){Label l=new Label();l.AutoSize=true;l.Font=new Font("Segoe UI Semibold",11.5f);l.ForeColor=Fg;l.BackColor=Color.Transparent;l.Text="Scanning...";return l;}
        private static Label MakeMuted(){Label l=new Label();l.AutoSize=true;l.Font=new Font("Segoe UI",9.5f);l.ForeColor=Muted;l.BackColor=Color.Transparent;l.Text="...";return l;}
        private static CheckBox MakeCheck(string text,bool value){CheckBox b=new CheckBox();b.Text=text;b.Checked=value;b.AutoSize=true;b.ForeColor=Fg;b.BackColor=Color.Transparent;b.Font=new Font("Segoe UI Semibold",10.25f);return b;}
        private static TextBox MakeField(){TextBox b=new TextBox();b.BorderStyle=BorderStyle.FixedSingle;b.BackColor=CardAlt;b.ForeColor=Fg;b.Font=new Font("Segoe UI",10.5f);return b;}
        private static TextBox MakeMultiline(){TextBox b=new TextBox();b.Multiline=true;b.ReadOnly=true;b.ScrollBars=ScrollBars.Vertical;b.BorderStyle=BorderStyle.FixedSingle;b.BackColor=Card;b.ForeColor=Fg;b.Font=new Font("Segoe UI",9.5f);return b;}
        private static string Truncate(string value,int max){if(string.IsNullOrEmpty(value)||value.Length<=max)return value;return value.Substring(0,max-1)+"…";}
    }
}
