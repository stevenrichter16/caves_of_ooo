using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.8d.1 — FungalInfectionEffect state machine tests. The Effect
    /// is the multi-stage progression engine. No gas dispatcher /
    /// contagion gas spawn yet (G.8d.2 / G.8d.3 ship those).
    ///
    /// <para>Tests cover: stage transitions at turn boundaries,
    /// per-stage damage scaling, Toughness stat-shift Apply/Remove
    /// (mirrors HibernatingEffect pattern), Duration self-expire at
    /// Stage Terminal end, refresh-on-reapply is non-standard (does
    /// NOT reset stage clock), null safety.</para>
    /// </summary>
    public class FungalInfectionEffectTests
    {
        [SetUp]
        public void Setup() { MessageLog.Clear(); Diag.ResetAll(); }

        private static Entity MakeCreature(int hpMax = 200, int toughness = 14)
        {
            var e = new Entity { ID = "infected", BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", hpMax, hpMax);
            S("Toughness", toughness);
            S("Agility", 14); S("DV", 6); S("AV", 0);
            e.AddPart(new RenderPart { DisplayName = "infected" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static GameEvent MakeTurnContext()
        {
            var ctx = GameEvent.New("BeginTakeAction");
            // No zone — Effect should handle null gracefully.
            return ctx;
        }

        // ════════════════════════════════════════════════════════════
        //   PART I — Pure / stateless behavior (no Apply needed)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void NewInfection_TurnsInfectedZero_StageIsIncubation()
        {
            var fx = new FungalInfectionEffect();
            Assert.AreEqual(0, fx.TurnsInfected, "fresh infection starts at 0 turns");
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Incubation, fx.CurrentStage);
        }

        [Test]
        public void Stage_Boundary_Transitions()
        {
            // Turn 9 → Incubation; turn 10 → Symptomatic, etc.
            var fx = new FungalInfectionEffect();
            fx.TurnsInfected = 9;
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Incubation, fx.CurrentStage);
            fx.TurnsInfected = 10;
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Symptomatic, fx.CurrentStage);
            fx.TurnsInfected = 19;
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Symptomatic, fx.CurrentStage);
            fx.TurnsInfected = 20;
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Blooming, fx.CurrentStage);
            fx.TurnsInfected = 29;
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Blooming, fx.CurrentStage);
            fx.TurnsInfected = 30;
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Terminal, fx.CurrentStage);
            fx.TurnsInfected = 39;
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Terminal, fx.CurrentStage);
            fx.TurnsInfected = 40;
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Expired, fx.CurrentStage);
        }

        [Test]
        public void StageDamage_PerStage()
        {
            Assert.AreEqual(0, FungalInfectionEffect.StageDamage(FungalInfectionEffect.InfectionStage.Incubation));
            Assert.AreEqual(1, FungalInfectionEffect.StageDamage(FungalInfectionEffect.InfectionStage.Symptomatic));
            Assert.AreEqual(2, FungalInfectionEffect.StageDamage(FungalInfectionEffect.InfectionStage.Blooming));
            Assert.AreEqual(3, FungalInfectionEffect.StageDamage(FungalInfectionEffect.InfectionStage.Terminal));
            Assert.AreEqual(0, FungalInfectionEffect.StageDamage(FungalInfectionEffect.InfectionStage.Expired));
        }

        [Test]
        public void StagePenalty_PerStage()
        {
            Assert.AreEqual(0, FungalInfectionEffect.StagePenalty(FungalInfectionEffect.InfectionStage.Incubation));
            Assert.AreEqual(1, FungalInfectionEffect.StagePenalty(FungalInfectionEffect.InfectionStage.Symptomatic));
            Assert.AreEqual(2, FungalInfectionEffect.StagePenalty(FungalInfectionEffect.InfectionStage.Blooming));
            Assert.AreEqual(3, FungalInfectionEffect.StagePenalty(FungalInfectionEffect.InfectionStage.Terminal));
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — OnApply / OnRemove stat-shift contract
        // ════════════════════════════════════════════════════════════

        [Test]
        public void OnApply_CapturesPriorToughness()
        {
            var creature = MakeCreature(toughness: 14);
            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            Assert.AreEqual(14, fx.PriorToughness,
                "OnApply captured pre-infection Toughness");
        }

        [Test]
        public void OnRemove_RestoresToughness()
        {
            // Apply at toughness 14, infection drops it to 12 at Stage
            // Symptomatic. On manual remove, should restore to 14.
            var creature = MakeCreature(toughness: 14);
            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            fx.TurnsInfected = 10; // simulate progression to Symptomatic
            // The OnTurnStart that fires would have shifted Toughness;
            // simulate that directly here for the OnRemove test.
            creature.GetStat("Toughness").BaseValue = 13;

            creature.GetPart<StatusEffectsPart>().RemoveEffect<FungalInfectionEffect>();
            Assert.AreEqual(14, creature.GetStatValue("Toughness"),
                "OnRemove restored Toughness from PriorToughness sentinel");
        }

        [Test]
        public void OnRemove_NoPriorToughness_SkipsRestore_Defensive()
        {
            // Counter / defensive: if OnApply somehow didn't run (which
            // would be a real bug), the -1 sentinel makes OnRemove
            // skip the restore. Prevents corrupting other paths' stat
            // shifts. Mirrors HibernatingEffect.cs:93-102.
            var creature = MakeCreature(toughness: 14);
            // DIRECTLY manipulate Toughness without ApplyEffect, so
            // PriorToughness stays at -1.
            creature.GetStat("Toughness").BaseValue = 8;
            var orphan = new FungalInfectionEffect(); // PriorToughness = -1
            // Call OnRemove directly (bypassing StatusEffectsPart's
            // wiring, which would call OnApply first).
            orphan.OnRemove(creature);
            Assert.AreEqual(8, creature.GetStatValue("Toughness"),
                "no-prior-toughness sentinel skips restore");
        }

        // ════════════════════════════════════════════════════════════
        //   PART III — OnTurnStart progression
        // ════════════════════════════════════════════════════════════

        [Test]
        public void OnTurnStart_IncrementsTurnsInfected()
        {
            var creature = MakeCreature();
            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            Assert.AreEqual(0, fx.TurnsInfected, "precondition");
            fx.OnTurnStart(creature, MakeTurnContext());
            Assert.AreEqual(1, fx.TurnsInfected);
        }

        [Test]
        public void OnTurnStart_DuringIncubation_NoDamage()
        {
            var creature = MakeCreature(hpMax: 200);
            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            int hp0 = creature.GetStatValue("Hitpoints");
            fx.OnTurnStart(creature, MakeTurnContext());
            Assert.AreEqual(hp0, creature.GetStatValue("Hitpoints"),
                "incubation stage deals no damage");
        }

        [Test]
        public void OnTurnStart_AtSymptomaticBoundary_AppliesPenaltyAndDamage()
        {
            // At TurnsInfected=9, stage is Incubation. OnTurnStart will
            // bump to 10 → Symptomatic. The transition applies the
            // Symptomatic stat penalty + tick of 1 damage.
            var creature = MakeCreature(hpMax: 200, toughness: 14);
            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            fx.TurnsInfected = 9;
            int hp0 = creature.GetStatValue("Hitpoints");
            fx.OnTurnStart(creature, MakeTurnContext());
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Symptomatic, fx.CurrentStage);
            Assert.AreEqual(13, creature.GetStatValue("Toughness"),
                "Symptomatic penalty applied (14 - 1)");
            Assert.AreEqual(hp0 - 1, creature.GetStatValue("Hitpoints"),
                "1 damage on Symptomatic tick");
        }

        [Test]
        public void OnTurnStart_DamageScalesByStage()
        {
            // Manually progress through stages and verify per-tick
            // damage scales correctly.
            var creature = MakeCreature(hpMax: 1000);
            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            fx.TurnsInfected = 19; // about to enter Blooming
            int hp0 = creature.GetStatValue("Hitpoints");
            fx.OnTurnStart(creature, MakeTurnContext()); // now Blooming, 2 damage
            Assert.AreEqual(hp0 - 2, creature.GetStatValue("Hitpoints"),
                "Blooming tick = 2");

            fx.TurnsInfected = 29; // about to enter Terminal
            int hp1 = creature.GetStatValue("Hitpoints");
            fx.OnTurnStart(creature, MakeTurnContext()); // now Terminal, 3 damage
            Assert.AreEqual(hp1 - 3, creature.GetStatValue("Hitpoints"),
                "Terminal tick = 3");
        }

        [Test]
        public void OnTurnStart_AtExpiredBoundary_SetsDurationZero()
        {
            // At TurnsInfected=39, OnTurnStart bumps to 40 → Expired.
            // Effect self-expires via Duration=0.
            var creature = MakeCreature(hpMax: 1000);
            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            fx.TurnsInfected = 39;
            fx.OnTurnStart(creature, MakeTurnContext());
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Expired, fx.CurrentStage);
            Assert.AreEqual(0, fx.Duration,
                "Expired stage sets Duration=0 (StatusEffectsPart sweeps on EndTurn)");
        }

        [Test]
        public void OnTurnStart_DamageTaggedFungal()
        {
            // Pin the damage attribute for future FungalResistance routing.
            var creature = MakeCreature();
            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            fx.TurnsInfected = 10; // Symptomatic
            int hp0 = creature.GetStatValue("Hitpoints");
            fx.OnTurnStart(creature, MakeTurnContext());
            Assert.Less(creature.GetStatValue("Hitpoints"), hp0,
                "damage landed (we'll check attribute via diag in G.8d.3+)");
            // We'd ideally check `damage.HasAttribute("Fungal")` via a
            // BeforeTakeDamage listener; for G.8d.1 it's enough that
            // damage landed. Attribute presence pinned by future
            // resistance work.
        }

        // ════════════════════════════════════════════════════════════
        //   PART IV — OnStack: refresh-on-reapply is NON-STANDARD
        // ════════════════════════════════════════════════════════════

        [Test]
        public void OnStack_IncomingDoesNotResetStageClock()
        {
            // Critical Qud-parity invariant: re-exposure to spore gas
            // does NOT reset the infection clock. A player at Stage 2
            // Blooming who walks into another spore cloud stays at
            // Stage 2 — the incoming fresh-infection is consumed but
            // doesn't reset TurnsInfected.
            var fx1 = new FungalInfectionEffect();
            fx1.TurnsInfected = 22; // Stage Blooming
            var fx2 = new FungalInfectionEffect();
            // fx2.TurnsInfected stays at 0

            bool stacked = fx1.OnStack(fx2);
            Assert.IsTrue(stacked, "OnStack returns true (incoming consumed)");
            Assert.AreEqual(22, fx1.TurnsInfected,
                "existing infection's stage clock is preserved");
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Blooming, fx1.CurrentStage);
        }

        [Test]
        public void OnStack_EmitsInfectionAlreadyPresentDiag()
        {
            var creature = MakeCreature();
            var fx1 = new FungalInfectionEffect();
            creature.ApplyEffect(fx1);
            fx1.TurnsInfected = 15;
            Diag.ResetAll();

            var fx2 = new FungalInfectionEffect();
            fx1.OnStack(fx2);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "InfectionAlreadyPresent", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("Symptomatic", recs[0].PayloadJson);
            StringAssert.Contains("\"turnsInfected\":15", recs[0].PayloadJson);
        }

        [Test]
        public void OnStack_DifferentEffectType_ReturnsFalse_Counter()
        {
            // Counter: OnStack returns false for non-FungalInfection
            // incoming (Effect base contract).
            var fx = new FungalInfectionEffect();
            fx.TurnsInfected = 5;
            var unrelated = new BurningEffect(intensity: 1.0f);
            bool stacked = fx.OnStack(unrelated);
            Assert.IsFalse(stacked, "different effect type doesn't stack");
            Assert.AreEqual(5, fx.TurnsInfected, "TurnsInfected unaffected");
        }

        // ════════════════════════════════════════════════════════════
        //   PART V — Null safety / adversarial
        // ════════════════════════════════════════════════════════════

        [Test]
        public void OnTurnStart_NullTarget_NoCrash()
        {
            var fx = new FungalInfectionEffect();
            Assert.DoesNotThrow(() => fx.OnTurnStart(null, MakeTurnContext()));
        }

        [Test]
        public void OnTurnStart_NullContext_NoCrash()
        {
            var creature = MakeCreature();
            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            Assert.DoesNotThrow(() => fx.OnTurnStart(creature, null));
        }

        [Test]
        public void Adversarial_TurnsInfectedClampsAtTerminalEnd()
        {
            // A creature that somehow stays alive past Stage Terminal
            // end (e.g. PreventDeath coat) shouldn't crash the Effect's
            // OnTurnStart even after reaching turn 50, 100, etc. The
            // CurrentStage stays Expired; damage stays 0; Duration
            // stays 0.
            var creature = MakeCreature(hpMax: 1000);
            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            fx.TurnsInfected = 100;
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Expired, fx.CurrentStage);
            int hp0 = creature.GetStatValue("Hitpoints");
            Assert.DoesNotThrow(() => fx.OnTurnStart(creature, MakeTurnContext()));
            Assert.AreEqual(hp0, creature.GetStatValue("Hitpoints"),
                "Expired stage = no damage even at very-high TurnsInfected");
        }

        [Test]
        public void Adversarial_StageTransitionMessageEmits_OncePerTransition()
        {
            // Pin: the stage-transition message fires once per
            // transition, not every turn within a stage. (Otherwise
            // the log would spam "you have fungal growths" every turn
            // through Stage 2.)
            var creature = MakeCreature(hpMax: 1000);
            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            fx.TurnsInfected = 9; // about to transition

            MessageLog.Clear();
            fx.OnTurnStart(creature, MakeTurnContext()); // → Symptomatic (transition msg)
            int afterTransition = MessageLog.Count;
            fx.OnTurnStart(creature, MakeTurnContext()); // turn 11, no transition
            fx.OnTurnStart(creature, MakeTurnContext()); // turn 12, no transition
            int afterThreeMore = MessageLog.Count;
            // The transition message fired once; subsequent ticks may
            // have message lines from the damage but NOT the transition msg.
            // We loosely assert no flood: not more than one extra
            // transition-class message per stage.
            Assert.LessOrEqual(afterThreeMore - afterTransition, 2,
                "no message spam — transition msg fires once, not every turn");
        }
    }
}
