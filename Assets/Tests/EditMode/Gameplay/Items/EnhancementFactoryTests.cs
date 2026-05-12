using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.1.3 — <see cref="EnhancementFactory"/>
    /// registry contract. Mirror of <c>SkillRegistry</c>'s dict-based
    /// by-name lookup pattern, simplified for code-side registration
    /// (E.5+ may add JSON loading).
    ///
    /// <para><b>Contract pinned here:</b></para>
    /// <list type="bullet">
    ///   <item>Registration by class name + display name</item>
    ///   <item>Case-insensitive lookups (matches SkillRegistry)</item>
    ///   <item><c>Create(name)</c> + <c>Create(name, tier)</c> instantiation</item>
    ///   <item>Idempotent registration (re-registering same name no-ops)</item>
    ///   <item>Reset-for-tests support so test isolation works</item>
    /// </list>
    /// </summary>
    public class EnhancementFactoryTests
    {
        [SetUp]
        public void Setup()
        {
            EnhancementFactory.ResetForTests();
        }

        // ── Test stubs ────────────────────────────────────────────

        public class StubEnhancementA : IItemEnhancement
        {
            public override string Name => nameof(StubEnhancementA);
            public override string GetDisplayName() => "Stub A";
        }

        public class StubEnhancementB : IItemEnhancement
        {
            public override string Name => nameof(StubEnhancementB);
            public override string GetDisplayName() => "Stub B";
        }

        // ── Register + TryGet ────────────────────────────────────

        [Test]
        public void Register_AddsEntry_DiscoverableByName()
        {
            EnhancementFactory.Register(typeof(StubEnhancementA));

            bool found = EnhancementFactory.TryGet(nameof(StubEnhancementA), out Type type);
            Assert.IsTrue(found);
            Assert.AreEqual(typeof(StubEnhancementA), type);
        }

        [Test]
        public void TryGet_UnknownName_ReturnsFalse()
        {
            bool found = EnhancementFactory.TryGet("NotRegistered", out Type type);
            Assert.IsFalse(found);
            Assert.IsNull(type);
        }

        [Test]
        public void TryGet_CaseInsensitive()
        {
            EnhancementFactory.Register(typeof(StubEnhancementA));
            Assert.IsTrue(EnhancementFactory.TryGet("stubenhancementa", out _));
            Assert.IsTrue(EnhancementFactory.TryGet("STUBENHANCEMENTA", out _));
            Assert.IsTrue(EnhancementFactory.TryGet("StubEnhancementA", out _));
        }

        [Test]
        public void Register_Twice_Idempotent_NoThrow()
        {
            EnhancementFactory.Register(typeof(StubEnhancementA));
            // Second registration of the same type is a no-op (per
            // SkillRegistry parity — content sometimes double-registers,
            // shouldn't crash).
            Assert.DoesNotThrow(() => EnhancementFactory.Register(typeof(StubEnhancementA)));
            // Still discoverable.
            Assert.IsTrue(EnhancementFactory.TryGet(nameof(StubEnhancementA), out _));
        }

        [Test]
        public void Register_MultipleDistinctTypes_AllDiscoverable()
        {
            EnhancementFactory.Register(typeof(StubEnhancementA));
            EnhancementFactory.Register(typeof(StubEnhancementB));

            Assert.IsTrue(EnhancementFactory.TryGet(nameof(StubEnhancementA), out var ta));
            Assert.IsTrue(EnhancementFactory.TryGet(nameof(StubEnhancementB), out var tb));
            Assert.AreEqual(typeof(StubEnhancementA), ta);
            Assert.AreEqual(typeof(StubEnhancementB), tb);
        }

        // ── TryGetByDisplayName ──────────────────────────────────

        [Test]
        public void TryGetByDisplayName_ResolvesViaGetDisplayName()
        {
            EnhancementFactory.Register(typeof(StubEnhancementA));
            // GetDisplayName returns "Stub A" — look up by that.
            bool found = EnhancementFactory.TryGetByDisplayName("Stub A", out Type type);
            Assert.IsTrue(found);
            Assert.AreEqual(typeof(StubEnhancementA), type);
        }

        [Test]
        public void TryGetByDisplayName_CaseInsensitive()
        {
            EnhancementFactory.Register(typeof(StubEnhancementA));
            Assert.IsTrue(EnhancementFactory.TryGetByDisplayName("stub a", out _));
            Assert.IsTrue(EnhancementFactory.TryGetByDisplayName("STUB A", out _));
        }

        [Test]
        public void TryGetByDisplayName_UnknownDisplay_ReturnsFalse()
        {
            EnhancementFactory.Register(typeof(StubEnhancementA));
            bool found = EnhancementFactory.TryGetByDisplayName("nonexistent", out _);
            Assert.IsFalse(found);
        }

        // ── Create ───────────────────────────────────────────────

        [Test]
        public void Create_KnownName_ReturnsInstance()
        {
            EnhancementFactory.Register(typeof(StubEnhancementA));
            IItemEnhancement inst = EnhancementFactory.Create(nameof(StubEnhancementA));
            Assert.IsNotNull(inst);
            Assert.IsInstanceOf<StubEnhancementA>(inst);
        }

        [Test]
        public void Create_UnknownName_ReturnsNull()
        {
            IItemEnhancement inst = EnhancementFactory.Create("NotRegistered");
            Assert.IsNull(inst);
        }

        [Test]
        public void Create_WithTier_SetsTierField()
        {
            EnhancementFactory.Register(typeof(StubEnhancementA));
            IItemEnhancement inst = EnhancementFactory.Create(nameof(StubEnhancementA), tier: 3);
            Assert.IsNotNull(inst);
            Assert.AreEqual(3, inst.Tier,
                "Create(name, tier) calls ApplyTier(tier) so the returned " +
                "instance has the right scaled state.");
        }

        // ── ResetForTests ────────────────────────────────────────

        [Test]
        public void ResetForTests_ClearsAllRegistrations()
        {
            EnhancementFactory.Register(typeof(StubEnhancementA));
            EnhancementFactory.Register(typeof(StubEnhancementB));
            Assert.IsTrue(EnhancementFactory.TryGet(nameof(StubEnhancementA), out _));

            EnhancementFactory.ResetForTests();

            Assert.IsFalse(EnhancementFactory.TryGet(nameof(StubEnhancementA), out _));
            Assert.IsFalse(EnhancementFactory.TryGet(nameof(StubEnhancementB), out _));
        }

        // ── Null-safety ──────────────────────────────────────────

        [Test]
        public void Register_NullType_DoesNotThrow_NoOp()
        {
            Assert.DoesNotThrow(() => EnhancementFactory.Register(null));
        }

        [Test]
        public void Register_NonEnhancementType_DoesNotThrow_NoOp()
        {
            // Defense-in-depth: registering a type that isn't an
            // IItemEnhancement subclass shouldn't crash. Skip silently
            // (or log) — mirrors SkillRegistry's defensive filtering.
            Assert.DoesNotThrow(() => EnhancementFactory.Register(typeof(string)));
            Assert.IsFalse(EnhancementFactory.TryGet("String", out _));
        }
    }
}
