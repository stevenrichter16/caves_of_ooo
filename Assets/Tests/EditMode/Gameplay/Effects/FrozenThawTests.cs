using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Docs/FREEZE-THAW.md. User-visible invariant: "A frozen creature or
    /// object thaws in proportion to how hard it was frozen, and fire
    /// thaws it."
    ///
    /// Why this exists: a Quench (cooldown 6) pushed the target's body
    /// below freezing and the thermal path always applied Cold = 1.0.
    /// Frozen then HELD while the body stayed below freezing, and the body
    /// only warms 2% of the gap to ambient per turn (~65 turns), so one
    /// cheap cast was a permanent lock (a sim build with Quench won every
    /// scenario at 100%). Nothing in the game thawed it.
    /// </summary>
    public class FrozenThawTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // A creature-shaped body: HP, flesh, thermal. Starts at `temperature`.
        private static Entity Body(float temperature, float heatCapacity = 1.0f, bool creature = true)
        {
            var e = new Entity { BlueprintName = creature ? "TestBody" : "TestObject" };
            if (creature) e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100, Owner = e };
            e.AddPart(new RenderPart { DisplayName = creature ? "test body" : "test post" });
            e.AddPart(new MaterialPart
            {
                MaterialID = creature ? "Flesh" : "Wood",
                Combustibility = 0.5f,
                MaterialTagsRaw = creature ? "Organic" : "Organic,Flammable,Freezable"
            });
            e.AddPart(new ThermalPart { Temperature = temperature, HeatCapacity = heatCapacity, FlameTemperature = 400f });
            return e;
        }

        private static void Heat(Entity e, float joules)
        {
            var heat = GameEvent.New("ApplyHeat");
            heat.SetParameter("Joules", (object)joules);
            heat.SetParameter("Radiant", (object)false);
            e.FireEvent(heat);
            heat.Release();
        }

        private static int TurnsUntilThawed(Entity e, int max = 80)
        {
            for (int turn = 1; turn <= max; turn++)
            {
                e.FireEvent(GameEvent.New("EndTurn"));
                if (!e.HasEffect<FrozenEffect>()) return turn;
            }
            return int.MaxValue;
        }

        private static int Recorded(string kind, string cause = null)
        {
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "effect", Kind = kind, Limit = 200 }).Records;
            if (cause == null) return recs.Count;
            int n = 0;
            foreach (var r in recs) if (r.PayloadJson.Contains("\"cause\":\"" + cause + "\"")) n++;
            return n;
        }

        // ════════════════════════════════════════════════════════
        // Thaw time follows magnitude
        // ════════════════════════════════════════════════════════

        [TestCase(0.2f, 2)]
        [TestCase(0.5f, 5)]
        [TestCase(1.0f, 10)]
        public void ThawTime_IsMagnitudeOverRate(float cold, int expectedTurns)
        {
            var e = Body(temperature: -50f);   // below freezing: ambient warmth plays no part
            e.ApplyEffect(new FrozenEffect(cold));

            Assert.AreEqual(expectedTurns, TurnsUntilThawed(e),
                $"cold {cold} at {FrozenEffect.THAW_PER_TURN}/turn");
        }

        [Test]
        public void StrongerFreeze_ThawsLater_ThanWeakerFreeze()
        {
            var weak = Body(-50f); weak.ApplyEffect(new FrozenEffect(0.3f));
            var strong = Body(-50f); strong.ApplyEffect(new FrozenEffect(0.9f));

            Assert.Less(TurnsUntilThawed(weak), TurnsUntilThawed(strong));
        }

        [Test]
        public void ColdBody_StillThaws_ItIsNoLongerHeldByFreezingTemperature()
        {
            // RED pre-fix: below FreezeTemperature the old OnTurnEnd did
            // nothing at all, so Cold never moved.
            var e = Body(-80f);
            e.ApplyEffect(new FrozenEffect(0.3f));

            Assert.AreEqual(3, TurnsUntilThawed(e));
            Assert.Less(e.GetPart<ThermalPart>().Temperature, 0f,
                "the body is still cold when it thaws; thaw is about the ice, not the temperature");
        }

        [Test]
        public void WarmBody_ThawsFaster_ThanColdBody()
        {
            var warm = Body(200f); warm.ApplyEffect(new FrozenEffect(1.0f));
            var cold = Body(-50f); cold.ApplyEffect(new FrozenEffect(1.0f));

            Assert.Less(TurnsUntilThawed(warm), TurnsUntilThawed(cold));
        }

        [Test]
        public void SoakedTarget_FreezesDeeper_SoThawsLater()
        {
            var dry = Body(-50f); dry.ApplyEffect(new FrozenEffect(0.4f));
            var wet = Body(-50f); wet.ApplyEffect(new WetEffect(0.8f)); wet.ApplyEffect(new FrozenEffect(0.4f));

            Assert.AreEqual(4, TurnsUntilThawed(dry));
            Assert.AreEqual(6, TurnsUntilThawed(wet), "0.4 x 1.5 = 0.6");
        }

        [Test]
        public void TurnsToThaw_ReportsTheRemainingEstimate()
        {
            var fz = new FrozenEffect(0.5f);
            Assert.AreEqual(5, fz.TurnsToThaw);
            fz.Thaw(0.2f, "test");
            Assert.AreEqual(3, fz.TurnsToThaw);
        }

        // ════════════════════════════════════════════════════════
        // A cold dose freezes as hard as it is deep
        // ════════════════════════════════════════════════════════

        [Test]
        public void ColdForDepth_IsMonotonic_FlooredAndCapped()
        {
            Assert.AreEqual(FrozenEffect.FREEZE_FLOOR, FrozenEffect.ColdForDepth(0f), 1e-5f);
            Assert.Less(FrozenEffect.ColdForDepth(30f), FrozenEffect.ColdForDepth(300f));
            Assert.AreEqual(1.0f, FrozenEffect.ColdForDepth(10000f), 1e-5f);
            Assert.AreEqual(FrozenEffect.FREEZE_FLOOR, FrozenEffect.ColdForDepth(-40f), 1e-5f,
                "a negative depth is not a deeper freeze");
        }

        [Test]
        public void ShallowDip_MakesAShallowerFreeze_ThanADeepDip()
        {
            var shallow = Body(20f); Heat(shallow, -50f);
            var deep = Body(20f); Heat(deep, -500f);

            Assert.IsTrue(shallow.HasEffect<FrozenEffect>(), "crossing 0 still freezes");
            Assert.IsTrue(deep.HasEffect<FrozenEffect>());
            Assert.Less(shallow.GetEffect<FrozenEffect>().Cold, deep.GetEffect<FrozenEffect>().Cold);
            Assert.AreEqual(FrozenEffect.ColdForDepth(30f), shallow.GetEffect<FrozenEffect>().Cold, 1e-4f,
                "20 - 50 = -30, 30 degrees below freezing");
        }

        [Test]
        public void QuenchSizedDose_ThawsBeforeQuenchCanBeCastAgain()
        {
            // The spell: Wet 0.8 plus a -150 J chill on a creature (flesh,
            // heat capacity 1.6) at room temperature. Pre-fix this froze for
            // ~65 turns against a cooldown of 6.
            var e = Body(25f, heatCapacity: 1.6f);
            e.ApplyEffect(new WetEffect(0.8f));
            Heat(e, Hydromancy_Quench.COOL_JOULES);
            Assert.IsTrue(e.HasEffect<FrozenEffect>(), "the dose still freezes (this is a nerf, not a removal)");

            Assert.Less(TurnsUntilThawed(e), Hydromancy_Quench.COOLDOWN,
                "a Quench must not be able to chain itself into a permanent lock");
        }

        [Test]
        public void DeepChill_StillOutlastsTheQuenchCooldown()
        {
            // Counter-check: magnitude is the lever, so a real cold source
            // keeps its teeth.
            var e = Body(25f, heatCapacity: 1.6f);
            Heat(e, -1500f);

            Assert.Greater(TurnsUntilThawed(e), Hydromancy_Quench.COOLDOWN);
        }

        // ════════════════════════════════════════════════════════
        // Fire thaws
        // ════════════════════════════════════════════════════════

        [Test]
        public void HeatDose_ThawsByTheTemperatureItRaises()
        {
            var e = Body(-50f);
            e.ApplyEffect(new FrozenEffect(0.5f));

            Heat(e, 100f);   // +100 degrees at capacity 1.0
            Assert.AreEqual(0.5f - 100f * FrozenEffect.THAW_PER_DEGREE_OF_HEAT,
                e.GetEffect<FrozenEffect>().Cold, 1e-4f);

            Heat(e, 100f);   // another +100: the rest of it
            Assert.IsFalse(e.HasEffect<FrozenEffect>(), "enough heat thaws it outright");
        }

        [Test]
        public void ColdDose_DoesNotThaw()
        {
            var e = Body(-50f);
            e.ApplyEffect(new FrozenEffect(0.5f));

            Heat(e, -100f);

            Assert.AreEqual(0.5f, e.GetEffect<FrozenEffect>().Cold, 1e-5f);
        }

        [Test]
        public void HeatCapacity_DecidesHowFarADoseGoes()
        {
            var light = Body(-50f, heatCapacity: 0.5f); light.ApplyEffect(new FrozenEffect(1.0f));
            var heavy = Body(-50f, heatCapacity: 4.0f); heavy.ApplyEffect(new FrozenEffect(1.0f));

            Heat(light, 100f);
            Heat(heavy, 100f);

            Assert.Less(light.GetEffect<FrozenEffect>().Cold, heavy.GetEffect<FrozenEffect>().Cold,
                "the same joules warm a light body more, so thaw it more");
        }

        [Test]
        public void IgnitingAFrozenTarget_MeltsIceByIntensity_AndStillCatchesFire()
        {
            // Fire can thaw: the flame melts THAW_PER_BURN_INTENSITY of Cold per point
            // of intensity. It does not stop the target from igniting (a frozen thing
            // that is also burning is the spellcraft priming state: see the trio test).
            var e = Body(-50f);
            e.ApplyEffect(new FrozenEffect(1.0f));

            bool applied = e.ApplyEffect(new BurningEffect(intensity: 0.4f));

            Assert.IsTrue(applied, "the flame took hold");
            Assert.IsTrue(e.HasEffect<BurningEffect>());
            Assert.AreEqual(1.0f - 0.4f * FrozenEffect.THAW_PER_BURN_INTENSITY,
                e.GetEffect<FrozenEffect>().Cold, 1e-4f, "and it cost the ice 0.4 x 0.5");
            Assert.AreEqual(1, Recorded("Thawed", "ignition"));
        }

        [Test]
        public void IgnitingAFrozenTarget_WithEnoughFlame_ThawsItCompletely()
        {
            var e = Body(-50f);
            e.ApplyEffect(new FrozenEffect(0.8f));

            bool applied = e.ApplyEffect(new BurningEffect(intensity: 2.0f));

            Assert.IsTrue(applied);
            Assert.IsFalse(e.HasEffect<FrozenEffect>(), "2.0 x 0.5 = 1.0 of thaw against 0.8 of ice");
            Assert.IsTrue(e.HasEffect<BurningEffect>());
        }

        [Test]
        public void WetThenFrozenThenBurning_StillStacksAllThree_ForTheResonanceRites()
        {
            // The consuming rites (Hollow Coin, Sundering Word) are primed by three
            // elemental marks on one target. Fire thawing ice must not make
            // Frozen + Burning mutually exclusive in this order, or that priming dies.
            var e = Body(25f);
            e.ApplyEffect(new WetEffect(1.0f));
            e.ApplyEffect(new FrozenEffect());

            e.ApplyEffect(new BurningEffect());

            Assert.IsTrue(e.HasEffect<WetEffect>());
            Assert.IsTrue(e.HasEffect<FrozenEffect>(), "thinned by the flame, not erased by it");
            Assert.IsTrue(e.HasEffect<BurningEffect>());
            Assert.Less(e.GetEffect<FrozenEffect>().Cold, 1.0f);
        }

        [Test]
        public void IgnitingAnUnfrozenTarget_IsUnchanged()
        {
            // Counter-check: the thaw branch must not touch ordinary ignition.
            var e = Body(25f);

            Assert.IsTrue(e.ApplyEffect(new BurningEffect(intensity: 0.4f)));
            Assert.IsTrue(e.HasEffect<BurningEffect>());
            Assert.AreEqual(0, Recorded("Thawed"));
        }

        [Test]
        public void FireDamage_Thaws_InProportionToTheDamage()
        {
            var zone = new Zone("Z");
            var e = Body(-50f);
            zone.AddEntity(e, 5, 5);
            e.ApplyEffect(new FrozenEffect(0.6f));

            var fire = new Damage(5); fire.AddAttribute("Fire");
            CombatSystem.ApplyDamage(e, fire, null, zone);

            Assert.AreEqual(0.6f - 5 * FrozenEffect.THAW_PER_FIRE_DAMAGE,
                e.GetEffect<FrozenEffect>().Cold, 1e-4f);
        }

        [Test]
        public void HeatDamage_AlsoThaws()
        {
            var zone = new Zone("Z");
            var e = Body(-50f);
            zone.AddEntity(e, 5, 5);
            e.ApplyEffect(new FrozenEffect(0.6f));

            var heat = new Damage(5); heat.AddAttribute("Heat");
            CombatSystem.ApplyDamage(e, heat, null, zone);

            Assert.Less(e.GetEffect<FrozenEffect>().Cold, 0.6f);
        }

        [TestCase("Slashing")]
        [TestCase("Cold")]
        [TestCase("Bludgeoning")]
        public void NonFireDamage_DoesNotThaw(string attribute)
        {
            // Counter-check for the two tests above. (Damage does not thaw
            // by itself; the fire in it does.)
            var zone = new Zone("Z");
            var e = Body(-50f);
            zone.AddEntity(e, 5, 5);
            e.ApplyEffect(new FrozenEffect(0.6f));

            var dmg = new Damage(5); dmg.AddAttribute(attribute);
            CombatSystem.ApplyDamage(e, dmg, null, zone);

            Assert.AreEqual(0.6f, e.GetEffect<FrozenEffect>().Cold, 1e-5f);
        }

        // ════════════════════════════════════════════════════════
        // Objects thaw too
        // ════════════════════════════════════════════════════════

        [Test]
        public void FrozenObject_ThawsOnTheSameSchedule()
        {
            var post = Body(-50f, creature: false);
            post.ApplyEffect(new FrozenEffect(0.5f));

            Assert.AreEqual(5, TurnsUntilThawed(post));
        }

        [Test]
        public void FrozenObject_IsThawedByHeat()
        {
            var post = Body(-50f, creature: false);
            post.ApplyEffect(new FrozenEffect(0.5f));

            Heat(post, 300f);

            Assert.IsFalse(post.HasEffect<FrozenEffect>());
        }

        // ════════════════════════════════════════════════════════
        // Observability (CLAUDE.md: every gate emits a diag record)
        // ════════════════════════════════════════════════════════

        [Test]
        public void ThawingByHeat_RecordsItsCause()
        {
            var e = Body(-50f);
            e.ApplyEffect(new FrozenEffect(0.5f));

            Heat(e, 100f);

            Assert.AreEqual(1, Recorded("Thawed", "heat"));
        }

        [Test]
        public void ThawingByFireDamage_RecordsItsCause()
        {
            var zone = new Zone("Z");
            var e = Body(-50f);
            zone.AddEntity(e, 5, 5);
            e.ApplyEffect(new FrozenEffect(0.6f));

            var fire = new Damage(2); fire.AddAttribute("Fire");
            CombatSystem.ApplyDamage(e, fire, null, zone);

            Assert.AreEqual(1, Recorded("Thawed", "fire"));
        }

        [Test]
        public void ThawingByTimeAlone_DoesNotFloodTheDiagBuffer()
        {
            var e = Body(-50f);
            e.ApplyEffect(new FrozenEffect(1.0f));

            TurnsUntilThawed(e);

            Assert.AreEqual(0, Recorded("Thawed"),
                "ten ticks of ordinary thaw are not events; heat and fire are");
        }

        // ════════════════════════════════════════════════════════
        // What the player reads
        // ════════════════════════════════════════════════════════

        [Test]
        public void Describer_SaysHowLongTheThawWillTake()
        {
            var text = EffectDescriber.Describe(new FrozenEffect(0.5f));

            StringAssert.Contains("Frozen over", text, "existing prefix is pinned by look-mode tests");
            StringAssert.Contains("5 turns", text);
            StringAssert.Contains("fire", text.ToLowerInvariant());
        }
    }
}
