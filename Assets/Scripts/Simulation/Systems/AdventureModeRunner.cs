#nullable enable
using PlantsVsZombies.Adventure;

using System;
using System.Collections.Generic;

using System.Diagnostics.CodeAnalysis;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns AdventureModeRunner responsibilities for one deterministic match.</summary>
    internal sealed class AdventureModeRunner
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureSpecials operations.</summary>
        private AdventureSpecials _adventureSpecials = null!;
        /// <summary>Provides BattlePresentationSystem operations.</summary>
        private BattlePresentationSystem _battlePresentationSystem = null!;
        /// <summary>Provides BoardEnvironment operations.</summary>
        private BoardEnvironment _boardEnvironment = null!;
        /// <summary>Provides BossSystem operations.</summary>
        private BossSystem _bossSystem = null!;
        /// <summary>Provides CoinSystem operations.</summary>
        private CoinSystem _coinSystem = null!;
        /// <summary>Provides ConveyorSystem operations.</summary>
        private ConveyorSystem _conveyorSystem = null!;
        /// <summary>Provides EconomySystem operations.</summary>
        private EconomySystem _economySystem = null!;
        /// <summary>Provides GraveSystem operations.</summary>
        private GraveSystem _graveSystem = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides MowerSystem operations.</summary>
        private MowerSystem _mowerSystem = null!;
        /// <summary>Provides PlantSystem operations.</summary>
        private PlantSystem _plantSystem = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides PurchaseSystem operations.</summary>
        private PurchaseSystem _purchaseSystem = null!;
        /// <summary>Provides RewardSystem operations.</summary>
        private RewardSystem _rewardSystem = null!;
        /// <summary>Provides SeedBankSystem operations.</summary>
        private SeedBankSystem _seedBankSystem = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides VaseSystem operations.</summary>
        private VaseSystem _vaseSystem = null!;
        /// <summary>Provides WaveSystem operations.</summary>
        private WaveSystem _waveSystem = null!;
        /// <summary>Provides WhackSystem operations.</summary>
        private WhackSystem _whackSystem = null!;
        /// <summary>Provides ZombieSystem operations.</summary>
        private ZombieSystem _zombieSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal AdventureModeRunner(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureSpecials">The AdventureSpecials collaborator.</param>
        /// <param name="battlePresentationSystem">The BattlePresentationSystem collaborator.</param>
        /// <param name="boardEnvironment">The BoardEnvironment collaborator.</param>
        /// <param name="bossSystem">The BossSystem collaborator.</param>
        /// <param name="coinSystem">The CoinSystem collaborator.</param>
        /// <param name="conveyorSystem">The ConveyorSystem collaborator.</param>
        /// <param name="economySystem">The EconomySystem collaborator.</param>
        /// <param name="graveSystem">The GraveSystem collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="mowerSystem">The MowerSystem collaborator.</param>
        /// <param name="plantSystem">The PlantSystem collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="purchaseSystem">The PurchaseSystem collaborator.</param>
        /// <param name="rewardSystem">The RewardSystem collaborator.</param>
        /// <param name="seedBankSystem">The SeedBankSystem collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="vaseSystem">The VaseSystem collaborator.</param>
        /// <param name="waveSystem">The WaveSystem collaborator.</param>
        /// <param name="whackSystem">The WhackSystem collaborator.</param>
        /// <param name="zombieSystem">The ZombieSystem collaborator.</param>
        internal void Connect(AdventureSpecials adventureSpecials, BattlePresentationSystem battlePresentationSystem, BoardEnvironment boardEnvironment, BossSystem bossSystem, CoinSystem coinSystem, ConveyorSystem conveyorSystem, EconomySystem economySystem, GraveSystem graveSystem, LegacySimulation legacySimulation, MowerSystem mowerSystem, PlantSystem plantSystem, PlantingSystem plantingSystem, ProjectileSystem projectileSystem, PurchaseSystem purchaseSystem, RewardSystem rewardSystem, SeedBankSystem seedBankSystem, SunSystem sunSystem, VaseSystem vaseSystem, WaveSystem waveSystem, WhackSystem whackSystem, ZombieSystem zombieSystem)
        {
            _adventureSpecials = adventureSpecials;
            _battlePresentationSystem = battlePresentationSystem;
            _boardEnvironment = boardEnvironment;
            _bossSystem = bossSystem;
            _coinSystem = coinSystem;
            _conveyorSystem = conveyorSystem;
            _economySystem = economySystem;
            _graveSystem = graveSystem;
            _legacySimulation = legacySimulation;
            _mowerSystem = mowerSystem;
            _plantSystem = plantSystem;
            _plantingSystem = plantingSystem;
            _projectileSystem = projectileSystem;
            _purchaseSystem = purchaseSystem;
            _rewardSystem = rewardSystem;
            _seedBankSystem = seedBankSystem;
            _sunSystem = sunSystem;
            _vaseSystem = vaseSystem;
            _waveSystem = waveSystem;
            _whackSystem = whackSystem;
            _zombieSystem = zombieSystem;
        }
        /// <summary>Gets the adventure board, which exists exactly when <see cref="IsAdventure"/> is true.</summary>
        /// <exception cref="InvalidOperationException">This simulation does not run an adventure level.</exception>
        internal AdventureBoard Adventure => _world._adventure ?? throw new InvalidOperationException("This simulation does not run an adventure level.");

        /// <summary>Performs initialize adventure.</summary>
        /// <param name = "initialized">The initialized.</param>
        internal void InitializeAdventure(ref bool initialized)
        {
            var level = AdventureLevels.ForRun(_world.Rules.AdventureLevel, _world.Rules.AdventureCycle, _world.Rules.AdventurePacketSlots);
            _world.RowCount = level.Rows;
            _world.SunAmount = level.StartingSun;
            var enabledZombieTypes = new bool[34];
            _world._adventure = new AdventureBoard(level)
            {
                Phase = level.ChoosesSeeds ? AdventurePhase.ChoosingSeeds : AdventurePhase.Playing,
                SelectedSeeds = level.DefaultSeeds,
                Waves = _world.Rules.DeferAdventureWaveInitialization ? Array.Empty<AdventureWave>()
                    : AdventureWavePlan.Build(level, _world._random, context: _world.Rules.AdventureWaveContext, enabledOutput: enabledZombieTypes),
                Tutorial = level.IsReplay ? AdventureTutorial.Complete : level.Number == 1 ? AdventureTutorial.ChooseSeed : level.Number == 2 ? AdventureTutorial.PlantSunflower : level.Number == 5 ? AdventureTutorial.Shovel : level.Number == 11 ? AdventureTutorial.NightMushrooms : level.Number == 21 ? AdventureTutorial.PoolSupport : level.Number == 31 ? AdventureTutorial.FogVisibility : level.Number == 41 ? AdventureTutorial.RoofPots : level.Mode == AdventureMode.Bowling ? AdventureTutorial.Bowling : level.Mode == AdventureMode.WhackAZombie ? AdventureTutorial.Hammer : level.Mode == AdventureMode.Vasebreaker ? AdventureTutorial.Vases : level.Mode == AdventureMode.Boss ? AdventureTutorial.Boss : level.HasConveyor ? AdventureTutorial.Conveyor : AdventureTutorial.Complete,
                WaveCountdown = !level.IsReplay && level.Number == 2 ? 50f : level.Mode == AdventureMode.Bowling || level.Mode == AdventureMode.WhackAZombie || level.Mode == AdventureMode.LittleTrouble || level.Mode == AdventureMode.Storm || level.Mode == AdventureMode.Bungee ? 2f : 18f,
                _nextPacket = 4f,
                _nextSkySun = _world.Rules.DeferAdventureWaveInitialization || level.IsNight ? 0f
                    : (425 + _world._random.Next(276)) * .01f,
                _nextHammerSun = 8f,
                BossHealth = level.Mode == AdventureMode.Boss ? 40000f : 0f,
                _bossTimer = 12f
            };
            Array.Copy(enabledZombieTypes, Adventure.EnabledZombieTypes, enabledZombieTypes.Length);
            Adventure.Fog = _world._boardCells;
            Adventure._whackSeedBankY = level.Mode == AdventureMode.WhackAZombie ? -87f : 0f;
            _seedBankSystem.InitializeAdventureSeedAccess();
            _seedBankSystem.InitializeReplaySeedChoices();
            Adventure._waveInitialCountdown = Adventure.WaveCountdown;
            _world.Mowers.Clear();
            for (var r = 0; r < _world.RowCount; r++)
            {
                _world.Mowers.Add(new Mower {
                    Row = r,
                    X = -0.5f,
                    Available = _world.Rules.MowersEnabled && level.IsLawnRow(r) && (!level.HasRoof || _world.Rules.AdventureRoofCleaners)
                });
            }

            if (_world.Rules.DeferAdventureWaveInitialization)
            {
                initialized = true;
                return;
            }

            if (level.HasRoof)
            {
                var columns = level.Number == 41 ? 5 : level.Number == 42 ? 4 : 3;
                for (var r = 0; r < _world.RowCount; r++)
                {
                    for (var c = 0; c < columns; c++)
                    {
                        _plantingSystem.PlaceAdventurePlant(PlantKind.FlowerPot, r, c);
                    }
                }
            }

            if (level.Number == 5 && !level.IsReplay)
            {
                for (var c = 3; c <= 5; c++)
                {
                    _plantingSystem.PlaceAdventurePlant(PlantKind.Peashooter, 2, c);
                }
            }

            if (level.Terrain == AdventureTerrain.Night && level.Mode != AdventureMode.Vasebreaker)
            {
                _graveSystem.InitializeAdventureGraves();
            }

            if (level.Mode == AdventureMode.Vasebreaker)
            {
                _vaseSystem.InitializeVaseStage();
            }

            if (level.Mode == AdventureMode.Bowling)
            {
                _vaseSystem.AddAdventurePacket(PlantKind.Wallnut);
            }

            if (level.Mode == AdventureMode.Boss)
            {
                _vaseSystem.AddAdventurePacket(PlantKind.CabbagePult);
                _vaseSystem.AddAdventurePacket(PlantKind.Jalapeno);
                _vaseSystem.AddAdventurePacket(PlantKind.CabbagePult);
                _vaseSystem.AddAdventurePacket(PlantKind.IceShroom);
                Adventure._nextPacket = 10f;
            }

            _purchaseSystem.InitializeAdventurePurchases();
            _waveSystem.InitializeAdventureWaveScheduler();
            initialized = true;
        }

        /// <summary>Performs choose adventure seeds.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <returns>The resulting choose adventure seeds.</returns>
        internal bool ChooseAdventureSeeds(IEnumerable<PlantKind>? seeds)
        {
            List<PlantKind>? selected;
            if (!TryPrepareAdventureSeeds(seeds, out selected))
            {
                return false;
            }

            Adventure.SelectedSeeds = selected.AsReadOnly();
            Adventure.Phase = AdventurePhase.Playing;
            return true;
        }

        /// <summary>Pure validation for hosts that must persist a selection before publishing gameplay.</summary>
        internal bool CanChooseAdventureSeeds(IEnumerable<PlantKind> seeds)
        {
            List<PlantKind>? selected;
            return TryPrepareAdventureSeeds(seeds, out selected);
        }

        /// <summary>Performs try prepare adventure seeds.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <param name = "selected">The selected.</param>
        /// <returns>The resulting try prepare adventure seeds.</returns>
        internal bool TryPrepareAdventureSeeds(IEnumerable<PlantKind>? seeds, [NotNullWhen(true)] out List<PlantKind>? selected)
        {
            selected = null;
            if (!_world.IsAdventure || _world.State != MatchState.Playing || Adventure.Phase != AdventurePhase.ChoosingSeeds || seeds == null)
            {
                return false;
            }

            var next = new List<PlantKind>();
            foreach (var kind in seeds)
            {
                if (next.Contains(kind) || !SeedBankSystem.ContainsSeed(Adventure.AvailableSeeds, kind) || kind == PlantKind.Imitater && !Adventure.ImitaterTarget.HasValue)
                {
                    return false;
                }

                next.Add(kind);
            }

            if (next.Count != Math.Min(Adventure.Level.PacketSlots, Adventure.AvailableSeeds.Count))
            {
                return false;
            }

            foreach (var kind in Adventure.RequiredSeeds)
            {
                if (!next.Contains(kind))
                {
                    return false;
                }
            }

            selected = next;
            return true;
        }

        /// <summary>Performs choose default adventure seeds.</summary>
        /// <returns>The resulting choose default adventure seeds.</returns>
        internal bool ChooseDefaultAdventureSeeds()
        {
            return ChooseAdventureSeeds(_world._adventure == null ? null : _world._adventure.SelectedSeeds);
        }

        /// <summary>Performs select adventure seed.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting select adventure seed.</returns>
        internal bool SelectAdventureSeed(PlantKind kind)
        {
            if (!_world.IsAdventure || _world.State != MatchState.Playing || Adventure.Phase != AdventurePhase.Playing || !SeedBankSystem.ContainsSeed(Adventure.SelectedSeeds, kind))
            {
                return false;
            }

            Adventure.SelectedSeed = kind;
            if (Adventure.Level.Number == 1 && Adventure.Tutorial == AdventureTutorial.ChooseSeed)
            {
                Adventure.Tutorial = AdventureTutorial.PlantPeashooter;
                Adventure.Notice(AdventureNoticeKind.Tutorial, (int)Adventure.Tutorial);
            }

            return true;
        }

        /// <summary>Performs acknowledge adventure tutorial.</summary>
        /// <returns>The resulting acknowledge adventure tutorial.</returns>
        internal bool AcknowledgeAdventureTutorial()
        {
            if (!_world.IsAdventure || _world.State != MatchState.Playing || Adventure.Tutorial == AdventureTutorial.Complete)
            {
                return false;
            }

            // Interactive first two tutorials complete through real actions.
            if (Adventure.Level.Number <= 2 || Adventure.Level.Number == 5)
            {
                return false;
            }

            Adventure.Tutorial = AdventureTutorial.Complete;
            return true;
        }

        /// <summary>Performs collect adventure sun.</summary>
        internal void CollectAdventureSun()
        {
            _seedBankSystem.SettleAdventureSunReservation();
            Adventure._collectedSun = true;
            if (Adventure.Level.Number == 1 && Adventure.Tutorial == AdventureTutorial.CollectSun)
            {
                Adventure.Tutorial = AdventureTutorial.Complete;
            }
        }

        /// <summary>Performs collect adventure reward.</summary>
        /// <returns>The resulting collect adventure reward.</returns>
        internal bool CollectAdventureReward()
        {
            if (!_world.IsAdventure || Adventure.Phase != AdventurePhase.Reward || _world.State != MatchState.Won)
            {
                return false;
            }

            Adventure.Phase = AdventurePhase.Finished;
            if (Adventure.RewardPresentation != null)
            {
                Adventure.RewardPresentation.BeginCollection();
                _legacySimulation.Raise(GameEvent.Won);
            }

            Adventure.Notice(AdventureNoticeKind.RewardCollected, Adventure.Level.Number);
            return true;
        }

        /// <summary>Performs advance adventure.</summary>
        /// <param name = "dt">The dt.</param>
        /// <param name = "handled">The handled.</param>
        internal void AdvanceAdventure(float dt, ref bool handled)
        {
            handled = true;
            _boardEnvironment.AdvanceAdventurePoolEffect();
            if (Adventure.Phase != AdventurePhase.Playing || Adventure.Level.Number == 5 && Adventure.Tutorial == AdventureTutorial.Shovel)
            {
                return;
            }

            _world.Elapsed += dt;
            _world._nativeFeedbackTick++;
            Adventure._ticks++;
            _battlePresentationSystem.AdventurePresentationTick(dt);
            _seedBankSystem.AdvanceAdventurePacketFeedback(dt);
            _seedBankSystem.AdvanceCooldowns(true);

            _boardEnvironment.AdvanceAdventureFog();
            BoardEnvironment.TickCellTimers(Adventure._craters, dt);
            _boardEnvironment.AdvanceAdventureIceTrails();
            _economySystem.AdvanceAdventureEconomy(dt);
            _conveyorSystem.AdvanceAdventureWhackSeedBank(dt);
            if (Adventure.Level.Mode == AdventureMode.Boss)
            {
                _bossSystem.AdvanceAdventureBoss(dt);
            }
            else if (Adventure.Level.Mode == AdventureMode.WhackAZombie)
            {
                _whackSystem.TickAdventureWhack(dt);
            }
            else if (Adventure.Level.Mode != AdventureMode.Vasebreaker)
            {
                _waveSystem.AdvanceScheduledAdventureWaves(dt);
            }

            _sunSystem.UpdateSuns(dt);
            _vaseSystem.AdvanceVasePackets();
            _plantSystem.AdvanceAdventurePlants(dt);
            _projectileSystem.AdvanceAdventureProjectiles(dt);
            _zombieSystem.AdvanceAdventureZombies(dt);
            _boardEnvironment.FinishAdventureIceTrapTick();
            _purchaseSystem.AdvanceAdventurePurchases(dt);
            if (_world.State != MatchState.Playing)
            {
                Adventure.Phase = AdventurePhase.Lost;
                _boardEnvironment.AdvanceAdventureStormTick();
                return;
            }

            _mowerSystem.AdvanceAdventureMowers(dt);
            _vaseSystem.AdvanceVaseMallet();
            _adventureSpecials.AdvanceAdventureSpecials(dt);
            _coinSystem.AdvanceAdventureUpgradeWorld(dt);
            _waveSystem.AdvanceAdventureProgressMeter();
            for (var i = _world.Flames.Count - 1; i >= 0; i--)
            {
                _world.Flames[i].Age += dt;
                if (_world.Flames[i].Age >= 1f)
                {
                    _projectileSystem.RemoveFlameAt(i);
                }
            }

            // PC Board::HasZombies (0x4148E0) excludes mind-controlled zombies.
            var cleared = _world.Zombies.TrueForAll(zombie => zombie.Hypnotized);
            if (Adventure.Level.Mode == AdventureMode.Boss)
            {
                cleared &= Adventure.BossHealth <= 0f && Adventure._balls.Count == 0;
            }
            else if (Adventure.Level.Mode == AdventureMode.Vasebreaker)
            {
                cleared &= Adventure.VaseStage == 2 && !Adventure.VaseInterludeActive && Adventure._vases.Count == 0;
            }
            else
            {
                cleared &= Adventure.CurrentWave >= Adventure.Waves.Count && Adventure._finalAmbush;
            }

            if (cleared)
            {
                // PC TrySpawnLevelAward (0x54BBC0) removes remaining allies without loot.
                _world.Zombies.Clear();
                _world.State = MatchState.Won;
                Adventure.Phase = AdventurePhase.Reward;
                if (_world.ModeSession == null)
                {
                    _rewardSystem.SpawnAdventureRewardPresentation();
                }

                Adventure.Notice(AdventureNoticeKind.RewardDropped, Adventure.Level.Number);
                if (_world.ModeSession != null)
                {
                    _legacySimulation.Raise(GameEvent.Won);
                }
            }

            _boardEnvironment.AdvanceAdventureStormTick();
        }
    }
}
