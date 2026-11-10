using System;
using System.Collections.Generic;
using PvZReplica.Adventure;
namespace PvZReplica.Adventure
{
    public sealed partial class AdventureBoard
    {
        public int RakeRow { get; internal set; } = -1;
        public int RakeColumn { get; internal set; } = 7;
        public bool RakeAttracting { get; internal set; }
        public bool RakeTriggered { get; internal set; }
        public float RakeAge { get; internal set; }
        internal bool RakeDamageDone;
    }
}
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        // Board.PlaceRake / GridItem.UpdateRake: column 7, 20 fps, .8 hit event, 200-tick lifetime.
        private void InitializeAdventurePurchases()
        {
            if(Rules.AdventurePlaceRake==null||Adventure.Level.Mode==AdventureMode.Boss||Adventure.Level.Mode==AdventureMode.WhackAZombie)return;
            var rows=new List<int>();for(int r=0;r<Math.Min(5,RowCount);r++)if(Adventure.Level.IsLawnRow(r)&&!Adventure.Level.IsWaterRow(r))rows.Add(r);
            if(rows.Count==0)return;int row=rows[random.Next(rows.Count)];
            int column=7;if(Adventure.Level.Mode==AdventureMode.Vasebreaker)foreach(var vase in Adventure.vases)if(vase.Column>0&&vase.Column<=column)column=vase.Column-1;
            if(!Rules.AdventurePlaceRake())return;Adventure.RakeRow=row;Adventure.RakeColumn=column;Adventure.RakeAttracting=true;
        }
        private void AdvanceAdventurePurchases(float dt)
        {
            var a=Adventure;if(a.RakeRow<0)return;
            Zombie target=null;foreach(var z in Zombies)if(z.Row==a.RakeRow&&!z.Airborne&&!z.Submerged&&!z.Hypnotized&&z.Kind!=ZombieKind.Bungee&&z.Kind!=ZombieKind.Bobsled&&z.X-0.3f<=a.RakeColumn+63f/80f&&z.X+0.3f>=a.RakeColumn){target=z;break;}
            if(!a.RakeTriggered){if(target!=null){a.RakeTriggered=true;a.RakeAge=0f;}return;}
            a.RakeAge+=dt;
            if(!a.RakeDamageDone&&a.RakeAge>=.32f){a.RakeDamageDone=true;if(target!=null)HitAdventureZombie(target,1800f);}
            if(a.RakeAge>=2f)a.RakeRow=-1;
        }
        private bool IsAdventureFirstAid(PlantKind kind,int row,int column)
        {
            if(!Rules.AdventureFirstAid||kind!=PlantKind.Wallnut&&kind!=PlantKind.TallNut&&kind!=PlantKind.Pumpkin)return false;
            Plant old=AdventurePlantAt(row,column,kind==PlantKind.Pumpkin?PlantLayer.Armor:PlantLayer.Main);
            return old!=null&&old.Kind==kind&&old.Health<(kind==PlantKind.TallNut?8000f:4000f)*2f/3f;
        }
    }
}
