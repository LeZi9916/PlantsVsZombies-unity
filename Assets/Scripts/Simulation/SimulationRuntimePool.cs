#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Recycles model records only after explicit board roots and linked records release them.</summary>
    /// <remarks>Two complete facade updates separate the last reference from reuse. Nested fixed ticks and
    /// mutation-safe snapshots finish before this sweep, so removal alone never resets a captured actor.</remarks>
    internal sealed class SimulationRuntimePool
    {
        /// <summary>Separates retained consumed sound history from short-lived visual event capacity.</summary>
        private const int SoundCategory = 4;
        /// <summary>Reserves new requests while cleared history waits for two completed update boundaries.</summary>
        private const int SoundGraceReserve = 256;
        /// <summary>Indexes concrete type pools created once during match loading.</summary>
        private readonly Dictionary<Type, Bucket> _buckets = new Dictionary<Type, Bucket>(96);
        /// <summary>Retains deterministic preparation order.</summary>
        private readonly List<Bucket> _ordered = new List<Bucket>(96);
        /// <summary>Tracks only rented or adopted model records, using reference identity.</summary>
        private readonly Dictionary<ISimulationPooledObject, Entry> _entries = new Dictionary<ISimulationPooledObject, Entry>(4096, ReferenceComparer.Instance);
        /// <summary>Tracks rented records independently of inactive capacity.</summary>
        private readonly List<Entry> _active = new List<Entry>(4096);
        /// <summary>Identifies completed facade update boundaries.</summary>
        private long _epoch;
        /// <summary>Tracks staged reusable collection preparation, excluded with this sidecar.</summary>
        private int _collectionStep;
        /// <summary>The requirements whose collection storage is currently being prepared.</summary>
        private SimulationCapacityRequirements _collectionRequirements;
        /// <summary>Whether a collection preparation request has been admitted.</summary>
        private bool _hasCollectionRequirements;
        /// <summary>The next restored sound record adopted outside the gameplay hot path.</summary>
        private int _soundHistoryPreparationIndex;
        /// <summary>Counts safe returns without allocating a statistics snapshot.</summary>
        internal long RecycledCount { get; private set; }
        /// <summary>Gets the number of records still protected by active or delayed ownership.</summary>
        internal int ActiveCount => _active.Count;
        /// <summary>Gets the total number constructed by the model pools.</summary>
        internal int CreatedCount
        {
            get
            {
                var count = 0;
                foreach (var bucket in _ordered)
                {
                    count += bucket.Created;
                }
                return count;
            }
        }
        /// <summary>Registers supported runtime record types without consuming any random stream.</summary>
        internal SimulationRuntimePool()
        {
            RegisterTypes();
        }

        /// <summary>Declares typed pools once; this method contains no gameplay initialization.</summary>
        private void RegisterTypes()
        {
            Add<AdventurePlantZombieState>(3, 128);
            Add<AdventureMagnetItem>(3, 128);
            Add<AdventureImpact>(2, 512);
            Add<AdventureSnowTrail>(2, 512);
            Add<AdventurePlantBiteEffect>(2, 512);
            Add<AdventureZombieCorpse>(2, 512);
            Add<AdventureCoin>(2, 512);
            Add<AdventureExplosion>(2, 512);
            Add<AdventureZombiePartDrop>(2, 512);
            Add<AdventureZombieArmDrop>(2, 512);
            Add<AdventureZombieSoundEvent>(SoundCategory, SimulationCapacityRequirements.MaximumSoundEvents);
            Add<AdventureZombieRiseEffect>(2, 512);
            Add<AdventureZombieSplash>(2, 512);
            Add<AdventureBloverState>(3, 128);
            Add<AdventureBobsledState>(3, 128);
            Add<AdventureBungeeState>(3, 128);
            Add<AdventureCactusState>(3, 128);
            Add<AdventureCatapultState>(3, 128);
            Add<AdventureCherryBombState>(3, 128);
            Add<AdventureChomperState>(3, 128);
            Add<AdventureCoffeeBeanState>(3, 128);
            Add<AdventureDancerState>(3, 128);
            Add<AdventureDiggerState>(3, 128);
            Add<AdventureDolphinState>(3, 128);
            Add<AdventureDoomShroomState>(3, 128);
            Add<AdventureGargantuarState>(3, 128);
            Add<AdventureJalapenoState>(3, 128);
            Add<AdventureMagnetState>(3, 128);
            Add<AdventurePogoState>(3, 128);
            Add<AdventurePotatoMineState>(3, 128);
            Add<AdventureScaredyShroomState>(3, 128);
            Add<AdventureSleepingState>(3, 128);
            Add<AdventureSquashState>(3, 128);
            Add<AdventureTangleKelpState>(3, 128);
            Add<AdventureThrownImpState>(3, 128);
            Add<AdventureZombiePoolState>(3, 128);
            Add<AdventureZomboniState>(3, 128);
            Add<Plant>(0, 512);
            Add<Zombie>(1, 1024);
            Add<BloverPlantBehavior>(3, 128);
            Add<CactusPlantBehavior>(3, 128);
            Add<CherryBombPlantBehavior>(3, 128);
            Add<ChomperPlantBehavior>(3, 128);
            Add<CoffeeBeanPlantBehavior>(3, 128);
            Add<DoomShroomPlantBehavior>(3, 128);
            Add<GraveBusterPlantBehavior>(3, 128);
            Add<IceShroomPlantBehavior>(3, 128);
            Add<JalapenoPlantBehavior>(3, 128);
            Add<MagnetShroomPlantBehavior>(3, 128);
            Add<MarigoldPlantBehavior>(3, 128);
            Add<PlantBehavior>(3, 128);
            Add<PotatoMinePlantBehavior>(3, 128);
            Add<ScaredyPlantBehavior>(3, 128);
            Add<ShooterPlantBehavior>(3, 128);
            Add<SpikeweedPlantBehavior>(3, 128);
            Add<SplitPeaPlantBehavior>(3, 128);
            Add<SquashPlantBehavior>(3, 128);
            Add<StarfruitPlantBehavior>(3, 128);
            Add<SunflowerPlantBehavior>(3, 128);
            Add<SunShroomPlantBehavior>(3, 128);
            Add<TangleKelpPlantBehavior>(3, 128);
            Add<ThreepeaterPlantBehavior>(3, 128);
            Add<TimedPlantBehavior>(3, 128);
            Add<TorchwoodPlantBehavior>(3, 128);
            Add<UpgradePlantBehavior>(3, 128);
            Add<BalloonZombieBehavior>(3, 128);
            Add<BobsledZombieBehavior>(3, 128);
            Add<BungeeZombieBehavior>(3, 128);
            Add<CatapultZombieBehavior>(3, 128);
            Add<DancerZombieBehavior>(3, 128);
            Add<DiggerZombieBehavior>(3, 128);
            Add<DolphinZombieBehavior>(3, 128);
            Add<GargantuarZombieBehavior>(3, 128);
            Add<JackInTheBoxZombieBehavior>(3, 128);
            Add<LadderZombieBehavior>(3, 128);
            Add<NewspaperZombieBehavior>(3, 128);
            Add<PogoZombieBehavior>(3, 128);
            Add<PoleVaulterZombieBehavior>(3, 128);
            Add<SnorkelZombieBehavior>(3, 128);
            Add<ThrownImpZombieBehavior>(3, 128);
            Add<WalkingZombieBehavior>(3, 128);
            Add<ZomboniZombieBehavior>(3, 128);
        }

        /// <summary>Registers a bounded pool for one concrete model type.</summary>
        /// <typeparam name="T">The resettable model record.</typeparam>
        /// <param name="category">The preparation requirement applied to this type.</param>
        /// <param name="capacity">The maximum inactive records retained after a burst.</param>
        private void Add<T>(int category, int capacity) where T : class, ISimulationPooledObject, new()
        {
            var bucket = new TypedBucket<T>(this, category, capacity);
            _buckets.Add(typeof(T), bucket);
            _ordered.Add(bucket);
        }

        /// <summary>Rents one reset record and tracks it until all board references disappear.</summary>
        /// <typeparam name="T">The concrete supported record type.</typeparam>
        /// <returns>A record with construction defaults.</returns>
        internal T Rent<T>() where T : class, ISimulationPooledObject, new()
        {
            var bucket = (TypedBucket<T>)_buckets[typeof(T)];
            var item = bucket.Pool.Rent();
            if (!_entries.TryGetValue(item, out var entry))
            {
                entry = new Entry(item, bucket);
                _entries.Add(item, entry);
            }
            entry.LastMarked = _epoch;
            entry.UnreferencedSince = -1;
            entry.InUse = true;
            bucket.Active++;
            _active.Add(entry);
            Attach(item);
            return item;
        }

        /// <summary>Connects lazily created actor capabilities to this match-local owner.</summary>
        /// <param name="item">The newly rented or restored record.</param>
        private void Attach(ISimulationPooledObject item)
        {
            if (item is Plant plant)
            {
                plant.RuntimePool = this;
            }
            else if (item is Zombie zombie)
            {
                zombie.RuntimePool = this;
            }
        }

        /// <summary>Preallocates lifetime metadata with its record, outside the active game loop.</summary>
        /// <param name="item">The newly constructed inactive record.</param>
        /// <param name="bucket">The corresponding concrete pool.</param>
        private void RegisterPrepared(ISimulationPooledObject item, Bucket bucket)
        {
            _entries.Add(item, new Entry(item, bucket));
        }

        /// <summary>Marks a record and its explicitly declared references once per facade update.</summary>
        /// <param name="value">The referenced actor, capability or short-lived event, or null.</param>
        internal void Mark(object? value)
        {
            if (!(value is ISimulationPooledObject item))
            {
                return;
            }
            if (!_entries.TryGetValue(item, out var entry))
            {
                // Restored and fixture-created records use the same lifecycle without changing their values.
                var bucket = _buckets[item.GetType()];
                entry = new Entry(item, bucket) { InUse = true };
                _entries.Add(item, entry);
                _active.Add(entry);
                bucket.Active++;
                Attach(item);
            }
            if (entry.LastMarked == _epoch)
            {
                return;
            }
            entry.LastMarked = _epoch;
            entry.UnreferencedSince = -1;
            item.MarkPoolReferences(this);
        }

        /// <summary>Marks every authoritative root and recycles only records released for two full updates.</summary>
        /// <param name="world">The completed match whose snapshots and callbacks have returned.</param>
        internal void CompleteFrame(SimulationWorld world)
        {
            _epoch++;
            foreach (var plant in world.Plants)
            {
                Mark(plant);
            }
            foreach (var zombie in world.Zombies)
            {
                Mark(zombie);
            }
            foreach (var projectile in world.Peas)
            {
                Mark(projectile.PresentationSnowTrail);
            }
            var presentation = world.Presentation;
            MarkList(presentation.Impacts);
            MarkList(presentation.Corpses);
            MarkList(presentation.SnowTrails);
            MarkList(presentation.PlantBiteEffects);
            MarkList(presentation.Explosions);
            MarkList(presentation.ZombiePartDrops);
            MarkList(presentation.ArmDrops);
            MarkList(presentation.ZombieSounds);
            MarkList(presentation.ZombieRiseEffects);
            MarkList(presentation.ZombieSplashes);
            foreach (var squished in presentation.SquishedPlants)
            {
                Mark(squished.Plant);
            }
            if (world._adventure != null)
            {
                MarkList(world._adventure._coins);
            }
            for (var index = _active.Count - 1; index >= 0; index--)
            {
                var entry = _active[index];
                if (entry.LastMarked == _epoch)
                {
                    continue;
                }
                if (entry.UnreferencedSince < 0)
                {
                    entry.UnreferencedSince = _epoch;
                    continue;
                }
                if (_epoch - entry.UnreferencedSince < 2)
                {
                    continue;
                }
                entry.Item.ResetForPool();
                entry.InUse = false;
                entry.Bucket.Active--;
                if (!entry.Bucket.Return(entry.Item))
                {
                    _entries.Remove(entry.Item);
                }
                var last = _active.Count - 1;
                _active[index] = _active[last];
                _active.RemoveAt(last);
                RecycledCount++;
            }
        }

        /// <summary>Marks a concrete list without boxing its enumerator.</summary>
        /// <typeparam name="T">The live presentation record type.</typeparam>
        /// <param name="items">The authoritative ordered records.</param>
        private void MarkList<T>(List<T> items) where T : class
        {
            for (var index = 0; index < items.Count; index++)
            {
                Mark(items[index]);
            }
        }

        /// <summary>Prepares bounded typed capacity in deterministic order.</summary>
        /// <param name="requirements">The requested reusable high-water counts.</param>
        /// <param name="maxObjects">The maximum objects constructed during this call.</param>
        /// <returns>Whether every pool has reached the requested capacity.</returns>
        internal bool PrepareCapacityStep(in SimulationCapacityRequirements requirements, int maxObjects)
        {
            var remaining = Math.Max(0, maxObjects);
            var complete = true;
            foreach (var bucket in _ordered)
            {
                var available = RequiredAvailability(bucket, requirements);
                remaining -= bucket.Prepare(available, remaining);
                complete &= bucket.Available >= available;
            }
            return complete;
        }

        /// <summary>Gets the configured total target for one concrete record category.</summary>
        /// <param name="bucket">The concrete record category.</param>
        /// <param name="requirements">The caller's bounded capacity targets.</param>
        /// <returns>The requested live-plus-inactive target.</returns>
        private static int CategoryTarget(Bucket bucket, in SimulationCapacityRequirements requirements)
        {
            if (bucket.Category == 0)
            {
                return requirements.Plants;
            }
            if (bucket.Category == 1)
            {
                return requirements.Zombies;
            }
            if (bucket.Category == 2)
            {
                return requirements.Events;
            }
            if (bucket.Category == SoundCategory)
            {
                return requirements.SoundEvents;
            }
            return requirements.Capabilities;
        }

        /// <summary>Reserves sound grace capacity even when restored or active history already exceeds its total target.</summary>
        /// <param name="bucket">The concrete record pool.</param>
        /// <param name="requirements">The bounded caller targets.</param>
        /// <returns>An inactive target that never exceeds the registered retained capacity.</returns>
        private static int RequiredAvailability(Bucket bucket, in SimulationCapacityRequirements requirements)
        {
            var target = CategoryTarget(bucket, requirements);
            var available = Math.Max(0, target - bucket.Active);
            if (bucket.Category == SoundCategory)
            {
                available = Math.Max(available, Math.Min(SoundGraceReserve, target));
            }
            return available;
        }

        /// <summary>Checks every capacity field without boxing a requirements value.</summary>
        /// <param name="left">The previous request.</param>
        /// <param name="right">The current request.</param>
        /// <returns>Whether the same collection capacities remain applicable.</returns>
        private static bool SameRequirements(in SimulationCapacityRequirements left, in SimulationCapacityRequirements right)
        {
            return left.Plants == right.Plants && left.Zombies == right.Zombies
                && left.Projectiles == right.Projectiles && left.Events == right.Events
                && left.Capabilities == right.Capabilities && left.SoundEvents == right.SoundEvents;
        }

        /// <summary>Reserves ownership indices for every prepared record and sound-history adoption.</summary>
        /// <param name="world">The board whose restored history remains authoritative.</param>
        /// <param name="requirements">The requested bounded record counts.</param>
        private void PrepareEntryIndices(SimulationWorld world, in SimulationCapacityRequirements requirements)
        {
            var capacity = 0;
            foreach (var bucket in _ordered)
            {
                var total = Math.Max(bucket.Active + bucket.Available, CategoryTarget(bucket, requirements));
                if (bucket.Category == SoundCategory)
                {
                    var live = Math.Max(bucket.Active, world.Presentation.ZombieSounds.Count);
                    total = Math.Max(total, live + Math.Min(SoundGraceReserve, requirements.SoundEvents));
                }
                capacity += total;
            }
            _entries.EnsureCapacity(capacity);
            PrepareList(_active, capacity);
        }

        /// <summary>Gets inactive sound capacity without allocating a diagnostic snapshot.</summary>
        internal int AvailableSoundEvents => _buckets[typeof(AdventureZombieSoundEvent)].Available;

        /// <summary>Gets sound records retained by active history or grace-period ownership.</summary>
        internal int ActiveSoundEvents => _buckets[typeof(AdventureZombieSoundEvent)].Active;

        /// <summary>Prepares one collection owner per loading call before pool population begins.</summary>
        /// <param name="world">The match whose logical collection contents remain unchanged.</param>
        /// <param name="requirements">The requested high-water counts.</param>
        /// <returns>Whether every collection owner has been prepared.</returns>
        internal bool PrepareCollectionsStep(SimulationWorld world, in SimulationCapacityRequirements requirements)
        {
            if (!_hasCollectionRequirements || !SameRequirements(_collectionRequirements, requirements))
            {
                _collectionRequirements = requirements;
                _hasCollectionRequirements = true;
                _collectionStep = 0;
                _soundHistoryPreparationIndex = 0;
            }
            var presentation = world.Presentation;
            switch (_collectionStep++)
            {
                case 0:
                    world.Plants.PrepareCapacity(requirements.Plants, world.RowCount);
                    break;
                case 1:
                    world.Zombies.PrepareCapacity(requirements.Zombies, world.RowCount);
                    break;
                case 2:
                    PrepareList(world.Peas, requirements.Projectiles);
                    break;
                case 3:
                    PrepareList(world.Suns, 64);
                    break;
                case 4:
                    PrepareList(presentation.Impacts, requirements.Events);
                    break;
                case 5:
                    PrepareList(presentation.Corpses, requirements.Zombies);
                    break;
                case 6:
                    PrepareList(presentation.SnowTrails, requirements.Events);
                    break;
                case 7:
                    PrepareList(presentation.PlantBiteEffects, requirements.Events);
                    break;
                case 8:
                    PrepareList(presentation.Explosions, requirements.Events);
                    break;
                case 9:
                    PrepareList(presentation.ZombiePartDrops, requirements.Events);
                    break;
                case 10:
                    PrepareList(presentation.ArmDrops, requirements.Events);
                    break;
                case 11:
                    PrepareList(presentation.ZombieSounds, Math.Max(requirements.SoundEvents,
                        presentation.ZombieSounds.Count + Math.Min(SoundGraceReserve, requirements.SoundEvents)));
                    break;
                case 12:
                    PrepareList(presentation.ZombieRiseEffects, requirements.Events);
                    break;
                case 13:
                    PrepareList(presentation.ZombieSplashes, requirements.Events);
                    break;
                case 14:
                    if (world._adventure != null)
                    {
                        PrepareList(world._adventure._coins, requirements.Events);
                    }
                    break;
                case 15:
                    PrepareEntryIndices(world, requirements);
                    break;
                case 16:
                    if (_soundHistoryPreparationIndex < presentation.ZombieSounds.Count)
                    {
                        Mark(presentation.ZombieSounds[_soundHistoryPreparationIndex++]);
                        _collectionStep = 16;
                        return false;
                    }
                    break;
                default:
                    _collectionStep = 17;
                    return true;
            }
            return false;
        }

        /// <summary>Reserves one list's storage without constructing logical entries.</summary>
        /// <typeparam name="T">The list element type.</typeparam>
        /// <param name="items">The authoritative list.</param>
        /// <param name="capacity">The requested minimum capacity.</param>
        private static void PrepareList<T>(List<T> items, int capacity)
        {
            if (items.Capacity < capacity)
            {
                items.Capacity = capacity;
            }
        }

        /// <summary>Owns one stable record identity and its delayed retirement state.</summary>
        private sealed class Entry
        {
            /// <summary>The resettable record.</summary>
            internal readonly ISimulationPooledObject Item;
            /// <summary>The type-specific inactive owner.</summary>
            internal readonly Bucket Bucket;
            /// <summary>The latest frame which observed a board reference.</summary>
            internal long LastMarked = -1;
            /// <summary>The first completed update without an authoritative reference.</summary>
            internal long UnreferencedSince = -1;
            /// <summary>Whether the record is currently rented.</summary>
            internal bool InUse;
            /// <summary>Creates runtime-only tracking metadata.</summary>
            /// <param name="item">The tracked record.</param>
            /// <param name="bucket">The typed inactive owner.</param>
            internal Entry(ISimulationPooledObject item, Bucket bucket)
            {
                Item = item;
                Bucket = bucket;
            }
        }

        /// <summary>Describes a typed pool without reflection during marking or preparation.</summary>
        private abstract class Bucket
        {
            /// <summary>Chooses plant, zombie, event or capability preparation counts.</summary>
            internal readonly int Category;
            /// <summary>Counts currently rented and adopted records.</summary>
            internal int Active;
            /// <summary>Gets inactive availability.</summary>
            internal abstract int Available { get; }
            /// <summary>Gets construction count.</summary>
            internal abstract int Created { get; }
            /// <summary>Creates category metadata.</summary>
            /// <param name="category">The preparation requirement category.</param>
            protected Bucket(int category)
            {
                Category = category;
            }
            /// <summary>Returns an unreferenced reset record.</summary>
            /// <param name="item">The retired record.</param>
            /// <returns>Whether the bounded owner retained it.</returns>
            internal abstract bool Return(ISimulationPooledObject item);
            /// <summary>Creates a bounded inactive prefix.</summary>
            /// <param name="target">The requested available count.</param>
            /// <param name="budget">The maximum construction count.</param>
            /// <returns>The number constructed.</returns>
            internal abstract int Prepare(int target, int budget);
        }

        /// <summary>Owns reusable records of one concrete type.</summary>
        /// <typeparam name="T">The concrete resettable type.</typeparam>
        private sealed class TypedBucket<T> : Bucket where T : class, ISimulationPooledObject, new()
        {
            /// <summary>Receives preparation metadata without allocating a callback per frame.</summary>
            private readonly Action<T> _prepared;
            /// <summary>Owns prepared record metadata.</summary>
            private readonly SimulationRuntimePool _owner;
            /// <summary>Retains inactive records.</summary>
            internal readonly SimulationObjectPool<T> Pool;
            /// <inheritdoc/>
            internal override int Available => Pool.AvailableCount;
            /// <inheritdoc/>
            internal override int Created => Pool.CreatedCount;
            /// <summary>Creates the bounded typed pool.</summary>
            /// <param name="category">The preparation category.</param>
            /// <param name="capacity">The retained inactive limit.</param>
            internal TypedBucket(SimulationRuntimePool owner, int category, int capacity) : base(category)
            {
                _owner = owner;
                _prepared = Prepared;
                Pool = new SimulationObjectPool<T>(0, capacity);
            }
            /// <inheritdoc/>
            internal override bool Return(ISimulationPooledObject item)
            {
                return Pool.TryReturn((T)item);
            }
            /// <inheritdoc/>
            internal override int Prepare(int target, int budget)
            {
                return Pool.PrepareCapacityStep(target, budget, _prepared);
            }
            /// <summary>Registers inactive metadata during budgeted preparation.</summary>
            /// <param name="item">The newly constructed record.</param>
            private void Prepared(T item)
            {
                _owner.RegisterPrepared(item, this);
            }
        }

        /// <summary>Uses reference identity for mutable model records.</summary>
        private sealed class ReferenceComparer : IEqualityComparer<ISimulationPooledObject>
        {
            /// <summary>Shares one stateless comparer.</summary>
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();
            /// <inheritdoc/>
            public bool Equals(ISimulationPooledObject? left, ISimulationPooledObject? right)
            {
                return ReferenceEquals(left, right);
            }
            /// <inheritdoc/>
            public int GetHashCode(ISimulationPooledObject value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }
    }
}
