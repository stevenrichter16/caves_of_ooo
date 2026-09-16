using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class WitnessIntakeRenderingAdversarialTests
    {
        private static SpawnRing3DCatalog Catalog=>Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
        [TestCase("Overworld.17.5.0")] [TestCase("Overworld.12.12.0")]
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
        [TestCase("Overworld.17.5.0","RecensionScribe")]
        [TestCase("Overworld.17.5.0","CurationSorter")]
        [TestCase("Overworld.12.12.0","FilerClerk")]
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
            foreach(var id in WitnessIntakeRenderingTests.Addresses())
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
        [TestCase("Overworld.17.5.0")] [TestCase("Overworld.12.12.0")]
        public void WarrenQuestMobRequiresItsRealDeathFact(string id)
        {
            var z=new Zone(id);var e=GrovelandsCompositionTests.Factory().CreateEntity("Snapjaw");z.AddEntity(e,20,10);
            e.GetPart<RenderPart>().RenderString="g";e.AddPart(new AddFactWhenSlain{Fact="warren_gnomes_routed",Amount=1});
            var r=SpawnRing3DRecipes.Resolve(z,e,Catalog);Assert.NotNull(r.ModelId);Assert.IsTrue(r.Transient);
            e.GetPart<AddFactWhenSlain>().Amount=2;Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
            e.GetPart<AddFactWhenSlain>().Amount=1;
            e.GetPart<AddFactWhenSlain>().Fact="unrelated";Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
        }
        [TestCase("Overworld.17.5.0","SurveyStake")]
        [TestCase("Overworld.17.5.0","ReadingTable")]
        [TestCase("Overworld.17.5.0","PreFellingBody")]
        [TestCase("Overworld.12.12.0","StoneCoffer")]
        [TestCase("Overworld.12.12.0","SaltCuredBody")]
        public void StaticArtNeverAddsMechanicsOrRebuildsRemovedOwners(string id,string bp)
        {
            var z=new Zone(id);var e=GrovelandsCompositionTests.Factory().CreateEntity(bp);z.AddEntity(e,20,10);
            int parts=e.Parts.Count;int version=z.EntityVersion;
            var r=SpawnRing3DRecipes.Resolve(z,e,Catalog);Assert.NotNull(r.ModelId);Assert.IsTrue(r.Batched);Assert.IsFalse(r.Transient);
            Assert.AreEqual(parts,e.Parts.Count);Assert.AreEqual(version,z.EntityVersion);
            z.RemoveEntity(e);Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);Assert.AreEqual(0,z.EntityCount);
        }
        [TestCase("PeatBank")] [TestCase("Duckboard")]
        public void NativeDamageRemovesOnlyTheVictimsGeometry(string bp)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.17.5.0"))
            {
                f.Zone=new Zone("Overworld.17.5.0");f.Bind(f.Zone);f.Set("FullReveal",true);
                f.Add("Floor",20,10);f.Add("Floor",21,10);var a=f.Add(bp,20,10);var b=f.Add(bp,21,10);f.Refresh();
                Assert.IsTrue(f.Find(a,out _,out _));Assert.IsTrue(f.Find(b,out _,out _));
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(a,1000,null,f.Zone));f.Refresh(new System.Collections.Generic.HashSet<int>());
                Assert.IsFalse(f.Find(a,out _,out _));Assert.IsTrue(f.Find(b,out _,out _));
                Assert.IsFalse(f.Zone.GetCell(20,10).Objects.Any(e=>e.HasPart<LiquidPoolPart>()));
            }
        }
        [TestCase("StoneCoffer")] [TestCase("SaltCuredBody")] [TestCase("SealedBogTakenBody")]
        public void HaulableOrPortableOwnersRetainTheirModelAcrossMoveAndRemoval(string bp)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.12.0"))
            {
                f.Zone=new Zone("Overworld.12.12.0");f.Bind(f.Zone);f.Set("FullReveal",true);
                var owner=f.Add(bp,20,10);f.Refresh();
                var before=SpawnRing3DRecipes.Resolve(f.Zone,owner,Catalog);Assert.NotNull(before.ModelId);
                Assert.IsTrue(f.Find(owner,out _,out _));
                for(int x=21;x<26;x++)
                {
                    f.Zone.RemoveEntity(owner);Assert.IsTrue(f.Zone.AddEntity(owner,x,10));f.Refresh(new System.Collections.Generic.HashSet<int>());
                    var after=SpawnRing3DRecipes.Resolve(f.Zone,owner,Catalog);
                    Assert.AreEqual(before.ModelId,after.ModelId,"Hauling must not change the cargo's shape.");
                    Assert.AreEqual(Village3DProjection.CellCentre(x,10),after.Position);
                    Assert.IsTrue(f.Find(owner,out _,out _));
                }
                f.Zone.RemoveEntity(owner);f.Refresh(new System.Collections.Generic.HashSet<int>());
                Assert.IsFalse(f.Find(owner,out _,out _));
            }
        }
        [Test]
        public void AllThreeBodyIdentitiesRemainDistinctAndParcelKeepsItsIdentityBetweenSites()
        {
            var factory=GrovelandsCompositionTests.Factory();var ids=new System.Collections.Generic.HashSet<string>();
            var z=new Zone("Overworld.12.12.0");
            foreach(string bp in new[]{"PreFellingBody","SaltCuredBody","SealedBogTakenBody"})
            {
                var e=factory.CreateEntity(bp);z.AddEntity(e,20,10);
                var recipe=SpawnRing3DRecipes.Resolve(z,e,Catalog);Assert.NotNull(recipe.ModelId);Assert.IsTrue(ids.Add(recipe.ModelId));
                if(bp=="SealedBogTakenBody")
                {
                    Assert.IsTrue(recipe.Transient);z.RemoveEntity(e);var ledger=new Zone("Overworld.17.5.0");ledger.AddEntity(e,40,10);
                    Assert.AreEqual(recipe.ModelId,SpawnRing3DRecipes.Resolve(ledger,e,Catalog).ModelId);
                    Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
                }
            }
            Assert.AreEqual(3,ids.Count);
        }
    }
}
