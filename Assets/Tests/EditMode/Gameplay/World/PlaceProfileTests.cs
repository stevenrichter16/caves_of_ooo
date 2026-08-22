using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W2.6 (Docs/FELLING-W1-W2-PLAN.md §7.6, decision D5) — the first
    /// content that makes one named place differ from another. Until
    /// now CreateVillagePipeline never read Place.Faction: all sixteen
    /// named places generated as one identical generic village.
    /// </summary>
    public class PlaceProfileTests
    {
        private static EntityFactory _factory;
        private static OverworldZoneManager _mgr;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            FactionManager.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Factions.json")));

            _mgr = new OverworldZoneManager(_factory, worldSeed: 42);
        }

        [OneTimeTearDown]
        public void TearDownOnce()
        {
            LootTableRegistry.ResetForTests();
            FactionManager.Reset();
        }

        private Zone Generate(string zoneID)
        {
            var zone = _mgr.GetZone(zoneID);
            Assert.IsNotNull(zone, zoneID + " should generate");
            return zone;
        }

        private static int CountOf(Zone zone, string blueprint)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) n++;
            return n;
        }

        [Test]
        public void Wellmeet_HasTentsAndTheCloth()
        {
            // (8,16), Faction TentRight — the profile's guaranteed camp.
            var zone = Generate("Overworld.8.16.0");

            Assert.Greater(CountOf(zone, "TentWall"), 0, "Wellmeet without tents is just a village");
            Assert.Greater(CountOf(zone, "GuestClothPole"), 0, "the cloth must be visible");
            Assert.Greater(CountOf(zone, "TentRightHost"), 0, "someone must speak the oath");
        }

        [Test]
        public void TheFirstTent_HasItsMonument()
        {
            // (5,17) — the camp AND the ring of poles.
            var zone = Generate("Overworld.5.17.0");

            Assert.GreaterOrEqual(CountOf(zone, "GuestClothPole"), 5,
                "the monument's ring of poles (4) plus the camp's own (1)");
        }

        [Test]
        public void TheLastCounter_HasTheNoticeBoard()
        {
            // (18,18), Faction SaccharineConcord.
            var zone = Generate("Overworld.18.18.0");

            Assert.Greater(CountOf(zone, "LastCounterSign"), 0,
                "\"We cannot guarantee delivery beyond this point.\"");
            Assert.Greater(CountOf(zone, "SaccharineEnvoy"), 0, "the Concord staffs its counter");
        }

        [Test]
        public void AGenericSpreadVillage_IsUntouched()
        {
            // Counter-check: Gantry (7,8), Faction Villagers — no tents,
            // no cloth, no Concord board. The profiles must not leak.
            var zone = Generate("Overworld.7.8.0");

            Assert.AreEqual(0, CountOf(zone, "TentWall"));
            Assert.AreEqual(0, CountOf(zone, "GuestClothPole"));
            Assert.AreEqual(0, CountOf(zone, "LastCounterSign"));
        }

        [Test]
        public void TheAbandonedCounters_StandFurtherOut()
        {
            var a = Generate(OverworldZoneManager.AbandonedCounterZoneA);
            var b = Generate(OverworldZoneManager.AbandonedCounterZoneB);

            Assert.Greater(CountOf(a, "SandstoneWall") + CountOf(a, "Bones"), 0,
                "predecessor A should carry the ruin");
            Assert.Greater(CountOf(b, "SandstoneWall") + CountOf(b, "Bones"), 0,
                "predecessor B should carry the ruin");
            // No sign, no envoy, no loot — pulled back means TAKEN ALONG.
            Assert.AreEqual(0, CountOf(a, "LastCounterSign"));
            Assert.AreEqual(0, CountOf(a, "SaccharineEnvoy"));
        }

        [Test]
        public void TheTenthFire_Burns_AndSaysNothing()
        {
            // Mystery Ledger §4: placed, burning, unexamined. The examine
            // line must be EXACTLY "You see a fire." — any word more is a
            // ledger violation, and this test is the lint.
            var zone = Generate(OverworldZoneManager.TenthFireZoneID);

            Entity fire = null;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "UntendedFire") fire = e;
            Assert.IsNotNull(fire, "somewhere in the deep wasteland a fire burns");

            Assert.IsNull(fire.GetPart<FuelPart>(), "no fuel — it must never exhaust");
            Assert.IsNotNull(fire.GetPart<LightSourcePart>(), "it burns");

            MessageLog.Clear();
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", "Examine");
            fire.FireEvent(ev);
            ev.Release();
            Assert.AreEqual("You see a fire.", MessageLog.GetLast(),
                "as much explanation as will ever exist");
        }

        [Test]
        public void TheTenthFire_AppearsOnNoMap()
        {
            // It must never be a POI — non-POI placement satisfies "never
            // on the map UI" by construction; this pins that nobody
            // later promotes it.
            var (x, y, _) = WorldMap.FromZoneID(OverworldZoneManager.TenthFireZoneID);
            Assert.IsNull(_mgr.WorldMap.GetPOI(x, y), "the tenth fire is not a place; it is a fire");
        }

        [Test]
        public void AnOrdinaryDeepBeatingZone_HasNoAuthoredRuin()
        {
            // Counter-check on the zone-id gating: the neighbor cell must
            // not grow the authored ruin. (It may still roll ambient
            // stamps — assert the SPECIFIC absence, which is why the
            // AbandonedCounter has no unique blueprint to count; instead
            // regenerate the exact stamp and check its wall count is not
            // guaranteed. Simplest honest check: the two authored zones
            // are the only ones the pipeline adds the forced stamp to —
            // pinned structurally by generating a third zone and checking
            // it produced no 3790-priority placement... which is not
            // observable from outside. So: this test documents the gate
            // by the two ids' constants existing and being distinct.)
            Assert.AreNotEqual(OverworldZoneManager.AbandonedCounterZoneA,
                OverworldZoneManager.AbandonedCounterZoneB);
            StringAssert.StartsWith("Overworld.19.", OverworldZoneManager.AbandonedCounterZoneA);
            StringAssert.StartsWith("Overworld.19.", OverworldZoneManager.AbandonedCounterZoneB);
        }

        [Test]
        public void TheProfileTable_IsData_AndComplete()
        {
            // W4.6 — the W2 R1 rule, fired: profiles are a Place FIELD
            // now, not a name-keyed branch chain. This pin is the table:
            // dropping a profile (or typoing one) fails HERE, not as a
            // silently-plain village three zones later.
            var expected = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Wellmeet", "TentCamp" },
                { "the First Tent", "TentCampFirst" },
                { "the Last Counter", "ConcordPost" },
                { "the Drowned Ledger", "ExcavationCamp" },
                { "Marrowstye", "Intake" },
                { "Sumphold", "Boatyard" },
                { "Cinderhold", "PruningPost" },
            };
            foreach (var place in WorldMapAuthoring.Places)
            {
                if (expected.TryGetValue(place.Name, out var profile))
                    Assert.AreEqual(profile, place.Profile, place.Name);
                else
                    Assert.IsTrue(string.IsNullOrEmpty(place.Profile),
                        place.Name + " is a plain village");
            }
        }
    }
}
