using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class StorageLabelTests : FiftyWorldFixture
    {
        const BindingFlags Hidden = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;

        static object Begin(Entity actor, Entity vessel)
        {
            var type = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.StorageLabels");
            Assert.NotNull(type);
            return type.GetMethod("Begin", Hidden).Invoke(null, new object[] { actor, vessel });
        }

        static bool Commit(object draft, StorageLabelMode mode, int color, int icon, string text)
        {
            Assert.NotNull(draft);
            return (bool)draft.GetType().GetMethod("Commit", Hidden).Invoke(draft, new object[] { mode, color, icon, text });
        }

        [TestCase(StorageLabelMode.ColorMark, 1, 0, "")]
        [TestCase(StorageLabelMode.Icon, 4, 2, "")]
        [TestCase(StorageLabelMode.Text, 3, 0, "seed kit")]
        public void ALabelIsSavedWithTheExactPhysicalVesselAndItsLoad(StorageLabelMode mode, int color, int icon, string text)
        {
            var vessel = Carry("FieldSatchel");
            var content = Factory.CreateEntity("FireClay");
            Assert.IsTrue(vessel.GetPart<ContainerPart>().AddItem(content));
            string oldName = vessel.GetDisplayName();
            int weight = HandlingService.GetWeight(vessel);
            Assert.IsTrue(Commit(Begin(Actor, vessel), mode, color, icon, text));
            Assert.AreEqual(oldName, vessel.GetDisplayName());
            Assert.AreEqual(weight, HandlingService.GetWeight(vessel));

            var restored = PartRoundTripHelper.RoundTripEntityViaTokenGraph(vessel);
            var label = restored.GetPart<StorageLabelPart>();
            Assert.AreEqual(vessel.ID, restored.ID);
            Assert.AreEqual(content.ID, restored.GetPart<ContainerPart>().Contents.Single().ID);
            Assert.AreEqual((int)mode, label.Mode);
            Assert.AreEqual(color, label.ColorIndex);
            Assert.AreEqual(icon, label.IconIndex);
            Assert.AreEqual(text, label.Text);
        }

        [Test]
        public void EqualLabelsDoNotMergeAndAnOldEditorCannotOverwriteOrClearANewLabel()
        {
            var first = Carry("FieldSatchel");
            var second = Carry("FieldSatchel");
            Assert.AreNotSame(first, second);
            var stale = Begin(Actor, first);
            Assert.IsTrue(Commit(Begin(Actor, first), StorageLabelMode.Icon, 2, 5, ""));
            Assert.IsTrue(Commit(Begin(Actor, second), StorageLabelMode.Icon, 2, 5, ""));
            Assert.IsFalse(Commit(stale, StorageLabelMode.None, 0, 0, ""));
            Assert.AreEqual(2, Actor.GetPart<InventoryPart>().Objects.Count(e => e.BlueprintName == "FieldSatchel"));
            Assert.IsTrue(Commit(Begin(Actor, first), StorageLabelMode.None, 0, 0, ""));
            Assert.IsNull(first.GetPart<StorageLabelPart>());
            Assert.NotNull(second.GetPart<StorageLabelPart>());
        }

        [Test]
        public void AnExistingNestedLoadDoesNotHideAnOtherwiseValidCosmeticLabel()
        {
            var box = Carry("CampBox");
            var satchel = Factory.CreateEntity("FieldSatchel");
            Assert.IsTrue(box.GetPart<ContainerPart>().AddItem(satchel));
            Assert.IsFalse(CarriedStorage.IsCarried(Actor, box), "nested items are handled by the standing container");
            Assert.IsTrue(CarriedStorage.TryBrowse(Actor, box, out var outer));
            Assert.AreSame(box.GetPart<ContainerPart>(), outer);
            Assert.IsTrue(Commit(Begin(Actor, box), StorageLabelMode.ColorMark, 1, 0, ""));
            Assert.IsTrue(Commit(Begin(Actor, satchel), StorageLabelMode.Icon, 2, 1, ""));
            var onlyInsideTheSatchel = Factory.CreateEntity("Dagger");
            Assert.IsTrue(satchel.GetPart<ContainerPart>().AddItem(onlyInsideTheSatchel));
            Assert.IsFalse(CarriedStorage.TryBrowse(Actor, satchel, out _));
            Assert.IsNull(Begin(Actor, onlyInsideTheSatchel));
            Assert.AreSame(satchel, outer.Contents.Single());
        }

        [Test]
        public void AVisibleLooseChildCanBeRetrievedFromALegacyNestedBox()
        {
            var box = Carry("CampBox");
            var satchel = Factory.CreateEntity("FieldSatchel");
            var dagger = Factory.CreateEntity("Dagger");
            var container = box.GetPart<ContainerPart>();
            Assert.IsTrue(container.AddItem(satchel));
            Assert.IsTrue(container.AddItem(dagger));
            Assert.IsTrue(Commit(Begin(Actor, dagger), StorageLabelMode.Text, 1, 0, "field knife"));
            int weight = Pack.GetCarriedWeight();

            Assert.IsFalse(CarriedStorage.TryWithdraw(Actor, box, satchel, Zone, out _));
            Assert.IsTrue(CarriedStorage.TryWithdraw(Actor, box, dagger, Zone, out var failure), failure);
            Assert.AreSame(satchel, container.Contents.Single());
            Assert.AreSame(Actor, dagger.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual("field knife", dagger.GetPart<StorageLabelPart>().Text);
            Assert.AreEqual(weight, Pack.GetCarriedWeight());
        }

        [Test]
        public void LabelsFollowOrdinaryItemsThroughEquipmentAndSave()
        {
            var dagger = Carry("Dagger");
            var packDraft = Begin(Actor, dagger);
            Assert.IsTrue(InventorySystem.Equip(Actor, dagger));
            Assert.IsFalse(Commit(packDraft, StorageLabelMode.ColorMark, 1, 0, ""));
            Assert.IsTrue(Commit(Begin(Actor, dagger), StorageLabelMode.Text, 2, 0, "keep dry"));
            var restored = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            var restoredDagger = restored.GetPart<InventoryPart>().EquippedItems.Values.Single(e => e.ID == dagger.ID);
            Assert.AreEqual("keep dry", restoredDagger.GetPart<StorageLabelPart>().Text);
            Assert.IsTrue(Commit(Begin(restored, restoredDagger), StorageLabelMode.Icon, 1, 0, ""));
            Assert.IsTrue(InventorySystem.UnequipItem(Actor, dagger));
            Assert.AreEqual("keep dry", dagger.GetPart<StorageLabelPart>().Text);
        }

        [Test]
        public void DirectContentsRequireTheExactOwnedStillAccessibleVessel()
        {
            var first = Carry("FieldSatchel");
            var second = Carry("FieldSatchel");
            var firstContainer = first.GetPart<ContainerPart>();
            var secondContainer = second.GetPart<ContainerPart>();
            var firstItem = Factory.CreateEntity("Dagger");
            var secondItem = Factory.CreateEntity("Dagger");
            Assert.IsTrue(firstContainer.AddItem(firstItem));
            Assert.IsTrue(secondContainer.AddItem(secondItem));

            var closedDraft = Begin(Actor, firstItem);
            firstContainer.Locked = true;
            Assert.IsNull(Begin(Actor, firstItem));
            Assert.IsFalse(Commit(closedDraft, StorageLabelMode.ColorMark, 1, 0, ""));
            firstContainer.Locked = false;
            var movedDraft = Begin(Actor, firstItem);
            Assert.IsTrue(firstContainer.RemoveItem(firstItem));
            Assert.IsTrue(secondContainer.RemoveItem(secondItem));
            Assert.IsTrue(secondContainer.AddItem(firstItem));
            Assert.IsTrue(firstContainer.AddItem(secondItem));
            Assert.IsFalse(Commit(movedDraft, StorageLabelMode.ColorMark, 1, 0, ""));
            Assert.IsTrue(Commit(Begin(Actor, firstItem), StorageLabelMode.Text, 4, 0, "second pouch"));
            Assert.IsNull(secondItem.GetPart<StorageLabelPart>());
            Assert.AreSame(firstItem, secondContainer.Contents.Single());
            Assert.AreSame(secondItem, firstContainer.Contents.Single());

            var foreign = Place("FieldSatchel");
            var foreignItem = Factory.CreateEntity("Dagger");
            Assert.IsTrue(foreign.GetPart<ContainerPart>().AddItem(foreignItem));
            Assert.IsNull(Begin(Actor, foreignItem));
        }

        [Test]
        public void DifferentLabelsSeparatePhysicalStacksAndEqualLabelsPermitOrdinaryPackMerges()
        {
            var first = Carry("PreparedLiningClay");
            Assert.IsTrue(Commit(Begin(Actor, first), StorageLabelMode.Text, 4, 0, "well lining"));
            var second = Carry("PreparedLiningClay");
            Assert.IsTrue(Commit(Begin(Actor, second), StorageLabelMode.Text, 4, 0, "rig lining"));
            var firstStack = first.GetPart<StackerPart>();
            var secondStack = second.GetPart<StackerPart>();

            Assert.IsFalse(firstStack.CanStackWith(second));
            Assert.AreEqual(0, firstStack.MergeFrom(second));
            Assert.AreEqual(1, firstStack.StackCount);
            Assert.AreEqual(1, secondStack.StackCount);
            Assert.IsTrue(Commit(Begin(Actor, second), StorageLabelMode.None, 0, 0, ""));
            Assert.IsFalse(firstStack.CanStackWith(second));
            Assert.IsTrue(Commit(Begin(Actor, second), StorageLabelMode.Text, 4, 0, "well lining"));
            Assert.IsTrue(firstStack.CanStackWith(second));
            var beforeMerge = Begin(Actor, first);
            Assert.IsTrue(Pack.RemoveObject(second));
            Assert.IsTrue(Pack.AddObject(second));
            Assert.AreEqual(2, firstStack.StackCount);
            Assert.AreEqual(0, secondStack.StackCount);
            Assert.AreSame(first, Pack.Objects.Single(item => item.BlueprintName == "PreparedLiningClay"));
            Assert.IsFalse(Commit(beforeMerge, StorageLabelMode.ColorMark, 2, 0, ""));

            var split = firstStack.SplitStack(1);
            Assert.NotNull(split);
            Assert.AreNotEqual(first.ID, split.ID);
            Assert.AreEqual(1, firstStack.StackCount);
            Assert.AreNotSame(first.GetPart<StorageLabelPart>(), split.GetPart<StorageLabelPart>());
            Assert.AreEqual("well lining", split.GetPart<StorageLabelPart>().Text);
            Assert.AreEqual("well lining", first.GetPart<StorageLabelPart>().Text);
        }

        [Test]
        public void MaterialStackLabelPreservesRealSplitAndMergeCapabilities()
        {
            var clay = Carry("PreparedLiningClay");
            var stack = clay.GetPart<StackerPart>();
            stack.StackCount = 3;
            Assert.IsTrue(Commit(Begin(Actor, clay), StorageLabelMode.Icon, 3, 2, ""));
            Assert.IsTrue(stack.CanSplit());
            var unit = stack.RemoveOne();
            Assert.NotNull(unit);
            Assert.AreEqual(2, unit.GetPart<StorageLabelPart>().IconIndex);
            Assert.IsTrue(stack.CanStackWith(unit));
            Assert.AreEqual(1, stack.MergeFrom(unit));
            Assert.AreEqual(3, stack.StackCount);
        }

        [Test]
        public void RelabellingOneCountedGroupMemberLeavesTheOtherOwnerUnchanged()
        {
            var first = Flask(2);
            var second = Flask(3);
            second.GetPart<LiquidVesselPart>().Volume = 2;
            var items = new[] { first, second };
            System.Collections.Generic.List<InventoryItemGroup> Groups()
                => InventoryItemGroups.Project(InventoryScreenData.Build(Actor).Categories.SelectMany(category => category.Items)
                    .Where(display => items.Contains(display.Item)), first, true);

            Assert.AreEqual(2, Groups().Single().Members.Count);
            Assert.IsTrue(Commit(Begin(Actor, first), StorageLabelMode.Text, 1, 0, "field water"));
            Assert.AreEqual(2, Groups().Count);
            Assert.IsNull(second.GetPart<StorageLabelPart>());
            Assert.IsTrue(Commit(Begin(Actor, second), StorageLabelMode.Text, 1, 0, "field water"));
            var grouped = Groups().Single();
            Assert.AreEqual(2, grouped.Members.Count);
            Assert.AreSame(first, grouped.Selected.Item);
        }

        [Test]
        public void AnEmptyVesselTradesAtItsOwnPriceButItsContentsCannotRideAlong()
        {
            var vessel = Carry("FieldSatchel");
            Assert.IsTrue(TradeSystem.CanBeTraded(vessel, Actor, null, "Sell"));
            Assert.IsTrue(vessel.GetPart<ContainerPart>().AddItem(Factory.CreateEntity("FireClay")));
            Assert.IsFalse(TradeSystem.CanBeTraded(vessel, Actor, null, "Sell"));
        }

        [Test]
        public void AFormerOwnerAndAnInvalidTextCannotChangeTheLabel()
        {
            var vessel = Carry("FieldSatchel");
            var draft = Begin(Actor, vessel);
            Assert.IsFalse(Commit(draft, StorageLabelMode.Text, 1, 0, "bad\nline"));
            Assert.IsFalse(Commit(draft, StorageLabelMode.Text, 1, 0, "&R markup"));
            Assert.IsFalse(Commit(draft, StorageLabelMode.Text, 1, 0, "é"));
            Assert.IsNull(vessel.GetPart<StorageLabelPart>());
            Assert.IsTrue(Actor.GetPart<InventoryPart>().RemoveObject(vessel));
            Assert.IsTrue(Zone.AddEntity(vessel, 11, 10));
            Assert.IsFalse(Commit(draft, StorageLabelMode.Text, 1, 0, "Travel"));
            Assert.IsNull(vessel.GetPart<StorageLabelPart>());
            Assert.IsNull(Begin(Actor, vessel));
        }
    }
}
