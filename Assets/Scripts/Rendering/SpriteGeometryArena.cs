#nullable enable
using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Owns bounded reusable storage shared by all renderers in the active presentation session.</summary>
    internal sealed class SpriteGeometryArena : IDisposable
    {
        /// <summary>The maximum region count, never splitting one sprite between segments.</summary>
        internal const int Capacity = 1024;
        /// <summary>The numeric source snapshots.</summary>
        internal NativeArray<SpriteRegionInput> Inputs = new NativeArray<SpriteRegionInput>(Capacity, Allocator.Persistent);
        /// <summary>The flat polygon output.</summary>
        internal NativeArray<SpriteClipVertex> Vertices = new NativeArray<SpriteClipVertex>(Capacity * 12, Allocator.Persistent);
        /// <summary>The ordered result descriptors.</summary>
        internal NativeArray<SpriteRegionResult> Results = new NativeArray<SpriteRegionResult>(Capacity, Allocator.Persistent);
        /// <summary>The first clipping scratch buffer, independent of all inputs and outputs.</summary>
        private NativeArray<SpriteClipVertex> _first = new NativeArray<SpriteClipVertex>(12, Allocator.Persistent);
        /// <summary>The second clipping scratch buffer, independent of all inputs and outputs.</summary>
        private NativeArray<SpriteClipVertex> _second = new NativeArray<SpriteClipVertex>(12, Allocator.Persistent);
        /// <summary>The managed command snapshots, cleared after each segment.</summary>
        internal readonly DeferredSpriteCommand[] Commands = new DeferredSpriteCommand[Capacity];
        /// <summary>The sampled textures, copied before provider caches may be reused.</summary>
        internal readonly Texture?[] Textures = new Texture?[Capacity];
        /// <summary>The submission-time opacity classification per region.</summary>
        internal readonly bool[] Opaque = new bool[Capacity];
        /// <summary>The renderer currently owning the populated prefix.</summary>
        internal ObjectSpriteRenderer? Owner;
        /// <summary>The number of populated region inputs.</summary>
        internal int RegionCount;
        /// <summary>The number of populated logical commands.</summary>
        internal int CommandCount;
        /// <summary>Whether native buffers have already been released.</summary>
        private bool _disposed;

        /// <summary>Executes the entire numeric segment through the selected synchronous path.</summary>
        /// <param name="execution">The managed or Burst executor to use.</param>
        internal void Execute(SpriteGeometryExecution execution)
        {
            var job = new SpriteGeometryJob
            {
                Inputs = Inputs, Vertices = Vertices, Results = Results, ScratchInput = _first,
                ScratchOutput = _second, Count = RegionCount
            };
            if (execution == SpriteGeometryExecution.Managed)
            {
                job.Execute();
            }
            else
            {
                job.Run();
            }
        }

        /// <summary>Drops every managed reference after geometry has been consumed or a flush fails.</summary>
        internal void Clear()
        {
            Array.Clear(Commands, 0, CommandCount);
            Array.Clear(Textures, 0, RegionCount);
            Owner = null;
            RegionCount = 0;
            CommandCount = 0;
        }

        /// <summary>Releases all native storage once the final renderer in the session is disposed.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            Clear();
            Inputs.Dispose();
            Vertices.Dispose();
            Results.Dispose();
            _first.Dispose();
            _second.Dispose();
            _disposed = true;
        }
    }
}
