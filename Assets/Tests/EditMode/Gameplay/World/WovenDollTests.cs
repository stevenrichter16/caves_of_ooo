using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W4.5 (Docs/FELLING-W4-PLAN.md §3, design gate 4) — the doll in
    /// the wall. ONE authored wilderness Grovelands zone holds a woven
    /// doll beside the column that has grown around one of its arms —
    /// uneaten, unexplained, no examine text beyond bare sight. The
    /// tenth-fire pattern: a const zone id, a forced stamp, and a gate
    /// test that pins the exact line so any future word more fails a
    /// build.
    /// </summary>
    public class WovenDollTests
    {
        private static EntityFactory _factory;
        private static OverworldZoneManager _mgr;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            _mgr = new OverworldZoneManager(_factory, worldSeed: 42);
        }

        private static int CountOf(Zone zone, string blueprint)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) n++;
            return n;
        }

        [Test]
        public void TheDoll_IsHeld_AndSaysOnlyWhatYouSee()
        {
            var zone = _mgr.GetZone(OverworldZoneManager.WovenDollZoneID);
            Assert.IsNotNull(zone);

            Entity doll = null;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "WovenDoll") doll = e;
            Assert.IsNotNull(doll, "the doll is there");
            Assert.AreEqual(1, CountOf(zone, "WovenDoll"), "and there is one");

            // The column that holds it stands beside it.
            var dp = zone.GetEntityPosition(doll);
            bool columnAdjacent = false;
            foreach (var e in zone.GetAllEntities())
            {
                if (e.BlueprintName != "MycelialColumn") continue;
                var cp = zone.GetEntityPosition(e);
                if (System.Math.Abs(cp.x - dp.x) <= 1 && System.Math.Abs(cp.y - dp.y) <= 1)
                    columnAdjacent = true;
            }
            Assert.IsTrue(columnAdjacent, "the growth holds one arm");

            // Bare sight, exactly — the tenth-fire lint shape. Any word
            // more is a design-gate violation.
            MessageLog.Clear();
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", "Examine");
            doll.FireEvent(ev);
            ev.Release();
            Assert.AreEqual(
                "You see a woven doll. No longer than a hand. The column beside it has grown around one arm, and stopped.",
                MessageLog.GetLast(),
                "no examine text beyond bare sight — ever");
        }

        [Test]
        public void TheDollsGrove_IsQuiet()
        {
            // The Bloom-front never touches the doll's zone — authored
            // stillness, pinned structurally like the R8 exclusions.
            var pipe = new ExposingManager(_factory).Pipe(
                OverworldZoneManager.WovenDollZoneID);
            foreach (var b in pipe.Builders)
                Assert.IsFalse(b is BloomFrontBuilder,
                    "the front does not land where the doll waits");
        }

        [Test]
        public void AnOrdinaryGrovelandsZone_HasNoDoll()
        {
            // Counter-check on the zone-id gate.
            var zone = _mgr.GetZone("Overworld.2.6.0");
            Assert.IsNotNull(zone);
            Assert.AreEqual(0, CountOf(zone, "WovenDoll"),
                "one zone, one doll, nowhere else");
        }

        [Test]
        public void TheDoll_AppearsOnNoMap()
        {
            var (x, y, _) = WorldMap.FromZoneID(OverworldZoneManager.WovenDollZoneID);
            Assert.IsNull(_mgr.WorldMap.GetPOI(x, y),
                "the doll's grove is not a place; it is a grove");
        }

        [Test]
        public void AuthoredZones_AreReservedFromOpportunisticPOIs()
        {
            // PlacePOIs rolls 3-5 lairs and 2-3 camps onto any cell far
            // enough from the authored Places — and (1,6) qualifies
            // (nearest Place is Cinderhold at distance 5). On those
            // seeds the POI pipeline wins and design-gate-4 canon simply
            // does not exist in that world. Same latent hole under the
            // tenth fire. Sweep seeds; the authored cells stay clear.
            foreach (var zoneId in new[] { OverworldZoneManager.WovenDollZoneID,
                                           OverworldZoneManager.TenthFireZoneID })
            {
                var (x, y, _) = WorldMap.FromZoneID(zoneId);
                for (int seed = 1; seed <= 60; seed++)
                {
                    var mgr = new OverworldZoneManager(_factory, worldSeed: seed);
                    Assert.IsNull(mgr.WorldMap.GetPOI(x, y),
                        $"seed {seed}: an opportunistic POI claimed {zoneId} — " +
                        "the authored scene would not exist in that world");
                }
            }
        }

        [Test]
        public void TheDollsGrove_IsBare()
        {
            // W2.8's tenth-fire lesson, verbatim: "the first look pass
            // placed the fire in a zone that had rolled a hermit's hut
            // ... the mystery needs emptiness." The doll rode the full
            // busy Grovelands pipeline — ambient stamps, containers, a
            // tier-3 population — thirty cells from the scene.
            var pipe = new ExposingManager(_factory).Pipe(
                OverworldZoneManager.WovenDollZoneID);
            // Scoped to the tenth fire's actual shape: it strips
            // ambient stamps and containers but KEEPS population — a
            // wholly creatureless zone reads as broken, not quiet, and
            // Choir-country fauna (moths) belongs at a grove. What
            // dilutes a mystery is a hermit's hut or a stashed chest
            // thirty cells away, not a moth.
            int landmarks = 0;
            foreach (var b in pipe.Builders)
            {
                Assert.IsFalse(b is ContainerBuilder,
                    "nothing is stashed beside the doll");
                if (b is LandmarkBuilder) landmarks++;
            }
            Assert.AreEqual(1, landmarks,
                "exactly one landmark builder — the doll's own stamp, " +
                "no ambient hut rolling in beside the mystery");
        }

        private sealed class ExposingManager : OverworldZoneManager
        {
            public ExposingManager(EntityFactory f) : base(f, worldSeed: 42) { }
            public ZoneGenerationPipeline Pipe(string id) => GetPipelineForZone(id);
        }
    }
}
