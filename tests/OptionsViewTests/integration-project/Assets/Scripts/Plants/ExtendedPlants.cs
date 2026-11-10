namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private const float JalapenoDamage = 1800f;

        private bool UpdateExtendedPlant(int index, Plant plant)
        {
            if (plant.Kind == PlantKind.TallNut) return true;
            if (plant.Kind != PlantKind.Jalapeno) return false;
            // One hundred 0.01-second float steps can leave a tiny positive remainder.
            if (plant.ActionTimer > 0.000001f) return true;

            for (int z = Zombies.Count - 1; z >= 0; z--)
            {
                Zombie zombie = Zombies[z];
                if (zombie.Row != plant.Row) continue;

                switch (zombie.Kind)
                {
                    case ZombieKind.Normal:
                    case ZombieKind.Flag:
                    case ZombieKind.Conehead:
                    case ZombieKind.Buckethead:
                    case ZombieKind.ScreenDoor:
                        zombie.ChillRemaining = 0f;
                        DamageZombie(z, JalapenoDamage);
                        break;
                }
            }

            Plants.RemoveAt(index);
            EmitLaneFlame(plant.Row);
            Raise(GameEvent.JalapenoExplosion);
            return true;
        }
    }
}
