using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class RepairDoorAffordanceTests
    {
        sealed class Fixture
        {
            public readonly Zone Zone = new Zone("repair-door-affordance");
            public readonly Entity Player, Owner;
            public Fixture(string blueprint, bool repaired)
            {
                var factory = new EntityFactory();
                factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
                Player = new Entity { ID = "repair-hint-player", BlueprintName = "Player" };
                Player.SetTag("Player"); Player.AddPart(new PhysicsPart()); Player.AddPart(new InventoryPart());
                Player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 30, Max = 30, Owner = Player };
                Assert.True(Zone.AddEntity(Player, 10, 10));
                Owner = factory.CreateEntity(blueprint); Assert.NotNull(Owner);
                if (Owner.GetPart<RepairablePart>() is RepairablePart repair) repair.Repaired = repaired;
                Assert.True(Zone.AddEntity(Owner, 11, 10));
                Zone.GetCell(11, 10).Explored = Zone.GetCell(11, 10).IsVisible = true;
            }
            public WorldAffordance? Hint() => WorldAffordanceQuery.Find(Player, Zone, true, 11, 10);
            public string DoorCommand => Owner.GetPart<DoorPart>().IsClosed ? DoorPart.OpenCommand : DoorPart.CloseCommand;
        }

        [TestCase("GleanersBuckledWicket", false)] [TestCase("GleanersBuckledWicket", true)]
        [TestCase("RepairWoodenGate", false)] [TestCase("RepairWoodenGate", true)]
        public void CurrentDamageControlsDoorHintWithoutDispatchingOrChangingState(string blueprint, bool repaired)
        {
            var f = new Fixture(blueprint, repaired); var door = f.Owner.GetPart<DoorPart>();
            bool open = door.IsOpen; int clock = WorldClock.CurrentTick;
            Assert.AreEqual(repaired, door.CanOperate(f.Player, f.Zone), "The ordinary operation already knows the repair requirement.");
            var actorTrap = new QueryTrap(); var ownerTrap = new QueryTrap();
            f.Player.AddPart(actorTrap); f.Owner.AddPart(ownerTrap);
            for (int i = 0; i < 3; i++)
            {
                var hint = f.Hint();
                Assert.AreEqual(repaired, hint.HasValue, "A damaged door must not advertise an unavailable open/close action.");
                if (repaired) Assert.AreEqual(f.DoorCommand, hint.Value.Command);
            }
            Assert.AreEqual(0, actorTrap.Calls + ownerTrap.Calls);
            Assert.AreEqual(repaired, f.Owner.GetPart<RepairablePart>().Repaired);
            Assert.AreEqual(open, door.IsOpen); Assert.AreEqual(clock, WorldClock.CurrentTick);
        }

        [TestCase("GleanersBuckledWicket", false)] [TestCase("GleanersBuckledWicket", true)]
        [TestCase("RepairWoodenGate", false)] [TestCase("RepairWoodenGate", true)]
        public void RealMenuKeepsRepairAndExamineButOffersDoorUseOnlyWhenRestored(string blueprint, bool repaired)
        {
            var f = new Fixture(blueprint, repaired); bool open = f.Owner.GetPart<DoorPart>().IsOpen;
            int clock = WorldClock.CurrentTick;
            var rows = WorldInteractionSystem.GatherActions(f.Owner, f.Player);
            Assert.AreEqual(repaired, rows.Any(a => a.Command == f.DoorCommand));
            Assert.AreEqual(!repaired, rows.Any(a => a.Command == RepairablePart.RepairCommand));
            Assert.True(rows.Any(a => a.Command == "Examine"));
            Assert.AreEqual(open, f.Owner.GetPart<DoorPart>().IsOpen);
            Assert.AreEqual(repaired, f.Owner.GetPart<RepairablePart>().Repaired);
            Assert.AreEqual(clock, WorldClock.CurrentTick);
        }

        [TestCase("VillageDoor")] [TestCase("SpreadFieldGate")]
        public void OrdinaryDoorsWithoutRepairFaultKeepCurrentMenuAndHint(string blueprint)
        {
            var f = new Fixture(blueprint, false); Assert.Null(f.Owner.GetPart<RepairablePart>());
            Assert.True(f.Owner.GetPart<DoorPart>().CanOperate(f.Player, f.Zone));
            Assert.True(WorldInteractionSystem.GatherActions(f.Owner, f.Player).Any(a => a.Command == f.DoorCommand));
            Assert.True(f.Hint().HasValue); Assert.AreEqual(f.DoorCommand, f.Hint().Value.Command);
        }

        sealed class QueryTrap : Part
        {
            public int Calls;
            public override bool HandleEvent(GameEvent e)
            { Calls++; throw new InvalidOperationException("Read-only hint dispatched " + e.ID); }
        }
    }
}
