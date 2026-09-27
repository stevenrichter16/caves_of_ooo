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
    public sealed class DensityGeneratedDoorNativeFollowupTests
    {
        const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        readonly List<(IDictionary dictionary,List<DictionaryEntry> entries)> saved=new List<(IDictionary,List<DictionaryEntry>)>();
        bool loaded;HotbarSaveFixture isolation;EntityFactory factory;
        [SetUp]public void Setup()
        {
            foreach(var type in new[]{typeof(HouseDramaLoader),typeof(HouseDramaRuntime)})
            {var d=(IDictionary)type.GetField("_dramas",Static).GetValue(null);var copy=new List<DictionaryEntry>();foreach(DictionaryEntry e in d)copy.Add(e);saved.Add((d,copy));d.Clear();}
            loaded=(bool)typeof(HouseDramaLoader).GetField("_loaded",Static).GetValue(null);typeof(HouseDramaLoader).GetField("_loaded",Static).SetValue(null,true);
            isolation=new HotbarSaveFixture(false,false);factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
        }
        [TearDown]public void Cleanup(){isolation?.Dispose();foreach(var pair in saved){pair.dictionary.Clear();foreach(var e in pair.entries)pair.dictionary.Add(e.Key,e.Value);}saved.Clear();typeof(HouseDramaLoader).GetField("_loaded",Static).SetValue(null,loaded);}
        void Register(string role)
        {var data=new HouseDramaData{ID="DoorContextProbe",NpcRoles=new List<NpcRoleData>{new NpcRoleData{Id="door-npc",Role=role,Alive=true}}};HouseDramaLoader.Register(data);HouseDramaRuntime.RegisterDrama(data);}
        [TestCase("RisingInheritor",true)][TestCase("RisingInheritor",false)]
        [TestCase("DiminishedHead",true)][TestCase("DiminishedHead",false)]
        [TestCase("NamedAntagonist",true)][TestCase("NamedAntagonist",false)]
        [TestCase("SilencedHelper",true)][TestCase("SilencedHelper",false)]
        public void LateDramaResidentsOnlyGainDoorPermissionInExplicitOrdinaryContext(string role,bool enabled)
        {
            Register(role);var zone=new Zone("drama-context");var builder=new HouseDramaZoneBuilder("DoorContextProbe");
            typeof(HouseDramaZoneBuilder).GetProperty("CanOpenOrdinaryDoors")?.SetValue(builder,enabled);
            Assert.True(builder.BuildZone(zone,factory,new System.Random(64)));var npc=zone.GetReadOnlyEntities().Single(e=>e.HasPart<HouseDramaPart>());
            Assert.AreEqual(enabled,npc.HasTag("CanOpenDoors"));Assert.AreEqual("door-npc",npc.GetPart<HouseDramaPart>().NpcId);
        }
        [TestCase("Overworld.10.10.0")][TestCase("Overworld.5.9.0")][TestCase("Overworld.15.15.0")][TestCase("Overworld.16.1.0")]
        public void ActualOrdinaryPipelineWiresLateDramaPermissionsAndReservations(string id)
        {
            Register("RisingInheritor");var manager=OverworldZoneManager.CreateDetached(factory,64);
            var pipeline=(ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(manager,new object[]{id});
            Assert.True(pipeline.Builders.Any(b=>b is VillageDoorPlacementBuilder));var drama=pipeline.Builders.OfType<HouseDramaZoneBuilder>().Single();
            Assert.True(drama.RespectReservations,"Late residents must keep doorway and approaches free.");
            Assert.AreEqual(true,typeof(HouseDramaZoneBuilder).GetProperty("CanOpenOrdinaryDoors")?.GetValue(drama));
            var zone=manager.GetZone(id);var residents=zone.GetReadOnlyEntities().Where(e=>e.HasPart<HouseDramaPart>()).ToArray();Assert.IsNotEmpty(residents);
            foreach(var resident in residents){Assert.True(resident.HasTag("CanOpenDoors"));var c=zone.GetEntityCell(resident);Assert.False(zone.GenReservedCells.Contains((c.X,c.Y)));}
        }
        [Test]public void StationaryDoorStateInvalidatesExistingLightWithoutInventingEntityMovement()
        {
            var zone=new Zone("light-door"){AmbientLevel=.05f};for(int x=0;x<Zone.Width;x++)if(x!=34)Assert.True(zone.AddEntity(factory.CreateEntity("StoneWall"),x,5));
            var actor=factory.CreateEntity("Player");var door=factory.CreateEntity("VillageDoor");Assert.True(zone.AddEntity(actor,34,6));Assert.True(zone.AddEntity(door,34,5));Assert.True(zone.AddEntity(factory.CreateEntity("Torch"),34,6));
            Assert.True(door.GetPart<DoorPart>().TrySetOpen(actor,zone,false));int version=zone.EntityVersion;var cached=new LightMap();cached.Compute(zone);float closed=cached.GetBrightness(34,4);
            Assert.True(door.GetPart<DoorPart>().TrySetOpen(actor,zone,true));cached.Compute(zone);var fresh=new LightMap();fresh.Compute(zone);float open=fresh.GetBrightness(34,4);Assert.Greater(open,closed+.1f);Assert.AreEqual(open,cached.GetBrightness(34,4),.0001f);
            Assert.True(door.GetPart<DoorPart>().TrySetOpen(actor,zone,false));cached.Compute(zone);Assert.AreEqual(closed,cached.GetBrightness(34,4),.0001f);Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual((34,6),zone.GetEntityPosition(actor));
        }
        [TestCase(true)][TestCase(false)]
        public void UnlockInvalidatesLightOnlyAccordingToTheSavedLatch(bool open)
        {
            var zone=new Zone("unlock-light"){AmbientLevel=.05f};for(int x=0;x<Zone.Width;x++)if(x!=34)Assert.True(zone.AddEntity(factory.CreateEntity("StoneWall"),x,5));
            var actor=factory.CreateEntity("Player");var door=factory.CreateEntity("VillageDoor");door.GetPart<DoorPart>().IsOpen=open;door.AddPart(new LockPart{IsLocked=true,KeyId=""});
            Assert.True(zone.AddEntity(actor,34,6));Assert.True(zone.AddEntity(door,34,5));Assert.True(zone.AddEntity(factory.CreateEntity("Torch"),34,6));
            var cached=new LightMap();cached.Compute(zone);float locked=cached.GetBrightness(34,4);int version=zone.EntityVersion;
            var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)actor);e.SetParameter("Zone",(object)zone);e.SetParameter("Command","Unlock");door.FireEventAndRelease(e);
            Assert.False(door.GetPart<LockPart>().IsLocked);Assert.AreEqual(open,door.GetPart<DoorPart>().IsOpen);cached.Compute(zone);var fresh=new LightMap();fresh.Compute(zone);
            if(open)Assert.Greater(fresh.GetBrightness(34,4),locked+.1f);else Assert.AreEqual(locked,fresh.GetBrightness(34,4),.0001f);
            Assert.AreEqual(fresh.GetBrightness(34,4),cached.GetBrightness(34,4),.0001f);Assert.AreEqual(version,zone.EntityVersion);
        }
    }
}
