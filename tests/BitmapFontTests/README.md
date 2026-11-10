# Bitmap Font Tests

从工作区运行：

```powershell
dotnet run --project .\PvZUnity\tests\BitmapFontTests\BitmapFontTests.csproj --configuration Release
& .\PvZUnity\Tools\PrepareBitmapFonts.ps1
& .\PvZUnity\tests\BitmapFontTests\RunUnitySmoke.ps1
```

独立测试读取全部 25 层、逐项比较三套原始中文字体，验证 kerning、多页和异常输入。Unity 脚本需要 Windows、Unity 6000.3.17f1 的 Windows 构建支持和可用图形设备；可用 `-Unity` 指定编辑器路径。它在 `.unity-runs/<随机 ID>` 中复制并编译运行时源码，在 `artifacts/<随机 ID>` 写日志、截图和结果，不打开或修改主项目。

Player 从隐藏窗口启动，然后只将自己的窗口移出桌面并以非激活状态显示，以便 Unity 触发真正的 IMGUI Repaint。测试从 Player 截图回读像素，按原 PNG 的透明度和指定颜色计算预期值。可删除此测试目录下生成的 `.unity-runs`、`artifacts`、`bin`、`obj`；它们不是运行时输入。
