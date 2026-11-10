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
| `Assets/Scripts/Game` | 游戏入口、界面导航与各模块协调 |
| `Assets/Scripts/Simulation` | 模拟状态、游戏规则与模拟主循环 |
| `Assets/Scripts/Adventure` | 冒险流程、波次、奖励与表现状态 |
| `Assets/Scripts/Rendering` | URP 相机、对象池、MeshRenderer 和渲染资源生命周期 |
| `Assets/Scripts/Reanimation` | 原版动画解析、采样与渲染命令生成 |
| `Assets/Scripts/UI` | 界面交互及位图字体 |
| `Assets/Scripts/Diagnostics` | 显式命令行触发的冒烟验证组件 |
| `Assets/Scripts/VerifiedData` | 原版静态数据模型、只读目录与可重生成的数据表 |
| `Assets/Editor` | 场景、构建、资源导入与 URP 配置工具 |
| `Assets/Tests` | Unity EditMode 与 PlayMode 回归 |
| `Tools/CSharp` | 独立 C# 开发工具 |
| `Tools/Tests` | 独立验证项目与隔离 Unity 测试脚本 |

运行时代码属于 `PlantsVsZombies.Runtime`，编辑器工具属于仅限 Editor 的 `PlantsVsZombies.Editor`。项目类型使用单文件声明，原来的 partial 声明已经合并，公开嵌套数据类型已提升到各自职责目录。脚本移动保留原 `.meta`，尤其保留场景引用的 `PvZGame` 脚本 GUID。

## 渲染

项目使用 Unity 随附的 **URP 17.3.0**。`Assets/Settings` 内的 Pipeline、Renderer、Global Settings 与 Volume Profile 是 Unity Editor 生成的共享资产；Graphics 与所有 Quality 等级引用相同的 Pipeline。

棋盘图像与动画四边形提交到复用的 `GameObject + MeshFilter + MeshRenderer` 对象，由 URP 相机渲染到纹理。对象池复用网格、顶点缓冲及材质属性，资源随渲染器 `Dispose` 释放。第 31 层 `PvZ Rendering` 专供隔离的渲染相机使用。

原版界面的文字、输入及弹窗继续使用 IMGUI，棋盘结果作为一张纹理合成，以保留现有界面层叠。动画底层不再调用 GL 即时绘制。这次迁移不包含整套 UI Toolkit 界面重写。

运行素材实际包含半透明像素，不能将所有对象直接放入不透明队列。完全不透明纹理及不透明 tint 使用深度写入；带透明边缘、渐变和加色的绘制保留透明混合。绘制深度遵循原来的提交顺序。

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
