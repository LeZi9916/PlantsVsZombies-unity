#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original PC usable-seed drop motion, ground lifetime, and cursor return rules.</summary>
    public sealed class VasebreakerPacketTests
    {
        /// <summary>Checks every vase recipe creates a lawn pickup at its original pot rather than a bank slot.</summary>
        /// <param name="mode">Zero for adventure or the original independent puzzle mode identifier.</param>
        /// <param name="stage">The zero-based adventure stage, unused for independent puzzles.</param>
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 2)]
        [TestCase(51, 0)]
        [TestCase(52, 0)]
        [TestCase(53, 0)]
        [TestCase(54, 0)]
        [TestCase(55, 0)]
        [TestCase(56, 0)]
        [TestCase(57, 0)]
        [TestCase(58, 0)]
        [TestCase(59, 0)]
        [TestCase(60, 0)]
        public void EveryVaseBoardDropsSeedsAtTheOriginalPot(int mode, int stage)
        {
            var game = CreateGame(mode, stage);
            var packet = OpenSeededVase(game);
            Assert.That(packet.IsVaseDrop, Is.True);
            Assert.That(packet.PresentationX, Is.EqualTo(60f + packet.FromVaseColumn!.Value * 80f));
            Assert.That(packet.PresentationY, Is.EqualTo(80f + packet.FromVaseRow!.Value * 100f));
            Assert.That(packet.PresentationPreviousX, Is.EqualTo(packet.PresentationX));
            Assert.That(packet.PresentationPreviousY, Is.EqualTo(packet.PresentationY));
            Assert.That(packet.HitGround, Is.False);
            Assert.That(packet.GroundTicks, Is.Zero);
            Assert.That(packet.FadeTicks, Is.Zero);
            Assert.That(packet.FallVelocityY, Is.InRange(-3.4f, -1.7f));
            Assert.That(packet.FallVelocityX, Is.InRange(-.4f, .4f));
            Assert.That(packet.GroundY, Is.InRange(packet.PresentationY + 15f, packet.PresentationY + 34f));
        }

        /// <summary>Checks native random call order, position-before-gravity integration, and the first grounded update.</summary>
        [Test]
        public void FromPlantMotionUsesNativeTicketsAndLandingCoordinates()
        {
            var game = CreateGame(0, 0);
            var vase = new AdventureVase { Row = 2, Column = 6, _seed = PlantKind.Peashooter };
            game.Adventure._vases.Clear();
            game.Adventure._vases.Add(vase);
            game.Adventure._vases.Add(new AdventureVase { Row = 0, Column = 8, _seed = PlantKind.Squash });
            Assert.That(game.SmashAdventureVase(2, 6), Is.True);
            game.Update(.19f);
            var random = new PacketRandom();
            typeof(GameSimulation).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, random);
            game.Update(.01f);
            var packet = game.Adventure.Packets.Single();
            Assert.That(random.Calls, Is.EqualTo(new[] { "float", "float", "20" }));
            Assert.That(packet.PresentationX, Is.EqualTo(540f));
            Assert.That(packet.PresentationY, Is.EqualTo(280f));
            Assert.That(packet.FallVelocityY, Is.EqualTo(-1.7f).Within(.000001f));
            Assert.That(packet.FallVelocityX, Is.EqualTo(.4f).Within(.000001f));
            Assert.That(packet.GroundY, Is.EqualTo(302f));
            game.Update(.01f);
            Assert.That(packet.PresentationPreviousX, Is.EqualTo(540f));
            Assert.That(packet.PresentationPreviousY, Is.EqualTo(280f));
            Assert.That(packet.PresentationX, Is.EqualTo(540.4f).Within(.0001f));
            Assert.That(packet.PresentationY, Is.EqualTo(278.3f).Within(.0001f));
            Assert.That(packet.FallVelocityY, Is.EqualTo(-1.61f).Within(.000001f));
            Assert.That(packet.GroundTicks, Is.Zero);
            game.Update(.47f);
            Assert.That(packet.HitGround, Is.False, "The native trajectory remains airborne for its first forty-eight updates.");
            Assert.That(packet.PresentationY, Is.EqualTo(299.92f).Within(.001f));
            game.Update(.01f);
            Assert.That(packet.HitGround, Is.True);
            Assert.That(packet.PresentationX, Is.EqualTo(559f));
            Assert.That(packet.PresentationY, Is.EqualTo(302f));
            Assert.That(packet.GroundTicks, Is.EqualTo(1), "Lifetime starts on the landing update rather than at creation.");
        }

        /// <summary>Checks landing comparisons and horizontal rounding retain the original extended intermediates.</summary>
        [Test]
        public void LandingUsesNativePrecisionBeforePositionStores()
        {
            var random = new Random(0);
            var packet = new AdventurePacket
            {
                PresentationY = 100f, FallVelocityY = 19.999996f, GroundY = 120
            };
            Assert.That(packet.TickVaseDrop(random), Is.False);
            Assert.That(packet.PresentationY, Is.EqualTo(120f), "The float position store rounds up only after the airborne comparison.");
            Assert.That(packet.HitGround, Is.False);
            Assert.That(packet.GroundTicks, Is.Zero);
            packet.TickVaseDrop(random);
            Assert.That(packet.HitGround, Is.True);
            Assert.That(packet.GroundTicks, Is.EqualTo(1));

            var rounding = new AdventurePacket
            {
                PresentationX = .49999997f, PresentationY = 120f, GroundY = 120
            };
            rounding.TickVaseDrop(random);
            Assert.That(rounding.PresentationX, Is.Zero, "Native half-away rounding converts the extended sum directly to an integer.");
        }

        /// <summary>Checks the fifteen-second grounded lifetime followed by the original fifteen-update fade.</summary>
        /// <param name="mode">Zero for adventure or the independent Leftpeater recipe identifier.</param>
        [TestCase(0)]
        [TestCase(52)]
        public void GroundLifetimeAndFadeAreSharedByAdventureAndPuzzles(int mode)
        {
            var game = CreateGame(mode, 0);
            var packet = OpenSeededVase(game, mode == 52);
            Land(packet);
            packet.Age = 100f;
            packet.GroundTicks = 1498;
            Advance(game, .01f);
            Assert.That(game.Adventure.Packets, Does.Contain(packet), "Total age must not replace the grounded disappear counter.");
            Assert.That(packet.GroundTicks, Is.EqualTo(1499));
            Assert.That(packet.FadeTicks, Is.Zero);
            Advance(game, .01f);
            Assert.That(packet.GroundTicks, Is.EqualTo(1500));
            Assert.That(packet.FadeTicks, Is.EqualTo(15));
            Assert.That(packet.PresentationAlpha, Is.EqualTo(1f));
            Advance(game, .14f);
            Assert.That(game.Adventure.Packets, Does.Contain(packet));
            Assert.That(packet.FadeTicks, Is.EqualTo(1));
            Assert.That(packet.PresentationAlpha, Is.EqualTo(1f / 15f).Within(.000001f));
            Advance(game, .01f);
            Assert.That(game.Adventure.Packets, Has.No.Member(packet));
            if (game.ModeSession != null)
            {
                Assert.That(game.ModeSession._backwardPackets, Has.No.Member(packet.Id));
            }
        }

        /// <summary>Checks the original final-three-second warning uses thirty-update brightness bands.</summary>
        [Test]
        public void ExpirationWarningUsesNativeGroundCounterBands()
        {
            var packet = OpenSeededVase(CreateGame(0, 0));
            packet.GroundTicks = 1200;
            Assert.That(packet.PresentationGrayness, Is.EqualTo(255));
            packet.GroundTicks = 1201;
            Assert.That(packet.PresentationGrayness, Is.EqualTo(192));
            packet.GroundTicks = 1229;
            Assert.That(packet.PresentationGrayness, Is.EqualTo(192));
            packet.GroundTicks = 1230;
            Assert.That(packet.PresentationGrayness, Is.EqualTo(255));
            packet.GroundTicks = 1260;
            Assert.That(packet.PresentationGrayness, Is.EqualTo(192));
        }

        /// <summary>Checks a held card is retained and only its first return grants a minimum three-second lifetime.</summary>
        /// <param name="mode">Zero for adventure or the original independent Vasebreaker identifier.</param>
        [TestCase(0)]
        [TestCase(51)]
        public void HeldSeedUsesNativeCounterAndFirstReturnGrace(int mode)
        {
            var game = CreateGame(mode, 0);
            var packet = OpenSeededVase(game);
            Land(packet);
            packet.GroundTicks = 1499;
            Assert.That(game.CollectVasePacket(packet.Id), Is.True);
            Assert.That(packet.IsHeld, Is.True);
            Assert.That(packet.PresentationGrayness, Is.EqualTo(128));
            Advance(game, .05f);
            Assert.That(packet.GroundTicks, Is.EqualTo(1504));
            Assert.That(packet.FadeTicks, Is.Zero);
            Assert.That(game.Adventure.Packets, Does.Contain(packet));
            game.DropVasePacket(packet.Id);
            Assert.That(packet.IsHeld, Is.False);
            Assert.That(packet.GroundTicks, Is.EqualTo(1200));
            Assert.That(packet.TimesDropped, Is.EqualTo(1));
            Assert.That(game.CollectVasePacket(packet.Id), Is.True);
            packet.GroundTicks = 1499;
            Advance(game, .01f);
            game.DropVasePacket(packet.Id);
            Assert.That(packet.GroundTicks, Is.EqualTo(1500), "Further returns must not renew the first-drop grace.");
            Assert.That(packet.TimesDropped, Is.EqualTo(2));
            Advance(game, .01f);
            Assert.That(packet.FadeTicks, Is.EqualTo(15));
        }

        /// <summary>Checks pausing the board freezes airborne movement, grounded lifetime, and fade.</summary>
        [Test]
        public void PauseStopsDropMovementAndExpiration()
        {
            var game = CreateGame(0, 0);
            var packet = OpenSeededVase(game);
            var x = packet.PresentationX;
            var y = packet.PresentationY;
            game.TogglePause();
            game.Update(1f);
            Assert.That(packet.PresentationX, Is.EqualTo(x));
            Assert.That(packet.PresentationY, Is.EqualTo(y));
            Assert.That(packet.GroundTicks, Is.Zero);
            game.TogglePause();
            Land(packet);
            packet.GroundTicks = 1499;
            game.Update(.01f);
            game.TogglePause();
            game.Update(1f);
            Assert.That(packet.FadeTicks, Is.EqualTo(15));
        }

        /// <summary>Creates an isolated public adventure or standalone puzzle simulation.</summary>
        /// <param name="mode">Zero for adventure or the original standalone puzzle identifier.</param>
        /// <param name="stage">The zero-based adventure vase stage to initialize.</param>
        /// <returns>The prepared simulation with unopened pots preventing premature level completion.</returns>
        private static GameSimulation CreateGame(int mode, int stage)
        {
            if (mode != 0)
            {
                return new ModeSession(mode, randomSeed: 7529).Game;
            }

            var game = new GameSimulation(new GameRules { AdventureLevel = 35 }, 7529);
            if (stage != 0)
            {
                game.Adventure.VaseStage = stage;
                typeof(GameSimulation).GetMethod("InitializeVaseStage", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, null);
            }

            return game;
        }

        /// <summary>Opens one plant pot through the production twenty-update hammer action.</summary>
        /// <param name="game">The simulation that owns the unopened vase.</param>
        /// <param name="backwards">Whether to choose the native Leftpeater card to check metadata retirement.</param>
        /// <returns>The newly created ground seed packet.</returns>
        private static AdventurePacket OpenSeededVase(GameSimulation game, bool backwards = false)
        {
            var vase = backwards
                ? game.Adventure.Vases.First(value => game.RequiredModeSession._vaseSeeds.TryGetValue(value.Row * 9 + value.Column, out var seed) && seed == 52)
                : game.Adventure.Vases.First(value => value._seed.HasValue);
            var accepted = game.ModeSession == null ? game.SmashAdventureVase(vase.Row, vase.Column)
                : game.SmashModeVase(vase.Row, vase.Column);
            Assert.That(accepted, Is.True);
            Advance(game, .2f);
            return game.Adventure.Packets.Single();
        }

        /// <summary>Places only the packet's test state on its already randomized original ground line.</summary>
        /// <param name="packet">The packet whose lifetime boundary is being tested independently of flight duration.</param>
        private static void Land(AdventurePacket packet)
        {
            packet.PresentationY = packet.GroundY;
            packet.PresentationPreviousY = packet.GroundY;
            packet.HitGround = true;
            packet.FallVelocityY = 0f;
        }

        /// <summary>Advances the native update loop belonging to the current puzzle session.</summary>
        /// <param name="game">The isolated simulation.</param>
        /// <param name="seconds">The unpaused native simulation time to advance.</param>
        private static void Advance(GameSimulation game, float seconds)
        {
            if (game.ModeSession == null)
            {
                game.Update(seconds);
            }
            else
            {
                game.UpdateMode(seconds);
            }
        }

        /// <summary>Supplies independent native boundary tickets for the three original drop initialization draws.</summary>
        private sealed class PacketRandom : Random
        {
            /// <summary>Records the raw-float and exclusive integer requests in their original order.</summary>
            private readonly List<string> _calls = new List<string>();
            /// <summary>Tracks the two raw draws consumed by the vertical and horizontal velocities.</summary>
            private int _rawDraws;
            /// <summary>Gets the exact production random request sequence.</summary>
            internal IReadOnlyList<string> Calls => _calls;

            /// <summary>Returns the opposite ends of the original thirty-one-bit random domain.</summary>
            /// <returns>Zero for vertical launch and the final legal integer for horizontal launch.</returns>
            public override int Next()
            {
                _calls.Add("float");
                Assert.That(_rawDraws, Is.LessThan(2), "A vase plant packet consumes exactly two native floating-point tickets.");
                return _rawDraws++ == 0 ? 0 : int.MaxValue - 1;
            }

            /// <summary>Returns the original landing-height ticket and rejects an unexpected random call.</summary>
            /// <param name="maxValue">The native exclusive landing-height bound.</param>
            /// <returns>Ticket seven within the original twenty-pixel ground spread.</returns>
            public override int Next(int maxValue)
            {
                _calls.Add(maxValue.ToString());
                Assert.That(maxValue, Is.EqualTo(20));
                return 7;
            }
        }
    }
}
