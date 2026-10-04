using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;
using F = CavesOfOoo.Tests.MultiCellAbilityConsumerTests;

namespace CavesOfOoo.Tests
{
    /// <summary>Chosen-cell authority must survive malformed cells and stale,
    /// carried, dead, removed or geometrically misleading entity aliases.</summary>
    public sealed class DirectedMeleeTargetingAdversarialTests
    {
        private static IEnumerable<TestCaseData> InvalidSelections()
        {
            foreach (string skill in DirectedMeleeFixture.SkillNames)
                foreach (string invalid in new[] { "remote", "foreign-zone", "cell-alias", "self-cell", "removed", "anchor-hole" })
                    yield return new TestCaseData(skill, invalid);
        }

        [TestCaseSource(nameof(InvalidSelections))]
        public void InvalidExplicitSelectionRefusesInsteadOfRetargetingAdjacentCreature(string skill, string invalid)
        {
            var f = new DirectedMeleeFixture(skill);
            Cell chosen = f.Zone.GetCell(11, 10);
            switch (invalid)
            {
                case "remote": f.Zone.MoveEntity(f.East, 13, 10); chosen = f.Zone.GetCell(13, 10); break;
                case "foreign-zone": chosen = new Zone(f.Zone.ZoneID).GetCell(11, 10); break;
                case "cell-alias": chosen = new Cell(11, 10, f.Zone); chosen.Objects.Add(f.East); break;
                case "self-cell": chosen = f.Zone.GetCell(10, 10); break;
                case "removed": f.Zone.RemoveEntity(f.East); chosen.Objects.Add(f.East); break;
                case "anchor-hole":
                    f.Zone.RemoveEntity(f.East); f.East.AddPart(new SpatialFootprintPart { CellsRaw = "1,0" });
                    F.Place(f.Zone, f.East, 11, 10); break;
            }
            Assert.IsFalse(f.Cast(chosen), "An invalid explicit target must never fall back to the northern creature.");
            Assert.AreEqual(0, f.Ability.CooldownRemaining);
            Assert.AreEqual(0, f.Rng.Calls);
            Assert.AreEqual(1000, f.East.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, f.East.GetPart<StatusEffectsPart>().GetAllEffects().Count);
            Assert.IsNotNull(f.East.GetPart<InventoryPart>().GetEquippedWithPart<MeleeWeaponPart>());
            DirectedMeleeFixture.AssertUntouched(f.North, f.Zone, 10, 9);
        }

        [TestCase("dead")] [TestCase("carried")] [TestCase("equipped")]
        [TestCase("foreign-owner")] [TestCase("non-creature")]
        public void AnIneligibleSelectedCreatureCannotBeUsedAsATargetAlias(string invalid)
        {
            var f = new DirectedMeleeFixture("Axe_RendArmor");
            switch (invalid)
            {
                case "dead": f.East.GetStat("Hitpoints").BaseValue = 0; break;
                case "carried": f.East.GetPart<PhysicsPart>().InInventory = f.Actor; break;
                case "equipped": f.East.GetPart<PhysicsPart>().Equipped = f.Actor; break;
                case "foreign-owner":
                    f.Zone.RemoveEntity(f.East); F.Place(new Zone("other"), f.East, 11, 10);
                    f.Zone.GetCell(11, 10).Objects.Add(f.East); break;
                case "non-creature": f.East.Tags.Remove("Creature"); break;
            }
            Assert.IsFalse(f.Cast(f.Zone.GetCell(11, 10)));
            Assert.IsFalse(f.East.HasEffect<ShatterArmorEffect>());
            Assert.AreEqual(0, f.Ability.CooldownRemaining);
            Assert.AreEqual(0, f.Rng.Calls);
            DirectedMeleeFixture.AssertUntouched(f.North, f.Zone, 10, 9);
        }

        [Test]
        public void SelectedCellIsAuthoritativeEvenWhenDirectionFieldsPointAtOtherCreature()
        {
            var f = new DirectedMeleeFixture("Cudgel_Conk");
            Assert.IsTrue(f.Cast(f.Zone.GetCell(11, 10), dx: 0, dy: -1));
            f.AssertAffected(f.East);
            DirectedMeleeFixture.AssertUntouched(f.North, f.Zone, 10, 9);
        }

        [Test]
        public void SelectionDoesNotPersistIntoLaterLegacyCommand()
        {
            var f = new DirectedMeleeFixture("Axe_RendArmor");
            Assert.IsTrue(f.Cast(f.Zone.GetCell(11, 10)));
            Assert.IsTrue(f.East.HasEffect<ShatterArmorEffect>());
            Assert.IsFalse(f.North.HasEffect<ShatterArmorEffect>());
            f.Ability.CooldownRemaining = 0;
            Assert.IsTrue(f.Cast(null, includeTarget: false));
            Assert.IsTrue(f.North.HasEffect<ShatterArmorEffect>());
        }
    }
}
