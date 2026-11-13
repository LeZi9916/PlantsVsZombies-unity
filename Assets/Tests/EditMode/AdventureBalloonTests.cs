#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the PC balloon's appearance cue, flying attachment, original pop and landing transition.</summary>
    public sealed class AdventureBalloonTests
    {
        /// <summary>Checks PC initialization preserves altitude twenty-five and requests Foley sixty with its original pitch range.</summary>
        [Test]
        public void SpawnRetainsOriginalFlightAltitudeAndInflationSound()
        {
            var game = Board();
            var balloon = Spawn(game, 1);
            Assert.That(balloon.PresentationNativePhase, Is.EqualTo(73));
            Assert.That(balloon.PresentationAltitude, Is.EqualTo(25f));
            Assert.That(balloon.Airborne, Is.True);
            Assert.That(balloon.FlyingHealth, Is.EqualTo(20));
            Assert.That(balloon.Health, Is.EqualTo(270f));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 60), Has.Count.EqualTo(1));
            Assert.That(AdventureZombieFoleyData.Definitions[60].Samples, Is.EqualTo(new[] { "ballooninflate" }));
            Assert.That(AdventureZombieFoleyData.Definitions[60].PitchRange, Is.EqualTo(10f));
        }

        /// <summary>Checks the original stationary twenty-eight-frame pop remains flying until its once-and-hold clip finishes.</summary>
        [Test]
        public void PoppedBalloonFallsOnePixelPerTickThenStartsUnblendedWalking()
        {
            var game = Board();
            var balloon = Spawn(game, 1);
            var x = balloon.X;
            var velocity = balloon.PresentationNativeVelocity;
            Invoke(game, "HitAdventureZombie", balloon, 20f, false, 0U);
            Assert.That(balloon.PresentationNativePhase, Is.EqualTo(74));
            Assert.That(balloon.PresentationNativeHeight, Is.EqualTo(7));
            Assert.That(balloon.Airborne, Is.True);
            Assert.That(balloon.Health, Is.EqualTo(270f));
            Assert.That(balloon.FlyingHealth, Is.Zero);
            Assert.That(balloon.PresentationNativeVelocity, Is.EqualTo(velocity));
            Assert.That(game.AdventurePresentationZombieAnimation(balloon), Is.EqualTo("anim_pop"));
            Assert.That(game.AdventurePresentationZombieRate(balloon), Is.EqualTo(24f));
            Assert.That(AdventureZombieMotionRules.PlaysOnce(balloon), Is.True);
            Assert.That(balloon.PresentationBalloonBlendTicks, Is.EqualTo(20));
            Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(balloon, PlantKind.Cactus), Is.False);
            Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(balloon, PlantKind.Peashooter), Is.False);
            Assert.That(Invoke(game, "CanTakeAdventureAshDamage", balloon, 0x7FU), Is.True);
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 22), Has.Count.EqualTo(1));
            balloon.ChillRemaining = 10f;
            game.Update(.24f);
            Assert.That(balloon.PresentationAltitude, Is.EqualTo(1f));
            Assert.That(balloon.X, Is.EqualTo(x));
            game.Update(.01f);
            Assert.That(balloon.PresentationAltitude, Is.Zero);
            Assert.That(balloon.PresentationNativeHeight, Is.Zero);
            game.Update(.91f);
            Assert.That(balloon.PresentationNativePhase, Is.EqualTo(74));
            Assert.That(balloon.X, Is.EqualTo(x));
            game.Update(.01f);
            Assert.That(balloon.PresentationNativePhase, Is.EqualTo(75));
            Assert.That(balloon.Airborne, Is.False);
            Assert.That(balloon.PresentationBalloonBlendTicks, Is.Zero);
            Assert.That(game.AdventurePresentationZombieAnimation(balloon), Is.EqualTo("anim_walk"));
            Assert.That(balloon.PresentationNativeVelocity, Is.InRange(.23f, .37f));
        }

        /// <summary>Checks PC LandFlyer removes a popped pool-row balloon immediately instead of inventing a swimming landing.</summary>
        [Test]
        public void PoppedBalloonOnWaterIsRemovedWithoutALandCorpse()
        {
            var game = Board();
            var balloon = Spawn(game, 2);
            Assert.That(balloon.PresentationAltitude, Is.EqualTo(25f));
            Invoke(game, "HitAdventureZombie", balloon, 20f, false, 0U);
            Assert.That(game.Zombies.Contains(balloon), Is.False);
            Assert.That(game.Presentation.Corpses, Is.Empty);
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 22), Has.Count.EqualTo(1));
        }

        /// <summary>Checks the original wind adds ten pixels per update and exits only after the previous integer position passes 850.</summary>
        [Test]
        public void BlowerKeepsBalloonVisibleAndFlyingUntilOriginalRightEdgeExit()
        {
            var game = Board();
            var balloon = Spawn(game, 1);
            var popping = Spawn(game, 0);
            Invoke(game, "HitAdventureZombie", popping, 20f, false, 0U);
            Invoke(game, "BlowAdventureFliersAway");
            Assert.That(balloon.BlowingAway, Is.True);
            Assert.That(popping.BlowingAway, Is.False);
            Assert.That(game.Zombies.Contains(balloon), Is.True);
            var before = balloon.X;
            var velocity = balloon.PresentationNativeVelocity;
            game.Update(.01f);
            Assert.That((balloon.X - before) * 80f, Is.EqualTo(10f - velocity).Within(.001f));
            Assert.That(balloon.PresentationNativePhase, Is.EqualTo(73));
            Assert.That(game.AdventurePresentationZombieAnimation(balloon), Is.EqualTo("anim_idle"));
            balloon.FrozenRemaining = 1f;
            before = balloon.X;
            game.Update(.01f);
            Assert.That((balloon.X - before) * 80f, Is.EqualTo(10f).Within(.001f), "Native wind still moves an immobilized balloon.");
            balloon.X = AdventureZombieNativeRules.GridX(850.9f);
            Assert.That(Invoke(game, "AdvanceAdventureBalloonWind", balloon), Is.False);
            Assert.That(game.Zombies.Contains(balloon), Is.True, "PC compares integer mX, so 850.9 still remains for this update.");
            var draws = game.BattleRandomDrawCount;
            Assert.That(Invoke(game, "AdvanceAdventureBalloonWind", balloon), Is.True);
            Assert.That(game.Zombies.Contains(balloon), Is.False);
            Assert.That(game.Presentation.Corpses, Is.Empty);
            Assert.That(game.BattleRandomDrawCount, Is.GreaterThan(draws), "The original wind endpoint retains the ordinary loot roll.");
        }

        /// <summary>Checks native bit sixteen suppresses the pop sound and artwork on an instantly destroyed flying body.</summary>
        [Test]
        public void SuppressedPartsDoNotPlayThePopTransition()
        {
            var game = Board();
            var balloon = Spawn(game, 1);
            Invoke(game, "HitAdventureZombie", balloon, 1800f, false, 16U);
            Assert.That(game.Zombies.Contains(balloon), Is.False);
            Assert.That(game.Presentation.Corpses, Is.Empty);
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 22), Is.Empty);
        }

        /// <summary>Creates an active original fog-and-pool board with automatic waves postponed.</summary>
        /// <returns>The deterministic production board.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 38 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Creates a balloon through the production initializer at a fixed in-lawn position.</summary>
        /// <param name="game">The active board.</param>
        /// <param name="row">The original dry or pool row.</param>
        /// <returns>The actual living balloon.</returns>
        private static Zombie Spawn(GameSimulation game, int row)
        {
            return (Zombie)Invoke(game, "SpawnAdventureZombie", ZombieKind.Balloon, row, 5f, 0)!;
        }

        /// <summary>Invokes an existing native operation without adding a test-only runtime API.</summary>
        /// <param name="game">The production board owning the operation.</param>
        /// <param name="name">The private production operation.</param>
        /// <param name="arguments">The original operation arguments.</param>
        /// <returns>The original operation result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
