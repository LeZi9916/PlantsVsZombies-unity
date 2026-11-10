#nullable enable

namespace PlantsVsZombies.Waves
{
    /// <summary>Defines adventure wave schedule status.</summary>
    public sealed class AdventureWaveScheduleStatus
    {
        /// <summary>Gets or sets countdown ticks.</summary>
        public int CountdownTicks { get; internal set; }
        /// <summary>Gets or sets initial countdown ticks.</summary>
        public int InitialCountdownTicks { get; internal set; }
        /// <summary>Gets or sets warning ticks.</summary>
        public int WarningTicks { get; internal set; }
        /// <summary>Gets or sets ambush ticks.</summary>
        public int AmbushTicks { get; internal set; }
        /// <summary>Gets or sets final wave sound ticks.</summary>
        public int FinalWaveSoundTicks { get; internal set; }
        /// <summary>Gets or sets tutorial released.</summary>
        public bool TutorialReleased { get; internal set; }
        /// <summary>Gets or sets health at wave start.</summary>
        public int HealthAtWaveStart { get; internal set; }
        /// <summary>Gets or sets health to next wave.</summary>
        public int HealthToNextWave { get; internal set; }
        /// <summary>Gets or sets is final wave.</summary>
        public bool IsFinalWave { get; internal set; }
        /// <summary>Gets or sets ambush complete.</summary>
        public bool AmbushComplete { get; internal set; }
        /// <summary>Gets or sets elapsed ticks.</summary>
        public long ElapsedTicks { get; internal set; }

        /// <summary>Gets or sets remaining seconds.</summary>
        public float RemainingSeconds
        {
            get
            {
                return (WarningTicks > 0 ? WarningTicks : CountdownTicks) * 0.01f;
            }
        }
    }
}
