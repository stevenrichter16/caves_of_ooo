using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class FiftyWorldServiceTests : FiftyWorldFixture
    {
        [Test] public void NearbyWorkingWellCuresActualThirst()
        { var well = Place("Well"); Actor.ApplyEffect(new ParchedEffect { Stacks = 3 }); Assert.True(Act(well, "DrawWaterAtWell")); Assert.False(Actor.HasEffect<ParchedEffect>()); }
        [TestCase("remote")] [TestCase("removed")] [TestCase("dead")]
        public void WellSelectionMustStillHaveALivingNearbyPhysicalActor(string failure)
        {
            var well = Place("Well"); Actor.ApplyEffect(new ParchedEffect { Stacks = 2 });
            if (failure == "remote") Zone.MoveEntity(well, 20, 10);
            else if (failure == "removed") Zone.RemoveEntity(well); else Actor.GetStat("Hitpoints").BaseValue = 0;
            Assert.False(Act(well, "DrawWaterAtWell")); Assert.AreEqual(2, Actor.GetEffect<ParchedEffect>().Stacks);
            Assert.False(Actions(well).Any(a => a.Command == "DrawWaterAtWell"));
        }
        [TestCase("available", true)] [TestCase("empty", false)] [TestCase("frozen", false)]
        public void FiniteSeepDrinkingUsesItsActualWaterInsteadOfTheUnlimitedWellFallback(string state, bool expected)
        {
            var seep = Place("GroveSeep"); var pool = seep.GetPart<LiquidPoolPart>(); pool.Volume = state == "empty" ? 0 : 2;
            if (state == "frozen") { Zone.TileState.WriteCoating(11, 10, "ice", 5); Assert.True(Zone.TileState.HasCoating(11, 10, "ice")); }
            Actor.ApplyEffect(new ParchedEffect { Stacks = 2 }); int before = pool.Volume;
            Assert.AreEqual(expected, Act(seep, "DrawWaterAtWell")); Assert.AreEqual(before - (expected ? 1 : 0), pool.Volume);
            Assert.AreEqual(!expected, Actor.HasEffect<ParchedEffect>());
        }
        [Test] public void FailedOuterWellActionRestoresTheSameThirstPayload()
        {
            var well = Place("Well"); var thirst = new ParchedEffect { Stacks = 3 }; Actor.ApplyEffect(thirst); FailAfter();
            Assert.False(Act(well, "DrawWaterAtWell")); Assert.AreSame(thirst, Actor.GetEffect<ParchedEffect>()); Assert.AreEqual(3, thirst.Stacks);
        }
        [Test] public void SafeNearbyCampfireReallyHealsAndAdvancesSixtyTicks()
        {
            var fire = Place("Campfire"); Actor.GetStat("Hitpoints").BaseValue = 3; int tick = Clock.TickCount;
            Assert.True(Act(fire, "RestAtCampfire")); Assert.AreEqual(Actor.GetStat("Hitpoints").Max, Actor.GetStatValue("Hitpoints")); Assert.AreEqual(tick + 60, Clock.TickCount);
        }
        [TestCase("remote")] [TestCase("removed")] [TestCase("clock")] [TestCase("hostile")]
        public void RefusedCampfireRestIsNotAHandledPaidAction(string failure)
        {
            var fire = Place("Campfire"); Actor.GetStat("Hitpoints").BaseValue = 3; int tick = Clock.TickCount;
            if (failure == "remote") Zone.MoveEntity(fire, 20, 10); else if (failure == "removed") Zone.RemoveEntity(fire);
            else if (failure == "clock") WithoutClock();
            else { var enemy = new Entity { ID = "rest-enemy" }; enemy.AddPart(new PhysicsPart()); enemy.AddPart(new BrainPart()); enemy.GetPart<BrainPart>().SetPersonallyHostile(Actor, false); Assert.True(Zone.AddEntity(enemy, 12, 10)); }
            Assert.False(Act(fire, "RestAtCampfire")); Assert.AreEqual(3, Actor.GetStatValue("Hitpoints")); Assert.AreEqual(tick, Clock.TickCount);
        }
        [Test] public void CookingCoalsDoNotBecomeARestService()
        { var coals = Place("SpreadCookingCoals"); Assert.False(Act(coals, "RestAtCampfire")); Assert.False(Actions(coals).Any(a => a.Command == "RestAtCampfire")); }
    }
}
