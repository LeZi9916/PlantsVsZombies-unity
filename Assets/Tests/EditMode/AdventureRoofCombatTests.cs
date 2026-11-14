#nullable enable

using System;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC roof planting heights, stopped pot playback, and abandoned lobbed projectiles.</summary>
    public sealed class AdventureRoofCombatTests
    {
        /// <summary>Checks body and shadow share PC 473490 without mobile roof or mushroom scale corrections.</summary>
        [Test]
        public void RoofBodiesAndShadowsUseTheSameOriginalHeight()
        {
            var host = new GameObject("Inactive roof height fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var presenter = new GamePresenter(owner);
            try
            {
                var game = Board(42);
                owner._game = game;
                var pot = Place(game, PlantKind.FlowerPot);
                foreach (var kind in new[] { PlantKind.FlowerPot, PlantKind.Peashooter, PlantKind.CabbagePult,
                    PlantKind.KernelPult, PlantKind.MelonPult, PlantKind.WinterMelon, PlantKind.PuffShroom,
                    PlantKind.SunShroom, PlantKind.ScaredyShroom, PlantKind.HypnoShroom, PlantKind.MagnetShroom,
                    PlantKind.Chomper, PlantKind.Plantern, PlantKind.Pumpkin })
                {
                    var plant = kind == PlantKind.FlowerPot ? pot : Place(game, kind);
                    var supportHeight = kind == PlantKind.FlowerPot || kind == PlantKind.ScaredyShroom ? 0f
                        : kind == PlantKind.Chomper || kind == PlantKind.Plantern ? -10f : -5f;
                    var expected = AlmanacPortraitLayout.PlantHeightOffset((int)kind) + supportHeight;
                    var body = (float)typeof(GamePresenter).GetMethod("AdventurePlantHeight", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(presenter, new object[] { plant })!;
                    Assert.That(body, Is.EqualTo(expected), kind + " body");
                    Assert.That(AdventurePlantShadowRules.DrawHeight(plant, game.Plants, false, 0), Is.EqualTo(expected), kind + " shadow");
                    if (plant != pot)
                    {
                        game.Plants.Remove(plant);
                    }
                }
            }
            finally
            {
                presenter.Dispose();
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        /// <summary>Checks placement freezes the current pot frame and the final occupant's removal resumes it.</summary>
        [Test]
        public void OccupiedFlowerPotStopsAtItsCurrentIdleFrameAndResumesWhenUncovered()
        {
            var game = Board(42);
            var pot = Place(game, PlantKind.FlowerPot);
            game.Update(.17f);
            var frames = pot.PresentationBodyFrames;
            Assert.That(frames, Is.GreaterThan(0f));
            var plant = Place(game, PlantKind.Wallnut);
            var pumpkin = Place(game, PlantKind.Pumpkin);
            Assert.That(pot.PresentationFlowerPotPaused, Is.True);
            game.Update(.23f);
            Assert.That(pot.PresentationBodyFrames, Is.EqualTo(frames));
            Assert.That(game.TryShovelAdventurePlant(plant), Is.True);
            Assert.That(pot.PresentationFlowerPotPaused, Is.True, "A surviving pumpkin still covers the pot.");
            Assert.That(game.TryShovelAdventurePlant(pumpkin), Is.True);
            Assert.That(pot.PresentationFlowerPotPaused, Is.False);
            Assert.That(pot.PresentationRate, Is.InRange(10f, 15f));
            Assert.That(pot.PresentationBodyFrames, Is.EqualTo(frames), "Resuming must preserve the native animation phase.");
            game.Update(.01f);
            Assert.That(pot.PresentationBodyFrames, Is.GreaterThan(frames));
        }

        /// <summary>Checks all lobber weapons use the PC no-target velocity and impact the floor after target loss.</summary>
        /// <param name="level">The original flat or roof board.</param>
        /// <param name="targetAtLaunch">Whether the target dies after rather than before projectile creation.</param>
        [TestCase(12, false)]
        [TestCase(12, true)]
        [TestCase(42, false)]
        [TestCase(42, true)]
        public void LobbedShotsKeepTheirLaunchTrajectoryAndImpactTheOriginalFloor(int level, bool targetAtLaunch)
        {
            foreach (var weapon in new[] { (PlantKind.CabbagePult, false), (PlantKind.KernelPult, false),
                (PlantKind.KernelPult, true), (PlantKind.MelonPult, false), (PlantKind.WinterMelon, false) })
            {
                var game = Board(level);
                if (level == 42)
                {
                    Place(game, PlantKind.FlowerPot);
                }

                var plant = Place(game, weapon.Item1);
                plant.PresentationButter = weapon.Item2;
                plant._kernelWeaponPrepared = true;
                var target = new Zombie { Id = 500, Row = 1, X = 6f, Health = targetAtLaunch ? 270f : 0f, Speed = .3f };
                game.Zombies.Add(target);
                Invoke(game, "FireAdventureNativeTimedProjectile", plant);
                var shot = game.Peas[0];
                var launchX = 40d + shot.PresentationLobX * 80d;
                var velocity = shot.VelocityX;
                var initialVelocity = shot.PresentationLobInitialVelocity;
                Assert.That(shot.PresentationMotionInitialized, Is.True, "Resolve the target when firing, before the projectile update.");
                if (!targetAtLaunch)
                {
                    Assert.That(velocity * 80f / 100f, Is.EqualTo((700d - launchX) / 120d).Within(.00001d));
                    Assert.That(initialVelocity, Is.EqualTo(-7f));
                }

                game.Zombies.Clear();
                var expectedTick = OriginalContactTick((float)Math.Round(launchX), shot.PresentationLobNativeVelocityX, initialVelocity, level == 42, weapon.Item2, shot.Splash, null, out var landingX);

                Assert.That(expectedTick, Is.GreaterThan(0));
                for (var tick = 1; tick <= expectedTick; tick++)
                {
                    Invoke(game, "AdventurePresentationTick", .01f);
                    Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
                    Assert.That(game.Peas.Count, Is.EqualTo(tick == expectedTick ? 0 : 1), weapon + " tick " + tick);
                }

                Assert.That(game.Presentation.Impacts.Count, Is.EqualTo(1));
                Assert.That(game.Presentation.Impacts[0].X, Is.EqualTo((landingX - 40f) / 80f).Within(.0001f));
                Assert.That(shot.VelocityX, Is.EqualTo(velocity), "Target death must not retarget a flying lob.");
            }
        }

        /// <summary>Checks all five lobber weapons against a zombie eating the real body or its pumpkin shell.</summary>
        /// <param name="pumpkin">Whether the actual native eating target is the protective shell.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void EatingTargetsUseZeroLeadAndTheOriginalNearRangeCollision(bool pumpkin)
        {
            foreach (var weapon in new[] { (PlantKind.CabbagePult, false), (PlantKind.KernelPult, false),
                (PlantKind.KernelPult, true), (PlantKind.MelonPult, false), (PlantKind.WinterMelon, false) })
            {
                var game = Board(42);
                Place(game, PlantKind.FlowerPot);
                var plant = Place(game, weapon.Item1);
                var eaten = pumpkin ? Place(game, PlantKind.Pumpkin) : plant;
                plant.PresentationButter = weapon.Item2;
                plant._kernelWeaponPrepared = true;
                // One pixel of overlap with the body's 130..190 or pumpkin's 120..220 eating rectangle.
                var nativeX = pumpkin ? 169 : 139;
                var zombie = new Zombie { Id = 500, Kind = ZombieKind.Normal, Row = 1,
                    X = AdventureZombieNativeRules.GridX(nativeX), Health = 270f, StartingHealth = 270f,
                    Eating = true, PresentationNativePhase = 0, PresentationNativeVelocity = .3f,
                    Speed = AdventureZombieNativeRules.GridSpeed(.3f) };
                game.Zombies.Add(zombie);
                Assert.That(Invoke(game, "FindAdventureNativePlantTarget", zombie, AdventureBiteAttack.Chew), Is.SameAs(eaten));
                Assert.That(Invoke(game, "FindAdventureLobberTarget", 1, 1), Is.SameAs(zombie));
                Invoke(game, "FireAdventureNativeTimedProjectile", plant);
                var shot = game.Peas[0];
                var launchX = 120f + (weapon.Item1 == PlantKind.CabbagePult ? 5f
                    : weapon.Item1 == PlantKind.KernelPult ? weapon.Item2 ? 12f : 19f : 25f);
                var expectedDistance = Math.Max(40f, nativeX + 36f + 21f - launchX - 30f);
                Assert.That(shot.PresentationLobNativeVelocityX, Is.EqualTo(expectedDistance / 120f), weapon + " stationary lead");
                var bodyTop = (int)(155f + (400f - nativeX) * .25f - 30f);
                var initialVelocity = -7f + (bodyTop - shot.PresentationLaunchY) / 120f;
                Assert.That(shot.PresentationLobInitialVelocity, Is.EqualTo(initialVelocity));
                var melon = weapon.Item1 == PlantKind.MelonPult || weapon.Item1 == PlantKind.WinterMelon;
                var expectedTick = OriginalContactTick(launchX, expectedDistance / 120f,
                    initialVelocity, true, weapon.Item2, melon, nativeX + 36, out _);

                Assert.That(expectedTick, Is.GreaterThan(0), weapon + " original contact must exist");
                for (var tick = 1; tick <= expectedTick; tick++)
                {
                    Invoke(game, "AdventurePresentationTick", .01f);
                    Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
                    Assert.That(game.Peas.Count, Is.EqualTo(tick == expectedTick ? 0 : 1), weapon + " contact at " + tick);
                }

                Assert.That(zombie.Health, Is.EqualTo(270f - (melon ? 80f : weapon.Item2 || weapon.Item1 == PlantKind.CabbagePult ? 40f : 20f)));
                Assert.That(game.Presentation.Impacts.Count, Is.EqualTo(1));
            }
        }

        /// <summary>Checks every lobber from roof column two reaches a live zombie in column nine.</summary>
        /// <param name="moving">Whether the production zombie walks during the complete flight.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void EveryLobberHitsColumnNineFromRoofColumnTwo(bool moving)
        {
            foreach (var weapon in new[] { (PlantKind.CabbagePult, false), (PlantKind.KernelPult, false),
                (PlantKind.KernelPult, true), (PlantKind.MelonPult, false), (PlantKind.WinterMelon, false) })
            {
                var game = Board(42);
                Place(game, PlantKind.FlowerPot);
                var plant = Place(game, weapon.Item1);
                plant.ActionTimer = 1000000f;
                plant.PresentationButter = weapon.Item2;
                plant._kernelWeaponPrepared = true;
                var zombie = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Normal, 1,
                    AdventureZombieNativeRules.GridX(720f), 0)!;
                zombie.X = AdventureZombieNativeRules.GridX(720f);
                zombie.PresentationPreviousX = zombie.X;
                if (!moving)
                {
                    zombie.FrozenRemaining = 1000f;
                }

                Invoke(game, "FireAdventureNativeTimedProjectile", plant);
                Assert.That(game.Peas.Count, Is.EqualTo(1));
                var startingHealth = zombie.Health;
                for (var tick = 0; tick < 220 && game.Peas.Count > 0; tick++)
                {
                    game.Update(.01f);
                }

                Assert.That(game.Peas, Is.Empty, weapon + " must finish its flight");
                Assert.That(zombie.Health, Is.LessThan(startingHealth), weapon + " must hit column nine; moving=" + moving);
                Assert.That(game.Presentation.Impacts.Count, Is.EqualTo(1));
            }
        }

        /// <summary>Checks PC 475AA0 aims at the leftmost body while 47C130 resolves overlapping hits by zombie position.</summary>
        [Test]
        public void LobberAimAndCollisionKeepTheirDistinctOriginalTargetPriorities()
        {
            foreach (var kind in new[] { PlantKind.CabbagePult, PlantKind.KernelPult, PlantKind.MelonPult, PlantKind.WinterMelon })
            {
                var game = Board(42);
                Place(game, PlantKind.FlowerPot);
                var plant = Place(game, kind);
                var vehicle = new Zombie { Id = 1, Kind = ZombieKind.Catapult, Row = 1,
                    X = AdventureZombieNativeRules.GridX(210f), Health = 850f };
                var walker = new Zombie { Id = 2, Kind = ZombieKind.Normal, Row = 1,
                    X = AdventureZombieNativeRules.GridX(200f), Health = 270f };
                game.Zombies.Add(vehicle);
                game.Zombies.Add(walker);
                Assert.That(Invoke(game, "FindAdventureLobberTarget", 1, 1), Is.SameAs(vehicle), "The wide vehicle's body begins farther left.");
                Invoke(game, "FireAdventureNativeTimedProjectile", plant);
                var shot = game.Peas[0];
                // A previously fired lob now overlaps both original body rectangles.
                shot.PresentationLobNativeX = 250f;
                shot.PresentationLobNativeVelocityX = 0f;
                shot.X = (250f - 40f) / 80f;
                shot.VelocityX = 0f;
                var damage = shot.Damage;
                Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
                Assert.That(walker.Health, Is.EqualTo(270f - damage));
                Assert.That(vehicle.Health, Is.EqualTo(shot.Splash ? 824f : 850f),
                    "Collision selects native mX; only a melon's secondary splash can damage the vehicle.");
                Assert.That(game.Peas, Is.Empty);
            }
        }

        /// <summary>Checks lobbers search the planted attack rectangle even when the target lies behind a weapon's muzzle.</summary>
        [Test]
        public void LobberTargetAdmissionUsesThePlantRectangleAndKeepsItsNativeEdgeContact()
        {
            var game = Board(42);
            var zombie = new Zombie { Id = 1, Row = 1, Health = 270f, X = AdventureZombieNativeRules.GridX(102f) };
            game.Zombies.Add(zombie);
            Assert.That(Invoke(game, "FindAdventureLobberTarget", 1, 1), Is.SameAs(zombie), "Body right edge 180 touches attack left 180.");
            zombie.X = AdventureZombieNativeRules.GridX(101f);
            Assert.That(Invoke(game, "FindAdventureLobberTarget", 1, 1), Is.Null);
        }

        /// <summary>Transcribes PC 47C880's float32 integration and 47DFF0 contact ordering independently of simulation state.</summary>
        /// <param name="launchX">The original integer muzzle x coordinate.</param>
        /// <param name="velocityX">The PC horizontal velocity in pixels per update.</param>
        /// <param name="velocityZ">The PC initial altitude velocity.</param>
        /// <param name="roof">Whether the native floor changes beneath the projectile.</param>
        /// <param name="butter">Whether the original butter collision threshold applies.</param>
        /// <param name="melon">Whether the original melon collision rectangle and threshold apply.</param>
        /// <param name="bodyLeft">The fixed forty-two-pixel zombie body interval, or null for ground-only contact.</param>
        /// <param name="landingX">Receives the actual float32 position at impact.</param>
        /// <returns>The original first contact update, or zero when the fixture has no contact.</returns>
        private static int OriginalContactTick(float launchX, float velocityX, float velocityZ,
            bool roof, bool butter, bool melon, int? bodyLeft, out float landingX)
        {
            var x = launchX;
            var z = 0f;
            var threshold = butter ? -32f : melon ? -35f : -30f;
            for (var tick = 1; tick < 250; tick++)
            {
                var previousX = x;
                var left = (int)previousX + (melon ? 20 : 0);
                velocityZ += .115f;
                x += velocityX;
                z += velocityZ;
                var admitted = tick <= 20 || velocityZ >= 0f && z > threshold;
                if (admitted && (z > 80f || bodyLeft.HasValue
                    && Math.Min(left + (melon ? 60 : 40), bodyLeft.Value + 42) >= Math.Max(left, bodyLeft.Value)))
                {
                    landingX = x;
                    return tick;
                }

                if (roof)
                {
                    // All roof fixtures use original row one, whose flat baseline is 155.
                    var previousFloor = 155f + Math.Max(0f, 440f - previousX) * .25f;
                    var floor = 155f + Math.Max(0f, 440f - x) * .25f;
                    z -= floor - previousFloor;
                }
            }

            landingX = x;
            return 0;
        }

        /// <summary>Creates a controlled board with no scheduled waves or preplanted objects.</summary>
        /// <param name="level">The original level selecting flat ground or roof terrain.</param>
        /// <returns>The playing production simulation.</returns>
        private static GameSimulation Board(int level)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Plants.Clear();
            game.Zombies.Clear();
            game.Adventure._graves.Clear();
            return game;
        }

        /// <summary>Creates a plant through the production initializer at a fixed roof cell.</summary>
        /// <param name="game">The board receiving the plant.</param>
        /// <param name="kind">The original species to place.</param>
        /// <returns>The production-created plant.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", kind, 1, 1)!;
        }

        /// <summary>Invokes a production entry point without replacing its behavior.</summary>
        /// <param name="game">The simulation under test.</param>
        /// <param name="method">The production method's name.</param>
        /// <param name="arguments">The production call's arguments.</param>
        /// <returns>The production method's result.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
