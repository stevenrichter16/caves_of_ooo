using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>A finite ordinary-start journey. Every change is made by the
    /// shared native-key helpers; graph inspection only verifies their results.</summary>
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _district, _districtCombat, _districtPrepared, _districtPreparing;
        string _districtDaggerId, _districtCoatingId, _districtFrostPatchId;
        int _districtDaggerMaxBefore;
        bool DistrictConfronts=>_districtCombat;
        Guid _districtOriginalCalm;
        int _districtCalmCasts;
        static readonly string[] DistrictBaseChecks =
        {
            "ordinary_start", "district_actual_surface", "district_broken_service",
            "district_native_repair_refused", "district_native_cellar_entry",
            "district_actual_finite_cache", "district_native_selected_approach",
            "district_native_supplies_taken", "district_native_reward_used", "district_native_return",
            "district_native_repair", "district_native_draw", "district_checkpoint_saved",
            "district_unsaved_step", "district_restored_consequences", "district_finish"
        };
        static readonly string[] DistrictPreparedChecks=DistrictBaseChecks.Concat(new[]{
            "district_native_cold_harvest", "district_native_freezing_brew",
            "district_native_original_dagger_tempered", "district_native_preparation_return"}).ToArray();
        string[] DistrictChecks=>_districtPrepared?DistrictPreparedChecks:DistrictBaseChecks;
        const string DistrictIntent = "Ordinary seed64 or1729 N, continuous native-key glade/cellar round trip by the selected covered, confrontation or prepared-utility approach, real finite clay and reward retrieval, actual charged-book learning or buckler equipment, return and material repair, working-well drinking, then F5/unsaved-step/F6 preserving repair, empty cache, used reward and original player identity in replacement saved graphs. Prepared mode first walks the actual northern alembic and eastern forge loop, spends harvested frost lichen on a freezing coating and tempers the original dagger. Filling is verified only with a real carried vessel.";
        const string DistrictLimits = "One script-selected seed/profile per run using the isolated launcher legacy starter kit; its isolated save root bypasses the new build picker. This verifies neither a player-selected build nor all five builds, including their initially equipped weapons. Not unaided discovery, encounter balance, earned skill purchase, all-seed coverage or build performance. No transfers, grants, source edits, AI suppression or cooldown waits. Covered and prepared-utility modes use the permanent beam detour and at most four original ready defensive Calm casts, never claiming hauling. Prepared mode may genuinely fail to reach either hostile worksite; a stored freezing specification does not itself prove a live freeze proc. Confrontation uses the original dagger and may consume only original finite healing tonics through the existing native FightGuard helper. Well drinking does not prove a Parched cure when none was present; existing drinking/filling may be free. Named checkpoint clauses and graph differences retain actual failures. Screenshots need independent visual inspection.";

        IEnumerator DistrictJourney()
        {
            Require((Manager.WorldSeed==64||Manager.WorldSeed==1729) && Zone.ZoneID==GleanersDistrict.SurfaceID, "ordinary declared district seed and glade");
            int seed=Manager.WorldSeed;bool north=(unchecked((uint)seed)&1u)==0;
            int storeX=62+(int)(unchecked((uint)seed)%3u)*2;
            int SideY(int y)=>north?y:24-y;
            _observations.Add(new{phase="district-profile",seed,approach=_districtPrepared?"prepared-utility":_districtCombat?"confrontation":"covered",side=north?"north":"south"});
            var originalPlayer=Player;string playerId=Player.ID;
            _districtOriginalCalm=Player.GetPart<ActivatedAbilitiesPart>()?.AbilityList
                .SingleOrDefault(a=>a.Command=="CommandCalm")?.ID??Guid.Empty;
            Require(_districtOriginalCalm!=Guid.Empty,"original starting Calm, without grant");
            var surface=Zone;
            var well=DistrictOwner(surface,GleanersDistrict.RoleKey,"well");
            var entrance=DistrictOwner(surface,GleanersDistrict.RoleKey,"stairs");
            string wellId=well.ID;
            Check("district_actual_surface",surface.GetEntityPosition(well)==(41,8)
                &&surface.GetEntityPosition(entrance)==(28,19)
                &&well.GetPart<RepairablePart>()?.RecipeId=="clay-well-lining"
                &&well.GetPart<WellPart>()!=null &&entrance.HasPart<StairsDownPart>());
            Require(DistrictClay()==0,"ordinary starter has no granted repair clay");
            yield return DistrictApproach(well,50);
            int tick=Tick,energy=Energy;string gear=Gear(Player);
            yield return WorldAction(well,"Examine");
            var brokenActions=((List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions")).Select(a=>a.Command).ToArray();
            yield return ReadPages("district-02-broken-well");
            Check("district_broken_service",!well.GetPart<WellPart>().IsUsable
                &&!well.GetPart<RepairablePart>().Repaired
                &&brokenActions.Contains(RepairablePart.RepairCommand)
                &&!brokenActions.Contains("DrawWaterAtWell")
                &&_readerText.IndexOf("fire clay",StringComparison.OrdinalIgnoreCase)>=0
                &&Tick==tick&&Energy==energy&&Gear(Player)==gear);
            yield return CloseNormal();
            // The broken service correctly has no Draw menu entry. Exercise the
            // offered repair refusal instead of injecting an unavailable command.
            string marker=Mark("district-no-clay-repair-refusal");
            yield return WorldAction(well,RepairablePart.RepairCommand);yield return CloseNormal();
            Check("district_native_repair_refused",Tick==tick&&Energy==energy&&Gear(Player)==gear
                &&!well.GetPart<RepairablePart>().Repaired
                &&Window(marker).Any(e=>e.Category=="furniture"&&e.Kind=="RepairRejected"&&e.TargetId==wellId));
            yield return Capture("district-03-native-repair-refusal");

            if(_districtPrepared)yield return DistrictPreparation();
            Require(ReferenceEquals(Zone,surface)&&ReferenceEquals(Player,originalPlayer),"ordinary preparation returns to the original glade graph");
            yield return DistrictWalk(surface.GetCell(28,19),90);
            yield return Capture("district-04-cellar-entrance");
            yield return Paid(Tap(Key.LeftShift,Key.Period),"local","district-native-stairs-down");
            Check("district_native_cellar_entry",ReferenceEquals(Player,originalPlayer)&&Player.ID==playerId
                &&Zone.ZoneID==GleanersCellarBuilder.ZoneID&&At.X==40&&At.Y==12);
            var cellar=Zone;
            var stairs=DistrictOwner(cellar,GleanersCellarBuilder.RoleKey,"stairs");
            var notice=DistrictOwner(cellar,GleanersCellarBuilder.RoleKey,"notice");
            var beam=DistrictOwner(cellar,GleanersCellarBuilder.RoleKey,"beam");
            var guard=DistrictOwner(cellar,GleanersCellarBuilder.RoleKey,"guard");
            var crate=DistrictOwner(cellar,GleanersCellarBuilder.RoleKey,"supplies");
            var contents=crate.GetPart<ContainerPart>().Contents.ToArray();
            string rewardBlueprint=north?"ShatteredRimeGrimoire":"Buckler";
            var reward=contents.Single(e=>e.BlueprintName==rewardBlueprint);
            string crateId=crate.ID,rewardId=reward.ID,beamId=beam.ID;
            var beamPosition=cellar.GetEntityPosition(beam);
            Check("district_actual_finite_cache",contents.Where(e=>e.BlueprintName=="FireClay").Sum(Units)==2
                &&contents.All(e=>e.BlueprintName=="FireClay"||e==reward)
                &&contents.All(e=>e.GetPart<PhysicsPart>()?.InInventory==crate)
                &&(north?reward.GetPart<GrimoireChargePart>()?.Charges==10&&reward.GetPart<GrimoireChargePart>().MaxCharges==10
                    :reward.GetPart<ArmorPart>()?.AV==1&&reward.HasPart<EquippablePart>())
                &&cellar.GetEntityPosition(crate)==(storeX,SideY(7)));
            _observations.Add(new{phase="district-original-cache",zone=cellar.ZoneID,crate=crateId,
                goods=contents.Select(e=>new{id=e.ID,blueprint=e.BlueprintName,units=Units(e),owner=e.GetPart<PhysicsPart>()?.InInventory?.ID}).ToArray(),
                guard=guard.ID,guardHP=guard.GetStatValue("Hitpoints"),guardAt=cellar.GetEntityPosition(guard),beam=beamId,beamPosition});
            yield return Capture("district-05-cellar-landing");
            yield return WorldAction(notice,"Examine");yield return ReadPages("district-06-cellar-directions");yield return CloseNormal();
            if(DistrictConfronts)
            {
                var dagger=Player.GetPart<InventoryPart>().Objects.Concat(Player.GetPart<InventoryPart>().EquippedItems.Values)
                    .Distinct().Single(e=>e.BlueprintName=="Dagger");
                yield return ItemAction(dagger,"equip_auto");yield return CloseNormal();
                Require(dagger.GetPart<PhysicsPart>().Equipped==Player,"actual original dagger equipped for confrontation");
                _guard=guard;_guardId=guard.ID;_guardAttempt=false;_guardDamage=false;_guardLethal=false;
                yield return FightGuard();
                yield return DistrictApproach(crate,75);
                Check("district_native_selected_approach",CombatSystem.IsDeathHandled(guard)&&_guardAttempt&&_guardDamage&&_guardLethal
                    &&SpatialQuery.Distance(cellar,Player,crate)<=1);
                yield return Capture("district-07-native-confrontation");
            }
            else
            {
                // Mirror the authored side passage, while shared pathfinding and
                // native input still validate the actual current body and threats.
                foreach(var at in new[]{(40,5),(45,5),(46,4),(46,2),(50,2),(50,5),(storeX-2,5),(storeX-1,6)})
                {
                    yield return DistrictWalk(cellar.GetCell(at.Item1,SideY(at.Item2)),75);
                    if(at==(46,2))yield return Capture("district-07-permanent-beam-detour");
                }
                Check("district_native_selected_approach",Zone==cellar&&SpatialQuery.Distance(cellar,Player,crate)<=1
                    &&cellar.GetEntityPosition(beam)==beamPosition&&beam.ID==beamId
                    &&guard.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(guard));
            }
            yield return WorldAction(crate,"Examine");yield return ReadPages("district-08-supply-crate");yield return CloseNormal();
            int clayBefore=DistrictClay();
            yield return Paid(DistrictTakeCache(crate,contents),"local","district-native-cache-pickup");
            Check("district_native_supplies_taken",crate.GetPart<ContainerPart>().Contents.Count==0
                &&DistrictClay()==clayBefore+2&&Owns(Player,reward)&&CountGraphId(rewardId)==1
                &&(!north||reward.GetPart<GrimoireChargePart>().Charges==10));
            yield return Capture("district-10-real-clay-and-reward-acquired");

            yield return DistrictUseReward(reward,north);
            if(DistrictConfronts)yield return DistrictWalk(cellar.GetCell(40,12),90);
            else foreach(var at in new[]{(storeX-2,5),(50,5),(50,2),(46,2),(46,4),(45,5),(40,5),(40,12)})
                yield return DistrictWalk(cellar.GetCell(at.Item1,SideY(at.Item2)),75);
            Require(ReferenceEquals(Zone.GetEntityCell(stairs),At),"actual return stair occupied before native ascent");
            yield return Paid(Tap(Key.LeftShift,Key.Comma),"local","district-native-stairs-up");
            Check("district_native_return",ReferenceEquals(Player,originalPlayer)&&Player.ID==playerId
                &&ReferenceEquals(Zone,surface)&&At.X==28&&At.Y==19
                &&crate.GetPart<ContainerPart>().Contents.Count==0&&Owns(Player,reward));
            yield return DistrictApproach(well,90);
            int clay=DistrictClay();
            yield return Paid(WorldAction(well,RepairablePart.RepairCommand),"local","district-native-well-repair");
            Check("district_native_repair",well.ID==wellId&&well.GetPart<RepairablePart>().Repaired
                &&well.GetPart<WellPart>().IsUsable&&DistrictClay()==clay-2&&Owns(Player,reward));
            yield return Capture("district-11-well-repaired");
            yield return WorldAction(well,"Examine");yield return ReadPages("district-12-restored-well-reader");yield return CloseNormal();
            bool parchedBefore=Player.HasEffect<ParchedEffect>();tick=Tick;energy=Energy;
            marker=Mark("district-native-working-well-draw");
            yield return WorldAction(well,"DrawWaterAtWell");yield return CloseNormal();
            Check("district_native_draw",Window(marker).Any(e=>e.Category=="furniture"&&e.Kind=="WaterDrawn"
                &&e.ActorId==Player.ID&&e.TargetId==wellId)&&!Player.HasEffect<ParchedEffect>()&&Tick==tick&&Energy==energy);
            _observations.Add(new{phase="district-native-well-service",well=wellId,parchedBefore,
                parchedAfter=Player.HasEffect<ParchedEffect>(),freeNativeDrink=true,calmCasts=_districtCalmCasts});
            var vessel=Player.GetPart<InventoryPart>().Objects.FirstOrDefault(e=>e.GetPart<WaterskinPart>() is WaterskinPart skin
                &&skin.Charges<skin.Capacity&&Owns(Player,e));
            if(vessel!=null)
            {
                int before=vessel.GetPart<WaterskinPart>().Charges;tick=Tick;energy=Energy;
                yield return ItemAction(vessel,"FillWaterskin");yield return CloseNormal();
                Require(vessel.GetPart<WaterskinPart>().Charges>before&&Tick==tick&&Energy==energy,"actual carried vessel filled by native inventory action");
                _observations.Add(new{phase="district-optional-native-fill",vessel=vessel.ID,before,after=vessel.GetPart<WaterskinPart>().Charges});
            }
            else _notes.Add("DISTRICT FILL NOT EXERCISED: no naturally carried non-full waterskin; no vessel granted.");
            yield return Capture("district-13-working-well-service");
            yield return DistrictCheckpoint(wellId,crateId,rewardId,north);
            Check("district_finish",Player.ID==playerId&&Player.GetStatValue("Hitpoints")>0
                &&!CombatSystem.IsDeathHandled(Player)&&_localInputs<=360&&_mapSteps==(_districtPrepared?3:0)&&_districtCalmCasts<=4);
            yield return Capture("district-15-finished-ordinary-round-trip");
        }

        static Entity DistrictOwner(Zone zone,string key,string role)=>zone.GetReadOnlyEntities().Single(e=>e.GetProperty(key)==role);
        int DistrictClay()=>Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="FireClay"&&Owns(Player,e)).Sum(Units);

        IEnumerator DistrictTakeCache(Entity crate,Entity[] expected)
        {
            yield return WorldAction(crate,"OpenContainer");Require(State=="PickupOpen","native district container popup");
            var rows=(List<Entity>)Field(_input.PickupUI,"_items");
            Require(ReferenceEquals(Field(_input.PickupUI,"_sourceContainer"),crate)
                &&rows.Count==expected.Length&&expected.All(e=>rows.Contains(e)),"exact real district cache rows");
            yield return Capture("district-09-native-cache-contents");yield return Tap(Key.Tab);yield return CloseNormal();
        }

        IEnumerator DistrictWalk(Cell target,int budget)=>DistrictReach(c=>c==target,budget);
        IEnumerator DistrictApproach(Entity target,int budget)=>DistrictReach(c=>SpatialQuery.DistanceToCell(Zone,target,c.X,c.Y)<=1,budget);
        IEnumerator DistrictReach(Func<Cell,bool> goal,int budget)
        {
            for(int i=0;i<budget;i++)
            {
                if(goal(At))yield break;
                yield return DistrictDefensiveCalm();
                var path=PathTo(goal);
                if((path==null||path.Count==0)&&_districtPreparing&&_districtCalmCasts<4
                    &&Player.GetPart<ActivatedAbilitiesPart>()?.AbilityList.Any(a=>a.ID==_districtOriginalCalm&&a.Command=="CommandCalm"&&a.CooldownRemaining==0)==true)
                {
                    var goals=Enumerable.Range(0,Zone.Width).SelectMany(x=>Enumerable.Range(0,Zone.Height).Select(y=>Zone.GetCell(x,y))).Where(goal).ToArray();
                    foreach(var blocker in Threats(Zone).Where(e=>!ReferenceGladeRouteControl.HasLiveCalm(Zone,e)
                        &&goals.Any(c=>SpatialQuery.DistanceToCell(Zone,e,c.X,c.Y)<=ThreatClearance))
                        .OrderBy(e=>SpatialQuery.Distance(Zone,Player,e)).ThenBy(e=>e.ID,StringComparer.Ordinal))
                    {
                        if(ReferenceGladeRouteControl.TryCalm(Zone,Player,blocker,_districtOriginalCalm,out _,out _,out _))
                        {yield return DistrictDefensiveCalm(blocker);path=PathTo(goal);break;}
                        var at=Zone.GetEntityCell(blocker);
                        var vantage=PathTo(c=>
                        {
                            int dx=at.X-c.X,dy=at.Y-c.Y,distance=Math.Max(Math.Abs(dx),Math.Abs(dy));
                            return distance>=2&&distance<=CavesOfOoo.Skills.Spellcraft_Calm.RANGE
                                &&(dx==0||dy==0||Math.Abs(dx)==Math.Abs(dy))&&Safe(Zone,c,ThreatClearance)
                                &&AIHelpers.HasLineOfSight(Zone,c.X,c.Y,at.X,at.Y);
                        });
                        if(vantage==null||vantage.Count==0)continue;
                        path=vantage;var destination=vantage[vantage.Count-1];
                        _observations.Add(new{phase="district-native-obstruction-vantage",blocker=blocker.ID,
                            blockerX=at.X,blockerY=at.Y,vantageX=destination.x,vantageY=destination.y,remainingSteps=vantage.Count});break;
                    }
                }
                if(path==null||path.Count==0)RouteDiagnostic("district native waypoint",goal);
                Require(path!=null&&path.Count>0,"current safe district route remains available");
                yield return StepTo(path[0].x,path[0].y);
            }
            throw new InvalidOperationException("District native route exceeded its bounded movement budget.");
        }
        IEnumerator DistrictDefensiveCalm(Entity selectedBlocker=null)
        {
            if((DistrictConfronts&&!_districtPreparing)||_districtCalmCasts>=4)yield break;
            foreach(var target in Threats(Zone).Where(e=>selectedBlocker!=null?ReferenceEquals(e,selectedBlocker):ReferenceEquals(e.GetPart<BrainPart>()?.Target,Player))
                .OrderBy(e=>SpatialQuery.Distance(Zone,Player,e)).ThenBy(e=>e.ID,StringComparer.Ordinal))
            {
                if(!ReferenceGladeRouteControl.TryCalm(Zone,Player,target,_districtOriginalCalm,out int slot,out int dx,out int dy))continue;
                var ability=Player.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(slot);var origin=At;var zone=Zone;
                int tick=Tick,energy=Energy;string gear=Gear(Player);
                yield return Tap(Shortcut(slot==9?"Digit0":"Digit"+(slot+1)));
                Require(State=="AwaitingDirection"&&Tick==tick&&Energy==energy
                    &&ReferenceGladeRouteControl.TryCalm(Zone,Player,target,_districtOriginalCalm,out int freshSlot,out int freshDx,out int freshDy)
                    &&freshSlot==slot&&freshDx==dx&&freshDy==dy,"native ready original Calm targets exact current hostile owner");
                yield return Paid(Tap(Direction(dx,dy)),"local","district-native-defensive-calm");
                Require(Zone==zone&&At==origin&&Gear(Player)==gear&&ability.CooldownRemaining>0
                    &&ReferenceGladeRouteControl.HasLiveCalm(Zone,target),"actual original Calm paid and pacified the same owner");
                _districtCalmCasts++;
                _observations.Add(new{phase="district-defensive-calm",target=target.ID,slot,dx,dy,originalAbility=_districtOriginalCalm,
                    cooldown=ability.CooldownRemaining,casts=_districtCalmCasts,reason=selectedBlocker==null?"pursuer":"workstation obstruction"});yield break;
            }
        }

        IEnumerator DistrictCheckpoint(string wellId,string crateId,string rewardId,bool isBook)
        {
            Require(Manager.CachedZones.TryGetValue(GleanersCellarBuilder.ZoneID,out var cellar),"visited cellar cached before save");
            var actor=Player;var surface=Zone;var manager=Manager;
            string id=Player.ID,stats=Stats(Player),gear=Gear(Player),surfaceGraph=SourceGraph(surface),cellarGraph=SourceGraph(cellar);
            string preparedWeapon=DistrictDaggerSignature();
            int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,x=At.X,y=At.Y,drams=TradeSystem.GetDrams(Player);
            string file=SaveFile(),old=HashFile(file);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(file);
            Check("district_checkpoint_saved",old!=_checkpointHash&&MessageLog.NextSerialValue>serial
                &&MessageLog.GetLast()=="Game saved."&&SaveGameService.GetSaveInfo("Quick").ActiveZoneID==GleanersDistrict.SurfaceID);
            var step=Steps.Select(d=>Zone.GetCell(x+d.x,y+d.y)).FirstOrDefault(c=>Safe(Zone,c,ThreatClearance)&&Zone.CanPlaceFootprint(Player,c.X,c.Y));
            Require(step!=null,"safe ordinary unsaved district step");yield return StepTo(step.X,step.Y);
            Check("district_unsaved_step",(At.X!=x||At.Y!=y)&&Tick>tick&&HashFile(file)==_checkpointHash);
            yield return Reload(actor);
            Require(Manager.CachedZones.TryGetValue(GleanersCellarBuilder.ZoneID,out var loadedCellar),"native save retained visited cellar");
            var loadedWell=Owner(wellId);var loadedCrate=loadedCellar.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==crateId);
            var loadedReward=Player.GetPart<InventoryPart>().Objects.Concat(Player.GetPart<InventoryPart>().EquippedItems.Values).Distinct().SingleOrDefault(e=>e.ID==rewardId);
            var clauses=new Dictionary<string,bool>
            {
                ["playerGraphReplaced"]=!ReferenceEquals(Player,actor), ["samePlayerId"]=Player.ID==id,
                ["managerReplaced"]=!ReferenceEquals(Manager,manager), ["surfaceGraphReplaced"]=!ReferenceEquals(Zone,surface),
                ["cellarGraphReplaced"]=!ReferenceEquals(loadedCellar,cellar), ["surfaceAddress"]=Zone.ZoneID==GleanersDistrict.SurfaceID,
                ["playerPosition"]=At.X==x&&At.Y==y, ["stats"]=Stats(Player)==stats, ["gear"]=Gear(Player)==gear,
                ["surfaceOwnerState"]=SourceGraph(Zone)==surfaceGraph, ["cellarOwnerState"]=SourceGraph(loadedCellar)==cellarGraph,
                ["turn"]=Tick==tick, ["energy"]=Energy==energy, ["worldClock"]=WorldClock.CurrentTick==world,
                ["drams"]=TradeSystem.GetDrams(Player)==drams, ["saveBytes"]=HashFile(file)==_checkpointHash,
                ["repairFlag"]=loadedWell?.GetPart<RepairablePart>()?.Repaired==true,
                ["wellUsable"]=loadedWell?.GetPart<WellPart>()?.IsUsable==true,
                ["emptyCache"]=loadedCrate?.GetPart<ContainerPart>()?.Contents.Count==0,
                ["rewardOwned"]=Owns(Player,loadedReward), ["singleReward"]=CountGraphId(rewardId)==1,
                ["claySpent"]=DistrictClay()==0,
                ["preparedWeaponSaved"]=!_districtPrepared||DistrictDaggerSignature()==preparedWeapon,
                ["preparedCoatingSpent"]=!_districtPrepared||CountGraphId(_districtCoatingId)==0,
                ["preparedSourceDepleted"]=!_districtPrepared||Manager.CachedZones.TryGetValue("Overworld.11.9.0",out var bank)
                    &&!bank.GetReadOnlyEntities().Any(e=>e.ID==_districtFrostPatchId),
                ["rewardUse"]=isBook?loadedReward?.GetPart<GrimoireChargePart>()?.Charges==10
                    &&Player.GetPart<CavesOfOoo.Skills.SkillsPart>()?.HasSkill("Rites_ShatteredRime")==true
                    :loadedReward?.GetPart<PhysicsPart>()?.Equipped==Player
            };
            _observations.Add(new{phase="district-checkpoint-clauses",clauses,
                surfaceDifferences=DistrictGraphDifferences(surfaceGraph,SourceGraph(Zone)),
                cellarDifferences=DistrictGraphDifferences(cellarGraph,SourceGraph(loadedCellar)),
                expectedWorldClock=world,actualWorldClock=WorldClock.CurrentTick,
                expectedGear=gear,actualGear=Gear(Player),reward=rewardId,isBook,expectedPreparedWeapon=preparedWeapon,actualPreparedWeapon=DistrictDaggerSignature()});
            Check("district_restored_consequences",clauses.Values.All(value=>value));
            yield return Capture("district-14-restored-repair-and-finite-reward");
        }
        IEnumerator DistrictUseReward(Entity reward,bool isBook)
        {
            int tick=Tick,energy=Energy,points=Player.GetStatValue("SP");
            if(isBook)
            {
                var skills=Player.GetPart<CavesOfOoo.Skills.SkillsPart>();
                Require(skills!=null&&!skills.HasSkill("Rites_ShatteredRime"),"newly discovered rite not a starter grant");
                yield return ItemAction(reward,"ReadGrimoire");
                if(State=="AnnouncementOpen")yield return ReadPages("district-10b-native-charged-book-learning");
                yield return CloseNormal();
                Check("district_native_reward_used",skills.HasSkill("Rites_ShatteredRime")&&Owns(Player,reward)
                    &&reward.GetPart<GrimoireChargePart>()?.Charges==10&&Player.GetStatValue("SP")==points&&Tick==tick&&Energy==energy);
            }
            else
            {
                yield return ItemAction(reward,"equip_auto");yield return CloseNormal();
                Check("district_native_reward_used",reward.GetPart<PhysicsPart>()?.Equipped==Player
                    &&Player.GetPart<InventoryPart>().EquippedItems.Values.Contains(reward)
                    &&reward.GetPart<ArmorPart>()?.AV==1&&Player.GetStatValue("SP")==points&&Tick==tick&&Energy==energy);
            }
            yield return Capture("district-10c-native-reward-use");
        }
        IEnumerator DistrictPreparation()
        {
            var actor=Player;var inventory=actor.GetPart<InventoryPart>();
            var dagger=inventory.Objects.Single(e=>e.BlueprintName=="Dagger");
            _districtDaggerId=dagger.ID;_districtDaggerMaxBefore=dagger.GetStat("Hitpoints")?.Max??0;
            Require(_districtDaggerMaxBefore>2&&dagger.GetPart<WeaponTemperPart>()==null,"ordinary untempered starting dagger with real durability");
            _districtPreparing=true;
            try
            {
                yield return TravelSurface("Overworld.11.9.0");
                var bank=Zone;
                var patch=DistrictOwner(bank,SpreadExplorationWorksites.RoleKey,"cold-forage");
                var still=DistrictOwner(bank,SpreadExplorationWorksites.RoleKey,"still");
                Require(patch.BlueprintName=="FrostLichenPatch"&&patch.GetPart<HarvestablePart>()?.Harvested==false
                    &&still.BlueprintName=="AlchemyStill"&&still.HasPart<AlchemyStillPart>(),"real assigned northern frost forage and still");
                _districtFrostPatchId=patch.ID;
                yield return DistrictApproach(patch,100);
                yield return WorldAction(patch,"Examine");yield return ReadPages("district-prepared-01-cold-forage");yield return CloseNormal();
                int before=inventory.Objects.Where(e=>e.BlueprintName=="FrostLichen").Sum(Units);
                Require(before==0,"no starting frost reagent supplied by the audit");
                yield return Paid(WorldAction(patch,"Harvest"),"local","district-prepared-native-frost-harvest");
                int earned=inventory.Objects.Where(e=>e.BlueprintName=="FrostLichen").Sum(Units);
                Check("district_native_cold_harvest",patch.GetPart<HarvestablePart>().Harvested
                    &&bank.GetEntityCell(patch)==null&&earned>=1&&earned<=2&&ReferenceEquals(Player,actor));
                yield return DistrictApproach(still,100);
                foreach(var item in inventory.Objects.Where(e=>e.HasPart<ReagentPart>()&&CraftingMarkPart.IsMarked(e)).ToArray())
                {yield return WorldAction(still,CraftingMarkPart.ToggleCommandPrefix+item.ID);yield return CloseNormal();}
                var reagent=inventory.Objects.First(e=>e.BlueprintName=="FrostLichen");
                yield return WorldAction(still,CraftingMarkPart.ToggleCommandPrefix+reagent.ID);yield return CloseNormal();
                Require(CraftingMarkPart.IsMarked(reagent),"earned frost selected through actual still menu");
                var oldBrews=new HashSet<Entity>(inventory.Objects.Where(e=>e.HasPart<BrewItemPart>()));
                int tick=Tick,energy=Energy;
                yield return WorldAction(still,"BrewMix");yield return CloseNormal();
                var coating=inventory.Objects.Single(e=>e.HasPart<BrewItemPart>()&&!oldBrews.Contains(e));
                _districtCoatingId=coating.ID;
                var effects=coating.GetPart<BrewItemPart>().GetEffects();
                Check("district_native_freezing_brew",inventory.Objects.Where(e=>e.BlueprintName=="FrostLichen").Sum(Units)==earned-1
                    &&coating.GetPart<BrewItemPart>().Form=="Coating"&&effects.Count==1
                    &&effects[0].Property=="Frozen"&&effects[0].Potency==2&&Owns(Player,coating)&&Tick==tick&&Energy==energy);
                yield return Capture("district-prepared-02-real-freezing-coating");
                foreach(var item in inventory.Objects.Where(e=>e.HasPart<ReagentPart>()&&CraftingMarkPart.IsMarked(e)).ToArray())
                {yield return WorldAction(still,CraftingMarkPart.ToggleCommandPrefix+item.ID);yield return CloseNormal();}

                yield return TravelSurface("Overworld.12.10.0");
                var forge=DistrictOwner(Zone,SpreadExplorationWorksites.RoleKey,"forge");
                Require(forge.BlueprintName=="TinkersForge"&&forge.HasPart<ForgePart>()
                    &&Owns(Player,dagger)&&dagger.ID==_districtDaggerId&&Owns(Player,coating),"real eastern forge and same earned ingredients");
                yield return DistrictApproach(forge,120);
                yield return WorldAction(forge,"Examine");yield return ReadPages("district-prepared-03-native-forge");yield return CloseNormal();
                foreach(var item in inventory.Objects.Where(e=>(e.HasPart<MeleeWeaponPart>()||e.HasPart<BrewItemPart>())&&CraftingMarkPart.IsMarked(e)).ToArray())
                {yield return WorldAction(forge,CraftingMarkPart.ToggleCommandPrefix+item.ID);yield return CloseNormal();}
                yield return WorldAction(forge,CraftingMarkPart.ToggleCommandPrefix+dagger.ID);yield return CloseNormal();
                yield return WorldAction(forge,CraftingMarkPart.ToggleCommandPrefix+coating.ID);yield return CloseNormal();
                Require(CraftingMarkPart.IsMarked(dagger)&&CraftingMarkPart.IsMarked(coating),"actual forge selection includes original dagger and earned coating");
                tick=Tick;energy=Energy;
                yield return WorldAction(forge,"CraftKit");yield return CloseNormal();
                var temper=dagger.GetPart<WeaponTemperPart>();
                Check("district_native_original_dagger_tempered",ReferenceEquals(Player,actor)&&Owns(Player,dagger)&&dagger.ID==_districtDaggerId
                    &&!inventory.Objects.Contains(coating)&&CountGraphId(_districtCoatingId)==0
                    &&temper?.TemperCount==1&&temper.HpPenaltyTotal==2&&temper.AppliedSpecsRaw=="Frozen,40,,0,2"
                    &&dagger.GetPart<MeleeWeaponPart>().OnHitEffectsRaw.Split(';').Count(spec=>spec=="Frozen,40,,0,2")==1
                    &&dagger.GetStat("Hitpoints").Max==_districtDaggerMaxBefore-2
                    &&!Player.HasEffect<FrozenEffect>()&&Tick==tick&&Energy==energy);
                _observations.Add(new{phase="district-prepared-native-temper",dagger=_districtDaggerId,coating=_districtCoatingId,
                    oldMaximum=_districtDaggerMaxBefore,newMaximum=dagger.GetStat("Hitpoints").Max,
                    temperCount=temper?.TemperCount,penalty=temper?.HpPenaltyTotal,specification=temper?.AppliedSpecsRaw,
                    bound="Earned material and real weapon specification, not proof of a later random freeze proc."});
                yield return Capture("district-prepared-04-original-dagger-tempered");
                yield return ItemAction(dagger,"equip_auto");yield return CloseNormal();
                Require(dagger.GetPart<PhysicsPart>().Equipped==Player,"real tempered starting dagger equipped before return");
                yield return TravelSurface(GleanersDistrict.SurfaceID);
                Check("district_native_preparation_return",ReferenceEquals(Player,actor)&&Zone.ZoneID==GleanersDistrict.SurfaceID
                    &&_mapSteps==3&&Owns(Player,dagger)&&dagger.ID==_districtDaggerId&&DistrictClay()==0
                    &&bank.GetEntityCell(patch)==null&&CountGraphId(_districtCoatingId)==0);
            }
            finally{_districtPreparing=false;}
        }
        string DistrictDaggerSignature()
        {
            if(!_districtPrepared)return "not-prepared";
            var inventory=Player.GetPart<InventoryPart>();
            var dagger=inventory.Objects.Concat(inventory.EquippedItems.Values).Distinct().SingleOrDefault(e=>e.ID==_districtDaggerId);
            if(dagger==null)return "missing-original-dagger";
            var temper=dagger.GetPart<WeaponTemperPart>();var hp=dagger.GetStat("Hitpoints");
            return dagger.ID+":"+dagger.BlueprintName+":hp="+hp?.BaseValue+"/"+hp?.Max+":tempers="+temper?.TemperCount
                +":penalty="+temper?.HpPenaltyTotal+":applied="+temper?.AppliedSpecsRaw+":onHit="+dagger.GetPart<MeleeWeaponPart>()?.OnHitEffectsRaw;
        }
        static object DistrictGraphDifferences(string before,string after)
        {
            var oldRows=new HashSet<string>(before.Split('|'));var newRows=new HashSet<string>(after.Split('|'));
            return new{beforeOnly=oldRows.Except(newRows).Take(8).ToArray(),afterOnly=newRows.Except(oldRows).Take(8).ToArray(),
                beforeCount=oldRows.Count,afterCount=newRows.Count};
        }
    }
}
