#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original Blover body playback, delayed wind, continuing loop, and special-state removal.</summary>
    public sealed class AdventureBloverTests
    {
        /// <summary>Checks the planted body immediately blows, starts its effect on update fifty, loops at one second, and dies after update 250.</summary>
        [Test]
        public void BloverUsesOriginalBlowEffectLoopAndRemovalTicks()
        {
            var game = Board();
            var plant = Place(game);
            var flying = Spawn(game, 73);
            var popping = Spawn(game, 74);
            var friendly = Spawn(game, 73);
            friendly.Hypnotized = true;
            var state = plant.BloverPresentation!;
            Assert.That(state.Animation, Is.EqualTo("anim_blow"));
            Assert.That(AdventureBloverState.Rate, Is.EqualTo(20f));
            Assert.That(state.Loop, Is.False);
            for (var tick = 0; tick < 49; tick++)
            {
                Tick(game);
            }

            Assert.That(state.SpecialTicks, Is.EqualTo(1));
            Assert.That(flying.BlowingAway, Is.False);
            Assert.That(game.Adventure.FogBlownRemaining, Is.Zero);
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == -2), Is.Empty);
            Tick(game);
            Assert.That(state.Triggered, Is.True);
            Assert.That(state.DisappearTicks, Is.EqualTo(200));
            Assert.That(flying.BlowingAway, Is.True);
            Assert.That(friendly.BlowingAway, Is.True, "Original wind has no hostile-faction filter.");
            Assert.That(popping.BlowingAway, Is.False, "Original BlowAwayFliers explicitly excludes phase 74.");
            Assert.That(game.Zombies, Has.Member(flying), "The wind starts flight movement instead of removing the actor immediately.");
            Assert.That(game.Adventure.FogBlownRemaining, Is.EqualTo(40f));
            Assert.That(game.Plants, Has.Member(plant));
            for (var tick = 50; tick < 100; tick++)
            {
                Tick(game);
            }

            Assert.That(state.Animation, Is.EqualTo("anim_loop"));
            Assert.That(state.SampleSeconds(0f), Is.Zero);
            Assert.That(state.Loop, Is.True);
            for (var tick = 100; tick < 250; tick++)
            {
                Tick(game);
            }

            Assert.That(state.DisappearTicks, Is.Zero);
            Assert.That(game.Plants, Has.Member(plant), "PC 4715AB removes only when the special lifetime becomes negative.");
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == -2), Has.Count.EqualTo(1));
            Tick(game);
            Assert.That(game.Plants, Has.No.Member(plant));
        }

        /// <summary>Checks board pause holds the windup and the ordinary fixed update resumes its authored clock.</summary>
        [Test]
        public void BoardPauseHoldsBloverAnimationAndSpecialCountdown()
        {
            var game = Board();
            var plant = Place(game);
            var state = plant.BloverPresentation!;
            game.Update(.01f);
            Assert.That(state.AgeTicks, Is.EqualTo(1));
            game.TogglePause();
            game.Update(.1f);
            Assert.That(state.AgeTicks, Is.EqualTo(1));
            Assert.That(state.SpecialTicks, Is.EqualTo(49));
            game.TogglePause();
            game.Update(.01f);
            Assert.That(state.AgeTicks, Is.EqualTo(2));
        }

        /// <summary>Creates a playing fog board without unrelated wave spawns.</summary>
        /// <returns>The production board used by these original timing checks.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 32 }, 91);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Plants a Blover through the original initialization boundary.</summary>
        /// <param name="game">The board receiving the planted Blover.</param>
        /// <returns>The actual initialized plant.</returns>
        private static Plant Place(GameSimulation game)
        {
            return (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { PlantKind.Blover, 0, 4 })!;
        }

        /// <summary>Creates a native balloon and selects its live flight or popping phase.</summary>
        /// <param name="game">The board receiving the balloon.</param>
        /// <param name="phase">The native balloon phase under the wind's target gate.</param>
        /// <returns>The live balloon actor.</returns>
        private static Zombie Spawn(GameSimulation game, int phase)
        {
            var zombie = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { ZombieKind.Balloon, 0, 6f, 0 })!;
            zombie.PresentationNativePhase = phase;
            return zombie;
        }

        /// <summary>Advances only plant actions so the wind's exact trigger can be observed before zombie movement and fog decay.</summary>
        /// <param name="game">The production board whose plants are advanced.</param>
        private static void Tick(GameSimulation game)
        {
            typeof(GameSimulation).GetMethod("AdvanceAdventurePlants", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { .01f });
        }
    }
}
