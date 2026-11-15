#nullable enable
using System.Linq;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Tests.Shared;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC plant-zombie allocation and 542E50/543040/5431D0/543520 actions without running unrelated levels.</summary>
    public sealed class PlantZombieLogicTests
    {
        /// <summary>Checks native health, helms and walking state exist before the new actor becomes visible.</summary>
        /// <param name="kind">The original plant-headed species.</param>
        /// <param name="health">The original body health.</param>
        /// <param name="armor">The original head/helm health.</param>
        [TestCase(26, 270, 0)]
        [TestCase(27, 270, 1100)]
        [TestCase(28, 500, 0)]
        [TestCase(29, 270, 0)]
        [TestCase(30, 270, 0)]
        [TestCase(31, 270, 2200)]
        public void NativeAllocationPublishesCompletePlantZombieState(int kind, int health, int armor)
        {
            var session = Board();
            var zombie = Spawn(session.Game, kind);
            Assert.That(zombie.ModeHead, Is.EqualTo(kind));
            Assert.That(zombie.Kind, Is.EqualTo(ZombieKind.Normal));
            Assert.That(zombie.Health, Is.EqualTo(health));
            Assert.That(zombie.StartingHealth, Is.EqualTo(health));
            Assert.That(zombie.ArmorHealth, Is.EqualTo(armor));
            Assert.That(zombie.PresentationMaxArmor, Is.EqualTo(armor));
            Assert.That(zombie.PresentationWalk2, Is.True);
            Assert.That(zombie.PlantZombieState, Is.Not.Null);
            if (kind == 28)
            {
                var ticket = zombie.PlantZombieState!.Counter / 2;
                Assert.That(ticket * zombie.PresentationNativeVelocity, Is.InRange(274f, 450f));
                Assert.That(zombie.PlantZombieState.Counter % 2, Is.Zero);
            }
        }

        /// <summary>Checks the literal PC 35/100 shooting markers and 18/35/51/68 gatling launch markers.</summary>
        /// <param name="kind">The original shooter species.</param>
        /// <param name="start">The original shooting-animation counter.</param>
        /// <param name="rate">The original shooting-animation rate.</param>
        [TestCase(26, 35, 35)]
        [TestCase(29, 100, 38)]
        public void ShooterUsesNativeIntegerCountersAndClipRates(int kind, int start, int rate)
        {
            var session = Board();
            var zombie = Spawn(session.Game, kind);
            var state = zombie.PlantZombieState!;
            state.Counter = start + 1;
            TickActions(session.Game);
            Assert.That(state.Counter, Is.EqualTo(start));
            Assert.That(state.HeadClip, Is.EqualTo("anim_shooting"));
            Assert.That(state.HeadRate, Is.EqualTo(rate));
            Assert.That(state.HeadLoops, Is.False);
            Assert.That(session.HeadShots, Is.Empty);
            foreach (var counter in kind == 26 ? new[] { 0 } : new[] { 68, 51, 35, 18 })
            {
                session.HeadShots.Clear();
                state.Counter = counter + 1;
                TickActions(session.Game);
                Assert.That(session.HeadShots.Count, Is.EqualTo(1));
                Assert.That(session.HeadShots[0].NativeX, Is.EqualTo(session.HeadShots[0].PreviousNativeX), "A newly allocated projectile is first moved on the next board update.");
            }
            state.Counter = 1;
            TickActions(session.Game);
            Assert.That(state.Counter, Is.EqualTo(150));
            Assert.That(state.HeadClip, Is.EqualTo("anim_head_idle"));
        }

        /// <summary>Checks original action counters ignore walking chill but pause for immobilization.</summary>
        [Test]
        public void CounterIgnoresChillAndPausesForIceOrButter()
        {
            var session = Board();
            var zombie = Spawn(session.Game, 26);
            zombie.ChillRemaining = 10;
            TickActions(session.Game);
            Assert.That(zombie.PlantZombieState!.Counter, Is.EqualTo(149));
            zombie.FrozenRemaining = 10;
            TickActions(session.Game);
            Assert.That(zombie.PlantZombieState.Counter, Is.EqualTo(149));
            zombie.FrozenRemaining = 0;
            zombie.PresentationButter = 10;
            TickActions(session.Game);
            Assert.That(zombie.PlantZombieState.Counter, Is.EqualTo(149));
        }

        /// <summary>Checks the PC child clock remains independent of ice/chill while ApplyButter explicitly stops it.</summary>
        [Test]
        public void IndependentHeadClockStopsForButterRatherThanBodyChill()
        {
            var session = Board();
            var zombie = Spawn(session.Game, 29);
            zombie.FrozenRemaining = 10;
            SimulationInspection.GetMethod("AdvanceAdventureZombieClock").Invoke(session.Game, new object[] { zombie, .01f });
            Assert.That(zombie.PlantZombieState!.HeadSeconds, Is.EqualTo(.01f).Within(.000001f));
            zombie.PresentationButter = 10;
            SimulationInspection.GetMethod("AdvanceAdventureZombieClock").Invoke(session.Game, new object[] { zombie, .01f });
            Assert.That(zombie.PlantZombieState.HeadSeconds, Is.EqualTo(.01f).Within(.000001f));
        }

        /// <summary>Checks PC 543520's delayed whole-cell crush, thump hold and final body damage.</summary>
        [Test]
        public void SquashDetachesAndCrushesAtOriginalFallingCounterTwo()
        {
            var session = Board();
            var zombie = Spawn(session.Game, 30, 4f);
            zombie.Eating = true;
            TickActions(session.Game);
            var state = zombie.PlantZombieState!;
            Assert.That(state.Detached && zombie.Headless, Is.True);
            Assert.That(state.SquashPhase, Is.EqualTo(93));
            Assert.That(state.Counter, Is.EqualTo(95));
            Assert.That(state.HeadClip, Is.EqualTo("anim_jumpup"));
            state.Counter = 1;
            TickActions(session.Game);
            Assert.That(state.SquashPhase, Is.EqualTo(94));
            Assert.That(state.Counter, Is.EqualTo(10));
            Assert.That(state.HeadRate, Is.EqualTo(60));
            state.Counter = 1;
            TickActions(session.Game);
            Assert.That(state.SquashPhase, Is.EqualTo(95));
            Assert.That(state.Counter, Is.EqualTo(100));
            state.Counter = 1;
            TickActions(session.Game);
            Assert.That(state.HeadRemoved, Is.True);
            Assert.That(session.Game.Zombies, Has.No.Member(zombie));
        }

        /// <summary>Checks PC 543040 does not add the mobile port's hypnotized row-burn variant.</summary>
        [Test]
        public void JalapenoKeepsPcPlantRowAttackAndOriginalFwoosh()
        {
            var session = Board();
            var zombie = Spawn(session.Game, 28);
            zombie.Hypnotized = true;
            var plant = new Plant { Id = 9001, Kind = PlantKind.Wallnut, Row = 1, Column = 3, Health = 4000 };
            SimulationFixture.Plants(session.Game).Add(plant);
            zombie.PlantZombieState!.Counter = 1;
            TickActions(session.Game);
            Assert.That(session.Game.Plants, Has.No.Member(plant));
            Assert.That(session.Game.Zombies, Has.No.Member(zombie));
            Assert.That(session.Game.Presentation.JalapenoFires.Count, Is.EqualTo(1));
            Assert.That(session.Game.Presentation.ScreenShake.TicksRemaining, Is.GreaterThan(0));
        }

        /// <summary>Checks native plain-body pool setup is retained for every plant head.</summary>
        /// <param name="kind">The original plant-headed species.</param>
        [TestCase(26)]
        [TestCase(27)]
        [TestCase(28)]
        [TestCase(29)]
        [TestCase(30)]
        [TestCase(31)]
        public void AllPlantHeadsRetainTheirOriginalPoolTube(int kind)
        {
            var session = new ModeSession(32);
            var zombie = (Zombie)SimulationInspection.GetMethod("SpawnAdventureZombie").Invoke(session.Game,
                new object[] { (ZombieKind)kind, 2, 9.7f, 0 })!;
            Assert.That(zombie.PoolState?.HasDuckyTube, Is.True);
            Assert.That(zombie.ModeHead, Is.EqualTo(kind));
        }

        /// <summary>Checks original backwards velocity, short-plant immunity and pumpkin-first collision.</summary>
        [Test]
        public void HostilePeasUseNativeVelocityAndShortPlantImmunity()
        {
            var session = Board();
            var puff = new Plant { Id = 9002, Kind = PlantKind.PuffShroom, Row = 1, Column = 3, Health = 300 };
            SimulationFixture.Plants(session.Game).Add(puff);
            var shot = new ModeHeadShot { NativeX = 280, PreviousNativeX = 280, NativeY = 180, Row = 1 };
            session.HeadShots.Add(shot);
            TickActions(session.Game);
            Assert.That(shot.NativeX, Is.EqualTo(276.67f).Within(.0001f));
            Assert.That(puff.Health, Is.EqualTo(300));
            Assert.That(session.HeadShots, Has.Member(shot));
            var shell = new Plant { Id = 9003, Kind = PlantKind.Pumpkin, Layer = PlantLayer.Armor, Row = 1, Column = 3, Health = 4000 };
            SimulationFixture.Plants(session.Game).Add(shell);
            shot.NativeX = 290;
            TickActions(session.Game);
            Assert.That(shell.Health, Is.EqualTo(3980));
            Assert.That(puff.Health, Is.EqualTo(300));
            Assert.That(session.HeadShots, Is.Empty);
        }

        /// <summary>Checks the native whole-cell squash contact occurs only at falling counter two.</summary>
        [Test]
        public void SquashCrushWaitsUntilNativeCounterTwo()
        {
            var session = Board();
            var zombie = Spawn(session.Game, 30, 4f);
            zombie.Eating = true;
            TickActions(session.Game);
            var plant = new Plant { Id = 9004, Kind = PlantKind.Wallnut, Row = 1, Column = 3, Health = 4000 };
            SimulationFixture.Plants(session.Game).Add(plant);
            var state = zombie.PlantZombieState!;
            state.SquashPhase = 94;
            state.Counter = 4;
            TickActions(session.Game);
            Assert.That(session.Game.Plants, Has.Member(plant));
            TickActions(session.Game);
            Assert.That(session.Game.Plants, Has.No.Member(plant));
            Assert.That(session.Game.Presentation.SquishedPlants.Any(item => item.Plant == plant), Is.True);
        }

        /// <summary>Creates only the original Pea Heads board and puts its simulation in the playing state.</summary>
        /// <returns>The isolated native mode session.</returns>
        private static ModeSession Board()
        {
            var session = new ModeSession(16);
            Assert.That(session.ChooseSeeds(session.Game.Adventure.AvailableSeeds.Take(6)), Is.True);
            return session;
        }

        /// <summary>Allocates an original native plant-headed zombie through the production spawning path.</summary>
        /// <param name="game">The actual simulation.</param>
        /// <param name="kind">The original species identifier.</param>
        /// <param name="x">The requested logical lawn x.</param>
        /// <returns>The completely initialized actor.</returns>
        private static Zombie Spawn(GameSimulation game, int kind, float x = 7f)
        {
            return (Zombie)SimulationInspection.GetMethod("SpawnAdventureZombie").Invoke(game, new object[] { (ZombieKind)kind, 1, x, 0 })!;
        }

        /// <summary>Advances only the production native head and projectile actions.</summary>
        /// <param name="game">The isolated simulation.</param>
        private static void TickActions(GameSimulation game)
        {
            SimulationInspection.GetMethod("TickModeSpecialZombies").Invoke(game, new object[] { .01f });
        }
    }
}
