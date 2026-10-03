using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Short actual-build satellite expeditions. Seeds were chosen from
    /// the recorded source census, so these are targeted variant acceptance,
    /// not a blind random-world sample. Never creates or moves an owner directly.</summary>
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool ConnectedVariant => _connectedBuild == "breaker" || _connectedBuild == "bombardier";
        bool VariantBreaker => _connectedBuild == "breaker";
        int VariantSeed => VariantBreaker ? 1729 : 29;
        string VariantZone => VariantBreaker ? "Overworld.15.10.0" : "Overworld.13.12.0";
        string _variantCacheId, _variantFrameId, _variantManualId, _variantKeeperId, _variantHarvestedId;
        string _variantWeaponId;
        string[] _variantYieldIds=Array.Empty<string>();
        int _variantDefenses;
        bool _variantTiming;
        static readonly string[] VariantCommonChecks = { "ordinary_start", "variant_actual_source", "variant_source_choice",
            "variant_finite_cache", "variant_manual_learned", "variant_final_save_restore", "variant_finish" };
        string[] VariantChecks => VariantCommonChecks.Concat(VariantBreaker
            ? new[] { "variant_frame_pulled", "variant_cleared_crossing", "variant_clay_well" }
            : new[] { "variant_witnessed_breach", "variant_ground_throw", "variant_breach_save_restore", "variant_local_return", "variant_restitution" }).ToArray();
        const string VariantIntent = "Actual selected Breaker/seed1729 or Bombardier/seed29; ordinary native map/ground trip to one census-observed accepted satellite. Breaker uses its own strength to pull a real heavy frame, crosses the cleared aperture, retrieves finite clay/manual and repairs the original glade well. Bombardier spends one original tonic only with a useful lawful target or preserves it and uses a live alternate approach, retrieves finite grain/manual, commits an actually witnessed local reserve breach, saves/reloads it, leaves/returns and pays restitution using earned grain. Native reading, depletion and final replacement-graph save checks. Up to60seconds of observational frame sampling during useful travel, never clock-padding.";
        const string VariantLimits = "Targeted source-guided seeds selected after census; not blind five-seed coverage, unaided discovery, encounter balance or all-seed proof. No grants, player transfers, target setup, population edits, source replenishment or time changes. Breaker uses actual starting Cudgel/finite tonics if an adjacent hostile forces defense. Bombardier may preserve a throwable when no lawful useful ray exists; expenditure and bypass are separately reported. Witness failure is a retained acceptance failure, never fabricated permission. Native timing includes paced keys, turn/AI, screenshots and audit IO; no prior-build comparison, pure renderer cost, steady-state benchmark or GC-allocation claim. Shorter-than60second routes are reported honestly. Screenshots still need independent visual review.";

        IEnumerator ConnectedVariantChooseBuild()
        {
            var menu=(StartingBuildMenuController)Field(_input,"_buildMenuController");
            Require(menu.IsOpen,"actual variant starting-build picker");
            int index=menu.Model.Options.ToList().FindIndex(b=>b.Id==_connectedBuild);
            Require(index>=0&&index<9,"requested variant build card exists");
            yield return Capture("variant-00-build-picker");
            yield return Tap(Shortcut("Digit"+(index+1)));Require(menu.SelectedIndex==index,"actual variant card selected");
            var definition=menu.Model.Selected;yield return Tap(Key.Enter);yield return ConnectedCloseAndStabilize();
            var weapon=StartingBuildService.PrimaryHandWeapon(Player);
            _variantWeaponId=weapon?.ID;
            Check("ordinary_start",!menu.IsOpen&&Player.GetProperty(StartingBuildService.PropertyName)==_connectedBuild
                &&Manager.WorldSeed==VariantSeed&&Zone.ZoneID==GleanersDistrict.SurfaceID
                &&Player.GetStatValue("Hitpoints")==40&&Player.GetStatValue("Level")==1
                &&Player.GetStatValue("Strength")==definition.Attributes.Strength&&Player.GetStatValue("Agility")==definition.Attributes.Agility
                &&weapon?.BlueprintName==(VariantBreaker?"Cudgel":"Dagger")&&weapon.GetPart<PhysicsPart>()?.Equipped==Player
                &&Packed("HealingTonic")==2&&Packed("WateringGrimoire")==1&&TradeSystem.GetDrams(Player)==50
                &&(VariantBreaker||Packed("LightningTonic")==1&&Packed("FrostTonic")==1&&Packed("PoisonGasGrenade")==1)
                &&!DevMode.Enabled&&!Player.HasPart<BitLockerPart>()&&!DebugInvincibility.IsEnabled(Player));
            _districtOriginalCalm=Player.GetPart<ActivatedAbilitiesPart>()?.AbilityList.SingleOrDefault(a=>a.Command=="CommandCalm")?.ID??Guid.Empty;
            _districtPreparing=true;_connectedVisitedSurfaces.Add(Zone.ZoneID);
            _notes.Add("TARGETED VARIANT: actual "+definition.Id+", seed"+VariantSeed+" was selected after the published source census, not before observing placement.");
        }

        IEnumerator ConnectedVariantJourney()
        {
            yield return Capture("variant-01-ordinary-start");
            BeginTiming("targeted-variant-native-travel-up-to60seconds");_variantTiming=true;
            try
            {
                yield return TravelSurface(VariantZone);
                var cache=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.GetPart<ContainerPart>()?.Contents.Any(i=>i.BlueprintName=="DitchkeepersFootwork")==true);
                Require(cache!=null,"actual naturally committed finite satellite cache, never replacement generation");
                _variantCacheId=cache.ID;_variantManualId=cache.GetPart<ContainerPart>().Contents.Single(e=>e.BlueprintName=="DitchkeepersFootwork").ID;
                var planned=Manager.Exploration.Find(VariantZone);
                Check("variant_actual_source",Manager.Exploration.DispositionFor(VariantZone)==2
                    &&planned?.Family.ToString()==(VariantBreaker?"HeavyFrame":"WetCrossing")
                    &&!Player.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault"));
                yield return Capture("variant-02-current-satellite");
                if(VariantBreaker)yield return ConnectedVariantHeavy();
                else yield return ConnectedVariantWet();
                yield return DistrictApproach(cache,160);
                var goods=cache.GetPart<ContainerPart>().Contents.ToArray();
                int clay=DistrictClay(),grain=Packed("Emberwheat"),pulp=Packed("ClaspbeanPulp");
                int clayAdded=goods.Where(e=>e.BlueprintName=="FireClay").Sum(Units),grainAdded=goods.Where(e=>e.BlueprintName=="Emberwheat").Sum(Units),pulpAdded=goods.Where(e=>e.BlueprintName=="ClaspbeanPulp").Sum(Units);
                Require(VariantBreaker?clayAdded>=2:grainAdded>=2&&pulpAdded>=1,"actual declared finite useful supplement exists");
                yield return Paid(DistrictTakeCache(cache,goods),"local","variant-actual-finite-cache");
                Check("variant_finite_cache",cache.GetPart<ContainerPart>().Contents.Count==0&&DistrictClay()==clay+clayAdded
                    &&Packed("Emberwheat")==grain+grainAdded&&Packed("ClaspbeanPulp")==pulp+pulpAdded
                    &&Packed("DitchkeepersFootwork")==1&&CountGraphId(_variantManualId)==1);
                var manual=Player.GetPart<InventoryPart>().Objects.Single(e=>e.ID==_variantManualId);
                int tick=Tick,energy=Energy,sp=Player.GetStatValue("SP");
                yield return ItemAction(manual,"ReadGrimoire");yield return CloseNormal();
                Check("variant_manual_learned",Player.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault")&&Owns(Player,manual)
                    &&manual.GetPart<GrimoireChargePart>()==null&&Tick==tick&&Energy==energy&&Player.GetStatValue("SP")==sp);
                yield return Capture("variant-03-earned-manual");
                if(VariantBreaker)
                {
                    yield return TravelSurface(GleanersDistrict.SurfaceID);
                    var well=DistrictOwner(Zone,GleanersDistrict.RoleKey,"well");Require(!well.GetPart<RepairablePart>().Repaired,"same original broken glade well");
                    yield return DistrictApproach(well,140);clay=DistrictClay();
                    yield return Paid(WorldAction(well,RepairablePart.RepairCommand),"local","variant-clay-to-public-well");
                    Check("variant_clay_well",well.GetPart<RepairablePart>().Repaired&&DistrictClay()==clay-2);
                    yield return Capture("variant-04-clay-choice-well");
                }
                else yield return ConnectedVariantConsequence();
                yield return ConnectedVariantCheckpoint("variant_final_save_restore");
                Check("variant_finish",Player.GetProperty(StartingBuildService.PropertyName)==_connectedBuild&&Player.GetStatValue("Hitpoints")>10
                    &&!DevMode.Enabled&&!DebugInvincibility.IsEnabled(Player)&&!Player.HasPart<BitLockerPart>()&&_localInputs<=420&&_mapSteps<=16);
                yield return Capture("variant-09-finished");
            }
            finally
            {
                _variantTiming=false;EndTiming();
                _observations.Add(new{phase="variant-observational-performance-bound",retainedGraphCount=Manager?.CachedZoneCount,
                    elapsedSeconds=_clock.Elapsed.TotalSeconds,localInputs=_localInputs,mapSteps=_mapSteps,
                    bound="Frame window ended at60seconds or earlier actual route completion; no idle or world-turn padding, no baseline comparison. Timing includes audit IO, screenshots and paced input."});
            }
        }

        IEnumerator ConnectedVariantHeavy()
        {
            var frame=Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="ConnectedHeavyFrame");_variantFrameId=frame.ID;
            var original=Zone.GetEntityPosition(frame);var approach=Zone.GetCell(original.x,original.y-1);
            Require(frame.GetPart<HandlingPart>().Weight==136&&Player.GetStatValue("Strength")==20&&DragRules.CanDrag(Player,frame)==DragVerdict.Ok,"actual Breaker strength admits original frame");
            yield return DistrictWalk(approach,160);
            int speed=Player.GetStatValue("Speed"),tick=Tick,energy=Energy;
            yield return WorldAction(frame,HandlingPart.HaulCommand);yield return CloseNormal();
            Require(DragSystem.GetDragged(Player)==frame&&Tick==tick&&Energy==energy&&Player.GetStatValue("Speed")<speed,"native free grip applies only real haul penalty");
            Check("variant_source_choice",DragSystem.IsDragging(Player)&&StartingBuildService.PrimaryHandWeapon(Player)?.ID==_variantWeaponId);
            for(int n=1;n<=2;n++)
            {
                var previous=At;yield return StepTo(At.X,At.Y-1);
                Require(Zone.GetEntityCell(frame)==previous,"real pulled frame follows previous player cell");
            }
            Check("variant_frame_pulled",Zone.GetEntityPosition(frame)==(original.x,original.y-2)&&At.X==original.x&&At.Y==original.y-3);
            tick=Tick;energy=Energy;yield return WorldAction(frame,HandlingPart.ReleaseCommand);yield return CloseNormal();
            Require(!DragSystem.IsDragging(Player)&&frame.GetPart<DraggedPart>()==null&&Player.GetStatValue("Speed")==speed&&Tick==tick&&Energy==energy,"native release restores speed without a turn");
            yield return Capture("variant-frame-parked-open-shortcut");
            // Walk around the parked load, then through its original aperture.
            yield return DistrictWalk(Zone.GetCell(original.x+1,original.y-1),40);
            yield return DistrictWalk(Zone.GetCell(original.x,original.y),12);
            yield return DistrictWalk(Zone.GetCell(original.x,original.y+1),12);
            Check("variant_cleared_crossing",Zone.GetEntityPosition(frame)==(original.x,original.y-2)
                &&!Zone.GetCell(original.x,original.y).BlocksMovement(Player)&&At.X==original.x&&At.Y==original.y+1);
        }

        Entity VariantTonicTarget(int x,int y,Entity tonic)
        {
            if(tonic==null||!Owns(Player,tonic))return null;
            int range=HandlingService.GetThrowRange(Player,tonic);var hostiles=Threats(Zone);
            foreach(var target in hostiles.Where(e=>!ReferenceGladeRouteControl.HasLiveCalm(Zone,e)).OrderBy(e=>SpatialQuery.DistanceToCell(Zone,e,x,y)))
            {
                var cell=Zone.GetEntityCell(target);int distance=AIHelpers.ChebyshevDistance(x,y,cell.X,cell.Y);
                if(distance<3||distance>range)continue;
                var trace=LineTargeting.TraceFirstImpactToTarget(Zone,Player,x,y,cell.X,cell.Y,range);
                if(trace.HitEntity!=target||trace.BlockedBySolid)continue;
                // Tonic AOE is radius1. The player is outside it and every
                // current creature in that area must already be hostile.
                if(Zone.GetReadOnlyEntities().Any(e=>e.HasTag("Creature")&&e.GetStatValue("Hitpoints")>0
                    &&SpatialQuery.DistanceToCell(Zone,e,cell.X,cell.Y)<=1&&!hostiles.Contains(e)))continue;
                return target;
            }
            return null;
        }
        IEnumerator ConnectedVariantWet()
        {
            var pools=Zone.GetReadOnlyEntities().Where(e=>e.GetProperty(ConnectedSpreadSatellite.RoleKey)=="wet-lane").ToArray();
            var dividers=Zone.GetReadOnlyEntities().Where(e=>e.GetProperty(ConnectedSpreadSatellite.RoleKey)=="divider").ToArray();
            Require(pools.Length==4&&dividers.Length==6&&pools.All(e=>e.GetPart<LiquidPoolPart>()?.LiquidId=="water"),"real wet lane and opaque divider owners");
            var positions=dividers.Select(Zone.GetEntityPosition).ToArray();int cx=positions[0].x,cy=positions.Min(p=>p.y)+3;
            var tonic=Player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="LightningTonic");string tonicId=tonic.ID;
            int initial=Packed("LightningTonic");bool thrown=false;int initialCalm=_districtCalmCasts;
            for(int step=0;step<48;step++)
            {
                var target=VariantTonicTarget(At.X,At.Y,tonic);
                if(target!=null)
                {
                    var targetCell=Zone.GetEntityCell(target);var cursorCell=(targetCell.X,targetCell.Y);int hp=target.GetStatValue("Hitpoints");
                    var state=Zone.TileState.Get(targetCell.X,targetCell.Y);bool wet=target.HasEffect<WetEffect>()||state?.Coatings.Any(c=>c.Id=="water")==true;
                    string marker=Mark("variant-native-throw");
                    yield return ItemAction(tonic,"throw");Require(State=="ThrowTargeting","native selected finite tonic throw cursor");
                    var pending=Field(_input,"_pendingThrowTarget");Require(ReferenceEquals(Field(pending,"Item"),tonic),"same actual original tonic");
                    var cursor=(WorldCursorState)Field(_input,"_worldCursorState");
                    for(int n=0;cursor.X!=targetCell.X||cursor.Y!=targetCell.Y;n++)
                    {Require(n<20,"finite native tonic aiming");yield return Tap(Direction(targetCell.X-cursor.X,targetCell.Y-cursor.Y));}
                    Require(VariantTonicTarget(At.X,At.Y,tonic)==target&&Zone.GetEntityCell(target)==targetCell,"same lawful current impact before confirmation");
                    yield return Paid(Tap(Key.Enter),"local","variant-original-lightning-tonic");
                    thrown=Packed("LightningTonic")==initial-1&&!Owns(Player,tonic)
                        &&Window(marker).Any(e=>e.Category=="event"&&e.Kind=="TonicApplied"&&e.ActorId==Player.ID&&e.TargetId==target.ID);
                    Require(thrown,"real finite tonic debited and actually applied to hostile");
                    _observations.Add(new{phase="variant-selective-throwable",item=tonicId,target=target.ID,impact=cursorCell,wet,
                        hpBefore=hp,hpAfter=target.GetStatValue("Hitpoints"),consumed=true,
                        bound="One actual original tonic. Wet state is observed, never invented; this does not claim a wet damage multiplier unless independently recorded."});
                    break;
                }
                var path=PathTo(c=>VariantTonicTarget(c.X,c.Y,tonic)!=null);
                if(path==null||path.Count==0)break;
                yield return DistrictDefensiveCalm();
                // Re-evaluate after a real spell; no saved speculative step is reused.
                path=PathTo(c=>VariantTonicTarget(c.X,c.Y,tonic)!=null);if(path==null||path.Count==0)break;
                yield return StepTo(path[0].x,path[0].y);
            }
            // A preserved tonic is a valid disclosed choice; the long dry
            // approach still has to be reached with ordinary current owners.
            yield return DistrictWalk(Zone.GetCell(cx-4,cy),160);
            Check("variant_source_choice",(thrown?Packed("LightningTonic")==initial-1:Packed("LightningTonic")==initial)
                &&At.X==cx-4&&At.Y==cy&&!At.Occupants.Any(e=>e.HasPart<LiquidPoolPart>()));
            _observations.Add(new{phase="variant-wet-alternative",thrown,retainedTonic=!thrown,actualCalmCasts=_districtCalmCasts-initialCalm,
                dryOutsideX=At.X,dryOutsideY=At.Y,reason=thrown?"A real lawful useful ray was available.":"No bounded lawful useful throw approach; retained original tonic and walked the outer dry cover route."});
            yield return Capture("variant-wet-real-choice-and-dry-approach");
        }

        IEnumerator ConnectedVariantDefense()
        {
            if(!ConnectedVariant||!VariantBreaker||_fighting)yield break;
            var target=Threats(Zone).Where(e=>SpatialQuery.Distance(Zone,Player,e)<=1).OrderBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault();
            if(target==null)yield break;
            Require(_variantDefenses++<4&&StartingBuildService.PrimaryHandWeapon(Player)?.ID==_variantWeaponId
                &&StartingBuildService.PrimaryHandWeapon(Player)?.BlueprintName=="Cudgel","bounded real Breaker defense with original cudgel");
            var prior=_guard;string priorId=_guardId;bool fighting=_fighting,attempt=_guardAttempt,damage=_guardDamage,lethal=_guardLethal;
            _guard=target;_guardId=target.ID;_guardAttempt=_guardDamage=_guardLethal=false;
            try
            {
                yield return FightGuard(requireDirectLethal:false);
                _observations.Add(new{phase="variant-breaker-defense",target=target.ID,weapon=_variantWeaponId,defenses=_variantDefenses,
                    observedDamage=_guardDamage,directLethalObserved=_guardLethal,remainingTonics=Packed("HealingTonic")});
            }
            finally{_guard=prior;_guardId=priorId;_fighting=fighting;_guardAttempt=attempt;_guardDamage=damage;_guardLethal=lethal;}
        }

        string VariantReputation()=>string.Join("|",PlayerReputation.GetAll().OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+":"+p.Value));
        Entity VariantKeeper=>ConnectedAt(LocalGatheringClaimPart.ReserveZoneID,_variantKeeperId);
        IEnumerator ConnectedVariantConsequence()
        {
            yield return TravelSurface(LocalGatheringClaimPart.ReserveZoneID);
            var keeper=Zone.GetReadOnlyEntities().Single(e=>e.HasPart<LocalGatheringClaimPart>());_variantKeeperId=keeper.ID;
            var claim=keeper.GetPart<LocalGatheringClaimPart>();
            Require(claim.GetState(Player)==ReserveAccessState.Unknown&&Packed("Emberwheat")>=2,"no earlier reserve permission and actual earned restitution grain");
            yield return DistrictApproach(keeper,140);yield return WorldAction(keeper,LocalGatheringClaimPart.TermsCommand);yield return CloseNormal();
            var crop=new[]{claim.FirstSoil,claim.SecondSoil}.SelectMany(e=>Zone.GetEntityCell(e).Objects)
                .Single(e=>e.GetPart<CropPart>()?.GrowthStage==2);_variantHarvestedId=crop.ID;
            yield return DistrictApproach(crop,40);
            var source=Zone.GetEntityCell(crop);var eye=Zone.GetEntityCell(keeper);var brain=keeper.GetPart<BrainPart>();
            bool witness=AIHelpers.ChebyshevDistance(eye.X,eye.Y,At.X,At.Y)<=brain.SightRadius
                &&AIHelpers.ChebyshevDistance(eye.X,eye.Y,source.X,source.Y)<=brain.SightRadius
                &&AIHelpers.HasLineOfSight(Zone,eye.X,eye.Y,At.X,At.Y)&&AIHelpers.HasLineOfSight(Zone,eye.X,eye.Y,source.X,source.Y);
            _observations.Add(new{phase="variant-actual-witness-context",keeper=keeper.ID,crop=crop.ID,witness,warning=LocalGatheringClaims.WarningFor(Player,crop,Zone)});
            Require(witness,"actual keeper must be able to witness source and actor; no forced witness state");
            string reputation=VariantReputation(),marker=Mark("variant-observed-reserve-breach");int purse=TradeSystem.GetDrams(Player);
            yield return Paid(WorldAction(crop,"HarvestCultivatedCrop"),"local","variant-unauthorized-committed-harvest");
            _variantYieldIds=source.Objects.Where(e=>e.GetPart<PhysicsPart>()?.Takeable==true).Select(e=>e.ID).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
            Require(_variantYieldIds.Length>0,"actual committed harvest outputs remain on the claimed ground");
            Check("variant_witnessed_breach",claim.GetState(Player)==ReserveAccessState.Suspended
                &&claim.Permissions.Single(p=>p.Player==Player).BreachOrdinal==1&&CountGraphId(_variantHarvestedId)==0
                &&VariantReputation()==reputation&&TradeSystem.GetDrams(Player)==purse&&!FactionManager.IsHostile(keeper,Player)
                &&Window(marker).Any(e=>e.Kind=="ReserveBreachWitnessed"&&e.ActorId==Player.ID&&e.TargetId==keeper.ID));
            yield return Capture("variant-05-real-local-consequence");
            yield return ConnectedVariantGroundThrow(source);
            yield return ConnectedVariantCheckpoint("variant_breach_save_restore");
            yield return TravelSurface(GleanersDistrict.SurfaceID);yield return Capture("variant-06-left-suspended-reserve");
            yield return TravelSurface(LocalGatheringClaimPart.ReserveZoneID);keeper=VariantKeeper;claim=keeper.GetPart<LocalGatheringClaimPart>();
            yield return DistrictApproach(keeper,140);
            Check("variant_local_return",claim.GetState(Player)==ReserveAccessState.Suspended&&claim.Permissions.Single(p=>p.Player==Player).BreachOrdinal==1
                &&VariantReputation()==reputation&&!FactionManager.IsHostile(keeper,Player)&&CountGraphId(_variantHarvestedId)==0);
            int grain=Packed("Emberwheat"),stock=keeper.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="Emberwheat").Sum(Units);
            yield return Paid(WorldAction(keeper,LocalGatheringClaimPart.ReconcileCommand),"local","variant-earned-grain-restitution");
            Check("variant_restitution",claim.GetState(Player)==ReserveAccessState.Granted&&Packed("Emberwheat")==grain-2
                &&keeper.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="Emberwheat").Sum(Units)==stock+2
                &&claim.Permissions.Single(p=>p.Player==Player).BreachOrdinal==1&&VariantReputation()==reputation&&TradeSystem.GetDrams(Player)==purse);
            yield return Capture("variant-07-earned-restitution");
        }

        // Exercise the actual second throw popup, cancellation and targeting.
        // This route already witnessed the harvest; first-witness throw and
        // partial-stack ownership are covered by paired native EditMode cases.
        IEnumerator ConnectedVariantGroundThrow(Cell source)
        {
            var item=source.Objects.FirstOrDefault(e=>e.GetPart<ReserveYieldPart>()?.AlreadyWitnessedPlayer==Player
                &&Units(e)==1&&HandlingService.CanThrow(Player,e,out _));
            Require(item!=null,"actual throwable reserve harvest unit");
            var approach=Steps.Where(d=>Math.Abs(d.x)+Math.Abs(d.y)==1)
                .Select(d=>Zone.GetCell(source.X+d.x,source.Y+d.y))
                .Where(c=>c!=null&&Safe(Zone,c,ThreatClearance)&&Zone.CanPlaceFootprint(Player,c.X,c.Y))
                .OrderBy(c=>AIHelpers.ChebyshevDistance(At.X,At.Y,c.X,c.Y)).FirstOrDefault();
            Require(approach!=null,"actual safe cardinal throw approach");yield return DistrictWalk(approach,40);
            var target=Steps.Select(d=>Zone.GetCell(At.X+d.x,At.Y+d.y)).FirstOrDefault(c=>c!=null&&c!=source
                &&Safe(Zone,c,ThreatClearance)&&!c.Objects.Any(e=>e.HasPart<CultivatedSoilPart>())
                &&LineTargeting.TraceFirstImpactToTarget(Zone,Player,At.X,At.Y,c.X,c.Y,HandlingService.GetThrowRange(Player,item)).ImpactCell==c
                &&LineTargeting.TraceFirstImpactToTarget(Zone,Player,At.X,At.Y,c.X,c.Y,HandlingService.GetThrowRange(Player,item)).HitEntity==null);
            Require(target!=null,"empty public landing cell with a real clear throw ray");
            var provenance=item.GetPart<ReserveYieldPart>();int tick=Tick,energy=Energy;
            yield return WorldAction(item,"Throw");Require(State=="ThrowPopupOpen","native ground throw confirmation");
            var popup=Field(_input,"_throwPopup");var option=((IList)Field(popup,"Options"))[0];
            Require(((string)Field(option,"Label")).Contains("Nella's tied reserve"),"reserve warning in the actual second throw popup");
            yield return Capture("variant-ground-throw-warning");yield return Tap(Key.Escape);yield return CloseNormal();
            Require(Tick==tick&&Energy==energy&&Zone.GetEntityCell(item)==source&&ReferenceEquals(item.GetPart<ReserveYieldPart>(),provenance)
                &&!provenance.Released,"cancelled ground throw is free and preserves exact claim owner");
            yield return WorldAction(item,"Throw");yield return Tap(Key.Enter);Require(State=="ThrowTargeting","actual ground throw cursor");
            var cursor=(WorldCursorState)Field(_input,"_worldCursorState");
            for(int n=0;cursor.X!=target.X||cursor.Y!=target.Y;n++)
            {Require(n<8,"bounded ground produce aiming");yield return Tap(Direction(target.X-cursor.X,target.Y-cursor.Y));}
            yield return Paid(Tap(Key.Enter),"local","variant-ground-reserve-throw");
            var claim=VariantKeeper.GetPart<LocalGatheringClaimPart>();
            Check("variant_ground_throw",Zone.GetEntityCell(item)==target&&item.GetPart<ReserveYieldPart>()==null
                &&claim.GetState(Player)==ReserveAccessState.Suspended&&claim.Permissions.Single(p=>p.Player==Player).BreachOrdinal==1);
        }

        string ConnectedVariantDigest()
        {
            var cache=ConnectedAt(VariantZone,_variantCacheId);var frame=ConnectedAt(VariantZone,_variantFrameId);
            var claim=VariantKeeper?.GetPart<LocalGatheringClaimPart>();
            var record=claim?.Permissions.SingleOrDefault(p=>p.Player==Player);
            var well=Manager.CachedZones[GleanersDistrict.SurfaceID].GetReadOnlyEntities().Single(e=>e.GetProperty(GleanersDistrict.RoleKey)=="well");
            return string.Join("|",Player.ID,Player.GetProperty(StartingBuildService.PropertyName),VariantSeed,_variantCacheId,
                string.Join(",",cache.GetPart<ContainerPart>().Contents.Select(e=>e.ID+":"+Units(e))),
                frame==null?"no-frame":frame.ID+":"+Manager.CachedZones[VariantZone].GetEntityPosition(frame),
                Player.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault"),CountGraphId(_variantManualId),
                VariantKeeper?.ID,claim?.GetState(Player),record?.BreachOrdinal,record?.LastBreach,claim?.FirstSoilID,claim?.SecondSoilID,
                string.IsNullOrEmpty(_variantHarvestedId)?"no-harvest":CountGraphId(_variantHarvestedId).ToString(),
                string.Join(",",_variantYieldIds.Select(id=>
                {
                    var item=ConnectedAt(LocalGatheringClaimPart.ReserveZoneID,id);var provenance=item?.GetPart<ReserveYieldPart>();
                    return item==null?"missing:"+id:item.ID+":"+Units(item)+":"+Manager.CachedZones[LocalGatheringClaimPart.ReserveZoneID].GetEntityPosition(item)
                        +":"+provenance?.KeeperID+":"+provenance?.Soil?.ID+":"+provenance?.AlreadyWitnessedPlayer?.ID;
                })),well.ID,well.GetPart<RepairablePart>().Repaired,VariantReputation());
        }
        IEnumerator ConnectedVariantCheckpoint(string check)
        {
            yield return ConnectedCloseAndStabilize();
            var original=Player;var oldZone=Zone;var oldManager=Manager;var oldCache=ConnectedAt(VariantZone,_variantCacheId);var oldKeeper=VariantKeeper;
            string signature=ConnectedVariantDigest(),stats=Stats(Player),gear=Gear(Player),zone=Zone.ZoneID;
            int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,x=At.X,y=At.Y,purse=TradeSystem.GetDrams(Player);
            string file=SaveFile(),priorHash=File.Exists(SaveFile())?HashFile(file):null;
            yield return Tap(Key.F5);yield return ConnectedCloseAndStabilize();_checkpointHash=HashFile(file);
            Require(priorHash!=_checkpointHash&&MessageLog.GetLast()=="Game saved.","actual variant quicksave");
            long bytes=new FileInfo(file).Length;int graphs=Manager.CachedZoneCount;
            var next=Steps.Select(d=>Zone.GetCell(x+d.x,y+d.y)).FirstOrDefault(c=>Safe(Zone,c,ThreatClearance)&&Zone.CanPlaceFootprint(Player,c.X,c.Y));
            Require(next!=null,"safe actual unsaved variant step");yield return StepTo(next.X,next.Y);
            Require(Tick>tick&&HashFile(file)==_checkpointHash,"real unsaved movement after variant checkpoint");yield return Reload(original);
            Check(check,!ReferenceEquals(Player,original)&&!ReferenceEquals(Zone,oldZone)&&!ReferenceEquals(Manager,oldManager)
                &&!ReferenceEquals(ConnectedAt(VariantZone,_variantCacheId),oldCache)&&(oldKeeper==null||!ReferenceEquals(VariantKeeper,oldKeeper))
                &&Zone.ZoneID==zone&&At.X==x&&At.Y==y&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world
                &&Stats(Player)==stats&&Gear(Player)==gear&&TradeSystem.GetDrams(Player)==purse&&ConnectedVariantDigest()==signature
                &&HashFile(file)==_checkpointHash);
            _observations.Add(new{phase="variant-native-save-size-and-retention",check,saveBytes=bytes,cachedGraphs=graphs,
                restoredGraphs=Manager.CachedZoneCount,bound="Actual isolated save bytes and retained graph counts; not a serialized-world-size baseline comparison."});
            yield return Capture(check);
        }
    }
}
