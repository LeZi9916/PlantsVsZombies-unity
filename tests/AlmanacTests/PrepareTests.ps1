$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot '..\..\Tools\PrepareAlmanacAssets.ps1'
$target = Join-Path $PSScriptRoot 'ConflictFixture'
$image = Join-Path $target 'images\Almanac_IndexBack.jpg'
[void](New-Item -ItemType Directory -Force -Path (Split-Path -Parent $image))
[IO.File]::WriteAllText($image, 'user-owned conflict')
$failed = $false
try { & $script -Destination $target } catch { $failed = $_.Exception.Message -match 'nothing copied or overwritten' }
if (-not $failed) { throw 'The copy did not reject an existing different file.' }
if ([IO.File]::ReadAllText($image) -ne 'user-owned conflict') { throw 'The existing file was overwritten.' }
if (@(Get-ChildItem -LiteralPath $target -File -Recurse).Count -ne 1) { throw 'A preflight conflict copied additional files.' }
Write-Host 'Asset conflict preflight passed.'

$source = Join-Path $PSScriptRoot '../../../extracted_assets/raw'
$prepared = Join-Path $PSScriptRoot 'PreparedAssets'
foreach ($relative in @('images/packet_plants.png', 'reanim/Zombie_Boss_driver.reanim',
    'reanim/Zombie_boss_upperbody_.png', 'reanim/Zombie_boss_neck_.png')) {
    $original = Join-Path $source $relative
    $copy = Join-Path $prepared $relative
    if (-not (Test-Path -LiteralPath $copy -PathType Leaf)) { throw "Almanac render dependency is missing: $relative" }
    if ((Get-FileHash -LiteralPath $original).Hash -ne (Get-FileHash -LiteralPath $copy).Hash) {
        throw "Almanac render dependency differs from its original: $relative"
    }
}
Write-Host 'PASS: original packet atlas, boss driver and both JPG alpha masks are prepared exactly.'
