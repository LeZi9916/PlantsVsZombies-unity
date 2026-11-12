#nullable enable

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>
    /// Replaceable approximations for the incomplete 1-1 experiment. None of
    /// these defaults is a claim about the original game's complete behavior.
    /// Values and callbacks are captured when the simulation is constructed.
    /// </summary>
    public sealed class ExperimentalLevelOneOptions
    {
        // The conversion from the observed native counters to seconds is not verified.
        /// <summary>Stores seconds per native tick.</summary>
        public double SecondsPerNativeTick = 0.01;
        /// <summary>Stores initial wave delay seconds.</summary>
        public double InitialWaveDelaySeconds = 18.0;
        /// <summary>Stores between wave delay seconds.</summary>
        public double BetweenWaveDelaySeconds = 25.0;
        /// <summary>Stores zombie spawn x.</summary>
        public float ZombieSpawnX = 9.5f;
        /// <summary>Stores zombie spacing in wave.</summary>
        public float ZombieSpacingInWave = 0.45f;
        /// <summary>Stores zombie health.</summary>
        public float ZombieHealth = 270f;
        /// <summary>Stores sky sun target y.</summary>
        public float SkySunTargetY = 2.5f;
        /// <summary>Stores allow plant removal.</summary>
        public bool AllowPlantRemoval = false;
        /// <summary>
        /// Opt in to the separate pixel-space sky-sun model. When enabled,
        /// SkySunInterval, SkySunTargetY and SkySunFactory do not schedule sky
        /// suns; legacy and plant suns in GameSimulation.Suns remain unchanged.
        /// </summary>
        public bool UseNativeSkySunRules = false;
        /// <summary>
        /// Optional bounded draw source for native sky rules. Null uses the
        /// simulation's shared System.Random. Each result must be in [0, bound).
        /// This does not reproduce the EXE's global RNG sequence.
        /// </summary>
        public Func<int, int>? NativeSkySunRandomDraw;
        /// <summary>
        /// Optional replacement for the fixed inter-wave delay. Called after a
        /// batch is dispatched with its zero-based index, except after the last
        /// batch. The result must be finite and nonnegative; zero still waits
        /// until the next simulation tick.
        /// </summary>
        public Func<GameSimulation, int, double>? BetweenWaveDelay;
        /// <summary>
        /// Optional completion policy, evaluated only after all four batches
        /// have been dispatched. Null uses the provisional immediate-win rule:
        /// the simulation's zombie list is empty. No reward is created.
        /// </summary>
        public Func<GameSimulation, bool>? CompletionCondition;
        /// <summary>
        /// Optional replacement for the approximate single-row sky-sun
        /// creation. It must return a non-null sun with finite coordinates.
        /// When native sky rules are disabled, Rules.SkySunInterval controls
        /// repetition and can disable sky sun.
        /// The supplied Random is the simulation's shared random source.
        /// </summary>
        public Func<GameSimulation, Random, global::PlantsVsZombies.Simulation.Sun>? SkySunFactory;
    }
}
