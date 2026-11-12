#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Inputs;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects input coordinates when the native game viewport is scaled or letterboxed.</summary>
    public sealed class GameViewportTests
    {
        /// <summary>Preserves the original aspect ratio and centers a widescreen viewport.</summary>
        [Test]
        public void CentersViewportOnWideScreens()
        {
            Assert.That(GameViewport.CalculateRect(1600f, 900f), Is.EqualTo(new Rect(200f, 0f, 1200f, 900f)));
        }

        /// <summary>Centers the game vertically on a portrait display without distorting input.</summary>
        [Test]
        public void CentersViewportOnPortraitScreens()
        {
            Assert.That(GameViewport.CalculateRect(600f, 1000f), Is.EqualTo(new Rect(0f, 275f, 600f, 450f)));
        }

        /// <summary>Converts bottom-left screen pixels to the original downward pointer coordinates.</summary>
        [Test]
        public void ConvertsScreenPixelsToLogicalCoordinates()
        {
            Assert.That(GameViewport.ScreenToLogical(new Vector2(200f, 900f), 1600f, 900f), Is.EqualTo(Vector2.zero));
            Assert.That(GameViewport.ScreenToLogical(new Vector2(1400f, 0f), 1600f, 900f), Is.EqualTo(new Vector2(800f, 600f)));
            Assert.That(GameViewport.ScreenToLogical(new Vector2(800f, 450f), 1600f, 900f), Is.EqualTo(new Vector2(400f, 300f)));
        }

        /// <summary>Keeps letterbox pointer positions outside the logical play area.</summary>
        [Test]
        public void PreservesOutsideViewportCoordinates()
        {
            var outside = GameViewport.ScreenToLogical(new Vector2(50f, 450f), 1600f, 900f);
            Assert.That(outside.x, Is.LessThan(0f));
            Assert.That(new Rect(0f, 0f, GameViewport.Width, GameViewport.Height).Contains(outside), Is.False);
        }
    }
}
