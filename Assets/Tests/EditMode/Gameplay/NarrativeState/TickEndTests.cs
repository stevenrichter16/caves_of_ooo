using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Gameplay.NarrativeState
{
    /// <summary>
    /// M4a TDD tests: TickEnd GameEvent fires on world entity after each EndTurn.
    /// </summary>
    public class TickEndTests
    {
        [TearDown]
        public void TearDown()
        {
            TurnManager.World = null;
        }

        [Test]
        public void EndTurn_FiresTickEndOnWorldEntity()
        {
            var world = new Entity { BlueprintName = "World" };
            var counter = new TickEndCounterPart();
            world.AddPart(counter);
            TurnManager.World = world;

            var turnManager = new TurnManager();
            var actor = new Entity { BlueprintName = "Player" };
            turnManager.AddEntity(actor);

            turnManager.EndTurn(actor);

            Assert.AreEqual(1, counter.TickEndCount,
                "TickEnd event should have fired on world entity after EndTurn");
        }

        [Test]
        public void EndTurn_NoWorldEntity_DoesNotThrow()
        {
            TurnManager.World = null;
            var turnManager = new TurnManager();
            var actor = new Entity { BlueprintName = "Player" };
            turnManager.AddEntity(actor);

            Assert.DoesNotThrow(() => turnManager.EndTurn(actor));
        }

        [Test]
        public void EndTurn_MultipleActors_FiresTickEndEachTime()
        {
            var world = new Entity { BlueprintName = "World" };
            var counter = new TickEndCounterPart();
            world.AddPart(counter);
            TurnManager.World = world;

            var turnManager = new TurnManager();
            var actorA = new Entity { BlueprintName = "Player" };
            var actorB = new Entity { BlueprintName = "NPC" };
            turnManager.AddEntity(actorA);
            turnManager.AddEntity(actorB);

            turnManager.EndTurn(actorA);
            turnManager.EndTurn(actorB);

            Assert.AreEqual(2, counter.TickEndCount);
        }

        // --- Test helper ---

        private sealed class TickEndCounterPart : Part
        {
            public int TickEndCount;

            private static readonly int TickEndEventID = GameEvent.GetID("TickEnd");

            public override bool WantEvent(int eventID) => eventID == TickEndEventID;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "TickEnd")
                    TickEndCount++;
                return true;
            }
        }
    }
}
