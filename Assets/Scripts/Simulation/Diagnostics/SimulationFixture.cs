#nullable enable

using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Provides explicit mutable ownership access for isolated tests and diagnostic boards.</summary>
    internal static class SimulationFixture
    {
        /// <summary>Applies an explicit animation transition after a fixture changes native action state.</summary>
        /// <param name="game">The isolated match.</param>
        /// <param name="zombie">The actor whose fixture state changed.</param>
        /// <returns>The synchronized native clip.</returns>
        internal static string SynchronizeZombieAnimation(GameSimulation game, Zombie zombie)
        {
            return ((ZombieFeedbackSystem)game.MemberOwner("AdventurePresentationZombieAnimation")).SynchronizeAdventureZombieAnimation(zombie);
        }
        /// <summary>Gets the plant owner for fixture setup.</summary>
        /// <param name="game">The isolated match.</param>
        /// <returns>The ordered plant store.</returns>
        internal static EntityStore<Plant> Plants(GameSimulation game)
        {
            return ((SimulationWorld)game.MemberOwner("Plants")).Plants;
        }
        /// <summary>Gets the zombie owner for fixture setup.</summary>
        /// <param name="game">The isolated match.</param>
        /// <returns>The ordered zombie store.</returns>
        internal static EntityStore<Zombie> Zombies(GameSimulation game)
        {
            return ((SimulationWorld)game.MemberOwner("Zombies")).Zombies;
        }
        /// <summary>Gets mutable Peas ownership for isolated fixture setup.</summary>
        /// <param name="game">The isolated match.</param>
        /// <returns>The owned fixture collection.</returns>
        internal static List<Pea> Peas(GameSimulation game)
        {
            return ((SimulationWorld)game.MemberOwner("Peas")).Peas;
        }
        /// <summary>Gets mutable Suns ownership for isolated fixture setup.</summary>
        /// <param name="game">The isolated match.</param>
        /// <returns>The owned fixture collection.</returns>
        internal static List<global::PlantsVsZombies.Simulation.Sun> Suns(GameSimulation game)
        {
            return ((SimulationWorld)game.MemberOwner("Suns")).Suns;
        }
        /// <summary>Gets mutable Mowers ownership for isolated fixture setup.</summary>
        /// <param name="game">The isolated match.</param>
        /// <returns>The owned fixture collection.</returns>
        internal static List<Mower> Mowers(GameSimulation game)
        {
            return ((SimulationWorld)game.MemberOwner("Mowers")).Mowers;
        }
        /// <summary>Gets mutable Flames ownership for isolated fixture setup.</summary>
        /// <param name="game">The isolated match.</param>
        /// <returns>The owned fixture collection.</returns>
        internal static List<LaneFlame> Flames(GameSimulation game)
        {
            return ((SimulationWorld)game.MemberOwner("Flames")).Flames;
        }
    }
}
