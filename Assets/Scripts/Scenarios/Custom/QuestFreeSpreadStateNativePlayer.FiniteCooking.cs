using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class QuestFreeSpreadStateNativePlayer
    {
        const string FiniteCookingBoundary="Seed64, up to eight canonical v6 CoolingWorkPatch assignments, one actual committed source. One disclosed original-player setup transfer to the actual west border; native cardinal approach/Harvest/Cook/Wait/map exit/F5/map step/F6/return. No ingredient, source, heat/fuel, actor, RNG, time or visibility edits. 80 paid inputs/150 seconds; no repeat seed or forced success. Local material passes are measured separately from scheduler ticks. Automatic cooled mesh is checked before menu/move/refresh. Inactive saved heat does not imply away cooling. Images require human review.";
        const string CoalsBlueprint="SpreadCookingCoals",CoalsHot="spread-cooking-coals-hot",CoalsCooled="spread-cooking-coals-cooled";
        Entity _finiteSource,_finiteRow;Part[] _finiteSourceParts,_finiteRowParts;Cell _finiteAnchor,_finiteRowAnchor;int _finitePasses;
        IEnumerator FiniteCooking()
        {
            Require(Manager.Exploration.Version==6&&CarriedUnits("Emberwheat")==0&&CarriedUnits(CookingOutput)==0,"fresh v6 and no raw/prepared grain");
            string originalGear=CookingGear(Player);int originalDrams=TradeSystem.GetDrams(Player);Zone site=null;Cell entryCell=null,harvestStand=null,cookStand=null;
            var entries=Manager.Exploration.Entries.Where(e=>e.PlacementEligible&&e.Family.ToString()=="CoolingWorkPatch").OrderBy(e=>e.ZoneID,StringComparer.Ordinal).Take(8).ToArray();
            foreach(var entry in entries)
            {
                var z=Manager.GetZone(entry.ZoneID);if(z==null){_observations.Add(new{phase="finite-source-unavailable",zone=entry.ZoneID});continue;}
                var sources=z.GetReadOnlyEntities().Where(e=>e.BlueprintName==CoalsBlueprint).ToArray();
                var plan=SpreadCompositionPlan.Create(entry.ZoneID,Manager.WorldSeed,Formation.FieldStrips,entry.Topology);var port=z.GetCell(0,plan.WestY);
                bool admitted=false;Entity chosen=null;Cell hs=null,cs=null;int cost=-1;
                if(Manager.Exploration.DispositionFor(entry.ZoneID)==2&&sources.Length==1&&FiniteAuthoredSource(z,sources[0])&&DrySafe(z,port,null))
                {
                    var walkMask=FiniteWalkMask(z,sources[0]);
                    foreach(var row in z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="RipeCropRow").OrderBy(e=>z.GetEntityPosition(e).y).ThenBy(e=>z.GetEntityPosition(e).x))
                    {
                        var field=row.GetPart<FieldHarvestPart>();var rowCell=z.GetEntityCell(row);var sourceCell=z.GetEntityCell(sources[0]);
                        if(field?.ParentEntity!=row||field.Harvested||field.YieldBlueprint!="Emberwheat"||field.YieldCount!=1||rowCell==null||CoalsDistance(rowCell,sourceCell)<2||CoalsDistance(rowCell,sourceCell)>5)continue;
                        foreach(var h in FiniteAdjacent(z,rowCell,sources[0]))
                        {
                            var first=FinitePath(z,port,h,sources[0],walkMask);if(first==null)continue;
                            foreach(var c in FiniteAdjacent(z,sourceCell,sources[0]))
                            {
                                var last=FinitePath(z,h,c,sources[0],walkMask);if(last==null||last.Count>8||first.Count+last.Count+3>60)continue;
                                if(cost<0||first.Count+last.Count<cost){cost=first.Count+last.Count;chosen=row;hs=h;cs=c;}
                            }
                        }
                    }
                    admitted=chosen!=null;
                }
                _observations.Add(new{phase="finite-source-selection",zone=entry.ZoneID,disposition=Manager.Exploration.DispositionFor(entry.ZoneID),sources=sources.Select(e=>e.ID).ToArray(),row=chosen?.ID,westPort=new[]{port.X,port.Y},predictedMaterialPasses=cost<0?-1:cost+3,admitted});
                if(!admitted)continue;site=z;entryCell=port;_finiteSource=sources[0];_finiteRow=chosen;harvestStand=hs;cookStand=cs;break;
            }
            if(site==null){Unverified("finite-cooking","No actual committed useful source in the fixed eight seed64 assignments; no alternate seed, row or station is created.");yield break;}
            if(ControlledFinite)Require(site.ZoneID=="Overworld.5.11.0"&&entryCell.X==0&&entryCell.Y==16,"same first admitted controlled source and original west entry");
            string initialSource=FiniteFacts(site,_finiteSource),initialRow=FiniteRowFacts(site,_finiteRow);var originalPlayer=Player;
            yield return Transfer(site,entryCell,"actual finite cooking west-border entry");
            Require(ReferenceEquals(Player,originalPlayer)&&FiniteFacts(site,_finiteSource)==initialSource&&FiniteRowFacts(site,_finiteRow)==initialRow,"setup transfer preserves exact finite heat/fuel/row");
            BindFiniteOwners();Check("generated_finite_entry_and_original_owners",FiniteAuthoredSource(site,_finiteSource)&&!_finiteRow.GetPart<FieldHarvestPart>().Harvested&&Manager.Exploration.DispositionFor(site.ZoneID)==2);
            if(ControlledFinite)SuspendFiniteNpcTurns();
            yield return Capture("01-generated-west-entry");yield return FiniteWalk(harvestStand,"west-to-original-grain");
            Require(_finiteRowAnchor.IsVisible&&SpatialQuery.Distance(Zone,Player,_finiteRow)==1,"current visible actual harvest source");
            string rawRowModel=Model(_finiteRow);int ground=CookingGroundUnits(Zone,"Emberwheat");var beforeItems=Player.GetPart<InventoryPart>().Objects.ToArray();
            yield return FinitePaid(CookingWorldAction(_finiteRow,"Harvest"),"finite-original-row-harvest");
            var raw=Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e=>e.BlueprintName=="Emberwheat");var harvest=_exchangeLastWindow.Where(e=>e.Category=="loot"&&e.Kind=="FieldHarvested"&&e.ActorId==Player.ID&&e.TargetId==_finiteRow.ID).ToArray();
            Check("native_finite_row_one_earned_unit",harvest.Length==1&&Payload(harvest[0],"yield")=="Emberwheat"&&Payload(harvest[0],"count")=="1"&&Payload(harvest[0],"dropped")=="0"&&_finiteRow.GetPart<FieldHarvestPart>().Harvested&&raw!=null&&!beforeItems.Contains(raw)&&CookingOwns(raw)&&CookingUnits(raw)==1&&CookingGroundUnits(Zone,"Emberwheat")==ground&&Model(_finiteRow)==rawRowModel.Replace("grain-","stubble-"));
            yield return FiniteWalk(cookStand,"earned-grain-to-coals");Require(ReferenceEquals(FiniteSelectedStation(),_finiteSource),"exact current selected finite station before Cook");
            FiniteVisual(CoalsHot,"ready-before-menu");yield return FiniteRead("02-hot-ready-examine","Use carried raw food's Cook action",true);
            var rawId=raw.ID;float beforeCook=_finiteSource.GetPart<ThermalPart>().Temperature;
            yield return FinitePaid(CookingInventoryAction(raw,"Cook"),"native-finite-cook");
            var cooked=_exchangeLastWindow.Where(e=>e.Category=="event"&&e.Kind=="FoodCooked"&&e.ActorId==Player.ID&&e.TargetId==rawId).ToArray();
            var food=Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e=>e.BlueprintName==CookingOutput);
            Check("native_hot_source_one_for_one_cook",beforeCook>=CookingService.MinimumFiniteCookingTemperature&&cooked.Length==1&&Payload(cooked[0],"station")==CoalsBlueprint&&Payload(cooked[0],"from")=="Emberwheat"&&Payload(cooked[0],"into")==CookingOutput&&Payload(cooked[0],"count")=="1"&&food!=null&&food!=raw&&CookingOwns(food)&&CookingUnits(food)==1&&CookingUnits(raw)==0&&!Player.GetPart<InventoryPart>().Objects.Contains(raw)&&CarriedUnits("Emberwheat")==0&&CookingGear(Player)==originalGear&&TradeSystem.GetDrams(Player)==originalDrams&&_finitePasses+1<=60);
            yield return Capture("03-earned-food-cooked-while-ready");var standing=At;
            while(_finiteSource.GetPart<ThermalPart>().Temperature>=CookingService.MinimumFiniteCookingTemperature)
            {
                Require(At==standing&&DrySafe(Zone,At,null),"current safe stationary cooling wait");
                yield return FinitePaid(Tap(Key.Period),"ordinary-residual-cooling-wait");Require(At==standing,"ordinary wait never moved player");
                // Inspect before any menu, focus, movement, dirty request or explicit Refresh.
                if(_finiteSource.GetPart<ThermalPart>().Temperature<CookingService.MinimumFiniteCookingTemperature)FiniteVisual(CoalsCooled,"first-normal-threshold-crossing");
            }
            Check("native_paid_cooling_current_mesh",_finitePasses==67&&_finiteSource.GetPart<FuelPart>().FuelMass==25&&At==standing&&_finiteRow.GetPart<FieldHarvestPart>().Harvested);
            yield return Capture("04-normal-paid-cooling-threshold");yield return FiniteRead("05-cooled-unavailable-examine","no longer hot enough",true);
            Check("cooled_station_no_longer_selected",FiniteSelectedStation()==null&&CookingOwns(food)&&CookingUnits(food)==1);
            if(ControlledFinite)RestoreFiniteNpcTurns(true);
            yield return FiniteCheckpoint(food);
            Check("one_source_one_food_no_grants",CookingGear(Player)==originalGear&&TradeSystem.GetDrams(Player)==originalDrams&&CarriedUnits(CookingOutput)==1&&CarriedUnits("Emberwheat")==0&&Zone.GetReadOnlyEntities().Count(e=>e.BlueprintName==CoalsBlueprint)==1&&_finiteRow.GetPart<FieldHarvestPart>().Harvested);
        }
        static int CoalsDistance(Cell a,Cell b)=>Math.Max(Math.Abs(a.X-b.X),Math.Abs(a.Y-b.Y));
        bool FiniteAuthoredSource(Zone z,Entity e)
        {
            var c=e?.GetPart<CampfirePart>();var t=e?.GetPart<ThermalPart>();var f=e?.GetPart<FuelPart>();var p=e?.GetPart<PhysicsPart>();var r=e?.GetPart<RenderPart>();
            return e?.BlueprintName==CoalsBlueprint&&e.SpatialZone==z&&z.GetEntityCell(e)!=null&&c?.ParentEntity==e&&c.FiniteCooking&&!(bool)Field(c,"AllowRest")&&t?.ParentEntity==e&&t.Temperature==500&&t.AmbientTemperature==25&&t.AmbientDecayRate==.02f&&f?.ParentEntity==e&&f.FuelMass==25&&p?.ParentEntity==e&&!p.Solid&&!p.Takeable&&p.InInventory==null&&p.Equipped==null&&r?.ParentEntity==e&&r.Visible&&e.Parts.Count==6&&e.Parts.All(v=>v.ParentEntity==e)&&!e.HasPart<StatusEffectsPart>()&&!e.HasPart<LightSourcePart>()&&!e.HasPart<LightSourceFlickerPart>();
        }
        void BindFiniteOwners(){_finiteSourceParts=_finiteSource.Parts.ToArray();_finiteRowParts=_finiteRow.Parts.ToArray();_finiteAnchor=Zone.GetEntityCell(_finiteSource);_finiteRowAnchor=Zone.GetEntityCell(_finiteRow);}
        void FiniteCurrent()
        {Require(CookingCurrent(_finiteSource)&&CookingCurrent(_finiteRow)&&Zone.GetEntityCell(_finiteSource)==_finiteAnchor&&Zone.GetEntityCell(_finiteRow)==_finiteRowAnchor&&_finiteSource.Parts.SequenceEqual(_finiteSourceParts)&&_finiteRow.Parts.SequenceEqual(_finiteRowParts)&&_finiteSourceParts.All(p=>p.ParentEntity==_finiteSource)&&_finiteRowParts.All(p=>p.ParentEntity==_finiteRow)&&_finiteSource.GetPart<CampfirePart>().FiniteCooking&&!(bool)Field(_finiteSource.GetPart<CampfirePart>(),"AllowRest")&&!_finiteSource.HasPart<StatusEffectsPart>(),"same current finite source/row/parts/anchors without effects");}
        static string FiniteFacts(Zone z,Entity e)
        {var t=e.GetPart<ThermalPart>();var f=e.GetPart<FuelPart>();var c=e.GetPart<CampfirePart>();return e.ID+":"+e.BlueprintName+":"+z.GetEntityPosition(e)+":"+t.Temperature.ToString("R",CultureInfo.InvariantCulture)+":"+t.AmbientTemperature+":"+t.AmbientDecayRate+":"+f.FuelMass.ToString("R",CultureInfo.InvariantCulture)+":"+f.MaxFuel+":"+c.FiniteCooking+":"+Field(c,"AllowRest")+":"+string.Join(",",e.Parts.Select(p=>p.GetType().FullName));}
        static string FiniteRowFacts(Zone z,Entity e)=>e.ID+":"+e.BlueprintName+":"+z.GetEntityPosition(e)+":"+e.GetPart<FieldHarvestPart>().Harvested+":"+e.GetPart<FieldHarvestPart>().YieldBlueprint+":"+e.GetPart<FieldHarvestPart>().YieldCount;
        Entity FiniteSelectedStation(){var proof=typeof(CookingService).GetMethod("FindStation",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Player,Zone});return proof==null?null:(Entity)Field(proof,"Owner");}
        IEnumerable<Cell> FiniteAdjacent(Zone z,Cell cell,Entity source)=>Directions.Select(d=>z.GetCell(cell.X+d.x,cell.Y+d.y)).Where(c=>FinitePassable(z,c,source));
        bool FinitePassable(Zone z,Cell cell,Entity source)=>cell!=null&&cell!=z.GetEntityCell(source)&&DrySafe(z,cell,null);
        bool[,] FiniteWalkMask(Zone z,Entity source)
        {
            var threats=z.GetReadOnlyEntities().Where(e=>e!=Player&&e.HasTag("Creature")&&e.GetStatValue("Hitpoints")>0&&FactionManager.IsHostile(e,Player)).ToArray();
            var mask=new bool[Zone.Width,Zone.Height];var anchor=z.GetEntityCell(source);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                var c=z.GetCell(x,y);if(c==anchor||!z.CanPlaceFootprint(Player,x,y)||c.Occupants.Any(e=>e!=Player&&(e.HasTag("Creature")||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasPart<TriggerOnStepPart>()||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)))continue;
                var state=z.TileState.Get(x,y);if(state!=null&&(state.Heat>0||state.Cold>0||state.Charge>0||!string.IsNullOrEmpty(state.Cloud)||state.Coatings.Count>0))continue;
                mask[x,y]=!threats.Any(e=>SpatialQuery.DistanceToCell(z,e,x,y)<=3);
            }
            return mask;
        }
        List<Cell> FinitePath(Zone z,Cell start,Cell end,Entity source,bool[,] mask=null)
        {
            mask??=FiniteWalkMask(z,source);
            if(start==null||end==null||!mask[start.X,start.Y]||!mask[end.X,end.Y])return null;
            var q=new Queue<Cell>();var seen=new HashSet<Cell>{start};var parent=new Dictionary<Cell,Cell>();q.Enqueue(start);
            while(q.Count>0){var c=q.Dequeue();if(c==end){var path=new List<Cell>();while(c!=start){path.Add(c);c=parent[c];}path.Reverse();return path;}foreach(var d in Directions){var next=z.GetCell(c.X+d.x,c.Y+d.y);if(next==null||!seen.Add(next)||!mask[next.X,next.Y])continue;parent[next]=c;q.Enqueue(next);}}return null;
        }
        IEnumerator FiniteWalk(Cell destination,string label)
        {
            int replans=0;var path=FinitePath(Zone,At,destination,_finiteSource);Require(path!=null,"actual finite cardinal path "+label);
            while(At!=destination)
            {
                if(path.Count==0||!FinitePassable(Zone,path[0],_finiteSource)){Require(++replans<=6,"six finite current-route replans");path=FinitePath(Zone,At,destination,_finiteSource);Require(path!=null&&path.Count>0,"current route blocked "+label);}
                var next=path[0];path.RemoveAt(0);Require(Math.Abs(next.X-At.X)+Math.Abs(next.Y-At.Y)==1,"actual next cardinal cell");
                yield return FinitePaid(Tap(Direction(next.X-At.X,next.Y-At.Y)),label);Require(At==next,"native requested step occurred "+label);
            }
        }
        IEnumerator FinitePaid(IEnumerator action,string label)
        {
            if(ControlledFinite)ValidateFiniteSuspension("before-"+label);
            FiniteCurrent();var t=_finiteSource.GetPart<ThermalPart>();float before=t.Temperature,fuel=_finiteSource.GetPart<FuelPart>().FuelMass;yield return CookingPaid(action,label);_finitePasses++;FiniteCurrent();
            if(ControlledFinite)ObserveControlledPaid(label);
            float expected=before-(before-t.AmbientTemperature)*t.AmbientDecayRate;if(Math.Abs(expected-t.AmbientTemperature)<.5f)expected=t.AmbientTemperature;
            _observations.Add(new{phase="finite-material-pass",label,pass=_finitePasses,before,actual=t.Temperature,expected,fuel=_finiteSource.GetPart<FuelPart>().FuelMass,position=new[]{At.X,At.Y}});
            Require(Math.Abs(t.Temperature-expected)<.002f&&_finiteSource.GetPart<FuelPart>().FuelMass==fuel,"one actual native material decay without fuel burn "+label);
        }
        void FiniteVisual(string expected,string phase)
        {
            Presenter.TryGetEntityView(_finiteSource,out var root,out var model);var mesh=root?.GetComponent<MeshFilter>()?.sharedMesh;
            _observations.Add(new{phase,owner=_finiteSource.ID,temperature=_finiteSource.GetPart<ThermalPart>().Temperature,fuel=_finiteSource.GetPart<FuelPart>().FuelMass,expected,actual=model,mesh=mesh?.name,state=State,pass=_finitePasses});WriteReport();
            var actual=Visual(_finiteSource,expected,1);var entry=SpreadCookingCoalsLibrary.Load()?.Find(expected);
            Require(entry!=null&&Presenter.TryGetApprovedStyle(_finiteSource,out var proof)
                &&proof.ExpectedMesh==entry.Mesh&&proof.ExpectedMaterial==entry.Material&&entry.Material==ReferenceGladeVoxelLibrary.Load().Material
                &&actual.GetComponent<MeshFilter>()?.sharedMesh==proof.ExpectedMesh&&actual.GetComponent<Renderer>()?.sharedMaterial==proof.SubmittedMaterial,
                "bound exact current approved coals source mesh and owned submitted palette");
        }
        IEnumerator FiniteRead(string label,string phrase,bool menu)
        {
            FiniteCurrent();string source=FiniteFacts(Zone,_finiteSource),row=FiniteRowFacts(Zone,_finiteRow),player=PlayerSignature();int tick=Tick,energy=Energy,world=WorldClock.CurrentTick;
            if(menu){yield return Focus(_finiteSource);yield return Tap(Key.Enter);Require(State=="WorldActionMenuOpen","current coals menu");yield return SelectCurrentOwner(_finiteSource);var actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");Require(!actions.Any(a=>a.Command=="RestAtCampfire"||a.Command=="RestUntilNextBand"),"finite coals offer no resting action");yield return CloseNormal();}
            yield return Examine(_finiteSource,label,phrase);FiniteCurrent();
            Check(label+"_exact_heat_fuel_and_clock_free",FiniteFacts(Zone,_finiteSource)==source&&FiniteRowFacts(Zone,_finiteRow)==row&&PlayerSignature()==player&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world);
        }
        IEnumerator FiniteCheckpoint(Entity food)
        {
            var sourceZone=Zone;string zoneId=Zone.ZoneID,sourceId=_finiteSource.ID,rowId=_finiteRow.ID,foodId=food.ID;var departure=(At.X,At.Y);
            yield return ExchangePaid(Tap(Key.LeftShift,Key.Comma),"local","finite-native-source-exit");Require(WorldMap.IsWorldMapZoneID(Zone.ZoneID)&&ReferenceEquals(Manager.CachedZones[zoneId],sourceZone),"actual inactive retained source graph");
            string sourceFacts=FiniteFacts(sourceZone,_finiteSource),rowFacts=FiniteRowFacts(sourceZone,_finiteRow),playerFacts=PlayerSignature(),gear=CookingGear(Player),stats=CookingStats(Player);int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,drams=TradeSystem.GetDrams(Player);var mapAt=(At.X,At.Y);var oldPlayer=Player;var oldManager=Manager;var oldMap=Zone;var oldSource=_finiteSource;var oldRow=_finiteRow;
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"isolated native save metadata");string path=Path.Combine(SaveGameService.SaveRootOverride,info.GameID,"Quick.sav.gz"),before=PassageHash(path);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);yield return Settled();string saved=PassageHash(path);
            Check("save_inactive_spent_cooled_source",saved!=before&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."&&SaveGameService.GetSaveInfo("Quick").ActiveZoneID==Zone.ZoneID&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&FiniteFacts(sourceZone,_finiteSource)==sourceFacts&&FiniteRowFacts(sourceZone,_finiteRow)==rowFacts);
            var step=Directions.Select(d=>Zone.GetCell(At.X+d.x,At.Y+d.y)).FirstOrDefault(c=>c!=null&&c.IsPassable());Require(step!=null,"one real map mutation cell");
            yield return ExchangePaid(Tap(Direction(step.X-At.X,step.Y-At.Y)),"map","finite-native-unsaved-map-step");
            Check("inactive_source_does_not_cool_on_map_step",At==step&&(Tick!=tick||Energy!=energy)&&PassageHash(path)==saved&&FiniteFacts(sourceZone,_finiteSource)==sourceFacts&&FiniteRowFacts(sourceZone,_finiteRow)==rowFacts&&CookingOwns(food));
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;while(ReferenceEquals(Player,oldPlayer)){Require(Time.realtimeSinceStartupAsDouble-began<8,"native finite F6 replaces graph");yield return null;}yield return Settled();
            Require(Manager.CachedZones.TryGetValue(zoneId,out var restoredZone),"inactive source restored without generation");
            var restoredSource=restoredZone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==sourceId);var restoredRow=restoredZone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==rowId);var restoredFood=Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e=>e.ID==foodId);
            Check("load_exact_inactive_source_row_food_and_player",Manager!=oldManager&&Zone!=oldMap&&restoredZone!=sourceZone&&restoredSource!=null&&restoredSource!=oldSource&&restoredRow!=null&&restoredRow!=oldRow&&restoredFood!=null&&restoredFood!=food&&restoredFood.BlueprintName==CookingOutput&&CookingOwns(restoredFood)&&CookingUnits(restoredFood)==1&&FiniteFacts(restoredZone,restoredSource)==sourceFacts&&FiniteRowFacts(restoredZone,restoredRow)==rowFacts&&Manager.Exploration.DispositionFor(zoneId)==2&&Player.ID==oldPlayer.ID&&(At.X,At.Y)==mapAt&&PlayerSignature()==playerFacts&&CookingGear(Player)==gear&&CookingStats(Player)==stats&&TradeSystem.GetDrams(Player)==drams&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&PassageHash(path)==saved&&Manager.CachedZones.Values.Sum(z=>z.GetReadOnlyEntities().Count(e=>e.ID==foodId))==0);
            if(ControlledFinite)ObserveControlledInactiveLoad(restoredZone);
            float temperature=restoredSource.GetPart<ThermalPart>().Temperature;yield return ExchangePaid(Tap(Key.LeftShift,Key.Period),"local","finite-native-restored-return");
            if(ControlledFinite)ObserveControlledReturn(restoredZone);
            _finiteSource=restoredSource;_finiteRow=restoredRow;BindFiniteOwners();FiniteCurrent();var thermal=_finiteSource.GetPart<ThermalPart>();float expected=temperature-(temperature-thermal.AmbientTemperature)*thermal.AmbientDecayRate;
            Check("return_same_saved_cooled_spent_graph",ReferenceEquals(Zone,restoredZone)&&(At.X,At.Y)==departure&&Math.Abs(thermal.Temperature-expected)<.002f&&thermal.Temperature<CookingService.MinimumFiniteCookingTemperature&&_finiteSource.GetPart<FuelPart>().FuelMass==25&&_finiteRow.GetPart<FieldHarvestPart>().Harvested&&CookingOwns(restoredFood));
            FiniteVisual(CoalsCooled,"loaded-cooled-normal-return");yield return Capture("06-loaded-cooled-source-and-spent-row");yield return FiniteRead("07-loaded-cooled-readout","no longer hot enough",false);
        }
    }
}
