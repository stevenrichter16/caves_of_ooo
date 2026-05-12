using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.1.2 — <see cref="IItemEnhancement"/> abstract
    /// base contract.
    ///
    /// <para><b>Lifecycle pinned (E.1.1 lockdown):</b></para>
    /// <list type="number">
    ///   <item><c>Configure()</c> — called by ctor, sets default fields</item>
    ///   <item><c>ApplyTier(int)</c> — sets <c>Tier</c>, calls <c>TierConfigure()</c></item>
    ///   <item><c>Applicable(Entity item)</c> — returns false to reject application</item>
    ///   <item><c>Apply(Entity item)</c> — runs when the enhancement is added (auto-called
    ///         via <c>ItemEnhancing.Apply</c> in E.1.4)</item>
    ///   <item><c>Remove(Entity item)</c> — runs when the enhancement is removed</item>
    /// </list>
    ///
    /// <para><b>Qud parity:</b> mirrors
    /// <c>/Users/steven/qud-decompiled-project/XRL.World.Parts/IModification.cs</c>.
    /// CoO simplifies away the Examiner-Difficulty/Complexity scaling
    /// helpers (no Examiner in CoO).</para>
    /// </summary>
    public class IItemEnhancementTests
    {
        // ── Test stub ────────────────────────────────────────────

        /// <summary>
        /// Minimal concrete IItemEnhancement for testing the lifecycle.
        /// Tracks call counts so tests can assert "Configure fired" etc.
        /// </summary>
        public class StubEnhancement : IItemEnhancement
        {
            public override string Name => nameof(StubEnhancement);
            public int ConfigureCount;
            public int TierConfigureCount;
            public int ApplyCount;
            public int RemoveCount;
            public bool ApplicableReturn = true;
            public Entity LastAppliedTo;
            public Entity LastRemovedFrom;

            // Public field for round-trip — exercises the SL.6 reflection contract.
            public string Note = "default";

            public override void Configure() { ConfigureCount++; }
            public override void TierConfigure() { TierConfigureCount++; }
            public override bool Applicable(Entity item) => ApplicableReturn;
            public override void Apply(Entity item) { ApplyCount++; LastAppliedTo = item; }
            public override void Remove(Entity item) { RemoveCount++; LastRemovedFrom = item; }
            public override string GetDisplayName() => "Stub";
        }

        private static Entity MakeItem(string id = "weapon")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Takeable = true });
            return e;
        }

        // ── Defaults ─────────────────────────────────────────────

        [Test]
        public void Tier_DefaultIs1()
        {
            var stub = new StubEnhancement();
            Assert.AreEqual(1, stub.Tier,
                "Tier defaults to 1 (Qud parity — Tier 1 is the baseline).");
        }

        [Test]
        public void Configure_CalledOnConstruction()
        {
            var stub = new StubEnhancement();
            Assert.AreEqual(1, stub.ConfigureCount,
                "Configure() runs exactly once during construction. " +
                "Tests + content rely on this for default-field setup.");
        }

        [Test]
        public void Applicable_DefaultsToTrue_WhenSubclassDoesntOverride()
        {
            // Direct concrete instance with no override → base returns true.
            // (StubEnhancement overrides Applicable to expose ApplicableReturn,
            // so we test the BASE class behavior via reflection on a separate
            // throwaway subclass.)
            var openStub = new DefaultApplicableEnhancement();
            Assert.IsTrue(openStub.Applicable(MakeItem()),
                "Base Applicable returns true so enhancements opt OUT, not in.");
        }

        public class DefaultApplicableEnhancement : IItemEnhancement
        {
            public override string Name => nameof(DefaultApplicableEnhancement);
            public override string GetDisplayName() => "Default";
            // NO Applicable override — inherits the base.
        }

        // ── ApplyTier ────────────────────────────────────────────

        [Test]
        public void ApplyTier_SetsField()
        {
            var stub = new StubEnhancement();
            stub.ApplyTier(3);
            Assert.AreEqual(3, stub.Tier,
                "ApplyTier(N) sets the Tier field to N.");
        }

        [Test]
        public void ApplyTier_FiresTierConfigure()
        {
            var stub = new StubEnhancement();
            int before = stub.TierConfigureCount;
            stub.ApplyTier(2);
            Assert.AreEqual(before + 1, stub.TierConfigureCount,
                "ApplyTier triggers TierConfigure() so content can scale " +
                "numbers by tier.");
        }

        [Test]
        public void ApplyTier_RepeatedCalls_AccumulateTierConfigure()
        {
            // Each ApplyTier call fires TierConfigure. Mirrors Qud — a
            // re-tier (rare but possible at runtime, e.g. tier-up content
            // upgrade) re-runs the scaling code.
            var stub = new StubEnhancement();
            stub.ApplyTier(1);
            stub.ApplyTier(2);
            stub.ApplyTier(3);
            Assert.AreEqual(3, stub.TierConfigureCount);
            Assert.AreEqual(3, stub.Tier);
        }

        [Test]
        public void ApplyTier_DoesNotRefireConfigure()
        {
            // Configure runs ONCE per ctor. ApplyTier doesn't re-fire it
            // (Configure is for ctor-time defaults; TierConfigure is for
            // tier-scaled numbers).
            var stub = new StubEnhancement();
            int configureBefore = stub.ConfigureCount;
            stub.ApplyTier(5);
            Assert.AreEqual(configureBefore, stub.ConfigureCount,
                "ApplyTier MUST NOT re-fire Configure — that's ctor-time only.");
        }

        // ── Apply / Remove ───────────────────────────────────────

        [Test]
        public void Apply_PassesTargetEntity()
        {
            var stub = new StubEnhancement();
            var item = MakeItem();
            stub.Apply(item);
            Assert.AreSame(item, stub.LastAppliedTo,
                "Apply(item) passes the item Entity through to the override.");
            Assert.AreEqual(1, stub.ApplyCount);
        }

        [Test]
        public void Remove_PassesTargetEntity()
        {
            var stub = new StubEnhancement();
            var item = MakeItem();
            stub.Remove(item);
            Assert.AreSame(item, stub.LastRemovedFrom);
            Assert.AreEqual(1, stub.RemoveCount);
        }

        [Test]
        public void Apply_DefaultBaseImpl_DoesNotThrow_OnNull()
        {
            // Defense-in-depth — base Apply / Remove are abstract-default,
            // safe to call with null target (mirrors Effect.OnApply's null
            // safety, F.2.2 lesson).
            var openStub = new DefaultApplicableEnhancement();
            Assert.DoesNotThrow(() => openStub.Apply(null));
            Assert.DoesNotThrow(() => openStub.Remove(null));
        }

        // ── Applicable as veto gate ──────────────────────────────

        [Test]
        public void Applicable_CanRejectOrAccept()
        {
            var stub = new StubEnhancement();
            var item = MakeItem();
            stub.ApplicableReturn = false;
            Assert.IsFalse(stub.Applicable(item));
            stub.ApplicableReturn = true;
            Assert.IsTrue(stub.Applicable(item));
        }

        // ── Save/load round-trip (SL.6 reflection contract) ──────

        [Test]
        public void RoundTrip_PreservesTierAndFields()
        {
            // Critical contract: enhancements survive save/load with all
            // public-field state intact. Mirrors F.3.5 GrantsRepAsFollowerPart
            // round-trip pinning.
            //
            // Per SaveSystem.cs:1126 the generic Part fall-through uses
            // WritePublicFields, which catches the `Tier` and `Note` public
            // fields automatically.
            var item = MakeItem("weapon-id");
            var stub = new StubEnhancement();
            stub.ApplyTier(4);
            stub.Note = "round-trip-marker";
            item.AddPart(stub);

            Entity loaded = PartRoundTripHelper.RoundTripEntity(item);

            var loadedStub = loaded.GetPart<StubEnhancement>();
            Assert.IsNotNull(loadedStub,
                "Enhancement Part survives the generic reflection save.");
            Assert.AreEqual(4, loadedStub.Tier,
                "Tier field round-trips via WritePublicFields.");
            Assert.AreEqual("round-trip-marker", loadedStub.Note,
                "Arbitrary public string fields round-trip.");
        }

        // ── Display name ─────────────────────────────────────────

        [Test]
        public void GetDisplayName_Override_Returns_OverrideValue()
        {
            var stub = new StubEnhancement();
            Assert.AreEqual("Stub", stub.GetDisplayName());
        }
    }
}
