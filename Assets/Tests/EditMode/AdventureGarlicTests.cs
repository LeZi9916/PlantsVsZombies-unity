#nullable enable
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC 472AD0/547230/547380 garlic damage, reactions, lane motion and Foley.</summary>
    public sealed class AdventureGarlicTests
    {
        /// <summary>Checks the strict integer damage boundaries and original replacement face.</summary>
        /// <param name="health">The remaining native health.</param>
        /// <param name="stage">The expected native damage stage.</param>
        [TestCase(400f, 0)]
        [TestCase(266f, 0)]
        [TestCase(265f, 1)]
        [TestCase(133f, 1)]
        [TestCase(132f, 2)]
        public void DamageUsesOriginalGarlicArtwork(float health, int stage)
        {
            Assert.That(AdventureDefensivePlantRules.DamageStage(PlantKind.Garlic, health), Is.EqualTo(stage));
            Assert.That(AdventureDefensivePlantRules.DamageTrack(PlantKind.Garlic), Is.EqualTo("anim_face"));
            Assert.That(AdventureDefensivePlantRules.DamageImage(PlantKind.Garlic, health),
                Is.EqualTo(stage == 0 ? null : stage == 1 ? "IMAGE_REANIM_GARLIC_BODY2" : "IMAGE_REANIM_GARLIC_BODY3"));
        }

        /// <summary>Checks every native species' long or short reaction, including non-chewers.</summary>
        /// <param name="kind">The original species.</param>
        [TestCaseSource(nameof(Species))]
        public void ReactionUsesSpeciesFaceSupport(ZombieKind kind)
        {
            var game = Board(1);
            var zombie = Spawn(game, kind, 2);
            zombie.Eating = true;
            zombie.PresentationGarlicCounter = 0;
            for (var tick = 0; tick < 21; tick++)
            {
                Invoke(game, "AdvanceAdventureGarlicBite", zombie, .01f);
            }
            Assert.That(zombie.PresentationGarlicCounter, Is.EqualTo(AdventureGarlicRules.HasFace(kind) ? 21 : 170));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 72).Count,
                Is.EqualTo(AdventureGarlicRules.HasFace(kind) ? 0 : 1));
        }

        /// <summary>Checks plant-headed bodies and the additional native duck type retain their original face eligibility.</summary>
        /// <param name="nativeKind">The original executable's species identifier.</param>
        /// <param name="longPause">Whether that native type has the original gross-out image.</param>
        [TestCase(26, false)]
        [TestCase(27, false)]
        [TestCase(28, false)]
        [TestCase(29, false)]
        [TestCase(30, false)]
        [TestCase(31, false)]
        [TestCase(32, false)]
        [TestCase(33, true)]
        public void ExtendedSpeciesUseNativeTypeBeforeBodyMapping(int nativeKind, bool longPause)
        {
            var game = Board(1);
            var zombie = Spawn(game, (ZombieKind)nativeKind, 2);
            zombie.Eating = true;
            zombie.PresentationGarlicCounter = 0;
            for (var tick = 0; tick < 21; tick++)
            {
                Invoke(game, "AdvanceAdventureGarlicBite", zombie, .01f);
            }
            Assert.That(AdventureGarlicRules.HasFace(zombie), Is.EqualTo(longPause));
            Assert.That(zombie.PresentationGarlicCounter, Is.EqualTo(longPause ? 21 : 170));
            Assert.That(AdventureGarlicRules.ShowsFace(zombie), Is.False);
        }

        /// <summary>Checks long reactions survive removal of the bitten plant and preserve the paused animation clock.</summary>
        [Test]
        public void FacePauseDoesNotDependOnGarlicLifetime()
        {
            var game = Board(1);
            var zombie = Spawn(game, ZombieKind.Normal, 2);
            var garlic = new Plant { Kind = PlantKind.Garlic, Row = 2, Column = 4, Health = 400f };
            SimulationFixture.Plants(game).Add(garlic);
            zombie.Eating = true;
            SimulationFixture.SynchronizeZombieAnimation(game, zombie);
            zombie.PresentationAnimationAge = .2f;
            Invoke(game, "AnimateAdventureChew", zombie, garlic, false);
            SimulationFixture.Plants(game).Remove(garlic);
            for (var tick = 0; tick < 69; tick++)
            {
                Invoke(game, "AdvanceAdventureZombieClock", zombie, .01f);
                Invoke(game, "AdvanceAdventureGarlicBite", zombie, .01f);
            }
            Assert.That(zombie.PresentationAnimationAge, Is.EqualTo(.2f));
            Assert.That(zombie.Eating, Is.True);
            Assert.That(AdventureGarlicRules.ShowsFace(zombie), Is.False);
            Invoke(game, "AdvanceAdventureGarlicBite", zombie, .01f);
            Assert.That(zombie.Eating, Is.False);
            Assert.That(AdventureGarlicRules.ShowsFace(zombie), Is.True);
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 72).Count, Is.EqualTo(1));
            Assert.That(AdventureGarlicRules.FaceTrackVisible("anim_head_jaw"), Is.False);
        }

        /// <summary>Checks pool boundaries and the one-pixel approach shared by collision and visible position.</summary>
        [Test]
        public void RedirectMovesContinuouslyWithinItsTerrain()
        {
            var game = Board(21);
            var zombie = Spawn(game, ZombieKind.DuckyTube, 2);
            zombie.PresentationGarlicCounter = 169;
            Invoke(game, "AdvanceAdventureGarlicBite", zombie, .01f);
            Assert.That(zombie.Row, Is.EqualTo(3));
            Assert.That(zombie.PresentationGarlicRowOffset, Is.EqualTo(-84f));
            for (var tick = 0; tick < 84; tick++)
            {
                Invoke(game, "AdvanceAdventureGarlicBite", zombie, .01f);
            }
            Assert.That(zombie.PresentationGarlicRowOffset, Is.Zero);
            for (var tick = 0; tick < 17; tick++)
            {
                Invoke(game, "AdvanceAdventureGarlicBite", zombie, .01f);
            }
            Assert.That(zombie.PresentationGarlicCounter, Is.EqualTo(-1));
            Assert.That(AdventureGarlicRules.ShowsFace(zombie), Is.False);
        }

        /// <summary>Checks garlic replaces an existing dancer arm-raise with the native unblended held walking pose.</summary>
        [Test]
        public void DancerGarlicPauseUsesWalkInsteadOfItsDancePhase()
        {
            var game = Board(1);
            var zombie = Spawn(game, ZombieKind.Dancing, 2);
            zombie.PresentationNativePhase = 45;
            zombie.Eating = true;
            zombie.PresentationGarlicCounter = 69;
            SimulationFixture.SynchronizeZombieAnimation(game, zombie);
            Invoke(game, "AdvanceAdventureGarlicBite", zombie, .01f);
            Assert.That(SimulationFixture.SynchronizeZombieAnimation(game, zombie), Is.EqualTo("anim_walk"));
            Assert.That(zombie.PresentationBodyBlendTicks, Is.Zero);
            zombie.PresentationGarlicCounter = 169;
            Invoke(game, "AdvanceAdventureGarlicBite", zombie, .01f);
            Assert.That(zombie.PresentationBodyBlendTicks, Is.EqualTo(20));
        }

        /// <summary>Checks the original reaction is silent after head loss.</summary>
        [Test]
        public void HeadlessReactionDoesNotRequestYuck()
        {
            var game = Board(1);
            var zombie = Spawn(game, ZombieKind.Normal, 2);
            zombie.Headless = true;
            zombie.PresentationGarlicCounter = 69;
            game.Presentation.ZombieSounds.Clear();
            Invoke(game, "AdvanceAdventureGarlicBite", zombie, .01f);
            Assert.That(game.Presentation.ZombieSounds, Is.Empty);
            Assert.That(AdventureGarlicRules.ShowsFace(zombie), Is.False);
        }

        /// <summary>Enumerates each distinct original zombie species.</summary>
        private static System.Array Species => System.Enum.GetValues(typeof(ZombieKind));

        /// <summary>Creates a quiet original-terrain board.</summary>
        /// <param name="level">The native adventure level.</param>
        /// <returns>The isolated simulation.</returns>
        private static GameSimulation Board(int level)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Creates an initialized actor inside the lawn.</summary>
        /// <param name="game">The owning simulation.</param>
        /// <param name="kind">The original species.</param>
        /// <param name="row">The starting row.</param>
        /// <returns>The initialized actor.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind, int row)
        {
            var zombie = (Zombie)Invoke(game, "SpawnAdventureZombie", kind, row, 5f, 0)!;
            game.Presentation.ZombieSounds.Clear();
            return zombie;
        }

        /// <summary>Calls a production collaborator through the existing test inspection boundary.</summary>
        /// <param name="game">The owning simulation.</param>
        /// <param name="name">The native operation name.</param>
        /// <param name="arguments">The operation arguments.</param>
        /// <returns>The operation result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return SimulationInspection.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
