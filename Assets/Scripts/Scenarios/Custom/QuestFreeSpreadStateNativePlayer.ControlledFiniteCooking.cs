using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using Newtonsoft.Json;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class QuestFreeSpreadStateNativePlayer
    {
        const string ControlledFiniteBoundary="Controlled NPC scheduling, seed64 and the same first admitted actual5.11 source/west0,16. One disclosed original-player setup transfer. Only actual registered nonplayer turn entries are suspended across native westwalk/Harvest/Cook/67 total material passes. All suspended IDs/original energies and restoration are reported; NPC objects, locations, brains, stock and source are not edited. The current player clock/energy continue normally. Queue restoration precedes native map exit/F5/F6; exact NPC graph is compared while inactive BEFORE return. Normal NPC turns/movement on return are allowed and recorded, with no second pause. No source/food/heat/fuel/RNG grants.80 paid inputs/150seconds. This proves controlled mechanics/presentation only, not ordinary travel, threats, difficulty, survival or discovery. Original finite-cooking mode/failure remains separate. Pixels require human review.";
        bool ControlledFinite=>_mode=="finite-cooking-controlled";
        TurnManager _finiteSuspendedTurns;
        Zone _finiteSuspendedZone;
        Entity _finiteSuspendedPlayer;
        List<TurnManager.SavedTurnEntry> _finiteOriginalQueue;
        Dictionary<Entity,Part[]> _finiteNpcParts;
        string _finiteNpcGraph;
        bool _finiteQueueSuspended;
        int _finiteSuspensionTick,_finiteSuspensionEnergy;

        void BeginControlledDiagnostics()
        {
            const string key="turn-verbose";
            _exchangeOldChannels[key]=_exchangeChannelStore.TryGetValue(key,out var old)?(bool?)old:null;
            Diag.SetChannel(key,true);
        }
        IEnumerator ControlledFiniteCooking()
        {
            try { yield return FiniteCooking(); }
            finally { CleanupControlledSuspension(); }
        }
        void SuspendFiniteNpcTurns()
        {
            var turns=_input.TurnManager;var original=turns.GetSavedEntries();
            Require(!_finiteQueueSuspended&&turns.WaitingForInput&&turns.CurrentActor==Player
                &&original.Count(e=>e.Entity==Player)==1&&original.Select(e=>e.Entity).Distinct().Count()==original.Count,
                "ready original player and unique native queue before controlled suspension");
            var npcs=original.Where(e=>e.Entity!=Player).Select(e=>e.Entity).ToArray();
            Require(npcs.Length>0&&npcs.All(e=>ControlledCurrentNpc(Zone,e))
                &&npcs.Select(e=>e.ID).Distinct(StringComparer.Ordinal).Count()==npcs.Length,"all actual registered NPCs have exact current physical owners");
            _finiteSuspendedTurns=turns;_finiteSuspendedZone=Zone;_finiteSuspendedPlayer=Player;
            _finiteOriginalQueue=original;_finiteNpcParts=npcs.ToDictionary(e=>e,e=>e.Parts.ToArray());
            _finiteNpcGraph=ControlledNpcGraph(Zone,npcs);_finiteSuspensionTick=Tick;_finiteSuspensionEnergy=Energy;
            _observations.Add(new{phase="controlled-npc-suspension-before",zone=Zone.ZoneID,count=npcs.Length,tick=Tick,playerEnergy=Energy,
                currentActor=turns.CurrentActor?.ID,waiting=turns.WaitingForInput,queue=ControlledQueue(turns),npcs=ControlledNpcRows(Zone,npcs),
                boundary="Actual registered entries after generation and native entry hooks, including entry-born actors. Only scheduling entries will be removed; no actor is removed from its source."});
            // Own the reversal before the first mutation, so partial setup is cleaned too.
            _finiteQueueSuspended=true;
            foreach(var npc in npcs)turns.RemoveEntity(npc);
            ValidateFiniteSuspension("immediately-after-suspend");
            Check("controlled_suspension_keeps_player_clock_and_all_npc_owners",Tick==_finiteSuspensionTick&&Energy==_finiteSuspensionEnergy
                &&turns.CurrentActor==Player&&turns.WaitingForInput);
        }
        static bool ControlledCurrentNpc(Zone zone,Entity npc)
            =>npc!=null&&!npc.HasTag("Player")&&!string.IsNullOrEmpty(npc.ID)&&npc.SpatialZone==zone
                &&zone.GetEntityCell(npc)?.ParentZone==zone&&zone.GetEntityCell(npc)?.Objects.Contains(npc)==true
                &&zone.GetReadOnlyEntities().Count(e=>e.ID==npc.ID)==1;
        static IEnumerable<Entity> ControlledNpcItems(Entity npc)
            =>(npc.GetPart<InventoryPart>()?.Objects??new List<Entity>())
                .Concat(npc.GetPart<InventoryPart>()?.GetAllEquipped()??Enumerable.Empty<Entity>())
                .Concat((npc.GetPart<Body>()?.GetParts()??new List<BodyPart>()).Select(p=>p.Equipped).Where(e=>e!=null))
                .Concat(npc.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Distinct();
        static object[] ControlledNpcRows(Zone zone,IEnumerable<Entity> npcs)=>npcs.OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>(object)new
        {
            id=e.ID,blueprint=e.BlueprintName,zone=e.SpatialZone?.ZoneID,position=zone.GetEntityPosition(e),hp=e.GetStatValue("Hitpoints"),drams=TradeSystem.GetDrams(e),
            brainZone=e.GetPart<BrainPart>()?.CurrentZone?.ZoneID,
            parts=e.Parts.Select(p=>p.GetType().FullName).ToArray(),
            body=(e.GetPart<Body>()?.GetParts()??new List<BodyPart>()).Select(p=>new{id=p.ID,equipped=p.Equipped?.ID}).ToArray(),
            items=ControlledNpcItems(e).OrderBy(i=>i.ID,StringComparer.Ordinal).Select(i=>new{id=i.ID,blueprint=i.BlueprintName,quantity=CookingUnits(i),
                inventory=i.GetPart<PhysicsPart>()?.InInventory?.ID,equipped=i.GetPart<PhysicsPart>()?.Equipped?.ID,spatialZone=i.SpatialZone?.ZoneID}).ToArray()
        }).ToArray();
        static string ControlledNpcGraph(Zone zone,IEnumerable<Entity> npcs)=>JsonConvert.SerializeObject(ControlledNpcRows(zone,npcs));
        static object[] ControlledQueue(TurnManager turns)=>turns.GetSavedEntries().Select(e=>(object)new{id=e.Entity?.ID,blueprint=e.Entity?.BlueprintName,energy=e.Energy}).ToArray();
        bool ControlledOriginalOwnersCurrent()=>_finiteNpcParts.All(pair=>ControlledCurrentNpc(_finiteSuspendedZone,pair.Key)
            &&pair.Key.Parts.SequenceEqual(pair.Value)&&pair.Value.All(part=>part.ParentEntity==pair.Key));
        void ValidateFiniteSuspension(string label)
        {
            Require(_finiteQueueSuspended&&_input.TurnManager==_finiteSuspendedTurns&&Zone==_finiteSuspendedZone&&Player==_finiteSuspendedPlayer
                &&_finiteSuspendedTurns.Entities.SequenceEqual(new[]{Player})&&ControlledOriginalOwnersCurrent()
                &&ControlledNpcGraph(Zone,_finiteNpcParts.Keys)==_finiteNpcGraph,"controlled current queue/NPC graph unchanged: "+label);
        }
        void ObserveControlledPaid(string label)
        {
            ValidateFiniteSuspension(label);
            var npcTurns=_exchangeLastWindow.Where(e=>e.Category=="turn-verbose"&&(e.Kind=="Begin"||e.Kind=="End")).ToArray();
            _observations.Add(new{phase="controlled-paid-scheduling",label,pass=_finitePasses,tick=Tick,playerEnergy=Energy,
                suspendedCount=_finiteNpcParts.Count,registered=ControlledQueue(_finiteSuspendedTurns),npcTurns,npcGraphUnchanged=true});
            Require(npcTurns.Length==0,"no NPC turn during explicitly suspended interval");
        }
        void RestoreFiniteNpcTurns(bool strict)
        {
            if(!_finiteQueueSuspended)return;
            var turns=_finiteSuspendedTurns;var current=turns.GetSavedEntries();
            int tick=turns.TickCount;bool waiting=turns.WaitingForInput;var actor=turns.CurrentActor;
            bool sameRuntime=_input!=null&&_input.TurnManager==turns&&Player==_finiteSuspendedPlayer&&Zone==_finiteSuspendedZone;
            bool owned=sameRuntime&&current.Count==1&&current[0].Entity==_finiteSuspendedPlayer&&ControlledOriginalOwnersCurrent();
            var restored=new List<TurnManager.SavedTurnEntry>();
            if(owned)
            {
                foreach(var entry in _finiteOriginalQueue)restored.Add(entry.Entity==_finiteSuspendedPlayer?current[0]:entry);
            }
            else
            {
                // Preserve independent current entries/energies. Do not resurrect removed or replaced owners.
                restored.AddRange(current);
                if(sameRuntime)foreach(var entry in _finiteOriginalQueue)
                    if(entry.Entity!=_finiteSuspendedPlayer&&!restored.Any(e=>e.Entity==entry.Entity)
                        &&ControlledCurrentNpc(_finiteSuspendedZone,entry.Entity))restored.Add(entry);
            }
            turns.RestoreSavedState(tick,waiting,actor,restored);
            _finiteQueueSuspended=false;
            var after=turns.GetSavedEntries();
            bool currentPreserved=turns.TickCount==tick&&turns.WaitingForInput==waiting&&turns.CurrentActor==actor
                &&current.All(c=>after.Count(e=>e.Entity==c.Entity&&e.Energy==c.Energy)==1);
            bool graphUnchanged=ControlledOriginalOwnersCurrent()&&ControlledNpcGraph(_finiteSuspendedZone,_finiteNpcParts.Keys)==_finiteNpcGraph;
            bool exactRestored=owned&&after.Count==_finiteOriginalQueue.Count&&after.Select(e=>e.Entity).SequenceEqual(_finiteOriginalQueue.Select(e=>e.Entity))
                &&_finiteOriginalQueue.Where(e=>e.Entity!=_finiteSuspendedPlayer).All(old=>after.Single(e=>e.Entity==old.Entity).Energy==old.Energy);
            _observations.Add(new{phase="controlled-npc-suspension-restored",strict,owned,exactRestored,currentPreserved,graphUnchanged,
                fromTick=_finiteSuspensionTick,toTick=tick,initialPlayerEnergy=_finiteSuspensionEnergy,currentPlayerEnergy=turns.GetEnergy(_finiteSuspendedPlayer),
                before= current.Select(e=>new{id=e.Entity?.ID,energy=e.Energy}).ToArray(),after=ControlledQueue(turns),
                suspended=_finiteOriginalQueue.Where(e=>e.Entity!=_finiteSuspendedPlayer).Select(e=>new{id=e.Entity.ID,originalEnergy=e.Energy,
                    registered=turns.IsRegistered(e.Entity),restoredEnergy=turns.GetEnergy(e.Entity),energyDelta=turns.GetEnergy(e.Entity)-e.Energy}).ToArray(),
                boundary="Restored before exit. Current player energy, waiting actor and tick were preserved; suspended NPC energies are setup restoration, not earned turns."});
            if(!(exactRestored&&currentPreserved&&graphUnchanged))
            {
                const string failure="Controlled scheduler restoration encountered changed owners/queue; independent entries were preserved and no removed owner was resurrected.";
                if(strict)throw new InvalidOperationException(failure);
                _fatal=(_fatal==null?"":_fatal+"\n")+failure;
            }
        }
        void CleanupControlledSuspension()
        {
            if(!_finiteQueueSuspended)return;
            try { RestoreFiniteNpcTurns(false); }
            catch(Exception error){_fatal=(_fatal==null?"":_fatal+"\n")+"Controlled scheduler cleanup failed: "+error;}
        }
        void ObserveControlledInactiveLoad(Zone restoredZone)
        {
            Require(!_finiteQueueSuspended&&Zone!=restoredZone,"queue restored and source inactive before controlled F6 graph proof");
            var ids=new HashSet<string>(_finiteNpcParts.Keys.Select(e=>e.ID),StringComparer.Ordinal);
            var npcs=restoredZone.GetReadOnlyEntities().Where(e=>ids.Contains(e.ID)).ToArray();
            bool exact=npcs.Length==ids.Count&&npcs.All(e=>ControlledCurrentNpc(restoredZone,e)&&!_finiteNpcParts.ContainsKey(e))
                &&ControlledNpcGraph(restoredZone,npcs)==_finiteNpcGraph;
            _observations.Add(new{phase="controlled-inactive-loaded-npcs",zone=restoredZone.ZoneID,count=npcs.Length,exact,npcs=ControlledNpcRows(restoredZone,npcs),
                boundary="Replacement NPC graph compared while source is inactive, before any return. Future ordinary NPC turns and movement are allowed."});
            Check("controlled_exact_npc_graph_survives_inactive_full_save",exact);
        }
        void ObserveControlledReturn(Zone restoredZone)
        {
            Require(!_finiteQueueSuspended,"ordinary return is never re-paused");
            var ids=new HashSet<string>(_finiteNpcParts.Keys.Select(e=>e.ID),StringComparer.Ordinal);
            var npcs=restoredZone.GetReadOnlyEntities().Where(e=>ids.Contains(e.ID)).ToArray();
            _observations.Add(new{phase="controlled-ordinary-return-npcs",zone=restoredZone.ZoneID,queue=ControlledQueue(_input.TurnManager),
                npcs=ControlledNpcRows(restoredZone,npcs),turns=_exchangeLastWindow.Where(e=>e.Category=="turn-verbose").ToArray(),
                boundary="NPC scheduling is ordinary after restoration. These return actions/positions may differ; no zero-NPC-turn or unchanged-position assertion is made."});
        }
    }
}
