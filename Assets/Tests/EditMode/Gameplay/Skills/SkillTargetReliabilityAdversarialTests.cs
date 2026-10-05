using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;
using F = CavesOfOoo.Tests.MultiCellAbilityConsumerTests;

namespace CavesOfOoo.Tests
{
    public sealed class SkillTargetReliabilityAdversarialTests
    {
        static IEnumerable<TestCaseData> InvalidCases()
        {
            foreach (string name in SkillTargetReliabilityFixture.Names)
                foreach (string invalid in new[] { "remote", "foreign-zone", "cell-alias", "self-cell",
                    "removed", "anchor-hole", "dead", "carried", "equipped", "foreign-owner" })
                    yield return new TestCaseData(name, invalid);
        }

        [TestCaseSource(nameof(InvalidCases))]
        public void InvalidCellOrPhysicalOwnerNeverFallsBackToOtherNeighbor(string name, string invalid)
        {
            var f = new SkillTargetReliabilityFixture(name);
            Cell selected = f.Zone.GetCell(11, 10);
            switch (invalid)
            {
                case "remote": Assert.True(f.Zone.MoveEntity(f.East, 14, 10)); selected = f.Zone.GetCell(14, 10); break;
                case "foreign-zone": selected = new Zone(f.Zone.ZoneID).GetCell(11, 10); break;
                case "cell-alias": selected = new Cell(11, 10, f.Zone); selected.Objects.Add(f.East); break;
                case "self-cell": selected = f.Zone.GetCell(10, 10); break;
                case "removed": Assert.True(f.Zone.RemoveEntity(f.East)); selected.Objects.Add(f.East); break;
                case "anchor-hole":
                    Assert.True(f.Zone.RemoveEntity(f.East)); f.East.AddPart(new SpatialFootprintPart { CellsRaw = "1,0" });
                    F.Place(f.Zone, f.East, 11, 10); break;
                case "dead": f.East.GetStat("Hitpoints").BaseValue = 0; break;
                case "carried": f.East.GetPart<PhysicsPart>().InInventory = f.Actor; break;
                case "equipped": f.East.GetPart<PhysicsPart>().Equipped = f.Actor; break;
                case "foreign-owner":
                    Assert.True(f.Zone.RemoveEntity(f.East)); F.Place(new Zone("other"), f.East, 11, 10);
                    selected.Objects.Add(f.East); break;
            }
            f.AssertFreeRefusal(selected);
        }

        [Test]
        public void RemovedSelectedTargetDoesNotLeaveAStickySelectionForLegacyCommand()
        {
            var f = new SkillTargetReliabilityFixture("Cryomancy_Frostbind");
            Assert.True(f.Cast(f.Zone.GetCell(11, 10))); Assert.True(f.East.HasEffect<RootedEffect>());
            Assert.False(f.North.HasEffect<RootedEffect>()); f.Ability.CooldownRemaining = 0;
            Assert.True(f.Zone.RemoveEntity(f.East));
            Assert.True(f.Cast(null, includeTarget: false)); Assert.True(f.North.HasEffect<RootedEffect>());
        }

        [TestCase(true)] [TestCase(false)]
        public void PyroclasmRequiresCurrentBurningPhysicalObject(bool destroyed)
        {
            var f = new SkillTargetReliabilityFixture("Pyromancy_Pyroclasm");
            f.East.Tags.Remove("Creature");
            if (destroyed) f.East.AddPart(new DestructiblePart { HP = 0, MaxHP = 10, Gone = true });
            f.AssertFreeRefusal(f.Zone.GetCell(11, 10));
        }

        [Test]
        public void DismissChosenNonFollowerDoesNotDismissOtherEligibleFollower()
        {
            var f = new SkillTargetReliabilityFixture("Persuasion_Dismiss");
            f.East.GetEffect<RecruitedEffect>().Dismiss(f.Actor);
            f.AssertFreeRefusal(f.Zone.GetCell(11, 10));
        }

        [Test]
        public void RecruitChosenHostileDoesNotRecruitTheNeutralBystander()
        {
            var f = new SkillTargetReliabilityFixture("Persuasion_Recruit");
            f.East.GetPart<BrainPart>().PersonalEnemies.Add(f.Actor);
            f.AssertFreeRefusal(f.Zone.GetCell(11, 10));
        }
    }
}
