#nullable enable
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Levels
{
    /// <summary>Defines level one victory evidence.</summary>
    public static class LevelOneVictoryEvidence
    {
        /// <summary>The exact predicate inside the native zombie iterator.</summary>
        public static bool IsNativeVictoryBlocker(NativeVictoryZombieFields zombie)
        {
            return zombie.ByteBA && !zombie.ByteEC && zombie.Phase28 != 1 && zombie.Phase28 != 2 && zombie.Phase28 != 3 && !zombie.ByteB8;
        }

        /// <summary>
        /// Two necessary conditions for the normal reward-drop path, not a
        /// complete win predicate. Zombies must be the host's equivalent of the
        /// native board iterator. Other native board/candidate guards still apply.
        /// </summary>
        public static bool MeetsKnownRewardPrerequisites(int wavesDispatched, IEnumerable<NativeVictoryZombieFields> zombies)
        {
            if (wavesDispatched < 0)
            {
                throw new ArgumentOutOfRangeException("wavesDispatched");
            }

            if (zombies == null)
            {
                throw new ArgumentNullException("zombies");
            }

            if (wavesDispatched < LevelOneData.WaveCount)
            {
                return false;
            }

            foreach (NativeVictoryZombieFields zombie in zombies)
            {
                if (IsNativeVictoryBlocker(zombie))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
