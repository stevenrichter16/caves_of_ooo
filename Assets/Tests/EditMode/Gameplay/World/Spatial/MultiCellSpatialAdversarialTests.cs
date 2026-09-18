using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MultiCellLoadObservationPart : Part
    {
        public string ZoneId;
        public int X, Y;
        public static bool AfterSawBody, FinalSawBody;
        public override void OnAfterLoad(SaveReader reader)
            => AfterSawBody = reader.FindZone(ZoneId)?.GetOccupants(X, Y).Contains(ParentEntity) == true;
        public override void FinalizeLoad(SaveReader reader)
            => FinalSawBody = reader.FindZone(ZoneId)?.GetOccupants(X, Y).Contains(ParentEntity) == true;
    }

    /// <summary>Taxonomy probes: lifecycle, rollback, malformed restoration,
    /// identity aliasing, query stability and observable refusal paths.</summary>
    public sealed class MultiCellSpatialAdversarialTests
    {
        private const string Square = "0,0;1,0;0,1;1,1";
        private static Entity Body(string raw = Square, bool solid = true)
            => MultiCellSpatialTests.Body(raw, solid);
        private static void Place(Zone zone, Entity entity, int x = 10, int y = 10)
            => Assert.IsTrue(zone.AddEntity(entity, x, y));
        private static int References(Zone zone, Entity entity)
        {
            int count = 0;
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                    count += zone.GetCell(x, y).Objects.Count(e => e == entity);
            return count;
        }
        private static Diag.Entry[] Records(string kind, string category = "event")
            => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 100 }).Records.ToArray();

        [Test] public void Adversarial_AddingBodyToPlacedOwnerImmediatelyUpdatesPhysicalQueries()
        {
            var zone = new Zone(); var owner = Body(null); Place(zone, owner);
            owner.AddPart(new SpatialFootprintPart { CellsRaw = Square });
            Assert.IsTrue(zone.GetOccupants(11, 11).Contains(owner));
            Assert.IsTrue(zone.GetCell(11, 11).BlocksMovement());
            Assert.AreEqual(1, References(zone, owner));
            Assert.IsFalse(zone.GetOccupants(12, 11).Contains(owner));
        }

        [Test] public void Adversarial_BlockedDynamicAttachmentRollsBackPartsParentAndIndex()
        {
            var zone = new Zone(); var owner = Body(null); var blocker = Body(null);
            Place(zone, owner); Place(zone, blocker, 11, 11);
            var part = new SpatialFootprintPart { CellsRaw = Square }; int version = zone.EntityVersion;
            Assert.Throws<InvalidOperationException>(() => owner.AddPart(part));
            Assert.IsFalse(owner.HasPart<SpatialFootprintPart>()); Assert.IsNull(part.ParentEntity);
            Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(1, zone.GetOccupiedCells(owner).Count);
            Assert.IsFalse(zone.GetOccupants(11, 10).Contains(owner));
            zone.RemoveEntity(blocker); owner.AddPart(part);
            Assert.IsTrue(zone.GetOccupants(11, 11).Contains(owner));
        }

        [Test] public void Adversarial_MalformedDynamicAttachmentCannotHideAnExistingOwner()
        {
            var zone = new Zone(); var owner = Body(null); Place(zone, owner);
            var part = new SpatialFootprintPart { CellsRaw = "0,0;bad" };
            Assert.Throws<InvalidOperationException>(() => owner.AddPart(part));
            Assert.IsTrue(zone.GetOccupants(10, 10).Contains(owner));
            Assert.IsFalse(owner.HasPart<SpatialFootprintPart>()); Assert.IsNull(part.ParentEntity);
            part.CellsRaw = "0,0;1,0"; owner.AddPart(part);
            Assert.IsTrue(zone.GetOccupants(11, 10).Contains(owner));
        }

        [Test] public void Adversarial_RemovingBodyRestoresOnlyItsCanonicalAnchor()
        {
            var zone = new Zone(); var owner = Body(); Place(zone, owner);
            Assert.IsTrue(owner.RemovePart(owner.GetPart<SpatialFootprintPart>()));
            Assert.AreEqual(1, zone.GetOccupiedCells(owner).Count);
            Assert.IsTrue(zone.GetOccupants(10, 10).Contains(owner));
            Assert.IsFalse(zone.GetOccupants(11, 11).Contains(owner));
            Assert.IsTrue(zone.MoveEntity(owner, 12, 10));
            Assert.IsFalse(zone.GetOccupants(10, 10).Contains(owner));
            Assert.IsFalse(zone.GetOccupants(11, 10).Contains(owner));
        }

        [Test] public void Adversarial_RemovingHollowBodyRefusesOccupiedAnchorWithoutLosingOriginalBody()
        {
            var zone = new Zone(); var owner = Body("1,0;1,1"); var blocker = Body(null);
            Place(zone, owner); Place(zone, blocker); var part = owner.GetPart<SpatialFootprintPart>();
            int version = zone.EntityVersion;
            Assert.IsFalse(owner.RemovePart(part));
            Assert.AreSame(part, owner.GetPart<SpatialFootprintPart>()); Assert.AreSame(owner, part.ParentEntity);
            Assert.AreEqual(version, zone.EntityVersion); Assert.IsTrue(zone.GetOccupants(11, 11).Contains(owner));
            Assert.IsFalse(zone.GetOccupants(10, 10).Contains(owner));
            zone.RemoveEntity(blocker); Assert.IsTrue(owner.RemovePart(part));
            Assert.IsTrue(zone.GetOccupants(10, 10).Contains(owner));
            Assert.IsFalse(zone.GetOccupants(11, 11).Contains(owner));
        }

        [Test] public void Adversarial_SecondFootprintPartCannotCreateTwoAuthoritativeShapes()
        {
            var zone = new Zone(); var owner = Body(); Place(zone, owner);
            var second = new SpatialFootprintPart { CellsRaw = "0,0;2,0" };
            Assert.Throws<InvalidOperationException>(() => owner.AddPart(second));
            Assert.AreEqual(1, owner.Parts.Count(p => p is SpatialFootprintPart));
            Assert.IsNull(second.ParentEntity); Assert.IsTrue(zone.GetOccupants(11, 11).Contains(owner));
            Assert.IsFalse(zone.GetOccupants(12, 10).Contains(owner));
        }

        [Test] public void Adversarial_UnrelatedPartMutationDoesNotChangeBodyOrCanonicalMembership()
        {
            var zone = new Zone(); var owner = Body(); Place(zone, owner);
            var part = new DestructiblePart(); owner.AddPart(part); Assert.IsTrue(owner.RemovePart(part));
            Assert.AreEqual(4, zone.GetOccupiedCells(owner).Count); Assert.AreEqual(1, References(zone, owner));
            Assert.IsTrue(zone.GetOccupants(11, 11).Contains(owner));
        }

        [Test] public void Adversarial_ShapeChangeClearsOldBodyWhileCapturedCoordinatesStayStable()
        {
            var zone = new Zone(); var owner = Body(); Place(zone, owner);
            var before = zone.GetOccupiedCells(owner); var liveOldCorner = zone.GetOccupants(11, 11);
            Assert.IsTrue(zone.TryChangeFootprint(owner, "0,0;-1,0"));
            Assert.AreEqual(4, before.Count); Assert.AreSame(zone.GetCell(11, 11), before[3]);
            Assert.IsFalse(liveOldCorner.Contains(owner)); Assert.AreEqual(2, zone.GetOccupiedCells(owner).Count);
            Assert.IsTrue(zone.GetOccupants(9, 10).Contains(owner)); Assert.AreEqual(1, References(zone, owner));
        }

        [TestCase(false)] [TestCase(true)]
        public void Adversarial_RejectedShapeChangePreservesCommittedRawShapeAndEveryOldCell(bool malformed)
        {
            var zone = new Zone(); var owner = Body(); Place(zone, owner);
            Place(zone, Body(null), 12, 10); int version = zone.EntityVersion;
            string raw = malformed ? "0,0;1,0;garbage" : "0,0;1,0;2,0";
            Assert.IsFalse(zone.TryChangeFootprint(owner, raw));
            Assert.AreEqual(Square, owner.GetPart<SpatialFootprintPart>().CellsRaw);
            Assert.AreEqual(version, zone.EntityVersion); Assert.IsTrue(zone.GetOccupants(11, 11).Contains(owner));
            Assert.IsFalse(zone.GetOccupants(12, 10).Contains(owner));
        }

        [Test] public void Adversarial_DuplicateSavedReferencesRepairToFirstXThenYAnchorWithoutGhosts()
        {
            var zone = new Zone(); var owner = Body();
            zone.GetCell(10, 15).Objects.Add(owner); zone.GetCell(11, 5).Objects.Add(owner);
            zone.RebuildEntityCellsFromCells();
            Assert.AreEqual((10, 15), zone.GetEntityPosition(owner)); Assert.AreEqual(1, References(zone, owner));
            Assert.IsTrue(zone.GetOccupants(11, 16).Contains(owner));
            Assert.IsFalse(zone.GetOccupants(12, 6).Contains(owner));
            Assert.IsTrue(zone.RemoveEntity(owner));
            Assert.IsFalse(zone.GetOccupants(11, 16).Contains(owner));
            Assert.IsFalse(zone.GetOccupants(12, 6).Contains(owner));
        }

        [Test] public void Adversarial_DuplicateReferenceInsideOneSavedCellIsNotDuplicateOccupancy()
        {
            var zone = new Zone(); var owner = Body();
            zone.GetCell(10, 10).Objects.Add(owner); zone.GetCell(10, 10).Objects.Add(owner);
            zone.RebuildEntityCellsFromCells(); zone.RebuildEntityCellsFromCells();
            Assert.AreEqual(1, References(zone, owner)); Assert.AreEqual(1, zone.GetOccupants(10, 10).Count);
            Assert.AreEqual(1, zone.GetOccupants(11, 11).Count); zone.RemoveEntity(owner);
            Assert.AreEqual(0, zone.GetOccupants(10, 10).Count); Assert.AreEqual(0, zone.GetOccupants(11, 11).Count);
        }

        [TestCase(false)] [TestCase(true)]
        public void Adversarial_InvalidSavedShapeRejectsBeforeClearingPreviouslyUsableIndex(bool outside)
        {
            var zone = new Zone(); var valid = Body(); Place(zone, valid);
            var malformed = Body(outside ? Square : "0,0;bad");
            zone.GetCell(outside ? 79 : 20, outside ? 24 : 10).Objects.Add(malformed);
            int version = zone.EntityVersion;
            Assert.Throws<InvalidDataException>(() => zone.RebuildEntityCellsFromCells());
            Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual((10, 10), zone.GetEntityPosition(valid));
            Assert.IsTrue(zone.GetOccupants(11, 11).Contains(valid));
            Assert.IsNull(zone.GetEntityCell(malformed));
        }

        [Test] public void Adversarial_RebuildAfterCanonicalDeletionClearsBodyAndLocationOwnership()
        {
            var zone = new Zone(); var other = new Zone(); var owner = Body(); Place(zone, owner);
            zone.GetCell(10, 10).Objects.Remove(owner); zone.RebuildEntityCellsFromCells();
            Assert.IsNull(zone.GetEntityCell(owner)); Assert.IsFalse(zone.GetOccupants(11, 11).Contains(owner));
            Assert.IsTrue(other.AddEntity(owner, 20, 10));
            Assert.IsTrue(other.GetOccupants(21, 11).Contains(owner));
        }

        [Test] public void Adversarial_UnresolvedSavedPlaceholderCanAcquireBodyBeforeFinalRebuild()
        {
            var zone = new Zone(); var placeholder = new Entity();
            zone.GetCell(10, 10).Objects.Add(placeholder); zone.RebuildEntityCellsFromCells();
            placeholder.AddPart(new SpatialFootprintPart { CellsRaw = Square });
            Assert.IsTrue(zone.GetOccupants(11, 11).Contains(placeholder));
            zone.RebuildEntityCellsFromCells(); Assert.AreEqual(1, zone.GetOccupants(11, 11).Count);
        }

        [Test] public void Adversarial_SaveLoadHooksObserveCompleteRemoteBodyAndCanonicalOwnerAlias()
        {
            bool beforeAfter = MultiCellLoadObservationPart.AfterSawBody, beforeFinal = MultiCellLoadObservationPart.FinalSawBody;
            try
            {
                using (var fixture = new DecodeIsolationFixture(aura: false))
                {
                    var zone = fixture.Saved.ZoneManager.ActiveZone; var player = fixture.Saved.Player;
                    player.AddPart(new SpatialFootprintPart { CellsRaw = Square });
                    Assert.IsTrue(zone.TryChangeFootprint(player, "0,0;1,0;2,0"));
                    var anchor = zone.GetEntityCell(player);
                    player.AddPart(new MultiCellLoadObservationPart { ZoneId = zone.ZoneID, X = anchor.X + 2, Y = anchor.Y });
                    MultiCellLoadObservationPart.AfterSawBody = MultiCellLoadObservationPart.FinalSawBody = false;
                    var loaded = DecodeIsolationFixture.Decode(fixture.EncodeSaved()); var restored = loaded.ZoneManager.ActiveZone;
                    Assert.IsTrue(MultiCellLoadObservationPart.AfterSawBody, "OnAfterLoad must not see the pre-body placeholder index");
                    Assert.IsTrue(MultiCellLoadObservationPart.FinalSawBody);
                    Assert.AreEqual(1, References(restored, loaded.Player)); Assert.AreEqual(3, restored.GetOccupiedCells(loaded.Player).Count);
                    Assert.IsTrue(restored.GetOccupants(anchor.X + 2, anchor.Y).Contains(loaded.Player));
                    Assert.AreSame(loaded.Player, loaded.TurnManager.CurrentActor);
                }
            }
            finally { MultiCellLoadObservationPart.AfterSawBody = beforeAfter; MultiCellLoadObservationPart.FinalSawBody = beforeFinal; }
        }

        [Test] public void Adversarial_SaveAfterDestructionDoesNotResurrectRemoteBody()
        {
            using (var fixture = new DecodeIsolationFixture(aura: false))
            {
                var zone = fixture.Saved.ZoneManager.ActiveZone; var owner = Body(); owner.AddPart(new DestructiblePart());
                Place(zone, owner, 20, 10); string id = owner.ID;
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(owner, fixture.Saved.Player, zone, "adversarial"));
                var loaded = DecodeIsolationFixture.Decode(fixture.EncodeSaved()); var restored = loaded.ZoneManager.ActiveZone;
                Assert.IsFalse(restored.GetAllEntities().Any(e => e.ID == id));
                Assert.IsTrue(restored.GetCell(21, 11).IsEmpty());
                Assert.IsTrue(restored.AddEntity(Body(), 20, 10));
            }
        }

        [Test] public void Adversarial_CloneOwnsIndependentBodyAndNoSourceZoneBackReference()
        {
            var zone = new Zone(); var other = new Zone(); var owner = Body(); Place(zone, owner);
            var clone = owner.CloneForStack(); Assert.IsTrue(other.AddEntity(clone, 20, 10));
            Assert.IsTrue(other.TryChangeFootprint(clone, "0,0;2,0"));
            Assert.AreEqual(Square, owner.GetPart<SpatialFootprintPart>().CellsRaw);
            Assert.IsTrue(zone.GetOccupants(11, 11).Contains(owner)); Assert.IsFalse(other.GetOccupants(21, 11).Contains(clone));
            Assert.IsTrue(other.GetOccupants(22, 10).Contains(clone));
        }

        [Test] public void Adversarial_SameZoneTransferUsesWholeBodyValidationAndLeavesBlockedVersionUnchanged()
        {
            var zone = new Zone(); var owner = Body(); var blocker = Body(null);
            Place(zone, owner); Place(zone, blocker, 12, 11); int version = zone.EntityVersion;
            Assert.IsFalse(zone.TryTransferEntityTo(owner, zone, 11, 10));
            Assert.AreEqual(version, zone.EntityVersion); Assert.IsTrue(zone.GetOccupants(10, 11).Contains(owner));
            zone.RemoveEntity(blocker); Assert.IsTrue(zone.TryTransferEntityTo(owner, zone, 11, 10));
            Assert.AreEqual(1, References(zone, owner)); Assert.IsFalse(zone.GetOccupants(10, 11).Contains(owner));
        }

        [Test] public void Adversarial_WrongSourceTransferCannotStealAnotherZonesOwner()
        {
            var source = new Zone(); var wrong = new Zone(); var destination = new Zone(); var owner = Body(); Place(source, owner);
            int version = source.EntityVersion;
            Assert.IsFalse(wrong.TryTransferEntityTo(owner, destination, 20, 10));
            Assert.IsFalse(destination.AddEntity(owner, 20, 10));
            Assert.AreEqual(version, source.EntityVersion); Assert.IsTrue(source.GetOccupants(11, 11).Contains(owner));
            Assert.AreEqual(0, destination.EntityCount);
        }

        [Test] public void Adversarial_LiveOccupantViewChangesButExplicitSnapshotSurvivesRemoval()
        {
            var zone = new Zone(); var owner = Body(); Place(zone, owner);
            var view = zone.GetOccupants(11, 11); var snapshot = view.ToArray(); zone.RemoveEntity(owner);
            Assert.AreEqual(0, view.Count); Assert.IsFalse(view.Contains(owner));
            Assert.AreEqual(1, snapshot.Length); Assert.AreSame(owner, snapshot[0]);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var ignored = view[0]; });
        }

        [Test] public void Adversarial_RenderPrioritySelectsHighestVisibleOwnerAcrossMixedAnchorAndBodyLists()
        {
            var zone = new Zone(); var body = Body(solid: false); var item = Body(null, false);
            body.GetPart<RenderPart>().RenderLayer = 1; item.GetPart<RenderPart>().RenderLayer = 9;
            Place(zone, body); Place(zone, item, 11, 11); Assert.AreSame(item, zone.GetCell(11, 11).GetTopVisibleObject());
            item.GetPart<RenderPart>().Visible = false; Assert.AreSame(body, zone.GetCell(11, 11).GetTopVisibleObject());
            Assert.AreEqual(2, zone.GetOccupants(11, 11).Count);
        }

        [Test] public void Adversarial_DifferentOwnersWithSameExternalIdNeverCollapseIntoOneBody()
        {
            var zone = new Zone(); var first = Body(solid: false); var second = Body(solid: false); second.ID = first.ID;
            Place(zone, first); Place(zone, second); Assert.AreEqual(2, zone.GetOccupants(11, 11).Count);
            Assert.IsTrue(zone.RemoveEntity(first)); Assert.AreEqual(1, zone.GetOccupants(11, 11).Count);
            Assert.IsTrue(zone.GetOccupants(11, 11).Contains(second)); Assert.IsFalse(zone.GetOccupants(11, 11).Contains(first));
        }


        [Test] public void Adversarial_RenderCallbacksObserveOnlyCompleteCommittedMembership()
        {
            var zone = new Zone(); var owner = Body();
            var previous = ZoneRenderHooks.CellDirtyCallback;
            bool removing = false, sawPartial = false; int callbacks = 0;
            try
            {
                ZoneRenderHooks.CellDirtyCallback = (x, y, reason) =>
                {
                    if (reason == null || !reason.StartsWith("Footprint", StringComparison.Ordinal)) return;
                    callbacks++;
                    int count = 0;
                    for (int px = 10; px <= 11; px++) for (int py = 10; py <= 11; py++)
                        if (zone.GetOccupants(px, py).Contains(owner)) count++;
                    sawPartial |= count != (removing ? 0 : 4);
                    sawPartial |= (zone.GetEntityCell(owner) != null) == removing;
                };
                Place(zone, owner); Assert.Greater(callbacks, 0);
                Assert.IsFalse(sawPartial, "Placement listeners must see the complete canonical owner and body");
                callbacks = 0; removing = true;
                Assert.IsTrue(zone.RemoveEntity(owner)); Assert.Greater(callbacks, 0);
                Assert.IsFalse(sawPartial, "Removal listeners must see no canonical owner or old body cells");
            }
            finally { ZoneRenderHooks.CellDirtyCallback = previous; }
        }

        [Test] public void Adversarial_PlacementDiagnosticsDistinguishRejectionFromCommittedOwner()
        {
            var zone = new Zone("diagnostic-zone"); var owner = Body(); var blocker = Body(null); Place(zone, blocker, 11, 11);
            Diag.ResetAll(); Assert.IsFalse(zone.AddEntity(owner, 10, 10));
            Assert.AreEqual(1, Records("FootprintRejected").Length); Assert.AreEqual(0, Records("FootprintPlaced").Length);
            Assert.AreEqual(owner.ID, Records("FootprintRejected")[0].ActorId);
            zone.RemoveEntity(blocker); Assert.IsTrue(zone.AddEntity(owner, 10, 10));
            Assert.AreEqual(1, Records("FootprintPlaced").Length);
            StringAssert.Contains("\"cells\":4", Records("FootprintPlaced")[0].PayloadJson);
        }

        [Test] public void Adversarial_RemovalDiagnosticsFireOnceForOneCanonicalOwner()
        {
            var zone = new Zone(); var owner = Body(); Place(zone, owner); Diag.ResetAll();
            Assert.IsTrue(zone.RemoveEntity(owner)); Assert.IsFalse(zone.RemoveEntity(owner));
            var records = Records("FootprintRemoved"); Assert.AreEqual(1, records.Length);
            Assert.AreEqual(owner.ID, records[0].ActorId); StringAssert.Contains("\"cells\":4", records[0].PayloadJson);
        }

        [Test] public void Adversarial_ShapeChangeDiagnosticsDistinguishFailedAndCommittedShapes()
        {
            var zone = new Zone(); var owner = Body(); Place(zone, owner); Diag.ResetAll();
            Assert.IsFalse(zone.TryChangeFootprint(owner, "bad"));
            Assert.AreEqual(1, Records("FootprintChangeRejected").Length); Assert.AreEqual(0, Records("FootprintChanged").Length);
            Assert.IsTrue(zone.TryChangeFootprint(owner, "0,0;2,0"));
            Assert.AreEqual(1, Records("FootprintChanged").Length);
            Assert.AreEqual(owner.ID, Records("FootprintChanged")[0].ActorId);
        }

        [TestCase(false)] [TestCase(true)]
        public void Adversarial_TransitionEmitsOneFinalOutcomeWithoutCandidateProbeSpam(bool eligible)
        {
            var source = new Zone("Overworld.3.7.0"); var destination = new Zone("Overworld.4.7.0"); var owner = Body(); Place(source, owner);
            if (!eligible)
                for (int x = 0; x <= 11; x++) for (int y = 0; y < Zone.Height; y++)
                    Place(destination, Body(null), x, y);
            var manager = new ZoneManager(null, 28); manager.CachedZones[source.ZoneID] = source; manager.CachedZones[destination.ZoneID] = destination;
            Diag.ResetAll();
            var result = ZoneTransitionSystem.TransitionPlayer(owner, source, TransitionDirection.East, 10, 10, manager, null);
            Assert.AreEqual(eligible, result.Success);
            Assert.AreEqual(eligible ? 1 : 0, Records("FootprintArrivalSucceeded", "worldmap").Length);
            Assert.AreEqual(eligible ? 0 : 1, Records("FootprintArrivalRejected", "worldmap").Length);
            var record = Records(eligible ? "FootprintArrivalSucceeded" : "FootprintArrivalRejected", "worldmap")[0];
            Assert.AreEqual(owner.ID, record.ActorId); StringAssert.Contains(source.ZoneID, record.PayloadJson);
            StringAssert.Contains(destination.ZoneID, record.PayloadJson);
        }
    }
}
