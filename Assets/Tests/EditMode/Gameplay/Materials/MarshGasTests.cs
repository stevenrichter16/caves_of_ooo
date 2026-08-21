using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W3.3 (Docs/FELLING-W3-PLAN.md §3) — methane peat. BurnOffGasPart
    /// shipped in G.9 with its docstring promising "a peat bog venting
    /// methane when torched" — and no peat ever carried it. Now PeatBog
    /// and MirePool do, and the docstring is finally honored.
    ///
    /// <para><b>The substrate finding this SM fixed:</b> the part
    /// listens for TakeDamage, but terrain has no Hitpoints stat, so
    /// CombatSystem.ApplyDamage early-outs and the STRUCTURAL damage
    /// path (DestructionSystem.RouteDamage → Damage) never announced
    /// the event — a burning bog burned in silence and nothing could
    /// react. RouteDamage's destructible branch now fires TakeDamage
    /// with ApplyDamage's exact contract (pre-decrement, mutable
    /// amount, clamped ≥0).</para>
    ///
    /// <para><b>🧪 deferral (per plan):</b> gas-cloud IGNITION (a
    /// marsh-gas cloud touching flame and going up, chaining to the
    /// next bog) needs a flammable gas behavior; none ships. The vent
    /// alone ships; the chain is deferred with this note.</para>
    /// </summary>
    public class MarshGasTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            // The REAL shipped definition file — pins that the content
            // the game loads at bootstrap actually parses and registers.
            GasRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/GasDefinitions/marsh-gas.json")));
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            SettlementRuntime.ActiveZone = null;
        }

        private Zone _zone;

        private Entity Peat(string blueprint = "PeatBog", int x = 10, int y = 10)
        {
            _zone = new Zone("Z");
            SettlementRuntime.ActiveZone = _zone;
            var e = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(e, blueprint + " should exist");
            _zone.AddEntity(e, x, y);
            return e;
        }

        private static void FireAt(Entity e, Zone zone, int amount, string attribute = "Fire")
        {
            var d = new Damage(amount);
            d.AddAttribute(attribute);
            DestructionSystem.RouteDamage(e, d, null, zone);
        }

        private static int GasAt(Zone zone, int x, int y)
        {
            int n = 0;
            foreach (var ent in zone.GetAllEntities())
            {
                if (!ent.Tags.ContainsKey("Gas")) continue;
                var p = zone.GetEntityPosition(ent);
                if (p.x == x && p.y == y) n++;
            }
            return n;
        }

        // ════════════════════════════════════════════════════════
        // The vent
        // ════════════════════════════════════════════════════════

        [Test]
        public void FireOnAPeatBog_VentsMarshGas()
        {
            // End-to-end through the path burning terrain actually takes:
            // BurningEffect routes its tick through RouteDamage, so a
            // fire-attributed RouteDamage IS "the bog is burning".
            var bog = Peat();

            FireAt(bog, _zone, 6);

            Assert.AreEqual(1, GasAt(_zone, 10, 10),
                "six fire in, one cloud out — the bog answers the torch");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "BurnOff", Limit = 10 }).Records.Count);
        }

        [Test]
        public void ABluntBlow_MovesPeat_ButVentsNothing()
        {
            // Counter-check: the trigger filter. A spade is not a torch —
            // the structural damage still lands (the event precedes the
            // decrement, it doesn't replace it), but no gas.
            var bog = Peat();
            int hpBefore = bog.GetPart<DestructiblePart>().HP;

            FireAt(bog, _zone, 6, attribute: "Bludgeon");

            Assert.AreEqual(0, GasAt(_zone, 10, 10));
            Assert.Less(bog.GetPart<DestructiblePart>().HP, hpBefore,
                "the announcement must not swallow the damage");
        }

        [Test]
        public void TheMireVentsToo_ButTheWaterBarelyGivesGround()
        {
            // "MirePool gets it too (same peat)" — but the mire is mostly
            // water: Hardness 5 means a burning tick erodes it by the
            // 1-minimum, so it vents long before it ever burns away.
            var mire = Peat("MirePool");

            FireAt(mire, _zone, 6);

            Assert.AreEqual(1, GasAt(_zone, 10, 10), "the trapped gas comes up");
            Assert.AreEqual(199, mire.GetPart<DestructiblePart>().HP,
                "6 damage - hardness 5 = the 1-minimum: the water holds");
        }

        [Test]
        public void EnoughFire_BurnsTheBogAway()
        {
            // Peat is fuel. Seven six-point fire ticks exhaust HP 40 —
            // the bog is CONSUMED, venting as it goes.
            var bog = Peat();

            for (int i = 0; i < 7; i++) FireAt(bog, _zone, 6);

            Assert.IsNull(_zone.GetEntityCell(bog), "the peat is spent");
            Assert.GreaterOrEqual(DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "BurnOff", Limit = 20 }).Records.Count, 6,
                "it vented the whole way down");
        }

        [Test]
        public void TheDuckboard_BurnsLikeItsNeighbors()
        {
            // W3 re-review (RED pre-fix): Duckboard carried Combustibility
            // 0.6 + Thermal (so it visibly IGNITES) but no Destructible —
            // its burning ticks fell into the exact silent void
            // RouteDamage's own doc-comment exists to name. The safe line
            // burns like everything else old and wooden here; MirePool is
            // walkable, so a lost board never breaks reachability.
            var board = Peat("Duckboard");
            var part = board.GetPart<DestructiblePart>();
            Assert.IsNotNull(part, "burnable things carry a pool to burn from");
            int before = part.HP;

            FireAt(board, _zone, 6);

            Assert.Less(part.HP, before, "the fire does something");
            Assert.AreEqual(0, GasAt(_zone, 10, 10),
                "wood, not peat — it burns without venting");
        }

        [Test]
        public void ADeadTree_BurnsInSilence()
        {
            // Counter-check on the blueprint wiring: the event now fires
            // for every destructible, but only the peat carries the part.
            var tree = Peat("DeadTree");

            FireAt(tree, _zone, 6);

            Assert.AreEqual(0, GasAt(_zone, 10, 10),
                "wood burns; only peat holds a thousand years of gas");
        }

        // ════════════════════════════════════════════════════════
        // The gas itself
        // ════════════════════════════════════════════════════════

        [Test]
        public void MarshGas_Seeps_AndChokes()
        {
            var def = GasRegistry.Get("marsh-gas");
            Assert.IsNotNull(def, "the definition file registers");
            Assert.IsTrue(def.Seeping, "marsh gas finds its way through the reeds");
            Assert.AreEqual("Poison", def.BehaviorKind,
                "no flammable behavior ships (the 🧪 chain deferral); " +
                "the cloud chokes, which methane in a low hollow does");
        }

        [Test]
        public void TheSpawnedCloud_CarriesThePoisonBehavior()
        {
            var bog = Peat();
            FireAt(bog, _zone, 6);

            Entity cloud = null;
            foreach (var ent in _zone.GetAllEntities())
                if (ent.Tags.ContainsKey("Gas")) cloud = ent;
            Assert.IsNotNull(cloud);
            Assert.IsNotNull(cloud.GetPart<GasPoisonPart>(),
                "BehaviorKind resolves through GasFactory to the real part");
        }
    }
}
