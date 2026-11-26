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
            Assert.IsTrue(Commit(Begin(Actor, box), StorageLabelMode.ColorMark, 1, 0, ""));
            Assert.AreSame(satchel, box.GetPart<ContainerPart>().Contents.Single());
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
