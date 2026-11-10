param(
    [string]$Source = (Join-Path $PSScriptRoot '..\..\extracted_assets\converted\fonts'),
    [string]$Destination = (Join-Path $PSScriptRoot '..\Assets\Scripts\UI\BitmapFonts\Resources\PvZBitmapFonts')
)

$ErrorActionPreference = 'Stop'
$Source = (Resolve-Path -LiteralPath $Source).Path
$descriptions = @(Get-ChildItem -LiteralPath $Source -File -Filter '*.fnt' | Sort-Object Name)
$atlases = @(Get-ChildItem -LiteralPath $Source -File -Filter '*.png' | Sort-Object Name)
if ($descriptions.Count -eq 0 -or $atlases.Count -eq 0) {
    throw "No converted font descriptions or PNG atlases found in $Source"
}

$copies = [System.Collections.Generic.List[object]]::new()
foreach ($file in $descriptions) {
    $copies.Add([PSCustomObject]@{
        From = $file.FullName
        To = (Join-Path (Join-Path $Destination 'Descriptions') ($file.BaseName + '.bytes'))
    })
}
foreach ($file in $atlases) {
    $copies.Add([PSCustomObject]@{
        From = $file.FullName
        To = (Join-Path (Join-Path $Destination 'Atlases') ($file.BaseName + '.bytes'))
    })
}

$seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($copy in $copies) {
    if (-not $seen.Add($copy.To)) { throw "Duplicate resource destination: $($copy.To)" }
    if (Test-Path -LiteralPath $copy.To -PathType Leaf) {
        if ((Get-FileHash -LiteralPath $copy.From -Algorithm SHA256).Hash -ne
            (Get-FileHash -LiteralPath $copy.To -Algorithm SHA256).Hash) {
            throw "Destination differs; refusing to overwrite: $($copy.To)"
        }
    }
}
foreach ($copy in $copies) {
    if (-not (Test-Path -LiteralPath $copy.To -PathType Leaf)) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $copy.To) | Out-Null
        Copy-Item -LiteralPath $copy.From -Destination $copy.To
    }
}
Write-Host "Prepared $($descriptions.Count) BMFont layers and $($atlases.Count) PNG atlases at $Destination"
