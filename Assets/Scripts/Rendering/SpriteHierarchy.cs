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
            /// <summary>The small attachment groups separating repeated body and head artwork in the hierarchy.</summary>
            internal readonly Dictionary<int, GameObject> Attachments = new Dictionary<int, GameObject>();
            /// <summary>The readable species name most recently associated with this logical identity.</summary>
            internal string DisplayName;
            /// <summary>The most recent frame in which the simulation retained this actor.</summary>
            internal int AliveFrame;
            /// <summary>The actor visibility observed during the preceding presentation.</summary>
            internal bool Visible = true;

            /// <summary>Creates a logical actor below its category.</summary>
            /// <param name="parent">The category containing the actor.</param>
            /// <param name="name">The readable actor identity.</param>
            internal Actor(Transform parent, string name)
            {
                Root = CreateNode(parent, name);
                RootTransform = Root.transform;
                DisplayName = name;
            }
        }

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
            /// <param name="parent">The actor owning this sprite.</param>
            /// <param name="name">The original track or attachment name.</param>
            internal Node(Transform parent, string name)
            {
                Object = CreateNode(parent, name);
                Transform = Object.transform;
                Metadata = Object.AddComponent<SpriteHierarchySprite>();
            }
        }

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
            using var profile = SpriteRenderingProfiler.Measure(SpriteRenderingProfiler.Hierarchy);
            if (!ReferenceEquals(_session, session))
            {
                Clear();
                _session = session;
            }

            _frame++;
            RefreshVisibility();
        }

        /// <summary>Retains an actor independently of whether it is visible or submits any artwork.</summary>
        /// <param name="category">The model category, such as Plants or Zombies.</param>
        /// <param name="id">The simulation identity within the current session.</param>
        /// <param name="displayName">The readable species name used when first binding the actor.</param>
        internal void MarkAlive(string category, int id, string displayName)
        {
            GetActor(category, id, displayName).AliveFrame = _frame;
        }

        /// <summary>Begins a synchronous actor drawing context without changing its geometry or painter order.</summary>
        /// <param name="category">The logical model category.</param>
        /// <param name="id">The stable simulation identity.</param>
        /// <param name="displayName">The readable species name.</param>
        /// <returns>A scope restoring the preceding actor and attachment context.</returns>
        internal SpriteHierarchyScope BeginActor(string category, int id, string displayName)
        {
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
            var scope = SaveContext();
            if (_actor != null)
            {
                var key = (asset, trackIndex, _attachment);
                if (!_actor.Sprites.TryGetValue(key, out var node))
                {
                    var parent = _actor.Root;
                    if (_attachment != 0 && !_actor.Attachments.TryGetValue(_attachment, out parent))
                    {
                        var attachmentName = _attachment == 10 ? "Flag"
                            : _attachment == 11 ? "Held Item" : _attachment == 12 ? "Plant Head" : "Head " + _attachment;
                        parent = CreateNode(_actor.RootTransform, attachmentName);
                        _actor.Attachments.Add(_attachment, parent);
                    }

                    node = new Node(parent.transform, displayName);
                    _actor.Sprites.Add(key, node);
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
        internal static void RecordCurrentSprite(SpriteImage image, float width, float height, Rect uv, Matrix4x4 matrix, Color color, int mode)
        {
            if (s_currentMetadata != null)
            {
                s_currentMetadata.Record(image, width, height, uv, matrix, color, mode);
            }
        }

        /// <summary>Removes actors absent from both the live simulation and retained corpse list.</summary>
        internal void EndFrame()
        {
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
                DestroyNode(_actors[key].Root);
                _actors.Remove(key);
            }

            _retired.Clear();
        }

        /// <summary>Detects external hierarchy switches without modifying the user's active state.</summary>
        internal void RefreshVisibility()
        {
            foreach (var actor in _actors.Values)
            {
                var visible = actor.Root != null && actor.Root.activeInHierarchy;
                if (actor.Visible != visible)
                {
                    actor.Visible = visible;
                    VisibilityRevision++;
                }

                foreach (var node in actor.Sprites.Values)
                {
                    visible = node.Object != null && node.Object.activeInHierarchy;
                    if (node.Visible != visible)
                    {
                        node.Visible = visible;
                        VisibilityRevision++;
                    }
                }
            }
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
                    actor.Root.name = displayName + " " + id;
                }

                return actor;
            }

            if (_root == null)
            {
                _root = CreateNode(null, "PvZ Actors");
            }

            if (!_categories.TryGetValue(category, out var parent))
            {
                parent = CreateNode(_root.transform, category);
                _categories.Add(category, parent);
            }

            actor = new Actor(parent.transform, displayName + " " + id);
            actor.DisplayName = displayName;
            _actors.Add(key, actor);
            return actor;
        }

        /// <summary>Clears actor bindings when a simulation session or renderer ends.</summary>
        private void Clear()
        {
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
        /// <returns>The newly created node.</returns>
        private static GameObject CreateNode(Transform? parent, string name)
        {
            var node = new GameObject(name)
            {
                hideFlags = HideFlags.DontSave,
                layer = 31
            };
            node.transform.SetParent(parent, false);
            return node;
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
