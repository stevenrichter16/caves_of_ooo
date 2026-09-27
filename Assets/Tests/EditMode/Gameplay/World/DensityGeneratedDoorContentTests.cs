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
using Random=System.Random;
namespace CavesOfOoo.Tests
{
    public sealed class DensityGeneratedDoorContentTests
    {
        HotbarSaveFixture isolation;EntityFactory factory;
        [SetUp]public void Setup(){isolation=new HotbarSaveFixture(false,false);factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));}
        [TearDown]public void Cleanup(){isolation.Dispose();}
        static Type PlacementType=>typeof(VillageBuilder).Assembly.GetType("CavesOfOoo.Core.VillageDoorPlacementBuilder");
        IZoneBuilder Placement(VillageBuilder source){Assert.NotNull(PlacementType,"late placement must use actual ordinary room aperture provenance");return (IZoneBuilder)Activator.CreateInstance(PlacementType,new object[]{source});}
        VillageBuilder Village(Zone zone,int seed=64){var source=new VillageBuilder(BiomeType.Spread,new PointOfInterest(POIType.Village,"test village"));Assert.True(source.BuildZone(zone,factory,new Random(seed)));return source;}
        static Entity[] Doors(Zone zone)=>zone.GetAllEntities().Where(e=>e.BlueprintName=="VillageDoor").ToArray();
        static string Topology(Zone z)=>string.Join("",Enumerable.Range(0,Zone.Height).SelectMany(y=>Enumerable.Range(0,Zone.Width).Select(x=>z.GetCell(x,y).IsPassable()?".":"#")));
        [Test]public void ActualDoorBlueprintIsAnInitiallyOpenNonportableWorldOwner()
        {
            Assert.True(factory.Blueprints.ContainsKey("VillageDoor"));var door=factory.CreateEntity("VillageDoor");Assert.True(door.GetPart<DoorPart>().IsOpen);Assert.False(door.GetPart<PhysicsPart>().Solid);Assert.False(door.GetPart<PhysicsPart>().Takeable);Assert.True(door.HasTag("Furniture"));Assert.False(door.HasTag("Item"));Assert.IsNotEmpty(door.GetPart<ExaminablePart>().Description);Assert.False(door.HasPart<LockPart>());Assert.IsEmpty(factory.ValidateAsciiWorldBlueprint("VillageDoor"));
        }
        [TestCase(1)][TestCase(64)][TestCase(1729)][TestCase(2026)][TestCase(729490642)]
        public void ActualRoomSourcesGainDoorsWithoutChangingInitialTopologyOrRng(int seed)
        {
            var zone=new Zone("ordinary-village");var source=Village(zone,seed);string before=Topology(zone);var rng=new Random(seed);var control=new Random(seed);Assert.True(Placement(source).BuildZone(zone,factory,rng));
            Assert.IsNotEmpty(Doors(zone));Assert.AreEqual(before,Topology(zone));Assert.AreEqual(control.Next(),rng.Next(),"placement must not perturb downstream content rolls");
            foreach(var door in Doors(zone)){Assert.True(door.GetPart<DoorPart>().IsOpen);var c=zone.GetEntityCell(door);Assert.True(zone.GenReservedCells.Contains((c.X,c.Y)));}
            int count=Doors(zone).Length;Assert.True(Placement(source).BuildZone(zone,factory,rng));Assert.AreEqual(count,Doors(zone).Length,"replaying late placement does not clone owners");
        }
        [TestCase("Overworld.10.10.0")][TestCase("Overworld.5.9.0")][TestCase("Overworld.15.15.0")][TestCase("Overworld.16.1.0")]
        public void FullGenericVillagePipelineProvidesRealDoorsAndCapableResidents(string id)
        {
            var manager=OverworldZoneManager.CreateDetached(factory,64);var zone=manager.GetZone(id);Assert.IsNotEmpty(Doors(zone),"the real routed pipeline must place doors: "+id);
            var residents=zone.GetAllEntities().Where(e=>e.BlueprintName=="Villager").ToArray();Assert.IsNotEmpty(residents);Assert.True(residents.All(e=>e.HasTag("CanOpenDoors")));
            foreach(var door in Doors(zone)){var cell=zone.GetEntityCell(door);Assert.False(cell.Occupants.Any(e=>e.HasTag("Creature")));}
        }
        [TestCase("Overworld.14.9.0")][TestCase("Overworld.8.16.0")]
        public void AuthoredCompositionPipelinesDoNotGainGenericDoors(string id)
        {var manager=OverworldZoneManager.CreateDetached(factory,64);Assert.IsEmpty(Doors(manager.GetZone(id)));}
        [TestCase("missing-blueprint")][TestCase("foreign-zone")][TestCase("reserved")][TestCase("all-walls-cleared")]
        public void LatePlacementCannotInventOrClaimAnInvalidAperture(string fault)
        {
            var zone=new Zone("source");var source=Village(zone);
            if(fault=="missing-blueprint")factory.Blueprints.Remove("VillageDoor");
            if(fault=="foreign-zone")zone=new Zone("other");
            if(fault=="reserved")zone.ForEachCell((c,x,y)=>zone.GenReservedCells.Add((x,y)));
            if(fault=="all-walls-cleared")foreach(var e in zone.GetAllEntities().Where(e=>e.HasTag("Wall")).ToArray())zone.RemoveEntity(e);
            Assert.True(Placement(source).BuildZone(zone,factory,new Random(9)));Assert.IsEmpty(Doors(zone));
        }
        [TestCase(false)][TestCase(true)]
        public void DoorStateAndOwnershipRoundTripAsOrdinaryPartState(bool open)
        {
            Assert.True(factory.Blueprints.ContainsKey("VillageDoor"));var door=factory.CreateEntity("VillageDoor");door.GetPart<DoorPart>().IsOpen=open;door.GetPart<DoorPart>().OwnerId="actual-owner";
            Entity loaded;using(var bytes=new MemoryStream()){var w=new SaveWriter(bytes);w.WriteEntityReference(door);w.WriteQueuedEntityBodies();bytes.Position=0;var r=new SaveReader(bytes,factory);loaded=r.ReadEntityReference();r.ReadEntityBodies();}
            Assert.AreNotSame(loaded,door);Assert.AreEqual(door.ID,loaded.ID);Assert.AreEqual(open,loaded.GetPart<DoorPart>().IsOpen);Assert.AreEqual("actual-owner",loaded.GetPart<DoorPart>().OwnerId);Assert.AreEqual(open?"/":"+",loaded.GetPart<RenderPart>().RenderString);
        }
        public sealed class FactoryDoorProbe : PhysicsPart
        {
            public static Action<Entity> Callback;
            public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated"&&ParentEntity.BlueprintName=="VillageDoor")Callback?.Invoke(ParentEntity);return base.HandleEvent(e);}
        }
        [TestCase("foreign-owner")][TestCase("reserved")][TestCase("removed-wall")][TestCase("source-rebuilt")][TestCase("owned-latch")][TestCase("takeable")][TestCase("missing-part")]
        public void FactoryCallbacksCannotInvalidateOrStealSourcePlacement(string fault)
        {
            var zone=new Zone("callback-source");var source=Village(zone);var foreign=new Zone("foreign");var created=new List<Entity>();
            factory.RegisterPartType<FactoryDoorProbe>("Physics");
            FactoryDoorProbe.Callback=owner=>
            {
                created.Add(owner);
                if(fault=="foreign-owner")foreign.AddEntity(owner,4,4);
                if(fault=="reserved")zone.ForEachCell((c,x,y)=>zone.GenReservedCells.Add((x,y)));
                if(fault=="removed-wall")foreach(var e in zone.GetAllEntities().Where(e=>e.HasTag("Wall")).ToArray())zone.RemoveEntity(e);
                if(fault=="source-rebuilt")source.BuildZone(foreign,factory,new Random(7));
                if(fault=="owned-latch")owner.GetPart<DoorPart>().OwnerId="foreign-person";
                if(fault=="takeable")owner.GetPart<PhysicsPart>().Takeable=true;
                if(fault=="missing-part")owner.RemovePart(owner.GetPart<DoorPart>());
            };
            try{Assert.DoesNotThrow(()=>Placement(source).BuildZone(zone,factory,new Random(9)));Assert.IsNotEmpty(created);Assert.IsEmpty(Doors(zone));if(fault=="foreign-owner")Assert.True(created.All(e=>foreign.GetEntityCell(e)!=null));}
            finally{FactoryDoorProbe.Callback=null;}
        }

        [Test]public void RecordedApertureOrientationSurvivesSaveAndMatchesGeneratedTravelAxis()
        {
            var field=typeof(DoorPart).GetField("QuarterTurns");Assert.NotNull(field,"native door art must use saved aperture orientation");
            var seen=new HashSet<int>();
            foreach(int seed in new[]{1,64,1729,2026,729490642})
            {
                var zone=new Zone("orientation-"+seed);var source=Village(zone,seed);Placement(source).BuildZone(zone,factory,new Random(seed));
                foreach(var owner in Doors(zone))
                {
                    var cell=zone.GetEntityCell(owner);int quarter=(int)field.GetValue(owner.GetPart<DoorPart>());seen.Add(quarter);
                    Assert.That(quarter,Is.InRange(0,1));
                    Assert.True(quarter==0?zone.GetCell(cell.X-1,cell.Y).IsWall()&&zone.GetCell(cell.X+1,cell.Y).IsWall():zone.GetCell(cell.X,cell.Y-1).IsWall()&&zone.GetCell(cell.X,cell.Y+1).IsWall());
                    using(var bytes=new MemoryStream()){var w=new SaveWriter(bytes);w.WriteEntityReference(owner);w.WriteQueuedEntityBodies();bytes.Position=0;var r=new SaveReader(bytes,factory);var loaded=r.ReadEntityReference();r.ReadEntityBodies();Assert.AreEqual(quarter,field.GetValue(loaded.GetPart<DoorPart>()));}
                }
            }
            CollectionAssert.AreEquivalent(new[]{0,1},seen);
        }

        [TestCase("Grass",true)][TestCase("Bush",false)][TestCase("StairsUp",false)]
        public void ActualTerrainTagDoesNotMakeSceneryOrStairsBareGroundForClosure(string blueprint,bool bareGround)
        {
            var zone=new Zone("ground-shape");var actor=factory.CreateEntity("Player");var door=factory.CreateEntity("VillageDoor");
            Assert.True(zone.AddEntity(actor,10,10));Assert.True(zone.AddEntity(door,11,10));Assert.True(zone.AddEntity(factory.CreateEntity(blueprint),11,10));
            Assert.AreEqual(bareGround,door.GetPart<DoorPart>().TrySetOpen(actor,zone,false));Assert.AreEqual(!bareGround,door.GetPart<DoorPart>().IsOpen);
        }
        [TestCase("Grass",true)][TestCase("Bush",false)][TestCase("StairsUp",false)]
        public void ActualTerrainTagDoesNotMakeSceneryOrStairsAnEmptyAperture(string blueprint,bool bareGround)
        {
            var zone=new Zone("aperture-shape");var source=Village(zone);
            var apertures=typeof(VillageBuilder).GetProperty("DoorApertures",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.NotNull(apertures,"inspect existing aperture provenance without widening the runtime API");
            var aperture=((IEnumerable)apertures.GetValue(source)).Cast<object>().First();
            var apertureType=aperture.GetType();
            int x=(int)apertureType.GetField("X",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(aperture);
            int y=(int)apertureType.GetField("Y",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(aperture);
            Assert.True(zone.AddEntity(factory.CreateEntity(blueprint),x,y));
            Assert.True(Placement(source).BuildZone(zone,factory,new Random(9)));
            Assert.AreEqual(bareGround,zone.GetCell(x,y).Objects.Any(e=>e.BlueprintName=="VillageDoor"));
        }

    }
}
