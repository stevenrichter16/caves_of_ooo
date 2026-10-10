using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SteamDeckTerrainSourceOrderingTests
    {
        private static Entity Owner(Zone zone, int x, bool source)
        {
            var owner = new Entity();
            if (source) owner.AddPart(new TileStateSourcePart { Coating = "water", CoatingTurns = 3 });
            zone.AddEntity(owner, x, 1); return owner;
        }

        [TestCase(false)] [TestCase(true)]
        public void CallbackAttachmentToExistingOwnerHonorsOriginalVisitPosition(bool later)
        {
            var zone = new Zone(); Entity a, b;
            if (later) { a = Owner(zone, 9, true); b = Owner(zone, 1, false); }
            else { b = Owner(zone, 1, false); a = Owner(zone, 9, true); }
            zone.GetReadOnlyEntitiesWithPart<TileStateSourcePart>();
            bool attached = false;
            zone.TileState.OnCellChanged = (x, y) =>
            {
                if (attached) return; attached = true;
                b.AddPart(new TileStateSourcePart { Coating = "oil", CoatingTurns = 3 });
            };
            ZoneTileStateSystem.SeedTerrainSources(zone);
            Assert.AreEqual(later, zone.TileState.HasCoating(1, 1, "oil"),
                "The old full-owner snapshot saw new Parts only on owners it had not reached yet.");
            ZoneTileStateSystem.SeedTerrainSources(zone); Assert.IsTrue(zone.TileState.HasCoating(1, 1, "oil"));
        }

        [Test]
        public void CallbackSpawnedOwnerWaitsUntilNextPass()
        {
            var zone = new Zone(); Owner(zone, 1, true); Entity spawned = null;
            zone.TileState.OnCellChanged = (x, y) => { if (spawned == null) spawned = Owner(zone, 2, true); };
            ZoneTileStateSystem.SeedTerrainSources(zone); Assert.NotNull(spawned);
            Assert.IsFalse(zone.TileState.HasCoating(2, 1, "water"));
            ZoneTileStateSystem.SeedTerrainSources(zone); Assert.IsTrue(zone.TileState.HasCoating(2, 1, "water"));
        }

        [Test]
        public void SourcePartReattachmentDoesNotReorderExistingOwners()
        {
            var zone = new Zone(); var first = Owner(zone, 9, true); Owner(zone, 1, true);
            zone.GetReadOnlyEntitiesWithPart<TileStateSourcePart>();
            var part = first.GetPart<TileStateSourcePart>(); first.RemovePart(part); first.AddPart(part);
            var order = new List<int>(); zone.TileState.OnCellChanged = (x, y) => order.Add(x);
            ZoneTileStateSystem.SeedTerrainSources(zone);
            CollectionAssert.AreEqual(new[] { 9, 1 }, order, "Source Part order is not owner enumeration order.");
        }

        [Test]
        public void CallbackRemovedOwnerDoesNotSeedAfterRemoval()
        {
            var zone = new Zone(); Owner(zone, 1, true); var removed = Owner(zone, 2, true);
            zone.TileState.OnCellChanged = (x, y) => { if (x == 1) zone.RemoveEntity(removed); };
            ZoneTileStateSystem.SeedTerrainSources(zone); Assert.IsFalse(zone.TileState.HasCoating(2, 1, "water"));
        }

        [Test]
        public void CallbackReattachmentToEarlierOwnerDoesNotRepeatIt()
        {
            var zone = new Zone(); var first = Owner(zone, 1, true); Owner(zone, 2, true);
            var order = new List<int>();
            zone.TileState.OnCellChanged = (x, y) =>
            {
                order.Add(x); if (x != 2) return;
                var part = first.GetPart<TileStateSourcePart>(); first.RemovePart(part); first.AddPart(part);
            };
            ZoneTileStateSystem.SeedTerrainSources(zone); CollectionAssert.AreEqual(new[] { 1, 2 }, order);
        }

        [Test]
        public void CallbackRemovedAndReaddedOwnerKeepsCapturedVisitPosition()
        {
            var zone = new Zone(); Owner(zone, 1, true);
            var readded = Owner(zone, 2, false); Owner(zone, 3, true);
            var order = new List<int>();
            zone.TileState.OnCellChanged = (x, y) =>
            {
                order.Add(x);
                if (x != 1) return;
                zone.RemoveEntity(readded);
                Owner(zone, 9, true); // May reuse the removed dictionary slot.
                readded.AddPart(new TileStateSourcePart { Coating = "oil", CoatingTurns = 3 });
                zone.AddEntity(readded, 4, 1);
            };
            ZoneTileStateSystem.SeedTerrainSources(zone);
            CollectionAssert.AreEqual(new[] { 1, 4, 3 }, order,
                "The initial owner snapshot retains an existing owner's position across remove/readd.");
            Assert.IsFalse(zone.TileState.HasCoating(9, 1, "water"));
        }

        [Test]
        public void ZeroSourceZoneRemainsQuietAndNoticesLaterAttachment()
        {
            var zone = new Zone(); var owner = Owner(zone, 1, false);
            int writes = 0; zone.TileState.OnCellChanged = (x, y) => writes++;
            ZoneTileStateSystem.SeedTerrainSources(zone);
            ZoneTileStateSystem.SeedTerrainSources(zone);
            Assert.AreEqual(0, writes);
            owner.AddPart(new TileStateSourcePart { Coating = "oil", CoatingTurns = 3 });
            ZoneTileStateSystem.SeedTerrainSources(zone);
            Assert.AreEqual(1, writes);
            Assert.IsTrue(zone.TileState.HasCoating(1, 1, "oil"));
        }

        [Test]
        public void OwnerOrderSnapshotIsReusedForMovementAndReplacedForMembership()
        {
            var method = typeof(Zone).GetMethod("GetEntityOrderSnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, "Source passes need cached exact owner order, not unconditional whole-zone scans.");
            var zone = new Zone(); var first = Owner(zone, 1, true);
            object before = method.Invoke(zone, null); Assert.AreSame(before, method.Invoke(zone, null));
            zone.MoveEntity(first, 2, 1); Assert.AreSame(before, method.Invoke(zone, null));
            var added = Owner(zone, 3, false); object afterAdd = method.Invoke(zone, null); Assert.AreNotSame(before, afterAdd);
            zone.RemoveEntity(added); object afterRemove = method.Invoke(zone, null); Assert.AreNotSame(afterAdd, afterRemove);
            zone.RebuildEntityCellsFromCells(); Assert.AreNotSame(afterRemove, method.Invoke(zone, null));
        }
    }
}
