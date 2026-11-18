#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.VerifiedData;

namespace PlantsVsZombies.Game
{
    /// <summary>Prepares derived battle storage before the first admitted gameplay update without advancing the authoritative world.</summary>
    internal sealed class GamePreparationController
    {
        /// <summary>Failsafe bound on preparation steps in one call; normal preparation finishes far below it.</summary>
        private const int MaximumStepsPerAdvance = 1 << 16;
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
        private int _phase = 6;
        /// <summary>The bounded count of retained Editor inspection actors.</summary>
        private int _actors;
        /// <summary>The prepared playback identities, including native heads and attachments independently of Editor roots.</summary>
        private int _playbacks;
        /// <summary>The bounded count of retained Editor inspection sprites.</summary>
        private int _sprites;
        /// <summary>The expected clipped polygon population for scene-buffer preparation.</summary>
        private int _instances;
        /// <summary>The independently restored production lifecycle being rehearsed during loading.</summary>
        private SimulationPreparationRehearsal? _rehearsal;
        /// <summary>Gets whether necessary capacity has been prepared for the current board and stage.</summary>
        internal bool IsReady => _phase == 6;
        /// <summary>Gets whether board interaction must wait before the first admitted simulation tick.</summary>
        internal bool BlocksGameplay => !IsReady && _owner._currentView == PvZGame.ScreenView.Board
            && !_owner._choosingSeeds && !_owner.Opening.AdventureIntroActive;
        /// <summary>Gets the latest preparation duration, including indivisible Unity operations.</summary>
        internal double LastStepMilliseconds { get; private set; }
        /// <summary>Gets the number of admitted preparation steps for diagnostics.</summary>
        internal int StepCount { get; private set; }
        /// <summary>Tracks whether an abnormal preparation overrun was already reported for this board.</summary>
        private bool _overrunLogged;

        /// <summary>Creates a preparation coordinator without touching Unity resources.</summary>
        /// <param name="owner">The host whose board is being prepared.</param>
        internal GamePreparationController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Prepares a current board completely without changing its clocks or RNG.</summary>
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
                _phase = 6;
                _rehearsal = null;
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
            for (var step = 0; !IsReady; step++)
            {
                if (step >= MaximumStepsPerAdvance)
                {
                    if (!_overrunLogged)
                    {
                        _overrunLogged = true;
                        UnityEngine.Debug.LogWarning($"{nameof(GamePreparationController)}: Preparation did not finish within {MaximumStepsPerAdvance} steps; continuing next frame.");
                    }
                    break;
                }

                Step(game);
                StepCount++;
            }

            LastStepMilliseconds = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
        }

        /// <summary>Collects known stage dependencies before admitting its first gameplay update.</summary>
        /// <param name="game">The authoritative board, read without invoking gameplay constructors.</param>
        /// <param name="stage">The native stage identity.</param>
        private void Begin(GameSimulation game, int stage)
        {
            _preparedGame = game;
            _stage = stage;
            _rehearsal = null;
            _asset = 0;
            _assetOpened = false;
            StepCount = 0;
            LastStepMilliseconds = 0d;
            _overrunLogged = false;
            _assets.Clear();
            if (game.ModeSession is { Definition: { Survival: true }, HasNextStage: true })
            {
                // This board can be displayed immediately. The following stage owns its own combat preparation.
                _owner.RequiredAnimations.ReleasePreparedPages();
                _phase = 6;
                return;
            }

            _phase = 0;
            _requirements = game.GetCapacityRequirements();
            var population = Math.Max(1, _requirements.Plants + _requirements.Zombies);
            _actors = Math.Min(256, population);
            _playbacks = Math.Min(2048, RoundedCapacity(population * 2));
            _sprites = Math.Min(65536, RoundedCapacity(population * 40));
            _instances = Math.Min(65536, RoundedCapacity(population * 40));
            AdventureZombieDeathData.TryGet("Zombie", "anim_death", out _);
            for (var kind = 0; kind < VerifiedDataCatalog.Zombies.Count; kind++)
            {
                AdventureAnimationCatalog.AdventureZombieAnimation((ZombieKind)kind);
            }
            AddAsset("LawnMower");
            AddAsset("Sun");
            AddAsset("Coin_silver");
            AddAsset("Coin_gold");
            AddAsset("Diamond");
            AddAsset("splash");
            AddAsset("Zombie_surprise");
            AddAsset("Zombie_FlagPole");
            AddAsset("Digger_rising_dirt");
            for (var id = 36; id <= 41; id++)
            {
                if (VerifiedDataCatalog.TryGetReanimation(id, out var burn))
                {
                    AddAsset(Path.GetFileNameWithoutExtension(burn.Path.Replace('\\', '/')));
                }
            }
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
                    probe.Kind = (int)kind == 32 ? ZombieKind.Gargantuar : kind;
                    AddAsset(GameSimulation.AdventureZombiePresentationAsset(probe));
                    if (kind == ZombieKind.Dancing)
                    {
                        probe.Kind = ZombieKind.BackupDancer;
                        AddAsset(GameSimulation.AdventureZombiePresentationAsset(probe));
                    }
                    else if (kind == ZombieKind.Gargantuar || (int)kind == 32)
                    {
                        probe.Kind = ZombieKind.Imp;
                        AddAsset(GameSimulation.AdventureZombiePresentationAsset(probe));
                    }
                }
            }
            _owner._presenter.PrepareBattlePresentationSession();
            _owner._presenter.PrepareBattlePresentationCapacity(_requirements.Plants + _requirements.Zombies);
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
                    if (_owner.RequiredAnimations.PreparePresentationCapacityStep(_actors, _sprites, _playbacks, 1))
                    {
                        _phase = 3;
                    }
                    break;
                case 3:
                    if (_owner._presenter.PrepareBattleParticlesStep(Math.Min(128, Math.Max(4, _requirements.Events / 2))))
                    {
                        _phase = 4;
                    }
                    break;
                case 4:
                    if (_owner.RequiredAnimations.PrepareFinalRegionStorageStep()
                        && ObjectSpriteRenderer.PrepareSceneCapacityStep(_instances, 1))
                    {
                        _phase = 5;
                    }
                    break;
                case 5:
                    if (_rehearsal == null)
                    {
                        _rehearsal = game.CreatePreparationRehearsal();
                    }
                    else if (_rehearsal.Step())
                    {
                        _rehearsal = null;
                        _phase = 6;
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
