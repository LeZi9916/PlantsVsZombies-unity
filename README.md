# PlantsVsZombies Unity

本项目使用 **Unity 6000.3.17f1**，主代码根命名空间为 `PlantsVsZombies`，许可证为 GPLv3。

## 开发

```powershell
git submodule update --init --recursive
```

使用 `ProjectSettings/ProjectVersion.txt` 指定的 Editor 打开工程。不要通过根目录生成的 `.csproj`、`.sln` 或 `.slnx` 构建 Unity 主项目。

唯一启用的游戏场景是 `Assets/Scenes/Lawn.unity`。原版运行素材放在 `Assets/StreamingAssets` 的 `images`、`reanim`、`sounds`、`particles` 和 `properties` 子目录；资源解析同时兼容历史的 `StreamingAssets/PvZ` 布局。原版素材及个人存档不属于本次代码重构的提交内容。

## 代码组织

| 目录 | 职责 |
| --- | --- |
| `Assets/Scripts/Game` | 游戏生命周期、界面导航与游戏流程协调 |
| `Assets/Scripts/Simulation` | 模拟状态、游戏规则与模拟主循环 |
| `Assets/Scripts/Adventure` | 冒险流程、波次、奖励与表现状态 |
| `Assets/Scripts/Rendering` | URP 相机、对象池、MeshRenderer 和渲染资源生命周期 |
| `Assets/Scripts/Reanimation` | 原版动画解析、采样与渲染命令生成 |
| `Assets/Scripts/UI` | Canvas 界面、独立输入路由及位图字体 |
| `Assets/Scripts/Diagnostics` | 显式命令行触发的冒烟验证组件 |
| `Assets/Scripts/VerifiedData` | 原版静态数据模型、只读目录与可重生成的数据表 |
| `Assets/Editor` | 场景、构建、资源导入与 URP 配置工具 |
| `Assets/Tests` | Unity EditMode 与 PlayMode 回归 |
| `Tools/CSharp` | 独立 C# 开发工具 |
| `Tools/Tests` | 独立验证项目与隔离 Unity 测试脚本 |

运行时代码属于 `PlantsVsZombies.Runtime`，编辑器工具属于仅限 Editor 的 `PlantsVsZombies.Editor`。项目类型使用单文件声明，原来的 partial 声明已经合并，公开嵌套数据类型已提升到各自职责目录。脚本移动保留原 `.meta`，尤其保留场景引用的 `PvZGame` 脚本 GUID。

## 渲染

项目使用 Unity 随附的 **URP 17.3.0**。`Assets/Settings` 内的 Pipeline、Renderer、Global Settings 与 Volume Profile 是 Unity Editor 生成的共享资产；Graphics 与所有 Quality 等级引用相同的 Pipeline。

棋盘图像与动画四边形提交到复用的 `GameObject + MeshFilter + MeshRenderer` 对象，由启用的 URP 场景相机直接显示。对象池复用网格、顶点缓冲及材质属性，资源随渲染器 `Dispose` 释放。第 31 层 `PvZ Rendering` 专供游戏渲染相机使用。

界面图像、文字、输入框、滑块与弹窗由 Canvas 组件显示，`GamePresenter` 提交每帧表现，`PvZGame` 只保留生命周期、跨界面共享状态与每帧执行顺序；音效与音乐（`Audio`）、输入路由与菜单（`Game`）、冒险开场/关卡/奖励（`Adventure`）、独立模式（`Modes`）、商店/花园/智慧树/Dave（`Progression`）、存档工具（`Profiles`）、纹理缓存（`Rendering`）和棋盘布局（`BoardUI`）分别由 `PvZGame` 按需创建的协作类负责。架构测试限制 `PvZGame.cs` 不超过 700 行。输入在帧更新时独立采样，使用与相机、Canvas 相同的 800×600 逻辑视口换算；宽屏留边不参与棋盘点击。运行时不再依赖 `OnGUI`、`GUI` 绘制或 `Event.current`。

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
