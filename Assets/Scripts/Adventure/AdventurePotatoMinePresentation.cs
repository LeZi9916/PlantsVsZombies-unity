using System;
namespace PvZReplica.Adventure
{
    public sealed class AdventurePotatoMineState
    {
        public int Countdown = 1500, PhaseTicks, GlowFrames = 10, NearestDistancePixels = 1000;
        public float BodyRate = 12f, GlowTime;
        public bool LightCreated;
    }
    public sealed partial class AdventurePresentationState { public int PotatoMineRiseSoundSerial; }
}
namespace PvZReplica
{
    public sealed partial class Plant
    { public PvZReplica.Adventure.AdventurePotatoMineState PotatoMinePresentation { get; internal set; } }
    public sealed partial class GameSimulation
    {
        private void InitializeAdventurePotatoMine(Plant p)
        {
            if (p.PotatoMinePresentation != null) return;
            p.PotatoMinePresentation = new PvZReplica.Adventure.AdventurePotatoMineState();
            p.PresentationKind = PlantKind.PotatoMine; p.PresentationRate = 12f;
            p.ActionTimer = 15f;
        }
        private bool AdvanceAdventurePotatoMine(Plant p)
        {
            InitializeAdventurePotatoMine(p);
            var state = p.PotatoMinePresentation;
            switch (p.MinePhase)
            {
                case MinePhase.Buried:
                    if (state.Countdown > 0) state.Countdown--;
                    p.ActionTimer = state.Countdown * .01f;
                    if (state.Countdown == 0)
                    {
                        p.MinePhase = MinePhase.Rising; p.PhaseAge = 0; state.PhaseTicks = 0;
                        state.BodyRate = 18f; p.PresentationRate = 18f;
                        AdventurePresentationPotatoMineRise(p); Presentation.PotatoMineRiseSoundSerial++;
                    }
                    break;
                case MinePhase.Rising:
                    state.PhaseTicks++;
                    // The local reanim's anim_rise is 19 frames at native 18 fps.
                    if (state.PhaseTicks * 18 >= 1900)
                    {
                        p.MinePhase = MinePhase.Armed; p.PhaseAge = 0; state.PhaseTicks = 0;
                        state.BodyRate = 12f + AdventureExplosionRandomFloat(3f);
                        p.PresentationRate = state.BodyRate; state.LightCreated = true;
                        state.GlowFrames = 10; state.GlowTime = 0; p.ActionTimer = 0;
                        random.Next(400); // PC initializes blink countdown as 400+Rand(400).
                    }
                    break;
                case MinePhase.Armed:
                    state.PhaseTicks++;
                    bool touching;
                    int gap = AdventurePotatoMineDistance(p, out touching);
                    state.NearestDistancePixels = gap;
                    if (touching)
                    {
                        float x = p.Column + .5f;
                        BlastAdventure(x,p.Row,.8f,0,1800f,false,0x12);
                        AdventurePresentationExplosion(PlantKind.PotatoMine,x,p.Row);
                        RemoveAdventurePlantAndStack(p); Raise(GameEvent.MineExplosion); return true;
                    }
                    if (state.LightCreated)
                    {
                        // PC 46E405..46E423 -> curve(200,50,gap,10,3,Linear), rounded.
                        float t = Math.Max(0f, Math.Min(1f, (gap - 200f) / -150f));
                        state.GlowFrames = (int)(10f - 7f * t + .5f);
                        state.GlowTime += .01f * (state.BodyRate - 2f) / state.GlowFrames;
                        state.GlowTime -= (float)Math.Floor(state.GlowTime);
                    }
                    break;
            }
            return true;
        }
        private int AdventurePotatoMineDistance(Plant p, out bool touching)
        {
            touching = false;
            // PC 475D90 uses horizontal GetRectOverlap, clamped to a nonnegative gap.
            // Mine attack rect (PC Plant::GetPlantAttackRect): x=mX, width=80-25.
            float left = 40f + p.Column * 80f, right = left + 55f;
            int closest = 1000;
            foreach (var z in Zombies)
            {
                if (!PvZReplica.Adventure.AdventureZombieTargetingRules.CanBeAutoTargeted(z,PlantKind.PotatoMine) || z.Hypnotized || z.Airborne || z.Kind == ZombieKind.Bungee || z.Kind == ZombieKind.Boss || z.Row != p.Row || z.DropRemaining > 0) continue;
                float zl, zr; AdventurePotatoMineZombieBounds(z, out zl, out zr);
                float overlap = Math.Min(right, zr) - Math.Max(left, zl);
                if (overlap > 0) touching = true;
                int gap = Math.Max(0, (int)-overlap);
                if (gap < closest) closest = gap;
            }
            return closest;
        }
        private static void AdventurePotatoMineZombieBounds(Zombie z, out float left, out float right)
        {
            // Literal PC ZombieInitialize body rectangles (53DD90), same coordinates
            // used by the native corpse/reward placement. No sprite bounds or fitted size.
            float scale = z.IsSmall ? .5f : 1f;
            float center = 40f + z.X * 80f;
            float source = center - 57f * scale - 20f * (1f - scale);
            int x = 36, width = 42;
            switch (z.Kind)
            {
                case ZombieKind.Football: x = 50; width = 57; break;
                case ZombieKind.Digger: x = 50; width = 28; break;
                case ZombieKind.Gargantuar: x = -17; width = 125; break;
                case ZombieKind.Zomboni: case ZombieKind.Catapult: x = 0; width = 153; break;
                case ZombieKind.Snorkel: x = 12; width = 62; break;
                case ZombieKind.Bobsled: if (z.PresentationHasSled ?? !z.UsedAbility) { x = -50; width = 275; } break;
            }
            bool backwards = z.PresentationWalkingBackwards ?? (z.Kind == ZombieKind.Digger && z.AbilityPhase >= 1 && z.PresentationNativePhase != 38);
            if (backwards) x = 120 - x - width;
            left = (int)source + x * scale + 20f * (1f - scale); right = left + width * scale;
        }
    }
}
