using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        // Ordinary source-bound progression. Observations select existing owners;
        // all changes happen through the same native input path as the short opening.
        int _openingDefenseRolls, _openingParries, _openingGuarded;
        string _openingDefeatedGuardId, _openingDefeatedZoneId;
        IEnumerator OpeningEarnedPurchaseJourney()
        {
            Require(_connectedBuild=="warden"&&Zone.ZoneID==GleanersDistrict.SurfaceID,"Warden returned to glade");
            yield return ConnectedGleanGrain();
            yield return OpeningReachDryBed();
            var dry=DistrictOwner(Zone,GleanersCellarBuilder.RoleKey,"sootroot-dry");
            _connectedDryId=dry.ID;
            Require(dry.GetPart<CropPart>().GrowthStage==0&&dry.GetPart<CropPart>().MoistureTicks==0,"original unwatered bed");
            yield return ConnectedWater(dry);yield return ConnectedLeaveCellar();

            yield return TravelSurface(KitchenBatchPart.KitchenZoneID);
            var pan=Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="ConnectedBatchPan");_connectedPanId=pan.ID;
            yield return DistrictApproach(pan,140);
            yield return Paid(WorldAction(pan,RepairablePart.RepairCommand),"local","opening-original-clay-repair");
            Require(pan.GetPart<RepairablePart>().Repaired&&DistrictClay()==0,"both actual clay spent repairing pan");
            var cook=pan.GetPart<KitchenBatchPart>().Worker;yield return DistrictApproach(cook,30);
            yield return Paid(WorldAction(cook,CookIntroductionPart.IntroductionCommand),"local","opening-earned-introduction");
            var clasp=Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="ClaspbeanCrop"&&e.GetProperty("ConnectedSpread.Role")=="kitchen-crop");
            yield return ConnectedHarvest(clasp,"ClaspbeanPulp");
            yield return Capture("opening-progression-repaired-kitchen");

            yield return TravelSurface(LocalGatheringClaimPart.ReserveZoneID);
            var keeper=Zone.GetReadOnlyEntities().Single(e=>e.HasPart<LocalGatheringClaimPart>());_connectedKeeperId=keeper.ID;
            yield return DistrictApproach(keeper,140);
            yield return Paid(WorldAction(keeper,"ReserveAccess:introduction"),"local","opening-reserve-permission");
            Require(keeper.GetPart<LocalGatheringClaimPart>().GetState(Player)==ReserveAccessState.Granted,"real keeper permission");
            yield return ConnectedGleanGrain();
            Require(PackedGrain()==2&&Packed("ClaspbeanPulp")==2,"finite harvested ingredients");
            yield return TravelSurface(KitchenBatchPart.KitchenZoneID);pan=ConnectedPan;
            yield return DistrictApproach(pan,140);int drams=TradeSystem.GetDrams(Player);
            yield return Paid(WorldAction(pan,KitchenBatchPart.StartCommand),"local","opening-paid-meal-commission");
            Require(pan.GetPart<KitchenBatchPart>().State=="Working"&&TradeSystem.GetDrams(Player)==drams-2&&PackedGrain()==0,"paid real commission");

            yield return TravelSurface(GleanersDistrict.SurfaceID);yield return OpeningReachDryBed();
            Require(ConnectedDry?.GetPart<CropPart>()?.GrowthStage<2,"first watering does not skip full growth");
            yield return ConnectedWater(ConnectedDry);yield return ConnectedLeaveCellar();
            yield return TravelSurface(KitchenBatchPart.KitchenZoneID);var batch=ConnectedPan.GetPart<KitchenBatchPart>();
            yield return DistrictApproach(batch.Pickup,140);
            Require(batch.State=="Ready"&&batch.Output!=null,"real travel completed meal");var meal=batch.Output;
            yield return Paid(TakeSole(batch.Pickup,meal.ID),"local","opening-original-meal-collected");
            Require(Owns(Player,meal)&&batch.Pickup.GetPart<ContainerPart>().Contents.Count==0,"actual commissioned meal retained");
            Require(Player.GetPart<ConnectedSpreadProgressPart>()?.Awards==11,"three finite awards before original dry harvest");
            yield return TravelSurface(GleanersDistrict.SurfaceID);yield return OpeningReachDryBed();
            Require(ConnectedDry?.GetPart<CropPart>()?.GrowthStage==2,"same original dry crop matured on real travel");
            yield return ConnectedHarvest(ConnectedDry,"SootrootPulp");
            Check("opening_finite_progression",Player.GetPart<ConnectedSpreadProgressPart>()?.Awards==15
                &&ConnectedDry==null&&Player.GetStatValue("Level")>=2&&Player.GetStatValue("SP")==1);
            _notes.Add("Four source-bound accomplishments supply 120 XP. Actual level="+Player.GetStatValue("Level")+", remaining XP toward next level="+Player.GetStatValue("Experience")+". Other ordinary play, including combat, may contribute XP. First earned SP="+Player.GetStatValue("SP")+".");
            yield return Capture("opening-progression-earned-first-point");
            yield return ConnectedLeaveCellar();
            yield return OpeningInspectSkill("LongBlades_Deflect",false,true);
            Check("opening_earned_deflect_purchase",Player.GetPart<SkillsPart>().SkillList.Single(s=>s.GetType().Name=="LongBlades_Deflect").Rank==1
                &&Player.GetStatValue("SP")==0&&!Player.GetPart<SkillsPart>().HasSkill("LongBlades_Riposte"));
            // Return to the same still-live cellar owner with the purchased passive.
            // Original equipment, normal guard/attacks and finite tonics only.
            yield return OpeningReachDryBed();
            _guard=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.GetProperty(GleanersCellarBuilder.RoleKey)=="guard");
            if(_guard==null||CombatSystem.IsDeathHandled(_guard))
            {
                _notes.Add("Original cellar opponent was already defeated during ordinary travel. Earned-defense fight uses the actual admitted yard defender; no enemy is respawned.");
                yield return ConnectedLeaveCellar();yield return TravelSurface("Overworld.11.11.0");
                _guard=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.HasPart<WorksiteGuardPart>()&&!CombatSystem.IsDeathHandled(e));
            }
            Require(_guard!=null&&!CombatSystem.IsDeathHandled(_guard),"actual admitted original opponent for earned-defense fight");
            _guardId=_guard.ID;_openingDefeatedZoneId=Zone.ZoneID;
            _guardAttempt=_guardDamage=_guardLethal=false;
            _openingDefenseRolls=_openingParries=_openingGuarded=0;
            yield return FightGuard(false);
            _openingDefeatedGuardId=_guardId;
            _notes.Add("Earned defense fight: "+_openingDefenseRolls+" qualifying defense rolls, "+_openingParries+" full parries, "+_openingGuarded+" guarded blows. These are observations, not forced branch coverage. Exact action windows retain the rolls.");
            Check("opening_earned_defense_fight",_guardAttempt&&_guardDamage&&CombatSystem.IsDeathHandled(_guard)
                &&StartingBuildService.PrimaryHandWeapon(Player)?.ID==_connectedOriginalDaggerId);
            yield return Capture("opening-earned-defense-fight");
            if(Zone.ZoneID==GleanersCellarBuilder.ZoneID)yield return ConnectedLeaveCellar();
            else yield return TravelSurface(GleanersDistrict.SurfaceID);
        }

        IEnumerator OpeningReachDryBed()
        {
            Require(Manager.WorldSeed==64&&Zone.ZoneID==GleanersDistrict.SurfaceID,"fixed ordinary progression route");
            yield return DistrictWalk(Zone.GetCell(28,19),100);
            yield return Paid(Tap(Key.LeftShift,Key.Period),"local","opening-return-to-original-dry-bed");
            Require(Zone.ZoneID==GleanersCellarBuilder.ZoneID,"actual cellar re-entry");
            // Revisited actors may occupy the old detour. Route to the actual
            // bed from current geometry, not compulsory intermediate squares.
            var dry=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.GetProperty(GleanersCellarBuilder.RoleKey)=="sootroot-dry");
            if(dry!=null)yield return DistrictApproach(dry,140);
        }

        string OpeningCellarDepletion()
        {
            var cellar=Manager.CachedZones[_openingDefeatedZoneId];
            Require(!string.IsNullOrEmpty(_openingDefeatedGuardId)&&!cellar.GetReadOnlyEntities().Any(e=>e.ID==_openingDefeatedGuardId),"defeated original guard remains absent");
            // Includes actual corpse/drop identities, amounts and positions; a reload
            // cannot silently regenerate the owner or replace its physical remains.
            return _openingDefeatedZoneId+"/"+string.Join(";",cellar.GetReadOnlyEntities().OrderBy(e=>e.ID).Select(e=>
                e.ID+":"+e.BlueprintName+":"+cellar.GetEntityPosition(e)+":"+e.GetStatValue("Hitpoints")+":"+Units(e)));
        }

        string OpeningProgressionDigest()
        {
            if(!OpeningProgression)return "";
            var pan=ConnectedPan;var batch=pan.GetPart<KitchenBatchPart>();
            var rows=Manager.CachedZones.OrderBy(p=>p.Key).SelectMany(p=>p.Value.GetReadOnlyEntities()
                .Where(e=>_connectedGrainIds.Contains(e.ID)).OrderBy(e=>e.ID).Select(e=>p.Key+":"+e.ID+":"+e.GetPart<FieldHarvestPart>().Harvested));
            return string.Join("|",Player.GetPart<ConnectedSpreadProgressPart>().Awards,pan.ID,pan.GetPart<RepairablePart>().Repaired,
                batch.State,batch.JobOrdinal,batch.Pickup.ID,batch.Pickup.GetPart<ContainerPart>().Contents.Count,
                batch.Escrow.GetPart<ContainerPart>().Contents.Count,_connectedDryId,ConnectedDry==null,
                TradeSystem.GetDrams(Player),string.Join(";",rows),_connectedKeeperId,ConnectedKeeper.GetPart<LocalGatheringClaimPart>().GetState(Player),
                OpeningCellarDepletion());
        }
    }
}
