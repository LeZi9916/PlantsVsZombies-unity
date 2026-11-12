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
        internal GameSimulation _waveNoticeGame;
        /// <summary>Stores huge wave notice start,  final wave notice start.</summary>
        internal float _hugeWaveNoticeStart = -1f, _finalWaveNoticeStart = -1f;
        /// <summary>Stores bundled wave animations.</summary>
        internal global::PlantsVsZombies.Reanimation.ReanimationRenderer _bundledWaveAnimations;
        /// <summary>Performs initialize adventure wave notices.</summary>
        internal void InitializeAdventureWaveNotices()
        {
            if (_waveNoticeGame != null)
            {
                _waveNoticeGame.WaveScheduleEventRaised -= HandleAdventureWaveNotice;
            }

            _waveNoticeGame = _owner._game;
            _hugeWaveNoticeStart = _finalWaveNoticeStart = -1f;
            if (_waveNoticeGame != null)
            {
                _waveNoticeGame.WaveScheduleEventRaised += HandleAdventureWaveNotice;
            }
        }

        /// <summary>Performs handle adventure wave notice.</summary>
        /// <param name = "e">The e.</param>
        private void HandleAdventureWaveNotice(WaveScheduleEvent e)
        {
            if (_waveNoticeGame != _owner._game)
            {
                return;
            }

            if (e.Kind == WaveScheduleEventKind.HugeWaveWarning)
            {
                _hugeWaveNoticeStart = _owner._game.Elapsed;
            }
            else if (e.Kind == WaveScheduleEventKind.HugeWaveCleared)
            {
                _hugeWaveNoticeStart = -1f;
            }
            else if (e.Kind == WaveScheduleEventKind.FinalWaveApproaching)
            {
                _finalWaveNoticeStart = _owner._game.Elapsed;
            }
        }

        /// <summary>Performs dispose adventure wave notices.</summary>
        internal void DisposeAdventureWaveNotices()
        {
            if (_waveNoticeGame != null)
            {
                _waveNoticeGame.WaveScheduleEventRaised -= HandleAdventureWaveNotice;
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
