#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns RewardSystem responsibilities for one deterministic match.</summary>
    internal sealed class RewardSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BattlePresentationSystem operations.</summary>
        private BattlePresentationSystem _battlePresentationSystem = null!;
        /// <summary>Provides BoardEnvironment operations.</summary>
        private BoardEnvironment _boardEnvironment = null!;
        /// <summary>Provides CoinSystem operations.</summary>
        private CoinSystem _coinSystem = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides VaseSystem operations.</summary>
        private VaseSystem _vaseSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal RewardSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="battlePresentationSystem">The BattlePresentationSystem collaborator.</param>
        /// <param name="boardEnvironment">The BoardEnvironment collaborator.</param>
        /// <param name="coinSystem">The CoinSystem collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="vaseSystem">The VaseSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BattlePresentationSystem battlePresentationSystem, BoardEnvironment boardEnvironment, CoinSystem coinSystem, ProjectileSystem projectileSystem, SunSystem sunSystem, VaseSystem vaseSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _battlePresentationSystem = battlePresentationSystem;
            _boardEnvironment = boardEnvironment;
            _coinSystem = coinSystem;
            _projectileSystem = projectileSystem;
            _sunSystem = sunSystem;
            _vaseSystem = vaseSystem;
        }

        /// <summary>Performs remember adventure reward position.</summary>
        /// <param name = "zombie">The zombie.</param>
        internal void RememberAdventureRewardPosition(Zombie zombie)
        {
            // PC 0x54DE40: GetZombieRect centre. The basic body rect is (36,0,42,115).
            var scale = AdventureDancerRules.Scale(zombie);
            var sourceX = 40f + zombie.X * 80f - 57f * scale - 20f * (1f - scale);
            var floor = 80f + zombie.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f);
            if (_adventureModeRunner.Adventure.Level.HasRoof)
            {
                floor += Math.Max(0, 440f - (sourceX + 40f)) * .25f - 10f;
            }

            var sourceY = floor - 30f;
            if (zombie.Kind == ZombieKind.Balloon)
            {
                sourceY -= 30f;
            }

            int rectX = 36, rectY = 0, rectW = 42, rectH = 115;
            // Literal initial body rectangles from PC ZombieInitialize 0x53DD90.
            switch (zombie.Kind)
            {
                case ZombieKind.Football:
                    rectX = 50;
                    rectW = 57;
                    break;
                case ZombieKind.Digger:
                    rectX = 50;
                    rectW = 28;
                    break;
                case ZombieKind.Gargantuar:
                    rectX = -17;
                    rectY = -38;
                    rectW = 125;
                    rectH = 154;
                    break;
                case ZombieKind.Zomboni:
                case ZombieKind.Catapult:
                    rectX = 0;
                    rectY = -13;
                    rectW = 153;
                    rectH = 140;
                    break;
                case ZombieKind.Snorkel:
                    rectX = 12;
                    rectW = 62;
                    break;
                case ZombieKind.Bobsled:
                    if (zombie.PresentationHasSled ?? !zombie.UsedAbility)
                    {
                        rectX = -50;
                        rectW = 275;
                    }

                    break;
                case ZombieKind.Balloon:
                    rectY = 30;
                    break;
                case ZombieKind.Bungee:
                    rectX = -20;
                    rectY = 22;
                    rectW = 110;
                    rectH = 94;
                    break;
                case ZombieKind.Boss:
                    rectX = 700;
                    rectY = 80;
                    rectW = 90;
                    rectH = 430;
                    break;
            }

            // PC GetZombieRect (0x54DE40) mirrors the local body rectangle after mind control.
            if (zombie.Hypnotized)
            {
                var bodyWidth = zombie.Kind == ZombieKind.Gargantuar ? 180 : 120;
                rectX = bodyWidth - rectW - rectX;
            }

            _adventureModeRunner.Adventure._lastRewardX = (int)sourceX + (rectX + rectW / 2) * scale + 20f * (1f - scale);
            _adventureModeRunner.Adventure._lastRewardY = (int)sourceY + (rectY + rectH / 2) * scale + 120f * (1f - scale);
        }

        /// <summary>Performs spawn adventure reward presentation.</summary>
        internal void SpawnAdventureRewardPresentation()
        {
            var kind = AdventureRewardPresentation.NativeTypeFor(_adventureModeRunner.Adventure.Level);
            int width = 60, height = 60;
            if (kind == 7)
            {
                width = 50;
                height = 70;
            }
            else if (kind == 15)
            {
                width = 78;
                height = 52;
            }
            else if (kind >= 9 && kind <= 14)
            {
                width = 80;
                height = 80;
            }
            else if (kind == 21)
            {
                width = 157;
                height = 269;
            }
            else if (kind == 18)
            {
                width = 91;
                height = 78;
            }

            _adventureModeRunner.Adventure.RewardPresentation = new AdventureRewardPresentation(kind, _adventureModeRunner.Adventure._lastRewardX, _adventureModeRunner.Adventure._lastRewardY, width, height, (float)_world._random.NextDouble(), (float)_world._random.NextDouble(), _world._random.Next(20), _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Boss);
        }

        /// <summary>Performs advance adventure reward presentation.</summary>
        /// <param name = "delta">The delta.</param>
        internal void AdvanceAdventureRewardPresentation(float delta)
        {
            if (!_world.IsAdventure || _world.State != MatchState.Won || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta))
            {
                return;
            }

            if (_world.ModeSession != null)
            {
                _world._completedBoardAccumulated += delta;
                while (_world._completedBoardAccumulated + 0.0000001 >= SimulationWorld.SimulationStep)
                {
                    AdvanceAdventureRewardTick();
                    _world._completedBoardAccumulated -= SimulationWorld.SimulationStep;
                }

                return;
            }

            if (_adventureModeRunner.Adventure.RewardPresentation == null)
            {
                return;
            }

            _adventureModeRunner.Adventure.RewardPresentation.Update(delta, _world._rewardPresentationTick);
            if (!_adventureModeRunner.Adventure._rewardLightfillPlayed && _adventureModeRunner.Adventure.RewardPresentation.NativeType != 15 && _adventureModeRunner.Adventure.RewardPresentation.CollectionTicks >= 300)
            {
                _adventureModeRunner.Adventure._rewardLightfillPlayed = true;
                _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.RewardFade);
            }
        }

        /// <summary>Advances the existing board artwork for one native reward-presentation tick.</summary>
        internal void AdvanceAdventureRewardTick()
        {
            _boardEnvironment.AdvanceAdventurePoolEffect();
            _world.Elapsed += .01f;
            if (_adventureModeRunner.Adventure.Boss?.DeathComplete == true)
            {
                _adventureModeRunner.Adventure.Boss.AdvanceDriver();
                _adventureModeRunner.Adventure.Boss.AdvanceEffects();
            }
            foreach (var mower in _world.Mowers)
            {
                if (mower.SquishedTicks > 0)
                {
                    mower.SquishedTicks--;
                }
            }
            _battlePresentationSystem.AdventurePresentationTick(.01f);
            // Original Board::UpdateGameObjects keeps existing projectiles moving through the award sequence.
            _projectileSystem.AdvanceAdventureProjectiles(.01f, allowDamage: false);
            _sunSystem.AdvanceAdventurePresentationSuns(.01f);
            _vaseSystem.AdvanceVasePackets();
            _coinSystem.AdvanceAdventureCoins(.01f);
            _boardEnvironment.AdvanceAdventureStormTick();
            foreach (var plant in _world.Plants)
            {
                plant.Age += .01f;
                if (AdventureProducerGlow.HasNativeProductionHint(plant.Kind))
                {
                    AdventureProducerGlow.Tick(plant, false, 0, false);
                }
            }
        }
    }
}
