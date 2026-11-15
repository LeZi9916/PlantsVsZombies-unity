#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ZombieSpawnSystem responsibilities for one deterministic match.</summary>
    internal sealed class ZombieSpawnSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BungeeZombieActions operations.</summary>
        private BungeeZombieActions _bungeeZombieActions = null!;
        /// <summary>Provides IndependentModeRunner operations.</summary>
        private IndependentModeRunner _independentModeRunner = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal ZombieSpawnSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="bungeeZombieActions">The BungeeZombieActions collaborator.</param>
        /// <param name="independentModeRunner">The IndependentModeRunner collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BungeeZombieActions bungeeZombieActions, IndependentModeRunner independentModeRunner, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _bungeeZombieActions = bungeeZombieActions;
            _independentModeRunner = independentModeRunner;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Performs choose adventure zombie row.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting choose adventure zombie row.</returns>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal int ChooseAdventureZombieRow(ZombieKind kind)
        {
            var picker = _adventureModeRunner.Adventure.RowPicker;
            if (_adventureModeRunner.Adventure.RakeAttracting && CanAdventureZombieSpawnInRow(kind, _adventureModeRunner.Adventure.RakeRow))
            {
                _adventureModeRunner.Adventure.RakeAttracting = false;
                picker.RecordPick(_adventureModeRunner.Adventure.RakeRow);
                return _adventureModeRunner.Adventure.RakeRow;
            }

            for (var row = 0; row < AdventureRowPicker.RowCount; row++)
            {
                var weight = 0f;
                if (CanAdventureZombieSpawnInRow(kind, row))
                {
                    if (_world.ModeSession != null && _world.ModeSession.Definition.Id == 26)
                    {
                        weight = AdventurePortalRowSpawnWeight(row);
                    }
                    else if (_world.ModeSession == null || _world.ModeSession.Definition.Id != 21 || _adventureModeRunner.Adventure.CurrentWave > 3 || row != 5)
                    {
                        var sinceMowed = _adventureModeRunner.Adventure.CurrentWave - picker.LastMowedWave[row];
                        if (IsNativeContinuousChallenge && _adventureModeRunner.Adventure.CurrentWave == _adventureModeRunner.Adventure.Waves.Count - 1)
                        {
                            sinceMowed = 100;
                        }

                        weight = sinceMowed <= 1 ? 0.01f : sinceMowed == 2 ? 0.5f : 1f;
                    }
                }

                picker.Weights[row] = weight;
            }

            return picker.Pick(_world._random);
        }

        /// <summary>Checks PC 0x4197D0's lane gates before smoothing or a forced rake pick.</summary>
        /// <param name="kind">The native zombie identifier being created.</param>
        /// <param name="row">The candidate zero-based lane.</param>
        /// <returns>Whether the board and native mode permit this species in the lane.</returns>
        internal bool CanAdventureZombieSpawnInRow(ZombieKind kind, int row)
        {
            var level = _adventureModeRunner.Adventure.Level;
            if (row < 0 || row >= _world.RowCount || !level.IsLawnRow(row))
            {
                return false;
            }

            var mode = _world.ModeSession == null ? 0 : _world.ModeSession.Definition.Id;
            var wave = _adventureModeRunner.Adventure.CurrentWave;
            if (mode == 31)
            {
                wave += (_independentModeRunner.RequiredModeSession.Stage - 1) * _independentModeRunner.RequiredModeSession.Definition.WaveCount;
            }

            if (!AdventureZombiePoolRules.CanSpawnInRow(kind, level.IsWaterRow(row), wave)
                || level.HasRoof && (kind == ZombieKind.Zomboni || kind == ZombieKind.Bobsled)
                || kind == ZombieKind.Bobsled && !_adventureModeRunner.Adventure._ice.Exists(ice => ice.Row == row && ice.RemainingTicks > 0))
            {
                return false;
            }

            if (row == 0 && (kind == ZombieKind.Gargantuar || (int)kind == 32) && (mode < 11 || mode > 15))
            {
                return false;
            }

            return kind != ZombieKind.Dancing || level.HasPool || level.IsLawnRow(row - 1) && level.IsLawnRow(row + 1);
        }

        /// <summary>Checks PC 0x460B30's modes whose last authored wave is reused continuously.</summary>
        internal bool IsNativeContinuousChallenge
        {
            get
            {
                if (_world.ModeSession == null)
                {
                    return _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Boss;
                }

                var mode = _world.ModeSession.Definition.Id;
                return mode == 18 || mode == 20 || mode == 22 || mode == 24 || mode == 35
                    || mode == 36 || mode == 40 || mode == 71 || mode == 72;
            }
        }

        /// <summary>Preserves PC 0x432A40's portal-path distance and portal-presence lane weights.</summary>
        /// <param name="row">The zero-based lane from which the zombie would enter.</param>
        /// <returns>One hundredth near a mower, one on an open portal lane, or one fifth on another lane.</returns>
        /// <exception cref="InvalidOperationException">An open portal has no matching exit.</exception>
        internal float AdventurePortalRowSpawnWeight(int row)
        {
            var portals = _independentModeRunner.RequiredModeSession.Portals;
            var pathRow = row;
            var column = 10;
            var distance = 0;
            while (distance < 40)
            {
                ModePortal? entrance = null;
                foreach (var portal in portals)
                {
                    if (portal.Row == pathRow && portal.Column < column && (entrance == null || portal.Column >= entrance.Column))
                    {
                        entrance = portal;
                    }
                }

                if (entrance == null)
                {
                    distance += column;
                    break;
                }

                ModePortal? exit = null;
                foreach (var portal in portals)
                {
                    if (portal != entrance && portal.Pair == entrance.Pair)
                    {
                        exit = portal;
                        break;
                    }
                }

                if (exit == null)
                {
                    throw new InvalidOperationException("An open spawn portal has no matching exit.");
                }

                distance += column - entrance.Column;
                column = exit.Column;
                pathRow = exit.Row;
            }

            if (distance < 5)
            {
                return 0.01f;
            }

            foreach (var portal in portals)
            {
                if (portal.Row == row)
                {
                    return 1f;
                }
            }

            return 0.2f;
        }

        /// <summary>Performs spawn adventure zombie.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "wave">The wave.</param>
        /// <returns>The resulting spawn adventure zombie.</returns>
        internal Zombie SpawnAdventureZombie(ZombieKind kind, int row, float x, int wave)
        {
            return CreateAdventureZombie(kind, row, x, wave, -1);
        }

        /// <summary>Creates PC 551020's bungee with one column-constrained target selection.</summary>
        /// <param name="column">The original column selected by the boss.</param>
        /// <returns>The bungee at its native roof-relative initial altitude.</returns>
        internal Zombie SpawnAdventureBossBungee(int column)
        {
            var variant = _world._random.Next(5) == 0;
            var zombie = CreateAdventureZombie(ZombieKind.Bungee, 0, 0f, 0, column);
            zombie.NativeVariant = variant;
            zombie.PresentationAltitude = 80f + zombie.Row * 85f + Math.Max(0f, 5f - column) * 20f - 70f;
            return zombie;
        }

        /// <summary>Initializes a native zombie while preserving an optional boss target constraint.</summary>
        /// <param name="kind">The original species.</param>
        /// <param name="row">The original initial row.</param>
        /// <param name="x">The requested lawn position.</param>
        /// <param name="wave">The original source wave.</param>
        /// <param name="bungeeColumn">The boss-constrained target column, or minus one.</param>
        /// <returns>The newly initialized native zombie.</returns>
        private Zombie CreateAdventureZombie(ZombieKind kind, int row, float x, int wave, int bungeeColumn)
        {
            var health = kind == ZombieKind.Yeti ? 1350f : kind == ZombieKind.Bungee ? 450f : kind == ZombieKind.Ladder ? 500f : kind == ZombieKind.Gargantuar ? 3000f : kind == ZombieKind.Zomboni ? 1350f : kind == ZombieKind.Catapult ? 850f : kind == ZombieKind.PoleVaulter || kind == ZombieKind.DolphinRider || kind == ZombieKind.JackInTheBox || kind == ZombieKind.Pogo || kind == ZombieKind.Dancing ? 500f : 270f;
            var armor = global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.StartingArmor(kind);
            var shield = global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.StartingShield(kind);
            var z = new Zombie
            {
                Id = ++_adventureModeRunner.Adventure._zombieId,
                Kind = kind,
                Row = row,
                X = x,
                Wave = wave,
                Health = health,
                StartingHealth = health,
                ArmorHealth = armor,
                ShieldHealth = shield,
                Airborne = kind == ZombieKind.Balloon,
                FlyingHealth = kind == ZombieKind.Balloon ? 20 : 0,
                PresentationNativeHeight = 0,
                Submerged = kind == ZombieKind.Digger,
                AbilityTimer = kind == ZombieKind.Dancing ? 4f : kind == ZombieKind.Bungee ? 4f : 5f
            };
            if (_adventureModeRunner.Adventure.Level.Mode == AdventureMode.LittleTrouble)
            {
                z.IsSmall = true;
                z.Health = (int)z.Health / 4;
                z.ArmorHealth = (int)z.ArmorHealth / 4;
                z.ShieldHealth = (int)z.ShieldHealth / 4;
                z.FlyingHealth /= 4;
                z.StartingHealth = z.Health;
            }

            z.PresentationMaxArmor = z.ArmorHealth;
            z.PresentationMaxShield = z.ShieldHealth;
            _zombieFeedbackSystem.InitializeAdventureZombieNative(z, x >= 9.5f && kind != ZombieKind.Bungee);
            if (kind == ZombieKind.Bungee)
            {
                z.BungeeState = new AdventureBungeeState();
                z.PresentationAltitude = 3000f + _world._random.Next(151);
                z.PresentationNativeVelocity = 0f;
                z.Speed = 0f;
                z.PresentationNativePhase = 4;
                _bungeeZombieActions.PickAdventureBungeeTarget(z, bungeeColumn);
            }

            if (kind == ZombieKind.JackInTheBox)
            {
                // PC 53E808..53E895: distance tickets, rare early pop, then integer division by velocity and doubling.
                var distance = 450 + _world._random.Next(300);
                if (_world._random.Next(20) == 0)
                {
                    distance /= 3;
                }

                z.JackInTheBoxTicks = _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Vasebreaker
                    ? 10 : (int)(distance / (double)z.PresentationNativeVelocity) * 2;
                z.PresentationNativePhase = 15;
                z._presentationSpeedPhase = 15;
            }
            if (_adventureModeRunner.Adventure.Level.IsWaterRow(row) && (AdventureZombiePoolRules.UsesPlainBody(kind) || kind == ZombieKind.Balloon)
                || kind == ZombieKind.DuckyTube)
            {
                // PC LoadPlainZombieReanim 0x53FE40 enables tube artwork by the row, retaining the original kind and armor.
                z.PoolState = new AdventureZombiePoolState { HasDuckyTube = AdventureZombiePoolRules.UsesPlainBody(kind) };
                z.PresentationAltitude = kind == ZombieKind.Balloon ? 25f : 0f;
                z.PoolState.Altitude = z.PresentationAltitude;
                z.PresentationInPool = false;
                z.PresentationNativeHeight = 0;
            }

            var nativeKind = (int)kind;
            if (nativeKind >= 26 && nativeKind <= 31)
            {
                z.ModeHead = nativeKind;
                z.Kind = ZombieKind.Normal;
                z.PresentationWalk2 = true;
                var fuse = nativeKind == 28 ? 2 * (int)((275 + _world._random.Next(175)) / (double)z.PresentationNativeVelocity) : 0;
                z.PlantZombieState = new AdventurePlantZombieState(nativeKind, fuse);
                if (nativeKind == 28)
                {
                    z.Health = z.StartingHealth = 500;
                }
                if (nativeKind == 27 || nativeKind == 31)
                {
                    z.ArmorHealth = z.PresentationMaxArmor = nativeKind == 27 ? 1100 : 2200;
                }
                if (nativeKind == 30)
                {
                    z.PresentationNativePhase = 92;
                    z._presentationSpeedPhase = 92;
                }
            }
            else if (nativeKind == 32)
            {
                z.ModeHead = 32;
                z.Kind = ZombieKind.Gargantuar;
                z.Health = z.StartingHealth = 6000;
            }

            // A new actor has no preceding movement sample. Seed interpolation after edge placement
            // and target selection so its first rendered frame starts at the actual spawn position.
            z.PresentationPreviousX = z.X;
            _world.Zombies.Add(z);
            if (kind == ZombieKind.DolphinRider)
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(51);
            }
            else if (kind == ZombieKind.Balloon)
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(60);
            }

            if (kind == ZombieKind.Zomboni)
            {
                z.ZomboniState = new AdventureZomboniState();
                _zombieFeedbackSystem.RequestAdventureZombieFoley(16);
            }

            _world.SpawnedZombies++;
            return z;
        }

        /// <summary>Preserves PC 0x419A60's one-in-five prefix, also inlined for the bungee carrier at 0x41ECCF.</summary>
        /// <param name="kind">The native species identifier.</param>
        /// <param name="row">The already selected zero-based row.</param>
        /// <param name="x">The requested grid position, replaced by native edge initialization when appropriate.</param>
        /// <param name="wave">The originating zero-based wave.</param>
        /// <returns>The initialized independent zombie.</returns>
        internal Zombie SpawnAdventureBoardZombie(ZombieKind kind, int row, float x, int wave)
        {
            var variant = _world._random.Next(5) == 0;
            var zombie = SpawnAdventureZombie(kind, row, x, wave);
            zombie.NativeVariant = zombie.PlantZombieState == null && variant;
            return zombie;
        }
    }
}
