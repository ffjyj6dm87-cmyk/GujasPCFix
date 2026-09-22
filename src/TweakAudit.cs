using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GujasPCFix
{
    internal static class TweakAudit
    {
        const string Mmcss = "https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service";
        public static void Review(List<TweakDefinition> tweaks)
        {
            foreach (TweakDefinition t in tweaks)
            {
                t.Recommended = false;
                if (string.IsNullOrEmpty(t.Command))
                {
                    // A writable registry key is NOT evidence that Windows consumes it.
                    t.SettingsUri = SettingsFor(t);
                    t.DisabledReason = "Direct registry behavior has not been verified for your Windows version. Use the supported Windows control.";
                    t.Evidence = "https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings-app";
                }
                else
                {
                    t.Risk = TweakRisk.Advanced;
                    t.Evidence = "https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/" + t.Command.Split('|')[0].Replace(".exe", "");
                }
            }
            foreach (string id in new[] { "system-response", "game-task-priority" })
            {
                TweakDefinition t = Find(tweaks, id); t.SettingsUri = null; t.DisabledReason = null; t.Evidence = Mmcss;
                t.Description = id == "system-response"
                    ? "Sets the MMCSS background CPU reservation to 10%. Only affects applications using MMCSS. No guaranteed FPS or latency gain."
                    : "Sets MMCSS Games task priority to 6. Only affects registered MMCSS threads. Ignored when Scheduling Category is High.";
            }
            foreach (string id in new[] { "games-gpu-priority", "games-sf-priority" })
            {
                TweakDefinition t = Find(tweaks, id); t.SettingsUri = null; t.Evidence = Mmcss;
                t.DisabledReason = "Unavailable: Microsoft documents this priority value as unused. It cannot provide the advertised optimization.";
                t.Description = t.DisabledReason;
            }
            Find(tweaks, "games-sf-io").Description = "MMCSS High scheduling is intended for pro audio, not a general gaming boost. Automatic change disabled.";
            Find(tweaks, "listview-alpha").Description = "Selection rectangle appearance, not list fade or a demonstrated performance improvement.";
            Find(tweaks, "animations").Path = @"Control Panel\Desktop\WindowMetrics";
            Find(tweaks, "animations").Description = "Control minimize and maximize animations using Windows visual effects settings.";
            foreach (string id in new[] { "temp-user", "temp-windows", "thumb-cache", "directx-cache" })
            {
                TweakDefinition t = Find(tweaks, id); t.SettingsUri = "ms-settings:storagesense";
                t.DisabledReason = "Review and delete files using Windows Storage. The previous silent recursive deletion command is disabled.";
                t.Command = null;
                t.Description = "Open Windows Storage to review cleanup candidates and confirm deletion. Cleared caches may need rebuilding.";
            }
            string drive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)).TrimEnd('\\');
            Find(tweaks, "disk-health").Command = "chkdsk.exe|" + drive + " /scan";
            Find(tweaks, "disk-health").Description = "Runs an online NTFS scan on the Windows volume. This is maintenance, not an FPS optimization. May perform online repairs.";
            Find(tweaks, "trim").Command = "defrag.exe|" + drive + " /L";
            Find(tweaks, "trim").Description = "Requests retrim on the Windows volume. Requires filesystem and device support; unsupported volumes return an error.";
            Find(tweaks, "reset-ip").Command = "netsh.exe|int ip reset";
            Find(tweaks, "reset-ip").Description = "Resets network configuration and can remove custom settings. Use only for repair; restart required.";
            Find(tweaks, "dns-display").Description = "Displays cached DNS records in the activity log. Does not optimize or repair the system.";
            Find(tweaks, "flush-dns").Description = "Clears the DNS resolver cache for troubleshooting. Does not lower established game-server ping.";
            Find(tweaks, "system-health").Description = "Checks whether image corruption has already been flagged. Does not perform a full scan or repair.";
        }
        static TweakDefinition Find(List<TweakDefinition> tweaks, string id) { return tweaks.Single(t => t.Id == id); }
        static string SettingsFor(TweakDefinition t)
        {
            if (t.Id == "disable-shake" || t.Id == "snap-assist") return "ms-settings:multitasking";
            if (t.Category == "Explorer") return "explorer-options";
            if (t.Id == "background-apps") return "ms-settings:appsfeatures";
            if (t.Id == "tailored" || t.Id == "feedback") return "ms-settings:privacy-feedback";
            if (t.Id == "tips" || t.Id == "welcome") return "ms-settings:notifications";
            if (t.Id == "search-history") return "ms-settings:search-permissions";
            if (t.Id == "game-mode" || t.Id == "game-mode-allow") return "ms-settings:gaming-gamemode";
            if (t.Id == "hags" || t.Id == "vrr" || t.Id == "fullscreen") return "ms-settings:display-advancedgraphics";
            if (t.Category == "Gaming") return "ms-settings:gaming-gamedvr";
            if (t.Category == "Network") return "ms-settings:network-status";
            if (t.Category == "Background") return "ms-settings:privacy";
            return "ms-settings:easeofaccess-visualeffects";
        }
    }
}
