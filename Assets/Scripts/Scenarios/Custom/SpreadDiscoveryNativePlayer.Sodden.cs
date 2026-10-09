using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _soddenDistrict;
        const string SoddenIntent="Ordinary seed64 Duelist start, native map travel to Sumphold and its southern shelter; real harvested pads, ground travel across the dry bow to finite abandoned works stock, keyboard pack forging and equipping of the earned mallet/screen with exact submitted models, return, two-timber repair, two-dram preparation, and F5/unsaved-step/F6 literal replacement including gear. No transfers, item grants, edited clocks or AI suppression.";
        const string SoddenLimits="One predeclared seed and build, known destinations and bounded routes; not blind discovery, all-seed balance or immunity. The safe route is traversed; poison/fire/gas, typed electrical protection, learned combat skills and dressing application are checked separately in EditMode, not inflicted or granted by the harness. Screenshots require independent visual review. This journey does not visit the other two equipment sources.";
        static readonly string[] SoddenChecks={"ordinary_start","sodden_town_lead","sodden_broken_service","sodden_real_crop","sodden_dry_crossing","sodden_finite_works","sodden_equipment_preview","sodden_equipment_forged","sodden_equipment_equipped","sodden_repaired_return","sodden_paid_preparation","sodden_saved","sodden_loaded","sodden_finish"};
        string _soddenBench,_soddenSalvage,_soddenLocker,_soddenWorker;
        public void InitializeSoddenDistrict(ScenarioContext context)
        {
            if(string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))throw new InvalidOperationException("Isolated launcher required before Sodden bootstrap.");
            _soddenDistrict=true;Initialize(context,connectedBuild:"duelist",fieldwork:true);
        }
        Entity SoddenOwner(string blueprint)=>Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName==blueprint);
        void SoddenModel(Entity owner,string expected)
        {
            var presenter=_input.ZoneRenderer.SpawnRing3D;
            Require(presenter!=null&&presenter.TryGetEntityView(owner,out var view,out var model)&&model==expected&&presenter.IsRenderedEntity(owner),"actual current submitted Sodden model: "+expected);
            _observations.Add(new{phase="sodden-submitted-model",owner=owner.ID,blueprint=owner.BlueprintName,model=expected,zone=Zone.ZoneID});
        }
        IEnumerator SoddenEdge(bool east,string destination)
        {
            yield return DistrictWalk(Zone.GetCell(east?79:0,12),180);
            yield return Paid(Tap(Direction(east?1:-1,0)),"local","sodden-ground-boundary");
            Require(Zone.ZoneID==destination,"ordinary boundary reaches connected destination");
        }
        IEnumerator SoddenDryBow(bool east)
        {
            Require(Zone.ZoneID==SoddenDistrictPlan.CrossingZoneID,"actual crossing graph");
            foreach(var p in east?new[]{(20,12),(20,3),(60,3),(60,12)}:new[]{(60,12),(60,3),(20,3),(20,12)})
                yield return DistrictWalk(Zone.GetCell(p.Item1,p.Item2),100);
        }
        IEnumerator SoddenJourney()
        {
            Require(Manager.Exploration.Version>=14,"fresh district admission");
            yield return TravelSurface(SumpholdCompositionPlan.ZoneID);
            var toll=SoddenOwner("TollRolls");yield return DistrictApproach(toll,180);
            yield return WorldAction(toll,"Examine");yield return ReadPages("sodden-01-town-work-slip");yield return CloseNormal();
            Check("sodden_town_lead",NormalizeText(_readerText).Contains("DRESSING SHELTER")&&Zone.ZoneID==SumpholdCompositionPlan.ZoneID);
            yield return TravelSurface(SoddenDistrictPlan.StopZoneID);
            var bench=SoddenOwner("SoddenDressingBench");_soddenBench=bench.ID;_soddenWorker=bench.GetPart<SoddenPreparationPart>().Worker.ID;
            yield return DistrictApproach(bench,180);
            yield return WorldAction(bench,"Examine");yield return ReadPages("sodden-02-broken-bench-reader");yield return CloseNormal();
            Check("sodden_broken_service",!bench.GetPart<RepairablePart>().Repaired&&bench.GetPart<SoddenPreparationPart>().Configured&&Packed("SalvagedTimber")==0);
            yield return Capture("sodden-03-broken-shelter");
            SoddenModel(bench,"sodden-district-bench-broken");
            var crop=Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="SumpsieveCrop"&&e.GetPart<CropPart>().GrowthStage==2);
            string cropID=crop.ID;yield return ConnectedHarvest(crop,"SumpsievePad");
            Check("sodden_real_crop",Packed("SumpsievePad")==2&&Packed("SumpsieveSeed")==1&&CountGraphId(cropID)==0);
            BeginTiming("sodden-native-expedition-crossing-works-return");
            yield return SoddenEdge(true,SoddenDistrictPlan.CrossingZoneID);
            yield return SoddenDryBow(true);
            Check("sodden_dry_crossing",At.X==60&&At.Y==12&&Zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="MirePool")&&Zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="Bandfrog"));
            yield return Capture("sodden-04-dry-bow-crossing");
            yield return SoddenEdge(true,SoddenDistrictPlan.WorksZoneID);
            var locker=SoddenOwner("SoddenWorksLocker");_soddenLocker=locker.ID;
            yield return DistrictApproach(locker,200);var stock=locker.GetPart<ContainerPart>().Contents.ToArray();
            yield return Paid(DistrictTakeCache(locker,stock),"local","sodden-original-equipment-and-cord");
            var salvage=SoddenOwner("SoddenWorksSalvage");_soddenSalvage=salvage.ID;yield return DistrictApproach(salvage,180);
            yield return Capture("sodden-05a-sound-frame-before-salvage");SoddenModel(salvage,"sodden-district-works-salvage");
            yield return Paid(WorldAction(salvage,"Harvest"),"local","sodden-four-sound-timber");
            Check("sodden_finite_works",Packed("SalvagedTimber")==4&&Packed("KnotflaxCord")==2&&Packed("LeatherBoots")>=1&&Packed("Buckler")>=1
                &&locker.GetPart<ContainerPart>().Contents.Count==0&&CountGraphId(_soddenSalvage)==0);
            yield return Capture("sodden-05-looted-works");
            yield return EquipmentDiscoveryMallet();
            yield return SoddenEdge(false,SoddenDistrictPlan.CrossingZoneID);yield return SoddenDryBow(false);
            yield return SoddenEdge(false,SoddenDistrictPlan.StopZoneID);
            bench=Owner(_soddenBench);yield return DistrictApproach(bench,200);
            yield return Paid(WorldAction(bench,RepairablePart.RepairCommand),"local","sodden-two-timber-brace");
            Check("sodden_repaired_return",bench.GetPart<RepairablePart>().Repaired&&Packed("SalvagedTimber")==2&&bench.GetPart<SoddenPreparationPart>().Worker.ID==_soddenWorker);
            yield return Capture("sodden-06-restored-bench");
            SoddenModel(bench,"sodden-district-bench-working");
            EndTiming();
            int drams=TradeSystem.GetDrams(Player);
            yield return Paid(WorldAction(bench,SoddenPreparationPart.PrepareCommand),"local","sodden-paid-field-dressing");
            Check("sodden_paid_preparation",Packed("SoddenFieldDressing")==1&&Packed("SumpsievePad")==1&&Packed("KnotflaxCord")==1&&TradeSystem.GetDrams(Player)==drams-2);
            yield return WorldAction(bench,"Examine");yield return ReadPages("sodden-07-ready-service-reader");yield return CloseNormal();
            var player=Player;var zone=Zone;int tick=Tick,energy=Energy,x=At.X,y=At.Y;
            string file=SaveFile(),old=HashFile(file);yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(file);
            Check("sodden_saved",old!=_checkpointHash&&MessageLog.GetLast()=="Game saved.");
            var step=Steps.Select(d=>Zone.GetCell(x+d.x,y+d.y)).First(c=>Safe(Zone,c,ThreatClearance));yield return StepTo(step.X,step.Y);
            yield return Reload(player);
            var saved=Owner(_soddenBench);var works=Manager.GetZone(SoddenDistrictPlan.WorksZoneID);
            Check("sodden_loaded",Player!=player&&Zone!=zone&&saved!=bench&&saved.GetPart<RepairablePart>().Repaired
                &&saved.GetPart<SoddenPreparationPart>().Worker==Owner(_soddenWorker)&&Tick==tick&&Energy==energy&&At.X==x&&At.Y==y
                &&Packed("SoddenFieldDressing")==1&&Packed("SalvagedTimber")==2&&CountGraphId(_soddenSalvage)==0
                &&Player.GetStatValue("ElectricResistance")==50
                &&Player.GetPart<InventoryPart>().EquippedItems.Values.Any(e=>e.GetPart<WeaponAssemblyPart>()?.BladeBlueprint=="PeatMalletHeadComponent")
                &&works.GetReadOnlyEntities().Single(e=>e.ID==_soddenLocker).GetPart<ContainerPart>().Contents.Count==0);
            yield return Capture("sodden-08-saved-return");
            Check("sodden_finish",State=="Normal"&&Zone.ZoneID==SoddenDistrictPlan.StopZoneID&&!DevMode.Enabled&&!Player.HasPart<BitLockerPart>());
        }
    }
}
