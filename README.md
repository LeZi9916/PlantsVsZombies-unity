# PvZ Unity 移植工作区

2026-11-10 战斗与奖励后续修复：原生僵尸出生/速度/掉头掉旗、防具破损与命中音、尸体时长、原奖励拾取与介绍页、阳光音效及飞行额度、植物预览、传送带、灰烬爆炸和地雷、首屏加载条已接入。本轮收尾还包括无头僵尸继续索敌/保留进食状态、原咀嚼与植物受击、连续保龄球、寒冰射手/冰西瓜，以及启动花芽/僵尸头的实际打包补齐。85项定向Player检查通过，发布版启动/音频检查退出码0。具体证据及验证边界见 [本轮详细说明](COMBAT_FIDELITY_20261110.md)。

2026-11-10 后续修复：补齐音乐缺失 Listener、原主线镜头/铺草皮/选卡返回、卡槽冷却与拾取阳光层级，以及原按钮/石框/提示材质。详见 [音乐、开场与 UI 修复](INTRO_AUDIO_20261110.md)。

2026-11-10 后续画面修复：僵尸断手、原 ground 步态、分轨贴图加色、种子包掉落拾取与植物介绍，以及生产前提示已接入。详见 [本轮修复与证据](FEEDBACK_FIXES_20261110.md)。

## 当前状态（2026-11-10）

主菜单接入连续首轮冒险、后续周目、商店、禅境花园，以及独立小游戏 / 生存 / 解谜目录。仍共用一个 Unity 场景和纯 C# 战斗核心。

本次续作已接入：

- 升级植物和模仿者的正常商店购买、永久购买记录、独立冷却、卡组及模仿目标保存；没有把开发授权当成玩家已购买。
- 按用户保存金币钱包；自然僵尸掉落、金盏花、吸金磁、手动拾取、剩余清扫车和通关奖励均走实际结算。商店按本地 PC EXE 的价格、开放条件和库存处理；种子槽可从 6 升至 10，清扫车、草耙和坚果包扎术有实际战斗效果。
- 冒险二周目及之后周目：保留 40 种基础植物和购买内容，戴夫固定挑三张卡，使用二周目波数规则，并包含 4-10 的雪人遭遇。领取 5-10 奖励后，从冒险入口开始下一周目。
- 55 个独立模式：原版 GameMode 1..35、51..70，包含 20 个小游戏、15 个生存目录项和 20 个解谜目录项。模式有各自规则、正常解锁、成绩与奖励；生存跨轮保留草坪，支持无尽。详情见 Assets/Scripts/Modes/COVERAGE.md。
- 禅境花园的照料、成长、需要判断、不同园区、产币、蜗牛、移动、出售和巧克力；智慧树购买、消耗树肥、永久高度、原版成长轨道及原版提示文本。
- 原版年度版 users.dat v14 / user*.dat v12 的用户档案导入、单独导出；保留成就、大头贴和其他未知尾部数据。主菜单按 F9 打开工具。导入复制到独立档案，不覆盖原始文件。**局内 game*.dat 战斗对象快照仍不兼容**，不能把用户档案兼容解释为任意原版存档均可续战。
- 原版整数帧波次调度、健康阈值、大波预警、末波埋伏和冻结取消；战斗随机源使用逆向验证的原版 MT，提供捕获 / 恢复接口。
- 从原版 MO3 实际解码的分轨和段落，按原版 tracker 行边界、战斗强度及场景切换、循环、暂停；包括夜曲 76/77 入口。
- 原版启动 PopCap / 标题 / 草土加载条 / 发芽与僵尸头动画，实际等待 Unity 资源预热和音频加载完成，再显示点击开始。
- 战斗画面使用原版坐标、轨道、动画速度、插值和粒子定义；修正清扫车、植物、僵尸、发射 / 命中 / 受击 / 死亡与阳光显示的接线。新增展示修复的证据和边界见 ../analysis/ida/adventure_presentation/INTEGRATION.md。

以上是实际接入范围，**不宣称已逐帧、逐像素完整复刻原版**。全部特殊僵尸转阶段、全部护甲碎裂与附着事件、成就和专属演出仍有未复原之处；原版完整局内存档也尚未实现。完整说明见 IMPLEMENTATION_20261110.md。先前文档中的“商店、钱包、二周目等尚未实现”描述已被本次更新取代，旧 50 关固定种子报告只代表当时版本。

用户名称仍在 Application.persistentDataPath 的 replica-profiles.dat 中。冒险 v2 检查点在 replica-campaign.adventure，钱包、购买、花园、树和模式成绩在 replica-player.account。沿用旧冒险 v1 文件时自动读取，下一次保存写 v2；领取奖励和购买成功落盘后才发布进度，写入失败保留物品或奖励以便重试。

## 运行

- 使用 Unity Hub 将本目录添加为工程，选择 Unity **6000.3.17f1**，打开 `Assets/Scenes/Lawn.unity` 并进入 Play 模式。
- 也可以运行 `Build/PvZUnity.exe`；该目录需要保留随构建生成的 `PvZUnity_Data` 和 `MonoBleedingEdge`。
- 主菜单中的冒险按钮开始或继续当前用户的冒险。点击卡片后点击网格种植，点击阳光收集，右键取消选择；获得铲子后可铲除植物。传送带按卡片使用，打僵尸直接点击目标，砸罐直接点击罐子。胜利后点击场上掉落的种子包或原奖励物，等待拾取转场并阅读植物介绍，然后点击下一关；二周目普通钱袋奖励按原分支直接续关。菜单可重开当前关或返回主菜单。
- 可用 `Build/PvZUnity.exe -pvz-demo` 启动旧白天演示；它不推进新冒险进度。
- 可用 `Build/PvZUnity.exe -pvz-ui-review -force-d3d11 -screen-fullscreen 0` 手动检查界面；该模式使用临时用户，设置只保存在内存。
- 可用 `Build/PvZUnity.exe -pvz-level-one` 启动单独的 1-1 实验模式，随后从主菜单进入冒险；退出程序并正常启动可返回连续冒险。该模式不实现奖励或通关进度。

从已解包的素材重新准备 Unity 所需资源：

```powershell
& .\Tools\PrepareAssets.ps1
& .\Tools\PrepareAlmanacAssets.ps1
& .\Tools\PrepareBitmapFonts.ps1
python .\Tools\prepare_adventure_assets.py
python .\Tools\prepare_progression_assets.py
python .\Tools\prepare_intro_assets.py
python .\Tools\prepare_feedback_assets.py
```

冒险素材准备脚本需要 Python 和 Pillow；它会预检目标冲突，再准备额外地形、蒙版、金币、冰火球和投射物资源。

原包全部资源及转换结果位于 `Assets/OriginalPvZ/`；资源分类、Unity 导入方式、校验命令与运行时接入限制见 [ORIGINAL_ASSETS.md](ORIGINAL_ASSETS.md)。现有场景仍使用 `StreamingAssets/PvZ/` 的子集，并不会自动使用完整目录中的全部素材。

背景音乐已接入由原 MO3 解码的分轨，依据原 tracker 行边界调度分段、循环、暂停与战斗强度变化；实际 PCM 输出检查与已知边界见 INTRO_AUDIO_20261110.md。

## 验证与构建

在本目录执行战斗模拟测试：

```powershell
dotnet run --project .\tests\PvZSimulation.Tests.csproj --configuration Release
dotnet run --project .\tests\AdventureTests\AdventureTests.csproj --configuration Release
dotnet run --project .\tests\AdvancedPlantTests\AdvancedPlantTests.csproj --configuration Release
dotnet run --project .\tests\VerifiedDataTests\VerifiedDataTests.csproj --configuration Release
dotnet run --project .\tests\WaveTests\WaveTests.csproj --configuration Release
dotnet run --project .\tests\LevelOneTests\LevelOneTests.csproj --configuration Release
dotnet run --project .\tests\LevelOneTests\NativeSkySunIntegration\NativeSkySunIntegration.csproj --configuration Release
dotnet run --project .\tests\SunTests\SunTests.csproj --configuration Release
dotnet run --project .\tests\BoardUiTests\BoardUiTests.csproj --configuration Release -- --native-oracle
dotnet run --project .\tests\PlantTimingTests\PlantTimingTests.csproj --configuration Release
dotnet run --project .\tests\RandomTests\RandomTests.csproj --configuration Release
dotnet run --project .\tests\BitmapFontTests\BitmapFontTests.csproj --configuration Release
dotnet run --project .\tests\ProfileTests\ProfileTests.csproj --framework net9.0 --configuration Release
pwsh -NoProfile -File .\tests\UiComparisonTests\Run.ps1
```

在有可见桌面的 Windows 中，可运行以下检查，逐次保存日志、截图和退出码；检测到成功标记后崩溃退出也会判失败：

```powershell
& .\tests\PlayerSmokeTests\Run.ps1
& .\tests\PlayerSmokeTests\Run.ps1 -Width 1280 -Height 720 -Mode UI
& .\tests\PlayerSmokeTests\Run.ps1 -Mode FirstUser,UI,LevelOne -LogicOnly
```

前两种运行会显示 Player 窗口并验证截图，第三种仅验证逻辑。测试入口及输出见 [tests/PlayerSmokeTests/README.md](tests/PlayerSmokeTests/README.md)。图鉴的独立测试与本机 EXE 局部命中对照见上方图鉴说明。

在 Unity 编辑器中可使用 `PvZ Replica > Build Windows`。也可用本机指定版本批处理：

```powershell
& 'C:\Program Files\Unity Editors\6000.3.17f1\Editor\Unity.exe' `
  -batchmode -nographics -quit -projectPath (Get-Location).Path `
  -executeMethod PvZReplica.ProjectSetup.BuildWindows -logFile (Join-Path (Get-Location).Path 'unity-build.log')
```

构建程序使用 `-pvz-smoke` 参数时会运行战斗冒烟测试；使用 `-pvz-ui-smoke` 检查用户、帮助、主菜单、模式列表、图鉴和设置导航；`-pvz-first-user-smoke` 检查首次启动提示；`-pvz-level-one-smoke` 检查实验首关的单行背景、选卡、种植、收集阳光和首波僵尸。这些测试使用隔离的临时用户数据，并在 `Build/` 中写出截图与日志。测试中的姓名输入由程序注入，真实键盘和中文输入法尚未验证；这些检查不能证明完整原版兼容或 UI 像素一致。

无可用屏幕绘制时，可分别使用 `-pvz-ui-logic-smoke`、`-pvz-first-user-logic-smoke` 和 `-pvz-level-one-logic-smoke` 检查 Player 内部导航与交互；这些参数不截屏，也不验证画面。隐藏窗口和 Unity 批处理窗口在本机不能产生有效的 UI 截图，不能把逻辑测试结果当成视觉测试通过。

`Tools/CompareUiScreenshots.ps1` 可以比较同分辨率的原版和 Unity 截图，报告逐像素 RGBA 差异；测试方法与限制见 `tests/UiComparisonTests/README.md`。只有取得原版同场景、同状态截图并完成比较后，才可评价具体画面的一致性。

## 分析资料

- `../analysis/ida/REPORT.md`：IDA 静态分析、资源与存档调用链。
- `../analysis/ida/balance/FULL_TABLES_REPORT.md`：植物、僵尸、冒险关卡和模式的静态表及证据。
- `../analysis/ida/waves/REPORT.md`：原始机器码验证过的部分冒险波次规则；尚未接入完整刷怪调度。
- `../analysis/ida/random/README.md`：原版 31 位随机生成器的机器码验证；尚未接入战斗模拟。
- `../analysis/ida/level_1_1/REPORT.md`：首轮 1-1 的静态证据和仍未确定的规则。
- `../analysis/ida/board_ui/README.md`：首轮 1-1 草皮、背景与种子栏的原版绘制坐标和限制。
- `../analysis/ida/sun/README.md`：天降阳光、植物产阳光和收集消失的局部机器码证据。
- `../extracted_assets/README.md`：3,394 项资源的提取、转换、索引与校验。
- `../analysis/source/PlantsVsZombies.NET/`：仅供行为研究的独立参考源码；Unity 工程没有使用其兼容层或直接链接该项目。
