using System;
using System.Collections.Generic;

namespace PvZReplica.Levels
{
    /// <summary>
    /// The statically verified first-play, adventure-mode level 1 facts for the
    /// executable identified in analysis/ida/level_1_1/REPORT.md.
    /// This is not a complete gameplay simulation.
    /// </summary>
    public static class LevelOneData
    {
        public const int Columns = 9;
        public const int NativeRowSlots = 6;
        public const int GrassRow = 2;
        public const int StartingSun = 150;
        public const int SeedPacketCount = 1;
        public const int PeashooterSeedId = 0;
        public const int PeashooterCost = 100;
        public const int PeashooterRechargeNativeTicks = 750;
        public const int NormalZombieId = 0;
        public const int ZombieSpawnRow = GrassRow;
        public const int WaveCount = 4;
        public const int NativeRewardItemType = 7;

        /// <summary>Rows and columns are zero-based native grid indices.</summary>
        public static bool IsGrassCell(int column, int row)
        {
            if (column < 0 || column >= Columns) throw new ArgumentOutOfRangeException("column");
            if (row < 0 || row >= NativeRowSlots) throw new ArgumentOutOfRangeException("row");
            return row == GrassRow;
        }

        /// <summary>A zero-based entry in the precomputed native wave list.</summary>
        public static int GetZombieCount(int waveIndex)
        {
            if (waveIndex < 0 || waveIndex >= WaveCount) throw new ArgumentOutOfRangeException("waveIndex");
            return waveIndex == 3 ? 2 : 1;
        }

        public static int GetZombieId(int waveIndex, int indexInWave)
        {
            int count = GetZombieCount(waveIndex);
            if (indexInWave < 0 || indexInWave >= count) throw new ArgumentOutOfRangeException("indexInWave");
            return NormalZombieId;
        }

        public static bool IsFlagWave(int waveIndex)
        {
            GetZombieCount(waveIndex);
            return false;
        }
    }

    // These numeric values are the native Board field, not a complete enum.
    public enum LevelOneTutorialStage
    {
        NotStarted = 0,
        AwaitingSeedSelection = 1,
        AwaitingPlantPlacement = 2,
        AwaitingUsableSeed = 3,
        ZombieOnslaughtPrompt = 4
    }

    /// <summary>
    /// A transition and any native counters explicitly reset by that transition.
    /// Null means no reset, not that the counter is zero or absent.
    /// </summary>
    public struct LevelOneTutorialEffect
    {
        public readonly bool Changed;
        public readonly LevelOneTutorialStage Stage;
        public readonly int? SkySunCountdownNativeTicks;
        public readonly int? WaveCountdownAndInitialNativeTicks;

        internal LevelOneTutorialEffect(bool changed, LevelOneTutorialStage stage, int? skySun, int? wave)
        {
            Changed = changed;
            Stage = stage;
            SkySunCountdownNativeTicks = skySun;
            WaveCountdownAndInitialNativeTicks = wave;
        }
    }

    /// <summary>
    /// Only the proven first-play teaching transitions 0 through 4. Methods
    /// consume events already accepted by the host; they do not validate input,
    /// simulate sun or recharge, or claim to cover later native tutorial states.
    /// </summary>
    public sealed class LevelOneTutorial
    {
        public LevelOneTutorialStage Stage { get; private set; }

        public bool TutorialBlocksWaveCountdown
        {
            get
            {
                return Stage == LevelOneTutorialStage.AwaitingSeedSelection ||
                    Stage == LevelOneTutorialStage.AwaitingPlantPlacement ||
                    Stage == LevelOneTutorialStage.AwaitingUsableSeed;
            }
        }

        public LevelOneTutorialEffect OnFirstActiveGameplayUpdate()
        {
            if (Stage != LevelOneTutorialStage.NotStarted) return Unchanged();
            return Change(LevelOneTutorialStage.AwaitingSeedSelection, null, null);
        }

        public LevelOneTutorialEffect OnSeedPickedUp()
        {
            if (Stage != LevelOneTutorialStage.AwaitingSeedSelection) return Unchanged();
            return Change(LevelOneTutorialStage.AwaitingPlantPlacement, null, null);
        }

        public LevelOneTutorialEffect OnPlantingCursorCancelled()
        {
            if (Stage != LevelOneTutorialStage.AwaitingPlantPlacement) return Unchanged();
            return Change(LevelOneTutorialStage.AwaitingSeedSelection, null, null);
        }

        /// <param name="nativePlantArraySizeAfterPlacement">
        /// Board+0xD4 after the accepted placement: the native plant data-array
        /// allocation count, not a lifetime count or necessarily a visible/live
        /// count when an object is awaiting removal. It must be positive.
        /// </param>
        public LevelOneTutorialEffect OnPlantPlaced(int nativePlantArraySizeAfterPlacement)
        {
            if (nativePlantArraySizeAfterPlacement < 1) throw new ArgumentOutOfRangeException("nativePlantArraySizeAfterPlacement");
            if (Stage != LevelOneTutorialStage.AwaitingPlantPlacement) return Unchanged();
            if (nativePlantArraySizeAfterPlacement < 2)
                return Change(LevelOneTutorialStage.AwaitingUsableSeed, 200, null);
            return Change(LevelOneTutorialStage.ZombieOnslaughtPrompt, null, 100);
        }

        /// <summary>
        /// Call only when the native-equivalent seed usability test has passed,
        /// including both recharge and affordability under ordinary play.
        /// </summary>
        public LevelOneTutorialEffect OnSeedBecameUsable()
        {
            if (Stage != LevelOneTutorialStage.AwaitingUsableSeed) return Unchanged();
            return Change(LevelOneTutorialStage.AwaitingSeedSelection, null, null);
        }

        private LevelOneTutorialEffect Unchanged()
        {
            return new LevelOneTutorialEffect(false, Stage, null, null);
        }

        private LevelOneTutorialEffect Change(LevelOneTutorialStage next, int? skySun, int? wave)
        {
            Stage = next;
            return new LevelOneTutorialEffect(true, Stage, skySun, wave);
        }
    }

    /// <summary>
    /// Raw fields read by the native victory-blocker iterator. They deliberately
    /// retain offsets because not all field semantics have been established.
    /// </summary>
    public struct NativeVictoryZombieFields
    {
        public readonly bool ByteBA;
        public readonly bool ByteEC;
        public readonly int Phase28;
        public readonly bool ByteB8;

        public NativeVictoryZombieFields(bool byteBA, bool byteEC, int phase28, bool byteB8)
        {
            ByteBA = byteBA;
            ByteEC = byteEC;
            Phase28 = phase28;
            ByteB8 = byteB8;
        }
    }

    public static class LevelOneVictoryEvidence
    {
        /// <summary>The exact predicate inside the native zombie iterator.</summary>
        public static bool IsNativeVictoryBlocker(NativeVictoryZombieFields zombie)
        {
            return zombie.ByteBA && !zombie.ByteEC && zombie.Phase28 != 1 &&
                zombie.Phase28 != 2 && zombie.Phase28 != 3 && !zombie.ByteB8;
        }

        /// <summary>
        /// Two necessary conditions for the normal reward-drop path, not a
        /// complete win predicate. Zombies must be the host's equivalent of the
        /// native board iterator. Other native board/candidate guards still apply.
        /// </summary>
        public static bool MeetsKnownRewardPrerequisites(int wavesDispatched,
            IEnumerable<NativeVictoryZombieFields> zombies)
        {
            if (wavesDispatched < 0) throw new ArgumentOutOfRangeException("wavesDispatched");
            if (zombies == null) throw new ArgumentNullException("zombies");
            if (wavesDispatched < LevelOneData.WaveCount) return false;
            foreach (NativeVictoryZombieFields zombie in zombies)
                if (IsNativeVictoryBlocker(zombie)) return false;
            return true;
        }
    }
}
