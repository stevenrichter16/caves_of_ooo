using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.8a — GasStunPart + GasConfusionPart behavior tests. Both
    /// reuse the existing G.5 filter chain + existing StunnedEffect /
    /// ConfusedEffect, so the test surface is narrower than G.5's:
    /// confirm the effect lands, refresh-on-reapply works, no
    /// immediate damage (stun/confusion are non-damage), and the
    /// factory wires the right Part for each BehaviorKind.
    /// </summary>
    public class GasStunConfusionTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""stun-vapor"", ""GasType"":""Stun"",
                ""Glyph"":""°"", ""Color"":""&Y"",
                ""DefaultDensity"":80, ""DefaultLevel"":1,
                ""BehaviorKind"":""Stun"" },
              { ""Id"":""confusion-vapor"", ""GasType"":""Confusion"",
                ""Glyph"":""°"", ""Color"":""&M"",
                ""DefaultDensity"":80, ""DefaultLevel"":1,
                ""BehaviorKind"":""Confusion"" } ] }");
        }

        [TearDown]
        public void TearDown() => GasRegistry.ResetForTests();

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
        //   PART I — Factory wiring (BehaviorKind → concrete Part)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SpawnGas_BehaviorKindStun_AttachesGasStunPart()
        {
            var zone = new Zone("StunFactory");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "stun-vapor");
            Assert.IsNotNull(gas.GetPart<GasStunPart>(),
                "BehaviorKind=Stun attached GasStunPart");
            Assert.IsNotNull(gas.GetPart<IObjectGasBehaviorPart>(),
                "accessible via abstract base");
        }

        [Test]
        public void SpawnGas_BehaviorKindConfusion_AttachesGasConfusionPart()
        {
            var zone = new Zone("ConfusionFactory");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "confusion-vapor");
            Assert.IsNotNull(gas.GetPart<GasConfusionPart>());
            Assert.IsNotNull(gas.GetPart<IObjectGasBehaviorPart>());
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — Stun behavior
        // ════════════════════════════════════════════════════════════

        [Test]
        public void StunGas_ApplyGas_OnCreature_AppliesStunnedEffect()
        {
            var zone = new Zone("StunApply");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "stun-vapor", density: 80, level: 2);
            var target = MakeCreature(zone, 5, 5);

            bool applied = gas.GetPart<GasStunPart>().ApplyGas(target, zone);

            Assert.IsTrue(applied);
            var stun = target.GetEffect<StunnedEffect>();
            Assert.IsNotNull(stun, "StunnedEffect applied");
            Assert.AreEqual(GasStunPart.DURATION_PER_LEVEL * 2, stun.Duration,
                "duration = DURATION_PER_LEVEL × GasLevel");
        }

        [Test]
        public void StunGas_DoesNotDealImmediateDamage_Counter()
        {
            // Stun is incapacitation, not exposure damage — pin no HP loss.
            var zone = new Zone("StunNoDmg");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "stun-vapor", density: 80, level: 1);
            var target = MakeCreature(zone, 5, 5);
            int hp0 = target.GetStatValue("Hitpoints");

            gas.GetPart<GasStunPart>().ApplyGas(target, zone);

            Assert.AreEqual(hp0, target.GetStatValue("Hitpoints"),
                "stun gas does NOT deal immediate damage (unlike poison)");
        }

        [Test]
        public void StunGas_RefreshOnReapply_DoesNotStackDuration()
        {
            // Apply twice — Duration matches Level × DURATION_PER_LEVEL,
            // NOT 2× (refresh, not accumulate). Mirrors GasPoisonPart's
            // refresh-via-RemoveEffect convention.
            var zone = new Zone("StunRefresh");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "stun-vapor", density: 80, level: 1);
            var target = MakeCreature(zone, 5, 5);
            var poison = gas.GetPart<GasStunPart>();

            poison.ApplyGas(target, zone);
            int durationAfterFirst = target.GetEffect<StunnedEffect>().Duration;
            poison.ApplyGas(target, zone);
            int durationAfterSecond = target.GetEffect<StunnedEffect>().Duration;

            Assert.AreEqual(durationAfterFirst, durationAfterSecond,
                "re-applying stun gas refreshes Duration (does not accumulate)");
        }

        [Test]
        public void StunGas_OnNonCreature_Vetoed()
        {
            // Filter chain: non-Creature targets vetoed with NotACreature.
            // The shared RunFilterChain helper handles this.
            var zone = new Zone("StunNonCreature");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "stun-vapor");
            var item = new Entity { ID = "item", BlueprintName = "Item" };
            item.AddPart(new RenderPart { DisplayName = "item" });
            item.AddPart(new PhysicsPart { Solid = false });
            zone.AddEntity(item, 5, 5);

            bool applied = gas.GetPart<GasStunPart>().ApplyGas(item, zone);
            Assert.IsFalse(applied);
            Assert.IsNull(item.GetEffect<StunnedEffect>());
        }

        [Test]
        public void StunGas_GasImmunity_Vetoes()
        {
            // G.6 integration: GasImmunityPart for "Stun" vetoes via
            // CheckGasCanAffect. Confirms the new Part flows through
            // the same gate as GasPoisonPart.
            var zone = new Zone("StunImmunity");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "stun-vapor");
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasImmunityPart { GasType = "Stun" });

            bool applied = gas.GetPart<GasStunPart>().ApplyGas(target, zone);
            Assert.IsFalse(applied);
            Assert.IsNull(target.GetEffect<StunnedEffect>());
        }

        [Test]
        public void StunGas_EmitsAppliedDiag()
        {
            var zone = new Zone("StunDiag");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "stun-vapor", level: 3);
            var target = MakeCreature(zone, 5, 5);
            Diag.ResetAll();
            gas.GetPart<GasStunPart>().ApplyGas(target, zone);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Applied", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"gasId\":\"stun-vapor\"", recs[0].PayloadJson);
            StringAssert.Contains("\"gasType\":\"Stun\"", recs[0].PayloadJson);
            StringAssert.Contains("\"gasLevel\":3", recs[0].PayloadJson);
            StringAssert.Contains("\"effectDuration\":6", recs[0].PayloadJson); // 3 × 2
        }

        // ════════════════════════════════════════════════════════════
        //   PART III — Confusion behavior
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ConfusionGas_ApplyGas_OnCreature_AppliesConfusedEffect()
        {
            var zone = new Zone("ConfApply");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "confusion-vapor", density: 80, level: 1);
            var target = MakeCreature(zone, 5, 5);

            bool applied = gas.GetPart<GasConfusionPart>().ApplyGas(target, zone);

            Assert.IsTrue(applied);
            var confused = target.GetEffect<ConfusedEffect>();
            Assert.IsNotNull(confused, "ConfusedEffect applied");
            Assert.AreEqual(GasConfusionPart.DURATION_PER_LEVEL, confused.Duration,
                "Level 1 confusion duration = DURATION_PER_LEVEL (4)");
        }

        [Test]
        public void ConfusionGas_LevelTwo_DoublesDuration()
        {
            var zone = new Zone("ConfLevel");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "confusion-vapor", level: 2);
            var target = MakeCreature(zone, 5, 5);
            gas.GetPart<GasConfusionPart>().ApplyGas(target, zone);
            Assert.AreEqual(GasConfusionPart.DURATION_PER_LEVEL * 2,
                target.GetEffect<ConfusedEffect>().Duration);
        }

        [Test]
        public void ConfusionGas_DoesNotDealImmediateDamage()
        {
            var zone = new Zone("ConfNoDmg");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "confusion-vapor");
            var target = MakeCreature(zone, 5, 5);
            int hp0 = target.GetStatValue("Hitpoints");
            gas.GetPart<GasConfusionPart>().ApplyGas(target, zone);
            Assert.AreEqual(hp0, target.GetStatValue("Hitpoints"));
        }

        [Test]
        public void ConfusionGas_GasMask_FullyZeroIntake_Vetoes()
        {
            // G.6 integration: a full GasMask (Power=20 → intake -100)
            // vetoes via the ZeroIntake gate.
            var zone = new Zone("ConfMask");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "confusion-vapor");
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasMaskPart { Power = 20 });

            bool applied = gas.GetPart<GasConfusionPart>().ApplyGas(target, zone);
            Assert.IsFalse(applied);
            Assert.IsNull(target.GetEffect<ConfusedEffect>());
        }

        [Test]
        public void ConfusionGas_PerTurnDispatch_FromGasSystem()
        {
            // Integration: GasSystem.OnTickEnd dispatches per-turn apply
            // to all gas pools, so a creature standing in confusion gas
            // gets confused after one OnTickEnd call.
            var zone = new Zone("ConfTickDispatch");
            GasFactory.SpawnGas(zone, 5, 5, "confusion-vapor", density: 500);
            var target = MakeCreature(zone, 5, 5);

            GasSystem.OnTickEnd(zone);

            Assert.IsNotNull(target.GetEffect<ConfusedEffect>(),
                "per-turn dispatch confused the in-cell creature");
        }

        // ════════════════════════════════════════════════════════════
        //   PART IV — Cross-type isolation (counter to filter sharing)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void StunGas_DoesNotApplyConfusion_Counter()
        {
            // Counter: stun gas doesn't accidentally apply ConfusedEffect.
            var zone = new Zone("StunNotConf");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "stun-vapor");
            var target = MakeCreature(zone, 5, 5);
            gas.GetPart<GasStunPart>().ApplyGas(target, zone);
            Assert.IsNull(target.GetEffect<ConfusedEffect>(),
                "stun gas does not apply ConfusedEffect");
        }

        [Test]
        public void ConfusionGas_DoesNotApplyStun_Counter()
        {
            var zone = new Zone("ConfNotStun");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "confusion-vapor");
            var target = MakeCreature(zone, 5, 5);
            gas.GetPart<GasConfusionPart>().ApplyGas(target, zone);
            Assert.IsNull(target.GetEffect<StunnedEffect>(),
                "confusion gas does not apply StunnedEffect");
        }

        [Test]
        public void StunImmunity_DoesNotBlockConfusion_Counter()
        {
            // Per-type immunity: stun-immune creature still vulnerable
            // to confusion gas.
            var zone = new Zone("CrossImmunity");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "confusion-vapor");
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasImmunityPart { GasType = "Stun" });

            bool applied = gas.GetPart<GasConfusionPart>().ApplyGas(target, zone);
            Assert.IsTrue(applied, "Stun immunity doesn't block Confusion");
            Assert.IsNotNull(target.GetEffect<ConfusedEffect>());
        }
    }
}
