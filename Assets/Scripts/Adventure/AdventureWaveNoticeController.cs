#nullable enable
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Waves;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Tracks the huge-wave and final-wave banners shown for the active adventure board.</summary>
    internal sealed class AdventureWaveNoticeController
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose simulation raises the wave notices.</param>
        internal AdventureWaveNoticeController(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores wave notice game.</summary>
        internal GameSimulation? _waveNoticeGame;
        /// <summary>Stores huge wave notice start,  final wave notice start.</summary>
        internal float _hugeWaveNoticeStart = -1f, _finalWaveNoticeStart = -1f;
        /// <summary>Stores bundled wave animations.</summary>
        internal global::PlantsVsZombies.Reanimation.ReanimationRenderer? _bundledWaveAnimations;
        /// <summary>Performs initialize adventure wave notices.</summary>
        /// <param name="isRestored">Whether banner origins are retained from the saved board.</param>
        internal void InitializeAdventureWaveNotices(bool isRestored = false)
        {
            if (_waveNoticeGame != null)
            {
                _waveNoticeGame.WaveScheduleNotification -= HandleAdventureWaveNotice;
            }

            _waveNoticeGame = _owner._game;
            _hugeWaveNoticeStart = _finalWaveNoticeStart = -1f;
            if (_waveNoticeGame != null)
            {
                if (isRestored)
                {
                    _hugeWaveNoticeStart = _waveNoticeGame.Presentation.HugeWaveNoticeStart;
                    _finalWaveNoticeStart = _waveNoticeGame.Presentation.FinalWaveNoticeStart;
                }
                _waveNoticeGame.WaveScheduleNotification += HandleAdventureWaveNotice;
            }
        }

        /// <summary>Performs handle adventure wave notice.</summary>
        /// <param name="kind">The native wave event kind consumed synchronously.</param>
        private void HandleAdventureWaveNotice(WaveScheduleEventKind kind)
        {
            if (_waveNoticeGame != _owner._game)
            {
                return;
            }

            if (kind == WaveScheduleEventKind.HugeWaveWarning)
            {
                _hugeWaveNoticeStart = _owner.RequiredGame.Elapsed;
            }
            else if (kind == WaveScheduleEventKind.HugeWaveCleared)
            {
                _hugeWaveNoticeStart = -1f;
            }
            else if (kind == WaveScheduleEventKind.FinalWaveApproaching)
            {
                _finalWaveNoticeStart = _owner.RequiredGame.Elapsed;
            }
        }

        /// <summary>Performs dispose adventure wave notices.</summary>
        internal void DisposeAdventureWaveNotices()
        {
            if (_waveNoticeGame != null)
            {
                _waveNoticeGame.WaveScheduleNotification -= HandleAdventureWaveNotice;
            }

            _waveNoticeGame = null;
            if (_bundledWaveAnimations != null)
            {
                _bundledWaveAnimations.Dispose();
                _bundledWaveAnimations = null;
            }
        }
    }
}
