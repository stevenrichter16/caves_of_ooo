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
