namespace PvZReplica.Adventure
{
    public static class AdventureZombieTargetingRules
    {
        // PC Plant::FindTargetZombie 0x475AA0: has-head is required by these three contact plants only.
        // The separate PC EffectedByDamage gate rejects dying/burned/mowed/dead actors.
        public static bool CanBeAutoTargeted(Zombie z,PlantKind kind=PlantKind.Peashooter)
        {
            if(z==null||z.Health<=0||z.PresentationNativePhase>=1&&z.PresentationNativePhase<=3)return false;
            return !z.Headless||kind!=PlantKind.PotatoMine&&kind!=PlantKind.Chomper&&kind!=PlantKind.TangleKelp;
        }
    }
}
