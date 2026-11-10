using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PvZReplica.Adventure;
using PvZReplica.Progression;
using PvZReplica.UI;
using PvZReplica.VerifiedData;
using PvZReplica.Reanimation;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private AdventureProgressStore adventureStore;
        private AdventureCampaign adventureCampaign;
        private string adventureSavePath;
        private string adventureLoadError;
        private int selectedAdventurePacket = -1;
        private string adventureFeedback;
        private float adventureFeedbackUntil;
        private bool adventureTestBoard;
        private bool choosingImitater;
        private Vector2 adventurePointer;
        private static readonly Rect AdventureActionButton = new Rect(316, 356, 168, 48);
        private static readonly Rect AdventureMenuButton = new Rect(704, 6, 90, 34);
        private static readonly Rect AdventureShovelButton = new Rect(600, 0, 70, 72);

        private bool UseLegacyBoard
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                return experimentalLevelOne || Array.IndexOf(args, "-pvz-demo") >= 0 ||
                    (smokeMode && Array.IndexOf(args, "-pvz-combat-fidelity-smoke") < 0 && Array.IndexOf(args, "-pvz-intro-audio-smoke") < 0 && Array.IndexOf(args, "-pvz-feedback-smoke") < 0 && Array.IndexOf(args, "-pvz-progression-smoke") < 0 && Array.IndexOf(args, "-pvz-adventure-smoke") < 0 &&
                     Array.IndexOf(args, "-pvz-ui-review") < 0);
            }
        }

        private bool EnsureAdventureStore()
        {
            if (adventureStore != null) return true;
            if (adventureLoadError != null) return false;
            try
            {
                if (adventureSavePath == null)
                    adventureSavePath = smokeMode
                        ? Path.Combine(Application.temporaryCachePath, "pvz-campaign-" + Guid.NewGuid().ToString("N") + ".adventure")
                        : Path.Combine(Application.persistentDataPath, "replica-campaign.adventure");
                adventureStore = new AdventureProgressStore(adventureSavePath);
                return true;
            }
            catch (Exception error)
            {
                adventureLoadError = error.Message;
                return false;
            }
        }

        private bool StartAdventureCampaign()
        {
            if (profiles == null || !profiles.SelectedId.HasValue)
            {
                if (profileDialog != null) profileDialog.Open();
                return false;
            }
            if (!EnsureAdventureStore() || !EnsurePlayerAccount())
            {
                SetUnavailable("冒险进度无法读取", adventureLoadError);
                return false;
            }
            try
            {
                adventureCampaign = new AdventureCampaign(adventureStore, profiles.SelectedId.Value,accountStore);
                if (adventureCampaign.Progress.Completed)
                {
                    adventureStore.StartNextCycle(profiles.SelectedId.Value);
                }
                adventureTestBoard = false;
                AttachAdventureGame(adventureCampaign.BeginOrResume());
                currentView = ScreenView.Board;
                return true;
            }
            catch (Exception error)
            {
                SetUnavailable("冒险关卡无法开始", error.Message);
                return false;
            }
        }

        private void AttachAdventureGame(GameSimulation simulation)
        {
            game = simulation;
            animations.ResetNativePlayback();
            game.EventRaised = PlaySound;
            game.Adventure.NoticeRaised = AdventureNotice;
            InitializeAdventureWaveNotices();
            deck.Clear();
            foreach (PlantKind seed in game.Adventure.SelectedSeeds) deck.Add(seed);
            if (game.Adventure.Level.Mode == AdventureMode.WhackAZombie)
                deck.AddRange(new[] { PlantKind.PotatoMine, PlantKind.CherryBomb, PlantKind.GraveBuster });
            choosingSeeds = game.Adventure.Phase == AdventurePhase.ChoosingSeeds;
            pendingSeeds = new HashSet<PlantKind>(game.Adventure.SelectedSeeds);
            selected = -1;
            selectedAdventurePacket = -1;
            shovel = false;
            adventureFeedback = null;
            choosingImitater = false;
            InitializeAdventureOpening();
        }

        private void RestartAdventure()
        {
            if (adventureTestBoard)
            {
                StartAdventureTestLevel(game.Adventure.Level.Number);
                return;
            }
            try { AttachAdventureGame(adventureCampaign.BeginOrResume()); }
            catch (Exception error) { SetUnavailable("冒险关卡无法重新开始", error.Message); }
        }

        internal void StartAdventureTestLevel(int number, AdventureSeedAccess access = null)
        {
            if (!smokeMode) throw new InvalidOperationException("Test boards require an isolated smoke session.");
            adventureTestBoard = true;
            AttachAdventureGame(new GameSimulation(new GameRules { AdventureLevel = number, AdventureSeedAccess = access }, number * 104729));
            currentView = ScreenView.Board;
        }

        private void AdventureNotice(AdventureNotice notice)
        {
            if(notice.Kind==AdventureNoticeKind.CoinCollected){modeAccountSnapshot=null;displayAccountSnapshot=null;walletVisibleUntil=game.Elapsed+10f;}
            HandleAdventureRewardNoticeSound(notice);
            // Huge/final warnings are original animated wave messages, not a small tutorial advice box.
            if (notice.Kind == AdventureNoticeKind.VaseStage) AdventureFeedback("第 " + (notice.Value + 1) + " 轮：小心罐子里的僵尸！");
            else if (notice.Kind == AdventureNoticeKind.BossAttack && notice.Value == 2) AdventureFeedback("小心僵王的攻击！");
        }

        private void AdventureFeedback(string text, float duration = 2.5f)
        {
            adventureFeedback = text;
            adventureFeedbackUntil = Time.unscaledTime + duration;
        }

        private Rect AdventureSeedRect(int index, bool packet = false)
        {
            if (packet && game != null && game.UsesNativeAdventureConveyor && index < game.Adventure.Packets.Count)
                return new Rect(game.AdventureConveyorCardX(index), 8, 50, 70);
            return new Rect(packet ? 8f + index * 50f : AdventureSeedSlotLayout.PositionX(index, deck.Count), 8f, 50f, 70f);
        }
        private static Rect AdventureChooserRect(int index)
        {
            return new Rect(118f + index % 10 * 57f, 126f + index / 10 * 80f, 50f, 70f);
        }

        private void DrawAdventureProgress()
        {
            if (UseLegacyBoard || profiles == null || !profiles.SelectedId.HasValue || !EnsureAdventureStore()) return;
            var progress = adventureStore.Get(profiles.SelectedId.Value);
            string text = progress.Completed ? "冒险模式已通关 · 可开始下一周目" : "冒险第 "+progress.Cycle+" 周目  " + AdventureLevels.Get(progress.NextLevel).Label;
            Label(new Rect(500, 151, 195, 25), text, 17, new Color(1f, 0.94f, 0.54f), TextAnchor.MiddleCenter, true);
        }

        private string AdventurePlantName(PlantKind kind)
        {
            return kind == PlantKind.ExplodeONut ? "爆炸坚果" : almanacView.PlantDisplayName((int)kind);
        }

        private void DrawAdventureCard(PlantKind kind, Rect card, bool price)
        {
            Texture2D bank = Texture("seeds.png");
            if (card.width > 50.01f && Texture("SeedPacket_Larger.png") != null) DrawImage("SeedPacket_Larger.png", card);
            else if (bank != null) animations.DrawNativeImage(bank, card, new Rect(2f / 9f, 0, 1f / 9f, 1));
            int id = (int)kind;
            bool imitated = kind == PlantKind.Imitater && game.Adventure.ImitaterTarget.HasValue;
            if (imitated) id = (int)game.Adventure.ImitaterTarget.Value;
            if (id >= 0 && id < 49)
            {
                int cell = AlmanacPortraitLayout.PlantAtlasCell(id);
                Texture2D atlas = cell >= 0 ? Texture("packet_plants.png") : null;
                if (atlas != null)
                {
                    Rect source = new Rect(cell / 13f, 0, 1f / 13f, 1);
                    if (imitated) animations.DrawNativeClipped("imitater-card-" + card.x + "-" + card.y, card,
                        () => animations.DrawNativeImage(atlas, card, source), false, true);
                    else animations.DrawNativeImage(atlas, card, source);
                }
                else
                {
                    Vector2 origin; float scale;
                    AlmanacPortraitLayout.PlantThumbnailTransform(id, new Rect(0, 0, 50, 70), out origin, out scale);
                    origin = new Vector2(card.x + origin.x * card.width / 50f, card.y + origin.y * card.height / 70f);
                    scale *= card.width / 50f;
                    PlantKind iconKind = imitated ? game.Adventure.ImitaterTarget.Value : kind;
                    if (imitated)
                        animations.DrawNativeClipped("imitater-card-" + card.x + "-" + card.y, card,
                            () => animations.DrawNative(AdventurePlantAnimation(iconKind), origin, scale, 0f), false, true);
                    else animations.DrawNative(AdventurePlantAnimation(iconKind), origin, scale, 0f);
                }
            }
            else
            {
                Color previous = GUI.color;
                // PC ReanimatorCache::MakeCachedPlant 0x47E9A0 uses RGB(255,64,64).
                GUI.color = new Color(previous.r, previous.g * 64f / 255f, previous.b * 64f / 255f, previous.a);
                Vector2 origin; float scale;
                AlmanacPortraitLayout.PlantThumbnailTransform((int)PlantKind.Wallnut, new Rect(0, 0, 50, 70), out origin, out scale);
                origin = new Vector2(card.x + origin.x * card.width / 50f, card.y + origin.y * card.height / 70f);
                animations.DrawNative("Wallnut", origin, scale * card.width / 50f, 0f, "anim_idle");
                GUI.color = previous;
            }
            if (price) Label(new Rect(card.x + 2 * card.width / 50f, card.y + 53 * card.height / 70f, 43 * card.width / 50f, 15 * card.height / 70f),
                kind == PlantKind.Imitater && !game.Adventure.ImitaterTarget.HasValue ? "?" : game.AdventureSeedCost(kind).ToString(),
                Mathf.RoundToInt(13 * card.width / 50f), Color.black, TextAnchor.MiddleRight, true);
        }

        private void DrawAdventureInterface()
        {
            var board = game.Adventure;
            if (DrawAdventureOpeningInterface()) return;
            if (selectedAdventurePacket >= 0)
            {
                bool exists = false;
                foreach (var packet in board.Packets) if (packet.Id == selectedAdventurePacket) { exists = true; break; }
                if (!exists) selectedAdventurePacket = -1;
            }
            bool packets = board.Level.HasConveyor || board.Level.Mode == AdventureMode.Vasebreaker;
            if (game.UsesNativeAdventureConveyor) DrawAdventureConveyorBackground();
            else if (packets) DrawImage("ConveyorBelt_backdrop.png", new Rect(0, 0, 516, 87));
            else DrawAdventureSeedBankBackground(Vector2.zero, deck.Count);
            if (packets)
            {
                DrawAdventureConveyorPackets();
            }
            else
            {
                Label(new Rect(8, 59, 60, 22), game.SunAmount.ToString(), 18, Color.black, TextAnchor.MiddleCenter, true);
                for (int i = 0; i < deck.Count; i++)
                {
                    Rect card = AdventureSeedRect(i); DrawAdventurePacket(deck[i], card);

                }
            }
            if (board.Level.Number >= 5)
            {
                DrawImage("ShovelBank.png", AdventureShovelButton); DrawImage("Shovel.png", AdventureShovelButton);
                if (shovel) Outline(AdventureShovelButton, Color.yellow, 3);
            }
            DrawOriginalUiButton(AdventureMenuButton, "菜单");
            if (!packets) Label(new Rect(535, 6, 62, 28), board.Level.Label, 19, Color.white, TextAnchor.MiddleCenter, true);
            if (board.Level.Mode == AdventureMode.Boss)
            {
                Fill(new Rect(490, 570, 290, 18), new Color(0.1f, 0.05f, 0.05f, 0.9f));
                Fill(new Rect(492, 572, 286f * board.BossHealth / board.BossMaxHealth, 14), new Color(0.76f, 0.12f, 0.08f));
            }
            else Label(new Rect(600, 569, 190, 25), "关卡 " + board.Level.Label + "    " + board.CurrentWave + " / " + board.Waves.Count,
                16, Color.white, TextAnchor.MiddleRight, true);
            if(board.MoneyCollectedUnits>0||game.Elapsed<walletVisibleUntil)
            {
                var wallet=ReadDisplayAccount();if(wallet!=null){DrawImage("coinbank.png",new Rect(16,559,128,31));Label(new Rect(22,561,112,26),"$"+wallet.DisplayCoins.ToString("N0",System.Globalization.CultureInfo.InvariantCulture),17,new Color(.7f,1f,.35f),TextAnchor.MiddleRight,true);}
            }
            if (board.SelectedCobCannonId.HasValue)
                DrawImage("CobCannon_target.png", new Rect(adventurePointer.x - 36, adventurePointer.y - 36, 72, 72));
            string advice = board.SelectedCobCannonId.HasValue ? "点击目标位置发射，右键取消瞄准。" : AdventureFirstWaveTutorialAdvice() ?? AdventureTutorialText(board.Tutorial);
            if (adventureFeedback != null && Time.unscaledTime < adventureFeedbackUntil) advice = adventureFeedback;
            if (!string.IsNullOrEmpty(advice) && board.Phase == AdventurePhase.Playing)
            {
                DrawOriginalUiAdvice(new Rect(175, 94, 450, 36), advice);
                if (board.Level.Number > 2 && board.Tutorial != AdventureTutorial.Shovel && board.Tutorial != AdventureTutorial.Complete)
                    DrawOriginalUiButton(new Rect(706, 86, 80, 32), "知道了");
            }
            DrawAdventureWaveNotices();
            DrawAdventurePacketReadyFlashes();
            DrawCollectedSunsAboveSeedBank();
            if (choosingSeeds) DrawAdventureChooser();
            else if (board.Phase == AdventurePhase.Reward || board.Phase == AdventurePhase.Finished) DrawAdventureReward();
            else if (game.State == MatchState.Lost) DrawDialog("僵尸吃掉了你的脑子！", "重新开始", false);
            else if (game.State == MatchState.Paused && !optionsView.IsOpen) DrawDialog("已暂停", "继续游戏", true);
        }

        private bool ContainsRequiredSeed(PlantKind kind) {foreach(var seed in game.Adventure.RequiredSeeds)if(seed==kind)return true;return false;}
        private void DrawAdventureChooser()
        {
            if (adventureIntro == null) { DrawLegacyAdventureChooser(); return; }
            DrawImage("SeedChooser_Background.png", new Rect(0, 87, 465, 513));
            string title = OriginalProgressionText("CHOOSE_YOUR_PLANTS");
            Color nativeGold = new Color(213f / 255f, 159f / 255f, 43f / 255f);
            if (!DrawBitmap(optionsBitmap, title, 229, 110, 440, nativeGold, true)) Label(new Rect(18, 87, 425, 29), title, 19, nativeGold, TextAnchor.MiddleCenter, true);
            var seeds = choosingImitater ? game.Adventure.Level.UnlockedSeeds : game.Adventure.AvailableSeeds;
            foreach (PlantKind seed in seeds)
            {
                Rect rect = AdventureCampaignChooserRect(seed); Color previous = GUI.color;
                if (!choosingImitater && pendingSeeds.Contains(seed)) GUI.color = new Color(previous.r * 55f / 255f, previous.g * 55f / 255f, previous.b * 55f / 255f, previous.a);
                if (seed == PlantKind.Imitater) DrawImage("SeedChooser_ImitaterAddOn.png", new Rect(459, 503, 66, 93));
                DrawAdventureCard(seed, rect, true); GUI.color = previous;
            }
            int required = Math.Min(game.Adventure.Level.PacketSlots, game.Adventure.AvailableSeeds.Count);
            bool enabled = choosingImitater || pendingSeeds.Count == required;
            DrawImage(enabled ? "SeedChooser_Button.png" : "SeedChooser_Button_Disabled.png", AdventureChooserStartRect);
            string caption = choosingImitater ? OriginalProgressionText("CANCEL") : OriginalProgressionText("LETS_ROCK_BUTTON");
            if (!DrawBitmap(optionsBitmap, caption, AdventureChooserStartRect.center.x, 572, 150, nativeGold, true)) Label(AdventureChooserStartRect, caption, 18, nativeGold, TextAnchor.MiddleCenter, true);
        }
        private void DrawLegacyAdventureChooser()
        {
            DrawImage("SeedChooser_Background.png", new Rect(85, 70, 630, 505));
            Label(new Rect(140, 78, 520, 37), "关卡 " + game.Adventure.Level.Label + "  选择你的植物", 25, Color.white, TextAnchor.MiddleCenter, true);
            var unlocked = game.Adventure.AvailableSeeds;
            for (int i = 0; i < unlocked.Count; i++)
            {
                Rect card = AdventureChooserRect(i); DrawAdventureCard(unlocked[i], card, true);
                if (pendingSeeds.Contains(unlocked[i])) Outline(card, ContainsRequiredSeed(unlocked[i])?new Color(1f,0.5f,0.2f):Color.yellow, 3);
            }
            int required = Math.Min(game.Adventure.Level.PacketSlots, unlocked.Count);
            DrawOriginalUiButton(new Rect(300, 533, 200, 34), "开始战斗  " + pendingSeeds.Count + "/" + required, pendingSeeds.Count == required);
            if (choosingImitater)
            {
                DrawImage("SeedChooser_Background.png", new Rect(85, 70, 630, 505));
                Label(new Rect(140, 78, 520, 37), "模仿哪一种植物？", 25, Color.white, TextAnchor.MiddleCenter, true);
                var targets = game.Adventure.Level.UnlockedSeeds;
                for (int i = 0; i < targets.Count; i++) DrawAdventureCard(targets[i], AdventureChooserRect(i), true);
                DrawOriginalUiButton(new Rect(300, 533, 200, 34), "取消");
            }
        }
        private string AdventureRewardName()
        {
            var level = game.Adventure.Level;
            if (level.IsReplay) return level.Number==50?"获得金币奖励和向日葵奖杯":"获得金币奖励";
            if (level.RewardSeed.HasValue) return "获得 " + AdventurePlantName(level.RewardSeed.Value);
            switch (level.RewardKind)
            {
                case AdventureRewardKind.Shovel: return "获得铲子";
                case AdventureRewardKind.Almanac: return "获得图鉴";
                case AdventureRewardKind.CarKeys: return "获得戴夫的车钥匙";
                case AdventureRewardKind.Taco: return "获得玉米卷";
                case AdventureRewardKind.WateringCan: return "获得浇水壶";
                case AdventureRewardKind.Note: return "僵尸送来了一张纸条";
                default: return "获得银向日葵奖杯";
            }
        }
        private static string AdventureTutorialText(AdventureTutorial tutorial)
        {
            switch (tutorial)
            {
                case AdventureTutorial.ChooseSeed: return "点击种子卡片，选择豌豆射手。";
                case AdventureTutorial.PlantPeashooter: return "点击草坪，种下豌豆射手。";
                case AdventureTutorial.CollectSun: return "点击阳光，收集种植植物所需的能量。";
                case AdventureTutorial.PlantSunflower: return "先种下两株向日葵，生产更多阳光。";
                case AdventureTutorial.RechargeSunflower: return "收集阳光，等待向日葵种子恢复。";
                case AdventureTutorial.Shovel: return "选择铲子，把草坪上的三株植物铲掉。";
                case AdventureTutorial.NightMushrooms: return "夜晚没有从天而降的阳光，试试小蘑菇！";
                case AdventureTutorial.PoolSupport: return "先在水面种下睡莲，再放上其他植物。";
                case AdventureTutorial.FogVisibility: return "路灯花能照亮迷雾，三叶草能暂时吹走迷雾。";
                case AdventureTutorial.RoofPots: return "屋顶需要花盆，投手可以越过斜坡攻击。";
                case AdventureTutorial.Conveyor: return "点击传送带上的卡片，在草坪上使用。";
                case AdventureTutorial.Bowling: return "在红线左侧放出坚果，击倒僵尸！";
                case AdventureTutorial.Hammer: return "点击僵尸，用锤子保护你的脑子！";
                case AdventureTutorial.Vases: return "点击罐子，使用掉出的植物打败僵尸。";
                case AdventureTutorial.Boss: return "保护屋顶！冰菇克制火球，辣椒克制冰球。";
                default: return null;
            }
        }

        private void HandleAdventureClick(Vector2 point)
        {
            var board = game.Adventure;
            if (AdventureIntroActive && !AdventureIntroAwaitingSeeds) return;
            if (AdventureMenuButton.Contains(point)) { OpenOptions(); return; }
            if (choosingSeeds)
            {
                var unlocked = board.AvailableSeeds;
                int required = Math.Min(board.Level.PacketSlots, unlocked.Count);
                if (choosingImitater)
                {
                    var targets = board.Level.UnlockedSeeds;
                    for (int i = 0; i < targets.Count; i++)
                        if (AdventureCampaignChooserRect(targets[i]).Contains(point) && game.SetAdventureImitaterTarget(targets[i]))
                        { pendingSeeds.Add(PlantKind.Imitater); choosingImitater = false; return; }
                    if (AdventureChooserStartRect.Contains(point)) choosingImitater = false;
                    return;
                }
                for (int i = 0; i < unlocked.Count; i++)
                    if (AdventureCampaignChooserRect(unlocked[i]).Contains(point))
                    {
                        if(ContainsRequiredSeed(unlocked[i])){AdventureFeedback("这是戴夫为你挑选的植物。",2f);return;}
                        if (!pendingSeeds.Remove(unlocked[i]) && pendingSeeds.Count < required)
                        {
                            if (unlocked[i] == PlantKind.Imitater) choosingImitater = true;
                            else pendingSeeds.Add(unlocked[i]);
                        }
                        return;
                    }
                if (AdventureChooserStartRect.Contains(point) && pendingSeeds.Count == required)
                {
                    var seeds = new List<PlantKind>();
                    foreach (PlantKind kind in unlocked) if (pendingSeeds.Contains(kind)) seeds.Add(kind);
                    try
                    {
                        bool chosen = adventureTestBoard ? game.ChooseAdventureSeeds(seeds) : adventureCampaign.ChooseSeeds(seeds);
                        if (chosen) { choosingSeeds = false; deck.Clear(); deck.AddRange(seeds); BeginAdventureOpeningReturn(); }
                    }
                    catch (Exception error) { AdventureFeedback("无法保存选卡：" + error.Message, 5f); }
                }
                return;
            }
            if (board.Phase == AdventurePhase.Reward || board.Phase == AdventurePhase.Finished)
            {
                HandleAdventureRewardClick(point); return;
            }
            if (game.State == MatchState.Lost)
            {
                if (new Rect(316, 340, 168, 48).Contains(point)) RestartAdventure();
                return;
            }
            if (game.State == MatchState.Paused)
            {
                if (new Rect(316, 280, 168, 48).Contains(point)) game.TogglePause();
                else if (new Rect(316, 340, 168, 48).Contains(point)) RestartAdventure();
                return;
            }
            if (new Rect(706, 86, 80, 32).Contains(point) && game.AcknowledgeAdventureTutorial()) return;
            for (int i = board.Coins.Count - 1; i >= 0; i--)
                if (AdventureCoinRect(board.Coins[i]).Contains(point)) { if(!game.CollectAdventureCoin(board.Coins[i])&&adventureCampaign!=null&&adventureCampaign.LastPersistenceError!=null)AdventureFeedback("金币尚未保存："+adventureCampaign.LastPersistenceError,5f); return; }
            for (int i = game.Suns.Count - 1; i >= 0; i--)
                if (AdventureSunRect(game.Suns[i]).Contains(point)) { game.CollectSun(game.Suns[i]); return; }
            bool packets = board.Level.HasConveyor || board.Level.Mode == AdventureMode.Vasebreaker;
            if (packets)
            {
                for (int i = 0; i < board.Packets.Count && i < 10; i++)
                    if (AdventurePacketContains(i, point))
                    {
                        selectedAdventurePacket = selectedAdventurePacket == board.Packets[i].Id ? -1 : board.Packets[i].Id;
                        selected = -1; shovel = false; game.CancelAdventureCobCannonAim(); return;
                    }
            }
            else
            {
                for (int i = 0; i < deck.Count; i++)
                    if (AdventureSeedRect(i).Contains(point))
                    {
                        if (game.CooldownRemaining(deck[i]) <= 0 && game.AdventureSunAvailable >= game.AdventureSeedCost(deck[i]) &&
                            (board.Level.Mode == AdventureMode.WhackAZombie || game.SelectAdventureSeed(deck[i])))
                            selected = selected == i ? -1 : i;
                        shovel = false; selectedAdventurePacket = -1; game.CancelAdventureCobCannonAim(); return;
                    }
            }
            if (board.Level.Number >= 5 && AdventureShovelButton.Contains(point))
            { shovel = !shovel; selected = -1; selectedAdventurePacket = -1; game.CancelAdventureCobCannonAim(); return; }
            int col = Mathf.FloorToInt((point.x - AdventureGridX) / CellWidth);
            int row = Mathf.FloorToInt((point.y - AdventureGridY - AdventureSlope(col)) / AdventurePitch);
            if (col < 0 || col > 8 || row < 0 || row >= game.RowCount) return;
            if (board.SelectedCobCannonId.HasValue) { game.TryFireAdventureCobCannonAtCell(row, col); return; }
            if (shovel) { if (game.RemovePlant(row, col)) shovel = false; return; }
            if (selectedAdventurePacket >= 0)
            {
                if (game.TryUseAdventurePacket(selectedAdventurePacket, row, col)) selectedAdventurePacket = -1;
                else AdventurePlantingFeedback();
                return;
            }
            if (selected >= 0)
            {
                if (game.TryPlant(deck[selected], row, col)) selected = -1;
                else AdventurePlantingFeedback();
                return;
            }
            if (game.TryAimAdventureCobCannon(row, col)) return;
            if (board.Level.Mode == AdventureMode.Vasebreaker) { game.SmashAdventureVase(row, col); return; }
            if (board.Level.Mode == AdventureMode.WhackAZombie)
                for (int i = game.Zombies.Count - 1; i >= 0; i--)
                {
                    Zombie zombie = game.Zombies[i];
                    float x = AdventureGridX + zombie.X * CellWidth;
                    if (new Rect(x - 48, AdventureCellY(zombie.Row, zombie.X) - 35, 96, 140).Contains(point))
                    { game.HammerAdventureZombie(zombie.Id); return; }
                }
        }
        private void AdventurePlantingFeedback()
        {
            string message;
            switch (game.LastAdventurePlantingFailure)
            {
                case AdventurePlantingFailure.NeedsLilyPad: message = "水面上需要先种睡莲。"; break;
                case AdventurePlantingFailure.NeedsFlowerPot: message = "屋顶上需要先放花盆。"; break;
                case AdventurePlantingFailure.NeedsWater: message = "这种植物需要种在水里。"; break;
                case AdventurePlantingFailure.NeedsSleepingMushroom: message = "咖啡豆需要放在睡着的蘑菇上。"; break;
                case AdventurePlantingFailure.Grave: message = "这里有墓碑，试试墓碑吞噬者。"; break;
                case AdventurePlantingFailure.Occupied: message = "这个位置已经有植物了。"; break;
                case AdventurePlantingFailure.Sun: message = "阳光不足。"; break;
                case AdventurePlantingFailure.Cooldown: message = "种子还没有恢复。"; break;
                case AdventurePlantingFailure.BowlingLine: message = "请在左侧三列放出坚果。"; break;
                case AdventurePlantingFailure.Tutorial: message = AdventureTutorialText(game.Adventure.Tutorial); break;
                default: message = "这里暂时不能种植。"; break;
            }
            AdventureFeedback(message);
        }
    }
}
