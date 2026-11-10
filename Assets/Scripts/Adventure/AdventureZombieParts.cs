using System;
using System.Collections.Generic;

namespace PvZReplica.Adventure
{
    public sealed class AdventureZombiePartDrop
    {
        public int Id, Seed, NativeKind, Row, ParticleId;
        public float X, Age, Scale, AnimationAge, Rate;
        public string Asset, Animation, Anchor, Image;
        public bool Backwards, Chilled, Hypnotized;
        public float Duration { get { return ParticleId==12||ParticleId==13||ParticleId==54?.4f:
            ParticleId==8||ParticleId==9||ParticleId==10||ParticleId==11||ParticleId==14||ParticleId==74?.5f:1.8f; } }
    }
    public sealed partial class AdventurePresentationState
    {
        public readonly List<AdventureZombiePartDrop> ZombiePartDrops = new List<AdventureZombiePartDrop>();
        // PC DropArm/DropHead both request Foley 25 (limbs_pop), once per successful drop.
        public int ZombiePartSoundSerial;
    }
    public static class AdventureZombiePartsRules
    {
        public static bool ShouldLoseHead(int kind, int height, bool flying, bool sled, bool hasHead, float health, int maximum)
        { return hasHead && health < maximum / 3 && AdventureZombieFeedbackRules.CanLoseParts(kind, height, flying, sled); }
        public static int HeadParticle(int kind, bool mowed, bool inPool)
        {
            // IDs come from THIS PC EXE's 0x74C8D8 table, not the mobile ParticleEffect enum.
            if (kind == 5) return 17;
            if (kind == 18) return 61;
            if (kind == 16) return 18;
            return mowed ? 72 : inPool ? 74 : 6;
        }
        public static string ParticleName(int id)
        {
            switch (id)
            {
                case 6: return "ZombieHead";
                case 8: return "ZombieTrafficCone";
                case 9: return "ZombiePail";
                case 10: return "ZombieHelmet";
                case 12: return "ZombieDoor";
                case 13: return "ZombieNewspaper";
                case 14: return "ZombieHeadLight";
                case 54: return "ZombieLadder";
                case 11: return "ZombieFlag";
                case 17: return "ZombieNewspaperHead";
                case 18: return "ZombieBalloonHead";
                case 61: return "ZombiePogoHead";
                case 72: return "MoweredZombieHead";
                case 74: return "ZombieHeadPool";
                default: throw new ArgumentOutOfRangeException("id");
            }
        }
        public static string HeadImage(int kind)
        {
            // PC 0x54581A..0x5458D9, image globals resolved against the PC resource loader.
            switch (kind)
            {
                case 8: return "IMAGE_ZOMBIEDANCERHEAD";
                case 9: return "IMAGE_ZOMBIEBACKUPDANCERHEAD";
                case 13: return "IMAGE_ZOMBIEBOBSLEDHEAD";
                case 21: return "IMAGE_ZOMBIELADDERHEAD";
                case 24: return "IMAGE_ZOMBIEIMPHEAD";
                case 7: return "IMAGE_ZOMBIEFOOTBALLHEAD";
                case 3: return "IMAGE_ZOMBIEPOLEVAULTERHEAD";
                case 11: return "IMAGE_REANIM_ZOMBIE_SNORKLE_HEAD";
                case 17: return "IMAGE_ZOMBIEDIGGERHEAD";
                case 14: return "IMAGE_ZOMBIEDOLPHINRIDERHEAD";
                case 19: return "IMAGE_ZOMBIEYETIHEAD";
                default: return null; // Preserve each XML's head / hat / propeller / glasses images.
            }
        }
        public static bool HeadTrackVisible(int kind, string track)
        {
            if (track.StartsWith("anim_head", StringComparison.OrdinalIgnoreCase) ||
                track.StartsWith("anim_hair", StringComparison.OrdinalIgnoreCase) ||
                track.StartsWith("anim_tongue", StringComparison.OrdinalIgnoreCase)) return false;
            if (kind == 8 && (Same(track, "Zombie_disco_chops") || Same(track, "Zombie_disco_glasses"))) return false;
            if (kind == 9 && (Same(track, "Zombie_disco_chops") || Same(track, "Zombie_backup_stash"))) return false;
            if (kind == 16 && (track.StartsWith("anim_hat", StringComparison.OrdinalIgnoreCase) || track.StartsWith("hat", StringComparison.OrdinalIgnoreCase))) return false;
            if (kind == 3 && (Same(track, "Zombie_polevaulter_innerarm") || Same(track, "Zombie_polevaulter_innerhand") || Same(track, "Zombie_polevaulter_pole"))) return false;
            return (kind != 1 && kind != 33) || !Same(track, "Zombie_flaghand") && !Same(track, "Zombie_innerarm_screendoor");
        }
        private static bool Same(string a, string b) { return string.Equals(a, b, StringComparison.OrdinalIgnoreCase); }
    }
}
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private void AdvanceAdventureZombieParts(float dt)
        {
            for (int i = Presentation.ZombiePartDrops.Count - 1; i >= 0; i--)
            {
                var part = Presentation.ZombiePartDrops[i]; part.Age += dt;
                if (part.Age >= part.Duration) Presentation.ZombiePartDrops.RemoveAt(i);
            }
        }
        private void AddAdventureZombiePart(Zombie z, int kind, int particleId, string anchor, string image)
        {
            string clip = AdventurePresentationZombieAnimation(z);
            Presentation.ZombiePartDrops.Add(new PvZReplica.Adventure.AdventureZombiePartDrop {
                Id = ++Presentation.EffectId, Seed = z.Id * 397 ^ Presentation.EffectId, NativeKind = kind,
                ParticleId = particleId, X = z.X, Row = z.Row, Scale = z.IsSmall ? .5f : 1f,
                Asset = AdventureZombiePresentationAsset(z), Animation = clip,
                AnimationAge = z.PresentationAnimationAge, Rate = AdventurePresentationZombieRate(z), Anchor = anchor, Image = image,
                Backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && z.AbilityPhase == 2),
                Chilled = z.ChillRemaining > 0f || z.FrozenRemaining > 0f, Hypnotized = z.Hypnotized });
        }
        private void LoseAdventureZombieHead(Zombie z, int kind, bool inPool, bool mowed, uint flags)
        {
            // Keep the intact-pose animation/anchor snapshot before changing the walk/eat state.
            if ((flags & 16U) == 0 && (kind < 26 || kind > 31))
            {
                if (kind == 1 || kind == 33) AddAdventureZombiePart(z, kind, 11, "Zombie_flaghand", null);
                AddAdventureZombiePart(z, kind, PvZReplica.Adventure.AdventureZombiePartsRules.HeadParticle(kind, mowed, inPool),
                    "anim_head1", PvZReplica.Adventure.AdventureZombiePartsRules.HeadImage(kind));
                Presentation.ZombiePartSoundSerial++;RequestAdventureZombieFoley(25);
            }
            z.Headless = true; z.PresentationButter = 0f;
            if (z.Kind == ZombieKind.Pogo) z.UsedAbility = true;
        }
        private void AdventurePresentationZombieMowedParts(Zombie z)
        {
            int kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            int height = z.PresentationNativeHeight;
            bool sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            if (!PvZReplica.Adventure.AdventureZombieFeedbackRules.CanLoseParts(kind, height, z.Airborne, sled)) return;
            bool inPool = z.PresentationInPool ?? (Adventure.Level.IsWaterRow(z.Row) && z.X >= 0f && z.X < 9f);
            if (!z.Headless) LoseAdventureZombieHead(z, kind, inPool, true, 0);
            if (PvZReplica.Adventure.AdventureZombieFeedbackRules.ShouldLoseArm(kind, 3, height, z.Airborne, sled,
                !z.PresentationArmLost, PvZReplica.Adventure.AdventureZombieFeedbackRules.ShieldType(z), 1f, (int)z.StartingHealth))
                LoseAdventureZombieArm(z, kind, 3, inPool, 0);
        }
    }
}
