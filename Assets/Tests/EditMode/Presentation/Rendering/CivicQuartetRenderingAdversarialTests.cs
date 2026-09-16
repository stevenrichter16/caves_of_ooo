using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class CivicQuartetRenderingAdversarialTests
    {
        private static SpawnRing3DCatalog Catalog=>Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
        [TestCase("Overworld.7.8.0")][TestCase("Overworld.13.7.0")][TestCase("Overworld.14.9.0")][TestCase("Overworld.10.14.0")]
        public void NativeCarvedQuestTokenHasPortableArtOnlyForItsActualObjective(string id)
        {
            var z=new Zone(id);var e=new Entity{BlueprintName="CrunchyLocket",ID="CrunchyLocket"};
            e.AddPart(new RenderPart{RenderString="*",Visible=true});e.AddPart(new PhysicsPart{Takeable=true,Weight=1});
            e.AddPart(new CompleteObjectiveOnTaken{Quest="CrunchyLocket",Objective="find_locket"});z.AddEntity(e,10,10);
            var r=SpawnRing3DRecipes.Resolve(z,e,Catalog);Assert.NotNull(r.ModelId);StringAssert.StartsWith("cinderhold-token-",r.ModelId);Assert.IsTrue(r.Transient);
            z.RemoveEntity(e);z.AddEntity(e,20,10);Assert.AreEqual(r.ModelId,SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
            e.GetPart<CompleteObjectiveOnTaken>().Objective="unrelated";Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
            e.GetPart<CompleteObjectiveOnTaken>().Objective="find_locket";e.GetPart<CompleteObjectiveOnTaken>().Quest="unrelated";
            Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
            e.GetPart<CompleteObjectiveOnTaken>().Quest="CrunchyLocket";e.GetPart<PhysicsPart>().Takeable=false;
            Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
        }
        [TestCase("Overworld.7.8.0","GantryRegistrar")][TestCase("Overworld.13.7.0","Scribe")]
        [TestCase("Overworld.14.9.0","Scribe")][TestCase("Overworld.10.14.0","Quartermaster")]
        public void ActorBodySurvivesMovementAndRejectsUnrelatedReskins(string id,string bp)
        {
            var z=new Zone(id);var e=GrovelandsCompositionTests.Factory().CreateEntity(bp);z.AddEntity(e,10,10);
            var first=SpawnRing3DRecipes.Resolve(z,e,Catalog);Assert.NotNull(first.ModelId);Assert.IsTrue(first.Transient);
            z.RemoveEntity(e);z.AddEntity(e,20,10);Assert.AreEqual(first.ModelId,SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
            e.GetPart<RenderPart>().RenderString="?";Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
        }
        [TestCase("b","Baker_Quest","MessageForHermit")]
        [TestCase("h","Hermit_Quest",null)]
        [TestCase("f","Warren_Quest","ClearTheWarren")]
        [TestCase("c","Crunchy_Quest","CrunchyLocket")]
        [TestCase("P","CandyTax_Quest","TheCandyTax")]
        [TestCase("c","CandyCitizen",null)]
        public void NativeQuestAppearanceNeedsItsConversationAndQuestIdentity(string glyph,string conversation,string quest)
        {
            foreach(var id in CivicQuartetRenderingTests.Addresses())
            {
                var z=new Zone(id);var e=GrovelandsCompositionTests.Factory().CreateEntity("Villager");z.AddEntity(e,20,10);
                e.GetPart<RenderPart>().RenderString=glyph;
                var c=e.GetPart<ConversationPart>();if(c==null){c=new ConversationPart();e.AddPart(c);}c.ConversationID=conversation;
                if(quest!=null)e.AddPart(new QuestBeaconPart{Quest=quest});
                Assert.NotNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId,id+" "+glyph);
                c.ConversationID="unrelated";Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
                c.ConversationID=conversation;if(quest!=null){e.GetPart<QuestBeaconPart>().Quest="unrelated";Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);}
            }
        }
        [TestCase("Overworld.7.8.0")][TestCase("Overworld.13.7.0")][TestCase("Overworld.14.9.0")][TestCase("Overworld.10.14.0")]
        public void WarrenQuestMobRequiresItsRealDeathFact(string id)
        {
            var z=new Zone(id);var e=GrovelandsCompositionTests.Factory().CreateEntity("Snapjaw");z.AddEntity(e,20,10);
            e.GetPart<RenderPart>().RenderString="g";e.AddPart(new AddFactWhenSlain{Fact="warren_gnomes_routed",Amount=1});
            var r=SpawnRing3DRecipes.Resolve(z,e,Catalog);Assert.NotNull(r.ModelId);Assert.IsTrue(r.Transient);
            e.GetPart<AddFactWhenSlain>().Amount=2;Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
            e.GetPart<AddFactWhenSlain>().Amount=1;
            e.GetPart<AddFactWhenSlain>().Fact="unrelated";Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
        }
        [TestCase("Overworld.7.8.0","GantryRegistryDesk")][TestCase("Overworld.13.7.0","Duckboard")]
        [TestCase("Overworld.14.9.0","QuillholdArchiveShelf")][TestCase("Overworld.10.14.0","Crate")]
        public void StaticArtNeverAddsMechanicsOrRebuildsRemovedOwners(string id,string bp)
        {
            var z=new Zone(id);var e=GrovelandsCompositionTests.Factory().CreateEntity(bp);z.AddEntity(e,20,10);
            int parts=e.Parts.Count;int version=z.EntityVersion;
            var r=SpawnRing3DRecipes.Resolve(z,e,Catalog);Assert.NotNull(r.ModelId);Assert.IsTrue(r.Batched);Assert.IsFalse(r.Transient);
            Assert.AreEqual(parts,e.Parts.Count);Assert.AreEqual(version,z.EntityVersion);
            z.RemoveEntity(e);Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);Assert.AreEqual(0,z.EntityCount);
        }
        [TestCase("Overworld.7.8.0","GantryTimberWall")][TestCase("Overworld.13.7.0","SandstoneWall")]
        [TestCase("Overworld.14.9.0","QuillholdArchiveShelf")][TestCase("Overworld.10.14.0","TentWall")]
        public void NativeDamageRemovesOnlyTheVictimsGeometry(string id,string bp)
        {
            using(var f=new SpawnRing3DIntegrationFixture(id))
            {
                f.Zone=new Zone(id);f.Bind(f.Zone);f.Set("FullReveal",true);
                f.Add("StoneFloor",20,10);f.Add("StoneFloor",21,10);var a=f.Add(bp,20,10);var b=f.Add(bp,21,10);f.Refresh();
                Assert.IsTrue(f.Find(a,out _,out _));Assert.IsTrue(f.Find(b,out _,out _));
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(a,1000,null,f.Zone));f.Refresh(new System.Collections.Generic.HashSet<int>());
                Assert.IsFalse(f.Find(a,out _,out _));Assert.IsTrue(f.Find(b,out _,out _));
            }
        }
        [TestCase("Overworld.7.8.0")][TestCase("Overworld.13.7.0")][TestCase("Overworld.14.9.0")][TestCase("Overworld.10.14.0")]
        public void CrossMapFallbackRejectsSameProfileOnWrongPlaceType(string id)
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);var at=WorldMap.FromZoneID(id);var z=m.GetZone(id);Assert.IsTrue(AreaCompositionScope.Allows(z));
            var p=m.WorldMap.GetPOI(at.x,at.y);string profile=p.Profile;m.WorldMap.SetPOI(at.x,at.y,new PointOfInterest(POIType.Lair,p.Name,profile:profile));
            Assert.IsFalse(AreaCompositionScope.Allows(z));var e=z.GetAllEntities().First();Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
        }
        [Test] public void DestroyingTimberArmRefreshesSurvivingWallAcrossPatchBoundary()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.7.8.0"))
            {
                f.Zone=new Zone("Overworld.7.8.0");f.Bind(f.Zone);f.Set("FullReveal",true);
                var corner=f.Add("GantryTimberWall",19,10);var arm=f.Add("GantryTimberWall",20,10);f.Add("GantryTimberWall",19,9);
                f.Add("Rock",61,21);f.Refresh();Assert.AreEqual(0,SpawnRing3DRecipes.Resolve(f.Zone,corner,Catalog).QuarterTurns);
                int before=f.Revision(19,10),far=f.Revision(61,21);Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(arm,1000,null,f.Zone));
                f.Refresh(SpawnRing3DIntegrationFixture.Dirty(20,10));Assert.Greater(f.Revision(19,10),before);Assert.AreEqual(far,f.Revision(61,21));
                Assert.AreEqual(1,SpawnRing3DRecipes.Resolve(f.Zone,corner,Catalog).QuarterTurns);Assert.IsFalse(f.Find(arm,out _,out _));Assert.IsTrue(f.Find(corner,out _,out _));
                int stable=f.Revision(19,10);f.Refresh(new System.Collections.Generic.HashSet<int>());Assert.AreEqual(stable,f.Revision(19,10));
            }
        }
        [Test] public void TineNativeReedsHaveTheirExistingVoxelBodyWithoutClaimingAnotherTown()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.13.7.0");var reed=f.CreateEntity("Reeds");z.AddEntity(reed,20,10);
            var recipe=SpawnRing3DRecipes.Resolve(z,reed,Catalog);Assert.NotNull(recipe.ModelId,recipe.Failure);StringAssert.StartsWith("spread-reeds-",recipe.ModelId);
            reed.GetPart<RenderPart>().Visible=false;Assert.IsNull(SpawnRing3DRecipes.Resolve(z,reed,Catalog).ModelId);reed.GetPart<RenderPart>().Visible=true;z.RemoveEntity(reed);Assert.IsNull(SpawnRing3DRecipes.Resolve(z,reed,Catalog).ModelId);
        }
        [TestCase("GantryRegistrar")][TestCase("TentRightHost")][TestCase("Scribe")]
        public void ATravellingPersonDoesNotChangeBodyBetweenTheFourCivicAreas(string bp)
        {
            var person=GrovelandsCompositionTests.Factory().CreateEntity(bp);person.ID="same-person-travelling";string expected=null;Zone previous=null;
            foreach(string id in CivicQuartetRenderingTests.Addresses())
            {
                previous?.RemoveEntity(person);var zone=new Zone(id);Assert.IsTrue(zone.AddEntity(person,21,11));var recipe=SpawnRing3DRecipes.Resolve(zone,person,Catalog);Assert.NotNull(recipe.ModelId,recipe.Failure);
                if(expected==null)expected=recipe.ModelId;else Assert.AreEqual(expected,recipe.ModelId,"A town's art palette may not change the same person's body: "+id);
                if(previous!=null)Assert.IsNull(SpawnRing3DRecipes.Resolve(previous,person,Catalog).ModelId);previous=zone;
            }
        }
    }
}
