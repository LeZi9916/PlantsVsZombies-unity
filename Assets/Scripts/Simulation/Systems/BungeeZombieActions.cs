#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns BungeeZombieActions responsibilities for one deterministic match.</summary>
    internal sealed class BungeeZombieActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
        /// <summary>Provides ZombieSpawnSystem operations.</summary>
        private ZombieSpawnSystem _zombieSpawnSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal BungeeZombieActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        /// <param name="zombieLifecycle">The ZombieLifecycle collaborator.</param>
        /// <param name="zombieSpawnSystem">The ZombieSpawnSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, BoardQueries boardQueries, PlantingSystem plantingSystem, ZombieFeedbackSystem zombieFeedbackSystem, ZombieLifecycle zombieLifecycle, ZombieSpawnSystem zombieSpawnSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _boardQueries = boardQueries;
            _plantingSystem = plantingSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieLifecycle = zombieLifecycle;
            _zombieSpawnSystem = zombieSpawnSystem;
        }

        /// <summary>Creates PC 0x41EBB0's temporary target grid for columns four through eight and five roof rows.</summary>
        /// <returns>The twenty-five target weights, initially ten thousand each.</returns>
        internal static int[] CreateAdventureBungeeDropWeights()
        {
            var weights = new int[25];
            for (var index = 0; index < weights.Length; index++)
            {
                weights[index] = 10000;
            }

            return weights;
        }

        /// <summary>Selects PC 0x41EC80's weighted bungee target and leaves its small residual weight.</summary>
        /// <param name="weights">The original column-major twenty-five-cell target grid.</param>
        /// <returns>The target encoded as column times ten plus row.</returns>
        /// <exception cref="InvalidOperationException">The target grid has no selectable weight.</exception>
        internal int PickAdventureBungeeDropCell(int[] weights)
        {
            var total = 0;
            foreach (var weight in weights)
            {
                total += weight;
            }

            var ticket = _world._random.Next(total);
            for (var index = 0; index < weights.Length; index++)
            {
                ticket -= weights[index];
                if (ticket < 0)
                {
                    weights[index] = 1;
                    return (index / 5 + 4) * 10 + index % 5;
                }
            }

            throw new InvalidOperationException("The native bungee target grid has no selectable cell.");
        }

        /// <summary>Performs drop adventure zombie.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <param name = "wave">The wave.</param>
        internal void DropAdventureZombie(ZombieKind kind, int row, int column, int wave)
        {
            // PC 0x41EC80 creates both actors before the bungee update checks Umbrella Leaf.
            var carrierRow = _zombieSpawnSystem.ChooseAdventureZombieRow(ZombieKind.Bungee);
            var carrier = _zombieSpawnSystem.SpawnAdventureBoardZombie(ZombieKind.Bungee, carrierRow, column + 0.5f, wave);
            var passengerRow = _zombieSpawnSystem.ChooseAdventureZombieRow(kind);
            var passenger = _zombieSpawnSystem.SpawnAdventureBoardZombie(kind, passengerRow, column + 0.5f, wave);
            passenger.Row = row;
            passenger.DropRemaining = float.MaxValue;
            passenger.BungeeCarrierId = carrier.Id;
            passenger.PresentationNativeHeight = 9;
            passenger.X = AdventureZombieNativeRules.GridX(25f + column * 80f, AdventureDancerRules.Scale(passenger));
            passenger.PresentationPreviousX = passenger.X;
            carrier.BungeeCarrier = true;
            carrier.Row = row;
            carrier.TargetColumn = column;
            carrier.X = AdventureZombieNativeRules.GridX(40f + column * 80f, AdventureDancerRules.Scale(carrier));
            carrier.PresentationPreviousX = carrier.X;
            carrier.BungeeState!.CarriesZombie = true;
            carrier.BungeeState.AnimationSeconds = 0f;
            carrier.LinkedZombieId = passenger.Id;
            _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.RoofDrop, (int)kind, row, column);
        }

        /// <summary>Reproduces PC 540180's weighted cell selection and reservation, including protection of the last sun producer.</summary>
        /// <param name="zombie">The bungee selecting its initial target.</param>
        /// <param name="column">A boss-constrained column, or minus one for the complete lawn.</param>
        internal void PickAdventureBungeeTarget(Zombie zombie, int column = -1)
        {
            var producers = 0;
            foreach (var plant in _world.Plants)
            {
                if (plant.Health > 0f && plant.BungeeState != 2 && BungeeSunProducer(plant.Kind))
                {
                    producers++;
                }
            }

            var reservedProducers = 0;
            foreach (var other in _world.Zombies)
            {
                if (other.Kind == ZombieKind.Bungee && other.Health > 0f && other.TargetColumn >= 0)
                {
                    var target = _biteSystem.AdventureAirAttackPlantAt(other.Row, other.TargetColumn);
                    if (target != null && BungeeSunProducer(target.Kind))
                    {
                        reservedProducers++;
                    }
                }
            }

            var protectProducer = reservedProducers == producers - 1;
            var total = 0;
            for (var x = 0; x < 9; x++)
            {
                for (var row = 0; row < 6; row++)
                {
                    var index = x * 6 + row;
                    _world._bungeeTargetWeights[index] = 0;
                    if (!_adventureModeRunner.Adventure.Level.IsLawnRow(row) || column >= 0 && x != column)
                    {
                        continue;
                    }

                    var graveAtCell = false;
                    foreach (var grave in _adventureModeRunner.Adventure._graves)
                    {
                        if (grave.Row == row && grave.Column == x)
                        {
                            graveAtCell = true;
                            break;
                        }
                    }

                    if (graveAtCell)
                    {
                        continue;
                    }

                    var plant = _biteSystem.AdventureAirAttackPlantAt(row, x);
                    if (plant != null && (plant.Kind == PlantKind.GraveBuster || plant.Kind == PlantKind.CobCannon
                        || protectProducer && BungeeSunProducer(plant.Kind)))
                    {
                        continue;
                    }

                    var reserved = false;
                    foreach (var other in _world.Zombies)
                    {
                        if (other.Kind == ZombieKind.Bungee && other.Health > 0f
                            && other.Row == row && other.TargetColumn == x)
                        {
                            reserved = true;
                            break;
                        }
                    }

                    if (!reserved)
                    {
                        _world._bungeeTargetWeights[index] = plant == null ? 1 : 10000;
                        total += _world._bungeeTargetWeights[index];
                    }
                }
            }

            if (total == 0)
            {
                zombie.Health = 0f;
                zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                return;
            }

            var draw = _world._random.Next(total);
            for (var index = 0; index < _world._bungeeTargetWeights.Length; index++)
            {
                draw -= _world._bungeeTargetWeights[index];
                if (draw < 0)
                {
                    zombie.Row = index % 6;
                    zombie.TargetColumn = index / 6;
                    zombie.X = AdventureZombieNativeRules.GridX(40f + zombie.TargetColumn * 80f, AdventureDancerRules.Scale(zombie));
                    zombie.PresentationPreviousX = zombie.X;
                    return;
                }
            }
        }

        /// <summary>Checks the original MakesSun plant types used by bungee target selection.</summary>
        /// <param name="kind">The candidate original plant type.</param>
        /// <returns>Whether the plant produces sun.</returns>
        internal static bool BungeeSunProducer(PlantKind kind)
        {
            return kind == PlantKind.Sunflower || kind == PlantKind.TwinSunflower || kind == PlantKind.SunShroom;
        }

        /// <summary>Advances PC 540C20/540980's original bungee altitude, phase counters and capture lifecycle.</summary>
        /// <param name="zombie">The live bungee after immobilization checks.</param>
        internal void AdvanceAdventureBungee(Zombie zombie)
        {
            if (zombie.Health <= 0f)
            {
                zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                _zombieLifecycle.KillAdventureZombie(zombie);
                return;
            }

            var state = zombie.BungeeState ??= _world.LifetimePool.Rent<AdventureBungeeState>();
            if (state.TargetPlant != null && !_world.Plants.Contains(state.TargetPlant))
            {
                state.TargetPlant = null;
            }

            if (state.PhaseTicks > 0)
            {
                state.PhaseTicks--;
            }

            if (state.Phase == 4 || state.Phase == 5)
            {
                var previous = zombie.PresentationAltitude;
                zombie.PresentationAltitude -= 8f;
                if (previous > 2596f && zombie.PresentationAltitude <= 2596f && zombie.LinkedZombieId == 0)
                {
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(74);
                }

                if (state.Phase == 4 && zombie.PresentationAltitude < 1500f && _adventureModeRunner.Adventure.Level.Mode != AdventureMode.Boss)
                {
                    state.Phase = 5;
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(44);
                }

                if (zombie.PresentationAltitude <= 40f)
                {
                    Plant? umbrella = null;
                    foreach (var plant in _world.Plants)
                    {
                        if (plant.Kind == PlantKind.UmbrellaLeaf && plant.Health > 0f && plant.BungeeState != 2
                            && Math.Abs(plant.Row - zombie.Row) <= 1 && Math.Abs(plant.Column - zombie.TargetColumn) <= 1)
                        {
                            umbrella = plant;
                            break;
                        }
                    }

                    if (umbrella != null)
                    {
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(-6);
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(73);
                        if (umbrella.PresentationUmbrellaAge < 0f)
                        {
                            umbrella.PresentationUmbrellaAge = 0f;
                        }

                        state.HitUmbrella = true;
                        state.Phase = 8;
                    }
                    else if (zombie.PresentationAltitude <= 0f)
                    {
                        zombie.PresentationAltitude = 0f;
                        Zombie? passenger = null;
                        foreach (var other in _world.Zombies)
                        {
                            if (other.Id == zombie.LinkedZombieId)
                            {
                                passenger = other;
                                break;
                            }
                        }

                        if (passenger != null)
                        {
                            passenger.BungeeCarrierId = 0;
                            passenger.DropRemaining = 0f;
                            passenger.PresentationNativeHeight = 0;
                            passenger.PresentationAnimation = string.Empty;
                            _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(passenger);
                            zombie.LinkedZombieId = 0;
                            state.CarriesZombie = false;
                            state.Enter(8);
                        }
                        else
                        {
                            state.Enter(6, 300, 5);
                        }
                    }
                }
            }
            else if (state.Phase == 6 && state.PhaseTicks == 0)
            {
                state.Enter(7, 0, 20);
                var target = _biteSystem.AdventureAirAttackPlantAt(zombie.Row, zombie.TargetColumn);
                if (target != null && target.Kind != PlantKind.CobCannon && target.Kind != PlantKind.GraveBuster
                    && !target.PresentationBiteNotOnGround)
                {
                    state.TargetPlant = target;
                    target.BungeeState = 1;
                }
            }
            else if (state.Phase == 7 && state.AnimationSeconds * 24f >= 15f)
            {
                var target = state.TargetPlant;
                if (target != null)
                {
                    foreach (var other in _world.Zombies)
                    {
                        if (other != zombie && other.BungeeState?.TargetPlant == target)
                        {
                            other.BungeeState.TargetPlant = null;
                        }
                    }

                    target.BungeeState = 2;
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(68);
                    if (target.Kind == PlantKind.Cattail && _boardQueries.AdventurePlantAt(target.Row, target.Column, PlantLayer.Armor) != null)
                    {
                        var previous = _world.Presentation._nativePlantingEffects;
                        _world.Presentation._nativePlantingEffects = true;
                        try
                        {
                            _plantingSystem.PlaceAdventurePlant(PlantKind.LilyPad, target.Row, target.Column);
                        }
                        finally
                        {
                            _world.Presentation._nativePlantingEffects = previous;
                        }
                    }
                }

                state.Enter(8);
            }
            else if (state.Phase == 8)
            {
                zombie.PresentationAltitude += 8f;
                if (zombie.PresentationAltitude >= 600f)
                {
                    zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                    _zombieLifecycle.KillAdventureZombie(zombie);
                    return;
                }
            }

            zombie.PresentationNativePhase = state.Phase;
            zombie.PresentationAnimation = state.Animation;
            zombie.PresentationAnimationAge = state.AnimationSeconds;
        }

        /// <summary>Restores a grabbed plant on interruption, removes lifted cargo, and releases no suspended zombie on carrier death.</summary>
        /// <param name="zombie">The departing or dying bungee whose ownership must be cleared.</param>
        internal void ReleaseAdventureBungeeCargo(Zombie zombie)
        {
            var state = zombie.BungeeState;
            var plant = state?.TargetPlant;
            if (plant != null && _world.Plants.Contains(plant))
            {
                if (plant.BungeeState == 2)
                {
                    _plantingSystem.RemoveAdventurePlantAndStack(plant);
                }
                else
                {
                    plant.BungeeState = 0;
                }
            }

            if (state != null)
            {
                state.TargetPlant = null;
            }

            foreach (var passenger in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                if (zombie.LinkedZombieId != 0 && passenger.Id == zombie.LinkedZombieId
                    || zombie.Id != 0 && passenger.BungeeCarrierId == zombie.Id)
                {
                    passenger.BungeeCarrierId = 0;
                    passenger.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                    _zombieLifecycle.KillAdventureZombie(passenger);
                }
            }

            zombie.LinkedZombieId = 0;
        }
    }
}
