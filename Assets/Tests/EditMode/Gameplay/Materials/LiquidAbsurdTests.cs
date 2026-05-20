using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LA — absurd-property coats. 5 liquids, each with a qualitatively
    /// NEW mechanic (not another stat dial): element immunity, reflect,
    /// HP rewind, knockback, undying+pacifist. Same RED→GREEN content +
    /// behavior pattern as LiquidBuffsTests.
    /// </summary>
    public class LiquidAbsurdTests
    {
        private const string DefDir = "Resources/Content/Data/LiquidDefinitions";

        [SetUp]
        public void Setup() { MessageLog.Clear(); Diag.ResetAll(); }

        [TearDown]
        public void TearDown() => LiquidRegistry.ResetForTests();

        private static LiquidDefinition LoadFromFile(string id)
        {
            string path = Path.Combine(UnityEngine.Application.dataPath, DefDir, id + ".json");
            Assert.IsTrue(File.Exists(path),
                $"Shipped absurd-liquid JSON missing: Assets/{DefDir}/{id}.json");
            LiquidRegistry.InitializeFromJsonSources(new[] { File.ReadAllText(path) });
            var def = LiquidRegistry.Get(id);
            Assert.IsNotNull(def, $"{id}.json failed to parse/register.");
            return def;
        }

        private static Entity MakeCreature(int hpMax = 400)
        {
            var e = new Entity { ID = "c", BlueprintName = "C" };
            e.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", hpMax, hpMax); S("Toughness", 12);
            S("Agility", 14); S("DV", 6); S("AV", 0);
            S("HeatResistance", 0); S("ColdResistance", 0);
            S("ElectricResistance", 0); S("AcidResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        // ════════════════ LA.2 — Veined Pulse Mycelium (ImmuneElement) ════════════════

        [Test]
        public void VeinedPulseMycelium_Json_GrantsElectricImmunity()
        {
            // Content RED: JSON file present, parses, declares ImmuneElement="Electric".
            // Lore anchor: §L5 Branchwork — fungal cognition routes electrical
            // discharge around obstacles (the mycelium is the obstacle the
            // discharge routes around).
            var d = LoadFromFile("veined-pulse-mycelium");
            Assert.AreEqual("Electric", d.ImmuneElement,
                "veined-pulse declares Electric as the immune element");
            Assert.AreEqual("pulse-veined", d.Adjective);
        }

        [Test]
        public void VeinedPulseCoat_NullifiesElectricDamage_Completely()
        {
            // Behavior RED before the OnBeforeTakeDamage early-out is added.
            // Pin: an Electric hit on a veined-pulse-coated creature deals 0,
            // not "reduced" — distinct from resistance, which scales the
            // amount. The mycelium routes the current around the host.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""veined-pulse-mycelium"", ""Adjective"":""pulse-veined"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""Electric"" } ] }");
            var c = MakeCreature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("veined-pulse-mycelium", 30));
            var d = new Damage(75); d.AddAttribute("Electric");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.AreEqual(hp0, c.GetStatValue("Hitpoints"),
                "Electric damage must be fully nullified (Amount→0)");
        }

        [Test]
        public void VeinedPulseCoat_NonImmuneElement_StillDamages_Counter()
        {
            // Counter: only the named element is nullified. A Heat hit on a
            // veined-pulse (electric-immune) creature lands normally.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""veined-pulse-mycelium"", ""Adjective"":""pulse-veined"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""Electric"" } ] }");
            var c = MakeCreature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("veined-pulse-mycelium", 30));
            var d = new Damage(50); d.AddAttribute("Heat");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.Less(c.GetStatValue("Hitpoints"), hp0,
                "non-immune element (Heat) must still damage");
        }

        [Test]
        public void NonImmuneCoat_ElectricHit_StillDamages_Counter()
        {
            // Counter: a coat without ImmuneElement does NOT confer immunity.
            // A water coat (Conductivity=100, no ImmuneElement) takes the
            // hit through the existing conductivity amplifier — not nullified.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""water"", ""Adjective"":""wet"", ""Conductivity"":100,
                ""Fluidity"":30, ""Evaporativity"":20 } ] }");
            var c = MakeCreature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("water", 30));
            var d = new Damage(20); d.AddAttribute("Electric");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.Less(c.GetStatValue("Hitpoints"), hp0,
                "water coat has no ImmuneElement — Electric damage still lands");
        }

        [Test]
        public void VeinedPulseCoat_ImmunityFires_EmitsDiag()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""veined-pulse-mycelium"", ""Adjective"":""pulse-veined"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""Electric"" } ] }");
            var c = MakeCreature(hpMax: 200);
            c.ApplyEffect(new LiquidCoveredEffect("veined-pulse-mycelium", 30));
            var d = new Damage(50); d.AddAttribute("Electric");
            CombatSystem.ApplyDamage(c, d, null, null);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "ElementImmunity", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count, "one ElementImmunity record per nullified hit");
            StringAssert.Contains("\"liquidId\":\"veined-pulse-mycelium\"", recs[0].PayloadJson);
            StringAssert.Contains("\"element\":\"Electric\"", recs[0].PayloadJson);
        }

        [Test]
        public void VeinedPulseCoat_AliasCollapse_LightningTaggedHit_AlsoNullified()
        {
            // Adversarial: spells/weapons tag "Lightning", "Shock",
            // "Electricity" — all collapse to the Electric flag. The
            // immunity gate must match the FLAG, not the literal string,
            // mirroring the LQ.5/Fix-1 alias-collapse precedent.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""veined-pulse-mycelium"", ""Adjective"":""pulse-veined"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""Electric"" } ] }");
            var c = MakeCreature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("veined-pulse-mycelium", 30));
            // Lightning is a damage-attribute alias for Electric (Damage.cs:214).
            var d = new Damage(40); d.AddAttribute("Lightning");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.AreEqual(hp0, c.GetStatValue("Hitpoints"),
                "Lightning alias should also be nullified by Electric immunity");
        }
    }
}
