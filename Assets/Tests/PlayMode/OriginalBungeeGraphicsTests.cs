#nullable enable

using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original bungee artwork, rope geometry, target position and native cargo-arm composition.</summary>
    public sealed class OriginalBungeeGraphicsTests
    {
        /// <summary>Compares the actual bungee pass with independent original-body/cargo/foreground-arms composition.</summary>
        /// <returns>Real graphics frame boundaries before reading the original artwork.</returns>
        [UnityTest]
        public IEnumerator OriginalBodyRopeReticleAndCargoGroupsMatchNativeGeometry()
        {
            foreach (var level in new[] { 11, 48 })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(level);
                SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow2.png", null!);
                SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow.png", null!);
                var game = fixture.Owner.RequiredGame;
                game.ChooseDefaultAdventureSeeds();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!
                    .Invoke(game, new object[] { PlantKind.Wallnut, 2, 3 })!;
                var zombie = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureBoardZombie", flags)!
                    .Invoke(game, new object[] { ZombieKind.Bungee, 2, 9.5f, 0 })!;
                zombie.Row = 2;
                zombie.TargetColumn = 3;
                zombie.X = AdventureZombieNativeRules.GridX(280f);
                zombie.PresentationPreviousX = zombie.X;
                Assert.That(fixture.Animations.TryGetNativeAnimationInfo("Zombie_bungi", "anim_grab", out var grab), Is.True);
                Assert.That(grab.Count, Is.EqualTo(15));
                Assert.That(fixture.Animations.TryGetNativeSpriteSize("IMAGE_BUNGEECORD", out var cord), Is.True);
                Assert.That(cord, Is.EqualTo(new Vector2Int(8, 64)));
                Assert.That(fixture.Animations.TryGetNativeSpriteSize("IMAGE_BUNGEETARGET", out var target), Is.True);
                Assert.That(target, Is.EqualTo(new Vector2Int(71, 76)));
                var state = zombie.BungeeState!;
                foreach (var phase in new[] { 4, 6, 7, 8 })
                {
                    state.Enter(phase);
                    state.AnimationSeconds = phase == 8 ? .6f : .25f;
                    zombie.PresentationNativePhase = phase;
                    zombie.PresentationAltitude = phase == 4 ? 80f : phase == 8 ? 120f : 0f;
                    state.TargetPlant = phase == 7 || phase == 8 ? plant : null;
                    plant.BungeeState = phase == 7 ? 1 : phase == 8 ? 2 : 0;
                    yield return fixture.Capture(null, () =>
                    {
                        fixture.DrawPass("DrawAdventureBungee", zombie);
                        fixture.DrawPass("DrawAdventureBungeeTarget", zombie);
                    });
                    var actual = fixture.Pixels.GetPixels32();
                    yield return fixture.Capture(null, () =>
                    {
                        var origin = new Vector2(295f, (level == 48 ? 232f : 242f) - zombie.PresentationAltitude);
                        Assert.That(fixture.Animations.TryGetNativeTrackCenter("Zombie_bungi", state.Animation, "Zombie_bungi_body",
                            state.AnimationSeconds, state.AnimationRate, out var anchor, state.AnimationLoops), Is.True);
                        for (var y = origin.y + anchor.y - .5f - 64f; y > -64f; y -= 64f)
                        {
                            fixture.Animations.DrawResourceSprite("IMAGE_BUNGEECORD", new Vector2(319f, y + 32f), 1f);
                        }

                        fixture.Animations.DrawNativePlayback("Zombie_bungi", origin, 1f, state.AnimationSeconds,
                            state.Animation, state.AnimationLoops, state.AnimationRate, track => !ForegroundArms(track));
                        if (state.TargetPlant != null)
                        {
                            fixture.Animations.DrawNativePlayback("Wallnut", new Vector2(280f, 280f - zombie.PresentationAltitude),
                                1f, 0f, "anim_idle", true, 12f);
                        }

                        fixture.Animations.DrawNativePlayback("Zombie_bungi", origin, 1f, state.AnimationSeconds,
                            state.Animation, state.AnimationLoops, state.AnimationRate, ForegroundArms);
                        if (phase != 8)
                        {
                            fixture.Animations.DrawResourceSprite("IMAGE_BUNGEETARGET", new Vector2(325.5f, level == 48 ? 338f : 348f), 1f);
                        }
                    });
                    Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "Original bungee pose, cord and cargo groups must agree at native phase " + phase);
                }
            }
        }

        /// <summary>Checks the native lower bungee layer remains behind normal plants and the target stays above them.</summary>
        /// <returns>Real complete-board camera frames.</returns>
        [UnityTest]
        public IEnumerator WaitingBungeeIsBehindNormalPlantsButReticleIsAboveThem()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(11);
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow2.png", null!);
            var game = fixture.Owner.RequiredGame;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!
                .Invoke(game, new object[] { PlantKind.TallNut, 2, 3 })!;
            var zombie = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureBoardZombie", flags)!
                .Invoke(game, new object[] { ZombieKind.Bungee, 2, 9.5f, 0 })!;
            zombie.Row = 2;
            zombie.TargetColumn = 3;
            zombie.X = AdventureZombieNativeRules.GridX(280f);
            zombie.PresentationPreviousX = zombie.X;
            zombie.PresentationAltitude = 0f;
            zombie.BungeeState!.Enter(6, 300, 5);
            zombie.PresentationNativePhase = 6;
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            var actual = fixture.Pixels.GetPixels32();
            game.Plants.Remove(plant);
            zombie.LinkedZombieId = -1;
            yield return fixture.Capture("DrawAdventureBoard", () =>
            {
                fixture.DrawPass("DrawAdventurePlant", plant);
                zombie.LinkedZombieId = 0;
                fixture.DrawPass("DrawAdventureBungeeTarget", zombie);
            }, true);
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "PC 301007 bungee body precedes normal plants; its target uses projectile-row order 305001.");
        }

        /// <summary>Checks the original frozen passenger is drawn between the bungee body and its foreground hands.</summary>
        /// <returns>Actual native graphics frame boundaries.</returns>
        [UnityTest]
        public IEnumerator SuspendedPassengerUsesTheOriginalOffsetAndCargoLayer()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(11);
            SpriteSourceTestUtilities.SetImage(fixture.Owner.Textures, "plantshadow2.png", null!);
            var game = fixture.Owner.RequiredGame;
            typeof(GameSimulation).GetMethod("DropAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { ZombieKind.Normal, 2, 3, 0 });
            var carrier = game.Zombies[0];
            carrier.PresentationAltitude = 200f;
            carrier.BungeeState!.AnimationSeconds = .2f;
            yield return fixture.Capture(null, () => fixture.DrawPass("DrawAdventureBungee", carrier));
            var actual = fixture.Pixels.GetPixels32();
            yield return fixture.Capture(null, () =>
            {
                var origin = new Vector2(295f, 42f);
                Assert.That(fixture.Animations.TryGetNativeTrackCenter("Zombie_bungi", "anim_raise", "Zombie_bungi_body",
                    .2f, 36f, out var anchor, false), Is.True);
                for (var y = origin.y + anchor.y - .5f - 64f; y > -64f; y -= 64f)
                {
                    fixture.Animations.DrawResourceSprite("IMAGE_BUNGEECORD", new Vector2(319f, y + 32f), 1f);
                }

                fixture.Animations.DrawNativePlayback("Zombie_bungi", origin, 1f, .2f, "anim_raise", false, 36f, track => !ForegroundArms(track));
                fixture.Animations.DrawZombiePlayback(new Vector2(280f, 42f), 1f, 0f, Reanimation.ZombieVariant.Normal,
                    "anim_idle", true, 0f, true);
                fixture.Animations.DrawNativePlayback("Zombie_bungi", origin, 1f, .2f, "anim_raise", false, 36f, ForegroundArms);
            });
            Assert.That(fixture.Pixels.GetPixels32(), Is.EqualTo(actual), "PC 540410 holds the passenger fifteen pixels left of its carrier at the same native row baseline.");
        }

        /// <summary>Checks preview bungees use the original drop clip and integer sine bob rather than an idle pose.</summary>
        /// <returns>Camera frames checking the production preview remains visible and animated.</returns>
        [UnityTest]
        public IEnumerator StreetPreviewUsesOriginalDropPoseCordAndSineBob()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(48);
            var owner = fixture.Owner;
            var context = new AdventureIntroContext { Level = 45, NativeBackground = 4, IsConveyor = true };
            var preview = AdventureIntroPresentation.PlaceStreetZombie(context, 20, 1, 2,
                maximum => maximum == 201 ? 100 : 0, maximum => 0f, (kind, clip) => clip == "anim_idle" || clip == "anim_drop");
            Assert.That(preview.Animation, Is.EqualTo("anim_drop"));
            Assert.That(preview.AnimationRate, Is.EqualTo(24f));
            owner._currentView = Game.PvZGame.ScreenView.Board;
            owner.Opening._adventureIntro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(context));
            for (var tick = 0; tick < 350; tick++)
            {
                owner.Opening.RequiredAdventureIntro.Tick();
            }

            owner.Opening._adventureIntroFrame = owner.Opening.RequiredAdventureIntro.Frame;
            owner.Opening._adventureIntroDrawn = true;
            owner.Opening._adventureIntroEventHandler = owner.Opening.HandleAdventureIntroEvent;
            owner.Opening._adventurePreview.Add(preview);
            owner.Dave._crazyDaveMessageIndex = -1;
            owner.Opening.UpdateAdventureOpening(.01f);
            Assert.That(preview.BungeeTicks, Is.EqualTo(99));
            Assert.That(preview.Altitude, Is.EqualTo(41f));
            yield return fixture.Capture("DrawAdventureOpeningActors");
            var first = fixture.Pixels.GetPixels32();
            for (var tick = 0; tick < 49; tick++)
            {
                owner.Opening.UpdateAdventureOpening(.01f);
            }

            Assert.That(preview.BungeeTicks, Is.EqualTo(50));
            Assert.That(preview.Altitude, Is.EqualTo(80f));
            yield return fixture.Capture("DrawAdventureOpeningActors");
            var second = fixture.Pixels.GetPixels32();
            var changed = 0;
            for (var index = 0; index < first.Length; index++)
            {
                if (Math.Abs(first[index].r - second[index].r) + Math.Abs(first[index].g - second[index].g)
                    + Math.Abs(first[index].b - second[index].b) > 12)
                {
                    changed++;
                }
            }

            Assert.That(changed, Is.GreaterThan(100));
        }

        /// <summary>Matches all four prefixes assigned to render group two by PC 53E2F2..53E326.</summary>
        /// <param name="track">The original reanimation track name.</param>
        /// <returns>Whether the track belongs to the native foreground arms.</returns>
        private static bool ForegroundArms(string track)
        {
            return track.StartsWith("Zombie_bungi_rightarm_lower2", StringComparison.OrdinalIgnoreCase)
                || track.StartsWith("Zombie_bungi_rightarm_hand2", StringComparison.OrdinalIgnoreCase)
                || track.StartsWith("Zombie_bungi_leftarm_lower2", StringComparison.OrdinalIgnoreCase)
                || track.StartsWith("Zombie_bungi_leftarm_hand2", StringComparison.OrdinalIgnoreCase);
        }
    }
}
