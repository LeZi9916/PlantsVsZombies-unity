#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Reanimation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the local PC cross-animation angle rule independently of authored keyframe interpolation.</summary>
    public sealed class ReanimationPoseBlendTests
    {
        /// <summary>Checks the exact inclusive 180-degree boundary and the original direct-new-angle branch.</summary>
        /// <param name="previous">The interrupted animation's retained angle.</param>
        /// <param name="current">The new animation's current angle.</param>
        /// <param name="fraction">The elapsed blend fraction.</param>
        /// <param name="expected">The angle produced by the local executable's BlendTransform.</param>
        [TestCase(10f, -170f, .5f, -80f)]
        [TestCase(-10f, 170f, .5f, 80f)]
        [TestCase(10f, -170.01f, .5f, -170.01f)]
        [TestCase(-10f, 170.01f, .5f, 170.01f)]
        [TestCase(0f, -358.4f, 0f, -358.4f)]
        [TestCase(0f, -358.4f, .5f, -358.4f)]
        [TestCase(-358.4f, 0f, .5f, 0f)]
        [TestCase(0f, 720f, .5f, 720f)]
        [TestCase(0f, -720f, .5f, -720f)]
        [TestCase(-30f, 30f, .25f, -15f)]
        public void CrossAnimationAnglesMatchLocalPcBoundary(float previous, float current, float fraction, float expected)
        {
            Assert.That(ReanimationRenderer.BlendNativeAngle(previous, current, fraction), Is.EqualTo(expected).Within(.0001f));
        }
    }
}
