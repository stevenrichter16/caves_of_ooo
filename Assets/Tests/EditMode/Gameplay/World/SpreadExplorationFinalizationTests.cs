using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadExplorationFinalizationTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        DensityLootTestScope scope;IDictionary loot;DictionaryEntry[] oldLoot;object oldLootInit;
        Dictionary<string,LiquidDefinition> oldLiquids;bool oldLiquidInit;
        [SetUp]public void Setup()
        {
            loot=(IDictionary)typeof(LootTableRegistry).GetField("_byName",All).GetValue(null);var rows=new List<DictionaryEntry>();foreach(DictionaryEntry e in loot)rows.Add(e);oldLoot=rows.ToArray();oldLootInit=typeof(LootTableRegistry).GetField("_initialized",All).GetValue(null);
            var liquid=(Dictionary<string,LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId",All).GetValue(null);oldLiquids=new Dictionary<string,LiquidDefinition>(liquid);oldLiquidInit=LiquidRegistry.IsInitialized;
            scope=new DensityLootTestScope();LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));
        }
        [TearDown]public void Cleanup()
        {
            try{scope?.Dispose();}finally
            {
                if(loot!=null&&oldLoot!=null){loot.Clear();foreach(var e in oldLoot)loot.Add(e.Key,e.Value);typeof(LootTableRegistry).GetField("_initialized",All).SetValue(null,oldLootInit);}
                if(oldLiquids!=null){var d=(Dictionary<string,LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId",All).GetValue(null);d.Clear();foreach(var p in oldLiquids)d.Add(p.Key,p.Value);typeof(LiquidRegistry).GetField("_initialized",All).SetValue(null,oldLiquidInit);}
            }
        }
        sealed class Callback:IZoneBuilder
        {public string Name=>"FixtureBeforeExploration";public int Priority=>4299;public Action<Zone> Run;public bool BuildZone(Zone z,EntityFactory f,System.Random rng){Run?.Invoke(z);return true;}}
        sealed class ObservedManager:OverworldZoneManager
        {
            public SpreadCompositionBuilder Terrain;public SpreadExplorationBuilder Composer;public ContainerBuilder Containers;public PopulationBuilder Population;public Action<Zone> Before,After;public Zone Staged;public bool Committed;
            public ObservedManager(EntityFactory f):base(f,64){typeof(OverworldZoneManager).GetProperty("Exploration",All).SetValue(this,SpreadExplorationPlan.Create(this));}
            protected override ZoneGenerationPipeline GetPipelineForZone(string id)
            {var p=base.GetPipelineForZone(id);Terrain=p.Builders.OfType<SpreadCompositionBuilder>().SingleOrDefault();Composer=p.Builders.OfType<SpreadExplorationBuilder>().SingleOrDefault();Containers=p.Builders.OfType<ContainerBuilder>().SingleOrDefault();Population=p.Builders.OfType<PopulationBuilder>().SingleOrDefault();p.AddBuilder(new Callback{Run=z=>Before?.Invoke(z)});return p;}
            protected override void OnZoneGenerated(Zone z,string id)
            {base.OnZoneGenerated(z,id);Staged=z;Committed=Composer?.LastResult==Exploration.Find(id)?.Family.ToString();if(Committed)After?.Invoke(z);}
        }
        static bool Passable(Cell c)=>c!=null&&!c.Occupants.Any(e=>e.HasTag("Creature")||e.HasTag("Solid")||e.GetPart<PhysicsPart>()?.Solid==true||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasPart<TriggerOnStepPart>()||e.GetPart<DoorPart>()?.IsClosed==true);
        static Entity Wall(Zone z,Cell c)
        {var wall=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="fixture-solid"};wall.AddPart(new PhysicsPart{Solid=true});Assert.True(z.AddEntity(wall,c.X,c.Y));return wall;}
        Zone FirstCommitted(ObservedManager m,SpreadExplorationFamily family)
        {
            foreach(var entry in m.Exploration.Entries.Where(e=>e.Family==family))
            {scope.Seed(64^FormationSelector.StableIndex(entry.ZoneID,int.MaxValue));m.Staged=null;m.Committed=false;var z=m.GetZone(entry.ZoneID);if(m.Committed)return z;}
            Assert.Fail("No actual committed source in finite seed64 selection for "+family);return null;
        }
        [TestCase(SpreadExplorationFamily.RoadSpill,false)][TestCase(SpreadExplorationFamily.RoadSpill,true)]
        [TestCase(SpreadExplorationFamily.OccupiedBank,false)][TestCase(SpreadExplorationFamily.OccupiedBank,true)]
        [TestCase(SpreadExplorationFamily.LastGleanings,false)][TestCase(SpreadExplorationFamily.LastGleanings,true)]
        [TestCase(SpreadExplorationFamily.WateringMargin,false)][TestCase(SpreadExplorationFamily.WateringMargin,true)]
        public void LaterCallbackCannotEraseAnAcceptedCriticalApproach(SpreadExplorationFamily family,bool block)
        {
            var m=new ObservedManager(scope.Factory);Cell changed=null;bool witnessed=false;
            m.After=z=>{
                changed=Enumerable.Range(0,Zone.Width*Zone.Height).Select(n=>z.GetCell(n%Zone.Width,n/Zone.Width))
                    .First(c=>block?(c.X==0||c.X==Zone.Width-1||c.Y==0||c.Y==Zone.Height-1)&&Passable(c)&&z.TileState.Get(c.X,c.Y)?.IsEmpty!=false
                    :c.X>2&&c.X<Zone.Width-3&&c.Y>2&&c.Y<Zone.Height-3&&c.Occupants.Any(e=>e.GetPart<PhysicsPart>()?.Solid==true&&!e.HasTag("Creature")));
                Assert.AreEqual(block,Passable(changed));Wall(z,changed);Assert.False(Passable(changed));witnessed=true;
            };
            var result=FirstCommitted(m,family);Assert.True(witnessed);
            if(block){Assert.IsNull(result);Assert.False(m.CachedZones.ContainsKey(m.Staged.ZoneID));Assert.AreEqual(0,m.Exploration.DispositionFor(m.Staged.ZoneID));}
            else{Assert.AreSame(m.Staged,result);Assert.AreEqual(2,m.Exploration.DispositionFor(result.ZoneID));}
        }
        [TestCase(false)][TestCase(true)]
        public void RoadPacketRetainsOtherClaimedSourceOwnersAtTheirOriginalPositions(bool move)
        {
            var m=new ObservedManager(scope.Factory);bool witnessed=false;
            var before=new Dictionary<Entity,(int x,int y)>();
            m.Before=z=>{before.Clear();foreach(var r in new[]{m.Containers?.SourceReceipt,m.Population?.LooseSourceReceipt}.Where(r=>r!=null))foreach(var e in r.Owners)before[e]=z.GetEntityPosition(e);};
            m.After=z=>{
                var receipt=new[]{m.Containers?.SourceReceipt,m.Population?.LooseSourceReceipt}.FirstOrDefault(r=>r!=null&&r.Owners.Count>1&&(bool)typeof(SpreadGenerationReceipt).GetField("consumed",All).GetValue(r));
                if(receipt==null)return;var selected=receipt.Owners.Where(e=>z.GetEntityPosition(e)!=before[e]).ToArray();Assert.AreEqual(1,selected.Length);
                var other=receipt.Owners.First(e=>e!=selected[0]);var old=z.GetEntityPosition(other);
                if(move){var g=Geometry(z,other);var t=g.GetType();var cell=Enumerable.Range(0,Zone.Width*Zone.Height).Select(n=>z.GetCell(n%Zone.Width,n/Zone.Width)).First(c=>(c.X,c.Y)!=old&&(bool)t.GetMethod("Place",All).Invoke(g,new object[]{c.X,c.Y}));Assert.True(z.MoveEntity(other,cell.X,cell.Y));Assert.AreNotEqual(old,z.GetEntityPosition(other));}
                witnessed=true;
            };
            foreach(var entry in m.Exploration.Entries.Where(e=>e.Family==SpreadExplorationFamily.RoadSpill))
            {
                scope.Seed(64^FormationSelector.StableIndex(entry.ZoneID,int.MaxValue));var z=m.GetZone(entry.ZoneID);if(!witnessed)continue;
                if(move){Assert.IsNull(z);Assert.False(m.CachedZones.ContainsKey(entry.ZoneID));Assert.Zero(m.Exploration.DispositionFor(entry.ZoneID));}
                else{Assert.NotNull(z);Assert.AreEqual(2,m.Exploration.DispositionFor(entry.ZoneID));}return;
            }
            Assert.Fail("No actual multi-owner consumed road receipt in finite seed64 cohort.");
        }
        [TestCase(false,false)][TestCase(false,true)][TestCase(true,false)][TestCase(true,true)]
        public void CurrentWaterSourceCellCannotAcquireAForeignBlocker(bool reuse,bool overlap)
        {
            var m=new ObservedManager(scope.Factory);Entity existing=null;bool witnessed=false;
            if(reuse)m.Before=z=>{var bank=Bank(z,m.Terrain);if(bank==null)return;existing=scope.Factory.CreateEntity("WaterPuddle");existing.GetPart<LiquidPoolPart>().Volume=1;Assert.True(z.AddEntity(existing,bank.X,bank.Y));};
            m.After=z=>{
                var source=reuse?existing:z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="SpreadDrawPoint");Assert.NotNull(source);var c=z.GetEntityCell(source);
                if(!overlap)c=Enumerable.Range(0,Zone.Width*Zone.Height).Select(n=>z.GetCell(n%Zone.Width,n/Zone.Width)).First(cell=>cell.X>2&&cell.X<Zone.Width-3&&cell.Y>2&&cell.Y<Zone.Height-3&&cell.Occupants.Any(e=>e.GetPart<PhysicsPart>()?.Solid==true&&!e.HasTag("Creature")));
                Wall(z,c);Assert.True(c.Occupants.Any(e=>e.GetPart<PhysicsPart>()?.Solid==true));witnessed=true;
            };
            var result=FirstCommitted(m,SpreadExplorationFamily.WateringMargin);Assert.True(witnessed);
            if(overlap){Assert.IsNull(result);Assert.False(m.CachedZones.ContainsKey(m.Staged.ZoneID));Assert.Zero(m.Exploration.DispositionFor(m.Staged.ZoneID));}
            else{Assert.NotNull(result);Assert.AreEqual(2,m.Exploration.DispositionFor(result.ZoneID));}
        }
        static object Geometry(Zone z,Entity ignored=null)
        {var t=typeof(SpreadWildernessSituationBuilder).GetNestedType("Geometry",BindingFlags.NonPublic);return Activator.CreateInstance(t,All,null,new object[]{z,ignored==null?new HashSet<Entity>():new HashSet<Entity>{ignored}},null);}
        static Cell Bank(Zone z,SpreadCompositionBuilder land)
        {
            var g=Geometry(z);var t=g.GetType();var reach=(bool[,])t.GetField("BorderReach",All).GetValue(g);
            for(int y=3;y<Zone.Height-3;y++)for(int x=3;x<Zone.Width-3;x++)
            {
                if(!(bool)t.GetMethod("Place",All).Invoke(g,new object[]{x,y})||!reach[x,y])continue;
                bool wet=new[]{(x-1,y),(x+1,y),(x,y-1),(x,y+1)}.Any(p=>land.Plan.IsWater(p.Item1,p.Item2)&&z.TileState.HasCoating(p.Item1,p.Item2,"water"));
                if(wet&&(bool)t.GetMethod("PreservesRoutes",All).Invoke(g,new object[]{new[]{(x,y)}}))return z.GetCell(x,y);
            }
            return null;
        }
        [TestCase(1)][TestCase(3)][TestCase(120)]
        public void ExistingPureFiniteWaterIsReusedWithoutAddingRefillingOrMoving(int volume)
        {
            var m=new ObservedManager(scope.Factory);Entity existing=null;Cell bank=null;int countBefore=0;string liquid=null;
            m.Before=z=>{bank=Bank(z,m.Terrain);if(bank==null)return;existing=scope.Factory.CreateEntity("WaterPuddle");existing.GetPart<LiquidPoolPart>().Volume=volume;liquid=existing.GetPart<LiquidPoolPart>().LiquidId;Assert.True(z.AddEntity(existing,bank.X,bank.Y));countBefore=z.GetReadOnlyEntities().Count;};
            var accepted=FirstCommitted(m,SpreadExplorationFamily.WateringMargin);Assert.NotNull(existing);Assert.NotNull(accepted);
            Assert.AreEqual(countBefore,accepted.GetReadOnlyEntities().Count,"Reusing a source must not add another draw owner.");
            Assert.AreSame(bank,accepted.GetEntityCell(existing));Assert.AreEqual(volume,existing.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(liquid,existing.GetPart<LiquidPoolPart>().LiquidId);
            Assert.False(accepted.GetReadOnlyEntities().Any(e=>e.BlueprintName=="SpreadDrawPoint"));
            m.UnloadZone(accepted.ZoneID);Assert.AreSame(accepted,m.GetZone(accepted.ZoneID));Assert.AreEqual(volume,existing.GetPart<LiquidPoolPart>().Volume);
        }
        [Test]public void ExistingExhaustedBankSourceDoesNotCreateAReplacementRefill()
        {
            var m=new ObservedManager(scope.Factory);Entity existing=null;Cell bank=null;
            foreach(var entry in m.Exploration.Entries.Where(e=>e.Family==SpreadExplorationFamily.WateringMargin))
            {
                m.Before=z=>{bank=Bank(z,m.Terrain);if(bank==null)return;existing=scope.Factory.CreateEntity("WaterPuddle");existing.GetPart<LiquidPoolPart>().Volume=0;Assert.True(z.AddEntity(existing,bank.X,bank.Y));};
                var z=m.GetZone(entry.ZoneID);if(existing==null)continue;Assert.NotNull(z);Assert.AreSame(bank,z.GetEntityCell(existing));Assert.Zero(existing.GetPart<LiquidPoolPart>().Volume);
                Assert.False(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="SpreadDrawPoint"));Assert.AreEqual(1,m.Exploration.DispositionFor(z.ZoneID));return;
            }
            Assert.Fail("No physical bank source in finite seed64 selection.");
        }
        [TestCase("mixed")][TestCase("portable")][TestCase("hidden")][TestCase("custom")]
        public void IncompatibleExistingPoolIsNeverChangedOrClaimedAsTheReusableSource(string fault)
        {
            var m=new ObservedManager(scope.Factory);Entity existing=null;Cell bank=null;int before=0;
            m.Before=z=>{bank=Bank(z,m.Terrain);if(bank==null)return;existing=scope.Factory.CreateEntity("WaterPuddle");existing.GetPart<LiquidPoolPart>().Volume=1;
                if(fault=="portable")existing.GetPart<PhysicsPart>().Takeable=true;
                if(fault=="hidden")existing.GetPart<RenderPart>().Visible=false;
                if(fault=="custom")existing.GetPart<RenderPart>().VisualID="fixture-custom-water";
                Assert.True(z.AddEntity(existing,bank.X,bank.Y));
                if(fault=="mixed")z.TileState.WriteCoating(bank.X,bank.Y,"acid",5);
                before=z.GetReadOnlyEntities().Count;};
            var z=FirstCommitted(m,SpreadExplorationFamily.WateringMargin);Assert.NotNull(existing);Assert.NotNull(z);
            Assert.AreSame(bank,z.GetEntityCell(existing));Assert.AreEqual(1,existing.GetPart<LiquidPoolPart>().Volume);
            Assert.AreEqual(before+1,z.GetReadOnlyEntities().Count);var added=z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="SpreadDrawPoint");
            Assert.AreEqual(int.Parse(scope.Factory.Blueprints["Waterskin"].Parts["Waterskin"]["Capacity"]),added.GetPart<LiquidPoolPart>().Volume);
            if(fault=="portable")Assert.True(existing.GetPart<PhysicsPart>().Takeable);
            if(fault=="hidden")Assert.False(existing.GetPart<RenderPart>().Visible);
            if(fault=="custom")Assert.AreEqual("fixture-custom-water",existing.GetPart<RenderPart>().VisualID);
            if(fault=="mixed")Assert.True(z.TileState.HasCoating(bank.X,bank.Y,"acid"));
        }
        [TestCase("spent")][TestCase("removed")]
        public void ReusedSourceChangedAfterCommitCannotInstallAValidUtilityClaim(string change)
        {
            var m=new ObservedManager(scope.Factory);Entity existing=null;
            m.Before=z=>{var bank=Bank(z,m.Terrain);if(bank==null)return;existing=scope.Factory.CreateEntity("WaterPuddle");existing.GetPart<LiquidPoolPart>().Volume=1;Assert.True(z.AddEntity(existing,bank.X,bank.Y));};
            bool witnessed=false;m.After=z=>{Assert.NotNull(existing);Assert.False(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="SpreadDrawPoint"));witnessed=true;if(change=="spent")existing.GetPart<LiquidPoolPart>().Volume=0;else Assert.True(z.RemoveEntity(existing));};
            Assert.IsNull(FirstCommitted(m,SpreadExplorationFamily.WateringMargin));Assert.True(witnessed);Assert.AreEqual(0,m.Exploration.DispositionFor(m.Staged.ZoneID));
        }
        [Test]public void ReuseRequiresNoNewDrawBlueprintAndSavedSpentSourceCannotRefill()
        {
            scope.Factory.Blueprints.Remove("SpreadDrawPoint");var m=new ObservedManager(scope.Factory);Entity existing=null;
            m.Before=z=>{var bank=Bank(z,m.Terrain);if(bank==null)return;existing=scope.Factory.CreateEntity("WaterPuddle");existing.GetPart<LiquidPoolPart>().Volume=1;Assert.True(z.AddEntity(existing,bank.X,bank.Y));};
            var accepted=FirstCommitted(m,SpreadExplorationFamily.WateringMargin);Assert.NotNull(accepted);Assert.NotNull(existing);string id=existing.ID;var at=accepted.GetEntityPosition(existing);existing.GetPart<LiquidPoolPart>().Volume=0;
            var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("reused-water","fixture",m,null,null));var restored=loaded.ZoneManager.GetZone(accepted.ZoneID);
            var saved=restored.GetReadOnlyEntities().Single(e=>e.ID==id);Assert.AreNotSame(existing,saved);Assert.AreEqual(at,restored.GetEntityPosition(saved));Assert.Zero(saved.GetPart<LiquidPoolPart>().Volume);
            loaded.ZoneManager.UnloadZone(restored.ZoneID);Assert.AreSame(restored,loaded.ZoneManager.GetZone(restored.ZoneID));Assert.False(restored.GetReadOnlyEntities().Any(e=>e.BlueprintName=="SpreadDrawPoint"));
        }
    }
}
