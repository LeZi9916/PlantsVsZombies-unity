#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Output = (Join-Path $PSScriptRoot '../FEATURE_COVERAGE.csv'),
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$analysis = Join-Path $project '../analysis/ida/balance'
$plants = (Get-Content -LiteralPath (Join-Path $analysis 'plants_full.json') -Raw | ConvertFrom-Json).rows
$zombies = (Get-Content -LiteralPath (Join-Path $analysis 'zombies_full.json') -Raw | ConvertFrom-Json).rows
$levels = (Get-Content -LiteralPath (Join-Path $analysis 'adventure_levels_full.json') -Raw | ConvertFrom-Json).levels
$modes = (Get-Content -LiteralPath (Join-Path $analysis 'challenge_modes_full.json') -Raw | ConvertFrom-Json).rows
if ($plants.Count -ne 53 -or $zombies.Count -ne 34 -or $levels.Count -ne 50 -or $modes.Count -ne 72) {
    throw 'Unexpected original data table sizes; review before generating coverage.'
}

$strings = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::OrdinalIgnoreCase)
$pending = $null
$stringsPath = Join-Path $project 'Assets/OriginalPvZ/Raw/properties/LawnStrings.txt'
foreach ($line in [IO.File]::ReadAllLines($stringsPath, [Text.Encoding]::UTF8)) {
    if ($line -match '^\[([^\]]+)\]\s*$') { $pending = $Matches[1]; continue }
    if ($pending -and $line.Trim().Length -gt 0) {
        if (-not $strings.ContainsKey($pending)) { $strings.Add($pending, $line.Trim()) }
        $pending = $null
    }
}
function Localized([string]$Key) {
    if ($strings.ContainsKey($Key)) { return $strings[$Key] }
    return ''
}
function Row([string]$Category, [int]$Id, [string]$Key, [string]$Name, [string]$State, [string]$Note) {
    [pscustomobject]@{ 类别 = $Category; 原版ID = [string]$Id; 原始标识 = $Key; 中文名称 = $Name; 状态 = $State; 说明 = $Note }
}

$partialPlants = @(0..52)
$partialZombies = @(0..32)
$rows = [Collections.Generic.List[object]]::new()
foreach ($item in $plants) {
    $id = [int]$item.id
    $state = if ($id -in $partialPlants) { '部分实现' } else { '未实现' }
    $note = if ($id -ge 40 -and $id -le 48) { '商店正常购买、购买记录、卡组与模仿目标持久化已接；战斗、发射与受击展示仍非逐帧全等。' }
        elseif ($id -eq 38) { '金盏花产币、手动及吸金磁拾取已接持久化钱包和实际商店扣款。' }
        elseif ($id -eq 49) { '冒险保龄球爆炸坚果有实际移动、碰撞和爆炸；非普通种植卡。' }
        elseif ($id -ge 50) { '原特殊变体由独立模式或花园状态表示：巨大坚果、幼苗、反向双发；不作为普通卡出售。' }
        elseif ($id -in $partialPlants) { '连续冒险有实际行为；碰撞、速度、动画和部分特殊条件仍近似。' }
        elseif ($id -ge 49) { '原生表中的特殊或变体记录；静态表不等于可玩行为。' }
        else { '图鉴展示、素材和静态定义不等于可玩行为。' }
    $rows.Add((Row '植物定义' $id $item.internal_name (Localized $item.internal_name) $state $note))
}
foreach ($item in $zombies) {
    $id = [int]$item.id
    $state = if ($id -in $partialZombies) { '部分实现' } else { '未实现' }
    $note = if ($id -ge 26 -and $id -le 32) { '独立模式通过原类型编号、植物头原附着和对应技能表示；红眼巨人有生命差异，特殊阶段仍未完整还原。' } elseif ($id -in $partialZombies) { '冒险核心有护具、状态及对应特殊行为；速度、阶段、动画和部分特例仍近似。' }
        elseif ($id -ge 26) { '原生表中的特殊或变体记录；同名项按 ID 区分。' }
        else { '图鉴展示、素材和静态定义不等于可玩行为。' }
    $rows.Add((Row '僵尸定义' $id $item.internal_name (Localized $item.internal_name) $state $note))
}
foreach ($item in $levels) {
    $id = [int]$item.level_index_1_based
    $world = [int][Math]::Floor(($id - 1) / 10) + 1
    $stage = (($id - 1) % 10) + 1
    $state = '部分实现'
    $note = '已接首轮及后续周目、戴夫选卡、奖励、钱包、原随机与完整波次调度；仍保留战斗和演出差异。'
    $rows.Add((Row '冒险关卡' $id ("LEVEL_{0:D2}" -f $id) ("$world-$stage") $state $note))
}
foreach ($item in $modes) {
    $id = [int]$item.id
    $key = ([string]$item.localization_key).Trim([char[]]'[]')
    $supported = ($id -ge 1 -and $id -le 35) -or ($id -ge 51 -and $id -le 70) -or $id -in @(43,50)
    $state = if ($supported) { '部分实现' } else { '未实现' }
    $note = if ($id -eq 43) { '禅境花园已接原素材、照料、成长、产币与保存。' } elseif ($id -eq 50) { '智慧树已接购买、施肥、原成长轨道与文本、高度保存。' } elseif ($supported) { '独立模式目录、对应玩法、原版解锁和成绩奖励已接；不宣称所有强制分支及演出逐帧一致。' } else { '原版隐藏实验或内部模式；未作为可玩模式曝光。' }
    $rows.Add((Row '模式元数据' $id $key (Localized $key) $state $note))
}

$csv = (($rows | ConvertTo-Csv -NoTypeInformation) -join "`n") + "`n"
$outputPath = [IO.Path]::GetFullPath($Output, (Get-Location).ProviderPath)
if ($Check) {
    if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) { throw "Coverage file missing: $outputPath" }
    $existing = [IO.File]::ReadAllText($outputPath, [Text.Encoding]::UTF8).Replace("`r`n", "`n")
    if ($existing -cne $csv) { throw 'Coverage differs from this snapshot; review implementation and status mappings.' }
    Write-Output "PASS: $($rows.Count) coverage records match the 2026-11-10 snapshot."
}
else {
    if (Test-Path -LiteralPath $outputPath) { throw "Refusing to overwrite existing coverage file: $outputPath" }
    [IO.File]::WriteAllText($outputPath, $csv, [Text.UTF8Encoding]::new($true))
    Write-Output "Wrote $($rows.Count) coverage records: $outputPath"
}
