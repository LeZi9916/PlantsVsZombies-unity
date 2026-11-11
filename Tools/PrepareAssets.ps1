param(
    [string]$Source = (Join-Path $PSScriptRoot '..\..\extracted_assets\raw'),
    [string]$Destination = (Join-Path $PSScriptRoot '..\Assets\StreamingAssets\PvZ')
)

$ErrorActionPreference = 'Stop'
$Source = (Resolve-Path -LiteralPath $Source).Path
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$Destination = (Resolve-Path -LiteralPath $Destination).Path
$copied = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$missing = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

function Copy-Asset([string]$relative) {
    $relative = $relative.Replace('/', [IO.Path]::DirectorySeparatorChar).Replace('\', [IO.Path]::DirectorySeparatorChar)
    if ($copied.Contains($relative)) { return $true }
    $from = Join-Path $Source $relative
    if (-not (Test-Path -LiteralPath $from -PathType Leaf)) { return $false }
    $to = Join-Path $Destination $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $to) | Out-Null
    if (Test-Path -LiteralPath $to -PathType Leaf) {
        if ((Get-FileHash -LiteralPath $from -Algorithm SHA256).Hash -ne
            (Get-FileHash -LiteralPath $to -Algorithm SHA256).Hash) {
            throw "Destination differs from original; refusing to overwrite: $to"
        }
    }
    else { Copy-Item -LiteralPath $from -Destination $to }
    [void]$copied.Add($relative)
    return $true
}

[xml]$resources = [IO.File]::ReadAllText((Join-Path $Source 'properties\resources.xml'))
$catalog = @{}
foreach ($group in $resources.SelectNodes('//Resources')) {
    $path = ''
    $prefix = ''
    foreach ($element in $group.ChildNodes) {
        if ($element.Name -eq 'SetDefaults') {
            if ($element.HasAttribute('path')) { $path = $element.GetAttribute('path') }
            if ($element.HasAttribute('idprefix')) { $prefix = $element.GetAttribute('idprefix') }
        }
        elseif ($element.Name -eq 'Image') {
            $id = $prefix + $element.GetAttribute('id')
            $catalog[$id] = Join-Path $path $element.GetAttribute('path')
        }
    }
}

$animations = @('PeaShooterSingle', 'PeaShooter', 'SunFlower', 'Wallnut', 'Tallnut', 'CherryBomb', 'Jalapeno', 'fire', 'SnowPea', 'PotatoMine', 'Zombie', 'Zombie_flagpole', 'Zombie_surprise', 'Sun', 'LawnMower')
foreach ($name in $animations) {
    $relative = "reanim\$name.reanim"
    if (-not (Copy-Asset $relative)) { throw "Missing animation: $relative" }
    $text = [IO.File]::ReadAllText((Join-Path $Source $relative))
    [xml]$animation = '<root>' + $text + '</root>'
    foreach ($image in $animation.SelectNodes('//i')) {
        $id = $image.InnerText.Trim()
        if (-not $id) { continue }
        $candidates = [System.Collections.Generic.List[string]]::new()
        if ($catalog.ContainsKey($id)) { $candidates.Add($catalog[$id]) }
        if ($id.StartsWith('IMAGE_REANIM_', [StringComparison]::OrdinalIgnoreCase)) {
            $candidates.Add('reanim\' + $id.Substring(13))
        }
        elseif ($id.StartsWith('IMAGE_', [StringComparison]::OrdinalIgnoreCase)) {
            $candidates.Add('images\' + $id.Substring(6))
        }
        $found = $false
        foreach ($candidate in $candidates) {
            foreach ($extension in @('.png', '.jpg', '.gif')) {
                if (Copy-Asset ($candidate + $extension)) { $found = $true; break }
            }
            if ($found) { break }
        }
        if (-not $found) { [void]$missing.Add($id) }
    }
}

foreach ($name in @('background1.jpg', 'background1unsodded.jpg', 'SeedBank.png', 'seeds.png', 'ShovelBank.png',
    'Shovel.png', 'FlagMeter.png', 'FlagMeterParts.png', 'SeedChooser_Background.png', 'SeedChooser_Button.png',
    'SeedChooser_Button_Disabled.png', 'Challenge_Background.jpg', 'Challenge_Window.png', 'Challenge_Thumbnails.jpg', 'Store_MainMenuButton.png',
    'plantshadow.png', 'ProjectilePea.png', 'ProjectileSnowPea.png')) {
    if (-not (Copy-Asset "images\$name")) { throw "Missing image: $name" }
}
foreach ($name in @('Wallnut_cracked1.png', 'Wallnut_cracked2.png', 'Tallnut_cracked1.png', 'Tallnut_cracked2.png', 'Zombie_paper_madhead.png')) {
    if (-not (Copy-Asset "reanim\$name")) { throw "Missing damaged plant image: $name" }
}
foreach ($name in @('coin_silver_dollar.png', 'coin_gold_dollar.png')) {
    if (-not (Copy-Asset "reanim\$name")) { throw "Missing original collection face: $name" }
}
foreach ($name in @('plant.ogg', 'throw.ogg', 'points.ogg', 'splat.ogg',
    'lawnmower.ogg', 'cherrybomb.ogg', 'potato_mine.ogg', 'jalapeno.ogg', 'winmusic.ogg', 'losemusic.ogg', 'newspaper_rarrgh.ogg', 'newspaper_rarrgh2.ogg', 'coin.ogg')) {
    if (-not (Copy-Asset "sounds\$name")) { throw "Missing sound: $name" }
}
[void](Copy-Asset 'properties\resources.xml')
[void](Copy-Asset 'properties\LawnStrings.txt')
$diamondSource = Join-Path $Source '..\converted\audio\diamond.wav'
$diamondDestination = Join-Path $Destination 'sounds\diamond.wav'
if (-not (Test-Path -LiteralPath $diamondSource -PathType Leaf)) { throw "Missing converted original diamond sound: $diamondSource" }
if (Test-Path -LiteralPath $diamondDestination -PathType Leaf) {
    if ((Get-FileHash -LiteralPath $diamondSource -Algorithm SHA256).Hash -ne
        (Get-FileHash -LiteralPath $diamondDestination -Algorithm SHA256).Hash) {
        throw "Destination differs from original diamond sound; refusing to overwrite: $diamondDestination"
    }
}
else { Copy-Item -LiteralPath $diamondSource -Destination $diamondDestination }
$sodSource = Join-Path $Source '..\converted\rgba\images\sod1row.png'
$sodDestination = Join-Path $Destination 'images\sod1row.png'
if (-not (Test-Path -LiteralPath $sodSource -PathType Leaf)) { throw "Missing converted lawn image: $sodSource" }
if (Test-Path -LiteralPath $sodDestination -PathType Leaf) {
    if ((Get-FileHash -LiteralPath $sodSource -Algorithm SHA256).Hash -ne
        (Get-FileHash -LiteralPath $sodDestination -Algorithm SHA256).Hash) {
        throw "Destination differs from converted image; refusing to overwrite: $sodDestination"
    }
}
else { Copy-Item -LiteralPath $sodSource -Destination $sodDestination }
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Source 'reanim') -File -Filter 'SelectorScreen*') {
    if ($file.Extension -in @('.png', '.jpg')) { [void](Copy-Asset ("reanim\" + $file.Name)) }
}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $Source 'images') -File -Filter 'SelectorScreen*') {
    if ($file.Extension -in @('.png', '.jpg')) { [void](Copy-Asset ("images\" + $file.Name)) }
}
foreach ($name in @('options_menuback.jpg', 'options_menuback_.png', 'options_sliderslot.png',
    'options_sliderknob2.png', 'options_checkbox0.png', 'options_checkbox1.png',
    'options_backtogamebutton0.png', 'options_backtogamebutton2.png',
    'button_left.png', 'button_middle.png', 'button_right.png',
    'button_down_left.png', 'button_down_middle.png', 'button_down_right.png')) {
    if (-not (Copy-Asset "images\$name")) { throw "Missing options image: $name" }
}
foreach ($name in @('dialog_topleft.png', 'dialog_topmiddle.png', 'dialog_topright.png',
    'dialog_centerleft.png', 'dialog_centermiddle.png', 'dialog_centerright.png',
    'dialog_bigbottomleft.png', 'dialog_bigbottommiddle.png', 'dialog_bigbottomright.png', 'dialog_header.png')) {
    if (-not (Copy-Asset "images\$name")) { throw "Missing user dialog image: $name" }
}
foreach ($name in @('ZombieNote.jpg', 'ZombieNote_.png', 'ZombieNoteHelpBlack.png', 'ZombieNoteHelp.png', 'SeedChooser_Button_Glow.png')) {
    if (-not (Copy-Asset "images\$name")) { throw "Missing help image: $name" }
}
$renderedMusic = Join-Path $Source '..\converted\audio\mainmusic.wav'
$musicDestination = Join-Path $Destination 'sounds\mainmusic.ogg'
if ((Test-Path -LiteralPath $renderedMusic -PathType Leaf) -and -not (Test-Path -LiteralPath $musicDestination)) {
    $ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($ffmpeg) {
        & $ffmpeg.Source -nostdin -v error -n -i $renderedMusic -c:a libvorbis -q:a 4 $musicDestination
        if ($LASTEXITCODE -ne 0) { throw 'Could not convert the rendered music to OGG.' }
    }
    else { Write-Warning 'ffmpeg was not found; background music was not converted.' }
}
Write-Host "Prepared $($copied.Count) original asset files at $Destination (new or already identical)"
if ($missing.Count -gt 0) {
    Write-Warning "Unresolved animation image IDs ($($missing.Count)): $($missing -join ', ')"
}
