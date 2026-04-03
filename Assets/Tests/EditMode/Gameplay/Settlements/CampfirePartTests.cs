using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class CampfirePartTests
    {
        private Zone _zone;

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            _zone = new Zone("TestZone");
            SettlementRuntime.ActiveZone = _zone;
        }

        [TearDown]
        public void TearDown()
        {
            SettlementRuntime.Reset();
        }

        [Test]
        public void CampfireFlickers_BetweenRedAndYellow()
        {
            Entity campfire = CreateCampfire();

            int redCount = 0;
            int yellowCount = 0;
            for (int i = 0; i < 65; i++)
            {
                var e = CreateRenderEvent(campfire);
                campfire.FireEvent(e);
                string color = e.GetStringParameter("ColorString", "&R");
                if (color == "&Y") yellowCount++;
                else redCount++;
            }

            Assert.Greater(redCount, 0, "Should have red frames");
            Assert.Greater(yellowCount, 0, "Should have yellow flicker frames");
            Assert.Greater(redCount, yellowCount, "Red should be more common than yellow");
        }

        [Test]
        public void ProximityMessage_ShowsOnceWhenPlayerAdjacent()
        {
            Entity campfire = CreateCampfire();
            _zone.AddEntity(campfire, 10, 10);

            Entity player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player", "");
            _zone.AddEntity(player, 11, 10);

            MessageLog.Clear();

            campfire.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(1, MessageLog.Count);
            Assert.AreEqual("The campfire crackles warmly.", MessageLog.GetLast());

            campfire.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(1, MessageLog.Count, "Should not duplicate proximity message");
        }

        [Test]
        public void ProximityMessage_DoesNotShow_WhenPlayerFarAway()
        {
            Entity campfire = CreateCampfire();
            _zone.AddEntity(campfire, 10, 10);

            Entity player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player", "");
            _zone.AddEntity(player, 20, 20);

            MessageLog.Clear();

            campfire.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(0, MessageLog.Count);
        }

        [Test]
        public void ResetProximityMessage_AllowsMessageAgain()
        {
            Entity campfire = CreateCampfire();
            _zone.AddEntity(campfire, 10, 10);

            Entity player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player", "");
            _zone.AddEntity(player, 11, 10);

            MessageLog.Clear();

            campfire.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(1, MessageLog.Count);

            campfire.GetPart<CampfirePart>().ResetProximityMessage();
            campfire.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(2, MessageLog.Count);
        }

        private static Entity CreateCampfire()
        {
            Entity campfire = new Entity { BlueprintName = "Campfire" };
            campfire.AddPart(new RenderPart { RenderString = "*", ColorString = "&R", DisplayName = "campfire" });
            campfire.AddPart(new CampfirePart());
            return campfire;
        }

        private static GameEvent CreateRenderEvent(Entity entity)
        {
            var render = entity.GetPart<RenderPart>();
            var e = GameEvent.New("Render");
            e.SetParameter("Entity", (object)entity);
            e.SetParameter("RenderPart", (object)render);
            e.SetParameter("ColorString", render.ColorString ?? "&R");
            e.SetParameter("DetailColor", render.DetailColor ?? "");
            return e;
        }
    }
}
