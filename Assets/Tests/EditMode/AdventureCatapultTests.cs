#nullable enable

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original PC catapult state machine, actual hostile lob and authored vehicle deaths.</summary>
    public sealed class AdventureCatapultTests
    {
        /// <summary>Checks native X650, initial ammunition and the original complete shoot/reload cycle.</summary>
        [Test]
        public void NativeStopFireAndReloadUseTheOriginalSeparateClocks()
        {
            var game = Board();
            var target = Place(game, PlantKind.Wallnut, 0);
            var zombie = Spawn(game, 651f);
            Assert.That(zombie.CatapultState!.Ammunition, Is.EqualTo(20));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(5.5f));
            Tick(game, 1);
            Assert.That(zombie.PresentationNativePhase, Is.Zero);
            zombie.X = AdventureZombieNativeRules.GridX(650f);
            Tick(game, 1);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(67));
            Assert.That(zombie.CatapultState.PhaseTicks, Is.EqualTo(300));
            Assert.That(zombie.PresentationBodyBlendTicks, Is.Zero);
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_shoot"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(24f));
            var x = zombie.X;
            Tick(game, 72);
            Assert.That(Sounds(game, 37), Is.Zero);
            Tick(game, 1);
            Assert.That(Sounds(game, 37), Is.EqualTo(1));
            Assert.That(game.Peas.Single().Basketball, Is.True);
            Assert.That(target.Health, Is.EqualTo(4000f), "Launching cannot immediately damage a target.");
            Assert.That(zombie.CatapultState.Ammunition, Is.EqualTo(20));
            Tick(game, 61);
            Assert.That(zombie.CatapultState.Ammunition, Is.EqualTo(19));
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(68));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_idle"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(12f));
            Assert.That(zombie.PresentationBodyBlendTicks, Is.EqualTo(20));
            Tick(game, 165);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(68));
            Tick(game, 1);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(67));
            Assert.That(zombie.X, Is.EqualTo(x));
            Assert.That(Sounds(game, 37), Is.EqualTo(1));
        }

        /// <summary>Checks native vehicle squashing continues during launch without starting an eating clip.</summary>
        [Test]
        public void CrushingAPlantDuringLaunchDoesNotResetTheShootingAnimation()
        {
            var game = Board();
            Place(game, PlantKind.Wallnut, 0);
            var zombie = Spawn(game, 650f);
            Tick(game, 1);
            Assert.That(zombie.PresentationNativePhase, Is.EqualTo(67));
            var crushed = Place(game, PlantKind.Sunflower, 8);
            Tick(game, 1);
            Assert.That(game.Plants, Has.No.Member(crushed));
            Assert.That(zombie.Eating, Is.False);
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_shoot"));
            var age = zombie.PresentationAnimationAge;
            Tick(game, 5);
            Assert.That(zombie.PresentationAnimationAge, Is.GreaterThan(age));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_shoot"));
        }

        /// <summary>Checks all twenty actual shots complete before the vehicle resumes its six-FPS walk.</summary>
        /// <param name="level">The ground or roof background used by the projectile floor.</param>
        [TestCase(9)]
        [TestCase(49)]
        public void TwentyBasketballsDealSeventyFiveDamageEachBeforeWalking(int level)
        {
            var game = Board(level);
            if (level == 49)
            {
                Place(game, PlantKind.FlowerPot, 0);
            }

            var target = Place(game, PlantKind.Wallnut, 0);
            target.Health = 100000f;
            var zombie = Spawn(game, 650f);
            for (var tick = 0; tick < 6500 && zombie.CatapultState!.Ammunition > 0; tick++)
            {
                Tick(game, 1);
            }

            Assert.That(zombie.CatapultState!.Ammunition, Is.Zero);
            Assert.That(Sounds(game, 37), Is.EqualTo(20));
            Assert.That(zombie.PresentationNativePhase, Is.Zero);
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(6f));
            var x = zombie.X;
            Tick(game, 160);
            Assert.That(zombie.X, Is.LessThan(x));
            Assert.That(Sounds(game, 37), Is.EqualTo(20));
            Assert.That(target.Health, Is.EqualTo(98500f));
            Assert.That(game.Peas, Is.Empty);
        }

        /// <summary>Checks leftmost selection, one-hundred-pixel minimum separation and original aerial layer priority.</summary>
        [Test]
        public void TargetingRejectsSpikesLiftedBodiesAndClosePlants()
        {
            var game = Board();
            Place(game, PlantKind.Spikerock, 0);
            var lifted = Place(game, PlantKind.Peashooter, 1);
            lifted.BungeeState = 2;
            var target = Place(game, PlantKind.Sunflower, 2);
            var shell = Place(game, PlantKind.Pumpkin, 2);
            Place(game, PlantKind.Wallnut, 4);
            var zombie = Spawn(game, 650f);
            Assert.That(Invoke(game, "FindAdventureCatapultTarget", zombie), Is.SameAs(target));
            target.BungeeState = 2;
            Assert.That(Invoke(game, "FindAdventureCatapultTarget", zombie), Is.SameAs(shell));
            game.Plants.Clear();
            Place(game, PlantKind.Wallnut, 7);
            Tick(game, 1);
            Assert.That(zombie.PresentationNativePhase, Is.Zero);
            Assert.That(Sounds(game, 37), Is.Zero);
        }

        /// <summary>Checks a disappearing aim produces the original fallback trajectory and a finite ground impact.</summary>
        [Test]
        public void MissingLaunchTargetUsesTheOriginalFallbackAndStillLands()
        {
            var game = Board();
            var zombie = Spawn(game, 650f);
            Invoke(game, "FireAdventureBasketball", zombie, null!);
            var shot = game.Peas.Single();
            Assert.That(shot.PresentationLobNativeX, Is.EqualTo(763f));
            Assert.That(shot.PresentationLobNativeY, Is.EqualTo(106f));
            Assert.That(shot.PresentationLobNativeVelocityX, Is.EqualTo(-393f / 120f));
            Assert.That(shot.PresentationLobVelocity, Is.EqualTo(-7f - 106f / 120f));
            Tick(game, 200);
            Assert.That(game.Peas, Is.Empty);
            Assert.That(Sounds(game, 37), Is.EqualTo(1));
            Assert.That(Sounds(game, 1), Is.EqualTo(1));
        }

        /// <summary>Checks native float32 vertical integration and roof rebasing against an independent numerical oracle.</summary>
        [Test]
        public void BasketballFlightKeepsTheOriginalNativeIntegrationOrderOnRoof()
        {
            var game = Board(49);
            var target = Place(game, PlantKind.Wallnut, 0);
            var zombie = Spawn(game, 650f);
            Invoke(game, "FireAdventureBasketball", zombie, target);
            var shot = game.Peas.Single();
            var x = 763f;
            var y = 81f;
            var z = 0f;
            var velocity = -7f + (255f - 81f) / 120f;
            for (var tick = 0; tick < 100; tick++)
            {
                velocity += .115f;
                var next = x - 703f / 120f;
                z += velocity;
                var floorChange = (Math.Max(0f, 440f - next) - Math.Max(0f, 440f - x)) * .25f;
                y += floorChange;
                z -= floorChange;
                x = next;
                Assert.That(Invoke(game, "AdvanceAdventureBasketball", shot, false), Is.False);
                Assert.That(shot.PresentationLobNativeX, Is.EqualTo(x));
                Assert.That(shot.PresentationLobNativeY, Is.EqualTo(y));
                Assert.That(shot.PresentationLobNativeZ, Is.EqualTo(z));
            }
        }

        /// <summary>Checks the original extra forty-pixel pool contact height before resolving hostile lob collision.</summary>
        /// <param name="level">The original dry or six-row pool background.</param>
        /// <param name="hits">Whether native height sixty-one is already low enough to hit a plant.</param>
        [TestCase(9, true)]
        [TestCase(29, false)]
        public void BasketballContactAddsTheOriginalPoolHeight(int level, bool hits)
        {
            var game = Board(level);
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.LilyPad, 2, 0)!;
            var shot = new Pea { Basketball = true, Row = 2, Damage = 75f, PresentationLobNativeX = 60f,
                PresentationLobNativeY = 200f, PresentationLobNativeZ = 61f, PresentationLobVelocity = 0f };
            Assert.That(Invoke(game, "AdvanceAdventureBasketball", shot, true), Is.EqualTo(hits));
            Assert.That(plant.Health, Is.EqualTo(hits ? 225f : 300f));
        }

        /// <summary>Checks the umbrella's five-update trigger holds the live ball before original top-layer reflection.</summary>
        [Test]
        public void UmbrellaTriggersThenReflectsWithoutDamagingTheProtectedPlant()
        {
            var game = Board();
            var target = Place(game, PlantKind.Wallnut, 0);
            var umbrella = Place(game, PlantKind.UmbrellaLeaf, 1);
            var shot = new Pea { Basketball = true, Row = 1, Damage = 75f, PresentationLobNativeX = 60f,
                PresentationLobNativeY = 100f, PresentationLobNativeZ = 61f, PresentationLobVelocity = 0f };
            Assert.That(Invoke(game, "AdvanceAdventureBasketball", shot, true), Is.False);
            Assert.That(umbrella.PresentationUmbrellaAge, Is.Zero);
            Assert.That(Sounds(game, 73), Is.EqualTo(1));
            umbrella.PresentationUmbrellaAge = .04f;
            Assert.That(Invoke(game, "AdvanceAdventureBasketball", shot, true), Is.False);
            umbrella.PresentationUmbrellaAge = .05f;
            Assert.That(Invoke(game, "AdvanceAdventureBasketball", shot, true), Is.True);
            Assert.That(target.Health, Is.EqualTo(4000f));
            Assert.That(Sounds(game, 1), Is.EqualTo(1));
            Assert.That(game.Presentation.ZomboniParticles.Single().Particle, Is.EqualTo("UmbrellaReflect"));
            Assert.That(game.Presentation.ZomboniParticles.Single().Foreground, Is.True);
        }

        /// <summary>Checks cold immobilization freezes the timer and shooting pose together, while chill preserves timer speed.</summary>
        /// <param name="butter">Whether the independent butter counter holds the body instead of ice.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void ImmobilizationHoldsTheLaunchAndReloadClock(bool butter)
        {
            var game = Board();
            Place(game, PlantKind.Wallnut, 0);
            var zombie = Spawn(game, 650f);
            Tick(game, 1);
            zombie.FrozenRemaining = butter ? 0f : 2f;
            zombie.PresentationButter = butter ? 2f : 0f;
            Tick(game, 30);
            Assert.That(zombie.CatapultState!.PhaseTicks, Is.EqualTo(300));
            Assert.That(zombie.PresentationAnimationAge, Is.Zero);
            zombie.FrozenRemaining = 0f;
            zombie.PresentationButter = 0f;
            zombie.ChillRemaining = 5f;
            Tick(game, 50);
            Assert.That(zombie.CatapultState.PhaseTicks, Is.EqualTo(250));
            Assert.That(Sounds(game, 37), Is.Zero);
        }

        /// <summary>Checks the original basket balls disappear only after the corresponding shot clip completes.</summary>
        [Test]
        public void BasketTracksAndFinalEmptyPoleFollowTheOriginalAmmunitionCounter()
        {
            var game = Board();
            var zombie = Spawn(game, 650f);
            var state = zombie.CatapultState!;
            for (var remaining = 5; remaining > 0; remaining--)
            {
                state.Ammunition = remaining;
                state.Fired = true;
                zombie.PresentationNativePhase = 67;
                zombie.PresentationAnimationAge = 32f / 24f;
                Invoke(game, "AdvanceAdventureCatapult", zombie);
                Assert.That(state.Ammunition, Is.EqualTo(remaining - 1));
                Assert.That(state.TrackVisible("Zombie_catapult_basketball"), Is.False);
                Assert.That(state.TrackVisible("Zombie_catapult_basketball2"), Is.EqualTo(remaining > 4));
                Assert.That(state.TrackVisible("Zombie_catapult_basketball3"), Is.EqualTo(remaining > 3));
                Assert.That(state.TrackVisible("Zombie_catapult_basketball4"), Is.EqualTo(remaining > 2));
            }

            Assert.That(state.Image("Zombie_catapult_pole", "Zombie_catapult_pole_withball.png", 0),
                Is.EqualTo("IMAGE_REANIM_ZOMBIE_CATAPULT_POLE"));
        }

        /// <summary>Checks native puncture keeps the bounce for 280 updates then emits one real vehicle explosion.</summary>
        [Test]
        public void SpikeDamageStartsTheOriginalBounceSmokeAndDelayedExplosion()
        {
            var game = Board();
            var zombie = Spawn(game, 650f);
            Invoke(game, "HitAdventureZombie", zombie, 20f, true, 32u);
            Assert.That(zombie.CatapultState!.FlatTires, Is.True);
            Assert.That(zombie.CatapultState.DeathTicks, Is.EqualTo(280));
            Assert.That(game.AdventurePresentationZombieAnimation(zombie), Is.EqualTo("anim_bounce"));
            Assert.That(game.AdventurePresentationZombieRate(zombie), Is.EqualTo(12f));
            Assert.That(zombie.PresentationBodyBlendTicks, Is.EqualTo(10));
            Assert.That(zombie.CatapultState.SmokeStartAge, Is.GreaterThanOrEqualTo(0f));
            Assert.That(zombie.Speed, Is.Zero);
            Assert.That(Sounds(game, 22), Is.EqualTo(1));
            Assert.That(game.Presentation.ZomboniParticles.Single().X, Is.EqualTo(679f));
            Tick(game, 279);
            Assert.That(game.Zombies, Has.Member(zombie));
            Tick(game, 1);
            Assert.That(game.Zombies, Has.No.Member(zombie));
            Assert.That(game.Presentation.Corpses, Is.Empty);
            Assert.That(Sounds(game, 23), Is.EqualTo(1));
            Assert.That(game.Presentation.ZomboniParticles.Count(particle => particle.Particle == "CatapultExplosion"), Is.EqualTo(1));
        }

        /// <summary>Checks puncture preserves native ice or butter while its death timer continues independently.</summary>
        /// <param name="butter">Whether the retained immobilization is butter instead of ice.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void PuncturePreservesAndExpiresExistingColdAndButterCounters(bool butter)
        {
            var game = Board();
            var zombie = Spawn(game, 650f);
            zombie.FrozenRemaining = butter ? 0f : .2f;
            zombie.PresentationButter = butter ? .2f : 0f;
            zombie.ChillRemaining = .1f;
            Invoke(game, "HitAdventureZombie", zombie, 20f, true, 32u);
            Assert.That(butter ? zombie.PresentationButter : zombie.FrozenRemaining, Is.EqualTo(.2f));
            Tick(game, 10);
            Assert.That(zombie.PresentationAnimationAge, Is.Zero);
            Assert.That(zombie.CatapultState!.DeathTicks, Is.EqualTo(270));
            Assert.That(zombie.ChillRemaining, Is.EqualTo(0f).Within(.00001f));
            Tick(game, 30);
            Assert.That(zombie.PresentationAnimationAge, Is.GreaterThan(0f));
            Assert.That(zombie.CatapultState.DeathTicks, Is.EqualTo(240));
        }

        /// <summary>Checks real spike actions enter retained puncture death instead of deleting the vehicle immediately.</summary>
        /// <param name="kind">The original single-use weed or reusable rock.</param>
        [TestCase(PlantKind.Spikeweed)]
        [TestCase(PlantKind.Spikerock)]
        public void RealSpikeContactRetainsThePuncturedCatapultAndConsumesOnePlantHit(PlantKind kind)
        {
            var game = Board();
            var spike = Place(game, kind, 4);
            spike.ActionTimer = 0f;
            var zombie = Spawn(game, 328f);
            Tick(game, 1);
            Assert.That(game.Zombies, Has.Member(zombie));
            Assert.That(zombie.CatapultState!.FlatTires, Is.True);
            Assert.That(zombie.CatapultState.DeathTicks, Is.InRange(279, 280));
            if (kind == PlantKind.Spikeweed)
            {
                Assert.That(game.Plants, Has.No.Member(spike));
            }
            else
            {
                Assert.That(game.Plants, Has.Member(spike));
                Assert.That(spike.VehicleHitsTaken, Is.EqualTo(1));
                Tick(game, 200);
                Assert.That(spike.VehicleHitsTaken, Is.EqualTo(1), "A dying vehicle cannot consume another rock hit.");
            }
        }

        /// <summary>Checks an ash burn keeps the original charred catapult rather than the normal explosion.</summary>
        [Test]
        public void AshUsesTheExistingOriginalCharredVehicleDefinition()
        {
            var game = Board();
            var zombie = Spawn(game, 650f);
            Invoke(game, "BurnAdventureRow", 1);
            Assert.That(game.Zombies, Has.No.Member(zombie));
            Assert.That(game.Presentation.ZomboniParticles, Is.Empty);
            Assert.That(game.Presentation.Corpses.Count, Is.EqualTo(1));
            Assert.That(game.Presentation.Corpses[0].Burn, Is.Not.Null);
        }

        /// <summary>Checks normal lethal body damage explodes immediately rather than creating a walking corpse.</summary>
        [Test]
        public void LethalDamageExplodesWithoutThePunctureDelay()
        {
            var game = Board();
            var zombie = Spawn(game, 650f);
            Invoke(game, "HitAdventureZombie", zombie, 850f, true, 0u);
            Assert.That(game.Zombies, Is.Empty);
            Assert.That(game.Presentation.Corpses, Is.Empty);
            Assert.That(Sounds(game, 22), Is.Zero);
            Assert.That(Sounds(game, 23), Is.EqualTo(1));
            Assert.That(game.Presentation.ZomboniParticles.Single().Particle, Is.EqualTo("CatapultExplosion"));
        }

        /// <summary>Creates an isolated playing board without scheduled targets or background objects.</summary>
        /// <param name="level">The original ground or roof level.</param>
        /// <returns>The initialized production board.</returns>
        private static GameSimulation Board(int level = 9)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Plants.Clear();
            game.Zombies.Clear();
            return game;
        }

        /// <summary>Places an original plant in row one of a controlled native column.</summary>
        /// <param name="game">The initialized board.</param>
        /// <param name="kind">The original plant identifier.</param>
        /// <param name="column">The zero-based board column.</param>
        /// <returns>The live production plant.</returns>
        private static Plant Place(GameSimulation game, PlantKind kind, int column)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", kind, 1, column)!;
        }

        /// <summary>Spawns the original vehicle at an exact native pixel coordinate.</summary>
        /// <param name="game">The initialized board.</param>
        /// <param name="x">The original vehicle X coordinate.</param>
        /// <returns>The live initialized catapult.</returns>
        private static Zombie Spawn(GameSimulation game, float x)
        {
            var zombie = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Catapult, 1,
                AdventureZombieNativeRules.GridX(x), 0)!;
            zombie.X = AdventureZombieNativeRules.GridX(x);
            zombie.PresentationPreviousX = zombie.X;
            return zombie;
        }

        /// <summary>Advances complete production updates at the original hundred-hertz rate.</summary>
        /// <param name="game">The board being advanced.</param>
        /// <param name="ticks">The exact number of native updates.</param>
        private static void Tick(GameSimulation game, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
            {
                game.Update(.01f);
            }
        }

        /// <summary>Counts native Foley events generated by the production action.</summary>
        /// <param name="game">The board recording the events.</param>
        /// <param name="foley">The original Foley identifier.</param>
        /// <returns>The number of matching events.</returns>
        private static int Sounds(GameSimulation game, int foley)
        {
            return game.Presentation.ZombieSounds.Count(sound => sound.Foley == foley);
        }

        /// <summary>Invokes a retained production action for a controlled native state boundary.</summary>
        /// <param name="game">The initialized board.</param>
        /// <param name="method">The original production method name.</param>
        /// <param name="arguments">The unchanged action parameters.</param>
        /// <returns>The original action result.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
