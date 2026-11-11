#nullable enable
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Simulation;
using UnityEngine;

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
            var cooldowns = (int[])typeof(GameSimulation).GetField("_seedCooldownTicks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_owner._game)!;
            cooldowns[(int)PlantKind.Repeater] = 0;
            Click(_owner.AdventureSeedRect(0).center);
            Assert.That(_owner._selected, Is.EqualTo(-1));
            Assert.That(_owner.SunMoneyWarningRed, Is.False);
            _owner._game.Adventure._ticks++;
            Assert.That(_owner.SunMoneyWarningRed, Is.True);
            _owner._game.TogglePause();
            _owner._game.Update(.25f);
            Assert.That(_owner.SunMoneyWarningRed, Is.True, "Paused boards retain the native warning phase.");
            _owner._game.TogglePause();
            _owner._game.Adventure._ticks += 69;
            Assert.That(_owner.SunMoneyWarningRed, Is.False);
        }

        /// <summary>Preserves the native priority of cooldown rejection over an insufficient-sun warning.</summary>
        [Test]
        public void CoolingSeedDoesNotStartSunWarning()
        {
            _owner._deck.Clear();
            _owner._deck.Add(PlantKind.Repeater);
            var cooldowns = (int[])typeof(GameSimulation).GetField("_seedCooldownTicks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_owner._game)!;
            cooldowns[(int)PlantKind.Repeater] = 25;
            Click(_owner.AdventureSeedRect(0).center);
            _owner._game.Adventure._ticks += 11;
            Assert.That(_owner.SunMoneyWarningRed, Is.False);
            Assert.That(_owner._selected, Is.EqualTo(-1));
        }

        /// <summary>Uses the pumpkin rim for shell removal while its center targets the contained plant.</summary>
        [Test]
        public void ShovelPreviewAndClickChooseTheSamePlantLayer()
        {
            var main = new Plant { Kind = PlantKind.Wallnut, Layer = PlantLayer.Main, Row = 2, Column = 3, Health = 4000 };
            var shell = new Plant { Kind = PlantKind.Pumpkin, Layer = PlantLayer.Armor, Row = 2, Column = 3, Health = 4000 };
            _owner._game.Plants.Add(main);
            _owner._game.Plants.Add(shell);
            var center = new Vector2(PvZGame.AdventureGridX + 3 * PvZGame.CellWidth + 40, _owner.AdventureCellY(2, 3) + 40);
            Assert.That(_owner.AdventureShovelTarget(center), Is.SameAs(main));
            var rim = center + new Vector2(35, 0);
            Assert.That(_owner.AdventureShovelTarget(rim), Is.SameAs(shell));
            _owner._shovel = true;
            Click(rim);
            Assert.That(_owner._game.Plants.Contains(shell), Is.False);
            Assert.That(_owner._game.Plants.Contains(main), Is.True);
            Assert.That(_owner._shovel, Is.False);
        }

        /// <summary>Does not highlight or remove a grave buster while its original action is active.</summary>
        [Test]
        public void GraveBusterIsExcludedFromTheShovelTarget()
        {
            var plant = new Plant { Kind = PlantKind.GraveBuster, Layer = PlantLayer.Main, Row = 2, Column = 3, Health = 300 };
            _owner._game.Plants.Add(plant);
            Assert.That(_owner.AdventureShovelTarget(new Vector2(320, 320)), Is.Null);
            Assert.That(_owner._game.TryShovelAdventurePlant(plant), Is.False);
            Assert.That(_owner._game.Plants.Contains(plant), Is.True);
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

        /// <summary>Routes one logical click directly through the production adventure input handler.</summary>
        /// <param name="point">The original eight-hundred-by-six-hundred pointer coordinate.</param>
        private void Click(Vector2 point)
        {
            typeof(PvZGame).GetMethod("HandleAdventureClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_owner, new object[] { point });
        }
    }
}
