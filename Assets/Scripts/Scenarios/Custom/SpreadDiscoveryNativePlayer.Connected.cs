using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Actual selected-build connected trip. All mutations use native keys;
    /// owner queries below are observations, never scenario setup.</summary>
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        string _connectedBuild, _connectedPanId, _connectedKeeperId, _connectedCrateId, _connectedDryId;
        string _connectedRipeId, _connectedResinCropId, _connectedResinSoilId, _connectedMealId;
        string[] _connectedGrainIds = Array.Empty<string>();
        string _connectedReplantedRootId, _connectedReplantedResinId, _connectedRootSoilId;
        string _connectedLastPlantId, _connectedOriginalDaggerId;
        int _connectedDefenses, _connectedAnnouncements;
        readonly HashSet<string> _connectedVisitedSurfaces = new HashSet<string>(StringComparer.Ordinal);
        bool Connected => !string.IsNullOrEmpty(_connectedBuild);
        static readonly string[] ConnectedBaseChecks = {
            "ordinary_start", "connected_cellar_supplies", "connected_sootroot_harvest", "connected_replanted_cellar_bed", "connected_first_watering",
            "connected_material_choice", "connected_repair_introduction", "connected_reserve_access", "connected_real_grain",
            "connected_commission", "connected_pending_save_restore", "connected_returned_reserve", "connected_replanted_reserve_bed", "connected_meal_return",
            "connected_meal_used", "connected_travel_growth", "connected_finite_cultivation_award", "connected_final_save_restore", "connected_finish" };
        string[] ConnectedChecks => ConnectedBaseChecks.Concat(ConnectedInkChecks).ToArray();
        const string ConnectedIntent = "Actual native build selection, ordinary movement and map travel, finite original cellar clay/book, lawful charged Rime use, real sootroot harvest and repeat watering, material choice to repair the kitchen pan, spoken introduction and local reserve permission, harvested grain/claspbean/resin, paid physical kitchen escrow and 120-tick return, pending F5/F6 conservation, real ink preparation/re-inking and later hostile cast, meal pickup/use, actual returned-seed replanting on harvested cellar and claimed reserve beds, physical cellar revisit, final save/load of connected consequences.";
        const string ConnectedLimits = "One selected build and seed64 per run, not all-build/all-seed coverage or unaided discovery. No player/source transfers, grants, fake targets, resource replenishment, artificial charge drain or clock changes. Native route uses a finite observer pathfinder and may honestly fail against live enemies. Duelist may defend against up to six actual adjacent hostiles using its original equipped dagger and finite tonics; resolved combat does not imply a direct-hit kill when bleeding finishes the target. Stormcaller instead retains its existing bounded original Calm choice. A later rite may revisit an already visited surface if the original cellar guard died. Full-HP meal consumption does not demonstrate healing benefit or bleeding removal. Actual screenshots need independent visual review. Dry crop growth is proven from its original saved owner and native world time, not a weather assumption.";

        IEnumerator ConnectedChooseBuild()
        {
            var menu = (StartingBuildMenuController)Field(_input, "_buildMenuController");
            Require(menu.IsOpen, "real native starting-build picker is open");
            int index = menu.Model.Options.ToList().FindIndex(b => b.Id == _connectedBuild);
            Require(index >= 0 && index < 9, "actual requested build card exists");
            yield return Capture("connected-00-build-picker");
            yield return Tap(Shortcut("Digit" + (index + 1)));
            Require(menu.SelectedIndex == index, "native build card selected");
            var def = menu.Model.Selected;
            yield return Tap(Key.Enter); yield return Settled();
            var pack = Player.GetPart<InventoryPart>();
            Check("ordinary_start", !menu.IsOpen && Player.GetProperty(StartingBuildService.PropertyName) == _connectedBuild
                && Manager.WorldSeed == (_claimedSupplies ? 1 : 64) && Zone.ZoneID == GleanersDistrict.SurfaceID
                && Player.GetStatValue("Hitpoints") == 40 && Player.GetStatValue("Level") == 1
                && Player.GetStatValue("Strength") == def.Attributes.Strength && Player.GetStatValue("Agility") == def.Attributes.Agility
                && pack.EquippedItems.Values.Any(e => e.BlueprintName == "Dagger" && e.GetPart<PhysicsPart>()?.Equipped == Player)
                && Packed("HealingTonic") == 2 && Packed("WateringGrimoire") == 1 && TradeSystem.GetDrams(Player) == 50
                && !DevMode.Enabled && !Player.HasPart<BitLockerPart>() && !DebugInvincibility.IsEnabled(Player));
            _connectedOriginalDaggerId=StartingBuildService.PrimaryHandWeapon(Player)?.ID;
            Require(!string.IsNullOrEmpty(_connectedOriginalDaggerId),"real chosen starting primary dagger");
            _connectedVisitedSurfaces.Add(Zone.ZoneID);
            _districtOriginalCalm = Player.GetPart<ActivatedAbilitiesPart>()?.AbilityList.SingleOrDefault(a => a.Command == "CommandCalm")?.ID ?? Guid.Empty;
            _districtPreparing = true;
            _notes.Add("ACTUAL CHOSEN BUILD: " + def.Id + ". Starting equipped dagger remains real; no legacy-kit replacement.");
        }
        int Packed(string blueprint) => Player.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == blueprint && Owns(Player,e)).Sum(Units);
        Entity ConnectedAt(string zoneId, string id) => Manager.CachedZones.TryGetValue(zoneId,out var z) ? z.GetReadOnlyEntities().SingleOrDefault(e => e.ID == id) : null;
        Entity ConnectedPan => ConnectedAt(KitchenBatchPart.KitchenZoneID, _connectedPanId);
        Entity ConnectedKeeper => ConnectedAt(LocalGatheringClaimPart.ReserveZoneID, _connectedKeeperId);
        Entity ConnectedDry => ConnectedAt(GleanersCellarBuilder.ZoneID, _connectedDryId);
        Entity ConnectedResin => ConnectedAt(LocalGatheringClaimPart.ReserveZoneID, _connectedResinCropId);
        /// <summary>Wait through real queued announcements and effect/input
        /// gates. Three observed ordinary frames close the late-popup race;
        /// this never clears the message queue or advances gameplay itself.</summary>
        IEnumerator ConnectedCloseAndStabilize()
        {
            double began=UnityEngine.Time.realtimeSinceStartupAsDouble;
            int normalFrames=0,closures=0;
            while(true)
            {
                Require(UnityEngine.Time.realtimeSinceStartupAsDouble-began<8,"connected native modal stabilization deadline: "+State);
                // The rite has already committed; InputHandler itself completes its
                // paid turn after the blocking effect. Escape is not a free modal
                // dismissal here and must never race that pending turn settlement.
                if(State=="WaitingForFxResolution")
                {
                    normalFrames=0;
                    yield return null;
                    continue;
                }
                if(State=="AnnouncementOpen")
                {
                    Require(++closures<=8,"bounded actual announcement/modal dismissals");normalFrames=0;
                    int tick=Tick,energy=Energy;string label="connected-announcement-"+(++_connectedAnnouncements);
                    yield return ReadPages(label);
                    _observations.Add(new{phase="connected-native-announcement",label,text=_readerText,zone=Zone.ZoneID,tick,energy});
                    yield return Tap(Key.Escape);
                    Require(Tick==tick&&Energy==energy,"reading and dismissing a real announcement stays free");
                    continue;
                }
                if(State!="Normal")
                {
                    Require(++closures<=8,"bounded connected modal closure: "+State);normalFrames=0;
                    string state=State;int tick=Tick,energy=Energy;yield return Tap(Key.Escape);
                    Require(Tick==tick&&Energy==energy,"closing actual "+state+" stays free");continue;
                }
                bool blocked=_input.ZoneRenderer?.WorldFx?.HasBlockingFx==true||MessageLog.HasPendingAnnouncement
                    ||UnityEngine.Time.time-(float)Field(_input,"_lastMoveTime")<_input.MoveRepeatDelay;
                normalFrames=blocked?0:normalFrames+1;
                if(normalFrames>=3)yield break;
                yield return null;
            }
        }
        IEnumerator ConnectedDuelistDefense()
        {
            if(!Connected||_connectedBuild!="duelist"||_fighting)yield break;
            if(_fieldwork){yield return FieldworkDuelistDefense();yield break;}
            var target=Threats(Zone).Where(e=>SpatialQuery.Distance(Zone,Player,e)<=1
                &&!ReferenceGladeRouteControl.HasLiveCalm(Zone,e))
                .OrderBy(e=>SpatialQuery.Distance(Zone,Player,e)).ThenBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault();
            if(target==null)yield break;
            Require(_connectedDefenses<6,"bounded six ordinary close-hostile defenses");
            var weapon=StartingBuildService.PrimaryHandWeapon(Player);
            Require(weapon?.ID==_connectedOriginalDaggerId&&weapon.BlueprintName=="Dagger"
                &&weapon.GetPart<PhysicsPart>()?.Equipped==Player,"same real chosen equipped dagger for defense");
            var oldGuard=_guard;string oldId=_guardId;bool oldFighting=_fighting;
            bool oldAttempt=_guardAttempt,oldDamage=_guardDamage,oldLethal=_guardLethal;
            int hp=Player.GetStatValue("Hitpoints"),tonics=Packed("HealingTonic");string targetId=target.ID;
            _connectedDefenses++;_guard=target;_guardId=targetId;_guardAttempt=_guardDamage=_guardLethal=false;
            try
            {
                yield return FightGuard(requireDirectLethal:false);
                Require(CombatSystem.IsDeathHandled(target)&&_guardAttempt&&_guardDamage,"real close-hostile combat resolved after observed chosen-weapon attacks");
                _observations.Add(new{phase="connected-duelist-native-defense",zone=Zone.ZoneID,target=targetId,
                    targetBlueprint=target.BlueprintName,originalWeapon=_connectedOriginalDaggerId,defense=_connectedDefenses,
                    hpBefore=hp,hpAfter=Player.GetStatValue("Hitpoints"),tonicsBefore=tonics,tonicsAfter=Packed("HealingTonic"),
                    directLethalObserved=_guardLethal,actualDamageObserved=_guardDamage,
                    bound="Normal attacks and original finite tonics only. Death after player damage may be a bleed or counterattack; direct lethality is reported separately."});
                yield return Capture("connected-duelist-defense-"+_connectedDefenses);
            }
            finally
            {
                _guard=oldGuard;_guardId=oldId;_fighting=oldFighting;
                _guardAttempt=oldAttempt;_guardDamage=oldDamage;_guardLethal=oldLethal;
            }
        }
        IEnumerator ConnectedJourney()
        {
            yield return Capture("connected-01-real-chosen-start");
            Require(PackedGrain()==0,"ordinary chosen start has no granted grain");
            yield return ConnectedGleanGrain();
            yield return DistrictWalk(Zone.GetCell(28,19),100);
            yield return Paid(Tap(Key.LeftShift,Key.Period),"local","connected-native-cellar-down");
            Require(Zone.ZoneID == GleanersCellarBuilder.ZoneID, "real cellar reached");
            var crate = DistrictOwner(Zone,GleanersCellarBuilder.RoleKey,"supplies"); _connectedCrateId = crate.ID;
            var contents = crate.GetPart<ContainerPart>().Contents.ToArray();
            _connectedBookId = contents.Single(e => e.BlueprintName == "ShatteredRimeGrimoire").ID;
            Require(contents.Where(e => e.BlueprintName == "FireClay").Sum(Units) == 2 && DistrictClay() == 0,"actual finite original clay");
            foreach(var at in new[]{(40,5),(45,5),(46,4),(46,2),(50,2),(50,5),(62,5),(63,6)})
                yield return DistrictWalk(Zone.GetCell(at.Item1,at.Item2),80);
            yield return Paid(DistrictTakeCache(crate,contents),"local","connected-finite-cache-take");
            Check("connected_cellar_supplies", DistrictClay()==2 && crate.GetPart<ContainerPart>().Contents.Count==0
                && Owns(Player,contents.Single(e=>e.ID==_connectedBookId)) && CountGraphId(_connectedBookId)==1);
            yield return ConnectedRimeCastOnHostile();
            var ripe = DistrictOwner(Zone,GleanersCellarBuilder.RoleKey,"sootroot-ripe"); _connectedRipeId=ripe.ID;
            yield return ConnectedHarvest(ripe,"SootrootPulp");
            Check("connected_sootroot_harvest",Packed("SootrootPulp")==2 && CountGraphId(_connectedRipeId)==0 && Packed("SootrootSeed")==1);
            yield return ConnectedReplantCurrentBed("SootrootSeed","SootrootCrop");
            _connectedReplantedRootId=_connectedLastPlantId;
            _connectedRootSoilId=At.Objects.Single(e=>e.HasTag("Terrain")&&e.HasPart<CultivatedSoilPart>()).ID;
            Check("connected_replanted_cellar_bed",Packed("SootrootSeed")==0&&_connectedReplantedRootId!=_connectedRipeId
                &&At.Objects.Any(e=>e.ID==_connectedReplantedRootId)&&CultivatedSoilPart.IsCultivated(Zone,At));
            var dry = DistrictOwner(Zone,GleanersCellarBuilder.RoleKey,"sootroot-dry"); _connectedDryId=dry.ID;
            Require(dry.GetPart<CropPart>().GrowthStage==0 && dry.GetPart<CropPart>().MoistureTicks==0,"original dry source starts dormant");
            yield return ConnectedWater(dry);
            Check("connected_first_watering",dry.GetPart<CropPart>().MoistureTicks>0 && dry.GetPart<CropPart>().GrowthStage<2
                &&ConnectedAt(GleanersCellarBuilder.ZoneID,_connectedReplantedRootId)?.GetPart<CropPart>()?.MoistureTicks>0);
            yield return Capture("connected-02-earned-roots-watered-return-source");
            yield return ConnectedLeaveCellar();

            yield return TravelSurface(KitchenBatchPart.KitchenZoneID);
            var pan = Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="ConnectedBatchPan");_connectedPanId=pan.ID;
            yield return DistrictApproach(pan,140);
            yield return Paid(WorldAction(pan,RepairablePart.RepairCommand),"local","connected-pan-material-choice");
            var well=DistrictOwner(Manager.CachedZones[GleanersDistrict.SurfaceID],GleanersDistrict.RoleKey,"well");
            Check("connected_material_choice",pan.GetPart<RepairablePart>().Repaired && !well.GetPart<RepairablePart>().Repaired && DistrictClay()==0);
            var cook=pan.GetPart<KitchenBatchPart>().Worker;yield return DistrictApproach(cook,30);
            int tick=Tick,energy=Energy,drams=TradeSystem.GetDrams(Player);
            yield return Paid(WorldAction(cook,CookIntroductionPart.IntroductionCommand),"local","connected-earned-spoken-introduction");
            Check("connected_repair_introduction",Player.HasPart<CookIntroductionKnowledgePart>()&&TradeSystem.GetDrams(Player)==drams);
            var clasp=Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="ClaspbeanCrop" && e.GetProperty("ConnectedSpread.Role")=="kitchen-crop");
            yield return ConnectedHarvest(clasp,"ClaspbeanPulp");yield return Capture("connected-03-repaired-pan-and-real-claspbean");

            yield return TravelSurface(LocalGatheringClaimPart.ReserveZoneID);
            var keeper=Zone.GetReadOnlyEntities().Single(e=>e.HasPart<LocalGatheringClaimPart>());_connectedKeeperId=keeper.ID;
            yield return DistrictApproach(keeper,140);tick=Tick;energy=Energy;drams=TradeSystem.GetDrams(Player);
            yield return Paid(WorldAction(keeper,"ReserveAccess:introduction"),"local","connected-native-relayed-introduction");
            Check("connected_reserve_access",keeper.GetPart<LocalGatheringClaimPart>().GetState(Player)==ReserveAccessState.Granted
                && TradeSystem.GetDrams(Player)==drams);
            var resin=Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="PitchpodCrop"&&e.GetProperty("ConnectedSpread.Role")=="reserve-crop");
            _connectedResinCropId=resin.ID;
            _connectedResinSoilId=Zone.GetEntityCell(resin).Objects.Single(e=>e.HasTag("Terrain")&&e.HasPart<CultivatedSoilPart>()).ID;
            if(resin.GetPart<CropPart>().GrowthStage<2)yield return ConnectedWater(resin);
            // This seed's after-harvest field has one finite row. The other
            // grain was physically gleaned at the glade before the cellar trip.
            yield return ConnectedGleanGrain();
            Check("connected_real_grain",PackedGrain()==2&&Packed("ClaspbeanPulp")==2);
            yield return Capture("connected-04-local-permission-and-actual-grain");

            yield return TravelSurface(KitchenBatchPart.KitchenZoneID);pan=ConnectedPan;
            yield return DistrictApproach(pan,140);drams=TradeSystem.GetDrams(Player);
            yield return Paid(WorldAction(pan,KitchenBatchPart.StartCommand),"local","connected-paid-kitchen-commission");
            var batch=pan.GetPart<KitchenBatchPart>();
            Check("connected_commission",batch.State=="Working"&&batch.Commissioner==Player&&batch.DueTick-batch.StartTick==120
                &&batch.InputCounts.Sum()==3&&batch.Inputs.All(e=>e.GetPart<PhysicsPart>()?.InInventory==batch.Escrow)
                &&PackedGrain()==0&&Packed("ClaspbeanPulp")==1&&TradeSystem.GetDrams(Player)==drams-2);
            yield return Capture("connected-05-physical-escrow-working");
            yield return ConnectedCheckpoint(true);

            // Return to the actual reserve after ordinary travel. If the dry form
            // needs its second watering, collect the finished meal on a useful
            // intervening round trip instead of injecting time or waiting for growth.
            yield return TravelSurface(LocalGatheringClaimPart.ReserveZoneID);resin=ConnectedResin;
            bool returnForResin=resin.GetPart<CropPart>().GrowthStage<2;
            if(returnForResin)yield return ConnectedWater(resin);
            else yield return ConnectedHarvest(resin,"PitchpodResin");
            yield return TravelSurface(KitchenBatchPart.KitchenZoneID);pan=ConnectedPan;batch=pan.GetPart<KitchenBatchPart>();
            yield return DistrictApproach(batch.Pickup,140);
            Require(batch.State=="Ready"&&batch.Output!=null,"ordinary travel finished real work");
            var meal=batch.Output;_connectedMealId=meal.ID;
            yield return Paid(TakeSole(batch.Pickup,_connectedMealId),"local","connected-real-meal-pickup");
            Check("connected_meal_return",Owns(Player,meal)&&batch.Pickup.GetPart<ContainerPart>().Contents.Count==0&&batch.Escrow.GetPart<ContainerPart>().Contents.Count==0);
            int hp=Player.GetStatValue("Hitpoints");bool bled=Player.HasEffect<BleedingEffect>();
            yield return Paid(ItemAction(meal,"Eat"),"local","connected-native-meal-eaten");
            Check("connected_meal_used",!Owns(Player,meal)&&CountGraphId(_connectedMealId)==0&&Player.GetStatValue("Hitpoints")>=hp);
            _observations.Add(new{phase="connected-meal-use",hpBefore=hp,hpAfter=Player.GetStatValue("Hitpoints"),ordinaryBleedingBefore=bled,ordinaryBleedingAfter=Player.HasEffect<BleedingEffect>()});
            yield return Capture("connected-06-finished-meal-used");
            if(returnForResin)
            {
                yield return TravelSurface(LocalGatheringClaimPart.ReserveZoneID);resin=ConnectedResin;
                Require(resin.GetPart<CropPart>().GrowthStage==2,"second genuine return grew pitchpod to harvest");
                yield return ConnectedHarvest(resin,"PitchpodResin");
            }
            yield return TravelSurface(LocalGatheringClaimPart.ReserveZoneID);
            Require(Zone.ZoneID==LocalGatheringClaimPart.ReserveZoneID,"replant while physically in claimed reserve");
            // The early-ripe branch collected resin before the meal return; walk
            // back to its exact original bound bed before spending its saved seed.
            var claim=ConnectedKeeper.GetPart<LocalGatheringClaimPart>();
            var resinBed=new[]{claim.FirstSoil,claim.SecondSoil}.SingleOrDefault(e=>e?.ID==_connectedResinSoilId);
            Require(resinBed!=null,"exact retained claimed resin ground");
            yield return DistrictWalk(Zone.GetEntityCell(resinBed),140);
            yield return ConnectedReplantCurrentBed("PitchpodSeed","PitchpodCrop");_connectedReplantedResinId=_connectedLastPlantId;
            Check("connected_replanted_reserve_bed",Packed("PitchpodSeed")==0&&_connectedReplantedResinId!=_connectedResinCropId
                &&ConnectedKeeper.GetPart<LocalGatheringClaimPart>().GetState(Player)==ReserveAccessState.Granted
                &&ConnectedSpread3DLibrary.ResolveReserveBed(Zone,resinBed)=="connected-spread-reserve-bed");
            Check("connected_returned_reserve",Packed("PitchpodResin")==2&&CountGraphId(_connectedResinCropId)==0
                &&ConnectedKeeper.GetPart<LocalGatheringClaimPart>().GetState(Player)==ReserveAccessState.Granted);
            yield return Capture("connected-07-legal-resin-and-persistent-cord");
            yield return ConnectedInkJourney();
            Require(Zone.ZoneID==GleanersCellarBuilder.ZoneID,"earned ink journey returns to original cellar");
            dry=ConnectedDry;Require(dry!=null&&dry.GetPart<CropPart>().GrowthStage<2,"original first watering alone cannot fully grow56-unit sootroot");
            yield return ConnectedWater(dry);
            yield return ConnectedLeaveCellar();
            // Observe a real arrival and the real bed. Reading a cached inactive
            // crop is not evidence that arrival reconciliation has occurred.
            yield return Paid(Tap(Key.LeftShift,Key.Period),"local","connected-native-return-to-grown-beds");
            Require(Zone.ZoneID==GleanersCellarBuilder.ZoneID,"actual cellar re-entry for growth inspection");
            foreach(var at in new[]{(40,5),(45,5),(46,4),(46,2),(50,2),(50,5),(62,5)})
                yield return DistrictWalk(Zone.GetCell(at.Item1,at.Item2),90);
            var replantedRoot=ConnectedAt(GleanersCellarBuilder.ZoneID,_connectedReplantedRootId);
            Require(replantedRoot!=null,"same originally planted root survives return");
            yield return DistrictApproach(replantedRoot,40);
            var rootSoil=ConnectedAt(GleanersCellarBuilder.ZoneID,_connectedRootSoilId);
            Check("connected_travel_growth",ConnectedDry?.GetPart<CropPart>()?.GrowthStage==2&&ConnectedDry.ID==_connectedDryId
                &&replantedRoot.GetPart<CropPart>()?.GrowthStage==2&&replantedRoot.ID!=_connectedRipeId
                &&Zone.GetEntityCell(replantedRoot)==Zone.GetEntityCell(rootSoil)
                &&CultivatedSoilPart.IsCultivated(Zone,Zone.GetEntityCell(rootSoil))
                &&ConnectedAt(GleanersCellarBuilder.ZoneID,_connectedCrateId).GetPart<ContainerPart>().Contents.Count==0);
            yield return Capture("connected-08-original-and-replanted-beds-grown-on-real-return");
            var progress=Player.GetPart<ConnectedSpreadProgressPart>();
            Require(progress!=null&&progress.Awards==11,"only stores, restoration and kitchen awards before original dry harvest");
            yield return ConnectedHarvest(ConnectedDry,"SootrootPulp");
            Check("connected_finite_cultivation_award",ConnectedDry==null&&CountGraphId(_connectedDryId)==0
                &&progress.Awards==15&&Player.GetStatValue("Level")>=2);
            yield return ConnectedLeaveCellar();
            yield return ConnectedCheckpoint(false);
            Check("connected_finish",Player.GetProperty(StartingBuildService.PropertyName)==_connectedBuild&&Player.GetStatValue("Hitpoints")>10
                &&!DebugInvincibility.IsEnabled(Player)&&!DevMode.Enabled&&!Player.HasPart<BitLockerPart>()&&_localInputs<=1000&&_mapSteps<=30);
            yield return Capture("connected-09-final-saved-consequences");
        }
        IEnumerator ConnectedGleanGrain()
        {
            var row=Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="RipeCropRow"&&e.GetPart<FieldHarvestPart>()?.Harvested==false)
                .OrderBy(e=>SpatialQuery.Distance(Zone,Player,e)).FirstOrDefault(e=>PathTo(c=>SpatialQuery.DistanceToCell(Zone,e,c.X,c.Y)<=1)!=null);
            Require(row!=null,"actual reachable unspent grain row in current location");
            yield return DistrictApproach(row,140);int count=PackedGrain();
            yield return Paid(WorldAction(row,"Harvest"),"local","connected-real-finite-grain");
            Require(PackedGrain()==count+1&&row.GetPart<FieldHarvestPart>().Harvested,"one native finite grain harvest");
            _connectedGrainIds=_connectedGrainIds.Concat(new[]{row.ID}).ToArray();
        }
        IEnumerator ConnectedHarvest(Entity crop,string produce)
        {
            yield return DistrictApproach(crop,140);Require(crop.GetPart<CropPart>()?.GrowthStage==2,"real ripe planted crop");
            var at=Zone.GetEntityCell(crop);int before=Packed(produce),expected=crop.GetPart<CropPart>().YieldCount;
            yield return Paid(WorldAction(crop,"HarvestCultivatedCrop"),"local","connected-real-crop-harvest-"+produce);
            Require(Zone.GetEntityCell(crop)==null,"committed harvest removed old crop owner");
            yield return DistrictWalk(at,40);yield return Paid(ConnectedGroundPickup(),"local","connected-produced-ground-pickup");
            Require(Packed(produce)==before+expected,"actual harvest products picked up from source ground");
        }
        IEnumerator ConnectedReplantCurrentBed(string seedBlueprint,string cropBlueprint)
        {
            var cell=At;var zone=Zone;
            Require(!cell.Objects.Any(e=>e.HasPart<CropPart>())&&CultivatedSoilPart.IsCultivated(zone,cell),"harvest left the exact prepared bed reusable");
            var seed=Player.GetPart<InventoryPart>().Objects.FirstOrDefault(e=>e.BlueprintName==seedBlueprint&&Owns(Player,e));
            Require(seed?.GetPart<SeedPart>()?.CropBlueprint==cropBlueprint,"actual returned seed supports the original crop");
            int count=Packed(seedBlueprint),tick=Tick,energy=Energy;string id=seed.ID;
            yield return ItemAction(seed,"PlantSeed");yield return CloseNormal();
            var crop=cell.Objects.SingleOrDefault(e=>e.BlueprintName==cropBlueprint&&e.HasPart<CropPart>());
            Require(Zone==zone&&At==cell&&Packed(seedBlueprint)==count-1&&crop!=null
                &&crop.GetPart<CropPart>().GrowthStage==0&&crop.GetPart<CropPart>().MoistureTicks==0
                &&Tick==tick&&Energy==energy,"one returned seed planted on actual harvested bed using existing free native planting");
            _connectedLastPlantId=crop.ID;
            _observations.Add(new{phase="connected-native-replant",zone=zone.ZoneID,x=cell.X,y=cell.Y,
                seed=id,seedBlueprint,crop=crop.ID,cropBlueprint,beforeUnits=count,afterUnits=Packed(seedBlueprint),freeExistingPlanting=true});
        }
        IEnumerator ConnectedGroundPickup()
        {yield return Tap(Key.G);if(State=="PickupOpen")yield return Tap(Key.Tab);yield return CloseNormal();}
        IEnumerator ConnectedWater(Entity crop)
        {
            yield return DistrictApproach(crop,140);
            var abilities=Player.GetPart<ActivatedAbilitiesPart>();
            var ability=abilities.AbilityList.SingleOrDefault(a=>a.Command=="CommandConjureRain");
            if(ability==null)
            {
                var book=Player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="WateringGrimoire"&&Owns(Player,e));
                int tick=Tick,energy=Energy;yield return ItemAction(book,"ReadGrimoire");yield return CloseNormal();
                Require(Tick==tick&&Energy==energy,"native reading leaves clock unchanged");
                ability=abilities.AbilityList.SingleOrDefault(a=>a.Command=="CommandConjureRain");
            }
            Require(ability!=null&&ability.CooldownRemaining==0,"genuinely learned ready rain");int slot=abilities.GetSlotForAbility(ability.ID);
            Require(slot>=0&&slot<10,"actual rain hotbar slot");
            yield return Paid(Tap(Shortcut(slot==9?"Digit0":"Digit"+(slot+1))),"local","connected-native-crop-watering");
            Require(crop.GetPart<CropPart>().MoistureTicks>0,"actual original crop watered");
        }
        IEnumerator ConnectedLeaveCellar()
        {
            Require(Zone.ZoneID==GleanersCellarBuilder.ZoneID,"actual cellar departure");
            foreach(var at in new[]{(62,5),(50,5),(50,2),(46,2),(46,4),(45,5),(40,5),(40,12)})
                yield return DistrictWalk(Zone.GetCell(at.Item1,at.Item2),90);
            yield return Paid(Tap(Key.LeftShift,Key.Comma),"local","connected-native-cellar-up");
            Require(Zone.ZoneID==GleanersDistrict.SurfaceID,"original stair returns home");
        }
        string ConnectedDigest(bool pending)
        {
            var batch=ConnectedPan.GetPart<KitchenBatchPart>();
            var crate=ConnectedAt(GleanersCellarBuilder.ZoneID,_connectedCrateId);
            var keeper=ConnectedKeeper.GetPart<LocalGatheringClaimPart>();
            var book=Player.GetPart<InventoryPart>().Objects.Single(e=>e.ID==_connectedBookId);
            return string.Join("|",Player.ID,Player.GetProperty(StartingBuildService.PropertyName),ConnectedPan.ID,ConnectedPan.GetPart<RepairablePart>().Repaired,
                batch.State,batch.JobOrdinal,batch.StartTick,batch.DueTick,batch.OutputID,string.Join(",",batch.InputIDs),string.Join(",",batch.InputCounts),
                string.Join(",",batch.Escrow.GetPart<ContainerPart>().Contents.Select(e=>e.ID+":"+Units(e))),
                string.Join(",",batch.Pickup.GetPart<ContainerPart>().Contents.Select(e=>e.ID+":"+Units(e))),
                crate.GetPart<ContainerPart>().Contents.Count,keeper.GetState(Player),
                string.Join(",",Manager.CachedZones.Values.SelectMany(z=>z.GetReadOnlyEntities()).Where(e=>_connectedGrainIds.Contains(e.ID)).OrderBy(e=>e.ID).Select(e=>e.ID+":"+e.GetPart<FieldHarvestPart>()?.Harvested)),CountGraphId(_connectedRipeId),CountGraphId(_connectedResinCropId),
                ConnectedCropDigest(GleanersCellarBuilder.ZoneID,_connectedDryId),
                Player.GetPart<ConnectedSpreadProgressPart>()?.Awards,
                ConnectedCropDigest(GleanersCellarBuilder.ZoneID,_connectedReplantedRootId),
                ConnectedCropDigest(LocalGatheringClaimPart.ReserveZoneID,_connectedReplantedResinId),
                book.GetPart<GrimoireChargePart>().Charges,
                pending?"pending":CountGraphId(_connectedMealId).ToString());
        }
        string ConnectedCropDigest(string zoneId,string cropId)
        {
            if(string.IsNullOrEmpty(cropId))return "not-planted-yet";
            var crop=ConnectedAt(zoneId,cropId);var part=crop?.GetPart<CropPart>();
            return crop==null?"missing:"+cropId:crop.ID+":"+Manager.CachedZones[zoneId].GetEntityPosition(crop)+":"+part.GrowthStage+":"+part.TicksInStage+":"+part.MoistureTicks;
        }
        IEnumerator ConnectedCheckpoint(bool pending)
        {
            var oldPlayer=Player;var oldZone=Zone;var oldPan=ConnectedPan;var oldKeeper=ConnectedKeeper;
            string signature=ConnectedDigest(pending),stats=Stats(Player),gear=Gear(Player),zone=Zone.ZoneID;
            int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,x=At.X,y=At.Y,drams=TradeSystem.GetDrams(Player);
            if(pending)Require(ConnectedPan.GetPart<KitchenBatchPart>().State=="Working"&&WorldClock.CurrentTick<ConnectedPan.GetPart<KitchenBatchPart>().DueTick,"save genuinely pending work");
            string file=SaveFile(),oldHash=HashFile(file);yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(file);
            Require(oldHash!=_checkpointHash&&MessageLog.GetLast()=="Game saved.","actual successful new quicksave");
            var step=Steps.Select(d=>Zone.GetCell(x+d.x,y+d.y)).FirstOrDefault(c=>Safe(Zone,c,ThreatClearance)&&Zone.CanPlaceFootprint(Player,c.X,c.Y));
            Require(step!=null,"one genuine unsaved step");yield return StepTo(step.X,step.Y);Require(Tick>tick&&HashFile(file)==_checkpointHash,"unsaved change does not overwrite checkpoint");
            yield return Reload(oldPlayer);
            Check(pending?"connected_pending_save_restore":"connected_final_save_restore",!ReferenceEquals(Player,oldPlayer)&&!ReferenceEquals(Zone,oldZone)
                &&!ReferenceEquals(ConnectedPan,oldPan)&&!ReferenceEquals(ConnectedKeeper,oldKeeper)&&Zone.ZoneID==zone&&At.X==x&&At.Y==y
                &&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&Stats(Player)==stats&&Gear(Player)==gear&&TradeSystem.GetDrams(Player)==drams
                &&ConnectedDigest(pending)==signature&&HashFile(file)==_checkpointHash);
            yield return Capture(pending?"connected-pending-reloaded":"connected-final-reloaded");
        }
    }
}
