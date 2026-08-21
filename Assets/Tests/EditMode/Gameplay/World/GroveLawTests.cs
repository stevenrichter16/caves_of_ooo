using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W4.2 (Docs/FELLING-W4-PLAN.md §3) — the grove rules with teeth.
    /// The sign's law (Lore/Codex/11): the seep is free (shipped and
    /// pinned in W4.1), eat nothing red (GroveRed — the best vital
    /// reagent in the game AND a brew-ruiner, per WORLD-INGREDIENTS),
    /// do not dig (RotChoir standing, immediate), and — implicit in
    /// what a grove IS — fire is a crime.
    ///
    /// <para>Zone ids here are REAL authored cells: GroveLaw reads the
    /// biome off the zone id, so "Overworld.0.0.0" (a G cell) is Choir
    /// ground and "Overworld.8.16.0" (Wellmeet's B cell) is not.</para>
    /// </summary>
    public class GroveLawTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            HarvestablePart.Factory = _factory;
        }

        [OneTimeTearDown]
        public void TearDownOnce() => HarvestablePart.Factory = null;

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            PlayerReputation.Reset();
        }

        [TearDown]
        public void TearDown() => PlayerReputation.Reset();

        private const string GroveZone = "Overworld.0.0.0";     // authored 'G'
        private const string BeatingZone = "Overworld.8.16.0";  // authored 'B' (Wellmeet's cell)

        private static Entity Player(Zone zone)
        {
            var p = new Entity { ID = "player", BlueprintName = "Player" };
            p.Tags["Player"] = "";
            p.Tags["Creature"] = "";
            p.AddPart(new RenderPart { DisplayName = "you" });
            p.AddPart(new InventoryPart());
            zone.AddEntity(p, 10, 10);
            return p;
        }

        private static void Harvest(Entity target, Entity actor, Zone zone)
        {
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", "Harvest");
            ev.SetParameter("Actor", (object)actor);
            ev.SetParameter("Zone", (object)zone);
            ev.SetParameter("Random", (object)new Random(3));
            target.FireEvent(ev);
            ev.Release();
        }

        private static int FactionRecords(string kind)
            => DiagQuery.Apply(new DiagQuery.Filter
            { Category = "faction", Kind = kind, Limit = 20 }).Records.Count;

        // ════════════════════════════════════════════════════════
        // Do not dig
        // ════════════════════════════════════════════════════════

        [Test]
        public void DiggingAVein_OnChoirGround_CostsStanding()
        {
            var zone = new Zone(GroveZone);
            var player = Player(zone);
            var vein = _factory.CreateEntity("ChoirIronVein");
            zone.AddEntity(vein, 11, 10);

            Harvest(vein, player, zone);

            Assert.AreEqual(GroveLaw.DigRepLoss, PlayerReputation.Get("RotChoir"),
                "\"Do not dig.\" — the sign said so, plainly");
            Assert.AreEqual(1, FactionRecords("GroveDug"),
                "the act is recorded — the Choir never forgets");
        }

        [Test]
        public void TheSameDig_AnywhereElse_CostsNothing()
        {
            var zone = new Zone(BeatingZone);
            var player = Player(zone);
            var vein = _factory.CreateEntity("PaleSaltVein");
            zone.AddEntity(vein, 11, 10);

            Harvest(vein, player, zone);

            Assert.AreEqual(0, PlayerReputation.Get("RotChoir"),
                "the wasteland's salt is nobody's grief");
        }

        [Test]
        public void PickingTheRed_IsForaging_NotDigging()
        {
            // "Eat nothing red" is a WARNING to you, not a law with
            // teeth — the growth is at the edge where the sign can see
            // it, and nothing stops you picking it.
            var zone = new Zone(GroveZone);
            var player = Player(zone);
            var growth = _factory.CreateEntity("GroveRedGrowth");
            zone.AddEntity(growth, 11, 10);

            Harvest(growth, player, zone);

            Assert.AreEqual(0, PlayerReputation.Get("RotChoir"),
                "foraging is not digging; the warning is the label, not a fine");
            bool gotRed = false;
            foreach (var it in player.GetPart<InventoryPart>().Objects)
                if (it.BlueprintName == "GroveRed") gotRed = true;
            Assert.IsTrue(gotRed, "and the red is yours now, for whatever you think it is");
        }

        [Test]
        public void AnNpcDigging_IsNotThePlayersLedger()
        {
            var zone = new Zone(GroveZone);
            var npc = new Entity { ID = "npc", BlueprintName = "Villager" };
            npc.Tags["Creature"] = "";
            npc.AddPart(new RenderPart { DisplayName = "villager" });
            npc.AddPart(new InventoryPart());
            zone.AddEntity(npc, 10, 10);
            var vein = _factory.CreateEntity("GlowQuartzVein");
            zone.AddEntity(vein, 11, 10);

            Harvest(vein, npc, zone);

            Assert.AreEqual(0, PlayerReputation.Get("RotChoir"));
        }

        // ════════════════════════════════════════════════════════
        // Fire is a crime
        // ════════════════════════════════════════════════════════

        private Entity Ignitable(Zone zone, int x, int y)
        {
            // Mirrors MaterialPrimitivesPhaseATests' proven fixture: a
            // Hitpoints stat, Combustibility on the 0-1 scale, defaults
            // elsewhere. FlameTemperature 100 so a modest pulse crosses
            // it without tripping the |delta|>200 thermal-shock branch.
            var e = new Entity { ID = "kindling" + x, BlueprintName = "Kindling" };
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            e.AddPart(new RenderPart { DisplayName = "kindling" });
            e.AddPart(new MaterialPart { Combustibility = 0.8f, MaterialTagsRaw = "Organic,Flammable" });
            e.AddPart(new ThermalPart { FlameTemperature = 100f, HeatCapacity = 1.0f });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static void Heat(Entity target, Entity source, Zone zone, float joules)
        {
            var ev = GameEvent.New("ApplyHeat");
            ev.SetParameter("Joules", (object)joules);
            ev.SetParameter("Radiant", (object)false);
            ev.SetParameter("Source", (object)source);
            ev.SetParameter("Zone", (object)zone);
            target.FireEvent(ev);
            ev.Release();
        }

        [Test]
        public void SettingAFire_OnChoirGround_IsACrime()
        {
            var zone = new Zone(GroveZone);
            var player = Player(zone);
            var kindling = Ignitable(zone, 11, 10);

            Heat(kindling, player, zone, 150f);

            Assert.IsTrue(kindling.HasEffect<BurningEffect>(), "setup: it caught");
            Assert.AreEqual(GroveLaw.FireRepLoss, PlayerReputation.Get("RotChoir"),
                "fire, in a grove — the single fastest way to be remembered");
            Assert.AreEqual(1, FactionRecords("GroveBurned"));
        }

        [Test]
        public void TheSameFire_AnywhereElse_IsJustFire()
        {
            var zone = new Zone(BeatingZone);
            var player = Player(zone);
            var kindling = Ignitable(zone, 11, 10);

            Heat(kindling, player, zone, 150f);

            Assert.IsTrue(kindling.HasEffect<BurningEffect>());
            Assert.AreEqual(0, PlayerReputation.Get("RotChoir"),
                "the wasteland burns without witnesses");
        }

        [Test]
        public void FireSpread_ChargesOnlyTheArson()
        {
            // Propagation events carry the BURNING ENTITY as source —
            // not the player — so a spreading blaze is one crime, not
            // one per tile it eats.
            var zone = new Zone(GroveZone);
            Player(zone);
            var first = Ignitable(zone, 11, 10);
            var second = Ignitable(zone, 12, 10);

            Heat(second, first, zone, 150f);   // the fire itself spreads

            Assert.IsTrue(second.HasEffect<BurningEffect>());
            Assert.AreEqual(0, PlayerReputation.Get("RotChoir"),
                "the spread is the fire's doing; the Choir charged the hand, once");
        }

        // ════════════════════════════════════════════════════════
        // Eat nothing red — the reagent itself
        // ════════════════════════════════════════════════════════

        [Test]
        public void GroveRed_IsTheBestVitalReagent_AndABrewRuiner()
        {
            // WORLD-INGREDIENTS.md:46, shipped exactly: vital:3 toxic:1,
            // value 18, and the flavor line word for word. The
            // folk-warning as brew-math.
            var red = _factory.CreateEntity("GroveRed");
            var reagent = red.GetPart<ReagentPart>();
            Assert.IsNotNull(reagent, "the red is a reagent");

            int vital = 0, toxic = 0;
            foreach (var p in reagent.GetProperties())
            {
                if (p.Property == BrewProperties.Vital) vital = p.Potency;
                if (p.Property == BrewProperties.Toxic) toxic = p.Potency;
            }
            Assert.AreEqual(3, vital, "astonishing health");
            Assert.AreEqual(1, toxic, "and it ruins every brew");
            Assert.AreEqual(18, red.GetPart<CommercePart>().Value,
                "somebody always pays for astonishing health");
            StringAssert.Contains("grown from everyone the grove has ever loved",
                reagent.FlavorText, "the flavor line as designed");
        }

        [Test]
        public void TheDigLaw_HasARealTemptation_InTheWorld()
        {
            // Reachability audit finding (RED pre-fix): veins spawned
            // only underground and in Beating salt pans — NO vein
            // existed on Grovelands surface, so the dig law shipped
            // with no reachable trigger. The fen now surfaces choir
            // iron rarely; this proves the whole chain END TO END in a
            // real authored fen zone: the vein generates, the player
            // digs it, the Choir charges.
            int veinsSeen = 0;
            bool chainProven = false;
            for (int seed = 0; seed < 20 && !chainProven; seed++)
            {
                var zone = new Zone(GroveZone);   // authored 'G' cell — real Choir ground
                for (int x = 1; x < Zone.Width - 1; x++)
                    for (int y = 1; y < Zone.Height - 1; y++)
                    {
                        var g = _factory.CreateEntity("Grass");
                        if (g != null) zone.AddEntity(g, x, y);
                    }
                new GrovelandsFormationBuilder { Override = Formation.TendrilFen }
                    .BuildZone(zone, _factory, new Random(seed));

                Entity vein = null;
                foreach (var e in zone.GetAllEntities())
                    if (e.BlueprintName == "ChoirIronVein") vein = e;
                if (vein == null) continue;
                veinsSeen++;

                PlayerReputation.Reset();
                var player = Player(zone);
                Harvest(vein, player, zone);
                chainProven = PlayerReputation.Get("RotChoir") == GroveLaw.DigRepLoss;
            }
            Assert.Greater(veinsSeen, 0,
                "twenty fens and no iron — the law has no temptation again");
            Assert.IsTrue(chainProven,
                "generate, dig, be remembered — the whole chain, in a real zone");
        }

        [Test]
        public void TheRedGrowth_GrowsAtGroveEdges()
        {
            // Sited by the Grove routine (edge ring) and the fen banks.
            var zone = new Zone("Overworld.2.2.0");
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var g = _factory.CreateEntity("Grass");
                    if (g != null) zone.AddEntity(g, x, y);
                }
            int total = 0;
            for (int seed = 0; seed < 8; seed++)
            {
                var z = new Zone("Overworld.2.2.0");
                for (int x = 1; x < Zone.Width - 1; x++)
                    for (int y = 1; y < Zone.Height - 1; y++)
                    {
                        var g = _factory.CreateEntity("Grass");
                        if (g != null) z.AddEntity(g, x, y);
                    }
                new GrovelandsFormationBuilder { Override = Formation.Grove }
                    .BuildZone(z, _factory, new Random(seed));
                foreach (var e in z.GetAllEntities())
                    if (e.BlueprintName == "GroveRedGrowth") total++;
            }
            Assert.Greater(total, 0, "the red grows where the sign can see it");
        }
    }
}
