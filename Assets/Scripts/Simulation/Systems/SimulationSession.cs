#nullable enable

using PlantsVsZombies.Randomness;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SimulationSession responsibilities for one deterministic match.</summary>
    internal sealed class SimulationSession
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides LevelOneModeRunner operations.</summary>
        private LevelOneModeRunner _levelOneModeRunner = null!;
        /// <summary>Provides RewardSystem operations.</summary>
        private RewardSystem _rewardSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal SimulationSession(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="levelOneModeRunner">The LevelOneModeRunner collaborator.</param>
        /// <param name="rewardSystem">The RewardSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, LevelOneModeRunner levelOneModeRunner, RewardSystem rewardSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _levelOneModeRunner = levelOneModeRunner;
            _rewardSystem = rewardSystem;
        }
        /// <summary>Initializes game simulation.</summary>
        /// <param name = "rules">The rules.</param>
        /// <param name = "randomSeed">The random seed.</param>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal void Initialize(GameRules? rules = null, int randomSeed = 12345)
        {
            _world._rewardPresentationTick = _rewardSystem.AdvanceAdventureRewardTick;
            _world.Rules = rules ?? new GameRules();
            _world.IsExperimentalLevelOne = _world.Rules.EnableExperimentalLevelOne;
            _world.IsAdventure = _world.Rules.AdventureLevel != 0;
            if (_world.IsAdventure && _world.IsExperimentalLevelOne)
            {
                throw new ArgumentException("Adventure and the legacy 1-1 experiment are separate modes.");
            }

            _world._random = new global::PlantsVsZombies.Randomness.OriginalRandomAdapter(unchecked((uint)randomSeed));
            _world._gravePresentationRandom = new OriginalRandomAdapter(unchecked((uint)randomSeed));
            _world.State = MatchState.Playing;
            _world.SunAmount = _world.Rules.StartingSun;
            _world._nextZombie = _world.Rules.FirstZombieDelay;
            _world._nextSkySun = _world.Rules.SkySunInterval;
            for (var row = 0; row < SimulationWorld.Rows; row++)
            {
                _world.Mowers.Add(new Mower { Row = row, X = -0.5f, Available = _world.Rules.MowersEnabled });
            }

            if (_world.IsAdventure)
            {
                var initialized = false;
                _adventureModeRunner.InitializeAdventure(ref initialized);
                if (!initialized)
                {
                    throw new InvalidOperationException("Adventure requires the Adventure module.");
                }
            }

            if (_world.IsExperimentalLevelOne)
            {
                var initialized = false;
                _levelOneModeRunner.InitializeExperimentalLevelOne(ref initialized);
                if (!initialized)
                {
                    throw new InvalidOperationException("Experimental level 1-1 requires the Levels module.");
                }
            }
        }
    }
}
