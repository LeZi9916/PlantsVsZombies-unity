#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.UI;
using PlantsVsZombies.VerifiedData;
using UnityEngine;

namespace PlantsVsZombies.Game
{
    /// <summary>Prepares derived battle storage in bounded main-thread steps without advancing the authoritative world.</summary>
    internal sealed class GamePreparationController
    {
        /// <summary>The cooperative preparation budget; a single Unity resource operation cannot be interrupted.</summary>
        internal const double BudgetMilliseconds = 2d;
        /// <summary>The game host whose current board owns this preparation.</summary>
        private readonly PvZGame _owner;
        /// <summary>The resource dependencies retained across preparation steps.</summary>
        private readonly List<string> _assets = new List<string>(96);
        /// <summary>The board generation for which capacity is being prepared.</summary>
        private GameSimulation? _preparedGame;
        /// <summary>The native survival stage whose wave dependencies are prepared.</summary>
        private int _stage = -1;
        /// <summary>The nonauthoritative pool capacities computed before gameplay begins.</summary>
        private SimulationCapacityRequirements _requirements;
        /// <summary>The next asset dependency to prepare.</summary>
        private int _asset;
        /// <summary>Tracks whether the current animation has been parsed before incremental region preparation.</summary>
        private bool _assetOpened;
        /// <summary>The current resource, simulation, presentation, or GPU preparation phase.</summary>
        private int _phase = 5;
        /// <summary>The bounded count of retained Editor inspection actors.</summary>
        private int _actors;
        /// <summary>The bounded count of retained Editor inspection sprites.</summary>
        private int _sprites;
        /// <summary>The expected clipped polygon population for scene-buffer preparation.</summary>
        private int _instances;
        /// <summary>The lazily created preparation label style.</summary>
        private GUIStyle? _style;
        /// <summary>The last formatted preparation percentage.</summary>
        private int _displayedPercent = -1;
        /// <summary>The preparation message, formatted only when progress changes.</summary>
        private string _message = string.Empty;
        /// <summary>Gets whether necessary capacity has been prepared for the current board and stage.</summary>
        internal bool IsReady => _phase == 5;
        /// <summary>Gets whether board interaction must wait before the first admitted simulation tick.</summary>
        internal bool BlocksGameplay => !IsReady && _owner._currentView == PvZGame.ScreenView.Board
            && !_owner._choosingSeeds && !_owner.Opening.AdventureIntroActive;
        /// <summary>Gets the latest cooperative preparation duration, including indivisible Unity operations.</summary>
        internal double LastStepMilliseconds { get; private set; }
        /// <summary>Gets the number of admitted preparation steps for diagnostics.</summary>
        internal int StepCount { get; private set; }

        /// <summary>Creates a preparation coordinator without touching Unity resources.</summary>
        /// <param name="owner">The host whose board and loading UI are coordinated.</param>
        internal GamePreparationController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Spends at most the cooperative budget preparing a current board without changing its clocks or RNG.</summary>
        internal void Advance()
        {
            var game = _owner._game;
            if (game == null || !game.IsAdventure || _owner._currentView != PvZGame.ScreenView.Board)
            {
                if (_preparedGame != null)
                {
                    _owner._animations?.ReleasePreparedPages();
                }
                _preparedGame = null;
                _stage = -1;
                _phase = 5;
                _assets.Clear();
                return;
            }
            var stage = game.ModeSession?.Stage ?? 0;
            if (!ReferenceEquals(_preparedGame, game) || _stage != stage)
            {
                Begin(game, stage);
            }
            if (IsReady)
            {
                return;
            }
            var start = Stopwatch.GetTimestamp();
            do
            {
                Step(game);
                StepCount++;
                LastStepMilliseconds = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
            }
            while (!IsReady && LastStepMilliseconds < BudgetMilliseconds);
        }

        /// <summary>Displays progress only when the player would otherwise enter an unprepared live board.</summary>
        internal void DrawOverlay()
        {
            if (!BlocksGameplay || _owner.Loading.OriginalLoadingActive)
            {
                return;
            }
            var percent = _phase == 0 ? _asset * 20 / Math.Max(1, _assets.Count) : _phase * 20;
            if (_displayedPercent != percent)
            {
                _displayedPercent = percent;
                _message = "正在准备关卡… " + percent + "%";
            }
            if (_style == null)
            {
                _style = new GUIStyle { font = _owner._font, fontSize = 20, alignment = TextAnchor.MiddleCenter };
                _style.normal.textColor = Color.white;
            }
            var previous = CanvasUI.Color;
            CanvasUI.Color = new Color(0f, 0f, 0f, .85f);
            CanvasUI.DrawTexture(new Rect(170f, 260f, 460f, 80f), Texture2D.whiteTexture);
            CanvasUI.Color = Color.white;
            CanvasUI.Label(new Rect(170f, 260f, 460f, 80f), _message, _style);
            CanvasUI.Color = previous;
        }

        /// <summary>Collects known stage dependencies before admitting its first gameplay update.</summary>
        /// <param name="game">The authoritative board, read without invoking gameplay constructors.</param>
        /// <param name="stage">The native stage identity.</param>
        private void Begin(GameSimulation game, int stage)
        {
            _preparedGame = game;
            _stage = stage;
            _phase = 0;
            _asset = 0;
            _assetOpened = false;
            _displayedPercent = -1;
            StepCount = 0;
            _requirements = game.GetCapacityRequirements();
            var population = Math.Max(1, _requirements.Plants + _requirements.Zombies);
            _actors = Math.Min(256, population);
            _sprites = Math.Min(4096, RoundedCapacity(population * 24));
            _instances = Math.Min(65536, RoundedCapacity(population * 40));
            _assets.Clear();
            AddAsset("LawnMower");
            foreach (var plant in game.Plants)
            {
                AddPlant(plant.Kind);
            }
            foreach (var seed in game.Adventure.AvailableSeeds)
            {
                AddPlant(seed);
            }
            foreach (var zombie in game.Zombies)
            {
                AddAsset(GameSimulation.AdventureZombiePresentationAsset(zombie));
            }
            var probe = new Zombie();
            foreach (var wave in game.Adventure.Waves)
            {
                foreach (var kind in wave.Zombies)
                {
                    probe.Kind = kind;
                    AddAsset(GameSimulation.AdventureZombiePresentationAsset(probe));
                    if (kind == ZombieKind.Dancing)
                    {
                        probe.Kind = ZombieKind.BackupDancer;
                        AddAsset(GameSimulation.AdventureZombiePresentationAsset(probe));
                    }
                    else if (kind == ZombieKind.Gargantuar)
                    {
                        probe.Kind = ZombieKind.Imp;
                        AddAsset(GameSimulation.AdventureZombiePresentationAsset(probe));
                    }
                }
            }
            _owner._presenter.PrepareBattlePresentationSession();
            _owner.RequiredAnimations.PreparePresentationSession(game);
        }

        /// <summary>Performs one independently bounded preparation operation.</summary>
        /// <param name="game">The board whose derived capacity is being prepared.</param>
        private void Step(GameSimulation game)
        {
            switch (_phase)
            {
                case 0:
                    if (_asset < _assets.Count)
                    {
                        if (!_assetOpened)
                        {
                            _assetOpened = _owner.RequiredAnimations.PreparePresentationAsset(_assets[_asset]);
                            if (!_assetOpened)
                            {
                                _asset++;
                                break;
                            }
                        }
                        if (_owner.RequiredAnimations.PreparePresentationAssetRegionsStep(_assets[_asset], 1))
                        {
                            _asset++;
                            _assetOpened = false;
                        }
                    }
                    else
                    {
                        _phase = 1;
                    }
                    break;
                case 1:
                    if (game.PrepareCapacityStep(in _requirements, 16))
                    {
                        _phase = 2;
                    }
                    break;
                case 2:
                    if (_owner.RequiredAnimations.PreparePresentationCapacityStep(_actors, _sprites, _actors, 1))
                    {
                        _phase = 3;
                    }
                    break;
                case 3:
                    if (_owner._presenter.PrepareBattleParticlesStep(Math.Min(32, Math.Max(4, _requirements.Events / 8))))
                    {
                        _phase = 4;
                    }
                    break;
                case 4:
                    if (ObjectSpriteRenderer.PrepareSceneCapacityStep(_instances, 1))
                    {
                        _phase = 5;
                    }
                    break;
            }
        }

        /// <summary>Adds one immutable plant resource identity without sampling a live plant.</summary>
        /// <param name="kind">The plant whose original animation will be available in this board.</param>
        private void AddPlant(PlantKind kind)
        {
            var id = (int)kind;
            if (id >= 0 && id < VerifiedDataCatalog.Plants.Count
                && VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Plants[id].ReanimationId, out var definition))
            {
                AddAsset(Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/')));
            }
        }

        /// <summary>Retains a known immutable resource once per stage preparation.</summary>
        /// <param name="name">The original animation identity.</param>
        private void AddAsset(string name)
        {
            if (!_assets.Contains(name))
            {
                _assets.Add(name);
            }
        }

        /// <summary>Adds twenty-five percent headroom and rounds up to reusable power-of-two capacity.</summary>
        /// <param name="demand">The observed or stage-derived demand.</param>
        /// <returns>The rounded capacity with bounded arithmetic.</returns>
        private static int RoundedCapacity(int demand)
        {
            var target = Math.Min(65536, Math.Max(1, demand + (demand + 3) / 4));
            var capacity = 1;
            while (capacity < target)
            {
                capacity *= 2;
            }
            return capacity;
        }
    }
}
