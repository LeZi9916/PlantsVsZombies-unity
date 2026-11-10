#nullable enable

using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Explicit host-supplied access, not a shop purchase or persistent entitlement.</summary>
    public sealed class AdventureSeedAccess
    {
        /// <summary>Gets or sets additional seeds.</summary>
        public IReadOnlyList<PlantKind> AdditionalSeeds { get; private set; }
        /// <summary>Gets or sets imitater target.</summary>
        public PlantKind? ImitaterTarget { get; private set; }

        /// <summary>Initializes adventure seed access.</summary>
        /// <param name = "additionalSeeds">The additional seeds.</param>
        /// <param name = "imitaterTarget">The imitater target.</param>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        public AdventureSeedAccess(IEnumerable<PlantKind> additionalSeeds, PlantKind? imitaterTarget = null)
        {
            if (additionalSeeds == null)
            {
                throw new ArgumentNullException("additionalSeeds");
            }

            var values = new List<PlantKind>();
            foreach (var kind in additionalSeeds)
            {
                if ((int)kind < 40 || (int)kind > 48 || values.Contains(kind))
                {
                    throw new ArgumentException("Explicit access accepts unique IDs 40..48 only.");
                }

                values.Add(kind);
            }

            if (imitaterTarget.HasValue && ((int)imitaterTarget.Value < 0 || (int)imitaterTarget.Value > 39 || !values.Contains(PlantKind.Imitater)))
            {
                throw new ArgumentException("Imitater requires access and a basic plant target ID 0..39.");
            }

            AdditionalSeeds = Array.AsReadOnly(values.ToArray());
            ImitaterTarget = imitaterTarget;
        }

        /// <summary>Performs development all.</summary>
        /// <param name = "imitaterTarget">The imitater target.</param>
        /// <returns>The resulting development all.</returns>
        public static AdventureSeedAccess DevelopmentAll(PlantKind? imitaterTarget = null)
        {
            var values = new List<PlantKind>();
            for (var id = 40; id <= 48; id++)
            {
                values.Add((PlantKind)id);
            }

            return new AdventureSeedAccess(values, imitaterTarget);
        }
    }
}
