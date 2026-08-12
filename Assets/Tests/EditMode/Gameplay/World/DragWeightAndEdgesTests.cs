using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D4/D5 of Drag &amp; Haul (<c>Docs/DRAG-AND-HAUL.md</c> §7) — hauling
    /// costs you something, and the link survives contact with the rest of
    /// the game.
    ///
    /// <para><b>The cost is a Speed penalty</b>, not a per-action charge.
    /// The verification sweep (§11) established that there is no per-action
    /// cost in <c>TurnManager</c> to multiply — every action deducts the same
    /// fixed <c>ActionThreshold</c>. Carried weight already expresses "this
    /// is slowing me down" as <c>speed.Penalty</c>
    /// (<c>InventoryPart.RefreshHandlingCarryPenalty</c>), so hauling uses
    /// the same lever. Mirroring an existing mechanism beats inventing a
    /// parallel one.</para>
    ///
    /// <para>The edges here are the ones that would be actual bugs rather
    /// than missing polish: a penalty that never lifts, a link that survives
    /// death, and a link that does not survive a save.</para>
    /// </summary>
    public class DragWeightAndEdgesTests
    {
        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
        }

        private static Entity Hauler(int strength = 16, int speed = 100)
        {
            var e = new Entity { ID = "hauler", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Statistics["Strength"] = new Stat
            { Owner = e, Name = "Strength", BaseValue = strength, Min = 0, Max = 40 };
            e.Statistics["Speed"] = new Stat
            { Owner = e, Name = "Speed", BaseValue = speed, Min = 0, Max = 999 };
            e.AddPart(new PhysicsPart { Weight = 100, Takeable = false });
            return e;
        }

        private static Entity Load(int weight = 120, string id = "load")
        {
            var e = new Entity { ID = id, BlueprintName = "MillStone" };
            e.AddPart(new PhysicsPart { Weight = weight, Takeable = false });
            e.AddPart(new HandlingPart { Weight = weight, Carryable = false });
            return e;
        }

        private static (Zone zone, Entity actor, Entity load) Hauling(int weight = 120)
        {
            var zone = new Zone("T");
            var actor = Hauler();
            var load = Load(weight);
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(load, 6, 5);
            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(actor, load, zone));
            return (zone, actor, load);
        }

        private static int Speed(Entity e) => e.GetStat("Speed").Value;

        // ════════════════════════════════════════════════════════
        // Hauling slows you down
        // ════════════════════════════════════════════════════════

        [Test]
        public void TakingHoldSlowsYou()
        {
            var actor = Hauler();
            int before = Speed(actor);
            var load = Load();
            var zone = new Zone("T");
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(load, 6, 5);

            DragSystem.TryGrab(actor, load, zone);

            Assert.Less(Speed(actor), before, "a millstone in tow must cost something");
        }

        [Test]
        public void LettingGoGivesTheSpeedBack()
        {
            // The invariant that matters most: a penalty that does not lift
            // is a permanent character debuff shipped by accident.
            var actor = Hauler();
            int before = Speed(actor);
            var load = Load();
            var zone = new Zone("T");
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(load, 6, 5);

            DragSystem.TryGrab(actor, load, zone);
            DragSystem.Release(actor);

            Assert.AreEqual(before, Speed(actor), "exactly the speed you started with");
        }

        [Test]
        public void SlippingAlsoGivesTheSpeedBack()
        {
            // The other exit from the hauling state. Missing this is how you
            // ship a permanent debuff that only triggers on an edge case.
            var (zone, actor, load) = Hauling();
            int hauling = Speed(actor);

            var wall = new Entity { ID = "rubble", BlueprintName = "Rock" };
            wall.AddPart(new PhysicsPart { Solid = true, Takeable = false });
            zone.AddEntity(wall, 5, 5);
            MovementSystem.TryMoveTo(actor, zone, 4, 5);

            Assert.IsFalse(DragSystem.IsDragging(actor), "precondition: the grip broke");
            Assert.Greater(Speed(actor), hauling, "and the penalty lifted with it");
            Assert.AreEqual(100, Speed(actor));
        }

        [Test]
        public void AHeavierLoadSlowsYouMore()
        {
            // Both must sit inside the drag cap (Strength 16 hauls 128) or
            // the heavier one is refused and the test proves nothing.
            var (_, lightHauler, _) = Hauling(weight: 60);
            var (_, heavyHauler, _) = Hauling(weight: 120);

            Assert.Greater(Speed(lightHauler), Speed(heavyHauler),
                "120 must cost more than 60, or the weight is decoration");
        }

        [Test]
        public void ThePenaltyNeverStopsYouCompletely()
        {
            // A floor exists so an unlucky combination cannot pin the player
            // in place forever, which is unrecoverable without the menu.
            var zone = new Zone("T");
            var actor = Hauler(strength: 40);
            var load = Load(weight: 320);
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(load, 6, 5);

            DragSystem.TryGrab(actor, load, zone);

            Assert.Greater(Speed(actor), 0, "you can always still move, however slowly");
        }

        [Test]
        public void GrabbingTwiceDoesNotStackThePenalty()
        {
            // TryGrab refuses the second grab (HandsFull), but if the
            // penalty were applied before that gate it would accumulate.
            var (zone, actor, load) = Hauling();
            int once = Speed(actor);

            DragSystem.TryGrab(actor, load, zone);
            DragSystem.TryGrab(actor, Load(id: "other"), zone);

            Assert.AreEqual(once, Speed(actor));
        }

        // ════════════════════════════════════════════════════════
        // Edges that would be real bugs
        // ════════════════════════════════════════════════════════

        [Test]
        public void DyingWhileHaulingDropsTheLoad()
        {
            // Otherwise a corpse keeps a millstone reserved forever and
            // nobody else can ever pick it up.
            var (zone, actor, load) = Hauling();

            actor.FireEventAndRelease(GameEvent.New("Died"));

            Assert.IsFalse(DragSystem.IsDragging(actor));
            Assert.IsFalse(DragSystem.IsBeingDragged(load), "the load is free again");
        }

        [Test]
        public void ALoadRemovedFromTheZoneDoesNotLeaveADanglingLink()
        {
            // Anything that despawns a load — a fire, a script, a cleanup
            // pass — must not leave the hauler holding a reference to
            // something no longer in the world.
            var (zone, actor, load) = Hauling();

            zone.RemoveEntity(load);
            DragSystem.ValidateLink(actor, zone);

            Assert.IsFalse(DragSystem.IsDragging(actor));
        }

        [Test]
        public void AHaulerWhoLeftTheZoneDropsTheLoad()
        {
            // Zone transition. The load cannot come along (it lives in the
            // old zone's grid), so the link must end rather than span zones.
            var (zone, actor, load) = Hauling();

            zone.RemoveEntity(actor);
            DragSystem.ValidateLink(actor, zone);

            Assert.IsFalse(DragSystem.IsDragging(actor));
            Assert.IsFalse(DragSystem.IsBeingDragged(load));
        }

        [Test]
        public void ValidateLinkLeavesAHealthyLinkAlone()
        {
            // Counter-check: a validator that clears everything would pass
            // all three tests above and break the feature.
            var (zone, actor, load) = Hauling();

            DragSystem.ValidateLink(actor, zone);

            Assert.IsTrue(DragSystem.IsDragging(actor));
            Assert.AreSame(load, DragSystem.GetDragged(actor));
        }

        // ════════════════════════════════════════════════════════
        // Save / load
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheLinkSurvivesASaveAndLoad()
        {
            // DragPart and DraggedPart hold Entity fields. The save graph
            // stores those by ID and resolves them on load; this is the
            // first test in the drag feature that proves it, rather than
            // citing the sweep.
            var (zone, actor, load) = Hauling();

            // The token graph is the helper that carries REFERENCED entities
            // through the stream; RoundTripEntity alone saves one entity, so
            // any cross-entity reference resolves to null and the test would
            // report a bug the save system does not have.
            var reloadedActor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);

            var part = reloadedActor.GetPart<DragPart>();
            Assert.IsNotNull(part, "the hauler still has a DragPart after loading");
            Assert.IsNotNull(part.Dragged, "and it still points at a load");
            Assert.AreEqual(load.ID, part.Dragged.ID);
        }
    }
}
