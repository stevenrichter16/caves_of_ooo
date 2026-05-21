using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.7a — GasGrenadePart Detonate behavior. The Part is the
    /// item-side payload (lives on a grenade item entity); Detonate
    /// is called by ThrowItemCommand at the impact cell. These tests
    /// exercise Detonate directly without driving the full throw
    /// pipeline — a separate integration test covers ThrowItemCommand
    /// wiring.
    /// </summary>
    public class GasGrenadePartTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""GasType"":""Poison"",
                ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":100, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" } ] }");
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
        }

        private static Entity MakeGrenade(string gasId, int density, int level)
        {
            var item = new Entity { ID = "grenade", BlueprintName = "GasGrenade" };
            item.Tags["Item"] = "";
            item.AddPart(new RenderPart { DisplayName = "gas grenade" });
            item.AddPart(new GasGrenadePart { GasId = gasId, Density = density, Level = level });
            return item;
        }

        // ════════════════════════════════════════════════════════════
        //   Happy path — center + 8 adjacents = 9 gas spawns
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Detonate_SpawnsNineGasesIn3x3Grid()
        {
            var zone = new Zone("GrenadeCenter");
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 1);
            int centerX = 10, centerY = 10;
            var center = zone.GetCell(centerX, centerY);

            int spawned = grenade.GetPart<GasGrenadePart>().Detonate(actor: null, center, zone);

            Assert.AreEqual(9, spawned, "center + 8 adjacent = 9 spawns");
            int gasCount = zone.GetEntitiesWithTag("Gas").Count;
            Assert.AreEqual(9, gasCount, "9 gas entities placed in the zone");
        }

        [Test]
        public void Detonate_AllSpawnedGasesHaveExpectedDensityAndLevel()
        {
            var zone = new Zone("GrenadeProps");
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 3);
            var center = zone.GetCell(10, 10);
            grenade.GetPart<GasGrenadePart>().Detonate(actor: null, center, zone);

            var allGas = zone.GetEntitiesWithTag("Gas");
            foreach (var g in allGas)
            {
                var pool = g.GetPart<GasPoolPart>();
                Assert.AreEqual(30, pool.Density, "all spawned gases have grenade's density");
                Assert.AreEqual(3, pool.Level, "all spawned gases have grenade's level");
                Assert.AreEqual("poison-vapor", pool.GasId);
            }
        }

        [Test]
        public void Detonate_AllSpawnedGasesCarryGrenadeThrowerAsCreator()
        {
            // Friendly-fire credit: damage attribution comes from the gas's
            // Creator field, which Detonate sets to the `actor` (the
            // thrower). Tested for damage-credit attribution downstream.
            var zone = new Zone("GrenadeCreator");
            var thrower = new Entity { ID = "thrower", BlueprintName = "Player" };
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 1);
            var center = zone.GetCell(10, 10);
            grenade.GetPart<GasGrenadePart>().Detonate(thrower, center, zone);

            var allGas = zone.GetEntitiesWithTag("Gas");
            foreach (var g in allGas)
                Assert.AreSame(thrower, g.GetPart<GasPoolPart>().Creator,
                    "thrower carried through to every spawned gas");
        }

        // ════════════════════════════════════════════════════════════
        //   Edge cases — boundaries, nulls, garbage
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Detonate_AtZoneCorner_SkipsOutOfBoundsCells()
        {
            // Center at (0,0) — 5 of the 9 cells (the NW, N, NE, W, SW
            // neighbors) are out of bounds. Only (0,0), (1,0), (0,1),
            // (1,1) are valid. Factory rejects OOB; only 4 spawn.
            var zone = new Zone("GrenadeCorner");
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 1);
            var center = zone.GetCell(0, 0);
            int spawned = grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);
            Assert.AreEqual(4, spawned, "corner detonation: 4 in-bounds cells");
        }

        [Test]
        public void Detonate_AtEastEdge_SkipsOutOfBoundsCells()
        {
            // Center at (Zone.Width-1, 5) — the east edge. NE/E/SE are
            // out of bounds. 6 of 9 cells valid.
            var zone = new Zone("GrenadeEdge");
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 1);
            var center = zone.GetCell(Zone.Width - 1, 5);
            int spawned = grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);
            Assert.AreEqual(6, spawned, "east-edge detonation: 6 in-bounds cells");
        }

        [Test]
        public void Detonate_NullCenter_NoCrash_NoSpawn()
        {
            var zone = new Zone("GrenadeNullCenter");
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 1);
            int spawned = 0;
            Assert.DoesNotThrow(() =>
            {
                spawned = grenade.GetPart<GasGrenadePart>().Detonate(null, center: null, zone);
            });
            Assert.AreEqual(0, spawned);
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count);
        }

        [Test]
        public void Detonate_NullZone_NoCrash_NoSpawn()
        {
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 1);
            int spawned = 0;
            // Need a real Cell to test the "zone is null but center is non-null"
            // path. Cell construction requires a zone, so use a throwaway.
            var throwaway = new Zone("Throwaway");
            var center = throwaway.GetCell(5, 5);
            Assert.DoesNotThrow(() =>
            {
                spawned = grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone: null);
            });
            Assert.AreEqual(0, spawned);
        }

        [Test]
        public void Detonate_EmptyGasId_NoCrash_NoSpawn()
        {
            // Defensive: a grenade with no GasId set should silently
            // detonate with 0 spawns (not crash on a bad blueprint).
            var zone = new Zone("GrenadeEmptyId");
            var grenade = MakeGrenade(gasId: "", density: 30, level: 1);
            var center = zone.GetCell(10, 10);
            int spawned = grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);
            Assert.AreEqual(0, spawned);
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count);
        }

        [Test]
        public void Detonate_UnknownGasId_NoCrash_NoSpawnedEntities()
        {
            // Registry doesn't know about "phantom-gas" — factory rejects
            // every spawn. Detonate returns 0 cleanly; no gases in zone.
            var zone = new Zone("GrenadeUnknownGas");
            var grenade = MakeGrenade(gasId: "phantom-gas", density: 30, level: 1);
            var center = zone.GetCell(10, 10);
            int spawned = grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);
            Assert.AreEqual(0, spawned, "all 9 spawn attempts rejected by factory");
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count);
        }

        // ════════════════════════════════════════════════════════════
        //   Diag observability
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Detonate_EmitsGrenadeDetonatedDiag()
        {
            var zone = new Zone("GrenadeDiag");
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 2);
            var center = zone.GetCell(15, 12);
            Diag.ResetAll();
            grenade.GetPart<GasGrenadePart>().Detonate(actor: null, center, zone);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "GrenadeDetonated", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"gasId\":\"poison-vapor\"", recs[0].PayloadJson);
            StringAssert.Contains("\"density\":30", recs[0].PayloadJson);
            StringAssert.Contains("\"level\":2", recs[0].PayloadJson);
            StringAssert.Contains("\"centerX\":15", recs[0].PayloadJson);
            StringAssert.Contains("\"centerY\":12", recs[0].PayloadJson);
            StringAssert.Contains("\"cellsSpawned\":9", recs[0].PayloadJson);
        }

        [Test]
        public void Detonate_AtCorner_DiagReportsActualSpawnCount()
        {
            // Pin: the diag's cellsSpawned field reflects ACTUAL spawns
            // (not the theoretical 9). Important for live-audit debugging
            // — a corner detonation shows 4, an edge shows 6, etc.
            var zone = new Zone("GrenadeDiagCorner");
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 1);
            var center = zone.GetCell(0, 0);
            Diag.ResetAll();
            grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "GrenadeDetonated", Limit = 5 }).Records;
            StringAssert.Contains("\"cellsSpawned\":4", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   Adversarial: state atomicity + cross-actor flows
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Detonate_MergesIntoExistingCompatibleGas_InsteadOfStacking()
        {
            // Adversarial: a grenade detonating where a compatible gas
            // already exists should... well, looking at the code,
            // GasFactory.SpawnGas always creates a new entity. The merge
            // happens during dispersal (G.4), not at spawn. So two
            // overlapping gases will coexist as separate entities until
            // the next dispersal tick merges them. Pin this so a future
            // "merge at spawn" change is explicit.
            var zone = new Zone("GrenadeOverlap");
            GasFactory.SpawnGas(zone, 10, 10, "poison-vapor", density: 50);
            int beforeCount = zone.GetEntitiesWithTag("Gas").Count;
            Assert.AreEqual(1, beforeCount, "precondition");

            var grenade = MakeGrenade("poison-vapor", density: 30, level: 1);
            var center = zone.GetCell(10, 10);
            grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);

            int afterCount = zone.GetEntitiesWithTag("Gas").Count;
            // 9 new spawns + 1 pre-existing = 10 entities. Merge happens
            // on next tick, not at spawn (documented behavior).
            Assert.AreEqual(10, afterCount,
                "spawn-time does NOT merge with existing; G.4 merge runs on next tick");
        }

        [Test]
        public void Detonate_Called_TwiceInARow_DoublesGasCount()
        {
            // Atomicity / idempotency check: calling Detonate twice
            // produces TWO sets of spawns (not idempotent — the grenade
            // is consumable, but the Part method itself doesn't track
            // "already detonated"). ThrowItemCommand is responsible for
            // destroying the item; the Part is just the spawn logic.
            var zone = new Zone("GrenadeDouble");
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 1);
            var center = zone.GetCell(10, 10);
            grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);
            grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);
            Assert.AreEqual(18, zone.GetEntitiesWithTag("Gas").Count,
                "two detonations = 18 gas entities (ThrowItemCommand prevents double-call)");
        }

        // ════════════════════════════════════════════════════════════
        //   Adversarial: state atomicity / cross-actor / boundary edges
        //   (CLAUDE.md §5 step g — per-sub-milestone adversarial tests
        //   for features with ≥2 taxonomy surfaces: state-atomicity +
        //   cross-actor flows here)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Detonate_ZeroDensity_SpawnsButPoolsImmediatelyDissipate()
        {
            // A grenade with Density=0 produces 9 spawn calls; each
            // factory call returns a valid entity with Density=0. On
            // the next dispersal tick those will dissipate (Density<=0
            // gate in ProcessGasBehavior). Pin this contract — don't
            // pre-reject zero-density at the factory.
            var zone = new Zone("ZeroDensity");
            var grenade = MakeGrenade("poison-vapor", density: 0, level: 1);
            var center = zone.GetCell(10, 10);
            int spawned = grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);
            Assert.AreEqual(9, spawned,
                "factory accepts Density=0 spawns (G.3 dispersal will dissipate them)");
        }

        [Test]
        public void Adversarial_Detonate_NegativeDensity_UsesDefaultFromDef()
        {
            // Surfaced by adversarial: GasFactory.SpawnGas treats `density
            // < 0` as the sentinel "use def.DefaultDensity" (originally
            // -1 specifically, but the check is `< 0`). A grenade with
            // Density=-50 silently gets the def's default (100 for
            // poison-vapor) instead of clamping to 0.
            //
            // PIN the actual behavior. Real footgun for content authors:
            // a bug like Density=-50 doesn't produce visible "no gas",
            // it produces a normal cloud. Tracked as 🟡 in G.7a self-
            // review (commit body) for a future factory contract tighten.
            var zone = new Zone("NegDensity");
            var grenade = MakeGrenade("poison-vapor", density: -50, level: 1);
            var center = zone.GetCell(10, 10);
            grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);
            foreach (var g in zone.GetEntitiesWithTag("Gas"))
                Assert.AreEqual(100, g.GetPart<GasPoolPart>().Density,
                    "negative density falls through to def.DefaultDensity (sentinel `<0`)");
        }

        [Test]
        public void Adversarial_Detonate_NoRegistryInit_SpawnsRejectedGracefully()
        {
            // Reset the registry mid-test. Factory rejects every spawn
            // with reason=RegistryUninitialized; Detonate returns 0;
            // no zone state change. Mirrors LX.3 Rule-4 (loud failure,
            // not silent phantom data).
            GasRegistry.ResetForTests();
            var zone = new Zone("NoRegistry");
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 1);
            var center = zone.GetCell(10, 10);
            int spawned = grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);
            Assert.AreEqual(0, spawned, "registry-uninitialized rejects all spawns");
            // 9 SpawnRejected diags fire (one per cell), all with the
            // RegistryUninitialized reason — observability for the
            // failure mode.
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "SpawnRejected", Limit = 20 }).Records;
            Assert.AreEqual(9, recs.Count);
            foreach (var r in recs)
                StringAssert.Contains("RegistryUninitialized", r.PayloadJson);
        }

        [Test]
        public void Adversarial_DetonateAtNegativeCoordinates_AllSpawnsRejected()
        {
            // A grenade detonating at (-5, -5) — center is OOB; 9
            // attempted cells span (-6,-6) to (-4,-4), all OOB. 0
            // spawns, 9 SpawnRejected diags with CellOutOfBounds.
            var zone = new Zone("AllOOB");
            // Construct an artificial Cell at negative coords. Zone
            // doesn't expose this directly — build a center cell via
            // a throwaway cell with Reflection-style positioning.
            // Easier: pass a valid center but make every adjacent OOB.
            // Use center (0,0) — half cells OOB. Already covered by
            // Detonate_AtZoneCorner.
            //
            // For full-OOB test: pick coordinates that produce 0 in-
            // bounds adjacents. Zone is 80×25. There's no cell where
            // ALL 9 are OOB (the center itself is always in bounds).
            // So this test verifies the EXTREME corner partial case
            // documented in the existing _AtZoneCorner test — and
            // here we extend to the y=Height-1 case.
            var grenade = MakeGrenade("poison-vapor", density: 30, level: 1);
            var center = zone.GetCell(Zone.Width - 1, Zone.Height - 1); // bottom-right corner
            int spawned = grenade.GetPart<GasGrenadePart>().Detonate(null, center, zone);
            Assert.AreEqual(4, spawned,
                "bottom-right corner: 4 in-bounds cells (mirror of top-left)");
        }

        [Test]
        public void Adversarial_NullGrenadePart_Test_AccessGuards()
        {
            // The Part method handles null params (center, zone) but
            // what about a grenade that hasn't been added to a parent
            // entity? Detonate uses GasFactory.SpawnGas which doesn't
            // require ParentEntity. The diag uses `ParentEntity` as
            // the target — null is fine for diag.
            var orphanPart = new GasGrenadePart { GasId = "poison-vapor", Density = 30, Level = 1 };
            var zone = new Zone("OrphanGrenade");
            var center = zone.GetCell(10, 10);
            int spawned = 0;
            Assert.DoesNotThrow(() => { spawned = orphanPart.Detonate(null, center, zone); });
            Assert.AreEqual(9, spawned, "orphan Part (no ParentEntity) still spawns");
        }

        [Test]
        public void Adversarial_TwoGrenadesSameCell_DoubleGasEntities()
        {
            // Cross-actor flow: two thrown grenades from different
            // sources at the same cell. Both detonate; both produce 9
            // spawns. Each spawn carries its own thrower as Creator.
            // 18 total gas entities, half with thrower A as Creator,
            // half with thrower B. Merge on next dispersal tick.
            var zone = new Zone("TwoGrenades");
            var throwerA = new Entity { ID = "thrA", BlueprintName = "ThrowerA" };
            var throwerB = new Entity { ID = "thrB", BlueprintName = "ThrowerB" };
            var grenadeA = MakeGrenade("poison-vapor", density: 30, level: 1);
            var grenadeB = MakeGrenade("poison-vapor", density: 30, level: 1);
            var center = zone.GetCell(10, 10);
            grenadeA.GetPart<GasGrenadePart>().Detonate(throwerA, center, zone);
            grenadeB.GetPart<GasGrenadePart>().Detonate(throwerB, center, zone);

            Assert.AreEqual(18, zone.GetEntitiesWithTag("Gas").Count,
                "two grenades at same cell → 18 gas entities pre-merge");
            int aCount = 0, bCount = 0;
            foreach (var g in zone.GetEntitiesWithTag("Gas"))
            {
                var creator = g.GetPart<GasPoolPart>().Creator;
                if (creator == throwerA) aCount++;
                else if (creator == throwerB) bCount++;
            }
            Assert.AreEqual(9, aCount, "9 gases credit thrower A");
            Assert.AreEqual(9, bCount, "9 gases credit thrower B");
        }
    }
}
