using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.5 — GasPoisonPart + PoisonedByGasEffect + filter pipeline
    /// tests. Two halves:
    /// (1) The filter chain — each gate passes/fails as designed
    ///     (Creature tag, CheckGasCanAffect, GetRespiratoryPerformance)
    /// (2) Effect lifecycle — PoisonedByGasEffect applied, ticks
    ///     damage outside the cloud, suppresses tick while inside,
    ///     duration decrements.
    /// </summary>
    public class GasPoisonPartTests
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
                ""BehaviorKind"":""Poison"" } ] }");
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
            S("AcidResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════
        //   PART I — Factory wires the behavior Part
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SpawnGas_WithBehaviorKindPoison_AttachesGasPoisonPart()
        {
            var zone = new Zone("PoisonGasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            Assert.IsNotNull(gas.GetPart<GasPoisonPart>(),
                "BehaviorKind=Poison attached GasPoisonPart");
            Assert.IsNotNull(gas.GetPart<IObjectGasBehaviorPart>(),
                "GasPoisonPart is accessible via the abstract base");
        }

        [Test]
        public void SpawnGas_WithEmptyBehaviorKind_NoBehaviorPart()
        {
            GasRegistry.ResetForTests();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""inert-mist"", ""GasType"":""Inert"",
                ""Glyph"":""°"", ""Color"":""&w"", ""DefaultDensity"":50,
                ""BehaviorKind"":"""" } ] }");
            var zone = new Zone("InertGasFactoryTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "inert-mist");
            Assert.IsNotNull(gas, "spawn succeeded");
            Assert.IsNull(gas.GetPart<IGasBehaviorPart>(),
                "empty BehaviorKind = visual-only (no behavior Part)");
        }

        [Test]
        public void SpawnGas_WithUnknownBehaviorKind_NoBehaviorPart_NoCrash()
        {
            GasRegistry.ResetForTests();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""mystery-gas"", ""GasType"":""Mystery"",
                ""Glyph"":""°"", ""Color"":""&m"", ""DefaultDensity"":50,
                ""BehaviorKind"":""Fhqwhgads"" } ] }");
            var zone = new Zone("UnknownBehaviorTest");
            Entity gas = null;
            Assert.DoesNotThrow(() => { gas = GasFactory.SpawnGas(zone, 5, 5, "mystery-gas"); });
            Assert.IsNotNull(gas, "spawn still succeeded (graceful unknown-kind path)");
            Assert.IsNull(gas.GetPart<IGasBehaviorPart>(),
                "unknown BehaviorKind = no behavior Part attached");
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — Filter chain
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ApplyGas_OnCreature_DealsImmediateDamage_AppliesEffect()
        {
            var zone = new Zone("PoisonApply");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100, level: 2);
            var target = MakeCreature(zone, 5, 5);
            int hp0 = target.GetStatValue("Hitpoints");

            var poison = gas.GetPart<GasPoisonPart>();
            bool applied = poison.ApplyGas(target, zone);

            Assert.IsTrue(applied, "ApplyGas returned true (creature was affected)");
            Assert.Less(target.GetStatValue("Hitpoints"), hp0,
                "immediate exposure damage landed");
            Assert.IsNotNull(target.GetEffect<PoisonedByGasEffect>(),
                "PoisonedByGasEffect applied");
        }

        [Test]
        public void ApplyGas_OnNonCreature_VetoesWithNotACreature()
        {
            var zone = new Zone("PoisonApplyNonCreature");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            var item = new Entity { ID = "item", BlueprintName = "Item" };
            item.AddPart(new RenderPart { DisplayName = "item" });
            item.AddPart(new PhysicsPart { Solid = false });
            // Intentionally NO "Creature" tag.
            zone.AddEntity(item, 5, 5);

            var poison = gas.GetPart<GasPoisonPart>();
            Diag.ResetAll();
            bool applied = poison.ApplyGas(item, zone);

            Assert.IsFalse(applied);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "ApplyVetoed", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("NotACreature", recs[0].PayloadJson);
        }

        [Test]
        public void ApplyGas_SelfTarget_NoOp()
        {
            // Self-guard: gas can't gas itself.
            var zone = new Zone("PoisonApplySelf");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            var poison = gas.GetPart<GasPoisonPart>();
            bool applied = poison.ApplyGas(gas, zone);
            Assert.IsFalse(applied);
        }

        [Test]
        public void ApplyGas_NullTarget_NoCrash()
        {
            var zone = new Zone("PoisonApplyNull");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            var poison = gas.GetPart<GasPoisonPart>();
            Assert.DoesNotThrow(() => poison.ApplyGas(null, zone));
        }

        [Test]
        public void ApplyGas_TargetWithGasImmunity_Vetoes()
        {
            // CheckGasCanAffect event-veto pin. Stub Part returns false
            // from HandleEvent when the GasType matches — same shape
            // G.6's GasImmunityPart will use.
            var zone = new Zone("PoisonImmunity");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new TestGasImmunity("Poison"));

            var poison = gas.GetPart<GasPoisonPart>();
            int hp0 = target.GetStatValue("Hitpoints");
            Diag.ResetAll();
            bool applied = poison.ApplyGas(target, zone);

            Assert.IsFalse(applied);
            Assert.AreEqual(hp0, target.GetStatValue("Hitpoints"),
                "immune target takes no damage");
            Assert.IsNull(target.GetEffect<PoisonedByGasEffect>(),
                "no PoisonedByGasEffect applied");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "ApplyVetoed", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("GasImmunity", recs[0].PayloadJson);
        }

        [Test]
        public void ApplyGas_GasImmunityForDifferentType_DoesNotVeto()
        {
            // Counter: an immunity for "Cryo" doesn't veto a "Poison"
            // gas — the gate is per-type, not blanket.
            var zone = new Zone("PoisonImmunityCounter");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new TestGasImmunity("Cryo"));

            var poison = gas.GetPart<GasPoisonPart>();
            bool applied = poison.ApplyGas(target, zone);
            Assert.IsTrue(applied, "Cryo immunity doesn't block Poison");
        }

        [Test]
        public void ApplyGas_TargetWithMask_StillAffected_ButLessImmediateDamage()
        {
            // GetRespiratoryPerformance event-mutate pin. Stub Part
            // reduces "Intake" param by -90 — leaves 10 of 100 baseline.
            // Result: target IS affected (intake > 0) but immediate
            // damage is small.
            var zone = new Zone("PoisonMask");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", level: 1);
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new TestRespiratoryReducer(-90));

            var poison = gas.GetPart<GasPoisonPart>();
            int hp0 = target.GetStatValue("Hitpoints");
            bool applied = poison.ApplyGas(target, zone);

            Assert.IsTrue(applied, "masked but not immune — effect still applied");
            int damageTaken = hp0 - target.GetStatValue("Hitpoints");
            // intake=10 → immediate = (10+1)/20 = 0 → floored to 1
            Assert.AreEqual(1, damageTaken,
                "low intake → immediate damage capped at the 1-floor");
        }

        [Test]
        public void ApplyGas_TargetWithFullMask_ZeroIntake_Vetoes()
        {
            // Adversarial: a full mask (Intake = 0) vetoes entirely.
            var zone = new Zone("PoisonFullMask");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new TestRespiratoryReducer(-100));

            var poison = gas.GetPart<GasPoisonPart>();
            int hp0 = target.GetStatValue("Hitpoints");
            Diag.ResetAll();
            bool applied = poison.ApplyGas(target, zone);

            Assert.IsFalse(applied);
            Assert.AreEqual(hp0, target.GetStatValue("Hitpoints"), "no damage");
            Assert.IsNull(target.GetEffect<PoisonedByGasEffect>(), "no effect");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "ApplyVetoed", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("ZeroIntake", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   PART III — Per-turn dispatch via GasSystem.OnTickEnd
        // ════════════════════════════════════════════════════════════

        [Test]
        public void GasSystem_OnTickEnd_AppliesPoisonToCreaturesInCell()
        {
            var zone = new Zone("PoisonTickDispatch");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 500, level: 1);
            var target = MakeCreature(zone, 5, 5);
            int hp0 = target.GetStatValue("Hitpoints");

            GasSystem.OnTickEnd(zone);

            Assert.IsNotNull(target.GetEffect<PoisonedByGasEffect>(),
                "per-turn dispatch applied the effect to the in-cell creature");
            Assert.Less(target.GetStatValue("Hitpoints"), hp0,
                "immediate damage from the per-turn dose");
        }

        [Test]
        public void GasSystem_OnTickEnd_DoesNotApplyToCreaturesInOtherCells()
        {
            var zone = new Zone("PoisonTickIsolation");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 200);
            var farTarget = MakeCreature(zone, 20, 5);

            GasSystem.OnTickEnd(zone);

            Assert.IsNull(farTarget.GetEffect<PoisonedByGasEffect>(),
                "creature far from the gas was not affected");
        }

        // ════════════════════════════════════════════════════════════
        //   PART IV — PoisonedByGasEffect tick semantics
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Effect_TickInGasCell_SuppressesDamage()
        {
            // While the target is IN a matching gas cloud, the effect's
            // per-turn tick is SUPPRESSED (the gas itself doses them).
            // Mirrors Qud PoisonGasPoison.cs:73-84.
            var zone = new Zone("EffectTickSuppress");
            SettlementRuntime.ActiveZone = zone;
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 200, level: 1);
            var target = MakeCreature(zone, 5, 5);
            var fx = new PoisonedByGasEffect { Duration = 5, DamagePerTurn = 10, GasTypeKey = "Poison" };
            target.ApplyEffect(fx);

            int hp0 = target.GetStatValue("Hitpoints");
            var ctx = GameEvent.New("BeginTakeAction");
            ctx.SetParameter("Zone", (object)zone);
            fx.OnTurnStart(target, ctx);

            Assert.AreEqual(hp0, target.GetStatValue("Hitpoints"),
                "while in poison cloud, the effect's own tick is suppressed");
        }

        [Test]
        public void Effect_TickOutsideGasCell_DealsDamage()
        {
            // Counter: when the target has LEFT the cloud, the effect
            // ticks damage.
            var zone = new Zone("EffectTickOutside");
            SettlementRuntime.ActiveZone = zone;
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 200);
            var target = MakeCreature(zone, 10, 10); // far from the gas
            var fx = new PoisonedByGasEffect { Duration = 5, DamagePerTurn = 10, GasTypeKey = "Poison" };
            target.ApplyEffect(fx);

            int hp0 = target.GetStatValue("Hitpoints");
            var ctx = GameEvent.New("BeginTakeAction");
            ctx.SetParameter("Zone", (object)zone);
            fx.OnTurnStart(target, ctx);

            Assert.AreEqual(hp0 - 10, target.GetStatValue("Hitpoints"),
                "outside the cloud, the effect deals the 10-damage tick");
        }

        [Test]
        public void Effect_TickInDifferentGasType_DoesNotSuppress()
        {
            // Counter / boundary: standing in a CRYO cell while
            // GAS-POISONED should still let the poison tick fire (the
            // suppression check matches by GasType, not just any gas).
            GasRegistry.ResetForTests();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""cryo-mist"", ""GasType"":""Cryo"",
                ""Glyph"":""°"", ""Color"":""&C"", ""DefaultDensity"":100,
                ""BehaviorKind"":"""" } ] }");
            var zone = new Zone("EffectTickDifferentType");
            SettlementRuntime.ActiveZone = zone;
            GasFactory.SpawnGas(zone, 5, 5, "cryo-mist", density: 100);
            var target = MakeCreature(zone, 5, 5);
            var fx = new PoisonedByGasEffect { Duration = 5, DamagePerTurn = 10, GasTypeKey = "Poison" };
            target.ApplyEffect(fx);

            int hp0 = target.GetStatValue("Hitpoints");
            var ctx = GameEvent.New("BeginTakeAction");
            ctx.SetParameter("Zone", (object)zone);
            fx.OnTurnStart(target, ctx);

            Assert.AreEqual(hp0 - 10, target.GetStatValue("Hitpoints"),
                "cryo gas in cell doesn't suppress the Poison effect tick");
        }

        [Test]
        public void Effect_OnStack_RefreshesDurationAndDamage()
        {
            // OnStack picks the larger Duration + larger DamagePerTurn.
            // Doesn't accumulate (mirrors LiquidCoveredEffect.OnStack
            // convention).
            var fx1 = new PoisonedByGasEffect { Duration = 3, DamagePerTurn = 2, GasTypeKey = "Poison" };
            var fx2 = new PoisonedByGasEffect { Duration = 7, DamagePerTurn = 5, GasTypeKey = "Poison" };
            bool stacked = fx1.OnStack(fx2);
            Assert.IsTrue(stacked);
            Assert.AreEqual(7, fx1.Duration, "larger Duration wins");
            Assert.AreEqual(5, fx1.DamagePerTurn, "larger DamagePerTurn wins");
        }

        [Test]
        public void Effect_OnStack_DoesNotDowngrade()
        {
            // Counter: smaller incoming doesn't downgrade existing.
            var fx1 = new PoisonedByGasEffect { Duration = 7, DamagePerTurn = 5 };
            var fx2 = new PoisonedByGasEffect { Duration = 2, DamagePerTurn = 1 };
            fx1.OnStack(fx2);
            Assert.AreEqual(7, fx1.Duration);
            Assert.AreEqual(5, fx1.DamagePerTurn);
        }

        // ════════════════════════════════════════════════════════════
        //   PART V — Diag observability
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ApplyGas_EmitsAppliedDiag()
        {
            var zone = new Zone("DiagApplied");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100, level: 3);
            var target = MakeCreature(zone, 5, 5);
            Diag.ResetAll();
            gas.GetPart<GasPoisonPart>().ApplyGas(target, zone);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Applied", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"gasId\":\"poison-vapor\"", recs[0].PayloadJson);
            StringAssert.Contains("\"gasType\":\"Poison\"", recs[0].PayloadJson);
            StringAssert.Contains("\"gasLevel\":3", recs[0].PayloadJson);
            StringAssert.Contains("\"effectDamagePerTurn\":6", recs[0].PayloadJson); // 3 * 2
        }

        [Test]
        public void EntityEnteredCell_TriggersApplyGas()
        {
            // The on-entry dispatch path: when an entity enters the gas's
            // cell, EntityEnteredCell fires on the gas, which routes to
            // ApplyGas. Pinned without needing to plumb MovementSystem —
            // we fire the event manually.
            var zone = new Zone("OnEntryDispatch");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            var target = MakeCreature(zone, 10, 10); // start far away
            // Manually fire the entry event ON THE GAS, mirroring
            // MovementSystem.FireCellEnteredEvents
            var e = GameEvent.New("EntityEnteredCell");
            e.SetParameter("Actor", (object)target);
            e.SetParameter("Zone", (object)zone);
            gas.FireEventAndRelease(e);

            Assert.IsNotNull(target.GetEffect<PoisonedByGasEffect>(),
                "on-entry dispatch applied the effect");
        }
    }

    // ──────────── Test-support Parts ────────────

    /// <summary>Listens to CheckGasCanAffect; vetoes when GasType matches.
    /// Stub for G.6's GasImmunityPart so G.5 can test the gate today.</summary>
    internal class TestGasImmunity : Part
    {
        public override string Name => "TestGasImmunity";
        private readonly string _immuneType;
        public TestGasImmunity(string immuneType) { _immuneType = immuneType; }
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "CheckGasCanAffect") return true;
            string gasType = e.GetParameter<string>("GasType");
            if (gasType == _immuneType) return false; // veto
            return true;
        }
    }

    /// <summary>Listens to GetRespiratoryPerformance; adjusts "Intake".
    /// Negative `delta` = mask (reduce intake). Stub for G.6's GasMaskPart.</summary>
    internal class TestRespiratoryReducer : Part
    {
        public override string Name => "TestRespiratoryReducer";
        private readonly int _delta;
        public TestRespiratoryReducer(int delta) { _delta = delta; }
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "GetRespiratoryPerformance") return true;
            int intake = e.GetParameter<int>("Intake");
            int adjusted = intake + _delta;
            if (adjusted < 0) adjusted = 0;
            e.SetParameter("Intake", (object)adjusted);
            return true;
        }
    }
}
