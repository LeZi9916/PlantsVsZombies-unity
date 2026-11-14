#nullable enable

using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the supplied PC executable's storm state machine, local flash noise and rain lifecycle.</summary>
    public sealed class AdventureStormTests
    {
        /// <summary>Checks original integer flash opacities with the native seed-zero noise of minus 28.</summary>
        /// <param name="phase">The native challenge state.</param>
        /// <param name="countdown">The native remaining update count.</param>
        /// <param name="black">The expected black opacity.</param>
        /// <param name="white">The expected white opacity.</param>
        [TestCase(5, 300, 255, 0)]
        [TestCase(5, 299, 0, 252)]
        [TestCase(5, 225, 100, 0)]
        [TestCase(5, 150, 135, 92)]
        [TestCase(5, 75, 181, 0)]
        [TestCase(6, 299, 0, 252)]
        [TestCase(6, 150, 100, 0)]
        [TestCase(7, 150, 255, 0)]
        [TestCase(7, 149, 0, 252)]
        [TestCase(0, 0, 255, 0)]
        public void FlashUsesOriginalBlackWhiteCurves(int phase, int countdown, int black, int white)
        {
            var state = new AdventureStormState { Phase = phase, Countdown = countdown };
            state.GetFlashAlpha(out var actualBlack, out var actualWhite);
            Assert.That(actualBlack, Is.EqualTo(black));
            Assert.That(actualWhite, Is.EqualTo(white));
        }

        /// <summary>Checks separate introduction/start states, exact thunder boundaries and the native pause latch.</summary>
        [Test]
        public void IntroStartThunderAndPauseFollowNativeCountdowns()
        {
            var random = new OriginalRandomAdapter(41);
            var state = new AdventureStormState();
            state.BeginIntro();
            Assert.That(state.Phase, Is.EqualTo(6));
            Assert.That(state.Countdown, Is.EqualTo(150));
            for (var tick = 0; tick < 150; tick++)
            {
                Assert.That(state.Advance(random, false, false, false, false), Is.False);
            }

            Assert.That(state.Phase, Is.Zero);
            Assert.That(state.MainTicks, Is.Zero);
            Assert.That(state.EffectTicks, Is.EqualTo(150));
            state.BeginPlaying();
            Assert.That(state.Countdown, Is.EqualTo(400));
            for (var tick = 0; tick < 99; tick++)
            {
                Assert.That(state.Advance(random, true, false, false, false), Is.False);
            }

            Assert.That(state.Advance(random, true, false, false, false), Is.True);
            Assert.That(state.Countdown, Is.EqualTo(300));
            Assert.That(state.FlashVisible, Is.False);
            state.Countdown = 151;
            var effects = state.EffectTicks;
            var main = state.MainTicks;
            Assert.That(state.Advance(random, true, true, false, false), Is.True);
            Assert.That(state.Countdown, Is.EqualTo(150));
            Assert.That(state.Advance(random, true, true, false, false), Is.False);
            Assert.That(state.Countdown, Is.EqualTo(1));
            for (var tick = 0; tick < 100; tick++)
            {
                Assert.That(state.Advance(random, true, true, false, false), Is.False);
            }

            Assert.That(state.Countdown, Is.EqualTo(1));
            Assert.That(state.EffectTicks, Is.EqualTo(effects));
            Assert.That(state.MainTicks, Is.EqualTo(main));
        }

        /// <summary>Checks both native random interval branches, the Yeti interval and loss flashes without rain consuming draws.</summary>
        /// <param name="seed">The original MT seed selecting either interval branch.</param>
        /// <param name="yeti">Whether a Yeti remains on the board.</param>
        /// <param name="lost">Whether zombies won the board.</param>
        [TestCase(1u, false, false)]
        [TestCase(2u, false, false)]
        [TestCase(1u, true, false)]
        [TestCase(1u, false, true)]
        public void FlashIntervalsPreserveNativeRandomDrawOrder(uint seed, bool yeti, bool lost)
        {
            var expected = new OriginalRandomAdapter(seed);
            var countdown = lost ? 100 + expected.Next(101) : yeti ? 350 + expected.Next(101)
                : 450 + expected.Next(expected.Next(2) == 0 ? 451 : 101);
            var phase = lost ? 7 : 5 + expected.Next(3);
            var actual = new OriginalRandomAdapter(seed);
            var state = new AdventureStormState { Countdown = 0 };
            state.Advance(actual, !lost, true, lost, yeti);
            Assert.That(state.Countdown, Is.EqualTo(countdown));
            Assert.That(state.Phase, Is.EqualTo(phase));
            Assert.That(actual.Generator.DrawCount, Is.EqualTo(expected.Generator.DrawCount));
            Assert.That(actual.Next(), Is.EqualTo(expected.Next()));
        }

        /// <summary>Checks native splash bands, opacity, scale and the nineteen/eight-frame one-shot lifetimes.</summary>
        [Test]
        public void RainUsesOriginalThreeBandsAndOneShotReanimations()
        {
            var random = new OriginalRandomAdapter(1);
            // PC MT seed one, 0x5E7060 float stores and 0x437750 range additions.
            var native = new[,]
            {
                { 623.830810546875f, 239.1554412841797f, 1.1325573921203613f },
                { 40.160133361816406f, 320.7498779296875f, 1.0992324352264404f },
                { 245.458251953125f, 511.38311767578125f, 1.0965807437896729f }
            };
            var alpha = new[] { 103, 122, 102 };
            var state = new AdventureStormState();
            state.Advance(random, true, false, false, false);
            Assert.That(state.Rain.Count, Is.EqualTo(3));
            for (var index = 0; index < 3; index++)
            {
                var circle = index == 1;
                var rain = state.Rain[index];
                Assert.That(rain.Circle, Is.EqualTo(circle));
                Assert.That(rain.X, Is.EqualTo(native[index, 0]));
                Assert.That(rain.Y, Is.EqualTo(native[index, 1]));
                Assert.That(rain.Alpha, Is.EqualTo(alpha[index]));
                Assert.That(rain.Scale, Is.EqualTo(native[index, 2]));
                Assert.That(rain.Ticks, Is.Zero);
            }

            const int gap = 14;
            Assert.That(random.Generator.DrawCount, Is.EqualTo(13));
            for (var tick = 0; tick < gap; tick++)
            {
                state.Advance(random, true, false, false, false);
            }

            Assert.That(state.Rain.Count, Is.EqualTo(3));
            state.Advance(random, true, false, false, false);
            Assert.That(state.Rain.Count, Is.EqualTo(6), "A counter of ten through twenty spawns after eleven through twenty-one updates.");
            for (var tick = gap + 1; tick < 159; tick++)
            {
                state.Advance(random, true, false, false, false);
                for (var index = 0; index < state.Rain.Count; index++)
                {
                    var rain = state.Rain[index];
                    Assert.That(rain.Ticks * 12, Is.LessThan((rain.Circle ? 19 : 8) * 100));
                }
            }

            for (var index = 0; index < state.Rain.Count; index++)
            {
                Assert.That(state.Rain[index].Ticks, Is.LessThan(159));
            }
        }

        /// <summary>Checks flash sampling is stable, uses six-update seed groups and never consumes board randomness.</summary>
        [Test]
        public void FlashNoiseIsLocalAndReseedingReusesTheNativeGenerator()
        {
            var generator = new OriginalRandom(99);
            generator.Next31();
            generator.Reseed(0);
            Assert.That(generator.DrawCount, Is.Zero);
            Assert.That(generator.Next31(), Is.EqualTo(2146374468u));
            generator.Reseed(1);
            Assert.That(generator.Next31(), Is.EqualTo(1791095845u));
            var random = new OriginalRandomAdapter(99);
            var state = new AdventureStormState { Phase = 6, Countdown = 150 };
            for (var tick = 0; tick < 6; tick++)
            {
                state.Advance(random, true, false, false, false);
            }

            state.Countdown = 150;
            var draws = random.Generator.DrawCount;
            state.GetFlashAlpha(out var black, out var white);
            Assert.That(black, Is.EqualTo(133), "MainCounter / 6 equals one, whose native first noise is five.");
            Assert.That(white, Is.Zero);
            state.GetFlashAlpha(out var repeatedBlack, out var repeatedWhite);
            Assert.That(repeatedBlack, Is.EqualTo(black));
            Assert.That(repeatedWhite, Is.EqualTo(white));
            Assert.That(random.Generator.DrawCount, Is.EqualTo(draws));
        }

        /// <summary>Checks both original forced flash cues survive an interval spanning their native boundaries.</summary>
        [Test]
        public void IntroEventIntervalIncludesBothOriginalStormFlashes()
        {
            var plan = AdventureIntroPresentation.CreatePlan(new AdventureIntroContext { Level = 40, IsStormyNight = true, IsConveyor = true });
            var flashes = 0;
            AdventureIntroPresentation.EventsBetween(plan, 0, plan.End, item =>
            {
                if (item == AdventureIntroEvent.StormFlashStarted)
                {
                    flashes++;
                }
            });
            Assert.That(flashes, Is.EqualTo(2));
        }

        /// <summary>Checks the original award phase continues weather while the board scene remains Playing.</summary>
        [Test]
        public void VictoryRewardKeepsOriginalWeatherActive()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 40 }, 91);
            game.Adventure.CurrentWave = game.Adventure.Waves.Count;
            game.Adventure._finalAmbush = true;
            game.Update(.01f);
            Assert.That(game.State, Is.EqualTo(MatchState.Won));
            var storm = game.Adventure.Storm;
            var effects = storm.EffectTicks;
            var main = storm.MainTicks;
            var countdown = storm.Countdown;
            game.Update(.1f);
            Assert.That(storm.EffectTicks, Is.EqualTo(effects + 10));
            Assert.That(storm.MainTicks, Is.EqualTo(main + 10));
            Assert.That(storm.Countdown, Is.EqualTo(countdown - 10));
            Assert.That(storm.Lost, Is.False);
        }

        /// <summary>Checks repeated float centiseconds do not lose native paused/loss updates through accumulated rounding.</summary>
        [Test]
        public void PresentationClockPreservesCentisecondTicksAcrossPauseAndLoss()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 40 }, 91);
            var storm = game.Adventure.Storm;
            storm.Phase = 6;
            storm.Countdown = 2000;
            game.TogglePause();
            for (var tick = 0; tick < 1100; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(storm.Countdown, Is.EqualTo(900));
            Assert.That(storm.EffectTicks, Is.Zero);
            typeof(GameSimulation).GetProperty(nameof(GameSimulation.State))!.SetValue(game, MatchState.Lost);
            for (var tick = 0; tick < 1100; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(storm.LostTicks, Is.EqualTo(1100));
            Assert.That(storm.LossDialogReady, Is.True);
            Assert.That(storm.EffectTicks, Is.EqualTo(1099), "The dialog pauses the board before the 1100th loss tick's effect update.");
            Assert.That(storm.MainTicks, Is.Zero);
        }

        /// <summary>Checks the production gameplay, pause and loss entry points retain the native storm behavior.</summary>
        [Test]
        public void SimulationTicksStormWithoutHidingInteractionInTheRightColumns()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 40 }, 91);
            Assert.That(game.AdventureHasFog, Is.False);
            Assert.That(game.IsAdventureCellVisible(0, 8), Is.True);
            for (var tick = 0; tick < 100; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(game.Adventure.Storm.Countdown, Is.EqualTo(300));
            Assert.That(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 17).Count, Is.EqualTo(1));
            game.TogglePause();
            var effects = game.Adventure.Storm.EffectTicks;
            game.Update(.05f);
            Assert.That(game.Adventure.Storm.Countdown, Is.EqualTo(295));
            Assert.That(game.Adventure.Storm.EffectTicks, Is.EqualTo(effects));
            typeof(GameSimulation).GetProperty(nameof(GameSimulation.State))!.SetValue(game, MatchState.Lost);
            game.Adventure.Storm.Countdown = 1;
            game.Update(.01f);
            Assert.That(game.Adventure.Storm.Phase, Is.EqualTo(7));
            Assert.That(game.Adventure.Storm.Countdown, Is.InRange(100, 200));
            Assert.That(game.Adventure.Storm.EffectTicks, Is.EqualTo(effects + 1));
            var transition = new AdventureStormState();
            var random = new OriginalRandomAdapter(91);
            transition.Advance(random, false, false, true, false);
            transition.Advance(random, false, false, true, false);
            Assert.That(transition.MainTicks, Is.Zero, "PC 420E20 updates game objects before checking the scene, so the losing combat update does not increment MainCounter.");
        }
    }
}
