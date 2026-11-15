#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using PlantsVsZombies.Tests.Shared;
using static UnityEngine.Object;
using System.Diagnostics.CodeAnalysis;

namespace PlantsVsZombies.Tests.PlayMode.Smoke
{
    /// <summary>Defines progression player smoke.</summary>
    public sealed class ProgressionPlayerSmoke : PlayerSmokeFixture
    {
        /// <summary>Keeps the loading presentation visible until the scenario explicitly completes it.</summary>
        protected override bool ShowLoading => true;

        /// <summary>Stores controller.</summary>
        private PvZGame? _controller;

        /// <summary>Gets the controller, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private PvZGame RequiredController => _controller ?? throw new System.InvalidOperationException("The member '_controller' is not available.");
        /// <summary>Stores logged error.</summary>
        private string? _loggedError;
        /// <summary>Stores checks.</summary>
        private int _checks;
        /// <summary>Stores private.</summary>
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        /// <summary>Performs field.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting field.</returns>
        private object Field(string name)
        {
            return GameMemberReflection.GetField(RequiredController, name);
        }

        /// <summary>Performs set.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "value">The value.</param>
        private void Set(string name, object value)
        {
            GameMemberReflection.SetField(RequiredController, name, value);
        }

        /// <summary>Performs call.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "args">The args.</param>
        /// <returns>The resulting call.</returns>
        private object Call(string name, params object[] args)
        {
            return GameMemberReflection.Invoke(RequiredController, name, args);
        }

        /// <summary>Performs view.</summary>
        /// <param name = "view">The view.</param>
        private void View(string view)
        {
            var field = GameMemberReflection.FindField(RequiredController, "_currentView", out var owner) ?? throw new MissingMemberException(nameof(PvZGame), "_currentView");
            field.SetValue(owner, Enum.Parse(field.FieldType, view));
        }

        /// <summary>Gets or sets sim.</summary>
        private GameSimulation Sim
        {
            get
            {
                return (GameSimulation)Field("_game");
            }
        }

        /// <summary>Performs check.</summary>
        /// <param name = "passed">The passed.</param>
        /// <param name = "message">The message.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        private void Check([DoesNotReturnIf(false)] bool passed, string message)
        {
            _checks++;
            if (!passed)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>Runs the migrated scenario through the PlayMode Test Runner.</summary>
        /// <returns>The original scenario's frame, rendering, and audio waits.</returns>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator RunsScenario()
        {
            _controller = Controller;
            yield return Run();
        }

        /// <summary>Performs capture.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting capture.</returns>
        private IEnumerator Capture(string name)
        {
            yield return SmokeTestArtifacts.WaitForFrame();
            SmokeTestArtifacts.CaptureFrame(Path.Combine(SmokeTestArtifacts.DirectoryPath, name + ".png"));
        }

        /// <summary>Performs run.</summary>
        /// <returns>The resulting run.</returns>
        private IEnumerator Run()
        {
            Set("_smokeMode", false);
            yield return new WaitForSecondsRealtime(.8f);
            yield return Capture("00-original-loading");
            var deadline = Time.unscaledTime + 60f;
            while ((!RequiredController.Loading.OriginalLoadingReady || (float)Field("_originalLoadWidth") < 313.99f || (int)Field("_originalTitleTicks") < 500) && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            Check(RequiredController.Loading.OriginalLoadingReady && (float)Field("_originalLoadWidth") >= 313.99f, "Original loading finishes real progress and title animation");
            yield return Capture("00b-original-title-ready");
            Set("_smokeMode", true);
            Call("CompleteOriginalLoading", false);
            Check(!RequiredController.Loading.OriginalLoadingActive && RequiredController.Music.DynamicAudioReady, "Original loading completes after real resources");
            Check((bool)Call("EnsureAdventureStore") && (bool)Call("EnsurePlayerAccount"), "Isolated stores initialized");
            var catalog = (ProfileCatalog)Field("_profiles");
            var id = catalog.RequiredSelectedId;
            var accounts = (PlayerAccountStore)Field("_accountStore");
            var campaign = (AdventureProgressStore)Field("_adventureStore");
            var purchases = new int[80];
            purchases[13] = 1;
            purchases[14] = 5;
            purchases[15] = 5;
            purchases[16] = 1;
            purchases[17] = 1;
            purchases[18] = 1;
            purchases[19] = 1;
            purchases[20] = 1;
            purchases[25] = 1;
            purchases[27] = 1;
            purchases[28] = 5;
            var utc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var plants = new List<GardenPlant>
            {
                new GardenPlant
                {
                    Id = Guid.NewGuid(),
                    Kind = PlantKind.Sunflower,
                    Slot = 0,
                    Growth = 3,
                    FeedingsPerGrow = 3
                },
                new GardenPlant
                {
                    Id = Guid.NewGuid(),
                    Kind = PlantKind.Marigold,
                    Slot = 10,
                    Growth = 0,
                    FeedingsPerGrow = 3
                },
                new GardenPlant
                {
                    Id = Guid.NewGuid(),
                    Kind = PlantKind.FumeShroom,
                    Garden = GardenKind.Mushroom,
                    Slot = 2,
                    Growth = 3,
                    FeedingsPerGrow = 3
                },
                new GardenPlant
                {
                    Id = Guid.NewGuid(),
                    Kind = PlantKind.TangleKelp,
                    Garden = GardenKind.Aquarium,
                    Slot = 3,
                    Growth = 3,
                    FeedingsPerGrow = 3
                }
            };
            accounts.Import(id, 20000, purchases, new int[100], 25, plants);
            campaign.Import(id, 8, 1);
            Call("OpenStore");
            yield return Capture("01-store-first-page");
            for (var i = 1; i < 4; i++)
            {
                Set("_shopPage", i);
                yield return Capture("02-store-page-" + i);
            }

            var before = accounts.Get(id).Coins;
            Set("_shopPage", 0);
            Call("HandleClick", new Vector2(407, 350));
            yield return Capture("02b-store-purchase");
            Call("HandleClick", new Vector2(310, 369));
            Check(accounts.Get(id).Coins == before - 500 && accounts.Get(id).Owns(PlantKind.GatlingPea), "Actual purchased upgrade entitlement");
            Call("OpenZenGarden");
            yield return Capture("03-garden-greenhouse");
            Set("_gardenKind", GardenKind.Mushroom);
            yield return Capture("04-garden-mushroom");
            Set("_gardenKind", GardenKind.Aquarium);
            yield return Capture("05-garden-aquarium");
            Call("OpenWisdomTree");
            yield return Capture("06-wisdom-tree");
            accounts.FeedWisdomTree(id);
            Check(accounts.Get(id).TreeHeight == 26, "Tree feed persisted");
            Call("OpenModes", 1);
            yield return Capture("07-minigames-directory");
            Check((bool)Call("StartIndependentMode", 16), "Independent mode launches");
            var mode = Sim.RequiredModeSession;
            Check(mode.ChooseSeeds(Sim.Adventure.SelectedSeeds), "Mode accepts real loadout");
            Set("_choosingSeeds", false);
            mode.Update(28f);
            yield return Capture("08-independent-mode");
            View("Selector");
            Check((bool)Call("StartAdventureCampaign"), "Replay launches from normal menu");
            Check(Sim.Adventure.Level.IsReplay && Sim.Adventure.RequiredSeeds.Count == 3 && Sim.Adventure.AvailableSeeds.Count == 41, "Replay and owned seeds wired");
            yield return Capture("09-replay-chooser");
            RequiredController.Campaign.StartAdventureTestLevel(8, accounts.Get(id).SeedAccess());
            Check(Sim.ChooseDefaultAdventureSeeds(), "Test board seed chooser");
            Set("_choosingSeeds", false);
            var plantMethod = SimulationInspection.GetMethod("PlaceAdventurePlant", Private);
            var peashooter = (Plant)plantMethod.Invoke(Sim, new object[] { PlantKind.Peashooter, 2, 2 });
            peashooter.Timing = global::PlantsVsZombies.Timing.PlantTiming.FromRaw(global::PlantsVsZombies.Timing.PlantKind.Peashooter, 1, 0);
            var sunflower = (Plant)plantMethod.Invoke(Sim, new object[] { PlantKind.Sunflower, 1, 1 });
            sunflower.Timing = global::PlantsVsZombies.Timing.PlantTiming.FromRaw(global::PlantsVsZombies.Timing.PlantKind.Sunflower, 1, 0);
            plantMethod.Invoke(Sim, new object[] { PlantKind.CabbagePult, 3, 2 });
            var spawn = SimulationInspection.GetMethod("SpawnAdventureZombie", Private);
            var zombie = (Zombie)spawn.Invoke(Sim, new object[] {
                ZombieKind.Normal,
                2,
                6f,
                0
            });
            spawn.Invoke(Sim, new object[] {
                ZombieKind.Buckethead,
                3,
                7f,
                0
            });
            yield return new WaitForSecondsRealtime(.6f);
            yield return Capture("10-board-animations");
            SimulationInspection.GetMethod("HitAdventureZombie", Private).Invoke(Sim, new object[] {
                zombie,
                2000f,
                false,
                0U
            });
            yield return new WaitForSecondsRealtime(.12f);
            yield return Capture("11-zombie-death");
            yield return new WaitForSecondsRealtime(.4f);
            yield return Capture("12-projectiles-and-sun");
            Check(_loggedError == null, "Player rendered all inspected pages without errors");
        }
    }
}
