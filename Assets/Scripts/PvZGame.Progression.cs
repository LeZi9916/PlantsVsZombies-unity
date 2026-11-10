using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using PvZReplica.Adventure;
using PvZReplica.Profiles;
using PvZReplica.Progression;
using PvZReplica.UI;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private PlayerAccountStore accountStore;
        private PlayerAccount displayAccountSnapshot;
        private float displayAccountRefresh,walletVisibleUntil;
        private string accountError;
        private int shopPage;
        private int selectedShopItem=-1;
        private ScreenView storeReturnView=ScreenView.Selector;
        private string progressionMessage;
        private GardenKind gardenKind;
        private GardenTool gardenTool;
        private Guid? movingGardenPlant;
        private long gardenUpdateUtc;
        private float treeGrowStarted=-100f;
        private readonly Dictionary<string,string> originalProgressionStrings=new Dictionary<string,string>();
        private string originalImportPath="";
        private string originalExportPath="";
        private IReadOnlyList<OriginalUserSave> importCandidates;
        private int importSelection;
        private static readonly int[][] OriginalShopPages={new[]{21,22,24,23,0,1,2,3},new[]{6,5,4,7,8,29},new[]{10,11,12,13,14,15,16,17},new[]{18,25,19,20,27,28}};
        private static long UtcSeconds { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }
        private bool EnsurePlayerAccount()
        {
            if(accountStore!=null)return profiles!=null&&profiles.SelectedId.HasValue;
            if(accountError!=null)return false;
            if(profiles==null||!profiles.SelectedId.HasValue){if(profileDialog!=null)profileDialog.Open();return false;}
            try{accountStore=new PlayerAccountStore(Path.Combine(smokeMode?Application.temporaryCachePath:Application.persistentDataPath,
                smokeMode?"pvz-account-"+Guid.NewGuid().ToString("N")+".account":"replica-player.account"));return true;}
            catch(Exception e){accountError=e.Message;SetUnavailable("金币档案无法读取",e.Message);return false;}
        }
        private PlayerAccount GetSelectedAccount(){return EnsurePlayerAccount()?accountStore.Get(profiles.SelectedId.Value):null;}
        private PlayerAccount ReadDisplayAccount()
        {
            if(profiles==null||!profiles.SelectedId.HasValue)return null;
            if(displayAccountSnapshot==null||displayAccountSnapshot.ProfileId!=profiles.SelectedId.Value||Time.unscaledTime>=displayAccountRefresh){displayAccountSnapshot=GetSelectedAccount();displayAccountRefresh=Time.unscaledTime+1f;}return displayAccountSnapshot;
        }
        private void ProgressionAction(Action action)
        {
            try{progressionMessage=null;action();}
            catch(Exception e){progressionMessage=e.Message;}
        }
        private void OpenStore()
        {
            if(!EnsurePlayerAccount()||!EnsureAdventureStore())return;
            var progress=adventureStore.Get(profiles.SelectedId.Value);
            if(progress.CompletedRuns==0&&progress.NextLevel<25){SetUnavailable("疯狂戴夫的商店","通过冒险模式 3-4，取得车钥匙后即可进入。");return;}
            storeReturnView=currentView==ScreenView.Garden||currentView==ScreenView.Tree?currentView:ScreenView.Selector;
            currentView=ScreenView.Store;shopPage=0;selectedShopItem=-1;progressionMessage=null;
        }
        private void OpenZenGarden()
        {
            if(!EnsurePlayerAccount()||!EnsureAdventureStore())return;
            var progress=adventureStore.Get(profiles.SelectedId.Value);
            if(progress.CompletedRuns==0&&progress.NextLevel<45){SetUnavailable("禅境花园","通过冒险模式 5-4，取得浇水壶后即可进入。");return;}
            currentView=ScreenView.Garden;gardenKind=GardenKind.Greenhouse;gardenTool=GardenTool.Water;movingGardenPlant=null;progressionMessage=null;
            ProgressionAction(()=>accountStore.UpdateGarden(profiles.SelectedId.Value,UtcSeconds,true));
        }
        private void OpenWisdomTree()
        {
            var p=GetSelectedAccount();if(p==null)return;
            if(p.PurchaseCount(27)==0){SetUnavailable("智慧树","请先在商店购买智慧树。");return;}
            currentView=ScreenView.Tree;treeGrowStarted=-100f;progressionMessage=null;
        }
        private void UpdateProgressionMusicContext()
        {
            DynamicMusicOverride=currentView==ScreenView.Garden||currentView==ScreenView.Tree||currentView==ScreenView.Store&&(storeReturnView==ScreenView.Garden||storeReturnView==ScreenView.Tree)?(Audio.MusicCue?)Audio.MusicCue.ZenGarden:
                IndependentModeMusicCue;
        }
        private void UpdateProgression()
        {
            if(currentView==ScreenView.Garden&&accountStore!=null&&UtcSeconds!=gardenUpdateUtc)
            {gardenUpdateUtc=UtcSeconds;ProgressionAction(()=>accountStore.UpdateGarden(profiles.SelectedId.Value,gardenUpdateUtc));}
        }
        private void DrawAccountStatus()
        {
            if(profiles==null||!profiles.SelectedId.HasValue)return;
            var p=GetSelectedAccount();if(p==null)return;
            Label(new Rect(528,181,210,26),"金币："+p.DisplayCoins,17,new Color(1f,0.92f,0.4f),TextAnchor.MiddleCenter,true);
            Label(new Rect(10,576,250,22),"F9：原版档案导入 / 导出",13,Color.white,TextAnchor.MiddleLeft,false);
        }
        private void DrawNativeProgressionImage(string name,float x,float y)
        {
            var t=Texture(name);if(t==null&&name.EndsWith(".png"))t=Texture(name.Substring(0,name.Length-4)+".jpg");
            if(t!=null)GUI.DrawTexture(new Rect(x,y,t.width,t.height),t,ScaleMode.StretchToFill,true);
        }
        private static Rect ShopItemRect(int slot){return slot<4?new Rect(422+74*slot,206,70,100):new Rect(372+74*(slot-4),310,70,100);}
        private void DrawProgressionPlantCard(PlantKind kind,Rect card)
        {
            var seeds=Texture("seeds.png");if(seeds!=null)GUI.DrawTextureWithTexCoords(card,seeds,new Rect(2f/9f,0,1f/9f,1));
            int cell=AlmanacPortraitLayout.PlantAtlasCell((int)kind);var atlas=cell<0?null:Texture("packet_plants.png");
            if(atlas!=null)GUI.DrawTextureWithTexCoords(card,atlas,new Rect(cell/13f,0,1f/13f,1));
            else {Vector2 origin;float scale;AlmanacPortraitLayout.PlantThumbnailTransform((int)kind,card,out origin,out scale);animations.DrawNative(AdventurePlantAnimation(kind),origin,scale,0f);}
        }
        private void DrawStore()
        {
            var p=GetSelectedAccount();if(p==null)return;var progress=adventureStore.Get(profiles.SelectedId.Value);
            DrawImage("Store_Background.jpg",new Rect(0,0,800,600));DrawNativeProgressionImage("Store_Car.png",196,138);
            DrawNativeProgressionImage("Store_HatchbackOpen.png",299,0);DrawNativeProgressionImage("Store_Sign.png",285,0);
            animations.DrawNative("CrazyDave",new Vector2(-42,68),1f,Time.unscaledTime,"anim_idle");
            var page=OriginalShopPages[shopPage];
            for(int i=0;i<page.Length;i++)
            {
                var item=ShopCatalog.Get(page[i]);Rect rect=ShopItemRect(i);bool locked=!ShopCatalog.IsUnlocked(item,progress.NextLevel,progress.CompletedRuns),sold=ShopCatalog.SoldOut(item,p,UtcSeconds);
                if(item.Id<9)DrawProgressionPlantCard((PlantKind)(40+item.Id),new Rect(rect.x,rect.y,50,70));
                else if(item.Id>=10&&item.Id<=12)animations.Draw("ZenGarden_sprout",new Rect(rect.x,rect.y+12,70,62),Time.unscaledTime,"anim_idle");
                else if(item.Id==20)animations.Draw(item.Id==22?"PoolCleaner":item.Id==23?"RoofCleaner":"Stinky",new Rect(rect.x+8,rect.y+8,58,55),0f);
                else if(item.Icon!=null){float dx=item.Id==13?-14:item.Id==14||item.Id==28?-11:item.Id==18||item.Id==25||item.Id==27?-8:item.Id==24?-5:item.Id==21?-7:item.Id==22?1:item.Id==29?-1:-12;float dy=item.Id==13?-4:item.Id==14||item.Id==28?-2:item.Id==18||item.Id==25||item.Id==27?2:item.Id==24?10:item.Id==21||item.Id==22?7:item.Id==23?28:item.Id==29?13:3;DrawNativeProgressionImage(item.Icon,rect.x+dx,rect.y+dy);}
                if(item.Id==21)Label(new Rect(rect.x,rect.y+6,55,70),(p.PacketSlots+1).ToString(),36,new Color(.75f,.94f,.2f),TextAnchor.MiddleCenter,true);
                DrawNativeProgressionImage("Store_PriceTag.png",rect.x-3,rect.y+70);
                string price="$"+(ShopCatalog.Price(item,p)*10).ToString("N0",System.Globalization.CultureInfo.InvariantCulture);
                if(!DrawBitmap(sunCounterBitmap,price,rect.x+23,rect.y+85,90,Color.black,true))Label(new Rect(rect.x-5,rect.y+66,75,24),price,14,Color.black,TextAnchor.MiddleCenter,true);
                if(locked||sold)Label(rect,OriginalProgressionText(locked?"COMING_SOON":"SOLD_OUT"),16,Color.red,TextAnchor.MiddleCenter,true);
                else if(item.Id<9&&rect.Contains(adventurePointer)) DrawImage("SeedPacketFlash.png",new Rect(rect.x,rect.y,50,70));
                if(item.Id==selectedShopItem)Outline(rect,Color.yellow,2);
            }
            OriginalUiWidgets.ImageButton(Texture,new Rect(252,402,Texture("Store_PrevButton.png").width,Texture("Store_PrevButton.png").height),
                "Store_PrevButton.png","Store_PrevButtonHighlight.png",null,
                OriginalUiWidgets.Hovered(new Rect(252,402,96,75)),OriginalUiWidgets.Pressed(new Rect(252,402,96,75)),true);
            OriginalUiWidgets.ImageButton(Texture,new Rect(596,402,Texture("Store_NextButton.png").width,Texture("Store_NextButton.png").height),
                "Store_NextButton.png","Store_NextButtonHighlight.png",null,
                OriginalUiWidgets.Hovered(new Rect(596,402,136,83)),OriginalUiWidgets.Pressed(new Rect(596,402,136,83)),true);
            OriginalUiWidgets.ImageButton(Texture,new Rect(366,512,138,80),"Store_MainMenuButton.png","Store_MainMenuButtonHighlight.png","Store_MainMenuButtonDown.png",OriginalUiWidgets.Hovered(new Rect(366,512,138,80)),OriginalUiWidgets.Pressed(new Rect(366,512,138,80)));Label(new Rect(366,512,138,80),"主菜单",15,new Color(98f/255f,153f/255f,235f/255f),TextAnchor.MiddleCenter,true);
            DrawNativeProgressionImage("coinbank.png",650,559);Label(new Rect(650,559,116,30),p.DisplayCoins.ToString(),23,new Color(0.7f,1f,0.35f),TextAnchor.MiddleRight,true);
            Label(new Rect(330,475,280,26),"疯狂戴夫的商店  "+(shopPage+1)+" / 4",19,Color.white,TextAnchor.MiddleCenter,true);
            DrawProgressionMessage();
            if(selectedShopItem>=0)DrawStorePurchaseDialog();
        }
        private void DrawStorePurchaseDialog()
        {
            DrawOriginalUiDialog(new Rect(200,166,400,270),OriginalProgressionText("BUY_ITEM_HEADER"),
                string.IsNullOrEmpty(progressionMessage)?OriginalProgressionText("BUY_ITEM"):progressionMessage);
            DrawOriginalUiButton(new Rect(245,346,135,46),OriginalProgressionText("DIALOG_BUTTON_YES"));
            DrawOriginalUiButton(new Rect(420,346,135,46),OriginalProgressionText("DIALOG_BUTTON_NO"));
        }
        private Rect GardenPlantRect(GardenPlant g)
        {
            var grid=OriginalGardenLayout.Grids[(int)g.Garden];int slot=Mathf.Clamp(g.Slot,0,grid.Length-1);
            return new Rect(grid[slot][0],grid[slot][1],80,85);
        }
        private static string GardenNeedName(GardenNeed need)
        {switch(need){case GardenNeed.Water:return "需要浇水";case GardenNeed.Fertilizer:return "需要肥料";case GardenNeed.BugSpray:return "需要杀虫剂";case GardenNeed.Music:return "想听音乐";case GardenNeed.Happy:return "心满意足";default:return "休息中";}}
        private void DrawGarden()
        {
            var p=GetSelectedAccount();if(p==null)return;
            DrawImage(gardenKind==GardenKind.Greenhouse?"Background_Greenhouse.jpg":gardenKind==GardenKind.Mushroom?"Background_MushroomGarden.jpg":"aquarium1.jpg",new Rect(0,0,800,600));
            foreach(var g in p.Plants)
            {
                if(g.Garden!=gardenKind)continue;Rect r=GardenPlantRect(g);
                if(g.Garden!=GardenKind.Aquarium)animations.Draw("FlowerPot",new Rect(r.x+8,r.y+39,64,49),0f,"anim_zengarden");
                string anim=g.Growth==0?"ZenGarden_sprout":AdventurePlantAnimation(g.Kind);float size=g.Growth==0?55:g.Growth==1?56:g.Growth==2?68:80;
                animations.Draw(anim,new Rect(r.center.x-size/2,r.y+70-size,size,size),Time.unscaledTime,g.Growth==0?"anim_idle":g.Garden==GardenKind.Aquarium&&(g.Kind==PlantKind.TangleKelp||g.Kind==PlantKind.SeaShroom)?"anim_idle_aquarium":null);
                var need=g.Need(UtcSeconds);Label(new Rect(r.x-13,r.y-15,106,20),GardenNeedName(need),12,need==GardenNeed.Happy?Color.yellow:Color.white,TextAnchor.MiddleCenter,true);
                if(g.PendingCoins>0){DrawImage("coinbank.png",new Rect(r.x+10,r.y+60,60,20));Label(new Rect(r.x,r.y+62,80,20),"+"+g.PendingCoins*10,14,Color.yellow,TextAnchor.MiddleCenter,true);}
            }
            string[] names={"浇水","施肥","杀虫","音乐","收金币","出售","搬运","巧克力"};string[] icons={p.PurchaseCount(13)>0?"WateringCanGold.png":"WateringCan.png","Fertilizer.png","BugSpray.png","Phonograph.png","Zen_MoneySign.png","Zen_MoneySign.png","Zen_GardenGlove.png","chocolate.png"};
            for(int i=0;i<8;i++){var r=new Rect(8+i*78,4,73,57);if(icons[i]!=null)DrawImage(icons[i],new Rect(r.x+16,r.y,45,38));Label(new Rect(r.x,r.y+37,78,19),names[i]+(i==1?" "+p.PurchaseCount(14):i==2?" "+p.PurchaseCount(15):""),13,Color.white,TextAnchor.MiddleCenter,true);if((int)gardenTool==i)Outline(r,Color.yellow,2);}
            DrawOriginalUiButton(new Rect(674,6,120,36),"主菜单");DrawOriginalUiButton(new Rect(654,540,136,42),"商店");
            DrawOriginalUiButton(new Rect(8,540,155,42),"下一个花园");DrawOriginalUiButton(new Rect(175,540,120,42),"智慧树");
            Label(new Rect(310,548,200,28),"金币："+p.DisplayCoins,19,Color.yellow,TextAnchor.MiddleCenter,true);
            if(p.PurchaseCount(20)>0){animations.Draw("Stinky",new Rect(500,494,80,62),Time.unscaledTime);Label(new Rect(490,551,105,22),"点击唤醒蜗牛",13,Color.white,TextAnchor.MiddleCenter,true);}
            DrawProgressionMessage();
        }
        private string OriginalProgressionText(string key)
        {
            if(originalProgressionStrings.Count==0)
            {
                string file=Path.Combine(root,"properties","LawnStrings.txt");if(!File.Exists(file))return key;
                string current=null;var value=new StringBuilder();
                foreach(string line in File.ReadAllLines(file))
                {
                    if(line.StartsWith("[")&&line.EndsWith("]")){if(current!=null)originalProgressionStrings[current]=value.ToString().Trim();current=line.Substring(1,line.Length-2);value.Length=0;}
                    else if(current!=null)value.AppendLine(line);
                }
                if(current!=null)originalProgressionStrings[current]=value.ToString().Trim();
            }
            string result;return originalProgressionStrings.TryGetValue(key,out result)?result:"";
        }
        private void DrawWisdomTree()
        {
            var p=GetSelectedAccount();if(p==null)return;
            int pose=Math.Min(treeGrowStarted<0?50:51,Math.Max(1,p.TreeHeight));
            Reanimation.ReanimationRenderer.AnimationInfo treeInfo;float fps=animations.TryGetAnimationInfo("TreeOfWisdom","anim_grow"+pose,out treeInfo)?treeInfo.FramesPerSecond:12f;
            float age=treeGrowStarted<0?100f:(Time.unscaledTime-treeGrowStarted)*8f/fps;
            animations.DrawNative("TreeOfWisdom",Vector2.zero,1f,age,"anim_grow"+pose,false);
            DrawOriginalUiButton(new Rect(655,8,138,40),"禅境花园");DrawOriginalUiButton(new Rect(655,548,138,42),"商店");
            DrawImage("TreeFood.png",new Rect(24,505,68,80));DrawOriginalUiButton(new Rect(102,539,175,44),"施肥（"+p.PurchaseCount(28)+"）");
            if(p.TreeHeight>=50)Label(new Rect(250,8,300,30),OriginalProgressionText("TREE_OF_WISDOM_HIEGHT").Replace("{HEIGHT}",p.TreeHeight.ToString()),20,Color.white,TextAnchor.MiddleCenter,true);
            int wisdom=p.TreeHeight==100?800:p.TreeHeight==500?900:p.TreeHeight==1000?1000:p.TreeHeight>1000?1100:Math.Max(1,Math.Min(49,p.TreeHeight-1));
            string advice=OriginalProgressionText("TREE_OF_WISDOM_"+wisdom);
            float speechX=p.TreeHeight<7?400:p.TreeHeight<12?395:390,speechY=p.TreeHeight<7?152:p.TreeHeight<12?60:52;
            DrawNativeProgressionImage("Store_SpeechBubble2.png",speechX,speechY);Label(new Rect(speechX+25,speechY+6,233,144),advice,16,Color.black,TextAnchor.MiddleCenter,false);DrawProgressionMessage();
        }
        private void DrawProgressionMessage(){if(!string.IsNullOrEmpty(progressionMessage))DrawOriginalUiAdvice(new Rect(12,574,776,24),progressionMessage);}
        private bool HandleProgressionClick(Vector2 point)
        {
            if(currentView==ScreenView.Store)
            {
                if(selectedShopItem>=0)
                {
                    if(new Rect(245,346,135,46).Contains(point)){var progress=adventureStore.Get(profiles.SelectedId.Value);ProgressionAction(()=>{accountStore.Purchase(profiles.SelectedId.Value,selectedShopItem,progress.NextLevel,progress.CompletedRuns,UtcSeconds);selectedShopItem=-1;displayAccountSnapshot=null;});}
                    else if(new Rect(420,346,135,46).Contains(point)){selectedShopItem=-1;progressionMessage=null;}return true;
                }
                if(new Rect(366,512,138,80).Contains(point)){currentView=storeReturnView;return true;}
                if(new Rect(252,402,96,75).Contains(point)){shopPage=(shopPage+3)%4;selectedShopItem=-1;return true;}
                if(new Rect(596,402,136,83).Contains(point)){shopPage=(shopPage+1)%4;selectedShopItem=-1;return true;}
                var page=OriginalShopPages[shopPage];for(int i=0;i<page.Length;i++)if(ShopItemRect(i).Contains(point)){var item=ShopCatalog.Get(page[i]);var progress=adventureStore.Get(profiles.SelectedId.Value);var account=GetSelectedAccount();if(ShopCatalog.IsUnlocked(item,progress.NextLevel,progress.CompletedRuns)&&!ShopCatalog.SoldOut(item,account,UtcSeconds))selectedShopItem=page[i];progressionMessage=null;return true;}return true;
            }
            if(currentView==ScreenView.Tree)
            {
                if(new Rect(655,8,138,40).Contains(point))currentView=ScreenView.Garden;
                else if(new Rect(655,548,138,42).Contains(point))OpenStore();
                else if(new Rect(24,505,253,80).Contains(point)){if(Time.unscaledTime-treeGrowStarted<1.2f)return true;ProgressionAction(()=>accountStore.FeedWisdomTree(profiles.SelectedId.Value));treeGrowStarted=Time.unscaledTime;}
                return true;
            }
            if(currentView==ScreenView.Import)return true;
            if(currentView!=ScreenView.Garden)return false;
            if(new Rect(674,6,120,36).Contains(point)){currentView=ScreenView.Selector;return true;}
            if(new Rect(654,540,136,42).Contains(point)){OpenStore();return true;}
            if(new Rect(175,540,120,42).Contains(point)){OpenWisdomTree();return true;}
            if(new Rect(8,540,155,42).Contains(point))
            {
                var p=GetSelectedAccount();GardenKind next=(GardenKind)(((int)gardenKind+1)%3);
                if(next==GardenKind.Mushroom&&p.PurchaseCount(18)==0)next=GardenKind.Aquarium;if(next==GardenKind.Aquarium&&p.PurchaseCount(25)==0)next=GardenKind.Greenhouse;
                if(movingGardenPlant.HasValue){Guid id=movingGardenPlant.Value;ProgressionAction(()=>accountStore.TendGarden(profiles.SelectedId.Value,id,GardenTool.Move,UtcSeconds,next));movingGardenPlant=null;}
                gardenKind=next;return true;
            }
            if(new Rect(490,490,105,82).Contains(point)&&GetSelectedAccount().PurchaseCount(20)>0){ProgressionAction(()=>accountStore.WakeSnail(profiles.SelectedId.Value,UtcSeconds));return true;}
            for(int i=0;i<8;i++)if(new Rect(8+i*78,4,73,57).Contains(point)){gardenTool=(GardenTool)i;return true;}
            foreach(var g in GetSelectedAccount().Plants)if(g.Garden==gardenKind&&GardenPlantRect(g).Contains(point))
            {
                if(gardenTool==GardenTool.Move){movingGardenPlant=g.Id;progressionMessage="选择下一个花园，将植物放过去。";}
                else ProgressionAction(()=>accountStore.TendGarden(profiles.SelectedId.Value,g.Id,gardenTool,UtcSeconds));return true;
            }
            return true;
        }
        private void DrawOriginalSaveUtility()
        {
            DrawImage("Challenge_Background.jpg",new Rect(0,0,800,600));
            DrawOriginalUiDialog(new Rect(20,20,760,560),"原版年度版档案导入 / 导出",null,true,false);
            Label(new Rect(55,88,690,36),"输入 userdata 文件夹，或单独的 user*.dat 路径",17,Color.white,TextAnchor.MiddleLeft,false);
            originalImportPath=OriginalUiTextField(new Rect(55,128,570,32),originalImportPath);if(OriginalUiButton(new Rect(635,128,110,32),"读取"))ProgressionAction(()=>{importCandidates=Directory.Exists(originalImportPath)?OriginalSaveFormat.ReadDirectory(originalImportPath):Array.AsReadOnly(new[]{OriginalSaveFormat.ReadUser(originalImportPath)});importSelection=0;});
            if(importCandidates!=null)
            {
                int shown=Math.Min(6,importCandidates.Count);for(int i=0;i<shown;i++){var p=importCandidates[i];if(OriginalUiListRow(new Rect(70,181+i*33,660,29),p.Name+"  冒险 "+AdventureLevels.Get(p.Level).Label+"  金币 "+p.Coins*10+"  通关 "+p.FinishedAdventure+" 次",i==importSelection))importSelection=i;}
                if(shown>0&&OriginalUiButton(new Rect(300,389,200,36),"导入为新用户"))ProgressionAction(ImportOriginalUser);
            }
            Label(new Rect(55,434,690,30),"从当前用户导出独立 user*.dat（需先导入原版用户）",16,Color.white,TextAnchor.MiddleLeft,false);
            originalExportPath=OriginalUiTextField(new Rect(55,471,570,32),originalExportPath);if(OriginalUiButton(new Rect(635,471,110,32),"导出"))ProgressionAction(()=>{if(!EnsurePlayerAccount()||!EnsureAdventureStore())return;string copy=Path.Combine(Application.persistentDataPath,"original-"+profiles.SelectedId.Value.ToString("N")+".dat");OriginalSaveFormat.ExportUser(copy,originalExportPath,GetSelectedAccount(),adventureStore.Get(profiles.SelectedId.Value));progressionMessage="导出完成："+originalExportPath;});
            if(OriginalUiButton(new Rect(320,530,160,36),"返回主菜单"))currentView=ScreenView.Selector;DrawProgressionMessage();
        }
        private void ImportOriginalUser()
        {
            if(importCandidates==null||importSelection>=importCandidates.Count||!EnsureAdventureStore()||!EnsurePlayerAccount())return;
            var source=importCandidates[importSelection];string name=source.Name;if(name.Length>26)name=name.Substring(0,26);int suffix=1;bool used;
            do{used=false;foreach(var existing in profiles.Profiles)if(string.Equals(existing.Name,name,StringComparison.OrdinalIgnoreCase)){used=true;break;}if(used)name=source.Name.Substring(0,Math.Min(24,source.Name.Length))+"（"+suffix+++"）";}while(used);
            Guid importedId=Guid.NewGuid();string copy=Path.Combine(Application.persistentDataPath,"original-"+importedId.ToString("N")+".dat");OriginalSaveFormat.KeepImportedSource(source,copy);
            accountStore.Import(importedId,source.Coins,source.Purchases,source.ChallengeRecords,source.ChallengeRecords[49],source.Plants,source.MinigamesUnlocked,source.PuzzleUnlocked,source.SurvivalUnlocked);
            adventureStore.Import(importedId,source.Level,source.FinishedAdventure);profiles.CreateImported(importedId,name);progressionMessage="已导入用户："+name;currentView=ScreenView.Selector;
        }
    }
}
