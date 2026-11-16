#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Editor
{
    /// <summary>Exposes saved model values while retaining graph identities and writing boxed structures back to their owners.</summary>
    internal sealed class MatchCheckpointFieldNode
    {
        /// <summary>Reads the current value instead of retaining a stale boxed structure.</summary>
        private readonly Func<object?> _read;
        /// <summary>Writes a scalar or a changed boxed structure back to its actual owner.</summary>
        private readonly Action<object?>? _write;
        /// <summary>Gets the stable native field or collection entry name.</summary>
        internal string Name { get; }
        /// <summary>Gets the stable UI expansion path.</summary>
        internal string Path { get; }
        /// <summary>Gets the declared scalar or model type.</summary>
        internal Type ValueType { get; }
        /// <summary>Gets whether this entire subtree is immutable in the editor.</summary>
        internal bool ReadOnly { get; }
        /// <summary>Gets the current value without invoking gameplay model getters.</summary>
        internal object? Value => _read();
        /// <summary>Gets whether this node can write a scalar back to the original owner.</summary>
        internal bool CanWrite => !ReadOnly && _write != null;

        /// <summary>Creates a model, field or logical collection entry.</summary>
        /// <param name="name">The displayed field name.</param>
        /// <param name="path">The stable expansion path.</param>
        /// <param name="type">The declared value type.</param>
        /// <param name="read">Reads the current actual value.</param>
        /// <param name="write">Writes changes back, or null for a nonreplaceable reference.</param>
        /// <param name="readOnly">Whether this subtree is immutable.</param>
        internal MatchCheckpointFieldNode(string name, string path, Type type, Func<object?> read, Action<object?>? write = null, bool readOnly = false)
        {
            Name = name;
            Path = path;
            ValueType = type;
            _read = read;
            _write = write;
            ReadOnly = readOnly;
        }

        /// <summary>Writes an edited scalar while rejecting immutable nodes and nonfinite user input.</summary>
        /// <param name="value">The edited value with its original declared type.</param>
        /// <exception cref="InvalidOperationException">This node is read-only.</exception>
        /// <exception cref="ArgumentException">The value has an incompatible type or is not finite.</exception>
        internal void SetValue(object? value)
        {
            if (!CanWrite)
            {
                throw new InvalidOperationException("该字段为只读。");
            }
            if (value is float single && (float.IsNaN(single) || float.IsInfinity(single))
                || value is double number && (double.IsNaN(number) || double.IsInfinity(number)))
            {
                throw new ArgumentException("输入值必须是有限数字。", nameof(value));
            }
            _write!(value);
        }

        /// <summary>Gets model-field count or logical collection length without exposing indexes or BCL storage.</summary>
        internal int ChildCount
        {
            get
            {
                var value = Value;
                if (value == null || IsScalar(value.GetType()))
                {
                    return 0;
                }
                if (value is ICollection collection)
                {
                    return collection.Count;
                }
                if (value is IEnumerable)
                {
                    return (int)value.GetType().GetProperty("Count")!.GetValue(value)!;
                }
                return MatchCheckpointCodec.Fields(value.GetType()).Count;
            }
        }

        /// <summary>Builds only the visible page of logical model children.</summary>
        /// <param name="offset">The first child index.</param>
        /// <param name="count">The maximum visible child count.</param>
        /// <returns>The requested page in native field or collection order.</returns>
        internal IReadOnlyList<MatchCheckpointFieldNode> Children(int offset, int count)
        {
            var result = new List<MatchCheckpointFieldNode>();
            var value = Value;
            if (value == null || IsScalar(value.GetType()))
            {
                return result;
            }
            if (value is IDictionary dictionary)
            {
                var index = 0;
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (index >= offset && index < offset + count)
                    {
                        var key = entry.Key;
                        var itemType = value.GetType().GetGenericArguments()[1];
                        result.Add(new MatchCheckpointFieldNode("[" + key + "]", Path + "/" + index, itemType,
                            () => dictionary[key], dictionary.IsReadOnly ? null : edited => dictionary[key] = edited, ReadOnly));
                    }
                    if (++index >= offset + count)
                    {
                        break;
                    }
                }
            }
            else if (value is Array array)
            {
                for (var index = offset; index < Math.Min(array.Length, offset + count); index++)
                {
                    var coordinates = Coordinates(array, index);
                    var label = "[" + string.Join(",", coordinates) + "]";
                    if (Path.Contains("_seed") && array.Rank == 1)
                    {
                        label += " " + (PlantKind)index;
                    }
                    result.Add(new MatchCheckpointFieldNode(label, Path + "/" + index, array.GetType().GetElementType()!,
                        () => array.GetValue(coordinates), edited => array.SetValue(edited, coordinates), ReadOnly));
                }
            }
            else if (value is IEnumerable sequence)
            {
                var list = value as IList;
                var index = 0;
                foreach (var item in sequence)
                {
                    if (index >= offset && index < offset + count)
                    {
                        var position = index;
                        var retained = item;
                        var itemType = value.GetType().GetGenericArguments()[0];
                        result.Add(new MatchCheckpointFieldNode("[" + index + "]", Path + "/" + index, itemType,
                            list == null ? () => retained : () => list[position],
                            list == null || list.IsReadOnly ? null : edited => list[position] = edited, ReadOnly));
                    }
                    if (++index >= offset + count)
                    {
                        break;
                    }
                }
            }
            else
            {
                var fields = MatchCheckpointCodec.Fields(value.GetType());
                for (var index = offset; index < Math.Min(fields.Count, offset + count); index++)
                {
                    var field = fields[index];
                    var name = FieldName(field.Name);
                    var readOnly = ReadOnly || IsIdentity(name) || value.GetType().IsValueType && !CanWrite;
                    // A boxed value snapshot can be rebuilt even when its individual fields
                    // are readonly; the completed value is written back through its owner.
                    Action<object?>? write = field.IsInitOnly && !value.GetType().IsValueType ? null : edited =>
                    {
                        var owner = Value!;
                        var property = field.DeclaringType!.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (property?.PropertyType == field.FieldType && property.GetSetMethod(true) != null)
                        {
                            property.SetValue(owner, edited);
                        }
                        else
                        {
                            field.SetValue(owner, edited);
                        }
                        if (owner.GetType().IsValueType)
                        {
                            SetValue(owner);
                        }
                    };
                    result.Add(new MatchCheckpointFieldNode(name, Path + "/" + field.Name, field.FieldType,
                        () => field.GetValue(Value), write, readOnly));
                }
            }
            return result;
        }

        /// <summary>Identifies leaf values represented by a native editor field.</summary>
        /// <param name="type">The model field type.</param>
        /// <returns>Whether the type is a scalar.</returns>
        internal static bool IsScalar(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsEnum || type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(Guid) || type == typeof(DateTime);
        }

        /// <summary>Converts compiler backing names into the corresponding visible property name.</summary>
        /// <param name="name">The actual saved field name.</param>
        /// <returns>The model property or unchanged native state name.</returns>
        private static string FieldName(string name)
        {
            if (name.StartsWith("<", StringComparison.Ordinal))
            {
                return name.Substring(1, name.IndexOf('>') - 1);
            }
            if (name.Length > 1 && name[0] == '_' && char.IsLower(name[1]))
            {
                return char.ToUpperInvariant(name[1]) + name.Substring(2);
            }
            return name;
        }

        /// <summary>Protects persistent identities, type tags, immutable definitions and random streams.</summary>
        /// <param name="name">The normalized model field name.</param>
        /// <returns>Whether the entire field subtree is read-only.</returns>
        private static bool IsIdentity(string name)
        {
            return name.EndsWith("Id", StringComparison.Ordinal) || name.EndsWith("Kind", StringComparison.Ordinal) || name == "Definition"
                || name == "Level" || name == "Rules" || name == "WaveContext" || name == "Waves" || name == "RowCount"
                || name == "IsAdventure" || name == "IsExperimentalLevelOne" || name == "Random" || name == "FoleyRandom"
                || name == "AvailableSeeds" || name == "RequiredSeeds";
        }

        /// <summary>Maps a logical linear position to the original multidimensional array coordinates.</summary>
        /// <param name="array">The saved array.</param>
        /// <param name="position">The logical item offset.</param>
        /// <returns>The coordinates accepted by GetValue and SetValue.</returns>
        private static int[] Coordinates(Array array, int position)
        {
            var coordinates = new int[array.Rank];
            for (var dimension = array.Rank - 1; dimension >= 0; dimension--)
            {
                coordinates[dimension] = position % array.GetLength(dimension);
                position /= array.GetLength(dimension);
            }
            return coordinates;
        }
    }
}
