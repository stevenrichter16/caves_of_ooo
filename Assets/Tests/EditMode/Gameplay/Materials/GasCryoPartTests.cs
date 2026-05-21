using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.8b — GasCryoPart behavior tests. Cryo is architecturally
    /// different from the other G.5/G.8 behavior Parts because it
    /// bypasses the Creature gate (any Hitpoints-bearing entity takes
    /// cryo damage) and the respiratory gate (cryo damages via
    /// temperature, not inhalation). Filter chain is just:
    /// self-guard → Hitpoints check → CheckGasCanAffect.
    /// </summary>
    public class GasCryoPartTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""cryo-mist"", ""GasType"":""Cryo"",
                ""Glyph"":""°"", ""Color"":""&C"",
                ""DefaultDensity"":100, ""DefaultLevel"":1,
                ""BehaviorKind"":""Cryo"" } ] }");
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
            S("ColdResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        private static Entity MakeHitpointsBearingItem(Zone zone, int x, int y, int hpMax = 50)
        {
            // No "Creature" tag — just a damageable object (a wooden
            // crate that can be frozen + shattered).
            var e = new Entity { ID = "obj_" + x + "_" + y, BlueprintName = "TestObject" };
            e.Statistics["Hitpoints"] = new Stat
            { Owner = e, Name = "Hitpoints", BaseValue = hpMax, Max = hpMax, Min = -200 };
            e.AddPart(new RenderPart { DisplayName = "crate" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════
        //   Factory wiring
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SpawnGas_BehaviorKindCryo_AttachesGasCryoPart()
        {
            var zone = new Zone("CryoFactory");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "cryo-mist");
            Assert.IsNotNull(gas.GetPart<GasCryoPart>());
            Assert.IsNotNull(gas.GetPart<IObjectGasBehaviorPart>(),
                "still accessible via the dispatch base — G.5 per-turn loop picks it up");
        }

        // ════════════════════════════════════════════════════════════
        //   Cryo applies cold damage
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Cryo_ApplyGas_DealsColdDamage()
        {
            var zone = new Zone("CryoDmg");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "cryo-mist", density: 100, level: 1);
            var target = MakeCreature(zone, 5, 5);
            int hp0 = target.GetStatValue("Hitpoints");

            bool applied = gas.GetPart<GasCryoPart>().ApplyGas(target, zone);

            Assert.IsTrue(applied);
            int dmg = hp0 - target.GetStatValue("Hitpoints");
            // density 100 / 5 = 20 damage
            Assert.AreEqual(20, dmg, "density 100 → 20 cold damage");
        }

        [Test]
        public void Cryo_ApplyGas_AppliesFrozenEffect()
        {
            var zone = new Zone("CryoFreeze");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "cryo-mist", density: 100, level: 1);
            var target = MakeCreature(zone, 5, 5);
            gas.GetPart<GasCryoPart>().ApplyGas(target, zone);
            var frozen = target.GetEffect<FrozenEffect>();
            Assert.IsNotNull(frozen);
            Assert.AreEqual(GasCryoPart.COLD_PER_LEVEL, frozen.Cold,
                "Level 1 → COLD_PER_LEVEL Cold intensity");
        }

        [Test]
        public void Cryo_HighLevel_ClampsColdAtOne()
        {
            // Level 5 → 5 * 0.30 = 1.5 → clamped to 1.0 by FrozenEffect
            // ctor. Pin the clamp chain works.
            var zone = new Zone("CryoClamp");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "cryo-mist", level: 5);
            var target = MakeCreature(zone, 5, 5);
            gas.GetPart<GasCryoPart>().ApplyGas(target, zone);
            Assert.AreEqual(1.0f, target.GetEffect<FrozenEffect>().Cold,
                "Cold intensity clamped to 1.0");
        }

        // ════════════════════════════════════════════════════════════
        //   Architectural divergence: not gated on Creature
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Cryo_AffectsNonCreatureWithHitpoints()
        {
            // Distinguishes GasCryoPart from GasPoison/Stun/Confusion.
            // A non-Creature object (e.g. a crate with Hitpoints) takes
            // cold damage — Qud parity: cryo affects all matter.
            var zone = new Zone("CryoCrate");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "cryo-mist", density: 100);
            var crate = MakeHitpointsBearingItem(zone, 5, 5);
            int hp0 = crate.GetStatValue("Hitpoints");
            bool applied = gas.GetPart<GasCryoPart>().ApplyGas(crate, zone);
            Assert.IsTrue(applied, "cryo affects non-Creature damageables");
            Assert.Less(crate.GetStatValue("Hitpoints"), hp0);
        }

        [Test]
        public void Cryo_NoHitpoints_Skipped()
        {
            // Counter: an entity with no Hitpoints stat (a pure pool,
            // a renderless object) is skipped — there's nothing to
            // damage. Cleaner than crashing on TakeDamage.
            var zone = new Zone("CryoNoHp");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "cryo-mist");
            var ghostly = new Entity { ID = "ghost", BlueprintName = "Ghostly" };
            ghostly.AddPart(new RenderPart { DisplayName = "g" });
            zone.AddEntity(ghostly, 5, 5);
            // No Hitpoints stat.
            bool applied = gas.GetPart<GasCryoPart>().ApplyGas(ghostly, zone);
            Assert.IsFalse(applied);
        }

        // ════════════════════════════════════════════════════════════
        //   Cryo immunity (G.6 integration)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Cryo_GasImmunity_Vetoes()
        {
            var zone = new Zone("CryoImmunity");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "cryo-mist");
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasImmunityPart { GasType = "Cryo" });
            int hp0 = target.GetStatValue("Hitpoints");
            bool applied = gas.GetPart<GasCryoPart>().ApplyGas(target, zone);
            Assert.IsFalse(applied);
            Assert.AreEqual(hp0, target.GetStatValue("Hitpoints"));
            Assert.IsNull(target.GetEffect<FrozenEffect>());
        }

        [Test]
        public void Cryo_GasMask_ReducesDamage_ViaGasAttribute()
        {
            // The Cold damage carries "Gas" attribute → GasMask's
            // BeforeTakeDamage scales it. Test that the mask reduces
            // (but doesn't eliminate) cryo damage. Note: GasMask's
            // intake-reduction (Power*5) doesn't apply to cryo because
            // cryo doesn't fire GetRespiratoryPerformance — the damage-
            // scaling gate is the only path.
            var zone = new Zone("CryoMask");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "cryo-mist", density: 100);
            var bare = MakeCreature(zone, 5, 5);
            var masked = MakeCreature(zone, 5, 5);
            masked.AddPart(new GasMaskPart { Power = 30 });

            int bareHp0 = bare.GetStatValue("Hitpoints");
            int maskedHp0 = masked.GetStatValue("Hitpoints");
            var cryo = gas.GetPart<GasCryoPart>();
            cryo.ApplyGas(bare, zone);
            cryo.ApplyGas(masked, zone);

            int bareDmg = bareHp0 - bare.GetStatValue("Hitpoints");
            int maskedDmg = maskedHp0 - masked.GetStatValue("Hitpoints");
            Assert.Less(maskedDmg, bareDmg,
                $"mask reduces gas-damage via BeforeTakeDamage gate (bare={bareDmg}, masked={maskedDmg})");
        }

        // ════════════════════════════════════════════════════════════
        //   Per-turn dispatch
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Cryo_PerTurnDispatch_AppliesToCellOccupants()
        {
            // GasSystem.DispatchPerTurnApply looks for IObjectGasBehavior,
            // which GasCryoPart still inherits — so it gets called even
            // though its filter chain is slimmer than its siblings.
            var zone = new Zone("CryoTickDispatch");
            GasFactory.SpawnGas(zone, 5, 5, "cryo-mist", density: 100);
            var target = MakeCreature(zone, 5, 5);
            int hp0 = target.GetStatValue("Hitpoints");

            GasSystem.OnTickEnd(zone);

            Assert.IsNotNull(target.GetEffect<FrozenEffect>());
            Assert.Less(target.GetStatValue("Hitpoints"), hp0);
        }

        // ════════════════════════════════════════════════════════════
        //   Diag observability
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Cryo_EmitsAppliedDiag()
        {
            var zone = new Zone("CryoDiag");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "cryo-mist", density: 100, level: 2);
            var target = MakeCreature(zone, 5, 5);
            Diag.ResetAll();
            gas.GetPart<GasCryoPart>().ApplyGas(target, zone);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Applied", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"gasId\":\"cryo-mist\"", recs[0].PayloadJson);
            StringAssert.Contains("\"gasType\":\"Cryo\"", recs[0].PayloadJson);
            StringAssert.Contains("\"gasLevel\":2", recs[0].PayloadJson);
            StringAssert.Contains("\"coldDamage\":20", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   Cross-type isolation
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Cryo_DoesNotApplyStunnedOrConfused_Counter()
        {
            var zone = new Zone("CryoCounter");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "cryo-mist");
            var target = MakeCreature(zone, 5, 5);
            gas.GetPart<GasCryoPart>().ApplyGas(target, zone);
            Assert.IsNull(target.GetEffect<StunnedEffect>());
            Assert.IsNull(target.GetEffect<ConfusedEffect>());
            Assert.IsNull(target.GetEffect<PoisonedByGasEffect>());
        }

        // ════════════════════════════════════════════════════════════
        //   FrozenEffect interaction: extinguishes BurningEffect
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Cryo_AppliedToBurningCreature_Extinguishes()
        {
            // FrozenEffect.OnApply removes BurningEffect. Pin the
            // existing cross-effect contract — gas system interactions
            // with the burning/freezing axes.
            var zone = new Zone("CryoVsBurn");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "cryo-mist");
            var target = MakeCreature(zone, 5, 5);
            target.ApplyEffect(new BurningEffect(intensity: 1.0f));
            Assert.IsNotNull(target.GetEffect<BurningEffect>(), "precondition");

            gas.GetPart<GasCryoPart>().ApplyGas(target, zone);
            Assert.IsNull(target.GetEffect<BurningEffect>(),
                "cryo-applied FrozenEffect extinguished the burn");
        }
    }
}
