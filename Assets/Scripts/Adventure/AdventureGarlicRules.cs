#nullable enable
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Preserves PC 547230/547280 garlic face selection and track visibility.</summary>
    internal static class AdventureGarlicRules
    {
        /// <summary>Checks whether the PC actor supports the original gross-out face.</summary>
        /// <param name="kind">The original zombie species.</param>
        /// <returns>Whether this species takes the long garlic pause.</returns>
        internal static bool HasFace(ZombieKind kind)
        {
            return kind == ZombieKind.Normal || kind == ZombieKind.Flag || kind == ZombieKind.Conehead
                || kind == ZombieKind.Buckethead || kind == ZombieKind.ScreenDoor || kind == ZombieKind.DuckyTube
                || kind == ZombieKind.Dancing || kind == ZombieKind.BackupDancer || (int)kind == 33 || kind == ZombieKind.Newspaper
                || kind == ZombieKind.PoleVaulter;
        }

        /// <summary>Checks the original species before plant-headed actors are mapped onto the shared ordinary body.</summary>
        /// <param name="zombie">The actor retaining its original plant-head type.</param>
        /// <returns>Whether the actual native species supports the gross-out image.</returns>
        internal static bool HasFace(Zombie zombie)
        {
            return HasFace(zombie.ModeHead >= 26 ? (ZombieKind)zombie.ModeHead : zombie.Kind);
        }

        /// <summary>Checks whether the retained native gross-out face is currently displayed.</summary>
        /// <param name="zombie">The actor carrying its garlic update counter.</param>
        /// <returns>Whether the original head image overrides its ordinary head.</returns>
        internal static bool ShowsFace(Zombie zombie)
        {
            return !zombie.Headless && HasFace(zombie) && zombie.PresentationGarlicCounter >= 70;
        }

        /// <summary>Filters the three tracks hidden while the original gross-out head is attached.</summary>
        /// <param name="track">The native reanimation track name.</param>
        /// <returns>Whether the track remains visible with the replacement head.</returns>
        internal static bool FaceTrackVisible(string track)
        {
            return track != "anim_head2" && track != "anim_head_jaw" && track != "anim_tongue";
        }
    }
}
