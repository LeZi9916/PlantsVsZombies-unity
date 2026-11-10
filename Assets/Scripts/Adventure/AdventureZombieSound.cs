using System.Collections.Generic;
namespace PvZReplica.Adventure
{
    public sealed class AdventureZombieSoundEvent
    { public int Id,Foley,Tick; }
    public sealed partial class AdventurePresentationState
    { public readonly List<AdventureZombieSoundEvent> ZombieSounds=new List<AdventureZombieSoundEvent>(); }
    public static class AdventureZombieSoundRules
    {
        // PC Projectile::PlayImpactSound 0x47D140, prior to damage changing the helm.
        public static int[] ProjectileFoley(int projectile,int helm,bool fireSplash)
        {
            if(projectile==10)return new[]{32};
            if(projectile==6&&fireSplash)return new[]{38};
            var cues=new List<int>();bool splat=true;
            if(projectile==12){cues.Add(43);splat=false;}
            if(projectile==3||projectile==5){cues.Add(62);splat=false;}
            if(helm==2){cues.Add(46);splat=false;}
            else if(helm==1||helm==3||helm==4)cues.Add(58);
            if(splat)cues.Add(1);return cues.ToArray();
        }
    }
}
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private void RequestAdventureZombieFoley(int id)
        { Presentation.ZombieSounds.Add(new PvZReplica.Adventure.AdventureZombieSoundEvent { Id=++Presentation.EffectId,Foley=id,Tick=(int)System.Math.Round(Elapsed*100f) }); }
        private void RequestAdventureProjectileImpactSound(Pea p,Zombie z)
        {
            int type=p.Butter?12:p.Fire?6:p.SourceKind==PlantKind.KernelPult?10:p.ChillSplash?5:p.Splash?3:0;
            int helm=z!=null&&z.ArmorHealth>0?PvZReplica.Adventure.AdventureZombieEquipmentRules.HelmType(z):0;
            // PC fire splash is disabled against vehicles and door/ladder shields.
            bool fireSplash=z!=null&&z.Kind!=ZombieKind.Zomboni&&z.Kind!=ZombieKind.Catapult&&
                (z.ShieldHealth<=0||z.Kind!=ZombieKind.ScreenDoor&&z.Kind!=ZombieKind.Ladder);
            foreach(int id in PvZReplica.Adventure.AdventureZombieSoundRules.ProjectileFoley(type,helm,fireSplash))RequestAdventureZombieFoley(id);
        }
    }
}
