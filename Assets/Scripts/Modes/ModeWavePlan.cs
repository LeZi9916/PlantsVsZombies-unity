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
            // PC Board::InitSurvivalStage 0x41693F generates the re-selection table before 0x41694A clears
            // the board wave index, so the finished round's last wave is still visible to 0x41F370 there.
            var previousWaveIndex = stageIndex > 0 ? session.Definition.WaveCount : 0;
            var enabled = ModeWaveRules.BuildAllowed(session.Definition, stageIndex, terrain, session.WaveContext, previousWaveIndex);
            Array.Copy(enabled, session.Game.Adventure.EnabledZombieTypes, enabled.Length);
            return ModeWaveRules.Build(session.Definition, stageIndex, terrain, session.WaveContext, random, enabled, previousWaveIndex);
        }
    }
}
