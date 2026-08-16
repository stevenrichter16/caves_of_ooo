using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Docs/LIQUID-SLIP.md — the <c>Slippery</c> flag wakes up. Qud's
    /// slide (BaseLiquid.ObjectEnteredCell: step onto a slippery cell,
    /// fail the roll, get flung one random cell) with a flat per-liquid
    /// <c>SlipChance</c> instead of the Agility save (user decision).
    ///
    /// <para>Outcomes are forced by AUTHORING the chance (0 / 100 on a
    /// scratch registry row), not by hunting RNG seeds — a bare
    /// <c>Next(100) &lt; chance</c> has no seed to argue about at the
    /// extremes. Seeds are used only where the DIRECTION matters.</para>
    /// </summary>
    public class LiquidSlipTests
    {
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            LiquidRegistry.ResetForTests();
            var liquidJson = new System.Collections.Generic.List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(
                Application.dataPath, "Resources/Content/Data/LiquidDefinitions"), "*.json"))
                liquidJson.Add(File.ReadAllText(f));
            LiquidRegistry.InitializeFromJsonSources(liquidJson);
            LiquidSlipSystem.TestRng = new System.Random(1);
        }

        [TearDown]
        public void TearDown()
        {
            LiquidSlipSystem.TestRng = null;
            LiquidRegistry.ResetForTests();
        }

        // ── Fixtures ─────────────────────────────────────────────────

        /// <summary>Overlay a scratch liquid row so a test can dial the
        /// odds without touching shipped content. Later sources win on
        /// Id collision, so this replaces the real row for the test.</summary>
        private static void AuthorLiquid(string id, bool slippery, int chance)
        {
            var json = "{\"Liquids\":[{\"Id\":\"" + id + "\",\"DisplayName\":\"" + id
                + "\",\"Slippery\":" + (slippery ? "true" : "false")
                + ",\"SlipChance\":" + chance + "}]}";
            var all = new System.Collections.Generic.List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(
                Application.dataPath, "Resources/Content/Data/LiquidDefinitions"), "*.json"))
                all.Add(File.ReadAllText(f));
            all.Add(json);
            LiquidRegistry.InitializeFromJsonSources(all);
        }

        private static Entity Creature(Zone zone, int x, int y, string name = "snapjaw")
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

        private static Entity Wall(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "wall" + x + "_" + y, BlueprintName = "Wall" };
            e.Tags["Solid"] = "";
            e.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity Item(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "rock" + x + "_" + y, BlueprintName = "Rock" };
            e.AddPart(new RenderPart { DisplayName = "rock" });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static void Ice(Zone zone, int x, int y) => zone.TileState.WriteCoating(x, y, "ice", 4);

        private static int Chebyshev((int x, int y) a, (int x, int y) b)
            => System.Math.Max(System.Math.Abs(a.x - b.x), System.Math.Abs(a.y - b.y));

        private static System.Collections.Generic.IReadOnlyList<Diag.Entry> Recs(string kind)
            => DiagQuery.Apply(new DiagQuery.Filter { Category = "liquid", Kind = kind, Limit = 50 }).Records;

        // ════════════════════════════════════════════════════════════
        // 1. Data
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SlipChance_DefaultsTo50_WhenAbsentFromJson()
        {
            LiquidRegistry.Initialize("{\"Liquids\":[{\"Id\":\"scratch\",\"Slippery\":true}]}");
            Assert.AreEqual(50, LiquidRegistry.Get("scratch").SlipChance,
                "a row that says slippery must BE slippery — default is not 0");
        }

        [Test]
        public void IceIsSlippery_WaterIsNot()
        {
            Assert.IsTrue(LiquidRegistry.Get("ice").Slippery, "ice.json flipped to Slippery:true");
            Assert.AreEqual(50, LiquidRegistry.Get("ice").SlipChance);
            Assert.IsFalse(LiquidRegistry.Get("water").Slippery,
                "Qud: water slips only when frozen — and frozen water is the ice row here");
            Assert.IsTrue(LiquidRegistry.Get("oil").Slippery);
            Assert.AreEqual(40, LiquidRegistry.Get("oil").SlipChance);
        }

        // ════════════════════════════════════════════════════════════
        // 2-4. The mechanic + counter-checks
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SteppingOntoIce_AtChance100_SlidesOneRandomCell()
        {
            AuthorLiquid("ice", slippery: true, chance: 100);
            var zone = new Zone("Z");
            var mover = Creature(zone, 10, 10);
            Ice(zone, 11, 10);

            Assert.IsTrue(MovementSystem.TryMove(mover, zone, 1, 0), "the step itself succeeds");

            var pos = zone.GetEntityPosition(mover);
            Assert.AreNotEqual((11, 10), pos, "flung off the ice cell");
            Assert.AreEqual(1, Chebyshev(pos, (11, 10)), "by exactly one cell, any of the 8");
            Assert.IsTrue(MessageLog.GetRecent(5).Any(m => m.Contains("slips on the ice")),
                "message: " + string.Join(" | ", MessageLog.GetRecent(5)));
        }

        [Test]
        public void SteppingOntoIce_AtChance0_StaysPut()
        {
            AuthorLiquid("ice", slippery: true, chance: 0);
            var zone = new Zone("Z");
            var mover = Creature(zone, 10, 10);
            Ice(zone, 11, 10);

            Assert.IsTrue(MovementSystem.TryMove(mover, zone, 1, 0));

            Assert.AreEqual((11, 10), zone.GetEntityPosition(mover),
                "counter-check: Slippery:true with chance 0 is legal cosmetic-slippery");
            Assert.AreEqual(1, Recs("SlipRolled").Count, "the roll still happened (and was diag'd)");
            Assert.AreEqual(0, Recs("Slipped").Count);
        }

        [Test]
        public void SteppingOntoWater_NeverSlips_EvenAtChance100()
        {
            AuthorLiquid("water", slippery: false, chance: 100);
            var zone = new Zone("Z");
            var mover = Creature(zone, 10, 10);
            zone.TileState.WriteCoating(11, 10, "water", 6);

            Assert.IsTrue(MovementSystem.TryMove(mover, zone, 1, 0));

            Assert.AreEqual((11, 10), zone.GetEntityPosition(mover),
                "counter-check: the Slippery gate beats the number");
            Assert.AreEqual(0, Recs("SlipRolled").Count, "no gate, no roll");
        }

        // ════════════════════════════════════════════════════════════
        // 5-7. Universes and gates
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SteppingOntoOilPoolEntity_CanSlip()
        {
            AuthorLiquid("oil", slippery: true, chance: 100);
            var zone = new Zone("Z");
            var mover = Creature(zone, 10, 10);
            var pool = new Entity { ID = "pool", BlueprintName = "OilPool" };
            pool.AddPart(new RenderPart { DisplayName = "oil pool" });
            pool.AddPart(new LiquidPoolPart { LiquidId = "oil", Volume = 10 });
            zone.AddEntity(pool, 11, 10);

            Assert.IsTrue(MovementSystem.TryMove(mover, zone, 1, 0));

            // A pool ENTITY is mirrored into the tile layer by
            // Zone.ProjectPool (Zone.cs, Palimpsest P2b), so the resolver
            // reads one place and a puddle slips exactly like a spill —
            // and rolls ONCE, not once per universe.
            Assert.AreNotEqual((11, 10), zone.GetEntityPosition(mover), "a pool entity slips too");
            var rolled = Recs("SlipRolled");
            Assert.AreEqual(1, rolled.Count, "one roll per move");
            StringAssert.Contains("\"liquidId\":\"oil\"", rolled[0].PayloadJson);
        }

        [Test]
        public void SteppingOntoDryGround_NoRollAtAll()
        {
            var zone = new Zone("Z");
            var mover = Creature(zone, 10, 10);

            Assert.IsTrue(MovementSystem.TryMove(mover, zone, 1, 0));

            Assert.AreEqual(0, Recs("SlipRolled").Count, "the early-out is real — dry ground never rolls");
        }

        [Test]
        public void AnItemMovedOntoIce_DoesNotSlip()
        {
            AuthorLiquid("ice", slippery: true, chance: 100);
            var zone = new Zone("Z");
            var rock = Item(zone, 10, 10);
            Ice(zone, 11, 10);

            Assert.IsTrue(MovementSystem.ForceMoveTo(rock, zone, 11, 10));

            Assert.AreEqual((11, 10), zone.GetEntityPosition(rock), "only creatures have feet to lose");
            Assert.AreEqual(0, Recs("SlipRolled").Count);
        }

        // ════════════════════════════════════════════════════════════
        // 8-11. Where the slide can go
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SlideIntoAWall_SlipsButDoesNotMove()
        {
            AuthorLiquid("ice", slippery: true, chance: 100);
            var zone = new Zone("Z");
            var mover = Creature(zone, 10, 10);
            Ice(zone, 11, 10);
            // Box the ice cell in: walls on all 8 neighbours, INCLUDING
            // the origin (a wall sharing the mover's cell doesn't stop it
            // leaving — ForceMoveTo skips BeforeMove and only the
            // destination is checked). Whatever direction the slide
            // picks, it is illegal.
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (!(dx == 0 && dy == 0))
                        Wall(zone, 11 + dx, 10 + dy);

            Assert.IsTrue(MovementSystem.ForceMoveTo(mover, zone, 11, 10));

            Assert.AreEqual((11, 10), zone.GetEntityPosition(mover), "nowhere to slide — position holds");
            Assert.IsTrue(MessageLog.GetRecent(5).Any(m => m.Contains("slips on the ice")),
                "but the slip still HAPPENED: " + string.Join(" | ", MessageLog.GetRecent(5)));
            var slipped = Recs("Slipped");
            Assert.AreEqual(1, slipped.Count);
            StringAssert.Contains("\"displaced\":false", slipped[0].PayloadJson);
        }

        [Test]
        public void SlideOntoMoreIce_ChainsButStopsAtThree()
        {
            AuthorLiquid("ice", slippery: true, chance: 100);
            var zone = new Zone("Z");
            var mover = Creature(zone, 2, 10);
            // A wide sheet so every random direction lands on more ice.
            for (int x = 0; x < 20; x++)
                for (int y = 5; y < 16; y++)
                    Ice(zone, x, y);

            Assert.IsTrue(MovementSystem.TryMove(mover, zone, 1, 0));

            var slipped = Recs("Slipped");
            Assert.AreEqual(LiquidSlipSystem.MAX_SLIDE_CHAIN, slipped.Count,
                "chance 100 on an ice sheet slides every time — only the cap stops it");
            Assert.LessOrEqual(Chebyshev(zone.GetEntityPosition(mover), (3, 10)),
                LiquidSlipSystem.MAX_SLIDE_CHAIN,
                "net displacement from the first landing is bounded by the cap");
            Assert.AreEqual(LiquidSlipSystem.MAX_SLIDE_CHAIN, Recs("SlipRolled").Count,
                "the step rolls, slide 1 rolls, slide 2 rolls; the landing of slide 3 is refused a roll — 3, not 4");
        }

        [Test]
        public void SlideNeverLandsOnAnotherCreature()
        {
            AuthorLiquid("ice", slippery: true, chance: 100);
            var zone = new Zone("Z");
            var mover = Creature(zone, 10, 10, "mover");
            Ice(zone, 11, 10);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (!(dx == 0 && dy == 0) && !(dx == -1 && dy == 0))
                        Creature(zone, 11 + dx, 10 + dy, "bystander");
            Creature(zone, 10, 10, "filler"); // the origin fills too — 8/8 occupied

            Assert.IsTrue(MovementSystem.ForceMoveTo(mover, zone, 11, 10));

            Assert.AreEqual((11, 10), zone.GetEntityPosition(mover), "every neighbour holds a creature");
            foreach (var e in zone.GetAllEntities())
                if (e != mover && e.HasTag("Creature"))
                    Assert.AreNotEqual(zone.GetEntityPosition(mover), zone.GetEntityPosition(e));
            StringAssert.Contains("\"displaced\":false", Recs("Slipped")[0].PayloadJson);
        }

        [Test]
        public void KnockbackOntoIce_RollsOnce()
        {
            AuthorLiquid("ice", slippery: true, chance: 0); // roll happens, never slides — count it cleanly
            var zone = new Zone("Z");
            var brute = Creature(zone, 9, 10, "brute");
            var victim = Creature(zone, 10, 10, "victim");
            Ice(zone, 11, 10);

            Assert.IsTrue(CavesOfOoo.Skills.SkillCombatHelpers.TryPush(brute, victim, zone, 1));

            Assert.AreEqual((11, 10), zone.GetEntityPosition(victim));
            var rolled = Recs("SlipRolled");
            Assert.AreEqual(1, rolled.Count, "a shove onto ice is a step onto ice");
            StringAssert.Contains("\"chainDepth\":0", rolled[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        // 12-15. Observability, NPCs, the turn
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SlipRolled_ChanceInPayload_MatchesTheJson()
        {
            AuthorLiquid("ice", slippery: true, chance: 37);
            var zone = new Zone("Z");
            var mover = Creature(zone, 10, 10);
            Ice(zone, 11, 10);

            MovementSystem.TryMove(mover, zone, 1, 0);

            var rolled = Recs("SlipRolled");
            Assert.AreEqual(1, rolled.Count);
            StringAssert.Contains("\"chance\":37", rolled[0].PayloadJson);
            StringAssert.Contains("\"liquidId\":\"ice\"", rolled[0].PayloadJson);
        }

        [Test]
        public void NpcStepGoal_OntoIce_Slips()
        {
            AuthorLiquid("ice", slippery: true, chance: 100);
            var zone = new Zone("Z");
            var npc = Creature(zone, 10, 10);
            var brain = new BrainPart { CurrentZone = zone, Rng = new System.Random(1) };
            npc.AddPart(brain);
            Ice(zone, 11, 10);

            var goal = new StepGoal(1, 0);
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsTrue(goal.Finished(), "the step succeeded from the goal's point of view");
            Assert.AreNotEqual((11, 10), zone.GetEntityPosition(npc), "NPCs are not exempt");
            Assert.AreEqual(1, Recs("Slipped").Count);
        }

        [Test]
        public void ASlip_IsOneMoveToTheCaller_AndTwoAfterMovesToTheMover()
        {
            // The C5 reason the slip is post-move: the caller (InputHandler /
            // StepGoal) sees ONE successful move and spends ONE turn; the
            // displacement is a Forced AfterMove inside that same move.
            AuthorLiquid("ice", slippery: true, chance: 100);
            var zone = new Zone("Z");
            var mover = Creature(zone, 10, 10);
            Ice(zone, 11, 10);
            int afterMoves = 0, forced = 0;
            mover.AddPart(new AfterMoveCounter(() => afterMoves++, () => forced++));

            var (moved, blockedBy) = MovementSystem.TryMoveEx(mover, zone, 1, 0);

            Assert.IsTrue(moved);
            Assert.IsNull(blockedBy);
            Assert.AreEqual(2, afterMoves, "step + slide");
            Assert.AreEqual(1, forced, "the slide is the forced one");
        }

        private sealed class AfterMoveCounter : Part
        {
            private readonly System.Action _any, _forced;
            public AfterMoveCounter(System.Action any, System.Action forced) { _any = any; _forced = forced; }
            public override string Name => "AfterMoveCounter";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "AfterMove") return true;
                _any();
                if (e.HasParameter("Forced") && e.GetParameter<bool>("Forced")) _forced();
                return true;
            }
        }

        [Test]
        public void SlipRolled_OnEveryRoll_Slipped_OnlyOnFailure()
        {
            AuthorLiquid("ice", slippery: true, chance: 100);
            var zone = new Zone("Z");
            var mover = Creature(zone, 10, 10);
            Ice(zone, 11, 10);

            MovementSystem.TryMove(mover, zone, 1, 0);

            Assert.GreaterOrEqual(Recs("SlipRolled").Count, 1);
            Assert.GreaterOrEqual(Recs("Slipped").Count, 1);
            var s = Recs("Slipped")[0].PayloadJson;
            StringAssert.Contains("\"fromX\":11", s);
            StringAssert.Contains("\"fromY\":10", s);
            StringAssert.Contains("\"liquidId\":\"ice\"", s);
        }

        // ════════════════════════════════════════════════════════════
        // 16. Readout
        // ════════════════════════════════════════════════════════════

        [Test]
        public void GroundLine_SaysSlippery()
        {
            var zone = new Zone("Z");
            Ice(zone, 5, 5);
            zone.TileState.WriteCoating(6, 5, "water", 6);
            foreach (var c in new[] { zone.GetCell(5, 5), zone.GetCell(6, 5) })
            { c.Explored = true; c.IsVisible = true; }

            StringAssert.Contains("ice (4 turns, slippery)", CellStatusReadout.GroundLine(zone, zone.GetCell(5, 5)));
            StringAssert.DoesNotContain("slippery", CellStatusReadout.GroundLine(zone, zone.GetCell(6, 5)),
                "counter-check: water is not flagged");
        }
    }
}
