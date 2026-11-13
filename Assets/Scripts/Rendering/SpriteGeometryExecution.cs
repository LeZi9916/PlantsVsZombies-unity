#nullable enable
namespace PlantsVsZombies.Rendering
{
    /// <summary>Selects the original immediate oracle or one of the bounded deferred geometry executors.</summary>
    internal enum SpriteGeometryExecution
    {
        /// <summary>Uses the original managed per-sprite implementation.</summary>
        Immediate,
        /// <summary>Executes the numeric job directly as managed code.</summary>
        Managed,
        /// <summary>Runs the synchronous job through Burst when available, with Unity's managed fallback.</summary>
        Burst
    }
}
