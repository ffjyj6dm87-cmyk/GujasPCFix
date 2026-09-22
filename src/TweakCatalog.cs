using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace GujasPCFix
{
    internal enum TweakRisk { Safe, Advanced }

    internal sealed class TweakDefinition
    {
        public string Id, Category, Name, Description, Path, ValueName, Command, UndoCommand;
        public RegistryHive Hive;
        public object Value, UndoValue;
        public RegistryValueKind Kind = RegistryValueKind.DWord;
        public bool Recommended, RestartRequired;
        public TweakRisk Risk;

        public override string ToString() { return Name; }
    }

    internal sealed class TweakRunResult
    {
        public int Applied;
        public readonly List<string> Log = new List<string>();
        public readonly List<string> Errors = new List<string>();
    }

    internal static class TweakCatalog
    {
        private const string Explorer = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        private const string GameBar = @"Software\Microsoft\GameBar";
        private const string GameStore = @"System\GameConfigStore";
        private const string Desktop = @"Control Panel\Desktop";
        private const string Privacy = @"Software\Microsoft\Windows\CurrentVersion\Privacy";

        public static List<TweakDefinition> Create()
        {
            List<TweakDefinition> t = new List<TweakDefinition>();

            // Gaming, 10
            R(t,"game-mode","Gaming","Enable Windows Game Mode","Prioritizes game workloads while a game is running.",RegistryHive.CurrentUser,GameBar,"AutoGameModeEnabled",1,0,true,false);
            R(t,"game-mode-allow","Gaming","Allow automatic Game Mode","Lets Windows recognize supported games automatically.",RegistryHive.CurrentUser,GameBar,"AllowAutoGameMode",1,0,true,false);
            R(t,"disable-dvr","Gaming","Disable background Game DVR","Stops background game recording when you do not use it.",RegistryHive.CurrentUser,GameStore,"GameDVR_Enabled",0,1,true,false);
            R(t,"disable-capture","Gaming","Disable background capture","Reduces capture overhead. Xbox recording will be unavailable.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\GameDVR","AppCaptureEnabled",0,1,true,false);
            R(t,"hags","Gaming","Enable GPU hardware scheduling","Allows supported GPUs to schedule work in hardware. Restart required.",RegistryHive.LocalMachine,@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers","HwSchMode",2,1,true,true);
            R(t,"vrr","Gaming","Enable variable refresh optimizations","Enables Windows variable refresh support for compatible displays.",RegistryHive.CurrentUser,GameBar,"VariableRefreshRate",1,0,true,false);
            R(t,"fullscreen","Gaming","Prefer modern fullscreen optimizations","Keeps Windows fullscreen optimization enabled for lower-latency presentation.",RegistryHive.CurrentUser,GameStore,"GameDVR_FSEBehaviorMode",0,2,true,false);
            R(t,"capture-audio","Gaming","Disable capture audio","Stops Windows capture audio processing when recording is unused.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\GameDVR","AudioCaptureEnabled",0,1,false,false);
            R(t,"capture-cursor","Gaming","Disable captured mouse pointer","Avoids capturing the pointer in Game DVR clips.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\GameDVR","CursorCaptureEnabled",0,1,false,false);
            R(t,"game-task-priority","Gaming","Prioritize Games task profile","Uses the Windows multimedia Games task priority.",RegistryHive.LocalMachine,@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games","Priority",6,2,true,true);

            // Windows responsiveness, 10
            R(t,"menu-delay","Windows","Reduce menu show delay","Makes menus open sooner.",RegistryHive.CurrentUser,Desktop,"MenuShowDelay","100","400",true,false,RegistryValueKind.String);
            R(t,"foreground-lock","Windows","Allow faster foreground switching","Reduces the delay before an app can take focus.",RegistryHive.CurrentUser,Desktop,"ForegroundLockTimeout",0,200000,false,false);
            R(t,"startup-delay","Windows","Remove startup app delay","Starts enabled startup apps without the artificial Explorer delay.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize","StartupDelayInMSec",0,null,true,false);
            R(t,"animations","Windows","Reduce window animation time","Shortens minimize and restore animation delay.",RegistryHive.CurrentUser,Desktop,"MinAnimate","0","1",false,false,RegistryValueKind.String);
            R(t,"taskbar-animations","Windows","Disable taskbar animations","Reduces small taskbar animation work.",RegistryHive.CurrentUser,Explorer,"TaskbarAnimations",0,1,false,false);
            R(t,"listview-alpha","Windows","Disable list-view fade","Makes Explorer lists draw without fade animations.",RegistryHive.CurrentUser,Explorer,"ListviewAlphaSelect",0,1,false,false);
            R(t,"tooltip-animation","Windows","Disable tooltip animation","Displays tooltips without animation.",RegistryHive.CurrentUser,Explorer,"EnableToolTipAnimation",0,1,false,false);
            R(t,"tooltip-fade","Windows","Disable tooltip fade","Displays tooltips without fade effects.",RegistryHive.CurrentUser,Explorer,"EnableToolTipFade",0,1,false,false);
            R(t,"drag-full-window","Windows","Show window outline while dragging","Reduces redraw work while moving windows.",RegistryHive.CurrentUser,Desktop,"DragFullWindows","0","1",false,false,RegistryValueKind.String);
            R(t,"transparency","Windows","Disable transparency effects","Reduces desktop composition effects on lower-end PCs.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","EnableTransparency",0,1,false,false);

            // Background activity, 10
            R(t,"background-apps","Background","Limit Store apps in background","Prevents supported Store apps from running in the background.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications","GlobalUserDisabled",1,0,false,false);
            R(t,"silent-apps","Background","Disable silent app installation","Stops suggested Store apps from installing silently.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager","SilentInstalledAppsEnabled",0,1,true,false);
            R(t,"suggested-apps","Background","Disable suggested apps","Stops Start menu app suggestions.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager","SystemPaneSuggestionsEnabled",0,1,true,false);
            R(t,"tips","Background","Disable Windows tips","Stops rotating Windows tips and suggestions.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager","SoftLandingEnabled",0,1,true,false);
            R(t,"welcome","Background","Disable welcome experience","Stops the post-update welcome screen.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager","SubscribedContent-310093Enabled",0,1,true,false);
            R(t,"settings-suggestions","Background","Disable Settings suggestions","Removes suggested content inside Settings.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager","SubscribedContent-338393Enabled",0,1,true,false);
            R(t,"consumer-features","Background","Disable consumer feature suggestions","Stops promotional Windows content for this user.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager","SubscribedContent-338388Enabled",0,1,true,false);
            R(t,"tailored","Background","Disable tailored experiences","Stops diagnostic data from personalizing tips and ads.",RegistryHive.CurrentUser,Privacy,"TailoredExperiencesWithDiagnosticDataEnabled",0,1,true,false);
            R(t,"feedback","Background","Reduce feedback prompts","Stops Windows from repeatedly asking for feedback.",RegistryHive.CurrentUser,@"Software\Microsoft\Siuf\Rules","NumberOfSIUFInPeriod",0,null,true,false);
            R(t,"search-history","Background","Disable device search history","Stops local Windows search history collection.",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\SearchSettings","IsDeviceSearchHistoryEnabled",0,1,false,false);

            // Explorer, 10
            R(t,"show-extensions","Explorer","Show file extensions","Makes file types visible and easier to identify.",RegistryHive.CurrentUser,Explorer,"HideFileExt",0,1,true,false);
            R(t,"launch-this-pc","Explorer","Open Explorer to This PC","Skips the Home feed when opening Explorer.",RegistryHive.CurrentUser,Explorer,"LaunchTo",1,2,false,false);
            R(t,"separate-process","Explorer","Run folders in separate process","Improves Explorer isolation at a small memory cost.",RegistryHive.CurrentUser,Explorer,"SeparateProcess",1,0,false,false);
            R(t,"disable-shake","Explorer","Disable Aero Shake","Prevents accidental minimizing when moving a window.",RegistryHive.CurrentUser,Explorer,"DisallowShaking",1,0,false,false);
            R(t,"snap-assist","Explorer","Keep Snap Assist enabled","Keeps efficient window snapping available.",RegistryHive.CurrentUser,Explorer,"SnapAssist",1,0,true,false);
            R(t,"show-status","Explorer","Show Explorer status bar","Shows item counts without opening properties.",RegistryHive.CurrentUser,Explorer,"ShowStatusBar",1,0,false,false);
            R(t,"compact-mode","Explorer","Use compact Explorer spacing","Fits more files on screen.",RegistryHive.CurrentUser,Explorer,"UseCompactMode",1,0,false,false);
            R(t,"recent-files","Explorer","Disable recent files in Home","Reduces Home feed indexing and clutter.",RegistryHive.CurrentUser,Explorer,"ShowRecent",0,1,false,false);
            R(t,"frequent-folders","Explorer","Disable frequent folders in Home","Reduces Home feed tracking and clutter.",RegistryHive.CurrentUser,Explorer,"ShowFrequent",0,1,false,false);
            R(t,"thumbs-local","Explorer","Keep thumbnails enabled","Keeps local image and video previews available.",RegistryHive.CurrentUser,Explorer,"IconsOnly",0,1,true,false);

            // Network, 10. Conservative changes and maintenance only.
            C(t,"flush-dns","Network","Flush DNS cache","Clears stale local DNS entries.","ipconfig.exe","/flushdns","",true,false);
            C(t,"renew-dhcp","Network","Renew DHCP lease","Requests a fresh local network lease. Connection may pause briefly.","ipconfig.exe","/renew","",false,false);
            C(t,"reset-winsock","Network","Reset Winsock catalog","Repairs damaged socket providers. Restart required.","netsh.exe","winsock reset","",false,true);
            C(t,"reset-ip","Network","Reset TCP/IP stack","Repairs damaged TCP/IP configuration. Restart required.","netsh.exe","int ip reset","",false,true);
            R(t,"net-throttle","Network","Disable multimedia network throttling","Removes the legacy multimedia packet throttle.",RegistryHive.LocalMachine,@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile","NetworkThrottlingIndex",unchecked((int)0xffffffff),10,true,true);
            R(t,"system-response","Network","Set gaming system responsiveness","Reserves less CPU time for background multimedia tasks.",RegistryHive.LocalMachine,@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile","SystemResponsiveness",10,20,true,true);
            R(t,"games-sf-io","Network","Set Games scheduling category","Uses the supported High scheduling category for Games.",RegistryHive.LocalMachine,@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games","Scheduling Category","High","Medium",true,true,RegistryValueKind.String);
            R(t,"games-gpu-priority","Network","Set Games GPU priority","Raises the multimedia Games GPU priority value.",RegistryHive.LocalMachine,@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games","GPU Priority",8,2,true,true);
            R(t,"games-sf-priority","Network","Set Games SFIO priority","Uses the supported High storage I/O priority for Games.",RegistryHive.LocalMachine,@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games","SFIO Priority","High","Normal",true,true,RegistryValueKind.String);
            C(t,"arp-cache","Network","Clear ARP cache","Clears stale local network address mappings.","netsh.exe","interface ip delete arpcache","",false,false);

            // Maintenance, 10
            C(t,"temp-user","Maintenance","Clean user temporary files","Removes files that are no longer locked from your Temp folder.","cmd.exe","/c del /q /f /s \"%TEMP%\\*\" 2>nul","",true,false);
            C(t,"temp-windows","Maintenance","Clean Windows temporary files","Removes unlocked files from the Windows Temp folder.","cmd.exe","/c del /q /f /s \"%WINDIR%\\Temp\\*\" 2>nul","",true,false);
            C(t,"thumb-cache","Maintenance","Clear thumbnail cache","Rebuilds Explorer thumbnail databases when needed.","cmd.exe","/c del /q /f \"%LOCALAPPDATA%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db\" 2>nul","",false,false);
            C(t,"directx-cache","Maintenance","Clean DirectX shader cache","Clears the per-user DirectX shader cache.","cmd.exe","/c del /q /f /s \"%LOCALAPPDATA%\\D3DSCache\\*\" 2>nul","",false,false);
            C(t,"dns-display","Maintenance","Verify DNS cache","Runs a quick DNS resolver cache check.","ipconfig.exe","/displaydns","",false,false);
            C(t,"component-cleanup","Maintenance","Clean replaced Windows components","Uses DISM to remove superseded component versions.","dism.exe","/Online /Cleanup-Image /StartComponentCleanup","",false,false);
            C(t,"disk-health","Maintenance","Scan Windows drive health","Runs a read-only online disk scan.","chkdsk.exe","C: /scan","",false,false);
            C(t,"system-health","Maintenance","Check Windows image health","Checks whether the Windows component store is repairable.","dism.exe","/Online /Cleanup-Image /CheckHealth","",false,false);
            C(t,"trim","Maintenance","Run SSD retrim","Sends TRIM hints to supported SSDs.","defrag.exe","C: /L /O","",false,false);
            C(t,"time-sync","Maintenance","Synchronize Windows time","Requests a time-service resynchronization.","w32tm.exe","/resync","",false,false);

            return t;
        }

        private static void R(List<TweakDefinition> list,string id,string category,string name,string description,RegistryHive hive,string path,string valueName,object value,object undo,bool recommended,bool restart)
        { R(list,id,category,name,description,hive,path,valueName,value,undo,recommended,restart,RegistryValueKind.DWord); }

        private static void R(List<TweakDefinition> list,string id,string category,string name,string description,RegistryHive hive,string path,string valueName,object value,object undo,bool recommended,bool restart,RegistryValueKind kind)
        {
            list.Add(new TweakDefinition { Id=id,Category=category,Name=name,Description=description,Hive=hive,Path=path,ValueName=valueName,Value=value,UndoValue=undo,Recommended=recommended,RestartRequired=restart,Kind=kind,Risk=hive==RegistryHive.LocalMachine?TweakRisk.Advanced:TweakRisk.Safe });
        }

        private static void C(List<TweakDefinition> list,string id,string category,string name,string description,string command,string arguments,string undo,bool recommended,bool restart)
        {
            list.Add(new TweakDefinition { Id=id,Category=category,Name=name,Description=description,Command=command+"|"+arguments,UndoCommand=undo,Recommended=recommended,RestartRequired=restart,Risk=TweakRisk.Safe });
        }
    }

    internal static class TweakExecutor
    {
        public static TweakRunResult Apply(IEnumerable<TweakDefinition> tweaks, Action<string> progress)
        {
            TweakRunResult result = new TweakRunResult();
            foreach (TweakDefinition tweak in tweaks)
            {
                try
                {
                    if (!string.IsNullOrEmpty(tweak.Command)) Run(tweak.Command);
                    else Write(tweak, false);
                    result.Applied++;
                    result.Log.Add("Applied: " + tweak.Name);
                    if (progress != null) progress("Applied: " + tweak.Name);
                }
                catch (Exception ex) { result.Errors.Add(tweak.Name + ": " + ex.Message); }
            }
            return result;
        }

        public static TweakRunResult Restore(IEnumerable<TweakDefinition> tweaks, Action<string> progress)
        {
            TweakRunResult result = new TweakRunResult();
            foreach (TweakDefinition tweak in tweaks)
            {
                if (!string.IsNullOrEmpty(tweak.Command)) continue;
                try { Write(tweak, true); result.Applied++; if (progress != null) progress("Restored: " + tweak.Name); }
                catch (Exception ex) { result.Errors.Add(tweak.Name + ": " + ex.Message); }
            }
            return result;
        }

        private static void Write(TweakDefinition tweak, bool undo)
        {
            RegistryKey root = RegistryKey.OpenBaseKey(tweak.Hive, RegistryView.Registry64);
            using (root)
            using (RegistryKey key = root.CreateSubKey(tweak.Path))
            {
                if (key == null) throw new InvalidOperationException("Registry key unavailable");
                object value = undo ? tweak.UndoValue : tweak.Value;
                if (value == null) key.DeleteValue(tweak.ValueName, false);
                else key.SetValue(tweak.ValueName, value, tweak.Kind);
            }
        }

        private static void Run(string packed)
        {
            int split = packed.IndexOf('|');
            string file = split < 0 ? packed : packed.Substring(0, split);
            string args = split < 0 ? "" : packed.Substring(split + 1);
            ProcessStartInfo psi = new ProcessStartInfo(file, Environment.ExpandEnvironmentVariables(args));
            psi.UseShellExecute = false; psi.CreateNoWindow = true;
            using (Process p = Process.Start(psi))
            {
                if (p == null) throw new InvalidOperationException("Could not start " + file);
                p.WaitForExit(120000);
                if (!p.HasExited) { p.Kill(); throw new TimeoutException(file + " timed out"); }
                if (p.ExitCode != 0 && file.IndexOf("cmd",StringComparison.OrdinalIgnoreCase)<0)
                    throw new InvalidOperationException(file + " returned " + p.ExitCode);
            }
        }
    }
}
