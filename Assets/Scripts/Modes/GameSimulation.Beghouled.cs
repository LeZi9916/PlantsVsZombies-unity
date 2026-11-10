using System;
using System.Collections.Generic;
using PvZReplica.Adventure;
using PvZReplica.Modes;
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        private PlantKind[] ModeMatchKinds()
        {return new[]{ModeSession.MatchUpgrades[1]?PlantKind.FumeShroom:PlantKind.PuffShroom,PlantKind.Starfruit,PlantKind.MagnetShroom,PlantKind.SnowPea,ModeSession.MatchUpgrades[2]?PlantKind.TallNut:PlantKind.Wallnut,ModeSession.MatchUpgrades[0]?PlantKind.Repeater:PlantKind.Peashooter};}
        private void InitializeModeMatch()
        {
            Adventure.graves.Clear();Plants.Clear();
            for(int r=0;r<5;r++)for(int c=0;c<8;c++)PlaceAdventurePlant(ModePickMatchSeed(r,c,false),r,c);
            ModeSession.MatchDelay=0.2f;
        }
        private PlantKind ModePickMatchSeed(int row,int col,bool allowMatches)
        {
            var allowed=new List<PlantKind>();
            foreach(var kind in ModeMatchKinds())
            {
                if(!allowMatches&&(col>=2&&ModeMatchKindAt(row,col-1)==kind&&ModeMatchKindAt(row,col-2)==kind||row>=2&&ModeMatchKindAt(row-1,col)==kind&&ModeMatchKindAt(row-2,col)==kind))continue;
                allowed.Add(kind);
            }
            return allowed[random.Next(allowed.Count)];
        }
        private PlantKind? ModeMatchKindAt(int row,int col)
        {var plant=AdventurePlantAt(row,col,PlantLayer.Main);return plant==null?(PlantKind?)null:plant.Kind;}
        private HashSet<Plant> ModeFindMatches(out int groups)
        {
            groups=0;var found=new HashSet<Plant>();
            for(int r=0;r<5;r++)for(int c=0;c<8;)
            {
                var p=AdventurePlantAt(r,c,PlantLayer.Main);int end=c+1;
                if(p!=null)while(end<8&&ModeMatchKindAt(r,end)==p.Kind)end++;
                if(p!=null&&end-c>=3){groups++;for(int x=c;x<end;x++)found.Add(AdventurePlantAt(r,x,PlantLayer.Main));}c=end;
            }
            for(int c=0;c<8;c++)for(int r=0;r<5;)
            {
                var p=AdventurePlantAt(r,c,PlantLayer.Main);int end=r+1;
                if(p!=null)while(end<5&&ModeMatchKindAt(end,c)==p.Kind)end++;
                if(p!=null&&end-r>=3){groups++;for(int y=r;y<end;y++)found.Add(AdventurePlantAt(y,c,PlantLayer.Main));}r=end;
            }
            return found;
        }
        public bool ClickModeMatchCell(int row,int column)
        {
            var s=ModeSession;if(s==null||!IsModeMatch||s.Phase!=ModePhase.Playing||State!=MatchState.Playing||row<0||row>=5||column<0||column>=8||s.MatchDelay>0)return false;
            var a=AdventurePlantAt(row,column,PlantLayer.Main);if(a==null)return false;
            if(s.Definition.Mechanic==ModeMechanic.BeghouledTwist)
            {
                if(row>=4||column>=7)return false;
                var b=AdventurePlantAt(row,column+1,PlantLayer.Main);var c=AdventurePlantAt(row+1,column+1,PlantLayer.Main);var d=AdventurePlantAt(row+1,column,PlantLayer.Main);
                if(b==null||c==null||d==null)return false;
                a.Column++;b.Row++;c.Column--;d.Row--;s.SelectedCell=-1;ResolveModeMatch();s.MatchDelay=0.25f;return true;
            }
            int cell=row*9+column;
            if(s.SelectedCell<0){s.SelectedCell=cell;return true;}
            int old=s.SelectedCell;s.SelectedCell=-1;var other=AdventurePlantAt(old/9,old%9,PlantLayer.Main);
            if(other==null||Math.Abs(other.Row-row)+Math.Abs(other.Column-column)!=1){s.SelectedCell=cell;return true;}
            int r=other.Row,cx=other.Column;other.Row=row;other.Column=column;a.Row=r;a.Column=cx;
            int groups;ModeFindMatches(out groups);
            if(groups==0){a.Row=row;a.Column=column;other.Row=r;other.Column=cx;s.Advice="交换需要形成横向或纵向三个相同植物。";return false;}
            ResolveModeMatch();s.MatchDelay=0.25f;return true;
        }
        private bool ResolveModeMatch()
        {
            int groups;var matched=ModeFindMatches(out groups);if(groups==0)return false;
            ModeSession.Score+=groups;ModeSession.SelectedCell=-1;
            foreach(var p in matched)Plants.Remove(p);
            // PC score function 0x42C7D0 / .NET BeghouledScore: a group awards 1..5 sun objects.
            int suns=Math.Max(groups,Math.Min(groups*5,matched.Count-groups*2));
            for(int i=0;i<suns;i++)Suns.Add(new Sun{X=2.5f+(float)random.NextDouble()*3f,Y=1f,TargetY=1.5f+(float)random.NextDouble()*2f});
            for(int c=0;c<8;c++)for(int r=4;r>=0;r--)
            {
                if(AdventurePlantAt(r,c,PlantLayer.Main)!=null||Adventure.craters.Exists(x=>x.Row==r&&x.Column==c))continue;
                Plant falling=null;for(int above=r-1;above>=0;above--){if(Adventure.craters.Exists(x=>x.Row==above&&x.Column==c))break;falling=AdventurePlantAt(above,c,PlantLayer.Main);if(falling!=null)break;}
                if(falling!=null)falling.Row=r;else PlaceAdventurePlant(ModePickMatchSeed(r,c,true),r,c);
            }
            return true;
        }
        private void TickModeMatch(float dt)
        {
            var s=ModeSession;s.MatchDelay=Math.Max(0,s.MatchDelay-dt);
            if(s.MatchDelay<=0&&ResolveModeMatch())s.MatchDelay=0.25f;
            if(s.MatchDelay<=0)for(int r=0;r<5;r++)for(int c=0;c<8;c++)if(AdventurePlantAt(r,c,PlantLayer.Main)==null&&!Adventure.craters.Exists(x=>x.Row==r&&x.Column==c))Adventure.craters.Add(new AdventureCellTimer{Row=r,Column=c,Remaining=float.MaxValue});
        }
        /// <param name="action">0 repeater, 1 fume-shroom, 2 tall-nut, 3 shuffle, 4 fill a crater.</param>
        public bool BuyModeMatchAction(int action)
        {
            var s=ModeSession;if(s==null||!IsModeMatch||s.Phase!=ModePhase.Playing||State!=MatchState.Playing||action<0||action>4||s.MatchDelay>0)return false;
            int[] costs={1000,500,250,100,200};if(SunAmount<costs[action]||action<3&&s.MatchUpgrades[action])return false;
            if(action==4&&Adventure.craters.Count==0)return false;SunAmount-=costs[action];
            if(action<3)
            {
                s.MatchUpgrades[action]=true;PlantKind[] from={PlantKind.Peashooter,PlantKind.PuffShroom,PlantKind.Wallnut};PlantKind[] to={PlantKind.Repeater,PlantKind.FumeShroom,PlantKind.TallNut};
                foreach(var p in Plants.ToArray())if(p.Kind==from[action]){Plants.Remove(p);PlaceAdventurePlant(to[action],p.Row,p.Column);}
            }
            else if(action==3)
            {
                foreach(var p in Plants.ToArray())if(p.Column<8)Plants.Remove(p);
                for(int r=0;r<5;r++)for(int c=0;c<8;c++)if(!Adventure.craters.Exists(x=>x.Row==r&&x.Column==c))PlaceAdventurePlant(ModePickMatchSeed(r,c,false),r,c);
            }
            else{var crater=Adventure.craters[0];Adventure.craters.RemoveAt(0);PlaceAdventurePlant(ModePickMatchSeed(crater.Row,crater.Column,true),crater.Row,crater.Column);}
            s.MatchDelay=0.25f;return true;
        }
    }
}
