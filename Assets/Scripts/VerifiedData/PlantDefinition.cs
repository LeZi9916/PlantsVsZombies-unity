#nullable enable

namespace PlantsVsZombies.VerifiedData
{
    /// <summary>
    /// A static plant row. Costs and recharge thresholds are base values that runtime
    /// rules may override. RechargeThresholdRaw is not a verified duration in seconds.
    /// </summary>
    public sealed class PlantDefinition
    {
        /// <summary>Creates one verified static plant-table row.</summary>
        /// <param name = "id">The zero-based original plant identifier.</param>
        /// <param name = "internalName">The original internal plant name.</param>
        /// <param name = "sunCost">The base price in sun units.</param>
        /// <param name = "rechargeThresholdRaw">The raw recharge counter threshold.</param>
        /// <param name = "reanimationId">The original animation-table identifier.</param>
        /// <param name = "actionDispatchCodeRaw">The raw action-dispatch selector.</param>
        /// <param name = "actionPeriodRaw">The raw action-period value.</param>
        /// <param name = "unidentified0x04">The uninterpreted value at original row offset 0x04.</param>
        /// <param name = "unidentified0x0c">The uninterpreted value at original row offset 0x0C.</param>
        internal PlantDefinition(int id, string internalName, int sunCost, int rechargeThresholdRaw, int reanimationId, int actionDispatchCodeRaw, int actionPeriodRaw, int unidentified0x04, int unidentified0x0c)
        {
            Id = id;
            InternalName = internalName;
            SunCost = sunCost;
            RechargeThresholdRaw = rechargeThresholdRaw;
            ReanimationId = reanimationId;
            ActionDispatchCodeRaw = actionDispatchCodeRaw;
            ActionPeriodRaw = actionPeriodRaw;
            Unidentified0x04 = unidentified0x04;
            Unidentified0x0C = unidentified0x0c;
        }

        /// <summary>Gets the original zero-based plant identifier.</summary>
        public int Id { get; }
        /// <summary>Gets the internal uppercase plant name recorded in the verified table.</summary>
        public string InternalName { get; }
        /// <summary>Gets the base sun cost before mode-specific runtime adjustments.</summary>
        public int SunCost { get; }
        /// <summary>Gets the original recharge threshold in raw counter units, not seconds.</summary>
        public int RechargeThresholdRaw { get; }
        /// <summary>Gets the original reanimation identifier used by the plant.</summary>
        public int ReanimationId { get; }
        /// <summary>Gets the original action-dispatch code without interpreting it as a gameplay action.</summary>
        public int ActionDispatchCodeRaw { get; }
        /// <summary>Gets the original action-period field in raw counter units.</summary>
        public int ActionPeriodRaw { get; }
        /// <summary>Gets the uninterpreted integer stored at offset 0x04 in the original row.</summary>
        public int Unidentified0x04 { get; }
        /// <summary>Gets the uninterpreted integer stored at offset 0x0C in the original row.</summary>
        public int Unidentified0x0C { get; }
    }
}
