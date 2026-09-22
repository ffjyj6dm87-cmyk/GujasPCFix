using System;
using System.Collections.Generic;
using System.Text;

namespace GujasPCFix
{
    internal enum HardwareTier
    {
        Unknown,
        Low,
        Mid,
        High
    }

    internal sealed class TuneProfile
    {
        public string Summary;
        public string[] Tips;
        public bool CleanTemp = true;
        public bool EmptyRecycleBin = true;
        public bool FlushDns = true;
        public bool HighPerformancePower;
        public bool GameMode = true;
        public bool VisualPerformance;
        public bool GpuScheduling = true;
        public bool NetworkTuning = true;
        public bool Thumbnails = true;
    }

    internal static class HardwareAdvisor
    {
        public static TuneProfile Recommend(string cpu, string gpu, int ramGb)
        {
            HardwareTier cpuTier = ClassifyCpu(cpu);
            HardwareTier gpuTier = ClassifyGpu(gpu);
            bool nvidia = Contains(gpu, "nvidia") || Contains(gpu, "geforce") || Contains(gpu, "rtx") || Contains(gpu, "gtx");
            bool amdGpu = Contains(gpu, "amd") || Contains(gpu, "radeon") || Contains(gpu, "rx ");
            bool intelGpu = Contains(gpu, "intel") && (Contains(gpu, "uhd") || Contains(gpu, "iris") || Contains(gpu, "arc"));
            bool laptopHint = Contains(gpu, "laptop") || Contains(cpu, "u ") || EndsWithU(cpu);

            TuneProfile profile = new TuneProfile();
            HardwareTier overall = Combine(cpuTier, gpuTier);

            profile.HighPerformancePower = overall != HardwareTier.Low || !laptopHint;
            profile.VisualPerformance = overall == HardwareTier.Low || ramGb > 0 && ramGb < 8;
            profile.GameMode = true;
            profile.GpuScheduling = overall != HardwareTier.Low;
            profile.NetworkTuning = true;
            profile.Thumbnails = true;

            StringBuilder summary = new StringBuilder();
            summary.Append("Profile for ");
            summary.Append(string.IsNullOrWhiteSpace(cpu) ? "your CPU" : cpu.Trim());
            summary.Append(" + ");
            summary.Append(string.IsNullOrWhiteSpace(gpu) ? "your GPU" : gpu.Trim());
            summary.Append(". ");
            summary.Append(DescribeTier(overall));
            if (ramGb > 0)
                summary.Append(" RAM detected/entered context: " + ramGb + " GB.");
            profile.Summary = summary.ToString();

            List<string> tips = new List<string>();
            tips.Add("Gujas PC Fix will tick Boost options that match this hardware. Review them, then press BOOST PC.");

            if (overall == HardwareTier.High)
            {
                tips.Add("This is a strong setup: use High performance (or Ultimate Performance) so CPU/GPU are not power-limited.");
                tips.Add("Leave Windows visual effects on — turning them off will not raise FPS on high-end cards.");
                tips.Add("In games, start at your monitor's native resolution with DLSS/FSR Quality if you have it, not Ultra-everything blindly.");
            }
            else if (overall == HardwareTier.Mid)
            {
                tips.Add("Sweet-spot hardware: High performance power plan, Game Mode on, and a clean temp folder before long sessions.");
                tips.Add("In games, prefer High (not Ultra) textures if VRAM is 8 GB or less; cap FPS to your monitor refresh.");
            }
            else if (overall == HardwareTier.Low)
            {
                tips.Add("Prioritize free disk/RAM: temp cleanup, Recycle Bin, and best-performance visual effects help more than fake RAM boosters.");
                tips.Add("Use 1080p or 900p in games, Low/Medium presets, and FSR/XeSS Performance if available.");
                if (laptopHint)
                    tips.Add("If this is a laptop, plug in before boosting. High performance drains battery and raises heat.");
            }
            else
            {
                tips.Add("Could not fully classify this hardware. Cleaning junk files, flushing DNS, and Game Mode are still safe wins.");
            }

            if (nvidia)
            {
                tips.Add("NVIDIA: in NVIDIA Control Panel set Power management mode to Prefer maximum performance for games, Low Latency Mode to On.");
                if (Contains(gpu, "rtx"))
                    tips.Add("RTX: use DLSS if the game supports it; Frame Generation only if you already have a high, stable FPS.");
            }
            else if (amdGpu)
            {
                tips.Add("AMD: in Radeon Software use Radeon Anti-Lag for competitive titles; keep Chill off if you want uncapped FPS.");
                tips.Add("Enable FSR in-game rather than forcing sharpening globally.");
            }
            else if (intelGpu)
            {
                tips.Add("Intel graphics: keep drivers current from Intel, enable XeSS where available, and avoid overlay-heavy recording tools.");
            }

            if (Contains(cpu, "amd") || Contains(cpu, "ryzen"))
            {
                tips.Add("Ryzen: Windows Game Mode plus up-to-date chipset drivers matter more than disabling Core parking by hand.");
            }
            else if (Contains(cpu, "intel"))
            {
                tips.Add("Intel: leave Efficiency cores enabled on 12th-gen and newer; Windows already schedules games onto P-cores.");
            }

            if (ramGb > 0 && ramGb < 16)
                tips.Add("Under 16 GB RAM: close Chrome/Discord overlays you do not need, and keep visual-effects trim enabled.");
            else if (ramGb >= 32)
                tips.Add("32 GB+ RAM: skip aggressive 'RAM cleaner' tools — they only page working sets to disk and can hitch.");

            profile.Tips = tips.ToArray();
            return profile;
        }

        private static bool EndsWithU(string cpu)
        {
            if (string.IsNullOrEmpty(cpu)) return false;
            string t = cpu.Trim();
            return t.EndsWith("U", StringComparison.OrdinalIgnoreCase) || t.IndexOf("-U ", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static HardwareTier Combine(HardwareTier cpu, HardwareTier gpu)
        {
            if (gpu == HardwareTier.High || cpu == HardwareTier.High && gpu != HardwareTier.Low)
                return HardwareTier.High;
            if (gpu == HardwareTier.Low || cpu == HardwareTier.Low)
                return gpu == HardwareTier.Low && cpu == HardwareTier.Low ? HardwareTier.Low : (gpu == HardwareTier.Low ? HardwareTier.Low : HardwareTier.Mid);
            if (gpu == HardwareTier.Mid || cpu == HardwareTier.Mid)
                return HardwareTier.Mid;
            return HardwareTier.Unknown;
        }

        private static string DescribeTier(HardwareTier tier)
        {
            if (tier == HardwareTier.High) return "Classified as high-end.";
            if (tier == HardwareTier.Mid) return "Classified as mid-range.";
            if (tier == HardwareTier.Low) return "Classified as entry-level / integrated.";
            return "Classification uncertain from the names entered.";
        }

        private static HardwareTier ClassifyCpu(string cpu)
        {
            if (string.IsNullOrEmpty(cpu)) return HardwareTier.Unknown;
            if (Contains(cpu, "celeron") || Contains(cpu, "pentium") || Contains(cpu, "atom") || Contains(cpu, "athlon 3") || Contains(cpu, "i3") || Contains(cpu, "ryzen 3") || Contains(cpu, "r3 "))
                return HardwareTier.Low;
            if (Contains(cpu, "i9") || Contains(cpu, "i7") || Contains(cpu, "ryzen 9") || Contains(cpu, "ryzen 7") || Contains(cpu, "r9 ") || Contains(cpu, "r7 ") || Contains(cpu, "threadripper") || Contains(cpu, "xeon"))
                return HardwareTier.High;
            if (Contains(cpu, "i5") || Contains(cpu, "ryzen 5") || Contains(cpu, "r5 "))
                return HardwareTier.Mid;
            return HardwareTier.Unknown;
        }

        private static HardwareTier ClassifyGpu(string gpu)
        {
            if (string.IsNullOrEmpty(gpu)) return HardwareTier.Unknown;
            if (Contains(gpu, "uhd") || Contains(gpu, "vega 8") || Contains(gpu, "vega 7") || Contains(gpu, "iris") && !Contains(gpu, "arc") || Contains(gpu, "1650") || Contains(gpu, "1630") || Contains(gpu, "1050") || Contains(gpu, "rx 6400") || Contains(gpu, "mx "))
                return HardwareTier.Low;
            if (Contains(gpu, "4090") || Contains(gpu, "4080") || Contains(gpu, "4070") || Contains(gpu, "3090") || Contains(gpu, "3080") || Contains(gpu, "7900") || Contains(gpu, "7800") || Contains(gpu, "a770") || Contains(gpu, "ti"))
            {
                if (Contains(gpu, "1650") || Contains(gpu, "1050")) return HardwareTier.Low;
                return HardwareTier.High;
            }
            if (Contains(gpu, "4060") || Contains(gpu, "3070") || Contains(gpu, "3060") || Contains(gpu, "2080") || Contains(gpu, "2070") || Contains(gpu, "6750") || Contains(gpu, "6700") || Contains(gpu, "7600") || Contains(gpu, "arc a750") || Contains(gpu, "arc a580"))
                return HardwareTier.Mid;
            if (Contains(gpu, "rtx") || Contains(gpu, "rx 6") || Contains(gpu, "rx 7"))
                return HardwareTier.Mid;
            if (Contains(gpu, "gtx") || Contains(gpu, "radeon"))
                return HardwareTier.Low;
            return HardwareTier.Unknown;
        }

        private static bool Contains(string hay, string needle)
        {
            return hay != null && hay.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
