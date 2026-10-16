using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    // Natural progression observer: all mutations use the existing native input
    // adapter. Queries choose current authored sources; they never fabricate one.
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _throwingNatural;
        int _throwingNaturalSeed, _throwingNaturalActionBudget;
        public int ThrowingNaturalPaidActions => NaturalActions;
        public string ThrowingNaturalOutcome => _throwingNaturalOutcome;
        string _throwingNaturalOutcome = "not-started";
        static readonly string[] ThrowingNaturalChecks = { "ordinary_start", "throwing_natural_starting_budget",
            "throwing_natural_finite_sources", "throwing_natural_earned_point", "throwing_natural_root_purchase",
            "throwing_natural_opportunity", "throwing_natural_checkpoint", "throwing_natural_bound" };
        const string ThrowingNaturalIntent = "Actual selected build and seed; original finite cellar clay, real kitchen repair and permission, commissioned meal, and returned dry crop earn the first point. Native Field Manual purchase of Sure Grip, then source-guided travel seeking one useful original-weapon throw against a live hostile. Original finite supplies, real enemy turns, no XP/item/target grants, transfers, time changes or resets.";
        const string ThrowingNaturalLimits = "Source-guided bounded observer, not unaided discovery or a guaranteed opening. Existing Safe/PathTo observes current wider terrain and threats for route selection; this is an omniscient safety-routing harness. Throw opportunity selection uses only known visible trajectory data. Seed29 WetCrossing and seed1729 HeavyFrame are previously censused; seed64 is held out for this throwing route. A failed route, absent source, death, retreat or unavailable throw is retained. A committed throw does not claim an accuracy retry, hit or superiority. This first-point route does not prove naturally earned child skills or 4/6SP mature pacing. No merchant supply or replenishment is assumed. Native keyboard input is not a physical Steam Deck test.";

        public void InitializeThrowingNatural(ScenarioContext context, string build, int seed, int actionBudget=700)
        {
            _throwingNatural = true; _throwingNaturalSeed = seed; _openingExpectedSeed = seed;
            _throwingNaturalActionBudget = Math.Min(2000, Math.Max(1, actionBudget));
            Initialize(context, connectedBuild: build);
        }
        int NaturalSide(int y) => (unchecked((uint)_throwingNaturalSeed) & 1u) == 0 ? y : 24-y;
        bool NaturalAlive => Player.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(Player);
        int NaturalActions => _localInputs + _mapSteps + _rests;
        Entity NaturalWeapon => Player.GetPart<InventoryPart>().Objects
            .Concat(Player.GetPart<InventoryPart>().EquippedItems.Values).FirstOrDefault(e => e.ID == _connectedOriginalDaggerId);
        string NaturalDestination => _connectedBuild == "breaker" && _throwingNaturalSeed == 1729
            ? "Overworld.15.10.0" : _connectedBuild == "bombardier" && _throwingNaturalSeed == 29
                ? "Overworld.13.12.0" : "Overworld.11.11.0";

        IEnumerator ThrowingNaturalJourney()
        {
            _throwingNaturalOutcome = "progressing";
            BeginTiming("throwing-natural-actual-travel-up-to60seconds"); _variantTiming = true;
            Check("throwing_natural_starting_budget", Player.GetStatValue("SP") == 0
                && Player.GetStatValue("Experience") == 0 && Player.GetStatValue("Level") == 1
                && !Player.GetPart<SkillsPart>().HasSkill("ThrowingSkill") && NaturalWeapon != null);
            _observations.Add(new { phase="throwing-natural-start", seed=Manager.WorldSeed, cohort=_connectedBuild,
                gear=Gear(Player), stats=Stats(Player), weapon=NaturalWeapon?.ID, weaponBlueprint=NaturalWeapon?.BlueprintName,
                sourceGuided=true, priorCensusKnown=_throwingNaturalSeed==29 || _throwingNaturalSeed==1729,
                destination=NaturalDestination, grants=Player.GetPart<SkillsPart>().SkillList.Select(s=>s.GetType().Name).ToArray() });
            yield return Capture("throwing-natural-01-actual-start");
            try
            {
                yield return ThrowingNaturalEarnPoint();
                int before=Player.GetStatValue("SP");
                Check("throwing_natural_earned_point", NaturalAlive && Player.GetStatValue("Level") >= 2 && before >= 1);
                Require(before >= 1, "first point earned by actual source actions or recorded combat");
                yield return OpeningInspectSkill("ThrowingSkill", false, true);
                Check("throwing_natural_root_purchase", Player.GetPart<SkillsPart>().HasSkill("ThrowingSkill")
                    && Player.GetStatValue("SP") == before-1);
                _observations.Add(new { phase="throwing-natural-purchase", actions=NaturalActions, tick=Tick,
                    worldTick=WorldClock.CurrentTick, hp=Player.GetStatValue("Hitpoints"), level=Player.GetStatValue("Level"),
                    experience=Player.GetStatValue("Experience"), banked=Player.GetStatValue("SP"),
                    sourceAwards=Player.GetPart<ConnectedSpreadProgressPart>()?.Awards, originalWeapon=NaturalWeapon?.ID });
                yield return Capture("throwing-natural-02-earned-sure-grip");
                yield return TravelSurface(NaturalDestination);
                var site=Manager.Exploration.Find(NaturalDestination);
                _observations.Add(new { phase="throwing-natural-current-site", zone=Zone.ZoneID,
                    disposition=Manager.Exploration.DispositionFor(NaturalDestination), family=site?.Family.ToString(),
                    caches=Zone.GetReadOnlyEntities().Where(e=>e.HasPart<ContainerPart>())
                        .Select(e=>new { e.ID, e.BlueprintName, contents=e.GetPart<ContainerPart>().Contents.Select(i=>new{i.ID,i.BlueprintName,units=Units(i)}).ToArray() }).ToArray(),
                    note="Observed actual site and finite sources; no stock granted or consumed by observation." });
                yield return ThrowingNaturalSeekOpportunity();
                yield return ThrowingNaturalCheckpoint();
                yield return ThrowingNaturalContinueCampaign();
                Check("throwing_natural_bound", NaturalActions <= _throwingNaturalActionBudget && NaturalAlive
                    && !DevMode.Enabled && !Player.HasPart<BitLockerPart>() && !DebugInvincibility.IsEnabled(Player));
            }
            finally
            {
                _variantTiming = false; EndTiming();
                _observations.Add(new { phase="throwing-natural-final", outcome=_throwingNaturalOutcome,
                    seed=_throwingNaturalSeed, cohort=_connectedBuild, actions=NaturalActions, actionBudget=_throwingNaturalActionBudget, hp=Player.GetStatValue("Hitpoints"),
                    level=Player.GetStatValue("Level"), experience=Player.GetStatValue("Experience"), banked=Player.GetStatValue("SP"),
                    gear=Gear(Player), skills=Player.GetPart<SkillsPart>().SkillList.Select(s=>s.GetType().Name).ToArray(),
                    childPacing="Only recorded native purchases verify acquisition", matureFourSixSP="Only recorded continuation checkpoints verify mature pacing; no controlled XP substituted",
                    merchantSupply=_throwingVendorAttempted?"See actual optional vendor observations; no stock, affordability or restock is inferred.":"No vendor acquisition observed by this source circuit." });
            }
        }

        IEnumerator NaturalEnterCellar(bool first)
        {
            Require(Zone.ZoneID == GleanersDistrict.SurfaceID, "actual glade before cellar descent");
            yield return DistrictWalk(Zone.GetCell(28,19),120);
            yield return Paid(Tap(Key.LeftShift,Key.Period),"local","throwing-natural-cellar-down");
            Require(Zone.ZoneID == GleanersCellarBuilder.ZoneID, "actual cellar reached");
            if(first)
            {
                int storeX=62+(int)(unchecked((uint)_throwingNaturalSeed)%3u)*2;
                foreach(var p in new[]{(40,5),(45,5),(46,4),(46,2),(50,2),(50,5),(storeX,5),(storeX-1,6)})
                    yield return DistrictWalk(Zone.GetCell(p.Item1,NaturalSide(p.Item2)),100);
            }
            else
            {
                var dry=DistrictOwner(Zone,GleanersCellarBuilder.RoleKey,"sootroot-dry");
                yield return DistrictApproach(dry,160);
            }
        }
        IEnumerator NaturalLeaveCellar()
        {
            Require(Zone.ZoneID == GleanersCellarBuilder.ZoneID, "actual cellar departure");
            foreach(var p in new[]{(50,5),(50,2),(46,2),(46,4),(45,5),(40,5),(40,12)})
                yield return DistrictWalk(Zone.GetCell(p.Item1,NaturalSide(p.Item2)),110);
            yield return Paid(Tap(Key.LeftShift,Key.Comma),"local","throwing-natural-cellar-up");
            Require(Zone.ZoneID == GleanersDistrict.SurfaceID,"actual stairs return home");
        }
        IEnumerator ThrowingNaturalEarnPoint()
        {
            yield return ConnectedGleanGrain();
            yield return NaturalEnterCellar(true);
            var crate=DistrictOwner(Zone,GleanersCellarBuilder.RoleKey,"supplies");
            var contents=crate.GetPart<ContainerPart>().Contents.ToArray();
            Require(DistrictClay()==0 && contents.Where(e=>e.BlueprintName=="FireClay").Sum(Units)==2,
                "original finite clay, not supplied to make progression pass");
            _connectedCrateId=crate.ID;
            yield return Paid(DistrictTakeCache(crate,contents),"local","throwing-natural-original-cellar-goods");
            Check("throwing_natural_finite_sources", DistrictClay()==2 && crate.GetPart<ContainerPart>().Contents.Count==0
                && contents.All(e=>Owns(Player,e)));
            var dry=DistrictOwner(Zone,GleanersCellarBuilder.RoleKey,"sootroot-dry"); _connectedDryId=dry.ID;
            Require(dry.GetPart<CropPart>().GrowthStage==0 && dry.GetPart<CropPart>().MoistureTicks==0,"original dry crop");
            yield return ConnectedWater(dry); yield return NaturalLeaveCellar();
            yield return TravelSurface(KitchenBatchPart.KitchenZoneID);
            var pan=Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="ConnectedBatchPan"); _connectedPanId=pan.ID;
            yield return DistrictApproach(pan,160);
            yield return Paid(WorldAction(pan,RepairablePart.RepairCommand),"local","throwing-natural-clay-repair");
            Require(pan.GetPart<RepairablePart>().Repaired && DistrictClay()==0,"real material paid for repair");
            var cook=pan.GetPart<KitchenBatchPart>().Worker;yield return DistrictApproach(cook,40);
            yield return Paid(WorldAction(cook,CookIntroductionPart.IntroductionCommand),"local","throwing-natural-introduction");
            var clasp=Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="ClaspbeanCrop"&&e.GetProperty("ConnectedSpread.Role")=="kitchen-crop");
            yield return ConnectedHarvest(clasp,"ClaspbeanPulp");
            yield return TravelSurface(LocalGatheringClaimPart.ReserveZoneID);
            var keeper=Zone.GetReadOnlyEntities().Single(e=>e.HasPart<LocalGatheringClaimPart>()); _connectedKeeperId=keeper.ID;
            yield return DistrictApproach(keeper,160);
            yield return Paid(WorldAction(keeper,"ReserveAccess:introduction"),"local","throwing-natural-permission");
            Require(keeper.GetPart<LocalGatheringClaimPart>().GetState(Player)==ReserveAccessState.Granted,"actual keeper permission");
            yield return ConnectedGleanGrain();
            Require(PackedGrain()==2&&Packed("ClaspbeanPulp")==2,"finite gathered ingredients");
            yield return TravelSurface(KitchenBatchPart.KitchenZoneID);pan=ConnectedPan;
            yield return DistrictApproach(pan,160);int purse=TradeSystem.GetDrams(Player);
            yield return Paid(WorldAction(pan,KitchenBatchPart.StartCommand),"local","throwing-natural-paid-commission");
            Require(pan.GetPart<KitchenBatchPart>().State=="Working"&&TradeSystem.GetDrams(Player)==purse-2,"real paid commission");
            yield return TravelSurface(GleanersDistrict.SurfaceID);yield return NaturalEnterCellar(false);
            if(ConnectedDry.GetPart<CropPart>().GrowthStage<2)yield return ConnectedWater(ConnectedDry);
            yield return NaturalLeaveCellar();yield return TravelSurface(KitchenBatchPart.KitchenZoneID);
            var batch=ConnectedPan.GetPart<KitchenBatchPart>();yield return DistrictApproach(batch.Pickup,160);
            Require(batch.State=="Ready"&&batch.Output!=null,"ordinary travel completed meal, no time injected");
            var meal=batch.Output;yield return Paid(TakeSole(batch.Pickup,meal.ID),"local","throwing-natural-meal-collected");
            Require(Owns(Player,meal),"actual commissioned meal retained");
            yield return TravelSurface(GleanersDistrict.SurfaceID);yield return NaturalEnterCellar(false);
            Require(ConnectedDry.GetPart<CropPart>().GrowthStage==2,"same naturally matured dry crop");
            yield return ConnectedHarvest(ConnectedDry,"SootrootPulp");
            Require(Player.GetPart<ConnectedSpreadProgressPart>()?.Awards==15,"four real finite source awards");
            yield return NaturalLeaveCellar();
        }
        Entity NaturalThrowTarget(Entity weapon)
        {
            if(weapon==null)return null;
            int range=HandlingService.GetThrowRange(Player,weapon);
            return Threats(Zone).Where(e=>!ReferenceGladeRouteControl.HasLiveCalm(Zone,e))
                .Where(e=>SpatialQuery.Distance(Zone,Player,e)>1 && SpatialQuery.Distance(Zone,Player,e)<=range)
                .OrderBy(e=>SpatialQuery.Distance(Zone,Player,e)).FirstOrDefault(e=>
                {
                    var cell=Zone.GetEntityCell(e);
                    if(!ThrowTrajectory.IsKnownOwner(cell,e))return false;
                    var trace=ThrowTrajectory.Trace(Zone,Player,weapon,ThrowIntent.AtEntity(e,cell.X,cell.Y),range,knownOnly:true);
                    return !trace.Uncertain&&trace.HitEntity==e;
                });
        }
        IEnumerator ThrowingNaturalSeekOpportunity()
        {
            var weapon=NaturalWeapon;Require(weapon!=null,"same finite original physical weapon still owned");
            Entity target=NaturalThrowTarget(weapon);
            // Route toward an existing visible hostile only through the ordinary
            // safe pathfinder. A missing route remains an observed miss.
            for(int step=0;target==null && NaturalAlive && step<80;step++)
            {
                var candidate=Threats(Zone).Where(e=>!ReferenceGladeRouteControl.HasLiveCalm(Zone,e))
                    .Where(e=>Zone.GetEntityCell(e)?.IsVisible==true).OrderBy(e=>SpatialQuery.Distance(Zone,Player,e)).FirstOrDefault();
                if(candidate==null)
                {
                    // The destination itself is source-guided, not a hidden-enemy
                    // reveal. Walk toward an existing finite cache and reassess
                    // only currently visible hostiles after every real step.
                    var cache=Zone.GetReadOnlyEntities().Where(e=>e.HasPart<ContainerPart>())
                        .OrderBy(e=>SpatialQuery.Distance(Zone,Player,e)).FirstOrDefault();
                    if(cache==null)break;
                    var route=PathTo(c=>SpatialQuery.DistanceToCell(Zone,cache,c.X,c.Y)<=2);
                    if(route==null||route.Count==0)break;
                    yield return StepTo(route[0].x,route[0].y); target=NaturalThrowTarget(weapon); continue;
                }
                var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,candidate,c.X,c.Y)>1
                    && SpatialQuery.DistanceToCell(Zone,candidate,c.X,c.Y)<=HandlingService.GetThrowRange(Player,weapon)
                    && AIHelpers.HasLineOfSight(Zone,c.X,c.Y,Zone.GetEntityCell(candidate).X,Zone.GetEntityCell(candidate).Y));
                if(path==null||path.Count==0)break;
                yield return StepTo(path[0].x,path[0].y); target=NaturalThrowTarget(weapon);
            }
            if(target==null)
            {
                _throwingNaturalOutcome="no-useful-visible-throw";
                _notes.Add("Actual route produced no lawful useful physical ray within 80 approach actions; no target created or repositioned.");
                Check("throwing_natural_opportunity",false);yield return Capture("throwing-natural-no-opportunity");yield break;
            }
            int pointBefore=Player.GetStatValue("SP"),hpBefore=target.GetStatValue("Hitpoints");string id=weapon.ID;
            var aim=Zone.GetEntityCell(target);int tx=aim.X,ty=aim.Y;string marker=Mark("throwing-natural-first-earned-use");
            yield return ItemAction(weapon,"throw");Require(State=="ThrowTargeting","real original inventory weapon throw");
            var cursor=(WorldCursorState)Field(_input,"_worldCursorState");
            for(int n=0;cursor.X!=tx||cursor.Y!=ty;n++)
            {Require(n<110,"bounded native throwing cursor");yield return Tap(cursor.X<tx?Key.RightArrow:cursor.X>tx?Key.LeftArrow:cursor.Y<ty?Key.DownArrow:Key.UpArrow);}
            yield return Capture("throwing-natural-03-useful-native-aim");
            int serial=Player.GetPart<ThrowingStatePart>().ThrowSerial;
            yield return Paid(Tap(Key.Enter),"local","throwing-natural-real-physical-throw");
            bool committed=Player.GetPart<ThrowingStatePart>().ThrowSerial==serial+1;
            Check("throwing_natural_opportunity",committed);
            _throwingNaturalOutcome=committed?"earned-root-used":"throw-refused";
            _observations.Add(new { phase="throwing-natural-first-use", weapon=id, blueprint=weapon.BlueprintName,
                target=target.ID, targetBlueprint=target.BlueprintName, targetHpBefore=hpBefore,targetHpAfter=target.GetStatValue("Hitpoints"),
                pointBefore,pointAfter=Player.GetStatValue("SP"),beforeNextPoint=pointBefore==Player.GetStatValue("SP"),
                actions=NaturalActions,committed,rows=Window(marker), custody=weapon.GetPart<PhysicsPart>()?.InInventory?.ID,
                equipment=weapon.GetPart<PhysicsPart>()?.Equipped?.ID, position=Zone.GetEntityCell(weapon)?.ToString() });
            yield return Capture("throwing-natural-04-actual-aftermath");
        }
        IEnumerator ThrowingNaturalCheckpoint(bool initial=true)
        {
            var oldPlayer=Player;string id=Player.ID,stats=Stats(Player),gear=Gear(Player),skills=OpeningSkillState();
            string file=SaveFile(),oldHash=HashFile(file);int tick=Tick,energy=Energy;
            yield return Tap(Key.F5);yield return ConnectedCloseAndStabilize();
            Require(HashFile(file)!=oldHash&&MessageLog.GetLast()=="Game saved.","natural route saves actual acquired state");
            yield return Reload(oldPlayer);
            bool preserved=Player.ID==id&&Stats(Player)==stats&&Gear(Player)==gear
                &&OpeningSkillState()==skills&&Tick==tick&&Energy==energy;
            if(initial)Check("throwing_natural_checkpoint",preserved);
            else Require(preserved,"actual continuation checkpoint restores retained losses and purchases");
            yield return Capture("throwing-natural-05-retained-earned-checkpoint");
        }
    }
}
