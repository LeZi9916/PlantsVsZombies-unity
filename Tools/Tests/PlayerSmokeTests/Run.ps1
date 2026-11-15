#Requires -Version 7.0
<#
.SYNOPSIS
Runs the migrated Player smoke scenarios with the Unity PlayMode Test Runner.
#>
[CmdletBinding()]
param(
    [string]$Unity,
    [ValidateSet('FirstUser', 'UI', 'LevelOne', 'Battle', 'Adventure', 'Feedback', 'Progression', 'IntroAudio', 'CombatFidelity')]
    [string[]]$Mode = @('FirstUser', 'UI', 'LevelOne', 'Battle', 'Adventure'),
    [switch]$LogicOnly,
    [string]$ReuseProject,
    [ValidateRange(60, 1800)]
    [int]$TimeoutSeconds = 600
)

$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../../..')).Path
if ([string]::IsNullOrWhiteSpace($env:PVZ_BOARD_STRESS_ASSETS)) {
    $env:PVZ_BOARD_STRESS_ASSETS = (Resolve-Path -LiteralPath (Join-Path $repository 'Assets/StreamingAssets')).Path
}
$filters = foreach ($testMode in $Mode) {
    switch ($testMode) {
        'FirstUser' { 'UISmokeTest.FirstUser' + $(if ($LogicOnly) { 'LogicScenario' } else { 'VisualScenario' }) }
        'UI' { 'UISmokeTest.Navigation' + $(if ($LogicOnly) { 'LogicScenario' } else { 'VisualScenario' }) }
        'LevelOne' { 'LevelOneVisualSmokeTest.' + $(if ($LogicOnly) { 'LogicScenario' } else { 'VisualScenario' }) }
        default {
            if ($LogicOnly) {
                throw "$testMode requires real rendering/audio and has no logic-only scenario."
            }
            switch ($testMode) {
                'Battle' { 'VisualSmokeTest.RunsScenario' }
                'Adventure' { 'AdventurePlayerSmoke.RunsScenario' }
                'Feedback' { 'FeedbackPlayerSmoke.RunsScenario' }
                'Progression' { 'ProgressionPlayerSmoke.RunsScenario' }
                'IntroAudio' { 'IntroAudioPlayerSmoke.RunsScenario' }
                'CombatFidelity' { 'CombatFidelityPlayerSmoke.RunsScenario' }
            }
        }
    }
}
$options = @{
    Platform = 'PlayMode'
    TestFilter = (($filters | ForEach-Object { 'PlantsVsZombies.Tests.PlayMode.Smoke.' + $_ }) -join ';')
    TimeoutSeconds = $TimeoutSeconds
}
if ($Unity) { $options.Unity = $Unity }
if ($ReuseProject) { $options.ReuseProject = $ReuseProject }
& (Join-Path $PSScriptRoot '../Run-UnityTests.ps1') @options
