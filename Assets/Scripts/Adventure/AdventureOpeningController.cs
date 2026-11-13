#nullable enable
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Runs the pre-level street preview, Crazy Dave dialogue, packet upgrade offer, vase interlude and bowling shovel lesson.</summary>
    internal sealed class AdventureOpeningController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose board, seed chooser and Dave the opening drives.</param>
        internal AdventureOpeningController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores adventure intro.</summary>
        internal AdventureIntroClock? _adventureIntro;

        /// <summary>Gets the adventure intro, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        internal AdventureIntroClock RequiredAdventureIntro => _adventureIntro ?? throw new System.InvalidOperationException("The member '_adventureIntro' is not available.");
        /// <summary>Stores adventure intro frame.</summary>
        internal AdventureIntroFrame? _adventureIntroFrame;

        /// <summary>Gets the adventure intro frame, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        internal AdventureIntroFrame RequiredAdventureIntroFrame => _adventureIntroFrame ?? throw new System.InvalidOperationException("The member '_adventureIntroFrame' is not available.");
        /// <summary>Stores adventure preview.</summary>
        internal readonly List<AdventureIntroPreviewZombie> _adventurePreview = new List<AdventureIntroPreviewZombie>();
        /// <summary>Stores adventure intro remainder.</summary>
        private double _adventureIntroRemainder;
        /// <summary>Stores adventure intro visual age.</summary>
        internal float _adventureIntroVisualAge;
        /// <summary>Stores adventure intro drawn.</summary>
        internal bool _adventureIntroDrawn;
        /// <summary>The first supported original opening entry, or -1 when absent.</summary>
        private int _crazyDaveStartIndex = -1;
        /// <summary>Whether Dave is presenting an adventure vase stage transition.</summary>
        internal bool _vaseDialogueActive;
        /// <summary>Stores fractional native ticks for the vase transition animation.</summary>
        private double _vaseDialogueRemainder;
        /// <summary>Whether the original packet upgrade confirmation is awaiting a choice.</summary>
        internal bool _packetUpgradeOfferActive;
        /// <summary>The original packet upgrade confirmation's affirmative button bounds.</summary>
        internal static readonly Rect s_packetOfferYes = new Rect(365, 346, 135, 46);
        /// <summary>The original packet upgrade confirmation's negative button bounds.</summary>
        internal static readonly Rect s_packetOfferNo = new Rect(540, 346, 135, 46);
        /// <summary>Whether Dave has left while the player completes the first shovel lesson.</summary>
        internal bool _bowlingShovelLessonActive;
        /// <summary>Whether Dave has introduced the adventure bowling boundary.</summary>
        internal bool _bowlingLineVisible;
        /// <summary>Whether the current adventure board follows a loss or restart.</summary>
        internal bool _adventureOpeningIsRestart;
        /// <summary>Whether the final-boss introduction has reached Dave's abduction.</summary>
        internal bool _bossDaveGrabbed;
        /// <summary>Gets or sets adventure intro active.</summary>
        internal bool AdventureIntroActive
        {
            get
            {
                return _adventureIntro != null && !_adventureIntro.Complete;
            }
        }

        /// <summary>Gets or sets adventure intro awaiting seeds.</summary>
        internal bool AdventureIntroAwaitingSeeds
        {
            get
            {
                return AdventureIntroActive && RequiredAdventureIntro.WaitingForSeeds;
            }
        }

        /// <summary>Performs initialize adventure opening.</summary>
        internal void InitializeAdventureOpening()
        {
            _adventureIntro = null;
            _adventureIntroFrame = null;
            _adventurePreview.Clear();
            _adventureIntroRemainder = 0;
            _adventureIntroVisualAge = 0;
            _adventureIntroDrawn = false;
            _crazyDaveStartIndex = -1;
            _owner.Dave._crazyDaveMessageIndex = -1;
            _owner.Dave._crazyDaveText = "";
            _owner.Dave._crazyDaveAnimation = null;
            _bowlingShovelLessonActive = false;
            _packetUpgradeOfferActive = false;
            _bowlingLineVisible = false;
            _bossDaveGrabbed = false;
            _vaseDialogueActive = false;
            _vaseDialogueRemainder = 0;
            _owner.Dave._crazyDaveHeldItem = null;
            _owner.Dave._crazyDaveIdleMouth = null;
            _owner.Dave._crazyDaveBlinkAge = -1f;
            _owner.Dave._crazyDaveBlinkTicks = UnityEngine.Random.Range(400, 801);
            _owner.Dave._crazyDaveVoice.Stop();
            // Older explicitly isolated feature probes start at the battle they are inspecting.
            // The new opening probe and normal player always use the complete original opening.
            if (_owner._game == null || !_owner._game.IsAdventure || _owner.Campaign._adventureTestBoard || _owner._smokeMode && Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-intro-audio-smoke") < 0)
            {
                return;
            }

            if (_owner._game.ModeSession != null)
            {
                InitializeIndependentPuzzleOpening();
                return;
            }

            var level = _owner._game.Adventure.Level;
            _bowlingLineVisible = level.Mode == AdventureMode.Bowling && level.IsReplay;
            var account = _owner.Accounts.GetSelectedAccount();
            var upgradeCount = account?.PurchaseCount(21) ?? -1;
            var canOffer = account != null && account.PacketUpgradeDeclines < 2
                && account.Coins >= ShopCatalog.Price(ShopCatalog.Get(21), account);
            var start = CrazyDaveDialogue.SelectAdventureStart(level.Number, !level.IsReplay, level.Mode,
                canOffer && upgradeCount == 0, canOffer && upgradeCount == 1, _adventureOpeningIsRestart);
            // Enable sequences whose original opening actions are implemented.
            if (CrazyDaveDialogue.IsSupportedAdventureOpening(start))
            {
                var key = "CRAZY_DAVE_" + start;
                var first = _owner._presenter.OriginalProgressionText(key);
                if (!string.IsNullOrEmpty(first) && first != key)
                {
                    _crazyDaveStartIndex = start;
                }
                else
                {
                    Debug.LogWarning($"{nameof(PvZGame)}: Original Crazy Dave text {key} is missing.");
                }
            }

            var context = new AdventureIntroContext
            {
                Level = level.Number,
                FirstAdventure = !level.IsReplay,
                NativeMode = 0,
                NativeBackground = (int)level.Terrain,
                IsConveyor = level.HasConveyor,
                IsStormyNight = level.Mode == AdventureMode.Storm,
                IsFinalBoss = level.Mode == AdventureMode.Boss,
                IsLittleTrouble = level.Mode == AdventureMode.LittleTrouble,
                HasGraveStones = _owner._game.Adventure.Graves.Count > 0,
                CanShowAlmanacOrStore = level.IsReplay || level.Number >= 15,
                ChooseSeedsOverride = level.ChoosesSeeds,
                HasCrazyDaveDialog = _crazyDaveStartIndex >= 0
            };
            _adventureIntro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(context));
            _owner.RequiredGame.BeginAdventureFogIntro();
            _owner._game.Adventure.GravesEnabled = _adventureIntro.Plan.GraveStoneTime == 0
                && !(level.Mode == AdventureMode.WhackAZombie && _crazyDaveStartIndex == 401);
            _adventureIntroFrame = _adventureIntro.Frame;
            if (_adventureIntro.Plan.ChooseSeeds)
            {
                _owner._game.Adventure.Phase = AdventurePhase.ChoosingSeeds;
                _owner._choosingSeeds = true;
                var savedSelection = _owner.Campaign._adventureCampaign != null && _owner.Campaign._adventureCampaign.Progress.SelectedSeeds.Count > 0;
                if (!savedSelection)
                {
                    _owner._pendingSeeds = new AdventureSeedChooserState(_owner._game.Adventure.RequiredSeeds);
                }
            }

            var waves = new List<int[]>();
            foreach (var wave in _owner._game.Adventure.Waves)
            {
                var kinds = new int[wave.Zombies.Count];
                for (var i = 0; i < kinds.Length; i++)
                {
                    kinds[i] = (int)wave.Zombies[i];
                }

                waves.Add(kinds);
            }

            // Original MT, isolated from battle's already-created wave list. Full native RNG draw order is not claimed.
            var previewRandom = new OriginalRandomAdapter(unchecked((uint)(level.Number * 104729 + level.Cycle * 31)));
            _adventurePreview.AddRange(AdventureIntroPresentation.CreateStreetPreview(context, waves, bound => previewRandom.Next(bound), limit => (float)previewRandom.NextDouble() * limit, (kind, clip) => kind < 26 && _owner.AnimationCatalog.AdventureClipExists(AdventureAnimationCatalog.AdventureZombieAnimation((ZombieKind)kind), clip)));
            _adventurePreview.Sort((a, b) => a.RenderOrder.CompareTo(b.RenderOrder));
        }

        /// <summary>Starts original dialogue for the first independent puzzle levels.</summary>
        private void InitializeIndependentPuzzleOpening()
        {
            var mode = _owner.RequiredGame.RequiredModeSession.Definition.Id;
            // PC CutScene::StartLevelIntro 0x43A900: I, Zombie always speaks;
            // Vasebreaker speaks only while its first level has not been beaten.
            var start = mode == 61 ? 2200 : mode == 51 && _owner.IndependentModes._modeFirstClear ? 3000 : -1;
            if (start < 0)
            {
                return;
            }

            var key = "CRAZY_DAVE_" + start;
            var first = _owner._presenter.OriginalProgressionText(key);
            if (string.IsNullOrEmpty(first) || first == key)
            {
                Debug.LogWarning($"{nameof(PvZGame)}: Original Crazy Dave text {key} is missing.");
                return;
            }

            _crazyDaveStartIndex = start;
            var context = new AdventureIntroContext
            {
                Level = 0,
                NativeMode = mode,
                NativeBackground = (int)_owner.RequiredGame.Adventure.Level.Terrain,
                FirstAdventure = false,
                ChooseSeedsOverride = false,
                HasCrazyDaveDialog = true
            };
            _adventureIntro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(context));
            _adventureIntroFrame = _adventureIntro.Frame;
        }

        /// <summary>Reuses the intro event callback across native simulation ticks.</summary>
        internal Action<AdventureIntroEvent> _adventureIntroEventHandler = null!;
        /// <summary>Performs update adventure opening.</summary>
        /// <param name = "dt">The dt.</param>
        /// <returns>The resulting update adventure opening.</returns>
        internal bool UpdateAdventureOpening(float dt)
        {
            if (!AdventureIntroActive || _owner._currentView != PvZGame.ScreenView.Board)
            {
                if (_owner._currentView == PvZGame.ScreenView.Board && _owner._choosingSeeds
                    && !_owner.RequiredOptionsView.IsOpen && !_owner._confirmingQuit && _owner._unavailableFeature == null)
                {
                    var session = _owner.RequiredGame.ModeSession;
                    var repick = session != null && session.Definition.Survival && session.Stage > 1;
                    _owner.RequiredGame.AdvanceAdventurePoolPresentation(dt, repick);
                }

                return false;
            }

            if (!_adventureIntroDrawn || _owner.RequiredOptionsView.IsOpen || _owner._confirmingQuit || _owner._unavailableFeature != null || _owner.RequiredGame.State == MatchState.Paused)
            {
                return true;
            }

            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt))
            {
                return true;
            }

            _adventureIntroRemainder += dt;
            while (_adventureIntroRemainder + .000000001 >= (double).01f && !RequiredAdventureIntro.Complete)
            {
                _adventureIntroRemainder -= (double).01f;
                _adventureIntroVisualAge += .01f;
                _owner.RequiredGame.AdvanceAdventurePoolEffect(RequiredAdventureIntro.Plan.RequiredContext.SurvivalRepick);
                _owner.Dave.UpdateCrazyDaveAnimation(.01f);
                _owner.RequiredGame.AdvanceAdventureGraves();
                RequiredAdventureIntro.Tick(dialogActive: _owner.Dave._crazyDaveMessageIndex >= 0, shovelTutorialActive: _bowlingShovelLessonActive, emit: _adventureIntroEventHandler);
                var plan = RequiredAdventureIntro.Plan;
                _owner.RequiredGame.AdvanceAdventureFog(true,
                    RequiredAdventureIntro.NativeTime > 5950 + plan.DaveTime + plan.SodTime + plan.GraveStoneTime);
                if (_crazyDaveStartIndex >= 0 && RequiredAdventureIntro.NativeTime == 2000 && _owner.Dave._crazyDaveAnimation == null)
                {
                    _owner.Dave.SetCrazyDaveAnimation("anim_enter", 24f, false);
                }

                if (_crazyDaveStartIndex >= 0 && RequiredAdventureIntro.NativeTime == 2750 && _owner.Dave._crazyDaveMessageIndex < 0)
                {
                    var index = _crazyDaveStartIndex;
                    _crazyDaveStartIndex = -1;
                    _owner.Dave.ShowCrazyDaveMessage(index);
                }

                _adventureIntroFrame = RequiredAdventureIntro.Frame;
            }

            return true;
        }

        /// <summary>Advances the displayed opening dialogue or starts Dave's departure.</summary>
        internal void AdvanceCrazyDaveDialogue()
        {
            if (AdventureIntroActive && _owner.RequiredGame.ModeSession == null && _owner.Dave._crazyDaveMessageIndex == 2406)
            {
                // PC CutScene::AdvanceCrazyDaveDialog starts the shovel lesson here.
                _bowlingShovelLessonActive = true;
                _owner.Dave.LeaveCrazyDaveDialogue();
                return;
            }

            var next = CrazyDaveDialogue.NextMessageIndex(_owner.Dave._crazyDaveMessageIndex, index =>
            {
                var key = "CRAZY_DAVE_" + index;
                var value = _owner._presenter.OriginalProgressionText(key);
                return !string.IsNullOrEmpty(value) && value != key;
            });
            if (next >= 0)
            {
                if (next == 2411 && AdventureIntroActive && _owner.RequiredGame.ModeSession == null)
                {
                    _bowlingLineVisible = true;
                }

                if (next == 406 && _owner.RequiredGame.Adventure.Level.Mode == AdventureMode.WhackAZombie)
                {
                    _owner.RequiredGame.Adventure.GravesEnabled = true;
                }

                if (_vaseDialogueActive && _owner.RequiredGame.PopulateAdventureVaseInterlude(next))
                {
                    _owner.Sounds.PlayPresentationSound("plant");
                }

                _owner.Dave.ShowCrazyDaveMessage(next);
                if (next == 1503 || next == 1553)
                {
                    _packetUpgradeOfferActive = true;
                }

                return;
            }

            if (_vaseDialogueActive)
            {
                // If an incomplete string table omitted the population entry, do not strand the level.
                _owner.RequiredGame.PopulateAdventureVaseInterlude(_owner.RequiredGame.Adventure.VaseStage == 1 ? 2702 : 2801);
            }

            if (_owner.RequiredGame.Adventure.Level.Mode == AdventureMode.WhackAZombie && !_owner.RequiredGame.Adventure.GravesEnabled)
            {
                // A truncated original string table must not leave the graves permanently hidden.
                _owner.RequiredGame.Adventure.GravesEnabled = true;
            }

            var bossOpening = AdventureIntroActive && _owner.RequiredGame.ModeSession == null && _owner.RequiredGame.Adventure.Level.Mode == AdventureMode.Boss;
            _owner.Dave.LeaveCrazyDaveDialogue();
            if (bossOpening)
            {
                // PC CutScene::AdvanceCrazyDaveDialog replaces Dave's exit with the bungee grab.
                _bossDaveGrabbed = true;
                _owner.Dave.SetCrazyDaveAnimation("anim_grab", 18f, false);
                _owner.Sounds.PlayPresentationSound("bungee_scream");
            }
        }

        /// <summary>Persists an upgrade answer before continuing Dave's original response.</summary>
        /// <param name="accepted">Whether the player accepted the upgrade.</param>
        internal void ResolvePacketUpgradeOffer(bool accepted)
        {
            if (!_packetUpgradeOfferActive || _owner.Accounts._accountStore == null || _owner._profiles == null || !_owner._profiles.SelectedId.HasValue)
            {
                return;
            }

            var offer = _owner.Dave._crazyDaveMessageIndex;
            try
            {
                _owner.Accounts._accountStore.ResolveAdventurePacketOffer(_owner._profiles.SelectedId.Value, accepted);
                if (accepted)
                {
                    var account = _owner.Accounts._accountStore.Get(_owner._profiles.SelectedId.Value);
                    _owner.Campaign._adventureCampaign?.UpdatePacketSlots(account.PacketSlots);
                    _owner.RequiredGame.Rules.AdventurePacketSlots = account.PacketSlots;
                    _owner.RequiredGame.Adventure.Level.SetPacketSlots(account.PacketSlots);
                    _owner._pendingSeeds = new AdventureSeedChooserState(_owner.RequiredGame.Adventure.RequiredSeeds);
                }

                _packetUpgradeOfferActive = false;
                _owner.Accounts._displayAccountSnapshot = null;
                _owner.Accounts._progressionMessage = null;
                _owner.Dave.ShowCrazyDaveMessage(offer == 1503 ? accepted ? 1510 : 1520 : accepted ? 1560 : 1570);
            }
            catch (Exception e)
            {
                _owner.Accounts._progressionMessage = e.Message;
                Debug.LogWarning($"{nameof(PvZGame)}: Failed to save the packet upgrade choice: {e.Message}");
            }
        }

        /// <summary>Starts the original adventure Vasebreaker conversation after a stage clears.</summary>
        /// <param name="start">The first original message index.</param>
        internal void StartVaseInterlude(int start)
        {
            var key = "CRAZY_DAVE_" + start;
            var message = _owner._presenter.OriginalProgressionText(key);
            if (string.IsNullOrEmpty(message) || message == key)
            {
                Debug.LogWarning($"{nameof(PvZGame)}: Original Crazy Dave text {key} is missing.");
                _owner.RequiredGame.PopulateAdventureVaseInterlude(start == 2700 ? 2702 : 2801);
                _owner.RequiredGame.FinishAdventureVaseInterlude();
                return;
            }

            _vaseDialogueActive = true;
            _vaseDialogueRemainder = 0;
            _owner.Router.CancelBoardSelection();
            _owner.Sounds.PlayPresentationSound("hugewave");
            _owner.Dave.SetCrazyDaveAnimation("anim_enter", 24f, false);
            _owner.Dave.ShowCrazyDaveMessage(start);
        }

        /// <summary>Updates Dave until he has left the vase stage transition.</summary>
        /// <param name="delta">The elapsed frame time.</param>
        internal void UpdateVaseInterlude(float delta)
        {
            if (!_vaseDialogueActive || _owner._currentView != PvZGame.ScreenView.Board || _owner.RequiredOptionsView.IsOpen || _owner._confirmingQuit
                || _owner._unavailableFeature != null || _owner.RequiredGame.State == MatchState.Paused || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta))
            {
                return;
            }

            _vaseDialogueRemainder += delta;
            while (_vaseDialogueRemainder + .000000001 >= (double).01f && _vaseDialogueActive)
            {
                _vaseDialogueRemainder -= (double).01f;
                _owner.Dave.UpdateCrazyDaveAnimation(.01f);
                if (_owner.Dave._crazyDaveAnimation == null)
                {
                    _owner.RequiredGame.FinishAdventureVaseInterlude();
                    _vaseDialogueActive = false;
                }
            }
        }

        /// <summary>Performs handle adventure intro event.</summary>
        /// <param name = "item">The item.</param>
        internal void HandleAdventureIntroEvent(AdventureIntroEvent item)
        {
            if (item == AdventureIntroEvent.GraveStonesEnabled)
            {
                _owner.RequiredGame.Adventure.GravesEnabled = true;
                for (var graveIndex = 0; graveIndex < _owner.RequiredGame.Adventure.Graves.Count; graveIndex++)
                {
                    var grave = _owner.RequiredGame.Adventure.Graves[graveIndex];
                    _owner.Sounds.PlayPresentationSound("dirt_rise");
                }
            }

            if (item == AdventureIntroEvent.ReadySetPlantStarted)
            {
                _owner.Sounds.PlayAdventureRewardSound("readysetplant.ogg");
            }

            if (item == AdventureIntroEvent.SodStarted)
            {
                _owner.Sounds.PlayAdventureRewardSound("roll_in.ogg");
            }

            if (item == AdventureIntroEvent.SeedChooserOpened)
            {
                _owner._choosingSeeds = true;
            }

            if (item == AdventureIntroEvent.IntroFinished)
            {
                _owner._choosingSeeds = false;
                _adventurePreview.Clear();
            }
        }

        /// <summary>Performs begin adventure opening return.</summary>
        internal void BeginAdventureOpeningReturn()
        {
            if (_adventureIntro == null)
            {
                return;
            }

            _adventureIntro.BeginReturn();
            _adventureIntroFrame = _adventureIntro.Frame;
        }

        /// <summary>Performs adventure campaign chooser rect.</summary>
        /// <param name = "seed">The seed.</param>
        /// <returns>The resulting adventure campaign chooser rect.</returns>
        internal Rect AdventureCampaignChooserRect(PlantKind seed)
        {
            if (_adventureIntro == null)
            {
                var index = 0;
                var availableSeeds = _owner.RequiredGame.Adventure.AvailableSeeds;
                for (var seedIndex = 0; seedIndex < availableSeeds.Count; seedIndex++)
                {
                    var kind = availableSeeds[seedIndex];
                    if (kind == seed)
                    {
                        break;
                    }

                    index++;
                }

                return BoardLayout.AdventureChooserRect(index);
            }

            var id = (int)seed;
            if (id == 48)
            {
                return new Rect(468, 515, 50, 70);
            }

            var upgrades = _owner.RequiredGame.Adventure.Level.IsReplay;
            var chooserSeeds = _owner.RequiredGame.Adventure.AvailableSeeds;
            for (var seedIndex = 0; seedIndex < chooserSeeds.Count; seedIndex++)
            {
                var kind = chooserSeeds[seedIndex];
                if ((int)kind >= 40 && (int)kind < 48)
                {
                    upgrades = true;
                }
            }

            return new Rect(22 + id % 8 * 53, (upgrades ? 123 : 128) + id / 8 * (upgrades ? 70 : 73), 50, 70);
        }

        /// <summary>Gets or sets adventure chooser start rect.</summary>
        internal Rect AdventureChooserStartRect
        {
            get
            {
                return _adventureIntro != null ? new Rect(154, 545, 156, 42) : new Rect(300, 533, 200, 34);
            }
        }

        /// <summary>Handles only the original shovel interaction while the opening is paused.</summary>
        /// <param name="point">The logical board pointer position.</param>
        internal void HandleBowlingShovelLesson(Vector2 point)
        {
            if (_owner.Dave._crazyDaveAnimation != null)
            {
                return;
            }

            if (_owner.Layout.AdventureShovelButton.Contains(point))
            {
                _owner._shovel = !_owner._shovel;
                _owner.Sounds.PlayPresentationSound("shovel");
                return;
            }

            if (!_owner._shovel || !_owner.RequiredGame.TryShovelAdventurePlant(_owner.Layout.AdventureShovelTarget(point)))
            {
                return;
            }

            _owner._shovel = false;
            _owner.Sounds.PlayPresentationSound("plant");
            if (_owner.RequiredGame.Adventure.Tutorial == AdventureTutorial.Complete)
            {
                _bowlingShovelLessonActive = false;
                _crazyDaveStartIndex = 2410;
                RequiredAdventureIntro.RestartAfterShovelTutorial();
                _adventureIntroFrame = RequiredAdventureIntro.Frame;
            }
        }
    }
}
