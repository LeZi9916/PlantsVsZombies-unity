using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using PvZReplica.Reanimation;
using PvZReplica.UI;
using PvZReplica.Profiles;
using PvZReplica.Levels;
using PvZReplica.BoardUI;
using PvZReplica.UI.BitmapFonts;

namespace PvZReplica
{
    public sealed partial class PvZGame : MonoBehaviour
    {
        private enum ScreenView { Selector, Board, Modes, Almanac, Help, Store, Garden, Tree, Import }
        private const float Width = 800f;
        private const float Height = 600f;
        private const float GridX = 70f;
        private const float GridY = 80f;
        private const float CellWidth = 80f;
        private const float CellHeight = 100f;

        private readonly List<PlantKind> deck = new List<PlantKind> {
            PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.Wallnut, PlantKind.CherryBomb,
            PlantKind.SnowPea, PlantKind.PotatoMine, PlantKind.Repeater
        };
        private readonly PlantKind[] availablePlants = {
            PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.Wallnut, PlantKind.CherryBomb,
            PlantKind.SnowPea, PlantKind.PotatoMine, PlantKind.Repeater, PlantKind.Jalapeno, PlantKind.TallNut
        };
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<GameEvent, AudioClip> sounds = new Dictionary<GameEvent, AudioClip>();
        private readonly Dictionary<string, float> shootingDurations = new Dictionary<string, float>();
        private readonly Dictionary<ImGuiBitmapFont, Dictionary<string, BitmapTextLayout>> bitmapLayouts =
            new Dictionary<ImGuiBitmapFont, Dictionary<string, BitmapTextLayout>>();
        private GameSimulation game;
        private ReanimationRenderer animations;
        private SelectorView selectorView;
        private OptionsView optionsView;
        private AlmanacView almanacView;
        private ModeSelectionView modeView;
        private HelpView helpView;
        private ProfileCatalog profiles;
        private ProfileDialog profileDialog;
        private ImGuiBitmapFont optionsBitmap;
        private ImGuiBitmapFont modeTitleBitmap;
        private ImGuiBitmapFont modeLabelBitmap;
        private ImGuiBitmapFont sunCounterBitmap;
        private string profileLoadError;
        private ScreenView currentView = ScreenView.Selector;
        private ScreenView almanacReturnView = ScreenView.Selector;
        private bool almanacReturnsToOptions;
        private AudioSource audioSource;
        private AudioSource musicSource;
        private Font font;
        private GUIStyle label;
        private string root;
        private int selected = -1;
        private bool shovel;
        private bool choosingSeeds;
        private HashSet<PlantKind> pendingSeeds;
        private float musicVolume;
        private float soundVolume;
        private string unavailableFeature;
        private string unavailableDetail = "尚未实现";
        private bool smokeMode;
        private Vector2? smokePointer = null; // Explicit Player scenarios only; normal input uses the native GUI event.
        private bool confirmingQuit;
        private bool experimentalLevelOne;
        private bool audioLoadingFinished;
        private bool quitRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PvZGame>() == null)
                new GameObject("PvZ Game").AddComponent<PvZGame>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Application.wantsToQuit += CanQuit;
            string[] arguments = Environment.GetCommandLineArgs();
            smokeMode = Array.IndexOf(arguments, "-pvz-combat-fidelity-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-intro-audio-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-feedback-smoke") >= 0 || Array.IndexOf(arguments, "-pvz-progression-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-ui-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-first-user-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-level-one-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-level-one-logic-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-ui-logic-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-first-user-logic-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-ui-review") >= 0 ||
                Array.IndexOf(arguments, "-pvz-adventure-smoke") >= 0;
            if (smokeMode) Application.runInBackground = true;
            experimentalLevelOne = Array.IndexOf(arguments, "-pvz-level-one") >= 0 ||
                Array.IndexOf(arguments, "-pvz-level-one-smoke") >= 0 ||
                Array.IndexOf(arguments, "-pvz-level-one-logic-smoke") >= 0;
            root = Path.Combine(Application.streamingAssetsPath, "PvZ");
            animations = new ReanimationRenderer(root);
            selectorView = new SelectorView(root);
            optionsView = new OptionsView(root);
            almanacView = new AlmanacView(root, animations);
            helpView = new HelpView(root);
            audioSource = gameObject.AddComponent<AudioSource>();
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("PvZReplica.Music", 0.35f));
            soundVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("PvZReplica.Sound", 0.7f));
            musicSource.volume = musicVolume;
            audioSource.volume = soundVolume;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 16);
            modeView = new ModeSelectionView(root, Texture, font);
            InitializeModes();
            ExternalChallengeRequested = id => { if(id==43)OpenZenGarden(); else if(id==50)OpenWisdomTree(); };
            optionsBitmap = LoadBitmapFont("DwarvenTodcraft18");
            modeTitleBitmap = LoadBitmapFont("HouseofTerror28");
            modeLabelBitmap = LoadBitmapFont("BrianneTod12");
            sunCounterBitmap = LoadBitmapFont("ContinuumBold14");
            optionsView.OriginalRightLabel = DrawOptionsBitmap;
            modeView.OriginalCenteredLabel = DrawModeBitmap;
            InitializeProfiles();
            ResetGame();
            StartCoroutine(LoadSounds());
            InitializeDynamicAudio();
            InitializeOriginalLoading();
        }

        private void ResetGame()
        {
            if (RestartModeIfActive()) return;
            if (game != null && game.IsAdventure && !UseLegacyBoard) { RestartAdventure(); return; }
            if (experimentalLevelOne)
            {
                deck.Clear();
                deck.Add(PlantKind.Peashooter);
            }
            var rules = new GameRules { EnableExperimentalLevelOne = experimentalLevelOne };
            if (experimentalLevelOne) rules.LevelOneOptions.UseNativeSkySunRules = true;
            game = new GameSimulation(rules);
            game.EventRaised = PlaySound;
            selected = -1;
            shovel = false;
            choosingSeeds = false;
        }

        private void Update()
        {
            if (UpdateOriginalLoading(Time.unscaledDeltaTime)) { UpdateDynamicAudio(); return; }
            UpdateProgressionMusicContext();
            UpdateProgression();
            if (Input.GetKeyDown(KeyCode.F9) && currentView != ScreenView.Board && !optionsView.IsOpen && (profileDialog == null || !profileDialog.IsOpen)) { currentView=ScreenView.Import; progressionMessage=null; }
            bool openingBlocksSimulation = UpdateAdventureOpening(Time.deltaTime);
            if (!openingBlocksSimulation && currentView == ScreenView.Board && !choosingSeeds && !optionsView.IsOpen &&
                (profileDialog == null || !profileDialog.IsOpen))
                if (!TickModes(Time.deltaTime)) game.Update(Time.deltaTime);
            UpdateAdventureRewardContinuation();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (optionsView.IsOpen) CloseOptions();
                else if (profileDialog != null && profileDialog.IsOpen) profileDialog.HandleBack();
                else if (confirmingQuit) confirmingQuit = false;
                else if (unavailableFeature != null) unavailableFeature = null;
                else if (currentView == ScreenView.Almanac)
                {
                    almanacView.HandleBack();
                    ReturnFromAlmanacIfClosed();
                }
                else if (currentView == ScreenView.Modes || currentView == ScreenView.Garden || currentView == ScreenView.Import) currentView = ScreenView.Selector;
                else if (currentView == ScreenView.Store) currentView=storeReturnView;
                else if (currentView == ScreenView.Tree) currentView=ScreenView.Garden;
                else if (currentView == ScreenView.Help)
                {
                    helpView.HandleKey(KeyCode.Escape);
                    currentView = ScreenView.Selector;
                }
                else if (currentView == ScreenView.Board)
                {
                    if (choosingSeeds)
                    {
                        if (game.IsAdventure) currentView = ScreenView.Selector;
                        else choosingSeeds = false;
                    }
                    else if (game.State == MatchState.Playing) OpenOptions();
                    else if (game.State == MatchState.Paused) game.TogglePause();
                }
            }
            else if (currentView == ScreenView.Help && !optionsView.IsOpen &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
            {
                helpView.HandleKey(KeyCode.Return);
                currentView = ScreenView.Selector;
            }
            UpdateDynamicAudio();
        }

        private bool CanQuit()
        {
            if (audioLoadingFinished && DynamicAudioReady) return true;
            RequestQuit(0);
            return false;
        }

        internal void RequestQuit(int exitCode)
        {
            if (quitRequested) return;
            quitRequested = true;
            StartCoroutine(QuitAfterAudioLoading(exitCode));
        }

        private IEnumerator QuitAfterAudioLoading(int exitCode)
        {
            // An early quit can race Unity's native audio initialization/decoding.
            // Finish the existing loader before entering the engine shutdown path.
            while (!audioLoadingFinished || !DynamicAudioReady) yield return null;
            yield return null;
            Application.Quit(exitCode);
        }

        private void OnDestroy()
        {
            DisposeDynamicAudio();
            DisposeOriginalLoading();
            Application.wantsToQuit -= CanQuit;
            if (selectorView != null) selectorView.Dispose();
            if (optionsView != null) optionsView.Dispose();
            if (almanacView != null) almanacView.Dispose();
            if (profileDialog != null) profileDialog.Dispose();
            if (helpView != null) helpView.Dispose();
            if (optionsBitmap != null) optionsBitmap.Dispose();
            if (modeTitleBitmap != null) modeTitleBitmap.Dispose();
            if (modeLabelBitmap != null) modeLabelBitmap.Dispose();
            if (sunCounterBitmap != null) sunCounterBitmap.Dispose();
            if (animations != null) animations.Dispose();
            OriginalUiWidgets.ReleaseFonts();
            if (originalTooltip != null) originalTooltip.Dispose();
            foreach (Texture2D texture in textures.Values) if (texture != null) Destroy(texture);
            foreach (AudioClip clip in sounds.Values) if (clip != null) Destroy(clip);
            if (font != null) Destroy(font);
        }

        private IEnumerator LoadSounds()
        {
            try { yield return LoadSoundsCore(); }
            finally { audioLoadingFinished = true; }
        }

        private IEnumerator LoadSoundsCore()
        {
            Dictionary<GameEvent, string> files = new Dictionary<GameEvent, string> {
                { GameEvent.Planted, "plant.ogg" }, { GameEvent.Shoot, "throw.ogg" },
                { GameEvent.SunPickupStarted, "points.ogg" }, { GameEvent.ZombieHit, "splat.ogg" },
                { GameEvent.Mower, "lawnmower.ogg" }, { GameEvent.Explosion, "cherrybomb.ogg" },
                { GameEvent.MineExplosion, "potato_mine.ogg" },
                { GameEvent.JalapenoExplosion, "jalapeno.ogg" },
                { GameEvent.Won, "winmusic.ogg" }, { GameEvent.Lost, "losemusic.ogg" }
            };
            foreach (KeyValuePair<GameEvent, string> item in files)
            {
                string path = Path.Combine(root, "sounds", item.Value);
                if (!File.Exists(path)) continue;
                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.OGGVORBIS))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                        sounds[item.Key] = DownloadHandlerAudioClip.GetContent(request);
                    else Debug.LogWarning("Could not load sound " + item.Value + ": " + request.error);
                }
            }
            yield return LoadAdventureRewardSounds();
        }

        private void PlaySound(GameEvent gameEvent)
        {
            if (SuppressLegacyZombieHitSound(gameEvent)) return;
            if (PlayAdventureCherryBombSounds(gameEvent)) return;
            AudioClip clip;
            if (sounds.TryGetValue(gameEvent, out clip) && clip != null)
                audioSource.PlayOneShot(clip, gameEvent == GameEvent.ZombieHit ? 0.35f : 0.7f);
        }

        private void OnGUI()
        {
            if (DrawOriginalLoading()) return;
            float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            float offsetX = (Screen.width - Width * scale) * 0.5f;
            float offsetY = (Screen.height - Height * scale) * 0.5f;
            Event current = Event.current;
            Vector2 mouse = new Vector2((current.mousePosition.x - offsetX) / scale,
                (current.mousePosition.y - offsetY) / scale);
            if (smokeMode && smokePointer.HasValue) mouse = smokePointer.Value;
            adventurePointer = mouse;
            selectorView.HoveredAction = selectorView.HitTest(mouse);
            if (profileDialog != null && profileDialog.IsOpen)
            {
                if (current.type == EventType.MouseDown && current.button == 0 && profileDialog.HandleClick(mouse))
                    current.Use();
            }
            else if (optionsView.IsOpen)
            {
                EventType eventType = current.type;
                bool pointer = eventType == EventType.MouseMove ||
                    ((eventType == EventType.MouseDown || eventType == EventType.MouseDrag ||
                      eventType == EventType.MouseUp) && current.button == 0);
                if (pointer && optionsView.HandlePointer(mouse, eventType))
                {
                    current.Use();
                    if (!optionsView.IsOpen) ApplyOptions();
                }
            }
            else if (currentView == ScreenView.Almanac)
            {
                EventType eventType = current.type;
                if (eventType == EventType.ScrollWheel)
                {
                    if (almanacView.HandleScroll(mouse, current.delta.y)) current.Use();
                }
                else
                {
                    bool pointer = eventType == EventType.MouseMove ||
                        ((eventType == EventType.MouseDown || eventType == EventType.MouseDrag ||
                          eventType == EventType.MouseUp) && current.button == 0);
                    if (pointer && HandleAlmanacPointer(mouse, eventType)) current.Use();
                }
            }
            else if (currentView == ScreenView.Help)
            {
                EventType eventType = current.type;
                bool pointer = eventType == EventType.MouseMove ||
                    ((eventType == EventType.MouseDown || eventType == EventType.MouseDrag ||
                      eventType == EventType.MouseUp) && current.button == 0);
                if (pointer && HandleHelpPointer(mouse, eventType)) current.Use();
            }
            else if (currentView != ScreenView.Import && current.type == EventType.MouseDown && current.button == 0)
            {
                HandleClick(mouse);
                current.Use();
            }
            else if (current.type == EventType.MouseDown && current.button == 1 &&
                currentView == ScreenView.Board && !choosingSeeds && unavailableFeature == null && !confirmingQuit)
            {
                CancelBoardSelection();
                current.Use();
            }

            Matrix4x4 previous = GUI.matrix;
            try
            {
                GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0), Quaternion.identity,
                    new Vector3(scale, scale, 1f));
                switch (currentView)
                {
                    case ScreenView.Selector: selectorView.Draw(Time.unscaledTime); DrawProfileName(); DrawAdventureProgress(); DrawAccountStatus(); break;
                    case ScreenView.Modes: modeView.Draw(); break;
                    case ScreenView.Store: DrawStore(); break;
                    case ScreenView.Garden: DrawGarden(); break;
                    case ScreenView.Tree: DrawWisdomTree(); break;
                    case ScreenView.Import: DrawOriginalSaveUtility(); break;
                    case ScreenView.Almanac: almanacView.Draw(Time.unscaledTime); break;
                    case ScreenView.Help: helpView.Draw(); break;
                    case ScreenView.Board: DrawBoard(); DrawInterface(); DrawAdventurePlantCursor(); break;
                }
                DrawOriginalHoverTips();
                if (unavailableFeature != null) DrawUnavailable();
                if (confirmingQuit) DrawQuitConfirmation();
                if (optionsView.IsOpen) optionsView.Draw();
                if (profileDialog != null && profileDialog.IsOpen) profileDialog.Draw();
            }
            finally { GUI.matrix = previous; }
        }

        private void HandleClick(Vector2 point)
        {
            if (confirmingQuit)
            {
                if (new Rect(245, 346, 135, 46).Contains(point)) RequestQuit(0);
                else if (new Rect(420, 346, 135, 46).Contains(point)) confirmingQuit = false;
                return;
            }
            if (unavailableFeature != null)
            {
                if (new Rect(320, 330, 160, 42).Contains(point)) unavailableFeature = null;
                return;
            }
            if (HandleProgressionClick(point)) return;
            if (currentView == ScreenView.Selector)
            {
                HandleSelector(selectorView.HitTest(point));
                return;
            }
            if (currentView == ScreenView.Modes)
            {
                modeView.HandleClick(point);
                if (modeView.Closed) currentView = ScreenView.Selector;
                return;
            }
            if (currentView == ScreenView.Almanac)
            {
                almanacView.HandleClick(point);
                ReturnFromAlmanacIfClosed();
                return;
            }
            if (TryHandleModeClick(point)) return;
            if (game.IsAdventure && currentView == ScreenView.Board)
            {
                HandleAdventureClick(point);
                return;
            }
            if (choosingSeeds)
            {
                HandleSeedChooserClick(point);
                return;
            }
            if (game.State == MatchState.Won || game.State == MatchState.Lost)
            {
                if (new Rect(316, 340, 168, 48).Contains(point)) ResetGame();
                return;
            }
            if (new Rect(704, 6, 90, 34).Contains(point))
            {
                OpenOptions();
                return;
            }
            if (game.State == MatchState.Paused)
            {
                if (new Rect(316, 280, 168, 48).Contains(point)) game.TogglePause();
                else if (new Rect(316, 340, 168, 48).Contains(point)) ResetGame();
                return;
            }
            if (!game.IsExperimentalLevelOne && new Rect(596, 6, 100, 34).Contains(point))
            {
                choosingSeeds = true;
                pendingSeeds = new HashSet<PlantKind>(deck);
                return;
            }

            if (game.LevelOneUsesNativeSkySunRules)
            {
                int x = Mathf.FloorToInt(point.x), y = Mathf.FloorToInt(point.y);
                for (int i = game.LevelOneNativeSkySuns.Count - 1; i >= 0; i--)
                {
                    LevelOneNativeSkySun sun = game.LevelOneNativeSkySuns[i];
                    if (game.TryClickLevelOneNativeSkySun(sun, x, y)) return;
                }
            }
            for (int i = game.Suns.Count - 1; i >= 0; i--)
            {
                if (SunRect(game.Suns[i]).Contains(point))
                {
                    game.CollectSun(game.Suns[i]);
                    return;
                }
            }
            for (int i = 0; i < deck.Count; i++)
            {
                if (!SeedRect(i).Contains(point)) continue;
                if (game.IsExperimentalLevelOne)
                {
                    if (game.LevelOneSelectedSeed.HasValue)
                    {
                        if (game.CancelLevelOneSeedSelection()) selected = -1;
                    }
                    else if (game.TrySelectLevelOneSeed(deck[i])) selected = i;
                    shovel = false;
                }
                else if (game.CooldownRemaining(deck[i]) <= 0f && game.SunAmount >= GameSimulation.Cost(deck[i]))
                {
                    selected = selected == i ? -1 : i;
                    shovel = false;
                }
                return;
            }
            if (!game.IsExperimentalLevelOne && new Rect(522, 0, 70, 72).Contains(point))
            {
                shovel = !shovel;
                selected = -1;
                return;
            }

            int column = Mathf.FloorToInt((point.x - GridX) / CellWidth);
            int row = Mathf.FloorToInt((point.y - GridY) / CellHeight);
            if (row < 0 || row >= GameSimulation.Rows || column < 0 || column >= GameSimulation.Columns) return;
            if (shovel)
            {
                if (game.RemovePlant(row, column)) shovel = false;
            }
            else if (selected >= 0 && game.TryPlant(deck[selected], row, column)) selected = -1;
        }

        private void CancelBoardSelection()
        {
            selectedAdventurePacket = -1;
            if (game.IsAdventure) game.CancelAdventureCobCannonAim();
            if (game.State != MatchState.Playing) return;
            if (game.IsExperimentalLevelOne && game.LevelOneSelectedSeed.HasValue &&
                !game.CancelLevelOneSeedSelection()) return;
            selected = -1;
            shovel = false;
        }

        private void HandleSelector(SelectorAction action)
        {
            switch (action)
            {
                case SelectorAction.Adventure:
                    if (!UseLegacyBoard) { StartAdventureCampaign(); break; }
                    ResetGame();
                    currentView = ScreenView.Board;
                    break;
                case SelectorAction.MiniGames: OpenModes(1); break;
                case SelectorAction.Puzzle: OpenModes(3); break;
                case SelectorAction.Survival: OpenModes(0); break;
                case SelectorAction.Almanac: OpenAlmanac(ScreenView.Selector); break;
                case SelectorAction.Options: OpenOptions(); break;
                case SelectorAction.Quit: confirmingQuit = true; break;
                case SelectorAction.ZenGarden: OpenZenGarden(); break;
                case SelectorAction.Store: OpenStore(); break;
                case SelectorAction.Help: helpView.Open(); currentView = ScreenView.Help; break;
                case SelectorAction.ChangeUser:
                    if (profileDialog != null) profileDialog.Open();
                    else SetUnavailable("用户目录无法读取", "原文件未更改");
                    break;
                case SelectorAction.Zombatar: SetUnavailable("僵尸大头贴", "尚未实现"); break;
            }
        }

        private bool HandleHelpPointer(Vector2 point, EventType eventType)
        {
            bool consumed = helpView.HandlePointer(point, eventType);
            if (!helpView.IsOpen) currentView = ScreenView.Selector;
            return consumed;
        }

        private void InitializeProfiles()
        {
            try
            {
                string[] arguments = Environment.GetCommandLineArgs();
                bool firstUserSmoke = Array.IndexOf(arguments, "-pvz-first-user-smoke") >= 0 ||
                    Array.IndexOf(arguments, "-pvz-first-user-logic-smoke") >= 0;
                string path = smokeMode
                    ? Path.Combine(Application.temporaryCachePath, "pvz-smoke-" + Guid.NewGuid().ToString("N") + ".dat")
                    : Path.Combine(Application.persistentDataPath, "replica-profiles.dat");
                profiles = new ProfileCatalog(path);
                profileDialog = new ProfileDialog(root, profiles);
                if (smokeMode && !firstUserSmoke) profiles.Create("自动测试");
                else if (profiles.Profiles.Count == 0) profileDialog.Open();
            }
            catch (Exception error)
            {
                profileLoadError = error.Message;
                SetUnavailable("用户目录无法读取", "原文件未更改");
                Debug.LogError("Could not open replica profiles: " + error.Message);
            }
        }

        private void DrawProfileName()
        {
            if (profiles == null) return;
            try
            {
                Guid? selectedId = profiles.SelectedId;
                if (!selectedId.HasValue) return;
                foreach (UserProfile profile in profiles.Profiles)
                {
                    if (profile.Id != selectedId.Value) continue;
                    Rect rect = new Rect(57, 89, 220, 27);
                    if (label == null) label = new GUIStyle(GUI.skin.label);
                    label.font = font;
                    label.fontStyle = FontStyle.Bold;
                    label.alignment = TextAnchor.MiddleCenter;
                    label.normal.textColor = new Color(0.25f, 1f, 0.12f);
                    label.fontSize = 18;
                    while (label.fontSize > 10 && label.CalcSize(new GUIContent(profile.Name)).x > rect.width)
                        label.fontSize--;
                    GUI.Label(rect, profile.Name, label);
                    return;
                }
            }
            catch (InvalidOperationException error)
            {
                if (profileLoadError == null) Debug.LogError("Replica profile state unavailable: " + error.Message);
                profileLoadError = error.Message;
            }
        }

        private static ImGuiBitmapFont LoadBitmapFont(string name)
        {
            try { return ImGuiBitmapFont.LoadFromResources(name); }
            catch (Exception error) { Debug.LogWarning("Bitmap font " + name + ": " + error.Message); return null; }
        }

        private bool DrawOptionsBitmap(string text, float right, float baseline, Color color)
        {
            return DrawBitmap(optionsBitmap, text, right, baseline, float.PositiveInfinity, color, false);
        }

        private bool DrawModeBitmap(string text, bool title, float center, float baseline, float width, Color color)
        {
            return DrawBitmap(title ? modeTitleBitmap : modeLabelBitmap, text, center, baseline, width, color, true);
        }

        private bool DrawBitmap(ImGuiBitmapFont bitmap, string text, float anchor, float baseline,
            float maxWidth, Color color, bool centered, bool integerCenter = false)
        {
            if (bitmap == null || string.IsNullOrEmpty(text)) return false;
            Dictionary<string, BitmapTextLayout> layouts;
            if (!bitmapLayouts.TryGetValue(bitmap, out layouts))
            {
                layouts = new Dictionary<string, BitmapTextLayout>();
                bitmapLayouts.Add(bitmap, layouts);
            }
            BitmapTextLayout layout;
            if (!layouts.TryGetValue(text, out layout))
            {
                layout = bitmap.Layout(text);
                layouts.Add(text, layout);
            }
            if (layout.MissingGlyphs.Count > 0 || layout.LineCount != 1 || layout.Width > maxWidth) return false;
            float x = centered ? anchor - (integerCenter ? Mathf.Floor(layout.Width * 0.5f) : layout.Width * 0.5f)
                : anchor - layout.Width;
            bitmap.Draw(layout, x, baseline - bitmap.Data.Base, color);
            return true;
        }

        private void DrawUnavailable()
        {
            DrawOriginalUiDialog(new Rect(200, 166, 400, 270), unavailableFeature, unavailableDetail);
            DrawOriginalUiButton(new Rect(320, 330, 160, 42), "确定");
        }

        private void SetUnavailable(string feature, string detail)
        {
            unavailableFeature = feature;
            unavailableDetail = detail;
        }

        private void DrawQuitConfirmation()
        {
            Fill(new Rect(0, 0, Width, Height), new Color(0, 0, 0, 0.55f));
            Rect panel = new Rect(200, 166, 400, 270);
            float middleWidth = panel.width - 227f;
            float middleHeight = panel.height - 247f;
            DrawImage("dialog_topleft.png", new Rect(panel.x, panel.y, 107, 97));
            DrawImage("dialog_topmiddle.png", new Rect(panel.x + 107, panel.y, middleWidth, 97));
            DrawImage("dialog_topright.png", new Rect(panel.xMax - 120, panel.y, 120, 97));
            DrawImage("dialog_centerleft.png", new Rect(panel.x, panel.y + 97, 107, middleHeight));
            DrawImage("dialog_centermiddle.png", new Rect(panel.x + 107, panel.y + 97, middleWidth, middleHeight));
            DrawImage("dialog_centerright.png", new Rect(panel.xMax - 120, panel.y + 97, 120, middleHeight));
            DrawImage("dialog_bigbottomleft.png", new Rect(panel.x, panel.yMax - 150, 107, 150));
            DrawImage("dialog_bigbottommiddle.png", new Rect(panel.x + 107, panel.yMax - 150, middleWidth, 150));
            DrawImage("dialog_bigbottomright.png", new Rect(panel.xMax - 120, panel.yMax - 150, 120, 150));
            DrawImage("dialog_header.png", new Rect(panel.center.x - 93.5f, panel.y - 8, 187, 64));
            Label(new Rect(250, 210, 300, 30), "退出", 22, new Color(1f, 0.86f, 0.49f), TextAnchor.MiddleCenter, true);
            Label(new Rect(245, 245, 310, 76), "你确定想要\n退出游戏吗？", 20, Color.white, TextAnchor.MiddleCenter, true);
            DrawThreePartButton(new Rect(245, 346, 135, 46), "退出游戏");
            DrawThreePartButton(new Rect(420, 346, 135, 46), "取消");
        }

        private void DrawThreePartButton(Rect rect, string text)
        { DrawOriginalUiButton(rect, text); }

        private void OpenModes(int category)
        {
            modeView.Open(category);
            currentView = ScreenView.Modes;
        }

        private void OpenAlmanac(ScreenView returnView, bool returnToOptions = false)
        {
            almanacReturnView = returnView;
            almanacReturnsToOptions = returnToOptions;
            almanacView.Open();
            currentView = ScreenView.Almanac;
        }

        private bool HandleAlmanacPointer(Vector2 point, EventType eventType)
        {
            bool consumed = almanacView.HandlePointer(point, eventType);
            ReturnFromAlmanacIfClosed();
            return consumed;
        }

        private void ReturnFromAlmanacIfClosed()
        {
            if (!almanacView.IsClosed) return;
            currentView = almanacReturnView;
            if (almanacReturnsToOptions) optionsView.ResumeFromAlmanac();
            almanacReturnsToOptions = false;
        }

        private void OpenOptions()
        {
            optionsView.GameplayButtonsEnabled = currentView == ScreenView.Board;
            optionsView.Open(musicVolume, soundVolume, Screen.fullScreen);
        }

        private void CloseOptions()
        {
            optionsView.Close();
            if (!optionsView.IsOpen) ApplyOptions();
        }

        private void ApplyOptions()
        {
            // The original almanac is modal over the options dialog. Retain its
            // unapplied draft and keep gameplay suspended until Return to Game.
            if (optionsView.CloseResult == OptionsCloseResult.OpenAlmanac)
            {
                OpenAlmanac(currentView, true);
                return;
            }
            musicVolume = optionsView.MusicVolume;
            soundVolume = optionsView.SoundVolume;
            musicSource.volume = musicVolume;
            audioSource.volume = soundVolume;
            if (!smokeMode)
            {
                PlayerPrefs.SetFloat("PvZReplica.Music", musicVolume);
                PlayerPrefs.SetFloat("PvZReplica.Sound", soundVolume);
                PlayerPrefs.Save();
                if (Screen.fullScreen != optionsView.FullScreen) Screen.fullScreen = optionsView.FullScreen;
            }
            switch (optionsView.CloseResult)
            {
                case OptionsCloseResult.RestartLevel: ResetGame(); break;
                case OptionsCloseResult.MainMenu: currentView = ScreenView.Selector; break;
            }
        }

        private void HandleSeedChooserClick(Vector2 point)
        {
            for (int i = 0; i < availablePlants.Length; i++)
            {
                if (!ChooserSeedRect(i).Contains(point)) continue;
                PlantKind kind = availablePlants[i];
                if (pendingSeeds.Contains(kind)) pendingSeeds.Remove(kind);
                else if (pendingSeeds.Count < 7) pendingSeeds.Add(kind);
                return;
            }
            if (new Rect(322, 440, 156, 42).Contains(point) && pendingSeeds.Count > 0)
            {
                deck.Clear();
                foreach (PlantKind kind in availablePlants)
                    if (pendingSeeds.Contains(kind)) deck.Add(kind);
                choosingSeeds = false;
                ResetGame();
            }
            else if (new Rect(341, 493, 118, 30).Contains(point)) choosingSeeds = false;
        }

        private void DrawBoard()
        {
            if (game.IsAdventure) { DrawAdventureBoard(); return; }
            if (game.LevelOneUsesSingleRowLawn)
            {
                NativeRect background = FirstAdventureBoardUi.UnsoddedBackground(0, 0).Destination;
                NativeRect grass = FirstAdventureBoardUi.SingleRowSod(0, 0, 1000).Destination;
                DrawImage("background1unsodded.jpg", new Rect(background.X, background.Y, background.Width, background.Height));
                DrawImage("sod1row.png", new Rect(grass.X, grass.Y, grass.Width, grass.Height));
            }
            else DrawImage("background1.jpg", new Rect(-180, 0, 1400, 600));
            for (int row = 0; row < GameSimulation.Rows; row++)
            {
                foreach (Mower mower in game.Mowers)
                {
                    if (mower.Row != row || (!mower.Available && !mower.Active)) continue;
                    Rect r = new Rect(GridX + mower.X * CellWidth - 22f, GridY + row * CellHeight + 43f, 78f, 58f);
                    animations.Draw("LawnMower", r, game.Elapsed);
                }
                foreach (Plant plant in game.Plants)
                {
                    if (plant.Row != row) continue;
                    float x = GridX + plant.Column * CellWidth;
                    float y = GridY + row * CellHeight;
                    DrawImage("plantshadow.png", new Rect(x + 2, y + 71, 76, 24));
                    Rect plantRect = new Rect(x, y + 5, 80, 88);
                    if (plant.Kind == PlantKind.PotatoMine)
                    {
                        string clip = plant.MinePhase == MinePhase.Armed ? "anim_armed" :
                            plant.MinePhase == MinePhase.Rising ? "anim_rise" : "anim_idle";
                        float time = plant.MinePhase == MinePhase.Rising ? plant.PhaseAge * 1.5f : plant.PhaseAge;
                        animations.Draw("PotatoMine", plantRect, time, clip, plant.MinePhase != MinePhase.Rising);
                    }
                    else if (plant.Kind == PlantKind.Wallnut || plant.Kind == PlantKind.TallNut)
                    {
                        bool tall = plant.Kind == PlantKind.TallNut;
                        float maxHealth = tall ? 8000f : 4000f;
                        string prefix = tall ? "IMAGE_REANIM_TALLNUT_CRACKED" : "IMAGE_REANIM_WALLNUT_CRACKED";
                        string cracked = plant.Health < maxHealth / 3f ? prefix + "2" :
                            plant.Health < maxHealth * 2f / 3f ? prefix + "1" : null;
                        animations.DrawWithImageOverride(tall ? "Tallnut" : "Wallnut", plantRect, plant.Age,
                            tall ? "anim_idle" : "anim_face", cracked);
                    }
                    else if ((plant.Kind == PlantKind.Peashooter || plant.Kind == PlantKind.SnowPea ||
                        plant.Kind == PlantKind.Repeater) && plant.ShootAge >= 0f)
                    {
                        string animationName = AnimationName(plant.Kind);
                        float shootFps = plant.Kind == PlantKind.Repeater ? 45f : 35f;
                        float duration;
                        if (!shootingDurations.TryGetValue(animationName, out duration))
                        {
                            ReanimationRenderer.AnimationInfo info;
                            duration = animations.TryGetAnimationInfo(animationName, "anim_shooting", out info)
                                ? info.FrameCount / shootFps : 0f;
                            shootingDurations[animationName] = duration;
                        }
                        if (plant.ShootAge < duration)
                            animations.DrawAttachedPlant(animationName, plantRect, plant.Age, plant.ShootAge,
                                "anim_shooting", false, 0f, shootFps);
                        else animations.Draw(animationName, plantRect, plant.Age);
                    }
                    else animations.Draw(AnimationName(plant.Kind), plantRect, plant.Age);
                }
                foreach (Zombie zombie in game.Zombies)
                {
                    if (zombie.Row != row) continue;
                    float x = GridX + zombie.X * CellWidth;
                    float y = GridY + row * CellHeight;
                    DrawImage("plantshadow.png", new Rect(x - 38, y + 76, 76, 22));
                    Color previous = GUI.color;
                    if (zombie.ChillRemaining > 0f) GUI.color = new Color(0.65f, 0.8f, 1f, previous.a);
                    animations.DrawZombie(new Rect(x - 62, y - 44, 125, 145), zombie.Age,
                        ZombieAppearance(zombie.Kind), zombie.Kind == ZombieKind.Flag ||
                            (zombie.Kind == ZombieKind.ScreenDoor ? zombie.ShieldHealth > 0f : zombie.ArmorHealth > 0f),
                        zombie.Eating ? "anim_eat" : "anim_walk");
                    GUI.color = previous;
                }
                foreach (Pea pea in game.Peas)
                {
                    if (pea.Row == row)
                        DrawImage(pea.IsSnow ? "ProjectileSnowPea.png" : "ProjectilePea.png", new Rect(GridX + pea.X * CellWidth - 13,
                            GridY + row * CellHeight + 20, 26, 26));
                }
                foreach (LaneFlame flame in game.Flames)
                {
                    if (flame.Row != row) continue;
                    for (int i = 0; i < 11; i++)
                        animations.Draw("fire", new Rect(GridX + i * 68f - 20f, GridY + row * CellHeight - 18f, 82f, 112f),
                            flame.Age + i * 0.025f, "anim_flame");
                }
            }
            foreach (Sun sun in game.Suns)
                animations.Draw("Sun", SunRect(sun), sun.Age);
            if (game.LevelOneUsesNativeSkySunRules)
            {
                foreach (LevelOneNativeSkySun sun in game.LevelOneNativeSkySuns)
                {
                    Color previous = GUI.color;
                    try
                    {
                        if (sun.FadeTicksRemaining > 0)
                            GUI.color = new Color(previous.r, previous.g, previous.b,
                                previous.a * sun.FadeTicksRemaining / 15f);
                        animations.Draw("Sun", new Rect(sun.PixelX, sun.PixelY,
                            LevelOneNativeSkySun.BaseWidthPixels, LevelOneNativeSkySun.BaseHeightPixels), game.Elapsed);
                    }
                    finally { GUI.color = previous; }
                }
            }
        }

        private void DrawInterface()
        {
            if (DrawModesInterface()) return;
            if (game.IsAdventure) { DrawAdventureInterface(); return; }
            DrawImage("SeedBank.png", new Rect(0, 0, 446, 87));
            if (game.IsExperimentalLevelOne)
            {
                Texture2D bank = Texture("SeedBank.png");
                NativeBlit cap = FirstAdventureBoardUi.SeedBankEndCap(0, 0);
                if (bank != null)
                    GUI.DrawTextureWithTexCoords(new Rect(cap.Destination.X, cap.Destination.Y, cap.Destination.Width, cap.Destination.Height),
                        bank, new Rect((float)cap.Source.X / bank.width, 0f, (float)cap.Source.Width / bank.width, 1f));
                if (!DrawBitmap(sunCounterBitmap, FirstAdventureBoardUi.DisplayedSun(game.SunAmount).ToString(),
                    38f, 78f, 76f, Color.black, true, true))
                    Label(new Rect(10, 59, 55, 22), game.SunAmount.ToString(), 18, Color.black, TextAnchor.MiddleCenter, true);
            }
            else Label(new Rect(10, 59, 55, 22), game.SunAmount.ToString(), 18, Color.black, TextAnchor.MiddleCenter, true);
            Texture2D packets = Texture("seeds.png");
            for (int i = 0; i < deck.Count; i++)
            {
                Rect card = SeedRect(i);
                if (packets != null) GUI.DrawTextureWithTexCoords(card, packets, new Rect(0, 0, 1f / 9f, 1));
                DrawSeedIcon(deck[i], card);
                Label(new Rect(card.x + 1, card.y + 51, 46, 17), GameSimulation.Cost(deck[i]).ToString(),
                    13, Color.black, TextAnchor.MiddleRight, true);
                float fraction = game.CooldownRemaining(deck[i]) / GameSimulation.RechargeTime(deck[i]);
                if (fraction > 0f) Fill(new Rect(card.x, card.y, card.width, card.height * fraction), new Color(0, 0, 0, 0.55f));
                else if (game.SunAmount < GameSimulation.Cost(deck[i])) Fill(card, new Color(0, 0, 0, 0.36f));
                if (selected == i) Outline(card, new Color(1f, 0.91f, 0.35f), 3f);
            }
            if (!game.IsExperimentalLevelOne)
            {
                DrawImage("ShovelBank.png", new Rect(522, 0, 70, 72));
                DrawImage("Shovel.png", new Rect(522, 0, 70, 70));
                if (shovel) Outline(new Rect(522, 0, 70, 72), new Color(1f, 0.91f, 0.35f), 3f);
                Fill(new Rect(596, 6, 100, 34), new Color(0.28f, 0.18f, 0.09f, 0.9f));
                Label(new Rect(596, 6, 100, 34), "选植物", 17, Color.white, TextAnchor.MiddleCenter, true);
            }
            Fill(new Rect(704, 6, 90, 34), new Color(0.28f, 0.18f, 0.09f, 0.9f));
            Label(new Rect(704, 6, 90, 34), game.State == MatchState.Paused ? "继续" : "菜单",
                17, Color.white, TextAnchor.MiddleCenter, true);
            float progress = game.IsExperimentalLevelOne ? game.LevelOneWaveProgress :
                game.Rules.ZombieCount == 0 ? 0 : (float)game.SpawnedZombies / game.Rules.ZombieCount;
            Texture2D meter = Texture("FlagMeter.png");
            if (meter != null)
            {
                Rect meterRect = new Rect(632, 570, 158, 27);
                GUI.DrawTextureWithTexCoords(meterRect, meter, new Rect(0, 0.5f, 1, 0.5f));
                float fillWidth = 144f * Mathf.Clamp01(progress);
                if (fillWidth > 0f)
                {
                    float start = 151f - fillWidth;
                    GUI.DrawTextureWithTexCoords(new Rect(meterRect.x + start, meterRect.y, fillWidth, meterRect.height),
                        meter, new Rect(start / 158f, 0, fillWidth / 158f, 0.5f));
                }
                Texture2D parts = Texture("FlagMeterParts.png");
                if (parts != null)
                    GUI.DrawTextureWithTexCoords(new Rect(meterRect.x + 138f - 120f * progress, meterRect.y - 3, 25, 25),
                        parts, new Rect(0, 0, 1f / 3f, 1));
            }

            if (game.State == MatchState.Paused)
                DrawDialog("已暂停", "继续游戏", true);
            else if (game.State == MatchState.Won)
                DrawDialog("你赢了！", "再玩一次", false);
            else if (game.State == MatchState.Lost)
                DrawDialog("僵尸吃掉了你的脑子！", "再试一次", false);
            if (choosingSeeds) DrawSeedChooser();
            if (game.IsExperimentalLevelOne && game.State == MatchState.Playing) DrawLevelOneAdvice();
        }

        private void DrawLevelOneAdvice()
        {
            string message = null;
            switch (game.CurrentLevelOneTutorialStage)
            {
                case LevelOneTutorialStage.AwaitingSeedSelection:
                    message = game.Plants.Count == 0 ? "点击种子包，把它捡起来！" : "点击豌豆射手，再种一棵！";
                    break;
                case LevelOneTutorialStage.AwaitingPlantPlacement: message = "点击草地种下你的种子！"; break;
                case LevelOneTutorialStage.AwaitingUsableSeed: message = "干得漂亮！"; break;
                case LevelOneTutorialStage.ZombieOnslaughtPrompt: message = "别让僵尸靠近你的房子！"; break;
            }
            if (message != null)
                Label(new Rect(125, 520, 550, 35), message, 19, Color.white, TextAnchor.MiddleCenter, true);
        }

        private void DrawSeedChooser()
        {
            Fill(new Rect(0, 0, Width, Height), new Color(0, 0, 0, 0.58f));
            DrawImage("SeedChooser_Background.png", new Rect(168, 45, 465, 513));
            Label(new Rect(197, 83, 310, 34), "选择植物", 24, Color.white, TextAnchor.MiddleLeft, true);
            Label(new Rect(500, 83, 95, 34), pendingSeeds.Count + " / 7", 18, Color.white, TextAnchor.MiddleRight, true);
            Texture2D packets = Texture("seeds.png");
            for (int i = 0; i < availablePlants.Length; i++)
            {
                Rect card = ChooserSeedRect(i);
                if (packets != null) GUI.DrawTextureWithTexCoords(card, packets, new Rect(0, 0, 1f / 9f, 1));
                DrawSeedIcon(availablePlants[i], card);
                Label(new Rect(card.x + 1, card.y + 51, 46, 17), GameSimulation.Cost(availablePlants[i]).ToString(),
                    13, Color.black, TextAnchor.MiddleRight, true);
                if (pendingSeeds.Contains(availablePlants[i])) Outline(card, new Color(0.9f, 0.92f, 0.35f), 3);
            }
            DrawImage(pendingSeeds.Count > 0 ? "SeedChooser_Button.png" : "SeedChooser_Button_Disabled.png",
                new Rect(322, 440, 156, 42));
            Label(new Rect(322, 440, 156, 42), "重选并开始", 17, Color.white, TextAnchor.MiddleCenter, true);
            DrawOriginalUiButton(new Rect(341, 493, 118, 30), "取消");
        }

        private void DrawSeedIcon(PlantKind kind, Rect card)
        {
            Rect icon = new Rect(card.x + 3, card.y + 8, 44, 40);
            if (kind == PlantKind.PotatoMine) animations.Draw("PotatoMine", icon, game.Elapsed, "anim_armed");
            else animations.Draw(AnimationName(kind), icon, game.Elapsed);
        }

        private void DrawDialog(string title, string button, bool paused)
        {
            DrawOriginalUiDialog(new Rect(200, 166, 400, 270), title, null);
            DrawOriginalUiButton(new Rect(316, paused ? 280 : 340, 168, 48), button);
            if (paused) DrawOriginalUiButton(new Rect(316, 340, 168, 48), "重新开始");
        }

        private Rect SeedRect(int index)
        {
            if (game.IsExperimentalLevelOne)
            {
                NativeRect slot = FirstAdventureBoardUi.FirstSeedSlot(0, 0);
                return new Rect(slot.X + 59 * index, slot.Y, slot.Width, slot.Height);
            }
            return new Rect(80 + 50 * index, 8, 50, 70);
        }
        private static Rect ChooserSeedRect(int index) { return new Rect(198 + index % 6 * 65, 150 + index / 6 * 84, 50, 70); }
        private static Rect SunRect(Sun sun)
        {
            return new Rect(GridX + sun.X * CellWidth - 35, GridY + sun.Y * CellHeight - 35, 70, 70);
        }
        private static string AnimationName(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.Sunflower: return "SunFlower";
                case PlantKind.Wallnut: return "Wallnut";
                case PlantKind.CherryBomb: return "CherryBomb";
                case PlantKind.SnowPea: return "SnowPea";
                case PlantKind.PotatoMine: return "PotatoMine";
                case PlantKind.Repeater: return "PeaShooter";
                case PlantKind.Jalapeno: return "Jalapeno";
                case PlantKind.TallNut: return "Tallnut";
                default: return "PeaShooterSingle";
            }
        }

        private static ZombieVariant ZombieAppearance(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Conehead: return ZombieVariant.Conehead;
                case ZombieKind.Buckethead: return ZombieVariant.Buckethead;
                case ZombieKind.Flag: return ZombieVariant.Flag;
                case ZombieKind.ScreenDoor: return ZombieVariant.ScreenDoor;
                default: return ZombieVariant.Normal;
            }
        }

        private Texture2D Texture(string name)
        {
            Texture2D texture;
            if (textures.TryGetValue(name, out texture)) return texture;
            string path = Path.Combine(root, "images", name);
            if (!File.Exists(path)) { textures[name] = null; return null; }
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path))) { Destroy(texture); texture = null; }
            else { texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp; }
            textures[name] = texture;
            return texture;
        }

        private void DrawImage(string name, Rect rect)
        {
            Texture2D texture = Texture(name);
            if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Outline(Rect rect, Color color, float thickness)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void Label(Rect rect, string text, int size, Color color, TextAnchor alignment, bool bold)
        {
            if (label == null) label = new GUIStyle(GUI.skin.label);
            label.font = font;
            label.fontSize = size;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = alignment;
            label.normal.textColor = color;
            GUI.Label(rect, text, label);
        }
    }
}
