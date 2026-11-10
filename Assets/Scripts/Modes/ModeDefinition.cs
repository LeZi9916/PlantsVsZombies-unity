#nullable enable

namespace PlantsVsZombies.Modes
{
    /// <summary>Defines mode definition.</summary>
    public sealed class ModeDefinition
    {
        /// <summary>Gets or sets id.</summary>
        public int Id { get; internal set; }
        /// <summary>Gets or sets name.</summary>
        public string Name { get; internal set; }

        /// <summary>Gets or sets record index.</summary>
        public int RecordIndex
        {
            get
            {
                return Id - 1;
            }
        }

        /// <summary>Gets or sets mechanic.</summary>
        public ModeMechanic Mechanic { get; internal set; }
        /// <summary>Gets or sets board level.</summary>
        public int BoardLevel { get; internal set; }
        /// <summary>Gets or sets wave count.</summary>
        public int WaveCount { get; internal set; }

        /// <summary>Gets or sets endless.</summary>
        public bool Endless
        {
            get
            {
                return Id >= 11 && Id <= 15 || Id == 60 || Id == 70;
            }
        }

        /// <summary>Gets or sets survival.</summary>
        public bool Survival
        {
            get
            {
                return Mechanic == ModeMechanic.Survival;
            }
        }

        /// <summary>Gets or sets hard.</summary>
        public bool Hard
        {
            get
            {
                return Id >= 6 && Id <= 15;
            }
        }

        /// <summary>Gets or sets puzzle.</summary>
        public bool Puzzle
        {
            get
            {
                return Id >= 51 && Id <= 70;
            }
        }

        /// <summary>Gets or sets flags per stage.</summary>
        public int FlagsPerStage
        {
            get
            {
                return Survival && Hard ? 2 : 1;
            }
        }

        /// <summary>Gets or sets stages.</summary>
        public int Stages
        {
            get
            {
                return Survival ? 5 : Mechanic == ModeMechanic.LastStand ? 5 : 1;
            }
        }

        /// <summary>Gets or sets uses packets.</summary>
        public bool UsesPackets
        {
            get
            {
                return Mechanic == ModeMechanic.Bowling || Mechanic == ModeMechanic.Slots || Mechanic == ModeMechanic.RainingSeeds || Mechanic == ModeMechanic.Invisible || Mechanic == ModeMechanic.LittleZombies || Mechanic == ModeMechanic.Columns || Mechanic == ModeMechanic.Portals || Mechanic == ModeMechanic.Boss || Mechanic == ModeMechanic.Vases;
            }
        }

        /// <summary>Gets or sets supports rake.</summary>
        public bool SupportsRake
        {
            get
            {
                return Mechanic != ModeMechanic.IZombie && Mechanic != ModeMechanic.Aquarium && Mechanic != ModeMechanic.Boss && Mechanic != ModeMechanic.Whack && Mechanic != ModeMechanic.Beghouled && Mechanic != ModeMechanic.BeghouledTwist && Mechanic != ModeMechanic.Bobsled;
            }
        }

        /// <summary>Gets or sets chooses seeds.</summary>
        public bool ChoosesSeeds
        {
            get
            {
                return !UsesPackets && Mechanic != ModeMechanic.Beghouled && Mechanic != ModeMechanic.BeghouledTwist && Mechanic != ModeMechanic.Aquarium && Mechanic != ModeMechanic.Whack && Mechanic != ModeMechanic.IZombie;
            }
        }
    }
}
