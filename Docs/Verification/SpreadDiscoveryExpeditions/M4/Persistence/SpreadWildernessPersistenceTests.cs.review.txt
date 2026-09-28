using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Save/command bridge, not an ordinary player journey: real producer stock
    // and loadouts, bounded fixture seed selection, then explicit player transfer.
    public sealed class SpreadWildernessPersistenceTests
    {
        DensityLootTestScope scope;
        IDictionary loot; DictionaryEntry[] oldLoot; object oldInitialized;
        static readonly BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic;
        [SetUp] public void Setup()
        {
            loot=(IDictionary)typeof(LootTableRegistry).GetField("_byName",Flags).GetValue(null);
            var entries=new List<DictionaryEntry>();foreach(DictionaryEntry entry in loot)entries.Add(entry);oldLoot=entries.ToArray();oldInitialized=typeof(LootTableRegistry).GetField("_initialized",Flags).GetValue(null);
            scope=new DensityLootTestScope();
        }
        [TearDown] public void Cleanup()
        {
            try{scope?.Dispose();}finally{if(loot!=null&&oldLoot!=null){loot.Clear();foreach(var e in oldLoot)loot.Add(e.Key,e.Value);if(oldInitialized!=null)typeof(LootTableRegistry).GetField("_initialized",Flags).SetValue(null,oldInitialized);}}
        }
        static Zone Spatial(Entity e)=>(Zone)typeof(Entity).GetField("SpatialZone",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(e);
        static int Units(Entity e)=>e.GetPart<StackerPart>()?.StackCount??1;
        static string Item(Entity e)=>e.ID+":"+e.BlueprintName+":"+Units(e)+":"+e.GetPart<PhysicsPart>()?.InInventory?.ID+":"+e.GetPart<PhysicsPart>()?.Equipped?.ID;
        static string Contents(Entity e)=>string.Join("|",e.GetPart<ContainerPart>().Contents.Select(Item));
        static string Gear(Entity e)=>string.Join("|",DensityLootTestScope.Gear(e).Select(Item).OrderBy(x=>x,StringComparer.Ordinal))
            +";slots="+string.Join("|",e.GetPart<InventoryPart>().EquippedItems.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value.ID))
            +";body="+string.Join("|",e.GetPart<Body>().GetParts().Select(p=>p.Type+":"+p._Laterality+":"+p._Equipped?.ID));
        static IEnumerable<string> Addresses(){for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)yield return WorldMap.ToZoneID(x,y);}
        static PopulationTable Group()=>new PopulationTable{Name="SpreadTier1",Entries=new List<PopulationEntry>{new PopulationEntry{BlueprintName="MarlbackScrabbler",EncounterGroup="SpreadTier1Encounter",MinCount=2,MaxCount=2}}};
        [TestCase("cargo")][TestCase("shelter")]
        public void RelocatedPartialCacheAndGuardGearSurviveCachedReturnAndFullSave(string kind)
        {
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64);
            string id=Addresses().First(x=>SpreadWildernessSituationPlan.Select(manager,x,manager.Wayhouse.ZoneID)==kind);
            Zone source=null;Entity cache=null;Entity[] guards=null;Entity taken=null;Entity player=null;
            // No stock is inserted/replaced. Pick a bounded producer RNG fixture
            // which actually rolled multiple distinct entries and usable gear.
            for(int trial=14;trial<46&&source==null;trial++)
            {
                scope.Seed(1);var z=new Zone(id);var terrain=new SpreadCompositionBuilder(64);
                var p=new PopulationBuilder(Group());var c=new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness);
                var composer=new SpreadWildernessSituationBuilder(manager,terrain,p,c,manager.Wayhouse.ZoneID);
                Assert.True(terrain.BuildZone(z,scope.Factory,new Random(1)));var rng=new Random(trial);
                Assert.True(p.BuildZone(z,scope.Factory,rng));Assert.True(c.BuildZone(z,scope.Factory,rng));
                Assert.True(new HaulablePropBuilder(BiomeType.Spread).BuildZone(z,scope.Factory,rng));
                var owner=c.SourceReceipt.Owners.FirstOrDefault(e=>(e.BlueprintName=="Crate"||e.BlueprintName=="Sack")&&!e.GetPart<ContainerPart>().IsLocked);
                if(owner==null||owner.GetPart<ContainerPart>().Contents.Count<2)continue;
                var actor=scope.Factory.CreateEntity("Player");var inventory=actor.GetPart<InventoryPart>();
                var item=owner.GetPart<ContainerPart>().Contents.FirstOrDefault(e=>InventoryPart.GetItemWeight(e)>0&&!inventory.Objects.Any(i=>i.BlueprintName==e.BlueprintName));
                if(item==null)continue;var before=z.GetEntityPosition(owner);
                Assert.True(composer.BuildZone(z,scope.Factory,new Random(711)));if(composer.LastResult!=kind)continue;
                Assert.AreNotEqual(before,z.GetEntityPosition(owner),"The saved cache must actually have been relocated.");
                source=z;cache=owner;guards=p.SourceReceipt.Owners.ToArray();taken=item;player=actor;
                TestContext.WriteLine("kind="+kind+" zone="+id+" fixtureRng="+trial+" movedCache="+cache.ID);
            }
            Assert.NotNull(source,"Bounded generated fixture needs a real committed partial-loot source.");
            var at=source.GetEntityPosition(cache);Assert.True(source.AddEntity(player,at.x,at.y));manager.SetActiveZone(source);
            var cp=cache.GetPart<ContainerPart>();var inv=player.GetPart<InventoryPart>();string untouched=Contents(cache);int originalCount=cp.Contents.Count;
            // Actual full-inventory refusal paired with the same-item successful
            // command; no source/owner replacement or generated stock refill.
            int maxWeight=inv.MaxWeight;inv.MaxWeight=inv.GetCarriedWeight();
            Assert.Greater(InventoryPart.GetItemWeight(taken),0);Assert.False(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cache,taken),player,source).Success);
            Assert.AreEqual(untouched,Contents(cache));Assert.AreSame(cache,taken.GetPart<PhysicsPart>().InInventory);
            inv.MaxWeight=maxWeight;Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cache,taken),player,source).Success);
            Assert.AreEqual(originalCount-1,cp.Contents.Count);Assert.Greater(cp.Contents.Count,0);Assert.False(cp.Contents.Contains(taken));
            Assert.AreSame(player,taken.GetPart<PhysicsPart>().InInventory);Assert.AreSame(taken,inv.Objects.Single(e=>e.ID==taken.ID));
            string partial=Contents(cache),playerGear=Gear(player);var anchors=guards.ToDictionary(e=>e.ID,e=>source.GetEntityPosition(e));var gear=guards.ToDictionary(e=>e.ID,Gear);
            Assert.True(guards.All(e=>DensityLootTestScope.Gear(e).Any()),"Real producer equipment makes guard save checks nonvacuous.");
            var away=new Zone("Overworld.0.0.0");Assert.True(source.RemoveEntity(player));Assert.True(away.AddEntity(player,1,1));manager.SetActiveZone(away);
            Assert.AreSame(source,manager.GetZone(id));Assert.AreEqual(partial,Contents(cache));
            var restored=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("m4-partial-"+kind,"fixture",manager,null,player));
            Assert.AreNotSame(player,restored.Player);Assert.AreEqual(away.ZoneID,restored.ZoneManager.ActiveZone.ZoneID);Assert.AreEqual(playerGear,Gear(restored.Player));
            var loaded=restored.ZoneManager.GetZone(id);Assert.AreNotSame(source,loaded);
            var savedCache=loaded.GetReadOnlyEntities().Single(e=>e.ID==cache.ID);Assert.AreNotSame(cache,savedCache);Assert.AreEqual(at,loaded.GetEntityPosition(savedCache));Assert.AreEqual(partial,Contents(savedCache));
            foreach(var item in savedCache.GetPart<ContainerPart>().Contents){Assert.AreSame(savedCache,item.GetPart<PhysicsPart>().InInventory);Assert.IsNull(Spatial(item));Assert.IsNull(loaded.GetEntityCell(item));}
            var savedTaken=restored.Player.GetPart<InventoryPart>().Objects.Single(e=>e.ID==taken.ID);Assert.AreNotSame(taken,savedTaken);Assert.AreEqual(Units(taken),Units(savedTaken));Assert.AreSame(restored.Player,savedTaken.GetPart<PhysicsPart>().InInventory);
            Assert.False(savedCache.GetPart<ContainerPart>().Contents.Any(e=>e.ID==taken.ID));
            foreach(var guard in guards){var current=loaded.GetReadOnlyEntities().Single(e=>e.ID==guard.ID);Assert.AreNotSame(guard,current);Assert.AreSame(loaded,Spatial(current));Assert.AreEqual(anchors[guard.ID],loaded.GetEntityPosition(current));Assert.AreEqual(gear[guard.ID],Gear(current));}
            var loadedAway=restored.ZoneManager.ActiveZone;Assert.True(loadedAway.RemoveEntity(restored.Player));Assert.True(loaded.AddEntity(restored.Player,at.x,at.y));restored.ZoneManager.SetActiveZone(loaded);
            Assert.AreSame(loaded,restored.ZoneManager.GetZone(id));Assert.AreEqual(partial,Contents(savedCache));Assert.AreEqual(playerGear,Gear(restored.Player));
            // Deliberately no UnloadZone: ordinary cached wilderness retention is
            // unchanged; this does not promise new permanent unload semantics.
        }
    }
}
