#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Owns stable, renderer-free actor and sprite nodes used to inspect and hide batched artwork.</summary>
    internal sealed class SpriteHierarchy : IDisposable
    {
        /// <summary>Stores the logical actor and the sprite nodes retained for its lifetime.</summary>
        private sealed class Actor
        {
            /// <summary>The scene node representing this actor.</summary>
            internal readonly GameObject Root;
            /// <summary>The actor transform cached once instead of querying Unity during each drawing scope.</summary>
            internal readonly Transform RootTransform;
            /// <summary>The stable source-track nodes owned by this actor.</summary>
            internal readonly Dictionary<(object Asset, int Track, int Attachment), Node> Sprites = new Dictionary<(object, int, int), Node>();
            /// <summary>The per-source numeric track tables retaining the same nodes as the inspection dictionary.</summary>
            internal readonly Dictionary<(object Asset, int Attachment), TrackNodes> TrackTables = new Dictionary<(object, int), TrackNodes>();
            /// <summary>The source asset whose numeric table was most recently used by this actor.</summary>
            internal object? LastTrackAsset;
            /// <summary>The attachment slot owning the most recently used numeric table.</summary>
            internal int LastTrackAttachment;
            /// <summary>The most recently used source table, avoiding repeated composite-key hashing.</summary>
            internal TrackNodes? LastTrackTable;
            /// <summary>The small attachment groups separating repeated body and head artwork in the hierarchy.</summary>
            internal readonly Dictionary<int, GameObject> Attachments = new Dictionary<int, GameObject>();
            /// <summary>The readable species name most recently associated with this logical identity.</summary>
            internal string DisplayName;
            /// <summary>The most recent frame in which the simulation retained this actor.</summary>
            internal int AliveFrame;
            /// <summary>The actor visibility observed during the preceding presentation.</summary>
            internal bool Visible = true;

            /// <summary>Creates a logical actor below its category.</summary>
            /// <param name="owner">The inspection registry owning this node.</param>
            /// <param name="parent">The category containing the actor.</param>
            /// <param name="name">The readable actor identity.</param>
            internal Actor(SpriteHierarchy owner, Transform parent, string name)
            {
                Root = owner.CreateObservedNode(parent, name);
                RootTransform = Root.transform;
                DisplayName = name;
            }
        }

        /// <summary>Retains direct numeric source-track access for one actor, asset, and attachment slot.</summary>
        private sealed class TrackNodes
        {
            /// <summary>The lazily expanded source-index table, bounded independently of sparse external indices.</summary>
            internal Node?[] Nodes = new Node?[64];
        }

        /// <summary>The largest source-track table retained for one asset and attachment slot.</summary>
        private const int MaximumTrackTableLength = 4096;

        /// <summary>Tracks the previously observed visibility of one inspectable sprite.</summary>
        private sealed class Node
        {
            /// <summary>The source sprite's renderer-free hierarchy object.</summary>
            internal readonly GameObject Object;
            /// <summary>The source transform cached once for allocation-free native drawing contexts.</summary>
            internal readonly Transform Transform;
            /// <summary>The cached pose inspector, avoiding per-command component lookups.</summary>
            internal readonly SpriteHierarchySprite Metadata;
            /// <summary>The visibility observed during the preceding presentation.</summary>
            internal bool Visible = true;

            /// <summary>Creates a source sprite node.</summary>
            /// <param name="owner">The inspection registry owning this source component.</param>
            /// <param name="parent">The actor owning this sprite.</param>
            /// <param name="name">The original track or attachment name.</param>
            internal Node(SpriteHierarchy owner, Transform parent, string name)
            {
                Object = owner.CreateObservedNode(parent, name, true);
                Transform = Object.transform;
                Metadata = Object.GetComponent<SpriteHierarchySprite>();
            }
        }

        /// <summary>Bounds retired actor roots retained between spawns.</summary>
        private const int MaximumInactiveActors = 256;
        /// <summary>Bounds retired source-track objects retained between spawns.</summary>
        private const int MaximumInactiveSprites = 4096;
        /// <summary>Bounds retired attachment nodes retained between spawns.</summary>
        private const int MaximumInactiveAttachments = 512;
        /// <summary>Reuses complete actor dictionaries and root objects after model retirement.</summary>
        private readonly Stack<Actor> _inactiveActors = new Stack<Actor>();
        /// <summary>Reuses inspected sprite objects separately from logical track bindings.</summary>
        private readonly Stack<Node> _inactiveSprites = new Stack<Node>();
        /// <summary>Reuses numeric source-track lookup tables after actor retirement.</summary>
        private readonly Stack<TrackNodes> _inactiveTrackTables = new Stack<TrackNodes>();
        /// <summary>Retains caller-prepared labels only until their exact model binding consumes them.</summary>
        private readonly Dictionary<(string Category, int Id, string Name), string> _preparedActorLabels = new Dictionary<(string, int, string), string>();
        /// <summary>Counts model labels that were formatted outside explicit preparation.</summary>
        internal int ActorLabelMissCount { get; private set; }
        /// <summary>Counts actor roots constructed because prepared and recycled capacity was exhausted.</summary>
        internal int ActorRootCreationCount { get; private set; }
        /// <summary>Reuses named head and held-item grouping nodes.</summary>
        private readonly Stack<GameObject> _inactiveAttachments = new Stack<GameObject>();
        /// <summary>The hidden inactive root whose children cannot affect live actor visibility.</summary>
        private GameObject? _poolRoot;
        /// <summary>Rejects lifecycle notifications from a previous match binding.</summary>
        private uint _bindingGeneration;
        /// <summary>Gets whether inspection objects are enabled for this build.</summary>
        internal bool Enabled
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
        /// <summary>Gets how often a source node needed a new Unity component.</summary>
        internal int SpriteNodeCreationCount { get; private set; }
        /// <summary>Gets the number of reusable retired source nodes.</summary>
        internal int InactiveSpriteCount => _inactiveSprites.Count;

        /// <summary>The stable actor identities belonging to the current simulation session.</summary>
        private readonly Dictionary<(string Category, int Id), Actor> _actors = new Dictionary<(string, int), Actor>();
        /// <summary>The category nodes shared by plants, zombies, and other actor kinds.</summary>
        private readonly Dictionary<string, GameObject> _categories = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        /// <summary>The reusable retirement buffer avoiding allocations while a session is stable.</summary>
        private readonly List<(string Category, int Id)> _retired = new List<(string, int)>();
        /// <summary>The hierarchy root, created only after the first actor is observed.</summary>
        private GameObject? _root;
        /// <summary>The simulation session whose numeric identities are currently bound.</summary>
        private object? _session;
        /// <summary>The actor receiving the current synchronous drawing callbacks.</summary>
        private Actor? _actor;
        /// <summary>The attachment slot separating repeated body and head tracks.</summary>
        private int _attachment;
        /// <summary>The frame identity used for model-based actor retirement.</summary>
        private int _frame;
        /// <summary>The cached pose inspector associated with the current source-track context.</summary>
        private static SpriteHierarchySprite? s_currentMetadata;

        /// <summary>Gets the stable node associated with the current logical drawing command.</summary>
        internal static Transform? CurrentSprite { get; private set; }
        /// <summary>Gets the current logical source object without a native Transform.gameObject query.</summary>
        internal static GameObject? CurrentSpriteObject { get; private set; }
        /// <summary>Gets the revision invalidating frozen captures after hierarchy visibility changes.</summary>
        internal int VisibilityRevision { get; private set; }
        /// <summary>Gets the number of live logical actor roots.</summary>
        internal int ActorCount => _actors.Count;

        /// <summary>Starts a model-liveness frame and detects externally toggled hierarchy objects.</summary>
        /// <param name="session">The simulation instance separating numeric identities across matches.</param>
        internal void BeginFrame(object session)
        {
            if (!Enabled)
            {
                return;
            }
            using var profile = SpriteRenderingProfiler.Measure(SpriteRenderingProfiler.Hierarchy);
            if (!ReferenceEquals(_session, session))
            {
                Clear();
                _session = session;
            }

            _frame++;
        }

        /// <summary>Retains an actor independently of whether it is visible or submits any artwork.</summary>
        /// <param name="category">The model category, such as Plants or Zombies.</param>
        /// <param name="id">The simulation identity within the current session.</param>
        /// <param name="displayName">The readable species name used when first binding the actor.</param>
        internal void MarkAlive(string category, int id, string displayName)
        {
            if (!Enabled)
            {
                return;
            }
            GetActor(category, id, displayName).AliveFrame = _frame;
        }

        /// <summary>Begins a synchronous actor drawing context without changing its geometry or painter order.</summary>
        /// <param name="category">The logical model category.</param>
        /// <param name="id">The stable simulation identity.</param>
        /// <param name="displayName">The readable species name.</param>
        /// <returns>A scope restoring the preceding actor and attachment context.</returns>
        internal SpriteHierarchyScope BeginActor(string category, int id, string displayName)
        {
            if (!Enabled)
            {
                return default;
            }
            var scope = SaveContext();
            _actor = GetActor(category, id, displayName);
            _actor.AliveFrame = _frame;
            _attachment = 0;
            CurrentSprite = _actor.RootTransform;
            CurrentSpriteObject = _actor.Root;
            s_currentMetadata = null;
            return scope;
        }

        /// <summary>Separates repeated source tracks belonging to an attached head or flag.</summary>
        /// <param name="slot">The stable attachment slot within its actor.</param>
        /// <returns>A scope restoring the previous attachment slot.</returns>
        internal SpriteHierarchyScope BeginAttachment(int slot)
        {
            if (!Enabled)
            {
                return default;
            }
            var scope = SaveContext();
            _attachment = slot;
            return scope;
        }

        /// <summary>Associates a source animation track with its stable inspection node.</summary>
        /// <param name="asset">The parsed animation asset identity.</param>
        /// <param name="trackIndex">The original source-track index.</param>
        /// <param name="displayName">The original readable track name.</param>
        /// <returns>A scope restoring the preceding sprite context.</returns>
        internal SpriteHierarchyScope BeginSprite(object asset, int trackIndex, string displayName)
        {
            if (!Enabled)
            {
                return default;
            }
            var scope = SaveContext();
            if (_actor != null)
            {
                TrackNodes? table = null;
                Node? node = null;
                if ((uint)trackIndex < MaximumTrackTableLength)
                {
                    if (!ReferenceEquals(_actor.LastTrackAsset, asset) || _actor.LastTrackAttachment != _attachment
                        || _actor.LastTrackTable == null)
                    {
                        if (!_actor.TrackTables.TryGetValue((asset, _attachment), out var tracks))
                        {
                            tracks = _inactiveTrackTables.Count > 0 ? _inactiveTrackTables.Pop() : new TrackNodes();
                            _actor.TrackTables.Add((asset, _attachment), tracks);
                        }
                        _actor.LastTrackAsset = asset;
                        _actor.LastTrackAttachment = _attachment;
                        _actor.LastTrackTable = tracks;
                    }
                    table = _actor.LastTrackTable;
                    if (trackIndex < table.Nodes.Length)
                    {
                        node = table.Nodes[trackIndex];
                    }
                }
                var key = (asset, trackIndex, _attachment);
                if (node == null && !_actor.Sprites.TryGetValue(key, out node))
                {
                    var parent = _actor.Root;
                    if (_attachment != 0 && !_actor.Attachments.TryGetValue(_attachment, out parent))
                    {
                        var attachmentName = _attachment == 10 ? "Flag"
                            : _attachment == 11 ? "Held Item" : _attachment == 12 ? "Plant Head" : "Head " + _attachment;
                        parent = RentAttachment(_actor.RootTransform, attachmentName);
                        _actor.Attachments.Add(_attachment, parent);
                    }

                    node = RentSprite(parent.transform, displayName);
                    _actor.Sprites.Add(key, node);
                }
                if (table != null && (trackIndex >= table.Nodes.Length || table.Nodes[trackIndex] == null))
                {
                    if (trackIndex >= table.Nodes.Length)
                    {
                        var length = table.Nodes.Length;
                        while (length <= trackIndex)
                        {
                            length *= 2;
                        }
                        Array.Resize(ref table.Nodes, length);
                    }
                    table.Nodes[trackIndex] = node;
                }

                CurrentSprite = node.Transform;
                CurrentSpriteObject = node.Object;
                s_currentMetadata = node.Metadata;
            }

            return scope;
        }

        /// <summary>Associates an original standalone shadow or effect with a stable actor child.</summary>
        /// <param name="name">The stable original drawing role.</param>
        /// <returns>A scope restoring the preceding source-track context.</returns>
        internal SpriteHierarchyScope BeginNamedSprite(string name)
        {
            return BeginSprite(name, 0, name);
        }

        /// <summary>Publishes source and full affine pose information for the currently associated source-track node.</summary>
        /// <param name="image">The logical source image before atlas remapping.</param>
        /// <param name="width">The requested original drawing width.</param>
        /// <param name="height">The requested original drawing height.</param>
        /// <param name="uv">The original source rectangle before atlas remapping.</param>
        /// <param name="matrix">The complete source-to-target affine matrix.</param>
        /// <param name="color">The vertex tint and opacity.</param>
        /// <param name="mode">The source compositing mode.</param>
        internal static void RecordCurrentSprite(SpriteImage image, float width, float height, Rect uv, in Matrix4x4 matrix, Color color, int mode)
        {
            if (s_currentMetadata != null)
            {
                s_currentMetadata.Record(image, width, height, uv, matrix, color, mode);
            }
        }

        /// <summary>Removes actors absent from both the live simulation and retained corpse list.</summary>
        internal void EndFrame()
        {
            if (!Enabled)
            {
                return;
            }
            _retired.Clear();
            foreach (var pair in _actors)
            {
                if (pair.Value.AliveFrame != _frame)
                {
                    _retired.Add(pair.Key);
                }
            }

            foreach (var key in _retired)
            {
                RetireActor(_actors[key]);
                _actors.Remove(key);
            }

            _retired.Clear();
        }

        /// <summary>Detects external hierarchy switches without modifying the user's active state.</summary>
        internal void RefreshVisibility()
        {
            if (!Enabled)
            {
                return;
            }
            // Lifecycle observers notify immediately, including changes to ancestor active state.
        }

        /// <summary>Releases all inspection nodes without affecting independently owned batch meshes.</summary>
        public void Dispose()
        {
            Clear();
            _session = null;
        }

        /// <summary>Restores a synchronous scope captured before an actor, attachment, or sprite draw.</summary>
        /// <param name="actor">The preceding private actor record.</param>
        /// <param name="attachment">The preceding attachment slot.</param>
        /// <param name="sprite">The preceding logical source node.</param>
        /// <param name="spriteObject">The preceding cached source object.</param>
        /// <param name="metadata">The preceding source pose inspector.</param>
        internal void Restore(object? actor, int attachment, Transform? sprite, GameObject? spriteObject, SpriteHierarchySprite? metadata)
        {
            _actor = (Actor?)actor;
            _attachment = attachment;
            CurrentSprite = sprite;
            CurrentSpriteObject = spriteObject;
            s_currentMetadata = metadata;
        }

        /// <summary>Saves the current synchronous drawing context without allocating a callback.</summary>
        /// <returns>The scope containing the preceding context.</returns>
        private SpriteHierarchyScope SaveContext()
        {
            return new SpriteHierarchyScope(this, _actor, _attachment, CurrentSprite, CurrentSpriteObject, s_currentMetadata);
        }

        /// <summary>Gets or creates a stable actor record in the current session.</summary>
        /// <param name="category">The actor category.</param>
        /// <param name="id">The numeric simulation identity.</param>
        /// <param name="displayName">The readable species name.</param>
        /// <returns>The retained actor record.</returns>
        private Actor GetActor(string category, int id, string displayName)
        {
            var key = (category, id);
            if (_actors.TryGetValue(key, out var actor))
            {
                if (!string.Equals(actor.DisplayName, displayName, StringComparison.Ordinal))
                {
                    actor.DisplayName = displayName;
                    actor.Root.name = ActorLabel(category, id, displayName);
                }

                return actor;
            }

            if (_root == null)
            {
                _root = CreateObservedNode(null, "PvZ Actors");
            }

            if (!_categories.TryGetValue(category, out var parent))
            {
                parent = CreateObservedNode(_root.transform, category);
                _categories.Add(category, parent);
            }

            if (_inactiveActors.Count > 0)
            {
                actor = _inactiveActors.Pop();
                actor.RootTransform.SetParent(parent.transform, false);
                ResetTransform(actor.RootTransform);
                actor.Root.name = ActorLabel(category, id, displayName);
                BindObserver(actor.Root);
                actor.Root.SetActive(true);
            }
            else
            {
                actor = new Actor(this, parent.transform, ActorLabel(category, id, displayName));
                ActorRootCreationCount++;
            }
            actor.DisplayName = displayName;
            _actors.Add(key, actor);
            return actor;
        }

        /// <summary>Clears actor bindings when a simulation session or renderer ends.</summary>
        private void Clear()
        {
            _bindingGeneration++;
            foreach (var actor in _inactiveActors)
            {
                if (actor.Root != null)
                {
                    DestroyNode(actor.Root);
                }
            }
            _inactiveActors.Clear();
            _inactiveSprites.Clear();
            _inactiveTrackTables.Clear();
            _preparedActorLabels.Clear();
            _inactiveAttachments.Clear();
            if (_poolRoot != null)
            {
                DestroyNode(_poolRoot);
                _poolRoot = null;
            }
            if (_root != null)
            {
                DestroyNode(_root);
            }

            _root = null;
            _actor = null;
            CurrentSprite = null;
            CurrentSpriteObject = null;
            s_currentMetadata = null;
            _actors.Clear();
            _categories.Clear();
            _retired.Clear();
            VisibilityRevision++;
        }

        /// <summary>Creates an inspection-only node with no rendering components.</summary>
        /// <param name="parent">The optional logical parent.</param>
        /// <param name="name">The hierarchy label.</param>
        /// <param name="sprite">Whether to attach the source-pose inspector instead of a grouping observer.</param>
        /// <returns>The newly created node.</returns>
        private GameObject CreateObservedNode(Transform? parent, string name, bool sprite = false)
        {
            var node = new GameObject(name)
            {
                hideFlags = HideFlags.DontSave,
                layer = 31
            };
            node.transform.SetParent(parent, false);
            if (sprite)
            {
                node.AddComponent<SpriteHierarchySprite>().BindVisibility(this, _bindingGeneration);
                SpriteNodeCreationCount++;
            }
            else
            {
                node.AddComponent<SpriteHierarchyVisibility>().Bind(this, _bindingGeneration);
            }
            return node;
        }

        /// <summary>Notifies synchronous visibility changes from a currently bound inspection component.</summary>
        /// <param name="generation">The observer's match generation.</param>
        internal void NotifyVisibilityChanged(uint generation)
        {
            if (generation == _bindingGeneration)
            {
                VisibilityRevision++;
            }
        }

        /// <summary>Clears manual transform edits before another logical model acquires a pooled inspection node.</summary>
        /// <param name="transform">The reused source or grouping transform.</param>
        private static void ResetTransform(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>Rebinds a grouping observer after an inactive node is reused.</summary>
        /// <param name="node">The grouping object.</param>
        private void BindObserver(GameObject node)
        {
            node.GetComponent<SpriteHierarchyVisibility>().Bind(this, _bindingGeneration);
        }

        /// <summary>Gets the inactive root without adding it below the visible actor hierarchy.</summary>
        /// <returns>The inactive storage transform.</returns>
        private Transform PoolRoot()
        {
            if (_poolRoot == null)
            {
                _poolRoot = new GameObject("PvZ inactive inspection nodes")
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    layer = 31
                };
                _poolRoot.SetActive(false);
            }
            return _poolRoot.transform;
        }

        /// <summary>Takes a source object while resetting all metadata and visibility from the previous binding.</summary>
        /// <param name="parent">The new actor or attachment.</param>
        /// <param name="name">The immutable source-track label.</param>
        /// <returns>A visible source node with no previous pose.</returns>
        private Node RentSprite(Transform parent, string name)
        {
            if (_inactiveSprites.Count == 0)
            {
                return new Node(this, parent, name);
            }
            var node = _inactiveSprites.Pop();
            node.Metadata.ResetPose();
            node.Object.name = name;
            node.Transform.SetParent(parent, false);
            ResetTransform(node.Transform);
            node.Metadata.BindVisibility(this, _bindingGeneration);
            node.Object.SetActive(true);
            return node;
        }

        /// <summary>Takes an attachment grouping object without creating another component when warmed.</summary>
        /// <param name="parent">The actor root.</param>
        /// <param name="name">The attachment label.</param>
        /// <returns>The rebound grouping object.</returns>
        private GameObject RentAttachment(Transform parent, string name)
        {
            if (_inactiveAttachments.Count == 0)
            {
                return CreateObservedNode(parent, name);
            }
            var node = _inactiveAttachments.Pop();
            node.transform.SetParent(parent, false);
            ResetTransform(node.transform);
            node.name = name;
            BindObserver(node);
            node.SetActive(true);
            return node;
        }

        /// <summary>Recycles retired bindings without carrying user visibility or old source references to another model.</summary>
        /// <param name="actor">The logically retired actor.</param>
        private void RetireActor(Actor actor)
        {
            foreach (var node in actor.Sprites.Values)
            {
                node.Metadata.BindVisibility(null, 0);
                node.Metadata.ResetPose();
                node.Object.SetActive(false);
                node.Transform.SetParent(PoolRoot(), false);
                if (_inactiveSprites.Count < MaximumInactiveSprites)
                {
                    _inactiveSprites.Push(node);
                }
                else
                {
                    DestroyNode(node.Object);
                }
            }
            foreach (var attachment in actor.Attachments.Values)
            {
                attachment.GetComponent<SpriteHierarchyVisibility>().Bind(null, 0);
                attachment.SetActive(false);
                attachment.transform.SetParent(PoolRoot(), false);
                if (_inactiveAttachments.Count < MaximumInactiveAttachments)
                {
                    _inactiveAttachments.Push(attachment);
                }
                else
                {
                    DestroyNode(attachment);
                }
            }
            actor.Sprites.Clear();
            foreach (var table in actor.TrackTables.Values)
            {
                Array.Clear(table.Nodes, 0, table.Nodes.Length);
                if (_inactiveTrackTables.Count < 1024)
                {
                    _inactiveTrackTables.Push(table);
                }
            }
            actor.TrackTables.Clear();
            actor.Attachments.Clear();
            actor.LastTrackAsset = null;
            actor.LastTrackTable = null;
            actor.DisplayName = string.Empty;
            actor.Root.GetComponent<SpriteHierarchyVisibility>().Bind(null, 0);
            actor.Root.SetActive(false);
            actor.RootTransform.SetParent(PoolRoot(), false);
            if (_inactiveActors.Count < MaximumInactiveActors)
            {
                _inactiveActors.Push(actor);
            }
            else
            {
                DestroyNode(actor.Root);
            }
            VisibilityRevision++;
        }

        /// <summary>Formats one known Editor label before gameplay without consuming simulation state.</summary>
        /// <param name="category">The actor category.</param>
        /// <param name="id">The reserved model identifier.</param>
        /// <param name="displayName">The native species label.</param>
        internal void PrepareActorLabel(string category, int id, string displayName)
        {
            if (!Enabled)
            {
                return;
            }
            var key = (category, id, displayName);
            if (_preparedActorLabels.Count < 16384 && !_preparedActorLabels.ContainsKey(key))
            {
                _preparedActorLabels.Add(key, displayName + " " + id);
            }
        }

        /// <summary>Consumes a prepared label or records a capacity miss while preserving the correct inspection name.</summary>
        /// <param name="category">The actor category.</param>
        /// <param name="id">The logical identifier.</param>
        /// <param name="name">The species label.</param>
        /// <returns>The complete readable actor label.</returns>
        private string ActorLabel(string category, int id, string name)
        {
            var key = (category, id, name);
            if (_preparedActorLabels.TryGetValue(key, out var label))
            {
                _preparedActorLabels.Remove(key);
                return label;
            }
            ActorLabelMissCount++;
            return name + " " + id;
        }

        /// <summary>Adopts the session before caller-budgeted preparation so the first draw preserves prepared objects.</summary>
        /// <param name="session">The owning simulation.</param>
        internal void PrepareSession(object session)
        {
            if (!Enabled)
            {
                return;
            }
            if (!ReferenceEquals(_session, session))
            {
                Clear();
                _session = session;
            }
            _preparedActorLabels.Clear();
            if (_root == null)
            {
                _root = CreateObservedNode(null, "PvZ Actors");
            }
            if (!_categories.ContainsKey("Plants"))
            {
                _categories.Add("Plants", CreateObservedNode(_root.transform, "Plants"));
            }
            if (!_categories.ContainsKey("Zombies"))
            {
                _categories.Add("Zombies", CreateObservedNode(_root.transform, "Zombies"));
            }
        }

        /// <summary>Creates at most the requested number of reusable inspection objects.</summary>
        /// <param name="actorCount">The desired live plus inactive actor capacity.</param>
        /// <param name="spriteCount">The desired live plus inactive source-node capacity.</param>
        /// <param name="maxObjects">The maximum number of Unity objects to create in this call.</param>
        /// <returns>Whether the bounded requested capacity is available.</returns>
        internal bool PrepareCapacityStep(int actorCount, int spriteCount, int maxObjects)
        {
            if (!Enabled)
            {
                return true;
            }
            _actors.EnsureCapacity(Math.Max(_actors.Count, actorCount));
            if (_retired.Capacity < actorCount)
            {
                _retired.Capacity = actorCount;
            }
            var liveSprites = 0;
            foreach (var actor in _actors.Values)
            {
                liveSprites += actor.Sprites.Count;
            }
            var actors = Math.Min(Math.Max(0, actorCount - _actors.Count), MaximumInactiveActors);
            var sprites = Math.Min(Math.Max(0, spriteCount - liveSprites), MaximumInactiveSprites);
            var remaining = Math.Max(1, maxObjects);
            while (remaining > 0 && _inactiveActors.Count < actors)
            {
                var actor = new Actor(this, PoolRoot(), "Prepared actor");
                ActorRootCreationCount++;
                actor.Root.GetComponent<SpriteHierarchyVisibility>().Bind(null, 0);
                actor.Root.SetActive(false);
                actor.Sprites.EnsureCapacity(64);
                actor.TrackTables.EnsureCapacity(4);
                actor.Attachments.EnsureCapacity(4);
                if (_inactiveTrackTables.Count < 1024)
                {
                    _inactiveTrackTables.Push(new TrackNodes());
                }
                _inactiveActors.Push(actor);
                remaining--;
            }
            while (remaining > 0 && _inactiveSprites.Count < sprites)
            {
                PrepareSpriteNodes(1);
                remaining--;
            }
            var attachments = Math.Min(Math.Max(0, actorCount * 3), MaximumInactiveAttachments);
            while (remaining > 0 && _inactiveAttachments.Count < attachments)
            {
                var attachment = CreateObservedNode(PoolRoot(), "Prepared attachment");
                attachment.GetComponent<SpriteHierarchyVisibility>().Bind(null, 0);
                attachment.SetActive(false);
                _inactiveAttachments.Push(attachment);
                remaining--;
            }
            return _inactiveActors.Count >= actors && _inactiveSprites.Count >= sprites
                && _inactiveAttachments.Count >= attachments;
        }

        /// <summary>Creates reusable source nodes during a caller-budgeted preparation step.</summary>
        /// <param name="count">The number of additional source nodes to prepare.</param>
        internal void PrepareSpriteNodes(int count)
        {
            if (!Enabled)
            {
                return;
            }
            for (var index = 0; index < count && _inactiveSprites.Count < MaximumInactiveSprites; index++)
            {
                var node = new Node(this, PoolRoot(), "Prepared sprite");
                node.Metadata.BindVisibility(null, 0);
                node.Object.SetActive(false);
                _inactiveSprites.Push(node);
            }
        }

        /// <summary>Destroys a logical hierarchy object using the current Unity lifetime mode.</summary>
        /// <param name="node">The node whose logical lifetime ended.</param>
        private static void DestroyNode(GameObject node)
        {
            node.SetActive(false);
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(node);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(node);
            }
        }
    }
}
