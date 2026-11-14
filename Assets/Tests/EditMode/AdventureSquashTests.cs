#nullable enable

using System;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Game;
using PlantsVsZombies.Simulation;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the local PC Squash sequence, target redirection, narrow impact, and original damage flags.</summary>
    public sealed class AdventureSquashTests
    {
        /// <summary>Checks every original countdown, delayed damage, actual landing cue, and retained flattened body.</summary>
        [Test]
        public void ProductionUpdatesKeepTheCompleteOriginalAttackAndLandingSequence()
        {
            var game = Fixture();
            var plant = Place(game);
            var zombie = Target(game, ZombieKind.Normal, 390);
            game.Presentation.ZombieSounds.Clear();
            game.Update(.01f);
            var state = plant.SquashPresentation!;
            Assert.That(state.NativePhase, Is.EqualTo(3));
            Assert.That(state.Animation, Is.EqualTo("anim_lookright"));
            Assert.That(state.Countdown, Is.EqualTo(80));
            Assert.That(SoundCount(game, 41), Is.EqualTo(1));
            Assert.That(game.Zombies, Has.Member(zombie));
            game.Update(.8f);
            Assert.That(state.NativePhase, Is.EqualTo(4));
            Assert.That(state.Countdown, Is.EqualTo(45));
            Assert.That(state.Animation, Is.EqualTo("anim_jumpup"));
            game.Update(.45f);
            Assert.That(state.NativePhase, Is.EqualTo(5));
            Assert.That(state.Countdown, Is.EqualTo(50));
            Assert.That(plant.PresentationBiteNotOnGround, Is.True);
            Assert.That(game.AdventurePlantAt(1, 4, PlantLayer.Main), Is.Null);
            Assert.That((AdventurePlantingFailure)Invoke(game, "CheckAdventurePlanting", PlantKind.Peashooter, 1, 4, true)!,
                Is.EqualTo(AdventurePlantingFailure.None), "The original ground cell becomes available while Squash is airborne.");
            Assert.That(game.TryShovelAdventurePlant(plant), Is.False);
            var host = new GameObject("Inactive airborne Squash shovel fixture");
            host.SetActive(false);
            try
            {
                var owner = host.AddComponent<PvZGame>();
                owner._game = game;
                Assert.That(new BoardLayout(owner).AdventureShovelTarget(new Vector2(400f, 220f)), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }

            game.Update(.3f);
            Assert.That(state.NativeX, Is.EqualTo(state.TargetX));
            Assert.That(state.NativeY, Is.EqualTo(68));
            game.Update(.2f);
            Assert.That(state.NativePhase, Is.EqualTo(6));
            Assert.That(state.Countdown, Is.EqualTo(10));
            Assert.That(state.Animation, Is.EqualTo("anim_jumpdown"));
            Assert.That(state.BlendTicks, Is.Zero);
            game.Update(.04f);
            Assert.That(game.Zombies, Has.Member(zombie));
            game.Update(.01f);
            Assert.That(state.Countdown, Is.EqualTo(5));
            Assert.That(game.Zombies, Has.No.Member(zombie));
            Assert.That(game.Presentation.Corpses, Is.Empty);
            Assert.That(game.Presentation.ZombiePartDrops, Is.Empty);
            Assert.That(game.Presentation.ArmDrops, Is.Empty);
            Assert.That(SoundCount(game, 40), Is.Zero);
            game.Update(.05f);
            Assert.That(state.NativePhase, Is.EqualTo(7));
            Assert.That(state.Countdown, Is.EqualTo(100));
            Assert.That(SoundCount(game, 40), Is.EqualTo(1));
            Assert.That(SoundCount(game, 55), Is.Zero);
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.EqualTo(12));
            Assert.That(game.Presentation._squashLandings.Count, Is.EqualTo(1));
            game.Update(.99f);
            Assert.That(game.Plants, Has.Member(plant));
            game.Update(.01f);
            Assert.That(game.Plants, Has.No.Member(plant));
        }

        /// <summary>Checks the look direction and the original thirty-update prediction re-evaluate a vanished target before flight.</summary>
        [Test]
        public void SquashLooksLeftAndRedirectsOnlyAtLaunchWhenItsOriginalTargetDisappears()
        {
            var game = Fixture();
            var plant = Place(game);
            var first = Target(game, ZombieKind.Normal, 305);
            game.Update(.01f);
            var state = plant.SquashPresentation!;
            Assert.That(state.Animation, Is.EqualTo("anim_lookleft"));
            var firstX = state.TargetX;
            game.Zombies.Remove(first);
            var replacement = Target(game, ZombieKind.Normal, 430);
            game.Update(.8f);
            Assert.That(state.TargetX, Is.EqualTo(firstX));
            game.Update(.45f);
            var body = AdventureZombieTargetingRules.BodyRectangle(replacement);
            Assert.That(state.TargetX, Is.EqualTo(body.X + body.Width / 2 - 40));
            Assert.That(state.TargetX, Is.GreaterThan(firstX));
        }

        /// <summary>Checks original eighteen-flag damage pierces held shields, suppresses ordinary corpses, and preserves giant survival.</summary>
        [Test]
        public void ImpactUsesOriginalShieldAndCorpseRulesInsteadOfAnExplosion()
        {
            var game = Fixture();
            var plant = Place(game);
            var door = Target(game, ZombieKind.ScreenDoor, 360);
            door.ShieldHealth = 1100f;
            var bucket = Target(game, ZombieKind.Buckethead, 360);
            bucket.ArmorHealth = 1100f;
            var headless = Target(game, ZombieKind.Normal, 360);
            headless.Headless = true;
            var giant = Target(game, ZombieKind.Gargantuar, 360);
            giant.Health = giant.StartingHealth = 3000f;
            var flying = Target(game, ZombieKind.Balloon, 360);
            flying.Airborne = true;
            var friendly = Target(game, ZombieKind.Normal, 360);
            friendly.Hypnotized = true;
            var state = new AdventureSquashState { NativeX = 360 };
            Invoke(game, "DamageAdventureSquashTargets", plant, state);
            Assert.That(game.Zombies, Has.No.Member(door));
            Assert.That(game.Zombies, Has.No.Member(bucket));
            Assert.That(game.Zombies, Has.No.Member(headless));
            Assert.That(door.ShieldHealth, Is.Zero);
            Assert.That(bucket.ArmorHealth, Is.Zero);
            Assert.That(giant.Health, Is.EqualTo(1200f));
            Assert.That(flying.Health, Is.EqualTo(270f));
            Assert.That(friendly.Health, Is.EqualTo(270f));
            Assert.That(game.Presentation.Corpses, Is.Empty);
            Assert.That(game.Presentation.ZombiePartDrops, Is.Empty);
            Assert.That(game.Presentation.Explosions, Is.Empty);
            Invoke(game, "DamageAdventureSquashTargets", plant, state);
            Assert.That(game.Presentation.Corpses.Count, Is.EqualTo(1));
            Assert.That(game.Presentation.Corpses[0].Zombie, Is.SameAs(giant), "PC keeps the Gargantuar's own lethal death animation.");
            Assert.That(game.Presentation.Corpses[0].Burn, Is.Null);
        }

        /// <summary>Checks the original two-stage ease-in-out curve reaches its destination before the twenty-tick hover.</summary>
        [Test]
        public void FlightUsesTheOriginalIntegerCurveAndHover()
        {
            Assert.That(AdventureSquashRules.RisingPosition(50, 100, 200), Is.EqualTo(100));
            Assert.That(AdventureSquashRules.RisingPosition(35, 100, 200), Is.EqualTo(150));
            Assert.That(AdventureSquashRules.RisingPosition(20, 100, 200), Is.EqualTo(200));
            Assert.That(AdventureSquashRules.RisingPosition(0, 100, 200), Is.EqualTo(200));
        }

        /// <summary>Checks ZombieTargetLeadX treats launching and reloading catapults as stationary.</summary>
        /// <param name="phase">The original stopped catapult native phase.</param>
        [TestCase(67)]
        [TestCase(68)]
        public void StoppedCatapultUsesItsOriginalBodyCenterWithoutWalkingLead(int phase)
        {
            var game = Fixture();
            var zombie = Spawn(game, ZombieKind.Catapult, 1, 450f);
            zombie.PresentationNativePhase = phase;
            Assert.That(zombie.PresentationNativeVelocity, Is.GreaterThan(0f), "A stopped phase retains the original walking velocity.");
            // PC vehicle body is [mX,mX+153]; integer center 450+76, minus half the plant width forty.
            Assert.That(Invoke(game, "AdventureSquashTargetX", zombie, 30), Is.EqualTo(486));
        }

        /// <summary>Checks PC 54E240 excludes a live zombie already owned by a Tangle Kelp from Squash acquisition.</summary>
        [Test]
        public void KelpOwnedZombieCannotTriggerTheSquashLook()
        {
            var game = Fixture(29);
            Invoke(game, "PlaceAdventurePlant", PlantKind.LilyPad, 2, 4);
            var squash = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Squash, 2, 4)!;
            var zombie = Spawn(game, ZombieKind.Normal, 2, 390f);
            zombie.FrozenRemaining = 100f;
            zombie.TangleKelpGrab = new AdventureTangleKelpState { TargetZombieId = zombie.Id };
            game.Update(.01f);
            Assert.That(squash.SquashPresentation!.NativePhase, Is.Zero);
            Assert.That(game.Presentation.ZombieSounds.Exists(sound => sound.Foley == 41), Is.False);
        }

        /// <summary>Checks the PC target scan allows multiple Squashes to select the same live zombie.</summary>
        [Test]
        public void PcOriginalDoesNotReserveTargetsBetweenDifferentSquashes()
        {
            var game = Fixture();
            var first = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Squash, 1, 4)!;
            var second = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Squash, 1, 5)!;
            var zombie = Spawn(game, ZombieKind.Normal, 1, 440f);
            zombie.FrozenRemaining = 100f;
            game.Update(.01f);
            Assert.That(first.SquashPresentation!.NativePhase, Is.EqualTo(3));
            Assert.That(second.SquashPresentation!.NativePhase, Is.EqualTo(3));
            Assert.That(first.SquashPresentation.TargetZombieId, Is.EqualTo(zombie.Id));
            Assert.That(second.SquashPresentation.TargetZombieId, Is.EqualTo(zombie.Id));
        }

        /// <summary>Creates a playing original board with no automatic waves during the Squash action.</summary>
        /// <param name="level">The original background; existing cases retain the default night lawn.</param>
        /// <returns>The isolated production simulation.</returns>
        private static GameSimulation Fixture(int level = 19)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 4231);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Plants.Clear();
            game.Zombies.Clear();
            game.Adventure._graves.Clear();
            return game;
        }

        /// <summary>Creates the Squash through production placement without overriding its original trigger timing.</summary>
        /// <param name="game">The isolated board receiving the plant.</param>
        /// <returns>The planted original contact attacker.</returns>
        private static Plant Place(GameSimulation game)
        {
            return (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Squash, 1, 4)!;
        }

        /// <summary>Creates a stationary native target whose freeze isolates plant state timing.</summary>
        /// <param name="game">The isolated simulation receiving the target.</param>
        /// <param name="kind">The original zombie species.</param>
        /// <param name="nativeX">The unscaled native zombie origin.</param>
        /// <returns>The live target stored by the simulation.</returns>
        private static Zombie Target(GameSimulation game, ZombieKind kind, int nativeX)
        {
            var zombie = new Zombie
            {
                Id = game.Zombies.Count + 1000, Kind = kind, Row = 1,
                X = AdventureZombieNativeRules.GridX(nativeX), Health = 270f, StartingHealth = 270f,
                FrozenRemaining = 100f
            };
            game.Zombies.Add(zombie);
            return zombie;
        }

        /// <summary>Spawns a real native actor at an exact original pixel coordinate.</summary>
        /// <param name="game">The controlled playing board.</param>
        /// <param name="kind">The original zombie identifier.</param>
        /// <param name="row">The zero-based lane.</param>
        /// <param name="x">The native horizontal position.</param>
        /// <returns>The initialized live actor.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind, int row, float x)
        {
            var zombie = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", kind, row, AdventureZombieNativeRules.GridX(x), 0)!;
            zombie.X = AdventureZombieNativeRules.GridX(x);
            zombie.PresentationPreviousX = zombie.X;
            return zombie;
        }

        /// <summary>Counts native Foley requests retained for the fixture's plant action.</summary>
        /// <param name="game">The simulation supplying the sound queue.</param>
        /// <param name="foley">The original sound category.</param>
        /// <returns>The number of matching original requests.</returns>
        private static int SoundCount(GameSimulation game, int foley)
        {
            return game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == foley).Count;
        }

        /// <summary>Invokes production actions without widening their runtime visibility.</summary>
        /// <param name="game">The production simulation owning the action.</param>
        /// <param name="name">The nonpublic method name.</param>
        /// <param name="arguments">The original action arguments.</param>
        /// <returns>The method's optional result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
