#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Source = (Join-Path $PSScriptRoot '..\..\extracted_assets'),
    [string]$Destination = (Join-Path $PSScriptRoot '..\Assets\OriginalPvZ'),
    [switch]$VerifyOnly
)

$ErrorActionPreference = 'Stop'
$sourceRoot = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Source).ProviderPath).TrimEnd([IO.Path]::DirectorySeparatorChar)
$destinationRoot = [IO.Path]::GetFullPath($Destination, (Get-Location).ProviderPath).TrimEnd([IO.Path]::DirectorySeparatorChar)
$separator = [IO.Path]::DirectorySeparatorChar
$comparison = [StringComparison]::OrdinalIgnoreCase
if ($sourceRoot.Equals($destinationRoot, $comparison) -or
    $destinationRoot.StartsWith($sourceRoot + $separator, $comparison) -or
    $sourceRoot.StartsWith($destinationRoot + $separator, $comparison)) {
    throw 'Source and destination must be separate directory trees.'
}
if (Test-Path -LiteralPath $destinationRoot -PathType Leaf) { throw "Destination is a file: $destinationRoot" }

$rawRoot = Join-Path $sourceRoot 'raw'
$convertedRoot = Join-Path $sourceRoot 'converted'
$executableRoot = Join-Path $sourceRoot 'executable/.rsrc'
$manifestPath = Join-Path $sourceRoot 'pak_manifest.csv'
foreach ($required in @($rawRoot, $convertedRoot, $executableRoot)) {
    if (-not (Test-Path -LiteralPath $required -PathType Container)) { throw "Missing source directory: $required" }
}
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "Missing archive manifest: $manifestPath" }

$rawManifest = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($entry in @(Import-Csv -LiteralPath $manifestPath)) {
    $path = [string]$entry.extracted_path
    if (-not $path.StartsWith('raw/', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Invalid raw manifest path: $path"
    }
    $relative = $path.Substring(4).Replace('/', $separator).Replace('\', $separator)
    if ([IO.Path]::IsPathRooted($relative) -or $relative.Split($separator) -contains '..' -or
        $entry.sha256 -notmatch '^[0-9a-fA-F]{64}$' -or $entry.size_bytes -notmatch '^\d+$') {
        throw "Invalid raw manifest entry: $path"
    }
    if (-not $rawManifest.TryAdd($relative, $entry)) { throw "Duplicate raw manifest entry: $path" }
}
if ($rawManifest.Count -eq 0) { throw 'The raw archive manifest is empty.' }

$plan = [Collections.Generic.List[object]]::new()
$destinations = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
function Add-FilePlan([string]$From, [string]$To, [string]$Group, [object]$Expected) {
    if (-not $destinations.Add($To)) { throw "Two inputs map to the same destination: $To" }
    $item = Get-Item -LiteralPath $From
    if ($Expected -and $item.Length -ne [long]$Expected.size_bytes) { throw "Raw size mismatch: $From" }
    $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $From).Hash
    if ($Expected -and -not $sourceHash.Equals([string]$Expected.sha256, $comparison)) {
        throw "Raw SHA-256 mismatch: $From"
    }
    if (Test-Path -LiteralPath $To) {
        if (-not (Test-Path -LiteralPath $To -PathType Leaf)) { throw "Destination is not a file: $To" }
        if (-not (Get-FileHash -Algorithm SHA256 -LiteralPath $To).Hash.Equals($sourceHash, $comparison)) {
            throw "Destination differs; refusing to overwrite: $To"
        }
    }
    elseif ($VerifyOnly) { throw "Imported file is missing: $To" }
    $plan.Add([pscustomobject]@{ Source = $From; Destination = $To; Group = $Group; Hash = $sourceHash })
}

$rawFiles = @(Get-ChildItem -LiteralPath $rawRoot -File -Recurse -Force)
if ($rawFiles.Count -ne $rawManifest.Count) {
    throw "Raw file count $($rawFiles.Count) differs from manifest count $($rawManifest.Count)."
}
foreach ($file in $rawFiles) {
    $relative = [IO.Path]::GetRelativePath($rawRoot, $file.FullName)
    if (-not $rawManifest.ContainsKey($relative)) { throw "Raw file not listed in manifest: $relative" }
    Add-FilePlan $file.FullName (Join-Path (Join-Path $destinationRoot 'Raw') $relative) 'Raw' $rawManifest[$relative]
}
foreach ($tree in @(@($convertedRoot, 'Converted'), @($executableRoot, 'ExecutableResources'))) {
    $base = [string]$tree[0]
    $group = [string]$tree[1]
    foreach ($file in @(Get-ChildItem -LiteralPath $base -File -Recurse -Force)) {
        $relative = [IO.Path]::GetRelativePath($base, $file.FullName)
        Add-FilePlan $file.FullName (Join-Path (Join-Path $destinationRoot $group) $relative) $group $null
    }
}
foreach ($name in @('pak_manifest.csv', 'audio_manifest.csv', 'image_manifest.csv', 'image_conversions.csv',
        'definition_manifest.csv', 'font_manifest.csv', 'icon_manifest.csv', 'verification.json', 'README.md')) {
    $file = Join-Path $sourceRoot $name
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing source inventory: $file" }
    Add-FilePlan $file (Join-Path (Join-Path $destinationRoot 'Manifests') $name) 'Manifests' $null
}

$copied = 0
foreach ($item in $plan) {
    if (Test-Path -LiteralPath $item.Destination -PathType Leaf) { continue }
    New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($item.Destination)) | Out-Null
    Copy-Item -LiteralPath $item.Source -Destination $item.Destination
    if (-not (Get-FileHash -Algorithm SHA256 -LiteralPath $item.Destination).Hash.Equals($item.Hash, $comparison)) {
        throw "Copied file failed SHA-256 verification: $($item.Destination)"
    }
    $copied++
}
$counts = $plan | Group-Object Group | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Count)" }
Write-Output "Original PvZ import verified: $($plan.Count) files ($($counts -join ', ')); copied=$copied; destination=$destinationRoot"
