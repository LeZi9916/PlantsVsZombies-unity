#nullable enable
using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Describes bounded reusable capacity without advancing gameplay or random state.</summary>
    internal readonly struct SimulationCapacityRequirements
    {
        /// <summary>Bounds inactive sound storage independently of short-lived visual events.</summary>
        internal const int MaximumSoundEvents = 8192;
        /// <summary>Gets the prepared plant population.</summary>
        internal int Plants { get; }
        /// <summary>Gets the prepared zombie population, including retained corpses.</summary>
        internal int Zombies { get; }
        /// <summary>Gets the prepared projectile population.</summary>
        internal int Projectiles { get; }
        /// <summary>Gets the prepared capacity of each short-lived event type.</summary>
        internal int Events { get; }
        /// <summary>Gets the independently prepared sound-history and grace-period capacity.</summary>
        internal int SoundEvents { get; }
        /// <summary>Gets the prepared count of each behavior and capability type.</summary>
        internal int Capabilities { get; }

        /// <summary>Creates bounded capacity requirements for incremental loading preparation.</summary>
        /// <param name="plants">The target plant population.</param>
        /// <param name="zombies">The target zombie and corpse population.</param>
        /// <param name="projectiles">The target projectile population.</param>
        /// <param name="events">The target count of each event type.</param>
        /// <param name="capabilities">The target count of each capability type.</param>
        /// <param name="soundEvents">The optional sound-history target; omitted values follow the visual event target for small fixtures.</param>
        internal SimulationCapacityRequirements(int plants, int zombies, int projectiles, int events, int capabilities, int? soundEvents = null)
        {
            Plants = Math.Clamp(plants, 0, 512);
            Zombies = Math.Clamp(zombies, 0, 1024);
            Projectiles = Math.Clamp(projectiles, 0, 256);
            Events = Math.Clamp(events, 0, 512);
            Capabilities = Math.Clamp(capabilities, 0, 128);
            SoundEvents = Math.Clamp(soundEvents ?? Events, 0, MaximumSoundEvents);
        }
    }
}
