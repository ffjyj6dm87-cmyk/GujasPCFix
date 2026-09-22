@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Could not find the C# compiler at %CSC%
  exit /b 1
)
if not exist src\Program.cs (
  echo Run this from the GujasPCFix folder.
  exit /b 1
)
if not exist assets\bg.jpg (
  echo Missing assets\bg.jpg
  exit /b 1
)
"%CSC%" /nologo /optimize+ /target:winexe /platform:anycpu /win32manifest:app.manifest /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /res:assets\bg.jpg,GujasPCFix.bg.jpg /res:assets\design-reference.png,GujasPCFix.games.png /res:assets\dashboard-reference.png,GujasPCFix.dashboard.png /out:GujasPCFix.exe src\*.cs
if errorlevel 1 exit /b 1
echo Built GujasPCFix.exe
endlocal
