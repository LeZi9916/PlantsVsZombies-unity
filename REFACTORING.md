# 2026-11-10 重构与验证记录

## 已实施

- 统一 `PlantsVsZombies` 根命名空间，按 Simulation、Game、Diagnostics 和各业务目录归档类型。
- 合并项目类型的 partial 声明，将公开嵌套 class/struct 提取为独立类型文件；保留现有主脚本 GUID，修正场景的程序集/类型标识并添加入口组件迁移属性。
- 添加 Runtime/Editor asmdef 边界、CRLF、nullable 指令、带花括号的控制语句，并按符号更新私有字段及反射访问名称。
- 接入 Unity 6000.3.17f1 随附的 URP 17.3.0；Graphics 和所有 Quality 等级绑定共享 Pipeline，保留 Render Graph。
- 将动画与棋盘纹理改为池化对象的 MeshRenderer 渲染，替换 GL 立即绘制；复用网格、属性块、相机和离屏目标，明确销毁所有权。
- 全不透明纹理与不透明 tint 使用 Opaque 队列、深度写入及按提交次序分配的 z；透明边缘、半透明 tint、粒子和预乘纹理维持透明混合。Alpha 分类缓存随 `Texture.updateCount` 失效。
- 生成数据表与手写目录分离为两个独立类型，生成脚本仍可重复生成并核对文件。
- 修复独立测试的历史路径、主源码清单、程序集引用和私有字段反射；将位于 Assets 的模式验证项目迁到 Tools/Tests。
- 为当前 StreamingAssets 目录布局和历史 PvZ 子目录布局提供统一解析。

## 已执行的验证

所有主 Unity 验证均使用 **6000.3.17f1**，未通过根目录生成的 .NET 项目构建 Unity 主代码。

| 命令或测试入口 | 结果 |
| --- | --- |
| `./Tools/Tests/Run-UnityTests.ps1 -ReuseProject .unity-runs/d2bbc41c/p` | EditMode 6、PlayMode 11 全部通过，0 失败、0 跳过 |
| EditMode SceneBindingTests | Lawn 场景只有一个有效 PvZGame；所有对象没有缺失脚本；未保存测试场景 |
| PlayMode ObjectSpriteRendererTests | 真实 GPU 像素、Alpha、深度/绘制顺序、变换、二值纹理平滑边缘、不可读纹理、动态 Alpha 分类失效、active RT 恢复、对象复用与释放通过 |
| `dotnet run --project Tools/Tests/ArchitectureTests/ArchitectureTests.csproj -c Release` | 0 违规；NuGet 漏洞数据源暂不可访问产生 NU1900，不影响扫描执行 |
| `./Tools/Tests/ProjectRefactorTests/Run.ps1` | dry run、partial 合并、嵌套类型提取、符号引用/命名同步、GUID 保留、缺元数据的 using 保留及行为回归通过 |
| `dotnet run --project Tools/Tests/RandomTests/RandomTests.csproj -c Release` | 37,702 断言通过 |
| `dotnet run --project Tools/Tests/SunTests/SunTests.csproj -c Release` | 35,754 断言、5,948 机器码用例通过 |
| `dotnet run --project Tools/Tests/BitmapFontTests/BitmapFontTests.csproj -c Release` | 13,749 断言通过 |
| `dotnet run --project Tools/Tests/BoardUiTests/BoardUiTests.csproj -c Release` | 382 断言通过 |
| `dotnet run --project Tools/Tests/LevelOneTests/LevelOneTests.csproj -c Release` | 823 + 231 断言通过 |
| `dotnet run --project Tools/Tests/ModeTests/ModeTests.csproj -c Release` | 112 断言通过 |
| `dotnet run --project Tools/Tests/PlantTimingTests/PlantTimingTests.csproj -c Release` | 70,399 断言、20 机器码场景通过 |
| `dotnet run --project Tools/Tests/WaveTests/WaveTests.csproj -c Release` | 60,579 断言、10,770 原生 oracle 向量通过 |
| `dotnet run --project Tools/Tests/ProfileTests/ProfileTests.csproj -c Release` | 127 断言通过 |
| `dotnet run --project Tools/Tests/ProgressionTests/ProgressionTests.csproj -c Release` | 33 断言通过 |
| `dotnet run --project Tools/Tests/VerifiedDataTests/VerifiedDataTests.csproj -c Release` | 10,231 断言通过 |
| `dotnet run --project Tools/Tests/ProfileDialogTests/ProfileDialogTests.csproj -c Release` | 80 条逻辑断言通过；测试 UI 外观方法使用明确的惰性 fixture |
| `dotnet run --project Tools/Tests/ProfileDialogTests/RealCatalogTests/RealCatalogTests.csproj -c Release -- Assets/StreamingAssets` | 28 条真实目录断言通过 |
| `dotnet run --project Tools/Tests/AlmanacTests/AlmanacTests.csproj -c Release -p:RuntimeAssembly=.unity-runs/d2bbc41c/p/Library/ScriptAssemblies/PlantsVsZombies.Runtime.dll -- Tools/Tests/AlmanacTests/PreparedAssets` | 实际 .NET 执行 392 条逻辑断言通过；引用 Unity 已编译程序集，未运行 native-oracle 或视觉检查 |
| `./Tools/Tests/VerifiedDataTests/GeneratorTests.ps1` | 正常生成核对及 5 个错误输入分支通过 |
| `./Tools/GenerateVerifiedData.ps1 -Check` | 生成数据与原始 JSON 导出一致 |
| `git diff --check` | 通过 |
| 资产元数据检查 | 本次代码/测试/配置/shader 目录无缺失配对、孤立 meta 或重复 GUID；没有生成目录或根 IDE 工程变更 |

可保留的本地 Unity 结果位于忽略目录 `.unity-runs/d2bbc41c`，包括 `summary.json`、NUnit XML 和日志。测试不写回正式场景或 Player Settings。

## 仍存在的限制

- `AdventureTests --unit` 在重构前基线就失败于 `SelectionAndTutorial` 的 `actual first wave starts`；重构后复现同一失败，未修改断言或游戏行为掩盖该问题。
- nullable 已启用，但全纯模拟核心仍有 **183 条 nullable 警告**：Adventure 84、Simulation 73、Progression 16、Modes 6、Waves 4。Profiles、Infrastructure 和 VerifiedData 的本轮边界整理已无 nullable 警告。这不是全项目零警告验收，Unity 层也仍需逐模块审查空值生命周期。
- 原来的界面导航和模拟协调器已合并为单文件，依然较大；本轮没有重写游戏规则或逐一拆出所有业务服务。大多数原有 API 补有 XML 文档，未逐条人工润色整个旧代码库。
- 完整 GUI/Windows Player 交互**未验证**。额外隔离 Player 构建成功，但隐藏窗口没有产生 OnGUI Repaint，最终界面截图检查未通过有效前置条件；不能用离屏 GPU 测试推断整个界面显示已通过。
- Linux、macOS、Android、iOS、ARM64、其他图形 API 及 IL2CPP Player/真机测试**未验证**。
- 完整连续冒险 50 关及图鉴可选的 native-oracle 验证**未运行**。
- 旧 Tools/Tests 下的 UnityProject、integration-project、UnitySmokeProject 是历史复制快照，未整体迁移；当前主工程的正式 Unity 回归入口是 Assets/Tests 与 Run-UnityTests.ps1。
