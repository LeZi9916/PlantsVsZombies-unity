#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC mask-nine Fume-shroom acquisition and instant row damage against native phases and rectangles.</summary>
    public sealed class AdventureFumeShroomTests
    {
        /// <summary>Checks PC 54D810 admits landed waiting and grabbing bungees only.</summary>
        /// <param name="phase">The original bungee movement phase.</param>
        /// <param name="admitted">Whether the native phase allows acquisition and damage.</param>
        [TestCase(4, false)]
        [TestCase(5, false)]
        [TestCase(6, true)]
        [TestCase(7, true)]
        [TestCase(8, false)]
        [TestCase(10, false)]
        public void BungeeAdmissionUsesOriginalBottomPhases(int phase, bool admitted)
        {
            var game = Board();
            var plant = Mushroom(game);
            var zombie = Target(game, ZombieKind.Bungee, 0, 200, phase);
            Assert.That(AdventureFumeShroomRules.CanTarget(plant, zombie), Is.EqualTo(admitted));
            Assert.That(Invoke(game, "FindAdventureFumeTarget", plant), Is.EqualTo(admitted ? zombie : null));
            var health = zombie.Health;
            Invoke(game, "FireAdventureFume", plant);
            Assert.That(zombie.Health, Is.EqualTo(health - (admitted ? 20f : 0f)));
            Assert.That(game.Peas, Is.Empty);
            zombie.Row = 1;
            Assert.That(Invoke(game, "FindAdventureFumeTarget", plant), Is.Null);
            Assert.That(AdventureFumeShroomRules.CanDamage(plant, zombie), Is.False);
        }

        /// <summary>Checks an already started attack rechecks the bungee phase on its original firing tick.</summary>
        /// <param name="phase">The phase entered immediately before the scheduled strike.</param>
        /// <param name="damaged">Whether the native phase permits the pending strike.</param>
        [TestCase(6, true)]
        [TestCase(7, true)]
        [TestCase(8, false)]
        public void ScheduledAttackRechecksTheBungeePhase(int phase, bool damaged)
        {
            var game = Board();
            var plant = Mushroom(game);
            var zombie = Target(game, ZombieKind.Bungee, 0, 200, 6);
            plant.Timing = global::PlantsVsZombies.Timing.PlantTiming.FromRaw(global::PlantsVsZombies.Timing.PlantKind.FumeShroom, 0, 0);
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(plant.Timing!.Value.ShootingCountdown, Is.EqualTo(50));
            for (var tick = 0; tick < 48; tick++)
            {
                Invoke(game, "AdvanceAdventurePlants", .01f);
            }

            Assert.That(zombie.Health, Is.EqualTo(450f));
            zombie.BungeeState!.Enter(phase);
            zombie.PresentationNativePhase = phase;
            Invoke(game, "AdvanceAdventurePlants", .01f);
            Assert.That(zombie.Health, Is.EqualTo(damaged ? 430f : 450f));
            Assert.That(game.Peas, Is.Empty);
        }

        /// <summary>Checks mask nine rejects the pole-vault phase without rejecting its neighboring grounded phases.</summary>
        /// <param name="phase">The original pole-vaulter phase.</param>
        /// <param name="admitted">Whether the phase can be acquired and damaged.</param>
        [TestCase(11, true)]
        [TestCase(12, false)]
        [TestCase(13, true)]
        public void PoleVaultPhaseTwelveCannotBeTargetedOrDamaged(int phase, bool admitted)
        {
            var game = Board();
            var plant = Mushroom(game);
            var zombie = Target(game, ZombieKind.PoleVaulter, 0, 300, phase);
            Assert.That(AdventureFumeShroomRules.CanTarget(plant, zombie), Is.EqualTo(admitted));
            Assert.That(Invoke(game, "FindAdventureFumeTarget", plant), Is.EqualTo(admitted ? zombie : null));
            var health = zombie.Health;
            Invoke(game, "FireAdventureFume", plant);
            Assert.That(zombie.Health, Is.EqualTo(health - (admitted ? 20f : 0f)));
        }

        /// <summary>Checks both PC 4765B0 interval ends at one-pixel overlap, exact contact and one-pixel separation.</summary>
        /// <param name="kind">The species whose native body rectangle is tested.</param>
        /// <param name="offset">The native body offset from the actor origin.</param>
        /// <param name="width">The original body width.</param>
        [TestCase(ZombieKind.Normal, 36, 42)]
        [TestCase(ZombieKind.Bungee, -20, 110)]
        public void NativeEdgesRetainTouchAcquisitionAndStrictDamage(ZombieKind kind, int offset, int width)
        {
            for (var side = 0; side < 2; side++)
            {
                for (var gap = -1; gap <= 1; gap++)
                {
                    var game = Board();
                    var plant = Mushroom(game);
                    // PC plant x=120: attack [180,520], independent of visible cloud pixels.
                    var left = side == 0 ? 180 - width - gap : 520 + gap;
                    var zombie = Target(game, kind, 0, left - offset, kind == ZombieKind.Bungee ? 6 : 0);
                    Assert.That(Invoke(game, "FindAdventureFumeTarget", plant), Is.EqualTo(gap <= 0 ? zombie : null));
                    var health = zombie.Health;
                    Invoke(game, "FireAdventureFume", plant);
                    Assert.That(zombie.Health, Is.EqualTo(health - (gap < 0 ? 20f : 0f)), $"{kind}, side={side}, gap={gap}");
                }
            }
        }

        /// <summary>Checks only the original exposed-head phases admit the separate boss target on every row.</summary>
        /// <param name="phase">The original boss action phase.</param>
        /// <param name="admitted">Whether PC 54D810 admits the exposed head.</param>
        [TestCase(78, false)]
        [TestCase(86, false)]
        [TestCase(87, true)]
        [TestCase(88, true)]
        [TestCase(89, true)]
        [TestCase(90, false)]
        public void BossAcquisitionAndDamageUseExposedPhasesAcrossRows(int phase, bool admitted)
        {
            for (var row = 0; row < 5; row++)
            {
                var game = Board(50);
                var plant = Mushroom(game, row, 5);
                var boss = game.Adventure.Boss!;
                boss.Enter(phase, "anim_head_idle", 12f, 0);
                game.Adventure.BossHeadVulnerable = boss.HeadVulnerable;
                Assert.That(game.Zombies.Contains(boss.Target), Is.False);
                Assert.That(Invoke(game, "FindAdventureFumeTarget", plant), Is.EqualTo(admitted ? boss.Target : null));
                var health = game.Adventure.BossHealth;
                Invoke(game, "FireAdventureFume", plant);
                Assert.That(game.Adventure.BossHealth, Is.EqualTo(health - (admitted ? 20f : 0f)));
            }
        }

        /// <summary>Checks the exposed boss still obeys the original horizontal range.</summary>
        /// <param name="column">The mushroom column whose native interval is tested.</param>
        /// <param name="admitted">Whether that interval overlaps the boss body at x=700.</param>
        [TestCase(3, false)]
        [TestCase(4, true)]
        public void ExposedBossStillRequiresNativeRangeOverlap(int column, bool admitted)
        {
            var game = Board(50);
            var plant = Mushroom(game, 2, column);
            var boss = game.Adventure.Boss!;
            boss.Enter(88, "anim_head_idle", 12f, 0);
            game.Adventure.BossHeadVulnerable = true;
            Assert.That(Invoke(game, "FindAdventureFumeTarget", plant), Is.EqualTo(admitted ? boss.Target : null));
            var health = game.Adventure.BossHealth;
            Invoke(game, "FireAdventureFume", plant);
            Assert.That(game.Adventure.BossHealth, Is.EqualTo(health - (admitted ? 20f : 0f)));
        }

        /// <summary>Checks native leftmost-body selection and the first-allocated boss's equal-position priority.</summary>
        /// <param name="bodyLeft">The ordinary zombie body's left edge relative to the boss at 700.</param>
        [TestCase(650)]
        [TestCase(700)]
        [TestCase(730)]
        public void BossAndOrdinaryTargetsKeepNativeOrderAndBothReceiveOneStrike(int bodyLeft)
        {
            var game = Board(50);
            var plant = Mushroom(game, 4, 5);
            var boss = game.Adventure.Boss!;
            boss.Enter(88, "anim_head_idle", 12f, 0);
            game.Adventure.BossHeadVulnerable = true;
            var zombie = Target(game, ZombieKind.Normal, 4, bodyLeft - 36, 0);
            Assert.That(Invoke(game, "FindAdventureFumeTarget", plant), Is.SameAs(bodyLeft < 700 ? zombie : boss.Target));
            var health = zombie.Health;
            var bossHealth = game.Adventure.BossHealth;
            Invoke(game, "FireAdventureFume", plant);
            Assert.That(zombie.Health, Is.EqualTo(health - 20f));
            Assert.That(game.Adventure.BossHealth, Is.EqualTo(bossHealth - 20f));
            Assert.That(game.Peas, Is.Empty);
        }

        /// <summary>Creates an awake board without automatic waves or ordinary occupants.</summary>
        /// <param name="level">The native night or boss background.</param>
        /// <returns>The isolated production simulation.</returns>
        private static GameSimulation Board(int level = 19)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            if (game.Adventure.Phase == AdventurePhase.ChoosingSeeds)
            {
                Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            }
            game.ResetAdventureWaveSchedule(100000, false, false);
            SimulationFixture.Plants(game).Clear();
            SimulationFixture.Zombies(game).Clear();
            return game;
        }

        /// <summary>Places an initialized awake mushroom for the original logical attack calls.</summary>
        /// <param name="game">The owning simulation.</param>
        /// <param name="row">The mushroom row.</param>
        /// <param name="column">The mushroom column.</param>
        /// <returns>The initialized production plant.</returns>
        private static Plant Mushroom(GameSimulation game, int row = 0, int column = 1)
        {
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.FumeShroom, row, column)!;
            plant.Asleep = false;
            return plant;
        }

        /// <summary>Initializes a live zombie at an exact native actor origin and movement phase.</summary>
        /// <param name="game">The owning simulation.</param>
        /// <param name="kind">The native species.</param>
        /// <param name="row">The logical row.</param>
        /// <param name="nativeX">The integer actor origin before the species body offset.</param>
        /// <param name="phase">The native phase for acquisition and damage.</param>
        /// <returns>The initialized production target.</returns>
        private static Zombie Target(GameSimulation game, ZombieKind kind, int row, int nativeX, int phase)
        {
            var zombie = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", kind, row, 9.5f, 0)!;
            zombie.Row = row;
            zombie.X = AdventureZombieNativeRules.GridX(nativeX + .25f);
            zombie.PresentationNativePhase = phase;
            if (kind == ZombieKind.Bungee)
            {
                zombie.BungeeState!.Enter(phase, 300);
                zombie.PresentationAltitude = 0f;
            }
            return zombie;
        }

        /// <summary>Calls an existing simulation boundary without advancing unrelated systems.</summary>
        /// <param name="game">The owning simulation.</param>
        /// <param name="method">The production method name.</param>
        /// <param name="arguments">The method arguments.</param>
        /// <returns>The production result, or null for void boundaries.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return SimulationInspection.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
