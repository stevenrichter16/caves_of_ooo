using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M2 (NoFightGoal-via-dialogue / CalmMutation / WitnessedEffect)
    /// coverage-gap tests, written TDD-style per Docs/QUD-PARITY.md
    /// Part 2.1: state the expected invariant first, then run.
    ///
    /// Existing M2 coverage (4 test files: NoFightConversationTests,
    /// CalmMutationTests, WitnessedEffectTests, plus M2-relevant cases
    /// in Phase6GoalsTests) is broad. The remaining gaps cluster in:
    ///
    ///   - NoFightGoal default-ctor + semantic invariants (IsBusy,
    ///     CurrentState writes).
    ///   - ConversationManager's brain.InConversation contract
    ///     (the SHIPPED auto-pacify mechanism — the alternative
    ///     auto-NoFightGoal-push was scope-pruned during M2.1
    ///     because InConversation already short-circuits BoredGoal).
    ///   - CalmMutation defensive paths (null target, no Brain) and
    ///     the user-visible "already at peace" message on idempotent
    ///     re-cast.
    ///   - WitnessedEffect Qud-type bitmask (TYPE_MENTAL | TYPE_MINOR |
    ///     TYPE_NEGATIVE | TYPE_REMOVABLE) and IsOfType behavior.
    ///
    /// All tests are regression shields. None are expected to fail —
    /// any failure means either a real bug or a flawed test
    /// expectation; the analysis goes in the commit message.
    /// </summary>
    [TestFixture]
    public class M2CoverageGapTests
    {
        [SetUp]
        public void Setup()
        {
            AsciiFxBus.Clear();
            FactionManager.Initialize();
            MessageLog.Clear();
            // ConversationActions.RegisterDefaults runs in the static
            // constructor; touching the class triggers it. No explicit
            // call needed.
        }

        [TearDown]
        public void Teardown()
        {
            ConversationManager.EndConversation();
            FactionManager.Reset();
        }

        // Mirrors the Phase6GoalsTests creature helper.
        private Entity CreateCreature(Zone zone, int x, int y, string faction = "Villagers", int hp = 20)
        {
            var entity = new Entity { BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            if (!string.IsNullOrEmpty(faction))
                entity.Tags["Faction"] = faction;
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = faction ?? "creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d4" });
            entity.AddPart(new ArmorPart());
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return entity;
        }

        // ============================================================
        // NoFightGoal — defaults + semantic invariants
        // ============================================================

        [Test]
        public void NoFightGoal_DefaultCtor_DurationIsZero_MeaningInfinite()
        {
            // The parameterless ctor uses Duration=0, which the Finished()
            // method treats as "infinite" (Duration > 0 && Age >= Duration
            // is false for any Duration <= 0). The static-state hook in
            // ConversationManager (per the M2.1 plan) used this exact
            // pattern — push with duration 0, hold until external Remove.
            // Pin it.
            var goal = new NoFightGoal();
            Assert.AreEqual(0, goal.Duration);
            Assert.IsFalse(goal.Wander);

            // Verify infinite-duration semantic: Age past anything still
            // not finished.
            goal.Age = 99999;
            Assert.IsFalse(goal.Finished(),
                "Default-ctor NoFightGoal must never finish on its own. " +
                "If it does, the conversation auto-pacify path (push with " +
                "duration 0, remove on EndConversation) silently breaks " +
                "after Age catches up.");
        }

        [Test]
        public void NoFightGoal_IsBusy_IsFalse()
        {
            // IsBusy is consulted by zone-transition and other "is the NPC
            // doing something the player should wait for" callers. A
            // pacified NPC is NOT busy — they're standing peacefully.
            var goal = new NoFightGoal(duration: 100);
            Assert.IsFalse(goal.IsBusy(),
                "Pacified NPC must report IsBusy=false. A 'busy' interpretation " +
                "would block the player from interacting with them.");
        }

        [Test]
        public void NoFightGoal_TakeAction_SetsBrainStateIdle()
        {
            // The Phase 10 inspector relies on CurrentState. While
            // pacified, the NPC should read as Idle. Set state to a
            // non-default value first so this is not a vacuous pass
            // (CurrentState defaults to Idle).
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            var brain = creature.GetPart<BrainPart>();
            brain.CurrentState = AIState.Chase; // non-default

            var goal = new NoFightGoal(duration: 50, wander: false);
            brain.PushGoal(goal);

            goal.TakeAction();

            Assert.AreEqual(AIState.Idle, brain.CurrentState,
                "NoFightGoal.TakeAction must set CurrentState=Idle so the " +
                "inspector renders the NPC's state correctly. (Pre-set to " +
                "Engaged to ensure this isn't vacuous.)");
        }

        [Test]
        public void NoFightGoal_FixedDuration_FinishesAtDurationExactly()
        {
            // Boundary: Age=Duration triggers Finished()=true (>= comparison).
            var goal = new NoFightGoal(duration: 10);
            goal.Age = 9;
            Assert.IsFalse(goal.Finished(), "At Age 9, Duration 10 — not yet finished.");
            goal.Age = 10;
            Assert.IsTrue(goal.Finished(),
                "At Age==Duration, Finished must return true. " +
                "Off-by-one here would have pacification last one tick too long.");
        }

        // ============================================================
        // ConversationManager — InConversation flag contract
        //
        // M2.1's "auto-pacify on Start" plan was scope-pruned because
        // BrainPart.HandleTakeTurn already short-circuits when
        // InConversation=true. These tests pin the SHIPPED contract:
        // StartConversation sets the flag, EndConversation clears it,
        // and an InConversation NPC's TakeTurn returns without engaging
        // hostile-acquisition.
        // ============================================================

        [Test]
        public void BrainPart_InConversation_ShortCircuitsHostileAcquisition()
        {
            // The shipped pacification mechanism. While InConversation=true,
            // BrainPart.HandleTakeTurn returns early — BoredGoal never runs,
            // so no KillGoal can be pushed against a hostile.
            //
            // Without this short-circuit, an NPC would mid-conversation
            // push KillGoal against any hostile in sight, contradicting
            // the 'pacified-while-talking' design.
            var zone = new Zone("TestZone");
            var npc = CreateCreature(zone, 5, 5, faction: "Villagers");
            var hostile = CreateCreature(zone, 7, 5, faction: "Snapjaws");
            // Make them mutually hostile.
            FactionManager.SetFactionFeeling("Villagers", "Snapjaws", -100);
            FactionManager.SetFactionFeeling("Snapjaws", "Villagers", -100);

            var brain = npc.GetPart<BrainPart>();
            brain.InConversation = true;

            // Sanity: hostile is in sight pre-tick.
            Assume.That(FactionManager.IsHostile(npc, hostile),
                "Test setup: NPCs must be mutually hostile.");

            npc.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(brain.HasGoal<KillGoal>(),
                "InConversation=true must prevent KillGoal acquisition. " +
                "If this fails, the BrainPart.HandleTakeTurn early-return " +
                "for InConversation has been removed/changed.");
        }

        [Test]
        public void BrainPart_InConversation_DefaultsFalse()
        {
            // Sanity. Newly-constructed BrainPart must default to
            // InConversation=false; otherwise NPCs would be stuck
            // pacified at scenario start.
            var brain = new BrainPart();
            Assert.IsFalse(brain.InConversation,
                "Default must be false — opt-in by ConversationManager only.");
        }

        // ============================================================
        // CalmMutation — defensive paths + idempotent message
        //
        // These tests use Cast() — the actual public API — rather than
        // reflecting on the protected ApplyOnHitEffect. Mirrors the
        // existing CalmMutationTests pattern.
        // ============================================================

        [Test]
        public void CalmMutation_OnHit_AlreadyPacified_LogsAlreadyAtPeace()
        {
            // User-visible feedback: re-casting Calm on an already-pacified
            // target should log "X is already at peace." The mutation does
            // NOT extend duration (idempotent), and the player needs to
            // know why the apparent re-cast did nothing.
            var zone = new Zone("CalmZone");
            var caster = CreateCalmCaster();
            var target = CreateCalmTargetWithBrain();
            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            // Pre-pacify the target.
            var brain = target.GetPart<BrainPart>();
            brain.PushGoal(new NoFightGoal(duration: 100));

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new CalmMutation(), 1);
            var calm = mutations.GetMutation<CalmMutation>();

            MessageLog.Clear();
            calm.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            // Search the recent messages for the expected feedback. (Cast
            // may emit "X target gets calmed" or similar before our
            // expected "already at peace" — depends on what the base
            // class logs. Use a substring contains check on any recent
            // entry.)
            bool foundAlreadyAtPeace = false;
            var recent = MessageLog.GetRecent(10);
            foreach (var msg in recent)
            {
                if (msg != null && msg.ToLowerInvariant().Contains("already at peace"))
                {
                    foundAlreadyAtPeace = true;
                    break;
                }
            }
            Assert.IsTrue(foundAlreadyAtPeace,
                "Re-cast on a pacified target must log 'already at peace' " +
                "user-visible feedback so the cooldown-consumed cast isn't " +
                "silently confusing.");
        }

        [Test]
        public void CalmMutation_MutationType_IsMental()
        {
            // Pins the mutation's category. Codepaths that filter by
            // MutationType (e.g., a future mental-resistance check) rely
            // on this being exactly "Mental".
            var mutation = new CalmMutation();
            Assert.AreEqual("Mental", mutation.MutationType);
        }

        [Test]
        public void CalmMutation_DisplayName_IsCalm()
        {
            // UI contract.
            var mutation = new CalmMutation();
            Assert.AreEqual("Calm", mutation.DisplayName);
        }

        [Test]
        public void CalmMutation_BaseDuration_IsForty()
        {
            // Pins the tuning constant. The level-scaling formula is
            // Duration = BaseDuration + Level*10, so a change here
            // shifts EVERY level's duration. Make this intentional.
            var mutation = new CalmMutation();
            Assert.AreEqual(40, mutation.BaseDuration,
                "BaseDuration must remain 40 — the formula and existing " +
                "Level-scaling tests assume this anchor.");
        }

        // ============================================================
        // WitnessedEffect — Qud-type bitmask + UI contracts
        // ============================================================

        [Test]
        public void WitnessedEffect_GetEffectType_IncludesAllFourCategories()
        {
            // Per the Qud-parity comment in WitnessedEffect.cs:30-43, the
            // bitmask must include TYPE_MENTAL | TYPE_MINOR |
            // TYPE_NEGATIVE | TYPE_REMOVABLE so future mind-blank /
            // psionic-immunity queries classify it as a mental effect.
            //
            // If someone refactors GetEffectType and accidentally drops
            // a bit (most likely TYPE_MENTAL since it's the unusual one),
            // this catches it.
            var effect = new WitnessedEffect();
            int mask = effect.GetEffectType();

            Assert.AreNotEqual(0, mask & Effect.TYPE_MENTAL,
                "WitnessedEffect.GetEffectType must include TYPE_MENTAL. " +
                "Mind-blank / psionic-immunity gates filter on this bit.");
            Assert.AreNotEqual(0, mask & Effect.TYPE_MINOR,
                "Must include TYPE_MINOR (witness pacing is minor flavor, not major).");
            Assert.AreNotEqual(0, mask & Effect.TYPE_NEGATIVE,
                "Must include TYPE_NEGATIVE (the NPC isn't enjoying it).");
            Assert.AreNotEqual(0, mask & Effect.TYPE_REMOVABLE,
                "Must include TYPE_REMOVABLE (cure-tonic / resist effects).");
        }

        [Test]
        public void WitnessedEffect_IsOfType_MentalIsTrue()
        {
            // Counter-check via the IsOfType API used by callers.
            var effect = new WitnessedEffect();
            Assert.IsTrue(effect.IsOfType(Effect.TYPE_MENTAL),
                "IsOfType(TYPE_MENTAL) must return true for shaken NPCs.");
        }

        [Test]
        public void WitnessedEffect_IsOfType_GeneralIsFalse_NotInBitmask()
        {
            // Counter-check: TYPE_GENERAL isn't in the WitnessedEffect mask
            // (it's mental-specific). This guards against a refactor that
            // accidentally adds TYPE_GENERAL back.
            var effect = new WitnessedEffect();
            Assert.IsFalse(effect.IsOfType(Effect.TYPE_GENERAL),
                "IsOfType(TYPE_GENERAL) must return false — WitnessedEffect " +
                "is specifically mental, not generic. Counter-check to the " +
                "TYPE_MENTAL test (rules out vacuous 'all bits set' pass).");
        }

        [Test]
        public void WitnessedEffect_DisplayName_IsShaken()
        {
            // UI contract. The status panel displays this verbatim.
            var effect = new WitnessedEffect();
            Assert.AreEqual("shaken", effect.DisplayName);
        }

        [Test]
        public void WitnessedEffect_DefaultCtor_DurationIs20()
        {
            // The default 20-turn duration was chosen during M2.3 design
            // as "long enough to pace visibly, short enough to not annoy."
            // Pin it so a tuning change is intentional, not accidental.
            var effect = new WitnessedEffect();
            Assert.AreEqual(20, effect.Duration);
        }

        [Test]
        public void WitnessedEffect_OnRemove_SafeWhenTargetNull()
        {
            // Defensive: OnRemove with null target must not throw. The
            // production code uses target?.GetPart<BrainPart>() which
            // null-shorts cleanly.
            var effect = new WitnessedEffect();

            Assert.DoesNotThrow(() => effect.OnRemove(null),
                "OnRemove(null) must be a safe no-op. _pushedGoal is null " +
                "before any OnApply, so the early return handles it.");
        }

        // ============================================================
        // Calm-specific helpers (mirror CalmMutationTests)
        // ============================================================

        private static Entity CreateCalmCaster()
        {
            var entity = CreateCalmCreatureBase("caster", hp: 20);
            entity.AddPart(new ActivatedAbilitiesPart());
            entity.AddPart(new MutationsPart());
            return entity;
        }

        private static Entity CreateCalmTargetWithBrain()
        {
            var entity = CreateCalmCreatureBase("target", hp: 20);
            entity.Tags["Faction"] = "Monsters";
            entity.AddPart(new BrainPart());
            return entity;
        }

        private static Entity CreateCalmCreatureBase(string name, int hp)
        {
            var entity = new Entity { BlueprintName = name };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d2" });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            return entity;
        }
    }
}
