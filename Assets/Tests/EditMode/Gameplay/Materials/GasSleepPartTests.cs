using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.8c — GasSleepPart + AsleepByGasEffect tests. The Effect has
    /// the wake-on-damage twist that distinguishes it from
    /// GasStunPart's incapacitation; pinned by dedicated tests for
    /// damage waking + zero-damage-doesn't-wake counter.
    /// </summary>
    public class GasSleepPartTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""sleep-vapor"", ""GasType"":""Sleep"",
                ""Glyph"":""°"", ""Color"":""&B"",
                ""DefaultDensity"":80, ""DefaultLevel"":1,
                ""BehaviorKind"":""Sleep"" } ] }");
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
        //   AsleepByGasEffect — direct effect tests
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Asleep_AllowAction_ReturnsFalse()
        {
            // The action-block is the entire point. A sleeping creature
            // cannot act.
            var target = new Entity { ID = "t", BlueprintName = "T" };
            var fx = new AsleepByGasEffect(duration: 5);
            target.ApplyEffect(fx);
            Assert.IsFalse(fx.AllowAction(target),
                "sleeping creature blocks action");
        }

        [Test]
        public void Asleep_DamageBreaksSleep_SetsDurationZero()
        {
            // Wake-on-damage: when the effect's OnTakeDamage fires
            // with a non-zero Damage amount, Duration drops to 0
            // (StatusEffectsPart's EndTurn cleanup removes it).
            var zone = new Zone("SleepWake");
            var target = MakeCreature(zone, 5, 5);
            var fx = new AsleepByGasEffect(duration: 10);
            target.ApplyEffect(fx);
            Assert.AreEqual(10, fx.Duration, "precondition");

            CombatSystem.ApplyDamage(target, new Damage(5), null, zone);

            Assert.AreEqual(0, fx.Duration,
                "any non-zero damage sets Duration to 0 (wakes the sleeper)");
        }

        [Test]
        public void Asleep_FullyResistedDamage_DoesNotWake_Counter()
        {
            // Counter: a hit fully nullified by resistance (Amount=0
            // after BeforeTakeDamage) does NOT wake the sleeper. The
            // wake gate fires only on damage that actually lands.
            var zone = new Zone("SleepNoWake");
            var target = MakeCreature(zone, 5, 5);
            var fx = new AsleepByGasEffect(duration: 10);
            target.ApplyEffect(fx);

            // Directly fire OnTakeDamage with a 0-amount Damage.
            var e = GameEvent.New("TakeDamage");
            var d = new Damage(0);
            e.SetParameter("Damage", (object)d);
            fx.OnTakeDamage(target, e);
            e.Release();

            Assert.AreEqual(10, fx.Duration,
                "0-damage hit does NOT wake — Duration unchanged");
        }

        [Test]
        public void Asleep_OnStack_MaxDurationWins()
        {
            // Refresh-on-reapply (or via OnStack): take the larger
            // Duration. Mirrors PoisonedByGasEffect.OnStack.
            var fx1 = new AsleepByGasEffect(duration: 3);
            var fx2 = new AsleepByGasEffect(duration: 7);
            bool stacked = fx1.OnStack(fx2);
            Assert.IsTrue(stacked);
            Assert.AreEqual(7, fx1.Duration);
        }

        [Test]
        public void Asleep_OnStack_SmallerIncoming_DoesNotDowngrade()
        {
            var fx1 = new AsleepByGasEffect(duration: 7);
            var fx2 = new AsleepByGasEffect(duration: 2);
            fx1.OnStack(fx2);
            Assert.AreEqual(7, fx1.Duration);
        }

        // ════════════════════════════════════════════════════════════
        //   GasSleepPart — factory + filter chain
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SpawnGas_BehaviorKindSleep_AttachesGasSleepPart()
        {
            var zone = new Zone("SleepFactory");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "sleep-vapor");
            Assert.IsNotNull(gas.GetPart<GasSleepPart>());
            Assert.IsNotNull(gas.GetPart<IObjectGasBehaviorPart>(),
                "accessible via abstract base — picked up by per-turn dispatch");
        }

        [Test]
        public void SleepGas_ApplyGas_OnCreature_AppliesAsleepEffect()
        {
            var zone = new Zone("SleepApply");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "sleep-vapor", density: 80, level: 1);
            var target = MakeCreature(zone, 5, 5);

            bool applied = gas.GetPart<GasSleepPart>().ApplyGas(target, zone);

            Assert.IsTrue(applied);
            var fx = target.GetEffect<AsleepByGasEffect>();
            Assert.IsNotNull(fx);
            Assert.AreEqual(GasSleepPart.DURATION_PER_LEVEL, fx.Duration,
                "Level 1 → DURATION_PER_LEVEL turns");
        }

        [Test]
        public void SleepGas_LevelTwo_DoublesDuration()
        {
            var zone = new Zone("SleepLevel");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "sleep-vapor", level: 2);
            var target = MakeCreature(zone, 5, 5);
            gas.GetPart<GasSleepPart>().ApplyGas(target, zone);
            Assert.AreEqual(GasSleepPart.DURATION_PER_LEVEL * 2,
                target.GetEffect<AsleepByGasEffect>().Duration);
        }

        [Test]
        public void SleepGas_DoesNotDealImmediateDamage_Counter()
        {
            var zone = new Zone("SleepNoDmg");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "sleep-vapor");
            var target = MakeCreature(zone, 5, 5);
            int hp0 = target.GetStatValue("Hitpoints");
            gas.GetPart<GasSleepPart>().ApplyGas(target, zone);
            Assert.AreEqual(hp0, target.GetStatValue("Hitpoints"));
        }

        [Test]
        public void SleepGas_RefreshOnReapply_DoesNotStackDuration()
        {
            var zone = new Zone("SleepRefresh");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "sleep-vapor", level: 1);
            var target = MakeCreature(zone, 5, 5);
            var sleep = gas.GetPart<GasSleepPart>();

            sleep.ApplyGas(target, zone);
            int durationAfterFirst = target.GetEffect<AsleepByGasEffect>().Duration;
            sleep.ApplyGas(target, zone);
            int durationAfterSecond = target.GetEffect<AsleepByGasEffect>().Duration;

            Assert.AreEqual(durationAfterFirst, durationAfterSecond);
        }

        [Test]
        public void SleepGas_OnNonCreature_Vetoed()
        {
            var zone = new Zone("SleepNonCreature");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "sleep-vapor");
            var item = new Entity { ID = "item", BlueprintName = "Item" };
            item.AddPart(new RenderPart { DisplayName = "item" });
            zone.AddEntity(item, 5, 5);
            bool applied = gas.GetPart<GasSleepPart>().ApplyGas(item, zone);
            Assert.IsFalse(applied);
        }

        [Test]
        public void SleepGas_GasImmunity_Vetoes()
        {
            var zone = new Zone("SleepImmunity");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "sleep-vapor");
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasImmunityPart { GasType = "Sleep" });
            bool applied = gas.GetPart<GasSleepPart>().ApplyGas(target, zone);
            Assert.IsFalse(applied);
            Assert.IsNull(target.GetEffect<AsleepByGasEffect>());
        }

        [Test]
        public void SleepGas_PerTurnDispatch_FromGasSystem()
        {
            var zone = new Zone("SleepTickDispatch");
            GasFactory.SpawnGas(zone, 5, 5, "sleep-vapor", density: 500);
            var target = MakeCreature(zone, 5, 5);
            GasSystem.OnTickEnd(zone);
            Assert.IsNotNull(target.GetEffect<AsleepByGasEffect>(),
                "per-turn dispatch put creature to sleep");
        }

        [Test]
        public void SleepGas_EmitsAppliedDiag()
        {
            var zone = new Zone("SleepDiag");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "sleep-vapor", level: 3);
            var target = MakeCreature(zone, 5, 5);
            Diag.ResetAll();
            gas.GetPart<GasSleepPart>().ApplyGas(target, zone);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Applied", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"gasId\":\"sleep-vapor\"", recs[0].PayloadJson);
            StringAssert.Contains("\"gasType\":\"Sleep\"", recs[0].PayloadJson);
            StringAssert.Contains("\"effectDuration\":9", recs[0].PayloadJson); // 3 × 3
        }

        // ════════════════════════════════════════════════════════════
        //   Cross-type isolation
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SleepGas_DoesNotApplyOtherEffects_Counter()
        {
            var zone = new Zone("SleepCounter");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "sleep-vapor");
            var target = MakeCreature(zone, 5, 5);
            gas.GetPart<GasSleepPart>().ApplyGas(target, zone);
            Assert.IsNull(target.GetEffect<StunnedEffect>());
            Assert.IsNull(target.GetEffect<ConfusedEffect>());
            Assert.IsNull(target.GetEffect<PoisonedByGasEffect>());
            Assert.IsNull(target.GetEffect<FrozenEffect>());
        }

        // ════════════════════════════════════════════════════════════
        //   Integration: sleeper takes damage → wakes mid-effect
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Sleep_ThenHit_WakesAndEffectExpires()
        {
            // Integration: a creature is put to sleep, then attacked.
            // The next StatusEffectsPart.EndTurn pass should remove the
            // expired (Duration=0) effect.
            var zone = new Zone("SleepHitWake");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "sleep-vapor", level: 2);
            var target = MakeCreature(zone, 5, 5);
            gas.GetPart<GasSleepPart>().ApplyGas(target, zone);
            Assert.IsNotNull(target.GetEffect<AsleepByGasEffect>(), "precondition");

            CombatSystem.ApplyDamage(target, new Damage(10), null, zone);

            var fx = target.GetEffect<AsleepByGasEffect>();
            // The effect may still be present in the list (Duration=0
            // but not yet swept by EndTurn) — what matters is Duration
            // dropped to 0.
            Assert.AreEqual(0, fx?.Duration ?? 0,
                "damage drove Duration to 0 (will be swept on next EndTurn)");
        }
    }
}
