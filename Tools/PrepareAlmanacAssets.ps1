param(
    [string]$Source = (Join-Path $PSScriptRoot '..\..\extracted_assets\raw'),
    [string]$Destination = (Join-Path $PSScriptRoot '..\Assets\StreamingAssets\PvZ')
)

$ErrorActionPreference = 'Stop'
$Source = (Resolve-Path -LiteralPath $Source).Path
$Destination = [IO.Path]::GetFullPath($Destination)
$catalogPath = Join-Path $PSScriptRoot '..\Assets\Scripts\VerifiedData\VerifiedData.generated.cs'
$catalog = [IO.File]::ReadAllText($catalogPath)
$plantRows = [regex]::Matches($catalog, 'new PlantDefinition\((\d+),\s*"[^"]+",\s*-?\d+,\s*-?\d+,\s*(\d+),')
$zombieRows = [regex]::Matches($catalog, 'new ZombieDefinition\((\d+),\s*"[^"]+",\s*(\d+),')
$animationRows = [regex]::Matches($catalog, 'new ReanimationDefinition\((\d+),\s*"([^"]+)"')
if ($plantRows.Count -lt 49 -or $zombieRows.Count -lt 26 -or $animationRows.Count -eq 0) {
    throw 'Could not read the expected original IDs from VerifiedData.generated.cs.'
}
$animationPaths = @{}
foreach ($row in $animationRows) {
    $animationPaths[[int]$row.Groups[1].Value] = $row.Groups[2].Value.Replace('\\', '\')
}
$animationIds = [Collections.Generic.HashSet[int]]::new()
foreach ($row in $plantRows) {
    if ([int]$row.Groups[1].Value -lt 49) { [void]$animationIds.Add([int]$row.Groups[2].Value) }
}
foreach ($row in $zombieRows) {
    if ([int]$row.Groups[1].Value -lt 26) { [void]$animationIds.Add([int]$row.Groups[2].Value) }
}
# DrawZombie(Flag) attaches this separate original animation.
[void]$animationIds.Add(140)
# Native boss portrait/cache attaches the separate driver reanimation.
[void]$animationIds.Add(93)
$plan = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::OrdinalIgnoreCase)
$missing = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

function Add-File([string]$relative, [bool]$required = $true) {
    $relative = $relative.Replace('/', [IO.Path]::DirectorySeparatorChar).Replace('\', [IO.Path]::DirectorySeparatorChar)
    if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|[\\/])\.\.([\\/]|$)') {
        throw "Unsafe resource path: $relative"
    }
    $path = Join-Path $Source $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        if ($required) { [void]$missing.Add($relative) }
        return $false
    }
    if (-not $plan.ContainsKey($relative)) { $plan.Add($relative, $path) }
    return $true
}

[void](Add-File 'properties\LawnStrings.txt')
foreach ($name in @('Almanac_IndexBack.jpg', 'Almanac_PlantBack.jpg', 'Almanac_ZombieBack.jpg',
    'Almanac_PlantCard.png', 'Almanac_ZombieCard.png', 'Almanac_Imitater.png',
    'Almanac_ZombieBlank.png', 'Almanac_ZombieWindow.png', 'Almanac_ZombieWindow2.png',
    'Almanac_GroundDay.jpg', 'Almanac_GroundNight.jpg', 'Almanac_GroundPool.jpg',
    'Almanac_GroundNightPool.jpg', 'Almanac_GroundRoof.jpg', 'Almanac_GroundIce.jpg',
    'Almanac_CloseButton.png', 'Almanac_CloseButtonHighlight.png', 'Almanac_IndexButton.png',
    'Almanac_IndexButtonHighlight.png', 'seeds.png', 'packet_plants.png', 'SeedChooser_Button.png',
    'SeedChooser_Button_Glow.png', 'button_left.png', 'button_middle.png', 'button_right.png')) {
    [void](Add-File "images\$name")
}

# ReanimationRenderer resolves IMAGE_REANIM_* and IMAGE_* directly by basename.
$imagePaths = @{}
foreach ($directory in @('images', 'reanim')) {
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Source $directory) -File) {
        if ($file.Extension -notin @('.png', '.jpg', '.jpeg')) { continue }
        $key = $directory + '/' + $file.BaseName
        if (-not $imagePaths.ContainsKey($key) -or $file.Extension -ieq '.png') {
            $imagePaths[$key] = Join-Path $directory $file.Name
        }
    }
}
foreach ($id in ($animationIds | Sort-Object)) {
    if (-not $animationPaths.ContainsKey($id)) { throw "Missing reanimation catalog ID: $id" }
    $relative = $animationPaths[$id]
    if (-not (Add-File $relative)) { continue }
    $fragment = [IO.File]::ReadAllText((Join-Path $Source $relative))
    $settings = [Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $document = [Xml.XmlDocument]::new()
    $document.XmlResolver = $null
    $reader = [Xml.XmlReader]::Create([IO.StringReader]::new('<root>' + $fragment + '</root>'), $settings)
    try { $document.Load($reader) } finally { $reader.Dispose() }
    foreach ($node in $document.SelectNodes('//i')) {
        $imageId = $node.InnerText.Trim()
        if (-not $imageId) { continue }
        if ($imageId.StartsWith('IMAGE_REANIM_', [StringComparison]::OrdinalIgnoreCase)) {
            $key = 'reanim/' + $imageId.Substring(13)
        } elseif ($imageId.StartsWith('IMAGE_', [StringComparison]::OrdinalIgnoreCase)) {
            $key = 'images/' + $imageId.Substring(6)
        } else {
            $key = 'images/' + $imageId
        }
        if ($imagePaths.ContainsKey($key)) {
            $relativeImage = $imagePaths[$key]
            [void](Add-File $relativeImage)
            # PopCap JPG color layers carry a separate grayscale PNG alpha mask.
            if ([IO.Path]::GetExtension($relativeImage) -ieq '.jpg') {
                $mask = Join-Path ([IO.Path]::GetDirectoryName($relativeImage)) ([IO.Path]::GetFileNameWithoutExtension($relativeImage) + '_.png')
                [void](Add-File $mask $false)
            }
        }
        else { [void]$missing.Add("$imageId (referenced by $relative)") }
    }
}
if ($missing.Count -gt 0) {
    throw "Missing original almanac resources; nothing copied:`n$(($missing | Sort-Object) -join "`n")"
}

# Preflight the entire copy so a conflict cannot be hidden by -Force or partial copying.
$conflicts = [Collections.Generic.List[string]]::new()
foreach ($entry in $plan.GetEnumerator()) {
    $target = Join-Path $Destination $entry.Key
    if (Test-Path -LiteralPath $target -PathType Container) { $conflicts.Add($entry.Key); continue }
    if (Test-Path -LiteralPath $target -PathType Leaf) {
        if ((Get-FileHash -Algorithm SHA256 -LiteralPath $entry.Value).Hash -ne
            (Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash) { $conflicts.Add($entry.Key) }
    }
}
if ($conflicts.Count -gt 0) {
    throw "Existing files differ from the originals; nothing copied or overwritten:`n$(($conflicts | Sort-Object) -join "`n")"
}
$copied = 0
$unchanged = 0
foreach ($entry in $plan.GetEnumerator()) {
    $target = Join-Path $Destination $entry.Key
    if (Test-Path -LiteralPath $target -PathType Leaf) { $unchanged++; continue }
    [void](New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target))
    [IO.File]::Copy($entry.Value, $target, $false)
    $copied++
}
Write-Host "Almanac originals: $copied copied, $unchanged already identical, $($animationIds.Count) animation IDs."
