#nullable enable

using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure progress.</summary>
    public sealed class AdventureProgress
    {
        /// <summary>Gets or sets profile id.</summary>
        public Guid ProfileId { get; internal set; }
        /// <summary>Gets or sets next level.</summary>
        public int NextLevel { get; internal set; } = 1;
        /// <summary>Gets or sets cycle.</summary>
        public int Cycle { get; internal set; } = 1;

        /// <summary>Gets or sets completed runs.</summary>
        public int CompletedRuns
        {
            get
            {
                return Cycle - 1 + (Completed ? 1 : 0);
            }
        }

        /// <summary>Gets or sets packet slots.</summary>
        public int PacketSlots { get; internal set; } = 6;
        /// <summary>Gets or sets imitater target.</summary>
        public PlantKind? ImitaterTarget { get; internal set; }
        /// <summary>Gets or sets collected rewards.</summary>
        public ulong CollectedRewards { get; internal set; }

        /// <summary>Gets or sets completed.</summary>
        public bool Completed
        {
            get
            {
                return CollectedRewards == ((1UL << 50) - 1);
            }
        }

        /// <summary>Gets or sets attempt seed.</summary>
        public int AttemptSeed { get; internal set; } = 12345;
        /// <summary>Gets or sets selected seeds.</summary>
        public IReadOnlyList<PlantKind> SelectedSeeds { get; internal set; } = Array.AsReadOnly(new PlantKind[0]);

        /// <summary>Performs copy.</summary>
        /// <returns>The resulting copy.</returns>
        internal AdventureProgress Copy()
        {
            return new AdventureProgress
            {
                ProfileId = ProfileId,
                NextLevel = NextLevel,
                Cycle = Cycle,
                PacketSlots = PacketSlots,
                ImitaterTarget = ImitaterTarget,
                CollectedRewards = CollectedRewards,
                AttemptSeed = AttemptSeed,
                SelectedSeeds = Array.AsReadOnly(ToArray(SelectedSeeds))
            };
        }

        /// <summary>Performs to array.</summary>
        /// <param name = "values">The values.</param>
        /// <returns>The resulting to array.</returns>
        private static PlantKind[] ToArray(IReadOnlyList<PlantKind> values)
        {
            var result = new PlantKind[values.Count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = values[i];
            }

            return result;
        }
    }
}
