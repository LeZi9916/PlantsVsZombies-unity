using System;
using System.Collections.Generic;
using OriginalSun = PvZUnity.Sun;

namespace PvZReplica.Adventure
{
    public enum AdventureZombieDeathPresentation { Normal, Mowed, Burned, Eaten, Removed }
    public enum AdventureSunMotion { Sky = 0, Plant = 2, Coin = 3 }
    public enum AdventureBurnVisual { None, CharredDefinition, FrozenOriginal, NormalDeath }
    public sealed class AdventureBurnPose
    {
        public AdventureBurnVisual Visual;
        public int ReanimationId = -1;
        public string Animation;
        public float X, Y, Scale = 1f, PivotX, PivotY;
        public bool FacingLeft, HideImp;
    }
    public static class AdventureBurnPresentationRules
    {
        // Exact branch order and constants from the PC function 0x54E920..0x54EDB3.
        public static AdventureBurnPose Resolve(int kind, int phase, bool inPool, bool flying,
            bool hasHead, bool hasSled, bool backwards, bool hasObject, float scale, float healthBeforeHit)
        {
            var result = new AdventureBurnPose();
            if (kind == 25 || healthBeforeHit >= 1800f) { result.Visual = AdventureBurnVisual.NormalDeath; return result; }
            if (inPool || phase == 1 || phase == 3 || phase == 12 || phase == 14 || phase == 32 || phase == 33 ||
                phase == 34 || phase == 35 || phase == 50 || phase == 52 || phase == 53 || phase == 54 || phase == 58 || phase == 71)
                return result;
            if (kind == 19 || kind == 20 || kind >= 26 && kind <= 31 || hasSled || flying || !hasHead)
            {
                result.Visual = AdventureBurnVisual.FrozenOriginal; result.Scale = scale; result.FacingLeft = backwards; return result;
            }
            result.Visual = AdventureBurnVisual.CharredDefinition;
            result.X = 22f; result.Y = -10f; result.ReanimationId = 36;
            switch (kind)
            {
                case 0: case 1: case 2: case 3: case 4: case 5: case 6: case 7:
                case 8: case 9: case 10: case 11: case 13: case 14: case 15: case 16: case 18: case 21: break;
                case 12: result.ReanimationId = 39; result.X += 61f; result.Y -= 16f; break;
                case 17: result.ReanimationId = 38; if (backwards) result.X += 14f;
                    result.Animation = phase == 38 ? "anim_crumble_noaxe" : "anim_crumble"; break;
                case 22: result.ReanimationId = 40; result.X -= 36f; result.Y -= 20f; break;
                case 23: case 32: result.ReanimationId = 41; result.X -= 15f; result.Y -= 10f;
                    result.HideImp = !hasObject; break;
                case 24: result.ReanimationId = 37; result.X -= 6f; break;
                default: result.Visual = AdventureBurnVisual.None; return result;
            }
            if (kind == 16) result.Y += 31f;
            // PC-only rule: dancing/backup charred bodies restore scale 1, unlike the mobile port.
            result.Scale = kind == 8 || kind == 9 ? 1f : scale;
            result.PivotX = 20f * (1f - result.Scale); result.PivotY = 120f * (1f - result.Scale);
            result.FacingLeft = backwards;
            return result;
        }
    }
    public sealed partial class AdventureZombieCorpse
    {
        public Zombie Zombie;
        public string Animation;
        public AdventureBurnPose Burn;
        public float FrozenSeconds, FrozenRate, FrozenHeadSeconds;
        public float Age, Rate, RateScale = 1f, Duration = float.PositiveInfinity;
    }
    public sealed partial class AdventureImpact
    {
        public int Id, Seed;
        public PlantKind Kind;
        public float X, Row, Age;
        public bool Snow, Fire, Butter, Cob, Mowed;
    }
    public sealed class AdventureBowlingPresentation { public float Age, Rate; }
    public sealed partial class AdventurePresentationState
    {
        public readonly List<AdventureZombieCorpse> Corpses = new List<AdventureZombieCorpse>();
        public readonly List<AdventureImpact> Impacts = new List<AdventureImpact>();
        public readonly Dictionary<Mower, float> MowerTimes = new Dictionary<Mower, float>();
        public readonly Dictionary<AdventureBowlingNut, AdventureBowlingPresentation> Bowling = new Dictionary<AdventureBowlingNut, AdventureBowlingPresentation>();
        internal int EffectId;
    }
    public static class AdventurePresentationRules
    {
        // PC 0x46D240, 0x472C70. Counts are native logic updates, not guessed reanim frames.
        public static float ShootingRate(PlantKind kind)
        {
            if (kind == PlantKind.Repeater || kind == PlantKind.SplitPea) return 45f;
            if (kind == PlantKind.GatlingPea) return 38f;
            if (kind == PlantKind.Threepeater) return 20f;
            if (kind == PlantKind.GloomShroom) return 14f;
            if (kind == PlantKind.Starfruit) return 28f;
            if (kind == PlantKind.Cattail) return 30f;
            return 35f;
        }
        public static float LaunchAge(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.Peashooter: case PlantKind.SnowPea: case PlantKind.Cactus: case PlantKind.Threepeater: return .34f;
                case PlantKind.Repeater: case PlantKind.SplitPea: return .25f;
                case PlantKind.CabbagePult: return .31f;
                case PlantKind.KernelPult: return .29f;
                case PlantKind.MelonPult: case PlantKind.WinterMelon: return .35f;
                case PlantKind.FumeShroom: return .49f;
                case PlantKind.Starfruit: return .39f;
                case PlantKind.ScaredyShroom: return .24f;
                case PlantKind.GatlingPea: return .31f;
                case PlantKind.Cattail: return .30f;
                default: return .28f;
            }
        }
        public static bool AttachedHead(PlantKind kind)
        {
            return kind == PlantKind.Peashooter || kind == PlantKind.SnowPea || kind == PlantKind.Repeater ||
                kind == PlantKind.GatlingPea || kind == PlantKind.Threepeater || kind == PlantKind.SplitPea;
        }
    }
}
namespace PvZReplica
{
    public sealed partial class Plant
    {
        public float PresentationRate, PresentationAwakeAge;
        public readonly float[] PresentationHeadAge = { -1f, -1f, -1f };
        public float PresentationShotAge = -1f;
        public bool PresentationButter;
        internal bool PresentationPrefiring;
        internal PlantKind PresentationKind = (PlantKind)(-1);
    }
    public sealed partial class Zombie
    {
        public float PresentationAnimationAge, PresentationPlantHeadAge, PresentationFlash, PresentationArmorFlash, PresentationButter, PresentationPreviousX;
        public bool PresentationWalk2;
        public string PresentationAnimation;
        public PvZReplica.Adventure.AdventureZombieDeathPresentation PresentationDeath;
        internal bool PresentationInitialized;
        public float PresentationHealthBeforeHit = -1f;
        public int PresentationNativePhase = -1;
        public bool? PresentationInPool, PresentationHasSled, PresentationWalkingBackwards;
    }
    public sealed partial class Pea
    {
        public int PresentationPlantId;
        public float PresentationAge;
        public int PresentationLaunchRow, PresentationLaunchColumn;
        public float PresentationLobVelocity = -7f;
        public float PresentationPreviousX, PresentationPreviousRow, PresentationRotation, PresentationSpin;
        public float PresentationNativeAge, PresentationLobHeight, PresentationPreviousLobHeight, PresentationLobInitialVelocity = -7f, PresentationLobX, PresentationLaunchY;
        public bool PresentationMotionInitialized;
        public bool PresentationCanImpact { get { return !Lobbed || PresentationLobVelocity >= 0f && PresentationLobHeight >= (Butter ? -32f : Splash ? -35f : -30f); } }
        public bool PresentationInitialized;
    }

    public sealed partial class Sun
    {
        public bool PresentationNative, PresentationFromPlant, PresentationSpecialExpansion;
        public Adventure.AdventureSunMotion PresentationMotion;
        public OriginalSun.SunPickupState PresentationPickup;
        public float PresentationPreviousX, PresentationPreviousY, PresentationScale = 1f;
        public float PresentationVelocityX, PresentationVelocityY, PresentationLandingY;
    }

    public sealed partial class GameSimulation
    {
        public float AdventurePresentationRemainder { get { return State == MatchState.Playing && IsAdventure && Adventure.Phase == PvZReplica.Adventure.AdventurePhase.Playing ? (float)Math.Max(0d, Math.Min(SimulationStep, ModeSession == null ? accumulatedTime : modeAccumulated)) : 0f; } }
        public PvZReplica.Adventure.AdventurePresentationState Presentation { get; private set; } = new PvZReplica.Adventure.AdventurePresentationState();

        // Declarations live with their implementations so shared simulation files only need calls.
        partial void AdventurePresentationTick(float dt);
        partial void AdventurePresentationShot(Plant plant, Pea shot);
        partial void AdventurePresentationZombieDeath(Zombie zombie);
        partial void AdventurePresentationImpact(Pea shot, Zombie zombie);
        partial void AdventurePresentationZombieHit(Zombie zombie, bool armor);

        partial void AdventurePresentationTick(float dt)
        {
            AdvanceAdventureExplosionPresentation();
            AdvanceAdventureIcePresentation(dt);
            AdvanceAdventureZombieFeedback(dt);
            for (int i = Presentation.Corpses.Count - 1; i >= 0; i--)
            {
                var corpse = Presentation.Corpses[i]; corpse.Age += dt;
                AdvanceAdventureZombieCorpseSound(corpse);
                corpse.Zombie.PresentationFlash = Math.Max(0, corpse.Zombie.PresentationFlash - dt);
                corpse.Zombie.PresentationArmorFlash = Math.Max(0, corpse.Zombie.PresentationArmorFlash - dt);
                if (corpse.Age >= corpse.Duration) Presentation.Corpses.RemoveAt(i);
            }
            for (int i = Presentation.Impacts.Count - 1; i >= 0; i--)
            {
                Presentation.Impacts[i].Age += dt;
                if (Presentation.Impacts[i].Age > 3f) Presentation.Impacts.RemoveAt(i);
            }
            foreach (Mower mower in Mowers)
            {
                float time; Presentation.MowerTimes.TryGetValue(mower, out time);
                Presentation.MowerTimes[mower] = mower.Active ? time + dt : 0f;
            }
            foreach (var nut in Adventure.BowlingNuts)
            {
                PvZReplica.Adventure.AdventureBowlingPresentation pose;
                if (!Presentation.Bowling.TryGetValue(nut, out pose))
                {
                    bool giant = ModeSession != null && ModeSession.GiantNuts.Contains(nut);
                    pose = new PvZReplica.Adventure.AdventureBowlingPresentation { Rate = (giant ? 6f : 12f) + (float)random.NextDouble() * (giant ? 4f : 6f) };
                    Presentation.Bowling.Add(nut, pose);
                }
                if (!nut.NativeMotionInitialized) pose.Age += dt;
            }
            foreach (Pea shot in Peas) { shot.PresentationAge += dt; shot.PresentationPreviousX = shot.X; shot.PresentationPreviousRow = shot.FlightRow; shot.PresentationPreviousLobHeight = shot.PresentationLobHeight; }
            foreach (Zombie zombie in Zombies)
            {
                zombie.PresentationPreviousX = zombie.X;
                if (!zombie.PresentationInitialized)
                {
                    zombie.PresentationInitialized = true;
                    zombie.PresentationWalk2 = zombie.Kind == ZombieKind.Flag || random.Next(2) == 0;
                }
                AdventurePresentationZombieAnimation(zombie);
                zombie.PresentationFlash = Math.Max(0, zombie.PresentationFlash - dt);
                zombie.PresentationArmorFlash = Math.Max(0, zombie.PresentationArmorFlash - dt);
                zombie.PresentationButter = Math.Max(0, zombie.PresentationButter - dt);
                AdvanceAdventureZombieClock(zombie, dt);
            }
            foreach (Plant plant in Plants)
            {
                if (plant.PresentationKind != plant.Kind)
                {
                    plant.PresentationKind = plant.Kind;
                    float minimum = PvZReplica.Adventure.AdventurePresentationRules.AttachedHead(plant.Kind) ? 15f :
                        plant.Kind == PlantKind.CherryBomb || plant.Kind == PlantKind.Imitater ? 25f : 10f;
                    plant.PresentationRate = minimum + (float)random.NextDouble() * 5f;
                }
                if (plant.Asleep) continue;
                plant.PresentationAwakeAge += dt;
                if (plant.PresentationShotAge >= 0) plant.PresentationShotAge += dt;
                for (int head = 0; head < 3; head++) if (plant.PresentationHeadAge[head] >= 0) plant.PresentationHeadAge[head] += dt;
                if (plant.Kind == PlantKind.GatlingPea || plant.Kind == PlantKind.GloomShroom || plant.Kind == PlantKind.Cattail)
                {
                    if (plant.UpgradeBurstTicks > 0)
                    {
                        float elapsed = ((plant.Kind == PlantKind.GloomShroom ? 200 : plant.Kind == PlantKind.GatlingPea ? 100 : 50) - plant.UpgradeBurstTicks) * .01f;
                        plant.PresentationShotAge = elapsed; plant.PresentationHeadAge[0] = elapsed;
                    }
                    continue;
                }
                if (plant.Timing.HasValue && (plant.Kind == PlantKind.Peashooter || plant.Kind == PlantKind.SnowPea ||
                    plant.Kind == PlantKind.Repeater || plant.Kind == PlantKind.Sunflower)) continue;
                float lead = PvZReplica.Adventure.AdventurePresentationRules.LaunchAge(plant.Kind);
                if (plant.ActionTimer > 0 && plant.ActionTimer <= lead && !plant.PresentationPrefiring && CanPresentAdventureWindup(plant))
                {
                    plant.PresentationPrefiring = true;
                    plant.PresentationShotAge = Math.Max(0, lead - plant.ActionTimer);
                    for (int h = 0; h < 3; h++) plant.PresentationHeadAge[h] = plant.PresentationShotAge;
                }
            }
        }

        private bool CanPresentAdventureWindup(Plant plant)
        {
            switch (plant.Kind)
            {
                case PlantKind.Peashooter: case PlantKind.SnowPea: case PlantKind.Repeater:
                case PlantKind.Threepeater: case PlantKind.SplitPea: case PlantKind.PuffShroom:
                case PlantKind.FumeShroom: case PlantKind.ScaredyShroom: case PlantKind.SeaShroom:
                case PlantKind.CabbagePult: case PlantKind.KernelPult: case PlantKind.MelonPult:
                case PlantKind.WinterMelon: case PlantKind.Cactus: case PlantKind.Starfruit:
                    return Zombies.Exists(z => !z.Hypnotized && PvZReplica.Adventure.AdventureZombieTargetingRules.CanBeAutoTargeted(z,plant.Kind) && (!z.Airborne || plant.Kind == PlantKind.Cactus) &&
                        (plant.Kind == PlantKind.Threepeater ? Math.Abs(z.Row - plant.Row) <= 1 : z.Row == plant.Row) &&
                        (z.X > plant.Column || plant.Kind == PlantKind.SplitPea));
                default: return false;
            }
        }

        partial void AdventurePresentationShot(Plant plant, Pea shot)
        {
            shot.PresentationInitialized = true; shot.PresentationPlantId = plant.Id;
            shot.PresentationLaunchRow = plant.Row; shot.PresentationLaunchColumn = plant.Column;
            shot.PresentationPreviousX = shot.X; shot.PresentationPreviousRow = shot.FlightRow;
            plant.PresentationPrefiring = false;
            float age = PvZReplica.Adventure.AdventurePresentationRules.LaunchAge(plant.Kind);
            if (plant.Kind == PlantKind.GatlingPea || plant.Kind == PlantKind.Cattail)
                age = Math.Max(0, ((plant.Kind == PlantKind.GatlingPea ? 100 : 50) - plant.UpgradeBurstTicks) * .01f);
            plant.PresentationShotAge = age;
            int head = plant.Kind == PlantKind.Threepeater ? Math.Max(0, Math.Min(2, 1 - (shot.Row - plant.Row))) :
                plant.Kind == PlantKind.SplitPea && shot.Backward ? 1 : 0;
            plant.PresentationHeadAge[head] = age;
            RequestAdventureIceLaunch(plant, shot);
        }

        partial void AdventurePresentationZombieHit(Zombie zombie, bool armor)
        {
            // PC body/armor shot counter 25; additive channel = counter * 10 (0..255).
            zombie.PresentationHealthBeforeHit = zombie.Health;
            if (armor && PvZReplica.Adventure.AdventureZombieFeedbackRules.ShieldType(zombie) != 0)
            { zombie.PresentationArmorFlash = .25f; zombie.PresentationShieldRecoil = .12f; }
            else zombie.PresentationFlash = .25f;
        }
        partial void AdventurePresentationImpact(Pea shot, Zombie zombie)
        {
            RequestAdventureProjectileImpactSound(shot,zombie);
            if (shot.Butter && zombie != null) zombie.PresentationButter = Math.Max(zombie.PresentationButter, 4f);
            var effect = new PvZReplica.Adventure.AdventureImpact { Id = ++Presentation.EffectId, Seed = random.Next(), Kind = shot.SourceKind,
                X = zombie == null ? shot.X : zombie.X, Row = shot.FlightRow, Snow = shot.IsSnow, Fire = shot.Fire, Butter = shot.Butter };
            RecordAdventureIceImpact(shot, zombie, effect);
            Presentation.Impacts.Add(effect);
        }
        partial void AdventurePresentationZombieDeath(Zombie zombie)
        {
            if (zombie.PresentationDeath == PvZReplica.Adventure.AdventureZombieDeathPresentation.Mowed)
            {
                AdventurePresentationZombieMowedParts(zombie);
                Presentation.Impacts.Add(new PvZReplica.Adventure.AdventureImpact { Id = ++Presentation.EffectId, Seed = random.Next(), X = zombie.X, Row = zombie.Row, Mowed = true });
                return;
            }
            if (zombie.PresentationDeath == PvZReplica.Adventure.AdventureZombieDeathPresentation.Eaten ||
                zombie.PresentationDeath == PvZReplica.Adventure.AdventureZombieDeathPresentation.Removed ||
                zombie.BungeeCarrier && zombie.PresentationDeath != PvZReplica.Adventure.AdventureZombieDeathPresentation.Burned) return;
            if (zombie.PresentationDeath == PvZReplica.Adventure.AdventureZombieDeathPresentation.Burned && CreateAdventureBurnPresentation(zombie)) return;
            float rate = zombie.Kind == ZombieKind.Football ? 24f :
                zombie.Kind == ZombieKind.Gargantuar || zombie.Kind == ZombieKind.Snorkel || zombie.Kind == ZombieKind.Yeti ? 14f :
                zombie.Kind == ZombieKind.Digger || zombie.Kind == ZombieKind.Boss ? 18f : 24f + (float)random.NextDouble() * 6f;
            int roll = random.Next(100);
            string clip = Adventure.Level.IsWaterRow(zombie.Row) ? "anim_waterdeath" :
                roll == 99 && Adventure.Level.Number > 5 && zombie.ChillRemaining <= 0 && Zombies.Count <= 5 ? "anim_superlongdeath" :
                roll > 50 ? "anim_death2" : "anim_death";
            if (clip == "anim_superlongdeath") rate = 14f;
            Presentation.Corpses.Add(InitializeAdventureZombieCorpse(new PvZReplica.Adventure.AdventureZombieCorpse { Zombie = zombie, Animation = clip, Rate = rate }));
        }

        private bool CreateAdventureBurnPresentation(Zombie zombie)
        {
            int kind = zombie.ModeHead >= 26 ? zombie.ModeHead : (int)zombie.Kind;
            int phase = zombie.PresentationNativePhase;
            if (phase < 0)
            {
                phase = 0;
                if (zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase < 2) phase = zombie.AbilityPhase == 0 ? 33 : 35;
                else if (zombie.Kind == ZombieKind.PoleVaulter && zombie.UsedAbility && zombie.AbilityTimer > 0 && zombie.AbilityTimer <= .75f) phase = 12;
                else if (zombie.Kind == ZombieKind.DolphinRider && zombie.UsedAbility && zombie.AbilityTimer > 0 && zombie.AbilityTimer <= .75f) phase = 54;
            }
            bool inPool = zombie.PresentationInPool ?? (Adventure.Level.IsWaterRow(zombie.Row) && zombie.X >= 0 && zombie.X < 9);
            bool backwards = zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase >= 1 && phase != 38);
            bool hasSled = zombie.PresentationHasSled ?? (zombie.Kind == ZombieKind.Bobsled && !zombie.UsedAbility);
            float scale = zombie.IsSmall ? .5f : 1f;
            var pose = PvZReplica.Adventure.AdventureBurnPresentationRules.Resolve(kind, phase, inPool, zombie.Airborne,
                !zombie.Headless, hasSled, backwards, !zombie.ThrownImp, scale, zombie.PresentationHealthBeforeHit);
            // PC removes ice/butter and stops hit coloration before choosing the burn visual.
            zombie.FrozenRemaining = 0; zombie.PresentationButter = 0;
            zombie.PresentationFlash = 0; zombie.PresentationArmorFlash = 0;
            if (pose.Visual == PvZReplica.Adventure.AdventureBurnVisual.NormalDeath) return false;
            if (pose.Visual == PvZReplica.Adventure.AdventureBurnVisual.None) return true;
            Presentation.Corpses.Add(new PvZReplica.Adventure.AdventureZombieCorpse { Zombie = zombie, Burn = pose,
                Animation = zombie.PresentationAnimation, FrozenSeconds = zombie.PresentationAnimationAge, FrozenHeadSeconds = zombie.PresentationPlantHeadAge,
                RateScale = pose.Visual == PvZReplica.Adventure.AdventureBurnVisual.CharredDefinition ? .9f + (float)random.NextDouble() * .2f : 1f,
                Duration = pose.Visual == PvZReplica.Adventure.AdventureBurnVisual.FrozenOriginal ? 3f : float.PositiveInfinity });
            return true;
        }

        // Called after horizontal integration. Snow trail sampling follows the torch conversion in the caller.
        // Plant::Fire native lob velocity uses a 120-tick horizontal lead and acceleration .115.
        private void AdvanceAdventureProjectilePresentation(Pea shot, float dt)
        {
            if (!shot.PresentationMotionInitialized)
            {
                shot.PresentationMotionInitialized = true;
                shot.PresentationRotation = shot.SourceKind == PlantKind.KernelPult && !shot.Butter ? 0f :
                    shot.Splash ? -1.2566371f : -.87964594f;
                float minimum = shot.SourceKind == PlantKind.KernelPult && !shot.Butter ? -.2f : -.08f;
                float maximum = shot.SourceKind == PlantKind.KernelPult && !shot.Butter ? -.08f : -.02f;
                shot.PresentationSpin = shot.Lobbed ? minimum + (float)random.NextDouble() * (maximum - minimum) : 0;
                if (shot.Lobbed)
                {
                    float launch = shot.PresentationLaunchColumn + (shot.SourceKind == PlantKind.CabbagePult ? 5f :
                        shot.SourceKind == PlantKind.KernelPult ? shot.Butter ? 12f : 19f : 25f) / 80f;
                    Zombie target = FindAdventureTarget(shot.Row, launch, 20f, false, true);
                    float distance = target == null ? 700f - (40f + launch * 80f) : (target.X - launch - target.Speed * .5f) * 80f - 30f;
                    shot.PresentationLobX = launch; shot.PresentationPreviousX = launch;
                    shot.VelocityX = Math.Max(40f, distance) / 120f * 100f / 80f;
                    float pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
                    float launchOffset = shot.SourceKind == PlantKind.KernelPult ? shot.Butter ? -56f : -37f : shot.Splash ? -46f : -12f;
                    float launchY = 80 + shot.PresentationLaunchRow * pitch + (Adventure.Level.HasRoof ? Math.Max(0, 5 - shot.PresentationLaunchColumn) * 20 - 10 : 0) + launchOffset;
                    if (Plants.Exists(p => p.Kind == PlantKind.FlowerPot && p.Column == shot.PresentationLaunchColumn && p.Row == shot.PresentationLaunchRow)) launchY -= 5;
                    shot.PresentationLaunchY = launchY;
                    float heightDifference = 0;
                    if (target != null)
                    {
                        float floorX = 40 + target.X * 80 - 57f * (target.IsSmall ? .5f : 1f) + 40;
                        float targetY = 80 + target.Row * pitch + (Adventure.Level.HasRoof ? Math.Max(0, 440 - floorX) * .25f - 10 : 0) - 30;
                        if (target.Kind == ZombieKind.Gargantuar || target.Kind == ZombieKind.Catapult) targetY -= 13;
                        if (target.Kind == ZombieKind.Zomboni) targetY += 22;
                        heightDifference = targetY - launchY;
                    }
                    shot.PresentationLobInitialVelocity = -7f + heightDifference / 120f;
                    shot.PresentationLobVelocity = shot.PresentationLobInitialVelocity;
                }
            }
            shot.PresentationNativeAge += dt;
            if (!shot.Lobbed) return;
            float ticks = shot.PresentationNativeAge * 100f;
            shot.X = shot.PresentationLobX + shot.VelocityX * shot.PresentationNativeAge;
            shot.PresentationLobHeight = shot.PresentationLobInitialVelocity * ticks + .115f * ticks * (ticks + 1f) * .5f;
            shot.PresentationLobVelocity = shot.PresentationLobInitialVelocity + .115f * ticks;
        }

        private Sun CreateAdventurePresentationSun(Plant producer, int value = 25)
        {
            var sun = new Sun { Value = value };
            AdventurePresentationSunSpawn(sun, producer);
            return sun;
        }
        partial void AdventurePresentationSunSpawn(Sun sun, Plant producer);
        partial void AdventurePresentationSunSpawn(Sun sun, Plant producer)
        {
            float pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            if (producer != null)
            {
                float nativeX = 40 + producer.Column * 80;
                float nativeY = 80 + producer.Row * pitch + (Adventure.Level.HasRoof ? Math.Max(0, 5 - producer.Column) * 20 - 10 : 0);
                InitializeAdventurePresentationPlantSunAtPoint(sun, nativeX, nativeY);
                return;
            }
            OriginalSun.SunKind kind = sun.Value == 15 ? OriginalSun.SunKind.Small : sun.Value == 50 ? OriginalSun.SunKind.Large : OriginalSun.SunKind.Normal;
            float x = 100 + random.Next(550), y = 60;
            sun.PresentationMotion = PvZReplica.Adventure.AdventureSunMotion.Sky;
            sun.PresentationFromPlant = false; sun.PresentationVelocityX = 0;
            sun.PresentationLandingY = 300 + random.Next(250); sun.PresentationVelocityY = .67f;
            sun.PresentationNative = true;
            sun.PresentationScale = kind == OriginalSun.SunKind.Small ? .5f : kind == OriginalSun.SunKind.Large ? 2f : 1f;
            sun.PresentationPickup = new OriginalSun.SunPickupState(kind, x, y);
            sun.PresentationPreviousX = x; sun.PresentationPreviousY = y;
            sun.X = (x + 30 - 40) / 80f; sun.Y = (y + 30 - 80) / pitch;
        }
        // Inputs are original Coin top-left pixel coordinates, not the reanim centre.
        private Sun CreateAdventurePresentationSunFromPoint(float nativeX, float nativeY, int value = 25)
        {
            var sun = new Sun { Value = value };
            InitializeAdventurePresentationPlantSunAtPoint(sun, nativeX, nativeY);
            return sun;
        }
        private void InitializeAdventurePresentationPlantSunAtPoint(Sun sun, float nativeX, float nativeY)
        {
            float pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            OriginalSun.SunKind kind = sun.Value == 15 ? OriginalSun.SunKind.Small : sun.Value == 50 ? OriginalSun.SunKind.Large : OriginalSun.SunKind.Normal;
            sun.PresentationMotion = PvZReplica.Adventure.AdventureSunMotion.Plant;
            sun.PresentationFromPlant = true;
            sun.PresentationVelocityY = -1.7f - (float)random.NextDouble() * 1.7f;
            sun.PresentationVelocityX = -.4f + (float)random.NextDouble() * .8f;
            sun.PresentationLandingY = (int)(nativeY + 15 + random.Next(20));
            sun.PresentationScale = .4f * (kind == OriginalSun.SunKind.Small ? .5f : kind == OriginalSun.SunKind.Large ? 2f : 1f);
            sun.PresentationPickup = new OriginalSun.SunPickupState(kind, nativeX, nativeY);
            sun.PresentationPreviousX = nativeX; sun.PresentationPreviousY = nativeY;
            sun.X = (nativeX + 30 - 40) / 80f; sun.Y = (nativeY + 30 - 80) / pitch;
            sun.PresentationNative = true;
        }
        // Existing mode drops use board-space centres. Only the explicit sky factory may
        // randomize their birth position. PC 0x43BE43: motion 3, -3-R(2), R(1)-.5,
        // birthY+45+R(20), clamped to 80..521; 0x43C1F0 applies gravity .15.
        private void InitializeAdventurePresentationDroppedSun(Sun sun)
        {
            if (sun == null || sun.PresentationNative) return;
            float pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            float x = 40f + sun.X * 80f - 30f, y = 80f + sun.Y * pitch - 30f;
            OriginalSun.SunKind kind = sun.Value == 15 ? OriginalSun.SunKind.Small : sun.Value == 50 ? OriginalSun.SunKind.Large : OriginalSun.SunKind.Normal;
            sun.PresentationMotion = PvZReplica.Adventure.AdventureSunMotion.Coin;
            sun.PresentationFromPlant = false;
            sun.PresentationVelocityY = -3f - (float)random.NextDouble() * 2f;
            sun.PresentationVelocityX = (float)random.NextDouble() - .5f;
            sun.PresentationLandingY = Math.Max(80, Math.Min(521, (int)(y + 45f + random.Next(20))));
            sun.PresentationScale = kind == OriginalSun.SunKind.Small ? .5f : kind == OriginalSun.SunKind.Large ? 2f : 1f;
            sun.PresentationPickup = new OriginalSun.SunPickupState(kind, x, y);
            sun.PresentationPreviousX = x; sun.PresentationPreviousY = y;
            sun.PresentationNative = true;
        }
        private Sun CreateAdventurePresentationDroppedSun(float x, float row, int value = 25)
        {
            var sun = new Sun { X = x, Y = row, TargetY = row, Value = value };
            InitializeAdventurePresentationDroppedSun(sun);
            return sun;
        }

        private bool TryCollectAdventurePresentationSun(Sun sun)
        {
            if (!IsAdventure || State != MatchState.Playing || !Suns.Contains(sun)) return false;
            if (!sun.PresentationNative) InitializeAdventurePresentationDroppedSun(sun);
            OriginalSun.SunPickupState clicked;
            if (!OriginalSun.SunPickupRules.TryClick(sun.PresentationPickup, (int)sun.PresentationPickup.X + 30,
                (int)sun.PresentationPickup.Y + 30, 0, true, false, sun.PresentationSpecialExpansion, out clicked)) return false;
            sun.PresentationPickup = clicked;
            // PC Coin::MouseDown 0x43D6E0 plays the sun foley before collection flight.
            Raise(GameEvent.SunPickupStarted);
            AdventureSunSelectedForPacketFeedback(sun);
            return true;
        }
        private void AdvanceAdventurePresentationSuns(float dt)
        {
            float pitch = Adventure.Level.HasPool || Adventure.Level.HasRoof ? 85f : 100f;
            for (int i = Suns.Count - 1; i >= 0; i--)
            {
                Sun sun = Suns[i];
                if (!sun.PresentationNative) InitializeAdventurePresentationDroppedSun(sun);
                sun.Age += dt;
                OriginalSun.SunPickupState pickup = sun.PresentationPickup;
                sun.PresentationPreviousX = pickup.X; sun.PresentationPreviousY = pickup.Y;
                float nativeScale = pickup.Kind == OriginalSun.SunKind.Small ? .5f : pickup.Kind == OriginalSun.SunKind.Large ? 2f : 1f;
                bool ground = false;
                if (!pickup.Collecting && pickup.FadeCountdown == 0 && !pickup.Dead)
                {
                    float x = pickup.X, y = pickup.Y + sun.PresentationVelocityY;
                    ground = y >= sun.PresentationLandingY;
                    if (ground)
                    {
                        y = sun.PresentationLandingY;
                        x = (float)(x >= 0 ? Math.Floor(x + .5f) : Math.Ceiling(x - .5f));
                    }
                    else
                    {
                        if (sun.PresentationMotion == PvZReplica.Adventure.AdventureSunMotion.Plant) sun.PresentationVelocityY += .09f;
                        else if (sun.PresentationMotion == PvZReplica.Adventure.AdventureSunMotion.Coin) sun.PresentationVelocityY += .15f;
                        x += sun.PresentationVelocityX;
                        if (x > 740) { x = 740; sun.PresentationVelocityX = -.4f - (float)random.NextDouble() * .4f; }
                        else if (x < 0) { x = 0; sun.PresentationVelocityX = .4f + (float)random.NextDouble() * .4f; }
                    }
                    pickup = pickup.WithPosition(x, y);
                    if (sun.PresentationFromPlant) sun.PresentationScale = sun.PresentationScale < nativeScale ? sun.PresentationScale + .02f : nativeScale;
                }
                if (pickup.Collecting)
                {
                    float dx = pickup.X - 15f, dy = pickup.Y;
                    sun.PresentationScale = Math.Max(.5f, Math.Min(1f, (float)Math.Sqrt(dx * dx + dy * dy) * .05f)) * nativeScale;
                }
                int mode = ModeSession == null ? 0 : ModeSession.Definition.Id;
                var tick = OriginalSun.SunPickupRules.Tick(pickup, SunAmount, true, ground, true, false, mode);
                sun.PresentationPickup = tick.State;
                sun.X = (tick.State.X + 30 - 40) / 80f; sun.Y = (tick.State.Y + 30 - 80) / pitch;
                if (tick.Credited) { SunAmount = tick.Balance; CollectAdventureSun(); Raise(GameEvent.SunCollected); }
                if (tick.State.Dead) Suns.RemoveAt(i);
            }
        }

        // 88 ascending updates to -704, then 84 descending updates until Z > -40.
        public const float AdventurePresentationCobFlightSeconds = 1.72f;
        // Cob impacts are produced by the actual shell explosion, without a second damage source.
        private void AdventurePresentationCobImpact(float x, float row)
        {
            Presentation.Impacts.Add(new PvZReplica.Adventure.AdventureImpact { Id = ++Presentation.EffectId,
                Seed = random.Next(), Kind = PlantKind.CobCannon, X = x, Row = row, Cob = true });
        }
    }
}
