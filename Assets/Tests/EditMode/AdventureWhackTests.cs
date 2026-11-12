#nullable enable

using System;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the PC Whack-a-Zombie hammer, emergence, and native spawn counters.</summary>
    public sealed class AdventureWhackTests
    {
        /// <summary>Checks the native body origin, inclusive circle contact, and negative-height grave branch.</summary>
        /// <param name="riseTicks">The original grave emergence counter.</param>
        /// <param name="missClickY">The board click rejected by the original overlap branch.</param>
        /// <param name="hitClickY">The board click accepted by the original overlap branch.</param>
        [TestCase(0, 224f, 225f)]
        [TestCase(50, 470f, 469f)]
        public void HammerInputUsesOriginalCircleAndGraveRectangle(int riseTicks, float missClickY, float hitClickY)
        {
            var host = new UnityEngine.GameObject("Inactive Whack input fixture");
            host.SetActive(false);
            var controller = host.AddComponent<PvZGame>();
            try
            {
                controller._game = Fixture();
                var zombie = Spawn(controller._game, ZombieKind.Normal);
                zombie.X = AdventureZombieNativeRules.GridX(400f);
                zombie.GraveRiseTicks = riseTicks;
                var hammer = typeof(PvZGame).GetMethod("TryHammerAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!;
                hammer.Invoke(controller, new object[] { new UnityEngine.Vector2(457f, missClickY) });
                Assert.That(controller._game.Zombies, Does.Contain(zombie));
                Assert.That(controller._game.Presentation.Explosions, Is.Empty);

                // PC 0x428460 checks the near edge first, even when a rise makes the rectangle height negative.
                // Therefore y=449 is accepted above a top edge of 450, while y=450 is measured from bottom 275.
                hammer.Invoke(controller, new object[] { new UnityEngine.Vector2(457f, hitClickY) });
                Assert.That(controller._game.Zombies, Is.Empty);
                Assert.That(controller._game.Presentation.Explosions, Has.Count.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        /// <summary>Checks PC 0x42D390 uses Pow at the click and removes its victim without a corpse.</summary>
        [Test]
        public void BareHeadHitUsesOriginalPowAndImmediateDeath()
        {
            var game = Fixture();
            var zombie = Spawn(game, ZombieKind.Normal);
            Assert.That(game.HammerAdventureZombie(zombie.Id, 420f, 280f), Is.True);
            Assert.That(game.Zombies, Is.Empty);
            Assert.That(game.Presentation.Corpses, Is.Empty);
            Assert.That(game.Presentation.ArmDrops, Is.Empty);
            Assert.That(game.Presentation.ZombiePartDrops, Is.Empty);
            Assert.That(game.Adventure.Coins, Has.Count.EqualTo(1), "Immediate hammer death still follows the original loot branch.");
            Assert.That(game.Adventure.Coins[0].Kind, Is.EqualTo(AdventureCoinKind.Silver));
            Assert.That(game.Presentation.Explosions, Has.Count.EqualTo(1));
            var hit = game.Presentation.Explosions[0];
            Assert.That(hit.Effect, Is.EqualTo(AdventureExplosionEffect.Pow));
            Assert.That(hit.Particle, Is.EqualTo("Pow"));
            Assert.That(hit.NativeX, Is.EqualTo(417f));
            Assert.That(hit.NativeY, Is.EqualTo(289f));
            Assert.That(hit.Overlay, Is.True);
            Assert.That(hit.DurationTicks, Is.EqualTo(50));
            Assert.That(game.HammerAdventureZombie(zombie.Id, 420f, 280f), Is.False);
            Assert.That(game.Presentation.Explosions, Has.Count.EqualTo(1));
            Assert.That(game.Adventure.Coins, Has.Count.EqualTo(1));

            for (var tick = 0; tick < 49; tick++)
            {
                Invoke(game, "AdvanceAdventureExplosionPresentation");
            }

            Assert.That(game.Presentation.Explosions, Has.Count.EqualTo(1));
            Invoke(game, "AdvanceAdventureExplosionPresentation");
            Assert.That(game.Presentation.Explosions, Is.Empty);
        }

        /// <summary>Checks the native 900-point helmet hit never spills damage into the body.</summary>
        /// <param name="kind">The original helmet-bearing zombie type.</param>
        /// <param name="afterFirstHit">The helmet health remaining after the first hammer hit.</param>
        [TestCase(ZombieKind.Conehead, 0f)]
        [TestCase(ZombieKind.Buckethead, 200f)]
        public void HelmetHitsPreserveBodyUntilTheNextBareHeadHit(ZombieKind kind, float afterFirstHit)
        {
            var game = Fixture();
            var zombie = Spawn(game, kind);
            var bodyHealth = zombie.Health;
            Assert.That(game.HammerAdventureZombie(zombie.Id, 420f, 280f), Is.True);
            Assert.That(zombie.ArmorHealth, Is.EqualTo(afterFirstHit));
            Assert.That(zombie.Health, Is.EqualTo(bodyHealth));
            Assert.That(game.Zombies, Does.Contain(zombie));
            Assert.That(game.Presentation.Explosions, Is.Empty);

            if (zombie.ArmorHealth > 0f)
            {
                Assert.That(game.HammerAdventureZombie(zombie.Id, 420f, 280f), Is.True);
                Assert.That(zombie.ArmorHealth, Is.Zero);
                Assert.That(zombie.Health, Is.EqualTo(bodyHealth));
                Assert.That(game.Zombies, Does.Contain(zombie));
                Assert.That(game.Presentation.Explosions, Is.Empty);
            }

            Assert.That(game.HammerAdventureZombie(zombie.Id, 420f, 280f), Is.True);
            Assert.That(game.Zombies, Is.Empty);
            Assert.That(game.Presentation.Corpses, Is.Empty);
            Assert.That(game.Presentation.Explosions, Has.Count.EqualTo(1));
        }

        /// <summary>Checks fifty native emergence ticks preserve the Whack velocity before walking resumes.</summary>
        [Test]
        public void GraveEmergenceKeepsItsNativeVelocityAndBlocksMovementForFiftyTicks()
        {
            var game = Fixture();
            game.Adventure.CurrentWave = 1;
            game.Adventure.WaveCountdown = 20f;
            game.Adventure._whackSpawnTimer = .01f;
            Invoke(game, "TickAdventureWhack", .01f);
            Assert.That(game.Zombies, Has.Count.EqualTo(1));
            var zombie = game.Zombies[0];
            var startX = zombie.X;
            var velocity = zombie.PresentationNativeVelocity;
            Assert.That(velocity, Is.EqualTo(.75f).Within(.00001f), "The original random range is in pixels per tick.");
            Assert.That(zombie.Speed, Is.EqualTo(AdventureZombieNativeRules.GridSpeed(velocity)).Within(.00001f));
            Assert.That(zombie.GraveRiseTicks, Is.EqualTo(50));
            Assert.That(zombie.DropRemaining, Is.Zero);
            Assert.That(game.Presentation.ZombieRiseEffects[0].Whack, Is.True);

            for (var tick = 0; tick < 50; tick++)
            {
                Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
                Invoke(game, "AdvanceAdventureZombies", .01f);
                Assert.That(zombie.X, Is.EqualTo(startX));
            }

            Assert.That(zombie.GraveRiseTicks, Is.Zero);
            Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
            Invoke(game, "AdvanceAdventureZombies", .01f);
            Assert.That(zombie.PresentationNativeVelocity, Is.EqualTo(velocity));
            Assert.That(zombie.X, Is.LessThan(startX));
        }

        /// <summary>Checks PC 0x43139E counts the wave-entry update in the three/three-hundred-tick delay.</summary>
        /// <param name="previousWave">The completed wave before the transition.</param>
        /// <param name="delayTicks">The original number of updates before the next batch.</param>
        [TestCase(0, 3)]
        [TestCase(7, 300)]
        public void WaveEntryWaitsForOriginalSpawnDelay(int previousWave, int delayTicks)
        {
            var game = Fixture();
            game.Adventure.CurrentWave = previousWave;
            game.Adventure.WaveCountdown = .01f;
            game.Adventure._whackGravesRaised = true;
            for (var tick = 0; tick < delayTicks - 1; tick++)
            {
                Invoke(game, "TickAdventureWhack", .01f);
                Assert.That(game.Zombies, Is.Empty);
            }

            Assert.That(game.Adventure.CurrentWave, Is.EqualTo(previousWave + 1));
            Invoke(game, "TickAdventureWhack", .01f);
            Assert.That(game.Zombies, Has.Count.EqualTo(1));
            if (previousWave == 7)
            {
                Assert.That(game.Adventure._finalAmbush, Is.True);
                Assert.That(game.Adventure.WaveCountdown, Is.Zero);
            }
        }

        /// <summary>Checks the PC integer curve results and inclusive spawn interval bounds.</summary>
        /// <param name="wave">The active one-based adventure wave.</param>
        /// <param name="maximumSpeed">The rounded native maximum velocity.</param>
        /// <param name="minimumTicks">The original minimum spawn interval.</param>
        /// <param name="maximumTicks">The original maximum spawn interval.</param>
        [TestCase(2, 1f, 94, 187)]
        [TestCase(6, 1f, 68, 136)]
        [TestCase(7, 2f, 62, 124)]
        [TestCase(8, 2f, 55, 111)]
        public void SpawnUsesNativeIntegerCurves(int wave, float maximumSpeed, int minimumTicks, int maximumTicks)
        {
            var game = Fixture();
            game.Adventure.CurrentWave = wave;
            game.Adventure.WaveCountdown = 20f;
            game.Adventure._whackSpawnTimer = .01f;
            Invoke(game, "TickAdventureWhack", .01f);
            Assert.That(game.Zombies, Has.Count.EqualTo(1));
            Assert.That(game.Zombies[0].PresentationNativeVelocity, Is.EqualTo((.5f + maximumSpeed) / 2f).Within(.00001f));
            var random = (FixedRandom)typeof(GameSimulation).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(game)!;
            Assert.That(random.Minimum, Is.EqualTo(minimumTicks));
            Assert.That(random.ExclusiveMaximum, Is.EqualTo(maximumTicks + 1));
        }

        /// <summary>Creates a private-state fixture with one eligible grave and deterministic midpoint velocities.</summary>
        /// <returns>The isolated Whack-a-Zombie simulation.</returns>
        private static GameSimulation Fixture()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 15 }, 1570935);
            game.Adventure._graves.Clear();
            game.Adventure._graves.Add(new AdventureCellTimer { Row = 2, Column = 6, Remaining = float.MaxValue });
            typeof(GameSimulation).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, new FixedRandom());
            return game;
        }

        /// <summary>Creates an explicitly isolated zombie through the actual production initialization path.</summary>
        /// <param name="game">The isolated simulation.</param>
        /// <param name="kind">The requested zombie and helmet type.</param>
        /// <returns>The initialized live zombie.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind)
        {
            return (Zombie)Invoke(game, "SpawnAdventureZombie", kind, 2, 6.5f, 0)!;
        }

        /// <summary>Invokes a production hook without broadening its runtime visibility.</summary>
        /// <param name="game">The isolated simulation.</param>
        /// <param name="name">The private hook name.</param>
        /// <param name="arguments">The hook arguments.</param>
        /// <returns>The hook result, or null for a void hook.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }

        /// <summary>Fixes test randomness while recording the production inclusive interval request.</summary>
        private sealed class FixedRandom : Random
        {
            /// <summary>Gets the last interval's inclusive lower bound.</summary>
            public int Minimum { get; private set; }

            /// <summary>Gets the last interval's exclusive upper bound.</summary>
            public int ExclusiveMaximum { get; private set; }

            /// <summary>Selects one ordinary zombie and an explicit silver-coin loot roll.</summary>
            /// <param name="maxValue">The exclusive upper bound.</param>
            /// <returns>The silver-coin ticket for loot, otherwise the last valid index or zero.</returns>
            public override int Next(int maxValue)
            {
                if (maxValue == 30000)
                {
                    return 1000;
                }

                return Math.Max(0, maxValue - 1);
            }

            /// <summary>Records an interval and returns its lower bound.</summary>
            /// <param name="minValue">The inclusive lower bound.</param>
            /// <param name="maxValue">The exclusive upper bound.</param>
            /// <returns>The requested lower bound.</returns>
            public override int Next(int minValue, int maxValue)
            {
                Minimum = minValue;
                ExclusiveMaximum = maxValue;
                return minValue;
            }

            /// <summary>Returns a midpoint sample for native velocity interpolation.</summary>
            /// <returns>One half.</returns>
            public override double NextDouble()
            {
                return .5d;
            }
        }
    }
}
