[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Reference,
    [Parameter(Mandatory = $true)][string]$Candidate,
    [string]$DifferenceImage,
    [ValidateRange(1, 64)][int]$DifferenceAmplification = 4,
    [switch]$RequireIdentical
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing.Common

if (-not ('PvZUiImageComparison' -as [type])) {
    $drawingReferences = @([System.Drawing.Bitmap].Assembly.Location, [System.Drawing.Color].Assembly.Location)
    foreach ($name in @('System.Private.Windows.GdiPlus', 'System.Private.Windows.Core')) {
        $assembly = [AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq $name } | Select-Object -First 1
        if ($assembly) { $drawingReferences += $assembly.Location }
    }
    Add-Type -ReferencedAssemblies $drawingReferences -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;

public sealed class PvZUiImageComparisonResult
{
    public int Width { get; set; }
    public int Height { get; set; }
    public long PixelCount { get; set; }
    public long DifferentPixels { get; set; }
    public double DifferentPixelPercent { get; set; }
    public int MaxChannelDifference { get; set; }
    public double MeanAbsoluteChannelDifference { get; set; }
    public double RootMeanSquareChannelDifference { get; set; }
    public int[] DifferenceBounds { get; set; }
    public bool Identical { get; set; }
}

public static class PvZUiImageComparison
{
    public static PvZUiImageComparisonResult Compare(string reference, string candidate,
        string differenceImage, int amplification)
    {
        using (var expected = new Bitmap(reference))
        using (var actual = new Bitmap(candidate))
        {
            if (expected.Width != actual.Width || expected.Height != actual.Height)
                throw new ArgumentException("Image dimensions differ; compare matching native viewports.");

            int width = expected.Width, height = expected.Height;
            long different = 0, sum = 0, sumSquares = 0;
            int max = 0, minX = width, minY = height, maxX = -1, maxY = -1;
            using (var difference = String.IsNullOrEmpty(differenceImage) ? null : new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color a = expected.GetPixel(x, y), b = actual.GetPixel(x, y);
                        int dr = Math.Abs(a.R - b.R), dg = Math.Abs(a.G - b.G);
                        int db = Math.Abs(a.B - b.B), da = Math.Abs(a.A - b.A);
                        if ((dr | dg | db | da) != 0)
                        {
                            different++;
                            minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                            maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                        }
                        max = Math.Max(max, Math.Max(Math.Max(dr, dg), Math.Max(db, da)));
                        sum += dr + dg + db + da;
                        sumSquares += dr * dr + dg * dg + db * db + da * da;
                        if (difference != null)
                            difference.SetPixel(x, y, Color.FromArgb(255,
                                Math.Min(255, Math.Max(dr, da) * amplification),
                                Math.Min(255, Math.Max(dg, da) * amplification),
                                Math.Min(255, Math.Max(db, da) * amplification)));
                    }
                }
                if (difference != null) difference.Save(differenceImage, ImageFormat.Png);
            }

            long pixels = (long)width * height;
            double channels = pixels * 4.0;
            return new PvZUiImageComparisonResult {
                Width = width, Height = height, PixelCount = pixels, DifferentPixels = different,
                DifferentPixelPercent = different * 100.0 / pixels,
                MaxChannelDifference = max, MeanAbsoluteChannelDifference = sum / channels,
                RootMeanSquareChannelDifference = Math.Sqrt(sumSquares / channels),
                DifferenceBounds = different == 0 ? null : new[] { minX, minY, maxX - minX + 1, maxY - minY + 1 },
                Identical = different == 0
            };
        }
    }
}
'@
}

$referencePath = (Resolve-Path -LiteralPath $Reference).ProviderPath
$candidatePath = (Resolve-Path -LiteralPath $Candidate).ProviderPath
$differencePath = $null
if ($DifferenceImage) {
    $differencePath = [IO.Path]::GetFullPath($DifferenceImage, (Get-Location).ProviderPath)
    if (Test-Path -LiteralPath $differencePath) {
        throw "Difference image already exists: $differencePath"
    }
    if (-not (Test-Path -LiteralPath ([IO.Path]::GetDirectoryName($differencePath)) -PathType Container)) {
        throw "Difference image parent directory does not exist: $differencePath"
    }
}

$result = [PvZUiImageComparison]::Compare($referencePath, $candidatePath, $differencePath, $DifferenceAmplification)
[pscustomobject]@{
    Reference = $referencePath
    Candidate = $candidatePath
    Width = $result.Width
    Height = $result.Height
    PixelCount = $result.PixelCount
    DifferentPixels = $result.DifferentPixels
    DifferentPixelPercent = $result.DifferentPixelPercent
    MaxChannelDifference = $result.MaxChannelDifference
    MeanAbsoluteChannelDifference = $result.MeanAbsoluteChannelDifference
    RootMeanSquareChannelDifference = $result.RootMeanSquareChannelDifference
    DifferenceBoundsXYWH = $result.DifferenceBounds
    Identical = $result.Identical
    DifferenceImage = $differencePath
    DifferenceAmplification = if ($differencePath) { $DifferenceAmplification } else { $null }
} | ConvertTo-Json -Depth 4

if ($RequireIdentical -and -not $result.Identical) {
    throw 'Decoded RGBA pixels differ.'
}
