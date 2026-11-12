#nullable enable
using System.Reflection;

namespace PlantsVsZombies.Diagnostics
{
    /// <summary>A method of a game session or one of its collaborators, resolved once and invoked without naming its owner.</summary>
    internal sealed class BoundGameMethod
    {
        /// <summary>Provides the method metadata.</summary>
        private readonly MethodInfo _method;
        /// <summary>Provides the object that declares the method.</summary>
        private readonly object _owner;

        /// <summary>Binds a method to the object that declares it.</summary>
        /// <param name="method">The method metadata.</param>
        /// <param name="owner">The session or collaborator the method runs on.</param>
        internal BoundGameMethod(MethodInfo method, object owner)
        {
            _method = method;
            _owner = owner;
        }

        /// <summary>Gets the declared parameters of the method.</summary>
        /// <returns>The parameter metadata in declaration order.</returns>
        internal ParameterInfo[] GetParameters()
        {
            return _method.GetParameters();
        }

        /// <summary>Invokes the bound method.</summary>
        /// <param name="ignored">An unused instance, kept so call sites read like <see cref="MethodBase.Invoke(object,object[])"/>.</param>
        /// <param name="arguments">The method arguments, or null for a method without parameters.</param>
        /// <returns>The method result, or null for a void method, as with the reflection call it mirrors.</returns>
        internal object Invoke(object? ignored, object?[]? arguments)
        {
            return _method.Invoke(_owner, arguments)!;
        }
    }
}
