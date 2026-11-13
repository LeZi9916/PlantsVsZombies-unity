#nullable enable

using PlantsVsZombies.Adventure;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Modes
{
    /// <summary>Connects original independent-mode wave rules to the active battle board.</summary>
    internal static class ModeWavePlan
    {
        /// <summary>Initializes eligibility once and generates the current stage using the combat random stream.</summary>
        /// <param name="session">The active independent-mode session.</param>
        /// <param name="random">The shared combat random source used by the native weighted selector.</param>
        /// <returns>The original wave table for this stage.</returns>
        internal static IReadOnlyList<AdventureWave> Build(ModeSession session, Random random)
        {
            var stageIndex = session.Stage - 1;
            var terrain = session.Game.Adventure.Level.Terrain;
            var enabled = ModeWaveRules.BuildAllowed(session.Definition, stageIndex, terrain, session.WaveContext);
            Array.Copy(enabled, session.Game.Adventure.EnabledZombieTypes, enabled.Length);
            return ModeWaveRules.Build(session.Definition, stageIndex, terrain, session.WaveContext, random, enabled);
        }
    }
}
