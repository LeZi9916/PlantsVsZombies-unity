using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PvZReplica.VerifiedData
{
    /// <summary>
    /// A static plant row. Costs and recharge thresholds are base values that runtime
    /// rules may override. RechargeThresholdRaw is not a verified duration in seconds.
    /// </summary>
    public sealed class PlantDefinition
    {
        internal PlantDefinition(int id, string internalName, int sunCost, int rechargeThresholdRaw,
            int reanimationId, int actionDispatchCodeRaw, int actionPeriodRaw,
            int unidentified0x04, int unidentified0x0c)
        {
            Id = id;
            InternalName = internalName;
            SunCost = sunCost;
            RechargeThresholdRaw = rechargeThresholdRaw;
            ReanimationId = reanimationId;
            ActionDispatchCodeRaw = actionDispatchCodeRaw;
            ActionPeriodRaw = actionPeriodRaw;
            Unidentified0x04 = unidentified0x04;
            Unidentified0x0C = unidentified0x0c;
        }

        public int Id { get; private set; }
        public string InternalName { get; private set; }
        public int SunCost { get; private set; }
        public int RechargeThresholdRaw { get; private set; }
        public int ReanimationId { get; private set; }
        public int ActionDispatchCodeRaw { get; private set; }
        public int ActionPeriodRaw { get; private set; }
        public int Unidentified0x04 { get; private set; }
        public int Unidentified0x0C { get; private set; }
    }

    /// <summary>
    /// A static zombie selection row. Costs, gates and weights are raw selection inputs,
    /// not health values, spawn guarantees, or a replacement for runtime exceptions.
    /// </summary>
    public sealed class ZombieDefinition
    {
        internal ZombieDefinition(int id, string internalName, int reanimationId,
            int waveBudgetCostRaw, int baseLevelGateRaw, int baseWaveGateRaw,
            int baseSelectionWeightRaw)
        {
            Id = id;
            InternalName = internalName;
            ReanimationId = reanimationId;
            WaveBudgetCostRaw = waveBudgetCostRaw;
            BaseLevelGateRaw = baseLevelGateRaw;
            BaseWaveGateRaw = baseWaveGateRaw;
            BaseSelectionWeightRaw = baseSelectionWeightRaw;
        }

        public int Id { get; private set; }
        public string InternalName { get; private set; }
        public int ReanimationId { get; private set; }
        public int WaveBudgetCostRaw { get; private set; }
        public int BaseLevelGateRaw { get; private set; }
        public int BaseWaveGateRaw { get; private set; }
        public int BaseSelectionWeightRaw { get; private set; }
    }
    /// <summary>
    /// One 1-based adventure table row. Base waves and raw zombie flags are static
    /// inputs, not a complete level script or a list of guaranteed spawns. Runtime
    /// rules can override wave counts and bypass or modify selection.
    /// </summary>
    public sealed class AdventureLevelDefinition
    {
        private readonly ReadOnlyCollection<int> zombieFlagsRaw;

        internal AdventureLevelDefinition(int levelIndex, int baseWaveCount, int[] zombieFlagsRaw)
        {
            LevelIndex = levelIndex;
            BaseWaveCount = baseWaveCount;
            this.zombieFlagsRaw = Array.AsReadOnly(zombieFlagsRaw);
        }

        public int LevelIndex { get; private set; }
        public int BaseWaveCount { get; private set; }

        /// <summary>Raw flag values indexed by zombie ID, not final spawn decisions.</summary>
        public IReadOnlyList<int> ZombieFlagsRaw { get { return zombieFlagsRaw; } }
    }

    /// <summary>A stored zombie-matrix row header, identified by its lookup index.</summary>
    public sealed class AdventureZombieRowHeader
    {
        internal AdventureZombieRowHeader(int zombieIdIndex, int storedHeaderRaw)
        {
            ZombieIdIndex = zombieIdIndex;
            StoredHeaderRaw = storedHeaderRaw;
        }

        public int ZombieIdIndex { get; private set; }
        public int StoredHeaderRaw { get; private set; }
    }

    /// <summary>
    /// Static mode UI metadata. A row does not establish that a mode is accessible
    /// or provide a playable level script. Adventure mode 0 is not in this table.
    /// </summary>
    public sealed class ChallengeModeDefinition
    {
        internal ChallengeModeDefinition(int id, string localizationKey, int uiCategoryIndex,
            int uiGridRow, int uiGridColumn, int unidentified0x04)
        {
            Id = id;
            LocalizationKey = localizationKey;
            UiCategoryIndex = uiCategoryIndex;
            UiGridRow = uiGridRow;
            UiGridColumn = uiGridColumn;
            Unidentified0x04 = unidentified0x04;
        }

        public int Id { get; private set; }
        public string LocalizationKey { get; private set; }
        public int UiCategoryIndex { get; private set; }
        public int UiGridRow { get; private set; }
        public int UiGridColumn { get; private set; }
        public int Unidentified0x04 { get; private set; }
    }

    /// <summary>A static reanimation ID-to-path mapping and its unidentified raw field.</summary>
    public sealed class ReanimationDefinition
    {
        internal ReanimationDefinition(int id, string path, int unknown0x08)
        {
            Id = id;
            Path = path;
            Unknown0x08 = unknown0x08;
        }

        public int Id { get; private set; }
        public string Path { get; private set; }
        public int Unknown0x08 { get; private set; }
    }
    /// <summary>
    /// Immutable, ID-indexed data generated from the verified IDA exports. This is
    /// a static catalog, not gameplay or complete level scripts. Unknown IDs return
    /// false; they are never clamped or matched by name.
    /// </summary>
    public static partial class VerifiedDataCatalog
    {
        public static IReadOnlyList<PlantDefinition> Plants { get { return plants; } }
        public static IReadOnlyList<ZombieDefinition> Zombies { get { return zombies; } }
        public static IReadOnlyList<AdventureLevelDefinition> AdventureLevels { get { return adventureLevels; } }
        public static IReadOnlyList<AdventureZombieRowHeader> AdventureZombieRowHeaders { get { return adventureZombieRowHeaders; } }
        public static IReadOnlyList<ChallengeModeDefinition> ChallengeModes { get { return challengeModes; } }
        public static IReadOnlyList<ReanimationDefinition> Reanimations { get { return reanimations; } }

        public static bool TryGetPlant(int id, out PlantDefinition definition)
        {
            definition = id >= 0 && id < plants.Count ? plants[id] : null;
            return definition != null;
        }

        public static bool TryGetZombie(int id, out ZombieDefinition definition)
        {
            definition = id >= 0 && id < zombies.Count ? zombies[id] : null;
            return definition != null;
        }

        public static bool TryGetAdventureLevel(int levelIndex, out AdventureLevelDefinition definition)
        {
            definition = levelIndex >= 1 && levelIndex <= adventureLevels.Count
                ? adventureLevels[levelIndex - 1] : null;
            return definition != null;
        }

        public static bool TryGetAdventureZombieRowHeader(int zombieId, out AdventureZombieRowHeader header)
        {
            header = zombieId >= 0 && zombieId < adventureZombieRowHeaders.Count
                ? adventureZombieRowHeaders[zombieId] : null;
            return header != null;
        }

        /// <summary>Returns a raw table value, or false for an unknown level or zombie ID.</summary>
        public static bool TryGetAdventureZombieFlagRaw(int levelIndex, int zombieId, out int flag)
        {
            AdventureLevelDefinition level;
            if (!TryGetAdventureLevel(levelIndex, out level) || zombieId < 0 || zombieId >= level.ZombieFlagsRaw.Count)
            {
                flag = default(int);
                return false;
            }

            flag = level.ZombieFlagsRaw[zombieId];
            return true;
        }

        public static bool TryGetChallengeMode(int id, out ChallengeModeDefinition definition)
        {
            definition = id >= 1 && id <= challengeModes.Count ? challengeModes[id - 1] : null;
            return definition != null;
        }

        public static bool TryGetReanimation(int id, out ReanimationDefinition definition)
        {
            definition = id >= 0 && id < reanimations.Count ? reanimations[id] : null;
            return definition != null;
        }
    }
}
