using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadSillQuestVisualTests
    {
        [TestCase("Hallun")][TestCase("Ellun")]
        public void ActualAuthoredQuestVillagerHasAnOwnedAnimatedVillagerBody(string name)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.10.10.0"))
            {
                var actor=Authored(f,name);var position=f.Zone.GetEntityPosition(actor);
                var render=actor.GetPart<RenderPart>();var conversation=actor.GetPart<ConversationPart>();var beacon=actor.GetPart<QuestBeaconPart>();
                string glyph=render.RenderString,color=render.ColorString,quest=beacon.Quest,dialogue=conversation.ConversationID;
                f.Set("FullReveal",true);f.Refresh();
                var recipe=SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition);
                Assert.AreEqual(SpreadBiomeHumanoidLibrary.ModelId("Villager"),recipe.ModelId);
                Assert.True(f.Find(actor,out var root,out var id));Assert.AreEqual(recipe.ModelId,id);Assert.True(f.Rendered(actor));Assert.True(f.Pick(actor,out _));
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(actor,out var proof),proof.Failure);
                Assert.AreSame(SpreadBiomeHumanoidLibrary.Load().Find(recipe.ModelId).Mesh,proof.SubmittedMesh);
                Assert.AreEqual(position,f.Zone.GetEntityPosition(actor));Assert.AreEqual(glyph,render.RenderString);Assert.AreEqual(color,render.ColorString);
                Assert.AreEqual(quest,beacon.Quest);Assert.AreEqual(dialogue,conversation.ConversationID);
            }
        }
        [TestCase("glyph")][TestCase("color")][TestCase("conversation")][TestCase("beacon")]
        [TestCase("foreign-conversation")][TestCase("foreign-beacon")][TestCase("missing-beacon")]
        [TestCase("mixed-identity")][TestCase("visual-override")][TestCase("portable")]
        public void AuthoredExceptionRequiresTheWholeCurrentAppearanceContract(string mutation)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.10.10.0"))
            {
                foreach(string name in new[]{"Hallun","Ellun"})
                {
                    var actor=Authored(f,name);var render=actor.GetPart<RenderPart>();var conversation=actor.GetPart<ConversationPart>();var beacon=actor.GetPart<QuestBeaconPart>();
                    switch(mutation)
                    {
                        case "glyph":render.RenderString="?";break;
                        case "color":render.ColorString="&K";break;
                        case "conversation":conversation.ConversationID="Other";break;
                        case "beacon":beacon.Quest="Other";break;
                        case "foreign-conversation":conversation.ParentEntity=f.Player;break;
                        case "foreign-beacon":beacon.ParentEntity=f.Player;break;
                        case "missing-beacon":Assert.True(actor.RemovePart(beacon));break;
                        case "mixed-identity":conversation.ConversationID=name=="Hallun"?"BMO_Quest":"RootBeerGuy_Quest";beacon.Quest=name=="Hallun"?"BmoCartridge":"RootBeerGuyCase";break;
                        case "visual-override":render.VisualID="foreign-explicit-body";break;
                        case "portable":actor.GetPart<PhysicsPart>().Takeable=true;break;
                    }
                    Assert.AreNotEqual(SpreadBiomeHumanoidLibrary.ModelId("Villager"),SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId,name+"/"+mutation);
                }
            }
        }
        [Test]
        public void MerelyRenamedOrRecoloredGenericVillagerDoesNotAcquireQuestAppearanceAuthority()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.10.10.0"))
            {
                var actor=f.Add("Villager");var render=actor.GetPart<RenderPart>();render.DisplayName="Hallun";render.RenderString="r";render.ColorString="&r";
                Assert.AreNotEqual(SpreadBiomeHumanoidLibrary.ModelId("Villager"),SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId);
            }
        }
        private static Entity Authored(SpawnRing3DIntegrationFixture f,string name)
        {
            var actor=f.Zone.GetReadOnlyEntities().Single(x=>x.BlueprintName=="Villager"&&x.GetPart<RenderPart>()?.DisplayName==name);
            Assert.NotNull(actor.GetPart<ConversationPart>());Assert.NotNull(actor.GetPart<QuestBeaconPart>());return actor;
        }
    }
}
