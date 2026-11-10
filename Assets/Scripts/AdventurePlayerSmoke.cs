using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PvZReplica.Adventure;
using PvZReplica.UI;
using UnityEngine;

namespace PvZReplica
{
    // Runs only in an explicitly requested, isolated Player smoke session.
    internal sealed class AdventurePlayerSmoke : MonoBehaviour
    {
        private PvZGame controller;
        private int checks;
        private string logError;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameSimulation Sim { get { return (GameSimulation)Field("game"); } }
        private AdventureCampaign Campaign { get { return (AdventureCampaign)Field("adventureCampaign"); } }
        private object Field(string name) { return typeof(PvZGame).GetField(name, Private).GetValue(controller); }
        private object Call(string name, params object[] args) { return typeof(PvZGame).GetMethod(name, Private).Invoke(controller, args); }
        private void Click(float x, float y) { Call("HandleClick", new Vector2(x, y)); }
        private void Check(bool passed, string message) { checks++; if (!passed) throw new InvalidOperationException(message); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-adventure-smoke") >= 0)
                new GameObject("Adventure Player smoke").AddComponent<AdventurePlayerSmoke>();
        }
        private IEnumerator Start()
        {
            yield return null;
            controller = FindFirstObjectByType<PvZGame>();
            Application.logMessageReceived += OnLog;
            IEnumerator steps = Run();
            while (true)
            {
                bool next;
                try { next = steps.MoveNext(); }
                catch (Exception error)
                {
                    Debug.LogError("PVZ_ADVENTURE_SMOKE_FAIL: " + error);
                    SmokeTestArtifacts.Quit(1);
                    yield break;
                }
                if (!next) break;
                yield return steps.Current;
            }
            if (logError != null)
            {
                Debug.LogError("PVZ_ADVENTURE_SMOKE_FAIL: Player logged " + logError);
                SmokeTestArtifacts.Quit(1);
            }
            else
            {
                Debug.Log("PVZ_ADVENTURE_SMOKE_PASS: " + checks + " host/campaign/input checks; 22 visible screenshots.");
                SmokeTestArtifacts.Quit(0);
            }
        }
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) logError = message;
        }
        private void OnDestroy() { Application.logMessageReceived -= OnLog; }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(SmokeTestArtifacts.DirectoryPath, name + ".png"), image.EncodeToPNG());
            Destroy(image);
        }
        private static float CellY(GameSimulation sim, int row, int column)
        {
            bool roof = sim.Adventure.Level.HasRoof;
            return 80 + row * (sim.Adventure.Level.HasPool || roof ? 85 : 100) +
                (roof ? Mathf.Max(0, 5 - column) * 20 - 10 : 0) + 50;
        }
        private void BoardClick(int row, int column) { Click(80 + column * 80, CellY(Sim, row, column)); }
        private bool PlantFromCard(PlantKind kind, int row, int column)
        {
            var seeds = (List<PlantKind>)Field("deck"); int index = seeds.IndexOf(kind);
            if (index < 0 || Sim.SunAmount < Sim.AdventureSeedCost(kind) || Sim.CooldownRemaining(kind) > 0) return false;
            Click(105 + index * 50, 43);
            if (Sim.CanPlantAdventure(kind, row, column) != AdventurePlantingFailure.None) return false;
            int before = Sim.Plants.Count;
            BoardClick(row, column);
            return Sim.Plants.Count > before;
        }
        private void CollectVisibleSuns()
        {
            for (int i = Sim.Suns.Count - 1; i >= 0; i--)
            {
                Sun sun = Sim.Suns[i];
                float x = 40 + sun.X * 80, y = 80 + sun.Y * (Sim.Adventure.Level.HasPool || Sim.Adventure.Level.HasRoof ? 85 : 100);
                if (x > 0 && x < 800 && y > 80 && y < 580) Click(x, y);
            }
        }
        private void ChooseDefaults()
        {
            if (Sim.Adventure.Phase != AdventurePhase.ChoosingSeeds) return;
            Click(400, 550);
            Check(Sim.Adventure.Phase == AdventurePhase.Playing && !(bool)Field("choosingSeeds"), "Visible choose/start did not activate the board.");
        }
        private void UseFirstLegalPacket()
        {
            if (Sim.Adventure.Packets.Count == 0) return;
            var packet = Sim.Adventure.Packets[0];
            for (int row = 0; row < Sim.RowCount; row++) for (int col = 0; col < 9; col++)
                if (Sim.CanPlantAdventure(packet.Kind, row, col) == AdventurePlantingFailure.None)
                {
                    Click(33, 43); BoardClick(row, col); return;
                }
        }
        private void ChooseCard(PlantKind kind)
        {
            int index = -1;
            for (int i = 0; i < Sim.Adventure.AvailableSeeds.Count; i++) if (Sim.Adventure.AvailableSeeds[i] == kind) index = i;
            Check(index >= 0, "Requested test entitlement is not available: " + kind);
            Click(143 + index % 10 * 57, 161 + index / 10 * 80);
        }
        private void ClearChosenCards()
        {
            var selected = new List<PlantKind>((HashSet<PlantKind>)Field("pendingSeeds"));
            foreach (PlantKind seed in selected) ChooseCard(seed);
        }
        private void CheckGrayPlant(int plantId)
        {
            var renderer = (Reanimation.ReanimationRenderer)Field("animations");
            var surfaces = (System.Collections.IDictionary)typeof(Reanimation.ReanimationRenderer)
                .GetField("nativeSurfaces", Private).GetValue(renderer);
            object surface = surfaces["imitated-plant-" + plantId];
            Check(surface != null, "The transformed Imitater has no grayscale render surface.");
            RenderTexture target = (RenderTexture)surface.GetType().GetField("Texture").GetValue(surface);
            RenderTexture previous = RenderTexture.active;
            Texture2D pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                int opaque = 0, colored = 0;
                foreach (Color32 pixel in pixels.GetPixels32())
                    if (pixel.a > 250) { opaque++; if (Math.Abs(pixel.r - pixel.g) > 2 || Math.Abs(pixel.g - pixel.b) > 2) colored++; }
                Check(opaque > 100 && colored == 0, "Imitater source pixels were not rendered in grayscale.");
            }
            finally { RenderTexture.active = previous; Destroy(pixels); }
        }
        private IEnumerator Run()
        {
            Check(controller != null, "Main game controller was not created.");
            Call("HandleSelector", SelectorAction.Adventure);
            Check(Sim.IsAdventure && Sim.Adventure.Level.Number == 1 && Campaign != null, "Normal Adventure menu did not open the campaign.");
            yield return Capture("adventure-1-1");
            Check(PlantFromCard(PlantKind.Peashooter, 2, 0), "Visible first card/grass input did not plant the first Peashooter.");
            yield return Capture("adventure-1-1-planted");
            for (int tick = 0; tick < 9000 && Sim.State == MatchState.Playing; tick++)
            {
                Sim.Update(0.1f); CollectVisibleSuns();
                for (int col = 1; col < 5; col++)
                    if (Sim.AdventurePlantAt(2, col, PlantLayer.Main) == null && PlantFromCard(PlantKind.Peashooter, 2, col)) break;
                if (tick % 100 == 0) yield return null;
            }
            Check(Sim.State == MatchState.Won && Sim.Adventure.Phase == AdventurePhase.Reward, "First campaign battle did not reach an earned reward.");
            yield return Capture("adventure-reward");
            Click(Sim.Adventure.RewardPresentation.X + 25, Sim.Adventure.RewardPresentation.Y + 35);
            Check(Sim.Adventure.Phase == AdventurePhase.Finished && Campaign.Progress.NextLevel == 2, "Reward click did not commit progress.");
            string save = (string)Field("adventureSavePath");
            var reopened = new AdventureProgressStore(save);
            Check(reopened.Get(Campaign.ProfileId).NextLevel == 2, "Reward was not durably saved.");
            File.Copy(save, Path.Combine(SmokeTestArtifacts.DirectoryPath, "campaign-after-first-reward.adventure"));
            while (Sim.Adventure.RewardPresentation.Stage != AdventureAwardStage.Introduction || Sim.Adventure.RewardPresentation.IntroductionTicks < 60) Sim.Update(.1f);
            yield return Capture("adventure-reward-collected");
            Click(402, 565);
            Check(Sim.Adventure.Level.Number == 2 && Sim.State == MatchState.Playing, "Next-level button reset the demo instead of advancing.");
            yield return Capture("adventure-1-2");
            Call("ResetGame");
            Check(Sim.Adventure.Level.Number == 2 && Campaign.Progress.NextLevel == 2, "Restart discarded campaign progress.");

            controller.StartAdventureTestLevel(5);
            Check(Sim.Plants.Count == 3, "Bowling did not start with shovel teaching plants.");
            for (int col = 3; col <= 5; col++) { Click(635, 35); BoardClick(2, col); }
            Check(Sim.Plants.Count == 0, "Shovel pointer actions did not clear the teaching plants.");
            Sim.Update(4f); UseFirstLegalPacket();
            Check(Sim.Adventure.BowlingNuts.Count > 0, "Bowling packet pointer input did not roll a nut.");
            yield return Capture("adventure-1-5");

            int[] levels = {10, 11, 15, 21, 25, 31, 35, 40, 41, 45, 50};
            foreach (int level in levels)
            {
                controller.StartAdventureTestLevel(level);
                if (level == 41) yield return Capture("adventure-5-1-chooser");
                ChooseDefaults();
                Click(750, 100);
                Check(Sim.RowCount == Sim.Adventure.Level.Rows, "Host row count mismatch for level " + level);
                if (level == 11)
                {
                    bool planted = false;
                    for (int r = 0; r < 5 && !planted; r++) for (int c = 0; c < 3 && !planted; c++)
                        planted = PlantFromCard(PlantKind.PuffShroom, r, c);
                    Check(planted, "Night mushroom could not be planted through the seed/board input.");
                }
                if (level == 21)
                {
                    Check(Sim.RowCount == 6 && PlantFromCard(PlantKind.LilyPad, 2, 0), "Six-row water support input failed.");
                }
                if (level == 35)
                {
                    int before = Sim.Adventure.Vases.Count; var vase = Sim.Adventure.Vases[0];
                    BoardClick(vase.Row, vase.Column);
                    Check(Sim.Adventure.Vases.Count == before - 1, "Vase pointer input did not break a vase.");
                }
                if (level == 15)
                {
                    for (int n = 0; n < 400 && Sim.Zombies.Count == 0; n++) Sim.Update(0.1f);
                    Sim.Update(0.8f);
                    Check(Sim.Zombies.Count > 0, "Whack-a-zombie did not spawn a target.");
                    var z = Sim.Zombies[0]; int count = Sim.Zombies.Count; float health = z.Health + z.ArmorHealth;
                    Click(40 + z.X * 80, CellY(Sim, z.Row, (int)z.X) - 10);
                    Check(Sim.Zombies.Count < count || z.Health + z.ArmorHealth < health || z.Headless, "Hammer pointer input did not hit a target.");
                }
                if (Sim.Adventure.Level.HasConveyor)
                {
                    Sim.Update(6f); int packets = Sim.Adventure.Packets.Count;
                    Check(packets > 0, "Conveyor has no actionable packets: " + level);
                    UseFirstLegalPacket();
                    Check(Sim.Adventure.Packets.Count < packets, "Conveyor pointer input did not consume a legal packet: " + level);
                }
                if (level == 50) Sim.Update(29f);
                yield return Capture("adventure-" + Sim.Adventure.Level.Label);
            }
            // Explicit test entitlements exercise the input/render binding, not a fictional shop purchase.
            controller.StartAdventureTestLevel(49, new AdventureSeedAccess(new[] { PlantKind.CobCannon, PlantKind.Imitater }));
            ClearChosenCards(); ChooseCard(PlantKind.Imitater);
            Check((bool)Field("choosingImitater"), "Imitater card did not open target selection.");
            yield return Capture("adventure-imitater-target");
            // Kernel-pult is earned by this level; target selection uses the same visible card grid.
            ChooseCard(PlantKind.KernelPult);
            Check(Sim.Adventure.ImitaterTarget == PlantKind.KernelPult, "Imitater target input was not recorded.");
            foreach (PlantKind seed in new[] { PlantKind.Sunflower, PlantKind.CabbagePult, PlantKind.KernelPult, PlantKind.CobCannon, PlantKind.Marigold }) ChooseCard(seed);
            ChooseDefaults();
            Check(Sim.AdventureSeedCost(PlantKind.Imitater) == Sim.AdventureSeedCost(PlantKind.KernelPult), "Imitater card has the wrong target cost.");
            yield return Capture("adventure-upgrade-cards");
            for (int tick = 0; tick < 1200 && Sim.State == MatchState.Playing && Sim.SunAmount < 100; tick++)
            { Sim.Update(0.1f); CollectVisibleSuns(); if (tick % 100 == 0) yield return null; }
            Check(PlantFromCard(PlantKind.Imitater, 0, 0), "Imitater could not be planted through its target-priced card.");
            Check(Sim.Plants.Exists(p => p.Kind == PlantKind.Imitater), "Imitater transformed without its delay.");
            Sim.Update(3.3f);
            Plant copied = Sim.Plants.Find(p => p.IsImitated && p.Kind == PlantKind.KernelPult);
            Check(copied != null, "Imitater did not transform into its selected plant.");
            yield return Capture("adventure-imitater-transformed");
            CheckGrayPlant(copied.Id);

            controller.StartAdventureTestLevel(49);
            ClearChosenCards();
            foreach (PlantKind seed in new[] { PlantKind.Sunflower, PlantKind.CabbagePult, PlantKind.KernelPult, PlantKind.Marigold, PlantKind.CherryBomb, PlantKind.CoffeeBean }) ChooseCard(seed);
            ChooseDefaults();
            Check(PlantFromCard(PlantKind.Marigold, 0, 0), "Marigold could not be planted through the normal card input.");
            for (int tick = 0; tick < 600 && Sim.State == MatchState.Playing && Sim.Adventure.Coins.Count == 0; tick++)
            { Sim.Update(0.1f); if (tick % 100 == 0) yield return null; }
            Check(Sim.Adventure.Coins.Count > 0, "Marigold did not create an actual collectible coin.");
            yield return Capture("adventure-marigold-coins");
            var produced = Sim.Adventure.Coins[0]; int value = produced.ValueUnits;
            Click(40 + produced.X * 80, CellY(Sim, (int)produced.Row, (int)produced.X) + 6);
            Check(Sim.Adventure.MoneyCollectedUnits == value, "Coin pointer input did not credit this board.");
            Check(logError == null, "The Player raised an error while drawing adventure terrain/units.");
        }
    }
}
