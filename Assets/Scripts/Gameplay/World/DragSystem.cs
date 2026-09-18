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

        /// <summary>The Speed penalty this load imposes, computed once at
        /// grab time.</summary>
        public int SpeedPenalty;

        /// <summary>How much of <see cref="SpeedPenalty"/> was actually
        /// applied after clamping. Stored rather than recomputed so that
        /// lifting it can never leave a residue — the bug where a character
        /// keeps a fraction of a debuff forever.</summary>
        public int AppliedPenalty;

        // Grip points are body offsets, not anchors. Primitive fields preserve
        // the hand/load contact through save/load; old single-cell saves use zero.
        public int LoadGripX, LoadGripY, HaulerGripX, HaulerGripY;

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
            if (e.ID == "Died")
            {
                // A corpse must not keep a millstone reserved forever.
                DragSystem.Release(ParentEntity);
                return true;
            }

            if (e.ID != "AfterMove") return true;
            var actor = ParentEntity;
            if (actor == null) return true;
            // Cleanup can append a replacement DragPart during dispatch. That
            // new grip must wait for a new move, while nested real movement
            // carries its own event and can still follow normally.
            if (ReferenceEquals(e.GetParameter<Entity>("DragFollowActor"), actor)) return true;
            e.SetParameter("DragFollowActor", (object)actor);
            var zone = e.GetParameter<Cell>("Cell")?.ParentZone;
            DragSystem.ValidateLink(actor, zone);
            // Validation can remove this Part. A callback may also establish
            // a different grip; this event must not borrow that replacement.
            if (!ReferenceEquals(actor.GetPart<DragPart>(), this) || Dragged == null) return true;

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
            DragSystem.FollowInto(actor, Dragged, zone, oldX, oldY);
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

            var dragPart = new DragPart { Dragged = target, SpeedPenalty = PenaltyFor(target) };
            var actorAnchor=zone.GetEntityCell(actor);
            var loadAnchor=zone.GetEntityCell(target);
            var loadContact=SpatialQuery.ClosestCell(zone,target,actorAnchor.X,actorAnchor.Y);
            var handContact=SpatialQuery.ClosestCell(zone,actor,loadContact.X,loadContact.Y);
            dragPart.LoadGripX=loadContact.X-loadAnchor.X;
            dragPart.LoadGripY=loadContact.Y-loadAnchor.Y;
            dragPart.HaulerGripX=handContact.X-actorAnchor.X;
            dragPart.HaulerGripY=handContact.Y-actorAnchor.Y;
            var draggedPart = new DraggedPart { Dragger = actor };
            actor.AddPart(dragPart);
            target.AddPart(draggedPart);
            dragPart.AppliedPenalty = ApplySpeedPenalty(actor, dragPart.SpeedPenalty);

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
            return SpatialQuery.Distance(zone,actor,target) <= GrabReach;
        }

        // ════════════════════════════════════════════════════════
        // What it costs to haul (D4)
        // ════════════════════════════════════════════════════════

        /// <summary>Speed lost per 10 units of load.</summary>
        public const int PenaltyPerTenWeight = 4;

        /// <summary>Speed a hauler always keeps. A load must never pin the
        /// player in place — that is unrecoverable without the menu.</summary>
        public const int MinimumHaulingSpeed = 20;

        /// <summary>
        /// The Speed penalty a load imposes.
        ///
        /// <para><b>A Speed penalty, not an action cost.</b> There is no
        /// per-action cost in <c>TurnManager</c> to multiply — every action
        /// deducts the same fixed <c>ActionThreshold</c>. Carried weight
        /// already says "this is slowing me down" through
        /// <c>speed.Penalty</c> (<c>InventoryPart.RefreshHandlingCarryPenalty</c>),
        /// so hauling pulls the same lever rather than inventing a parallel
        /// one.</para>
        /// </summary>
        public static int PenaltyFor(Entity load)
        {
            int weight = DragRules.WeightOf(load);
            if (weight <= 0) return 0;
            return weight * PenaltyPerTenWeight / 10;
        }

        /// <summary>Apply (positive) or lift (negative) a speed penalty,
        /// clamped so hauling never reduces Speed below
        /// <see cref="MinimumHaulingSpeed"/>.</summary>
        private static int ApplySpeedPenalty(Entity actor, int delta)
        {
            var speed = actor?.GetStat("Speed");
            if (speed == null || delta <= 0) return 0;

            int headroom = speed.Value - MinimumHaulingSpeed;
            if (headroom <= 0) return 0;
            if (delta > headroom) delta = headroom;

            speed.Penalty += delta;
            return delta;
        }

        /// <summary>Remove whatever penalty this DragPart actually applied.
        /// Reads the stored amount rather than recomputing it, so a load
        /// whose weight changed mid-haul cannot leave a residue.</summary>
        private static void LiftSpeedPenalty(Entity actor, DragPart part)
        {
            var speed = actor?.GetStat("Speed");
            if (speed == null || part == null || part.AppliedPenalty == 0) return;
            speed.Penalty -= part.AppliedPenalty;
            part.AppliedPenalty = 0;
        }

        // ════════════════════════════════════════════════════════
        // Keeping the link honest (D5)
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// Drop the load if the link no longer makes sense: either end gone
        /// from <paramref name="zone"/>, or the load re-taken by someone
        /// else. Cheap enough to call on turn boundaries and after anything
        /// that removes entities.
        ///
        /// <para>Without this, a hauler who dies, changes zone, or whose
        /// load burns up keeps a reference to it forever — and the load
        /// stays reserved so nobody else can ever take it.</para>
        /// </summary>
        public static void ValidateLink(Entity actor, Zone zone)
        {
            var dragPart = actor?.GetPart<DragPart>();
            if (dragPart == null) return;

            Entity load = dragPart.Dragged;
            bool broken =
                load == null
                || zone == null
                || zone.GetEntityPosition(actor).x < 0
                || zone.GetEntityPosition(load).x < 0
                || !ReferenceEquals(GetDragger(load), actor)
                || IsGone(actor) || IsGone(load);

            if (broken) Slip(actor, load, "link no longer valid");
        }

        // Structural HP can be zero when BeforeDestroy vetoes destruction.
        // Gone marks committed prop destruction; combat uses base HP for death.
        private static bool IsGone(Entity entity)
            => entity?.GetPart<DestructiblePart>()?.Gone == true
                || (entity != null && entity.HasTag("Creature")
                    && entity.GetStat("Hitpoints") != null
                    && entity.GetStat("Hitpoints").BaseValue <= 0);

        /// <summary>Repair both sides of an entity's saved hauling links in
        /// its rebuilt zone, or null when it is no longer placed.</summary>
        public static void ValidateEntity(Entity entity, Zone zone)
        {
            if (entity == null) return;
            var inverse = entity.GetPart<DraggedPart>();
            ValidateLink(entity, zone);
            if (inverse == null || !ReferenceEquals(entity.GetPart<DraggedPart>(), inverse)) return;
            var actor = inverse.Dragger;
            // Never clear a different valid load merely because this stale
            // inverse happens to name its hauler.
            if (ReferenceEquals(GetDragged(actor), entity)) ValidateLink(actor, zone);
            if (ReferenceEquals(entity.GetPart<DraggedPart>(), inverse)
                && !ReferenceEquals(GetDragged(actor), entity)) entity.RemovePart(inverse);
        }

        /// <summary>Detach both captured sides after successful zone removal.
        /// Repeated or failed removals do not call this method.</summary>
        public static void DetachRemovedEntity(Entity entity)
        {
            if (entity == null) return;
            var inverse = entity.GetPart<DraggedPart>();
            Release(entity);
            if (inverse == null || !ReferenceEquals(entity.GetPart<DraggedPart>(), inverse)) return;
            var actor = inverse.Dragger;
            if (ReferenceEquals(GetDragged(actor), entity)) Release(actor);
            if (ReferenceEquals(entity.GetPart<DraggedPart>(), inverse)) entity.RemovePart(inverse);
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
            var grip = hauler?.GetPart<DragPart>();
            if (grip == null || !ReferenceEquals(grip.Dragged, load)) return;
            ValidateLink(hauler, zone);
            if (!ReferenceEquals(hauler.GetPart<DragPart>(), grip)
                || !ReferenceEquals(grip.Dragged, load)) return;

            x += grip.HaulerGripX - grip.LoadGripX;
            y += grip.HaulerGripY - grip.LoadGripY;
            var destination = zone.GetCell(x, y);
            if (destination == null) { Slip(hauler, load, "off the map"); return; }
            if(load.HasPart<SpatialFootprintPart>() && !zone.CanPlaceFootprint(load,x,y))
            {Slip(hauler,load,"body blocked");return;}

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
            if (dragPart != null)
            {
                LiftSpeedPenalty(hauler, dragPart);
                hauler.RemovePart(dragPart);
            }
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
            LiftSpeedPenalty(actor, dragPart);
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
