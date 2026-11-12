#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original final-wave grave ambush and its independent native phase.</summary>
    public sealed class AdventureZombieRiseTests
    {
        /// <summary>Checks the original one-hundred-tick underground wait and fifty-tick ascent.</summary>
        [Test]
        public void NativeAltitudeAndClipFollowOriginalCounters()
        {
            Assert.That(AdventureZombieRiseRules.Altitude(150), Is.EqualTo(-200f));
            Assert.That(AdventureZombieRiseRules.Altitude(50), Is.EqualTo(-200f));
            Assert.That(AdventureZombieRiseRules.Altitude(49), Is.EqualTo(-196f));
            Assert.That(AdventureZombieRiseRules.Altitude(25), Is.EqualTo(-100f));
            Assert.That(AdventureZombieRiseRules.Altitude(0), Is.Zero);
            Assert.That(AdventureZombieRiseRules.ClipBottom(150f, 150), Is.EqualTo(223f));
            Assert.That(AdventureZombieRiseRules.ClipBottom(150f, 25), Is.EqualTo(238f));
            Assert.That(AdventureZombieRiseRules.ClipBottom(150f, 0), Is.EqualTo(263f));
        }

        /// <summary>Checks the actual final-wave grave branch creates the native rise, particles, sound, and delayed movement.</summary>
        [Test]
        public void FinalWaveGraveAmbushRisesBeforeWalkingOrTakingProjectileDamage()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Adventure._graves.Clear();
            game.Adventure._graves.Add(new AdventureCellTimer { Row = 1, Column = 6, Remaining = float.MaxValue });
            Invoke(game, "SpawnScheduledFinalAmbush");
            Assert.That(game.Zombies.Count, Is.EqualTo(1));
            var zombie = game.Zombies[0];
            var startX = zombie.X;
            Assert.That(AdventureZombieNativeRules.NativePositionX(startX), Is.EqualTo(495f).Within(.001f));
            Assert.That(zombie.DropRemaining, Is.Zero, "Grave emergence must not reuse the airborne falling animation.");
            Assert.That(zombie.GraveRiseTicks, Is.EqualTo(150));
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(14));
            Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(zombie), Is.False);
            Assert.That(game.Presentation.ZombieRiseEffects.Count, Is.EqualTo(1));
            Assert.That(game.Presentation.ZombieRiseEffects[0].X, Is.EqualTo(555f));
            Assert.That(game.Presentation.ZombieRiseEffects[0].Y, Is.EqualTo(260f));
            Assert.That(game.Presentation.ZombieSounds.Exists(sound => sound.Foley == 79), Is.True);
            Assert.That(AdventureZombieFoleyData.Definitions[79].Samples, Is.EqualTo(new[] { "gravestone_rumble" }));

            var health = zombie.Health;
            Invoke(game, "HitAdventureZombie", zombie, 20f, false, 0U);
            Assert.That(zombie.Health, Is.EqualTo(health), "The original rising phase is immune to ordinary weapon damage.");
            for (var tick = 0; tick < 149; tick++)
            {
                Invoke(game, "AdvanceAdventureZombies", .01f);
                Assert.That(zombie.X, Is.EqualTo(startX), "An emerging zombie cannot move before its native counter expires.");
                Assert.That(zombie.Eating, Is.False);
            }

            Assert.That(zombie.GraveRiseTicks, Is.EqualTo(1));
            Invoke(game, "AdvanceAdventureZombies", .01f);
            Assert.That(zombie.GraveRiseTicks, Is.Zero);
            Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(zombie), Is.True);
            Invoke(game, "AdvanceAdventureZombies", .01f);
            Assert.That(zombie.X, Is.LessThan(startX), "The emerged zombie resumes its original walking branch.");
        }

        /// <summary>Invokes a production simulation hook without broadening its runtime visibility.</summary>
        /// <param name="game">The isolated board fixture.</param>
        /// <param name="name">The private hook to invoke.</param>
        /// <param name="arguments">The production hook arguments.</param>
        private static void Invoke(GameSimulation game, string name, params object[] arguments)
        {
            typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
