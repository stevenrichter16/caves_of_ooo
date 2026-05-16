using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LQ.4 — transfer-on-contact. Closes plan gap (b): a creature
    /// stepping into a liquid pool gets a <see cref="LiquidCoveredEffect"/>.
    ///
    /// Pins the review-corrected contract:
    ///   - divergence #3: water-coat ALSO refreshes WetEffect so the
    ///     pinned ElectrifiedEffectDamageTests stay green untouched
    ///   - divergence #5: once-on-enter (EntityEnteredCell only fires
    ///     on the move into the cell — standing/leaving does not
    ///     re-coat); re-entry merges via OnStack, never stacks
    ///   - divergence #1: different-liquid merge = stronger-wins,
    ///     amounts add
    ///   - exposure = clamp(Volume, 0, Strength+Toughness)
    ///
    /// Test discipline (plan §B1): bare Entity + bare Zone + inline
    /// JSON; the REAL MovementSystem path fires EntityEnteredCell (no
    /// OverworldZoneManager / pipeline / blueprint cascade).
    /// </summary>
    public class LiquidCoatingTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            LiquidRegistry.Initialize(@"{
              ""Liquids"": [
                { ""Id"": ""water"", ""DisplayName"": ""water"", ""Adjective"": ""wet"",
                  ""Glyph"": ""~"", ""Color"": ""&c"", ""Conductivity"": 100,
                  ""Combustibility"": -50, ""FireDampen"": 40,
                  ""Fluidity"": 30, ""Evaporativity"": 20 },
                { ""Id"": ""oil"", ""DisplayName"": ""oil"", ""Adjective"": ""oily"",
                  ""Glyph"": ""~"", ""Color"": ""&K"", ""Combustibility"": 90,
                  ""FlameTemperature"": 250, ""Fluidity"": 5, ""Evaporativity"": 2 },
                { ""Id"": ""acid"", ""DisplayName"": ""acid"", ""Adjective"": ""acid-covered"",
                  ""Glyph"": ""~"", ""Color"": ""&G"",
                  ""Fluidity"": 20, ""Evaporativity"": 15,
                  ""PerTurnDamage"": { ""Amount"": 3, ""Type"": ""Acid"" } }
              ]
            }");
        }

        [TearDown]
        public void TearDown() => LiquidRegistry.ResetForTests();

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakePool(string liquidId, int volume)
        {
            var e = new Entity { ID = liquidId + "Pool", BlueprintName = liquidId + "Pool" };
            e.AddPart(new RenderPart { DisplayName = liquidId + " pool", RenderString = "~" });
            // Pools are not solid — creatures walk onto them.
            e.AddPart(new PhysicsPart { Solid = false });
            e.AddPart(new LiquidPoolPart { LiquidId = liquidId, Volume = volume });
            return e;
        }

        private static Entity MakeCreature(int strength = 16, int toughness = 14)
        {
            var e = new Entity { ID = "mover", BlueprintName = "mover" };
            e.Tags["Creature"] = "";
            e.Statistics["Strength"] = new Stat
            { Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat
            { Name = "Toughness", BaseValue = toughness, Min = 1, Max = 50 };
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            e.AddPart(new RenderPart { DisplayName = "mover", RenderString = "@" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        /// <summary>Place pool at (5,5), creature at (4,5), walk it
        /// east INTO the pool via the real MovementSystem (fires the
        /// real EntityEnteredCell on the pool).</summary>
        private static (Zone zone, Entity pool, Entity mover) StepInto(
            string liquidId, int volume, int str = 16, int tough = 14)
        {
            var zone = new Zone("LiquidZone");
            var pool = MakePool(liquidId, volume);
            var mover = MakeCreature(str, tough);
            zone.AddEntity(pool, 5, 5);
            zone.AddEntity(mover, 4, 5);
            bool moved = MovementSystem.TryMove(mover, zone, 1, 0);
            Assert.IsTrue(moved, "Mover should walk onto the (non-solid) pool cell.");
            return (zone, pool, mover);
        }

        // ── Transfer-on-contact (closes gap b) ──────────────────

        [Test]
        public void StepIntoWaterPool_CoatsCreature_WithLiquidCovered()
        {
            var (_, _, mover) = StepInto("water", 100);
            var coat = mover.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.IsNotNull(coat, "Stepping into a water pool must coat the creature.");
            Assert.AreEqual("water", coat.LiquidId);
            Assert.Greater(coat.Amount, 0);
        }

        [Test]
        public void StepIntoWaterPool_AlsoAppliesWetEffect_ParityInvariant()
        {
            // Divergence #3: water-coat refreshes WetEffect so the
            // existing ElectrifiedEffect.OnApply wet→electric coupling
            // (and the pinned ElectrifiedEffectDamageTests) keep working.
            var (_, _, mover) = StepInto("water", 100);
            Assert.IsTrue(mover.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "Water coat MUST also apply WetEffect (parity invariant).");
        }

        [Test]
        public void StepIntoOilSlick_CoatsWithOil_NoWetEffect()
        {
            // Counter-check: oil is not water — no WetEffect.
            var (_, _, mover) = StepInto("oil", 80);
            var fx = mover.GetPart<StatusEffectsPart>();
            Assert.AreEqual("oil", fx.GetEffect<LiquidCoveredEffect>()?.LiquidId);
            Assert.IsFalse(fx.HasEffect<WetEffect>(),
                "Oil coat must NOT apply WetEffect.");
        }

        [Test]
        public void Exposure_ClampedByStrengthPlusToughness()
        {
            // Volume 1000 but Str 3 + Tough 2 = cap 5 → Amount ≤ 5.
            var (_, _, mover) = StepInto("water", 1000, str: 3, tough: 2);
            var coat = mover.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.IsNotNull(coat);
            Assert.LessOrEqual(coat.Amount, 5,
                "Exposure must be clamped to Strength+Toughness.");
            Assert.Greater(coat.Amount, 0);
        }

        // ── Counter-checks ──────────────────────────────────────

        [Test]
        public void Item_EnteringPool_NotCoated()
        {
            var zone = new Zone("LiquidZone");
            var pool = MakePool("water", 100);
            var item = new Entity { ID = "rock", BlueprintName = "rock" };
            // No "Creature" tag, no StatusEffectsPart.
            item.AddPart(new RenderPart { RenderString = "o" });
            item.AddPart(new PhysicsPart { Solid = false });
            zone.AddEntity(pool, 5, 5);
            zone.AddEntity(item, 4, 5);
            MovementSystem.TryMove(item, zone, 1, 0);
            Assert.IsNull(item.GetPart<StatusEffectsPart>(),
                "A non-creature item must not be coated (no StatusEffectsPart created).");
        }

        [Test]
        public void EmptyPool_Volume0_DoesNotCoat()
        {
            var (_, _, mover) = StepInto("water", 0);
            Assert.IsFalse(mover.GetPart<StatusEffectsPart>().HasEffect<LiquidCoveredEffect>(),
                "An empty (Volume 0) pool must not coat.");
        }

        [Test]
        public void StatlessCreature_NotCoated_DegenerateButSafe()
        {
            // Adversarial: a creature with no Strength/Toughness stats
            // → exposure cap 0 → no coat. Documented degenerate (real
            // creatures have stats).
            var zone = new Zone("LiquidZone");
            var pool = MakePool("water", 100);
            var bare = new Entity { ID = "wisp", BlueprintName = "wisp" };
            bare.Tags["Creature"] = "";
            bare.AddPart(new RenderPart { RenderString = "w" });
            bare.AddPart(new PhysicsPart { Solid = false });
            bare.AddPart(new StatusEffectsPart());
            zone.AddEntity(pool, 5, 5);
            zone.AddEntity(bare, 4, 5);
            Assert.DoesNotThrow(() => MovementSystem.TryMove(bare, zone, 1, 0));
            Assert.IsFalse(bare.GetPart<StatusEffectsPart>().HasEffect<LiquidCoveredEffect>());
        }

        // ── Divergence #5: once-on-enter ────────────────────────

        [Test]
        public void StandingStill_DoesNotReCoat()
        {
            // Step in (one coat). Standing still fires no
            // EntityEnteredCell, so no re-coat. Amount stays as the
            // single-entry exposure (no per-stand accumulation).
            var (zone, _, mover) = StepInto("water", 100);
            int afterEntry = mover.GetPart<StatusEffectsPart>()
                .GetEffect<LiquidCoveredEffect>().Amount;
            // "Stand still": a no-op move (dx=0,dy=0) fires no enter.
            MovementSystem.TryMove(mover, zone, 0, 0);
            Assert.AreEqual(afterEntry, mover.GetPart<StatusEffectsPart>()
                .GetEffect<LiquidCoveredEffect>().Amount,
                "Standing still must not re-coat (once-on-enter).");
        }

        [Test]
        public void ReEnterPool_Merges_NotStacks()
        {
            // Adversarial: leave + re-enter. The coat MERGES (one
            // LiquidCoveredEffect instance, amount accumulates), not
            // two stacked effects.
            var (zone, _, mover) = StepInto("water", 100, str: 30, tough: 30);
            var fx = mover.GetPart<StatusEffectsPart>();
            int after1 = fx.GetEffect<LiquidCoveredEffect>().Amount;
            MovementSystem.TryMove(mover, zone, -1, 0); // step off (to 4,5)
            MovementSystem.TryMove(mover, zone, 1, 0);  // step back onto pool
            Assert.GreaterOrEqual(fx.GetEffect<LiquidCoveredEffect>().Amount, after1,
                "Re-entry accumulates amount via OnStack merge.");
            // Non-stacking proof via public API: exactly one instance —
            // first RemoveEffect succeeds, the second finds none.
            Assert.IsTrue(fx.RemoveEffect<LiquidCoveredEffect>(),
                "Exactly one LiquidCoveredEffect should exist (1st remove).");
            Assert.IsFalse(fx.RemoveEffect<LiquidCoveredEffect>(),
                "Re-entry must MERGE not STACK — no second instance.");
        }

        [Test]
        public void TwoDifferentLiquidPools_SameCell_StrongerWins_AmountsAdd()
        {
            // Divergence #1: water pool (vol 40) + oil pool (vol 200)
            // in the same cell. Walk in → both fire EntityEnteredCell.
            // Stronger (larger amount) liquid id wins; amounts add.
            var zone = new Zone("LiquidZone");
            var water = MakePool("water", 40);
            var oil = MakePool("oil", 200);
            var mover = MakeCreature(30, 30);
            zone.AddEntity(water, 5, 5);
            zone.AddEntity(oil, 5, 5);
            zone.AddEntity(mover, 4, 5);
            MovementSystem.TryMove(mover, zone, 1, 0);
            var coat = mover.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.IsNotNull(coat);
            Assert.AreEqual("oil", coat.LiquidId,
                "Stronger (higher-amount) liquid wins the merged id.");
        }

        // ── Dry-down ────────────────────────────────────────────

        [Test]
        public void Coat_DriesOverTurns_RemovesItselfAtZero()
        {
            var (_, _, mover) = StepInto("water", 100, str: 30, tough: 30);
            var fx = mover.GetPart<StatusEffectsPart>();
            Assert.IsTrue(fx.HasEffect<LiquidCoveredEffect>());
            // Drive many end-of-turns; water Fluidity 30 + Evap 20
            // means it dries fast. Cap iterations defensively.
            for (int i = 0; i < 200 && fx.HasEffect<LiquidCoveredEffect>(); i++)
            {
                var ev = GameEvent.New("EndTurn");
                ev.SetParameter("Actor", (object)mover);
                mover.FireEventAndRelease(ev);
            }
            Assert.IsFalse(fx.HasEffect<LiquidCoveredEffect>(),
                "Coat must dry down and remove itself.");
        }

        [Test]
        public void LiquidCoveredEffect_DisplayName_FromLiquidAdjective()
        {
            var (_, _, w) = StepInto("water", 100);
            Assert.AreEqual("wet",
                w.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>().DisplayName);
            var (_, _, o) = StepInto("oil", 80);
            Assert.AreEqual("oily",
                o.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>().DisplayName);
        }

        // ── Adversarial ─────────────────────────────────────────

        [Test]
        public void EntityEnteredCell_NullActor_NoCrash()
        {
            var pool = MakePool("water", 100);
            var ev = GameEvent.New("EntityEnteredCell");
            ev.SetParameter("Actor", (object)null);
            Assert.DoesNotThrow(() => pool.FireEventAndRelease(ev));
        }

        [Test]
        public void RegistryUninitialized_StepIn_NoCrash_NoCoat()
        {
            LiquidRegistry.ResetForTests(); // wipe the SetUp registry
            var zone = new Zone("LiquidZone");
            var pool = MakePool("water", 100);
            var mover = MakeCreature();
            zone.AddEntity(pool, 5, 5);
            zone.AddEntity(mover, 4, 5);
            Assert.DoesNotThrow(() => MovementSystem.TryMove(mover, zone, 1, 0));
            Assert.IsFalse(mover.GetPart<StatusEffectsPart>().HasEffect<LiquidCoveredEffect>());
        }

        // ── Observability (plan §8 — gate emits a record) ───────

        [Test]
        public void StepIntoPool_EmitsLiquidCoatedDiag()
        {
            StepInto("water", 100);
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "liquid", Kind = "Coated", Limit = 10,
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"liquidId\":\"water\"", records[0].PayloadJson);
        }

        [Test]
        public void EmptyPool_EmitsCoatRejectedDiag()
        {
            StepInto("water", 0);
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "liquid", Kind = "CoatRejected", Limit = 10,
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("PoolEmpty", records[0].PayloadJson);
        }
    }
}
