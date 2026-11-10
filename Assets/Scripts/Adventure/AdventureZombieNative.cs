using System;

namespace PvZReplica.Adventure
{
    // PC 0x53DD90 / 0x540500 / 0x54ADC0. Velocities are pixels per 100 Hz tick.
    public static class AdventureZombieNativeRules
    {
        // Host origin = (40 + 80*grid - 57*scale). PC body origin = mPosX + 15 + 30*(1-scale).
        public static float NativePositionX(float gridX, float scale = 1f) { return 80f * gridX - 5f - 27f * scale; }
        public static float GridX(float nativePositionX, float scale = 1f) { return (nativePositionX + 5f + 27f * scale) / 80f; }
        public static float ReanimationSourceX(float gridX, float scale = 1f) { return 20f + 80f * gridX - 37f * scale; }
        public static float GridSpeed(float pixelsPerTick) { return pixelsPerTick * 100f / 80f; }
        public static float ZamboniVelocity(float nativeX, float previous)
        {
            // PC 0x5464A0 truncates mPosX before the 700..300 linear calculation,
            // and retains the prior velocity once mPosX <= 400 (normal undamaged vehicle).
            if (nativeX <= 400f) return previous;
            float t = Math.Max(0f, Math.Min(1f, ((int)nativeX - 700) / -400f));
            return .25f - .2f * t;
        }
        public static float SpawnSourceX(int kind, int random40, int random10)
        {
            switch (kind)
            {
                case 1: case 33: return 800f;
                case 3: return 870f + random10;
                case 12: return 800f + random10;
                case 13: return 880f;
                case 22: return 825f + random10;
                case 23: case 32: return 845f + random10;
                default: return 780f + random40;
            }
        }
        public static float PickSpeed(int kind, int phase, bool iZombie, float randomUnit)
        {
            if (phase == 59) return .3f;
            if (phase == 37) return iZombie ? .23f : .12f;
            if (kind == 24 && iZombie) return .9f;
            if (phase == 91) return .8f;
            if (kind == 19) return .4f;
            if (kind == 8 || kind == 9 || kind == 18 || kind == 1 || kind == 33) return .45f;
            if (phase == 32 || phase == 11 || kind == 7 || kind == 11 || kind == 15) return .66f + .02f * randomUnit;
            if (phase == 76 || kind == 30) return .79f + .02f * randomUnit;
            if (phase == 31 || phase == 51 || phase == 56) return .89f + .02f * randomUnit;
            return .23f + .14f * randomUnit;
        }
    }
}
namespace PvZReplica
{
    public sealed partial class Zombie
    {
        public float PresentationNativeVelocity;
        internal int PresentationSpeedPhase = int.MinValue, PresentationSpeedKind = -1;
    }
    public sealed partial class GameSimulation
    {
        private bool AdventureZombieIsIZombie { get { return ModeSession != null && ModeSession.Definition.Mechanic == Modes.ModeMechanic.IZombie; } }
        private void PickAdventureZombieNativeSpeed(Zombie z, int phase)
        {
            int kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            bool iZombie = AdventureZombieIsIZombie;
            float low = PvZReplica.Adventure.AdventureZombieNativeRules.PickSpeed(kind, phase, iZombie, 0f);
            float high = PvZReplica.Adventure.AdventureZombieNativeRules.PickSpeed(kind, phase, iZombie, 1f);
            z.PresentationNativeVelocity = PvZReplica.Adventure.AdventureZombieNativeRules.PickSpeed(kind, phase, iZombie, low == high ? 0f : (float)random.NextDouble());
            z.PresentationSpeedPhase = phase; z.PresentationSpeedKind = kind;
            z.Speed = PvZReplica.Adventure.AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
            z.PresentationMovementRate = -1f; z.PresentationMovementAnimation = null;
        }
        private void InitializeAdventureZombieNative(Zombie z, bool rightEdge)
        {
            // Explicit grave/vase/IZombie/summoned positions remain at their requested world cell.
            if (rightEdge)
            {
                int r40 = random.Next(40), r10 = 0;
                if (z.Kind == ZombieKind.PoleVaulter || z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult || z.Kind == ZombieKind.Gargantuar) r10 = random.Next(10);
                z.X = PvZReplica.Adventure.AdventureZombieNativeRules.GridX(PvZReplica.Adventure.AdventureZombieNativeRules.SpawnSourceX((int)z.Kind, r40, r10), z.IsSmall ? .5f : 1f);
            }
            PickAdventureZombieNativeSpeed(z, AdventureZombieFeedbackPhase(z));
            // These assignments occur after the initial PickRandomSpeed in PC Init.
            if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility)
            { z.PresentationNativeVelocity = .6f; z.Speed = PvZReplica.Adventure.AdventureZombieNativeRules.GridSpeed(.6f); }
        }
        private void RefreshAdventureZombieNativeSpeed(Zombie z)
        {
            int phase = AdventureZombieFeedbackPhase(z);
            int kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            if (z.PresentationSpeedPhase != phase || z.PresentationSpeedKind != kind) PickAdventureZombieNativeSpeed(z, phase);
            if (z.Kind == ZombieKind.Zomboni)
            {
                float nativeX = PvZReplica.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, z.IsSmall ? .5f : 1f);
                z.PresentationNativeVelocity = PvZReplica.Adventure.AdventureZombieNativeRules.ZamboniVelocity(nativeX, z.PresentationNativeVelocity);
                z.Speed = PvZReplica.Adventure.AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
            }
        }
    }
}
