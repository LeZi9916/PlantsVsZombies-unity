#nullable enable

using PlantsVsZombies.Randomness;
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SimulationRandom responsibilities for one deterministic match.</summary>
    internal sealed class SimulationRandom
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal SimulationRandom(SimulationWorld world)
        {
            _world = world;
        }

        /// <summary>Advances one original app update while an opening or dialogue owns the board clock.</summary>
        internal void AdvanceNativeFeedbackClock()
        {
            _world._nativeFeedbackTick++;
        }

        /// <summary>Stops an explicitly stopped original Foley voice.</summary>
        /// <param name="foley">The original category being stopped.</param>
        internal void StopAdventureFoley(int foley)
        {
            _world._foleyRandom.Stop(foley, _world._nativeFeedbackTick);
        }

        /// <summary>Consumes an original bounded draw for an explicit native opening or UI action.</summary>
        /// <param name="maximum">The original exclusive integer bound.</param>
        /// <returns>The next original remainder-selected integer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The exclusive bound is negative.</exception>
        internal int DrawAdventureRandomInt(int maximum)
        {
            return _world._random.Next(maximum);
        }

        /// <summary>Consumes an original scaled float draw for an explicit native opening action.</summary>
        /// <param name="scale">The original single-precision scale.</param>
        /// <returns>The scaled original draw after the native float store.</returns>
        internal float DrawAdventureRandomFloat(float scale)
        {
            return AdventureSpawnRandom.NextFloat(_world._random, scale);
        }

        /// <summary>Resolves an explicit native UI Foley request using the active game's shared state.</summary>
        /// <param name="sound">The original request receiving its admitted playback choices.</param>
        internal void ResolveAdventureFoley(AdventureZombieSoundEvent sound)
        {
            sound.Tick = Math.Max(sound.Tick, _world._nativeFeedbackTick);
            _world._foleyRandom.Resolve(sound, _world._random, _world.Rules.AdventureSoundMuted);
        }

        /// <summary>Gets or sets battle random draw count.</summary>
        internal ulong BattleRandomDrawCount
        {
            get
            {
                var original = _world._random as OriginalRandomAdapter;
                return original == null ? 0 : original.Generator.DrawCount;
            }
        }

        /// <summary>Performs capture battle random state.</summary>
        /// <returns>The resulting capture battle random state.</returns>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal OriginalRandomState CaptureBattleRandomState()
        {
            var original = _world._random as OriginalRandomAdapter;
            if (original == null)
            {
                throw new InvalidOperationException("GameSimulation must use OriginalRandomAdapter.");
            }

            return original.CaptureState();
        }

        /// <summary>Performs restore battle random state.</summary>
        /// <param name = "snapshot">The snapshot.</param>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void RestoreBattleRandomState(OriginalRandomState snapshot)
        {
            var original = _world._random as OriginalRandomAdapter;
            if (original == null)
            {
                throw new InvalidOperationException("GameSimulation must use OriginalRandomAdapter.");
            }

            original.RestoreState(snapshot);
        }
    }
}
