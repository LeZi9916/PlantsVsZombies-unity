using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PvZReplica.Adventure;
using PvZReplica.Audio;
using PvZReplica.Modes;
using PvZReplica.Progression;
namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private ModeSession modeSession;
        private Guid modeProfileId;
        private bool modeFirstClear,modeRewardClaimed,modeChoosingImitater;
        private int modeSavedStage=-1;
        private string modeSaveError;
        private PlayerAccount modeAccountSnapshot;
        private bool modeHasFinishedAdventure;
        private int modeAdventureNextLevel=1;
        private float modeAccountRefresh, modeNextSaveAttempt;
        public Action<int> ExternalChallengeRequested;
        private MusicCue? IndependentModeMusicCue
        {
            get
            {
                if(!IsModeBoard)return null;if(game.State==MatchState.Won||game.State==MatchState.Lost)return MusicCue.None;
                if(choosingSeeds)return MusicCue.ChooseSeeds;var m=modeSession.Definition.Mechanic;
                if(modeSession.Definition.Puzzle)return MusicCue.Puzzle;if(m==ModeMechanic.Boss)return MusicCue.Boss;if(m==ModeMechanic.Columns)return MusicCue.Conveyor;
                if(m==ModeMechanic.Bowling||m==ModeMechanic.Whack||m==ModeMechanic.LittleZombies||m==ModeMechanic.Speed)return MusicCue.Minigame;
                var level=game.Adventure.Level;return level.Terrain==AdventureTerrain.Fog?MusicCue.Fog:level.IsNight?MusicCue.Night:level.HasPool?MusicCue.Pool:level.HasRoof?MusicCue.Roof:MusicCue.Day;
            }
        }
        private bool IsModeBoard { get { return modeSession!=null&&ReferenceEquals(game,modeSession.Game)&&currentView==ScreenView.Board; } }
        private static readonly Rect ModeBackButton=new Rect(620,570,174,26);
        private static readonly Rect ModeNextButton=new Rect(306,342,188,42);
        private static readonly Rect ModeRestartButton=new Rect(306,394,188,40);
        private static readonly Rect ModeExitButton=new Rect(306,444,188,40);
        private void InitializeModes()
        {
            modeView.LaunchRequested=StartIndependentMode;
            modeView.ProgressLookup=ReadModeRecord;
            modeView.LockReasonLookup=ReadModeLockReason;
            modeView.CanLaunch=id=>{ModeDefinition d;return ModeCatalog.TryGet(id,out d)?ReadModeLockReason(id)==null:ExternalChallengeRequested!=null&&(id==43||id==50);};
        }
        private int ReadModeRecord(int modeId)
        {
            if(profiles==null||!profiles.SelectedId.HasValue||!EnsurePlayerAccount())return 0;
            if(modeAccountSnapshot==null||modeAccountSnapshot.ProfileId!=profiles.SelectedId.Value||Time.unscaledTime>=modeAccountRefresh)
            {modeAccountSnapshot=GetSelectedAccount();if(EnsureAdventureStore()){var progress=adventureStore.Get(profiles.SelectedId.Value);modeHasFinishedAdventure=progress.CompletedRuns>0;modeAdventureNextLevel=progress.NextLevel;}modeAccountRefresh=Time.unscaledTime+1f;}
            return modeAccountSnapshot==null?0:modeAccountSnapshot.ModeRecord(modeId-1);
        }
        private string ReadModeLockReason(int originalModeId)
        {
            if(profiles==null||!profiles.SelectedId.HasValue)return "请先选择用户。";
            if(!EnsurePlayerAccount()||!EnsureAdventureStore())return "无法读取账户或冒险进度。";
            ReadModeRecord(originalModeId);
            bool giftAccess=originalModeId<=15?modeAccountSnapshot.SurvivalUnlocked:originalModeId<=35?modeAccountSnapshot.MinigamesUnlocked:modeAccountSnapshot.PuzzleUnlocked;
            if(!giftAccess&&!ModeUnlockRules.CategoryUnlocked(originalModeId,modeAdventureNextLevel,modeHasFinishedAdventure))return originalModeId<=15?"通关冒险模式后解锁生存模式。":originalModeId<=35?"通过冒险模式3-2，领取小游戏礼物后解锁。":"通过冒险模式4-6，领取解谜模式礼物后解锁。";
            return ModeUnlockRules.LockReason(originalModeId,modeHasFinishedAdventure,index=>modeAccountSnapshot.ModeRecord(index));
        }
        private bool StartIndependentMode(int originalModeId)
        {
            ModeDefinition definition;
            if(!ModeCatalog.TryGet(originalModeId,out definition)){if(ExternalChallengeRequested!=null){ExternalChallengeRequested(originalModeId);return true;}return false;}
            if(profiles==null||!profiles.SelectedId.HasValue){if(profileDialog!=null)profileDialog.Open();return false;}
            if(!EnsurePlayerAccount())return false;
            string locked=ReadModeLockReason(originalModeId);if(locked!=null){SetUnavailable("挑战尚未解锁",locked);return false;}
            try
            {
                var account=GetSelectedAccount();modeProfileId=profiles.SelectedId.Value;
                int record=account.ModeRecord(definition.RecordIndex);
                var progress=adventureStore.Get(modeProfileId);int basicSeeds=progress.CompletedRuns>0?40:Math.Min(40,AdventureLevelSpec.AwardSeedId(progress.NextLevel));
                modeFirstClear=definition.Survival?record<(definition.Hard?10:5):record==0;
                modeSession=new ModeSession(originalModeId,account.SeedAccess(),account.PacketSlots,unchecked(Environment.TickCount^(int)DateTime.UtcNow.Ticks),account.PurchaseCount(23)>0,account.PurchaseCount(22)>0,basicSeeds);
                Guid coinProfile=modeProfileId;string coinReceipt=modeSession.RunId.ToString("N");
                modeSession.Game.Rules.AdventurePacketSlots=account.PacketSlots;
                modeSession.Game.Rules.AdventureDisableLoot=definition.Mechanic==ModeMechanic.IZombie;
                modeSession.Game.Rules.AdventurePottedLoot=progress.CompletedRuns>0&&account.Plants.Count<32;
                modeSession.Game.Rules.AdventureChocolateLoot=account.PurchaseCount(20)>0&&account.PurchaseCount(26)<10;
                modeSession.Game.Rules.AdventureLootCommit=coin=>{try{accountStore.CollectBattleLoot(coinProfile,coinReceipt+":"+coin.Id,coin.Kind==AdventureCoinKind.Chocolate,coin.GiftPlant??PlantKind.Peashooter,UtcSeconds);return true;}catch(IOException e){modeSaveError=e.Message;return false;}catch(InvalidOperationException e){modeSaveError=e.Message;return false;}};
                modeSession.Game.Rules.AdventureCoinCommit=total=>
                {
                    try{accountStore.CreditBattleCoins(coinProfile,coinReceipt,total);modeAccountSnapshot=null;modeSaveError=null;return true;}
                    catch(IOException error){modeSaveError="金币尚未存入，请再次拾取："+error.Message;return false;}
                    catch(UnauthorizedAccessException error){modeSaveError="金币存档无法写入："+error.Message;return false;}
                };
                modeRewardClaimed=false;modeNextSaveAttempt=0;modeSavedStage=-1;modeSaveError=null;modeChoosingImitater=false;
                modeSession.Game.ApplyModePurchases(account.PurchaseCount(29)>0,account.PurchaseCount(24)>0?(Func<bool>)(()=>
                {
                    try{if(accountStore.Get(coinProfile).PurchaseCount(24)<=0)return false;accountStore.ConsumeRake(coinProfile);modeAccountSnapshot=null;return true;}
                    catch(IOException error){modeSaveError="草耙尚未扣除，本局未放置："+error.Message;return false;}
                    catch(UnauthorizedAccessException error){modeSaveError="草耙库存无法保存，本局未放置："+error.Message;return false;}
                }):null);
                AttachAdventureGame(modeSession.Game);deck.Clear();deck.AddRange(game.Adventure.SelectedSeeds);AdventureFeedback(modeSession.Advice,8f);
                choosingSeeds=modeSession.Phase==ModePhase.ChoosingSeeds;pendingSeeds=new HashSet<PlantKind>(game.Adventure.SelectedSeeds);
                currentView=ScreenView.Board;return true;
            }
            catch(Exception error){SetUnavailable("无法开始独立模式",error.Message);return false;}
        }
        private bool RestartModeIfActive()
        {if(!IsModeBoard)return false;if(modeSession.Phase==ModePhase.Won&&!modeRewardClaimed&&!SaveModeRecord(true))return true;StartIndependentMode(modeSession.Definition.Id);return true;}
        private bool TickModes(float dt)
        {
            if(!IsModeBoard)return false;
            modeSession.Update(dt);
            if((modeSession.Phase==ModePhase.StageCleared||modeSession.Phase==ModePhase.Lost)&&modeSavedStage!=modeSession.ClearedStages&&Time.unscaledTime>=modeNextSaveAttempt)SaveModeRecord(false);
            return true;
        }
        private bool SaveModeRecord(bool claimReward)
        {
            if(modeSession==null||accountStore==null)return false;
            try
            {
                if(claimReward&&!modeSession.Game.BankAdventureCoins())return false;
                var s=modeSession;int score=s.Definition.Survival?s.FlagsCompleted:s.Definition.Endless?s.ClearedStages:s.Phase==ModePhase.Won?1:0;
                if(claimReward&&s.Phase==ModePhase.Won&&!s.Definition.Endless)
                {
                    accountStore.RecordModeResult(modeProfileId,s.Definition.RecordIndex,score,s.RunId.ToString("N"),modeFirstClear?100:25);modeRewardClaimed=true;
                }
                else accountStore.RecordModeResult(modeProfileId,s.Definition.RecordIndex,score,s.RunId.ToString("N")+":score:"+s.ClearedStages,0);
                modeSavedStage=s.ClearedStages;modeSaveError=null;modeAccountSnapshot=null;return true;
            }
            catch(Exception error){modeSaveError="成绩/奖励保存失败："+error.Message;modeNextSaveAttempt=Time.unscaledTime+2f;return false;}
        }
        private void ReturnToModeDirectory()
        {
            if(modeSession!=null&&(modeSession.Finished||modeSession.HasNextStage))
                if(!SaveModeRecord(modeSession.Phase==ModePhase.Won&&!modeRewardClaimed))return;
            currentView=ScreenView.Modes;int id=modeSession==null?16:modeSession.Definition.Id;modeView.Open(id<=15?0:id>=51?3:1);
            selected=-1;selectedAdventurePacket=-1;shovel=false;choosingSeeds=false;
        }
        private bool TryHandleModeClick(Vector2 point)
        {
            if(!IsModeBoard)return false;var s=modeSession;
            if(ModeBackButton.Contains(point)){ReturnToModeDirectory();return true;}
            if(s.Phase==ModePhase.ChoosingSeeds){HandleModeChooser(point);return true;}
            if(s.HasNextStage||s.Finished)
            {
                if(ModeNextButton.Contains(point))
                {
                    if(s.HasNextStage)
                    {
                        if(SaveModeRecord(false)&&s.Continue()){animations.ResetNativePlayback();choosingSeeds=s.Phase==ModePhase.ChoosingSeeds;pendingSeeds=new HashSet<PlantKind>(game.Adventure.SelectedSeeds);deck.Clear();deck.AddRange(game.Adventure.SelectedSeeds);selected=-1;selectedAdventurePacket=-1;shovel=false;AdventureFeedback(s.Advice,6f);}
                    }
                    else if(s.Phase==ModePhase.Won){if(!modeRewardClaimed)SaveModeRecord(true);else ReturnToModeDirectory();}
                    else RestartModeIfActive();
                }
                else if(ModeRestartButton.Contains(point))RestartModeIfActive();
                else if(ModeExitButton.Contains(point))ReturnToModeDirectory();return true;
            }
            if(AdventureMenuButton.Contains(point)){OpenOptions();return true;}
            if(game.State==MatchState.Paused)return true;
            if(new Rect(690,86,100,32).Contains(point)){s.Advice=null;adventureFeedback=null;return true;}
            if(s.Phase==ModePhase.Preparing&&new Rect(295,535,210,34).Contains(point)){s.BeginDefense();return true;}
            for(int i=game.Adventure.Coins.Count-1;i>=0;i--)if(AdventureCoinRect(game.Adventure.Coins[i]).Contains(point)){game.CollectAdventureCoin(game.Adventure.Coins[i]);return true;}
            for(int i=game.Suns.Count-1;i>=0;i--)if(ModeSunRect(game.Suns[i]).Contains(point)){game.CollectSun(game.Suns[i]);return true;}
            var m=s.Definition.Mechanic;
            if(m==ModeMechanic.Aquarium)
            {
                if(new Rect(100,10,155,55).Contains(point))s.BuyFish();
                else if(new Rect(275,10,175,55).Contains(point))s.BuyAquariumTrophy();
                else s.Feed(point.x,point.y);return true;
            }
            if(m==ModeMechanic.Slots&&new Rect(500,8,95,64).Contains(point)){s.Spin();return true;}
            if(m==ModeMechanic.Beghouled||m==ModeMechanic.BeghouledTwist)
            {
                for(int i=0;i<5;i++)if(new Rect(80+i*98,8,92,60).Contains(point)){game.BuyModeMatchAction(i);return true;}
            }
            if(m==ModeMechanic.IZombie)
            {
                for(int i=0;i<s.ZombieCards.Count;i++)if(ModeZombieCardRect(i).Contains(point)){selected=selected==i?-1:i;return true;}
            }
            else if(s.Definition.UsesPackets)
            {
                if(m==ModeMechanic.RainingSeeds)
                {
                    for(int i=s.RainPackets.Count-1;i>=0;i--)if(ModeRainRect(s.RainPackets[i]).Contains(point)){selectedAdventurePacket=selectedAdventurePacket==s.RainPackets[i].PacketId?-1:s.RainPackets[i].PacketId;shovel=false;return true;}
                }
                else for(int i=0;i<game.Adventure.Packets.Count&&i<10;i++)if(AdventurePacketContains(i,point)){selectedAdventurePacket=selectedAdventurePacket==game.Adventure.Packets[i].Id?-1:game.Adventure.Packets[i].Id;selected=-1;shovel=false;game.CancelAdventureCobCannonAim();return true;}
                if(m==ModeMechanic.Vases&&new Rect(550,8,47,64).Contains(point)){selected=selected==0?-1:0;selectedAdventurePacket=-1;return true;}
            }
            else if(m!=ModeMechanic.Beghouled&&m!=ModeMechanic.BeghouledTwist)
            {
                for(int i=0;i<deck.Count;i++)if(ModeSeedRect(i).Contains(point)){selected=selected==i?-1:i;selectedAdventurePacket=-1;shovel=false;game.CancelAdventureCobCannonAim();return true;}
            }
            if(m!=ModeMechanic.IZombie&&m!=ModeMechanic.Beghouled&&m!=ModeMechanic.BeghouledTwist&&AdventureShovelButton.Contains(point)){shovel=!shovel;selected=-1;selectedAdventurePacket=-1;game.CancelAdventureCobCannonAim();return true;}
            int col=Mathf.FloorToInt((point.x-AdventureGridX)/CellWidth);int row=Mathf.FloorToInt((point.y-AdventureGridY-AdventureSlope(col))/AdventurePitch);
            if(col<0||col>8||row<0||row>=game.RowCount)return true;
            if(m==ModeMechanic.IZombie){if(selected>=0&&!s.PlaceZombie(s.ZombieCards[selected],row,col))AdventureFeedback("放在线的右侧，并准备足够阳光；蹦极僵尸要选左侧植物。",3f);return true;}
            if(m==ModeMechanic.Beghouled||m==ModeMechanic.BeghouledTwist){s.ClickMatchCell(row,col);return true;}
            if(game.Adventure.SelectedCobCannonId.HasValue){game.TryFireAdventureCobCannonAtCell(row,col);return true;}
            if(shovel){if(game.RemovePlant(row,col))shovel=false;return true;}
            if(selectedAdventurePacket>=0){if(s.UsePacket(selectedAdventurePacket,row,col))selectedAdventurePacket=-1;else AdventurePlantingFeedback();return true;}
            if(m==ModeMechanic.Vases&&selected==0){if(game.PlantModeVaseCherry(row,col))selected=-1;else AdventurePlantingFeedback();return true;}
            if(selected>=0&&selected<deck.Count){if(s.Plant(deck[selected],row,col))selected=-1;else AdventurePlantingFeedback();return true;}
            if(game.TryAimAdventureCobCannon(row,col))return true;
            if(m==ModeMechanic.Vases){s.SmashVase(row,col);return true;}
            if(m==ModeMechanic.Whack)for(int i=game.Zombies.Count-1;i>=0;i--){var z=game.Zombies[i];if(new Rect(AdventureGridX+z.X*CellWidth-48,AdventureCellY(z.Row,z.X)-35,96,140).Contains(point)){game.HammerAdventureZombie(z.Id);break;}}
            return true;
        }
        private void HandleModeChooser(Vector2 point)
        {
            var unlocked=game.Adventure.AvailableSeeds;int required=Math.Min(modeSession.PacketSlots,unlocked.Count);
            if(modeChoosingImitater)
            {
                for(int i=0;i<40;i++)if(AdventureChooserRect(i).Contains(point)&&game.SetModeImitaterTarget((PlantKind)i)){pendingSeeds.Add(PlantKind.Imitater);modeChoosingImitater=false;return;}
                if(new Rect(300,533,200,34).Contains(point))modeChoosingImitater=false;return;
            }
            for(int i=0;i<unlocked.Count;i++)if(AdventureChooserRect(i).Contains(point))
            {if(!pendingSeeds.Remove(unlocked[i])&&pendingSeeds.Count<required){if(unlocked[i]==PlantKind.Imitater)modeChoosingImitater=true;else pendingSeeds.Add(unlocked[i]);}return;}
            if(new Rect(300,533,200,34).Contains(point)&&pendingSeeds.Count==required)
            {var seeds=new List<PlantKind>();foreach(var k in unlocked)if(pendingSeeds.Contains(k))seeds.Add(k);if(modeSession.ChooseSeeds(seeds)){choosingSeeds=false;deck.Clear();deck.AddRange(seeds);}}
        }
        private static string ModeZombieName(ZombieKind kind)
        {
            string[] names={"普通僵尸","旗帜僵尸","路障僵尸","撑杆僵尸","铁桶僵尸","读报僵尸","铁门僵尸","橄榄球僵尸","舞王僵尸","伴舞僵尸","泳圈僵尸","潜水僵尸","冰车僵尸","雪橇僵尸","海豚僵尸","小丑僵尸","气球僵尸","矿工僵尸","蹦蹦僵尸","雪人僵尸","蹦极僵尸","扶梯僵尸","投石车僵尸","巨人僵尸","小鬼僵尸"};
            return (int)kind>=0&&(int)kind<names.Length?names[(int)kind]:kind.ToString();
        }
        private Rect ModeSeedRect(int index)
        {return new Rect(AdventureSeedSlotLayout.PositionX(index,deck.Count),8f,50f,70f);}
        private static Rect ModeZombieCardRect(int index){return new Rect(80+index*57,8,54,65);}
        private static Rect ModeRainRect(ModeRainPacket packet){return new Rect(packet.X-25,packet.Y-35,50,70);}
        private Rect ModeSunRect(Sun sun)
        {return AdventureSunRect(sun);}
        private bool DrawModesInterface()
        {
            if(!IsModeBoard)return false;var s=modeSession;var d=s.Definition;
            if(d.Mechanic==ModeMechanic.Aquarium)DrawModeAquarium();
            DrawModeBoardOverlays();
            if (game.UsesNativeAdventureConveyor) DrawAdventureConveyorBackground();
            else
            {
                Fill(new Rect(0,0,800,80),new Color(0.25f,0.19f,0.08f,0.95f));
                Label(new Rect(0,5,78,30),"阳光",17,Color.yellow,TextAnchor.MiddleCenter,true);
                Label(new Rect(0,35,78,32),game.SunAmount.ToString(),23,Color.white,TextAnchor.MiddleCenter,true);
            }
            if(d.Mechanic==ModeMechanic.Aquarium){ModeButton(new Rect(100,10,155,55),"潜水僵尸 100");ModeButton(new Rect(275,10,175,55),"奖杯 1000");}
            else if(d.Mechanic==ModeMechanic.Beghouled||d.Mechanic==ModeMechanic.BeghouledTwist)
            {string[] names={"双发 1000","大喷 500","高坚果 250","重排 100","填坑 200"};for(int i=0;i<5;i++)ModeButton(new Rect(80+i*98,8,92,60),names[i]);}
            else if(d.Mechanic==ModeMechanic.IZombie)
            {
                for(int i=0;i<s.ZombieCards.Count;i++)
                {var k=s.ZombieCards[i];Rect card=ModeZombieCardRect(i);DrawNativeModeZombieCard(k,card);if(selected==i)Outline(card,Color.yellow,3);}
            }
            else if(d.UsesPackets)
            {
                if(d.Mechanic!=ModeMechanic.RainingSeeds) DrawAdventureConveyorPackets(true);
                if(d.Mechanic==ModeMechanic.Vases){Rect card=new Rect(550,8,47,64);DrawAdventureCard(PlantKind.CherryBomb,card,true);if(selected==0)Outline(card,Color.yellow,3);}
            }
            else for(int i=0;i<deck.Count;i++){Rect card=ModeSeedRect(i);DrawAdventurePacket(deck[i],card);}
            if(d.Mechanic==ModeMechanic.Slots)ModeButton(new Rect(500,8,95,64),s.SlotRollingRemaining>0?"转动中":"拉动 25");
            if(d.Mechanic!=ModeMechanic.IZombie&&d.Mechanic!=ModeMechanic.Aquarium&&d.Mechanic!=ModeMechanic.Beghouled&&d.Mechanic!=ModeMechanic.BeghouledTwist){DrawImage("ShovelBank.png",AdventureShovelButton);DrawImage("Shovel.png",AdventureShovelButton);if(shovel)Outline(AdventureShovelButton,Color.yellow,3);}
            ModeButton(AdventureMenuButton,"菜单");
            Fill(new Rect(0,568,800,32),new Color(0.06f,0.1f,0.04f,0.9f));
            string status=d.Survival?"旗帜 "+s.FlagsCompleted+" / "+(d.Endless?"∞":(d.Hard?10:5).ToString()):d.Endless?"连续通过 "+s.ClearedStages:d.Mechanic==ModeMechanic.Stars?"杨桃 "+game.ModeStarCount()+" / 14":d.Mechanic==ModeMechanic.Beghouled||d.Mechanic==ModeMechanic.BeghouledTwist?"配对 "+s.Score+" / 75":d.Mechanic==ModeMechanic.IZombie?"大脑 "+Array.FindAll(s.Brains,x=>x).Length+" / 5":"波次 "+game.Adventure.CurrentWave+" / "+game.Adventure.Waves.Count;
            Label(new Rect(8,570,605,26),d.Name+"  "+status,16,Color.white,TextAnchor.MiddleLeft,true);ModeButton(ModeBackButton,"返回模式选择");
            string advice=s.Advice;if(adventureFeedback!=null&&Time.unscaledTime<adventureFeedbackUntil)advice=adventureFeedback;
            if(modeSaveError!=null)advice=modeSaveError;
            if(!string.IsNullOrEmpty(advice)&&s.Phase!=ModePhase.ChoosingSeeds){DrawOriginalUiAdvice(new Rect(125,83,550,40),advice);ModeButton(new Rect(690,86,100,32),"知道了");}
            if(s.Phase==ModePhase.Preparing)ModeButton(new Rect(295,535,210,34),s.Stage==1?"开始进攻":"继续进攻");
            DrawAdventurePacketReadyFlashes();
            DrawCollectedSunsAboveSeedBank();
            if(s.Phase==ModePhase.ChoosingSeeds)DrawModeChooser();
            if(s.HasNextStage||s.Finished)DrawModeResult();return true;
        }
        private void ModeButton(Rect rect,string text)
        {DrawOriginalUiButton(rect,text);}
        private void DrawModeChooser()
        {
            Fill(new Rect(0,80,800,488),new Color(0,0,0,0.6f));DrawImage("SeedChooser_Background.png",new Rect(100,84,600,486));
            Label(new Rect(130,90,540,30),modeChoosingImitater?"选择模仿对象":"选择植物 "+pendingSeeds.Count+" / "+Math.Min(modeSession.PacketSlots,game.Adventure.AvailableSeeds.Count),22,Color.yellow,TextAnchor.MiddleCenter,true);
            var seeds=game.Adventure.AvailableSeeds;
            if(modeChoosingImitater){for(int i=0;i<40;i++)DrawAdventureCard((PlantKind)i,AdventureChooserRect(i),true);}
            else for(int i=0;i<seeds.Count;i++){Rect card=AdventureChooserRect(i);DrawAdventureCard(seeds[i],card,true);if(pendingSeeds.Contains(seeds[i]))Outline(card,Color.yellow,3);}
            ModeButton(new Rect(300,533,200,34),modeChoosingImitater?"取消":"开始游戏");
        }
        private void DrawModeResult()
        {
            var s=modeSession;DrawOriginalUiDialog(new Rect(200,166,400,370),s.HasNextStage?"本轮完成":s.Phase==ModePhase.Won?"挑战成功！":"挑战结束",null);
            string detail=s.HasNextStage?"阵型和阳光将保留，准备下一轮。":s.Phase==ModePhase.Won?(modeRewardClaimed?"奖励已存入金币钱包":"领取奖励："+(modeFirstClear?1000:250)+"金币"):s.Definition.Endless?"最高连续通过 "+s.ClearedStages+" 轮":"可以重新开始这一挑战。";
            if(modeSaveError!=null)detail=modeSaveError;
            Label(new Rect(240,260,320,70),detail,18,Color.white,TextAnchor.MiddleCenter,true);
            ModeButton(ModeNextButton,s.HasNextStage?"继续下一轮":s.Phase==ModePhase.Won?(modeRewardClaimed?"返回模式选择":"领取奖励"):"再试一次");
            ModeButton(ModeRestartButton,"重新开始");ModeButton(ModeExitButton,"返回模式选择");
        }
        private void DrawModeBoardOverlays()
        {
            var s=modeSession;var m=s.Definition.Mechanic;
            if(m==ModeMechanic.Vases)foreach(var vase in game.Adventure.Vases)
            {
                bool lantern=game.Plants.Exists(p=>p.Kind==PlantKind.Plantern&&Math.Max(Math.Abs(p.Row-vase.Row),Math.Abs(p.Column-vase.Column))<=1);
                if(!lantern&&!vase.ShowsPlant)continue;
                Rect icon=new Rect(AdventureGridX+vase.Column*CellWidth+20,AdventureCellY(vase.Row,vase.Column)+20,40,56);
                if(vase.Seed.HasValue)DrawAdventureCard(vase.Seed.Value,icon,false);
                else if(lantern&&vase.Zombie.HasValue)animations.Draw(AdventureZombieAnimation(vase.Zombie.Value),icon,game.Elapsed);
                else if(lantern)animations.Draw("Sun",icon,game.Elapsed);
            }
            if(m==ModeMechanic.IZombie)
            {
                int boundary=s.Definition.Id<=65?4:s.Definition.Id==69?6:5;Fill(new Rect(AdventureGridX+boundary*CellWidth-2,80,4,480),Color.red);
                for(int r=0;r<5;r++)if(!s.Brains[r]){Rect rect=new Rect(8,AdventureCellY(r,0)+27,38,35);Texture2D brain=Texture("brain.png");if(brain!=null)GUI.DrawTexture(rect,brain);else Label(rect,"脑",23,new Color(1,0.6f,0.7f),TextAnchor.MiddleCenter,true);}
            }
            if(m==ModeMechanic.Stars)for(int r=0;r<5;r++)for(int c=0;c<9;c++)if(GameSimulation.IsModeStarCell(r,c))Outline(new Rect(AdventureGridX+c*CellWidth+8,AdventureCellY(r,c)+18,64,65),new Color(1,0.95f,0.2f,0.8f),2);
            if(m==ModeMechanic.Beghouled||m==ModeMechanic.BeghouledTwist)if(s.SelectedCell>=0)Outline(new Rect(AdventureGridX+(s.SelectedCell%9)*CellWidth,AdventureCellY(s.SelectedCell/9,s.SelectedCell%9),80,90),Color.yellow,3);
            if(m==ModeMechanic.Portals)foreach(var p in s.Portals)animations.DrawNative(p.Pair==0?"Portal_Circle":"Portal_Square",new Vector2(AdventureGridX+p.Column*CellWidth,AdventureCellY(p.Row,p.Column)),1f,game.Elapsed,"anim_pulse");
            if(m==ModeMechanic.RainingSeeds)foreach(var rain in s.RainPackets){AdventurePacket packet=null;foreach(var p in game.Adventure.Packets)if(p.Id==rain.PacketId){packet=p;break;}if(packet==null)continue;Rect rect=ModeRainRect(rain);DrawAdventureConveyorPacket(packet,rect);}
            if(m==ModeMechanic.Slots)
            {for(int i=0;i<3;i++){int symbol=s.SlotSymbols[i];string text=symbol==56?"阳光":symbol==57?"钻石":AdventurePlantName((PlantKind)symbol);Label(new Rect(250+i*105,130,100,40),s.SlotRollingRemaining>0?"转动":text,19,Color.yellow,TextAnchor.MiddleCenter,true);}}
            foreach(var shot in s.HeadShots)DrawImage("PeaNormal_0.png",new Rect(AdventureGridX+shot.X*CellWidth-12,AdventureCellY((int)shot.Row,shot.X)+28,24,24));
        }
        private void DrawModeAquarium()
        {
            DrawImage("aquarium1.jpg",new Rect(0,0,800,600));
            foreach(var fish in modeSession.Fish)
            {animations.Draw("Zombie_snorkle",new Rect(fish.X-35,fish.Y-30,70,65),game.Elapsed,"anim_aquarium_swim");if(fish.Hunger<=15)Label(new Rect(fish.X-40,fish.Y-50,80,22),"饿了！",15,Color.red,TextAnchor.MiddleCenter,true);}
            foreach(var food in modeSession.Food)DrawImage("brain.png",new Rect(food.X-16,food.Y-15.5f,32,31));
            foreach(var sun in game.Suns)if(!sun.PresentationNative||!sun.PresentationPickup.Collecting)DrawAdventureSun(sun);
        }
    }
}
