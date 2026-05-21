using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.6 — GasMaskPart + GasImmunityPart production tests. Promotes
    /// the G.5 test stubs (TestGasImmunity / TestRespiratoryReducer) to
    /// real Parts and pins their behavior at both unit level (one event,
    /// one Part) and integration (gas + creature with defense → expected
    /// reduced/zero damage).
    /// </summary>
    public class GasDefensesTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""DisplayName"":""poison vapor"",
                ""GasType"":""Poison"", ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":100, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" },
              { ""Id"":""cryo-mist"", ""DisplayName"":""cryo mist"",
                ""GasType"":""Cryo"", ""Glyph"":""°"", ""Color"":""&C"",
                ""DefaultDensity"":100, ""DefaultLevel"":1,
                ""BehaviorKind"":"""" } ] }");
            GasPoisonPart.TestRng = new System.Random(42);
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            GasPoisonPart.TestRng = null;
            SettlementRuntime.Reset();
        }

        private static Entity MakeCreature(Zone zone, int x, int y, int hpMax = 200)
        {
            var e = new Entity { ID = "c_" + x + "_" + y, BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", hpMax, hpMax); S("Toughness", 12);
            S("Agility", 14); S("DV", 6); S("AV", 0);
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════
        //   PART I — GasMaskPart unit tests
        // ════════════════════════════════════════════════════════════

        [Test]
        public void GasMaskPart_ReducesIntakeByPowerTimesFive()
        {
            // Default Power=10 → intake -50. Fire the event directly,
            // observe the param change.
            var creature = MakeCreature(new Zone("MaskUnit"), 5, 5);
            creature.AddPart(new GasMaskPart { Power = 10 });

            var e = GameEvent.New("GetRespiratoryPerformance");
            e.SetParameter("Intake", (object)100);
            creature.FireEvent(e);
            int intake = e.GetParameter<int>("Intake");
            e.Release();
            Assert.AreEqual(50, intake, "Power 10 → -50 intake");
        }

        [Test]
        public void GasMaskPart_Power20_ZerosIntakeFully()
        {
            // Power=20 → 100 reduction → intake floored at 0. This is
            // the "hazmat mask" — effectively immune via the ZeroIntake
            // gate downstream.
            var creature = MakeCreature(new Zone("HazmatUnit"), 5, 5);
            creature.AddPart(new GasMaskPart { Power = 20 });

            var e = GameEvent.New("GetRespiratoryPerformance");
            e.SetParameter("Intake", (object)100);
            creature.FireEvent(e);
            int intake = e.GetParameter<int>("Intake");
            e.Release();
            Assert.AreEqual(0, intake, "Power 20 → 0 intake (floored)");
        }

        [Test]
        public void GasMaskPart_HighPower_DoesNotProduceNegativeIntake()
        {
            // Defensive: Power=50 → would compute -250, must clamp to 0.
            var creature = MakeCreature(new Zone("OverpowerUnit"), 5, 5);
            creature.AddPart(new GasMaskPart { Power = 50 });

            var e = GameEvent.New("GetRespiratoryPerformance");
            e.SetParameter("Intake", (object)100);
            creature.FireEvent(e);
            int intake = e.GetParameter<int>("Intake");
            e.Release();
            Assert.AreEqual(0, intake, "negative intake floored to 0");
        }

        [Test]
        public void GasMaskPart_BeforeTakeDamage_ScalesGasDamage()
        {
            // BeforeTakeDamage gate: Power=10 damage scaled by 90%.
            var creature = MakeCreature(new Zone("MaskDmgUnit"), 5, 5);
            creature.AddPart(new GasMaskPart { Power = 10 });

            var e = GameEvent.New("BeforeTakeDamage");
            var dmg = new Damage(100);
            dmg.AddAttribute("Poison");
            dmg.AddAttribute("Gas"); // mandatory for the gate to fire
            e.SetParameter("Damage", (object)dmg);
            creature.FireEventAndRelease(e);

            Assert.AreEqual(90, dmg.Amount, "Power 10 → 90% damage (10% mitigated)");
        }

        [Test]
        public void GasMaskPart_DoesNotReduceNonGasDamage_Counter()
        {
            // Counter: a Heat-only hit (no "Gas" attribute) is NOT
            // reduced. The mask is specifically for gas exposure.
            var creature = MakeCreature(new Zone("NonGasDmgUnit"), 5, 5);
            creature.AddPart(new GasMaskPart { Power = 30 });

            var e = GameEvent.New("BeforeTakeDamage");
            var dmg = new Damage(100);
            dmg.AddAttribute("Heat");
            e.SetParameter("Damage", (object)dmg);
            creature.FireEventAndRelease(e);

            Assert.AreEqual(100, dmg.Amount, "Heat-only damage unaffected by gas mask");
        }

        [Test]
        public void GasMaskPart_HighPower_GasDamage_DoesNotGoNegative()
        {
            // Power=120 would produce negative — defensive clamp.
            var creature = MakeCreature(new Zone("MaskOverpowerDmg"), 5, 5);
            creature.AddPart(new GasMaskPart { Power = 120 });

            var e = GameEvent.New("BeforeTakeDamage");
            var dmg = new Damage(100);
            dmg.AddAttribute("Gas");
            e.SetParameter("Damage", (object)dmg);
            creature.FireEventAndRelease(e);

            Assert.GreaterOrEqual(dmg.Amount, 0, "damage clamped non-negative");
        }

        [Test]
        public void GasMaskPart_EmitsMaskIntakeReducedDiag()
        {
            var creature = MakeCreature(new Zone("MaskDiag"), 5, 5);
            creature.AddPart(new GasMaskPart { Power = 10 });
            Diag.ResetAll();

            var e = GameEvent.New("GetRespiratoryPerformance");
            e.SetParameter("Intake", (object)100);
            creature.FireEventAndRelease(e);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "MaskIntakeReduced", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"power\":10", recs[0].PayloadJson);
            StringAssert.Contains("\"intakeBefore\":100", recs[0].PayloadJson);
            StringAssert.Contains("\"intakeAfter\":50", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — GasImmunityPart unit tests
        // ════════════════════════════════════════════════════════════

        [Test]
        public void GasImmunityPart_MatchingGasType_Vetoes()
        {
            var creature = MakeCreature(new Zone("ImmunUnit"), 5, 5);
            creature.AddPart(new GasImmunityPart { GasType = "Poison" });

            var e = GameEvent.New("CheckGasCanAffect");
            e.SetParameter("GasType", (object)"Poison");
            bool canAffect = creature.FireEventAndRelease(e);
            Assert.IsFalse(canAffect, "matching gas type → vetoed");
        }

        [Test]
        public void GasImmunityPart_DifferentGasType_DoesNotVeto()
        {
            // Counter: immunity for "Poison" doesn't block "Cryo".
            var creature = MakeCreature(new Zone("ImmunCounter"), 5, 5);
            creature.AddPart(new GasImmunityPart { GasType = "Poison" });

            var e = GameEvent.New("CheckGasCanAffect");
            e.SetParameter("GasType", (object)"Cryo");
            bool canAffect = creature.FireEventAndRelease(e);
            Assert.IsTrue(canAffect, "different gas type → not vetoed");
        }

        [Test]
        public void GasImmunityPart_EmptyGasType_DoesNotVetoAnything()
        {
            // Defensive: empty GasType field shouldn't be a blanket
            // veto (which would happen if we did `eventType == GasType`
            // without the empty-string guard).
            var creature = MakeCreature(new Zone("EmptyImmun"), 5, 5);
            creature.AddPart(new GasImmunityPart { GasType = "" });

            var e = GameEvent.New("CheckGasCanAffect");
            e.SetParameter("GasType", (object)"Poison");
            bool canAffect = creature.FireEventAndRelease(e);
            Assert.IsTrue(canAffect, "empty immune-type doesn't blanket-veto");
        }

        [Test]
        public void GasImmunityPart_MultipleInstances_ImmuneToMultipleTypes()
        {
            // A creature can carry N GasImmunityParts. Each handles the
            // event independently; the first to return false vetoes.
            var creature = MakeCreature(new Zone("MultiImmun"), 5, 5);
            creature.AddPart(new GasImmunityPart { GasType = "Poison" });
            creature.AddPart(new GasImmunityPart { GasType = "Cryo" });

            // Poison → vetoed
            var e1 = GameEvent.New("CheckGasCanAffect");
            e1.SetParameter("GasType", (object)"Poison");
            Assert.IsFalse(creature.FireEventAndRelease(e1));
            // Cryo → vetoed
            var e2 = GameEvent.New("CheckGasCanAffect");
            e2.SetParameter("GasType", (object)"Cryo");
            Assert.IsFalse(creature.FireEventAndRelease(e2));
            // Stun → NOT vetoed (no immunity Part for it)
            var e3 = GameEvent.New("CheckGasCanAffect");
            e3.SetParameter("GasType", (object)"Stun");
            Assert.IsTrue(creature.FireEventAndRelease(e3));
        }

        [Test]
        public void GasImmunityPart_EmitsImmunityVetoDiag()
        {
            var creature = MakeCreature(new Zone("ImmunDiag"), 5, 5);
            creature.AddPart(new GasImmunityPart { GasType = "Poison" });
            Diag.ResetAll();

            var e = GameEvent.New("CheckGasCanAffect");
            e.SetParameter("GasType", (object)"Poison");
            creature.FireEventAndRelease(e);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "ImmunityVeto", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"immuneTo\":\"Poison\"", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   PART III — Integration: full ApplyGas pipeline
        // ════════════════════════════════════════════════════════════

        [Test]
        public void GasMaskWearer_TakesGas_StillAffectedButReducedDamage()
        {
            // Integration: poison-vapor + Power=10 mask. Intake reduced
            // 100→50, immediate damage = (50+1)/20 = 2 (floored to at
            // least 1, here 2). BeforeTakeDamage then scales the Gas-
            // tagged 2-damage by 90% = 1 (integer math floor). Net: 1
            // damage gets through, effect still applies.
            var zone = new Zone("MaskIntegration");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100, level: 1);
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasMaskPart { Power = 10 });

            int hp0 = target.GetStatValue("Hitpoints");
            gas.GetPart<GasPoisonPart>().ApplyGas(target, zone);

            int hp1 = target.GetStatValue("Hitpoints");
            int damageDealt = hp0 - hp1;
            Assert.Less(damageDealt, 5,
                $"masked wearer takes ≤4 damage (got {damageDealt})");
            Assert.IsNotNull(target.GetEffect<PoisonedByGasEffect>(),
                "effect still applies (intake>0 didn't veto)");
        }

        [Test]
        public void GasMaskWearer_Power20_FullyImmuneViaZeroIntake()
        {
            // Hazmat mask: intake reduced to 0, fully vetoes.
            var zone = new Zone("HazmatIntegration");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100, level: 1);
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasMaskPart { Power = 20 });

            int hp0 = target.GetStatValue("Hitpoints");
            Diag.ResetAll();
            bool applied = gas.GetPart<GasPoisonPart>().ApplyGas(target, zone);

            Assert.IsFalse(applied);
            Assert.AreEqual(hp0, target.GetStatValue("Hitpoints"), "no damage");
            Assert.IsNull(target.GetEffect<PoisonedByGasEffect>(), "no effect");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "ApplyVetoed", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("ZeroIntake", recs[0].PayloadJson);
        }

        [Test]
        public void GasImmuneCreature_TakesNoGasEffect()
        {
            // Integration: GasImmunityPart for Poison blocks the entire
            // pipeline at the CheckGasCanAffect gate.
            var zone = new Zone("ImmunIntegration");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasImmunityPart { GasType = "Poison" });

            int hp0 = target.GetStatValue("Hitpoints");
            bool applied = gas.GetPart<GasPoisonPart>().ApplyGas(target, zone);

            Assert.IsFalse(applied);
            Assert.AreEqual(hp0, target.GetStatValue("Hitpoints"));
            Assert.IsNull(target.GetEffect<PoisonedByGasEffect>());
        }

        [Test]
        public void GasImmuneCreature_DifferentType_NotImmune_Counter()
        {
            // Counter: a creature immune to "Cryo" is still affected
            // by "Poison" gas. This pins the per-type contract.
            var zone = new Zone("ImmunCounterIntegration");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasImmunityPart { GasType = "Cryo" });

            int hp0 = target.GetStatValue("Hitpoints");
            bool applied = gas.GetPart<GasPoisonPart>().ApplyGas(target, zone);

            Assert.IsTrue(applied, "Cryo immunity doesn't block Poison");
            Assert.Less(target.GetStatValue("Hitpoints"), hp0);
            Assert.IsNotNull(target.GetEffect<PoisonedByGasEffect>());
        }

        [Test]
        public void GasMaskAndImmunity_BothPresent_ImmunityVetoesFirst()
        {
            // Order check: in the filter pipeline, CheckGasCanAffect
            // fires BEFORE GetRespiratoryPerformance. So a creature with
            // both an immunity AND a mask: the immunity veto fires
            // first, the mask path is never reached.
            var zone = new Zone("BothDefenses");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasImmunityPart { GasType = "Poison" });
            target.AddPart(new GasMaskPart { Power = 10 });

            Diag.ResetAll();
            gas.GetPart<GasPoisonPart>().ApplyGas(target, zone);

            // The mask-intake-reduced diag should NOT have fired —
            // CheckGasCanAffect bailed us before GetRespiratoryPerformance.
            var maskDiags = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "MaskIntakeReduced", Limit = 5 }).Records;
            Assert.AreEqual(0, maskDiags.Count,
                "immunity veto fires before mask intake — mask diag never emits");
        }

        [Test]
        public void GasMaskWearer_TickFromGasCloud_DamageReduced()
        {
            // Pin the BeforeTakeDamage gate end-to-end: a masked wearer
            // standing in poison gas takes less immediate damage than a
            // mask-less wearer.
            var zone = new Zone("MaskTickIntegration");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100, level: 5);
            var masked = MakeCreature(zone, 5, 5);
            masked.AddPart(new GasMaskPart { Power = 10 });
            var bare = MakeCreature(zone, 5, 5);

            int maskedHp0 = masked.GetStatValue("Hitpoints");
            int bareHp0 = bare.GetStatValue("Hitpoints");
            var poison = gas.GetPart<GasPoisonPart>();
            poison.ApplyGas(masked, zone);
            poison.ApplyGas(bare, zone);

            int maskedDmg = maskedHp0 - masked.GetStatValue("Hitpoints");
            int bareDmg = bareHp0 - bare.GetStatValue("Hitpoints");
            Assert.Less(maskedDmg, bareDmg,
                $"masked took less damage than bare (masked={maskedDmg}, bare={bareDmg})");
        }
    }
}
