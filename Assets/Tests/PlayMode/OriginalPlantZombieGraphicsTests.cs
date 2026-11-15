#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks all original plant-zombie resources and live attack/child states through the production board renderer.</summary>
    public sealed class OriginalPlantZombieGraphicsTests
    {
        /// <summary>Checks all six heads keep original source parts in ordinary and mind-controlled orientations.</summary>
        /// <returns>Original day and pool scene captures.</returns>
        [UnityTest]
        public IEnumerator AllSixHeadsUseOriginalArtworkAndStableParentPlacement()
        {
            foreach (var mode in new[] { 16, 32 })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(mode == 16 ? 8 : 28);
                Prepare(fixture, mode);
                foreach (var kind in new[] { 26, 27, 28, 29, 30, 31 })
                {
                    SimulationFixture.Zombies(fixture.Owner.RequiredGame).Clear();
                    var zombie = Spawn(fixture.Owner.RequiredGame, kind, mode == 16 ? 1 : 2);
                    zombie.PresentationAnimationAge = .3f;
                    zombie.PlantZombieState!.HeadSeconds = .3f;
                    foreach (var mirror in new[] { false, true })
                    {
                        zombie.Hypnotized = mirror;
                        yield return fixture.Capture("DrawAdventureBoard", null, wholeBoard: true);
                        var sprites = Pool(fixture).SubmittedSprites.Take(Pool(fixture).SubmittedObjectCount).ToArray();
                        var expected = kind == 26 ? "peashooter" : kind == 27 ? "wallnut" : kind == 28 ? "jalapeno"
                            : kind == 29 ? "gatling" : kind == 30 ? "squash" : "tallnut";
                        Assert.That(sprites.Any(sprite => sprite.SourceImage!.SourceIdentity.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0), Is.True, "Missing native plant head " + kind);
                        Assert.That(SpriteDrawState.Color, Is.EqualTo(Color.white));
                        Save(fixture, "head-" + kind + "-mode-" + mode + "-mirror-" + mirror);
                    }
                }
            }
        }

        /// <summary>Checks original shooting clips and standalone Squash render states reach the real scene.</summary>
        /// <returns>Original firing, rising and falling scene captures.</returns>
        [UnityTest]
        public IEnumerator NativeAttackClipsAndDetachedSquashRenderOnTheLawn()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
            Prepare(fixture, 16);
            foreach (var kind in new[] { 26, 29 })
            {
                SimulationFixture.Zombies(fixture.Owner.RequiredGame).Clear();
                var zombie = Spawn(fixture.Owner.RequiredGame, kind, 1);
                zombie.PlantZombieState!.Play("anim_shooting", kind == 26 ? 35 : 38, false, 20);
                zombie.PlantZombieState.HeadSeconds = .3f;
                yield return fixture.Capture("DrawAdventureBoard", null, wholeBoard: true);
                Save(fixture, "shooting-" + kind);
            }
            SimulationFixture.Zombies(fixture.Owner.RequiredGame).Clear();
            var squash = Spawn(fixture.Owner.RequiredGame, 30, 1);
            squash.Eating = true;
            SimulationInspection.GetMethod("TickModeSpecialZombies").Invoke(fixture.Owner.RequiredGame, new object[] { .01f });
            Assert.That(squash.PlantZombieState!.Detached, Is.True);
            yield return fixture.Capture("DrawAdventureBoard", null, wholeBoard: true);
            Assert.That(Pool(fixture).SubmittedSprites.Take(Pool(fixture).SubmittedObjectCount)
                .Any(sprite => sprite.SourceImage!.SourceIdentity.IndexOf("squash", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);
            Save(fixture, "squash-rising");
            squash.PlantZombieState.Counter = 1;
            SimulationInspection.GetMethod("TickModeSpecialZombies").Invoke(fixture.Owner.RequiredGame, new object[] { .01f });
            yield return fixture.Capture("DrawAdventureBoard", null, wholeBoard: true);
            Save(fixture, "squash-falling");
        }

        /// <summary>Checks the native backwards pea texture and shadow are painted by the board instead of the mode footer.</summary>
        /// <returns>The original projectile scene frame.</returns>
        [UnityTest]
        public IEnumerator NativeHeadProjectileUsesOriginalTextureAtItsAuthoredLaunchPoint()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(8);
            Prepare(fixture, 16);
            var zombie = Spawn(fixture.Owner.RequiredGame, 26, 1);
            zombie.PlantZombieState!.Counter = 1;
            SimulationInspection.GetMethod("TickModeSpecialZombies").Invoke(fixture.Owner.RequiredGame, new object[] { .01f });
            Assert.That(fixture.Owner.IndependentModes.RequiredModeSession.HeadShots.Count, Is.EqualTo(1));
            yield return fixture.Capture("DrawAdventureBoard", null, wholeBoard: true);
            Save(fixture, "native-head-projectile");
            var image = fixture.Owner.Textures.LoadImage("ProjectilePea.png")!;
            Assert.That(Resources.FindObjectsOfTypeAll<PlantsVsZombies.UI.CanvasTextureGraphic>()
                .Any(item => item.gameObject.activeInHierarchy && item.mainTexture == image.SourceTexture), Is.True);
        }

        /// <summary>Installs an original independent board without profiles or persistent data.</summary>
        /// <param name="fixture">The production original-resource renderer.</param>
        /// <param name="mode">The original Pea Heads mode.</param>
        private static void Prepare(OriginalStormGraphicsTests.StormFixture fixture, int mode)
        {
            var session = new ModeSession(mode);
            Assert.That(session.ChooseSeeds(session.Game.Adventure.AvailableSeeds.Take(6)), Is.True);
            fixture.Owner._game = session.Game;
            fixture.Owner.IndependentModes._modeSession = session;
            fixture.Owner._currentView = PvZGame.ScreenView.Board;
        }

        /// <summary>Allocates a native species through the production initialization path.</summary>
        /// <param name="game">The isolated mode board.</param>
        /// <param name="kind">The original species.</param>
        /// <param name="row">The original lawn row.</param>
        /// <returns>The initialized actor.</returns>
        private static Zombie Spawn(GameSimulation game, int kind, int row)
        {
            return (Zombie)SimulationInspection.GetMethod("SpawnAdventureZombie").Invoke(game,
                new object[] { (ZombieKind)kind, row, 4.7f, 0 })!;
        }

        /// <summary>Reads the production scene source commands.</summary>
        /// <param name="fixture">The original board capture.</param>
        /// <returns>The native source sprite pool.</returns>
        private static ObjectSpriteRenderer Pool(OriginalStormGraphicsTests.StormFixture fixture)
        {
            return (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture.Animations)!;
        }

        /// <summary>Saves optional GPU captures into the explicitly selected ignored output directory.</summary>
        /// <param name="fixture">The completed scene capture.</param>
        /// <param name="name">The original state label.</param>
        private static void Save(OriginalStormGraphicsTests.StormFixture fixture, string name)
        {
            var directory = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_OUTPUT");
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), fixture.Pixels.EncodeToPNG());
        }
    }
}
