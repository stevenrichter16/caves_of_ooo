using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class QuestFreeSpreadStateNativePlayer
    {
        const string HaulingBoundary = "Seed64, first eight canonical HeavySalvage assignments; actual committed beam/barrel and twelve original hedges. One disclosed original-player transfer to the source-side approach. Native C/direction/G grab, two paid pulls, release, short crossing, inactive F5/F6 replacement and actual cache return. Long eight-direction bypass is measured geometry, not walked. No source, item, health, strength, clock, RNG or NPC-scheduling grants. Thirty paid inputs maximum;150seconds. Batched source/submitted proof is not an unoccluded-pixel or human-readability claim; images require review.";
        static readonly (int x,int y)[] HaulDirections = {(0,-1),(1,-1),(1,0),(1,1),(0,1),(-1,1),(-1,0),(-1,-1)};
        sealed class HaulSite
        {
            internal Zone Zone;
            internal Entity Load;
            internal Entity[] Hedges;
            internal Cell Aperture,Approach,Opposite,Parked,FinalPlayer;
            internal int DX,DY;
            internal List<Cell> Bypass,ClearPath;
        }
        sealed class HaulOwner
        {
            internal Entity Owner;
            internal Part[] Parts;
            internal (int x,int y) Position;
            internal string Facts;
            internal EntityVisualFacing Facing;
            internal HaulOwner(Entity owner,Zone zone)
            {Owner=owner;Parts=HaulParts(owner);Position=zone.GetEntityPosition(owner);Facts=HaulFacts(owner);Facing=owner.GetPart<RenderPart>().VisualFacing;}
        }
        sealed class HaulStyle
        {
            internal string Model;
            internal int Quarter;
            internal Mesh Mesh;
            internal Material[] Materials;
        }
        void BeginHaulingDiagnostics()
        {
            // Share the existing finally-restored channel store; no diagnostic
            // state survives this observer, including absent prior keys.
            _exchangeOldChannels["drag"]=_exchangeChannelStore.TryGetValue("drag",out bool value)?(bool?)value:null;
            Diag.SetChannel("drag",true);
        }
        IEnumerator Hauling()
        {
            if(!Enum.TryParse("HeavySalvage",out SpreadExplorationFamily family))
            {Unverified("heavy-salvage","Current build has no HeavySalvage family.");yield break;}
            HaulSite site=null;
            foreach(var entry in Candidates(family))
            {
                var zone=Manager.GetZone(entry.ZoneID);
                string reason=zone==null?"generation-refused":"no-complete-current-source";
                if(zone!=null&&Manager.Exploration.DispositionFor(entry.ZoneID)==2)
                    foreach(var load in zone.GetReadOnlyEntities().Where(HaulBlueprint).OrderBy(e=>e.ID,StringComparer.Ordinal))
                    {
                        if(HaulQualify(zone,load,out site,out reason))break;
                    }
                _observations.Add(new{phase="hauling-source-selection",zone=entry.ZoneID,disposition=Manager.Exploration.DispositionFor(entry.ZoneID),owner=site?.Load.ID,reason,admitted=site!=null});WriteReport();
                if(site!=null)break;
            }
            if(site==null){Unverified("heavy-salvage","No current safe committed source in the fixed eight seed64 assignments; no alternate seed or source is created.");yield break;}
            var loadState=new HaulOwner(site.Load,site.Zone);
            var supports=site.Hedges.Select(e=>new HaulOwner(e,site.Zone)).ToArray();
            var originalPlayer=Player;string gear=CookingGear(Player),inventory=PlayerSignature();int speed=Player.GetStatValue("Speed"),drams=TradeSystem.GetDrams(Player);
            yield return Transfer(site.Zone,site.Approach,"actual generated two-pull hauling shortcut");
            Require(ReferenceEquals(Player,originalPlayer)&&PlayerSignature()==inventory&&CookingGear(Player)==gear,"same original player and gear after disclosed hauling transfer");
            HaulCurrent(site,loadState,supports,site.Aperture,false);
            var bypass=HaulPath(Zone,site.Approach,site.Opposite,null,null);
            Require(bypass!=null&&bypass.Count>=14&&bypass.Count<=60,"current actual eight-direction bypass after entry");
            var predicted=HaulPath(Zone,site.FinalPlayer,site.Opposite,site.Load,site.Parked);
            Require(predicted!=null&&predicted.Count<=6&&bypass.Count>=4+predicted.Count+6,"actual parked-layout shortcut remains useful");
            Check("current_generated_hauling_geometry",Manager.Exploration.DispositionFor(Zone.ZoneID)==2&&site.Hedges.Length==12&&site.Aperture.BlocksMovement(Player)&&At==site.Approach);
            _observations.Add(new{phase="actual-hauling-geometry",zone=Zone.ZoneID,owner=site.Load.ID,blueprint=site.Load.BlueprintName,aperture=HaulXY(site.Aperture),approach=HaulXY(site.Approach),opposite=HaulXY(site.Opposite),parked=HaulXY(site.Parked),finalPlayer=HaulXY(site.FinalPlayer),hedges=supports.Select(s=>new{id=s.Owner.ID,position=new[]{s.Position.x,s.Position.y}}).ToArray(),initialBypassSteps=bypass.Count,initialBypass=bypass.Select(HaulXY).ToArray(),predictedClearSteps=predicted.Count,predictedClearPath=predicted.Select(HaulXY).ToArray(),boundary="Read-only equal-step eight-direction physical dry route; the long bypass is not walked. Prediction ignores only the exact load at its original cell and blocks its proposed parked cell."});
            var style=HaulVisual(site.Load,null,"initial-approved-haulable");
            yield return HaulCueCapture(site.Load,HandlingPart.HaulCommand,"01-generated-obstructed-shortcut");
            yield return HaulMenu(site.Load,false,"native-grab-original-source");
            HaulCurrent(site,loadState,supports,site.Aperture,true);
            int penalty=DragSystem.PenaltyFor(site.Load),applied=Math.Min(penalty,Math.Max(0,speed-DragSystem.MinimumHaulingSpeed));
            Check("native_grip_exact_penalty",Player.GetPart<DragPart>().SpeedPenalty==penalty&&Player.GetPart<DragPart>().AppliedPenalty==applied&&Player.GetStatValue("Speed")==speed-applied);
            int haulTick=Tick,haulEnergy=Energy;
            for(int n=1;n<=2;n++)
            {
                var oldPlayer=At;var next=Zone.GetCell(At.X-site.DX,At.Y-site.DY);
                Require(HaulDry(Zone,next,null,null,true),"current safe straight haul destination");
                yield return ExchangePaid(Tap(HaulKey(-site.DX,-site.DY)),"local","native-straight-haul-"+n);
                Check("native_pull_"+n+"_exact_follow",At==next&&Zone.GetEntityCell(site.Load)==oldPlayer&&Player.GetStatValue("Speed")==speed-applied);
                Check("native_pull_"+n+"_actual_movement_facing",site.Load.GetPart<RenderPart>().VisualFacing==HaulFacing(-site.DX,-site.DY));
                HaulCurrent(site,loadState,supports,oldPlayer,true);
                HaulVisual(site.Load,style,"held-after-pull-"+n);
            }
            Check("two_actual_pulls_reach_parking",At==site.FinalPlayer&&Zone.GetEntityCell(site.Load)==site.Parked);
            _observations.Add(new{phase="two-paid-haul-cost",beforeTick=haulTick,afterTick=Tick,beforeEnergy=haulEnergy,afterEnergy=Energy,speed=Player.GetStatValue("Speed"),paidSteps=2,work=(long)haulEnergy+(long)(Tick-haulTick)*Player.GetStatValue("Speed")-Energy});
            yield return HaulCueCapture(site.Load,HandlingPart.ReleaseCommand,"02-native-held-load-after-two-pulls");
            yield return HaulMenu(site.Load,true,"native-release-parked-source");
            HaulCurrent(site,loadState,supports,site.Parked,false);
            Check("native_release_refunds_only_grip",Player.GetStatValue("Speed")==speed&&Player.GetPart<DragPart>()==null&&site.Load.GetPart<DraggedPart>()==null&&!site.Aperture.BlocksMovement(Player));
            HaulVisual(site.Load,style,"released-parked-style");yield return HaulCueCapture(site.Load,HandlingPart.HaulCommand,"03-native-parked-load-and-open-gap");
            var clear=HaulPath(Zone,At,site.Opposite,null,null);
            Require(clear!=null&&clear.Count<=6&&clear.Contains(site.Aperture),"current short route actually crosses the cleared aperture");
            int clearTick=Tick,clearEnergy=Energy;
            foreach(var next in clear)
            {
                Require(HaulDry(Zone,next,null,null,true),"current safe short crossing cell");
                int dx=next.X-At.X,dy=next.Y-At.Y;
                Require(Math.Abs(dx)<=1&&Math.Abs(dy)<=1&&(dx!=0||dy!=0),"one real eight-direction step");
                yield return ExchangePaid(Tap(HaulKey(dx,dy)),"local","native-cleared-shortcut-step");
                Require(At==next,"native shortcut step reached actual cell");HaulCurrent(site,loadState,supports,site.Parked,false);
            }
            Check("native_cleared_shortcut_reaches_other_side",At==site.Opposite&&Player.GetStatValue("Speed")==speed&&CookingGear(Player)==gear&&TradeSystem.GetDrams(Player)==drams);
            _observations.Add(new{phase="actual-cleared-crossing",paidSteps=clear.Count,path=clear.Select(HaulXY).ToArray(),beforeTick=clearTick,afterTick=Tick,beforeEnergy=clearEnergy,afterEnergy=Energy,work=(long)clearEnergy+(long)(Tick-clearTick)*speed-Energy,initialMeasuredBypassSteps=bypass.Count,boundary="Only two haul steps plus this clear route were walked; no native matched-detour timing claim."});
            HaulVisual(site.Load,style,"cleared-crossing-current-style");yield return Capture("04-native-player-through-cleared-gap");
            yield return HaulCheckpoint(site,loadState,supports,style,speed);
            Check("bounded_original_hauling_aftermath",CookingGear(Player)==gear&&TradeSystem.GetDrams(Player)==drams&&Player.GetStatValue("Speed")==speed&&!DragSystem.IsDragging(Player));
        }
        static bool HaulBlueprint(Entity e)=>e!=null&&(e.BlueprintName=="FallenBeam"||e.BlueprintName=="HaulBarrel");
        bool HaulQualify(Zone zone,Entity load,out HaulSite site,out string reason)
        {
            site=null;reason="unsupported-current-load";var centre=zone.GetEntityCell(load);
            if(centre==null||load.SpatialZone!=zone||!centre.Objects.Contains(load)||load.HasPart<SpatialFootprintPart>()||load.HasPart<DragPart>()||load.HasPart<DraggedPart>()||load.Parts.Any(p=>p.ParentEntity!=load)||DragRules.CanDrag(Player,load)!=DragVerdict.Ok)return false;
            if(zone.GetReadOnlyEntities().Any(e=>e!=Player&&e.HasTag("Creature")&&e.GetStatValue("Hitpoints")>0&&FactionManager.IsHostile(e,Player)&&SpatialQuery.DistanceToCell(zone,e,centre.X,centre.Y)<=8))
            {reason="current-hostile-within-eight";return false;}
            foreach(var d in Directions)
            {
                int px=-d.y,py=d.x;var hedges=new List<Entity>();bool valid=true;
                for(int offset=-6;offset<=6;offset++)if(offset!=0)
                {
                    var c=zone.GetCell(centre.X+px*offset,centre.Y+py*offset);
                    var rows=c?.Occupants.Where(e=>e.BlueprintName=="Hedge"&&e.SpatialZone==zone&&zone.GetEntityCell(e)==c).ToArray();
                    if(rows==null||rows.Length!=1||rows[0].GetPart<DestructiblePart>()?.HP!=10||rows[0].GetPart<DestructiblePart>().Gone||rows[0].GetPart<PhysicsPart>()?.Solid!=true||rows[0].HasPart<DraggedPart>()||rows[0].HasPart<StatusEffectsPart>()||rows[0].Parts.Any(p=>p.ParentEntity!=rows[0])){valid=false;break;}
                    hedges.Add(rows[0]);
                }
                if(!valid||hedges.Distinct().Count()!=12)continue;
                var a=zone.GetCell(centre.X-d.x,centre.Y-d.y);var b=zone.GetCell(centre.X+d.x,centre.Y+d.y);
                var park=zone.GetCell(centre.X-2*d.x,centre.Y-2*d.y);var end=zone.GetCell(centre.X-3*d.x,centre.Y-3*d.y);
                if(!HaulDry(zone,a,null,null,true)||!HaulDry(zone,b,null,null,true)||!HaulDry(zone,park,null,null,true)||!HaulDry(zone,end,null,null,true))continue;
                var before=HaulPath(zone,a,b,null,null);var after=HaulPath(zone,end,b,load,park);
                if(before==null||before.Count<14||before.Count>60||after==null||after.Count>6||!after.Contains(centre)||before.Count<4+after.Count+6)continue;
                site=new HaulSite{Zone=zone,Load=load,Hedges=hedges.ToArray(),Aperture=centre,Approach=a,Opposite=b,Parked=park,FinalPlayer=end,DX=d.x,DY=d.y,Bypass=before,ClearPath=after};reason="admitted";return true;
            }
            reason="no-complete-useful-safe-straight-layout";return false;
        }
        bool HaulDry(Zone zone,Cell cell,Entity virtualRemoved,Cell virtualBlocked,bool threats)
        {
            if(cell==null||cell==virtualBlocked||cell.HasClosedArchiveBarrier())return false;
            foreach(var e in cell.Occupants)
            {
                if(e==Player||e==virtualRemoved)continue;
                if(e.HasTag("Creature")||e.HasTag("Solid")||e.GetPart<PhysicsPart>()?.Solid==true||e.GetPart<DoorPart>()?.IsClosed==true||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasPart<TriggerOnStepPart>()||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)return false;
            }
            var s=zone.TileState.Get(cell.X,cell.Y);
            if(s!=null&&(s.Heat>0||s.Cold>0||s.Charge>0||!string.IsNullOrEmpty(s.Cloud)||s.Coatings.Count>0))return false;
            return !threats||!zone.GetReadOnlyEntities().Any(e=>e!=Player&&e.HasTag("Creature")&&e.GetStatValue("Hitpoints")>0&&FactionManager.IsHostile(e,Player)&&SpatialQuery.DistanceToCell(zone,e,cell.X,cell.Y)<=3);
        }
        List<Cell> HaulPath(Zone zone,Cell start,Cell finish,Entity virtualRemoved,Cell virtualBlocked)
        {
            if(!HaulDry(zone,start,virtualRemoved,virtualBlocked,false)||!HaulDry(zone,finish,virtualRemoved,virtualBlocked,false))return null;
            var pending=new Queue<Cell>();var seen=new HashSet<Cell>{start};var from=new Dictionary<Cell,Cell>();pending.Enqueue(start);
            while(pending.Count>0)
            {
                var cell=pending.Dequeue();if(cell==finish){var result=new List<Cell>();while(cell!=start){result.Add(cell);cell=from[cell];}result.Reverse();return result;}
                foreach(var d in HaulDirections)
                {var next=zone.GetCell(cell.X+d.x,cell.Y+d.y);if(next==null||!seen.Add(next)||!HaulDry(zone,next,virtualRemoved,virtualBlocked,false))continue;from[next]=cell;pending.Enqueue(next);}
            }
            return null;
        }
        static Key HaulKey(int dx,int dy)
        {
            Require(Math.Abs(dx)<=1&&Math.Abs(dy)<=1&&(dx!=0||dy!=0),"nonzero adjacent hauling key");
            if(dx==0)return dy<0?Key.W:Key.S;if(dy==0)return dx<0?Key.A:Key.D;
            return dx<0?(dy<0?Key.Numpad7:Key.Numpad1):(dy<0?Key.Numpad9:Key.Numpad3);
        }
        static int[] HaulXY(Cell c)=>new[]{c.X,c.Y};
        static Part[] HaulParts(Entity e)=>e.Parts.Where(p=>!(p is DragPart)&&!(p is DraggedPart)).ToArray();
        static string HaulFacts(Entity e)
        {
            // No recursive entity serialization: pin authored primitive fields,
            // tags, properties and stats; membership/grip are checked separately.
            // Movement legitimately changes VisualFacing; validate that runtime
            // state against the actual pull direction and save snapshot instead.
            var parts=HaulParts(e).Select(p=>new{type=p.GetType().FullName,fields=p.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public).Where(f=>(f.FieldType.IsPrimitive||f.FieldType.IsEnum||f.FieldType==typeof(string))&&!(p is RenderPart&&f.Name==nameof(RenderPart.VisualFacing))).OrderBy(f=>f.Name,StringComparer.Ordinal).Select(f=>new{name=f.Name,value=Convert.ToString(f.GetValue(p),CultureInfo.InvariantCulture)}).ToArray()}).ToArray();
            return JsonConvert.SerializeObject(new{e.ID,e.BlueprintName,parts,tags=e.Tags.OrderBy(p=>p.Key,StringComparer.Ordinal).ToArray(),properties=e.Properties.OrderBy(p=>p.Key,StringComparer.Ordinal).ToArray(),stats=CookingStats(e)});
        }
        static EntityVisualFacing HaulFacing(int dx,int dy)
            =>Math.Abs(dx)>=Math.Abs(dy)&&dx!=0?(dx<0?EntityVisualFacing.West:EntityVisualFacing.East):(dy<0?EntityVisualFacing.North:EntityVisualFacing.South);
        void HaulCurrent(HaulSite site,HaulOwner load,HaulOwner[] supports,Cell loadCell,bool held)
        {
            Require(Zone==site.Zone&&load.Owner==site.Load&&CookingCurrent(site.Load)&&Zone.GetEntityCell(site.Load)==loadCell&&HaulFacts(site.Load)==load.Facts&&HaulParts(site.Load).SequenceEqual(load.Parts)&&load.Parts.All(p=>p.ParentEntity==site.Load),"same current original load, authored parts and expected anchor");
            Require(site.Load.GetPart<RenderPart>().VisualFacing==(loadCell==site.Aperture?load.Facing:HaulFacing(-site.DX,-site.DY)),"load facing matches initial state or actual pull direction");
            Require(site.Load.GetPart<PhysicsPart>().InInventory==null&&site.Load.GetPart<PhysicsPart>().Equipped==null,"load remains world cargo");
            foreach(var s in supports)Require(CookingCurrent(s.Owner)&&Zone.GetEntityPosition(s.Owner)==s.Position&&HaulFacts(s.Owner)==s.Facts&&s.Owner.GetPart<RenderPart>().VisualFacing==s.Facing&&HaulParts(s.Owner).SequenceEqual(s.Parts)&&s.Parts.All(p=>p.ParentEntity==s.Owner),"same unchanged original boundary owner");
            Require(held?ReferenceEquals(DragSystem.GetDragged(Player),site.Load)&&ReferenceEquals(DragSystem.GetDragger(site.Load),Player):Player.GetPart<DragPart>()==null&&site.Load.GetPart<DraggedPart>()==null,"exact reciprocal hauling state");
        }
        IEnumerator HaulMenu(Entity load,bool release,string label)
        {
            Require(State=="Normal"&&CookingCurrent(load)&&SpatialQuery.Distance(Zone,Player,load)==1,"current adjacent native haul menu");
            var at=At;var facing=load.GetPart<RenderPart>().VisualFacing;int tick=Tick,energy=Energy,world=WorldClock.CurrentTick;string before=HaulFacts(load),player=PlayerSignature(),marker=ExchangeMarker(label);
            var cell=Zone.GetEntityCell(load);yield return Tap(Key.C);Require(State=="AwaitingTalkDirection","real C direction prompt");yield return Tap(HaulKey(cell.X-at.X,cell.Y-at.Y));
            Require(State=="WorldActionMenuOpen","native directional owner menu");yield return SelectCurrentOwner(load);
            var menu=_input.WorldActionMenuUI;var actions=(List<InventoryAction>)Field(menu,"_actions");var shortcuts=(char[])Field(menu,"_shortcuts");string command=release?HandlingPart.ReleaseCommand:HandlingPart.HaulCommand,opposite=release?HandlingPart.HaulCommand:HandlingPart.ReleaseCommand;
            int index=actions.FindIndex(a=>a.Command==command);
            Require(ReferenceEquals(menu.SelectedTarget,load)&&index>=0&&actions[index].Key=='g'&&shortcuts[index]=='g'&&actions[index].Display==(release?"let go":"haul")&&!actions.Any(a=>a.Command==opposite),"exact target and exclusive native G hauling action");
            yield return Tap(Key.G);yield return Settled();var rows=ExchangeWindow(marker);
            Check(label+"_free_exact_native_command",State=="Normal"&&At==at&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&HaulFacts(load)==before&&load.GetPart<RenderPart>().VisualFacing==facing&&PlayerSignature()==player&&rows.Count(e=>e.Category=="drag"&&e.Kind==(release?"Released":"Grabbed")&&e.ActorId==Player.ID&&e.TargetId==load.ID)==1&&!rows.Any(e=>e.Category=="turn"&&e.Kind=="End"&&e.ActorId==Player.ID));
            _observations.Add(new{phase=label,owner=load.ID,command,beforeTick=tick,afterTick=Tick,beforeEnergy=energy,afterEnergy=Energy,rows});WriteReport();
        }
        IEnumerator HaulCueCapture(Entity load,string expectedCommand,string label)
        {
            Require(State=="Normal"&&CookingCurrent(load),"current Normal-state hauling cue observation");
            // Observe the normal renderer after LateUpdate; never force a redraw,
            // focus, menu action or a different nearby selection to obtain a cue.
            yield return new WaitForEndOfFrame();
            int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,speed=Player.GetStatValue("Speed");
            var facing=load.GetPart<RenderPart>().VisualFacing;var standing=At;var loadCell=Zone.GetEntityCell(load);var grip=Player.GetPart<DragPart>();var held=load.GetPart<DraggedPart>();
            string player=PlayerSignature(),facts=HaulFacts(load);
            var actual=_input.QueryWorldAffordance(Zone,Player);var cached=(WorldAffordance?)Field(_input.ZoneRenderer,"_worldAffordance");
            var draw=(WorldAffordanceRenderer)Field(_input.ZoneRenderer,"_worldAffordanceRenderer");
            string[] sidebar=HaulSidebarRows();
            bool selectedLoad=actual.HasValue&&ReferenceEquals(actual.Value.Target,load);
            bool submitted=draw!=null&&(actual.HasValue
                ?cached.HasValue&&ReferenceEquals(cached.Value.Target,actual.Value.Target)&&ReferenceEquals(cached.Value.Cell,actual.Value.Cell)
                    &&cached.Value.Command==actual.Value.Command&&cached.Value.Hint==actual.Value.Hint&&draw.IsVisible
                    &&draw.CurrentCell==new Vector2Int(actual.Value.Cell.X,actual.Value.Cell.Y)
                :!cached.HasValue&&!draw.IsVisible);
            bool hintSubmitted=!actual.HasValue||sidebar.Any(line=>line.Contains(actual.Value.Hint));
            _observations.Add(new{phase="normal-hauling-cue",label,state=State,load=load.ID,expectedCommand,selectedLoad,
                target=actual?.Target?.ID,blueprint=actual?.Target?.BlueprintName,command=actual?.Command,hint=actual?.Hint,
                cell=actual.HasValue?HaulXY(actual.Value.Cell):null,cachedTarget=cached?.Target?.ID,cachedCommand=cached?.Command,
                markerVisible=draw?.IsVisible,markerCell=draw==null?null:new[]{draw.CurrentCell.x,draw.CurrentCell.y},submitted,hintSubmitted,sidebarRows=sidebar,
                boundary="Actual Normal-state selection and submitted sidebar glyphs/corner state. Another normal-priority owner is permitted and reported; no focused query, Look rectangle or forced selection. Pixels and human readability require image review."});
            yield return Capture(label);
            Check(label+"_current_normal_cue_and_free_capture",submitted&&hintSubmitted&&(!selectedLoad||actual.Value.Command==expectedCommand)
                &&State=="Normal"&&At==standing&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&PlayerSignature()==player
                &&CookingCurrent(load)&&Zone.GetEntityCell(load)==loadCell&&HaulFacts(load)==facts&&load.GetPart<RenderPart>().VisualFacing==facing&&Player.GetStatValue("Speed")==speed
                &&ReferenceEquals(Player.GetPart<DragPart>(),grip)&&ReferenceEquals(load.GetPart<DraggedPart>(),held));
        }
        string[] HaulSidebarRows()
        {
            var map=_input.ZoneRenderer.SidebarTilemap;
            Require(map!=null&&map.gameObject.activeInHierarchy,"actual live sidebar tilemap");
            var bounds=map.cellBounds;var rows=new List<string>();
            for(int y=bounds.yMax-1;y>=bounds.yMin;y--)
            {
                var chars=new char[bounds.size.x];for(int i=0;i<chars.Length;i++)chars[i]=' ';
                for(int x=bounds.xMin;x<bounds.xMax;x++)
                {
                    var sprite=map.GetSprite(new Vector3Int(x,y,0));
                    if(sprite==null||!sprite.name.StartsWith("Text_",StringComparison.Ordinal))continue;
                    if(!int.TryParse(sprite.name.Substring(5),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out int code)||code<32||code>126)continue;
                    Require(ReferenceEquals(sprite,CP437TilesetGenerator.GetTextTile((char)code)?.sprite),"actual submitted sidebar text atlas");
                    chars[x-bounds.xMin]=(char)code;
                }
                string line=new string(chars).Trim();if(line.Length>0)rows.Add(line);
            }
            return rows.ToArray();
        }
        HaulStyle HaulVisual(Entity load,HaulStyle before,string label)
        {
            Require(CookingCurrent(load)&&Zone.GetEntityCell(load).IsVisible&&load.GetPart<RenderPart>().Visible&&Presenter.IsRenderedEntity(load),"current visible rendered haulable");
            Require(Presenter.TryGetApprovedStyle(load,out var proof)&&proof.Batched&&proof.PieceCount>0,"approved exact batched haulable geometry and palette");
            var recipe=SpawnRing3DRecipes.Resolve(Zone,load,(SpawnRing3DCatalog)Field(Presenter,"definition"));
            Require(recipe.Owner==load&&recipe.Batched&&recipe.ModelId==proof.ModelId&&recipe.Position==Village3DProjection.CellCentre(Zone.GetEntityCell(load).X,Zone.GetEntityCell(load).Y),"current batched recipe follows physical anchor");
            Require(load.BlueprintName=="FallenBeam"?proof.ModelId.StartsWith("ring-fallen-beam-",StringComparison.Ordinal):proof.ModelId=="ring-haul-barrel","authored original load family");
            var source=SpreadNativeStyle3DLibrary.Load()?.ForOwner(Zone,recipe);Require(source!=null&&source.Mesh==proof.ExpectedMesh,"approved persistent hauled model source");
            var materials=source.Materials??new[]{source.Material};Require(materials.Length==proof.PieceCount,"all source material pieces covered");
            var pieces=new List<object>();for(int i=0;i<proof.PieceCount;i++){var piece=proof.GetPiece(i);Require(piece.ExpectedMesh==source.Mesh&&piece.ExpectedMaterial==materials[i]&&piece.SubmittedMesh!=null&&piece.SubmittedMesh.vertexCount>0&&piece.SubmittedMaterial!=null,"nonempty actual submitted batch fragment");pieces.Add(new{slot=i,expectedMesh=piece.ExpectedMesh.name,submittedMesh=piece.SubmittedMesh.name,expectedMaterial=piece.ExpectedMaterial.name,submittedMaterial=piece.SubmittedMaterial.name});}
            if(before!=null)Require(before.Model==proof.ModelId&&before.Quarter==recipe.QuarterTurns&&before.Mesh==proof.ExpectedMesh&&before.Materials.SequenceEqual(materials),"same physical load retains authored model and palette");
            var camera=Presenter.WorldCamera;var point=camera.WorldToViewportPoint(recipe.Position);Require(point.z>0&&point.x>=0&&point.x<=1&&point.y>=0&&point.y<=1,"source anchor is on current world camera");
            _observations.Add(new{phase=label,owner=load.ID,blueprint=load.BlueprintName,position=HaulXY(Zone.GetEntityCell(load)),proof.ModelId,proof.Batched,recipe.QuarterTurns,pieces,viewport=new[]{point.x,point.y,point.z},boundary="Submitted batch proof covers this exact current contribution. Patch bounds are not isolated owner bounds. Human readability and unobscured pixels require image review."});WriteReport();
            return before??new HaulStyle{Model=proof.ModelId,Quarter=recipe.QuarterTurns,Mesh=proof.ExpectedMesh,Materials=materials.ToArray()};
        }
        IEnumerator HaulCheckpoint(HaulSite site,HaulOwner load,HaulOwner[] supports,HaulStyle style,int speed)
        {
            string zoneId=Zone.ZoneID,loadId=site.Load.ID;var departure=(At.X,At.Y);var oldZone=Zone;var oldLoad=site.Load;
            var loadFacing=site.Load.GetPart<RenderPart>().VisualFacing;var supportFacings=supports.ToDictionary(s=>s.Owner.ID,s=>s.Facing);
            string loadFacts=HaulFacts(site.Load);var supportFacts=supports.ToDictionary(s=>s.Owner.ID,s=>s.Facts);var supportPositions=supports.ToDictionary(s=>s.Owner.ID,s=>s.Position);var oldSupports=site.Hedges.ToArray();
            yield return ExchangePaid(Tap(Key.LeftShift,Key.Comma),"local","hauling-native-source-exit");Require(WorldMap.IsWorldMapZoneID(Zone.ZoneID)&&ReferenceEquals(Manager.CachedZones[zoneId],oldZone),"real inactive retained hauling source");
            Require(HaulFacts(oldLoad)==loadFacts&&oldLoad.GetPart<RenderPart>().VisualFacing==loadFacing&&oldZone.GetEntityPosition(oldLoad)==(site.Parked.X,site.Parked.Y),"source remains actually parked on exit");
            string playerFacts=PlayerSignature(),stats=CookingStats(Player),gear=CookingGear(Player);int tick=Tick,energy=Energy,world=WorldClock.CurrentTick;var mapAt=(At.X,At.Y);var oldPlayer=Player;var oldManager=Manager;var oldMap=Zone;
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"isolated native save metadata");string path=Path.Combine(SaveGameService.SaveRootOverride,info.GameID,"Quick.sav.gz"),before=PassageHash(path);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);yield return Settled();string saved=PassageHash(path);
            Check("save_inactive_parked_hauling_graph",saved!=before&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."&&SaveGameService.GetSaveInfo("Quick").ActiveZoneID==Zone.ZoneID&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&HaulFacts(oldLoad)==loadFacts&&oldLoad.GetPart<RenderPart>().VisualFacing==loadFacing);
            var next=Directions.Select(d=>Zone.GetCell(At.X+d.x,At.Y+d.y)).FirstOrDefault(c=>c!=null&&c.IsPassable());Require(next!=null,"one ordinary unsaved world-map step");
            yield return ExchangePaid(Tap(HaulKey(next.X-At.X,next.Y-At.Y)),"map","hauling-native-unsaved-map-step");
            Check("inactive_parked_source_stays_unchanged",At==next&&(Tick!=tick||Energy!=energy)&&PassageHash(path)==saved&&HaulFacts(oldLoad)==loadFacts&&oldLoad.GetPart<RenderPart>().VisualFacing==loadFacing&&oldZone.GetEntityPosition(oldLoad)==(site.Parked.X,site.Parked.Y));
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;while(ReferenceEquals(Player,oldPlayer)){Require(Time.realtimeSinceStartupAsDouble-began<8,"native hauling F6 replaces player graph");yield return null;}yield return Settled();
            Require(Manager.CachedZones.TryGetValue(zoneId,out var restoredZone),"saved inactive hauling graph restored without generation");
            var restoredLoad=restoredZone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==loadId);var restoredSupports=oldSupports.Select(e=>restoredZone.GetReadOnlyEntities().SingleOrDefault(v=>v.ID==e.ID)).ToArray();
            Check("load_exact_parked_replacement_and_player",Manager!=oldManager&&Zone!=oldMap&&restoredZone!=oldZone&&restoredLoad!=null&&restoredLoad!=oldLoad&&restoredZone.GetEntityPosition(restoredLoad)==(site.Parked.X,site.Parked.Y)&&HaulFacts(restoredLoad)==loadFacts&&restoredLoad.GetPart<RenderPart>().VisualFacing==loadFacing&&restoredSupports.All(e=>e!=null&&!oldSupports.Contains(e)&&HaulFacts(e)==supportFacts[e.ID]&&e.GetPart<RenderPart>().VisualFacing==supportFacings[e.ID]&&restoredZone.GetEntityPosition(e)==supportPositions[e.ID])&&Player.ID==oldPlayer.ID&&(At.X,At.Y)==mapAt&&PlayerSignature()==playerFacts&&CookingStats(Player)==stats&&CookingGear(Player)==gear&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&PassageHash(path)==saved&&Manager.Exploration.DispositionFor(zoneId)==2&&Player.GetPart<DragPart>()==null&&restoredLoad.GetPart<DraggedPart>()==null&&Player.GetStatValue("Speed")==speed);
            yield return ExchangePaid(Tap(Key.LeftShift,Key.Period),"local","hauling-native-restored-source-return");
            Require(ReferenceEquals(Zone,restoredZone)&&(At.X,At.Y)==departure,"ordinary native return restores the actual departed cell");
            site.Zone=restoredZone;site.Load=restoredLoad;site.Hedges=restoredSupports;site.Aperture=Zone.GetCell(site.Aperture.X,site.Aperture.Y);site.Approach=Zone.GetCell(site.Approach.X,site.Approach.Y);site.Opposite=Zone.GetCell(site.Opposite.X,site.Opposite.Y);site.Parked=Zone.GetCell(site.Parked.X,site.Parked.Y);site.FinalPlayer=Zone.GetCell(site.FinalPlayer.X,site.FinalPlayer.Y);
            var nowLoad=new HaulOwner(restoredLoad,Zone);var nowSupports=restoredSupports.Select(e=>new HaulOwner(e,Zone)).ToArray();HaulCurrent(site,nowLoad,nowSupports,site.Parked,false);
            Check("native_return_keeps_parked_clear_shortcut",HaulFacts(restoredLoad)==loadFacts&&restoredLoad.GetPart<RenderPart>().VisualFacing==loadFacing&&restoredSupports.All(e=>HaulFacts(e)==supportFacts[e.ID]&&e.GetPart<RenderPart>().VisualFacing==supportFacings[e.ID]&&Zone.GetEntityPosition(e)==supportPositions[e.ID])&&!site.Aperture.BlocksMovement(Player)&&Zone.GetReadOnlyEntities().Count(e=>e.ID==loadId)==1&&!Presenter.TryGetApprovedStyle(oldLoad,out _));
            HaulVisual(restoredLoad,style,"loaded-parked-current-style");yield return Capture("05-loaded-parked-source-and-open-shortcut");
        }
    }
}
