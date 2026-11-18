# PlantsVsZombies Unity

本项目使用 **Unity 7000.0.0a16**，主代码根命名空间为 `PlantsVsZombies`，许可证为 GPLv3。

## 开发

```powershell
git submodule update --init --recursive
```

使用 `ProjectSettings/ProjectVersion.txt` 指定的 Editor 打开工程。不要通过根目录生成的 `.csproj`、`.sln` 或 `.slnx` 构建 Unity 主项目。

唯一启用的游戏场景是 `Assets/Scenes/Lawn.unity`。原版运行素材放在 `Assets/StreamingAssets` 的 `images`、`reanim`、`sounds`、`particles` 和 `properties` 子目录；资源解析同时兼容历史的 `StreamingAssets/PvZ` 布局。原版素材及个人存档不属于本次代码重构的提交内容。

## 原版运行资源

完整发布原版美术、动画、粒子、字体和音频到当前 `Assets/StreamingAssets` 布局：

```sh
python3 -B Tools/PrepareRuntimeAssets.py \
  --source /Volumes/OCZ-ARC100/pvz-origin/extracted_assets \
  --reference Assets/OriginalPvZ \
  --destination Assets/StreamingAssets \
  --report .unity-runs/assets-import/import-report.json --apply
```

工具先校验全部 PAK 清单及独立原始资源副本，再补齐运行资源和转换后的 PNG、字体及 WAV；相同内容和仅换行不同的文本不重写，已有有效 GUID 保留。真实内容冲突需按工具报告逐项指定 `--replace-existing`，原文件会备份到忽略的报告目录。`--verify-only` 完全只读。完整规则和回归命令见 `Tools/Tests/RuntimeAssetImportTests/README.md`。

原始 MO3/AU 和 151 份 Flash 美术源一并保留；Unity 音频使用已转换的 OGG/WAV，动态音乐仍使用现有分轨。菜单 **PlantsVsZombies > Verify Imported Runtime Assets** 可验证 Unity 图片、音频、字体引用及 metadata，结果位于 `.unity-runs/assets-import`。原始素材、导入报告和个人运行数据不自动提交。

## 代码组织

| 目录 | 职责 |
| --- | --- |
| `Assets/Scripts/Game` | 游戏生命周期、界面导航与游戏流程协调 |
| `Assets/Scripts/Simulation` | 模拟状态、游戏规则与模拟主循环 |
| `Assets/Scripts/Adventure` | 冒险流程、波次、奖励与表现状态 |
| `Assets/Scripts/Rendering` | 自定义渲染管线（`Pipeline/`）、相机、对象池、MeshRenderer 和渲染资源生命周期 |
| `Assets/Scripts/Reanimation` | 原版动画解析、采样与渲染命令生成 |
| `Assets/Scripts/UI` | Canvas 界面、独立输入路由及位图字体 |
| `Assets/Scripts/Inputs` | Input System 设备采样、触控归属及逻辑输入快照 |
| `Assets/Scripts/VerifiedData` | 原版静态数据模型、只读目录与可重生成的数据表 |
| `Assets/Editor` | 场景、构建、资源导入与渲染管线配置工具 |
| `Assets/Tests` | Unity Test Runner 的 EditMode、PlayMode 回归与冒烟场景 |
| `Tools/CSharp` | 独立 C# 开发工具 |
| `Tools/Tests` | 独立验证项目与隔离 Unity 测试脚本 |

模拟器的职责划分、行为组合、只读实体接口和迁移约定见 [`Assets/Scripts/Simulation/README.md`](Assets/Scripts/Simulation/README.md)；重构前后状态对照、性能数据与验证限制见 [`Assets/Tests/SimulationRefactor.md`](Assets/Tests/SimulationRefactor.md)。

运行时代码属于 `PlantsVsZombies.Runtime`，编辑器工具属于仅限 Editor 的 `PlantsVsZombies.Editor`。项目类型使用单文件声明，原来的 partial 声明已经合并，公开嵌套数据类型已提升到各自职责目录。脚本移动保留原 `.meta`，尤其保留场景引用的 `PvZGame` 脚本 GUID。

## 渲染

项目使用自定义的轻量渲染管线 **PvZRenderPipeline**（`Assets/Scripts/Rendering/Pipeline`），不依赖 URP 或任何渲染管线包，只提供游戏需要的无光照 2D 绘制：按深度排序的相机、清屏与视口、不透明/透明两个绘制阶段、渲染到 RenderTexture 的 render request、uGUI 与 Editor 视图。`Assets/Settings/PvZRenderPipeline.asset` 由菜单 **PlantsVsZombies > Configure Render Pipeline** 生成，Graphics 与所有 Quality 等级引用它，且所有 Quality 等级关闭 MSAA。结构、支持范围及扩展方式见该目录的 `README.md`。

棋盘图像与动画部件通过逻辑图片句柄提交，优先使用 `Graphics.RenderMeshPrimitives` 按原绘制顺序实例化。同一批次可引用八张既有纹理，A/B/A 页序列无需重排或重新烘焙即可合并；Canvas、水面、其他池插层、混合变化及容量限制仍拆批。实例沿用精确多边形裁剪，每块固定容纳 8192 个部件，最多八块、约 16 MiB GPU 数据。全帧封存后才上传变化前缀并提交；冻结画面复用缓冲内容。能力不足、预算溢出与局部合成保留 Mesh 兼容路径，固定布局与容量避免普通帧重复清空网格。第 31 层 `PvZ Rendering` 专供游戏渲染相机使用。

Unity 7 中显式实例绘制与原生 Canvas 仅设置 `sortingOrder` 无法保证交错顺序，因此完整场景把原绘制跨度压缩到 3000–5000 材质队列。桥接复用材质副本，保留原始纹理、Stencil、字体及 Shader 属性，仅在场景和 SceneView 的第 31 层使用队列优先并保留 CanvasOrder；Canvas 的排序值和输入优先级不变。非默认原生 sorting layer、跨度或材质预算不足时整帧使用 Mesh。不会按纹理重排透明部件，也不会改变每个部件的深度。

进入 Play Mode 和构建 Player 前会增量生成四档无损图集（256、512、1024、2048）；也可使用 **PlantsVsZombies > Generate Rendering Atlases**。页面按实际占用输出紧致矩形（可为非二次幂），常用战斗页由完整动画、装备换图与粒子依赖确定，冷资源单独存放。运行时先根据 `SystemInfo.maxTextureSize` 和默认 16 MiB 单页预算选档，再按需加载页面；缺页或尺寸错配会降档，超大原图通过带采样保护边的切片保留像素精度。生成物位于忽略的 `Assets/Generated/PvZRendering`，不要提交。原始素材和既有 `.meta` 不变，首次生成需要额外磁盘空间。正式资源路径同时打包动画、粒子与原始文本；外部素材目录及合成测试保留散图兼容路径。

仅 Editor 创建 `PvZ Actors/Plants` 和 `PvZ Actors/Zombies` 检查层级，根节点、附件与轨道分别复用；Player 跳过这些节点及其遍历。节点开关同步更新可见性版本，选中 sprite 可查看源资源和完整变换。表现缓存使用会话代次与实体 ID 组成的值身份；局部动画只在精确采样时间、资源、附件与混合版本相同时复用，位置、装备、颜色和绘制顺序继续实时应用。透明几何保留双线性边缘，命中、灰度及亮度反馈沿用原混色语义。

进入新关卡或生存阶段时，`GamePreparationController` 在开始战斗前准备资源、模型池、表现容量、粒子和实例缓冲。恢复已完成的生存阶段时直接显示棋盘，随后在新的阶段准备战斗资源。白天泳池的闪光粒子恢复时最多预热 100 个原版 tick，水面网格继续使用存档计时。模型回收检查全部权威引用，并经过两个完整更新边界，覆盖尸体、附属投射物与待消费事件；运行时池不进入存档。超出准备容量仍安全扩容并继续玩法，性能证据保留这些帧。复现与平台验证命令见 `Tools/Tests/RenderingPerformance/README.md` 和 `Tools/Tests/PerformanceReplay/README.md`。

性能续作已在 Unity 7000.0.0a16 / macOS Metal 上完成三起点的模型与像素对照，精灵提交减少 92.97%–96.01%；已补齐容量、事件池、活跃动画容器与分帧生命周期预热，并执行五分钟 CoreCLR Player 压力观察。完整零 GC 和 Windows 平台验收仍未通过，当前已测结果、剩余分配及平台限制见 [`OrderedInstancingPerformance.md`](Assets/Tests/OrderedInstancingPerformance.md)。

界面图像、文字、输入框、滑块与弹窗由 Canvas 组件显示，`GamePresenter` 提交每帧表现，`PvZGame` 只保留生命周期、跨界面共享状态与每帧执行顺序；音效与音乐（`Audio`）、输入路由与菜单（`Game`）、冒险开场/关卡/奖励（`Adventure`）、独立模式（`Modes`）、商店/花园/智慧树/Dave（`Progression`）、存档工具（`Profiles`）、纹理缓存（`Rendering`）和棋盘布局（`BoardUI`）分别由 `PvZGame` 按需创建的协作类负责。架构测试限制 `PvZGame.cs` 不超过 700 行。输入在帧更新时独立采样，使用与相机、Canvas 相同的 800×600 逻辑视口换算；宽屏留边不参与棋盘点击。运行时不再依赖 `OnGUI`、`GUI` 绘制或 `Event.current`。

输入使用 Unity 7 随附的 **Input System 7.0.0**，Player Settings 的 Active Input Handling 为 **Input System Package (New)**。设备输入统一采样到 `GameInputSnapshot`，保留鼠标左右键、滚轮、稳定的主触控、Escape、Return/小键盘 Enter、Space 和 F9。触摸历史在首个输入更新前启用；失焦或设备移除会取消捕获，并等待旧手势结束。滚轮保留原来的方向和每格三单位缩放。

原生 Canvas 控件使用官方 `InputSystemUIInputModule`，由现有 `CanvasInputModule` 处理取消时的 pointer-up/end-drag 和后续手势；文本框的组合串、IME 开关及候选窗位置接到新键盘接口。游戏热键、按钮按下状态和图鉴都读取统一快照。修改输入后端后，已打开的 Editor 需要重启才能启用新的原生后端。

原版动画中需要局部合成的特效仍使用有界的 RenderTexture；棋盘不会先渲染成整屏纹理再交给界面显示。界面与世界对象的绘制顺序由各自的场景组件和 Canvas 层级维护。

运行素材实际包含半透明像素，不能将所有对象直接放入不透明队列。场景对象保留透明混合，并与 Canvas 共用有序的图层；局部离屏处理中，完全不透明纹理及不透明 tint 可以使用深度写入。带透明边缘、渐变和加色的绘制保留原有混合规则。

## 验证

独立验证项目的运行方式见各目录 README。常用命令：

```powershell
dotnet run --project Tools/Tests/ArchitectureTests/ArchitectureTests.csproj -c Release
dotnet run --project Tools/Tests/RandomTests/RandomTests.csproj -c Release
dotnet run --project Tools/Tests/SunTests/SunTests.csproj -c Release
dotnet run --project Tools/Tests/LevelOneTests/LevelOneTests.csproj -c Release
./Tools/Tests/Run-UnityTests.ps1
./Tools/GenerateVerifiedData.ps1 -Check
git diff --check
```

Unity 测试脚本使用指定版本 Editor，在忽略的临时副本中编译和运行 EditMode/PlayMode 测试，不修改主工程的场景和 Player Settings。渲染测试需要真实图形设备，不能使用 `-nographics` 替代像素验证。

独立纯模拟用例的通过不代表整局 Player、移动设备或其他目标平台的运行结果。Windows 图形回归也不能替代 Linux、macOS、Android、iOS 及各 ABI 的构建和真机验证。

## 存档编辑器

在 Unity 菜单 **PlantsVsZombies > Save Editor** 打开存档编辑窗口。默认读取当前项目的 `Application.persistentDataPath`，也可以浏览其他游戏存档目录；目录内需已有 `profiles.dat` 和至少一个游戏用户。

选择用户后，可编辑金币、模式解锁、智慧树、商店物品、模式记录、禅境花园植物和照料时间、冒险关卡与选卡，以及高级奖励标记和既有回执。金币使用存档单位（1 单位等于 10 游戏金币），时间字段使用 UTC Unix 秒并显示本地时间。窗口中的用户选择不会改变游戏当前用户。

**模式解锁** 页支持 **解锁所有模式**，也可为每个已实现模式设置 **跟随游戏进度 / 解锁 / 锁定**。这些设置保存到账号中，独立于成绩与通关记录；解锁时同时开启相应的模式入口。恢复“跟随游戏进度”后重新使用原有冒险进度和奖杯规则。页面按当前冒险草稿显示可进入状态，改完需点击 **保存账号数据**；冒险草稿仍需单独保存。

修改先保存在窗口草稿中。使用 **保存账号数据**、**保存冒险进度** 或 **保存名称** 分别写入对应文件；切换用户、重载和关闭窗口时会提示处理未保存内容。草稿会随窗口序列化，在脚本重新编译后恢复。进入 Play Mode 后暂停编辑，退出后需重载；检测到外部存档变化时也会拒绝覆盖。保存复用现有格式校验、文件锁和备份逻辑，保留其他用户数据及未编辑字段。原版导入源文件 `original-*.dat` 不在编辑范围内。

相关隔离回归测试：

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -AssemblyNames PlantsVsZombies.Tests.EditorRendering
```


## 对局存档编辑器

在 Unity 菜单 **PlantsVsZombies > Match Checkpoint Editor** 打开对局存档窗口。默认使用
`Application.persistentDataPath`，也可浏览其他存档目录；需要 `profiles.dat` 及已有的
`replica-game-<用户标识>-<模式>.dat`。先在游戏中退出正在进行的关卡即可生成对局存档。
窗口按用户列出已有对局，不改变游戏的当前用户选择，不要求账号或冒险进度 sidecar 可读。

可编辑阳光、波次与倒计时、植物生命值与行列、僵尸状态、种子冷却、投射物、拾取物、
小推车及已有动画/场景状态。“全部字段”显示实际存档模型，可展开嵌套能力、数组和字典；
较大集合分页显示，循环引用有显示限制。对象类型、ID、引用关系、关卡定义、随机流和集合
结构保持只读。冷却单位为原版 tick（0.01 秒），行列和波次字段从 0 开始。

修改先留在独立草稿中，不推进对局。点击 **保存当前对局** 后仅写入该对局文件，保留
原金币/掉落回执、关联实体和随机状态，并将上一版保存在同名 `.backup` 文件中。
账号金币、解锁和冒险通关进度仍在 **Save Editor** 中单独编辑。外部文件变化或占用时
拒绝覆盖；游戏与编辑器的保存、删除共享同一对局锁。

切换、重载和关闭窗口会处理未保存草稿；草稿及原始文件基线随窗口序列化，可在脚本
重新编译后恢复。进入 Play Mode 后暂停编辑，退出后须重载。损坏或格式不兼容的文件
只显示读取错误，不会被空白对局覆盖。

```powershell
./Tools/Tests/Run-UnityTests.ps1 -Platform EditMode -TestFilter MatchCheckpointEditorTests
dotnet run --project Tools/Tests/CheckpointTests/CheckpointTests.csproj -c Release
dotnet run --project Tools/Tests/ArchitectureTests/ArchitectureTests.csproj -c Release
```
