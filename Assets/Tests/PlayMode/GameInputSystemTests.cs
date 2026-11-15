#nullable enable
using System;
using NUnit.Framework;
using PlantsVsZombies.Inputs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks production input snapshots with isolated Input System devices rather than OS input.</summary>
    public sealed class GameInputSystemTests : InputTestFixture
    {
        /// <summary>Stores the isolated mouse.</summary>
        private Mouse _mouse = null!;
        /// <summary>Stores the isolated keyboard.</summary>
        private Keyboard _keyboard = null!;

        /// <summary>Creates isolated devices and initializes the production touch history before input arrives.</summary>
        public override void Setup()
        {
            GameInput.Reset();
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            GameInput.Capture();
        }

        /// <summary>Releases production subscriptions before restoring the real Input System.</summary>
        public override void TearDown()
        {
            GameInput.Reset();
            base.TearDown();
        }

        /// <summary>Checks pointer coordinates, held state, secondary clicks and native wheel scale.</summary>
        [Test]
        public void MouseUsesLogicalViewportAndPreservesWheelScale()
        {
            var point = new Vector2(Screen.width * .4f, Screen.height * .6f);
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = point, scroll = new Vector2(0f, 1.25f) }
                .WithButton(MouseButton.Left).WithButton(MouseButton.Right));
            InputSystem.Update();
            var input = GameInput.Capture();
            Assert.That(input.Position, Is.EqualTo(GameViewport.ScreenToLogical(point)));
            Assert.That(input.InsideViewport, Is.True);
            Assert.That(input.Pressed && input.Held && input.RightPressed, Is.True);
            Assert.That(input.ScrollDelta, Is.EqualTo(-3.75f));
            InputSystem.Update();
            input = GameInput.Capture();
            Assert.That(input.Pressed || input.RightPressed, Is.False);
            Assert.That(input.Held, Is.True);
            Assert.That(input.ScrollDelta, Is.Zero);
        }

        /// <summary>Preserves ordered press and release when both arrive within the same input update.</summary>
        [Test]
        public void MouseQuickClickKeepsBothTransitions()
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(_mouse, new MouseState());
            InputSystem.Update();
            var input = GameInput.Capture();
            Assert.That(input.Pressed && input.Released, Is.True);
            Assert.That(input.Held, Is.False);
            Assert.That(input.PhaseCount, Is.EqualTo(2));
            Assert.That(input.GetPhase(0), Is.EqualTo(PointerPhase.Down));
            Assert.That(input.GetPhase(1), Is.EqualTo(PointerPhase.Up));
        }

        /// <summary>Samples all existing keyboard shortcuts through the same input frame.</summary>
        /// <param name="key">The physical shortcut key.</param>
        [TestCase(Key.Escape)]
        [TestCase(Key.Enter)]
        [TestCase(Key.NumpadEnter)]
        [TestCase(Key.Space)]
        [TestCase(Key.F9)]
        public void KeyboardShortcutsAreFrameBounded(Key key)
        {
            Press(_keyboard[key]);
            var input = GameInput.Capture();
            Assert.That(input.EscapePressed, Is.EqualTo(key == Key.Escape));
            Assert.That(input.EnterPressed, Is.EqualTo(key == Key.Enter || key == Key.NumpadEnter));
            Assert.That(input.SpacePressed, Is.EqualTo(key == Key.Space));
            Assert.That(input.SaveUtilityPressed, Is.EqualTo(key == Key.F9));
            InputSystem.Update();
            input = GameInput.Capture();
            Assert.That(input.EscapePressed || input.EnterPressed || input.SpacePressed || input.SaveUtilityPressed, Is.False);
        }

        /// <summary>Cancels a removed mouse instead of releasing a clickable pointer over the last control.</summary>
        [Test]
        public void DeviceRemovalCancelsCapturedMouse()
        {
            Press(_mouse.leftButton);
            Assert.That(GameInput.Capture().Held, Is.True);
            InputSystem.RemoveDevice(_mouse);
            var input = GameInput.Capture();
            Assert.That(input.Released, Is.True);
            Assert.That(input.InsideViewport || input.Held, Is.False);
        }

        /// <summary>Prevents focus reset releases from clicking and requires a fresh neutral gesture.</summary>
        [Test]
        public void FocusLossCancelsWithoutAcceptingHeldPointerOnReturn()
        {
            Press(_mouse.leftButton);
            GameInput.Capture();
            GameInput.SetFocus(false);
            var input = GameInput.Capture();
            Assert.That(input.Released && !input.InsideViewport, Is.True);
            GameInput.SetFocus(true);
            Assert.That(GameInput.Capture().Held, Is.False);
            Release(_mouse.leftButton);
            Assert.That(GameInput.Capture().Released, Is.False);
            Press(_mouse.leftButton);
            Assert.That(GameInput.Capture().Pressed, Is.True);
        }

        /// <summary>Keeps the first finger, ignores secondary and mouse input, and waits for all fingers to leave.</summary>
        [Test]
        public void TouchKeepsPrimaryOwnershipAndSuppressesMouse()
        {
            var screen = InputSystem.AddDevice<Touchscreen>();
            var first = new Vector2(120f, 160f);
            BeginTouch(10, first, screen: screen);
            Assert.That(GameInput.Capture().Pressed, Is.True);
            BeginTouch(20, new Vector2(300f, 300f), screen: screen);
            Press(_mouse.rightButton);
            var input = GameInput.Capture();
            Assert.That(input.Position, Is.EqualTo(GameViewport.ScreenToLogical(first)));
            Assert.That(input.RightPressed, Is.False);
            EndTouch(10, first, screen: screen);
            input = GameInput.Capture();
            Assert.That(input.Released && !input.Held, Is.True);
            MoveTouch(20, new Vector2(310f, 310f), screen: screen);
            Assert.That(GameInput.Capture().Held, Is.False);
            EndTouch(20, new Vector2(310f, 310f), screen: screen);
            GameInput.Capture();
            InputSystem.Update();
            GameInput.Capture();
            InputSystem.Update();
            GameInput.Capture();
            BeginTouch(30, first, screen: screen);
            Assert.That(GameInput.Capture().Pressed, Is.True);
        }

        /// <summary>Checks scene-start touch history records a press that arrives before the first game Update.</summary>
        [Test]
        public void StartupHistoryKeepsTouchBeforeFirstCapture()
        {
            GameInput.Reset();
            GameInput.Initialize();
            var screen = InputSystem.AddDevice<Touchscreen>();
            BeginTouch(10, new Vector2(120f, 160f), screen: screen);
            Assert.That(GameInput.Capture().Pressed, Is.True);
        }

        /// <summary>Preserves a rapid touch tap through Enhanced Touch's frame history.</summary>
        [Test]
        public void QuickTouchTapIsNotLostBetweenUpdates()
        {
            var screen = InputSystem.AddDevice<Touchscreen>();
            var point = new Vector2(120f, 160f);
            BeginTouch(10, point, queueEventOnly: true, screen: screen);
            EndTouch(10, point, queueEventOnly: true, screen: screen);
            InputSystem.Update();
            Assert.That(GameInput.Capture().Pressed, Is.True);
            InputSystem.Update();
            Assert.That(GameInput.Capture().Released, Is.True);
        }

        /// <summary>Cancels explicitly canceled and removed touch devices without a clickable release.</summary>
        /// <param name="removeDevice">Whether to remove the screen rather than send a cancellation phase.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void InterruptedTouchReleasesOutsideViewport(bool removeDevice)
        {
            var screen = InputSystem.AddDevice<Touchscreen>();
            var point = new Vector2(120f, 160f);
            BeginTouch(10, point, screen: screen);
            GameInput.Capture();
            if (removeDevice)
            {
                InputSystem.RemoveDevice(screen);
            }
            else
            {
                CancelTouch(10, point, screen: screen);
            }
            var input = GameInput.Capture();
            Assert.That(input.Released, Is.True);
            Assert.That(input.InsideViewport || input.Held, Is.False);
        }

        /// <summary>Checks the warmed frame sampler keeps the existing allocation budget.</summary>
        [Test]
        public void WarmedCaptureDoesNotAllocate()
        {
            for (var i = 0; i < 10; i++)
            {
                GameInput.Capture();
            }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
            {
                GameInput.Capture();
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}
