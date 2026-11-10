#nullable enable
using static PlantsVsZombies.VerifiedData.VerifiedDataTables;

using System.Collections.Generic;

namespace PlantsVsZombies.VerifiedData
{
    /// <summary>
    /// Immutable, ID-indexed data generated from the verified IDA exports. This is
    /// a static catalog, not gameplay or complete level scripts. Unknown IDs return
    /// false; they are never clamped or matched by name.
    /// </summary>
    public static class VerifiedDataCatalog
    {
        /// <summary>Identifies the executable from which the catalog was verified.</summary>
        public const string SourceExeSha256 = VerifiedDataTables.SourceExeSha256;
        /// <summary>Gets the verified plants indexed by their original identifiers.</summary>
        public static IReadOnlyList<PlantDefinition> Plants
        {
            get
            {
                return s_plants;
            }
        }

        /// <summary>Gets the verified zombies indexed by their original identifiers.</summary>
        public static IReadOnlyList<ZombieDefinition> Zombies
        {
            get
            {
                return s_zombies;
            }
        }

        /// <summary>Gets adventure levels in original level order; list index zero represents level one.</summary>
        public static IReadOnlyList<AdventureLevelDefinition> AdventureLevels
        {
            get
            {
                return s_adventureLevels;
            }
        }

        /// <summary>Gets the verified adventure zombie row headers indexed by their original identifiers.</summary>
        public static IReadOnlyList<AdventureZombieRowHeader> AdventureZombieRowHeaders
        {
            get
            {
                return s_adventureZombieRowHeaders;
            }
        }

        /// <summary>Gets challenge modes in original identifier order; list index zero represents mode one.</summary>
        public static IReadOnlyList<ChallengeModeDefinition> ChallengeModes
        {
            get
            {
                return s_challengeModes;
            }
        }

        /// <summary>Gets the verified reanimations indexed by their original identifiers.</summary>
        public static IReadOnlyList<ReanimationDefinition> Reanimations
        {
            get
            {
                return s_reanimations;
            }
        }

        /// <summary>Looks up a verified plant row by its original identifier.</summary>
        /// <param name="id">The zero-based plant identifier.</param>
        /// <param name="definition">The matching row on success, or null when the identifier is unknown.</param>
        /// <returns>True when the identifier belongs to the verified plant table.</returns>
        public static bool TryGetPlant(int id, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out PlantDefinition? definition)
        {
            definition = id >= 0 && id < s_plants.Count ? s_plants[id] : null;
            return definition != null;
        }

        /// <summary>Looks up a verified zombie-selection row by its original identifier.</summary>
        /// <param name="id">The zero-based zombie identifier.</param>
        /// <param name="definition">The matching row on success, or null when the identifier is unknown.</param>
        /// <returns>True when the identifier belongs to the verified zombie table.</returns>
        public static bool TryGetZombie(int id, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ZombieDefinition? definition)
        {
            definition = id >= 0 && id < s_zombies.Count ? s_zombies[id] : null;
            return definition != null;
        }

        /// <summary>Looks up a verified adventure-level row using its original one-based level index.</summary>
        /// <param name="levelIndex">The original adventure level index, starting at one.</param>
        /// <param name="definition">The matching row on success, or null when the index is unknown.</param>
        /// <returns>True when the index belongs to the verified adventure-level table.</returns>
        public static bool TryGetAdventureLevel(int levelIndex, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AdventureLevelDefinition? definition)
        {
            definition = levelIndex >= 1 && levelIndex <= s_adventureLevels.Count ? s_adventureLevels[levelIndex - 1] : null;
            return definition != null;
        }

        /// <summary>Looks up the raw matrix-row header for an original zombie identifier.</summary>
        /// <param name="zombieId">The zero-based original zombie identifier.</param>
        /// <param name="header">The stored header on success, or null when no row exists.</param>
        /// <returns>True when the verified zombie matrix contains the requested row.</returns>
        public static bool TryGetAdventureZombieRowHeader(int zombieId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AdventureZombieRowHeader? header)
        {
            header = zombieId >= 0 && zombieId < s_adventureZombieRowHeaders.Count ? s_adventureZombieRowHeaders[zombieId] : null;
            return header != null;
        }

        /// <summary>Returns a raw table value, or false for an unknown level or zombie ID.</summary>
        /// <param name="levelIndex">The original adventure level index starting at one.</param>
        /// <param name="zombieId">The zero-based zombie identifier within the level's raw flag row.</param>
        /// <param name="flag">The stored flag on success, or zero when either identifier is unknown.</param>
        /// <returns>True when both identifiers address an entry in the verified matrix.</returns>
        public static bool TryGetAdventureZombieFlagRaw(int levelIndex, int zombieId, out int flag)
        {
            if (!TryGetAdventureLevel(levelIndex, out var level) || zombieId < 0 || zombieId >= level.ZombieFlagsRaw.Count)
            {
                flag = default(int);
                return false;
            }

            flag = level.ZombieFlagsRaw[zombieId];
            return true;
        }

        /// <summary>Looks up verified challenge-menu metadata by original challenge identifier.</summary>
        /// <param name="id">The original challenge identifier starting at one; adventure mode zero is excluded.</param>
        /// <param name="definition">The matching row on success, or null when the identifier is unknown.</param>
        /// <returns>True when the identifier belongs to the verified challenge table.</returns>
        public static bool TryGetChallengeMode(int id, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChallengeModeDefinition? definition)
        {
            definition = id >= 1 && id <= s_challengeModes.Count ? s_challengeModes[id - 1] : null;
            return definition != null;
        }

        /// <summary>Looks up the original resource path for a verified reanimation identifier.</summary>
        /// <param name="id">The zero-based original reanimation identifier.</param>
        /// <param name="definition">The path mapping on success, or null when the identifier is unknown.</param>
        /// <returns>True when the identifier belongs to the verified reanimation table.</returns>
        public static bool TryGetReanimation(int id, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ReanimationDefinition? definition)
        {
            definition = id >= 0 && id < s_reanimations.Count ? s_reanimations[id] : null;
            return definition != null;
        }
    }
}
