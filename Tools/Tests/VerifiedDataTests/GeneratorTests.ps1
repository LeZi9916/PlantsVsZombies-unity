Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$generator = Join-Path $PSScriptRoot '../../Tools/GenerateVerifiedData.ps1'
$source = Join-Path $PSScriptRoot '../../../analysis/ida/balance'
$fixture = Join-Path $PSScriptRoot ('generator-fixture-' + [Guid]::NewGuid().ToString('N'))
$names = @('plants_full', 'zombies_full', 'adventure_levels_full', 'challenge_modes_full', 'reanimations_full')
$utf8 = [System.Text.UTF8Encoding]::new($false)

function Save-Table([string]$name, $table) {
    $json = $table | ConvertTo-Json -Depth 50
    [IO.File]::WriteAllText((Join-Path $fixture ($name + '.json')), $json, $utf8)
}

function Restore-Table([string]$name) {
    Copy-Item -LiteralPath (Join-Path $source ($name + '.json')) -Destination $fixture -Force
}

function Expect-Failure([string]$expectedText) {
    $failure = $null
    try { & $generator -Check -InputDirectory $fixture }
    catch { $failure = $_ }
    if ($null -eq $failure -or $failure.ToString().IndexOf($expectedText, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Expected generator failure containing '$expectedText'; got '$failure'"
    }
}

try {
    [void][IO.Directory]::CreateDirectory($fixture)
    foreach ($name in $names) { Restore-Table $name }
    & $generator -Check -InputDirectory $fixture

    $table = [IO.File]::ReadAllText((Join-Path $fixture 'plants_full.json'), $utf8) | ConvertFrom-Json
    $table.rows[1].id = 0
    Save-Table 'plants_full' $table
    Expect-Failure 'Unexpected plant row ID'
    Restore-Table 'plants_full'

    $table = [IO.File]::ReadAllText((Join-Path $fixture 'adventure_levels_full.json'), $utf8) | ConvertFrom-Json
    $table.levels[0].zombie_flag_cells[1].zombie_id = 0
    Save-Table 'adventure_levels_full' $table
    Expect-Failure 'zombie flag row ID'
    Restore-Table 'adventure_levels_full'

    $table = [IO.File]::ReadAllText((Join-Path $fixture 'zombies_full.json'), $utf8) | ConvertFrom-Json
    $table.rows[0].reanimation_id.value = 22
    Save-Table 'zombies_full' $table
    Expect-Failure 'Invalid animation reference'
    Restore-Table 'zombies_full'

    $table = [IO.File]::ReadAllText((Join-Path $fixture 'plants_full.json'), $utf8) | ConvertFrom-Json
    $table.rows[0].sun_cost.value = 101
    Save-Table 'plants_full' $table
    Expect-Failure 'missing or out of date'
    Restore-Table 'plants_full'

    $table = [IO.File]::ReadAllText((Join-Path $fixture 'reanimations_full.json'), $utf8) | ConvertFrom-Json
    $table.source.all_exported_table_bytes_match_exe = $false
    Save-Table 'reanimations_full' $table
    Expect-Failure 'Unexpected or unverified EXE source'
    Write-Host 'Generator tests passed (baseline and 5 failure cases).'
}
finally {
    foreach ($name in $names) {
        $path = Join-Path $fixture ($name + '.json')
        if ([IO.File]::Exists($path)) { [IO.File]::Delete($path) }
    }
    if ([IO.Directory]::Exists($fixture)) { [IO.Directory]::Delete($fixture) }
}
