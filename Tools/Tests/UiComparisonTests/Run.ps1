$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing.Common
$tool = Join-Path $PSScriptRoot '../../Tools/CompareUiScreenshots.ps1'
$directory = Join-Path $PSScriptRoot ('artifacts/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $directory | Out-Null
$reference = Join-Path $directory 'reference.png'
$candidate = Join-Path $directory 'candidate.png'
$differentSize = Join-Path $directory 'different-size.png'
$difference = Join-Path $directory 'difference.png'

$checks = 0
function Assert-Check([bool]$condition, [string]$message) {
    $script:checks++
    if (-not $condition) { throw "FAILED: $message" }
}
function Assert-Near([double]$actual, [double]$expected, [string]$message) {
    Assert-Check ([Math]::Abs($actual - $expected) -lt 0.000001) $message
}

$image = [Drawing.Bitmap]::new(3, 2)
try {
    for ($y = 0; $y -lt 2; $y++) {
        for ($x = 0; $x -lt 3; $x++) {
            $image.SetPixel($x, $y, [Drawing.Color]::FromArgb(255, 10, 20, 30))
        }
    }
    $image.Save($reference, [Drawing.Imaging.ImageFormat]::Png)
    $image.SetPixel(1, 0, [Drawing.Color]::FromArgb(255, 13, 15, 30))
    $image.SetPixel(2, 1, [Drawing.Color]::FromArgb(128, 10, 20, 30))
    $image.Save($candidate, [Drawing.Imaging.ImageFormat]::Png)
} finally { $image.Dispose() }
$image = [Drawing.Bitmap]::new(1, 1)
try { $image.Save($differentSize, [Drawing.Imaging.ImageFormat]::Png) } finally { $image.Dispose() }

$same = & $tool -Reference $reference -Candidate $reference | ConvertFrom-Json
Assert-Check $same.Identical 'same image is identical'
Assert-Check ($same.DifferentPixels -eq 0) 'same image has no differing pixels'
Assert-Check ($null -eq $same.DifferenceBoundsXYWH) 'same image has no bounds'
Assert-Near $same.MeanAbsoluteChannelDifference 0 'same image has zero error'

$changed = & $tool -Reference $reference -Candidate $candidate -DifferenceImage $difference | ConvertFrom-Json
Assert-Check (-not $changed.Identical) 'modified pixels are detected'
Assert-Check ($changed.Width -eq 3 -and $changed.Height -eq 2 -and $changed.PixelCount -eq 6) 'image dimensions'
Assert-Check ($changed.DifferentPixels -eq 2) 'two changed pixels'
Assert-Near $changed.DifferentPixelPercent (200.0 / 6.0) 'changed pixel percentage'
Assert-Check ($changed.MaxChannelDifference -eq 127) 'largest channel difference includes alpha'
Assert-Near $changed.MeanAbsoluteChannelDifference (135.0 / 24.0) 'RGBA mean absolute error'
Assert-Near $changed.RootMeanSquareChannelDifference ([Math]::Sqrt(16163.0 / 24.0)) 'RGBA root mean square error'
Assert-Check (($changed.DifferenceBoundsXYWH -join ',') -eq '1,0,2,2') 'difference bounds'
Assert-Check ((Test-Path -LiteralPath $difference) -and $changed.DifferenceAmplification -eq 4) 'difference image saved'
$image = [Drawing.Bitmap]::new($difference)
try {
    $pixel = $image.GetPixel(1, 0)
    Assert-Check ($pixel.R -eq 12 -and $pixel.G -eq 20 -and $pixel.B -eq 0) 'difference image amplifies RGB errors'
    $pixel = $image.GetPixel(2, 1)
    Assert-Check ($pixel.R -eq 255 -and $pixel.G -eq 255 -and $pixel.B -eq 255) 'difference image shows alpha errors'
} finally { $image.Dispose() }

$failed = $false
try { & $tool -Reference $reference -Candidate $differentSize | Out-Null } catch { $failed = $true }
Assert-Check $failed 'dimension mismatch fails'
$failed = $false
try { & $tool -Reference $reference -Candidate $candidate -DifferenceImage $difference | Out-Null } catch { $failed = $true }
Assert-Check $failed 'existing difference output is not overwritten'
$failed = $false
try { & $tool -Reference $reference -Candidate $candidate -RequireIdentical | Out-Null } catch { $failed = $true }
Assert-Check $failed 'require-identical fails for changed pixels'
& $tool -Reference $reference -Candidate $reference -RequireIdentical | Out-Null
Assert-Check $true 'require-identical accepts matching pixels'

Write-Output "PASS: $checks screenshot comparison checks."
