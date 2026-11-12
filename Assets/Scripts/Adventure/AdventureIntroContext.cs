#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure intro context.</summary>
    public sealed class AdventureIntroContext
    {
        /// <summary>Stores level, native mode, native background.</summary>
        public int Level = 1, NativeMode, NativeBackground;
        /// <summary>Stores first adventure, has board, survival repick, has grave stones.</summary>
        public bool FirstAdventure = true, HasBoard = true, SurvivalRepick, HasGraveStones;
        /// <summary>Stores is stormy night, is final boss, is little trouble, can show almanac or store.</summary>
        public bool IsStormyNight, IsFinalBoss, IsLittleTrouble, CanShowAlmanacOrStore;
        /// <summary>Stores is conveyor, easy planting cheat, has crazy dave dialog.</summary>
        public bool IsConveyor, EasyPlantingCheat, HasCrazyDaveDialog;
        /// <summary>Stores crazy dave time override.</summary>
        public int CrazyDaveTimeOverride = -1;
        /// <summary>Stores choose seeds override, non scrolling override.</summary>
        public bool? ChooseSeedsOverride, NonScrollingOverride;
        /// <summary>Stores logical width, seed bank image height, seed bank start x.</summary>
        public int LogicalWidth = 800, SeedBankImageHeight = 87, SeedBankStartX;
        // Native predicates outside the presentation helper remain owned by the host.
        /// <summary>Gets or sets is shovel.</summary>
        public bool IsShovel
        {
            get
            {
                return HasBoard && (NativeMode == 17 || NativeMode == 33 || NativeMode == 0 && Level == 5);
            }
        }

        /// <summary>Gets or sets is whack.</summary>
        public bool IsWhack
        {
            get
            {
                return HasBoard && (NativeMode == 30 || NativeMode == 0 && Level == 15);
            }
        }

        /// <summary>Gets or sets is bowling.</summary>
        public bool IsBowling
        {
            get
            {
                return HasBoard && NativeMode == 49;
            }
        }

        /// <summary>Gets or sets is izombie.</summary>
        public bool IsIZombie
        {
            get
            {
                return HasBoard && NativeMode >= 61 && NativeMode <= 70;
            }
        }

        /// <summary>Gets or sets is vase.</summary>
        public bool IsVase
        {
            get
            {
                return NativeMode >= 51 && NativeMode <= 60 || NativeMode == 0 && Level == 35;
            }
        }

        /// <summary>Gets or sets has pool.</summary>
        public bool HasPool
        {
            get
            {
                return NativeBackground == 2 || NativeBackground == 3;
            }
        }

        /// <summary>Gets or sets has roof.</summary>
        public bool HasRoof
        {
            get
            {
                return NativeBackground == 4 || NativeBackground == 5;
            }
        }
    }
}
