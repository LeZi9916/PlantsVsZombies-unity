#nullable enable
using PlantsVsZombies.Inputs;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Levels;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using PlantsVsZombies.UI.BitmapFonts;
using PlantsVsZombies.VerifiedData;
using PlantsVsZombies.Waves;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

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
            _textureLookup = owner.Textures.Load;
            _drawPacketMask = DrawAdventurePacketMask;
            _doomTrackOffset = DoomShroomTrackOffset;
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
            _hammerParticles?.Clear();
            _levelNameFont?.Dispose();
            _seedCostFont?.Dispose();
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
        /// <summary>Reuses the original Doom-shroom head shake sampler without creating a per-frame callback.</summary>
        private readonly Func<string, Vector2> _doomTrackOffset;
        /// <summary>Provides the head shake values consumed synchronously by the current Doom-shroom draw.</summary>
        private AdventureDoomShroomState? _doomShroomState;
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
        private readonly Func<string, Texture2D?> _textureLookup;
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
        /// <summary>The retained wave progress label.</summary>
        private string _waveCaption = "";
        /// <summary>Retains the original PC HouseofTerror16 font used by the lower-right level name.</summary>
        private CanvasBitmapFont? _levelNameFont;
        /// <summary>Retains the original PC Pico129 font used by seed packet prices.</summary>
        private CanvasBitmapFont? _seedCostFont;
        /// <summary>Stores label.</summary>
        private GUIStyle? _label;
        /// <summary>Wraps the localized original dialogue inside the speech bubble.</summary>
        private GUIStyle? _crazyDaveLabel;

        /// <summary>Submits the scene and Canvas presentation once after simulation.</summary>
        internal void Present()
        {
            _presentationKeys.BeginFrame();
            ObjectSpriteRenderer.BeginSceneFrame();
            SpriteDrawState.Clip = GameViewport.ScreenRect;
            CanvasUI.BeginFrame();
            _owner.RequiredAnimations.BeginObjectFrame();
            try
            {
                if (DrawOriginalLoading())
                {
                    return;
                }

                CanvasUI.Matrix = ViewportMatrix();
                CanvasUI.Enabled = !_owner.RequiredOptionsView.IsOpen && !_owner._confirmingQuit && _owner._unavailableFeature == null && (_owner._profileDialog == null || !_owner._profileDialog.IsOpen);
                switch (_owner._currentView)
                {
                    case PvZGame.ScreenView.Selector:
                        _owner.RequiredSelectorView.Draw(Time.unscaledTime);
                        DrawProfileName();
                        DrawAdventureProgress();
                        DrawAccountStatus();
                        break;
                    case PvZGame.ScreenView.Modes:
                        _owner.RequiredModeView.Draw();
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
                        _owner.RequiredAlmanacView.Draw(Time.unscaledTime);
                        break;
                    case PvZGame.ScreenView.Help:
                        _owner.RequiredHelpView.Draw();
                        break;
                    case PvZGame.ScreenView.Board:
                        DrawBoardPresentation();
                        DrawAdventureCrazyDave();
                        if (_owner.Opening._packetUpgradeOfferActive)
                        {
                            DrawPacketUpgradeOffer();
                        }
                        DrawAdventurePlantCursor();
                        DrawAdventureShovelCursor();
                        DrawAdventureHammerCursor();
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

                if (_owner.RequiredOptionsView.IsOpen)
                {
                    _owner.RequiredOptionsView.Draw();
                }

                if (_owner._profileDialog != null && _owner._profileDialog.IsOpen)
                {
                    _owner._profileDialog.Draw();
                }
            }
            finally
            {
                _owner.RequiredAnimations.EndObjectFrame();
                CanvasUI.EndFrame();
            }
        }

        /// <summary>Maps the original logical screen to the current display without a board widget offset.</summary>
        /// <returns>The unshifted 800 by 600 viewport transform.</returns>
        private static Matrix4x4 ViewportMatrix()
        {
            var scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            var offsetX = (Screen.width - Width * scale) * .5f;
            var offsetY = (Screen.height - Height * scale) * .5f;
            return Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        /// <summary>Moves board actors and HUD with the PC widget while leaving Dave and the pointer at screen coordinates.</summary>
        private void DrawBoardPresentation()
        {
            var previous = CanvasUI.Matrix;
            var game = _owner.RequiredGame;
            if (game.IsAdventure)
            {
                var shake = game.Presentation.ScreenShake;
                CanvasUI.Matrix = previous * Matrix4x4.Translate(new Vector3(shake.OffsetX, shake.OffsetY, 0f));
            }

            try
            {
                DrawBoard();
                DrawInterface();
                DrawAdventureHammerHits();
            }
            finally
            {
                CanvasUI.Matrix = previous;
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
        private bool DrawBitmap(CanvasBitmapFont? bitmap, string text, float anchor, float baseline, float maxWidth, Color color, bool centered, bool integerCenter = false)
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
            _owner.RequiredAnimations.BeginObjectFrame();
            _renderingBoardObjects = true;
            try
            {
                DrawBoardObjects();
            }
            finally
            {
                _renderingBoardObjects = false;
                _owner.RequiredAnimations.EndObjectFrame();
            }
        }

        /// <summary>Performs draw board objects.</summary>
        private void DrawBoardObjects()
        {
            if (_owner.RequiredGame.IsAdventure)
            {
                DrawAdventureBoard();
                return;
            }

            if (_owner.RequiredGame.LevelOneUsesSingleRowLawn)
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
                foreach (Mower mower in _owner.RequiredGame.Mowers)
                {
                    if (mower.Row != row || (!mower.Available && !mower.Active))
                    {
                        continue;
                    }

                    var r = new Rect(BoardLayout.GridX + mower.X * BoardLayout.CellWidth - 22f, BoardLayout.GridY + row * BoardLayout.CellHeight + 43f, 78f, 58f);
                    _owner.RequiredAnimations.Draw("LawnMower", r, _owner.RequiredGame.Elapsed);
                }

                foreach (Plant plant in _owner.RequiredGame.Plants)
                {
                    if (plant.Row != row)
                    {
                        continue;
                    }

                    var x = BoardLayout.GridX + plant.Column * BoardLayout.CellWidth;
                    var y = BoardLayout.GridY + row * BoardLayout.CellHeight;
                    DrawImage("plantshadow.png", new Rect(x + 2, y + 71, 76, 24));
                    var plantRect = new Rect(x, y + 5, 80, 88);
                    if (plant.Kind == PlantKind.PotatoMine)
                    {
                        var clip = plant.MinePhase == MinePhase.Armed ? "anim_armed" : plant.MinePhase == MinePhase.Rising ? "anim_rise" : "anim_idle";
                        var time = plant.MinePhase == MinePhase.Rising ? plant.PhaseAge * 1.5f : plant.PhaseAge;
                        _owner.RequiredAnimations.Draw("PotatoMine", plantRect, time, clip, plant.MinePhase != MinePhase.Rising);
                    }
                    else if (plant.Kind == PlantKind.Wallnut || plant.Kind == PlantKind.TallNut)
                    {
                        var tall = plant.Kind == PlantKind.TallNut;
                        var maxHealth = tall ? 8000f : 4000f;
                        var prefix = tall ? "IMAGE_REANIM_TALLNUT_CRACKED" : "IMAGE_REANIM_WALLNUT_CRACKED";
                        var cracked = plant.Health < maxHealth / 3f ? prefix + "2" : plant.Health < maxHealth * 2f / 3f ? prefix + "1" : null;
                        _owner.RequiredAnimations.DrawWithImageOverride(tall ? "Tallnut" : "Wallnut", plantRect, plant.Age, tall ? "anim_idle" : "anim_face", cracked);
                    }
                    else if ((plant.Kind == PlantKind.Peashooter || plant.Kind == PlantKind.SnowPea || plant.Kind == PlantKind.Repeater) && plant.ShootAge >= 0f)
                    {
                        var animationName = AnimationName(plant.Kind);
                        var shootFps = plant.Kind == PlantKind.Repeater ? 45f : 35f;
                        float duration;
                        if (!_shootingDurations.TryGetValue(animationName, out duration))
                        {
                            global::PlantsVsZombies.Reanimation.AnimationInfo info;
                            duration = _owner.RequiredAnimations.TryGetAnimationInfo(animationName, "anim_shooting", out info) ? info.FrameCount / shootFps : 0f;
                            _shootingDurations[animationName] = duration;
                        }

                        if (plant.ShootAge < duration)
                        {
                            _owner.RequiredAnimations.DrawAttachedPlant(animationName, plantRect, plant.Age, plant.ShootAge, "anim_shooting", false, 0f, shootFps);
                        }
                        else
                        {
                            _owner.RequiredAnimations.Draw(animationName, plantRect, plant.Age);
                        }
                    }
                    else
                    {
                        _owner.RequiredAnimations.Draw(AnimationName(plant.Kind), plantRect, plant.Age);
                    }
                }

                foreach (Zombie zombie in _owner.RequiredGame.Zombies)
                {
                    if (zombie.Row != row)
                    {
                        continue;
                    }

                    var x = BoardLayout.GridX + zombie.X * BoardLayout.CellWidth;
                    var y = BoardLayout.GridY + row * BoardLayout.CellHeight;
                    DrawImage("plantshadow.png", new Rect(x - 38, y + 76, 76, 22));
                    var previous = CanvasUI.Color;
                    if (zombie.ChillRemaining > 0f)
                    {
                        CanvasUI.Color = new Color(0.65f, 0.8f, 1f, previous.a);
                    }

                    _owner.RequiredAnimations.DrawZombie(new Rect(x - 62, y - 44, 125, 145), zombie.Age, ZombieAppearance(zombie.Kind), zombie.Kind == ZombieKind.Flag || (zombie.Kind == ZombieKind.ScreenDoor ? zombie.ShieldHealth > 0f : zombie.ArmorHealth > 0f), zombie.Eating ? "anim_eat" : "anim_walk");
                    CanvasUI.Color = previous;
                }

                foreach (Pea pea in _owner.RequiredGame.Peas)
                {
                    if (pea.Row == row)
                    {
                        DrawImage(pea.IsSnow ? "ProjectileSnowPea.png" : "ProjectilePea.png", new Rect(BoardLayout.GridX + pea.X * BoardLayout.CellWidth - 13, BoardLayout.GridY + row * BoardLayout.CellHeight + 20, 26, 26));
                    }
                }

                foreach (LaneFlame flame in _owner.RequiredGame.Flames)
                {
                    if (flame.Row != row)
                    {
                        continue;
                    }

                    for (var i = 0; i < 11; i++)
                    {
                        _owner.RequiredAnimations.Draw("fire", new Rect(BoardLayout.GridX + i * 68f - 20f, BoardLayout.GridY + row * BoardLayout.CellHeight - 18f, 82f, 112f), flame.Age + i * 0.025f, "anim_flame");
                    }
                }
            }

            foreach (global::PlantsVsZombies.Simulation.Sun sun in _owner.RequiredGame.Suns)
            {
                _owner.RequiredAnimations.Draw("Sun", BoardLayout.SunRect(sun), sun.Age);
            }

            if (_owner.RequiredGame.LevelOneUsesNativeSkySunRules)
            {
                foreach (LevelOneNativeSkySun sun in _owner.RequiredGame.LevelOneNativeSkySuns)
                {
                    var previous = CanvasUI.Color;
                    try
                    {
                        if (sun.FadeTicksRemaining > 0)
                        {
                            CanvasUI.Color = new Color(previous.r, previous.g, previous.b, previous.a * sun.FadeTicksRemaining / 15f);
                        }

                        _owner.RequiredAnimations.Draw("Sun", new Rect(sun.PixelX, sun.PixelY, LevelOneNativeSkySun.BaseWidthPixels, LevelOneNativeSkySun.BaseHeightPixels), _owner.RequiredGame.Elapsed);
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

            if (_owner.RequiredGame.IsAdventure)
            {
                DrawAdventureInterface();
                return;
            }

            DrawImage("SeedBank.png", new Rect(0, 0, 446, 87));
            if (_owner.RequiredGame.IsExperimentalLevelOne)
            {
                var bank = _owner.Textures.Load("SeedBank.png");
                var cap = FirstAdventureBoardUi.SeedBankEndCap(0, 0);
                if (bank != null)
                {
                    CanvasUI.DrawTextureWithTexCoords(new Rect(cap.Destination.X, cap.Destination.Y, cap.Destination.Width, cap.Destination.Height), bank, new Rect((float)cap.Source.X / bank.width, 0f, (float)cap.Source.Width / bank.width, 1f));
                }

                if (!DrawBitmap(_owner._sunCounterBitmap, FirstAdventureBoardUi.DisplayedSun(_owner.RequiredGame.SunAmount).ToString(), 38f, 78f, 76f, Color.black, true, true))
                {
                    Label(new Rect(10, 59, 55, 22), NumberLabel(_owner.RequiredGame.SunAmount), 18, _owner.AdventureBoard.SunMoneyWarningRed ? Color.red : Color.black, TextAnchor.MiddleCenter, true);
                }
            }
            else
            {
                Label(new Rect(10, 59, 55, 22), NumberLabel(_owner.RequiredGame.SunAmount), 18, _owner.AdventureBoard.SunMoneyWarningRed ? Color.red : Color.black, TextAnchor.MiddleCenter, true);
            }

            var packets = _owner.Textures.Load("seeds.png");
            for (var i = 0; i < _owner._deck.Count; i++)
            {
                var card = _owner.Layout.SeedRect(i);
                if (packets != null)
                {
                    CanvasUI.DrawTextureWithTexCoords(card, packets, new Rect(0, 0, 1f / 9f, 1));
                }

                DrawSeedIcon(_owner._deck[i], card);
                Label(new Rect(card.x + 1, card.y + 51, 46, 17), NumberLabel(GameSimulation.Cost(_owner._deck[i])), 13, Color.black, TextAnchor.MiddleRight, true);
                var fraction = _owner.RequiredGame.CooldownRemaining(_owner._deck[i]) / GameSimulation.RechargeTime(_owner._deck[i]);
                if (fraction > 0f)
                {
                    Fill(new Rect(card.x, card.y, card.width, card.height * fraction), new Color(0, 0, 0, 0.55f));
                }
                else if (_owner.RequiredGame.SunAmount < GameSimulation.Cost(_owner._deck[i]))
                {
                    Fill(card, new Color(0, 0, 0, 0.36f));
                }

                if (_owner._selected == i)
                {
                    Outline(card, new Color(1f, 0.91f, 0.35f), 3f);
                }
            }

            if (!_owner.RequiredGame.IsExperimentalLevelOne)
            {
                DrawShovelBank(new Rect(522, 0, 70, 72));

                Fill(new Rect(596, 6, 100, 34), new Color(0.28f, 0.18f, 0.09f, 0.9f));
                Label(new Rect(596, 6, 100, 34), "选植物", 17, Color.white, TextAnchor.MiddleCenter, true);
            }

            Fill(new Rect(704, 6, 90, 34), new Color(0.28f, 0.18f, 0.09f, 0.9f));
            Label(new Rect(704, 6, 90, 34), _owner.RequiredGame.State == MatchState.Paused ? "继续" : "菜单", 17, Color.white, TextAnchor.MiddleCenter, true);
            var progress = _owner.RequiredGame.IsExperimentalLevelOne ? _owner.RequiredGame.LevelOneWaveProgress : _owner.RequiredGame.Rules.ZombieCount == 0 ? 0 : (float)_owner.RequiredGame.SpawnedZombies / _owner.RequiredGame.Rules.ZombieCount;
            var meter = _owner.Textures.Load("FlagMeter.png");
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

                var parts = _owner.Textures.Load("FlagMeterParts.png");
                if (parts != null)
                {
                    CanvasUI.DrawTextureWithTexCoords(new Rect(meterRect.x + 138f - 120f * progress, meterRect.y - 3, 25, 25), parts, new Rect(0, 0, 1f / 3f, 1));
                }
            }

            if (_owner.RequiredGame.State == MatchState.Paused)
            {
                DrawDialog("已暂停", "继续游戏", true);
            }
            else if (_owner.RequiredGame.State == MatchState.Won)
            {
                DrawDialog("你赢了！", "再玩一次", false);
            }
            else if (_owner.RequiredGame.State == MatchState.Lost)
            {
                DrawDialog("僵尸吃掉了你的脑子！", "再试一次", false);
            }

            if (_owner._choosingSeeds)
            {
                DrawSeedChooser();
            }

            if (_owner.RequiredGame.IsExperimentalLevelOne && _owner.RequiredGame.State == MatchState.Playing)
            {
                DrawLevelOneAdvice();
            }
        }

        /// <summary>Performs draw level one advice.</summary>
        private void DrawLevelOneAdvice()
        {
            string? message = null;
            switch (_owner.RequiredGame.CurrentLevelOneTutorialStage)
            {
                case LevelOneTutorialStage.AwaitingSeedSelection:
                    message = _owner.RequiredGame.Plants.Count == 0 ? "点击种子包，把它捡起来！" : "点击豌豆射手，再种一棵！";
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
            Label(new Rect(500, 83, 95, 34), _owner.RequiredPendingSeeds.Count + " / 7", 18, Color.white, TextAnchor.MiddleRight, true);
            for (var i = 0; i < _owner._availablePlants.Length; i++)
            {
                var kind = _owner._availablePlants[i];
                var card = BoardLayout.ChooserSeedRect(i);
                var previous = CanvasUI.Color;
                if (_owner.RequiredPendingSeeds.Contains(kind) || _owner.RequiredPendingSeeds.IsMoving(kind))
                {
                    CanvasUI.Color = new Color(previous.r * 55f / 255f, previous.g * 55f / 255f, previous.b * 55f / 255f, previous.a);
                }

                DrawSimpleChooserCard(kind, card);
                CanvasUI.Color = previous;
            }

            DrawAdventureSeedBankBackground(Vector2.zero, 7);
            for (var i = 0; i < _owner.RequiredPendingSeeds.Count; i++)
            {
                var seed = _owner.RequiredPendingSeeds[i];
                if (!_owner.RequiredPendingSeeds.IsMoving(seed))
                {
                    DrawSimpleChooserCard(seed, new Rect(AdventureSeedSlotLayout.PositionX(i, 7), 8, 50, 70));
                }
            }

            for (var i = 0; i < _owner._availablePlants.Length; i++)
            {
                var seed = _owner._availablePlants[i];
                if (_owner.RequiredPendingSeeds.IsMoving(seed))
                {
                    var home = BoardLayout.ChooserSeedRect(i);
                    _owner.RequiredPendingSeeds.GetPosition(seed, (int)home.x, (int)home.y, 7, out var x, out var y);
                    DrawSimpleChooserCard(seed, new Rect(x, y, 50, 70));
                }
            }

            DrawImage(_owner.RequiredPendingSeeds.Count > 0 ? "SeedChooser_Button.png" : "SeedChooser_Button_Disabled.png", new Rect(322, 440, 156, 42));
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
                _owner.RequiredAnimations.Draw("PotatoMine", icon, _owner.RequiredGame.Elapsed, "anim_armed");
            }
            else
            {
                _owner.RequiredAnimations.Draw(AnimationName(kind), icon, _owner.RequiredGame.Elapsed);
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
            var texture = _owner.Textures.Load(name);
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
                _owner.RequiredAnimations.DrawNativeImage(texture, destination, source);
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

            var highlighted = _owner._shovel && ReferenceEquals(plant, _owner.Layout.AdventureShovelTarget(_owner.AdventureBoard._adventurePointer));
            RegisterPlantSurface(plant, highlighted);
            ActorRenderer.DrawPlant(plant, "CherryBomb", origin, scale, age, AdventureCherryBombState.Animation, false, rate,
                null, _presentationKeys.Get("plant-", plant.Id), new Rect(origin.x - 80, origin.y - 160, 300, 340), 0, AdventureRenderRemainder, _presentationKeys.Get("producer-plant-", plant.Id), highlighted);

            return true;
        }
        /// <summary>Performs draw adventure seed bank background.</summary>
        /// <param name = "origin">The origin.</param>
        /// <param name = "count">The count.</param>
        private void DrawAdventureSeedBankBackground(Vector2 origin, int count)
        {
            var image = _owner.Textures.Load("SeedBank.png");
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
            var backdrop = _owner.Textures.Load("ConveyorBelt_backdrop.png");
            if (backdrop != null)
            {
                CanvasUI.DrawTexture(new Rect(83, 0, backdrop.width, backdrop.height), backdrop);
            }

            var belt = _owner.Textures.Load("ConveyorBelt.png");
            if (belt == null)
            {
                return;
            }

            var row = _owner.RequiredGame.Adventure.ConveyorTicks / 4 % 6;
            CanvasUI.DrawTextureWithTexCoords(new Rect(90, 63, belt.width, belt.height / 6f), belt, new Rect(0, 1f - (row + 1) / 6f, 1, 1f / 6f));
        }

        /// <summary>Performs draw adventure conveyor packets.</summary>
        /// <param name = "showBackwards">The show backwards.</param>
        private void DrawAdventureConveyorPackets(bool showBackwards = false)
        {
            _showBackwardsPackets = showBackwards;
            if (_owner.RequiredGame.UsesNativeAdventureConveyor)
            {
                _owner.RequiredAnimations.DrawNativeClipped("native-conveyor-packets", BoardLayout.s_nativeConveyorClip, _drawConveyorPackets);
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
            for (var i = 0; i < _owner.RequiredGame.Adventure.Packets.Count && i < 10; i++)
            {
                var packet = _owner.RequiredGame.Adventure.Packets[i];
                var card = _owner.Layout.AdventureSeedRect(i, true);
                DrawAdventureConveyorPacket(packet, card);
                if (_showBackwardsPackets && _owner.IndependentModes._modeSession != null && _owner.IndependentModes._modeSession._backwardPackets.Contains(packet.Id))
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
            if (_owner._game == null || !_owner._game.IsAdventure || _owner._game.State != MatchState.Playing || _owner._choosingSeeds || _owner.Opening.AdventureIntroActive || _owner._shovel || _owner._game.Adventure.SelectedCobCannonId.HasValue || _owner.RequiredOptionsView.IsOpen || _owner._confirmingQuit || _owner._unavailableFeature != null || !new Rect(0, 0, 800, 600).Contains(_owner.AdventureBoard._adventurePointer))
            {
                return false;
            }

            if (_owner.AdventureBoard._selectedAdventurePacket >= 0)
            {
                foreach (AdventurePacket packet in _owner._game.Adventure.Packets)
                {
                    if (packet.Id == _owner.AdventureBoard._selectedAdventurePacket)
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

            var column = Mathf.FloorToInt((_owner.AdventureBoard._adventurePointer.x - BoardLayout.AdventureGridX) / BoardLayout.CellWidth);
            var targetRow = Mathf.FloorToInt((_owner.AdventureBoard._adventurePointer.y - BoardLayout.AdventureGridY - _owner.Layout.AdventureSlope(column)) / _owner.Layout.AdventurePitch);
            if (column < 0 || column > 8 || targetRow != row || targetRow < 0 || targetRow >= _owner.RequiredGame.RowCount || _owner.RequiredGame.CanPlantAdventure(kind, row, column) != AdventurePlantingFailure.None)
            {
                return;
            }

            var resolved = kind == PlantKind.Imitater && _owner.RequiredGame.Adventure.ImitaterTarget.HasValue ? _owner.RequiredGame.Adventure.RequiredImitaterTarget : kind;
            var heightProbe = _placementProbe;
            heightProbe.Kind = resolved;
            heightProbe.Row = row;
            heightProbe.Column = column;
            var origin = new Vector2(BoardLayout.AdventureGridX + column * BoardLayout.CellWidth, _owner.Layout.AdventureCellY(row, column) + AdventurePlantHeight(heightProbe));
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

            // PC 0x4445FA samples the cursor's packet type before DrawSeedType resolves imitation artwork.
            var height = (int)kind < 49 ? AlmanacPortraitLayout.PlantHeightOffset((int)kind) : 0f;
            // PC CursorObject::Update 0x444100 offsets the mouse by(-25,-35).
            // Draw 0x4441A0 adds(-10,height-25), with another +30 for native IDs 35/11.
            if (kind == PlantKind.CoffeeBean || kind == PlantKind.GraveBuster)
            {
                height += 30f;
            }

            DrawAdventureCursorPlant(kind, new Vector2(_owner.AdventureBoard._adventurePointer.x - 35f, _owner.AdventureBoard._adventurePointer.y - 60f + height), 1f, "cursor-held");
        }

        /// <summary>Draws the original shovel attached to the logical pointer.</summary>
        private void DrawAdventureShovelCursor()
        {
            if (!_owner._shovel || _owner._choosingSeeds || _owner.Opening.AdventureIntroActive && !_owner.Opening._bowlingShovelLessonActive || _owner.RequiredGame.State != MatchState.Playing
                || _owner.RequiredOptionsView.IsOpen || _owner._confirmingQuit || _owner._unavailableFeature != null
                || !new Rect(0, 0, 800, 600).Contains(_owner.AdventureBoard._adventurePointer))
            {
                return;
            }

            var image = _owner.Textures.Load("Shovel.png");
            if (image != null)
            {
                // PC 0x444100 and 0x4441A0: cursor(mouse-25,-35), shovel(+10,-30).
                DrawImage("Shovel.png", new Rect(_owner.AdventureBoard._adventurePointer.x - 15f, _owner.AdventureBoard._adventurePointer.y - 65f, image.width, image.height));
            }
        }

        /// <summary>Draws the original hammer attached to the logical pointer in Whack-a-Zombie boards.</summary>
        private void DrawAdventureHammerCursor()
        {
            var whackBoard = _owner._game != null && _owner._game.IsAdventure
                && (_owner._game.Adventure.Level.Mode == AdventureMode.WhackAZombie
                    || _owner.IndependentModes.IsModeBoard && _owner.IndependentModes.RequiredModeSession.Definition.Mechanic == ModeMechanic.Whack);
            if (!whackBoard
                || _owner._choosingSeeds || _owner.Opening.AdventureIntroActive || _owner.RequiredGame.State != MatchState.Playing
                || _owner.RequiredOptionsView.IsOpen || _owner._confirmingQuit || _owner._unavailableFeature != null
                || !new Rect(0, 0, 800, 600).Contains(_owner.AdventureBoard._adventurePointer))
            {
                return;
            }

            // PC CursorObject::Update 0x444100 stores (mouse-25,-35); the Hammer
            // reanimation is then drawn at that origin with anim_whack_zombie held.
            _owner.RequiredAnimations.DrawNativePlayback("Hammer",
                new Vector2(_owner.AdventureBoard._adventurePointer.x - 25f, _owner.AdventureBoard._adventurePointer.y - 35f),
                1f, _owner.AdventureBoard._hammerAnimationAge, "anim_whack_zombie", false, 24f);
        }

        /// <summary>Draws the original holder with its shovel removed while the tool is held.</summary>
        /// <param name="rect">The native holder rectangle.</param>
        private void DrawShovelBank(Rect rect)
        {
            DrawImage("ShovelBank.png", rect);
            if (!_owner._shovel)
            {
                var image = _owner.Textures.Load("Shovel.png");
                if (image != null)
                {
                    // PC Board::DrawShovel 0x424548/0x42454C offsets the tool by (-7,-3).
                    DrawImage("Shovel.png", new Rect(rect.x - 7f, rect.y - 3f, image.width, image.height));
                }
            }
        }

        /// <summary>Draws the available shovel alongside the seed bank, below the lawn actors.</summary>
        private void DrawAdventureShovelBank()
        {
            if (_owner.IndependentModes.IsModeBoard)
            {
                var mechanic = _owner.IndependentModes.RequiredModeSession.Definition.Mechanic;
                if (!_owner.Opening.AdventureIntroActive && mechanic != ModeMechanic.IZombie && mechanic != ModeMechanic.Aquarium
                    && mechanic != ModeMechanic.Beghouled && mechanic != ModeMechanic.BeghouledTwist && mechanic != ModeMechanic.Whack)
                {
                    DrawShovelBank(AdventureBoardController.s_adventureShovelButton);
                }
            }
            else
            {
                var level = _owner.RequiredGame.Adventure.Level;
                if (level.Number >= 5 && level.Mode != AdventureMode.WhackAZombie
                    && (!_owner.Opening.AdventureIntroActive || level.Mode == AdventureMode.Bowling))
                {
                    DrawShovelBank(AdventureBoardController.s_adventureShovelButton);
                }
            }
        }

        /// <summary>Draws the original sun counter and its seventy-tick insufficient-money warning.</summary>
        /// <param name="origin">The moving seed bank's native top-left position.</param>
        private void DrawAdventureSunAmount(Vector2 origin)
        {
            var color = _owner.AdventureBoard.SunMoneyWarningRed ? Color.red : Color.black;
            if (!DrawBitmap(_owner._sunCounterBitmap, NumberLabel(Math.Max(0, _owner.RequiredGame.SunAmount)), origin.x + 38f, origin.y + 78f, 76f, color, true, true))
            {
                Label(new Rect(origin.x + 8, origin.y + 59, 60, 22), NumberLabel(Math.Max(0, _owner.RequiredGame.SunAmount)), 18, color, TextAnchor.MiddleCenter, true);
            }
        }

        /// <summary>Performs draw adventure cursor plant.</summary>
        /// <param name = "seed">The seed.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "alpha">The alpha.</param>
        /// <param name = "key">The key.</param>
        private void DrawAdventureCursorPlant(PlantKind seed, Vector2 origin, float alpha, string key)
        {
            var imitated = seed == PlantKind.Imitater && _owner.RequiredGame.Adventure.ImitaterTarget.HasValue;
            var kind = imitated ? _owner.RequiredGame.Adventure.RequiredImitaterTarget : seed;
            var name = AdventurePlantAnimation(kind);
            var nativeScale = 1f;
            // PC ReanimatorCache::MakeCachedPlant 0x47E9A0 scales native IDs 4/35.
            if (kind == PlantKind.PotatoMine || kind == PlantKind.CoffeeBean)
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

            foreach (var explosion in _owner.RequiredGame.Presentation.Explosions)
            {
                if (!explosion.Overlay && explosion.Row == row)
                {
                    DrawAdventureExplosion(explosion, _adventureParticles);
                }
            }

            foreach (var fire in _owner.RequiredGame.Presentation.JalapenoFires)
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

                    var origin = new Vector2(flame.X, _owner.Layout.AdventureCellY(row, (flame.X + 10f - BoardLayout.AdventureGridX) / BoardLayout.CellWidth) - 10f);
                    _owner.RequiredAnimations.DrawNativeFullLastFrame("fire", origin, flame.ScaleX, seconds, done ? "anim_done" : "anim_flame", !done, done ? 15f : flame.Rate);
                }
            }
        }

        // Rendering.cs owner calls after all rows, before the Sun pass (PC layer 400000).
        /// <summary>Draws native explosion overlays before the pickup layer.</summary>
        private void DrawAdventureExplosionOverlay()
        {
            if (_adventureParticles == null)
            {
                return;
            }

            foreach (var explosion in _owner.RequiredGame.Presentation.Explosions)
            {
                if (explosion.Overlay && explosion.Effect != AdventureExplosionEffect.Pow)
                {
                    DrawAdventureExplosion(explosion, _adventureParticles);
                }
            }
        }

        /// <summary>Performs draw adventure explosion.</summary>
        /// <param name = "explosion">The explosion.</param>
        /// <param name="particles">The particle renderer owning this effect's native draw layer.</param>
        private void DrawAdventureExplosion(AdventureExplosion explosion, OriginalParticleRenderer particles)
        {
            var point = explosion.Effect == AdventureExplosionEffect.Pow
                ? new Vector2(explosion.NativeX, explosion.NativeY)
                : new Vector2(BoardLayout.AdventureGridX + explosion.X * BoardLayout.CellWidth, _owner.Layout.AdventureCellY(explosion.Row, explosion.X - .5f) + 40f);
            particles.Draw("native-explosion-" + explosion.Id, explosion.Particle, explosion.Ticks * .01f + AdventureRenderRemainder,
                point, explosion.Seed, fullScreenMatrix: ViewportMatrix());
        }

        /// <summary>Retains the hammer particle systems independently of the earlier board particle pass.</summary>
        private OriginalParticleRenderer? _hammerParticles;
        /// <summary>Identifies the simulation owning the retained hammer particle systems.</summary>
        private GameSimulation? _hammerPresentedGame;

        /// <summary>Draws the PC 800000 hammer layer after the interface and collecting suns.</summary>
        private void DrawAdventureHammerHits()
        {
            var game = _owner._game;
            if (game == null || !game.IsAdventure
                || game.Adventure.Level.Mode != AdventureMode.WhackAZombie
                    && (game.ModeSession == null || game.ModeSession.Definition.Mechanic != ModeMechanic.Whack))
            {
                return;
            }

            var particles = _hammerParticles;
            if (!ReferenceEquals(_hammerPresentedGame, game) || particles == null)
            {
                particles?.Clear();
                particles = new OriginalParticleRenderer(_owner._root, _owner._animations);
                _hammerParticles = particles;
                _hammerPresentedGame = game;
            }

            particles.BeginFrame();
            foreach (var explosion in game.Presentation.Explosions)
            {
                if (explosion.Effect == AdventureExplosionEffect.Pow)
                {
                    DrawAdventureExplosion(explosion, particles);
                }
            }

            particles.EndFrame();
        }

        /// <summary>Performs draw adventure snow trails.</summary>
        /// <param name = "row">The row.</param>
        private void DrawAdventureSnowTrails(int row)
        {
            foreach (var trail in _owner.RequiredGame.Presentation.SnowTrails)
            {
                if (trail.Row != row)
                {
                    continue;
                }

                var fraction = Mathf.Clamp01(AdventureRenderRemainder * 100f);
                var origin = trail.Stopped ? new Vector2(trail.X, trail.Y) : new Vector2(Mathf.Lerp(trail.PreviousX, trail.X, fraction), Mathf.Lerp(trail.PreviousY, trail.Y, fraction));
                RequiredAdventureParticles.Draw("snow-trail-" + trail.Id, "SnowPeaTrail", trail.Age + AdventureRenderRemainder, origin, trail.Seed, 1f, trail.StopAge);
            }
        }

        /// <summary>Performs draw adventure opening world.</summary>
        private void DrawAdventureOpeningWorld()
        {
            if (_owner.Opening._adventureIntroFrame == null)
            {
                return;
            }

            _owner.Opening._adventureIntroDrawn = true;

            foreach (var blit in _owner.Opening._adventureIntroFrame.SodBlits)
            {
                if (blit.Width <= 0 || blit.Height <= 0)
                {
                    continue;
                }

                var image = _owner.Textures.Load(blit.Resource + (blit.Resource.StartsWith("background", StringComparison.Ordinal) ? ".jpg" : ".png"));
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
            if (!_owner.Opening.AdventureIntroActive || _owner.Opening._adventureIntroFrame == null)
            {
                return;
            }

            for (var previewIndex = 0; previewIndex < _owner.Opening._adventurePreview.Count; previewIndex++)
            {
                var preview = _owner.Opening._adventurePreview[previewIndex];
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
                // The original preview zombies are real board zombies, so each starts with its full ZombieInitialize equipment.
                zombie.ArmorHealth = AdventureZombieEquipmentRules.StartingArmor(zombie.Kind);
                zombie.ShieldHealth = AdventureZombieEquipmentRules.StartingShield(zombie.Kind);
                zombie.PresentationMaxArmor = zombie.ArmorHealth;
                zombie.PresentationMaxShield = zombie.ShieldHealth;
                zombie.IsSmall = preview.Scale <= .5f;
                string asset = AdventureAnimationCatalog.AdventureZombieAnimation(zombie.Kind), clip = preview.Animation;
                if (!_owner.AnimationCatalog.AdventureClipExists(asset, clip))
                {
                    clip = _owner.AnimationCatalog.AdventureClipExists(asset, "anim_idle") ? "anim_idle" : "anim_walk";
                }

                global::PlantsVsZombies.Reanimation.NativeAnimationInfo info;
                var seconds = _owner.Opening._adventureIntroVisualAge;
                if (_owner.RequiredAnimations.TryGetNativeAnimationInfo(asset, clip, out info) && preview.AnimationRate > 0)
                {
                    seconds += preview.InitialPhase * info.Count / preview.AnimationRate;
                }

                DrawAdventureZombiePose(zombie, asset, clip, new Vector2(preview.WorldX, preview.WorldY - preview.Altitude), preview.Scale, seconds, preview.AnimationRate, preview.Loop);
            }

            foreach (var animation in _owner.Opening._adventureIntroFrame.Animations)
            {
                if (animation.Asset != "StartReadySetPlant")
                {
                    _owner.RequiredAnimations.DrawNativeDefinition(animation.Asset, new Vector2(animation.X, animation.Y), 1f, animation.Seconds, false, animation.Rate);
                }
            }

            foreach (var particle in _owner.Opening._adventureIntroFrame.Particles)
            {
                RequiredAdventureParticles.Draw("opening-" + particle.Asset + "-" + particle.Y, particle.Asset, particle.Seconds, new Vector2(particle.X, particle.Y), (int)particle.Y + 137);
            }
        }

        /// <summary>Draws the original opening conversation bubble and Dave reanimation.</summary>
        private void DrawAdventureCrazyDave()
        {
            if ((!_owner.Opening.AdventureIntroActive && !_owner.Opening._vaseDialogueActive && !_owner.Tree._treeDaveIntroActive && !_owner.Garden._gardenDaveIntroActive && !_owner.Garden._gardenSaleDaveActive) || _owner.Dave._crazyDaveAnimation == null)
            {
                return;
            }

            if (_owner.Dave._crazyDaveMessageIndex >= 0)
            {
                DrawNativeProgressionImage("Store_SpeechBubble2.png", 285, 20);
                _crazyDaveLabel ??= new GUIStyle();
                _crazyDaveLabel.font = _owner._font;
                _crazyDaveLabel.fontSize = 16;
                _crazyDaveLabel.alignment = TextAnchor.MiddleCenter;
                _crazyDaveLabel.wordWrap = true;
                _crazyDaveLabel.normal.textColor = Color.black;
                CanvasUI.Label(new Rect(310, 26, 233, 144), _owner.Dave._crazyDaveText, _crazyDaveLabel);
                if (!_owner.Garden._gardenSalePlant.HasValue)
                {
                    Label(new Rect(340, 148, 168, 16), OriginalProgressionText("CLICK_TO_CONTINUE"), 10, Color.black, TextAnchor.MiddleCenter, false);
                }
            }

            DrawCrazyDaveAt(Vector2.zero);
        }

        /// <summary>Draws Dave and his attached blink at the supplied original-screen position.</summary>
        /// <param name="origin">The original-screen drawing origin.</param>
        private void DrawCrazyDaveAt(Vector2 origin)
        {
            var animation = _owner.Dave._crazyDaveAnimation;
            if (animation == null)
            {
                return;
            }

            var blinking = _owner.Dave._crazyDaveBlinkAge >= 0f;
            var idle = animation == "anim_idle" || animation == "anim_idle_handing";
            _owner.RequiredAnimations.DrawNativePlayback("CrazyDave", origin, 1f, _owner.Dave._crazyDaveAnimationAge,
                animation, _owner.Dave._crazyDaveAnimationLoop, _owner.Dave._crazyDaveAnimationRate,
                visible: blinking ? s_crazyDaveBlinkVisible : null,
                overrideTrack: idle && _owner.Dave._crazyDaveIdleMouth != null ? "Dave_mouths" : null,
                overrideImage: idle ? _owner.Dave._crazyDaveIdleMouth : null,
                crazyDaveHeldItem: _owner.Dave._crazyDaveHeldItem, crazyDaveHeldItemAge: _owner.Dave._crazyDaveHeldItemAge);
            if (blinking)
            {
                _owner.RequiredAnimations.DrawNativeCrazyDaveBlink(origin, 1f, animation, _owner.Dave._crazyDaveAnimationAge,
                    _owner.Dave._crazyDaveAnimationRate, _owner.Dave._crazyDaveAnimationLoop, _owner.Dave._crazyDaveBlinkAge);
            }
        }

        /// <summary>Hides Dave's eye while the attached blink animation is drawn.</summary>
        private static readonly Func<string, bool> s_crazyDaveBlinkVisible = track => track != "Dave_eye";

        /// <summary>Performs draw adventure opening interface.</summary>
        /// <returns>The resulting draw adventure opening interface.</returns>
        private bool DrawAdventureOpeningInterface()
        {
            if (!_owner.Opening.AdventureIntroActive || _owner.Opening._adventureIntroFrame == null)
            {
                return false;
            }

            var frame = _owner.Opening._adventureIntroFrame;
            _owner.Opening._adventureIntroDrawn = true;

            if (_owner.RequiredGame.ModeSession == null && _owner.RequiredGame.Adventure.Level.Mode == AdventureMode.Bowling)
            {
                if (_owner.Opening._bowlingShovelLessonActive)
                {
                    var advice = _owner.AdventureBoard.AdventureTutorialText(_owner.RequiredGame.Adventure.Tutorial);
                    if (!string.IsNullOrEmpty(advice))
                    {
                        DrawOriginalBoardAdvice(advice, false);
                    }
                }
            }

            if (_owner.Opening.RequiredAdventureIntro.Plan.DrawSeedBank && frame.SeedBankY > -87)
            {
                var slots = _owner.Layout.SeedBankCapacity;
                var origin = _owner.Layout.AdventureSeedBankOrigin;
                DrawAdventureSeedBankBackground(origin, slots);
                DrawAdventureSunAmount(origin);
                IReadOnlyList<PlantKind>? selectedSeeds = _owner.Opening.RequiredAdventureIntro.Plan.ChooseSeeds
                    ? _owner._pendingSeeds
                    : _owner._deck;
                if (frame.ChooserVisible)
                {
                    DrawChooserEmptySlots(origin, slots);
                }

                if (selectedSeeds == null)
                {
                    throw new System.InvalidOperationException("The value 'selectedSeeds' is not available.");
                }

                for (var i = 0; i < selectedSeeds.Count; i++)
                {
                    if (_owner.Opening.RequiredAdventureIntro.Plan.ChooseSeeds && _owner.RequiredPendingSeeds.IsMoving(selectedSeeds[i]))
                    {
                        continue;
                    }

                    var card = _owner.Layout.AdventureSeedRect(i);
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

            if (_owner.Opening.RequiredAdventureIntro.Plan.ChooseSeeds)
            {
                DrawChooserMovingPackets(_owner.RequiredGame.Adventure.AvailableSeeds,
                    _owner.Layout.SeedBankCapacity);
            }

            foreach (var animation in frame.Animations)
            {
                if (animation.Asset == "StartReadySetPlant")
                {
                    _owner.RequiredAnimations.DrawNativeDefinition(animation.Asset, new Vector2(animation.X, animation.Y), 1f, animation.Seconds, false, animation.Rate);
                }
            }

            return true;
        }
        /// <summary>Draws a chooser packet's home, dimmed while its packet occupies the bank or flies.</summary>
        /// <param name="kind">The displayed packet.</param>
        /// <param name="card">The packet's chooser position.</param>
        private void DrawChooserHome(PlantKind kind, Rect card)
        {
            var previous = CanvasUI.Color;
            if (_owner.RequiredPendingSeeds.Contains(kind) || _owner.RequiredPendingSeeds.IsMoving(kind))
            {
                CanvasUI.Color = new Color(previous.r * 55f / 255f, previous.g * 55f / 255f, previous.b * 55f / 255f, previous.a);
            }

            DrawAdventureCard(kind, card, true);
            CanvasUI.Color = previous;
        }

        /// <summary>Draws settled chooser packets using the bank capacity rather than the current selection count.</summary>
        /// <param name="slots">The bank's configured capacity.</param>
        private void DrawChooserBank(int slots)
        {
            var origin = _owner.Layout.AdventureSeedBankOrigin;
            DrawAdventureSeedBankBackground(origin, slots);
            DrawAdventureSunAmount(origin);
            DrawChooserEmptySlots(origin, slots);
            for (var i = 0; i < _owner.RequiredPendingSeeds.Count; i++)
            {
                var seed = _owner.RequiredPendingSeeds[i];
                if (!_owner.RequiredPendingSeeds.IsMoving(seed))
                {
                    DrawAdventureCard(seed, new Rect(origin.x + AdventureSeedSlotLayout.PositionX(i, slots), origin.y + 8, 50, 70), true);
                }
            }
        }

        /// <summary>Draws original silhouettes in chooser slots that do not yet contain a settled packet.</summary>
        /// <param name="origin">The current native bank position.</param>
        /// <param name="slots">The full configured capacity, including slots beyond the current selection.</param>
        private void DrawChooserEmptySlots(Vector2 origin, int slots)
        {
            // PC 0x49A5C0..0x49A60F asks 49B930 for IN_BANK packets only;
            // entering, returning, and gap-closing flights retain an empty silhouette below them.
            for (var index = 0; index < slots; index++)
            {
                if (index >= _owner.RequiredPendingSeeds.Count || _owner.RequiredPendingSeeds.IsMoving(_owner.RequiredPendingSeeds[index]))
                {
                    DrawImage("SeedPacketSilhouette.png", new Rect(origin.x + AdventureSeedSlotLayout.PositionX(index, slots), origin.y + 8, 50, 70));
                }
            }
        }

        /// <summary>Draws flying packets after the chooser panel so the original flights remain visible above it.</summary>
        /// <param name="seeds">The available packets in chooser grid order.</param>
        /// <param name="slots">The bank's configured capacity.</param>
        /// <param name="mode">Whether independent mode grid positions should be used.</param>
        private void DrawChooserMovingPackets(IReadOnlyList<PlantKind> seeds, int slots, bool mode = false)
        {
            for (var i = 0; i < seeds.Count; i++)
            {
                var seed = seeds[i];
                if (_owner.RequiredPendingSeeds.IsMoving(seed))
                {
                    var home = mode ? BoardLayout.AdventureChooserRect(i) : _owner.Opening.AdventureCampaignChooserRect(seed);
                    _owner.RequiredPendingSeeds.GetPosition(seed, (int)home.x, (int)home.y, slots, out var x, out var y);
                    DrawAdventureCard(seed, new Rect(x, y, 50, 70), true);
                }
            }
        }

        /// <summary>Draws the development chooser packet using its existing icon and cost presentation.</summary>
        /// <param name="kind">The plant represented by the packet.</param>
        /// <param name="card">The packet's logical rectangle.</param>
        private void DrawSimpleChooserCard(PlantKind kind, Rect card)
        {
            var packets = _owner.Textures.Load("seeds.png");
            if (packets != null)
            {
                CanvasUI.DrawTextureWithTexCoords(card, packets, new Rect(0, 0, 1f / 9f, 1));
            }

            DrawSeedIcon(kind, card);
            Label(new Rect(card.x + 1, card.y + 51, 46, 17), NumberLabel(GameSimulation.Cost(kind)), 13, Color.black, TextAnchor.MiddleRight, true);
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
            var highlighted = _owner._shovel && ReferenceEquals(plant, _owner.Layout.AdventureShovelTarget(_owner.AdventureBoard._adventurePointer));
            RegisterPlantSurface(plant, highlighted);
            ActorRenderer.DrawPlant(plant, "PotatoMine", nativeOrigin, nativeScale, age, clip, plant.MinePhase != MinePhase.Rising,
                rate, null, _presentationKeys.Get("plant-", plant.Id), new Rect(origin.x - 80, origin.y - 160, 300, 340),
                plant.MinePhase == MinePhase.Rising ? 20 : 0, AdventureRenderRemainder, _presentationKeys.Get("producer-plant-", plant.Id), highlighted);

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

            ReanimationDefinition? definition;
            var name = VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Plants[id].ReanimationId, out definition) ? Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/')) : "PeaShooterSingle";
            s_plantAnimationNames.Add(kind, name);
            return name;
        }

        /// <summary>Performs draw adventure board.</summary>
        private void DrawAdventureBoard()
        {
            BeginAdventurePresentation();
            var openingMatrix = CanvasUI.Matrix;
            if (_owner.Opening.AdventureIntroActive)
            {
                CanvasUI.Matrix = openingMatrix * Matrix4x4.Translate(new Vector3(_owner.Opening.RequiredAdventureIntroFrame.BoardX, _owner.Opening.RequiredAdventureIntroFrame.BoardY, 0));
            }

            try
            {
                var level = _owner.RequiredGame.Adventure.Level;
                var background = level.Terrain == AdventureTerrain.Night ? "background2.jpg" : level.Terrain == AdventureTerrain.Pool ? "background3.jpg" : level.Terrain == AdventureTerrain.Fog ? "background4.jpg" : level.Terrain == AdventureTerrain.Roof ? "background5.jpg" : level.Terrain == AdventureTerrain.BossRoof ? "background6boss.jpg" : "background1.jpg";
                if (_owner.Opening._adventureIntroFrame != null && !level.IsReplay && level.Number <= 4)
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

                if (!_owner.Opening.AdventureIntroActive && !_owner._choosingSeeds)
                {
                    // PC Board::Draw 0x421D80 sorts BottomUi at 100001 in play; 0x425CDD draws the whole bank.
                    if (_owner.IndependentModes.IsModeBoard)
                    {
                        DrawModeSeedBank();
                    }
                    else
                    {
                        DrawAdventureSeedBankContents();
                    }

                    DrawAdventurePacketReadyFlashes();
                }

                // PC Board::DrawBottomUI 0x425B40 includes the shovel at layer 100001 during play.
                DrawAdventureShovelBank();

                foreach (var crater in _owner.RequiredGame.Adventure.Craters)
                {
                    DrawAdventureCrater(crater);
                }

                foreach (var ice in _owner.RequiredGame.Adventure.IceTrails)
                {
                    DrawImage("ice.png", new Rect(BoardLayout.AdventureGridX + ice.Column * BoardLayout.CellWidth, _owner.Layout.AdventureCellY(ice.Row, ice.Column) + 44, 84, 42));
                }

                if (level.Mode == AdventureMode.Bowling && (!_owner.Opening.AdventureIntroActive || _owner.Opening._bowlingLineVisible))
                {
                    Fill(new Rect(278, 90, 4, 475), new Color(0.9f, 0.08f, 0.04f));
                }

                if (level.Mode == AdventureMode.Boss)
                {
                    _owner.RequiredAnimations.DrawBossNative(new Vector2(0, 0), 1f, _owner.RequiredGame.Elapsed, _owner.RequiredGame.Adventure.BossHeadVulnerable);
                }

                for (var row = 0; row < _owner.RequiredGame.RowCount; row++)
                {
                    foreach (var grave in _owner.RequiredGame.Adventure._graves)
                    {
                        if (grave.Row == row)
                        {
                            DrawAdventureGrave(grave);
                        }
                    }

                    if (_owner.RequiredGame.Adventure.RakeRow == row)
                    {
                        _owner.RequiredAnimations.DrawNativePlayback("Rake", new Vector2(BoardLayout.AdventureGridX + _owner.RequiredGame.Adventure.RakeColumn * BoardLayout.CellWidth + 20, _owner.Layout.AdventureCellY(row, _owner.RequiredGame.Adventure.RakeColumn)), 1f, _owner.RequiredGame.Adventure.RakeTriggered ? _owner.RequiredGame.Adventure.RakeAge + AdventureRenderRemainder : 0f, null, false, 20f);
                    }

                    foreach (Mower mower in _owner.RequiredGame.Mowers)
                    {
                        if (mower.Row != row || !mower.Available && !mower.Active)
                        {
                            continue;
                        }

                        if (!_owner.Opening.AdventureIntroActive || _owner.Opening.RequiredAdventureIntroFrame.MowerX[mower.Row] > -80)
                        {
                            DrawAdventureMower(mower);
                        }
                    }

                    for (var layer = 0; layer <= (int)PlantLayer.Effect; layer++)
                    {
                        foreach (Plant plant in _owner.RequiredGame.Plants)
                        {
                            if (plant.Row == row && (int)plant.Layer == layer)
                            {
                                DrawAdventurePlant(plant);
                            }
                        }
                    }

                    DrawAdventurePlantPreview(row);
                    foreach (var vase in _owner.RequiredGame.Adventure.Vases)
                    {
                        if (vase.Row != row)
                        {
                            continue;
                        }

                        var image = _owner.Textures.Load("Scary_Pot.png");
                        if (image != null)
                        {
                            DrawTextureWithSource(new Rect(BoardLayout.AdventureGridX + vase.Column * BoardLayout.CellWidth, _owner.Layout.AdventureCellY(row, vase.Column) - 6, 80, 101), image, new Rect(vase.ShowsPlant ? 1f / 3f : 0, 0, 1f / 3f, 0.5f));
                        }
                    }

                    DrawAdventureDetachedZombieParts(row, true);
                    DrawAdventureZombieFootsteps(row);
                    DrawAdventureCorpses(row);
                    foreach (Zombie zombie in _owner.RequiredGame.Zombies)
                    {
                        if (zombie.Row == row)
                        {
                            DrawAdventureZombie(zombie);
                        }
                    }

                    foreach (var effect in _owner.RequiredGame.Presentation.ZombieRiseEffects)
                    {
                        if (effect.Row == row)
                        {
                            RequiredAdventureParticles.Draw(_presentationKeys.Get("grave-zombie-rise-", effect.Id), effect.Dancer ? "DancerRise" : effect.Whack ? "WhackAZombieRise" : "ZombieRise",
                                effect.Age + AdventureRenderRemainder, new Vector2(effect.X, effect.Y), effect.Id);
                        }
                    }

                    DrawAdventureDetachedArms(row);
                    DrawAdventureDetachedZombieParts(row, false);
                    DrawAdventurePlantBiteEffects(row);
                    DrawAdventureImpacts(row);
                    DrawAdventureExplosionRow(row);
                    foreach (Pea pea in _owner.RequiredGame.Peas)
                    {
                        if (pea.Row != row)
                        {
                            continue;
                        }

                        if (pea.FlightRow < 0 || pea.FlightRow >= _owner.RequiredGame.RowCount)
                        {
                            continue;
                        }

                        DrawAdventureProjectile(pea);
                    }

                    DrawAdventureSnowTrails(row);
                    foreach (var nut in _owner.RequiredGame.Adventure.BowlingNuts)
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

                        var giant = _owner.RequiredGame.ModeSession != null && _owner.RequiredGame.RequiredModeSession._giantNuts.Contains(nut);
                        AdventureBowlingPresentation pose;
                        _owner.RequiredGame.Presentation.Bowling.TryGetValue(nut, out pose);
                        _owner.RequiredAnimations.DrawNativePlayback("Wallnut", new Vector2((nut.NativeMotionInitialized ? nut.NativePixelX : BoardLayout.AdventureGridX + nut.X * BoardLayout.CellWidth - 40) + (giant ? -76 : 0), (nut.NativeMotionInitialized ? nut.NativePixelY : _owner.Layout.AdventureCellY(row, nut.X)) + (giant ? -64 : 0)), giant ? 2f : 1f, (pose == null ? _owner.RequiredGame.Elapsed : pose.Age) + AdventureRenderRemainder, "_ground", true, pose == null ? 12 : pose.Rate);
                        CanvasUI.Color = previous;
                    }

                    foreach (var ball in _owner.RequiredGame.Adventure.BossBalls)
                    {
                        if (ball.Row == row)
                        {
                            _owner.RequiredAnimations.Draw(ball.IsIce ? "Zombie_boss_iceball" : "Zombie_boss_fireball", new Rect(BoardLayout.AdventureGridX + ball.X * BoardLayout.CellWidth - 40, _owner.Layout.AdventureCellY(row, ball.X) - 2, 80, 80), _owner.RequiredGame.Elapsed, "anim_role");
                        }
                    }

                    foreach (LaneFlame flame in _owner.RequiredGame.Flames)
                    {
                        if (flame.Row == row)
                        {
                            for (var c = 0; c < 10; c++)
                            {
                                _owner.RequiredAnimations.Draw("fire", new Rect(BoardLayout.AdventureGridX + c * 76, _owner.Layout.AdventureCellY(row, c) - 10, 84, 108), flame.Age + c * 0.025f, "anim_flame");
                            }
                        }
                    }
                }

                DrawAdventurePlantingEffects();
                DrawAdventureExplosionOverlay();
                foreach (global::PlantsVsZombies.Simulation.Sun sun in _owner.RequiredGame.Suns)
                {
                    if (!sun.PresentationNative || !sun.PresentationPickup.Collecting)
                    {
                        DrawAdventureSun(sun);
                    }
                }

                foreach (var coin in _owner.RequiredGame.Adventure.Coins)
                {
                    var coinRect = _owner.Layout.AdventureCoinRect(coin);
                    if (coin.Kind == AdventureCoinKind.PlantGift || coin.Kind == AdventureCoinKind.Chocolate || coin.Kind == AdventureCoinKind.MinigameUnlock || coin.Kind == AdventureCoinKind.PuzzleUnlock)
                    {
                        var image = coin.Kind == AdventureCoinKind.Chocolate ? "chocolate.png" : "Present.png";
                        var gift = _owner.Textures.Load(image);
                        if (gift != null)
                        {
                            DrawImage(image, new Rect(coinRect.center.x - gift.width * .5f, coinRect.center.y - gift.height * .5f, gift.width, gift.height));
                        }

                        continue;
                    }

                    var interpolation = AdventureRenderRemainder * 100f;
                    var x = Mathf.Lerp(coin.PresentationPreviousX, coin.PresentationX, interpolation);
                    var y = Mathf.Lerp(coin.PresentationPreviousY, coin.PresentationY, interpolation);
                    var diamond = coin.Kind == AdventureCoinKind.Diamond;
                    var tint = CanvasUI.Color;
                    CanvasUI.Color = new Color(tint.r, tint.g, tint.b, tint.a * coin.PresentationAlpha);
                    if (!diamond && coin.IsBeingCollected)
                    {
                        // PC 0x43CC10/0x43CEE0 hides rotating money during collection and draws its dollar face.
                        _owner.RequiredAnimations.DrawResourceSprite("IMAGE_REANIM_COINGLOW", new Vector2(x + 26f, y + 28f), coin.PresentationScale);
                        var gold = coin.Kind == AdventureCoinKind.Gold;
                        _owner.RequiredAnimations.DrawResourceSprite(gold ? "IMAGE_REANIM_COIN_GOLD_DOLLAR" : "IMAGE_REANIM_COIN_SILVER_DOLLAR",
                            new Vector2(x + (gold ? 30.5f : 29.5f), y + (gold ? 32.5f : 30f)), coin.PresentationScale);
                    }
                    else
                    {
                        // PC 0x43B610 attachment origins; 0x43CC10 keeps a shrinking diamond centred on its original pivot.
                        var origin = diamond
                            ? new Vector2(x + 15f - 18f * coin.PresentationScale, y + 17f - 13f * coin.PresentationScale)
                            : new Vector2(x + 9f, y + 9f);
                        _owner.RequiredAnimations.DrawNative(diamond ? "Diamond" : coin.Kind == AdventureCoinKind.Gold ? "Coin_gold" : "Coin_silver", origin, coin.PresentationScale, coin.Age);
                    }

                    CanvasUI.Color = tint;
                }

                foreach (var shell in _owner.RequiredGame.Adventure.CobShells)
                {
                    var ticks = Math.Max(0, GameSimulation.AdventureCobFlightSeconds - shell.FlightRemaining + AdventureRenderRemainder) * 100f;
                    var rising = ticks < 88f;
                    var x = rising ? shell.LaunchX : shell.TargetX;
                    var row = rising ? shell.LaunchRow : shell.TargetRow;
                    var z = rising ? -8f * ticks : -704f + 8f * (ticks - 88f);
                    _owner.RequiredAnimations.DrawResourceSprite("IMAGE_REANIM_COBCANNON_COB", new Vector2(BoardLayout.AdventureGridX + x * BoardLayout.CellWidth, BoardLayout.AdventureGridY + row * _owner.Layout.AdventurePitch + _owner.Layout.AdventureSlope(x) + z), 1f, rising ? 90 : -90);
                }

                if (level.Terrain == AdventureTerrain.Fog && _owner.RequiredGame.Adventure.FogBlownRemaining <= 0)
                {
                    for (var row = 0; row < _owner.RequiredGame.RowCount; row++)
                    {
                        for (var col = 0; col < 9; col++)
                        {
                            if (!_owner.RequiredGame.IsAdventureCellVisible(row, col))
                            {
                                var fog = _owner.Textures.Load("fog.png");
                                if (fog != null)
                                {
                                    DrawTextureWithSource(new Rect(BoardLayout.AdventureGridX + col * BoardLayout.CellWidth - 55, _owner.Layout.AdventureCellY(row, col) - 35, 190, 170), fog, new Rect((col % 8) / 8f, 0, 1f / 8f, 1));
                                }
                            }
                        }
                    }
                }

                if (level.Mode == AdventureMode.Storm && !_owner.RequiredGame.Adventure.StormFlash)
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
            Texture2D? texture;
            if (!_owner.Textures._textures.TryGetValue(relative, out texture))
            {
                var file = Path.Combine(_owner._root, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(file))
                {
                    _owner.Textures._textures[relative] = null;
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

                _owner.Textures._textures[relative] = texture;
            }

            if (texture != null)
            {
                var fit = Mathf.Min(destination.width / texture.width, destination.height / texture.height);
                var fitted = new Rect(destination.center.x - texture.width * fit * 0.5f, destination.center.y - texture.height * fit * 0.5f, texture.width * fit, texture.height * fit);
                DrawTextureWithSource(fitted, texture, new Rect(0, 0, 1, 1));
            }
        }

        /// <summary>Draws the original gravestone cell, soil mound, and emergence particles.</summary>
        /// <param name="grave">The grave whose original grid-item counter is sampled.</param>
        private void DrawAdventureGrave(AdventureCellTimer grave)
        {
            var pose = grave.GravePresentation;
            if (!_owner.RequiredGame.Adventure.GravesEnabled || pose == null)
            {
                return;
            }

            var origin = new Vector2(BoardLayout.AdventureGridX + grave.Column * BoardLayout.CellWidth,
                _owner.Layout.AdventureCellY(grave.Row, grave.Column));
            var stone = _owner.Textures.Load("Tombstones.png");
            if (stone != null)
            {
                var eaten = 0;
                foreach (var plant in _owner.RequiredGame.Plants)
                {
                    if (plant.Kind == PlantKind.GraveBuster && plant.Row == grave.Row && plant.Column == grave.Column && AdventureGraveBusterRules.IsEating(plant.ActionTimer))
                    {
                        eaten = AdventureIntroPresentation.Curve(0, 400, 400 - AdventureGraveBusterRules.EatingTicksRemaining(plant.ActionTimer), 10, 40, 1);
                        break;
                    }
                }

                DrawAdventureGraveBlit(stone, pose.Stone(grave.Row, stone.width / 5, stone.height / 4, eaten), origin);
            }

            var mound = _owner.Textures.Load("Tombstone_mounds.png");
            if (mound != null)
            {
                DrawAdventureGraveBlit(mound, pose.Mound(grave.Row, mound.width / 5, mound.height / 4), origin);
            }

            if (pose.Age < 1.6f)
            {
                RequiredAdventureParticles.Draw(_presentationKeys.Get("grave-rise-", grave.Row * 9 + grave.Column),
                    "GraveStoneRise", pose.Age, origin + new Vector2(14 + pose.OffsetX, 78 + pose.OffsetY),
                    grave.Row * 9 + grave.Column);
            }
        }

        /// <summary>Submits an unscaled and clipped original grave atlas operation.</summary>
        /// <param name="texture">The original stone or soil atlas.</param>
        /// <param name="blit">The source cell and relative destination.</param>
        /// <param name="origin">The lawn cell origin in board pixels.</param>
        private void DrawAdventureGraveBlit(Texture texture, NativeBlit blit, Vector2 origin)
        {
            var source = blit.Source;
            if (source.Height <= 0)
            {
                return;
            }

            var destination = blit.Destination;
            DrawTextureWithSource(new Rect(origin.x + destination.X, origin.y + destination.Y, destination.Width, destination.Height),
                texture, new Rect(source.X / (float)texture.width, 1f - (source.Y + source.Height) / (float)texture.height,
                    source.Width / (float)texture.width, source.Height / (float)texture.height));
        }

        /// <summary>Stores adventure particles.</summary>
        private OriginalParticleRenderer? _adventureParticles;

        /// <summary>Gets the adventure particles, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private OriginalParticleRenderer RequiredAdventureParticles => _adventureParticles ?? throw new System.InvalidOperationException("The member '_adventureParticles' is not available.");
        /// <summary>Stores adventure presented game.</summary>
        private GameSimulation? _adventurePresentedGame;
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
                    _owner.RequiredAnimations.ReleaseNativeSurface(key);
                }

                _adventurePresentationSurfaces.Clear();
                _owner.RequiredAnimations.ResetNativePlayback();
                _adventurePresentedGame = _owner._game;
                _adventureParticles = new OriginalParticleRenderer(_owner._root, _owner._animations);
            }

            _adventurePresentationLiveSurfaces.Clear();
            RequiredAdventureParticles.BeginFrame();
        }

        /// <summary>Performs end adventure presentation.</summary>
        private void EndAdventurePresentation()
        {
            RequiredAdventureParticles.EndFrame();
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
                _owner.RequiredAnimations.ReleaseNativeSurface(key);
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
            return _owner.RequiredAnimations.DrawNativeClipped(key, viewport, draw, false, grayscale, additive);
        }

        /// <summary>Gets or sets adventure render remainder.</summary>
        private float AdventureRenderRemainder
        {
            get
            {
                return _owner.RequiredGame.AdventurePresentationRemainder;
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
            return BoardLayout.AdventureGridY + row * _owner.Layout.AdventurePitch + (_owner.RequiredGame.Adventure.Level.HasRoof ? Mathf.Max(0, 440f - x) * .25f - 10f : 0f);
        }

        /// <summary>Places native zombie artwork from the PC row baseline and unscaled species offsets.</summary>
        /// <param name="zombie">The zombie supplying its species and existing jump or drop state.</param>
        /// <param name="x">The interpolated horizontal board coordinate.</param>
        /// <returns>The native source origin before the body's scaling pivot is applied.</returns>
        private Vector2 AdventureZombieSourceOrigin(Zombie zombie, float x)
        {
            var size = AdventureDancerRules.Scale(zombie);
            var pixelX = AdventureZombieNativeRules.ReanimationSourceX(x, size);
            var nativeX = AdventureZombieNativeRules.NativePositionX(x, size);
            // PC 0x547FF0 applies the body overlay eight pixels above mPosY.
            var y = AdventureFloorYAtPixel(zombie.Row, nativeX + 40f) - 38f;
            if (zombie.Kind == ZombieKind.Balloon)
            {
                y -= 30f;
            }
            else if (zombie.Kind == ZombieKind.Pogo)
            {
                // PC GetPosYBasedOnRow (0x54D620) always lowers the pogo's native position by 16.
                y -= 16f;
            }

            // PC GetDrawPos (0x549740) applies unscaled species offsets before the body overlay.
            switch (zombie.Kind)
            {
                case ZombieKind.Football:
                    y -= 16f;
                    break;
                case ZombieKind.PoleVaulter:
                    pixelX -= 6f;
                    y -= 11f;
                    break;
                case ZombieKind.Yeti:
                    y -= 20f;
                    break;
                case ZombieKind.Catapult:
                    pixelX -= 25f;
                    y -= 18f;
                    break;
                case ZombieKind.Pogo:
                    y += 16f;
                    break;
                case ZombieKind.Balloon:
                    y += 17f;
                    break;
                case ZombieKind.Zomboni:
                    pixelX += 68f;
                    y -= 23f;
                    break;
                case ZombieKind.Gargantuar:
                    y -= 8f;
                    break;
                case ZombieKind.Bobsled:
                    y -= 12f;
                    break;
                case ZombieKind.Bungee:
                    pixelX -= 18f;
                    break;
            }

            if (zombie.Kind == ZombieKind.Pogo && !zombie.UsedAbility)
            {
                y -= 16f;
            }

            if (zombie.DropRemaining > 0)
            {
                y -= Mathf.Min(260f, zombie.DropRemaining * 120f);
            }

            if (zombie.GraveRiseTicks > 0)
            {
                y -= AdventureZombieRiseRules.Altitude(zombie.GraveRiseTicks);
            }
            else if (zombie.PresentationNativePhase == 50 && zombie.DancerState != null)
            {
                y -= AdventureDancerRules.RiseAltitude(zombie.DancerState.PhaseTicks);
            }

            return new Vector2(pixelX, y);
        }

        /// <summary>Applies the original body scaling pivot and the small football alignment correction.</summary>
        /// <param name="zombie">The zombie supplying its species and body scale.</param>
        /// <param name="x">The interpolated horizontal board coordinate.</param>
        /// <returns>The placement origin passed to the unchanged native reanimation tracks.</returns>
        private Vector2 AdventureZombieOrigin(Zombie zombie, float x)
        {
            var scale = AdventureDancerRules.Scale(zombie);
            var origin = AdventureZombieSourceOrigin(zombie, x) + new Vector2(20f * (1f - scale), 120f * (1f - scale));
            if (zombie.Kind == ZombieKind.Football && scale < 1f)
            {
                // PC UpdateReanim (0x548204..0x548225) preserves the small football body's ground alignment.
                origin.y += 20f * (1f - scale);
            }

            return origin;
        }

        /// <summary>Performs draw adventure mower.</summary>
        /// <param name = "mower">The mower.</param>
        private void DrawAdventureMower(Mower mower)
        {
            var pool = _owner.RequiredGame.Adventure.Level.IsWaterRow(mower.Row) && _owner.RequiredGame.Rules.AdventurePoolCleaners;
            var roof = _owner.RequiredGame.Adventure.Level.HasRoof;
            var name = roof ? "RoofCleaner" : pool ? "PoolCleaner" : "LawnMower";
            var clip = pool ? mower.X > .125f ? "anim_water" : "anim_land" : roof ? null : "anim_normal";
            float age;
            _owner.RequiredGame.Presentation.MowerTimes.TryGetValue(mower, out age);
            if (mower.Active)
            {
                age += AdventureRenderRemainder;
            }
            else
            {
                age = 0;
            }

            var x = _owner.Opening.AdventureIntroActive ? _owner.Opening.RequiredAdventureIntroFrame.MowerX[mower.Row] : BoardLayout.AdventureGridX + mower.X * BoardLayout.CellWidth - 21f;
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

            _owner.RequiredAnimations.DrawNativePlayback(name, origin, pool ? .8f : .85f, age, clip, true, mower.Active ? pool ? 35f : 70f : 0f);
        }

        /// <summary>Draws one original crater atlas cell with its native terrain, aging and fade.</summary>
        /// <param name="crater">The occupied cell and remaining original crater lifetime.</param>
        private void DrawAdventureCrater(AdventureCellTimer crater)
        {
            // PC GridItem::DrawCrater 0x458EA0 draws one cel of a two-column image at its original size.
            var level = _owner.RequiredGame.Adventure.Level;
            var x = BoardLayout.AdventureGridX + crater.Column * BoardLayout.CellWidth - 8f;
            var y = _owner.Layout.AdventureCellY(crater.Row, crater.Column) + 40f;
            var fading = crater.Remaining < 90f;
            var image = "crater.png";
            var column = 0;
            if (level.IsWaterRow(crater.Row))
            {
                image = level.IsNight ? "crater_water_night.png" : "crater_water_day.png";
                column = fading ? 1 : 0;
                var phase = crater.Row * Mathf.PI + crater.Column * Mathf.PI * .25f
                    + _owner.RequiredGame.Adventure._ticks * Mathf.PI * 2f / 200f;
                y += (float)Math.Sin(phase) * 2f;
            }
            else if (level.HasRoof)
            {
                if (crater.Column < 5)
                {
                    image = "crater_roof_left.png";
                    x += 16f;
                    y -= 16f;
                }
                else
                {
                    image = "crater_roof_center.png";
                    x += 18f;
                    y -= 9f;
                }

                column = fading ? 1 : 0;
            }
            else
            {
                column = level.IsNight ? 1 : 0;
                if (fading)
                {
                    image = "crater_fading.png";
                }
            }

            var texture = _owner.Textures.Load(image);
            if (texture == null)
            {
                return;
            }

            var previous = CanvasUI.Color;
            if (crater.Remaining < .25f)
            {
                var ticks = Math.Max(0, (int)Math.Round(crater.Remaining * 100f));
                var alpha = (int)Math.Round(ticks * 255f / 25f, MidpointRounding.AwayFromZero);
                CanvasUI.Color = new Color(previous.r, previous.g, previous.b, previous.a * alpha / 255f);
            }

            try
            {
                DrawTextureWithSource(new Rect(x, y, texture.width * .5f, texture.height), texture, new Rect(column * .5f, 0f, .5f, 1f));
            }
            finally
            {
                CanvasUI.Color = previous;
            }
        }

        /// <summary>Returns the original independent shake of the active Doom-shroom head track.</summary>
        /// <param name="track">The native image track currently being drawn.</param>
        /// <returns>The recorded two-dimensional head displacement, or zero for an unaffected track.</returns>
        private Vector2 DoomShroomTrackOffset(string track)
        {
            var state = _doomShroomState;
            if (state == null)
            {
                return Vector2.zero;
            }

            var head = string.Equals(track, "DoomShroom_head1", StringComparison.OrdinalIgnoreCase) ? 0
                : string.Equals(track, "DoomShroom_head2", StringComparison.OrdinalIgnoreCase) ? 1
                : string.Equals(track, "DoomShroom_head3", StringComparison.OrdinalIgnoreCase) ? 2 : -1;
            return head < 0 ? Vector2.zero : new Vector2(state.HeadShakeX[head], state.HeadShakeY[head]);
        }

        /// <summary>Performs adventure plant height.</summary>
        /// <param name = "plant">The plant.</param>
        /// <returns>The resulting adventure plant height.</returns>
        private float AdventurePlantHeight(Plant plant)
        {
            var id = (int)plant.Kind;
            var offset = id < 49 ? AlmanacPortraitLayout.PlantHeightOffset(id) : 0;
            if (_owner.RequiredGame.Adventure.Level.HasRoof)
            {
                offset -= 12f;
            }

            var hasFlowerPot = false;
            if (plant.Kind != PlantKind.FlowerPot)
            {
                foreach (var support in _owner.RequiredGame.Plants)
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
            var firing = !plant.Asleep && age >= 0 && _owner.RequiredAnimations.TryGetNativeAnimationInfo(name, shoot, out info) && age < info.Count / rate;
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
            float x = BoardLayout.AdventureGridX + plant.Column * BoardLayout.CellWidth, y = _owner.Layout.AdventureCellY(plant.Row, plant.Column);
            var name = AdventurePlantAnimation(plant.Kind);
            var size = plant.IsSmall ? .5f : 1f;
            if (plant.Kind == PlantKind.PuffShroom)
            {
                var shadowName = _owner.RequiredGame.Adventure.Level.IsNight ? "plantshadow2.png" : "plantshadow.png";
                var shadow = _owner.Textures.Load(shadowName);
                if (shadow != null)
                {
                    // PC 0x473AD0: offset(-3,42), scale .5; 0x527D20 scales around the unscaled image center.
                    var width = shadow.width * .5f * size;
                    var height = shadow.height * .5f * size;
                    var center = new Vector2(x - 3f + shadow.width * .5f, y + AdventurePlantHeight(plant) + 42f + shadow.height * .5f);
                    DrawImage(shadowName, new Rect(center.x - width * .5f, center.y - height * .5f, width, height));
                }
            }
            else
            {
                DrawImage("plantshadow.png", new Rect(x + 1, y + 65, 76 * plant.ColumnSpan, 24));
            }
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
            var doom = plant.Kind == PlantKind.DoomShroom && !plant.Asleep ? plant.DoomShroomPresentation : null;
            if (doom != null)
            {
                // PC UpdateDoomShroom 0x46F1A0 selects this authored layer and its three head shake overrides.
                clip = "anim_explode";
                rate = AdventureDoomShroomState.Rate;
                age = doom.SampleSeconds(AdventureRenderRemainder);
                loop = false;
            }

            if (plant.Kind == PlantKind.GraveBuster)
            {
                var landing = !AdventureGraveBusterRules.IsEating(plant.ActionTimer);
                clip = landing ? "anim_land" : "anim_idle";
                age = landing ? plant.Age + AdventureRenderRemainder : Math.Max(0f, plant.Age - AdventureGraveBusterRules.LandingSeconds) + AdventureRenderRemainder;
                rate = 12f;
                loop = !landing;
                // Plant::Animate sinks the eating buster up to 30px as its countdown runs from 400 to 0.
                origin.y += AdventureGraveBusterRules.SinkOffset(plant.ActionTimer);
            }

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

            if (plant.Asleep && plant.Kind != PlantKind.SunShroom && _owner.AnimationCatalog.AdventureClipExists(name, "anim_sleep"))
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

            if (plant.Kind == PlantKind.Imitater && _owner.AnimationCatalog.AdventureClipExists(name, "anim_morph"))
            {
                clip = "anim_morph";
                loop = false;
            }

            if (plant.Kind == PlantKind.Chomper && plant.ChomperPresentation != null)
            {
                var chomper = plant.ChomperPresentation;
                clip = chomper.Animation;
                age = chomper.AnimationAge + AdventureRenderRemainder;
                rate = chomper.Rate > 0f ? chomper.Rate : rate;
                loop = chomper.Loop;
            }

            var shooting = plant.Kind == PlantKind.Starfruit ? "anim_shoot" : "anim_shooting";
            global::PlantsVsZombies.Reanimation.NativeAnimationInfo shot;
            if (!plant.Asleep && !AdventurePresentationRules.AttachedHead(plant.Kind) && plant.Kind != PlantKind.CobCannon && plant.PresentationShotAge >= 0 && _owner.RequiredAnimations.TryGetNativeAnimationInfo(name, shooting, out shot) && plant.PresentationShotAge < shot.Count / AdventurePresentationRules.ShootingRate(plant.Kind))
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
                    heads[0] = AdventurePlantHead(plant, name, "anim_head_idle", "anim_shooting", _owner.AnimationCatalog.AdventureClipExists(name, "anim_stem") ? "anim_stem" : "anim_idle", 0);
                }
            }

            var highlighted = _owner._shovel && ReferenceEquals(plant, _owner.Layout.AdventureShovelTarget(_owner.AdventureBoard._adventurePointer));
            RegisterPlantSurface(plant, highlighted);
            if (plant.Kind == PlantKind.Wallnut || plant.Kind == PlantKind.TallNut)
            {
                age = (plant.PresentationBodyFrames + AdventureRenderRemainder * plant.PresentationBodyRate) / 12f;
                rate = 12f;
            }

            _doomShroomState = doom;
            ActorRenderer.DrawPlant(plant, name, origin, size, heads == null ? age : plant.Age + AdventureRenderRemainder,
                clip, loop, rate, heads, _presentationKeys.Get("plant-", plant.Id), new Rect(x - 80, y - 160, 300, 340),
                doom != null ? 0 : plant.Kind == PlantKind.Threepeater || plant.Kind == PlantKind.Cattail ? 10 : 20, AdventureRenderRemainder,
                _presentationKeys.Get("producer-plant-", plant.Id), highlighted, doom == null ? null : _doomTrackOffset);

            if (plant.Kind == PlantKind.FumeShroom && plant.PresentationShotAge >= .35f && plant.PresentationShotAge < 1.35f)
            {
                RequiredAdventureParticles.Draw(_presentationKeys.Get("fume-", plant.Id), "FumeCloud", plant.PresentationShotAge - .35f, new Vector2(x + 85, y + 31), plant.Id);
            }

            if (plant.Kind == PlantKind.GloomShroom)
            {
                var nativeBursts = s_gloomBursts;
                for (var burst = 0; burst < nativeBursts.Length; burst++)
                {
                    if (plant.PresentationShotAge >= nativeBursts[burst] && plant.PresentationShotAge < nativeBursts[burst] + .6f)
                    {
                        RequiredAdventureParticles.Draw(_presentationKeys.Get("gloom-", plant.Id, burst), "GloomCloud", plant.PresentationShotAge - nativeBursts[burst], new Vector2(x + 40, y + 40), plant.Id + burst);
                    }
                }
            }
        }

        /// <summary>Tracks the composition surface used by a plant's active feedback.</summary>
        /// <param name="plant">The plant whose effects may require a retained surface.</param>
        /// <param name="highlighted">Whether the native shovel requires additive composition.</param>
        private void RegisterPlantSurface(Plant plant, bool highlighted = false)
        {
            if (highlighted || AdventureProducerGlow.Visual(plant).Enabled || plant.IsImitated || plant.PresentationEatenFlashTicks > 0)
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
        private string? AdventureZombieClip(Zombie zombie, string name)
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
            else if (zombie.Kind == ZombieKind.PoleVaulter && zombie.UsedAbility && zombie.AbilityTimer > 0)
            {
                clip = "anim_jump";
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
                clip = _owner.RequiredGame.Adventure.Level.IsWaterRow(zombie.Row) ? "anim_ride" : "anim_walkdolphin";
            }
            else if (zombie.Kind == ZombieKind.Snorkel && zombie.Submerged)
            {
                clip = "anim_swim";
            }
            else if (zombie.Kind == ZombieKind.DuckyTube && _owner.RequiredGame.Adventure.Level.IsWaterRow(zombie.Row))
            {
                clip = "anim_swim";
            }
            else if (zombie.Kind == ZombieKind.Zomboni || zombie.Kind == ZombieKind.Catapult)
            {
                clip = "anim_drive";
            }
            else
            {
                clip = zombie.PresentationWalk2 && _owner.AnimationCatalog.AdventureClipExists(name, "anim_walk2") ? "anim_walk2" : "anim_walk";
            }

            if (_owner.AnimationCatalog.AdventureClipExists(name, clip))
            {
                return clip;
            }

            return _owner.AnimationCatalog.AdventureClipExists(name, "anim_walk") ? "anim_walk" : _owner.AnimationCatalog.AdventureClipExists(name, "anim_idle") ? "anim_idle" : null;
        }

        /// <summary>Performs adventure zombie rate.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "name">The name.</param>
        /// <param name = "clip">The clip.</param>
        /// <param name = "size">The size.</param>
        /// <returns>The resulting adventure zombie rate.</returns>
        private float AdventureZombieRate(Zombie zombie, string name, string? clip, float size)
        {
            if (zombie.Kind == ZombieKind.PoleVaulter && zombie.UsedAbility && zombie.AbilityTimer > 0)
            {
                return 24f;
            }

            if (zombie.Eating)
            {
                return zombie.Kind == ZombieKind.PoleVaulter || zombie.Kind == ZombieKind.Balloon || zombie.Kind == ZombieKind.Imp || zombie.Kind == ZombieKind.Digger || zombie.Kind == ZombieKind.JackInTheBox || zombie.Kind == ZombieKind.Snorkel || zombie.Kind == ZombieKind.Yeti ? 20f : 36f;
            }

            global::PlantsVsZombies.Reanimation.NativeAnimationInfo info;
            if (_owner.RequiredAnimations.TryGetNativeAnimationInfo(name, clip, out info) && info.GroundDistance > .000001f)
            {
                return zombie.Speed * BoardLayout.CellWidth * .01f * 47f * info.Count / info.GroundDistance / size;
            }

            return _owner.RequiredAnimations.TryGetNativeAnimationInfo(name, clip, out info) ? info.SourceRate : 12f;
        }

        /// <summary>Performs draw adventure zombie.</summary>
        /// <param name = "zombie">The zombie.</param>
        private void DrawAdventureZombie(Zombie zombie)
        {
            if (!_owner.RequiredGame.IsModeZombieVisible(zombie))
            {
                return;
            }

            float size = AdventureDancerRules.Scale(zombie), x = AdventureRenderX(zombie);
            var origin = AdventureZombieOrigin(zombie, x);
            var inPool = zombie.PresentationInPool ?? (_owner.RequiredGame.Adventure.Level.IsWaterRow(zombie.Row) && x >= 0f && x < 9f);
            var shadowName = _owner.RequiredGame.Adventure.Level.IsNight ? "plantshadow2.png" : "plantshadow.png";
            var shadow = _owner.Textures.Load(shadowName);
            if (shadow != null && HasAdventureZombieShadow(zombie, inPool))
            {
                var nativeX = AdventureZombieNativeRules.NativePositionX(x, size);
                var floor = AdventureFloorYAtPixel(zombie.Row, nativeX + 40f);
                DrawImage(shadowName, AdventureZombieShadowBounds(zombie, x, floor, new Vector2(shadow.width, shadow.height), inPool));
            }
            string name = AdventureAnimationCatalog.AdventureZombieAnimation(zombie.Kind), clip = _owner.RequiredGame.AdventurePresentationZombieAnimation(zombie);
            if (!string.Equals(zombie.PresentationAnimation, clip, StringComparison.Ordinal))
            {
                zombie.PresentationAnimation = clip;
                zombie.PresentationAnimationAge = 0;
            }

            var age = zombie.PresentationAnimationAge + (zombie.FrozenRemaining > 0 ? 0 : AdventureRenderRemainder * (zombie.ChillRemaining > 0 ? .5f : 1f));
            var rate = _owner.RequiredGame.AdventurePresentationZombieRate(zombie);
            DrawAdventureZombiePose(zombie, name, clip, origin, size, age, rate,
                zombie.PresentationNewspaperGaspRemaining <= 0f && !string.Equals(clip, "anim_jump", StringComparison.Ordinal)
                && !(AdventureDancerRules.IsDancer(zombie) && zombie.PresentationNativePhase >= 41 && zombie.PresentationNativePhase <= 43));
            if (zombie.PresentationNewspaperSurpriseAge >= 0f)
            {
                // PC 0x54E050 attaches at mPosX-11,mPosY, independently of the body's +15,-8 overlay.
                var nativeX = AdventureZombieNativeRules.NativePositionX(x, size);
                var questionOrigin = new Vector2(nativeX - 11f, AdventureFloorYAtPixel(zombie.Row, nativeX + 40f) - 30f);
                _owner.RequiredAnimations.DrawNativePlayback("Zombie_surprise", questionOrigin, 1f,
                    zombie.PresentationNewspaperSurpriseAge + AdventureRenderRemainder, null, false, 12f);
            }

            if (zombie.FrozenRemaining > 0 && zombie.PresentationButter <= 0)
            {
                _owner.RequiredAnimations.DrawResourceSprite("IMAGE_ICETRAP", new Vector2(BoardLayout.AdventureGridX + x * BoardLayout.CellWidth, _owner.Layout.AdventureCellY(zombie.Row, x) + 79), size);
            }

            if (zombie.PresentationButter > 0)
            {
                Vector2 head;
                if (_owner.RequiredAnimations.TryGetNativeTrackPosition(name, clip, "anim_head1", age, rate, true, out head))
                {
                    _owner.RequiredAnimations.DrawResourceSprite("IMAGE_REANIM_CORNPULT_BUTTER_SPLAT", origin + (head + new Vector2(11, 21)) * size, size);
                }
            }
        }

        /// <summary>Checks the native shadow exclusions before submitting a zombie's ground projection.</summary>
        /// <param name="zombie">The current native zombie state.</param>
        /// <param name="inPool">Whether the zombie is currently in the pool.</param>
        /// <returns>Whether PC Zombie::HasShadow permits the projection.</returns>
        internal static bool HasAdventureZombieShadow(Zombie zombie, bool inPool)
        {
            // PC 0x54FD80 excludes these native kinds, heights, and animation phases.
            if (inPool || zombie.Kind == ZombieKind.Zomboni || zombie.Kind == ZombieKind.Catapult || zombie.Kind == ZombieKind.Boss
                || zombie.DropRemaining > 0f || zombie.PresentationNativeHeight == 3 || zombie.PresentationNativeHeight == 8 || zombie.PresentationNativeHeight == 9
                || zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 0)
            {
                return false;
            }

            switch (zombie.PresentationNativePhase)
            {
                case 1:
                case 33:
                case 34:
                case 35:
                case 32:
                case 14:
                case 50:
                case 18:
                case 12:
                case 52:
                case 58:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>Places a shadow using the original image-center scaling around an unscaled top-left origin.</summary>
        /// <param name="zombie">The displayed zombie, including size and facing state.</param>
        /// <param name="gridX">The same interpolated horizontal coordinate used by the body.</param>
        /// <param name="floorY">The native row floor evaluated at the body's ground position.</param>
        /// <param name="imageSize">The unscaled original shadow image dimensions.</param>
        /// <param name="inPool">Whether to use the original water shadow placement.</param>
        /// <returns>The native shadow bounds before the shared viewport transform.</returns>
        internal static Rect AdventureZombieShadowBounds(Zombie zombie, float gridX, float floorY, Vector2 imageSize, bool inPool)
        {
            var scale = AdventureDancerRules.Scale(zombie);
            var shadowScale = scale;
            var backwards = zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 2);
            var offsetX = backwards ? 11f : 23f;
            var offsetY = 0f;
            // Native GetDrawPos followed by Zombie::DrawShadow, not the reanimation pivot.
            switch (zombie.Kind)
            {
                case ZombieKind.Football:
                    offsetX = backwards ? -11f * scale : 20f + 21f * scale;
                    break;
                case ZombieKind.Newspaper:
                    offsetX = backwards ? 5f : 29f;
                    offsetY = 4f;
                    break;
                case ZombieKind.PoleVaulter:
                    offsetX = -6f + (backwards ? -5f : 36f);
                    break;
                case ZombieKind.Bobsled:
                    offsetX = backwards ? 13f : 20f;
                    offsetY = 1f;
                    break;
                case ZombieKind.Imp:
                    offsetX = backwards ? 13f : 25f;
                    offsetY = 7f;
                    shadowScale *= .6f;
                    break;
                case ZombieKind.Digger:
                    offsetX = backwards ? 14f : 17f;
                    offsetY = 5f;
                    break;
                case ZombieKind.Snorkel:
                    offsetX = backwards ? -2f : 35f;
                    offsetY = 5f;
                    break;
                case ZombieKind.DolphinRider:
                    offsetX = backwards ? 15f : 19f;
                    offsetY = 11f;
                    break;
                case ZombieKind.Yeti:
                    offsetX = backwards ? 20f : 3f;
                    break;
                case ZombieKind.Gargantuar:
                    offsetX = 27f;
                    offsetY = -1f;
                    shadowScale *= 1.5f;
                    break;
                case ZombieKind.Zomboni:
                    offsetX += 68f;
                    offsetY = -23f;
                    break;
                case ZombieKind.Catapult:
                    offsetX -= 25f;
                    offsetY = -18f;
                    break;
                case ZombieKind.Bungee:
                    offsetX -= 30f;
                    break;
            }

            // PC 0x527D20 adds half the UNSCALED image dimensions before applying scale.
            // Its x/y arguments are not the shadow center; subtracting a scaled half-size
            // directly from those arguments incorrectly moves a normal shadow 43px left/18px up.
            var centerX = AdventureZombieNativeRules.NativePositionX(gridX, scale) + 20f * scale - 20f + offsetX + imageSize.x * .5f;
            var centerY = floorY - 30f + offsetY + (inPool ? 67f : 92f) + imageSize.y * .5f;
            var width = imageSize.x * shadowScale;
            var height = imageSize.y * shadowScale;
            return new Rect(centerX - width * .5f, centerY - height * .5f, width, height);
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
        private void DrawAdventureZombiePose(Zombie zombie, string name, string? clip, Vector2 origin, float size, float age, float rate, bool loop)
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
            foreach (var arm in _owner.RequiredGame.Presentation.ArmDrops)
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
                _owner.RequiredAnimations.DrawNativeZombieArmDrop(_adventureParticles, arm, AdventureZombieOrigin(pose, arm.X), AdventureRenderRemainder);
            }
        }

        /// <summary>Performs draw adventure detached zombie parts.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "behindBody">The behind body.</param>
        private void DrawAdventureDetachedZombieParts(int row, bool behindBody)
        {
            foreach (var part in _owner.RequiredGame.Presentation.ZombiePartDrops)
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
                _owner.RequiredAnimations.DrawNativeZombiePartDrop(_adventureParticles, part, AdventureZombieOrigin(pose, part.X), AdventureRenderRemainder);
            }
        }

        /// <summary>Draws native foot dust at its fixed board coordinates below the zombie body layer.</summary>
        /// <param name="row">The lawn row whose original foot-dust bursts are submitted.</param>
        private void DrawAdventureZombieFootsteps(int row)
        {
            foreach (var effect in _owner.RequiredGame.Presentation.FootstepEffects)
            {
                if (effect.Row != row)
                {
                    continue;
                }

                RequiredAdventureParticles.Draw(_presentationKeys.Get("native-footstep-", effect.Id), "Dust_Foot",
                    effect.Age + AdventureRenderRemainder, new Vector2(effect.X, effect.Y), effect.Seed);
            }
        }

        /// <summary>Draws the original planting emitters above board actors at native render order 400000.</summary>
        private void DrawAdventurePlantingEffects()
        {
            foreach (var effect in _owner.RequiredGame.Presentation.PlantingEffects)
            {
                RequiredAdventureParticles.Draw(_presentationKeys.Get("native-planting-", effect.Id), effect.Pool ? "PlantingPool" : "Planting",
                    effect.Age + AdventureRenderRemainder, new Vector2(effect.X, effect.Y), effect.Seed);
            }
        }

        /// <summary>Performs draw adventure plant bite effects.</summary>
        /// <param name = "row">The row.</param>
        private void DrawAdventurePlantBiteEffects(int row)
        {
            foreach (var effect in _owner.RequiredGame.Presentation.PlantBiteEffects)
            {
                if (effect.Row != row)
                {
                    continue;
                }

                RequiredAdventureParticles.Draw(_presentationKeys.Get("native-plant-bite-", effect.Id), effect.ParticleId == 35 ? "WallnutEatSmall" : effect.ParticleId == 36 ? "WallnutEatLarge" : "MindControl", effect.Age + AdventureRenderRemainder, new Vector2(effect.NativeX + effect.OffsetX, effect.NativeY + effect.OffsetY), effect.Seed);
            }
        }

        /// <summary>Performs draw adventure corpses.</summary>
        /// <param name = "row">The row.</param>
        private void DrawAdventureCorpses(int row)
        {
            foreach (AdventureZombieCorpse corpse in _owner.RequiredGame.Presentation.Corpses)
            {
                var zombie = corpse.RequiredZombie;
                if (zombie.Row != row || !_owner.RequiredGame.IsModeZombieVisible(zombie))
                {
                    continue;
                }

                if (corpse.Burn != null)
                {
                    DrawAdventureBurnedCorpse(corpse);
                    continue;
                }

                string? name = AdventureAnimationCatalog.AdventureZombieAnimation(zombie.Kind), clip = corpse.Animation;
                var origin = AdventureZombieOrigin(zombie, zombie.X);
                float rate = corpse.Rate, size = AdventureDancerRules.Scale(zombie);
                if (!_owner.AnimationCatalog.AdventureClipExists(name, clip))
                {
                    clip = "anim_death";
                }

                global::PlantsVsZombies.Reanimation.NativeAnimationInfo info;
                if (!_owner.RequiredAnimations.TryGetNativeAnimationInfo(name, clip, out info))
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
            var burn = corpse.RequiredBurn;
            var zombie = corpse.RequiredZombie;
            if (burn.Visual == AdventureBurnVisual.FrozenOriginal)
            {
                if (corpse.Age >= 3f)
                {
                    return;
                }

                var name = AdventureAnimationCatalog.AdventureZombieAnimation(zombie.Kind);
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
                        _owner.RequiredAnimations.DrawZombiePlayback(origin, burn.Scale, corpse.FrozenSeconds, variant, clip, true, corpse.FrozenRate);
                    }
                    else
                    {
                        _owner.RequiredAnimations.DrawNativePlayback(name, origin, burn.Scale, corpse.FrozenSeconds, clip, true, corpse.FrozenRate, visible, facingLeft: burn.FacingLeft, mirrorWidth: 60f);
                    }

                    if (plantHead && !zombie.Headless)
                    {
                        _owner.RequiredAnimations.DrawNativePlantHeadAttachment(origin, burn.Scale, clip, corpse.FrozenSeconds, corpse.FrozenRate, zombie.ModeHead, corpse.FrozenHeadSeconds, burn.FacingLeft);
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

            ReanimationDefinition? definition;
            if (!VerifiedDataCatalog.TryGetReanimation(burn.ReanimationId, out definition))
            {
                corpse.Duration = 0;
                return;
            }

            var asset = Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/'));
            global::PlantsVsZombies.Reanimation.NativeAnimationInfo info;
            var full = burn.Animation == null;
            var found = full ? _owner.RequiredAnimations.TryGetNativeDefinitionInfo(asset, out info) : _owner.RequiredAnimations.TryGetNativeAnimationInfo(asset, burn.Animation, out info);
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

            if (zombie == null)
            {
                throw new System.InvalidOperationException("The value 'zombie' is not available.");
            }

            var position = AdventureZombieSourceOrigin(zombie, zombie.X) + new Vector2(burn.X + burn.PivotX, burn.Y + burn.PivotY);
            var trackVisible = burn.HideImp ? (Func<string, bool>)(track => !string.Equals(track, "impblink", StringComparison.OrdinalIgnoreCase) && !string.Equals(track, "imphead", StringComparison.OrdinalIgnoreCase)) : null;
            if (full)
            {
                _owner.RequiredAnimations.DrawNativeDefinition(asset, position, burn.Scale, corpse.Age + AdventureRenderRemainder, false, rate, trackVisible, burn.FacingLeft, 60f);
            }
            else
            {
                _owner.RequiredAnimations.DrawNativePlayback(asset, position, burn.Scale, corpse.Age + AdventureRenderRemainder, burn.Animation, false, rate, trackVisible, facingLeft: burn.FacingLeft, mirrorWidth: 60f);
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
                center = new Vector2(BoardLayout.AdventureGridX + sun.X * BoardLayout.CellWidth, BoardLayout.AdventureGridY + sun.Y * _owner.Layout.AdventurePitch);
            }

            var previous = CanvasUI.Color;
            CanvasUI.Color = new Color(previous.r, previous.g, previous.b, previous.a * alpha);
            // PC 0x43B610 creates the original Sun reanimation at the 60x60 pickup centre, rate 6.
            // The authored glow/rays extend beyond that logical pickup rectangle; never fit them into it.
            _owner.RequiredAnimations.DrawNativePlayback("Sun", center, scale, sun.Age + AdventureRenderRemainder, null, true, 6f);
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
            var y = BoardLayout.AdventureGridY + row * _owner.Layout.AdventurePitch + _owner.Layout.AdventureSlope(x) + 30f;
            var age = pea.PresentationAge + AdventureRenderRemainder;
            var shortPuff = AdventurePuffShroomRules.IsShortRange(pea.SourceKind);
            if (shortPuff)
            {
                y = pea.PresentationLaunchY;
            }

            float rotation = 0;
            if (pea.Lobbed)
            {
                y = pea.PresentationLaunchY + Mathf.Lerp(pea.PresentationPreviousLobHeight, pea.PresentationLobHeight, AdventureRenderRemainder * 100f);
                rotation = (pea.PresentationRotation + pea.PresentationSpin * pea.PresentationNativeAge * 100f) * Mathf.Rad2Deg;
                _owner.RequiredAnimations.DrawResourceSprite("IMAGE_PEA_SHADOWS", new Vector2(BoardLayout.AdventureGridX + x * BoardLayout.CellWidth, _owner.Layout.AdventureCellY(pea.Row, x) + 80), 1.6f * 200f / (Mathf.Clamp(-pea.PresentationLobHeight, 0, 200) + 200), 0, _owner.RequiredGame.Adventure.Level.IsNight ? 1 : 0);
            }

            var center = new Vector2(BoardLayout.AdventureGridX + x * BoardLayout.CellWidth, y);
            if (pea.Fire)
            {
                _owner.RequiredAnimations.DrawNativePlayback("FirePea", center, 1f, age, null, true);
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
                rotation = Mathf.Atan2(pea.VelocityY * _owner.Layout.AdventurePitch, pea.VelocityX * BoardLayout.CellWidth) * Mathf.Rad2Deg;
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

            var scale = shortPuff ? AdventurePuffShroomRules.ImageScale(pea.PresentationNativeAge + AdventureRenderRemainder) : 1f;
            _owner.RequiredAnimations.DrawResourceSprite(image, center, scale, rotation);
        }

        /// <summary>Performs draw adventure impacts.</summary>
        /// <param name = "row">The row.</param>
        private void DrawAdventureImpacts(int row)
        {
            foreach (AdventureImpact impact in _owner.RequiredGame.Presentation.Impacts)
            {
                if ((int)Math.Round(impact.Row) != row)
                {
                    continue;
                }

                var point = new Vector2(BoardLayout.AdventureGridX + impact.X * BoardLayout.CellWidth, BoardLayout.AdventureGridY + impact.Row * _owner.Layout.AdventurePitch + _owner.Layout.AdventureSlope(impact.X) + 42);
                if (impact.NativeIcePoint)
                {
                    float nativeX, nativeY;
                    _owner.RequiredGame.AdventureIceImpactPoint(impact, AdventureRenderRemainder * 100f, out nativeX, out nativeY);
                    point = new Vector2(nativeX, nativeY);
                }

                var key = _presentationKeys.Get("impact-", impact.Id);
                if (impact.Mowed)
                {
                    RequiredAdventureParticles.Draw(key + "-head", "MoweredZombieHead", impact.Age, point, impact.Seed);
                    RequiredAdventureParticles.Draw(key + "-arm", "MoweredZombieArm", impact.Age, point, impact.Seed + 1);
                    continue;
                }

                if (impact.Cob)
                {
                    RequiredAdventureParticles.Draw(key + "-blast", "BlastMark", impact.Age, point, impact.Seed);
                    RequiredAdventureParticles.Draw(key + "-popcorn", "PopcornSplash", impact.Age, point, impact.Seed + 1);
                    continue;
                }

                if (impact.Fire)
                {
                    global::PlantsVsZombies.Reanimation.NativeAnimationInfo fire;
                    if (_owner.RequiredAnimations.TryGetNativeAnimationInfo("fire", "anim_flame", out fire) && impact.Age < fire.Count / 24f * .75f)
                    {
                        _owner.RequiredAnimations.DrawNativePlayback("fire", point + new Vector2(38, -20), .7f, impact.Age + fire.Count / 24f * .25f, "anim_flame", false, 24f, verticalScale: .4f);
                    }

                    continue;
                }

                var effect = impact.Butter ? "ButterSplat" : impact.Kind == PlantKind.MelonPult ? "melonimpact" : impact.Kind == PlantKind.WinterMelon ? "WinterMelonImpact" : impact.Kind == PlantKind.CabbagePult ? "CabbageSplat" : impact.Kind == PlantKind.Starfruit ? "StarSplat" : impact.Kind == PlantKind.PuffShroom || impact.Kind == PlantKind.ScaredyShroom || impact.Kind == PlantKind.SeaShroom ? "PuffSplat" : impact.Kind == PlantKind.Cactus || impact.Kind == PlantKind.Cattail || impact.Kind == PlantKind.KernelPult || impact.Kind == PlantKind.FumeShroom ? null : impact.Snow ? "SnowPeaSplat" : "PeaSplat";
                if (effect != null)
                {
                    RequiredAdventureParticles.Draw(key, effect, impact.Age, point, impact.Seed);
                }
            }
        }

        /// <summary>Stores award particles.</summary>
        private OriginalParticleRenderer? _awardParticles;

        /// <summary>Gets the award particles, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private OriginalParticleRenderer RequiredAwardParticles => _awardParticles ?? throw new System.InvalidOperationException("The member '_awardParticles' is not available.");
        /// <summary>Stores award presented game.</summary>
        private GameSimulation? _awardPresentedGame;

        /// <summary>Performs draw adventure reward.</summary>
        private void DrawAdventureReward()
        {
            var reward = _owner.RequiredGame.Adventure.RewardPresentation;
            if (reward == null)
            {
                return;
            }

            if (!ReferenceEquals(_awardPresentedGame, _owner._game))
            {
                _awardPresentedGame = _owner._game;
                _awardParticles = new OriginalParticleRenderer(_owner._root, _owner._animations);
            }

            RequiredAwardParticles.BeginFrame();
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
                    RequiredAwardParticles.Draw("award-rays", "Award", (reward.CollectionTicks + fraction) * .01f, center, 571);
                }

                if (reward.OnGround)
                {
                    RequiredAwardParticles.Draw("award-pickup-arrow", reward.NativeType == 7 ? "SeedPacket" : "AwardPickupArrow", reward.GroundTicks * .01f, new Vector2(x + reward.Width / 2, y + reward.Height / 2 - 60), 570);
                }

                if (reward.NativeType == 7 && _owner.RequiredGame.Adventure.Level.RewardSeed.HasValue)
                {
                    DrawAdventureCard(_owner.RequiredGame.Adventure.Level.RequiredRewardSeed, new Rect(center.x - 25 * size, center.y - 35 * size, 50 * size, 70 * size), true);
                }
                else
                {
                    DrawAdventureAwardObject(reward, center, size);
                }

                // Starburst is native render order coin+1; the attached Award rays are coin-1.
                if (reward.Stage == AdventureAwardStage.Collecting)
                {
                    RequiredAwardParticles.Draw("award-starburst", "Starburst", (reward.CollectionTicks + fraction) * .01f, new Vector2(reward.CollectionStartX + 30, reward.CollectionStartY + 30), 572);
                }

                if (reward.FadeAlpha > 0)
                {
                    Fill(new Rect(0, 0, 800, 600), new Color(reward.NativeType == 15 ? 0 : 1, reward.NativeType == 15 ? 0 : 1, reward.NativeType == 15 ? 0 : 1, reward.FadeAlpha));
                }
            }

            RequiredAwardParticles.EndFrame();
            if (!string.IsNullOrEmpty(_owner.AdventureBoard._adventureFeedback) && Time.unscaledTime < _owner.AdventureBoard._adventureFeedbackUntil)
            {
                Label(new Rect(140, 110, 520, 70), _owner.AdventureBoard._adventureFeedback, 17, Color.yellow, TextAnchor.MiddleCenter, true);
            }
        }

        /// <summary>Performs draw adventure award button.</summary>
        /// <param name = "rect">The rect.</param>
        /// <param name = "enabled">The enabled.</param>
        private void DrawAdventureAwardButton(Rect rect, bool enabled)
        {
            DrawImage(enabled ? "SeedChooser_Button.png" : "SeedChooser_Button_Disabled.png", rect);
            if (enabled && rect.Contains(_owner.AdventureBoard._adventurePointer))
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
            var source = _owner.Textures.Load(image);
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
            var level = _owner.RequiredGame.Adventure.Level;
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
            var background = _owner.Textures.Load("AwardScreen_Back_new.jpg");
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
                name = _owner.AdventureBoard.AdventurePlantName(kind);
                description = OriginalProgressionText(VerifiedDataCatalog.Plants[(int)kind].InternalName + "_TOOLTIP");
                // Original PC DrawAwardSeed 0x40FE70: 2x packet at (190,360).
                DrawAdventureCard(kind, new Rect(190, 360, 100, 140), true);
            }
            else
            {
                AdventureAwardText(award.NativeType, out title, out name, out description);
                var icon = _owner.Textures.Load(AdventureAwardImage(award.NativeType, true));
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
            var next = AdventureRewardController.s_awardSeedNext;
            DrawAdventureAwardButton(next, award.IntroductionTicks >= 180);
            var caption = OriginalProgressionText("NEXT_LEVEL_BUTTON");
            if (!OriginalUiWidgets.TextAtBaseline(caption, next.center.x, next.y + 27, next.width, "DwarvenTodcraft15", gold))
            {
                Label(next, caption, 18, gold, TextAnchor.MiddleCenter, true);
            }

            DrawImage("Almanac_CloseButton.png", AdventureRewardController.s_awardMenu);
            var menuText = OriginalProgressionText("AWARD_MAIN_MENU_BUTTON");
            if (!OriginalUiWidgets.TextAtBaseline(menuText, AdventureRewardController.s_awardMenu.center.x, AdventureRewardController.s_awardMenu.y + 17, AdventureRewardController.s_awardMenu.width, "BrianneTod12", new Color(42f / 255, 42f / 255, 90f / 255)))
            {
                Label(AdventureRewardController.s_awardMenu, menuText, 12, new Color(42f / 255, 42f / 255, 90f / 255), TextAnchor.MiddleCenter, true);
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
            var note = award.NativeType == 18 ? 4 : _owner.RequiredGame.Adventure.Level.Number / 10;
            DrawImage(note % 2 == 0 ? "background2.jpg" : "background1.jpg", new Rect(-700, -300, 2800, 1200));
            var paper = _owner.Textures.Load("ZombieNote.jpg");
            if (paper != null)
            {
                CanvasUI.DrawTexture(new Rect(80, 80, paper.width, paper.height), paper);
            }

            var writing = note < 4 ? "ZombieNote" + (note + 1) + ".png" : "Credits_ZombieNote.png";
            // resources.xml supplies a black color image plus the handwriting alpha grid.
            var inkColor = note < 4 ? "ZombieNoteBlack" + (note + 1) + ".png" : "Credits_ZombieNoteBlack.png";
            var ink = _owner.Textures.LoadAlphaGrid(inkColor, writing);
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
        private OriginalParticleRenderer? _seedBankParticles;

        /// <summary>Gets the seed bank particles, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private OriginalParticleRenderer RequiredSeedBankParticles => _seedBankParticles ?? throw new System.InvalidOperationException("The member '_seedBankParticles' is not available.");
        /// <summary>Stores seed bank presented game.</summary>
        private GameSimulation? _seedBankPresentedGame;
        /// <summary>Performs draw adventure packet.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "card">The card.</param>
        private void DrawAdventurePacket(PlantKind kind, Rect card)
        {
            var context = _owner.RequiredGame.AdventurePacketContext(kind, destination: _packetContext);
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
                    _owner.RequiredAnimations.DrawNativeClipped(_presentationKeys.Get("seed-cooldown-", (int)kind), card, _drawPacketMask,
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
                if (_owner.AdventureBoard._selectedAdventurePacket == packet.Id)
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
            _seedCostFont ??= CanvasBitmapFont.LoadFromResources("Pico129");
            var text = kind == PlantKind.Imitater && !_owner.RequiredGame.Adventure.ImitaterTarget.HasValue
                ? "?" : NumberLabel(_owner.RequiredGame.AdventureSeedCost(kind));
            var previous = CanvasUI.Matrix;
            try
            {
                CanvasUI.Matrix = previous * Matrix4x4.TRS(new Vector3(card.x, card.y, 0f), Quaternion.identity,
                    new Vector3(card.width / 50f, card.height / 70f, 1f));
                // PC 0x49D260 calls ImageFont::GetAscent (vtable +4), not GetHeight, at baseline 54 + ascent.
                DrawBitmap(_seedCostFont, text, 32f, 54f + _seedCostFont.Data.Base, float.PositiveInfinity, Color.black, false);
            }
            finally
            {
                CanvasUI.Matrix = previous;
            }
        }

        /// <summary>Performs draw adventure packet ready flashes.</summary>
        private void DrawAdventurePacketReadyFlashes()
        {
            if (!ReferenceEquals(_seedBankPresentedGame, _owner._game))
            {
                _seedBankPresentedGame = _owner._game;
                _seedBankParticles = new OriginalParticleRenderer(_owner._root, _owner._animations);
            }

            RequiredSeedBankParticles.BeginFrame();
            foreach (var flash in _owner.RequiredGame.Adventure.SeedFlashes)
            {
                for (var i = 0; i < _owner._deck.Count; i++)
                {
                    if (_owner._deck[i] == flash.Kind)
                    {
                        var card = _owner.IndependentModes.IsModeBoard ? _owner.IndependentModes.ModeSeedRect(i) : _owner.Layout.AdventureSeedRect(i);
                        RequiredSeedBankParticles.Draw(_presentationKeys.Get("packet-ready-", flash.Id), AdventureSeedPacketFlash.Asset, flash.Age + AdventureRenderRemainder, new Vector2(card.x, card.y), flash.Id);
                        break;
                    }
                }
            }

            RequiredSeedBankParticles.EndFrame();
        }

        /// <summary>Performs adventure first wave tutorial advice.</summary>
        /// <returns>The resulting adventure first wave tutorial advice.</returns>
        private string? AdventureFirstWaveTutorialAdvice()
        {
            var board = _owner.RequiredGame.Adventure;
            if (board.Level.IsReplay || board.CurrentWave != 0 || board.Phase != AdventurePhase.Playing)
            {
                return null;
            }

            int peas = 0, flowers = 0;
            foreach (Plant plant in _owner.RequiredGame.Plants)
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

                if (_owner.RequiredGame.AdventureSunAvailable >= 100 && _owner.RequiredGame.CooldownRemaining(PlantKind.Peashooter) <= 0)
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
        private AdventureWaveTextAnimation? _hugeWaveTextAnimation;
        /// <summary>Stores huge wave text.</summary>
        private string? _hugeWaveText;
        /// <summary>Stores huge wave glyphs.</summary>
        private readonly List<BitmapTextLayout> _hugeWaveGlyphs = new List<BitmapTextLayout>();
        /// <summary>Stores huge wave text width.</summary>
        private float _hugeWaveTextWidth;
        /// <summary>Stores bundled wave root.</summary>
        private string? _bundledWaveRoot;

        /// <summary>Performs draw adventure wave notices.</summary>
        private void DrawAdventureWaveNotices()
        {
            if (_owner._game == null || !_owner._game.IsAdventure || _owner._choosingSeeds || _owner.Opening.AdventureIntroActive)
            {
                return;
            }

            if (_owner.WaveNotices._waveNoticeGame != _owner._game)
            {
                _owner.WaveNotices.InitializeAdventureWaveNotices();
            }

            if (_owner.WaveNotices._hugeWaveNoticeStart >= 0f)
            {
                var age = Math.Max(0, (int)Math.Round((_owner._game.Elapsed - _owner.WaveNotices._hugeWaveNoticeStart) * 100d));
                if (age < AdventureWaveNoticeRules.HugeDuration)
                {
                    DrawOriginalHugeWaveText(age);
                }
            }

            if (_owner.WaveNotices._finalWaveNoticeStart >= 0f)
            {
                var age = Math.Max(0, _owner._game.Elapsed - _owner.WaveNotices._finalWaveNoticeStart);
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
        private string? EnsureOriginalWaveFiles()
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
        private global::PlantsVsZombies.Reanimation.ReanimationRenderer? OriginalFinalWaveRenderer()
        {
            if (File.Exists(Path.Combine(_owner._root, "reanim", "FinalWave.reanim")) && File.Exists(Path.Combine(_owner._root, "reanim", "FinalWave.png")))
            {
                return _owner._animations;
            }

            if (_owner.WaveNotices._bundledWaveAnimations == null)
            {
                var bundled = EnsureOriginalWaveFiles();
                if (bundled == null)
                {
                    return null;
                }

                _owner.WaveNotices._bundledWaveAnimations = new global::PlantsVsZombies.Reanimation.ReanimationRenderer(bundled);
            }

            return _owner.WaveNotices._bundledWaveAnimations;
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
            if (_owner.Campaign.UseLegacyBoard || _owner._profiles == null || !_owner._profiles.SelectedId.HasValue || !_owner.Campaign.EnsureAdventureStore())
            {
                return;
            }

            var progress = _owner.Campaign.RequiredAdventureStore.Get(_owner._profiles.SelectedId.Value);
            var text = progress.Completed ? "冒险模式已通关 · 可开始下一周目" : "冒险第 " + progress.Cycle + " 周目  " + AdventureLevels.Get(progress.NextLevel).Label;
            Label(new Rect(500, 151, 195, 25), text, 17, new Color(1f, 0.94f, 0.54f), TextAnchor.MiddleCenter, true);
        }

        /// <summary>Performs draw adventure card.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "card">The card.</param>
        /// <param name = "price">The price.</param>
        private void DrawAdventureCard(PlantKind kind, Rect card, bool price)
        {
            var bank = _owner.Textures.Load("seeds.png");
            if (card.width > 50.01f && _owner.Textures.Load("SeedPacket_Larger.png") != null)
            {
                DrawImage("SeedPacket_Larger.png", card);
            }
            else if (bank != null)
            {
                _owner.RequiredAnimations.DrawNativeImage(bank, card, new Rect(2f / 9f, 0, 1f / 9f, 1));
            }

            var id = (int)kind;
            var imitated = kind == PlantKind.Imitater && _owner.RequiredGame.Adventure.ImitaterTarget.HasValue;
            if (imitated)
            {
                id = (int)_owner.RequiredGame.Adventure.RequiredImitaterTarget;
            }

            if (id >= 0 && id < 49)
            {
                var cell = AlmanacPortraitLayout.PlantAtlasCell(id);
                var atlas = cell >= 0 ? _owner.Textures.Load("packet_plants.png") : null;
                if (atlas != null)
                {
                    var source = new Rect(cell / 13f, 0, 1f / 13f, 1);
                    if (imitated)
                    {
                        _cardAtlas = atlas;
                        _cardDestination = card;
                        _cardSource = source;
                        _owner.RequiredAnimations.DrawNativeClipped(_presentationKeys.Get("imitater-card-", (int)card.x, (int)card.y), card, _drawCardImage, false, true);
                    }
                    else
                    {
                        _owner.RequiredAnimations.DrawNativeImage(atlas, card, source);
                    }
                }
                else
                {
                    Vector2 origin;
                    float scale;
                    AlmanacPortraitLayout.PlantThumbnailTransform(id, new Rect(0, 0, 50, 70), out origin, out scale);
                    origin = new Vector2(card.x + origin.x * card.width / 50f, card.y + origin.y * card.height / 70f);
                    scale *= card.width / 50f;
                    var iconKind = imitated ? _owner.RequiredGame.Adventure.RequiredImitaterTarget : kind;
                    if (imitated)
                    {
                        ActorRenderer.DrawCursor(AdventurePlantAnimation(iconKind), origin, scale, null, false, true,
                            _presentationKeys.Get("imitater-card-", (int)card.x, (int)card.y), card);
                    }
                    else
                    {
                        _owner.RequiredAnimations.DrawNative(AdventurePlantAnimation(iconKind), origin, scale, 0f);
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
                _owner.RequiredAnimations.DrawNative("Wallnut", origin, scale * card.width / 50f, 0f, "anim_idle");
                CanvasUI.Color = previous;
            }

            if (price)
            {
                DrawAdventurePacketCost(kind, card);
            }
        }

        /// <summary>Submits the immediately configured packet artwork for grayscale composition.</summary>
        /// <returns>Whether packet artwork was submitted.</returns>
        private bool DrawCardImage()
        {
            return _owner.RequiredAnimations.DrawNativeImage(_cardAtlas, _cardDestination, _cardSource);
        }

        /// <summary>Draws the complete PC seed bank below lawn actors during play.</summary>
        private void DrawAdventureSeedBankContents()
        {
            var board = _owner.RequiredGame.Adventure;
            var packets = board.Level.HasConveyor || board.Level.Mode == AdventureMode.Vasebreaker;
            if (_owner.RequiredGame.UsesNativeAdventureConveyor)
            {
                DrawAdventureConveyorBackground();
            }
            else if (packets)
            {
                DrawImage("ConveyorBelt_backdrop.png", new Rect(0, 0, 516, 87));
            }
            else
            {
                DrawAdventureSeedBankBackground(_owner.Layout.AdventureSeedBankOrigin, _owner.Layout.SeedBankCapacity);
            }

            if (packets)
            {
                DrawAdventureConveyorPackets();
            }
            else
            {
                DrawAdventureSunAmount(_owner.Layout.AdventureSeedBankOrigin);
                for (var i = 0; i < _owner._deck.Count; i++)
                {
                    var card = _owner.Layout.AdventureSeedRect(i);
                    DrawAdventurePacket(_owner._deck[i], card);
                }
            }

        }

        /// <summary>Draws the original PC flag meter and right-aligned adventure level name.</summary>
        private void DrawAdventureProgressAndName()
        {
            var board = _owner.RequiredGame.Adventure;
            var progress = _owner.RequiredGame.Presentation.ProgressMeter;
            var visible = (progress.Visible || board.Level.Mode == AdventureMode.Boss) && board.Level.Mode != AdventureMode.Vasebreaker;
            var meter = visible ? _owner.Textures.Load("FlagMeter.png") : null;
            if (meter != null)
            {
                // PC DrawProgressMeter 0x4229F0: (600,575), 143px fill, 135px head travel.
                CanvasUI.DrawTextureWithTexCoords(new Rect(600, 575, 158, 27), meter, new Rect(0, .5f, 1, .5f));
                var fill = progress.FillWidth;
                if (fill > 0)
                {
                    var sourceX = 151 - fill;
                    CanvasUI.DrawTextureWithTexCoords(new Rect(600 + sourceX, 575, fill, 27), meter,
                        new Rect(sourceX / 158f, 0, fill / 158f, .5f));
                }

                var parts = _owner.Textures.Load("FlagMeterParts.png");
                if (parts != null)
                {
                    if (AdventureProgressMeterState.HasFlags(board.Level.Number, board.Level.IsReplay))
                    {
                        var perFlag = board.Level.IsReplay ? 10 : Math.Min(10, Math.Max(1, board.Waves.Count));
                        for (var wave = perFlag; wave <= board.Waves.Count; wave += perFlag)
                        {
                            var x = AdventureProgressMeterState.FlagX(wave, board.Waves.Count);
                            CanvasUI.DrawTextureWithTexCoords(new Rect(x, 571, 25, 25), parts, new Rect(1f / 3f, 0, 1f / 3f, 1));
                            CanvasUI.DrawTextureWithTexCoords(new Rect(x, 572 - progress.FlagLift(wave), 25, 25), parts, new Rect(2f / 3f, 0, 1f / 3f, 1));
                        }
                    }
                }

                DrawImage("FlagMeterLevelProgress.png", new Rect(638, 589, 86, 11));
                // PC Board::DrawProgressMeter (0x4229F0) draws the moving head over the level-progress lettering.
                if (parts != null && board.Level.Mode != AdventureMode.Boss)
                {
                    CanvasUI.DrawTextureWithTexCoords(new Rect(progress.HeadX, 572, 25, 25), parts, new Rect(0, 0, 1f / 3f, 1));
                }
            }

            if (!ReferenceEquals(_captionBoard, board))
            {
                _captionBoard = board;
                _waveCaption = OriginalProgressionText("LEVEL") + " " + board.Level.Label;
            }

            _levelNameFont ??= CanvasBitmapFont.LoadFromResources("HouseofTerror16");
            var right = visible ? 593 : 780;
            var gold = new Color(224f / 255f, 187f / 255f, 98f / 255f);
            if (!DrawBitmap(_levelNameFont, _waveCaption, right, 595, 580, gold, false))
            {
                Label(new Rect(right - 580, 575, 580, 25), _waveCaption, 16, gold, TextAnchor.MiddleRight, true);
            }
        }

        /// <summary>Performs draw adventure interface.</summary>
        private void DrawAdventureInterface()
        {
            var board = _owner.RequiredGame.Adventure;
            if (DrawAdventureOpeningInterface())
            {
                return;
            }

            var packets = board.Level.HasConveyor || board.Level.Mode == AdventureMode.Vasebreaker;
            DrawOriginalUiButton(AdventureBoardController.s_adventureMenuButton, "菜单");
            DrawAdventureProgressAndName();

            if (board.MoneyCollectedUnits > 0 || _owner.RequiredGame.Elapsed < _walletVisibleUntil)
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
                DrawImage("CobCannon_target.png", new Rect(_owner.AdventureBoard._adventurePointer.x - 36, _owner.AdventureBoard._adventurePointer.y - 36, 72, 72));
            }

            var advice = AdventureFirstWaveTutorialAdvice() ?? _owner.AdventureBoard.AdventureTutorialText(board.Tutorial);
            if (_owner.AdventureBoard._adventureFeedback != null && Time.unscaledTime < _owner.AdventureBoard._adventureFeedbackUntil)
            {
                advice = _owner.AdventureBoard._adventureFeedback;
            }

            if (!string.IsNullOrEmpty(advice) && board.Phase == AdventurePhase.Playing)
            {
                DrawOriginalBoardAdvice(advice, board.Level.Number <= 2);
            }

            DrawAdventureWaveNotices();
            DrawCollectedSunsAboveSeedBank();
            if (_owner._choosingSeeds)
            {
                DrawAdventureChooser();
            }
            else if (board.Phase == AdventurePhase.Reward || board.Phase == AdventurePhase.Finished)
            {
                DrawAdventureReward();
            }
            else if (_owner.RequiredGame.State == MatchState.Lost)
            {
                DrawDialog("僵尸吃掉了你的脑子！", "重新开始", false);
            }
            else if (_owner.RequiredGame.State == MatchState.Paused && !_owner.RequiredOptionsView.IsOpen)
            {
                DrawDialog("已暂停", "继续游戏", true);
            }
        }

        /// <summary>Performs draw adventure chooser.</summary>
        private void DrawAdventureChooser()
        {
            if (_owner.Opening._adventureIntro == null)
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

            var seeds = _owner.AdventureBoard._choosingImitater ? _owner.RequiredGame.Adventure.Level.UnlockedSeeds : _owner.RequiredGame.Adventure.AvailableSeeds;
            if (!_owner.AdventureBoard._choosingImitater)
            {
                for (var index = 0; index < 40; index++)
                {
                    var available = false;
                    for (var candidate = 0; candidate < seeds.Count; candidate++)
                    {
                        if ((int)seeds[candidate] == index)
                        {
                            available = true;
                            break;
                        }
                    }

                    if (!available)
                    {
                        DrawImage("SeedPacketSilhouette.png", _owner.Opening.AdventureCampaignChooserRect((PlantKind)index));
                    }
                }
            }

            for (var seedIndex = 0; seedIndex < seeds.Count; seedIndex++)
            {
                var seed = seeds[seedIndex];
                var rect = _owner.Opening.AdventureCampaignChooserRect(seed);
                var previous = CanvasUI.Color;
                if (!_owner.AdventureBoard._choosingImitater && (_owner.RequiredPendingSeeds.Contains(seed) || _owner.RequiredPendingSeeds.IsMoving(seed)))
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

            var required = Math.Min(_owner.RequiredGame.Adventure.Level.PacketSlots, _owner.RequiredGame.Adventure.AvailableSeeds.Count);
            var enabled = _owner.AdventureBoard._choosingImitater || _owner.RequiredPendingSeeds.Count == required && !_owner.RequiredPendingSeeds.HasFlyingSeeds;
            DrawImage(enabled ? "SeedChooser_Button.png" : "SeedChooser_Button_Disabled.png", _owner.Opening.AdventureChooserStartRect);
            var caption = _owner.AdventureBoard._choosingImitater ? OriginalProgressionText("CANCEL") : OriginalProgressionText("LETS_ROCK_BUTTON");
            if (!DrawBitmap(_owner._optionsBitmap, caption, _owner.Opening.AdventureChooserStartRect.center.x, 572, 150, nativeGold, true))
            {
                Label(_owner.Opening.AdventureChooserStartRect, caption, 18, nativeGold, TextAnchor.MiddleCenter, true);
            }
        }

        /// <summary>Performs draw legacy adventure chooser.</summary>
        private void DrawLegacyAdventureChooser()
        {
            DrawImage("SeedChooser_Background.png", new Rect(85, 70, 630, 505));
            Label(new Rect(140, 78, 520, 37), "关卡 " + _owner.RequiredGame.Adventure.Level.Label + "  选择你的植物", 25, Color.white, TextAnchor.MiddleCenter, true);
            var unlocked = _owner.RequiredGame.Adventure.AvailableSeeds;
            for (var i = 0; i < unlocked.Count; i++)
            {
                var card = BoardLayout.AdventureChooserRect(i);
                DrawChooserHome(unlocked[i], card);
            }

            var required = Math.Min(_owner.RequiredGame.Adventure.Level.PacketSlots, unlocked.Count);
            DrawOriginalUiButton(new Rect(300, 533, 200, 34), "开始战斗  " + _owner.RequiredPendingSeeds.Count + "/" + required, _owner.RequiredPendingSeeds.Count == required);
            DrawChooserBank(_owner.Layout.SeedBankCapacity);
            DrawChooserMovingPackets(unlocked, _owner.Layout.SeedBankCapacity);
            if (_owner.AdventureBoard._choosingImitater)
            {
                DrawImage("SeedChooser_Background.png", new Rect(85, 70, 630, 505));
                Label(new Rect(140, 78, 520, 37), "模仿哪一种植物？", 25, Color.white, TextAnchor.MiddleCenter, true);
                var targets = _owner.RequiredGame.Adventure.Level.UnlockedSeeds;
                for (var i = 0; i < targets.Count; i++)
                {
                    DrawAdventureCard(targets[i], BoardLayout.AdventureChooserRect(i), true);
                }

                DrawOriginalUiButton(new Rect(300, 533, 200, 34), "取消");
            }
        }

        /// <summary>Performs draw original loading.</summary>
        /// <returns>The resulting draw original loading.</returns>
        private bool DrawOriginalLoading()
        {
            if (!_owner.Loading._originalLoadingActive)
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
            _owner.Loading._originalLoadingFirstDraw = true;

            if (_owner.Loading._originalTitleTicks <= 400)
            {
                var alpha = _owner.Loading._originalTitleTicks < 50 ? _owner.Loading._originalTitleTicks / 50f : _owner.Loading._originalTitleTicks > 350 ? (400 - _owner.Loading._originalTitleTicks) / 50f : 1f;
                CanvasUI.Color = new Color(1, 1, 1, Mathf.Clamp01(alpha));
                DrawOriginalTitleTexture("popcap", new Rect(250, 150, 300, 300));
            }
            else
            {
                DrawOriginalTitleTexture("title", new Rect(0, 0, 800, 600));
                var ticks = Math.Max(0, _owner.Loading._originalTitleScreenTicks);
                var logoY = ticks > 60 ? OriginalLoadingController.OriginalTitleCurve(100, 60, ticks, -150, 10, false) : OriginalLoadingController.OriginalTitleCurve(60, 50, ticks, 10, 15, true);
                DrawOriginalTitleTexture("logo", new Rect(50, logoY, 700, 116));
                var buttonY = ticks > 10 ? OriginalLoadingController.OriginalTitleCurve(60, 10, ticks, 650, 534, false) : OriginalLoadingController.OriginalTitleCurve(10, 0, ticks, 534, 529, true);
                var grassY = buttonY - 17f;
                DrawOriginalTitleTexture("dirt", new Rect(244, grassY + 18, 321, 53));
                // PC 4A3777 truncates width before ClipRect. Crop UVs without stretching
                // or changing the IMGUI clip origin, preserving the native 314x33 pixels.
                DrawOriginalLoadingGrass(240f, grassY, _owner.Loading._originalLoadWidth);
                if (_owner.Loading._originalLoadWidth < 314f)
                {
                    var travel = _owner.Loading._originalLoadWidth * .94f;
                    var capScale = Mathf.Lerp(1f, .5f, _owner.Loading._originalLoadWidth / 314f);
                    float x = 251f + travel, y = grassY - 3f - 35f * capScale + 35f;
                    Texture2D cap;
                    if (_owner.Loading._originalTitleTextures.TryGetValue("sodroll", out cap) && cap != null)
                    {
                        var beforeCap = CanvasUI.Matrix;
                        // Literal PC TodScaleRotateTransformMatrix, with its native image origin.
                        CanvasUI.Matrix = beforeCap * Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.Euler(0, 0, -travel * 2f), new Vector3(capScale, capScale, 1));
                        CanvasUI.DrawTexture(new Rect(-cap.width * .5f, -cap.height * .5f, cap.width, cap.height), cap, ScaleMode.StretchToFill, true);
                        CanvasUI.Matrix = beforeCap;
                    }
                }

                for (var i = 0; i < _owner.Loading._originalLoadSproutAges.Length; i++)
                {
                    DrawOriginalLoadSprout(i);
                }

                var ready = _owner.Loading.OriginalLoadingReady; // PC enables the start label on real completion, independently of bar smoothing.
                var button = new Rect(240, buttonY, 314, 50);
                var pointer = CanvasUI.PointerPosition;
                Color label = ready && button.Contains(pointer) ? new Color32(250, 90, 15, 255) : new Color32(218, 184, 33, 255);
                var text = ready ? "点击开始！" : "读取中…";
                var baseline = buttonY + (50f + (_owner.Loading._originalLoadingFont == null ? 15f : _owner.Loading._originalLoadingFont.Data.Base)) * 0.5f - 1f;
                if (!DrawBitmap(_owner.Loading._originalLoadingFont, text, 397, baseline, 314, label, true))
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
            if (_owner.Loading._originalLoadSproutAges[index] < 0f)
            {
                return;
            }

            var head = index == 4;
            var info = head ? _owner.Loading._originalHeadInfo : _owner.Loading._originalSproutInfo;
            if (info.FrameCount < 1 || info.FramesPerSecond <= 0f)
            {
                return;
            }

            var x = 225f + _owner.Loading._originalLoadTriggers[index] * 314f - (head ? 20f : 0f);
            var y = index == 2 ? 506f : 511f;
            var sx = index == 1 || index == 3 ? -1f : index == 2 ? 1.1f : 1f;
            var sy = index == 2 ? 1.3f : 1f;
            var seconds = _owner.Loading._originalLoadSproutAges[index];
            var before = CanvasUI.Matrix;
            CanvasUI.Matrix = before * Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.identity, new Vector3(sx, sy, 1));
            // PC creates the full 18-frame definition, rate 18, PlayOnceAndHold.
            // Native playback owns frame normalization; do not convert/truncate it a second time.
            _owner.RequiredAnimations.DrawNativeDefinition(head ? "LoadBar_Zombiehead" : "LoadBar_sprout", Vector2.zero, 1f, seconds, false, 18f);
            CanvasUI.Matrix = before;
        }

        /// <summary>Performs draw original loading grass.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "width">The width.</param>
        private void DrawOriginalLoadingGrass(float x, float y, float width)
        {
            Texture2D grass;
            if (!_owner.Loading._originalTitleTextures.TryGetValue("grass", out grass) || grass == null)
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
            if (_owner.Loading._originalTitleTextures.TryGetValue(key, out image) && image != null)
            {
                CanvasUI.DrawTexture(rect, image, ScaleMode.StretchToFill, true);
            }
        }

        /// <summary>Draws independent mode seed controls at the same BottomUi position as the PC seed bank.</summary>
        private void DrawModeSeedBank()
        {
            var s = _owner.IndependentModes.RequiredModeSession;
            var d = s.Definition;
            if (_owner.RequiredGame.UsesNativeAdventureConveyor)
            {
                DrawAdventureConveyorBackground();
            }
            else if (!d.UsesPackets && d.Mechanic != ModeMechanic.Aquarium && d.Mechanic != ModeMechanic.Beghouled
                && d.Mechanic != ModeMechanic.BeghouledTwist && d.Mechanic != ModeMechanic.IZombie)
            {
                DrawAdventureSeedBankBackground(_owner.Layout.AdventureSeedBankOrigin, _owner.Layout.SeedBankCapacity);
                DrawAdventureSunAmount(_owner.Layout.AdventureSeedBankOrigin);
            }
            else
            {
                Fill(new Rect(0, 0, 800, 80), new Color(0.25f, 0.19f, 0.08f, 0.95f));
                Label(new Rect(0, 5, 78, 30), "阳光", 17, Color.yellow, TextAnchor.MiddleCenter, true);
                Label(new Rect(0, 35, 78, 32), NumberLabel(_owner.RequiredGame.SunAmount), 23,
                    _owner.AdventureBoard.SunMoneyWarningRed ? Color.red : Color.white, TextAnchor.MiddleCenter, true);
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
                    var card = IndependentModeController.ModeZombieCardRect(i);
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
                    var card = _owner.IndependentModes.ModeSeedRect(i);
                    DrawAdventurePacket(_owner._deck[i], card);
                }
            }

        }

        /// <summary>Performs draw modes interface.</summary>
        /// <returns>The resulting draw modes interface.</returns>
        private bool DrawModesInterface()
        {
            if (!_owner.IndependentModes.IsModeBoard)
            {
                return false;
            }

            if (_owner.Opening.AdventureIntroActive)
            {
                _owner.Opening._adventureIntroDrawn = true;
                return true;
            }

            var s = _owner.IndependentModes.RequiredModeSession;
            var d = s.Definition;
            if (d.Mechanic == ModeMechanic.Aquarium)
            {
                DrawModeAquarium();
            }

            DrawModeBoardOverlays();
            if (d.Mechanic == ModeMechanic.Slots)
            {
                ModeButton(new Rect(500, 8, 95, 64), s.SlotRollingRemaining > 0 ? "转动中" : "拉动 25");
            }

            ModeButton(AdventureBoardController.s_adventureMenuButton, "菜单");
            Fill(new Rect(0, 568, 800, 32), new Color(0.06f, 0.1f, 0.04f, 0.9f));
            var status = d.Survival ? "旗帜 " + s.FlagsCompleted + " / " + (d.Endless ? "∞" : (d.Hard ? 10 : 5).ToString()) : d.Endless ? "连续通过 " + s.ClearedStages : d.Mechanic == ModeMechanic.Stars ? "杨桃 " + _owner.RequiredGame.ModeStarCount() + " / 14" : d.Mechanic == ModeMechanic.Beghouled || d.Mechanic == ModeMechanic.BeghouledTwist ? "配对 " + s.Score + " / 75" : d.Mechanic == ModeMechanic.IZombie ? "大脑 " + Array.FindAll(s.Brains, x => x).Length + " / 5" : "波次 " + _owner.RequiredGame.Adventure.CurrentWave + " / " + _owner.RequiredGame.Adventure.Waves.Count;
            Label(new Rect(8, 570, 605, 26), d.Name + "  " + status, 16, Color.white, TextAnchor.MiddleLeft, true);
            ModeButton(IndependentModeController.s_modeBackButton, "返回模式选择");
            var advice = ResolveAdventureAdvice(s.Advice);
            if (_owner.AdventureBoard._adventureFeedback != null && Time.unscaledTime < _owner.AdventureBoard._adventureFeedbackUntil)
            {
                advice = _owner.AdventureBoard._adventureFeedback;
            }

            if (_owner.IndependentModes._modeSaveError != null)
            {
                advice = _owner.IndependentModes._modeSaveError;
            }

            if (!string.IsNullOrEmpty(advice) && s.Phase != ModePhase.ChoosingSeeds)
            {
                DrawOriginalBoardAdvice(advice, false);
            }

            if (s.Phase == ModePhase.Preparing)
            {
                ModeButton(new Rect(295, 535, 210, 34), s.Stage == 1 ? "开始进攻" : "继续进攻");
            }

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
            Label(new Rect(130, 90, 540, 30), _owner.IndependentModes._modeChoosingImitater ? "选择模仿对象" : "选择植物 " + _owner.RequiredPendingSeeds.Count + " / " + Math.Min(_owner.IndependentModes.RequiredModeSession.PacketSlots, _owner.RequiredGame.Adventure.AvailableSeeds.Count), 22, Color.yellow, TextAnchor.MiddleCenter, true);
            var seeds = _owner.RequiredGame.Adventure.AvailableSeeds;
            if (_owner.IndependentModes._modeChoosingImitater)
            {
                for (var i = 0; i < 40; i++)
                {
                    DrawAdventureCard((PlantKind)i, BoardLayout.AdventureChooserRect(i), true);
                }
            }
            else
            {
                for (var i = 0; i < seeds.Count; i++)
                {
                    var card = BoardLayout.AdventureChooserRect(i);
                    DrawChooserHome(seeds[i], card);
                }
            }

            var slots = _owner.Layout.SeedBankCapacity;
            DrawChooserBank(slots);
            DrawChooserMovingPackets(seeds, slots, true);
            ModeButton(new Rect(300, 533, 200, 34), _owner.IndependentModes._modeChoosingImitater ? "取消" : "开始游戏");
        }

        /// <summary>Performs draw mode result.</summary>
        private void DrawModeResult()
        {
            var s = _owner.IndependentModes.RequiredModeSession;
            DrawOriginalUiDialog(new Rect(200, 166, 400, 370), s.HasNextStage ? "本轮完成" : s.Phase == ModePhase.Won ? "挑战成功！" : "挑战结束", null);
            var detail = s.HasNextStage ? "阵型和阳光将保留，准备下一轮。" : s.Phase == ModePhase.Won ? (_owner.IndependentModes._modeRewardClaimed ? "奖励已存入金币钱包" : "领取奖励：" + (_owner.IndependentModes._modeFirstClear ? 1000 : 250) + "金币") : s.Definition.Endless ? "最高连续通过 " + s.ClearedStages + " 轮" : "可以重新开始这一挑战。";
            if (_owner.IndependentModes._modeSaveError != null)
            {
                detail = _owner.IndependentModes._modeSaveError;
            }

            Label(new Rect(240, 260, 320, 70), detail, 18, Color.white, TextAnchor.MiddleCenter, true);
            ModeButton(IndependentModeController.s_modeNextButton, s.HasNextStage ? "继续下一轮" : s.Phase == ModePhase.Won ? (_owner.IndependentModes._modeRewardClaimed ? "返回模式选择" : "领取奖励") : "再试一次");
            ModeButton(IndependentModeController.s_modeRestartButton, "重新开始");
            ModeButton(IndependentModeController.s_modeExitButton, "返回模式选择");
        }

        /// <summary>Performs draw mode board overlays.</summary>
        private void DrawModeBoardOverlays()
        {
            var s = _owner.IndependentModes.RequiredModeSession;
            var m = s.Definition.Mechanic;
            if (m == ModeMechanic.Vases)
            {
                foreach (var vase in _owner.RequiredGame.Adventure.Vases)
                {
                    var lantern = _owner.RequiredGame.Plants.Exists(p => p.Kind == PlantKind.Plantern && Math.Max(Math.Abs(p.Row - vase.Row), Math.Abs(p.Column - vase.Column)) <= 1);
                    if (!lantern && !vase.ShowsPlant)
                    {
                        continue;
                    }

                    var icon = new Rect(BoardLayout.AdventureGridX + vase.Column * BoardLayout.CellWidth + 20, _owner.Layout.AdventureCellY(vase.Row, vase.Column) + 20, 40, 56);
                    if (vase._seed.HasValue)
                    {
                        DrawAdventureCard(vase._seed.Value, icon, false);
                    }
                    else if (lantern && vase._zombie.HasValue)
                    {
                        _owner.RequiredAnimations.Draw(AdventureAnimationCatalog.AdventureZombieAnimation(vase._zombie.Value), icon, _owner.RequiredGame.Elapsed);
                    }
                    else if (lantern)
                    {
                        _owner.RequiredAnimations.Draw("Sun", icon, _owner.RequiredGame.Elapsed);
                    }
                }
            }

            if (m == ModeMechanic.IZombie)
            {
                var boundary = s.Definition.Id <= 65 ? 4 : s.Definition.Id == 69 ? 6 : 5;
                Fill(new Rect(BoardLayout.AdventureGridX + boundary * BoardLayout.CellWidth - 2, 80, 4, 480), Color.red);
                for (var r = 0; r < 5; r++)
                {
                    if (!s.Brains[r])
                    {
                        var rect = new Rect(8, _owner.Layout.AdventureCellY(r, 0) + 27, 38, 35);
                        var brain = _owner.Textures.Load("brain.png");
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
                            Outline(new Rect(BoardLayout.AdventureGridX + c * BoardLayout.CellWidth + 8, _owner.Layout.AdventureCellY(r, c) + 18, 64, 65), new Color(1, 0.95f, 0.2f, 0.8f), 2);
                        }
                    }
                }
            }

            if (m == ModeMechanic.Beghouled || m == ModeMechanic.BeghouledTwist)
            {
                if (s.SelectedCell >= 0)
                {
                    Outline(new Rect(BoardLayout.AdventureGridX + (s.SelectedCell % 9) * BoardLayout.CellWidth, _owner.Layout.AdventureCellY(s.SelectedCell / 9, s.SelectedCell % 9), 80, 90), Color.yellow, 3);
                }
            }

            if (m == ModeMechanic.Portals)
            {
                foreach (var p in s.Portals)
                {
                    _owner.RequiredAnimations.DrawNative(p.Pair == 0 ? "Portal_Circle" : "Portal_Square", new Vector2(BoardLayout.AdventureGridX + p.Column * BoardLayout.CellWidth, _owner.Layout.AdventureCellY(p.Row, p.Column)), 1f, _owner.RequiredGame.Elapsed, "anim_pulse");
                }
            }

            if (m == ModeMechanic.RainingSeeds)
            {
                foreach (var rain in s.RainPackets)
                {
                    AdventurePacket? packet = null;
                    foreach (var p in _owner.RequiredGame.Adventure.Packets)
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

                    var rect = IndependentModeController.ModeRainRect(rain);
                    DrawAdventureConveyorPacket(packet, rect);
                }
            }

            if (m == ModeMechanic.Slots)
            {
                for (var i = 0; i < 3; i++)
                {
                    var symbol = s.SlotSymbols[i];
                    var text = symbol == 56 ? "阳光" : symbol == 57 ? "钻石" : _owner.AdventureBoard.AdventurePlantName((PlantKind)symbol);
                    Label(new Rect(250 + i * 105, 130, 100, 40), s.SlotRollingRemaining > 0 ? "转动" : text, 19, Color.yellow, TextAnchor.MiddleCenter, true);
                }
            }

            foreach (var shot in s.HeadShots)
            {
                DrawImage("PeaNormal_0.png", new Rect(BoardLayout.AdventureGridX + shot.X * BoardLayout.CellWidth - 12, _owner.Layout.AdventureCellY((int)shot.Row, shot.X) + 28, 24, 24));
            }
        }

        /// <summary>Performs draw mode aquarium.</summary>
        private void DrawModeAquarium()
        {
            DrawImage("aquarium1.jpg", new Rect(0, 0, 800, 600));
            foreach (var fish in _owner.IndependentModes.RequiredModeSession.Fish)
            {
                _owner.RequiredAnimations.Draw("Zombie_snorkle", new Rect(fish.X - 35, fish.Y - 30, 70, 65), _owner.RequiredGame.Elapsed, "anim_aquarium_swim");
                if (fish.Hunger <= 15)
                {
                    Label(new Rect(fish.X - 40, fish.Y - 50, 80, 22), "饿了！", 15, Color.red, TextAnchor.MiddleCenter, true);
                }
            }

            foreach (var food in _owner.IndependentModes.RequiredModeSession.Food)
            {
                DrawImage("brain.png", new Rect(food.X - 16, food.Y - 15.5f, 32, 31));
            }

            foreach (global::PlantsVsZombies.Simulation.Sun sun in _owner.RequiredGame.Suns)
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
        private void DrawOriginalUiDialog(Rect panel, string? title, string? body, bool tallBottom = true, bool shade = true)
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

        /// <summary>Draws the original MessageWidget tutorial or planting hint strip.</summary>
        /// <param name="text">The translated original advice.</param>
        /// <param name="tutorial">Whether the level-one or level-two tutorial layout is required.</param>
        private void DrawOriginalBoardAdvice(string text, bool tutorial)
        {
            // Original MessageWidget: tutorial y=476, height=60; Hint y=527, height=40.
            var bounds = new Rect(0, tutorial ? 446 : 497, 800, tutorial ? 60 : 40);
            Fill(bounds, new Color(0f, 0f, 0f, 128f / 255f));
            bounds.y -= 3f;
            var color = new Color(253f / 255f, 245f / 255f, 173f / 255f);
            if (!OriginalUiWidgets.Text(bounds, text, "HouseofTerror16", color, true, true))
            {
                Label(bounds, text, 18, color, TextAnchor.MiddleCenter, true);
            }
        }
        /// <summary>Stores display account refresh,  wallet visible until.</summary>
        private float _displayAccountRefresh, _walletVisibleUntil;
        /// <summary>Stores original progression strings.</summary>
        private readonly Dictionary<string, string> _originalProgressionStrings = new Dictionary<string, string>();
        /// <summary>Retains the last original advice key to avoid per-frame substring and placeholder allocations.</summary>
        private string? _resolvedAdviceSource;
        /// <summary>Retains the translated advice for the current original key and mode.</summary>
        private string _resolvedAdviceText = "";
        /// <summary>Identifies the mode whose native advice placeholders were resolved.</summary>
        private int _resolvedAdviceModeId = -1;

        /// <summary>Performs read display account.</summary>
        /// <returns>The resulting read display account.</returns>
        private PlayerAccount? ReadDisplayAccount()
        {
            if (_owner._profiles == null || !_owner._profiles.SelectedId.HasValue)
            {
                return null;
            }

            if (_owner.Accounts._displayAccountSnapshot == null || _owner.Accounts._displayAccountSnapshot.ProfileId != _owner._profiles.SelectedId.Value || Time.unscaledTime >= _displayAccountRefresh)
            {
                _owner.Accounts._displayAccountSnapshot = _owner.Accounts.GetSelectedAccount();
                _displayAccountRefresh = Time.unscaledTime + 1f;
            }

            return _owner.Accounts._displayAccountSnapshot;
        }

        /// <summary>Performs draw account status.</summary>
        private void DrawAccountStatus()
        {
            if (_owner._profiles == null || !_owner._profiles.SelectedId.HasValue)
            {
                return;
            }

            var p = _owner.Accounts.GetSelectedAccount();
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
            var t = _owner.Textures.Load(name);
            if (t == null && name.EndsWith(".png"))
            {
                t = _owner.Textures.Load(name.Substring(0, name.Length - 4) + ".jpg");
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
            var seeds = _owner.Textures.Load("seeds.png");
            if (seeds != null)
            {
                CanvasUI.DrawTextureWithTexCoords(card, seeds, new Rect(2f / 9f, 0, 1f / 9f, 1));
            }

            var cell = AlmanacPortraitLayout.PlantAtlasCell((int)kind);
            var atlas = cell < 0 ? null : _owner.Textures.Load("packet_plants.png");
            if (atlas != null)
            {
                CanvasUI.DrawTextureWithTexCoords(card, atlas, new Rect(cell / 13f, 0, 1f / 13f, 1));
            }
            else
            {
                Vector2 origin;
                float scale;
                AlmanacPortraitLayout.PlantThumbnailTransform((int)kind, card, out origin, out scale);
                _owner.RequiredAnimations.DrawNative(AdventurePlantAnimation(kind), origin, scale, 0f);
            }
        }

        /// <summary>Performs draw store.</summary>
        private void DrawStore()
        {
            var p = _owner.Accounts.GetSelectedAccount();
            if (p == null)
            {
                return;
            }

            var progress = _owner.Campaign.RequiredAdventureStore.Get(_owner.RequiredProfiles.RequiredSelectedId);
            DrawImage("Store_Background.jpg", new Rect(0, 0, 800, 600));
            if (_owner.Store._storeHatchOpen && _owner.Store._storeHatchTicks == 0)
            {
                DrawNativeProgressionImage("Store_Car.png", 196, 138);
                DrawNativeProgressionImage("Store_HatchbackOpen.png", 299, 0);
            }
            else
            {
                DrawNativeProgressionImage("Store_CarClosed.png", 196, 138);
            }
            DrawNativeProgressionImage("Store_Sign.png", 285, 0);
            if (_owner.Store._storeTacoDiamondVisible)
            {
                _owner.RequiredAnimations.DrawNative("Diamond", new Vector2(77, _owner.Store.StoreTacoDiamondY + 4f), 1f, _owner.Store._storeTacoDiamondAge);
            }
            if (_owner.Dave._crazyDaveAnimation != null)
            {
                if (_owner.Dave._crazyDaveMessageIndex >= 0)
                {
                    DrawNativeProgressionImage("Store_SpeechBubble.png", 63, 10);
                    _crazyDaveLabel ??= new GUIStyle();
                    _crazyDaveLabel.font = _owner._font;
                    _crazyDaveLabel.fontSize = 16;
                    _crazyDaveLabel.alignment = TextAnchor.MiddleCenter;
                    _crazyDaveLabel.wordWrap = true;
                    _crazyDaveLabel.normal.textColor = Color.black;
                    CanvasUI.Label(new Rect(88, 16, 233, 144), _owner.Dave._crazyDaveText, _crazyDaveLabel);
                }

                DrawCrazyDaveAt(new Vector2(-42, 68));
            }
            var page = CrazyDaveStoreController.s_originalShopPages[_owner.Store._shopPage];
            for (var i = 0; i < page.Length && _owner.Store._storeHatchOpen && _owner.Store._storeHatchTicks == 0; i++)
            {
                var item = ShopCatalog.Get(page[i]);
                var rect = CrazyDaveStoreController.ShopItemRect(i);
                bool locked = !ShopCatalog.IsUnlocked(item, progress.NextLevel, progress.CompletedRuns), sold = ShopCatalog.SoldOut(item, p, PvZGame.UtcSeconds);
                if (item.Id < 9)
                {
                    DrawProgressionPlantCard((PlantKind)(40 + item.Id), new Rect(rect.x, rect.y, 50, 70));
                }
                else if (item.Id >= 10 && item.Id <= 12)
                {
                    _owner.RequiredAnimations.Draw("ZenGarden_sprout", new Rect(rect.x, rect.y + 12, 70, 62), Time.unscaledTime, "anim_idle");
                }
                else if (item.Id == 20)
                {
                    _owner.RequiredAnimations.Draw(item.Id == 22 ? "PoolCleaner" : item.Id == 23 ? "RoofCleaner" : "Stinky", new Rect(rect.x + 8, rect.y + 8, 58, 55), 0f);
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
                else if (item.Id < 9 && rect.Contains(_owner.AdventureBoard._adventurePointer))
                {
                    DrawImage("SeedPacketFlash.png", new Rect(rect.x, rect.y, 50, 70));
                }

                if (item.Id == _owner.Store._selectedShopItem)
                {
                    Outline(rect, Color.yellow, 2);
                }
            }

            OriginalUiWidgets.ImageButton(_textureLookup, new Rect(252, 402, _owner.Textures.RequiredLoad("Store_PrevButton.png").width, _owner.Textures.RequiredLoad("Store_PrevButton.png").height), "Store_PrevButton.png", "Store_PrevButtonHighlight.png", null, OriginalUiWidgets.Hovered(new Rect(252, 402, 96, 75)), OriginalUiWidgets.Pressed(new Rect(252, 402, 96, 75)), true);
            OriginalUiWidgets.ImageButton(_textureLookup, new Rect(596, 402, _owner.Textures.RequiredLoad("Store_NextButton.png").width, _owner.Textures.RequiredLoad("Store_NextButton.png").height), "Store_NextButton.png", "Store_NextButtonHighlight.png", null, OriginalUiWidgets.Hovered(new Rect(596, 402, 136, 83)), OriginalUiWidgets.Pressed(new Rect(596, 402, 136, 83)), true);
            OriginalUiWidgets.ImageButton(_textureLookup, new Rect(366, 512, 138, 80), "Store_MainMenuButton.png", "Store_MainMenuButtonHighlight.png", "Store_MainMenuButtonDown.png", OriginalUiWidgets.Hovered(new Rect(366, 512, 138, 80)), OriginalUiWidgets.Pressed(new Rect(366, 512, 138, 80)));
            Label(new Rect(366, 512, 138, 80), OriginalProgressionText(_owner.Store._storeIntroNextLabel ? "STORE_NEXT_LEVEL_BUTTON" : "STORE_MAIN_MENU_BUTTON"), 15, new Color(98f / 255f, 153f / 255f, 235f / 255f), TextAnchor.MiddleCenter, true);
            DrawNativeProgressionImage("coinbank.png", 650, 559);
            Label(new Rect(650, 559, 116, 30), p.DisplayCoins.ToString(), 23, new Color(0.7f, 1f, 0.35f), TextAnchor.MiddleRight, true);
            Label(new Rect(330, 475, 280, 26), "疯狂戴夫的商店  " + (_owner.Store._shopPage + 1) + " / 4", 19, Color.white, TextAnchor.MiddleCenter, true);
            DrawProgressionMessage();
            if (_owner.Store._selectedShopItem >= 0)
            {
                DrawStorePurchaseDialog();
            }
        }

        /// <summary>Draws the original wallet and packet upgrade confirmation.</summary>
        private void DrawPacketUpgradeOffer()
        {
            var account = _owner.Accounts.GetSelectedAccount();
            if (account == null)
            {
                return;
            }

            var amount = CrazyDaveDialogue.FormatMoney(account.DisplayCoins);
            DrawImage("coinbank.png", new Rect(16, 559, 128, 31));
            Label(new Rect(22, 561, 112, 26), amount, 17, new Color(.7f, 1f, .35f), TextAnchor.MiddleRight, true);
            var body = OriginalProgressionText("UPGRADE_DIALOG_BODY").Replace("{SLOTS}", (account.PacketSlots + 1).ToString());
            DrawOriginalUiDialog(new Rect(320, 166, 400, 270), amount,
                string.IsNullOrEmpty(_owner.Accounts._progressionMessage) ? body : _owner.Accounts._progressionMessage);
            DrawOriginalUiButton(AdventureOpeningController.s_packetOfferYes, OriginalProgressionText("DIALOG_BUTTON_YES"));
            DrawOriginalUiButton(AdventureOpeningController.s_packetOfferNo, OriginalProgressionText("DIALOG_BUTTON_NO"));
        }

        /// <summary>Performs draw store purchase dialog.</summary>
        private void DrawStorePurchaseDialog()
        {
            DrawOriginalUiDialog(new Rect(200, 166, 400, 270), OriginalProgressionText("BUY_ITEM_HEADER"), string.IsNullOrEmpty(_owner.Accounts._progressionMessage) ? OriginalProgressionText("BUY_ITEM") : _owner.Accounts._progressionMessage);
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
            var p = _owner.Accounts.GetSelectedAccount();
            if (p == null)
            {
                return;
            }

            DrawImage(_owner.Garden._gardenKind == GardenKind.Greenhouse ? "Background_Greenhouse.jpg" : _owner.Garden._gardenKind == GardenKind.Mushroom ? "Background_MushroomGarden.jpg" : "aquarium1.jpg", new Rect(0, 0, 800, 600));
            foreach (var g in p.Plants)
            {
                if (g.Garden != _owner.Garden._gardenKind)
                {
                    continue;
                }

                var r = _owner.Garden.GardenPlantRect(g);
                if (g.Garden != GardenKind.Aquarium)
                {
                    _owner.RequiredAnimations.Draw("FlowerPot", new Rect(r.x + 8, r.y + 39, 64, 49), 0f, "anim_zengarden");
                }

                var anim = g.Growth == 0 ? "ZenGarden_sprout" : AdventurePlantAnimation(g.Kind);
                float size = g.Growth == 0 ? 55 : g.Growth == 1 ? 56 : g.Growth == 2 ? 68 : 80;
                _owner.RequiredAnimations.Draw(anim, new Rect(r.center.x - size / 2, r.y + 70 - size, size, size), Time.unscaledTime, g.Growth == 0 ? "anim_idle" : g.Garden == GardenKind.Aquarium && (g.Kind == PlantKind.TangleKelp || g.Kind == PlantKind.SeaShroom) ? "anim_idle_aquarium" : null);
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
                if ((int)_owner.Garden._gardenTool == i)
                {
                    Outline(r, Color.yellow, 2);
                }
            }

            if (!_owner.Garden._gardenReturnToAdventure || _owner.Garden._gardenTutorialStage == 6)
            {
                DrawOriginalUiButton(new Rect(674, 6, 120, 36), _owner.Garden._gardenReturnToAdventure ? OriginalProgressionText("CONTINUE_BUTTON") : "主菜单");
            }

            if (!_owner.Garden._gardenReturnToAdventure || _owner.Garden._gardenTutorialStage >= 4)
            {
                DrawOriginalUiButton(new Rect(654, 540, 136, 42), "商店");
            }

            if (!_owner.Garden._gardenReturnToAdventure)
            {
                DrawOriginalUiButton(new Rect(8, 540, 155, 42), "下一个花园");
                DrawOriginalUiButton(new Rect(175, 540, 120, 42), "智慧树");
            }
            Label(new Rect(310, 548, 200, 28), "金币：" + p.DisplayCoins, 19, Color.yellow, TextAnchor.MiddleCenter, true);
            if (p.PurchaseCount(20) > 0)
            {
                _owner.RequiredAnimations.Draw("Stinky", new Rect(500, 494, 80, 62), Time.unscaledTime);
                Label(new Rect(490, 551, 105, 22), "点击唤醒蜗牛", 13, Color.white, TextAnchor.MiddleCenter, true);
            }

            DrawAdventureCrazyDave();
            if (_owner.Garden._gardenSalePlant.HasValue)
            {
                DrawOriginalUiDialog(new Rect(320, 226, 400, 270), OriginalProgressionText("ZEN_SELL_HEADER"), OriginalProgressionText("ZEN_SELL_LINES"));
                DrawOriginalUiButton(ZenGardenController.s_gardenSaleYes, OriginalProgressionText("DIALOG_BUTTON_YES"));
                DrawOriginalUiButton(ZenGardenController.s_gardenSaleNo, OriginalProgressionText("DIALOG_BUTTON_NO"));
            }
            DrawProgressionMessage();
        }

        /// <summary>Resolves an original bracketed board-advice key while preserving explicit diagnostic text.</summary>
        /// <param name="advice">An original LawnStrings key, translated advice, or runtime diagnostic.</param>
        /// <returns>The original localized advice with its native flag target, an unchanged diagnostic, or an empty string.</returns>
        internal string ResolveAdventureAdvice(string? advice)
        {
            if (string.IsNullOrEmpty(advice))
            {
                return "";
            }

            if (advice.Length < 3 || advice[0] != '[' || advice[advice.Length - 1] != ']')
            {
                return advice;
            }

            var definition = _owner.IndependentModes._modeSession?.Definition ?? _owner._game?.ModeSession?.Definition;
            var modeId = definition?.Id ?? 0;
            if (modeId == _resolvedAdviceModeId && string.Equals(advice, _resolvedAdviceSource, StringComparison.Ordinal))
            {
                return _resolvedAdviceText;
            }

            var key = advice.Substring(1, advice.Length - 2);
            var text = OriginalProgressionText(key);
            if (text == key)
            {
                text = "";
            }

            if (definition != null && key == "ADVICE_SURVIVE_FLAGS")
            {
                var flags = definition.Mechanic == ModeMechanic.LastStand || !definition.Hard ? 5 : 10;
                text = text.Replace("{FLAGS}", flags.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            _resolvedAdviceSource = advice;
            _resolvedAdviceModeId = modeId;
            _resolvedAdviceText = text;
            return text;
        }

        /// <summary>Performs original progression text.</summary>
        /// <param name = "key">The key.</param>
        /// <returns>The resulting original progression text.</returns>
        internal string OriginalProgressionText(string key)
        {
            if (_originalProgressionStrings.Count == 0)
            {
                var file = Path.Combine(_owner._root, "properties", "LawnStrings.txt");
                if (!File.Exists(file))
                {
                    return key;
                }

                string? current = null;
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
            var p = _owner.Accounts.GetSelectedAccount();
            if (p == null)
            {
                return;
            }

            var pose = Math.Min(_owner.Tree._treeGrowStarted < 0 ? 50 : 51, Math.Max(1, p.TreeHeight));
            global::PlantsVsZombies.Reanimation.AnimationInfo treeInfo;
            var fps = _owner.RequiredAnimations.TryGetAnimationInfo("TreeOfWisdom", "anim_grow" + pose, out treeInfo) ? treeInfo.FramesPerSecond : 12f;
            var age = _owner.Tree._treeGrowStarted < 0 ? 100f : (Time.unscaledTime - _owner.Tree._treeGrowStarted) * 8f / fps;
            _owner.RequiredAnimations.DrawNative("TreeOfWisdom", Vector2.zero, 1f, age, "anim_grow" + pose, false);
            if (!_owner.Tree._treeDaveIntroPending)
            {
                DrawOriginalUiButton(new Rect(655, 8, 138, 40), "禅境花园");
                DrawOriginalUiButton(new Rect(655, 548, 138, 42), "商店");
                DrawImage("TreeFood.png", new Rect(24, 505, 68, 80));
                DrawOriginalUiButton(new Rect(102, 539, 175, 44), "施肥（" + p.PurchaseCount(28) + "）");
            }
            if (p.TreeHeight >= 50)
            {
                Label(new Rect(250, 8, 300, 30), OriginalProgressionText("TREE_OF_WISDOM_HIEGHT").Replace("{HEIGHT}", p.TreeHeight.ToString()), 20, Color.white, TextAnchor.MiddleCenter, true);
            }

            var wisdom = _owner.Tree._treeSpeechClock?.MessageIndex ?? -1;
            if (wisdom >= 0 && !_owner.Tree._treeDaveIntroPending)
            {
                var advice = OriginalProgressionText("TREE_OF_WISDOM_" + wisdom);
                float speechX = p.TreeHeight < 7 ? 400 : p.TreeHeight < 12 ? 395 : 390, speechY = p.TreeHeight < 7 ? 152 : p.TreeHeight < 12 ? 60 : 52;
                DrawNativeProgressionImage("Store_SpeechBubble2.png", speechX, speechY);
                Label(new Rect(speechX + 25, speechY + 6, 233, 144), advice, 16, Color.black, TextAnchor.MiddleCenter, false);
            }

            DrawAdventureCrazyDave();
            DrawProgressionMessage();
        }

        /// <summary>Performs draw progression message.</summary>
        private void DrawProgressionMessage()
        {
            if (!string.IsNullOrEmpty(_owner.Accounts._progressionMessage))
            {
                DrawOriginalUiAdvice(new Rect(12, 574, 776, 24), _owner.Accounts._progressionMessage);
            }
        }

        /// <summary>Performs draw original save utility.</summary>
        private void DrawOriginalSaveUtility()
        {
            DrawImage("Challenge_Background.jpg", new Rect(0, 0, 800, 600));
            DrawOriginalUiDialog(new Rect(20, 20, 760, 560), "原版年度版档案导入 / 导出", null, true, false);
            Label(new Rect(55, 88, 690, 36), "输入 userdata 文件夹，或单独的 user*.dat 路径", 17, Color.white, TextAnchor.MiddleLeft, false);
            _owner.SaveUtility._originalImportPath = OriginalUiTextField(new Rect(55, 128, 570, 32), _owner.SaveUtility._originalImportPath);
            if (OriginalUiButton(new Rect(635, 128, 110, 32), "读取"))
            {
                _owner.QueueUiAction(_owner.SaveUtility.ReadOriginalSaves);
            }

            if (_owner.SaveUtility._importCandidates != null)
            {
                var shown = Math.Min(6, _owner.SaveUtility._importCandidates.Count);
                for (var i = 0; i < shown; i++)
                {
                    var p = _owner.SaveUtility._importCandidates[i];
                    if (OriginalUiListRow(new Rect(70, 181 + i * 33, 660, 29), p.Name + "  冒险 " + AdventureLevels.Get(p.Level).Label + "  金币 " + p.Coins * 10 + "  通关 " + p.FinishedAdventure + " 次", i == _owner.SaveUtility._importSelection))
                    {
                        _owner.SaveUtility._importSelection = i;
                    }
                }

                if (shown > 0 && OriginalUiButton(new Rect(300, 389, 200, 36), "导入为新用户"))
                {
                    _owner.QueueUiAction(_owner.SaveUtility.ImportOriginalUser);
                }
            }

            Label(new Rect(55, 434, 690, 30), "从当前用户导出独立 user*.dat（需先导入原版用户）", 16, Color.white, TextAnchor.MiddleLeft, false);
            _owner.SaveUtility._originalExportPath = OriginalUiTextField(new Rect(55, 471, 570, 32), _owner.SaveUtility._originalExportPath);
            if (OriginalUiButton(new Rect(635, 471, 110, 32), "导出"))
            {
                _owner.QueueUiAction(_owner.SaveUtility.ExportOriginalSave);
            }

            if (OriginalUiButton(new Rect(320, 530, 160, 36), "返回主菜单"))
            {
                _owner.QueueUiAction(_owner.SaveUtility.ReturnFromSaveUtility);
            }

            DrawProgressionMessage();
        }

        /// <summary>Performs draw original hover tips.</summary>
        private void DrawOriginalHoverTips()
        {
            if (_owner.RequiredOptionsView.IsOpen || _owner._unavailableFeature != null || _owner._confirmingQuit || _owner._profileDialog != null && _owner._profileDialog.IsOpen)
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

            if (_owner.Opening.AdventureIntroActive && !_owner.Opening.AdventureIntroAwaitingSeeds)
            {
                return;
            }

            if (_owner.Opening.AdventureIntroAwaitingSeeds)
            {
                foreach (PlantKind kind in _owner._game.Adventure.AvailableSeeds)
                {
                    var rect = _owner.Opening.AdventureCampaignChooserRect(kind);
                    if (rect.Contains(_owner.AdventureBoard._adventurePointer))
                    {
                        DrawOriginalPlantTooltip(kind, rect, true);
                        return;
                    }
                }

                return;
            }

            if (_owner.IndependentModes.IsModeBoard && _owner.IndependentModes.RequiredModeSession.Definition.UsesPackets || _owner._game.Adventure.Level.HasConveyor)
            {
                return;
            }

            for (var i = 0; i < _owner._deck.Count; i++)
            {
                var rect = _owner.IndependentModes.IsModeBoard ? _owner.IndependentModes.ModeSeedRect(i) : _owner.Layout.AdventureSeedRect(i);
                if (rect.Contains(_owner.AdventureBoard._adventurePointer))
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
            var shown = kind == PlantKind.Imitater && _owner.RequiredGame.Adventure.ImitaterTarget.HasValue ? _owner.RequiredGame.Adventure.RequiredImitaterTarget : kind;
            if ((int)shown < 0 || (int)shown >= VerifiedDataCatalog.Plants.Count)
            {
                return;
            }

            var key = VerifiedDataCatalog.Plants[(int)shown].InternalName;
            var warning = chooser ? "" : _owner.RequiredGame.CooldownRemaining(kind) > 0 ? OriginalProgressionText("WAITING_TO_REFRESH") : _owner.RequiredGame.AdventureSunAvailable < _owner.RequiredGame.AdventureSeedCost(kind) ? OriginalProgressionText("NOT_ENOUGH_SUN") : "";
            DrawOriginalTooltipRequest(new OriginalTooltipRequest {
                Title = OriginalProgressionText(key),
                Body = chooser ? OriginalProgressionText(key + "_TOOLTIP") : "",
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
            var seeds = _owner.Textures.Load("seeds.png");
            if (seeds != null)
            {
                _owner.RequiredAnimations.DrawNativeImage(seeds, rect, new Rect(2f / 9f, 0, 1f / 9f, 1));
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
                _owner.RequiredAnimations.DrawZombieNative(origin, scale, 0f, variant, "anim_idle", true);
            }
            else
            {
                _owner.RequiredAnimations.DrawNative(AdventureAnimationCatalog.AdventureZombieAnimation(kind), origin, scale, 0f, kind == ZombieKind.Pogo ? "anim_pogo" : kind == ZombieKind.PoleVaulter ? "anim_walk" : "anim_idle");
            }

            Label(new Rect(rect.x + 2, rect.y + rect.height - 17, rect.width - 7, 15), GameSimulation.ModeZombieCost(kind).ToString(), 13, Color.black, TextAnchor.MiddleRight, true);
        }
    }
}
