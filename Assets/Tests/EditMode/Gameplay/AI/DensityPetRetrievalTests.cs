using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityPetRetrievalTests
    {
        private LogScope log;
        [SetUp] public void Setup() { log = new LogScope(); }
        [TearDown] public void Cleanup() { log?.Dispose(); }

        internal static Entity Creature(Zone zone, int x, int y, string id, bool retriever = false)
        {
            var actor = new Entity { ID = id, BlueprintName = retriever ? "PetDog" : "Villager" };
            actor.SetTag("Creature"); actor.SetTag("Faction", "Villagers");
            foreach (var pair in new[] { ("Hitpoints", 15), ("Strength", 10), ("Agility", 14), ("Speed", 100) })
                actor.Statistics[pair.Item1] = new Stat { Owner = actor, Name = pair.Item1, BaseValue = pair.Item2, Max = pair.Item2 };
            actor.AddPart(new PhysicsPart { Solid = true });
            actor.AddPart(new RenderPart { DisplayName = id });
            actor.AddPart(new InventoryPart { MaxWeight = 10 });
            actor.AddPart(new BrainPart { CurrentZone = zone, Rng = new Random(7), Passive = true, Wanders = false, WandersRandomly = false });
            if (retriever) actor.AddPart(new AIRetrieverPart { NoticeRadius = 10 });
            Assert.True(zone.AddEntity(actor, x, y));
            return actor;
        }
        internal static Entity Item(string id = "thrown", string blueprint = "Bone", int count = 1, bool weapon = false)
        {
            var item = new Entity { ID = id, BlueprintName = blueprint };
            item.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            item.AddPart(new HandlingPart { Carryable = true, Throwable = true, Weight = 1 });
            item.AddPart(new RenderPart { DisplayName = blueprint });
            item.AddPart(new StackerPart { StackCount = count });
            if (weapon) item.AddPart(new EquippablePart { Slot = "Hand" });
            return item;
        }
        internal static void Turn(Entity dog)
        {
            var e = GameEvent.New("TakeTurn");
            dog.FireEventAndRelease(e);
        }
        internal static void Advance(Entity dog, int count = 35)
        { for (int i = 0; i < count; i++) Turn(dog); }
        internal static void Land(Entity dog, Entity thrower, Entity item, Cell cell)
        {
            var e = GameEvent.New(ItemLandedEvent.ID);
            e.SetParameter("Item", (object)item); e.SetParameter("Thrower", (object)thrower); e.SetParameter("LandingCell", (object)cell);
            dog.FireEventAndRelease(e);
        }
        internal static GoFetchGoal Fetch(Entity dog) => dog.GetPart<BrainPart>().FindGoal<GoFetchGoal>();
        internal static void Returned(Zone zone, Entity dog, Entity thrower, Entity item)
        {
            Assert.NotNull(zone.GetEntityCell(item), "The actual fetched object must return to the floor.");
            Assert.AreSame(zone.GetEntityCell(dog), zone.GetEntityCell(item), "Normal drop places it at the actual dog's cell.");
            Assert.LessOrEqual(SpatialQuery.Distance(zone, dog, thrower), 1, "Return tracks the current thrower position.");
            Assert.IsNull(item.GetPart<PhysicsPart>().InInventory); Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);
            Assert.False(dog.GetPart<InventoryPart>().Contains(item)); Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
            Assert.IsNull(Fetch(dog));
        }

        [Test]
        public void ActualLandedEventReturnsTheSameObjectAndLeavesHoarderHomeUnchanged()
        {
            var z = new Zone("fetch"); var dog = Creature(z, 5, 5, "dog", true); var thrower = Creature(z, 9, 5, "thrower");
            var item = Item(); Assert.True(z.AddEntity(item, 6, 5));
            Land(dog, thrower, item, z.GetEntityCell(item)); Assert.NotNull(Fetch(dog)); Assert.False(Fetch(dog).ReturnHome);
            Advance(dog); Returned(z, dog, thrower, item);
            Assert.AreEqual(5, dog.GetPart<BrainPart>().StartingCellX, "Fetching does not relocate the dog's home.");
        }
        [Test]
        public void ReturnReplansForThrowerMovingAfterTheActualPickup()
        {
            var z = new Zone("moving"); var dog = Creature(z, 5, 5, "dog", true); var thrower = Creature(z, 8, 5, "thrower");
            var item = Item(); Assert.True(z.AddEntity(item, 5, 5)); Land(dog, thrower, item, z.GetEntityCell(item));
            Turn(dog); Assert.True(dog.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.True(MovementSystem.TryMove(thrower, z, 1, 1)); Assert.True(MovementSystem.TryMove(thrower, z, 1, 1));
            Advance(dog); Returned(z, dog, thrower, item);
        }
        [Test]
        public void FetchDoesNotMergeIntoTheDogsPreexistingCompatibleStack()
        {
            var z = new Zone("stack"); var dog = Creature(z, 5, 5, "dog", true); var thrower = Creature(z, 8, 5, "thrower");
            var resident = Item("resident", count: 3); Assert.True(dog.GetPart<InventoryPart>().AddObject(resident));
            var item = Item(); Assert.True(z.AddEntity(item, 5, 5)); Land(dog, thrower, item, z.GetEntityCell(item)); Turn(dog);
            Assert.AreEqual(3, resident.GetPart<StackerPart>().StackCount, "Previously owned units are not part of the fetch.");
            Assert.True(dog.GetPart<InventoryPart>().Objects.Contains(item)); Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
            Assert.AreSame(dog, item.GetPart<PhysicsPart>().InInventory);
        }
        [Test]
        public void FetchKeepsGoldPhysicalInsteadOfCreditingTheDogsPurse()
        {
            var z = new Zone("gold"); var dog = Creature(z, 5, 5, "dog", true); var thrower = Creature(z, 8, 5, "thrower");
            int purse = TradeSystem.GetDrams(dog); var item = Item(blueprint: "GoldCoin");
            Assert.True(z.AddEntity(item, 5, 5)); Land(dog, thrower, item, z.GetEntityCell(item)); Turn(dog);
            Assert.AreEqual(purse, TradeSystem.GetDrams(dog)); Assert.True(dog.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
        }
        [Test]
        public void FetchKeepsTheThrownWeaponCarriedRatherThanEquippingIt()
        {
            var z = new Zone("weapon"); var dog = Creature(z, 5, 5, "dog", true); var thrower = Creature(z, 8, 5, "thrower");
            var item = Item(blueprint: "Dagger", weapon: true); Assert.True(z.AddEntity(item, 5, 5));
            Land(dog, thrower, item, z.GetEntityCell(item)); Turn(dog);
            Assert.IsNull(item.GetPart<PhysicsPart>().Equipped); Assert.AreSame(dog, item.GetPart<PhysicsPart>().InInventory);
            Assert.True(dog.GetPart<InventoryPart>().Objects.Contains(item));
        }

        [TestCase("null-thrower")][TestCase("dead-thrower")][TestCase("removed-thrower")]
        [TestCase("foreign-cell")][TestCase("different-source-cell")][TestCase("dead-dog")]
        public void InvalidLandedSourceOrRecipientNeverStartsAnAcquisition(string mutation)
        {
            var z = new Zone("authority"); var dog = Creature(z, 5, 5, "dog", true); var thrower = Creature(z, 8, 5, "thrower");
            var item = Item(); Assert.True(z.AddEntity(item, 6, 5)); var cell = z.GetEntityCell(item);
            if (mutation == "null-thrower") thrower = null;
            if (mutation == "dead-thrower") thrower.GetStat("Hitpoints").BaseValue = 0;
            if (mutation == "removed-thrower") Assert.True(z.RemoveEntity(thrower));
            if (mutation == "foreign-cell") cell = new Zone("foreign").GetCell(6, 5);
            if (mutation == "different-source-cell") cell = z.GetCell(7, 5);
            if (mutation == "dead-dog") dog.GetStat("Hitpoints").BaseValue = 0;
            Land(dog, thrower, item, cell);
            Assert.IsNull(Fetch(dog)); Assert.AreSame(item, z.GetCell(6, 5).Objects.Single(x => x == item));
        }
        [TestCase(false)][TestCase(true)]
        public void AlliancePolicyStillControlsRealLivingThrowers(bool alliesOnly)
        {
            var z = new Zone("alliance"); var dog = Creature(z, 5, 5, "dog", true); var thrower = Creature(z, 8, 5, "thrower");
            dog.GetPart<AIRetrieverPart>().AlliesOnly = alliesOnly;
            dog.GetPart<BrainPart>().PersonalEnemies.Add(thrower);
            var item = Item(); Assert.True(z.AddEntity(item, 6, 5)); Land(dog, thrower, item, z.GetEntityCell(item));
            Assert.AreEqual(!alliesOnly, Fetch(dog) != null);
        }
        [Test]
        public void DuplicateLandedEventKeepsTheOriginalGoalAndPayload()
        {
            var z = new Zone("duplicate"); var dog = Creature(z, 5, 5, "dog", true); var thrower = Creature(z, 8, 5, "thrower");
            var item = Item(); Assert.True(z.AddEntity(item, 6, 5)); Land(dog, thrower, item, z.GetEntityCell(item)); var first = Fetch(dog);
            var other = Item("other"); Assert.True(z.AddEntity(other, 7, 5)); Land(dog, thrower, other, z.GetEntityCell(other));
            Assert.AreSame(first, Fetch(dog)); Assert.AreSame(item, first.Item); Assert.AreEqual(1, dog.GetPart<BrainPart>().GoalCount);
        }

        [TestCase("stack")][TestCase("gold")][TestCase("weapon")]
        public void OrdinaryPickupRetainsItsExistingAcquisitionSemantics(string kind)
        {
            var z = new Zone("ordinary"); var actor = Creature(z, 5, 5, "actor");
            var resident = Item("resident", count: 3); Assert.True(actor.GetPart<InventoryPart>().AddObject(resident));
            var item = Item(blueprint: kind == "gold" ? "GoldCoin" : kind == "weapon" ? "Dagger" : "Bone", weapon: kind == "weapon");
            Assert.True(z.AddEntity(item, 5, 5)); int purse = TradeSystem.GetDrams(actor);
            Assert.True(InventorySystem.Pickup(actor, item, z));
            if (kind == "stack") { Assert.AreEqual(4, resident.GetPart<StackerPart>().StackCount); Assert.AreEqual(0, item.GetPart<StackerPart>().StackCount); }
            if (kind == "gold") { Assert.AreEqual(purse + 5, TradeSystem.GetDrams(actor)); Assert.AreEqual(0, item.GetPart<StackerPart>().StackCount); }
            if (kind == "weapon") { Assert.AreSame(actor, item.GetPart<PhysicsPart>().Equipped); Assert.False(actor.GetPart<InventoryPart>().Objects.Contains(item)); }
        }
        [Test]
        public void ExistingHoarderModeStillKeepsTheItemAndWalksToItsOwnHome()
        {
            var z = new Zone("home"); var dog = Creature(z, 5, 5, "hoarder"); var brain = dog.GetPart<BrainPart>();
            brain.StartingCellX = 9; brain.StartingCellY = 5;
            var item = Item(); Assert.True(z.AddEntity(item, 5, 5)); brain.PushGoal(new GoFetchGoal(item, true)); Advance(dog);
            Assert.AreEqual((9, 5), z.GetEntityPosition(dog)); Assert.True(dog.GetPart<InventoryPart>().Objects.Contains(item)); Assert.IsNull(z.GetEntityCell(item));
        }
        [TestCase(false)][TestCase(true)]
        public void SaveRoundTripRetainsActiveFetchProgressRatherThanRestartingPickup(bool carried)
        {
            var z = new Zone("saved"); var dog = Creature(z, 5, 5, "dog"); var brain = dog.GetPart<BrainPart>();
            brain.StartingCellX = 9; brain.StartingCellY = 5;
            var item = Item(); Assert.True(z.AddEntity(item, carried ? 5 : 8, 5)); var goal = new GoFetchGoal(item, true); brain.PushGoal(goal);
            goal.TakeAction(); string before = goal.GetDetails(); Assert.False(goal.Finished());
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(dog); var saved = loaded.GetPart<BrainPart>().FindGoal<GoFetchGoal>();
            Assert.NotNull(saved); Assert.AreNotSame(goal, saved); Assert.AreNotSame(item, saved.Item);
            Assert.AreEqual(before, saved.GetDetails(), "Serialized goal progress must not be constructor-defaulted.");
            if (carried) Assert.AreSame(loaded, saved.Item.GetPart<PhysicsPart>().InInventory);
        }

        internal sealed class LogScope : IDisposable
        {
            private readonly Action<string> observer = MessageLog.OnMessage;
            private readonly Func<int> ticks = MessageLog.TickProvider;
            private readonly List<(IList list, object[] items)> lists = new List<(IList, object[])>();
            private readonly int next, flash;
            private static readonly BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
            public LogScope()
            {
                foreach (string name in new[] { "Messages", "Ticks", "Serials" })
                { var list = (IList)typeof(MessageLog).GetField(name, Flags).GetValue(null); lists.Add((list, list.Cast<object>().ToArray())); }
                next = (int)typeof(MessageLog).GetField("NextSerial", Flags).GetValue(null); flash = MessageLog.FlashStamp;
                MessageLog.OnMessage = null; MessageLog.TickProvider = null;
            }
            public void Dispose()
            {
                foreach (var row in lists) { row.list.Clear(); foreach (var item in row.items) row.list.Add(item); }
                typeof(MessageLog).GetField("NextSerial", Flags).SetValue(null, next); MessageLog.FlashStamp = flash;
                MessageLog.OnMessage = observer; MessageLog.TickProvider = ticks;
            }
        }
    }
}
