#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Game;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
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
    /// <summary>Defines feedback player smoke.</summary>
    public sealed class FeedbackPlayerSmoke : PlayerSmokeFixture
    {
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
        /// <summary>Gets or sets sim.</summary>
        private GameSimulation Sim
        {
            get
            {
                return (GameSimulation)Field("_game");
            }
        }

        /// <summary>Performs field.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting field.</returns>
        private object Field(string name)
        {
            return GameMemberReflection.GetField(RequiredController, name);
        }

        /// <summary>Performs call.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "args">The args.</param>
        /// <returns>The resulting call.</returns>
        private object Call(string name, params object[] args)
        {
            var method = GameMemberReflection.FindMethod(RequiredController, name, out var owner);
            if (method != null)
            {
                return method.Invoke(owner, args);
            }

            var presenter = GameMemberReflection.GetField(RequiredController, "_presenter");
            return presenter.GetType().GetMethod(name, Private).Invoke(presenter, args);
        }

        /// <summary>Performs sim call.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "args">The args.</param>
        /// <returns>The resulting sim call.</returns>
        private object SimCall(string name, params object[] args)
        {
            var method = SimulationInspection.GetMethod(name, Private);
            var parameters = method.GetParameters(Sim);
            if (args.Length < parameters.Length)
            {
                var supplied = args.Length;
                Array.Resize(ref args, parameters.Length);
                for (var i = supplied; i < parameters.Length; i++)
                {
                    args[i] = parameters[i].DefaultValue;
                }
            }

            return method.Invoke(Sim, args);
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
            var deadline = Time.unscaledTime + 60;
            while (RequiredController.Loading.OriginalLoadingActive && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            Check(!RequiredController.Loading.OriginalLoadingActive, "Startup did not finish");
            RequiredController.Campaign.StartAdventureTestLevel(8);
            Check(Sim.ChooseDefaultAdventureSeeds(), "Could not choose test seeds");
            GameMemberReflection.SetField(RequiredController, "_choosingSeeds", false);
            yield return WaitForBattlePreparation();
            var sunflower = (Plant)SimCall("PlaceAdventurePlant", PlantKind.Sunflower, 1, 1);
            sunflower.Timing = global::PlantsVsZombies.Timing.PlantTiming.FromRaw(global::PlantsVsZombies.Timing.PlantKind.Sunflower, 150, 0);
            sunflower.ActionTimer = 1.5f;
            var twin = (Plant)SimCall("PlaceAdventurePlant", PlantKind.TwinSunflower, 2, 1);
            twin.ActionTimer = 1.5f;
            var shroom = (Plant)SimCall("PlaceAdventurePlant", PlantKind.SunShroom, 3, 1);
            shroom.Asleep = false;
            shroom.ActionTimer = 1.5f;
            var marigold = (Plant)SimCall("PlaceAdventurePlant", PlantKind.Marigold, 4, 1);
            marigold.ActionTimer = 1.5f;
            var zombie = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 1, 6.5f, 0);
            var door = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.ScreenDoor, 3, 6.6f, 0);
            yield return Capture("00-producers-before-hint");
            yield return new WaitForSecondsRealtime(.9f);
            Check(sunflower.ProducerGlow.FlashTicks > 0 && twin.ProducerGlow.FlashTicks > 0 && shroom.ProducerGlow.FlashTicks > 0 && marigold.ProducerGlow.FlashTicks > 0, "Actual production countdown did not start original hint");
            yield return Capture("01-producers-brightening");
            SimCall("HitAdventureZombie", zombie, 100f, false);
            SimCall("HitAdventureZombie", door, 20f, false);
            Check(zombie.PresentationArmLost && !zombie.Headless && Sim.Presentation.ArmDrops.Count > 0, "Body-health threshold did not detach arm");
            Check(door.PresentationArmorFlash > 0 && door.PresentationFlash == 0, "Door hit also flashed the body");
            yield return Capture("02-arm-drop-and-hit");
            yield return new WaitForSecondsRealtime(.1f);
            yield return Capture("03-hit-decay");
            yield return new WaitForSecondsRealtime(.6f);
            Check(Sim.Suns.Count >= 4 && marigold.ActionCount > 0, "Real sun/coin production did not follow its hint");
            yield return Capture("04-production-complete");
            var movement = new List<float>();
            for (var i = 0; i < 160; i++)
            {
                var before = zombie.X;
                Sim.Update(.01f);
                movement.Add(before - zombie.X);
            }

            float low = float.MaxValue, high = float.MinValue;
            foreach (float move in movement)
            {
                low = Math.Min(low, move);
                high = Math.Max(high, move);
            }

            Check(high - low > .0001f, "Zombie movement remained linear across the original walking cycle");
            File.WriteAllText(Path.Combine(SmokeTestArtifacts.DirectoryPath, "walk-displacements.txt"), string.Join("\n", movement));
            yield return Capture("05-walk-cycle");
            Call("HandleSelector", SelectorAction.Adventure);
            Check(Sim.IsAdventure && Sim.Adventure.Level.Number == 1, "Normal menu did not create campaign");
            var final = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 2, 5.4f, 0);
            SimCall("HitAdventureZombie", final, 2000f, false);
            // Force only the end-of-level condition to inspect the real reward UI without replaying a whole level.
            SimulationInspection.GetProperty("State").SetValue(Sim, MatchState.Won);
            Sim.Adventure.Phase = AdventurePhase.Reward;
            SimCall("SpawnAdventureRewardPresentation");
            var campaign = (AdventureCampaign)Field("_adventureCampaign");
            Check(campaign.Progress.NextLevel == 1, "A drop saved progress before pickup");
            yield return new WaitForSecondsRealtime(.12f);
            yield return Capture("06-seed-reward-falling");
            deadline = Time.unscaledTime + 5;
            while (!Sim.Adventure.RequiredRewardPresentation.OnGround && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            Check(Sim.Adventure.RequiredRewardPresentation.OnGround, "Seed reward did not land");
            yield return Capture("07-seed-reward-pickup-arrow");
            Call("HandleClick", new Vector2(50, 500));
            Check(campaign.Progress.NextLevel == 1, "Unrelated click collected the reward");
            var reward = Sim.Adventure.RequiredRewardPresentation;
            Call("HandleClick", new Vector2(reward.X + 25, reward.Y + 35));
            Check(reward.Stage == AdventureAwardStage.Collecting && campaign.Progress.NextLevel == 2, "Visible seed pickup did not persist and start original animation");
            yield return new WaitForSecondsRealtime(.8f);
            yield return Capture("08-seed-reward-collecting");
            deadline = Time.unscaledTime + 8;
            while ((reward.Stage != AdventureAwardStage.Introduction || reward.IntroductionTicks < 180) && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            Check(reward.Stage == AdventureAwardStage.Introduction, "Original pickup transition did not show introduction");
            Check(!string.IsNullOrEmpty((string)Call("OriginalProgressionText", "SUNFLOWER_TOOLTIP")), "Original plant tooltip missing");
            yield return Capture("09-original-seed-introduction");
            Call("HandleClick", new Vector2(402, 566));
            Check(Sim.Adventure.Level.Number == 2 && Sim.State == MatchState.Playing, "Original next button did not advance");
            yield return Capture("10-next-adventure-level");
            var progress = (AdventureProgressStore)Field("_adventureStore");
            progress.Import(campaign.ProfileId, 1, 1);
            Call("HandleSelector", SelectorAction.Adventure);
            Check(Sim.Adventure.Level.IsReplay && Sim.ChooseDefaultAdventureSeeds(), "Replay setup failed");
            GameMemberReflection.SetField(RequiredController, "_choosingSeeds", false);
            SimulationInspection.GetProperty("State").SetValue(Sim, MatchState.Won);
            Sim.Adventure.Phase = AdventurePhase.Reward;
            SimCall("SpawnAdventureRewardPresentation");
            reward = Sim.Adventure.RewardPresentation;
            if (reward == null)
            {
                throw new System.InvalidOperationException("The value 'reward' is not available.");
            }

            Call("HandleClick", new Vector2(reward.X + reward.Width / 2, reward.Y + reward.Height / 2));
            Check(reward.NativeType == 18 && reward.Stage == AdventureAwardStage.Collecting, "Replay did not award a moneybag");
            Sim.Update(6.1f);
            Call("UpdateAdventureRewardContinuation");
            Check(Sim.Adventure.Level.IsReplay && Sim.Adventure.Level.Number == 2, "Replay incorrectly displayed a new-plant award");
            yield return Capture("11-replay-native-continuation");
            Check(_loggedError == null, "Player reported an error");
        }
    }
}
