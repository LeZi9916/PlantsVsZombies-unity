param(
    [string]$Unity = 'C:\Program Files\Unity Editors\6000.3.17f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'UnityProject'
$scripts = Join-Path $PSScriptRoot '..\..\Assets\Scripts'
$assets = Join-Path $project 'Assets'
$log = Join-Path $PSScriptRoot 'unity-profile-tests.log'
$env:PVZ_PROFILE_RESOURCE_ROOT = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\extracted_assets\raw')).Path
Copy-Item -LiteralPath (Join-Path $scripts 'UI\ProfileDialog.cs') -Destination (Join-Path $assets 'ProfileDialog.cs') -Force
Copy-Item -LiteralPath (Join-Path $scripts 'Profiles\ProfileCatalog.cs') -Destination (Join-Path $assets 'ProfileCatalog.cs') -Force
Copy-Item -LiteralPath (Join-Path $scripts 'Profiles\UserProfile.cs') -Destination (Join-Path $assets 'UserProfile.cs') -Force
$process = Start-Process -FilePath $Unity -ArgumentList @(
    '-batchmode', '-nographics', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'ProfileDialogUnityTests.Run', '-logFile', ('"' + $log + '"')
) -Wait -PassThru -WindowStyle Hidden
Select-String -LiteralPath $log -Pattern 'PROFILE_UNITY_TESTS_(PASS|FAIL)'
if ($process.ExitCode -ne 0) { throw "Unity failed with exit code $($process.ExitCode); see $log" }
if (-not (Select-String -LiteralPath $log -Pattern 'PROFILE_UNITY_TESTS_PASS' -Quiet)) {
    throw "Unity exited without a pass marker; see $log"
}
