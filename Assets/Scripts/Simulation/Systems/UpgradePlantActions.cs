#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns UpgradePlantActions responsibilities for one deterministic match.</summary>
    internal sealed class UpgradePlantActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides PlantTimingSystem operations.</summary>
        private PlantTimingSystem _plantTimingSystem = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides SeedBankSystem operations.</summary>
        private SeedBankSystem _seedBankSystem = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal UpgradePlantActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="plantTimingSystem">The PlantTimingSystem collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="seedBankSystem">The SeedBankSystem collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BoardQueries boardQueries, CombatSystem combatSystem, LegacySimulation legacySimulation, PlantTimingSystem plantTimingSystem, PlantingSystem plantingSystem, ProjectileSystem projectileSystem, SeedBankSystem seedBankSystem, SunSystem sunSystem, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _boardQueries = boardQueries;
            _combatSystem = combatSystem;
            _legacySimulation = legacySimulation;
            _plantTimingSystem = plantTimingSystem;
            _plantingSystem = plantingSystem;
            _projectileSystem = projectileSystem;
            _seedBankSystem = seedBankSystem;
            _sunSystem = sunSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Performs upgrade prerequisite.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting upgrade prerequisite.</returns>
        internal static PlantKind? UpgradePrerequisite(PlantKind kind)
        {
            switch (kind)
            {
                case PlantKind.GatlingPea:
                    return PlantKind.Repeater;
                case PlantKind.TwinSunflower:
                    return PlantKind.Sunflower;
                case PlantKind.GloomShroom:
                    return PlantKind.FumeShroom;
                case PlantKind.Cattail:
                    return PlantKind.LilyPad;
                case PlantKind.WinterMelon:
                    return PlantKind.MelonPult;
                case PlantKind.GoldMagnet:
                    return PlantKind.MagnetShroom;
                case PlantKind.Spikerock:
                    return PlantKind.Spikeweed;
                case PlantKind.CobCannon:
                    return PlantKind.KernelPult;
                default:
                    return null;
            }
        }

        /// <summary>Performs check adventure upgrade.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting check adventure upgrade.</returns>
        internal AdventurePlantingFailure CheckAdventureUpgrade(PlantKind kind, int row, int column)
        {
            var prerequisite = UpgradePrerequisite(kind);
            if (!prerequisite.HasValue)
            {
                return AdventurePlantingFailure.NeedsUpgrade;
            }

            var l = _adventureModeRunner.Adventure.Level;
            if (kind == PlantKind.Cattail)
            {
                var lily = _boardQueries.AdventurePlantAt(row, column, PlantLayer.Support);
                return l.IsWaterRow(row) && lily != null && lily.BungeeState == 0 && lily.Kind == PlantKind.LilyPad && _boardQueries.AdventurePlantAt(row, column, PlantLayer.Main) == null ? AdventurePlantingFailure.None : AdventurePlantingFailure.NeedsUpgrade;
            }

            var source = _boardQueries.AdventurePlantAt(row, column, PlantLayer.Main);
            if (source == null || source.BungeeState != 0 || source.Column != column || source.Kind != prerequisite.Value)
            {
                return AdventurePlantingFailure.NeedsUpgrade;
            }

            if (kind == PlantKind.Spikerock && (l.IsWaterRow(row) || l.HasRoof || _boardQueries.AdventurePlantAt(row, column, PlantLayer.Support) != null))
            {
                return AdventurePlantingFailure.RoofRestriction;
            }

            if (kind == PlantKind.CobCannon)
            {
                if (column >= SimulationWorld.Columns - 1)
                {
                    return AdventurePlantingFailure.CannonFootprint;
                }

                var right = _boardQueries.AdventurePlantAt(row, column + 1, PlantLayer.Main);
                if (right == null || right.BungeeState != 0 || right.Kind != PlantKind.KernelPult || right.Column != column + 1 || _boardQueries.AdventurePlantAt(row, column, PlantLayer.Armor) != null || _boardQueries.AdventurePlantAt(row, column + 1, PlantLayer.Armor) != null)
                {
                    return AdventurePlantingFailure.NeedsUpgrade;
                }

                Plant leftPot = _boardQueries.AdventurePlantAt(row, column, PlantLayer.Support), rightPot = _boardQueries.AdventurePlantAt(row, column + 1, PlantLayer.Support);
                bool a = leftPot != null && leftPot.Kind == PlantKind.FlowerPot, b = rightPot != null && rightPot.Kind == PlantKind.FlowerPot;
                if (a != b)
                {
                    return AdventurePlantingFailure.CannonFootprint;
                }
            }

            return AdventurePlantingFailure.None;
        }

        /// <summary>Performs place adventure upgrade.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting place adventure upgrade.</returns>
        internal Plant PlaceAdventureUpgrade(PlantKind kind, int row, int column)
        {
            if (kind == PlantKind.Imitater)
            {
                var target = _seedBankSystem.ResolveAdventureSeed(kind);
                var mimic = new Plant
                {
                    Id = ++_adventureModeRunner.Adventure._plantId,
                    Kind = kind,
                    Row = row,
                    Column = column,
                    Health = 300f,
                    Layer = target == PlantKind.CoffeeBean ? PlantLayer.Effect : PlantingSystem.LayerFor(target),
                    ImitaterTarget = target,
                    ImitaterTransformRemaining = 3.2f,
                    ActionTimer = float.MaxValue
                };
                AdventurePlantBlinkRules.Initialize(mimic, _world._random);
                AdventurePlantBlinkRules.CompleteInitialization(mimic, _world._random);
                _world.Plants.Add(mimic);
                _legacySimulation.Raise(GameEvent.Planted);
                return mimic;
            }

            var source = _boardQueries.AdventurePlantAt(row, column, kind == PlantKind.Cattail ? PlantLayer.Support : PlantLayer.Main);
            bool asleep = source != null && source.Asleep, coffee = source != null && source.AwakeByCoffee;
            var wake = source == null ? 0 : source.WakeTimer;
            if (source != null)
            {
                _world.Plants.Remove(source);
            }

            if (kind == PlantKind.CobCannon)
            {
                var right = _boardQueries.AdventurePlantAt(row, column + 1, PlantLayer.Main);
                if (right != null)
                {
                    _world.Plants.Remove(right);
                }
            }

            var result = new Plant
            {
                Id = ++_adventureModeRunner.Adventure._plantId,
                Kind = kind,
                Row = row,
                Column = column,
                Layer = PlantLayer.Main,
                Health = kind == PlantKind.Spikerock ? 450f : 300f,
                ColumnSpan = kind == PlantKind.CobCannon ? 2 : 1,
                Asleep = kind == PlantKind.GloomShroom && asleep,
                AwakeByCoffee = kind == PlantKind.GloomShroom && coffee,
                WakeTimer = kind == PlantKind.GloomShroom ? wake : 0,
                ActionTimer = kind == PlantKind.TwinSunflower ? Math.Max(0.01f, source == null ? 5f : source.ActionTimer) : 0.5f,
                CannonPhase = CobCannonPhase.Arming,
                CannonReloadRemaining = kind == PlantKind.CobCannon ? 5f : 0
            };
            AdventurePlantBlinkRules.Initialize(result, _world._random);
            _plantingSystem.InitializeAdventureSleepingPlant(result);
            _plantTimingSystem.InitializeAdventureNativePlantTiming(result);
            AdventurePlantBlinkRules.CompleteInitialization(result, _world._random);
            _world.Plants.Add(result);
            _legacySimulation.Raise(GameEvent.Planted);
            return result;
        }
        /// <summary>Performs advance adventure upgrade plant.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "dt">The dt.</param>
        /// <returns>The resulting advance adventure upgrade plant.</returns>
        internal bool AdvanceAdventureUpgradePlant(Plant p, float dt)
        {
            if (p.Kind == PlantKind.Imitater)
            {
                p.ImitaterTransformRemaining = Math.Max(0, p.ImitaterTransformRemaining - dt);
                if (p.ImitaterTransformRemaining <= 0.000001f)
                {
                    MorphAdventureImitater(p);
                }

                return true;
            }

            if (p.Kind == PlantKind.CobCannon)
            {
                AdvanceAdventureCannon(p, dt);
                return true;
            }

            if (p.Kind == PlantKind.GatlingPea || p.Kind == PlantKind.GloomShroom || p.Kind == PlantKind.Cattail)
            {
                if (p.UpgradeBurstTicks > 0)
                {
                    var tick = --p.UpgradeBurstTicks;
                    if (p.Kind == PlantKind.GatlingPea && (tick == 68 || tick == 51 || tick == 35 || tick == 18))
                    {
                        _projectileSystem.FireAdventurePea(p, p.Row, false, 20f);
                    }
                    else if (p.Kind == PlantKind.GloomShroom && (tick == 126 || tick == 98 || tick == 70 || tick == 42))
                    {
                        foreach (Zombie z in new PooledSnapshot<Zombie>(_world.Zombies))
                        {
                            if (IsUpgradeAreaTarget(z, p, true))
                            {
                                _combatSystem.HitAdventureZombie(z, 20f, true);
                                _zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                            }
                        }

                        p.ActionCount++;
                        // PC Gloom-shroom's four area pulses have no launch Foley.
                        _world.Presentation._nativeShootSuppressions++;
                        _legacySimulation.Raise(GameEvent.Shoot);
                    }
                    else if (p.Kind == PlantKind.Cattail && tick == 19)
                    {
                        var target = FindAdventureCattailTarget(p.Column + 0.5f, p.Row);
                        if (target != null)
                        {
                            var dart = _projectileSystem.FireAdventurePea(p, p.Row, false, 20f);
                            dart.Homing = true;
                            dart.TargetZombieId = target.Id;
                            dart.HitsAir = true;
                            // PC Fire 4752E0 creates projectile 8 at plant +(20,-3), with a 40x40 collision box.
                            dart.X = p.Column + 20f / 80f;
                            dart.StartX = dart.X;
                            dart.PresentationPreviousX = dart.X;
                            dart.VelocityX = 2f * 100f / 80f;
                            dart.VelocityY = 0f;
                            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
                            dart.PresentationHomingY = 80f + p.Row * pitch - 3f
                                + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0, 5 - p.Column) * 20f - 10f : 0f)
                                - (_boardQueries.HasPlantKindAt(PlantKind.FlowerPot, p.Row, p.Column) ? 5f : 0f);
                            dart.PresentationPreviousHomingY = dart.PresentationHomingY;
                        }
                    }
                }

                var startsCattailSecondShot = p.Kind == PlantKind.Cattail && (int)Math.Round(p.ActionTimer * 100f) == 50;
                if (p.ActionTimer <= 0.000001f || startsCattailSecondShot)
                {
                    if (!startsCattailSecondShot)
                    {
                        p.ActionTimer = (LegacySimulation.DefinitionFor(p.Kind).ActionPeriodRaw - _world._random.Next(15)) * 0.01f;
                    }
                    var target = p.Kind == PlantKind.GatlingPea ? _boardQueries.FindAdventureTarget(p.Row, p.Column + 0.4f, 20f, false) != null : p.Kind == PlantKind.Cattail ? FindAdventureCattailTarget(p.Column + 0.5f, p.Row) != null : _legacySimulation.HasUpgradeAreaTarget(p);
                    if (target)
                    {
                        p.UpgradeBurstTicks = p.Kind == PlantKind.GloomShroom ? 200 : p.Kind == PlantKind.GatlingPea ? 100 : 50;
                        p.ShootAge = 0;
                        p.PresentationShotAge = 0f;
                    }
                }

                return true;
            }

            if (p.Kind == PlantKind.TwinSunflower)
            {
                if (p.ActionTimer <= 0.000001f)
                {
                    p.ActionTimer = (2350 + _world._random.Next(151)) * 0.01f;
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(4);
                    _world.Suns.Add(_sunSystem.CreateAdventurePresentationSun(p, 25));
                    _world.Suns.Add(_sunSystem.CreateAdventurePresentationSun(p, 25));
                    p.ActionCount++;
                }

                return true;
            }

            if (p.Kind == PlantKind.Spikerock)
            {
                if (p.ActionTimer <= 0.000001f)
                {
                    foreach (Zombie z in new PooledSnapshot<Zombie>(_world.Zombies))
                    {
                        if (!z.Hypnotized && !z.Airborne && !z.Submerged && z.DropRemaining <= 0 && z.Kind != ZombieKind.Bungee
                            && z.CatapultState?.FlatTires != true && z.ZomboniState?.FlatTires != true
                            && z.Row == p.Row && Math.Abs(z.X - (p.Column + 0.5f)) < 0.6f)
                        {
                            if (z.Kind == ZombieKind.Zomboni || z.Kind == ZombieKind.Catapult)
                            {
                                _combatSystem.HitAdventureZombie(z, 1800f, true, 32U);
                                _zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                                p.Health -= 50f;
                                p.VehicleHitsTaken++;
                                if (p.Health <= 0)
                                {
                                    _plantingSystem.RemoveAdventurePlantAndStack(p);
                                    break;
                                }
                            }
                            else
                            {
                                _combatSystem.HitAdventureZombie(z, 20f, true);
                                _zombieFeedbackSystem.RequestAdventureZombieFoley(1);
                            }
                        }
                    }

                    p.ActionTimer = 0.5f;
                }

                return true;
            }

            if (p.Kind == PlantKind.GoldMagnet)
            {
                if (p.ActionTimer <= 0.000001f)
                {
                    using var choicesLease = _world.CoinScratch.Rent();
                    var choices = choicesLease.Items;
                    foreach (var coin in _adventureModeRunner.Adventure._coins)
                    {
                        if (!coin.IsBeingCollected && coin.Age >= 0.5f && coin.ValueUnits > 0)
                        {
                            choices.Add(coin);
                        }
                    }

                    for (var i = 1; i < choices.Count; i++)
                    {
                        var coin = choices[i];
                        var distance = CoinDistance(coin, p);
                        var index = i - 1;
                        while (index >= 0 && CoinDistance(choices[index], p) > distance)
                        {
                            choices[index + 1] = choices[index];
                            index--;
                        }

                        choices[index + 1] = coin;
                    }
                    var count = Math.Min(5, choices.Count);
                    for (var i = 0; i < count; i++)
                    {
                        choices[i].IsBeingCollected = true;
                        choices[i].CollectorPlantId = p.Id;
                        choices[i]._collectionRemaining = 0.5f;
                    }

                    if (count > 0)
                    {
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(42);
                        p.ActionCount++;
                        p.ActionTimer = 2f + _world._random.Next(101) * 0.01f;
                    }
                    else
                    {
                        p.ActionTimer = 0.5f;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>Performs coin distance.</summary>
        /// <param name = "c">The c.</param>
        /// <param name = "p">The p.</param>
        /// <returns>The resulting coin distance.</returns>
        internal static float CoinDistance(AdventureCoin c, Plant p)
        {
            float x = c.X - (p.Column + 0.5f), y = c.Row - p.Row;
            return x * x + y * y;
        }

        /// <summary>Checks the PC Gloom-shroom's adjacent rows and horizontal body-rectangle overlap.</summary>
        /// <param name="z">The candidate zombie whose original logical body is tested.</param>
        /// <param name="p">The mushroom whose native position defines its 240-pixel attack rectangle.</param>
        /// <param name="requirePositiveOverlap">Whether a damage pulse requires positive overlap instead of acquisition's edge contact.</param>
        /// <returns>Whether the candidate passes the target gates and the requested native overlap boundary.</returns>
        internal static bool IsUpgradeAreaTarget(Zombie z, Plant p, bool requirePositiveOverlap = false)
        {
            if (z.Hypnotized || !AdventureZombieTargetingRules.CanBeAutoTargeted(z, p.Kind) || z.Airborne || z.Submerged
                || z.DropRemaining > 0 || z.Kind == ZombieKind.Bungee || z.Kind != ZombieKind.Boss && Math.Abs(z.Row - p.Row) > 1)
            {
                return false;
            }

            // PC 4765B0: (mX-80,mY-80,240,240). Both callers select rows, then compare horizontal bodies only.
            var left = 40 + p.Column * 80 + p.PositionOffsetX - 80;
            var body = AdventureZombieTargetingRules.BodyRectangle(z);
            var overlap = Math.Min(left + 240, body.X + body.Width) - Math.Max(left, body.X);
            // PC 475AA0 admits touching bodies; DoRowAreaDamage 46D030 requires strictly positive overlap.
            return requirePositiveOverlap ? overlap > 0 : overlap >= 0;
        }

        /// <summary>Performs find adventure cattail target.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        /// <returns>The resulting find adventure cattail target.</returns>
        internal Zombie? FindAdventureCattailTarget(float x, float row)
        {
            Zombie? best = null;
            var score = int.MinValue;
            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            var originX = 40f + x * 80f;
            var originY = 80f + row * pitch + 40f
                + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0f, 5f - (x - .5f)) * 20f - 10f : 0f);
            foreach (var zombie in _world.Zombies)
            {
                if (!_combatSystem.CanTakeAdventureAshDamage(zombie, 11U))
                {
                    continue;
                }

                var body = CattailTargetBody(zombie);
                var dx = body.X + body.Width / 2 - originX;
                var dy = body.Y + body.Height / 2 - originY;
                var candidate = -(int)Math.Sqrt(dx * dx + dy * dy) + (zombie.Airborne ? 10000 : 0);
                if (best == null || candidate > score)
                {
                    best = zombie;
                    score = candidate;
                }
            }

            return best;
        }

        /// <summary>Gets the original body in board pixels for homing aim and collision.</summary>
        /// <param name="zombie">The target whose physical row displacement is retained.</param>
        /// <returns>The original unscaled body rectangle.</returns>
        internal global::PlantsVsZombies.BoardUI.NativeRect CattailTargetBody(Zombie zombie)
        {
            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            var nativeX = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            var nativeY = 80f + zombie.Row * pitch - 30f + zombie.PresentationGarlicRowOffset
                + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0f, 5f - nativeX / 80f) * 20f - 10f : 0f)
                - (zombie.Kind == ZombieKind.Balloon ? 30f : 0f) - (zombie.Kind == ZombieKind.Pogo ? 16f : 0f)
                + zombie.PresentationVaultPositionY + (zombie.ModeHead == 31 ? 30f : 0f);
            return AdventureZombieTargetingRules.BodyRectangle(zombie, (int)nativeY);
        }

        /// <summary>Integrates PC 47CCB0 homing steering at two pixels per native tick without retargeting dead victims.</summary>
        /// <param name="p">The original cactus spike carrying its chosen target identity.</param>
        /// <param name="dt">One native simulation update in seconds.</param>
        internal void UpdateAdventureHomingVelocity(Pea p, float dt)
        {
            if (!p.Homing)
            {
                return;
            }

            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            foreach (var zombie in _world.Zombies)
            {
                if (zombie.Id != p.TargetZombieId || !_combatSystem.CanTakeAdventureAshDamage(zombie, 11U))
                {
                    continue;
                }

                var body = CattailTargetBody(zombie);
                var dx = body.X + body.Width / 2 - (40f + p.X * 80f + 20f);
                var dy = body.Y + body.Height / 2 - (p.PresentationHomingY + 20f);
                var distance = (float)Math.Sqrt(dx * dx + dy * dy);
                if (distance > .000001f)
                {
                    var acceleration = .001f * (float)Math.Round((p.PresentationNativeAge + dt) * 100f);
                    var vx = p.VelocityX * 80f / 100f + dx / distance * acceleration;
                    var vy = p.VelocityY * pitch / 100f + dy / distance * acceleration;
                    var magnitude = (float)Math.Sqrt(vx * vx + vy * vy);
                    if (magnitude > .000001f)
                    {
                        p.VelocityX = vx / magnitude * 2f * 100f / 80f;
                        p.VelocityY = vy / magnitude * 2f * 100f / pitch;
                    }
                }
                break;
            }

            p.PresentationPreviousHomingY = p.PresentationHomingY;
            p.PresentationHomingY += p.VelocityY * pitch * dt;
        }

        /// <summary>Performs morph adventure imitater.</summary>
        /// <param name = "p">The p.</param>
        internal void MorphAdventureImitater(Plant p)
        {
            var target = p.ImitaterTarget;
            _world.Plants.Remove(p);
            if (!target.HasValue)
            {
                return;
            }

            if (target.Value == PlantKind.CoffeeBean)
            {
                var main = _boardQueries.AdventurePlantAt(p.Row, p.Column, PlantLayer.Main);
                if (main != null && main.Asleep)
                {
                    _plantingSystem.PlaceAdventurePlant(target.Value, p.Row, p.Column);
                }
            }
            else
            {
                var transformed = _plantingSystem.RequiredPlaceAdventurePlant(target.Value, p.Row, p.Column);
                transformed.Id = p.Id;
                transformed.IsImitated = true;
                transformed.ImitaterTarget = target;
            }

            _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.ImitaterMorphed, (int)target.Value, p.Row, p.Column);
        }

        /// <summary>Performs try aim adventure cob cannon.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try aim adventure cob cannon.</returns>
        internal bool TryAimAdventureCobCannon(int row, int column)
        {
            var p = _world.IsAdventure ? _boardQueries.AdventurePlantAt(row, column, PlantLayer.Main) : null;
            return p != null && TryAimAdventureCobCannon(p.Id);
        }

        /// <summary>Performs try aim adventure cob cannon.</summary>
        /// <param name = "plantId">The plant id.</param>
        /// <returns>The resulting try aim adventure cob cannon.</returns>
        internal bool TryAimAdventureCobCannon(int plantId)
        {
            if (!_world.IsAdventure || _world.State != MatchState.Playing || _adventureModeRunner.Adventure.Phase != AdventurePhase.Playing)
            {
                return false;
            }

            var cannon = _world.Plants.Find(p => p.Id == plantId && p.Kind == PlantKind.CobCannon);
            if (cannon == null || cannon.CannonPhase != CobCannonPhase.Ready)
            {
                return false;
            }

            CancelAdventureCobCannonAim();
            cannon.CannonPhase = CobCannonPhase.Aiming;
            var board = _adventureModeRunner.Adventure;
            board.SelectedCobCannonId = plantId;
            board.CobCannonAimDelayTicks = 30;
            board.CobCannonAimX = 80f + cannon.Column * 80f;
            board.CobCannonAimY = 120f + cannon.Row * (board.Level.HasPool || board.Level.HasRoof ? 85f : 100f)
                + (board.Level.HasRoof ? Math.Max(0f, 5f - cannon.Column) * 20f - 10f : 0f);
            _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.CobAimStarted, plantId, cannon.Row, cannon.Column);
            return true;
        }

        /// <summary>Performs cancel adventure cob cannon aim.</summary>
        /// <returns>The resulting cancel adventure cob cannon aim.</returns>
        internal bool CancelAdventureCobCannonAim()
        {
            if (!_world.IsAdventure || !_adventureModeRunner.Adventure.SelectedCobCannonId.HasValue)
            {
                return false;
            }

            var p = _world.Plants.Find(c => c.Id == _adventureModeRunner.Adventure.SelectedCobCannonId.Value && c.Kind == PlantKind.CobCannon);
            if (p != null && p.CannonPhase == CobCannonPhase.Aiming)
            {
                p.CannonPhase = CobCannonPhase.Ready;
            }

            _adventureModeRunner.Adventure.SelectedCobCannonId = null;
            _adventureModeRunner.Adventure.CobCannonAimDelayTicks = 0;
            return true;
        }

        /// <summary>Performs try fire adventure cob cannon.</summary>
        /// <param name = "targetX">The target x.</param>
        /// <param name = "targetRow">The target row.</param>
        /// <returns>The resulting try fire adventure cob cannon.</returns>
        internal bool TryFireAdventureCobCannon(float targetX, float targetRow)
        {
            if (!_world.IsAdventure || _world.State != MatchState.Playing || _adventureModeRunner.Adventure.Phase != AdventurePhase.Playing || !_adventureModeRunner.Adventure.SelectedCobCannonId.HasValue || float.IsNaN(targetX) || float.IsInfinity(targetX) || float.IsNaN(targetRow) || float.IsInfinity(targetRow) || targetX < -.5f || targetX >= 9.5f)
            {
                return false;
            }

            var p = _world.Plants.Find(c => c.Id == _adventureModeRunner.Adventure.SelectedCobCannonId.Value && c.Kind == PlantKind.CobCannon && c.CannonPhase == CobCannonPhase.Aiming);
            if (p == null)
            {
                return false;
            }

            var board = _adventureModeRunner.Adventure;
            var nativeX = 40f + targetX * 80f;
            var nativeY = 80f + targetRow * (board.Level.HasPool || board.Level.HasRoof ? 85f : 100f)
                + (board.Level.HasRoof ? Math.Max(0f, 5f - (float)Math.Floor(targetX)) * 20f - 10f : 0f);
            if (nativeY < 80f || nativeY >= 600f)
            {
                return false;
            }
            var dx = nativeX - board.CobCannonAimX;
            var dy = nativeY - board.CobCannonAimY;
            if (board.CobCannonAimDelayTicks > 0 && dx * dx + dy * dy < 2500f)
            {
                return false;
            }

            p.CannonTargetX = targetX;
            // PC 475230 stores mouse X - 47 before 4752E0 selects the target row at that shifted roof column.
            var rowColumn = Math.Max(0, Math.Min(8, (int)Math.Floor(targetX - 47f / 80f)));
            // PC PixelToGridY 428160 uses (4-column)*20; GridToPixelY's drawing offset is a separate formula.
            var rowFloor = board.Level.HasRoof ? Math.Max(0, 4 - rowColumn) * 20f : 0f;
            p.CannonTargetRow = Math.Max(0f, Math.Min(_world.RowCount - 1,
                (float)Math.Floor((nativeY - 80f - rowFloor) / (board.Level.HasPool || board.Level.HasRoof ? 85f : 100f))));
            p.CannonPhase = CobCannonPhase.Firing;
            p.PhaseAge = 0f;
            p.CannonReloadRemaining = SimulationWorld.AdventureCobFiringSeconds;
            _adventureModeRunner.Adventure.SelectedCobCannonId = null;
            _adventureModeRunner.Adventure.CobCannonAimDelayTicks = 0;
            return true;
        }

        /// <summary>Performs try fire adventure cob cannon at cell.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting try fire adventure cob cannon at cell.</returns>
        internal bool TryFireAdventureCobCannonAtCell(int row, int column)
        {
            return TryFireAdventureCobCannon(column + 0.5f, row);
        }

        /// <summary>Performs advance adventure cannon.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureCannon(Plant p, float dt)
        {
            if (p.CannonPhase == CobCannonPhase.Ready || p.CannonPhase == CobCannonPhase.Aiming)
            {
                return;
            }

            var previousRemaining = p.CannonReloadRemaining;
            p.CannonReloadRemaining = Math.Max(0, (int)Math.Round(previousRemaining * 100f) - (int)Math.Round(dt * 100f)) * .01f;
            var cueRemaining = p.CannonPhase == CobCannonPhase.Loading
                ? SimulationWorld.AdventureCobLoadingSeconds - .5f * 15f / 12f
                : SimulationWorld.AdventureCobFiringSeconds - .48f * 42f / 12f;
            if ((p.CannonPhase == CobCannonPhase.Loading || p.CannonPhase == CobCannonPhase.Firing)
                && previousRemaining > cueRemaining + .000001f && p.CannonReloadRemaining <= cueRemaining + .000001f)
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(p.CannonPhase == CobCannonPhase.Loading ? 64 : 76);
            }

            var releaseRemaining = SimulationWorld.AdventureCobFiringSeconds - AdventureCobCannonRules.ReleaseSeconds;
            if (p.CannonPhase == CobCannonPhase.Firing && previousRemaining > releaseRemaining + .000001f
                && p.CannonReloadRemaining <= releaseRemaining + .000001f)
            {
                var level = _adventureModeRunner.Adventure.Level;
                var pitch = level.HasPool || level.HasRoof ? 85f : 100f;
                // PC CobCannonFire subtracts 47, Fire subtracts 40, and the impact adds 80 pixels.
                var impactX = p.CannonTargetX - 7f / 80f;
                var targetColumn = Math.Max(0, Math.Min(8, (int)Math.Floor(p.CannonTargetX - 87f / 80f)));
                var targetY = 80f + p.CannonTargetRow * pitch
                    + (level.HasRoof ? Math.Max(0, 5 - targetColumn) * 20f - 10f : 0f);
                _adventureModeRunner.Adventure._cobShells.Add(new AdventureCobShell
                {
                    SourcePlantId = p.Id,
                    TargetX = impactX,
                    TargetRow = p.CannonTargetRow,
                    TargetNativeY = targetY,
                    LaunchX = p.Column + 37f / 80f,
                    LaunchRow = p.Row,
                    LaunchNativeY = 80f + p.Row * pitch - 145f
                        + (level.HasRoof ? Math.Max(0, 5 - p.Column) * 20f - 10f : 0f)
                        - (_boardQueries.HasPlantKindAt(PlantKind.FlowerPot, p.Row, p.Column) ? 5f : 0f),
                    FlightRemaining = SimulationWorld.AdventureCobFlightSeconds + (level.IsWaterRow((int)p.CannonTargetRow) ? .02f : 0f)
                });
                _zombieFeedbackSystem.RequestAdventureZombieFoley(3);
                _world.Presentation._nativeShootSuppressions++;
                _legacySimulation.Raise(GameEvent.Shoot);
                _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.CobFired, p.Id, p.Row, p.Column);
            }

            if (p.CannonReloadRemaining > 0.000001f)
            {
                return;
            }

            p.PhaseAge = 0f;
            if (p.CannonPhase == CobCannonPhase.Arming || p.CannonPhase == CobCannonPhase.Reloading)
            {
                p.CannonPhase = CobCannonPhase.Loading;
                p.CannonReloadRemaining = SimulationWorld.AdventureCobLoadingSeconds;
            }
            else if (p.CannonPhase == CobCannonPhase.Loading)
            {
                p.CannonPhase = CobCannonPhase.Ready;
                _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.CobReady, p.Id, p.Row, p.Column);
            }
            else if (p.CannonPhase == CobCannonPhase.Firing)
            {
                p.CannonPhase = CobCannonPhase.Reloading;
                p.CannonReloadRemaining = 30f;
                p.CannonResetVersion++;
            }
        }
    }
}
