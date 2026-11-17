#nullable enable
using PlantsVsZombies.Rendering;

namespace PlantsVsZombies.Game
{
    /// <summary>Produces value identities without retaining or formatting entity keys.</summary>
    internal sealed class PresentationKeyCache
    {
        /// <summary>The match whose numeric identities are currently active.</summary>
        private object? _session;
        /// <summary>The generation distinguishing identifiers reused by another match.</summary>
        private uint _generation;

        /// <summary>Begins a presentation without allocating key collection storage.</summary>
        internal void BeginFrame()
        {
        }

        /// <summary>Changes the generation only when the owning simulation changes.</summary>
        /// <param name="session">The current simulation or standalone presenter owner.</param>
        internal void SetSession(object session)
        {
            if (!ReferenceEquals(_session, session))
            {
                _session = session;
                _generation++;
            }
        }

        /// <summary>Retires all identities when the presentation session ends.</summary>
        internal void Clear()
        {
            _session = null;
            _generation++;
        }

        /// <summary>Gets an allocation-free role, entity, and part identity.</summary>
        /// <param name="prefix">The existing immutable role identifier.</param>
        /// <param name="id">The model identifier.</param>
        /// <param name="part">The optional model part.</param>
        /// <returns>The session-scoped value identity.</returns>
        internal PresentationIdentity Get(string prefix, int id, int part = -1)
        {
            return PresentationIdentity.Create(prefix, id, part, _generation);
        }
    }
}
