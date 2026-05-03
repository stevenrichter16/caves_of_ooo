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
    }
}
