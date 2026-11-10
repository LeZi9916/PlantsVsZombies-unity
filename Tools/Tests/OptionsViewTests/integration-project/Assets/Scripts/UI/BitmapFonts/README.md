# BitmapFonts 使用说明

本目录提供独立的 BMFont 文本解析、排版和 Unity IMGUI 绘制，不修改或依赖现有 UI、其他移植项目的兼容层。数据来自工作区 `extracted_assets/converted/fonts`，不是对原版 ImageFont 渲染器的完整复刻；差异和证据见 `analysis/ida/fonts/README.md`。

## 准备资源

在 `PvZUnity` 目录执行：

```powershell
& .\Tools\PrepareBitmapFonts.ps1
```

脚本将所有转换后的 `.fnt` 和 PNG 原样复制为此目录 `Resources/PvZBitmapFonts/Descriptions/*.bytes` 与 `Atlases/*.bytes`。脚本可重复运行；若目标文件存在但内容不同，会拒绝覆盖。Unity 在运行时以严格 UTF-8 解析描述，并以 PNG 解码字图，保留其实际尺寸，使用 Point 采样、Clamp、无 mipmap。不要手工把 `.bytes` 改为会被 Unity 压缩或缩放的纹理资源。

## API

```csharp
using PvZReplica.UI.BitmapFonts;
using UnityEngine;

// 在组件中持有，切勿每次 OnGUI 都加载图集。
private ImGuiBitmapFont font;
private BitmapTextLayout caption;

private void Awake()
{
    font = ImGuiBitmapFont.LoadFromResources("DwarvenTodcraft18");
    caption = font.Layout("植物大战僵尸\n中文", scale: 1f, maxWidth: 220f);
    caption.ThrowIfMissing();
}

private void OnGUI()
{
    font.Draw(caption, 100f, 80f, Color.white);
    // 动态文字也可直接绘制，返回排版结果：
    // BitmapTextLayout result = font.Draw("中文", 100f, 120f, Color.white, 2f);
}

private void OnDestroy()
{
    if (font != null) font.Dispose();
}
```

`DwarvenTodcraft12`、`DwarvenTodcraft15`、`DwarvenTodcraft18` 等裸名称会解析到 `__Main`；也可显式加载 `BrianneTod32__Outline` 等转换后的单层名称。每个实例持有自己解码的纹理，必须 `Dispose`；相同字体建议重复使用实例和不变的 `BitmapTextLayout`。所有 Unity 加载、绘制及销毁操作应在主线程执行。

`Draw` 的 `(x, y)` 是首行**左上行原点**，不是可见像素边界，也不是原版基线。字形矩形按 `xoffset/yoffset` 相对于此点定位；它们可能为负。缩放和 `maxWidth` 使用当前 GUI 坐标单位，并与现有 `GUI.matrix` 一起生效。仅 `Repaint` 事件提交纹理；所有事件都会检查缺字。颜色与原有 `GUI.color` 相乘，绘制后恢复原值。

`BitmapTextLayout` 提供缩放后的 `Width`（各行最大进位宽度）、`Height`、`LineCount`、`LineWidths`、`Glyphs` 与 `MissingGlyphs`。`MinX/MinY/MaxX/MaxY` 是非空**字图矩形**并集，包含图块内透明边缘，不是精确墨迹边界。`font.Data` 提供 `LineHeight`、`Base`、字图尺寸，以及 `TryGetGlyph(codePoint, out glyph)` 和 `GetKerning(first, second)`；单个字形的度量保持 BMFont 原始整数单位，未乘缩放。

缺字不会静默回退：`Layout` 收集 Unicode 码点和 UTF-16 下标，`Draw` 默认抛出 `MissingBitmapGlyphException`，其中也有完整列表。只有调用者显式设置 `allowMissing: true` 时才跳过缺字，跳过的字符不产生进位，也不与邻字产生 kerning。未配对的 UTF-16 surrogate 直接报错。`\n`、`\r`、`\r\n` 表示换行；提供有限的正 `maxWidth` 时按字形逐个换行，不做按词断行、中文禁则或空格裁剪，单个超宽字形仍会占一行。空字符串占一行；非正缩放/宽度被拒绝。

支持 Unicode BMFont **文本格式**、多页 PNG、`chnl=15` 的非 packed 字图和 kerning；不支持 BMFont XML/二进制、分通道 packed 字图、字体塑形或原版字体指令解释。读取错误、缺页、图集尺寸不匹配及未知记录均明确抛错。
