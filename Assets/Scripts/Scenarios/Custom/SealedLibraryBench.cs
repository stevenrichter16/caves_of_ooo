using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    [Scenario(name:"Sealed Library Access Audit",category:"World",
        description:"Sealed architecture controls, native locked/keyed entry, then75 seconds of walking through the unlocked door.")]
    public sealed class SealedLibraryBench : IScenario
    {
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public string RunId { get; private set; }
        public Entity Door { get; private set; }
        public int DoorX { get; private set; }
        public int DoorY { get; private set; }
        public readonly List<string> Audit=new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            RunId=Guid.NewGuid().ToString("N");Cases=Failures=0;Audit.Clear();Diag.SetChannel("scenario",true);
            Require(ctx.Factory.Blueprints.ContainsKey("SealedLibraryDoor"),"door blueprint");
            foreach(var e in ctx.Zone.GetAllEntities().ToArray()){ctx.Turns.RemoveEntity(e);ctx.Zone.RemoveEntity(e);}
            Require(new SealedLibraryBuilder().BuildZone(ctx.Zone,ctx.Factory,ctx.Rng),"archive authored");
            Door=ctx.Zone.GetAllEntities().Single(e=>e.BlueprintName=="SealedLibraryDoor");var p=ctx.Zone.GetEntityPosition(Door);DoorX=p.x;DoorY=p.y;
            var player=ctx.PlayerEntity;player.GetPart<StatusEffectsPart>()?.RemoveAllEffects();
            var hp=player.GetStat("Hitpoints");hp.Max=hp.BaseValue=1000000;hp.Penalty=0;
            ctx.Zone.AddEntity(player,DoorX-1,DoorY);ctx.Turns.AddEntity(player);ctx.Turns.ProcessUntilPlayerTurn();
            Check("closed_queries",ctx.Zone.GetCell(DoorX,DoorY).IsSolid()&&ctx.Zone.GetCell(DoorX,DoorY).IsWall());
            Check("keyless_bump_refused",!MovementSystem.TryMove(player,ctx.Zone,1,0)&&Door.GetPart<LockPart>().IsLocked);
            Check("forced_entry_refused",!ctx.Zone.MoveEntity(player,DoorX,DoorY)&&ctx.Zone.GetEntityPosition(player)==(DoorX-1,DoorY));
            Check("vault_refused",!new Acrobatics_Vault().OnCommand(new SkillEventContext{Attacker=player,Defender=player,Zone=ctx.Zone,Rng=ctx.Rng,DirectionX=1,DirectionY=0}));
            Check("demolition_refused",DestructionSystem.Damage(Door,int.MaxValue,player,ctx.Zone)==DestroyVerdict.Indestructible);
            Check("hauling_refused",DragRules.CanDrag(player,Door)==DragVerdict.Rooted);
            var key=GiveTestKey(player);Require(!MovementSystem.TryMove(player,ctx.Zone,1,0),"unlock bump consumes a move attempt");
            Check("matching_key_unlocks",!Door.GetPart<LockPart>().IsLocked&&!ctx.Zone.GetCell(DoorX,DoorY).IsWall());
            Check("unlocked_entry_control",MovementSystem.TryMove(player,ctx.Zone,1,0)&&MovementSystem.TryMove(player,ctx.Zone,1,0)
                &&ctx.Zone.GetEntityCell(player).HasObjectWithTag("ExcludeZoneArrival"));
            // Native input independently repeats the keyless and keyed flow.
            ctx.Zone.MoveEntity(player,DoorX-1,DoorY);player.GetPart<InventoryPart>().Objects.Remove(key);
            Door.GetPart<LockPart>().IsLocked=true;Door.GetPart<PhysicsPart>().Solid=true;
            ZoneRenderHooks.MarkFullDirty("SealedLibraryBench");
            if(Application.isPlaying)new GameObject("Sealed Library Native Audit").AddComponent<SealedLibraryBenchPlayer>().Initialize(ctx,this);
        }
        /// <summary>Scenario-only key, never added to shipped blueprints or loot.</summary>
        public static Entity GiveTestKey(Entity player)
        {
            var key=new Entity{BlueprintName="ScenarioLibraryKey"};key.AddPart(new KeyPart{KeyId=SealedLibraryBuilder.KeyID});
            player.GetPart<InventoryPart>().AddObject(key);return key;
        }
        public void Check(string name,bool passed)
        {
            Cases++;if(!passed)Failures++;Audit.Add(name+":"+(passed?"PASS":"FAIL"));
            Diag.Record("scenario","SealedLibraryAudit",payload:new{runId=RunId,name,passed});
        }
        private static void Require(bool value,string condition)
        {if(!value)throw new InvalidOperationException("Sealed-library audit precondition failed: "+condition);}
    }
}
