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

namespace PlantsVsZombies.Diagnostics
{
    // Runs only in an explicitly requested, isolated Player smoke session.
    /// <summary>Defines adventure player smoke.</summary>
    internal sealed class AdventurePlayerSmoke : MonoBehaviour
    {
        /// <summary>Stores controller.</summary>
        private PvZGame _controller;
        /// <summary>Stores checks.</summary>
        private int _checks;
        /// <summary>Stores log error.</summary>
        private string _logError;
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

        /// <summary>Gets or sets campaign.</summary>
        private AdventureCampaign Campaign
        {
            get
            {
                return (AdventureCampaign)Field("_adventureCampaign");
            }
        }

        /// <summary>Performs field.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting field.</returns>
        private object Field(string name)
        {
            return typeof(PvZGame).GetField(name, Private).GetValue(_controller);
        }

        /// <summary>Performs call.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "args">The args.</param>
        /// <returns>The resulting call.</returns>
        private object Call(string name, params object[] args)
        {
            return typeof(PvZGame).GetMethod(name, Private).Invoke(_controller, args);
        }

        /// <summary>Performs click.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        private void Click(float x, float y)
        {
            Call("HandleClick", new Vector2(x, y));
        }

        /// <summary>Performs check.</summary>
        /// <param name = "passed">The passed.</param>
        /// <param name = "message">The message.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        private void Check(bool passed, string message)
        {
            _checks++;
            if (!passed)
            {
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>Performs start if requested.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-adventure-smoke") >= 0)
            {
                new GameObject("Adventure Player smoke").AddComponent<AdventurePlayerSmoke>();
            }
        }

        /// <summary>Performs start.</summary>
        /// <returns>The resulting start.</returns>
        private IEnumerator Start()
        {
            yield return null;
            _controller = FindFirstObjectByType<PvZGame>();
            Application.logMessageReceived += OnLog;
            var steps = Run();
            while (true)
            {
                bool next;
                try
                {
                    next = steps.MoveNext();
                }
                catch (Exception error)
                {
                    Debug.LogError("PVZ_ADVENTURE_SMOKE_FAIL: " + error);
                    SmokeTestArtifacts.Quit(1);
                    yield break;
                }

                if (!next)
                {
                    break;
                }

                yield return steps.Current;
            }

            if (_logError != null)
            {
                Debug.LogError("PVZ_ADVENTURE_SMOKE_FAIL: Player logged " + _logError);
                SmokeTestArtifacts.Quit(1);
            }
            else
            {
                Debug.Log("PVZ_ADVENTURE_SMOKE_PASS: " + _checks + " host/campaign/input checks; 22 visible screenshots.");
                SmokeTestArtifacts.Quit(0);
            }
        }

        /// <summary>Performs on log.</summary>
        /// <param name = "message">The message.</param>
        /// <param name = "stack">The stack.</param>
        /// <param name = "type">The type.</param>
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _logError = message;
            }
        }

        /// <summary>Performs on destroy.</summary>
        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }

        /// <summary>Performs capture.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting capture.</returns>
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(SmokeTestArtifacts.DirectoryPath, name + ".png"), image.EncodeToPNG());
            Destroy(image);
        }

        /// <summary>Performs cell y.</summary>
        /// <param name = "sim">The sim.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting cell y.</returns>
        private static float CellY(GameSimulation sim, int row, int column)
        {
            var roof = sim.Adventure.Level.HasRoof;
            return 80 + row * (sim.Adventure.Level.HasPool || roof ? 85 : 100) + (roof ? Mathf.Max(0, 5 - column) * 20 - 10 : 0) + 50;
        }

        /// <summary>Performs board click.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        private void BoardClick(int row, int column)
        {
            Click(80 + column * 80, CellY(Sim, row, column));
        }

        /// <summary>Performs plant from card.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting plant from card.</returns>
        private bool PlantFromCard(PlantKind kind, int row, int column)
        {
            var seeds = (List<PlantKind>)Field("_deck");
            var index = seeds.IndexOf(kind);
            if (index < 0 || Sim.SunAmount < Sim.AdventureSeedCost(kind) || Sim.CooldownRemaining(kind) > 0)
            {
                return false;
            }

            var card = _controller.AdventureSeedRect(index);
            Click(card.center.x, card.center.y);
            if (Sim.CanPlantAdventure(kind, row, column) != AdventurePlantingFailure.None)
            {
                return false;
            }

            var before = Sim.Plants.Count;
            BoardClick(row, column);
            return Sim.Plants.Count > before;
        }

        /// <summary>Performs collect visible suns.</summary>
        private void CollectVisibleSuns()
        {
            for (var i = Sim.Suns.Count - 1; i >= 0; i--)
            {
                var sun = Sim.Suns[i];
                float x = 40 + sun.X * 80, y = 80 + sun.Y * (Sim.Adventure.Level.HasPool || Sim.Adventure.Level.HasRoof ? 85 : 100);
                if (x > 0 && x < 800 && y > 80 && y < 580)
                {
                    Click(x, y);
                }
            }
        }

        /// <summary>Performs choose defaults.</summary>
        private void ChooseDefaults()
        {
            if (Sim.Adventure.Phase != AdventurePhase.ChoosingSeeds)
            {
                return;
            }

            var start = _controller.AdventureChooserStartRect;
            Click(start.center.x, start.center.y);
            Check(Sim.Adventure.Phase == AdventurePhase.Playing && !(bool)Field("_choosingSeeds"), "Visible choose/start did not activate the board.");
        }

        /// <summary>Performs use first legal packet.</summary>
        private void UseFirstLegalPacket()
        {
            if (Sim.Adventure.Packets.Count == 0)
            {
                return;
            }

            var packet = Sim.Adventure.Packets[0];
            for (var row = 0; row < Sim.RowCount; row++)
            {
                for (var col = 0; col < 9; col++)
                {
                    if (Sim.CanPlantAdventure(packet.Kind, row, col) == AdventurePlantingFailure.None)
                    {
                        var card = _controller.AdventureSeedRect(0, true);
                        Click(card.center.x, card.center.y);
                        BoardClick(row, col);
                        return;
                    }
                }
            }
        }

        /// <summary>Performs choose card.</summary>
        /// <param name = "kind">The kind.</param>
        private void ChooseCard(PlantKind kind)
        {
            var index = -1;
            for (var i = 0; i < Sim.Adventure.AvailableSeeds.Count; i++)
            {
                if (Sim.Adventure.AvailableSeeds[i] == kind)
                {
                    index = i;
                }
            }

            Check(index >= 0, "Requested test entitlement is not available: " + kind);
            var card = _controller.AdventureCampaignChooserRect(kind);
            Click(card.center.x, card.center.y);
        }

        /// <summary>Performs clear chosen cards.</summary>
        private void ClearChosenCards()
        {
            var selected = new List<PlantKind>((IEnumerable<PlantKind>)Field("_pendingSeeds"));
            foreach (var seed in selected)
            {
                if (_controller.ContainsRequiredSeed(seed))
                {
                    continue;
                }

                var pending = _controller._pendingSeeds;
                for (var i = 0; i < pending.Count; i++)
                {
                    if (pending[i] == seed)
                    {
                        var slots = Math.Min(Sim.Adventure.Level.PacketSlots, Sim.Adventure.AvailableSeeds.Count);
                        Click(AdventureSeedSlotLayout.PositionX(i, slots) + 25, 43);
                        break;
                    }
                }
            }
        }

        /// <summary>Performs check gray plant.</summary>
        /// <param name = "plantId">The plant id.</param>
        private void CheckGrayPlant(int plantId)
        {
            var renderer = (global::PlantsVsZombies.Reanimation.ReanimationRenderer)Field("_animations");
            var surfaces = (System.Collections.IDictionary)typeof(global::PlantsVsZombies.Reanimation.ReanimationRenderer).GetField("_nativeSurfaces", Private).GetValue(renderer);
            var surface = surfaces["producer-plant-" + plantId];
            Check(surface != null, "The transformed Imitater has no grayscale render surface.");
            var target = (RenderTexture)surface.GetType().GetField("Texture").GetValue(surface);
            var previous = RenderTexture.active;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                int opaque = 0, colored = 0;
                foreach (Color32 pixel in pixels.GetPixels32())
                {
                    if (pixel.a > 250)
                    {
                        opaque++;
                        if (Math.Abs(pixel.r - pixel.g) > 2 || Math.Abs(pixel.g - pixel.b) > 2)
                        {
                            colored++;
                        }
                    }
                }

                Check(opaque > 100 && colored == 0, "Imitater source pixels were not rendered in grayscale.");
            }
            finally
            {
                RenderTexture.active = previous;
                Destroy(pixels);
            }
        }

        /// <summary>Performs run.</summary>
        /// <returns>The resulting run.</returns>
        private IEnumerator Run()
        {
            Check(_controller != null, "Main game controller was not created.");
            var startupDeadline = Time.realtimeSinceStartup + 60f;
            while (_controller.OriginalLoadingActive && Time.realtimeSinceStartup < startupDeadline)
            {
                yield return null;
            }

            Check(!_controller.OriginalLoadingActive, "Original startup loading did not complete within 60 seconds.");
            Call("HandleSelector", SelectorAction.Adventure);
            Check(Sim.IsAdventure && Sim.Adventure.Level.Number == 1 && Campaign != null, "Normal Adventure menu did not open the campaign.");
            yield return Capture("adventure-1-1");
            Check(PlantFromCard(PlantKind.Peashooter, 2, 0), "Visible first card/grass input did not plant the first Peashooter.");
            yield return Capture("adventure-1-1-planted");
            for (var tick = 0; tick < 9000 && Sim.State == MatchState.Playing; tick++)
            {
                Sim.Update(0.1f);
                CollectVisibleSuns();
                for (var col = 1; col < 5; col++)
                {
                    if (Sim.AdventurePlantAt(2, col, PlantLayer.Main) == null && PlantFromCard(PlantKind.Peashooter, 2, col))
                    {
                        break;
                    }
                }

                if (tick % 100 == 0)
                {
                    yield return null;
                }
            }

            Check(Sim.State == MatchState.Won && Sim.Adventure.Phase == AdventurePhase.Reward, "First campaign battle did not reach an earned reward.");
            yield return Capture("adventure-reward");
            Click(Sim.Adventure.RewardPresentation.X + 25, Sim.Adventure.RewardPresentation.Y + 35);
            Check(Sim.Adventure.Phase == AdventurePhase.Finished && Campaign.Progress.NextLevel == 2, "Reward click did not commit progress.");
            var save = (string)Field("_adventureSavePath");
            var reopened = new AdventureProgressStore(save);
            Check(reopened.Get(Campaign.ProfileId).NextLevel == 2, "Reward was not durably saved.");
            File.Copy(save, Path.Combine(SmokeTestArtifacts.DirectoryPath, "campaign-after-first-reward.adventure"));
            var reward = Sim.Adventure.RewardPresentation;
            for (var update = 0; update < 200 && (reward.Stage != AdventureAwardStage.Introduction || reward.IntroductionTicks < 180); update++)
            {
                Sim.Update(.1f);
            }

            Check(reward.Stage == AdventureAwardStage.Introduction && reward.IntroductionTicks >= 180,
                "Reward continuation did not become ready; stage=" + reward.Stage + "; introductionTicks=" + reward.IntroductionTicks);
            yield return Capture("adventure-reward-collected");
            Click(402, 565);
            Check(Sim.Adventure.Level.Number == 2 && Sim.State == MatchState.Playing, "Next-level button reset the demo instead of advancing.");
            yield return Capture("adventure-1-2");
            Call("ResetGame");
            Check(Sim.Adventure.Level.Number == 2 && Campaign.Progress.NextLevel == 2, "Restart discarded campaign progress.");
            _controller.StartAdventureTestLevel(5);
            Check(Sim.Plants.Count == 3, "Bowling did not start with shovel teaching plants.");
            for (var col = 3; col <= 5; col++)
            {
                Click(635, 35);
                BoardClick(2, col);
            }

            Check(Sim.Plants.Count == 0, "Shovel pointer actions did not clear the teaching plants.");
            Sim.Update(4f);
            UseFirstLegalPacket();
            Check(Sim.Adventure.BowlingNuts.Count > 0, "Bowling packet pointer input did not roll a nut.");
            yield return Capture("adventure-1-5");
            int[] levels =
            {
                10,
                11,
                15,
                21,
                25,
                31,
                35,
                40,
                41,
                45,
                50
            };
            foreach (int level in levels)
            {
                _controller.StartAdventureTestLevel(level);
                if (level == 41)
                {
                    yield return Capture("adventure-5-1-chooser");
                }

                ChooseDefaults();
                Click(750, 100);
                Check(Sim.RowCount == Sim.Adventure.Level.Rows, "Host row count mismatch for level " + level);
                if (level == 11)
                {
                    var planted = false;
                    for (var r = 0; r < 5 && !planted; r++)
                    {
                        for (var c = 0; c < 3 && !planted; c++)
                        {
                            planted = PlantFromCard(PlantKind.PuffShroom, r, c);
                        }
                    }

                    Check(planted, "Night mushroom could not be planted through the seed/board input.");
                }

                if (level == 21)
                {
                    Check(Sim.RowCount == 6 && PlantFromCard(PlantKind.LilyPad, 2, 0), "Six-row water support input failed.");
                }

                if (level == 35)
                {
                    var before = Sim.Adventure.Vases.Count;
                    var vase = Sim.Adventure.Vases[0];
                    BoardClick(vase.Row, vase.Column);
                    Check(Sim.Adventure.Vases.Count == before - 1, "Vase pointer input did not break a vase.");
                }

                if (level == 15)
                {
                    for (var n = 0; n < 400 && Sim.Zombies.Count == 0; n++)
                    {
                        Sim.Update(0.1f);
                    }

                    Sim.Update(0.8f);
                    Check(Sim.Zombies.Count > 0, "Whack-a-zombie did not spawn a target.");
                    var z = Sim.Zombies[0];
                    var count = Sim.Zombies.Count;
                    var health = z.Health + z.ArmorHealth;
                    Click(40 + z.X * 80, CellY(Sim, z.Row, (int)z.X) - 10);
                    Check(Sim.Zombies.Count < count || z.Health + z.ArmorHealth < health || z.Headless, "Hammer pointer input did not hit a target.");
                }

                if (Sim.Adventure.Level.HasConveyor)
                {
                    Sim.Update(6f);
                    var packets = Sim.Adventure.Packets.Count;
                    Check(packets > 0, "Conveyor has no actionable packets: " + level);
                    UseFirstLegalPacket();
                    Check(Sim.Adventure.Packets.Count < packets, "Conveyor pointer input did not consume a legal packet: " + level);
                }

                if (level == 50)
                {
                    Sim.Update(29f);
                }

                yield return Capture("adventure-" + Sim.Adventure.Level.Label);
            }

            // Explicit test entitlements exercise the input/render binding, not a fictional shop purchase.
            _controller.StartAdventureTestLevel(49, new AdventureSeedAccess(new[] { PlantKind.CobCannon, PlantKind.Imitater }));
            ClearChosenCards();
            ChooseCard(PlantKind.Imitater);
            Check((bool)Field("_choosingImitater"), "Imitater card did not open target selection.");
            yield return Capture("adventure-imitater-target");
            // Kernel-pult is earned by this level; target selection uses the same visible card grid.
            ChooseCard(PlantKind.KernelPult);
            Check(Sim.Adventure.ImitaterTarget == PlantKind.KernelPult, "Imitater target input was not recorded.");
            foreach (PlantKind seed in new[]
            {
                PlantKind.Sunflower,
                PlantKind.CabbagePult,
                PlantKind.KernelPult,
                PlantKind.CobCannon,
                PlantKind.Marigold
            }

            )
            {
                ChooseCard(seed);
            }

            ChooseDefaults();
            Check(Sim.AdventureSeedCost(PlantKind.Imitater) == Sim.AdventureSeedCost(PlantKind.KernelPult), "Imitater card has the wrong target cost.");
            yield return Capture("adventure-upgrade-cards");
            for (var tick = 0; tick < 1200 && Sim.State == MatchState.Playing && Sim.SunAmount < 100; tick++)
            {
                Sim.Update(0.1f);
                CollectVisibleSuns();
                if (tick % 100 == 0)
                {
                    yield return null;
                }
            }

            Check(PlantFromCard(PlantKind.Imitater, 0, 0), "Imitater could not be planted through its target-priced card.");
            Check(Sim.Plants.Exists(p => p.Kind == PlantKind.Imitater), "Imitater transformed without its delay.");
            Sim.Update(3.3f);
            var copied = Sim.Plants.Find(p => p.IsImitated && p.Kind == PlantKind.KernelPult);
            Check(copied != null, "Imitater did not transform into its selected plant.");
            yield return Capture("adventure-imitater-transformed");
            CheckGrayPlant(copied.Id);
            _controller.StartAdventureTestLevel(49);
            ClearChosenCards();
            foreach (PlantKind seed in new[]
            {
                PlantKind.Sunflower,
                PlantKind.CabbagePult,
                PlantKind.KernelPult,
                PlantKind.Marigold,
                PlantKind.CherryBomb,
                PlantKind.CoffeeBean
            }

            )
            {
                ChooseCard(seed);
            }

            ChooseDefaults();
            Check(PlantFromCard(PlantKind.Marigold, 0, 0), "Marigold could not be planted through the normal card input.");
            for (var tick = 0; tick < 600 && Sim.State == MatchState.Playing && Sim.Adventure.Coins.Count == 0; tick++)
            {
                Sim.Update(0.1f);
                if (tick % 100 == 0)
                {
                    yield return null;
                }
            }

            Check(Sim.Adventure.Coins.Count > 0, "Marigold did not create an actual collectible coin.");
            yield return Capture("adventure-marigold-coins");
            var produced = Sim.Adventure.Coins[0];
            var value = produced.ValueUnits;
            var collectedBefore = Sim.Adventure.MoneyCollectedUnits;
            var coinsBefore = Sim.Adventure.Coins.Count;
            var coin = _controller.AdventureCoinRect(produced);
            Click(coin.center.x, coin.center.y);
            Check(Sim.Adventure.Coins.Count == coinsBefore - 1 && Sim.Adventure.MoneyCollectedUnits == collectedBefore + value,
                "Coin pointer input did not credit this board; before=" + collectedBefore + "; value=" + value + "; after=" + Sim.Adventure.MoneyCollectedUnits);
            Check(_logError == null, "The Player raised an error while drawing adventure terrain/units.");
        }
    }
}
