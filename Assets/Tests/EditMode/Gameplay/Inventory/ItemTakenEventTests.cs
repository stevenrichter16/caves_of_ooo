using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M1 (Docs/QUEST-WORLD-PARTS.md — Q5.2/Q5.3 prerequisite) — the item-side
    /// <c>"Taken"</c> GameEvent. The acquisition commands (PickupCommand from
    /// the ground, TakeFromContainerCommand from a chest/corpse) fire
    /// <c>"Taken"</c> ON THE ITEM after a SUCCESSFUL add, naming the taker as
    /// the <c>"Actor"</c> parameter and the item as <c>"Item"</c> — the CoO
    /// analog of Qud's item-side <c>TakenEvent</c> (XRL.World.Parts.QuestStarter
    /// / CompleteQuestOnTaken hook it). This is the foundation the world-object
    /// quest Parts hook in M2/M3.
    ///
    /// Counter-checks: a FAILED acquisition (overweight, locked container)
    /// fires NO Taken — the event marks SUCCESSFUL acquisition only.
    /// </summary>
    public class ItemTakenEventTests
    {
        [SetUp]
        public void SetUp() => MessageLog.Clear();

        /// <summary>Captures "Taken" events fired on its owning entity.</summary>
        private sealed class TakenCapture : Part
        {
            public override string Name => "TakenCapture";
            public int Count;
            public Entity LastActor;
            public Entity LastItem;
            public override bool WantEvent(int eventID) => true;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Taken")
                {
                    Count++;
                    LastActor = e.GetParameter<Entity>("Actor");
                    LastItem = e.GetParameter<Entity>("Item");
                }
                return true;
            }
        }

        private static Entity Actor(int maxWeight = 150)
        {
            var a = new Entity { BlueprintName = "Taker" };
            a.Tags["Creature"] = "";
            a.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            a.AddPart(new InventoryPart { MaxWeight = maxWeight });
            return a;
        }

        private static (Entity item, TakenCapture cap) TakeableWithProbe(int weight = 5)
        {
            var item = new Entity { BlueprintName = "Relic" };
            item.Tags["Item"] = "";
            item.AddPart(new PhysicsPart { Takeable = true, Weight = weight });
            var cap = new TakenCapture();
            item.AddPart(cap);
            return (item, cap);
        }

        private static Entity Container()
        {
            var c = new Entity { BlueprintName = "Chest" };
            c.AddPart(new PhysicsPart { Solid = true });
            c.AddPart(new ContainerPart());
            return c;
        }

        // ════════════════ pickup from the ground ════════════════

        [Test]
        public void Pickup_FiresTakenOnItem_WithTaker()
        {
            var zone = new Zone();
            var actor = Actor();
            zone.AddEntity(actor, 5, 5);
            var (item, cap) = TakeableWithProbe();
            zone.AddEntity(item, 5, 5);

            var result = new InventoryCommandExecutor().Execute(
                new PickupCommand(item), new InventoryContext(actor, zone));

            Assert.IsTrue(result.Success, "pickup should succeed");
            Assert.AreEqual(1, cap.Count, "item receives exactly one Taken event on pickup");
            Assert.AreSame(actor, cap.LastActor, "Taken names the taker as Actor");
            Assert.AreSame(item, cap.LastItem, "Taken names the item itself as Item");
        }

        [Test]
        public void Pickup_WeightExceeded_DoesNotFireTaken()
        {
            // Counter-check: a FAILED pickup (rolled back to the zone) fires no
            // Taken — the event marks SUCCESSFUL acquisition only.
            var zone = new Zone();
            var actor = Actor(maxWeight: 10);
            zone.AddEntity(actor, 5, 5);
            var (heavy, cap) = TakeableWithProbe(weight: 20);
            zone.AddEntity(heavy, 5, 5);

            var result = new InventoryCommandExecutor().Execute(
                new PickupCommand(heavy), new InventoryContext(actor, zone));

            Assert.IsFalse(result.Success, "overweight pickup fails");
            Assert.AreEqual(0, cap.Count, "no Taken event on a failed/rolled-back pickup");
        }

        // ════════════════ take from a container ════════════════

        [Test]
        public void TakeFromContainer_FiresTakenOnItem_WithTaker()
        {
            var actor = Actor();
            var chest = Container();
            var (item, cap) = TakeableWithProbe();
            chest.GetPart<ContainerPart>().AddItem(item);

            var result = new InventoryCommandExecutor().Execute(
                new TakeFromContainerCommand(chest, item), new InventoryContext(actor));

            Assert.IsTrue(result.Success, "container take should succeed");
            Assert.AreEqual(1, cap.Count, "item receives exactly one Taken event on container take");
            Assert.AreSame(actor, cap.LastActor, "Taken names the taker as Actor");
            Assert.AreSame(item, cap.LastItem, "Taken names the item itself as Item");
        }

        [Test]
        public void TakeFromContainer_Locked_DoesNotFireTaken()
        {
            // Counter-check: a locked container blocks the take → no Taken.
            var actor = Actor();
            var chest = Container();
            chest.GetPart<ContainerPart>().Locked = true;
            var (item, cap) = TakeableWithProbe();
            chest.GetPart<ContainerPart>().AddItem(item);

            var result = new InventoryCommandExecutor().Execute(
                new TakeFromContainerCommand(chest, item), new InventoryContext(actor));

            Assert.IsFalse(result.Success, "take from a locked container fails");
            Assert.AreEqual(0, cap.Count, "no Taken event when the take is blocked");
        }

        // ════════════════ mutation resistance (step g) ════════════════

        [Test]
        public void Taken_FiresOnItem_NotOnActor()
        {
            // The defining contract: Taken is dispatched ON THE ITEM (Qud's
            // quest Part lives on the item), NOT on the actor. A mutation that
            // fired it on the actor instead would pass an actor-probe test but
            // break every item-side world-Part. Pin it: an actor-side probe
            // sees zero Taken even though the pickup events fire on the actor.
            var zone = new Zone();
            var actor = Actor();
            var actorProbe = new TakenCapture();
            actor.AddPart(actorProbe);
            zone.AddEntity(actor, 5, 5);
            var (item, itemProbe) = TakeableWithProbe();
            zone.AddEntity(item, 5, 5);

            var result = new InventoryCommandExecutor().Execute(
                new PickupCommand(item), new InventoryContext(actor, zone));

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, itemProbe.Count, "the item sees Taken");
            Assert.AreEqual(0, actorProbe.Count, "the actor must NOT see Taken (item-side only)");
        }

        [Test]
        public void Pickup_TwoItems_EachFiresOwnTaken_NoCrosstalk()
        {
            // Cross-instance: picking up two items fires one Taken per item,
            // each naming its OWN item — no shared/leaked event state.
            var zone = new Zone();
            var actor = Actor();
            zone.AddEntity(actor, 5, 5);
            var (itemA, capA) = TakeableWithProbe();
            var (itemB, capB) = TakeableWithProbe();
            zone.AddEntity(itemA, 5, 5);
            zone.AddEntity(itemB, 5, 5);

            var exec = new InventoryCommandExecutor();
            exec.Execute(new PickupCommand(itemA), new InventoryContext(actor, zone));
            exec.Execute(new PickupCommand(itemB), new InventoryContext(actor, zone));

            Assert.AreEqual(1, capA.Count, "item A sees exactly one Taken");
            Assert.AreSame(itemA, capA.LastItem, "A's Taken names A");
            Assert.AreEqual(1, capB.Count, "item B sees exactly one Taken");
            Assert.AreSame(itemB, capB.LastItem, "B's Taken names B");
        }
    }
}
