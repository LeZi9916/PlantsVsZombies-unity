#nullable enable

using PlantsVsZombies.Progression;
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Profiles
{
    /// <summary>Contains validated user data and the original bytes needed for lossless export.</summary>
    public sealed class OriginalUserSave
    {
        /// <summary>Creates a parsed save with all reference data supplied by the original-format reader.</summary>
        /// <param name="name">The filename-derived name, later replaced by the users.dat display name when available.</param>
        /// <param name="originalBytes">An owned copy of the complete original user file.</param>
        /// <param name="purchases">The purchase counters indexed by their original identifiers.</param>
        /// <param name="challengeRecords">The challenge records indexed by original mode identifiers.</param>
        /// <param name="plants">The parsed garden plants excluding opaque wheelbarrow records.</param>
        /// <exception cref="ArgumentNullException">A required parsed value is null.</exception>
        internal OriginalUserSave(string name, byte[] originalBytes, int[] purchases, int[] challengeRecords, IReadOnlyList<GardenPlant> plants)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            OriginalBytes = originalBytes ?? throw new ArgumentNullException(nameof(originalBytes));
            Purchases = purchases ?? throw new ArgumentNullException(nameof(purchases));
            ChallengeRecords = challengeRecords ?? throw new ArgumentNullException(nameof(challengeRecords));
            Plants = plants ?? throw new ArgumentNullException(nameof(plants));
        }

        /// <summary>Gets the imported display name, or the filename stem when no directory entry was read.</summary>
        public string Name { get; internal set; }
        /// <summary>Gets the identifier from the original users.dat directory.</summary>
        public uint OriginalId { get; internal set; }
        /// <summary>Gets the original directory's last-use sequence number.</summary>
        public uint UseSequence { get; internal set; }
        /// <summary>Gets the stored one-based adventure level in the range 1 through 50.</summary>
        public int Level { get; internal set; }
        /// <summary>Gets the wallet balance in the original save's coin units.</summary>
        public int Coins { get; internal set; }
        /// <summary>Gets the number of completed adventure runs stored in the original file.</summary>
        public int FinishedAdventure { get; internal set; }
        /// <summary>Gets whether the original minigames menu has been unlocked.</summary>
        public bool MinigamesUnlocked { get; internal set; }
        /// <summary>Gets whether the original puzzle menu has been unlocked.</summary>
        public bool PuzzleUnlocked { get; internal set; }
        /// <summary>Gets whether the original survival menu has been unlocked.</summary>
        public bool SurvivalUnlocked { get; internal set; }
        /// <summary>Gets normalized purchase counters indexed by original shop-item identifiers.</summary>
        public int[] Purchases { get; }
        /// <summary>Gets stored challenge records indexed by original mode identifiers.</summary>
        public int[] ChallengeRecords { get; }
        /// <summary>Gets the parsed garden plants, excluding wheelbarrow records preserved in the source bytes.</summary>
        public IReadOnlyList<GardenPlant> Plants { get; }

        /// <summary>Gets the owned original file bytes used to preserve fields that are not decoded.</summary>
        internal byte[] OriginalBytes { get; }
    }
}
