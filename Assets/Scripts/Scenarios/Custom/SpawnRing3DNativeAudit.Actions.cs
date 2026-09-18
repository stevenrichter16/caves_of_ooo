using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    // Optional native acceptance only. No automatic hook and no gameplay override.
    public sealed partial class SpawnRing3DNativeAudit
    {
        readonly List<ActionProbeCheck> actionChecks=new List<ActionProbeCheck>();
        readonly List<ActionHookRow> actionHooks=new List<ActionHookRow>();
        readonly List<ActionViewRow> actionViews=new List<ActionViewRow>();
        bool actionComplete,actionCleanup;

        IEnumerator RunRingActionsIfRequested()
        {
            if(!Environment.GetCommandLineArgs().Contains("-spawnRing3dActions"))yield break;
            Require(OwnedCheckpoint()&&ReadyForPlayerInput(),"Actions require the owned live N session after its F5/F6 and UI gates.");
            yield return WaitForRing();
            var zone=input.CurrentZone;var player=input.PlayerEntity;var turns=input.TurnManager;
            Entity vein=null,snapjaw=null;
            EntityAttackVisualHandler attackObserver=null;EntityDamageVisualHandler damageObserver=null;
            var originalAttack=EntityVisualHooks.AttackCallback;var originalDamage=EntityVisualHooks.DamageCallback;
            int startTick=turns.TickCount,startHp=player.GetStatValue("Hitpoints"),harvestYield=0;
            string[] combatLog=Array.Empty<string>();
            try
            {
                // Explicit fixture creation and placement. Existing terrain, actors,
                // inventory, factions, HP, RNGs and turn energy remain untouched.
                var veinCell=ActionFixtureCell(zone);
                vein=input.ZoneManager.Factory.CreateEntity("GlowQuartzVein");
                Require(vein!=null&&vein.GetPart<HarvestablePart>()!=null,"Actual authored GlowQuartzVein Harvestable exists.");
                var harvest=vein.GetPart<HarvestablePart>();
                Require(harvest.YieldBlueprint=="GlowQuartz"&&harvest.YieldChance==100&&harvest.YieldMin==1&&harvest.YieldMax==2
                    &&ReferenceEquals(HarvestablePart.Factory,input.ZoneManager.Factory),"Unmodified shipped harvest configuration and bootstrap factory.");
                Require(zone.AddEntity(vein,veinCell.x,veinCell.y),"Place only the declared vein on an adjacent empty native floor.");
                ZoneRenderHooks.MarkCellDirty(veinCell.x,veinCell.y,"SpawnRingNative.ActionVein");
                yield return null;yield return null;
                var veinView=ActionView(vein,"harvest-before");actionViews.Add(veinView);
                ActionCheck("modeled_vein_is_current",veinView.shown&&!string.IsNullOrEmpty(veinView.modelId),"The vein's view may be a shared ground-patch root, not an individual GameObject.");
                Require(veinView.shown,"Actual vein representation must exist before testing its removal.");
                int patchBefore=Presenter().GroundPatchRevision(veinCell.x,veinCell.y),unitsBefore=ActionYieldUnits(zone,player,"GlowQuartz");
                int harvestTick=turns.TickCount,harvestEnergy=turns.GetEnergy(player);
                yield return Capture("actions-harvest-before");
                yield return Tap(Key.C);Require(State()=="AwaitingTalkDirection","Actual C interaction direction prompt.");
                yield return Tap(ActionDirection(Position(),veinCell));
                var menu=input.WorldActionMenuUI;
                Require(State()=="WorldActionMenuOpen"&&menu!=null&&menu.IsOpen&&ReferenceEquals(menu.SelectedTarget,vein),"Ordinary direction opens this exact vein's menu.");
                var actions=(List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Private).GetValue(menu);
                var shortcuts=(char[])typeof(WorldActionMenuUI).GetField("_shortcuts",Private).GetValue(menu);
                int harvestIndex=actions.FindIndex(a=>a.Command=="Harvest");
                Require(harvestIndex>=0&&harvestIndex<shortcuts.Length&&shortcuts[harvestIndex]>='a'&&shortcuts[harvestIndex]<='z',"Read the actual rendered Harvest shortcut; do not guess h/index.");
                yield return Capture("actions-harvest-menu");
                yield return Tap((Key)Enum.Parse(typeof(Key),char.ToUpperInvariant(shortcuts[harvestIndex]).ToString()));
                yield return null;yield return null;
                harvestYield=ActionYieldUnits(zone,player,"GlowQuartz")-unitsBefore;
                ActionCheck("native_harvest_consumes_and_yields",zone.GetEntityCell(vein)==null&&harvestYield>=1&&harvestYield<=2,
                    "Actual carried plus ground GlowQuartz increase="+harvestYield+"; inventory overflow is a supported native result.");
                ActionCheck("harvest_view_removed_and_patch_changed",!Presenter().TryGetEntityView(vein,out _,out _)
                    &&!Presenter().IsRenderedEntity(vein)&&Presenter().GroundPatchRevision(veinCell.x,veinCell.y)>patchBefore);
                ActionCheck("harvest_current_clock_contract",ReadyForPlayerInput()&&turns.TickCount==harvestTick&&turns.GetEnergy(player)==harvestEnergy,
                    "Current generic world InventoryAction dispatch consumes no turn; no Interact/Cast pose is emitted by HarvestablePart.");
                yield return Capture("actions-harvest-after");
                Require(ReadyForPlayerInput(),"Harvest returned to the live normal player boundary.");

                var enemyCell=ActionFixtureCell(zone);
                snapjaw=input.ZoneManager.Factory.CreateEntity("Snapjaw");
                Require(snapjaw!=null&&snapjaw.HasTag("Creature")&&snapjaw.GetPart<BrainPart>()!=null&&snapjaw.GetStatValue("Hitpoints")>0,
                    "Actual factory Snapjaw with its authored anatomy, loadout, health and brain.");
                Require(FactionManager.IsHostile(player,snapjaw),"Native faction relation must permit an ordinary bump attack; never force hostility.");
                Require(zone.AddEntity(snapjaw,enemyCell.x,enemyCell.y),"Place only the declared enemy adjacent to the player.");
                snapjaw.GetPart<BrainPart>().CurrentZone=zone; // Required context for this newly placed fixture only.
                turns.AddEntity(snapjaw); // Normal registration; Brain initializes its own native RNG on TakeTurn.
                ZoneRenderHooks.MarkCellDirty(enemyCell.x,enemyCell.y,"SpawnRingNative.ActionSnapjaw");
                yield return null;yield return null;
                actionViews.Add(ActionView(player,"combat-player-before"));actionViews.Add(ActionView(snapjaw,"combat-fixture-before"));
                Require(actionViews.Last().shown&&ActionView(player,"preflight").shown,"Both actual actor models are visible before the action.");
                ActionCheck("no_stale_combat_pose",QueuedActionClip(player)!="Attack"&&QueuedActionClip(player)!="Hit"&&QueuedActionClip(snapjaw)!="Attack"&&QueuedActionClip(snapjaw)!="Hit");
                int enemyHpBefore=snapjaw.GetStatValue("Hitpoints"),playerHpBefore=player.GetStatValue("Hitpoints"),tickBefore=turns.TickCount;
                var bumpFrom=Position();int combatLogStart=MessageLog.Count;string enemyDisplayName=snapjaw.GetDisplayName();
                attackObserver=(attacker,defender,current)=>
                {
                    if(!ReferenceEquals(current,zone))return;
                    if(ReferenceEquals(attacker,player)&&ReferenceEquals(defender,snapjaw))RecordActionHook("Attack",attacker,defender,current,0,false,"player-to-fixture");
                    else if(ReferenceEquals(attacker,snapjaw)&&ReferenceEquals(defender,player))RecordActionHook("Attack",attacker,defender,current,0,false,"fixture-retaliation");
                };
                damageObserver=(target,source,current,amount,lethal)=>
                {
                    if(ReferenceEquals(current,zone)&&(ReferenceEquals(target,player)||ReferenceEquals(target,snapjaw)))
                        RecordActionHook("Hit",target,source,current,amount,lethal,ReferenceEquals(target,snapjaw)?"fixture-damage":"player-damage");
                };
                EntityVisualHooks.AttackCallback+=attackObserver;EntityVisualHooks.DamageCallback+=damageObserver;
                yield return Capture("actions-combat-before");
                // One real movement-key pulse through InputHandler's hostile bump
                // branch. No direct CombatSystem call, RNG replacement or retry.
                yield return PulseActionKey(ActionDirection(bumpFrom,enemyCell));
                actionViews.Add(ActionView(player,"combat-player-action-frame"));actionViews.Add(ActionView(snapjaw,"combat-fixture-action-frame"));
                yield return Capture("actions-combat-action-frame");
                int enemyHpAfter=snapjaw.GetStatValue("Hitpoints"),playerHpAfter=player.GetStatValue("Hitpoints");
                var attempted=actionHooks.Where(r=>r.route=="player-to-fixture").ToArray();
                var dealt=actionHooks.Where(r=>r.route=="fixture-damage").ToArray();
                int damageFromPlayer=dealt.Where(r=>r.otherIsPlayer).Sum(r=>r.amount);
                ActionCheck("one_native_bump_attack",attempted.Length==1&&attempted[0].nativeX==bumpFrom.x&&attempted[0].nativeY==bumpFrom.y
                    &&turns.TickCount>tickBefore&&ReferenceEquals(input.CurrentZone,zone)&&ReadyForPlayerInput(),"Actual ordinary input/turn; player HP="+playerHpBefore+" -> "+playerHpAfter);
                ActionCheck("actual_attack_reaches_ring_consumer",attempted.Length==1&&attempted[0].view.shown&&attempted[0].view.animatorHasExpected&&attempted[0].view.queuedClip=="Attack",
                    "Additive callback reads the exact ring view after its listener. Retaliation can supersede Attack with Hit before rendering.");
                ActionCheck("fixture_hp_loss_has_damage_callbacks",dealt.Sum(r=>r.amount)>=Math.Max(0,enemyHpBefore-enemyHpAfter));
                if(dealt.Length==0)
                    ActionCheck("native_no_damage_countercheck",enemyHpAfter==enemyHpBefore&&damageFromPlayer==0,
                        "Valid native miss/resist/no-penetration outcome. Fixture Hit pose unobserved; no retry or artificial damage.");
                else
                    ActionCheck("actual_damage_reaches_hit_consumer",dealt.All(r=>r.amount>0&&(!r.nativeVisible||(r.view.shown&&r.view.animatorHasExpected&&r.view.queuedClip=="Hit"))),
                        "HP="+enemyHpBefore+" -> "+enemyHpAfter+"; player-attributed damage="+damageFromPlayer+"; exact source IDs retained, including any native third-party damage.");
                var playerDamage=actionHooks.Where(r=>r.route=="player-damage").ToArray();
                ActionCheck("player_hp_loss_has_damage_callbacks",playerDamage.Sum(r=>r.amount)>=Math.Max(0,playerHpBefore-playerHpAfter));
                ActionCheck("visible_player_damage_reaches_hit_consumer",playerDamage.All(r=>r.amount>0&&(!r.nativeVisible||(r.view.shown&&r.view.animatorHasExpected&&r.view.queuedClip=="Hit"))));
                combatLog=MessageLog.GetMessages().Skip(combatLogStart).ToArray();
                ActionCheck("native_combat_log_observed",combatLog.Any(line=>line.IndexOf(enemyDisplayName,StringComparison.Ordinal)>=0),"Only messages added after this attack preflight are included.");
                yield return new WaitForSecondsRealtime(.35f);
                actionViews.Add(ActionView(player,"combat-player-after"));actionViews.Add(ActionView(snapjaw,"combat-fixture-after"));
                yield return Capture("actions-combat-after");
                Require(ReadyForPlayerInput(),"Action probe ends alive with native controls usable; never heal/resurrect/force-yield.");
                actionComplete=true;
            }
            finally
            {
                EntityVisualHooks.AttackCallback-=attackObserver;EntityVisualHooks.DamageCallback-=damageObserver;
                ActionCheck("additive_visual_hooks_restored",!ActionDelegateContains(EntityVisualHooks.AttackCallback,attackObserver)
                    &&!ActionDelegateContains(EntityVisualHooks.DamageCallback,damageObserver)
                    &&ActionDelegatesPreserved(originalAttack,EntityVisualHooks.AttackCallback)&&ActionDelegatesPreserved(originalDamage,EntityVisualHooks.DamageCallback));
                // No broad before/after entity deletion: legitimate loot/corpses,
                // harvest yield and all real preexisting actors/items remain native.
                var preserved=zone.GetReadOnlyEntities().Where(e=>!ReferenceEquals(e,vein)&&!ReferenceEquals(e,snapjaw))
                    .Select(e=>(entity:e,cell:zone.GetEntityCell(e))).ToArray();
                ActionRemoveFixture(zone,turns,snapjaw);ActionRemoveFixture(zone,turns,vein);
                actionCleanup=(snapjaw==null||(!turns.IsRegistered(snapjaw)&&zone.GetEntityCell(snapjaw)==null))
                    &&(vein==null||zone.GetEntityCell(vein)==null)&&preserved.All(p=>ReferenceEquals(zone.GetEntityCell(p.entity),p.cell));
                ActionCheck("only_declared_fixtures_removed",actionCleanup);
                ActionCheck("optional_action_workload_complete",actionComplete);
                var result=new ActionProbeReport {runId=RunId,saveRoot=saveRoot,zoneId=zone.ZoneID,worldSeed=input.WorldMap.Seed,
                    complete=actionComplete,cleanup=actionCleanup,failures=actionChecks.Count(c=>!c.pass),startTick=startTick,endTick=turns.TickCount,
                    startPlayerHp=startHp,endPlayerHp=player.GetStatValue("Hitpoints"),harvestYield=harvestYield,checks=actionChecks.ToArray(),hooks=actionHooks.ToArray(),views=actionViews.ToArray(),combatLog=combatLog,
                    screenshots=screenshots.Where(f=>Path.GetFileName(f).StartsWith(Stem+"-actions-",StringComparison.Ordinal)).ToArray(),
                    bounds="Optional native acceptance after F5/F6 and UI gates, not profiling. Actual factory GlowQuartzVein and Snapjaw are explicit temporary fixture entities on existing adjacent visible empty cells. Real C/direction/resolved menu shortcut harvest and one ordinary hostile bump key. No HP/stat, faction, RNG, speed, AI policy, turn energy or clock override; newly placed Snapjaw gets normal CurrentZone/TurnManager registration. A miss/resist is valid and reports unobserved fixture Hit. Damage callbacks record exact sources and the live ring consumer's queued clip, controller and current/next Animator hashes; same-call retaliation/death may supersede or hide a pose before the screenshot. No claim that an unobserved Attack/Hit frame rendered or that Harvest emits Interact. Actor death/blocked input fails usability honestly. Only the two declared entities are removed/unregistered; real harvest yield, death loot/corpse/gear, reputation, damage and other normal consequences remain in this private disposable session. No subsequent save or load is performed by this partial. Human screenshot inspection remains required."};
                File.WriteAllText(Path.Combine(Dir,Stem+"-actions.json"),JsonUtility.ToJson(result,true));
            }
        }

        (int x,int y) ActionFixtureCell(Zone zone)
        {
            var at=Position();int[] dx={1,-1,0,0},dy={0,0,1,-1};
            for(int i=0;i<4;i++)
            {
                var cell=zone.GetCell(at.x+dx[i],at.y+dy[i]);
                if(cell!=null&&cell.IsVisible&&!cell.BlocksMovement(input.PlayerEntity)&&cell.Objects.All(WorldInteractionSystem.IsTerrain))return(cell.X,cell.Y);
            }
            throw new InvalidOperationException("No visible adjacent empty native floor for the declared action fixture; do not clear the real world.");
        }
        static Key ActionDirection((int x,int y) from,(int x,int y) to)
        {
            int dx=to.x-from.x,dy=to.y-from.y;
            if(dx==1&&dy==0)return Key.D;if(dx==-1&&dy==0)return Key.A;if(dy==1&&dx==0)return Key.S;if(dy==-1&&dx==0)return Key.W;
            throw new InvalidOperationException("Native action fixture must remain cardinally adjacent.");
        }
        IEnumerator PulseActionKey(Key key)
        {
            double start=Time.realtimeSinceStartupAsDouble;
            while(Time.time-(float)typeof(InputHandler).GetField("_lastMoveTime",Private).GetValue(input)<input.MoveRepeatDelay)
            {Require(Time.realtimeSinceStartupAsDouble-start<3,"Native action input rate gate did not reopen.");yield return null;}
            keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
            // No post-key sleep: next capture observes the first available action frame.
        }
        static int ActionYieldUnits(Zone zone,Entity player,string blueprint)
        {
            int total=0;var inventory=player.GetPart<InventoryPart>();
            if(inventory!=null)foreach(var e in inventory.Objects)if(e.BlueprintName==blueprint)total+=Math.Max(0,e.GetPart<StackerPart>()?.StackCount??1);
            foreach(var e in zone.GetReadOnlyEntities())if(e.BlueprintName==blueprint)total+=Math.Max(0,e.GetPart<StackerPart>()?.StackCount??1);
            return total;
        }
        void RecordActionHook(string clip,Entity actor,Entity other,Zone zone,int amount,bool lethal,string route)
        {
            try
            {
                var at=zone.GetEntityPosition(actor);var view=ActionView(actor,"callback-"+clip,clip);
                actionHooks.Add(new ActionHookRow {route=route,clip=clip,actorId=actor.ID,otherId=other?.ID,zoneId=zone.ZoneID,amount=amount,lethal=lethal,otherIsPlayer=ReferenceEquals(other,input.PlayerEntity),
                    frame=Time.frameCount,nativeX=at.x,nativeY=at.y,tick=input.TurnManager.TickCount,nativeVisible=zone.GetEntityCell(actor)?.IsVisible==true,view=view});
            }
            catch(Exception error){ActionCheck("observer_failed",false,error.ToString());} // Never break actual combat with an audit observer.
        }
        string QueuedActionClip(Entity entity)
        {
            var p=Presenter();if(p==null)return null;
            var dict=typeof(SpawnRing3DPresenter).GetField("views",Private)?.GetValue(p) as IDictionary;
            if(dict==null||!dict.Contains(entity))return null;
            object view=dict[entity];return (string)view.GetType().GetField("Clip",BindingFlags.Public|BindingFlags.Instance).GetValue(view);
        }
        ActionViewRow ActionView(Entity entity,string stage,string expected=null)
        {
            var row=new ActionViewRow {stage=stage,id=entity?.ID,blueprint=entity?.BlueprintName,frame=Time.frameCount,expectedClip=expected};
            if(entity==null)return row;var p=Presenter();var at=input.CurrentZone.GetEntityPosition(entity);row.nativeX=at.x;row.nativeY=at.y;
            if(p==null||!p.TryGetEntityView(entity,out var root,out string model)||root==null)return row;
            row.modelId=model;row.rootName=root.name;row.rootInstanceId=root.GetInstanceID();row.shown=p.IsRenderedEntity(entity)&&root.activeInHierarchy;
            row.worldX=root.transform.position.x;row.worldY=root.transform.position.y;row.worldZ=root.transform.position.z;row.queuedClip=QueuedActionClip(entity);
            var animator=root.GetComponentInChildren<Animator>(true);if(animator==null)return row;
            row.animatorName=animator.name;row.controller=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.name:null;
            if(animator.layerCount>0)
            {row.currentState=animator.GetCurrentAnimatorStateInfo(0).shortNameHash;row.nextState=animator.GetNextAnimatorStateInfo(0).shortNameHash;
                row.inTransition=animator.IsInTransition(0);row.animatorHasExpected=expected!=null&&animator.HasState(0,Animator.StringToHash(expected));}
            return row;
        }
        void ActionRemoveFixture(Zone zone,TurnManager turns,Entity entity)
        {
            if(entity==null)return;
            try{turns.RemoveEntity(entity);var cell=zone.GetEntityCell(entity);if(cell!=null){zone.RemoveEntity(entity);if(ReferenceEquals(input.CurrentZone,zone))ZoneRenderHooks.MarkCellDirty(cell.X,cell.Y,"SpawnRingNative.ActionCleanup");}}
            catch(Exception error){ActionCheck("fixture_cleanup_failed",false,error.ToString());}
        }
        static bool ActionDelegateContains(Delegate live,Delegate probe)=>probe!=null&&live!=null&&live.GetInvocationList().Contains(probe);
        static bool ActionDelegatesPreserved(Delegate original,Delegate live)=>original==null||original.GetInvocationList().All(d=>live!=null&&live.GetInvocationList().Contains(d));
        void ActionCheck(string name,bool pass,string detail=null){actionChecks.Add(new ActionProbeCheck{name=name,pass=pass,detail=detail});Check("actions:"+name,pass,detail);}
        [Serializable] public sealed class ActionProbeCheck{public string name,detail;public bool pass;}
        [Serializable] public sealed class ActionHookRow{public string route,clip,actorId,otherId,zoneId;public int amount,frame,nativeX,nativeY,tick;public bool lethal,nativeVisible,otherIsPlayer;public ActionViewRow view;}
        [Serializable] public sealed class ActionViewRow{public string stage,id,blueprint,modelId,rootName,animatorName,controller,queuedClip,expectedClip;public int frame,rootInstanceId,nativeX,nativeY,currentState,nextState;public float worldX,worldY,worldZ;public bool shown,inTransition,animatorHasExpected;}
        [Serializable] public sealed class ActionProbeReport{public string runId,saveRoot,zoneId,bounds;public int worldSeed,failures,startTick,endTick,startPlayerHp,endPlayerHp,harvestYield;public bool complete,cleanup;
            public ActionProbeCheck[] checks;public ActionHookRow[] hooks;public ActionViewRow[] views;public string[] screenshots,combatLog;}
    }
}
