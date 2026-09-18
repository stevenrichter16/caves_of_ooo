using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastContentWorldTests
    {
        private EntityFactory factory;
        private Zone zone, previousZone;
        private Entity player, previousPlayer;
        private StoryletPart previousStorylets;
        [SetUp] public void Setup()
        {
            previousZone = SettlementRuntime.ActiveZone; previousPlayer = StoryletPart.LocalPlayer; previousStorylets = StoryletPart.Current;
            factory = MorrowfastTestWorld.Factory(); zone = new OverworldZoneManager(factory, 64).GetZone(MorrowfastSceneRuntime.ZoneID);
            player = MorrowfastTestWorld.Actor(factory, zone);
            SettlementRuntime.ActiveZone = zone; StoryletPart.LocalPlayer = player; StoryletPart.Current = new StoryletPart();
            MorrowfastTestWorld.OpenAllDoors(zone, player);
        }
        [TearDown] public void TearDown()
        { SettlementRuntime.ActiveZone = previousZone; StoryletPart.LocalPlayer = previousPlayer; StoryletPart.Current = previousStorylets; ConversationManager.EndConversation(); }
        private Entity Approach(string id)
        {
            var target = MorrowfastSceneRuntime.FindOwner(zone, id); var p = MorrowfastTestWorld.Approach(zone, player, target);
            Assert.IsTrue(zone.MoveEntity(player, p.x, p.y)); return target;
        }
        private bool Talk(string id, string action) => MorrowfastQuests.TryConversation(Approach(id), player, action);
        private bool Act(string id, string action, out int energy) => MorrowfastQuests.TryWorldAction(Approach(id), player, zone, action, out energy);
        private int Units(string bp) => player.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == bp).Sum(i => i.GetPart<StackerPart>()?.StackCount ?? 1);

        [Test] public void BellRequiresTheRealCordThenConsumesExactlyOneClayAndResolvesOnlyOnce()
        {
            Assert.IsTrue(Talk("north-guard-west", "accept-bell"));
            var inv = player.GetPart<InventoryPart>(); inv.AddObject(factory.CreateEntity("FireClay")); inv.AddObject(factory.CreateEntity("FireClay"));
            Assert.IsFalse(Act("north-oath-arch", MorrowfastQuests.RepairBellCommand, out int denied)); Assert.AreEqual(0, denied); Assert.AreEqual(2, Units("FireClay"));
            Assert.IsTrue(Act("rope-reserve-coil", MorrowfastQuests.InspectCordCommand, out int inspect)); Assert.AreEqual(1000, inspect);
            Assert.IsTrue(Act("north-oath-arch", MorrowfastQuests.RepairBellCommand, out int labor)); Assert.AreEqual(1000, labor); Assert.AreEqual(1, Units("FireClay"));
            var arch = MorrowfastSceneRuntime.FindOwner(zone, "north-oath-arch"); Assert.AreEqual("quiet", arch.GetProperty("MorrowfastBellMode"));
            Assert.IsFalse(Act("north-oath-arch", MorrowfastQuests.RepairBellCommand, out denied)); Assert.AreEqual(0, denied); Assert.AreEqual(1, Units("FireClay"));
            Assert.AreEqual(1, StoryletPart.Current.GetQuestState(MorrowfastQuests.BellQuestId).CurrentStageIndex);
            Assert.IsTrue(Talk("north-guard-west", "report-bell")); Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(MorrowfastQuests.BellQuestId));
            Assert.IsFalse(Talk("north-guard-west", "report-bell"));
            Assert.IsTrue(Act("north-oath-arch", MorrowfastQuests.LoudBellCommand, out labor)); Assert.AreEqual("loud", arch.GetProperty("MorrowfastBellMode"));
            Assert.AreEqual(1, Units("FireClay")); Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(MorrowfastQuests.BellQuestId));
        }
        [Test] public void BareClapperOffersAMaterialFreeResolutionAndMissingClayDoesNotAdvance()
        {
            Assert.IsTrue(Talk("north-guard-west", "accept-bell")); Assert.IsTrue(Act("rope-reserve-coil", MorrowfastQuests.InspectCordCommand, out _));
            int drams = TradeSystem.GetDrams(player);
            Assert.IsFalse(Act("north-oath-arch", MorrowfastQuests.RepairBellCommand, out int failed)); Assert.AreEqual(0, failed);
            Assert.AreEqual(0, player.GetIntProperty(MorrowfastQuests.BellWorked));
            Assert.IsTrue(Act("north-oath-arch", MorrowfastQuests.LoudBellCommand, out int labor)); Assert.AreEqual(1000, labor);
            Assert.AreEqual(drams, TradeSystem.GetDrams(player)); Assert.IsTrue(Talk("north-guard-west", "report-bell"));
        }
        [Test] public void RingingQuietAndLoudBellsChangesAwarenessOnTheActualLivingResidents()
        {
            Assert.IsTrue(Talk("north-guard-west", "accept-bell")); Assert.IsTrue(Act("rope-reserve-coil", MorrowfastQuests.InspectCordCommand, out _));
            player.GetPart<InventoryPart>().AddObject(factory.CreateEntity("FireClay")); Assert.IsTrue(Act("north-oath-arch", MorrowfastQuests.RepairBellCommand, out _));
            Assert.IsTrue(Act("north-oath-arch", MorrowfastQuests.RingBellCommand, out _));
            Assert.GreaterOrEqual(MorrowfastSceneRuntime.FindOwner(zone, "north-guard-east").GetPart<MorrowfastResidentPart>().LastBellTurn, 0);
            Assert.AreEqual(-1, MorrowfastSceneRuntime.FindOwner(zone, "southern-food-vendor").GetPart<MorrowfastResidentPart>().LastBellTurn);
            Assert.IsTrue(Act("north-oath-arch", MorrowfastQuests.LoudBellCommand, out _)); Assert.IsTrue(Act("north-oath-arch", MorrowfastQuests.RingBellCommand, out _));
            Assert.GreaterOrEqual(MorrowfastSceneRuntime.FindOwner(zone, "southern-food-vendor").GetPart<MorrowfastResidentPart>().LastBellTurn, 0);
        }
        [Test] public void SupperQuestMovesTheRealStoolWithinItsRoomAndCannotBeRepeated()
        {
            Assert.IsTrue(Talk("farra-sprig", "accept-supper")); Assert.IsFalse(Talk("farra-sprig", "report-supper"));
            var stool = Approach("guest-stool-south"); var before = zone.GetEntityPosition(stool);
            Assert.IsTrue(MorrowfastQuests.TryWorldAction(stool, player, zone, MorrowfastQuests.MoveStoolCommand, out int labor)); Assert.AreEqual(1000, labor);
            var after = zone.GetEntityPosition(stool); Assert.AreNotEqual(before, after);
            Assert.AreEqual("dry-hem-guesthouse", MorrowfastSceneDefinition.Load().RoomAt(after.x, after.y));
            Assert.IsFalse(zone.GetCell(before.x, before.y).BlocksMovement(player), "The actual old collision must be released.");
            Assert.IsTrue(Talk("farra-sprig", "report-supper")); Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(MorrowfastQuests.SupperQuestId));
            Assert.IsFalse(Act("guest-stool-south", MorrowfastQuests.MoveStoolCommand, out labor)); Assert.AreEqual(0, labor);
        }
        [Test] public void GuestRestUsesNativeHealingAndHostileRefusalWithoutChargingMoney()
        {
            var bed = Approach("guest-bed-west"); int drams = TradeSystem.GetDrams(player); var hp = player.GetStat("Hitpoints");
            hp.BaseValue = Math.Max(1, hp.Max - 5); int before = hp.Value;
            var hostile = factory.CreateEntity("Snapjaw"); hostile.GetPart<BrainPart>().SetPersonallyHostile(player);
            var pc = zone.GetEntityCell(player); zone.AddEntity(hostile, pc.X, pc.Y);
            Assert.IsFalse(MorrowfastQuests.TryWorldAction(bed, player, zone, MorrowfastQuests.RestCommand, out int energy)); Assert.AreEqual(0, energy); Assert.AreEqual(before, hp.Value);
            zone.RemoveEntity(hostile);
            Assert.IsTrue(MorrowfastQuests.TryWorldAction(bed, player, zone, MorrowfastQuests.RestCommand, out energy)); Assert.AreEqual(0, energy);
            Assert.AreEqual(hp.Max, hp.Value); Assert.AreEqual(drams, TradeSystem.GetDrams(player));
        }
        [Test] public void ADetachedOrForgedWorldOwnerCannotConsumeClayOrSetBellState()
        {
            Assert.IsTrue(Talk("north-guard-west", "accept-bell")); Assert.IsTrue(Act("rope-reserve-coil", MorrowfastQuests.InspectCordCommand, out _));
            player.GetPart<InventoryPart>().AddObject(factory.CreateEntity("FireClay")); var arch = Approach("north-oath-arch");
            var fake = factory.CreateEntity("PhysicalObject"); fake.ID = "morrowfast-owner:north-oath-arch"; MorrowfastQuests.AttachProp(fake, "north-oath-arch");
            var pc = zone.GetEntityCell(player); zone.AddEntity(fake, pc.X, pc.Y);
            Assert.IsFalse(MorrowfastQuests.TryWorldAction(fake, player, zone, MorrowfastQuests.RepairBellCommand, out int energy)); Assert.AreEqual(0, energy); Assert.AreEqual(1, Units("FireClay"));
            zone.RemoveEntity(fake); zone.RemoveEntity(arch);
            Assert.IsFalse(MorrowfastQuests.TryWorldAction(arch, player, zone, MorrowfastQuests.RepairBellCommand, out energy)); Assert.AreEqual(1, Units("FireClay"));
        }
        [Test] public void SupperWorkFromTheRealDoorApproachPreservesAccessToBothBedsAndTheWholeRoom()
        {
            Assert.IsTrue(Talk("farra-sprig", "accept-supper"));
            var stool = MorrowfastSceneRuntime.FindOwner(zone, "guest-stool-south");
            Assert.IsTrue(zone.MoveEntity(player, 31, 12));
            var before = MorrowfastTestWorld.Flood(zone, player);
            Assert.IsTrue(MorrowfastQuests.TryWorldAction(stool, player, zone, MorrowfastQuests.MoveStoolCommand, out int energy));
            Assert.AreEqual(1000, energy); var after = MorrowfastTestWorld.Flood(zone, player); var moved = zone.GetEntityPosition(stool);
            var room = MorrowfastSceneDefinition.Load().buildings.First(b => b.id == "dry-hem-guesthouse");
            foreach (var c in room.interior)
                if ((c.x, c.y) != moved && before.Contains((c.x, c.y)))
                    Assert.IsTrue(after.Contains((c.x, c.y)), "A formerly reachable guesthouse cell was sealed: " + (c.x, c.y));
            foreach (var owner in MorrowfastSceneDefinition.Load().owners.Where(o => o.roomId == room.id))
                MorrowfastTestWorld.Approach(zone, player, MorrowfastSceneRuntime.FindOwner(zone, owner.id));
            Assert.AreNotEqual((32, 11), moved, "The narrow north-room route must remain free.");
        }
        [Test] public void DirectStoolPlacementRejectsTheDoorwayChokePointWithoutChangingPositionOrQuest()
        {
            Assert.IsTrue(Talk("farra-sprig", "accept-supper")); zone.MoveEntity(player, 31, 12);
            var stool = MorrowfastSceneRuntime.FindOwner(zone, "guest-stool-south"); var before = zone.GetEntityPosition(stool);
            Assert.IsFalse(MorrowfastSceneRuntime.TryMoveFurniture(player, zone, "guest-stool-south", 32, 11));
            Assert.AreEqual(before, zone.GetEntityPosition(stool)); Assert.AreEqual(0, player.GetIntProperty(MorrowfastQuests.SupperMoved));
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(MorrowfastQuests.SupperQuestId));
            Assert.IsTrue(MorrowfastSceneRuntime.TryMoveFurniture(player, zone, "guest-stool-south", 30, 10));
            Assert.AreEqual((30, 10), zone.GetEntityPosition(stool));
            MorrowfastTestWorld.Approach(zone, player, MorrowfastSceneRuntime.FindOwner(zone, "guest-bed-west"));
            MorrowfastTestWorld.Approach(zone, player, MorrowfastSceneRuntime.FindOwner(zone, "guest-bed-east"));
        }
    }
}
