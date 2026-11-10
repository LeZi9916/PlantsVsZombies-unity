using System;
using System.Collections.Generic;
using PvZReplica.Adventure;
using PvZReplica.VerifiedData;
namespace PvZReplica.Modes
{
    /// <summary>PC 0x430B50 / 0x4309A0 eligibility, 0x4150C0 budget, 0x429660 stage length.</summary>
    internal static class ModeWavePlan
    {
        internal static IReadOnlyList<AdventureWave> Build(ModeSession session,Random random)
        {
            ModeDefinition mode=session.Definition;int stage=session.Stage-1;
            var allowed=new List<int>();
            if(mode.Survival)
            {
                allowed.Add(0);allowed.Add(2);if(mode.Hard)allowed.Add(4);
                if(stage>0)
                {
                    allowed.Clear();allowed.Add(0);allowed.Add(random.Next(5)==0?5:2);
                    var choices=new List<int>();
                    for(int z=2;z<=24;z++)
                    {
                        if(z==9||z==10||z==13||z==19||z==24||allowed.Contains(z))continue;
                        if((z==11||z==14)&&!session.Game.Adventure.Level.HasPool)continue;
                        if(session.Game.Adventure.Level.HasRoof&&(z==17||z==8))continue;
                        if(session.Game.Adventure.Level.Terrain==AdventureTerrain.Night&&z==12)continue;
                        if(!session.Game.Adventure.Level.HasRoof&&!mode.Endless&&z==20)continue;
                        if(session.FlagsCompleted<4&&(z==23||z==12))continue;
                        if(mode.Id<=3&&z>11)continue;
                        choices.Add(z);
                    }
                    for(int i=0;i<Math.Min(stage+1,9)&&choices.Count>0;i++){int k=random.Next(choices.Count);allowed.Add(choices[k]);choices.RemoveAt(k);}
                    if(mode.Endless&&session.FlagsCompleted>=10)allowed.Add(32);
                }
            }
            else
            {
                int[] ids;
                switch(mode.Id)
                {
                    case 16:ids=new[]{26,27};break;case 32:ids=new[]{26,27,28,29,30,31};break;
                    case 17:ids=new[]{0,2,4,3,5};break;case 33:ids=new[]{0,2,4,3,5,8,6};break;
                    case 26:ids=new[]{0,4,7,16};break;case 28:ids=new[]{12,13};break;
                    case 29:ids=new[]{0,2,14,3};break;case 34:ids=new[]{18};break;
                    case 20:case 24:ids=new[]{0,2,4,6,7};break;
                    case 19:ids=new[]{0,2,4,6,7,5,15,20};break;
                    case 21:ids=new[]{0,2,4,14,12,15};break;
                    case 31:ids=new[]{0,2,4,6,7,5,15,3,14,21};break;
                    case 27:ids=new[]{0,2,4,7};break;
                    default:ids=new[]{0,2,4};break;
                }
                allowed.AddRange(ids);
            }
            var waves=new List<AdventureWave>();
            for(int w=0;w<mode.WaveCount;w++)
            {
                bool flag=w%10==9;int budget=mode.Id==31?(2*(w+stage*10)+20)/5+1:mode.Survival&&stage>0?(w+stage*mode.WaveCount)/5+1:w/3+1;
                var zombies=new List<ZombieKind>();
                if(flag){int count=Math.Min(budget,8);budget=(int)(budget*2.5f);if(mode.Id!=16&&mode.Id!=32){for(int i=0;i<count;i++){zombies.Add(ZombieKind.Normal);budget-=1;}zombies.Add(ZombieKind.Flag);budget-=1;}}
                if(mode.Mechanic==ModeMechanic.Bowling||mode.Mechanic==ModeMechanic.LittleZombies)budget*=4;
                if(mode.Id==21||mode.Id==26)budget*=2;if(mode.Id==27)budget*=6;
                while(budget>0&&zombies.Count<50)
                {
                    int total=0;var choices=new List<int>();
                    foreach(int id in allowed)
                    {
                        ZombieDefinition d=VerifiedDataCatalog.Zombies[id];
                        if(d.BaseWaveGateRaw>w||d.WaveBudgetCostRaw>budget||d.BaseSelectionWeightRaw<=0)continue;
                        choices.Add(id);total+=d.BaseSelectionWeightRaw;
                    }
                    if(total==0){zombies.Add((ZombieKind)allowed[0]);break;}
                    int ticket=random.Next(total),chosen=choices[0];
                    foreach(int id in choices){ticket-=VerifiedDataCatalog.Zombies[id].BaseSelectionWeightRaw;if(ticket<0){chosen=id;break;}}
                    zombies.Add((ZombieKind)chosen);budget-=Math.Max(1,VerifiedDataCatalog.Zombies[chosen].WaveBudgetCostRaw);
                }
                if(zombies.Count==0)zombies.Add((ZombieKind)allowed[0]);
                waves.Add(new AdventureWave{Index=w,IsFlag=flag,Zombies=zombies.AsReadOnly()});
            }
            return waves.AsReadOnly();
        }
    }
}
