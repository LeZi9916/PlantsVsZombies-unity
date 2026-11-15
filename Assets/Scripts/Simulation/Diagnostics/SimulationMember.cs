#nullable enable
using System;

using System.Reflection;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Routes diagnostic access to the declared owner of simulation state or behavior.</summary>
    internal sealed class SimulationMember
    {
        /// <summary>Identifies the inspected member.</summary>
        private readonly string _name;
        /// <summary>Includes members regardless of their production visibility.</summary>
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        /// <summary>Creates a deferred diagnostic lookup.</summary>
        /// <param name="name">The member name.</param>
        internal SimulationMember(string name)
        {
            _name = name;
        }
        /// <summary>Invokes a simulation operation for an isolated fixture.</summary>
        /// <param name="simulation">The match under inspection.</param>
        /// <param name="arguments">The original invocation arguments.</param>
        /// <returns>The operation result.</returns>
        internal object? Invoke(object? simulation, object?[]? arguments)
        {
            var owner = ((GameSimulation)simulation!).MemberOwner(_name);
            foreach (var method in owner.GetType().GetMethods(Flags))
            {
                if (method.Name == _name && method.GetParameters().Length == (arguments?.Length ?? 0))
                {
                    return method.Invoke(owner, arguments);
                }
            }

            throw new MissingMethodException(owner.GetType().Name, _name);
        }
        /// <summary>Gets the operation parameters for optional fixture arguments.</summary>
        /// <param name="simulation">The inspected match.</param>
        /// <returns>The declared parameters.</returns>
        internal ParameterInfo[] GetParameters(GameSimulation simulation)
        {
            var owner = simulation.MemberOwner(_name);
            return owner.GetType().GetMethod(_name, Flags)!.GetParameters();
        }
        /// <summary>Reads a fixture observation.</summary>
        /// <param name="simulation">The inspected match.</param>
        /// <returns>The stored value.</returns>
        internal object? GetValue(object simulation)
        {
            var owner = ((GameSimulation)simulation).MemberOwner(_name);
            var field = owner.GetType().GetField(_name, Flags);
            return field != null ? field.GetValue(owner) : owner.GetType().GetProperty(_name, Flags)!.GetValue(owner);
        }
        /// <summary>Sets isolated fixture state on its actual owner.</summary>
        /// <param name="simulation">The inspected match.</param>
        /// <param name="value">The fixture value.</param>
        internal void SetValue(object simulation, object? value)
        {
            var owner = ((GameSimulation)simulation).MemberOwner(_name);
            var field = owner.GetType().GetField(_name, Flags);
            if (field != null)
            {
                field.SetValue(owner, value);
            }
            else
            {
                owner.GetType().GetProperty(_name, Flags)!.SetValue(owner, value);
            }
        }
    }
}
