#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Unity = 'C:\Program Files\Unity Editors\6000.3.17f1\Editor\Unity.exe',
    [switch]$SkipBuild,
    [ValidateRange(30, 600)][int]$TimeoutSeconds = 180
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'The isolated almanac Player requires Windows.' }
Add-Type -AssemblyName System.Drawing.Common
$project = Join-Path $PSScriptRoot 'UnityProject'
$scripts = Join-Path $project 'Assets/Scripts'
$assetRoot = Join-Path $PSScriptRoot 'PreparedAssets'
$runName = 'visible-' + [DateTimeOffset]::Now.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$output = Join-Path $PSScriptRoot ('UnityOutput/' + $runName)
New-Item -ItemType Directory -Path $output | Out-Null
$sourceNames = @('Reanimation/ReanimationRenderer.cs', 'VerifiedData/VerifiedDataCatalog.cs',
    'VerifiedData/VerifiedData.generated.cs', 'UI/AlmanacView.cs', 'UI/AlmanacPortraitLayout.cs')

function Wait-OwnedProcess([Diagnostics.Process]$Process, [string]$Label) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while (-not $Process.WaitForExit(500)) {
        if ($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds) {
            $Process.Kill()
            $Process.WaitForExit()
            throw "$Label timed out; stopped only the process created by this script."
        }
    }
    if ($Process.ExitCode -ne 0) { throw "$Label exited with code $($Process.ExitCode)." }
}

$report = [ordered]@{
    StartedAt = [DateTimeOffset]::Now.ToString('o'); FinishedAt = $null
    Passed = $false; NativeVisualComparison = $false
    SourceHashes = @(); AssemblySha256 = $null; Screenshots = @(); FrozenIndexPixelsUnchanged = $false; PortraitRasterPassed = $false; Error = $null
}
try {
    if (-not $SkipBuild) {
        foreach ($name in $sourceNames) {
            Copy-Item -LiteralPath (Join-Path $PSScriptRoot "../../Assets/Scripts/$name") -Destination $scripts
        }
        $shaderRoot = Join-Path $project 'Assets/Resources/Shaders'
        New-Item -ItemType Directory -Path $shaderRoot -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../../Assets/Resources/Shaders/PvZNativePortrait.shader') -Destination $shaderRoot
        $editorLog = Join-Path $output 'editor.log'
        $editor = Start-Process -FilePath $Unity -ArgumentList @('-batchmode', '-nographics', '-quit',
            '-projectPath', ('"' + $project + '"'), '-executeMethod', 'AlmanacBuild.Build',
            '-logFile', ('"' + $editorLog + '"')) -WindowStyle Hidden -PassThru
        try { Wait-OwnedProcess $editor 'Isolated almanac build' } finally { $editor.Dispose() }
    }
    $report.SourceHashes = @($sourceNames | ForEach-Object {
        $path = Join-Path $scripts ([IO.Path]::GetFileName($_))
        [pscustomobject]@{ Name = $_; Sha256 = (Get-FileHash -LiteralPath $path).Hash }
    })
    $player = Join-Path $project 'SmokeBuild/AlmanacSmoke.exe'
    $assembly = Join-Path $project 'SmokeBuild/AlmanacSmoke_Data/Managed/Assembly-CSharp.dll'
    $report.AssemblySha256 = (Get-FileHash -LiteralPath $assembly).Hash
    $start = [Diagnostics.ProcessStartInfo]::new($player)
    $start.UseShellExecute = $false
    $start.WorkingDirectory = [IO.Path]::GetDirectoryName($player)
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Environment['PVZ_ALMANAC_ROOT'] = $assetRoot
    $start.Environment['PVZ_ALMANAC_OUTPUT'] = $output
    foreach ($arg in @('-force-d3d11', '-screen-width', '800', '-screen-height', '600',
        '-screen-fullscreen', '0', '-logFile', (Join-Path $output 'player.log'))) { $start.ArgumentList.Add($arg) }
    Write-Output "Running visible isolated almanac -> $output"
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    try { Wait-OwnedProcess $process 'Isolated almanac Player' }
    finally {
        $stdout.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $output 'stdout.log') -Encoding utf8
        $stderr.GetAwaiter().GetResult() | Set-Content -LiteralPath (Join-Path $output 'stderr.log') -Encoding utf8
        $process.Dispose()
    }
    if ((Get-Content -LiteralPath (Join-Path $output 'result.txt') -Raw).Trim() -ne 'PASS') {
        throw "Almanac rendering checks failed: $output/result.txt"
    }
    $diagnostics = Get-Content -LiteralPath (Join-Path $output 'diagnostics.txt') -Raw
    if (-not [string]::IsNullOrWhiteSpace($diagnostics)) { throw "Almanac diagnostics: $diagnostics" }
    $shots = @(Get-ChildItem -LiteralPath $output -Filter '*.png' -File | Sort-Object Name)
    if ($shots.Count -ne 62) { throw "Expected 62 almanac screenshots (including both poses of all 26 zombies), got $($shots.Count)." }
    $report.Screenshots = @($shots | ForEach-Object {
        $image = [Drawing.Bitmap]::new($_.FullName)
        try {
            if ($image.Width -ne 800 -or $image.Height -ne 600) { throw "Invalid almanac viewport: $($_.Name)" }
            [pscustomobject]@{ Name = $_.Name; Width = $image.Width; Height = $image.Height; Sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
        }
        finally { $image.Dispose() }
    })
    $normalIndex = [Drawing.Bitmap]::new((Join-Path $output 'zombie.png'))
    $bossIndex = [Drawing.Bitmap]::new((Join-Path $output 'boss.png'))
    try {
        for ($y = 80; $y -lt 565; $y++) {
            for ($x = 0; $x -lt 444; $x++) {
                # The newly hovered boss frame is the sole expected index change.
                if ($x -ge 192 -and $x -lt 268 -and $y -ge 486 -and $y -lt 562) { continue }
                if ($normalIndex.GetPixel($x, $y).ToArgb() -ne $bossIndex.GetPixel($x, $y).ToArgb()) {
                    throw "The frozen index changed outside the selected frame at ($x,$y); check native clipping."
                }
            }
        }
        $report.FrozenIndexPixelsUnchanged = $true
    }
    finally { $normalIndex.Dispose(); $bossIndex.Dispose() }
    & python (Join-Path $PSScriptRoot "verify_portraits.py") $assetRoot $output
    if ($LASTEXITCODE -ne 0) { throw "Independent portrait raster comparison failed; see portrait-raster-results.json." }
    $report.PortraitRasterPassed = $true
    $report.Passed = $true
    Write-Output 'PASS: all 75 selections, rendering, disposal, 62 nonblank screenshots, exit code 0.'
}
catch { $report.Error = $_.Exception.Message; throw }
finally {
    $report.FinishedAt = [DateTimeOffset]::Now.ToString('o')
    $report | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $output 'results.json') -Encoding utf8
    Write-Output "Report: $(Join-Path $output 'results.json')"
}
