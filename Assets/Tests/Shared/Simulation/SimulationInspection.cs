#nullable enable

using System;
using System.Reflection;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Resolves diagnostic fixture access across simulation collaborators.</summary>
    internal static class SimulationInspection
    {
        /// <summary>Locates a collaborator for diagnostics without exposing mutable session state.</summary>
        /// <param name="game">The isolated match being inspected.</param>
        /// <param name="name">The member to inspect.</param>
        /// <returns>The unique member owner.</returns>
        /// <exception cref="ArgumentException">The name does not identify a collaborator member.</exception>
        internal static object MemberOwner(this GameSimulation game, string name)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            foreach (var field in typeof(GameSimulation).GetFields(flags))
            {
                var owner = field.GetValue(game);
                if (owner != null && owner.GetType().GetMember(name, flags).Length > 0)
                {
                    return owner;
                }
            }

            throw new ArgumentException("Unknown simulation member: " + name, nameof(name));
        }

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
