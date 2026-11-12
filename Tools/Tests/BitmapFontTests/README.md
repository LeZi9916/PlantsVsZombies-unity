# Bitmap Font Tests

从仓库根目录运行纯解析与排版验证：

```powershell
dotnet run --project Tools/Tests/BitmapFontTests/BitmapFontTests.csproj -c Release
./Tools/Tests/Run-UnityTests.ps1 -Platform PlayMode -TestFilter CanvasPresentationTests
```

独立测试读取全部 25 层、逐项比较三套原始中文字体，验证 kerning、多页和异常输入。
当前运行时是 `CanvasBitmapFont`，字形由原生 CanvasRenderer 显示，不依赖 GUI 事件。
主工程的 `CanvasPresentationTests` 通过 Unity 6000.3.17f1 和实际图形设备检查 Canvas 图层、
裁剪、复用、原生文本控件及位图字形；隔离运行目录由主工程测试脚本管理。

本目录的 `UnitySmokeProject` 和 `RunUnitySmoke.ps1` 是历史 IMGUI 验证快照，
不是当前 Canvas 运行时的测试入口。保留历史结果不表示这些快照已迁移或重新通过。
