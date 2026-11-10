using System;

namespace PvZReplica.Profiles
{
    /// <summary>An immutable snapshot in the replica's own, non-original format.</summary>
    public sealed class UserProfile
    {
        internal UserProfile(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public Guid Id { get; }
        public string Name { get; }

        // No game progress is implemented or imported by this catalog.
        public int AdventureLevel { get { return 1; } }
    }
}
