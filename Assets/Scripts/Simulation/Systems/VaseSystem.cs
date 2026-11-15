#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns VaseSystem responsibilities for one deterministic match.</summary>
    internal sealed class VaseSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides ConveyorSystem operations.</summary>
        private ConveyorSystem _conveyorSystem = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides VaseMode operations.</summary>
        private VaseMode _vaseMode = null!;
        /// <summary>Provides ZombieSpawnSystem operations.</summary>
        private ZombieSpawnSystem _zombieSpawnSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal VaseSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="conveyorSystem">The ConveyorSystem collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="vaseMode">The VaseMode collaborator.</param>
        /// <param name="zombieSpawnSystem">The ZombieSpawnSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, ConveyorSystem conveyorSystem, ProjectileSystem projectileSystem, SunSystem sunSystem, VaseMode vaseMode, ZombieSpawnSystem zombieSpawnSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _conveyorSystem = conveyorSystem;
            _projectileSystem = projectileSystem;
            _sunSystem = sunSystem;
            _vaseMode = vaseMode;
            _zombieSpawnSystem = zombieSpawnSystem;
        }

        /// <summary>Performs add adventure packet.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting add adventure packet.</returns>
        internal AdventurePacket AddAdventurePacket(PlantKind kind, int? row = null, int? column = null)
        {
            var p = new AdventurePacket
            {
                Id = ++_adventureModeRunner.Adventure._packetId,
                Kind = kind,
                FromVaseRow = row,
                FromVaseColumn = column
            };
            if (p.IsVaseDrop)
            {
                p.InitializeVaseDrop(_world._random);
            }

            _adventureModeRunner.Adventure._packets.Add(p);
            _conveyorSystem.InitializeAdventureConveyorPacket(p, _adventureModeRunner.Adventure._packets.Count - 1);
            _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.ConveyorPacket, p.Id);
            return p;
        }

        /// <summary>Collects a live vase seed coin for the normal cursor.</summary>
        /// <param name="packetId">The identifier of the usable seed coin beneath the pointer.</param>
        /// <returns>Whether a live, unheld vase seed was collected while the board was playing.</returns>
        internal bool CollectVasePacket(int packetId)
        {
            if (!_world.IsAdventure || _world.State != MatchState.Playing || _adventureModeRunner.Adventure.Phase != AdventurePhase.Playing
                || _world.ModeSession != null && _world.ModeSession.Phase != ModePhase.Playing)
            {
                return false;
            }

            foreach (var packet in _adventureModeRunner.Adventure._packets)
            {
                if (packet.Id == packetId && packet.IsVaseDrop && !packet.IsHeld)
                {
                    packet.CollectVaseDrop();
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns a selected usable seed to its original drop position after cursor cancellation.</summary>
        /// <param name="packetId">The identifier of the vase seed coin previously held by the cursor.</param>
        internal void DropVasePacket(int packetId)
        {
            if (!_world.IsAdventure)
            {
                return;
            }

            foreach (var packet in _adventureModeRunner.Adventure._packets)
            {
                if (packet.Id == packetId && packet.IsVaseDrop && packet.IsHeld)
                {
                    packet.DropVaseDrop();
                    return;
                }
            }
        }

        /// <summary>Advances all original vase seed coins once, including expiration and independent recipe cleanup.</summary>
        internal void AdvanceVasePackets()
        {
            for (var index = _adventureModeRunner.Adventure._packets.Count - 1; index >= 0; index--)
            {
                var packet = _adventureModeRunner.Adventure._packets[index];
                if (!packet.IsVaseDrop)
                {
                    continue;
                }

                packet.Age += .01f;
                if (packet.TickVaseDrop(_world._random))
                {
                    _conveyorSystem.RemoveAdventurePacket(packet);
                    _world.ModeSession?._backwardPackets.Remove(packet.Id);
                }
            }
        }

        /// <summary>Performs initialize vase stage.</summary>
        internal void InitializeVaseStage()
        {
            _world.Presentation.VaseMalletTicks = 0;
            _adventureModeRunner.Adventure._packets.Clear();
            _adventureModeRunner.Adventure._vases.Clear();
            _world.Plants.Clear();
            _projectileSystem.ClearPeas();
            _sunSystem.ClearSuns();
            var stage = _adventureModeRunner.Adventure.VaseStage;
            var contents = new List<AdventureVase>();
            AddVaseSeeds(contents, PlantKind.Peashooter, stage == 1 ? 4 : 5);
            if (stage > 0)
            {
                AddVaseSeeds(contents, PlantKind.SnowPea, 5);
            }

            AddVaseSeeds(contents, stage == 2 ? PlantKind.HypnoShroom : PlantKind.Squash, stage == 0 ? 5 : stage == 1 ? 4 : 5);
            AddVaseZombies(contents, ZombieKind.Normal, stage == 0 ? 4 : stage == 1 ? 5 : 6);
            AddVaseZombies(contents, ZombieKind.Buckethead, stage == 2 ? 2 : 1);
            if (stage == 1)
            {
                AddVaseZombies(contents, ZombieKind.Football, 1);
            }

            if (stage == 2)
            {
                AddVaseZombies(contents, ZombieKind.Dancing, 1);
                AddVaseZombies(contents, ZombieKind.JackInTheBox, 1);
            }

            using var cellsLease = _world.IntegerScratch.Rent();
            var cells = cellsLease.Items;
            for (var c = stage == 0 ? 6 : stage == 1 ? 5 : 4; c < 9; c++)
            {
                for (var r = 0; r < 5; r++)
                {
                    cells.Add(r * 9 + c);
                }
            }

            foreach (var vase in contents)
            {
                int i = _world._random.Next(cells.Count), cell = cells[i];
                cells.RemoveAt(i);
                vase.Row = cell / 9;
                vase.Column = cell % 9;
                _adventureModeRunner.Adventure._vases.Add(vase);
            }

            // PC 0x4338B0 only requests green plant vases for the second and third stages.
            RevealRandomVasePlants(stage == 0 ? 0 : stage + 1);

            _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.VaseStage, stage);
        }

        /// <summary>Selects the original green plant vases uniformly without replacement in creation order.</summary>
        /// <param name="count">The number of hidden plant vases to reveal.</param>
        internal void RevealRandomVasePlants(int count)
        {
            var plants = new List<AdventureVase>();
            foreach (var vase in _adventureModeRunner.Adventure._vases)
            {
                if (vase._seed.HasValue && !vase.ShowsPlant)
                {
                    plants.Add(vase);
                }
            }

            for (var index = 0; index < count && plants.Count > 0; index++)
            {
                var pick = _world._random.Next(plants.Count);
                plants[pick].ShowsPlant = true;
                plants.RemoveAt(pick);
            }
        }

        /// <summary>Performs add vase seeds.</summary>
        /// <param name = "v">The v.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "count">The count.</param>
        internal static void AddVaseSeeds(List<AdventureVase> v, PlantKind kind, int count)
        {
            for (var i = 0; i < count; i++)
            {
                v.Add(new AdventureVase { _seed = kind });
            }
        }

        /// <summary>Performs add vase zombies.</summary>
        /// <param name = "v">The v.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "count">The count.</param>
        internal static void AddVaseZombies(List<AdventureVase> v, ZombieKind kind, int count)
        {
            for (var i = 0; i < count; i++)
            {
                v.Add(new AdventureVase { _zombie = kind });
            }
        }

        /// <summary>Starts the original delayed hammer swing over an adventure vase.</summary>
        /// <param name="row">The target vase's lawn row.</param>
        /// <param name="column">The target vase's lawn column.</param>
        /// <returns>Whether an available vase accepted a new hammer swing.</returns>
        internal bool SmashAdventureVase(int row, int column)
        {
            if (!_world.IsAdventure || _world.ModeSession != null || _world.State != MatchState.Playing || _adventureModeRunner.Adventure.Phase != AdventurePhase.Playing || _adventureModeRunner.Adventure.Level.Mode != AdventureMode.Vasebreaker
                || _adventureModeRunner.Adventure.VaseInterludeActive && _adventureModeRunner.Adventure._vaseInterludePopulated)
            {
                return false;
            }

            return BeginVaseMallet(row, column);
        }

        /// <summary>Starts the single native pot-opening hammer without releasing its contents early.</summary>
        /// <param name="row">The target vase's lawn row.</param>
        /// <param name="column">The target vase's lawn column.</param>
        /// <returns>Whether the target exists and no earlier hammer is still active.</returns>
        internal bool BeginVaseMallet(int row, int column)
        {
            if (_world.Presentation.VaseMalletTicks > 0)
            {
                return false;
            }

            foreach (var vase in _adventureModeRunner.Adventure._vases)
            {
                if (vase.Row == row && vase.Column == column)
                {
                    // PC 0x434A50 creates one Hammer, anim_open_pot, PlayOnceAndHold at 40 FPS.
                    _world.Presentation.VaseMalletRow = row;
                    _world.Presentation.VaseMalletColumn = column;
                    _world.Presentation.VaseMalletTicks = AdventurePresentationState.VaseMalletDurationTicks;
                    _world.Presentation.VaseSwingSoundSerial++;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Releases the targeted pot only after the original hammer has completed one loop.</summary>
        internal void AdvanceVaseMallet()
        {
            if (_world.Presentation.VaseMalletTicks == 0)
            {
                return;
            }

            _world.Presentation.VaseMalletTicks--;
            if (_world.Presentation.VaseMalletTicks > 0)
            {
                return;
            }

            for (var index = 0; index < _adventureModeRunner.Adventure._vases.Count; index++)
            {
                var vase = _adventureModeRunner.Adventure._vases[index];
                if (vase.Row != _world.Presentation.VaseMalletRow || vase.Column != _world.Presentation.VaseMalletColumn)
                {
                    continue;
                }

                _adventureModeRunner.Adventure._vases.RemoveAt(index);
                if (_world.ModeSession == null)
                {
                    OpenAdventureVase(vase);
                }
                else
                {
                    _vaseMode.OpenModeVase(vase);
                }

                _world.Presentation.VaseBreakSoundSerial++;
                break;
            }
        }

        /// <summary>Releases the plant or zombie hidden in an already removed adventure vase.</summary>
        /// <param name="vase">The vase reached by the completed native hammer swing.</param>
        internal void OpenAdventureVase(AdventureVase vase)
        {
            if (vase._seed.HasValue)
            {
                AddAdventurePacket(vase._seed.Value, vase.Row, vase.Column);
            }

            if (vase._zombie.HasValue)
            {
                _zombieSpawnSystem.SpawnAdventureBoardZombie(vase._zombie.Value, vase.Row, vase.Column + 0.5f, _adventureModeRunner.Adventure.VaseStage);
            }
        }

        /// <summary>Starts the original half-second flash at the native puzzle-stage clearing point.</summary>
        internal void StartVaseScreenFlash()
        {
            _world.Presentation.VaseScreenFlashSerial++;
            _world.Presentation.VaseScreenFlashTicks = 0;
        }
    }
}
