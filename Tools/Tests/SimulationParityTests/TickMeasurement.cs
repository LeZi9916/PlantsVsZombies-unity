#nullable enable
namespace PlantsVsZombies.Tests.SimulationParity
{
    /// <summary>Records the distribution and allocation of one warmed native-update fixture.</summary>
    internal sealed class TickMeasurement
    {
        /// <summary>Gets median elapsed milliseconds per update.</summary>
        public double MedianMilliseconds { get; }
        /// <summary>Gets the 95th percentile elapsed milliseconds per update.</summary>
        public double P95Milliseconds { get; }
        /// <summary>Gets bytes allocated by the measured updates, including gameplay effects.</summary>
        public long AllocatedBytes { get; }
        /// <summary>Creates an immutable fixture measurement.</summary>
        /// <param name="median">The median native update duration.</param>
        /// <param name="p95">The 95th percentile native update duration.</param>
        /// <param name="allocated">Bytes allocated by all measured updates.</param>
        internal TickMeasurement(double median, double p95, long allocated)
        {
            MedianMilliseconds = median;
            P95Milliseconds = p95;
            AllocatedBytes = allocated;
        }
    }
}
