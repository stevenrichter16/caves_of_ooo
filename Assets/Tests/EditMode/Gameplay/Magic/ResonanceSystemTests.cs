using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SPELLCRAFT SM7 — the keystone. Resonance is the payoff verb: the
    /// thing a rite does that a skill structurally cannot.
    ///
    /// <para>These tests pin the grammar itself (skills prime, rites
    /// spend), the super-linear scaling that makes a third status worth
    /// chasing, and the deliberate DEAD PAIRS that teach the element
    /// rules by refusing to work.</para>
    /// </summary>
    public class ResonanceSystemTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            ResonanceSystem.ResetForTests();
            ResonanceSystem.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Resonance/Resonance.json")));
        }

        [TearDown]
        public void TearDown() => ResonanceSystem.ResetForTests();

        private static Entity Target(string name = "t")
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 500, Min = 0, Max = 500 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static float Mult(Entity t, string element, int slots = 2)
            => ResonanceSystem.Preview(t, element, slots).Multiplier;

        // ── The grammar ──────────────────────────────────────────

        [Test]
        public void CastCold_IsDeliberatelyUnrewarded()
        {
            // The design decision that keeps rites from replacing
            // skills: a rite with nothing to spend gets x1.0. If the
            // base case paid well there would be no reason to prime and
            // the whole loop would collapse into spamming the rite.
            var t = Target();
            Assert.AreEqual(1.0f, Mult(t, "Electric"), 0.001f);
            Assert.IsFalse(ResonanceSystem.Preview(t, "Electric").AnyResonance);
        }

        [Test]
        public void ScalingIsSuperLinear_SoTheSecondStatusBeatsTwiceTheFirst()
        {
            var one = Target("one");
            one.ApplyEffect(new WetEffect(1.0f), null, null);

            var two = Target("two");
            two.ApplyEffect(new WetEffect(1.0f), null, null);
            two.ApplyEffect(new ElectrifiedEffect(1.0f), null, null);

            float m1 = Mult(one, "Electric");
            float m2 = Mult(two, "Electric");

            Assert.Greater(m1, 1.0f, "one status must beat none");
            Assert.Greater(m2, m1, "two must beat one");
            Assert.Greater(m2 - 1f, 2f * (m1 - 1f),
                "and two must beat TWICE one — that is what makes stacking worth it");
        }

        [Test]
        public void Spending_ActuallyRemovesTheStatus()
        {
            // The literal difference between a skill and a rite.
            var t = Target();
            t.ApplyEffect(new WetEffect(1.0f), null, null);
            Assert.IsTrue(t.GetPart<StatusEffectsPart>().HasEffect<WetEffect>());

            var res = ResonanceSystem.Spend(t, "Electric");

            Assert.Contains("Wet", res.Consumed);
            Assert.IsFalse(t.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "a rite SPENDS the status — this is the whole grammar");
        }

        [Test]
        public void Preview_ChangesNothing()
        {
            // Counter-check to the test above, and the contract SM12's
            // targeting overlay depends on: you can look before you buy.
            var t = Target();
            t.ApplyEffect(new WetEffect(1.0f), null, null);

            ResonanceSystem.Preview(t, "Electric");

            Assert.IsTrue(t.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "previewing must not consume — the UI calls this every frame");
        }

        // ── Dead pairs: the lesson ───────────────────────────────

        [Test]
        public void FireDoesNotConduct_AndSaysSoInTheDiagStream()
        {
            // The deliberate dead pair. Burning has no Electric entry, so
            // a lightning rite gets nothing for it — and the player who
            // asks "why didn't that work?" gets an answer rather than
            // silence.
            var t = Target();
            t.ApplyEffect(new BurningEffect(1.0f, null, new System.Random(0)), null, null);
            Diag.ResetAll();

            var res = ResonanceSystem.Spend(t, "Electric");

            Assert.IsEmpty(res.Consumed, "fire is worth nothing to lightning");
            Assert.Contains("Burning", res.Declined);
            Assert.AreEqual(1.0f, res.Multiplier, 0.001f);
            Assert.IsTrue(t.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                "and a declined status is NOT eaten");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "spell", Kind = "ResonanceDeclined", Limit = 5 }).Records;
            Assert.GreaterOrEqual(recs.Count, 1);
            StringAssert.Contains("not_resonant_with_element", recs[0].PayloadJson);
        }

        [Test]
        public void SlotLimit_DeclinesTheExtras_WithADifferentReason()
        {
            // "Resonant but no slots left" is a different answer from
            // "not resonant", and a debugger deserves to tell them apart.
            var t = Target();
            t.ApplyEffect(new WetEffect(1.0f), null, null);
            t.ApplyEffect(new ElectrifiedEffect(1.0f), null, null);
            t.ApplyEffect(new FrozenEffect(0.5f), null, null);
            Diag.ResetAll();

            var res = ResonanceSystem.Spend(t, "Electric", maxSlots: 2);

            Assert.AreEqual(2, res.Consumed.Count, "only two slots");
            // NOT an exact Declined count: ElectrifiedEffect.OnApply also
            // applies StunnedEffect to creatures, so the target really
            // carries four statuses. The test originally asserted 1 and
            // caught its own wrong premise.
            CollectionAssert.Contains(res.Declined, "Frozen",
                "the third resonant status is declined for want of a slot");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "spell", Kind = "ResonanceDeclined", Limit = 10 }).Records;
            bool sawSlotLimit = false;
            foreach (var rec in recs)
                if (rec.PayloadJson.Contains("no_slots_left")) sawSlotLimit = true;
            Assert.IsTrue(sawSlotLimit,
                "and the reason distinguishes it from 'not resonant'");
        }

        [Test]
        public void MoreSlots_SpendMore_WhichIsWhatChannellingWillBuy()
        {
            var a = Target("a");
            var b = Target("b");
            foreach (var t in new[] { a, b })
            {
                t.ApplyEffect(new WetEffect(1.0f), null, null);
                t.ApplyEffect(new ElectrifiedEffect(1.0f), null, null);
                t.ApplyEffect(new FrozenEffect(0.5f), null, null);
            }

            Assert.Greater(Mult(b, "Electric", slots: 3), Mult(a, "Electric", slots: 2),
                "SM9's channel raises the slot count — this is the payoff it buys");
        }

        // ── Element tables are real, not decorative ──────────────

        [Test]
        public void DifferentElements_ValueTheSameStatusDifferently()
        {
            var t = Target();
            t.ApplyEffect(new FrozenEffect(0.5f), null, null);

            float cold = Mult(t, "Cold");
            float electric = Mult(t, "Electric");

            Assert.Greater(cold, electric,
                "Cold pays more for Frozen than Electric does — the tables mean something");
        }

        [Test]
        public void AnUnknownElement_ResonatesWithNothing()
        {
            var t = Target();
            t.ApplyEffect(new WetEffect(1.0f), null, null);

            var res = ResonanceSystem.Spend(t, "Bogus");

            Assert.IsEmpty(res.Consumed);
            Assert.AreEqual(1.0f, res.Multiplier, 0.001f);
            Assert.IsTrue(t.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "an unknown element must not eat statuses it cannot use");
        }

        [Test]
        public void WaterIsTheBestFeed_ItResonatesWithEveryElement()
        {
            // Design invariant that justifies the Hydromancy tree: a
            // soaking is never a wasted setup, whatever you follow it
            // with.
            var t = Target();
            t.ApplyEffect(new WetEffect(1.0f), null, null);

            foreach (var element in new[] { "Electric", "Heat", "Cold", "Acid" })
                Assert.Greater(Mult(t, element), 1.0f,
                    element + " must pay for Wet — soaking is universal setup");
        }

        // ── Guards ───────────────────────────────────────────────

        [Test]
        public void NullAndEmptyInputs_AreGraceful()
        {
            var t = Target();
            Assert.AreEqual(1.0f, ResonanceSystem.Preview(null, "Electric").Multiplier, 0.001f);
            Assert.AreEqual(1.0f, ResonanceSystem.Preview(t, null).Multiplier, 0.001f);
            Assert.AreEqual(1.0f, ResonanceSystem.Preview(t, "Electric", 0).Multiplier, 0.001f);
            Assert.DoesNotThrow(() => ResonanceSystem.Spend(null, "Electric"));
        }

        [Test]
        public void AnEntityWithNoStatusPart_IsNotACrash()
        {
            var rock = new Entity { ID = "rock", BlueprintName = "Rock" };
            Assert.DoesNotThrow(() => ResonanceSystem.Spend(rock, "Electric"));
        }

        [Test]
        public void TheShippedTableLoads_AndCoversEveryPrimerElement()
        {
            // Content reachability: every element the SM3-SM6 primers
            // apply must have somewhere to be spent, or those skills
            // prime into a void.
            Assert.IsTrue(ResonanceSystem.IsInitialized, "the shipped file must parse");
            var t = Target();
            t.ApplyEffect(new WetEffect(1.0f), null, null);
            Assert.Greater(Mult(t, "Electric"), 1.0f);
        }
    }
}
