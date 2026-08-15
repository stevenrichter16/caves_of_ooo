using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// CHARACTERIZATION NET for the cell-status readout subsystem — written
    /// GREEN against current code BEFORE the simplification pass
    /// (Docs/STATUS-EFFECTS-STUDY-2026-08.md; adversarial review
    /// 2026-08-15). Each test pins a behavior the review found UNPINNED,
    /// so the refactors that follow cannot move it silently.
    ///
    /// <para>G1 also serves as the DURABLE PROOF for a reachability
    /// assumption: several text paths (the picker's no-target message)
    /// only fire when <c>ResolveTarget</c> returns null. G1 pins that
    /// null means "empty cell" and nothing else — if target resolution
    /// ever changes, this fails loudly instead of the messages silently
    /// degrading.</para>
    /// </summary>
    public class CellStatusCharacterizationTests
    {
        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
            foreach (var input in UnityEngine.Object.FindObjectsOfType<InputHandler>())
                UnityEngine.Object.DestroyImmediate(input.gameObject);
            foreach (var menu in UnityEngine.Object.FindObjectsOfType<WorldActionMenuUI>())
                UnityEngine.Object.DestroyImmediate(menu.gameObject);
        }

        // ── Fixtures ─────────────────────────────────────────────────

        private static void Reveal(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            cell.Explored = true;
            cell.IsVisible = true;
        }

        private static Entity Thing(Zone zone, string name, int x, int y, bool terrain = false)
        {
            var e = new Entity { ID = name + x + "_" + y, BlueprintName = name };
            e.AddPart(new RenderPart { DisplayName = name });
            if (terrain) e.Tags["Terrain"] = "";
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity Creature(Zone zone, string name, int x, int y)
        {
            var e = Thing(zone, name, x, y);
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            return e;
        }

        // ── G1: ResolveTarget's only-null-when-empty contract ────────

        [Test]
        public void G1_ResolveTarget_NullOnlyForEmptyCells()
        {
            var zone = new Zone("Z");
            Assert.IsNull(WorldInteractionSystem.ResolveTarget(zone.GetCell(1, 1)),
                "empty cell resolves null");

            Thing(zone, "grass", 2, 1, terrain: true);
            Assert.IsNotNull(WorldInteractionSystem.ResolveTarget(zone.GetCell(2, 1)),
                "terrain-only cell RESOLVES (terrain is a target)");

            Thing(zone, "rock", 3, 1);
            Assert.IsNotNull(WorldInteractionSystem.ResolveTarget(zone.GetCell(3, 1)),
                "single non-terrain resolves");

            Thing(zone, "grass", 4, 1, terrain: true);
            var rock = Thing(zone, "rock", 4, 1);
            Assert.AreSame(rock, WorldInteractionSystem.ResolveTarget(zone.GetCell(4, 1)),
                "mixed cell resolves the non-terrain over terrain");

            Thing(zone, "rock", 5, 1);
            Thing(zone, "coin", 5, 1);
            Assert.IsNotNull(WorldInteractionSystem.ResolveTarget(zone.GetCell(5, 1)),
                "pile cell resolves");
        }

        // ── G2/G3: Collect vocabulary + singular forms ───────────────

        [Test]
        public void G2_GroundLine_EnergyAndCloudVocabulary()
        {
            var zone = new Zone("Z");
            Reveal(zone, 7, 5);
            zone.TileState.AddHeat(7, 5, 1);
            zone.TileState.AddCold(7, 5, 1);
            zone.TileState.AddCharge(7, 5, 1);
            zone.TileState.WriteCloud(7, 5, "smoke", 3);

            string line = CellStatusReadout.GroundLine(zone, zone.GetCell(7, 5), 7, 5);

            Assert.AreEqual("On the ground: hot, cold, charged, smoke cloud (3 turns)", line);
        }

        [Test]
        public void G3_SingularTurn_ForCoatingAndCloud()
        {
            var zone = new Zone("Z");
            Reveal(zone, 7, 5);
            zone.TileState.WriteCoating(7, 5, "water", 1);
            zone.TileState.WriteCloud(7, 5, "steam", 1);

            string line = CellStatusReadout.GroundLine(zone, zone.GetCell(7, 5), 7, 5);

            Assert.AreEqual("On the ground: water (1 turn), steam cloud (1 turn)", line);
        }

        // ── G4: summary tokens are the line's tokens, same order ─────

        [Test]
        public void G4_GroundSummary_IsGroundLineWithoutCounts()
        {
            var zone = new Zone("Z");
            Reveal(zone, 7, 5);
            zone.TileState.WriteCoating(7, 5, "oil", 8);
            zone.TileState.WriteCoating(7, 5, "water", 5);
            zone.TileState.WriteResidue(7, 5, "embers", 3);
            zone.TileState.AddCharge(7, 5, 2);
            zone.TileState.WriteCloud(7, 5, "smoke", 2);
            var cell = zone.GetCell(7, 5);

            string line = CellStatusReadout.GroundLine(zone, cell, 7, 5)
                .Substring("On the ground: ".Length);
            string summary = CellStatusReadout.GroundSummary(zone, cell, 7, 5);

            // Strip "(N turns)" groups from the line: what remains must be
            // exactly the summary, token for token, in order.
            string stripped = System.Text.RegularExpressions.Regex
                .Replace(line, @" \(\d+ turns?\)", "");
            Assert.AreEqual(stripped, summary);
        }

        // ── G5: the empty-wet-cell interact message, end to end ──────

        [Test]
        public void G5_InteractKey_OnEmptyWetCell_LogsWaterOnTheGround()
        {
            var zone = new Zone("Z");
            var player = Creature(zone, "you", 10, 10);
            player.Tags["Player"] = "";
            player.Statistics["Speed"] = new Stat
            { Owner = player, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            Reveal(zone, 11, 10);
            zone.TileState.WriteCoating(11, 10, "water", 6);
            Assert.IsNull(WorldInteractionSystem.ResolveTarget(zone.GetCell(11, 10)),
                "precondition: the target cell is empty (only tile state)");

            var inputGo = new GameObject("InputHandler");
            var input = inputGo.AddComponent<InputHandler>();
            input.PlayerEntity = player;
            input.CurrentZone = zone;
            var menuGo = new GameObject("WorldActionMenuUI");
            input.WorldActionMenuUI = menuGo.AddComponent<WorldActionMenuUI>();

            MessageLog.Clear();
            MethodInfo open = typeof(InputHandler).GetMethod(
                "OpenWorldActionMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            open.Invoke(input, new object[] { 11, 10 });

            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m == "You see water on the ground."),
                "'c' at an empty wet cell must name the water: "
                + string.Join(" | ", MessageLog.GetMessages()));
        }

        // ── G6/G7: menu builders' null tolerances ────────────────────

        [Test]
        public void G6_StatusBlock_NullZone_KeepsAfflictionsDropsGround()
        {
            var zone = new Zone("Z");
            Reveal(zone, 7, 5);
            zone.TileState.WriteCoating(7, 5, "water", 5);
            var v = Creature(zone, "viper", 7, 5);
            v.ApplyEffect(new FrozenEffect(cold: 0.6f));

            var lines = WorldActionMenuUI.BuildStatusLinesFor(v, zone.GetCell(7, 5), zone: null);

            Assert.IsTrue(lines.Exists(l => l.Contains("Frozen over")));
            Assert.IsFalse(lines.Exists(l => l.StartsWith("On the ground:")),
                "no zone, no ground line — never a throw");
        }

        [Test]
        public void G7_Title_NullTarget_IsCellTextOnly()
        {
            var zone = new Zone("Z");
            Thing(zone, "grass", 7, 5, terrain: true);

            string title = WorldActionMenuUI.BuildTitleFor(zone.GetCell(7, 5), null);

            Assert.AreEqual("You see the grass.", title, "no target, no [HP] suffix");
        }

        // ── S3 gap: afflictions reach the look snapshot ──────────────

        [Test]
        public void S3_LookSnapshot_CarriesAfflictionLines()
        {
            // The adversarial review found NO test pinned afflictions in
            // BuildSnapshot's DetailLines — an inline that dropped the call
            // would have passed the whole suite.
            var zone = new Zone("Z");
            var player = Creature(zone, "you", 5, 5);
            player.Tags["Player"] = "";
            var v = Creature(zone, "viper", 7, 5);
            v.ApplyEffect(new FrozenEffect(cold: 0.6f));

            var snapshot = LookQueryService.BuildSnapshot(player, zone, 7, 5);

            Assert.IsTrue(snapshot.DetailLines.Contains("Afflicted:"));
            Assert.IsTrue(snapshot.DetailLines.Any(l => l.Contains("Frozen over")));
        }
    }
}
