#nullable enable

namespace PlantsVsZombies.Simulation
{
    /// <summary>Identifies the original ordered species-action slots within a zombie update.</summary>
    internal enum ZombieActionPhase
    {
        /// <summary>Runs the Wind action slot.</summary>
        Wind,
        /// <summary>Runs the TimedSpecial action slot.</summary>
        TimedSpecial,
        /// <summary>Runs the Motion action slot.</summary>
        Motion,
        /// <summary>Runs the AfterMotion action slot.</summary>
        AfterMotion,
        /// <summary>Runs the Carrier action slot.</summary>
        Carrier,
        /// <summary>Runs the Dance action slot.</summary>
        Dance,
        /// <summary>Runs the Giant action slot.</summary>
        Giant,
        /// <summary>Runs the Equipment action slot.</summary>
        Equipment,
        /// <summary>Runs the Underground action slot.</summary>
        Underground,
        /// <summary>Runs the Vehicle action slot.</summary>
        Vehicle,
        /// <summary>Runs the AfterWalking action slot.</summary>
        AfterWalking,
    }
}
