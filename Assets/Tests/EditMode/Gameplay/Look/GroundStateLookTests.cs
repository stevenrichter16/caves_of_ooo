using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Status-effects study fix 1 (Docs/STATUS-EFFECTS-STUDY-2026-08.md
    /// §1): tile-layer state — coatings, residues, energy, clouds — was
    /// written and map-rendered but invisible to every TEXT surface, so
    /// Jet Blast's water read as "empty ground" in look mode while the
    /// map painted a blue '~' on the same cell.
    ///
    /// <para>These tests pin the new "On the ground:" detail line in
    /// <see cref="LookQueryService.BuildSnapshot"/>, including the two
    /// traps the study's verifier flagged: permanent pool projections
    /// (Turns == int.MaxValue must not print a turns count) and the
    /// renderer's fog rule (never reveal state the glyph layer hides).</para>
    /// </summary>
    public class GroundStateLookTests
    {
        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown() => FactionManager.Reset();

        // ── Fixtures ─────────────────────────────────────────────────

        private static Entity Player(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "player", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.AddPart(new RenderPart { DisplayName = "you" });
            zone.AddEntity(e, x, y);
            return e;
        }

        /// <summary>The ground line only shows what the map shows —
        /// reveal the cell the way normal play does.</summary>
        private static void Reveal(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            cell.Explored = true;
            cell.IsVisible = true;
        }

        private static string GroundLine(LookSnapshot snapshot)
            => snapshot.DetailLines.FirstOrDefault(l => l.StartsWith("On the ground:"));

        // ── The reported bug ─────────────────────────────────────────

        [Test]
        public void WetTile_NamesTheWaterInLookMode()
        {
            var zone = new Zone("Z");
            var player = Player(zone, 5, 5);
            Reveal(zone, 7, 5);
            ZoneTileStateSystem.WriteCoating(zone, 7, 5, "water", 5, player, "test");

            var snapshot = LookQueryService.BuildSnapshot(player, zone, 7, 5);

            Assert.AreEqual("On the ground: water (5 turns)", GroundLine(snapshot),
                "the map paints a blue '~' here — look mode must be able to name it");
        }

        [Test]
        public void JetBlast_GroundWater_IsVisibleInLookMode_EndToEnd()
        {
            // The exact reported repro: cast Jet Blast at empty ground,
            // then look at a soaked cell.
            var zone = new Zone("Z");
            var caster = Player(zone, 5, 5);
            caster.AddPart(new ActivatedAbilitiesPart());
            caster.AddPart(new SkillsPart());
            Assert.IsTrue(caster.GetPart<SkillsPart>().AddSkill(new Hydromancy_JetBlast()));

            var cmd = GameEvent.New("CommandJetBlast");
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("RNG", (object)new System.Random(11));
            cmd.SetParameter("SourceCell", (object)zone.GetEntityCell(caster));
            cmd.SetParameter("DirectionX", 1);
            cmd.SetParameter("DirectionY", 0);
            caster.FireEvent(cmd);
            bool handled = cmd.Handled;
            cmd.Release();
            Assert.IsTrue(handled, "a miss still wets the ground — a real cast");

            Reveal(zone, 6, 5);
            var snapshot = LookQueryService.BuildSnapshot(caster, zone, 6, 5);

            string line = GroundLine(snapshot);
            Assert.IsNotNull(line, "Jet Blast's water must be nameable in look mode");
            StringAssert.Contains("water", line);
            StringAssert.Contains(Hydromancy_JetBlast.GroundTurns + " turns", line);
        }

        // ── The verifier's two traps ─────────────────────────────────

        [Test]
        public void PermanentPool_PrintsNoTurnsCount()
        {
            // River/pool projections are written with Turns = int.MaxValue
            // (ZoneTileState.Permanent). "water (2147483647 turns)" is
            // the failure this pins against.
            var zone = new Zone("Z");
            var player = Player(zone, 5, 5);
            Reveal(zone, 8, 5);
            zone.TileState.WriteCoating(8, 5, "water", ZoneTileState.Permanent);

            var snapshot = LookQueryService.BuildSnapshot(player, zone, 8, 5);

            Assert.AreEqual("On the ground: water", GroundLine(snapshot));
        }

        [Test]
        public void FoggedCell_RevealsNothing()
        {
            // Mirror of the renderer's rule (ZoneRenderer.PaintTileStateMark):
            // never reveal state through fog. Both halves of the gate.
            var zone = new Zone("Z");
            var player = Player(zone, 5, 5);
            ZoneTileStateSystem.WriteCoating(zone, 7, 5, "water", 5, player, "test");

            // Unexplored:
            var unexplored = LookQueryService.BuildSnapshot(player, zone, 7, 5);
            Assert.IsNull(GroundLine(unexplored), "unexplored cells stay dark");

            // Explored but not currently visible:
            var cell = zone.GetCell(7, 5);
            cell.Explored = true;
            cell.IsVisible = false;
            var remembered = LookQueryService.BuildSnapshot(player, zone, 7, 5);
            Assert.IsNull(GroundLine(remembered),
                "explored-but-fogged cells must not leak live state the map hides");
        }

        // ── Shape of the line ────────────────────────────────────────

        [Test]
        public void CleanTile_HasNoGroundLine()
        {
            var zone = new Zone("Z");
            var player = Player(zone, 5, 5);
            Reveal(zone, 7, 5);

            var snapshot = LookQueryService.BuildSnapshot(player, zone, 7, 5);

            Assert.IsNull(GroundLine(snapshot),
                "the common hover contributes zero lines (sparse store: absent = clean)");
        }

        [Test]
        public void ManyLayers_AllJoinedOnOneLine()
        {
            // Coatings coexist by design (water + oil on one tile), and
            // residues/energy stack alongside — the line must name ALL
            // of it, not just the glyph-priority winner.
            var zone = new Zone("Z");
            var player = Player(zone, 5, 5);
            Reveal(zone, 7, 5);
            ZoneTileStateSystem.WriteCoating(zone, 7, 5, "water", 5, player, "test");
            ZoneTileStateSystem.WriteCoating(zone, 7, 5, "oil", 8, player, "test");
            ZoneTileStateSystem.WriteResidue(zone, 7, 5, "embers", 3, player, "test");
            ZoneTileStateSystem.AddCharge(zone, 7, 5, 2, player, "test");

            var snapshot = LookQueryService.BuildSnapshot(player, zone, 7, 5);

            string line = GroundLine(snapshot);
            Assert.IsNotNull(line);
            StringAssert.Contains("water (5 turns)", line);
            StringAssert.Contains("oil (8 turns)", line);
            StringAssert.Contains("embers (3 turns)", line);
            StringAssert.Contains("charged", line);
        }
    }
}
