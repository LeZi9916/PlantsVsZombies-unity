#nullable enable
using System;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Provides optional allocation-free CPU markers for the ordered sprite submission path.</summary>
    internal static class SpriteRenderingProfiler
    {
        /// <summary>Measures source-region preparation and cached atlas mapping.</summary>
        internal static readonly ProfilerMarker Regions = new ProfilerMarker(ProfilerCategory.Scripts, "PvZ.Atlas.Regions");
        /// <summary>Measures logical source submission, clipping, and combined mesh assembly.</summary>
        internal static readonly ProfilerMarker Submit = new ProfilerMarker(ProfilerCategory.Scripts, "PvZ.Sprite.Submit");
        /// <summary>Measures the completed mesh uploads.</summary>
        internal static readonly ProfilerMarker Upload = new ProfilerMarker(ProfilerCategory.Scripts, "PvZ.Sprite.Upload");
        /// <summary>Measures hierarchy visibility refresh and actor retirement.</summary>
        internal static readonly ProfilerMarker Hierarchy = new ProfilerMarker(ProfilerCategory.Scripts, "PvZ.Hierarchy.Refresh");
        /// <summary>Measures animation sampling and its nested source submissions.</summary>
        internal static readonly ProfilerMarker Animation = new ProfilerMarker(ProfilerCategory.Scripts, "PvZ.Animation.Sample");

        /// <summary>Forces marker collection for an explicit diagnostic recorder.</summary>
        internal static bool ForceEnabled { get; set; }
        /// <summary>Gets whether marker sampling is enabled for this complete presentation.</summary>
        private static bool Enabled { get; set; }

        /// <summary>Updates profiler state once per complete frame instead of entering native markers for every inactive sample.</summary>
        internal static void BeginFrame()
        {
            Enabled = ForceEnabled || UnityEngine.Profiling.Profiler.enabled;
        }

        /// <summary>Creates a stack-only marker scope with a managed fast path when profiling is disabled.</summary>
        /// <param name="marker">The CPU region to observe.</param>
        /// <returns>A scope that ends only a sample that was actually started.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Scope Measure(ProfilerMarker marker)
        {
            return new Scope(marker, Enabled);
        }

        /// <summary>Skips both native marker calls when no profiler is observing the presentation.</summary>
        internal readonly struct Scope : IDisposable
        {
            /// <summary>The marker to finish when this scope was sampled.</summary>
            private readonly ProfilerMarker _marker;
            /// <summary>Whether construction actually began a native sample.</summary>
            private readonly bool _enabled;

            /// <summary>Begins an optional CPU sample without allocating.</summary>
            /// <param name="marker">The CPU marker to observe.</param>
            /// <param name="enabled">Whether a profiler requested this sample.</param>
            internal Scope(ProfilerMarker marker, bool enabled)
            {
                _marker = marker;
                _enabled = enabled;
                if (enabled)
                {
                    _marker.Begin();
                }
            }

            /// <summary>Ends the native sample only when collection was enabled at construction.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                if (_enabled)
                {
                    _marker.End();
                }
            }
        }

        /// <summary>Registers all markers before an optional recorder starts observing them.</summary>
        internal static void Initialize()
        {
            Submit.Begin();
            Submit.End();
        }
    }
}
