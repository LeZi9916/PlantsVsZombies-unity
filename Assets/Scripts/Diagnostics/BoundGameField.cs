#nullable enable
using System;
using System.Reflection;

namespace PlantsVsZombies.Diagnostics
{
    /// <summary>A field of a game session or one of its collaborators, resolved once and addressed without naming its owner.</summary>
    internal sealed class BoundGameField
    {
        /// <summary>Provides the field metadata.</summary>
        private readonly FieldInfo _field;
        /// <summary>Provides the object that declares the field.</summary>
        private readonly object _owner;

        /// <summary>Binds a field to the object that declares it.</summary>
        /// <param name="field">The field metadata.</param>
        /// <param name="owner">The session or collaborator whose field is read and written.</param>
        internal BoundGameField(FieldInfo field, object owner)
        {
            _field = field;
            _owner = owner;
        }

        /// <summary>Gets the declared type of the field.</summary>
        internal Type FieldType => _field.FieldType;

        /// <summary>Reads the bound field.</summary>
        /// <param name="ignored">An unused instance, kept so call sites read like <see cref="FieldInfo.GetValue(object)"/>.</param>
        /// <returns>The current field value; null when the field holds null, as with the reflection call it mirrors.</returns>
        internal object GetValue(object? ignored)
        {
            return _field.GetValue(_owner)!;
        }

        /// <summary>Writes the bound field.</summary>
        /// <param name="ignored">An unused instance, kept so call sites read like <see cref="FieldInfo.SetValue(object,object)"/>.</param>
        /// <param name="value">The value to store.</param>
        internal void SetValue(object? ignored, object? value)
        {
            _field.SetValue(_owner, value);
        }
    }
}
