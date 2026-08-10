using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// PALIMPSEST P5 — one rite, different rooms, different outcomes,
    /// and the rite knows about none of it.
    ///
    /// <para>This is the phase's POC question and the architectural
    /// claim of the whole prototype made testable. Fulmination consumes
    /// the target's water, deals a small hit, writes Charge 2 to the
    /// tile, and stops. It contains no knowledge of puddles, grates or
    /// stone floors — but it behaves completely differently on each.</para>
    /// </summary>
    public class FulminationTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();

            ResonanceSystem.ResetForTests();
            ResonanceSystem.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Resonance/Resonance.json")));

            TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));

            LiquidRegistry.ResetForTests();
            var liquids = new System.Collections.Generic.List<string>();
            foreach (var f in Directory.GetFiles(Path.Combine(
                Application.dataPath, "Resources/Content/Data/LiquidDefinitions"), "*.json"))
                liquids.Add(File.ReadAllText(f));
            LiquidRegistry.InitializeFromJsonSources(liquids);

            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [TearDown]
        public void TearDown()
        {
            ResonanceSystem.ResetForTests();
            TileReactionSystem.ResetForTests();
            LiquidRegistry.ResetForTests();
        }

        private static Entity Creature(Zone zone, string name, int x, int y, int hp = 300)
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

        /// <summary>A caster who knows the rite and carries an inked book.</summary>
        private static (Entity caster, FulminationMutation rite) Caster(Zone zone, int x, int y)
        {
            var c = Creature(zone, "caster", x, y);
            c.AddPart(new MutationsPart());
            var inv = new InventoryPart { MaxWeight = 500 };
            c.AddPart(inv);
            var book = new Entity { ID = "book", BlueprintName = "FulminationGrimoire" };
            book.AddPart(new RenderPart { DisplayName = "Fulmination" });
            book.AddPart(new GrimoireChargePart { Charges = 10, MaxCharges = 10 });
            inv.AddObject(book);

            var rite = new FulminationMutation();
            c.AddPart(rite);
            rite.Mutate(c, 1);
            return (c, rite);
        }

        private Entity Grate(Zone zone, int x, int y)
        {
            var g = _factory.CreateEntity("MetalGrate");
            zone.AddEntity(g, x, y);
            return g;
        }

        // ════════════════════════════════════════════════════════
        // THE POC QUESTION
        // ════════════════════════════════════════════════════════

        [Test]
        public void SameRite_DryStoneRoom_ChargeGoesNowhere()
        {
            var zone = new Zone();
            var (caster, rite) = Caster(zone, 5, 5);
            var target = Creature(zone, "target", 8, 5);
            var bystander = Creature(zone, "bystander", 10, 5);

            int bystanderHp = bystander.GetStatValue("Hitpoints");
            rite.Cast(zone, 1, 0);

            Assert.AreEqual(bystanderHp, bystander.GetStatValue("Hitpoints"),
                "on a dry floor the charge has nowhere to run — a correct outcome, not a failure");
        }

        [Test]
        public void SameRite_FloodedRoom_ChargeRunsThroughTheWater()
        {
            // Identical cast. Different room. The rite is unchanged.
            var zone = new Zone();
            var (caster, rite) = Caster(zone, 5, 5);
            var target = Creature(zone, "target", 8, 5);
            var bystander = Creature(zone, "bystander", 10, 5);

            // A connected sheet from the target to the bystander.
            for (int x = 8; x <= 10; x++) zone.TileState.WriteCoating(x, 5, "water", 6);

            int bystanderHp = bystander.GetStatValue("Hitpoints");
            rite.Cast(zone, 1, 0);

            Assert.Less(bystander.GetStatValue("Hitpoints"), bystanderHp,
                "the water carried it to someone the rite never targeted");
        }

        [Test]
        public void SameRite_GratedRoom_ChargeCrossesFurtherThanWaterWould()
        {
            var zone = new Zone();
            var (caster, rite) = Caster(zone, 5, 5);
            var target = Creature(zone, "target", 8, 5);

            // Grating running past where water would have stopped.
            for (int x = 8; x <= 13; x++) Grate(zone, x, 5);
            var far = Creature(zone, "far", 12, 5);
            zone.TileState.WriteCoating(12, 5, "water", 6);  // a puddle on the grate

            int farHp = far.GetStatValue("Hitpoints");
            rite.Cast(zone, 1, 0);

            Assert.Less(far.GetStatValue("Hitpoints"), farHp,
                "metal carried it four tiles past the mark");
        }

        [Test]
        public void TheRiteItself_ContainsNoKnowledgeOfAnyOfThis()
        {
            // The claim, stated as an invariant a future edit has to
            // break deliberately: Fulmination writes charge and stops.
            // If it ever starts asking "is the tile wet?", the world
            // stops owning the consequences and the architecture is gone.
            string src = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Scripts/Gameplay/Mutations/FulminationMutation.cs"));

            StringAssert.DoesNotContain("HasCoating", src,
                "a rite must not inspect the tile it writes to");
            StringAssert.DoesNotContain("IsConductive", src,
                "conduction is the world's business, not the rite's");
            StringAssert.DoesNotContain("\"water\"", src,
                "the rite has no idea what a puddle is");
        }

        // ── The rite's own behaviour ─────────────────────────────

        [Test]
        public void Fulmination_WritesChargeToTheTargetsTile()
        {
            var zone = new Zone();
            var (caster, rite) = Caster(zone, 5, 5);
            var target = Creature(zone, "target", 8, 5);

            rite.Cast(zone, 1, 0);

            // Dry stone: nothing consumed the charge, so it is still there.
            Assert.Greater(zone.TileState.Charge(8, 5), 0,
                "the charge is left in the ground for the world to use");
        }

        [Test]
        public void Fulmination_ConsumesTheTargetsWater()
        {
            var zone = new Zone();
            var (caster, rite) = Caster(zone, 5, 5);
            var target = Creature(zone, "target", 8, 5);
            target.ApplyEffect(new WetEffect(1.0f), caster, zone);

            rite.Cast(zone, 1, 0);

            Assert.IsFalse(target.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "it is still a rite: it spends a status");
        }

        [Test]
        public void Fulmination_SpendsInk_AndRefusesWithoutIt()
        {
            var zone = new Zone();
            var (caster, rite) = Caster(zone, 5, 5);
            Creature(zone, "target", 8, 5);
            var book = caster.GetPart<InventoryPart>().Objects[0].GetPart<GrimoireChargePart>();

            rite.Cast(zone, 1, 0);
            Assert.AreEqual(9, book.Charges);

            book.Charges = 0;
            Diag.ResetAll();
            rite.Cast(zone, 1, 0);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "spell", Kind = "RiteRejected", Limit = 5 }).Records;
            Assert.GreaterOrEqual(recs.Count, 1);
            StringAssert.Contains("no_ink", recs[0].PayloadJson);
        }

        [Test]
        public void Fulmination_WithNoTarget_DoesNotWasteACharge()
        {
            var zone = new Zone();
            var (caster, rite) = Caster(zone, 5, 5);
            var book = caster.GetPart<InventoryPart>().Objects[0].GetPart<GrimoireChargePart>();

            rite.Cast(zone, 1, 0);

            Assert.AreEqual(10, book.Charges, "no mark, no ink spent");
        }

        [Test]
        public void FulminationGrimoire_IsObtainableAndInked()
        {
            var book = _factory.CreateEntity("FulminationGrimoire");
            Assert.IsNotNull(book);
            Assert.Greater(book.GetPart<GrimoireChargePart>().Charges, 0);

            var loot = File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json"));
            StringAssert.Contains("FulminationGrimoire", loot,
                "a rite nobody can buy does not exist");
        }
    }
}
