using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SaltVaultApproachAdversarialTests
    {
        HotbarSaveFixture scope;
        EntityFactory factory;
        OverworldZoneManager manager;
        Zone zone;
        Entity file,indexer,door,key;
        int rotation;
        static readonly BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        [SetUp] public void SetUp()
        { scope=new HotbarSaveFixture(false,false);factory=GrovelandsCompositionTests.Factory();manager=OverworldZoneManager.CreateDetached(factory,224401440);Build(0); }
        [TearDown] public void TearDown(){CreationProbe.Callback=null;scope?.Dispose();}
        (int x,int y) Point(int x,int y)
        { int dx=x-18,dy=y-8;for(int n=0;n<rotation;n++){int old=dx;dx=-dy;dy=old;}return(18+dx,8+dy); }
        Cell Cell(int x,int y){var p=Point(x,y);return zone.GetCell(p.x,p.y);}
        Entity Put(string bp,int x,int y)
        {var e=factory.CreateEntity(bp);var p=Point(x,y);Assert.True(zone.AddEntity(e,p.x,p.y));return e;}
        void Build(int facing)
        {
            rotation=facing;zone=new Zone(StillleafSaltVault.ZoneId);
            for(int y=5;y<=9;y++)for(int x=15;x<=17;x++){Put("StoneFloor",x,y);Cell(x,y).IsInterior=true;}
            for(int y=5;y<=11;y++)for(int x=18;x<=25;x++)Put("Sand",x,y);
            Put("SandstoneWall",18,7);Put("SandstoneWall",18,9);door=Put("VillageDoor",18,8);
            file=Put(StillleafSaltVault.CabinetBlueprint,17,6);file.ID=StillleafSaltVault.CabinetId;
            indexer=Put(StillleafSaltVault.IndexerBlueprint,16,6);indexer.ID=StillleafSaltVault.IndexerId;
            key=factory.CreateEntity(StillleafArchive.KeyBlueprint);key.ID=StillleafSaltVault.KeyId;
            Assert.True(file.GetPart<ContainerPart>().AddItem(key));
            foreach(var p in new[]{Point(17,8),Point(18,8),Point(19,8)})zone.GenReservedCells.Add(p);
        }
        bool Apply()
        {return (bool)typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SaltVaultArchiveApproach",true).GetMethod("TryApply",Static).Invoke(null,new object[]{zone,manager});}
        Dictionary<Entity,Cell> Snapshot()=>zone.GetReadOnlyEntities().ToDictionary(e=>e,e=>zone.GetEntityCell(e));
        void SameOwners(Dictionary<Entity,Cell> before)
        {CollectionAssert.AreEquivalent(before.Keys,zone.GetReadOnlyEntities());foreach(var p in before)Assert.AreSame(p.Value,zone.GetEntityCell(p.Key));}
        Entity Sand(int x,int y)=>Cell(x,y).Objects.Single(e=>e.BlueprintName=="Sand");
        Entity[] Paving()=>zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="StoneFloor"&&!zone.GetEntityCell(e).IsInterior).ToArray();
        public sealed class Meaning : Part { public override string Name=>"ArchiveGroundMeaning"; }
        public sealed class CreationProbe : RenderPart
        {
            public static Action<Entity> Callback;
            public override bool HandleEvent(GameEvent e)
            {if(e.ID=="ObjectCreated"&&ParentEntity.BlueprintName=="StoneFloor")Callback?.Invoke(ParentEntity);return base.HandleEvent(e);}
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void Adversarial_RealDoorOrientationPreservesThresholdAndExistingArchive(int facing)
        {
            Build(facing);var before=Snapshot();var reserved=zone.GenReservedCells.ToArray();var contents=file.GetPart<ContainerPart>().Contents.ToArray();
            Assert.True(Apply());Assert.AreEqual(9,Paving().Length);
            foreach(var e in before.Keys.Where(e=>e.BlueprintName!="Sand"))Assert.AreSame(before[e],zone.GetEntityCell(e));
            CollectionAssert.AreEquivalent(reserved,zone.GenReservedCells);CollectionAssert.AreEqual(contents,file.GetPart<ContainerPart>().Contents);Assert.True(file.GetPart<ContainerPart>().Locked);
            for(int x=20;x<=21;x++)for(int y=7;y<=9;y++)Assert.AreEqual("StoneFloor",Cell(x,y).Objects.Single().BlueprintName);
            for(int x=22;x<=24;x++)Assert.AreEqual("StoneFloor",Cell(x,8).Objects.Single().BlueprintName);
            Assert.AreSame(before[Sand(19,8)],zone.GetEntityCell(Sand(19,8)));Assert.AreSame(Cell(18,8),zone.GetEntityCell(door));
            var after=Snapshot();Assert.False(Apply(),"A second application cannot extend or duplicate the approach");SameOwners(after);
        }
        [TestCase("cache")][TestCase("address")][TestCase("biome")][TestCase("poi")][TestCase("name")][TestCase("faction")][TestCase("profile")]
        [TestCase("short-map")][TestCase("null-pois")][TestCase("missing-floor")][TestCase("unlocked")][TestCase("missing-file")][TestCase("missing-indexer")][TestCase("missing-door")][TestCase("missing-flank")]
        public void Adversarial_ChangedAuthorityOrArchiveDeclinesWithoutPaving(string fault)
        {
            if(fault=="cache")manager.CachedZones[zone.ZoneID]=zone;
            if(fault=="address")zone.ZoneID="Overworld.15.14.0";
            if(fault=="biome")manager.WorldMap.Tiles[15,15]=BiomeType.Sodden;
            if(fault=="poi")manager.WorldMap.SetPOI(15,15,new PointOfInterest(POIType.Lair,"the Salt-Vault","PaleCuration"));
            if(fault=="name")manager.WorldMap.GetPOI(15,15).Name="renamed";
            if(fault=="faction")manager.WorldMap.GetPOI(15,15).Faction="Villagers";
            if(fault=="profile")manager.WorldMap.GetPOI(15,15).Profile="TentCamp";
            if(fault=="short-map")manager.WorldMap.Tiles=new BiomeType[1,1];
            if(fault=="null-pois")manager.WorldMap.POIs=null;
            if(fault=="missing-floor")factory.Blueprints.Remove("StoneFloor");
            if(fault=="unlocked")file.GetPart<ContainerPart>().Locked=false;
            if(fault=="missing-file")zone.RemoveEntity(file);
            if(fault=="missing-indexer")zone.RemoveEntity(indexer);
            if(fault=="missing-door")zone.RemoveEntity(door);
            if(fault=="missing-flank")zone.RemoveEntity(Cell(18,7).Objects.Single(e=>e.BlueprintName=="SandstoneWall"));
            var before=Snapshot();Assert.False(Apply());SameOwners(before);
        }
        [TestCase("reserved")][TestCase("coating")][TestCase("resident")][TestCase("loose")][TestCase("footprint")][TestCase("foreign-part")]
        [TestCase("name")][TestCase("description")][TestCase("property")][TestCase("stat")][TestCase("missing-physics")][TestCase("interior")][TestCase("takeable")][TestCase("visual-cue")]
        public void Adversarial_CoreCannotEraseAnOwnerOrItsMeaning(string fault)
        {
            var sand=Sand(20,7);
            if(fault=="reserved")zone.GenReservedCells.Add((20,7));
            if(fault=="coating")zone.TileState.WriteCoating(20,7,"water",4);
            if(fault=="resident")Put("Villager",20,7);
            if(fault=="loose")Put("FireClay",20,7);
            if(fault=="footprint"){var e=factory.CreateEntity("FireClay");e.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});Assert.True(zone.AddEntity(e,19,7));Assert.True(Cell(20,7).Occupants.Contains(e));Assert.False(Cell(20,7).Objects.Contains(e));}
            if(fault=="foreign-part")sand.AddPart(new Meaning());
            if(fault=="name")sand.GetPart<RenderPart>().DisplayName="burial sand";
            if(fault=="description")sand.GetPart<ExaminablePart>().Text="There is a name pressed into this ground.";
            if(fault=="property")sand.Properties["Custody"]="a real owner";
            if(fault=="stat")sand.Statistics.Add("Hitpoints",new Stat{Name="Hitpoints",BaseValue=1,Owner=sand});
            if(fault=="missing-physics")sand.RemovePart(sand.GetPart<PhysicsPart>());
            if(fault=="interior")Cell(20,7).IsInterior=true;
            if(fault=="takeable")sand.GetPart<PhysicsPart>().Takeable=true;
            if(fault=="visual-cue")sand.GetPart<RenderPart>().VisualID="authored-ground";
            var before=Snapshot();Assert.False(Apply());SameOwners(before);
            if(fault=="coating")Assert.True(zone.TileState.Has(20,7));
            if(fault=="reserved")Assert.True(zone.GenReservedCells.Contains((20,7)));
        }
        [Test] public void Adversarial_StemStopsAtFirstObstructionWithoutJumpingIt()
        {var blocker=Put("FireClay",23,8);var farther=Sand(24,8);Assert.True(Apply());Assert.AreEqual(7,Paving().Length);Assert.AreSame(Cell(23,8),zone.GetEntityCell(blocker));Assert.AreSame(Cell(24,8),zone.GetEntityCell(farther));}
        [TestCase("noop")][TestCase("scope")][TestCase("door")][TestCase("target")][TestCase("reserve")][TestCase("floor-label")][TestCase("floor-reader")][TestCase("duplicate-id")][TestCase("placed-floor")][TestCase("throw")]
        public void Adversarial_StagingRechecksCurrentAuthorityAndDeclinesBrokenContent(string fault)
        {
            factory.RegisterPartType<CreationProbe>("Render");var before=Snapshot();bool first=true;var foreign=new Zone("foreign");
            var oldDetailed=Diag.DetailedCaptureEnabled;var oldChannel=Diag.IsChannelEnabled("worldgen");
            var oldTraces=new HashSet<string>(Diag.Snapshot(512).Select(e=>e.TraceId));
            if(fault=="throw"){Diag.DetailedCaptureEnabled=true;Diag.SetChannel("worldgen",true);}
            CreationProbe.Callback=e=>
            {
                if(fault=="duplicate-id")e.ID="same-staged-id";
                if(fault=="placed-floor")foreign.AddEntity(e,2,2);
                if(fault=="floor-label")e.GetPart<RenderPart>().DisplayName="other floor";
                if(fault=="floor-reader")e.GetPart<ExaminablePart>().Text="other meaning";
                if(first)
                {
                    first=false;
                    if(fault=="scope")manager.WorldMap.GetPOI(15,15).Name="changed";
                    if(fault=="door"){zone.RemoveEntity(door);door=Put("VillageDoor",18,8);}
                    if(fault=="target"){zone.RemoveEntity(Sand(20,7));Put("Sand",20,7);}
                    if(fault=="reserve")zone.GenReservedCells.Add((20,7));
                    before=Snapshot();
                }
                if(fault=="throw")throw new InvalidOperationException("optional paving fixture");
            };
            try
            {
                Assert.AreEqual(fault=="noop",Apply());
                if(fault=="noop")Assert.AreEqual(9,Paving().Length);else SameOwners(before);
                if(fault=="throw")Assert.True(Diag.Snapshot(512).Any(e=>!oldTraces.Contains(e.TraceId)&&e.Category=="worldgen"&&e.Kind=="SaltVaultApproachRefused"&&e.PayloadJson.Contains("floor_creation_failed")));
            }
            finally{CreationProbe.Callback=null;Diag.DetailedCaptureEnabled=oldDetailed;Diag.SetChannel("worldgen",oldChannel);}
        }
    }
}
