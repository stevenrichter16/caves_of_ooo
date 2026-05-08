using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ST.3 — SkillsPart + BaseSkillPart runtime tests.
    ///
    /// Pins:
    /// <list type="bullet">
    ///   <item>AddSkill (instance overload) — attaches part, calls
    ///         lifecycle hook, populates SkillList.</item>
    ///   <item>AddSkill (string-class overload) — reflection lookup
    ///         resolves CavesOfOoo.Tests.* and CavesOfOoo.Skills.* names.</item>
    ///   <item>AddSkill rollback — if the lifecycle hook returns false,
    ///         the part is detached + SkillList unchanged.</item>
    ///   <item>AddSkill duplicate — returns false; doesn't double-attach.</item>
    ///   <item>RemoveSkill — detaches, calls hook, updates list.</item>
    ///   <item>HasSkill / GetSkill — query by class name.</item>
    ///   <item>OnAfterLoad — rebuilds SkillList from ParentEntity.Parts.</item>
    ///   <item>Diag plumbing — AddSkill emits skill/Added,
    ///         RemoveSkill emits skill/Removed (user-mandated for ST.3).</item>
    ///   <item>Counter-check — RemoveSkill of an unowned skill is a
    ///         clean no-op (plan-mandated).</item>
    /// </list>
    /// </summary>
    public class SkillsPartTests
    {
        // ── Test stubs ───────────────────────────────────────────────────

        /// <summary>
        /// Public + namespace-qualified so SkillsPart.CreateSkillByClassName's
        /// Type.GetType("CavesOfOoo.Tests.SkillsPartTests+TestStubSkill")
        /// resolves it. Using a nested class (with the '+' separator)
        /// keeps the stub local to this fixture without polluting the
        /// global type namespace.
        /// </summary>
        public class TestStubSkill : BaseSkillPart
        {
            public bool AddSkillCalled = false;
            public bool RemoveSkillCalled = false;
            public override bool AddSkill(Entity entity)    { AddSkillCalled = true;    return true; }
            public override bool RemoveSkill(Entity entity) { RemoveSkillCalled = true; return true; }
        }

        public class TestFailingStubSkill : BaseSkillPart
        {
            public override bool AddSkill(Entity entity) => false;
        }

        public class TestStubSkillAlt : BaseSkillPart { }

        // ── Setup ────────────────────────────────────────────────────────

        [SetUp]
        public void Setup()
        {
            Diag.ResetAll();
            SkillRegistry.ResetForTests();
        }

        private static (Entity entity, SkillsPart skills) MakeActorWithSkills()
        {
            var entity = new Entity { ID = "actor", BlueprintName = "TestActor" };
            entity.AddPart(new RenderPart { DisplayName = "actor" });
            var skills = new SkillsPart();
            entity.AddPart(skills);
            return (entity, skills);
        }

        // ====================================================================
        // 1. AddSkill (instance) — positive
        // ====================================================================

        [Test]
        public void AddSkill_InstanceOverload_AttachesAndCallsHook()
        {
            var (entity, skills) = MakeActorWithSkills();
            var stub = new TestStubSkill();

            bool added = skills.AddSkill(stub);

            Assert.IsTrue(added, "AddSkill should return true on success.");
            Assert.AreEqual(1, skills.SkillList.Count, "SkillList should contain the new skill.");
            Assert.AreSame(stub, skills.SkillList[0], "SkillList[0] should be the added stub.");
            Assert.AreSame(entity, stub.ParentEntity, "Stub's ParentEntity should be set by AddPart.");
            Assert.IsTrue(stub.AddSkillCalled, "BaseSkillPart.AddSkill hook should have been called.");
            Assert.IsTrue(entity.Parts.Contains(stub), "Stub should be attached to entity.Parts.");
        }

        // ====================================================================
        // 2. AddSkill (string-class) — reflection lookup resolves
        // ====================================================================

        [Test]
        public void AddSkill_StringClass_ResolvesViaReflectionAndAttaches()
        {
            var (entity, skills) = MakeActorWithSkills();

            // Fully-qualified path includes the nested-class '+' separator.
            string fqClassName = typeof(TestStubSkill).FullName;
            bool added = skills.AddSkill(fqClassName);

            Assert.IsTrue(added, $"AddSkill('{fqClassName}') should resolve + add.");
            Assert.AreEqual(1, skills.SkillList.Count);
            Assert.IsInstanceOf<TestStubSkill>(skills.SkillList[0]);
        }

        // ====================================================================
        // 3. AddSkill (string-class) — unknown class returns false (counter-check)
        // ====================================================================

        [Test]
        public void AddSkill_StringClass_UnknownClass_ReturnsFalseCleanly()
        {
            var (entity, skills) = MakeActorWithSkills();

            // Unknown class name. Must not throw; must return false.
            bool added = skills.AddSkill("NotARealSkill_TotallyBogus");

            Assert.IsFalse(added, "AddSkill of a non-existent class should return false.");
            Assert.AreEqual(0, skills.SkillList.Count, "SkillList should be unchanged.");
        }

        // ====================================================================
        // 4. AddSkill duplicate — second add of same class returns false
        // ====================================================================

        [Test]
        public void AddSkill_Duplicate_ReturnsFalseAndDoesNotDoubleAttach()
        {
            var (entity, skills) = MakeActorWithSkills();
            skills.AddSkill(new TestStubSkill());

            // Second add of same TYPE (different instance).
            bool secondAdd = skills.AddSkill(new TestStubSkill());

            Assert.IsFalse(secondAdd,
                "Adding a second skill of the same type should return false.");
            Assert.AreEqual(1, skills.SkillList.Count,
                "SkillList should still contain only the first instance.");
        }

        // ====================================================================
        // 5. AddSkill rollback — if hook returns false, part is detached
        // ====================================================================

        [Test]
        public void AddSkill_HookReturnsFalse_PartIsRolledBack()
        {
            var (entity, skills) = MakeActorWithSkills();
            var failing = new TestFailingStubSkill();

            bool added = skills.AddSkill(failing);

            Assert.IsFalse(added, "AddSkill should return false when hook fails.");
            Assert.AreEqual(0, skills.SkillList.Count,
                "SkillList must NOT contain a skill whose AddSkill hook failed.");
            Assert.IsFalse(entity.Parts.Contains(failing),
                "Failed-hook skill must be detached from entity.Parts (rollback).");
        }

        // ====================================================================
        // 6. RemoveSkill — positive
        // ====================================================================

        [Test]
        public void RemoveSkill_DetachesPartAndCallsHook()
        {
            var (entity, skills) = MakeActorWithSkills();
            var stub = new TestStubSkill();
            skills.AddSkill(stub);

            bool removed = skills.RemoveSkill(stub);

            Assert.IsTrue(removed);
            Assert.AreEqual(0, skills.SkillList.Count, "SkillList should be empty after remove.");
            Assert.IsTrue(stub.RemoveSkillCalled, "Lifecycle RemoveSkill hook should have fired.");
            Assert.IsFalse(entity.Parts.Contains(stub), "Stub should be detached from entity.Parts.");
        }

        // ====================================================================
        // 7. RemoveSkill counter-check — unowned skill is a no-op (plan-mandated)
        // ====================================================================

        [Test]
        public void RemoveSkill_OnEntityWithoutSkill_DoesNothing()
        {
            var (entity, skills) = MakeActorWithSkills();
            var stub = new TestStubSkill();
            // Note: stub was NEVER added to skills. Calling Remove must be safe.

            bool removed = skills.RemoveSkill(stub);

            Assert.IsFalse(removed,
                "Removing a skill that was never added should return false (idempotent no-op).");
            Assert.AreEqual(0, skills.SkillList.Count);
            Assert.IsFalse(stub.RemoveSkillCalled,
                "RemoveSkill hook must NOT fire on a skill that wasn't owned.");
        }

        // ====================================================================
        // 8. HasSkill / GetSkill queries
        // ====================================================================

        [Test]
        public void HasSkillAndGetSkill_FindByClassName()
        {
            var (entity, skills) = MakeActorWithSkills();
            var stub = new TestStubSkill();
            skills.AddSkill(stub);

            Assert.IsTrue(skills.HasSkill(nameof(TestStubSkill)),
                "HasSkill should return true for an owned skill class.");
            Assert.IsFalse(skills.HasSkill("NotOwnedSkill"),
                "HasSkill should return false for an unowned class.");
            Assert.IsFalse(skills.HasSkill(""),
                "HasSkill on empty string should return false (defensive).");

            BaseSkillPart fetched = skills.GetSkill(nameof(TestStubSkill));
            Assert.AreSame(stub, fetched, "GetSkill should return the registered instance.");

            Assert.IsNull(skills.GetSkill("NotOwnedSkill"),
                "GetSkill for unowned should return null.");
        }

        // ====================================================================
        // 9. OnAfterLoad — rebuilds SkillList from entity.Parts
        // ====================================================================

        [Test]
        public void OnAfterLoad_RebuildsSkillListFromEntityParts()
        {
            var (entity, skills) = MakeActorWithSkills();
            var stub1 = new TestStubSkill();
            var stub2 = new TestStubSkillAlt();
            skills.AddSkill(stub1);
            skills.AddSkill(stub2);

            // Simulate save/load: SkillList state is lost (the [NonSerialized]
            // convention from MutationsPart pattern), but the parts remain
            // attached to entity.Parts via the Part-system serialization.
            // OnAfterLoad must rebuild the SkillList from entity.Parts.
            skills.SkillList.Clear();
            Assert.AreEqual(0, skills.SkillList.Count,
                "Precondition: simulated post-load empty SkillList.");

            skills.OnAfterLoad(reader: null);

            Assert.AreEqual(2, skills.SkillList.Count,
                "OnAfterLoad should rebuild SkillList from entity.Parts.");
            Assert.IsTrue(skills.SkillList.Contains(stub1));
            Assert.IsTrue(skills.SkillList.Contains(stub2));
        }

        // ====================================================================
        // 10. Diag plumbing — AddSkill emits skill/Added (user-mandated)
        // ====================================================================

        [Test]
        public void AddSkill_EmitsSkillAddedDiag()
        {
            var (entity, skills) = MakeActorWithSkills();
            Diag.ResetAll(); // ignore any setup-time records

            skills.AddSkill(new TestStubSkill(), source: "purchase");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill",
                Kind = "Added",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count,
                "AddSkill should emit exactly 1 skill/Added record.");
            string payload = records[0].PayloadJson;
            StringAssert.Contains("TestStubSkill", payload,
                "Payload should include the skill class name.");
            StringAssert.Contains("purchase", payload,
                "Payload should include the source tag.");
        }

        // ====================================================================
        // 11. Diag plumbing — RemoveSkill emits skill/Removed (user-mandated)
        // ====================================================================

        [Test]
        public void RemoveSkill_EmitsSkillRemovedDiag()
        {
            var (entity, skills) = MakeActorWithSkills();
            var stub = new TestStubSkill();
            skills.AddSkill(stub);
            Diag.ResetAll(); // ignore the Add record; only inspect Remove

            skills.RemoveSkill(stub, cause: "unlearn");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill",
                Kind = "Removed",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count,
                "RemoveSkill should emit exactly 1 skill/Removed record.");
            string payload = records[0].PayloadJson;
            StringAssert.Contains("TestStubSkill", payload,
                "Payload should include the removed skill class name.");
            StringAssert.Contains("unlearn", payload,
                "Payload should include the cause tag.");
        }

        // ====================================================================
        // 12. Diag plumbing counter-check — failed AddSkill (rollback) emits NO record
        // ====================================================================

        [Test]
        public void AddSkill_RolledBack_DoesNotEmitDiag()
        {
            // Counter-check on the diag plumbing: when AddSkill rolls back
            // due to a failing hook, NO skill/Added record should fire.
            // The skill was never actually active; recording it would
            // mislead an observability consumer into thinking it was.
            var (entity, skills) = MakeActorWithSkills();
            Diag.ResetAll();

            skills.AddSkill(new TestFailingStubSkill());

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill",
                Kind = "Added",
                Limit = 10,
            }).Records;

            Assert.AreEqual(0, records.Count,
                "Failed/rolled-back AddSkill must NOT emit skill/Added — " +
                "the skill was never active; emitting would lie to observers.");
        }

        // ====================================================================
        // 13. WSP5 cold-eye Finding 2 — ActivatedAbilityID save/load persistence
        // ====================================================================

        /// <summary>
        /// Stub active-ability skill: declares an ActivatedAbilitySpec so
        /// SkillsPart.AddSkill assigns a non-empty ActivatedAbilityID Guid.
        /// Used to exercise the post-WSP4.4 save/load Guid-persistence
        /// pin (see WSP5 Finding 2).
        /// </summary>
        public class TestStubActivatedSkill : BaseSkillPart
        {
            public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
            {
                return new ActivatedAbilitySpec
                {
                    DisplayName = "Stub",
                    Command = "CommandStub",
                    Class = "Skills",
                    TargetingMode = AbilityTargetingMode.SelfCentered,
                    Range = 0,
                    Cooldown = 1,
                };
            }
        }

        [Test]
        public void ActivatedAbilityID_NotMarkedNonSerialized_PinsWSP44Fix()
        {
            // Structural regression pin for WSP4.4 cold-eye finding 🔴 #1:
            // ActivatedAbilityID MUST NOT be [NonSerialized]. If it is, the
            // skill→ability link breaks across save/load (every skill's
            // Guid post-load is Guid.Empty → TryRouteSkillCommand always
            // returns false → Conk/Berserk/etc. silently dead after a
            // reload). The behavioral consequence is hard to spot in a
            // gameplay scrub (the ability appears in the hotbar; clicking
            // it just does nothing). Pin the structural cause directly so
            // a regression that re-adds the attribute fails fast.
            var field = typeof(BaseSkillPart).GetField(
                nameof(BaseSkillPart.ActivatedAbilityID),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(field,
                "BaseSkillPart.ActivatedAbilityID should be a public instance field.");

            bool hasNonSerialized = System.Attribute.IsDefined(
                field, typeof(System.NonSerializedAttribute));

            Assert.IsFalse(hasNonSerialized,
                "BaseSkillPart.ActivatedAbilityID must NOT be marked [NonSerialized]. " +
                "ActivatedAbilitiesPart.AbilityByGuid IS serialized; if this Guid is " +
                "stripped on load, every active-ability skill (Conk/Berserk/...) is " +
                "silently dead post-load. WSP4.4 cold-eye finding 🔴 #1.");
        }

        [Test]
        public void OnAfterLoad_PreservesActivatedAbilityID_OnSkillsWithAbilities()
        {
            // Behavioral pin for WSP4.4 🔴 #1: after AddSkill assigns an
            // ActivatedAbilityID, that Guid must survive the simulated-
            // post-load OnAfterLoad call (which mirrors what the save
            // system does after deserializing the skill instance + the
            // ActivatedAbilitiesPart). Specifically: the skill instance's
            // ActivatedAbilityID field IS the link into
            // ActivatedAbilitiesPart.AbilityByGuid, so if the Guid drops
            // to Empty, TryRouteSkillCommand can't resolve the owning
            // skill on the next swing.
            var entity = new Entity { ID = "actor", BlueprintName = "TestActor" };
            entity.AddPart(new RenderPart { DisplayName = "actor" });
            entity.AddPart(new ActivatedAbilitiesPart());
            var skills = new SkillsPart();
            entity.AddPart(skills);

            var stub = new TestStubActivatedSkill();
            bool added = skills.AddSkill(stub);

            Assert.IsTrue(added, "Pre-condition: stub active-ability skill should add cleanly.");
            Assert.AreNotEqual(System.Guid.Empty, stub.ActivatedAbilityID,
                "Pre-condition: AddSkill should have populated ActivatedAbilityID.");
            System.Guid capturedId = stub.ActivatedAbilityID;

            // Simulate the post-load state (SkillList lost; parts retained).
            // The skill instance's ActivatedAbilityID is the field under
            // test — if it gets zeroed during a real save→load roundtrip,
            // a [NonSerialized] regression is the proximate cause.
            skills.SkillList.Clear();
            skills.OnAfterLoad(reader: null);

            // Behavioral assertion: the rebuilt SkillList contains the same
            // instance and that instance's Guid is unchanged.
            Assert.AreEqual(1, skills.SkillList.Count,
                "OnAfterLoad should rebuild SkillList from entity.Parts.");
            Assert.AreSame(stub, skills.SkillList[0],
                "OnAfterLoad must recover the SAME skill instance from entity.Parts.");
            Assert.AreEqual(capturedId, skills.SkillList[0].ActivatedAbilityID,
                "ActivatedAbilityID must persist across the simulated-load path. " +
                "If this fails, BaseSkillPart.ActivatedAbilityID likely got " +
                "[NonSerialized] re-added (see structural-pin counterpart " +
                "ActivatedAbilityID_NotMarkedNonSerialized_PinsWSP44Fix). " +
                "WSP4.4 cold-eye finding 🔴 #1.");
        }

        // ====================================================================
        // 14. HandleEvent — GameEvent command dispatch routes to OnCommand
        // ====================================================================
        //
        // This pins the production input-dispatch path that the
        // InputHandler's ResolveAbilityCommand uses (line 2819 of
        // InputHandler.cs):
        //
        //     PlayerEntity.FireEvent(cmd);  // cmd.ID = "CommandSlam" etc.
        //
        // Pre-fix, only mutations had HandleEvent overrides for command
        // names; skills' OnCommand was wired only via TryRouteSkillCommand
        // which production code never called. The result: pressing Slam
        // / Conk / Berserk / Shank / HookAndDrag prompted "choose a
        // direction" then immediately reported "The rite fails to
        // resolve." because cmd.Handled stayed false.
        //
        // The fix: SkillsPart.HandleEvent override that detects
        // "Command*" event IDs and routes to TryRouteSkillCommand, which
        // calls OnCommand + applies cooldown. These tests pin the
        // integration end-to-end so the bug can't regress.

        /// <summary>
        /// Test stub that records OnCommand calls. Lets us verify
        /// the GameEvent → SkillsPart.HandleEvent → TryRouteSkillCommand
        /// → skill.OnCommand chain fires end-to-end.
        /// </summary>
        public class TestRecordingActivatedSkill : BaseSkillPart
        {
            public int OnCommandCallCount;
            public SkillEventContext LastCtx;
            public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
            {
                return new ActivatedAbilitySpec
                {
                    DisplayName = "Test", Command = "CommandTest",
                    Class = "Skills",
                    TargetingMode = AbilityTargetingMode.AdjacentCell,
                    Range = 1, Cooldown = 5,
                };
            }
            public override void OnCommand(SkillEventContext ctx)
            {
                OnCommandCallCount++;
                LastCtx = ctx;
            }
        }

        [Test]
        public void HandleEvent_CommandMatchingOwnedSkill_RoutesToOnCommandAndMarksHandled()
        {
            var entity = new Entity { ID = "actor", BlueprintName = "TestActor" };
            entity.AddPart(new RenderPart { DisplayName = "actor" });
            entity.AddPart(new ActivatedAbilitiesPart());
            var skills = new SkillsPart();
            entity.AddPart(skills);
            var skill = new TestRecordingActivatedSkill();
            skills.AddSkill(skill);

            // Fire the GameEvent the way InputHandler.ResolveAbilityCommand
            // does (line 2808-2819). Set Zone + RNG params; check Handled
            // post-fire.
            var rng = new System.Random(42);
            var cmd = GameEvent.New("CommandTest");
            cmd.SetParameter("RNG", (object)rng);

            bool result = entity.FireEvent(cmd);
            bool handled = cmd.Handled;
            cmd.Release();

            Assert.AreEqual(1, skill.OnCommandCallCount,
                "OnCommand must fire exactly once when the GameEvent's ID matches " +
                "the skill's registered Command.");
            Assert.IsTrue(handled,
                "cmd.Handled must be true post-fire. Without this, " +
                "InputHandler.ResolveAbilityCommand logs 'The rite fails to " +
                "resolve.' and skill activations silently fail.");
            Assert.AreSame(rng, skill.LastCtx.Rng,
                "The RNG threaded through the GameEvent must reach OnCommand's " +
                "ctx.Rng — otherwise skill RNG isn't deterministic from input.");
        }

        [Test]
        public void HandleEvent_NonCommandEvent_DoesNotMarkHandled()
        {
            // Counter-check: events whose ID doesn't start with "Command"
            // must NOT be intercepted. Otherwise SkillsPart would
            // accidentally consume unrelated events (TakeDamage, EndTurn,
            // etc.) and break their dispatch.
            var entity = new Entity { ID = "actor" };
            entity.AddPart(new RenderPart { DisplayName = "actor" });
            entity.AddPart(new ActivatedAbilitiesPart());
            var skills = new SkillsPart();
            entity.AddPart(skills);
            skills.AddSkill(new TestRecordingActivatedSkill());

            var ev = GameEvent.New("EndTurn");
            entity.FireEvent(ev);
            bool handled = ev.Handled;
            ev.Release();

            Assert.IsFalse(handled,
                "Non-Command events must NOT be marked Handled by SkillsPart " +
                "— this would clobber the dispatch of EndTurn / TakeDamage / etc.");
        }

        [Test]
        public void HandleEvent_CommandNotMatchingAnyOwnedSkill_DoesNotMarkHandled()
        {
            // Counter-check: Command events whose name doesn't match any
            // owned skill must propagate. Otherwise SkillsPart would block
            // mutation commands when a player has both skills + mutations.
            var entity = new Entity { ID = "actor" };
            entity.AddPart(new RenderPart { DisplayName = "actor" });
            entity.AddPart(new ActivatedAbilitiesPart());
            var skills = new SkillsPart();
            entity.AddPart(skills);
            skills.AddSkill(new TestRecordingActivatedSkill());  // owns CommandTest

            // Fire a different command — represents "player has Slam (skill)
            // AND FireBolt (mutation), pressed FireBolt's keybind."
            var ev = GameEvent.New("CommandFireBolt");
            entity.FireEvent(ev);
            bool handled = ev.Handled;
            ev.Release();

            Assert.IsFalse(handled,
                "Command events for unowned skills must NOT be marked Handled " +
                "by SkillsPart — must propagate to mutations / other Parts.");
        }

        [Test]
        public void HandleEvent_CommandOnCooldown_DoesNotMarkHandled()
        {
            // When the ability is on cooldown, TryRouteSkillCommand
            // returns false. SkillsPart.HandleEvent leaves Handled=false
            // so the event propagates (no other Part will claim it,
            // and the InputHandler's "The rite fails to resolve."
            // surfaces as the failure message — at least informative,
            // even if the cooldown gate was already checked upstream).
            var entity = new Entity { ID = "actor" };
            entity.AddPart(new RenderPart { DisplayName = "actor" });
            entity.AddPart(new ActivatedAbilitiesPart());
            var skills = new SkillsPart();
            entity.AddPart(skills);
            var skill = new TestRecordingActivatedSkill();
            skills.AddSkill(skill);

            // Force cooldown ON.
            var abilities = entity.GetPart<ActivatedAbilitiesPart>();
            var ability = abilities.GetAbility(skill.ActivatedAbilityID);
            ability.CooldownRemaining = 5;
            Assert.IsFalse(ability.IsUsable, "Pre-condition: cooldown is active.");

            var ev = GameEvent.New("CommandTest");
            entity.FireEvent(ev);
            bool handled = ev.Handled;
            ev.Release();

            Assert.AreEqual(0, skill.OnCommandCallCount,
                "Cooldown-blocked command must NOT invoke OnCommand.");
            Assert.IsFalse(handled,
                "Cooldown-blocked command must leave Handled=false so the " +
                "event propagates and the failure surfaces.");
        }
    }
}
