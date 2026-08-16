using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Adversarial sweep for Docs/LIQUID-SLIP.md (CLAUDE.md gate:
    /// probabilistic boundaries, cross-actor flows, malformed content,
    /// re-entrancy). Each test names the bug class it probes and why a
    /// wrong implementation would fail it.
    /// </summary>
    public class LiquidSlipAdversarialTests
    {
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            LiquidRegistry.ResetForTests();
            LiquidSlipSystem.TestRng = new System.Random(7);
            LoadAll(extraJson: null);
        }

        [TearDown]
        public void TearDown()
        {
            LiquidSlipSystem.TestRng = null;
            LiquidRegistry.ResetForTests();
        }

        private static void LoadAll(string extraJson)
        {
            var all = new System.Collections.Generic.List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(
                Application.dataPath, "Resources/Content/Data/LiquidDefinitions"), "*.json"))
                all.Add(File.ReadAllText(f));
            if (extraJson != null) all.Add(extraJson);
            LiquidRegistry.InitializeFromJsonSources(all);
        }

        private static void Author(string id, bool slippery, string chanceLiteral)
            => LoadAll("{\"Liquids\":[{\"Id\":\"" + id + "\",\"DisplayName\":\"" + id
                + "\",\"Slippery\":" + (slippery ? "true" : "false")
                + ",\"SlipChance\":" + chanceLiteral + "}]}");

        private static Entity Creature(Zone zone, int x, int y, string name = "c")
        {
            var e = new Entity { ID = name + x + "_" + y, BlueprintName = "Snapjaw" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            zone.AddEntity(e, x, y);
            return e;
        }

        private static System.Collections.Generic.IReadOnlyList<Diag.Entry> Recs(string kind)
            => DiagQuery.Apply(new DiagQuery.Filter { Category = "liquid", Kind = kind, Limit = 100 }).Records;

        // ════════════════════════════════════════════════════════════
        // Probability boundaries + malformed content
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SlipChanceOver100_ClampsToAlways_NeverThrows()
        {
            Author("ice", true, "250");
            var zone = new Zone("Z");
            var m = Creature(zone, 10, 10);
            zone.TileState.WriteCoating(11, 10, "ice", 4);

            Assert.DoesNotThrow(() => MovementSystem.TryMove(m, zone, 1, 0));

            StringAssert.Contains("\"chance\":100", Recs("SlipRolled")[0].PayloadJson,
                "clamped at read time — content typos cannot make Next(100) misbehave");
            Assert.AreEqual(1, Recs("Slipped").Count);
        }

        [Test]
        public void Adversarial_NegativeSlipChance_ClampsToNever()
        {
            Author("ice", true, "-30");
            var zone = new Zone("Z");
            var m = Creature(zone, 10, 10);
            zone.TileState.WriteCoating(11, 10, "ice", 4);

            MovementSystem.TryMove(m, zone, 1, 0);

            StringAssert.Contains("\"chance\":0", Recs("SlipRolled")[0].PayloadJson);
            Assert.AreEqual((11, 10), zone.GetEntityPosition(m));
        }

        [Test]
        public void Adversarial_UnknownCoatingId_NoRoll_NoThrow()
        {
            // A coating whose id no liquid file knows (a stale save, a
            // typo in a reaction's OutputCoating). Registry answers null;
            // the resolver must treat that as "not slippery", not crash.
            var zone = new Zone("Z");
            var m = Creature(zone, 10, 10);
            zone.TileState.WriteCoating(11, 10, "not-a-liquid", 4);

            Assert.DoesNotThrow(() => MovementSystem.TryMove(m, zone, 1, 0));
            Assert.AreEqual(0, Recs("SlipRolled").Count);
        }

        [Test]
        public void Adversarial_RegistryUninitialized_NoRoll_NoThrow()
        {
            LiquidRegistry.ResetForTests();
            var zone = new Zone("Z");
            var m = Creature(zone, 10, 10);
            zone.TileState.WriteCoating(11, 10, "ice", 4);

            Assert.DoesNotThrow(() => MovementSystem.TryMove(m, zone, 1, 0));
            Assert.AreEqual(0, Recs("SlipRolled").Count, "no registry, no opinion about ice");
        }

        // ════════════════════════════════════════════════════════════
        // One roll per move — coexisting layers, pool + coating
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_OilCoatingPlusOilPoolInOneCell_RollsOnce()
        {
            // The two-universe double-roll (plan §8 R3). Zone.ProjectPool
            // mirrors the pool as a permanent coating; a thrown flask on
            // an Oilmark slick then holds "oil" twice at the tile level?
            // No — WriteLayer replaces same-id layers. Either way: ONE roll.
            Author("oil", true, "0");
            var zone = new Zone("Z");
            var m = Creature(zone, 10, 10);
            zone.TileState.WriteCoating(11, 10, "oil", 6);
            var pool = new Entity { ID = "pool", BlueprintName = "OilPool" };
            pool.AddPart(new RenderPart { DisplayName = "oil pool" });
            pool.AddPart(new LiquidPoolPart { LiquidId = "oil", Volume = 5 });
            zone.AddEntity(pool, 11, 10);

            MovementSystem.TryMove(m, zone, 1, 0);

            Assert.AreEqual(1, Recs("SlipRolled").Count, "coating + projected pool = one question");
        }

        [Test]
        public void Adversarial_WaterUnderIce_RollsForTheIceOnly()
        {
            // Coatings coexist (water+oil is the design's showcase). Water
            // is not slippery; if the scan stopped at the first coating
            // regardless of flag, water listed first would mask the ice.
            Author("ice", true, "0");
            var zone = new Zone("Z");
            var m = Creature(zone, 10, 10);
            zone.TileState.WriteCoating(11, 10, "water", 6);   // listed first
            zone.TileState.WriteCoating(11, 10, "ice", 4);

            MovementSystem.TryMove(m, zone, 1, 0);

            var rolled = Recs("SlipRolled");
            Assert.AreEqual(1, rolled.Count);
            StringAssert.Contains("\"liquidId\":\"ice\"", rolled[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        // No move, no roll — the freeze-under-your-feet case (R7)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_FreezeUnderStandingCreature_DoesNotRollSlip()
        {
            Author("ice", true, "100");
            var zone = new Zone("Z");
            var m = Creature(zone, 10, 10);

            zone.TileState.WriteCoating(10, 10, "ice", 4);   // the ground freezes under it

            Assert.AreEqual((10, 10), zone.GetEntityPosition(m));
            Assert.AreEqual(0, Recs("SlipRolled").Count,
                "a slip is a consequence of STEPPING; standing still on new ice rolls nothing");
        }

        // ════════════════════════════════════════════════════════════
        // Chain + re-entrancy + zone edge
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Chance100_TenCellSheet_StillCapsAtMaxChain()
        {
            Author("ice", true, "100");
            var zone = new Zone("Z");
            var m = Creature(zone, 1, 10);
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                    zone.TileState.WriteCoating(x, y, "ice", 4);   // the WHOLE zone

            Assert.DoesNotThrow(() => MovementSystem.TryMove(m, zone, 1, 0));

            Assert.AreEqual(LiquidSlipSystem.MAX_SLIDE_CHAIN, Recs("Slipped").Count,
                "the cap is load-bearing with a flat chance — no wall ever stops this sheet");
        }

        [Test]
        public void Adversarial_SlideAtZoneEdge_NeverLeavesBounds()
        {
            Author("ice", true, "100");
            var zone = new Zone("Z");
            var m = Creature(zone, 1, 0);
            zone.TileState.WriteCoating(0, 0, "ice", 4);   // the corner

            for (int i = 0; i < 20; i++)
            {
                LiquidSlipSystem.TestRng = new System.Random(i);
                MovementSystem.ForceMoveTo(m, zone, 0, 0);
                var p = zone.GetEntityPosition(m);
                Assert.IsTrue(zone.InBounds(p.x, p.y), "seed " + i + " slid out of bounds to " + p);
            }
        }

        [Test]
        public void Adversarial_MoverDiesOnLanding_ChainStopsCleanly()
        {
            // A step trap on the landing cell kills the mover mid-chain.
            // The next Resolve must see "not in zone" and stop, no NRE.
            Author("ice", true, "100");
            var zone = new Zone("Z");
            var m = Creature(zone, 10, 10);
            zone.TileState.WriteCoating(11, 10, "ice", 4);
            // Ring the ice cell with traps so whichever way it slides, it dies there.
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (!(dx == 0 && dy == 0))
                    {
                        var t = new Entity { ID = "trap" + dx + "_" + dy, BlueprintName = "Trap" };
                        t.AddPart(new KillOnEnter(zone));
                        zone.AddEntity(t, 11 + dx, 10 + dy);
                        zone.TileState.WriteCoating(11 + dx, 10 + dy, "ice", 4);
                    }

            Assert.DoesNotThrow(() => MovementSystem.TryMove(m, zone, 1, 0));
            Assert.AreEqual((-1, -1), zone.GetEntityPosition(m), "the trap removed it");
            Assert.AreEqual(1, Recs("Slipped").Count,
                "one slide onto the trap; the resolver sees the mover is no longer in the landed cell and stops");
        }

        private sealed class KillOnEnter : Part
        {
            private readonly Zone _zone;
            public KillOnEnter(Zone zone) { _zone = zone; }
            public override string Name => "KillOnEnter";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "EntityEnteredCell") return true;
                var who = e.GetParameter<Entity>("Actor") ?? e.GetParameter<Entity>("Entity");
                if (who != null && who.HasTag("Creature")) _zone.RemoveEntity(who);
                return true;
            }
        }

        // ════════════════════════════════════════════════════════════
        // Cross-actor + pollution
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SlideBackOntoOrigin_IsLegal()
        {
            // The random direction may point back where the mover came
            // from. That is a legal landing (Qud allows it) — pinned as
            // allowed so nobody "fixes" it into a bias.
            Author("ice", true, "100");
            var zone = new Zone("Z");
            var m = Creature(zone, 10, 10);
            zone.TileState.WriteCoating(11, 10, "ice", 4);
            for (int dx = -1; dx <= 1; dx++)          // wall every neighbour but the origin
                for (int dy = -1; dy <= 1; dy++)
                    if (!(dx == 0 && dy == 0) && !(dx == -1 && dy == 0))
                    {
                        var w = new Entity { ID = "w" + dx + dy, BlueprintName = "Wall" };
                        w.Tags["Solid"] = "";
                        zone.AddEntity(w, 11 + dx, 10 + dy);
                    }

            bool everWentBack = false;
            for (int i = 0; i < 40 && !everWentBack; i++)
            {
                LiquidSlipSystem.TestRng = new System.Random(i);
                MovementSystem.ForceMoveTo(m, zone, 10, 10);
                MovementSystem.ForceMoveTo(m, zone, 11, 10);
                everWentBack = zone.GetEntityPosition(m) == (10, 10);
            }
            Assert.IsTrue(everWentBack, "across 40 seeds the only legal direction was picked at least once");
        }

        [Test]
        public void Adversarial_TestRngNull_UsesDefault_NoThrow()
        {
            // Pollution guard: a fixture that forgets to set TestRng must
            // still get a working roll, not a null RNG.
            LiquidSlipSystem.TestRng = null;
            Author("ice", true, "50");
            var zone = new Zone("Z");
            var m = Creature(zone, 10, 10);
            zone.TileState.WriteCoating(11, 10, "ice", 4);

            Assert.DoesNotThrow(() => MovementSystem.TryMove(m, zone, 1, 0));
            Assert.AreEqual(1, Recs("SlipRolled").Count);
        }

        [Test]
        public void Adversarial_ForcedMoveOfANonCreatureOntoIce_NoRoll()
        {
            Author("ice", true, "100");
            var zone = new Zone("Z");
            var barrel = new Entity { ID = "barrel", BlueprintName = "Barrel" };
            barrel.AddPart(new RenderPart { DisplayName = "barrel" });
            zone.AddEntity(barrel, 10, 10);
            zone.TileState.WriteCoating(11, 10, "ice", 4);

            MovementSystem.ForceMoveTo(barrel, zone, 11, 10);   // a shoved barrel

            Assert.AreEqual((11, 10), zone.GetEntityPosition(barrel));
            Assert.AreEqual(0, Recs("SlipRolled").Count, "props do not slip");
        }
    }
}
