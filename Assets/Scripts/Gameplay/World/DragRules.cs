namespace CavesOfOoo.Core
{
    /// <summary>Why a drag was allowed or refused. One reason per branch,
    /// so the message log and the diag record can say the same specific
    /// thing instead of a shrug.</summary>
    public enum DragVerdict
    {
        Ok = 0,
        NoActor,
        NoTarget,
        /// <summary>It is alive. You do not drag people.</summary>
        Living,
        /// <summary>Part of the scenery — a wall, a standing tree, grass.
        /// Not heavy: <i>attached</i>.</summary>
        Rooted,
        /// <summary>The shipped pickup path would accept this. Drag is the
        /// wrong verb, not a refusal.</summary>
        CarryInstead,
        /// <summary>Past what this Strength can haul at all.</summary>
        TooHeavy,
        /// <summary>Within the weight budget, but the blueprint asks for a
        /// Strength this actor does not have — an anvil is not hard because
        /// it is heavy, it is hard because there is nowhere to hold it.</summary>
        NotStrongEnough,

        // ── World-state refusals (D2) ────────────────────────────
        // CanDrag never returns these: they are properties of the world at
        // a moment rather than of the two entities, and DragSystem.TryGrab
        // is what checks them. They share this enum so the message log and
        // the diag payload have one vocabulary for "why not".

        /// <summary>Out of reach. Hauling starts from arm's length.</summary>
        NotAdjacent,
        /// <summary>This actor is already hauling something else.</summary>
        HandsFull,
        /// <summary>Someone else has hold of it.</summary>
        TakenByAnother,
    }

    /// <summary>
    /// Whether a thing can be dragged, and by whom.
    ///
    /// <para><b>Carrying is what fits in your arms; dragging is what
    /// doesn't.</b> The two questions share one Strength stat, so the
    /// outcomes form a single ladder rather than two unrelated systems:</para>
    ///
    /// <code>
    ///   HandlingService.CanLift says yes  → take it
    ///   weight ≤ Strength × 8             → drag it
    ///   heavier                           → it does not move
    /// </code>
    ///
    /// <para><b>The carry rung is asked, not re-derived.</b> This class
    /// calls <see cref="HandlingService.CanLift"/> — the same gate
    /// <c>PickupCommand</c> enforces — rather than reimplementing it. That
    /// matters more than it looks: the shipped lift requirement is
    /// <c>ceil(weight/2)+1</c>, which at Strength 16 caps a single lift near
    /// 30, while an actor's <i>pack capacity</i> is Strength × 15 = 240.
    /// Reading the pack number as the lift gate — the original draft of this
    /// file did exactly that — reports "just pick it up" for objects the
    /// pickup path then refuses, leaving them reachable by no verb at all.</para>
    ///
    /// <para><b>Pure by design.</b> No zone, no state, no side effects — just
    /// "could this happen?". The grab, the follow rule and the ground
    /// modifiers are later slices (<c>Docs/DRAG-AND-HAUL.md</c> §7); they all
    /// gate on this.</para>
    /// </summary>
    public static class DragRules
    {
        /// <summary>Weight haulable per point of Strength.</summary>
        ///
        /// <remarks>Roughly 4× the shipped single-lift ceiling of
        /// <c>2×(Strength−1)</c> across the whole stat range — a person hauls
        /// substantially more than they lift, and the ratio stays put whether
        /// you are Strength 8 or Strength 20 (80/18 ≈ 160/38).</remarks>
        public const int DRAG_PER_STRENGTH = 8;

        /// <summary>Max weight this actor can drag. 0 when it has no Strength
        /// stat — dragging is a deliberate physical act, so absence of
        /// strength means none of it, not the carry rule's "unlimited"
        /// sentinel.</summary>
        public static int MaxDragWeight(Entity actor)
        {
            var str = actor?.GetStat("Strength");
            if (str == null) return 0;
            return str.Value > 0 ? str.Value * DRAG_PER_STRENGTH : 0;
        }

        /// <summary>The weight the drag rules use. Delegates to
        /// <see cref="HandlingService.GetWeight"/> so hauling and carrying can
        /// never disagree about how heavy a thing is.</summary>
        public static int WeightOf(Entity target) => HandlingService.GetWeight(target);

        /// <summary>
        /// Scenery, not cargo. A blueprint that says nothing about handling
        /// and cannot be taken is part of the world: walls, standing trees,
        /// grass, ore veins, chests.
        ///
        /// <para>This partition is exact against shipped content — of 62
        /// blueprints marked <c>Solid</c>, the only ones carrying a
        /// <c>HandlingPart</c> are the six authored to be hauled. Adding a
        /// <c>HandlingPart</c> is therefore the gesture that makes a piece of
        /// furniture draggable, which is the right way round: haulable is
        /// opt-in, and forgetting to opt in fails safe.</para>
        /// </summary>
        public static bool IsRooted(Entity target)
        {
            if (target == null) return false;
            if (target.GetPart<HandlingPart>() != null) return false;
            var physics = target.GetPart<PhysicsPart>();
            return physics == null || !physics.Takeable;
        }

        /// <summary>
        /// Can <paramref name="actor"/> drag <paramref name="target"/>?
        ///
        /// <para>Adjacency, occupancy and whether the actor is already hauling
        /// something are NOT checked here — those are properties of the world
        /// at a moment, and this answers the question about the two entities
        /// alone.</para>
        /// </summary>
        public static DragVerdict CanDrag(Entity actor, Entity target)
        {
            if (actor == null) return DragVerdict.NoActor;
            if (target == null) return DragVerdict.NoTarget;

            // The brief's one hard line: non-living only.
            if (target.HasTag("Creature")) return DragVerdict.Living;

            if (IsRooted(target)) return DragVerdict.Rooted;

            // Ask the pickup path its own question rather than guessing at it.
            // Reported as its own outcome rather than a refusal so the UI can
            // offer "take" instead of saying no.
            if (HandlingService.CanLift(actor, target, out _)) return DragVerdict.CarryInstead;

            if (WeightOf(target) > MaxDragWeight(actor)) return DragVerdict.TooHeavy;

            var handling = target.GetPart<HandlingPart>();
            if (handling != null && handling.MinLiftStrength > 0)
            {
                var str = actor.GetStat("Strength");
                int have = str != null ? str.Value : 0;
                if (have < handling.MinLiftStrength) return DragVerdict.NotStrongEnough;
            }

            return DragVerdict.Ok;
        }

        /// <summary>True only for the clean yes.</summary>
        public static bool CanDragNow(Entity actor, Entity target)
            => CanDrag(actor, target) == DragVerdict.Ok;
    }
}
