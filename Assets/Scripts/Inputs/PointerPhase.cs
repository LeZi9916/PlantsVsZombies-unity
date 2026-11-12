#nullable enable

namespace PlantsVsZombies.Inputs
{
    /// <summary>Describes a pointer transition sampled independently of interface rendering.</summary>
    public enum PointerPhase
    {
        /// <summary>The pointer moved without a primary press.</summary>
        Move,
        /// <summary>The primary pointer was pressed this frame.</summary>
        Down,
        /// <summary>The primary pointer remains pressed.</summary>
        Drag,
        /// <summary>The primary pointer was released this frame.</summary>
        Up
    }
}
