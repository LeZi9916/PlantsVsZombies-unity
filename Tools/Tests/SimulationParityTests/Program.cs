#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PlantsVsZombies.Tests.SimulationParity
{
    /// <summary>Compares old and new compiled pure simulations with identical scripts and seeds.</summary>
    internal static class Program
    {
        /// <summary>Includes stored state independently of visibility.</summary>
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        /// <summary>Uses the baseline schema to compare fields migrated to properties.</summary>
        private static readonly Dictionary<string, string[]> s_schema = new Dictionary<string, string[]>();
        /// <summary>Runs deterministic traces and isolated tick measurements.</summary>
        /// <param name="args">Baseline assembly, current assembly, ignored report directory, and optional --traces-only.</param>
        /// <returns>Zero when every state trace matches.</returns>
        private static int Main(string[] args)
        {
            Directory.CreateDirectory(args[2]);
            var baselineContext = new AssemblyLoadContext("baseline", true);
            var currentContext = new AssemblyLoadContext("current", true);
            var baseline = baselineContext.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
            var current = currentContext.LoadFromAssemblyPath(Path.GetFullPath(args[1]));
            foreach (var type in baseline.GetTypes())
            {
                if (type.FullName?.StartsWith("PlantsVsZombies.", StringComparison.Ordinal) == true)
                {
                    s_schema[type.FullName] = type.GetFields(Flags).Where(field => !field.IsStatic && !field.Name.Contains("k__BackingField", StringComparison.Ordinal))
                        .Select(field => field.Name).Concat(type.GetProperties(Flags).Where(property => property.GetIndexParameters().Length == 0 && (!property.Name.StartsWith("Required", StringComparison.Ordinal) || type.GetProperty(property.Name.Substring("Required".Length), Flags) == null && type.GetField(property.Name.Substring("Required".Length), Flags) == null) && property.Name != "Game" && property.Name != "RunId").Select(property => property.Name))
                        .Distinct().OrderBy(name => name, StringComparer.Ordinal).ToArray();
                }
            }
            var failures = 0;
            var results = new List<object>();
            foreach (var level in new[] { 9, 19, 29, 39, 49 })
            {
                var oldTrace = Trace(baseline, level);
                var newTrace = Trace(current, level);
                var first = Enumerable.Range(0, oldTrace.Count).FirstOrDefault(index => oldTrace[index] != newTrace[index], -1);
                if (first >= 0)
                {
                    failures++;
                    File.WriteAllText(Path.Combine(args[2], "mismatch-" + level + ".json"), JsonSerializer.Serialize(new { tick = first, before = oldTrace[first], after = newTrace[first] }));
                }
                results.Add(new { level, ticks = oldTrace.Count, firstMismatch = first, digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", newTrace)))) });
                Console.WriteLine("Level " + level + ": " + (first < 0 ? "PASS" : "mismatch tick " + first));
            }
            var definitions = (IEnumerable)baseline.GetType("PlantsVsZombies.Modes.ModeCatalog", true)!.GetProperty("All")!.GetValue(null)!;
            foreach (var definition in definitions)
            {
                var id = (int)Read(definition, "Id")!;
                var oldTrace = TraceMode(baseline, id);
                var newTrace = TraceMode(current, id);
                var first = Enumerable.Range(0, oldTrace.Count).FirstOrDefault(index => oldTrace[index] != newTrace[index], -1);
                if (first >= 0)
                {
                    failures++;
                    File.WriteAllText(Path.Combine(args[2], "mode-mismatch-" + id + ".json"), JsonSerializer.Serialize(new { tick = first, before = oldTrace[first], after = newTrace[first] }));
                }
                results.Add(new { mode = id, ticks = oldTrace.Count, firstMismatch = first });
                Console.WriteLine("Mode " + id + ": " + (first < 0 ? "PASS" : "mismatch tick " + first));
            }
            var measurements = new List<object>();
            if (!args.Contains("--traces-only", StringComparer.Ordinal))
            {
                foreach (var count in new[] { 10, 100, 300 })
                {
                    measurements.Add(MeasurePair(baseline, current, count));
                }
            }
            File.WriteAllText(Path.Combine(args[2], "parity.json"), JsonSerializer.Serialize(new { failures, traces = results, measurements }, new JsonSerializerOptions { WriteIndented = true }));
            return failures == 0 ? 0 : 1;
        }
        /// <summary>Creates a native board fixture without consuming extra random draws.</summary>
        /// <param name="assembly">The implementation under test.</param>
        /// <param name="level">The native board level.</param>
        /// <param name="count">The number of actors, or zero for varied species.</param>
        /// <returns>The prepared match.</returns>
        private static object Board(Assembly assembly, int level, int count)
        {
            var type = assembly.GetType("PlantsVsZombies.Simulation.GameSimulation", true)!;
            var rules = Activator.CreateInstance(assembly.GetType("PlantsVsZombies.Simulation.GameRules", true)!)!;
            Set(rules, "AdventureLevel", level);
            var game = Activator.CreateInstance(type, new[] { rules, (object)12345 })!;
            var adventure = Read(game, "Adventure")!;
            Set(adventure, "Phase", Enum.Parse(assembly.GetType("PlantsVsZombies.Adventure.AdventurePhase", true)!, "Playing"));
            Set(adventure, "_nextSkySun", float.MaxValue);
            Call(game, "ResetAdventureWaveSchedule", 1000000, false, false);
            var rowCount = (int)Read(game, "RowCount")!;
            var plantKind = assembly.GetType("PlantsVsZombies.Simulation.PlantKind", true)!;
            var zombieKind = assembly.GetType("PlantsVsZombies.Simulation.ZombieKind", true)!;
            for (var row = 0; row < rowCount; row++)
            {
                for (var column = 0; column < (count >= 100 ? 9 : 6); column++)
                {
                    var kinds = new[] { 0, 1, 7, 10, 32, 3, 18, 34, 22 };
                    var plant = Call(game, "PlaceAdventurePlant", Enum.ToObject(plantKind, kinds[column]), row, column)!;
                    if (count > 0)
                    {
                        Set(plant, "Health", 100000f);
                    }
                }
            }
            var actors = count == 0 ? 25 : count;
            for (var i = 0; i < actors; i++)
            {
                var zombie = Call(game, "SpawnAdventureBoardZombie", Enum.ToObject(zombieKind, count == 0 ? i : 0), i % rowCount, 8f + i * .01f, 0)!;
                if (count > 0)
                {
                    Set(zombie, "Health", 100000f);
                    Set(zombie, "StartingHealth", 100000f);
                }
            }
            return game;
        }
        /// <summary>Records state after each scripted native update.</summary>
        /// <param name="assembly">The implementation under test.</param>
        /// <param name="level">The native board level.</param>
        /// <returns>The ordered state records.</returns>
        private static List<string> Trace(Assembly assembly, int level)
        {
            var game = Board(assembly, level, 0);
            return Record(game, "Update", 500);
        }
        /// <summary>Records a catalogued independent mode's native update path.</summary>
        /// <param name="assembly">The implementation under test.</param>
        /// <param name="id">The original mode identifier.</param>
        /// <returns>The ordered native state records.</returns>
        private static List<string> TraceMode(Assembly assembly, int id)
        {
            var type = assembly.GetType("PlantsVsZombies.Modes.ModeSession", true)!;
            var constructor = type.GetConstructors().Single();
            var arguments = constructor.GetParameters().Select(parameter => parameter.DefaultValue).ToArray();
            arguments[0] = id;
            var session = constructor.Invoke(arguments);
            Set(session, "Phase", Enum.Parse(assembly.GetType("PlantsVsZombies.Modes.ModePhase", true)!, "Playing"));
            var game = Read(session, "Game")!;
            Set(Read(game, "Adventure")!, "Phase", Enum.Parse(assembly.GetType("PlantsVsZombies.Adventure.AdventurePhase", true)!, "Playing"));
            return Record(game, "UpdateMode", 200);
        }
        /// <summary>Records observations and event order across pause and resume boundaries.</summary>
        /// <param name="game">The prepared match.</param>
        /// <param name="update">The mode's public update entry point.</param>
        /// <param name="count">The number of scripted frame updates.</param>
        /// <returns>The ordered observations.</returns>
        private static List<string> Record(object game, string update, int count)
        {
            var events = new List<object>();
            foreach (var name in new[] { "EventRaised", "WaveScheduleEventRaised" })
            {
                var field = game.GetType().GetField(name, Flags);
                var eventType = field != null ? field.FieldType : game.GetType().GetProperty(name, Flags)!.PropertyType;
                var capture = typeof(Program).GetMethod(nameof(CaptureEvent), Flags)!.MakeGenericMethod(eventType.GenericTypeArguments[0]).Invoke(null, new object[] { events })!;
                Set(game, name, capture);
            }
            var deltas = new[] { .003f, .017f, .035f, .005f, 0f, .01f };
            var records = new List<string>();
            for (var tick = 0; tick < count; tick++)
            {
                events.Clear();
                if (tick == 40 || tick == 60)
                {
                    Call(game, "TogglePause");
                }
                Call(game, update, deltas[tick % deltas.Length]);
                var text = new StringBuilder();
                foreach (var name in new[] { "State", "Elapsed", "SunAmount", "SpawnedZombies", "Plants", "Zombies", "Peas", "Suns", "Mowers", "Flames", "WaveSchedule", "Presentation", "Adventure", "ModeSession", "_seedCooldownTicks", "_accumulatedTime", "_modeAccumulated", "_completedBoardAccumulated", "_scheduledWaveRemainder", "_idlePoolRemainder" })
                {
                    text.Append(name).Append('=');
                    Append(text, Read(game, name), 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
                }
                Append(text, Call(game, "CaptureBattleRandomState"), 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
                Append(text, events, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
                records.Add(text.ToString());
            }
            return records;
        }
        /// <summary>Creates a callback of the implementation's exact event type.</summary>
        /// <typeparam name="T">The event type loaded in the isolated assembly context.</typeparam>
        /// <param name="events">The ordered event observations.</param>
        /// <returns>A callback that records each event once.</returns>
        private static Action<T> CaptureEvent<T>(List<object> events)
        {
            return value => events.Add(value!);
        }
        /// <summary>Alternates old and new measurements to reduce run-order bias.</summary>
        /// <param name="baseline">The original implementation.</param>
        /// <param name="current">The refactored implementation.</param>
        /// <param name="count">The initial actor count.</param>
        /// <returns>Five paired per-tick measurements with medians across repetitions.</returns>
        private static object MeasurePair(Assembly baseline, Assembly current, int count)
        {
            var oldRuns = new TickMeasurement[5];
            var newRuns = new TickMeasurement[5];
            for (var repeat = 0; repeat < 5; repeat++)
            {
                if (repeat % 2 == 0)
                {
                    oldRuns[repeat] = Measure(baseline, count);
                    newRuns[repeat] = Measure(current, count);
                }
                else
                {
                    newRuns[repeat] = Measure(current, count);
                    oldRuns[repeat] = Measure(baseline, count);
                }
            }
            return new
            {
                initialZombies = count,
                plants = count >= 100 ? 45 : 30,
                before = Summarize(oldRuns),
                after = Summarize(newRuns),
                beforeRuns = oldRuns,
                afterRuns = newRuns
            };
        }
        /// <summary>Summarizes repeated individual-tick distributions.</summary>
        /// <param name="runs">The paired fixture measurements.</param>
        /// <returns>The median of run medians and run P95 values, plus total allocations.</returns>
        private static object Summarize(TickMeasurement[] runs)
        {
            return new
            {
                medianMilliseconds = runs.Select(run => run.MedianMilliseconds).OrderBy(value => value).ElementAt(runs.Length / 2),
                p95Milliseconds = runs.Select(run => run.P95Milliseconds).OrderBy(value => value).ElementAt(runs.Length / 2),
                allocatedBytes = runs.Sum(run => run.AllocatedBytes)
            };
        }
        /// <summary>Measures individual fixed updates independently of reflection and serialization.</summary>
        /// <param name="assembly">The implementation under test.</param>
        /// <param name="count">The initial actor count.</param>
        /// <returns>The median, P95 and allocated bytes of 1000 warmed updates.</returns>
        private static TickMeasurement Measure(Assembly assembly, int count)
        {
            var game = Board(assembly, 9, count);
            var update = (Action<float>)game.GetType().GetMethod("Update")!.CreateDelegate(typeof(Action<float>), game);
            for (var i = 0; i < 200; i++)
            {
                update(.01f);
            }
            var times = new double[1000];
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < times.Length; i++)
            {
                var start = Stopwatch.GetTimestamp();
                update(.01f);
                times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
            Array.Sort(times);
            return new TickMeasurement(times[times.Length / 2], times[(int)(times.Length * .95)], allocated);
        }
        /// <summary>Reads a logical member across field-to-property migrations.</summary>
        /// <param name="value">The declaring object.</param>
        /// <param name="name">The logical member name.</param>
        /// <returns>The member value.</returns>
        private static object? Read(object value, string name)
        {
            var type = value.GetType();
            if (type.Name == "GameSimulation" && type.GetField(name, Flags) == null && type.GetProperty(name, Flags) == null)
            {
                value = MemberOwner(value, name);
                type = value.GetType();
            }
            var field = type.GetField(name, Flags);
            return field != null ? field.GetValue(value) : type.GetProperty(name, Flags)!.GetValue(value);
        }
        /// <summary>Sets a deliberate fixture value.</summary>
        /// <param name="target">The fixture object.</param>
        /// <param name="name">The member name.</param>
        /// <param name="value">The fixture value.</param>
        private static void Set(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, Flags);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                target.GetType().GetProperty(name, Flags)!.SetValue(target, value);
            }
        }
        /// <summary>Invokes the same operation on either ownership layout.</summary>
        /// <param name="game">The fixture match.</param>
        /// <param name="name">The original operation name.</param>
        /// <param name="arguments">The explicit arguments.</param>
        /// <returns>The operation result.</returns>
        private static object? Call(object game, string name, params object[] arguments)
        {
            var owner = game;
            var method = owner.GetType().GetMethods(Flags).FirstOrDefault(candidate => candidate.Name == name && candidate.GetParameters().Length >= arguments.Length);
            if (method == null)
            {
                owner = MemberOwner(game, name);
                method = owner.GetType().GetMethods(Flags).First(candidate => candidate.Name == name && candidate.GetParameters().Length >= arguments.Length);
            }
            var parameters = method.GetParameters();
            var supplied = arguments.Length;
            Array.Resize(ref arguments, parameters.Length);
            for (var i = supplied; i < arguments.Length; i++)
            {
                arguments[i] = parameters[i].DefaultValue!;
            }
            return method.Invoke(owner, arguments);
        }
        /// <summary>Resolves fixture ownership through the original instance API or the current test-only extension.</summary>
        /// <param name="game">The match loaded in its independent assembly context.</param>
        /// <param name="name">The member whose owning collaborator is required.</param>
        /// <returns>The exact collaborator used by that revision's fixture inspection.</returns>
        /// <exception cref="MissingMethodException">Neither revision exposes a supported inspection entry point.</exception>
        private static object MemberOwner(object game, string name)
        {
            var type = game.GetType();
            var legacy = type.GetMethod("MemberOwner", Flags);
            if (legacy != null)
            {
                return legacy.Invoke(game, new object[] { name })!;
            }
            var inspection = type.Assembly.GetType("PlantsVsZombies.Simulation.SimulationInspection");
            var extension = inspection?.GetMethod("MemberOwner", Flags);
            if (extension == null)
            {
                throw new MissingMethodException(type.FullName, "MemberOwner / SimulationInspection.MemberOwner");
            }
            return extension.Invoke(null, new object[] { game, name })!;
        }
        /// <summary>Serializes baseline-defined state with exact round-trip numeric values.</summary>
        /// <param name="text">The output buffer.</param>
        /// <param name="value">The state value.</param>
        /// <param name="depth">The current traversal depth.</param>
        /// <param name="visited">The current reference path.</param>
        private static void Append(StringBuilder text, object? value, int depth, HashSet<object> visited)
        {
            if (value == null)
            {
                text.Append("null;");
                return;
            }
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is decimal)
            {
                text.Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append(';');
                return;
            }
            if (!type.IsValueType && !visited.Add(value))
            {
                var id = type.GetField("Id", Flags)?.GetValue(value) ?? type.GetProperty("Id", Flags)?.GetValue(value);
                text.Append("reference:").Append(type.FullName).Append(':').Append(id).Append(';');
                return;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                Append(text, Read(value, "Key"), depth + 1, visited);
                Append(text, Read(value, "Value"), depth + 1, visited);
            }
            else if (value is IEnumerable sequence)
            {
                text.Append('[');
                foreach (var item in sequence)
                {
                    Append(text, item, depth + 1, visited);
                }
                text.Append(']');
            }
            else if (type.FullName != null && s_schema.TryGetValue(type.FullName, out var names))
            {
                text.Append(type.FullName).Append('{');
                foreach (var name in names)
                {
                    text.Append(name).Append(':');
                    Append(text, Read(value, name), depth + 1, visited);
                }
                text.Append('}');
            }
            else if (value is Delegate callback)
            {
                text.Append("callback:").Append(callback.Method.Name).Append(';');
            }
            else if (type.IsValueType)
            {
                text.Append(type.FullName).Append('{');
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance).OrderBy(field => field.Name, StringComparer.Ordinal))
                {
                    text.Append(field.Name).Append(':');
                    Append(text, field.GetValue(value), depth + 1, visited);
                }
                text.Append('}');
            }
            else
            {
                throw new InvalidOperationException("The parity schema does not cover state type " + type.FullName);
            }
            visited.Remove(value);
        }
    }
}
