#nullable enable
using System;
using UnityEngine;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Restores the actor and sprite association after a synchronous nested drawing operation.</summary>
    internal readonly struct SpriteHierarchyScope : IDisposable
    {
        /// <summary>The registry owning the saved context.</summary>
        private readonly SpriteHierarchy? _owner;
        /// <summary>The preceding opaque actor record.</summary>
        private readonly object? _actor;
        /// <summary>The preceding attachment slot.</summary>
        private readonly int _attachment;
        /// <summary>The preceding source sprite node.</summary>
        private readonly Transform? _sprite;
        /// <summary>The preceding cached source object.</summary>
        private readonly GameObject? _spriteObject;
        /// <summary>The preceding cached pose inspector.</summary>
        private readonly SpriteHierarchySprite? _metadata;

        /// <summary>Creates an allocation-free nested hierarchy scope.</summary>
        /// <param name="owner">The registry restoring the context.</param>
        /// <param name="actor">The preceding actor record.</param>
        /// <param name="attachment">The preceding attachment slot.</param>
        /// <param name="sprite">The preceding source sprite node.</param>
        /// <param name="spriteObject">The preceding cached source object.</param>
        /// <param name="metadata">The preceding source pose inspector.</param>
        internal SpriteHierarchyScope(SpriteHierarchy owner, object? actor, int attachment, Transform? sprite, GameObject? spriteObject, SpriteHierarchySprite? metadata)
        {
            _owner = owner;
            _actor = actor;
            _attachment = attachment;
            _sprite = sprite;
            _spriteObject = spriteObject;
            _metadata = metadata;
        }

        /// <summary>Restores the context captured before this synchronous drawing operation.</summary>
        public void Dispose()
        {
            _owner?.Restore(_actor, _attachment, _sprite, _spriteObject, _metadata);
        }
    }
}
