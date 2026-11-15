#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns IndependentModeRunner responsibilities for one deterministic match.</summary>
    internal sealed class IndependentModeRunner
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Reuses the native tick admission callback.</summary>
        private readonly Func<bool> _canAdvance;
        /// <summary>Reuses the native tick callback.</summary>
        private readonly Action<float> _advance;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides AdventureSpecials operations.</summary>
        private AdventureSpecials _adventureSpecials = null!;
        /// <summary>Provides AquariumMode operations.</summary>
        private AquariumMode _aquariumMode = null!;
        /// <summary>Provides BattlePresentationSystem operations.</summary>
        private BattlePresentationSystem _battlePresentationSystem = null!;
        /// <summary>Provides BoardEnvironment operations.</summary>
        private BoardEnvironment _boardEnvironment = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides BossSystem operations.</summary>
        private BossSystem _bossSystem = null!;
        /// <summary>Provides BowlingSystem operations.</summary>
        private BowlingSystem _bowlingSystem = null!;
        /// <summary>Provides CoinSystem operations.</summary>
        private CoinSystem _coinSystem = null!;
        /// <summary>Provides ConveyorSystem operations.</summary>
        private ConveyorSystem _conveyorSystem = null!;
        /// <summary>Provides EconomySystem operations.</summary>
        private EconomySystem _economySystem = null!;
        /// <summary>Provides GraveSystem operations.</summary>
        private GraveSystem _graveSystem = null!;
        /// <summary>Provides IZombieMode operations.</summary>
        private IZombieMode _iZombieMode = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides MatchMode operations.</summary>
        private MatchMode _matchMode = null!;
        /// <summary>Provides ModePlantActions operations.</summary>
        private ModePlantActions _modePlantActions = null!;
        /// <summary>Provides ModeZombieActions operations.</summary>
        private ModeZombieActions _modeZombieActions = null!;
        /// <summary>Provides MowerSystem operations.</summary>
        private MowerSystem _mowerSystem = null!;
        /// <summary>Provides PacketMode operations.</summary>
        private PacketMode _packetMode = null!;
        /// <summary>Provides PlantSystem operations.</summary>
        private PlantSystem _plantSystem = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides PortalMode operations.</summary>
        private PortalMode _portalMode = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides PurchaseSystem operations.</summary>
        private PurchaseSystem _purchaseSystem = null!;
        /// <summary>Provides RewardSystem operations.</summary>
        private RewardSystem _rewardSystem = null!;
        /// <summary>Provides SeedBankSystem operations.</summary>
        private SeedBankSystem _seedBankSystem = null!;
        /// <summary>Provides SlotMode operations.</summary>
        private SlotMode _slotMode = null!;
        /// <summary>Provides StarsMode operations.</summary>
        private StarsMode _starsMode = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides VaseMode operations.</summary>
        private VaseMode _vaseMode = null!;
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
        internal IndependentModeRunner(SimulationWorld world)
        {
            _world = world;
            _canAdvance = () => _world.State == MatchState.Playing && _world.ModeSession!.Phase == ModePhase.Playing;
            _advance = TickMode;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="adventureSpecials">The AdventureSpecials collaborator.</param>
        /// <param name="aquariumMode">The AquariumMode collaborator.</param>
        /// <param name="battlePresentationSystem">The BattlePresentationSystem collaborator.</param>
        /// <param name="boardEnvironment">The BoardEnvironment collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="bossSystem">The BossSystem collaborator.</param>
        /// <param name="bowlingSystem">The BowlingSystem collaborator.</param>
        /// <param name="coinSystem">The CoinSystem collaborator.</param>
        /// <param name="conveyorSystem">The ConveyorSystem collaborator.</param>
        /// <param name="economySystem">The EconomySystem collaborator.</param>
        /// <param name="graveSystem">The GraveSystem collaborator.</param>
        /// <param name="iZombieMode">The IZombieMode collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="matchMode">The MatchMode collaborator.</param>
        /// <param name="modePlantActions">The ModePlantActions collaborator.</param>
        /// <param name="modeZombieActions">The ModeZombieActions collaborator.</param>
        /// <param name="mowerSystem">The MowerSystem collaborator.</param>
        /// <param name="packetMode">The PacketMode collaborator.</param>
        /// <param name="plantSystem">The PlantSystem collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="portalMode">The PortalMode collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="purchaseSystem">The PurchaseSystem collaborator.</param>
        /// <param name="rewardSystem">The RewardSystem collaborator.</param>
        /// <param name="seedBankSystem">The SeedBankSystem collaborator.</param>
        /// <param name="slotMode">The SlotMode collaborator.</param>
        /// <param name="starsMode">The StarsMode collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="vaseMode">The VaseMode collaborator.</param>
        /// <param name="vaseSystem">The VaseSystem collaborator.</param>
        /// <param name="waveSystem">The WaveSystem collaborator.</param>
        /// <param name="whackSystem">The WhackSystem collaborator.</param>
        /// <param name="zombieSystem">The ZombieSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, AdventureSpecials adventureSpecials, AquariumMode aquariumMode, BattlePresentationSystem battlePresentationSystem, BoardEnvironment boardEnvironment, BoardQueries boardQueries, BossSystem bossSystem, BowlingSystem bowlingSystem, CoinSystem coinSystem, ConveyorSystem conveyorSystem, EconomySystem economySystem, GraveSystem graveSystem, IZombieMode iZombieMode, LegacySimulation legacySimulation, MatchMode matchMode, ModePlantActions modePlantActions, ModeZombieActions modeZombieActions, MowerSystem mowerSystem, PacketMode packetMode, PlantSystem plantSystem, PlantingSystem plantingSystem, PortalMode portalMode, ProjectileSystem projectileSystem, PurchaseSystem purchaseSystem, RewardSystem rewardSystem, SeedBankSystem seedBankSystem, SlotMode slotMode, StarsMode starsMode, SunSystem sunSystem, VaseMode vaseMode, VaseSystem vaseSystem, WaveSystem waveSystem, WhackSystem whackSystem, ZombieSystem zombieSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _adventureSpecials = adventureSpecials;
            _aquariumMode = aquariumMode;
            _battlePresentationSystem = battlePresentationSystem;
            _boardEnvironment = boardEnvironment;
            _boardQueries = boardQueries;
            _bossSystem = bossSystem;
            _bowlingSystem = bowlingSystem;
            _coinSystem = coinSystem;
            _conveyorSystem = conveyorSystem;
            _economySystem = economySystem;
            _graveSystem = graveSystem;
            _iZombieMode = iZombieMode;
            _legacySimulation = legacySimulation;
            _matchMode = matchMode;
            _modePlantActions = modePlantActions;
            _modeZombieActions = modeZombieActions;
            _mowerSystem = mowerSystem;
            _packetMode = packetMode;
            _plantSystem = plantSystem;
            _plantingSystem = plantingSystem;
            _portalMode = portalMode;
            _projectileSystem = projectileSystem;
            _purchaseSystem = purchaseSystem;
            _rewardSystem = rewardSystem;
            _seedBankSystem = seedBankSystem;
            _slotMode = slotMode;
            _starsMode = starsMode;
            _sunSystem = sunSystem;
            _vaseMode = vaseMode;
            _vaseSystem = vaseSystem;
            _waveSystem = waveSystem;
            _whackSystem = whackSystem;
            _zombieSystem = zombieSystem;
        }

        /// <summary>Gets the mode session, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        internal ModeSession RequiredModeSession => _world.ModeSession ?? throw new System.InvalidOperationException("The member 'ModeSession' is not available.");
        /// <summary>Gets or sets is independent mode.</summary>
        internal bool IsIndependentMode
        {
            get
            {
                return _world.ModeSession != null;
            }
        }

        /// <summary>Gets or sets mode simulation remainder.</summary>
        internal float ModeSimulationRemainder
        {
            get
            {
                return (float)Math.Max(0d, Math.Min(SimulationWorld.SimulationStep, _world._modeAccumulated));
            }
        }

        /// <summary>Gets or sets mode needs final ambush.</summary>
        internal bool ModeNeedsFinalAmbush
        {
            get
            {
                var m = RequiredModeSession.Definition.Mechanic;
                return m != ModeMechanic.PeaHeads && m != ModeMechanic.Stars && m != ModeMechanic.Slots && m != ModeMechanic.Beghouled && m != ModeMechanic.BeghouledTwist && m != ModeMechanic.Whack && m != ModeMechanic.Vases && m != ModeMechanic.IZombie && m != ModeMechanic.Boss && m != ModeMechanic.Aquarium;
            }
        }

        /// <summary>Performs configure mode.</summary>
        /// <param name = "session">The session.</param>
        /// <param name = "access">The access.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void ConfigureMode(ModeSession session, AdventureSeedAccess? access)
        {
            if (!_world.IsAdventure || session == null)
            {
                throw new InvalidOperationException("Modes require a prepared battle board.");
            }

            _world.ModeSession = session;
            var a = _adventureModeRunner.Adventure;
            var d = session.Definition;
            if (!_world._boardCells.LooksInitialized)
            {
                // PC 413BDE consumes a board-seed draw before cell initialization for survival modes 1..15.
                if (d.Id >= 1 && d.Id <= 15)
                {
                    _world._random.Next();
                }

                _world._boardCells.InitializeLooks(_world._random);
            }

            _world.Rules.AdventureDisableLoot = d.Mechanic == ModeMechanic.IZombie;
            _world.Plants.Clear();
            _world.Zombies.Clear();
            _projectileSystem.ClearPeas();
            _sunSystem.ClearSuns();
            _projectileSystem.ClearFlames();
            _world._adventureSunReservation = 0;
            a.ConveyorTicks = 0;
            a._packets.Clear();
            a._vases.Clear();
            a._graves.Clear();
            a._craters.Clear();
            a._ice.Clear();
            a._nuts.Clear();
            a._coins.Clear();
            a._cobShells.Clear();
            a.Tutorial = AdventureTutorial.Complete;
            a.Phase = AdventurePhase.Playing;
            a.CurrentWave = 0;
            a._finalAmbush = false;
            a.VaseStage = 2;
            a._nextSkySun = a.Level.IsNight ? 0f : (425 + _world._random.Next(276)) * .01f;
            a._nextPacket = 4f;
            a._nextHammerSun = 8f;
            a.WaveCountdown = d.ChoosesSeeds ? 18f : 2f;
            var available = new List<PlantKind>();
            for (var id = 0; id < session.UnlockedBasicSeeds; id++)
            {
                available.Add((PlantKind)id);
            }

            if (access != null)
            {
                foreach (var kind in access.AdditionalSeeds)
                {
                    available.Add(kind);
                }

                a.ImitaterTarget = access.ImitaterTarget;
            }

            if (d.Mechanic == ModeMechanic.LastStand)
            {
                available.RemoveAll(k => k == PlantKind.Sunflower || k == PlantKind.SunShroom || k == PlantKind.TwinSunflower);
            }

            a.AvailableSeeds = available.AsReadOnly();
            var defaults = new List<PlantKind>
            {
                PlantKind.Sunflower,
                PlantKind.Peashooter,
                PlantKind.Wallnut,
                PlantKind.PotatoMine,
                PlantKind.CherryBomb,
                PlantKind.SnowPea
            };
            if (a.Level.IsNight)
            {
                defaults = new List<PlantKind>
                {
                    PlantKind.SunShroom,
                    PlantKind.PuffShroom,
                    PlantKind.FumeShroom,
                    PlantKind.GraveBuster,
                    PlantKind.HypnoShroom,
                    PlantKind.IceShroom
                };
            }

            if (a.Level.HasPool)
            {
                defaults[2] = PlantKind.LilyPad;
                defaults[3] = PlantKind.TangleKelp;
            }

            if (a.Level.HasRoof)
            {
                defaults = new List<PlantKind>
                {
                    PlantKind.Sunflower,
                    PlantKind.FlowerPot,
                    PlantKind.CabbagePult,
                    PlantKind.KernelPult,
                    PlantKind.Wallnut,
                    PlantKind.CherryBomb
                };
            }

            if (d.Mechanic == ModeMechanic.Stars)
            {
                defaults[1] = PlantKind.Starfruit;
            }

            if (d.Mechanic == ModeMechanic.LastStand)
            {
                defaults = new List<PlantKind>
                {
                    PlantKind.LilyPad,
                    PlantKind.Repeater,
                    PlantKind.Torchwood,
                    PlantKind.TallNut,
                    PlantKind.Pumpkin,
                    PlantKind.Cattail
                };
            }

            defaults.RemoveAll(k => !available.Contains(k));
            foreach (var k in available)
            {
                if (defaults.Count < session.PacketSlots && !defaults.Contains(k))
                {
                    defaults.Add(k);
                }
            }

            a.SelectedSeeds = defaults.AsReadOnly();
            if (d.Mechanic == ModeMechanic.Whack)
            {
                a.SelectedSeeds = Array.AsReadOnly(new[] { PlantKind.PotatoMine, PlantKind.GraveBuster, PlantKind.IceShroom });
            }

            _world.SunAmount = d.Mechanic == ModeMechanic.LastStand ? 5000 : d.Mechanic == ModeMechanic.IZombie ? 150 : d.Puzzle || d.Mechanic == ModeMechanic.Whack || IsModeMatch ? 0 : 50;
            for (var r = 0; r < _world.RowCount; r++)
            {
                _world.Mowers[r].Available = !d.Puzzle && d.Mechanic != ModeMechanic.Aquarium && (!a.Level.HasRoof || _world.Rules.AdventureRoofCleaners);
            }

            if (a.Level.HasRoof)
            {
                for (var r = 0; r < _world.RowCount; r++)
                {
                    for (var c = 0; c < 3; c++)
                    {
                        _plantingSystem.PlaceAdventurePlant(PlantKind.FlowerPot, r, c);
                    }
                }
            }

            if (a.Level.Terrain == AdventureTerrain.Night && d.Mechanic != ModeMechanic.Vases && d.Mechanic != ModeMechanic.IZombie && d.Mechanic != ModeMechanic.Beghouled && d.Mechanic != ModeMechanic.BeghouledTwist && d.Mechanic != ModeMechanic.Portals)
            {
                _graveSystem.InitializeAdventureGraves();
            }

            if (d.Mechanic == ModeMechanic.Vases)
            {
                _vaseMode.InitializeModeVases();
            }
            else if (d.Mechanic == ModeMechanic.IZombie)
            {
                _iZombieMode.InitializeModeIZombie();
            }
            else if (IsModeMatch)
            {
                _matchMode.InitializeModeMatch();
            }
            else if (d.Mechanic == ModeMechanic.Aquarium)
            {
                _world.SunAmount = 50;
                _aquariumMode.AddModeFish();
                _aquariumMode.AddModeFish();
            }
            else if (d.Mechanic == ModeMechanic.Portals)
            {
                _portalMode.InitializeModePortals();
            }
            else if (d.Mechanic == ModeMechanic.Bobsled)
            {
                for (var r = 0; r < _world.RowCount; r++)
                {
                    if (!a.Level.IsWaterRow(r))
                    {
                        a._ice.Add(new AdventureIceTrail
                        {
                            Id = ++_world.Presentation._effectId, Row = r, MinX = 400, RemainingTicks = int.MaxValue
                        });
                    }
                }
            }

            _packetMode.InitializeModePackets();
            a.Waves = ModeWavePlan.Build(session, _world._random);
            if (d.Mechanic == ModeMechanic.Boss)
            {
                a.BossMaxHealth = a.BossHealth = 60000f;
                _bossSystem.InitializeAdventureBoss();
            }
            _waveSystem.ResetAdventureWaveSchedule((int)Math.Round(a.WaveCountdown * 100), ModeNeedsFinalAmbush);
            if (d.Mechanic == ModeMechanic.Whack)
            {
                a._finalAmbush = false;
            }

            session.Phase = d.ChoosesSeeds ? ModePhase.ChoosingSeeds : ModePhase.Playing;
            if (d.ChoosesSeeds)
            {
                a.Phase = AdventurePhase.ChoosingSeeds;
            }

            session.Advice = ModeInstruction();
        }

        /// <summary>Gets or sets is mode match.</summary>
        internal bool IsModeMatch
        {
            get
            {
                return RequiredModeSession.Definition.Mechanic == ModeMechanic.Beghouled || RequiredModeSession.Definition.Mechanic == ModeMechanic.BeghouledTwist;
            }
        }

        /// <summary>Performs choose mode seeds.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <returns>The resulting choose mode seeds.</returns>
        internal bool ChooseModeSeeds(IEnumerable<PlantKind> seeds)
        {
            if (_world.ModeSession == null || _world.ModeSession.Phase != ModePhase.ChoosingSeeds || seeds == null)
            {
                return false;
            }

            var next = new List<PlantKind>();
            foreach (var kind in seeds)
            {
                if (next.Contains(kind) || !SeedBankSystem.ContainsSeed(_adventureModeRunner.Adventure.AvailableSeeds, kind) || kind == PlantKind.Imitater && !_adventureModeRunner.Adventure.ImitaterTarget.HasValue)
                {
                    return false;
                }

                next.Add(kind);
            }

            if (next.Count != Math.Min(_world.ModeSession.PacketSlots, _adventureModeRunner.Adventure.AvailableSeeds.Count))
            {
                return false;
            }

            _adventureModeRunner.Adventure.SelectedSeeds = next.AsReadOnly();
            _adventureModeRunner.Adventure.Phase = AdventurePhase.Playing;
            _world.ModeSession.Phase = _world.ModeSession.Definition.Mechanic == ModeMechanic.LastStand ? ModePhase.Preparing : ModePhase.Playing;
            return true;
        }

        /// <summary>Performs apply mode purchases.</summary>
        /// <param name = "firstAid">The first aid.</param>
        /// <param name = "placeRake">The place rake.</param>
        internal void ApplyModePurchases(bool firstAid, Func<bool>? placeRake)
        {
            if (_world.ModeSession == null)
            {
                return;
            }

            _world.Rules.AdventureFirstAid = firstAid;
            _world.Rules.AdventurePlaceRake = _world.ModeSession.Definition.SupportsRake ? placeRake : null;
            if (_adventureModeRunner.Adventure.RakeRow < 0)
            {
                _purchaseSystem.InitializeAdventurePurchases();
            }
        }

        /// <summary>Performs set mode imitater target.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting set mode imitater target.</returns>
        internal bool SetModeImitaterTarget(PlantKind kind)
        {
            if (_world.ModeSession == null || _world.ModeSession.Phase != ModePhase.ChoosingSeeds || (int)kind < 0 || (int)kind > 39 || !SeedBankSystem.ContainsSeed(_adventureModeRunner.Adventure.AvailableSeeds, PlantKind.Imitater))
            {
                return false;
            }

            _adventureModeRunner.Adventure.ImitaterTarget = kind;
            return true;
        }

        /// <summary>Performs plant mode vase cherry.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting plant mode vase cherry.</returns>
        internal bool PlantModeVaseCherry(int row, int column)
        {
            if (_world.ModeSession == null || _world.ModeSession.Definition.Mechanic != ModeMechanic.Vases || _world.ModeSession.Phase != ModePhase.Playing)
            {
                return false;
            }

            _world.LastAdventurePlantingFailure = _plantingSystem.CheckAdventurePlanting(PlantKind.CherryBomb, row, column, false);
            if (_world.LastAdventurePlantingFailure != AdventurePlantingFailure.None)
            {
                return false;
            }

            _seedBankSystem.SpendAdventureSun(LegacySimulation.Cost(PlantKind.CherryBomb));
            _world._seedCooldownTicks[(int)PlantKind.CherryBomb] = LegacySimulation.DefinitionFor(PlantKind.CherryBomb).RechargeThresholdRaw + 1;
            _plantingSystem.PlaceAdventurePlantWithEffects(PlantKind.CherryBomb, row, column);
            return true;
        }

        /// <summary>Performs begin mode defense.</summary>
        /// <returns>The resulting begin mode defense.</returns>
        internal bool BeginModeDefense()
        {
            if (_world.ModeSession == null || _world.ModeSession.Phase != ModePhase.Preparing)
            {
                return false;
            }

            _world.ModeSession.Phase = ModePhase.Playing;
            _adventureModeRunner.Adventure.WaveCountdown = 3f;
            _waveSystem.ResetAdventureWaveSchedule(300, ModeNeedsFinalAmbush);
            return true;
        }

        /// <summary>Performs update mode.</summary>
        /// <param name = "dt">The dt.</param>
        internal void UpdateMode(float dt)
        {
            if (_world.ModeSession == null || dt < 0f || float.IsNaN(dt) || float.IsInfinity(dt))
            {
                return;
            }

            if (_world.State == MatchState.Won)
            {
                _rewardSystem.AdvanceAdventureRewardPresentation(dt);
                return;
            }

            if (_world.State != MatchState.Playing || _world.ModeSession.Phase != ModePhase.Playing)
            {
                var repick = _world.ModeSession.Definition.Survival && _world.ModeSession.Stage > 1 && _world.ModeSession.Phase == ModePhase.ChoosingSeeds;
                _boardEnvironment.AdvanceAdventurePoolPresentation(dt, repick);
                return;
            }

            FixedStepClock.Advance(ref _world._modeAccumulated, dt, _canAdvance, _advance);
        }

        /// <summary>Performs tick mode.</summary>
        /// <param name = "dt">The dt.</param>
        internal void TickMode(float dt)
        {
            _boardEnvironment.AdvanceAdventurePoolEffect();
            var s = RequiredModeSession;
            var d = s.Definition;
            _world.Elapsed += dt;
            _world._nativeFeedbackTick++;
            _adventureModeRunner.Adventure._ticks++;
            _battlePresentationSystem.AdventurePresentationTick(dt);
            _seedBankSystem.AdvanceAdventurePacketFeedback(dt);
            _seedBankSystem.AdvanceCooldowns(true);

            _boardEnvironment.AdvanceAdventureFog();
            BoardEnvironment.TickCellTimers(_adventureModeRunner.Adventure._craters, dt);
            _boardEnvironment.AdvanceAdventureIceTrails();
            if (d.Mechanic == ModeMechanic.Aquarium)
            {
                _aquariumMode.TickModeAquarium(dt);
                _sunSystem.UpdateSuns(dt);
                return;
            }

            if (!d.Puzzle && d.Mechanic != ModeMechanic.LastStand && (!d.UsesPackets || d.Mechanic == ModeMechanic.Slots) && !IsModeMatch)
            {
                _economySystem.AdvanceAdventureEconomy(dt);
            }

            if (d.Mechanic == ModeMechanic.Slots)
            {
                _slotMode.TickModeSlot(dt);
            }

            if (d.UsesPackets)
            {
                _packetMode.TickModePackets(dt);
            }

            if (d.Mechanic == ModeMechanic.Boss)
            {
                _bossSystem.AdvanceAdventureBoss(dt);
            }
            else if (!d.Puzzle)
            {
                if ((d.Mechanic == ModeMechanic.Stars || d.Mechanic == ModeMechanic.Slots || IsModeMatch) && _adventureModeRunner.Adventure.CurrentWave >= _adventureModeRunner.Adventure.Waves.Count && _adventureModeRunner.Adventure.Waves.Count > 0)
                {
                    if (s._continuousWaveTimer < 0)
                    {
                        s._continuousWaveTimer = 25f;
                    }

                    s._continuousWaveTimer -= dt;
                    float health = 0;
                    foreach (var z in _world.Zombies)
                    {
                        if (z.Wave == _adventureModeRunner.Adventure.Waves.Count - 1 && !z.Hypnotized && !z.Headless)
                        {
                            health += z.Health + z.ArmorHealth + z.ShieldHealth * 0.2f;
                        }
                    }

                    if (s._continuousWaveTimer < 21f && _world.WaveSchedule != null && health <= _world.WaveSchedule.HealthToNextWave)
                    {
                        s._continuousWaveTimer = Math.Min(2f, s._continuousWaveTimer);
                    }

                    if (s._continuousWaveTimer <= 0)
                    {
                        var last = _adventureModeRunner.Adventure.Waves.Count - 1;
                        var repeats = new List<ZombieKind>();
                        foreach (var z in _adventureModeRunner.Adventure.Waves[last].Zombies)
                        {
                            repeats.Add(z == ZombieKind.Flag ? ZombieKind.Normal : z);
                        }

                        var waves = new List<AdventureWave>(_adventureModeRunner.Adventure.Waves);
                        waves[last] = new AdventureWave
                        {
                            Index = last,
                            IsFlag = false,
                            Zombies = repeats.AsReadOnly()
                        };
                        _adventureModeRunner.Adventure.Waves = waves.AsReadOnly();
                        _adventureModeRunner.Adventure.CurrentWave = last;
                        s._continuousWaveTimer = -1f;
                        _waveSystem.ResetAdventureWaveSchedule(1, false);
                    }
                }

                if (d.Mechanic == ModeMechanic.Whack)
                {
                    _whackSystem.TickModeWhack(dt);
                }
                else
                {
                    _waveSystem.AdvanceScheduledAdventureWaves(dt);
                }
            }

            if (d.Mechanic == ModeMechanic.IZombie)
            {
                _iZombieMode.BeforeModeIZombie();
            }

            _modePlantActions.TickModeBackwardPlants(dt);
            _sunSystem.UpdateSuns(dt);
            _vaseSystem.AdvanceVasePackets();
            _plantSystem.AdvanceAdventurePlants(dt);
            _projectileSystem.AdvanceAdventureProjectiles(dt);
            _zombieSystem.AdvanceAdventureZombies(dt);
            _boardEnvironment.FinishAdventureIceTrapTick();
            if (_world.State == MatchState.Lost)
            {
                s.Phase = ModePhase.Lost;
                return;
            }

            _mowerSystem.AdvanceAdventureMowers(dt);
            _purchaseSystem.AdvanceAdventurePurchases(dt);
            _vaseSystem.AdvanceVaseMallet();
            if (d.Mechanic == ModeMechanic.Bowling)
            {
                _bowlingSystem.TickModeBowling(dt);
            }
            else if (d.Mechanic != ModeMechanic.Vases)
            {
                _adventureSpecials.AdvanceAdventureSpecials(dt);
            }

            _coinSystem.AdvanceAdventureUpgradeWorld(dt);
            _modeZombieActions.TickModeSpecialZombies(dt);
            if (d.Mechanic == ModeMechanic.Portals)
            {
                _portalMode.TickModePortals(dt);
            }

            if (d.Mechanic == ModeMechanic.IZombie)
            {
                _iZombieMode.AfterModeIZombie(dt);
            }

            if (IsModeMatch)
            {
                _matchMode.TickModeMatch(dt);
            }

            for (var i = _world.Flames.Count - 1; i >= 0; i--)
            {
                _world.Flames[i].Age += dt;
                if (_world.Flames[i].Age >= 1f)
                {
                    _projectileSystem.RemoveFlameAt(i);
                }
            }

            var cleared = false;
            if (d.Mechanic == ModeMechanic.Vases)
            {
                cleared = _adventureModeRunner.Adventure._vases.Count == 0 && _world.Zombies.TrueForAll(z => z.Hypnotized);
            }
            else if (d.Mechanic == ModeMechanic.IZombie)
            {
                cleared = true;
                foreach (bool brain in s.Brains)
                {
                    if (!brain)
                    {
                        cleared = false;
                    }
                }
            }
            else if (d.Mechanic == ModeMechanic.Stars)
            {
                cleared = _starsMode.ModeStarCount() == 14;
            }
            else if (d.Mechanic == ModeMechanic.Slots)
            {
                cleared = _world.SunAmount >= 2000;
            }
            else if (IsModeMatch)
            {
                cleared = s.Score >= 75;
            }
            else if (d.Mechanic == ModeMechanic.Boss)
            {
                cleared = _adventureModeRunner.Adventure.Boss?.DeathComplete == true && _world.Zombies.TrueForAll(zombie => zombie.Hypnotized) && _adventureModeRunner.Adventure._balls.Count == 0;
            }
            else
            {
                cleared = _adventureModeRunner.Adventure.CurrentWave >= _adventureModeRunner.Adventure.Waves.Count && _adventureModeRunner.Adventure._finalAmbush && _world.Zombies.TrueForAll(zombie => zombie.Hypnotized);
            }

            if (cleared)
            {
                ClearModeStage();
            }
        }

        /// <summary>Performs clear mode stage.</summary>
        internal void ClearModeStage()
        {
            var s = RequiredModeSession;
            if (s.Phase != ModePhase.Playing)
            {
                return;
            }

            s.ClearedStages = s.Stage;
            if (s.Definition.Survival)
            {
                s.Score = s.FlagsCompleted;
            }
            else if (s.Definition.Endless)
            {
                s.Score = s.ClearedStages;
            }
            else
            {
                s.Score = Math.Max(1, s.Score);
            }

            var next = s.Definition.Endless || s.Stage < s.Definition.Stages;
            // PC 0x41FAB0 preserves allies only while survival is waiting for another seed selection.
            if (!s.Definition.Survival || !next)
            {
                _world.Zombies.Clear();
            }

            s.Phase = next ? ModePhase.StageCleared : ModePhase.Won;
            _adventureModeRunner.Adventure.Phase = AdventurePhase.Reward;
            _world.State = MatchState.Won;
            if (!s.Definition.Survival || !next)
            {
                _legacySimulation.Raise(GameEvent.Won);
            }
        }

        /// <summary>Performs continue mode stage.</summary>
        /// <returns>The resulting continue mode stage.</returns>
        internal bool ContinueModeStage()
        {
            var s = _world.ModeSession;
            if (s == null || s.Phase != ModePhase.StageCleared)
            {
                return false;
            }

            s.Stage++;
            s.SelectedCell = -1;
            s._failureDelay = 0;
            _world.State = MatchState.Playing;
            _adventureModeRunner.Adventure.Phase = AdventurePhase.Playing;
            _world.Zombies.Clear();
            _projectileSystem.ClearPeas();
            _projectileSystem.ClearFlames();
            _adventureModeRunner.Adventure._nuts.Clear();
            _adventureModeRunner.Adventure._balls.Clear();
            _adventureModeRunner.Adventure._packets.Clear();
            s._backwardPackets.Clear();
            s._giantPackets.Clear();
            s._giantNuts.Clear();
            s.RainPackets.Clear();
            s.HeadShots.Clear();
            _world._modeAccumulated = 0;
            _world._completedBoardAccumulated = 0;
            _adventureModeRunner.Adventure.CurrentWave = 0;
            _adventureModeRunner.Adventure.TotalWavesSpawned = 0;
            _adventureModeRunner.Adventure.FlagsSpawned = 0;
            _adventureModeRunner.Adventure._finalAmbush = false;
            _adventureModeRunner.Adventure._warned = false;
            _adventureModeRunner.Adventure._previousWaveHealth = 0;
            _adventureModeRunner.Adventure.WaveCountdown = s.Definition.Survival ? 6f : 18f;
            _adventureModeRunner.Adventure.RowPicker.ResetMowedWaves();
            if (s.Definition.Mechanic == ModeMechanic.Vases)
            {
                _world.Plants.Clear();
                _sunSystem.ClearSuns();
                _vaseSystem.StartVaseScreenFlash();
                _vaseMode.InitializeModeVases();
                s.Phase = ModePhase.Playing;
            }
            else if (s.Definition.Mechanic == ModeMechanic.IZombie)
            {
                _world.Plants.Clear();
                _iZombieMode.InitializeModeIZombie();
                s.Phase = ModePhase.Playing;
            }
            else if (s.Definition.Mechanic == ModeMechanic.LastStand)
            {
                _world.SunAmount += 250;
                s.Phase = ModePhase.Preparing;
            }
            else
            {
                s.Phase = ModePhase.ChoosingSeeds;
                _adventureModeRunner.Adventure.Phase = AdventurePhase.ChoosingSeeds;
            }

            _adventureModeRunner.Adventure.Waves = ModeWavePlan.Build(s, _world._random);
            _waveSystem.ResetAdventureWaveSchedule((int)Math.Round(_adventureModeRunner.Adventure.WaveCountdown * 100), ModeNeedsFinalAmbush);
            s._continuousWaveTimer = -1f;
            s.Advice = ModeInstruction();
            return true;
        }

        /// <summary>Performs try plant mode.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try plant mode.</returns>
        internal bool TryPlantMode(PlantKind kind, int row, int column)
        {
            var s = _world.ModeSession;
            if (s == null || _world.State != MatchState.Playing || (s.Phase != ModePhase.Playing && s.Phase != ModePhase.Preparing) || s.Definition.Puzzle || IsModeMatch || s.Definition.Mechanic == ModeMechanic.Aquarium)
            {
                return false;
            }

            if (s.Definition.UsesPackets)
            {
                return false;
            }

            if (s.Definition.Mechanic == ModeMechanic.Stars && StarsMode.IsModeStarCell(row, column) && kind != PlantKind.Starfruit && kind != PlantKind.Pumpkin)
            {
                return false;
            }

            return _legacySimulation.TryPlant(kind, row, column);
        }

        /// <summary>Performs try use mode packet.</summary>
        /// <param name = "id">The id.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try use mode packet.</returns>
        internal bool TryUseModePacket(int id, int row, int column)
        {
            var s = _world.ModeSession;
            if (s == null || s.Phase != ModePhase.Playing || _world.State != MatchState.Playing)
            {
                return false;
            }

            var p = _adventureModeRunner.Adventure._packets.Find(x => x.Id == id);
            if (p == null)
            {
                return false;
            }

            if (s.Definition.Mechanic == ModeMechanic.Columns)
            {
                var placed = false;
                for (var r = 0; r < _world.RowCount; r++)
                {
                    if (_plantingSystem.CheckAdventurePlanting(p.Kind, r, column, true) == AdventurePlantingFailure.None)
                    {
                        _plantingSystem.PlaceAdventurePlantWithEffects(p.Kind, r, column);
                        placed = true;
                    }
                }

                if (placed)
                {
                    _conveyorSystem.RemoveAdventurePacket(p);
                }

                return placed;
            }

            var giant = s._giantPackets.Contains(id);
            if (!_plantingSystem.TryUseAdventurePacket(id, row, column))
            {
                return false;
            }

            if (giant)
            {
                s._giantPackets.Remove(id);
                if (_adventureModeRunner.Adventure._nuts.Count > 0)
                {
                    s._giantNuts.Add(_adventureModeRunner.Adventure._nuts[_adventureModeRunner.Adventure._nuts.Count - 1]);
                }
            }

            if (s._backwardPackets.Remove(id))
            {
                var plant = _boardQueries.AdventurePlantAt(row, column, PlantLayer.Main);
                if (plant != null)
                {
                    plant.ModeFacesLeft = true;
                }
            }

            return true;
        }

        /// <summary>Selects the original introductory advice for the current independent mode.</summary>
        /// <returns>A bracketed original LawnStrings key, or an empty string when no original advice is displayed.</returns>
        internal string ModeInstruction()
        {
            var d = RequiredModeSession.Definition;
            if (d.Survival)
            {
                // Original Challenge::InitLevel shows survival advice only before the first stage.
                return RequiredModeSession.Stage == 1 ? d.Endless ? "[ADVICE_SURVIVE_ENDLESS]" : "[ADVICE_SURVIVE_FLAGS]" : "";
            }

            switch (d.Mechanic)
            {
                case ModeMechanic.Vases:
                    // ScaryPotterStart's shovel tutorial belongs only to adventure, not standalone puzzles.
                    return "";
                case ModeMechanic.IZombie:
                    return "[ADVICE_I_ZOMBIE_EAT_ALL_BRAINS]";
                case ModeMechanic.Beghouled:
                    return "[ADVICE_BEGHOULED_DRAG_TO_MATCH_3]";
                case ModeMechanic.BeghouledTwist:
                    return "[ADVICE_BEGHOULED_TWIST_TO_MATCH_3]";
                case ModeMechanic.Stars:
                    return "[ADVICE_FILL_IN_STARFRUIT]";
                case ModeMechanic.Aquarium:
                    return "[ADVICE_ZOMBIQUARIUM_CLICK_TO_FEED]";
                case ModeMechanic.LastStand:
                    return RequiredModeSession.Stage == 1 ? "[ADVICE_SURVIVE_FLAGS]" : "";
                case ModeMechanic.Slots:
                    return "[ADVICE_SLOT_MACHINE_PULL]";
                default:
                    return "";
            }
        }
    }
}
