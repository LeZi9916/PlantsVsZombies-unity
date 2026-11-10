# 主菜单帮助页

这是独立的 `PlantsVsZombies.UI.HelpView`，没有修改主场景，也没有自动接入选择器。
它只实现本地 EXE 的主菜单帮助变体：一张纸条和一个返回主菜单按钮，
没有发现需要实现的翻页按钮。`PageCount` 固定为 1。

## 接入

在 Unity 主线程创建 `new HelpView(resourceRoot)`，再调用 `Open()`。
初始状态为关闭；重复打开会抛异常。关闭后可以再次打开，新的 `Open()`
会清除上次的 `CloseResult` 和鼠标捕获。离开时在 Unity 主线程 `Dispose()`；
重复释放无害，释放后的操作方法会抛 `ObjectDisposedException`。

表现层在帧更新后调用 `Draw()`，把图像和文字提交到 Canvas 的 800×600
逻辑坐标。`Draw()` 不读输入事件、不导航；关闭时不绘制。素材必须是可通过
文件系统读取的真实文件，不直接支持打包在 APK 或 URL 中的 StreamingAssets。
打开时，将转换为同一逻辑空间的坐标传给
`HandlePointer(position, phase)`：鼠标移动以及**主键**按下、拖动、释放
分别使用 `PointerPhase.Move`、`Down`、`Drag`、`Up`。按下必须从按钮
内开始，释放也在按钮内才返回；拖出后释放取消。即使坐标在画布外，也应继续
转发拖动和释放。上述事件在打开时均被消费；宿主应阻止这些事件和其他鼠标、
滚轮、键盘事件落到下层界面。鼠标事件类型与按钮号应由宿主筛选，
`Draw()` 不会二次处理事件。

将按键按下事件交给 `HandleKey(key)`：Escape、Return、Space 返回，
KeypadEnter 也是本实现的便利扩展。宿主也可以调用 `Close()`。当
`IsOpen` 变为 false 且 `CloseResult == HelpCloseResult.ReturnToMainMenu`，
宿主应只处理一次返回结果并切换回选择器。`Close()` 不直接切换场景。
`ReturnButtonBounds`、`IsReturnHovered`、`IsReturnPressed` 可供宿主检查。

宿主的输入路由器在每帧独立采样鼠标或触摸，并优先转发到当前弹窗。
帮助页关闭的那次输入也必须被消费，避免同一按下或释放触发新露出的主菜单。
绘制由表现层单独完成，不需要 `OnGUI`、`Event.current` 或 GUI 矩阵。
## 素材

开发时可将 `resourceRoot` 设为本工作区 `extracted_assets/raw`。部署时保持
下面的相对路径，按需从本地解包素材复制。不会修改共用准备脚本。

```text
images/background1.jpg
images/ZombieNote.jpg
images/ZombieNote_.png
images/ZombieNoteHelpBlack.png
images/ZombieNoteHelp.png
images/SeedChooser_Button.png
images/SeedChooser_Button_Glow.png
properties/LawnStrings.txt
```

当前 `PvZUnity/Assets/StreamingAssets/PvZ` 缺少四个 `ZombieNote*` 文件，其余
四项目前存在。纸张 JPEG 与 `ZombieNote_.png`、黑色文字图与
`ZombieNoteHelp.png` 分别按灰度蒙版合成 RGBA；中文返回文字来自
`[MAIN_MENU_BUTTON]`。缺失、损坏或尺寸不符会抛异常，不会静默替换资源。
返回按钮还需要系统提供 CJK 字体；字体、字号和字基线不是原版位图字体。

## 验证与限制

在工作区根目录运行：

```powershell
& .\PvZUnity\tests\HelpTests\run.ps1 -SkipPlayer  # Unity 编辑器交互测试
& .\PvZUnity\tests\HelpTests\run.ps1              # 额外构建并运行截图冒烟
```

可用 `-Unity <Unity.exe>` 指定 Unity 6000.3.17f1。测试项目只在本目录内
复制当前 `HelpView.cs` 并生成中间文件。编辑器测试使用真实 Unity 图像解码，
检查蒙版像素、关闭/重开、命中边界、捕获、拖拽取消、键盘、异常和释放。
播放器冒烟会尝试保存正常和悬停截图，检查非空图像及按钮像素差异；
只有日志中的 `HELP_SMOKE_PASS` 才表示该检查成功。

本机实际结果：编辑器测试通过（15,181 次断言，其中 15,137 次为逐像素
黑色墨迹检查）；独立播放器构建通过。隐藏窗口运行时得到全黑截图，
`HELP_SMOKE_FAIL colors=1 hoverPixels=0`，因此实际 Unity 绘制、悬停视觉和
本机显示效果**尚未通过截图验证**。`help-reference.png` 是
`compose_reference.py` 用原始素材、蒙版和 IDA 坐标独立合成的 800x600
参考图；它不是 Unity 截图，也不是与原版的视觉对照。

IDA 证据与函数导出见 `analysis/ida/help_ui/README.md`。此实现不是完整复刻，
也未证明像素一致。没有验证原 EXE 与 Unity 的逐像素截图、原版字体和
文字基线、悬停叠加模式、音效、光标、进入/退出淡入淡出、不同 DPI 或实际
主工程场景集成。宿主必须自行决定导航并接入声音；现有 `PvZGame` 仍未接入。
