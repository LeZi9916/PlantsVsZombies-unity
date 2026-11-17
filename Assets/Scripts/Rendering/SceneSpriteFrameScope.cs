#nullable enable
using System;

namespace PlantsVsZombies.Rendering
{
    /// <summary>Completes one scene presentation even when drawing returns early or throws.</summary>
    internal readonly struct SceneSpriteFrameScope : IDisposable
    {
        /// <summary>Flushes pending geometry and closes unused scene pools.</summary>
        public void Dispose()
        {
            ObjectSpriteRenderer.EndSceneFrame();
        }
    }
}
