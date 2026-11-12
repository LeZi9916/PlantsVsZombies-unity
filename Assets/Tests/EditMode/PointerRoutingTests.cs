#nullable enable

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Inputs;
using PlantsVsZombies.UI;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks logical viewport mapping and modal pointer capture without GUI events or original assets.</summary>
    public sealed class PointerRoutingTests
    {
        /// <summary>The maximum float rounding error accepted in logical pixels and viewport coordinates.</summary>
        private const float CoordinateTolerance = .0001f;

        /// <summary>The isolated temporary resource directory owned by this fixture.</summary>
        private string _resourceRoot = null!;

        /// <summary>Creates small decoded textures for the real modal view constructors.</summary>
        [OneTimeSetUp]
        public void CreateResources()
        {
            _resourceRoot = Path.Combine(Path.GetTempPath(), "PvZPointerRouting-" + Guid.NewGuid().ToString("N"));
            var images = Path.Combine(_resourceRoot, "images");
            var properties = Path.Combine(_resourceRoot, "properties");
            Directory.CreateDirectory(images);
            Directory.CreateDirectory(properties);
            File.WriteAllText(Path.Combine(properties, "LawnStrings.txt"), "[MAIN_MENU_BUTTON]\nMain menu\n");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texture.Apply();
                var pixels = texture.EncodeToPNG();
                var names = new[]
                {
                    "background1.jpg",
                    "ZombieNote.jpg",
                    "ZombieNote_.png",
                    "ZombieNoteHelpBlack.png",
                    "ZombieNoteHelp.png",
                    "SeedChooser_Button.png",
                    "SeedChooser_Button_Glow.png",
                    "options_menuback.jpg",
                    "options_menuback_.png",
                    "options_sliderslot.png",
                    "options_sliderknob2.png",
                    "options_checkbox0.png",
                    "options_checkbox1.png",
                    "options_backtogamebutton0.png",
                    "options_backtogamebutton2.png",
                    "button_left.png",
                    "button_middle.png",
                    "button_right.png"
                };
                foreach (var name in names)
                {
                    File.WriteAllBytes(Path.Combine(images, name), pixels);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Removes only the unique temporary resource directory created by this fixture.</summary>
        [OneTimeTearDown]
        public void DeleteResources()
        {
            if (_resourceRoot != null && Directory.Exists(_resourceRoot))
            {
                Directory.Delete(_resourceRoot, true);
            }
        }

        /// <summary>Preserves aspect ratio and centers the viewport on wide, tall, and scaled displays.</summary>
        /// <param name="width">The physical screen width.</param>
        /// <param name="height">The physical screen height.</param>
        /// <param name="x">The expected left inset.</param>
        /// <param name="y">The expected bottom inset.</param>
        /// <param name="viewportWidth">The expected playable screen width.</param>
        /// <param name="viewportHeight">The expected playable screen height.</param>
        [TestCase(800f, 600f, 0f, 0f, 800f, 600f)]
        [TestCase(1280f, 720f, 160f, 0f, 960f, 720f)]
        [TestCase(600f, 1000f, 0f, 275f, 600f, 450f)]
        [TestCase(1600f, 1200f, 0f, 0f, 1600f, 1200f)]
        public void CentersTheLogicalViewport(float width, float height, float x, float y, float viewportWidth, float viewportHeight)
        {
            var viewport = GameViewport.CalculateRect(width, height);
            Assert.That(viewport.x, Is.EqualTo(x).Within(CoordinateTolerance));
            Assert.That(viewport.y, Is.EqualTo(y).Within(CoordinateTolerance));
            Assert.That(viewport.width, Is.EqualTo(viewportWidth).Within(CoordinateTolerance));
            Assert.That(viewport.height, Is.EqualTo(viewportHeight).Within(CoordinateTolerance));
            AssertLogicalPosition(GameViewport.ScreenToLogical(new Vector2(width * .5f, height * .5f), width, height), new Vector2(400f, 300f));
        }

        /// <summary>Converts Unity's upward screen axis and rejects clicks in either wide-screen margin.</summary>
        [Test]
        public void KeepsLetterboxPixelsOutsideTheBoard()
        {
            var logicalBounds = new Rect(0f, 0f, GameViewport.Width, GameViewport.Height);
            AssertLogicalPosition(GameViewport.ScreenToLogical(new Vector2(160f, 720f), 1280f, 720f), Vector2.zero);
            AssertLogicalPosition(GameViewport.ScreenToLogical(new Vector2(1120f, 0f), 1280f, 720f), new Vector2(800f, 600f));
            Assert.That(logicalBounds.Contains(GameViewport.ScreenToLogical(new Vector2(159f, 360f), 1280f, 720f)), Is.False);
            Assert.That(logicalBounds.Contains(GameViewport.ScreenToLogical(new Vector2(1121f, 360f), 1280f, 720f)), Is.False);
            Assert.That(logicalBounds.Contains(GameViewport.ScreenToLogical(new Vector2(640f, 360f), 1280f, 720f)), Is.True);
        }

        /// <summary>Rejects a minimized viewport without dividing by a zero scale.</summary>
        [Test]
        public void RejectsZeroSizedScreens()
        {
            Assert.That(GameViewport.ScreenToLogical(Vector2.zero, 0f, 0f), Is.EqualTo(Vector2.negativeInfinity));
        }

        /// <summary>Produces pointer transitions from frame state without a GUI event type.</summary>
        /// <param name="pressed">Whether the primary pointer was pressed this frame.</param>
        /// <param name="released">Whether the primary pointer was released this frame.</param>
        /// <param name="held">Whether the primary pointer is still held.</param>
        /// <param name="expected">The transition dispatched to modal views.</param>
        [TestCase(false, false, false, PointerPhase.Move)]
        [TestCase(true, false, true, PointerPhase.Down)]
        [TestCase(false, false, true, PointerPhase.Drag)]
        [TestCase(false, true, false, PointerPhase.Up)]
        public void DerivesPointerPhaseFromSnapshot(bool pressed, bool released, bool held, PointerPhase expected)
        {
            var snapshot = new GameInputSnapshot(new Vector2(25f, 30f), true, pressed, released, held, false, 0f, false, false);
            Assert.That(snapshot.Phase, Is.EqualTo(expected));
            Assert.That(snapshot.Position, Is.EqualTo(new Vector2(25f, 30f)));
            Assert.That(snapshot.InsideViewport, Is.True);
            Assert.That(snapshot.PhaseCount, Is.EqualTo(1));
            Assert.That(snapshot.GetPhase(0), Is.EqualTo(expected));
        }

        /// <summary>Retains both transitions when a complete click occurs between two game updates.</summary>
        [Test]
        public void PreservesBothTransitionsInASameFrameClick()
        {
            var snapshot = new GameInputSnapshot(new Vector2(400f, 480f), true, true, true, false, false, 0f, false, false);
            Assert.That(snapshot.PhaseCount, Is.EqualTo(2));
            Assert.That(snapshot.GetPhase(0), Is.EqualTo(PointerPhase.Down));
            Assert.That(snapshot.GetPhase(1), Is.EqualTo(PointerPhase.Up));
            Assert.That(snapshot.Phase, Is.EqualTo(PointerPhase.Down));
            Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.GetPhase(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.GetPhase(2));
        }

        /// <summary>Delivers a complete sampled click to its original modal without clicking the exposed selector.</summary>
        /// <param name="optionsModal">Whether options or help owns the input at the start of the frame.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void SameFrameClickClosesOnlyItsOriginalModal(bool optionsModal)
        {
            using var selector = new SelectorView(_resourceRoot);
            using var options = new OptionsView(_resourceRoot);
            using var help = new HelpView(_resourceRoot);
            var host = new GameObject("Inactive pointer routing fixture");
            host.SetActive(false);
            var controller = host.AddComponent<PvZGame>();
            var previousInput = GameInput.Current;
            var previousPointer = CanvasUI.PointerPosition;
            try
            {
                SetCoordinatorField(controller, "_selectorView", selector);
                SetCoordinatorField(controller, "_optionsView", options);
                SetCoordinatorField(controller, "_helpView", help);
                SetCoordinatorField(controller, "_smokeMode", true);
                SetCoordinatorField(controller, "_musicSource", host.AddComponent<AudioSource>());
                SetCoordinatorField(controller, "_audioSource", host.AddComponent<AudioSource>());
                SetCoordinatorField(controller, "_crazyDaveVoice", host.AddComponent<AudioSource>());
                var presenterType = typeof(PvZGame).Assembly.GetType("PlantsVsZombies.Game.GamePresenter", true)!;
                var presenter = Activator.CreateInstance(presenterType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { controller }, null)!;
                SetCoordinatorField(controller, "_presenter", presenter);
                var viewField = typeof(PvZGame).GetField("_currentView", BindingFlags.Instance | BindingFlags.NonPublic)!;
                Vector2 point;
                if (optionsModal)
                {
                    options.Open(.35f, .7f, false);
                    point = new Vector2(400f, 480f);
                }
                else
                {
                    help.Open();
                    viewField.SetValue(controller, Enum.Parse(viewField.FieldType, "Help"));
                    point = HelpView.ReturnButtonBounds.center;
                }

                var snapshot = new GameInputSnapshot(point, true, true, true, false, false, 0f, false, false);
                SetInputSnapshot(snapshot);
                var router = typeof(PvZGame).GetProperty("Router", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(controller)!;
                var route = router.GetType().GetMethod("HandleInput", BindingFlags.Instance | BindingFlags.NonPublic)!;
                Assert.DoesNotThrow(() => route.Invoke(router, null));
                Assert.That(optionsModal ? options.IsOpen : help.IsOpen, Is.False, "Both sampled transitions must reach the original modal.");
                Assert.That(viewField.GetValue(controller)!.ToString(), Is.EqualTo("Selector"), "The exposed selector must not receive the same click.");
                Assert.That(typeof(PvZGame).GetField("_almanacView", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(controller), Is.Null);
            }
            finally
            {
                SetInputSnapshot(previousInput);
                CanvasUI.PointerPosition = previousPointer;
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        /// <summary>Requires the help return press and release to begin and end within the button.</summary>
        [Test]
        public void HelpCancelsAnOutsideReleaseAndConsumesItsClosingRelease()
        {
            using var help = new HelpView(_resourceRoot);
            help.Open();
            var inside = HelpView.ReturnButtonBounds.center;
            var outside = new Vector2(900f, 900f);
            Assert.That(help.HandlePointer(inside, PointerPhase.Up), Is.True);
            Assert.That(help.IsOpen, Is.True, "A release without a captured press cannot activate the button.");
            Assert.That(help.HandlePointer(inside, PointerPhase.Down), Is.True);
            Assert.That(help.IsReturnPressed, Is.True);
            Assert.That(help.HandlePointer(outside, PointerPhase.Drag), Is.True);
            Assert.That(help.IsReturnPressed, Is.False);
            Assert.That(help.HandlePointer(outside, PointerPhase.Up), Is.True);
            Assert.That(help.IsOpen, Is.True);
            help.HandlePointer(outside, PointerPhase.Down);
            help.HandlePointer(inside, PointerPhase.Up);
            Assert.That(help.IsOpen, Is.True, "Releasing over a button cannot capture an earlier outside press.");
            help.HandlePointer(inside, PointerPhase.Down);
            Assert.That(help.HandlePointer(inside, PointerPhase.Up), Is.True, "The closing release belongs to the modal.");
            Assert.That(help.IsOpen, Is.False);
            Assert.That(help.CloseResult, Is.EqualTo(HelpCloseResult.ReturnToMainMenu));
            Assert.That(help.HandlePointer(inside, PointerPhase.Down), Is.False);
        }

        /// <summary>Clears help capture when closing and reopening through a different navigation path.</summary>
        [Test]
        public void ReopeningHelpDoesNotRetainAStalePress()
        {
            using var help = new HelpView(_resourceRoot);
            var inside = HelpView.ReturnButtonBounds.center;
            help.Open();
            help.HandlePointer(inside, PointerPhase.Down);
            help.Close();
            help.Open();
            help.HandlePointer(inside, PointerPhase.Up);
            Assert.That(help.IsOpen, Is.True);
            Assert.That(help.IsReturnPressed, Is.False);
            Assert.That(help.CloseResult, Is.EqualTo(HelpCloseResult.None));
        }

        /// <summary>Keeps an options slider captured outside its bounds and stops dragging after release.</summary>
        [Test]
        public void OptionsSliderCapturesOutsideTheViewportUntilRelease()
        {
            using var options = new OptionsView(_resourceRoot);
            options.GameplayButtonsEnabled = true;
            options.Open(.5f, .25f, false);
            Assert.That(options.HandlePointer(new Vector2(450f, 180f), PointerPhase.Down), Is.True);
            Assert.That(options.HandlePointer(new Vector2(1000f, 900f), PointerPhase.Drag), Is.True);
            Assert.That(options.MusicVolume, Is.EqualTo(1f));
            Assert.That(options.SoundVolume, Is.EqualTo(.25f));
            Assert.That(options.HandlePointer(new Vector2(1000f, 900f), PointerPhase.Up), Is.True);
            options.HandlePointer(new Vector2(-100f, -100f), PointerPhase.Drag);
            Assert.That(options.MusicVolume, Is.EqualTo(1f), "An outside release must end slider capture.");
            Assert.That(options.IsOpen, Is.True);
        }

        /// <summary>Consumes all supported modal pointer phases and cancels a return-button drag outside.</summary>
        [Test]
        public void OptionsConsumesClosingInputAndCancelsOutsideRelease()
        {
            using var options = new OptionsView(_resourceRoot);
            options.Open(.35f, .7f, false);
            var inside = new Vector2(400f, 480f);
            var outside = new Vector2(900f, 900f);
            foreach (var phase in new[] { PointerPhase.Move, PointerPhase.Down, PointerPhase.Drag, PointerPhase.Up })
            {
                Assert.That(options.HandlePointer(outside, phase), Is.True);
            }

            options.HandlePointer(inside, PointerPhase.Down);
            options.HandlePointer(outside, PointerPhase.Drag);
            options.HandlePointer(outside, PointerPhase.Up);
            Assert.That(options.IsOpen, Is.True);
            options.HandlePointer(inside, PointerPhase.Down);
            Assert.That(options.HandlePointer(inside, PointerPhase.Up), Is.True);
            Assert.That(options.IsOpen, Is.False);
            Assert.That(options.CloseResult, Is.EqualTo(OptionsCloseResult.ReturnToGame));
            Assert.That(options.HandlePointer(inside, PointerPhase.Down), Is.False);
        }

        /// <summary>Preserves option drafts while the host opens and then closes a nested almanac modal.</summary>
        [Test]
        public void ResumingOptionsFromAlmanacPreservesDraftAndClearsCapture()
        {
            using var options = new OptionsView(_resourceRoot);
            options.GameplayButtonsEnabled = true;
            options.Open(.35f, .7f, true);
            var almanac = new Vector2(400f, 315f);
            options.HandlePointer(almanac, PointerPhase.Down);
            Assert.That(options.HandlePointer(almanac, PointerPhase.Up), Is.True);
            Assert.That(options.IsOpen, Is.False);
            Assert.That(options.CloseResult, Is.EqualTo(OptionsCloseResult.OpenAlmanac));
            options.ResumeFromAlmanac();
            options.HandlePointer(almanac, PointerPhase.Up);
            Assert.That(options.IsOpen, Is.True, "The old modal release must not reopen the almanac.");
            Assert.That(options.MusicVolume, Is.EqualTo(.35f));
            Assert.That(options.SoundVolume, Is.EqualTo(.7f));
            Assert.That(options.FullScreen, Is.True);
            Assert.That(options.CloseResult, Is.EqualTo(OptionsCloseResult.None));
        }

        /// <summary>Checks both logical axes within a fraction of a pixel after float viewport arithmetic.</summary>
        /// <param name="actual">The position calculated by the shared viewport mapper.</param>
        /// <param name="expected">The position defined by the original logical interface.</param>
        private static void AssertLogicalPosition(Vector2 actual, Vector2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(CoordinateTolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(CoordinateTolerance));
        }

        /// <summary>Configures only the dependencies exercised by the real coordinator's input router.</summary>
        /// <param name="controller">The inactive coordinator whose Awake and presentation lifecycle are suppressed.</param>
        /// <param name="name">The runtime dependency field to assign, declared by the coordinator or one of its collaborators.</param>
        /// <param name="value">The isolated test dependency.</param>
        private static void SetCoordinatorField(PvZGame controller, string name, object value)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var owner = (object)controller;
            var field = typeof(PvZGame).GetField(name, flags);
            if (field == null)
            {
                foreach (var property in typeof(PvZGame).GetProperties(flags | BindingFlags.DeclaredOnly))
                {
                    if (property.Name.StartsWith("Required", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var collaborator = property.GetValue(controller);
                    field = collaborator?.GetType().GetField(name, flags);
                    if (field != null)
                    {
                        owner = collaborator!;
                        break;
                    }
                }
            }

            field!.SetValue(owner, value);
        }

        /// <summary>Supplies a deterministic sampled frame without polling physical hardware or constructing GUI events.</summary>
        /// <param name="snapshot">The frame dispatched to the coordinator.</param>
        private static void SetInputSnapshot(GameInputSnapshot snapshot)
        {
            typeof(GameInput).GetProperty(nameof(GameInput.Current))!.GetSetMethod(true)!.Invoke(null, new object[] { snapshot });
        }
    }
}
