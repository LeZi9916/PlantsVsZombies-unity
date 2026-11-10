#nullable enable

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure burn pose.</summary>
    public sealed class AdventureBurnPose
    {
        /// <summary>Stores visual.</summary>
        public AdventureBurnVisual Visual;
        /// <summary>Stores reanimation id.</summary>
        public int ReanimationId = -1;
        /// <summary>Stores animation.</summary>
        public string Animation;
        /// <summary>Stores x, y, scale, pivot x, pivot y.</summary>
        public float X, Y, Scale = 1f, PivotX, PivotY;
        /// <summary>Stores facing left, hide imp.</summary>
        public bool FacingLeft, HideImp;
    }
}
