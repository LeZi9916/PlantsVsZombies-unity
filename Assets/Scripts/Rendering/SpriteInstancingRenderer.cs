#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Retains ordered polygon instances without copying atlas pages or changing transparency order.</summary>
    internal sealed class SpriteInstancingRenderer : IDisposable
    {
        /// <summary>The bounded number of polygons in one independently owned GPU chunk.</summary>
        internal const int InstancesPerChunk = 8192;
        /// <summary>The maximum clipped polygon size produced by the shared geometry implementation.</summary>
        internal const int VerticesPerInstance = 12;
        /// <summary>The sixteen-MiB GPU budget, excluding the tiny shared template.</summary>
        internal const int MaximumChunks = 8;
        /// <summary>The number of existing textures that can be sampled without another draw.</summary>
        private const int TextureSlots = 8;
        /// <summary>The bounded retained native binding snapshots shared by all scene spans.</summary>
        private const int MaximumSubmissionBindings = 8192;
        /// <summary>The fixed number of complete retired sessions retained until their native draw frame expires.</summary>
        private const int MaximumRetiredSessions = 8;
        /// <summary>The bounded resource owners whose native submissions cannot yet be released.</summary>
        private static readonly SpriteInstancingRenderer?[] s_retiredSessions = new SpriteInstancingRenderer?[MaximumRetiredSessions];
        /// <summary>The active prefix in the fixed retired-session array.</summary>
        private static int s_retiredSessionCount;
        /// <summary>Whether the single shared rendering and shutdown hooks have been installed.</summary>
        private static bool s_retirementHooksInstalled;
        /// <summary>Gets total sessions that safely declined instancing because all deferred-release slots were occupied.</summary>
        internal static long RetirementCapacityFallbackCount { get; private set; }
        /// <summary>Gets complete resource owners retained until their last submitted Unity frame expires.</summary>
        internal static int PendingRetiredSessionCount => s_retiredSessionCount;
        /// <summary>The structured vertex buffer binding.</summary>
        private static readonly int s_verticesId = Shader.PropertyToID("_PvZVertices");
        /// <summary>The structured instance buffer binding.</summary>
        private static readonly int s_instancesId = Shader.PropertyToID("_PvZInstances");
        /// <summary>The first instance selected by one draw.</summary>
        private static readonly int s_startId = Shader.PropertyToID("_PvZInstanceStart");
        /// <summary>The generation captured by one submitted draw.</summary>
        private static readonly int s_runGenerationId = Shader.PropertyToID("_PvZRunGeneration");
        /// <summary>The latest complete scene generation, invalidating earlier same-frame submissions.</summary>
        private static readonly int s_generationId = Shader.PropertyToID("_PvZSceneGeneration");
        /// <summary>The existing texture bindings in each ordered bank.</summary>
        private static readonly int[] s_textureIds = CreateTextureIds();
        /// <summary>The original premultiplied and additive hardware blend materials.</summary>
        private readonly Material[] _materials;
        /// <summary>The fixed fan whose unused triangles degenerate in the vertex shader.</summary>
        private readonly Mesh _template;
        /// <summary>The CPU and GPU chunks retained throughout this rendering session.</summary>
        private readonly List<Chunk> _chunks = new List<Chunk>(MaximumChunks);
        /// <summary>The retained ordered draw descriptors.</summary>
        private readonly List<Run> _runs = new List<Run>(64);
        /// <summary>The number of active draw descriptors.</summary>
        private int _runCount;
        /// <summary>The number of draws registered for this generation.</summary>
        private int _submittedRuns;
        /// <summary>The Unity frame whose retained draws have already been registered.</summary>
        private int _submissionFrame = -1;
        /// <summary>The Unity frame whose completed native snapshots were last released for reuse.</summary>
        private int _bindingsCleanupFrame = -1;
        /// <summary>The Unity frame whose submitted draw still owns the current GPU buffer contents.</summary>
        private int _bufferSubmissionFrame = -1;
        /// <summary>The scene generation submitted with the current GPU buffer contents.</summary>
        private int _bufferSubmissionGeneration;
        /// <summary>The active append chunk.</summary>
        private int _chunkIndex;
        /// <summary>The next frame's bounded capacity request.</summary>
        private int _requestedChunks = 1;
        /// <summary>The contiguous run prefix whose first native snapshot and material were prepared before presentation.</summary>
        private int _preparedSubmissionRuns;
        /// <summary>The current complete scene generation.</summary>
        private int _generation;
        /// <summary>Whether a resource failure permanently selected the safe mesh path for this session.</summary>
        private bool _failed;
        /// <summary>Whether bounded ordering selected complete mesh rendering for the current scene.</summary>
        private bool _suppressed;
        /// <summary>Whether this renderer has retired and cannot be disposed twice.</summary>
        private bool _disposed;
        /// <summary>The Unity frame whose outstanding native registrations caused resource retirement.</summary>
        private int _retirementFrame = -1;
        /// <summary>Gets the GPU bytes retained by the fixed-capacity chunks.</summary>
        internal long RetainedGpuBytes => (long)_chunks.Count * InstancesPerChunk * (VerticesPerInstance * 16 + 64);
        /// <summary>Gets the number of active draw runs.</summary>
        internal int RunCount => _suppressed ? 0 : _runCount;
        /// <summary>Gets the number of changed GPU prefixes uploaded in this scene frame.</summary>
        internal int UploadCount { get; private set; }
        /// <summary>Gets the number of uploaded bytes in this scene frame.</summary>
        internal long UploadBytes { get; private set; }
        /// <summary>Gets all actual structured-buffer upload bytes since this renderer was created.</summary>
        internal long TotalUploadBytes { get; private set; }
        /// <summary>Gets all successful structured-buffer creations, including buffers cleaned up after a later allocation failure.</summary>
        internal long BufferCreationCount { get; private set; }
        /// <summary>Gets boundaries caused by exhausting an ordered eight-texture bank; concurrent reasons are counted independently.</summary>
        internal long TextureBankSplitCount { get; private set; }
        /// <summary>Gets boundaries caused by a hardware blend change; concurrent reasons are counted independently.</summary>
        internal long BlendSplitCount { get; private set; }
        /// <summary>Gets boundaries preserving another pool or intervening painter order; concurrent reasons are counted independently.</summary>
        internal long PainterOrderSplitCount { get; private set; }
        /// <summary>Gets boundaries crossing fixed-capacity chunks; concurrent reasons are counted independently.</summary>
        internal long CapacitySplitCount { get; private set; }
        /// <summary>Gets the number of scene polygons routed through the safe mesh path.</summary>
        internal int FallbackCount { get; private set; }
        /// <summary>Gets every polygon rejected by the instance path since this renderer was created.</summary>
        internal long TotalFallbackCount { get; private set; }
        /// <summary>Gets complete retained scene generations retired because one of their source pools was disposed.</summary>
        internal long OwnerRetirementCount { get; private set; }
        /// <summary>Gets complete retained scene generations retired because a sampled texture was destroyed.</summary>
        internal long MissingTextureRetirementCount { get; private set; }
        /// <summary>Gets all draw spans cleared by explicit source or resource retirement.</summary>
        internal long RetiredRunCount { get; private set; }
        /// <summary>Gets actual retained property-block and material snapshot pairs created for immutable native submissions.</summary>
        internal long SubmissionBindingCreationCount { get; private set; }
        /// <summary>Gets actual retained run descriptors created for scene painter spans.</summary>
        internal long RunCreationCount { get; private set; }
        /// <summary>Gets actual independently owned submission materials created for this rendering session.</summary>
        internal long SubmissionMaterialCreationCount { get; private set; }
        /// <summary>Gets run descriptors created during presentation after the explicit preparation budget was exceeded.</summary>
        internal long RunCapacityMissCount { get; private set; }
        /// <summary>Gets native snapshots created during presentation after the prepared reusable snapshot budget was exceeded.</summary>
        internal long SubmissionBindingCapacityMissCount { get; private set; }
        /// <summary>Gets retained run descriptors available without constructing another descriptor during presentation.</summary>
        internal int PreparedRunCapacity => _runs.Count;
        /// <summary>Gets the contiguous run prefix with a fully prepared native snapshot and material.</summary>
        internal int PreparedSubmissionBindingCapacity => _preparedSubmissionRuns;
        /// <summary>Gets complete scene generations routed to meshes to avoid overwriting GPU data already submitted in the same Unity frame.</summary>
        internal long SameFrameBufferFallbackCount { get; private set; }
        /// <summary>Gets the number of polygon slots already prepared without recording draw commands.</summary>
        internal int PreparedInstanceCapacity => _chunks.Count * InstancesPerChunk;

        /// <summary>Checks vertex-buffer instancing support without assuming a desktop graphics backend.</summary>
        /// <returns>Whether the supported shader and two structured vertex inputs are available.</returns>
        internal static bool IsSupported()
        {
            ReleaseRetiredSessions(false);
            if (s_retiredSessionCount == MaximumRetiredSessions)
            {
                RetirementCapacityFallbackCount++;
                return false;
            }
            return SystemInfo.supportsInstancing && SystemInfo.graphicsShaderLevel >= 45
                && SystemInfo.maxComputeBufferInputsVertex >= 2;
        }

        /// <summary>Creates the shared template and two hardware blend variants.</summary>
        /// <param name="shader">The supported shader implementing the fixed instance layout.</param>
        internal SpriteInstancingRenderer(Shader shader)
        {
            _materials = new Material[2];
            for (var blend = 0; blend < _materials.Length; blend++)
            {
                var material = new Material(shader)
                {
                    name = "PvZ ordered instances " + blend,
                    hideFlags = HideFlags.HideAndDontSave,
                    enableInstancing = true
                };
                material.SetInt("_SrcBlend", (int)(blend == 1 ? BlendMode.SrcAlpha : BlendMode.One));
                material.SetInt("_DstBlend", (int)(blend == 1 ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                material.SetInt("_SrcBlendAlpha", (int)(blend == 1 ? BlendMode.Zero : BlendMode.One));
                material.SetInt("_DstBlendAlpha", (int)(blend == 1 ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                material.SetInt("_ZWrite", 0);
                _materials[blend] = material;
            }
            _template = new Mesh { name = "PvZ twelve-vertex instance fan", hideFlags = HideFlags.HideAndDontSave };
            var indices = new int[(VerticesPerInstance - 2) * 3];
            for (var triangle = 0; triangle < VerticesPerInstance - 2; triangle++)
            {
                indices[triangle * 3] = 0;
                indices[triangle * 3 + 1] = triangle + 1;
                indices[triangle * 3 + 2] = triangle + 2;
            }
            _template.vertices = new Vector3[VerticesPerInstance];
            _template.triangles = indices;
            _template.bounds = new Bounds(Vector3.zero, Vector3.one);
        }

        /// <summary>Starts staging the latest complete presentation and warms at most one requested chunk.</summary>
        /// <param name="generation">The scene generation shared with the mesh and Canvas painter order.</param>
        internal void BeginFrame(int generation)
        {
            ReleaseCompletedSubmissionBindings();
            _generation = generation;
            _suppressed = false;
            Shader.SetGlobalInt(s_generationId, generation);
            _runCount = 0;
            _submittedRuns = 0;
            _chunkIndex = 0;
            UploadCount = 0;
            UploadBytes = 0;
            FallbackCount = 0;
            PrepareCapacityStep(_requestedChunks * InstancesPerChunk, 1, false);
            foreach (var chunk in _chunks)
            {
                chunk.BeginFrame();
            }
        }

        /// <summary>Prepares bounded chunk, run, property-block, or material work without submitting draws or advancing a frame.</summary>
        /// <param name="expectedInstances">The expected number of simultaneously staged scene polygons.</param>
        /// <param name="maxObjects">The maximum independently bounded resource work units allowed in this preparation step.</param>
        /// <param name="prepareSubmissions">Whether explicit loading preparation also reserves painter spans and complete native bindings.</param>
        /// <returns>Whether the bounded requested capacity is ready or safe fallback was selected.</returns>
        internal bool PrepareCapacityStep(int expectedInstances, int maxObjects = 1, bool prepareSubmissions = true)
        {
            var target = Math.Min(MaximumChunks, Math.Max(0, ((long)expectedInstances + InstancesPerChunk - 1) / InstancesPerChunk));
            var remaining = Math.Max(0, maxObjects);
            if (!_failed && _chunks.Count < target && remaining > 0)
            {
                try
                {
                    _chunks.Add(new Chunk(this));
                    remaining--;
                }
                catch (Exception error)
                {
                    _failed = true;
                    Debug.LogWarning($"{nameof(SpriteInstancingRenderer)}: GPU instancing allocation failed; using meshes. {error.Message}");
                }
            }
            if (_failed || _chunks.Count < target || !prepareSubmissions || expectedInstances <= 0)
            {
                return _failed || _chunks.Count >= target;
            }
            var runTarget = Math.Min(SpriteSceneRenderQueues.MaximumSpans, Math.Max(128, (int)(((long)expectedInstances + 31) / 32)));
            while (_preparedSubmissionRuns < runTarget)
            {
                if (_preparedSubmissionRuns == _runs.Count)
                {
                    if (remaining == 0)
                    {
                        return false;
                    }
                    _runs.Add(new Run());
                    RunCreationCount++;
                    remaining--;
                }
                var run = _runs[_preparedSubmissionRuns];
                if (!run.HasSubmissionBinding && SubmissionBindingCreationCount >= MaximumSubmissionBindings)
                {
                    _failed = true;
                    Debug.LogWarning($"{nameof(SpriteInstancingRenderer)}: Native submission preparation exceeded its bounded snapshot budget; using meshes for this session.");
                    return true;
                }
                if (!run.PrepareSubmissionStep(this, _materials[0], _chunks[0], ref remaining))
                {
                    return false;
                }
                _preparedSubmissionRuns++;
            }
            return true;
        }

        /// <summary>Appends an already clipped polygon while retaining the original painter sequence.</summary>
        /// <param name="owner">The scene pool owning the source command.</param>
        /// <param name="texture">The unchanged atlas page, standalone image, or native surface.</param>
        /// <param name="blend">Zero for premultiplied alpha or one for additive hardware blending.</param>
        /// <param name="order">The logical painter order reserved by the existing renderer.</param>
        /// <param name="vertices">The completed CPU clipping buffer.</param>
        /// <param name="offset">The first completed polygon vertex.</param>
        /// <param name="count">The number of live polygon vertices.</param>
        /// <param name="color">The unquantized source color.</param>
        /// <param name="effects">The existing grayscale, brightness, mode, and hit flags.</param>
        /// <param name="highlight">The existing colored hit contribution.</param>
        /// <returns>Whether this polygon fits supported retained instancing capacity.</returns>
        internal bool TryAppend(ObjectSpriteRenderer owner, Texture texture, int blend, int order,
            SpriteClipVertex[] vertices, int offset, int count, Color color, Vector4 effects, Vector4 highlight)
        {
            if (_failed || count < 3 || count > VerticesPerInstance || _chunkIndex >= _chunks.Count)
            {
                FallbackCount++;
                TotalFallbackCount++;
                return false;
            }
            var chunk = _chunks[_chunkIndex];
            if (chunk.InstanceCount == InstancesPerChunk)
            {
                _chunkIndex++;
                if (_chunkIndex >= _chunks.Count)
                {
                    _requestedChunks = Math.Min(MaximumChunks, _chunkIndex + 1);
                    FallbackCount++;
                    TotalFallbackCount++;
                    return false;
                }
                chunk = _chunks[_chunkIndex];
            }
            Run? run = _runCount > 0 ? _runs[_runCount - 1] : null;
            var slot = run?.FindTexture(texture) ?? -1;
            if (run == null || !ReferenceEquals(run.Owner, owner) || run.ChunkIndex != _chunkIndex || run.Blend != blend
                || order != run.LastOrder && order != run.LastOrder + 1 || slot < 0 && run.TextureCount == TextureSlots)
            {
                if (run != null)
                {
                    if (!ReferenceEquals(run.Owner, owner) || order != run.LastOrder && order != run.LastOrder + 1)
                    {
                        PainterOrderSplitCount++;
                    }
                    if (run.ChunkIndex != _chunkIndex)
                    {
                        CapacitySplitCount++;
                    }
                    if (run.Blend != blend)
                    {
                        BlendSplitCount++;
                    }
                    if (slot < 0 && run.TextureCount == TextureSlots)
                    {
                        TextureBankSplitCount++;
                    }
                }
                if (_runCount >= SpriteSceneRenderQueues.MaximumSpans)
                {
                    SpriteSceneRenderQueues.RequireCompleteFallback("The bounded instance-span budget was exhausted.");
                    FallbackCount++;
                    TotalFallbackCount++;
                    return false;
                }
                if (_runCount == _runs.Count)
                {
                    _runs.Add(new Run());
                    RunCreationCount++;
                    RunCapacityMissCount++;
                }
                run = _runs[_runCount++];
                run.Reset(owner, _chunkIndex, chunk.InstanceCount, blend, order);
                SpriteSceneRenderQueues.RegisterOrder(order);
                slot = -1;
            }
            if (slot < 0)
            {
                slot = run.TextureCount++;
                run.Textures[slot] = texture;
            }
            var depth = -order / 32768f;
            var record = new SpriteInstanceData
            {
                VertexOffset = (uint)chunk.VertexCount, VertexCount = (uint)count, TextureSlot = (uint)slot,
                Depth = depth, Color = color, Effects = effects, Highlight = highlight
            };
            for (var index = 0; index < count; index++)
            {
                var vertex = vertices[offset + index];
                var value = new Vector4(vertex.Position.x, vertex.Position.y, vertex.Coordinates.x, vertex.Coordinates.y);
                var target = chunk.VertexCount++;
                if (!chunk.Vertices[target].Equals(value))
                {
                    chunk.Vertices[target] = value;
                    chunk.VerticesDirty = true;
                }
                run.Encapsulate(new Vector3(value.x, -value.y, depth));
            }
            if (!chunk.Instances[chunk.InstanceCount].Equals(record))
            {
                chunk.Instances[chunk.InstanceCount] = record;
                chunk.InstancesDirty = true;
            }
            chunk.InstanceCount++;
            run.Count++;
            run.LastOrder = order;
            return true;
        }

        /// <summary>Uploads all staged prefixes before registering any newly completed instanced draw.</summary>
        internal void Publish()
        {
            ReleaseCompletedSubmissionBindings();
            if (_suppressed || !ValidateRetainedTextures())
            {
                return;
            }
            if (_submissionFrame != Time.frameCount)
            {
                _submissionFrame = Time.frameCount;
                _submittedRuns = 0;
            }
            if (_bufferSubmissionFrame == Time.frameCount && _bufferSubmissionGeneration != _generation)
            {
                foreach (var chunk in _chunks)
                {
                    var changesVertices = chunk.VertexCount > 0 && (chunk.VerticesDirty || chunk.VertexCount > chunk.UploadedVertices);
                    var changesInstances = chunk.InstanceCount > 0 && (chunk.InstancesDirty || chunk.InstanceCount > chunk.UploadedInstances);
                    if (changesVertices || changesInstances)
                    {
                        SameFrameBufferFallbackCount++;
                        SpriteSceneRenderQueues.RequireCompleteFallback("A later presentation requires different GPU data within the same Unity frame.");
                        SuppressForFrame();
                        return;
                    }
                }
            }
            foreach (var chunk in _chunks)
            {
                if (chunk.VertexCount > 0 && (chunk.VerticesDirty || chunk.VertexCount > chunk.UploadedVertices))
                {
                    chunk.VertexBuffer.SetData(chunk.Vertices, 0, 0, chunk.VertexCount);
                    UploadCount++;
                    UploadBytes += chunk.VertexCount * 16L;
                    TotalUploadBytes += chunk.VertexCount * 16L;
                }
                if (chunk.InstanceCount > 0 && (chunk.InstancesDirty || chunk.InstanceCount > chunk.UploadedInstances))
                {
                    chunk.InstanceBuffer.SetData(chunk.Instances, 0, 0, chunk.InstanceCount);
                    UploadCount++;
                    UploadBytes += chunk.InstanceCount * 64L;
                    TotalUploadBytes += chunk.InstanceCount * 64L;
                }
                chunk.UploadedVertices = chunk.VertexCount;
                chunk.UploadedInstances = chunk.InstanceCount;
                chunk.VerticesDirty = false;
                chunk.InstancesDirty = false;
            }
            for (; _submittedRuns < _runCount; _submittedRuns++)
            {
                var run = _runs[_submittedRuns];
                var chunk = _chunks[run.ChunkIndex];
                var binding = run.GetSubmissionBinding(this);
                if (binding == null)
                {
                    SpriteSceneRenderQueues.RequireCompleteFallback("The bounded immutable native-submission budget was exhausted.");
                    SuppressForFrame();
                    return;
                }
                var properties = binding.Properties;
                properties.SetBuffer(s_verticesId, chunk.VertexBuffer);
                properties.SetBuffer(s_instancesId, chunk.InstanceBuffer);
                properties.SetInt(s_startId, run.FirstInstance);
                properties.SetInt(s_runGenerationId, _generation);
                for (var slot = 0; slot < TextureSlots; slot++)
                {
                    properties.SetTexture(s_textureIds[slot], run.Textures[Math.Min(slot, run.TextureCount - 1)]);
                }
                var parameters = new RenderParams(binding.GetMaterial(this, _materials[run.Blend], SpriteSceneRenderQueues.GetQueue(run.FirstOrder),
                    chunk.VertexBuffer, chunk.InstanceBuffer))
                {
                    layer = 31,
                    sortingLayerID = 0,
                    sortingOrder = run.FirstOrder,
                    worldBounds = new Bounds((run.Minimum + run.Maximum) * .5f, run.Maximum - run.Minimum),
                    matProps = properties,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                    lightProbeUsage = LightProbeUsage.Off,
                    reflectionProbeUsage = ReflectionProbeUsage.Off,
                    motionVectorMode = MotionVectorGenerationMode.ForceNoMotion
                };
                Graphics.RenderMeshPrimitives(parameters, _template, 0, run.Count);
                _bufferSubmissionFrame = Time.frameCount;
                _bufferSubmissionGeneration = _generation;
            }
        }

        /// <summary>Releases native property references only after their Unity frame's explicit draws have expired.</summary>
        private void ReleaseCompletedSubmissionBindings()
        {
            var frame = Time.frameCount;
            if (_bindingsCleanupFrame == frame)
            {
                return;
            }
            _bindingsCleanupFrame = frame;
            foreach (var run in _runs)
            {
                run.ReleaseCompletedBindings(frame);
            }
        }

        /// <summary>Invalidates a complete retained generation before a source pool releases its textures.</summary>
        /// <param name="owner">The source pool being disposed.</param>
        internal void RetireOwner(ObjectSpriteRenderer owner)
        {
            for (var index = 0; index < _runCount; index++)
            {
                if (ReferenceEquals(_runs[index].Owner, owner))
                {
                    OwnerRetirementCount++;
                    RetireSceneGeneration();
                    return;
                }
            }
        }

        /// <summary>Checks every live binding before native submission or capture can access a destroyed Unity texture.</summary>
        /// <returns>Whether the complete retained scene still owns valid sampled resources.</returns>
        private bool ValidateRetainedTextures()
        {
            for (var index = 0; index < _runCount; index++)
            {
                var run = _runs[index];
                for (var slot = 0; slot < run.TextureCount; slot++)
                {
                    if (run.Textures[slot] == null)
                    {
                        MissingTextureRetirementCount++;
                        RetireSceneGeneration();
                        Debug.LogWarning($"{nameof(SpriteInstancingRenderer)}: Retired the retained scene because a sampled texture was destroyed before presentation.");
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Clears every source reference and invalidates pending explicit draws together without substituting image data.</summary>
        private void RetireSceneGeneration()
        {
            Shader.SetGlobalInt(s_generationId, ~_generation);
            _suppressed = true;
            RetiredRunCount += _runCount;
            for (var index = 0; index < _runCount; index++)
            {
                _runs[index].Retire();
            }
            _runCount = 0;
            _submittedRuns = 0;
            SpriteSceneRenderQueues.CompleteFrame(false);
        }

        /// <summary>Counts the active runs belonging to a source pool.</summary>
        /// <param name="owner">The pool whose diagnostics are requested.</param>
        /// <returns>The number of independently sorted instance draws.</returns>
        internal int CountRuns(ObjectSpriteRenderer owner)
        {
            if (_suppressed)
            {
                return 0;
            }
            var count = 0;
            for (var index = 0; index < _runCount; index++)
            {
                if (ReferenceEquals(_runs[index].Owner, owner))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Invalidates every already registered instance draw when complete-frame ordering requires meshes.</summary>
        internal void SuppressForFrame()
        {
            _suppressed = true;
            Shader.SetGlobalInt(s_generationId, ~_generation);
        }

        /// <summary>Replays one pool into its capture-only mesh path without changing its retained instance records.</summary>
        /// <param name="owner">The pool receiving a rare explicit capture after scene submission.</param>
        internal void ReplayForCapture(ObjectSpriteRenderer owner)
        {
            if (!ValidateRetainedTextures())
            {
                return;
            }
            for (var index = 0; index < _runCount; index++)
            {
                var run = _runs[index];
                if (!ReferenceEquals(run.Owner, owner))
                {
                    continue;
                }
                var chunk = _chunks[run.ChunkIndex];
                for (var instance = run.FirstInstance; instance < run.FirstInstance + run.Count; instance++)
                {
                    var data = chunk.Instances[instance];
                    owner.AppendCaptureInstance(chunk.Vertices, data, run.Textures[data.TextureSlot]);
                }
            }
        }

        /// <summary>Releases every retained GPU resource at session teardown.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (_bufferSubmissionFrame == Time.frameCount)
            {
                Shader.SetGlobalInt(s_generationId, ~_generation);
                _retirementFrame = Time.frameCount;
                InstallRetirementHooks();
                // Every new renderer is admitted only while this fixed array has room.
                // There is one live renderer shared by every ObjectSpriteRenderer pool.
                s_retiredSessions[s_retiredSessionCount++] = this;
                return;
            }
            DisposeResources();
        }

        /// <summary>Installs allocation-free lifecycle hooks shared by every rendering session.</summary>
        private static void InstallRetirementHooks()
        {
            if (s_retirementHooksInstalled)
            {
                return;
            }
            RenderPipelineManager.endContextRendering += ReleaseRetiredSessionsAfterContext;
            Application.quitting += ReleaseRetiredSessionsOnShutdown;
            s_retirementHooksInstalled = true;
        }

        /// <summary>Releases only sessions whose Unity-frame explicit registrations have already expired.</summary>
        /// <param name="context">The completed graphics context.</param>
        /// <param name="cameras">The cameras rendered by this context, including local render requests.</param>
        private static void ReleaseRetiredSessionsAfterContext(ScriptableRenderContext context, List<Camera> cameras)
        {
            ReleaseRetiredSessions(false);
        }

        /// <summary>Releases every retired resource owner after rendering has stopped for application shutdown.</summary>
        private static void ReleaseRetiredSessionsOnShutdown()
        {
            ReleaseRetiredSessions(true);
        }

        /// <summary>Compacts the fixed retirement array while releasing completed native resource owners.</summary>
        /// <param name="shutdown">Whether rendering has stopped and every owner can be released.</param>
        private static void ReleaseRetiredSessions(bool shutdown)
        {
            var retained = 0;
            for (var index = 0; index < s_retiredSessionCount; index++)
            {
                var renderer = s_retiredSessions[index]!;
                if (shutdown || renderer._retirementFrame != Time.frameCount)
                {
                    renderer.DisposeResources();
                }
                else
                {
                    s_retiredSessions[retained++] = renderer;
                }
            }
            Array.Clear(s_retiredSessions, retained, s_retiredSessionCount - retained);
            s_retiredSessionCount = retained;
        }

        /// <summary>Releases retained GPU resources after their native CPU submission frame can no longer inspect them.</summary>
        private void DisposeResources()
        {
            foreach (var chunk in _chunks)
            {
                chunk.Dispose();
            }
            _chunks.Clear();
            foreach (var run in _runs)
            {
                run.ReleaseMaterials();
            }
            _runs.Clear();
            foreach (var material in _materials)
            {
                Destroy(material);
            }
            Destroy(_template);
        }

        /// <summary>Allocates the fixed property-name lookup once.</summary>
        /// <returns>The eight ordered texture binding identifiers.</returns>
        private static int[] CreateTextureIds()
        {
            var ids = new int[TextureSlots];
            for (var index = 0; index < ids.Length; index++)
            {
                ids[index] = Shader.PropertyToID("_PvZTexture" + index);
            }
            return ids;
        }

        /// <summary>Destroys an owned Unity resource using the current lifecycle.</summary>
        /// <param name="resource">The owned material or fixed template.</param>
        private static void Destroy(UnityEngine.Object resource)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(resource);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(resource);
            }
        }

        /// <summary>Owns fixed-capacity staging arrays and their two stable GPU buffers.</summary>
        private sealed class Chunk : IDisposable
        {
            /// <summary>The compact clipped vertex staging array.</summary>
            internal NativeArray<Vector4> Vertices;
            /// <summary>The compact polygon staging array.</summary>
            internal NativeArray<SpriteInstanceData> Instances;
            /// <summary>The stable vertex structured buffer.</summary>
            internal readonly GraphicsBuffer VertexBuffer;
            /// <summary>The stable instance structured buffer.</summary>
            internal readonly GraphicsBuffer InstanceBuffer;
            /// <summary>The current active vertex prefix.</summary>
            internal int VertexCount;
            /// <summary>The current active instance prefix.</summary>
            internal int InstanceCount;
            /// <summary>The last uploaded vertex prefix.</summary>
            internal int UploadedVertices;
            /// <summary>The last uploaded instance prefix.</summary>
            internal int UploadedInstances;
            /// <summary>Whether this generation changed vertex values.</summary>
            internal bool VerticesDirty;
            /// <summary>Whether this generation changed instance values.</summary>
            internal bool InstancesDirty;

            /// <summary>Allocates one bounded pair, cleaning up if the second allocation fails.</summary>
            /// <param name="owner">The renderer accumulating actual native allocation diagnostics.</param>
            internal Chunk(SpriteInstancingRenderer owner)
            {
                GraphicsBuffer? vertices = null;
                try
                {
                    Vertices = new NativeArray<Vector4>(InstancesPerChunk * VerticesPerInstance, Allocator.Persistent);
                    Instances = new NativeArray<SpriteInstanceData>(InstancesPerChunk, Allocator.Persistent);
                    vertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Vertices.Length, 16);
                    owner.BufferCreationCount++;
                    InstanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, Instances.Length, 64);
                    owner.BufferCreationCount++;
                    VertexBuffer = vertices;
                }
                catch
                {
                    vertices?.Dispose();
                    if (Vertices.IsCreated)
                    {
                        Vertices.Dispose();
                    }
                    if (Instances.IsCreated)
                    {
                        Instances.Dispose();
                    }
                    throw;
                }
            }

            /// <summary>Starts comparison against retained CPU content while preserving changes not yet uploaded by a prior mesh fallback.</summary>
            internal void BeginFrame()
            {
                VertexCount = 0;
                InstanceCount = 0;
            }

            /// <summary>Releases the two buffers when the session ends.</summary>
            public void Dispose()
            {
                VertexBuffer.Dispose();
                InstanceBuffer.Dispose();
                Vertices.Dispose();
                Instances.Dispose();
            }
        }

        /// <summary>Retains one contiguous painter span and its original eight-texture bank.</summary>
        private sealed class Run
        {
            /// <summary>The unchanged source textures in first-appearance order.</summary>
            internal readonly Texture[] Textures = new Texture[TextureSlots];
            /// <summary>The retained native snapshots, quarantined until the Unity frame using them has completed.</summary>
            private readonly List<SubmissionBinding> _bindings = new List<SubmissionBinding>(1);
            /// <summary>Gets whether this painter span already owns at least one retained native snapshot.</summary>
            internal bool HasSubmissionBinding => _bindings.Count > 0;
            /// <summary>The scene pool owning this span.</summary>
            internal ObjectSpriteRenderer? Owner;
            /// <summary>The chunk containing the contiguous instance prefix.</summary>
            internal int ChunkIndex;
            /// <summary>The first instance in that chunk.</summary>
            internal int FirstInstance;
            /// <summary>The number of instances in the span.</summary>
            internal int Count;
            /// <summary>The current hardware blend variant.</summary>
            internal int Blend;
            /// <summary>The first painter order used for external sorting.</summary>
            internal int FirstOrder;
            /// <summary>The most recently appended painter order.</summary>
            internal int LastOrder;
            /// <summary>The number of populated texture slots.</summary>
            internal int TextureCount;
            /// <summary>The minimum active world position.</summary>
            internal Vector3 Minimum;
            /// <summary>The maximum active world position.</summary>
            internal Vector3 Maximum;
            /// <summary>Whether the first vertex has initialized the bounds.</summary>
            private bool _hasBounds;

            /// <summary>Begins a new span without allocating new property blocks or arrays.</summary>
            /// <param name="owner">The source pool.</param>
            /// <param name="chunk">The stable chunk index.</param>
            /// <param name="first">The first instance in the chunk.</param>
            /// <param name="blend">The hardware blend variant.</param>
            /// <param name="order">The span's first painter order.</param>
            internal void Reset(ObjectSpriteRenderer owner, int chunk, int first, int blend, int order)
            {
                Array.Clear(Textures, 0, Textures.Length);
                Owner = owner;
                ChunkIndex = chunk;
                FirstInstance = first;
                Blend = blend;
                FirstOrder = order;
                LastOrder = order;
                Count = 0;
                TextureCount = 0;
                _hasBounds = false;
            }

            /// <summary>Clears CPU source references while preserving already submitted native bindings until their frame expires.</summary>
            internal void Retire()
            {
                Array.Clear(Textures, 0, Textures.Length);
                Owner = null;
                Count = 0;
                TextureCount = 0;
                _hasBounds = false;
            }

            /// <summary>Finds an existing slot without hashing Unity objects or allocating a lookup.</summary>
            /// <param name="texture">The exact source texture object.</param>
            /// <returns>The matching slot, or minus one.</returns>
            internal int FindTexture(Texture texture)
            {
                for (var slot = 0; slot < TextureCount; slot++)
                {
                    if (ReferenceEquals(Textures[slot], texture))
                    {
                        return slot;
                    }
                }
                return -1;
            }

            /// <summary>Gets a native snapshot that has not been submitted during this Unity frame.</summary>
            /// <param name="owner">The renderer enforcing and counting retained snapshot capacity.</param>
            /// <returns>An immutable-until-next-frame snapshot, or null when bounded capacity requires complete mesh fallback.</returns>
            internal SubmissionBinding? GetSubmissionBinding(SpriteInstancingRenderer owner)
            {
                var frame = Time.frameCount;
                foreach (var binding in _bindings)
                {
                    if (binding.SubmittedFrame != frame)
                    {
                        binding.SubmittedFrame = frame;
                        return binding;
                    }
                }
                if (owner.SubmissionBindingCreationCount >= MaximumSubmissionBindings)
                {
                    return null;
                }
                var created = new SubmissionBinding { SubmittedFrame = frame };
                _bindings.Add(created);
                owner.SubmissionBindingCreationCount++;
                owner.SubmissionBindingCapacityMissCount++;
                return created;
            }

            /// <summary>Prepares this span's first native snapshot and material in separate bounded loading steps.</summary>
            /// <param name="owner">The renderer owning preparation and diagnostic counters.</param>
            /// <param name="source">The original premultiplied blend template used to initialize the owned material.</param>
            /// <param name="chunk">An existing prepared chunk supplying valid native buffer defaults.</param>
            /// <param name="remaining">The remaining independently bounded resource work units.</param>
            /// <returns>Whether at least one complete native snapshot can be reused without constructing resources.</returns>
            internal bool PrepareSubmissionStep(SpriteInstancingRenderer owner, Material source, Chunk chunk, ref int remaining)
            {
                if (_bindings.Count == 0)
                {
                    if (remaining == 0 || owner.SubmissionBindingCreationCount >= MaximumSubmissionBindings)
                    {
                        return false;
                    }
                    _bindings.Add(new SubmissionBinding());
                    owner.SubmissionBindingCreationCount++;
                    remaining--;
                }
                var binding = _bindings[0];
                if (!binding.HasMaterial)
                {
                    if (remaining == 0)
                    {
                        return false;
                    }
                    binding.GetMaterial(owner, source, 3000, chunk.VertexBuffer, chunk.InstanceBuffer);
                    binding.PrepareProperties(chunk);
                    remaining--;
                }
                return true;
            }

            /// <summary>Clears completed native property references without touching a snapshot submitted in this frame.</summary>
            /// <param name="frame">The current Unity frame whose native submissions must stay immutable.</param>
            internal void ReleaseCompletedBindings(int frame)
            {
                foreach (var binding in _bindings)
                {
                    if (binding.SubmittedFrame != frame && binding.SubmittedFrame >= 0)
                    {
                        binding.Properties.Clear();
                        binding.SubmittedFrame = -1;
                    }
                }
            }

            /// <summary>Releases every owned queue material at session teardown.</summary>
            internal void ReleaseMaterials()
            {
                foreach (var binding in _bindings)
                {
                    binding.ReleaseMaterial();
                }
            }

            /// <summary>Includes one original clipped world vertex in the conservative draw bounds.</summary>
            /// <param name="vertex">The current polygon vertex with its original painter depth.</param>
            internal void Encapsulate(Vector3 vertex)
            {
                if (!_hasBounds)
                {
                    Minimum = vertex;
                    Maximum = vertex;
                    _hasBounds = true;
                }
                else
                {
                    Minimum = Vector3.Min(Minimum, vertex);
                    Maximum = Vector3.Max(Maximum, vertex);
                }
            }
        }

        /// <summary>Retains one property block and material without mutation while a Unity frame can still consume its submission.</summary>
        private sealed class SubmissionBinding
        {
            /// <summary>The native binding snapshot passed to one explicit draw in the current Unity frame.</summary>
            internal readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
            /// <summary>The Unity frame using this snapshot, or minus one after its native references are released.</summary>
            internal int SubmittedFrame = -1;
            /// <summary>The owned hardware blend material carrying this snapshot's compressed queue.</summary>
            private Material? _material;
            /// <summary>The blend template most recently copied into this independent material.</summary>
            private Material? _materialSource;
            /// <summary>The structured vertex buffer supplied to the material's native validation path.</summary>
            private GraphicsBuffer? _vertexBuffer;
            /// <summary>The structured instance buffer supplied to the material's native validation path.</summary>
            private GraphicsBuffer? _instanceBuffer;
            /// <summary>Gets whether preparation or an earlier presentation already created this snapshot's owned material.</summary>
            internal bool HasMaterial => _material != null;

            /// <summary>Initializes the property sheet's fixed binding shape before any presentation records it.</summary>
            /// <param name="chunk">The existing prepared chunk used for native buffer validation.</param>
            internal void PrepareProperties(Chunk chunk)
            {
                Properties.SetBuffer(s_verticesId, chunk.VertexBuffer);
                Properties.SetBuffer(s_instancesId, chunk.InstanceBuffer);
                Properties.SetInt(s_startId, 0);
                Properties.SetInt(s_runGenerationId, 0);
                for (var slot = 0; slot < TextureSlots; slot++)
                {
                    Properties.SetTexture(s_textureIds[slot], Texture2D.whiteTexture);
                }
            }

            /// <summary>Gets an owned material whose properties and queue stay immutable until this Unity frame completes.</summary>
            /// <param name="source">The unchanged hardware blend template.</param>
            /// <param name="owner">The renderer recording actual native material creations.</param>
            /// <param name="queue">The compressed painter-span queue of this submission.</param>
            /// <param name="vertices">The unchanged chunk vertex buffer also bound by this submission's property block.</param>
            /// <param name="instances">The unchanged chunk instance buffer also bound by this submission's property block.</param>
            /// <returns>The material owned only by this native submission snapshot.</returns>
            internal Material GetMaterial(SpriteInstancingRenderer owner, Material source, int queue, GraphicsBuffer vertices, GraphicsBuffer instances)
            {
                if (_material == null)
                {
                    _material = new Material(source) { hideFlags = HideFlags.HideAndDontSave, enableInstancing = true };
                    _materialSource = source;
                    owner.SubmissionMaterialCreationCount++;
                    _vertexBuffer = null;
                    _instanceBuffer = null;
                }
                else if (!ReferenceEquals(_materialSource, source))
                {
                    _material.CopyPropertiesFromMaterial(source);
                    _material.enableInstancing = true;
                    _materialSource = source;
                    _vertexBuffer = null;
                    _instanceBuffer = null;
                }
                // Vulkan validates required structured bindings on the material before
                // applying the explicit draw's property block. Both paths must name
                // the same retained chunk; no texture or per-instance data is copied.
                if (!ReferenceEquals(_vertexBuffer, vertices))
                {
                    _material.SetBuffer(s_verticesId, vertices);
                    _vertexBuffer = vertices;
                }
                if (!ReferenceEquals(_instanceBuffer, instances))
                {
                    _material.SetBuffer(s_instancesId, instances);
                    _instanceBuffer = instances;
                }
                if (_material.renderQueue != queue)
                {
                    _material.renderQueue = queue;
                }
                return _material;
            }

            /// <summary>Releases this snapshot's material at session teardown.</summary>
            internal void ReleaseMaterial()
            {
                if (_material != null)
                {
                    Destroy(_material);
                    _material = null;
                }
            }
        }
    }
}
