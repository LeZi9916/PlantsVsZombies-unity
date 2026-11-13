#nullable enable

using System;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original Scaredy-shroom circle, complete hiding cycle, and shooting ownership.</summary>
    public sealed class AdventureScaredyShroomTests
    {
        /// <summary>Checks the original circle uses the cell's left edge and real vertical rectangle distance.</summary>
        /// <param name="x">The potential zombie rectangle's left edge.</param>
        /// <param name="y">The potential zombie rectangle's top edge.</param>
        /// <param name="expected">Whether the original 120-pixel circle overlaps that rectangle.</param>
        [TestCase(400, 280, true)]
        [TestCase(401, 280, false)]
        [TestCase(376, 372, true)]
        [TestCase(377, 372, false)]
        public void FearUsesOriginalCircleAndRectangle(int x, int y, bool expected)
        {
            var plant = new Plant { Kind = PlantKind.ScaredyShroom, Column = 3, Row = 2 };
            var zombie = new Zombie { Row = 2, Health = 270f };
            Assert.That(AdventureScaredyShroomRules.CanScare(plant, zombie, new NativeRect(x, y, 42, 115), 280), Is.EqualTo(expected));
        }

        /// <summary>Checks fear excludes hypnotized, dying, and distant-lane actors but retains airborne actors.</summary>
        [Test]
        public void FearUsesOriginalHostilityAndLaneGates()
        {
            var plant = new Plant { Kind = PlantKind.ScaredyShroom, Column = 3, Row = 2 };
            var zombie = new Zombie { Row = 3, Health = 270f, Airborne = true };
            var body = new NativeRect(280, 280, 42, 115);
            Assert.That(AdventureScaredyShroomRules.CanScare(plant, zombie, body, 280), Is.True);
            zombie.Hypnotized = true;
            Assert.That(AdventureScaredyShroomRules.CanScare(plant, zombie, body, 280), Is.False);
            zombie.Hypnotized = false;
            zombie.PresentationNativePhase = 1;
            Assert.That(AdventureScaredyShroomRules.CanScare(plant, zombie, body, 280), Is.False);
            zombie.PresentationNativePhase = -1;
            zombie.Row = 4;
            Assert.That(AdventureScaredyShroomRules.CanScare(plant, zombie, body, 280), Is.False);
            zombie.Kind = ZombieKind.Boss;
            Assert.That(AdventureScaredyShroomRules.CanScare(plant, zombie, body, 280), Is.True);
        }

        /// <summary>Checks the production simulation plays all authored fear phases and restarts the full launch delay.</summary>
        [Test]
        public void NearbyZombieLowersThenLeavingRaisesBeforeFullReload()
        {
            var game = CreateGame();
            var plant = Place(game);
            game.Zombies.Add(CreateZombie(364f));
            game.Update(.01f);
            var state = plant.ScaredyShroomPresentation!;
            Assert.That(state.NativePhase, Is.EqualTo(20));
            Assert.That(state.Animation, Is.EqualTo("anim_scared"));
            Assert.That(state.Rate, Is.EqualTo(10f));
            Assert.That(plant.RequiredTiming.Countdown, Is.EqualTo(149));
            Tick(game, 129);
            Assert.That(state.NativePhase, Is.EqualTo(20));
            Tick(game, 1);
            Assert.That(state.NativePhase, Is.EqualTo(21));
            Assert.That(state.Animation, Is.EqualTo("anim_scaredidle"));
            Assert.That(state.Rate, Is.EqualTo(10f), "A zero PlayBodyReanim rate retains the previous ten fps.");
            Tick(game, 20);
            Assert.That(plant.RequiredTiming.Countdown, Is.EqualTo(149));
            Assert.That(game.Peas.Count, Is.Zero);

            game.Zombies.Clear();
            Tick(game, 1);
            Assert.That(state.NativePhase, Is.EqualTo(22));
            Assert.That(state.Animation, Is.EqualTo("anim_grow"));
            Assert.That(state.Rate, Is.InRange(7f, 12f));
            var raisingTicks = (int)Math.Ceiling(1000f / state.Rate);
            Tick(game, raisingTicks - 1);
            Assert.That(state.NativePhase, Is.EqualTo(22));
            Assert.That(plant.RequiredTiming.Countdown, Is.EqualTo(149));
            Tick(game, 1);
            Assert.That(state.NativePhase, Is.EqualTo(1));
            Assert.That(state.Animation, Is.EqualTo("anim_idle"));
            Assert.That(state.Rate, Is.InRange(10f, 15f));
            Assert.That(plant.RequiredTiming.Countdown, Is.EqualTo(148));

            game.Zombies.Add(CreateZombie(600f));
            Tick(game, 147);
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.Zero);
            Tick(game, 1);
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.EqualTo(25));
        }

        /// <summary>Checks a nearby enemy cannot interrupt the original fifteen-frame shooting animation.</summary>
        [Test]
        public void ActiveShotCompletesBeforeLoweringWithoutFiringTwice()
        {
            var game = CreateGame();
            var plant = Place(game);
            plant.Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw(PlantsVsZombies.Timing.PlantKind.ScaredyShroom, 1, 0);
            var zombie = CreateZombie(600f);
            game.Zombies.Add(zombie);
            Tick(game, 1);
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.EqualTo(25));
            zombie.X = AdventureZombieNativeRules.GridX(364f);
            Tick(game, 24);
            Assert.That(plant.ScaredyShroomPresentation!.NativePhase, Is.EqualTo(1));
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.EqualTo(1));
            Assert.That(game.Peas.Count, Is.EqualTo(1));
            Tick(game, 18);
            Assert.That(plant.ScaredyShroomPresentation.NativePhase, Is.EqualTo(1));
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.EqualTo(1));
            Assert.That(plant.PresentationShotAge, Is.EqualTo(.42f).Within(.0001f));
            Tick(game, 1);
            Assert.That(plant.ScaredyShroomPresentation.NativePhase, Is.EqualTo(20));
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.Zero);
            Assert.That(plant.PresentationShotAge, Is.EqualTo(.43f).Within(.0001f));
        }

        /// <summary>Checks sleeping mushrooms keep their sleep body instead of updating the fear state.</summary>
        [Test]
        public void SleepingMushroomDoesNotBeginHiding()
        {
            var game = CreateGame();
            var plant = Place(game);
            plant.Asleep = true;
            game.Zombies.Add(CreateZombie(364f));
            Tick(game, 1);
            Assert.That(plant.ScaredyShroomPresentation, Is.Null);
        }

        /// <summary>Checks the native top-edge branch retains signed underground heights without constructing an invalid rectangle.</summary>
        /// <param name="top">The clipped zombie body's native top edge.</param>
        /// <param name="height">The signed body height after underground clipping.</param>
        /// <param name="expected">Whether the original circle-to-rectangle branch accepts the body.</param>
        [TestCase(420, -85, true)]
        [TestCase(421, -85, false)]
        [TestCase(301, -500, true)]
        [TestCase(300, -120, true)]
        [TestCase(300, -121, false)]
        [TestCase(300, 0, true)]
        public void SignedUndergroundHeightPreservesOriginalTopEdgeOrder(int top, int height, bool expected)
        {
            var plant = new Plant { Kind = PlantKind.ScaredyShroom, Column = 3, Row = 2 };
            var zombie = new Zombie { Row = 2, Health = 270f };
            var body = new NativeRect(280, 250, 42, 115);
            Assert.That(AdventureScaredyShroomRules.CanScare(plant, zombie, body, top, height, 280), Is.EqualTo(expected));
        }

        /// <summary>Runs the complete production leader entrance, summon, and all 150 backup emergence updates with an awake mushroom.</summary>
        [Test]
        public void FullGameUpdateHandlesActualDancerSummonAndBackupEmergence()
        {
            var game = CreateGame();
            var plant = Place(game, 2, 0);
            plant.Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw(PlantsVsZombies.Timing.PlantKind.ScaredyShroom, 10000, 0);
            plant.ActionTimer = 100f;
            var leader = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { ZombieKind.Dancing, 2, AdventureZombieNativeRules.GridX(600f, .8f), 0 })!;
            Tick(game, leader.DancerState!.PhaseTicks);
            Assert.That(leader.PresentationNativePhase, Is.EqualTo(41));
            Tick(game, 37);
            Assert.That(game.Zombies.Count, Is.EqualTo(1));
            Tick(game, 1);
            Assert.That(game.Zombies.Count, Is.EqualTo(5));
            var backups = new Zombie[4];
            for (var slot = 0; slot < backups.Length; slot++)
            {
                backups[slot] = AdventureDancerRules.Find(game.Zombies, leader.DancerState.Followers[slot])!;
                Assert.That(backups[slot].PresentationNativePhase, Is.EqualTo(50));
                Assert.That(backups[slot].DancerState!.PhaseTicks, Is.EqualTo(150));
            }

            // The next full game update visits their -85-pixel clipped heights in the mushroom's fear loop.
            Tick(game, 149);
            foreach (var backup in backups)
            {
                Assert.That(backup.PresentationNativePhase, Is.EqualTo(50));
                Assert.That(backup.DancerState!.PhaseTicks, Is.EqualTo(1));
            }

            Tick(game, 1);
            foreach (var backup in backups)
            {
                Assert.That(backup.PresentationNativePhase, Is.EqualTo(44));
                Assert.That(backup.DancerState!.PhaseTicks, Is.Zero);
            }

            Assert.That(plant.ScaredyShroomPresentation!.NativePhase, Is.EqualTo(1), "Distant summons must complete without changing the original fear range.");
        }

        /// <summary>Runs the full grave ambush and preserves the original negative-height fear boundary during its final ascent.</summary>
        [Test]
        public void FullGameUpdatePreservesGraveEmergenceSignedFearBoundary()
        {
            var game = CreateGame();
            var plant = Place(game);
            plant.Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw(PlantsVsZombies.Timing.PlantKind.ScaredyShroom, 10000, 0);
            plant.ActionTimer = 100f;
            game.Adventure._graves.Clear();
            game.Adventure._graves.Add(new AdventureCellTimer { Row = 2, Column = 4, Remaining = float.MaxValue });
            typeof(GameSimulation).GetMethod("SpawnScheduledFinalAmbush", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, Array.Empty<object>());
            Assert.That(game.Zombies.Count, Is.EqualTo(1));
            var zombie = game.Zombies[0];
            Assert.That(zombie.GraveRiseTicks, Is.EqualTo(150));
            Tick(game, 118);
            Assert.That(zombie.GraveRiseTicks, Is.EqualTo(32));
            Assert.That(plant.ScaredyShroomPresentation!.NativePhase, Is.EqualTo(1));
            Tick(game, 1);
            Assert.That(zombie.GraveRiseTicks, Is.EqualTo(31));
            Assert.That(plant.ScaredyShroomPresentation.NativePhase, Is.EqualTo(20),
                "PC circle overlap accepts dx91/dy78 at rise counter32, even though the clipped height is -45.");
            Tick(game, 31);
            Assert.That(zombie.GraveRiseTicks, Is.Zero);
            Assert.That(plant.ScaredyShroomPresentation.NativePhase, Is.EqualTo(20));
        }

        /// <summary>Creates a night board with automatic wave spawning postponed beyond this regression.</summary>
        /// <returns>The active deterministic simulation.</returns>
        private static GameSimulation CreateGame()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Places an awake Scaredy-shroom through the production initialization path.</summary>
        /// <param name="game">The active night board.</param>
        /// <param name="row">The requested lawn row.</param>
        /// <param name="column">The requested lawn column.</param>
        /// <returns>The planted mushroom initialized through the production placement path.</returns>
        private static Plant Place(GameSimulation game, int row = 2, int column = 3)
        {
            return (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { PlantKind.ScaredyShroom, row, column })!;
        }

        /// <summary>Creates a stationary living enemy at an exact native board origin.</summary>
        /// <param name="nativeX">The original zombie body origin's x coordinate.</param>
        /// <returns>A frozen enemy that remains available for native fear checks.</returns>
        private static Zombie CreateZombie(float nativeX)
        {
            return new Zombie
            {
                Id = 91,
                Kind = ZombieKind.Normal,
                Row = 2,
                X = AdventureZombieNativeRules.GridX(nativeX),
                Health = 270f,
                StartingHealth = 270f,
                FrozenRemaining = 1000f
            };
        }

        /// <summary>Advances an exact number of original 100 Hz simulation updates.</summary>
        /// <param name="game">The active board being checked.</param>
        /// <param name="ticks">The number of original updates.</param>
        private static void Tick(GameSimulation game, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                game.Update(.01f);
            }
        }
    }
}
