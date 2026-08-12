using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D2 of Drag &amp; Haul (<c>Docs/DRAG-AND-HAUL.md</c> §7) — taking hold
    /// of a thing, and letting go of it.
    ///
    /// <para>D1 answered "could this happen?" as pure arithmetic. D2 is the
    /// first slice with <b>state</b>, and state is where the interesting
    /// failures live: half-attached grabs, two people holding one millstone,
    /// a dragger who wanders off still linked to something across the
    /// room.</para>
    ///
    /// <para>The link is deliberately two-sided — <c>DragPart</c> on the
    /// hauler, <c>DraggedPart</c> on the load — because every question the
    /// game needs to ask is asked from one side or the other: "what am I
    /// dragging?" when the hauler moves, and "is anyone holding this?" when
    /// something happens to the load. A one-sided link would force a zone
    /// scan for half of those.</para>
    /// </summary>
    public class DragGrabTests
    {
        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
        }

        private static Entity Hauler(int strength = 16)
        {
            var e = new Entity { ID = "hauler", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Statistics["Strength"] = new Stat
            { Owner = e, Name = "Strength", BaseValue = strength, Min = 0, Max = 40 };
            return e;
        }

        /// <summary>A 120-weight anvil-ish load: too heavy to lift at any
        /// sane Strength, inside the drag cap from Strength 15 up.</summary>
        private static Entity Load(int weight = 120, string id = "load")
        {
            var e = new Entity { ID = id, BlueprintName = "MillStone" };
            e.AddPart(new PhysicsPart { Weight = weight, Takeable = false, Solid = true });
            e.AddPart(new HandlingPart { Weight = weight, Carryable = false });
            return e;
        }

        private static Zone Placed(Entity actor, Entity load, int ax, int ay, int lx, int ly)
        {
            var zone = new Zone("T");
            zone.AddEntity(actor, ax, ay);
            zone.AddEntity(load, lx, ly);
            return zone;
        }

        // ════════════════════════════════════════════════════════
        // Taking hold
        // ════════════════════════════════════════════════════════

        [Test]
        public void Grabbing_LinksBothSides()
        {
            var actor = Hauler();
            var load = Load();
            var zone = Placed(actor, load, 5, 5, 6, 5);

            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(actor, load, zone));

            Assert.AreSame(load, DragSystem.GetDragged(actor), "hauler must know its load");
            Assert.AreSame(actor, DragSystem.GetDragger(load), "load must know its hauler");
            Assert.IsTrue(DragSystem.IsDragging(actor));
            Assert.IsTrue(DragSystem.IsBeingDragged(load));
        }

        [Test]
        public void DiagonalCountsAsAdjacent()
        {
            // The 3×3 box, matching ForgePart.IsNearForge. Reaching a
            // millstone that is catty-corner is not a different act.
            var actor = Hauler();
            var load = Load();
            var zone = Placed(actor, load, 5, 5, 6, 6);
            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(actor, load, zone));
        }

        [Test]
        public void ReachingAcrossTheRoom_IsRefused_AndLeavesNothingBehind()
        {
            // Atomicity: a refused grab must not half-attach. If TryGrab
            // added DragPart before validating, this would leave a hauler
            // linked to a millstone three cells away.
            var actor = Hauler();
            var load = Load();
            var zone = Placed(actor, load, 5, 5, 8, 5);

            Assert.AreEqual(DragVerdict.NotAdjacent, DragSystem.TryGrab(actor, load, zone));

            Assert.IsFalse(DragSystem.IsDragging(actor));
            Assert.IsFalse(DragSystem.IsBeingDragged(load));
            Assert.IsNull(actor.GetPart<DragPart>(), "no orphan DragPart");
            Assert.IsNull(load.GetPart<DraggedPart>(), "no orphan DraggedPart");
        }

        [Test]
        public void TheD1RefusalsStillApply_AndAlsoLeaveNothingBehind()
        {
            // D2 gates on D1 rather than re-deriving it. A creature is
            // refused for being alive, not for being far away.
            var actor = Hauler();
            var person = Load();
            person.Tags["Creature"] = "";
            var zone = Placed(actor, person, 5, 5, 6, 5);

            Assert.AreEqual(DragVerdict.Living, DragSystem.TryGrab(actor, person, zone));
            Assert.IsFalse(DragSystem.IsDragging(actor));
            Assert.IsNull(person.GetPart<DraggedPart>());
        }

        [Test]
        public void TooHeavyIsReportedAsTooHeavy_NotAsSomeGenericNo()
        {
            var weakling = Hauler(strength: 4);      // hauls 32
            var load = Load(weight: 120);
            var zone = Placed(weakling, load, 5, 5, 6, 5);
            Assert.AreEqual(DragVerdict.TooHeavy, DragSystem.TryGrab(weakling, load, zone));
        }

        // ════════════════════════════════════════════════════════
        // One load per hauler, one hauler per load
        // ════════════════════════════════════════════════════════

        [Test]
        public void YouCannotGrabASecondThing()
        {
            var actor = Hauler();
            var first = Load(id: "first");
            var second = Load(id: "second");
            var zone = Placed(actor, first, 5, 5, 6, 5);
            zone.AddEntity(second, 4, 5);

            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(actor, first, zone));
            Assert.AreEqual(DragVerdict.HandsFull, DragSystem.TryGrab(actor, second, zone));

            Assert.AreSame(first, DragSystem.GetDragged(actor), "the first load is still held");
            Assert.IsFalse(DragSystem.IsBeingDragged(second));
        }

        [Test]
        public void TwoPeopleCannotHaulOneMillstone()
        {
            var first = Hauler();
            var second = Hauler();
            second.ID = "hauler2";
            var load = Load();
            var zone = Placed(first, load, 5, 5, 6, 5);
            zone.AddEntity(second, 7, 5);

            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(first, load, zone));
            Assert.AreEqual(DragVerdict.TakenByAnother, DragSystem.TryGrab(second, load, zone));

            Assert.AreSame(first, DragSystem.GetDragger(load));
            Assert.IsFalse(DragSystem.IsDragging(second));
        }

        [Test]
        public void RegrabbingWhatYouAlreadyHold_IsHandsFull_NotACorruptedLink()
        {
            // The self-referential case. A naive "already dragged?" check
            // that forgot to compare identities could either double-attach
            // or report TakenByAnother about yourself.
            var actor = Hauler();
            var load = Load();
            var zone = Placed(actor, load, 5, 5, 6, 5);

            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(actor, load, zone));
            Assert.AreEqual(DragVerdict.HandsFull, DragSystem.TryGrab(actor, load, zone));
            Assert.AreSame(load, DragSystem.GetDragged(actor));
            Assert.AreSame(actor, DragSystem.GetDragger(load));
        }

        // ════════════════════════════════════════════════════════
        // Letting go
        // ════════════════════════════════════════════════════════

        [Test]
        public void ReleasingClearsBothSides()
        {
            var actor = Hauler();
            var load = Load();
            var zone = Placed(actor, load, 5, 5, 6, 5);
            DragSystem.TryGrab(actor, load, zone);

            Assert.IsTrue(DragSystem.Release(actor));

            Assert.IsFalse(DragSystem.IsDragging(actor));
            Assert.IsFalse(DragSystem.IsBeingDragged(load));
            Assert.IsNull(actor.GetPart<DragPart>(), "the hauler's part is removed, not just nulled");
            Assert.IsNull(load.GetPart<DraggedPart>(), "the load's part is removed, not just nulled");
        }

        [Test]
        public void ReleasingDoesNotStealBackALoadSomeoneElseNowHolds()
        {
            // The defensive branch in Release, which nothing else reaches:
            // if a load's DraggedPart names a different hauler by the time
            // we let go, that link belongs to them. Clearing it would leave
            // the other hauler holding a rope tied to nothing.
            //
            // Reachable only through corrupted or externally-rewritten
            // state today, which is exactly why it is worth pinning: the
            // day D3 or D5 introduces a transfer path, this test says what
            // the contract was.
            var first = Hauler();
            var second = Hauler();
            second.ID = "hauler2";
            var load = Load();
            var zone = Placed(first, load, 5, 5, 6, 5);

            DragSystem.TryGrab(first, load, zone);
            load.GetPart<DraggedPart>().Dragger = second;   // simulate a hand-off

            Assert.IsTrue(DragSystem.Release(first), "the first hauler still lets go");
            Assert.IsFalse(DragSystem.IsDragging(first));
            Assert.AreSame(second, DragSystem.GetDragger(load),
                "the other hauler's claim survives");
        }

        [Test]
        public void ReleasingNothing_IsFalseNotACrash()
        {
            Assert.IsFalse(DragSystem.Release(Hauler()));
            Assert.IsFalse(DragSystem.Release(null));
        }

        [Test]
        public void AfterReleasing_YouCanGrabAgain()
        {
            // Counter-check on Release: proves it actually freed the load
            // rather than merely hiding it from IsDragging.
            var actor = Hauler();
            var load = Load();
            var zone = Placed(actor, load, 5, 5, 6, 5);

            DragSystem.TryGrab(actor, load, zone);
            DragSystem.Release(actor);
            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(actor, load, zone));
        }

        [Test]
        public void AReleasedLoad_CanBeTakenBySomeoneElse()
        {
            var first = Hauler();
            var second = Hauler();
            second.ID = "hauler2";
            var load = Load();
            var zone = Placed(first, load, 5, 5, 6, 5);
            zone.AddEntity(second, 7, 5);

            DragSystem.TryGrab(first, load, zone);
            DragSystem.Release(first);
            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(second, load, zone));
            Assert.AreSame(second, DragSystem.GetDragger(load));
        }

        // ════════════════════════════════════════════════════════
        // Degenerate inputs
        // ════════════════════════════════════════════════════════

        [Test]
        public void NullsAndUnplacedEntities_AreNamedRefusals()
        {
            var actor = Hauler();
            var load = Load();
            var zone = Placed(actor, load, 5, 5, 6, 5);

            Assert.AreEqual(DragVerdict.NoActor, DragSystem.TryGrab(null, load, zone));
            Assert.AreEqual(DragVerdict.NoTarget, DragSystem.TryGrab(actor, null, zone));
            Assert.AreEqual(DragVerdict.NotAdjacent, DragSystem.TryGrab(actor, load, null),
                "no zone means no way to be next to anything");

            // An entity that exists but was never placed in the zone.
            var ghost = Load(id: "ghost");
            Assert.AreEqual(DragVerdict.NotAdjacent, DragSystem.TryGrab(actor, ghost, zone));
        }

        [Test]
        public void QueriesOnNullsAreSafe()
        {
            Assert.IsNull(DragSystem.GetDragged(null));
            Assert.IsNull(DragSystem.GetDragger(null));
            Assert.IsFalse(DragSystem.IsDragging(null));
            Assert.IsFalse(DragSystem.IsBeingDragged(null));
        }

        // ════════════════════════════════════════════════════════
        // Observability — every gate emits (CLAUDE.md §Observability)
        // ════════════════════════════════════════════════════════

        [Test]
        public void ASuccessfulGrabEmitsGrabbed()
        {
            var actor = Hauler();
            var load = Load();
            var zone = Placed(actor, load, 5, 5, 6, 5);
            DragSystem.TryGrab(actor, load, zone);

            var records = Records("Grabbed");
            Assert.AreEqual(1, records.Count, "the success path is the proof the system worked");
            StringAssert.Contains("120", records[0].PayloadJson, "payload carries the weight");
        }

        [Test]
        public void EveryRefusalEmitsItsOwnReason()
        {
            // The point of a per-branch verdict enum: "why didn't it work?"
            // is answerable by query instead of by reading code.
            var actor = Hauler();
            var far = Load();
            var zone = Placed(actor, far, 5, 5, 9, 9);

            DragSystem.TryGrab(actor, far, zone);

            var refusals = Records("Refused");
            Assert.AreEqual(1, refusals.Count);
            StringAssert.Contains("NotAdjacent", refusals[0].PayloadJson,
                "the reason must name which gate fired");
        }

        [Test]
        public void ARefusalIsNotAlsoASuccess()
        {
            // Counter-check on the diag pair: a buggy emitter that fired
            // Grabbed unconditionally would pass the test above.
            var actor = Hauler();
            var far = Load();
            var zone = Placed(actor, far, 5, 5, 9, 9);

            DragSystem.TryGrab(actor, far, zone);

            Assert.AreEqual(0, Records("Grabbed").Count,
                "a refused grab must not report success");
        }

        [Test]
        public void ReleasingEmitsReleased()
        {
            var actor = Hauler();
            var load = Load();
            var zone = Placed(actor, load, 5, 5, 6, 5);
            DragSystem.TryGrab(actor, load, zone);
            Diag.ResetAll();

            DragSystem.Release(actor);

            Assert.AreEqual(1, Records("Released").Count);
        }

        [Test]
        public void ReleasingNothing_EmitsNothing()
        {
            // Counter-check: no phantom Released records from no-op calls,
            // or the trace stops meaning anything.
            DragSystem.Release(Hauler());
            Assert.AreEqual(0, Records("Released").Count);
        }

        // ════════════════════════════════════════════════════════
        // The verb in the world-action menu
        // ════════════════════════════════════════════════════════

        [Test]
        public void AHaulableThing_OffersHaul()
        {
            var actor = Hauler();
            var load = Load();
            var actions = WorldInteractionSystem.GatherActions(load, actor);
            CollectionAssert.Contains(CommandsOf(actions), "HaulObject");
        }

        [Test]
        public void ARockYouCouldPocket_DoesNotOfferHaul()
        {
            // Counter-check: the row is gated on the D1 verdict, so a
            // carryable pebble must not advertise hauling.
            var actor = Hauler();
            var pebble = new Entity { ID = "pebble", BlueprintName = "Rock" };
            pebble.AddPart(new PhysicsPart { Weight = 2, Takeable = true });

            var actions = WorldInteractionSystem.GatherActions(pebble, actor);
            CollectionAssert.DoesNotContain(CommandsOf(actions), "HaulObject");
        }

        [Test]
        public void WhileHauling_TheMenuOffersLetGoInstead()
        {
            var actor = Hauler();
            var load = Load();
            var zone = Placed(actor, load, 5, 5, 6, 5);
            DragSystem.TryGrab(actor, load, zone);

            var commands = CommandsOf(WorldInteractionSystem.GatherActions(load, actor));
            CollectionAssert.Contains(commands, "ReleaseHaul");
            CollectionAssert.DoesNotContain(commands, "HaulObject",
                "offering both at once would be a menu that lies about state");
        }

        /// <summary>All drag-category records of one kind, oldest first.</summary>
        private static System.Collections.Generic.IReadOnlyList<Diag.Entry> Records(string kind)
            => DiagQuery.Apply(new DiagQuery.Filter
            { Category = "drag", Kind = kind, Limit = 50 }).Records;

        private static System.Collections.Generic.List<string> CommandsOf(
            System.Collections.Generic.List<InventoryAction> actions)
        {
            var list = new System.Collections.Generic.List<string>();
            if (actions == null) return list;
            foreach (var a in actions) list.Add(a.Command);
            return list;
        }
    }
}
