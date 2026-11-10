param(
    [string]$Unity = 'C:\Program Files\Unity Editors\6000.3.17f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Unity -PathType Leaf)) { throw "Unity editor not found: $Unity" }
$runtime = Join-Path $PSScriptRoot '..\..\Assets\Scripts\UI\BitmapFonts'
$resources = Join-Path $runtime 'Resources\PvZBitmapFonts'
if (-not (Test-Path -LiteralPath $resources -PathType Container)) {
    throw 'Run PvZUnity/Tools/PrepareBitmapFonts.ps1 first.'
}
$id = [Guid]::NewGuid().ToString('N')
$project = Join-Path $PSScriptRoot ('.unity-runs\' + $id)
$output = Join-Path $PSScriptRoot ('artifacts\' + $id)
New-Item -ItemType Directory -Force -Path $project, $output | Out-Null
Copy-Item -Path (Join-Path $PSScriptRoot 'UnitySmokeProject\*') -Destination $project -Recurse
$codeTarget = Join-Path $project 'Assets\BitmapFonts'
New-Item -ItemType Directory -Force -Path $codeTarget | Out-Null
Get-ChildItem -LiteralPath $runtime -File -Filter '*.cs' | Copy-Item -Destination $codeTarget
$resourceTarget = Join-Path $project 'Assets\Resources\PvZBitmapFonts'
foreach ($directory in @('Descriptions', 'Atlases')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $resourceTarget $directory) | Out-Null
}
foreach ($name in @('DwarvenTodcraft12', 'DwarvenTodcraft15', 'DwarvenTodcraft18')) {
    Copy-Item -LiteralPath (Join-Path $resources "Descriptions\${name}__Main.bytes") `
        -Destination (Join-Path $resourceTarget 'Descriptions')
    Copy-Item -LiteralPath (Join-Path $resources "Atlases\${name}.bytes") `
        -Destination (Join-Path $resourceTarget 'Atlases')
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

Write-Host "Building isolated Unity project: $project"
Invoke-CheckedProcess $Unity @('-batchmode', '-nographics', '-quit', '-projectPath', $project,
    '-executeMethod', 'BitmapFontSmokeBuild.Build', '-logFile', (Join-Path $output 'editor.log')) 300
Write-Host 'Running the graphics-enabled player and comparing its screenshot with the source atlases.'
Invoke-CheckedProcess (Join-Path $project 'Build\BitmapFontSmoke.exe') @(
    '-bitmap-font-output', $output, '-logFile', (Join-Path $output 'player.log')) 120
$result = Join-Path $output 'result.txt'
if (-not (Test-Path -LiteralPath $result -PathType Leaf)) { throw "No smoke result: $output" }
$content = Get-Content -LiteralPath $result -Raw
if (-not $content.StartsWith('PASS')) { throw "Smoke test failed: $content" }
Write-Host $content
Write-Host "Artifacts: $output"
