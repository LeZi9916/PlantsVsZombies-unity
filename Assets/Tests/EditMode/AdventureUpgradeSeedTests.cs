#nullable enable
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC purple packet backgrounds, startup/post-use cooldowns and survival-endless prices.</summary>
    public sealed class AdventureUpgradeSeedTests
    {
        /// <summary>Checks every purple card uses its original purple frame and 5000-tick table refresh.</summary>
        /// <param name="kind">The original upgrade packet.</param>
        [TestCase(PlantKind.GatlingPea)]
        [TestCase(PlantKind.TwinSunflower)]
        [TestCase(PlantKind.GloomShroom)]
        [TestCase(PlantKind.Cattail)]
        [TestCase(PlantKind.WinterMelon)]
        [TestCase(PlantKind.GoldMagnet)]
        [TestCase(PlantKind.Spikerock)]
        [TestCase(PlantKind.CobCannon)]
        public void UpgradeUsesPurplePacketAndNativeRefresh(PlantKind kind)
        {
            Assert.That(AdventureUpgradeSeedRules.PacketFrame(kind), Is.EqualTo(1));
            var session = Session(13, kind);
            Assert.That(session.Game.AdventureSeedRechargeTime(kind), Is.EqualTo(50.01f).Within(.0001f));
            Assert.That(session.Game.CooldownRemaining(kind), Is.EqualTo(35.01f).Within(.0001f));
            var context = session.Game.AdventurePacketContext(kind);
            Assert.That(context.RefreshTime, Is.EqualTo(3500));
            Assert.That(context.RefreshCounter, Is.Zero);
            for (var tick = 0; tick < 3500; tick++)
            {
                Invoke(session.Game, "AdvanceCooldowns", false);
            }
            Assert.That(session.Game.AdventurePacketContext(kind).Active, Is.False);
            Invoke(session.Game, "AdvanceCooldowns", false);
            Assert.That(session.Game.AdventurePacketContext(kind).Active, Is.True);
        }

        /// <summary>Checks price population counts each two-cell cannon once and decreases when that plant is removed.</summary>
        /// <param name="mode">The original survival-endless mode.</param>
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        public void EndlessPriceCountsCurrentCopiesOnly(int mode)
        {
            var session = Session(mode, PlantKind.CobCannon);
            var game = session.Game;
            var first = new Plant { Id = 70, Kind = PlantKind.CobCannon, Row = 0, Column = 0, ColumnSpan = 2, Health = 300f };
            var second = new Plant { Id = 71, Kind = PlantKind.CobCannon, Row = 1, Column = 2, ColumnSpan = 2, Health = 300f };
            var other = new Plant { Id = 72, Kind = PlantKind.GatlingPea, Row = 0, Column = 3, Health = 300f };
            Assert.That(game.AdventureSeedCost(PlantKind.CobCannon), Is.EqualTo(500));
            SimulationFixture.Plants(game).Add(first);
            SimulationFixture.Plants(game).Add(second);
            SimulationFixture.Plants(game).Add(other);
            Assert.That(game.AdventureSeedCost(PlantKind.CobCannon), Is.EqualTo(600));
            Assert.That(game.AdventureSeedCost(PlantKind.GatlingPea), Is.EqualTo(300));
            SimulationFixture.Plants(game).Remove(first);
            Assert.That(game.AdventureSeedCost(PlantKind.CobCannon), Is.EqualTo(550));
            SimulationFixture.Plants(game).Remove(second);
            Assert.That(game.AdventureSeedCost(PlantKind.CobCannon), Is.EqualTo(500));
        }

        /// <summary>Checks a successful planting spends the live price and restarts the full original cooldown.</summary>
        [Test]
        public void PlantingSpendsAcceleratedPriceAndUsesFullRefresh()
        {
            var session = Session(11, PlantKind.Spikerock);
            var game = session.Game;
            for (var tick = 0; tick < 3501; tick++)
            {
                Invoke(game, "AdvanceCooldowns", false);
            }
            ((SimulationWorld)game.MemberOwner("SunAmount")).SunAmount = 1000;
            SimulationFixture.Plants(game).Add(new Plant { Id = 71, Kind = PlantKind.Spikerock, Layer = PlantLayer.Main, Row = 1, Column = 1, Health = 450f });
            SimulationFixture.Plants(game).Add(new Plant { Id = 72, Kind = PlantKind.Spikeweed, Layer = PlantLayer.Main, Row = 1, Column = 3, Health = 300f });
            Assert.That(game.AdventureSeedCost(PlantKind.Spikerock), Is.EqualTo(175));
            Assert.That(game.TryPlant(PlantKind.Spikerock, 1, 3), Is.True);
            Assert.That(game.SunAmount, Is.EqualTo(825));
            Assert.That(game.AdventureSeedCost(PlantKind.Spikerock), Is.EqualTo(225));
            Assert.That(game.AdventurePacketContext(PlantKind.Spikerock).RefreshTime, Is.EqualTo(5000));
            Assert.That(game.CooldownRemaining(PlantKind.Spikerock), Is.EqualTo(50.01f).Within(.0001f));
            for (var tick = 0; tick < 5000; tick++)
            {
                Invoke(game, "AdvanceCooldowns", false);
            }
            Assert.That(game.AdventurePacketContext(PlantKind.Spikerock).Active, Is.False);
            Invoke(game, "AdvanceCooldowns", false);
            Assert.That(game.AdventurePacketContext(PlantKind.Spikerock).Active, Is.True);
        }

        /// <summary>Checks later survival selection preserves carried refresh and starts newly selected upgrades ready.</summary>
        [Test]
        public void LaterSurvivalBankRetainsOnlyCarriedPacketRefresh()
        {
            var session = Session(13, PlantKind.GatlingPea);
            Invoke(session.Game, "AdvanceCooldowns", false);
            var remaining = session.Game.CooldownRemaining(PlantKind.GatlingPea);
            session.Stage = 2;
            Invoke(session.Game, "InitializeUpgradePacketCooldowns", new object[] { new[] { PlantKind.GatlingPea, PlantKind.CobCannon } });
            Assert.That(session.Game.CooldownRemaining(PlantKind.GatlingPea), Is.EqualTo(remaining));
            Assert.That(session.Game.CooldownRemaining(PlantKind.CobCannon), Is.Zero);
        }

        /// <summary>Checks only endless survival uses accelerated pricing, including its chooser plus sign eligibility.</summary>
        /// <param name="mode">The original native mode.</param>
        [TestCase(1)]
        [TestCase(6)]
        [TestCase(10)]
        [TestCase(31)]
        [TestCase(60)]
        [TestCase(70)]
        public void OtherModesKeepBasePrice(int mode)
        {
            Assert.That(AdventureUpgradeSeedRules.UsesAcceleratedPricing(PlantKind.CobCannon, mode), Is.False);
            Assert.That(AdventureUpgradeSeedRules.PacketFrame(PlantKind.Imitater), Is.Zero);
            Assert.That(AdventureUpgradeSeedRules.PacketFrame(PlantKind.KernelPult), Is.EqualTo(2));
        }

        /// <summary>Creates a chosen bank containing the tested purchased upgrade.</summary>
        /// <param name="mode">The original mode identifier.</param>
        /// <param name="kind">The upgrade placed in the bank.</param>
        /// <returns>The isolated mode session.</returns>
        private static ModeSession Session(int mode, PlantKind kind)
        {
            var session = new ModeSession(mode, AdventureSeedAccess.DevelopmentAll());
            Assert.That(session.ChooseSeeds(new[] { kind, PlantKind.Peashooter, PlantKind.Wallnut,
                PlantKind.PotatoMine, PlantKind.SnowPea, PlantKind.Repeater }), Is.True);
            return session;
        }

        /// <summary>Invokes a production collaborator through the existing inspection boundary.</summary>
        /// <param name="game">The owning simulation.</param>
        /// <param name="name">The original operation.</param>
        /// <param name="arguments">The operation arguments.</param>
        /// <returns>The operation result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return SimulationInspection.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
