using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D1 of Drag &amp; Haul (<c>Docs/DRAG-AND-HAUL.md</c> §7) — the
    /// arithmetic, and only the arithmetic.
    ///
    /// <para>Carrying is what fits in your arms; dragging is what doesn't.
    /// The outcomes are one ladder: take it, haul it, or fail to move it.
    /// These tests hold the ladder's rungs, the seam where the carry rung is
    /// answered by the <i>shipped</i> pickup gate rather than a private copy
    /// of it, and the two refusals that are not about weight at all — a
    /// living thing, and a thing that is part of the world.</para>
    ///
    /// <para>Reference points used throughout: the shipped lift requirement
    /// is <c>ceil(weight/2) + 1</c> (one-handed grip), so Strength 10 lifts up
    /// to weight 18; the drag cap is <c>Strength × 8</c>, so Strength 10 hauls
    /// up to 80.</para>
    /// </summary>
    public class DragRulesTests
    {
        private static Entity Actor(int strength)
        {
            var e = new Entity { ID = "actor", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Statistics["Strength"] = new Stat
            { Owner = e, Name = "Strength", BaseValue = strength, Min = 0, Max = 40 };
            return e;
        }

        /// <summary>An ordinary loose object: takeable, so it is cargo rather
        /// than scenery, and the weight decides the rest.</summary>
        private static Entity Thing(int weight)
        {
            var e = new Entity { ID = "thing", BlueprintName = "Barrel" };
            e.AddPart(new PhysicsPart { Weight = weight, Takeable = true });
            return e;
        }

        /// <summary>Authored furniture: a HandlingPart is the opt-in that says
        /// "this is meant to be hauled", the way the six shipped heavy
        /// blueprints do.</summary>
        private static Entity Furniture(int weight, int minLift = 0)
        {
            var e = new Entity { ID = "furniture", BlueprintName = "SmithAnvil" };
            e.AddPart(new PhysicsPart { Weight = weight, Takeable = false });
            e.AddPart(new HandlingPart
            { Weight = weight, MinLiftStrength = minLift, Carryable = false });
            return e;
        }

        // ════════════════════════════════════════════════════════
        // The ladder
        // ════════════════════════════════════════════════════════

        [Test]
        public void DragCapacity_IsStrengthTimesEight()
        {
            Assert.AreEqual(80, DragRules.MaxDragWeight(Actor(10)));
            Assert.AreEqual(128, DragRules.MaxDragWeight(Actor(16)));
        }

        [Test]
        public void LightThing_WantsTakeNotDrag()
        {
            // Not a refusal — a redirection. The UI should offer "take".
            Assert.AreEqual(DragVerdict.CarryInstead, DragRules.CanDrag(Actor(10), Thing(4)));
        }

        [Test]
        public void HeavyButHaulable_IsDraggable()
        {
            // Str 10 lifts 18, hauls 80. A 60-weight beam is exactly the case
            // the feature exists for.
            Assert.AreEqual(DragVerdict.Ok, DragRules.CanDrag(Actor(10), Thing(60)));
        }

        [Test]
        public void PastDragCapacity_DoesNotMove()
        {
            Assert.AreEqual(DragVerdict.TooHeavy, DragRules.CanDrag(Actor(10), Thing(81)));
        }

        [Test]
        public void TheBoundaries_AreInclusiveOnBothRungs()
        {
            var a = Actor(10);                        // lifts ≤18, hauls ≤80
            Assert.AreEqual(DragVerdict.CarryInstead, DragRules.CanDrag(a, Thing(18)));
            Assert.AreEqual(DragVerdict.Ok, DragRules.CanDrag(a, Thing(19)));
            Assert.AreEqual(DragVerdict.Ok, DragRules.CanDrag(a, Thing(80)));
            Assert.AreEqual(DragVerdict.TooHeavy, DragRules.CanDrag(a, Thing(81)));
        }

        [Test]
        public void StrengthChangesWhatYouCanHaul()
        {
            // The counter-check that makes the whole ladder meaningful: the
            // same object, two people, two answers.
            Assert.AreEqual(DragVerdict.TooHeavy, DragRules.CanDrag(Actor(6), Thing(60)));
            Assert.AreEqual(DragVerdict.Ok, DragRules.CanDrag(Actor(10), Thing(60)));
        }

        // ════════════════════════════════════════════════════════
        // The carry rung is the SHIPPED carry rung
        // ════════════════════════════════════════════════════════

        [Test]
        public void CarryInstead_TracksTheRealPickupGate_NotPackCapacity()
        {
            // The bug this feature shipped a fix for before it ever ran.
            //
            // Pack capacity at Strength 16 is 240 (Str × 15), so a weight-60
            // beam "fits". But PickupCommand gates each lift on
            // ceil(60/2)+1 = 31 > 16 and refuses it. If CanDrag answered the
            // carry question with pack capacity it would say CarryInstead —
            // and the beam would be reachable by no verb at all: too heavy to
            // pick up, and officially not a drag target either.
            var beam = Thing(60);
            var actor = Actor(16);

            Assert.IsFalse(HandlingService.CanLift(actor, beam, out _),
                "precondition: the shipped pickup path refuses this");
            Assert.Less(60, 16 * 15,
                "precondition: pack capacity would have said yes");
            Assert.AreEqual(DragVerdict.Ok, DragRules.CanDrag(actor, beam));
        }

        [Test]
        public void AnUnliftableThing_IsNotCarriedRegardlessOfHowLightItIs()
        {
            // Carryable=false is the blueprint saying "there is no picking
            // this up", and it holds even at a Strength that could trivially
            // manage the weight.
            var crate = Furniture(10);
            Assert.AreEqual(DragVerdict.Ok, DragRules.CanDrag(Actor(40), crate));
        }

        // ════════════════════════════════════════════════════════
        // Refusals that are not about weight
        // ════════════════════════════════════════════════════════

        [Test]
        public void LivingThings_AreNeverDragged()
        {
            var creature = Thing(60);
            creature.Tags["Creature"] = "";
            Assert.AreEqual(DragVerdict.Living, DragRules.CanDrag(Actor(40), creature));
        }

        [Test]
        public void ACorpse_IsNotAlive_AndMayBeHauled()
        {
            // Counter-check to the above: a body is a heavy object, not a
            // person. (Shipped CreatureCorpse weighs 10 and is simply
            // carryable; SaltCuredBody is the heavy one this rung is for.)
            var body = Thing(90);
            body.BlueprintName = "SaltCuredBody";
            Assert.AreEqual(DragVerdict.Ok, DragRules.CanDrag(Actor(16), body));
        }

        [Test]
        public void Scenery_DoesNotBudge()
        {
            // No handling metadata and not takeable — a wall, a standing
            // tree, grass. Weight would say yes; the world says no.
            var tree = new Entity { ID = "tree", BlueprintName = "Tree" };
            tree.AddPart(new PhysicsPart { Solid = true, Takeable = false });
            Assert.AreEqual(DragVerdict.Rooted, DragRules.CanDrag(Actor(40), tree));

            // Terrain glyphs have no weight at all and must not become
            // draggable by scoring zero on a weight check.
            var grass = new Entity { ID = "grass", BlueprintName = "Grass" };
            grass.AddPart(new PhysicsPart());
            Assert.AreEqual(DragVerdict.Rooted, DragRules.CanDrag(Actor(40), grass));
        }

        [Test]
        public void AHandlingPart_IsTheOptInThatMakesFurnitureHaulable()
        {
            // Counter-check to Scenery_DoesNotBudge, and the whole reason the
            // rooted rule is written the way it is: same solid, untakeable
            // shape — one authored for hauling, one not.
            var untagged = new Entity { ID = "a", BlueprintName = "Pillar" };
            untagged.AddPart(new PhysicsPart { Solid = true, Weight = 60, Takeable = false });
            Assert.AreEqual(DragVerdict.Rooted, DragRules.CanDrag(Actor(16), untagged));

            Assert.AreEqual(DragVerdict.Ok, DragRules.CanDrag(Actor(16), Furniture(60)));
        }

        [Test]
        public void MinLiftStrength_GatesIndependentlyOfWeight()
        {
            // An anvil is not hard because it is heavy; it is hard because
            // there is nowhere to hold it. Same weight, two actors — only the
            // blueprint's MinLiftStrength differs between the answers.
            //
            // The weight matters to the FIXTURE, not just the fiction: it has
            // to sit inside the drag cap for BOTH actors, or the stronger one
            // is refused for weight and the gate is never reached. At 120:
            // Str 16 hauls 128 ✓, Str 18 hauls 144 ✓.
            var anvil = Furniture(120, minLift: 18);
            Assert.AreEqual(DragVerdict.NotStrongEnough, DragRules.CanDrag(Actor(16), anvil));
            Assert.AreEqual(DragVerdict.Ok, DragRules.CanDrag(Actor(18), anvil));
        }

        [Test]
        public void WeightRefusalOutranksTheGripRefusal()
        {
            // A thing that is both too heavy AND too awkward reports the
            // weight, because that is the one the player can do something
            // about by levelling.
            var millstone = Furniture(150, minLift: 18);
            Assert.AreEqual(DragVerdict.TooHeavy, DragRules.CanDrag(Actor(10), millstone));
        }

        // ════════════════════════════════════════════════════════
        // Degenerate inputs
        // ════════════════════════════════════════════════════════

        [Test]
        public void NullsAreNamedRefusals_NotCrashes()
        {
            Assert.AreEqual(DragVerdict.NoActor, DragRules.CanDrag(null, Thing(100)));
            Assert.AreEqual(DragVerdict.NoTarget, DragRules.CanDrag(Actor(10), null));
            Assert.AreEqual(0, DragRules.MaxDragWeight(null));
            Assert.AreEqual(0, DragRules.WeightOf(null));
            Assert.IsFalse(DragRules.IsRooted(null));
        }

        [Test]
        public void NoStrengthStat_HaulsNothing()
        {
            var weakling = new Entity { ID = "ghost" };
            Assert.AreEqual(0, DragRules.MaxDragWeight(weakling));
            Assert.AreEqual(DragVerdict.TooHeavy, DragRules.CanDrag(weakling, Furniture(1)));
        }

        [Test]
        public void ZeroOrNegativeStrength_HaulsNothing()
        {
            Assert.AreEqual(0, DragRules.MaxDragWeight(Actor(0)));
            Assert.AreEqual(DragVerdict.TooHeavy, DragRules.CanDrag(Actor(0), Furniture(1)));
        }

        [Test]
        public void HandlingWeight_WinsOverPhysicsWeight()
        {
            // HandlingPart is the richer metadata; when a blueprint authored
            // one, it is the authority — and DragRules must agree with the
            // inventory about which number that is.
            var e = new Entity { ID = "mixed" };
            e.AddPart(new PhysicsPart { Weight = 5 });
            e.AddPart(new HandlingPart { Weight = 120 });
            Assert.AreEqual(120, DragRules.WeightOf(e));
            Assert.AreEqual(HandlingService.GetWeight(e), DragRules.WeightOf(e),
                "hauling and carrying must never disagree about how heavy a thing is");
        }

        [Test]
        public void CanDragNow_IsTrueOnlyForTheCleanYes()
        {
            Assert.IsTrue(DragRules.CanDragNow(Actor(10), Thing(60)));
            Assert.IsFalse(DragRules.CanDragNow(Actor(10), Thing(4)),
                "CarryInstead is not a yes");
            Assert.IsFalse(DragRules.CanDragNow(Actor(10), Thing(9999)));
        }
    }
}
