#nullable enable

namespace PlantsVsZombies.Modes
{
    /// <summary>Captures the original PC profile and board inputs used to initialize independent-mode waves.</summary>
    public sealed class ModeWaveContext
    {
        /// <summary>Gets the raw profile +0x20 value used by PC Board::GetLevelRandSeed.</summary>
        public uint ProfileSeed { get; }

        /// <summary>Gets the raw board +0x5634 seed offset.</summary>
        public uint BoardSeedOffset { get; }

        /// <summary>Gets the raw profile +0x54 progress value used by the Yeti gate.</summary>
        public int ProfileProgress { get; }

        /// <summary>Gets the raw profile +0x4C adventure-level value used by the Yeti gate.</summary>
        public int ProfileLevel { get; }

        /// <summary>Gets whether the native profile pointer is present.</summary>
        public bool HasPlayer { get; }

        /// <summary>Gets whether the original application +0x868 special-rule branches are enabled.</summary>
        public bool SpecialRulesEnabled { get; }

        /// <summary>Captures explicit native inputs without deriving an original profile seed from an account identifier.</summary>
        /// <param name="profileSeed">The raw profile +0x20 seed contribution.</param>
        /// <param name="boardSeedOffset">The raw board +0x5634 seed contribution.</param>
        /// <param name="profileProgress">The raw profile +0x54 progress.</param>
        /// <param name="profileLevel">The raw profile +0x4C level.</param>
        /// <param name="hasPlayer">Whether the profile pointer is present.</param>
        /// <param name="specialRulesEnabled">Whether the application's special-rule branches are enabled.</param>
        public ModeWaveContext(uint profileSeed, uint boardSeedOffset = 0, int profileProgress = 0,
            int profileLevel = 1, bool hasPlayer = true, bool specialRulesEnabled = true)
        {
            ProfileSeed = profileSeed;
            BoardSeedOffset = boardSeedOffset;
            ProfileProgress = profileProgress;
            ProfileLevel = profileLevel;
            HasPlayer = hasPlayer;
            SpecialRulesEnabled = specialRulesEnabled;
        }

        /// <summary>Computes PC 0x415E40's independent-mode seed with native unsigned wraparound.</summary>
        /// <param name="modeId">The original independent-mode identifier.</param>
        /// <param name="stageIndex">The zero-based survival stage.</param>
        /// <returns>The seed for the local eligibility generator.</returns>
        public uint DeriveSeed(int modeId, int stageIndex)
        {
            return unchecked(ProfileSeed + BoardSeedOffset + ((uint)stageIndex * 101u) + (uint)modeId);
        }
    }
}
