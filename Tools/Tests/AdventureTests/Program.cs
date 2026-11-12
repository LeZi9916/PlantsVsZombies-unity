#nullable enable
using System;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Profiles;

internal static class Program
{
    /// <summary>Finds the repository independently of simulation source layout.</summary>
    /// <returns>The absolute repository directory.</returns>
    /// <exception cref="DirectoryNotFoundException">The repository cannot be found.</exception>
    private static string Workspace()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "ProjectSettings", "ProjectVersion.txt"))
                    && File.Exists(Path.Combine(directory.FullName, ".editorconfig")))
                {
                    return directory.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Project workspace not found.");
    }
    private static void Require(bool condition,string reason) {if(!condition)throw new Exception(reason);}
    public static void Main(string[] args)
    {
        string root=Workspace();Directory.SetCurrentDirectory(root);
        var outputDirectory = Path.Combine(root, "Tools", "Tests", "AdventureTests", "artifacts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        if (args.Length > 0 && args[0] == "--reward-projectiles")
        {
            RewardProjectileRegressionTests.Run();
            return;
        }

        if (args.Length > 0 && args[0] == "--combat-fidelity")
        {
            CombatFidelityRegressionTests.Run();
            return;
        }

        if (args.Length > 0 && args[0] == "--puff-shroom")
        {
            PuffShroomRegressionTests.Run();
            return;
        }

        if (args.Length > 0 && args[0] == "--fume-shroom")
        {
            FumeShroomRegressionTests.Run();
            return;
        }

        if (args.Length > 0 && args[0] == "--multi-head")
        {
            MultiHeadTimingRegressionTests.Run();
            return;
        }

        if (args.Length > 0 && args[0] == "--starfruit")
        {
            StarfruitRegressionTests.Run();
            return;
        }

        if (args.Length > 0 && args[0] == "--coin-feedback")
        {
            CoinFeedbackRegressionTests.Run();
            return;
        }

        if (args.Length == 0 || args[0] == "--unit")
        {
            AdventureUnitTests.Run(root);
            UpgradePlantTests.Run(root);
            CombatFidelityRegressionTests.Run();
            RewardProjectileRegressionTests.Run();
            PuffShroomRegressionTests.Run();
            FumeShroomRegressionTests.Run();
            MultiHeadTimingRegressionTests.Run();
            StarfruitRegressionTests.Run();
            CoinFeedbackRegressionTests.Run();
            if (args.Length > 0)
            {
                return;
            }
        }
        int single=args.Length>=2 && args[0]=="--level"?int.Parse(args[1]):0;
        bool campaignMode=single==0 && (args.Length==0 || args[0]!="--all-levels");
        string header="level,mode,result,seconds,waves,spawned,plants,packets,tools,zombies_left,boss_health,seed,reward_committed,checkpoint_reloaded";
        var rows=new List<string>{header};
        AdventureProgressStore store=null;AdventureCampaign campaign=null;string catalogPath=null,savePath=null;byte[] catalogBytes=null;
        if(campaignMode)
        {
            string dir=Path.Combine(outputDirectory,"campaign");Directory.CreateDirectory(dir);
            catalogPath=Path.Combine(dir,"profiles.dat");savePath=Path.Combine(dir,"campaign.adventure");
            var catalog=new ProfileCatalog(catalogPath);var profile=catalog.Create("连续冒险回归");catalogBytes=File.ReadAllBytes(catalogPath);
            store=new AdventureProgressStore(savePath);store.SaveAttempt(profile.Id,104729,new PlantKind[0]);
            campaign=new AdventureCampaign(store,profile.Id);campaign.BeginOrResume();
        }
        int won=0;
        for(int n=single==0?1:single;n<=(single==0?50:single);n++)
        {
            int seed=args.Length>=3?int.Parse(args[2]):n*104729;
            var g=campaignMode?campaign.Game:new GameSimulation(new GameRules{AdventureLevel=n},seed);
            Require(g.Adventure.Level.Number==n,"Continue opened wrong level.");
            var bot=new PlaythroughBot(g,campaignMode?campaign:null);var trace=new List<string>();
            for(int ticks=0;ticks<30000 && g.State==MatchState.Playing;ticks++)
            {
                bot.Act();g.Update(0.1f);
                if(single>0 && ticks%100==0)trace.Add(g.Elapsed.ToString("F1")+" sun="+g.SunAmount+" wave="+g.Adventure.CurrentWave+" plants="+
                    string.Join(";",g.Plants.Select(p=>p.Kind+"("+p.Row+","+p.Column+")"))+" zombies="+string.Join(";",g.Zombies.Select(z=>z.Kind+"("+z.Row+","+z.X.ToString("F1")+")")));
            }
            if(single>0)File.WriteAllLines(Path.Combine(outputDirectory,"trace-"+single+".txt"),trace);
            bool reward=false,reloaded=false;
            if(g.State==MatchState.Won)
            {
                won++;
                if(campaignMode)
                {
                    Require(store.Get(campaign.ProfileId).NextLevel==n,"Match win advanced progress without reward pickup.");
                    Require(campaign.CollectReward(),"Reward pickup failed.");
                    Require(!campaign.CollectReward(),"Duplicate pickup succeeded.");reward=true;
                    var checkpoint=new AdventureProgressStore(savePath).Get(campaign.ProfileId);
                    Require(checkpoint.NextLevel==Math.Min(50,n+1) && checkpoint.CollectedRewards==((1UL<<n)-1),"Checkpoint did not reload the collected rewards.");reloaded=true;
                    Require(File.ReadAllBytes(catalogPath).SequenceEqual(catalogBytes),"Campaign changed old v1 user catalog.");
                    if(n<50)
                    {store.SaveAttempt(campaign.ProfileId,(n+1)*104729,new PlantKind[0]);Require(campaign.StartNextLevel(),"Next level refused after reward.");}
                    else Require(campaign.Progress.Completed && !campaign.StartNextLevel(),"Fiftieth reward did not finish first adventure.");
                }
            }
            string row=string.Join(",",new[]{g.Adventure.Level.Label,g.Adventure.Level.Mode.ToString(),g.State.ToString(),g.Elapsed.ToString("F1",CultureInfo.InvariantCulture),g.Adventure.CurrentWave.ToString(),g.SpawnedZombies.ToString(),bot.PlantActions.ToString(),bot.PacketActions.ToString(),bot.ToolActions.ToString(),g.Zombies.Count.ToString(),g.Adventure.BossHealth.ToString("F0",CultureInfo.InvariantCulture),seed.ToString(),reward.ToString(),reloaded.ToString()});
            rows.Add(row);Console.WriteLine(row);
            if(g.State!=MatchState.Won)
            {
                if(single>0) {foreach(var z in g.Zombies.Take(10))Console.WriteLine("LIVE "+z.Kind+" r"+z.Row+" x"+z.X+" hp"+z.Health+" armor"+z.ArmorHealth);Console.WriteLine("SUN "+g.SunAmount+" DECK "+string.Join("/",g.Adventure.SelectedSeeds));}
                if(campaignMode)break;
            }
        }
        string report=Path.Combine(outputDirectory,single==0?"playthrough-results.csv":"playthrough-"+single+".csv");File.WriteAllLines(report,rows);
        Console.WriteLine((campaignMode?"Continuous campaign, saved reward and reloaded checkpoint at every level: ":"Public-action independent playthrough wins: ")+won+"/"+(single==0?50:1));
        if(campaignMode)Console.WriteLine("Replica-only checkpoint evidence: "+savePath);
        if(won!=(single==0?50:1))Environment.ExitCode=1;
    }
}
