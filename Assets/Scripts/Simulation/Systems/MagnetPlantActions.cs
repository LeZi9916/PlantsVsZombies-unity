#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns MagnetPlantActions responsibilities for one deterministic match.</summary>
    internal sealed class MagnetPlantActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides DiggerZombieActions operations.</summary>
        private DiggerZombieActions _diggerZombieActions = null!;
        /// <summary>Provides PogoZombieActions operations.</summary>
        private PogoZombieActions _pogoZombieActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal MagnetPlantActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="diggerZombieActions">The DiggerZombieActions collaborator.</param>
        /// <param name="pogoZombieActions">The PogoZombieActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(BiteSystem biteSystem, DiggerZombieActions diggerZombieActions, PogoZombieActions pogoZombieActions, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _biteSystem = biteSystem;
            _diggerZombieActions = diggerZombieActions;
            _pogoZombieActions = pogoZombieActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Gets the original body rectangle used by the magnet's circular reach and nearest-target priority.</summary>
        /// <param name="zombie">The potential magnetic target.</param>
        /// <returns>The body rectangle after native altitude and clipping.</returns>
        internal NativeRect AdventureMagnetBodyRectangle(Zombie zombie)
        {
            var scale = AdventureDancerRules.Scale(zombie);
            var x = AdventureZombieNativeRules.NativePositionX(zombie.X, scale);
            var altitude = zombie.PoolState != null ? AdventureZombiePoolRules.BodyAltitude(zombie) : zombie.PresentationAltitude;
            var y = (int)_biteSystem.AdventureBiteNativeY(zombie, x) + (int)-altitude;
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie, y);
            var clip = zombie.PoolState != null ? zombie.PresentationBiteClipHeight : (int)AdventureZombieMotionRules.ClipHeight(zombie, scale);
            return new NativeRect(body.X, body.Y, body.Width, Math.Max(0, body.Height - (clip > -100 ? clip : 0)));
        }

        /// <summary>Runs PC UpdateMagnetShroom 470170, including the original independent body and cooldown clocks.</summary>
        /// <param name="plant">The awake Magnet-shroom updated at one hundred hertz.</param>
        internal void AdvanceAdventureMagnetShroom(Plant plant)
        {
            var state = plant.MagnetPresentation;
            if (state == null)
            {
                state = _world.LifetimePool.Rent<AdventureMagnetState>();
                state.AnimationRate = plant.PresentationRate > 0f ? plant.PresentationRate : 12f;
                plant.MagnetPresentation = state;
            }
            state.AnimationSeconds += .01f;
            if (state.Item != null)
            {
                state.Item.AgeTicks++;
            }

            if (state.CooldownTicks > 0)
            {
                state.CooldownTicks--;
            }

            plant.ActionTimer = state.CooldownTicks * .01f;
            plant.PresentationBiteNativeState = state.NativePhase;
            if (state.NativePhase == 27)
            {
                if (state.CooldownTicks == 0)
                {
                    state.NativePhase = 1;
                    plant.PresentationBiteNativeState = 1;
                    state.AnimationSeconds = 0f;
                    state.AnimationRate = _zombieFeedbackSystem.AdventureZombieIsIZombie ? 0f : 10f + (float)_world._random.NextDouble() * 5f;
                    state.BlendTicks = 30;
                    state.Item = null;
                }

                return;
            }

            if (state.NativePhase == 26)
            {
                if (state.AnimationSeconds * 12f / AdventureMagnetRules.SuckingFrames >= 1f)
                {
                    state.NativePhase = 27;
                    plant.PresentationBiteNativeState = 27;
                    state.AnimationSeconds = 0f;
                    state.AnimationRate = _zombieFeedbackSystem.AdventureZombieIsIZombie ? 0f : 2f;
                    state.BlendTicks = 20;
                }

                return;
            }

            var plantX = 40 + plant.Column * 80;
            var plantY = _biteSystem.AdventurePlantEatingRectangle(plant).Y;
            Zombie? target = null;
            var nearest = float.MaxValue;
            foreach (var zombie in _world.Zombies)
            {
                var body = AdventureMagnetBodyRectangle(zombie);
                if (!AdventureMagnetRules.CanTarget(zombie, body, plant.Row)
                    || !AdventureMagnetRules.InRange(plantX, plantY, body, zombie.Eating))
                {
                    continue;
                }

                var distance = AdventureMagnetRules.TargetDistance(plantX, plantY, body, Math.Abs(zombie.Row - plant.Row));
                if (distance < nearest)
                {
                    nearest = distance;
                    target = zombie;
                }
            }

            if (target != null)
            {
                AttractAdventureMagnetMetal(plant, target);
                return;
            }

            Plant? ladder = null;
            var ladderScore = float.MaxValue;
            foreach (var other in _world.Plants)
            {
                if (!other.HasLadder || other.Health <= 0f)
                {
                    continue;
                }

                var dx = Math.Abs(other.Column - plant.Column);
                var dy = Math.Abs(other.Row - plant.Row);
                var distance = Math.Max(dx, dy);
                var score = distance + dy * .05f;
                if (distance <= 2 && score < ladderScore)
                {
                    ladder = other;
                    ladderScore = score;
                }
            }

            if (ladder != null)
            {
                foreach (var other in _world.Plants)
                {
                    if (other.Row == ladder.Row && other.Column == ladder.Column)
                    {
                        other.HasLadder = false;
                    }
                }

                var pooledRecord1 = _world.LifetimePool.Rent<AdventureMagnetItem>();
                pooledRecord1.NativeKind = 20;
                pooledRecord1.X = 40 + ladder.Column * 80 + 40;
                pooledRecord1.Y = _biteSystem.AdventurePlantEatingRectangle(ladder).Y;
                pooledRecord1.PositionResolved = true;
                pooledRecord1.DestinationX = 10f + RandomAdventureMagnetOffset();
                pooledRecord1.DestinationY = RandomAdventureMagnetOffset();
                BeginAdventureMagnetAttraction(plant, pooledRecord1);
            }
        }

        /// <summary>Gets the original uniform destination jitter for a magnetic item.</summary>
        /// <returns>A native pixel offset in minus ten through plus ten.</returns>
        internal float RandomAdventureMagnetOffset()
        {
            return -10f + (float)_world._random.NextDouble() * 20f;
        }

        /// <summary>Starts PC 46F9C0's original shooting clip, metal item and single attraction Foley.</summary>
        /// <param name="plant">The ready Magnet-shroom.</param>
        /// <param name="item">The original removed metal item.</param>
        internal void BeginAdventureMagnetAttraction(Plant plant, AdventureMagnetItem item)
        {
            var state = plant.MagnetPresentation!;
            state.NativePhase = 26;
            state.CooldownTicks = AdventureMagnetRules.CooldownTicks;
            state.AnimationSeconds = 0f;
            state.AnimationRate = 12f;
            state.BlendTicks = 20;
            state.Item = item;
            plant.PresentationBiteNativeState = 26;
            plant.ActionTimer = 15f;
            _zombieFeedbackSystem.RequestAdventureZombieFoley(42);
        }

        /// <summary>Captures the original GetTrackPosition input after its species-specific magnetic disarming operation.</summary>
        /// <param name="zombie">The disarmed actor whose original artwork supplies the source point.</param>
        /// <param name="track">The original retained metal track.</param>
        /// <returns>A source-pose snapshot which cannot follow later target movement or death.</returns>
        internal AdventureZombiePartDrop CaptureAdventureMagnetSource(Zombie zombie, string track)
        {
            var animation = _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
            var rate = _zombieFeedbackSystem.SynchronizeAdventureZombieRate(zombie);
            var age = zombie.PresentationAnimationAge;
            if (zombie.Kind == ZombieKind.Digger && zombie.PresentationNativePhase == 34 && zombie.DiggerState != null)
            {
                rate = 12f;
                age = zombie.DiggerState.PausedDigSeconds;
            }

            var pooledRecord2 = _world.LifetimePool.Rent<AdventureZombiePartDrop>();
            pooledRecord2.NativeKind = (int)zombie.Kind;
            pooledRecord2.NativePhase = zombie.PresentationNativePhase;
            pooledRecord2.NativeAltitude = zombie.PoolState != null ? AdventureZombiePoolRules.BodyAltitude(zombie) : zombie.PresentationAltitude;
            pooledRecord2.Row = zombie.Row;
            pooledRecord2.X = zombie.X;
            pooledRecord2.Scale = AdventureDancerRules.Scale(zombie);
            pooledRecord2.Asset = ZombieFeedbackSystem.AdventureZombiePresentationAsset(zombie);
            pooledRecord2.Animation = animation;
            pooledRecord2.AnimationAge = age;
            pooledRecord2.Rate = rate;
            pooledRecord2.Anchor = track;
            pooledRecord2.Backwards = zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized
                    || zombie.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(zombie));
            return pooledRecord2;
        }

        /// <summary>Applies PC 46F9C0's per-item removal without damaging unrelated equipment or creating duplicate debris.</summary>
        /// <param name="plant">The Magnet-shroom taking the selected nearest metal.</param>
        /// <param name="zombie">The original eligible source zombie.</param>
        internal void AttractAdventureMagnetMetal(Plant plant, Zombie zombie)
        {
            _zombieFeedbackSystem.EnsureAdventureZombieEquipment(zombie);
            var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            var nativeY = _biteSystem.AdventureBiteNativeY(zombie, nativeX);
            var item = _world.LifetimePool.Rent<AdventureMagnetItem>();
            item.X = nativeX;
            item.Y = nativeY;
            var helm = AdventureZombieEquipmentRules.HelmType(zombie);
            if (zombie.ArmorHealth > 0f && (helm == 2 || helm == 3))
            {
                var stage = AdventureZombieEquipmentRules.DamageIndex(zombie.ArmorHealth, (int)zombie.PresentationMaxArmor);
                zombie.ArmorHealth = 0f;
                item.NativeKind = (helm == 2 ? 1 : 4) + stage;
                if (helm == 2)
                {
                    item.Source = CaptureAdventureMagnetSource(zombie, "anim_bucket");
                    item.AnchorImage = "IMAGE_REANIM_ZOMBIE_BUCKET1";
                    item.DestinationX = 25f;
                }
                else
                {
                    item.X = nativeX + 37f;
                    item.Y = nativeY - 60f;
                    item.PositionResolved = true;
                    item.DestinationX = 20f;
                }

                item.DestinationY = 20f;
            }
            else if (zombie.ShieldHealth > 0f && (zombie.Kind == ZombieKind.ScreenDoor || zombie.Kind == ZombieKind.Ladder))
            {
                var stage = AdventureZombieEquipmentRules.DamageIndex(zombie.ShieldHealth, (int)zombie.PresentationMaxShield);
                zombie.ShieldHealth = 0f;
                zombie.PresentationNativePhase = 0;
                if (!zombie.Eating)
                {
                    // PC 54EEA0/46F9C0 uses StartWalkAnim(0) after either held shield is removed.
                    zombie.PresentationAnimation = string.Empty;
                    zombie.PresentationBodyBlendTicks = 0;
                    _zombieFeedbackSystem.PickAdventureZombieNativeSpeed(zombie, 0);
                }
                else
                {
                    // A latched meal keeps its native velocity until the later StopEating walk reset.
                    zombie._presentationSpeedPhase = 0;
                    zombie._presentationSpeedKind = zombie.ModeHead >= 26 ? zombie.ModeHead : (int)zombie.Kind;
                }

                if (zombie.Kind == ZombieKind.ScreenDoor)
                {
                    item.NativeKind = 7 + stage;
                    item.Source = CaptureAdventureMagnetSource(zombie, "anim_screendoor");
                    item.AnchorImage = "IMAGE_REANIM_ZOMBIE_SCREENDOOR1";
                }
                else
                {
                    zombie.UsedAbility = true;
                    item.NativeKind = 14 + stage;
                    item.X = nativeX + 31f;
                    item.Y = nativeY + 20f;
                    item.PositionResolved = true;
                    item.AnchorImage = "IMAGE_REANIM_ZOMBIE_LADDER_5";
                }

                item.DestinationX = 30f;
            }
            else if (zombie.Kind == ZombieKind.Pogo)
            {
                _pogoZombieActions.BreakAdventurePogo(zombie, 16U);
                item.NativeKind = zombie.PresentationArmLost ? 12 : 10;
                item.Source = CaptureAdventureMagnetSource(zombie, "Zombie_pogo_stick");
                item.AnchorImage = "IMAGE_REANIM_ZOMBIE_POGO_STICKDAMAGE1";
                item.SourceOffsetX = 40f;
                item.SourceOffsetY = 84f;
                item.DestinationX = 44f;
                item.DestinationY = 10f;
            }
            else if (zombie.Kind == ZombieKind.JackInTheBox && zombie.PresentationNativePhase == 15)
            {
                zombie.UsedAbility = true;
                zombie.AbilityTimer = float.MaxValue;
                _zombieFeedbackSystem.PickAdventureZombieNativeSpeed(zombie, 15);
                zombie.PresentationNativePhase = 0;
                zombie.JackInTheBoxTicks = 0;
                item.NativeKind = 13;
                item.Source = CaptureAdventureMagnetSource(zombie, "Zombie_jackbox_box");
                item.AnchorImage = "IMAGE_REANIM_ZOMBIE_JACKBOX_BOX";
                item.DestinationX = 20f;
                item.DestinationY = 15f;
            }
            else if (zombie.Kind == ZombieKind.Digger)
            {
                _diggerZombieActions.LoseAdventureDiggerAxe(zombie);
                item.NativeKind = 21;
                item.Source = CaptureAdventureMagnetSource(zombie, "Zombie_digger_pickaxe");
                item.AnchorImage = "IMAGE_REANIM_ZOMBIE_DIGGER_PICKAXE";
                item.DestinationX = 45f;
                item.DestinationY = 15f;
            }

            item.DestinationX += RandomAdventureMagnetOffset();
            item.DestinationY += RandomAdventureMagnetOffset();
            BeginAdventureMagnetAttraction(plant, item);
        }
    }
}
