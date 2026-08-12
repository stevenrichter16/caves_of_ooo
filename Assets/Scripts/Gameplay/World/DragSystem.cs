using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Attached to the hauler while it is dragging something.</summary>
    ///
    /// <remarks>
    /// <para>The link is two-sided (<see cref="DraggedPart"/> is the other
    /// half) because the game asks the question from both ends: "what am I
    /// dragging?" when the hauler moves, and "is anyone holding this?" when
    /// something happens to the load. A one-sided link would turn half of
    /// those into a zone scan.</para>
    ///
    /// <para>Presence of the Part IS the state — there is no
    /// <c>IsDragging</c> flag to fall out of sync with it. Release removes
    /// both Parts rather than nulling their fields.</para>
    /// </remarks>
    public sealed class DragPart : Part
    {
        public override string Name => "Drag";

        /// <summary>The load. Serialized by ID via the save graph's entity
        /// reference support — never a raw object graph.</summary>
        public Entity Dragged;

        /// <summary>
        /// The follow rule (D3). When the hauler finishes a move, the load
        /// is pulled into the cell the hauler just left.
        ///
        /// <para>Hooked on <c>AfterMove</c> rather than <c>BeforeMove</c> so
        /// the hauler has actually arrived — the vacated cell is only empty
        /// once the move is real, and a vetoed move must not drag anything.
        /// <c>ForceMoveTo</c> fires the same event, so being shoved while
        /// hauling takes the load along too, which is the behaviour a
        /// knockback should have.</para>
        /// </summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "AfterMove") return true;
            if (Dragged == null || ParentEntity == null) return true;

            int oldX = e.GetIntParameter("OldX");
            int oldY = e.GetIntParameter("OldY");
            int newX = e.GetIntParameter("NewX");
            int newY = e.GetIntParameter("NewY");

            // First placement (old = -1) and a move to the cell you already
            // occupy both mean "no cell was vacated". Following either would
            // stack the load on top of the hauler.
            if (oldX < 0 || oldY < 0) return true;
            if (oldX == newX && oldY == newY) return true;

            // The zone comes from the cell the mover arrived in — the event
            // is the only thing here that knows which zone this happened in,
            // and an entity has no back-pointer to one.
            var zone = e.GetParameter<Cell>("Cell")?.ParentZone;
            DragSystem.FollowInto(ParentEntity, Dragged, zone, oldX, oldY);
            return true;
        }
    }

    /// <summary>Attached to the load while it is being dragged.</summary>
    public sealed class DraggedPart : Part
    {
        public override string Name => "Dragged";

        /// <summary>Who has hold of it.</summary>
        public Entity Dragger;
    }

    /// <summary>
    /// Taking hold of a thing, and letting go of it — D2 of
    /// <c>Docs/DRAG-AND-HAUL.md</c> §7.
    ///
    /// <para><see cref="DragRules"/> answers "could this ever happen?" from
    /// the two entities alone. This answers "can it happen <i>now</i>?",
    /// which additionally needs the world: where they are standing, and
    /// whether either of them is already busy.</para>
    ///
    /// <para><b>Atomic by construction.</b> Every gate runs before any Part
    /// is attached, so a refused grab leaves no trace on either entity. The
    /// alternative — attach, then validate, then unwind — is how you get a
    /// hauler linked to a millstone three rooms away after an edge case
    /// nobody tested.</para>
    /// </summary>
    public static class DragSystem
    {
        /// <summary>Reach, in cells. 1 = the 3×3 box around the hauler,
        /// matching <c>ForgePart.IsNearForge</c>: you can take hold of what
        /// is beside you, including diagonally.</summary>
        public const int GrabReach = 1;

        // ════════════════════════════════════════════════════════
        // Queries
        // ════════════════════════════════════════════════════════

        public static Entity GetDragged(Entity actor) => actor?.GetPart<DragPart>()?.Dragged;

        public static Entity GetDragger(Entity target) => target?.GetPart<DraggedPart>()?.Dragger;

        public static bool IsDragging(Entity actor) => GetDragged(actor) != null;

        public static bool IsBeingDragged(Entity target) => GetDragger(target) != null;

        // ════════════════════════════════════════════════════════
        // Taking hold
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Try to take hold of <paramref name="target"/>.
        /// Returns <see cref="DragVerdict.Ok"/> on success; any other value
        /// means nothing changed on either entity.
        /// </summary>
        public static DragVerdict TryGrab(Entity actor, Entity target, Zone zone)
        {
            DragVerdict verdict = Evaluate(actor, target, zone);

            if (verdict != DragVerdict.Ok)
            {
                Diag.Record(
                    category: "drag",
                    kind: "Refused",
                    actor: actor,
                    target: target,
                    payload: new
                    {
                        reason = verdict.ToString(),
                        blueprintName = target?.BlueprintName,
                        weight = DragRules.WeightOf(target),
                        dragCapacity = DragRules.MaxDragWeight(actor),
                    });
                return verdict;
            }

            var dragPart = new DragPart { Dragged = target };
            var draggedPart = new DraggedPart { Dragger = actor };
            actor.AddPart(dragPart);
            target.AddPart(draggedPart);

            Diag.Record(
                category: "drag",
                kind: "Grabbed",
                actor: actor,
                target: target,
                payload: new
                {
                    blueprintName = target.BlueprintName,
                    weight = DragRules.WeightOf(target),
                    dragCapacity = DragRules.MaxDragWeight(actor),
                });

            MessageLog.Add($"You take hold of {target.GetDisplayName()}.");
            return DragVerdict.Ok;
        }

        /// <summary>
        /// Every gate, in one place, with no side effects. Ordered so the
        /// most specific answer wins: what the thing IS (D1) before where it
        /// is, and where it is before who is busy — a creature across the
        /// room reports <see cref="DragVerdict.Living"/>, not
        /// <see cref="DragVerdict.NotAdjacent"/>, because walking closer
        /// will not help.
        /// </summary>
        private static DragVerdict Evaluate(Entity actor, Entity target, Zone zone)
        {
            DragVerdict basic = DragRules.CanDrag(actor, target);
            if (basic != DragVerdict.Ok) return basic;

            if (!WithinReach(actor, target, zone)) return DragVerdict.NotAdjacent;

            // Already hauling — including re-grabbing the same load, which is
            // still "your hands are full", not a fresh grab.
            if (IsDragging(actor)) return DragVerdict.HandsFull;

            Entity holder = GetDragger(target);
            if (holder != null && !ReferenceEquals(holder, actor)) return DragVerdict.TakenByAnother;

            return DragVerdict.Ok;
        }

        /// <summary>True when both are placed in <paramref name="zone"/> and
        /// sit within <see cref="GrabReach"/> cells of each other.</summary>
        private static bool WithinReach(Entity actor, Entity target, Zone zone)
        {
            if (zone == null) return false;

            (int ax, int ay) = zone.GetEntityPosition(actor);
            if (ax < 0 || ay < 0) return false;

            (int tx, int ty) = zone.GetEntityPosition(target);
            if (tx < 0 || ty < 0) return false;

            int dx = ax - tx; if (dx < 0) dx = -dx;
            int dy = ay - ty; if (dy < 0) dy = -dy;
            return dx <= GrabReach && dy <= GrabReach;
        }

        // ════════════════════════════════════════════════════════
        // Following (D3)
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Pull <paramref name="load"/> into the cell the hauler just left.
        /// If it cannot go there, the grip breaks.
        ///
        /// <para><b>Why the vacated cell.</b> It needs no pathfinding and it
        /// cannot pick an illegal destination: the hauler was standing in it
        /// one tick ago. It also gives the right feel for free — the load
        /// trails behind, and turning a corner swings it around rather than
        /// sliding it sideways through a wall.</para>
        ///
        /// <para><b>Why breaking is the failure mode.</b> The alternatives
        /// are worse: teleporting the load is a lie, stacking it on another
        /// entity corrupts the cell, and silently keeping the link lets a
        /// hauler walk off with a rope tied to something across the map.
        /// Dropping it is the one outcome the player can see and
        /// understand.</para>
        /// </summary>
        internal static void FollowInto(Entity hauler, Entity load, Zone zone, int x, int y)
        {
            if (zone == null) { Slip(hauler, load, "no zone"); return; }

            var destination = zone.GetCell(x, y);
            if (destination == null) { Slip(hauler, load, "off the map"); return; }

            // BlocksMovement, not IsSolid: the latter tests the Solid tag
            // alone, and the haulable furniture sets only PhysicsPart.Solid.
            // Asking the weaker question here would slide a millstone
            // straight through another millstone.
            if (destination.BlocksMovement(load)) { Slip(hauler, load, "blocked"); return; }

            if (!MovementSystem.ForceMoveTo(load, zone, x, y))
                Slip(hauler, load, "could not move");
        }

        /// <summary>Lose the grip, and say so. One record, one message.</summary>
        private static void Slip(Entity hauler, Entity load, string reason)
        {
            Diag.Record(
                category: "drag",
                kind: "Slipped",
                actor: hauler,
                target: load,
                payload: new
                {
                    reason,
                    blueprintName = load?.BlueprintName,
                });

            // Break the link WITHOUT the Released record and message — this
            // is not the player letting go, and conflating the two would
            // make "did they drop it or lose it?" unanswerable by query.
            var dragPart = hauler?.GetPart<DragPart>();
            if (dragPart != null) hauler.RemovePart(dragPart);
            var draggedPart = load?.GetPart<DraggedPart>();
            if (draggedPart != null && ReferenceEquals(draggedPart.Dragger, hauler))
                load.RemovePart(draggedPart);

            if (load != null)
                MessageLog.Add($"{load.GetDisplayName()} slips from your grip.");
        }

        // ════════════════════════════════════════════════════════
        // Letting go
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Let go of whatever <paramref name="actor"/> is hauling. Returns
        /// false when it was hauling nothing — a no-op that emits no record,
        /// so the trace keeps meaning something.
        /// </summary>
        public static bool Release(Entity actor)
        {
            var dragPart = actor?.GetPart<DragPart>();
            if (dragPart == null) return false;

            Entity load = dragPart.Dragged;
            actor.RemovePart(dragPart);

            // Clear the far side only if it still points back at us. A load
            // whose DraggedPart names someone else was re-taken somewhere
            // this code does not know about, and stealing it back here would
            // corrupt that link.
            var draggedPart = load?.GetPart<DraggedPart>();
            if (draggedPart != null && ReferenceEquals(draggedPart.Dragger, actor))
                load.RemovePart(draggedPart);

            Diag.Record(
                category: "drag",
                kind: "Released",
                actor: actor,
                target: load,
                payload: new { blueprintName = load?.BlueprintName });

            if (load != null)
                MessageLog.Add($"You let go of {load.GetDisplayName()}.");
            return true;
        }
    }
}
