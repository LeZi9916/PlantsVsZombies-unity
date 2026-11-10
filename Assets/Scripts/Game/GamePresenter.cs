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
    /// <summary>Updates the direct scene renderers and retained Canvas interface.</summary>
    internal sealed class GamePresenter
    {
        /// <summary>Coordinates the simulation state consumed by this presentation.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates a presentation for the active game coordinator.</summary>
        /// <param name="owner">The session providing state and game commands.</param>
        internal GamePresenter(PvZGame owner)
        {
            _owner = owner;
            _drawConveyorPackets = DrawAdventureConveyorPacketContents;
            _drawCardImage = DrawCardImage;
            _textureLookup = owner.Texture;
            _drawPacketMask = DrawAdventurePacketMask;
        }

        /// <summary>Updates the logical pointer used by hover artwork and cursor presentation.</summary>
        /// <param name="position">The pointer in the original 800 by 600 coordinate space.</param>
        internal void SetPointerPosition(Vector2 position)
        {
            CanvasUI.PointerPosition = position;
        }

        /// <summary>Shows the board wallet after collecting a coin.</summary>
        /// <param name="until">The simulation time until which the wallet remains visible.</param>
        internal void ShowWalletUntil(float until)
        {
            _walletVisibleUntil = until;
        }

        /// <summary>Releases retained interface objects when the game lifetime ends.</summary>
        internal void Dispose()
        {
            CanvasUI.Dispose();
        }

        /// <summary>Stores width.</summary>
        private const float Width = 800f;
        /// <summary>Stores height.</summary>
        private const float Height = 600f;
        /// <summary>Stores shooting durations.</summary>
        private readonly Dictionary<string, float> _shootingDurations = new Dictionary<string, float>();
        /// <summary>Stores bitmap layouts.</summary>
        private readonly Dictionary<CanvasBitmapFont, Dictionary<string, BitmapTextLayout>> _bitmapLayouts = new Dictionary<CanvasBitmapFont, Dictionary<string, BitmapTextLayout>>();
        /// <summary>Reuses live actor and effect identities across presentation frames.</summary>
        private readonly PresentationKeyCache _presentationKeys = new PresentationKeyCache();
        /// <summary>Reuses numeric labels with a fixed upper bound on retained values.</summary>
        private readonly Dictionary<int, string> _numberLabels = new Dictionary<int, string>();
        /// <summary>Reuses native draw callbacks for all actors in this synchronous presenter.</summary>
        private AdventureActorRenderer? _actorRenderer;
        /// <summary>Reuses the selected seed traversal buffer.</summary>
        private readonly List<PlantKind> _openingSelectedSeeds = new List<PlantKind>(10);
        /// <summary>Reuses the street preview's lightweight zombie poses.</summary>
        private readonly List<Zombie> _openingZombiePoses = new List<Zombie>();
        /// <summary>Reuses the positional probe used by detached zombie parts.</summary>
        private readonly Zombie _detachedPartProbe = new Zombie();
        /// <summary>Reuses the probe used to calculate cursor placement.</summary>
        private readonly Plant _placementProbe = new Plant();
        /// <summary>Reuses attached head buffers for each supported head count.</summary>
        private readonly HeadPlayback[][] _plantHeads = { new HeadPlayback[1], new HeadPlayback[2], new HeadPlayback[3] };
        /// <summary>Reuses the list used to retire expired composition surfaces.</summary>
        private readonly List<string> _expiredSurfaces = new List<string>();
        /// <summary>Reuses the synchronous conveyor composition callback.</summary>
        private readonly Func<bool> _drawConveyorPackets;
        /// <summary>Whether conveyor contents should display backwards packet hints.</summary>
        private bool _showBackwardsPackets;
        /// <summary>Reuses the authored Gloom-shroom burst schedule.</summary>
        private static readonly float[] s_gloomBursts = { .64f, .92f, 1.20f, 1.48f };
        /// <summary>Caches resolved native plant animation names.</summary>
        private static readonly Dictionary<PlantKind, string> s_plantAnimationNames = new Dictionary<PlantKind, string>();
        /// <summary>Gets the session's reusable actor renderer.</summary>
        private AdventureActorRenderer ActorRenderer => _actorRenderer ??= new AdventureActorRenderer(_owner._animations);

        /// <summary>Gets a reusable numeric label without allowing an unbounded text cache.</summary>
        /// <param name="value">The displayed integer.</param>
        /// <returns>The retained decimal representation.</returns>
        private string NumberLabel(int value)
        {
            if (!_numberLabels.TryGetValue(value, out var text))
            {
                if (_numberLabels.Count >= 256)
                {
                    _numberLabels.Clear();
                }

                text = value.ToString();
                _numberLabels.Add(value, text);
            }

            return text;
        }

        /// <summary>Reuses the seed packet context consumed immediately by its presentation sampler.</summary>
        private readonly AdventureSeedBankContext _packetContext = new AdventureSeedBankContext();
        /// <summary>Reuses the synchronous seed cooldown composition callback.</summary>
        private readonly Func<bool> _drawPacketMask;
        /// <summary>The immediately consumed seed kind and rectangle for cooldown composition.</summary>
        private PlantKind _maskKind;
        /// <summary>The immediately consumed packet rectangle for cooldown composition.</summary>
        private Rect _maskCard;
        /// <summary>Reuses the host texture lookup passed to original interface widgets.</summary>
        private readonly Func<string, Texture2D> _textureLookup;
        /// <summary>Reuses the native edit field's transparent appearance.</summary>
        private GUIStyle? _textFieldStyle;
        /// <summary>Reuses the callback used to grayscale an imitated packet image.</summary>
        private readonly Func<bool> _drawCardImage;
        /// <summary>The immediately consumed imitated packet image.</summary>
        private Texture _cardAtlas = null!;
        /// <summary>The immediately consumed packet destination and atlas coordinates.</summary>
        private Rect _cardDestination, _cardSource;
        /// <summary>The last displayed board and wave used to refresh the status caption.</summary>
        private AdventureBoard? _captionBoard;
        /// <summary>The last displayed wave number.</summary>
        private int _captionWave = -1;
        /// <summary>The retained wave progress label.</summary>
        private string _waveCaption = "";
        /// <summary>Stores label.</summary>
        private GUIStyle _label;

        /// <summary>Submits the scene and Canvas presentation once after simulation.</summary>
        internal void Present()
        {
            _presentationKeys.BeginFrame();
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = GameViewport.ScreenRect;
            CanvasUI.BeginFrame();
            _owner._animations.BeginObjectFrame();
            try
            {
                if (DrawOriginalLoading())
                {
                    return;
                }

                var scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
                var offsetX = (Screen.width - Width * scale) * 0.5f;
                var offsetY = (Screen.height - Height * scale) * 0.5f;
                CanvasUI.Matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0), Quaternion.identity, new Vector3(scale, scale, 1f));
                CanvasUI.Enabled = !_owner._optionsView.IsOpen && !_owner._confirmingQuit && _owner._unavailableFeature == null && (_owner._profileDialog == null || !_owner._profileDialog.IsOpen);
                switch (_owner._currentView)
                {
                    case PvZGame.ScreenView.Selector:
                        _owner._selectorView.Draw(Time.unscaledTime);
                        DrawProfileName();
                        DrawAdventureProgress();
                        DrawAccountStatus();
                        break;
                    case PvZGame.ScreenView.Modes:
                        _owner._modeView.Draw();
                        break;
                    case PvZGame.ScreenView.Store:
                        DrawStore();
                        break;
                    case PvZGame.ScreenView.Garden:
                        DrawGarden();
                        break;
                    case PvZGame.ScreenView.Tree:
                        DrawWisdomTree();
                        break;
                    case PvZGame.ScreenView.Import:
                        DrawOriginalSaveUtility();
                        break;
                    case PvZGame.ScreenView.Almanac:
                        _owner._almanacView.Draw(Time.unscaledTime);
                        break;
                    case PvZGame.ScreenView.Help:
                        _owner._helpView.Draw();
                        break;
                    case PvZGame.ScreenView.Board:
                        DrawBoard();
                        DrawInterface();
                        DrawAdventurePlantCursor();
                        break;
                }

                DrawOriginalHoverTips();
                if (_owner._unavailableFeature != null)
                {
                    DrawUnavailable();
                }

                if (_owner._confirmingQuit)
                {
                    DrawQuitConfirmation();
                }

                if (_owner._optionsView.IsOpen)
                {
                    _owner._optionsView.Draw();
                }

                if (_owner._profileDialog != null && _owner._profileDialog.IsOpen)
                {
                    _owner._profileDialog.Draw();
                }
            }
            finally
            {
                _owner._animations.EndObjectFrame();
                CanvasUI.EndFrame();
            }
        }

        /// <summary>Performs draw profile name.</summary>
        private void DrawProfileName()
        {
            if (_owner._profiles == null)
            {
                return;
            }

            try
            {
                var selectedId = _owner._profiles.SelectedId;
                if (!selectedId.HasValue)
                {
                    return;
                }

                foreach (UserProfile profile in _owner._profiles.Profiles)
                {
                    if (profile.Id != selectedId.Value)
                    {
                        continue;
                    }

                    var rect = new Rect(57, 89, 220, 27);
                    if (_label == null)
                    {
                        _label = new GUIStyle();
                    }

                    _label.font = _owner._font;
                    _label.fontStyle = FontStyle.Bold;
                    _label.alignment = TextAnchor.MiddleCenter;
                    _label.normal.textColor = new Color(0.25f, 1f, 0.12f);
                    _label.fontSize = 18;
                    while (_label.fontSize > 10 && _label.CalcSize(new GUIContent(profile.Name)).x > rect.width)
                    {
                        _label.fontSize--;
                    }

                    CanvasUI.Label(rect, profile.Name, _label);
                    return;
                }
            }
            catch (InvalidOperationException error)
            {
                if (_owner._profileLoadError == null)
                {
                    Debug.LogError("Replica profile state unavailable: " + error.Message);
                }

                _owner._profileLoadError = error.Message;
            }
        }

        /// <summary>Performs draw options bitmap.</summary>
        /// <param name = "text">The text.</param>
        /// <param name = "right">The right.</param>
        /// <param name = "baseline">The baseline.</param>
        /// <param name = "color">The color.</param>
        /// <returns>The resulting draw options bitmap.</returns>
        internal bool DrawOptionsBitmap(string text, float right, float baseline, Color color)
        {
            return DrawBitmap(_owner._optionsBitmap, text, right, baseline, float.PositiveInfinity, color, false);
        }

        /// <summary>Performs draw mode bitmap.</summary>
        /// <param name = "text">The text.</param>
        /// <param name = "title">The title.</param>
        /// <param name = "center">The center.</param>
        /// <param name = "baseline">The baseline.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "color">The color.</param>
        /// <returns>The resulting draw mode bitmap.</returns>
        internal bool DrawModeBitmap(string text, bool title, float center, float baseline, float width, Color color)
        {
            return DrawBitmap(title ? _owner._modeTitleBitmap : _owner._modeLabelBitmap, text, center, baseline, width, color, true);
        }

        /// <summary>Performs draw bitmap.</summary>
        /// <param name = "bitmap">The bitmap.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "anchor">The anchor.</param>
        /// <param name = "baseline">The baseline.</param>
        /// <param name = "maxWidth">The max width.</param>
        /// <param name = "color">The color.</param>
        /// <param name = "centered">The centered.</param>
        /// <param name = "integerCenter">The integer center.</param>
        /// <returns>The resulting draw bitmap.</returns>
        private bool DrawBitmap(CanvasBitmapFont bitmap, string text, float anchor, float baseline, float maxWidth, Color color, bool centered, bool integerCenter = false)
        {
            if (bitmap == null || string.IsNullOrEmpty(text))
            {
                return false;
            }

            Dictionary<string, BitmapTextLayout> layouts;
            if (!_bitmapLayouts.TryGetValue(bitmap, out layouts))
            {
                layouts = new Dictionary<string, BitmapTextLayout>();
                _bitmapLayouts.Add(bitmap, layouts);
            }

            BitmapTextLayout layout;
            if (!layouts.TryGetValue(text, out layout))
            {
                if (layouts.Count >= 128)
                {
                    layouts.Clear();
                }

                layout = bitmap.Layout(text);
                layouts.Add(text, layout);
            }

            if (layout.MissingGlyphs.Count > 0 || layout.LineCount != 1 || layout.Width > maxWidth)
            {
                return false;
            }

            var x = centered ? anchor - (integerCenter ? Mathf.Floor(layout.Width * 0.5f) : layout.Width * 0.5f) : anchor - layout.Width;
            bitmap.Draw(layout, x, baseline - bitmap.Data.Base, color);
            return true;
        }

        /// <summary>Performs draw unavailable.</summary>
        private void DrawUnavailable()
        {
            DrawOriginalUiDialog(new Rect(200, 166, 400, 270), _owner._unavailableFeature, _owner._unavailableDetail);
            DrawOriginalUiButton(new Rect(320, 330, 160, 42), "确定");
        }

        /// <summary>Performs draw quit confirmation.</summary>
        private void DrawQuitConfirmation()
        {
            Fill(new Rect(0, 0, Width, Height), new Color(0, 0, 0, 0.55f));
            var panel = new Rect(200, 166, 400, 270);
            var middleWidth = panel.width - 227f;
            var middleHeight = panel.height - 247f;
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

        /// <summary>Performs draw three part button.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        private void DrawThreePartButton(Rect rect, string text)
        {
            DrawOriginalUiButton(rect, text);
        }

        /// <summary>Tracks whether board textures belong to the current object rendering frame.</summary>
        private bool _renderingBoardObjects;
        /// <summary>Renders the board through pooled scene renderers before drawing its interface.</summary>
        private void DrawBoard()
        {
            _owner._animations.BeginObjectFrame();
            _renderingBoardObjects = true;
            try
            {
                DrawBoardObjects();
            }
            finally
            {
                _renderingBoardObjects = false;
                _owner._animations.EndObjectFrame();
            }
        }

        /// <summary>Performs draw board objects.</summary>
        private void DrawBoardObjects()
        {
            if (_owner._game.IsAdventure)
            {
                DrawAdventureBoard();
                return;
            }

            if (_owner._game.LevelOneUsesSingleRowLawn)
            {
                var background = FirstAdventureBoardUi.UnsoddedBackground(0, 0).Destination;
                var grass = FirstAdventureBoardUi.SingleRowSod(0, 0, 1000).Destination;
                DrawImage("background1unsodded.jpg", new Rect(background.X, background.Y, background.Width, background.Height));
                DrawImage("sod1row.png", new Rect(grass.X, grass.Y, grass.Width, grass.Height));
            }
            else
            {
                DrawImage("background1.jpg", new Rect(-180, 0, 1400, 600));
            }

            for (var row = 0; row < GameSimulation.Rows; row++)
            {
                foreach (Mower mower in _owner._game.Mowers)
                {
                    if (mower.Row != row || (!mower.Available && !mower.Active))
                    {
                        continue;
                    }

                    var r = new Rect(PvZGame.GridX + mower.X * PvZGame.CellWidth - 22f, PvZGame.GridY + row * PvZGame.CellHeight + 43f, 78f, 58f);
                    _owner._animations.Draw("LawnMower", r, _owner._game.Elapsed);
                }

                foreach (Plant plant in _owner._game.Plants)
                {
                    if (plant.Row != row)
                    {
                        continue;
                    }

                    var x = PvZGame.GridX + plant.Column * PvZGame.CellWidth;
                    var y = PvZGame.GridY + row * PvZGame.CellHeight;
                    DrawImage("plantshadow.png", new Rect(x + 2, y + 71, 76, 24));
                    var plantRect = new Rect(x, y + 5, 80, 88);
                    if (plant.Kind == PlantKind.PotatoMine)
                    {
                        var clip = plant.MinePhase == MinePhase.Armed ? "anim_armed" : plant.MinePhase == MinePhase.Rising ? "anim_rise" : "anim_idle";
                        var time = plant.MinePhase == MinePhase.Rising ? plant.PhaseAge * 1.5f : plant.PhaseAge;
                        _owner._animations.Draw("PotatoMine", plantRect, time, clip, plant.MinePhase != MinePhase.Rising);
                    }
                    else if (plant.Kind == PlantKind.Wallnut || plant.Kind == PlantKind.TallNut)
                    {
                        var tall = plant.Kind == PlantKind.TallNut;
                        var maxHealth = tall ? 8000f : 4000f;
                        var prefix = tall ? "IMAGE_REANIM_TALLNUT_CRACKED" : "IMAGE_REANIM_WALLNUT_CRACKED";
                        var cracked = plant.Health < maxHealth / 3f ? prefix + "2" : plant.Health < maxHealth * 2f / 3f ? prefix + "1" : null;
                        _owner._animations.DrawWithImageOverride(tall ? "Tallnut" : "Wallnut", plantRect, plant.Age, tall ? "anim_idle" : "anim_face", cracked);
                    }
                    else if ((plant.Kind == PlantKind.Peashooter || plant.Kind == PlantKind.SnowPea || plant.Kind == PlantKind.Repeater) && plant.ShootAge >= 0f)
                    {
                        var animationName = AnimationName(plant.Kind);
                        var shootFps = plant.Kind == PlantKind.Repeater ? 45f : 35f;
                        float duration;
                        if (!_shootingDurations.TryGetValue(animationName, out duration))
                        {
                            global::PlantsVsZombies.Reanimation.AnimationInfo info;
                            duration = _owner._animations.TryGetAnimationInfo(animationName, "anim_shooting", out info) ? info.FrameCount / shootFps : 0f;
                            _shootingDurations[animationName] = duration;
                        }

                        if (plant.ShootAge < duration)
                        {
                            _owner._animations.DrawAttachedPlant(animationName, plantRect, plant.Age, plant.ShootAge, "anim_shooting", false, 0f, shootFps);
                        }
                        else
                        {
                            _owner._animations.Draw(animationName, plantRect, plant.Age);
                        }
                    }
                    else
                    {
                        _owner._animations.Draw(AnimationName(plant.Kind), plantRect, plant.Age);
                    }
                }

                foreach (Zombie zombie in _owner._game.Zombies)
                {
                    if (zombie.Row != row)
                    {
                        continue;
                    }

                    var x = PvZGame.GridX + zombie.X * PvZGame.CellWidth;
                    var y = PvZGame.GridY + row * PvZGame.CellHeight;
                    DrawImage("plantshadow.png", new Rect(x - 38, y + 76, 76, 22));
                    var previous = CanvasUI.Color;
                    if (zombie.ChillRemaining > 0f)
                    {
                        CanvasUI.Color = new Color(0.65f, 0.8f, 1f, previous.a);
                    }

                    _owner._animations.DrawZombie(new Rect(x - 62, y - 44, 125, 145), zombie.Age, ZombieAppearance(zombie.Kind), zombie.Kind == ZombieKind.Flag || (zombie.Kind == ZombieKind.ScreenDoor ? zombie.ShieldHealth > 0f : zombie.ArmorHealth > 0f), zombie.Eating ? "anim_eat" : "anim_walk");
                    CanvasUI.Color = previous;
                }

                foreach (Pea pea in _owner._game.Peas)
                {
                    if (pea.Row == row)
                    {
                        DrawImage(pea.IsSnow ? "ProjectileSnowPea.png" : "ProjectilePea.png", new Rect(PvZGame.GridX + pea.X * PvZGame.CellWidth - 13, PvZGame.GridY + row * PvZGame.CellHeight + 20, 26, 26));
                    }
                }

                foreach (LaneFlame flame in _owner._game.Flames)
                {
                    if (flame.Row != row)
                    {
                        continue;
                    }

                    for (var i = 0; i < 11; i++)
                    {
                        _owner._animations.Draw("fire", new Rect(PvZGame.GridX + i * 68f - 20f, PvZGame.GridY + row * PvZGame.CellHeight - 18f, 82f, 112f), flame.Age + i * 0.025f, "anim_flame");
                    }
                }
            }

            foreach (global::PlantsVsZombies.Simulation.Sun sun in _owner._game.Suns)
            {
                _owner._animations.Draw("Sun", PvZGame.SunRect(sun), sun.Age);
            }

            if (_owner._game.LevelOneUsesNativeSkySunRules)
            {
                foreach (LevelOneNativeSkySun sun in _owner._game.LevelOneNativeSkySuns)
                {
                    var previous = CanvasUI.Color;
                    try
                    {
                        if (sun.FadeTicksRemaining > 0)
                        {
                            CanvasUI.Color = new Color(previous.r, previous.g, previous.b, previous.a * sun.FadeTicksRemaining / 15f);
                        }

                        _owner._animations.Draw("Sun", new Rect(sun.PixelX, sun.PixelY, LevelOneNativeSkySun.BaseWidthPixels, LevelOneNativeSkySun.BaseHeightPixels), _owner._game.Elapsed);
                    }
                    finally
                    {
                        CanvasUI.Color = previous;
                    }
                }
            }
        }

        /// <summary>Performs draw interface.</summary>
        private void DrawInterface()
        {
            if (DrawModesInterface())
            {
                return;
            }

            if (_owner._game.IsAdventure)
            {
                DrawAdventureInterface();
                return;
            }

            DrawImage("SeedBank.png", new Rect(0, 0, 446, 87));
            if (_owner._game.IsExperimentalLevelOne)
            {
                var bank = _owner.Texture("SeedBank.png");
                var cap = FirstAdventureBoardUi.SeedBankEndCap(0, 0);
                if (bank != null)
                {
                    CanvasUI.DrawTextureWithTexCoords(new Rect(cap.Destination.X, cap.Destination.Y, cap.Destination.Width, cap.Destination.Height), bank, new Rect((float)cap.Source.X / bank.width, 0f, (float)cap.Source.Width / bank.width, 1f));
                }

                if (!DrawBitmap(_owner._sunCounterBitmap, FirstAdventureBoardUi.DisplayedSun(_owner._game.SunAmount).ToString(), 38f, 78f, 76f, Color.black, true, true))
                {
                    Label(new Rect(10, 59, 55, 22), NumberLabel(_owner._game.SunAmount), 18, Color.black, TextAnchor.MiddleCenter, true);
                }
            }
            else
            {
                Label(new Rect(10, 59, 55, 22), NumberLabel(_owner._game.SunAmount), 18, Color.black, TextAnchor.MiddleCenter, true);
            }

            var packets = _owner.Texture("seeds.png");
            for (var i = 0; i < _owner._deck.Count; i++)
            {
                var card = _owner.SeedRect(i);
                if (packets != null)
                {
                    CanvasUI.DrawTextureWithTexCoords(card, packets, new Rect(0, 0, 1f / 9f, 1));
                }

                DrawSeedIcon(_owner._deck[i], card);
                Label(new Rect(card.x + 1, card.y + 51, 46, 17), NumberLabel(GameSimulation.Cost(_owner._deck[i])), 13, Color.black, TextAnchor.MiddleRight, true);
                var fraction = _owner._game.CooldownRemaining(_owner._deck[i]) / GameSimulation.RechargeTime(_owner._deck[i]);
                if (fraction > 0f)
                {
                    Fill(new Rect(card.x, card.y, card.width, card.height * fraction), new Color(0, 0, 0, 0.55f));
                }
                else if (_owner._game.SunAmount < GameSimulation.Cost(_owner._deck[i]))
                {
                    Fill(card, new Color(0, 0, 0, 0.36f));
                }

                if (_owner._selected == i)
                {
                    Outline(card, new Color(1f, 0.91f, 0.35f), 3f);
                }
            }

            if (!_owner._game.IsExperimentalLevelOne)
            {
                DrawImage("ShovelBank.png", new Rect(522, 0, 70, 72));
                DrawImage("Shovel.png", new Rect(522, 0, 70, 70));
                if (_owner._shovel)
                {
                    Outline(new Rect(522, 0, 70, 72), new Color(1f, 0.91f, 0.35f), 3f);
                }

                Fill(new Rect(596, 6, 100, 34), new Color(0.28f, 0.18f, 0.09f, 0.9f));
                Label(new Rect(596, 6, 100, 34), "选植物", 17, Color.white, TextAnchor.MiddleCenter, true);
            }

            Fill(new Rect(704, 6, 90, 34), new Color(0.28f, 0.18f, 0.09f, 0.9f));
            Label(new Rect(704, 6, 90, 34), _owner._game.State == MatchState.Paused ? "继续" : "菜单", 17, Color.white, TextAnchor.MiddleCenter, true);
            var progress = _owner._game.IsExperimentalLevelOne ? _owner._game.LevelOneWaveProgress : _owner._game.Rules.ZombieCount == 0 ? 0 : (float)_owner._game.SpawnedZombies / _owner._game.Rules.ZombieCount;
            var meter = _owner.Texture("FlagMeter.png");
            if (meter != null)
            {
                var meterRect = new Rect(632, 570, 158, 27);
                CanvasUI.DrawTextureWithTexCoords(meterRect, meter, new Rect(0, 0.5f, 1, 0.5f));
                var fillWidth = 144f * Mathf.Clamp01(progress);
                if (fillWidth > 0f)
                {
                    var start = 151f - fillWidth;
                    CanvasUI.DrawTextureWithTexCoords(new Rect(meterRect.x + start, meterRect.y, fillWidth, meterRect.height), meter, new Rect(start / 158f, 0, fillWidth / 158f, 0.5f));
                }

                var parts = _owner.Texture("FlagMeterParts.png");
                if (parts != null)
                {
                    CanvasUI.DrawTextureWithTexCoords(new Rect(meterRect.x + 138f - 120f * progress, meterRect.y - 3, 25, 25), parts, new Rect(0, 0, 1f / 3f, 1));
                }
            }

            if (_owner._game.State == MatchState.Paused)
            {
                DrawDialog("已暂停", "继续游戏", true);
            }
            else if (_owner._game.State == MatchState.Won)
            {
                DrawDialog("你赢了！", "再玩一次", false);
            }
            else if (_owner._game.State == MatchState.Lost)
            {
                DrawDialog("僵尸吃掉了你的脑子！", "再试一次", false);
            }

            if (_owner._choosingSeeds)
            {
                DrawSeedChooser();
            }

            if (_owner._game.IsExperimentalLevelOne && _owner._game.State == MatchState.Playing)
            {
                DrawLevelOneAdvice();
            }
        }

        /// <summary>Performs draw level one advice.</summary>
        private void DrawLevelOneAdvice()
        {
            string message = null;
            switch (_owner._game.CurrentLevelOneTutorialStage)
            {
                case LevelOneTutorialStage.AwaitingSeedSelection:
                    message = _owner._game.Plants.Count == 0 ? "点击种子包，把它捡起来！" : "点击豌豆射手，再种一棵！";
                    break;
                case LevelOneTutorialStage.AwaitingPlantPlacement:
                    message = "点击草地种下你的种子！";
                    break;
                case LevelOneTutorialStage.AwaitingUsableSeed:
                    message = "干得漂亮！";
                    break;
                case LevelOneTutorialStage.ZombieOnslaughtPrompt:
                    message = "别让僵尸靠近你的房子！";
                    break;
            }

            if (message != null)
            {
                Label(new Rect(125, 520, 550, 35), message, 19, Color.white, TextAnchor.MiddleCenter, true);
            }
        }

        /// <summary>Performs draw seed chooser.</summary>
        private void DrawSeedChooser()
        {
            Fill(new Rect(0, 0, Width, Height), new Color(0, 0, 0, 0.58f));
            DrawImage("SeedChooser_Background.png", new Rect(168, 45, 465, 513));
            Label(new Rect(197, 83, 310, 34), "选择植物", 24, Color.white, TextAnchor.MiddleLeft, true);
            Label(new Rect(500, 83, 95, 34), _owner._pendingSeeds.Count + " / 7", 18, Color.white, TextAnchor.MiddleRight, true);
            var packets = _owner.Texture("seeds.png");
            for (var i = 0; i < _owner._availablePlants.Length; i++)
            {
                var card = PvZGame.ChooserSeedRect(i);
                if (packets != null)
                {
                    CanvasUI.DrawTextureWithTexCoords(card, packets, new Rect(0, 0, 1f / 9f, 1));
                }

                DrawSeedIcon(_owner._availablePlants[i], card);
                Label(new Rect(card.x + 1, card.y + 51, 46, 17), NumberLabel(GameSimulation.Cost(_owner._availablePlants[i])), 13, Color.black, TextAnchor.MiddleRight, true);
                if (_owner._pendingSeeds.Contains(_owner._availablePlants[i]))
                {
                    Outline(card, new Color(0.9f, 0.92f, 0.35f), 3);
                }
            }

            DrawImage(_owner._pendingSeeds.Count > 0 ? "SeedChooser_Button.png" : "SeedChooser_Button_Disabled.png", new Rect(322, 440, 156, 42));
            Label(new Rect(322, 440, 156, 42), "重选并开始", 17, Color.white, TextAnchor.MiddleCenter, true);
            DrawOriginalUiButton(new Rect(341, 493, 118, 30), "取消");
        }

        /// <summary>Performs draw seed icon.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "card">The card.</param>
        private void DrawSeedIcon(PlantKind kind, Rect card)
        {
            var icon = new Rect(card.x + 3, card.y + 8, 44, 40);
            if (kind == PlantKind.PotatoMine)
            {
                _owner._animations.Draw("PotatoMine", icon, _owner._game.Elapsed, "anim_armed");
            }
            else
            {
                _owner._animations.Draw(AnimationName(kind), icon, _owner._game.Elapsed);
            }
        }

        /// <summary>Performs draw dialog.</summary>
        /// <param name = "title">The title.</param>
        /// <param name = "button">The button.</param>
        /// <param name = "paused">The paused.</param>
        private void DrawDialog(string title, string button, bool paused)
        {
            DrawOriginalUiDialog(new Rect(200, 166, 400, 270), title, null);
            DrawOriginalUiButton(new Rect(316, paused ? 280 : 340, 168, 48), button);
            if (paused)
            {
                DrawOriginalUiButton(new Rect(316, 340, 168, 48), "重新开始");
            }
        }

        /// <summary>Performs animation name.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting animation name.</returns>
        private static string AnimationName(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.Sunflower:
                    return "SunFlower";
                case PlantKind.Wallnut:
                    return "Wallnut";
                case PlantKind.CherryBomb:
                    return "CherryBomb";
                case PlantKind.SnowPea:
                    return "SnowPea";
                case PlantKind.PotatoMine:
                    return "PotatoMine";
                case PlantKind.Repeater:
                    return "PeaShooter";
                case PlantKind.Jalapeno:
                    return "Jalapeno";
                case PlantKind.TallNut:
                    return "Tallnut";
                default:
                    return "PeaShooterSingle";
            }
        }

        /// <summary>Performs zombie appearance.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting zombie appearance.</returns>
        private static ZombieVariant ZombieAppearance(ZombieKind kind)
        {
            switch (kind)
            {
                case ZombieKind.Conehead:
                    return ZombieVariant.Conehead;
                case ZombieKind.Buckethead:
                    return ZombieVariant.Buckethead;
                case ZombieKind.Flag:
                    return ZombieVariant.Flag;
                case ZombieKind.ScreenDoor:
                    return ZombieVariant.ScreenDoor;
                default:
                    return ZombieVariant.Normal;
            }
        }

        /// <summary>Performs draw image.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "rect">The rect.</param>
        private void DrawImage(string name, Rect rect)
        {
            var texture = _owner.Texture(name);
            if (texture != null)
            {
                DrawTextureWithSource(rect, texture, new Rect(0, 0, 1, 1));
            }
        }

        /// <summary>Submits a texture to the active board frame or the surrounding interface.</summary>
        /// <param name = "destination">The destination in logical interface coordinates.</param>
        /// <param name = "texture">The texture to display.</param>
        /// <param name = "source">The normalized texture coordinates.</param>
        private void DrawTextureWithSource(Rect destination, Texture texture, Rect source)
        {
            if (_renderingBoardObjects)
            {
                _owner._animations.DrawNativeImage(texture, destination, source);
            }
            else
            {
                CanvasUI.DrawTextureWithTexCoords(destination, texture, source, true);
            }
        }

        /// <summary>Performs fill.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "color">The color.</param>
        private void Fill(Rect rect, Color color)
        {
            var previous = CanvasUI.Color;
            CanvasUI.Color = color;
            DrawTextureWithSource(rect, Texture2D.whiteTexture, new Rect(0, 0, 1, 1));
            CanvasUI.Color = previous;
        }

        /// <summary>Performs outline.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "color">The color.</param>
        /// <param name = "thickness">The thickness.</param>
        private void Outline(Rect rect, Color color, float thickness)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        /// <summary>Performs label.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "size">The size.</param>
        /// <param name = "color">The color.</param>
        /// <param name = "alignment">The alignment.</param>
        /// <param name = "bold">The bold.</param>
        private void Label(Rect rect, string text, int size, Color color, TextAnchor alignment, bool bold)
        {
            if (_label == null)
            {
                _label = new GUIStyle();
            }

            _label.font = _owner._font;
            _label.fontSize = size;
            _label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            _label.alignment = alignment;
            _label.normal.textColor = color;
            CanvasUI.Label(rect, text, _label);
        }
        // Hook after the existing plantshadow and origin/size calculation, before generic
        // plant playback. No interpolated scale/pulse: every pose is the original reanim.
        /// <summary>Performs draw adventure cherry bomb.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <returns>The resulting draw adventure cherry bomb.</returns>
        private bool DrawAdventureCherryBomb(Plant plant, Vector2 origin, float scale)
        {
            if (plant.Kind != PlantKind.CherryBomb)
            {
                return false;
            }

            var state = plant.CherryBombPresentation;
            var age = state == null ? 0f : state.SampleSeconds(AdventureRenderRemainder);
            var rate = state == null ? 10f : state.Rate;
            if (state != null)
            {
                origin += new Vector2(state.ShakeX, state.ShakeY);
            }

            RegisterPlantSurface(plant);
            ActorRenderer.DrawPlant(plant, "CherryBomb", origin, scale, age, AdventureCherryBombState.Animation, false, rate,
                null, _presentationKeys.Get("plant-", plant.Id), new Rect(origin.x - 80, origin.y - 160, 300, 340), 0, AdventureRenderRemainder, _presentationKeys.Get("producer-plant-", plant.Id));

            return true;
        }
        /// <summary>Performs draw adventure seed bank background.</summary>
        /// <param name = "origin">The origin.</param>
        /// <param name = "count">The count.</param>
        private void DrawAdventureSeedBankBackground(Vector2 origin, int count)
        {
            var image = _owner.Texture("SeedBank.png");
            if (image == null)
            {
                return;
            }

            CanvasUI.DrawTexture(new Rect(origin.x, origin.y, image.width, image.height), image);
            var extra = AdventureSeedSlotLayout.BackgroundExtra(count);
            if (extra <= 0)
            {
                return;
            }

            // PC 0x49F100 copies the original right-hand strip; it does not stretch the panel.
            CanvasUI.DrawTextureWithTexCoords(new Rect(origin.x + image.width - 12, origin.y, extra + 12, image.height), image, new Rect((image.width - extra - 12f) / image.width, 0, (extra + 12f) / image.width, 1));
        }

        /// <summary>Performs draw adventure conveyor background.</summary>
        private void DrawAdventureConveyorBackground()
        {
            var backdrop = _owner.Texture("ConveyorBelt_backdrop.png");
            if (backdrop != null)
            {
                CanvasUI.DrawTexture(new Rect(83, 0, backdrop.width, backdrop.height), backdrop);
            }

            var belt = _owner.Texture("ConveyorBelt.png");
            if (belt == null)
            {
                return;
            }

            var row = _owner._game.Adventure.ConveyorTicks / 4 % 6;
            CanvasUI.DrawTextureWithTexCoords(new Rect(90, 63, belt.width, belt.height / 6f), belt, new Rect(0, 1f - (row + 1) / 6f, 1, 1f / 6f));
        }

        /// <summary>Performs draw adventure conveyor packets.</summary>
        /// <param name = "showBackwards">The show backwards.</param>
        private void DrawAdventureConveyorPackets(bool showBackwards = false)
        {
            _showBackwardsPackets = showBackwards;
            if (_owner._game.UsesNativeAdventureConveyor)
            {
                _owner._animations.DrawNativeClipped("native-conveyor-packets", PvZGame.s_nativeConveyorClip, _drawConveyorPackets);
            }
            else
            {
                DrawAdventureConveyorPacketContents();
            }
        }

        /// <summary>Draws the currently configured conveyor contents synchronously.</summary>
        /// <returns>Whether the conveyor content traversal completed.</returns>
        private bool DrawAdventureConveyorPacketContents()
        {
            for (var i = 0; i < _owner._game.Adventure.Packets.Count && i < 10; i++)
            {
                var packet = _owner._game.Adventure.Packets[i];
                var card = _owner.AdventureSeedRect(i, true);
                DrawAdventureConveyorPacket(packet, card);
                if (_showBackwardsPackets && _owner._modeSession != null && _owner._modeSession._backwardPackets.Contains(packet.Id))
                {
                    Label(new Rect(card.x, card.y + 45, card.width, 20), "←", 18, Color.yellow, TextAnchor.MiddleCenter, true);
                }
            }

            return true;
        }

        /// <summary>Performs try get adventure cursor plant.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting try get adventure cursor plant.</returns>
        private bool TryGetAdventureCursorPlant(out PlantKind kind)
        {
            kind = PlantKind.Peashooter;
            if (_owner._game == null || !_owner._game.IsAdventure || _owner._game.State != MatchState.Playing || _owner._choosingSeeds || _owner.AdventureIntroActive || _owner._shovel || _owner._game.Adventure.SelectedCobCannonId.HasValue || _owner._optionsView.IsOpen || _owner._confirmingQuit || _owner._unavailableFeature != null || !new Rect(0, 0, 800, 600).Contains(_owner._adventurePointer))
            {
                return false;
            }

            if (_owner._selectedAdventurePacket >= 0)
            {
                foreach (AdventurePacket packet in _owner._game.Adventure.Packets)
                {
                    if (packet.Id == _owner._selectedAdventurePacket)
                    {
                        kind = packet.Kind;
                        return true;
                    }
                }

                return false;
            }

            if (_owner._selected < 0 || _owner._selected >= _owner._deck.Count)
            {
                return false;
            }

            kind = _owner._deck[_owner._selected];
            return true;
        }

        /// <summary>Performs draw adventure plant preview.</summary>
        /// <param name = "row">The row.</param>
        private void DrawAdventurePlantPreview(int row)
        {
            PlantKind kind;
            if (!TryGetAdventureCursorPlant(out kind))
            {
                return;
            }

            var column = Mathf.FloorToInt((_owner._adventurePointer.x - PvZGame.AdventureGridX) / PvZGame.CellWidth);
            var targetRow = Mathf.FloorToInt((_owner._adventurePointer.y - PvZGame.AdventureGridY - _owner.AdventureSlope(column)) / _owner.AdventurePitch);
            if (column < 0 || column > 8 || targetRow != row || targetRow < 0 || targetRow >= _owner._game.RowCount || _owner._game.CanPlantAdventure(kind, row, column) != AdventurePlantingFailure.None)
            {
                return;
            }

            var resolved = kind == PlantKind.Imitater && _owner._game.Adventure.ImitaterTarget.HasValue ? _owner._game.Adventure.ImitaterTarget.Value : kind;
            var heightProbe = _placementProbe;
            heightProbe.Kind = resolved;
            heightProbe.Row = row;
            heightProbe.Column = column;
            var origin = new Vector2(PvZGame.AdventureGridX + column * PvZGame.CellWidth, _owner.AdventureCellY(row, column) + AdventurePlantHeight(heightProbe));
            // PC CursorPreview::Draw 0x444800 uses white with alpha100 after native CanPlantAt succeeds.
            DrawAdventureCursorPlant(kind, origin, 100f / 255f, "cursor-preview");
        }

        /// <summary>Performs draw adventure plant cursor.</summary>
        private void DrawAdventurePlantCursor()
        {
            PlantKind kind;
            if (!TryGetAdventureCursorPlant(out kind))
            {
                return;
            }

            var resolved = kind == PlantKind.Imitater && _owner._game.Adventure.ImitaterTarget.HasValue ? _owner._game.Adventure.ImitaterTarget.Value : kind;
            var height = (int)resolved < 49 ? AlmanacPortraitLayout.PlantHeightOffset((int)resolved) : 0f;
            // PC CursorObject::Update 0x444100 offsets the mouse by(-25,-35).
            // Draw 0x4441A0 adds(-10,height-25), with another+30 for flowerpot/puffshroom.
            if (resolved == PlantKind.FlowerPot || resolved == PlantKind.PuffShroom)
            {
                height += 30f;
            }

            DrawAdventureCursorPlant(kind, new Vector2(_owner._adventurePointer.x - 35f, _owner._adventurePointer.y - 60f + height), 1f, "cursor-held");
        }

        /// <summary>Performs draw adventure cursor plant.</summary>
        /// <param name = "seed">The seed.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "alpha">The alpha.</param>
        /// <param name = "key">The key.</param>
        private void DrawAdventureCursorPlant(PlantKind seed, Vector2 origin, float alpha, string key)
        {
            var imitated = seed == PlantKind.Imitater && _owner._game.Adventure.ImitaterTarget.HasValue;
            var kind = imitated ? _owner._game.Adventure.ImitaterTarget.Value : seed;
            var name = AdventurePlantAnimation(kind);
            var nativeScale = 1f;
            if (kind == PlantKind.PotatoMine || kind == PlantKind.FlowerPot)
            {
                nativeScale = .8f;
                origin += new Vector2(12, 12);
            }

            var clip = kind == PlantKind.PotatoMine ? "anim_armed" : null;
            var previous = CanvasUI.Color;
            try
            {
                var explosive = kind == PlantKind.ExplodeONut;
                var greenBlue = explosive ? 64f / 255f : 1f;
                CanvasUI.Color = new Color(previous.r, previous.g * greenBlue, previous.b * greenBlue, previous.a * alpha);
                // Plant::DrawSeedType uses the cached, stationary native reanimation pose.
                ActorRenderer.DrawCursor(name, origin, nativeScale, clip, kind == PlantKind.KernelPult, imitated, key,
                    new Rect(origin.x - 80, origin.y - 160, 320, 340));
            }
            finally
            {
                CanvasUI.Color = previous;
            }
        }

        // Rendering.cs owner calls after DrawAdventureImpacts(row).
        /// <summary>Performs draw adventure explosion row.</summary>
        /// <param name = "row">The row.</param>
        private void DrawAdventureExplosionRow(int row)
        {
            if (_adventureParticles == null)
            {
                return;
            }

            foreach (var explosion in _owner._game.Presentation.Explosions)
            {
                if (!explosion.Overlay && explosion.Row == row)
                {
                    DrawAdventureExplosion(explosion);
                }
            }

            foreach (var fire in _owner._game.Presentation.JalapenoFires)
            {
                if (fire.Row != row)
                {
                    continue;
                }

                foreach (var flame in fire.Flames)
                {
                    var done = flame.DoneTick >= 0;
                    var seconds = ((done ? fire.Ticks - flame.DoneTick : fire.Ticks) * .01f) + AdventureRenderRemainder;
                    if (done && seconds >= 10f / 15f)
                    {
                        continue;
                    }

                    var origin = new Vector2(flame.X, _owner.AdventureCellY(row, (flame.X + 10f - PvZGame.AdventureGridX) / PvZGame.CellWidth) - 10f);
                    _owner._animations.DrawNativeFullLastFrame("fire", origin, flame.ScaleX, seconds, done ? "anim_done" : "anim_flame", !done, done ? 15f : flame.Rate);
                }
            }
        }

        // Rendering.cs owner calls after all rows, before the Sun pass (PC layer 400000).
        /// <summary>Performs draw adventure explosion overlay.</summary>
        private void DrawAdventureExplosionOverlay()
        {
            if (_adventureParticles == null)
            {
                return;
            }

            foreach (var explosion in _owner._game.Presentation.Explosions)
            {
                if (explosion.Overlay)
                {
                    DrawAdventureExplosion(explosion);
                }
            }
        }

        /// <summary>Performs draw adventure explosion.</summary>
        /// <param name = "explosion">The explosion.</param>
        private void DrawAdventureExplosion(AdventureExplosion explosion)
        {
            var point = new Vector2(PvZGame.AdventureGridX + explosion.X * PvZGame.CellWidth, _owner.AdventureCellY(explosion.Row, explosion.X - .5f) + 40f);
            _adventureParticles.Draw("native-explosion-" + explosion.Id, explosion.Particle, explosion.Ticks * .01f + AdventureRenderRemainder, point, explosion.Seed);
        }

        /// <summary>Performs draw adventure snow trails.</summary>
        /// <param name = "row">The row.</param>
        private void DrawAdventureSnowTrails(int row)
        {
            foreach (var trail in _owner._game.Presentation.SnowTrails)
            {
                if (trail.Row != row)
                {
                    continue;
                }

                var fraction = Mathf.Clamp01(AdventureRenderRemainder * 100f);
                var origin = trail.Stopped ? new Vector2(trail.X, trail.Y) : new Vector2(Mathf.Lerp(trail.PreviousX, trail.X, fraction), Mathf.Lerp(trail.PreviousY, trail.Y, fraction));
                _adventureParticles.Draw("snow-trail-" + trail.Id, "SnowPeaTrail", trail.Age + AdventureRenderRemainder, origin, trail.Seed, 1f, trail.StopAge);
            }
        }

        /// <summary>Performs draw adventure opening world.</summary>
        private void DrawAdventureOpeningWorld()
        {
            if (_owner._adventureIntroFrame == null)
            {
                return;
            }

            _owner._adventureIntroDrawn = true;

            foreach (var blit in _owner._adventureIntroFrame.SodBlits)
            {
                if (blit.Width <= 0 || blit.Height <= 0)
                {
                    continue;
                }

                var image = _owner.Texture(blit.Resource + (blit.Resource.StartsWith("background", StringComparison.Ordinal) ? ".jpg" : ".png"));
                if (image == null)
                {
                    continue;
                }

                CanvasUI.DrawTextureWithTexCoords(new Rect(blit.X, blit.Y, blit.Width, blit.Height), image, new Rect(blit.SourceX / (float)image.width, 1f - (blit.SourceY + blit.Height) / (float)image.height, blit.Width / (float)image.width, blit.Height / (float)image.height));
            }
        }

        /// <summary>Performs draw adventure opening actors.</summary>
        private void DrawAdventureOpeningActors()
        {
            if (!_owner.AdventureIntroActive || _owner._adventureIntroFrame == null)
            {
                return;
            }

            for (var previewIndex = 0; previewIndex < _owner._adventurePreview.Count; previewIndex++)
            {
                var preview = _owner._adventurePreview[previewIndex];
                if (preview.NativeKind < 0 || preview.NativeKind >= 26)
                {
                    continue;
                }

                while (_openingZombiePoses.Count <= previewIndex)
                {
                    _openingZombiePoses.Add(new Zombie());
                }

                var zombie = _openingZombiePoses[previewIndex];
                zombie.Id = -1000 - previewIndex;
                zombie.Kind = (ZombieKind)preview.NativeKind;
                zombie.Health = 270;
                zombie.StartingHealth = 270;
                zombie.ArmorHealth = preview.NativeKind == 2 || preview.NativeKind == 4 ? 100 : 0;
                zombie.ShieldHealth = preview.NativeKind == 5 || preview.NativeKind == 6 || preview.NativeKind == 21 ? 100 : 0;
                zombie.IsSmall = preview.Scale < 1f;
                string asset = PvZGame.AdventureZombieAnimation(zombie.Kind), clip = preview.Animation;
                if (!_owner.AdventureClipExists(asset, clip))
                {
                    clip = _owner.AdventureClipExists(asset, "anim_idle") ? "anim_idle" : "anim_walk";
                }

                global::PlantsVsZombies.Reanimation.NativeAnimationInfo info;
                var seconds = _owner._adventureIntroVisualAge;
                if (_owner._animations.TryGetNativeAnimationInfo(asset, clip, out info) && preview.AnimationRate > 0)
                {
                    seconds += preview.InitialPhase * info.Count / preview.AnimationRate;
                }

                DrawAdventureZombiePose(zombie, asset, clip, new Vector2(preview.WorldX, preview.WorldY - preview.Altitude), preview.Scale, seconds, preview.AnimationRate, preview.Loop);
            }

            foreach (var animation in _owner._adventureIntroFrame.Animations)
            {
                if (animation.Asset != "StartReadySetPlant")
                {
                    _owner._animations.DrawNativeDefinition(animation.Asset, new Vector2(animation.X, animation.Y), 1f, animation.Seconds, false, animation.Rate);
                }
            }

            foreach (var particle in _owner._adventureIntroFrame.Particles)
            {
                _adventureParticles.Draw("opening-" + particle.Asset + "-" + particle.Y, particle.Asset, particle.Seconds, new Vector2(particle.X, particle.Y), (int)particle.Y + 137);
            }
        }

        /// <summary>Performs draw adventure opening interface.</summary>
        /// <returns>The resulting draw adventure opening interface.</returns>
        private bool DrawAdventureOpeningInterface()
        {
            if (!_owner.AdventureIntroActive || _owner._adventureIntroFrame == null)
            {
                return false;
            }

            var frame = _owner._adventureIntroFrame;
            _owner._adventureIntroDrawn = true;

            if (_owner._adventureIntro.Plan.DrawSeedBank && frame.SeedBankY > -87)
            {
                DrawAdventureSeedBankBackground(new Vector2(frame.SeedBankX, frame.SeedBankY), _owner._deck.Count);
                Label(new Rect(frame.SeedBankX + 8, frame.SeedBankY + 59, 60, 22), NumberLabel(_owner._game.SunAmount), 18, Color.black, TextAnchor.MiddleCenter, true);
                var selectedSeeds = _openingSelectedSeeds;
                selectedSeeds.Clear();
                var availableSeeds = _owner._game.Adventure.AvailableSeeds;
                for (var seedIndex = 0; seedIndex < availableSeeds.Count; seedIndex++)
                {
                    var seed = availableSeeds[seedIndex];
                    if (_owner._pendingSeeds.Contains(seed))
                    {
                        selectedSeeds.Add(seed);
                    }
                }

                if (!_owner._adventureIntro.Plan.ChooseSeeds)
                {
                    selectedSeeds = _owner._deck;
                }

                for (var i = 0; i < selectedSeeds.Count; i++)
                {
                    var card = new Rect(AdventureSeedSlotLayout.PositionX(i, Math.Max(_owner._deck.Count, selectedSeeds.Count)) + frame.SeedBankX, 8 + frame.SeedBankY, 50, 70);
                    var old = CanvasUI.Color;
                    var shade = frame.SeedBankDarken / 255f;
                    CanvasUI.Color = new Color(old.r * shade, old.g * shade, old.b * shade, old.a);
                    DrawAdventureCard(selectedSeeds[i], card, false);
                    CanvasUI.Color = old;
                    DrawAdventurePacketCost(selectedSeeds[i], card);
                }
            }

            if (frame.ChooserVisible)
            {
                var prior = CanvasUI.Matrix;
                try
                {
                    CanvasUI.Matrix = prior * Matrix4x4.Translate(new Vector3(0, frame.ChooserY, 0));
                    DrawAdventureChooser();
                }
                finally
                {
                    CanvasUI.Matrix = prior;
                }
            }

            foreach (var animation in frame.Animations)
            {
                if (animation.Asset == "StartReadySetPlant")
                {
                    _owner._animations.DrawNativeDefinition(animation.Asset, new Vector2(animation.X, animation.Y), 1f, animation.Seconds, false, animation.Rate);
                }
            }

            return true;
        }
        /// <summary>Performs draw adventure potato mine.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        /// <returns>The resulting draw adventure potato mine.</returns>
        private bool DrawAdventurePotatoMine(Plant plant, Vector2 origin, float scale)
        {
            if (plant.Kind != PlantKind.PotatoMine)
            {
                return false;
            }

            var state = plant.PotatoMinePresentation;
            var clip = plant.MinePhase == MinePhase.Buried ? "anim_idle" : plant.MinePhase == MinePhase.Rising ? "anim_rise" : "anim_armed";
            var rate = state == null ? 12f : state.BodyRate;
            var age = plant.MinePhase == MinePhase.Buried ? 0f : plant.PhaseAge + AdventureRenderRemainder;
            // PC UpdateReanim 471D60: translation (+12,+12), scale (.8,.8) for type 4.
            var nativeOrigin = origin + new Vector2(12f * scale, 12f * scale);
            var nativeScale = .8f * scale;
            RegisterPlantSurface(plant);
            ActorRenderer.DrawPlant(plant, "PotatoMine", nativeOrigin, nativeScale, age, clip, plant.MinePhase != MinePhase.Rising,
                rate, null, _presentationKeys.Get("plant-", plant.Id), new Rect(origin.x - 80, origin.y - 160, 300, 340),
                plant.MinePhase == MinePhase.Rising ? 20 : 0, AdventureRenderRemainder, _presentationKeys.Get("producer-plant-", plant.Id));

            return true;
        }

        /// <summary>Performs adventure plant animation.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure plant animation.</returns>
        private static string AdventurePlantAnimation(PlantKind kind)
        {
            if (s_plantAnimationNames.TryGetValue(kind, out var cached))
            {
                return cached;
            }

            var id = (int)kind;
            if (kind == PlantKind.ExplodeONut)
            {
                return "Wallnut";
            }

            if (id < 0 || id >= VerifiedDataCatalog.Plants.Count)
            {
                return "PeaShooterSingle";
            }

            ReanimationDefinition definition;
            var name = VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Plants[id].ReanimationId, out definition) ? Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/')) : "PeaShooterSingle";
            s_plantAnimationNames.Add(kind, name);
            return name;
        }

        /// <summary>Performs draw adventure board.</summary>
        private void DrawAdventureBoard()
        {
            BeginAdventurePresentation();
            var openingMatrix = CanvasUI.Matrix;
            if (_owner.AdventureIntroActive)
            {
                CanvasUI.Matrix = openingMatrix * Matrix4x4.Translate(new Vector3(_owner._adventureIntroFrame.BoardX, _owner._adventureIntroFrame.BoardY, 0));
            }

            try
            {
                var level = _owner._game.Adventure.Level;
                var background = level.Terrain == AdventureTerrain.Night ? "background2.jpg" : level.Terrain == AdventureTerrain.Pool ? "background3.jpg" : level.Terrain == AdventureTerrain.Fog ? "background4.jpg" : level.Terrain == AdventureTerrain.Roof ? "background5.jpg" : level.Terrain == AdventureTerrain.BossRoof ? "background6boss.jpg" : "background1.jpg";
                if (_owner._adventureIntroFrame != null && !level.IsReplay && level.Number <= 4)
                {
                    DrawImage("background1unsodded.jpg", new Rect(-220, 0, 1400, 600));
                    DrawAdventureOpeningWorld();
                }
                else if (level.Number <= 3 && !level.IsReplay)
                {
                    DrawImage("background1unsodded.jpg", new Rect(-220, 0, 1400, 600));
                    if (level.Number == 1)
                    {
                        DrawImage("sod1row.png", new Rect(19, 265, 771, 127));
                    }
                    else
                    {
                        DrawImage("sod3row.png", new Rect(15, 149, 771, 355));
                    }
                }
                else
                {
                    DrawImage(background, new Rect(-220, 0, 1400, 600));
                }

                if (level.HasPool)
                {
                    DrawImage(level.IsNight ? "pool_night.jpg" : "pool.jpg", new Rect(30, 279, 720, 159));
                }

                foreach (var crater in _owner._game.Adventure.Craters)
                {
                    DrawImage(level.HasRoof ? "crater_roof_center.png" : level.HasPool && level.IsWaterRow(crater.Row) ? level.IsNight ? "crater_water_night.png" : "crater_water_day.png" : "crater.png", new Rect(PvZGame.AdventureGridX + crater.Column * PvZGame.CellWidth - 24, _owner.AdventureCellY(crater.Row, crater.Column) + 45, 128, 44));
                }

                foreach (var ice in _owner._game.Adventure.IceTrails)
                {
                    DrawImage("ice.png", new Rect(PvZGame.AdventureGridX + ice.Column * PvZGame.CellWidth, _owner.AdventureCellY(ice.Row, ice.Column) + 44, 84, 42));
                }

                foreach (var grave in _owner._game.Adventure.Graves)
                {
                    var image = _owner.Texture("Tombstones.png");
                    if (image != null)
                    {
                        DrawTextureWithSource(new Rect(PvZGame.AdventureGridX + grave.Column * PvZGame.CellWidth, _owner.AdventureCellY(grave.Row, grave.Column), 80, 91), image, new Rect(0, 0.75f, 0.2f, 0.25f));
                    }
                }

                if (level.Mode == AdventureMode.Bowling)
                {
                    Fill(new Rect(278, 90, 4, 475), new Color(0.9f, 0.08f, 0.04f));
                }

                if (level.Mode == AdventureMode.Boss)
                {
                    _owner._animations.DrawBossNative(new Vector2(0, 0), 1f, _owner._game.Elapsed, _owner._game.Adventure.BossHeadVulnerable);
                }

                for (var row = 0; row < _owner._game.RowCount; row++)
                {
                    if (_owner._game.Adventure.RakeRow == row)
                    {
                        _owner._animations.DrawNativePlayback("Rake", new Vector2(PvZGame.AdventureGridX + _owner._game.Adventure.RakeColumn * PvZGame.CellWidth + 20, _owner.AdventureCellY(row, _owner._game.Adventure.RakeColumn)), 1f, _owner._game.Adventure.RakeTriggered ? _owner._game.Adventure.RakeAge + AdventureRenderRemainder : 0f, null, false, 20f);
                    }

                    foreach (Mower mower in _owner._game.Mowers)
                    {
                        if (mower.Row != row || !mower.Available && !mower.Active)
                        {
                            continue;
                        }

                        if (!_owner.AdventureIntroActive || _owner._adventureIntroFrame.MowerX[mower.Row] > -80)
                        {
                            DrawAdventureMower(mower);
                        }
                    }

                    for (var layer = 0; layer <= (int)PlantLayer.Effect; layer++)
                    {
                        foreach (Plant plant in _owner._game.Plants)
                        {
                            if (plant.Row == row && (int)plant.Layer == layer)
                            {
                                DrawAdventurePlant(plant);
                            }
                        }
                    }

                    DrawAdventurePlantPreview(row);
                    foreach (var vase in _owner._game.Adventure.Vases)
                    {
                        if (vase.Row != row)
                        {
                            continue;
                        }

                        var image = _owner.Texture("Scary_Pot.png");
                        if (image != null)
                        {
                            DrawTextureWithSource(new Rect(PvZGame.AdventureGridX + vase.Column * PvZGame.CellWidth, _owner.AdventureCellY(row, vase.Column) - 6, 80, 101), image, new Rect(vase.ShowsPlant ? 1f / 3f : 0, 0, 1f / 3f, 0.5f));
                        }
                    }

                    DrawAdventureDetachedZombieParts(row, true);
                    DrawAdventureCorpses(row);
                    foreach (Zombie zombie in _owner._game.Zombies)
                    {
                        if (zombie.Row == row)
                        {
                            DrawAdventureZombie(zombie);
                        }
                    }

                    DrawAdventureDetachedArms(row);
                    DrawAdventureDetachedZombieParts(row, false);
                    DrawAdventurePlantBiteEffects(row);
                    DrawAdventureImpacts(row);
                    DrawAdventureExplosionRow(row);
                    foreach (Pea pea in _owner._game.Peas)
                    {
                        if (pea.Row != row)
                        {
                            continue;
                        }

                        if (pea.FlightRow < 0 || pea.FlightRow >= _owner._game.RowCount)
                        {
                            continue;
                        }

                        DrawAdventureProjectile(pea);
                    }

                    DrawAdventureSnowTrails(row);
                    foreach (var nut in _owner._game.Adventure.BowlingNuts)
                    {
                        if (nut.Row != row)
                        {
                            continue;
                        }

                        var previous = CanvasUI.Color;
                        if (nut.Explosive)
                        {
                            CanvasUI.Color = new Color(1f, 64f / 255f, 64f / 255f);
                        }

                        var giant = _owner._game.ModeSession != null && _owner._game.ModeSession._giantNuts.Contains(nut);
                        AdventureBowlingPresentation pose;
                        _owner._game.Presentation.Bowling.TryGetValue(nut, out pose);
                        _owner._animations.DrawNativePlayback("Wallnut", new Vector2((nut.NativeMotionInitialized ? nut.NativePixelX : PvZGame.AdventureGridX + nut.X * PvZGame.CellWidth - 40) + (giant ? -76 : 0), (nut.NativeMotionInitialized ? nut.NativePixelY : _owner.AdventureCellY(row, nut.X)) + (giant ? -64 : 0)), giant ? 2f : 1f, (pose == null ? _owner._game.Elapsed : pose.Age) + AdventureRenderRemainder, "_ground", true, pose == null ? 12 : pose.Rate);
                        CanvasUI.Color = previous;
                    }

                    foreach (var ball in _owner._game.Adventure.BossBalls)
                    {
                        if (ball.Row == row)
                        {
                            _owner._animations.Draw(ball.IsIce ? "Zombie_boss_iceball" : "Zombie_boss_fireball", new Rect(PvZGame.AdventureGridX + ball.X * PvZGame.CellWidth - 40, _owner.AdventureCellY(row, ball.X) - 2, 80, 80), _owner._game.Elapsed, "anim_role");
                        }
                    }

                    foreach (LaneFlame flame in _owner._game.Flames)
                    {
                        if (flame.Row == row)
                        {
                            for (var c = 0; c < 10; c++)
                            {
                                _owner._animations.Draw("fire", new Rect(PvZGame.AdventureGridX + c * 76, _owner.AdventureCellY(row, c) - 10, 84, 108), flame.Age + c * 0.025f, "anim_flame");
                            }
                        }
                    }
                }

                DrawAdventureExplosionOverlay();
                foreach (global::PlantsVsZombies.Simulation.Sun sun in _owner._game.Suns)
                {
                    if (!sun.PresentationNative || !sun.PresentationPickup.Collecting)
                    {
                        DrawAdventureSun(sun);
                    }
                }

                foreach (var coin in _owner._game.Adventure.Coins)
                {
                    var coinRect = _owner.AdventureCoinRect(coin);
                    if (coin.Kind == AdventureCoinKind.PlantGift || coin.Kind == AdventureCoinKind.Chocolate || coin.Kind == AdventureCoinKind.MinigameUnlock || coin.Kind == AdventureCoinKind.PuzzleUnlock)
                    {
                        var image = coin.Kind == AdventureCoinKind.Chocolate ? "chocolate.png" : "Present.png";
                        var gift = _owner.Texture(image);
                        if (gift != null)
                        {
                            DrawImage(image, new Rect(coinRect.center.x - gift.width * .5f, coinRect.center.y - gift.height * .5f, gift.width, gift.height));
                        }

                        continue;
                    }

                    _owner._animations.DrawNative(coin.Kind == AdventureCoinKind.Diamond ? "Diamond" : coin.Kind == AdventureCoinKind.Gold ? "Coin_gold" : "Coin_silver", new Vector2(coinRect.x, coinRect.y), 1f, coin.Age);
                }

                foreach (var shell in _owner._game.Adventure.CobShells)
                {
                    var ticks = Math.Max(0, GameSimulation.AdventureCobFlightSeconds - shell.FlightRemaining + AdventureRenderRemainder) * 100f;
                    var rising = ticks < 88f;
                    var x = rising ? shell.LaunchX : shell.TargetX;
                    var row = rising ? shell.LaunchRow : shell.TargetRow;
                    var z = rising ? -8f * ticks : -704f + 8f * (ticks - 88f);
                    _owner._animations.DrawResourceSprite("IMAGE_REANIM_COBCANNON_COB", new Vector2(PvZGame.AdventureGridX + x * PvZGame.CellWidth, PvZGame.AdventureGridY + row * _owner.AdventurePitch + _owner.AdventureSlope(x) + z), 1f, rising ? 90 : -90);
                }

                if (level.Terrain == AdventureTerrain.Fog && _owner._game.Adventure.FogBlownRemaining <= 0)
                {
                    for (var row = 0; row < _owner._game.RowCount; row++)
                    {
                        for (var col = 0; col < 9; col++)
                        {
                            if (!_owner._game.IsAdventureCellVisible(row, col))
                            {
                                var fog = _owner.Texture("fog.png");
                                if (fog != null)
                                {
                                    DrawTextureWithSource(new Rect(PvZGame.AdventureGridX + col * PvZGame.CellWidth - 55, _owner.AdventureCellY(row, col) - 35, 190, 170), fog, new Rect((col % 8) / 8f, 0, 1f / 8f, 1));
                                }
                            }
                        }
                    }
                }

                if (level.Mode == AdventureMode.Storm && !_owner._game.Adventure.StormFlash)
                {
                    Fill(new Rect(0, 80, 800, 520), new Color(0.01f, 0.02f, 0.08f, 0.72f));
                }

                DrawAdventureOpeningActors();
            }
            finally
            {
                CanvasUI.Matrix = openingMatrix;
                EndAdventurePresentation();
            }
        }

        /// <summary>Performs draw adventure reanim image.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "destination">The destination.</param>
        private void DrawAdventureReanimImage(string name, Rect destination)
        {
            DrawAdventureFileImage("reanim/" + name, destination);
        }

        /// <summary>Performs draw adventure file image.</summary>
        /// <param name = "relative">The relative.</param>
        /// <param name = "destination">The destination.</param>
        private void DrawAdventureFileImage(string relative, Rect destination)
        {
            Texture2D texture;
            if (!_owner._textures.TryGetValue(relative, out texture))
            {
                var file = Path.Combine(_owner._root, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(file))
                {
                    _owner._textures[relative] = null;
                    return;
                }

                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(file)))
                {
                    UnityEngine.Object.Destroy(texture);
                    texture = null;
                }
                else
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                }

                _owner._textures[relative] = texture;
            }

            if (texture != null)
            {
                var fit = Mathf.Min(destination.width / texture.width, destination.height / texture.height);
                var fitted = new Rect(destination.center.x - texture.width * fit * 0.5f, destination.center.y - texture.height * fit * 0.5f, texture.width * fit, texture.height * fit);
                DrawTextureWithSource(fitted, texture, new Rect(0, 0, 1, 1));
            }
        }

        /// <summary>Stores adventure particles.</summary>
        private OriginalParticleRenderer _adventureParticles;
        /// <summary>Stores adventure presented game.</summary>
        private GameSimulation _adventurePresentedGame;
        /// <summary>Stores adventure presentation surfaces.</summary>
        private readonly HashSet<string> _adventurePresentationSurfaces = new HashSet<string>();
        /// <summary>Stores adventure presentation live surfaces.</summary>
        private readonly HashSet<string> _adventurePresentationLiveSurfaces = new HashSet<string>();
        /// <summary>Performs begin adventure presentation.</summary>
        private void BeginAdventurePresentation()
        {
            if (!ReferenceEquals(_adventurePresentedGame, _owner._game))
            {
                if (_adventureParticles != null)
                {
                    _adventureParticles.Clear();
                }

                foreach (string key in _adventurePresentationSurfaces)
                {
                    _owner._animations.ReleaseNativeSurface(key);
                }

                _adventurePresentationSurfaces.Clear();
                _owner._animations.ResetNativePlayback();
                _adventurePresentedGame = _owner._game;
                _adventureParticles = new OriginalParticleRenderer(_owner._root, _owner._animations);
            }

            _adventurePresentationLiveSurfaces.Clear();
            _adventureParticles.BeginFrame();
        }

        /// <summary>Performs end adventure presentation.</summary>
        private void EndAdventurePresentation()
        {
            _adventureParticles.EndFrame();
            var expired = _expiredSurfaces;
            expired.Clear();
            foreach (string key in _adventurePresentationSurfaces)
            {
                if (!_adventurePresentationLiveSurfaces.Contains(key))
                {
                    expired.Add(key);
                }
            }

            foreach (string key in expired)
            {
                _owner._animations.ReleaseNativeSurface(key);
                _adventurePresentationSurfaces.Remove(key);
            }
        }

        /// <summary>Performs draw adventure surface.</summary>
        /// <param name = "key">The key.</param>
        /// <param name = "viewport">The viewport.</param>
        /// <param name = "draw">The draw.</param>
        /// <param name = "grayscale">The grayscale.</param>
        /// <param name = "additive">The additive.</param>
        /// <returns>The resulting draw adventure surface.</returns>
        private bool DrawAdventureSurface(string key, Rect viewport, Func<bool> draw, bool grayscale = false, float additive = 0f)
        {
            _adventurePresentationSurfaces.Add(key);
            _adventurePresentationLiveSurfaces.Add(key);
            return _owner._animations.DrawNativeClipped(key, viewport, draw, false, grayscale, additive);
        }

        /// <summary>Gets or sets adventure render remainder.</summary>
        private float AdventureRenderRemainder
        {
            get
            {
                return _owner._game.AdventurePresentationRemainder;
            }
        }

        /// <summary>Performs adventure render x.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <returns>The resulting adventure render x.</returns>
        private float AdventureRenderX(Zombie zombie)
        {
            return zombie._presentationInitialized ? Mathf.Lerp(zombie.PresentationPreviousX, zombie.X, AdventureRenderRemainder * 100f) : zombie.X;
        }

        /// <summary>Performs adventure floor yat pixel.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "x">The x.</param>
        /// <returns>The resulting adventure floor yat pixel.</returns>
        private float AdventureFloorYAtPixel(int row, float x)
        {
            return PvZGame.AdventureGridY + row * _owner.AdventurePitch + (_owner._game.Adventure.Level.HasRoof ? Mathf.Max(0, 440f - x) * .25f - 10f : 0f);
        }

        /// <summary>Performs adventure zombie source origin.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "x">The x.</param>
        /// <returns>The resulting adventure zombie source origin.</returns>
        private Vector2 AdventureZombieSourceOrigin(Zombie zombie, float x)
        {
            var size = zombie.IsSmall ? .5f : 1f;
            var pixelX = AdventureZombieNativeRules.ReanimationSourceX(x, size);
            var y = AdventureFloorYAtPixel(zombie.Row, pixelX + 40f) - 30f;
            if (zombie.Kind == ZombieKind.Balloon)
            {
                y -= 30f;
            }

            if (zombie.Kind == ZombieKind.Pogo && !zombie.UsedAbility)
            {
                y -= 16f;
            }

            if (zombie.DropRemaining > 0)
            {
                y -= Mathf.Min(260f, zombie.DropRemaining * 120f);
            }

            return new Vector2(pixelX, y);
        }

        /// <summary>Performs adventure zombie origin.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "x">The x.</param>
        /// <returns>The resulting adventure zombie origin.</returns>
        private Vector2 AdventureZombieOrigin(Zombie zombie, float x)
        {
            var scale = zombie.IsSmall ? .5f : 1f;
            return AdventureZombieSourceOrigin(zombie, x) + new Vector2(20f * (1f - scale), 120f * (1f - scale));
        }

        /// <summary>Performs draw adventure mower.</summary>
        /// <param name = "mower">The mower.</param>
        private void DrawAdventureMower(Mower mower)
        {
            var pool = _owner._game.Adventure.Level.IsWaterRow(mower.Row) && _owner._game.Rules.AdventurePoolCleaners;
            var roof = _owner._game.Adventure.Level.HasRoof;
            var name = roof ? "RoofCleaner" : pool ? "PoolCleaner" : "LawnMower";
            var clip = pool ? mower.X > .125f ? "anim_water" : "anim_land" : roof ? null : "anim_normal";
            float age;
            _owner._game.Presentation.MowerTimes.TryGetValue(mower, out age);
            if (mower.Active)
            {
                age += AdventureRenderRemainder;
            }
            else
            {
                age = 0;
            }

            var x = _owner.AdventureIntroActive ? _owner._adventureIntroFrame.MowerX[mower.Row] : PvZGame.AdventureGridX + mower.X * PvZGame.CellWidth - 21f;
            // PC 0x465F60: row floor+23, attached reanim y=18, scale .85 (.8 for pool).
            // Draw translates x+6; ready roof/pool offsets are -40/-33 and active -4/-7.
            var origin = new Vector2(x + 6f, AdventureFloorYAtPixel(mower.Row, x + 40f) + 23f + (mower.Active ? 18f : 19f));
            if (pool)
            {
                origin.y -= mower.Active ? 7f : 33f;
                if (mower.Active)
                {
                    origin.x -= 10f;
                }
            }

            if (roof)
            {
                origin.y -= mower.Active ? 4f : 40f;
                if (mower.Active)
                {
                    origin.x -= 10f;
                }
            }

            _owner._animations.DrawNativePlayback(name, origin, pool ? .8f : .85f, age, clip, true, mower.Active ? pool ? 35f : 70f : 0f);
        }

        /// <summary>Performs adventure plant height.</summary>
        /// <param name = "plant">The plant.</param>
        /// <returns>The resulting adventure plant height.</returns>
        private float AdventurePlantHeight(Plant plant)
        {
            var id = (int)plant.Kind;
            var offset = id < 49 ? AlmanacPortraitLayout.PlantHeightOffset(id) : 0;
            if (_owner._game.Adventure.Level.HasRoof)
            {
                offset -= 12f;
            }

            var hasFlowerPot = false;
            if (plant.Kind != PlantKind.FlowerPot)
            {
                foreach (var support in _owner._game.Plants)
                {
                    if (support.Kind == PlantKind.FlowerPot && support.Row == plant.Row && support.Column == plant.Column)
                    {
                        hasFlowerPot = true;
                        break;
                    }
                }
            }

            if (hasFlowerPot)
            {
                offset -= plant.Kind == PlantKind.Chomper || plant.Kind == PlantKind.Plantern ? 10f : 5f;
                if (plant.Kind == PlantKind.ScaredyShroom)
                {
                    offset -= 3f;
                }

                if (plant.Kind == PlantKind.PuffShroom || plant.Kind == PlantKind.SunShroom)
                {
                    offset -= 4f;
                }

                if (plant.Kind == PlantKind.HypnoShroom || plant.Kind == PlantKind.MagnetShroom)
                {
                    offset -= 8f;
                }
            }

            return offset;
        }

        /// <summary>Performs adventure plant head.</summary>
        /// <param name = "plant">The plant.</param>
        /// <param name = "name">The name.</param>
        /// <param name = "idle">The idle.</param>
        /// <param name = "shoot">The shoot.</param>
        /// <param name = "anchor">The anchor.</param>
        /// <param name = "head">The head.</param>
        /// <returns>The resulting adventure plant head.</returns>
        private global::PlantsVsZombies.Reanimation.HeadPlayback AdventurePlantHead(Plant plant, string name, string idle, string shoot, string anchor, int head)
        {
            var rate = AdventurePresentationRules.ShootingRate(plant.Kind);
            var age = plant.PresentationHeadAge[head];
            global::PlantsVsZombies.Reanimation.NativeAnimationInfo info;
            var firing = !plant.Asleep && age >= 0 && _owner._animations.TryGetNativeAnimationInfo(name, shoot, out info) && age < info.Count / rate;
            return new global::PlantsVsZombies.Reanimation.HeadPlayback
            {
                Anchor = anchor,
                Animation = firing ? shoot : idle,
                Seconds = firing ? age + AdventureRenderRemainder : plant.Age + AdventureRenderRemainder,
                Rate = firing ? rate : plant.PresentationRate > 0 ? plant.PresentationRate : 15f,
                Loop = !firing
            };
        }

        /// <summary>Performs draw adventure plant.</summary>
        /// <param name = "plant">The plant.</param>
        private void DrawAdventurePlant(Plant plant)
        {
            float x = PvZGame.AdventureGridX + plant.Column * PvZGame.CellWidth, y = _owner.AdventureCellY(plant.Row, plant.Column);
            var name = AdventurePlantAnimation(plant.Kind);
            var size = plant.IsSmall ? .5f : 1f;
            DrawImage("plantshadow.png", new Rect(x + 1, y + 65, 76 * plant.ColumnSpan, 24));
            var origin = new Vector2(x + (1f - size) * 40f, y + AdventurePlantHeight(plant) + (1f - size) * 80f);
            if (DrawAdventureCherryBomb(plant, origin, size))
            {
                return;
            }

            if (DrawAdventurePotatoMine(plant, origin, size))
            {
                return;
            }

            var clip = "anim_idle";
            var loop = true;
            var rate = plant.PresentationRate > 0 ? plant.PresentationRate : 12f;
            var age = plant.Age + AdventureRenderRemainder;
            if (plant.Kind == PlantKind.PotatoMine)
            {
                clip = plant.MinePhase == MinePhase.Buried ? "anim_idle" : plant.MinePhase == MinePhase.Rising ? "anim_rise" : "anim_armed";
                if (plant.MinePhase == MinePhase.Rising)
                {
                    age = plant.PhaseAge + AdventureRenderRemainder;
                    rate = 18f;
                    loop = false;
                }
            }

            if (plant.Kind == PlantKind.SunShroom)
            {
                var awakeAge = plant.PresentationAwakeAge;
                if (awakeAge >= 120f && awakeAge < 121f)
                {
                    clip = "anim_grow";
                    age = awakeAge - 120f + AdventureRenderRemainder;
                    rate = 12f;
                    loop = false;
                }
                else if (awakeAge >= 121f)
                {
                    clip = plant.Asleep ? "anim_bigsleep" : "anim_bigidle";
                }
            }

            if (plant.Asleep && plant.Kind != PlantKind.SunShroom && _owner.AdventureClipExists(name, "anim_sleep"))
            {
                clip = "anim_sleep";
            }
            else if (plant.Asleep && plant.Kind == PlantKind.SunShroom && plant.PresentationAwakeAge < 120f)
            {
                clip = "anim_sleep";
            }

            if (plant.Kind == PlantKind.CobCannon)
            {
                clip = plant.CannonPhase == CobCannonPhase.Firing ? "anim_shooting" : plant.CannonPhase == CobCannonPhase.Arming || plant.CannonPhase == CobCannonPhase.Reloading ? "anim_unarmed_idle" : plant.CannonPhase == CobCannonPhase.Loading ? "anim_charge" : "anim_idle";
                loop = plant.CannonPhase != CobCannonPhase.Firing && plant.CannonPhase != CobCannonPhase.Loading;
                rate = 12f;
                if (!loop)
                {
                    age = Math.Max(0, (plant.CannonPhase == CobCannonPhase.Firing ? GameSimulation.AdventureCobFiringSeconds : GameSimulation.AdventureCobLoadingSeconds) - plant.CannonReloadRemaining) + AdventureRenderRemainder;
                }
            }

            if (plant.Kind == PlantKind.Imitater && _owner.AdventureClipExists(name, "anim_morph"))
            {
                clip = "anim_morph";
                loop = false;
            }

            var shooting = plant.Kind == PlantKind.Starfruit ? "anim_shoot" : "anim_shooting";
            global::PlantsVsZombies.Reanimation.NativeAnimationInfo shot;
            if (!plant.Asleep && !AdventurePresentationRules.AttachedHead(plant.Kind) && plant.Kind != PlantKind.CobCannon && plant.PresentationShotAge >= 0 && _owner._animations.TryGetNativeAnimationInfo(name, shooting, out shot) && plant.PresentationShotAge < shot.Count / AdventurePresentationRules.ShootingRate(plant.Kind))
            {
                clip = shooting;
                age = plant.PresentationShotAge + AdventureRenderRemainder;
                rate = AdventurePresentationRules.ShootingRate(plant.Kind);
                loop = false;
            }

            HeadPlayback[]? heads = null;
            if (AdventurePresentationRules.AttachedHead(plant.Kind) && !plant.Asleep)
            {
                if (plant.Kind == PlantKind.Threepeater)
                {
                    heads = _plantHeads[2];
                    heads[0] = AdventurePlantHead(plant, name, "anim_head_idle1", "anim_shooting1", "anim_head1", 0);
                    heads[1] = AdventurePlantHead(plant, name, "anim_head_idle2", "anim_shooting2", "anim_head2", 1);
                    heads[2] = AdventurePlantHead(plant, name, "anim_head_idle3", "anim_shooting3", "anim_head3", 2);
                }
                else if (plant.Kind == PlantKind.SplitPea)
                {
                    heads = _plantHeads[1];
                    heads[0] = AdventurePlantHead(plant, name, "anim_head_idle", "anim_shooting", "anim_idle", 0);
                    heads[1] = AdventurePlantHead(plant, name, "anim_splitpea_idle", "anim_splitpea_shooting", "anim_idle", 1);
                }
                else
                {
                    heads = _plantHeads[0];
                    heads[0] = AdventurePlantHead(plant, name, "anim_head_idle", "anim_shooting", _owner.AdventureClipExists(name, "anim_stem") ? "anim_stem" : "anim_idle", 0);
                }
            }

            RegisterPlantSurface(plant);
            ActorRenderer.DrawPlant(plant, name, origin, size, heads == null ? age : plant.Age + AdventureRenderRemainder,
                clip, loop, rate, heads, _presentationKeys.Get("plant-", plant.Id), new Rect(x - 80, y - 160, 300, 340),
                plant.Kind == PlantKind.Threepeater || plant.Kind == PlantKind.Cattail ? 10 : 20, AdventureRenderRemainder, _presentationKeys.Get("producer-plant-", plant.Id));

            if (plant.Kind == PlantKind.FumeShroom && plant.PresentationShotAge >= .35f && plant.PresentationShotAge < 1.35f)
            {
                _adventureParticles.Draw(_presentationKeys.Get("fume-", plant.Id), "FumeCloud", plant.PresentationShotAge - .35f, new Vector2(x + 85, y + 31), plant.Id);
            }

            if (plant.Kind == PlantKind.GloomShroom)
            {
                var nativeBursts = s_gloomBursts;
                for (var burst = 0; burst < nativeBursts.Length; burst++)
                {
                    if (plant.PresentationShotAge >= nativeBursts[burst] && plant.PresentationShotAge < nativeBursts[burst] + .6f)
                    {
                        _adventureParticles.Draw(_presentationKeys.Get("gloom-", plant.Id, burst), "GloomCloud", plant.PresentationShotAge - nativeBursts[burst], new Vector2(x + 40, y + 40), plant.Id + burst);
                    }
                }
            }
        }

        /// <summary>Tracks the composition surface used by a plant's active feedback.</summary>
        /// <param name="plant">The plant whose effects may require a retained surface.</param>
        private void RegisterPlantSurface(Plant plant)
        {
            if (AdventureProducerGlow.Visual(plant).Enabled || plant.IsImitated || plant.PresentationEatenFlashTicks > 0)
            {
                var key = _presentationKeys.Get("producer-plant-", plant.Id);
                _adventurePresentationSurfaces.Add(key);
                _adventurePresentationLiveSurfaces.Add(key);
            }
        }

        /// <summary>Performs adventure zombie clip.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting adventure zombie clip.</returns>
        private string AdventureZombieClip(Zombie zombie, string name)
        {
            string clip;
            if (zombie.Kind == ZombieKind.Newspaper && zombie.ShieldHealth <= 0)
            {
                clip = zombie.Eating ? "anim_eat_nopaper" : "anim_walk_nopaper";
            }
            else if (zombie.Kind == ZombieKind.Ladder && zombie.ShieldHealth > 0)
            {
                clip = zombie.Eating ? "anim_laddereat" : "anim_ladderwalk";
            }
            else if (zombie.Eating)
            {
                clip = "anim_eat";
            }
            else if (zombie.Kind == ZombieKind.PoleVaulter && !zombie.UsedAbility)
            {
                clip = "anim_run";
            }
            else if (zombie.Kind == ZombieKind.Pogo && !zombie.UsedAbility)
            {
                clip = "anim_pogo";
            }
            else if (zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 0)
            {
                clip = "anim_dig";
            }
            else if (zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 1)
            {
                clip = "anim_dizzy";
            }
            else if (zombie.Kind == ZombieKind.DolphinRider && !zombie.UsedAbility)
            {
                clip = _owner._game.Adventure.Level.IsWaterRow(zombie.Row) ? "anim_ride" : "anim_walkdolphin";
            }
            else if (zombie.Kind == ZombieKind.Snorkel && zombie.Submerged)
            {
                clip = "anim_swim";
            }
            else if (zombie.Kind == ZombieKind.DuckyTube && _owner._game.Adventure.Level.IsWaterRow(zombie.Row))
            {
                clip = "anim_swim";
            }
            else if (zombie.Kind == ZombieKind.Zomboni || zombie.Kind == ZombieKind.Catapult)
            {
                clip = "anim_drive";
            }
            else
            {
                clip = zombie.PresentationWalk2 && _owner.AdventureClipExists(name, "anim_walk2") ? "anim_walk2" : "anim_walk";
            }

            if (_owner.AdventureClipExists(name, clip))
            {
                return clip;
            }

            return _owner.AdventureClipExists(name, "anim_walk") ? "anim_walk" : _owner.AdventureClipExists(name, "anim_idle") ? "anim_idle" : null;
        }

        /// <summary>Performs adventure zombie rate.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "name">The name.</param>
        /// <param name = "clip">The clip.</param>
        /// <param name = "size">The size.</param>
        /// <returns>The resulting adventure zombie rate.</returns>
        private float AdventureZombieRate(Zombie zombie, string name, string clip, float size)
        {
            if (zombie.Eating)
            {
                return zombie.Kind == ZombieKind.PoleVaulter || zombie.Kind == ZombieKind.Balloon || zombie.Kind == ZombieKind.Imp || zombie.Kind == ZombieKind.Digger || zombie.Kind == ZombieKind.JackInTheBox || zombie.Kind == ZombieKind.Snorkel || zombie.Kind == ZombieKind.Yeti ? 20f : 36f;
            }

            global::PlantsVsZombies.Reanimation.NativeAnimationInfo info;
            if (_owner._animations.TryGetNativeAnimationInfo(name, clip, out info) && info.GroundDistance > .000001f)
            {
                return zombie.Speed * PvZGame.CellWidth * .01f * 47f * info.Count / info.GroundDistance / size;
            }

            return _owner._animations.TryGetNativeAnimationInfo(name, clip, out info) ? info.SourceRate : 12f;
        }

        /// <summary>Performs draw adventure zombie.</summary>
        /// <param name = "zombie">The zombie.</param>
        private void DrawAdventureZombie(Zombie zombie)
        {
            if (!_owner._game.IsModeZombieVisible(zombie))
            {
                return;
            }

            float size = zombie.IsSmall ? .5f : 1f, x = AdventureRenderX(zombie);
            var origin = AdventureZombieOrigin(zombie, x);
            DrawImage("plantshadow.png", new Rect(PvZGame.AdventureGridX + x * PvZGame.CellWidth - 38 * size, _owner.AdventureCellY(zombie.Row, x) + 68, 76 * size, 22 * size));
            string name = PvZGame.AdventureZombieAnimation(zombie.Kind), clip = _owner._game.AdventurePresentationZombieAnimation(zombie);
            if (!string.Equals(zombie.PresentationAnimation, clip, StringComparison.Ordinal))
            {
                zombie.PresentationAnimation = clip;
                zombie.PresentationAnimationAge = 0;
            }

            var age = zombie.PresentationAnimationAge + (zombie.FrozenRemaining > 0 ? 0 : AdventureRenderRemainder * (zombie.ChillRemaining > 0 ? .5f : 1f));
            var rate = _owner._game.AdventurePresentationZombieRate(zombie);
            DrawAdventureZombiePose(zombie, name, clip, origin, size, age, rate, zombie.PresentationNewspaperGaspRemaining <= 0f);
            if (zombie.FrozenRemaining > 0 && zombie.PresentationButter <= 0)
            {
                _owner._animations.DrawResourceSprite("IMAGE_ICETRAP", new Vector2(PvZGame.AdventureGridX + x * PvZGame.CellWidth, _owner.AdventureCellY(zombie.Row, x) + 79), size);
            }

            if (zombie.PresentationButter > 0)
            {
                Vector2 head;
                if (_owner._animations.TryGetNativeTrackPosition(name, clip, "anim_head1", age, rate, true, out head))
                {
                    _owner._animations.DrawResourceSprite("IMAGE_REANIM_CORNPULT_BUTTER_SPLAT", origin + (head + new Vector2(11, 21)) * size, size);
                }
            }
        }

        /// <summary>Performs draw adventure zombie pose.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "name">The name.</param>
        /// <param name = "clip">The clip.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "size">The size.</param>
        /// <param name = "age">The age.</param>
        /// <param name = "rate">The rate.</param>
        /// <param name = "loop">The loop.</param>
        private void DrawAdventureZombiePose(Zombie zombie, string name, string clip, Vector2 origin, float size, float age, float rate, bool loop)
        {
            var previous = CanvasUI.Color;
            var surface = _presentationKeys.Get("native-zombie-combat-", zombie.Id);
            _adventurePresentationSurfaces.Add(surface);
            _adventurePresentationLiveSurfaces.Add(surface);
            ActorRenderer.DrawZombie(zombie, name, clip, origin, size, age, rate, loop, _presentationKeys.Get("actor-", zombie.Id), surface);
            CanvasUI.Color = previous;
        }

        /// <summary>Performs draw adventure detached arms.</summary>
        /// <param name = "row">The row.</param>
        private void DrawAdventureDetachedArms(int row)
        {
            foreach (var arm in _owner._game.Presentation.ArmDrops)
            {
                if ((int)arm.Row != row)
                {
                    continue;
                }

                var pose = new Zombie
                {
                    Kind = (ZombieKind)arm.NativeKind,
                    Row = row,
                    X = arm.X,
                    IsSmall = arm.Scale < 1f,
                    UsedAbility = arm.Animation != "anim_pogo" && arm.Animation != "anim_run" && arm.Animation != "anim_walkdolphin"
                };
                _owner._animations.DrawNativeZombieArmDrop(_adventureParticles, arm, AdventureZombieOrigin(pose, arm.X), AdventureRenderRemainder);
            }
        }

        /// <summary>Performs draw adventure detached zombie parts.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "behindBody">The behind body.</param>
        private void DrawAdventureDetachedZombieParts(int row, bool behindBody)
        {
            foreach (var part in _owner._game.Presentation.ZombiePartDrops)
            {
                if (part.Row != row || (part.NativeKind == 8 && part.ParticleId != 11) != behindBody)
                {
                    continue;
                }

                var pose = _detachedPartProbe;
                pose.Kind = (ZombieKind)part.NativeKind;
                pose.Row = row;
                pose.X = part.X;
                pose.IsSmall = part.Scale < 1f;
                pose.UsedAbility = part.Animation != "anim_pogo" && part.Animation != "anim_run" && part.Animation != "anim_walkdolphin";
                _owner._animations.DrawNativeZombiePartDrop(_adventureParticles, part, AdventureZombieOrigin(pose, part.X), AdventureRenderRemainder);
            }
        }

        /// <summary>Performs draw adventure plant bite effects.</summary>
        /// <param name = "row">The row.</param>
        private void DrawAdventurePlantBiteEffects(int row)
        {
            foreach (var effect in _owner._game.Presentation.PlantBiteEffects)
            {
                if (effect.Row != row)
                {
                    continue;
                }

                _adventureParticles.Draw(_presentationKeys.Get("native-plant-bite-", effect.Id), effect.ParticleId == 35 ? "WallnutEatSmall" : "MindControl", effect.Age + AdventureRenderRemainder, new Vector2(effect.NativeX + effect.OffsetX, effect.NativeY + effect.OffsetY), effect.Seed);
            }
        }

        /// <summary>Performs draw adventure corpses.</summary>
        /// <param name = "row">The row.</param>
        private void DrawAdventureCorpses(int row)
        {
            foreach (AdventureZombieCorpse corpse in _owner._game.Presentation.Corpses)
            {
                var zombie = corpse.Zombie;
                if (zombie.Row != row || !_owner._game.IsModeZombieVisible(zombie))
                {
                    continue;
                }

                if (corpse.Burn != null)
                {
                    DrawAdventureBurnedCorpse(corpse);
                    continue;
                }

                string name = PvZGame.AdventureZombieAnimation(zombie.Kind), clip = corpse.Animation;
                var origin = AdventureZombieOrigin(zombie, zombie.X);
                float rate = corpse.Rate, size = zombie.IsSmall ? .5f : 1f;
                if (!_owner.AdventureClipExists(name, clip))
                {
                    clip = "anim_death";
                }

                global::PlantsVsZombies.Reanimation.NativeAnimationInfo info;
                if (!_owner._animations.TryGetNativeAnimationInfo(name, clip, out info))
                {
                    corpse.Duration = 0;
                    continue;
                }

                if (corpse.Age < corpse.Duration)
                {
                    var color = CanvasUI.Color;
                    CanvasUI.Color = new Color(color.r, color.g, color.b, color.a * corpse.Alpha(AdventureRenderRemainder));
                    try
                    {
                        DrawAdventureZombiePose(zombie, name, clip, origin, size, Mathf.Min(corpse.AnimationDuration, corpse.Age + AdventureRenderRemainder), rate, false);
                    }
                    finally
                    {
                        CanvasUI.Color = color;
                    }
                }
            }
        }

        /// <summary>Performs draw adventure burned corpse.</summary>
        /// <param name = "corpse">The corpse.</param>
        private void DrawAdventureBurnedCorpse(AdventureZombieCorpse corpse)
        {
            var burn = corpse.Burn;
            var zombie = corpse.Zombie;
            if (burn.Visual == AdventureBurnVisual.FrozenOriginal)
            {
                if (corpse.Age >= 3f)
                {
                    return;
                }

                var name = PvZGame.AdventureZombieAnimation(zombie.Kind);
                var clip = corpse.Animation ?? AdventureZombieClip(zombie, name);
                if (corpse.FrozenRate <= 0)
                {
                    corpse.FrozenRate = AdventureZombieRate(zombie, name, clip, burn.Scale);
                }

                var variant = zombie.Kind == ZombieKind.Flag ? ZombieVariant.Flag : zombie.Kind == ZombieKind.Conehead && zombie.ArmorHealth > 0 ? ZombieVariant.Conehead : zombie.Kind == ZombieKind.Buckethead && zombie.ArmorHealth > 0 ? ZombieVariant.Buckethead : zombie.Kind == ZombieKind.ScreenDoor && zombie.ShieldHealth > 0 ? ZombieVariant.ScreenDoor : ZombieVariant.Normal;
                var plantHead = zombie.ModeHead >= 26 && zombie.ModeHead <= 31;
                Func<string, bool> visible = track => (!plantHead || !track.StartsWith("anim_head", StringComparison.OrdinalIgnoreCase) && !string.Equals(track, "anim_hair", StringComparison.OrdinalIgnoreCase) && (zombie.ModeHead == 26 || zombie.ModeHead == 29 || zombie.ModeHead == 30 || !string.Equals(track, "Zombie_tie", StringComparison.OrdinalIgnoreCase))) && (!zombie.Headless || track.IndexOf("head", StringComparison.OrdinalIgnoreCase) < 0 && track.IndexOf("hair", StringComparison.OrdinalIgnoreCase) < 0 && !string.Equals(track, "anim_cone", StringComparison.OrdinalIgnoreCase) && !string.Equals(track, "anim_bucket", StringComparison.OrdinalIgnoreCase)) && (!string.Equals(name, "Zombie", StringComparison.OrdinalIgnoreCase) || zombie.Kind == ZombieKind.DuckyTube && string.Equals(track, "Zombie_duckytube", StringComparison.OrdinalIgnoreCase) || ReanimationRenderer.IsZombieTrackVisible(track, variant));
                var previous = CanvasUI.Color;
                CanvasUI.Color = new Color(0, 0, 0, previous.a);
                var origin = AdventureZombieOrigin(zombie, zombie.X);
                try
                {
                    if (string.Equals(name, "Zombie", StringComparison.OrdinalIgnoreCase) && !zombie.Headless && !plantHead && !burn.FacingLeft && zombie.Kind != ZombieKind.DuckyTube)
                    {
                        _owner._animations.DrawZombiePlayback(origin, burn.Scale, corpse.FrozenSeconds, variant, clip, true, corpse.FrozenRate);
                    }
                    else
                    {
                        _owner._animations.DrawNativePlayback(name, origin, burn.Scale, corpse.FrozenSeconds, clip, true, corpse.FrozenRate, visible, facingLeft: burn.FacingLeft, mirrorWidth: 60f);
                    }

                    if (plantHead && !zombie.Headless)
                    {
                        _owner._animations.DrawNativePlantHeadAttachment(origin, burn.Scale, clip, corpse.FrozenSeconds, corpse.FrozenRate, zombie.ModeHead, corpse.FrozenHeadSeconds, burn.FacingLeft);
                    }
                }
                finally
                {
                    CanvasUI.Color = previous;
                }

                return;
            }

            if (burn.Visual != AdventureBurnVisual.CharredDefinition)
            {
                return;
            }

            ReanimationDefinition definition;
            if (!VerifiedDataCatalog.TryGetReanimation(burn.ReanimationId, out definition))
            {
                corpse.Duration = 0;
                return;
            }

            var asset = Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/'));
            global::PlantsVsZombies.Reanimation.NativeAnimationInfo info;
            var full = burn.Animation == null;
            var found = full ? _owner._animations.TryGetNativeDefinitionInfo(asset, out info) : _owner._animations.TryGetNativeAnimationInfo(asset, burn.Animation, out info);
            if (!found)
            {
                corpse.Duration = 0;
                return;
            }

            var rate = info.SourceRate * corpse.RateScale;
            corpse.Duration = info.Count / rate;
            if (corpse.Age >= corpse.Duration)
            {
                return;
            }

            var position = AdventureZombieSourceOrigin(zombie, zombie.X) + new Vector2(burn.X + burn.PivotX, burn.Y + burn.PivotY);
            var trackVisible = burn.HideImp ? (Func<string, bool>)(track => !string.Equals(track, "impblink", StringComparison.OrdinalIgnoreCase) && !string.Equals(track, "imphead", StringComparison.OrdinalIgnoreCase)) : null;
            if (full)
            {
                _owner._animations.DrawNativeDefinition(asset, position, burn.Scale, corpse.Age + AdventureRenderRemainder, false, rate, trackVisible, burn.FacingLeft, 60f);
            }
            else
            {
                _owner._animations.DrawNativePlayback(asset, position, burn.Scale, corpse.Age + AdventureRenderRemainder, burn.Animation, false, rate, trackVisible, facingLeft: burn.FacingLeft, mirrorWidth: 60f);
            }
        }

        /// <summary>Performs draw collected suns above seed bank.</summary>
        private void DrawCollectedSunsAboveSeedBank()
        {
            if (_owner._game == null || !_owner._game.IsAdventure)
            {
                return;
            }

            foreach (global::PlantsVsZombies.Simulation.Sun sun in _owner._game.Suns)
            {
                if (sun.PresentationNative && sun.PresentationPickup.Collecting)
                {
                    DrawAdventureSun(sun);
                }
            }
        }

        /// <summary>Performs draw adventure sun.</summary>
        /// <param name = "sun">The sun.</param>
        private void DrawAdventureSun(global::PlantsVsZombies.Simulation.Sun sun)
        {
            Vector2 center;
            var scale = sun.Value == 15 ? .5f : sun.Value == 50 ? 2f : 1f;
            var alpha = 1f;
            if (sun.PresentationNative)
            {
                var pickup = sun.PresentationPickup;
                if (pickup.Dead)
                {
                    return;
                }

                center = new Vector2(Mathf.Lerp(sun.PresentationPreviousX, pickup.X, AdventureRenderRemainder * 100f) + 30, Mathf.Lerp(sun.PresentationPreviousY, pickup.Y, AdventureRenderRemainder * 100f) + 30);
                scale = sun.PresentationScale;
                if (pickup.FadeCountdown > 0)
                {
                    alpha = pickup.FadeCountdown / 15f;
                }
            }
            else
            {
                center = new Vector2(PvZGame.AdventureGridX + sun.X * PvZGame.CellWidth, PvZGame.AdventureGridY + sun.Y * _owner.AdventurePitch);
            }

            var previous = CanvasUI.Color;
            CanvasUI.Color = new Color(previous.r, previous.g, previous.b, previous.a * alpha);
            // PC 0x43B610 creates the original Sun reanimation at the 60x60 pickup centre, rate 6.
            // The authored glow/rays extend beyond that logical pickup rectangle; never fit them into it.
            _owner._animations.DrawNativePlayback("Sun", center, scale, sun.Age + AdventureRenderRemainder, null, true, 6f);
            CanvasUI.Color = previous;
        }

        /// <summary>Performs draw adventure projectile.</summary>
        /// <param name = "pea">The pea.</param>
        private void DrawAdventureProjectile(Pea pea)
        {
            if (pea.SourceKind == PlantKind.FumeShroom)
            {
                return; // Original fume uses its authored cloud, not a flying pea.
            }

            var x = pea.PresentationInitialized ? Mathf.Lerp(pea.PresentationPreviousX, pea.X, AdventureRenderRemainder * 100f) : pea.X;
            var row = pea.PresentationInitialized ? Mathf.Lerp(pea.PresentationPreviousRow, pea.FlightRow, AdventureRenderRemainder * 100f) : pea.FlightRow;
            var y = PvZGame.AdventureGridY + row * _owner.AdventurePitch + _owner.AdventureSlope(x) + 30f;
            var age = pea.PresentationAge + AdventureRenderRemainder;
            float rotation = 0;
            if (pea.Lobbed)
            {
                y = pea.PresentationLaunchY + Mathf.Lerp(pea.PresentationPreviousLobHeight, pea.PresentationLobHeight, AdventureRenderRemainder * 100f);
                rotation = (pea.PresentationRotation + pea.PresentationSpin * pea.PresentationNativeAge * 100f) * Mathf.Rad2Deg;
                _owner._animations.DrawResourceSprite("IMAGE_PEA_SHADOWS", new Vector2(PvZGame.AdventureGridX + x * PvZGame.CellWidth, _owner.AdventureCellY(pea.Row, x) + 80), 1.6f * 200f / (Mathf.Clamp(-pea.PresentationLobHeight, 0, 200) + 200), 0, _owner._game.Adventure.Level.IsNight ? 1 : 0);
            }

            var center = new Vector2(PvZGame.AdventureGridX + x * PvZGame.CellWidth, y);
            if (pea.Fire)
            {
                _owner._animations.DrawNativePlayback("FirePea", center, 1f, age, null, true);
                return;
            }

            string image;
            if (pea.Lobbed)
            {
                image = pea.Butter ? "IMAGE_REANIM_CORNPULT_BUTTER" : pea.Splash ? pea.IsSnow ? "IMAGE_REANIM_WINTERMELON_PROJECTILE" : "IMAGE_REANIM_MELONPULT_MELON" : pea.SourceKind == PlantKind.KernelPult ? "IMAGE_REANIM_CORNPULT_KERNAL" : "IMAGE_REANIM_CABBAGEPULT_CABBAGE";
            }
            else if (pea.SourceKind == PlantKind.Starfruit)
            {
                image = "IMAGE_PROJECTILE_STAR";
            }
            else if (pea.SourceKind == PlantKind.Cattail)
            {
                image = "IMAGE_REANIM_CATTAIL_SPIKE";
                rotation = Mathf.Atan2(pea.VelocityY * _owner.AdventurePitch, pea.VelocityX * PvZGame.CellWidth) * Mathf.Rad2Deg;
            }
            else if (pea.SourceKind == PlantKind.Cactus)
            {
                image = "IMAGE_PROJECTILECACTUS";
            }
            else if (pea.SourceKind == PlantKind.PuffShroom || pea.SourceKind == PlantKind.ScaredyShroom || pea.SourceKind == PlantKind.SeaShroom)
            {
                image = "IMAGE_PUFFSHROOM_PUFF1";
            }
            else
            {
                image = pea.IsSnow ? "IMAGE_PROJECTILESNOWPEA" : "IMAGE_PROJECTILEPEA";
            }

            _owner._animations.DrawResourceSprite(image, center, 1f, rotation);
        }

        /// <summary>Performs draw adventure impacts.</summary>
        /// <param name = "row">The row.</param>
        private void DrawAdventureImpacts(int row)
        {
            foreach (AdventureImpact impact in _owner._game.Presentation.Impacts)
            {
                if ((int)Math.Round(impact.Row) != row)
                {
                    continue;
                }

                var point = new Vector2(PvZGame.AdventureGridX + impact.X * PvZGame.CellWidth, PvZGame.AdventureGridY + impact.Row * _owner.AdventurePitch + _owner.AdventureSlope(impact.X) + 42);
                if (impact.NativeIcePoint)
                {
                    float nativeX, nativeY;
                    _owner._game.AdventureIceImpactPoint(impact, AdventureRenderRemainder * 100f, out nativeX, out nativeY);
                    point = new Vector2(nativeX, nativeY);
                }

                var key = _presentationKeys.Get("impact-", impact.Id);
                if (impact.Mowed)
                {
                    _adventureParticles.Draw(key + "-head", "MoweredZombieHead", impact.Age, point, impact.Seed);
                    _adventureParticles.Draw(key + "-arm", "MoweredZombieArm", impact.Age, point, impact.Seed + 1);
                    continue;
                }

                if (impact.Cob)
                {
                    _adventureParticles.Draw(key + "-blast", "BlastMark", impact.Age, point, impact.Seed);
                    _adventureParticles.Draw(key + "-popcorn", "PopcornSplash", impact.Age, point, impact.Seed + 1);
                    continue;
                }

                if (impact.Fire)
                {
                    global::PlantsVsZombies.Reanimation.NativeAnimationInfo fire;
                    if (_owner._animations.TryGetNativeAnimationInfo("fire", "anim_flame", out fire) && impact.Age < fire.Count / 24f * .75f)
                    {
                        _owner._animations.DrawNativePlayback("fire", point + new Vector2(38, -20), .7f, impact.Age + fire.Count / 24f * .25f, "anim_flame", false, 24f, verticalScale: .4f);
                    }

                    continue;
                }

                var effect = impact.Butter ? "ButterSplat" : impact.Kind == PlantKind.MelonPult ? "melonimpact" : impact.Kind == PlantKind.WinterMelon ? "WinterMelonImpact" : impact.Kind == PlantKind.CabbagePult ? "CabbageSplat" : impact.Kind == PlantKind.Starfruit ? "StarSplat" : impact.Kind == PlantKind.PuffShroom || impact.Kind == PlantKind.ScaredyShroom || impact.Kind == PlantKind.SeaShroom ? "PuffSplat" : impact.Kind == PlantKind.Cactus || impact.Kind == PlantKind.Cattail || impact.Kind == PlantKind.KernelPult || impact.Kind == PlantKind.FumeShroom ? null : impact.Snow ? "SnowPeaSplat" : "PeaSplat";
                if (effect != null)
                {
                    _adventureParticles.Draw(key, effect, impact.Age, point, impact.Seed);
                }
            }
        }

        /// <summary>Stores award particles.</summary>
        private OriginalParticleRenderer _awardParticles;
        /// <summary>Stores award presented game.</summary>
        private GameSimulation _awardPresentedGame;

        /// <summary>Performs draw adventure reward.</summary>
        private void DrawAdventureReward()
        {
            var reward = _owner._game.Adventure.RewardPresentation;
            if (reward == null)
            {
                return;
            }

            if (!ReferenceEquals(_awardPresentedGame, _owner._game))
            {
                _awardPresentedGame = _owner._game;
                _awardParticles = new OriginalParticleRenderer(_owner._root, _owner._animations);
            }

            _awardParticles.BeginFrame();
            if (reward.Stage == AdventureAwardStage.Introduction)
            {
                DrawAdventureAwardIntroduction(reward);
            }
            else
            {
                var fraction = reward.RenderFraction;
                float x = Mathf.Lerp(reward.PreviousX, reward.X, fraction), y = Mathf.Lerp(reward.PreviousY, reward.Y, fraction);
                var size = Mathf.Lerp(reward.PreviousScale, reward.Scale, fraction);
                var center = new Vector2(x + reward.Width * .5f, y + reward.Height * .5f);
                if (reward.Stage == AdventureAwardStage.Collecting)
                {
                    // PC particle table 0x74C8D8: effect 47 is Award.xml. Its attachment follows
                    // the packet centre without inheriting the packet enlargement (0x43D740/43C570).
                    _awardParticles.Draw("award-rays", "Award", (reward.CollectionTicks + fraction) * .01f, center, 571);
                }

                if (reward.OnGround)
                {
                    _awardParticles.Draw("award-pickup-arrow", reward.NativeType == 7 ? "SeedPacket" : "AwardPickupArrow", reward.GroundTicks * .01f, new Vector2(x + reward.Width / 2, y + reward.Height / 2 - 60), 570);
                }

                if (reward.NativeType == 7 && _owner._game.Adventure.Level.RewardSeed.HasValue)
                {
                    DrawAdventureCard(_owner._game.Adventure.Level.RewardSeed.Value, new Rect(center.x - 25 * size, center.y - 35 * size, 50 * size, 70 * size), true);
                }
                else
                {
                    DrawAdventureAwardObject(reward, center, size);
                }

                // Starburst is native render order coin+1; the attached Award rays are coin-1.
                if (reward.Stage == AdventureAwardStage.Collecting)
                {
                    _awardParticles.Draw("award-starburst", "Starburst", (reward.CollectionTicks + fraction) * .01f, new Vector2(reward.CollectionStartX + 30, reward.CollectionStartY + 30), 572);
                }

                if (reward.FadeAlpha > 0)
                {
                    Fill(new Rect(0, 0, 800, 600), new Color(reward.NativeType == 15 ? 0 : 1, reward.NativeType == 15 ? 0 : 1, reward.NativeType == 15 ? 0 : 1, reward.FadeAlpha));
                }
            }

            _awardParticles.EndFrame();
            if (!string.IsNullOrEmpty(_owner._adventureFeedback) && Time.unscaledTime < _owner._adventureFeedbackUntil)
            {
                Label(new Rect(140, 110, 520, 70), _owner._adventureFeedback, 17, Color.yellow, TextAnchor.MiddleCenter, true);
            }
        }

        /// <summary>Performs draw adventure award button.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "enabled">The enabled.</param>
        private void DrawAdventureAwardButton(Rect rect, bool enabled)
        {
            DrawImage(enabled ? "SeedChooser_Button.png" : "SeedChooser_Button_Disabled.png", rect);
            if (enabled && rect.Contains(_owner._adventurePointer))
            {
                DrawImage("SeedChooser_Button_Glow.png", rect);
            }
        }

        /// <summary>Performs adventure award image.</summary>
        /// <param name = "nativeType">The native type.</param>
        /// <param name = "highResolution">The high resolution.</param>
        /// <returns>The resulting adventure award image.</returns>
        private string AdventureAwardImage(int nativeType, bool highResolution = false)
        {
            switch (nativeType)
            {
                case 9:
                    return highResolution ? "Shovel_hi_res.png" : "Shovel.png";
                case 10:
                    return "Almanac.png";
                case 11:
                    return "CarKeys.png";
                case 13:
                    return "WateringCan.png";
                case 14:
                    return "Taco.png";
                case 15:
                    return "ZombieNoteSmall.png";
                case 21:
                    return "Sunflower_trophy.png";
                default:
                    return highResolution ? "moneybag_hi_res.png" : "moneybag.png";
            }
        }

        /// <summary>Performs draw adventure award object.</summary>
        /// <param name = "award">The award.</param>
        /// <param name = "center">The center.</param>
        /// <param name = "size">The size.</param>
        private void DrawAdventureAwardObject(AdventureRewardPresentation award, Vector2 center, float size)
        {
            if (award.NativeType == 15 && award.Stage == AdventureAwardStage.Collecting)
            {
                return;
            }

            var image = AdventureAwardImage(award.NativeType);
            var source = _owner.Texture(image);
            if (source == null)
            {
                return;
            }

            var columns = award.NativeType == 21 ? 2 : 1;
            var destination = new Rect(center.x - source.width * size / columns / 2, center.y - source.height * size / 2, source.width * size / columns, source.height * size);
            CanvasUI.DrawTextureWithTexCoords(destination, source, new Rect(0, 0, 1f / columns, 1));
        }

        /// <summary>Performs draw adventure award introduction.</summary>
        /// <param name = "award">The award.</param>
        private void DrawAdventureAwardIntroduction(AdventureRewardPresentation award)
        {
            var level = _owner._game.Adventure.Level;
            if (award.NativeType == 15 || award.NativeType == 18 && level.Number == 50)
            {
                DrawAdventureNoteIntroduction(award);
                return;
            }

            if (level.IsReplay && award.NativeType == 18 && level.Number < 50)
            {
                return;
            }

            // PC app+0x884 is ADManager, not WidgetManager. 0x405750 loads
            // images/ad/game_pass.jpg into ADManager+104/+4; 0x40FCF0 draws it at(0,0,800,300).
            var background = _owner.Texture("AwardScreen_Back_new.jpg");
            if (background != null)
            {
                CanvasUI.DrawTexture(new Rect(0, 0, 800, 600), background);
            }

            DrawImage("ad/game_pass.jpg", new Rect(0, 0, 800, 300));
            string title = "", name = "", description = "";
            if (award.NativeType == 7 && level.RewardSeed.HasValue)
            {
                var kind = level.RewardSeed.Value;
                title = OriginalProgressionText("NEW_PLANT");
                name = _owner.AdventurePlantName(kind);
                description = OriginalProgressionText(VerifiedDataCatalog.Plants[(int)kind].InternalName + "_TOOLTIP");
                // Original PC DrawAwardSeed 0x40FE70: 2x packet at (190,360).
                DrawAdventureCard(kind, new Rect(190, 360, 100, 140), true);
            }
            else
            {
                AdventureAwardText(award.NativeType, out title, out name, out description);
                var icon = _owner.Texture(AdventureAwardImage(award.NativeType, true));
                if (icon != null)
                {
                    CanvasUI.DrawTexture(new Rect(240 - icon.width * .5f, 426 - icon.height * .5f, icon.width, icon.height), icon);
                }
            }

            var gold = new Color(213f / 255, 159f / 255, 43f / 255);
            // PC 0x40FCF0 fonts: Dwarven24 / Dwarven18Yellow / Brianne16.
            if (!OriginalUiWidgets.TextAtBaseline(title, 567, 356, 320, "DwarvenTodcraft24", gold))
            {
                Label(new Rect(407, 327, 320, 40), title, 24, gold, TextAnchor.MiddleCenter, true);
            }

            if (!OriginalUiWidgets.TextAtBaseline(name, 559, 392, 290, "DwarvenTodcraft18Yellow", Color.white))
            {
                Label(new Rect(414, 369, 290, 31), name, 18, Color.yellow, TextAnchor.MiddleCenter, true);
            }

            if (_label == null)
            {
                _label = new GUIStyle();
            }

            var oldWrap = _label.wordWrap;
            _label.wordWrap = true;
            var ink = new Color(40f / 255, 50f / 255, 90f / 255);
            if (!OriginalUiWidgets.Text(new Rect(444, 416, 230, 90), description, "BrianneTod16", ink, true, true))
            {
                Label(new Rect(444, 416, 230, 90), description, 16, ink, TextAnchor.MiddleCenter, false);
            }

            _label.wordWrap = oldWrap;
            var next = PvZGame.s_awardSeedNext;
            DrawAdventureAwardButton(next, award.IntroductionTicks >= 180);
            var caption = OriginalProgressionText("NEXT_LEVEL_BUTTON");
            if (!OriginalUiWidgets.TextAtBaseline(caption, next.center.x, next.y + 27, next.width, "DwarvenTodcraft15", gold))
            {
                Label(next, caption, 18, gold, TextAnchor.MiddleCenter, true);
            }

            DrawImage("Almanac_CloseButton.png", PvZGame.s_awardMenu);
            var menuText = OriginalProgressionText("AWARD_MAIN_MENU_BUTTON");
            if (!OriginalUiWidgets.TextAtBaseline(menuText, PvZGame.s_awardMenu.center.x, PvZGame.s_awardMenu.y + 17, PvZGame.s_awardMenu.width, "BrianneTod12", new Color(42f / 255, 42f / 255, 90f / 255)))
            {
                Label(PvZGame.s_awardMenu, menuText, 12, new Color(42f / 255, 42f / 255, 90f / 255), TextAnchor.MiddleCenter, true);
            }

            if (award.IntroductionAlpha > 0)
            {
                Fill(new Rect(0, 0, 800, 600), new Color(1, 1, 1, award.IntroductionAlpha));
            }
        }

        /// <summary>Performs draw adventure note introduction.</summary>
        /// <param name = "award">The award.</param>
        private void DrawAdventureNoteIntroduction(AdventureRewardPresentation award)
        {
            var note = award.NativeType == 18 ? 4 : _owner._game.Adventure.Level.Number / 10;
            DrawImage(note % 2 == 0 ? "background2.jpg" : "background1.jpg", new Rect(-700, -300, 2800, 1200));
            var paper = _owner.Texture("ZombieNote.jpg");
            if (paper != null)
            {
                CanvasUI.DrawTexture(new Rect(80, 80, paper.width, paper.height), paper);
            }

            var writing = note < 4 ? "ZombieNote" + (note + 1) + ".png" : "Credits_ZombieNote.png";
            var ink = _owner.Texture(writing);
            var x = note == 1 ? 133 : note == 2 ? 120 : note == 3 ? 102 : note == 4 ? 114 : 131;
            var y = note == 1 ? 127 : note == 2 || note == 3 ? 117 : note == 4 ? 138 : 132;
            if (ink != null)
            {
                CanvasUI.DrawTexture(new Rect(x, y, ink.width, ink.height), ink);
            }

            Label(new Rect(200, 40, 400, 40), OriginalProgressionText("FOUND_NOTE"), 22, new Color(1, 200f / 255, 0), TextAnchor.MiddleCenter, true);
            var next = new Rect(324, 500, 156, 42);
            DrawAdventureAwardButton(next, award.IntroductionTicks >= 180);
            var continueText = OriginalProgressionText("CONTINUE_BUTTON");
            if (!OriginalUiWidgets.TextAtBaseline(continueText, next.center.x, next.y + 27, next.width, "DwarvenTodcraft15", new Color(213f / 255, 159f / 255, 43f / 255)))
            {
                Label(next, continueText, 18, new Color(213f / 255, 159f / 255, 43f / 255), TextAnchor.MiddleCenter, true);
            }

            if (award.IntroductionAlpha > 0)
            {
                Fill(new Rect(0, 0, 800, 600), new Color(0, 0, 0, award.IntroductionAlpha));
            }
        }

        /// <summary>Performs adventure award text.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "title">The title.</param>
        /// <param name = "name">The name.</param>
        /// <param name = "description">The description.</param>
        private void AdventureAwardText(int kind, out string title, out string name, out string description)
        {
            string titleKey, nameKey, descriptionKey;
            switch (kind)
            {
                case 9:
                    titleKey = "GOT_SHOVEL";
                    nameKey = "SHOVEL";
                    descriptionKey = "SHOVEL_DESCRIPTION";
                    break;
                case 10:
                    titleKey = "FOUND_SUBURBAN_ALMANAC";
                    nameKey = "SUBURBAN_ALMANAC";
                    descriptionKey = "SUBURBAN_ALMANAC_DESCRIPTION";
                    break;
                case 11:
                    titleKey = "FOUND_KEYS";
                    nameKey = "KEYS";
                    descriptionKey = "KEYS_DESCRIPTION";
                    break;
                case 13:
                    titleKey = "FOUND_WATERING_CAN";
                    nameKey = "WATERING_CAN";
                    descriptionKey = "WATERING_CAN_DESCRIPTION";
                    break;
                case 14:
                    titleKey = "FOUND_TACO";
                    nameKey = "TACO";
                    descriptionKey = "TACO_DESCRIPTION";
                    break;
                case 21:
                    titleKey = "WIN_MESSAGE1";
                    nameKey = "SILVER_SUNFLOWER_TROPHY";
                    descriptionKey = "WIN_MESSAGE2";
                    break;
                default:
                    titleKey = "BEAT_GAME_MESSAGE1";
                    nameKey = "GOLD_SUNFLOWER_TROPHY";
                    descriptionKey = "BEAT_GAME_MESSAGE2";
                    break;
            }

            title = OriginalProgressionText(titleKey);
            name = OriginalProgressionText(nameKey);
            description = OriginalProgressionText(descriptionKey);
        }

        /// <summary>Stores seed bank particles.</summary>
        private OriginalParticleRenderer _seedBankParticles;
        /// <summary>Stores seed bank presented game.</summary>
        private GameSimulation _seedBankPresentedGame;
        /// <summary>Performs draw adventure packet.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "card">The card.</param>
        private void DrawAdventurePacket(PlantKind kind, Rect card)
        {
            var context = _owner._game.AdventurePacketContext(kind, destination: _packetContext);
            context.Picked = _owner._selected >= 0 && _owner._selected < _owner._deck.Count && _owner._deck[_owner._selected] == kind;
            var visual = AdventureSeedBankPresentation.Sample(context);
            var previous = CanvasUI.Color;
            try
            {
                var shade = visual.BaseColor / 255f;
                CanvasUI.Color = new Color(previous.r * shade, previous.g * shade, previous.b * shade, previous.a);
                DrawAdventureCard(kind, card, false);
                if (visual.DrawMask)
                {
                    CanvasUI.Color = new Color(previous.r * visual.MaskColor / 255f, previous.g * visual.MaskColor / 255f, previous.b * visual.MaskColor / 255f, previous.a);
                    _maskKind = kind;
                    _maskCard = card;
                    _owner._animations.DrawNativeClipped(_presentationKeys.Get("seed-cooldown-", (int)kind), card, _drawPacketMask,
                        topCrop: Math.Min(card.height, visual.MaskHeight));
                }
            }
            finally
            {
                CanvasUI.Color = previous;
            }

            if (visual.DrawCost)
            {
                DrawAdventurePacketCost(kind, card);
            }
        }

        /// <summary>Submits the immediately configured cooldown mask without a captured delegate.</summary>
        /// <returns>Whether packet artwork was submitted.</returns>
        private bool DrawAdventurePacketMask()
        {
            DrawAdventureCard(_maskKind, _maskCard, false);
            return true;
        }

        /// <summary>Performs draw adventure conveyor packet.</summary>
        /// <param name = "packet">The packet.</param>
        /// <param name = "card">The card.</param>
        private void DrawAdventureConveyorPacket(AdventurePacket packet, Rect card)
        {
            var previous = CanvasUI.Color;
            try
            {
                if (_owner._selectedAdventurePacket == packet.Id)
                {
                    CanvasUI.Color = new Color(previous.r * 64f / 255f, previous.g * 64f / 255f, previous.b * 64f / 255f, previous.a);
                }

                DrawAdventureCard(packet.Kind, card, false);
            }
            finally
            {
                CanvasUI.Color = previous;
            }
        }

        /// <summary>Performs draw adventure packet cost.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "card">The card.</param>
        private void DrawAdventurePacketCost(PlantKind kind, Rect card)
        {
            Label(new Rect(card.x + 2, card.y + 53, 43, 15), NumberLabel(_owner._game.AdventureSeedCost(kind)), 13, Color.black, TextAnchor.MiddleRight, true);
        }

        /// <summary>Performs draw adventure packet ready flashes.</summary>
        private void DrawAdventurePacketReadyFlashes()
        {
            if (!ReferenceEquals(_seedBankPresentedGame, _owner._game))
            {
                _seedBankPresentedGame = _owner._game;
                _seedBankParticles = new OriginalParticleRenderer(_owner._root, _owner._animations);
            }

            _seedBankParticles.BeginFrame();
            foreach (var flash in _owner._game.Adventure.SeedFlashes)
            {
                for (var i = 0; i < _owner._deck.Count; i++)
                {
                    if (_owner._deck[i] == flash.Kind)
                    {
                        var card = _owner.IsModeBoard ? _owner.ModeSeedRect(i) : _owner.AdventureSeedRect(i);
                        _seedBankParticles.Draw(_presentationKeys.Get("packet-ready-", flash.Id), AdventureSeedPacketFlash.Asset, flash.Age + AdventureRenderRemainder, new Vector2(card.x, card.y), flash.Id);
                        break;
                    }
                }
            }

            _seedBankParticles.EndFrame();
        }

        /// <summary>Performs adventure first wave tutorial advice.</summary>
        /// <returns>The resulting adventure first wave tutorial advice.</returns>
        private string AdventureFirstWaveTutorialAdvice()
        {
            var board = _owner._game.Adventure;
            if (board.Level.IsReplay || board.CurrentWave != 0 || board.Phase != AdventurePhase.Playing)
            {
                return null;
            }

            int peas = 0, flowers = 0;
            foreach (Plant plant in _owner._game.Plants)
            {
                if (plant.Kind == PlantKind.Peashooter)
                {
                    peas++;
                }

                if (plant.Kind == PlantKind.Sunflower)
                {
                    flowers++;
                }
            }

            // These original tutorial messages explain the same accepted-plant thresholds as the PC scheduler.
            if (board.Level.Number == 1 && peas == 1)
            {
                if (_owner._selected >= 0)
                {
                    return OriginalProgressionText("ADVICE_CLICK_ON_GRASS");
                }

                if (_owner._game.AdventureSunAvailable >= 100 && _owner._game.CooldownRemaining(PlantKind.Peashooter) <= 0)
                {
                    return OriginalProgressionText("ADVICE_CLICK_PEASHOOTER");
                }

                return OriginalProgressionText(board._collectedSun ? "ADVICE_CLICKED_ON_SUN" : "ADVICE_CLICK_ON_SUN");
            }

            if (board.Level.Number == 2 && flowers < 3)
            {
                return OriginalProgressionText("ADVICE_PLANT_SUNFLOWER2");
            }

            return null;
        }
        /// <summary>Stores huge wave text animation.</summary>
        private AdventureWaveTextAnimation _hugeWaveTextAnimation;
        /// <summary>Stores huge wave text.</summary>
        private string _hugeWaveText;
        /// <summary>Stores huge wave glyphs.</summary>
        private readonly List<BitmapTextLayout> _hugeWaveGlyphs = new List<BitmapTextLayout>();
        /// <summary>Stores huge wave text width.</summary>
        private float _hugeWaveTextWidth;
        /// <summary>Stores bundled wave root.</summary>
        private string _bundledWaveRoot;

        /// <summary>Performs draw adventure wave notices.</summary>
        private void DrawAdventureWaveNotices()
        {
            if (_owner._game == null || !_owner._game.IsAdventure || _owner._choosingSeeds || _owner.AdventureIntroActive)
            {
                return;
            }

            if (_owner._waveNoticeGame != _owner._game)
            {
                _owner.InitializeAdventureWaveNotices();
            }

            if (_owner._hugeWaveNoticeStart >= 0f)
            {
                var age = Math.Max(0, (int)Math.Round((_owner._game.Elapsed - _owner._hugeWaveNoticeStart) * 100d));
                if (age < AdventureWaveNoticeRules.HugeDuration)
                {
                    DrawOriginalHugeWaveText(age);
                }
            }

            if (_owner._finalWaveNoticeStart >= 0f)
            {
                var age = Math.Max(0, _owner._game.Elapsed - _owner._finalWaveNoticeStart);
                var renderer = OriginalFinalWaveRenderer();
                global::PlantsVsZombies.Reanimation.NativeAnimationInfo info;
                if (renderer != null && renderer.TryGetNativeAnimationInfo("FinalWave", null, out info) && age < info.Count / info.SourceRate)
                {
                    renderer.DrawNativeDefinition("FinalWave", new Vector2(0, 30), 1f, age, false);
                }
            }
        }

        /// <summary>Performs ensure original wave files.</summary>
        /// <returns>The resulting ensure original wave files.</returns>
        private string EnsureOriginalWaveFiles()
        {
            if (_bundledWaveRoot != null)
            {
                return _bundledWaveRoot;
            }

            var text = Resources.Load<TextAsset>("PvZWaveNotices/TextFadeOn");
            var final = Resources.Load<TextAsset>("PvZWaveNotices/FinalWave");
            var image = Resources.Load<TextAsset>("PvZWaveNotices/FinalWaveImage");
            if (text == null || final == null || image == null)
            {
                return null;
            }

            // Existing renderer consumes original disk definitions. Materialize ONLY these byte-identical
            // bundled originals in our own cache; never alter StreamingAssets or another renderer's index.
            var cache = Path.Combine(Application.temporaryCachePath, "pvz-original-wave-notices-20261110");
            var reanim = Path.Combine(cache, "reanim");
            Directory.CreateDirectory(reanim);
            File.WriteAllBytes(Path.Combine(reanim, "TextFadeOn.reanim"), text.bytes);
            File.WriteAllBytes(Path.Combine(reanim, "FinalWave.reanim"), final.bytes);
            File.WriteAllBytes(Path.Combine(reanim, "FinalWave.png"), image.bytes);
            _bundledWaveRoot = cache;
            return cache;
        }

        /// <summary>Performs original final wave renderer.</summary>
        /// <returns>The resulting original final wave renderer.</returns>
        private global::PlantsVsZombies.Reanimation.ReanimationRenderer OriginalFinalWaveRenderer()
        {
            if (File.Exists(Path.Combine(_owner._root, "reanim", "FinalWave.reanim")) && File.Exists(Path.Combine(_owner._root, "reanim", "FinalWave.png")))
            {
                return _owner._animations;
            }

            if (_owner._bundledWaveAnimations == null)
            {
                var bundled = EnsureOriginalWaveFiles();
                if (bundled == null)
                {
                    return null;
                }

                _owner._bundledWaveAnimations = new global::PlantsVsZombies.Reanimation.ReanimationRenderer(bundled);
            }

            return _owner._bundledWaveAnimations;
        }

        /// <summary>Performs draw original huge wave text.</summary>
        /// <param name = "age">The age.</param>
        private void DrawOriginalHugeWaveText(int age)
        {
            if (_owner._modeTitleBitmap == null)
            {
                return; // existing HouseofTerror28 atlas; no OS-font substitute.
            }

            if (_hugeWaveTextAnimation == null)
            {
                var path = Path.Combine(_owner._root, "reanim", "TextFadeOn.reanim");
                if (!File.Exists(path))
                {
                    var bundled = EnsureOriginalWaveFiles();
                    if (bundled == null)
                    {
                        return;
                    }

                    path = Path.Combine(bundled, "reanim", "TextFadeOn.reanim");
                }

                _hugeWaveTextAnimation = AdventureWaveTextAnimation.Load(path);
            }

            if (_hugeWaveText == null)
            {
                _hugeWaveText = OriginalProgressionText("ADVICE_HUGE_WAVE");
                _hugeWaveTextWidth = _owner._modeTitleBitmap.Layout(_hugeWaveText).Width;
                foreach (char c in _hugeWaveText)
                {
                    _hugeWaveGlyphs.Add(_owner._modeTitleBitmap.Layout(c.ToString()));
                }
            }

            var offset = -_hugeWaveTextWidth * .5f;
            var old = CanvasUI.Matrix;
            try
            {
                var slide = 100 + _hugeWaveText.Length;
                for (var i = 0; i < _hugeWaveGlyphs.Count; i++)
                {
                    var pose = _hugeWaveTextAnimation.Sample(age, i, _hugeWaveText.Length);
                    var x = pose.X + offset;
                    if (age < slide)
                    {
                        x += offset * (1f - pose.Phase);
                    }

                    var y = pose.Y + AdventureWaveNoticeRules.Baseline - 300;
                    if (pose.Alpha > 0f && pose.ScaleX != 0f && pose.ScaleY != 0f)
                    {
                        CanvasUI.Matrix = old * Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.identity, new Vector3(pose.ScaleX, pose.ScaleY, 1));
                        _owner._modeTitleBitmap.Draw(_hugeWaveGlyphs[i], 0, -_owner._modeTitleBitmap.Data.Base, new Color(1, 0, 0, pose.Alpha));
                    }

                    BmFontGlyph glyph;
                    if (_owner._modeTitleBitmap.Data.TryGetGlyph(_hugeWaveText[i], out glyph))
                    {
                        offset += glyph.XAdvance;
                    }
                }
            }
            finally
            {
                CanvasUI.Matrix = old;
            }
        }

        /// <summary>Performs draw adventure progress.</summary>
        private void DrawAdventureProgress()
        {
            if (_owner.UseLegacyBoard || _owner._profiles == null || !_owner._profiles.SelectedId.HasValue || !_owner.EnsureAdventureStore())
            {
                return;
            }

            var progress = _owner._adventureStore.Get(_owner._profiles.SelectedId.Value);
            var text = progress.Completed ? "冒险模式已通关 · 可开始下一周目" : "冒险第 " + progress.Cycle + " 周目  " + AdventureLevels.Get(progress.NextLevel).Label;
            Label(new Rect(500, 151, 195, 25), text, 17, new Color(1f, 0.94f, 0.54f), TextAnchor.MiddleCenter, true);
        }

        /// <summary>Performs draw adventure card.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "card">The card.</param>
        /// <param name = "price">The price.</param>
        private void DrawAdventureCard(PlantKind kind, Rect card, bool price)
        {
            var bank = _owner.Texture("seeds.png");
            if (card.width > 50.01f && _owner.Texture("SeedPacket_Larger.png") != null)
            {
                DrawImage("SeedPacket_Larger.png", card);
            }
            else if (bank != null)
            {
                _owner._animations.DrawNativeImage(bank, card, new Rect(2f / 9f, 0, 1f / 9f, 1));
            }

            var id = (int)kind;
            var imitated = kind == PlantKind.Imitater && _owner._game.Adventure.ImitaterTarget.HasValue;
            if (imitated)
            {
                id = (int)_owner._game.Adventure.ImitaterTarget.Value;
            }

            if (id >= 0 && id < 49)
            {
                var cell = AlmanacPortraitLayout.PlantAtlasCell(id);
                var atlas = cell >= 0 ? _owner.Texture("packet_plants.png") : null;
                if (atlas != null)
                {
                    var source = new Rect(cell / 13f, 0, 1f / 13f, 1);
                    if (imitated)
                    {
                        _cardAtlas = atlas;
                        _cardDestination = card;
                        _cardSource = source;
                        _owner._animations.DrawNativeClipped(_presentationKeys.Get("imitater-card-", (int)card.x, (int)card.y), card, _drawCardImage, false, true);
                    }
                    else
                    {
                        _owner._animations.DrawNativeImage(atlas, card, source);
                    }
                }
                else
                {
                    Vector2 origin;
                    float scale;
                    AlmanacPortraitLayout.PlantThumbnailTransform(id, new Rect(0, 0, 50, 70), out origin, out scale);
                    origin = new Vector2(card.x + origin.x * card.width / 50f, card.y + origin.y * card.height / 70f);
                    scale *= card.width / 50f;
                    var iconKind = imitated ? _owner._game.Adventure.ImitaterTarget.Value : kind;
                    if (imitated)
                    {
                        ActorRenderer.DrawCursor(AdventurePlantAnimation(iconKind), origin, scale, null, false, true,
                            _presentationKeys.Get("imitater-card-", (int)card.x, (int)card.y), card);
                    }
                    else
                    {
                        _owner._animations.DrawNative(AdventurePlantAnimation(iconKind), origin, scale, 0f);
                    }
                }
            }
            else
            {
                var previous = CanvasUI.Color;
                // PC ReanimatorCache::MakeCachedPlant 0x47E9A0 uses RGB(255,64,64).
                CanvasUI.Color = new Color(previous.r, previous.g * 64f / 255f, previous.b * 64f / 255f, previous.a);
                Vector2 origin;
                float scale;
                AlmanacPortraitLayout.PlantThumbnailTransform((int)PlantKind.Wallnut, new Rect(0, 0, 50, 70), out origin, out scale);
                origin = new Vector2(card.x + origin.x * card.width / 50f, card.y + origin.y * card.height / 70f);
                _owner._animations.DrawNative("Wallnut", origin, scale * card.width / 50f, 0f, "anim_idle");
                CanvasUI.Color = previous;
            }

            if (price)
            {
                Label(new Rect(card.x + 2 * card.width / 50f, card.y + 53 * card.height / 70f, 43 * card.width / 50f, 15 * card.height / 70f), kind == PlantKind.Imitater && !_owner._game.Adventure.ImitaterTarget.HasValue ? "?" : NumberLabel(_owner._game.AdventureSeedCost(kind)), Mathf.RoundToInt(13 * card.width / 50f), Color.black, TextAnchor.MiddleRight, true);
            }
        }

        /// <summary>Submits the immediately configured packet artwork for grayscale composition.</summary>
        /// <returns>Whether packet artwork was submitted.</returns>
        private bool DrawCardImage()
        {
            return _owner._animations.DrawNativeImage(_cardAtlas, _cardDestination, _cardSource);
        }

        /// <summary>Performs draw adventure interface.</summary>
        private void DrawAdventureInterface()
        {
            var board = _owner._game.Adventure;
            if (DrawAdventureOpeningInterface())
            {
                return;
            }

            var packets = board.Level.HasConveyor || board.Level.Mode == AdventureMode.Vasebreaker;
            if (_owner._game.UsesNativeAdventureConveyor)
            {
                DrawAdventureConveyorBackground();
            }
            else if (packets)
            {
                DrawImage("ConveyorBelt_backdrop.png", new Rect(0, 0, 516, 87));
            }
            else
            {
                DrawAdventureSeedBankBackground(Vector2.zero, _owner._deck.Count);
            }

            if (packets)
            {
                DrawAdventureConveyorPackets();
            }
            else
            {
                Label(new Rect(8, 59, 60, 22), NumberLabel(_owner._game.SunAmount), 18, Color.black, TextAnchor.MiddleCenter, true);
                for (var i = 0; i < _owner._deck.Count; i++)
                {
                    var card = _owner.AdventureSeedRect(i);
                    DrawAdventurePacket(_owner._deck[i], card);
                }
            }

            if (board.Level.Number >= 5)
            {
                DrawImage("ShovelBank.png", PvZGame.s_adventureShovelButton);
                DrawImage("Shovel.png", PvZGame.s_adventureShovelButton);
                if (_owner._shovel)
                {
                    Outline(PvZGame.s_adventureShovelButton, Color.yellow, 3);
                }
            }

            DrawOriginalUiButton(PvZGame.s_adventureMenuButton, "菜单");
            if (!packets)
            {
                Label(new Rect(535, 6, 62, 28), board.Level.Label, 19, Color.white, TextAnchor.MiddleCenter, true);
            }

            if (board.Level.Mode == AdventureMode.Boss)
            {
                Fill(new Rect(490, 570, 290, 18), new Color(0.1f, 0.05f, 0.05f, 0.9f));
                Fill(new Rect(492, 572, 286f * board.BossHealth / board.BossMaxHealth, 14), new Color(0.76f, 0.12f, 0.08f));
            }
            else
            {
                if (!ReferenceEquals(_captionBoard, board) || _captionWave != board.CurrentWave)
                {
                    _captionBoard = board;
                    _captionWave = board.CurrentWave;
                    _waveCaption = "关卡 " + board.Level.Label + "    " + board.CurrentWave + " / " + board.Waves.Count;
                }

                Label(new Rect(600, 569, 190, 25), _waveCaption, 16, Color.white, TextAnchor.MiddleRight, true);
            }

            if (board.MoneyCollectedUnits > 0 || _owner._game.Elapsed < _walletVisibleUntil)
            {
                var wallet = ReadDisplayAccount();
                if (wallet != null)
                {
                    DrawImage("coinbank.png", new Rect(16, 559, 128, 31));
                    Label(new Rect(22, 561, 112, 26), "$" + wallet.DisplayCoins.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), 17, new Color(.7f, 1f, .35f), TextAnchor.MiddleRight, true);
                }
            }

            if (board.SelectedCobCannonId.HasValue)
            {
                DrawImage("CobCannon_target.png", new Rect(_owner._adventurePointer.x - 36, _owner._adventurePointer.y - 36, 72, 72));
            }

            var advice = board.SelectedCobCannonId.HasValue ? "点击目标位置发射，右键取消瞄准。" : AdventureFirstWaveTutorialAdvice() ?? PvZGame.AdventureTutorialText(board.Tutorial);
            if (_owner._adventureFeedback != null && Time.unscaledTime < _owner._adventureFeedbackUntil)
            {
                advice = _owner._adventureFeedback;
            }

            if (!string.IsNullOrEmpty(advice) && board.Phase == AdventurePhase.Playing)
            {
                DrawOriginalUiAdvice(new Rect(175, 94, 450, 36), advice);
                if (board.Level.Number > 2 && board.Tutorial != AdventureTutorial.Shovel && board.Tutorial != AdventureTutorial.Complete)
                {
                    DrawOriginalUiButton(new Rect(706, 86, 80, 32), "知道了");
                }
            }

            DrawAdventureWaveNotices();
            DrawAdventurePacketReadyFlashes();
            DrawCollectedSunsAboveSeedBank();
            if (_owner._choosingSeeds)
            {
                DrawAdventureChooser();
            }
            else if (board.Phase == AdventurePhase.Reward || board.Phase == AdventurePhase.Finished)
            {
                DrawAdventureReward();
            }
            else if (_owner._game.State == MatchState.Lost)
            {
                DrawDialog("僵尸吃掉了你的脑子！", "重新开始", false);
            }
            else if (_owner._game.State == MatchState.Paused && !_owner._optionsView.IsOpen)
            {
                DrawDialog("已暂停", "继续游戏", true);
            }
        }

        /// <summary>Performs draw adventure chooser.</summary>
        private void DrawAdventureChooser()
        {
            if (_owner._adventureIntro == null)
            {
                DrawLegacyAdventureChooser();
                return;
            }

            DrawImage("SeedChooser_Background.png", new Rect(0, 87, 465, 513));
            var title = OriginalProgressionText("CHOOSE_YOUR_PLANTS");
            var nativeGold = new Color(213f / 255f, 159f / 255f, 43f / 255f);
            if (!DrawBitmap(_owner._optionsBitmap, title, 229, 110, 440, nativeGold, true))
            {
                Label(new Rect(18, 87, 425, 29), title, 19, nativeGold, TextAnchor.MiddleCenter, true);
            }

            var seeds = _owner._choosingImitater ? _owner._game.Adventure.Level.UnlockedSeeds : _owner._game.Adventure.AvailableSeeds;
            for (var seedIndex = 0; seedIndex < seeds.Count; seedIndex++)
            {
                var seed = seeds[seedIndex];
                var rect = _owner.AdventureCampaignChooserRect(seed);
                var previous = CanvasUI.Color;
                if (!_owner._choosingImitater && _owner._pendingSeeds.Contains(seed))
                {
                    CanvasUI.Color = new Color(previous.r * 55f / 255f, previous.g * 55f / 255f, previous.b * 55f / 255f, previous.a);
                }

                if (seed == PlantKind.Imitater)
                {
                    DrawImage("SeedChooser_ImitaterAddOn.png", new Rect(459, 503, 66, 93));
                }

                DrawAdventureCard(seed, rect, true);
                CanvasUI.Color = previous;
            }

            var required = Math.Min(_owner._game.Adventure.Level.PacketSlots, _owner._game.Adventure.AvailableSeeds.Count);
            var enabled = _owner._choosingImitater || _owner._pendingSeeds.Count == required;
            DrawImage(enabled ? "SeedChooser_Button.png" : "SeedChooser_Button_Disabled.png", _owner.AdventureChooserStartRect);
            var caption = _owner._choosingImitater ? OriginalProgressionText("CANCEL") : OriginalProgressionText("LETS_ROCK_BUTTON");
            if (!DrawBitmap(_owner._optionsBitmap, caption, _owner.AdventureChooserStartRect.center.x, 572, 150, nativeGold, true))
            {
                Label(_owner.AdventureChooserStartRect, caption, 18, nativeGold, TextAnchor.MiddleCenter, true);
            }
        }

        /// <summary>Performs draw legacy adventure chooser.</summary>
        private void DrawLegacyAdventureChooser()
        {
            DrawImage("SeedChooser_Background.png", new Rect(85, 70, 630, 505));
            Label(new Rect(140, 78, 520, 37), "关卡 " + _owner._game.Adventure.Level.Label + "  选择你的植物", 25, Color.white, TextAnchor.MiddleCenter, true);
            var unlocked = _owner._game.Adventure.AvailableSeeds;
            for (var i = 0; i < unlocked.Count; i++)
            {
                var card = PvZGame.AdventureChooserRect(i);
                DrawAdventureCard(unlocked[i], card, true);
                if (_owner._pendingSeeds.Contains(unlocked[i]))
                {
                    Outline(card, _owner.ContainsRequiredSeed(unlocked[i]) ? new Color(1f, 0.5f, 0.2f) : Color.yellow, 3);
                }
            }

            var required = Math.Min(_owner._game.Adventure.Level.PacketSlots, unlocked.Count);
            DrawOriginalUiButton(new Rect(300, 533, 200, 34), "开始战斗  " + _owner._pendingSeeds.Count + "/" + required, _owner._pendingSeeds.Count == required);
            if (_owner._choosingImitater)
            {
                DrawImage("SeedChooser_Background.png", new Rect(85, 70, 630, 505));
                Label(new Rect(140, 78, 520, 37), "模仿哪一种植物？", 25, Color.white, TextAnchor.MiddleCenter, true);
                var targets = _owner._game.Adventure.Level.UnlockedSeeds;
                for (var i = 0; i < targets.Count; i++)
                {
                    DrawAdventureCard(targets[i], PvZGame.AdventureChooserRect(i), true);
                }

                DrawOriginalUiButton(new Rect(300, 533, 200, 34), "取消");
            }
        }

        /// <summary>Performs draw original loading.</summary>
        /// <returns>The resulting draw original loading.</returns>
        private bool DrawOriginalLoading()
        {
            if (!_owner._originalLoadingActive)
            {
                return false;
            }

            var oldMatrix = CanvasUI.Matrix;
            var oldColor = CanvasUI.Color;
            CanvasUI.Matrix = Matrix4x4.identity;
            CanvasUI.Color = Color.black;
            CanvasUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            float ox = (Screen.width - 800f * scale) * 0.5f, oy = (Screen.height - 600f * scale) * 0.5f;
            CanvasUI.Matrix = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            CanvasUI.Color = Color.white;
            _owner._originalLoadingFirstDraw = true;

            if (_owner._originalTitleTicks <= 400)
            {
                var alpha = _owner._originalTitleTicks < 50 ? _owner._originalTitleTicks / 50f : _owner._originalTitleTicks > 350 ? (400 - _owner._originalTitleTicks) / 50f : 1f;
                CanvasUI.Color = new Color(1, 1, 1, Mathf.Clamp01(alpha));
                DrawOriginalTitleTexture("popcap", new Rect(250, 150, 300, 300));
            }
            else
            {
                DrawOriginalTitleTexture("title", new Rect(0, 0, 800, 600));
                var ticks = Math.Max(0, _owner._originalTitleScreenTicks);
                var logoY = ticks > 60 ? PvZGame.OriginalTitleCurve(100, 60, ticks, -150, 10, false) : PvZGame.OriginalTitleCurve(60, 50, ticks, 10, 15, true);
                DrawOriginalTitleTexture("logo", new Rect(50, logoY, 700, 116));
                var buttonY = ticks > 10 ? PvZGame.OriginalTitleCurve(60, 10, ticks, 650, 534, false) : PvZGame.OriginalTitleCurve(10, 0, ticks, 534, 529, true);
                var grassY = buttonY - 17f;
                DrawOriginalTitleTexture("dirt", new Rect(244, grassY + 18, 321, 53));
                // PC 4A3777 truncates width before ClipRect. Crop UVs without stretching
                // or changing the IMGUI clip origin, preserving the native 314x33 pixels.
                DrawOriginalLoadingGrass(240f, grassY, _owner._originalLoadWidth);
                if (_owner._originalLoadWidth < 314f)
                {
                    var travel = _owner._originalLoadWidth * .94f;
                    var capScale = Mathf.Lerp(1f, .5f, _owner._originalLoadWidth / 314f);
                    float x = 251f + travel, y = grassY - 3f - 35f * capScale + 35f;
                    Texture2D cap;
                    if (_owner._originalTitleTextures.TryGetValue("sodroll", out cap) && cap != null)
                    {
                        var beforeCap = CanvasUI.Matrix;
                        // Literal PC TodScaleRotateTransformMatrix, with its native image origin.
                        CanvasUI.Matrix = beforeCap * Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.Euler(0, 0, -travel * 2f), new Vector3(capScale, capScale, 1));
                        CanvasUI.DrawTexture(new Rect(-cap.width * .5f, -cap.height * .5f, cap.width, cap.height), cap, ScaleMode.StretchToFill, true);
                        CanvasUI.Matrix = beforeCap;
                    }
                }

                for (var i = 0; i < _owner._originalLoadSproutAges.Length; i++)
                {
                    DrawOriginalLoadSprout(i);
                }

                var ready = _owner.OriginalLoadingReady; // PC enables the start label on real completion, independently of bar smoothing.
                var button = new Rect(240, buttonY, 314, 50);
                var pointer = CanvasUI.PointerPosition;
                Color label = ready && button.Contains(pointer) ? new Color32(250, 90, 15, 255) : new Color32(218, 184, 33, 255);
                var text = ready ? "点击开始！" : "读取中…";
                var baseline = buttonY + (50f + (_owner._originalLoadingFont == null ? 15f : _owner._originalLoadingFont.Data.Base)) * 0.5f - 1f;
                if (!DrawBitmap(_owner._originalLoadingFont, text, 397, baseline, 314, label, true))
                {
                    Label(button, text, 16, label, TextAnchor.UpperCenter, false);
                }


            }

            CanvasUI.Color = oldColor;
            CanvasUI.Matrix = oldMatrix;
            return true;
        }

        /// <summary>Performs draw original load sprout.</summary>
        /// <param name = "index">The index.</param>
        private void DrawOriginalLoadSprout(int index)
        {
            if (_owner._originalLoadSproutAges[index] < 0f)
            {
                return;
            }

            var head = index == 4;
            var info = head ? _owner._originalHeadInfo : _owner._originalSproutInfo;
            if (info.FrameCount < 1 || info.FramesPerSecond <= 0f)
            {
                return;
            }

            var x = 225f + _owner._originalLoadTriggers[index] * 314f - (head ? 20f : 0f);
            var y = index == 2 ? 506f : 511f;
            var sx = index == 1 || index == 3 ? -1f : index == 2 ? 1.1f : 1f;
            var sy = index == 2 ? 1.3f : 1f;
            var seconds = _owner._originalLoadSproutAges[index];
            var before = CanvasUI.Matrix;
            CanvasUI.Matrix = before * Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.identity, new Vector3(sx, sy, 1));
            // PC creates the full 18-frame definition, rate 18, PlayOnceAndHold.
            // Native playback owns frame normalization; do not convert/truncate it a second time.
            _owner._animations.DrawNativeDefinition(head ? "LoadBar_Zombiehead" : "LoadBar_sprout", Vector2.zero, 1f, seconds, false, 18f);
            CanvasUI.Matrix = before;
        }

        /// <summary>Performs draw original loading grass.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "width">The width.</param>
        private void DrawOriginalLoadingGrass(float x, float y, float width)
        {
            Texture2D grass;
            if (!_owner._originalTitleTextures.TryGetValue("grass", out grass) || grass == null)
            {
                return;
            }

            var pixels = Math.Max(0, Math.Min(314, (int)width));
            if (pixels == 0)
            {
                return;
            }

            CanvasUI.DrawTextureWithTexCoords(new Rect(x, y, pixels, grass.height), grass, new Rect(0, 0, pixels / (float)grass.width, 1), true);
        }

        /// <summary>Performs draw original title texture.</summary>
        /// <param name = "key">The key.</param>
        /// <param name = "rect">The rect.</param>
        private void DrawOriginalTitleTexture(string key, Rect rect)
        {
            Texture2D image;
            if (_owner._originalTitleTextures.TryGetValue(key, out image) && image != null)
            {
                CanvasUI.DrawTexture(rect, image, ScaleMode.StretchToFill, true);
            }
        }

        /// <summary>Performs draw modes interface.</summary>
        /// <returns>The resulting draw modes interface.</returns>
        private bool DrawModesInterface()
        {
            if (!_owner.IsModeBoard)
            {
                return false;
            }

            var s = _owner._modeSession;
            var d = s.Definition;
            if (d.Mechanic == ModeMechanic.Aquarium)
            {
                DrawModeAquarium();
            }

            DrawModeBoardOverlays();
            if (_owner._game.UsesNativeAdventureConveyor)
            {
                DrawAdventureConveyorBackground();
            }
            else
            {
                Fill(new Rect(0, 0, 800, 80), new Color(0.25f, 0.19f, 0.08f, 0.95f));
                Label(new Rect(0, 5, 78, 30), "阳光", 17, Color.yellow, TextAnchor.MiddleCenter, true);
                Label(new Rect(0, 35, 78, 32), NumberLabel(_owner._game.SunAmount), 23, Color.white, TextAnchor.MiddleCenter, true);
            }

            if (d.Mechanic == ModeMechanic.Aquarium)
            {
                ModeButton(new Rect(100, 10, 155, 55), "潜水僵尸 100");
                ModeButton(new Rect(275, 10, 175, 55), "奖杯 1000");
            }
            else if (d.Mechanic == ModeMechanic.Beghouled || d.Mechanic == ModeMechanic.BeghouledTwist)
            {
                string[] names =
                {
                    "双发 1000",
                    "大喷 500",
                    "高坚果 250",
                    "重排 100",
                    "填坑 200"
                };
                for (var i = 0; i < 5; i++)
                {
                    ModeButton(new Rect(80 + i * 98, 8, 92, 60), names[i]);
                }
            }
            else if (d.Mechanic == ModeMechanic.IZombie)
            {
                for (var i = 0; i < s.ZombieCards.Count; i++)
                {
                    var k = s.ZombieCards[i];
                    var card = PvZGame.ModeZombieCardRect(i);
                    DrawNativeModeZombieCard(k, card);
                    if (_owner._selected == i)
                    {
                        Outline(card, Color.yellow, 3);
                    }
                }
            }
            else if (d.UsesPackets)
            {
                if (d.Mechanic != ModeMechanic.RainingSeeds)
                {
                    DrawAdventureConveyorPackets(true);
                }

                if (d.Mechanic == ModeMechanic.Vases)
                {
                    var card = new Rect(550, 8, 47, 64);
                    DrawAdventureCard(PlantKind.CherryBomb, card, true);
                    if (_owner._selected == 0)
                    {
                        Outline(card, Color.yellow, 3);
                    }
                }
            }
            else
            {
                for (var i = 0; i < _owner._deck.Count; i++)
                {
                    var card = _owner.ModeSeedRect(i);
                    DrawAdventurePacket(_owner._deck[i], card);
                }
            }

            if (d.Mechanic == ModeMechanic.Slots)
            {
                ModeButton(new Rect(500, 8, 95, 64), s.SlotRollingRemaining > 0 ? "转动中" : "拉动 25");
            }

            if (d.Mechanic != ModeMechanic.IZombie && d.Mechanic != ModeMechanic.Aquarium && d.Mechanic != ModeMechanic.Beghouled && d.Mechanic != ModeMechanic.BeghouledTwist)
            {
                DrawImage("ShovelBank.png", PvZGame.s_adventureShovelButton);
                DrawImage("Shovel.png", PvZGame.s_adventureShovelButton);
                if (_owner._shovel)
                {
                    Outline(PvZGame.s_adventureShovelButton, Color.yellow, 3);
                }
            }

            ModeButton(PvZGame.s_adventureMenuButton, "菜单");
            Fill(new Rect(0, 568, 800, 32), new Color(0.06f, 0.1f, 0.04f, 0.9f));
            var status = d.Survival ? "旗帜 " + s.FlagsCompleted + " / " + (d.Endless ? "∞" : (d.Hard ? 10 : 5).ToString()) : d.Endless ? "连续通过 " + s.ClearedStages : d.Mechanic == ModeMechanic.Stars ? "杨桃 " + _owner._game.ModeStarCount() + " / 14" : d.Mechanic == ModeMechanic.Beghouled || d.Mechanic == ModeMechanic.BeghouledTwist ? "配对 " + s.Score + " / 75" : d.Mechanic == ModeMechanic.IZombie ? "大脑 " + Array.FindAll(s.Brains, x => x).Length + " / 5" : "波次 " + _owner._game.Adventure.CurrentWave + " / " + _owner._game.Adventure.Waves.Count;
            Label(new Rect(8, 570, 605, 26), d.Name + "  " + status, 16, Color.white, TextAnchor.MiddleLeft, true);
            ModeButton(PvZGame.s_modeBackButton, "返回模式选择");
            var advice = s.Advice;
            if (_owner._adventureFeedback != null && Time.unscaledTime < _owner._adventureFeedbackUntil)
            {
                advice = _owner._adventureFeedback;
            }

            if (_owner._modeSaveError != null)
            {
                advice = _owner._modeSaveError;
            }

            if (!string.IsNullOrEmpty(advice) && s.Phase != ModePhase.ChoosingSeeds)
            {
                DrawOriginalUiAdvice(new Rect(125, 83, 550, 40), advice);
                ModeButton(new Rect(690, 86, 100, 32), "知道了");
            }

            if (s.Phase == ModePhase.Preparing)
            {
                ModeButton(new Rect(295, 535, 210, 34), s.Stage == 1 ? "开始进攻" : "继续进攻");
            }

            DrawAdventurePacketReadyFlashes();
            DrawCollectedSunsAboveSeedBank();
            if (s.Phase == ModePhase.ChoosingSeeds)
            {
                DrawModeChooser();
            }

            if (s.HasNextStage || s.Finished)
            {
                DrawModeResult();
            }

            return true;
        }

        /// <summary>Performs mode button.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        private void ModeButton(Rect rect, string text)
        {
            DrawOriginalUiButton(rect, text);
        }

        /// <summary>Performs draw mode chooser.</summary>
        private void DrawModeChooser()
        {
            Fill(new Rect(0, 80, 800, 488), new Color(0, 0, 0, 0.6f));
            DrawImage("SeedChooser_Background.png", new Rect(100, 84, 600, 486));
            Label(new Rect(130, 90, 540, 30), _owner._modeChoosingImitater ? "选择模仿对象" : "选择植物 " + _owner._pendingSeeds.Count + " / " + Math.Min(_owner._modeSession.PacketSlots, _owner._game.Adventure.AvailableSeeds.Count), 22, Color.yellow, TextAnchor.MiddleCenter, true);
            var seeds = _owner._game.Adventure.AvailableSeeds;
            if (_owner._modeChoosingImitater)
            {
                for (var i = 0; i < 40; i++)
                {
                    DrawAdventureCard((PlantKind)i, PvZGame.AdventureChooserRect(i), true);
                }
            }
            else
            {
                for (var i = 0; i < seeds.Count; i++)
                {
                    var card = PvZGame.AdventureChooserRect(i);
                    DrawAdventureCard(seeds[i], card, true);
                    if (_owner._pendingSeeds.Contains(seeds[i]))
                    {
                        Outline(card, Color.yellow, 3);
                    }
                }
            }

            ModeButton(new Rect(300, 533, 200, 34), _owner._modeChoosingImitater ? "取消" : "开始游戏");
        }

        /// <summary>Performs draw mode result.</summary>
        private void DrawModeResult()
        {
            var s = _owner._modeSession;
            DrawOriginalUiDialog(new Rect(200, 166, 400, 370), s.HasNextStage ? "本轮完成" : s.Phase == ModePhase.Won ? "挑战成功！" : "挑战结束", null);
            var detail = s.HasNextStage ? "阵型和阳光将保留，准备下一轮。" : s.Phase == ModePhase.Won ? (_owner._modeRewardClaimed ? "奖励已存入金币钱包" : "领取奖励：" + (_owner._modeFirstClear ? 1000 : 250) + "金币") : s.Definition.Endless ? "最高连续通过 " + s.ClearedStages + " 轮" : "可以重新开始这一挑战。";
            if (_owner._modeSaveError != null)
            {
                detail = _owner._modeSaveError;
            }

            Label(new Rect(240, 260, 320, 70), detail, 18, Color.white, TextAnchor.MiddleCenter, true);
            ModeButton(PvZGame.s_modeNextButton, s.HasNextStage ? "继续下一轮" : s.Phase == ModePhase.Won ? (_owner._modeRewardClaimed ? "返回模式选择" : "领取奖励") : "再试一次");
            ModeButton(PvZGame.s_modeRestartButton, "重新开始");
            ModeButton(PvZGame.s_modeExitButton, "返回模式选择");
        }

        /// <summary>Performs draw mode board overlays.</summary>
        private void DrawModeBoardOverlays()
        {
            var s = _owner._modeSession;
            var m = s.Definition.Mechanic;
            if (m == ModeMechanic.Vases)
            {
                foreach (var vase in _owner._game.Adventure.Vases)
                {
                    var lantern = _owner._game.Plants.Exists(p => p.Kind == PlantKind.Plantern && Math.Max(Math.Abs(p.Row - vase.Row), Math.Abs(p.Column - vase.Column)) <= 1);
                    if (!lantern && !vase.ShowsPlant)
                    {
                        continue;
                    }

                    var icon = new Rect(PvZGame.AdventureGridX + vase.Column * PvZGame.CellWidth + 20, _owner.AdventureCellY(vase.Row, vase.Column) + 20, 40, 56);
                    if (vase._seed.HasValue)
                    {
                        DrawAdventureCard(vase._seed.Value, icon, false);
                    }
                    else if (lantern && vase._zombie.HasValue)
                    {
                        _owner._animations.Draw(PvZGame.AdventureZombieAnimation(vase._zombie.Value), icon, _owner._game.Elapsed);
                    }
                    else if (lantern)
                    {
                        _owner._animations.Draw("Sun", icon, _owner._game.Elapsed);
                    }
                }
            }

            if (m == ModeMechanic.IZombie)
            {
                var boundary = s.Definition.Id <= 65 ? 4 : s.Definition.Id == 69 ? 6 : 5;
                Fill(new Rect(PvZGame.AdventureGridX + boundary * PvZGame.CellWidth - 2, 80, 4, 480), Color.red);
                for (var r = 0; r < 5; r++)
                {
                    if (!s.Brains[r])
                    {
                        var rect = new Rect(8, _owner.AdventureCellY(r, 0) + 27, 38, 35);
                        var brain = _owner.Texture("brain.png");
                        if (brain != null)
                        {
                            CanvasUI.DrawTexture(rect, brain);
                        }
                        else
                        {
                            Label(rect, "脑", 23, new Color(1, 0.6f, 0.7f), TextAnchor.MiddleCenter, true);
                        }
                    }
                }
            }

            if (m == ModeMechanic.Stars)
            {
                for (var r = 0; r < 5; r++)
                {
                    for (var c = 0; c < 9; c++)
                    {
                        if (GameSimulation.IsModeStarCell(r, c))
                        {
                            Outline(new Rect(PvZGame.AdventureGridX + c * PvZGame.CellWidth + 8, _owner.AdventureCellY(r, c) + 18, 64, 65), new Color(1, 0.95f, 0.2f, 0.8f), 2);
                        }
                    }
                }
            }

            if (m == ModeMechanic.Beghouled || m == ModeMechanic.BeghouledTwist)
            {
                if (s.SelectedCell >= 0)
                {
                    Outline(new Rect(PvZGame.AdventureGridX + (s.SelectedCell % 9) * PvZGame.CellWidth, _owner.AdventureCellY(s.SelectedCell / 9, s.SelectedCell % 9), 80, 90), Color.yellow, 3);
                }
            }

            if (m == ModeMechanic.Portals)
            {
                foreach (var p in s.Portals)
                {
                    _owner._animations.DrawNative(p.Pair == 0 ? "Portal_Circle" : "Portal_Square", new Vector2(PvZGame.AdventureGridX + p.Column * PvZGame.CellWidth, _owner.AdventureCellY(p.Row, p.Column)), 1f, _owner._game.Elapsed, "anim_pulse");
                }
            }

            if (m == ModeMechanic.RainingSeeds)
            {
                foreach (var rain in s.RainPackets)
                {
                    AdventurePacket packet = null;
                    foreach (var p in _owner._game.Adventure.Packets)
                    {
                        if (p.Id == rain.PacketId)
                        {
                            packet = p;
                            break;
                        }
                    }

                    if (packet == null)
                    {
                        continue;
                    }

                    var rect = PvZGame.ModeRainRect(rain);
                    DrawAdventureConveyorPacket(packet, rect);
                }
            }

            if (m == ModeMechanic.Slots)
            {
                for (var i = 0; i < 3; i++)
                {
                    var symbol = s.SlotSymbols[i];
                    var text = symbol == 56 ? "阳光" : symbol == 57 ? "钻石" : _owner.AdventurePlantName((PlantKind)symbol);
                    Label(new Rect(250 + i * 105, 130, 100, 40), s.SlotRollingRemaining > 0 ? "转动" : text, 19, Color.yellow, TextAnchor.MiddleCenter, true);
                }
            }

            foreach (var shot in s.HeadShots)
            {
                DrawImage("PeaNormal_0.png", new Rect(PvZGame.AdventureGridX + shot.X * PvZGame.CellWidth - 12, _owner.AdventureCellY((int)shot.Row, shot.X) + 28, 24, 24));
            }
        }

        /// <summary>Performs draw mode aquarium.</summary>
        private void DrawModeAquarium()
        {
            DrawImage("aquarium1.jpg", new Rect(0, 0, 800, 600));
            foreach (var fish in _owner._modeSession.Fish)
            {
                _owner._animations.Draw("Zombie_snorkle", new Rect(fish.X - 35, fish.Y - 30, 70, 65), _owner._game.Elapsed, "anim_aquarium_swim");
                if (fish.Hunger <= 15)
                {
                    Label(new Rect(fish.X - 40, fish.Y - 50, 80, 22), "饿了！", 15, Color.red, TextAnchor.MiddleCenter, true);
                }
            }

            foreach (var food in _owner._modeSession.Food)
            {
                DrawImage("brain.png", new Rect(food.X - 16, food.Y - 15.5f, 32, 31));
            }

            foreach (global::PlantsVsZombies.Simulation.Sun sun in _owner._game.Suns)
            {
                if (!sun.PresentationNative || !sun.PresentationPickup.Collecting)
                {
                    DrawAdventureSun(sun);
                }
            }
        }

        // Shared hosts can call these without changing pointer handling or business behavior.
        /// <summary>Performs draw original ui dialog.</summary>
        /// <param name = "panel">The panel.</param>
        /// <param name = "title">The title.</param>
        /// <param name = "body">The body.</param>
        /// <param name = "tallBottom">The tall bottom.</param>
        /// <param name = "shade">The shade.</param>
        private void DrawOriginalUiDialog(Rect panel, string title, string body, bool tallBottom = true, bool shade = true)
        {
            if (shade)
            {
                Fill(new Rect(0, 0, 800, 600), new Color(0, 0, 0, .5f));
            }

            OriginalUiWidgets.DialogFrame(_textureLookup, panel, tallBottom, !string.IsNullOrEmpty(title));
            if (!string.IsNullOrEmpty(title))
            {
                var heading = new Rect(panel.x + 40, panel.y + 35, panel.width - 80, 36);
                if (!OriginalUiWidgets.Text(heading, title, "DwarvenTodcraft24", new Color(224f / 255f, 187f / 255f, 98f / 255f)))
                {
                    Label(heading, title, 22, new Color(224f / 255f, 187f / 255f, 98f / 255f), TextAnchor.MiddleCenter, true);
                }
            }

            if (!string.IsNullOrEmpty(body))
            {
                var lines = new Rect(panel.x + 40, panel.y + 80, panel.width - 80, System.Math.Max(30, panel.height - (tallBottom ? 165 : 120)));
                if (!OriginalUiWidgets.Text(lines, body, "DwarvenTodcraft15", new Color(224f / 255f, 187f / 255f, 98f / 255f), true, true))
                {
                    Label(lines, body, 17, new Color(224f / 255f, 187f / 255f, 98f / 255f), TextAnchor.MiddleCenter, false);
                }
            }
        }

        /// <summary>Performs draw original ui button.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "enabled">The enabled.</param>
        private void DrawOriginalUiButton(Rect rect, string text, bool enabled = true)
        {
            bool hover = enabled && OriginalUiWidgets.Hovered(rect), down = hover && OriginalUiWidgets.Pressed(rect);
            OriginalUiWidgets.StoneButton(_textureLookup, rect, hover, down, enabled);
            if (!OriginalUiWidgets.StoneLabel(rect, text, hover, down, enabled))
            {
                Label(new Rect(rect.x + (down ? 1 : 0), rect.y + (down ? 1 : 0), rect.width, rect.height), text, 15, enabled ? OriginalUiWidgets.StoneText : Color.gray, TextAnchor.MiddleCenter, true);
            }
        }

        /// <summary>Performs original ui button.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "enabled">The enabled.</param>
        /// <returns>The resulting original ui button.</returns>
        private bool OriginalUiButton(Rect rect, string text, bool enabled = true)
        {
            var previous = CanvasUI.Enabled;
            CanvasUI.Enabled = previous && enabled;
            bool clicked;
            try
            {
                clicked = CanvasUI.Button(rect, GUIContent.none, GUIStyle.none);
                DrawOriginalUiButton(rect, text, previous && enabled);
            }
            finally
            {
                CanvasUI.Enabled = previous;
            }

            return clicked;
        }

        /// <summary>Performs original ui text field.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "value">The value.</param>
        /// <param name = "maxLength">The max length.</param>
        /// <returns>The resulting original ui text field.</returns>
        private string OriginalUiTextField(Rect rect, string value, int maxLength = 1024)
        {
            OriginalUiWidgets.EditBox(_textureLookup, rect);
            if (_textFieldStyle == null)
            {
                _textFieldStyle = OriginalUiWidgets.TransparentTextField(GUIStyle.none);
                _textFieldStyle.font = _owner._font;
                _textFieldStyle.fontSize = 16;
                _textFieldStyle.padding = new RectOffset(6, 6, 5, 5);
            }

            var style = _textFieldStyle;
            return CanvasUI.TextField(rect, value, maxLength, style);
        }

        /// <summary>Performs original ui list row.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        /// <param name = "selected">The selected.</param>
        /// <returns>The resulting original ui list row.</returns>
        private bool OriginalUiListRow(Rect rect, string text, bool selected)
        {
            // PC UserDialog/ListWidget palette at0x74A9C4, not a temporary button skin.
            Fill(rect, selected ? new Color(20f / 255f, 180f / 255f, 15f / 255f) : new Color(23f / 255f, 24f / 255f, 35f / 255f));
            var color = new Color(235f / 255f, 225f / 255f, 180f / 255f);
            if (!OriginalUiWidgets.Text(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height), text, "BrianneTod16", color, false))
            {
                Label(new Rect(rect.x + 5, rect.y, rect.width - 10, rect.height), text, 15, color, TextAnchor.MiddleLeft, false);
            }

            return CanvasUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        /// <summary>Performs draw original ui advice.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "text">The text.</param>
        private void DrawOriginalUiAdvice(Rect rect, string text)
        {
            // Native advice is original font text with a dark shadow; no invented panel texture.
            var shadow = new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height);
            if (OriginalUiWidgets.Text(shadow, text, "DwarvenTodcraft18", Color.black, true, true) && OriginalUiWidgets.Text(rect, text, "DwarvenTodcraft18", new Color(1f, 1f, 0f), true, true))
            {
                return;
            }

            Label(shadow, text, 18, Color.black, TextAnchor.MiddleCenter, true);
            Label(rect, text, 18, Color.yellow, TextAnchor.MiddleCenter, true);
        }
        /// <summary>Stores display account refresh,  wallet visible until.</summary>
        private float _displayAccountRefresh, _walletVisibleUntil;
        /// <summary>Stores original progression strings.</summary>
        private readonly Dictionary<string, string> _originalProgressionStrings = new Dictionary<string, string>();

        /// <summary>Performs read display account.</summary>
        /// <returns>The resulting read display account.</returns>
        private PlayerAccount ReadDisplayAccount()
        {
            if (_owner._profiles == null || !_owner._profiles.SelectedId.HasValue)
            {
                return null;
            }

            if (_owner._displayAccountSnapshot == null || _owner._displayAccountSnapshot.ProfileId != _owner._profiles.SelectedId.Value || Time.unscaledTime >= _displayAccountRefresh)
            {
                _owner._displayAccountSnapshot = _owner.GetSelectedAccount();
                _displayAccountRefresh = Time.unscaledTime + 1f;
            }

            return _owner._displayAccountSnapshot;
        }

        /// <summary>Performs draw account status.</summary>
        private void DrawAccountStatus()
        {
            if (_owner._profiles == null || !_owner._profiles.SelectedId.HasValue)
            {
                return;
            }

            var p = _owner.GetSelectedAccount();
            if (p == null)
            {
                return;
            }

            Label(new Rect(528, 181, 210, 26), "金币：" + p.DisplayCoins, 17, new Color(1f, 0.92f, 0.4f), TextAnchor.MiddleCenter, true);
            Label(new Rect(10, 576, 250, 22), "F9：原版档案导入 / 导出", 13, Color.white, TextAnchor.MiddleLeft, false);
        }

        /// <summary>Performs draw native progression image.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        private void DrawNativeProgressionImage(string name, float x, float y)
        {
            var t = _owner.Texture(name);
            if (t == null && name.EndsWith(".png"))
            {
                t = _owner.Texture(name.Substring(0, name.Length - 4) + ".jpg");
            }

            if (t != null)
            {
                CanvasUI.DrawTexture(new Rect(x, y, t.width, t.height), t, ScaleMode.StretchToFill, true);
            }
        }

        /// <summary>Performs draw progression plant card.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "card">The card.</param>
        private void DrawProgressionPlantCard(PlantKind kind, Rect card)
        {
            var seeds = _owner.Texture("seeds.png");
            if (seeds != null)
            {
                CanvasUI.DrawTextureWithTexCoords(card, seeds, new Rect(2f / 9f, 0, 1f / 9f, 1));
            }

            var cell = AlmanacPortraitLayout.PlantAtlasCell((int)kind);
            var atlas = cell < 0 ? null : _owner.Texture("packet_plants.png");
            if (atlas != null)
            {
                CanvasUI.DrawTextureWithTexCoords(card, atlas, new Rect(cell / 13f, 0, 1f / 13f, 1));
            }
            else
            {
                Vector2 origin;
                float scale;
                AlmanacPortraitLayout.PlantThumbnailTransform((int)kind, card, out origin, out scale);
                _owner._animations.DrawNative(AdventurePlantAnimation(kind), origin, scale, 0f);
            }
        }

        /// <summary>Performs draw store.</summary>
        private void DrawStore()
        {
            var p = _owner.GetSelectedAccount();
            if (p == null)
            {
                return;
            }

            var progress = _owner._adventureStore.Get(_owner._profiles.SelectedId.Value);
            DrawImage("Store_Background.jpg", new Rect(0, 0, 800, 600));
            DrawNativeProgressionImage("Store_Car.png", 196, 138);
            DrawNativeProgressionImage("Store_HatchbackOpen.png", 299, 0);
            DrawNativeProgressionImage("Store_Sign.png", 285, 0);
            _owner._animations.DrawNative("CrazyDave", new Vector2(-42, 68), 1f, Time.unscaledTime, "anim_idle");
            var page = PvZGame.s_originalShopPages[_owner._shopPage];
            for (var i = 0; i < page.Length; i++)
            {
                var item = ShopCatalog.Get(page[i]);
                var rect = PvZGame.ShopItemRect(i);
                bool locked = !ShopCatalog.IsUnlocked(item, progress.NextLevel, progress.CompletedRuns), sold = ShopCatalog.SoldOut(item, p, PvZGame.UtcSeconds);
                if (item.Id < 9)
                {
                    DrawProgressionPlantCard((PlantKind)(40 + item.Id), new Rect(rect.x, rect.y, 50, 70));
                }
                else if (item.Id >= 10 && item.Id <= 12)
                {
                    _owner._animations.Draw("ZenGarden_sprout", new Rect(rect.x, rect.y + 12, 70, 62), Time.unscaledTime, "anim_idle");
                }
                else if (item.Id == 20)
                {
                    _owner._animations.Draw(item.Id == 22 ? "PoolCleaner" : item.Id == 23 ? "RoofCleaner" : "Stinky", new Rect(rect.x + 8, rect.y + 8, 58, 55), 0f);
                }
                else if (item.Icon != null)
                {
                    float dx = item.Id == 13 ? -14 : item.Id == 14 || item.Id == 28 ? -11 : item.Id == 18 || item.Id == 25 || item.Id == 27 ? -8 : item.Id == 24 ? -5 : item.Id == 21 ? -7 : item.Id == 22 ? 1 : item.Id == 29 ? -1 : -12;
                    float dy = item.Id == 13 ? -4 : item.Id == 14 || item.Id == 28 ? -2 : item.Id == 18 || item.Id == 25 || item.Id == 27 ? 2 : item.Id == 24 ? 10 : item.Id == 21 || item.Id == 22 ? 7 : item.Id == 23 ? 28 : item.Id == 29 ? 13 : 3;
                    DrawNativeProgressionImage(item.Icon, rect.x + dx, rect.y + dy);
                }

                if (item.Id == 21)
                {
                    Label(new Rect(rect.x, rect.y + 6, 55, 70), (p.PacketSlots + 1).ToString(), 36, new Color(.75f, .94f, .2f), TextAnchor.MiddleCenter, true);
                }

                DrawNativeProgressionImage("Store_PriceTag.png", rect.x - 3, rect.y + 70);
                var price = "$" + (ShopCatalog.Price(item, p) * 10).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
                if (!DrawBitmap(_owner._sunCounterBitmap, price, rect.x + 23, rect.y + 85, 90, Color.black, true))
                {
                    Label(new Rect(rect.x - 5, rect.y + 66, 75, 24), price, 14, Color.black, TextAnchor.MiddleCenter, true);
                }

                if (locked || sold)
                {
                    Label(rect, OriginalProgressionText(locked ? "COMING_SOON" : "SOLD_OUT"), 16, Color.red, TextAnchor.MiddleCenter, true);
                }
                else if (item.Id < 9 && rect.Contains(_owner._adventurePointer))
                {
                    DrawImage("SeedPacketFlash.png", new Rect(rect.x, rect.y, 50, 70));
                }

                if (item.Id == _owner._selectedShopItem)
                {
                    Outline(rect, Color.yellow, 2);
                }
            }

            OriginalUiWidgets.ImageButton(_textureLookup, new Rect(252, 402, _owner.Texture("Store_PrevButton.png").width, _owner.Texture("Store_PrevButton.png").height), "Store_PrevButton.png", "Store_PrevButtonHighlight.png", null, OriginalUiWidgets.Hovered(new Rect(252, 402, 96, 75)), OriginalUiWidgets.Pressed(new Rect(252, 402, 96, 75)), true);
            OriginalUiWidgets.ImageButton(_textureLookup, new Rect(596, 402, _owner.Texture("Store_NextButton.png").width, _owner.Texture("Store_NextButton.png").height), "Store_NextButton.png", "Store_NextButtonHighlight.png", null, OriginalUiWidgets.Hovered(new Rect(596, 402, 136, 83)), OriginalUiWidgets.Pressed(new Rect(596, 402, 136, 83)), true);
            OriginalUiWidgets.ImageButton(_textureLookup, new Rect(366, 512, 138, 80), "Store_MainMenuButton.png", "Store_MainMenuButtonHighlight.png", "Store_MainMenuButtonDown.png", OriginalUiWidgets.Hovered(new Rect(366, 512, 138, 80)), OriginalUiWidgets.Pressed(new Rect(366, 512, 138, 80)));
            Label(new Rect(366, 512, 138, 80), "主菜单", 15, new Color(98f / 255f, 153f / 255f, 235f / 255f), TextAnchor.MiddleCenter, true);
            DrawNativeProgressionImage("coinbank.png", 650, 559);
            Label(new Rect(650, 559, 116, 30), p.DisplayCoins.ToString(), 23, new Color(0.7f, 1f, 0.35f), TextAnchor.MiddleRight, true);
            Label(new Rect(330, 475, 280, 26), "疯狂戴夫的商店  " + (_owner._shopPage + 1) + " / 4", 19, Color.white, TextAnchor.MiddleCenter, true);
            DrawProgressionMessage();
            if (_owner._selectedShopItem >= 0)
            {
                DrawStorePurchaseDialog();
            }
        }

        /// <summary>Performs draw store purchase dialog.</summary>
        private void DrawStorePurchaseDialog()
        {
            DrawOriginalUiDialog(new Rect(200, 166, 400, 270), OriginalProgressionText("BUY_ITEM_HEADER"), string.IsNullOrEmpty(_owner._progressionMessage) ? OriginalProgressionText("BUY_ITEM") : _owner._progressionMessage);
            DrawOriginalUiButton(new Rect(245, 346, 135, 46), OriginalProgressionText("DIALOG_BUTTON_YES"));
            DrawOriginalUiButton(new Rect(420, 346, 135, 46), OriginalProgressionText("DIALOG_BUTTON_NO"));
        }

        /// <summary>Performs garden need name.</summary>
        /// <param name = "need">The need.</param>
        /// <returns>The resulting garden need name.</returns>
        private static string GardenNeedName(GardenNeed need)
        {
            switch (need)
            {
                case GardenNeed.Water:
                    return "需要浇水";
                case GardenNeed.Fertilizer:
                    return "需要肥料";
                case GardenNeed.BugSpray:
                    return "需要杀虫剂";
                case GardenNeed.Music:
                    return "想听音乐";
                case GardenNeed.Happy:
                    return "心满意足";
                default:
                    return "休息中";
            }
        }

        /// <summary>Performs draw garden.</summary>
        private void DrawGarden()
        {
            var p = _owner.GetSelectedAccount();
            if (p == null)
            {
                return;
            }

            DrawImage(_owner._gardenKind == GardenKind.Greenhouse ? "Background_Greenhouse.jpg" : _owner._gardenKind == GardenKind.Mushroom ? "Background_MushroomGarden.jpg" : "aquarium1.jpg", new Rect(0, 0, 800, 600));
            foreach (var g in p.Plants)
            {
                if (g.Garden != _owner._gardenKind)
                {
                    continue;
                }

                var r = _owner.GardenPlantRect(g);
                if (g.Garden != GardenKind.Aquarium)
                {
                    _owner._animations.Draw("FlowerPot", new Rect(r.x + 8, r.y + 39, 64, 49), 0f, "anim_zengarden");
                }

                var anim = g.Growth == 0 ? "ZenGarden_sprout" : AdventurePlantAnimation(g.Kind);
                float size = g.Growth == 0 ? 55 : g.Growth == 1 ? 56 : g.Growth == 2 ? 68 : 80;
                _owner._animations.Draw(anim, new Rect(r.center.x - size / 2, r.y + 70 - size, size, size), Time.unscaledTime, g.Growth == 0 ? "anim_idle" : g.Garden == GardenKind.Aquarium && (g.Kind == PlantKind.TangleKelp || g.Kind == PlantKind.SeaShroom) ? "anim_idle_aquarium" : null);
                var need = g.Need(PvZGame.UtcSeconds);
                Label(new Rect(r.x - 13, r.y - 15, 106, 20), GardenNeedName(need), 12, need == GardenNeed.Happy ? Color.yellow : Color.white, TextAnchor.MiddleCenter, true);
                if (g.PendingCoins > 0)
                {
                    DrawImage("coinbank.png", new Rect(r.x + 10, r.y + 60, 60, 20));
                    Label(new Rect(r.x, r.y + 62, 80, 20), "+" + g.PendingCoins * 10, 14, Color.yellow, TextAnchor.MiddleCenter, true);
                }
            }

            string[] names =
            {
                "浇水",
                "施肥",
                "杀虫",
                "音乐",
                "收金币",
                "出售",
                "搬运",
                "巧克力"
            };
            string[] icons =
            {
                p.PurchaseCount(13) > 0 ? "WateringCanGold.png" : "WateringCan.png",
                "Fertilizer.png",
                "BugSpray.png",
                "Phonograph.png",
                "Zen_MoneySign.png",
                "Zen_MoneySign.png",
                "Zen_GardenGlove.png",
                "chocolate.png"
            };
            for (var i = 0; i < 8; i++)
            {
                var r = new Rect(8 + i * 78, 4, 73, 57);
                if (icons[i] != null)
                {
                    DrawImage(icons[i], new Rect(r.x + 16, r.y, 45, 38));
                }

                Label(new Rect(r.x, r.y + 37, 78, 19), names[i] + (i == 1 ? " " + p.PurchaseCount(14) : i == 2 ? " " + p.PurchaseCount(15) : ""), 13, Color.white, TextAnchor.MiddleCenter, true);
                if ((int)_owner._gardenTool == i)
                {
                    Outline(r, Color.yellow, 2);
                }
            }

            DrawOriginalUiButton(new Rect(674, 6, 120, 36), "主菜单");
            DrawOriginalUiButton(new Rect(654, 540, 136, 42), "商店");
            DrawOriginalUiButton(new Rect(8, 540, 155, 42), "下一个花园");
            DrawOriginalUiButton(new Rect(175, 540, 120, 42), "智慧树");
            Label(new Rect(310, 548, 200, 28), "金币：" + p.DisplayCoins, 19, Color.yellow, TextAnchor.MiddleCenter, true);
            if (p.PurchaseCount(20) > 0)
            {
                _owner._animations.Draw("Stinky", new Rect(500, 494, 80, 62), Time.unscaledTime);
                Label(new Rect(490, 551, 105, 22), "点击唤醒蜗牛", 13, Color.white, TextAnchor.MiddleCenter, true);
            }

            DrawProgressionMessage();
        }

        /// <summary>Performs original progression text.</summary>
        /// <param name = "key">The key.</param>
        /// <returns>The resulting original progression text.</returns>
        private string OriginalProgressionText(string key)
        {
            if (_originalProgressionStrings.Count == 0)
            {
                var file = Path.Combine(_owner._root, "properties", "LawnStrings.txt");
                if (!File.Exists(file))
                {
                    return key;
                }

                string current = null;
                var value = new StringBuilder();
                foreach (string line in File.ReadAllLines(file))
                {
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        if (current != null)
                        {
                            _originalProgressionStrings[current] = value.ToString().Trim();
                        }

                        current = line.Substring(1, line.Length - 2);
                        value.Length = 0;
                    }
                    else if (current != null)
                    {
                        value.AppendLine(line);
                    }
                }

                if (current != null)
                {
                    _originalProgressionStrings[current] = value.ToString().Trim();
                }
            }

            string result;
            return _originalProgressionStrings.TryGetValue(key, out result) ? result : "";
        }

        /// <summary>Performs draw wisdom tree.</summary>
        private void DrawWisdomTree()
        {
            var p = _owner.GetSelectedAccount();
            if (p == null)
            {
                return;
            }

            var pose = Math.Min(_owner._treeGrowStarted < 0 ? 50 : 51, Math.Max(1, p.TreeHeight));
            global::PlantsVsZombies.Reanimation.AnimationInfo treeInfo;
            var fps = _owner._animations.TryGetAnimationInfo("TreeOfWisdom", "anim_grow" + pose, out treeInfo) ? treeInfo.FramesPerSecond : 12f;
            var age = _owner._treeGrowStarted < 0 ? 100f : (Time.unscaledTime - _owner._treeGrowStarted) * 8f / fps;
            _owner._animations.DrawNative("TreeOfWisdom", Vector2.zero, 1f, age, "anim_grow" + pose, false);
            DrawOriginalUiButton(new Rect(655, 8, 138, 40), "禅境花园");
            DrawOriginalUiButton(new Rect(655, 548, 138, 42), "商店");
            DrawImage("TreeFood.png", new Rect(24, 505, 68, 80));
            DrawOriginalUiButton(new Rect(102, 539, 175, 44), "施肥（" + p.PurchaseCount(28) + "）");
            if (p.TreeHeight >= 50)
            {
                Label(new Rect(250, 8, 300, 30), OriginalProgressionText("TREE_OF_WISDOM_HIEGHT").Replace("{HEIGHT}", p.TreeHeight.ToString()), 20, Color.white, TextAnchor.MiddleCenter, true);
            }

            var wisdom = p.TreeHeight == 100 ? 800 : p.TreeHeight == 500 ? 900 : p.TreeHeight == 1000 ? 1000 : p.TreeHeight > 1000 ? 1100 : Math.Max(1, Math.Min(49, p.TreeHeight - 1));
            var advice = OriginalProgressionText("TREE_OF_WISDOM_" + wisdom);
            float speechX = p.TreeHeight < 7 ? 400 : p.TreeHeight < 12 ? 395 : 390, speechY = p.TreeHeight < 7 ? 152 : p.TreeHeight < 12 ? 60 : 52;
            DrawNativeProgressionImage("Store_SpeechBubble2.png", speechX, speechY);
            Label(new Rect(speechX + 25, speechY + 6, 233, 144), advice, 16, Color.black, TextAnchor.MiddleCenter, false);
            DrawProgressionMessage();
        }

        /// <summary>Performs draw progression message.</summary>
        private void DrawProgressionMessage()
        {
            if (!string.IsNullOrEmpty(_owner._progressionMessage))
            {
                DrawOriginalUiAdvice(new Rect(12, 574, 776, 24), _owner._progressionMessage);
            }
        }

        /// <summary>Performs draw original save utility.</summary>
        private void DrawOriginalSaveUtility()
        {
            DrawImage("Challenge_Background.jpg", new Rect(0, 0, 800, 600));
            DrawOriginalUiDialog(new Rect(20, 20, 760, 560), "原版年度版档案导入 / 导出", null, true, false);
            Label(new Rect(55, 88, 690, 36), "输入 userdata 文件夹，或单独的 user*.dat 路径", 17, Color.white, TextAnchor.MiddleLeft, false);
            _owner._originalImportPath = OriginalUiTextField(new Rect(55, 128, 570, 32), _owner._originalImportPath);
            if (OriginalUiButton(new Rect(635, 128, 110, 32), "读取"))
            {
                _owner.QueueUiAction(_owner.ReadOriginalSaves);
            }

            if (_owner._importCandidates != null)
            {
                var shown = Math.Min(6, _owner._importCandidates.Count);
                for (var i = 0; i < shown; i++)
                {
                    var p = _owner._importCandidates[i];
                    if (OriginalUiListRow(new Rect(70, 181 + i * 33, 660, 29), p.Name + "  冒险 " + AdventureLevels.Get(p.Level).Label + "  金币 " + p.Coins * 10 + "  通关 " + p.FinishedAdventure + " 次", i == _owner._importSelection))
                    {
                        _owner._importSelection = i;
                    }
                }

                if (shown > 0 && OriginalUiButton(new Rect(300, 389, 200, 36), "导入为新用户"))
                {
                    _owner.QueueUiAction(_owner.ImportOriginalUser);
                }
            }

            Label(new Rect(55, 434, 690, 30), "从当前用户导出独立 user*.dat（需先导入原版用户）", 16, Color.white, TextAnchor.MiddleLeft, false);
            _owner._originalExportPath = OriginalUiTextField(new Rect(55, 471, 570, 32), _owner._originalExportPath);
            if (OriginalUiButton(new Rect(635, 471, 110, 32), "导出"))
            {
                _owner.QueueUiAction(_owner.ExportOriginalSave);
            }

            if (OriginalUiButton(new Rect(320, 530, 160, 36), "返回主菜单"))
            {
                _owner.QueueUiAction(_owner.ReturnFromSaveUtility);
            }

            DrawProgressionMessage();
        }

        /// <summary>Performs draw original hover tips.</summary>
        private void DrawOriginalHoverTips()
        {
            if (_owner._optionsView.IsOpen || _owner._unavailableFeature != null || _owner._confirmingQuit || _owner._profileDialog != null && _owner._profileDialog.IsOpen)
            {
                return;
            }

            if (_owner._smokeMode && _owner._tooltipInspection != null)
            {
                DrawOriginalTooltipRequest(_owner._tooltipInspection);
                return;
            }

            if (_owner._currentView != PvZGame.ScreenView.Board || _owner._game == null || !_owner._game.IsAdventure)
            {
                return;
            }

            if (_owner.AdventureIntroActive && !_owner.AdventureIntroAwaitingSeeds)
            {
                return;
            }

            if (_owner.AdventureIntroAwaitingSeeds)
            {
                foreach (PlantKind kind in _owner._game.Adventure.AvailableSeeds)
                {
                    var rect = _owner.AdventureCampaignChooserRect(kind);
                    if (rect.Contains(_owner._adventurePointer))
                    {
                        DrawOriginalPlantTooltip(kind, rect, true);
                        return;
                    }
                }

                return;
            }

            if (_owner.IsModeBoard && _owner._modeSession.Definition.UsesPackets || _owner._game.Adventure.Level.HasConveyor)
            {
                return;
            }

            for (var i = 0; i < _owner._deck.Count; i++)
            {
                var rect = _owner.IsModeBoard ? _owner.ModeSeedRect(i) : _owner.AdventureSeedRect(i);
                if (rect.Contains(_owner._adventurePointer))
                {
                    DrawOriginalPlantTooltip(_owner._deck[i], rect, false);
                    return;
                }
            }
        }

        /// <summary>Performs draw original plant tooltip.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "card">The card.</param>
        /// <param name = "chooser">The chooser.</param>
        private void DrawOriginalPlantTooltip(PlantKind kind, Rect card, bool chooser)
        {
            var shown = kind == PlantKind.Imitater && _owner._game.Adventure.ImitaterTarget.HasValue ? _owner._game.Adventure.ImitaterTarget.Value : kind;
            if ((int)shown < 0 || (int)shown >= VerifiedDataCatalog.Plants.Count)
            {
                return;
            }

            var key = VerifiedDataCatalog.Plants[(int)shown].InternalName;
            var warning = chooser ? "" : _owner._game.CooldownRemaining(kind) > 0 ? OriginalProgressionText("WAITING_TO_REFRESH") : _owner._game.AdventureSunAvailable < _owner._game.AdventureSeedCost(kind) ? OriginalProgressionText("NOT_ENOUGH_SUN") : "";
            DrawOriginalTooltipRequest(new OriginalTooltipRequest {
                Title = OriginalProgressionText(key),
                Body = OriginalProgressionText(key + "_TOOLTIP"),
                Warning = warning,
                X = (int)card.center.x,
                Y = (int)card.yMax + 7,
                Centered = true,
                UseSeedChooserLineWidth = chooser,
                WarningFlashCounter = 0
            });
        }

        /// <summary>Performs draw original tooltip request.</summary>
        /// <param name = "request">The request.</param>
        private void DrawOriginalTooltipRequest(OriginalTooltipRequest request)
        {
            if (_owner._originalTooltip == null)
            {
                _owner._originalTooltip = new OriginalTooltip();
            }

            _owner._originalTooltip.Draw(request);
        }

        /// <summary>Performs draw native mode zombie card.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "rect">The rect.</param>
        private void DrawNativeModeZombieCard(ZombieKind kind, Rect rect)
        {
            var seeds = _owner.Texture("seeds.png");
            if (seeds != null)
            {
                _owner._animations.DrawNativeImage(seeds, rect, new Rect(2f / 9f, 0, 1f / 9f, 1));
            }

            // PC0x49D260 ->0x49CEE0 ->0x4744F0 ->0x47ECE0 caches original body at (40,40).
            var scale = kind == ZombieKind.Gargantuar || kind == ZombieKind.Zomboni ? .23f : kind == ZombieKind.Imp ? .4f : kind == ZombieKind.Bungee ? .3f : kind == ZombieKind.Football ? .33f : .35f;
            float x = kind == ZombieKind.PoleVaulter ? -8 : kind == ZombieKind.Football ? -7 : kind == ZombieKind.Zomboni ? 12 : kind == ZombieKind.Gargantuar ? 4 : kind == ZombieKind.Imp ? -12 : kind == ZombieKind.Bungee ? 1 : -3;
            float y = kind == ZombieKind.PoleVaulter ? -11 : kind == ZombieKind.Football ? -8 : kind == ZombieKind.Imp ? -16 : kind == ZombieKind.Zomboni || kind == ZombieKind.Gargantuar ? 4 : kind == ZombieKind.Bungee ? 0 : kind == ZombieKind.Balloon ? -4 : kind == ZombieKind.Ladder || kind == ZombieKind.Digger || kind == ZombieKind.Pogo || kind == ZombieKind.ScreenDoor ? -9 : -6;
            if (kind == ZombieKind.Dancing)
            {
                scale = .37f * .8f;
                x = -18 + 20;
                y = -37 + 40;
            }

            var bankScale = rect.width / 50f;
            var origin = new Vector2(rect.x + (x + 40 * scale) * bankScale, rect.y + (y + (kind == ZombieKind.Gargantuar ? 60 : 40) * scale) * bankScale);
            scale *= bankScale;
            var variant = kind == ZombieKind.Conehead ? ZombieVariant.Conehead : kind == ZombieKind.Buckethead ? ZombieVariant.Buckethead : kind == ZombieKind.ScreenDoor ? ZombieVariant.ScreenDoor : ZombieVariant.Normal;
            if (kind == ZombieKind.Normal || kind == ZombieKind.Conehead || kind == ZombieKind.Buckethead || kind == ZombieKind.ScreenDoor)
            {
                _owner._animations.DrawZombieNative(origin, scale, 0f, variant, "anim_idle", true);
            }
            else
            {
                _owner._animations.DrawNative(PvZGame.AdventureZombieAnimation(kind), origin, scale, 0f, kind == ZombieKind.Pogo ? "anim_pogo" : kind == ZombieKind.PoleVaulter ? "anim_walk" : "anim_idle");
            }

            Label(new Rect(rect.x + 2, rect.y + rect.height - 17, rect.width - 7, 15), GameSimulation.ModeZombieCost(kind).ToString(), 13, Color.black, TextAnchor.MiddleRight, true);
        }
    }
}
