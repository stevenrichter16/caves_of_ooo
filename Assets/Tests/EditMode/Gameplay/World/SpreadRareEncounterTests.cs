using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SpreadRareEncounterTests
    {
        [TestCase("SpreadHurdleCutter", "ShortSword", "LeatherCap")]
        [TestCase("SpreadDitchMate", "Cudgel", null)]
        public void OriginalPairOwnsOnlyItsAuthoredTierOneGear(string blueprint, string weapon, string cap)
        {
            using (var scope = new DensityLootTestScope())
            {
                Assert.IsTrue(scope.Factory.Blueprints.ContainsKey(blueprint), "Original encounter blueprint must exist.");
                var actor = scope.Factory.CreateEntity(blueprint);
                Assert.AreEqual(15, actor.GetStatValue("Hitpoints"));
                CollectionAssert.AreEquivalent(cap == null ? new[] { weapon } : new[] { weapon, cap },
                    DensityLootTestScope.Gear(actor).Select(e => e.BlueprintName));
                Assert.IsTrue(actor.GetPart<Body>().GetParts().Any(p => p.Equipped?.BlueprintName == weapon));
                Assert.IsTrue(actor.GetPart<CombatTacticsPart>().AssistAllies);
                Assert.AreEqual(4, actor.GetPart<CombatTacticsPart>().AssistRadius);
            }
        }

        [Test]
        public void BoundedFreshWorldSelectionHasAProductionOwner()
        {
            Assert.IsNotNull(typeof(OverworldZoneManager).GetProperty("RareEncounters"),
                "The current manager must own its bounded selection; presentation cannot initialize content.");
        }
    }
}
