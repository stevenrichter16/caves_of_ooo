using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WorksiteBoxProcessingTests : FiftyWorldFixture
    {
        const string Prefix="CampBoxWork|";
        EntityFactory prior;
        [SetUp] public void PrepareBoxWork()
        {
            prior=ForgePart.Factory;ForgePart.Factory=Factory;MaterialProcessRegistry.ResetForTests();
            Assert.IsEmpty(MaterialProcessRegistry.InitializeFromJson(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/MaterialProcesses.json"))));
            foreach(var c in Zone.Cells){c.IsVisible=true;c.Explored=true;}
            Carry("Dagger");
        }
        [TearDown] public void RestoreBoxWork(){ForgePart.Factory=prior;MaterialProcessRegistry.ResetForTests();}
        Entity Box()
        {
            var box=Place("CampBox");box.AddPart(new InstallablePart());
            box.AddPart(new InstallationMarkerPart{Origin="Player",InstallationId="player:"+box.ID,RetainZone=true});return box;
        }
        Entity Stock(Entity box,string blueprint,int count=1)
        {
            var item=Factory.CreateEntity(blueprint);Assert.NotNull(item);item.GetPart<StackerPart>().StackCount=count;
            Assert.True(box.GetPart<ContainerPart>().AddItem(item));return item;
        }
        [Test] public void SelectedOwnBoxProvidesDirectMaterialsWithoutCarryingThem()
        {
            var box=Box();var input=Stock(box,"CuredLeatherOffcut",3);int tick=Clock.TickCount;
            string action=Choice(box,Prefix);Assert.AreEqual(tick,Clock.TickCount);
            Assert.True(Act(box,action));Assert.AreEqual(2,input.GetPart<StackerPart>().StackCount);
            Assert.AreSame(box,input.GetPart<PhysicsPart>().InInventory);Assert.False(Pack.Objects.Contains(input));
            Assert.AreEqual(1,Count("LeatherBindingComponent"));Assert.False(Act(box,action));
        }
        [Test] public void WorldRowDeclaresThePaidNativeCommandDispatch()
        {
            var box=Box();Stock(box,"CuredLeatherOffcut");
            var row=CellMenu(11,10).Single(a=>a.Command.StartsWith(Prefix,StringComparison.Ordinal));
            Assert.AreEqual(ActionTime.OneAction,row.Time,"The actual C-menu must route through the transaction-owning declared-action dispatcher.");
        }
        [Test] public void BoxInstructionsDescribeDirectStockWithoutReplacingItsOpenQuote()
        {
            var box=Box();Stock(box,"CuredLeatherOffcut");string command=Choice(box,Prefix);
            for(int i=0;i<3;i++)StringAssert.Contains("direct contents",UseGuides.SectionText(box,Zone,Zone.GetEntityCell(box),Actor));
            Assert.True(Act(box,command));Assert.AreEqual(1,Count("LeatherBindingComponent"));
        }
        [Test] public void RegatherForWorldDetailsRetainsOnlyAnUnchangedQuote()
        {
            var box=Box();var input=Stock(box,"CuredLeatherOffcut",2);string command=Choice(box,Prefix);
            Assert.AreEqual(command,Choice(box,Prefix),"F1 compares the highlighted action with freshly gathered rows.");
            input.GetPart<StackerPart>().StackCount=3;string changed=Choice(box,Prefix);
            Assert.AreNotEqual(command,changed);Assert.False(Act(box,command));Assert.True(Act(box,changed));Assert.AreEqual(2,input.GetPart<StackerPart>().StackCount);
        }
        [TestCase("tool")] [TestCase("extra")] [TestCase("permission")] [TestCase("box")]
        public void RegatherNeverRetainsALostRequirement(string change)
        {
            var box=Box();var cord=Stock(box,"KnotflaxCord");var binding=Stock(box,"LeatherBindingComponent");string command=Choice(box,Prefix);
            if(change=="tool")Pack.RemoveObject(Pack.Objects.Single(e=>e.BlueprintName=="Dagger"));
            if(change=="extra")box.GetPart<ContainerPart>().RemoveItem(binding);
            if(change=="permission")cord.GetPart<PreparationRecipePart>().RecipeIds="";
            if(change=="box")box.GetPart<InstallationMarkerPart>().Origin="Generated";
            Assert.False(Actions(box).Any(a=>a.Command.StartsWith(Prefix,StringComparison.Ordinal)));Assert.False(Act(box,command));Assert.AreEqual(0,Count("GuardLashingComponent"));
        }
        [Test] public void ExtraInputsComeFromThatSameBoxOnly()
        {
            var box=Box();Stock(box,"KnotflaxCord");Carry("LeatherBindingComponent");
            Assert.False(Actions(box).Any(a=>a.Command.StartsWith(Prefix,StringComparison.Ordinal)));
            Pack.RemoveObject(Pack.Objects.Single(e=>e.BlueprintName=="LeatherBindingComponent"));Stock(box,"LeatherBindingComponent");
            Assert.True(Act(box,Choice(box,Prefix)));Assert.AreEqual(1,Count("GuardLashingComponent"));Assert.IsEmpty(box.GetPart<ContainerPart>().Contents);
        }

        [TestCase("make steam condenser", "SteamCondenser", 1, 3, false)]
        [TestCase("make hearth nozzle", "HearthNozzle", 2, 2, false)]
        [TestCase("make hearth nozzle", "HearthNozzle", 2, 3, true)]
        [TestCase("make hearth nozzle", "HearthNozzle", 2, 2, true)]
        public void OwnBoxForgePaymentSpendsAndRestoresTheExactClayUnits(
            string label, string result, int quantity, int initialCount, bool interrupted)
        {
            Place("TinkersForge", 10, 11);
            var box = Box();
            var container = box.GetPart<ContainerPart>();
            var spike = Stock(box, "IronSpikeComponent");
            var clay = Stock(box, "FireClay");
            string key = Guid.NewGuid().ToString("N");
            Assert.True(ConnectedSpreadProgress.BindClay(clay, key, 0));
            var origin = clay.GetPart<ConnectedClayOriginPart>();
            var stack = clay.GetPart<StackerPart>();
            stack.StackCount = initialCount;
            origin.Units = 3;
            var packClay = Carry("FireClay");
            Assert.That(Count("FireClay"), Is.EqualTo(1));

            string action = Choice(box, Prefix, a => a.Display.StartsWith(label, StringComparison.Ordinal));
            int remaining = initialCount - quantity;
            int paidMask = quantity == 1 ? 2 : 0;
            bool sawPayment = false;
            Actor.AddPart(new After { Callback = () =>
            {
                sawPayment = true;
                Assert.That(origin.Units, Is.EqualTo(paidMask));
                Assert.That(stack.StackCount, Is.EqualTo(Math.Max(1, remaining)));
                Assert.That(container.Contents.Contains(clay), Is.EqualTo(remaining > 0));
                Assert.That(Count(result), Is.EqualTo(1));
            } });
            if (interrupted) FailAfter();

            Assert.That(Act(box, action), Is.EqualTo(!interrupted));
            Assert.True(sawPayment);
            Assert.That(stack.StackCount, Is.EqualTo(interrupted ? initialCount : Math.Max(1, remaining)));
            Assert.That(origin.Units, Is.EqualTo(interrupted ? 3 : paidMask));
            Assert.That(origin.WorldKey, Is.EqualTo(key));
            Assert.That(origin.OwnerID, Is.EqualTo(clay.ID));
            Assert.That(clay.GetPart<ConnectedClayOriginPart>(), Is.SameAs(origin));
            Assert.That(container.Contents.Contains(clay), Is.EqualTo(interrupted || remaining > 0));
            Assert.That(clay.GetPart<PhysicsPart>().InInventory, Is.SameAs(interrupted || remaining > 0 ? box : null));
            Assert.That(container.Contents.Contains(spike), Is.EqualTo(interrupted));
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == "FireClay"), Is.SameAs(packClay));
            Assert.That(Count("FireClay"), Is.EqualTo(1));
            Assert.That(Count(result), Is.EqualTo(interrupted ? 0 : 1));
        }

        [TestCase("locked")] [TestCase("handed-over")] [TestCase("loose")] [TestCase("far")] [TestCase("hidden")]
        [TestCase("no-tool")] [TestCase("town-chest")] [TestCase("duplicate-id")]
        public void AccessAndExactCustodyAreRequired(string change)
        {
            var box=Box();var input=Stock(box,"CuredLeatherOffcut");
            if(change=="locked")box.GetPart<ContainerPart>().Locked=true;
            if(change=="handed-over")box.GetPart<InstallationMarkerPart>().Origin="Generated";
            if(change=="loose")box.GetPart<InstallablePart>().State=InstallablePart.Loose;
            if(change=="far"){Zone.RemoveEntity(box);Zone.AddEntity(box,20,10);}
            if(change=="hidden")Zone.GetEntityCell(box).IsVisible=false;
            if(change=="no-tool")Pack.RemoveObject(Pack.Objects.Single(e=>e.BlueprintName=="Dagger"));
            if(change=="town-chest")box.BlueprintName="Chest";
            if(change=="duplicate-id")Stock(box,"KnotflaxCord").ID=input.ID;
            Assert.False(Actions(box).Any(a=>a.Command.StartsWith(Prefix,StringComparison.Ordinal)));
        }
        [TestCase("locked")] [TestCase("handover")] [TestCase("replace-container")]
        public void RetainedQuoteCannotBypassChangedAccess(string change)
        {
            var box=Box();Stock(box,"CuredLeatherOffcut");string action=Choice(box,Prefix);
            if(change=="locked")box.GetPart<ContainerPart>().Locked=true;
            if(change=="handover")box.GetPart<InstallationMarkerPart>().Origin="Generated";
            if(change=="replace-container"){box.RemovePart(box.GetPart<ContainerPart>());box.AddPart(new ContainerPart());}
            Assert.False(Act(box,action));Assert.AreEqual(0,Count("LeatherBindingComponent"));
        }
        public sealed class After : Part
        { public Action Callback; public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Callback?.Invoke();return true;} }
        [Test] public void FailureRestoresPaymentWithoutRewindingIndependentRemainderTransferOrInjury()
        {
            var box=Box();var input=Stock(box,"CuredLeatherOffcut",3);string action=Choice(box,Prefix);int hp=Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new After{Callback=()=>{
                box.GetPart<ContainerPart>().RemoveItem(input);Zone.AddEntity(input,13,10);
                Actor.GetStat("Hitpoints").BaseValue--;throw new InvalidOperationException("outer failure");
            }});
            Assert.False(Act(box,action));Assert.AreEqual(3,input.GetPart<StackerPart>().StackCount);
            Assert.NotNull(Zone.GetEntityCell(input));Assert.IsEmpty(box.GetPart<ContainerPart>().Contents);
            Assert.AreEqual(hp-1,Actor.GetStatValue("Hitpoints"));Assert.AreEqual(0,Count("LeatherBindingComponent"));
        }
        [Test] public void FullInputFailureRestoresTheExactOriginalBoxEntry()
        {
            var box=Box();var input=Stock(box,"CuredLeatherOffcut");string action=Choice(box,Prefix);FailAfter();
            Assert.False(Act(box,action));Assert.AreSame(input,box.GetPart<ContainerPart>().Contents.Single());
            Assert.AreSame(box,input.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(0,Count("LeatherBindingComponent"));
        }
        [Test] public void AnotherBoxCannotDispatchTheSelectedQuote()
        {
            var box=Box();Stock(box,"CuredLeatherOffcut");string action=Choice(box,Prefix);
            var other=Place("CampBox",10,11);Assert.False(Act(other,action));Assert.AreEqual(0,Count("LeatherBindingComponent"));
        }
        [TestCase("input")] [TestCase("actor")] [TestCase("tool")] [TestCase("world")]
        public void BoxIdentityCannotAliasAnotherOwner(string alias)
        {
            var box=Box();var input=Stock(box,"CuredLeatherOffcut");
            if(alias=="input")input.ID=box.ID;
            if(alias=="actor")box.ID=Actor.ID;
            if(alias=="tool")box.ID=Pack.Objects.Single(e=>e.BlueprintName=="Dagger").ID;
            if(alias=="world")Place("Chest",13,10).ID=box.ID;
            Assert.False(Actions(box).Any(a=>a.Command.StartsWith(Prefix,StringComparison.Ordinal)));
        }
        [Test] public void ALogicallyDestroyedBoxDoesNotRegainItsPaidUnit()
        {
            var box=Box();var input=Stock(box,"CuredLeatherOffcut");string action=Choice(box,Prefix);
            Actor.AddPart(new After{Callback=()=>{box.GetPart<DestructiblePart>().Gone=true;throw new InvalidOperationException("destroyed listener");}});
            Assert.False(Act(box,action));Assert.AreEqual((11,10),Zone.GetEntityPosition(input));Assert.IsEmpty(box.GetPart<ContainerPart>().Contents);
            Assert.False(Actions(box).Any(a=>a.Command.StartsWith(Prefix,StringComparison.Ordinal)));Assert.AreEqual(0,Count("LeatherBindingComponent"));
        }
        [TestCase("foreign-part")] [TestCase("overflow")]
        public void MalformedRemaindersCannotRedirectOrOverflowARefund(string mutation)
        {
            var box=Box();var input=Stock(box,"CuredLeatherOffcut",3);string action=Choice(box,Prefix);
            var part=input.GetPart<StackerPart>();var foreign=Factory.CreateEntity("SalvagedTimber");
            Actor.AddPart(new After{Callback=()=>{
                if(mutation=="foreign-part"){input.RemovePart(part);foreign.RemovePart(foreign.GetPart<StackerPart>());foreign.AddPart(part);part.StackCount=17;}
                else part.StackCount=int.MaxValue;
                throw new InvalidOperationException("changed remainder");
            }});
            Assert.False(Act(box,action));Assert.AreEqual(mutation=="foreign-part"?17:int.MaxValue,part.StackCount);Assert.AreEqual(0,Count("LeatherBindingComponent"));
        }
    }
}
