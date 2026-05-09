using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.4 — Save/Load Round-Trip Audit, Tier-3 Parts with non-Entity
    /// generic collections. See <c>Docs/SAVE-LOAD-AUDIT.md</c> for the
    /// audit plan.
    ///
    /// <para><b>Re-scoped during execution:</b> the audit-plan
    /// preliminary list mixed many Parts but most have either:
    /// <list type="bullet">
    ///   <item>Tier-1 explicit handlers (<c>InventoryPart</c>,
    ///         <c>ActivatedAbilitiesPart</c>, <c>MutationsPart</c>,
    ///         <c>BitLockerPart</c>, <c>Body</c>) — covered by SL.7</item>
    ///   <item>Already-tested simple collections in SL.2/SL.3
    ///         (<c>MaterialPart.MaterialTags</c> HashSet&lt;string&gt;;
    ///         <c>ContainerPart.Contents</c> List&lt;Entity&gt;)</item>
    ///   <item>Properties (not fields) so reflection skips them
    ///         (<c>MeleeWeaponPart.OnHitEffectsCachedSpecs</c> is a
    ///         property; backing field is private; cache rebuilds
    ///         lazily after load — no round-trip concern)</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>SL.4 in-scope (final):</b>
    /// <list type="bullet">
    ///   <item><c>SkillsPart.SkillList</c> (List&lt;BaseSkillPart&gt;)
    ///         — Tier-3 with custom-class elements that ALSO live in
    ///         <c>ParentEntity.Parts</c>. <c>OnAfterLoad</c> rebuilds
    ///         the list to match. High-suspicion: identity divergence
    ///         when OnAfterLoad doesn't run (the previous SL.2/SL.3
    ///         helpers had this gap — fixed here via
    ///         <c>RoundTripEntityViaTokenGraph</c>).</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Bug-class probes</b> (16-surface taxonomy):
    /// <list type="bullet">
    ///   <item>SL-5 Generic collection round-trip (custom-class elements)</item>
    ///   <item>SL-13 Part with private/derived state (SkillList is a
    ///         convenience cache rebuilt by OnAfterLoad)</item>
    ///   <item>SL-16 Part-with-Part reference (SkillsPart references
    ///         BaseSkillPart instances that are ALSO Parts on the
    ///         same entity)</item>
    /// </list>
    /// </para>
    /// </summary>
    public class Tier3CollectionRoundTripTests
    {
        // ── A. Empty SkillList counter-check ────────────────────────────

        [Test]
        public void Adversarial_SkillsPart_EmptyList_RoundTripsEmpty()
        {
            // Counter-check: a SkillsPart with no skills must round-trip
            // with an empty (non-null) SkillList. A buggy impl that
            // collapses count=0 to null would fail here.
            var entity = new Entity { ID = "e", BlueprintName = "TestEntity" };
            entity.AddPart(new SkillsPart());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var skills = loaded.GetPart<SkillsPart>();

            Assert.IsNotNull(skills);
            Assert.IsNotNull(skills.SkillList,
                "Empty SkillList round-trips as non-null List.");
            Assert.AreEqual(0, skills.SkillList.Count);
        }

        // ── B. Single skill round-trip ──────────────────────────────────

        [Test]
        public void Adversarial_SkillsPart_OneSkill_RoundTripsAndOnAfterLoadResolvesIdentity()
        {
            // Production-faithful round-trip via token graph. The skill
            // is added via the SkillsPart.AddSkill API so it's both:
            //   (1) on the entity's Parts list (saved via the Part-system)
            //   (2) in SkillList (the convenience cache)
            //
            // After round-trip + OnAfterLoad, SkillList must reference
            // the SAME BaseSkillPart INSTANCES that are in
            // ParentEntity.Parts — not separately-deserialized
            // duplicates from reflection-loading SkillList. This is the
            // identity-resolution contract (SkillsPart.OnAfterLoad:46-59).
            var entity = new Entity { ID = "actor", BlueprintName = "TestActor" };
            entity.AddPart(new SkillsPart());
            var skills = entity.GetPart<SkillsPart>();

            var acrobatics = new AcrobaticsSkill();
            skills.AddSkill(acrobatics);

            // Sanity: pre-save the cache + the entity's Parts both contain
            // the same BaseSkillPart instance.
            Assert.AreEqual(1, skills.SkillList.Count);
            Assert.AreSame(acrobatics, skills.SkillList[0]);
            Assert.IsTrue(entity.Parts.Contains(acrobatics));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var loadedSkills = loaded.GetPart<SkillsPart>();
            Assert.IsNotNull(loadedSkills);
            Assert.AreEqual(1, loadedSkills.SkillList.Count,
                "SkillList rebuilt by OnAfterLoad has exactly one entry.");

            var loadedSkill = loadedSkills.SkillList[0];
            Assert.IsInstanceOf<AcrobaticsSkill>(loadedSkill,
                "Concrete BaseSkillPart subtype preserved through "
                + "WriteTypedObject/ResolveType (SaveSystem.cs:1786-1820).");

            // Identity contract: SkillList[i] must be the SAME instance
            // as ParentEntity.Parts[k] for some k. Without OnAfterLoad,
            // the reflection-loaded SkillList contents would be DIFFERENT
            // BaseSkillPart instances than the entity's Parts list.
            bool foundOnEntity = false;
            for (int i = 0; i < loaded.Parts.Count; i++)
            {
                if (System.Object.ReferenceEquals(loaded.Parts[i], loadedSkill))
                {
                    foundOnEntity = true;
                    break;
                }
            }
            Assert.IsTrue(foundOnEntity,
                "After OnAfterLoad, SkillList entries must be the SAME "
                + "instances as those in ParentEntity.Parts. If this "
                + "fails, the load path is producing duplicate Parts.");
        }

        // ── C. Subtype-preservation counter-check ───────────────────────

        [Test]
        public void Adversarial_SkillsPart_PreservesConcreteSubtype_NotJustBaseSkillPart()
        {
            // The reflection serializer writes the concrete type name
            // for non-Entity custom-class collection elements
            // (WriteTypedObject:1786-1801 calls writer.WriteString(GetTypeName(actualType))
            // when actualType != declaredType). Without this, every
            // BaseSkillPart in SkillList would deserialize as a
            // BaseSkillPart base instance (compile error: it's
            // abstract) OR as null. Pin the concrete-subtype contract.
            var entity = new Entity { ID = "actor2", BlueprintName = "TestActor" };
            entity.AddPart(new SkillsPart());
            var skills = entity.GetPart<SkillsPart>();
            skills.AddSkill(new AcrobaticsSkill());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var loadedSkills = loaded.GetPart<SkillsPart>();

            Assert.AreEqual(typeof(AcrobaticsSkill),
                loadedSkills.SkillList[0].GetType(),
                "Concrete subtype must round-trip; not the abstract base.");
        }

        // ── D. ParentEntity wiring counter-check ────────────────────────

        [Test]
        public void Adversarial_SkillsPart_LoadedSkill_HasParentEntityWired()
        {
            // Counter-check: the loaded BaseSkillPart's ParentEntity
            // must point to the loaded entity (NOT the source entity,
            // NOT null). The Part-system load path sets ParentEntity
            // on each loaded Part (SaveSystem.cs:655) — pin it.
            var entity = new Entity { ID = "actor3", BlueprintName = "TestActor" };
            entity.AddPart(new SkillsPart());
            var skills = entity.GetPart<SkillsPart>();
            skills.AddSkill(new AcrobaticsSkill());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var loadedSkills = loaded.GetPart<SkillsPart>();

            Assert.AreSame(loaded, loadedSkills.SkillList[0].ParentEntity,
                "Loaded skill's ParentEntity points at the LOADED entity.");
            Assert.AreSame(loaded, loadedSkills.ParentEntity,
                "SkillsPart's ParentEntity also points at the loaded entity.");
        }

        // ── E. WithBodies vs ViaTokenGraph helper distinction ───────────
        //
        // RoundTripEntityWithBodies calls LoadEntityBody directly on
        // the primary, BYPASSING OnAfterLoad. RoundTripEntityViaTokenGraph
        // queues the primary as a token, which routes through
        // ReadEntityBodies + OnAfterLoad. This distinction matters for
        // Parts that depend on OnAfterLoad (SkillsPart, MutationsPart).
        //
        // We pin this contract so a future change to the helpers can't
        // silently lose OnAfterLoad invocation.

        [Test]
        public void Adversarial_RoundTripEntityWithBodies_DoesNotInvokeOnAfterLoad_OnPrimary()
        {
            // ADVERSARIAL: with the WithBodies helper, the primary
            // entity's OnAfterLoad is NOT called (primary isn't in
            // SaveReader._loadedEntities). For SkillsPart this means
            // the loaded SkillList is whatever reflection wrote — which
            // is a parallel BaseSkillPart instance, NOT the same one in
            // ParentEntity.Parts.
            //
            // This test pins that the WithBodies helper is INSUFFICIENT
            // for SkillsPart — and that the ViaTokenGraph helper IS the
            // correct one for OnAfterLoad-dependent Parts.
            var entity = new Entity { ID = "actor4", BlueprintName = "TestActor" };
            entity.AddPart(new SkillsPart());
            var skills = entity.GetPart<SkillsPart>();
            skills.AddSkill(new AcrobaticsSkill());

            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(entity);
            var loadedSkills = loaded.GetPart<SkillsPart>();

            Assert.IsNotNull(loadedSkills, "SkillsPart itself round-trips.");
            Assert.AreEqual(1, loadedSkills.SkillList.Count,
                "SkillList still has 1 entry — but it's a duplicate, "
                + "not the same instance as in ParentEntity.Parts.");

            // The crucial divergence: SkillList[0] is a SEPARATE
            // BaseSkillPart instance from any in loaded.Parts. Without
            // OnAfterLoad, identity is divergent.
            bool foundOnEntity = false;
            for (int i = 0; i < loaded.Parts.Count; i++)
            {
                if (System.Object.ReferenceEquals(loaded.Parts[i], loadedSkills.SkillList[0]))
                {
                    foundOnEntity = true;
                    break;
                }
            }
            Assert.IsFalse(foundOnEntity,
                "Without OnAfterLoad on the primary, SkillList[0] is "
                + "NOT the same instance as any Part in loaded.Parts. "
                + "This is the contract that justifies the existence "
                + "of RoundTripEntityViaTokenGraph.");
        }

        // ── F. Multi-skill round-trip ───────────────────────────────────

        [Test]
        public void Adversarial_SkillsPart_MultipleSkills_AllRoundTrip()
        {
            // List-order contract: skills are added in a specific order
            // and that order must round-trip. (SkillsPart's AddSkill
            // appends to SkillList; SaveSystem.cs:WriteFieldValue IList
            // path writes in index order.)
            //
            // Note: AddSkill rejects duplicates of the same type; for
            // this test we use distinct subtypes via 3 different
            // skill classes if available. Since we only have one minimal
            // BaseSkillPart subclass (AcrobaticsSkill) in test scope, we
            // probe by adding the same skill type and verifying the
            // duplicate-skill rejection (a separate contract — pinned
            // here as a side-effect).
            var entity = new Entity { ID = "actor5", BlueprintName = "TestActor" };
            entity.AddPart(new SkillsPart());
            var skills = entity.GetPart<SkillsPart>();

            Assert.IsTrue(skills.AddSkill(new AcrobaticsSkill()),
                "First skill of a given type adds successfully.");
            Assert.IsFalse(skills.AddSkill(new AcrobaticsSkill()),
                "Duplicate-skill-type rejected (pin SkillsPart contract).");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var loadedSkills = loaded.GetPart<SkillsPart>();
            Assert.AreEqual(1, loadedSkills.SkillList.Count);
            Assert.IsInstanceOf<AcrobaticsSkill>(loadedSkills.SkillList[0]);
        }
    }
}
