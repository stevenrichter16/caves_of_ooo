using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Playtest bug: the world-action menu is reachable from look mode,
    /// whose cursor sits anywhere on the map — and only Break carried a
    /// reach check. The player could open chests, chat with NPCs, draw
    /// well water, throw distant items, and pocket loot from across the
    /// zone. The unified gate in InputHandler's dispatch now asks
    /// <see cref="InputHandler.ActionRequiresReach"/>; this pins that
    /// contract (the wiring itself is Presentation-layer input, verified
    /// by the shared IsWithinStrikeReach seam Break already used).
    /// </summary>
    public class WorldActionReachTests
    {
        [TestCase("Chat")]
        [TestCase("OpenContainer")]
        [TestCase("Take")]
        [TestCase("Throw")]
        [TestCase("Harvest")]
        [TestCase("DrawWaterAtWell")]
        [TestCase("Break")]
        public void ActingOnTheWorld_RequiresBeingThere(string command)
        {
            Assert.IsTrue(InputHandler.ActionRequiresReach(command),
                command + " acts on the world; telekinesis is not a shipped feature");
        }

        [Test]
        public void TakeItemsPileRow_RequiresBeingThere()
        {
            Assert.IsTrue(InputHandler.ActionRequiresReach(WorldInteractionSystem.ViewPileCommand),
                "the pile-pickup row takes items; taking needs hands");
        }

        [Test]
        public void LookingAndMenuNavigation_StayRangeless()
        {
            Assert.IsFalse(InputHandler.ActionRequiresReach("Examine"),
                "examining at a distance is what look mode IS");
            Assert.IsFalse(InputHandler.ActionRequiresReach("CraftNoop"));
            Assert.IsFalse(InputHandler.ActionRequiresReach(WorldInteractionSystem.PickCellCommand));
            Assert.IsFalse(InputHandler.ActionRequiresReach(
                WorldInteractionSystem.PickTargetCommandPrefix + "some-entity-id"),
                "picking a row from the what's-here list is navigation, not action");
            Assert.IsFalse(InputHandler.ActionRequiresReach(null));
        }
    }
}
