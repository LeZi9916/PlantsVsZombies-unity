#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure intro plan.</summary>
    public sealed class AdventureIntroPlan
    {
        /// <summary>Stores context.</summary>
        public AdventureIntroContext? Context;

        /// <summary>Gets the context, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        public AdventureIntroContext RequiredContext => Context ?? throw new System.InvalidOperationException("The member 'Context' is not available.");
        /// <summary>Stores choose seeds, non scrolling, scrolled left at start, draw seed bank.</summary>
        public bool ChooseSeeds, NonScrolling, ScrolledLeftAtStart, DrawSeedBank;
        /// <summary>Stores sod time, grave stone time, ready time, lawn mower time, dave time, fog time, boss time.</summary>
        public int SodTime, GraveStoneTime, ReadyTime, LawnMowerTime, DaveTime, FogTime, BossTime;
        /// <summary>Gets or sets pan right start.</summary>
        public int PanRightStart
        {
            get
            {
                return 1500 + DaveTime;
            }
        }

        /// <summary>Gets or sets pan right end.</summary>
        public int PanRightEnd
        {
            get
            {
                return 3500 + DaveTime;
            }
        }

        /// <summary>Gets or sets chooser on start.</summary>
        public int ChooserOnStart
        {
            get
            {
                return 4000 + DaveTime;
            }
        }

        /// <summary>Gets or sets chooser on end.</summary>
        public int ChooserOnEnd
        {
            get
            {
                return 4250 + DaveTime;
            }
        }

        /// <summary>Gets or sets chooser off start.</summary>
        public int ChooserOffStart
        {
            get
            {
                return 4500 + DaveTime;
            }
        }

        /// <summary>Gets or sets chooser off end.</summary>
        public int ChooserOffEnd
        {
            get
            {
                return 4750 + DaveTime;
            }
        }

        /// <summary>Gets or sets pan left end.</summary>
        public int PanLeftEnd
        {
            get
            {
                return 6000 + DaveTime;
            }
        }

        /// <summary>Gets or sets sod start.</summary>
        public int SodStart
        {
            get
            {
                return 6000 + DaveTime;
            }
        }

        /// <summary>Gets or sets sod end.</summary>
        public int SodEnd
        {
            get
            {
                return SodStart + SodTime;
            }
        }

        /// <summary>Gets or sets grave stone start.</summary>
        public int GraveStoneStart
        {
            get
            {
                return 6000 + DaveTime + SodTime;
            }
        }

        /// <summary>Gets or sets bank on start.</summary>
        public int BankOnStart
        {
            get
            {
                return 4000 + DaveTime + (ChooseSeeds ? 0 : 2000 + SodTime + GraveStoneTime + FogTime + BossTime);
            }
        }

        /// <summary>Gets or sets bank on end.</summary>
        public int BankOnEnd
        {
            get
            {
                return BankOnStart + 250;
            }
        }

        /// <summary>Gets or sets ready start.</summary>
        public int ReadyStart
        {
            get
            {
                return 6000 + DaveTime + LawnMowerTime + SodTime + GraveStoneTime + FogTime + BossTime;
            }
        }

        /// <summary>Gets or sets end.</summary>
        public int End
        {
            get
            {
                return ReadyStart + ReadyTime;
            }
        }
    }
}
