using System;
using System.Collections.Generic;
using UnityEngine;
using PvZReplica.Adventure;
using PvZReplica.Randomness;
using PvZReplica.Reanimation;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private AdventureIntroClock adventureIntro;
        private AdventureIntroFrame adventureIntroFrame;
        private readonly List<AdventureIntroPreviewZombie> adventurePreview = new List<AdventureIntroPreviewZombie>();
        private double adventureIntroRemainder;
        private float adventureIntroVisualAge;
        private bool adventureIntroDrawn;
        private bool AdventureIntroActive { get { return adventureIntro != null && !adventureIntro.Complete; } }
        private bool AdventureIntroAwaitingSeeds { get { return AdventureIntroActive && adventureIntro.WaitingForSeeds; } }
        private void InitializeAdventureOpening()
        {
            adventureIntro = null; adventureIntroFrame = null; adventurePreview.Clear(); adventureIntroRemainder = 0; adventureIntroVisualAge = 0; adventureIntroDrawn = false;
            // Older explicitly isolated feature probes start at the battle they are inspecting.
            // The new opening probe and normal player always use the complete original opening.
            if (game == null || !game.IsAdventure || game.ModeSession != null || adventureTestBoard ||
                smokeMode && Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-intro-audio-smoke") < 0) return;
            var level = game.Adventure.Level;
            var context = new AdventureIntroContext {
                Level = level.Number, FirstAdventure = !level.IsReplay, NativeMode = 0, NativeBackground = (int)level.Terrain,
                IsConveyor = level.HasConveyor, IsStormyNight = level.Mode == AdventureMode.Storm,
                IsFinalBoss = level.Mode == AdventureMode.Boss, IsLittleTrouble = level.Mode == AdventureMode.LittleTrouble,
                HasGraveStones = game.Adventure.Graves.Count > 0, CanShowAlmanacOrStore = level.IsReplay || level.Number >= 15,
                ChooseSeedsOverride = level.ChoosesSeeds, HasCrazyDaveDialog = false
            };
            adventureIntro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(context)); adventureIntroFrame = adventureIntro.Frame;
            if (adventureIntro.Plan.ChooseSeeds)
            {
                game.Adventure.Phase = AdventurePhase.ChoosingSeeds; choosingSeeds = true;
                bool savedSelection = adventureCampaign != null && adventureCampaign.Progress.SelectedSeeds.Count > 0;
                if (!savedSelection) pendingSeeds = new HashSet<PlantKind>(game.Adventure.RequiredSeeds);
            }
            var waves = new List<int[]>();
            foreach (var wave in game.Adventure.Waves) { int[] kinds = new int[wave.Zombies.Count]; for (int i = 0; i < kinds.Length; i++) kinds[i] = (int)wave.Zombies[i]; waves.Add(kinds); }
            // Original MT, isolated from battle's already-created wave list. Full native RNG draw order is not claimed.
            var previewRandom = new OriginalRandomAdapter(unchecked((uint)(level.Number * 104729 + level.Cycle * 31)));
            adventurePreview.AddRange(AdventureIntroPresentation.CreateStreetPreview(context, waves,
                bound => previewRandom.Next(bound), limit => (float)previewRandom.NextDouble() * limit,
                (kind, clip) => kind < 26 && AdventureClipExists(AdventureZombieAnimation((ZombieKind)kind), clip)));
            adventurePreview.Sort((a,b) => a.RenderOrder.CompareTo(b.RenderOrder));
        }
        private bool UpdateAdventureOpening(float dt)
        {
            if (!AdventureIntroActive || currentView != ScreenView.Board) return false;
            if (!adventureIntroDrawn || optionsView.IsOpen || confirmingQuit || unavailableFeature != null || game.State == MatchState.Paused) return true;
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return true;
            adventureIntroRemainder += dt;
            while (adventureIntroRemainder + .000000001 >= (double).01f && !adventureIntro.Complete)
            {
                adventureIntroRemainder -= (double).01f; adventureIntroVisualAge += .01f;
                adventureIntro.Tick(emit: HandleAdventureIntroEvent); adventureIntroFrame = adventureIntro.Frame;
            }
            return true;
        }
        private void HandleAdventureIntroEvent(AdventureIntroEvent item)
        {
            if (item == AdventureIntroEvent.ReadySetPlantStarted) PlayAdventureRewardSound("readysetplant.ogg");
            if (item == AdventureIntroEvent.SodStarted) PlayAdventureRewardSound("roll_in.ogg");
            if (item == AdventureIntroEvent.SeedChooserOpened) choosingSeeds = true;
            if (item == AdventureIntroEvent.IntroFinished) { choosingSeeds = false; adventurePreview.Clear(); }
        }
        private void BeginAdventureOpeningReturn()
        {
            if (adventureIntro == null) return;
            adventureIntro.BeginReturn(); adventureIntroFrame = adventureIntro.Frame;
        }
        private void DrawAdventureOpeningWorld()
        {
            if (adventureIntroFrame == null) return;
            if (Event.current.type == EventType.Repaint) adventureIntroDrawn = true;
            foreach (var blit in adventureIntroFrame.SodBlits)
            {
                if (blit.Width <= 0 || blit.Height <= 0) continue;
                Texture2D image = Texture(blit.Resource + (blit.Resource.StartsWith("background", StringComparison.Ordinal) ? ".jpg" : ".png"));
                if (image == null) continue;
                GUI.DrawTextureWithTexCoords(new Rect(blit.X, blit.Y, blit.Width, blit.Height), image,
                    new Rect(blit.SourceX / (float)image.width, 1f - (blit.SourceY + blit.Height) / (float)image.height,
                        blit.Width / (float)image.width, blit.Height / (float)image.height));
            }
        }
        private void DrawAdventureOpeningActors()
        {
            if (!AdventureIntroActive || adventureIntroFrame == null) return;
            foreach (var preview in adventurePreview)
            {
                if (preview.NativeKind < 0 || preview.NativeKind >= 26) continue;
                var zombie = new Zombie { Id = -1000 - adventurePreview.IndexOf(preview), Kind = (ZombieKind)preview.NativeKind,
                    Health = 270, StartingHealth = 270, ArmorHealth = preview.NativeKind == 2 || preview.NativeKind == 4 ? 100 : 0,
                    ShieldHealth = preview.NativeKind == 5 || preview.NativeKind == 6 || preview.NativeKind == 21 ? 100 : 0,
                    IsSmall = preview.Scale < 1f };
                string asset = AdventureZombieAnimation(zombie.Kind), clip = preview.Animation;
                if (!AdventureClipExists(asset, clip)) clip = AdventureClipExists(asset, "anim_idle") ? "anim_idle" : "anim_walk";
                ReanimationRenderer.NativeAnimationInfo info; float seconds = adventureIntroVisualAge;
                if (animations.TryGetNativeAnimationInfo(asset, clip, out info) && preview.AnimationRate > 0)
                    seconds += preview.InitialPhase * info.Count / preview.AnimationRate;
                DrawAdventureZombiePose(zombie, asset, clip, new Vector2(preview.WorldX, preview.WorldY - preview.Altitude),
                    preview.Scale, seconds, preview.AnimationRate, preview.Loop, "intro-zombie-" + zombie.Id);
            }
            foreach (var animation in adventureIntroFrame.Animations)
                if (animation.Asset != "StartReadySetPlant") animations.DrawNativeDefinition(animation.Asset,
                    new Vector2(animation.X, animation.Y), 1f, animation.Seconds, false, animation.Rate);
            foreach (var particle in adventureIntroFrame.Particles)
                adventureParticles.Draw("opening-" + particle.Asset + "-" + particle.Y, particle.Asset, particle.Seconds,
                    new Vector2(particle.X, particle.Y), (int)particle.Y + 137);
        }
        private bool DrawAdventureOpeningInterface()
        {
            if (!AdventureIntroActive || adventureIntroFrame == null) return false;
            var frame = adventureIntroFrame;
            if (Event.current.type == EventType.Repaint) adventureIntroDrawn = true;
            if (adventureIntro.Plan.DrawSeedBank && frame.SeedBankY > -87)
            {
                DrawAdventureSeedBankBackground(new Vector2(frame.SeedBankX, frame.SeedBankY), deck.Count);
                Label(new Rect(frame.SeedBankX + 8, frame.SeedBankY + 59, 60, 22), game.SunAmount.ToString(), 18, Color.black, TextAnchor.MiddleCenter, true);
                var selectedSeeds = new List<PlantKind>(); foreach (PlantKind seed in game.Adventure.AvailableSeeds) if (pendingSeeds.Contains(seed)) selectedSeeds.Add(seed);
                if (!adventureIntro.Plan.ChooseSeeds) selectedSeeds = deck;
                for (int i = 0; i < selectedSeeds.Count; i++)
                {
                    Rect card = new Rect(AdventureSeedSlotLayout.PositionX(i, Math.Max(deck.Count, selectedSeeds.Count)) + frame.SeedBankX, 8 + frame.SeedBankY, 50, 70);
                    Color old = GUI.color; float shade = frame.SeedBankDarken / 255f; GUI.color = new Color(old.r * shade, old.g * shade, old.b * shade, old.a);
                    DrawAdventureCard(selectedSeeds[i], card, false); GUI.color = old; DrawAdventurePacketCost(selectedSeeds[i], card);
                }
            }
            if (frame.ChooserVisible)
            {
                Matrix4x4 prior = GUI.matrix;
                try { GUI.matrix = prior * Matrix4x4.Translate(new Vector3(0, frame.ChooserY, 0)); DrawAdventureChooser(); }
                finally { GUI.matrix = prior; }
            }
            foreach (var animation in frame.Animations)
                if (animation.Asset == "StartReadySetPlant") animations.DrawNativeDefinition(animation.Asset,
                    new Vector2(animation.X, animation.Y), 1f, animation.Seconds, false, animation.Rate);
            return true;
        }
        private Rect AdventureCampaignChooserRect(PlantKind seed)
        {
            if (adventureIntro == null) { int index = 0; foreach (PlantKind kind in game.Adventure.AvailableSeeds) { if (kind == seed) break; index++; } return AdventureChooserRect(index); }
            int id = (int)seed; if (id == 48) return new Rect(468, 515, 50, 70);
            bool upgrades = game.Adventure.Level.IsReplay; foreach (PlantKind kind in game.Adventure.AvailableSeeds) if ((int)kind >= 40 && (int)kind < 48) upgrades = true;
            return new Rect(22 + id % 8 * 53, (upgrades ? 123 : 128) + id / 8 * (upgrades ? 70 : 73), 50, 70);
        }
        private Rect AdventureChooserStartRect { get { return adventureIntro != null ? new Rect(154, 545, 156, 42) : new Rect(300, 533, 200, 34); } }
    }
}
