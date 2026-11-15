#nullable enable

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks the original lobber image pivot, scale and rotation against PC Projectile::Draw.</summary>
    public sealed class OriginalLobberGraphicsTests
    {
        /// <summary>Checks all five original weapons rotate about the unscaled image center above their native top-left flight point.</summary>
        /// <returns>Real original-artwork frames and their submitted source geometry.</returns>
        [UnityTest]
        public IEnumerator LobberImagesUseOriginalPivotsScaleAndRotation()
        {
            foreach (var weapon in new[] { (PlantKind.CabbagePult, false), (PlantKind.KernelPult, false),
                (PlantKind.KernelPult, true), (PlantKind.MelonPult, false), (PlantKind.WinterMelon, false) })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
                var shot = new Pea { SourceKind = weapon.Item1, Row = 1, FlightRow = 1, Lobbed = true,
                    Butter = weapon.Item2, Splash = weapon.Item1 == PlantKind.MelonPult || weapon.Item1 == PlantKind.WinterMelon,
                    IsSnow = weapon.Item1 == PlantKind.WinterMelon, X = 3.25f, PresentationPreviousX = 3.25f,
                    PresentationInitialized = true, PresentationLaunchY = 150f,
                    PresentationLobHeight = -50f, PresentationPreviousLobHeight = -50f,
                    PresentationRotation = .4f, PresentationSpin = .07f, PresentationNativeAge = .5f };
                var inherited = Matrix4x4.identity;
                yield return fixture.Capture(null, () =>
                {
                    inherited = SpriteDrawState.Matrix;
                    fixture.DrawPass("DrawAdventureProjectile", shot);
                });
                var pool = (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture.Animations)!;
                var image = pool.SubmittedSprites.Last();
                var source = image.SourceImage!;
                Assert.That(source.SourceIdentity, Does.Contain(weapon.Item2 ? "butter" : shot.Splash ? "melon" : weapon.Item1 == PlantKind.KernelPult ? "kernal" : "cabbage").IgnoreCase);
                var scale = weapon.Item2 ? .8f : weapon.Item1 == PlantKind.KernelPult ? .95f : 1f;
                // PC 47D950 uses mPos + the unscaled source half-size, then TodScaleRotateTransformMatrix's negative rotation.
                var matrix = inherited * Matrix4x4.TRS(new Vector3(300f + source.Width * .5f, 100f + source.Height * .5f),
                    Quaternion.Euler(0f, 0f, -3.9f * Mathf.Rad2Deg), new Vector3(scale, scale, 1f));
                var expected = matrix.MultiplyPoint3x4(new Vector3(-source.Width * .5f, -source.Height * .5f));
                var actual = image.Vertices[0];
                Assert.That(actual.x, Is.EqualTo(expected.x).Within(.0001f), weapon + " native image pivot X");
                Assert.That(-actual.y, Is.EqualTo(expected.y).Within(.0001f), weapon + " native image pivot Y");
            }
        }
    }
}
