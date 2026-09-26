using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DensityContainerOpenProbe : Part
    {
        public int Opens;
        public override string Name => "DensityContainerOpenProbe";
        public override bool HandleEvent(GameEvent e) { if (e.ID == "OpenContainer") Opens++; return true; }
    }
    public class DensityContainerLockAuthorityTests
    {
        DensityLootTestScope scope; Entity actor, chest; Zone zone;
        [SetUp] public void Setup()
        {
            scope = new DensityLootTestScope(); actor = scope.Factory.CreateEntity("Player"); chest = scope.Factory.CreateEntity("LockedChest");
            zone = new Zone("lock-authority"); zone.AddEntity(actor, 10, 10); zone.AddEntity(chest, 11, 10);
        }
        [TearDown] public void Cleanup() => scope.Dispose();
        void SetLocks(bool real, bool legacy) { chest.GetPart<LockPart>().IsLocked = real; chest.GetPart<ContainerPart>().Locked = legacy; }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void EitherLockAuthorityBlocksActualOpenEvent(bool real, bool legacy)
        {
            SetLocks(real, legacy); var probe = new DensityContainerOpenProbe(); actor.AddPart(probe);
            var e = GameEvent.New("InventoryAction"); e.SetParameter("Actor", (object)actor); e.SetParameter("Zone", (object)zone); e.SetParameter("Command", "OpenContainer");
            chest.FireEventAndRelease(e); Assert.AreEqual(real || legacy ? 0 : 1, probe.Opens);
            Assert.AreEqual(real, chest.GetPart<LockPart>().IsLocked); Assert.AreEqual(legacy, chest.GetPart<ContainerPart>().Locked);
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void EitherLockAuthorityBlocksTakeWithoutOwnershipMutation(bool real, bool legacy)
        {
            SetLocks(real, legacy); var item = scope.Factory.CreateEntity("Dagger"); Assert.IsTrue(chest.GetPart<ContainerPart>().AddItem(item));
            Assert.AreEqual(!real && !legacy, InventorySystem.TakeFromContainer(actor, chest, item));
            Assert.AreSame(real || legacy ? chest : actor, item.GetPart<PhysicsPart>().InInventory);
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void EitherLockAuthorityBlocksTakeAllWithoutOwnershipMutation(bool real, bool legacy)
        {
            SetLocks(real, legacy); var item = scope.Factory.CreateEntity("Dagger"); Assert.IsTrue(chest.GetPart<ContainerPart>().AddItem(item));
            Assert.AreEqual(real || legacy ? 0 : 1, InventorySystem.TakeAllFromContainer(actor, chest));
            Assert.AreSame(real || legacy ? chest : actor, item.GetPart<PhysicsPart>().InInventory);
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void EitherLockAuthorityBlocksPutWithoutOwnershipMutation(bool real, bool legacy)
        {
            SetLocks(real, legacy); var item = scope.Factory.CreateEntity("Dagger"); Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(item));
            Assert.AreEqual(!real && !legacy, InventorySystem.PutInContainer(actor, chest, item));
            Assert.AreSame(real || legacy ? actor : chest, item.GetPart<PhysicsPart>().InInventory);
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void MenuDoesNotOfferOpenThroughEitherLockOrDuplicateUnlock(bool real, bool legacy)
        {
            SetLocks(real, legacy); var list = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions"); e.SetParameter("Actor", (object)actor); e.SetParameter("Actions", (object)list); chest.FireEventAndRelease(e);
            Assert.AreEqual(real || legacy ? 0 : 1, list.Actions.Count(a => a.Command == "OpenContainer"));
            Assert.AreEqual(real || legacy ? 1 : 0, list.Actions.Count(a => a.Command == "Unlock"));
        }
    }
}
