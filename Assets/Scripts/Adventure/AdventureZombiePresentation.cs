using System;
using System.IO;
using PvZReplica.VerifiedData;

namespace PvZReplica
{
    public sealed partial class Zombie
    {
        public bool PresentationArmLost;
        public float PresentationShieldRecoil;
        public int PresentationNativeHeight = -1;
        public float PresentationMovementRate = -1f;
        internal string PresentationMovementAnimation;
        internal float PresentationWalkPhase, PresentationTickSlow = 1f;
        internal int PresentationWalkLoops;
    }
    public sealed partial class GameSimulation
    {
        private void AdvanceAdventureZombieFeedback(float dt)
        {
            for (int i = Presentation.ArmDrops.Count - 1; i >= 0; i--)
            {
                Presentation.ArmDrops[i].Age += dt;
                if (Presentation.ArmDrops[i].Age >= .6f) Presentation.ArmDrops.RemoveAt(i);
            }
            AdvanceAdventureZombieParts(dt);
            foreach (Zombie z in Zombies)
            {
                z.PresentationShieldRecoil=Math.Max(0f,z.PresentationShieldRecoil-dt);
                if(z.PresentationNewspaperGaspRemaining>0f&&z.FrozenRemaining<=0f)
                {
                    z.PresentationNewspaperGaspRemaining=Math.Max(0f,z.PresentationNewspaperGaspRemaining-dt*(z.ChillRemaining>0?.5f:1f));
                    if(z.PresentationNewspaperGaspRemaining<=0f){z.PresentationNativePhase=31;RefreshAdventureZombieNativeSpeed(z);}
                }
            }
        }
        private void AdvanceAdventureZombieClock(Zombie z, float dt)
        {
            EnsureAdventureZombieEquipment(z);
            RefreshAdventureZombieNativeSpeed(z);
            string clip = AdventurePresentationZombieAnimation(z);
            z.PresentationTickSlow = z.ChillRemaining > 0f ? .5f : 1f;
            if (z.FrozenRemaining > 0f) return;
            float step = dt * z.PresentationTickSlow;
            float rate = AdventurePresentationZombieRate(z);
            PvZReplica.Adventure.AdventureZombieGroundClip data;
            if (rate > 0f && PvZReplica.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z), clip, out data))
            {
                z.PresentationWalkPhase += step * rate / data.Count;
                int loops = (int)Math.Floor(z.PresentationWalkPhase);
                z.PresentationWalkPhase -= loops; z.PresentationWalkLoops += loops;
                z.PresentationAnimationAge = (z.PresentationWalkLoops + z.PresentationWalkPhase) * data.Count / rate;
            }
            else z.PresentationAnimationAge += step;
            z.PresentationPlantHeadAge += step;
        }
        private int AdventureZombieFeedbackPhase(Zombie z)
        {
            if (z.PresentationNativePhase >= 0) return z.PresentationNativePhase;
            if (z.Kind == ZombieKind.Newspaper) return z.ShieldHealth > 0 ? 29 : 31;
            if (z.Kind == ZombieKind.Ladder && z.ShieldHealth > 0) return 76;
            if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility) return 17;
            if (z.Kind == ZombieKind.Pogo && !z.UsedAbility) return 20;
            if (z.Kind == ZombieKind.Balloon && z.Airborne) return 73;
            if (z.Kind == ZombieKind.Digger) return z.AbilityPhase == 0 ? 32 : z.AbilityPhase == 1 ? 36 : 37;
            if (z.Kind == ZombieKind.DolphinRider)
                return z.UsedAbility ? (Adventure.Level.IsWaterRow(z.Row) ? 55 : 56) : Adventure.Level.IsWaterRow(z.Row) ? 53 : 51;
            if (z.Kind == ZombieKind.Snorkel && z.Submerged && Adventure.Level.IsWaterRow(z.Row)) return 59;
            if (z.Kind == ZombieKind.PoleVaulter && !z.UsedAbility) return 11;
            return 0;
        }
        public static string AdventureZombiePresentationAsset(Zombie z)
        {
            ReanimationDefinition definition;
            int kind = (int)z.Kind;
            return kind >= 0 && kind < VerifiedDataCatalog.Zombies.Count &&
                VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Zombies[kind].ReanimationId, out definition)
                ? Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/')) : "Zombie";
        }
        public string AdventurePresentationZombieAnimation(Zombie z)
        {
            string asset = AdventureZombiePresentationAsset(z), clip;
            if (z.PresentationNewspaperGaspRemaining>0f) clip="anim_gasp";
            else if (z.Kind == ZombieKind.Newspaper && z.ShieldHealth <= 0) clip = z.Eating ? "anim_eat_nopaper" : "anim_walk_nopaper";
            else if (z.Kind == ZombieKind.Ladder && z.ShieldHealth > 0) clip = z.Eating ? "anim_laddereat" : "anim_ladderwalk";
            else if (z.Eating) clip = "anim_eat";
            else if (z.Kind == ZombieKind.PoleVaulter && !z.UsedAbility) clip = "anim_run";
            else if (z.Kind == ZombieKind.Pogo && !z.UsedAbility) clip = "anim_pogo";
            else if (z.Kind == ZombieKind.Digger && z.AbilityPhase == 0) clip = "anim_dig";
            else if (z.Kind == ZombieKind.Digger && z.AbilityPhase == 1) clip = "anim_dizzy";
            else if (z.Kind == ZombieKind.DolphinRider && !z.UsedAbility) clip = Adventure.Level.IsWaterRow(z.Row) ? "anim_ride" : "anim_walkdolphin";
            else if (z.Kind == ZombieKind.Snorkel && z.Submerged) clip = "anim_swim";
            else if (z.Kind == ZombieKind.DuckyTube && Adventure.Level.IsWaterRow(z.Row)) clip = "anim_swim";
            else if (z.Kind == ZombieKind.Balloon && z.Airborne) clip = "anim_idle";
            else if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility) clip = "anim_push";
            else if (z.Kind == ZombieKind.Zomboni) clip = "anim_drive";
            else if (z.Kind == ZombieKind.Catapult) clip = "anim_walk";
            else clip = z.PresentationWalk2 ? "anim_walk2" : "anim_walk";
            PvZReplica.Adventure.AdventureZombieGroundClip data;
            if (!PvZReplica.Adventure.AdventureZombieGroundData.TryGet(asset, clip, out data))
                clip = PvZReplica.Adventure.AdventureZombieGroundData.TryGet(asset, "anim_walk", out data) ? "anim_walk" : "anim_idle";
            if (!string.Equals(z.PresentationAnimation, clip, StringComparison.Ordinal))
            {
                z.PresentationAnimation = clip; z.PresentationAnimationAge = 0f;
                z.PresentationWalkPhase = 0f; z.PresentationWalkLoops = 0;
                z.PresentationMovementAnimation = null; z.PresentationMovementRate = -1f;
            }
            return clip;
        }
        public float AdventurePresentationZombieRate(Zombie z)
        {
            string clip = AdventurePresentationZombieAnimation(z);
            if (z.PresentationNewspaperGaspRemaining>0f)return 8f;
            if (z.Eating) return z.Kind == ZombieKind.PoleVaulter || z.Kind == ZombieKind.Balloon || z.Kind == ZombieKind.Imp ||
                z.Kind == ZombieKind.Digger || z.Kind == ZombieKind.JackInTheBox || z.Kind == ZombieKind.Snorkel || z.Kind == ZombieKind.Yeti ? 20f : 36f;
            if (z.Kind == ZombieKind.Catapult) return 5.5f;
            if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility) return 30f;
            if (z.Kind == ZombieKind.Balloon && z.Airborne || z.Kind == ZombieKind.Zomboni) return 12f;
            if (z.PresentationMovementRate >= 0 && z.PresentationMovementAnimation == clip) return z.PresentationMovementRate;
            return AdventureZombiePlaybackRate(z, clip, z.Speed);
        }
        private static float AdventureZombiePlaybackRate(Zombie z, string clip, float speed)
        {
            PvZReplica.Adventure.AdventureZombieGroundClip data;
            if (!PvZReplica.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z), clip, out data)) return 12f;
            return data.GroundDistance > .000001f ? speed * 80f * .01f * 47f * data.Count / data.GroundDistance / (z.IsSmall ? .5f : 1f) : data.SourceRate;
        }
        private float AdventurePresentationWalkingDistance(Zombie z, float dt, float fallbackDistance)
        {
            if (dt <= 0f || z.PresentationNewspaperGaspRemaining>0f || z.FrozenRemaining > 0f || z.DropRemaining > 0f || z.Eating) return 0f;
            string clip = AdventurePresentationZombieAnimation(z);
            int phase = AdventureZombieFeedbackPhase(z), kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            bool sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            PvZReplica.Adventure.AdventureZombieGroundClip data;
            if (!PvZReplica.Adventure.AdventureZombieFeedbackRules.UsesGround(kind, phase, sled) ||
                !PvZReplica.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z), clip, out data) || data.Ground == null)
                return fallbackDistance;
            float slow = z.PresentationTickSlow;
            float rate = AdventureZombiePlaybackRate(z, clip, fallbackDistance / (dt * slow));
            z.PresentationMovementAnimation = clip; z.PresentationMovementRate = rate;
            if (rate > 0f) z.PresentationAnimationAge = (z.PresentationWalkLoops + z.PresentationWalkPhase) * data.Count / rate;
            return data.VelocityAtPhase(z.PresentationWalkPhase, rate * slow) * (z.IsSmall ? .5f : 1f) * (dt * 100f) / 80f;
        }
        private void AdventurePresentationZombieHit(Zombie z, float damage, bool bypassShield = false, uint damageFlags = 0)
        {
            EnsureAdventureZombieEquipment(z);
            z.PresentationArmorBeforeHit=z.ArmorHealth;z.PresentationShieldBeforeHit=z.ShieldHealth;
            z.PresentationHealthBeforeHit = z.Health;
            if (damage <= 0f || (damageFlags & 8U) != 0) return;
            float remaining = damage;
            if (!bypassShield && z.ShieldHealth > 0f)
            {
                if((damageFlags&2U)==0&&(z.Kind==ZombieKind.ScreenDoor||z.Kind==ZombieKind.Ladder))RequestAdventureZombieFoley(46);
                z.PresentationArmorFlash = .25f;
                if ((damageFlags & 2U) == 0) z.PresentationShieldRecoil = .12f;
                remaining -= Math.Min(remaining, z.ShieldHealth);
            }
            // Helm damage uses the body's additive group, including the still attached helm.
            if (remaining > 0f) z.PresentationFlash = .25f;
        }
        private void AdventurePresentationZombieBodyDamaged(Zombie z, uint damageFlags = 0)
        {
            int kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            int height = z.PresentationNativeHeight;
            if (height < 0 && ModeSession != null && ModeSession.Definition.Mechanic == Modes.ModeMechanic.Aquarium) height = 10;
            int phase = AdventureZombieFeedbackPhase(z);
            bool sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            bool inPool = z.PresentationInPool ?? (Adventure.Level.IsWaterRow(z.Row) && z.X >= 0f && z.X < 9f);
            // Burn death selects its own native charred attachment; don't turn it into flesh parts.
            if (z.PresentationDeath == PvZReplica.Adventure.AdventureZombieDeathPresentation.Burned && z.Health <= 0f) return;
            if (PvZReplica.Adventure.AdventureZombieFeedbackRules.ShouldLoseArm(kind, phase, height, z.Airborne, sled,
                !z.PresentationArmLost, PvZReplica.Adventure.AdventureZombieFeedbackRules.ShieldType(z), z.Health, (int)z.StartingHealth))
                LoseAdventureZombieArm(z, kind, phase, inPool, damageFlags);
            // Unlike the arm test this also runs after a lethal body hit (PC 0x546160).
            if (PvZReplica.Adventure.AdventureZombiePartsRules.ShouldLoseHead(kind, height, z.Airborne, sled,
                !z.Headless, z.Health, (int)z.StartingHealth))
                LoseAdventureZombieHead(z, kind, inPool, phase == 3, damageFlags);
        }
        private void LoseAdventureZombieArm(Zombie z, int kind, int phase, bool inPool, uint damageFlags)
        {
            z.PresentationArmLost = true;
            if (inPool || (damageFlags & 16U) != 0) return;
            Presentation.ZombiePartSoundSerial++;RequestAdventureZombieFoley(25);
            string clip = AdventurePresentationZombieAnimation(z);
            Presentation.ArmDrops.Add(new PvZReplica.Adventure.AdventureZombieArmDrop {
                Id = ++Presentation.EffectId, Seed = z.Id * 397 ^ Presentation.EffectId, NativeKind = kind,
                X = z.X, Row = z.Row, Scale = z.IsSmall ? .5f : 1f, Asset = AdventureZombiePresentationAsset(z),
                Animation = clip, AnimationAge = z.PresentationAnimationAge, Rate = AdventurePresentationZombieRate(z),
                Anchor = PvZReplica.Adventure.AdventureZombieFeedbackRules.ArmAnchor(kind), Image = PvZReplica.Adventure.AdventureZombieFeedbackRules.ArmImage(kind),
                Mowed = phase == 3, Backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && z.AbilityPhase == 2),
                Chilled = z.ChillRemaining > 0f || z.FrozenRemaining > 0f, Hypnotized = z.Hypnotized });
        }
    }
}
