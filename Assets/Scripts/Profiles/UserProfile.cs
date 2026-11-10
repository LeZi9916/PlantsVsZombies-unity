#nullable enable
using System;

namespace PlantsVsZombies.Profiles
{
    /// <summary>Provides an immutable identity and display-name snapshot from the local profile catalog.</summary>
    public sealed class UserProfile
    {
        /// <summary>Creates a snapshot after the catalog has validated its identity and display name.</summary>
        /// <param name="id">The persistent, nonempty profile identifier.</param>
        /// <param name="name">The normalized display name.</param>
        internal UserProfile(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>Gets the persistent identifier shared by all snapshots of this profile.</summary>
        public Guid Id { get; }
        /// <summary>Gets the display name captured when this snapshot was created.</summary>
        public string Name { get; }

        // No game progress is implemented or imported by this catalog.
        /// <summary>Gets the legacy catalog placeholder level; campaign progress is stored separately.</summary>
        public int AdventureLevel
        {
            get
            {
                return 1;
            }
        }
    }
}
