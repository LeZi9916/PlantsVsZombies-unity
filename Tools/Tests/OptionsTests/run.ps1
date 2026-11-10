param(
    [string]$Unity = 'C:\Program Files\Unity Editors\6000.3.17f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot '..\..\Assets\Scripts\UI\OptionsView.cs'
$project = Join-Path $PSScriptRoot 'UnityProject'
$log = Join-Path $PSScriptRoot 'unity-options-tests.log'
$env:PVZ_OPTIONS_RESOURCE_ROOT = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\extracted_assets\raw')).Path
Copy-Item -LiteralPath $source -Destination (Join-Path $project 'Assets\OptionsView.cs') -Force
$process = Start-Process -FilePath $Unity -ArgumentList @(
    '-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'OptionsEditorTests.Run', '-logFile', ('"' + $log + '"')
) -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(180000)) { throw "Unity test timed out; see $log" }
Select-String -LiteralPath $log -Pattern 'OPTIONS_TESTS_(PASS|FAIL)'
if ($process.ExitCode -ne 0) { throw "Unity test process failed with exit code $($process.ExitCode); see $log" }
if (-not (Select-String -LiteralPath $log -Pattern 'OPTIONS_TESTS_PASS' -Quiet)) {
    throw "Unity exited without a test pass marker; see $log"
}
