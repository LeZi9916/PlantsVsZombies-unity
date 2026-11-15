#nullable enable
using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Advances a caller-owned accumulator without changing native tick or stop ordering.</summary>
    internal static class FixedStepClock
    {
        /// <summary>Consumes complete native updates and preserves the fractional remainder.</summary>
        /// <param name="accumulated">The mode-specific accumulated seconds.</param>
        /// <param name="delta">The validated elapsed frame time.</param>
        /// <param name="enabled">Checks whether the next native update is still admitted.</param>
        /// <param name="advance">Runs one complete native update.</param>
        internal static void Advance(ref double accumulated, float delta, Func<bool> enabled, Action<float> advance)
        {
            accumulated += delta;
            while (accumulated + 0.0000001 >= SimulationWorld.SimulationStep && enabled())
            {
                advance((float)SimulationWorld.SimulationStep);
                accumulated -= SimulationWorld.SimulationStep;
                if (Math.Abs(accumulated) < 0.0000001)
                {
                    accumulated = 0d;
                }
            }
        }
    }
}
