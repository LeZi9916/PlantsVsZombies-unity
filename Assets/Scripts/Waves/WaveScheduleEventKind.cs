#nullable enable

namespace PlantsVsZombies.Waves
{
    /// <summary>Defines wave schedule event kind.</summary>
    public enum WaveScheduleEventKind
    {
        /// <summary>Identifies first wave approaching.</summary>
        FirstWaveApproaching,
        /// <summary>Identifies wave approaching.</summary>
        WaveApproaching,
        /// <summary>Identifies huge wave warning.</summary>
        HugeWaveWarning,
        /// <summary>Identifies huge wave sound.</summary>
        HugeWaveSound,
        /// <summary>Identifies music burst.</summary>
        MusicBurst,
        /// <summary>Identifies final wave approaching.</summary>
        FinalWaveApproaching,
        /// <summary>Identifies wave spawned.</summary>
        WaveSpawned,
        /// <summary>Identifies ambush.</summary>
        Ambush,
        /// <summary>Identifies huge wave cleared.</summary>
        HugeWaveCleared,
        /// <summary>Identifies final wave sound.</summary>
        FinalWaveSound,
        /// <summary>Identifies siren sound.</summary>
        SirenSound
    }
}
