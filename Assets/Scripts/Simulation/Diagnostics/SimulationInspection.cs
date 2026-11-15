#nullable enable

using System.Reflection;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Resolves diagnostic fixture access across simulation collaborators.</summary>
    internal static class SimulationInspection
    {
        /// <summary>Locates a method without binding fixtures to its owning collaborator.</summary>
        /// <param name="name">The method name.</param>
        /// <param name="flags">The original reflection flags.</param>
        /// <returns>A deferred member accessor.</returns>
        internal static SimulationMember GetMethod(string name, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            return new SimulationMember(name);
        }
        /// <summary>Locates a field for isolated fixture setup.</summary>
        /// <param name="name">The field name.</param>
        /// <param name="flags">The original reflection flags.</param>
        /// <returns>A deferred member accessor.</returns>
        internal static SimulationMember GetField(string name, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            return new SimulationMember(name);
        }
        /// <summary>Locates a property for isolated fixture setup.</summary>
        /// <param name="name">The property name.</param>
        /// <param name="flags">The original reflection flags.</param>
        /// <returns>A deferred member accessor.</returns>
        internal static SimulationMember GetProperty(string name, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            return new SimulationMember(name);
        }
    }
}
