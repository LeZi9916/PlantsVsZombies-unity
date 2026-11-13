#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the local PC executable's ambient vocal countdown, population draw, and species gates.</summary>
    public sealed class AdventureZombieGroanTests
    {
        /// <summary>Checks both inclusive native initialization boundaries and the first constructor draw.</summary>
        /// <param name="lastTicket">Whether the original bounded draw chooses its last valid value.</param>
        /// <param name="ticks">The expected initial native countdown.</param>
        [TestCase(false, 400)]
        [TestCase(true, 500)]
        public void InitialVocalCountdownUsesOriginalHundredAndOneTickets(bool lastTicket, int ticks)
        {
            var game = Board();
            var random = Attach(game, lastTicket);
            var zombie = Spawn(game, ZombieKind.Normal);
            Assert.That(random.Bounds[0], Is.EqualTo(101));
            Assert.That(zombie.GroanTicks, Is.EqualTo(ticks));
        }

        /// <summary>Checks original species and initialization variants select the unchanged PC Foley categories.</summary>
        /// <param name="kind">The original actor species.</param>
        /// <param name="variant">The one-in-five native initialization variant.</param>
        /// <param name="foley">The expected original vocal category.</param>
        [TestCase(ZombieKind.Normal, false, 11)]
        [TestCase(ZombieKind.Normal, true, 12)]
        [TestCase(ZombieKind.Gargantuar, true, 70)]
        public void OriginalVocalAttemptUsesSpeciesAndVariant(ZombieKind kind, bool variant, int foley)
        {
            var game = Board();
            var zombie = Spawn(game, kind);
            zombie.NativeVariant = variant;
            zombie.GroanTicks = 2;
            var random = Attach(game);
            Groan(game, zombie);
            Assert.That(game.Presentation.ZombieSounds, Is.Empty);
            Assert.That(random.Bounds, Is.Empty);
            Groan(game, zombie);
            Assert.That(game.Presentation.ZombieSounds, Has.Count.EqualTo(1));
            Assert.That(game.Presentation.ZombieSounds[0].Foley, Is.EqualTo(foley));
            Assert.That(game.Presentation.ZombieSounds[0].Pitch, Is.Zero);
            Assert.That(random.Bounds, Is.EqualTo(new[] { 1, 1000 }));
            Assert.That(zombie.GroanTicks, Is.EqualTo(500));
        }

        /// <summary>Checks the PC equality branch attempts once and does not retry every tick after a population miss.</summary>
        [Test]
        public void PopulationMissLeavesCounterNegativeWithoutRepeatedAttempts()
        {
            var game = Board();
            var zombie = Spawn(game, ZombieKind.Normal);
            Spawn(game, ZombieKind.Normal);
            zombie.GroanTicks = 1;
            var random = Attach(game, true);
            Groan(game, zombie);
            Groan(game, zombie);
            Assert.That(random.Bounds, Is.EqualTo(new[] { 2 }));
            Assert.That(zombie.GroanTicks, Is.EqualTo(-1));
            Assert.That(game.Presentation.ZombieSounds, Is.Empty);
        }

        /// <summary>Checks head loss, dying actors, the boss, preview actors, and winners cannot produce new ambient vocals.</summary>
        /// <param name="kind">The tested original species.</param>
        /// <param name="headless">Whether this actor lost its head.</param>
        /// <param name="phase">The original actor phase.</param>
        /// <param name="wave">The original source wave, including preview and winner sentinels.</param>
        [TestCase(ZombieKind.Normal, true, -1, 0)]
        [TestCase(ZombieKind.Normal, false, 1, 0)]
        [TestCase(ZombieKind.Normal, false, 2, 0)]
        [TestCase(ZombieKind.Normal, false, 3, 0)]
        [TestCase(ZombieKind.Boss, false, -1, 0)]
        [TestCase(ZombieKind.Normal, false, -1, -2)]
        [TestCase(ZombieKind.Normal, false, -1, -3)]
        [TestCase(ZombieKind.Normal, false, -1, -4)]
        public void InvalidVocalActorsRemainSilent(ZombieKind kind, bool headless, int phase, int wave)
        {
            var game = Board();
            var zombie = new Zombie { Kind = kind, Health = 270f, GroanTicks = 1, Headless = headless, PresentationNativePhase = phase, Wave = wave };
            game.Zombies.Add(zombie);
            Attach(game);
            Groan(game, zombie);
            Assert.That(game.Presentation.ZombieSounds, Is.Empty);
        }

        /// <summary>Checks original immobilization does not stop ambient vocal timing, while board pause does.</summary>
        [Test]
        public void FrozenActorsCanGroanButPausedBoardsAndSeedPreviewsCannot()
        {
            var game = Board();
            var zombie = Spawn(game, ZombieKind.Normal);
            zombie.GroanTicks = 1;
            zombie.FrozenRemaining = 10f;
            zombie.PresentationButter = 10f;
            Attach(game);
            game.TogglePause();
            game.Update(.1f);
            Assert.That(zombie.GroanTicks, Is.EqualTo(1));
            game.TogglePause();
            game.Update(.01f);
            Assert.That(game.Presentation.ZombieSounds, Has.Count.EqualTo(1));
            zombie.GroanTicks = 1;
            game.Adventure.Phase = AdventurePhase.ChoosingSeeds;
            Groan(game, zombie);
            Assert.That(zombie.GroanTicks, Is.EqualTo(1));
            Assert.That(game.Presentation.ZombieSounds, Has.Count.EqualTo(1));
        }

        /// <summary>Checks Little Trouble uses the PC explicit forty-to-fifty pitch interval except for the giant vocal.</summary>
        /// <param name="kind">The original actor species.</param>
        /// <param name="pitch">The expected pitch for the controlled midpoint ticket.</param>
        [TestCase(ZombieKind.Normal, 45f)]
        [TestCase(ZombieKind.Gargantuar, 0f)]
        public void LittleTroubleUsesOriginalExplicitPitch(ZombieKind kind, float pitch)
        {
            var game = Board(25);
            var zombie = Spawn(game, kind);
            zombie.GroanTicks = 1;
            Attach(game);
            Groan(game, zombie);
            Assert.That(game.Presentation.ZombieSounds[0].Pitch, Is.EqualTo(pitch));
        }

        /// <summary>Creates a playing board without automatic waves or unrelated plants.</summary>
        /// <param name="level">The original adventure board number.</param>
        /// <returns>The quiet production simulation.</returns>
        private static GameSimulation Board(int level = 18)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            if (game.Adventure.Phase == AdventurePhase.ChoosingSeeds)
            {
                Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            }

            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Creates one actual actor using its original constructor path.</summary>
        /// <param name="game">The board receiving the actor.</param>
        /// <param name="kind">The original actor species.</param>
        /// <returns>The initialized actor.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind)
        {
            return (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { kind, 1, 6f, 0 })!;
        }

        /// <summary>Runs only the original ambient vocal boundary.</summary>
        /// <param name="game">The production board.</param>
        /// <param name="zombie">The actor whose counter is advanced.</param>
        private static void Groan(GameSimulation game, Zombie zombie)
        {
            typeof(GameSimulation).GetMethod("AdvanceAdventureZombieGroan", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { zombie });
        }

        /// <summary>Replaces unrelated random history with controlled valid tickets at the tested boundary.</summary>
        /// <param name="game">The board receiving the controlled stream.</param>
        /// <param name="lastTicket">Whether bounded draws select the final valid ticket.</param>
        /// <returns>The stream retaining its original bounded call arguments.</returns>
        private static VocalRandom Attach(GameSimulation game, bool lastTicket = false)
        {
            var random = new VocalRandom(lastTicket);
            typeof(GameSimulation).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, random);
            return random;
        }

        /// <summary>Records original population and countdown ranges while supplying deterministic valid tickets.</summary>
        private sealed class VocalRandom : Random
        {
            /// <summary>Retains the exclusive bound of each original bounded draw.</summary>
            internal List<int> Bounds { get; } = new List<int>();
            /// <summary>Stores whether each bounded draw selects the final valid ticket.</summary>
            private readonly bool _lastTicket;
            /// <summary>Creates a controlled stream.</summary>
            /// <param name="lastTicket">Whether to select each range's final ticket.</param>
            internal VocalRandom(bool lastTicket)
            {
                _lastTicket = lastTicket;
            }

            /// <summary>Returns and records one original bounded ticket.</summary>
            /// <param name="maxValue">The exclusive upper bound.</param>
            /// <returns>The first or last valid ticket.</returns>
            public override int Next(int maxValue)
            {
                Bounds.Add(maxValue);
                return _lastTicket ? maxValue - 1 : 0;
            }

            /// <summary>Returns a valid raw ticket used by unrelated original float sampling.</summary>
            /// <returns>The nonzero raw ticket one.</returns>
            public override int Next()
            {
                return 1;
            }

            /// <summary>Returns the midpoint used by explicit native pitch sampling.</summary>
            /// <returns>One half.</returns>
            public override double NextDouble()
            {
                return .5;
            }
        }
    }
}
