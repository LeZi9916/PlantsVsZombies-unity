#Requires -Version 7.0
<#
.SYNOPSIS
Builds and measures a complete replay Player from an isolated source snapshot.
.DESCRIPTION
Requires Unity 7000.0.0a16 and an ASCII working path. Original checkpoints and
the source project remain read only. All generated assets, settings and Player
output stay in the dedicated project copy. No process uses -nographics.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SourceProject,
    [Parameter(Mandatory)][string]$Checkpoint,
    [Parameter(Mandatory)][string]$WorkingRoot,
    [string]$Label = 'candidate',
    [string]$Unity = 'C:\Program Files\Unity Editors\7000.0.0a16\Editor\Unity.exe',
    [ValidateSet('CoreCLR', 'Mono', 'IL2CPP')][string]$Backend = 'CoreCLR',
    [ValidateSet('Vulkan', 'D3D11')][string]$Graphics = 'Vulkan',
    [int]$AdvanceTicks = 6000,
    [int]$Frames = 900,
    [ValidateRange(120, 100000)][int]$WarmupFrames = 120,
    [int]$DurationSeconds = 0,
    [switch]$Frozen,
    [switch]$NativeProfiler,
    [int]$BuildTimeoutSeconds = 1800,
    [int]$RunTimeoutSeconds = 900
)

$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
$source = (Resolve-Path -LiteralPath $SourceProject).Path
$save = (Resolve-Path -LiteralPath $Checkpoint).Path
$work = [IO.Path]::GetFullPath($WorkingRoot)
if ($work -match '[^\x00-\x7F]') {
    throw 'WorkingRoot must be ASCII because Unity 7 can misread Unicode command-line values.'
}
if ($Label -notmatch '^[a-zA-Z0-9_-]+$') {
    throw 'Label may contain ASCII letters, digits, underscores and hyphens only.'
}
$project = Join-Path $work ($Label + '/p')
if (Test-Path -LiteralPath $project) {
    throw "The replay project already exists; select a fresh WorkingRoot or Label: $project"
}
$version = Get-Content -LiteralPath (Join-Path $source 'ProjectSettings/ProjectVersion.txt') -Raw
if ($version -notmatch '(?m)^m_EditorVersion:\s*7000\.0\.0a16\s*$') {
    throw 'The selected source must use Unity 7000.0.0a16.'
}
$installed = (Get-Item -LiteralPath $Unity).VersionInfo.ProductVersion
if (-not $installed.StartsWith('7000.0.0a16', [StringComparison]::Ordinal)) {
    throw "Expected Unity 7000.0.0a16; selected editor reports $installed."
}
$run = Join-Path $work $Label
New-Item -ItemType Directory -Path (Join-Path $project 'Assets') -Force | Out-Null
foreach ($asset in Get-ChildItem -LiteralPath (Join-Path $source 'Assets') -Force) {
    if ($asset.Name -notin @('Tests', 'Tests.meta', 'OriginalPvZ', 'OriginalPvZ.meta', 'StreamingAssets', 'StreamingAssets.meta', 'Generated', 'Generated.meta')) {
        Copy-Item -LiteralPath $asset.FullName -Destination (Join-Path $project 'Assets') -Recurse
    }
}
foreach ($name in @('Packages', 'ProjectSettings')) {
    Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $project $name) -Recurse
}
foreach ($name in @('StreamingAssets', 'Generated')) {
    Copy-Item -LiteralPath (Join-Path $repository ('Assets/' + $name)) -Destination (Join-Path $project ('Assets/' + $name)) -Recurse
    $meta = Join-Path $repository ('Assets/' + $name + '.meta')
    if (Test-Path -LiteralPath $meta) {
        Copy-Item -LiteralPath $meta -Destination (Join-Path $project ('Assets/' + $name + '.meta'))
    }
}
New-Item -ItemType Directory -Path (Join-Path $project 'Assets/Scripts/Diagnostics') -Force | Out-Null
foreach ($diagnostic in Get-ChildItem -LiteralPath (Join-Path $repository 'Assets/Scripts/Diagnostics') -Force) {
    Copy-Item -LiteralPath $diagnostic.FullName -Destination (Join-Path $project 'Assets/Scripts/Diagnostics') -Recurse -Force
}
Copy-Item -LiteralPath (Join-Path $repository 'Assets/Scripts/Diagnostics.meta') -Destination (Join-Path $project 'Assets/Scripts/Diagnostics.meta') -Force
foreach ($name in @('PerformanceReplayBuild.cs', 'PerformanceReplayBuild.cs.meta')) {
    Copy-Item -LiteralPath (Join-Path $repository ('Assets/Editor/' + $name)) -Destination (Join-Path $project ('Assets/Editor/' + $name)) -Force
}
$cache = Join-Path $repository 'Library/PackageCache'
if (Test-Path -LiteralPath $cache) {
    New-Item -ItemType Directory -Path (Join-Path $project 'Library') -Force | Out-Null
    Copy-Item -LiteralPath $cache -Destination (Join-Path $project 'Library/PackageCache') -Recurse
}
'Isolated replay project; settings and generated assets are disposable.' | Set-Content -LiteralPath (Join-Path $project '.pvz-replay-isolated') -Encoding utf8
$copy = Join-Path $run 'source.dat'
$sourceHash = (Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash
Copy-Item -LiteralPath $save -Destination $copy
$player = Join-Path $run 'build/Replay.exe'
$buildLog = Join-Path $run 'build.log'

function Invoke-ReplayProcess {
    param([string]$Executable, [string[]]$Arguments, [int]$TimeoutSeconds)
    $start = [Diagnostics.ProcessStartInfo]::new($Executable)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    foreach ($argument in $Arguments) {
        $start.ArgumentList.Add($argument)
    }
    $process = [Diagnostics.Process]::Start($start)
    $watch = [Diagnostics.Stopwatch]::StartNew()
    try {
        while (-not $process.WaitForExit(1000)) {
            if ($watch.Elapsed.TotalSeconds -gt $TimeoutSeconds) {
                $process.Kill($true)
                throw "The isolated replay process timed out: $Executable"
            }
        }
        if ($process.ExitCode -ne 0) {
            throw "The isolated replay process exited with code $($process.ExitCode): $Executable"
        }
    }
    finally {
        $process.Dispose()
    }
}

Invoke-ReplayProcess $Unity @('-batchmode', '-quit', '-projectPath', $project, '-buildTarget', 'Win64',
    '-executeMethod', 'PlantsVsZombies.Editor.PerformanceReplayBuild.BuildFromCommandLine', '-pvz-replay-isolated',
    '-pvz-replay-build-output', $player, '-pvz-replay-backend', $Backend, '-pvz-replay-graphics', $Graphics,
    '-logFile', $buildLog) $BuildTimeoutSeconds
$output = Join-Path $run 'measurement'
$playerArguments = @('-pvz-performance-replay', '-pvz-replay-save', $copy, '-pvz-replay-output', $output,
    '-pvz-replay-label', $Label, '-pvz-replay-advance-ticks', $AdvanceTicks.ToString(), '-pvz-replay-frames', $Frames.ToString(),
    '-pvz-replay-warmup-frames', $WarmupFrames.ToString(),
    '-pvz-replay-duration', $DurationSeconds.ToString(), '-screen-width', '1920', '-screen-height', '1080',
    '-screen-fullscreen', '0', '-logFile', (Join-Path $run 'player.log'))
if ($Frozen) {
    $playerArguments += '-pvz-replay-freeze'
}
if ($NativeProfiler) {
    $playerArguments += '-pvz-replay-native-profiler'
}
Invoke-ReplayProcess $player $playerArguments $RunTimeoutSeconds
$report = Get-Content -LiteralPath (Join-Path $output 'replay.json') -Raw | ConvertFrom-Json
if ($report.Status -ne 'complete' -or -not $report.SourceUnchanged -or -not $report.AllocationPositiveControl -or -not $report.ByteCounterPositiveControl) {
    throw "The Player did not produce valid complete replay evidence: $output"
}
if ($DurationSeconds -gt 0 -and $report.RecordedSeconds -lt $DurationSeconds) {
    throw "The Player recorded only $($report.RecordedSeconds) seconds of the requested $DurationSeconds seconds."
}
$expectedBackend = if ($Backend -eq 'IL2CPP') { 'IL2CPP' } else { 'CoreCLR' }
if ($report.ActualScriptingBackend -ne $expectedBackend) {
    throw "The built replay used $($report.ActualScriptingBackend), expected $expectedBackend."
}
if ((Get-FileHash -LiteralPath $save -Algorithm SHA256).Hash -ne $sourceHash) {
    throw 'The original source checkpoint changed during the replay run.'
}
Write-Output "Replay evidence: $output"
