param(
    [string]$Unity = 'C:\Program Files\Unity Editors\6000.3.17f1\Editor\Unity.exe',
    [switch]$SkipPlayer
)

$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot '..\..\Assets\Scripts\UI\HelpView.cs'
$project = Join-Path $PSScriptRoot 'UnityProject'
$env:PVZ_HELP_RESOURCE_ROOT = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\extracted_assets\raw')).Path
$env:PVZ_HELP_TEST_DIR = $PSScriptRoot
Copy-Item -LiteralPath $source -Destination (Join-Path $project 'Assets\HelpView.cs') -Force

function Wait-Child($process, [string]$description) {
    if (-not $process.WaitForExit(300000)) {
        $process.Kill()
        $process.WaitForExit()
        throw "$description timed out"
    }
    $process.Refresh()
}

function Invoke-Unity([string]$method, [string]$logName) {
    $log = Join-Path $PSScriptRoot $logName
    Remove-Item -LiteralPath $log -ErrorAction SilentlyContinue
    $p = Start-Process -FilePath $Unity -ArgumentList @(
        '-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'),
        '-executeMethod', $method, '-logFile', ('"' + $log + '"')
    ) -PassThru -WindowStyle Hidden
    Wait-Child $p $method
    Select-String -LiteralPath $log -Pattern 'HELP_.*(PASS|FAIL)' | ForEach-Object { $_.Line }
    if ($p.ExitCode -ne 0) { throw "$method exited $($p.ExitCode); see $log" }
    if (-not (Select-String -LiteralPath $log -Pattern 'HELP_.*_PASS' -Quiet)) {
        throw "$method did not write a pass marker; see $log"
    }
}

Invoke-Unity 'HelpEditorTests.Run' 'unity-editor.log'
if (-not $SkipPlayer) {
    $buildLog = Join-Path $PSScriptRoot 'unity-build.log'
    Remove-Item -LiteralPath $buildLog -ErrorAction SilentlyContinue
    $p = Start-Process -FilePath $Unity -ArgumentList @(
        '-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $project + '"'),
        '-executeMethod', 'HelpBuild.Build', '-logFile', ('"' + $buildLog + '"')
    ) -PassThru -WindowStyle Hidden
    Wait-Child $p 'HelpBuild.Build'
    Select-String -LiteralPath $buildLog -Pattern 'HELP_.*(PASS|FAIL)' | ForEach-Object { $_.Line }
    if ($p.ExitCode -ne 0 -or -not (Select-String -LiteralPath $buildLog -Pattern 'HELP_BUILD_PASS' -Quiet)) {
        throw "Help player build failed; see $buildLog"
    }
    $playerLog = Join-Path $PSScriptRoot 'unity-player.log'
    $player = Join-Path $project 'SmokeBuild\HelpSmoke.exe'
    Remove-Item -LiteralPath $playerLog -ErrorAction SilentlyContinue
    $p = Start-Process -FilePath $player -ArgumentList @(
        '-screen-fullscreen', '0', '-screen-width', '800', '-screen-height', '600',
        '-help-root', ('"' + $env:PVZ_HELP_RESOURCE_ROOT + '"'),
        '-help-output', ('"' + $PSScriptRoot + '"'), '-logFile', ('"' + $playerLog + '"')
    ) -PassThru -WindowStyle Hidden
    Wait-Child $p 'Help player smoke'
    Select-String -LiteralPath $playerLog -Pattern 'HELP_.*(PASS|FAIL)' | ForEach-Object { $_.Line }
    if ($p.ExitCode -ne 0 -or -not (Select-String -LiteralPath $playerLog -Pattern 'HELP_SMOKE_PASS' -Quiet)) {
        throw "Help player smoke failed; see $playerLog and captured images"
    }
}
