# Gujas PC Fix

A transparent Windows 10 and 11 performance utility. Every tweak shows what it changes before it runs.

## Version 2.1 Reference Edition

- Dashboard and Game Tweaks match the supplied blue and black reference layouts
- 60 reviewed catalog entries, explicitly separated into automatic operations, manual Windows controls and unavailable entries
- Search games and filter each profile by category
- Save original values and types before supported automatic registry changes, then verify both apply and restore by reading back
- Game profiles for Counter-Strike 2, Fortnite, Rainbow Six Siege, Minecraft Java and Far Cry 6
- Game-specific manual graphics guidance, separately labelled from system-wide Windows changes
- System scan and technical activity log
- Administrator manifest for protected changes

## Build

Open `GujasPCFix.csproj` in Visual Studio 2022 and build Release, or run `build.bat` from a Developer Command Prompt on Windows.

The GitHub Actions workflow also creates a downloadable Windows build artifact after every push.

Download the versioned `GujasPCFix-2.1-Reference-Windows` artifact from a successful run, and launch `GujasPCFix-2.1.exe`. The old executable in the repository root is not the new build.

## Verification and limits

The Windows workflow checks all catalog IDs and profile links, blocks unverified registry writes, tests original-value backup and restoration in a unique disposable HKCU key, verifies a nonzero command exit is reported as failure, and renders every page. The QA artifact contains screenshots, the test transcript and a per-entry review table.

These checks do not prove FPS improvements or validate every command on all hardware. Destructive cleanup and network resets are not run on the build machine. Most legacy registry shortcuts are now manual Windows controls because writing a registry value does not prove Windows uses it. The two MMCSS GPU and SFIO priorities are blocked because Microsoft documents them as unused. Native game files are not edited automatically.

Sources: [MMCSS](https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service), [Windows settings](https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings), [disk scan](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/chkdsk), [drive optimization](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/defrag).

## Safety

The app does not disable Windows Security, Windows Update, the firewall, core isolation or system services. Command-based maintenance cannot be undone automatically. Registry restoration only runs when an original-value backup exists. Windows settings availability depends on OS version and hardware.
