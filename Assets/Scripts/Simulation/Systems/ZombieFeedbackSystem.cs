#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.VerifiedData;
using System;

using System.IO;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ZombieFeedbackSystem responsibilities for one deterministic match.</summary>
    internal sealed class ZombieFeedbackSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BattlePresentationSystem operations.</summary>
        private BattlePresentationSystem _battlePresentationSystem = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BobsledZombieActions operations.</summary>
        private BobsledZombieActions _bobsledZombieActions = null!;
        /// <summary>Provides PogoZombieActions operations.</summary>
        private PogoZombieActions _pogoZombieActions = null!;
        /// <summary>Provides VehicleZombieActions operations.</summary>
        private VehicleZombieActions _vehicleZombieActions = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal ZombieFeedbackSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="battlePresentationSystem">The BattlePresentationSystem collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="bobsledZombieActions">The BobsledZombieActions collaborator.</param>
        /// <param name="pogoZombieActions">The PogoZombieActions collaborator.</param>
        /// <param name="vehicleZombieActions">The VehicleZombieActions collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BattlePresentationSystem battlePresentationSystem, BiteSystem biteSystem, BobsledZombieActions bobsledZombieActions, PogoZombieActions pogoZombieActions, VehicleZombieActions vehicleZombieActions)
        {
            _adventureModeRunner = adventureModeRunner;
            _battlePresentationSystem = battlePresentationSystem;
            _biteSystem = biteSystem;
            _bobsledZombieActions = bobsledZombieActions;
            _pogoZombieActions = pogoZombieActions;
            _vehicleZombieActions = vehicleZombieActions;
        }

        /// <summary>Performs adventure presentation zombie hit.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name = "armor">The armor.</param>
        internal void AdventurePresentationZombieHit(Zombie zombie, bool armor)
        {
            // PC body/armor shot counter 25; additive channel = counter * 10 (0..255).
            zombie.PresentationHealthBeforeHit = zombie.Health;
            if (armor && global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ShieldType(zombie) != 0)
            {
                zombie.PresentationArmorFlash = .25f;
                zombie.PresentationShieldRecoil = .12f;
            }
            else
            {
                zombie.PresentationFlash = .25f;
            }
        }

        /// <summary>Performs adventure presentation zombie death.</summary>
        /// <param name = "zombie">The zombie.</param>
        /// <param name="damageFlags">The original lethal hit flags passed to Zombie::DieWithLoot.</param>
        internal void AdventurePresentationZombieDeath(Zombie zombie, uint damageFlags = 0)
        {
            if (zombie.Kind == ZombieKind.Catapult
                && (zombie.PresentationDeath == AdventureZombieDeathPresentation.Normal
                    || zombie.PresentationDeath == AdventureZombieDeathPresentation.Mowed))
            {
                _vehicleZombieActions.ExplodeAdventureCatapult(zombie);
                return;
            }

            if (zombie.Kind == ZombieKind.Zomboni
                && (zombie.PresentationDeath == AdventureZombieDeathPresentation.Normal
                    || zombie.PresentationDeath == AdventureZombieDeathPresentation.Mowed))
            {
                _vehicleZombieActions.ExplodeAdventureZomboni(zombie);
                return;
            }

            if (zombie.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Mowed)
            {
                AdventurePresentationZombieMowedParts(zombie);
                _world.Presentation.Impacts.Add(new global::PlantsVsZombies.Adventure.AdventureImpact {
                    Id = ++_world.Presentation._effectId,
                    Seed = _world._random.Next(),
                    X = zombie.X,
                    Row = zombie.Row,
                    Mowed = true
                });
                return;
            }

            if (zombie.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Eaten || zombie.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Removed || zombie.BungeeCarrier && zombie.PresentationDeath != global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Burned)
            {
                return;
            }

            if (zombie.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Burned && _battlePresentationSystem.CreateAdventureBurnPresentation(zombie))
            {
                return;
            }

            // PC 0x54F0F0..0x54F107 sends no-body-part deaths straight to Die.
            // Gargantuars and the boss explicitly retain their own death animations.
            var kind = zombie.ModeHead >= 26 ? zombie.ModeHead : (int)zombie.Kind;
            if ((damageFlags & 16U) != 0 && kind != 23 && kind != 32 && kind != 25)
            {
                return;
            }

            // PC DieWithLoot 0x54F030 calls DropShield(1) before selecting the death clip.
            DropAdventureZombieHeldShield(zombie, 1U);

            if (zombie.Kind == ZombieKind.Snorkel && zombie.PresentationNativePhase == 59)
            {
                // PC UpdateDamageStates removes a headless swimming snorkel before any land death clip.
                return;
            }

            if (zombie.Kind == ZombieKind.DolphinRider && zombie.PresentationNativePhase != 55)
            {
                // PC PlayDeathAnim 54F084 keeps a body reanimation only after the dolphin has been lost in water.
                return;
            }

            var rate = zombie.Kind == ZombieKind.Football ? 24f : zombie.Kind == ZombieKind.Gargantuar || zombie.Kind == ZombieKind.Snorkel || zombie.Kind == ZombieKind.Yeti ? 14f : zombie.Kind == ZombieKind.Digger || zombie.Kind == ZombieKind.Boss ? 18f : 24f + (float)_world._random.NextDouble() * 6f;
            if (kind == 23 || kind == 32)
            {
                // PC 54F27F requests the giant's death groan before the death-clip random ticket.
                RequestAdventureZombieFoley(91);
            }
            var roll = _world._random.Next(100);
            var clip = zombie.Kind == ZombieKind.DolphinRider ? "anim_death" : zombie.PresentationInPool == true ? "anim_waterdeath" : roll == 99 && _adventureModeRunner.Adventure.Level.Number > 5 && zombie.ChillRemaining <= 0 && _world.Zombies.Count <= 5 ? "anim_superlongdeath" : roll > 50 ? "anim_death2" : "anim_death";
            if (clip == "anim_superlongdeath")
            {
                rate = 14f;
            }

            _world.Presentation.Corpses.Add(InitializeAdventureZombieCorpse(new global::PlantsVsZombies.Adventure.AdventureZombieCorpse { Zombie = zombie, Animation = clip, Rate = rate }));
        }

        /// <summary>Performs initialize adventure zombie corpse.</summary>
        /// <param name = "corpse">The corpse.</param>
        /// <returns>The resulting initialize adventure zombie corpse.</returns>
        internal global::PlantsVsZombies.Adventure.AdventureZombieCorpse InitializeAdventureZombieCorpse(global::PlantsVsZombies.Adventure.AdventureZombieCorpse corpse)
        {
            // PC PlayDeathAnim replaces every live phase, stops eating, and retains the current pool altitude.
            corpse.RequiredZombie.PresentationNativePhase = 1;
            corpse.RequiredZombie.Eating = false;
            if (corpse.RequiredZombie.PresentationNativeHeight == 6)
            {
                corpse.RequiredZombie.PresentationNativeHeight = 7;
            }

            if (corpse.RequiredZombie.PresentationBodyBlendTicks >= 0)
            {
                corpse.RequiredZombie.PresentationBodyBlendTicks = 20;
            }
            if (corpse.RequiredZombie.Kind == ZombieKind.Pogo)
            {
                corpse.RequiredZombie.PresentationAltitude = 0f;
                corpse.RequiredZombie.PresentationBiteBodyY = 0;
                (corpse.RequiredZombie.PogoState ??= new AdventurePogoState()).AnimationBlendTicks = 20;
            }

            if (corpse.RequiredZombie.Kind == ZombieKind.DolphinRider)
            {
                corpse.RequiredZombie.PresentationNativeVelocity = 0f;
                corpse.RequiredZombie.Speed = 0f;
                (corpse.RequiredZombie.DolphinState ??= new AdventureDolphinState()).AnimationBlendTicks = 20;
            }

            int frames;
            var asset = AdventureZombiePresentationAsset(corpse.RequiredZombie);
            if (!global::PlantsVsZombies.Adventure.AdventureZombieDeathData.TryGet(asset, corpse.Animation, out frames))
            {
                corpse.Animation = "anim_death";
                if (!global::PlantsVsZombies.Adventure.AdventureZombieDeathData.TryGet(asset, corpse.Animation, out frames))
                {
                    corpse.Duration = 0f;
                    return corpse;
                }
            }

            // PC SetAnimRate 54AD40/ApplyAnimRate 54AD60 keeps the original dying chill multiplier.
            // Death counters no longer advance, so apply it once when constructing the retained playback.
            if (corpse.RequiredZombie.ChillRemaining > 0f || AdventureDancerRules.IsDancer(corpse.RequiredZombie)
                && AdventureDancerRules.GroupAffected(corpse.RequiredZombie, _world.Zombies, true))
            {
                corpse.Rate *= .5f;
            }

            corpse.AnimationDuration = global::PlantsVsZombies.Adventure.AdventureZombieDeathRules.AnimationSeconds(frames, corpse.Rate);
            corpse.InPool = corpse.RequiredZombie.PresentationInPool ?? (_adventureModeRunner.Adventure.Level.IsWaterRow(corpse.RequiredZombie.Row) && corpse.RequiredZombie.X >= 0 && corpse.RequiredZombie.X < 9);
            corpse.FadeDuration = global::PlantsVsZombies.Adventure.AdventureZombieDeathRules.FadeSeconds(corpse.InPool);
            corpse.Duration = corpse.AnimationDuration + corpse.FadeDuration;
            return corpse;
        }

        /// <summary>Performs advance adventure zombie corpse sound.</summary>
        /// <param name = "corpse">The corpse.</param>
        internal void AdvanceAdventureZombieCorpseSound(global::PlantsVsZombies.Adventure.AdventureZombieCorpse corpse)
        {
            if (corpse.Burn != null || corpse.FallSoundPlayed || corpse.InPool)
            {
                return;
            }

            var kind = corpse.RequiredZombie.ModeHead >= 26 ? corpse.RequiredZombie.ModeHead : (int)corpse.RequiredZombie.Kind;
            var phase = global::PlantsVsZombies.Adventure.AdventureZombieDeathRules.FallPhase(kind, corpse.Animation);
            if (phase < 0 || corpse.Age < corpse.AnimationDuration * phase)
            {
                return;
            }

            corpse.FallSoundPlayed = true;
            RequestAdventureZombieFoley(28);
            if (kind == 23 || kind == 32)
            {
                RequestAdventureZombieFoley(40);
            }
        }

        /// <summary>Performs ensure adventure zombie equipment.</summary>
        /// <param name = "z">The z.</param>
        internal void EnsureAdventureZombieEquipment(Zombie z)
        {
            if (z.PresentationMaxArmor <= 0 && z.ArmorHealth > 0)
            {
                z.PresentationMaxArmor = z.ArmorHealth;
            }

            if (z.PresentationMaxShield <= 0 && z.ShieldHealth > 0)
            {
                z.PresentationMaxShield = z.ShieldHealth;
            }
        }

        /// <summary>Performs adventure presentation zombie equipment damaged.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "flags">The flags.</param>
        internal void AdventurePresentationZombieEquipmentDamaged(Zombie z, uint flags)
        {
            if (z.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Burned && z.Health <= 0)
            {
                return;
            }

            if (z.Kind == ZombieKind.Bobsled && z.BobsledState?.Position == 0
                && z.PresentationNativePhase != 19 && z._presentationArmorBeforeHit > 0f
                && z.ArmorHealth <= 0f && (flags & 16U) == 0)
            {
                _bobsledZombieActions.CrashAdventureBobsled(z);
            }

            var suppressParts = (flags & 16U) != 0;
            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            if (!suppressParts && z._presentationArmorBeforeHit > 0 && z.ArmorHealth <= 0)
            {
                int helm = global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.HelmType(z), id = global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.HelmParticle(helm);
                if (id >= 0)
                {
                    AddAdventureZombiePart(z, kind, id, global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.HelmTrack(helm), null);
                }
            }

            if (z._presentationShieldBeforeHit > 0 && z.ShieldHealth <= 0)
            {
                var id = AdventureZombieEquipmentRules.ShieldParticle(z);
                if (id >= 0 && !suppressParts)
                {
                    AddAdventureZombiePart(z, kind, id, global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.ShieldTrack(z), null);
                }

                if (z.Kind == ZombieKind.Newspaper)
                {
                    // PC 0x54C7A0: 13 original gasp frames, 8fps, then phase 31.
                    z.PresentationNativePhase = 30;
                    z.PresentationNewspaperGaspRemaining = global::PlantsVsZombies.Adventure.AdventureZombieDeathRules.AnimationSeconds(13, 8f);
                    z.UsedAbility = true;
                    z.Eating = false;
                    if (!suppressParts && (flags & 1U) == 0)
                    {
                        RequestAdventureZombieFoley(67);
                        // PC 0x54C8C2 attaches reanimation115 at the zombie's (-11,0).
                        z.PresentationNewspaperSurpriseAge = 0f;
                    }
                }
            }
        }

        /// <summary>Gets or sets adventure zombie is izombie.</summary>
        internal bool AdventureZombieIsIZombie
        {
            get
            {
                return _world.ModeSession != null && _world.ModeSession.Definition.Mechanic == global::PlantsVsZombies.Modes.ModeMechanic.IZombie;
            }
        }

        /// <summary>Performs pick adventure zombie native speed.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "phase">The phase.</param>
        internal void PickAdventureZombieNativeSpeed(Zombie z, int phase)
        {
            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            var iZombie = AdventureZombieIsIZombie;
            var low = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.PickSpeed(kind, phase, iZombie, 0f);
            var high = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.PickSpeed(kind, phase, iZombie, 1f);
            z.PresentationNativeVelocity = low == high ? low : AdventureSpawnRandom.NextRange(_world._random, low, high);
            z._presentationSpeedPhase = phase;
            z._presentationSpeedKind = kind;
            z.Speed = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
            z.PresentationMovementRate = -1f;
            z._presentationMovementAnimation = null;
        }

        /// <summary>Performs initialize adventure zombie native.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "rightEdge">The right edge.</param>
        internal void InitializeAdventureZombieNative(Zombie z, bool rightEdge)
        {
            // PC 53DE17 draws the initial X before 53DED5 draws the groan clock, even when a caller replaces X.
            var random40 = _world._random.Next(40);
            z.GroanTicks = 400 + _world._random.Next(101);
            // PC 53E02E chooses a speed; 53FF50 -> 54B050 chooses it again and draws the walk variant.
            PickAdventureZombieNativeSpeed(z, 0);
            PickAdventureZombieNativeSpeed(z, 0);
            var walk = _world._random.Next(2);
            z.PresentationWalk2 = z.Kind == ZombieKind.Flag || walk == 0;
            z._presentationInitialized = true;
            var random10 = 0;
            if (z.Kind == ZombieKind.PoleVaulter || z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult || z.Kind == ZombieKind.Gargantuar || (int)z.Kind == 32)
            {
                random10 = _world._random.Next(10);
            }

            // Explicit grave/vase/IZombie/summoned positions remain at their requested world cell.
            if (rightEdge)
            {
                var waves = _adventureModeRunner.Adventure.Waves;
                var flagWave = z.Wave >= 0 && z.Wave < waves.Count && waves[z.Wave].IsFlag;
                z.X = AdventureZombieNativeRules.GridX(
                    AdventureZombieNativeRules.SpawnSourceX((int)z.Kind, random40, random10, flagWave), AdventureDancerRules.Scale(z));
            }

            if (z.Kind == ZombieKind.PoleVaulter)
            {
                z.PresentationNativePhase = 11;
            }
            else if (z.Kind == ZombieKind.Ladder)
            {
                z.PresentationNativePhase = z.ShieldHealth > 0f ? 76 : 0;
            }
            else if (z.Kind == ZombieKind.Pogo)
            {
                z.PogoState = new AdventurePogoState { PhaseTicks = 1 + _world._random.Next(AdventurePogoRules.BounceTicks) };
                z.PresentationNativePhase = 20;
                z.PresentationNativeHeight = 0;
            }
            else if (z.Kind == ZombieKind.Digger)
            {
                z.DiggerState = new AdventureDiggerState();
                z.PresentationNativePhase = 32;
                z.PresentationNativeHeight = 0;
            }
            else if (z.Kind == ZombieKind.Snorkel)
            {
                z.PresentationNativePhase = 57;
                z.Submerged = false;
            }
            else if (z.Kind == ZombieKind.DolphinRider)
            {
                z.DolphinState = new AdventureDolphinState();
                z.PresentationNativePhase = 51;
            }
            else if (z.Kind == ZombieKind.Balloon)
            {
                z.PresentationNativePhase = 73;
                z.PresentationAltitude = 25f;
                z.PresentationNativeHeight = 0;
            }

            if (z.Kind == ZombieKind.Gargantuar || (int)z.Kind == 32)
            {
                var roll = _world._random.Next(100);
                z.GargantuarState = new AdventureGargantuarState
                {
                    Weapon = _adventureModeRunner.Adventure.Level.Number == 48 ? 0 : roll < 10 ? 2 : roll < 35 ? 1 : 0
                };
                z.NativeVariant = false;
                z.PresentationNativePhase = 0;
                z.PresentationBodyBlendTicks = 0;
            }

            if (z.Kind == ZombieKind.Catapult)
            {
                z.CatapultState = new AdventureCatapultState();
                z.PresentationNativePhase = 0;
                z.PresentationBodyBlendTicks = 0;
            }

            // Explicit in-lawn spawns retain their pool placement; edge spawns enter through CheckForPool.
            z.PresentationPoolRow = _adventureModeRunner.Adventure.Level.IsWaterRow(z.Row);
            z.PresentationInPool = !rightEdge && z.PresentationPoolRow
                && AdventureZombieMotionRules.IsPoolPosition(AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z)));
            if (z.PresentationInPool == true)
            {
                z.PresentationAltitude = z.Kind == ZombieKind.Snorkel ? 10f : -40f * AdventureDancerRules.Scale(z);
                if (z.Kind == ZombieKind.Snorkel)
                {
                    z.PresentationNativePhase = 59;
                    z.Submerged = true;
                }
                else if (z.Kind == ZombieKind.DolphinRider)
                {
                    z.PresentationAltitude = 0f;
                    z.PresentationNativePhase = 53;
                }
            }

            if (z.Kind == ZombieKind.Balloon)
            {
                z.PresentationInPool = false;
                z.PresentationAltitude = 25f;
            }

            if (AdventureDancerRules.IsDancer(z))
            {
                AdventureDancerRules.Initialize(z, z.Kind == ZombieKind.Dancing ? _world._random.Next(12) : 0);
            }

            // Only the original species branches that restart walking choose another random speed.
            var phase = z.Kind == ZombieKind.DolphinRider ? 51 : AdventureZombieFeedbackPhase(z);
            if (z.Kind == ZombieKind.PoleVaulter || z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Digger
                || z.Kind == ZombieKind.Ladder || AdventureZombieNativeRules.PickSpeed((int)z.Kind, phase, AdventureZombieIsIZombie, 0f)
                    == AdventureZombieNativeRules.PickSpeed((int)z.Kind, phase, AdventureZombieIsIZombie, 1f))
            {
                PickAdventureZombieNativeSpeed(z, phase);
            }
            else
            {
                z._presentationSpeedPhase = phase;
            }
            if (z.Kind == ZombieKind.DolphinRider)
            {
                z._presentationSpeedPhase = z.PresentationNativePhase;
            }
            // These assignments occur after the initial PickRandomSpeed in PC Init.
            if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility)
            {
                z.BobsledState = new AdventureBobsledState { LeaderId = z.Id };
                z.PresentationNativePhase = 17;
                z.PresentationHasSled = true;
                z.PresentationAltitude = AdventureBobsledRules.InitialAltitude(0);
                z.PresentationNativeVelocity = .6f;
                z.Speed = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridSpeed(.6f);
            }
        }

        /// <summary>Performs refresh adventure zombie native speed.</summary>
        /// <param name = "z">The z.</param>
        internal void RefreshAdventureZombieNativeSpeed(Zombie z)
        {
            if (z.PlantZombieState?.Detached == true)
            {
                return;
            }

            if (z.Kind == ZombieKind.Bungee && z.BungeeState != null)
            {
                z.PresentationNativeVelocity = 0f;
                z.Speed = 0f;
                return;
            }

            if (z.ZomboniState?.FlatTires == true || z.CatapultState?.FlatTires == true)
            {
                z.PresentationNativeVelocity = 0f;
                z.Speed = 0f;
                return;
            }

            if (z.PresentationWhackSpeed)
            {
                return;
            }

            var phase = AdventureZombieFeedbackPhase(z);
            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            if (z.Kind == ZombieKind.Bobsled && z.BobsledState != null && !z.UsedAbility)
            {
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = kind;
                z.Speed = AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
                return;
            }

            if (AdventurePogoRules.IsBouncing(z) && z.PogoState != null)
            {
                // PC 540F10 retains the explicitly selected zero or forward-vault velocity until the next landing.
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = kind;
                z.Speed = AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
                return;
            }

            if (z.Kind == ZombieKind.Digger && phase >= 33 && phase <= 36)
            {
                // PC emergence and axe-loss pause retain the tunnelling velocity until StartWalkAnim.
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = kind;
                return;
            }

            if (z.Kind == ZombieKind.Balloon && phase == 74)
            {
                // PC LandFlyer changes only the clip: PickRandomSpeed happens after the pop finishes.
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = kind;
                z.Speed = AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
                return;
            }

            if (z.Kind == ZombieKind.DolphinRider && (phase == 52 || phase == 53 || phase == 54))
            {
                // PC 541C70 changes these clips without PickRandomSpeed; takeoff and landing set velocity explicitly.
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = kind;
                z.Speed = AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
                return;
            }

            if (z._presentationSpeedPhase != phase || z._presentationSpeedKind != kind)
            {
                PickAdventureZombieNativeSpeed(z, phase);
            }

            if (z.Kind == ZombieKind.Zomboni)
            {
                var nativeX = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
                z.PresentationNativeVelocity = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.ZamboniVelocity(nativeX, z.PresentationNativeVelocity);
                z.Speed = global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
            }
        }

        /// <summary>Performs advance adventure zombie parts.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureZombieParts(float dt)
        {
            for (var i = _world.Presentation.ZombiePartDrops.Count - 1; i >= 0; i--)
            {
                var part = _world.Presentation.ZombiePartDrops[i];
                part.Age += dt;
                if (part.Age >= part.Duration)
                {
                    _world.Presentation.ZombiePartDrops.RemoveAt(i);
                }
            }
        }

        /// <summary>Performs add adventure zombie part.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "particleId">The particle id.</param>
        /// <param name = "anchor">The anchor.</param>
        /// <param name = "image">The image.</param>
        internal void AddAdventureZombiePart(Zombie z, int kind, int particleId, string? anchor, string? image)
        {
            var clip = SynchronizeAdventureZombieAnimation(z);
            _world.Presentation.ZombiePartDrops.Add(new global::PlantsVsZombies.Adventure.AdventureZombiePartDrop {
                Id = ++_world.Presentation._effectId,
                Seed = z.Id * 397 ^ _world.Presentation._effectId,
                NativeKind = kind,
                NativePhase = z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Pogo ? z.PresentationNativePhase : -1,
                NativeAltitude = z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Pogo ? z.PresentationAltitude : 0f,
                ParticleId = particleId,
                X = z.X,
                Row = z.Row,
                Scale = AdventureDancerRules.Scale(z),
                Asset = AdventureZombiePresentationAsset(z),
                Animation = clip,
                AnimationAge = z.PresentationAnimationAge,
                Rate = SynchronizeAdventureZombieRate(z),
                Anchor = anchor,
                Image = image,
                Backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z)),
                Chilled = z.ChillRemaining > 0f || z.FrozenRemaining > 0f,
                Hypnotized = z.Hypnotized
            });
        }

        /// <summary>Performs lose adventure zombie head.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "inPool">The in pool.</param>
        /// <param name = "mowed">The mowed.</param>
        /// <param name = "flags">The flags.</param>
        internal void LoseAdventureZombieHead(Zombie z, int kind, bool inPool, bool mowed, uint flags)
        {
            // Keep the intact-pose animation/anchor snapshot before changing the walk/eat state.
            if ((flags & 16U) == 0 && (kind < 26 || kind > 31))
            {
                if (kind == 1 || kind == 33)
                {
                    AddAdventureZombiePart(z, kind, 11, "Zombie_flaghand", null);
                }

                AddAdventureZombiePart(z, kind, global::PlantsVsZombies.Adventure.AdventureZombiePartsRules.HeadParticle(kind, mowed, inPool), "anim_head1", global::PlantsVsZombies.Adventure.AdventureZombiePartsRules.HeadImage(kind));
                _world.Presentation.ZombiePartSoundSerial++;
                RequestAdventureZombieFoley(25);
            }

            z.Headless = true;
            z.PresentationButter = 0f;
            if (z.Kind == ZombieKind.Pogo)
            {
                _pogoZombieActions.BreakAdventurePogo(z, flags);
            }
        }

        /// <summary>Detaches a remaining shield from the held pose before a native death replaces it.</summary>
        /// <param name="zombie">The dying shield carrier whose intact pose is captured.</param>
        /// <param name="flags">The original DropShield mask; bit 1 suppresses the newspaper rip and bit 16 suppresses particles.</param>
        internal void DropAdventureZombieHeldShield(Zombie zombie, uint flags)
        {
            if (zombie.ShieldHealth <= 0f)
            {
                return;
            }

            var particle = AdventureZombieEquipmentRules.ShieldParticle(zombie);
            if (particle < 0)
            {
                return;
            }

            if ((flags & 16U) == 0)
            {
                var kind = zombie.ModeHead >= 26 ? zombie.ModeHead : (int)zombie.Kind;
                AddAdventureZombiePart(zombie, kind, particle, AdventureZombieEquipmentRules.ShieldTrack(zombie), null);
                if (zombie.Kind == ZombieKind.Newspaper && (flags & 1U) == 0)
                {
                    RequestAdventureZombieFoley(67);
                }
            }

            zombie.ShieldHealth = 0f;
        }

        /// <summary>Performs adventure presentation zombie mowed parts.</summary>
        /// <param name = "z">The z.</param>
        internal void AdventurePresentationZombieMowedParts(Zombie z)
        {
            var inPool = z.PresentationInPool ?? (_adventureModeRunner.Adventure.Level.IsWaterRow(z.Row) && z.X >= 0f && z.X < 9f);
            if (!inPool || !_adventureModeRunner.Adventure.Level.IsWaterRow(z.Row))
            {
                // PC MowDown 0x54E776/0x54E8CA drops shields, including a newspaper rip.
                DropAdventureZombieHeldShield(z, 0U);
            }

            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            var height = z.PresentationNativeHeight;
            var sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            if (!global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.CanLoseParts(kind, height, z.Airborne, sled))
            {
                return;
            }

            if (!z.Headless)
            {
                LoseAdventureZombieHead(z, kind, inPool, true, 0);
            }

            if (global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ShouldLoseArm(kind, 3, height, z.Airborne, sled, !z.PresentationArmLost, global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ShieldType(z), 1f, (int)z.StartingHealth))
            {
                LoseAdventureZombieArm(z, kind, 3, inPool, 0);
            }
        }

        /// <summary>Performs advance adventure zombie feedback.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureZombieFeedback(float dt)
        {
            for (var i = _world.Presentation.ArmDrops.Count - 1; i >= 0; i--)
            {
                _world.Presentation.ArmDrops[i].Age += dt;
                if (_world.Presentation.ArmDrops[i].Age >= .6f)
                {
                    _world.Presentation.ArmDrops.RemoveAt(i);
                }
            }

            AdvanceAdventureZombieParts(dt);
            foreach (Zombie z in _world.Zombies)
            {
                z.PresentationShieldRecoil = Math.Max(0f, z.PresentationShieldRecoil - dt);
                if (z.PresentationNewspaperSurpriseAge >= 0f)
                {
                    // Attachments keep their own source-rate clock, including while the body is chilled or frozen.
                    z.PresentationNewspaperSurpriseAge += dt;
                    if (z.PresentationNewspaperSurpriseAge >= 1.25f)
                    {
                        z.PresentationNewspaperSurpriseAge = -1f;
                    }
                }

                if (z.PresentationNewspaperGaspRemaining > 0f && z.FrozenRemaining <= 0f)
                {
                    z.PresentationNewspaperGaspRemaining = Math.Max(0f, z.PresentationNewspaperGaspRemaining - dt * (z.ChillRemaining > 0 ? .5f : 1f));
                    if (z.PresentationNewspaperGaspRemaining <= 0f)
                    {
                        z.PresentationNativePhase = 31;
                        RefreshAdventureZombieNativeSpeed(z);
                        // PC 0x541770: roar only after the gasp finishes, with a head and at most ten live opponents.
                        if (!z.Headless)
                        {
                            var count = 0;
                            foreach (var other in _world.Zombies)
                            {
                                if (other.Health > 0f && !other.Headless && !other.Hypnotized)
                                {
                                    count++;
                                }
                            }

                            if (count <= 10)
                            {
                                RequestAdventureZombieFoley(66);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Performs advance adventure zombie clock.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureZombieClock(Zombie z, float dt)
        {
            if (z.PlantZombieState != null)
            {
                var head = z.PlantZombieState;
                // Ice/chill change the body clock; ApplyButter alone explicitly stops the special child reanimation.
                if (z.PresentationButter <= 0 || head.Detached)
                {
                    head.HeadSeconds += dt;
                }
                z.PresentationPlantHeadAge = head.HeadSeconds;
            }
            EnsureAdventureZombieEquipment(z);
            if (z.BungeeCarrierId != 0)
            {
                return;
            }

            if (z.Kind == ZombieKind.Bungee && z.BungeeState != null)
            {
                if (z.FrozenRemaining <= 0f && z.PresentationButter <= 0f)
                {
                    z.BungeeState.AnimationSeconds += dt;
                    z.PresentationAnimationAge = z.BungeeState.AnimationSeconds;
                }

                return;
            }

            RefreshAdventureZombieNativeSpeed(z);
            var clip = SynchronizeAdventureZombieAnimation(z);
            z._presentationPreviousWalkCycle = z._presentationWalkLoops + (double)z._presentationWalkPhase;
            z._presentationTickSlow = (AdventureDancerRules.IsDancer(z)
                ? AdventureDancerRules.GroupAffected(z, _world.Zombies, true) : z.ChillRemaining > 0f) ? .5f : 1f;
            if (z.Kind == ZombieKind.Balloon && z.Airborne
                || z.Kind == ZombieKind.DolphinRider && (z.PresentationNativePhase == 52 || z.PresentationNativePhase == 54))
            {
                // PC UpdateAnimSpeed leaves the original clocks on flat ground tracks unaffected by chill.
                z._presentationTickSlow = 1f;
            }
            if (z.FrozenRemaining > 0f || z.PresentationButter > 0f)
            {
                return;
            }

            if (z.Kind == ZombieKind.Digger && z.PresentationNativePhase == 34)
            {
                // SetAnimRate(0) preserves the current digging frame during the axe-loss pause.
                return;
            }

            if (z.PresentationGarlicCounter >= 0 && z.PresentationGarlicCounter < 170)
            {
                return;
            }

            var step = dt * z._presentationTickSlow;
            if (AdventurePogoRules.IsBouncing(z))
            {
                var state = z.PogoState ??= new AdventurePogoState();
                state.AnimationSeconds = Math.Min(AdventurePogoRules.HeldAnimationSeconds, state.AnimationSeconds + step);
                z.PresentationAnimationAge = state.AnimationSeconds;
                return;
            }

            var rate = SynchronizeAdventureZombieRate(z);
            global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
            if (rate > 0f && global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z), clip, out data))
            {
                z._presentationWalkPhase += step * rate / data.Count;
                var loops = (int)Math.Floor(z._presentationWalkPhase);
                z._presentationWalkPhase -= loops;
                z._presentationWalkLoops += loops;
                z.PresentationAnimationAge = (z._presentationWalkLoops + z._presentationWalkPhase) * data.Count / rate;
            }
            else
            {
                z.PresentationAnimationAge += step;
            }

            if (z.PlantZombieState == null)
            {
                z.PresentationPlantHeadAge += step;
            }
        }

        /// <summary>Performs adventure zombie feedback phase.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure zombie feedback phase.</returns>
        internal int AdventureZombieFeedbackPhase(Zombie z)
        {
            if (z.PresentationNativePhase >= 0)
            {
                return z.PresentationNativePhase;
            }

            if (z.Kind == ZombieKind.Newspaper)
            {
                return z.ShieldHealth > 0 ? 29 : 31;
            }

            if (z.Kind == ZombieKind.Ladder && z.ShieldHealth > 0)
            {
                return 76;
            }

            if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility)
            {
                return 17;
            }

            if (z.Kind == ZombieKind.Pogo && !z.UsedAbility)
            {
                return 20;
            }

            if (z.Kind == ZombieKind.Balloon && z.Airborne)
            {
                return 73;
            }

            if (z.Kind == ZombieKind.Digger)
            {
                return z.AbilityPhase == 0 ? 32 : z.AbilityPhase == 1 ? 36 : 37;
            }

            if (z.Kind == ZombieKind.DolphinRider)
            {
                return z.UsedAbility ? (_adventureModeRunner.Adventure.Level.IsWaterRow(z.Row) ? 55 : 56) : _adventureModeRunner.Adventure.Level.IsWaterRow(z.Row) ? 53 : 51;
            }

            if (z.Kind == ZombieKind.Snorkel && z.Submerged && _adventureModeRunner.Adventure.Level.IsWaterRow(z.Row))
            {
                return 59;
            }

            if (z.Kind == ZombieKind.PoleVaulter && z.PresentationNativePhase == 12)
            {
                return 12;
            }

            if (z.Kind == ZombieKind.PoleVaulter && !z.UsedAbility)
            {
                return 11;
            }

            return 0;
        }

        /// <summary>Normalizes catalog paths once before any per-zombie simulation updates.</summary>
        /// <returns>Asset basenames indexed by the original zombie identifier.</returns>
        internal static string[] CreateZombiePresentationAssets()
        {
            var assets = new string[VerifiedDataCatalog.Zombies.Count];
            for (var i = 0; i < assets.Length; i++)
            {
                assets[i] = VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Zombies[i].ReanimationId, out var definition)
                    ? Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/'))
                    : "Zombie";
            }

            return assets;
        }

        /// <summary>Performs adventure zombie presentation asset.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure zombie presentation asset.</returns>
        internal static string AdventureZombiePresentationAsset(Zombie z)
        {
            var kind = (int)z.Kind;
            return kind >= 0 && kind < SimulationWorld.s_zombiePresentationAssets.Length ? SimulationWorld.s_zombiePresentationAssets[kind] : "Zombie";
        }

        /// <summary>Performs adventure presentation zombie animation.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure presentation zombie animation.</returns>
        internal string AdventurePresentationZombieAnimation(Zombie z)
        {
            if (z.PlantZombieState?.Detached == true)
            {
                return "anim_idle";
            }

            if (z.BungeeCarrierId != 0)
            {
                return "anim_idle";
            }

            if (z.Kind == ZombieKind.Zomboni && z.ZomboniState?.FlatTires == true)
            {
                return z.ZomboniState.Animation;
            }

            string asset = AdventureZombiePresentationAsset(z), clip;
            var motionClip = AdventureZombieMotionRules.Animation(z);
            if (z.Kind == ZombieKind.JackInTheBox && z.PresentationNativePhase == 16)
            {
                clip = "anim_pop";
            }
            else if (motionClip != null)
            {
                clip = motionClip;
            }
            else if (AdventureDancerRules.IsDancer(z))
            {
                // PC StopEating starts a walking pose even when garlic holds a dancer in a raised-arm phase.
                clip = z.PresentationGarlicCounter >= 70 && z.PresentationGarlicCounter < 170
                    ? "anim_walk" : AdventureDancerRules.Animation(z);
            }
            else if (z.PresentationNewspaperGaspRemaining > 0f)
            {
                clip = "anim_gasp";
            }
            else if (z.Kind == ZombieKind.Newspaper && z.ShieldHealth <= 0)
            {
                clip = z.Eating ? "anim_eat_nopaper" : "anim_walk_nopaper";
            }
            else if (z.Kind == ZombieKind.Ladder && z.ShieldHealth > 0)
            {
                clip = z.Eating ? "anim_laddereat" : "anim_ladderwalk";
            }
            else if (z.Eating)
            {
                clip = "anim_eat";
            }
            else if (z.Kind == ZombieKind.PoleVaulter && z.PresentationNativePhase == 12)
            {
                clip = "anim_jump";
            }
            else if (z.Kind == ZombieKind.PoleVaulter && !z.UsedAbility)
            {
                clip = "anim_run";
            }
            else if (z.Kind == ZombieKind.Pogo && !z.UsedAbility)
            {
                clip = "anim_pogo";
            }
            else if (z.Kind == ZombieKind.Digger && z.AbilityPhase == 0)
            {
                clip = "anim_dig";
            }
            else if (z.Kind == ZombieKind.Digger && z.AbilityPhase == 1)
            {
                clip = "anim_dizzy";
            }
            else if (z.Kind == ZombieKind.DolphinRider && !z.UsedAbility)
            {
                clip = _adventureModeRunner.Adventure.Level.IsWaterRow(z.Row) ? "anim_ride" : "anim_walkdolphin";
            }
            else if (z.Kind == ZombieKind.Snorkel && z.Submerged)
            {
                clip = "anim_swim";
            }
            else if (z.PresentationInPool == true && z.PoolState?.HasDuckyTube == true && z.PresentationNativeHeight == 0)
            {
                clip = "anim_swim";
            }
            else if (z.Kind == ZombieKind.Balloon && z.Airborne)
            {
                clip = "anim_idle";
            }
            else if (z.Kind == ZombieKind.Bobsled && z.PresentationNativePhase == 18)
            {
                clip = "anim_jump";
            }
            else if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility && z.PresentationNativePhase != 19)
            {
                clip = "anim_push";
            }
            else if (z.Kind == ZombieKind.Zomboni)
            {
                clip = "anim_drive";
            }
            else if (z.Kind == ZombieKind.Catapult)
            {
                clip = z.CatapultState?.Animation(z.PresentationNativePhase) ?? "anim_walk";
            }
            else if (z.Kind == ZombieKind.Bungee && z.BungeeState != null)
            {
                clip = z.BungeeState.Animation;
            }
            else if (z.BungeeCarrierId != 0)
            {
                clip = "anim_idle";
            }
            else
            {
                clip = z.PresentationWalk2 ? "anim_walk2" : "anim_walk";
            }

            global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
            if (z.Kind != ZombieKind.Bungee && !(z.Kind == ZombieKind.Ladder && z.PresentationNativePhase == 77)
                && !global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(asset, clip, out data))
            {
                clip = global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(asset, "anim_walk", out data) ? "anim_walk" : "anim_idle";
            }

            return clip;
        }

        /// <summary>Applies a native animation transition at a simulation-owned call site.</summary>
        /// <param name="z">The actor whose action state changed.</param>
        /// <returns>The selected native clip.</returns>
        internal string SynchronizeAdventureZombieAnimation(Zombie z)
        {
            if (z.BungeeCarrierId != 0 || z.Kind == ZombieKind.Zomboni && z.ZomboniState?.FlatTires == true)
            {
                return AdventurePresentationZombieAnimation(z);
            }
            var clip = AdventurePresentationZombieAnimation(z);
            if (!string.Equals(z.PresentationAnimation, clip, StringComparison.Ordinal))
            {
                if (z.Kind == ZombieKind.PoleVaulter && z.PresentationAnimation.Length > 0)
                {
                    z.PresentationPoleVaultBlendTicks = 20;
                }

                if (z.Kind == ZombieKind.Balloon && z.PresentationAnimation.Length > 0)
                {
                    z.PresentationBalloonBlendTicks = 20;
                }

                if (z.Kind == ZombieKind.DolphinRider
                    && (z.Eating || string.Equals(z.PresentationAnimation, "anim_eat", StringComparison.Ordinal)))
                {
                    (z.DolphinState ??= new AdventureDolphinState()).AnimationBlendTicks = 20;
                }

                if (AdventureDancerRules.IsDancer(z) && z.DancerState != null
                    && (z.Eating || string.Equals(z.PresentationAnimation, "anim_eat", StringComparison.Ordinal)))
                {
                    // PC StartEating and StopEating use twenty-tick blends independently of the current dance phase.
                    z.DancerState.AnimationBlendTicks = 20;
                }

                if (z.Kind == ZombieKind.Pogo && z.PogoState != null
                    && (z.Eating || string.Equals(z.PresentationAnimation, "anim_eat", StringComparison.Ordinal)))
                {
                    z.PogoState.AnimationBlendTicks = 20;
                }

                if (z.Kind == ZombieKind.Digger && z.DiggerState != null
                    && (z.Eating || string.Equals(z.PresentationAnimation, "anim_eat", StringComparison.Ordinal)))
                {
                    z.DiggerState.AnimationBlendTicks = 20;
                }

                if (z.PresentationBodyBlendTicks >= 0 && (z.Eating || z.PresentationAnimation == "anim_eat"))
                {
                    z.PresentationBodyBlendTicks = 20;
                }

                z.PresentationAnimation = clip;
                z.PresentationAnimationAge = 0f;
                z._presentationWalkPhase = 0f;
                z._presentationWalkLoops = 0;
                z._presentationMovementAnimation = null;
                z.PresentationMovementRate = -1f;
            }

            return clip;
        }

        /// <summary>Performs adventure presentation zombie rate.</summary>
        /// <param name = "z">The z.</param>
        /// <returns>The resulting adventure presentation zombie rate.</returns>
        internal float AdventurePresentationZombieRate(Zombie z)
        {
            if (z.PlantZombieState?.Detached == true)
            {
                return 12f;
            }

            if (z.BungeeCarrierId != 0)
            {
                return 0f;
            }

            if (z.Kind == ZombieKind.Bungee && z.BungeeState != null)
            {
                return z.BungeeState.AnimationRate;
            }

            if (z.Kind == ZombieKind.Zomboni && z.ZomboniState?.FlatTires == true)
            {
                return z.ZomboniState.Rate;
            }

            var clip = AdventurePresentationZombieAnimation(z);
            if (AdventurePogoRules.IsBouncing(z))
            {
                return 40f;
            }

            if (z.Kind == ZombieKind.Digger)
            {
                var phase = AdventureZombieFeedbackPhase(z);
                if (phase == 32)
                {
                    return 12f;
                }

                if (phase == 34 || phase == 35 && z.DiggerState?.PhaseTicks > 30)
                {
                    return 0f;
                }

                if (phase == 33)
                {
                    return z.DiggerState?.PhaseTicks > 30 ? 20f : 12f;
                }

                if (phase == 35 || phase == 36)
                {
                    return 12f;
                }
            }

            if (z.Kind == ZombieKind.Ladder && z.PresentationNativePhase == 77)
            {
                return 24f;
            }

            if (z.Kind == ZombieKind.JackInTheBox && z.PresentationNativePhase == 16)
            {
                return 28f;
            }

            if (z.Kind == ZombieKind.Balloon && z.PresentationNativePhase == 74)
            {
                return 24f;
            }

            if (AdventureDancerRules.IsDancer(z))
            {
                var fixedRate = AdventureDancerRules.FixedRate(z);
                if (fixedRate >= 0f)
                {
                    return fixedRate;
                }

                if (AdventureDancerRules.GroupAffected(z, _world.Zombies, false))
                {
                    return 0f;
                }
            }

            if (z.PresentationNewspaperGaspRemaining > 0f)
            {
                return 8f;
            }

            if (z.Kind == ZombieKind.PoleVaulter && z.PresentationNativePhase == 12)
            {
                return 24f;
            }

            if (z.Kind == ZombieKind.Snorkel)
            {
                if (z.PresentationNativePhase == 58)
                {
                    return 16f;
                }

                if (z.PresentationNativePhase == 60 || z.PresentationNativePhase == 62)
                {
                    return 24f;
                }

                if (z.PresentationNativePhase == 59)
                {
                    return 12f;
                }
            }

            if (z.Kind == ZombieKind.DolphinRider)
            {
                if (z.PresentationNativePhase == 52)
                {
                    return 16f;
                }

                if (z.PresentationNativePhase == 53)
                {
                    return 12f;
                }

                if (z.PresentationNativePhase == 54)
                {
                    return 10f;
                }
            }

            if (z.Kind == ZombieKind.Gargantuar && z.PresentationNativePhase == 69)
            {
                return 24f;
            }

            if (z.Kind == ZombieKind.Gargantuar && z.PresentationNativePhase == 70)
            {
                return 16f;
            }

            if (z.Kind == ZombieKind.Imp && z.PresentationNativePhase == 71)
            {
                return 18f;
            }

            if (z.Kind == ZombieKind.Imp && z.PresentationNativePhase == 72)
            {
                return 24f;
            }

            if (z.Eating)
            {
                return z.Kind == ZombieKind.PoleVaulter || z.Kind == ZombieKind.Balloon || z.Kind == ZombieKind.Imp || z.Kind == ZombieKind.Digger || z.Kind == ZombieKind.JackInTheBox || z.Kind == ZombieKind.Snorkel || z.Kind == ZombieKind.Yeti ? 20f : 36f;
            }

            if (z.Kind == ZombieKind.Catapult)
            {
                return z.CatapultState?.Rate(z.PresentationNativePhase) ?? 5.5f;
            }

            if (z.Kind == ZombieKind.Bobsled && z.PresentationNativePhase == 18)
            {
                return 20f;
            }

            if (z.Kind == ZombieKind.Bobsled && !z.UsedAbility && z.PresentationNativePhase != 19)
            {
                return 30f;
            }

            if (z.Kind == ZombieKind.Balloon && z.Airborne || z.Kind == ZombieKind.Zomboni)
            {
                return 12f;
            }

            if (z.PresentationMovementRate >= 0 && z._presentationMovementAnimation == clip)
            {
                return z.PresentationMovementRate;
            }

            return AdventureZombiePlaybackRate(z, clip, z.Speed);
        }

        /// <summary>Preserves native animation synchronization at simulation-owned rate requests.</summary>
        /// <param name="z">The active actor.</param>
        /// <returns>The native playback rate after any original action transition.</returns>
        internal float SynchronizeAdventureZombieRate(Zombie z)
        {
            if (z.PlantZombieState?.Detached == true)
            {
                return 12f;
            }

            if (z.BungeeCarrierId == 0 && !(z.Kind == ZombieKind.Bungee && z.BungeeState != null)
                && !(z.Kind == ZombieKind.Zomboni && z.ZomboniState?.FlatTires == true))
            {
                SynchronizeAdventureZombieAnimation(z);
            }
            return AdventurePresentationZombieRate(z);
        }

        /// <summary>Performs adventure zombie playback rate.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "clip">The clip.</param>
        /// <param name = "speed">The speed.</param>
        /// <returns>The resulting adventure zombie playback rate.</returns>
        internal static float AdventureZombiePlaybackRate(Zombie z, string clip, float speed)
        {
            global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
            if (!global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z), clip, out data))
            {
                return 12f;
            }

            return data.GroundDistance > .000001f ? speed * 80f * .01f * 47f * data.Count / data.GroundDistance / (AdventureDancerRules.Scale(z)) : data.SourceRate;
        }

        /// <summary>Performs adventure presentation walking distance.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "dt">The dt.</param>
        /// <param name = "fallbackDistance">The fallback distance.</param>
        /// <returns>The resulting adventure presentation walking distance.</returns>
        internal float AdventurePresentationWalkingDistance(Zombie z, float dt, float fallbackDistance)
        {
            if (dt <= 0f || z.PresentationNewspaperGaspRemaining > 0f || z.FrozenRemaining > 0f || z.PresentationButter > 0f || z.DropRemaining > 0f || z.Eating
                || AdventureDancerRules.IsDancer(z) && !AdventureDancerRules.CanWalk(z, _world.Zombies))
            {
                return 0f;
            }

            if (AdventurePogoRules.IsBouncing(z))
            {
                // PC 546730 moves bouncing pogo zombies by mVelX, independently of their held body clip.
                return fallbackDistance;
            }

            if (z.Kind == ZombieKind.Balloon && z.Airborne)
            {
                // PC 546730 uses native velocity in flight and the pop's constant ground track while landing.
                return z.PresentationNativePhase == 74 ? 0f : z.Speed * dt * (z.ChillRemaining > 0f ? .4f : 1f);
            }

            if (z.Kind == ZombieKind.DolphinRider && z.PresentationNativePhase == 52)
            {
                // The authored entry ground is constant; do not replace its sixteen-fps animation clock with the source rate.
                return 0f;
            }

            if (z.Kind == ZombieKind.DolphinRider && z.PresentationNativePhase == 54)
            {
                // PC 546730 takes the native velocity directly during a dolphin jump, even while chilled.
                return z.Speed * dt;
            }

            var clip = SynchronizeAdventureZombieAnimation(z);
            int phase = AdventureZombieFeedbackPhase(z), kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            var sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            global::PlantsVsZombies.Adventure.AdventureZombieGroundClip? data;
            if (!global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.UsesGround(kind, phase, sled) || !global::PlantsVsZombies.Adventure.AdventureZombieGroundData.TryGet(AdventureZombiePresentationAsset(z), clip, out data) || data.Ground == null)
            {
                return fallbackDistance;
            }

            var slow = z._presentationTickSlow;
            var rate = AdventureZombiePlaybackRate(z, clip, fallbackDistance / (dt * slow));
            z._presentationMovementAnimation = clip;
            z.PresentationMovementRate = rate;
            if (rate > 0f)
            {
                z.PresentationAnimationAge = (z._presentationWalkLoops + z._presentationWalkPhase) * data.Count / rate;
            }

            return data.VelocityAtPhase(z._presentationWalkPhase, rate * slow) * (AdventureDancerRules.Scale(z)) * (dt * 100f) / 80f;
        }

        /// <summary>Emits the original football running dust at each authored foot contact.</summary>
        /// <param name="z">The walking zombie whose animation clock was advanced this tick.</param>
        /// <param name="gridX">The zombie's horizontal grid position before this tick's movement.</param>
        internal void RecordAdventureZombieFootsteps(Zombie z, float gridX)
        {
            if (z.Kind != ZombieKind.Football || z.Wave == -4 || z.PresentationButter > 0f)
            {
                return;
            }

            // PC UpdateZombieWalking 0x546730: these independent events use mX/mY, without mirroring or scaling.
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(gridX, AdventureDancerRules.Scale(z));
            var nativeY = (int)_biteSystem.AdventureBiteNativeY(z, nativeX);
            var before = z._presentationPreviousWalkCycle;
            var after = z._presentationWalkLoops + (double)z._presentationWalkPhase;
            if (AdventureZombieBiteRules.Crossed(before, after, .03f))
            {
                AddAdventureZombieFootstep(z, nativeX + 81, nativeY + 106);
            }

            if (AdventureZombieBiteRules.Crossed(before, after, .61f))
            {
                AddAdventureZombieFootstep(z, nativeX + 87, nativeY + 110);
            }
        }

        /// <summary>Retains one independent twenty-tick Dust_Foot particle instance.</summary>
        /// <param name="z">The zombie whose row supplies the original render layer.</param>
        /// <param name="x">The unscaled original horizontal particle origin.</param>
        /// <param name="y">The unscaled original vertical particle origin.</param>
        internal void AddAdventureZombieFootstep(Zombie z, int x, int y)
        {
            var id = ++_world.Presentation._effectId;
            _world.Presentation.FootstepEffects.Add(new AdventureZombieFootstepEffect
            {
                Id = id,
                Row = z.Row,
                Seed = unchecked(z.Id * 397 ^ id),
                X = x,
                Y = y
            });
        }

        /// <summary>Performs adventure presentation zombie hit.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "damage">The damage.</param>
        /// <param name = "bypassShield">The bypass shield.</param>
        /// <param name = "damageFlags">The damage flags.</param>
        internal void AdventurePresentationZombieHit(Zombie z, float damage, bool bypassShield = false, uint damageFlags = 0)
        {
            EnsureAdventureZombieEquipment(z);
            z._presentationArmorBeforeHit = z.ArmorHealth;
            z._presentationShieldBeforeHit = z.ShieldHealth;
            z.PresentationHealthBeforeHit = z.Health;
            if (damage <= 0f || (damageFlags & 8U) != 0)
            {
                return;
            }

            if (z.Kind == ZombieKind.Zomboni)
            {
                // PC TakeBodyDamage 0x54D080 requests ShieldHit on every unsuppressed body hit.
                RequestAdventureZombieFoley(46);
            }

            var remaining = damage;
            if ((!bypassShield || (damageFlags & 2U) != 0) && z.ShieldHealth > 0f)
            {
                if ((damageFlags & 2U) == 0 && (z.Kind == ZombieKind.ScreenDoor || z.Kind == ZombieKind.Ladder))
                {
                    RequestAdventureZombieFoley(46);
                }

                z.PresentationArmorFlash = .25f;
                if ((damageFlags & 2U) == 0)
                {
                    z.PresentationShieldRecoil = .12f;
                }

                if ((damageFlags & 2U) == 0)
                {
                    remaining -= Math.Min(remaining, z.ShieldHealth);
                }
            }

            // Helm damage uses the body's additive group, including the still attached helm.
            if (remaining > 0f)
            {
                z.PresentationFlash = .25f;
            }
        }

        /// <summary>Performs adventure presentation zombie body damaged.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "damageFlags">The damage flags.</param>
        internal void AdventurePresentationZombieBodyDamaged(Zombie z, uint damageFlags = 0)
        {
            if (z.Kind == ZombieKind.Catapult)
            {
                var state = z.CatapultState ??= new AdventureCatapultState();
                if (z.Health > 0f && state.SmokeStartAge < 0f
                    && AdventureZombieEquipmentRules.DamageIndex(z.Health, (int)z.StartingHealth) == 2)
                {
                    state.SmokeStartAge = z.Age;
                }

                return;
            }

            if (z.Kind == ZombieKind.Zomboni)
            {
                var state = z.ZomboniState ??= new AdventureZomboniState();
                if (z.Health > 0f && state.SmokeStartAge < 0f
                    && AdventureZombieEquipmentRules.DamageIndex(z.Health, (int)z.StartingHealth) == 2)
                {
                    state.SmokeStartAge = z.Age;
                }

                return;
            }

            var kind = z.ModeHead >= 26 ? z.ModeHead : (int)z.Kind;
            var height = z.PresentationNativeHeight;
            if (height < 0 && _world.ModeSession != null && _world.ModeSession.Definition.Mechanic == global::PlantsVsZombies.Modes.ModeMechanic.Aquarium)
            {
                height = 10;
            }

            var phase = AdventureZombieFeedbackPhase(z);
            var sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
            var inPool = z.PresentationInPool ?? (_adventureModeRunner.Adventure.Level.IsWaterRow(z.Row) && z.X >= 0f && z.X < 9f);
            // Burn death selects its own native charred attachment; don't turn it into flesh parts.
            if (z.PresentationDeath == global::PlantsVsZombies.Adventure.AdventureZombieDeathPresentation.Burned && z.Health <= 0f)
            {
                return;
            }

            if (global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ShouldLoseArm(kind, phase, height, z.Airborne, sled, !z.PresentationArmLost, global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ShieldType(z), z.Health, (int)z.StartingHealth))
            {
                LoseAdventureZombieArm(z, kind, phase, inPool, damageFlags);
            }

            // Unlike the arm test this also runs after a lethal body hit (PC 0x546160).
            if (global::PlantsVsZombies.Adventure.AdventureZombiePartsRules.ShouldLoseHead(kind, height, z.Airborne, sled, !z.Headless, z.Health, (int)z.StartingHealth))
            {
                LoseAdventureZombieHead(z, kind, inPool, phase == 3, damageFlags);
            }
        }

        /// <summary>Performs lose adventure zombie arm.</summary>
        /// <param name = "z">The z.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "phase">The phase.</param>
        /// <param name = "inPool">The in pool.</param>
        /// <param name = "damageFlags">The damage flags.</param>
        internal void LoseAdventureZombieArm(Zombie z, int kind, int phase, bool inPool, uint damageFlags)
        {
            z.PresentationArmLost = true;
            if (inPool || (damageFlags & 16U) != 0)
            {
                return;
            }

            _world.Presentation.ZombiePartSoundSerial++;
            RequestAdventureZombieFoley(25);
            var clip = SynchronizeAdventureZombieAnimation(z);
            _world.Presentation.ArmDrops.Add(new global::PlantsVsZombies.Adventure.AdventureZombieArmDrop {
                Id = ++_world.Presentation._effectId,
                Seed = z.Id * 397 ^ _world.Presentation._effectId,
                NativeKind = kind,
                NativePhase = z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Pogo ? z.PresentationNativePhase : -1,
                NativeAltitude = z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Pogo ? z.PresentationAltitude : 0f,
                X = z.X,
                Row = z.Row,
                Scale = AdventureDancerRules.Scale(z),
                Asset = AdventureZombiePresentationAsset(z),
                Animation = clip,
                AnimationAge = z.PresentationAnimationAge,
                Rate = SynchronizeAdventureZombieRate(z),
                Anchor = global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ArmAnchor(kind),
                Image = global::PlantsVsZombies.Adventure.AdventureZombieFeedbackRules.ArmImage(kind),
                Mowed = phase == 3,
                Backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z)),
                Chilled = z.ChillRemaining > 0f || z.FrozenRemaining > 0f,
                Hypnotized = z.Hypnotized
            });
        }

        /// <summary>Performs request adventure zombie foley.</summary>
        /// <param name = "id">The id.</param>
        /// <param name="pitch">The explicit original pitch adjustment in semitones.</param>
        /// <param name="presentationTick">The advancing cutscene tick when the battle elapsed clock is frozen.</param>
        internal void RequestAdventureZombieFoley(int id, float pitch = 0f, int? presentationTick = null)
        {
            var sound = new AdventureZombieSoundEvent
            {
                Id = ++_world.Presentation._effectId, Foley = id, Tick = Math.Max(_world._nativeFeedbackTick, presentationTick ?? _world._nativeFeedbackTick), Pitch = pitch
            };
            if (id >= 0)
            {
                _world._foleyRandom.Resolve(sound, _world._random, _world.Rules.AdventureSoundMuted);
            }

            _world.Presentation.ZombieSounds.Add(sound);
        }

        /// <summary>Performs request adventure projectile impact sound.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "z">The z.</param>
        internal void RequestAdventureProjectileImpactSound(Pea p, Zombie? z)
        {
            var type = p.Butter ? 12 : p.Fire ? 6 : p.SourceKind == PlantKind.KernelPult ? 10 : p.ChillSplash ? 5 : p.Splash ? 3 : 0;
            var helm = z != null && z.ArmorHealth > 0 ? global::PlantsVsZombies.Adventure.AdventureZombieEquipmentRules.HelmType(z) : 0;
            // PC fire splash is disabled against vehicles and door/ladder shields.
            var fireSplash = z != null && z.Kind != ZombieKind.Zomboni && z.Kind != ZombieKind.Catapult && (z.ShieldHealth <= 0 || z.Kind != ZombieKind.ScreenDoor && z.Kind != ZombieKind.Ladder);
            foreach (int id in global::PlantsVsZombies.Adventure.AdventureZombieSoundRules.ProjectileFoley(type, helm, fireSplash))
            {
                RequestAdventureZombieFoley(id);
            }
        }

        /// <summary>Runs the PC playing update's ambient vocal attempt before immobilization and action checks.</summary>
        /// <param name="zombie">The current original actor whose vocal counter is decremented.</param>
        internal void AdvanceAdventureZombieGroan(Zombie zombie)
        {
            if (_world.State != MatchState.Playing || _adventureModeRunner.Adventure.Phase != AdventurePhase.Playing
                || zombie.Wave == -2 || zombie.Wave == -3 || zombie.Wave == -4
                || zombie.Health <= 0f || zombie.PresentationNativePhase >= 1 && zombie.PresentationNativePhase <= 3)
            {
                return;
            }

            // PC 547016..547032 tests equality after decrement, unlike the mobile port's repeated <= 0 attempts.
            zombie.GroanTicks--;
            if (zombie.GroanTicks != 0 || _world._random.Next(_world.Zombies.Count) != 0 || zombie.Headless
                || zombie.Kind == ZombieKind.Boss || _adventureModeRunner.Adventure.RewardPresentation != null)
            {
                return;
            }

            var pitch = _adventureModeRunner.Adventure.Level.Mode == AdventureMode.LittleTrouble ? 40f + (float)_world._random.NextDouble() * 10f : 0f;
            // PC 547091 selects the low giant vocal before considering the original one-in-five actor variant.
            RequestAdventureZombieFoley(zombie.Kind == ZombieKind.Gargantuar ? 70 : zombie.NativeVariant ? 12 : 11,
                zombie.Kind == ZombieKind.Gargantuar ? 0f : pitch);
            zombie.GroanTicks = 500 + _world._random.Next(1000);
        }
    }
}
