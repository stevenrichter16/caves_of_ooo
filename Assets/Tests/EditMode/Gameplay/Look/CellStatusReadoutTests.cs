using System.IO;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Status-effects study step 2 (Docs/STATUS-EFFECTS-STUDY-2026-08.md
    /// §6 Option 1): CellStatusReadout is THE read-only answer to "what
    /// is on this cell's ground?", consumed by look mode, the FOCUS
    /// panel, and the interact-menu text; TileStateCatalog is the
    /// display authority for tile-layer ids, backed by LiquidRegistry.
    ///
    /// <para>Also pins the new ice liquid definition as
    /// BEHAVIOR-NEUTRAL: before it existed, LiquidRegistry.Get("ice")
    /// returned null, which the tile systems treated as neither
    /// conductive nor flammable. The authored row must answer the same
    /// — the definition is a display gain, not a physics change.</para>
    /// </summary>
    public class CellStatusReadoutTests
    {
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            LiquidRegistry.ResetForTests();
        }

        [TearDown]
        public void TearDown() => LiquidRegistry.ResetForTests();

        private static void Reveal(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            cell.Explored = true;
            cell.IsVisible = true;
        }

        // ── TileStateCatalog ─────────────────────────────────────────

        [Test]
        public void Catalog_UnknownId_ReadsAsItself_NeverEmpty()
        {
            // The pre-broken guard: a brand-new tile id must ship with a
            // readable name, not a blank.
            Assert.AreEqual("embers", TileStateCatalog.DisplayName("embers"));
            Assert.AreEqual("mystery-goop", TileStateCatalog.DisplayName("mystery-goop"));
            Assert.AreEqual("", TileStateCatalog.DisplayName(null));
        }

        [Test]
        public void Catalog_UsesTheLiquidRegistryDisplayName()
        {
            LiquidRegistry.Initialize(
                "{\"Liquids\":[{\"Id\":\"test-syrup\",\"DisplayName\":\"golden syrup\"}]}");
            Assert.AreEqual("golden syrup", TileStateCatalog.DisplayName("test-syrup"));
            Assert.AreEqual("embers", TileStateCatalog.DisplayName("embers"),
                "non-liquid ids still read as themselves with the registry live");
        }

        // ── The ice definition ───────────────────────────────────────

        [Test]
        public void IceDefinition_Exists_AndIsBehaviorNeutral()
        {
            LiquidRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/LiquidDefinitions/ice.json")));

            var ice = LiquidRegistry.Get("ice");
            Assert.IsNotNull(ice, "ice finally has a liquid row");
            Assert.AreEqual("ice", ice.DisplayName);

            // Behavior-neutral: the null-definition era meant "neither
            // conductive nor flammable" — the authored row must agree.
            var zone = new Zone("Z");
            zone.TileState.WriteCoating(5, 5, "ice", 10);
            Assert.IsFalse(TilePropagationSystem.IsConductive(zone, 5, 5),
                "an ice tile did not conduct before the definition existed");
            Assert.IsFalse(TilePropagationSystem.IsFlammable(zone, 5, 5),
                "an ice tile did not burn before the definition existed");
        }

        // ── GroundSummary (the compact form) ─────────────────────────

        [Test]
        public void GroundSummary_NamesWithoutCounts()
        {
            var zone = new Zone("Z");
            Reveal(zone, 7, 5);
            zone.TileState.WriteCoating(7, 5, "water", 5);
            zone.TileState.WriteResidue(7, 5, "embers", 3);

            Assert.AreEqual("water, embers",
                CellStatusReadout.GroundSummary(zone, zone.GetCell(7, 5), 7, 5));
        }

        [Test]
        public void GroundSummary_RespectsFogAndAbsence()
        {
            var zone = new Zone("Z");
            zone.TileState.WriteCoating(7, 5, "water", 5);
            Assert.IsNull(CellStatusReadout.GroundSummary(zone, zone.GetCell(7, 5), 7, 5),
                "fogged cells reveal nothing");

            Reveal(zone, 9, 5);
            Assert.IsNull(CellStatusReadout.GroundSummary(zone, zone.GetCell(9, 5), 9, 5),
                "clean cells say nothing");
        }

        // ── The interact-menu text (the 'c' half of the reported bug) ─

        [Test]
        public void DescribeCell_EmptyWetCell_NamesTheWater()
        {
            var zone = new Zone("Z");
            Reveal(zone, 7, 5);
            zone.TileState.WriteCoating(7, 5, "water", 5);

            Assert.AreEqual("You see water on the ground.",
                WorldInteractionSystem.DescribeCell(zone.GetCell(7, 5), zone));
        }

        [Test]
        public void DescribeCell_FoggedWetCell_StaysDark()
        {
            var zone = new Zone("Z");
            zone.TileState.WriteCoating(7, 5, "water", 5);

            Assert.AreEqual("You see nothing here.",
                WorldInteractionSystem.DescribeCell(zone.GetCell(7, 5), zone));
        }

        [Test]
        public void DescribeCell_WithAnEntity_AppendsUnderfoot()
        {
            // A named entity keeps its title AND the ground still
            // speaks. (The first version of this test pinned the
            // opposite — ground text only on empty cells — and the very
            // first playtest broke it: terrain lives on nearly every
            // cell, so "empty only" meant "never".)
            var zone = new Zone("Z");
            Reveal(zone, 7, 5);
            zone.TileState.WriteCoating(7, 5, "water", 5);
            var rock = new Entity { ID = "rock", BlueprintName = "Rock" };
            rock.AddPart(new RenderPart { DisplayName = "rock" });
            zone.AddEntity(rock, 7, 5);

            string text = WorldInteractionSystem.DescribeCell(zone.GetCell(7, 5), zone);
            StringAssert.Contains("rock", text);
            StringAssert.Contains("(water underfoot)", text);
        }

        [Test]
        public void DescribeCell_TerrainCell_NamesTheWater_TheLiveRepro()
        {
            // The exact reported repro: Jet Blast water on a GRASS cell,
            // 'c' menu said "You see the grass." and nothing else.
            var zone = new Zone("Z");
            Reveal(zone, 7, 5);
            zone.TileState.WriteCoating(7, 5, "water", 6);
            var grass = new Entity { ID = "grass", BlueprintName = "Grass" };
            grass.Tags["Terrain"] = "";
            grass.AddPart(new RenderPart { DisplayName = "grass" });
            zone.AddEntity(grass, 7, 5);

            Assert.AreEqual("You see the grass. (water underfoot)",
                WorldInteractionSystem.DescribeCell(zone.GetCell(7, 5), zone));
        }

        // ── Entity afflictions: one wording source, two lengths ──────

        private static Entity Viper(Zone zone, int x, int y)
        {
            var v = new Entity { ID = "viper", BlueprintName = "Viper" };
            v.Tags["Creature"] = "";
            v.AddPart(new RenderPart { DisplayName = "viper" });
            v.Statistics["Hitpoints"] = new Stat
            { Owner = v, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            zone.AddEntity(v, x, y);
            return v;
        }

        [Test]
        public void AfflictionSummary_SharesWordingWithTheLongForm()
        {
            // The divergence contract: the compact form is derived from
            // EffectDescriber's own lines, so a label in the summary
            // must appear verbatim at the head of a long-form line.
            var zone = new Zone("Z");
            var viper = Viper(zone, 7, 5);
            viper.ApplyEffect(new FrozenEffect(cold: 0.6f));
            viper.ApplyEffect(new WetEffect(0.5f));

            string summary = CellStatusReadout.AfflictionSummary(viper);
            var lines = new System.Collections.Generic.List<string>();
            CellStatusReadout.AppendAfflictionLines(viper, lines);
            string joined = string.Join(" | ", lines).ToLowerInvariant();

            Assert.AreEqual("frozen over, soaked", summary);
            foreach (var label in summary.Split(new[] { ", " }, System.StringSplitOptions.None))
                StringAssert.Contains(label, joined,
                    "every short label must exist inside the long form — one wording source");
        }

        [Test]
        public void AfflictionSummary_CapsWithPlusN()
        {
            var zone = new Zone("Z");
            var viper = Viper(zone, 7, 5);
            viper.ApplyEffect(new FrozenEffect(cold: 0.6f));
            viper.ApplyEffect(new WetEffect(0.5f));
            viper.ApplyEffect(new HobbledEffect(3));

            Assert.AreEqual("frozen over, soaked, +1",
                CellStatusReadout.AfflictionSummary(viper));
        }

        // ── The menu title: same readout as look mode ────────────────

        [Test]
        public void MenuTitle_NamesTheFrozenStatus_TheViperRepro()
        {
            // The reported bug: a frozen viper showed "Frozen over" in
            // look mode and NOTHING in the 'c' menu.
            var zone = new Zone("Z");
            Reveal(zone, 7, 5);
            var viper = Viper(zone, 7, 5);
            viper.ApplyEffect(new FrozenEffect(cold: 0.6f));

            string title = CavesOfOoo.Rendering.WorldActionMenuUI
                .BuildTitleFor(zone.GetCell(7, 5), viper, zone);

            StringAssert.Contains("viper", title);
            StringAssert.Contains("HP", title);
            StringAssert.Contains("frozen over", title);
        }

        [Test]
        public void MenuTitle_DropsWholeGroupsByPriority_NeverMidCuts()
        {
            // The title row clips at 44 chars; suffix groups must drop
            // WHOLE, in priority order (afflictions beat ground): a
            // frozen viper on wet ground names "frozen over" and drops
            // "underfoot" entirely rather than clipping mid-word.
            var zone = new Zone("Z");
            Reveal(zone, 7, 5);
            zone.TileState.WriteCoating(7, 5, "water", 5);
            var viper = Viper(zone, 7, 5);
            viper.ApplyEffect(new FrozenEffect(cold: 0.6f));

            string title = CavesOfOoo.Rendering.WorldActionMenuUI
                .BuildTitleFor(zone.GetCell(7, 5), viper, zone);

            StringAssert.Contains("frozen over", title,
                "the target's own status outranks ground state");
            StringAssert.DoesNotContain("underfoot", title,
                "the group that does not fit drops whole");
            Assert.LessOrEqual(title.Length, 44, "never hand the renderer a mid-cut");
        }

        [Test]
        public void MenuTitle_GroundStillShowsOnTerrainTargets()
        {
            // The grass repro, now pinned through the TITLE path.
            var zone = new Zone("Z");
            Reveal(zone, 7, 5);
            zone.TileState.WriteCoating(7, 5, "water", 6);
            var grass = new Entity { ID = "grass", BlueprintName = "Grass" };
            grass.Tags["Terrain"] = "";
            grass.AddPart(new RenderPart { DisplayName = "grass" });
            zone.AddEntity(grass, 7, 5);

            string title = CavesOfOoo.Rendering.WorldActionMenuUI
                .BuildTitleFor(zone.GetCell(7, 5), grass, zone);

            Assert.AreEqual("You see the grass. (water underfoot)", title);
        }

        // ── Delegation: look mode reads the same readout ─────────────

        [Test]
        public void LookMode_GroundLine_ComesFromTheReadout()
        {
            var zone = new Zone("Z");
            var player = new Entity { ID = "p", BlueprintName = "Player" };
            player.Tags["Creature"] = "";
            player.Tags["Player"] = "";
            player.AddPart(new RenderPart { DisplayName = "you" });
            zone.AddEntity(player, 5, 5);
            Reveal(zone, 7, 5);
            zone.TileState.WriteCoating(7, 5, "water", 5);

            string direct = CellStatusReadout.GroundLine(zone, zone.GetCell(7, 5), 7, 5);
            var snapshot = LookQueryService.BuildSnapshot(player, zone, 7, 5);

            Assert.AreEqual("On the ground: water (5 turns)", direct);
            Assert.Contains(direct, (System.Collections.ICollection)snapshot.DetailLines,
                "look mode and the readout must agree — one answer, every surface");
        }
    }
}
