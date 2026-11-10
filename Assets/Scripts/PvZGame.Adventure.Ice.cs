using UnityEngine;
namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private void DrawAdventureSnowTrails(int row)
        {
            foreach(var trail in game.Presentation.SnowTrails)
            {
                if(trail.Row!=row)continue;
                float fraction=Mathf.Clamp01(AdventureRenderRemainder*100f);
                Vector2 origin=trail.Stopped?new Vector2(trail.X,trail.Y):
                    new Vector2(Mathf.Lerp(trail.PreviousX,trail.X,fraction),Mathf.Lerp(trail.PreviousY,trail.Y,fraction));
                adventureParticles.Draw("snow-trail-"+trail.Id,"SnowPeaTrail",trail.Age+AdventureRenderRemainder,
                    origin,trail.Seed,1f,trail.StopAge);
            }
        }
    }
}
