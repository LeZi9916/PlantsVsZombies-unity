#nullable enable
using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects shared logical artwork ownership across independent menu and modal views.</summary>
    public sealed class UiSpriteImageTests
    {
        /// <summary>The unique ignored runtime directory containing small synthetic menu images.</summary>
        private string _root = "";

        /// <summary>Creates only the original filenames required by the view constructors.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Application.temporaryCachePath, "PvZUiImages-" + Guid.NewGuid().ToString("N"));
            var images = Directory.CreateDirectory(Path.Combine(_root, "images")).FullName;
            Directory.CreateDirectory(Path.Combine(_root, "properties"));
            File.WriteAllText(Path.Combine(_root, "properties", "LawnStrings.txt"), "[MAIN_MENU_BUTTON]\r\nReturn\r\n");
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            source.Apply();
            var bytes = source.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(source);
            foreach (var name in new[]
            {
                "options_menuback.jpg", "options_menuback_.png", "options_sliderslot.png", "options_sliderknob2.png",
                "options_checkbox0.png", "options_checkbox1.png", "options_backtogamebutton0.png", "options_backtogamebutton2.png",
                "button_left.png", "button_middle.png", "button_right.png", "button_down_left.png", "button_down_middle.png",
                "button_down_right.png", "background1.jpg", "ZombieNote.jpg", "ZombieNote_.png", "ZombieNoteHelpBlack.png",
                "ZombieNoteHelp.png", "SeedChooser_Button.png", "SeedChooser_Button_Glow.png", "dialog_topleft.png",
                "dialog_topmiddle.png", "dialog_topright.png", "dialog_centerleft.png", "dialog_centermiddle.png",
                "dialog_centerright.png", "dialog_bigbottomleft.png", "dialog_bigbottommiddle.png", "dialog_bigbottomright.png",
                "dialog_header.png", "editbox.png", "selector_nomask.jpg"
            })
            {
                File.WriteAllBytes(Path.Combine(images, name), bytes);
            }
        }

        /// <summary>Removes only this fixture's generated resources after every acquisition has been released.</summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        /// <summary>Shares artwork and preserves live views when another view releases its provider acquisition.</summary>
        [Test]
        public void IndependentViewsShareProviderWithoutDestroyingEachOthersImages()
        {
            using var provider = SpriteAssetProvider.ForRoot(_root);
            using var animations = new ReanimationRenderer(_root);
            using var options = new OptionsView(_root);
            using var help = new HelpView(_root);
            using var selector = new SelectorView(_root);
            using var almanac = new AlmanacView(_root, animations);
            using var profiles = new ProfileDialog(_root, new ProfileCatalog(Path.Combine(_root, "unused-profiles.dat")));
            foreach (var view in new object[] { options, help, selector, almanac, profiles })
            {
                Assert.That(ReadField(view, "_assets"), Is.SameAs(provider));
            }

            var optionsBackground = provider.LoadAlphaGrid("options_menuback.jpg", "options_menuback_.png");
            Assert.That(ReadField(options, "_background"), Is.SameAs(optionsBackground));
            Assert.That(ReadField(help, "_writing"), Is.SameAs(provider.LoadAlphaGrid("ZombieNoteHelpBlack.png", "ZombieNoteHelp.png")));
            var button = provider.LoadImage("button_left.png");
            options.Dispose();
            Assert.That(provider.LoadImage("button_left.png"), Is.SameAs(button));
            Assert.That(provider.LoadAlphaGrid("ZombieNoteHelpBlack.png", "ZombieNoteHelp.png").Width, Is.EqualTo(2));
            help.Dispose();
            Assert.That(provider.LoadImage("button_left.png"), Is.SameAs(button));
        }

        /// <summary>Prevents a previously unmasked lookup from bypassing a later mandatory-alpha request.</summary>
        [Test]
        public void SelectorRequiresMaskEvenAfterAnUnmaskedImageWasCached()
        {
            using var selector = new SelectorView(_root);
            var lookup = typeof(SelectorView).GetMethod("GetTexture", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var plain = (SpriteImage?)lookup.Invoke(selector, new object[] { "images/selector_nomask", false });
            Assert.That(plain, Is.Not.Null);
            LogAssert.Expect(LogType.Warning, new Regex("SelectorView.*selector_nomask"));
            var masked = lookup.Invoke(selector, new object[] { "images/selector_nomask", true });
            Assert.That(masked, Is.Null);
        }

        /// <summary>Reads a view's retained provider or logical handle without adding a production diagnostics API.</summary>
        /// <param name="target">The view owning the field.</param>
        /// <param name="name">The existing private field name.</param>
        /// <returns>The retained provider or logical image value.</returns>
        private static object? ReadField(object target, string name)
        {
            return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target);
        }
    }
}
