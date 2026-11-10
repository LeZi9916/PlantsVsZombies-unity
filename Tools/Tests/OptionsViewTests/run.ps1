param(
    [string]$Unity = 'C:\Program Files\Unity Editors\6000.3.17f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Unity -PathType Leaf)) { throw "Unity not found: $Unity" }
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$workspace = (Resolve-Path (Join-Path $projectRoot '..')).Path
$id = [Guid]::NewGuid().ToString('N')
$project = Join-Path $PSScriptRoot ('.unity-runs\' + $id)
$output = Join-Path $PSScriptRoot ('artifacts\' + $id)
New-Item -ItemType Directory -Force -Path $project, $output | Out-Null
Copy-Item -Path (Join-Path $PSScriptRoot 'UnityProject\*') -Destination $project -Recurse
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets\Scripts\UI\OptionsView.cs') -Destination (Join-Path $project 'Assets')
$runtime = Join-Path $projectRoot 'Assets\Scripts\UI\BitmapFonts'
$codeTarget = Join-Path $project 'Assets\BitmapFonts'
New-Item -ItemType Directory -Force -Path $codeTarget | Out-Null
Get-ChildItem -LiteralPath $runtime -File -Filter '*.cs' | Copy-Item -Destination $codeTarget
$sourceResources = Join-Path $runtime 'Resources\PvZBitmapFonts'
$targetResources = Join-Path $project 'Assets\Resources\PvZBitmapFonts'
foreach ($directory in @('Descriptions', 'Atlases')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $targetResources $directory) | Out-Null
}
foreach ($name in @('DwarvenTodcraft15', 'DwarvenTodcraft18', 'DwarvenTodcraft24',
                     'DwarvenTodcraft18GreenInset', 'DwarvenTodcraft18BrightGreenInset',
                     'DwarvenTodcraft36GreenInset', 'DwarvenTodcraft36BrightGreenInset')) {
    Copy-Item -LiteralPath (Join-Path $sourceResources "Descriptions\${name}__Main.bytes") -Destination (Join-Path $targetResources 'Descriptions')
    Copy-Item -LiteralPath (Join-Path $sourceResources "Atlases\${name}.bytes") -Destination (Join-Path $targetResources 'Atlases')
}

function Invoke-CheckedProcess([string]$Executable, [string[]]$Arguments, [int]$TimeoutSeconds) {
    $quoted = @($Arguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }) -join ' '
    $process = Start-Process -FilePath $Executable -ArgumentList $quoted -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill()
        throw "Process timed out: $Executable"
    }
    if ($process.ExitCode -ne 0) { throw "Process failed with exit code $($process.ExitCode): $Executable" }
}

$env:PVZ_OPTIONS_RESOURCE_ROOT = Join-Path $projectRoot 'Assets\StreamingAssets\PvZ'
$editorLog = Join-Path $output 'editor.log'
Write-Host "Running isolated edit tests and building player: $project"
Invoke-CheckedProcess $Unity @('-batchmode', '-nographics', '-quit', '-projectPath', $project,
    '-executeMethod', 'OptionsViewTests.RunAndBuild', '-logFile', $editorLog) 300
if (-not (Select-String -LiteralPath $editorLog -Pattern 'OPTIONS_VIEW_EDITOR_PASS' -Quiet)) {
    throw "No editor pass marker; see $editorLog"
}
Select-String -LiteralPath $editorLog -Pattern 'OPTIONS_VIEW_EDITOR_PASS'

$player = Join-Path $project 'Build\OptionsViewSmoke.exe'
foreach ($variant in @('streaming', 'original')) {
    $variantOutput = Join-Path $output $variant
    New-Item -ItemType Directory -Force -Path $variantOutput | Out-Null
    $root = if ($variant -eq 'streaming') { $env:PVZ_OPTIONS_RESOURCE_ROOT } else { Join-Path $workspace 'extracted_assets\raw' }
    Write-Host "Running graphics smoke: $variant"
    Invoke-CheckedProcess $player @('-screen-width', '800', '-screen-height', '600', '-screen-fullscreen', '0',
        '-options-root', $root, '-options-output', $variantOutput, '-logFile', (Join-Path $variantOutput 'player.log')) 120
    $result = Join-Path $variantOutput 'result.txt'
    if (-not (Test-Path -LiteralPath $result -PathType Leaf)) { throw "Missing result: $variantOutput" }
    $content = Get-Content -LiteralPath $result -Raw
    if (-not $content.StartsWith('PASS')) { throw "Graphics smoke failed: $content" }
    Write-Host $content
}
Add-Type -AssemblyName System.Drawing
$streamingImage = [System.Drawing.Bitmap]::new((Join-Path $output 'streaming\menu-pressed.png'))
$originalImage = [System.Drawing.Bitmap]::new((Join-Path $output 'original\menu-pressed.png'))
try {
    $different = 0
    for ($y = 335; $y -lt 381; $y++) {
        for ($x = 295; $x -lt 505; $x++) {
            if ($streamingImage.GetPixel($x, $y).ToArgb() -ne $originalImage.GetPixel($x, $y).ToArgb()) { $different++ }
        }
    }
    if ($different -ne 0) { throw "Prepared and original pressed button renderings differ: $different pixels" }
    Write-Host 'Prepared and original pressed button renderings match in the checked region'
} finally {
    $streamingImage.Dispose()
    $originalImage.Dispose()
}
Write-Host "Artifacts: $output"
