#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ProjectileSystem responsibilities for one deterministic match.</summary>
    internal sealed class ProjectileSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BattlePresentationSystem operations.</summary>
        private BattlePresentationSystem _battlePresentationSystem = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides BossSystem operations.</summary>
        private BossSystem _bossSystem = null!;
        /// <summary>Provides ColdSystem operations.</summary>
        private ColdSystem _coldSystem = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides UpgradePlantActions operations.</summary>
        private UpgradePlantActions _upgradePlantActions = null!;
        /// <summary>Provides VehicleZombieActions operations.</summary>
        private VehicleZombieActions _vehicleZombieActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal ProjectileSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="battlePresentationSystem">The BattlePresentationSystem collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="bossSystem">The BossSystem collaborator.</param>
        /// <param name="coldSystem">The ColdSystem collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="upgradePlantActions">The UpgradePlantActions collaborator.</param>
        /// <param name="vehicleZombieActions">The VehicleZombieActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BattlePresentationSystem battlePresentationSystem, BiteSystem biteSystem, BoardQueries boardQueries, BossSystem bossSystem, ColdSystem coldSystem, CombatSystem combatSystem, LegacySimulation legacySimulation, UpgradePlantActions upgradePlantActions, VehicleZombieActions vehicleZombieActions, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _battlePresentationSystem = battlePresentationSystem;
            _biteSystem = biteSystem;
            _boardQueries = boardQueries;
            _bossSystem = bossSystem;
            _coldSystem = coldSystem;
            _combatSystem = combatSystem;
            _legacySimulation = legacySimulation;
            _upgradePlantActions = upgradePlantActions;
            _vehicleZombieActions = vehicleZombieActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Takes a projectile with clean gameplay and presentation state.</summary>
        /// <returns>A reusable projectile with the same defaults as a newly constructed entry.</returns>
        internal Pea RentPea()
        {
            var pea = _world._peaPool.Rent();
            pea.Reset();
            return pea;
        }

        /// <summary>Retires a projectile after detaching its active snow trail.</summary>
        /// <param name="index">The index of the active projectile to retire.</param>
        internal void RemovePeaAt(int index)
        {
            var pea = _world.Peas[index];
            if (_world.IsAdventure)
            {
                _coldSystem.StopAdventureSnowTrail(pea);
            }

            _world.Peas.RemoveAt(index);
            pea.PresentationSnowTrail = null!;
            _world._peaPool.Return(pea);
        }

        /// <summary>Returns all active projectiles before a mode or stage clears its board.</summary>
        internal void ClearPeas()
        {
            for (var i = _world.Peas.Count - 1; i >= 0; i--)
            {
                RemovePeaAt(i);
            }
        }

        /// <summary>Retires an expired lane flame.</summary>
        /// <param name="index">The index of the active flame to retire.</param>
        internal void RemoveFlameAt(int index)
        {
            var flame = _world.Flames[index];
            _world.Flames.RemoveAt(index);
            _world._flamePool.Return(flame);
        }

        /// <summary>Returns all active flames before a mode or stage clears its board.</summary>
        internal void ClearFlames()
        {
            for (var i = _world.Flames.Count - 1; i >= 0; i--)
            {
                RemoveFlameAt(i);
            }
        }

        /// <summary>Performs emit lane flame.</summary>
        /// <param name = "row">The row.</param>
        internal void EmitLaneFlame(int row)
        {
            var flame = _world._flamePool.Rent();
            flame.Row = row;
            flame.Age = 0f;
            _world.Flames.Add(flame);
        }

        /// <summary>Performs fire adventure pea.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "back">The back.</param>
        /// <param name = "damage">The damage.</param>
        /// <returns>The resulting fire adventure pea.</returns>
        internal Pea FireAdventurePea(Plant p, int row, bool back, float damage)
        {
            var shot = RentPea();
            shot.SourceKind = p.Kind;
            shot.SourceIsImitated = p.IsImitated;
            shot.Row = row;
            shot.FlightRow = row;
            shot.X = p.Column + (back ? 0.15f : 0.85f);
            shot.StartX = p.Column + 0.5f;
            // PC UpdateMotion 47CCB0 advances normal and backward peas by 3.33 pixels per native update.
            var peaVelocity = AdventureTorchwoodRules.IsPea(p.Kind) ? 3.33f * 100f / 80f : 4.5f;
            shot.VelocityX = back ? -peaVelocity : peaVelocity;
            shot.Damage = damage;
            shot.Backward = back;
            // Preserve the existing muzzle while retaining its fixed original trajectory after launch.
            shot.PresentationLaunchY = 80f + row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f)
                + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0f, 5f - shot.X) * 20f - 10f : 0f) + 30f;
            if (p.Kind == PlantKind.Peashooter || p.Kind == PlantKind.SnowPea
                || p.Kind == PlantKind.Repeater || p.Kind == PlantKind.GatlingPea)
            {
                AdventurePeaShooterRules.HeadOffset(p.Age, p.PresentationRate > 0f ? p.PresentationRate : 12f,
                    out var headX, out var headY);
                shot.X = p.Column + (headX + (p.Kind == PlantKind.GatlingPea ? 34f : 24f) + 14f) / 80f;
                var plantY = 80f + p.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f)
                    + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0, 5 - p.Column) * 20f - 10f : 0f);
                shot.PresentationLaunchY = plantY + headY - 33f + 14f
                    - (_boardQueries.HasPlantKindAt(PlantKind.FlowerPot, p.Row, p.Column) ? 5f : 0f);
            }
            else if (p.Kind == PlantKind.Threepeater)
            {
                // PC Plant::Fire 0x4752E0 and Projectile::UpdateMotion 0x47CCB0.
                // The logical collision row is fixed at launch; only the artwork cuts into that lane.
                shot.X = p.Column + 59f / 80f;
                shot.VelocityX = 3.33f * 100f / 80f;
                shot.PresentationLaunchY = 80f + p.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f)
                    + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0, 5 - p.Column) * 20f - 10f : 0f) + 24f
                    - (_boardQueries.HasPlantKindAt(PlantKind.FlowerPot, p.Row, p.Column) ? 5f : 0f);
                shot.PresentationLaneVelocity = Math.Sign(row - p.Row) * 3f;
            }
            else if (p.Kind == PlantKind.SplitPea)
            {
                // PC Plant::Fire 0x4752E0 uses the same moving body anchor for both heads.
                AdventureSplitPeaRules.HeadOffset(p.Age, p.PresentationRate > 0f ? p.PresentationRate : 12f, out var headX, out var headY);
                shot.X = p.Column + (headX + (back ? -64f : 24f) + 14f) / 80f;
                shot.StartX = shot.X;
                shot.PresentationLaunchY = 80f + p.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f)
                    + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0, 5 - p.Column) * 20f - 10f : 0f)
                    + headY - 33f + 14f - (_boardQueries.HasPlantKindAt(PlantKind.FlowerPot, p.Row, p.Column) ? 5f : 0f);
            }

            _world.Peas.Add(shot);
            _battlePresentationSystem.AdventurePresentationShot(p, shot);
            if (AdventurePuffShroomRules.IsShortRange(p.Kind))
            {
                shot.X = AdventurePuffShroomRules.LaunchCenterX(p.Kind, p.Column) + p.PositionOffsetX / 80f;
                shot.StartX = shot.X;
                shot.PresentationPreviousX = shot.X;
                shot.VelocityX = AdventurePuffShroomRules.GridSpeed;
                var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
                var plantY = 80f + p.Row * pitch + p.PositionOffsetY + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0, 5 - p.Column) * 20f - 10f : 0f);
                shot.PresentationLaunchY = AdventurePuffShroomRules.LaunchCenterY(p.Kind, plantY,
                    _boardQueries.HasPlantKindAt(PlantKind.FlowerPot, p.Row, p.Column));
            }

            p.ShootAge = 0;
            if (p.Kind != PlantKind.SnowPea && p.Kind != PlantKind.WinterMelon)
            {
                if (p.Kind != PlantKind.Starfruit)
                {
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(3);
                }

                if (AdventurePuffShroomRules.IsShortRange(p.Kind) || p.Kind == PlantKind.ScaredyShroom)
                {
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(29);
                }

                _world.Presentation._nativeShootSuppressions++;
            }

            _legacySimulation.Raise(GameEvent.Shoot);
            return shot;
        }

        /// <summary>Finds a Starfruit target using the original five projectile paths.</summary>
        /// <param name="plant">The Starfruit acquiring a target.</param>
        /// <returns>The nearest eligible target, or null when no path intersects a target.</returns>
        internal Zombie? FindAdventureStarfruitTarget(Plant plant)
        {
            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            var plantY = 80f + plant.Row * pitch + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0f, 5f - plant.Column) * 20f - 10f : 0f);
            Zombie? target = null;
            var targetLeft = float.MaxValue;
            foreach (var zombie in _world.Zombies)
            {
                if (!AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, plant.Kind)
                    || !_combatSystem.CanTakeAdventureAshDamage(zombie, 1U))
                {
                    continue;
                }

                var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                var bodyY = (int)_biteSystem.AdventureBiteNativeY(zombie, nativeX);
                var body = AdventureZombieTargetingRules.BodyRectangle(zombie, bodyY);
                var stationary = AdventureStarfruitRules.TargetNotWalking(zombie)
                    || AdventureDancerRules.IsDancer(zombie) && AdventureDancerRules.GroupAffected(zombie, _world.Zombies, false);
                var velocity = stationary ? 0f : zombie.PresentationNativeVelocity * (zombie.ChillRemaining > 0f ? .5f : 1f);
                if (zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(zombie)))
                {
                    velocity = -velocity;
                }

                if (AdventureStarfruitRules.CanTarget(plant, zombie, body, (int)plantY, velocity, true)
                    && body.X < targetLeft)
                {
                    target = zombie;
                    targetLeft = body.X;
                }
            }

            return target;
        }

        /// <summary>Applies the original Fume-shroom row-area attack at the scheduled firing tick.</summary>
        /// <param name="plant">The Fume-shroom whose attack has reached its firing tick.</param>
        internal void FireAdventureFume(Plant plant)
        {
            foreach (var zombie in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                if (AdventureFumeShroomRules.CanDamage(plant, zombie))
                {
                    _combatSystem.HitAdventureZombie(zombie, 20f, true, 2U);
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                }
            }

            plant._presentationPrefiring = false;
            _zombieFeedbackSystem.RequestAdventureZombieFoley(30);
            _world.Presentation._nativeShootSuppressions++;
            _legacySimulation.Raise(GameEvent.Shoot);
        }

        /// <summary>Creates a projectile when a native body shooting countdown reaches its firing tick.</summary>
        /// <param name="plant">The plant with a pending attack.</param>
        internal void FireAdventureNativeTimedProjectile(Plant plant)
        {
            var lob = plant.Kind == PlantKind.CabbagePult || plant.Kind == PlantKind.KernelPult
                || plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon;
            var melon = plant.Kind == PlantKind.MelonPult || plant.Kind == PlantKind.WinterMelon;
            var shot = FireAdventurePea(plant, plant.Row, false, melon ? 80f : plant.Kind == PlantKind.CabbagePult ? 40f : 20f);
            shot.HitsAir = plant.Kind == PlantKind.Cactus && plant.CactusPresentation?.NativePhase == 32;
            if (plant.Kind == PlantKind.ScaredyShroom)
            {
                // PC 4752E0: the spore's original top-left is plant +(29,21), then flower-pot height -5.
                shot.X = plant.Column + 45f / 80f;
                shot.PresentationPreviousX = shot.X;
                shot.VelocityX = 3.33f * 100f / 80f;
                shot.PresentationLaunchY = 80f + plant.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f)
                    + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0, 5 - plant.Column) * 20f - 10f : 0f) + 34.5f
                    - (_boardQueries.HasPlantKindAt(PlantKind.FlowerPot, plant.Row, plant.Column) ? 5f : 0f);
            }

            if (plant.Kind == PlantKind.Cactus)
            {
                // PC Plant::Fire 0x4752E0 creates the 30x16 spike at +(93,-50) or +(70,23).
                // The renderer stores its centre, whereas native projectile coordinates name its top-left corner.
                shot.X = plant.Column + (shot.HitsAir ? 108f : 85f) / 80f;
                shot.StartX = shot.X;
                shot.PresentationPreviousX = shot.X;
                shot.VelocityX = 3.33f * 100f / 80f;
                var plantY = 80f + plant.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f)
                    + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0f, 5f - plant.Column) * 20f - 10f : 0f);
                shot.PresentationLaunchY = plantY + (shot.HitsAir ? -42f : 31f)
                    - (_boardQueries.HasPlantKindAt(PlantKind.FlowerPot, plant.Row, plant.Column) ? 5f : 0f);
            }
            shot.Range = 20f;
            shot.Lobbed = lob;
            shot.PiercesShield = lob;
            shot.Splash = melon;
            if (plant.Kind == PlantKind.WinterMelon)
            {
                shot.IsSnow = true;
                shot.ChillSplash = true;
            }

            if (plant.Kind == PlantKind.KernelPult)
            {
                shot.Butter = plant._kernelWeaponPrepared && plant.PresentationButter;
                plant._kernelWeaponPrepared = false;
                // PC 47301D switches the basket back to kernels before creating the butter projectile.
                plant.PresentationButter = false;
                if (shot.Butter)
                {
                    shot.Damage = 40f;
                }

                plant.PresentationShotAge = .29f;
            }

            if (lob)
            {
                // PC Plant::Fire resolves its target at creation, before any other plant can kill it.
                _battlePresentationSystem.AdvanceAdventureProjectilePresentation(shot, 0f);
            }

        }

        /// <summary>Fires the five original Starfruit projectiles at the scheduled tick.</summary>
        /// <param name="plant">The Starfruit whose body shooting counter has expired.</param>
        internal void FireAdventureStarfruit(Plant plant)
        {
            _zombieFeedbackSystem.RequestAdventureZombieFoley(3);
            var rowPitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            var plantY = 80f + plant.Row * rowPitch
                + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0, 5 - plant.Column) * 20f - 10f : 0f);
            for (var index = 0; index < 5; index++)
            {
                var star = FireAdventurePea(plant, plant.Row, false, 20f);
                star.X = plant.Column + 25f / 80f;
                star.StartX = star.X;
                star.PresentationPreviousX = star.X;
                star.VelocityX = AdventureStarfruitRules.VelocityX(index) * 100f / 80f;
                star.VelocityY = AdventureStarfruitRules.VelocityY(index) * 100f / rowPitch;
                star.Backward = star.VelocityX < 0f;
                star.PiercesShield = star.Backward;
                star.PresentationLaunchY = AdventureStarfruitRules.LaunchY((int)plantY);
                star.PresentationStarShadowY = plantY + 82f
                    - (_adventureModeRunner.Adventure.Level.HasRoof && AdventureStarfruitRules.LaunchX(plant.Column) < 480 ? 12f : 0f);
                // PC ProjectileInitialize 47BB30: independent original signed angular velocity per star.
                star.PresentationRotation = 0f;
                star.PresentationSpin = .05f + (float)_world._random.NextDouble() * .05f;
                if (_world._random.Next(2) == 0)
                {
                    star.PresentationSpin = -star.PresentationSpin;
                }

                star.PresentationMotionInitialized = true;
                star.Range = float.MaxValue;
            }

            plant._presentationPrefiring = false;
        }

        /// <summary>Checks PC 47DFF0's lobber rectangle at the preceding integer projectile position.</summary>
        /// <param name="shot">The cabbage, kernel, butter or melon being integrated.</param>
        /// <param name="previousX">The original integer projectile position before this native motion update.</param>
        /// <param name="zombie">The admitted collision candidate.</param>
        /// <returns>Whether the original horizontal body intervals touch or overlap.</returns>
        internal static bool AdventureLobberCollides(Pea shot, int previousX, Zombie zombie)
        {
            var left = previousX + (shot.Splash ? 20 : 0);
            var width = shot.Splash ? 60 : 40;
            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
            return Math.Min(left + width, body.X + body.Width) >= Math.Max(left, body.X);
        }

        /// <summary>Performs can shoot adventure boss.</summary>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting can shoot adventure boss.</returns>
        internal bool CanShootAdventureBoss(Plant p)
        {
            return _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Boss && _adventureModeRunner.Adventure.BossHeadVulnerable && _adventureModeRunner.Adventure.BossHealth > 0f;
        }

        /// <summary>Advances PC 47CCB0's star motion and 47C130's native row and horizontal-rectangle collision.</summary>
        /// <param name="shot">The original five-direction star projectile.</param>
        /// <param name="dt">The fixed simulation interval in seconds.</param>
        /// <param name="allowDamage">Whether the current board permits combat damage.</param>
        /// <returns>Whether the star has hit a target, struck the roof or left the original board bounds.</returns>
        internal bool AdvanceAdventureStarProjectile(Pea shot, float dt, bool allowDamage)
        {
            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            var previousX = 40f + shot.X * 80f;
            shot.X += shot.VelocityX * dt;
            shot.FlightRow += shot.VelocityY * dt;
            shot.PresentationNativeAge += dt;
            var x = 40f + shot.X * 80f;
            var y = AdventureStarfruitRules.PositionY(shot, pitch);
            shot.PresentationStarShadowY += shot.VelocityY * dt * pitch;
            if (shot.VelocityY != 0f)
            {
                shot.Row = AdventureStarfruitRules.CollisionRow(x, y, _adventureModeRunner.Adventure.Level.HasPool, _adventureModeRunner.Adventure.Level.HasRoof);
            }

            if (AdventureStarfruitRules.OffBoard(x, y))
            {
                return true;
            }

            Zombie? hit = null;
            var hitX = int.MaxValue;
            if (allowDamage && shot.PresentationStarShadowY - y <= 90f)
            {
                foreach (var zombie in _world.Zombies)
                {
                    if ((zombie.Row != shot.Row && zombie.Kind != ZombieKind.Boss)
                        || !AdventureZombieTargetingRules.CanBeAutoTargeted(zombie, PlantKind.Starfruit)
                        || !_combatSystem.CanTakeAdventureAshDamage(zombie, 1U)
                        || zombie.Kind == ZombieKind.Snorkel && zombie.PresentationNativePhase == 59
                        || zombie.Kind == ZombieKind.Digger && shot.VelocityX >= 0f
                            && (int)Math.Round(shot.PresentationNativeAge * 100f) < 25)
                    {
                        continue;
                    }

                    // UpdateMotion stores integer mX/mY after collision, so GetProjectileRect uses the previous x.
                    var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                    var overlap = Math.Min((int)previousX + 40, body.X + body.Width) - Math.Max((int)previousX, body.X);
                    var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
                    if (overlap >= 0 && nativeX < hitX)
                    {
                        hit = zombie;
                        hitX = nativeX;
                    }
                }
            }

            if (hit != null)
            {
                _battlePresentationSystem.AdventurePresentationImpact(shot, hit);
                _combatSystem.HitAdventureZombie(hit, shot.Damage, shot.PiercesShield);
                return true;
            }

            if (shot.PresentationStarShadowY - y < 23f)
            {
                _battlePresentationSystem.AdventurePresentationImpact(shot, null);
                return true;
            }

            // PC UpdateMotion adjusts only the shadow after this tick's collisions; stars stay on a straight line.
            if (_adventureModeRunner.Adventure.Level.HasRoof)
            {
                shot.PresentationStarShadowY += (Math.Max(0f, 440f - x) - Math.Max(0f, 440f - previousX)) * .25f;
            }

            return false;
        }

        /// <summary>Runs original Plant::UpdateTorchwood before projectiles advance their positions.</summary>
        /// <param name="torch">The live Torchwood checking normal and snow peas in its logical row.</param>
        internal void UpdateAdventureTorchwood(Plant torch)
        {
            foreach (var shot in _world.Peas)
            {
                if (!AdventureTorchwoodRules.Overlaps(torch, shot))
                {
                    continue;
                }

                shot.LastTorchColumn = torch.Column;
                if (shot.IsSnow)
                {
                    // PC ConvertToPea destroys the trail and leaves this same tree unable to ignite it again.
                    shot.IsSnow = false;
                    _coldSystem.UpdateAdventureSnowTrail(shot);
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(3);
                }
                else
                {
                    // PC ConvertToFireball 47E0E0 retains mPos/mVel/mMotion and creates a new attachment clock.
                    shot.Fire = true;
                    shot.Damage = 40f;
                    shot.Splash = true;
                    shot.PresentationFireAge = 0f;
                    shot.PresentationFireRate = 50f + (float)_world._random.NextDouble() * 30f;
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(39);
                }
            }
        }

        /// <summary>Checks PC CheckForCollision's chosen-target spike rectangle and strict physical y interval.</summary>
        /// <param name="shot">The original forty-pixel homing hit box.</param>
        /// <param name="target">The still-eligible chosen zombie.</param>
        /// <returns>Whether this update reaches its original target body.</returns>
        private bool AdventureCattailCollides(Pea shot, Zombie target)
        {
            var body = _upgradePlantActions.CattailTargetBody(target);
            var x = (int)(40f + shot.X * 80f);
            return x + 40 >= body.X && x - 25 <= body.X + body.Width
                && shot.PresentationHomingY > body.Y && shot.PresentationHomingY < body.Y + body.Height;
        }

        /// <summary>Performs advance adventure projectiles.</summary>
        /// <param name = "dt">The dt.</param>
        /// <param name="allowDamage">Whether live combat targets can receive hits before the level has completed.</param>
        internal void AdvanceAdventureProjectiles(float dt, bool allowDamage = true)
        {
            for (var i = _world.Peas.Count - 1; i >= 0; i--)
            {
                var p = _world.Peas[i];
                if (p.Basketball)
                {
                    if (_vehicleZombieActions.AdvanceAdventureBasketball(p, allowDamage))
                    {
                        RemovePeaAt(i);
                    }

                    continue;
                }

                if (p.Fire)
                {
                    p.PresentationFireAge += dt;
                }

                if (p.SourceKind == PlantKind.Starfruit)
                {
                    if (AdvanceAdventureStarProjectile(p, dt, allowDamage))
                    {
                        RemovePeaAt(i);
                    }

                    continue;
                }

                if (p.Lobbed && !p.PresentationMotionInitialized)
                {
                    _battlePresentationSystem.AdvanceAdventureProjectilePresentation(p, 0f);
                }

                _upgradePlantActions.UpdateAdventureHomingVelocity(p, dt);
                var before = p.X;
                var beforeNativeX = (int)p.PresentationLobNativeX;
                p.X += p.VelocityX * dt;
                p.FlightRow += p.VelocityY * dt;
                if (p.SourceKind == PlantKind.Threepeater)
                {
                    p.PresentationPreviousLaneOffset = p.PresentationLaneOffset;
                    p.PresentationLaneOffset += p.PresentationLaneVelocity * (dt * 100f);
                    p.PresentationLaneVelocity *= .97f;
                }
                _battlePresentationSystem.AdvanceAdventureProjectilePresentation(p, dt);
                if (AdventurePuffShroomRules.IsShortRange(p.SourceKind) && AdventurePuffShroomRules.Expired(p.PresentationNativeAge))
                {
                    RemovePeaAt(i);
                    continue;
                }

                if (p.Homing)
                {
                    var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
                    p.FlightRow = (p.PresentationHomingY - 80f) / pitch;
                    p.Row = Math.Max(0, Math.Min(_world.RowCount - 1, (int)Math.Floor(p.FlightRow)));
                }
                else
                {
                    p.Row = (int)Math.Round(p.FlightRow);
                }
                _coldSystem.UpdateAdventureSnowTrail(p);
                Zombie? hit = null;
                foreach (Zombie z in _world.Zombies)
                {
                    if (allowDamage && p.PresentationCanImpact && (p.Lobbed ? _combatSystem.CanTakeAdventureAshDamage(z, 13U) : AdventureZombieTargetingRules.CanBeAutoTargeted(z, p.SourceKind)) && z.GraveRiseTicks <= 0 && z.PresentationNativePhase != 50 && z.PresentationNativePhase != 74
                        && !(z.Kind == ZombieKind.DolphinRider && (z.PresentationNativePhase == 52 || z.PresentationNativePhase == 54))
                        && !(z.Kind == ZombieKind.Digger && z.PresentationNativePhase >= 33 && z.PresentationNativePhase <= 35)
                        && (!p.Homing || z.Id == p.TargetZombieId && _combatSystem.CanTakeAdventureAshDamage(z, 11U)) && !z.Hypnotized
                        && (p.Homing || Math.Abs(z.Row - p.FlightRow) < 0.45f) && !p.HitIds.Contains(z.Id) && (!z.Airborne || p.HitsAir)
                        && (p.SourceKind != PlantKind.Cactus || z.Airborne == p.HitsAir && AdventureZombieTargetingRules.CanBeAutoTargeted(z, PlantKind.Cactus))
                        && (!z.Submerged || p.Lobbed) && (!p.Lobbed || z.PresentationNativePhase != 59
                            || p.PresentationLobContactHeight > 45f)
                        && (p.Homing ? AdventureCattailCollides(p, z) : p.Lobbed ? AdventureLobberCollides(p, beforeNativeX, z) : z.Kind == ZombieKind.Zomboni
                        ? AdventureZomboniRules.ProjectileCollides(p, z)
                        : AdventurePuffShroomRules.IsShortRange(p.SourceKind) ? AdventurePuffShroomRules.Collides(p, z)
                        : z.X >= Math.Min(before, p.X) - 0.25f && z.X <= Math.Max(before, p.X) + 0.25f))
                    {
                        if (hit == null || (p.Lobbed ? (int)AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z))
                            < (int)AdventureZombieNativeRules.NativePositionX(hit.X, AdventureDancerRules.Scale(hit))
                            : Math.Abs(z.X - before) < Math.Abs(hit.X - before)))
                        {
                            hit = z;
                        }
                    }
                }

                var boss = _adventureModeRunner.Adventure.Boss;
                var bossHit = allowDamage && p.Lobbed && p.PresentationCanImpact && boss?.HeadVulnerable == true
                    && AdventureLobberCollides(p, beforeNativeX, boss.Target);
                if (bossHit)
                {
                    // PC FindProjectileCollision compares native actor X; the boss at X=0 wins this overlap.
                    hit = null;
                }
                var remove = false;
                if (hit != null)
                {
                    _battlePresentationSystem.AdventurePresentationImpact(p, hit);
                    p.HitIds.Add(hit.Id);
                    _coldSystem.ApplyAdventureProjectileCold(p, hit);
                    _combatSystem.HitAdventureZombie(hit, p.Damage, p.PiercesShield);
                    if (p.Splash)
                    {
                        foreach (Zombie other in new PooledSnapshot<Zombie>(_world.Zombies))
                        {
                            if (other != hit && !other.Hypnotized && !other.Airborne
                                && !(other.Kind == ZombieKind.DolphinRider && (other.PresentationNativePhase == 52 || other.PresentationNativePhase == 54))
                                && Math.Abs(other.Row - hit.Row) <= (p.Fire ? 0 : 1) && Math.Abs(other.X - hit.X) <= (p.Fire ? 0.5f : 1f))
                            {
                                if (p.ChillSplash)
                                {
                                    _coldSystem.ApplyAdventureChill(other);
                                }

                                _combatSystem.HitAdventureZombie(other, p.Fire ? 20f : 26f, p.PiercesShield);
                            }
                        }
                    }

                    remove = !p.PiercesZombies;
                }

                if (bossHit && !remove)
                {
                    _battlePresentationSystem.AdventurePresentationImpact(p, boss!.Target);
                    _bossSystem.ApplyAdventureBossProjectileCold(p);
                    _bossSystem.DamageAdventureBoss(p.Damage);
                    remove = true;
                }

                if (!p.Lobbed && !p.Homing && _adventureModeRunner.Adventure.Level.HasRoof && p.PresentationLaunchY != 0f)
                {
                    // PC UpdateNormalMotion resolves zombie collision and high ground before UpdateMotion rebases the shadow.
                    var puff = AdventurePuffShroomRules.IsShortRange(p.SourceKind) || p.SourceKind == PlantKind.ScaredyShroom;
                    var halfX = p.SourceKind == PlantKind.Cactus ? 15f : puff ? 16f : 14f;
                    var halfY = p.SourceKind == PlantKind.Cactus ? 8f : puff ? 13.5f : 14f;
                    var nativeX = 40f + p.X * 80f - halfX;
                    var previousX = 40f + before * 80f - halfX;
                    if (p.PresentationNativeAge <= dt + .000001f)
                    {
                        var column = Math.Max(0, Math.Min(8, ((int)previousX - 40) / 80));
                        p.PresentationStarShadowY = 70f + p.Row * 85f + Math.Max(0, 5 - column) * 20f + 67f
                            - (previousX < 480f ? 12f : 0f);
                    }

                    p.PresentationStarShadowY += p.PresentationLaneVelocity * dt * 100f;
                    var topY = p.PresentationLaunchY + p.PresentationLaneOffset - halfY;
                    if (!remove && p.PresentationStarShadowY - topY < (puff ? 0f : 28f))
                    {
                        _battlePresentationSystem.AdventurePresentationImpact(p, null);
                        remove = true;
                    }

                    p.PresentationStarShadowY += (Math.Max(0f, 440f - nativeX)
                        - Math.Max(0f, 440f - previousX)) * .25f;
                }

                if (!remove && p.Lobbed && p.PresentationCanImpact
                    && p.PresentationLobContactHeight > 80f)
                {
                    // PC UpdateLobMotion 47C880 impacts the floor even when no zombie survives beneath the shot.
                    _battlePresentationSystem.AdventurePresentationImpact(p, null);
                    remove = true;
                }

                if (remove || p.X > 12f || p.X < -2f || !p.Homing && (p.FlightRow < -1f || p.FlightRow > _world.RowCount) || Math.Abs(p.X - p.StartX) > p.Range)
                {
                    RemovePeaAt(i);
                }
            }
        }
    }
}
