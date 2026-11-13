#nullable enable
using System.Collections.Generic;

namespace PlantsVsZombies.Game
{
    /// <summary>Reuses actor keys while retaining only the current and preceding presentation frames.</summary>
    internal sealed class PresentationKeyCache
    {
        /// <summary>The keys requested during the current frame.</summary>
        private Dictionary<(string Prefix, int Id, int Part), string> _current = new Dictionary<(string, int, int), string>();
        /// <summary>The preceding frame's keys available for reuse.</summary>
        private Dictionary<(string Prefix, int Id, int Part), string> _previous = new Dictionary<(string, int, int), string>();

        /// <summary>Retires older keys and starts collecting the next frame's live identities.</summary>
        internal void BeginFrame()
        {
            var reusable = _previous;
            _previous = _current;
            _current = reusable;
            _current.Clear();
        }

        /// <summary>Releases all retained identities when their presentation lifetime ends.</summary>
        internal void Clear()
        {
            _current.Clear();
            _previous.Clear();
        }

        /// <summary>Gets a stable string for a live actor or effect without formatting it again.</summary>
        /// <param name="prefix">The renderer's existing identity prefix.</param>
        /// <param name="id">The simulation identity.</param>
        /// <param name="part">An optional attachment or effect index.</param>
        /// <returns>The existing key, or a newly formatted key for a newly visible identity.</returns>
        internal string Get(string prefix, int id, int part = -1)
        {
            var identity = (prefix, id, part);
            if (_current.TryGetValue(identity, out var key))
            {
                return key;
            }

            if (!_previous.TryGetValue(identity, out key))
            {
                key = part < 0 ? prefix + id : prefix + id + "-" + part;
            }

            _current.Add(identity, key);
            return key;
        }
    }
}
