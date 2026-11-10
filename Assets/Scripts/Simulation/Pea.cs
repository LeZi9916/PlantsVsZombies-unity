#nullable enable

using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Defines pea.</summary>
    public sealed class Pea
    {
        /// <summary>Stores presentation snow trail.</summary>
        public global::PlantsVsZombies.Adventure.AdventureSnowTrail PresentationSnowTrail;
        /// <summary>Stores presentation plant id.</summary>
        public int PresentationPlantId;
        /// <summary>Stores presentation age.</summary>
        public float PresentationAge;
        /// <summary>Stores presentation launch row, presentation launch column.</summary>
        public int PresentationLaunchRow, PresentationLaunchColumn;
        /// <summary>Stores presentation lob velocity.</summary>
        public float PresentationLobVelocity = -7f;
        /// <summary>Stores presentation previous x, presentation previous row, presentation rotation, presentation spin.</summary>
        public float PresentationPreviousX, PresentationPreviousRow, PresentationRotation, PresentationSpin;
        /// <summary>Stores presentation native age, presentation lob height, presentation previous lob height, presentation lob initial velocity, presentation lob x, presentation launch y.</summary>
        public float PresentationNativeAge, PresentationLobHeight, PresentationPreviousLobHeight, PresentationLobInitialVelocity = -7f, PresentationLobX, PresentationLaunchY;
        /// <summary>Stores presentation motion initialized.</summary>
        public bool PresentationMotionInitialized;
        /// <summary>Gets or sets presentation can impact.</summary>
        public bool PresentationCanImpact
        {
            get
            {
                return !Lobbed || PresentationLobVelocity >= 0f && PresentationLobHeight >= (Butter ? -32f : Splash ? -35f : -30f);
            }
        }

        /// <summary>Stores presentation initialized.</summary>
        public bool PresentationInitialized;
        /// <summary>Stores damage.</summary>
        public float Damage = 20f;
        /// <summary>Stores velocity x.</summary>
        public float VelocityX = 4.5f;
        /// <summary>Stores velocity y.</summary>
        public float VelocityY;
        /// <summary>Stores flight row.</summary>
        public float FlightRow;
        /// <summary>Stores start x.</summary>
        public float StartX;
        /// <summary>Stores range.</summary>
        public float Range = 20f;
        /// <summary>Stores lobbed.</summary>
        public bool Lobbed;
        /// <summary>Stores pierces shield.</summary>
        public bool PiercesShield;
        /// <summary>Stores pierces zombies.</summary>
        public bool PiercesZombies;
        /// <summary>Stores splash.</summary>
        public bool Splash;
        /// <summary>Stores butter.</summary>
        public bool Butter;
        /// <summary>Stores fire.</summary>
        public bool Fire;
        /// <summary>Stores hits air.</summary>
        public bool HitsAir;
        /// <summary>Stores backward.</summary>
        public bool Backward;
        /// <summary>Stores last torch column.</summary>
        public int LastTorchColumn = -1;
        /// <summary>Stores hit ids.</summary>
        public readonly HashSet<int> HitIds = new HashSet<int>();
        /// <summary>Stores homing.</summary>
        public bool Homing;
        /// <summary>Stores target zombie id.</summary>
        public int TargetZombieId;
        /// <summary>Stores chill splash.</summary>
        public bool ChillSplash;
        /// <summary>Plant kind captured at creation; visual metadata, independent of current elemental flags.</summary>
        public PlantKind SourceKind { get; internal set; } = PlantKind.Peashooter;
        /// <summary>Gets or sets source is imitated.</summary>
        public bool SourceIsImitated { get; internal set; }

        /// <summary>Stores row.</summary>
        public int Row;
        /// <summary>Stores x.</summary>
        public float X;
        /// <summary>Stores is snow.</summary>
        public bool IsSnow;
    }
}
