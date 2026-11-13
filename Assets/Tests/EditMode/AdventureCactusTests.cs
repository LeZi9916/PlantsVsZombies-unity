#nullable enable
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original cactus's animation-owned height transitions and distinct ground and air weapons.</summary>
    public sealed class AdventureCactusTests
    {
        /// <summary>Checks the entire fifteen-frame rise finishes before the same-tick high shot is scheduled.</summary>
        [Test]
        public void AirTargetStartsNativeRiseThenImmediateHighShot()
        {
            var game = CreateGame();
            var plant = Place(game);
            game.Zombies.Add(Target(true));
            Tick(game, plant, 1);
            var pose = plant.CactusPresentation!;
            Assert.That(pose.NativePhase, Is.EqualTo(31));
            Assert.That(pose.Animation, Is.EqualTo("anim_rise"));
            Assert.That(pose.Rate, Is.EqualTo(12f));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 63), Has.Count.EqualTo(1));
            Tick(game, plant, 124);
            Assert.That(pose.NativePhase, Is.EqualTo(31));
            Assert.That(game.Peas, Is.Empty);
            Tick(game, plant, 1);
            Assert.That(pose.NativePhase, Is.EqualTo(32));
            Assert.That(pose.Animation, Is.EqualTo("anim_shootinghigh"));
            Assert.That(pose.Rate, Is.EqualTo(35f));
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.EqualTo(23));
            Tick(game, plant, 21);
            Assert.That(game.Peas, Is.Empty);
            Tick(game, plant, 1);
            Assert.That(game.Peas, Has.Count.EqualTo(1));
            Assert.That(game.Peas[0].HitsAir, Is.True);
            Assert.That(game.Peas[0].X, Is.EqualTo(plant.Column + 108f / 80f));
            Assert.That(game.Peas[0].PresentationLaunchY, Is.EqualTo(80f + plant.Row * 85f - 42f));
            Assert.That(game.Peas[0].VelocityX, Is.EqualTo(3.33f * 100f / 80f));
        }

        /// <summary>Checks an airborne arrival cannot interrupt the low shot or shorten its authored eighteen-frame recovery.</summary>
        [Test]
        public void GroundShotFinishesBeforeCactusCanGrow()
        {
            var game = CreateGame();
            var plant = Place(game);
            plant.Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw(PlantsVsZombies.Timing.PlantKind.Cactus, 1, 0);
            game.Zombies.Add(Target(false));
            Tick(game, plant, 1);
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.EqualTo(35));
            game.Zombies.Add(Target(true));
            Tick(game, plant, 34);
            Assert.That(plant.CactusPresentation!.NativePhase, Is.EqualTo(30));
            Assert.That(game.Peas, Has.Count.EqualTo(1));
            Assert.That(game.Peas[0].HitsAir, Is.False);
            Assert.That(game.Peas[0].X, Is.EqualTo(plant.Column + 85f / 80f));
            Assert.That(game.Peas[0].PresentationLaunchY, Is.EqualTo(80f + plant.Row * 85f + 31f));
            Tick(game, plant, 17);
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.EqualTo(1));
            Assert.That(plant.CactusPresentation.NativePhase, Is.EqualTo(30));
            Tick(game, plant, 1);
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.Zero);
            Assert.That(plant.CactusPresentation.NativePhase, Is.EqualTo(31));
            Assert.That(game.Peas, Has.Count.EqualTo(1), "Waiting for the body animation must not repeat the queued shot.");
        }

        /// <summary>Checks target loss cannot interrupt a high shot and lowering uses the original thirteen-frame layer.</summary>
        [Test]
        public void HighShotFinishesBeforeNativeLowering()
        {
            var game = CreateGame();
            var plant = Place(game);
            var balloon = Target(true);
            game.Zombies.Add(balloon);
            Tick(game, plant, 126);
            balloon.PresentationNativePhase = 74;
            Tick(game, plant, 39);
            Assert.That(plant.CactusPresentation!.NativePhase, Is.EqualTo(32));
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.EqualTo(1));
            Tick(game, plant, 1);
            Assert.That(plant.CactusPresentation.NativePhase, Is.EqualTo(33));
            Assert.That(plant.CactusPresentation.Animation, Is.EqualTo("anim_lower"));
            Assert.That(plant.CactusPresentation.Rate, Is.EqualTo(12f));
            Tick(game, plant, 108);
            Assert.That(plant.CactusPresentation.NativePhase, Is.EqualTo(33));
            Tick(game, plant, 1);
            Assert.That(plant.CactusPresentation.NativePhase, Is.EqualTo(30));
            Assert.That(plant.CactusPresentation.Animation, Is.EqualTo("anim_idle"));
            Assert.That(plant.CactusPresentation.Rate, Is.EqualTo(12f));
        }

        /// <summary>Checks a disappearing target cannot reverse an unfinished rise and the original high state lasts one update.</summary>
        [Test]
        public void RiseFinishesBeforeMissingTargetCanStartLowering()
        {
            var game = CreateGame();
            var plant = Place(game);
            game.Zombies.Add(Target(true));
            Tick(game, plant, 1);
            game.Zombies.Clear();
            Tick(game, plant, 125);
            Assert.That(plant.CactusPresentation!.NativePhase, Is.EqualTo(32));
            Assert.That(plant.RequiredTiming.ShootingCountdown, Is.Zero);
            Tick(game, plant, 1);
            Assert.That(plant.CactusPresentation.NativePhase, Is.EqualTo(33));
            Assert.That(game.Peas, Is.Empty);
        }

        /// <summary>Checks primary targeting requires a damageable flying zombie rather than any zombie in the lane.</summary>
        [Test]
        public void PrimaryAndSecondaryTargetsRetainOriginalDamageMasks()
        {
            var plant = new Plant { Kind = PlantKind.Cactus, Row = 1, Column = 3 };
            var balloon = Target(true);
            Assert.That(AdventureCactusState.CanTarget(plant, balloon, true), Is.True);
            Assert.That(AdventureCactusState.CanTarget(plant, balloon, false), Is.False);
            balloon.PresentationNativePhase = 74;
            Assert.That(AdventureCactusState.CanTarget(plant, balloon, true), Is.False, "Balloon popping requires damage mask 0x10, which the cactus air weapon does not have.");
            balloon.PresentationNativePhase = 73;
            balloon.Row = 0;
            Assert.That(AdventureCactusState.CanTarget(plant, balloon, true), Is.False);
            balloon.Row = 1;
            balloon.X = AdventureZombieNativeRules.GridX(820f);
            Assert.That(AdventureCactusState.CanTarget(plant, balloon, true), Is.False);
            var ground = Target(false);
            Assert.That(AdventureCactusState.CanTarget(plant, ground, true), Is.False);
            Assert.That(AdventureCactusState.CanTarget(plant, ground, false), Is.True);
        }

        /// <summary>Checks an airborne spike crosses a ground zombie without changing its original damage target group.</summary>
        [Test]
        public void HighSpikeCannotDamageGroundZombies()
        {
            var game = CreateGame();
            var plant = Place(game);
            game.Zombies.Add(Target(true));
            Tick(game, plant, 148);
            var spike = game.Peas[0];
            var ground = Target(false);
            ground.X = spike.X;
            game.Zombies.Add(ground);
            typeof(GameSimulation).GetMethod("AdvanceAdventureProjectiles", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { .01f, true });
            Assert.That(ground.Health, Is.EqualTo(270f));
            Assert.That(game.Peas, Has.Count.EqualTo(1));
        }

        /// <summary>Creates a deterministic fog board without advancing its waves or unrelated objects.</summary>
        /// <returns>The active native-timing simulation.</returns>
        private static GameSimulation CreateGame()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 32 }, 91);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            return game;
        }

        /// <summary>Places the cactus through its production initialization path.</summary>
        /// <param name="game">The simulation receiving the cactus.</param>
        /// <returns>The planted cactus with its original native timing state.</returns>
        private static Plant Place(GameSimulation game)
        {
            return (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { PlantKind.Cactus, 1, 3 })!;
        }

        /// <summary>Creates a stationary original ground or flying target inside the cactus's native attack rectangle.</summary>
        /// <param name="air">Whether to create a flying balloon zombie.</param>
        /// <returns>The living native target.</returns>
        private static Zombie Target(bool air)
        {
            return new Zombie
            {
                Id = air ? 1 : 2,
                Kind = air ? ZombieKind.Balloon : ZombieKind.Normal,
                Row = 1,
                X = AdventureZombieNativeRules.GridX(600f),
                Health = 270f,
                StartingHealth = 270f,
                Airborne = air,
                PresentationNativePhase = air ? 73 : 0
            };
        }

        /// <summary>Advances only the production cactus branch by an exact number of native ticks.</summary>
        /// <param name="game">The active simulation.</param>
        /// <param name="plant">The cactus being updated.</param>
        /// <param name="ticks">The number of original hundred-Hz updates.</param>
        private static void Tick(GameSimulation game, Plant plant, int ticks)
        {
            var update = typeof(GameSimulation).GetMethod("AdvanceAdventureNativeTimedPlant", BindingFlags.Instance | BindingFlags.NonPublic)!;
            for (var tick = 0; tick < ticks; tick++)
            {
                update.Invoke(game, new object[] { plant });
            }
        }
    }
}
