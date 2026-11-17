#nullable enable
#if PVZ_PERFORMANCE_REPLAY
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace PlantsVsZombies.Diagnostics
{
    /// <summary>Measures complete production Update and Present scopes independently of engine and replay boundary allocations.</summary>
    internal static class PerformanceReplayAllocations
    {
        /// <summary>The bounded event samples retained by each synchronous scope.</summary>
        private const int Capacity = 4096;
        /// <summary>The update allocation recorder, initialized before replay warm-up.</summary>
        private static ProfilerRecorder s_update;
        /// <summary>The presentation allocation recorder, initialized before replay warm-up.</summary>
        private static ProfilerRecorder s_present;
        /// <summary>The current and preceding Unity frame, without a per-frame report object.</summary>
        private static readonly Frame[] s_frames = new Frame[2];
        /// <summary>Whether recorder initialization and its deliberate allocation control succeeded.</summary>
        private static bool s_ready;
        /// <summary>Whether the explicitly requested short diagnostic records nested attribution scopes.</summary>
        private static bool s_detailsEnabled;
        /// <summary>Gets whether any measured scope exceeded the bounded allocation-event recorder.</summary>
        internal static bool Overflowed { get; private set; }

        /// <summary>Accumulates short diagnostic attribution without changing production code in ordinary builds.</summary>
        private static readonly Dictionary<string, long> s_details = new Dictionary<string, long>(32);

        /// <summary>Resets attributed bytes after warm-up without discarding prepared category keys.</summary>
        internal static void ResetDetails()
        {
            foreach (var key in s_details.Keys.ToArray())
            {
                s_details[key] = 0;
            }
        }

        /// <summary>Formats nested attribution outside measurement; values must not be summed as independent work.</summary>
        /// <returns>The attributed category totals.</returns>
        internal static string DescribeDetails()
        {
            return string.Join(";", s_details.Select(pair => pair.Key + "=" + pair.Value));
        }

        /// <summary>Attributes one diagnostic subpath while preserving the full Update and Present allocation totals.</summary>
        internal readonly struct DetailScope : IDisposable
        {
            /// <summary>The static diagnostic category name.</summary>
            private readonly string _name;
            /// <summary>The thread allocation count at entry.</summary>
            private readonly long _before;
            /// <summary>Whether the diagnostic recorder was initialized.</summary>
            private readonly bool _enabled;
            /// <summary>Starts one nested allocation attribution scope.</summary>
            /// <param name="name">The retained static category name.</param>
            internal DetailScope(string name)
            {
                _name = name;
                _enabled = s_ready && s_detailsEnabled;
                if (_enabled && !s_details.ContainsKey(name))
                {
                    s_details.Add(name, 0);
                }
                _before = _enabled ? GC.GetAllocatedBytesForCurrentThread() : 0;
            }
            /// <summary>Accumulates attributed bytes without allocating a category record.</summary>
            public void Dispose()
            {
                if (_enabled)
                {
                    s_details[_name] += GC.GetAllocatedBytesForCurrentThread() - _before;
                }
            }
        }

        /// <summary>Creates scoped recorders and verifies both with an intentionally allocated array.</summary>
        /// <exception cref="InvalidOperationException">A required recorder or its allocation positive control is unavailable.</exception>
        /// <param name="details">Whether nested allocation attribution is requested for a separate diagnostic run.</param>
        internal static void Initialize(bool details)
        {
            s_detailsEnabled = details;
            s_update = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", Capacity, ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            s_present = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", Capacity, ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
            if (!s_update.Valid || !s_present.Valid)
            {
                throw new InvalidOperationException("Production allocation scope recorders are unavailable.");
            }
            s_update.Start();
            s_present.Start();
            var control = new byte[32768];
            s_update.Stop();
            s_present.Stop();
            GC.KeepAlive(control);
            if (s_update.Count == 0 || s_present.Count == 0)
            {
                throw new InvalidOperationException("Production allocation scope recorders failed their positive control.");
            }
            s_ready = true;
            Overflowed = false;
        }

        /// <summary>Copies allocation evidence from the preceding completed Unity frame.</summary>
        /// <param name="sample">The current replay record receiving completed-frame scope values.</param>
        internal static void Read(ref PerformanceReplaySample sample)
        {
            var frame = Time.frameCount - 1;
            ref var slot = ref s_frames[frame & 1];
            var available = slot.Number == frame && slot.HasUpdate && slot.HasPresent;
            sample.UpdateAllocationBytes = available ? slot.UpdateBytes : -1;
            sample.PresentAllocationBytes = available ? slot.PresentBytes : -1;
            sample.UpdateAllocationEvents = available ? slot.UpdateEvents : -1;
            sample.PresentAllocationEvents = available ? slot.PresentEvents : -1;
        }

        /// <summary>Releases diagnostic recorders after the replay host ends.</summary>
        internal static void Dispose()
        {
            s_ready = false;
            s_update.Dispose();
            s_present.Dispose();
        }

        /// <summary>Stores complete production scope totals for one Unity frame.</summary>
        private struct Frame
        {
            /// <summary>The Unity frame represented by this slot.</summary>
            internal int Number;
            /// <summary>The complete Update allocation byte count.</summary>
            internal long UpdateBytes;
            /// <summary>The complete Present allocation byte count.</summary>
            internal long PresentBytes;
            /// <summary>The complete Update allocation event count.</summary>
            internal int UpdateEvents;
            /// <summary>The complete Present allocation event count.</summary>
            internal int PresentEvents;
            /// <summary>Whether production Update visited this frame.</summary>
            internal bool HasUpdate;
            /// <summary>Whether production Present visited this frame.</summary>
            internal bool HasPresent;
        }

        /// <summary>Records one synchronous production scope, including early returns and exceptional exits.</summary>
        internal readonly struct Scope : IDisposable
        {
            /// <summary>Selects presentation or update ownership.</summary>
            private readonly bool _presentation;
            /// <summary>Whether scoped recording was initialized before this call.</summary>
            private readonly bool _enabled;
            /// <summary>The Unity frame whose completed evidence receives these totals.</summary>
            private readonly int _frame;
            /// <summary>The main-thread allocated bytes before production work begins.</summary>
            private readonly long _before;

            /// <summary>Starts allocation recording for the complete production call.</summary>
            /// <param name="presentation">Whether this is Present rather than Update.</param>
            internal Scope(bool presentation)
            {
                _presentation = presentation;
                _enabled = s_ready;
                _frame = Time.frameCount;
                _before = 0;
                if (!_enabled)
                {
                    return;
                }
                ref var recorder = ref (_presentation ? ref s_present : ref s_update);
                recorder.Reset();
                recorder.Start();
                _before = GC.GetAllocatedBytesForCurrentThread();
            }

            /// <summary>Stores scope totals without constructing a new frame record.</summary>
            public void Dispose()
            {
                if (!_enabled)
                {
                    return;
                }
                var bytes = GC.GetAllocatedBytesForCurrentThread() - _before;
                ref var recorder = ref (_presentation ? ref s_present : ref s_update);
                recorder.Stop();
                var events = recorder.Count;
                Overflowed |= events >= Capacity;
                ref var slot = ref s_frames[_frame & 1];
                if (slot.Number != _frame)
                {
                    slot = new Frame { Number = _frame };
                }
                if (_presentation)
                {
                    slot.PresentBytes += bytes;
                    slot.PresentEvents += events;
                    slot.HasPresent = true;
                }
                else
                {
                    slot.UpdateBytes += bytes;
                    slot.UpdateEvents += events;
                    slot.HasUpdate = true;
                }
            }
        }
    }
}
#endif
