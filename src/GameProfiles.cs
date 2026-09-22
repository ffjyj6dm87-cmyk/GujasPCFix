using System;
using System.Collections.Generic;
using System.IO;

namespace GujasPCFix
{
    internal sealed class GameProfile
    {
        public string Name, ProcessName, LaunchOptions;
        public string[] TweakIds, InstallHints, Settings;
        public override string ToString() { return Name; }

        public string FindInstall()
        {
            foreach (string raw in InstallHints)
            {
                string path = Environment.ExpandEnvironmentVariables(raw);
                if (File.Exists(path) || Directory.Exists(path)) return path;
            }
            return null;
        }
    }

    internal static class GameProfiles
    {
        public static List<GameProfile> Create()
        {
            return new List<GameProfile>
            {
                new GameProfile {
                    Name="Counter-Strike 2", ProcessName="cs2.exe", LaunchOptions="-novid +fps_max 0",
                    TweakIds=new[]{"game-mode","game-mode-allow","disable-dvr","disable-capture","hags","fullscreen","net-throttle","system-response","games-sf-io","games-gpu-priority","games-sf-priority","flush-dns"},
                    InstallHints=new[]{@"%ProgramFiles(x86)%\Steam\steamapps\common\Counter-Strike Global Offensive\game\bin\win64\cs2.exe",@"%ProgramFiles%\Steam\steamapps\common\Counter-Strike Global Offensive\game\bin\win64\cs2.exe"},
                    Settings=new[]{"Use fullscreen mode","Enable NVIDIA Reflex or AMD Anti-Lag 2 when available","Set a stable FPS cap if frametimes fluctuate","Keep multicore rendering enabled"}
                },
                new GameProfile {
                    Name="Fortnite", ProcessName="FortniteClient-Win64-Shipping.exe", LaunchOptions="Use DirectX 12 for modern GPUs, Performance Mode for weaker PCs",
                    TweakIds=new[]{"game-mode","disable-dvr","disable-capture","hags","vrr","fullscreen","net-throttle","system-response","games-gpu-priority","flush-dns"},
                    InstallHints=new[]{@"%ProgramFiles%\Epic Games\Fortnite\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe",@"%ProgramFiles(x86)%\Epic Games\Fortnite\FortniteGame\Binaries\Win64\FortniteClient-Win64-Shipping.exe"},
                    Settings=new[]{"Use Performance Mode for maximum FPS","Use DX12 on a modern GPU after shader compilation settles","Disable cosmetic streaming only if storage space is available","Use NVIDIA Reflex Low Latency On + Boost when supported"}
                },
                new GameProfile {
                    Name="Rainbow Six Siege", ProcessName="RainbowSix.exe", LaunchOptions="Choose the DX12 or Vulkan launcher that is smoother on your driver",
                    TweakIds=new[]{"game-mode","disable-dvr","disable-capture","hags","vrr","fullscreen","net-throttle","system-response","games-sf-io","games-gpu-priority","flush-dns"},
                    InstallHints=new[]{@"%ProgramFiles(x86)%\Steam\steamapps\common\Tom Clancy's Rainbow Six Siege\RainbowSix.exe",@"%ProgramFiles(x86)%\Ubisoft\Ubisoft Game Launcher\games\Tom Clancy's Rainbow Six Siege\RainbowSix.exe"},
                    Settings=new[]{"Use fullscreen display mode","Turn VSync off for competitive play","Set render scaling to 100 percent first","Use a stable FPS limit above monitor refresh if temperatures allow"}
                },
                new GameProfile {
                    Name="Minecraft Java", ProcessName="javaw.exe", LaunchOptions="Use 4 GB RAM for vanilla, 6 to 8 GB for large modpacks",
                    TweakIds=new[]{"game-mode","disable-dvr","disable-capture","hags","fullscreen","temp-user","directx-cache"},
                    InstallHints=new[]{@"%APPDATA%\.minecraft",@"%LOCALAPPDATA%\Packages\Microsoft.4297127D64EC6_8wekyb3d8bbwe"},
                    Settings=new[]{"Install Sodium for Fabric performance","Do not allocate all system RAM to Java","Lower simulation distance before render distance","Use G1GC unless a modpack documents another collector"}
                },
                new GameProfile {
                    Name="Far Cry 6", ProcessName="FarCry6.exe", LaunchOptions="Use the in-game benchmark after every graphics change",
                    TweakIds=new[]{"game-mode","disable-dvr","disable-capture","hags","vrr","fullscreen","games-gpu-priority","directx-cache"},
                    InstallHints=new[]{@"%ProgramFiles(x86)%\Ubisoft\Ubisoft Game Launcher\games\Far Cry 6\bin\FarCry6.exe",@"%ProgramFiles%\Epic Games\FarCry6\bin\FarCry6.exe"},
                    Settings=new[]{"Disable HD textures on GPUs with less than 11 GB VRAM","Use FSR Quality before lowering resolution","Lower shadows and geometry before textures","Keep VSync off when FreeSync or G-Sync is active"}
                }
            };
        }
    }
}
