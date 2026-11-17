#nullable enable
using System;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Identifies live presentation state without formatting entity identifiers into strings.</summary>
    internal readonly struct PresentationIdentity : IEquatable<PresentationIdentity>
    {
        /// <summary>The immutable caller role or legacy external identity.</summary>
        private readonly string? _role;
        /// <summary>The optional derived composition role for legacy identities.</summary>
        private readonly string? _derivedRole;
        /// <summary>The simulation identity within the owning session.</summary>
        private readonly int _entity;
        /// <summary>The original optional source-part identity.</summary>
        private readonly int _part;
        /// <summary>The attached playback slot, independent of the source-part identity.</summary>
        private readonly int _attachment;
        /// <summary>Separates numeric identities belonging to different matches.</summary>
        private readonly uint _generation;
        /// <summary>Distinguishes external string identities from numeric production identities.</summary>
        private readonly bool _numeric;

        /// <summary>Creates a complete presentation identity.</summary>
        /// <param name="role">The immutable role or external key.</param>
        /// <param name="entity">The logical entity.</param>
        /// <param name="part">The original source part.</param>
        /// <param name="attachment">The attached playback slot.</param>
        /// <param name="generation">The simulation generation.</param>
        /// <param name="numeric">Whether the identity uses numeric production fields.</param>
        /// <param name="derivedRole">The optional legacy composition role.</param>
        private PresentationIdentity(string? role, int entity, int part, int attachment, uint generation, bool numeric, string? derivedRole = null)
        {
            _derivedRole = derivedRole;
            _role = role;
            _entity = entity;
            _part = part;
            _attachment = attachment;
            _generation = generation;
            _numeric = numeric;
        }

        /// <summary>Gets whether no playback or surface identity was supplied.</summary>
        internal bool IsEmpty => string.IsNullOrEmpty(_role);

        /// <summary>Creates an allocation-free numeric identity in the current session.</summary>
        /// <param name="role">The retained immutable role.</param>
        /// <param name="entity">The model identifier.</param>
        /// <param name="part">The optional model part.</param>
        /// <param name="generation">The owning match generation.</param>
        /// <returns>The value identity.</returns>
        internal static PresentationIdentity Create(string role, int entity, int part, uint generation)
        {
            return new PresentationIdentity(role, entity, part, -1, generation, true);
        }

        /// <summary>Adopts a legacy key without interning, copying, or changing ordinal equality.</summary>
        /// <param name="key">The existing external key.</param>
        /// <returns>The equivalent external identity.</returns>
        internal static PresentationIdentity FromExternal(string? key)
        {
            return new PresentationIdentity(key, 0, 0, -1, 0, false);
        }

        /// <summary>Separates an attached playback stream without concatenating a child key.</summary>
        /// <param name="attachment">The attached playback index.</param>
        /// <returns>The derived stream identity.</returns>
        internal PresentationIdentity WithAttachment(int attachment)
        {
            return _numeric
                ? new PresentationIdentity(_role, _entity, _part, attachment, _generation, true, _derivedRole)
                : FromExternal(_role + "-head-" + attachment);
        }

        /// <summary>Derives a numeric composition role while retaining legacy caller identity isolation.</summary>
        /// <param name="role">The immutable composition role.</param>
        /// <returns>The role-specific identity.</returns>
        internal PresentationIdentity WithRole(string role)
        {
            return _numeric
                ? new PresentationIdentity(_role, _entity, _part, _attachment, _generation, true, role)
                : FromExternal(role + _role);
        }

        /// <summary>Compares complete logical identities with ordinal role equality.</summary>
        /// <param name="other">The identity to compare.</param>
        /// <returns>Whether every identity field matches.</returns>
        public bool Equals(PresentationIdentity other)
        {
            return _entity == other._entity && _part == other._part && _attachment == other._attachment
                && _generation == other._generation && _numeric == other._numeric
                && string.Equals(_role, other._role, StringComparison.Ordinal)
                && string.Equals(_derivedRole, other._derivedRole, StringComparison.Ordinal);
        }

        /// <summary>Compares a boxed identity for collection compatibility.</summary>
        /// <param name="obj">The comparison value.</param>
        /// <returns>Whether the value is an equal presentation identity.</returns>
        public override bool Equals(object? obj)
        {
            return obj is PresentationIdentity other && Equals(other);
        }

        /// <summary>Hashes all value fields without boxing.</summary>
        /// <returns>The dictionary hash.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = _role == null ? 0 : StringComparer.Ordinal.GetHashCode(_role);
                hash = hash * 397 ^ (_derivedRole == null ? 0 : StringComparer.Ordinal.GetHashCode(_derivedRole));
                hash = hash * 397 ^ _entity;
                hash = hash * 397 ^ _part;
                hash = hash * 397 ^ _attachment;
                hash = hash * 397 ^ (int)_generation;
                return hash * 397 ^ (_numeric ? 1 : 0);
            }
        }

        /// <summary>Adopts a caller-owned legacy key without allocating.</summary>
        /// <param name="key">The external identity.</param>
        /// <returns>The value identity wrapping that string.</returns>
        public static implicit operator PresentationIdentity(string? key)
        {
            return FromExternal(key);
        }
    }
}
