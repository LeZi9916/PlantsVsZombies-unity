# 1-1 实验模式：接入和边界

这是不完整的实验模式，不是原版完整复现。它默认关闭，使用 `-pvz-level-one` 启动 Unity Player 后可从冒险入口进入；普通启动仍使用白天演示。静态背景、草皮和种子栏位置使用 `analysis/ida/board_ui/README.md` 的已确认坐标，但入场动画、教学文字和波次进度条仍是近似实现。本实验没有运行原游戏进行动态比对。

## 模式与查询

```csharp
var rules = new GameRules { EnableExperimentalLevelOne = true };
var game = new GameSimulation(rules);
```

`GameRules.EnableExperimentalLevelOne` 默认为 `false`。模拟器在构造时确定模式；构造后改动该开关不会改变现有实例。实验选项及回调也在构造时捕获；需要更换时构造新实例。`GameRules.SkySunInterval`、`ZombieSpeed` 等原有规则仍由现有模拟读取。

以下 API 只描述本实验，不等于原版完整行为：

| API | 含义 |
| --- | --- |
| `IsExperimentalLevelOne`、`LevelOneUsesSingleRowLawn` | 模式及单行草坪检测；正常白天演示中均为 false。 |
| `IsLevelOnePlantingCell(row, column)` | 零起点格子是否属于中间草地；仅检查布局，不检查占位/选卡。无效坐标返回 false。 |
| `CanSelectLevelOneSeed(kind)`、`CanPlantLevelOneSeedAt(row, column)` | 无副作用的当前条件查询。最终仍由 `TrySelectLevelOneSeed`、`TryPlant` 校验。 |
| `LevelOneSelectedSeed` | 当前已选卡，未选或非实验模式时为 null。 |
| `CurrentLevelOneTutorialStage` | 可空枚举；非实验模式为 null，实验模式只覆盖原生状态 0–4。 |
| `LevelOneTutorialBlocksWaveCountdown` | 已确认的教学波次门；它不包含所有可能的暂停或结束原因。 |
| `LevelOneWavesDispatched`、`LevelOneTotalWaves` | 已派发批次与总批次；实验模式为 0–4 和 4，非实验模式总数为 0。 |
| `LevelOneWaveProgress`、`LevelOneWaveCountdownSeconds` | 近似批次进度 0–1 和近似倒计时；不是原版按血量推进的进度条。倒计时在教学门关闭时暂停，全部派发或非实验模式时为 null。 |
| `LevelOneTotalZombies`、`SpawnedZombies` | 实验模式预定数量 5 与已创建数量；不要用原有 `Rules.ZombieCount` 计算实验进度。 |

实验模式 `TryPlant` 只接受已选中的豌豆射手及第 3 行的 9 格。公共 `Plants`、`Zombies` 等列表仍与现有模拟一样可被宿主直接修改；绕过 API 写入列表不受上述限制。

## 宿主/UI 集成步骤

当前 `PvZGame` 仅在显式参数下启用实验模式。以下记录核心接入方式及其局限；单独的 `-pvz-level-one-smoke` 使用隔离档案执行 Player 冒烟测试。

1. 实验启动参数创建上述 `GameRules`，重新绑定 `EventRaised`，清空当前选择、铲子和选卡界面状态。`deck` 只含 `PlantKind.Peashooter`，并隐藏“选植物”入口。启动参数在单次进程中固定；退出后正常启动恢复普通演示牌组。
2. 继续按现有方式在活动棋盘调用 `game.Update(Time.deltaTime)`。构造后初始教学状态为 `NotStarted`；第一个有效固定步更新才进入 `AwaitingSeedSelection`，因此在这之前选择接口会返回 false。暂停时更新和输入均不推进。
3. 点击豌豆卡时，若 `LevelOneSelectedSeed` 已有值，调用 `CancelLevelOneSeedSelection()`；否则先用 `CanSelectLevelOneSeed(PlantKind.Peashooter)` 检查，再调用 `TrySelectLevelOneSeed(PlantKind.Peashooter)`。用 `LevelOneSelectedSeed` 同步现有 UI 的 `selected` 标记。不要只改 UI 的 `selected`：实验模式要求模型收到选中事件。取消/选中在暂停、胜负结束或条件不满足时返回 false。
4. 草地点击仍按现有 `GridX`、`GridY`、`CellWidth`、`CellHeight` 算 `row`、`column`。`IsLevelOnePlantingCell` 可用于高亮草地，`CanPlantLevelOneSeedAt` 可用于预览当前能否种植；最终调用 `game.TryPlant(PlantKind.Peashooter, row, column)`。成功时模型自动清空选卡、扣阳光并推进教学，失败时保持选中；UI 应根据模型属性同步显示。种子冷却及植物攻击继续使用现有模拟与 `PvZUnity.Timing.PlantTiming` 接入。
5. 核心模拟默认保留旧的 `CollectSun` 路径；Unity 实验入口显式启用 `UseNativeSkySunRules`，天空阳光使用单独的像素坐标列表和点击接口，飞到阳光栏后才入账。详细假设见 `NativeSkySunIntegration/INTEGRATION.md`。默认不允许 `RemovePlant`。
6. `DrawBoard` 使用 `FirstAdventureBoardUi` 的背景和已展开草皮位置；种子栏和首个卡槽也使用该模块。当前假设棋盘、种子栏原点为零且草皮已完全展开；入场动画、色彩叠绘、字体可见基线与实际游戏画面尚未比对。
7. 用 `CurrentLevelOneTutorialStage` 选择文字：状态 1 可提示选豌豆卡，2 提示点击草地，3 提示首次种植成功/等待种子可用，4 提示僵尸来袭。此处只提供状态，不提供完整原版文案持续时间、箭头和音效。用 `LevelOneWaveProgress` 显示实验性的批次进度；它不是原版旗帜/血量进度。

## 明确的暂定行为及替换方式

| 项目 | 默认实验行为 | 替换接口及限制 |
| --- | --- | --- |
| 原生 tick 转秒 | 假定 1 tick = 0.01 秒；教学重置的 200/100 tick 因而为 2/1 秒。 | `LevelOneOptions.SecondsPerNativeTick`；这个换算尚未运行原版验证。现有种子冷却仍由共享模拟的 0.01 秒步长计算，750 表字段对应现实现的 751 步。 |
| 波次时序 | 初始等待 18 秒，但第二次有效种植会按教学效果重置；之后固定 25 秒一批。 | `InitialWaveDelaySeconds`、`BetweenWaveDelaySeconds` 或 `BetweenWaveDelay(game, previousWaveIndex)`；索引从 0 开始，只在前三批后调用。返回值有限且非负，0 也要等下一个模拟步。原版随机性、血量提前推进和准确时序未实现。 |
| 波次内容 | 确认的 1/1/1/2，普通僵尸，行 2。 | 内容来自 `LevelOneData`；只有时序、位置等是可配置近似。 |
| 出生位置/血量 | 起点 9.5，批内相距 0.45，生命 270，速度沿用 `GameRules.ZombieSpeed`。 | `ZombieSpawnX`、`ZombieSpacingInWave`、`ZombieHealth` 和 `ZombieSpeed`。碰撞、攻击、动画与其他战斗细节沿用现有模拟，并非原版验证。 |
| 天降阳光 | 核心默认保留旧近似；Unity 实验入口启用 `UseNativeSkySunRules`，使用已验证的计数、下落、收集和消失状态机。 | 原版全局随机调用顺序、场景门控及 tick 到秒的换算仍未还原；Unity 中阳光动画尺寸和淡出绘制是近似。旧路径可用 `SkySunTargetY`、`SkySunFactory`、`SkySunInterval` 配置。 |
| 铲子/割草机 | 默认禁止通过 `RemovePlant` 铲植物；沿用 `MowersEnabled`，仅中间行保留可用割草机。 | `AllowPlantRemoval`、`GameRules.MowersEnabled`。这两项的首关原版时机仍未核实。 |
| 结束与奖励 | 四批派发后，只要模拟器的僵尸列表为空就立即 `Won`；**不创建奖励**。 | `CompletionCondition(game)` 可替换该占位规则；仅在四批派发后调用，返回 true 才获胜。可由宿主自行管理奖励对象及拾取标志后返回 true，但这仍不是原版奖励实现。 |

例如宿主需要先自行展示并拾取实验性奖励时，可在构造前设置 `CompletionCondition = g => rewardCollected && g.Zombies.Count == 0`。模型不会生成或设置 `rewardCollected`；若回调一直返回 false，关卡保持进行中。回调应避免重入 `Update` 或修改正在枚举的列表。

教学中 `Plants.Count` 近似原版植物数据数组的分配计数；原版可能有延迟释放条目。状态 4 后的完整教学、原版奖励掉落候选过滤、拾取动画、600 tick 结束过程、真正转场及随机数序列都没有实现。`PlantTiming` 的现有接入被保留，但其宿主输入条件、动画轨道和本模拟碰撞仍不等于完整原版。

## 测试

- `LevelOneTests.csproj` 同时运行原 IDA 数据检查和新增实验模式测试，覆盖模式默认值、行/卡限制、选中取消、阳光与冷却、教学门、波次、可替换回调、固定步长以及通过实际豌豆攻击结束关卡。
- `FullSimulationRegression`、`FullAdvancedRegression` 将原测试源码与完整 `Levels/` 一起编译，以验证 Unity 脚本集合存在时普通演示仍通过；原两个测试工程本身也需单独运行。
- `UnityCoreCompile` 用 C# 7.3/.NET Standard 2.1 编译核心脚本；它不是 Unity 编辑器构建。额外运行了现有 `PlantTimingTests`，其原始 x86 机器码对照与本实验无关，只用于验证保留的植物计时模型。
- `wave_native_oracle.py` 在合成的首轮 1-1 内存状态中执行 EXE 的 `0x416860`、`0x4150C0`，得到唯一可用 ID 0 与 `[[0],[0],[0],[0,0]]`。它只拦截 `0x5267C0`，并确认候选恰为一个 ID 0 后返回该候选，不模拟 RNG。原始写入轨迹还显示末批的首个普通僵尸由 `0x41502D` 直接插入，另一个经过选择。该结果不是原版游戏实机运行，也不验证教程、时序或胜利。
- 在已配置 `Ubuntu-24.04` WSL 且已有 Unicorn 依赖的本机，从工作区根目录可重复上述额外检查：

```powershell
& "$env:WINDIR\System32\wsl.exe" -d Ubuntu-24.04 -- sh -c 'PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=analysis/ida/plant_timing/vendor python3 PvZUnity/tests/LevelOneTests/wave_native_oracle.py'
```

  这里只使用既有的第三方 Unicorn 依赖，不复制其他项目的行为实现。若该环境或依赖不存在，可运行 .NET 测试，但不能声称运行了此机器码检查。
- 本次 .NET 测试将两个原测试工程的中间及输出路径重定向到 `tests/LevelOneTests/`。未运行 Unity 编辑器构建，也未做原版游戏动态对照。
