using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace GujasPCFix
{
    internal sealed class BoostOptions
    {
        public bool CleanTemp;
        public bool EmptyRecycleBin;
        public bool FlushDns;
        public bool HighPerformancePower;
        public bool GameMode;
        public bool VisualPerformance;
        public bool GpuScheduling;
        public bool NetworkTuning;
        public bool Thumbnails;
    }

    internal sealed class ScanResult
    {
        public long TempBytes;
        public string CpuName;
        public int LogicalCores;
        public ulong TotalRamBytes;
        public ulong AvailRamBytes;
        public List<DriveSnapshot> Drives = new List<DriveSnapshot>();
        public string OsName;
        public string PowerPlan;
        public string GpuName;
        public uint MemoryLoad;
        public string OsCaption;
    }

    internal sealed class DriveSnapshot
    {
        public string Name;
        public long TotalBytes;
        public long FreeBytes;
    }

    internal sealed class BoostResult
    {
        public long BytesFreed;
        public int FilesRemoved;
        public List<string> Log = new List<string>();
        public List<string> Warnings = new List<string>();
    }

    internal static class PcFixEngine
    {
        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        public static ScanResult Scan(Action<string> progress)
        {
            var result = new ScanResult();
            progress("Reading hardware...");

            result.LogicalCores = Environment.ProcessorCount;
            result.CpuName = ReadCpuName();
            result.GpuName = ReadGpuName();
            result.OsName = Environment.OSVersion.VersionString;

            var mem = QueryMemory();
            result.TotalRamBytes = mem.ullTotalPhys;
            result.AvailRamBytes = mem.ullAvailPhys;
            result.MemoryLoad = mem.dwMemoryLoad;
            result.OsCaption = ReadOsCaption();

            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
            {
                try
                {
                    result.Drives.Add(new DriveSnapshot
                    {
                        Name = drive.Name,
                        TotalBytes = drive.TotalSize,
                        FreeBytes = drive.AvailableFreeSpace
                    });
                }
                catch
                {
                    // skip unreadable volumes
                }
            }

            progress("Measuring junk files...");
            result.TempBytes = MeasureFolders(GetTempTargets());
            result.PowerPlan = QueryActivePowerPlan();
            return result;
        }

        public static BoostResult Boost(BoostOptions options, Action<string> progress)
        {
            var result = new BoostResult();
            Action<string> log = delegate(string msg)
            {
                result.Log.Add(msg);
                progress(msg);
            };

            if (options.CleanTemp)
            {
                log("[01] Temp & INet cache  %TEMP%  %WINDIR%\\Temp  INetCache");
                int files;
                long bytes;
                CleanFolders(GetTempTargets(), out files, out bytes);
                result.FilesRemoved += files;
                result.BytesFreed += bytes;
                log("    OK  " + FormatBytes(bytes) + "  " + files + " objects removed (in-use files skipped).");
            }

            if (options.EmptyRecycleBin)
            {
                log("[02] Recycle Bin  SHEmptyRecycleBin");
                try
                {
                    int hr = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                    if (hr == 0 || hr == -2147418113)
                        log("Recycle Bin emptied.");
                    else
                        result.Warnings.Add("Recycle Bin returned code " + hr + " (may already be empty).");
                }
                catch (Exception ex)
                {
                    result.Warnings.Add("Recycle Bin: " + ex.Message);
                }
            }

            if (options.FlushDns)
            {
                log("[03] DNS resolver cache  ipconfig /flushdns");
                RunHidden("ipconfig.exe", "/flushdns", log, result);
            }

            if (options.HighPerformancePower)
            {
                log("[04] Power scheme  powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                ApplyHighPerformance(log, result);
            }

            if (options.GameMode)
            {
                log("[05] Game Mode  HKCU\\Software\\Microsoft\\GameBar  AutoGameModeEnabled=1");
                try
                {
                    EnableGameMode();
                    log("    OK  GameBar AutoGameModeEnabled / AllowAutoGameMode  GameDVR_Enabled=0");
                }
                catch (Exception ex)
                {
                    result.Warnings.Add("Game Mode: " + ex.Message);
                }
            }

            if (options.VisualPerformance)
            {
                log("[06] VisualFXSetting=2 (best performance)  HKCU\\...\\Explorer\\VisualEffects");
                try
                {
                    ApplyVisualPerformance();
                    log("    OK  Sign-out may be required for Explorer to pick this up.");
                }
                catch (Exception ex)
                {
                    result.Warnings.Add("Visual effects: " + ex.Message);
                }
            }

            if (options.Thumbnails)
            {
                log("[07] Explorer thumbnail cache  %LOCALAPPDATA%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db");
                int files;
                long bytes;
                CleanThumbnails(out files, out bytes);
                result.FilesRemoved += files;
                result.BytesFreed += bytes;
                log("    Freed " + FormatBytes(bytes) + " across " + files + " cache files.");
            }

            if (options.GpuScheduling)
            {
                log("[08] Hardware-accelerated GPU scheduling  HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers HwSchMode=2");
                try
                {
                    ApplyGpuScheduling();
                    log("    OK  Takes effect after reboot on supported GPUs (Windows 10 2004+).");
                }
                catch (Exception ex)
                {
                    result.Warnings.Add("GPU scheduling: " + ex.Message);
                }
            }

            if (options.NetworkTuning)
            {
                log("[09] Multimedia SystemProfile  NetworkThrottlingIndex=0xFFFFFFFF  SystemResponsiveness=10");
                try
                {
                    ApplyNetworkTuning();
                    log("    OK  Reduces Windows multimedia network throttle for games.");
                }
                catch (Exception ex)
                {
                    result.Warnings.Add("Network tuning: " + ex.Message);
                }
            }

            log("---- pass complete ----");
            return result;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return kb.ToString("0.#") + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024) return mb.ToString("0.#") + " MB";
            return (mb / 1024.0).ToString("0.##") + " GB";
        }

        public static string FormatBytes(ulong bytes)
        {
            return FormatBytes((long)bytes);
        }

        private static MEMORYSTATUSEX QueryMemory()
        {
            var mem = new MEMORYSTATUSEX();
            mem.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            GlobalMemoryStatusEx(ref mem);
            return mem;
        }

        private static string ReadCpuName()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                {
                    if (key != null)
                    {
                        var name = key.GetValue("ProcessorNameString") as string;
                        if (!string.IsNullOrWhiteSpace(name))
                            return name.Trim();
                    }
                }
            }
            catch
            {
            }
            return "Unknown CPU";
        }

        public static string ReadOsCaption()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        string product = key.GetValue("ProductName") as string;
                        string build = key.GetValue("CurrentBuild") as string;
                        if (!string.IsNullOrEmpty(product) && !string.IsNullOrEmpty(build))
                            return product + "  build " + build;
                        if (!string.IsNullOrEmpty(product))
                            return product;
                    }
                }
            }
            catch
            {
            }
            return Environment.OSVersion.VersionString;
        }

        public static string ReadGpuName()
        {
            string fromWmic = QueryCommand("wmic.exe", "path Win32_VideoController get Name");
            string gpu = FirstHardwareLine(fromWmic, "Name");
            if (!string.IsNullOrEmpty(gpu))
                return gpu;

            string fromPs = QueryCommand("powershell.exe", "-NoProfile -NonInteractive -Command \"Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name\"");
            gpu = FirstHardwareLine(fromPs, null);
            if (!string.IsNullOrEmpty(gpu))
                return gpu;

            return "Unknown GPU";
        }

        private static string QueryCommand(string file, string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = file;
                psi.Arguments = args;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process p = Process.Start(psi))
                {
                    if (p == null) return "";
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(12000);
                    return output == null ? "" : output;
                }
            }
            catch
            {
                return "";
            }
        }

        private static string FirstHardwareLine(string output, string skipHeader)
        {
            if (string.IsNullOrEmpty(output)) return "";
            string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;
                if (skipHeader != null && string.Equals(line, skipHeader, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (line.IndexOf("Microsoft Basic", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                return line;
            }
            return "";
        }

        private static IEnumerable<string> GetTempTargets()
        {
            var list = new List<string>();
            AddIfExists(list, Path.GetTempPath());
            AddIfExists(list, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"));
            AddIfExists(list, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
            AddIfExists(list, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\INetCache"));
            AddIfExists(list, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer"));
            return list.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static void AddIfExists(List<string> list, string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                list.Add(path);
        }

        private static long MeasureFolders(IEnumerable<string> folders)
        {
            long total = 0;
            foreach (var folder in folders)
                total += MeasureFolder(folder);
            return total;
        }

        private static long MeasureFolder(string folder)
        {
            long total = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
                {
                    try { total += new FileInfo(file).Length; }
                    catch { }
                }
                foreach (var dir in Directory.EnumerateDirectories(folder))
                {
                    try { total += MeasureFolder(dir); }
                    catch { }
                }
            }
            catch
            {
            }
            return total;
        }

        private static void CleanFolders(IEnumerable<string> folders, out int files, out long bytes)
        {
            files = 0;
            bytes = 0;
            foreach (var folder in folders)
            {
                int f;
                long b;
                CleanFolder(folder, folder, out f, out b);
                files += f;
                bytes += b;
            }
        }

        private static void CleanFolder(string root, string folder, out int files, out long bytes)
        {
            files = 0;
            bytes = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        long size = info.Length;
                        info.Attributes = FileAttributes.Normal;
                        info.Delete();
                        files++;
                        bytes += size;
                    }
                    catch
                    {
                        // in-use files stay
                    }
                }

                foreach (var dir in Directory.EnumerateDirectories(folder))
                {
                    int f;
                    long b;
                    CleanFolder(root, dir, out f, out b);
                    files += f;
                    bytes += b;
                    try
                    {
                        if (!PathsEqual(root, dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                            Directory.Delete(dir, false);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private static bool PathsEqual(string a, string b)
        {
            return string.Equals(
                Path.GetFullPath(a).TrimEnd('\\'),
                Path.GetFullPath(b).TrimEnd('\\'),
                StringComparison.OrdinalIgnoreCase);
        }

        private static void RunHidden(string file, string args, Action<string> log, BoostResult result)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var p = Process.Start(psi))
                {
                    if (p == null)
                    {
                        result.Warnings.Add("Could not start " + file);
                        return;
                    }
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(20000);
                    if (!string.IsNullOrWhiteSpace(output))
                        log(output.Trim());
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add(file + ": " + ex.Message);
            }
        }

        private static string QueryActivePowerPlan()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "/getactivescheme",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using (var p = Process.Start(psi))
                {
                    if (p == null) return "Unknown";
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(8000);
                    int start = output.LastIndexOf('(');
                    int end = output.LastIndexOf(')');
                    if (start >= 0 && end > start)
                        return output.Substring(start + 1, end - start - 1).Trim();
                    return output.Trim();
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        private static void ApplyHighPerformance(Action<string> log, BoostResult result)
        {
            const string highPerf = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
            const string ultimate = "e9a42b02-d5df-448d-aa00-03f14749eb61";

            if (TrySetPlan(highPerf))
            {
                log("High performance power plan is active.");
                return;
            }

            RunHidden("powercfg.exe", "-duplicatescheme " + ultimate, log, result);
            if (TrySetPlan(ultimate))
            {
                log("Ultimate Performance power plan is active.");
                return;
            }

            result.Warnings.Add("Could not switch power plan. Some OEM laptops hide High performance.");
        }

        private static bool TrySetPlan(string guid)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "/setactive " + guid,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var p = Process.Start(psi))
                {
                    if (p == null) return false;
                    p.WaitForExit(8000);
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void EnableGameMode()
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\GameBar"))
            {
                if (key == null) throw new InvalidOperationException("Could not open GameBar key.");
                key.SetValue("AutoGameModeEnabled", 1, Microsoft.Win32.RegistryValueKind.DWord);
                key.SetValue("AllowAutoGameMode", 1, Microsoft.Win32.RegistryValueKind.DWord);
            }
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore"))
            {
                if (key != null)
                    key.SetValue("GameDVR_Enabled", 0, Microsoft.Win32.RegistryValueKind.DWord);
            }
        }

        private static void ApplyVisualPerformance()
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"))
            {
                if (key != null)
                    key.SetValue("VisualFXSetting", 2, Microsoft.Win32.RegistryValueKind.DWord);
            }
        }

        private static void ApplyGpuScheduling()
        {
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"))
            {
                if (key == null) throw new InvalidOperationException("GraphicsDrivers key unavailable.");
                key.SetValue("HwSchMode", 2, Microsoft.Win32.RegistryValueKind.DWord);
            }
        }

        private static void ApplyNetworkTuning()
        {
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
            {
                if (key == null) throw new InvalidOperationException("Multimedia SystemProfile key unavailable.");
                key.SetValue("NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), Microsoft.Win32.RegistryValueKind.DWord);
                key.SetValue("SystemResponsiveness", 10, Microsoft.Win32.RegistryValueKind.DWord);
            }
        }

        private static void CleanThumbnails(out int files, out long bytes)
        {
            files = 0;
            bytes = 0;
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer");
            if (!Directory.Exists(folder)) return;
            string[] caches;
            try
            {
                caches = Directory.GetFiles(folder, "thumbcache_*.db");
            }
            catch
            {
                return;
            }
            for (int i = 0; i < caches.Length; i++)
            {
                try
                {
                    FileInfo info = new FileInfo(caches[i]);
                    long size = info.Length;
                    info.Attributes = FileAttributes.Normal;
                    info.Delete();
                    files++;
                    bytes += size;
                }
                catch
                {
                }
            }
        }
    }
}
