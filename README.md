# Gujas PC Fix

A transparent Windows 10 and 11 performance utility. Every tweak shows what it changes before it runs.

## Version 2 features

- 60 organized Windows performance and maintenance tweaks
- Search and category filters
- Recommended preset
- Restore defaults for registry tweaks
- Game profiles for Counter-Strike 2, Fortnite, Rainbow Six Siege, Minecraft Java and Far Cry 6
- Automatic game detection in common install folders
- System scan and technical activity log
- Administrator manifest for protected changes

## Build

Open `GujasPCFix.csproj` in Visual Studio 2022 and build Release, or run `build.bat` from a Developer Command Prompt on Windows.

The GitHub Actions workflow also creates a downloadable Windows build artifact after every push.

## Safety

The app does not disable Windows Security, Windows Update, the firewall, core isolation or system services. Command-based maintenance actions cannot be automatically undone. Registry tweaks include a Restore defaults action.
