#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Player = (Join-Path $PSScriptRoot '../../Build/PvZUnity.exe'),
    [ValidateSet('FirstUser', 'UI', 'LevelOne', 'Battle', 'Adventure')]
    [string[]]$Mode = @('FirstUser', 'UI', 'LevelOne', 'Battle', 'Adventure'),
    [string]$OutputRoot = (Join-Path $PSScriptRoot '../../Build/SmokeTests'),
    [switch]$LogicOnly,
    [ValidateRange(10, 600)][int]$TimeoutSeconds = 120,
    [ValidateRange(800, 3840)][int]$Width = 800,
    [ValidateRange(600, 2160)][int]$Height = 600
)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'This runner requires the Windows Player.' }
if ($LogicOnly -and ('Battle' -in $Mode -or 'Adventure' -in $Mode)) {
    throw 'Battle and Adventure have no logic-only switch. Use -Mode FirstUser,UI,LevelOne -LogicOnly.'
}
$playerPath = (Resolve-Path -LiteralPath $Player).ProviderPath
$playerDirectory = [IO.Path]::GetDirectoryName($playerPath)
$assemblyPath = Join-Path $playerDirectory 'PvZUnity_Data/Managed/Assembly-CSharp.dll'
if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
    throw "Player assembly missing: $assemblyPath"
}
Add-Type -AssemblyName System.Drawing.Common

$session = [DateTimeOffset]::Now.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$runDirectory = Join-Path ([IO.Path]::GetFullPath($OutputRoot)) $session
New-Item -ItemType Directory -Path $runDirectory | Out-Null
$results = [Collections.Generic.List[object]]::new()
$report = [ordered]@{
    StartedAt = [DateTimeOffset]::Now.ToString('o')
    FinishedAt = $null
    Player = $playerPath
    PlayerSha256 = (Get-FileHash -LiteralPath $playerPath -Algorithm SHA256).Hash
    AssemblySha256 = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash
    LogicOnly = [bool]$LogicOnly
    Width = $Width
    Height = $Height
    Graphics = if ($LogicOnly) { 'Null; no rendering checked' } else { 'Direct3D 11; visible window' }
    NativeVisualComparison = $false
    Passed = $false
    Results = $results
}

function Read-Screenshot([string]$Path) {
    $bitmap = [Drawing.Bitmap]::new($Path)
    try {
        if ($bitmap.Width -ne $Width -or $bitmap.Height -ne $Height) {
            throw "Screenshot dimensions differ from requested viewport: $Path ($($bitmap.Width)x$($bitmap.Height))."
        }
        $visible = 0
        $samples = 0
        for ($y = 5; $y -lt $bitmap.Height; $y += 10) {
            for ($x = 5; $x -lt $bitmap.Width; $x += 10) {
                $pixel = $bitmap.GetPixel($x, $y)
                if ([int]$pixel.R + [int]$pixel.G + [int]$pixel.B -gt 24) { $visible++ }
                $samples++
            }
        }
        if ($visible -lt $samples * 0.01) { throw "Screenshot is blank or nearly black: $Path" }
        [pscustomobject]@{
            Name = [IO.Path]::GetFileName($Path)
            Sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
            Width = $bitmap.Width
            Height = $bitmap.Height
            VisibleSampleFraction = $visible / [double]$samples
        }
    }
    finally { $bitmap.Dispose() }
}

try {
    foreach ($testMode in $Mode) {
        $stem = switch ($testMode) {
            'FirstUser' { 'first-user' }
            'UI' { 'ui' }
            'LevelOne' { 'level-one' }
            'Battle' { '' }
            'Adventure' { 'adventure' }
        }
        $flag = if ($testMode -eq 'Battle') { '-pvz-smoke' }
            elseif ($LogicOnly) { "-pvz-$stem-logic-smoke" } else { "-pvz-$stem-smoke" }
        $marker = if ($testMode -eq 'Battle') { 'PVZ_SMOKE_PASS' }
            else { 'PVZ_' + $stem.Replace('-', '_').ToUpperInvariant() + $(if ($LogicOnly) { '_LOGIC' }) + '_SMOKE_PASS' }
        $expectedScreenshots = if ($LogicOnly) { 0 } else {
            switch ($testMode) { 'FirstUser' { 1 } 'UI' { 21 } 'LevelOne' { 1 } 'Battle' { 3 } 'Adventure' { 22 } }
        }
        $testDirectory = Join-Path $runDirectory $testMode
        New-Item -ItemType Directory -Path $testDirectory | Out-Null
        $logPath = Join-Path $testDirectory 'player.log'
        $record = [ordered]@{
            Mode = $testMode; Flag = $flag; PassMarker = $marker
            StartedAt = [DateTimeOffset]::Now.ToString('o'); DurationSeconds = $null
            ExitCode = $null; Passed = $false; Error = $null; Screenshots = @()
        }
        $results.Add($record)
        $start = [Diagnostics.ProcessStartInfo]::new($playerPath)
        $start.WorkingDirectory = $playerDirectory
        $start.UseShellExecute = $false
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $start.WindowStyle = if ($LogicOnly) { [Diagnostics.ProcessWindowStyle]::Hidden } else { [Diagnostics.ProcessWindowStyle]::Normal }
        foreach ($argument in @($flag, '-pvz-smoke-output', $testDirectory, '-logFile', $logPath,
            '-screen-fullscreen', '0', '-screen-width', [string]$Width, '-screen-height', [string]$Height)) {
            $start.ArgumentList.Add($argument)
        }
        if ($LogicOnly) { $start.ArgumentList.Add('-batchmode'); $start.ArgumentList.Add('-nographics') }
        else { $start.ArgumentList.Add('-force-d3d11') }
        Write-Output "Running $testMode ($flag) -> $testDirectory"
        $timer = [Diagnostics.Stopwatch]::StartNew()
        $process = [Diagnostics.Process]::Start($start)
        $standardOutput = $process.StandardOutput.ReadToEndAsync()
        $standardError = $process.StandardError.ReadToEndAsync()
        try {
            while (-not $process.WaitForExit(500)) {
                if ($timer.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
                    # This Process instance belongs to this invocation; never stop an existing editor/player.
                    $process.Kill()
                    $process.WaitForExit()
                    throw "$testMode exceeded $TimeoutSeconds seconds."
                }
            }
            $record.ExitCode = $process.ExitCode
            if ($process.ExitCode -ne 0) { throw "$testMode exited with code $($process.ExitCode)." }
            $log = [IO.File]::ReadAllText($logPath)
            if (-not $log.Contains($marker + ':') -or $log -match 'PVZ_[A-Z_]*FAIL:') {
                throw "$testMode did not produce a clean $marker marker; inspect $logPath"
            }
            $screenshots = @(Get-ChildItem -LiteralPath $testDirectory -Filter '*.png' -File | Sort-Object Name)
            if ($screenshots.Count -ne $expectedScreenshots) {
                throw "$testMode wrote $($screenshots.Count) screenshots; expected $expectedScreenshots."
            }
            $record.Screenshots = @($screenshots | ForEach-Object { Read-Screenshot $_.FullName })
            $record.Passed = $true
            Write-Output "PASS: $testMode, $($screenshots.Count) screenshots, exit code 0."
        }
        catch { $record.Error = $_.Exception.Message; throw }
        finally {
            $record.DurationSeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 3)
            $standardOutput.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $testDirectory 'stdout.log') -Encoding utf8
            $standardError.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $testDirectory 'stderr.log') -Encoding utf8
            $process.Dispose()
        }
    }
    $report.Passed = $true
}
finally {
    $report.FinishedAt = [DateTimeOffset]::Now.ToString('o')
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $runDirectory 'results.json') -Encoding utf8
    Write-Output "Report: $(Join-Path $runDirectory 'results.json')"
}
