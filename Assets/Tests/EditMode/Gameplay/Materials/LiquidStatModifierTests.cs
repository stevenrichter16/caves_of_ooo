using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LQ.6 — the 3 stat/resistance liquids + the expandability proof.
    /// Brine/Pitch/Carapace-Ichor are JSON-only content; the apply/
    /// reverse engine (LiquidCoveredEffect.ApplyStatModifiers /
    /// ReverseStatModifiers) is the only C#, and a 4th liquid needs
    /// ZERO new C# (Expandability_FourthLiquid_JsonOnly).
    ///
    /// Pins the §3 trade-offs and the EquipBonus net-zero invariant:
    ///   - Brine: +HeatRes AND −ElectricRes simultaneously
    ///   - Pitch: −Agility/−DV AND Fire-amplified (LQ.5 reuse)
    ///   - Ichor: +AV AND −ColdRes
    ///   - every coat removal (expire/dry/cure) nets the stats to zero
    ///   - an id-swap (OnStack stronger-wins) reverses the OUTGOING
    ///     liquid then applies the INCOMING one (no leak)
    ///   - save/load mid-coat preserves the deltas EXACTLY ONCE
    ///     (AppliedModsRaw round-trips; OnApply is not re-run on load)
    ///
    /// Test discipline (plan §B1): bare Entity + inline JSON.
    /// </summary>
    public class LiquidStatModifierTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            LiquidRegistry.Initialize(@"{
              ""Liquids"": [
                { ""Id"": ""water"", ""DisplayName"": ""water"", ""Adjective"": ""wet"",
                  ""Conductivity"": 100, ""FireDampen"": 40,
                  ""Fluidity"": 30, ""Evaporativity"": 20 },
                { ""Id"": ""brine"", ""DisplayName"": ""brine"", ""Adjective"": ""briny"",
                  ""Conductivity"": 100, ""Fluidity"": 25, ""Evaporativity"": 10,
                  ""ResistanceModifiers"": [
                    { ""Stat"": ""HeatResistance"", ""Delta"": 15 },
                    { ""Stat"": ""ElectricResistance"", ""Delta"": -15 } ] },
                { ""Id"": ""pitch"", ""DisplayName"": ""pitch"", ""Adjective"": ""pitch-covered"",
                  ""Combustibility"": 90, ""FlameTemperature"": 250,
                  ""Fluidity"": 5, ""Evaporativity"": 2, ""Sticky"": true,
                  ""StatModifiers"": [
                    { ""Stat"": ""Agility"", ""Delta"": -2 },
                    { ""Stat"": ""DV"", ""Delta"": -3 } ] },
                { ""Id"": ""carapace-ichor"", ""DisplayName"": ""carapace ichor"",
                  ""Adjective"": ""ichor-coated"", ""Fluidity"": 8, ""Evaporativity"": 6,
                  ""StatModifiers"": [ { ""Stat"": ""AV"", ""Delta"": 4 } ],
                  ""ResistanceModifiers"": [ { ""Stat"": ""ColdResistance"", ""Delta"": -20 } ] },
                { ""Id"": ""quicksilver"", ""DisplayName"": ""quicksilver"",
                  ""Adjective"": ""silvered"", ""Fluidity"": 12, ""Evaporativity"": 8,
                  ""StatModifiers"": [ { ""Stat"": ""Strength"", ""Delta"": 5 } ] }
              ]
            }");
        }

        [TearDown]
        public void TearDown() => LiquidRegistry.ResetForTests();

        // ── Fixture ─────────────────────────────────────────────

        private static Entity MakeCreature()
        {
            var e = new Entity { ID = "c", BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            void S(string n, int v) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -100, Max = 200 };
            S("Hitpoints", 200); S("Toughness", 10); S("Strength", 16);
            S("Agility", 14); S("DV", 6); S("AV", 2);
            S("HeatResistance", 0); S("ElectricResistance", 0); S("ColdResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "test" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static int V(Entity e, string stat) => e.GetStatValue(stat);

        // ── §3 trade-offs (each buff AND its paired debuff) ─────

        [Test]
        public void BrineCoat_AppliesHeatResUp_AND_ElectricResDown()
        {
            var c = MakeCreature();
            c.ApplyEffect(new LiquidCoveredEffect("brine", 30));
            Assert.AreEqual(15, V(c, "HeatResistance"), "Brine grants +15 HeatResistance.");
            Assert.AreEqual(-15, V(c, "ElectricResistance"),
                "Brine's trade-off: −15 ElectricResistance (salt conducts).");
        }

        [Test]
        public void PitchCoat_AppliesAgilityDown_AND_DVDown()
        {
            var c = MakeCreature();
            c.ApplyEffect(new LiquidCoveredEffect("pitch", 30));
            Assert.AreEqual(12, V(c, "Agility"), "Pitch: −2 Agility (14→12).");
            Assert.AreEqual(3, V(c, "DV"), "Pitch: −3 DV (6→3).");
        }

        [Test]
        public void PitchCoat_IsAlsoFireAmplified_TheLethalTradeoff()
        {
            // Pitch Combustibility 90 reuses the LQ.5 Fire branch — being
            // pitched makes fire lethal. Proves the stat debuff and the
            // element vulnerability coexist on one coat.
            var pitched = MakeCreature();
            pitched.ApplyEffect(new LiquidCoveredEffect("pitch", 30));
            int before = pitched.GetStatValue("Hitpoints");
            var d = new Damage(20); d.AddAttribute("Fire");
            CombatSystem.ApplyDamage(pitched, d, null, null);
            int pitchedDmg = before - pitched.GetStatValue("Hitpoints");

            var bare = MakeCreature();
            int b0 = bare.GetStatValue("Hitpoints");
            var d2 = new Damage(20); d2.AddAttribute("Fire");
            CombatSystem.ApplyDamage(bare, d2, null, null);
            int bareDmg = b0 - bare.GetStatValue("Hitpoints");

            Assert.Greater(pitchedDmg, bareDmg, "Pitch must amplify Fire (lethal trade-off).");
        }

        [Test]
        public void IchorCoat_AppliesAVUp_AND_ColdResDown()
        {
            var c = MakeCreature();
            c.ApplyEffect(new LiquidCoveredEffect("carapace-ichor", 30));
            Assert.AreEqual(6, V(c, "AV"), "Ichor hardens: +4 AV (2→6).");
            Assert.AreEqual(-20, V(c, "ColdResistance"),
                "Ichor's trade-off: −20 ColdResistance (brittle shell).");
        }

        // ── EquipBonus net-zero invariant ───────────────────────

        [Test]
        public void EveryCoat_RemovedExplicitly_NetsZero()
        {
            foreach (var id in new[] { "brine", "pitch", "carapace-ichor" })
            {
                var c = MakeCreature();
                var fx = c.GetPart<StatusEffectsPart>();
                fx.ApplyEffect(new LiquidCoveredEffect(id, 30));
                fx.RemoveEffect<LiquidCoveredEffect>();
                Assert.AreEqual(0, V(c, "HeatResistance"), id + " HeatRes net-zero");
                Assert.AreEqual(0, V(c, "ElectricResistance"), id + " ElecRes net-zero");
                Assert.AreEqual(0, V(c, "ColdResistance"), id + " ColdRes net-zero");
                Assert.AreEqual(14, V(c, "Agility"), id + " Agility net-zero");
                Assert.AreEqual(6, V(c, "DV"), id + " DV net-zero");
                Assert.AreEqual(2, V(c, "AV"), id + " AV net-zero");
            }
        }

        [Test]
        public void BrineCoat_DriesDownByEvaporation_ReversesDeltas()
        {
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("brine", 20));
            Assert.AreEqual(15, V(c, "HeatResistance"), "applied while coated");
            for (int i = 0; i < 200 && fx.HasEffect<LiquidCoveredEffect>(); i++)
            {
                var ev = GameEvent.New("EndTurn");
                ev.SetParameter("Actor", (object)c);
                c.FireEventAndRelease(ev);
            }
            Assert.IsFalse(fx.HasEffect<LiquidCoveredEffect>(), "coat dried off");
            Assert.AreEqual(0, V(c, "HeatResistance"),
                "Evaporation removal must reverse the stat deltas (net-zero).");
            Assert.AreEqual(0, V(c, "ElectricResistance"));
        }

        [Test]
        public void ReCoatSameLiquid_ThenRemove_NetsZero_NoDoubleApply()
        {
            // Re-applying the same liquid merges via OnStack (no second
            // apply of the deltas); a single removal must still net-zero.
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("brine", 20));
            fx.ApplyEffect(new LiquidCoveredEffect("brine", 20)); // merge
            Assert.AreEqual(15, V(c, "HeatResistance"),
                "Re-coat must NOT double-apply (+15, not +30).");
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(0, V(c, "HeatResistance"), "single removal nets zero");
            Assert.AreEqual(0, V(c, "ElectricResistance"));
        }

        [Test]
        public void IdSwap_StrongerWins_ReversesOld_AppliesNew_ThenNetsZero()
        {
            // brine (HeatRes+15) then a STRONGER pitch coat: OnStack must
            // reverse brine's deltas and apply pitch's, leaving HeatRes
            // back at 0 and Agility −2. Final removal nets everything.
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("brine", 20));
            fx.ApplyEffect(new LiquidCoveredEffect("pitch", 100)); // stronger → swap
            Assert.AreEqual(0, V(c, "HeatResistance"),
                "Brine's HeatRes must be reversed on the id swap (no leak).");
            Assert.AreEqual(0, V(c, "ElectricResistance"));
            Assert.AreEqual(12, V(c, "Agility"), "Pitch's Agility debuff now applied.");
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(14, V(c, "Agility"), "final removal nets zero");
            Assert.AreEqual(6, V(c, "DV"));
        }

        // ── Save/load: exactly once (no double-apply on load) ───

        [Test]
        public void SaveLoadMidCoat_PreservesDeltas_ExactlyOnce()
        {
            var c = MakeCreature();
            c.ApplyEffect(new LiquidCoveredEffect("brine", 30));
            Assert.AreEqual(15, V(c, "HeatResistance"));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(c);

            Assert.AreEqual(15, V(loaded, "HeatResistance"),
                "Mid-coat save/load must keep +15 (single application — not +30).");
            Assert.AreEqual(-15, V(loaded, "ElectricResistance"));
            var coat = loaded.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.IsNotNull(coat, "coat effect round-trips");
            StringAssert.Contains("HeatResistance:15", coat.AppliedModsRaw,
                "AppliedModsRaw round-trips so removal can reverse exactly.");
            loaded.GetPart<StatusEffectsPart>().RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(0, V(loaded, "HeatResistance"),
                "Post-load removal still nets zero (reversal uses round-tripped record).");
            Assert.AreEqual(0, V(loaded, "ElectricResistance"));
        }

        // ── Expandability proof (the brief's "more than 3") ─────

        [Test]
        public void Expandability_FourthLiquid_JsonOnly_NoCSharp()
        {
            // "quicksilver" exists ONLY as a JSON row in Setup — there is
            // no QuicksilverEffect / QuicksilverLiquid C# class anywhere.
            // It coats, applies its delta, and nets zero on removal with
            // zero new code. This is the brief's expand-to-more-than-3.
            var c = MakeCreature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("quicksilver", 30));
            Assert.AreEqual(21, V(c, "Strength"),
                "A data-only 4th liquid applies its delta (+5 Str, 16→21).");
            Assert.AreEqual("silvered",
                fx.GetEffect<LiquidCoveredEffect>().DisplayName,
                "…and its adjective, with no C# change.");
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(16, V(c, "Strength"), "data-only liquid nets zero too");
        }

        // ── Counter-checks / adversarial ────────────────────────

        [Test]
        public void WaterCoat_HasNoStatModifiers_NoChange()
        {
            var c = MakeCreature();
            c.ApplyEffect(new LiquidCoveredEffect("water", 30));
            Assert.AreEqual(0, V(c, "HeatResistance"), "water has no StatModifiers");
            Assert.AreEqual(14, V(c, "Agility"));
            Assert.AreEqual("", c.GetPart<StatusEffectsPart>()
                .GetEffect<LiquidCoveredEffect>().AppliedModsRaw,
                "no mods applied → AppliedModsRaw stays empty");
        }

        [Test]
        public void StatAbsentOnEntity_SkippedGracefully_NoCrash()
        {
            // A bare entity lacking the modified stats: applying a brine
            // coat must not crash and must leave AppliedModsRaw without
            // the absent stats (so reversal stays exact).
            var bare = new Entity { ID = "wisp", BlueprintName = "wisp" };
            bare.Tags["Creature"] = "";
            bare.Statistics["Hitpoints"] =
                new Stat { Owner = bare, Name = "Hitpoints", BaseValue = 10 };
            bare.AddPart(new StatusEffectsPart());
            Assert.DoesNotThrow(() =>
                bare.ApplyEffect(new LiquidCoveredEffect("brine", 30)));
            var coat = bare.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.IsNotNull(coat);
            Assert.AreEqual("", coat.AppliedModsRaw,
                "Absent stats are skipped — nothing recorded, nothing to reverse.");
        }

        [Test]
        public void StatModApplied_EmitsLiquidDiag()
        {
            var c = MakeCreature();
            c.ApplyEffect(new LiquidCoveredEffect("brine", 30));
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "StatModApplied", Limit = 10 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"liquidId\":\"brine\"", recs[0].PayloadJson);
        }
    }
}
