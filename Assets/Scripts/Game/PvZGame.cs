#nullable enable
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Audio;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Levels;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using PlantsVsZombies.UI.BitmapFonts;
using PlantsVsZombies.VerifiedData;
using PlantsVsZombies.Waves;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace PlantsVsZombies.Game
{
    /// <summary>Coordinates the game simulation, original interface, audio, and scene lifetime.</summary>
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

        /// <summary>Owns the scene and Canvas presentation for this game session.</summary>
        private GamePresenter _presenter = null!;
        /// <summary>Stores grid x.</summary>
        internal const float GridX = 70f;
        /// <summary>Stores grid y.</summary>
        internal const float GridY = 80f;
        /// <summary>Stores cell width.</summary>
        internal const float CellWidth = 80f;
        /// <summary>Stores cell height.</summary>
        internal const float CellHeight = 100f;
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
        /// <summary>Stores textures.</summary>
        internal readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        /// <summary>Stores sounds.</summary>
        private readonly Dictionary<GameEvent, AudioClip> _sounds = new Dictionary<GameEvent, AudioClip>();
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
        /// <summary>Stores almanac return view.</summary>
        private ScreenView _almanacReturnView = ScreenView.Selector;
        /// <summary>Stores almanac returns to options.</summary>
        private bool _almanacReturnsToOptions;
        /// <summary>Stores audio source.</summary>
        private AudioSource _audioSource;
        /// <summary>Stores music source.</summary>
        private AudioSource _musicSource;
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
        /// <summary>Stores music volume.</summary>
        private float _musicVolume;
        /// <summary>Stores sound volume.</summary>
        private float _soundVolume;
        /// <summary>Stores unavailable feature.</summary>
        internal string _unavailableFeature;
        /// <summary>Stores unavailable detail.</summary>
        internal string _unavailableDetail = "尚未实现";
        /// <summary>Stores smoke mode.</summary>
        internal bool _smokeMode;
        /// <summary>Stores smoke pointer.</summary>
        private Vector2? _smokePointer = null; // Explicit Player scenarios only; normal input uses the frame snapshot.
        /// <summary>Stores confirming quit.</summary>
        internal bool _confirmingQuit;
        /// <summary>Stores experimental level one.</summary>
        private bool _experimentalLevelOne;
        /// <summary>Stores audio loading finished.</summary>
        private bool _audioLoadingFinished;
        /// <summary>Stores quit requested.</summary>
        private bool _quitRequested;
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
            _adventureIntroEventHandler = HandleAdventureIntroEvent;
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
            _audioSource = gameObject.AddComponent<AudioSource>();
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("PvZReplica.Music", 0.35f));
            _soundVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("PvZReplica.Sound", 0.7f));
            _musicSource.volume = _musicVolume;
            _audioSource.volume = _soundVolume;
            _soundVoices = new AudioVoicePool(gameObject, _audioSource, 64);
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 16);
            _modeView = new ModeSelectionView(_root, Texture, _font);
            InitializeModes();
            ExternalChallengeRequested = id =>
            {
                if (id == 43)
                {
                    OpenZenGarden();
                }
                else if (id == 50)
                {
                    OpenWisdomTree();
                }
            };
            _optionsBitmap = LoadBitmapFont("DwarvenTodcraft18");
            _modeTitleBitmap = LoadBitmapFont("HouseofTerror28");
            _modeLabelBitmap = LoadBitmapFont("BrianneTod12");
            _sunCounterBitmap = LoadBitmapFont("ContinuumBold14");
            _optionsView.OriginalRightLabel = _presenter.DrawOptionsBitmap;
            _modeView.OriginalCenteredLabel = _presenter.DrawModeBitmap;
            InitializeProfiles();
            ResetGame();
            StartCoroutine(LoadSounds());
            InitializeDynamicAudio();
            InitializeOriginalLoading();
        }

        /// <summary>Performs reset game.</summary>
        private void ResetGame()
        {
            if (RestartModeIfActive())
            {
                return;
            }

            if (_game != null && _game.IsAdventure && !UseLegacyBoard)
            {
                RestartAdventure();
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
            _game.EventRaised = PlaySound;
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
            HandleInput();
            PreserveAdventureFeedbackDuringPause(Time.unscaledDeltaTime);
            if (UpdateOriginalLoading(Time.unscaledDeltaTime))
            {
                UpdateDynamicAudio();
                return;
            }

            UpdateProgressionMusicContext();
            UpdateProgression();
            if (Input.GetKeyDown(KeyCode.F9) && _currentView != ScreenView.Board && !_optionsView.IsOpen && (_profileDialog == null || !_profileDialog.IsOpen))
            {
                _currentView = ScreenView.Import;
                _progressionMessage = null;
            }

            if (_choosingSeeds && _pendingSeeds != null && _currentView == ScreenView.Board && !_optionsView.IsOpen
                && !_confirmingQuit && _unavailableFeature == null && _game.State != MatchState.Paused)
            {
                _pendingSeeds.Advance(Time.deltaTime);
            }

            var openingBlocksSimulation = UpdateAdventureOpening(Time.deltaTime);
            if (!openingBlocksSimulation && _currentView == ScreenView.Board && !_choosingSeeds && !_optionsView.IsOpen && (_profileDialog == null || !_profileDialog.IsOpen))
            {
                if (!TickModes(Time.deltaTime))
                {
                    _game.Update(Time.deltaTime);
                }
            }

            NormalizeAdventureSelection();
            UpdateAdventureZombiePartAudio();
            UpdateAdventureRewardContinuation();
            if (GameInput.Current.EscapePressed)
            {
                if (_optionsView.IsOpen)
                {
                    CloseOptions();
                }
                else if (_profileDialog != null && _profileDialog.IsOpen)
                {
                    _profileDialog.HandleBack();
                }
                else if (_confirmingQuit)
                {
                    _confirmingQuit = false;
                }
                else if (_unavailableFeature != null)
                {
                    _unavailableFeature = null;
                }
                else if (_currentView == ScreenView.Almanac)
                {
                    _almanacView.HandleBack();
                    ReturnFromAlmanacIfClosed();
                }
                else if (_currentView == ScreenView.Modes || _currentView == ScreenView.Garden || _currentView == ScreenView.Import)
                {
                    _currentView = ScreenView.Selector;
                }
                else if (_currentView == ScreenView.Store)
                {
                    _currentView = _storeReturnView;
                }
                else if (_currentView == ScreenView.Tree)
                {
                    _currentView = ScreenView.Garden;
                }
                else if (_currentView == ScreenView.Help)
                {
                    _helpView.HandleKey(KeyCode.Escape);
                    _currentView = ScreenView.Selector;
                }
                else if (_currentView == ScreenView.Board)
                {
                    if (_choosingSeeds)
                    {
                        if (_game.IsAdventure)
                        {
                            _currentView = ScreenView.Selector;
                        }
                        else
                        {
                            _choosingSeeds = false;
                        }
                    }
                    else if (_game.State == MatchState.Playing)
                    {
                        OpenOptions();
                    }
                    else if (_game.State == MatchState.Paused)
                    {
                        _game.TogglePause();
                    }
                }
            }
            else if (_currentView == ScreenView.Help && !_optionsView.IsOpen && (GameInput.Current.EnterPressed || Input.GetKeyDown(KeyCode.Space)))
            {
                _helpView.HandleKey(KeyCode.Return);
                _currentView = ScreenView.Selector;
            }

            UpdateDynamicAudio();
        }

        /// <summary>Retires a selected conveyor packet after the simulation removes it.</summary>
        private void NormalizeAdventureSelection()
        {
            if (!_game.IsAdventure)
            {
                return;
            }

            var board = _game.Adventure;
            if (_selectedAdventurePacket >= 0)
            {
                var exists = false;
                foreach (var packet in board.Packets)
                {
                    if (packet.Id == _selectedAdventurePacket)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    _selectedAdventurePacket = -1;
                }
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
            if (_audioLoadingFinished && DynamicAudioReady)
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
            while (!_audioLoadingFinished || !DynamicAudioReady)
            {
                yield return null;
            }

            yield return null;
            Application.Quit(exitCode);
        }

        /// <summary>Performs on destroy.</summary>
        private void OnDestroy()
        {
            _presenter?.Dispose();
            DisposeDynamicAudio();
            DisposeOriginalLoading();
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

            foreach (Texture2D texture in _textures.Values)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }

            foreach (AudioClip clip in _sounds.Values)
            {
                if (clip != null)
                {
                    Destroy(clip);
                }
            }

            if (_font != null)
            {
                Destroy(_font);
            }
        }

        /// <summary>Performs load sounds.</summary>
        /// <returns>The resulting load sounds.</returns>
        private IEnumerator LoadSounds()
        {
            try
            {
                yield return LoadSoundsCore();
            }
            finally
            {
                _audioLoadingFinished = true;
            }
        }

        /// <summary>Performs load sounds core.</summary>
        /// <returns>The resulting load sounds core.</returns>
        private IEnumerator LoadSoundsCore()
        {
            var files = new Dictionary<GameEvent, string>
            {
                {
                    GameEvent.Planted,
                    "plant.ogg"
                },
                {
                    GameEvent.Shoot,
                    "throw.ogg"
                },
                {
                    GameEvent.SunPickupStarted,
                    "points.ogg"
                },
                {
                    GameEvent.ZombieHit,
                    "splat.ogg"
                },
                {
                    GameEvent.Mower,
                    "lawnmower.ogg"
                },
                {
                    GameEvent.Explosion,
                    "cherrybomb.ogg"
                },
                {
                    GameEvent.MineExplosion,
                    "potato_mine.ogg"
                },
                {
                    GameEvent.JalapenoExplosion,
                    "jalapeno.ogg"
                },
                {
                    GameEvent.Won,
                    "winmusic.ogg"
                },
                {
                    GameEvent.Lost,
                    "losemusic.ogg"
                }
            };
            foreach (KeyValuePair<GameEvent, string> item in files)
            {
                var path = Path.Combine(_root, "sounds", item.Value);
                if (!File.Exists(path))
                {
                    continue;
                }

                using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.OGGVORBIS))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        _sounds[item.Key] = DownloadHandlerAudioClip.GetContent(request);
                    }
                    else
                    {
                        Debug.LogWarning("Could not load sound " + item.Value + ": " + request.error);
                    }
                }
            }

            yield return LoadAdventureRewardSounds();
        }

        /// <summary>Performs play sound.</summary>
        /// <param name = "gameEvent">The game event.</param>
        private void PlaySound(GameEvent gameEvent)
        {
            if (SuppressLegacyZombieHitSound(gameEvent))
            {
                return;
            }

            if (PlayAdventureCherryBombSounds(gameEvent))
            {
                return;
            }

            AudioClip clip;
            if (_sounds.TryGetValue(gameEvent, out clip) && clip != null)
            {
                _audioSource.PlayOneShot(clip, gameEvent == GameEvent.ZombieHit ? 0.35f : 0.7f);
            }
        }

        /// <summary>Routes one polled input snapshot before advancing the game.</summary>
        private void HandleInput()
        {
            var input = GameInput.Current;
            var mouse = _smokeMode && _smokePointer.HasValue ? _smokePointer.Value : input.Position;
            _presenter.SetPointerPosition(mouse);
            _adventurePointer = mouse;
            _selectorView.HoveredAction = _selectorView.HitTest(mouse);
            if (_originalLoadingActive)
            {
                if (input.Released && OriginalLoadingReady && !(_smokeMode && _originalShowLoadingForCapture))
                {
                    var ticks = Math.Max(0, _originalTitleScreenTicks);
                    var y = ticks > 10 ? OriginalTitleCurve(60, 10, ticks, 650, 534, false) : OriginalTitleCurve(10, 0, ticks, 534, 529, true);
                    if (_originalTitleTicks > 400 && new Rect(240, y, 314, 50).Contains(mouse))
                    {
                        CompleteOriginalLoading(true);
                    }
                }

                return;
            }

            if (_profileDialog != null && _profileDialog.IsOpen)
            {
                if (input.Pressed)
                {
                    _profileDialog.HandleClick(mouse);
                }

                return;
            }

            if (_optionsView.IsOpen)
            {
                for (var index = 0; index < input.PhaseCount && _optionsView.IsOpen; index++)
                {
                    _optionsView.HandlePointer(mouse, input.GetPhase(index));
                }

                if (!_optionsView.IsOpen)
                {
                    ApplyOptions();
                }

                return;
            }

            if (_currentView == ScreenView.Almanac)
            {
                if (input.ScrollDelta != 0f)
                {
                    _almanacView.HandleScroll(mouse, input.ScrollDelta);
                }

                for (var index = 0; index < input.PhaseCount && _currentView == ScreenView.Almanac; index++)
                {
                    HandleAlmanacPointer(mouse, input.GetPhase(index));
                }

                return;
            }

            if (_currentView == ScreenView.Help)
            {
                for (var index = 0; index < input.PhaseCount && _currentView == ScreenView.Help; index++)
                {
                    HandleHelpPointer(mouse, input.GetPhase(index));
                }

                return;
            }

            if (_currentView != ScreenView.Import && input.Pressed && input.InsideViewport)
            {
                HandleClick(mouse);
            }
            else if (input.RightPressed && _currentView == ScreenView.Board && !_choosingSeeds && _unavailableFeature == null && !_confirmingQuit)
            {
                CancelBoardSelection();
            }
        }

        /// <summary>Performs handle click.</summary>
        /// <param name = "point">The point.</param>
        private void HandleClick(Vector2 point)
        {
            if (_confirmingQuit)
            {
                if (new Rect(245, 346, 135, 46).Contains(point))
                {
                    RequestQuit(0);
                }
                else if (new Rect(420, 346, 135, 46).Contains(point))
                {
                    _confirmingQuit = false;
                }

                return;
            }

            if (_unavailableFeature != null)
            {
                if (new Rect(320, 330, 160, 42).Contains(point))
                {
                    _unavailableFeature = null;
                }

                return;
            }

            if (HandleProgressionClick(point))
            {
                return;
            }

            if (_currentView == ScreenView.Selector)
            {
                HandleSelector(_selectorView.HitTest(point));
                return;
            }

            if (_currentView == ScreenView.Modes)
            {
                _modeView.HandleClick(point);
                if (_modeView.Closed)
                {
                    _currentView = ScreenView.Selector;
                }

                return;
            }

            if (_currentView == ScreenView.Almanac)
            {
                _almanacView.HandleClick(point);
                ReturnFromAlmanacIfClosed();
                return;
            }

            if (TryHandleModeClick(point))
            {
                return;
            }

            if (_game.IsAdventure && _currentView == ScreenView.Board)
            {
                HandleAdventureClick(point);
                return;
            }

            if (_choosingSeeds)
            {
                HandleSeedChooserClick(point);
                return;
            }

            if (_game.State == MatchState.Won || _game.State == MatchState.Lost)
            {
                if (new Rect(316, 340, 168, 48).Contains(point))
                {
                    ResetGame();
                }

                return;
            }

            if (new Rect(704, 6, 90, 34).Contains(point))
            {
                OpenOptions();
                return;
            }

            if (_game.State == MatchState.Paused)
            {
                if (new Rect(316, 280, 168, 48).Contains(point))
                {
                    _game.TogglePause();
                }
                else if (new Rect(316, 340, 168, 48).Contains(point))
                {
                    ResetGame();
                }

                return;
            }

            if (!_game.IsExperimentalLevelOne && new Rect(596, 6, 100, 34).Contains(point))
            {
                _choosingSeeds = true;
                _pendingSeeds = new AdventureSeedChooserState(_deck);
                return;
            }

            if (_game.LevelOneUsesNativeSkySunRules)
            {
                int x = Mathf.FloorToInt(point.x), y = Mathf.FloorToInt(point.y);
                for (var i = _game.LevelOneNativeSkySuns.Count - 1; i >= 0; i--)
                {
                    var sun = _game.LevelOneNativeSkySuns[i];
                    if (_game.TryClickLevelOneNativeSkySun(sun, x, y))
                    {
                        return;
                    }
                }
            }

            for (var i = _game.Suns.Count - 1; i >= 0; i--)
            {
                if (SunRect(_game.Suns[i]).Contains(point))
                {
                    _game.CollectSun(_game.Suns[i]);
                    return;
                }
            }

            for (var i = 0; i < _deck.Count; i++)
            {
                if (!SeedRect(i).Contains(point))
                {
                    continue;
                }

                if (_game.IsExperimentalLevelOne)
                {
                    if (_game.LevelOneSelectedSeed.HasValue)
                    {
                        if (_game.CancelLevelOneSeedSelection())
                        {
                            _selected = -1;
                        }
                    }
                    else if (_game.TrySelectLevelOneSeed(_deck[i]))
                    {
                        _selected = i;
                    }
                    else
                    {
                        RejectSeedClick(_game.CooldownRemaining(_deck[i]) <= 0f && _game.SunAmount < GameSimulation.Cost(_deck[i]));
                    }

                    _shovel = false;
                }
                else if (_game.CooldownRemaining(_deck[i]) <= 0f && _game.SunAmount >= GameSimulation.Cost(_deck[i]))
                {
                    _selected = _selected == i ? -1 : i;
                    _shovel = false;
                }
                else
                {
                    RejectSeedClick(_game.CooldownRemaining(_deck[i]) <= 0f && _game.SunAmount < GameSimulation.Cost(_deck[i]));
                }

                return;
            }

            if (!_game.IsExperimentalLevelOne && new Rect(522, 0, 70, 72).Contains(point))
            {
                _shovel = !_shovel;
                _selected = -1;
                return;
            }

            var column = Mathf.FloorToInt((point.x - GridX) / CellWidth);
            var row = Mathf.FloorToInt((point.y - GridY) / CellHeight);
            if (row < 0 || row >= GameSimulation.Rows || column < 0 || column >= GameSimulation.Columns)
            {
                return;
            }

            if (_shovel)
            {
                if (_game.RemovePlant(row, column))
                {
                    _shovel = false;
                }
            }
            else if (_selected >= 0 && _game.TryPlant(_deck[_selected], row, column))
            {
                _selected = -1;
            }
        }

        /// <summary>Performs cancel board selection.</summary>
        private void CancelBoardSelection()
        {
            _selectedAdventurePacket = -1;
            if (_game.IsAdventure)
            {
                _game.CancelAdventureCobCannonAim();
            }

            if (_game.State != MatchState.Playing)
            {
                return;
            }

            if (_game.IsExperimentalLevelOne && _game.LevelOneSelectedSeed.HasValue && !_game.CancelLevelOneSeedSelection())
            {
                return;
            }

            _selected = -1;
            _shovel = false;
        }

        /// <summary>Performs handle selector.</summary>
        /// <param name = "action">The action.</param>
        private void HandleSelector(SelectorAction action)
        {
            switch (action)
            {
                case SelectorAction.Adventure:
                    if (!UseLegacyBoard)
                    {
                        StartAdventureCampaign();
                        break;
                    }

                    ResetGame();
                    _currentView = ScreenView.Board;
                    break;
                case SelectorAction.MiniGames:
                    OpenModes(1);
                    break;
                case SelectorAction.Puzzle:
                    OpenModes(3);
                    break;
                case SelectorAction.Survival:
                    OpenModes(0);
                    break;
                case SelectorAction.Almanac:
                    OpenAlmanac(ScreenView.Selector);
                    break;
                case SelectorAction.Options:
                    OpenOptions();
                    break;
                case SelectorAction.Quit:
                    _confirmingQuit = true;
                    break;
                case SelectorAction.ZenGarden:
                    OpenZenGarden();
                    break;
                case SelectorAction.Store:
                    OpenStore();
                    break;
                case SelectorAction.Help:
                    _helpView.Open();
                    _currentView = ScreenView.Help;
                    break;
                case SelectorAction.ChangeUser:
                    if (_profileDialog != null)
                    {
                        _profileDialog.Open();
                    }
                    else
                    {
                        SetUnavailable("用户目录无法读取", "原文件未更改");
                    }

                    break;
                case SelectorAction.Zombatar:
                    SetUnavailable("僵尸大头贴", "尚未实现");
                    break;
            }
        }

        /// <summary>Performs handle help pointer.</summary>
        /// <param name = "point">The point.</param>
        /// <param name = "eventType">The event type.</param>
        /// <returns>The resulting handle help pointer.</returns>
        private bool HandleHelpPointer(Vector2 point, PointerPhase eventType)
        {
            var consumed = _helpView.HandlePointer(point, eventType);
            if (!_helpView.IsOpen)
            {
                _currentView = ScreenView.Selector;
            }

            return consumed;
        }

        /// <summary>Performs initialize profiles.</summary>
        private void InitializeProfiles()
        {
            try
            {
                var arguments = Environment.GetCommandLineArgs();
                var firstUserSmoke = Array.IndexOf(arguments, "-pvz-first-user-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-first-user-logic-smoke") >= 0;
                var path = _smokeMode ? Path.Combine(Application.temporaryCachePath, "pvz-smoke-" + Guid.NewGuid().ToString("N") + ".dat") : Path.Combine(Application.persistentDataPath, "replica-profiles.dat");
                _profiles = new ProfileCatalog(path);
                _profileDialog = new ProfileDialog(_root, _profiles);
                if (_smokeMode && !firstUserSmoke)
                {
                    _profiles.Create("自动测试");
                }
                else if (_profiles.Profiles.Count == 0)
                {
                    _profileDialog.Open();
                }
            }
            catch (Exception error)
            {
                _profileLoadError = error.Message;
                SetUnavailable("用户目录无法读取", "原文件未更改");
                Debug.LogError("Could not open replica profiles: " + error.Message);
            }
        }

        /// <summary>Performs load bitmap font.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting load bitmap font.</returns>
        private static CanvasBitmapFont LoadBitmapFont(string name)
        {
            try
            {
                return CanvasBitmapFont.LoadFromResources(name);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Bitmap font " + name + ": " + error.Message);
                return null;
            }
        }

        /// <summary>Performs set unavailable.</summary>
        /// <param name = "feature">The feature.</param>
        /// <param name = "detail">The detail.</param>
        private void SetUnavailable(string feature, string detail)
        {
            _unavailableFeature = feature;
            _unavailableDetail = detail;
        }

        /// <summary>Performs open modes.</summary>
        /// <param name = "category">The category.</param>
        private void OpenModes(int category)
        {
            _modeView.Open(category);
            _currentView = ScreenView.Modes;
        }

        /// <summary>Performs open almanac.</summary>
        /// <param name = "returnView">The return view.</param>
        /// <param name = "returnToOptions">The return to options.</param>
        private void OpenAlmanac(ScreenView returnView, bool returnToOptions = false)
        {
            _almanacReturnView = returnView;
            _almanacReturnsToOptions = returnToOptions;
            _almanacView.Open();
            _currentView = ScreenView.Almanac;
        }

        /// <summary>Performs handle almanac pointer.</summary>
        /// <param name = "point">The point.</param>
        /// <param name = "eventType">The event type.</param>
        /// <returns>The resulting handle almanac pointer.</returns>
        private bool HandleAlmanacPointer(Vector2 point, PointerPhase eventType)
        {
            var consumed = _almanacView.HandlePointer(point, eventType);
            ReturnFromAlmanacIfClosed();
            return consumed;
        }

        /// <summary>Performs return from almanac if closed.</summary>
        private void ReturnFromAlmanacIfClosed()
        {
            if (!_almanacView.IsClosed)
            {
                return;
            }

            _currentView = _almanacReturnView;
            if (_almanacReturnsToOptions)
            {
                _optionsView.ResumeFromAlmanac();
            }

            _almanacReturnsToOptions = false;
        }

        /// <summary>Performs open options.</summary>
        private void OpenOptions()
        {
            _optionsView.GameplayButtonsEnabled = _currentView == ScreenView.Board;
            _optionsView.Open(_musicVolume, _soundVolume, Screen.fullScreen);
        }

        /// <summary>Performs close options.</summary>
        private void CloseOptions()
        {
            _optionsView.Close();
            if (!_optionsView.IsOpen)
            {
                ApplyOptions();
            }
        }

        /// <summary>Performs apply options.</summary>
        private void ApplyOptions()
        {
            // The original almanac is modal over the options dialog. Retain its
            // unapplied draft and keep gameplay suspended until Return to Game.
            if (_optionsView.CloseResult == OptionsCloseResult.OpenAlmanac)
            {
                OpenAlmanac(_currentView, true);
                return;
            }

            _musicVolume = _optionsView.MusicVolume;
            _soundVolume = _optionsView.SoundVolume;
            _musicSource.volume = _musicVolume;
            _audioSource.volume = _soundVolume;
            if (!_smokeMode)
            {
                PlayerPrefs.SetFloat("PvZReplica.Music", _musicVolume);
                PlayerPrefs.SetFloat("PvZReplica.Sound", _soundVolume);
                PlayerPrefs.Save();
                if (Screen.fullScreen != _optionsView.FullScreen)
                {
                    Screen.fullScreen = _optionsView.FullScreen;
                }
            }

            switch (_optionsView.CloseResult)
            {
                case OptionsCloseResult.RestartLevel:
                    ResetGame();
                    break;
                case OptionsCloseResult.MainMenu:
                    _currentView = ScreenView.Selector;
                    break;
            }
        }

        /// <summary>Performs handle seed chooser click.</summary>
        /// <param name = "point">The point.</param>
        private void HandleSeedChooserClick(Vector2 point)
        {
            _pendingSeeds.LandFlyingSeeds();
            if (ReturnSelectedChooserSeed(point, 7, false, true))
            {
                return;
            }

            for (var i = 0; i < _availablePlants.Length; i++)
            {
                var home = ChooserSeedRect(i);
                if (home.Contains(point))
                {
                    _pendingSeeds.Choose(_availablePlants[i], (int)home.x, (int)home.y, 7);
                    return;
                }
            }

            if (new Rect(322, 440, 156, 42).Contains(point) && _pendingSeeds.Count > 0)
            {
                _deck.Clear();
                _deck.AddRange(_pendingSeeds);
                _choosingSeeds = false;
                ResetGame();
            }
            else if (new Rect(341, 493, 118, 30).Contains(point))
            {
                _choosingSeeds = false;
            }
        }

        /// <summary>Returns a clicked bank packet and compacts the later packets in native order.</summary>
        /// <param name="point">The logical pointer position.</param>
        /// <param name="slots">The bank's configured capacity.</param>
        /// <param name="mode">Whether the independent mode chooser supplies packet homes.</param>
        /// <param name="simple">Whether the development chooser supplies packet homes.</param>
        /// <returns>Whether a selected bank packet consumed the click.</returns>
        private bool ReturnSelectedChooserSeed(Vector2 point, int slots, bool mode = false, bool simple = false)
        {
            for (var i = 0; i < _pendingSeeds.Count; i++)
            {
                var kind = _pendingSeeds[i];
                var origin = simple ? Vector2.zero : AdventureSeedBankOrigin;
                var bank = new Rect(origin.x + AdventureSeedSlotLayout.PositionX(i, slots), origin.y + 8f, 50, 70);
                if (!bank.Contains(point))
                {
                    continue;
                }

                if (!mode && !simple && ContainsRequiredSeed(kind))
                {
                    PlaySeedBankBuzzer();
                    return true;
                }

                var index = simple ? Array.IndexOf(_availablePlants, kind) : mode ? AdventureAvailableSeedIndex(kind) : -1;
                var home = simple ? ChooserSeedRect(index) : mode ? AdventureChooserRect(index) : AdventureCampaignChooserRect(kind);
                if (_pendingSeeds.Return(kind, (int)home.x, (int)home.y, slots))
                {
                    PlayAdventureRewardSound("tap2.ogg");
                }

                return true;
            }

            return false;
        }

        /// <summary>Finds a seed's current chooser grid index without creating an intermediate list.</summary>
        /// <param name="kind">The available seed to locate.</param>
        /// <returns>The available seed index, or minus one if it is unavailable.</returns>
        private int AdventureAvailableSeedIndex(PlantKind kind)
        {
            var seeds = _game.Adventure.AvailableSeeds;
            for (var i = 0; i < seeds.Count; i++)
            {
                if (seeds[i] == kind)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Performs seed rect.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting seed rect.</returns>
        internal Rect SeedRect(int index)
        {
            if (_game.IsAdventure)
            {
                return AdventureSeedRect(index);
            }

            if (_game.IsExperimentalLevelOne)
            {
                var slot = FirstAdventureBoardUi.FirstSeedSlot(0, 0);
                return new Rect(slot.X + 59 * index, slot.Y, slot.Width, slot.Height);
            }

            return new Rect(AdventureSeedSlotLayout.PositionX(index, _choosingSeeds ? 7 : _deck.Count), 8, 50, 70);
        }

        /// <summary>Performs chooser seed rect.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting chooser seed rect.</returns>
        internal static Rect ChooserSeedRect(int index)
        {
            return new Rect(198 + index % 6 * 65, 150 + index / 6 * 84, 50, 70);
        }

        /// <summary>Performs sun rect.</summary>
        /// <param name = "sun">The sun.</param>
        /// <returns>The resulting sun rect.</returns>
        internal static Rect SunRect(global::PlantsVsZombies.Simulation.Sun sun)
        {
            return new Rect(GridX + sun.X * CellWidth - 35, GridY + sun.Y * CellHeight - 35, 70, 70);
        }

        /// <summary>Performs texture.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting texture.</returns>
        internal Texture2D Texture(string name)
        {
            Texture2D texture;
            if (_textures.TryGetValue(name, out texture))
            {
                return texture;
            }

            var path = Path.Combine(_root, "images", name);
            if (!File.Exists(path))
            {
                _textures[name] = null;
                return null;
            }

            var maskPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_.png");
            if (string.Equals(Path.GetExtension(path), ".jpg", StringComparison.OrdinalIgnoreCase) && File.Exists(maskPath))
            {
                texture = LoadMaskedImage(path, maskPath);
            }
            else
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                {
                    Destroy(texture);
                    texture = null;
                }
                else
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                }
            }

            _textures[name] = texture;
            return texture;
        }

        /// <summary>Loads and retains an original resources.xml image with an explicit alpha grid.</summary>
        /// <param name="colorName">The original color image filename.</param>
        /// <param name="maskName">The original alpha grid filename.</param>
        /// <returns>The cached RGBA texture, or null when either source is missing.</returns>
        internal Texture2D? TextureAlphaGrid(string colorName, string maskName)
        {
            var key = colorName + "|" + maskName;
            if (_textures.TryGetValue(key, out var texture))
            {
                return texture;
            }

            var colorPath = Path.Combine(_root, "images", colorName);
            var maskPath = Path.Combine(_root, "images", maskName);
            if (!File.Exists(colorPath) || !File.Exists(maskPath))
            {
                _textures[key] = null;
                return null;
            }

            texture = LoadMaskedImage(colorPath, maskPath);
            _textures[key] = texture;
            return texture;
        }

        /// <summary>Combines original color artwork with the red channel of its separate alpha image.</summary>
        /// <param name="colorPath">The original RGB or RGBA image path.</param>
        /// <param name="maskPath">The matching original grayscale alpha image path.</param>
        /// <returns>A new RGBA texture owned by the caller.</returns>
        /// <exception cref="IOException">A source image cannot be read.</exception>
        /// <exception cref="InvalidDataException">An image is invalid or image dimensions differ.</exception>
        internal static Texture2D LoadMaskedImage(string colorPath, string maskPath)
        {
            var color = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var mask = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D? merged = null;
            try
            {
                if (!color.LoadImage(File.ReadAllBytes(colorPath)) || !mask.LoadImage(File.ReadAllBytes(maskPath))
                    || color.width != mask.width || color.height != mask.height)
                {
                    throw new InvalidDataException("Original image alpha dimensions or image data are invalid: " + maskPath);
                }

                var colors = color.GetPixels32();
                var alpha = mask.GetPixels32();
                for (var i = 0; i < colors.Length; i++)
                {
                    colors[i].a = alpha[i].r;
                }

                // Loading JPEG changes the source to RGB24; a separate RGBA texture preserves the alpha.
                merged = new Texture2D(color.width, color.height, TextureFormat.RGBA32, false)
                {
                    name = Path.GetFileName(colorPath) + "_masked",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                merged.SetPixels32(colors);
                merged.Apply(false, false);
                return merged;
            }
            catch
            {
                ReleaseImageTexture(merged);
                throw;
            }
            finally
            {
                ReleaseImageTexture(color);
                ReleaseImageTexture(mask);
            }
        }

        /// <summary>Releases a temporary decoded image on the current Unity lifecycle.</summary>
        /// <param name="texture">The temporary texture to release, if any.</param>
        private static void ReleaseImageTexture(Texture2D? texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }

        /// <summary>Reuses bounded sound voices for pitched explosions and zombie Foley.</summary>
        private AudioVoicePool _soundVoices = null!;

        // Called synchronously by PlaySound for the existing simulation event. Returning
        // true suppresses the old Explosion->cherrybomb mapping for this Cherry only.
        /// <summary>Performs play adventure cherry bomb sounds.</summary>
        /// <param name = "trigger">The trigger.</param>
        /// <returns>The resulting play adventure cherry bomb sounds.</returns>
        private bool PlayAdventureCherryBombSounds(GameEvent trigger)
        {
            if (_game == null || !_game.IsAdventure)
            {
                return false;
            }

            AdventureCherryBombSound sound;
            var detonation = false;
            while (_game.TryTakeAdventureCherryBombSound(trigger, out sound))
            {
                if (!sound.Detonation)
                {
                    PlayPresentationSound("reverse_explosion");
                    continue;
                }

                detonation = true;
                PlayPresentationSound("cherrybomb");
                AudioClip clip;
                if (_audioSource == null || !_presentationSounds.TryGetValue("juicy", out clip) || clip == null)
                {
                    continue;
                }

                // PC 63E850 applies integer DirectSound frequency after semitone scaling.
                var frequency = Math.Max(100.0, Math.Min(100000.0, clip.frequency * Math.Pow(1.0594630943592953, sound.JuicyPitchSteps)));
                _soundVoices.TryPlay(clip, _audioSource.volume * .7f, (float)((int)frequency / (double)clip.frequency), group: -1);
            }

            return trigger == GameEvent.Explosion && detonation;
        }

        /// <summary>Performs update adventure cherry bomb audio.</summary>
        private void UpdateAdventureCherryBombAudio()
        {
            // Also delivers fuse notifications from generated plants whose creation did
            // not go through the player's Planted event. This reads explicit events only.
            PlayAdventureCherryBombSounds(GameEvent.Planted);
            if (_audioSource != null)
            {
                _soundVoices?.SetVolume(_audioSource.volume * .7f, -1);
            }
        }

        /// <summary>Releases the prewarmed sound voices owned by this session.</summary>
        private void DisposeAdventureCherryBombAudio()
        {
            _soundVoices?.Dispose();
        }

        /// <summary>Stores native conveyor clip.</summary>
        internal static readonly Rect s_nativeConveyorClip = new Rect(90, 0, 501, 87);

        /// <summary>Performs adventure packet contains.</summary>
        /// <param name = "index">The index.</param>
        /// <param name = "point">The point.</param>
        /// <returns>The resulting adventure packet contains.</returns>
        private bool AdventurePacketContains(int index, Vector2 point)
        {
            return (!_game.UsesNativeAdventureConveyor || s_nativeConveyorClip.Contains(point)) && AdventureSeedRect(index, true).Contains(point);
        }

        /// <summary>Stores adventure intro.</summary>
        internal AdventureIntroClock _adventureIntro;
        /// <summary>Stores adventure intro frame.</summary>
        internal AdventureIntroFrame _adventureIntroFrame;
        /// <summary>Stores adventure preview.</summary>
        internal readonly List<AdventureIntroPreviewZombie> _adventurePreview = new List<AdventureIntroPreviewZombie>();
        /// <summary>Stores adventure intro remainder.</summary>
        private double _adventureIntroRemainder;
        /// <summary>Stores adventure intro visual age.</summary>
        internal float _adventureIntroVisualAge;
        /// <summary>Stores adventure intro drawn.</summary>
        internal bool _adventureIntroDrawn;
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
                return AdventureIntroActive && _adventureIntro.WaitingForSeeds;
            }
        }

        /// <summary>Performs initialize adventure opening.</summary>
        private void InitializeAdventureOpening()
        {
            _adventureIntro = null;
            _adventureIntroFrame = null;
            _adventurePreview.Clear();
            _adventureIntroRemainder = 0;
            _adventureIntroVisualAge = 0;
            _adventureIntroDrawn = false;
            // Older explicitly isolated feature probes start at the battle they are inspecting.
            // The new opening probe and normal player always use the complete original opening.
            if (_game == null || !_game.IsAdventure || _game.ModeSession != null || _adventureTestBoard || _smokeMode && Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-intro-audio-smoke") < 0)
            {
                return;
            }

            var level = _game.Adventure.Level;
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
                HasGraveStones = _game.Adventure.Graves.Count > 0,
                CanShowAlmanacOrStore = level.IsReplay || level.Number >= 15,
                ChooseSeedsOverride = level.ChoosesSeeds,
                HasCrazyDaveDialog = false
            };
            _adventureIntro = new AdventureIntroClock(AdventureIntroPresentation.CreatePlan(context));
            _game.Adventure.GravesEnabled = _adventureIntro.Plan.GraveStoneTime == 0;
            _adventureIntroFrame = _adventureIntro.Frame;
            if (_adventureIntro.Plan.ChooseSeeds)
            {
                _game.Adventure.Phase = AdventurePhase.ChoosingSeeds;
                _choosingSeeds = true;
                var savedSelection = _adventureCampaign != null && _adventureCampaign.Progress.SelectedSeeds.Count > 0;
                if (!savedSelection)
                {
                    _pendingSeeds = new AdventureSeedChooserState(_game.Adventure.RequiredSeeds);
                }
            }

            var waves = new List<int[]>();
            foreach (var wave in _game.Adventure.Waves)
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
            _adventurePreview.AddRange(AdventureIntroPresentation.CreateStreetPreview(context, waves, bound => previewRandom.Next(bound), limit => (float)previewRandom.NextDouble() * limit, (kind, clip) => kind < 26 && AdventureClipExists(AdventureZombieAnimation((ZombieKind)kind), clip)));
            _adventurePreview.Sort((a, b) => a.RenderOrder.CompareTo(b.RenderOrder));
        }

        /// <summary>Reuses the intro event callback across native simulation ticks.</summary>
        private Action<AdventureIntroEvent> _adventureIntroEventHandler = null!;
        /// <summary>Performs update adventure opening.</summary>
        /// <param name = "dt">The dt.</param>
        /// <returns>The resulting update adventure opening.</returns>
        private bool UpdateAdventureOpening(float dt)
        {
            if (!AdventureIntroActive || _currentView != ScreenView.Board)
            {
                return false;
            }

            if (!_adventureIntroDrawn || _optionsView.IsOpen || _confirmingQuit || _unavailableFeature != null || _game.State == MatchState.Paused)
            {
                return true;
            }

            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt))
            {
                return true;
            }

            _adventureIntroRemainder += dt;
            while (_adventureIntroRemainder + .000000001 >= (double).01f && !_adventureIntro.Complete)
            {
                _adventureIntroRemainder -= (double).01f;
                _adventureIntroVisualAge += .01f;
                _game.AdvanceAdventureGraves();
                _adventureIntro.Tick(emit: _adventureIntroEventHandler);
                _adventureIntroFrame = _adventureIntro.Frame;
            }

            return true;
        }

        /// <summary>Performs handle adventure intro event.</summary>
        /// <param name = "item">The item.</param>
        private void HandleAdventureIntroEvent(AdventureIntroEvent item)
        {
            if (item == AdventureIntroEvent.GraveStonesEnabled)
            {
                _game.Adventure.GravesEnabled = true;
                foreach (var grave in _game.Adventure.Graves)
                {
                    PlayPresentationSound("dirt_rise");
                }
            }

            if (item == AdventureIntroEvent.ReadySetPlantStarted)
            {
                PlayAdventureRewardSound("readysetplant.ogg");
            }

            if (item == AdventureIntroEvent.SodStarted)
            {
                PlayAdventureRewardSound("roll_in.ogg");
            }

            if (item == AdventureIntroEvent.SeedChooserOpened)
            {
                _choosingSeeds = true;
            }

            if (item == AdventureIntroEvent.IntroFinished)
            {
                _choosingSeeds = false;
                _adventurePreview.Clear();
            }
        }

        /// <summary>Performs begin adventure opening return.</summary>
        private void BeginAdventureOpeningReturn()
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
                var availableSeeds = _game.Adventure.AvailableSeeds;
                for (var seedIndex = 0; seedIndex < availableSeeds.Count; seedIndex++)
                {
                    var kind = availableSeeds[seedIndex];
                    if (kind == seed)
                    {
                        break;
                    }

                    index++;
                }

                return AdventureChooserRect(index);
            }

            var id = (int)seed;
            if (id == 48)
            {
                return new Rect(468, 515, 50, 70);
            }

            var upgrades = _game.Adventure.Level.IsReplay;
            var chooserSeeds = _game.Adventure.AvailableSeeds;
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

        /// <summary>Stores potato mine audio game.</summary>
        private GameSimulation _potatoMineAudioGame;
        /// <summary>Stores potato mine rise audio serial.</summary>
        private int _potatoMineRiseAudioSerial;

        /// <summary>Performs update adventure potato mine audio.</summary>
        private void UpdateAdventurePotatoMineAudio()
        {
            if (_potatoMineAudioGame != _game)
            {
                _potatoMineAudioGame = _game;
                _potatoMineRiseAudioSerial = 0;
            }

            if (_game == null || !_game.IsAdventure)
            {
                return;
            }

            var serial = _game.Presentation.PotatoMineRiseSoundSerial;
            while (_potatoMineRiseAudioSerial < serial)
            {
                _potatoMineRiseAudioSerial++;
                PlayPresentationSound("dirt_rise");
            }
        }

        /// <summary>Stores adventure grid x.</summary>
        internal const float AdventureGridX = 40f;
        /// <summary>Stores adventure grid y.</summary>
        internal const float AdventureGridY = 80f;
        /// <summary>Stores adventure clips.</summary>
        private readonly Dictionary<(string Asset, string Clip), bool> _adventureClips = new Dictionary<(string, string), bool>();
        /// <summary>Gets or sets adventure pitch.</summary>
        internal float AdventurePitch
        {
            get
            {
                return _game.Adventure.Level.HasPool || _game.Adventure.Level.HasRoof ? 85f : 100f;
            }
        }

        /// <summary>Performs adventure slope.</summary>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting adventure slope.</returns>
        internal float AdventureSlope(float column)
        {
            return _game.Adventure.Level.HasRoof ? Mathf.Max(0f, 5f - column) * 20f - 10f : 0f;
        }

        /// <summary>Performs adventure cell y.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting adventure cell y.</returns>
        internal float AdventureCellY(int row, float column)
        {
            return AdventureGridY + row * AdventurePitch + AdventureSlope(column);
        }

        /// <summary>Performs adventure coin rect.</summary>
        /// <param name = "coin">The coin.</param>
        /// <returns>The resulting adventure coin rect.</returns>
        internal Rect AdventureCoinRect(AdventureCoin coin)
        {
            return coin.IsBeingCollected ? new Rect(0f, 0f, 0f, 0f) : new Rect(coin.PresentationX, coin.PresentationY, 40f, 40f);
        }

        /// <summary>Performs adventure sun rect.</summary>
        /// <param name = "sun">The sun.</param>
        /// <returns>The resulting adventure sun rect.</returns>
        private Rect AdventureSunRect(global::PlantsVsZombies.Simulation.Sun sun)
        {
            if (sun.PresentationNative)
            {
                var pickup = sun.PresentationPickup;
                if (pickup.Dead || pickup.Collecting)
                {
                    return new Rect(0, 0, 0, 0);
                }

                var margin = pickup.Kind == global::PlantsVsZombies.Sun.SunKind.Normal || sun.PresentationSpecialExpansion ? 15f : 0f;
                return new Rect(pickup.X - margin, pickup.Y - margin, 60 + margin * 2, 60 + margin * 2 + (sun.PresentationSpecialExpansion ? 30 : 0));
            }

            var fallbackMargin = sun.Value == 25 ? 15f : 0f;
            return new Rect(AdventureGridX + sun.X * CellWidth - 30f - fallbackMargin, AdventureGridY + sun.Y * AdventurePitch - 30f - fallbackMargin, 60 + fallbackMargin * 2, 60 + fallbackMargin * 2);
        }

        /// <summary>Performs adventure clip exists.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "clip">The clip.</param>
        /// <returns>The resulting adventure clip exists.</returns>
        internal bool AdventureClipExists(string name, string clip)
        {
            var key = (name, clip);
            bool exists;
            if (!_adventureClips.TryGetValue(key, out exists))
            {
                global::PlantsVsZombies.Reanimation.AnimationInfo info;
                exists = _animations.TryGetAnimationInfo(name, clip, out info);
                _adventureClips.Add(key, exists);
            }

            return exists;
        }

        /// <summary>Caches immutable catalog animation paths for zombie kinds.</summary>
        private static readonly Dictionary<ZombieKind, string> s_zombieAnimationNames = new Dictionary<ZombieKind, string>();
        /// <summary>Performs adventure zombie animation.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure zombie animation.</returns>
        internal static string AdventureZombieAnimation(ZombieKind kind)
        {
            if (!s_zombieAnimationNames.TryGetValue(kind, out var name))
            {
                ReanimationDefinition definition;
                name = VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Zombies[(int)kind].ReanimationId, out definition) ? Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/')) : "Zombie";
                s_zombieAnimationNames.Add(kind, name);
            }

            return name;
        }
        /// <summary>Stores award introduction audio.</summary>
        private AdventureRewardPresentation _awardIntroductionAudio;
        /// <summary>Stores award sounds.</summary>
        private readonly Dictionary<string, AudioClip> _awardSounds = new Dictionary<string, AudioClip>();
        /// <summary>Stores award seed next.</summary>
        internal static readonly Rect s_awardSeedNext = new Rect(324, 546, 156, 42);
        /// <summary>Stores award menu.</summary>
        internal static readonly Rect s_awardMenu = new Rect(677, 553, 111, 26);
        /// <summary>Performs load adventure reward sounds.</summary>
        /// <returns>The resulting load adventure reward sounds.</returns>
        private IEnumerator LoadAdventureRewardSounds()
        {
            foreach (string file in new[]
            {
                "seedlift.ogg",
                "tap2.ogg",
                "lightfill.ogg",
                "readysetplant.ogg",
                "roll_in.ogg",
                "paper.ogg",
                "buttonclick.ogg",
                "shovel.ogg"
            }

            )
            {
                var location = Path.Combine(_root, "sounds", file);
                if (!File.Exists(location))
                {
                    continue;
                }

                using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(location).AbsoluteUri, AudioType.OGGVORBIS))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        _awardSounds[file] = DownloadHandlerAudioClip.GetContent(request);
                    }
                }
            }
        }

        /// <summary>Performs play adventure reward sound.</summary>
        /// <param name = "file">The file.</param>
        private void PlayAdventureRewardSound(string file)
        {
            AudioClip sound;
            if (_awardSounds.TryGetValue(file, out sound) && sound != null)
            {
                _audioSource.PlayOneShot(sound);
            }
        }

        /// <summary>Performs try resolve adventure reward music.</summary>
        /// <param name = "cue">The cue.</param>
        /// <returns>The resulting try resolve adventure reward music.</returns>
        private bool TryResolveAdventureRewardMusic(out MusicCue cue)
        {
            cue = MusicCue.None;
            if (_currentView != ScreenView.Board || _game == null || !_game.IsAdventure || IsModeBoard)
            {
                return false;
            }

            var reward = _game.Adventure.RewardPresentation;
            if (reward == null || reward.Stage != AdventureAwardStage.Introduction)
            {
                return false;
            }

            var paper = reward.NativeType == 15 || reward.NativeType == 18 && _game.Adventure.Level.Number == 50;
            // PC AwardScreen ctor 0x40FB7A..97 starts tune8 at order221; paper remains silent.
            cue = paper ? MusicCue.None : MusicCue.ZenGarden;
            return true;
        }

        /// <summary>Performs handle adventure reward notice sound.</summary>
        /// <param name = "notice">The notice.</param>
        private void HandleAdventureRewardNoticeSound(AdventureNotice notice)
        {
            if (notice.Kind == AdventureNoticeKind.RewardFade)
            {
                PlayAdventureRewardSound("lightfill.ogg");
                return;
            }

            if (notice.Kind != AdventureNoticeKind.RewardCollected || _game.Adventure.RewardPresentation == null)
            {
                return;
            }

            var level = _game.Adventure.Level.Number;
            if (!_game.Adventure.Level.IsReplay && level == 4)
            {
                PlayAdventureRewardSound("shovel.ogg");
            }
            else if (!_game.Adventure.Level.IsReplay && (level == 24 || level == 34 || level == 44))
            {
                PlayAdventureRewardSound("tap2.ogg");
            }
            else
            {
                PlayAdventureRewardSound("seedlift.ogg");
                PlayAdventureRewardSound("tap2.ogg");
            }
        }

        /// <summary>Performs update adventure reward continuation.</summary>
        private void UpdateAdventureRewardContinuation()
        {
            if (_currentView != ScreenView.Board || _game == null || !_game.IsAdventure || IsModeBoard)
            {
                return;
            }

            var level = _game.Adventure.Level;
            var reward = _game.Adventure.RewardPresentation;
            if (reward != null && reward.Stage == AdventureAwardStage.Introduction && !ReferenceEquals(_awardIntroductionAudio, reward))
            {
                _awardIntroductionAudio = reward;
                if (reward.NativeType == 15 || reward.NativeType == 18 && level.Number == 50)
                {
                    PlayAdventureRewardSound("paper.ogg");
                }
            }

            if (_adventureTestBoard || _adventureCampaign == null)
            {
                return;
            }

            // PC LawnApp::CheckForGameEnd 0x45F6E0 skips plant awards on replay, except notes/final awards.
            if (!level.IsReplay || level.Number == 50 || reward == null || reward.NativeType != 18 || reward.Stage != AdventureAwardStage.Introduction)
            {
                return;
            }

            if (_adventureFeedback != null && Time.unscaledTime < _adventureFeedbackUntil)
            {
                return;
            }

            try
            {
                if (_adventureCampaign.StartNextLevel())
                {
                    AttachAdventureGame(_adventureCampaign.Game);
                }
            }
            catch (Exception error)
            {
                AdventureFeedback("下一关无法开始：" + error.Message, 5f);
            }
        }

        /// <summary>Performs handle adventure reward click.</summary>
        /// <param name = "point">The point.</param>
        private void HandleAdventureRewardClick(Vector2 point)
        {
            var reward = _game.Adventure.RewardPresentation;
            if (reward == null)
            {
                return;
            }

            if (reward.Stage != AdventureAwardStage.Introduction)
            {
                if (!reward.Contains(point.x, point.y))
                {
                    return;
                }

                try
                {
                    var collected = _adventureTestBoard ? _game.CollectAdventureReward() : _adventureCampaign.CollectReward();
                    if (!collected)
                    {
                        AdventureFeedback("奖励尚未保存：" + (_adventureCampaign == null ? "请重试" : _adventureCampaign.LastPersistenceError), 5f);
                    }
                }
                catch (Exception error)
                {
                    AdventureFeedback("奖励尚未保存，请重试：" + error.Message, 6f);
                }

                return;
            }

            // PC AwardScreen starts its counter at180; controls enable when it reaches zero.
            if (reward.IntroductionTicks < 180)
            {
                return;
            }

            if (s_awardMenu.Contains(point))
            {
                PlayAdventureRewardSound("buttonclick.ogg");
                _currentView = ScreenView.Selector;
                return;
            }

            var next = reward.NativeType == 15 || reward.NativeType == 18 && _game.Adventure.Level.Number == 50 ? new Rect(324, 500, 156, 42) : s_awardSeedNext;
            if (!next.Contains(point))
            {
                return;
            }

            PlayAdventureRewardSound("buttonclick.ogg");
            try
            {
                if (_adventureTestBoard || _adventureCampaign.Progress.Completed)
                {
                    _currentView = ScreenView.Selector;
                }
                else if (_adventureCampaign.StartNextLevel())
                {
                    AttachAdventureGame(_adventureCampaign.Game);
                }
            }
            catch (Exception error)
            {
                AdventureFeedback("下一关无法开始：" + error.Message, 5f);
            }
        }

        /// <summary>Stores wave notice game.</summary>
        internal GameSimulation _waveNoticeGame;
        /// <summary>Stores huge wave notice start,  final wave notice start.</summary>
        internal float _hugeWaveNoticeStart = -1f, _finalWaveNoticeStart = -1f;
        /// <summary>Stores bundled wave animations.</summary>
        internal global::PlantsVsZombies.Reanimation.ReanimationRenderer _bundledWaveAnimations;
        /// <summary>Performs initialize adventure wave notices.</summary>
        internal void InitializeAdventureWaveNotices()
        {
            if (_waveNoticeGame != null)
            {
                _waveNoticeGame.WaveScheduleEventRaised -= HandleAdventureWaveNotice;
            }

            _waveNoticeGame = _game;
            _hugeWaveNoticeStart = _finalWaveNoticeStart = -1f;
            if (_waveNoticeGame != null)
            {
                _waveNoticeGame.WaveScheduleEventRaised += HandleAdventureWaveNotice;
            }
        }

        /// <summary>Performs handle adventure wave notice.</summary>
        /// <param name = "e">The e.</param>
        private void HandleAdventureWaveNotice(WaveScheduleEvent e)
        {
            if (_waveNoticeGame != _game)
            {
                return;
            }

            if (e.Kind == WaveScheduleEventKind.HugeWaveWarning)
            {
                _hugeWaveNoticeStart = _game.Elapsed;
            }
            else if (e.Kind == WaveScheduleEventKind.HugeWaveCleared)
            {
                _hugeWaveNoticeStart = -1f;
            }
            else if (e.Kind == WaveScheduleEventKind.FinalWaveApproaching)
            {
                _finalWaveNoticeStart = _game.Elapsed;
            }
        }

        /// <summary>Performs dispose adventure wave notices.</summary>
        private void DisposeAdventureWaveNotices()
        {
            if (_waveNoticeGame != null)
            {
                _waveNoticeGame.WaveScheduleEventRaised -= HandleAdventureWaveNotice;
            }

            _waveNoticeGame = null;
            if (_bundledWaveAnimations != null)
            {
                _bundledWaveAnimations.Dispose();
                _bundledWaveAnimations = null;
            }
        }

        /// <summary>Stores adventure store.</summary>
        internal AdventureProgressStore _adventureStore;
        /// <summary>Stores adventure campaign.</summary>
        private AdventureCampaign _adventureCampaign;
        /// <summary>Stores adventure save path.</summary>
        private string _adventureSavePath;
        /// <summary>Stores adventure load error.</summary>
        private string _adventureLoadError;
        /// <summary>Stores selected adventure packet.</summary>
        internal int _selectedAdventurePacket = -1;
        /// <summary>Stores adventure feedback.</summary>
        internal string _adventureFeedback;
        /// <summary>Stores adventure feedback until.</summary>
        internal float _adventureFeedbackUntil;
        /// <summary>Stores adventure test board.</summary>
        private bool _adventureTestBoard;
        /// <summary>Stores choosing imitater.</summary>
        internal bool _choosingImitater;
        /// <summary>Stores adventure pointer.</summary>
        internal Vector2 _adventurePointer;
        /// <summary>The board that received the most recent insufficient-sun click.</summary>
        private GameSimulation? _sunWarningGame;
        /// <summary>The native update at which the sun display began its seventy-tick warning.</summary>
        private int _sunWarningStartTick;
        /// <summary>Gets the native update used by input feedback, which stops while the board is paused.</summary>
        private int SeedFeedbackTick => _game.IsAdventure ? _game.Adventure._ticks : Mathf.FloorToInt(_game.Elapsed * 100f);
        /// <summary>Gets whether the native insufficient-sun counter currently requests red text.</summary>
        internal bool SunMoneyWarningRed
        {
            get
            {
                var remaining = ReferenceEquals(_sunWarningGame, _game) ? Math.Max(0, 70 - (SeedFeedbackTick - _sunWarningStartTick)) : 0;
                return remaining > 0 && remaining % 20 < 10;
            }
        }

        /// <summary>Plays the original rejection sample for an unavailable seed or fixed chooser card.</summary>
        internal void PlaySeedBankBuzzer()
        {
            PlayPresentationSound("buzzer");
        }

        /// <summary>Reports an unavailable seed using the original sound and optional sun-display warning.</summary>
        /// <param name="missingSun">Whether the ready seed failed the native affordability check.</param>
        private void RejectSeedClick(bool missingSun)
        {
            PlaySeedBankBuzzer();
            if (missingSun)
            {
                _sunWarningGame = _game;
                _sunWarningStartTick = SeedFeedbackTick;
            }
        }

        /// <summary>Finds the exact plant highlighted and removed by the held shovel.</summary>
        /// <param name="point">The logical pointer position on the board.</param>
        /// <returns>The native tool target, or null when no removable plant is under the pointer.</returns>
        internal Plant? AdventureShovelTarget(Vector2 point)
        {
            if (!_game.IsAdventure || point.y < 80f || point.x < 0f || point.x >= 800f || point.y >= 600f)
            {
                return null;
            }

            var column = Mathf.FloorToInt((point.x - AdventureGridX) / CellWidth);
            var row = Mathf.FloorToInt((point.y - AdventureGridY - AdventureSlope(column)) / AdventurePitch);
            Plant? main = null, support = null, pumpkin = null;
            foreach (var plant in _game.Plants)
            {
                var x = AdventureGridX + plant.Column * CellWidth;
                var y = AdventureCellY(plant.Row, plant.Column);
                if (plant.Kind == PlantKind.Pumpkin)
                {
                    var hasMain = false;
                    foreach (var other in _game.Plants)
                    {
                        if (other.Row == plant.Row && other.Layer == PlantLayer.Main && other.Column <= plant.Column && plant.Column < other.Column + other.ColumnSpan)
                        {
                            hasMain = true;
                            break;
                        }
                    }

                    var distance = (point - new Vector2(x + 40f, y + 40f)).sqrMagnitude;
                    if (distance >= (hasMain ? 625f : 0f) && distance <= 2500f && point.y > y + 25f)
                    {
                        return plant;
                    }
                }

                if (plant.Kind == PlantKind.CoffeeBean && (point - new Vector2(x + 40f, y)).sqrMagnitude < 225f)
                {
                    return plant;
                }

                if (plant.Row != row || plant.Column > column || column >= plant.Column + plant.ColumnSpan)
                {
                    continue;
                }

                if (plant.Layer == PlantLayer.Main)
                {
                    main = plant;
                }
                else if (plant.Layer == PlantLayer.Support)
                {
                    support = plant;
                }
                else if (plant.Kind == PlantKind.Pumpkin)
                {
                    pumpkin = plant;
                }
            }

            var target = main ?? (pumpkin == null ? support : null);
            return target?.Kind == PlantKind.GraveBuster ? null : target;
        }
        /// <summary>Stores adventure action button.</summary>
        private static readonly Rect s_adventureActionButton = new Rect(316, 356, 168, 48);
        /// <summary>Stores adventure menu button.</summary>
        internal static readonly Rect s_adventureMenuButton = new Rect(704, 6, 90, 34);
        /// <summary>Stores adventure shovel button.</summary>
        internal static readonly Rect s_adventureShovelButton = new Rect(600, 0, 70, 72);
        /// <summary>Gets or sets use legacy board.</summary>
        internal bool UseLegacyBoard
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                return _experimentalLevelOne || Array.IndexOf(args, "-pvz-demo") >= 0 || (_smokeMode && Array.IndexOf(args, "-pvz-combat-fidelity-smoke") < 0 && Array.IndexOf(args, "-pvz-intro-audio-smoke") < 0 && Array.IndexOf(args, "-pvz-feedback-smoke") < 0 && Array.IndexOf(args, "-pvz-progression-smoke") < 0 && Array.IndexOf(args, "-pvz-adventure-smoke") < 0 && Array.IndexOf(args, "-pvz-ui-review") < 0);
            }
        }

        /// <summary>Performs ensure adventure store.</summary>
        /// <returns>The resulting ensure adventure store.</returns>
        internal bool EnsureAdventureStore()
        {
            if (_adventureStore != null)
            {
                return true;
            }

            if (_adventureLoadError != null)
            {
                return false;
            }

            try
            {
                if (_adventureSavePath == null)
                {
                    _adventureSavePath = _smokeMode ? Path.Combine(Application.temporaryCachePath, "pvz-campaign-" + Guid.NewGuid().ToString("N") + ".adventure") : Path.Combine(Application.persistentDataPath, "replica-campaign.adventure");
                }

                _adventureStore = new AdventureProgressStore(_adventureSavePath);
                return true;
            }
            catch (Exception error)
            {
                _adventureLoadError = error.Message;
                return false;
            }
        }

        /// <summary>Performs start adventure campaign.</summary>
        /// <returns>The resulting start adventure campaign.</returns>
        private bool StartAdventureCampaign()
        {
            if (_profiles == null || !_profiles.SelectedId.HasValue)
            {
                if (_profileDialog != null)
                {
                    _profileDialog.Open();
                }

                return false;
            }

            if (!EnsureAdventureStore() || !EnsurePlayerAccount())
            {
                SetUnavailable("冒险进度无法读取", _adventureLoadError);
                return false;
            }

            try
            {
                _adventureCampaign = new AdventureCampaign(_adventureStore, _profiles.SelectedId.Value, _accountStore);
                if (_adventureCampaign.Progress.Completed)
                {
                    _adventureStore.StartNextCycle(_profiles.SelectedId.Value);
                }

                _adventureTestBoard = false;
                AttachAdventureGame(_adventureCampaign.BeginOrResume());
                _currentView = ScreenView.Board;
                return true;
            }
            catch (Exception error)
            {
                SetUnavailable("冒险关卡无法开始", error.Message);
                return false;
            }
        }

        /// <summary>Performs attach adventure game.</summary>
        /// <param name = "simulation">The simulation.</param>
        private void AttachAdventureGame(GameSimulation simulation)
        {
            _game = simulation;
            _animations.ResetNativePlayback();
            _game.EventRaised = PlaySound;
            _game.Adventure.NoticeRaised = AdventureNotice;
            InitializeAdventureWaveNotices();
            _deck.Clear();
            foreach (PlantKind seed in _game.Adventure.SelectedSeeds)
            {
                _deck.Add(seed);
            }

            if (_game.Adventure.Level.Mode == AdventureMode.WhackAZombie)
            {
                _deck.AddRange(new[] { PlantKind.PotatoMine, PlantKind.CherryBomb, PlantKind.GraveBuster });
            }

            _choosingSeeds = _game.Adventure.Phase == AdventurePhase.ChoosingSeeds;
            _pendingSeeds = new AdventureSeedChooserState(_game.Adventure.SelectedSeeds);
            _selected = -1;
            _selectedAdventurePacket = -1;
            _shovel = false;
            _adventureFeedback = null;
            _choosingImitater = false;
            InitializeAdventureOpening();
        }

        /// <summary>Performs restart adventure.</summary>
        private void RestartAdventure()
        {
            if (_adventureTestBoard)
            {
                StartAdventureTestLevel(_game.Adventure.Level.Number);
                return;
            }

            try
            {
                AttachAdventureGame(_adventureCampaign.BeginOrResume());
            }
            catch (Exception error)
            {
                SetUnavailable("冒险关卡无法重新开始", error.Message);
            }
        }

        /// <summary>Performs start adventure test level.</summary>
        /// <param name = "number">The number.</param>
        /// <param name = "access">The access.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void StartAdventureTestLevel(int number, AdventureSeedAccess access = null)
        {
            if (!_smokeMode)
            {
                throw new InvalidOperationException("Test boards require an isolated smoke session.");
            }

            _adventureTestBoard = true;
            AttachAdventureGame(new GameSimulation(new GameRules { AdventureLevel = number, AdventureSeedAccess = access }, number * 104729));
            _currentView = ScreenView.Board;
        }

        /// <summary>Performs adventure notice.</summary>
        /// <param name = "notice">The notice.</param>
        private void AdventureNotice(AdventureNotice notice)
        {
            if (notice.Kind == AdventureNoticeKind.CoinCollectionStarted)
            {
                PlayAdventureCoinPickupSound((AdventureCoinKind)notice.Value);
                _presenter.ShowWalletUntil(_game.Elapsed + 10f);
            }

            if (notice.Kind == AdventureNoticeKind.CoinCollected)
            {
                _modeAccountSnapshot = null;
                _displayAccountSnapshot = null;
                _presenter.ShowWalletUntil(_game.Elapsed + 10f);
            }

            if (notice.Kind == AdventureNoticeKind.CoinCollectionFailed)
            {
                AdventureFeedback(_modeSaveError ?? ("金币尚未保存：" + (_adventureCampaign?.LastPersistenceError ?? "请重试")), 5f);
            }

            HandleAdventureRewardNoticeSound(notice);
        }

        /// <summary>Plays the original pickup sample when a money collection flight begins.</summary>
        /// <param name="kind">The silver, gold, or diamond pickup being collected.</param>
        private void PlayAdventureCoinPickupSound(AdventureCoinKind kind)
        {
            var diamond = kind == AdventureCoinKind.Diamond;
            if (_audioSource == null || !_presentationSounds.TryGetValue(diamond ? "diamond" : "coin", out var clip) || clip == null)
            {
                return;
            }

            if (diamond)
            {
                _audioSource.PlayOneShot(clip);
                return;
            }

            // Original Coin Foley uses the coin sample with a ten-semitone pitch range.
            var semitones = (float)_zombieFoleyRandom.NextDouble() * 10f;
            var frequency = Math.Max(100d, Math.Min(100000d, clip.frequency * Math.Pow(1.059463094359295d, semitones)));
            _soundVoices.TryPlay(clip, _soundVolume, (float)(frequency / clip.frequency), 31, 8);
        }

        /// <summary>Performs adventure feedback.</summary>
        /// <param name = "text">The text.</param>
        /// <param name = "duration">The duration.</param>
        private void AdventureFeedback(string text, float duration = 2.5f)
        {
            _adventureFeedback = _presenter.ResolveAdventureAdvice(text);
            _adventureFeedbackUntil = Time.unscaledTime + duration;
        }

        /// <summary>Preserves the original board message lifetime while gameplay is paused by a modal.</summary>
        /// <param name="delta">The unscaled frame duration consumed by the host clock.</param>
        internal void PreserveAdventureFeedbackDuringPause(float delta)
        {
            if (_currentView != ScreenView.Board || _game == null || !_game.IsAdventure || _adventureFeedback == null
                || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta))
            {
                return;
            }

            if (_game.State == MatchState.Paused || _optionsView != null && _optionsView.IsOpen
                || _confirmingQuit || _unavailableFeature != null || _profileDialog != null && _profileDialog.IsOpen)
            {
                _adventureFeedbackUntil += delta;
            }
        }

        /// <summary>Performs adventure seed rect.</summary>
        /// <param name = "index">The index.</param>
        /// <param name = "packet">The packet.</param>
        /// <returns>The resulting adventure seed rect.</returns>
        internal Rect AdventureSeedRect(int index, bool packet = false)
        {
            if (packet && _game != null && _game.UsesNativeAdventureConveyor && index < _game.Adventure.Packets.Count)
            {
                return new Rect(_game.AdventureConveyorCardX(index), 8, 50, 70);
            }

            if (packet)
            {
                return new Rect(8f + index * 50f, 8f, 50f, 70f);
            }

            var origin = AdventureSeedBankOrigin;
            return new Rect(origin.x + AdventureSeedSlotLayout.PositionX(index, SeedBankCapacity), origin.y + 8f, 50f, 70f);
        }

        /// <summary>Gets the bank's configured capacity independently of how many cards are currently selected.</summary>
        /// <remarks>Early first-adventure levels retain their original fixed packet counts.</remarks>
        internal int SeedBankCapacity
        {
            get
            {
                if (IsModeBoard)
                {
                    return _modeSession.PacketSlots;
                }

                if (_game.IsAdventure)
                {
                    return _game.Adventure.Level.PacketSlots;
                }

                return _choosingSeeds ? 7 : _deck.Count;
            }
        }

        /// <summary>Gets the native bank position, retaining the final introduction pose during the playing handoff.</summary>
        internal Vector2 AdventureSeedBankOrigin
        {
            get
            {
                return !IsModeBoard && _game.IsAdventure && _adventureIntroFrame != null
                    ? new Vector2(_adventureIntroFrame.SeedBankX, _adventureIntroFrame.SeedBankY)
                    : Vector2.zero;
            }
        }

        /// <summary>Performs adventure chooser rect.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting adventure chooser rect.</returns>
        internal static Rect AdventureChooserRect(int index)
        {
            return new Rect(118f + index % 10 * 57f, 126f + index / 10 * 80f, 50f, 70f);
        }

        /// <summary>Performs adventure plant name.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure plant name.</returns>
        internal string AdventurePlantName(PlantKind kind)
        {
            return kind == PlantKind.ExplodeONut ? "爆炸坚果" : _almanacView.PlantDisplayName((int)kind);
        }

        /// <summary>Performs contains required seed.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting contains required seed.</returns>
        internal bool ContainsRequiredSeed(PlantKind kind)
        {
            foreach (var seed in _game.Adventure.RequiredSeeds)
            {
                if (seed == kind)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Performs adventure reward name.</summary>
        /// <returns>The resulting adventure reward name.</returns>
        private string AdventureRewardName()
        {
            var level = _game.Adventure.Level;
            if (level.IsReplay)
            {
                return level.Number == 50 ? "获得金币奖励和向日葵奖杯" : "获得金币奖励";
            }

            if (level.RewardSeed.HasValue)
            {
                return "获得 " + AdventurePlantName(level.RewardSeed.Value);
            }

            switch (level.RewardKind)
            {
                case AdventureRewardKind.Shovel:
                    return "获得铲子";
                case AdventureRewardKind.Almanac:
                    return "获得图鉴";
                case AdventureRewardKind.CarKeys:
                    return "获得戴夫的车钥匙";
                case AdventureRewardKind.Taco:
                    return "获得玉米卷";
                case AdventureRewardKind.WateringCan:
                    return "获得浇水壶";
                case AdventureRewardKind.Note:
                    return "僵尸送来了一张纸条";
                default:
                    return "获得银向日葵奖杯";
            }
        }

        /// <summary>Performs adventure tutorial text.</summary>
        /// <param name = "tutorial">The tutorial.</param>
        /// <returns>The resulting adventure tutorial text.</returns>
        internal string? AdventureTutorialText(AdventureTutorial tutorial)
        {
            var shovelPlants = 0;
            foreach (var plant in _game.Plants)
            {
                if (plant.Kind == PlantKind.Peashooter && plant.Row == 2 && plant.Column >= 3 && plant.Column <= 5)
                {
                    shovelPlants++;
                }
            }

            var key = AdventureAdviceRules.TutorialKey(tutorial, _selected >= 0, _shovel, shovelPlants);
            return key == null ? null : _presenter.OriginalProgressionText(key);
        }

        /// <summary>Performs handle adventure click.</summary>
        /// <param name = "point">The point.</param>
        private void HandleAdventureClick(Vector2 point)
        {
            var board = _game.Adventure;
            if (AdventureIntroActive && !AdventureIntroAwaitingSeeds)
            {
                return;
            }

            if (s_adventureMenuButton.Contains(point))
            {
                OpenOptions();
                return;
            }

            if (_choosingSeeds)
            {
                var unlocked = board.AvailableSeeds;
                var required = Math.Min(board.Level.PacketSlots, unlocked.Count);
                _pendingSeeds.LandFlyingSeeds();
                if (_choosingImitater)
                {
                    var targets = board.Level.UnlockedSeeds;
                    for (var i = 0; i < targets.Count; i++)
                    {
                        if (AdventureCampaignChooserRect(targets[i]).Contains(point) && _game.SetAdventureImitaterTarget(targets[i]))
                        {
                            var home = AdventureCampaignChooserRect(PlantKind.Imitater);
                            _pendingSeeds.Choose(PlantKind.Imitater, (int)home.x, (int)home.y, SeedBankCapacity);
                            PlayAdventureRewardSound("tap2.ogg");
                            _choosingImitater = false;
                            return;
                        }
                    }

                    if (AdventureChooserStartRect.Contains(point))
                    {
                        _choosingImitater = false;
                    }

                    return;
                }

                if (ReturnSelectedChooserSeed(point, SeedBankCapacity))
                {
                    return;
                }

                for (var i = 0; i < unlocked.Count; i++)
                {
                    if (AdventureCampaignChooserRect(unlocked[i]).Contains(point))
                    {
                        if (ContainsRequiredSeed(unlocked[i]))
                        {
                            return;
                        }

                        if (!_pendingSeeds.Contains(unlocked[i]) && _pendingSeeds.Count < required)
                        {
                            if (unlocked[i] == PlantKind.Imitater)
                            {
                                _choosingImitater = true;
                            }
                            else
                            {
                                var home = AdventureCampaignChooserRect(unlocked[i]);
                                _pendingSeeds.Choose(unlocked[i], (int)home.x, (int)home.y, SeedBankCapacity);
                                PlayAdventureRewardSound("tap2.ogg");
                            }
                        }

                        return;
                    }
                }

                if (AdventureChooserStartRect.Contains(point) && _pendingSeeds.Count == required)
                {
                    var seeds = new List<PlantKind>(_pendingSeeds);

                    try
                    {
                        var chosen = _adventureTestBoard ? _game.ChooseAdventureSeeds(seeds) : _adventureCampaign.ChooseSeeds(seeds);
                        if (chosen)
                        {
                            _choosingSeeds = false;
                            _deck.Clear();
                            _deck.AddRange(seeds);
                            BeginAdventureOpeningReturn();
                        }
                    }
                    catch (Exception error)
                    {
                        AdventureFeedback("无法保存选卡：" + error.Message, 5f);
                    }
                }

                return;
            }

            if (board.Phase == AdventurePhase.Reward || board.Phase == AdventurePhase.Finished)
            {
                HandleAdventureRewardClick(point);
                return;
            }

            if (_game.State == MatchState.Lost)
            {
                if (new Rect(316, 340, 168, 48).Contains(point))
                {
                    RestartAdventure();
                }

                return;
            }

            if (_game.State == MatchState.Paused)
            {
                if (new Rect(316, 280, 168, 48).Contains(point))
                {
                    _game.TogglePause();
                }
                else if (new Rect(316, 340, 168, 48).Contains(point))
                {
                    RestartAdventure();
                }

                return;
            }

            for (var i = board.Coins.Count - 1; i >= 0; i--)
            {
                if (AdventureCoinRect(board.Coins[i]).Contains(point))
                {
                    if (!_game.CollectAdventureCoin(board.Coins[i]) && _adventureCampaign != null && _adventureCampaign.LastPersistenceError != null)
                    {
                        AdventureFeedback("金币尚未保存：" + _adventureCampaign.LastPersistenceError, 5f);
                    }

                    return;
                }
            }

            for (var i = _game.Suns.Count - 1; i >= 0; i--)
            {
                if (AdventureSunRect(_game.Suns[i]).Contains(point))
                {
                    _game.CollectSun(_game.Suns[i]);
                    return;
                }
            }

            var packets = board.Level.HasConveyor || board.Level.Mode == AdventureMode.Vasebreaker;
            if (packets)
            {
                for (var i = 0; i < board.Packets.Count && i < 10; i++)
                {
                    if (AdventurePacketContains(i, point))
                    {
                        _selectedAdventurePacket = _selectedAdventurePacket == board.Packets[i].Id ? -1 : board.Packets[i].Id;
                        _selected = -1;
                        _shovel = false;
                        _game.CancelAdventureCobCannonAim();
                        return;
                    }
                }
            }
            else
            {
                for (var i = 0; i < _deck.Count; i++)
                {
                    if (AdventureSeedRect(i).Contains(point))
                    {
                        var context = _game.AdventurePacketContext(_deck[i]);
                        if (AdventureSeedBankPresentation.CanPickUp(context) && (board.Level.Mode == AdventureMode.WhackAZombie || _game.SelectAdventureSeed(_deck[i])))
                        {
                            _selected = _selected == i ? -1 : i;
                            PlayPresentationSound("seedlift");
                        }
                        else
                        {
                            RejectSeedClick(context.Active && !context.CanAfford);
                        }

                        _shovel = false;
                        _selectedAdventurePacket = -1;
                        _game.CancelAdventureCobCannonAim();
                        return;
                    }
                }
            }

            if (board.Level.Number >= 5 && s_adventureShovelButton.Contains(point))
            {
                _shovel = !_shovel;
                PlayPresentationSound("shovel");
                _selected = -1;
                _selectedAdventurePacket = -1;
                _game.CancelAdventureCobCannonAim();
                return;
            }

            var col = Mathf.FloorToInt((point.x - AdventureGridX) / CellWidth);
            var row = Mathf.FloorToInt((point.y - AdventureGridY - AdventureSlope(col)) / AdventurePitch);
            if (col < 0 || col > 8 || row < 0 || row >= _game.RowCount)
            {
                return;
            }

            if (board.SelectedCobCannonId.HasValue)
            {
                _game.TryFireAdventureCobCannonAtCell(row, col);
                return;
            }

            if (_shovel)
            {
                if (_game.TryShovelAdventurePlant(AdventureShovelTarget(point)))
                {
                    _shovel = false;
                    PlayPresentationSound("plant");
                }

                return;
            }

            if (_selectedAdventurePacket >= 0)
            {
                if (_game.TryUseAdventurePacket(_selectedAdventurePacket, row, col))
                {
                    _selectedAdventurePacket = -1;
                }
                else
                {
                    AdventurePlantingFeedback();
                }

                return;
            }

            if (_selected >= 0)
            {
                if (_game.TryPlant(_deck[_selected], row, col))
                {
                    _selected = -1;
                }
                else
                {
                    AdventurePlantingFeedback();
                }

                return;
            }

            if (_game.TryAimAdventureCobCannon(row, col))
            {
                return;
            }

            if (board.Level.Mode == AdventureMode.Vasebreaker)
            {
                _game.SmashAdventureVase(row, col);
                return;
            }

            if (board.Level.Mode == AdventureMode.WhackAZombie)
            {
                for (var i = _game.Zombies.Count - 1; i >= 0; i--)
                {
                    var zombie = _game.Zombies[i];
                    var x = AdventureGridX + zombie.X * CellWidth;
                    if (new Rect(x - 48, AdventureCellY(zombie.Row, zombie.X) - 35, 96, 140).Contains(point))
                    {
                        _game.HammerAdventureZombie(zombie.Id);
                        return;
                    }
                }
            }
        }

        /// <summary>Performs adventure planting feedback.</summary>
        private void AdventurePlantingFeedback()
        {
            if (_game.LastAdventurePlantingFailure == AdventurePlantingFailure.Tutorial)
            {
                return;
            }

            var kind = _selected >= 0 && _selected < _deck.Count ? _deck[_selected] : PlantKind.Peashooter;
            foreach (var packet in _game.Adventure.Packets)
            {
                if (packet.Id == _selectedAdventurePacket)
                {
                    kind = packet.Kind;
                    break;
                }
            }
            if (kind == PlantKind.Imitater && _game.Adventure.ImitaterTarget.HasValue)
            {
                kind = _game.Adventure.ImitaterTarget.Value;
            }

            var key = AdventureAdviceRules.PlantingKey(_game.LastAdventurePlantingFailure, kind, !_game.Adventure.Level.IsReplay && _game.Adventure.Level.Number == 41);
            if (key != null)
            {
                AdventureFeedback(_presenter.OriginalProgressionText(key), 5f);
            }
        }

        // Modes and garden pages may set this on entry, then clear it on return to the selector.
        /// <summary>Stores dynamic music override.</summary>
        public MusicCue? DynamicMusicOverride;
        /// <summary>Stores dynamic music clips.</summary>
        private readonly Dictionary<string, AudioClip> _dynamicMusicClips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        /// <summary>Stores dynamic music info.</summary>
        private readonly Dictionary<string, NativeMusicClip> _dynamicMusicInfo = new Dictionary<string, NativeMusicClip>(StringComparer.Ordinal);
        /// <summary>Stores presentation sounds.</summary>
        private readonly Dictionary<string, AudioClip> _presentationSounds = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        /// <summary>Stores presentation resource clips.</summary>
        private readonly HashSet<AudioClip> _presentationResourceClips = new HashSet<AudioClip>();
        /// <summary>Stores dynamic music stems.</summary>
        private OriginalMusicStem[] _dynamicMusicStems;
        /// <summary>Stores dynamic burst.</summary>
        private readonly OriginalMusicBurst _dynamicBurst = new OriginalMusicBurst();
        /// <summary>Stores playing music cue.</summary>
        private MusicCue _playingMusicCue = MusicCue.None;
        /// <summary>Stores subscribed audio game.</summary>
        private GameSimulation _subscribedAudioGame;
        /// <summary>Stores dynamic audio ready.</summary>
        private bool _dynamicAudioReady;
        /// <summary>Stores dynamic stem info.</summary>
        private readonly NativeMusicClip[] _dynamicStemInfo = new NativeMusicClip[3];
        /// <summary>Stores dynamic music next tick dsp.</summary>
        private double _dynamicMusicNextTickDsp = double.PositiveInfinity;
        /// <summary>Stores dynamic music clock paused.</summary>
        private bool _dynamicMusicClockPaused;
        /// <summary>Stores dynamic music paused at.</summary>
        private double _dynamicMusicPausedAt;
        /// <summary>Stores dynamic audio tasks,  dynamic audio completed.</summary>
        private int _dynamicAudioTasks, _dynamicAudioCompleted;
        /// <summary>Stores owned music listener.</summary>
        private AudioListener _ownedMusicListener;
        /// <summary>Stores next audio listener check.</summary>
        private float _nextAudioListenerCheck;
        /// <summary>Stores dynamic audio failures.</summary>
        private int _dynamicAudioFailures;
        /// <summary>Stores music output probe running.</summary>
        private bool _musicOutputProbeRunning;
        /// <summary>Gets or sets dynamic audio ready.</summary>
        public bool DynamicAudioReady
        {
            get
            {
                return _dynamicAudioReady;
            }
        }

        /// <summary>Gets or sets dynamic audio progress.</summary>
        private float DynamicAudioProgress
        {
            get
            {
                return _dynamicAudioReady ? 1f : _dynamicAudioCompleted / (float)Math.Max(1, _dynamicAudioTasks);
            }
        }

        /// <summary>Performs initialize dynamic audio.</summary>
        private void InitializeDynamicAudio()
        {
            if (_dynamicMusicStems != null)
            {
                return;
            }

            EnsureOriginalAudioListener();
            _dynamicMusicStems = new[]
            {
                new OriginalMusicStem(gameObject, _musicSource),
                new OriginalMusicStem(gameObject),
                new OriginalMusicStem(gameObject)
            };
            StartCoroutine(LoadDynamicOriginalAudio());
            StartMusicProbeFromArguments();
        }

        /// <summary>Performs load dynamic original audio.</summary>
        /// <returns>The resulting load dynamic original audio.</returns>
        private IEnumerator LoadDynamicOriginalAudio()
        {
            var json = Resources.Load<TextAsset>("PvZMusic/native_music");
            var manifest = json == null ? null : JsonUtility.FromJson<NativeMusicManifest>(json.text);
            string[] cues =
            {
                "hugewave",
                "finalwave",
                "siren",
                "awooga",
                "loadingbar_flower",
                "loadingbar_zombie",
                "buttonclick",
                "reverse_explosion",
                "cherrybomb",
                "juicy",
                "dirt_rise",
                "buzzer",
                "seedlift",
                "shovel",
                "plant",
                "coin",
                "diamond"
            };
            _dynamicAudioTasks = (manifest == null || manifest.clips == null ? 0 : manifest.clips.Length) + cues.Length;
            if (manifest != null && manifest.clips != null)
            {
                foreach (var entry in manifest.clips)
                {
                    var request = Resources.LoadAsync<AudioClip>(entry.resource);
                    yield return request;
                    var clip = request.asset as AudioClip;
                    if (clip != null)
                    {
                        clip.LoadAudioData();
                        while (clip.loadState == AudioDataLoadState.Loading)
                        {
                            yield return null;
                        }

                        if (clip.loadState == AudioDataLoadState.Loaded && clip.samples > 0)
                        {
                            _dynamicMusicClips[entry.key] = clip;
                            _dynamicMusicInfo[entry.key] = entry;
                        }
                        else
                        {
                            _dynamicAudioFailures++;
                            Debug.LogWarning("Original tracker failed to decode: " + entry.resource + " (" + clip.loadState + ")");
                        }
                    }
                    else
                    {
                        _dynamicAudioFailures++;
                        Debug.LogWarning("Original tracker stem is missing: " + entry.resource);
                    }

                    _dynamicAudioCompleted++;
                }
            }
            else
            {
                _dynamicAudioFailures++;
                Debug.LogWarning("Original music manifest is missing. Run Audio/Tools/prepare_original_music.py before building.");
            }

            foreach (string cue in cues)
            {
                var diamond = cue == "diamond";
                var path = Path.Combine(_root, "sounds", cue + (diamond ? ".wav" : ".ogg"));
                if (File.Exists(path))
                {
                    using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, diamond ? AudioType.WAV : AudioType.OGGVORBIS))
                    {
                        yield return request.SendWebRequest();
                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            _presentationSounds[cue] = DownloadHandlerAudioClip.GetContent(request);
                        }
                    }
                }

                if (!_presentationSounds.ContainsKey(cue))
                {
                    // The shipped StreamingAssets set is intentionally small. Wave cues must also travel
                    // as original Resources audio, otherwise an event can fire successfully into silence.
                    var cherry = cue == "reverse_explosion" || cue == "cherrybomb" || cue == "juicy";
                    var bundled = Resources.LoadAsync<AudioClip>((cherry ? "PvZCherryBombSounds/" : cue == "dirt_rise" ? "PvZMineSounds/" : "PvZWaveNotices/") + cue);
                    yield return bundled;
                    var clip = bundled.asset as AudioClip;
                    if (clip != null)
                    {
                        clip.LoadAudioData();
                        while (clip.loadState == AudioDataLoadState.Loading)
                        {
                            yield return null;
                        }

                        if (clip.loadState == AudioDataLoadState.Loaded)
                        {
                            _presentationSounds[cue] = clip;
                            _presentationResourceClips.Add(clip);
                        }
                    }
                }

                _dynamicAudioCompleted++;
            }

            _dynamicAudioReady = true;
            // A context may start while only its base clip is loaded. Restart that same original cue once
            // the complete stem set is resident so the drums/hihats are not permanently left unassigned.
            if (_playingMusicCue != MusicCue.None)
            {
                PlayOriginalMusicCue(_playingMusicCue);
            }
        }

        /// <summary>Performs update dynamic audio.</summary>
        private void UpdateDynamicAudio()
        {
            if (_dynamicMusicStems == null)
            {
                return;
            }

            UpdateAdventureCherryBombAudio();
            UpdateAdventurePotatoMineAudio();
            if (Time.unscaledTime >= _nextAudioListenerCheck)
            {
                EnsureOriginalAudioListener();
                _nextAudioListenerCheck = Time.unscaledTime + 2f;
            }

            if (_subscribedAudioGame != _game)
            {
                if (_subscribedAudioGame != null)
                {
                    _subscribedAudioGame.WaveScheduleEventRaised -= HandleScheduledWaveAudio;
                }

                _subscribedAudioGame = _game;
                if (_game != null)
                {
                    _game.WaveScheduleEventRaised += HandleScheduledWaveAudio;
                }
            }

            var desired = ResolveOriginalMusicCue();
            if (desired != _playingMusicCue || desired != MusicCue.None && !_dynamicMusicStems[0].IsPlaying)
            {
                PlayOriginalMusicCue(desired);
            }

            var paused = _optionsView != null && _optionsView.IsOpen || _currentView == ScreenView.Board && _game != null && _game.State == MatchState.Paused;
            var now = AudioSettings.dspTime;
            var volume = _optionsView != null && _optionsView.IsOpen ? _optionsView.MusicVolume : _musicVolume;
            if (paused)
            {
                if (!_dynamicMusicClockPaused)
                {
                    _dynamicMusicClockPaused = true;
                    _dynamicMusicPausedAt = now;
                }

                foreach (var stem in _dynamicMusicStems)
                {
                    stem.Update(volume, true);
                }

                return;
            }

            if (_dynamicMusicClockPaused)
            {
                _dynamicMusicNextTickDsp += Math.Max(0, now - _dynamicMusicPausedAt) + OriginalMusicStem.SchedulingLeadSeconds;
                foreach (var stem in _dynamicMusicStems)
                {
                    stem.Update(volume, false);
                }

                _dynamicMusicClockPaused = false;
            }

            var living = 0;
            if (_game != null)
            {
                foreach (Zombie z in _game.Zombies)
                {
                    if (z.Health > 0f && !z.Headless && !z.Hypnotized && z.Kind != ZombieKind.Bungee)
                    {
                        living++;
                    }
                }
            }

            if (IsTerrainCue(desired) && _dynamicStemInfo[0] != null && _dynamicMusicStems[0].IsPlaying)
            {
                while (_dynamicMusicNextTickDsp <= now + 0.00000001)
                {
                    var info = _dynamicStemInfo[0];
                    var sample = _dynamicMusicStems[0].NativeSampleAtDsp(_dynamicMusicNextTickDsp, info.sampleRate);
                    _dynamicBurst.Tick(living, desired == MusicCue.Night, info.PositionAtSample(sample));
                    if (_dynamicBurst.RequestedNightOrder != -1)
                    {
                        // Apply the native order jump with its original time origin. The seek includes the
                        // elapsed part of the row, so a main-thread frame cannot restart a late drum attack.
                        var playbackDsp = now + OriginalMusicStem.SchedulingLeadSeconds;
                        var key = _dynamicBurst.RequestedNightOrder == 76 ? "night_burst" : "night_burst77";
                        NativeMusicClip jump;
                        if (_dynamicMusicInfo.TryGetValue(key, out jump))
                        {
                            var phase = (int)Math.Floor(Math.Max(0, playbackDsp - _dynamicMusicNextTickDsp) * jump.sampleRate);
                            PlayOriginalStem(1, key, playbackDsp, phase);
                        }
                    }

                    _dynamicMusicNextTickDsp += 0.01;
                }
            }

            _dynamicMusicStems[0].Gain = desired == MusicCue.Night ? _dynamicBurst.NightBase : 1f;
            _dynamicMusicStems[1].Gain = _dynamicBurst.Drums;
            _dynamicMusicStems[2].Gain = _dynamicBurst.HiHats;
            foreach (var stem in _dynamicMusicStems)
            {
                stem.Update(volume, false);
            }
        }

        /// <summary>Performs resolve original music cue.</summary>
        /// <returns>The resulting resolve original music cue.</returns>
        private MusicCue ResolveOriginalMusicCue()
        {
            if (OriginalLoadingActive)
            {
                return MusicCue.Title;
            }

            // PC CutScene::StartLevelIntro 0x446F64 selects tune 6 (ChooseYourSeeds).
            if (AdventureIntroActive && _currentView == ScreenView.Board && _game.Adventure.Level.Mode != AdventureMode.Boss)
            {
                return MusicCue.ChooseSeeds;
            }

            if (_currentView == ScreenView.Board && _game != null)
            {
                MusicCue awardCue;
                if (TryResolveAdventureRewardMusic(out awardCue))
                {
                    return awardCue;
                }

                if (_game.State == MatchState.Won || _game.State == MatchState.Lost)
                {
                    return MusicCue.None;
                }

                if (_choosingSeeds || _game.IsAdventure && _game.Adventure.Phase == AdventurePhase.ChoosingSeeds)
                {
                    return MusicCue.ChooseSeeds;
                }
            }

            if (DynamicMusicOverride.HasValue)
            {
                return DynamicMusicOverride.Value;
            }

            if (_currentView != ScreenView.Board || _game == null)
            {
                return MusicCue.Title;
            }

            if (!_game.IsAdventure)
            {
                return MusicCue.Day;
            }

            var level = _game.Adventure.Level;
            if (level.Mode == AdventureMode.Boss)
            {
                return MusicCue.Boss;
            }

            if (level.Mode == AdventureMode.Storm)
            {
                return MusicCue.None;
            }

            if (level.Mode == AdventureMode.Bowling || level.Mode == AdventureMode.WhackAZombie || level.Mode == AdventureMode.LittleTrouble || level.Mode == AdventureMode.Bungee)
            {
                return MusicCue.Minigame;
            }

            if (level.Mode == AdventureMode.Vasebreaker)
            {
                return MusicCue.Puzzle;
            }

            if (level.Number == 10 || level.Number == 20 || level.Number == 30)
            {
                return MusicCue.Conveyor;
            }

            if (level.Terrain == AdventureTerrain.Fog)
            {
                return MusicCue.Fog;
            }

            if (level.IsNight)
            {
                return MusicCue.Night;
            }

            if (level.HasPool)
            {
                return MusicCue.Pool;
            }

            if (level.HasRoof)
            {
                return MusicCue.Roof;
            }

            return MusicCue.Day;
        }

        /// <summary>Performs is terrain cue.</summary>
        /// <param name = "cue">The cue.</param>
        /// <returns>The resulting is terrain cue.</returns>
        private static bool IsTerrainCue(MusicCue cue)
        {
            return cue == MusicCue.Day || cue == MusicCue.Night || cue == MusicCue.Pool || cue == MusicCue.Fog || cue == MusicCue.Roof;
        }

        /// <summary>Performs play original music cue.</summary>
        /// <param name = "cue">The cue.</param>
        private void PlayOriginalMusicCue(MusicCue cue)
        {
            foreach (var stem in _dynamicMusicStems)
            {
                stem.Stop();
            }

            _playingMusicCue = cue;
            _dynamicBurst.Reset();
            _dynamicMusicClockPaused = false;
            _dynamicMusicNextTickDsp = double.PositiveInfinity;
            Array.Clear(_dynamicStemInfo, 0, _dynamicStemInfo.Length);
            string key;
            switch (cue)
            {
                case MusicCue.Title:
                    key = "title";
                    break;
                case MusicCue.ChooseSeeds:
                    key = "choose";
                    break;
                case MusicCue.Day:
                    key = "day_base";
                    break;
                case MusicCue.Night:
                    key = "night_base";
                    break;
                case MusicCue.Pool:
                    key = "pool_base";
                    break;
                case MusicCue.Fog:
                    key = "fog_base";
                    break;
                case MusicCue.Roof:
                    key = "roof_base";
                    break;
                case MusicCue.Boss:
                    key = "boss";
                    break;
                case MusicCue.Minigame:
                    key = "minigame";
                    break;
                case MusicCue.Puzzle:
                    key = "puzzle";
                    break;
                case MusicCue.Conveyor:
                    key = "conveyor";
                    break;
                case MusicCue.ZenGarden:
                    key = "garden";
                    break;
                default:
                    return;
            }

            var start = AudioSettings.dspTime + OriginalMusicStem.SchedulingLeadSeconds;
            _dynamicMusicNextTickDsp = start;
            _dynamicMusicStems[0].Gain = 1f;
            _dynamicMusicStems[1].Gain = _dynamicMusicStems[2].Gain = 0f;
            PlayOriginalStem(0, key, start);
            if (IsTerrainCue(cue))
            {
                var prefix = key.Substring(0, key.Length - 5);
                PlayOriginalStem(1, cue == MusicCue.Night ? "night_standby" : prefix + "_drums", start);
                if (cue != MusicCue.Night)
                {
                    PlayOriginalStem(2, prefix + "_hihats", start);
                }
            }
        }

        /// <summary>Performs play original stem.</summary>
        /// <param name = "index">The index.</param>
        /// <param name = "key">The key.</param>
        /// <param name = "dsp">The dsp.</param>
        /// <param name = "nativeStartSample">The native start sample.</param>
        private void PlayOriginalStem(int index, string key, double dsp, int nativeStartSample = 0)
        {
            AudioClip clip;
            NativeMusicClip info;
            if (_dynamicMusicClips.TryGetValue(key, out clip) && _dynamicMusicInfo.TryGetValue(key, out info))
            {
                _dynamicStemInfo[index] = info;
                _dynamicMusicStems[index].Play(clip, info.loopStartSample, info.sampleRate, dsp, nativeStartSample);
            }
        }

        /// <summary>Performs handle scheduled wave audio.</summary>
        /// <param name = "e">The e.</param>
        private void HandleScheduledWaveAudio(WaveScheduleEvent e)
        {
            if (_currentView != ScreenView.Board || OriginalLoadingActive)
            {
                return;
            }

            if (e.Kind == WaveScheduleEventKind.HugeWaveSound)
            {
                PlayPresentationSound("hugewave");
            }
            else if (e.Kind == WaveScheduleEventKind.FinalWaveSound)
            {
                PlayPresentationSound("finalwave");
            }
            else if (e.Kind == WaveScheduleEventKind.SirenSound)
            {
                PlayPresentationSound("siren");
            }
            else if (e.Kind == WaveScheduleEventKind.FirstWaveApproaching)
            {
                PlayPresentationSound("awooga");
            }
            else if (e.Kind == WaveScheduleEventKind.MusicBurst)
            {
                _dynamicBurst.ForceBurst();
            }
        }

        /// <summary>Performs play presentation sound.</summary>
        /// <param name = "name">The name.</param>
        private void PlayPresentationSound(string name)
        {
            AudioClip clip;
            if (_audioSource != null && _presentationSounds.TryGetValue(name, out clip) && clip != null)
            {
                _audioSource.PlayOneShot(clip, 0.7f);
            }
        }

        /// <summary>Performs ensure original audio listener.</summary>
        private void EnsureOriginalAudioListener()
        {
            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var listener in listeners)
            {
                if (listener != _ownedMusicListener && listener.isActiveAndEnabled)
                {
                    if (_ownedMusicListener != null)
                    {
                        _ownedMusicListener.enabled = false;
                    }

                    return;
                }
            }

            if (_ownedMusicListener == null)
            {
                // Lawn stores the game bootstrap; the presentation camera does not own an audio listener.
                // A 2D source still needs one listener to reach Unity's output mixer.
                var listenerObject = new GameObject("PvZ Audio Listener");
                listenerObject.transform.SetParent(transform, false);
                _ownedMusicListener = listenerObject.AddComponent<AudioListener>();
                Debug.Log("Original audio installed the missing listener for the presentation scene.");
            }

            _ownedMusicListener.enabled = true;
        }

        /// <summary>Performs get music output report.</summary>
        /// <returns>The resulting get music output report.</returns>
        public MusicOutputReport GetMusicOutputReport()
        {
            var listeners = 0;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (listener.isActiveAndEnabled)
                {
                    listeners++;
                }
            }

            var states = _dynamicMusicStems == null ? new OriginalMusicStemState[0] : new OriginalMusicStemState[_dynamicMusicStems.Length];
            for (var i = 0; i < states.Length; i++)
            {
                states[i] = _dynamicMusicStems[i].Describe();
            }

            return new MusicOutputReport
            {
                cue = _playingMusicCue.ToString(),
                loadingReady = _dynamicAudioReady,
                listeners = listeners,
                decodedClips = _dynamicMusicClips.Count,
                decodeFailures = _dynamicAudioFailures,
                savedMusicVolume = _musicVolume,
                effectiveMusicVolume = _optionsView != null && _optionsView.IsOpen ? _optionsView.MusicVolume : _musicVolume,
                listenerVolume = AudioListener.volume,
                listenerPaused = AudioListener.pause,
                gamePaused = _dynamicMusicClockPaused,
                outputSampleRate = AudioSettings.outputSampleRate,
                stems = states
            };
        }

        /// <summary>Performs begin music output probe.</summary>
        /// <param name = "reportPath">The report path.</param>
        /// <param name = "seconds">The seconds.</param>
        /// <param name = "quitWhenFinished">The quit when finished.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        public void BeginMusicOutputProbe(string reportPath, float seconds = 6f, bool quitWhenFinished = false)
        {
            if (string.IsNullOrEmpty(reportPath))
            {
                throw new ArgumentException("A report path is required.", nameof(reportPath));
            }

            if (_musicOutputProbeRunning)
            {
                throw new InvalidOperationException("A music output probe is already running.");
            }

            _musicOutputProbeRunning = true;
            StartCoroutine(ProbeMusicOutput(Path.GetFullPath(reportPath), Mathf.Clamp(seconds, 1f, 30f), quitWhenFinished));
        }

        /// <summary>Performs start music probe from arguments.</summary>
        private void StartMusicProbeFromArguments()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-pvz-audio-probe");
            if (index < 0)
            {
                return;
            }

            var path = index + 1 < args.Length && !args[index + 1].StartsWith("-", StringComparison.Ordinal) ? args[index + 1] : Path.Combine(Application.persistentDataPath, "music-output.json");
            var seconds = 6f;
            var duration = Array.IndexOf(args, "-pvz-audio-probe-seconds");
            if (duration >= 0 && duration + 1 < args.Length)
            {
                float.TryParse(args[duration + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds);
            }

            BeginMusicOutputProbe(path, seconds <= 0 ? 6f : seconds, Array.IndexOf(args, "-pvz-audio-probe-quit") >= 0);
        }

        /// <summary>Performs probe music output.</summary>
        /// <param name = "reportPath">The report path.</param>
        /// <param name = "duration">The duration.</param>
        /// <param name = "quitWhenFinished">The quit when finished.</param>
        /// <returns>The resulting probe music output.</returns>
        private IEnumerator ProbeMusicOutput(string reportPath, float duration, bool quitWhenFinished)
        {
            var deadline = Time.realtimeSinceStartup + 45f;
            while (!_dynamicAudioReady && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            // Allow the actual scheduled start, decoder and at least one DSP output block to finish.
            yield return new WaitForSecondsRealtime(0.4f);
            var start = Time.realtimeSinceStartup;
            var report = GetMusicOutputReport();
            report.dspStart = AudioSettings.dspTime;
            var pcm = new float[1024];
            var probeInterval = new WaitForSecondsRealtime(0.02f);
            while (Time.realtimeSinceStartup - start < duration)
            {
                var musicVoice = false;
                if (_dynamicMusicStems != null)
                {
                    foreach (var stem in _dynamicMusicStems)
                    {
                        float peak;
                        int samples;
                        report.sourceEnergy += stem.ReadOutputEnergy(pcm, out peak, out samples);
                        report.sourceSamples += samples;
                        report.sourcePeak = Math.Max(report.sourcePeak, peak);
                        if (samples > 0 && stem.Gain > 0f)
                        {
                            musicVoice = true;
                        }
                    }
                }

                AudioListener.GetOutputData(pcm, 0);
                var musicOnly = musicVoice && _audioSource != null && !_audioSource.isPlaying && !_dynamicMusicClockPaused;
                foreach (float sample in pcm)
                {
                    if (float.IsNaN(sample) || float.IsInfinity(sample))
                    {
                        continue;
                    }

                    var peak = Math.Abs(sample);
                    var energy = sample * (double)sample;
                    report.listenerEnergy += energy;
                    report.listenerSamples++;
                    report.listenerPeak = Math.Max(report.listenerPeak, peak);
                    if (musicOnly)
                    {
                        report.musicOnlyEnergy += energy;
                        report.musicOnlySamples++;
                        report.musicOnlyPeak = Math.Max(report.musicOnlyPeak, peak);
                    }
                }

                report.windows++;
                if (musicOnly)
                {
                    report.musicOnlyWindows++;
                }

                yield return probeInterval;
            }

            var end = GetMusicOutputReport();
            report.cue = end.cue;
            report.loadingReady = end.loadingReady;
            report.listeners = end.listeners;
            report.decodedClips = end.decodedClips;
            report.decodeFailures = end.decodeFailures;
            report.listenerPaused = end.listenerPaused;
            report.gamePaused = end.gamePaused;
            report.savedMusicVolume = end.savedMusicVolume;
            report.effectiveMusicVolume = end.effectiveMusicVolume;
            report.listenerVolume = end.listenerVolume;
            report.outputSampleRate = end.outputSampleRate;
            report.stems = end.stems;
            report.dspEnd = AudioSettings.dspTime;
            report.seconds = Time.realtimeSinceStartup - start;
            report.outputPath = reportPath;
            report.sourcePcmNonzero = report.sourcePeak > 0.000001f;
            report.listenerPcmNonzero = report.listenerPeak > 0.000001f;
            report.musicOnlyPcmNonzero = report.musicOnlyPeak > 0.000001f;
            report.status = report.sourcePcmNonzero && report.musicOnlyPcmNonzero ? "PASS" : report.effectiveMusicVolume <= 0f || report.listenerVolume <= 0f ? "MUTED_BY_VOLUME" : report.listenerPaused || report.gamePaused ? "PAUSED" : "NO_MUSIC_OUTPUT";
            report.explanation = "PCM sampled from native AudioSource outputs and final AudioListener mix; music-only windows exclude presentation/SFX playback. User volumes were not changed. Physical OS/device output is outside this probe.";
            var parent = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            Debug.Log("Original music PCM probe " + report.status + ": " + reportPath);
            _musicOutputProbeRunning = false;
            if (quitWhenFinished)
            {
                Application.Quit(report.status == "PASS" ? 0 : 2);
            }
        }

        /// <summary>Performs dispose dynamic audio.</summary>
        private void DisposeDynamicAudio()
        {
            if (_subscribedAudioGame != null)
            {
                _subscribedAudioGame.WaveScheduleEventRaised -= HandleScheduledWaveAudio;
            }

            if (_dynamicMusicStems != null)
            {
                foreach (var stem in _dynamicMusicStems)
                {
                    stem.Dispose();
                }
            }

            foreach (var clip in _presentationSounds.Values)
            {
                if (clip != null && !_presentationResourceClips.Contains(clip))
                {
                    Destroy(clip);
                }
            }

            _presentationSounds.Clear();
            _presentationResourceClips.Clear();
            DisposeAdventureWaveNotices();
            DisposeAdventureCherryBombAudio();
            // Resources owns the imported music clips. They are not the runtime clips in LoadSoundsCore.
            _dynamicMusicClips.Clear();
            _dynamicMusicInfo.Clear();
            _dynamicMusicStems = null;
            if (_ownedMusicListener != null)
            {
                Destroy(_ownedMusicListener.gameObject);
                _ownedMusicListener = null;
            }
        }

        // Local Chinese GOTY EXE: TitleScreen Draw 0x4A3280, Update 0x4A3940.
        // This executable differs from the reference C# port (529 rather than 523; trigger .54 rather than .52).
        /// <summary>Stores original loading active,  original loading first draw,  original startup warmup ready,  original show loading for capture.</summary>
        internal bool _originalLoadingActive, _originalLoadingFirstDraw, _originalStartupWarmupReady, _originalShowLoadingForCapture;
        /// <summary>Stores original title ticks,  original title screen ticks.</summary>
        internal int _originalTitleTicks, _originalTitleScreenTicks = -1;
        /// <summary>Stores original title remainder.</summary>
        private double _originalTitleRemainder;
        /// <summary>Stores original load width,  original load velocity,  original load start progress,  original previous progress.</summary>
        internal float _originalLoadWidth, _originalLoadVelocity = 0.2f, _originalLoadStartProgress, _originalPreviousProgress;
        /// <summary>Stores original warmup tasks,  original warmup completed.</summary>
        private int _originalWarmupTasks, _originalWarmupCompleted;
        /// <summary>Stores original load triggers.</summary>
        internal readonly float[] _originalLoadTriggers =
        {
            0.11f,
            0.32f,
            0.54f,
            0.72f,
            0.906f
        };
        /// <summary>Stores original load sprout ages.</summary>
        internal readonly float[] _originalLoadSproutAges =
        {
            -1f,
            -1f,
            -1f,
            -1f,
            -1f
        };
        /// <summary>Stores original title textures.</summary>
        internal readonly Dictionary<string, Texture2D> _originalTitleTextures = new Dictionary<string, Texture2D>();
        /// <summary>Stores original loading font.</summary>
        internal CanvasBitmapFont _originalLoadingFont;
        /// <summary>Stores original sprout info,  original head info.</summary>
        internal global::PlantsVsZombies.Reanimation.AnimationInfo _originalSproutInfo, _originalHeadInfo;
        /// <summary>Gets or sets original loading active.</summary>
        public bool OriginalLoadingActive
        {
            get
            {
                return _originalLoadingActive;
            }
        }

        /// <summary>Gets or sets original loading ready.</summary>
        public bool OriginalLoadingReady
        {
            get
            {
                return _originalStartupWarmupReady && _audioLoadingFinished && _dynamicAudioReady;
            }
        }

        /// <summary>Gets or sets original loading progress.</summary>
        public float OriginalLoadingProgress
        {
            get
            {
                var warm = _originalStartupWarmupReady ? 1f : _originalWarmupCompleted / (float)Math.Max(1, _originalWarmupTasks);
                return Mathf.Clamp01((warm + DynamicAudioProgress + (_audioLoadingFinished ? 1f : 0f)) / 3f);
            }
        }

        /// <summary>Performs initialize original loading.</summary>
        private void InitializeOriginalLoading()
        {
            _originalLoadingActive = true;
            _originalLoadingFirstDraw = false;
            _originalShowLoadingForCapture = Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-show-loading") >= 0;
            _originalTitleTicks = 0;
            _originalTitleScreenTicks = -1;
            _originalTitleRemainder = 0;
            _originalLoadWidth = 0;
            _originalLoadVelocity = 0.2f;
            _originalLoadStartProgress = _originalPreviousProgress = 0;
            for (var i = 0; i < _originalLoadSproutAges.Length; i++)
            {
                _originalLoadSproutAges[i] = -1;
            }

            foreach (string key in new[]
            {
                "title",
                "popcap",
                "logo",
                "grass",
                "dirt",
                "sodroll"
            }

            )
            {
                var texture = Resources.Load<Texture2D>("PvZLoading/" + key);
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    _originalTitleTextures[key] = texture;
                }
                else
                {
                    Debug.LogWarning("Original title texture is missing: " + key);
                }
            }

            _originalLoadingFont = LoadBitmapFont("BrianneTod16");
            StartCoroutine(PrewarmOriginalStartup());
        }

        /// <summary>Performs prewarm original startup.</summary>
        /// <returns>The resulting prewarm original startup.</returns>
        private IEnumerator PrewarmOriginalStartup()
        {
            // Warm the real selector/first board assets and parsed original animation tracks, yielding between jobs.
            var images = new List<string>();
            var imageRoot = Path.Combine(_root, "images");
            if (Directory.Exists(imageRoot))
            {
                foreach (string path in Directory.GetFiles(imageRoot))
                {
                    var name = Path.GetFileName(path);
                    if (name.StartsWith("SelectorScreen", StringComparison.OrdinalIgnoreCase) || name.StartsWith("SeedChooser", StringComparison.OrdinalIgnoreCase) || name.StartsWith("SeedBank", StringComparison.OrdinalIgnoreCase) || name.StartsWith("background", StringComparison.OrdinalIgnoreCase))
                    {
                        var extension = Path.GetExtension(name);
                        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
                        {
                            images.Add(name);
                        }
                    }
                }
            }

            string[] tracks =
            {
                "LoadBar_sprout",
                "LoadBar_Zombiehead",
                "Peashooter",
                "SunFlower",
                "Wallnut",
                "PotatoMine",
                "SnowPea",
                "Repeater",
                "Zombie",
                "LawnMower"
            };
            _originalWarmupTasks = images.Count + tracks.Length;
            foreach (string image in images)
            {
                Texture(image);
                _originalWarmupCompleted++;
                yield return null;
            }

            foreach (string track in tracks)
            {
                global::PlantsVsZombies.Reanimation.AnimationInfo info;
                if (_animations.TryGetAnimationInfo(track, null, out info))
                {
                    if (track == "LoadBar_sprout")
                    {
                        _originalSproutInfo = info;
                    }
                    else if (track == "LoadBar_Zombiehead")
                    {
                        _originalHeadInfo = info;
                    }
                }
                else if (track == "LoadBar_sprout" || track == "LoadBar_Zombiehead")
                {
                    Debug.LogError("Missing original startup animation: " + track + ": " + _animations.LastError);
                }

                _originalWarmupCompleted++;
                yield return null;
            }

            _originalStartupWarmupReady = true;
        }

        /// <summary>Performs update original loading.</summary>
        /// <param name = "dt">The dt.</param>
        /// <returns>The resulting update original loading.</returns>
        private bool UpdateOriginalLoading(float dt)
        {
            if (!_originalLoadingActive)
            {
                return false;
            }

            if (_smokeMode && !_originalShowLoadingForCapture && OriginalLoadingReady)
            {
                CompleteOriginalLoading(false);
                return false;
            }

            if (!_originalLoadingFirstDraw)
            {
                return true;
            }

            _originalTitleRemainder += Math.Max(0f, dt);
            while (_originalTitleRemainder + 0.00000001 >= 0.01)
            {
                _originalTitleRemainder -= 0.01;
                UpdateOriginalTitleTick();
            }

            return true;
        }

        /// <summary>Performs update original title tick.</summary>
        private void UpdateOriginalTitleTick()
        {
            _originalTitleTicks++;
            // Original PopCap phase lasts 400 ticks in this local executable; the title settles over another 100.
            if (_originalTitleTicks <= 400)
            {
                return;
            }

            if (_originalTitleScreenTicks < 0)
            {
                _originalTitleScreenTicks = 100;
                var progress = OriginalLoadingProgress;
                var totalEstimate = progress > 0.000001f ? _originalTitleTicks / progress : 3000f;
                _originalLoadVelocity = 314f / Mathf.Clamp(totalEstimate * (1f - progress), 100f, 3000f);
                _originalLoadStartProgress = Math.Min(progress, 0.9f);
            }

            if (_originalTitleScreenTicks > 0)
            {
                _originalTitleScreenTicks--;
                return;
            }

            var previousWidth = _originalLoadWidth;
            _originalLoadWidth += _originalLoadVelocity;
            _originalLoadWidth = Math.Min(_originalLoadWidth, 314f * (OriginalLoadingReady ? 1f : 0.99f));
            var p = Mathf.Clamp01((OriginalLoadingProgress - _originalLoadStartProgress) / Math.Max(0.00001f, 1f - _originalLoadStartProgress));
            if (p > _originalPreviousProgress + 0.01f || OriginalLoadingReady)
            {
                // Native spring toward real loading progress, with the reference TodCurve EaseIn target.
                var difference = p * p * 314f - _originalLoadWidth;
                var acceleration = OriginalLoadingReady ? 0.0001f : Mathf.Lerp(0.0001f, 0.00001f, p);
                _originalLoadVelocity = Mathf.Clamp(_originalLoadVelocity + difference * Math.Abs(difference) * acceleration, Mathf.Lerp(0.2f, 0.01f, p), 1f);
                _originalPreviousProgress = p;
            }

            for (var i = 0; i < _originalLoadTriggers.Length; i++)
            {
                if (_originalLoadSproutAges[i] >= 0f)
                {
                    _originalLoadSproutAges[i] += 0.01f;
                }

                var at = _originalLoadTriggers[i] * 314f;
                if (previousWidth < at && _originalLoadWidth >= at && _originalLoadSproutAges[i] < 0f)
                {
                    _originalLoadSproutAges[i] = 0f;
                    PlayPresentationSound("loadingbar_flower");
                    if (i == 4)
                    {
                        PlayPresentationSound("loadingbar_zombie");
                    }
                }
            }
        }

        /// <summary>Performs original title curve.</summary>
        /// <param name = "start">The start.</param>
        /// <param name = "end">The end.</param>
        /// <param name = "value">The value.</param>
        /// <param name = "from">The from.</param>
        /// <param name = "to">The to.</param>
        /// <param name = "bounce">The bounce.</param>
        /// <returns>The resulting original title curve.</returns>
        internal static float OriginalTitleCurve(int start, int end, int value, float from, float to, bool bounce)
        {
            var t = Mathf.Clamp01((value - start) / (float)(end - start));
            var n = bounce ? 1f - Math.Abs(1f - 2f * t) : t * t;
            var result = from + (to - from) * n;
            return (int)(result + (result > 0f ? .5f : -.5f)); // PC TodAnimateCurve integer rounding.
        }

        /// <summary>Performs complete original loading.</summary>
        /// <param name = "clicked">The clicked.</param>
        private void CompleteOriginalLoading(bool clicked)
        {
            if (!OriginalLoadingReady)
            {
                return;
            }

            _originalLoadingActive = false;
            if (clicked)
            {
                PlayPresentationSound("buttonclick");
            }
        }

        /// <summary>Performs dispose original loading.</summary>
        private void DisposeOriginalLoading()
        {
            if (_originalLoadingFont != null)
            {
                _originalLoadingFont.Dispose();
                _originalLoadingFont = null;
            }

            _originalTitleTextures.Clear();
        }

        /// <summary>Stores mode session.</summary>
        internal ModeSession _modeSession;
        /// <summary>Stores mode profile id.</summary>
        private Guid _modeProfileId;
        /// <summary>Stores mode first clear,  mode reward claimed,  mode choosing imitater.</summary>
        internal bool _modeFirstClear, _modeRewardClaimed, _modeChoosingImitater;
        /// <summary>Stores mode saved stage.</summary>
        private int _modeSavedStage = -1;
        /// <summary>Stores mode save error.</summary>
        internal string _modeSaveError;
        /// <summary>Stores mode account snapshot.</summary>
        private PlayerAccount _modeAccountSnapshot;
        /// <summary>Stores mode has finished adventure.</summary>
        private bool _modeHasFinishedAdventure;
        /// <summary>Stores mode adventure next level.</summary>
        private int _modeAdventureNextLevel = 1;
        /// <summary>Stores mode account refresh,  mode next save attempt.</summary>
        private float _modeAccountRefresh, _modeNextSaveAttempt;
        /// <summary>Stores external challenge requested.</summary>
        public Action<int> ExternalChallengeRequested;
        /// <summary>Gets or sets independent mode music cue.</summary>
        private MusicCue? IndependentModeMusicCue
        {
            get
            {
                if (!IsModeBoard)
                {
                    return null;
                }

                if (_game.State == MatchState.Won || _game.State == MatchState.Lost)
                {
                    return MusicCue.None;
                }

                if (_choosingSeeds)
                {
                    return MusicCue.ChooseSeeds;
                }

                var m = _modeSession.Definition.Mechanic;
                if (_modeSession.Definition.Puzzle)
                {
                    return MusicCue.Puzzle;
                }

                if (m == ModeMechanic.Boss)
                {
                    return MusicCue.Boss;
                }

                if (m == ModeMechanic.Columns)
                {
                    return MusicCue.Conveyor;
                }

                if (m == ModeMechanic.Bowling || m == ModeMechanic.Whack || m == ModeMechanic.LittleZombies || m == ModeMechanic.Speed)
                {
                    return MusicCue.Minigame;
                }

                var level = _game.Adventure.Level;
                return level.Terrain == AdventureTerrain.Fog ? MusicCue.Fog : level.IsNight ? MusicCue.Night : level.HasPool ? MusicCue.Pool : level.HasRoof ? MusicCue.Roof : MusicCue.Day;
            }
        }

        /// <summary>Gets or sets is mode board.</summary>
        internal bool IsModeBoard
        {
            get
            {
                return _modeSession != null && ReferenceEquals(_game, _modeSession.Game) && _currentView == ScreenView.Board;
            }
        }

        /// <summary>Stores mode back button.</summary>
        internal static readonly Rect s_modeBackButton = new Rect(620, 570, 174, 26);
        /// <summary>Stores mode next button.</summary>
        internal static readonly Rect s_modeNextButton = new Rect(306, 342, 188, 42);
        /// <summary>Stores mode restart button.</summary>
        internal static readonly Rect s_modeRestartButton = new Rect(306, 394, 188, 40);
        /// <summary>Stores mode exit button.</summary>
        internal static readonly Rect s_modeExitButton = new Rect(306, 444, 188, 40);
        /// <summary>Performs initialize modes.</summary>
        private void InitializeModes()
        {
            _modeView.LaunchRequested = StartIndependentMode;
            _modeView.ProgressLookup = ReadModeRecord;
            _modeView.LockReasonLookup = ReadModeLockReason;
            _modeView.CanLaunch = id =>
            {
                ModeDefinition d;
                return ModeCatalog.TryGet(id, out d) ? ReadModeLockReason(id) == null : ExternalChallengeRequested != null && (id == 43 || id == 50);
            };
        }

        /// <summary>Performs read mode record.</summary>
        /// <param name = "modeId">The mode id.</param>
        /// <returns>The resulting read mode record.</returns>
        private int ReadModeRecord(int modeId)
        {
            if (_profiles == null || !_profiles.SelectedId.HasValue || !EnsurePlayerAccount())
            {
                return 0;
            }

            if (_modeAccountSnapshot == null || _modeAccountSnapshot.ProfileId != _profiles.SelectedId.Value || Time.unscaledTime >= _modeAccountRefresh)
            {
                _modeAccountSnapshot = GetSelectedAccount();
                if (EnsureAdventureStore())
                {
                    var progress = _adventureStore.Get(_profiles.SelectedId.Value);
                    _modeHasFinishedAdventure = progress.CompletedRuns > 0;
                    _modeAdventureNextLevel = progress.NextLevel;
                }

                _modeAccountRefresh = Time.unscaledTime + 1f;
            }

            return _modeAccountSnapshot == null ? 0 : _modeAccountSnapshot.ModeRecord(modeId - 1);
        }

        /// <summary>Performs read mode lock reason.</summary>
        /// <param name = "originalModeId">The original mode id.</param>
        /// <returns>The resulting read mode lock reason.</returns>
        private string ReadModeLockReason(int originalModeId)
        {
            if (_profiles == null || !_profiles.SelectedId.HasValue)
            {
                return "请先选择用户。";
            }

            if (!EnsurePlayerAccount() || !EnsureAdventureStore())
            {
                return "无法读取账户或冒险进度。";
            }

            ReadModeRecord(originalModeId);
            var giftAccess = originalModeId <= 15 ? _modeAccountSnapshot.SurvivalUnlocked : originalModeId <= 35 ? _modeAccountSnapshot.MinigamesUnlocked : _modeAccountSnapshot.PuzzleUnlocked;
            if (!giftAccess && !ModeUnlockRules.CategoryUnlocked(originalModeId, _modeAdventureNextLevel, _modeHasFinishedAdventure))
            {
                return originalModeId <= 15 ? "通关冒险模式后解锁生存模式。" : originalModeId <= 35 ? "通过冒险模式3-2，领取小游戏礼物后解锁。" : "通过冒险模式4-6，领取解谜模式礼物后解锁。";
            }

            return ModeUnlockRules.LockReason(originalModeId, _modeHasFinishedAdventure, index => _modeAccountSnapshot.ModeRecord(index));
        }

        /// <summary>Performs start independent mode.</summary>
        /// <param name = "originalModeId">The original mode id.</param>
        /// <returns>The resulting start independent mode.</returns>
        private bool StartIndependentMode(int originalModeId)
        {
            ModeDefinition definition;
            if (!ModeCatalog.TryGet(originalModeId, out definition))
            {
                if (ExternalChallengeRequested != null)
                {
                    ExternalChallengeRequested(originalModeId);
                    return true;
                }

                return false;
            }

            if (_profiles == null || !_profiles.SelectedId.HasValue)
            {
                if (_profileDialog != null)
                {
                    _profileDialog.Open();
                }

                return false;
            }

            if (!EnsurePlayerAccount())
            {
                return false;
            }

            var locked = ReadModeLockReason(originalModeId);
            if (locked != null)
            {
                SetUnavailable("挑战尚未解锁", locked);
                return false;
            }

            try
            {
                var account = GetSelectedAccount();
                _modeProfileId = _profiles.SelectedId.Value;
                var record = account.ModeRecord(definition.RecordIndex);
                var progress = _adventureStore.Get(_modeProfileId);
                var basicSeeds = progress.CompletedRuns > 0 ? 40 : Math.Min(40, AdventureLevelSpec.AwardSeedId(progress.NextLevel));
                _modeFirstClear = definition.Survival ? record < (definition.Hard ? 10 : 5) : record == 0;
                _modeSession = new ModeSession(originalModeId, account.SeedAccess(), account.PacketSlots, unchecked(Environment.TickCount ^ (int)DateTime.UtcNow.Ticks), account.PurchaseCount(23) > 0, account.PurchaseCount(22) > 0, basicSeeds);
                var coinProfile = _modeProfileId;
                var coinReceipt = _modeSession.RunId.ToString("N");
                _modeSession.Game.Rules.AdventurePacketSlots = account.PacketSlots;
                _modeSession.Game.Rules.AdventureDisableLoot = definition.Mechanic == ModeMechanic.IZombie;
                _modeSession.Game.Rules.AdventurePottedLoot = progress.CompletedRuns > 0 && account.Plants.Count < 32;
                _modeSession.Game.Rules.AdventureChocolateLoot = account.PurchaseCount(20) > 0 && account.PurchaseCount(26) < 10;
                _modeSession.Game.Rules.AdventureLootCommit = coin =>
                {
                    try
                    {
                        _accountStore.CollectBattleLoot(coinProfile, coinReceipt + ":" + coin.Id, coin.Kind == AdventureCoinKind.Chocolate, coin.GiftPlant ?? PlantKind.Peashooter, UtcSeconds);
                        return true;
                    }
                    catch (IOException e)
                    {
                        _modeSaveError = e.Message;
                        return false;
                    }
                    catch (InvalidOperationException e)
                    {
                        _modeSaveError = e.Message;
                        return false;
                    }
                };
                _modeSession.Game.Rules.AdventureCoinCommit = total =>
                {
                    try
                    {
                        _accountStore.CreditBattleCoins(coinProfile, coinReceipt, total);
                        _modeAccountSnapshot = null;
                        _modeSaveError = null;
                        return true;
                    }
                    catch (IOException error)
                    {
                        _modeSaveError = "金币尚未存入，请再次拾取：" + error.Message;
                        return false;
                    }
                    catch (UnauthorizedAccessException error)
                    {
                        _modeSaveError = "金币存档无法写入：" + error.Message;
                        return false;
                    }
                };
                _modeRewardClaimed = false;
                _modeNextSaveAttempt = 0;
                _modeSavedStage = -1;
                _modeSaveError = null;
                _modeChoosingImitater = false;
                _modeSession.Game.ApplyModePurchases(account.PurchaseCount(29) > 0, account.PurchaseCount(24) > 0 ? (Func<bool>)(() =>
                {
                    try
                    {
                        if (_accountStore.Get(coinProfile).PurchaseCount(24) <= 0)
                        {
                            return false;
                        }

                        _accountStore.ConsumeRake(coinProfile);
                        _modeAccountSnapshot = null;
                        return true;
                    }
                    catch (IOException error)
                    {
                        _modeSaveError = "草耙尚未扣除，本局未放置：" + error.Message;
                        return false;
                    }
                    catch (UnauthorizedAccessException error)
                    {
                        _modeSaveError = "草耙库存无法保存，本局未放置：" + error.Message;
                        return false;
                    }
                }) : null);
                AttachAdventureGame(_modeSession.Game);
                _deck.Clear();
                _deck.AddRange(_game.Adventure.SelectedSeeds);
                AdventureFeedback(_modeSession.Advice, 8f);
                _choosingSeeds = _modeSession.Phase == ModePhase.ChoosingSeeds;
                _pendingSeeds = new AdventureSeedChooserState(_game.Adventure.SelectedSeeds);
                _currentView = ScreenView.Board;
                return true;
            }
            catch (Exception error)
            {
                SetUnavailable("无法开始独立模式", error.Message);
                return false;
            }
        }

        /// <summary>Performs restart mode if active.</summary>
        /// <returns>The resulting restart mode if active.</returns>
        private bool RestartModeIfActive()
        {
            if (!IsModeBoard)
            {
                return false;
            }

            if (_modeSession.Phase == ModePhase.Won && !_modeRewardClaimed && !SaveModeRecord(true))
            {
                return true;
            }

            StartIndependentMode(_modeSession.Definition.Id);
            return true;
        }

        /// <summary>Performs tick modes.</summary>
        /// <param name = "dt">The dt.</param>
        /// <returns>The resulting tick modes.</returns>
        private bool TickModes(float dt)
        {
            if (!IsModeBoard)
            {
                return false;
            }

            _modeSession.Update(dt);
            if ((_modeSession.Phase == ModePhase.StageCleared || _modeSession.Phase == ModePhase.Lost) && _modeSavedStage != _modeSession.ClearedStages && Time.unscaledTime >= _modeNextSaveAttempt)
            {
                SaveModeRecord(false);
            }

            return true;
        }

        /// <summary>Performs save mode record.</summary>
        /// <param name = "claimReward">The claim reward.</param>
        /// <returns>The resulting save mode record.</returns>
        private bool SaveModeRecord(bool claimReward)
        {
            if (_modeSession == null || _accountStore == null)
            {
                return false;
            }

            try
            {
                if (claimReward && !_modeSession.Game.BankAdventureCoins())
                {
                    return false;
                }

                var s = _modeSession;
                var score = s.Definition.Survival ? s.FlagsCompleted : s.Definition.Endless ? s.ClearedStages : s.Phase == ModePhase.Won ? 1 : 0;
                if (claimReward && s.Phase == ModePhase.Won && !s.Definition.Endless)
                {
                    _accountStore.RecordModeResult(_modeProfileId, s.Definition.RecordIndex, score, s.RunId.ToString("N"), _modeFirstClear ? 100 : 25);
                    _modeRewardClaimed = true;
                }
                else
                {
                    _accountStore.RecordModeResult(_modeProfileId, s.Definition.RecordIndex, score, s.RunId.ToString("N") + ":score:" + s.ClearedStages, 0);
                }

                _modeSavedStage = s.ClearedStages;
                _modeSaveError = null;
                _modeAccountSnapshot = null;
                return true;
            }
            catch (Exception error)
            {
                _modeSaveError = "成绩/奖励保存失败：" + error.Message;
                _modeNextSaveAttempt = Time.unscaledTime + 2f;
                return false;
            }
        }

        /// <summary>Performs return to mode directory.</summary>
        private void ReturnToModeDirectory()
        {
            if (_modeSession != null && (_modeSession.Finished || _modeSession.HasNextStage))
            {
                if (!SaveModeRecord(_modeSession.Phase == ModePhase.Won && !_modeRewardClaimed))
                {
                    return;
                }
            }

            _currentView = ScreenView.Modes;
            var id = _modeSession == null ? 16 : _modeSession.Definition.Id;
            _modeView.Open(id <= 15 ? 0 : id >= 51 ? 3 : 1);
            _selected = -1;
            _selectedAdventurePacket = -1;
            _shovel = false;
            _choosingSeeds = false;
        }

        /// <summary>Performs try handle mode click.</summary>
        /// <param name = "point">The point.</param>
        /// <returns>The resulting try handle mode click.</returns>
        private bool TryHandleModeClick(Vector2 point)
        {
            if (!IsModeBoard)
            {
                return false;
            }

            var s = _modeSession;
            if (s_modeBackButton.Contains(point))
            {
                ReturnToModeDirectory();
                return true;
            }

            if (s.Phase == ModePhase.ChoosingSeeds)
            {
                HandleModeChooser(point);
                return true;
            }

            if (s.HasNextStage || s.Finished)
            {
                if (s_modeNextButton.Contains(point))
                {
                    if (s.HasNextStage)
                    {
                        if (SaveModeRecord(false) && s.Continue())
                        {
                            _animations.ResetNativePlayback();
                            _choosingSeeds = s.Phase == ModePhase.ChoosingSeeds;
                            _pendingSeeds = new AdventureSeedChooserState(_game.Adventure.SelectedSeeds);
                            _deck.Clear();
                            _deck.AddRange(_game.Adventure.SelectedSeeds);
                            _selected = -1;
                            _selectedAdventurePacket = -1;
                            _shovel = false;
                            AdventureFeedback(s.Advice, 6f);
                        }
                    }
                    else if (s.Phase == ModePhase.Won)
                    {
                        if (!_modeRewardClaimed)
                        {
                            SaveModeRecord(true);
                        }
                        else
                        {
                            ReturnToModeDirectory();
                        }
                    }
                    else
                    {
                        RestartModeIfActive();
                    }
                }
                else if (s_modeRestartButton.Contains(point))
                {
                    RestartModeIfActive();
                }
                else if (s_modeExitButton.Contains(point))
                {
                    ReturnToModeDirectory();
                }

                return true;
            }

            if (s_adventureMenuButton.Contains(point))
            {
                OpenOptions();
                return true;
            }

            if (_game.State == MatchState.Paused)
            {
                return true;
            }

            if (s.Phase == ModePhase.Preparing && new Rect(295, 535, 210, 34).Contains(point))
            {
                s.BeginDefense();
                return true;
            }

            for (var i = _game.Adventure.Coins.Count - 1; i >= 0; i--)
            {
                if (AdventureCoinRect(_game.Adventure.Coins[i]).Contains(point))
                {
                    _game.CollectAdventureCoin(_game.Adventure.Coins[i]);
                    return true;
                }
            }

            for (var i = _game.Suns.Count - 1; i >= 0; i--)
            {
                if (ModeSunRect(_game.Suns[i]).Contains(point))
                {
                    _game.CollectSun(_game.Suns[i]);
                    return true;
                }
            }

            var m = s.Definition.Mechanic;
            if (m == ModeMechanic.Aquarium)
            {
                if (new Rect(100, 10, 155, 55).Contains(point))
                {
                    s.BuyFish();
                }
                else if (new Rect(275, 10, 175, 55).Contains(point))
                {
                    s.BuyAquariumTrophy();
                }
                else
                {
                    s.Feed(point.x, point.y);
                }

                return true;
            }

            if (m == ModeMechanic.Slots && new Rect(500, 8, 95, 64).Contains(point))
            {
                s.Spin();
                return true;
            }

            if (m == ModeMechanic.Beghouled || m == ModeMechanic.BeghouledTwist)
            {
                for (var i = 0; i < 5; i++)
                {
                    if (new Rect(80 + i * 98, 8, 92, 60).Contains(point))
                    {
                        _game.BuyModeMatchAction(i);
                        return true;
                    }
                }
            }

            if (m == ModeMechanic.IZombie)
            {
                for (var i = 0; i < s.ZombieCards.Count; i++)
                {
                    if (ModeZombieCardRect(i).Contains(point))
                    {
                        _selected = _selected == i ? -1 : i;
                        return true;
                    }
                }
            }
            else if (s.Definition.UsesPackets)
            {
                if (m == ModeMechanic.RainingSeeds)
                {
                    for (var i = s.RainPackets.Count - 1; i >= 0; i--)
                    {
                        if (ModeRainRect(s.RainPackets[i]).Contains(point))
                        {
                            _selectedAdventurePacket = _selectedAdventurePacket == s.RainPackets[i].PacketId ? -1 : s.RainPackets[i].PacketId;
                            _shovel = false;
                            return true;
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < _game.Adventure.Packets.Count && i < 10; i++)
                    {
                        if (AdventurePacketContains(i, point))
                        {
                            _selectedAdventurePacket = _selectedAdventurePacket == _game.Adventure.Packets[i].Id ? -1 : _game.Adventure.Packets[i].Id;
                            _selected = -1;
                            _shovel = false;
                            _game.CancelAdventureCobCannonAim();
                            return true;
                        }
                    }
                }

                if (m == ModeMechanic.Vases && new Rect(550, 8, 47, 64).Contains(point))
                {
                    _selected = _selected == 0 ? -1 : 0;
                    _selectedAdventurePacket = -1;
                    return true;
                }
            }
            else if (m != ModeMechanic.Beghouled && m != ModeMechanic.BeghouledTwist)
            {
                for (var i = 0; i < _deck.Count; i++)
                {
                    if (ModeSeedRect(i).Contains(point))
                    {
                        var context = _game.AdventurePacketContext(_deck[i]);
                        if (AdventureSeedBankPresentation.CanPickUp(context))
                        {
                            _selected = _selected == i ? -1 : i;
                            PlayPresentationSound("seedlift");
                        }
                        else
                        {
                            RejectSeedClick(context.Active && !context.CanAfford);
                        }
                        _selectedAdventurePacket = -1;
                        _shovel = false;
                        _game.CancelAdventureCobCannonAim();
                        return true;
                    }
                }
            }

            if (m != ModeMechanic.IZombie && m != ModeMechanic.Beghouled && m != ModeMechanic.BeghouledTwist && s_adventureShovelButton.Contains(point))
            {
                _shovel = !_shovel;
                PlayPresentationSound("shovel");
                _selected = -1;
                _selectedAdventurePacket = -1;
                _game.CancelAdventureCobCannonAim();
                return true;
            }

            var col = Mathf.FloorToInt((point.x - AdventureGridX) / CellWidth);
            var row = Mathf.FloorToInt((point.y - AdventureGridY - AdventureSlope(col)) / AdventurePitch);
            if (col < 0 || col > 8 || row < 0 || row >= _game.RowCount)
            {
                return true;
            }

            if (m == ModeMechanic.IZombie)
            {
                if (_selected >= 0 && !s.PlaceZombie(s.ZombieCards[_selected], row, col))
                {
                    var bungee = s.ZombieCards[_selected] == ZombieKind.Bungee;
                    if (bungee ? col >= _game.ModeIZombieColumns : col < _game.ModeIZombieColumns)
                    {
                        AdventureFeedback(bungee ? "[ADVICE_I_ZOMBIE_LEFT_OF_LINE]" : "[ADVICE_I_ZOMBIE_NOT_PASSED_LINE]", 3f);
                    }
                }

                return true;
            }

            if (m == ModeMechanic.Beghouled || m == ModeMechanic.BeghouledTwist)
            {
                s.ClickMatchCell(row, col);
                return true;
            }

            if (_game.Adventure.SelectedCobCannonId.HasValue)
            {
                _game.TryFireAdventureCobCannonAtCell(row, col);
                return true;
            }

            if (_shovel)
            {
                if (_game.TryShovelAdventurePlant(AdventureShovelTarget(point)))
                {
                    _shovel = false;
                    PlayPresentationSound("plant");
                }

                return true;
            }

            if (_selectedAdventurePacket >= 0)
            {
                if (s.UsePacket(_selectedAdventurePacket, row, col))
                {
                    _selectedAdventurePacket = -1;
                }
                else
                {
                    AdventurePlantingFeedback();
                }

                return true;
            }

            if (m == ModeMechanic.Vases && _selected == 0)
            {
                if (_game.PlantModeVaseCherry(row, col))
                {
                    _selected = -1;
                }
                else
                {
                    AdventurePlantingFeedback();
                }

                return true;
            }

            if (_selected >= 0 && _selected < _deck.Count)
            {
                if (s.Plant(_deck[_selected], row, col))
                {
                    _selected = -1;
                }
                else
                {
                    AdventurePlantingFeedback();
                }

                return true;
            }

            if (_game.TryAimAdventureCobCannon(row, col))
            {
                return true;
            }

            if (m == ModeMechanic.Vases)
            {
                s.SmashVase(row, col);
                return true;
            }

            if (m == ModeMechanic.Whack)
            {
                for (var i = _game.Zombies.Count - 1; i >= 0; i--)
                {
                    var z = _game.Zombies[i];
                    if (new Rect(AdventureGridX + z.X * CellWidth - 48, AdventureCellY(z.Row, z.X) - 35, 96, 140).Contains(point))
                    {
                        _game.HammerAdventureZombie(z.Id);
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>Performs handle mode chooser.</summary>
        /// <param name = "point">The point.</param>
        private void HandleModeChooser(Vector2 point)
        {
            var unlocked = _game.Adventure.AvailableSeeds;
            var required = Math.Min(_modeSession.PacketSlots, unlocked.Count);
            _pendingSeeds.LandFlyingSeeds();
            if (_modeChoosingImitater)
            {
                for (var i = 0; i < 40; i++)
                {
                    if (AdventureChooserRect(i).Contains(point) && _game.SetModeImitaterTarget((PlantKind)i))
                    {
                        var home = AdventureChooserRect(AdventureAvailableSeedIndex(PlantKind.Imitater));
                        _pendingSeeds.Choose(PlantKind.Imitater, (int)home.x, (int)home.y, SeedBankCapacity);
                        PlayAdventureRewardSound("tap2.ogg");
                        _modeChoosingImitater = false;
                        return;
                    }
                }

                if (new Rect(300, 533, 200, 34).Contains(point))
                {
                    _modeChoosingImitater = false;
                }

                return;
            }

            if (ReturnSelectedChooserSeed(point, SeedBankCapacity, true))
            {
                return;
            }

            for (var i = 0; i < unlocked.Count; i++)
            {
                if (AdventureChooserRect(i).Contains(point))
                {
                    if (!_pendingSeeds.Contains(unlocked[i]) && _pendingSeeds.Count < required)
                    {
                        if (unlocked[i] == PlantKind.Imitater)
                        {
                            _modeChoosingImitater = true;
                        }
                        else
                        {
                            var home = AdventureChooserRect(i);
                            _pendingSeeds.Choose(unlocked[i], (int)home.x, (int)home.y, SeedBankCapacity);
                            PlayAdventureRewardSound("tap2.ogg");
                        }
                    }

                    return;
                }
            }

            if (new Rect(300, 533, 200, 34).Contains(point) && _pendingSeeds.Count == required)
            {
                var seeds = new List<PlantKind>(_pendingSeeds);

                if (_modeSession.ChooseSeeds(seeds))
                {
                    _choosingSeeds = false;
                    _deck.Clear();
                    _deck.AddRange(seeds);
                }
            }
        }

        /// <summary>Performs mode zombie name.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting mode zombie name.</returns>
        private static string ModeZombieName(ZombieKind kind)
        {
            string[] names =
            {
                "普通僵尸",
                "旗帜僵尸",
                "路障僵尸",
                "撑杆僵尸",
                "铁桶僵尸",
                "读报僵尸",
                "铁门僵尸",
                "橄榄球僵尸",
                "舞王僵尸",
                "伴舞僵尸",
                "泳圈僵尸",
                "潜水僵尸",
                "冰车僵尸",
                "雪橇僵尸",
                "海豚僵尸",
                "小丑僵尸",
                "气球僵尸",
                "矿工僵尸",
                "蹦蹦僵尸",
                "雪人僵尸",
                "蹦极僵尸",
                "扶梯僵尸",
                "投石车僵尸",
                "巨人僵尸",
                "小鬼僵尸"
            };
            return (int)kind >= 0 && (int)kind < names.Length ? names[(int)kind] : kind.ToString();
        }

        /// <summary>Performs mode seed rect.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting mode seed rect.</returns>
        internal Rect ModeSeedRect(int index)
        {
            var origin = AdventureSeedBankOrigin;
            return new Rect(origin.x + AdventureSeedSlotLayout.PositionX(index, SeedBankCapacity), origin.y + 8f, 50f, 70f);
        }

        /// <summary>Performs mode zombie card rect.</summary>
        /// <param name = "index">The index.</param>
        /// <returns>The resulting mode zombie card rect.</returns>
        internal static Rect ModeZombieCardRect(int index)
        {
            return new Rect(80 + index * 57, 8, 54, 65);
        }

        /// <summary>Performs mode rain rect.</summary>
        /// <param name = "packet">The packet.</param>
        /// <returns>The resulting mode rain rect.</returns>
        internal static Rect ModeRainRect(ModeRainPacket packet)
        {
            return new Rect(packet.X - 25, packet.Y - 35, 50, 70);
        }

        /// <summary>Performs mode sun rect.</summary>
        /// <param name = "sun">The sun.</param>
        /// <returns>The resulting mode sun rect.</returns>
        private Rect ModeSunRect(global::PlantsVsZombies.Simulation.Sun sun)
        {
            return AdventureSunRect(sun);
        }

        /// <summary>Stores account store.</summary>
        internal PlayerAccountStore _accountStore;
        /// <summary>Stores display account snapshot.</summary>
        internal PlayerAccount _displayAccountSnapshot;
        /// <summary>Stores account error.</summary>
        private string _accountError;
        /// <summary>Stores shop page.</summary>
        internal int _shopPage;
        /// <summary>Stores selected shop item.</summary>
        internal int _selectedShopItem = -1;
        /// <summary>Stores store return view.</summary>
        private ScreenView _storeReturnView = ScreenView.Selector;
        /// <summary>Stores progression message.</summary>
        internal string _progressionMessage;
        /// <summary>Stores garden kind.</summary>
        internal GardenKind _gardenKind;
        /// <summary>Stores garden tool.</summary>
        internal GardenTool _gardenTool;
        /// <summary>Stores moving garden plant.</summary>
        private Guid? _movingGardenPlant;
        /// <summary>Stores garden update utc.</summary>
        private long _gardenUpdateUtc;
        /// <summary>Stores tree grow started.</summary>
        internal float _treeGrowStarted = -100f;
        /// <summary>Stores original shop pages.</summary>
        internal static readonly int[][] s_originalShopPages =
        {
            new[]
            {
                21,
                22,
                24,
                23,
                0,
                1,
                2,
                3
            },
            new[]
            {
                6,
                5,
                4,
                7,
                8,
                29
            },
            new[]
            {
                10,
                11,
                12,
                13,
                14,
                15,
                16,
                17
            },
            new[]
            {
                18,
                25,
                19,
                20,
                27,
                28
            }
        };
        /// <summary>Gets or sets utc seconds.</summary>
        internal static long UtcSeconds
        {
            get
            {
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        /// <summary>Performs ensure player account.</summary>
        /// <returns>The resulting ensure player account.</returns>
        internal bool EnsurePlayerAccount()
        {
            if (_accountStore != null)
            {
                return _profiles != null && _profiles.SelectedId.HasValue;
            }

            if (_accountError != null)
            {
                return false;
            }

            if (_profiles == null || !_profiles.SelectedId.HasValue)
            {
                if (_profileDialog != null)
                {
                    _profileDialog.Open();
                }

                return false;
            }

            try
            {
                _accountStore = new PlayerAccountStore(Path.Combine(_smokeMode ? Application.temporaryCachePath : Application.persistentDataPath, _smokeMode ? "pvz-account-" + Guid.NewGuid().ToString("N") + ".account" : "replica-player.account"));
                return true;
            }
            catch (Exception e)
            {
                _accountError = e.Message;
                SetUnavailable("金币档案无法读取", e.Message);
                return false;
            }
        }

        /// <summary>Performs get selected account.</summary>
        /// <returns>The resulting get selected account.</returns>
        internal PlayerAccount GetSelectedAccount()
        {
            return EnsurePlayerAccount() ? _accountStore.Get(_profiles.SelectedId.Value) : null;
        }

        /// <summary>Performs progression action.</summary>
        /// <param name = "action">The action.</param>
        internal void ProgressionAction(Action action)
        {
            try
            {
                _progressionMessage = null;
                action();
            }
            catch (Exception e)
            {
                _progressionMessage = e.Message;
            }
        }

        /// <summary>Performs open store.</summary>
        private void OpenStore()
        {
            if (!EnsurePlayerAccount() || !EnsureAdventureStore())
            {
                return;
            }

            var progress = _adventureStore.Get(_profiles.SelectedId.Value);
            if (progress.CompletedRuns == 0 && progress.NextLevel < 25)
            {
                SetUnavailable("疯狂戴夫的商店", "通过冒险模式 3-4，取得车钥匙后即可进入。");
                return;
            }

            _storeReturnView = _currentView == ScreenView.Garden || _currentView == ScreenView.Tree ? _currentView : ScreenView.Selector;
            _currentView = ScreenView.Store;
            _shopPage = 0;
            _selectedShopItem = -1;
            _progressionMessage = null;
        }

        /// <summary>Performs open zen garden.</summary>
        private void OpenZenGarden()
        {
            if (!EnsurePlayerAccount() || !EnsureAdventureStore())
            {
                return;
            }

            var progress = _adventureStore.Get(_profiles.SelectedId.Value);
            if (progress.CompletedRuns == 0 && progress.NextLevel < 45)
            {
                SetUnavailable("禅境花园", "通过冒险模式 5-4，取得浇水壶后即可进入。");
                return;
            }

            _currentView = ScreenView.Garden;
            _gardenKind = GardenKind.Greenhouse;
            _gardenTool = GardenTool.Water;
            _movingGardenPlant = null;
            _progressionMessage = null;
            ProgressionAction(() => _accountStore.UpdateGarden(_profiles.SelectedId.Value, UtcSeconds, true));
        }

        /// <summary>Performs open wisdom tree.</summary>
        private void OpenWisdomTree()
        {
            var p = GetSelectedAccount();
            if (p == null)
            {
                return;
            }

            if (p.PurchaseCount(27) == 0)
            {
                SetUnavailable("智慧树", "请先在商店购买智慧树。");
                return;
            }

            _currentView = ScreenView.Tree;
            _treeGrowStarted = -100f;
            _progressionMessage = null;
        }

        /// <summary>Performs update progression music context.</summary>
        private void UpdateProgressionMusicContext()
        {
            DynamicMusicOverride = _currentView == ScreenView.Garden || _currentView == ScreenView.Tree || _currentView == ScreenView.Store && (_storeReturnView == ScreenView.Garden || _storeReturnView == ScreenView.Tree) ? (global::PlantsVsZombies.Audio.MusicCue? )global::PlantsVsZombies.Audio.MusicCue.ZenGarden : IndependentModeMusicCue;
        }

        /// <summary>Performs update progression.</summary>
        private void UpdateProgression()
        {
            if (_currentView == ScreenView.Garden && _accountStore != null && UtcSeconds != _gardenUpdateUtc)
            {
                _gardenUpdateUtc = UtcSeconds;
                ProgressionAction(() => _accountStore.UpdateGarden(_profiles.SelectedId.Value, _gardenUpdateUtc));
            }
        }

        /// <summary>Performs shop item rect.</summary>
        /// <param name = "slot">The slot.</param>
        /// <returns>The resulting shop item rect.</returns>
        internal static Rect ShopItemRect(int slot)
        {
            return slot < 4 ? new Rect(422 + 74 * slot, 206, 70, 100) : new Rect(372 + 74 * (slot - 4), 310, 70, 100);
        }

        /// <summary>Performs garden plant rect.</summary>
        /// <param name = "g">The g.</param>
        /// <returns>The resulting garden plant rect.</returns>
        internal Rect GardenPlantRect(GardenPlant g)
        {
            var grid = OriginalGardenLayout.Grids[(int)g.Garden];
            var slot = Mathf.Clamp(g.Slot, 0, grid.Length - 1);
            return new Rect(grid[slot][0], grid[slot][1], 80, 85);
        }

        /// <summary>Performs handle progression click.</summary>
        /// <param name = "point">The point.</param>
        /// <returns>The resulting handle progression click.</returns>
        private bool HandleProgressionClick(Vector2 point)
        {
            if (_currentView == ScreenView.Store)
            {
                if (_selectedShopItem >= 0)
                {
                    if (new Rect(245, 346, 135, 46).Contains(point))
                    {
                        var progress = _adventureStore.Get(_profiles.SelectedId.Value);
                        ProgressionAction(() =>
                        {
                            _accountStore.Purchase(_profiles.SelectedId.Value, _selectedShopItem, progress.NextLevel, progress.CompletedRuns, UtcSeconds);
                            _selectedShopItem = -1;
                            _displayAccountSnapshot = null;
                        });
                    }
                    else if (new Rect(420, 346, 135, 46).Contains(point))
                    {
                        _selectedShopItem = -1;
                        _progressionMessage = null;
                    }

                    return true;
                }

                if (new Rect(366, 512, 138, 80).Contains(point))
                {
                    _currentView = _storeReturnView;
                    return true;
                }

                if (new Rect(252, 402, 96, 75).Contains(point))
                {
                    _shopPage = (_shopPage + 3) % 4;
                    _selectedShopItem = -1;
                    return true;
                }

                if (new Rect(596, 402, 136, 83).Contains(point))
                {
                    _shopPage = (_shopPage + 1) % 4;
                    _selectedShopItem = -1;
                    return true;
                }

                var page = s_originalShopPages[_shopPage];
                for (var i = 0; i < page.Length; i++)
                {
                    if (ShopItemRect(i).Contains(point))
                    {
                        var item = ShopCatalog.Get(page[i]);
                        var progress = _adventureStore.Get(_profiles.SelectedId.Value);
                        var account = GetSelectedAccount();
                        if (ShopCatalog.IsUnlocked(item, progress.NextLevel, progress.CompletedRuns) && !ShopCatalog.SoldOut(item, account, UtcSeconds))
                        {
                            _selectedShopItem = page[i];
                        }

                        _progressionMessage = null;
                        return true;
                    }
                }

                return true;
            }

            if (_currentView == ScreenView.Tree)
            {
                if (new Rect(655, 8, 138, 40).Contains(point))
                {
                    _currentView = ScreenView.Garden;
                }
                else if (new Rect(655, 548, 138, 42).Contains(point))
                {
                    OpenStore();
                }
                else if (new Rect(24, 505, 253, 80).Contains(point))
                {
                    if (Time.unscaledTime - _treeGrowStarted < 1.2f)
                    {
                        return true;
                    }

                    ProgressionAction(() => _accountStore.FeedWisdomTree(_profiles.SelectedId.Value));
                    _treeGrowStarted = Time.unscaledTime;
                }

                return true;
            }

            if (_currentView == ScreenView.Import)
            {
                return true;
            }

            if (_currentView != ScreenView.Garden)
            {
                return false;
            }

            if (new Rect(674, 6, 120, 36).Contains(point))
            {
                _currentView = ScreenView.Selector;
                return true;
            }

            if (new Rect(654, 540, 136, 42).Contains(point))
            {
                OpenStore();
                return true;
            }

            if (new Rect(175, 540, 120, 42).Contains(point))
            {
                OpenWisdomTree();
                return true;
            }

            if (new Rect(8, 540, 155, 42).Contains(point))
            {
                var p = GetSelectedAccount();
                var next = (GardenKind)(((int)_gardenKind + 1) % 3);
                if (next == GardenKind.Mushroom && p.PurchaseCount(18) == 0)
                {
                    next = GardenKind.Aquarium;
                }

                if (next == GardenKind.Aquarium && p.PurchaseCount(25) == 0)
                {
                    next = GardenKind.Greenhouse;
                }

                if (_movingGardenPlant.HasValue)
                {
                    var id = _movingGardenPlant.Value;
                    ProgressionAction(() => _accountStore.TendGarden(_profiles.SelectedId.Value, id, GardenTool.Move, UtcSeconds, next));
                    _movingGardenPlant = null;
                }

                _gardenKind = next;
                return true;
            }

            if (new Rect(490, 490, 105, 82).Contains(point) && GetSelectedAccount().PurchaseCount(20) > 0)
            {
                ProgressionAction(() => _accountStore.WakeSnail(_profiles.SelectedId.Value, UtcSeconds));
                return true;
            }

            for (var i = 0; i < 8; i++)
            {
                if (new Rect(8 + i * 78, 4, 73, 57).Contains(point))
                {
                    _gardenTool = (GardenTool)i;
                    return true;
                }
            }

            foreach (var g in GetSelectedAccount().Plants)
            {
                if (g.Garden == _gardenKind && GardenPlantRect(g).Contains(point))
                {
                    if (_gardenTool == GardenTool.Move)
                    {
                        _movingGardenPlant = g.Id;
                        _progressionMessage = "选择下一个花园，将植物放过去。";
                    }
                    else
                    {
                        ProgressionAction(() => _accountStore.TendGarden(_profiles.SelectedId.Value, g.Id, _gardenTool, UtcSeconds));
                    }

                    return true;
                }
            }

            return true;
        }

        /// <summary>Stores original tooltip.</summary>
        internal OriginalTooltip _originalTooltip;
        /// <summary>Stores tooltip inspection.</summary>
        internal OriginalTooltipRequest _tooltipInspection;
        // Called at the top of the host PlaySound; keeps the old classic-game event behavior.
        /// <summary>Performs suppress legacy zombie hit sound.</summary>
        /// <param name = "evt">The evt.</param>
        /// <returns>The resulting suppress legacy zombie hit sound.</returns>
        private bool SuppressLegacyZombieHitSound(GameEvent evt)
        {
            if (_game == null || !_game.IsAdventure)
            {
                return false;
            }

            if (evt == GameEvent.Shoot && _game.Presentation._iceShootSuppressions > 0)
            {
                _game.Presentation._iceShootSuppressions--;
                return true;
            }

            return evt == GameEvent.ZombieHit;
        }

        /// <summary>Stores zombie part audio game.</summary>
        private GameSimulation _zombiePartAudioGame;
        /// <summary>Stores zombie part audio serial,  zombie sound read index.</summary>
        private int _zombiePartAudioSerial, _zombieSoundReadIndex;
        /// <summary>Stores zombie part audio loading,  zombie foley ready.</summary>
        private bool _zombiePartAudioLoading, _zombieFoleyReady;
        /// <summary>Stores zombie foley random.</summary>
        private readonly System.Random _zombieFoleyRandom = new global::PlantsVsZombies.Randomness.OriginalRandomAdapter(12345U);
        /// <summary>Stores zombie foley last tick.</summary>
        private readonly Dictionary<int, int> _zombieFoleyLastTick = new Dictionary<int, int>(AdventureZombieFoleyData.Definitions.Count);

        /// <summary>Performs update adventure zombie part audio.</summary>
        internal void UpdateAdventureZombiePartAudio()
        {
            if (!ReferenceEquals(_zombiePartAudioGame, _game))
            {
                _zombiePartAudioGame = _game;
                _zombiePartAudioSerial = 0;
                _zombieSoundReadIndex = 0;
                _zombieFoleyLastTick.Clear();
            }

            if (!_zombiePartAudioLoading)
            {
                _zombiePartAudioLoading = true;
                StartCoroutine(LoadAdventureZombiePartAudio());
            }

            if (!_zombieFoleyReady || _game == null || _audioSource == null)
            {
                return;
            }

            while (_zombieSoundReadIndex < _game.Presentation.ZombieSounds.Count)
            {
                var evt = _game.Presentation.ZombieSounds[_zombieSoundReadIndex++];
                if (evt.Foley == 25)
                {
                    _zombiePartAudioSerial++;
                }

                PlayAdventureZombieFoley(evt);
            }
        }

        /// <summary>Performs play adventure zombie foley.</summary>
        /// <param name = "evt">The evt.</param>
        private void PlayAdventureZombieFoley(AdventureZombieSoundEvent evt)
        {
            if (evt.Foley == -1)
            {
                AudioClip gulp;
                if (_presentationSounds.TryGetValue("gulp", out gulp) && gulp != null)
                {
                    _audioSource.PlayOneShot(gulp);
                }

                return;
            }

            AdventureZombieFoleyDefinition definition;
            if (!AdventureZombieFoleyData.Definitions.TryGetValue(evt.Foley, out definition))
            {
                return;
            }

            // Keep native limits for ambient/chewing Foley; each projectile impact needs its own feedback.
            var projectileImpact = evt.Foley == 1 || evt.Foley == 32 || evt.Foley == 38 || evt.Foley == 43
                || evt.Foley == 46 || evt.Foley == 58 || evt.Foley == 62;
            int previous;
            if (!projectileImpact && _zombieFoleyLastTick.TryGetValue(evt.Foley, out previous) && evt.Tick - previous < 10)
            {
                return;
            }

            if (!_soundVoices.CanPlay(evt.Foley, 8, replaceOldest: projectileImpact))
            {
                return;
            }

            // PC table 0x74B3B8: preserve duplicate entries as variation weights.
            var semitones = (float)_zombieFoleyRandom.NextDouble() * definition.PitchRange;
            var sample = definition.Samples[_zombieFoleyRandom.Next(definition.Samples.Length)];
            AudioClip clip;
            if (!_presentationSounds.TryGetValue(sample, out clip) || clip == null)
            {
                return;
            }

            // PC DSoundInstance::AdjustPitch 0x63E850, including native frequency limits.
            var frequency = Math.Max(100d, Math.Min(100000d, clip.frequency * Math.Pow(1.059463094359295d, semitones)));
            if (_soundVoices.TryPlay(clip, _soundVolume, (float)(frequency / clip.frequency), evt.Foley, 8, replaceOldest: projectileImpact))
            {
                _zombieFoleyLastTick[evt.Foley] = evt.Tick;
            }
        }

        /// <summary>Performs load adventure zombie part audio.</summary>
        /// <returns>The resulting load adventure zombie part audio.</returns>
        private IEnumerator LoadAdventureZombiePartAudio()
        {
            var names = new HashSet<string>
            {
                "gulp"
            };
            foreach (var definition in AdventureZombieFoleyData.Definitions.Values)
            {
                foreach (string name in definition.Samples)
                {
                    names.Add(name);
                }
            }

            foreach (string name in names)
            {
                if (_presentationSounds.ContainsKey(name))
                {
                    continue;
                }

                var path = Path.Combine(_root, "sounds", name + ".ogg");
                if (!File.Exists(path))
                {
                    Debug.LogError("Missing original zombie Foley: " + path);
                    continue;
                }

                using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.OGGVORBIS))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        _presentationSounds[name] = DownloadHandlerAudioClip.GetContent(request);
                    }
                    else
                    {
                        Debug.LogError("Original zombie Foley failed: " + name + ": " + request.error);
                    }
                }
            }

            // Existing DisposeDynamicAudio owns the clips; child audio sources share this component's lifetime.
            _zombieFoleyReady = true;
        }
        /// <summary>Stores original import path.</summary>
        internal string _originalImportPath = "";
        /// <summary>Stores original export path.</summary>
        internal string _originalExportPath = "";
        /// <summary>Stores import candidates.</summary>
        internal IReadOnlyList<OriginalUserSave>? _importCandidates;
        /// <summary>Stores import selection.</summary>
        internal int _importSelection;


        /// <summary>Retains native UI commands until the next game update.</summary>
        private Action? _pendingUiAction;

        /// <summary>Queues a native UI command for the gameplay update.</summary>
        /// <param name="action">The navigation or persistence command requested by the interface.</param>
        internal void QueueUiAction(Action action)
        {
            _pendingUiAction += action;
        }

        /// <summary>Reads the original save candidates requested by the import screen.</summary>
        internal void ReadOriginalSaves()
        {
            ProgressionAction(() =>
            {
                _importCandidates = Directory.Exists(_originalImportPath) ? OriginalSaveFormat.ReadDirectory(_originalImportPath) : Array.AsReadOnly(new[] { OriginalSaveFormat.ReadUser(_originalImportPath) });
                _importSelection = 0;
            });
        }

        /// <summary>Exports the selected account through the original save format.</summary>
        internal void ExportOriginalSave()
        {
            ProgressionAction(() =>
            {
                if (!EnsurePlayerAccount() || !EnsureAdventureStore())
                {
                    return;
                }

                var copy = Path.Combine(Application.persistentDataPath, "original-" + _profiles.SelectedId.Value.ToString("N") + ".dat");
                OriginalSaveFormat.ExportUser(copy, _originalExportPath, GetSelectedAccount(), _adventureStore.Get(_profiles.SelectedId.Value));
                _progressionMessage = "导出完成：" + _originalExportPath;
            });
        }

        /// <summary>Returns from the original save utility to the selector.</summary>
        internal void ReturnFromSaveUtility()
        {
            _currentView = ScreenView.Selector;
        }

        /// <summary>Performs import original user.</summary>
        internal void ImportOriginalUser()
        {
            ProgressionAction(ImportOriginalUserCore);
        }

        /// <summary>Imports the selected original user and its campaign/account state.</summary>
        private void ImportOriginalUserCore()
        {
            if (_importCandidates == null || _importSelection >= _importCandidates.Count || !EnsureAdventureStore() || !EnsurePlayerAccount())
            {
                return;
            }

            var source = _importCandidates[_importSelection];
            var name = source.Name;
            if (name.Length > 26)
            {
                name = name.Substring(0, 26);
            }

            var suffix = 1;
            bool used;
            do
            {
                used = false;
                foreach (var existing in _profiles.Profiles)
                {
                    if (string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        used = true;
                        break;
                    }
                }

                if (used)
                {
                    name = source.Name.Substring(0, Math.Min(24, source.Name.Length)) + "（" + suffix++ + "）";
                }
            }
            while (used);
            var importedId = Guid.NewGuid();
            var copy = Path.Combine(Application.persistentDataPath, "original-" + importedId.ToString("N") + ".dat");
            OriginalSaveFormat.KeepImportedSource(source, copy);
            _accountStore.Import(importedId, source.Coins, source.Purchases, source.ChallengeRecords, source.ChallengeRecords[49], source.Plants, source.MinigamesUnlocked, source.PuzzleUnlocked, source.SurvivalUnlocked);
            _adventureStore.Import(importedId, source.Level, source.FinishedAdventure);
            _profiles.CreateImported(importedId, name);
            _progressionMessage = "已导入用户：" + name;
            _currentView = ScreenView.Selector;
        }
    }
}
