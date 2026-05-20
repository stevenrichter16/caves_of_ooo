using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LB — buff coats: 5 positive lore-liquids + the 3 engine
    /// extensions that make them work. Same RED→GREEN pattern as
    /// LiquidExpansionContentTests / LiquidLoreContentTests:
    ///   1. Content-shape (on-disk knob asserts; RED before files).
    ///   2. Behavior pins + counter-checks for each liquid's headline
    ///      mechanic, exercising the engine extensions in their own
    ///      sub-milestone (LB.2 tepuibone is pure-JSON; LB.3+ add
    ///      signed PerTurnDamage / LightSource attach / killing-blow).
    /// </summary>
    public class LiquidBuffsTests
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
                $"Shipped buff JSON missing: Assets/{DefDir}/{id}.json");
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

        // ════════════════ LB.2 — Tepuibone slurry (pure JSON) ════════════════

        [Test]
        public void Tepuibone_Json_IsTheStoneFortress()
        {
            var d = LoadFromFile("tepuibone-slurry");
            var s = Mods(d.StatModifiers);
            var r = Mods(d.ResistanceModifiers);
            Assert.AreEqual(6, s["AV"], "tepuibone +6 AV");
            Assert.AreEqual(4, s["Toughness"], "tepuibone +4 Toughness");
            Assert.AreEqual(25, r["HeatResistance"], "tepuibone +25 HeatRes");
            Assert.AreEqual(25, r["ColdResistance"], "tepuibone +25 ColdRes");
            Assert.AreEqual(25, r["ElectricResistance"], "tepuibone +25 ElectricRes");
            Assert.AreEqual(25, r["AcidResistance"], "tepuibone +25 AcidRes");
            Assert.AreEqual("stone-stilled", d.Adjective);
        }

        [Test]
        public void TepuiboneCoat_FullDefensiveStack_NetZeroOnRemoval()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""tepuibone-slurry"", ""Adjective"":""stone-stilled"",
                ""Fluidity"":8, ""Evaporativity"":12,
                ""StatModifiers"":[ { ""Stat"":""AV"", ""Delta"":6 },
                                    { ""Stat"":""Toughness"", ""Delta"":4 } ],
                ""ResistanceModifiers"":[ { ""Stat"":""HeatResistance"", ""Delta"":25 },
                                          { ""Stat"":""ColdResistance"", ""Delta"":25 },
                                          { ""Stat"":""ElectricResistance"", ""Delta"":25 },
                                          { ""Stat"":""AcidResistance"", ""Delta"":25 } ] } ] }");
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("tepuibone-slurry", 30));
            Assert.AreEqual(6,  c.GetStatValue("AV"),                 "AV 0→6");
            Assert.AreEqual(16, c.GetStatValue("Toughness"),          "Tough 12→16");
            Assert.AreEqual(25, c.GetStatValue("HeatResistance"),     "HR +25");
            Assert.AreEqual(25, c.GetStatValue("ColdResistance"));
            Assert.AreEqual(25, c.GetStatValue("ElectricResistance"));
            Assert.AreEqual(25, c.GetStatValue("AcidResistance"));
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(0, c.GetStatValue("AV"),                  "AV net-zero");
            Assert.AreEqual(12, c.GetStatValue("Toughness"));
            Assert.AreEqual(0, c.GetStatValue("HeatResistance"));
            Assert.AreEqual(0, c.GetStatValue("AcidResistance"));
        }

        // ════════════════ LB.5 — Memory-Bath (killing-blow interception) ════════════════

        [Test]
        public void MemoryBath_Json_HasDeathAnchor()
        {
            var d = LoadFromFile("memory-bath");
            Assert.AreEqual(50, d.DeathAnchorPercent, "memory-bath anchors at 50% Max");
            Assert.AreEqual("memory-bathed", d.Adjective);
        }

        [Test]
        public void MemoryBathCoat_InterceptsKillingBlow_RestoresHpToHalf()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""memory-bath"", ""Adjective"":""memory-bathed"",
                ""Fluidity"":4, ""Evaporativity"":2,
                ""DeathAnchorPercent"":50 } ] }");
            var c = MakeCreature(hpMax: 200);
            c.GetStat("Hitpoints").BaseValue = 30; // wounded, low HP
            var coat = new LiquidCoveredEffect("memory-bath", 30);
            c.ApplyEffect(coat);
            // Fatal hit: 999 damage (HP is 30).
            var d = new Damage(999); d.AddAttribute("Heat");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.Greater(c.GetStatValue("Hitpoints"), 0,
                "death-anchor must prevent the kill");
            Assert.AreEqual(100, c.GetStatValue("Hitpoints"),
                "restored to Max*50% (200*0.5 = 100)");
            Assert.IsTrue(coat.AnchorConsumed, "one-shot consumed");
            Assert.AreEqual(0, coat.Duration, "coat queued for EndTurn cleanup");
        }

        [Test]
        public void MemoryBath_AnchorIsOneShot_SecondFatalHit_NotIntercepted()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""memory-bath"", ""Adjective"":""memory-bathed"",
                ""Fluidity"":4, ""Evaporativity"":2,
                ""DeathAnchorPercent"":50 } ] }");
            var c = MakeCreature(hpMax: 200);
            c.GetStat("Hitpoints").BaseValue = 30;
            c.ApplyEffect(new LiquidCoveredEffect("memory-bath", 30));
            // First fatal hit → anchored to 100.
            CombatSystem.ApplyDamage(c, new Damage(999), null, null);
            Assert.AreEqual(100, c.GetStatValue("Hitpoints"));
            // Second fatal hit in the same dispatch window → NO anchor
            // (one-shot), HP drops to 0.
            CombatSystem.ApplyDamage(c, new Damage(999), null, null);
            Assert.LessOrEqual(c.GetStatValue("Hitpoints"), 0,
                "second fatal hit not anchored (one-shot)");
        }

        [Test]
        public void NonAnchorCoat_FatalHit_Kills_Counter()
        {
            // Counter: a water coat does NOT have DeathAnchorPercent →
            // a fatal hit kills as normal.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""water"", ""Adjective"":""wet"", ""FireDampen"":40,
                ""Fluidity"":30, ""Evaporativity"":20 } ] }");
            var c = MakeCreature(hpMax: 200);
            c.GetStat("Hitpoints").BaseValue = 30;
            c.ApplyEffect(new LiquidCoveredEffect("water", 30));
            CombatSystem.ApplyDamage(c, new Damage(999), null, null);
            Assert.LessOrEqual(c.GetStatValue("Hitpoints"), 0,
                "water coat doesn't anchor — fatal hit lands");
        }

        [Test]
        public void MemoryBath_NonFatalHit_DoesNotTriggerAnchor()
        {
            // The anchor must trigger ONLY on a killing blow, not on
            // any hit. A 10-damage hit on 200 HP must pass through
            // normally and leave the coat intact.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""memory-bath"", ""Adjective"":""memory-bathed"",
                ""Fluidity"":4, ""Evaporativity"":2,
                ""DeathAnchorPercent"":50 } ] }");
            var c = MakeCreature(hpMax: 200);
            var coat = new LiquidCoveredEffect("memory-bath", 30);
            c.ApplyEffect(coat);
            CombatSystem.ApplyDamage(c, new Damage(10), null, null);
            Assert.Less(c.GetStatValue("Hitpoints"), 400, "the hit landed");
            Assert.IsFalse(coat.AnchorConsumed, "anchor not triggered by non-fatal hits");
            Assert.AreNotEqual(0, coat.Duration, "coat still active");
        }

        [Test]
        public void MemoryBath_DeathAnchored_EmitsDiag()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""memory-bath"", ""Adjective"":""memory-bathed"",
                ""Fluidity"":4, ""Evaporativity"":2,
                ""DeathAnchorPercent"":50 } ] }");
            var c = MakeCreature(hpMax: 200);
            c.GetStat("Hitpoints").BaseValue = 30;
            c.ApplyEffect(new LiquidCoveredEffect("memory-bath", 30));
            CombatSystem.ApplyDamage(c, new Damage(999), null, null);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "DeathAnchored", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"restoredTo\":100", recs[0].PayloadJson);
            StringAssert.Contains("\"percent\":50", recs[0].PayloadJson);
        }

        // ════════════════ LB.4 — Lantern-beetle ichor (LightSourcePart attach) ════════════════

        [Test]
        public void LanternBeetle_Json_EmitsLight()
        {
            var d = LoadFromFile("lantern-beetle-ichor");
            Assert.AreEqual(6, d.LightRadius, "lantern-beetle radius");
            Assert.AreEqual("&Y", d.LightColor, "warm amber");
            Assert.AreEqual("lantern-lit", d.Adjective);
        }

        [Test]
        public void LanternBeetleCoat_AttachesLightSourcePart_OnApply()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""lantern-beetle-ichor"", ""Adjective"":""lantern-lit"",
                ""Fluidity"":5, ""Evaporativity"":3,
                ""LightRadius"":6, ""LightColor"":""&Y"" } ] }");
            var c = MakeCreature();
            Assert.IsNull(c.GetPart<LightSourcePart>(), "no light before coat");
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("lantern-beetle-ichor", 30));
            var ls = c.GetPart<LightSourcePart>();
            Assert.IsNotNull(ls, "coat attaches a LightSourcePart");
            Assert.AreEqual(6, ls.Radius);
            Assert.AreEqual("&Y", ls.LightColor);
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.IsNull(c.GetPart<LightSourcePart>(), "coat removes its LightSourcePart");
        }

        [Test]
        public void LanternBeetleCoat_RespectsHeldLantern_DoesNotPickpocket()
        {
            // Counter / hardness: if the wearer already has a
            // LightSourcePart (they're carrying a lantern), the coat
            // must NOT add a second one — and on removal must NOT strip
            // the pre-existing one.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""lantern-beetle-ichor"", ""Adjective"":""lantern-lit"",
                ""Fluidity"":5, ""Evaporativity"":3,
                ""LightRadius"":6, ""LightColor"":""&Y"" } ] }");
            var c = MakeCreature();
            c.AddPart(new LightSourcePart { Radius = 3, LightColor = "&R" }); // held lantern
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("lantern-beetle-ichor", 30));
            var ls = c.GetPart<LightSourcePart>();
            Assert.IsNotNull(ls, "held lantern still present");
            Assert.AreEqual(3, ls.Radius, "coat must NOT overwrite (3 = held, not 6 = coat)");
            Assert.AreEqual("&R", ls.LightColor);
            fx.RemoveEffect<LiquidCoveredEffect>();
            ls = c.GetPart<LightSourcePart>();
            Assert.IsNotNull(ls, "removal must NOT pickpocket the held lantern");
            Assert.AreEqual(3, ls.Radius);
        }

        [Test]
        public void NonLanternCoat_AttachesNoLight()
        {
            // Counter: water doesn't emit light.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""water"", ""Adjective"":""wet"", ""FireDampen"":40,
                ""Fluidity"":30, ""Evaporativity"":20 } ] }");
            var c = MakeCreature();
            c.ApplyEffect(new LiquidCoveredEffect("water", 30));
            Assert.IsNull(c.GetPart<LightSourcePart>(), "non-lantern coat = no light");
        }

        // ════════════════ LB.3 — Convalessence (signed PerTurnDamage) ════════════════

        [Test]
        public void Convalessence_Json_HealsViaNegativePerTurnDamage()
        {
            var d = LoadFromFile("convalessence");
            Assert.IsNotNull(d.PerTurnDamage);
            Assert.AreEqual(-4, d.PerTurnDamage.Amount, "negative Amount = heal");
            Assert.AreEqual("convalescing", d.Adjective);
        }

        [Test]
        public void ConvalessenceCoat_HealsOnTurnStart_CappedAtMax()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""convalessence"", ""Adjective"":""convalescing"",
                ""Fluidity"":20, ""Evaporativity"":15,
                ""PerTurnDamage"":{ ""Amount"":-4, ""Type"":""Acid"" } } ] }");
            // Wound the target so heal has room.
            var c = MakeCreature(hpMax: 100);
            c.GetStat("Hitpoints").BaseValue = 50;
            var coat = new LiquidCoveredEffect("convalessence", 30);
            c.ApplyEffect(coat);
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            Assert.AreEqual(54, c.GetStatValue("Hitpoints"),
                "convalessence +4/turn (50 + |−4| = 54)");
            // Cap at Max.
            c.GetStat("Hitpoints").BaseValue = 99;
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            Assert.AreEqual(100, c.GetStatValue("Hitpoints"),
                "heal caps at Stat.Max (would have gone to 103)");
        }

        [Test]
        public void ConvalessenceCoat_HealTick_EmitsLiquidHealTickDiag()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""convalessence"", ""Adjective"":""convalescing"",
                ""Fluidity"":20, ""Evaporativity"":15,
                ""PerTurnDamage"":{ ""Amount"":-4, ""Type"":""Acid"" } } ] }");
            var c = MakeCreature();
            c.GetStat("Hitpoints").BaseValue = 200;
            var coat = new LiquidCoveredEffect("convalessence", 30);
            c.ApplyEffect(coat);
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "HealTick", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count, "one HealTick per heal");
            StringAssert.Contains("\"liquidId\":\"convalessence\"", recs[0].PayloadJson);
            StringAssert.Contains("\"gained\":4", recs[0].PayloadJson);
        }

        [Test]
        public void Acid_StillDamages_AfterSignedSwitch_BackwardCompat()
        {
            // Counter: positive PerTurnDamage must still go through the
            // damage path (a buggy "always-heal" impl would heal here).
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""acid"", ""Adjective"":""acid-covered"",
                ""Fluidity"":20, ""Evaporativity"":15,
                ""PerTurnDamage"":{ ""Amount"":3, ""Type"":""Acid"" } } ] }");
            var c = MakeCreature();
            int before = c.GetStatValue("Hitpoints");
            var coat = new LiquidCoveredEffect("acid", 30);
            c.ApplyEffect(coat);
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            Assert.Less(c.GetStatValue("Hitpoints"), before,
                "positive PerTurnDamage still damages (backward-compat)");
        }

        [Test]
        public void TepuiboneCoat_DoesNotTick_AndIsNotConductive()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""tepuibone-slurry"", ""Adjective"":""stone-stilled"",
                ""Fluidity"":8, ""Evaporativity"":12,
                ""ResistanceModifiers"":[ { ""Stat"":""HeatResistance"", ""Delta"":25 } ] } ] }");
            var c = MakeCreature();
            var coat = new LiquidCoveredEffect("tepuibone-slurry", 30);
            c.ApplyEffect(coat);
            int hp0 = c.GetStatValue("Hitpoints");
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            Assert.AreEqual(hp0, c.GetStatValue("Hitpoints"),
                "tepuibone has no PerTurnDamage — no tick");
            // Counter: HR+25 actually reduces incoming Heat (resistance layer
            // shows in HP delta — the brine/Heat 85% precedent).
            int before = c.GetStatValue("Hitpoints");
            var d = new Damage(40); d.AddAttribute("Heat");
            CombatSystem.ApplyDamage(c, d, null, null);
            int dealt = before - c.GetStatValue("Hitpoints");
            Assert.Less(dealt, 40, "+25 HeatRes must reduce Heat damage");
        }
    }
}
