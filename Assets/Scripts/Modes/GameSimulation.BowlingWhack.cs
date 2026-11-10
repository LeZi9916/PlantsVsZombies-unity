using System;
using System.Collections.Generic;
using PvZReplica.Adventure;
using PvZReplica.Modes;
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private void TickModeBowling(float dt)
        {
            var s=ModeSession;
            foreach(var nut in Adventure.nuts.ToArray())
            {
                bool giant=s.GiantNuts.Contains(nut);
                if(AdvanceAdventureBowlingNative(nut,dt,giant)){Adventure.nuts.Remove(nut);s.GiantNuts.Remove(nut);}
            }
        }
        // PC 0x4312E0: twelve 2000-tick rounds, native chance arrays and 100..200 -> 30..60 spawn interval.
        private void TickModeWhack(float dt)
        {
            var a=Adventure;var s=ModeSession;
            if(a.FinalAmbush)return;
            a.WaveCountdown-=dt;
            if(a.CurrentWave>0&&a.WaveCountdown<=1f&&!s.WhackGravesRaised)
            {ModeWhackAddGraves(Math.Max(1,5-a.graves.Count));s.WhackGravesRaised=true;}
            if(a.WaveCountdown<=0)
            {a.CurrentWave++;a.WaveCountdown=20f;s.WhackGravesRaised=false;s.WhackSpawnTimer=a.CurrentWave==12?1f:0.01f;}
            if(a.WaveCountdown<3f)return;
            s.WhackSpawnTimer-=dt;if(s.WhackSpawnTimer>0)return;
            int chance=Math.Max(0,Math.Min(5,(a.CurrentWave-1)*6/12));int[] triple={0,0,0,0,10,13},pair={0,30,10,10,15,18},bucket={0,0,0,10,15,15},cone={0,0,30,30,30,30};
            int roll=random.Next(100),typeRoll=random.Next(100);bool final=a.CurrentWave==12;
            int count=final?20:roll<triple[chance]?3:roll<triple[chance]+pair[chance]?2:1;
            ZombieKind kind=typeRoll<bucket[chance]&&count<3?ZombieKind.Buckethead:typeRoll<bucket[chance]+cone[chance]?ZombieKind.Conehead:ZombieKind.Normal;
            var graves=new List<AdventureCellTimer>();foreach(var g in a.graves)if(!Plants.Exists(p=>p.Row==g.Row&&p.Column==g.Column&&p.Kind==PlantKind.GraveBuster))graves.Add(g);
            float t=Math.Max(0,Math.Min(1,(a.CurrentWave-1)/11f));float speedMax=final?2f:1f+2f*t*t;
            for(int i=0;i<count&&graves.Count>0;i++)
            {int index=random.Next(graves.Count);var g=graves[index];graves.RemoveAt(index);var z=SpawnAdventureZombie(final?(random.Next(2)==0?ZombieKind.Conehead:ZombieKind.Buckethead):kind,g.Row,g.Column+0.5f,a.CurrentWave-1);z.Speed=(0.5f+(float)random.NextDouble()*(speedMax-0.5f))*1.25f;z.DropRemaining=0.5f;}
            float min=1f-0.7f*t,max=2f-1.4f*t;s.WhackSpawnTimer=min+(float)random.NextDouble()*(max-min);
            if(final){a.WaveCountdown=0;a.FinalAmbush=true;}
        }
        private void ModeWhackAddGraves(int count)
        {
            var cells=new List<int>();for(int r=0;r<5;r++)for(int c=3;c<9;c++)if(!Adventure.graves.Exists(g=>g.Row==r&&g.Column==c))cells.Add(r*9+c);
            for(int i=0;i<count&&cells.Count>0;i++){int index=random.Next(cells.Count),cell=cells[index];cells.RemoveAt(index);var p=AdventurePlantAt(cell/9,cell%9,PlantLayer.Main);if(p!=null)RemoveAdventurePlantAndStack(p);Adventure.graves.Add(new AdventureCellTimer{Row=cell/9,Column=cell%9,Remaining=float.MaxValue});}
        }
    }
}
