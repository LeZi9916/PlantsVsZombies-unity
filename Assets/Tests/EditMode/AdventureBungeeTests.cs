#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Regresses PC 53E245 and 540070..540DED bungee selection, movement, capture and interruption.</summary>
    public sealed class AdventureBungeeTests
    {
        /// <summary>Checks native health, altitude, authored clip and the unscaled original collision rectangle.</summary>
        [Test]
        public void SpawnUsesOriginalDivingStateAndGridPosition()
        {
            var game = Board();
            var zombie = Spawn(game);
            Assert.That(zombie.Health, Is.EqualTo(450f));
            Assert.That(zombie.PresentationAltitude, Is.InRange(3000f, 3150f));
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(4));
            Assert.That(zombie.BungeeState!.Animation, Is.EqualTo("anim_drop"));
            Assert.That(zombie.BungeeState.AnimationRate, Is.EqualTo(24f));
            Assert.That(AdventureZombieNativeRules.NativePositionX(zombie.X), Is.EqualTo(40f + zombie.TargetColumn * 80f));
            var box = AdventureZombieTargetingRules.BodyRectangle(zombie, 100);
            Assert.That(box.X, Is.EqualTo(20 + zombie.TargetColumn * 80));
            Assert.That(box.Y, Is.EqualTo(122));
            Assert.That(box.Width, Is.EqualTo(110));
            Assert.That(box.Height, Is.EqualTo(94));
        }

        /// <summary>Checks original weighted reservations and protection of the final unreserved sun producer.</summary>
        [Test]
        public void TargetSelectionReservesCellsAndLeavesTheLastSunProducer()
        {
            var game = Board();
            var sunflower = new Plant { Id = 1, Kind = PlantKind.Sunflower, Row = 1, Column = 3, Layer = PlantLayer.Main, Health = 300f };
            game.Plants.Add(sunflower);
            for (var index = 0; index < 12; index++)
            {
                var zombie = Spawn(game);
                Assert.That(zombie.Row != sunflower.Row || zombie.TargetColumn != sunflower.Column, Is.True);
                foreach (var other in game.Zombies)
                {
                    if (other != zombie)
                    {
                        Assert.That(other.Row != zombie.Row || other.TargetColumn != zombie.TargetColumn, Is.True);
                    }
                }
            }
        }

        /// <summary>Checks the original reticle-landing step and single scream threshold rather than a spawn-time scream.</summary>
        [Test]
        public void DescendingSoundsUseOriginalAltitudeCrossings()
        {
            var game = Board();
            var zombie = Spawn(game);
            zombie.PresentationAltitude = 2600f;
            Tick(game, 1);
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(2592f));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 74).Count, Is.EqualTo(1));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 44).Count, Is.Zero);
            zombie.PresentationAltitude = 1500f;
            Tick(game, 2);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(5));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 44).Count, Is.EqualTo(1));
        }

        /// <summary>Checks the original three-second bottom pause, fifteen-frame grab and eight-pixel ascent.</summary>
        [Test]
        public void CaptureRetainsThePlantUntilOriginalGrabAndLiftFinish()
        {
            var game = Board();
            var plant = AddTarget(game);
            var zombie = SpawnAtTarget(game);
            zombie.PresentationAltitude = 8f;
            Tick(game, 1);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(6));
            Assert.That(zombie.BungeeState!.PhaseTicks, Is.EqualTo(300));
            Assert.That(plant.BungeeState, Is.Zero);
            Tick(game, 299);
            Assert.That(zombie.BungeeState.PhaseTicks, Is.EqualTo(1));
            Tick(game, 1);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(7));
            Assert.That(zombie.BungeeState.TargetPlant, Is.SameAs(plant));
            Assert.That(plant.BungeeState, Is.EqualTo(1));
            Tick(game, 62);
            Assert.That(plant.BungeeState, Is.EqualTo(1));
            Tick(game, 1);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(8));
            Assert.That(plant.BungeeState, Is.EqualTo(2));
            Assert.That(game.AdventurePlantAt(1, 3, PlantLayer.Main), Is.Null);
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 68).Count, Is.EqualTo(1));
            Tick(game, 1);
            Assert.That(zombie.PresentationAltitude, Is.EqualTo(8f));
            Tick(game, 74);
            Assert.That(game.Zombies.Contains(zombie), Is.False);
            Assert.That(game.Plants.Contains(plant), Is.False);
        }

        /// <summary>Checks only the two native bottom phases permit projectile targeting or freezing.</summary>
        /// <param name="phase">The original bungee phase.</param>
        /// <param name="targetable">Whether ordinary targeting admits the phase.</param>
        [TestCase(4, false)]
        [TestCase(5, false)]
        [TestCase(6, true)]
        [TestCase(7, true)]
        [TestCase(8, false)]
        [TestCase(10, false)]
        public void TargetingAndFreezeGatesUseNativePhases(int phase, bool targetable)
        {
            var zombie = new Zombie { Kind = ZombieKind.Bungee, Health = 450f, PresentationNativePhase = phase, X = 4f };
            Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(zombie), Is.EqualTo(targetable));
            Assert.That(AdventureIceRules.CanFreeze(20, phase, true), Is.EqualTo(phase == 6));
        }

        /// <summary>Checks immobilization holds the original bottom countdown and body clock.</summary>
        [Test]
        public void FreezeAndButterSuspendTheGrabClock()
        {
            var game = Board();
            AddTarget(game);
            var zombie = SpawnAtTarget(game);
            zombie.BungeeState!.Enter(6, 300, 5);
            zombie.PresentationNativePhase = 6;
            zombie.PresentationAltitude = 0f;
            zombie.FrozenRemaining = 1f;
            var age = zombie.BungeeState.AnimationSeconds;
            Tick(game, 20);
            Assert.That(zombie.BungeeState.PhaseTicks, Is.EqualTo(300));
            Assert.That(zombie.BungeeState.AnimationSeconds, Is.EqualTo(age));
            zombie.FrozenRemaining = 0f;
            zombie.PresentationButter = 1f;
            Tick(game, 20);
            Assert.That(zombie.BungeeState.PhaseTicks, Is.EqualTo(300));
            Assert.That(zombie.BungeeState.AnimationSeconds, Is.EqualTo(age));
        }

        /// <summary>Checks killing a grabbing bungee restores the plant while killing a rising bungee removes lifted cargo.</summary>
        /// <param name="lifted">Whether the original lift has already detached the plant.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void DeathResolvesCapturedPlantOwnership(bool lifted)
        {
            var game = Board();
            var plant = AddTarget(game);
            var zombie = SpawnAtTarget(game);
            zombie.BungeeState!.TargetPlant = plant;
            zombie.BungeeState.Enter(lifted ? 8 : 7);
            plant.BungeeState = lifted ? 2 : 1;
            Invoke(game, "KillAdventureZombie", zombie, 0u);
            Assert.That(game.Plants.Contains(plant), Is.EqualTo(!lifted));
            if (!lifted)
            {
                Assert.That(plant.BungeeState, Is.Zero);
            }
        }

        /// <summary>Checks Umbrella Leaf acts only at the original altitude threshold and clears both carrier and passenger on departure.</summary>
        [Test]
        public void UmbrellaBouncesAtFortyPixelsAndPreservesItsOriginalDropPose()
        {
            var game = Board();
            game.Plants.Add(new Plant { Id = 1, Kind = PlantKind.UmbrellaLeaf, Row = 1, Column = 3, Health = 300f, Layer = PlantLayer.Main });
            var zombie = SpawnAtTarget(game);
            zombie.PresentationAltitude = 56f;
            Tick(game, 1);
            Assert.That(zombie.BungeeState!.HitUmbrella, Is.False);
            Tick(game, 1);
            Assert.That(zombie.BungeeState.HitUmbrella, Is.True);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(8));
            Assert.That(zombie.BungeeState.Animation, Is.EqualTo("anim_drop"));
            Assert.That(game.Presentation.ZombieSounds.Exists(sound => sound.Foley == -6), Is.True);
            Assert.That(game.Presentation.ZombieSounds.Exists(sound => sound.Foley == 73), Is.True);
            Tick(game, 70);
            Assert.That(game.Zombies.Contains(zombie), Is.False);
        }

        /// <summary>Checks suspended passengers remain untargetable until the original landing releases them alive.</summary>
        [Test]
        public void CarrierReleasesItsPassengerOnlyAtTheOriginalLanding()
        {
            var game = Board();
            Invoke(game, "DropAdventureZombie", ZombieKind.Normal, 1, 3, 0);
            var carrier = game.Zombies[0];
            var passenger = game.Zombies[1];
            Assert.That(passenger.BungeeCarrierId, Is.EqualTo(carrier.Id));
            Assert.That(passenger.PresentationNativeHeight, Is.EqualTo(9));
            Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(passenger), Is.False);
            Assert.That(game.AdventurePresentationZombieRate(passenger), Is.Zero);
            carrier.PresentationAltitude = 8f;
            Tick(game, 1);
            Assert.That(carrier.PresentationNativePhase, Is.EqualTo(8));
            Assert.That(carrier.LinkedZombieId, Is.Zero);
            Assert.That(passenger.BungeeCarrierId, Is.Zero);
            Assert.That(passenger.PresentationNativeHeight, Is.Zero);
            Assert.That(passenger.DropRemaining, Is.Zero);
            Tick(game, 75);
            Assert.That(game.Zombies.Contains(carrier), Is.False);
            Assert.That(game.Zombies.Contains(passenger), Is.True);
        }

        /// <summary>Checks killing a carrier removes its still-suspended passenger without releasing it onto the lawn.</summary>
        [Test]
        public void CarrierDeathDoesNotDropALivePassenger()
        {
            var game = Board();
            Invoke(game, "DropAdventureZombie", ZombieKind.Normal, 1, 3, 0);
            var carrier = game.Zombies[0];
            var passenger = game.Zombies[1];
            Invoke(game, "KillAdventureZombie", carrier, 0u);
            Assert.That(game.Zombies.Contains(carrier), Is.False);
            Assert.That(game.Zombies.Contains(passenger), Is.False);
        }

        /// <summary>Checks occupied grabbed plants cannot be upgraded or repaired before ownership is restored.</summary>
        [Test]
        public void GrabbedPlantsCannotBeUpgradedOrRepaired()
        {
            var game = Board();
            var repeater = new Plant { Id = 1, Kind = PlantKind.Repeater, Row = 1, Column = 3,
                Layer = PlantLayer.Main, Health = 300f, BungeeState = 1 };
            game.Plants.Add(repeater);
            Assert.That(Invoke(game, "CheckAdventureUpgrade", PlantKind.GatlingPea, 1, 3), Is.EqualTo(AdventurePlantingFailure.NeedsUpgrade));
            repeater.BungeeState = 0;
            Assert.That(Invoke(game, "CheckAdventureUpgrade", PlantKind.GatlingPea, 1, 3), Is.EqualTo(AdventurePlantingFailure.None));
            var wallnut = new Plant { Id = 2, Kind = PlantKind.Wallnut, Row = 1, Column = 4,
                Layer = PlantLayer.Main, Health = 1000f, BungeeState = 1 };
            game.Plants.Add(wallnut);
            game.Rules.AdventureFirstAid = true;
            Assert.That(Invoke(game, "IsAdventureFirstAid", PlantKind.Wallnut, 1, 4), Is.False);
            wallnut.BungeeState = 0;
            Assert.That(Invoke(game, "IsAdventureFirstAid", PlantKind.Wallnut, 1, 4), Is.True);
        }

        /// <summary>Checks an airborne stolen umbrella no longer protects the ground cell.</summary>
        [Test]
        public void LiftedUmbrellaStopsProtectingTheLawn()
        {
            var game = Board();
            var plant = new Plant { Id = 1, Kind = PlantKind.UmbrellaLeaf, Row = 1, Column = 3,
                Layer = PlantLayer.Main, Health = 300f, BungeeState = 1 };
            game.Plants.Add(plant);
            Assert.That(Invoke(game, "HasUmbrellaNear", 1, 3), Is.True);
            plant.BungeeState = 2;
            Assert.That(Invoke(game, "HasUmbrellaNear", 1, 3), Is.False);
        }

        /// <summary>Creates a real roof board without incidental wave spawning.</summary>
        /// <returns>The production bungee simulation fixture.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 48 }, 12345);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Adds a normal plant at the original controlled target cell.</summary>
        /// <param name="game">The roof simulation receiving the plant.</param>
        /// <returns>The real plant used by capture and interruption tests.</returns>
        private static Plant AddTarget(GameSimulation game)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Peashooter, 1, 3)!;
        }

        /// <summary>Creates a real bungee using normal production initialization.</summary>
        /// <param name="game">The production simulation.</param>
        /// <returns>The initialized bungee.</returns>
        private static Zombie Spawn(GameSimulation game)
        {
            return (Zombie)Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Bungee, 1, 9.5f, 0)!;
        }

        /// <summary>Places a production-initialized bungee at the controlled original target cell.</summary>
        /// <param name="game">The production simulation.</param>
        /// <returns>The initialized bungee with a fixed target.</returns>
        private static Zombie SpawnAtTarget(GameSimulation game)
        {
            var zombie = Spawn(game);
            zombie.Row = 1;
            zombie.TargetColumn = 3;
            zombie.X = AdventureZombieNativeRules.GridX(280f);
            zombie.PresentationPreviousX = zombie.X;
            return zombie;
        }

        /// <summary>Advances the complete production board by native centisecond updates.</summary>
        /// <param name="game">The production simulation.</param>
        /// <param name="count">The native update count.</param>
        private static void Tick(GameSimulation game, int count)
        {
            for (var tick = 0; tick < count; tick++)
            {
                game.Update(.01f);
            }
        }

        /// <summary>Invokes an existing production entry point without replacing its behavior.</summary>
        /// <param name="game">The production simulation.</param>
        /// <param name="name">The existing simulation method.</param>
        /// <param name="arguments">The immediately consumed original method arguments.</param>
        /// <returns>The original method result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
