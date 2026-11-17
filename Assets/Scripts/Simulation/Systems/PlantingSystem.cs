#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns PlantingSystem responsibilities for one deterministic match.</summary>
    internal sealed class PlantingSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BoardEnvironment operations.</summary>
        private BoardEnvironment _boardEnvironment = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides ConveyorSystem operations.</summary>
        private ConveyorSystem _conveyorSystem = null!;
        /// <summary>Provides LadderSystem operations.</summary>
        private LadderSystem _ladderSystem = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides PlantTimingSystem operations.</summary>
        private PlantTimingSystem _plantTimingSystem = null!;
        /// <summary>Provides PurchaseSystem operations.</summary>
        private PurchaseSystem _purchaseSystem = null!;
        /// <summary>Provides SeedBankSystem operations.</summary>
        private SeedBankSystem _seedBankSystem = null!;
        /// <summary>Provides SpecialPlantActions operations.</summary>
        private SpecialPlantActions _specialPlantActions = null!;
        /// <summary>Provides UpgradePlantActions operations.</summary>
        private UpgradePlantActions _upgradePlantActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal PlantingSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="boardEnvironment">The BoardEnvironment collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="conveyorSystem">The ConveyorSystem collaborator.</param>
        /// <param name="ladderSystem">The LadderSystem collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="plantTimingSystem">The PlantTimingSystem collaborator.</param>
        /// <param name="purchaseSystem">The PurchaseSystem collaborator.</param>
        /// <param name="seedBankSystem">The SeedBankSystem collaborator.</param>
        /// <param name="specialPlantActions">The SpecialPlantActions collaborator.</param>
        /// <param name="upgradePlantActions">The UpgradePlantActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        /// <param name="zombieLifecycle">The ZombieLifecycle collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, BoardEnvironment boardEnvironment, BoardQueries boardQueries, ConveyorSystem conveyorSystem, LadderSystem ladderSystem, LegacySimulation legacySimulation, PlantTimingSystem plantTimingSystem, PurchaseSystem purchaseSystem, SeedBankSystem seedBankSystem, SpecialPlantActions specialPlantActions, UpgradePlantActions upgradePlantActions, ZombieFeedbackSystem zombieFeedbackSystem, ZombieLifecycle zombieLifecycle)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _boardEnvironment = boardEnvironment;
            _boardQueries = boardQueries;
            _conveyorSystem = conveyorSystem;
            _ladderSystem = ladderSystem;
            _legacySimulation = legacySimulation;
            _plantTimingSystem = plantTimingSystem;
            _purchaseSystem = purchaseSystem;
            _seedBankSystem = seedBankSystem;
            _specialPlantActions = specialPlantActions;
            _upgradePlantActions = upgradePlantActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieLifecycle = zombieLifecycle;
        }

        /// <summary>Performs plant adventure.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "handled">The handled.</param>
        /// <param name = "planted">The planted.</param>
        internal void PlantAdventure(PlantKind kind, int row, int column, ref bool handled, ref bool planted)
        {
            handled = true;
            if (_adventureModeRunner.Adventure.Level.HasConveyor || _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Vasebreaker)
            {
                var packet = _adventureModeRunner.Adventure._packets.Find(p => p.Kind == kind);
                planted = packet != null && TryUseAdventurePacket(packet.Id, row, column);
                if (packet == null)
                {
                    _world.LastAdventurePlantingFailure = AdventurePlantingFailure.SeedUnavailable;
                }

                return;
            }

            _world.LastAdventurePlantingFailure = CheckAdventurePlanting(kind, row, column, false);
            if (_world.LastAdventurePlantingFailure != AdventurePlantingFailure.None)
            {
                return;
            }

            if (!_seedBankSystem.SpendAdventureSun(_seedBankSystem.AdventureSeedCost(kind)))
            {
                return;
            }

            _world._seedInitialCooldownThresholds[(int)kind] = 0;
            _world._seedCooldownTicks[(int)kind] = LegacySimulation.DefinitionFor(_seedBankSystem.ResolveAdventureSeed(kind)).RechargeThresholdRaw + 1;
            PlaceAdventurePlantWithEffects(kind, row, column);
            planted = true;
        }

        /// <summary>Places a player-selected plant and records the native planting effects at the successful action boundary.</summary>
        /// <param name="kind">The planted seed before any later Imitater transformation.</param>
        /// <param name="row">The selected lawn row.</param>
        /// <param name="column">The selected lawn column.</param>
        internal void PlaceAdventurePlantWithEffects(PlantKind kind, int row, int column)
        {
            var previous = _world.Presentation._nativePlantingEffects;
            _world.Presentation._nativePlantingEffects = true;
            try
            {
                PlaceAdventurePlant(kind, row, column);
            }
            finally
            {
                _world.Presentation._nativePlantingEffects = previous;
            }

            RecordAdventurePlantingEffect(kind, row, column);
        }

        /// <summary>Creates the original PC planting burst only after a successful player placement.</summary>
        /// <param name="kind">The planted seed type, before any later Imitater transformation.</param>
        /// <param name="row">The planted board row.</param>
        /// <param name="column">The planted board column.</param>
        internal void RecordAdventurePlantingEffect(PlantKind kind, int row, int column)
        {
            // PC Board::DoPlantingEffects 0x418AB0 chooses water by the square, even on an existing lily pad.
            _zombieFeedbackSystem.RequestAdventureZombieFoley(kind != PlantKind.CoffeeBean && _adventureModeRunner.Adventure.Level.IsWaterRow(row) ? 52 : 7);
            // PC Board::DoPlantingEffects 0x418AB0: coffee is flying and produces no soil or water burst.
            if (kind == PlantKind.CoffeeBean)
            {
                return;
            }

            var level = _adventureModeRunner.Adventure.Level;
            var y = 80f + row * (level.HasPool || level.HasRoof ? 85f : 100f) + 74f;
            if (level.HasRoof)
            {
                y += Math.Max(0f, 5f - column) * 20f - 10f;
            }

            y += kind == PlantKind.LilyPad ? 15f : kind == PlantKind.FlowerPot ? 30f : 0f;
            var id = ++_world.Presentation._effectId;
            _world.Presentation.PlantingEffects.Add(new global::PlantsVsZombies.Adventure.AdventurePlantingEffect
            {
                Id = id,
                Row = row,
                Seed = id * 397 ^ row * 9 + column,
                X = 40f + column * 80f + 41f,
                Y = y,
                Pool = level.IsWaterRow(row)
            });
        }

        /// <summary>Performs can plant adventure.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting can plant adventure.</returns>
        internal AdventurePlantingFailure CanPlantAdventure(PlantKind kind, int row, int column)
        {
            return !_world.IsAdventure ? AdventurePlantingFailure.NotPlaying : CheckAdventurePlanting(kind, row, column, _adventureModeRunner.Adventure.Level.HasConveyor || _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Vasebreaker);
        }

        /// <summary>Performs try use adventure packet.</summary>
        /// <param name = "packetId">The packet id.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try use adventure packet.</returns>
        internal bool TryUseAdventurePacket(int packetId, int row, int column)
        {
            if (!_world.IsAdventure)
            {
                return false;
            }

            var p = _adventureModeRunner.Adventure._packets.Find(x => x.Id == packetId);
            if (p == null)
            {
                return false;
            }

            _world.LastAdventurePlantingFailure = CheckAdventurePlanting(p.Kind, row, column, true);
            if (_world.LastAdventurePlantingFailure != AdventurePlantingFailure.None)
            {
                return false;
            }

            if (_adventureModeRunner.Adventure.Level.Mode == AdventureMode.Bowling)
            {
                _adventureModeRunner.Adventure._nuts.Add(new AdventureBowlingNut { Row = row, X = column + 0.5f, Explosive = p.Kind == PlantKind.ExplodeONut });
            }
            else
            {
                PlaceAdventurePlantWithEffects(p.Kind, row, column);
            }

            _conveyorSystem.RemoveAdventurePacket(p);
            return true;
        }

        /// <summary>Performs check adventure planting.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "packet">The packet.</param>
        /// <returns>The resulting check adventure planting.</returns>
        internal AdventurePlantingFailure CheckAdventurePlanting(PlantKind kind, int row, int column, bool packet)
        {
            var a = _adventureModeRunner.Adventure;
            var l = a.Level;
            if (_world.State != MatchState.Playing || a.Phase != AdventurePhase.Playing)
            {
                return AdventurePlantingFailure.NotPlaying;
            }

            if (!l.IsLawnRow(row) || column < 0 || column >= SimulationWorld.Columns || kind < PlantKind.Peashooter || kind > PlantKind.ExplodeONut)
            {
                return AdventurePlantingFailure.InvalidCell;
            }

            if (l.Number == 5 && a.Tutorial == AdventureTutorial.Shovel)
            {
                return AdventurePlantingFailure.Tutorial;
            }

            if (l.Mode == AdventureMode.Bowling)
            {
                return packet && (kind == PlantKind.Wallnut || kind == PlantKind.ExplodeONut) ? column < 3 ? AdventurePlantingFailure.None : AdventurePlantingFailure.BowlingLine : AdventurePlantingFailure.SeedUnavailable;
            }

            if (kind == PlantKind.Imitater && !a.ImitaterTarget.HasValue)
            {
                return AdventurePlantingFailure.ImitaterTarget;
            }

            var whackSeed = l.Mode == AdventureMode.WhackAZombie && (kind == PlantKind.CherryBomb || kind == PlantKind.GraveBuster || kind == PlantKind.PotatoMine);
            if (!packet && !whackSeed && !SeedBankSystem.ContainsSeed(a.SelectedSeeds, kind))
            {
                return AdventurePlantingFailure.SeedUnavailable;
            }

            if (!packet && _world._seedCooldownTicks[(int)kind] > 0)
            {
                return AdventurePlantingFailure.Cooldown;
            }

            if (!packet && _seedBankSystem.AdventureSunAvailable < _seedBankSystem.AdventureSeedCost(kind))
            {
                return AdventurePlantingFailure.Sun;
            }

            kind = _seedBankSystem.ResolveAdventureSeed(kind);
            if (!l.IsReplay && (l.Number == 1 && a.Tutorial == AdventureTutorial.ChooseSeed || l.Number == 2 && a._plantedSunflowers < 2 && kind != PlantKind.Sunflower || l.Number == 5 && a.Tutorial == AdventureTutorial.Shovel))
            {
                return AdventurePlantingFailure.Tutorial;
            }

            if (a._vases.Exists(v => v.Row == row && v.Column == column))
            {
                return AdventurePlantingFailure.Occupied;
            }

            if (a._craters.Exists(v => v.Row == row && v.Column == column))
            {
                return AdventurePlantingFailure.Crater;
            }

            if (a._ice.Exists(v => v.Row == row && v.CoversColumn(column)))
            {
                return AdventurePlantingFailure.Ice;
            }

            var grave = a._graves.Exists(g => g.Row == row && g.Column == column);
            if (kind == PlantKind.GraveBuster)
            {
                return grave && _boardQueries.AdventurePlantAt(row, column, PlantLayer.Main) == null ? AdventurePlantingFailure.None : AdventurePlantingFailure.Grave;
            }

            if (grave)
            {
                return AdventurePlantingFailure.Grave;
            }

            if (_purchaseSystem.IsAdventureFirstAid(kind, row, column))
            {
                return AdventurePlantingFailure.None;
            }

            var main = _boardQueries.AdventurePlantAt(row, column, PlantLayer.Main);
            if (UpgradePlantActions.UpgradePrerequisite(kind).HasValue)
            {
                return _upgradePlantActions.CheckAdventureUpgrade(kind, row, column);
            }

            if (kind == PlantKind.Pumpkin && main != null && main.Kind == PlantKind.CobCannon)
            {
                return AdventurePlantingFailure.CannonFootprint;
            }

            if (kind == PlantKind.CoffeeBean)
            {
                return main != null && main.BungeeState == 0 && main.Asleep && main.WakeTimer <= 0 && _boardQueries.AdventurePlantAt(row, column, PlantLayer.Effect) == null ? AdventurePlantingFailure.None : AdventurePlantingFailure.NeedsSleepingMushroom;
            }

            var aquatic = kind == PlantKind.LilyPad || kind == PlantKind.TangleKelp || kind == PlantKind.SeaShroom;
            if (aquatic && !l.IsWaterRow(row))
            {
                return AdventurePlantingFailure.NeedsWater;
            }

            if (l.HasRoof && kind == PlantKind.Spikeweed)
            {
                return AdventurePlantingFailure.RoofRestriction;
            }

            var layer = LayerFor(kind);
            if (_boardQueries.AdventurePlantAt(row, column, layer) != null)
            {
                return AdventurePlantingFailure.Occupied;
            }

            var support = _boardQueries.AdventurePlantAt(row, column, PlantLayer.Support);
            if (l.IsWaterRow(row) && kind == PlantKind.PotatoMine)
            {
                return support != null && support.Kind == PlantKind.LilyPad
                    ? AdventurePlantingFailure.PotatoMineOnLilyPad : AdventurePlantingFailure.NeedsLilyPad;
            }

            if (l.IsWaterRow(row) && !aquatic && !(kind == PlantKind.Pumpkin && main != null && main.Kind == PlantKind.Cattail) && (support == null || support.BungeeState != 0 || support.Kind != PlantKind.LilyPad))
            {
                return AdventurePlantingFailure.NeedsLilyPad;
            }

            if (kind == PlantKind.LilyPad && main != null)
            {
                return AdventurePlantingFailure.Occupied;
            }

            if (kind == PlantKind.FlowerPot && (l.IsWaterRow(row) || main != null || _boardQueries.AdventurePlantAt(row, column, PlantLayer.Armor) != null))
            {
                return AdventurePlantingFailure.Occupied;
            }

            if (l.HasRoof && kind != PlantKind.FlowerPot && (support == null || support.BungeeState != 0 || support.Kind != PlantKind.FlowerPot))
            {
                return AdventurePlantingFailure.NeedsFlowerPot;
            }

            return AdventurePlantingFailure.None;
        }

        /// <summary>Performs layer for.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting layer for.</returns>
        internal static PlantLayer LayerFor(PlantKind kind)
        {
            return kind == PlantKind.LilyPad || kind == PlantKind.FlowerPot ? PlantLayer.Support : kind == PlantKind.Pumpkin ? PlantLayer.Armor : kind == PlantKind.CoffeeBean ? PlantLayer.Effect : PlantLayer.Main;
        }

        /// <summary>Performs nocturnal.</summary>
        /// <param name = "k">The k.</param>
        /// <returns>The resulting nocturnal.</returns>
        internal static bool Nocturnal(PlantKind k)
        {
            return (int)k >= 8 && (int)k <= 15 && k != PlantKind.GraveBuster || k == PlantKind.SeaShroom || k == PlantKind.MagnetShroom || k == PlantKind.GloomShroom;
        }

        /// <summary>Creates or removes PC SetSleeping's independent original Z animation.</summary>
        /// <param name="plant">The allocated plant whose current sleep flag owns the animation.</param>
        internal void InitializeAdventureSleepingPlant(Plant plant)
        {
            if (!plant.Asleep)
            {
                plant.SleepingPresentation = null;
                return;
            }

            plant.BlinkAnimation = null;
            if (plant.SleepingPresentation == null)
            {
                var rate = 6f + (float)_world._random.NextDouble() * 2f;
                var pooledRecord1 = _world.LifetimePool.Rent<AdventureSleepingState>();
                pooledRecord1.Rate = rate;
                // PC reanimation 68 is Z.reanim: seventeen frames, random normalized time in [0,.9].
                pooledRecord1.AnimationSeconds = (float)_world._random.NextDouble() * .9f * 17f / rate;
                plant.SleepingPresentation = pooledRecord1;
            }
        }

        /// <summary>Resolves PC 498E30's chooser warning using original recommendation and Last Stand admission rules.</summary>
        /// <param name="kind">The seed actually hovered, including the original Imitater packet identifier.</param>
        /// <param name="coffeeChosen">Whether coffee is already selected in this chooser.</param>
        /// <returns>The original LawnStrings warning key, or an empty string for a recommended seed.</returns>
        internal string AdventureChooserWarningKey(PlantKind kind, bool coffeeChosen)
        {
            var mechanic = _world.ModeSession?.Definition.Mechanic;
            if (mechanic == ModeMechanic.LastStand && (kind == PlantKind.Sunflower || kind == PlantKind.SunShroom
                || kind == PlantKind.TwinSunflower || kind == PlantKind.SeaShroom || kind == PlantKind.PuffShroom))
            {
                return "NOT_ALLOWED_ON_THIS_LEVEL";
            }

            var level = _adventureModeRunner.Adventure.Level;
            if (Nocturnal(kind) && !level.IsNight && !coffeeChosen)
            {
                return "NOCTURNAL_WARNING";
            }

            var graves = level.Terrain == AdventureTerrain.Night && level.Mode != AdventureMode.Bowling
                && level.Mode != AdventureMode.Vasebreaker && mechanic != ModeMechanic.Bowling && mechanic != ModeMechanic.Pogo
                && mechanic != ModeMechanic.Beghouled && mechanic != ModeMechanic.BeghouledTwist
                && mechanic != ModeMechanic.Vases && mechanic != ModeMechanic.LastStand && mechanic != ModeMechanic.IZombie;
            if (kind == PlantKind.CoffeeBean && level.IsNight || kind == PlantKind.GraveBuster && !graves
                || kind == PlantKind.Plantern && !_boardEnvironment.AdventureHasFog || kind == PlantKind.FlowerPot && !level.HasRoof
                || level.HasRoof && (kind == PlantKind.Spikeweed || kind == PlantKind.Spikerock)
                || !level.HasPool && (kind == PlantKind.LilyPad || kind == PlantKind.TangleKelp
                    || kind == PlantKind.SeaShroom || kind == PlantKind.Cattail))
            {
                return "NOT_RECOMMENDED_FOR_LEVEL";
            }

            return string.Empty;
        }

        /// <summary>Performs place adventure plant.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting place adventure plant.</returns>
        internal Plant? PlaceAdventurePlant(PlantKind kind, int row, int column)
        {
            if ((int)kind >= 40 && (int)kind <= 48)
            {
                var upgraded = _upgradePlantActions.PlaceAdventureUpgrade(kind, row, column);
                _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.UpgradePlanted, (int)kind, row, column);
                return upgraded;
            }

            if (_purchaseSystem.IsAdventureFirstAid(kind, row, column))
            {
                var old = _boardQueries.AdventurePlantAt(row, column, kind == PlantKind.Pumpkin ? PlantLayer.Armor : PlantLayer.Main);
                _world.Plants.Remove(old);
            }

            var plant = _world.LifetimePool.Rent<Plant>();
            plant.Id = ++_adventureModeRunner.Adventure._plantId;
            plant.Kind = kind;
            plant.Row = row;
            plant.Column = column;
            plant.Layer = LayerFor(kind);
            plant.Health = kind == PlantKind.Wallnut || kind == PlantKind.Pumpkin ? 4000f : kind == PlantKind.TallNut ? 8000f : kind == PlantKind.Garlic ? 400f : 300f;
            plant.Asleep = Nocturnal(kind) && !_adventureModeRunner.Adventure.Level.IsNight;
            plant.ActionTimer = kind == PlantKind.PotatoMine ? 15f : kind == PlantKind.CherryBomb || kind == PlantKind.Jalapeno || kind == PlantKind.DoomShroom || kind == PlantKind.IceShroom ? 1f : kind == PlantKind.GraveBuster ? global::PlantsVsZombies.Adventure.AdventureGraveBusterRules.TotalSeconds : 0.5f;
            plant.IsSmall = false;
            AdventurePlantBlinkRules.Initialize(plant, _world._random);
            // PC 46BE80 creates the free sleep animation before the launch counter and planting displacement.
            InitializeAdventureSleepingPlant(plant);
            _plantTimingSystem.InitializeAdventureNativePlantTiming(plant);
            AdventurePlantBlinkRules.CompleteInitialization(plant, _world._random);
            if (kind == PlantKind.SunShroom || AdventurePuffShroomRules.IsShortRange(kind))
            {
                // PC 46C6F5..46C712 / 46C998..46C9B5 changes mX/mY once; row/column occupancy stays on the selected square.
                plant.PositionOffsetX = _world._random.Next(10) - 5;
                plant.PositionOffsetY = kind == PlantKind.SunShroom ? _world._random.Next(10) - 5 : _world._random.Next(6) - 3;
            }
            if (kind == PlantKind.CoffeeBean)
            {
                plant.CoffeePresentation = _world.LifetimePool.Rent<AdventureCoffeeBeanState>();
                plant._presentationKind = kind;

                plant.ActionTimer = 1f;
            }

            _world.Plants.Add(plant);
            if (kind != PlantKind.FlowerPot && _boardQueries.AdventurePlantAt(row, column, PlantLayer.Support) is Plant pot
                && pot.Kind == PlantKind.FlowerPot)
            {
                // PC 46CA8B..46CAD6 stops the existing pot at its current animation frame.
                pot.PresentationFlowerPotPaused = true;
            }

            if (kind == PlantKind.GraveBuster)
            {
                // Plant::PlantInit plays FOLEY_GRAVEBUSTERCHOMP when the buster lands in play.
                _world.Presentation.GraveBusterChompSoundSerial++;
            }

            if (kind == PlantKind.Jalapeno)
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(57);
            }
            else if (kind == PlantKind.Plantern)
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(-3);
            }

            if (kind == PlantKind.Sunflower)
            {
                _adventureModeRunner.Adventure._plantedSunflowers++;
                if (!_adventureModeRunner.Adventure.Level.IsReplay && _adventureModeRunner.Adventure.Level.Number == 2)
                {
                    _adventureModeRunner.Adventure.Tutorial = _adventureModeRunner.Adventure._plantedSunflowers >= 2 ? AdventureTutorial.Complete : AdventureTutorial.RechargeSunflower;
                }
            }

            if (!_adventureModeRunner.Adventure.Level.IsReplay && _adventureModeRunner.Adventure.Level.Number == 1)
            {
                _adventureModeRunner.Adventure.Tutorial = _adventureModeRunner.Adventure._collectedSun ? AdventureTutorial.Complete : AdventureTutorial.CollectSun;
            }

            if (_adventureModeRunner.Adventure.Tutorial == AdventureTutorial.PoolSupport && kind == PlantKind.LilyPad || _adventureModeRunner.Adventure.Tutorial == AdventureTutorial.RoofPots && kind == PlantKind.CabbagePult || _adventureModeRunner.Adventure.Tutorial == AdventureTutorial.NightMushrooms && Nocturnal(kind))
            {
                _adventureModeRunner.Adventure.Tutorial = AdventureTutorial.Complete;
            }

            _legacySimulation.Raise(GameEvent.Planted);
            return plant;
        }

        /// <summary>Calls <see cref="PlaceAdventurePlant"/> where a result must exist, failing with a descriptive exception instead of returning null.</summary>
        /// <param name="kind">The <c>kind</c> argument forwarded to <see cref="PlaceAdventurePlant"/>.</param>
        /// <param name="row">The <c>row</c> argument forwarded to <see cref="PlaceAdventurePlant"/>.</param>
        /// <param name="column">The <c>column</c> argument forwarded to <see cref="PlaceAdventurePlant"/>.</param>
        /// <returns>The non-null place adventure plant result.</returns>
        /// <exception cref="System.InvalidOperationException">The method returned no result.</exception>
        internal Plant RequiredPlaceAdventurePlant(PlantKind kind, int row, int column) => PlaceAdventurePlant(kind, row, column) ?? throw new System.InvalidOperationException("'PlaceAdventurePlant' returned no result.");

        /// <summary>Performs remove adventure plant.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "handled">The handled.</param>
        /// <param name = "removed">The removed.</param>
        internal void RemoveAdventurePlant(int row, int column, ref bool handled, ref bool removed)
        {
            handled = true;
            if (_adventureModeRunner.Adventure.Phase != AdventurePhase.Playing || !_adventureModeRunner.Adventure.Level.IsReplay && _adventureModeRunner.Adventure.Level.Number < 5 || _adventureModeRunner.Adventure.Level.Mode == AdventureMode.WhackAZombie)
            {
                return;
            }

            var p = _boardQueries.AdventurePlantAt(row, column, PlantLayer.Effect) ?? _boardQueries.AdventurePlantAt(row, column, PlantLayer.Armor) ?? _boardQueries.AdventurePlantAt(row, column, PlantLayer.Main) ?? _boardQueries.AdventurePlantAt(row, column, PlantLayer.Support);
            if (p == null)
            {
                return;
            }

            RemoveAdventurePlantAndStack(p);
            removed = true;
            if (_adventureModeRunner.Adventure.Tutorial == AdventureTutorial.Shovel && !_world.Plants.Exists(x => x.Row == 2 && x.Column >= 3 && x.Column <= 5 && x.Kind == PlantKind.Peashooter))
            {
                _adventureModeRunner.Adventure._removalTutorialDone = true;
                _adventureModeRunner.Adventure.Tutorial = AdventureTutorial.Complete;
            }
        }

        /// <summary>Removes the plant selected by the native shovel hit test.</summary>
        /// <param name="plant">The highlighted tool target, or null when the pointer has no target.</param>
        /// <returns>Whether the target was present and could be removed from the playing board.</returns>
        internal bool TryShovelAdventurePlant(Plant? plant)
        {
            if (!_world.IsAdventure || _world.State != MatchState.Playing || _adventureModeRunner.Adventure.Phase != AdventurePhase.Playing
                || !_adventureModeRunner.Adventure.Level.IsReplay && _adventureModeRunner.Adventure.Level.Number < 5 || _adventureModeRunner.Adventure.Level.Mode == AdventureMode.WhackAZombie
                || plant == null || plant.Kind == PlantKind.GraveBuster || !_world.Plants.Contains(plant)
                || plant.Kind == PlantKind.Squash && plant.PresentationBiteNotOnGround)
            {
                return false;
            }

            RemoveAdventurePlantAndStack(plant);
            if (_adventureModeRunner.Adventure.Tutorial == AdventureTutorial.Shovel && !_world.Plants.Exists(x => x.Row == 2 && x.Column >= 3 && x.Column <= 5 && x.Kind == PlantKind.Peashooter))
            {
                _adventureModeRunner.Adventure._removalTutorialDone = true;
                _adventureModeRunner.Adventure.Tutorial = AdventureTutorial.Complete;
            }

            return true;
        }

        /// <summary>Performs remove adventure plant and stack.</summary>
        /// <param name = "p">The p.</param>
        internal void RemoveAdventurePlantAndStack(Plant p)
        {
            if (p.Kind != PlantKind.CoffeeBean)
            {
                _ladderSystem.RemoveAdventureLadder(p.Row, p.Column);
            }

            _world.Plants.Remove(p);
            if (p.Kind == PlantKind.TangleKelp && p.TangleKelpPresentation != null)
            {
                var target = _specialPlantActions.FindAdventureKelpZombie(p.TangleKelpPresentation.TargetZombieId);
                if (target != null)
                {
                    // PC Plant::Die also finishes a grabbed target when the plant is shoveled.
                    target.TangleKelpGrab = null;
                    target.PresentationDeath = AdventureZombieDeathPresentation.Eaten;
                    _zombieLifecycle.KillAdventureZombie(target);
                }
            }

            if (p.Layer == PlantLayer.Support)
            {
                // PC NotOnGround keeps a rising, falling or flattened Squash independent of its former support cell.
                _world.Plants.RemoveAll(x => x.Row == p.Row && x.Column <= p.Column && p.Column < x.Column + x.ColumnSpan
                    && !x.PresentationBiteNotOnGround);
            }

            if (_biteSystem.AdventureAirAttackPlantAt(p.Row, p.Column) is Plant pot && pot.Kind == PlantKind.FlowerPot
                && pot.PresentationFlowerPotPaused)
            {
                // PC Plant::Die restarts only an uncovered pot, preserving its current animation phase.
                pot.PresentationFlowerPotPaused = false;
                pot.PresentationRate = 10f + (float)_world._random.NextDouble() * 5f;
                pot._presentationKind = PlantKind.FlowerPot;
            }

            if (_adventureModeRunner.Adventure.SelectedCobCannonId.HasValue && _boardQueries.FindPlantById(_adventureModeRunner.Adventure.SelectedCobCannonId.Value) == null)
            {
                _adventureModeRunner.Adventure.SelectedCobCannonId = null;
            }
        }
    }
}
