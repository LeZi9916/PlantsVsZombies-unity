#Requires -Version 7.0
<#
.SYNOPSIS
Runs the main project's Unity test assemblies in an isolated source snapshot.
.DESCRIPTION
Requires the exact editor from ProjectVersion.txt and a real graphics device.
Stores project caches, logs, NUnit XML, and summaries in an ignored run directory.
#>
[CmdletBinding()]
param(
    [string]$Unity = 'C:\Program Files\Unity Editors\6000.3.17f1\Editor\Unity.exe',
    [ValidateSet('EditMode', 'PlayMode', 'All')]
    [string]$Platform = 'All',
    [string]$AssemblyNames,
    [string]$TestFilter,
    [string]$PackageCache,
    [string]$ReuseProject,
    [ValidateRange(60, 1800)]
    [int]$TimeoutSeconds = 600
)

$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$versionText = Get-Content -LiteralPath (Join-Path $repository 'ProjectSettings/ProjectVersion.txt') -Raw
$requiredVersion = [regex]::Match($versionText, '(?m)^m_EditorVersion:\s*(\S+)').Groups[1].Value
if (-not (Test-Path -LiteralPath $Unity -PathType Leaf)) {
    throw "Unity $requiredVersion was not found: $Unity"
}

$installedVersion = (Get-Item -LiteralPath $Unity).VersionInfo.ProductVersion
if (-not $installedVersion.StartsWith($requiredVersion + '_', [StringComparison]::Ordinal) -and $installedVersion -ne $requiredVersion) {
    throw "Expected Unity $requiredVersion; selected editor reports $installedVersion."
}

$runId = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$runDirectory = Join-Path $repository ('.unity-runs/' + $runId)
$project = Join-Path $runDirectory 'p'
if ($ReuseProject) {
    $project = (Resolve-Path -LiteralPath $ReuseProject).Path
    $allowedRoot = [IO.Path]::GetFullPath((Join-Path $repository '.unity-runs')) + [IO.Path]::DirectorySeparatorChar
    if (-not $project.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($project) -ne 'p') {
        throw 'Only a previously created .unity-runs/<run>/p project can be reused.'
    }
    $runDirectory = [IO.Path]::GetDirectoryName($project)
}
$projectAssets = Join-Path $project 'Assets'
if ($ReuseProject -and (Test-Path -LiteralPath $projectAssets)) {
    $resolvedAssets = (Resolve-Path -LiteralPath $projectAssets).Path
    if (-not $resolvedAssets.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The reusable asset snapshot resolved outside .unity-runs.'
    }
    Remove-Item -LiteralPath $resolvedAssets -Recurse -Force
}
New-Item -ItemType Directory -Path $projectAssets -Force | Out-Null
foreach ($directory in @('Packages', 'ProjectSettings')) {
    Copy-Item -LiteralPath (Join-Path $repository $directory) -Destination $project -Recurse -Force
}

foreach ($asset in Get-ChildItem -LiteralPath (Join-Path $repository 'Assets') -Force) {
    if ($asset.Name -in @('StreamingAssets', 'StreamingAssets.meta', 'OriginalPvZ', 'OriginalPvZ.meta')) {
        continue
    }
    Copy-Item -LiteralPath $asset.FullName -Destination $projectAssets -Recurse
}

if ($PackageCache) {
    $cacheSource = (Resolve-Path -LiteralPath $PackageCache).Path
    $cacheTarget = Join-Path $project 'Library/PackageCache'
    New-Item -ItemType Directory -Path $cacheTarget -Force | Out-Null
    foreach ($package in Get-ChildItem -LiteralPath $cacheSource -Directory) {
        if (-not $package.Name.StartsWith('.')) {
            Copy-Item -LiteralPath $package.FullName -Destination $cacheTarget -Recurse
        }
    }
}

$platforms = if ($Platform -eq 'All') { @('EditMode', 'PlayMode') } else { @($Platform) }
$summary = [Collections.Generic.List[object]]::new()
Write-Output "Isolated Unity project: $project"
foreach ($testPlatform in $platforms) {
    $result = Join-Path $runDirectory ($testPlatform + '.xml')
    $log = Join-Path $runDirectory ($testPlatform + '.log')
    if (Test-Path -LiteralPath $result) {
        Remove-Item -LiteralPath $result
    }
    $start = [Diagnostics.ProcessStartInfo]::new($Unity)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    foreach ($argument in @('-batchmode', '-projectPath', $project, '-runTests', '-testPlatform', $testPlatform,
            '-testResults', $result, '-logFile', $log, '-pvz-test-runner')) {
        $start.ArgumentList.Add($argument)
    }
    if ($AssemblyNames) {
        $start.ArgumentList.Add('-assemblyNames')
        $start.ArgumentList.Add($AssemblyNames)
    }
    if ($TestFilter) {
        $start.ArgumentList.Add('-testFilter')
        $start.ArgumentList.Add($TestFilter)
    }

    $process = [Diagnostics.Process]::Start($start)
    $timer = [Diagnostics.Stopwatch]::StartNew()
    try {
        while (-not $process.WaitForExit(1000)) {
            if ($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds) {
                $process.Kill($true)
                throw "Unity $testPlatform timed out; stopped only the test process created by this script. See $log"
            }
        }
        if ($process.ExitCode -ne 0) {
            throw "Unity $testPlatform exited with $($process.ExitCode). See $log"
        }
    }
    finally {
        $process.Dispose()
    }

    if (-not (Test-Path -LiteralPath $result -PathType Leaf)) {
        throw "Unity $testPlatform did not produce NUnit results. See $log"
    }
    [xml]$xml = Get-Content -LiteralPath $result -Raw
    $testRun = $xml.'test-run'
    if ($null -eq $testRun -or [int]$testRun.passed -le 0 -or [int]$testRun.failed -ne 0 -or $testRun.result -ne 'Passed') {
        throw "Unity $testPlatform tests did not pass: $result"
    }
    $summary.Add([pscustomobject]@{
        Platform = $testPlatform
        Passed = [int]$testRun.passed
        Failed = [int]$testRun.failed
        Skipped = [int]$testRun.skipped
        DurationSeconds = $testRun.duration
        Results = $result
        Log = $log
    })
    Write-Output ("PASS {0}: {1} tests" -f $testPlatform, $testRun.passed)
}
$summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $runDirectory 'summary.json') -Encoding utf8
Write-Output "Results: $runDirectory"
