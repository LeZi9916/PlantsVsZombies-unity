#nullable enable
using PlantsVsZombies.Simulation;
using PlantsVsZombies.VerifiedData;
using System.Collections.Generic;
using System.IO;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Answers which reanimation clips exist and which animation set each zombie kind uses, caching the immutable lookups.</summary>
    internal sealed class AdventureAnimationCatalog
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose reanimation renderer answers clip queries.</param>
        internal AdventureAnimationCatalog(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Stores adventure clips.</summary>
        private readonly Dictionary<(string Asset, string? Clip), bool> _adventureClips = new Dictionary<(string, string?), bool>();

        /// <summary>Performs adventure clip exists.</summary>
        /// <param name = "name">The name.</param>
        /// <param name = "clip">The clip.</param>
        /// <returns>The resulting adventure clip exists.</returns>
        internal bool AdventureClipExists(string name, string? clip)
        {
            var key = (name, clip);
            bool exists;
            if (!_adventureClips.TryGetValue(key, out exists))
            {
                global::PlantsVsZombies.Reanimation.AnimationInfo info;
                exists = _owner.RequiredAnimations.TryGetAnimationInfo(name, clip, out info);
                _adventureClips.Add(key, exists);
            }

            return exists;
        }

        /// <summary>Caches immutable catalog animation paths for zombie kinds.</summary>
        private static readonly Dictionary<ZombieKind, string> s_zombieAnimationNames = new Dictionary<ZombieKind, string>();
        /// <summary>Performs adventure zombie animation.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure zombie animation.</returns>
        internal static string AdventureZombieAnimation(ZombieKind kind)
        {
            if (!s_zombieAnimationNames.TryGetValue(kind, out var name))
            {
                ReanimationDefinition? definition;
                name = VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Zombies[(int)kind].ReanimationId, out definition) ? Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/')) : "Zombie";
                s_zombieAnimationNames.Add(kind, name);
            }

            return name;
        }
    }
}
