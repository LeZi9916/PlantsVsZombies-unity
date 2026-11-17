#Requires -Version 7.0
<#
.SYNOPSIS
Runs ordered AB/BA/AB checkpoint trials against two isolated Unity source snapshots.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$BaselineProject,
    [Parameter(Mandatory)] [string]$CandidateProject,
    [Parameter(Mandatory)] [string]$Checkpoint,
    [Parameter(Mandatory)] [string]$Output,
    [string]$Unity = 'C:\Program Files\Unity Editors\7000.0.0a16\Editor\Unity.exe',
    [ValidateSet('Vulkan', 'D3D11')] [string]$Graphics = 'Vulkan',
    [ValidateSet('cleared', 'battle', 'frozen')] [string[]]$Scenarios = @('cleared', 'battle', 'frozen'),
    [ValidateRange(1, 3)] [int]$Trials = 3,
    [ValidateRange(1, 18000)] [int]$Frames = 900,
    [ValidateRange(60, 1800)] [int]$TimeoutSeconds = 900,
    [switch]$ProfileAllocations
)

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$baseline = (Resolve-Path -LiteralPath $BaselineProject).Path
$candidate = (Resolve-Path -LiteralPath $CandidateProject).Path
$source = (Resolve-Path -LiteralPath $Checkpoint).Path
$outputRoot = [IO.Path]::GetFullPath($Output)
if ($baseline -eq $candidate -or $baseline -eq $repository -or $candidate -eq $repository) {
    throw 'Baseline and candidate must be distinct isolated projects, not the main checkout.'
}
$version = [regex]::Match((Get-Content -LiteralPath (Join-Path $repository 'ProjectSettings/ProjectVersion.txt') -Raw), '(?m)^m_EditorVersion:\s*(\S+)').Groups[1].Value
$installed = (Get-Item -LiteralPath $Unity).VersionInfo.ProductVersion
if ($installed -ne $version -and -not $installed.StartsWith($version + '_', [StringComparison]::Ordinal)) {
    throw "Expected Unity $version; selected Editor reports $installed."
}
$fixture = Join-Path $repository 'Assets/Tests/PlayMode/CheckpointRenderingBenchmarkTests.cs'
foreach ($project in @($baseline, $candidate)) {
    $projectVersion = Get-Content -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Raw
    if (-not $projectVersion.Contains('m_EditorVersion: ' + $version)) {
        throw "The isolated project does not use $version`: $project"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $project 'Assets/StreamingAssets/images/pool.jpg'))) {
        throw "Complete physical StreamingAssets are required: $project"
    }
    Copy-Item -LiteralPath $fixture -Destination (Join-Path $project 'Assets/Tests/PlayMode/CheckpointRenderingBenchmarkTests.cs') -Force
    Copy-Item -LiteralPath ($fixture + '.meta') -Destination (Join-Path $project 'Assets/Tests/PlayMode/CheckpointRenderingBenchmarkTests.cs.meta') -Force
}
$sourceHash = (Get-FileHash -LiteralPath $source).Hash
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$manifest = [ordered]@{ SourceSha256 = $sourceHash; UnityVersion = $version; Graphics = $Graphics; Frames = $Frames; WarmupFrames = 120; Trials = $Trials; Snapshots = [ordered]@{} }
foreach ($label in @('baseline', 'candidate')) {
    $project = if ($label -eq 'baseline') { $baseline } else { $candidate }
    $files = @(& rg --files (Join-Path $project 'Assets/Scripts') (Join-Path $project 'Assets/Resources/Shaders'))
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inventory the isolated source snapshot: $project"
    }
    $entries = foreach ($file in $files | Sort-Object) {
        [pscustomobject]@{ Path = [IO.Path]::GetRelativePath($project, $file); Sha256 = (Get-FileHash -LiteralPath $file).Hash }
    }
    $manifest.Snapshots[$label] = $entries
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputRoot 'source-manifest.json') -Encoding utf8
$results = [Collections.Generic.List[object]]::new()
foreach ($scenario in $Scenarios) {
    foreach ($trial in 1..$Trials) {
        $order = if ($trial -eq 2) { @('candidate', 'baseline') } else { @('baseline', 'candidate') }
        foreach ($label in $order) {
            $project = if ($label -eq 'baseline') { $baseline } else { $candidate }
            $destination = Join-Path $outputRoot "$scenario/$trial/$label"
            if (Test-Path -LiteralPath (Join-Path $destination 'checkpoint.json')) {
                throw "Refusing to overwrite an existing accepted run: $destination"
            }
            New-Item -ItemType Directory -Path $destination -Force | Out-Null
            $temporary = Join-Path ([IO.Path]::GetTempPath()) ('pvz-checkpoint-' + [Guid]::NewGuid().ToString('N'))
            New-Item -ItemType Directory -Path $temporary -Force | Out-Null
            $xmlPath = Join-Path $temporary 'results.xml'
            $logPath = Join-Path $temporary 'editor.log'
            $start = [Diagnostics.ProcessStartInfo]::new($Unity)
            $start.UseShellExecute = $false
            $start.CreateNoWindow = $true
            $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
            $start.WorkingDirectory = $project
            foreach ($argument in @('-batchmode', '-projectPath', $project, '-runTests', '-testPlatform', 'PlayMode',
                    '-testFilter', 'RecordsExternalCheckpoint', '-testResults', $xmlPath, '-logFile', $logPath, '-pvz-test-runner')) {
                $start.ArgumentList.Add($argument)
            }
            $start.ArgumentList.Add($(if ($Graphics -eq 'Vulkan') { '-force-vulkan' } else { '-force-d3d11' }))
            $start.Environment['PVZ_RENDER_BENCHMARK_CHECKPOINT'] = $source
            $start.Environment['PVZ_RENDER_BENCHMARK_OUTPUT'] = $destination
            $start.Environment['PVZ_BOARD_STRESS_ASSETS'] = Join-Path $project 'Assets/StreamingAssets'
            $start.Environment['PVZ_RENDER_USE_BAKED'] = '1'
            $start.Environment['PVZ_RENDER_BACKEND'] = 'Auto'
            $start.Environment['PVZ_RENDER_BENCHMARK_FRAMES'] = [string]$Frames
            $start.Environment['PVZ_RENDER_BENCHMARK_NEXT_STAGE'] = if ($scenario -eq 'cleared') { '0' } else { '1' }
            $start.Environment['PVZ_RENDER_BENCHMARK_TICKS'] = if ($scenario -eq 'battle') { '6000' } elseif ($scenario -eq 'frozen') { '12000' } else { '0' }
            $start.Environment['PVZ_RENDER_BENCHMARK_ADVANCE'] = if ($scenario -eq 'battle') { '1' } else { '0' }
            $start.Environment['PVZ_RENDER_PROFILE_ALLOCATIONS'] = if ($ProfileAllocations) { '1' } else { '0' }
            Write-Output "START $scenario trial $trial $label"
            @{ Scenario = $scenario; Trial = $trial; Label = $label; Status = 'running' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputRoot 'status.json') -Encoding utf8
            $process = [Diagnostics.Process]::Start($start)
            $timer = [Diagnostics.Stopwatch]::StartNew()
            try {
                while (-not $process.WaitForExit(1000)) {
                    if ($timer.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
                        $process.Kill($true)
                        throw "The owned benchmark process timed out. See $logPath"
                    }
                }
                $exitCode = $process.ExitCode
            }
            finally {
                $process.Dispose()
            }
            if (Test-Path -LiteralPath $xmlPath) {
                Copy-Item -LiteralPath $xmlPath -Destination (Join-Path $destination 'results.xml')
            }
            if (Test-Path -LiteralPath $logPath) {
                Copy-Item -LiteralPath $logPath -Destination (Join-Path $destination 'editor.log')
            }
            if ($exitCode -ne 0 -or -not (Test-Path -LiteralPath $xmlPath)) {
                throw "Benchmark process failed with exit $exitCode. See $destination"
            }
            [xml]$xml = Get-Content -LiteralPath $xmlPath -Raw
            if ([int]$xml.'test-run'.failed -ne 0 -or [int]$xml.'test-run'.passed -ne 1) {
                throw "The benchmark did not produce exactly one passing result: $destination"
            }
            if ((Get-FileHash -LiteralPath $source).Hash -ne $sourceHash) {
                throw 'The read-only source checkpoint changed during playback.'
            }
            $results.Add([pscustomobject]@{ Scenario = $scenario; Trial = $trial; Label = $label; Seconds = $timer.Elapsed.TotalSeconds; Output = $destination })
            $results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $outputRoot 'runs.json') -Encoding utf8
            Write-Output "PASS $scenario trial $trial $label"
        }
    }
}
@{ Status = 'complete'; SourceSha256 = $sourceHash; Runs = $results.Count } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputRoot 'status.json') -Encoding utf8
