#nullable enable
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Audio;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using PlantsVsZombies.UI.BitmapFonts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsVsZombies.Game
{
    /// <summary>
    /// Coordinates one game session: it owns the state shared between screens, runs the per-frame order, and delegates each
    /// responsibility (audio, input, menus, adventure, modes, store, garden, tree) to the collaborators it exposes.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "PvZReplica", "Assembly-CSharp", "PvZGame")]
    public sealed class PvZGame : MonoBehaviour
    {
        /// <summary>Defines screen view.</summary>
        internal enum ScreenView
        {
            /// <summary>Identifies selector.</summary>
            Selector,
            /// <summary>Identifies board.</summary>
            Board,
            /// <summary>Identifies modes.</summary>
            Modes,
            /// <summary>Identifies almanac.</summary>
            Almanac,
            /// <summary>Identifies help.</summary>
            Help,
            /// <summary>Identifies store.</summary>
            Store,
            /// <summary>Identifies garden.</summary>
            Garden,
            /// <summary>Identifies tree.</summary>
            Tree,
            /// <summary>Identifies import.</summary>
            Import
        }

        // Collaborators. Each owns one slice of the session and is created on first use, so a host that never ran Awake
        // (an inactive test fixture, for instance) still reaches a working instance.
        /// <summary>Stores the lazily created one-shot sound effects.</summary>
        private GameSoundEffects? _soundEffects;
        /// <summary>Gets the one-shot sound effects, creating it on first use.</summary>
        internal GameSoundEffects Sounds => _soundEffects ??= new GameSoundEffects(this);

        /// <summary>Stores the lazily created dynamic music player.</summary>
        private DynamicMusicPlayer? _dynamicMusic;
        /// <summary>Gets the dynamic music player, creating it on first use.</summary>
        internal DynamicMusicPlayer Music => _dynamicMusic ??= new DynamicMusicPlayer(this);

        /// <summary>Stores the lazily created Crazy Dave presentation state.</summary>
        private CrazyDaveActor? _dave;
        /// <summary>Gets the Crazy Dave presentation state, creating it on first use.</summary>
        internal CrazyDaveActor Dave => _dave ??= new CrazyDaveActor(this);

        /// <summary>Stores the lazily created original image cache.</summary>
        private TextureCache? _textureCache;
        /// <summary>Gets the original image cache, creating it on first use.</summary>
        internal TextureCache Textures => _textureCache ??= new TextureCache(this);

        /// <summary>Stores the lazily created board layout.</summary>
        private BoardLayout? _layout;
        /// <summary>Gets the board layout, creating it on first use.</summary>
        internal BoardLayout Layout => _layout ??= new BoardLayout(this);

        /// <summary>Stores the lazily created original loading and title-screen controller.</summary>
        private OriginalLoadingController? _loading;
        /// <summary>Gets the original loading and title-screen controller, creating it on first use.</summary>
        internal OriginalLoadingController Loading => _loading ??= new OriginalLoadingController(this);

        /// <summary>Stores the lazily created menu controller.</summary>
        private MenuController? _menus;
        /// <summary>Gets the menu controller, creating it on first use.</summary>
        internal MenuController Menus => _menus ??= new MenuController(this);

        /// <summary>Stores the lazily created pointer and keyboard router.</summary>
        private GameInputRouter? _router;
        /// <summary>Gets the pointer and keyboard router, creating it on first use.</summary>
        internal GameInputRouter Router => _router ??= new GameInputRouter(this);

        /// <summary>Stores the lazily created player account session.</summary>
        private PlayerAccountSession? _accounts;
        /// <summary>Gets the player account session, creating it on first use.</summary>
        internal PlayerAccountSession Accounts => _accounts ??= new PlayerAccountSession(this);

        /// <summary>Stores the lazily created store controller.</summary>
        private CrazyDaveStoreController? _store;
        /// <summary>Gets the store controller, creating it on first use.</summary>
        internal CrazyDaveStoreController Store => _store ??= new CrazyDaveStoreController(this);

        /// <summary>Stores the lazily created Zen Garden controller.</summary>
        private ZenGardenController? _garden;
        /// <summary>Gets the Zen Garden controller, creating it on first use.</summary>
        internal ZenGardenController Garden => _garden ??= new ZenGardenController(this);

        /// <summary>Stores the lazily created Wisdom Tree controller.</summary>
        private WisdomTreeController? _tree;
        /// <summary>Gets the Wisdom Tree controller, creating it on first use.</summary>
        internal WisdomTreeController Tree => _tree ??= new WisdomTreeController(this);

        /// <summary>Stores the lazily created original save utility controller.</summary>
        private OriginalSaveUtilityController? _saveUtility;
        /// <summary>Gets the original save utility controller, creating it on first use.</summary>
        internal OriginalSaveUtilityController SaveUtility => _saveUtility ??= new OriginalSaveUtilityController(this);

        /// <summary>Stores the lazily created adventure campaign controller.</summary>
        private AdventureCampaignController? _campaign;
        /// <summary>Gets the adventure campaign controller, creating it on first use.</summary>
        internal AdventureCampaignController Campaign => _campaign ??= new AdventureCampaignController(this);

        /// <summary>Stores the lazily created adventure opening controller.</summary>
        private AdventureOpeningController? _opening;
        /// <summary>Gets the adventure opening controller, creating it on first use.</summary>
        internal AdventureOpeningController Opening => _opening ??= new AdventureOpeningController(this);

        /// <summary>Stores the lazily created adventure board controller.</summary>
        private AdventureBoardController? _adventureBoard;
        /// <summary>Gets the adventure board controller, creating it on first use.</summary>
        internal AdventureBoardController AdventureBoard => _adventureBoard ??= new AdventureBoardController(this);

        /// <summary>Stores the lazily created adventure reward controller.</summary>
        private AdventureRewardController? _rewards;
        /// <summary>Gets the adventure reward controller, creating it on first use.</summary>
        internal AdventureRewardController Rewards => _rewards ??= new AdventureRewardController(this);

        /// <summary>Stores the lazily created wave notice controller.</summary>
        private AdventureWaveNoticeController? _waveNotices;
        /// <summary>Gets the wave notice controller, creating it on first use.</summary>
        internal AdventureWaveNoticeController WaveNotices => _waveNotices ??= new AdventureWaveNoticeController(this);

        /// <summary>Stores the lazily created adventure animation catalog.</summary>
        private AdventureAnimationCatalog? _animationCatalog;
        /// <summary>Gets the adventure animation catalog, creating it on first use.</summary>
        internal AdventureAnimationCatalog AnimationCatalog => _animationCatalog ??= new AdventureAnimationCatalog(this);

        /// <summary>Stores the lazily created independent mode controller.</summary>
        private IndependentModeController? _independentModes;
        /// <summary>Gets the independent mode controller, creating it on first use.</summary>
        internal IndependentModeController IndependentModes => _independentModes ??= new IndependentModeController(this);

        /// <summary>Owns the scene and Canvas presentation for this game session.</summary>
        internal GamePresenter _presenter = null!;

        /// <summary>Stores deck.</summary>
        internal readonly List<PlantKind> _deck = new List<PlantKind>
        {
            PlantKind.Peashooter,
            PlantKind.Sunflower,
            PlantKind.Wallnut,
            PlantKind.CherryBomb,
            PlantKind.SnowPea,
            PlantKind.PotatoMine,
            PlantKind.Repeater
        };
        /// <summary>Stores available plants.</summary>
        internal readonly PlantKind[] _availablePlants =
        {
            PlantKind.Peashooter,
            PlantKind.Sunflower,
            PlantKind.Wallnut,
            PlantKind.CherryBomb,
            PlantKind.SnowPea,
            PlantKind.PotatoMine,
            PlantKind.Repeater,
            PlantKind.Jalapeno,
            PlantKind.TallNut
        };

        /// <summary>Stores game.</summary>
        internal GameSimulation _game;
        /// <summary>Stores animations.</summary>
        internal ReanimationRenderer _animations;
        /// <summary>Stores selector view.</summary>
        internal SelectorView _selectorView;
        /// <summary>Stores options view.</summary>
        internal OptionsView _optionsView;
        /// <summary>Stores almanac view.</summary>
        internal AlmanacView _almanacView;
        /// <summary>Stores mode view.</summary>
        internal ModeSelectionView _modeView;
        /// <summary>Stores help view.</summary>
        internal HelpView _helpView;
        /// <summary>Stores profiles.</summary>
        internal ProfileCatalog _profiles;
        /// <summary>Stores profile dialog.</summary>
        internal ProfileDialog _profileDialog;
        /// <summary>Stores options bitmap.</summary>
        internal CanvasBitmapFont _optionsBitmap;
        /// <summary>Stores mode title bitmap.</summary>
        internal CanvasBitmapFont _modeTitleBitmap;
        /// <summary>Stores mode label bitmap.</summary>
        internal CanvasBitmapFont _modeLabelBitmap;
        /// <summary>Stores sun counter bitmap.</summary>
        internal CanvasBitmapFont _sunCounterBitmap;
        /// <summary>Stores profile load error.</summary>
        internal string _profileLoadError;
        /// <summary>Stores current view.</summary>
        internal ScreenView _currentView = ScreenView.Selector;

        /// <summary>Stores font.</summary>
        internal Font _font;
        /// <summary>Stores root.</summary>
        internal string _root;
        /// <summary>Stores selected.</summary>
        internal int _selected = -1;
        /// <summary>Stores shovel.</summary>
        internal bool _shovel;
        /// <summary>Stores choosing seeds.</summary>
        internal bool _choosingSeeds;
        /// <summary>Stores pending seeds.</summary>
        internal AdventureSeedChooserState _pendingSeeds;
        /// <summary>Stores unavailable feature.</summary>
        internal string _unavailableFeature;
        /// <summary>Stores unavailable detail.</summary>
        internal string _unavailableDetail = "尚未实现";
        /// <summary>Stores smoke mode.</summary>
        internal bool _smokeMode;

        /// <summary>Stores confirming quit.</summary>
        internal bool _confirmingQuit;
        /// <summary>Stores experimental level one.</summary>
        internal bool _experimentalLevelOne;
        /// <summary>Stores quit requested.</summary>
        private bool _quitRequested;
        /// <summary>Stores whether the system cursor visibility has been synchronized.</summary>
        private bool _cursorVisibilityInitialized;
        /// <summary>Performs bootstrap.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-test-runner") >= 0)
            {
                return;
            }

            if (FindFirstObjectByType<PvZGame>() == null)
            {
                new GameObject("PvZ Game").AddComponent<PvZGame>();
            }
        }

        /// <summary>Performs awake.</summary>
        private void Awake()
        {
            _presenter = new GamePresenter(this);
            Opening._adventureIntroEventHandler = Opening.HandleAdventureIntroEvent;
            Application.targetFrameRate = 60;
            Application.wantsToQuit += CanQuit;
            var arguments = Environment.GetCommandLineArgs();
            _smokeMode = Array.IndexOf(arguments, "-pvz-combat-fidelity-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-intro-audio-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-feedback-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-progression-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-ui-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-first-user-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-level-one-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-level-one-logic-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-ui-logic-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-first-user-logic-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-ui-review") >= 0 || Array.IndexOf(arguments, "-pvz-adventure-smoke") >= 0;
            if (_smokeMode)
            {
                Application.runInBackground = true;
            }

            _experimentalLevelOne = Array.IndexOf(arguments, "-pvz-level-one") >= 0 || Array.IndexOf(arguments, "-pvz-level-one-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-level-one-logic-smoke") >= 0;
            _root = global::PlantsVsZombies.Infrastructure.RuntimeAssetPaths.ResolveRoot(Application.streamingAssetsPath);
            _animations = new ReanimationRenderer(_root);
            _selectorView = new SelectorView(_root);
            _optionsView = new OptionsView(_root);
            _almanacView = new AlmanacView(_root, _animations);
            _helpView = new HelpView(_root);
            var effectsSource = gameObject.AddComponent<AudioSource>();
            var voiceSource = gameObject.AddComponent<AudioSource>();
            var musicSource = gameObject.AddComponent<AudioSource>();
            var musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("PvZReplica.Music", 0.35f));
            var soundVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("PvZReplica.Sound", 0.7f));
            Sounds.Initialize(effectsSource, soundVolume);
            Dave.Initialize(voiceSource, soundVolume);
            Music.Initialize(musicSource, musicVolume);
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 16);
            _modeView = new ModeSelectionView(_root, Textures.Load, _font);
            IndependentModes.InitializeModes();
            IndependentModes.ExternalChallengeRequested = id =>
            {
                if (id == 43)
                {
                    Garden.OpenZenGarden();
                }
                else if (id == 50)
                {
                    Tree.OpenWisdomTree();
                }
            };
            _optionsBitmap = MenuController.LoadBitmapFont("DwarvenTodcraft18");
            _modeTitleBitmap = MenuController.LoadBitmapFont("HouseofTerror28");
            _modeLabelBitmap = MenuController.LoadBitmapFont("BrianneTod12");
            _sunCounterBitmap = MenuController.LoadBitmapFont("ContinuumBold14");
            _optionsView.OriginalRightLabel = _presenter.DrawOptionsBitmap;
            _modeView.OriginalCenteredLabel = _presenter.DrawModeBitmap;
            Menus.InitializeProfiles();
            ResetGame();
            StartCoroutine(Sounds.LoadSounds());
            Music.InitializeDynamicAudio();
            Loading.InitializeOriginalLoading();
        }

        /// <summary>Performs reset game.</summary>
        internal void ResetGame()
        {
            if (IndependentModes.RestartModeIfActive())
            {
                return;
            }

            if (_game != null && _game.IsAdventure && !Campaign.UseLegacyBoard)
            {
                Campaign.RestartAdventure();
                return;
            }

            if (_experimentalLevelOne)
            {
                _deck.Clear();
                _deck.Add(PlantKind.Peashooter);
            }

            var rules = new GameRules
            {
                EnableExperimentalLevelOne = _experimentalLevelOne
            };
            if (_experimentalLevelOne)
            {
                rules.LevelOneOptions.UseNativeSkySunRules = true;
            }

            _game = new GameSimulation(rules);
            _game.EventRaised = Sounds.PlaySound;
            _selected = -1;
            _shovel = false;
            _choosingSeeds = false;
        }

        /// <summary>Performs update.</summary>
        private void Update()
        {
            GameInput.Capture();
            var pendingAction = _pendingUiAction;
            _pendingUiAction = null;
            pendingAction?.Invoke();
            AdventureBoard.AdvanceHammerAnimation(Time.unscaledDeltaTime);
            Router.HandleInput();
            AdventureBoard.PreserveAdventureFeedbackDuringPause(Time.unscaledDeltaTime);
            if (Loading.UpdateOriginalLoading(Time.unscaledDeltaTime))
            {
                UpdateCursorVisibility();
                Music.UpdateDynamicAudio();
                return;
            }

            Music.UpdateProgressionMusicContext();
            UpdateProgression();
            SaveUtility.OpenWhenRequested();
            if (_choosingSeeds && _pendingSeeds != null && _currentView == ScreenView.Board && !_optionsView.IsOpen
                && !_confirmingQuit && _unavailableFeature == null && _game.State != MatchState.Paused)
            {
                _pendingSeeds.Advance(Time.deltaTime);
            }

            var openingBlocksSimulation = Opening.UpdateAdventureOpening(Time.deltaTime);
            Opening.UpdateVaseInterlude(Time.deltaTime);
            if (!openingBlocksSimulation && _currentView == ScreenView.Board && !_choosingSeeds && !_optionsView.IsOpen && (_profileDialog == null || !_profileDialog.IsOpen))
            {
                if (!IndependentModes.TickModes(Time.deltaTime))
                {
                    _game.Update(Time.deltaTime);
                }
            }

            AdventureBoard.NormalizeAdventureSelection();
            Sounds.UpdateAdventureZombiePartAudio();
            Rewards.UpdateAdventureRewardContinuation();
            Router.HandleEscape();

            UpdateCursorVisibility();
            Music.UpdateDynamicAudio();
        }

        /// <summary>Synchronizes the system cursor with the original adventure board presentation.</summary>
        private void UpdateCursorVisibility()
        {
            var whackBoard = _game != null && _game.IsAdventure
                && (_game.Adventure.Level.Mode == AdventureMode.WhackAZombie
                    || IndependentModes.IsModeBoard && IndependentModes._modeSession != null && IndependentModes._modeSession.Definition.Mechanic == ModeMechanic.Whack);
            var hidden = _currentView == ScreenView.Board && _game != null && _game.IsAdventure
                && whackBoard
                && !_optionsView.IsOpen && !_confirmingQuit && _unavailableFeature == null && !Loading._originalLoadingActive;
            var visible = !hidden;
            if (!_cursorVisibilityInitialized || UnityEngine.Cursor.visible != visible)
            {
                UnityEngine.Cursor.visible = visible;
                _cursorVisibilityInitialized = true;
            }
        }

        /// <summary>Updates presentation after gameplay has consumed the frame input.</summary>
        private void LateUpdate()
        {
            _presenter.Present();
        }

        /// <summary>Performs can quit.</summary>
        /// <returns>The resulting can quit.</returns>
        private bool CanQuit()
        {
            if (Sounds._audioLoadingFinished && Music.DynamicAudioReady)
            {
                return true;
            }

            RequestQuit(0);
            return false;
        }

        /// <summary>Performs request quit.</summary>
        /// <param name = "exitCode">The exit code.</param>
        internal void RequestQuit(int exitCode)
        {
            if (_quitRequested)
            {
                return;
            }

            _quitRequested = true;
            StartCoroutine(QuitAfterAudioLoading(exitCode));
        }

        /// <summary>Performs quit after audio loading.</summary>
        /// <param name = "exitCode">The exit code.</param>
        /// <returns>The resulting quit after audio loading.</returns>
        private IEnumerator QuitAfterAudioLoading(int exitCode)
        {
            // An early quit can race Unity's native audio initialization/decoding.
            // Finish the existing loader before entering the engine shutdown path.
            while (!Sounds._audioLoadingFinished || !Music.DynamicAudioReady)
            {
                yield return null;
            }

            yield return null;
            Application.Quit(exitCode);
        }

        /// <summary>Performs on destroy.</summary>
        private void OnDestroy()
        {
            UnityEngine.Cursor.visible = true;
            _presenter?.Dispose();
            Music.DisposeDynamicAudio();
            Loading.DisposeOriginalLoading();
            Application.wantsToQuit -= CanQuit;
            if (_selectorView != null)
            {
                _selectorView.Dispose();
            }

            if (_optionsView != null)
            {
                _optionsView.Dispose();
            }

            if (_almanacView != null)
            {
                _almanacView.Dispose();
            }

            if (_profileDialog != null)
            {
                _profileDialog.Dispose();
            }

            if (_helpView != null)
            {
                _helpView.Dispose();
            }

            if (_optionsBitmap != null)
            {
                _optionsBitmap.Dispose();
            }

            if (_modeTitleBitmap != null)
            {
                _modeTitleBitmap.Dispose();
            }

            if (_modeLabelBitmap != null)
            {
                _modeLabelBitmap.Dispose();
            }

            if (_sunCounterBitmap != null)
            {
                _sunCounterBitmap.Dispose();
            }

            if (_animations != null)
            {
                _animations.Dispose();
            }

            OriginalUiWidgets.ReleaseFonts();
            if (_originalTooltip != null)
            {
                _originalTooltip.Dispose();
            }

            Textures.ReleaseAll();
            Sounds.ReleaseClips();

            if (_font != null)
            {
                Destroy(_font);
            }
        }

        /// <summary>Gets or sets utc seconds.</summary>
        internal static long UtcSeconds
        {
            get
            {
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        /// <summary>Performs update progression.</summary>
        private void UpdateProgression()
        {
            Store.UpdateCrazyDaveStore(Time.unscaledDeltaTime);
            Tree.UpdateWisdomTreeDialogue(Time.unscaledDeltaTime);
            Garden.UpdateZenGarden();
        }

        /// <summary>Stores original tooltip.</summary>
        internal OriginalTooltip _originalTooltip;
        /// <summary>Stores tooltip inspection.</summary>
        internal OriginalTooltipRequest _tooltipInspection;

        /// <summary>Retains native UI commands until the next game update.</summary>
        private Action? _pendingUiAction;

        /// <summary>Queues a native UI command for the gameplay update.</summary>
        /// <param name="action">The navigation or persistence command requested by the interface.</param>
        internal void QueueUiAction(Action action)
        {
            _pendingUiAction += action;
        }
    }
}
