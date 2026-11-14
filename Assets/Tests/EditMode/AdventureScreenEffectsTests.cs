#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks native screen feedback at the production explosion and fixed-tick boundaries.</summary>
    public sealed class AdventureScreenEffectsTests
    {
        /// <summary>Checks the PC bounce envelope and the independent horizontal sign choice.</summary>
        [Test]
        public void ShakeUsesOriginalTwelveTickBounceAndHorizontalSign()
        {
            // PC Board::Update 0x42134B..4213BC and TodAnimateCurve 0x526F20, curve 9.
            var shake = new AdventureScreenShakeState();
            shake.Start(3, -4);
            Assert.That(shake.OffsetX, Is.Zero);
            Assert.That(shake.OffsetY, Is.Zero);
            // Samples were checked against the local PC 0x526F20 machine code.
            // Native float storage makes the final half-pixel horizontal sample round to zero.
            var horizontal = new[] { 1, 1, 2, 2, 3, 3, 3, 2, 2, 1, 0, 0 };
            var vertical = new[] { -1, -1, -2, -3, -3, -4, -3, -3, -2, -1, -1, 0 };
            for (var tick = 0; tick < horizontal.Length; tick++)
            {
                shake.Tick(false);
                Assert.That(shake.TicksRemaining, Is.EqualTo(11 - tick));
                Assert.That(shake.OffsetX, Is.EqualTo(horizontal[tick]), "Horizontal sample at native tick " + (tick + 1));
                Assert.That(shake.OffsetY, Is.EqualTo(vertical[tick]), "Vertical sample at native tick " + (tick + 1));
            }

            shake.Tick(true);
            Assert.That(shake.OffsetX, Is.Zero);
            Assert.That(shake.OffsetY, Is.Zero);
            shake.Start(3, -4);
            shake.Tick(true);
            Assert.That(shake.OffsetX, Is.EqualTo(-1));
            Assert.That(shake.OffsetY, Is.EqualTo(-1));
            shake.Tick(true);
            Assert.That(shake.OffsetX, Is.EqualTo(1));
            Assert.That(shake.OffsetY, Is.EqualTo(-1), "The PC random draw reverses only the horizontal amount.");
        }

        /// <summary>Checks a second explosion resets the native duration without moving the board immediately.</summary>
        [Test]
        public void RepeatedShakeRestartsTheCounterAndRetainsCurrentOffsets()
        {
            var shake = new AdventureScreenShakeState();
            shake.Start(3, -4);
            for (var tick = 0; tick < 6; tick++)
            {
                shake.Tick(false);
            }

            Assert.That(shake.OffsetX, Is.EqualTo(3));
            Assert.That(shake.OffsetY, Is.EqualTo(-4));
            shake.Start(3, -4);
            Assert.That(shake.TicksRemaining, Is.EqualTo(12));
            Assert.That(shake.OffsetX, Is.EqualTo(3));
            Assert.That(shake.OffsetY, Is.EqualTo(-4));
            shake.Tick(false);
            Assert.That(shake.OffsetX, Is.EqualTo(1));
            Assert.That(shake.OffsetY, Is.EqualTo(-1));
        }

        /// <summary>Checks the real ash and mine plant paths start screen shake on detonation.</summary>
        /// <param name="kind">The plant whose production special action is exercised.</param>
        [TestCase(PlantKind.CherryBomb)]
        [TestCase(PlantKind.Jalapeno)]
        [TestCase(PlantKind.DoomShroom)]
        [TestCase(PlantKind.PotatoMine)]
        public void ExplosivePlantsStartShakeOnlyWhenTheyDetonate(PlantKind kind)
        {
            var game = Fixture();
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", kind, 1, 4)!;
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.Zero);
            Assert.That(game.Presentation.Explosions, Is.Empty);
            if (kind == PlantKind.CherryBomb)
            {
                plant.RequiredCherryBombPresentation.Countdown = 1;
            }
            else if (kind == PlantKind.Jalapeno)
            {
                plant.JalapenoPresentation!.Countdown = 1;
            }
            else if (kind == PlantKind.PotatoMine)
            {
                plant.MinePhase = MinePhase.Armed;
                game.Zombies.Add(new Zombie
                {
                    Id = 1000,
                    Kind = ZombieKind.Normal,
                    Row = 1,
                    X = AdventureZombieNativeRules.GridX(360f - 36f),
                    Health = 270f,
                    StartingHealth = 270f
                });
            }
            else if (kind == PlantKind.DoomShroom)
            {
                plant.DoomShroomPresentation = new AdventureDoomShroomState { Countdown = 1 };
            }
            else
            {
                plant.ActionTimer = .01f;
            }

            game.Update(.01f);
            Assert.That(game.Plants.Contains(plant), Is.False);
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.EqualTo(12));
            Assert.That(game.Presentation.ScreenShake.OffsetX, Is.Zero, "The triggering tick stores the request before the following board update moves it.");
            Assert.That(game.Presentation.ScreenShake.OffsetY, Is.Zero);
            game.Update(.06f);
            Assert.That(Math.Abs(game.Presentation.ScreenShake.OffsetX), Is.EqualTo(3));
            Assert.That(game.Presentation.ScreenShake.OffsetY, Is.EqualTo(-4));
        }

        /// <summary>Checks the original Doom fuse starts only awake, animates for one hundred ticks, and creates one crater with its own sample.</summary>
        [Test]
        public void DoomShroomStartsItsOriginalFuseWhenWokenAndDetonatesOnce()
        {
            var game = Fixture(9);
            var events = new List<GameEvent>();
            game.EventRaised += events.Add;
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.DoomShroom, 1, 4)!;
            game.Update(.3f);
            Assert.That(plant.Asleep, Is.True);
            Assert.That(plant.DoomShroomPresentation, Is.Null);
            Assert.That(game.Presentation.ZombieSounds.Exists(sound => sound.Foley == 57), Is.False);
            plant.WakeTimer = .01f;
            game.Update(.01f);
            var state = plant.DoomShroomPresentation!;
            Assert.That(plant.Asleep, Is.False);
            Assert.That(state.Countdown, Is.EqualTo(99));
            Assert.That(state.AnimationTicks, Is.EqualTo(1));
            Assert.That(state.SampleSeconds(0f), Is.EqualTo(.01f));
            Assert.That(plant.PresentationRate, Is.EqualTo(23f));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 57), Has.Count.EqualTo(1));
            for (var head = 0; head < state.HeadShakeX.Length; head++)
            {
                var amount = head == 0 ? 1f : 2f;
                Assert.That(state.HeadShakeX[head], Is.InRange(-amount, amount));
                Assert.That(state.HeadShakeY[head], Is.InRange(-amount, amount));
            }

            game.Update(.98f);
            Assert.That(state.Countdown, Is.EqualTo(1));
            Assert.That(game.Plants.Contains(plant), Is.True);
            Assert.That(game.Adventure.Craters, Is.Empty);
            game.Update(.01f);
            Assert.That(game.Plants.Contains(plant), Is.False);
            Assert.That(game.Adventure.Craters, Has.Count.EqualTo(1));
            Assert.That(game.Adventure.Craters[0].Remaining, Is.EqualTo(180f));
            Assert.That(events.FindAll(evt => evt == GameEvent.DoomExplosion), Has.Count.EqualTo(1));
            Assert.That(events, Has.No.Member(GameEvent.Explosion));
            game.Update(.01f);
            Assert.That(game.Adventure.Craters, Has.Count.EqualTo(1));
        }

        /// <summary>Checks a flying cob shell starts shake at contact rather than before its flight ends.</summary>
        [Test]
        public void CobShellStartsShakeOnlyAtImpact()
        {
            var game = Fixture();
            game.Adventure._cobShells.Add(new AdventureCobShell
            {
                SourcePlantId = 1000,
                TargetX = 4.5f,
                TargetRow = 1f,
                FlightRemaining = .02f
            });
            game.Update(.01f);
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.Zero);
            Assert.That(game.Adventure.CobShells, Has.Count.EqualTo(1));
            game.Update(.01f);
            Assert.That(game.Adventure.CobShells, Is.Empty);
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.EqualTo(12));
            game.Update(.06f);
            Assert.That(Math.Abs(game.Presentation.ScreenShake.OffsetX), Is.EqualTo(3));
            Assert.That(game.Presentation.ScreenShake.OffsetY, Is.EqualTo(-4));
        }

        /// <summary>Checks the complete PC Squash sequence before its five-update damage-to-landing delay.</summary>
        /// <param name="level">The original ground, pool or roof background.</param>
        /// <param name="row">The original land or water lane.</param>
        /// <param name="shakes">Whether the original terrain permits landing shake.</param>
        [TestCase(19, 1, true)]
        [TestCase(29, 2, false)]
        [TestCase(49, 1, true)]
        public void SquashShakesFiveTicksAfterDamageOnlyOnLand(int level, int row, bool shakes)
        {
            // PC 46ED90: look80, prelaunch45, rising50, falling10; damage at counter5, landing at zero.
            // The supplied mobile-derived source differs: prelaunch30 and damage at counter4.
            var game = Fixture(level);
            Plant? support = null;
            if (game.Adventure.Level.IsWaterRow(row) || game.Adventure.Level.HasRoof)
            {
                support = (Plant)Invoke(game, "PlaceAdventurePlant",
                    game.Adventure.Level.HasRoof ? PlantKind.FlowerPot : PlantKind.LilyPad, row, 4)!;
            }

            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Squash, row, 4)!;
            // This general-purpose timer cannot skip the original authored action sequence.
            plant.ActionTimer = .01f;
            var target = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Normal, row,
                AdventureZombieNativeRules.GridX(390f), 0)!;
            target.X = AdventureZombieNativeRules.GridX(390f);
            target.PresentationPreviousX = target.X;
            target.FrozenRemaining = 100f;
            game.Update(.01f);
            var state = plant.SquashPresentation!;
            Assert.That(state.NativePhase, Is.EqualTo(3));
            Assert.That(state.Countdown, Is.EqualTo(80));
            Assert.That(state.Animation, Is.EqualTo("anim_lookright"));
            Assert.That(state.Rate, Is.EqualTo(24f));
            Assert.That(state.BlendTicks, Is.EqualTo(10));
            Assert.That(game.Zombies, Has.Member(target));
            game.Update(.79f);
            Assert.That(state.NativePhase, Is.EqualTo(3));
            Assert.That(state.Countdown, Is.EqualTo(1));
            game.Update(.01f);
            Assert.That(state.NativePhase, Is.EqualTo(4));
            Assert.That(state.Countdown, Is.EqualTo(45));
            Assert.That(state.Animation, Is.EqualTo("anim_jumpup"));
            Assert.That(state.BlendTicks, Is.EqualTo(20));
            game.Update(.44f);
            Assert.That(state.NativePhase, Is.EqualTo(4));
            Assert.That(state.Countdown, Is.EqualTo(1));
            game.Update(.01f);
            Assert.That(state.NativePhase, Is.EqualTo(5));
            Assert.That(state.Countdown, Is.EqualTo(50));
            Assert.That(plant.PresentationBiteNotOnGround, Is.True);
            game.Update(.49f);
            Assert.That(state.NativePhase, Is.EqualTo(5));
            Assert.That(state.Countdown, Is.EqualTo(1));
            game.Update(.01f);
            Assert.That(state.NativePhase, Is.EqualTo(6));
            Assert.That(state.Countdown, Is.EqualTo(10));
            Assert.That(state.Animation, Is.EqualTo("anim_jumpdown"));
            Assert.That(state.Rate, Is.EqualTo(60f));
            Assert.That(state.BlendTicks, Is.Zero);
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.Zero);
            game.Update(.04f);
            Assert.That(state.Countdown, Is.EqualTo(6));
            Assert.That(game.Zombies, Has.Member(target), "Damage must wait until falling counter five.");
            game.Update(.01f);
            Assert.That(state.Countdown, Is.EqualTo(5));
            Assert.That(game.Zombies, Has.No.Member(target));
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.Zero);
            game.Update(.04f);
            Assert.That(state.Countdown, Is.EqualTo(1));
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.Zero);
            game.Update(.01f);
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.EqualTo(shakes ? 12 : 0));
            Assert.That(game.Presentation._squashLandings.Count, Is.EqualTo(1));
            var landing = game.Presentation._squashLandings[0];
            Assert.That(landing.Water, Is.EqualTo(!shakes));
            Assert.That(landing.X, Is.EqualTo(state.NativeX + (shakes ? 40f : -11f)));
            Assert.That(landing.Y, Is.EqualTo(state.NativeY + (!shakes ? 20f : level == 49 ? 69f : 80f)));
            Assert.That(game.Plants.Contains(plant), Is.EqualTo(shakes));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 40).Count, Is.EqualTo(shakes ? 1 : 0));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == -4).Count, Is.EqualTo(shakes ? 0 : 1));
            if (support != null)
            {
                Assert.That(game.Plants, Has.Member(support));
            }

            if (shakes)
            {
                Assert.That(state.NativePhase, Is.EqualTo(7));
                Assert.That(state.Countdown, Is.EqualTo(100));
            }

            game.Update(.06f);
            Assert.That(Math.Abs(game.Presentation.ScreenShake.OffsetX), Is.EqualTo(shakes ? 1 : 0));
            Assert.That(game.Presentation.ScreenShake.OffsetY, Is.EqualTo(shakes ? 4 : 0));
            if (shakes)
            {
                game.Update(.93f);
                Assert.That(state.Countdown, Is.EqualTo(1));
                Assert.That(game.Plants, Has.Member(plant));
                game.Update(.01f);
                Assert.That(game.Plants, Has.No.Member(plant), "The original flattened body remains for exactly one hundred updates.");
            }
        }

        /// <summary>Checks the ice special action creates the original flash and snowflake particle system without shake.</summary>
        [Test]
        public void IceShroomCreatesOriginalIceTrapAndExpiresAtFortyTicks()
        {
            var game = Fixture();
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.IceShroom, 1, 4)!;
            plant.ActionTimer = .01f;
            Assert.That(game.Presentation.Explosions, Is.Empty);
            game.Update(.01f);
            Assert.That(game.Plants.Contains(plant), Is.False);
            Assert.That(game.Presentation.Explosions, Has.Count.EqualTo(1));
            var effect = game.Presentation.Explosions[0];
            Assert.That(effect.Effect, Is.EqualTo(AdventureExplosionEffect.IceTrap));
            Assert.That(effect.Particle, Is.EqualTo("IceTrap"));
            Assert.That(effect.DurationTicks, Is.EqualTo(40));
            Assert.That(effect.X, Is.EqualTo(4.5f));
            Assert.That(effect.Row, Is.EqualTo(1));
            Assert.That(effect.Ticks, Is.Zero);
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.Zero);
            game.Update(.39f);
            Assert.That(game.Presentation.Explosions, Has.Count.EqualTo(1));
            game.Update(.01f);
            Assert.That(game.Presentation.Explosions, Is.Empty);
        }

        /// <summary>Checks pause freezes native screen offsets and reward presentation finishes an outstanding shake.</summary>
        [Test]
        public void PauseFreezesShakeAndRewardPresentationReturnsTheBoardToOrigin()
        {
            var game = Fixture();
            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.CherryBomb, 1, 4)!;
            plant.RequiredCherryBombPresentation.Countdown = 1;
            game.Update(.07f);
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.EqualTo(6));
            var x = game.Presentation.ScreenShake.OffsetX;
            var y = game.Presentation.ScreenShake.OffsetY;
            game.TogglePause();
            game.Update(.1f);
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.EqualTo(6));
            Assert.That(game.Presentation.ScreenShake.OffsetX, Is.EqualTo(x));
            Assert.That(game.Presentation.ScreenShake.OffsetY, Is.EqualTo(y));
            game.TogglePause();
            typeof(GameSimulation).GetProperty(nameof(GameSimulation.State))!.SetValue(game, MatchState.Won);
            game.Adventure.Phase = AdventurePhase.Reward;
            game.Adventure.RewardPresentation = new AdventureRewardPresentation(15, 400f, 300f, 91, 78, .5f, .5f, 0);
            game.Update(.06f);
            Assert.That(game.Presentation.ScreenShake.TicksRemaining, Is.Zero);
            Assert.That(game.Presentation.ScreenShake.OffsetX, Is.Zero);
            Assert.That(game.Presentation.ScreenShake.OffsetY, Is.Zero);
        }

        /// <summary>Creates a legal night board with distant waves and no access to persistent player data.</summary>
        /// <param name="level">The level providing the required terrain; the default is a night lawn.</param>
        /// <returns>An isolated simulation on which production plant actions can be exercised.</returns>
        private static GameSimulation Fixture(int level = 19)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Invokes the existing production fixture hook without adding a runtime API.</summary>
        /// <param name="game">The isolated simulation.</param>
        /// <param name="name">The nonpublic production method.</param>
        /// <param name="arguments">Arguments accepted by the production method.</param>
        /// <returns>The result returned by the production method.</returns>
        /// <exception cref="TargetInvocationException">The production method throws.</exception>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
