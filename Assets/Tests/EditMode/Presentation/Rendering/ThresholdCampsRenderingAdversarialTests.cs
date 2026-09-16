using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class ThresholdCampsRenderingAdversarialTests
    {
        private static SpawnRing3DCatalog Catalog=>Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
        [TestCase("Overworld.5.17.0")] [TestCase("Overworld.18.18.0")]
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
        [TestCase("Overworld.5.17.0","TentRightHost")]
        [TestCase("Overworld.5.17.0","SaltMaster")]
        [TestCase("Overworld.18.18.0","SaccharineEnvoy")]
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
            foreach(var id in ThresholdCampsRenderingTests.Addresses())
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
        [TestCase("Overworld.5.17.0")] [TestCase("Overworld.18.18.0")]
        public void WarrenQuestMobRequiresItsRealDeathFact(string id)
        {
            var z=new Zone(id);var e=GrovelandsCompositionTests.Factory().CreateEntity("Snapjaw");z.AddEntity(e,20,10);
            e.GetPart<RenderPart>().RenderString="g";e.AddPart(new AddFactWhenSlain{Fact="warren_gnomes_routed",Amount=1});
            var r=SpawnRing3DRecipes.Resolve(z,e,Catalog);Assert.NotNull(r.ModelId);Assert.IsTrue(r.Transient);
            e.GetPart<AddFactWhenSlain>().Amount=2;Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
            e.GetPart<AddFactWhenSlain>().Amount=1;
            e.GetPart<AddFactWhenSlain>().Fact="unrelated";Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);
        }
        [TestCase("Overworld.5.17.0","GuestClothPole")]
        [TestCase("Overworld.5.17.0","TentWall")]
        [TestCase("Overworld.5.17.0","Well")]
        [TestCase("Overworld.18.18.0","LastCounterSign")]
        [TestCase("Overworld.18.18.0","Chest")]
        public void StaticArtNeverAddsMechanicsOrRebuildsRemovedOwners(string id,string bp)
        {
            var z=new Zone(id);var e=GrovelandsCompositionTests.Factory().CreateEntity(bp);z.AddEntity(e,20,10);
            int parts=e.Parts.Count;int version=z.EntityVersion;
            var r=SpawnRing3DRecipes.Resolve(z,e,Catalog);Assert.NotNull(r.ModelId);Assert.IsTrue(r.Batched);Assert.IsFalse(r.Transient);
            Assert.AreEqual(parts,e.Parts.Count);Assert.AreEqual(version,z.EntityVersion);
            z.RemoveEntity(e);Assert.IsNull(SpawnRing3DRecipes.Resolve(z,e,Catalog).ModelId);Assert.AreEqual(0,z.EntityCount);
        }
        [TestCase("Overworld.5.17.0","TentWall")]
        [TestCase("Overworld.18.18.0","Crate")]
        public void NativeDamageRemovesOnlyTheVictimsGeometry(string id,string bp)
        {
            using(var f=new SpawnRing3DIntegrationFixture(id))
            {
                f.Zone=new Zone(id);f.Bind(f.Zone);f.Set("FullReveal",true);
                f.Add("Sand",20,10);f.Add("Sand",21,10);var a=f.Add(bp,20,10);var b=f.Add(bp,21,10);f.Refresh();
                Assert.IsTrue(f.Find(a,out _,out _));Assert.IsTrue(f.Find(b,out _,out _));
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(a,1000,null,f.Zone));f.Refresh(new System.Collections.Generic.HashSet<int>());
                Assert.IsFalse(f.Find(a,out _,out _));Assert.IsTrue(f.Find(b,out _,out _));
            }
        }
        [TestCase("GuestClothPole")] [TestCase("LastCounterSign")]
        public void NativeUndamageableSignageDoesNotGainFakeDestruction(string bp)
        {
            foreach(var id in ThresholdCampsRenderingTests.Addresses())
            {
                var z=new Zone(id);var owner=GrovelandsCompositionTests.Factory().CreateEntity(bp);z.AddEntity(owner,20,10);
                Assert.IsFalse(owner.HasPart<DestructiblePart>());
                int version=z.EntityVersion;
                SpawnRing3DRecipes.Resolve(z,owner,Catalog);
                Assert.IsFalse(owner.HasPart<DestructiblePart>());Assert.AreEqual(version,z.EntityVersion);
                Assert.AreNotEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(owner,1000,null,z));
                Assert.AreSame(owner,z.GetCell(20,10).Objects.Single());
            }
        }
        [TestCase(19,10,20,10,19,9)]
        [TestCase(20,9,20,10,21,9)]
        public void DestroyingTentArmUpdatesTheSurvivingCornerAcrossPatchBoundary(int x,int y,int ax,int ay,int bx,int by)
        {
            // Hypothesis: a dirty cell in another10x5 patch could leave the
            // surviving tent corner stale even after the arm disappears.
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.5.17.0"))
            {
                f.Zone=new Zone("Overworld.5.17.0");f.Bind(f.Zone);f.Set("FullReveal",true);
                var corner=f.Add("TentWall",x,y);var arm=f.Add("TentWall",ax,ay);f.Add("TentWall",bx,by);
                f.Add("Rock",61,21);f.Refresh();
                StringAssert.StartsWith("firsttent-corner-",SpawnRing3DRecipes.Resolve(f.Zone,corner,Catalog).ModelId);
                int before=f.Revision(x,y),far=f.Revision(61,21);
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(arm,1000,null,f.Zone));
                f.Refresh(SpawnRing3DIntegrationFixture.Dirty(ax,ay));
                Assert.Greater(f.Revision(x,y),before);Assert.AreEqual(far,f.Revision(61,21));
                StringAssert.StartsWith("firsttent-tent-",SpawnRing3DRecipes.Resolve(f.Zone,corner,Catalog).ModelId);
                Assert.IsFalse(f.Find(arm,out _,out _));Assert.IsTrue(f.Find(corner,out _,out _));
                int stable=f.Revision(x,y);f.Refresh(new System.Collections.Generic.HashSet<int>());Assert.AreEqual(stable,f.Revision(x,y));
            }
        }
    }
}
