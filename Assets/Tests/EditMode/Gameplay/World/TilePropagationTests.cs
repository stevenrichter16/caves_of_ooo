using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// PALIMPSEST P4 — one local action changes another part of the room.
    ///
    /// <para>The phase's POC question, and the vignette that motivated
    /// the whole design: arc into a puddle, the charge finds the copper
    /// pipe, and something four tiles away happens. These pin that the
    /// spread is real, that it is bounded, and that it stops at
    /// materials that do not carry.</para>
    /// </summary>
    public class TilePropagationTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            LiquidRegistry.ResetForTests();
            // Conductivity/combustibility are read from the SHIPPED
            // liquid definitions, so the tests load the real files
            // rather than a fixture — a divergence between the two would
            // be exactly the bug worth catching.
            var liquidJson = new System.Collections.Generic.List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(
                Application.dataPath, "Resources/Content/Data/LiquidDefinitions"), "*.json"))
                liquidJson.Add(File.ReadAllText(f));
            LiquidRegistry.InitializeFromJsonSources(liquidJson);
            TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
        }

        [TearDown]
        public void TearDown()
        {
            TileReactionSystem.ResetForTests();
            LiquidRegistry.ResetForTests();
        }

        private static Entity Grate(Zone zone, int x, int y)
        {
            var e = new Entity { ID = $"grate{x}_{y}", BlueprintName = "MetalGrate" };
            e.AddPart(new RenderPart { DisplayName = "metal grate" });
            e.AddPart(new MaterialPart { Conductivity = 100, Combustibility = 0 });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity Creature(Zone zone, string name, int x, int y, int hp = 200)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["ElectricResistance"] = new Stat { Owner = e, Name = "ElectricResistance", BaseValue = 0, Min = -100, Max = 100 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        // ── What conducts ────────────────────────────────────────

        [Test]
        public void WaterConducts_OilDoesNot()
        {
            // Read straight off the shipped LiquidDefinitions — water is
            // Conductivity 100, oil is 0. No parallel substrate table
            // was built precisely so these cannot disagree.
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.WriteCoating(6, 5, "oil", 8);

            Assert.IsTrue(TilePropagationSystem.IsConductive(zone, 5, 5));
            Assert.IsFalse(TilePropagationSystem.IsConductive(zone, 6, 5),
                "oil insulates — which is what makes it a different tool");
        }

        [Test]
        public void MetalConducts_BareGroundDoesNot()
        {
            var zone = new Zone();
            Grate(zone, 5, 5);

            Assert.IsTrue(TilePropagationSystem.IsConductive(zone, 5, 5));
            Assert.IsFalse(TilePropagationSystem.IsConductive(zone, 9, 9),
                "an empty floor tile carries nothing");
        }

        [Test]
        public void OilBurns_WaterDoesNot()
        {
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "oil", 8);
            zone.TileState.WriteCoating(6, 5, "water", 6);

            Assert.IsTrue(TilePropagationSystem.IsFlammable(zone, 5, 5));
            Assert.IsFalse(TilePropagationSystem.IsFlammable(zone, 6, 5));
        }

        // ── Charge travels ───────────────────────────────────────

        [Test]
        public void ChargeSpreadsAlongAConnectedPuddle()
        {
            var zone = new Zone();
            for (int x = 5; x <= 8; x++) zone.TileState.WriteCoating(x, 5, "water", 6);
            zone.TileState.AddCharge(5, 5, 1);

            int reached = TilePropagationSystem.PropagateCharge(zone);

            Assert.Greater(reached, 0);
            Assert.Greater(zone.TileState.Charge(6, 5), 0, "the next tile is live");
            Assert.Greater(zone.TileState.Charge(7, 5), 0, "and the one after that");
        }

        [Test]
        public void ChargeStopsAtDryGround()
        {
            // Counter-check: the sheet has to be CONNECTED. A gap is a
            // real defence, and a real thing for a player to arrange.
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            // gap at 6
            zone.TileState.WriteCoating(7, 5, "water", 6);
            zone.TileState.AddCharge(5, 5, 1);

            TilePropagationSystem.PropagateCharge(zone);

            Assert.AreEqual(0, zone.TileState.Charge(7, 5),
                "a dry tile breaks the circuit");
        }

        [Test]
        public void ChargeIsBounded()
        {
            var zone = new Zone();
            for (int x = 5; x <= 40; x++) zone.TileState.WriteCoating(x, 5, "water", 6);
            zone.TileState.AddCharge(5, 5, 1);

            TilePropagationSystem.PropagateCharge(zone);

            int far = 5 + TilePropagationSystem.ChargeRange + 4;
            Assert.AreEqual(0, zone.TileState.Charge(far, 5),
                "a long puddle is not a zone-wide electrocution");
        }

        [Test]
        public void MetalCarriesFurtherThanWater()
        {
            // The reason to route a shot along grating rather than
            // through a puddle — and the mechanical heart of the
            // greenhouse vignette.
            Assert.Greater(TilePropagationSystem.MetalChargeRange,
                TilePropagationSystem.ChargeRange);

            var zone = new Zone();
            for (int x = 5; x <= 20; x++) Grate(zone, x, 5);
            zone.TileState.AddCharge(5, 5, 1);

            TilePropagationSystem.PropagateCharge(zone);

            int beyondWater = 5 + TilePropagationSystem.ChargeRange + 1;
            Assert.Greater(zone.TileState.Charge(beyondWater, 5), 0,
                "metal reaches past where water would have stopped");
        }

        // ── The vignette ─────────────────────────────────────────

        [Test]
        public void ChargeCrossesFromWaterOntoMetal_AndShocksSomeoneDownTheLine()
        {
            // THE POINT OF THE PHASE. A puddle, a run of grating, and a
            // victim standing on the far end. Nothing in the setup knows
            // about "combos" — water conducts, metal conducts, and the
            // charge finds its way.
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.WriteCoating(6, 5, "water", 6);
            for (int x = 7; x <= 10; x++) Grate(zone, x, 5);
            var victim = Creature(zone, "victim", 9, 5);
            zone.TileState.WriteCoating(9, 5, "water", 6);   // puddle on the grate

            zone.TileState.AddCharge(5, 5, 1);
            int hp = victim.GetStatValue("Hitpoints");

            ZoneTileStateSystem.ResolveWorld(zone);

            Assert.Less(victim.GetStatValue("Hitpoints"), hp,
                "the charge travelled the room and found them");
            Assert.IsTrue(victim.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>());
        }

        [Test]
        public void SpreadChargeReactsInTheSameAction_NotNextTurn()
        {
            // The secondary reaction pass. Without it, charge that
            // propagated into a puddle would sit inert until the next
            // turn and the chain would read as broken.
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.WriteCoating(6, 5, "water", 6);
            var victim = Creature(zone, "victim", 6, 5);
            zone.TileState.AddCharge(5, 5, 1);

            int hp = victim.GetStatValue("Hitpoints");
            ZoneTileStateSystem.ResolveWorld(zone);

            Assert.Less(victim.GetStatValue("Hitpoints"), hp,
                "reached AND resolved in one action");
        }

        // ── Fire travels, more slowly ────────────────────────────

        [Test]
        public void EmbersSpreadAcrossOil()
        {
            var zone = new Zone();
            for (int x = 5; x <= 8; x++) zone.TileState.WriteCoating(x, 5, "oil", 8);
            zone.TileState.WriteResidue(5, 5, "embers", 4);

            int spread = TilePropagationSystem.PropagateFire(zone);

            Assert.Greater(spread, 0);
            Assert.IsTrue(zone.TileState.HasResidue(6, 5, "embers"));
        }

        [Test]
        public void FireDoesNotCrossBareGround()
        {
            var zone = new Zone();
            zone.TileState.WriteResidue(5, 5, "embers", 4);
            zone.TileState.WriteCoating(7, 5, "oil", 8);   // gap at 6

            TilePropagationSystem.PropagateFire(zone);

            Assert.IsFalse(zone.TileState.HasResidue(7, 5, "embers"),
                "fire needs fuel to cross — a firebreak works");
        }

        [Test]
        public void FireCrawlsSlowerThanChargeTravels()
        {
            // Fire that spread as fast as electricity would consume a
            // room before a player could respond to it.
            Assert.Less(TilePropagationSystem.FireRange,
                TilePropagationSystem.ChargeRange);
        }

        // ── Guards ───────────────────────────────────────────────

        [Test]
        public void PropagationIsGracefulOnNullAndEmpty()
        {
            Assert.AreEqual(0, TilePropagationSystem.PropagateCharge(null));
            Assert.AreEqual(0, TilePropagationSystem.PropagateFire(null));
            Assert.AreEqual(0, TilePropagationSystem.PropagateCharge(new Zone()));
        }

        [Test]
        public void PropagationEmitsAStepRecord()
        {
            var zone = new Zone();
            zone.TileState.WriteCoating(5, 5, "water", 6);
            zone.TileState.WriteCoating(6, 5, "water", 6);
            zone.TileState.AddCharge(5, 5, 1);
            Diag.ResetAll();

            TilePropagationSystem.PropagateCharge(zone);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "tile", Kind = "PropagationStep", Limit = 10 }).Records;
            Assert.GreaterOrEqual(recs.Count, 1,
                "a chain has to be traceable or 'why did that happen?' is unanswerable");
            StringAssert.Contains("conductive_liquid", recs[0].PayloadJson);
        }

        [Test]
        public void MetalGrate_ExistsAndConducts()
        {
            // Reachability: a conduction system with no conductor in the
            // content is a system nobody can use.
            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));

            var grate = factory.CreateEntity("MetalGrate");
            Assert.IsNotNull(grate, "MetalGrate must exist");
            var mat = grate.GetPart<MaterialPart>();
            Assert.IsNotNull(mat, "and carry a material");
            Assert.GreaterOrEqual(mat.Conductivity,
                TilePropagationSystem.ConductiveThreshold,
                "and actually conduct");
            Assert.IsFalse(grate.HasTag("Solid"),
                "a grate you cannot stand on is scenery, not infrastructure");
        }
    }
}
