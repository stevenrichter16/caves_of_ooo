using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SumpholdFurnishingTests
    {
        const string Id = "Overworld.15.6.0";
        EntityFactory factory;
        OverworldZoneManager manager;
        SumpholdCompositionBuilder terrain;
        Zone zone;
        static readonly (int x,int y,string bp)[] Expected = {
            (12,4,"Chair"),(20,6,"Bed"),(65,6,"Chair"),(19,19,"Crate"),(65,19,"Crate") };
        [SetUp] public void Setup()
        {
            factory=GrovelandsCompositionTests.Factory(); manager=new OverworldZoneManager(factory,65);
            terrain=new SumpholdCompositionBuilder(65); zone=new Zone(Id);
            Assert.IsTrue(terrain.BuildZone(zone,factory,new Random(7)));
            Assert.IsTrue(new SumpholdProfileBuilder(terrain).BuildZone(zone,factory,new Random(8)));
        }
        IZoneBuilder Detail(OverworldZoneManager m=null, SumpholdCompositionBuilder b=null)
        {
            var t=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SumpholdFurnishingBuilder");
            Assert.IsNotNull(t,"Fresh Sumphold rooms/work fingers lack the bounded furnishing finalizer.");
            return (IZoneBuilder)Activator.CreateInstance(t,m??manager,b??terrain);
        }
        static string State(Zone z) => string.Join("\n",z.GetAllEntities().OrderBy(e=>e.ID).Select(e=>
            e.ID+"|"+e.BlueprintName+"|"+z.GetEntityPosition(e)+"|"+string.Join(";",e.Properties.OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value))
            +"|"+(e.GetPart<ChairPart>()?.Owner??"")+"|"+(e.GetPart<BedPart>()?.Owner??"")
            +"|"+string.Join(",",e.GetPart<ContainerPart>()?.Contents.Select(x=>x.ID)??Enumerable.Empty<string>())))
            +"\n"+z.TileState.ToSaveString()+"\n"+string.Join(";",z.GenReservedCells.OrderBy(p=>p.Item1).ThenBy(p=>p.Item2));
        void Unchanged(Action action)
        {var before=State(zone);action();Assert.AreEqual(before,State(zone));}
        [Test] public void AddsFiveUsefulOwnersWithoutChangingOriginalsReservationsOrRng()
        {
            var old=zone.GetAllEntities().ToArray(); var snapshot=State(zone);var reserved=zone.GenReservedCells.ToArray();
            var rng=new Random(123); var detail=Detail();Assert.AreEqual(4600,detail.Priority);
            Assert.IsTrue(detail.BuildZone(zone,factory,rng));Assert.AreEqual(new Random(123).Next(),rng.Next());
            var added=zone.GetAllEntities().Except(old).ToArray();Assert.AreEqual(5,added.Length);
            foreach(var p in Expected)Assert.AreEqual(1,added.Count(e=>e.BlueprintName==p.bp&&zone.GetEntityPosition(e)==(p.x,p.y)));
            foreach(var e in added)
            {
                Assert.IsTrue(e.HasPart<ExaminablePart>());Assert.IsFalse(e.HasPart<BrainPart>());
                if(e.BlueprintName=="Crate") {var c=e.GetPart<ContainerPart>();Assert.IsEmpty(c.Contents);Assert.IsFalse(c.IsLocked);Assert.AreEqual(8,c.MaxItems);}
            }
            CollectionAssert.AreEquivalent(reserved,zone.GenReservedCells);
            Assert.IsTrue(detail.BuildZone(zone,factory,rng));Assert.AreEqual(old.Length+5,zone.EntityCount);
            foreach(var e in added)zone.RemoveEntity(e);Assert.AreEqual(snapshot,State(zone));
        }
        [TestCase("zone")] [TestCase("graph")] [TestCase("profile")] [TestCase("biome")]
        [TestCase("factory")] [TestCase("unprofiled")] [TestCase("seed")] [TestCase("cached")]
        public void WrongAuthorityIsSuccessfulNoOp(string kind)
        {
            var b=Detail();var supplied=factory;var target=zone;
            if(kind=="zone")target=new Zone("Overworld.15.6.1");
            if(kind=="graph")target=new Zone(Id);
            if(kind=="profile")manager.WorldMap.GetPOI(15,6).Profile="Intake";
            if(kind=="biome")manager.WorldMap.Tiles[15,6]=BiomeType.Sodden;
            if(kind=="factory")supplied=GrovelandsCompositionTests.Factory();
            if(kind=="unprofiled") {terrain=new SumpholdCompositionBuilder(65);zone=new Zone(Id);Assert.IsTrue(terrain.BuildZone(zone,factory,new Random(4)));target=zone;b=Detail();}
            if(kind=="seed")b=Detail(new OverworldZoneManager(factory,64));
            if(kind=="cached")manager.SetActiveZone(zone);
            var before=State(zone);Assert.IsTrue(b.BuildZone(target,supplied,new Random(2)));Assert.AreEqual(before,State(zone));
        }
        [TestCase("reserved")] [TestCase("wet")] [TestCase("occupied")] [TestCase("ground")]
        [TestCase("interior")] [TestCase("footprint")]
        public void RefusesWholeRestPairWhenEitherDestinationIsUnavailable(string reason)
        {
            if(reason=="reserved")zone.GenReservedCells.Add((12,4));
            if(reason=="wet")zone.TileState.WriteCoating(12,4,"water",ZoneTileState.Permanent);
            if(reason=="occupied")zone.AddEntity(factory.CreateEntity("Chair"),12,4);
            if(reason=="ground")zone.GetCell(12,4).Objects[0].GetPart<PhysicsPart>().Solid=true;
            if(reason=="interior")zone.GetCell(12,4).IsInterior=false;
            if(reason=="footprint") {var e=factory.CreateEntity("Crate");e.AddPart(new SpatialFootprintPart {CellsRaw="0,0;1,0"});Assert.IsTrue(zone.AddEntity(e,11,4));}
            var old=zone.GetAllEntities().ToArray();Assert.IsTrue(Detail().BuildZone(zone,factory,new Random(1)));
            Assert.IsFalse(zone.GetAllEntities().Except(old).Any(e=>e.BlueprintName=="Bed"||zone.GetEntityPosition(e)==(12,4)));
            Assert.IsTrue(zone.GetAllEntities().Except(old).Any(e=>zone.GetEntityPosition(e)==(65,19)),"Independent valid worksite should remain eligible.");
        }
        [TestCase("anchor")] [TestCase("reserved")] [TestCase("wet")] [TestCase("occupied")] [TestCase("access")]
        public void RefusesUnavailableWestCargoWithoutRemovingOtherGroups(string reason)
        {
            if(reason=="anchor")zone.RemoveEntity(zone.GetCell(17,19).Objects.Single(e=>e.BlueprintName=="BoatFrame"));
            if(reason=="reserved")zone.GenReservedCells.Add((19,19));
            if(reason=="wet")zone.AddEntity(factory.CreateEntity("WaterPuddle"),19,19);
            if(reason=="occupied")zone.AddEntity(factory.CreateEntity("SariSnake"),19,19);
            if(reason=="access")foreach(var d in new[]{(-1,0),(1,0),(0,-1),(0,1)})zone.AddEntity(factory.CreateEntity("SandstoneWall"),19+d.Item1,19+d.Item2);
            var old=zone.GetAllEntities().ToArray();Assert.IsTrue(Detail().BuildZone(zone,factory,new Random(1)));
            Assert.IsFalse(zone.GetAllEntities().Except(old).Any(e=>zone.GetEntityPosition(e)==(19,19)));
            Assert.IsTrue(zone.GetAllEntities().Except(old).Any(e=>zone.GetEntityPosition(e)==(65,19)));
        }
        [TestCase("missing")] [TestCase("prefilled")] [TestCase("locked")] [TestCase("capacity")]
        [TestCase("Barren")] [TestCase("footprint")] [TestCase("owned")] [TestCase("hidden")]
        [TestCase("wreckage")] [TestCase("hardness")] [TestCase("thermal")] [TestCase("campfire")]
        [TestCase("variant")] [TestCase("glyph")] [TestCase("layer")]
        public void RejectsMalformedPayloadWithoutDamagingOriginals(string reason)
        {
            var before=zone.GetAllEntities().ToArray();var snapshot=State(zone);
            if(reason=="missing")factory.Blueprints.Remove("Crate");
            else
            {
                factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
                foreach(var bp in new[]{"Crate","Chair","Bed"})factory.Blueprints[bp].Parts["ReceiptCreated"]=new Dictionary<string,string>();
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=> {
                    if(reason=="prefilled"&&e.HasPart<ContainerPart>())e.GetPart<ContainerPart>().Contents.Add(new Entity{ID="existing-cargo",BlueprintName="cargo"});
                    if(reason=="locked"&&e.HasPart<ContainerPart>())e.GetPart<ContainerPart>().Locked=true;
                    if(reason=="capacity"&&e.HasPart<ContainerPart>())e.GetPart<ContainerPart>().MaxItems=9;
                    if(reason=="Barren")e.Tags["Barren"]="true";
                    if(reason=="footprint")e.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});
                    if(reason=="owned") {if(e.HasPart<BedPart>())e.GetPart<BedPart>().Owner="existing-owner";if(e.HasPart<ChairPart>())e.GetPart<ChairPart>().Owner="existing-owner";}
                    if(reason=="hidden")e.GetPart<RenderPart>().Visible=false;
                    if(reason=="wreckage"&&e.HasPart<DestructiblePart>())e.GetPart<DestructiblePart>().WreckageBlueprint="HealingTonic";
                    if(reason=="hardness"&&e.HasPart<DestructiblePart>())e.GetPart<DestructiblePart>().Hardness=1;
                    if(reason=="thermal")e.AddPart(new ThermalPart());
                    if(reason=="campfire")e.AddPart(new CampfirePart());
                    if(reason=="variant")e.GetPart<RenderPart>().VisualVariant="foreign";
                    if(reason=="glyph")e.GetPart<RenderPart>().RenderString="?";
                    if(reason=="layer")e.GetPart<RenderPart>().RenderLayer=9;
                };
            }
            try
            {
                Assert.IsTrue(Detail().BuildZone(zone,factory,new Random(3)));
                var added=zone.GetAllEntities().Except(before).ToArray();
                if(new[]{"missing","prefilled","locked","capacity","wreckage","hardness"}.Contains(reason))Assert.IsFalse(added.Any(e=>e.BlueprintName=="Crate"));
                else if(reason=="owned")Assert.IsFalse(added.Any(e=>e.BlueprintName=="Bed"||e.BlueprintName=="Chair"));
                else Assert.IsEmpty(added);
                foreach(var e in added)zone.RemoveEntity(e);Assert.AreEqual(snapshot,State(zone));
            }
            finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}
        }
        [Test] public void PreservesRealStockAndDoesNotReplenishRemovedFurnitureOnCachedReturn()
        {
            var merchant=factory.CreateEntity("Merchant");Assert.IsTrue(zone.AddEntity(merchant,30,10));
            var item=factory.CreateEntity("HealingTonic");item.GetPart<StackerPart>().StackCount=7;
            Assert.IsTrue(merchant.GetPart<InventoryPart>().AddObject(item));
            var oldCrate=zone.GetCell(64,5).Objects.Single(e=>e.BlueprintName=="Crate");
            var cargo=factory.CreateEntity("HealingTonic");cargo.GetPart<StackerPart>().StackCount=3;Assert.IsTrue(oldCrate.GetPart<ContainerPart>().AddItem(cargo));
            var old=zone.GetAllEntities().ToArray();var before=State(zone);var detail=Detail();
            Assert.IsTrue(detail.BuildZone(zone,factory,new Random(9)));
            Assert.AreSame(item,merchant.GetPart<InventoryPart>().Objects.Single());Assert.AreEqual(7,item.GetPart<StackerPart>().StackCount);Assert.AreSame(merchant,item.GetPart<PhysicsPart>().InInventory);
            Assert.AreSame(cargo,oldCrate.GetPart<ContainerPart>().Contents.Single());Assert.AreEqual(3,cargo.GetPart<StackerPart>().StackCount);Assert.AreSame(oldCrate,cargo.GetPart<PhysicsPart>().InInventory);
            foreach(var e in zone.GetAllEntities().Except(old).ToArray())zone.RemoveEntity(e);
            Assert.AreEqual(before,State(zone));Assert.IsTrue(detail.BuildZone(zone,factory,new Random(9)));Assert.AreEqual(before,State(zone));
            manager.SetActiveZone(zone);Assert.IsTrue(Detail().BuildZone(zone,factory,new Random(9)));Assert.AreEqual(before,State(zone));
            Assert.AreSame(zone,manager.GetZone(Id));
        }
        [TestCase(1)] [TestCase(64)] [TestCase(65)] [TestCase(1729)] [TestCase(729490642)]
        public void FullPipelinePreservesDryExitsDoorsAndServiceFrontages(int seed)
        {
            var m=new OverworldZoneManager(factory,seed);var z=m.GetZone(Id);var p=SumpholdCompositionPlan.Create(Id,seed);
            var owners=z.GetAllEntities().ToArray();var fixtures=owners.Where(e=>new[]{"BoatFrame","TollRolls","Crate","Bed","Chair","Well"}.Contains(e.BlueprintName)).Select(e=>z.GetEntityPosition(e)).ToArray();
            Assert.That(owners.Count(e=>e.BlueprintName=="BoatFrame"),Is.EqualTo(2));
            foreach(var e in owners.Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")))z.RemoveEntity(e);
            var reach=SumpholdCompositionTests.DryReach(z,p);
            foreach(var r in p.Rooms)Assert.IsTrue(reach.Contains((r.DoorX,r.DoorY)));
            foreach(var c in fixtures)Assert.IsTrue(new[]{(-1,0),(1,0),(0,-1),(0,1)}.Any(d=>reach.Contains((c.x+d.Item1,c.y+d.Item2))));
            Assert.IsTrue(reach.Contains((0,12)));Assert.IsTrue(reach.Contains((79,12)));Assert.IsTrue(reach.Contains((40,0)));
            Assert.AreSame(z,m.GetZone(Id));Assert.AreEqual(z.EntityCount,m.GetZone(Id).EntityCount);
        }
    }
}
