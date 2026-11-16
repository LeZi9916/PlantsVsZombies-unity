#nullable enable
using System;
using PlantsVsZombies.Simulation;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Preserves the full board graph and its original file baselines across editor recompilation.</summary>
    [Serializable]
    internal sealed class MatchCheckpointEditorDraft
    {
        /// <summary>The directory owning this draft.</summary>
        [field: SerializeField]
        internal string DirectoryPath { get; private set; } = string.Empty;
        /// <summary>The serialized profile identity.</summary>
        [SerializeField]
        private string _profileId = string.Empty;
        /// <summary>The serialized challenge receipt identity, or empty for adventure.</summary>
        [SerializeField]
        private string _runId = string.Empty;
        /// <summary>Gets the owning profile.</summary>
        internal Guid ProfileId => Guid.ParseExact(_profileId, "N");
        /// <summary>Gets the challenge run identity, or null for adventure.</summary>
        internal Guid? RunId => _runId.Length == 0 ? (Guid?)null : Guid.ParseExact(_runId, "N");
        /// <summary>The selected native mode.</summary>
        [field: SerializeField]
        internal int Mode { get; private set; }
        /// <summary>The original level identity.</summary>
        [field: SerializeField]
        internal int Level { get; private set; }
        /// <summary>The original adventure cycle.</summary>
        [field: SerializeField]
        internal int Cycle { get; private set; }
        /// <summary>The unchanged currency receipt identity.</summary>
        [field: SerializeField]
        internal string Receipt { get; private set; } = string.Empty;
        /// <summary>The complete serialized board draft.</summary>
        [field: SerializeField]
        internal byte[] Payload { get; private set; } = Array.Empty<byte>();
        /// <summary>The exact catalog baseline accepted before editing.</summary>
        [field: SerializeField]
        internal byte[] CatalogBytes { get; private set; } = Array.Empty<byte>();
        /// <summary>The exact checkpoint baseline accepted before editing.</summary>
        [field: SerializeField]
        internal byte[] CheckpointBytes { get; private set; } = Array.Empty<byte>();
        /// <summary>Whether an external change already made this draft stale.</summary>
        [field: SerializeField]
        internal bool Stale { get; private set; }

        /// <summary>Captures offline edits and their unchanged original baselines without touching disk.</summary>
        /// <param name="session">The loaded board session.</param>
        /// <returns>A window-serializable full board snapshot.</returns>
        /// <exception cref="InvalidOperationException">The session has no board draft.</exception>
        internal static MatchCheckpointEditorDraft Capture(MatchCheckpointEditorSession session)
        {
            var game = session.Game ?? throw new InvalidOperationException("No checkpoint draft is loaded.");
            return new MatchCheckpointEditorDraft
            {
                DirectoryPath = session.DirectoryPath,
                _profileId = session.SelectedProfileId.ToString("N"),
                _runId = session.OriginalRunId?.ToString("N") ?? string.Empty,
                Mode = session.SelectedModeId,
                Level = session.OriginalLevel,
                Cycle = session.OriginalCycle,
                Receipt = session.Receipt,
                Payload = game.CaptureCheckpoint(),
                CatalogBytes = session.CatalogBytes,
                CheckpointBytes = session.CheckpointBytes,
                Stale = session.HasStaleDraft
            };
        }
    }
}
