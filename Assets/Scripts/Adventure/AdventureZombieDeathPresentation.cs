using System;
namespace PvZReplica.Adventure
{
    public sealed partial class AdventureZombieCorpse
    {
        public float AnimationDuration,FadeDuration;
        public bool InPool,FallSoundPlayed;
        public float Alpha(float renderRemainder=0f)
        {
            if(Burn!=null)return 1f;
            float fadeAge=Age+renderRemainder-AnimationDuration;
            if(fadeAge<=0f)return 1f;
            int remaining=Math.Max(0,(int)Math.Round(FadeDuration*100f)-(int)Math.Floor(fadeAge*100f+0.0001f));
            // PC 0x548D40 uses min(255,255*fade/10), NOT fade/100 or mobile fade/30.
            return Math.Min(255,255*remaining/10)/255f;
        }
    }
    public static class AdventureZombieDeathRules
    {
        public static float AnimationSeconds(int frames,float rate)
        {return rate<=0f?0f:(float)(Math.Ceiling(frames*100d/rate)*.01d);}
        public static float FadeSeconds(bool pool){return pool?.1f:1f;}
        public static float FallPhase(int kind,string clip)
        {
            if(kind==11||kind==12||kind==14||kind==20||kind==22||kind==24||kind==25)return -1f;
            switch(kind){case 3:return .68f;case 7:return .52f;case 5:return .63f;case 8:case 9:return .76f;
                case 13:return .81f;case 15:return .64f;case 16:return .68f;case 17:return .85f;
                case 18:return .84f;case 19:return .68f;case 21:return .62f;case 23:case 32:return .86f;}
            return clip=="anim_superlongdeath"?.788f:clip=="anim_death2"?.71f:.77f;
        }
    }
}
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private PvZReplica.Adventure.AdventureZombieCorpse InitializeAdventureZombieCorpse(PvZReplica.Adventure.AdventureZombieCorpse corpse)
        {
            int frames;string asset=AdventureZombiePresentationAsset(corpse.Zombie);
            if(!PvZReplica.Adventure.AdventureZombieDeathData.TryGet(asset,corpse.Animation,out frames))
            {
                corpse.Animation="anim_death";
                if(!PvZReplica.Adventure.AdventureZombieDeathData.TryGet(asset,corpse.Animation,out frames)){corpse.Duration=0f;return corpse;}
            }
            corpse.AnimationDuration=PvZReplica.Adventure.AdventureZombieDeathRules.AnimationSeconds(frames,corpse.Rate);
            corpse.InPool=corpse.Zombie.PresentationInPool??(Adventure.Level.IsWaterRow(corpse.Zombie.Row)&&corpse.Zombie.X>=0&&corpse.Zombie.X<9);
            corpse.FadeDuration=PvZReplica.Adventure.AdventureZombieDeathRules.FadeSeconds(corpse.InPool);
            corpse.Duration=corpse.AnimationDuration+corpse.FadeDuration;
            return corpse;
        }
        private void AdvanceAdventureZombieCorpseSound(PvZReplica.Adventure.AdventureZombieCorpse corpse)
        {
            if(corpse.Burn!=null||corpse.FallSoundPlayed||corpse.InPool)return;
            int kind=corpse.Zombie.ModeHead>=26?corpse.Zombie.ModeHead:(int)corpse.Zombie.Kind;
            float phase=PvZReplica.Adventure.AdventureZombieDeathRules.FallPhase(kind,corpse.Animation);
            if(phase<0||corpse.Age<corpse.AnimationDuration*phase)return;
            corpse.FallSoundPlayed=true;RequestAdventureZombieFoley(28);
            if(kind==23||kind==32)RequestAdventureZombieFoley(40);
        }
    }
}
