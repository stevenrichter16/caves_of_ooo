using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LL.2 — lore-grounded liquids (tepui-thread canon), JSON-only.
    /// iron-gall-ink / sundew-mucilage / choir-wort / lumen-slime /
    /// bog-mire. Mirrors LiquidExpansionContentTests' two-layer shape:
    ///   1. CONTENT-SHAPE (RED→GREEN): read each shipped JSON from disk,
    ///      assert wired knobs. RED before files exist (File.Exists
    ///      false → Assert.Fail) — real, compile-able, observable.
    ///   2. BEHAVIOR pins + counter-checks against the shipped LQ.5/6
    ///      engine. No new engine code.
    ///
    /// Sweep note: CONDUCTIVITY_AMPLIFY_THRESHOLD is 50, so lumen/bog
    /// ship Conductivity 0 (their mechanics are −DV / FireDampen) —
    /// no inert-looking data (Rule-4 hygiene). Only iron-gall-ink
    /// (Cond 60 ≥ 50) is a conductor here.
    /// </summary>
    public class LiquidLoreContentTests
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
                $"Shipped liquid JSON missing: Assets/{DefDir}/{id}.json");
            LiquidRegistry.InitializeFromJsonSources(new[] { File.ReadAllText(path) });
            var def = LiquidRegistry.Get(id);
            Assert.IsNotNull(def, $"{id}.json failed to parse/register.");
            return def;
        }

        private static System.Collections.Generic.Dictionary<string,int> Mods(
            System.Collections.Generic.List<LiquidStatMod> list)
        {
            var d = new System.Collections.Generic.Dictionary<string,int>();
            if (list != null) foreach (var m in list) d[m.Stat] = m.Delta;
            return d;
        }

        // ── Content-shape (RED before the files exist) ────────────

        [Test]
        public void IronGallInk_Json_ConductiveAndSlowlyCorrosive()
        {
            var d = LoadFromFile("iron-gall-ink");
            Assert.AreEqual(60, d.Conductivity, "iron-gall-ink Conductivity (iron-salt)");
            Assert.AreEqual(0, d.Combustibility);
            Assert.IsNotNull(d.PerTurnDamage);
            Assert.AreEqual(2, d.PerTurnDamage.Amount, "slow Black-Gall corrosion");
            Assert.AreEqual("Acid", d.PerTurnDamage.Type);
            Assert.AreEqual("ink-stained", d.Adjective);
        }

        [Test]
        public void SundewMucilage_Json_IsTheStrongestEntrapment()
        {
            var d = LoadFromFile("sundew-mucilage");
            var m = Mods(d.StatModifiers);
            Assert.AreEqual(-4, m["Agility"], "sundew −4 Agility");
            Assert.AreEqual(-5, m["DV"], "sundew −5 DV (the patient predator)");
            Assert.IsTrue(d.Sticky);
            Assert.AreEqual(0, d.Conductivity, "sundew is organic, not a conductor");
            Assert.AreEqual("mucilage-bound", d.Adjective);
        }

        [Test]
        public void ChoirWort_Json_DigestsAndWeakens()
        {
            var d = LoadFromFile("choir-wort");
            Assert.IsNotNull(d.PerTurnDamage);
            Assert.AreEqual(4, d.PerTurnDamage.Amount, "external digestion tick");
            Assert.AreEqual("Acid", d.PerTurnDamage.Type);
            Assert.AreEqual(-3, Mods(d.StatModifiers)["Toughness"], "digestion saps Toughness");
            Assert.AreEqual("Choir-wetted", d.Adjective);
        }

        [Test]
        public void LumenSlime_Json_GlowBeaconDvOnly()
        {
            var d = LoadFromFile("lumen-slime");
            Assert.AreEqual(-3, Mods(d.StatModifiers)["DV"], "glow → beacon → −3 DV");
            Assert.AreEqual(0, d.Conductivity, "ships 0 (40<threshold would be inert)");
            Assert.AreEqual(0, d.Combustibility);
            Assert.IsTrue(d.PerTurnDamage == null || d.PerTurnDamage.Amount == 0,
                "lumen does not tick damage");
            Assert.AreEqual("lumen-painted", d.Adjective);
        }

        [Test]
        public void BogMire_Json_SmothersFireTannicSlow()
        {
            var d = LoadFromFile("bog-mire");
            Assert.AreEqual(50, d.FireDampen, "waterlogged peat smothers fire");
            Assert.IsNotNull(d.PerTurnDamage);
            Assert.AreEqual(1, d.PerTurnDamage.Amount, "tannic sting");
            Assert.AreEqual("Acid", d.PerTurnDamage.Type);
            Assert.AreEqual(-2, Mods(d.StatModifiers)["Agility"], "wading the mire");
            Assert.AreEqual(0, d.Conductivity, "ships 0 (20<threshold would be inert)");
            Assert.AreEqual("mire-soaked", d.Adjective);
        }

        // ── Behavior fixture ──────────────────────────────────────

        private static Entity MakeCreature()
        {
            var e = new Entity { ID = "c", BlueprintName = "C" };
            e.Tags["Creature"] = "";
            void S(string n, int v) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = 400 };
            S("Hitpoints", 400); S("Toughness", 12);
            S("Agility", 14); S("DV", 6);
            S("HeatResistance", 0); S("ElectricResistance", 0); S("AcidResistance", 0);
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
            { ""Id"":""iron-gall-ink"", ""Adjective"":""ink-stained"", ""Conductivity"":60,
              ""Fluidity"":8, ""Evaporativity"":4,
              ""PerTurnDamage"":{ ""Amount"":2, ""Type"":""Acid"" } },
            { ""Id"":""sundew-mucilage"", ""Adjective"":""mucilage-bound"",
              ""Combustibility"":20, ""Fluidity"":2, ""Evaporativity"":1, ""Sticky"":true,
              ""StatModifiers"":[ { ""Stat"":""Agility"", ""Delta"":-4 },
                                  { ""Stat"":""DV"", ""Delta"":-5 } ] },
            { ""Id"":""choir-wort"", ""Adjective"":""Choir-wetted"", ""Combustibility"":10,
              ""Fluidity"":12, ""Evaporativity"":3,
              ""PerTurnDamage"":{ ""Amount"":4, ""Type"":""Acid"" },
              ""StatModifiers"":[ { ""Stat"":""Toughness"", ""Delta"":-3 } ] },
            { ""Id"":""lumen-slime"", ""Adjective"":""lumen-painted"",
              ""Fluidity"":4, ""Evaporativity"":1,
              ""StatModifiers"":[ { ""Stat"":""DV"", ""Delta"":-3 } ] },
            { ""Id"":""bog-mire"", ""Adjective"":""mire-soaked"", ""FireDampen"":50,
              ""Fluidity"":6, ""Evaporativity"":1,
              ""PerTurnDamage"":{ ""Amount"":1, ""Type"":""Acid"" },
              ""StatModifiers"":[ { ""Stat"":""Agility"", ""Delta"":-2 } ] }
          ]
        }");

        // ── Behavior pins ─────────────────────────────────────────

        [Test]
        public void IronGallInk_Corrosive_AndConductive()
        {
            InitInline();
            var ink = MakeCreature();
            var coat = new LiquidCoveredEffect("iron-gall-ink", 30);
            ink.ApplyEffect(coat);
            int hp0 = ink.GetStatValue("Hitpoints");
            coat.OnTurnStart(ink, GameEvent.New("BeginTakeAction"));
            Assert.Less(ink.GetStatValue("Hitpoints"), hp0, "ink slowly corrodes (Acid tick)");

            var ink2 = MakeCreature(); ink2.ApplyEffect(new LiquidCoveredEffect("iron-gall-ink", 30));
            Assert.Greater(Hit(ink2, 20, "Electric"), Hit(MakeCreature(), 20, "Electric"),
                "iron-gall ink (Conductivity 60 ≥ 50) amplifies Electric");
        }

        [Test]
        public void SundewMucilage_StrongestSlow_NetZero_NoElectric()
        {
            InitInline();
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("sundew-mucilage", 30));
            Assert.AreEqual(10, c.GetStatValue("Agility"), "−4 Agility (14→10)");
            Assert.AreEqual(1, c.GetStatValue("DV"), "−5 DV (6→1) — the strongest trap");
            Assert.AreEqual(20, Hit(c, 20, "Electric"),
                "sundew is organic — NOT conductive, Electric unchanged");
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(14, c.GetStatValue("Agility"), "net-zero");
            Assert.AreEqual(6, c.GetStatValue("DV"));
        }

        [Test]
        public void ChoirWort_DigestsAndWeakens_NetZero()
        {
            InitInline();
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            var coat = new LiquidCoveredEffect("choir-wort", 30);
            fx.ApplyEffect(coat);
            Assert.AreEqual(9, c.GetStatValue("Toughness"), "−3 Toughness (12→9)");
            int hp0 = c.GetStatValue("Hitpoints");
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            Assert.Less(c.GetStatValue("Hitpoints"), hp0, "external digestion ticks (Acid 4)");
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(12, c.GetStatValue("Toughness"), "net-zero on removal");
        }

        [Test]
        public void LumenSlime_GlowBeacon_DvOnly_NoElement_NoTick()
        {
            InitInline();
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            var coat = new LiquidCoveredEffect("lumen-slime", 30);
            fx.ApplyEffect(coat);
            Assert.AreEqual(3, c.GetStatValue("DV"), "glow beacon → −3 DV (6→3)");
            Assert.AreEqual(20, Hit(c, 20, "Electric"), "lumen Conductivity 0 → no amp");
            Assert.AreEqual(20, Hit(c, 20, "Heat"), "lumen no Fire interaction");
            int hp0 = c.GetStatValue("Hitpoints");
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            Assert.AreEqual(hp0, c.GetStatValue("Hitpoints"), "lumen has no PerTurnDamage");
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(6, c.GetStatValue("DV"), "net-zero");
        }

        [Test]
        public void BogMire_SmothersFire_TannicTick_Slows_NetZero()
        {
            InitInline();
            var bog = MakeCreature();
            bog.ApplyEffect(new LiquidCoveredEffect("bog-mire", 30));
            Assert.Less(Hit(bog, 40, "Heat"), Hit(MakeCreature(), 40, "Heat"),
                "bog FireDampen 50 → Heat ≈ ×0.5");

            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            var coat = new LiquidCoveredEffect("bog-mire", 30);
            fx.ApplyEffect(coat);
            Assert.AreEqual(12, c.GetStatValue("Agility"), "−2 Agility (14→12)");
            int hp0 = c.GetStatValue("Hitpoints");
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            Assert.Less(c.GetStatValue("Hitpoints"), hp0, "tannic sting (Acid 1)");
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(14, c.GetStatValue("Agility"), "net-zero");
        }

        // ── Adversarial ───────────────────────────────────────────

        [Test]
        public void Sundew_AbsentStats_NoCrash_NoRecord()
        {
            InitInline();
            var bare = new Entity { ID = "wisp", BlueprintName = "wisp" };
            bare.Tags["Creature"] = "";
            bare.Statistics["Hitpoints"] =
                new Stat { Owner = bare, Name = "Hitpoints", BaseValue = 10 };
            bare.AddPart(new StatusEffectsPart());
            Assert.DoesNotThrow(() =>
                bare.ApplyEffect(new LiquidCoveredEffect("sundew-mucilage", 30)));
            Assert.AreEqual("", bare.GetPart<StatusEffectsPart>()
                .GetEffect<LiquidCoveredEffect>().AppliedModsRaw,
                "absent Agility/DV → nothing recorded, nothing to reverse");
        }

        [Test]
        public void BogMire_RegistryResetMidCoat_RemoveStillNetsZero()
        {
            InitInline();
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("bog-mire", 30));
            Assert.AreEqual(12, c.GetStatValue("Agility"));
            LiquidRegistry.ResetForTests();
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(14, c.GetStatValue("Agility"),
                "reversal reads AppliedModsRaw — exact even after registry reset");
        }
    }
}
