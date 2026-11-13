#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Simulation;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original box opening, blast targets, money falling, and temporary wallet visibility.</summary>
    public sealed class AdventureJackAndCoinTests
    {
        /// <summary>Checks native drop gravity, landing, and interruption by an actual pickup.</summary>
        /// <param name="kind">The original silver or gold denomination.</param>
        [TestCase(AdventureCoinKind.Silver)]
        [TestCase(AdventureCoinKind.Gold)]
        public void MoneyDropsRisesFallsAndLandsBeforeCollection(AdventureCoinKind kind)
        {
            var game = Board();
            var coin = game.RequiredSpawnAdventureCoin(kind, 4f, 2f);
            var startY = coin.PresentationY;
            var startX = coin.PresentationX;
            var velocityY = coin.FallVelocityY;
            var velocityX = coin.FallVelocityX;
            Assert.That(velocityY, Is.InRange(-5f, -3f));
            Assert.That(coin.GroundY, Is.InRange(startY + 45f, startY + 64f));
            Assert.That(coin.AnimationRate, Is.InRange(.6f, 1f));
            game.Update(.01f);
            Assert.That(coin.PresentationY, Is.EqualTo(startY + velocityY).Within(.0001f));
            Assert.That(coin.PresentationX, Is.EqualTo(startX + velocityX).Within(.0001f));
            Assert.That(coin.PresentationPreviousY, Is.EqualTo(startY));
            Assert.That(coin.FallVelocityY, Is.EqualTo(velocityY + .15f).Within(.0001f));
            game.TogglePause();
            game.Update(.1f);
            Assert.That(coin.PresentationY, Is.EqualTo(startY + velocityY).Within(.0001f));
            game.TogglePause();
            for (var tick = 0; tick < 100 && !coin.HitGround; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(coin.HitGround, Is.True);
            Assert.That(coin.PresentationY, Is.EqualTo(coin.GroundY));
            Assert.That(coin.PresentationX, Is.EqualTo(Mathf.Round(coin.PresentationX)));
            var ground = coin.PresentationY;
            game.Update(.1f);
            Assert.That(coin.PresentationY, Is.EqualTo(ground));
            Assert.That(game.CollectAdventureCoin(coin), Is.True);
            game.Update(.01f);
            Assert.That(coin.PresentationY, Is.EqualTo(ground + (558f - ground) / 21f).Within(.0001f));
            var edge = game.RequiredSpawnAdventureCoin(kind, 4f, 4f);
            Assert.That(edge.GroundY, Is.LessThanOrEqualTo(521f), "PC 43B610 clamps ground to 521, rather than the mobile port's 540.");
        }

        /// <summary>Checks the original stopped 110-tick pop and its single 80-tick surprise cue.</summary>
        [Test]
        public void BoxOpensAndExplodesAfterOriginalStoppedDelayWithoutLoot()
        {
            var game = Board();
            var jack = Spawn(game, ZombieKind.JackInTheBox, 4f);
            Assert.That(jack.JackInTheBoxTicks, Is.InRange(440, 2270));
            jack.JackInTheBoxTicks = 1;
            var x = jack.X;
            game.Update(.01f);
            Assert.That(jack.PresentationNativePhase, Is.EqualTo(16));
            Assert.That(jack.JackInTheBoxTicks, Is.EqualTo(110));
            Assert.That(game.AdventurePresentationZombieAnimation(jack), Is.EqualTo("anim_pop"));
            Assert.That(game.AdventurePresentationZombieRate(jack), Is.EqualTo(28f));
            Assert.That(AdventureZombieMotionRules.PlaysOnce(jack), Is.True);
            Assert.That(jack.X, Is.EqualTo(x));
            jack.ChillRemaining = 2f;
            for (var tick = 0; tick < 30; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(jack.JackInTheBoxTicks, Is.EqualTo(80), "Chilling slows walking, never the box phase counter.");
            Assert.That(jack.ChillRemaining, Is.LessThan(2f));
            Assert.That(game.Presentation.ZombieSounds.FindAll(cue => cue.Foley == 34), Has.Count.EqualTo(1));
            for (var tick = 0; tick < 79; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(game.Zombies, Has.Member(jack));
            Assert.That(jack.X, Is.EqualTo(x));
            game.Update(.01f);
            Assert.That(game.Zombies, Has.No.Member(jack));
            Assert.That(jack.PresentationDeath, Is.EqualTo(AdventureZombieDeathPresentation.Removed));
            Assert.That(game.Presentation.Corpses, Is.Empty);
            Assert.That(game.Adventure.Coins, Is.Empty);
            Assert.That(game.Presentation.Explosions.Exists(effect => effect.Particle == "JackExplode"), Is.True);
            Assert.That(game.Presentation.ZombieSounds.FindAll(cue => cue.Foley == 23), Has.Count.EqualTo(1));
        }

        /// <summary>Checks original hostile and hypnotized blast masks while preserving the opposite faction.</summary>
        /// <param name="hypnotized">Whether the box carrier fights for the player.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void BoxBlastUsesOriginalFactionMaskAndPlantCircle(bool hypnotized)
        {
            var game = Board();
            var jack = Spawn(game, ZombieKind.JackInTheBox, 4f);
            jack.Hypnotized = hypnotized;
            jack.PresentationNativePhase = 16;
            jack.JackInTheBoxTicks = 1;
            var normal = Spawn(game, ZombieKind.Normal, 4.1f);
            var friendly = Spawn(game, ZombieKind.Normal, 4.2f);
            friendly.Hypnotized = true;
            var near = new Plant { Id = 1, Kind = PlantKind.Peashooter, Row = 2, Column = 4, Health = 300f };
            var far = new Plant { Id = 2, Kind = PlantKind.Peashooter, Row = 2, Column = 7, Health = 300f };
            game.Plants.Add(near);
            game.Plants.Add(far);
            Invoke(game, "AdvanceAdventureZombies", .01f);
            Assert.That(game.Zombies.Contains(normal), Is.EqualTo(!hypnotized));
            Assert.That(game.Zombies.Contains(friendly), Is.EqualTo(hypnotized));
            Assert.That(game.Plants.Contains(near), Is.EqualTo(hypnotized));
            Assert.That(game.Plants, Has.Member(far));
        }

        /// <summary>Checks head loss, freeze and magnetic disarming cannot accidentally trigger a box pop.</summary>
        [Test]
        public void BoxCountdownStopsWhenFrozenAndCannotOpenAfterHeadOrBoxLoss()
        {
            var game = Board();
            var jack = Spawn(game, ZombieKind.JackInTheBox, 7f);
            jack.JackInTheBoxTicks = 1;
            jack.FrozenRemaining = .1f;
            game.Update(.01f);
            Assert.That(jack.JackInTheBoxTicks, Is.EqualTo(1));
            jack.FrozenRemaining = 0f;
            jack.Headless = true;
            game.Update(.01f);
            Assert.That(jack.PresentationNativePhase, Is.EqualTo(15));
            jack.Headless = false;
            jack.UsedAbility = true;
            jack.PresentationNativePhase = 0;
            game.Update(.02f);
            Assert.That(jack.PresentationNativePhase, Is.Zero);
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(jack, "zombie_jackbox_box"), Is.False);
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(jack, "zombie_jackbox_handle"), Is.False);
        }

        /// <summary>Checks scored money cannot pin the wallet on-screen or carry its timer into another board.</summary>
        [Test]
        public void WalletExpiresAndDoesNotLeakIntoNextBoard()
        {
            var host = new GameObject("Inactive wallet timing fixture");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var presenter = new GamePresenter(owner);
            try
            {
                owner._game = Board();
                owner._game.Adventure.MoneyCollectedUnits = 5;
                presenter.ShowWalletUntil(.2f);
                Assert.That(presenter.WalletAlpha, Is.EqualTo(1f));
                owner._game.Update(.1f);
                Assert.That(presenter.WalletAlpha, Is.InRange(.65f, .68f));
                owner._game.Update(.11f);
                Assert.That(presenter.WalletAlpha, Is.Zero);
                presenter.ShowWalletUntil(10f);
                owner._game = Board();
                Assert.That(presenter.WalletAlpha, Is.Zero);
                owner._presenter = presenter;
                var handler = typeof(AdventureCampaignController).GetMethod("AdventureNotice", BindingFlags.Instance | BindingFlags.NonPublic)!;
                owner._game.Adventure.NoticeRaised = notice => handler.Invoke(owner.Campaign, new object[] { notice });
                var coin = owner._game.RequiredSpawnAdventureCoin(AdventureCoinKind.Gold, 4f, 2f);
                Assert.That(owner._game.CollectAdventureCoin(coin), Is.True);
                owner._game.Update(1.5f);
                Assert.That(owner._game.Adventure.MoneyCollectedUnits, Is.EqualTo(5));
                owner._game.Update(8.6f);
                Assert.That(presenter.WalletAlpha, Is.Zero, "Arrival scoring must not restart the original ten-second timer from the click.");
            }
            finally
            {
                presenter.Dispose();
                Object.DestroyImmediate(host);
            }
        }

        /// <summary>Creates a production daytime board whose unrelated wave scheduling is held.</summary>
        /// <returns>A playing board without plants or automatic zombie spawns.</returns>
        private static GameSimulation Board()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 18 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            return game;
        }

        /// <summary>Creates an actor through the normal production spawn boundary.</summary>
        /// <param name="game">The board receiving the actor.</param>
        /// <param name="kind">The original zombie species.</param>
        /// <param name="x">The explicit lawn position.</param>
        /// <returns>The live spawned zombie.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind, float x)
        {
            return (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { kind, 2, x, 5 })!;
        }

        /// <summary>Calls the production fixed-tick boundary without unrelated plant attacks.</summary>
        /// <param name="game">The simulation being exercised.</param>
        /// <param name="name">The private production method.</param>
        /// <param name="arguments">The original boundary arguments.</param>
        private static void Invoke(GameSimulation game, string name, params object[] arguments)
        {
            typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
