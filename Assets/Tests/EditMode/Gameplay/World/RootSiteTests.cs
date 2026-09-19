using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The other advertised routes, ER.1 (Docs/ENDING-ROUTES.md): the Root is a
    /// place. On the world map, at the stump's crown north of the Felling-Site, a
    /// tier-5 cell named "the Root": a cleft with a way down and loose tepuibone,
    /// and below it one chamber where the taproot's face is exposed, solid,
    /// reachable from the stairs, dreaming, and — in this milestone — offering
    /// nothing yet. Older saved maps grow the place; nothing else moves.
    /// </summary>
    public sealed class RootSiteTests
    {
        private EntityFactory factory; private OverworldZoneManager manager;

        [SetUp] public void SetUp() { factory = GrovelandsCompositionTests.Factory(); manager = OverworldZoneManager.CreateDetached(factory, 64); Diag.ResetAll(); MessageLog.Clear(); }

        private Zone Mouth() => manager.GetZone(RootSiteBuilder.MouthZoneID);
        private Zone Chamber() => manager.GetZone(RootSiteBuilder.ChamberZoneID);
        private static Entity Face(Zone z) => z.GetAllEntities().SingleOrDefault(e => e.HasPart<RootFacePart>());
        private static Entity Stair(Zone z, bool up) => z.GetAllEntities().SingleOrDefault(e => up ? e.HasPart<StairsUpPart>() : e.HasPart<StairsDownPart>());
        private static int Count(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = kind, Limit = 20 }).Records.Count;
        private static InventoryActionList Offered(Entity target) { var list = new InventoryActionList(); var ev = GameEvent.New("GetInventoryActions"); ev.SetParameter("Actions", (object)list); target.FireEventAndRelease(ev); return list; }
        private static IEnumerable<(int x, int y)> Around((int x, int y) p)
        {
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) if (dx != 0 || dy != 0) yield return (p.x + dx, p.y + dy);
        }

        // ════════════════════════════════════════════════════════════
        //   The place on the map
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheRoot_IsAnAuthoredPlace_AtTheStumpsCrown_AndTheFellingSiteIsUntouched()
        {
            Assert.AreEqual((3, 3), (RootSiteBuilder.WorldX, RootSiteBuilder.WorldY), "canon reserves (3,3) for the Root");
            Assert.AreEqual(BiomeType.Stump, WorldMapAuthoring.BiomeAt(RootSiteBuilder.WorldX, RootSiteBuilder.WorldY));
            var poi = manager.WorldMap.GetPOI(RootSiteBuilder.WorldX, RootSiteBuilder.WorldY);
            Assert.IsNotNull(poi, "the place is stamped on a fresh world");
            Assert.AreEqual(POIType.Root, poi.Type); Assert.AreEqual(RootSiteBuilder.SiteName, poi.Name); Assert.AreEqual(5, poi.Tier);
            // Counter-check: the circle two cells south keeps its own place.
            Assert.AreEqual(POIType.FellingSite, manager.WorldMap.GetPOI(FellingSiteBuilder.WorldX, FellingSiteBuilder.WorldY)?.Type);
            Assert.IsTrue(FellingSceneRuntime.IsActive(manager.GetZone(FellingSiteBuilder.ZoneID)), "the circle still generates");
        }

        [Test]
        public void TheMap_ShowsTheRoot_ByItsName()
        {
            var (glyph, _, name) = WorldMapZoneBuilder.GetPOIRender(manager.WorldMap.GetPOI(RootSiteBuilder.WorldX, RootSiteBuilder.WorldY));
            Assert.AreNotEqual("?", glyph, "not the unknown marker"); Assert.AreEqual(RootSiteBuilder.SiteName, name);
        }

        [Test]
        public void POITypeRoot_IsAppendedLast_SoSavedIntsStayStable()
        {
            // The POI table is saved as ints; a new member must not renumber the old ones.
            Assert.AreEqual(Enum.GetValues(typeof(POIType)).Cast<int>().Max(), (int)POIType.Root);
        }

        [Test]
        public void OlderSavedMaps_GrowTheRoot_OnRehydrate_ButNeverDisplaceAnotherPlace()
        {
            var map = new WorldMap(64);
            map.SetPOI(RootSiteBuilder.WorldX, RootSiteBuilder.WorldY, null);
            map.RehydrateRoot();
            Assert.AreEqual(POIType.Root, map.GetPOI(RootSiteBuilder.WorldX, RootSiteBuilder.WorldY)?.Type, "an empty cell grows the place");
            // Counter-check: a saved place of another kind is never displaced.
            var other = new PointOfInterest(POIType.Lair, "a lair", tier: 2);
            map.SetPOI(RootSiteBuilder.WorldX, RootSiteBuilder.WorldY, other);
            map.RehydrateRoot();
            Assert.AreSame(other, map.GetPOI(RootSiteBuilder.WorldX, RootSiteBuilder.WorldY));
        }

        // ════════════════════════════════════════════════════════════
        //   The mouth (z=0)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheMouth_HasOneWayDown_WithItsConnection_AndLooseTepuiboneBesideIt()
        {
            var mouth = Mouth();
            var down = Stair(mouth, false); Assert.IsNotNull(down, "exactly one way down");
            var at = mouth.GetEntityPosition(down);
            var conns = manager.GetConnectionsTo(RootSiteBuilder.ChamberZoneID, "StairsDown");
            Assert.AreEqual(1, conns.Count, "the way down is registered to the chamber");
            Assert.AreEqual(RootSiteBuilder.MouthZoneID, conns[0].SourceZoneID);
            Assert.AreEqual((at.x, at.y), (conns[0].SourceX, conns[0].SourceY));
            int loose = mouth.GetAllEntities().Count(e => e.BlueprintName == "Tepuibone"
                && Math.Abs(mouth.GetEntityPosition(e).x - at.x) <= RootSiteBuilder.LooseRadius && Math.Abs(mouth.GetEntityPosition(e).y - at.y) <= RootSiteBuilder.LooseRadius);
            Assert.GreaterOrEqual(loose, RootSiteBuilder.LooseTepuibone, "tepuibone lies loose at the cleft");
            Assert.IsNull(Face(mouth), "the face is below, not at the mouth");
            var reach = ConnectivityBuilder.FloodFill(mouth, at.x, at.y); int open = 0;
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) if (reach[x, y]) open++;
            Assert.Greater(open, 200, "the way down is not walled in");
        }

        // ════════════════════════════════════════════════════════════
        //   The chamber (z=1)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheChamber_ExposesOneSolidFace_ReachableFromTheStairs_AndNothingGoesFurtherDown()
        {
            var chamber = Chamber();
            var face = Face(chamber); Assert.IsNotNull(face, "one face");
            Assert.AreEqual(RootSiteBuilder.FaceBlueprint, face.BlueprintName); Assert.AreEqual(RootSiteBuilder.FaceId, face.ID, "a fixed id for the picker");
            var f = chamber.GetEntityPosition(face);
            Assert.IsTrue(chamber.GetCell(f.x, f.y).BlocksMovement(), "the face is solid");
            var up = Stair(chamber, true); Assert.IsNotNull(up, "a way up");
            var u = chamber.GetEntityPosition(up); var reach = ConnectivityBuilder.FloodFill(chamber, u.x, u.y);
            Assert.IsTrue(Around(f).Any(n => chamber.GetCell(n.x, n.y) != null && reach[n.x, n.y]), "a cell beside the face is reachable from the stairs");
            Assert.IsNull(Stair(chamber, false), "nothing goes further down than the Root");
            StringAssert.Contains("dream", face.GetPart<ExaminablePart>()?.Text ?? "", "it dreams; what it dreams is not said");
        }

        [Test]
        public void TheFace_OffersOnlyTheRoutesEnactments_AndNothingOnceAnEndingIsEnacted()
        {
            // ER.1 made the face a place; ER.2 made it a choice. Whatever it offers is a
            // route's enactment, and once any ending is enacted it offers nothing.
            var face = Face(Chamber()); var player = factory.CreateEntity("Player"); CavesOfOoo.Storylets.StoryletPart.LocalPlayer = player;
            try
            {
                var offered = Offered(face).Actions.Where(a => a.Command.StartsWith("Ending")).ToList();
                Assert.IsNotEmpty(offered, "the face is a choice now"); Assert.IsTrue(offered.All(a => EndingRoutes.IsWorldCommand(a.Command)));
                player.SetIntProperty(EndingSpine.EndingProperty, EndingSpine.VesselPath);
                Assert.IsFalse(Offered(face).Actions.Any(a => a.Command.StartsWith("Ending")), "nothing once an ending is enacted");
            }
            finally { CavesOfOoo.Storylets.StoryletPart.LocalPlayer = null; }
        }

        [Test]
        public void BelowTheChamber_IsOrdinaryUnderground_WithNoSecondFace()
        {
            Assert.IsNull(Face(manager.GetZone(WorldMap.ToZoneID(RootSiteBuilder.WorldX, RootSiteBuilder.WorldY, 2))));
        }

        [Test]
        public void ALateralArrival_StillHasAWayUp_BecauseTheMouthGeneratesFirst()
        {
            var fresh = OverworldZoneManager.CreateDetached(factory, 65);
            var chamber = fresh.GetZone(RootSiteBuilder.ChamberZoneID);
            Assert.IsTrue(fresh.CachedZones.ContainsKey(RootSiteBuilder.MouthZoneID), "top-down: the mouth exists before the chamber");
            var up = Stair(chamber, true); Assert.IsNotNull(up);
            var down = Stair(fresh.GetZone(RootSiteBuilder.MouthZoneID), false); Assert.IsNotNull(down);
            Assert.AreEqual(fresh.GetZone(RootSiteBuilder.MouthZoneID).GetEntityPosition(down), chamber.GetEntityPosition(up), "the stairs meet");
        }

        [TestCase(1)] [TestCase(913)]
        public void OtherSeeds_StillExposeTheFace(int seed)
        {
            var m = OverworldZoneManager.CreateDetached(factory, seed);
            var chamber = m.GetZone(RootSiteBuilder.ChamberZoneID);
            Assert.IsNotNull(Face(chamber), $"seed {seed}"); Assert.IsNotNull(Stair(chamber, true), $"seed {seed}: a way up");
        }

        // ════════════════════════════════════════════════════════════
        //   Observability and idempotence
        // ════════════════════════════════════════════════════════════

        [Test]
        public void FreshChamber_EmitsOnePlacedRecord_AndARevisitNeverDuplicates()
        {
            Diag.ResetAll();
            var chamber = Chamber();
            Assert.AreEqual(1, Count("RootPlaced")); Assert.AreEqual(0, Count("RootRefused"), "the happy path never also refuses");
            Assert.AreSame(chamber, Chamber());
            Assert.AreEqual(1, Chamber().GetAllEntities().Count(e => e.HasPart<RootFacePart>()));
            Assert.AreEqual(1, Count("RootPlaced"), "a revisit places nothing");
        }
    }
}
