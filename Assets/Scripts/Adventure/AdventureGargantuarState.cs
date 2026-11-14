#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Retains the PC giant's authored weapon and once-per-clip action event.</summary>
    public sealed class AdventureGargantuarState
    {
        /// <summary>Identifies the original pole, crossing sign or zombie weapon variation.</summary>
        public int Weapon;
        /// <summary>Prevents repeating the current smash or throw animation's timed event.</summary>
        public bool ActionTriggered;
    }
}
