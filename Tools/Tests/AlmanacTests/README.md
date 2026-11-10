# 图鉴实现与验证

当前 `AlmanacView` 已接入主菜单和战斗设置。它使用本机原包素材、`VerifiedDataCatalog` 和 `ReanimationRenderer`，处理 49 个植物及 26 个僵尸图鉴 ID；这不表示对应战斗单位已经实现。

## 交互

- 主页、索引和关闭按钮在一次有效的鼠标按下/释放后执行；拖出按钮释放会取消。
- 条目在按下时选中。返回首页再切入分类会保留各分类的选择；重新打开整个图鉴会重置选择。
- 重复选择同一条目不重置说明滚动或预览动画；切换条目会重置。
- 描述支持滚轮、轨道点击及滚动条拖拽；拖动期间光标离开说明框仍保持捕获，释放后结束。
- 从战斗设置打开图鉴，关闭后恢复设置草稿，战斗继续暂停；从主菜单进入则返回主菜单。
- 真实宿主转发 `HandlePointer(point, type)`，坐标先转换为逻辑 800×600。`HandleClick(point)` 是测试或其他宿主的一次完整点击便捷接口。

## 大小、位置和动画

`AlmanacPortraitLayout.cs` 维护已导出的原版锚点、卡片静态图集索引、缩略图比例和裁剪范围。`ReanimationRenderer.DrawNative` 按原始像素绘制，不根据每个动画的包围框重新归一化大小。

- 普通与升级植物使用 `seeds.png` 的不同底图，并显示阳光价格。13 种特殊卡片直接读取原版 `packet_plants.png`。
- 其余植物缩略图使用原版分支的独立比例和偏移，保持静态；大型预览保持原始比例，并单独处理高坚果、玉米加农炮、花盆、咖啡豆等原点。
- 机枪射手在身体上附着头部、枪管和头盔；三重射手和裂荚射手合成各自的头部轨道。
- 僵尸缩略图按半尺寸缓存图的原点和独立偏移绘制，裁剪到窗口内部；预览按原版固定坐标和 `197×187` 裁剪范围绘制。
- 僵王博士使用 `anim_head_idle`、独立 reanim 93 驾驶员及前后遮挡顺序。缩略图缓存与动画预览有不同的驾驶员放置规则。
- 离屏 GL 绘制使用独立矩阵，并将部件变换只应用一次；不再把部件矩阵同时交给 IMGUI 和 GL，避免全部僵尸预览出现头身分离、偏移或被裁掉。带矩阵变换的僵尸图层先渲染到受限的离屏画布，再作为单张图片显示，以避免 IMGUI 嵌套组的变换裁剪失效。带独立 PNG 蒙版的 JPG 部件先合成为 RGBA32，避免 RGB24 丢失透明度。所需着色器位于 `Assets/Resources/Shaders/PvZNativePortrait.shader`，由 Resources 明确打包。

- 旗帜僵尸的静态缩略图先在身体原点绘制旗杆，再绘身体；动画预览使用持旗手相对第 0 帧的变换，避免旗杆重复旋转/平移。小鬼明确选择 `anim_walk`，不再误用文件中排在首位的 `anim_land`。

## 来源与限制

主要证据为本目录 `ida-evidence.json` 和 `analysis/ui_validation/20261109-almanac/native-draw/` 中从数据库副本导出的函数。`0x40CD90/0x40CE50` 区分导航和选择事件，`0x40B430` 保留分类选择；`0x409F60/0x473490` 定义植物实例和高度，`0x40BF40/0x47ECE0` 定义僵尸窗口与缓存，`0x49D260/0x49CEE0` 定义卡片比例和静态图集，`0x552610` 附着僵王驾驶员。

默认解锁集合仍为全部条目，尚未接入真实原版进度、购买与雪人发现字段。原版位图字体、全部特殊效果/影子/粒子、动画随机相位以及动态排版仍未完整复刻。当前没有本机原版同场景截图，不能据此宣称逐像素一致。静态原点测试也不验证完整原版动画状态。

## 验证

当前 .NET 测试引用 Unity 6000.3.17f1 已编译的 `PlantsVsZombies.Runtime.dll`，不通过普通 dotnet 重编译 URP 主工程。默认读取主项目 `Library/ScriptAssemblies`；也可使用新隔离测试运行生成的程序集：

```powershell
dotnet run --project Tools/Tests/AlmanacTests/AlmanacTests.csproj -c Release -p:RuntimeAssembly="D:/path/to/PvZUnity/.unity-runs/<run>/p/Library/ScriptAssemblies/PlantsVsZombies.Runtime.dll" -- Tools/Tests/AlmanacTests/PreparedAssets
```

这验证逻辑与数据，不证明渲染效果。当前渲染回归请运行 `Tools/Tests/Run-UnityTests.ps1`；本目录旧 `UnityProject` 和 `RunUnitySmoke.ps1` 保存历史验证快照，未迁移为当前 URP 主项目测试入口。

在 `PvZUnity/` 执行：

```powershell
& .\Tools\PrepareAlmanacAssets.ps1
& .\Tools\PrepareAlmanacAssets.ps1 -Destination .\Tools\Tests\AlmanacTests\PreparedAssets
dotnet run --project .\Tools\Tests\AlmanacTests\AlmanacTests.csproj -c Release -- .\Tools\Tests\AlmanacTests\PreparedAssets
dotnet run --project .\Tools\Tests\AlmanacTests\AlmanacTests.csproj -c Release -- .\Tools\Tests\AlmanacTests\PreparedAssets --native-oracle
& .\Tools\Tests\PlayerSmokeTests\Run.ps1 -Mode UI
& .\Tools\Tests\AlmanacTests\RunUnitySmoke.ps1
```

`--native-oracle` 使用已有 `Ubuntu-24.04` WSL / Unicorn 依赖，仅执行原 EXE `0x40CAA0/0x40CCF0` 的局部命中指令，对照边界、锁定集合和首页状态。可用性函数明确被桩替换，**不验证解锁规则、原版完整 UI 或渲染**。

.NET 检查覆盖中文文案、75 个 ID、分类记忆、按下/释放、取消、滚动、原点、静态图集选择和非法输入。Player UI 冒烟逐项选择全部条目，资源/绘制诊断非空即失败；包含高坚果、三重射手、咖啡豆、机枪射手和僵王博士专项截图，以及战斗暂停/返回设置检查。姓名输入和点击由程序调用，不能代替真实 OS 输入验证。

独立 Unity 工程只复制图鉴、布局、渲染器、静态数据和专用着色器，可在主工程关卡代码开发期间单独编译验证。旧隐藏窗口黑屏和旧原点/裁剪失败的产物保留在原目录；它们不被新运行覆盖，也不算通过。

2026-11-09 预览回归：独立可见 Player 为全部 26 种僵尸各保存 0 / 0.75 秒两张截图，共 62 张全界面 PNG，同时读取预览及缩略图 GPU 画布。`verify_portraits.py` 通过 Pillow/NumPy 独立解析源 reanim 并栅格化普通、旗帜、小鬼的两时刻预览/缩略图；12 项比较同时约束轮廓位置和不透明区域颜色，不接受仅非黑屏的结果。此校验需要 Python、Pillow 和 NumPy；它验证移植渲染几何，不代替原版截图对照。主程序新增旗帜/小鬼专项截图，并在 800×600、1280×720 下验证。
