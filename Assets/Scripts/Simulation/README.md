# Deterministic simulation ownership

`GameSimulation` is the gameplay facade and composition root. Its public commands
forward to explicitly connected collaborators; it does not dispatch species
behavior. `SimulationWorld` owns match state, random sources, ordered collections,
and bounded scratch pools. Collaborators receive the world and named dependencies,
never the facade. Constructors allocate state; `Connect` completes the dependency
graph before `SimulationSession.Initialize` consumes any random draws or runs rules.

## Update order

`LegacySimulation`, `AdventureModeRunner`, `LevelOneModeRunner`, and
`IndependentModeRunner` retain the original mode-specific ordering and early exits.
The two frame accumulators use `FixedStepClock` with the same 0.01-second step,
epsilon, remainder, and stop checks. Seed cooldown advancement is shared through
`SeedBankSystem`; each caller explicitly selects whether native feedback is emitted.

Keep initialization, random consumption, presentation clocks, plants, projectiles,
zombies, purchases, mowers, rewards, and mode transitions in their existing slots.
A new abstraction must not reorder operations merely because they look similar.
In particular, a dying entity can retire other entities during the current update.
`PooledSnapshot` preserves the captured sequence while membership checks observe
immediate removals. New actors do not enter an already captured traversal.

## Entity behavior and lifecycle

`Behaviors/Plants` and `Behaviors/Zombies` contain explicit factories and capability
owners. Shared shooter timing, movement, equipment, combat, and target algorithms
remain reusable collaborators. Behaviors own specialty state such as chomper
phases, cannon reloads, multi-head clocks, giant actions, and thrown-imp motion.
The entity's presentation-facing state properties project those owned records;
reading an absent capability does not allocate a behavior.

Plant hooks run initialization, native actions, growth gating, post-countdown
special actions, and ready actions in order. Zombie action phases identify the
original interleaving points among shared freezing, pool, movement, and biting
rules. The phase names are ordering contracts, not independently scheduled updates.

`PlantingSystem`, `CombatSystem`, `ZombieLifecycle`, and projectile/resource owners
retain the existing removal and side-effect rules. Removing a stored reference is
not equivalent to killing it: upgrades, imitation, stage cleanup, bungee release,
linked sled deaths, and ordinary damage have different effects and notifications.
Plants and zombies are not pooled because corpses and linked effects retain them.

## Collections and queries

All public collection properties return cached `EntityView<T>` instances. They
support ordered reads and struct enumeration without exposing mutation methods.
Actor scalar properties can be changed only inside the Runtime assembly.

`EntityStore<T>` retains native list order, live membership, and ID/lane indexes.
Indexed properties invalidate the owner's version. The next indexed query
synchronizes before returning candidates, so callers never observe stale results.
Rebuilds reuse buffers and preserve global insertion order, including lane changes,
ID replacement, and ties. Index membership does not imply target eligibility.

`PlantCellIndex` retains footprint candidates for the native six-by-nine maximum
board, including every layer and both cannon cells. Queries still apply live
bungee, airborne squash, and layer conditions. Cross-row and boss queries retain
their broader candidate rules. The existing enum IDs and native data tables remain
unchanged.

`ScratchBufferPool<T>` rents distinct lists for nested operations. Use one lease per
`using` scope and do not copy or dispose it twice. Returned lists clear references;
retention is limited to eight lists and 4096 elements per list capacity. Ordinary
projectile, sun, and flame pools retain their existing reset and retirement rules.

## Presentation reads

`AdventurePresentationZombieAnimation` and `AdventurePresentationZombieRate` are
read-only queries. Native transition sites explicitly call the corresponding
synchronization operations. The presenter must not update live actor clocks,
animation names, movement phases, or random state.

A `Presentation` prefix does not imply cosmetic state: ground-track displacement,
attack markers, freeze clocks, and throw events still belong to deterministic
simulation. Renderer-only interpolation consumes their results without advancing
them.

## API migration and tests

Use gameplay commands such as `TryPlant`, `RemovePlant`, `CollectSun`, and mode
operations instead of mutating public lists. Isolated tests and smoke fixtures can
use the internal `SimulationFixture` entry points; raw fixture removal intentionally
does not produce gameplay death effects. `SimulationInspection` resolves legacy
fixture operations on their actual collaborator rather than relying on private
methods remaining on the facade.

Experimental level-one callbacks now receive `SimulationView`, exposing plants,
zombies, elapsed time, match state, and sun amount. Public fields migrated to
properties; reflection clients must use properties or the fixture inspection API.
Native species IDs, profile formats, scene bindings, and existing asset GUIDs are
unchanged.

`SimulationArchitectureTests` covers collection ownership, footprint and lane
invalidation, stable target ordering, replacement IDs, behavior-owned state,
clock stopping, nested scratch buffers, read-only queries, and warmed allocation.
`GamePresentationPerformanceTests.RepeatedZombieDrawsDoNotMutateSimulationState`
checks the real draw path. Existing species tests retain their assertions and use
explicit fixture synchronization when they deliberately change action state.

Run Unity tests through `Tools/Tests/Run-UnityTests.ps1` with Unity 7000.0.0a16.
`Tools/Tests/SimulationParityTests` compares compiled baseline and current pure
simulation assemblies, including native state and random snapshots. These checks
are distinct from original-artwork, Player, and target-device validation.
