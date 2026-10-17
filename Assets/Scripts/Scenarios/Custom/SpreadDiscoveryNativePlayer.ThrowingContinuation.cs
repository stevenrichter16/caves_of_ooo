using System;
using System.Collections;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        // Continue the same living campaign and retained losses. No repetition
        // of source awards, no buying a new campaign's supplies for this one.
        bool NaturalContinuation => (_connectedBuild=="bombardier"&&_throwingNaturalSeed==29)
            || (_connectedBuild=="breaker"&&_throwingNaturalSeed==1729);
        string[] NaturalPurchasePlan => _connectedBuild=="bombardier"
            ? new[]{"Throwing_MeasuredThrow","Throwing_RecoveringStep","Throwing_DrivingThrow","Throwing_FocusedSplash","HydromancySkill"}
            : new[]{"HydromancySkill","Hydromancy_JetBlast","Throwing_MeasuredThrow","Throwing_RecoveringStep","Throwing_FocusedSplash"};
        int _throwingNaturalExpeditionEnd;
        int NaturalPurchased => 1+NaturalPurchasePlan.Count(s=>Player.GetPart<SkillsPart>().HasSkill(s));
        int NaturalEarned => Player.GetStatValue("Level")-1;
        string NaturalCampaign => _connectedBuild=="bombardier"?"throwing-specialist":"throwing-water-hybrid";

        IEnumerator ThrowingNaturalContinueCampaign()
        {
            if(!NaturalContinuation)yield break;
            // The first-point checkpoint is copied before any later action, then
            // later checkpoints preserve the actual same player/world graph.
            PreserveNaturalCheckpoint("first-point");
            yield return NaturalRecoverOriginal();
            yield return ThrowingNaturalVisitVendor();
            string[] circuit={GleanersDistrict.SurfaceID,"Overworld.11.11.0","Overworld.13.12.0",
                "Overworld.15.10.0","Overworld.12.11.0","Overworld.13.11.0","Overworld.14.11.0","Overworld.14.12.0"};
            for(int trip=0;trip<circuit.Length && NaturalAlive && NaturalActions<_throwingNaturalActionBudget-8;trip++)
            {
                int start=NaturalActions,earned=NaturalEarned;string before=Gear(Player);
                _throwingNaturalExpeditionEnd=Math.Min(start+250,_throwingNaturalActionBudget);
                yield return NaturalBuyEarnedNodes();
                string destination=circuit[trip];
                yield return TravelSurface(destination);
                var observed=Threats(Zone).Where(e=>Zone.GetEntityCell(e)?.IsVisible==true
                    && !ReferenceGladeRouteControl.HasLiveCalm(Zone,e)).OrderBy(e=>SpatialQuery.Distance(Zone,Player,e)).ToArray();
                int encountered=0;
                foreach(var enemy in observed)
                {
                    if(!NaturalAlive||NaturalActions-start>=180||NaturalActions>=_throwingNaturalActionBudget-30)break;
                    if(CombatSystem.IsDeathHandled(enemy)||Zone.GetEntityCell(enemy)==null)continue;
                    // HP and finite healing decide whether this expedition ends.
                    if(Player.GetStatValue("Hitpoints")<18&&Packed("HealingTonic")==0)break;
                    encountered++;yield return NaturalResolveHostile(enemy,Math.Min(start+200,_throwingNaturalActionBudget-20));
                    yield return NaturalRecoverOriginal();yield return NaturalBuyEarnedNodes();
                }
                bool returned=false;
                if(NaturalAlive&&NaturalActions-start<220&&NaturalActions<_throwingNaturalActionBudget-8)
                {yield return TravelSurface(GleanersDistrict.SurfaceID);returned=Zone.ZoneID==GleanersDistrict.SurfaceID;}
                _observations.Add(new{phase="throwing-natural-continuation-expedition",campaign=NaturalCampaign,trip,destination,
                    paidActions=NaturalActions-start,limit=250,encountered,returned,earnedBefore=earned,earnedAfter=NaturalEarned,
                    purchased=NaturalPurchased,banked=Player.GetStatValue("SP"),hp=Player.GetStatValue("Hitpoints"),
                    beforeGear=before,afterGear=Gear(Player),outcome=!NaturalAlive?"death":returned?"returned":"partial-retreat",
                    originalWeapon=NaturalWeapon?.ID,finiteTonics=Packed("HealingTonic")});
                Require(NaturalActions-start<=250,"finite natural outbound/return expedition bound");
                _throwingNaturalExpeditionEnd=0;
                if(!NaturalAlive)break;
                if(returned)yield return ThrowingNaturalVisitVendor(true);
                yield return ThrowingNaturalCheckpoint(false);
                PreserveNaturalCheckpoint("expedition-"+trip+"-earned-"+NaturalEarned);
                if(NaturalEarned>=6&&NaturalPurchased>=6)break;
                if(Player.GetStatValue("Hitpoints")<18&&Packed("HealingTonic")==0)
                {_notes.Add("Natural continuation retreated with low health and no finite healing; losses retained, no refill.");break;}
            }
            _observations.Add(new{phase="throwing-natural-mature-pacing",campaign=NaturalCampaign,
                actions=NaturalActions,budget=_throwingNaturalActionBudget,earned=NaturalEarned,purchased=NaturalPurchased,
                reachedFour=NaturalEarned>=4,reachedSix=NaturalEarned>=6,
                reason=!NaturalAlive?"death":NaturalActions>=_throwingNaturalActionBudget-8?"paid-action-budget":
                    Player.GetStatValue("Hitpoints")<18&&Packed("HealingTonic")==0?"finite-supply-retreat":"bounded-natural-circuit-ended",
                limit="Only actual earned checkpoints are verified. Unreached 4/6SP remains unverified; these are not synthetic XP trials."});
        }
        void PreserveNaturalCheckpoint(string label)
        {
            string source=SaveFile();Require(File.Exists(source),"actual saved campaign exists before retaining checkpoint");
            string path=Path.Combine(DirectoryPath,"campaign-"+label+".sav.gz");File.Copy(source,path,false);
            _observations.Add(new{phase="throwing-natural-retained-checkpoint",campaign=NaturalCampaign,label,path,
                hash=HashFile(path),player=Player.ID,level=Player.GetStatValue("Level"),banked=Player.GetStatValue("SP"),gear=Gear(Player)});
        }
        IEnumerator NaturalBuyEarnedNodes()
        {
            foreach(string skill in NaturalPurchasePlan)
            {
                if(Player.GetPart<SkillsPart>().HasSkill(skill))continue;
                if(Player.GetStatValue("SP")<=0)yield break;
                int before=Player.GetStatValue("SP");
                yield return OpeningInspectSkill(skill,false,true);
                _observations.Add(new{phase="throwing-natural-earned-child",campaign=NaturalCampaign,skill,
                    before,after=Player.GetStatValue("SP"),level=Player.GetStatValue("Level"),actions=NaturalActions});
            }
        }
        IEnumerator NaturalRecoverOriginal()
        {
            var item=NaturalWeapon;
            if(item==null)
            {
                item=Zone.GetReadOnlyEntities().FirstOrDefault(e=>e.ID==_connectedOriginalDaggerId);
                if(item==null)yield break;
                // Every paid step lets enemies and terrain change. Never replay
                // an entire route captured before those actual world responses.
                var sourceZone=Zone;
                for(int steps=0;steps<12&&NaturalAlive&&NaturalActions<_throwingNaturalActionBudget
                    &&(_throwingNaturalExpeditionEnd==0||NaturalActions<_throwingNaturalExpeditionEnd);steps++)
                {
                    yield return ConnectedCloseAndStabilize();
                    var cell=Zone==sourceZone?Zone.GetEntityCell(item):null;
                    if(cell==At)break;
                    if(cell==null||!cell.IsVisible||!cell.Explored||!Safe(Zone,cell,ThreatClearance))
                    {
                        _observations.Add(new{phase="throwing-natural-recovery-abandoned",reason="current-owner-or-destination-unsafe",steps,actions=NaturalActions});
                        yield break;
                    }
                    var path=PathTo(c=>c==cell);
                    if(path==null||path.Count==0||path.Count>12-steps)
                    {
                        _observations.Add(new{phase="throwing-natural-recovery-abandoned",reason="no-current-route-within-original-step-bound",steps,actions=NaturalActions});
                        yield break;
                    }
                    yield return StepTo(path[0].x,path[0].y);
                }
                if(Zone!=sourceZone||Zone.GetEntityCell(item)!=At||NaturalActions>=_throwingNaturalActionBudget
                    ||(_throwingNaturalExpeditionEnd>0&&NaturalActions>=_throwingNaturalExpeditionEnd))yield break;
                yield return Paid(ConnectedGroundPickup(),"local","throwing-natural-original-weapon-recovered");
            }
            if(Owns(Player,item)&&item.GetPart<PhysicsPart>()?.Equipped!=Player)
            {
                yield return ItemAction(item,"equip_auto");yield return ConnectedCloseAndStabilize();
                Require(item.GetPart<PhysicsPart>()?.Equipped==Player,"explicit ordinary equip after carry-only pickup");
            }
        }
        IEnumerator NaturalResolveHostile(Entity enemy,int actionEnd)
        {
            var previous=_guard;string oldId=_guardId;bool oldFighting=_fighting;
            bool oldAttempt=_guardAttempt,oldDamage=_guardDamage,oldLethal=_guardLethal;
            _guard=enemy;_guardId=enemy.ID;_fighting=true;
            int start=NaturalActions;string source=Gear(Player);
            try
            {
                for(int n=0;n<45&&NaturalAlive&&!CombatSystem.IsDeathHandled(enemy)&&NaturalActions<actionEnd;n++)
                {
                    var target=Zone.GetEntityCell(enemy);if(target==null)break;
                    if(Player.GetStatValue("Hitpoints")<=24)
                    {
                        var tonic=Player.GetPart<InventoryPart>().Objects.FirstOrDefault(e=>e.BlueprintName=="HealingTonic");
                        if(tonic!=null){yield return Paid(ItemAction(tonic,"ApplyTonic"),"local","throwing-natural-finite-defensive-tonic");continue;}
                        if(Player.GetStatValue("Hitpoints")<18)break;
                    }
                    int distance=SpatialQuery.Distance(Zone,Player,enemy);
                    if(distance<=1)
                    {yield return Paid(Tap(Direction(target.X-At.X,target.Y-At.Y)),"local","throwing-natural-melee-response");continue;}
                    var weapon=NaturalWeapon;
                    if(weapon!=null&&NaturalThrowTarget(weapon)==enemy)
                    {
                        if(Player.GetPart<SkillsPart>().HasSkill("Throwing_MeasuredThrow")
                            &&ThrowingRules.CooldownRemaining(Player,"Throwing_MeasuredThrow")==0)
                            yield return NaturalSelectedThrow(weapon,enemy,true);
                        // Re-evaluate after the paid preparation's actual enemy
                        // response; a moved target is never silently teleported.
                        if(NaturalAlive&&NaturalThrowTarget(weapon)==enemy)yield return NaturalSelectedThrow(weapon,enemy,false);
                        else yield return NaturalCancelThrowSelection();
                        continue;
                    }
                    var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,enemy,c.X,c.Y)==1);
                    if(path==null||path.Count==0)break;
                    yield return StepTo(path[0].x,path[0].y);
                }
            }
            finally
            {
                _observations.Add(new{phase="throwing-natural-continuation-combat",campaign=NaturalCampaign,enemy=enemy.ID,
                    enemyBlueprint=enemy.BlueprintName,actions=NaturalActions-start,hp=Player.GetStatValue("Hitpoints"),
                    enemyHp=enemy.GetStatValue("Hitpoints"),defeated=CombatSystem.IsDeathHandled(enemy),beforeGear=source,afterGear=Gear(Player)});
                _guard=previous;_guardId=oldId;_fighting=oldFighting;
                _guardAttempt=oldAttempt;_guardDamage=oldDamage;_guardLethal=oldLethal;
            }
        }
        IEnumerator NaturalSelectedThrow(Entity item,Entity enemy,bool prepare)
        {
            var target=Zone.GetEntityCell(enemy);
            if(target==null){yield return NaturalCancelThrowSelection();yield break;}
            if(!prepare&&State=="ThrowTargeting")
            {
                var pending=Field(_input,"_pendingThrowTarget");
                var intent=pending==null?null:(ThrowIntent)Field(pending,"Intent");
                if(pending!=null&&ReferenceEquals(Field(pending,"Item"),item)
                    &&ReferenceEquals(Field(pending,"PreparedTarget"),enemy)
                    &&(ThrowMode)Field(pending,"Mode")==ThrowMode.Ordinary&&intent?.Target==enemy
                    &&Player.GetPart<ThrowingStatePart>()?.Prepared==true)
                {
                    // Production tracks the same visible owner after its real
                    // turn. Confirm its retained aim; do not move the locked cursor.
                    yield return Paid(Tap(Key.Enter),"local","throwing-natural-retained-measured-throw");
                    yield break;
                }
                yield return NaturalCancelThrowSelection();
            }
            int x=target.X,y=target.Y;
            if(prepare)
            {
                yield return Tap(Key.M);Require(State=="AbilityManagerOpen","real preparation ability manager");
                var id=Player.GetPart<SkillsPart>().GetSkill("Throwing_MeasuredThrow").ActivatedAbilityID;
                int row=AbilityManagerStateBuilder.Build(Player).Rows.ToList().FindIndex(r=>r.AbilityID==id);
                Require(row>=0,"actual earned preparation row");
                for(int n=0;_input.AbilityManagerUI.CursorIndex!=row;n++)
                {Require(n<100,"finite ability manager navigation");yield return Tap(_input.AbilityManagerUI.CursorIndex<row?Key.DownArrow:Key.UpArrow);}
                yield return Tap(Key.Enter);Require(State=="ThrowPopupOpen","native earned preparation source picker");
                var picker=Field(_input,"_throwPopup");var options=(System.Collections.IList)Field(picker,"Options");int selected=-1;
                for(int i=0;i<options.Count;i++)if(ReferenceEquals(Field(options[i],"Item"),item))selected=i;
                Require(selected>=0,"same original physical source offered");
                for(int n=0;(int)Field(picker,"CursorIndex")!=selected;n++)
                {Require(n<100,"finite preparation source navigation");yield return Tap((int)Field(picker,"CursorIndex")<selected?Key.DownArrow:Key.UpArrow);}
                yield return Tap(Key.Enter);
            }
            else yield return ItemAction(item,"throw");
            Require(State=="ThrowTargeting","actual preparation or throw target cursor");
            var cursor=(WorldCursorState)Field(_input,"_worldCursorState");
            for(int n=0;cursor.X!=x||cursor.Y!=y;n++)
            {Require(n<110,"finite target cursor");yield return Tap(cursor.X<x?Key.RightArrow:cursor.X>x?Key.LeftArrow:cursor.Y<y?Key.DownArrow:Key.UpArrow);}
            yield return Paid(Tap(Key.Enter),"local",prepare?"throwing-natural-earned-measured-preparation":"throwing-natural-continuation-throw",keepThrowTarget:prepare);
        }
        IEnumerator NaturalCancelThrowSelection()
        {
            if(State!="ThrowTargeting")yield break;
            int tick=Tick,energy=Energy;
            yield return Tap(Key.Escape);yield return ConnectedCloseAndStabilize();
            Require(State=="Normal"&&Tick==tick&&Energy==energy,"abandoning a stale natural throw selection stays free");
        }
    }
}
