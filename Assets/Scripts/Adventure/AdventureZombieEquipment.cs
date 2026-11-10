using System;
namespace PvZReplica.Adventure
{
    public static class AdventureZombieEquipmentRules
    {
        // PC 0x54CD70 / 0x54C950: integer thirds; boundaries themselves stay in the healthier stage.
        public static int DamageIndex(float current, int maximum)
        { return maximum <= 0 ? 0 : current < maximum / 3 ? 2 : current < (2 * maximum) / 3 ? 1 : 0; }
        public static int HelmType(Zombie z)
        {
            if (z.ModeHead == 27) return 8; if (z.ModeHead == 31) return 9;
            switch (z.Kind) { case ZombieKind.Conehead:return 1;case ZombieKind.Buckethead:return 2;
                case ZombieKind.Football:return 3;case ZombieKind.Digger:return 4;case ZombieKind.Bobsled:return 7;default:return 0; }
        }
        public static string HelmTrack(int type)
        { switch(type){case 1:return "anim_cone";case 2:return "anim_bucket";case 3:return "zombie_football_helmet";
            case 4:return "Zombie_digger_hardhat";case 8:return "anim_face";case 9:return "anim_idle";default:return null;} }
        public static string HelmImage(int type,int stage)
        {
            if(stage==0)return null;
            switch(type){case 1:return stage==1?"IMAGE_REANIM_ZOMBIE_CONE2":"IMAGE_REANIM_ZOMBIE_CONE3";
                case 2:return stage==1?"IMAGE_REANIM_ZOMBIE_BUCKET2":"IMAGE_REANIM_ZOMBIE_BUCKET3";
                case 3:return stage==1?"IMAGE_REANIM_ZOMBIE_FOOTBALL_HELMET2":"IMAGE_REANIM_ZOMBIE_FOOTBALL_HELMET3";
                case 4:return stage==1?"IMAGE_REANIM_ZOMBIE_DIGGER_HARDHAT2":"IMAGE_REANIM_ZOMBIE_DIGGER_HARDHAT3";
                case 8:return stage==1?"IMAGE_REANIM_WALLNUT_CRACKED1":"IMAGE_REANIM_WALLNUT_CRACKED2";
                case 9:return stage==1?"IMAGE_REANIM_TALLNUT_CRACKED1":"IMAGE_REANIM_TALLNUT_CRACKED2";default:return null;}
        }
        public static string ShieldTrack(Zombie z)
        { return z.Kind==ZombieKind.ScreenDoor?"anim_screendoor":z.Kind==ZombieKind.Newspaper?"Zombie_paper_paper":z.Kind==ZombieKind.Ladder?"Zombie_ladder_1":null; }
        public static string ShieldImage(Zombie z,int stage)
        { if(stage==0)return null;
          if(z.Kind==ZombieKind.ScreenDoor)return stage==1?"IMAGE_REANIM_ZOMBIE_SCREENDOOR2":"IMAGE_REANIM_ZOMBIE_SCREENDOOR3";
          if(z.Kind==ZombieKind.Newspaper)return stage==1?"IMAGE_REANIM_ZOMBIE_PAPER_PAPER2":"IMAGE_REANIM_ZOMBIE_PAPER_PAPER3";
          if(z.Kind==ZombieKind.Ladder)return stage==1?"IMAGE_REANIM_ZOMBIE_LADDER_1_DAMAGE1":"IMAGE_REANIM_ZOMBIE_LADDER_1_DAMAGE2";
          return null; }
        public static int HelmParticle(int type)
        { return type==1?8:type==2?9:type==3?10:type==4?14:-1; }
        public static bool TrackVisible(Zombie z,string track)
        {
            int helm=HelmType(z); string h=HelmTrack(helm),shield=ShieldTrack(z);
            if(helm>0&&helm<8&&h!=null&&Same(track,h)&&z.ArmorHealth<=0)return false;
            if(shield!=null&&Same(track,shield)&&z.ShieldHealth<=0)return false;
            if((helm==1||helm==2||helm==3)&&z.ArmorHealth>0&&track.StartsWith("anim_hair",StringComparison.OrdinalIgnoreCase))return false;
            return true;
        }
        public static string Image(Zombie z,string asset,string body,string track)
        {
            int helm=HelmType(z);bool bodyTrack=Same(asset,body);
            bool child=(helm==8&&Same(asset,"Wallnut"))||(helm==9&&Same(asset,"Tallnut"));
            if((bodyTrack&&helm<8||child)&&z.ArmorHealth>0&&Same(track,HelmTrack(helm)))
                return HelmImage(helm,DamageIndex(z.ArmorHealth,(int)z.PresentationMaxArmor));
            return bodyTrack&&z.ShieldHealth>0&&Same(track,ShieldTrack(z))?
                ShieldImage(z,DamageIndex(z.ShieldHealth,(int)z.PresentationMaxShield)):null;
        }
        private static bool Same(string a,string b){return string.Equals(a,b,StringComparison.OrdinalIgnoreCase);}
    }
}
namespace PvZReplica
{
    public sealed partial class Zombie
    { public float PresentationMaxArmor,PresentationMaxShield,PresentationNewspaperGaspRemaining; internal float PresentationArmorBeforeHit,PresentationShieldBeforeHit; }
    public sealed partial class GameSimulation
    {
        private void EnsureAdventureZombieEquipment(Zombie z)
        {
            if(z.PresentationMaxArmor<=0&&z.ArmorHealth>0)z.PresentationMaxArmor=z.ArmorHealth;
            if(z.PresentationMaxShield<=0&&z.ShieldHealth>0)z.PresentationMaxShield=z.ShieldHealth;
        }
        private void AdventurePresentationZombieEquipmentDamaged(Zombie z,uint flags)
        {
            if((flags&16U)!=0||z.PresentationDeath==PvZReplica.Adventure.AdventureZombieDeathPresentation.Burned&&z.Health<=0)return;
            int kind=z.ModeHead>=26?z.ModeHead:(int)z.Kind;
            if(z.PresentationArmorBeforeHit>0&&z.ArmorHealth<=0)
            {
                int helm=PvZReplica.Adventure.AdventureZombieEquipmentRules.HelmType(z),id=PvZReplica.Adventure.AdventureZombieEquipmentRules.HelmParticle(helm);
                if(id>=0)AddAdventureZombiePart(z,kind,id,PvZReplica.Adventure.AdventureZombieEquipmentRules.HelmTrack(helm),null);
            }
            if(z.PresentationShieldBeforeHit>0&&z.ShieldHealth<=0)
            {
                int id=z.Kind==ZombieKind.ScreenDoor?12:z.Kind==ZombieKind.Newspaper?13:z.Kind==ZombieKind.Ladder?54:-1;
                if(id>=0)AddAdventureZombiePart(z,kind,id,PvZReplica.Adventure.AdventureZombieEquipmentRules.ShieldTrack(z),null);
                if(z.Kind==ZombieKind.Newspaper)
                {
                    // PC 0x54C7A0: 13 original gasp frames, 8fps, then phase 31.
                    z.PresentationNativePhase=30;z.PresentationNewspaperGaspRemaining=PvZReplica.Adventure.AdventureZombieDeathRules.AnimationSeconds(13,8f);z.UsedAbility=true;z.Eating=false;
                    z.FrozenRemaining=0f;z.PresentationButter=0f;
                    if((flags&1U)==0)RequestAdventureZombieFoley(67);
                }
            }
        }
    }
}
