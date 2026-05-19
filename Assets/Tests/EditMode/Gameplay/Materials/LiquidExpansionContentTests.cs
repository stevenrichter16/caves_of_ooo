using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LX.2 — Qud-liquid expansion (lava/gel/sap/honey), JSON-only.
    ///
    /// Two layers:
    ///   1. CONTENT-SHAPE (RED→GREEN): read each shipped JSON file from
    ///      disk and assert its wired knobs. RED before the files exist
    ///      (File.Exists false → Assert.Fail) — a real, compile-able,
    ///      observable failure, not a compressed step.
    ///   2. BEHAVIOR pins + counter-checks: inline JSON mirroring the
    ///      file content, exercising the already-shipped LQ.5/LQ.6
    ///      engine (lava ticks Heat + amps Electric + −HeatRes; gel
    ///      amps Electric; sap/honey −Agility(/−DV) net-zero on
    ///      removal). These prove the data wires to real behavior with
    ///      zero new engine code (the LQ.6 "expand by data" thesis).
    ///
    /// Test discipline (plan §B1): bare Entity + inline JSON for
    /// behavior; the content layer reads the real Resources files via
    /// Application.dataPath (EditMode → the project Assets folder).
    /// </summary>
    public class LiquidExpansionContentTests
    {
        private const string DefDir =
            "Resources/Content/Data/LiquidDefinitions";

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        [TearDown]
        public void TearDown() => LiquidRegistry.ResetForTests();

        private static LiquidDefinition LoadFromFile(string id)
        {
            string path = Path.Combine(
                UnityEngine.Application.dataPath, DefDir, id + ".json");
            Assert.IsTrue(File.Exists(path),
                $"Shipped liquid JSON missing: Assets/{DefDir}/{id}.json");
            LiquidRegistry.InitializeFromJsonSources(
                new[] { File.ReadAllText(path) });
            var def = LiquidRegistry.Get(id);
            Assert.IsNotNull(def, $"{id}.json failed to parse/register.");
            return def;
        }

        // ── Content-shape (RED before the files exist) ────────────

        [Test]
        public void Lava_Json_HasExpectedWiredKnobs()
        {
            var d = LoadFromFile("lava");
            Assert.AreEqual(90, d.Conductivity, "lava Conductivity");
            Assert.AreEqual(0, d.Combustibility, "lava Combustibility");
            Assert.IsNotNull(d.PerTurnDamage, "lava PerTurnDamage present");
            Assert.AreEqual(8, d.PerTurnDamage.Amount, "lava tick amount");
            Assert.AreEqual("Heat", d.PerTurnDamage.Type, "lava tick type");
            Assert.IsNotNull(d.ResistanceModifiers, "lava ResistanceModifiers");
            Assert.AreEqual(1, d.ResistanceModifiers.Count);
            Assert.AreEqual("HeatResistance", d.ResistanceModifiers[0].Stat);
            Assert.AreEqual(-25, d.ResistanceModifiers[0].Delta);
            Assert.AreEqual("lava-covered", d.Adjective);
        }

        [Test]
        public void Gel_Json_IsPureConductor()
        {
            var d = LoadFromFile("gel");
            Assert.AreEqual(100, d.Conductivity, "gel Conductivity");
            Assert.AreEqual(0, d.Combustibility);
            Assert.IsTrue(d.StatModifiers == null || d.StatModifiers.Count == 0,
                "gel has NO StatModifiers (pure conductor)");
            Assert.IsTrue(d.ResistanceModifiers == null || d.ResistanceModifiers.Count == 0);
            Assert.AreEqual("gel-covered", d.Adjective);
        }

        [Test]
        public void Sap_Json_IsFlammableWithAgilityDebuff()
        {
            var d = LoadFromFile("sap");
            Assert.AreEqual(70, d.Combustibility, "sap Combustibility");
            Assert.AreEqual(0, d.Conductivity, "sap is NOT conductive");
            Assert.AreEqual(250, d.FlameTemperature);
            Assert.IsNotNull(d.StatModifiers);
            Assert.AreEqual(1, d.StatModifiers.Count);
            Assert.AreEqual("Agility", d.StatModifiers[0].Stat);
            Assert.AreEqual(-2, d.StatModifiers[0].Delta);
            Assert.AreEqual("sap-covered", d.Adjective);
        }

        [Test]
        public void Honey_Json_IsFlammableWithAgilityAndDvDebuff()
        {
            var d = LoadFromFile("honey");
            Assert.AreEqual(60, d.Combustibility, "honey Combustibility");
            Assert.AreEqual(25, d.Adsorbence);
            Assert.IsNotNull(d.StatModifiers);
            Assert.AreEqual(2, d.StatModifiers.Count);
            var byStat = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var m in d.StatModifiers) byStat[m.Stat] = m.Delta;
            Assert.AreEqual(-2, byStat["Agility"], "honey Agility");
            Assert.AreEqual(-3, byStat["DV"], "honey DV");
            Assert.AreEqual("honey-covered", d.Adjective);
        }

        // ── Behavior fixture ──────────────────────────────────────

        private static Entity MakeCreature()
        {
            var e = new Entity { ID = "c", BlueprintName = "C" };
            e.Tags["Creature"] = "";
            void S(string n, int v) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = 400 };
            S("Hitpoints", 400); S("Toughness", 10);
            S("Agility", 14); S("DV", 6);
            S("HeatResistance", 0); S("ElectricResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static int Hit(Entity c, int amount, string attr)
        {
            int before = c.GetStatValue("Hitpoints");
            var d = new Damage(amount);
            if (!string.IsNullOrEmpty(attr)) d.AddAttribute(attr);
            CombatSystem.ApplyDamage(c, d, source: null, zone: null);
            return before - c.GetStatValue("Hitpoints");
        }

        private static void InitInline() => LiquidRegistry.Initialize(@"{
          ""Liquids"": [
            { ""Id"":""lava"", ""Adjective"":""lava-covered"", ""Conductivity"":90,
              ""Fluidity"":15, ""Evaporativity"":0,
              ""PerTurnDamage"":{ ""Amount"":8, ""Type"":""Heat"" },
              ""ResistanceModifiers"":[ { ""Stat"":""HeatResistance"", ""Delta"":-25 } ] },
            { ""Id"":""gel"", ""Adjective"":""gel-covered"", ""Conductivity"":100,
              ""Fluidity"":5, ""Evaporativity"":1 },
            { ""Id"":""sap"", ""Adjective"":""sap-covered"", ""Combustibility"":70,
              ""Fluidity"":3, ""Evaporativity"":1,
              ""StatModifiers"":[ { ""Stat"":""Agility"", ""Delta"":-2 } ] },
            { ""Id"":""honey"", ""Adjective"":""honey-covered"", ""Combustibility"":60,
              ""Fluidity"":10, ""Evaporativity"":1,
              ""StatModifiers"":[ { ""Stat"":""Agility"", ""Delta"":-2 },
                                  { ""Stat"":""DV"", ""Delta"":-3 } ] }
          ]
        }");

        // ── Behavior pins ─────────────────────────────────────────

        [Test]
        public void Lava_TicksHeat_OnTurnStart()
        {
            InitInline();
            var c = MakeCreature();
            var coat = new LiquidCoveredEffect("lava", 30);
            c.ApplyEffect(coat);
            int before = c.GetStatValue("Hitpoints");
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            Assert.Less(c.GetStatValue("Hitpoints"), before,
                "Lava coat must tick Heat damage each turn (PerTurnDamage).");
        }

        [Test]
        public void Lava_AmplifiesElectric_AndLowersHeatResistance()
        {
            InitInline();
            var lava = MakeCreature();
            lava.ApplyEffect(new LiquidCoveredEffect("lava", 30));
            Assert.Greater(Hit(lava, 20, "Electric"),
                Hit(MakeCreature(), 20, "Electric"),
                "Lava (Conductivity 90) must amplify Electric.");

            var lava2 = MakeCreature();
            lava2.ApplyEffect(new LiquidCoveredEffect("lava", 30));
            Assert.Greater(Hit(lava2, 40, "Heat"), Hit(MakeCreature(), 40, "Heat"),
                "Lava's −25 HeatResistance must make Heat hit harder.");
        }

        [Test]
        public void Gel_AmplifiesElectric_NoStatChange()
        {
            InitInline();
            var gel = MakeCreature();
            gel.ApplyEffect(new LiquidCoveredEffect("gel", 30));
            Assert.Greater(Hit(gel, 20, "Electric"),
                Hit(MakeCreature(), 20, "Electric"),
                "Gel (Conductivity 100) must amplify Electric.");
            Assert.AreEqual(14, gel.GetStatValue("Agility"),
                "Gel has no StatModifiers — Agility unchanged.");
            Assert.AreEqual("", gel.GetPart<StatusEffectsPart>()
                .GetEffect<LiquidCoveredEffect>().AppliedModsRaw);
        }

        [Test]
        public void Sap_ReducesAgility_NetZeroOnRemoval_NoElectric()
        {
            InitInline();
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("sap", 30));
            Assert.AreEqual(12, c.GetStatValue("Agility"), "sap −2 Agility (14→12)");
            // Counter-check: sap is NOT conductive.
            Assert.AreEqual(20, Hit(c, 20, "Electric"),
                "Sap must NOT touch Electric (Conductivity 0).");
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(14, c.GetStatValue("Agility"), "net-zero on removal");
        }

        [Test]
        public void Honey_ReducesAgilityAndDV_NetZeroOnRemoval()
        {
            InitInline();
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("honey", 30));
            Assert.AreEqual(12, c.GetStatValue("Agility"), "honey −2 Agility");
            Assert.AreEqual(3, c.GetStatValue("DV"), "honey −3 DV (6→3)");
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(14, c.GetStatValue("Agility"));
            Assert.AreEqual(6, c.GetStatValue("DV"));
        }

        // ── Counter / adversarial ─────────────────────────────────

        [Test]
        public void Water_DoesNotTick_CounterTo_LavaTick()
        {
            // Counter-check for Lava_TicksHeat: a no-PerTurnDamage
            // liquid must be tick-silent (a buggy always-tick impl
            // would fail this).
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""water"", ""Adjective"":""wet"", ""FireDampen"":40,
                ""Fluidity"":30, ""Evaporativity"":20 } ] }");
            var c = MakeCreature();
            var coat = new LiquidCoveredEffect("water", 30);
            c.ApplyEffect(coat);
            int before = c.GetStatValue("Hitpoints");
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            Assert.AreEqual(before, c.GetStatValue("Hitpoints"),
                "Water (no PerTurnDamage) must NOT tick.");
        }

        [Test]
        public void Sap_OnCreatureWithoutAgility_NoCrash_NoRecord()
        {
            InitInline();
            var bare = new Entity { ID = "wisp", BlueprintName = "wisp" };
            bare.Tags["Creature"] = "";
            bare.Statistics["Hitpoints"] =
                new Stat { Owner = bare, Name = "Hitpoints", BaseValue = 10 };
            bare.AddPart(new StatusEffectsPart());
            Assert.DoesNotThrow(() =>
                bare.ApplyEffect(new LiquidCoveredEffect("sap", 30)));
            Assert.AreEqual("", bare.GetPart<StatusEffectsPart>()
                .GetEffect<LiquidCoveredEffect>().AppliedModsRaw,
                "absent Agility → nothing recorded, nothing to reverse");
        }

        [Test]
        public void Lava_RegistryResetMidCoat_RemoveStillNetsZero()
        {
            InitInline();
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("lava", 30));
            Assert.AreEqual(-25, c.GetStatValue("HeatResistance"),
                "lava −25 HeatResistance applied");
            LiquidRegistry.ResetForTests();
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(0, c.GetStatValue("HeatResistance"),
                "reversal reads AppliedModsRaw, exact even after registry reset");
        }
    }
}
