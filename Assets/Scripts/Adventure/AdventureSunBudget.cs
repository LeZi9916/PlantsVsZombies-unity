using System;

namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        // PC Board::CanTakeSunMoney includes suns already flying to the bank (0x4275A0/4276D0).
        // Keep the existing nonnegative pickup-rule contract by representing native negative
        // mSunMoney as visible money plus a reservation settled by the arriving pickups.
        private int adventureSunReservation;
        public int AdventureSunAvailable
        {
            get
            {
                int total = SunAmount - adventureSunReservation;
                if (IsAdventure) foreach (Sun sun in Suns)
                    if (sun.PresentationNative && sun.PresentationPickup.Collecting && !sun.PresentationPickup.Dead) total += sun.Value;
                return total;
            }
        }
        private bool SpendAdventureSun(int cost)
        {
            if (cost < 0 || AdventureSunAvailable < cost) return false;
            int visible = Math.Min(SunAmount, cost);
            SunAmount -= visible; adventureSunReservation += cost - visible;
            return true;
        }
        private void SettleAdventureSunReservation()
        {
            int paid = Math.Min(SunAmount, adventureSunReservation);
            SunAmount -= paid; adventureSunReservation -= paid;
        }
    }
}
