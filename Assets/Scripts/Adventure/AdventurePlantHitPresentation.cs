using System;
using System.Collections.Generic;
namespace PvZReplica
{
    public sealed partial class Plant { public int PresentationEatenFlashTicks; }
}
namespace PvZReplica.Adventure
{
    public sealed class AdventurePlantBiteEffect
    { public int Id,ParticleId,Row,Seed;public float NativeX,NativeY,Age,OffsetX,OffsetY;public ZombieKind Kind; }
    public sealed partial class AdventurePresentationState
    { public readonly List<AdventurePlantBiteEffect> PlantBiteEffects=new List<AdventurePlantBiteEffect>(); }
    public static class AdventurePlantHitRules
    {
        public static int Brightness(int ticks,bool imitated)
        {return Math.Min(imitated?128:255,Math.Max(0,ticks)*3);}
        public static AdventureProducerGlowVisual Combined(Plant p)
        {
            var production=AdventureProducerGlow.Visual(p);int ticks=Math.Max(p.PresentationEatenFlashTicks,production.FlashTicks);
            return new AdventureProducerGlowVisual(ticks>0,ticks,(byte)Brightness(ticks,p.IsImitated));
        }
    }
}
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private float nativePlantHitTickRemainder;
        public void RecordAdventurePlantHit(Plant plant,int nativeTicks=25)
        {if(plant!=null)plant.PresentationEatenFlashTicks=Math.Max(plant.PresentationEatenFlashTicks,nativeTicks);}
        private void AdvanceAdventurePlantHitCounters(float dt)
        {
            nativePlantHitTickRemainder+=dt*100f;int ticks=(int)Math.Floor(nativePlantHitTickRemainder+.00001f);
            nativePlantHitTickRemainder-=ticks;
            foreach(var p in Plants)p.PresentationEatenFlashTicks=Math.Max(0,p.PresentationEatenFlashTicks-ticks);
            for(int i=Presentation.PlantBiteEffects.Count-1;i>=0;i--)
            {
                var e=Presentation.PlantBiteEffects[i];e.Age+=dt;
                if(e.Age>=2f)Presentation.PlantBiteEffects.RemoveAt(i);
            }
        }
        private void RecordAdventureChewEffect(Zombie z,Plant p)
        {
            if(p==null)return;RecordAdventurePlantHit(p);
            if(p.Kind!=PlantKind.Wallnut&&p.Kind!=PlantKind.TallNut)return;
            float x=37f,y=40f;
            if(z.Kind==ZombieKind.Snorkel||z.Kind==ZombieKind.DolphinRider){x-=7f;y+=70f;}
            else if(z.PresentationWalkingBackwards??(z.Hypnotized||z.Kind==ZombieKind.Digger&&z.AbilityPhase==2))x+=47f;
            else if(z.Kind==ZombieKind.Balloon)y+=47f;
            else if(z.Kind==ZombieKind.Imp){x+=24f;y+=40f;}
            Presentation.PlantBiteEffects.Add(new PvZReplica.Adventure.AdventurePlantBiteEffect {
                Id=++Presentation.EffectId,ParticleId=35,Row=z.Row,Kind=z.Kind,
                NativeX=PvZReplica.Adventure.AdventureZombieNativeRules.NativePositionX(z.X,z.IsSmall?.5f:1f),
                NativeY=AdventureBiteNativeY(z,PvZReplica.Adventure.AdventureZombieNativeRules.NativePositionX(z.X,z.IsSmall?.5f:1f)),OffsetX=x,OffsetY=y+z.PresentationBiteBodyY,Seed=z.Id*397^Presentation.EffectId });
        }
    }
}
