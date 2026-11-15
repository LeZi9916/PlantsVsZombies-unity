#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using PlantsVsZombies.Randomness;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Stores the authoritative model graph, retaining entity links and native clocks.</summary>
    /// <remarks>
    /// This is a replica format, not the PC executable's pointer-based game file. Only model
    /// fields are included; services, delegates, derived indexes and reusable buffers are rebuilt.
    /// Field names are checked so an incompatible model cannot silently produce a partial board.
    /// </remarks>
    internal sealed class MatchCheckpointCodec
    {
        /// <summary>Bounds graph work and allocation for damaged local files.</summary>
        private const int MaxEntries = 250000;
        /// <summary>Bounds recursive model nesting.</summary>
        private const int MaxDepth = 128;
        /// <summary>Indexes only types in the simulation's own assembly.</summary>
        private static readonly Dictionary<string, Type> s_types = CreateTypes();
        /// <summary>Caches the immutable schema selected for each model type.</summary>
        private static readonly Dictionary<Type, List<FieldInfo>> s_fields = new Dictionary<Type, List<FieldInfo>>();
        /// <summary>Stores each type descriptor once per file.</summary>
        private readonly Dictionary<Type, int> _writtenTypes = new Dictionary<Type, int>();
        /// <summary>Resolves previously checked type descriptors.</summary>
        private readonly List<Type?> _readTypes = new List<Type?>();
        /// <summary>Tracks model schemas already emitted in this file.</summary>
        private readonly HashSet<Type> _writtenSchemas = new HashSet<Type>();
        /// <summary>Tracks model schemas already verified in this file.</summary>
        private readonly HashSet<Type> _readSchemas = new HashSet<Type>();
        /// <summary>Retains graph identities using reference equality, including entity dictionary keys.</summary>
        private readonly Dictionary<object, int> _written = new Dictionary<object, int>(ReferenceComparer.Instance);
        /// <summary>Retains restored identities before reading their child fields.</summary>
        private readonly List<object?> _read = new List<object?>();
        /// <summary>Counts collection elements across the entire graph.</summary>
        private int _entries;

        /// <summary>Writes one complete world without advancing simulation or invoking callbacks.</summary>
        /// <param name="world">The current authoritative board.</param>
        /// <returns>The versioned model bytes.</returns>
        /// <exception cref="InvalidDataException">The model is unsupported, damaged, incompatible or exceeds the checkpoint limits.</exception>
        internal static byte[] Capture(SimulationWorld world)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(1);
            new MatchCheckpointCodec().Write(writer, world, 0);
            return stream.ToArray();
        }

        /// <summary>Reads a detached board; callers publish it only after the complete file validates.</summary>
        /// <param name="bytes">The checked model payload.</param>
        /// <returns>The restored authoritative world.</returns>
        /// <exception cref="InvalidDataException">The payload is damaged or incompatible.</exception>
        internal static SimulationWorld Restore(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadInt32() != 1)
            {
                throw new InvalidDataException("Unsupported match version.");
            }
            var world = new MatchCheckpointCodec().Read(reader, 0) as SimulationWorld;
            if (world == null || stream.Position != stream.Length || !world.IsAdventure || world._adventure == null
                || world.Rules == null || !(world._random is OriginalRandomAdapter) || world.RowCount < 1 || world.RowCount > 6)
            {
                throw new InvalidDataException("Invalid match state.");
            }
            return world;
        }

        /// <summary>Writes a scalar, a previous identity, or a new typed model.</summary>
        /// <param name="writer">The binary destination.</param>
        /// <param name="value">The model value.</param>
        /// <param name="depth">The bounded recursion depth.</param>
        /// <exception cref="InvalidDataException">The model is unsupported, damaged, incompatible or exceeds the checkpoint limits.</exception>
        private void Write(BinaryWriter writer, object? value, int depth)
        {
            CheckDepth(depth);
            if (value == null)
            {
                writer.Write((byte)0);
                return;
            }
            var type = value.GetType();
            if (type.IsEnum || Type.GetTypeCode(type) != TypeCode.Object || type == typeof(Guid))
            {
                writer.Write((byte)3);
                WriteType(writer, type);
                if (type.IsEnum)
                {
                    writer.Write(Convert.ToInt64(value));
                }
                else
                {
                    WriteScalar(writer, value, type);
                }
                return;
            }
            if (_written.TryGetValue(value, out var previous))
            {
                writer.Write((byte)1);
                writer.Write(previous);
                return;
            }
            AddEntries(1);
            _written.Add(value, _written.Count);
            writer.Write((byte)2);
            WriteType(writer, type);
            if (type.IsEnum)
            {
                writer.Write(Convert.ToInt64(value));
            }
            else if (WriteScalar(writer, value, type))
            {
            }
            else if (type.IsArray)
            {
                var array = (Array)value;
                AddEntries(array.Length);
                for (var dimension = 0; dimension < array.Rank; dimension++)
                {
                    writer.Write(array.GetLength(dimension));
                }
                foreach (var item in array)
                {
                    Write(writer, item, depth + 1);
                }
            }
            else if (IsReadOnlyCollection(type))
            {
                Write(writer, type.GetProperty("Items", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value), depth + 1);
            }
            else if (value is IDictionary dictionary)
            {
                AddEntries(dictionary.Count);
                writer.Write(dictionary.Count);
                foreach (DictionaryEntry pair in dictionary)
                {
                    Write(writer, pair.Key, depth + 1);
                    Write(writer, pair.Value, depth + 1);
                }
            }
            else if (IsSequence(type))
            {
                var count = (int)type.GetProperty("Count")!.GetValue(value)!;
                AddEntries(count);
                writer.Write(count);
                foreach (var item in (IEnumerable)value)
                {
                    Write(writer, item, depth + 1);
                }
            }
            else
            {
                var fields = Fields(type);
                if (_writtenSchemas.Add(type))
                {
                    writer.Write(fields.Count);
                    foreach (var field in fields)
                    {
                        writer.Write(field.DeclaringType!.FullName + ":" + field.Name);
                    }
                }
                foreach (var field in fields)
                {
                    Write(writer, field.GetValue(value), depth + 1);
                }
            }
        }

        /// <summary>Reads a typed value while preserving shared references.</summary>
        /// <param name="reader">The binary source.</param>
        /// <param name="depth">The bounded recursion depth.</param>
        /// <returns>The reconstructed value.</returns>
        /// <exception cref="InvalidDataException">The model is unsupported, damaged, incompatible or exceeds the checkpoint limits.</exception>
        private object? Read(BinaryReader reader, int depth)
        {
            CheckDepth(depth);
            var tag = reader.ReadByte();
            if (tag == 0)
            {
                return null;
            }
            if (tag == 1)
            {
                var index = reader.ReadInt32();
                if (index < 0 || index >= _read.Count || _read[index] == null)
                {
                    throw new InvalidDataException("Invalid match reference.");
                }
                return _read[index];
            }
            if (tag == 3)
            {
                var scalarType = ReadType(reader, 0);
                if (scalarType.IsEnum)
                {
                    return Enum.ToObject(scalarType, reader.ReadInt64());
                }
                if (ReadScalar(reader, scalarType, out var scalarValue))
                {
                    return scalarValue;
                }
                throw new InvalidDataException("Invalid match scalar.");
            }
            if (tag != 2)
            {
                throw new InvalidDataException("Invalid match value.");
            }
            AddEntries(1);
            var id = _read.Count;
            _read.Add(null);
            var type = ReadType(reader, 0);
            object value;
            if (type.IsEnum)
            {
                value = Enum.ToObject(type, reader.ReadInt64());
            }
            else if (ReadScalar(reader, type, out var scalar))
            {
                value = scalar!;
            }
            else if (type.IsArray)
            {
                var lengths = new int[type.GetArrayRank()];
                long total = 1;
                for (var dimension = 0; dimension < lengths.Length; dimension++)
                {
                    lengths[dimension] = ReadCount(reader);
                    total *= lengths[dimension];
                    if (total > MaxEntries)
                    {
                        throw new InvalidDataException("Match array is too large.");
                    }
                }
                AddEntries((int)total);
                var array = Array.CreateInstance(type.GetElementType()!, lengths);
                value = array;
                _read[id] = value;
                var indices = new int[lengths.Length];
                for (var item = 0; item < total; item++)
                {
                    array.SetValue(Read(reader, depth + 1), indices);
                    for (var dimension = lengths.Length - 1; dimension >= 0; dimension--)
                    {
                        if (++indices[dimension] < lengths[dimension])
                        {
                            break;
                        }
                        indices[dimension] = 0;
                    }
                }
            }
            else if (IsReadOnlyCollection(type))
            {
                value = Activator.CreateInstance(type, Read(reader, depth + 1))!;
            }
            else if (typeof(IDictionary).IsAssignableFrom(type) || IsSequence(type))
            {
                value = CreateCollection(type);
                _read[id] = value;
                var count = ReadCount(reader);
                AddEntries(count);
                if (value is IDictionary dictionary)
                {
                    for (var item = 0; item < count; item++)
                    {
                        dictionary.Add(Read(reader, depth + 1)!, Read(reader, depth + 1));
                    }
                }
                else
                {
                    var name = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Queue<>) ? "Enqueue" : "Add";
                    var add = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
                    for (var item = 0; item < count; item++)
                    {
                        add.Invoke(value, new[] { Read(reader, depth + 1) });
                    }
                }
            }
            else
            {
                // No gameplay constructor is run: constructors can consume RNG or create new entities.
                value = type == typeof(SimulationWorld) ? new SimulationWorld()
                    : type == typeof(OriginalRandomAdapter) ? new OriginalRandomAdapter(1)
                    : CreateModel(type);
                _read[id] = value;
                var fields = Fields(type);
                if (_readSchemas.Add(type))
                {
                    if (reader.ReadInt32() != fields.Count)
                    {
                        throw new InvalidDataException("Match model fields have changed.");
                    }
                    foreach (var field in fields)
                    {
                        if (reader.ReadString() != field.DeclaringType!.FullName + ":" + field.Name)
                        {
                            throw new InvalidDataException("Incompatible match field.");
                        }
                    }
                }
                foreach (var field in fields)
                {
                    field.SetValue(value, Read(reader, depth + 1));
                }
            }
            _read[id] = value;
            return value;
        }

        /// <summary>Creates collections with their normal invariants and entity index callbacks.</summary>
        /// <param name="type">The allowlisted collection type.</param>
        /// <returns>An empty collection.</returns>
        private static object CreateCollection(Type type)
        {
            if (type == typeof(EntityStore<Plant>))
            {
                return new EntityStore<Plant>(entity => entity.Id, entity => entity.Row, (entity, callback) => entity.IndexChanged = callback);
            }
            if (type == typeof(EntityStore<Zombie>))
            {
                return new EntityStore<Zombie>(entity => entity.Id, entity => entity.Row, (entity, callback) => entity.IndexChanged = callback);
            }
            return Activator.CreateInstance(type)!;
        }

        /// <summary>Allocates model storage without executing gameplay constructors.</summary>
        /// <param name="type">The allowlisted model type.</param>
        /// <returns>An uninitialized model whose complete field schema is populated before publication.</returns>
        private static object CreateModel(Type type)
        {
#if NET5_0_OR_GREATER
            return RuntimeHelpers.GetUninitializedObject(type);
#else
            return FormatterServices.GetUninitializedObject(type);
#endif
        }

        /// <summary>Selects model fields while excluding host callbacks and disposable derived storage.</summary>
        /// <param name="type">The model type.</param>
        /// <returns>Deterministically ordered instance fields.</returns>
        private static List<FieldInfo> Fields(Type type)
        {
            lock (s_fields)
            {
                if (!s_fields.TryGetValue(type, out var fields))
                {
                    fields = BuildFields(type);
                    s_fields.Add(type, fields);
                }
                return fields;
            }
        }

        /// <summary>Builds a schema once, preserving private capability state and excluding external services.</summary>
        /// <param name="type">The model type.</param>
        /// <returns>The ordered model fields.</returns>
        private static List<FieldInfo> BuildFields(Type type)
        {
            var fields = new List<FieldInfo>();
            for (var current = type; current != null && current.Assembly == typeof(SimulationWorld).Assembly; current = current.BaseType)
            {
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (typeof(Delegate).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(GameSimulation))
                    {
                        continue;
                    }
                    if (type == typeof(SimulationWorld) && (field.Name.Contains("View") || field.Name.Contains("Scratch")
                        || field.Name.EndsWith("Pool", StringComparison.Ordinal) || field.Name == "_packetFeedbackContext"))
                    {
                        continue;
                    }
                    fields.Add(field);
                }
            }
            fields.Sort((left, right) => string.CompareOrdinal(left.DeclaringType!.FullName + ":" + left.Name, right.DeclaringType!.FullName + ":" + right.Name));
            return fields;
        }

        /// <summary>Identifies the supported collections whose logical order is saved.</summary>
        /// <param name="type">The candidate type.</param>
        /// <returns>Whether the type is an ordered or set collection.</returns>
        private static bool IsSequence(Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }
            var definition = type.GetGenericTypeDefinition();
            return definition == typeof(List<>) || definition == typeof(HashSet<>) || definition == typeof(Queue<>) || definition == typeof(EntityStore<>);
        }

        /// <summary>Identifies read-only views whose backing list identity must be retained.</summary>
        /// <param name="type">The candidate type.</param>
        /// <returns>Whether the type is a read-only collection.</returns>
        private static bool IsReadOnlyCollection(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>);
        }

        /// <summary>Builds the closed model type allowlist without resolving types supplied by a file.</summary>
        /// <returns>Known model and scalar names.</returns>
        private static Dictionary<string, Type> CreateTypes()
        {
            var result = new Dictionary<string, Type>(StringComparer.Ordinal);
            var prefixes = new[] { "Simulation", "Adventure", "Modes", "Levels", "Plants", "Randomness", "Sun", "Timing", "Waves", "VerifiedData" };
            foreach (var type in typeof(SimulationWorld).Assembly.GetTypes())
            {
                foreach (var prefix in prefixes)
                {
                    if (type.Namespace == "PlantsVsZombies." + prefix && IsModelType(type))
                    {
                        result[type.FullName!] = type;
                        break;
                    }
                }
            }
            foreach (var type in new[] { typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(char), typeof(string), typeof(decimal), typeof(Guid), typeof(DateTime), typeof(OriginalRandomAdapter) })
            {
                result[type.FullName!] = type;
            }
            return result;
        }

        /// <summary>Admits project models while excluding delegates and classes backed by external engine/service bases.</summary>
        /// <param name="type">The type from the simulation assembly.</param>
        /// <returns>Whether the type can represent detached model storage.</returns>
        private static bool IsModelType(Type type)
        {
            if (type.ContainsGenericParameters || type.IsAbstract || type.IsInterface || typeof(Delegate).IsAssignableFrom(type))
            {
                return false;
            }
            var ancestor = type.BaseType;
            while (ancestor != null && ancestor.Assembly == typeof(SimulationWorld).Assembly)
            {
                ancestor = ancestor.BaseType;
            }
            return ancestor == null || ancestor == typeof(object) || ancestor == typeof(ValueType) || ancestor == typeof(Enum)
                || type == typeof(OriginalRandomAdapter);
        }

        /// <summary>Writes a structural type name from the closed set of model and collection kinds.</summary>
        /// <param name="writer">The destination.</param>
        /// <param name="type">The runtime model type.</param>
        private void WriteType(BinaryWriter writer, Type type)
        {
            if (_writtenTypes.TryGetValue(type, out var id))
            {
                writer.Write(id);
                return;
            }
            id = _writtenTypes.Count;
            _writtenTypes.Add(type, id);
            writer.Write(-id - 1);
            WriteNewType(writer, type);
        }

        /// <summary>Writes the first structural descriptor of a model or supported collection.</summary>
        /// <param name="writer">The destination.</param>
        /// <param name="type">The new type.</param>
        /// <exception cref="InvalidDataException">The model is unsupported, damaged, incompatible or exceeds the checkpoint limits.</exception>
        private void WriteNewType(BinaryWriter writer, Type type)
        {
            if (type.IsArray)
            {
                writer.Write("array");
                writer.Write(type.GetArrayRank());
                WriteType(writer, type.GetElementType()!);
            }
            else if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                var name = definition == typeof(Dictionary<,>) ? "dictionary" : definition == typeof(List<>) ? "list"
                    : definition == typeof(HashSet<>) ? "set" : definition == typeof(Queue<>) ? "queue"
                    : definition == typeof(ReadOnlyCollection<>) ? "readonly" : definition == typeof(EntityStore<>) ? "entities" : null;
                if (name == null)
                {
                    throw new InvalidDataException("Unsupported match collection: " + type);
                }
                writer.Write(name);
                foreach (var argument in type.GetGenericArguments())
                {
                    WriteType(writer, argument);
                }
            }
            else
            {
                if (!s_types.ContainsKey(type.FullName!))
                {
                    throw new InvalidDataException("Unsupported match model: " + type);
                }
                writer.Write(type.FullName!);
            }
        }

        /// <summary>Reads structural types without arbitrary assembly loading.</summary>
        /// <param name="reader">The source.</param>
        /// <param name="depth">The bounded type nesting.</param>
        /// <returns>The allowlisted type.</returns>
        /// <exception cref="InvalidDataException">The model is unsupported, damaged, incompatible or exceeds the checkpoint limits.</exception>
        private Type ReadType(BinaryReader reader, int depth)
        {
            CheckDepth(depth);
            var id = reader.ReadInt32();
            if (id >= 0)
            {
                if (id >= _readTypes.Count || _readTypes[id] == null)
                {
                    throw new InvalidDataException("Invalid match type reference.");
                }
                return _readTypes[id]!;
            }
            if (-id - 1 != _readTypes.Count || _readTypes.Count >= MaxEntries)
            {
                throw new InvalidDataException("Invalid match type definition.");
            }
            var index = _readTypes.Count;
            _readTypes.Add(null);
            var type = ReadNewType(reader, depth);
            _readTypes[index] = type;
            return type;
        }

        /// <summary>Validates the first structural descriptor of a model or supported collection.</summary>
        /// <param name="reader">The source.</param>
        /// <param name="depth">The bounded type nesting.</param>
        /// <returns>The validated type.</returns>
        /// <exception cref="InvalidDataException">The model is unsupported, damaged, incompatible or exceeds the checkpoint limits.</exception>
        private Type ReadNewType(BinaryReader reader, int depth)
        {
            CheckDepth(depth);
            var name = reader.ReadString();
            if (s_types.TryGetValue(name, out var type))
            {
                return type;
            }
            if (name == "array")
            {
                var rank = reader.ReadInt32();
                if (rank < 1 || rank > 3)
                {
                    throw new InvalidDataException("Invalid match array rank.");
                }
                var element = ReadType(reader, depth + 1);
                return rank == 1 ? element.MakeArrayType() : element.MakeArrayType(rank);
            }
            var definition = name == "dictionary" ? typeof(Dictionary<,>) : name == "list" ? typeof(List<>)
                : name == "set" ? typeof(HashSet<>) : name == "queue" ? typeof(Queue<>)
                : name == "readonly" ? typeof(ReadOnlyCollection<>) : name == "entities" ? typeof(EntityStore<>) : null;
            if (definition == null)
            {
                throw new InvalidDataException("Unknown match model.");
            }
            var arguments = new Type[definition.GetGenericArguments().Length];
            for (var index = 0; index < arguments.Length; index++)
            {
                arguments[index] = ReadType(reader, depth + 1);
            }
            return definition.MakeGenericType(arguments);
        }

        /// <summary>Writes scalar values exactly, including floating-point native clocks.</summary>
        /// <param name="writer">The destination.</param>
        /// <param name="value">The scalar value.</param>
        /// <param name="type">Its scalar type.</param>
        /// <returns>Whether the value was scalar.</returns>
        private static bool WriteScalar(BinaryWriter writer, object value, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    writer.Write((bool)value);
                    return true;
                case TypeCode.Byte:
                    writer.Write((byte)value);
                    return true;
                case TypeCode.SByte:
                    writer.Write((sbyte)value);
                    return true;
                case TypeCode.Int16:
                    writer.Write((short)value);
                    return true;
                case TypeCode.UInt16:
                    writer.Write((ushort)value);
                    return true;
                case TypeCode.Int32:
                    writer.Write((int)value);
                    return true;
                case TypeCode.UInt32:
                    writer.Write((uint)value);
                    return true;
                case TypeCode.Int64:
                    writer.Write((long)value);
                    return true;
                case TypeCode.UInt64:
                    writer.Write((ulong)value);
                    return true;
                case TypeCode.Single:
                    writer.Write((float)value);
                    return true;
                case TypeCode.Double:
                    writer.Write((double)value);
                    return true;
                case TypeCode.Decimal:
                    writer.Write((decimal)value);
                    return true;
                case TypeCode.Char:
                    writer.Write((char)value);
                    return true;
                case TypeCode.String:
                    writer.Write((string)value);
                    return true;
                case TypeCode.DateTime:
                    writer.Write(((DateTime)value).ToBinary());
                    return true;
                default:
                    if (type == typeof(Guid))
                    {
                        writer.Write(((Guid)value).ToByteArray());
                        return true;
                    }
                    return false;
            }
        }

        /// <summary>Reads an exact scalar representation.</summary>
        /// <param name="reader">The source.</param>
        /// <param name="type">The scalar type.</param>
        /// <param name="value">The decoded value.</param>
        /// <returns>Whether the type was scalar.</returns>
        private static bool ReadScalar(BinaryReader reader, Type type, out object? value)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    value = reader.ReadBoolean();
                    break;
                case TypeCode.Byte:
                    value = reader.ReadByte();
                    break;
                case TypeCode.SByte:
                    value = reader.ReadSByte();
                    break;
                case TypeCode.Int16:
                    value = reader.ReadInt16();
                    break;
                case TypeCode.UInt16:
                    value = reader.ReadUInt16();
                    break;
                case TypeCode.Int32:
                    value = reader.ReadInt32();
                    break;
                case TypeCode.UInt32:
                    value = reader.ReadUInt32();
                    break;
                case TypeCode.Int64:
                    value = reader.ReadInt64();
                    break;
                case TypeCode.UInt64:
                    value = reader.ReadUInt64();
                    break;
                case TypeCode.Single:
                    value = reader.ReadSingle();
                    break;
                case TypeCode.Double:
                    value = reader.ReadDouble();
                    break;
                case TypeCode.Decimal:
                    value = reader.ReadDecimal();
                    break;
                case TypeCode.Char:
                    value = reader.ReadChar();
                    break;
                case TypeCode.String:
                    value = reader.ReadString();
                    break;
                case TypeCode.DateTime:
                    value = DateTime.FromBinary(reader.ReadInt64());
                    break;
                default:
                    value = type == typeof(Guid) ? new Guid(reader.ReadBytes(16)) : (object?)null;
                    return type == typeof(Guid);
            }
            return true;
        }

        /// <summary>Reads a bounded nonnegative collection size.</summary>
        /// <param name="reader">The source.</param>
        /// <returns>The validated size.</returns>
        /// <exception cref="InvalidDataException">The model is unsupported, damaged, incompatible or exceeds the checkpoint limits.</exception>
        private static int ReadCount(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count < 0 || count > MaxEntries)
            {
                throw new InvalidDataException("Invalid match collection size.");
            }
            return count;
        }

        /// <summary>Bounds aggregate allocations.</summary>
        /// <param name="count">The additional entries.</param>
        /// <exception cref="InvalidDataException">The model is unsupported, damaged, incompatible or exceeds the checkpoint limits.</exception>
        private void AddEntries(int count)
        {
            _entries += count;
            if (_entries > MaxEntries)
            {
                throw new InvalidDataException("Match state is too large.");
            }
        }

        /// <summary>Bounds nested model traversal.</summary>
        /// <param name="depth">The current nesting.</param>
        /// <exception cref="InvalidDataException">The model is unsupported, damaged, incompatible or exceeds the checkpoint limits.</exception>
        private static void CheckDepth(int depth)
        {
            if (depth > MaxDepth)
            {
                throw new InvalidDataException("Match state is too deeply nested.");
            }
        }

        /// <summary>Compares object identities without model equality overrides.</summary>
        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            /// <summary>The shared identity comparer.</summary>
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();
            /// <inheritdoc/>
            public new bool Equals(object? left, object? right)
            {
                return ReferenceEquals(left, right);
            }
            /// <inheritdoc/>
            public int GetHashCode(object value)
            {
                return RuntimeHelpers.GetHashCode(value);
            }
        }
    }
}
