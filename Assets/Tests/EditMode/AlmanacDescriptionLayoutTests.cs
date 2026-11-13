#nullable enable
using System;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects native Canvas measurement for multiline and Chinese rich almanac descriptions.</summary>
    public sealed class AlmanacDescriptionLayoutTests
    {
        /// <summary>The temporary UTF-16 original string directory owned only by this fixture.</summary>
        private string _root = "";
        /// <summary>The renderer supplied to the native view without starting gameplay or loading saved data.</summary>
        private ReanimationRenderer _renderer = null!;
        /// <summary>The view whose real font and wrapped body style are measured.</summary>
        private AlmanacView _view = null!;
        /// <summary>The actual private body-height operation, inspected without adding a public layout API.</summary>
        private MethodInfo _measure = null!;
        /// <summary>The existing original-token formatter.</summary>
        private MethodInfo _format = null!;
        /// <summary>The real body style also supplied to native Canvas text.</summary>
        private GUIStyle _style = null!;

        /// <summary>Creates a native view using a small original-format UTF-16 source and the real platform font selection.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "PvZAlmanacText-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "properties"));
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"),
                "[PEASHOOTER]\r\n豌豆射手\r\n[PEASHOOTER_DESCRIPTION_HEADER]\r\n简短介绍\r\n"
                + "[PEASHOOTER_DESCRIPTION]\r\n{SHORTLINE}\r\n{KEYWORD}伤害：{STAT}中等\r\n{FLAVOR}完整介绍。\r\n", Encoding.Unicode);
            _renderer = new ReanimationRenderer(_root);
            _view = new AlmanacView(_root, _renderer);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(AlmanacView).GetMethod("EnsureStyles", flags)!.Invoke(_view, null);
            _measure = typeof(AlmanacView).GetMethod("MeasureBodyTextHeight", flags)!;
            _format = typeof(AlmanacView).GetMethod("FormatDescription", BindingFlags.Static | BindingFlags.NonPublic)!;
            _style = (GUIStyle)typeof(AlmanacView).GetField("_bodyStyle", flags)!.GetValue(_view)!;
        }

        /// <summary>Disposes the native layout generator, platform font, and isolated original-string source.</summary>
        [TearDown]
        public void TearDown()
        {
            _view?.Dispose();
            _renderer?.Dispose();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        /// <summary>Gives multiline content a positive complete height instead of a two-pixel clipping rectangle.</summary>
        [Test]
        public void ShortAndMultilineContentHavePositiveCanvasHeights()
        {
            var shortHeight = Measure("Short header", 258f);
            var multilineHeight = Measure("First line\nSecond line\nThird line", 250f);
            Assert.That(shortHeight, Is.GreaterThan(0f));
            Assert.That(multilineHeight, Is.GreaterThan(shortHeight * 2f));
        }

        /// <summary>Retains original Chinese tokens and leading spacing while deriving real scrollable body geometry.</summary>
        [Test]
        public void ChineseRichDescriptionHasHeightAndScrollableContent()
        {
            Assert.That(_view.PlantDisplayName(0), Is.EqualTo("豌豆射手"), "The UTF-16 source must keep its original Chinese text.");
            var raw = new StringBuilder("{SHORTLINE}\n{KEYWORD}伤害：{STAT}中等\n{SHORTLINE}\n{FLAVOR}");
            for (var line = 0; line < 24; line++)
            {
                raw.Append("完整的植物详细介绍，应当参与原生文本换行和滚动布局。\n");
            }

            var formatted = (string)_format.Invoke(null, new object[] { raw.ToString() })!;
            Assert.That(formatted, Does.StartWith("\n"), "Measurement must preserve the original leading short-line spacing.");
            Assert.That(formatted, Does.Contain("伤害："));
            Assert.That(formatted, Does.Contain("<color="));
            var height = Measure(formatted, 250f);
            var region = new Rect(485f, 312f, 258f, 193f);
            Assert.That(height, Is.GreaterThan(region.height));
            Assert.That(_view.HandleClick(new Vector2(208f, 366f)), Is.True);
            SetField("_formattedDescription", formatted);
            SetField("_descriptionHeight", height);
            SetField("_descriptionRect", region);
            Assert.That(_view.DetailPageCount, Is.GreaterThan(1));
            Assert.That(_view.HandleScroll(region.center, 1f), Is.True);
            var offset = (float)typeof(AlmanacView).GetField("_scrollOffset", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_view)!;
            Assert.That(offset, Is.GreaterThan(0f));
            Assert.That(offset, Is.LessThanOrEqualTo(height - region.height));
        }

        /// <summary>Matches Unity UI Text's actual generation settings for the same original rich content and logical width.</summary>
        [Test]
        public void BodyMeasurementMatchesNativeTextLayout()
        {
            var owner = new GameObject("Almanac native text layout oracle", typeof(RectTransform), typeof(Text));
            try
            {
                var text = owner.GetComponent<Text>();
                text.font = _style.font;
                text.fontSize = _style.fontSize;
                text.fontStyle = _style.fontStyle;
                text.alignment = _style.alignment;
                text.supportRichText = _style.richText;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.lineSpacing = 1f;
                text.resizeTextForBestFit = false;
                text.color = _style.normal.textColor;
                text.rectTransform.pivot = new Vector2(0f, 1f);
                const float width = 250f;
                const string value = "\n<color=#873222>伤害：</color><color=#28325A>中等</color>\n<color=#624C42>详细介绍应完整显示。\nSecond line.</color>";
                var settings = text.GetGenerationSettings(new Vector2(width, 0f));
                var expected = Mathf.Ceil(text.cachedTextGeneratorForLayout.GetPreferredHeight(value, settings) / text.pixelsPerUnit);
                Assert.That(expected, Is.GreaterThan(0f));
                Assert.That(Measure(value, width), Is.EqualTo(expected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        /// <summary>Invokes the real native body measurement without drawing scene assets.</summary>
        /// <param name="value">The original plain or formatted rich content.</param>
        /// <param name="width">The logical width used by the real body label.</param>
        /// <returns>The preferred height used by the almanac's clipping and scrolling logic.</returns>
        private float Measure(string value, float width)
        {
            return (float)_measure.Invoke(_view, new object[] { value, width })!;
        }

        /// <summary>Sets only retained layout state to exercise the existing public scrolling behavior.</summary>
        /// <param name="name">The existing private layout field.</param>
        /// <param name="value">The measured native layout value.</param>
        private void SetField(string name, object value)
        {
            typeof(AlmanacView).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_view, value);
        }
    }
}
