#nullable enable

using System;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original independent coffee/wake lifetimes and settled-bank recommendation brightness.</summary>
    public sealed class AdventureCoffeeBeanTests
    {
        /// <summary>Checks the original flying actor, hundred-update special, crumble lifetime and overlapping mushroom wake.</summary>
        /// <param name="level">The dry, water or roof daytime background.</param>
        /// <param name="row">The legal planting row.</param>
        [TestCase(9, 1)]
        [TestCase(29, 2)]
        [TestCase(49, 1)]
        public void CoffeeActorAndMushroomUseIndependentOriginalCounters(int level, int row)
        {
            var game = Board(level);
            if (game.Adventure.Level.HasRoof || game.Adventure.Level.IsWaterRow(row))
            {
                Place(game, game.Adventure.Level.HasRoof ? PlantKind.FlowerPot : PlantKind.LilyPad, row);
            }

            var mushroom = Place(game, PlantKind.PuffShroom, row);
            var coffee = Place(game, PlantKind.CoffeeBean, row);
            var state = coffee.CoffeePresentation!;
            Assert.That(coffee, Is.Not.SameAs(mushroom));
            Assert.That(coffee.Layer, Is.EqualTo(PlantLayer.Effect));
            Assert.That(game.AdventurePlantAt(row, 4, PlantLayer.Main), Is.SameAs(mushroom));
            Assert.That(game.AdventurePlantAt(row, 4, PlantLayer.Effect), Is.SameAs(coffee));
            Assert.That(mushroom.WakeTimer, Is.Zero);
            Assert.That(state.Countdown, Is.EqualTo(100));
            Assert.That(Invoke(game, "CheckAdventurePlanting", PlantKind.CoffeeBean, row, 4, true),
                Is.EqualTo(AdventurePlantingFailure.NeedsSleepingMushroom));
            Tick(game, 99);
            Assert.That(state.Crumbling, Is.False);
            Assert.That(state.Countdown, Is.EqualTo(1));
            Assert.That(mushroom.WakeTimer, Is.Zero);
            Assert.That(Sounds(game, 69), Is.Zero);
            Tick(game, 1);
            Assert.That(state.Crumbling, Is.True);
            Assert.That(state.AnimationTicks, Is.EqualTo(1));
            Assert.That(mushroom.WakeTimer, Is.EqualTo(1f));
            Assert.That(mushroom.Asleep, Is.True);
            Assert.That(Sounds(game, 69), Is.EqualTo(1));
            Tick(game, 40);
            Assert.That(mushroom.WakeTimer, Is.EqualTo(.6f).Within(.000001f));
            Assert.That(Sounds(game, 83), Is.EqualTo(1));
            Assert.That(game.Plants, Has.Member(coffee));
            Tick(game, 28);
            Assert.That(state.AnimationTicks, Is.EqualTo(69));
            Assert.That(game.Plants, Has.Member(coffee));
            Tick(game, 1);
            Assert.That(game.Plants, Has.No.Member(coffee));
            Assert.That(mushroom.Asleep, Is.True);
            Assert.That(mushroom.SleepingPresentation, Is.Not.Null);
            Tick(game, 30);
            Assert.That(mushroom.Asleep, Is.True);
            Assert.That(mushroom.WakeTimer, Is.EqualTo(.01f));
            Tick(game, 1);
            Assert.That(mushroom.Asleep, Is.False);
            Assert.That(mushroom.SleepingPresentation, Is.Null);
            Assert.That(mushroom.AwakeByCoffee, Is.True);
            Assert.That(mushroom.PresentationCoffeeWakeAge, Is.EqualTo(mushroom.Age));
            Assert.That(Sounds(game, 69), Is.EqualTo(1));
            Assert.That(Sounds(game, 83), Is.EqualTo(1));
        }

        /// <summary>Checks captured coffee suspends its own special without prematurely waking the ungrabbed mushroom.</summary>
        [Test]
        public void CapturedCoffeeDoesNotStartItsMushroomWakeCounter()
        {
            var game = Board(49);
            var mushroom = Place(game, PlantKind.PuffShroom, 1);
            var coffee = Place(game, PlantKind.CoffeeBean, 1);
            Tick(game, 90);
            coffee.BungeeState = 1;
            Tick(game, 30);
            Assert.That(coffee.CoffeePresentation!.Countdown, Is.EqualTo(10));
            Assert.That(mushroom.WakeTimer, Is.Zero);
            Assert.That(mushroom.Asleep, Is.True);
            Assert.That(Sounds(game, 69), Is.Zero);
            coffee.BungeeState = 0;
            Tick(game, 10);
            Assert.That(mushroom.WakeTimer, Is.EqualTo(1f));
            Assert.That(Sounds(game, 69), Is.EqualTo(1));
        }

        /// <summary>Checks grabbing a crumbling bean suspends its own update while body playback and the mushroom's wake remain independent.</summary>
        [Test]
        public void GrabbedCrumblingCoffeeKeepsAnimatingAndCannotPauseItsMushroomWake()
        {
            var game = Board(49);
            var mushroom = Place(game, PlantKind.PuffShroom, 1);
            var coffee = Place(game, PlantKind.CoffeeBean, 1);
            Tick(game, 120);
            var state = coffee.CoffeePresentation!;
            Assert.That(state.AnimationTicks, Is.EqualTo(21f));
            coffee.BungeeState = 1;
            Tick(game, 60);
            Assert.That(state.AnimationTicks, Is.EqualTo(81f));
            Assert.That(game.Plants, Has.Member(coffee));
            Assert.That(mushroom.WakeTimer, Is.EqualTo(.2f).Within(.000001f));
            coffee.BungeeState = 0;
            Tick(game, 1);
            Assert.That(game.Plants, Has.No.Member(coffee));
            Tick(game, 19);
            Assert.That(mushroom.Asleep, Is.False);
        }

        /// <summary>Checks the bean finishes its own crumble even if the original mushroom is removed before its special.</summary>
        [Test]
        public void MissingMushroomDoesNotDeleteCoffeeBeforeItsOriginalCrumble()
        {
            var game = Board(49);
            var mushroom = Place(game, PlantKind.PuffShroom, 1);
            var coffee = Place(game, PlantKind.CoffeeBean, 1);
            Assert.That(game.TryShovelAdventurePlant(mushroom), Is.True);
            Tick(game, 100);
            Assert.That(coffee.CoffeePresentation!.Crumbling, Is.True);
            Assert.That(game.Plants, Has.Member(coffee));
            Assert.That(Sounds(game, 69), Is.EqualTo(1));
            Tick(game, 69);
            Assert.That(game.Plants, Has.No.Member(coffee));
        }

        /// <summary>Checks the native wake curve compresses and stretches while returning to its initial height at both ends.</summary>
        [Test]
        public void OriginalWakeCurveHasTheNativeSquishAndStretchEnvelope()
        {
            Assert.That(AdventureCoffeeBeanState.WakeScale(100), Is.EqualTo(1f));
            Assert.That(AdventureCoffeeBeanState.WakeScale(70), Is.EqualTo(1f));
            Assert.That(AdventureCoffeeBeanState.WakeScale(60), Is.LessThan(1f));
            Assert.That(AdventureCoffeeBeanState.WakeScale(35), Is.EqualTo(1f).Within(.000001f));
            Assert.That(AdventureCoffeeBeanState.WakeScale(10), Is.GreaterThan(1f));
            Assert.That(AdventureCoffeeBeanState.WakeScale(0), Is.EqualTo(1f));
        }

        /// <summary>Checks chooser warnings and brightness react only to settled coffee, with selected homes retaining grayness fifty-five.</summary>
        [Test]
        public void CoffeeSelectionRestoresOnlyNocturnalHomesAfterItsBankFlight()
        {
            var game = Board(49);
            var state = new AdventureSeedChooserState(Array.Empty<PlantKind>());
            Assert.That(Grayness(game, state, PlantKind.PuffShroom), Is.EqualTo(115));
            Assert.That(Grayness(game, state, PlantKind.LilyPad), Is.EqualTo(115));
            Assert.That(state.Choose(PlantKind.CoffeeBean, 22, 128, 8), Is.True);
            state.Advance(.24f);
            Assert.That(state.PickedPlantType(PlantKind.CoffeeBean, null), Is.False);
            Assert.That(Grayness(game, state, PlantKind.PuffShroom), Is.EqualTo(115));
            state.Advance(.01f);
            Assert.That(state.PickedPlantType(PlantKind.CoffeeBean, null), Is.True);
            Assert.That(Grayness(game, state, PlantKind.PuffShroom), Is.EqualTo(255));
            Assert.That(Grayness(game, state, PlantKind.LilyPad), Is.EqualTo(115));
            Assert.That(state.HomeGrayness(PlantKind.CoffeeBean, true), Is.EqualTo(55));
            Assert.That(state.Return(PlantKind.CoffeeBean, 22, 128, 8), Is.True);
            Assert.That(Grayness(game, state, PlantKind.PuffShroom), Is.EqualTo(115));
        }

        /// <summary>Checks original PickedPlantType recognizes settled Imitater coffee.</summary>
        [Test]
        public void SettledImitaterCoffeeAlsoRemovesOnlyTheSleepRecommendation()
        {
            var state = new AdventureSeedChooserState(new[] { PlantKind.Imitater });
            Assert.That(state.PickedPlantType(PlantKind.CoffeeBean, PlantKind.CoffeeBean), Is.True);
            Assert.That(state.PickedPlantType(PlantKind.CoffeeBean, PlantKind.PuffShroom), Is.False);
        }

        /// <summary>Combines the production recommendation decision and native chooser state grayness.</summary>
        /// <param name="game">The original board whose terrain supplies the recommendation.</param>
        /// <param name="state">The original chooser bank and flight state.</param>
        /// <param name="kind">The home packet being inspected.</param>
        /// <returns>The original home RGB brightness.</returns>
        private static int Grayness(GameSimulation game, AdventureSeedChooserState state, PlantKind kind)
        {
            var warning = game.AdventureChooserWarningKey(kind, state.PickedPlantType(PlantKind.CoffeeBean, null));
            return state.HomeGrayness(kind, warning.Length == 0);
        }

        /// <summary>Creates a controlled daytime production board without scheduled waves or initial occupants.</summary>
        /// <param name="level">The original ground, water or roof level.</param>
        /// <returns>The initialized playing board.</returns>
        private static GameSimulation Board(int level)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            SimulationFixture.Plants(game).Clear();
            SimulationFixture.Zombies(game).Clear();
            return game;
        }

        /// <summary>Places an original plant in native column four.</summary>
        /// <param name="game">The controlled playing board.</param>
        /// <param name="kind">The original seed identifier.</param>
        /// <param name="row">The legal zero-based lane.</param>
        /// <returns>The live initialized actor.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind, int row)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", kind, row, 4)!;
        }

        /// <summary>Advances complete native production updates.</summary>
        /// <param name="game">The playing board.</param>
        /// <param name="ticks">The number of hundred-hertz updates.</param>
        private static void Tick(GameSimulation game, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                game.Update(.01f);
            }
        }

        /// <summary>Counts original Foley events emitted by an action.</summary>
        /// <param name="game">The source production board.</param>
        /// <param name="foley">The original Foley identifier.</param>
        /// <returns>The number of matching native events.</returns>
        private static int Sounds(GameSimulation game, int foley)
        {
            return game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == foley).Count;
        }

        /// <summary>Invokes a production boundary without substituting its underlying behavior.</summary>
        /// <param name="game">The source production board.</param>
        /// <param name="method">The original action method name.</param>
        /// <param name="arguments">The original action parameters.</param>
        /// <returns>The original action result.</returns>
        /// <exception cref="TargetInvocationException">The production action throws an exception.</exception>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return SimulationInspection.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
