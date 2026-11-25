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
        private static bool IsBoxScreeningRow(InventoryAction row) => !row.IsDisabled
            && row.Command?.StartsWith(Prefix, StringComparison.Ordinal) == true
            && row.Display?.IndexOf("screen", StringComparison.OrdinalIgnoreCase) >= 0;

        [Test]
        public void InstalledBoxScreensOnlyItsExactStoredPacketAndRetainsTheHeldSieve()
        {
            var box = Box();
            var stored = Stock(box, "DryClaySpoil", 2);
            var carried = Carry("DryClaySpoil");
            var sieve = Carry("HandSieve");
            int weight = Pack.GetCarriedWeight();
            int turn = Clock.TickCount;
            var row = CellMenu(11, 10).Single(IsBoxScreeningRow);
            Assert.That(row.Time, Is.EqualTo(ActionTime.OneAction));
            UseGuides.SectionText(box, Zone, Zone.GetEntityCell(box), Actor);
            Assert.That(CellMenu(11, 10).Single(IsBoxScreeningRow).Command, Is.EqualTo(row.Command));
            Assert.That(Clock.TickCount, Is.EqualTo(turn));

            Assert.That(ChooseRow(row, 11, 10), Is.True);
            Assert.That(Act(box, row.Command), Is.False, "An already spent quote cannot screen a second stored batch.");
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stored));
            Assert.That(stored.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(stored.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == "DryClaySpoil"), Is.SameAs(carried));
            Assert.That(carried.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(Pack.Objects.Contains(sieve), Is.True);
            Assert.That(Count("FireClay"), Is.EqualTo(2));
            Assert.That(Count("CoarseGrit"), Is.EqualTo(1));
            Assert.That(Pack.Objects.Single(e => e.BlueprintName == "CoarseGrit").GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.That(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "FireClay" || e.BlueprintName == "CoarseGrit"), Is.False);
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(weight + 3));
        }

        [Test]
        public void CarriedScreeningDoesNotUseSpoilInTheNearbyBox()
        {
            var box = Box();
            var stored = Stock(box, "DryClaySpoil");
            var carried = Carry("DryClaySpoil");
            var sieve = Carry("HandSieve");
            int weight = Pack.GetCarriedWeight();
            string action = Choice(carried, "MaterialProcess|screen_size_grades|");

            Assert.That(Act(carried, action), Is.True);
            Assert.That(box.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stored));
            Assert.That(stored.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.That(stored.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(Pack.Objects.Contains(carried), Is.False);
            Assert.That(Pack.Objects.Contains(sieve), Is.True);
            Assert.That(Count("FireClay"), Is.EqualTo(2));
            Assert.That(Count("CoarseGrit"), Is.EqualTo(1));
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(weight));
        }

        [TestCase("sealed")]
        [TestCase("nested")]
        [TestCase("offsite")]
        public void PreviouslyQuotedBoxScreeningCannotReachSealedNestedOrDistantStock(string change)
        {
            var box = Box();
            var stored = Stock(box, "DryClaySpoil");
            var sieve = Carry("HandSieve");
            var container = box.GetPart<ContainerPart>();
            var action = Actions(box).Single(IsBoxScreeningRow).Command;
            int turn = Clock.TickCount;
            var actualOwner = box;
            if (change == "sealed") container.Locked = true;
            if (change == "nested")
            {
                actualOwner = Factory.CreateEntity("WovenBasket");
                Assert.That(container.RemoveItem(stored), Is.True);
                Assert.That(actualOwner.GetPart<ContainerPart>().AddItem(stored), Is.True);
                Assert.That(container.AddItem(actualOwner), Is.True);
            }
            if (change == "offsite") Assert.That(Zone.MoveEntity(Actor, 20, 10), Is.True);

            Assert.That(Act(box, action), Is.False);
            Assert.That(Actions(box).Any(IsBoxScreeningRow), Is.False);
            Assert.That(stored.GetPart<PhysicsPart>().InInventory, Is.SameAs(actualOwner));
            Assert.That(actualOwner.GetPart<ContainerPart>().Contents.Single(), Is.SameAs(stored));
            Assert.That(stored.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(Count("FireClay"), Is.Zero);
            Assert.That(Count("CoarseGrit"), Is.Zero);
            Assert.That(Pack.Objects.Contains(sieve), Is.True);
            Assert.That(Clock.TickCount, Is.EqualTo(turn));
        }

        [TestCase(0)]
        [TestCase(2)]
        public void ScreeningFromStoredStockNeedsThreePoundsOfFreeSpace(int insufficientSpace)
        {
            var box = Box();
            var stored = Stock(box, "DryClaySpoil");
            var sieve = Carry("HandSieve");
            var container = box.GetPart<ContainerPart>();
            int weight = Pack.GetCarriedWeight();
            Pack.MaxWeight = weight + 3;
            var action = Actions(box).Single(IsBoxScreeningRow).Command;
            Pack.MaxWeight = weight + insufficientSpace;

            Assert.That(Act(box, action), Is.False);
            Assert.That(container.Contents.Single(), Is.SameAs(stored));
            Assert.That(stored.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(Count("FireClay"), Is.Zero);
            Assert.That(Count("CoarseGrit"), Is.Zero);
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(weight));

            Pack.MaxWeight = weight + 3;
            Assert.That(Act(box, Actions(box).Single(IsBoxScreeningRow).Command), Is.True);
            Assert.That(container.Contents, Is.Empty);
            Assert.That(Pack.Objects.Contains(sieve), Is.True);
            Assert.That(Count("FireClay"), Is.EqualTo(2));
            Assert.That(Count("CoarseGrit"), Is.EqualTo(1));
            Assert.That(Pack.GetCarriedWeight(), Is.EqualTo(Pack.MaxWeight));
        }

        [Test]
        public void FatalInterruptionReturnsTheExactPacketToTheIntactBoxAndLeavesOrdinaryDeathDrops()
        {
            var box = Box();
            var stored = Stock(box, "DryClaySpoil");
            var tool = Carry("HandSieve");
            var dagger = Pack.Objects.Single(e => e.BlueprintName == "Dagger");
            var container = box.GetPart<ContainerPart>();
            var drop = Zone.GetEntityCell(Actor);
            var action = Actions(box).Single(IsBoxScreeningRow).Command;
            bool sawProvisionalWork = false;
            Actor.AddPart(new After { Callback = () =>
            {
                sawProvisionalWork = !container.Contents.Contains(stored) && Count("FireClay") == 2 && Count("CoarseGrit") == 1;
                Actor.GetStat("Hitpoints").BaseValue = 0;
                CombatSystem.HandleDeath(Actor, null, Zone);
            } });

            Assert.That(Act(box, action), Is.False);
            Assert.That(sawProvisionalWork, Is.True);
            Assert.That(CombatSystem.IsDeathHandled(Actor), Is.True);
            Assert.That(container.Contents.Single(), Is.SameAs(stored));
            Assert.That(stored.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.That(stored.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(Zone.GetEntityCell(dagger), Is.SameAs(drop));
            Assert.That(Zone.GetEntityCell(tool), Is.SameAs(drop));
            Assert.That(Pack.Objects.Contains(dagger) || Pack.Objects.Contains(tool), Is.False);
            Assert.That(Count("FireClay"), Is.Zero);
            Assert.That(Count("CoarseGrit"), Is.Zero);
        }

        [Test]
        public void BrokenBoxSpillsTheExactLastPacketWithoutRestoringTheBoxOrProducts()
        {
            var box = Box();
            var stored = Stock(box, "DryClaySpoil");
            var unrelated = Stock(box, "KnotflaxCord");
            var tool = Carry("HandSieve");
            var container = box.GetPart<ContainerPart>();
            var boxCell = Zone.GetEntityCell(box);
            var action = Actions(box).Single(IsBoxScreeningRow).Command;
            var verdict = DestroyVerdict.NoTarget;
            bool sawProvisionalWork = false;
            Actor.AddPart(new After { Callback = () =>
            {
                sawProvisionalWork = !container.Contents.Contains(stored) && Count("FireClay") == 2 && Count("CoarseGrit") == 1;
                verdict = DestructionSystem.Destroy(box, null, Zone, "screening-interruption");
            } });

            Assert.That(Act(box, action), Is.False);
            Assert.That(sawProvisionalWork, Is.True);
            Assert.That(verdict, Is.EqualTo(DestroyVerdict.Destroyed));
            Assert.That(Zone.GetEntityCell(box), Is.Null);
            Assert.That(container.Contents, Is.Empty);
            Assert.That(Zone.GetEntityCell(stored), Is.SameAs(boxCell));
            Assert.That(Zone.GetEntityCell(unrelated), Is.SameAs(boxCell));
            Assert.That(boxCell.Objects.Single(e => e.BlueprintName == "DryClaySpoil"), Is.SameAs(stored));
            Assert.That(stored.GetPart<PhysicsPart>().InInventory, Is.Null);
            Assert.That(stored.GetPart<StackerPart>().StackCount, Is.EqualTo(1));
            Assert.That(Pack.Objects.Contains(tool), Is.True);
            Assert.That(Count("FireClay"), Is.Zero);
            Assert.That(Count("CoarseGrit"), Is.Zero);
        }

        [Test]
        public void ChangedScreenedRecipientKeepsItsNewCustodyWithoutReturningTheStoredPacket()
        {
            var box = Box();
            var stored = Stock(box, "DryClaySpoil");
            var sieve = Carry("HandSieve");
            var oldClay = Carry("FireClay");
            Assert.True(ConnectedSpreadProgress.BindClay(oldClay, Guid.NewGuid().ToString("N"), 0));
            var cell = Zone.GetCell(10, 12);
            var action = Actions(box).Single(IsBoxScreeningRow).Command;
            Actor.AddPart(new After { Callback = () =>
            {
                Assert.That(oldClay.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
                Assert.True(Pack.RemoveObject(oldClay));
                Assert.True(Zone.AddEntity(oldClay, cell.X, cell.Y));
                oldClay.GetPart<PhysicsPart>().Weight = 2;
            } });

            Assert.That(Act(box, action), Is.False);
            Assert.That(box.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(stored.GetPart<PhysicsPart>().InInventory, Is.Null);
            Assert.That(Zone.GetEntityCell(stored), Is.Null);
            Assert.That(Zone.GetEntityCell(oldClay), Is.SameAs(cell));
            Assert.That(oldClay.GetPart<StackerPart>().StackCount, Is.EqualTo(3));
            Assert.That(oldClay.GetPart<ConnectedClayOriginPart>().Units, Is.EqualTo(1));
            Assert.That(Count("CoarseGrit"), Is.Zero);
            Assert.That(Pack.Objects.Contains(sieve), Is.True);
        }

        [TestCase("same-box-entry")]
        [TestCase("replacement-box-entry")]
        [TestCase("replacement-pack-entry")]
        [TestCase("replacement-box-container")]
        public void LateReappearanceOfPaidPacketRejectsOutputsWithoutAddingAnAlias(string change)
        {
            var box = Box();
            var stored = Stock(box, "DryClaySpoil");
            Carry("HandSieve");
            var container = box.GetPart<ContainerPart>();
            var action = Actions(box).Single(IsBoxScreeningRow).Command;
            var replacement = change == "same-box-entry" ? stored : Factory.CreateEntity("DryClaySpoil");
            replacement.ID = stored.ID;
            var current = change == "replacement-box-container" ? new ContainerPart() : container;
            var destination = change == "replacement-pack-entry" ? Pack.Objects : current.Contents;
            Actor.AddPart(new After { Callback = () =>
            {
                Assert.That(container.Contents, Is.Empty);
                Assert.That(Count("FireClay"), Is.EqualTo(2));
                if (current != container)
                {
                    box.RemovePart(container);
                    box.AddPart(current);
                }
                destination.Add(replacement);
            } });

            Assert.That(Act(box, action), Is.False);
            Assert.That(Count("FireClay"), Is.Zero);
            Assert.That(Count("CoarseGrit"), Is.Zero);
            Assert.That(destination.Single(e => e.ID == stored.ID), Is.SameAs(replacement));
            Assert.That(Pack.Objects.Concat(current.Contents).Count(e => e.ID == stored.ID), Is.EqualTo(1));
            Assert.That(Zone.GetReadOnlyEntities().Any(e => e.ID == stored.ID), Is.False);
        }

        [Test]
        public void InventoryReplacementStillReturnsThePaidPacketToAnIntactBox()
        {
            var box = Box();
            var stored = Stock(box, "DryClaySpoil");
            Carry("HandSieve");
            var oldPack = Pack;
            var container = box.GetPart<ContainerPart>();
            var action = Actions(box).Single(IsBoxScreeningRow).Command;
            Actor.AddPart(new After { Callback = () =>
            {
                Assert.That(container.Contents, Is.Empty);
                Assert.That(Count("FireClay"), Is.EqualTo(2));
                Actor.RemovePart(oldPack);
            } });

            Assert.That(Act(box, action), Is.False);
            Assert.That(Actor.GetPart<InventoryPart>(), Is.Null);
            Assert.That(container.Contents.Single(), Is.SameAs(stored));
            Assert.That(stored.GetPart<PhysicsPart>().InInventory, Is.SameAs(box));
            Assert.That(oldPack.Objects.Any(e => e.BlueprintName == "FireClay" || e.BlueprintName == "CoarseGrit"), Is.False);
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
