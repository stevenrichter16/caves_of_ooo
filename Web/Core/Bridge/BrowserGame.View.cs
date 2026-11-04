using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;

namespace CavesOfOoo.Web
{
    public partial class BrowserGame
    {
        object ItemRow(Entity e,bool actions=false)
        {
            if(e==null)return null;
            return new {id=e.ID,name=e.GetDisplayName(),blueprint=e.BlueprintName,count=e.GetPart<StackerPart>()?.StackCount??1,
                focus=ThrowingRules.CanFocus(player,e),equipped=InventorySystem.IsEquipped(player,e),weight=InventoryPart.GetItemWeight(e),art=Art(e,0,0),
                actions=actions?InventorySystem.GetActions(player,e).Select(a=>new {name=a.Display,command=a.Command}).ToArray():null};
        }
        public string Snapshot()
        {
            if(!IsReady)return JsonConvert.SerializeObject(new {revision,error,ready=false});
            var here=zone.GetEntityCell(player);var cells=new List<object>(1600);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                var cell=zone.GetCell(x,y);if(!cell.Explored)continue;
                Entity top=null,groundOwner=null;
                foreach(var candidate in cell.Occupants)
                {
                    if(groundOwner==null&&(candidate.HasTag("Floor")||candidate.HasTag("Terrain")))groundOwner=candidate;
                    var r=candidate.GetPart<RenderPart>();if(r==null||!r.Visible||candidate.HasTag(WorldInteractionSystem.WorldMetadataTag))continue;
                    // Unseen actors, portable items and transient effects are never leaked through map data.
                    if(!cell.IsVisible&&!candidate.HasTag("Wall")&&!candidate.HasTag("Floor")&&!candidate.HasTag("Terrain"))continue;
                    if(top==null||r.RenderLayer>=top.GetPart<RenderPart>().RenderLayer)top=candidate;
                }
                var render=top?.GetPart<RenderPart>();
                string glyph=render?.ResolveGlyph(x,y).ToString()??"·";
                cells.Add(new {x,y,v=cell.IsVisible,g=glyph,c=render?.ColorString??"&K",bg=render?.BackgroundColor??"",art=Art(top,x,y),
                    hostile=cell.IsVisible&&top!=null&&top.HasTag("Creature")&&FactionManager.IsHostile(player,top),solid=cell.IsSolid(),floor=top?.HasTag("Floor")==true,ground=groundOwner?.BlueprintName??"Grass",wet=cell.IsVisible&&zone.TileState.Get(x,y)?.Coatings?.Any(k=>k.Id=="water"&&k.Turns>0)==true});
            }
            var pack=player.GetPart<InventoryPart>();var inventory=pack.Objects.Concat(pack.GetAllEquipped()).Distinct().Select(e=>ItemRow(e)).ToArray();
            var abilities=player.GetPart<ActivatedAbilitiesPart>()?.AbilityList.Select(a=>new {id=a.ID.ToString(),name=a.DisplayName,category=a.Class,range=a.Range,targeting=a.TargetingMode.ToString(),cooldown=a.CooldownRemaining}).ToArray();
            var nearby=new List<object>();var seen=new HashSet<string>();
            if(here!=null)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {
                var c=zone.GetCell(here.X+dx,here.Y+dy);if(c?.IsVisible!=true)continue;
                foreach(var e in c.Occupants)
                {if(e==player||e.GetPart<RenderPart>()?.Visible==false||!seen.Add(e.ID)||e.HasTag(WorldInteractionSystem.WorldMetadataTag))continue;
                    var actions=WorldInteractionSystem.GatherActions(e,player);if(actions.Count==0)continue;
                    nearby.Add(new {id=e.ID,name=e.GetDisplayName(),x=c.X,y=c.Y,art=Art(e,c.X,c.Y),actions=actions.Select(a=>new{name=a.Display,command=a.Command}).ToArray()});}
            }
            object look=null;
            var lookCell=zone.GetCell(lookX,lookY);
            if(panel=="look"&&lookCell?.IsVisible==true)
            {
                var snap=LookQueryService.BuildSnapshot(player,zone,lookX,lookY);
                look=new {x=lookX,y=lookY,title=snap.Header,summary=snap.Summary,details=snap.DetailLines,
                    target=selected==null?null:new {id=selected.ID,name=selected.GetDisplayName(),art=Art(selected,lookX,lookY)},
                    reachable=selected!=null&&Adjacent(selected),
                    actions=selected!=null&&Adjacent(selected)?WorldInteractionSystem.GatherActions(selected,player).Select(a=>new{name=a.Display,command=a.Command}).ToArray():null,
                    items=InventorySystem.GetTakeableItemsInCell(lookCell,player).Select(e=>ItemRow(e)).ToArray(),underfoot=here!=null&&here.X==lookX&&here.Y==lookY};
            }
            object conversation=ConversationManager.IsActive?new {speaker=ConversationManager.Speaker?.GetDisplayName(),text=ConversationManager.CurrentText,
                choices=ConversationManager.VisibleChoices.Select((c,i)=>new{index=i,text=c.Text}).ToArray(),escape=ConversationManager.CurrentNode?.AllowEscape??true}:null;
            var performance=TradeSystem.GetTradePerformance(player);
            object trade=trader!=null&&Adjacent(trader)?new {name=trader.GetDisplayName(),drams=TradeSystem.GetDrams(trader),
                stock=TradeSystem.GetTraderStock(trader).Select(e=>new {id=e.ID,name=e.GetDisplayName(),price=TradeSystem.GetBuyPrice(e,performance,trader),count=e.GetPart<StackerPart>()?.StackCount??1}).ToArray(),
                sell=pack.Objects.Select(e=>new{id=e.ID,name=e.GetDisplayName(),price=TradeSystem.GetSellPrice(e,performance,trader)}).ToArray()}:null;
            object contents=container!=null&&Adjacent(container)?new{name=container.GetDisplayName(),items=container.GetPart<ContainerPart>()?.Contents.Select(e=>ItemRow(e)).ToArray()}:null;
            return JsonConvert.SerializeObject(new {revision,ready=true,error,faulted,gameId,build=buildId,seed=zones.WorldSeed,
                name=player.GetDisplayName(),tick=turns.TickCount,zone=zone.ZoneID,zoneName=PlaceName(),x=here?.X??0,y=here?.Y??0,width=Zone.Width,height=Zone.Height,
                hp=player.GetStatValue("Hitpoints"),maxHp=player.GetStat("Hitpoints")?.Max??40,dead=Dead,
                level=player.GetStatValue("Level"),focus=player.GetStatValue("Focus"),drams=TradeSystem.GetDrams(player),weight=pack.GetCarriedWeight(),
                stats=new[]{"Strength","Agility","Toughness","Intelligence","Willpower","Ego","AV","DV","Speed","XP","SP"}.Select(s=>new{name=s,value=player.GetStatValue(s)}).ToArray(),
                cells,inventory,abilities,nearby,look,conversation,trade,contents,panel,skills=panel=="skills"?SkillsView():null,item=selected!=null&&Owned(selected.ID)!=null?ItemRow(selected,true):null,
                crafting=panel=="crafting"?CraftingView():null,throwing=panel=="throwing"?ThrowingView():null,throwPreview=panel=="throwPreview"&&pendingThrow!=null?new{description=pendingThrow.Description,mode=pendingThrow.Mode.ToString()}:null,
                messages=MessageLog.GetRecent(80),quests=QuestLogStateBuilder.Build(StoryletPart.Current),
                visited=zones.CachedZones.Keys.ToArray(),ground=InventorySystem.GetTakeableItemsAtFeet(player,zone).Select(e=>ItemRow(e)).ToArray()});
        }
        string PlaceName()
        {
            if(zone.ZoneID=="Overworld.11.10.0")return "Gleaners’ Glade";
            if(zone.ZoneID=="Overworld.11.10.1")return "Gleaners’ Cellar";
            if(zone.ZoneID=="Overworld.10.10.0")return "Sill";
            return zone.ZoneID.Replace("Overworld.","The Overworld · ").Replace('.','·');
        }
        static string Art(Entity e,int x,int y)
        {
            if(e==null)return null;
            string b=e.BlueprintName,v=e.GetPart<RenderPart>()?.VisualID;
            if(e.HasTag("Player"))return "opening-base:0";
            switch(b)
            {
                case "Warden":return "opening-base:9";
                case "Wall":return "opening-base:"+(v=="reference-glade-low-wall"?2:1);
                case "Reeds":return "opening-base:"+(3+((x+y)&1));
                case "Bush":return "opening-base:5";
                case "Rubble":case "BrokenColumn":return "opening-base:6";
                case "RipeCropRow":return "opening-base:"+(e.GetPart<FieldHarvestPart>()?.Harvested==true?8:7);
                case "CropRow":return "opening-base:8";
                case "Villager":return "opening-cast:0";
                case "PetDog":return "opening-cast:1";
                case "MarlbackScrabbler":return "opening-cast:2";
                case "MarlbackGleaner":return "opening-cast:3";
                case "RepairLinedWell":return "opening-anchors:"+(e.GetPart<RepairablePart>()?.Repaired==true?1:0);
                case "SpreadDrawPoint":return "opening-anchors:"+(e.GetPart<LiquidPoolPart>()?.Volume>0?2:3);
                case "StairsDown":return "opening-anchors:4";
                case "FallenBeam":return "opening-anchors:5";
                case "Chest":return "opening-anchors:6";
                case "WoodenBarrel":return "opening-anchors:7";
                case "Signpost":return "opening-anchors:8";
                case "MushroomRing":return "opening-anchors:9";
                case "GlowQuartzVein":return "opening-anchors:10";
                case "StairsUp":return "opening-cellar:0";
                case "WoodenCrate":return "opening-cellar:1";
                case "LowPallet":return "opening-cellar:2";
                case "StoneWall":return "opening-cellar:8";
                case "Campfire":return "opening-states:4";
                case "Torch":return "opening-states:"+(e.GetPart<TorchLightPart>()?.ControlledFlameActive==true?10:9);
                case "Waterskin":return "opening-states:11";
                case "GleanersBuckledWicket":return "opening-states:"+(RepairablePart.BlocksFunction(e)?0:e.GetPart<DoorPart>()?.IsClosed==true?1:2);
            }
            string[] items={"Emberwheat","ToastedEmberwheat","DrawgourdShell","DrawgourdSeed","Mushroom","RoastedMushroom","GlowQuartz","HealingTonic","DriedMeat","FireClay","SalvagedTimber","MarlbackCorpse"};
            int index=Array.IndexOf(items,b);return index>=0?"opening-items:"+index:null;
        }
    }
}
