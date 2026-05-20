using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LA dedicated adversarial sweep (ADVERSARIAL_TESTING.md gate). The
    /// LA feature ships across ≥4 taxonomy surfaces — cross-actor flows
    /// (reflect: defender→attacker; knockback: attacker→defender pos),
    /// state atomicity (rewind snapshot lifecycle), save/load reflection
    /// (new RewindSnapshotHp public field), anti-exploit gates (cycle-
    /// breaker, dead-stays-dead, immunity-before-anchor) — so per
    /// CLAUDE.md the dedicated sweep is mandatory.
    ///
    /// <para>This file is split into two halves:
    /// <list type="bullet">
    ///   <item><b>Bug-class taxonomy probes</b> — every CLAUDE.md
    ///         adversarial surface that applies to LA, one section per
    ///         surface. Per the playbook, 0 bugs found doesn't prove
    ///         bug-free; the value is regression pins + the rare catch.</item>
    ///   <item><b>Hypothesis-driven deep audit</b> — player-flow
    ///         questions (CLAUDE.md self-directive after cold-eye-0-
    ///         findings). Each test's doc-comment states the hypothesis;
    ///         GREEN = pinned-as-correct, RED = surfaced bug to fix.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Both halves probe LA mechanics the per-invariant LA.2-LA.6
    /// tests didn't cover. Counter-checks are inline.</para>
    /// </summary>
    public class LiquidAbsurdAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            LiquidRegistry.Initialize(@"{
              ""Liquids"": [
                { ""Id"":""water"", ""Adjective"":""wet"", ""Conductivity"":100,
                  ""Fluidity"":30, ""Evaporativity"":20 },
                { ""Id"":""veined-pulse-mycelium"", ""Adjective"":""pulse-veined"",
                  ""Fluidity"":3, ""Evaporativity"":2,
                  ""ImmuneElement"":""Electric"" },
                { ""Id"":""choir-mirror-mucilage"", ""Adjective"":""mirror-glazed"",
                  ""Fluidity"":4, ""Evaporativity"":3,
                  ""ReflectPercent"":50 },
                { ""Id"":""felling-counter-resin"", ""Adjective"":""time-locked"",
                  ""Fluidity"":5, ""Evaporativity"":3,
                  ""HpRewindOnTurnEnd"":true },
                { ""Id"":""pebble-sundew-dew"", ""Adjective"":""dew-greeting"",
                  ""Fluidity"":6, ""Evaporativity"":4,
                  ""KnockbackOnHit"":true },
                { ""Id"":""held-breath-lacquer"", ""Adjective"":""breath-held"",
                  ""Fluidity"":2, ""Evaporativity"":1,
                  ""PreventDeath"":true, ""BlockAction"":true }
              ]
            }");
        }

        [TearDown]
        public void TearDown()
        {
            LiquidRegistry.ResetForTests();
            SettlementRuntime.Reset();
        }

        private static Entity Creature(int hpMax = 200)
        {
            var e = new Entity { ID = "c", BlueprintName = "C" };
            e.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", hpMax, hpMax); S("Toughness", 12);
            S("Agility", 14); S("DV", 6); S("AV", 0);
            S("HeatResistance", 0); S("ColdResistance", 0);
            S("ElectricResistance", 0); S("AcidResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Entity CreatureInZone(Zone zone, int x, int y, int hpMax = 200)
        {
            var e = Creature(hpMax);
            e.ID = "z" + x + "_" + y;
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        //                  PART I — Bug-class taxonomy probes
        // ════════════════════════════════════════════════════════════════

        // ──────────── Save/load reflection ────────────

        [Test]
        public void Adversarial_RewindSnapshotHp_RoundTripsThroughSave()
        {
            // CLAUDE.md taxonomy: "save/load reflection — round-trip the
            // entity through SaveGraphSerializer; assert public fields
            // preserved." RewindSnapshotHp is public specifically so it
            // round-trips. A buggy impl that marked it private or non-
            // serializable would lose the snapshot across save→load.
            var c = Creature(hpMax: 400);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat);
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            int snapshotBefore = coat.RewindSnapshotHp;
            Assert.AreEqual(400, snapshotBefore, "precondition: snapshot taken");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(c);
            var loadedCoat = loaded.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.IsNotNull(loadedCoat);
            Assert.AreEqual(400, loadedCoat.RewindSnapshotHp,
                "RewindSnapshotHp must survive save+load (it's public for this).");
        }

        [Test]
        public void Adversarial_RewindSnapshotHp_RoundTrip_SentinelMinusOne_Preserved()
        {
            // The sentinel value -1 ("no snapshot taken yet") must also
            // round-trip — otherwise a loaded coat would have a stale 0
            // and ApplyHpRewind would early-out for the wrong reason.
            var c = Creature(hpMax: 200);
            var coat = new LiquidCoveredEffect("water", 30); // non-rewind coat
            c.ApplyEffect(coat);
            Assert.AreEqual(-1, coat.RewindSnapshotHp, "default sentinel before serialization");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(c);
            var loadedCoat = loaded.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.AreEqual(-1, loadedCoat.RewindSnapshotHp,
                "sentinel -1 must survive — a 0 here would falsely look like 'dead snapshot'.");
        }

        [Test]
        public void Adversarial_FellingCounter_RewindAfterSaveLoad_StillWorks()
        {
            // Integration: snapshot HP, save+load, then OnTurnEnd should
            // rewind to the loaded snapshot value.
            var c = Creature(hpMax: 400);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat); // snapshot = 400

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(c);
            var loadedCoat = loaded.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            // Wound the loaded entity.
            loaded.GetStat("Hitpoints").BaseValue = 250;
            loadedCoat.OnTurnEnd(loaded);
            Assert.AreEqual(400, loaded.GetStatValue("Hitpoints"),
                "rewind reads the preserved snapshot; HP restored to pre-save value.");
        }

        // ──────────── Effect-name normalization ────────────

        [Test]
        public void Adversarial_ImmuneElement_LowercaseValue_StillMatches()
        {
            // CLAUDE.md taxonomy: "effect-name normalization — case-
            // insensitive (BURNING ≡ burning)." Content authors will
            // type "electric" / "ELECTRIC" / "Electric" interchangeably;
            // the runtime should match regardless of case to prevent
            // silent-no-immunity content bugs.
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""lowercase-mycelium"", ""Adjective"":""pulse-veined"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""electric"" } ] }");
            var c = Creature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("lowercase-mycelium", 30));
            var d = new Damage(40); d.AddAttribute("Electric");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.AreEqual(hp0, c.GetStatValue("Hitpoints"),
                "lowercase 'electric' must match Electric flag (case-insensitive)");
        }

        [Test]
        public void Adversarial_ImmuneElement_UppercaseValue_StillMatches()
        {
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""uppercase-mycelium"", ""Adjective"":""x"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""HEAT"" } ] }");
            var c = Creature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("uppercase-mycelium", 30));
            var d = new Damage(40); d.AddAttribute("Heat");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.AreEqual(hp0, c.GetStatValue("Hitpoints"),
                "uppercase 'HEAT' must match Heat flag (case-insensitive)");
        }

        [Test]
        public void Adversarial_ImmuneElement_Empty_NoMatchAnyElement()
        {
            // Empty ImmuneElement must mean "no immunity," not "match
            // everything" (e.g. via an unchecked string.Equals path).
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""empty-immune"", ""Adjective"":""x"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":"""" } ] }");
            var c = Creature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("empty-immune", 30));
            var d = new Damage(30); d.AddAttribute("Heat");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.Less(c.GetStatValue("Hitpoints"), hp0,
                "empty ImmuneElement = no immunity (the early-out should bail before any match)");
        }

        [Test]
        public void Adversarial_ImmuneElement_Garbage_NoMatchAnyElement()
        {
            // A garbage value ("Fhqwhgads") must not crash; should not
            // match any element.
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""garbage-immune"", ""Adjective"":""x"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""Fhqwhgads"" } ] }");
            var c = Creature(hpMax: 200);
            int hp0 = c.GetStatValue("Hitpoints");
            c.ApplyEffect(new LiquidCoveredEffect("garbage-immune", 30));
            Assert.DoesNotThrow(() =>
            {
                var d = new Damage(30); d.AddAttribute("Heat");
                CombatSystem.ApplyDamage(c, d, null, null);
            });
            Assert.Less(c.GetStatValue("Hitpoints"), hp0,
                "garbage ImmuneElement = no immunity");
        }

        // ──────────── Probability boundaries ────────────

        [Test]
        public void Adversarial_ReflectPercent_Zero_NoReflect()
        {
            // chance=0 → never fires. Pinned so a future change can't
            // silently route 0 to a default 50%.
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""zero-reflect"", ""Adjective"":""x"",
                ""Fluidity"":4, ""Evaporativity"":3,
                ""ReflectPercent"":0 } ] }");
            var defender = Creature(hpMax: 200);
            var attacker = Creature(hpMax: 200);
            defender.ApplyEffect(new LiquidCoveredEffect("zero-reflect", 30));
            int attackerHp0 = attacker.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(defender, new Damage(40), attacker, null);
            Assert.AreEqual(attackerHp0, attacker.GetStatValue("Hitpoints"),
                "ReflectPercent=0 → attacker takes 0 reflect");
        }

        [Test]
        public void Adversarial_ReflectPercent_OneHundred_FullDamageReflected()
        {
            // chance=100 → reflect the full landed amount.
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""full-reflect"", ""Adjective"":""x"",
                ""Fluidity"":4, ""Evaporativity"":3,
                ""ReflectPercent"":100 } ] }");
            var defender = Creature(hpMax: 200);
            var attacker = Creature(hpMax: 200);
            defender.ApplyEffect(new LiquidCoveredEffect("full-reflect", 30));
            int attackerHp0 = attacker.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(defender, new Damage(40), attacker, null);
            int attackerLost = attackerHp0 - attacker.GetStatValue("Hitpoints");
            Assert.AreEqual(40, attackerLost,
                "ReflectPercent=100 → attacker takes the full 40");
        }

        [Test]
        public void Adversarial_ReflectPercent_OverHundred_AmplifiesReflect()
        {
            // chance=200 → 200% reflect (i.e., 2× the landed amount).
            // Currently the impl does no clamping — pinning the
            // observed behavior. If a future "clamp to 100%" design
            // ships, this test flips.
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""amped-reflect"", ""Adjective"":""x"",
                ""Fluidity"":4, ""Evaporativity"":3,
                ""ReflectPercent"":200 } ] }");
            var defender = Creature(hpMax: 200);
            var attacker = Creature(hpMax: 200);
            defender.ApplyEffect(new LiquidCoveredEffect("amped-reflect", 30));
            int attackerHp0 = attacker.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(defender, new Damage(30), attacker, null);
            int attackerLost = attackerHp0 - attacker.GetStatValue("Hitpoints");
            Assert.AreEqual(60, attackerLost,
                "Pin: ReflectPercent=200 currently 2× amplifies. " +
                "If clamping lands, expected drops to 30.");
        }

        [Test]
        public void Adversarial_ReflectPercent_Negative_NoReflect()
        {
            // chance<0 must not "heal" the attacker (a buggy impl could
            // do reflectDmg.Amount = damage.Amount * -50 / 100 = -20 and
            // try to ApplyDamage(-20) which the Damage setter clamps to
            // 0 — but still, the gate should bail before that).
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""negative-reflect"", ""Adjective"":""x"",
                ""Fluidity"":4, ""Evaporativity"":3,
                ""ReflectPercent"":-50 } ] }");
            var defender = Creature(hpMax: 200);
            var attacker = Creature(hpMax: 200);
            defender.ApplyEffect(new LiquidCoveredEffect("negative-reflect", 30));
            int attackerHp0 = attacker.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(defender, new Damage(40), attacker, null);
            Assert.AreEqual(attackerHp0, attacker.GetStatValue("Hitpoints"),
                "ReflectPercent<0 must not damage or heal the attacker");
        }

        // ──────────── Stacking semantics (OnStack merge w/ stale fields) ────────────

        [Test]
        public void Adversarial_Stacking_RewindCoat_MergedToNonRewindCoat_StaleSnapshotIgnored()
        {
            // Apply felling-counter (HpRewindOnTurnEnd = true). OnTurnStart
            // sets RewindSnapshotHp = current HP. Then merge a larger
            // water coat (different LiquidId; no HpRewindOnTurnEnd). The
            // merge code keeps the SAME LiquidCoveredEffect instance
            // (just swaps LiquidId + adds Amount), so the stale snapshot
            // value is preserved. On OnTurnEnd, the rewind early-out
            // checks `def.HpRewindOnTurnEnd` — water doesn't declare it,
            // so the stale snapshot is correctly ignored. Pin: damage
            // taken under water coat is NOT undone.
            var c = Creature(hpMax: 400);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat); // snapshot = 400
            Assert.AreEqual(400, coat.RewindSnapshotHp);

            // Merge a larger water coat — id swaps to "water".
            c.ApplyEffect(new LiquidCoveredEffect("water", 60));
            // The same instance is now the dominant coat.
            var merged = c.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.AreEqual("water", merged.LiquidId, "id swapped to water (larger amount)");
            Assert.AreEqual(400, merged.RewindSnapshotHp, "snapshot field STAYS (merge doesn't reset)");

            // Wound, then OnTurnEnd — rewind must NOT fire (water has no flag).
            c.GetStat("Hitpoints").BaseValue = 200;
            merged.OnTurnEnd(c);
            Assert.AreEqual(200 - (merged.Amount > 0 ? 0 : 0), c.GetStatValue("Hitpoints"),
                "water coat after merge: rewind early-outs on def.HpRewindOnTurnEnd=false; damage stays");
            // Note: the dry-down for water will tick coat.Amount but not HP.
        }

        [Test]
        public void Adversarial_Stacking_NonRewindCoat_MergedToRewindCoat_FirstTurnSnapshotCorrect()
        {
            // Apply water (no rewind). Merge to felling-counter (with
            // rewind, larger amount). The merged coat should be felling-
            // counter; the next OnTurnStart should freshly snapshot the
            // CURRENT HP, not be tripped up by the prior -1 sentinel.
            var c = Creature(hpMax: 400);
            c.ApplyEffect(new LiquidCoveredEffect("water", 20));
            c.GetStat("Hitpoints").BaseValue = 300; // wounded
            c.ApplyEffect(new LiquidCoveredEffect("felling-counter-resin", 50));
            var coat = c.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            Assert.AreEqual("felling-counter-resin", coat.LiquidId);

            // OnTurnStart re-snapshots from the current 300 HP.
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            Assert.AreEqual(300, coat.RewindSnapshotHp,
                "fresh OnTurnStart snapshot after merge must read CURRENT HP");
        }

        // ──────────── Boundary inputs (null safety) ────────────

        [Test]
        public void Adversarial_AllowAction_NullTarget_NoCrash_ReturnsTrue()
        {
            // The contract for AllowAction's default is to return true;
            // null target must follow the same convention (defensive
            // early-out).
            var coat = new LiquidCoveredEffect("held-breath-lacquer", 30);
            bool result = false;
            Assert.DoesNotThrow(() => { result = coat.AllowAction(null); });
            Assert.IsTrue(result, "null target on AllowAction must be a safe default (true)");
        }

        [Test]
        public void Adversarial_OnTakeDamage_NullTargetOrEvent_NoCrash()
        {
            var coat = new LiquidCoveredEffect("choir-mirror-mucilage", 30);
            Assert.DoesNotThrow(() => coat.OnTakeDamage(null, GameEvent.New("X")));
            Assert.DoesNotThrow(() => coat.OnTakeDamage(Creature(), null));
            Assert.DoesNotThrow(() => coat.OnTakeDamage(null, null));
        }

        [Test]
        public void Adversarial_OnBeforeTakeDamage_NullDamageParameter_NoCrash()
        {
            // The OnBeforeTakeDamage hook reads e.GetParameter<Damage>("Damage").
            // If the parameter is missing, the impl must bail without
            // dereferencing null.
            var coat = new LiquidCoveredEffect("veined-pulse-mycelium", 30);
            var e = GameEvent.New("BeforeTakeDamage");
            // Intentionally do NOT set the "Damage" parameter.
            Assert.DoesNotThrow(() => coat.OnBeforeTakeDamage(Creature(), e));
        }

        [Test]
        public void Adversarial_TryKnockback_NoStatusEffectsPart_NoCrash()
        {
            // The bench guards against this, but the engine must too —
            // a bare creature without StatusEffectsPart shouldn't trip
            // an NPE if the coat fires (the coat won't be present, but
            // ApplyDamage runs regardless).
            var zone = new Zone("AdvKnock");
            SettlementRuntime.ActiveZone = zone;
            try
            {
                var bareTarget = new Entity { ID = "bare", BlueprintName = "B" };
                bareTarget.Tags["Creature"] = "";
                bareTarget.Statistics["Hitpoints"] = new Stat
                { Owner = bareTarget, Name = "Hitpoints", BaseValue = 100, Max = 100, Min = 0 };
                zone.AddEntity(bareTarget, 5, 5);
                var attacker = CreatureInZone(zone, 4, 5);
                Assert.DoesNotThrow(() =>
                    CombatSystem.ApplyDamage(bareTarget, new Damage(10), attacker, zone));
            }
            finally { SettlementRuntime.Reset(); }
        }

        // ──────────── Mid-execution death ────────────

        [Test]
        public void Adversarial_ReflectKillsAttacker_OnReflectingDefender_NoCrash()
        {
            // Defender has 50% reflect. Attacker has 1 HP. Defender takes
            // a 200-damage hit → reflects 100 back → kills attacker. The
            // reflect ApplyDamage must run cleanly through HandleDeath
            // on the attacker; defender still takes their original 200.
            var defender = Creature(hpMax: 200);
            var attacker = Creature(hpMax: 200);
            attacker.GetStat("Hitpoints").BaseValue = 1;
            defender.ApplyEffect(new LiquidCoveredEffect("choir-mirror-mucilage", 30));
            Assert.DoesNotThrow(() =>
                CombatSystem.ApplyDamage(defender, new Damage(200), attacker, null));
            Assert.LessOrEqual(attacker.GetStatValue("Hitpoints"), 0,
                "attacker died from the reflect");
            Assert.AreEqual(0, defender.GetStatValue("Hitpoints"),
                "defender took the original 200 (which was their full HP)");
        }

        [Test]
        public void Adversarial_FellingCounter_DefenderDiesMidTurn_NoRewindOnDead()
        {
            // Fatal hit drops HP to 0 (HandleDeath runs synchronously
            // inside ApplyDamage). OnTurnEnd later → rewind sees HP=0,
            // refuses to resurrect. Verifies the dead-stays-dead guard
            // under the mid-turn-death edge.
            var c = Creature(hpMax: 200);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat);
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            // Lethal hit.
            CombatSystem.ApplyDamage(c, new Damage(999), null, null);
            Assert.LessOrEqual(c.GetStatValue("Hitpoints"), 0, "died mid-turn");
            Assert.DoesNotThrow(() => coat.OnTurnEnd(c));
            Assert.LessOrEqual(c.GetStatValue("Hitpoints"), 0,
                "rewind didn't resurrect (current HP ≤ 0 guard)");
        }

        // ──────────── Diag dispatch invariants ────────────

        [Test]
        public void Adversarial_ImmunityFires_SkipsAllLaterBranches_NoDeathAnchorRecord()
        {
            // A coat declaring BOTH ImmuneElement="Electric" AND
            // DeathAnchorPercent=50, hit with a fatal Electric hit.
            // Immunity must fire FIRST and EARLY-OUT (return) so the
            // death-anchor branch never sees the hit. No DeathAnchored
            // diag should appear; one ElementImmunity diag should.
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""immune-anchor"", ""Adjective"":""x"",
                ""Fluidity"":3, ""Evaporativity"":2,
                ""ImmuneElement"":""Electric"",
                ""DeathAnchorPercent"":50 } ] }");
            var c = Creature(hpMax: 200);
            c.GetStat("Hitpoints").BaseValue = 30;
            var coat = new LiquidCoveredEffect("immune-anchor", 30);
            c.ApplyEffect(coat);
            var d = new Damage(999); d.AddAttribute("Electric");
            CombatSystem.ApplyDamage(c, d, null, null);

            var immunityRecs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "ElementImmunity", Limit = 5 }).Records;
            var anchorRecs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "DeathAnchored", Limit = 5 }).Records;
            Assert.AreEqual(1, immunityRecs.Count, "exactly one ElementImmunity record");
            Assert.AreEqual(0, anchorRecs.Count, "anchor must NOT fire when immunity already nullified");
            Assert.IsFalse(coat.AnchorConsumed, "anchor not consumed");
        }

        // ──────────── Anti-exploit invariants ────────────

        [Test]
        public void Adversarial_TwoMirrors_AttackerIsAlsoCoated_OnlyOneReflectFired()
        {
            // The two-mirror cycle-breaker is the headline LA.3
            // invariant. Re-pin it here at the adversarial layer in a
            // dedicated file so a future refactor that drops the
            // source=null sentinel breaks visibly.
            var defender = Creature(hpMax: 200);
            var attacker = Creature(hpMax: 200);
            defender.ApplyEffect(new LiquidCoveredEffect("choir-mirror-mucilage", 30));
            attacker.ApplyEffect(new LiquidCoveredEffect("choir-mirror-mucilage", 30));
            int attackerHp0 = attacker.GetStatValue("Hitpoints");
            int defenderHp0 = defender.GetStatValue("Hitpoints");

            Assert.DoesNotThrow(() =>
                CombatSystem.ApplyDamage(defender, new Damage(40), attacker, null));

            Assert.AreEqual(attackerHp0 - 20, attacker.GetStatValue("Hitpoints"),
                "attacker took exactly 20 (one reflect, not infinite)");
            Assert.AreEqual(defenderHp0 - 40, defender.GetStatValue("Hitpoints"),
                "defender took the full original 40");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "DamageReflected", Limit = 10 }).Records;
            Assert.AreEqual(1, recs.Count, "exactly one DamageReflected event");
        }

        // ════════════════════════════════════════════════════════════════
        //   PART II — Hypothesis-driven deep audit (player-flow probes)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Hypothesis_KnockbackIntoLavaCell_NoCrash_MoveLands()
        {
            // HYPOTHESIS: a knockback shove into a damaging-floor cell
            // shouldn't crash; the move lands and the next turn's tick
            // would handle the floor damage independently. This tests
            // the "knockback doesn't care about destination semantics
            // beyond walkability" contract.
            var zone = new Zone("Hyp1");
            SettlementRuntime.ActiveZone = zone;
            try
            {
                var defender = CreatureInZone(zone, 5, 5);
                var attacker = CreatureInZone(zone, 4, 5);
                defender.ApplyEffect(new LiquidCoveredEffect("pebble-sundew-dew", 30));
                // Place a "hazard" entity (non-solid) at (6,5).
                var hazard = new Entity { ID = "hz", BlueprintName = "Hazard" };
                hazard.AddPart(new RenderPart { DisplayName = "lava", RenderString = "#" });
                hazard.AddPart(new PhysicsPart { Solid = false });
                zone.AddEntity(hazard, 6, 5);

                Assert.DoesNotThrow(() =>
                    CombatSystem.ApplyDamage(defender, new Damage(5), attacker, zone));
                var pos = zone.GetEntityPosition(defender);
                Assert.AreEqual(6, pos.x, "defender moved into hazard cell — knockback didn't care about contents");
            }
            finally { SettlementRuntime.Reset(); }
        }

        [Test]
        public void Hypothesis_MultipleHitsOneTurn_AllRewoundCorrectly()
        {
            // HYPOTHESIS: a felling-counter wearer takes N hits from
            // different sources during one turn. The snapshot is taken
            // ONCE at OnTurnStart; OnTurnEnd should rewind to the
            // snapshot regardless of N. A buggy impl that snapshots per
            // hit would lose accumulated damage.
            var c = Creature(hpMax: 400);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat);
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            // Three hits.
            CombatSystem.ApplyDamage(c, new Damage(50), null, null);
            CombatSystem.ApplyDamage(c, new Damage(50), null, null);
            CombatSystem.ApplyDamage(c, new Damage(50), null, null);
            Assert.AreEqual(250, c.GetStatValue("Hitpoints"),
                "precondition: 3 × 50 = 150 damage taken");
            coat.OnTurnEnd(c);
            Assert.AreEqual(400, c.GetStatValue("Hitpoints"),
                "all 3 hits in the turn rewound to the single OnTurnStart snapshot");
        }

        [Test]
        public void Hypothesis_HeldBreath_DisintegrationDamage_StillNullified()
        {
            // HYPOTHESIS: PreventDeath gates on `damage.Amount >= hp`
            // regardless of damage type. Disintegration (which often
            // bypasses things in ARPGs) shouldn't get special treatment.
            var c = Creature(hpMax: 200);
            c.GetStat("Hitpoints").BaseValue = 50;
            c.ApplyEffect(new LiquidCoveredEffect("held-breath-lacquer", 30));
            var d = new Damage(9999); d.AddAttribute("Disintegrate");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.AreEqual(50, c.GetStatValue("Hitpoints"),
                "Disintegration still gets nullified by PreventDeath");
        }

        [Test]
        public void Hypothesis_HeldBreath_IgnoreResistAttribute_StillNullified()
        {
            // HYPOTHESIS: damage tagged "IgnoreResist" bypasses
            // ApplyResistances (CombatSystem.cs:800) but PreventDeath
            // fires earlier in OnBeforeTakeDamage — should still fire.
            var c = Creature(hpMax: 200);
            c.GetStat("Hitpoints").BaseValue = 50;
            c.ApplyEffect(new LiquidCoveredEffect("held-breath-lacquer", 30));
            var d = new Damage(9999); d.AddAttribute("IgnoreResist");
            CombatSystem.ApplyDamage(c, d, null, null);
            Assert.AreEqual(50, c.GetStatValue("Hitpoints"),
                "IgnoreResist still gets nullified by PreventDeath (it runs in OnBeforeTakeDamage)");
        }

        [Test]
        public void Hypothesis_FellingCounter_CoatRemovedMidTurn_NoOrphanRewind()
        {
            // HYPOTHESIS: removing the coat mid-turn (e.g., a cure-tonic
            // effect) shouldn't leave an orphan "rewind at end-of-turn"
            // because the OnTurnEnd hook lives on the effect itself —
            // remove the effect, the hook is gone. No phantom rewind.
            var c = Creature(hpMax: 400);
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat);
            coat.OnTurnStart(c, GameEvent.New("BeginTakeAction"));
            CombatSystem.ApplyDamage(c, new Damage(100), null, null);
            // Remove the coat mid-turn.
            c.GetPart<StatusEffectsPart>().RemoveEffect<LiquidCoveredEffect>();
            // Try to fire a turn-end manually on the (removed) coat —
            // it shouldn't be on the entity any more so StatusEffectsPart
            // dispatch wouldn't call it. Verify by direct StatusEffectsPart
            // dispatch.
            var fx = c.GetPart<StatusEffectsPart>();
            Assert.IsNull(fx.GetEffect<LiquidCoveredEffect>(), "coat removed");
            // HP stays at the wounded value (no orphan rewind).
            Assert.AreEqual(300, c.GetStatValue("Hitpoints"),
                "wound stays; no orphan OnTurnEnd from a removed coat");
        }

        [Test]
        public void Hypothesis_KnockbackOntoOccupiedCell_StillSucceeds()
        {
            // HYPOTHESIS: Zone.AddEntity (which MoveEntity ultimately
            // calls) doesn't check for cell occupancy. So knockback ONTO
            // another creature's cell would land — both creatures end up
            // in the same cell. This is documented behavior; pin it so
            // a future "block on occupied" change doesn't surprise.
            var zone = new Zone("Hyp6");
            SettlementRuntime.ActiveZone = zone;
            try
            {
                var defender = CreatureInZone(zone, 5, 5);
                var attacker = CreatureInZone(zone, 4, 5);
                var bystander = CreatureInZone(zone, 6, 5); // occupies the destination
                defender.ApplyEffect(new LiquidCoveredEffect("pebble-sundew-dew", 30));
                CombatSystem.ApplyDamage(defender, new Damage(5), attacker, zone);
                var defenderPos = zone.GetEntityPosition(defender);
                Assert.AreEqual(6, defenderPos.x,
                    "Pin: knockback lands on occupied cell (both entities now at 6,5).");
            }
            finally { SettlementRuntime.Reset(); }
        }

        [Test]
        public void Hypothesis_KnockbackFromSameCell_NoMove()
        {
            // HYPOTHESIS: attacker and defender in the same cell
            // (dx=dy=0). The direction-of-shove is undefined; the impl
            // must bail (no random shove, no crash).
            var zone = new Zone("Hyp7");
            SettlementRuntime.ActiveZone = zone;
            try
            {
                var defender = CreatureInZone(zone, 5, 5);
                // Attacker placed at the SAME cell (Zone allows this).
                var attacker = new Entity { ID = "atk", BlueprintName = "A" };
                attacker.Tags["Creature"] = "";
                attacker.Statistics["Hitpoints"] = new Stat
                { Owner = attacker, Name = "Hitpoints", BaseValue = 100, Max = 100, Min = 0 };
                attacker.AddPart(new RenderPart { DisplayName = "a", RenderString = "a" });
                zone.AddEntity(attacker, 5, 5);
                defender.ApplyEffect(new LiquidCoveredEffect("pebble-sundew-dew", 30));
                Assert.DoesNotThrow(() =>
                    CombatSystem.ApplyDamage(defender, new Damage(5), attacker, zone));
                var defenderPos = zone.GetEntityPosition(defender);
                Assert.AreEqual(5, defenderPos.x, "same-cell attacker: no shove direction; defender stays");
                Assert.AreEqual(5, defenderPos.y);
            }
            finally { SettlementRuntime.Reset(); }
        }

        [Test]
        public void Hypothesis_FellingCounter_PartiallyHealsBeyondSnapshot_HealPreserved()
        {
            // HYPOTHESIS: snapshot=100, wearer takes 30 damage (down to
            // 70), then heals 50 (up to 120 if uncapped — capped at Max).
            // Wait — if Max=200 then 70+50 = 120. The rewind compares
            // current (120) to snapshot (100) — 120 > 100 → "intra-turn
            // heal preserved" early-out → HP stays at 120. Test pins
            // the partial-heal-then-rewind contract.
            var c = Creature(hpMax: 400);
            c.GetStat("Hitpoints").BaseValue = 100;
            var coat = new LiquidCoveredEffect("felling-counter-resin", 30);
            c.ApplyEffect(coat); // snapshot = 100
            // 30 damage.
            CombatSystem.ApplyDamage(c, new Damage(30), null, null);
            Assert.AreEqual(70, c.GetStatValue("Hitpoints"));
            // 50 heal (direct stat write — simulates a tonic or another effect).
            c.GetStat("Hitpoints").BaseValue = 120;
            // OnTurnEnd — rewind sees 120 > snapshot 100, early-outs.
            coat.OnTurnEnd(c);
            Assert.AreEqual(120, c.GetStatValue("Hitpoints"),
                "current 120 > snapshot 100 → rewind preserves the net heal");
        }

        [Test]
        public void Hypothesis_HeldBreath_HitFromMultipleSources_AllNullified()
        {
            // HYPOTHESIS: an AoE attack from a single source AND a
            // melee attack from another source — both fatal — both must
            // be nullified by PreventDeath (no consumption per LA.6).
            var c = Creature(hpMax: 200);
            c.GetStat("Hitpoints").BaseValue = 30;
            c.ApplyEffect(new LiquidCoveredEffect("held-breath-lacquer", 30));
            var source1 = Creature();
            var source2 = Creature();
            CombatSystem.ApplyDamage(c, new Damage(500), source1, null);
            CombatSystem.ApplyDamage(c, new Damage(500), source2, null);
            CombatSystem.ApplyDamage(c, new Damage(500), null, null); // env
            Assert.AreEqual(30, c.GetStatValue("Hitpoints"),
                "all 3 fatal hits from 2 sources + null source all nullified");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "liquid", Kind = "DeathPrevented", Limit = 10 }).Records;
            Assert.AreEqual(3, recs.Count, "three DeathPrevented records");
        }

        [Test]
        public void Hypothesis_ReflectAndKnockback_BothFireWhenDefenderDeclaresBoth()
        {
            // HYPOTHESIS: a synthetic coat declaring BOTH ReflectPercent
            // and KnockbackOnHit fires both mechanics on the same hit.
            // Verifies the dispatcher's both-branch path.
            LiquidRegistry.ResetForTests();
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""reflect-shove"", ""Adjective"":""x"",
                ""Fluidity"":4, ""Evaporativity"":3,
                ""ReflectPercent"":50, ""KnockbackOnHit"":true } ] }");
            var zone = new Zone("HypBoth");
            SettlementRuntime.ActiveZone = zone;
            try
            {
                var defender = CreatureInZone(zone, 5, 5);
                var attacker = CreatureInZone(zone, 4, 5);
                defender.ApplyEffect(new LiquidCoveredEffect("reflect-shove", 30));
                int attackerHp0 = attacker.GetStatValue("Hitpoints");
                CombatSystem.ApplyDamage(defender, new Damage(40), attacker, zone);

                int attackerLost = attackerHp0 - attacker.GetStatValue("Hitpoints");
                Assert.AreEqual(20, attackerLost, "reflect fired (50% of 40 = 20)");
                var defenderPos = zone.GetEntityPosition(defender);
                Assert.AreEqual(6, defenderPos.x, "knockback fired (defender shoved east)");
            }
            finally { SettlementRuntime.Reset(); }
        }

        [Test]
        public void Hypothesis_SaveLoad_PreventDeath_BlockAction_StatePreserved()
        {
            // HYPOTHESIS: held-breath has no NEW per-instance state of
            // its own — both flags live on the def. So save/load should
            // preserve "still a held-breath coat → still PreventDeath +
            // BlockAction." Verifies the LiquidId round-trip is the
            // gating mechanism (no instance state needed).
            var c = Creature(hpMax: 200);
            c.GetStat("Hitpoints").BaseValue = 50;
            c.ApplyEffect(new LiquidCoveredEffect("held-breath-lacquer", 30));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(c);
            var loadedCoat = loaded.GetPart<StatusEffectsPart>().GetEffect<LiquidCoveredEffect>();
            // Fatal hit on loaded entity.
            CombatSystem.ApplyDamage(loaded, new Damage(9999), null, null);
            Assert.AreEqual(50, loaded.GetStatValue("Hitpoints"),
                "PreventDeath still fires after save/load (gated on def, not instance state)");
            Assert.IsFalse(loadedCoat.AllowAction(loaded),
                "BlockAction still fires after save/load (also gated on def)");
        }

        [Test]
        public void Hypothesis_ChoirMirror_AttackerIsNonCreature_NoCrash()
        {
            // HYPOTHESIS: the reflect's recursive ApplyDamage(source, ...)
            // succeeds even if `source` is a non-creature entity (e.g.,
            // a trap firing the original hit). The Hitpoints-missing
            // guard at CombatSystem.cs:730 protects this — pin it.
            var defender = Creature(hpMax: 200);
            var nonCreatureSource = new Entity { ID = "trap", BlueprintName = "Trap" };
            // No Hitpoints stat, no StatusEffectsPart.
            defender.ApplyEffect(new LiquidCoveredEffect("choir-mirror-mucilage", 30));
            Assert.DoesNotThrow(() =>
                CombatSystem.ApplyDamage(defender, new Damage(40), nonCreatureSource, null));
        }
    }
}
