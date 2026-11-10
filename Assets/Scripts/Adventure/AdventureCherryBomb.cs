using System;
using System.Collections.Generic;

namespace PvZReplica.Adventure
{
    // Local PC: 46C07A..46C0A4 (10 + Rand(5)), 46C624..46C645
    // (anim_explode, PlayOnceAndHold, special=100, reverse-explosion Foley 57).
    public sealed class AdventureCherryBombState
    {
        public const int FuseTicks = 100;
        public const int ExplodeFrameCount = 14;
        public const string Animation = "anim_explode";
        public int Countdown { get; internal set; } = FuseTicks;
        public int AnimationTicks { get; internal set; }
        public float Rate { get; internal set; }
        public float NormalizedTime { get; internal set; }
        public float ShakeX { get; internal set; }
        public float ShakeY { get; internal set; }
        public bool Detonated { get; internal set; }

        public float SampleSeconds(float remainder)
        {
            if (Rate <= 0) return 0;
            // PC PlayOnceAndHold clamps mAnimTime to .999999, and samples
            // frameStart + mAnimTime * (frameCount - 1), not frameCount * time.
            double phase = Math.Min(.999999f, NormalizedTime + Math.Max(0f, remainder) * Rate / ExplodeFrameCount);
            return (float)(phase * ExplodeFrameCount / Rate);
        }
    }

    public struct AdventureCherryBombSound
    {
        public bool Detonation;
        public float JuicyPitchSteps;
    }
}

namespace PvZReplica
{
    public sealed partial class Plant
    {
        public PvZReplica.Adventure.AdventureCherryBombState CherryBombPresentation { get; internal set; }
    }

    public sealed partial class GameSimulation
    {
        private readonly Queue<PvZReplica.Adventure.AdventureCherryBombSound> adventureCherryFuseSounds = new Queue<PvZReplica.Adventure.AdventureCherryBombSound>();
        private readonly Queue<PvZReplica.Adventure.AdventureCherryBombSound> adventureCherryDetonationSounds = new Queue<PvZReplica.Adventure.AdventureCherryBombSound>();

        // Explicit notifications survive a headless tick or a host attaching before its first
        // Update. The host drains them at the corresponding existing Planted/Explosion event.
        public bool TryTakeAdventureCherryBombSound(GameEvent trigger, out PvZReplica.Adventure.AdventureCherryBombSound sound)
        {
            var queue = trigger == GameEvent.Planted ? adventureCherryFuseSounds :
                trigger == GameEvent.Explosion ? adventureCherryDetonationSounds : null;
            if (queue == null || queue.Count == 0) { sound = default(PvZReplica.Adventure.AdventureCherryBombSound); return false; }
            sound = queue.Dequeue(); return true;
        }

        // 5E7060 rounds after scaling the native 31-bit draw. NextDouble would round
        // a scale-1 result first, which changes some rate/shake/pitch results by one ULP.
        private float AdventureCherryRandomFloat(float range)
        { return (float)(random.Next() / 2147483647.0 * range); }

        private void InitializeAdventureCherryBomb(Plant plant)
        {
            if (plant.CherryBombPresentation != null) return;
            var state = new PvZReplica.Adventure.AdventureCherryBombState { Rate = 10f + AdventureCherryRandomFloat(5f) };
            plant.CherryBombPresentation = state;
            plant.PresentationKind = PlantKind.CherryBomb;
            plant.PresentationRate = state.Rate;
            plant.ActionTimer = PvZReplica.Adventure.AdventureCherryBombState.FuseTicks * .01f;
            // 52A5A0 selects the only original variation with Rand(1), even though its
            // choice is fixed. Keep that draw on the replayable simulation stream.
            random.Next(1);
            adventureCherryFuseSounds.Enqueue(new PvZReplica.Adventure.AdventureCherryBombSound());
        }

        private bool AdvanceAdventureCherryBomb(Plant plant)
        {
            InitializeAdventureCherryBomb(plant);
            var state = plant.CherryBombPresentation;
            if (state.Detonated) return true;
            // PC 471815..471825 decrements the special counter before testing zero.
            if (state.Countdown > 0) state.Countdown--;
            plant.ActionTimer = state.Countdown * .01f;
            if (state.Countdown == 0) DetonateAdventureCherryBomb(plant);
            // PC Update 47230D..472317 still calls Animate after DoSpecial/Die.
            // Animate 47321B..47324D consumes two Rand(2)-1 offsets, including tick 100.
            state.ShakeX = AdventureCherryRandomFloat(2f) - 1f;
            state.ShakeY = AdventureCherryRandomFloat(2f) - 1f;
            state.AnimationTicks++;
            state.NormalizedTime = Math.Min(.999999f, state.NormalizedTime + .01f * state.Rate / PvZReplica.Adventure.AdventureCherryBombState.ExplodeFrameCount);
            return true;
        }

        private void DetonateAdventureCherryBomb(Plant plant)
        {
            InitializeAdventureCherryBomb(plant);
            var state = plant.CherryBombPresentation;
            if (state.Detonated) return;
            state.Detonated = true; state.Countdown = 0; plant.ActionTimer = 0;
            // Local PC 474B83..474C32: Foley 55 (boom), then Foley 65 (juicy).
            // Foley table 74B3B8 gives pitch ranges 0 and 2 respectively.
            random.Next(1);
            float juicyPitch = AdventureCherryRandomFloat(2f);
            random.Next(1);
            adventureCherryDetonationSounds.Enqueue(new PvZReplica.Adventure.AdventureCherryBombSound { Detonation = true, JuicyPitchSteps = juicyPitch });
            Raise(GameEvent.Explosion);
            BlastAdventure(plant.Column + .5f, plant.Row, 1.5f, 1, 1800f, true);
            AdventurePresentationExplosion(PlantKind.CherryBomb, plant.Column + .5f, plant.Row);
            RemoveAdventurePlantAndStack(plant);
        }
    }
}
