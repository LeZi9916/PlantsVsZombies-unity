#nullable enable
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;
using UnityEngine;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Tests.Shared;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks the production seed rejection, shovel targeting, and native shadow placement paths.</summary>
    public sealed class BoardInteractionTests
    {
        /// <summary>Owns an inactive game coordinator without loading player data or booting a scene.</summary>
        private GameObject _object = null!;
        /// <summary>Routes the same clicks and target queries used during gameplay.</summary>
        private PvZGame _owner = null!;

        /// <summary>Creates a playing board with its earned seeds and no persistence dependencies.</summary>
        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("Board interaction regression");
            _object.SetActive(false);
            _owner = _object.AddComponent<PvZGame>();
            _owner._game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 9151);
            Assert.That(_owner._game.ChooseDefaultAdventureSeeds(), Is.True);
            _owner._game.ResetAdventureWaveSchedule(100000, false, false);
            _owner._deck.Clear();
            _owner._deck.AddRange(_owner._game.Adventure.SelectedSeeds);
        }

        /// <summary>Releases only the inactive coordinator created by this fixture.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_object);
        }

        /// <summary>Rejects an unaffordable ready seed and flashes red on the original twenty-tick cadence.</summary>
        [Test]
        public void UnaffordableSeedStartsSeventyTickSunWarning()
        {
            _owner._deck.Clear();
            _owner._deck.Add(PlantKind.Repeater);
            var cooldowns = (int[])SimulationInspection.GetField("_seedCooldownTicks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_owner._game)!;
            cooldowns[(int)PlantKind.Repeater] = 0;
            Click(_owner.Layout.AdventureSeedRect(0).center);
            Assert.That(_owner._selected, Is.EqualTo(-1));
            Assert.That(_owner.AdventureBoard.SunMoneyWarningRed, Is.False);
            _owner.RequiredGame.Adventure._ticks++;
            Assert.That(_owner.AdventureBoard.SunMoneyWarningRed, Is.True);
            _owner.RequiredGame.TogglePause();
            _owner.RequiredGame.Update(.25f);
            Assert.That(_owner.AdventureBoard.SunMoneyWarningRed, Is.True, "Paused boards retain the native warning phase.");
            _owner.RequiredGame.TogglePause();
            _owner.RequiredGame.Adventure._ticks += 69;
            Assert.That(_owner.AdventureBoard.SunMoneyWarningRed, Is.False);
        }

        /// <summary>Preserves the native priority of cooldown rejection over an insufficient-sun warning.</summary>
        [Test]
        public void CoolingSeedDoesNotStartSunWarning()
        {
            _owner._deck.Clear();
            _owner._deck.Add(PlantKind.Repeater);
            var cooldowns = (int[])SimulationInspection.GetField("_seedCooldownTicks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_owner._game)!;
            cooldowns[(int)PlantKind.Repeater] = 25;
            Click(_owner.Layout.AdventureSeedRect(0).center);
            _owner.RequiredGame.Adventure._ticks += 11;
            Assert.That(_owner.AdventureBoard.SunMoneyWarningRed, Is.False);
            Assert.That(_owner._selected, Is.EqualTo(-1));
        }

        /// <summary>Uses the pumpkin rim for shell removal while its center targets the contained plant.</summary>
        [Test]
        public void ShovelPreviewAndClickChooseTheSamePlantLayer()
        {
            var main = new Plant { Kind = PlantKind.Wallnut, Layer = PlantLayer.Main, Row = 2, Column = 3, Health = 4000 };
            var shell = new Plant { Kind = PlantKind.Pumpkin, Layer = PlantLayer.Armor, Row = 2, Column = 3, Health = 4000 };
            SimulationFixture.Plants(_owner.RequiredGame).Add(main);
            SimulationFixture.Plants(_owner.RequiredGame).Add(shell);
            var center = new Vector2(BoardLayout.AdventureGridX + 3 * BoardLayout.CellWidth + 40, _owner.Layout.AdventureCellY(2, 3) + 40);
            Assert.That(_owner.Layout.AdventureShovelTarget(center), Is.SameAs(main));
            var rim = center + new Vector2(35, 0);
            Assert.That(_owner.Layout.AdventureShovelTarget(rim), Is.SameAs(shell));
            _owner._shovel = true;
            Click(rim);
            Assert.That(_owner.RequiredGame.Plants.Contains(shell), Is.False);
            Assert.That(_owner.RequiredGame.Plants.Contains(main), Is.True);
            Assert.That(_owner._shovel, Is.False);
        }

        /// <summary>Does not highlight or remove a grave buster while its original action is active.</summary>
        [Test]
        public void GraveBusterIsExcludedFromTheShovelTarget()
        {
            var plant = new Plant { Kind = PlantKind.GraveBuster, Layer = PlantLayer.Main, Row = 2, Column = 3, Health = 300 };
            SimulationFixture.Plants(_owner.RequiredGame).Add(plant);
            Assert.That(_owner.Layout.AdventureShovelTarget(new Vector2(320, 320)), Is.Null);
            Assert.That(_owner.RequiredGame.TryShovelAdventurePlant(plant), Is.False);
            Assert.That(_owner.RequiredGame.Plants.Contains(plant), Is.True);
        }

        /// <summary>Moves the original centered shadow with the same interpolated native coordinate as the body.</summary>
        [Test]
        public void ShadowUsesNativePositionAndImageDimensions()
        {
            var zombie = new Zombie { Kind = ZombieKind.Normal };
            var first = GamePresenter.AdventureZombieShadowBounds(zombie, 5f, 280f, new Vector2(86, 36), false);
            Assert.That(first.position, Is.EqualTo(new Vector2(391f, 342f)));
            Assert.That(first.center, Is.EqualTo(new Vector2(434f, 360f)));
            Assert.That(first.size, Is.EqualTo(new Vector2(86, 36)));
            zombie.DropRemaining = 2f;
            var next = GamePresenter.AdventureZombieShadowBounds(zombie, 5.125f, 280f, new Vector2(86, 36), false);
            Assert.That(next.center.x - first.center.x, Is.EqualTo(10f));
            Assert.That(next.center.y, Is.EqualTo(first.center.y), "A descending body keeps its ground shadow on the lawn.");
        }

        /// <summary>Checks the native image helper scales around the original image center rather than the supplied origin.</summary>
        [Test]
        public void SmallZombieShadowKeepsTheUnscaledImageCenter()
        {
            var zombie = new Zombie { Kind = ZombieKind.Normal, IsSmall = true };
            var bounds = GamePresenter.AdventureZombieShadowBounds(zombie, 5f, 280f, new Vector2(86, 36), false);
            Assert.That(bounds.position, Is.EqualTo(new Vector2(416f, 351f)));
            Assert.That(bounds.size, Is.EqualTo(new Vector2(43f, 18f)));
            Assert.That(bounds.center, Is.EqualTo(new Vector2(437.5f, 360f)));
        }

        /// <summary>Preserves the original water, vehicle, and transient-phase shadow exclusions.</summary>
        [Test]
        public void ShadowExcludesNativeWaterVehicleAndVaultStates()
        {
            var zombie = new Zombie { Kind = ZombieKind.Normal };
            Assert.That(GamePresenter.HasAdventureZombieShadow(zombie, false), Is.True);
            Assert.That(GamePresenter.HasAdventureZombieShadow(zombie, true), Is.False);
            foreach (var kind in new[] { ZombieKind.Zomboni, ZombieKind.Catapult, ZombieKind.Boss })
            {
                zombie.Kind = kind;
                Assert.That(GamePresenter.HasAdventureZombieShadow(zombie, false), Is.False);
            }

            zombie.Kind = ZombieKind.Normal;
            foreach (var phase in new[] { 1, 33, 34, 35, 32, 14, 50, 18, 12, 52, 58 })
            {
                zombie.PresentationNativePhase = phase;
                Assert.That(GamePresenter.HasAdventureZombieShadow(zombie, false), Is.False);
            }
        }

        /// <summary>Checks held bank cards plant through overlapping drops, retaining selection after a rejected placement.</summary>
        /// <param name="independentMode">Whether the click uses the survival controller instead of adventure.</param>
        /// <param name="occupied">Whether an existing plant rejects the placement.</param>
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void HeldSeedClicksDoNotCollectDrops(bool independentMode, bool occupied)
        {
            ConfigurePickupBoard(independentMode);
            if (occupied)
            {
                SimulationFixture.Plants(_owner.RequiredGame).Add(new Plant
                {
                    Id = 95, Kind = PlantKind.Wallnut, Row = 2, Column = 3, Layer = PlantLayer.Main, Health = 4000f
                });
            }

            ClickBoard(independentMode ? _owner.IndependentModes.ModeSeedRect(0).center : _owner.Layout.AdventureSeedRect(0).center);
            Assert.That(_owner._selected, Is.EqualTo(0));
            var point = AddOverlappingDrops();
            ClickBoard(point);
            AssertDropsRemain();
            Assert.That(_owner._selected, Is.EqualTo(occupied ? 0 : -1));
            Assert.That(_owner.RequiredGame.Plants[0].Kind, Is.EqualTo(occupied ? PlantKind.Wallnut : PlantKind.Peashooter));
            _owner.Router.CancelBoardSelection();
            AssertDropsCanBeCollected(point);
        }

        /// <summary>Checks a real cannon selection fires through overlapping drops and restores ordinary pickup input afterward.</summary>
        /// <param name="independentMode">Whether the click uses the survival controller instead of adventure.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void CannonAimClicksFireInsteadOfCollectingDrops(bool independentMode)
        {
            ConfigurePickupBoard(independentMode);
            var cannon = new Plant
            {
                Id = 73, Kind = PlantKind.CobCannon, Row = 1, Column = 1, ColumnSpan = 2,
                Layer = PlantLayer.Main, Health = 300f, CannonPhase = CobCannonPhase.Ready
            };
            SimulationFixture.Plants(_owner.RequiredGame).Add(cannon);
            ClickBoard(new Vector2(160f, 200f));
            Assert.That(_owner.RequiredGame.Adventure.SelectedCobCannonId, Is.EqualTo(cannon.Id));
            var point = AddOverlappingDrops();
            ClickBoard(point);
            AssertDropsRemain();
            Assert.That(cannon.CannonPhase, Is.EqualTo(CobCannonPhase.Firing));
            Assert.That(_owner.RequiredGame.Adventure.SelectedCobCannonId, Is.Null);
            AssertDropsCanBeCollected(point);
        }

        /// <summary>Checks carried conveyor packets obey the same pickup restriction as bank cards.</summary>
        [Test]
        public void HeldPacketClicksPlantInsteadOfCollectingDrops()
        {
            ConfigurePickupBoard(false);
            var packet = new AdventurePacket { Id = 7, Kind = PlantKind.Peashooter };
            _owner.RequiredGame.Adventure._packets.Add(packet);
            _owner.AdventureBoard.SelectAdventurePacket(packet);
            var point = AddOverlappingDrops();
            ClickBoard(point);
            AssertDropsRemain();
            Assert.That(_owner.RequiredGame.Plants[0].Kind, Is.EqualTo(PlantKind.Peashooter));
            Assert.That(_owner.AdventureBoard._selectedAdventurePacket, Is.EqualTo(-1));
            AssertDropsCanBeCollected(point);
        }

        /// <summary>Checks the legacy board also preserves sun beneath a held seed.</summary>
        [Test]
        public void LegacyHeldSeedDoesNotCollectSun()
        {
            _owner._game = new GameSimulation(new GameRules(), 9151);
            _owner._currentView = PvZGame.ScreenView.Board;
            _owner._deck.Clear();
            _owner._deck.Add(PlantKind.Peashooter);
            _owner._selected = 0;
            var sun = new global::PlantsVsZombies.Simulation.Sun { X = 3.5f, Y = 2.4f };
            SimulationFixture.Suns(_owner.RequiredGame).Add(sun);
            ClickBoard(BoardLayout.SunRect(sun).center);
            Assert.That(_owner.RequiredGame.Suns.Contains(sun), Is.True);
            Assert.That(_owner.RequiredGame.Plants, Has.Count.EqualTo(1));
        }

        /// <summary>Prepares an unlocked bank and playing board without persistence or scene dependencies.</summary>
        /// <param name="independentMode">Whether to create a daytime endless session.</param>
        private void ConfigurePickupBoard(bool independentMode)
        {
            if (independentMode)
            {
                var session = new ModeSession(11, AdventureSeedAccess.DevelopmentAll());
                _owner.IndependentModes._modeSession = session;
                _owner._game = session.Game;
                Assert.That(session.Game.ChooseDefaultAdventureSeeds(), Is.True);
                session.Phase = ModePhase.Playing;
            }

            _owner._currentView = PvZGame.ScreenView.Board;
            _owner._choosingSeeds = false;
            _owner._selected = -1;
            _owner._deck.Clear();
            _owner._deck.Add(PlantKind.Peashooter);
            _owner.RequiredGame.Adventure.SelectedSeeds = new[] { PlantKind.Peashooter };
            typeof(GameSimulation).GetProperty(nameof(GameSimulation.SunAmount))!.SetValue(_owner.RequiredGame, 1000);
            _owner.RequiredGame.ResetAdventureWaveSchedule(100000, false, false);
            SimulationFixture.Plants(_owner.RequiredGame).Clear();
            var cooldowns = (int[])SimulationInspection.GetField("_seedCooldownTicks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_owner.RequiredGame)!;
            cooldowns[(int)PlantKind.Peashooter] = 0;
        }

        /// <summary>Overlaps a sun and a gold coin at a legal planting and cannon target cell.</summary>
        /// <returns>The logical click point inside both pickups.</returns>
        private Vector2 AddOverlappingDrops()
        {
            var sun = new global::PlantsVsZombies.Simulation.Sun { X = 3.5f, Y = 2.4f };
            SimulationFixture.Suns(_owner.RequiredGame).Add(sun);
            var point = _owner.Layout.AdventureSunRect(sun).center;
            var coin = _owner.RequiredGame.RequiredSpawnAdventureCoin(AdventureCoinKind.Gold, 3.5f, 2f);
            coin.PresentationX = point.x - 20f;
            coin.PresentationY = point.y - 20f;
            return point;
        }

        /// <summary>Checks both pickups stayed on the board without entering collection.</summary>
        private void AssertDropsRemain()
        {
            Assert.That(_owner.RequiredGame.Suns, Has.Count.EqualTo(1));
            Assert.That(_owner.RequiredGame.Suns[0].PresentationPickup.Collecting, Is.False);
            Assert.That(_owner.RequiredGame.Adventure.Coins, Has.Count.EqualTo(1));
            Assert.That(_owner.RequiredGame.Adventure.Coins[0].IsBeingCollected, Is.False);
        }

        /// <summary>Checks ordinary clicks collect the preserved coin and then the sun in existing priority order.</summary>
        /// <param name="point">The shared pickup point after the cursor returns to normal.</param>
        private void AssertDropsCanBeCollected(Vector2 point)
        {
            ClickBoard(point);
            Assert.That(_owner.RequiredGame.Adventure.Coins[0].IsBeingCollected, Is.True);
            Assert.That(_owner.RequiredGame.Suns, Has.Count.EqualTo(1));
            Assert.That(_owner.RequiredGame.Suns[0].PresentationPickup.Collecting, Is.False);
            ClickBoard(point);
            Assert.That(_owner.RequiredGame.Suns[0].PresentationPickup.Collecting, Is.True);
        }

        /// <summary>Routes a click through the production board dispatcher for the current game type.</summary>
        /// <param name="point">The logical eight-hundred-by-six-hundred pointer coordinate.</param>
        private void ClickBoard(Vector2 point)
        {
            typeof(GameInputRouter).GetMethod("HandleClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(_owner.Router, new object[] { point });
        }

        /// <summary>Routes one logical click directly through the production adventure input handler.</summary>
        /// <param name="point">The original eight-hundred-by-six-hundred pointer coordinate.</param>
        private void Click(Vector2 point)
        {
            GameMemberReflection.Invoke(_owner, "HandleAdventureClick", new object[] { point });
        }
    }
}
