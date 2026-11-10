using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using PvZReplica.Adventure;
using PvZReplica.Reanimation;
using PvZReplica.VerifiedData;
using PvZReplica.Audio;
using PvZReplica.UI;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private OriginalParticleRenderer awardParticles;
        private GameSimulation awardPresentedGame;
        private AdventureRewardPresentation awardIntroductionAudio;
        private readonly Dictionary<string, AudioClip> awardSounds = new Dictionary<string, AudioClip>();
        private static readonly Rect AwardSeedNext = new Rect(324, 546, 156, 42);
        private static readonly Rect AwardMenu = new Rect(677, 553, 111, 26);
        private IEnumerator LoadAdventureRewardSounds()
        {
            foreach (string file in new[] { "seedlift.ogg", "tap2.ogg", "lightfill.ogg", "readysetplant.ogg", "roll_in.ogg", "paper.ogg", "buttonclick.ogg", "shovel.ogg" })
            {
                string location = Path.Combine(root, "sounds", file);
                if (!File.Exists(location)) continue;
                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(new Uri(location).AbsoluteUri, AudioType.OGGVORBIS))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success) awardSounds[file] = DownloadHandlerAudioClip.GetContent(request);
                }
            }
        }
        private void PlayAdventureRewardSound(string file)
        {
            AudioClip sound;
            if (awardSounds.TryGetValue(file, out sound) && sound != null) audioSource.PlayOneShot(sound);
        }
        private void DrawAdventureReward()
        {
            var reward = game.Adventure.RewardPresentation;
            if (reward == null) return;
            if (!ReferenceEquals(awardPresentedGame, game)) { awardPresentedGame = game; awardParticles = new OriginalParticleRenderer(root, animations); }
            awardParticles.BeginFrame();
            if (reward.Stage == AdventureAwardStage.Introduction) DrawAdventureAwardIntroduction(reward);
            else
            {
                float fraction = reward.RenderFraction;
                float x = Mathf.Lerp(reward.PreviousX, reward.X, fraction), y = Mathf.Lerp(reward.PreviousY, reward.Y, fraction);
                float size = Mathf.Lerp(reward.PreviousScale, reward.Scale, fraction);
                Vector2 center = new Vector2(x + reward.Width * .5f, y + reward.Height * .5f);
                if (reward.Stage == AdventureAwardStage.Collecting)
                {
                    // PC particle table 0x74C8D8: effect 47 is Award.xml. Its attachment follows
                    // the packet centre without inheriting the packet enlargement (0x43D740/43C570).
                    awardParticles.Draw("award-rays", "Award", (reward.CollectionTicks + fraction) * .01f, center, 571);

                }
                if (reward.OnGround)
                    awardParticles.Draw("award-pickup-arrow", reward.NativeType == 7 ? "SeedPacket" : "AwardPickupArrow",
                        reward.GroundTicks * .01f, new Vector2(x + reward.Width / 2, y + reward.Height / 2 - 60), 570);
                if (reward.NativeType == 7 && game.Adventure.Level.RewardSeed.HasValue)
                    DrawAdventureCard(game.Adventure.Level.RewardSeed.Value, new Rect(center.x - 25 * size, center.y - 35 * size, 50 * size, 70 * size), true);
                else DrawAdventureAwardObject(reward, center, size);
                // Starburst is native render order coin+1; the attached Award rays are coin-1.
                if (reward.Stage == AdventureAwardStage.Collecting)
                    awardParticles.Draw("award-starburst", "Starburst", (reward.CollectionTicks + fraction) * .01f,
                        new Vector2(reward.CollectionStartX + 30, reward.CollectionStartY + 30), 572);
                if (reward.FadeAlpha > 0) Fill(new Rect(0, 0, 800, 600), new Color(reward.NativeType == 15 ? 0 : 1,
                    reward.NativeType == 15 ? 0 : 1, reward.NativeType == 15 ? 0 : 1, reward.FadeAlpha));
            }
            awardParticles.EndFrame();
            if (!string.IsNullOrEmpty(adventureFeedback) && Time.unscaledTime < adventureFeedbackUntil)
                Label(new Rect(140, 110, 520, 70), adventureFeedback, 17, Color.yellow, TextAnchor.MiddleCenter, true);
        }
        private void DrawAdventureAwardButton(Rect rect, bool enabled)
        {
            DrawImage(enabled ? "SeedChooser_Button.png" : "SeedChooser_Button_Disabled.png", rect);
            if (enabled && rect.Contains(adventurePointer)) DrawImage("SeedChooser_Button_Glow.png", rect);
        }
        private bool TryResolveAdventureRewardMusic(out MusicCue cue)
        {
            cue = MusicCue.None;
            if (currentView != ScreenView.Board || game == null || !game.IsAdventure || IsModeBoard) return false;
            AdventureRewardPresentation reward = game.Adventure.RewardPresentation;
            if (reward == null || reward.Stage != AdventureAwardStage.Introduction) return false;
            bool paper = reward.NativeType == 15 || reward.NativeType == 18 && game.Adventure.Level.Number == 50;
            // PC AwardScreen ctor 0x40FB7A..97 starts tune8 at order221; paper remains silent.
            cue = paper ? MusicCue.None : MusicCue.ZenGarden;
            return true;
        }
        private void HandleAdventureRewardNoticeSound(AdventureNotice notice)
        {
            if (notice.Kind == AdventureNoticeKind.RewardFade) { PlayAdventureRewardSound("lightfill.ogg"); return; }
            if (notice.Kind != AdventureNoticeKind.RewardCollected || game.Adventure.RewardPresentation == null) return;
            int level = game.Adventure.Level.Number;
            if (!game.Adventure.Level.IsReplay && level == 4) PlayAdventureRewardSound("shovel.ogg");
            else if (!game.Adventure.Level.IsReplay && (level == 24 || level == 34 || level == 44)) PlayAdventureRewardSound("tap2.ogg");
            else { PlayAdventureRewardSound("seedlift.ogg"); PlayAdventureRewardSound("tap2.ogg"); }
        }
        private string AdventureAwardImage(int nativeType, bool highResolution = false)
        {
            switch (nativeType)
            {
                case 9: return highResolution ? "Shovel_hi_res.png" : "Shovel.png";
                case 10: return "Almanac.png";
                case 11: return "CarKeys.png";
                case 13: return "WateringCan.png";
                case 14: return "Taco.png";
                case 15: return "ZombieNoteSmall.png";
                case 21: return "Sunflower_trophy.png";
                default: return highResolution ? "moneybag_hi_res.png" : "moneybag.png";
            }
        }
        private void DrawAdventureAwardObject(AdventureRewardPresentation award, Vector2 center, float size)
        {
            if (award.NativeType == 15 && award.Stage == AdventureAwardStage.Collecting) return;
            string image = AdventureAwardImage(award.NativeType);
            Texture2D source = Texture(image);
            if (source == null) return;
            int columns = award.NativeType == 21 ? 2 : 1;
            Rect destination = new Rect(center.x - source.width * size / columns / 2, center.y - source.height * size / 2, source.width * size / columns, source.height * size);
            GUI.DrawTextureWithTexCoords(destination, source, new Rect(0, 0, 1f / columns, 1));
        }
        private void DrawAdventureAwardIntroduction(AdventureRewardPresentation award)
        {
            var level = game.Adventure.Level;
            if (award.NativeType == 15 || award.NativeType == 18 && level.Number == 50) { DrawAdventureNoteIntroduction(award); return; }
            if (level.IsReplay && award.NativeType == 18 && level.Number < 50) return;
            // PC app+0x884 is ADManager, not WidgetManager. 0x405750 loads
            // images/ad/game_pass.jpg into ADManager+104/+4; 0x40FCF0 draws it at(0,0,800,300).
            Texture2D background = Texture("AwardScreen_Back_new.jpg");
            if (background != null) GUI.DrawTexture(new Rect(0, 0, 800, 600), background);
            DrawImage("ad/game_pass.jpg", new Rect(0, 0, 800, 300));
            string title = "", name = "", description = "";
            if (award.NativeType == 7 && level.RewardSeed.HasValue)
            {
                var kind = level.RewardSeed.Value;
                title = OriginalProgressionText("NEW_PLANT"); name = AdventurePlantName(kind);
                description = OriginalProgressionText(VerifiedDataCatalog.Plants[(int)kind].InternalName + "_TOOLTIP");
                // Original PC DrawAwardSeed 0x40FE70: 2x packet at (190,360).
                DrawAdventureCard(kind, new Rect(190, 360, 100, 140), true);
            }
            else
            {
                AdventureAwardText(award.NativeType, out title, out name, out description);
                Texture2D icon = Texture(AdventureAwardImage(award.NativeType, true));
                if (icon != null) GUI.DrawTexture(new Rect(240 - icon.width * .5f, 426 - icon.height * .5f, icon.width, icon.height), icon);
            }
            Color gold = new Color(213f / 255, 159f / 255, 43f / 255);
            // PC 0x40FCF0 fonts: Dwarven24 / Dwarven18Yellow / Brianne16.
            if (!OriginalUiWidgets.TextAtBaseline(title, 567, 356, 320, "DwarvenTodcraft24", gold))
                Label(new Rect(407, 327, 320, 40), title, 24, gold, TextAnchor.MiddleCenter, true);
            if (!OriginalUiWidgets.TextAtBaseline(name, 559, 392, 290, "DwarvenTodcraft18Yellow", Color.white))
                Label(new Rect(414, 369, 290, 31), name, 18, Color.yellow, TextAnchor.MiddleCenter, true);
            if (label == null) label = new GUIStyle(GUI.skin.label);
            bool oldWrap = label.wordWrap; label.wordWrap = true;
            Color ink = new Color(40f / 255, 50f / 255, 90f / 255);
            if (!OriginalUiWidgets.Text(new Rect(444, 416, 230, 90), description, "BrianneTod16", ink, true, true))
                Label(new Rect(444, 416, 230, 90), description, 16, ink, TextAnchor.MiddleCenter, false);
            label.wordWrap = oldWrap;
            Rect next = AwardSeedNext;
            DrawAdventureAwardButton(next, award.IntroductionTicks >= 180);
            string caption = OriginalProgressionText("NEXT_LEVEL_BUTTON");
            if (!OriginalUiWidgets.TextAtBaseline(caption, next.center.x, next.y + 27, next.width, "DwarvenTodcraft15", gold))
                Label(next, caption, 18, gold, TextAnchor.MiddleCenter, true);
            DrawImage("Almanac_CloseButton.png", AwardMenu);
            string menuText = OriginalProgressionText("AWARD_MAIN_MENU_BUTTON");
            if (!OriginalUiWidgets.TextAtBaseline(menuText, AwardMenu.center.x, AwardMenu.y + 17, AwardMenu.width, "BrianneTod12", new Color(42f / 255, 42f / 255, 90f / 255)))
                Label(AwardMenu, menuText, 12, new Color(42f / 255, 42f / 255, 90f / 255), TextAnchor.MiddleCenter, true);
            if (award.IntroductionAlpha > 0) Fill(new Rect(0, 0, 800, 600), new Color(1, 1, 1, award.IntroductionAlpha));
        }
        private void DrawAdventureNoteIntroduction(AdventureRewardPresentation award)
        {
            int note = award.NativeType == 18 ? 4 : game.Adventure.Level.Number / 10;
            DrawImage(note % 2 == 0 ? "background2.jpg" : "background1.jpg", new Rect(-700, -300, 2800, 1200));
            Texture2D paper = Texture("ZombieNote.jpg");
            if (paper != null) GUI.DrawTexture(new Rect(80, 80, paper.width, paper.height), paper);
            string writing = note < 4 ? "ZombieNote" + (note + 1) + ".png" : "Credits_ZombieNote.png";
            Texture2D ink = Texture(writing);
            int x = note == 1 ? 133 : note == 2 ? 120 : note == 3 ? 102 : note == 4 ? 114 : 131;
            int y = note == 1 ? 127 : note == 2 || note == 3 ? 117 : note == 4 ? 138 : 132;
            if (ink != null) GUI.DrawTexture(new Rect(x, y, ink.width, ink.height), ink);
            Label(new Rect(200, 40, 400, 40), OriginalProgressionText("FOUND_NOTE"), 22, new Color(1, 200f / 255, 0), TextAnchor.MiddleCenter, true);
            Rect next = new Rect(324, 500, 156, 42);
            DrawAdventureAwardButton(next, award.IntroductionTicks >= 180);
            string continueText = OriginalProgressionText("CONTINUE_BUTTON");
            if (!OriginalUiWidgets.TextAtBaseline(continueText, next.center.x, next.y + 27, next.width, "DwarvenTodcraft15", new Color(213f / 255, 159f / 255, 43f / 255)))
                Label(next, continueText, 18, new Color(213f / 255, 159f / 255, 43f / 255), TextAnchor.MiddleCenter, true);
            if (award.IntroductionAlpha > 0) Fill(new Rect(0, 0, 800, 600), new Color(0, 0, 0, award.IntroductionAlpha));
        }
        private void AdventureAwardText(int kind, out string title, out string name, out string description)
        {
            string titleKey, nameKey, descriptionKey;
            switch (kind)
            {
                case 9: titleKey = "GOT_SHOVEL"; nameKey = "SHOVEL"; descriptionKey = "SHOVEL_DESCRIPTION"; break;
                case 10: titleKey = "FOUND_SUBURBAN_ALMANAC"; nameKey = "SUBURBAN_ALMANAC"; descriptionKey = "SUBURBAN_ALMANAC_DESCRIPTION"; break;
                case 11: titleKey = "FOUND_KEYS"; nameKey = "KEYS"; descriptionKey = "KEYS_DESCRIPTION"; break;
                case 13: titleKey = "FOUND_WATERING_CAN"; nameKey = "WATERING_CAN"; descriptionKey = "WATERING_CAN_DESCRIPTION"; break;
                case 14: titleKey = "FOUND_TACO"; nameKey = "TACO"; descriptionKey = "TACO_DESCRIPTION"; break;
                case 21: titleKey = "WIN_MESSAGE1"; nameKey = "SILVER_SUNFLOWER_TROPHY"; descriptionKey = "WIN_MESSAGE2"; break;
                default: titleKey = "BEAT_GAME_MESSAGE1"; nameKey = "GOLD_SUNFLOWER_TROPHY"; descriptionKey = "BEAT_GAME_MESSAGE2"; break;
            }
            title = OriginalProgressionText(titleKey); name = OriginalProgressionText(nameKey); description = OriginalProgressionText(descriptionKey);
        }
        private void UpdateAdventureRewardContinuation()
        {
            if (currentView != ScreenView.Board || game == null || !game.IsAdventure || IsModeBoard) return;
            var level = game.Adventure.Level; var reward = game.Adventure.RewardPresentation;
            if (reward != null && reward.Stage == AdventureAwardStage.Introduction && !ReferenceEquals(awardIntroductionAudio, reward))
            {
                awardIntroductionAudio = reward;
                if (reward.NativeType == 15 || reward.NativeType == 18 && level.Number == 50) PlayAdventureRewardSound("paper.ogg");
            }
            if (adventureTestBoard || adventureCampaign == null) return;
            // PC LawnApp::CheckForGameEnd 0x45F6E0 skips plant awards on replay, except notes/final awards.
            if (!level.IsReplay || level.Number == 50 || reward == null || reward.NativeType != 18 || reward.Stage != AdventureAwardStage.Introduction) return;
            if (adventureFeedback != null && Time.unscaledTime < adventureFeedbackUntil) return;
            try { if (adventureCampaign.StartNextLevel()) AttachAdventureGame(adventureCampaign.Game); }
            catch (Exception error) { AdventureFeedback("下一关无法开始：" + error.Message, 5f); }
        }
        private void HandleAdventureRewardClick(Vector2 point)
        {
            var reward = game.Adventure.RewardPresentation;
            if (reward == null) return;
            if (reward.Stage != AdventureAwardStage.Introduction)
            {
                if (!reward.Contains(point.x, point.y)) return;
                try
                {
                    bool collected = adventureTestBoard ? game.CollectAdventureReward() : adventureCampaign.CollectReward();
                    if (!collected) AdventureFeedback("奖励尚未保存：" + (adventureCampaign == null ? "请重试" : adventureCampaign.LastPersistenceError), 5f);
                }
                catch (Exception error) { AdventureFeedback("奖励尚未保存，请重试：" + error.Message, 6f); }
                return;
            }
            // PC AwardScreen starts its counter at180; controls enable when it reaches zero.
            if (reward.IntroductionTicks < 180) return;
            if (AwardMenu.Contains(point)) { PlayAdventureRewardSound("buttonclick.ogg"); currentView = ScreenView.Selector; return; }
            Rect next = reward.NativeType == 15 || reward.NativeType == 18 && game.Adventure.Level.Number == 50 ? new Rect(324, 500, 156, 42) : AwardSeedNext;
            if (!next.Contains(point)) return;
            PlayAdventureRewardSound("buttonclick.ogg");
            try
            {
                if (adventureTestBoard || adventureCampaign.Progress.Completed) currentView = ScreenView.Selector;
                else if (adventureCampaign.StartNextLevel()) AttachAdventureGame(adventureCampaign.Game);
            }
            catch (Exception error) { AdventureFeedback("下一关无法开始：" + error.Message, 5f); }
        }
    }
}
