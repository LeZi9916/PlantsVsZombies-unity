#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$tool = Join-Path $PSScriptRoot '../../Tools/ImportOriginalAssets.ps1'
$root = Join-Path $PSScriptRoot ('artifacts/' + [Guid]::NewGuid().ToString('N'))
$source = Join-Path $root 'source'
$destination = Join-Path $root 'destination'
foreach ($folder in @('raw/images', 'raw/sounds', 'converted/reanim', 'converted/audio', 'executable/.rsrc/ICON')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $source $folder) | Out-Null
}
[IO.File]::WriteAllBytes((Join-Path $source 'raw/images/test.png'), [byte[]](1, 2, 3, 4))
[IO.File]::WriteAllBytes((Join-Path $source 'raw/sounds/test.ogg'), [byte[]](5, 6, 7))
[IO.File]::WriteAllText((Join-Path $source 'converted/reanim/test.json'), '{"track":1}')
[IO.File]::WriteAllText((Join-Path $source 'converted/audio/test.wav'), 'audio')
[IO.File]::WriteAllText((Join-Path $source 'executable/.rsrc/ICON/1.ico'), 'icon')
$rows = foreach ($relative in @('images/test.png', 'sounds/test.ogg')) {
    $file = Join-Path (Join-Path $source 'raw') $relative
    [pscustomobject]@{ extracted_path = 'raw/' + $relative; size_bytes = (Get-Item -LiteralPath $file).Length;
        sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $file).Hash }
}
$rows | Export-Csv -NoTypeInformation -LiteralPath (Join-Path $source 'pak_manifest.csv')
foreach ($name in @('audio_manifest.csv', 'image_manifest.csv', 'image_conversions.csv', 'definition_manifest.csv',
        'font_manifest.csv', 'icon_manifest.csv', 'verification.json', 'README.md')) {
    [IO.File]::WriteAllText((Join-Path $source $name), $name)
}

$checks = 0
function Check([bool]$condition, [string]$message) {
    $script:checks++
    if (-not $condition) { throw "FAILED: $message" }
}
$result = & $tool -Source $source -Destination $destination
Check ($result -match '14 files' -and $result -match 'copied=14') 'all source groups are copied'
foreach ($relative in @('Raw/images/test.png', 'Raw/sounds/test.ogg', 'Converted/reanim/test.json',
        'Converted/audio/test.wav', 'ExecutableResources/ICON/1.ico', 'Manifests/pak_manifest.csv')) {
    Check (Test-Path -LiteralPath (Join-Path $destination $relative) -PathType Leaf) "imported $relative"
}
$result = & $tool -Source $source -Destination $destination
Check ($result -match 'copied=0') 'repeat import is idempotent'
$result = & $tool -Source $source -Destination $destination -VerifyOnly
Check ($result -match 'copied=0') 'verify-only checks without copying'
$extra = Join-Path $destination 'unmanaged.txt'
[IO.File]::WriteAllText($extra, 'preserve')
& $tool -Source $source -Destination $destination | Out-Null
Check ([IO.File]::ReadAllText($extra) -eq 'preserve') 'unrelated destination file is preserved'

$conflict = Join-Path $destination 'Raw/images/test.png'
$missing = Join-Path $destination 'Raw/sounds/test.ogg'
[IO.File]::WriteAllText($conflict, 'changed')
Remove-Item -LiteralPath $missing
$failed = $false
try { & $tool -Source $source -Destination $destination | Out-Null } catch { $failed = $_.Exception.Message -match 'refusing to overwrite' }
Check $failed 'different destination file is rejected'
Check (-not (Test-Path -LiteralPath $missing)) 'conflict preflight runs before copying missing files'
Check ([IO.File]::ReadAllText($conflict) -eq 'changed') 'conflicting destination remains untouched'

$failed = $false
try { & $tool -Source $source -Destination (Join-Path $source 'nested') | Out-Null } catch { $failed = $true }
Check $failed 'overlapping source and destination are rejected'
[IO.File]::WriteAllText((Join-Path $source 'raw/images/test.png'), 'source modified')
$failed = $false
try { & $tool -Source $source -Destination $destination | Out-Null } catch { $failed = $_.Exception.Message -match 'Raw (size|SHA-256) mismatch' }
Check $failed 'source corruption is checked against archive manifest'

Write-Output "PASS: $checks asset import checks."
