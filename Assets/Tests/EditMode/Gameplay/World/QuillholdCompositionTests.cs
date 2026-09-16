using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class QuillholdCompositionTests
    {
        public const string Id="Overworld.14.9.0";
        public static EntityFactory Factory()=>GrovelandsCompositionTests.Factory();
        [SetUp] public void LoadLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetLoot()=>LootTableRegistry.ResetForTests();
        [Test] public void NativeAddressAndAuthoredTierRemainExactWhileSeedChangesSemanticRoles()
        {
            Assert.AreEqual(Id,QuillholdCompositionPlan.ZoneID);Assert.AreEqual("PrimaryArchive",QuillholdCompositionPlan.ProfileID);
            Assert.AreEqual(BiomeType.Spread,WorldMapAuthoring.BiomeAt(14,9));Assert.AreEqual(1,WorldMapAuthoring.TierAt(14,9));Assert.IsTrue(WorldMapAuthoring.IsRoad(14,9));Assert.IsFalse(WorldMapAuthoring.IsRiver(14,9));
            var names=new HashSet<string>();var relations=new HashSet<string>();
            foreach(int seed in Enumerable.Range(0,64))
            {
                var p=QuillholdCompositionPlan.Create(Id,seed);Assert.AreEqual(p.Signature(),QuillholdCompositionPlan.Create(Id,seed).Signature());names.Add(p.FormationName);
                var copy=p.Rooms.Single(r=>r.Role=="CopyHall");var stacks=p.Rooms.Single(r=>r.Role=="Stacks");relations.Add((copy.Y<12)+":"+(stacks.Y<12)+":"+(stacks.Height>12));
            }
            Assert.AreEqual(3,names.Count);Assert.AreEqual(3,relations.Count);Assert.AreNotEqual(QuillholdCompositionPlan.Create(Id,64).Signature(),QuillholdCompositionPlan.Create(Id,1729).Signature());
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.14.9.1")] [TestCase("Overworld.14.9.-1")] [TestCase("Overworld.014.9.0")]
        [TestCase("Overworld.14.09.0")] [TestCase(" Overworld.14.9.0")] [TestCase("Overworld.14.9.0 ")] [TestCase("overworld.14.9.0")]
        [TestCase("Overworld.10.14.0")] [TestCase("Overworld.999999999999.9.0")] [TestCase("Overworld.14.9")]
        public void AddressParsingCannotExpandTheArchive(string id)
        {Assert.IsFalse(QuillholdCompositionPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>QuillholdCompositionPlan.Create(id,64));}
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void PublicArchiveHasAccessibleFunctionalInteriorsAndRealStoredBooks(int seed)
        {
            var f=Factory();var z=new Zone(Id);var b=new QuillholdCompositionBuilder(seed);Assert.AreEqual(1000,b.Priority);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var p=b.Plan;
            CollectionAssert.AreEquivalent(new[]{"CopyHall","Stacks","Refectory","Receiving"},p.Rooms.Select(r=>r.Role));Assert.GreaterOrEqual(p.Rooms.Select(r=>r.Width*r.Height).Distinct().Count(),3);
            Assert.AreEqual(0,p.Profile.Count(o=>o.Blueprint=="Scribe"));Assert.AreEqual(6,p.Profile.Count(o=>o.Blueprint=="QuillholdArchiveShelf"));Assert.AreEqual(6,p.Profile.Count);
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="Scribe"||e.BlueprintName=="QuillholdArchiveShelf"));
            var late=new QuillholdProfileBuilder(b);Assert.AreEqual(3860,late.Priority);Assert.IsTrue(late.BuildZone(z,f,new Random(1)));var reach=DryReach(z);DrownedLedgerCompositionTests.AssertFourDirections(reach);
            foreach(var p1 in p.Profile)Assert.IsTrue(CinderholdCompositionTests.Neighbors(p1.X,p1.Y).Any(n=>reach.Contains(n)),p1.Blueprint+" lacks reachable standing access");
            foreach(var shelf in z.GetAllEntities().Where(e=>e.BlueprintName=="QuillholdArchiveShelf"))
            {Assert.NotNull(shelf.GetPart<ContainerPart>());Assert.IsTrue(shelf.GetPart<ContainerPart>().Contents.Count>0);Assert.IsFalse(shelf.GetPart<DestructiblePart>().Indestructible);Assert.IsFalse(shelf.HasPart<SealedLibraryBarrierPart>());}
            Assert.IsTrue(b.TryGetServiceCell("Scribe",out int sx,out int sy));Assert.IsTrue(p.IsInterior(sx,sy));Assert.IsFalse(p.IsReserved(sx,sy));Assert.IsFalse(b.TryGetServiceCell("FilerClerk",out _,out _));
            Assert.IsNull(p.ObjectAt(40,12));Assert.IsTrue(p.IsReserved(40,12));Assert.IsFalse(p.IsInterior(40,12));
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void RealManagerRetainsVillageServicesAndStockWithoutInventingARiver(int seed)
        {
            var m=new OverworldZoneManager(Factory(),seed);var pipe=CinderholdCompositionTests.Pipeline(m,Id);Assert.IsTrue(pipe.Builders.OfType<QuillholdCompositionBuilder>().Any());Assert.IsFalse(pipe.Builders.OfType<VillageBuilder>().Any());Assert.IsFalse(pipe.Builders.OfType<RiverChunkBuilder>().Any());
            var z=m.GetZone(Id);Assert.AreSame(z,m.GetZone(Id));var basis=QuillholdCompositionPlan.Create(Id,seed);Assert.IsTrue(basis.TryGetServiceCell("Scribe",out int sx,out int sy));Assert.AreEqual((sx,sy),z.GetEntityPosition(z.GetAllEntities().Single(e=>e.BlueprintName=="Scribe")));Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="Well"));Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="Scribe"));
            foreach(string bp in new[]{"Merchant","Quartermaster","Innkeeper","Elder"})Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName==bp),bp);
            Assert.AreEqual(6,z.GetAllEntities().Count(e=>e.BlueprintName=="QuillholdArchiveShelf"));Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));DrownedLedgerCompositionTests.AssertFourDirections(DryReach(z));
            m=new OverworldZoneManager(Factory(),seed);m.WorldMap.SetPOI(14,9,new PointOfInterest(POIType.Village,"Quillhold",tier:1));Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<RiverChunkBuilder>().Any());
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualPlacedScribeCopiesAnOriginalButCannotCopyAnEmptyCopy(bool copyOnly)
        {
            var old=ConversationActions.Factory;try
            {
                ConversationActions.Reset();ConversationPredicates.Reset();ConversationLoader.Reset();ConversationManager.EndConversation();
                ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Conversations/FriendlyNPCs.json")));
                var f=Factory();ConversationActions.Factory=f;var z=new OverworldZoneManager(f,64).GetZone(Id);var s=z.GetAllEntities().First(e=>e.BlueprintName=="Scribe");var player=CinderholdCompositionTests.Player();var inv=player.GetPart<InventoryPart>();var original=f.CreateEntity(copyOnly?"GrimoireCopy":"KindleGrimoire");Assert.IsTrue(inv.AddObject(original));int drams=TradeSystem.GetDrams(player);
                ConversationManager.StartConversation(s,player);Assert.IsTrue(CinderholdCompositionTests.Choose("I'd like a copy of this grimoire."));Assert.IsTrue(CinderholdCompositionTests.Choose("Please, make the copy."));
                Assert.IsTrue(inv.Objects.Contains(original));Assert.AreEqual(copyOnly?1:2,inv.Objects.Count);Assert.AreEqual(drams,TradeSystem.GetDrams(player));
                if(!copyOnly){var copy=inv.Objects.Single(e=>e!=original);Assert.AreEqual(original.GetPart<GrimoirePart>().SkillClassName,copy.GetPart<GrimoirePart>().SkillClassName);Assert.AreEqual(original.GetPart<GrimoirePart>().KnowledgeProperty,copy.GetPart<GrimoirePart>().KnowledgeProperty);}
            }finally{ConversationManager.EndConversation();ConversationLoader.Reset();ConversationActions.Factory=old;}
        }
        // A refectory serves communal meals; beds alone would silently turn
        // the lore's shared eating room into another dormitory.
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void RefectoryIsACommunalEatingRoomAndReceivingKeepsSeparateCourierRest(int seed)
        {
            var p=QuillholdCompositionPlan.Create(Id,seed);var dining=p.Rooms.Single(r=>r.Role=="Refectory");var receiving=p.Rooms.Single(r=>r.Role=="Receiving");
            var diningObjects=(from y in Enumerable.Range(dining.Y,dining.Height) from x in Enumerable.Range(dining.X,dining.Width) select p.ObjectAt(x,y)).ToArray();
            Assert.GreaterOrEqual(diningObjects.Count(bp=>bp=="QuillholdRefectoryTable"),2,"Joined native table cells communicate communal eating without an oversized indivisible prop.");
            Assert.GreaterOrEqual(diningObjects.Count(bp=>bp=="Chair"),4);Assert.AreEqual(0,diningObjects.Count(bp=>bp=="Bed"));
            Assert.IsTrue((from y in Enumerable.Range(receiving.Y,receiving.Height) from x in Enumerable.Range(receiving.X,receiving.Width) select p.ObjectAt(x,y)).Contains("Bed"),"Maintain a separate rest corner, not a bed disguised as a dining table.");
        }
        public static HashSet<(int x,int y)> DryReach(Zone z)
        {
            var start=(x:0,y:12);var seen=new HashSet<(int x,int y)>();var q=new Queue<(int x,int y)>();if(!z.GetCell(0,12).BlocksMovement()){seen.Add(start);q.Enqueue(start);}
            while(q.Count>0){var at=q.Dequeue();foreach(var n in CinderholdCompositionTests.Neighbors(at.x,at.y))if(z.InBounds(n.x,n.y)&&!z.GetCell(n.x,n.y).BlocksMovement()&&!z.GetCell(n.x,n.y).Objects.Any(e=>e.HasPart<LiquidPoolPart>())&&seen.Add(n))q.Enqueue(n);}return seen;
        }
    }
}
