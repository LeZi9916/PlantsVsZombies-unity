#nullable enable

using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks native Redeye weapon initialization and complete roof endless wave spawning.</summary>
    public sealed class AdventureRedeyeSpawnTests
    {
        /// <summary>Checks PC 53DD90's shared giant weapon draw survives native type-32 normalization and the first update.</summary>
        /// <param name="seed">The controlled production random seed.</param>
        /// <param name="weapon">The expected native weapon: pole, sign, or zombie.</param>
        [TestCase(0, 0)]
        [TestCase(2, 2)]
        [TestCase(10, 1)]
        [TestCase(15, 1)]
        public void RedeyeRetainsTheOriginalWeaponDraw(int seed, int weapon)
        {
            var ordinaryBoard = Board(seed);
            var redBoard = Board(seed);
            var ordinary = Spawn(ordinaryBoard, ZombieKind.Gargantuar);
            var red = Spawn(redBoard, (ZombieKind)32);
            Assert.That(ordinary.GargantuarState!.Weapon, Is.EqualTo(weapon));
            Assert.That(red.Kind, Is.EqualTo(ZombieKind.Gargantuar));
            Assert.That(red.ModeHead, Is.EqualTo(32));
            Assert.That(red.Health, Is.EqualTo(6000f));
            Assert.That(red.StartingHealth, Is.EqualTo(6000f));
            Assert.That(red.PresentationNativeVelocity, Is.EqualTo(ordinary.PresentationNativeVelocity));
            Assert.That(red.GargantuarState, Is.Not.Null);
            var state = red.GargantuarState!;
            Assert.That(state.Weapon, Is.EqualTo(weapon));
            var image = AdventureZombieEquipmentRules.Image(red, "Zombie_gargantuar", "Zombie_gargantuar", "zombie_gargantuar_telephonepole");
            Assert.That(image, Is.EqualTo(weapon == 2 ? "IMAGE_REANIM_ZOMBIE_GARGANTUAR_ZOMBIE"
                : weapon == 1 ? "IMAGE_REANIM_ZOMBIE_GARGANTUAR_DUCKXING" : null));
            redBoard.Update(.01f);
            Assert.That(red.GargantuarState, Is.SameAs(state));
            Assert.That(red.GargantuarState!.Weapon, Is.EqualTo(weapon));
        }

        /// <summary>Checks PC 4197D0 restricts high-ground rows rather than the ordinary rows used by a roof board.</summary>
        [Test]
        public void RoofRowsAdmitZomboniWhileSledsStillRequireIce()
        {
            var game = new ModeSession(15).Game;
            for (var row = 0; row < game.RowCount; row++)
            {
                Assert.That(Invoke(game, "CanAdventureZombieSpawnInRow", ZombieKind.Zomboni, row), Is.EqualTo(true));
                Assert.That(Invoke(game, "CanAdventureZombieSpawnInRow", ZombieKind.Bobsled, row), Is.EqualTo(false));
            }
        }

        /// <summary>Checks the original roof eligibility and weighted table can spawn a whole wave containing both Redeye and Zomboni.</summary>
        [Test]
        public void RoofEndlessSpawnsTheCompleteGeneratedMixedWave()
        {
            var definition = ModeCatalog.Get(15);
            for (uint seed = 0; seed < 128; seed++)
            {
                var context = new ModeWaveContext(seed);
                Assert.That(ModeWaveRules.BuildAllowed(definition, 4, AdventureTerrain.Roof, context)[32], Is.False);
                var allowed = ModeWaveRules.BuildAllowed(definition, 5, AdventureTerrain.Roof, context);
                if (!allowed[32] || !allowed[12])
                {
                    continue;
                }

                var waves = ModeWaveRules.Build(definition, 5, AdventureTerrain.Roof, context, new OriginalRandomAdapter(seed), allowed);
                var wave = waves.FirstOrDefault(value => value.Zombies.Any(kind => (int)kind == 32) && value.Zombies.Contains(ZombieKind.Zomboni));
                if (wave == null)
                {
                    continue;
                }

                var game = new ModeSession(15, randomSeed: (int)seed, waveContext: context).Game;
                SimulationFixture.Zombies(game).Clear();
                Invoke(game, "SpawnScheduledAdventureWave", wave);
                Assert.That(game.Zombies.Count, Is.EqualTo(wave.Zombies.Count));
                Assert.That(game.Zombies.Any(zombie => zombie.ModeHead == 32 && zombie.Health == 6000f), Is.True);
                Assert.That(game.Zombies.Any(zombie => zombie.Kind == ZombieKind.Zomboni), Is.True);
                return;
            }

            Assert.Fail("The controlled native seed domain did not produce the required mixed roof wave.");
        }

        /// <summary>Creates a quiet original daytime board with a controlled random stream.</summary>
        /// <param name="seed">The production random seed.</param>
        /// <returns>The initialized production simulation.</returns>
        private static GameSimulation Board(int seed)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 9 }, seed);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            SimulationFixture.Plants(game).Clear();
            SimulationFixture.Zombies(game).Clear();
            return game;
        }

        /// <summary>Spawns an actual original giant identifier through the board creation path.</summary>
        /// <param name="game">The production board receiving the actor.</param>
        /// <param name="kind">The original giant or Redeye identifier.</param>
        /// <returns>The live production-created giant.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind)
        {
            return (Zombie)Invoke(game, "SpawnAdventureBoardZombie", kind, 1, AdventureZombieNativeRules.GridX(650f), 0)!;
        }

        /// <summary>Invokes a production simulation boundary without opening a scene.</summary>
        /// <param name="game">The production simulation.</param>
        /// <param name="method">The production method name.</param>
        /// <param name="arguments">The method arguments.</param>
        /// <returns>The production result.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return SimulationInspection.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
