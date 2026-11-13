#nullable enable
namespace PlantsVsZombies.Rendering
{
    /// <summary>Describes the source workload, rasterized geometry, and causes of batch fragmentation.</summary>
    internal readonly struct SpriteBatchStatistics
    {
        /// <summary>Gets the number of visible logical source commands.</summary>
        internal int SubmittedSpriteCount { get; }
        /// <summary>Gets the number of actual combined GPU mesh draws.</summary>
        internal int BatchCount { get; }
        /// <summary>Gets the total clipped vertices uploaded by this pool.</summary>
        internal int VertexCount { get; }
        /// <summary>Gets the total triangles uploaded by this pool.</summary>
        internal int TriangleCount { get; }
        /// <summary>Gets the commands omitted for alpha, hierarchy visibility, empty source regions, or clipping.</summary>
        internal int CulledSpriteCount { get; }
        /// <summary>Gets the sum of clipped polygon areas, counting overlapping layers separately.</summary>
        internal double CoveredPixelArea { get; }
        /// <summary>Gets splits whose texture or hardware blend binding changed.</summary>
        internal int MaterialSplitCount { get; }
        /// <summary>Gets splits that preserve an intervening Canvas, mesh, or another sprite pool.</summary>
        internal int PainterOrderSplitCount { get; }
        /// <summary>Gets splits required by the portable 16-bit vertex-index limit.</summary>
        internal int CapacitySplitCount { get; }

        /// <summary>Creates a per-pool snapshot without allocating a diagnostic object.</summary>
        /// <param name="submittedSpriteCount">The visible logical source commands.</param>
        /// <param name="batchCount">The combined GPU mesh draws.</param>
        /// <param name="vertexCount">The uploaded clipped vertices.</param>
        /// <param name="triangleCount">The uploaded triangles.</param>
        /// <param name="culledSpriteCount">The omitted logical commands.</param>
        /// <param name="coveredPixelArea">The sum of clipped polygon areas in destination pixels.</param>
        /// <param name="materialSplitCount">Splits involving a texture or blend binding change.</param>
        /// <param name="painterOrderSplitCount">Splits preserving an intervening renderer.</param>
        /// <param name="capacitySplitCount">Splits preventing index overflow.</param>
        internal SpriteBatchStatistics(int submittedSpriteCount, int batchCount, int vertexCount, int triangleCount,
            int culledSpriteCount, double coveredPixelArea, int materialSplitCount, int painterOrderSplitCount, int capacitySplitCount)
        {
            SubmittedSpriteCount = submittedSpriteCount;
            BatchCount = batchCount;
            VertexCount = vertexCount;
            TriangleCount = triangleCount;
            CulledSpriteCount = culledSpriteCount;
            CoveredPixelArea = coveredPixelArea;
            MaterialSplitCount = materialSplitCount;
            PainterOrderSplitCount = painterOrderSplitCount;
            CapacitySplitCount = capacitySplitCount;
        }
    }
}
