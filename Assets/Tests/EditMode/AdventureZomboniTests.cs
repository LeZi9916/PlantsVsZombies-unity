#nullable enable
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the PC ice vehicle's sound, damage artwork, real body collision and original death effects.</summary>
    public sealed class AdventureZomboniTests
    {
        /// <summary>Checks exact health boundaries preserve the original healthier damage stage.</summary>
        /// <param name="health">The current original 1350-point body health.</param>
        /// <param name="stage">The expected original damage image suffix.</param>
        [TestCase(1350f, 0)]
        [TestCase(900f, 0)]
        [TestCase(899f, 1)]
        [TestCase(450f, 1)]
        [TestCase(449f, 2)]
        public void VehicleDamageImagesUseOriginalStrictThirds(float health, int stage)
        {
            var zombie = new Zombie { Kind = ZombieKind.Zomboni, StartingHealth = 1350f, Health = health };
            foreach (var part in new[] { 1, 2 })
            {
                var image = AdventureZombieEquipmentRules.Image(zombie, "Zombie_zamboni", "Zombie_zamboni", "Zombie_zamboni_" + part);
                Assert.That(image, Is.EqualTo(stage == 0 ? null : "IMAGE_REANIM_ZOMBIE_ZAMBONI_" + part + "_DAMAGE" + stage));
            }

            Assert.That(AdventureZomboniRules.DamageImage(zombie, "Zombie_zambonidriver_body"), Is.Null);
        }

        /// <summary>Checks native spawn, unsuppressed damage, heavy damage smoke and normal explosion cues.</summary>
        [Test]
        public void SpawnHitAndDeathUseOriginalVehicleSoundsAndParticles()
        {
            var game = Board();
            var zombie = Spawn(game);
            Assert.That(CountSound(game, 16), Is.EqualTo(1));
            Hit(game, zombie, 451f);
            Assert.That(zombie.Health, Is.EqualTo(899f));
            Assert.That(CountSound(game, 46), Is.EqualTo(1));
            Assert.That(zombie.ZomboniState!.SmokeStartAge, Is.LessThan(0f));
            Hit(game, zombie, 450f);
            Assert.That(zombie.Health, Is.EqualTo(449f));
            Assert.That(zombie.ZomboniState.SmokeStartAge, Is.EqualTo(zombie.Age));
            Assert.That(CountSound(game, 46), Is.EqualTo(2));
            Hit(game, zombie, 1f, 8U);
            Assert.That(CountSound(game, 46), Is.EqualTo(2), "PC damage bit 3 suppresses the vehicle hit Foley.");
            Hit(game, zombie, 448f);
            Assert.That(game.Zombies, Has.No.Member(zombie));
            Assert.That(game.Presentation.Corpses, Is.Empty, "A vehicle explodes instead of using an ordinary zombie death clip.");
            Assert.That(CountSound(game, 23), Is.EqualTo(1));
            var explosion = game.Presentation.ZomboniParticles.Single();
            Assert.That(explosion.Particle, Is.EqualTo("ZamboniExplosion"));
            Assert.That(explosion.X, Is.EqualTo(368f));
            Assert.That(explosion.Y, Is.EqualTo(210f));
            Assert.That(AdventureZombieFoleyData.Definitions[16].Samples, Is.EqualTo(new[] { "zamboni" }));
            Assert.That(AdventureZombieFoleyData.Definitions[16].PitchRange, Is.EqualTo(5f));
            Assert.That(AdventureZombieFoleyData.Definitions[23].Samples, Is.EqualTo(new[] { "explosion" }));
        }

        /// <summary>Checks the actual projectile loop uses the vehicle's rear body instead of its old narrow center gate.</summary>
        [Test]
        public void ProductionProjectileHitsOriginalWideVehicleBody()
        {
            var game = Board();
            var zombie = Spawn(game);
            var projectile = new Pea { Row = 1, FlightRow = 1f, X = 5.35f, VelocityX = -4.5f, Damage = 20f };
            game.Peas.Add(projectile);
            Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
            Assert.That(zombie.Health, Is.EqualTo(1330f), "The original 153-pixel body includes the visible rear, far beyond grid-center +/- 0.25.");
            Assert.That(game.Peas, Has.No.Member(projectile));
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie, 150);
            Assert.That(body.X, Is.EqualTo(288));
            Assert.That(body.Y, Is.EqualTo(137));
            Assert.That(body.Width, Is.EqualTo(153));
            Assert.That(body.Height, Is.EqualTo(140));
            var attack = game.AdventureZombieAttackRectangle(zombie);
            Assert.That(attack.Width, Is.EqualTo(133));
            Assert.That(attack.Height, Is.EqualTo(140));
        }

        /// <summary>Checks logical body collision does not shrink with the original small-vehicle artwork.</summary>
        [Test]
        public void SmallVehicleRetainsNativeLogicalBodyWidth()
        {
            var zombie = new Zombie { Kind = ZombieKind.Zomboni, X = 4f, IsSmall = true };
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
            Assert.That(body.Width, Is.EqualTo(153));
            Assert.That(body.Height, Is.EqualTo(140));
        }

        /// <summary>Checks original spike death retains the wheelie for all 280 native updates and only then explodes.</summary>
        [Test]
        public void PuncturedTiresPlayOriginalWheelieBeforeDelayedExplosion()
        {
            var game = Board();
            var zombie = Spawn(game);
            Hit(game, zombie, 20f, 32U);
            var state = zombie.ZomboniState!;
            Assert.That(state.FlatTires, Is.True);
            Assert.That(state.DeathTicks, Is.EqualTo(280));
            Assert.That(CountSound(game, 22), Is.EqualTo(1));
            Assert.That(game.Presentation.ZomboniParticles.Single().Particle, Is.EqualTo("ZamboniTire"));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo(state.Animation));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(state.Rate));
            Assert.That(AdventureZombieMotionRules.PlaysOnce(zombie), Is.True);
            var projectile = new Pea { Row = 1, FlightRow = 1f, X = 5.35f, VelocityX = -4.5f, Damage = 20f };
            game.Peas.Add(projectile);
            var spore = new Pea { Row = 1, FlightRow = 1f, X = 5.2f, VelocityX = -4.5f, Damage = 20f, SourceKind = PlantKind.PuffShroom };
            game.Peas.Add(spore);
            var health = zombie.Health;
            Invoke(game, "AdvanceAdventureProjectiles", .01f, true);
            Assert.That(game.Peas, Has.Member(projectile), "PC EffectedByDamage lets a projectile pass a dying punctured vehicle.");
            Assert.That(game.Peas, Has.Member(spore), "The same original dying-state exclusion applies to short-range spores.");
            Assert.That(zombie.Health, Is.EqualTo(health));
            game.Peas.Clear();
            var initial = zombie.X;
            for (var tick = 0; tick < 279; tick++)
            {
                Invoke(game, "AdvanceAdventureZombies", .01f);
            }

            Assert.That(game.Zombies, Has.Member(zombie));
            Assert.That(state.DeathTicks, Is.EqualTo(1));
            Assert.That(zombie.X, Is.EqualTo(initial));
            Assert.That(CountSound(game, 23), Is.Zero);
            Invoke(game, "AdvanceAdventureZombies", .01f);
            Assert.That(game.Zombies, Has.No.Member(zombie));
            Assert.That(CountSound(game, 23), Is.EqualTo(1));
            Assert.That(game.Presentation.ZomboniParticles.Last().Particle, Is.EqualTo(state.Wheelie2 ? "ZamboniExplosion2" : "ZamboniExplosion"));
            Assert.That(game.Presentation.Corpses, Is.Empty);
        }

        /// <summary>Checks only living vehicles strictly below two hundred health enter the original critical state.</summary>
        /// <param name="kind">The original vehicle or ordinary zombie species.</param>
        /// <param name="health">The native body health at the threshold boundary.</param>
        /// <param name="phase">The original native phase, including punctured-tire death.</param>
        /// <param name="critical">Whether the original body shaking and decay gate should open.</param>
        [TestCase(ZombieKind.Zomboni, 200f, -1, false)]
        [TestCase(ZombieKind.Zomboni, 199f, -1, true)]
        [TestCase(ZombieKind.Catapult, 199f, -1, true)]
        [TestCase(ZombieKind.Zomboni, 199f, 1, false)]
        [TestCase(ZombieKind.Normal, 199f, -1, false)]
        public void CriticalVehicleDamageUsesOriginalStrictThreshold(ZombieKind kind, float health, int phase, bool critical)
        {
            var zombie = new Zombie { Kind = kind, Health = health, PresentationNativePhase = phase };
            Assert.That(AdventureZomboniRules.HasCriticalDamage(zombie), Is.EqualTo(critical));
        }

        /// <summary>Checks the original critical-state leak uses silent three-point body hits and reaches the native vehicle death.</summary>
        /// <param name="kind">The original vehicle species that shares the critical damage behavior.</param>
        [TestCase(ZombieKind.Zomboni)]
        [TestCase(ZombieKind.Catapult)]
        public void CriticalVehiclesLoseHealthWithoutIncomingAttacks(ZombieKind kind)
        {
            var game = Board();
            var zombie = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { kind, 1, 4f, 5 })!;
            zombie.Health = 200f;
            for (var tick = 0; tick < 100; tick++)
            {
                Invoke(game, "AdvanceAdventureZombies", .01f);
            }

            Assert.That(zombie.Health, Is.EqualTo(200f), "The exact critical threshold must not leak health.");
            zombie.Health = 199f;
            for (var tick = 0; tick < 100; tick++)
            {
                Invoke(game, "AdvanceAdventureZombies", .01f);
            }

            Assert.That(zombie.Health, Is.LessThan(199f));
            Assert.That((199f - zombie.Health) % 3f, Is.Zero);
            Assert.That(CountSound(game, 46), Is.Zero, "Original damage flags 9 suppress the leak's hit sound.");
            for (var tick = 0; tick < 1000 && game.Zombies.Contains(zombie); tick++)
            {
                Invoke(game, "AdvanceAdventureZombies", .01f);
            }

            Assert.That(game.Zombies, Has.No.Member(zombie));
            if (kind == ZombieKind.Zomboni)
            {
                Assert.That(CountSound(game, 23), Is.EqualTo(1));
                Assert.That(game.Presentation.ZomboniParticles.Last().Particle, Is.EqualTo("ZamboniExplosion"));
            }
        }

        /// <summary>Checks the original small vehicle loses one body point instead of the full-sized vehicle's three.</summary>
        [Test]
        public void SmallCriticalVehicleUsesOriginalMaximumHealthDamageBranch()
        {
            var game = Board();
            var zombie = Spawn(game);
            zombie.IsSmall = true;
            zombie.StartingHealth = 337f;
            zombie.Health = 199f;
            var observedLeak = false;
            for (var tick = 0; tick < 100; tick++)
            {
                var previous = zombie.Health;
                Invoke(game, "AdvanceAdventureZombies", .01f);
                var damage = previous - zombie.Health;
                Assert.That(damage, Is.EqualTo(0f).Or.EqualTo(1f));
                observedLeak |= damage > 0f;
            }

            Assert.That(observedLeak, Is.True);
        }

        /// <summary>Creates a quiet daytime production board.</summary>
        /// <returns>The production simulation with automatic waves held.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 18 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Creates an original full-size vehicle at native coordinates 288,150.</summary>
        /// <param name="game">The board receiving the vehicle.</param>
        /// <returns>The production-created ice vehicle.</returns>
        private static Zombie Spawn(GameSimulation game)
        {
            return (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { ZombieKind.Zomboni, 1, 4f, 5 })!;
        }

        /// <summary>Applies a production body hit with the requested original damage flags.</summary>
        /// <param name="game">The source board.</param>
        /// <param name="zombie">The vehicle being hit.</param>
        /// <param name="damage">The body damage.</param>
        /// <param name="flags">The original native damage mask.</param>
        private static void Hit(GameSimulation game, Zombie zombie, float damage, uint flags = 0)
        {
            Invoke(game, "HitAdventureZombie", zombie, damage, true, flags);
        }

        /// <summary>Invokes a production simulation boundary without initializing a scene.</summary>
        /// <param name="game">The source board.</param>
        /// <param name="name">The private production boundary.</param>
        /// <param name="arguments">The production method arguments.</param>
        private static void Invoke(GameSimulation game, string name, params object[] arguments)
        {
            typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }

        /// <summary>Counts explicit original Foley requests emitted by this board.</summary>
        /// <param name="game">The source board.</param>
        /// <param name="foley">The original PC Foley number.</param>
        /// <returns>The retained matching request count.</returns>
        private static int CountSound(GameSimulation game, int foley)
        {
            return game.Presentation.ZombieSounds.Count(sound => sound.Foley == foley);
        }
    }
}
