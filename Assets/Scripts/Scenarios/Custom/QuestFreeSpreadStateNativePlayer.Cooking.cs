using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class QuestFreeSpreadStateNativePlayer
    {
        const string CookingOutput="ToastedEmberwheat";
        const string CookingModel="spread-toasted-emberwheat";
        const string CookingBoundary="Seed64, at most eight canonical eligible FieldStrips addresses. Real generated ripe row and existing authored Sill campfire; two disclosed original-player setup transfers. Native Harvest/Cook/drop/Take and F5/Take/step/F6 replacement graph. No ingredient, station, actor, stats, source rates, RNG or time grants. Six paid inputs maximum;150seconds. This is a local legacy-campfire cooking and prepared-food persistence witness, not ordinary discovery/travel or finite residual-coal station lifetime. Approved world model/bounds require separately reviewed pixels.";

        void BeginCookingDiagnostics()
        {
            // The existing shared cleanup restores every captured channel, including absence.
            foreach(var key in new[]{"loot","event"})
            { _exchangeOldChannels[key]=_exchangeChannelStore.TryGetValue(key,out var old)?(bool?)old:null;Diag.SetChannel(key,true); }
        }
        IEnumerator Cooking()
        {
            Require(CarriedUnits("Emberwheat")==0&&CarriedUnits(CookingOutput)==0,"no initial harvested or prepared grain");
            string initialStats=CookingStats(Player),initialGear=CookingGear(Player);int initialDrams=TradeSystem.GetDrams(Player);
            Zone field=null;Entity row=null;Cell arrival=null;
            var entries=Manager.Exploration.Entries.Where(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.FieldStrips)
                .OrderBy(e=>e.ZoneID,StringComparer.Ordinal).Take(8).ToArray();
            foreach(var entry in entries)
            {
                var z=Manager.GetZone(entry.ZoneID);if(z==null){_observations.Add(new{phase="cooking-field-unavailable",zone=entry.ZoneID});continue;}
                foreach(var candidate in z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="RipeCropRow").OrderBy(e=>e.ID,StringComparer.Ordinal))
                {
                    var crop=candidate.GetPart<FieldHarvestPart>();var at=z.GetEntityCell(candidate);
                    if(crop?.Harvested!=false||crop.YieldBlueprint!="Emberwheat"||crop.YieldCount!=1||crop.ParentEntity!=candidate||at==null)continue;
                    var near=CellsNear(z,at,1,1).FirstOrDefault(c=>DrySafe(z,c,null)&&AIHelpers.HasLineOfSight(z,c.X,c.Y,at.X,at.Y));
                    if(near==null)continue;field=z;row=candidate;arrival=near;break;
                }
                Selection(entry,z,row,arrival,"actual unspent single-unit Emberwheat row and current safe adjacent visible approach");if(row!=null)break;
            }
            if(row==null){Unverified("cooking","No admitted actual ripe row in the fixed eight seed64 FieldStrips addresses.");yield break;}
            yield return Transfer(field,arrival,"actual generated ripe Emberwheat row");
            var cropPart=row.GetPart<FieldHarvestPart>();var rowAt=Zone.GetEntityCell(row);string rowId=row.ID,fieldId=field.ZoneID;
            Require(rowAt.IsVisible&&cropPart.Harvested==false&&SpatialQuery.Distance(Zone,Player,row)<=1,"current visible unspent harvest source");
            int groundBefore=CookingGroundUnits(Zone,"Emberwheat");var beforeInventory=Player.GetPart<InventoryPart>().Objects.ToArray();string rawRowModel=Model(row);
            yield return Capture("01-real-ripe-row-before-harvest");
            yield return CookingPaid(CookingWorldAction(row,"Harvest"),"native-real-row-harvest");
            var harvest=_exchangeLastWindow.Where(e=>e.Category=="loot"&&e.Kind=="FieldHarvested"&&e.ActorId==Player.ID&&e.TargetId==rowId).ToArray();
            var grain=Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e=>e.BlueprintName=="Emberwheat");
            Check("native_harvest_exact_source_and_one_packed_unit",harvest.Length==1&&Payload(harvest[0],"yield")=="Emberwheat"&&Payload(harvest[0],"count")=="1"&&Payload(harvest[0],"dropped")=="0"
                &&row.GetPart<FieldHarvestPart>()==cropPart&&cropPart.Harvested&&field.GetEntityCell(row)==rowAt&&grain!=null&&!beforeInventory.Contains(grain)&&CookingOwns(grain)&&CookingUnits(grain)==1
                &&CookingGroundUnits(Zone,"Emberwheat")==groundBefore&&CarriedUnits(CookingOutput)==0&&Model(row)==rawRowModel.Replace("grain-","stubble-"));
            yield return Capture("02-harvested-row-stubble");
            var sill=Manager.GetZone(WorldMap.StartingZoneID);Require(sill!=null,"actual generated Sill graph");Entity fire=null;Cell fireArrival=null;
            foreach(var e in sill.GetReadOnlyEntities().Where(e=>e.HasPart<CampfirePart>()))
            {
                var at=sill.GetEntityCell(e);var physics=e.GetPart<PhysicsPart>();
                if(at==null||e.GetPart<CampfirePart>().ParentEntity!=e||e.GetPart<CampfirePart>().FiniteCooking||physics?.ParentEntity!=e||physics.InInventory!=null||physics.Equipped!=null)continue;
                var near=CellsNear(sill,at,1,1).FirstOrDefault(c=>DrySafe(sill,c,null)&&Directions.Any(d=>DrySafe(sill,sill.GetCell(c.X+d.x,c.Y+d.y),null)));
                if(near==null)continue;fire=e;fireArrival=near;break;
            }
            Require(fire!=null,"bounded actual authored Sill campfire and safe checkpoint step");yield return Transfer(sill,fireArrival,"existing authored Sill cooking campfire");
            var selected=typeof(CookingService).GetMethod("FindStation",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Player,Zone});
            Require(selected!=null&&ReferenceEquals(Field(selected,"Owner"),fire),"real CookingService selects this exact existing fire");
            var fireAt=Zone.GetEntityCell(fire);var firePart=fire.GetPart<CampfirePart>();string fireId=fire.ID,rawId=grain.ID;
            _observations.Add(new{phase="actual-cooking-source",zone=Zone.ZoneID,owner=fireId,blueprint=fire.BlueprintName,position=Zone.GetEntityPosition(fire),finite=firePart.FiniteCooking,temperature=fire.GetPart<ThermalPart>()?.Temperature,fuel=fire.GetPart<FuelPart>()?.FuelMass,boundary="Existing authored legacy station. No finite new-site lifetime claim."});
            yield return Capture("03-real-existing-sill-campfire");
            yield return CookingPaid(CookingInventoryAction(grain,"Cook"),"native-earned-grain-cook");
            var cookedRows=_exchangeLastWindow.Where(e=>e.Category=="event"&&e.Kind=="FoodCooked"&&e.ActorId==Player.ID&&e.TargetId==rawId).ToArray();
            var food=Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e=>e.BlueprintName==CookingOutput);
            Check("native_cook_one_for_one_original_food",cookedRows.Length==1&&Payload(cookedRows[0],"from")=="Emberwheat"&&Payload(cookedRows[0],"into")==CookingOutput&&Payload(cookedRows[0],"count")=="1"
                &&food!=null&&food!=grain&&CookingOwns(food)&&CookingUnits(food)==1&&CarriedUnits("Emberwheat")==0&&CookingUnits(grain)==0&&!Player.GetPart<InventoryPart>().Objects.Contains(grain)
                &&Zone.GetEntityCell(fire)==fireAt&&fire.GetPart<CampfirePart>()==firePart&&field.GetEntityCell(row)==rowAt&&cropPart.Harvested&&TradeSystem.GetDrams(Player)==initialDrams&&CookingStats(Player)==initialStats&&CookingGear(Player)==initialGear);
            yield return CookingDrop(food);
            Check("native_free_drop_keeps_exact_prepared_owner",CookingGround(food)&&Zone.GetEntityCell(food)==At&&CookingUnits(food)==1&&CarriedUnits(CookingOutput)==0);
            var viewStep=Directions.Select(d=>Zone.GetCell(At.X+d.x,At.Y+d.y)).FirstOrDefault(c=>DrySafe(Zone,c,null));Require(viewStep!=null,"safe native step clears the prepared food for viewing");
            yield return CookingPaid(Tap(Direction(viewStep.X-At.X,viewStep.Y-At.Y)),"native-step-beside-prepared-food");Require(At==viewStep&&SpatialQuery.Distance(Zone,Player,food)==1,"real adjacent food viewing position");
            yield return Capture("04-real-prepared-food-dropped");CookingVisual(food);
            yield return CookingCheckpoint(food,row,field,fire);
            Check("ordinary_cooking_no_grants_or_extra_food",CarriedUnits(CookingOutput)==1&&CarriedUnits("Emberwheat")==0&&TradeSystem.GetDrams(Player)==initialDrams&&CookingStats(Player)==initialStats&&CookingGear(Player)==initialGear&&_paidInputs<=6);
        }
        IEnumerator CookingCheckpoint(Entity food,Entity row,Zone field,Entity fire)
        {
            var oldPlayer=Player;var oldZone=Zone;var oldManager=Manager;var oldField=field;
            string id=food.ID,rowId=row.ID,fieldId=field.ZoneID,fireId=fire.ID,zoneId=Zone.ZoneID;
            var at=At;var foodAt=Zone.GetEntityPosition(food);var rowAt=field.GetEntityPosition(row);var fireAt=Zone.GetEntityPosition(fire);
            string signature=PlayerSignature(),stats=CookingStats(Player),gear=CookingGear(Player);int tick=Tick,energy=Energy,world=WorldClock.CurrentTick;
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"existing isolated initial save metadata");string gameId=info.GameID,path=Path.Combine(SaveGameService.SaveRootOverride,gameId,"Quick.sav.gz"),before=PassageHash(path);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);yield return Settled();string saved=PassageHash(path);
            Check("native_save_ground_food_and_cached_spent_row",saved!=before&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."&&SaveGameService.GetSaveInfo("Quick").GameID==gameId&&SaveGameService.GetSaveInfo("Quick").ActiveZoneID==zoneId&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world);
            yield return CookingPaid(CookingWorldAction(food,"Take"),"native-prepared-food-pickup");
            Check("native_take_same_prepared_item",CookingOwns(food)&&CookingUnits(food)==1&&Zone.GetEntityCell(food)==null&&CarriedUnits(CookingOutput)==1);
            var step=Directions.Select(d=>Zone.GetCell(At.X+d.x,At.Y+d.y)).FirstOrDefault(c=>DrySafe(Zone,c,null));Require(step!=null,"one real safe checkpoint mutation step");
            yield return CookingPaid(Tap(Direction(step.X-At.X,step.Y-At.Y)),"native-cooking-checkpoint-step");
            Check("native_unsaved_graph_and_clock_mutation",At==step&&At!=at&&CookingOwns(food)&&(Tick!=tick||Energy!=energy)&&PassageHash(path)==saved);
            yield return Capture("05-real-pickup-and-checkpoint-mutation");yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(oldPlayer,Player)){Require(Time.realtimeSinceStartupAsDouble-began<8,"native F6 replaces cooking graph");yield return null;}yield return Settled();
            Require(Manager.CachedZones.TryGetValue(fieldId,out var restoredField),"loaded cached source graph without remote generation");
            var restored=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==id);var restoredRow=restoredField.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==rowId);var restoredFire=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==fireId);
            Check("native_load_exact_food_stubble_and_player",!ReferenceEquals(oldManager,Manager)&&!ReferenceEquals(oldZone,Zone)&&!ReferenceEquals(oldField,restoredField)&&Zone.ZoneID==zoneId&&ReferenceEquals(Manager.CachedZones[zoneId],Zone)
                &&restored!=null&&!ReferenceEquals(restored,food)&&CookingGround(restored)&&restored.BlueprintName==CookingOutput&&CookingUnits(restored)==1&&Zone.GetEntityPosition(restored)==foodAt
                &&restoredRow!=null&&!ReferenceEquals(restoredRow,row)&&restoredRow.GetPart<FieldHarvestPart>()?.ParentEntity==restoredRow&&restoredRow.GetPart<FieldHarvestPart>().Harvested&&restoredField.GetEntityPosition(restoredRow)==rowAt
                &&restoredFire!=null&&!ReferenceEquals(restoredFire,fire)&&restoredFire.GetPart<CampfirePart>()?.ParentEntity==restoredFire&&!restoredFire.GetPart<CampfirePart>().FiniteCooking&&Zone.GetEntityPosition(restoredFire)==fireAt
                &&Player.ID==oldPlayer.ID&&At.X==at.X&&At.Y==at.Y&&PlayerSignature()==signature&&CookingStats(Player)==stats&&CookingGear(Player)==gear&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&PassageHash(path)==saved
                &&Manager.CachedZones.Values.Sum(z=>z.GetReadOnlyEntities().Count(e=>e.ID==id))==1&&CarriedUnits(CookingOutput)==0);
            yield return Capture("06-native-restored-prepared-food");CookingVisual(restored);
            yield return CookingPaid(CookingWorldAction(restored,"Take"),"native-restored-food-pickup");
            Check("native_restored_food_is_still_usable_owned_food",CookingOwns(restored)&&CookingUnits(restored)==1&&restored.HasPart<FoodPart>()&&!restored.HasPart<CookablePart>()&&CarriedUnits(CookingOutput)==1&&Zone.GetEntityCell(restored)==null&&restoredRow.GetPart<FieldHarvestPart>().Harvested);
            yield return CookingInventoryFrame(restored);
        }
        IEnumerator CookingInventoryFrame(Entity item)
        {
            int tick=Tick,energy=Energy,world=WorldClock.CurrentTick;string signature=PlayerSignature();
            yield return Tap(Key.I);yield return Tap(Key.Tab);Require(State=="InventoryOpen","actual final food inventory");
            var rows=(IList)Field(_input.InventoryUI,"_rows");int row=-1;for(int i=0;i<rows.Count;i++)if(ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i],"Item"))?.Item,item))row=i;
            Require(row>=0,"exact restored prepared-food inventory row");for(int n=0;(int)Field(_input.InventoryUI,"_cursorIndex")!=row;n++){Require(n<80,"bounded food inventory cursor");yield return Tap((int)Field(_input.InventoryUI,"_cursorIndex")<row?Key.DownArrow:Key.UpArrow);}
            yield return Capture("07-final-carried-prepared-food");yield return CloseNormal();
            Check("final_inventory_read_is_free",Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&PlayerSignature()==signature&&CookingOwns(item));
        }
        IEnumerator CookingWorldAction(Entity owner,string command)
        {Require(CookingCurrent(owner)&&SpatialQuery.Distance(Zone,Player,owner)<=1,"current reachable cooking-world owner");yield return Focus(owner);yield return Tap(Key.Enter);Require(State=="WorldActionMenuOpen","actual cooking world menu");yield return SelectCurrentOwner(owner);yield return MenuAction(command);yield return CloseNormal();}
        IEnumerator CookingInventoryAction(Entity owner,string command)
        {Require(CookingOwns(owner),"current exact earned cooking inventory owner");yield return ItemAction(owner,command);yield return CloseNormal();}
        IEnumerator CookingPaid(IEnumerator action,string label)
        {yield return ExchangePaid(action,"local",label);Require(_exchangeLastWindow.Count(e=>e.Category=="turn"&&e.Kind=="End"&&e.ActorId==Player.ID)==1,"exactly one real player action for "+label);}
        IEnumerator CookingDrop(Entity food)
        {
            int tick=Tick,energy=Energy,world=WorldClock.CurrentTick;var at=At;string marker=ExchangeMarker("native-free-prepared-drop");
            yield return CookingInventoryAction(food,"drop");var rows=ExchangeWindow(marker);
            Require(Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&At==at&&!rows.Any(e=>e.Category=="turn"&&e.Kind=="End"&&e.ActorId==Player.ID),"ordinary inventory drop is free");
            _observations.Add(new{phase="native-free-prepared-drop",owner=food.ID,beforeTick=tick,afterTick=Tick,beforeEnergy=energy,afterEnergy=Energy,rows});
        }
        void CookingVisual(Entity food)
        {
            var entry=SpreadCooking3DLibrary.Load()?.Find(SpreadCooking3DLibrary.Toasted);Require(entry!=null,"prepared-food art library must be imported before visual acceptance");
            var view=Visual(food,CookingModel,1);Require(Presenter.TryGetApprovedStyle(food,out var proof)&&proof.ExpectedMesh==entry.Mesh&&proof.ExpectedMaterial==entry.Material
                &&entry.Material==ReferenceGladeVoxelLibrary.Load().Material&&view.GetComponent<MeshFilter>()?.sharedMesh==entry.Mesh,"same persistent approved prepared-food mesh and material");
        }
        bool CookingOwns(Entity item)=>item!=null&&Player.GetPart<InventoryPart>().Objects.Contains(item)&&item.GetPart<PhysicsPart>()?.ParentEntity==item&&item.GetPart<PhysicsPart>().InInventory==Player&&item.GetPart<PhysicsPart>().Equipped==null&&item.SpatialZone==null;
        bool CookingGround(Entity item)=>CookingCurrent(item)&&item.GetPart<PhysicsPart>()?.ParentEntity==item&&item.GetPart<PhysicsPart>().InInventory==null&&item.GetPart<PhysicsPart>().Equipped==null&&!Player.GetPart<InventoryPart>().Objects.Contains(item);
        bool CookingCurrent(Entity item)=>item!=null&&item.SpatialZone==Zone&&Zone.GetEntityCell(item)?.Objects.Contains(item)==true&&Zone.GetReadOnlyEntities().Count(e=>e.ID==item.ID)==1;
        static int CookingUnits(Entity item)=>item.GetPart<StackerPart>()?.StackCount??1;
        static int CookingGroundUnits(Zone zone,string blueprint)=>zone.GetReadOnlyEntities().Where(e=>e.BlueprintName==blueprint).Sum(CookingUnits);
        static string CookingStats(Entity actor)=>string.Join("|",actor.Statistics.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+":"+p.Value.BaseValue+":"+p.Value.Bonus+":"+p.Value.Penalty+":"+p.Value.Boost+":"+p.Value.sValue+":"+p.Value.Min+":"+p.Value.Max));
        static string CookingGear(Entity actor)=>string.Join("|",(actor.GetPart<Body>()?.GetParts()??new List<BodyPart>()).Select(p=>p.ID+":"+p.Equipped?.ID))
            +";equipped="+string.Join("|",actor.GetPart<InventoryPart>().GetAllEquipped().Distinct().OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+CookingUnits(e)+":"+e.GetPart<PhysicsPart>()?.Equipped?.ID));
    }
}
