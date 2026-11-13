#nullable enable

using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original kelp attachment and hundred-update capture through the production simulation.</summary>
    public sealed class AdventureTangleKelpTests
    {
        /// <summary>Checks native attachment offsets without replacing the victim with an immediate kill.</summary>
        /// <param name="kind">The native swimming zombie kind.</param>
        /// <param name="offsetX">The original attachment's horizontal offset.</param>
        /// <param name="offsetY">The original attachment's vertical offset.</param>
        [TestCase(ZombieKind.Normal, -13, 15)]
        [TestCase(ZombieKind.Snorkel, -43, 55)]
        [TestCase(ZombieKind.DolphinRider, -20, 37)]
        public void GrabRetainsVictimWithOriginalSpeciesOffsets(ZombieKind kind, int offsetX, int offsetY)
        {
            var game = Board();
            var kelp = Place(game, PlantKind.TangleKelp, 4);
            var target = Target(game, kind, 4.5f);
            if (kind == ZombieKind.DolphinRider)
            {
                Place(game, PlantKind.LilyPad, 3);
                Place(game, PlantKind.Wallnut, 3);
                target.AbilityTimer = 0f;
            }

            game.Update(.01f);
            if (kind == ZombieKind.DolphinRider)
            {
                Assert.That(AdventureZombieMotionRules.ClipHeight(target, 1f), Is.EqualTo(-10f));
                Assert.That(AdventureZombieMotionRules.ClipBottom(target, 220f, 1f), Is.EqualTo(343f));
                Assert.That(target.UsedAbility, Is.False, "A kelp victim cannot begin the generic dolphin vault.");
                Assert.That(target.X, Is.GreaterThan(4.45f), "Grabbing must prevent the generic vault's 1.1-cell teleport.");
            }

            var state = kelp.TangleKelpPresentation;
            Assert.That(state, Is.Not.Null);
            Assert.That(state!.Countdown, Is.EqualTo(100));
            Assert.That(state.TargetZombieId, Is.EqualTo(target.Id));
            Assert.That(state.OffsetX, Is.EqualTo(offsetX));
            Assert.That(state.OffsetY, Is.EqualTo(offsetY));
            Assert.That(target.TangleKelpGrab, Is.SameAs(state));
            Assert.That(game.Zombies, Has.Member(target));
            Assert.That(kelp.PresentationBiteNativeState, Is.EqualTo(34));
            Assert.That(CountSound(game, 68), Is.EqualTo(1));
            Assert.That(CountSound(game, 52), Is.EqualTo(1));
        }

        /// <summary>Checks the PC riding and dismounted dolphin's distinct dragged waterline branches.</summary>
        /// <param name="phase">The original dolphin phase.</param>
        /// <param name="height">The native clip height at zero altitude.</param>
        /// <param name="bottom">The original board-space clip edge in pool row two.</param>
        [TestCase(53, -15f, 348f)]
        [TestCase(55, 36f, 347f)]
        public void DraggedDolphinUsesOriginalClipOffsets(int phase, float height, float bottom)
        {
            var zombie = new Zombie
            {
                Kind = ZombieKind.DolphinRider,
                Row = 2,
                PresentationNativePhase = phase,
                PresentationNativeHeight = 3,
                PresentationInPool = true,
                PresentationAltitude = 0f
            };
            Assert.That(AdventureZombieMotionRules.ClipHeight(zombie, 1f), Is.EqualTo(height));
            Assert.That(AdventureZombieMotionRules.ClipBottom(zombie, 220f, 1f), Is.EqualTo(bottom));
        }

        /// <summary>Checks delayed death, the fifty-update pull, its one-pixel descent, and the eighty-update splash.</summary>
        [Test]
        public void HundredUpdateGrabSinksAndRemovesOnlyAtNativeMilestones()
        {
            var game = Board();
            var kelp = Place(game, PlantKind.TangleKelp, 4);
            var target = Target(game, ZombieKind.Normal, 4.5f);
            game.Update(.01f);
            var state = kelp.TangleKelpPresentation!;
            var health = target.Health;
            for (var tick = 1; tick < 100; tick++)
            {
                game.Update(.01f);
                Assert.That(state.Countdown, Is.EqualTo(100 - tick));
                Assert.That(game.Plants, Has.Member(kelp));
                Assert.That(game.Zombies, Has.Member(target), "Kelp retains its victim until the full hundred-update grab ends.");
                Assert.That(target.Health, Is.EqualTo(health));
                Assert.That(target.PresentationNativeHeight, Is.EqualTo(tick < 50 ? 0 : 3));
                Assert.That(target.PoolState!.Altitude, Is.EqualTo(tick < 50 ? -40f : -40f - (tick - 49)));
                Assert.That(CountSound(game, 53), Is.EqualTo(tick < 80 ? 0 : 1));
                if (tick == 80)
                {
                    var splash = game.Presentation.ZombieSplashes.Single(effect => effect.ScaleX == 1.3f);
                    Assert.That(splash.ScaleY, Is.EqualTo(1.3f));
                    Assert.That(splash.X, Is.EqualTo(337f));
                    Assert.That(splash.Y, Is.EqualTo(257f));
                    Assert.That(splash.ParticleX, Is.EqualTo(391f));
                    Assert.That(splash.ParticleY, Is.EqualTo(314f));
                }
            }

            game.Update(.01f);
            Assert.That(state.Countdown, Is.Zero);
            Assert.That(game.Plants, Has.No.Member(kelp));
            Assert.That(game.Zombies, Has.No.Member(target));
            Assert.That(target.TangleKelpGrab, Is.Null);
            Assert.That(game.Presentation.Corpses, Is.Empty, "The original drowned victim has no ordinary death reanimation.");
        }

        /// <summary>Checks the kelp's authored final splash survives an earlier removal of its grabbed victim.</summary>
        [Test]
        public void RemovedVictimDoesNotCancelTheKelpSplashOrCountdown()
        {
            var game = Board();
            var kelp = Place(game, PlantKind.TangleKelp, 4);
            var target = Target(game, ZombieKind.Normal, 4.5f);
            game.Update(.01f);
            game.Zombies.Remove(target);
            for (var tick = 0; tick < 80; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(kelp.TangleKelpPresentation!.Countdown, Is.EqualTo(20));
            Assert.That(CountSound(game, 53), Is.EqualTo(1));
            Assert.That(game.Presentation.ZombieSplashes.Count(effect => effect.ScaleX == 1.3f), Is.EqualTo(1));
            for (var tick = 0; tick < 20; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(game.Plants, Has.No.Member(kelp));
            Assert.That(CountSound(game, 53), Is.EqualTo(1));
        }

        /// <summary>Checks grabbing blocks all new bite damage while retaining an existing meal until the pull starts.</summary>
        /// <param name="eating">Whether the victim was already eating when kelp caught it.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void GrabBlocksBitesAndPreservesMealUntilPull(bool eating)
        {
            var game = Board();
            var kelp = Place(game, PlantKind.TangleKelp, 4);
            Place(game, PlantKind.LilyPad, 3);
            var wallnut = Place(game, PlantKind.Wallnut, 3);
            var target = Target(game, ZombieKind.Normal, 4.5f);
            target.Eating = eating;
            var health = wallnut.Health;
            game.Update(.01f);
            Assert.That(kelp.TangleKelpPresentation, Is.Not.Null);
            for (var tick = 0; tick < 49; tick++)
            {
                Assert.That(target.Eating, Is.EqualTo(eating));
                Assert.That(wallnut.Health, Is.EqualTo(health), "A captured actor cannot start another bite.");
                game.Update(.01f);
            }

            Assert.That(kelp.TangleKelpPresentation!.Countdown, Is.EqualTo(51));
            Assert.That(target.Eating, Is.EqualTo(eating));
            game.Update(.01f);
            Assert.That(kelp.TangleKelpPresentation.Countdown, Is.EqualTo(50));
            Assert.That(target.Eating, Is.False);
            Assert.That(wallnut.Health, Is.EqualTo(health));
        }

        /// <summary>Checks two kelps cannot share a victim and shoveling the owner finishes its existing grab.</summary>
        [Test]
        public void GrabHasOneOwnerAndShovelFinishesItsVictim()
        {
            var game = Board();
            var first = Place(game, PlantKind.TangleKelp, 4);
            var second = Place(game, PlantKind.TangleKelp, 5);
            var entering = Target(game, ZombieKind.Normal, 5.05f);
            entering.PresentationInPool = false;
            entering.PoolState!.Altitude = 0f;
            var target = Target(game, ZombieKind.Normal, 5.1f);
            game.Update(.01f);

            Assert.That(first.TangleKelpPresentation, Is.Not.Null);
            Assert.That(first.TangleKelpPresentation!.TargetZombieId, Is.EqualTo(target.Id),
                "The nearer actor has not entered the pool and must not hide the eligible swimming victim.");
            Assert.That(entering.TangleKelpGrab, Is.Null);
            Assert.That(second.TangleKelpPresentation, Is.Null);
            Assert.That(target.TangleKelpGrab, Is.SameAs(first.TangleKelpPresentation));
            Assert.That(CountSound(game, 68), Is.EqualTo(1));
            Assert.That(game.TryShovelAdventurePlant(first), Is.True);
            Assert.That(game.Plants, Has.No.Member(first));
            Assert.That(game.Plants, Has.Member(second));
            Assert.That(game.Zombies, Has.No.Member(target));
            Assert.That(game.Zombies, Has.Member(entering));
            Assert.That(target.TangleKelpGrab, Is.Null);
        }

        /// <summary>Creates a quiet pool board without automatic spawning or resource dependencies.</summary>
        /// <returns>The production simulation in its playing phase.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 29 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Places a fixture plant through the existing production initializer.</summary>
        /// <param name="game">The board receiving the plant.</param>
        /// <param name="kind">The original plant kind.</param>
        /// <param name="column">The pool column to plant.</param>
        /// <returns>The plant created in pool row two.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind, int column)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", kind, 2, column)!;
        }

        /// <summary>Creates an already swimming actor through production zombie initialization.</summary>
        /// <param name="game">The board receiving the actor.</param>
        /// <param name="kind">The native swimming kind.</param>
        /// <param name="x">The logical target position inside the kelp's reach.</param>
        /// <returns>The live swimming actor.</returns>
        private static Zombie Target(GameSimulation game, ZombieKind kind, float x)
        {
            var target = (Zombie)Invoke(game, "SpawnAdventureZombie", kind, 2, x, 0)!;
            target.PresentationInPool = true;
            target.PresentationNativeHeight = 0;
            if (kind == ZombieKind.Normal)
            {
                target.PoolState = new AdventureZombiePoolState { HasDuckyTube = true, Altitude = -40f };
            }
            else
            {
                target.PresentationNativePhase = kind == ZombieKind.Snorkel ? 59 : 53;
                target.PresentationAltitude = kind == ZombieKind.Snorkel ? 10f : 0f;
                target.Submerged = kind == ZombieKind.Snorkel;
            }

            return target;
        }

        /// <summary>Counts one original Foley category in the retained presentation queue.</summary>
        /// <param name="game">The production simulation retaining Foley requests.</param>
        /// <param name="foley">The original category identifier.</param>
        /// <returns>The category's request count.</returns>
        private static int CountSound(GameSimulation game, int foley)
        {
            return game.Presentation.ZombieSounds.Count(sound => sound.Foley == foley);
        }

        /// <summary>Invokes an existing production initializer without exposing a test-only runtime entry point.</summary>
        /// <param name="game">The board whose initializer is invoked.</param>
        /// <param name="name">The existing nonpublic method name.</param>
        /// <param name="arguments">The initializer's arguments.</param>
        /// <returns>The initializer's return value.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
