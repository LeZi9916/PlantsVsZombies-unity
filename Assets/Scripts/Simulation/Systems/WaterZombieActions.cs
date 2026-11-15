#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns WaterZombieActions responsibilities for one deterministic match.</summary>
    internal sealed class WaterZombieActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides ColdSystem operations.</summary>
        private ColdSystem _coldSystem = null!;
        /// <summary>Provides VaultZombieActions operations.</summary>
        private VaultZombieActions _vaultZombieActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal WaterZombieActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="coldSystem">The ColdSystem collaborator.</param>
        /// <param name="vaultZombieActions">The VaultZombieActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        /// <param name="zombieLifecycle">The ZombieLifecycle collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, BoardQueries boardQueries, ColdSystem coldSystem, VaultZombieActions vaultZombieActions, ZombieFeedbackSystem zombieFeedbackSystem, ZombieLifecycle zombieLifecycle)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _boardQueries = boardQueries;
            _coldSystem = coldSystem;
            _vaultZombieActions = vaultZombieActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieLifecycle = zombieLifecycle;
        }

        /// <summary>Changes a dolphin clip while preserving the native velocity unless the original branch picks a new speed.</summary>
        /// <param name="z">The dolphin rider changing its original phase.</param>
        /// <param name="phase">The original phase identifier from fifty one through fifty six.</param>
        internal void SetAdventureDolphinPhase(Zombie z, int phase)
        {
            z.PresentationNativePhase = phase;
            var state = z.DolphinState ??= new AdventureDolphinState();
            state.AnimationBlendTicks = phase == 52 ? 20 : 0;
            if (phase == 55 || phase == 56)
            {
                // PC StartWalkAnim(0) replaces the jump/swim pose before any same-tick meal blend.
                state.WalkingPoseVersion++;
                z.PresentationPreviousX = z.X;
            }

            z.PresentationAnimation = string.Empty;
            _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(z);
            if (phase == 51 || phase == 55 || phase == 56)
            {
                _zombieFeedbackSystem.PickAdventureZombieNativeSpeed(z, phase);
            }
            else
            {
                z._presentationSpeedPhase = phase;
                z._presentationSpeedKind = (int)z.Kind;
                z.Speed = AdventureZombieNativeRules.GridSpeed(z.PresentationNativeVelocity);
            }
        }

        /// <summary>Checks PC ShouldTriggerTimedEvent's inclusive previous and exclusive current one-shot interval.</summary>
        /// <param name="z">The actor whose native animation was advanced this update.</param>
        /// <param name="progress">The updated normalized one-shot animation time.</param>
        /// <param name="marker">The original normalized event marker.</param>
        /// <returns>Whether the original event falls in this update's animation interval.</returns>
        internal static bool AdventureDolphinTimedEvent(Zombie z, double progress, float marker)
        {
            return z._presentationPreviousWalkCycle <= marker && progress > marker;
        }

        /// <summary>Runs PC UpdateZombieDolphinRider's bank, entry, ride, jump, and dismounted swimming branches.</summary>
        /// <param name="z">The actor whose original dolphin phase may be active.</param>
        /// <param name="dt">The active fixed update interval in seconds.</param>
        /// <returns>False because the common walking update still runs after the original dolphin action.</returns>
        internal bool AdvanceAdventureDolphin(Zombie z, float dt)
        {
            if (z.Kind != ZombieKind.DolphinRider)
            {
                return false;
            }

            var state = z.DolphinState ??= new AdventureDolphinState();
            var phase = z.PresentationNativePhase;
            if (phase == 54 && state.JumpTicks > 0)
            {
                state.JumpTicks--;
            }

            if (z.TangleKelpGrab != null || z.PresentationNativeHeight == 3)
            {
                // PC 541C70 returns before changing a kelp target's existing dolphin phase.
                return false;
            }

            var backwards = z.PresentationWalkingBackwards ?? z.Hypnotized;
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
            var progress = z._presentationWalkLoops + (double)z._presentationWalkPhase;
            if (phase == 51 && !backwards && nativeX > 700 && nativeX <= 720)
            {
                SetAdventureDolphinPhase(z, 52);
            }
            else if (phase == 52)
            {
                if (AdventureDolphinTimedEvent(z, progress, .56f))
                {
                    AddAdventureZombieSplash(z, -83f, 73f, 1.2f, .8f, 53);
                }

                if (z._presentationWalkLoops > 0)
                {
                    z.X -= 70f / 80f;
                    z.PresentationPreviousX -= 70f / 80f;
                    z.PresentationInPool = true;
                    z.PresentationAltitude = 0f;
                    SetAdventureDolphinPhase(z, 53);
                }
            }
            else if (phase == 53)
            {
                if (nativeX <= 10)
                {
                    z.PresentationNativeHeight = 2;
                    z.PresentationAltitude = -40f;
                    SetAdventureDolphinPhase(z, 51);
                    AddAdventureZombieSplash(z, 23f, 78f, .8f, .8f, 52, 65f);
                }
                else if (!z.Headless)
                {
                    var target = _boardQueries.FindAdventureNativePlantTarget(z, AdventureBiteAttack.Vault);
                    if (target != null)
                    {
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(50);
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(52);
                        state.JumpTicks = 120;
                        z.Eating = false;
                        z.PresentationNativeVelocity = .5f;
                        SetAdventureDolphinPhase(z, 54);
                    }
                }
            }
            else if (phase == 54)
            {
                z.PresentationAltitude = Math.Max(0, Math.Min(120, 120 - state.JumpTicks)) / 12f;
                var complete = false;
                if (AdventureDolphinTimedEvent(z, progress, .3f))
                {
                    var barrier = _boardQueries.FindAdventureNativePlantTarget(z, AdventureBiteAttack.Vault);
                    if (barrier != null && barrier.Kind == PlantKind.TallNut)
                    {
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(48);
                        _world.Presentation.PlantBiteEffects.Add(new AdventurePlantBiteEffect
                        {
                            Id = ++_world.Presentation._effectId, Row = z.Row, ParticleId = 29,
                            NativeX = 40 + barrier.Column * 80 + 60,
                            NativeY = 80 + barrier.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85 : 100) - 20,
                            Seed = z.Id
                        });
                        z.X = AdventureZombieNativeRules.GridX(40 + barrier.Column * 80 + 25, AdventureDancerRules.Scale(z));
                        z.PresentationAltitude = 30f;
                        z.PresentationNativeHeight = 7;
                        complete = true;
                    }
                }
                else if (AdventureDolphinTimedEvent(z, progress, .49f))
                {
                    AddAdventureZombieSplash(z, -63f, 73f, 1.2f, .8f, 53);
                    z.PresentationNativeVelocity = 0f;
                    z.Speed = 0f;
                }
                else if (z._presentationWalkLoops > 0)
                {
                    z.X -= 94f / 80f;
                    z.PresentationAltitude = 0f;
                    complete = true;
                }

                if (complete)
                {
                    z.UsedAbility = true;
                    SetAdventureDolphinPhase(z, 55);
                }
            }
            else if (phase == 55 && (nativeX <= 10 && !backwards || nativeX > 680 && backwards))
            {
                z.PresentationNativeHeight = 2;
                z.PresentationAltitude = -40f;
                SetAdventureDolphinPhase(z, 56);
                AddAdventureZombieSplash(z, 23f, 78f, .8f, .8f, 52, 65f);
            }

            return false;
        }

        /// <summary>Runs the original snorkel walk, jump, swim, rise, eat, and reverse-dive phases.</summary>
        /// <param name="z">The zombie whose snorkel phase may be active.</param>
        /// <param name="dt">The fixed simulation interval in seconds.</param>
        /// <returns>Whether the jump phase consumes movement and eating this tick.</returns>
        internal bool AdvanceAdventureSnorkel(Zombie z, float dt)
        {
            if (z.Kind != ZombieKind.Snorkel)
            {
                return false;
            }

            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
            var phase = z.PresentationNativePhase;
            if (phase == 57 && !z.Hypnotized && nativeX > 700 && nativeX <= 720)
            {
                _vaultZombieActions.SetAdventureZombieMotionPhase(z, 58);
                return true;
            }

            if (phase == 58)
            {
                var progress = z._presentationWalkLoops + (double)z._presentationWalkPhase;
                z.PresentationAltitude = Math.Min(1000, (int)(progress * 1000d)) * .01f;
                if (AdventureZombieBiteRules.Crossed(z._presentationPreviousWalkCycle, progress, .83f))
                {
                    AddAdventureZombieSplash(z, -47f, 73f, 1.2f, .8f, 53);
                }

                if (progress >= 1d)
                {
                    z.PresentationInPool = true;
                    _vaultZombieActions.SetAdventureZombieMotionPhase(z, 59);
                    // PC jump completion retains the 0.2 entry velocity until a later PickRandomSpeed.
                    z.PresentationNativeVelocity = .2f;
                    z.Speed = AdventureZombieNativeRules.GridSpeed(.2f);
                }
                else
                {
                    z.X -= .2f * (dt * 100f) / 80f;
                }

                z.Submerged = z.PresentationInPool == true && !z.Eating;
                return true;
            }

            if (phase == 59)
            {
                if (z.Headless)
                {
                    // PC UpdateDamageStates removes a headless submerged snorkel without a death reanimation.
                    z.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                    _zombieLifecycle.KillAdventureZombie(z);
                    return true;
                }

                if (!z.Headless && (nativeX <= 25 && !z.Hypnotized || nativeX > 640 && z.Hypnotized))
                {
                    z.PresentationNativeHeight = 2;
                    z.PresentationAltitude = -90f;
                    z.X += (z.Hypnotized ? 15f : -15f) / 80f;
                    _vaultZombieActions.SetAdventureZombieMotionPhase(z, 57);
                    AddAdventureZombieSplash(z, 23f, 78f, .8f, .8f, 52);
                }
                else if (z.Eating)
                {
                    _vaultZombieActions.SetAdventureZombieMotionPhase(z, 60);
                }
            }
            else if (phase == 60 || phase == 61)
            {
                if (!z.Eating)
                {
                    _vaultZombieActions.SetAdventureZombieMotionPhase(z, 62);
                }
                else if (phase == 60 && z._presentationWalkLoops > 0)
                {
                    _vaultZombieActions.SetAdventureZombieMotionPhase(z, 61);
                }
            }
            else if (phase == 62 && z._presentationWalkLoops > 0)
            {
                _vaultZombieActions.SetAdventureZombieMotionPhase(z, 59);
            }

            z.Submerged = z.PresentationInPool == true && !z.Eating;
            return false;
        }

        /// <summary>Updates native pool entry, exit, altitude, and attack-rectangle clipping.</summary>
        /// <param name="z">The zombie whose water contact is being checked.</param>
        /// <param name="dt">The fixed simulation interval in seconds.</param>
        internal void AdvanceAdventureZombiePool(Zombie z, float dt)
        {
            if (z.PoolState != null)
            {
                AdvanceAdventurePlainZombiePool(z);
                return;
            }

            var scale = AdventureDancerRules.Scale(z);
            if (z.PresentationNativeHeight == 3)
            {
                // PC UpdateZombiePool 0x5451F9 lowers a dragged body by one pixel per update.
                z.PresentationAltitude -= z.Kind == ZombieKind.DolphinRider ? 1f : dt * 100f;
            }
            else if (z.PresentationNativeHeight == 7 && z.Kind != ZombieKind.Pogo)
            {
                z.PresentationAltitude = Math.Max(0f, z.PresentationAltitude - (z.Kind == ZombieKind.DolphinRider || z.Kind == ZombieKind.Balloon ? 1f : dt * 100f));
                if (z.PresentationAltitude == 0f)
                {
                    z.PresentationNativeHeight = 0;
                    z.PresentationVaultPositionY = 0f;
                }
            }
            else if (z.PresentationNativeHeight == 1 || z.PresentationNativeHeight == 2)
            {
                var entering = z.PresentationNativeHeight == 1;
                z.PresentationAltitude += z.Kind == ZombieKind.DolphinRider ? entering ? -1f : 1f
                    : (entering ? -1f : z.Kind == ZombieKind.Snorkel ? 2f : 1f) * dt * 100f;
                var destination = entering ? -40f * scale : 0f;
                if (entering ? z.PresentationAltitude <= destination : z.PresentationAltitude >= destination)
                {
                    z.PresentationAltitude = destination;
                    z.PresentationNativeHeight = 0;
                    z.PresentationInPool = entering;
                }
            }
            else if (z.Kind != ZombieKind.Snorkel && z.Kind != ZombieKind.DolphinRider && !z.Airborne
                && (z.Kind == ZombieKind.Normal || z.Kind == ZombieKind.Conehead || z.Kind == ZombieKind.Buckethead
                    || z.Kind == ZombieKind.Flag || z.Kind == ZombieKind.DuckyTube || z.Kind == ZombieKind.Balloon))
            {
                var nativeX = AdventureZombieNativeRules.NativePositionX(z.X, scale);
                var pool = _adventureModeRunner.Adventure.Level.IsWaterRow(z.Row) && AdventureZombieMotionRules.IsPoolPosition(nativeX);
                if (pool != (z.PresentationInPool == true))
                {
                    z.PresentationNativeHeight = pool ? 1 : 2;
                    if (pool)
                    {
                        z.PresentationInPool = true;
                    }

                    AddAdventureZombieSplash(z, 23f, 78f, .8f, .8f, pool ? 19 : 52);
                }
            }

            z.PresentationBiteBodyY = (int)-z.PresentationAltitude;
            z.PresentationBiteClipHeight = (int)AdventureZombieMotionRules.ClipHeight(z, scale);
        }

        /// <summary>Emits the original Splash reanimation, PlantingPool particles, and water Foley.</summary>
        /// <param name="z">The zombie entering or leaving the pool.</param>
        /// <param name="offsetX">The native horizontal splash offset.</param>
        /// <param name="offsetY">The native vertical splash offset.</param>
        /// <param name="scaleX">The authored horizontal splash scale.</param>
        /// <param name="scaleY">The authored vertical splash scale.</param>
        /// <param name="foley">The original water Foley identifier.</param>
        /// <param name="particleOffsetY">The optional original particle offset when PoolSplash uses a distinct vertical origin.</param>
        internal void AddAdventureZombieSplash(Zombie z, float offsetX, float offsetY, float scaleX, float scaleY, int foley, float? particleOffsetY = null)
        {
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
            var nativeY = (int)_biteSystem.AdventureBiteNativeY(z, nativeX);
            _world.Presentation.ZombieSplashes.Add(new AdventureZombieSplash
            {
                Id = ++_world.Presentation._effectId, Row = z.Row,
                X = nativeX + offsetX,
                Y = nativeY + offsetY,
                ParticleX = nativeX + offsetX + 37f, ParticleY = nativeY + (particleOffsetY ?? offsetY + 42f),
                ScaleX = scaleX, ScaleY = scaleY
            });
            _zombieFeedbackSystem.RequestAdventureZombieFoley(foley);
        }

        /// <summary>Starts the original fifty-update pool ambush with its native swimming body and attached seaweed.</summary>
        /// <param name="zombie">The selected normal, conehead, or buckethead zombie retaining its original armor.</param>
        /// <param name="column">The selected pool cell.</param>
        internal void InitializeAdventurePoolZombie(Zombie zombie, int column)
        {
            var nativeX = 40f + column * 80f - 25f;
            zombie.X = AdventureZombieNativeRules.GridX(nativeX, AdventureDancerRules.Scale(zombie));
            zombie.PresentationPreviousX = zombie.X;
            zombie.PoolState ??= new AdventureZombiePoolState { HasDuckyTube = AdventureZombiePoolRules.UsesPlainBody(zombie.Kind) };
            zombie.PoolState.Altitude = AdventureZombiePoolRules.RiseAltitude(50, AdventureDancerRules.Scale(zombie));
            zombie.PoolState.SeaweedAge = 0f;
            zombie.PresentationInPool = true;
            zombie.PresentationNativeHeight = 0;
            zombie.GraveRiseTicks = 50;
            zombie.PresentationNativePhase = 14;
            zombie.DropRemaining = 0f;
            zombie.Eating = false;
            zombie.PresentationBiteBodyY = 0;
            zombie.PresentationBiteClipHeight = -10000;
            _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
            RecordAdventurePoolSplash(zombie, false);
        }

        /// <summary>Advances the original plain water body's entrance or exit without changing its species.</summary>
        /// <param name="zombie">The live zombie with an initialized pool body.</param>
        internal void AdvanceAdventurePlainZombiePool(Zombie zombie)
        {
            var state = zombie.PoolState;
            if (state == null || zombie.Airborne)
            {
                return;
            }

            var scale = AdventureDancerRules.Scale(zombie);
            if (zombie.PresentationNativeHeight == 3)
            {
                state.Altitude -= 1f;
            }
            else if (zombie.PresentationNativeHeight == 1)
            {
                state.Altitude = Math.Max(-40f * scale, state.Altitude - 1f);
                if (state.Altitude <= -40f * scale)
                {
                    zombie.PresentationNativeHeight = 0;
                    _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
                }
            }
            else if (zombie.PresentationNativeHeight == 2)
            {
                state.Altitude = Math.Min(0f, state.Altitude + 1f);
                if (state.Altitude >= 0f)
                {
                    zombie.PresentationNativeHeight = 0;
                    zombie.PresentationInPool = false;
                }
            }
            else
            {
                // PC PixelToGridX returns -1 left of x40 and clamps the right edge to column eight.
                var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, scale);
                var waterSquare = _adventureModeRunner.Adventure.Level.IsWaterRow(zombie.Row) && AdventureZombieMotionRules.IsPoolPosition(nativeX);
                if (waterSquare && zombie.PresentationInPool != true)
                {
                    if (_adventureModeRunner.Adventure.IceTrapTicks > 0)
                    {
                        zombie.FrozenRemaining = Math.Max(zombie.FrozenRemaining, _adventureModeRunner.Adventure.IceTrapTicks * .01f);
                        _coldSystem.ApplyAdventureChill(zombie, true);
                        return;
                    }

                    zombie.PresentationNativeHeight = 1;
                    zombie.PresentationInPool = true;
                    RecordAdventurePoolSplash(zombie, true);
                }
                else if (!waterSquare && zombie.PresentationInPool == true)
                {
                    zombie.PresentationNativeHeight = 2;
                    _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(zombie);
                    RecordAdventurePoolSplash(zombie, false);
                }
            }

            UpdateAdventurePoolBodyRectangle(zombie);
        }

        /// <summary>Updates the native submerged collision offsets after a pool altitude transition.</summary>
        /// <param name="zombie">The actor whose original body rectangle follows the water altitude.</param>
        internal static void UpdateAdventurePoolBodyRectangle(Zombie zombie)
        {
            if (zombie.PoolState == null)
            {
                return;
            }

            if (zombie.UseLadderColumn >= 0 && (zombie.PresentationNativeHeight == 6 || zombie.PresentationNativeHeight == 7))
            {
                zombie.PresentationBiteBodyY = (int)-zombie.PresentationAltitude;
                zombie.PresentationBiteClipHeight = -10000;
            }
            else if (zombie.PresentationInPool == true)
            {
                zombie.PresentationBiteBodyY = (int)-zombie.PoolState.Altitude;
                zombie.PresentationBiteClipHeight = (int)(-zombie.PoolState.Altitude - 7f
                    + 10f * (1f - AdventureDancerRules.Scale(zombie)) + (zombie.Eating ? 7f : 0f));
            }
            else
            {
                zombie.PresentationBiteBodyY = 0;
                zombie.PresentationBiteClipHeight = -10000;
            }
        }

        /// <summary>Records the original fixed splash origin and entry or emergence Foley for the actor's own render layer.</summary>
        /// <param name="zombie">The actor crossing the water surface.</param>
        /// <param name="entering">Whether the normal entrance uses pitched Foley 19 instead of Foley 52.</param>
        internal void RecordAdventurePoolSplash(Zombie zombie, bool entering)
        {
            var state = zombie.PoolState!;
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            var nativeY = (int)_biteSystem.AdventureBiteNativeY(zombie, nativeX);
            state.SplashAge = 0f;
            state.SplashNativeX = nativeX + 23f;
            state.SplashNativeY = nativeY + 78f;
            _zombieFeedbackSystem.RequestAdventureZombieFoley(entering ? 19 : 52);
        }

        /// <summary>Starts the original land grave ambush without treating the zombie as a falling actor.</summary>
        /// <param name="zombie">The new zombie created by the final-wave grave ambush.</param>
        /// <param name="column">The grave's lawn column.</param>
        internal void InitializeAdventureGraveZombie(Zombie zombie, int column)
        {
            var nativeX = 40f + column * 80f - 25f;
            var whack = _adventureModeRunner.Adventure.Level.Mode == AdventureMode.WhackAZombie || _world.ModeSession != null && _world.ModeSession.Definition.Mechanic == ModeMechanic.Whack;
            zombie.X = AdventureZombieNativeRules.GridX(nativeX, AdventureDancerRules.Scale(zombie));
            zombie.PresentationPreviousX = zombie.X;
            zombie.GraveRiseTicks = whack ? 50 : AdventureZombieRiseRules.DurationTicks;
            zombie.PresentationWhackSpeed = whack;
            zombie.PresentationNativePhase = 14;
            zombie.DropRemaining = 0f;
            zombie.Eating = false;
            // PC 0x54DCCF uses mPosX+60,mPosY+110 for the original ZombieRise particle.
            _world.Presentation.ZombieRiseEffects.Add(new AdventureZombieRiseEffect
            {
                Id = ++_world.Presentation._effectId,
                Row = zombie.Row,
                X = nativeX + 60f,
                Y = _biteSystem.AdventureBiteNativeY(zombie, (int)nativeX) + 110f,
                Whack = whack
            });
            if (whack)
            {
                _world.Presentation.ZombieRiseSoundSerial++;
            }
            else
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(79);
            }
        }
    }
}
