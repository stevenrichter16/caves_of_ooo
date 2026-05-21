using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.9 — BurnOffGasPart (outgassing-on-fire). The inverse coupling:
    /// fire damage taken by an entity → gas spawned at its cell.
    /// Behavior is exercised END-TO-END through CombatSystem.ApplyDamage
    /// so the tests prove the TakeDamage event actually reaches the Part
    /// in the real combat flow (not just a hand-fired event).
    ///
    /// Notable surfaces:
    ///   (a) threshold accumulation + multi-crossing while-loop
    ///   (b) Number roll — incl. the plain-int "1" that DiceRoller alone
    ///       CANNOT parse (the verification-sweep false premise)
    ///   (c) Chance gate (0 never / 100 always) — drains DamageTaken
    ///       even on a failed roll (Qud parity)
    ///   (d) trigger-attribute filtering (Heat/Fire yes, Cold/Bludgeon no)
    /// </summary>
    public class BurnOffGasPartTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""test-smoke"", ""GasType"":""Smoke"",
                ""Glyph"":""°"", ""Color"":""&K"",
                ""DefaultDensity"":50, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" } ] }");
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            BurnOffGasPart.TestRng = null;
            SettlementRuntime.ActiveZone = null;
        }

        // Entity that survives big fire hits (high HP) with 0 HeatResistance
        // so the full Heat amount lands at the TakeDamage event.
        private static Entity MakeFlammable(Zone zone, int x, int y,
            string gasId = "test-smoke", int damagePer = 10, int chance = 100,
            string number = "1")
        {
            var e = new Entity { ID = "f_" + x + "_" + y, BlueprintName = "Peat" };
            e.Tags["Creature"] = "";
            void S(string n, int v, int max = 100000) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", 100000, 100000);
            S("HeatResistance", 0); S("ColdResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "peat" });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new BurnOffGasPart
            {
                GasId = gasId, DamagePer = damagePer, Chance = chance, Number = number
            });
            zone.AddEntity(e, x, y);
            return e;
        }

        private static void Burn(Entity e, Zone zone, int amount, string attribute = "Heat")
        {
            var d = new Damage(amount);
            d.AddAttribute(attribute);
            CombatSystem.ApplyDamage(e, d, null, zone);
        }

        private static int CountGasAt(Zone zone, int x, int y)
        {
            int n = 0;
            foreach (var ent in zone.GetAllEntities())
            {
                if (!ent.Tags.ContainsKey("Gas")) continue;
                var p = zone.GetEntityPosition(ent);
                if (p.x == x && p.y == y) n++;
            }
            return n;
        }

        // ════════════════════════════════════════════════════════════
        //   PART I — accumulate + threshold spawn (via ApplyDamage)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void BurnOff_HeatDamageAtThreshold_SpawnsOne()
        {
            var zone = new Zone("BurnThreshold");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5);
            Burn(e, zone, 10);
            Assert.AreEqual(1, CountGasAt(zone, 5, 5), "10 Heat ≥ DamagePer 10 → 1 gas");
        }

        [Test]
        public void BurnOff_BelowThreshold_DoesNotSpawn()
        {
            var zone = new Zone("BurnBelow");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5);
            Burn(e, zone, 5);
            Assert.AreEqual(0, CountGasAt(zone, 5, 5), "5 < 10 → no spawn");
            Assert.AreEqual(5, e.GetPart<BurnOffGasPart>().DamageTaken, "accumulated 5");
        }

        [Test]
        public void BurnOff_AccumulatesAcrossHits()
        {
            var zone = new Zone("BurnAccum");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5);
            Burn(e, zone, 5);
            Assert.AreEqual(0, CountGasAt(zone, 5, 5), "first 5 doesn't spawn");
            Burn(e, zone, 5);
            Assert.AreEqual(1, CountGasAt(zone, 5, 5), "5+5=10 → spawns on 2nd hit");
            Assert.AreEqual(0, e.GetPart<BurnOffGasPart>().DamageTaken, "drained to 0");
        }

        [Test]
        public void BurnOff_LargeHit_CrossesThresholdTwice()
        {
            var zone = new Zone("BurnLarge");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5);
            Burn(e, zone, 25); // floor(25/10) = 2 cycles, remainder 5
            Assert.AreEqual(2, CountGasAt(zone, 5, 5), "25 crosses 10-threshold twice");
            Assert.AreEqual(5, e.GetPart<BurnOffGasPart>().DamageTaken, "remainder 5");
        }

        [Test]
        public void BurnOff_FireAttribute_AlsoTriggers()
        {
            var zone = new Zone("BurnFireAttr");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5);
            Burn(e, zone, 10, attribute: "Fire");
            Assert.AreEqual(1, CountGasAt(zone, 5, 5), "Fire is in DamageTriggerTypes");
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — Number roll (incl. the false-premise plain-int fix)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void BurnOff_Number1_SpawnsExactlyOne()
        {
            // THE false-premise pin: DiceRoller.Roll("1") returns 0 (no
            // 'd'). A naive port would spawn ZERO. RollNumber must handle
            // the plain int.
            var zone = new Zone("BurnNum1");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5, number: "1");
            Burn(e, zone, 10);
            Assert.AreEqual(1, CountGasAt(zone, 5, 5), "Number=\"1\" → exactly 1 (not 0)");
        }

        [Test]
        public void BurnOff_NumberPlainInt_SpawnsExactly()
        {
            var zone = new Zone("BurnNum3");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5, number: "3");
            Burn(e, zone, 10);
            Assert.AreEqual(3, CountGasAt(zone, 5, 5), "Number=\"3\" → 3");
        }

        [Test]
        public void BurnOff_NumberDiceFormula_Works()
        {
            // "2d1" = 2 dice of 1 side each = deterministically 2.
            var zone = new Zone("BurnNumDice");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5, number: "2d1");
            Burn(e, zone, 10);
            Assert.AreEqual(2, CountGasAt(zone, 5, 5), "\"2d1\" → 2");
        }

        [Test]
        public void BurnOff_MalformedNumber_SpawnsZero_NoCrash()
        {
            var zone = new Zone("BurnNumGarbage");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5, number: "garbage");
            Assert.DoesNotThrow(() => Burn(e, zone, 10));
            Assert.AreEqual(0, CountGasAt(zone, 5, 5), "unparseable Number → 0 spawns");
            Assert.AreEqual(0, e.GetPart<BurnOffGasPart>().DamageTaken,
                "threshold still drained even though nothing spawned");
        }

        // ════════════════════════════════════════════════════════════
        //   PART III — Chance gate + GasId + diag
        // ════════════════════════════════════════════════════════════

        [Test]
        public void BurnOff_ZeroChance_NeverSpawns_ButDrains()
        {
            var zone = new Zone("BurnChance0");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5, chance: 0);
            Burn(e, zone, 100); // 10 threshold crossings, all fail the roll
            Assert.AreEqual(0, CountGasAt(zone, 5, 5), "Chance 0 → never spawns");
            Assert.AreEqual(0, e.GetPart<BurnOffGasPart>().DamageTaken,
                "DamageTaken still drained (Qud parity: subtract before chance)");
        }

        [Test]
        public void BurnOff_EmptyGasId_NoSpawn_NoCrash()
        {
            var zone = new Zone("BurnNoGas");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5, gasId: "");
            Assert.DoesNotThrow(() => Burn(e, zone, 10));
            Assert.AreEqual(0, CountGasAt(zone, 5, 5), "empty GasId → no spawn");
        }

        [Test]
        public void BurnOff_EmitsBurnOffDiag()
        {
            var zone = new Zone("BurnDiag");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5, number: "2");
            Diag.ResetAll();
            Burn(e, zone, 10);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "BurnOff", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count, "one BurnOff record per cycle");
            StringAssert.Contains("\"gasId\":\"test-smoke\"", recs[0].PayloadJson);
            StringAssert.Contains("\"count\":2", recs[0].PayloadJson);
        }

        [Test]
        public void BurnOff_ChanceFail_EmitsChanceFailedDiag()
        {
            var zone = new Zone("BurnChanceFailDiag");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5, chance: 0);
            Diag.ResetAll();
            Burn(e, zone, 10);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "BurnOffChanceFailed", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count, "the chance gate emits a rejection record");
        }

        // ════════════════════════════════════════════════════════════
        //   PART IV — counter-checks (non-trigger damage)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void BurnOff_ColdDamage_DoesNotAccumulate()
        {
            var zone = new Zone("BurnCold");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5);
            Burn(e, zone, 50, attribute: "Cold");
            Assert.AreEqual(0, CountGasAt(zone, 5, 5), "Cold not in trigger types");
            Assert.AreEqual(0, e.GetPart<BurnOffGasPart>().DamageTaken, "no accumulation");
        }

        [Test]
        public void BurnOff_BludgeoningDamage_DoesNotTrigger()
        {
            var zone = new Zone("BurnBludgeon");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5);
            Burn(e, zone, 50, attribute: "Bludgeoning");
            Assert.AreEqual(0, CountGasAt(zone, 5, 5), "physical damage doesn't outgas");
        }

        // ════════════════════════════════════════════════════════════
        //   PART V — adversarial / boundary (step g)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void BurnOff_NullDamageParam_NoCrash()
        {
            var zone = new Zone("BurnNullDmg");
            SettlementRuntime.ActiveZone = zone;
            var e = MakeFlammable(zone, 5, 5);
            var ev = GameEvent.New("TakeDamage"); // no Damage param set
            Assert.DoesNotThrow(() => e.FireEventAndRelease(ev));
            Assert.AreEqual(0, CountGasAt(zone, 5, 5));
        }

        [Test]
        public void BurnOff_NoActiveZone_NoCrash_NoSpawn()
        {
            var zone = new Zone("BurnNoActiveZone");
            // Deliberately do NOT set SettlementRuntime.ActiveZone.
            var e = MakeFlammable(zone, 5, 5);
            Assert.DoesNotThrow(() => Burn(e, zone, 10));
            Assert.AreEqual(0, CountGasAt(zone, 5, 5), "no zone to resolve → no spawn");
        }

        [Test]
        public void BurnOff_NotInZone_NoCrash()
        {
            // Entity has the part + accumulates, but ActiveZone doesn't
            // contain it → GetEntityPosition returns sentinel → no spawn.
            var realZone = new Zone("BurnReal");
            var otherZone = new Zone("BurnOther");
            SettlementRuntime.ActiveZone = otherZone; // entity is NOT here
            var e = MakeFlammable(realZone, 5, 5);
            Assert.DoesNotThrow(() => Burn(e, realZone, 10));
            Assert.AreEqual(0, CountGasAt(otherZone, 5, 5), "not in active zone → no spawn");
        }
    }
}
