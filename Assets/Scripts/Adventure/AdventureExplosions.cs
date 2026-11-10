using System;
using System.Collections.Generic;

namespace PvZReplica.Adventure
{
    public enum AdventureExplosionEffect { Powie, Doom, PotatoMine, PotatoMineRise }
    public sealed class AdventureExplosion
    {
        public int Id, Seed, Row, Ticks;
        public float X;
        public AdventureExplosionEffect Effect;
        public bool Overlay { get { return Effect != AdventureExplosionEffect.PotatoMine && Effect != AdventureExplosionEffect.PotatoMineRise; } }
        public string Particle { get { return Effect.ToString(); } }
        // Max SystemDuration in the corresponding unmodified local PC XML.
        public int DurationTicks { get { return Effect == AdventureExplosionEffect.Doom ? 400 : Effect == AdventureExplosionEffect.PotatoMineRise ? 120 : 200; } }
    }
    public sealed class AdventureJalapenoFlame
    {
        public float X, Rate, ScaleX;
        public int DoneTick = -1;
    }
    public sealed class AdventureJalapenoFire
    {
        public int Id, Row, Ticks;
        public readonly List<AdventureJalapenoFlame> Flames = new List<AdventureJalapenoFlame>();
    }
    public sealed partial class AdventurePresentationState
    {
        public readonly List<AdventureExplosion> Explosions = new List<AdventureExplosion>();
        public readonly List<AdventureJalapenoFire> JalapenoFires = new List<AdventureJalapenoFire>();
    }
}
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private void AdventurePresentationExplosion(PlantKind kind, float x, int row)
        {
            PvZReplica.Adventure.AdventureExplosionEffect effect;
            switch (kind)
            {
                case PlantKind.CherryBomb: case PlantKind.ExplodeONut: effect = PvZReplica.Adventure.AdventureExplosionEffect.Powie; break;
                case PlantKind.DoomShroom: effect = PvZReplica.Adventure.AdventureExplosionEffect.Doom; break;
                case PlantKind.PotatoMine: effect = PvZReplica.Adventure.AdventureExplosionEffect.PotatoMine; break;
                case PlantKind.Jalapeno: AdventurePresentationJalapenoFire(row); return;
                default: return; // Cob already creates BlastMark + PopcornSplash, not Powie.
            }
            Presentation.Explosions.Add(new PvZReplica.Adventure.AdventureExplosion {
                Id = ++Presentation.EffectId, Seed = random.Next(), Row = row, X = x, Effect = effect });
        }
        private void AdventurePresentationPotatoMineRise(Plant plant)
        {
            Presentation.Explosions.Add(new PvZReplica.Adventure.AdventureExplosion { Id = ++Presentation.EffectId, Seed = random.Next(), Row = plant.Row, X = plant.Column + .5f, Effect = PvZReplica.Adventure.AdventureExplosionEffect.PotatoMineRise });
        }
        private float AdventureExplosionRandomFloat(float range)
        { return (float)(random.Next() / 2147483647.0 * range); }
        private void AdventurePresentationJalapenoFire(int row)
        {
            // PC Board::DoFwoosh 429060: replace a previous row's active flame tracks,
            // twelve placements from x=10 to 760, original fire (reanim type 34).
            Presentation.JalapenoFires.RemoveAll(f => f.Row == row);
            var fire = new PvZReplica.Adventure.AdventureJalapenoFire { Id = ++Presentation.EffectId, Row = row };
            for (int i = 0; i < 12; i++)
            {
                float speed = (float)(AdventureExplosionRandomFloat(.59999996f) + .699999988079071);
                float scale = (float)(AdventureExplosionRandomFloat(.20000005f) + .8999999761581421);
                if (random.Next(2) == 0) scale = -scale;
                fire.Flames.Add(new PvZReplica.Adventure.AdventureJalapenoFlame {
                    X = (float)(750.0 * i / 11.0 + 10.0), Rate = 12f * speed, ScaleX = scale });
            }
            Presentation.JalapenoFires.Add(fire);
        }
        private void AdvanceAdventureExplosionPresentation()
        {
            for (int i = Presentation.Explosions.Count - 1; i >= 0; i--)
                if (++Presentation.Explosions[i].Ticks >= Presentation.Explosions[i].DurationTicks)
                    Presentation.Explosions.RemoveAt(i);
            foreach (var fire in Presentation.JalapenoFires)
            {
                fire.Ticks++;
                int remaining = Math.Max(0, 100 - fire.Ticks);
                // PC 429250 -> TodAnimateCurve(50,0,remaining,12,0,Linear).
                // 526F20 rounds positive results by +.5 then truncation.
                int stillFlaming = (int)(12.0 * Math.Min(50, remaining) / 50.0 + .5);
                for (int j = 0; j < 12 - stillFlaming; j++)
                    if (fire.Flames[j].DoneTick < 0) fire.Flames[j].DoneTick = fire.Ticks;
            }
            // anim_done has ten frames; PlayOnceFullLastFrame at the PC's fixed 15 fps.
            Presentation.JalapenoFires.RemoveAll(f => f.Flames.TrueForAll(p => p.DoneTick >= 0 && (f.Ticks - p.DoneTick) * .01f >= 10f / 15f));
        }
        private void HitAdventureExplosionZombie(Zombie zombie, float damage, bool burned, uint flags)
        {
            // PC 42954D uses BurnBody (54E920); Potato takes the separate damage(1800,0x12)
            // branch. BurnBody compares BODY health, so thick armor cannot absorb an ash kill.
            if (burned && damage == 1800f && zombie.Kind != ZombieKind.Boss && zombie.Health < 1800f)
            {
                zombie.PresentationDeath = PvZReplica.Adventure.AdventureZombieDeathPresentation.Burned;
                zombie.ArmorHealth = 0; zombie.ShieldHealth = 0;
                HitAdventureZombie(zombie, damage, true, 0x12);
            }
            else
            {
                HitAdventureZombie(zombie, damage, true, burned ? 0x12U : flags);
            }
        }
        // Hook for the modes owner: PC explosive bowling nut also calls Powie (not PotatoMine).
        private void AdventurePresentationExplodeONut(float x, int row)
        { AdventurePresentationExplosion(PlantKind.ExplodeONut, x, row); }
    }
}
