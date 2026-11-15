using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class QuillholdCopyHallTests
    {
        const string Id="Overworld.14.9.0";
        [SetUp] public void Setup()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void Cleanup()=>LootTableRegistry.ResetForTests();

        [TestCase(13,19)] [TestCase(13,21)] [TestCase(24,21)]
        public void FreshCopyHallProvidesPairedSeatsAtDisclosedDesks(int x,int y)
        {
            var m=new OverworldZoneManager(QuillholdCompositionTests.Factory(),224401440);
            var z=m.GetZone(Id);var c=z.GetCell(x,y);
            Assert.IsTrue(c.IsInterior);Assert.IsFalse(z.GenReservedCells.Contains((x,y)));
            Assert.IsTrue(c.Objects.Any(e=>e.BlueprintName=="StoneFloor"));
            Assert.IsTrue(c.Objects.All(e=>e.BlueprintName=="StoneFloor"||e.BlueprintName=="Chair"),"Disclosed bare destination remains eligible.");
            Assert.IsTrue(z.GetCell(x,20).Objects.Any(e=>e.BlueprintName=="QuillholdCopyDesk"),"Real desk anchor.");
            Assert.That(c.Objects.Count(e=>e.BlueprintName=="Chair"),Is.EqualTo(1),"Missing ordinary seat at the copying bay.");
        }
        [Test] public void ExistingInnkeeperChairAndScribeApronRemainOwnedAndOpen()
        {
            var m=new OverworldZoneManager(QuillholdCompositionTests.Factory(),224401440);var z=m.GetZone(Id);
            var old=z.GetCell(24,19).Objects.Single(e=>e.BlueprintName=="Chair");
            Assert.AreEqual(z.GetAllEntities().Single(e=>e.BlueprintName=="Innkeeper").ID,old.GetPart<ChairPart>().Owner);
            Assert.AreEqual((13,18),z.GetEntityPosition(z.GetAllEntities().Single(e=>e.BlueprintName=="Scribe")));
            Assert.IsTrue(z.GenReservedCells.Contains((13,17)));Assert.IsFalse(z.GetCell(13,17).BlocksMovement());
            Assert.AreEqual(6,z.GetAllEntities().Count(e=>e.BlueprintName=="QuillholdArchiveShelf"));
        }

        static (OverworldZoneManager m,QuillholdCompositionBuilder b,Zone z,EntityFactory f) Fresh(int seed=224401440)
        {
            var f=QuillholdCompositionTests.Factory();var m=new OverworldZoneManager(f,seed);
            var b=new QuillholdCompositionBuilder(seed);var z=new Zone(Id);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(new QuillholdProfileBuilder(b).BuildZone(z,f,new Random(2)));
            return(m,b,z,f);
        }
        static string State(Zone z)=>string.Join("\n",z.GetAllEntities().OrderBy(e=>e.ID).Select(e=>e.ID+"|"+e.BlueprintName+"|"+z.GetEntityPosition(e)
            +"|"+e.GetPart<PhysicsPart>()?.Solid+"|"+e.GetPart<DestructiblePart>()?.HP+"|"+e.GetPart<ChairPart>()?.Owner
            +"|"+string.Join(",",e.GetPart<ContainerPart>()?.Contents.Select(i=>i.ID)??Enumerable.Empty<string>())))
            +"\n"+z.TileState.ToSaveString()+"\n"+string.Join(";",z.GenReservedCells.OrderBy(p=>p.Item1).ThenBy(p=>p.Item2));

        [TestCase(64)] [TestCase(2)] [TestCase(224401440)]
        public void AllThreeFormationsAddOnlyThreeOrdinarySeatsPreservingOwnersAndRoutes(int seed)
        {
            var s=Fresh(seed);var old=s.z.GetAllEntities().ToArray();string before=State(s.z);var rng=new Random(99);
            var reach=QuillholdCompositionTests.DryReach(s.z);var detail=new QuillholdCopyHallBuilder(s.m,s.b);
            Assert.AreEqual(4600,detail.Priority);Assert.IsTrue(detail.BuildZone(s.z,s.f,rng));Assert.AreEqual(new Random(99).Next(),rng.Next());
            var added=s.z.GetAllEntities().Except(old).ToArray();Assert.AreEqual(3,added.Length);
            foreach(var e in added){Assert.AreEqual("Chair",e.BlueprintName);Assert.IsFalse(e.GetPart<PhysicsPart>().Solid);Assert.IsEmpty(e.GetPart<ChairPart>().Owner);Assert.IsFalse(e.GetPart<ChairPart>().Occupied);}
            CollectionAssert.AreEquivalent(reach,QuillholdCompositionTests.DryReach(s.z));
            Assert.IsTrue(detail.BuildZone(s.z,s.f,rng));Assert.AreEqual(old.Length+3,s.z.EntityCount);
            foreach(var e in added)s.z.RemoveEntity(e);Assert.AreEqual(before,State(s.z));
            Assert.IsTrue(detail.BuildZone(s.z,s.f,rng));Assert.AreEqual(before,State(s.z),"Do not replenish removed furniture.");
        }
        [TestCase("zone")] [TestCase("graph")] [TestCase("profile")] [TestCase("biome")]
        [TestCase("factory")] [TestCase("seed")] [TestCase("cached")] [TestCase("unprofiled")]
        public void WrongOrCachedAuthorityIsUnchanged(string kind)
        {
            var s=Fresh();var supplied=s.f;var target=s.z;var manager=s.m;var terrain=s.b;
            if(kind=="zone")target=new Zone("Overworld.14.9.1");
            if(kind=="graph")target=new Zone(Id);
            if(kind=="profile")manager.WorldMap.GetPOI(14,9).Profile="Boatyard";
            if(kind=="biome")manager.WorldMap.Tiles[14,9]=BiomeType.Sodden;
            if(kind=="factory")supplied=QuillholdCompositionTests.Factory();
            if(kind=="seed")manager=new OverworldZoneManager(s.f,64);
            if(kind=="cached")manager.SetActiveZone(s.z);
            if(kind=="unprofiled"){terrain=new QuillholdCompositionBuilder(224401440);target=new Zone(Id);Assert.IsTrue(terrain.BuildZone(target,s.f,new Random(1)));}
            string before=State(s.z),other=State(target);
            Assert.IsTrue(new QuillholdCopyHallBuilder(manager,terrain).BuildZone(target,supplied,new Random(4)));
            Assert.AreEqual(before,State(s.z));Assert.AreEqual(other,State(target));
        }
        [TestCase("reserved")] [TestCase("wet")] [TestCase("occupied")] [TestCase("interior")]
        [TestCase("ground")] [TestCase("anchor")] [TestCase("footprint")]
        public void UnavailableDestinationOrDeskRefusesTheWholeGroup(string reason)
        {
            var s=Fresh();var c=s.z.GetCell(13,19);
            if(reason=="reserved")s.z.GenReservedCells.Add((13,19));
            if(reason=="wet")s.z.TileState.WriteCoating(13,19,"water",ZoneTileState.Permanent);
            if(reason=="occupied")s.z.AddEntity(s.f.CreateEntity("Chair"),13,19);
            if(reason=="interior")c.IsInterior=false;
            if(reason=="ground")c.Objects[0].GetPart<PhysicsPart>().Solid=true;
            if(reason=="anchor")s.z.RemoveEntity(s.z.GetCell(13,20).Objects.Single(e=>e.BlueprintName=="QuillholdCopyDesk"));
            if(reason=="footprint"){var e=s.f.CreateEntity("Crate");e.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});Assert.IsTrue(s.z.AddEntity(e,12,19));}
            string before=State(s.z);Assert.IsTrue(new QuillholdCopyHallBuilder(s.m,s.b).BuildZone(s.z,s.f,new Random(4)));Assert.AreEqual(before,State(s.z));
        }
        [TestCase("missing")] [TestCase("owned")] [TestCase("occupied")] [TestCase("solid")]
        [TestCase("hidden")] [TestCase("glyph")] [TestCase("Barren")] [TestCase("footprint")]
        [TestCase("layer")] [TestCase("variant")] [TestCase("campfire")]
        [TestCase("duplicate-chair")] [TestCase("duplicate-render")]
        [TestCase("duplicate-physics")] [TestCase("duplicate-examinable")]
        [TestCase("duplicate-status")] [TestCase("duplicate-stack")] [TestCase("empty-stack")]
        public void MalformedSeatCannotMutateTheExistingRoom(string kind)
        {
            var s=Fresh();string before=State(s.z);
            if(kind=="missing")s.f.Blueprints.Remove("Chair");
            else
            {
                s.f.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
                s.f.Blueprints["Chair"].Parts["ReceiptCreated"]=new Dictionary<string,string>();
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=>{
                    if(kind=="owned")e.GetPart<ChairPart>().Owner="existing-resident";
                    if(kind=="occupied")e.GetPart<ChairPart>().Occupied=true;
                    if(kind=="solid")e.GetPart<PhysicsPart>().Solid=true;
                    if(kind=="hidden")e.GetPart<RenderPart>().Visible=false;
                    if(kind=="glyph")e.GetPart<RenderPart>().RenderString="?";
                    if(kind=="Barren")e.Tags["Barren"]="true";
                    if(kind=="footprint")e.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});
                    if(kind=="layer")e.GetPart<RenderPart>().RenderLayer=9;
                    if(kind=="variant")e.GetPart<RenderPart>().VisualVariant="foreign";
                    if(kind=="campfire")e.AddPart(new CampfirePart());
                    if(kind=="duplicate-chair")e.AddPart(new ChairPart());
                    if(kind=="duplicate-render")e.AddPart(new RenderPart());
                    if(kind=="duplicate-physics")e.AddPart(new PhysicsPart());
                    if(kind=="duplicate-examinable")e.AddPart(new ExaminablePart());
                    if(kind=="duplicate-status"){e.AddPart(new StatusEffectsPart());e.AddPart(new StatusEffectsPart());}
                    if(kind=="duplicate-stack"){e.AddPart(new StackerPart());e.AddPart(new StackerPart());}
                    if(kind=="empty-stack")e.AddPart(new StackerPart{StackCount=0});
                };
            }
            try{Assert.IsTrue(new QuillholdCopyHallBuilder(s.m,s.b).BuildZone(s.z,s.f,new Random(2)));Assert.AreEqual(before,State(s.z));}
            finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}
        }
        [Test] public void CacheReturnDoesNotReplenishMissingSeatsOrModifyStock()
        {
            var s=Fresh();var detail=new QuillholdCopyHallBuilder(s.m,s.b);var old=s.z.GetAllEntities().ToArray();
            Assert.IsTrue(detail.BuildZone(s.z,s.f,new Random(2)));
            foreach(var e in s.z.GetAllEntities().Except(old).ToArray())s.z.RemoveEntity(e);
            s.m.SetActiveZone(s.z);string before=State(s.z);
            Assert.IsTrue(new QuillholdCopyHallBuilder(s.m,s.b).BuildZone(s.z,s.f,new Random(2)));
            Assert.AreSame(s.z,s.m.GetZone(Id));Assert.AreEqual(before,State(s.z));
        }
    }
}
