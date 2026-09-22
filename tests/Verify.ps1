$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path qa | Out-Null
$compiler = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$sourceFiles = @((Get-ChildItem src -Filter '*.cs').FullName) + @((Resolve-Path tests/Verification.cs).Path)
& $compiler /nologo /target:exe /main:GujasPCFix.Verification /out:qa/Verification.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /res:assets/design-reference.png,GujasPCFix.games.png /res:assets/dashboard-reference.png,GujasPCFix.dashboard.png $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Verification compilation failed' }
& ./qa/Verification.exe 2>&1 | Tee-Object -FilePath qa/results.txt
if ($LASTEXITCODE -ne 0) { throw 'Verification failed' }
