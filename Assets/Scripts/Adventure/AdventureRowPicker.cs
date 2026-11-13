#nullable enable

using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains PC Board's six-lane smooth-pick history across waves and survival reselections.</summary>
    internal sealed class AdventureRowPicker
    {
        /// <summary>The fixed PC row-array length, including the unused sixth slot on five-row boards.</summary>
        internal const int RowCount = 6;

        /// <summary>The current legal-lane weights assigned by Board::PickRowForNewZombie.</summary>
        internal float[] Weights { get; } = new float[RowCount];

        /// <summary>The eligible-pick age since each row was last selected.</summary>
        internal float[] LastPicked { get; } = new float[RowCount];

        /// <summary>The eligible-pick age since each row was selected before its most recent selection.</summary>
        internal float[] SecondLastPicked { get; } = new float[RowCount];

        /// <summary>The zero-based wave at which each lane's mower was triggered, or minus one hundred initially.</summary>
        internal int[] LastMowedWave { get; } = new int[RowCount];

        /// <summary>Initializes the original zero-age history and unused mower-wave markers.</summary>
        internal AdventureRowPicker()
        {
            ResetMowedWaves();
        }

        /// <summary>Resets the mower-wave markers at the original seed-bank reset boundary without resetting pick history.</summary>
        internal void ResetMowedWaves()
        {
            for (var row = 0; row < RowCount; row++)
            {
                LastMowedWave[row] = -100;
            }
        }

        /// <summary>Executes the original six-entry smooth selector at PC 0x5269A0.</summary>
        /// <param name="random">The board's original random stream, consumed once as a scaled float.</param>
        /// <returns>The selected zero-based row.</returns>
        /// <exception cref="InvalidOperationException">No legal lane has positive weight.</exception>
        internal int Pick(Random random)
        {
            var total = 0f;
            for (var row = 0; row < RowCount; row++)
            {
                total += Weights[row];
            }

            if (total <= 0f)
            {
                throw new InvalidOperationException("No legal zombie spawn lane has positive weight.");
            }

            var inverseTotal = 1f / total;
            var smoothTotal = 0f;
            for (var row = 0; row < RowCount; row++)
            {
                smoothTotal += CalculateSmoothWeight(Weights[row] * inverseTotal, LastPicked[row], SecondLastPicked[row]);
            }

            var ticket = AdventureSpawnRandom.NextFloat(random, smoothTotal);
            var cumulative = 0f;
            var selected = RowCount - 1;
            for (var row = 0; row < RowCount - 1; row++)
            {
                cumulative += CalculateSmoothWeight(Weights[row] * inverseTotal, LastPicked[row], SecondLastPicked[row]);
                if (ticket <= cumulative)
                {
                    selected = row;
                    break;
                }
            }

            RecordPick(selected);
            return selected;
        }

        /// <summary>Executes PC 0x526AD0, including a rake-forced selection that consumes no random draw.</summary>
        /// <param name="selected">The selected zero-based row.</param>
        internal void RecordPick(int selected)
        {
            for (var row = 0; row < RowCount; row++)
            {
                if (Weights[row] > 0f)
                {
                    LastPicked[row] += 1f;
                    SecondLastPicked[row] += 1f;
                }
            }

            SecondLastPicked[selected] = LastPicked[selected];
            LastPicked[selected] = 0f;
        }

        /// <summary>Preserves the explicit float stores in PC 0x5268B0's two-history weighting curve.</summary>
        /// <param name="weight">The lane weight normalized by the total legal weight.</param>
        /// <param name="lastPicked">The eligible-pick age of the most recent selection.</param>
        /// <param name="secondLastPicked">The eligible-pick age of the preceding selection.</param>
        /// <returns>The original clamped smooth weight, or zero for a negligible normalized weight.</returns>
        internal static float CalculateSmoothWeight(float weight, float lastPicked, float secondLastPicked)
        {
            if (weight < 0.000001f)
            {
                return 0f;
            }

            var expectedInterval = 1f / weight;
            var secondInterval = (float)(expectedInterval * 2.0);
            var secondOffset = (float)(secondLastPicked + 1.0 - secondInterval);
            var secondFactor = (float)(1.0 + secondOffset / (double)secondInterval * 2.0);
            var lastOffset = (float)(lastPicked + 1.0 - expectedInterval);
            var lastFactor = (float)(1.0 + lastOffset / (double)expectedInterval * 2.0);
            var factor = (float)(lastFactor * 0.75 + secondFactor * 0.25);
            return weight * Math.Max(0.01f, Math.Min(100f, factor));
        }
    }
}
