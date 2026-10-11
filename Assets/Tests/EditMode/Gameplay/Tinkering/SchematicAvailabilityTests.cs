using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public abstract class SchematicAvailabilityFixture
    {
        protected EntityFactory Factory; protected Entity Actor, Item; protected InventoryPart Pack;
        internal const string RecipeJson="{\"Recipes\":[{\"ID\":\"mod_sharp_melee\",\"DisplayName\":\"Test edge\",\"Type\":\"Mod\",\"Cost\":\"BC\"}]}";
        [SetUp] public void Setup(){Factory=new EntityFactory();Factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));Actor=Factory.CreateEntity("Player");Pack=Actor.GetPart<InventoryPart>();Item=Factory.CreateEntity("SchematicHonedEdge");Assert.True(Pack.AddObject(Item));TinkerRecipeRegistry.InitializeFromJson(RecipeJson);MessageLog.Clear();}
        [TearDown] public void Cleanup(){TinkerRecipeRegistry.ResetForTests();}
        protected BitLockerPart Access(){var b=new BitLockerPart();Actor.AddPart(b);return b;}
        protected bool Offered()=>InventorySystem.GetActions(Actor,Item).Any(a=>a.Command=="StudySchematic");
        protected bool Study()=>InventorySystem.PerformAction(Actor,Item,"StudySchematic");
        protected sealed class Hook:Part{readonly string id;readonly Action action;public Hook(string id,Action action){this.id=id;this.action=action;}public override bool HandleEvent(GameEvent e){if(e.ID==id)action();return true;}}
    }
    public sealed class SchematicAvailabilityTests:SchematicAvailabilityFixture
    {
        [Test] public void OrdinaryActorCanInspectButHasNoStudyActionOrTinkeringGrant(){Assert.False(Actor.HasPart<BitLockerPart>());Assert.False(Offered());Assert.False(Study());Assert.False(Actor.HasPart<BitLockerPart>());Assert.Contains(Item,Pack.Objects);}
        [TestCase(false)] [TestCase(true)] public void DeveloperAccessCanLearnAndConsumeOnlyWhenAuthored(bool consume){var b=Access();Item.GetPart<SchematicPart>().ConsumeOnStudy=consume;Assert.True(Offered());Assert.True(Study());Assert.True(b.KnowsRecipe("mod_sharp_melee"));Assert.AreEqual(!consume,Pack.Objects.Contains(Item));Assert.False(Offered());Assert.False(Study());}
        [TestCase("missing")] [TestCase("")] [TestCase("known")] public void UnavailableRecipeNeverAdvertisesOrReportsSuccess(string reason){var b=Access();if(reason=="known")b.LearnRecipe("mod_sharp_melee");else Item.GetPart<SchematicPart>().RecipeID=reason;Assert.False(Offered());Assert.False(Study());Assert.Contains(Item,Pack.Objects);Assert.AreEqual(reason=="known"?1:0,b.GetKnownRecipes().Count);}
        [Test] public void InspectionExplainsCurrentScopeAndActualRecipeWithoutMutation(){Assert.True(ItemExamineService.TryDescribeDetails(Item,out var text));StringAssert.Contains("Test edge",text);StringAssert.Contains("unavailable in this build",text);StringAssert.Contains("does not unlock",text);StringAssert.Contains("kept or traded",text);StringAssert.DoesNotContain("developer",text);StringAssert.Contains(text,Item.GetPart<ExaminablePart>().BuildExamineLine());Assert.False(Actor.HasPart<BitLockerPart>());Assert.Contains(Item,Pack.Objects);Assert.Greater(Item.GetPart<CommercePart>().Value,0);}
        [Test] public void InvalidPatternInspectionDoesNotPromiseLearning(){Item.GetPart<SchematicPart>().RecipeID="missing";Assert.True(ItemExamineService.TryDescribeDetails(Item,out var text));StringAssert.Contains("unavailable",text.ToLowerInvariant());StringAssert.DoesNotContain("Test edge",text);Assert.False(Offered());}
        [Test] public void ActorlessMenuCannotPromiseAnExecutableStudy(){Assert.False(InventorySystem.GetActions(null,Item).Any(a=>a.Command=="StudySchematic"));}
        [Test] public void LearnedDeveloperRecipeAndSchematicRoundTripWithoutGrantingOrdinaryAccess(){var b=Access();Assert.True(Study());var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);Assert.True(loaded.GetPart<BitLockerPart>().KnowsRecipe("mod_sharp_melee"));Assert.True(loaded.GetPart<InventoryPart>().Objects.Any(i=>i.BlueprintName==Item.BlueprintName));}
    }
    public sealed class SchematicAvailabilityAdversarialTests:SchematicAvailabilityFixture
    {
        [TestCase("lost")] [TestCase("wrong-owner")] [TestCase("equipped-alias")] [TestCase("empty-stack")] [TestCase("no-access")] [TestCase("registry")]
        public void ChangedAuthorityAfterMenuRejectsWithoutLearning(string change){var b=Access();Assert.True(Offered());if(change=="lost")Pack.RemoveObject(Item);if(change=="wrong-owner")Item.GetPart<PhysicsPart>().InInventory=new Entity();if(change=="equipped-alias")Item.GetPart<PhysicsPart>().Equipped=Actor;if(change=="empty-stack"){Item.AddPart(new StackerPart());Item.GetPart<StackerPart>().StackCount=0;}if(change=="no-access")Actor.RemovePart(b);if(change=="registry")TinkerRecipeRegistry.InitializeFromJson("{\"Recipes\":[]}");Assert.False(Offered());Assert.False(Study());Assert.False(b.KnowsRecipe("mod_sharp_melee"));}
        [TestCase("throw")] [TestCase("recipe")] [TestCase("registry")] [TestCase("access")]
        [TestCase("recipe-id")] [TestCase("part")] [TestCase("physics")] [TestCase("stack")] [TestCase("returned-item")] [TestCase("consume-flag")]
        public void AfterActionFailureOrMutationCannotLearnFromRefundedConsumption(string change){var b=Access();Item.GetPart<SchematicPart>().ConsumeOnStudy=true;Actor.AddPart(new Hook("AfterInventoryAction",()=>{if(change=="throw")throw new InvalidOperationException("schematic receipt probe");if(change=="recipe")Item.GetPart<SchematicPart>().RecipeID="missing";if(change=="registry")TinkerRecipeRegistry.InitializeFromJson(RecipeJson);if(change=="access")Actor.RemovePart(b);if(change=="recipe-id"){Assert.True(TinkerRecipeRegistry.TryGetRecipe("mod_sharp_melee",out var current));current.ID="unregistered";}if(change=="part")Item.RemovePart(Item.GetPart<SchematicPart>());if(change=="physics")Item.RemovePart(Item.GetPart<PhysicsPart>());if(change=="stack"){var old=Item.GetPart<StackerPart>();Assert.NotNull(old);Assert.True(Item.RemovePart(old));Item.AddPart(new StackerPart{StackCount=1});}if(change=="returned-item")Assert.True(Pack.AddObject(Item));if(change=="consume-flag")Item.GetPart<SchematicPart>().ConsumeOnStudy=false;}));Assert.False(Study());Assert.AreEqual(0,b.GetKnownRecipes().Count);Assert.Contains(Item,Pack.Objects);}
        [Test] public void ReentrantStudyCannotLearnTwiceOrConsumeTwoUnits(){var b=Access();Item.GetPart<SchematicPart>().ConsumeOnStudy=true;Item.GetPart<StackerPart>().StackCount=2;bool nested=true;int calls=0;Actor.AddPart(new Hook("AfterInventoryAction",()=>{if(calls++==0)nested=Study();}));Assert.True(Study());Assert.False(nested);Assert.AreEqual(1,Item.GetPart<StackerPart>().StackCount);Assert.Contains(Item,Pack.Objects);Assert.AreEqual(1,b.GetKnownRecipes().Count);}
        [Test] public void DirectRefusalDoesNotSetHandledOrStopOtherParts(){var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)Actor);e.SetParameter("Command","StudySchematic");try{Assert.True(Item.FireEvent(e));Assert.False(e.Handled);}finally{e.Release();}}
        [Test] public void RepeatedInspectionDoesNotConsumeOrTeachEvenWithAccess(){var b=Access();Item.GetPart<SchematicPart>().ConsumeOnStudy=true;for(int i=0;i<5;i++){Assert.True(ItemExamineService.TryDescribeDetails(Item,out _));Assert.True(Offered());}Assert.AreEqual(0,b.GetKnownRecipes().Count);Assert.Contains(Item,Pack.Objects);}
    }
}
