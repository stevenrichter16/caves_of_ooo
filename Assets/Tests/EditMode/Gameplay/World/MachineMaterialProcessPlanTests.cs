using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Read-only plan contract: no conversion, new content or actual-play claim.
    public sealed class MachineMaterialProcessPlanTests : FiftyWorldFixture
    {
        const string Data="{\"Processes\":[{\"Id\":\"point\",\"SourceBlueprint\":\"Dagger\",\"Mode\":\"Process\",\"InputCount\":1,\"WorkKind\":\"ForgeCold\",\"OutputBlueprints\":[\"IronSpikeComponent\"],\"OutputCounts\":[1]}]}";
        EntityFactory oldForge;
        Type Registry=>typeof(Part).Assembly.GetType("CavesOfOoo.Core.MaterialProcessRegistry");
        Type Planner=>typeof(Part).Assembly.GetType("CavesOfOoo.Core.MaterialProcessPlan");
        Type InputPart=>typeof(Part).Assembly.GetType("CavesOfOoo.Core.MaterialProcessInputPart");
        void Install(string data)=>Registry.GetMethod("InitializeFromJson").Invoke(null,new object[]{data});
        [SetUp] public void SetupPlan(){oldForge=ForgePart.Factory;ForgePart.Factory=Factory;Install(Data);foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}}
        [TearDown] public void RestorePlan(){ForgePart.Factory=oldForge;MaterialProcessRegistry.ResetForTests();}
        Entity Input(bool optIn=true)
        {
            var item=Carry("Dagger");
            if(optIn)
            {
                Assert.NotNull(InputPart,"Only explicitly opted-in input Parts may expose material processes.");
                var part=(Part)Activator.CreateInstance(InputPart);InputPart.GetField("ProcessIds").SetValue(part,"point");item.AddPart(part);
            }
            return item;
        }
        object Plan(Entity item)
        {
            Assert.NotNull(Planner,"A preview must resolve and retain exact source, recipe and physical station.");
            return Planner.GetMethod("TryCreate",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{Actor,item,"point",Zone,Factory});
        }
        bool Current(object plan)=>(bool)Planner.GetMethod("Current").Invoke(plan,null);
        [Test] public void PurePlanQuotesExactSourceWithoutCreatingOrSpendingAnything()
        {
            Place("TinkersForge");var item=Input();var before=Pack.Objects.ToArray();int tick=Clock.TickCount;
            var plan=Plan(item);Assert.NotNull(plan);Assert.True(Current(plan));
            CollectionAssert.AreEqual(before,Pack.Objects);Assert.AreEqual(tick,Clock.TickCount);Assert.AreEqual(0,Count("IronSpikeComponent"));
        }
        [Test] public void OrdinaryMetalItemDoesNotGainAnImplicitRecoveryManifest()
        {Place("TinkersForge");Assert.Null(Plan(Input(false)));}
        [TestCase("none")] [TestCase("far")] [TestCase("hidden")]
        public void UnavailableStationProducesNoPlan(string condition)
        {
            var item=Input();if(condition!="none"){var forge=Place("TinkersForge",condition=="far"?20:11,10);if(condition=="hidden")Zone.GetEntityCell(forge).IsVisible=false;}
            Assert.Null(Plan(item));Assert.True(Pack.Objects.Contains(item));
        }
        [TestCase("actor-move")] [TestCase("source-id")] [TestCase("source-weight")]
        [TestCase("source-part")] [TestCase("source-drop")] [TestCase("recipe-reload")]
        [TestCase("forge-move")] [TestCase("forge-replaced")] [TestCase("forge-blueprint")]
        [TestCase("authorization")] [TestCase("factory-replaced")]
        public void CapturedPreviewCannotAuthorizeChangedParticipants(string mutation)
        {
            var forge=Place("TinkersForge");var item=Input();var plan=Plan(item);Assert.NotNull(plan);
            if(mutation=="actor-move"){Zone.RemoveEntity(Actor);Zone.AddEntity(Actor,10,11);}
            if(mutation=="source-id")item.ID="different";
            if(mutation=="source-weight")item.GetPart<PhysicsPart>().Weight++;
            if(mutation=="source-part")item.AddPart(new ContainerPart());
            if(mutation=="source-drop"){Pack.RemoveObject(item);Zone.AddEntity(item,10,10);}
            if(mutation=="recipe-reload")Install(Data);
            if(mutation=="forge-blueprint")forge.BlueprintName="ChangedForge";
            if(mutation=="forge-move"){Zone.RemoveEntity(forge);Zone.AddEntity(forge,10,11);}
            if(mutation=="forge-replaced"){Zone.RemoveEntity(forge);Place("TinkersForge");}
            if(mutation=="authorization")InputPart.GetField("ProcessIds").SetValue(item.Parts.Single(p=>p.GetType()==InputPart),"");
            if(mutation=="factory-replaced")ForgePart.Factory=new EntityFactory();
            Assert.False(Current(plan));Assert.AreEqual(0,Count("IronSpikeComponent"));
        }
        [Test] public void MissingProductInvalidatesPreviewBeforeAnyCreation()
        {Place("TinkersForge");var item=Input();Factory.Blueprints.Remove("IronSpikeComponent");Assert.Null(Plan(item));Assert.True(Pack.Objects.Contains(item));}
    }
}
