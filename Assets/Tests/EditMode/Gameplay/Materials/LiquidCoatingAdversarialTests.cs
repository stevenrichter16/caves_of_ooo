using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LQ.7 dedicated adversarial sweep (ADVERSARIAL_TESTING.md gate).
    /// The liquid-coating feature touches ≥2 taxonomy surfaces — a
    /// parser (AppliedModsRaw), stacking/merge (OnStack), save/load
    /// reflection (WritePublicFields), cross-actor flow (pool→creature),
    /// state atomicity (id-swap reverse-then-apply), boundary inputs
    /// (exposure clamp) — so a dedicated sweep is mandatory.
    ///
    /// 0 bugs found does NOT prove bug-free (bounded by imagined bug
    /// classes); the value is (a) the rare real catch + (b) regression
    /// pins so future changes break visibly. Each test states the bug
    /// class and why a buggy impl would fail it.
    /// </summary>
    public class LiquidCoatingAdversarialTests
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
                { ""Id"": ""oil"", ""DisplayName"": ""oil"", ""Adjective"": ""oily"",
                  ""Combustibility"": 90, ""Fluidity"": 5, ""Evaporativity"": 2 },
                { ""Id"": ""acid"", ""DisplayName"": ""acid"", ""Adjective"": ""acid-covered"",
                  ""Fluidity"": 20, ""Evaporativity"": 15,
                  ""PerTurnDamage"": { ""Amount"": 5, ""Type"": ""Acid"" } },
                { ""Id"": ""brine"", ""DisplayName"": ""brine"", ""Adjective"": ""briny"",
                  ""Conductivity"": 100, ""Fluidity"": 25, ""Evaporativity"": 10,
                  ""ResistanceModifiers"": [
                    { ""Stat"": ""HeatResistance"", ""Delta"": 15 },
                    { ""Stat"": ""ElectricResistance"", ""Delta"": -15 } ] },
                { ""Id"": ""pitch"", ""DisplayName"": ""pitch"", ""Adjective"": ""pitch-covered"",
                  ""Combustibility"": 90, ""Fluidity"": 5, ""Evaporativity"": 2,
                  ""StatModifiers"": [
                    { ""Stat"": ""Agility"", ""Delta"": -2 },
                    { ""Stat"": ""DV"", ""Delta"": -3 } ] }
              ]
            }");
        }

        [TearDown]
        public void TearDown() => LiquidRegistry.ResetForTests();

        private static Entity Creature()
        {
            var e = new Entity { ID = "c", BlueprintName = "C" };
            e.Tags["Creature"] = "";
            void S(string n, int v) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -100, Max = 300 };
            S("Hitpoints", 100); S("Toughness", 14); S("Strength", 16);
            S("Agility", 14); S("DV", 6); S("AV", 2);
            S("HeatResistance", 0); S("ElectricResistance", 0); S("ColdResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static (Zone z, Entity pool, Entity mover) StepInto(
            string id, int vol, int str = 16, int tough = 14)
        {
            var z = new Zone("Z");
            var pool = new Entity { ID = id + "P", BlueprintName = id + "P" };
            pool.AddPart(new RenderPart { RenderString = "~" });
            pool.AddPart(new PhysicsPart { Solid = false });
            pool.AddPart(new LiquidPoolPart { LiquidId = id, Volume = vol });
            var m = Creature();
            m.Statistics["Strength"].BaseValue = str;
            m.Statistics["Toughness"].BaseValue = tough;
            z.AddEntity(pool, 5, 5);
            z.AddEntity(m, 4, 5);
            MovementSystem.TryMove(m, z, 1, 0);
            return (z, pool, m);
        }

        // ════════════════ Parser: malformed AppliedModsRaw ════════════════
        // ReverseStatModifiers parses a flat "Stat:delta,..." string. A
        // corrupted save or a future bug must degrade gracefully, never
        // crash and never throw away the rest of the list.

        [Test]
        public void Adversarial_AppliedModsRaw_Garbage_RemoveDoesNotThrow()
        {
            var c = Creature();
            var fx = c.GetPart<StatusEffectsPart>();
            var coat = new LiquidCoveredEffect("water", 30);
            fx.ApplyEffect(coat);
            coat.AppliedModsRaw = "###not:a:number,,:,HeatResistance:notint,:5";
            Assert.DoesNotThrow(() => fx.RemoveEffect<LiquidCoveredEffect>(),
                "Malformed AppliedModsRaw must not crash removal.");
        }

        [Test]
        public void Adversarial_AppliedModsRaw_MixedValidAndGarbage_ValidStillReversed()
        {
            var c = Creature();
            c.GetStat("HeatResistance").Bonus = 15; // pretend applied
            var coat = new LiquidCoveredEffect("brine", 30) { Owner = c };
            coat.AppliedModsRaw = "junk,HeatResistance:15,also:bad";
            // Reverse via OnRemove path.
            c.GetPart<StatusEffectsPart>().ApplyEffect(coat); // OnApply re-derives; reset:
            coat.AppliedModsRaw = "junk,HeatResistance:15,also:bad";
            c.GetStat("HeatResistance").Bonus = 15;
            c.GetPart<StatusEffectsPart>().RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(0, c.GetStatValue("HeatResistance"),
                "A valid pair amid garbage must still be reversed.");
        }

        [Test]
        public void Adversarial_StatModifier_EmptyStatName_Skipped()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"": [
              { ""Id"":""x"", ""Adjective"":""x"", ""Fluidity"":10, ""Evaporativity"":5,
                ""StatModifiers"":[ { ""Stat"":"""", ""Delta"":9 },
                                    { ""Stat"":""AV"", ""Delta"":3 } ] } ] }");
            var c = Creature();
            c.ApplyEffect(new LiquidCoveredEffect("x", 20));
            Assert.AreEqual(5, c.GetStatValue("AV"), "valid mod applied (2+3)");
            var raw = c.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>().AppliedModsRaw;
            Assert.IsFalse(raw.Contains(":9"), "empty-stat-name row skipped, not recorded");
        }

        [Test]
        public void Adversarial_StatModifier_ZeroDelta_NotRecorded()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"": [
              { ""Id"":""z"", ""Adjective"":""z"", ""Fluidity"":10, ""Evaporativity"":5,
                ""StatModifiers"":[ { ""Stat"":""AV"", ""Delta"":0 } ] } ] }");
            var c = Creature();
            c.ApplyEffect(new LiquidCoveredEffect("z", 20));
            Assert.AreEqual("", c.GetPart<StatusEffectsPart>()
                .GetEffect<LiquidCoveredEffect>().AppliedModsRaw,
                "Delta 0 is a no-op — nothing applied, nothing to reverse.");
        }

        // ════════════════ Stacking / merge semantics ════════════════

        [Test]
        public void Adversarial_EqualAmountDifferentId_KeepsExisting_Deterministic()
        {
            var c = Creature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("water", 30));
            fx.ApplyEffect(new LiquidCoveredEffect("oil", 30)); // equal → NOT stronger
            Assert.AreEqual("water", fx.GetEffect<LiquidCoveredEffect>().LiquidId,
                "Equal amounts must deterministically keep the existing id (no flip-flop).");
            Assert.AreEqual(60, fx.GetEffect<LiquidCoveredEffect>().Amount,
                "Amounts still add on a non-dominant merge.");
        }

        [Test]
        public void Adversarial_WeakerDifferentId_NoIdChange_NoStatThrash()
        {
            var c = Creature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("brine", 50)); // HeatRes +15
            fx.ApplyEffect(new LiquidCoveredEffect("pitch", 10)); // weaker → no swap
            Assert.AreEqual("brine", fx.GetEffect<LiquidCoveredEffect>().LiquidId);
            Assert.AreEqual(15, c.GetStatValue("HeatResistance"),
                "A weaker different liquid must NOT reverse/replace the dominant's stats.");
            Assert.AreEqual(14, c.GetStatValue("Agility"),
                "…and must NOT apply its own (it didn't win the id).");
        }

        [Test]
        public void Adversarial_RepeatedReCoat_NeverDoubleAppliesStats()
        {
            var c = Creature();
            var fx = c.GetPart<StatusEffectsPart>();
            for (int i = 0; i < 10; i++)
                fx.ApplyEffect(new LiquidCoveredEffect("brine", 10));
            Assert.AreEqual(15, c.GetStatValue("HeatResistance"),
                "10 re-coats must still be +15 (idempotent), not +150.");
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(0, c.GetStatValue("HeatResistance"), "single removal nets zero");
        }

        [Test]
        public void Adversarial_IdSwapChain_BrineToPitchToBrine_NetsZero()
        {
            var c = Creature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("brine", 10));
            fx.ApplyEffect(new LiquidCoveredEffect("pitch", 100)); // swap → pitch
            fx.ApplyEffect(new LiquidCoveredEffect("brine", 500)); // swap → brine
            Assert.AreEqual("brine", fx.GetEffect<LiquidCoveredEffect>().LiquidId);
            Assert.AreEqual(15, c.GetStatValue("HeatResistance"), "brine re-applied");
            Assert.AreEqual(14, c.GetStatValue("Agility"), "pitch fully reversed mid-chain");
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(0, c.GetStatValue("HeatResistance"));
            Assert.AreEqual(6, c.GetStatValue("DV"));
        }

        // ════════════════ Save/load reflection reach ════════════════

        [Test]
        public void Adversarial_RoundTrip_PreservesId_Amount_AppliedMods()
        {
            var c = Creature();
            c.ApplyEffect(new LiquidCoveredEffect("brine", 42));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(c);
            var coat = loaded.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.IsNotNull(coat);
            Assert.AreEqual("brine", coat.LiquidId);
            Assert.AreEqual(42, coat.Amount);
            StringAssert.Contains("HeatResistance:15", coat.AppliedModsRaw);
            Assert.AreEqual(15, loaded.GetStatValue("HeatResistance"),
                "Stat bonus single after load (no double-apply).");
        }

        [Test]
        public void Adversarial_RoundTrip_NoStatLiquid_EmptyAppliedMods()
        {
            var c = Creature();
            c.ApplyEffect(new LiquidCoveredEffect("water", 30));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(c);
            var coat = loaded.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.IsNotNull(coat);
            Assert.AreEqual("", coat.AppliedModsRaw, "no-mod coat round-trips empty (not null/garbage)");
        }

        // ════════════════ Mid-evaporation / death ════════════════

        [Test]
        public void Adversarial_AcidTick_KillsTarget_OnTurnEndNoCrash()
        {
            var c = Creature();
            c.Statistics["Hitpoints"].BaseValue = 6; // acid 5/turn → dies fast
            var fx = c.GetPart<StatusEffectsPart>();
            var coat = new LiquidCoveredEffect("acid", 30);
            fx.ApplyEffect(coat);
            var ctx = GameEvent.New("BeginTakeAction");
            ctx.SetParameter("Zone", (object)null);
            Assert.DoesNotThrow(() =>
            {
                coat.OnTurnStart(c, ctx);          // damage → maybe death
                coat.OnTurnStart(c, ctx);          // tick again at <=0 HP
                coat.OnTurnEnd(c);                  // dry-down on possibly-dead
            });
            Assert.LessOrEqual(c.GetStatValue("Hitpoints"), 6);
        }

        [Test]
        public void Adversarial_OnTurnStart_DeadTarget_NoAcidTick()
        {
            var c = Creature();
            c.Statistics["Hitpoints"].BaseValue = 0;
            var coat = new LiquidCoveredEffect("acid", 30);
            c.GetPart<StatusEffectsPart>().ApplyEffect(coat);
            var ctx = GameEvent.New("BeginTakeAction");
            Assert.DoesNotThrow(() => coat.OnTurnStart(c, ctx));
            Assert.AreEqual(0, c.GetStatValue("Hitpoints"),
                "Dead target guard: no negative-HP underflow tick.");
        }

        // ════════════════ Two-pool atomicity (cross-actor) ════════════════

        [Test]
        public void Adversarial_TwoPoolsSameCell_ExactlyOneCoat_StrongerId()
        {
            var z = new Zone("Z");
            Entity P(string id, int v)
            {
                var p = new Entity { ID = id, BlueprintName = id };
                p.AddPart(new RenderPart { RenderString = "~" });
                p.AddPart(new PhysicsPart { Solid = false });
                p.AddPart(new LiquidPoolPart { LiquidId = id, Volume = v });
                return p;
            }
            var w = P("water", 40);
            var o = P("oil", 200);
            var m = Creature();
            m.Statistics["Strength"].BaseValue = 30;
            m.Statistics["Toughness"].BaseValue = 30;
            z.AddEntity(w, 5, 5); z.AddEntity(o, 5, 5); z.AddEntity(m, 4, 5);
            MovementSystem.TryMove(m, z, 1, 0);
            var fx = m.GetPart<StatusEffectsPart>();
            Assert.IsTrue(fx.RemoveEffect<LiquidCoveredEffect>(),
                "Exactly one coat instance (atomic merge, not two stacked).");
            Assert.IsFalse(fx.RemoveEffect<LiquidCoveredEffect>());
        }

        [Test]
        public void Adversarial_MultiInstance_TwoCreatures_IndependentCoats()
        {
            var a = Creature(); var b = Creature();
            a.ApplyEffect(new LiquidCoveredEffect("brine", 30));
            Assert.AreEqual(15, a.GetStatValue("HeatResistance"));
            Assert.AreEqual(0, b.GetStatValue("HeatResistance"),
                "Coating A must not bleed into B (no shared mutable flyweight state).");
        }

        // ════════════════ Boundary inputs ════════════════

        [Test]
        public void Adversarial_ExposureExactlyStrPlusTough()
        {
            var (_, _, m) = StepInto("water", 30, str: 16, tough: 14); // cap == 30 == vol
            var coat = m.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.IsNotNull(coat);
            Assert.AreEqual(30, coat.Amount, "Volume==cap boundary: exposure exactly cap.");
        }

        [Test]
        public void Adversarial_ExposureOneOverCap_Clamped()
        {
            var (_, _, m) = StepInto("water", 31, str: 16, tough: 14); // vol 31 > cap 30
            Assert.AreEqual(30,
                m.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>().Amount,
                "Volume cap+1 clamps to cap.");
        }

        [Test]
        public void Adversarial_NegativeAmountCtor_ClampedToZero()
        {
            var coat = new LiquidCoveredEffect("water", -999);
            Assert.AreEqual(0, coat.Amount, "ctor clamps negative Amount to 0.");
        }

        [Test]
        public void Adversarial_NullTarget_AllHooks_NoCrash()
        {
            var coat = new LiquidCoveredEffect("brine", 30);
            var ev = GameEvent.New("BeforeTakeDamage");
            ev.SetParameter("Damage", (object)new Damage(10));
            Assert.DoesNotThrow(() =>
            {
                coat.OnApply(null);
                coat.OnTurnStart(null, null);
                coat.OnTurnEnd(null);
                coat.OnBeforeTakeDamage(null, ev);
                coat.OnRemove(null);
                coat.OnStack(new LiquidCoveredEffect("oil", 5)); // Owner null
            });
        }

        [Test]
        public void Adversarial_UnknownLiquidId_GracefulEverywhere()
        {
            var c = Creature();
            var coat = new LiquidCoveredEffect("unobtanium", 30);
            Assert.DoesNotThrow(() => c.GetPart<StatusEffectsPart>().ApplyEffect(coat));
            Assert.AreEqual("liquid-covered", coat.DisplayName, "unknown id → fallback adjective");
            Assert.AreEqual("", coat.AppliedModsRaw, "unknown id → no stat mods");
            var ev = GameEvent.New("BeforeTakeDamage");
            var dmg = new Damage(20); dmg.AddAttribute("Fire");
            ev.SetParameter("Damage", (object)dmg);
            Assert.DoesNotThrow(() => coat.OnBeforeTakeDamage(c, ev));
            Assert.AreEqual(20, dmg.Amount, "unknown id → no damage modification");
        }

        [Test]
        public void Adversarial_RegistryResetMidLife_RemoveStillNetsZero()
        {
            // Apply brine (HeatRes+15), then wipe the registry, THEN
            // remove. Reversal must use AppliedModsRaw (not re-derive
            // from the now-missing def) — net-zero must still hold.
            var c = Creature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("brine", 30));
            Assert.AreEqual(15, c.GetStatValue("HeatResistance"));
            LiquidRegistry.ResetForTests();
            fx.RemoveEffect<LiquidCoveredEffect>();
            Assert.AreEqual(0, c.GetStatValue("HeatResistance"),
                "Reversal reads AppliedModsRaw, not the def — exact even after registry reset.");
        }

        // ════════════════ Diag dispatch invariants ════════════════

        [Test]
        public void Adversarial_Diag_CoatExpired_FiresOnRemoval()
        {
            var c = Creature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("water", 30));
            fx.RemoveEffect<LiquidCoveredEffect>();
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "CoatExpired", Limit = 10 }).Records;
            Assert.AreEqual(1, recs.Count, "Removal must emit exactly one CoatExpired.");
            StringAssert.Contains("\"liquidId\":\"water\"", recs[0].PayloadJson);
        }

        [Test]
        public void Adversarial_Diag_StatModPair_AppliedThenRemoved()
        {
            var c = Creature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("brine", 30));
            fx.RemoveEffect<LiquidCoveredEffect>();
            int applied = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "StatModApplied", Limit = 10 }).Records.Count;
            int removed = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "StatModRemoved", Limit = 10 }).Records.Count;
            Assert.AreEqual(1, applied, "exactly one StatModApplied");
            Assert.AreEqual(1, removed, "exactly one paired StatModRemoved");
        }

        [Test]
        public void Adversarial_Diag_WaterCoat_NoStatModRecords()
        {
            // Counter to the pair test: a no-mod liquid must emit NO
            // StatMod records (a buggy impl emitting empty-mod records
            // would pollute the stream).
            var c = Creature();
            c.GetPart<StatusEffectsPart>().ApplyEffect(new LiquidCoveredEffect("water", 30));
            Assert.AreEqual(0, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "StatModApplied", Limit = 10 }).Records.Count);
        }

        // ════════════════ Idempotent remove ════════════════

        [Test]
        public void Adversarial_RemoveTwice_SecondIsNoOp_StatsStayZero()
        {
            var c = Creature();
            var fx = c.GetPart<StatusEffectsPart>();
            fx.ApplyEffect(new LiquidCoveredEffect("brine", 30));
            Assert.IsTrue(fx.RemoveEffect<LiquidCoveredEffect>());
            Assert.IsFalse(fx.RemoveEffect<LiquidCoveredEffect>(),
                "Second remove finds nothing (idempotent).");
            Assert.AreEqual(0, c.GetStatValue("HeatResistance"),
                "Stats remain net-zero (no spurious second reversal underflow).");
        }
    }
}
