using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Item Enhancements E.1.5 — <see cref="IMeleeEnhancement"/> filter
    /// contract. Pins that the base Applicable rejects items without
    /// a <see cref="MeleeWeaponPart"/>, and that subclasses can chain
    /// further filters via <c>base.Applicable</c>.
    /// </summary>
    public class IMeleeEnhancementTests
    {
        // ── Stubs ────────────────────────────────────────────────

        public class StubMeleeEnh : IMeleeEnhancement
        {
            public override string Name => nameof(StubMeleeEnh);
            public override string GetDisplayName() => "Stub Melee";
        }

        public class StubCuttingOnlyEnh : IMeleeEnhancement
        {
            public override string Name => nameof(StubCuttingOnlyEnh);
            public override string GetDisplayName() => "Stub Cutting Only";
            public override bool Applicable(Entity item)
            {
                if (!base.Applicable(item)) return false;
                var w = item.GetPart<MeleeWeaponPart>();
                return w != null && w.Attributes != null
                    && w.Attributes.Contains("Cutting");
            }
        }

        private static Entity MakeMeleeWeapon(string id, string attributes)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Takeable = true });
            e.AddPart(new MeleeWeaponPart { Attributes = attributes });
            return e;
        }

        private static Entity MakeNonWeapon(string id = "rock")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Takeable = true });
            // NO MeleeWeaponPart.
            return e;
        }

        // ── Filter contract ──────────────────────────────────────

        [Test]
        public void Applicable_OnMeleeWeapon_ReturnsTrue()
        {
            var weapon = MakeMeleeWeapon("mace", "Bludgeoning Cudgel");
            Assert.IsTrue(new StubMeleeEnh().Applicable(weapon),
                "Base IMeleeEnhancement.Applicable returns true when item " +
                "has MeleeWeaponPart.");
        }

        [Test]
        public void Applicable_OnNonWeapon_ReturnsFalse()
        {
            var rock = MakeNonWeapon();
            Assert.IsFalse(new StubMeleeEnh().Applicable(rock),
                "Base filter rejects items without MeleeWeaponPart.");
        }

        [Test]
        public void Applicable_OnNull_ReturnsFalse()
        {
            Assert.IsFalse(new StubMeleeEnh().Applicable(null),
                "Null item rejected via base IItemEnhancement.Applicable.");
        }

        // ── Subclass chaining ────────────────────────────────────

        [Test]
        public void Subclass_ChainsApplicable_CuttingOnly()
        {
            var cuttingSword = MakeMeleeWeapon("sword", "Cutting LongBlades");
            var bludgeoningMace = MakeMeleeWeapon("mace", "Bludgeoning Cudgel");

            var enh = new StubCuttingOnlyEnh();
            Assert.IsTrue(enh.Applicable(cuttingSword),
                "Cutting weapon → applicable.");
            Assert.IsFalse(enh.Applicable(bludgeoningMace),
                "Bludgeoning weapon → not applicable (subclass filter).");
        }

        // ── Integration with ItemEnhancing.Apply ─────────────────

        [Test]
        public void ItemEnhancingApply_RejectsMeleeEnhOnNonWeapon()
        {
            // The end-to-end veto: IMeleeEnhancement filter integrates
            // with ItemEnhancing.Apply's Applicable gate (E.1.4 hook).
            EnhancementFactory.ResetForTests();
            EnhancementFactory.Register(typeof(StubMeleeEnh));
            var rock = MakeNonWeapon();
            bool ok = ItemEnhancing.Apply(rock, nameof(StubMeleeEnh));
            Assert.IsFalse(ok,
                "Applying a melee enhancement to a non-weapon item is " +
                "vetoed at the Applicable gate.");
            Assert.IsNull(rock.GetPart<StubMeleeEnh>());
        }

        [Test]
        public void ItemEnhancingApply_AcceptsMeleeEnhOnWeapon()
        {
            EnhancementFactory.ResetForTests();
            EnhancementFactory.Register(typeof(StubMeleeEnh));
            var mace = MakeMeleeWeapon("mace", "Bludgeoning");
            bool ok = ItemEnhancing.Apply(mace, nameof(StubMeleeEnh));
            Assert.IsTrue(ok);
            Assert.IsNotNull(mace.GetPart<StubMeleeEnh>());
        }
    }
}
