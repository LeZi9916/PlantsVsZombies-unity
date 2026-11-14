#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Compares original giant actions, durability art, free charred catapult and flattened plant pixels.</summary>
    public sealed class OriginalGargantuarGraphicsTests
    {
        /// <summary>Checks original action clips, damage substitutions, weapon variants and released-imp visibility.</summary>
        /// <returns>Real camera frames comparing independent source-track composition.</returns>
        [UnityTest]
        public IEnumerator GiantActionsAndDamageMatchOriginalSourceTracks()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
            var xml = XElement.Parse("<r>" + File.ReadAllText(Path.Combine(fixture.Root, "reanim", "Zombie_gargantuar.reanim")) + "</r>");
            var tracks = xml.Elements("track").Select(track => track.Element("name")!.Value).ToArray();
            var id = 0;
            foreach (var sample in new[] {
                (0, 3000f, false, 0, false, "anim_walk", .3f, 12f),
                (70, 1999f, false, 1, false, "anim_smash", .8f, 16f),
                (69, 999f, true, 2, false, "anim_throw", 1.1f, 24f),
                (70, 1999f, true, 0, true, "anim_smash", 1.4f, 16f) })
            {
                var giant = new Zombie { Id = ++id, Kind = ZombieKind.Gargantuar, Row = 1,
                    X = AdventureZombieNativeRules.GridX(400f), Health = sample.Item2,
                    StartingHealth = sample.Item5 ? 6000f : 3000f, ModeHead = sample.Item5 ? 32 : -1,
                    PresentationNativePhase = sample.Item1, PresentationAnimation = sample.Item6,
                    PresentationAnimationAge = sample.Item7, PresentationMovementRate = sample.Item8,
                    _presentationMovementAnimation = sample.Item6, PresentationBodyBlendTicks = 0,
                    ThrownImp = sample.Item3, GargantuarState = new AdventureGargantuarState { Weapon = sample.Item4 } };
                fixture.Owner.RequiredGame.Zombies.Clear();
                fixture.Owner.RequiredGame.Zombies.Add(giant);
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", giant));
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () =>
                {
                    var damage = sample.Item2 < (sample.Item5 ? 2000f : 1000f) ? 2 : sample.Item2 < 2000f ? 1 : 0;
                    foreach (var track in tracks)
                    {
                        if (sample.Item3 && (track.StartsWith("Zombie_imp", StringComparison.OrdinalIgnoreCase)
                            || track.Equals("zombie_gargantuar_whiterope", StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        string? image = null;
                        if (track.Equals("zombie_gargantua_body1", StringComparison.OrdinalIgnoreCase) && damage > 0)
                        {
                            image = damage == 2 ? "IMAGE_REANIM_ZOMBIE_GARGANTUAR_BODY1_3" : "IMAGE_REANIM_ZOMBIE_GARGANTUAR_BODY1_2";
                        }

                        if (track.Equals("zombie_gargantuar_outerarm_lower", StringComparison.OrdinalIgnoreCase) && damage > 0)
                        {
                            image = "IMAGE_REANIM_ZOMBIE_GARGANTUAR_OUTERARM_LOWER2";
                        }

                        if (track.Equals("zombie_gargantuar_outerleg_foot", StringComparison.OrdinalIgnoreCase) && damage == 2)
                        {
                            image = "IMAGE_REANIM_ZOMBIE_GARGANTUAR_FOOT2";
                        }

                        if (track.Equals("anim_head1", StringComparison.OrdinalIgnoreCase))
                        {
                            image = sample.Item5 ? "IMAGE_REANIM_ZOMBIE_GARGANTUAR_HEAD2_REDEYE"
                                : damage == 2 ? "IMAGE_REANIM_ZOMBIE_GARGANTUAR_HEAD2" : null;
                        }

                        if (track.Equals("zombie_gargantuar_telephonepole", StringComparison.OrdinalIgnoreCase))
                        {
                            image = sample.Item4 == 2 ? "IMAGE_REANIM_ZOMBIE_GARGANTUAR_ZOMBIE"
                                : sample.Item4 == 1 ? "IMAGE_REANIM_ZOMBIE_GARGANTUAR_DUCKXING" : null;
                        }

                        fixture.Animations.DrawNativePlayback("Zombie_gargantuar", new Vector2(415f, 109f), 1f,
                            sample.Item7, sample.Item6, sample.Item1 == 0, sample.Item8, name => name == track, track, image);
                    }
                });
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "Original giant action " + sample);
            }
        }

        /// <summary>Checks flattened cached plants use PC's .25 scale, sixty-pixel argument and original pot order.</summary>
        /// <returns>Real camera frames comparing static unchanged source artwork.</returns>
        [UnityTest]
        public IEnumerator CrushedPlantsKeepOriginalCachedPictureAndQuarterHeight()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            var game = fixture.Owner.RequiredGame;
            game.Plants.Clear();
            var pot = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.FlowerPot, 1, 3)!;
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Peashooter, 1, 3)!;
            Invoke(game, "CrushAdventurePlantCell", 1, 3, false);
            Assert.That(game.Presentation.SquishedPlants.Count, Is.EqualTo(2));
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureSquishedPlants", 1));
            var actual = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () =>
            {
                var previous = SpriteDrawState.Matrix;
                try
                {
                    SpriteDrawState.Matrix = previous * Matrix4x4.Translate(new Vector3(280f, 266f, 0f))
                        * Matrix4x4.Scale(new Vector3(1f, .25f, 1f));
                    fixture.Animations.DrawNative("Pot", Vector2.zero, 1f, 0f, null);
                    SpriteDrawState.Matrix = previous * Matrix4x4.Translate(new Vector3(280f, 255f, 0f))
                        * Matrix4x4.Scale(new Vector3(1f, .25f, 1f));
                    fixture.Animations.DrawNative("PeaShooterSingle", Vector2.zero, 1f, 0f, null);
                }
                finally
                {
                    SpriteDrawState.Matrix = previous;
                }
            });
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual));
            Assert.That(Array.Exists(actual, pixel => pixel.r < 200 || pixel.g < 200 || pixel.b < 200), Is.True);
        }

        /// <summary>Checks actual roof projectile centers and unchanged original splat particles after a real slope collision.</summary>
        /// <returns>Real source-backed camera comparisons of muzzle and roof-contact frames.</returns>
        [UnityTest]
        public IEnumerator RoofShooterMuzzlesAndSlopeSplatsMatchOriginalArtwork()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            var game = fixture.Owner.RequiredGame;
            var particles = new OriginalParticleRenderer(fixture.Root, fixture.Animations);
            foreach (var kind in new[] { PlantKind.Peashooter, PlantKind.SnowPea, PlantKind.GatlingPea, PlantKind.Cactus, PlantKind.ScaredyShroom })
            {
                game.Plants.Clear();
                game.Peas.Clear();
                game.Presentation.Impacts.Clear();
                Invoke(game, "PlaceAdventurePlant", PlantKind.FlowerPot, 1, 1);
                var plant = (Plant)Invoke(game, "PlaceAdventurePlant", kind, 1, 1)!;
                if (kind == PlantKind.Cactus || kind == PlantKind.ScaredyShroom)
                {
                    Invoke(game, "FireAdventureNativeTimedProjectile", plant);
                }
                else
                {
                    var shot = (Pea)Invoke(game, "FireAdventurePea", plant, 1, false, 20f)!;
                    shot.IsSnow = kind == PlantKind.SnowPea;
                }

                var pea = game.Peas.Single();
                var x = kind == PlantKind.GatlingPea ? 205f : kind == PlantKind.Cactus ? 205f
                    : kind == PlantKind.ScaredyShroom ? 165f : 195f;
                var y = kind == PlantKind.Cactus ? 261f : kind == PlantKind.ScaredyShroom ? 264.5f : 259f;
                var image = kind == PlantKind.SnowPea ? "IMAGE_PROJECTILESNOWPEA" : kind == PlantKind.Cactus ? "IMAGE_PROJECTILECACTUS"
                    : kind == PlantKind.ScaredyShroom ? "IMAGE_PUFFSHROOM_PUFF1" : "IMAGE_PROJECTILEPEA";
                // Cell (1,1) is native Y235; these centers include the original five-pixel pot correction.
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureProjectile", pea));
                var muzzle = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () => fixture.Animations.DrawResourceSprite(image, new Vector2(x, y), 1f));
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(muzzle), "Original roof muzzle " + kind);
                if (kind == PlantKind.Cactus || kind == PlantKind.ScaredyShroom)
                {
                    continue;
                }

                for (var tick = 0; tick < 100 && game.Peas.Count > 0; tick++)
                {
                    Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
                }

                var impact = game.Presentation.Impacts.Single();
                impact.Age = .03f;
                yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureImpacts", 1));
                var actual = fixture.Pixels.GetPixels32();
                yield return fixture.Capture(null, () => particles.Draw("original-roof-splat-" + kind,
                    kind == PlantKind.SnowPea ? "SnowPeaSplat" : "PeaSplat", .03f,
                    new Vector2(40f + pea.X * 80f - 17f, 257f), impact.Seed));
                Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "Original roof splat " + kind);
                Assert.That(Array.Exists(actual, pixel => pixel.r < 245 || pixel.g < 245 || pixel.b < 245), Is.True,
                    "The original roof impact must submit visible source particles for " + kind);
            }
        }

        /// <summary>Checks the near-edge roof imp's first airborne body uses its parent's retained native floor.</summary>
        /// <returns>Real original thrown and landing source-art frames following production updates.</returns>
        [UnityTest]
        public IEnumerator ThrownImpKeepsOriginalRoofReleaseAndLandingArtwork()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(49);
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
            var game = fixture.Owner.RequiredGame;
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Plants.Clear();
            game.Zombies.Clear();
            var giant = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Gargantuar, 1,
                AdventureZombieNativeRules.GridX(401f), 0)!;
            giant.Health = 1499f;
            for (var tick = 0; tick < 106; tick++)
            {
                game.Update(.01f);
            }

            var imp = game.Zombies.Single(zombie => zombie.Kind == ZombieKind.Imp);
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", imp));
            var released = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () => fixture.Animations.DrawNativePlayback("Zombie_imp", new Vector2(283f, 29f),
                1f, 0f, "anim_thrown", false, 18f));
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(released));
            for (var tick = 0; tick < 200 && imp.PresentationNativePhase == 71; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(imp.PresentationNativePhase, Is.EqualTo(72));
            var nativeX = AdventureZombieNativeRules.NativePositionX(imp.PresentationPreviousX);
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureZombie", imp));
            var landed = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () => fixture.Animations.DrawNativePlayback("Zombie_imp",
                new Vector2(nativeX + 15f, 117f + Math.Max(0f, 400f - nativeX) * .25f),
                1f, 0f, "anim_land", false, 24f));
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(landed));
        }

        /// <summary>Calls a production simulation entry point without replacing behavior.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="method">The original private method name.</param>
        /// <param name="arguments">The production arguments.</param>
        /// <returns>The production method result.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
