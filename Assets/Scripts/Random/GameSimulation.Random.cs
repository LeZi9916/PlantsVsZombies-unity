using System;
using PvZReplica.Randomness;
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        public ulong BattleRandomDrawCount
        { get { var original = random as OriginalRandomAdapter; return original == null ? 0 : original.Generator.DrawCount; } }
        public OriginalRandomState CaptureBattleRandomState()
        {
            var original = random as OriginalRandomAdapter;
            if (original == null) throw new InvalidOperationException("GameSimulation must use OriginalRandomAdapter.");
            return original.CaptureState();
        }
        public void RestoreBattleRandomState(OriginalRandomState snapshot)
        {
            var original = random as OriginalRandomAdapter;
            if (original == null) throw new InvalidOperationException("GameSimulation must use OriginalRandomAdapter.");
            original.RestoreState(snapshot);
        }
    }
}
