using System;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>One abstract two-round hauling maneuver for the recovered field
    /// workshop. Input owns payment; this plan owns exact physical admission and
    /// coherent placement. It never simulates intermediate entry effects.</summary>
    internal sealed class RigRelocation
    {
        internal const string Refusal = "You cannot haul the rig across here. Line up on plain, clear ground.";
        internal const string Effort = "You brace the rig for crossing: two hauling rounds. Interrupted effort stays spent.";
        internal readonly Entity Actor, Rig;
        internal readonly Zone Source, Destination;
        readonly ZoneManager manager;
        readonly Cell sourceActor, sourceRig, receivingActor, receivingRig;
        readonly DragPart grip;
        readonly DraggedPart inverse;
        readonly Part[] rigParts;
        readonly string actorId, rigId, sourceId, destinationId;
        readonly bool repaired;
        readonly int dx, dy, appliedPenalty;
        bool committed;

        internal static bool IsAttempt(Entity actor) => FieldWorkRigRules.IsRig(DragSystem.GetDragged(actor));

        RigRelocation(Entity actor, Entity rig, Zone source, Zone destination, ZoneManager zones, int x, int y, int vx, int vy,
            string expectedSource, string expectedDestination)
        {
            Actor = actor; Rig = rig; Source = source; Destination = destination; manager = zones;
            sourceId = expectedSource; destinationId = expectedDestination;
            dx = vx; dy = vy; sourceActor = source.GetEntityCell(actor); sourceRig = source.GetEntityCell(rig);
            receivingRig = destination.GetCell(x, y); receivingActor = destination.GetCell(x + dx, y + dy);
            grip = actor.GetPart<DragPart>(); inverse = rig.GetPart<DraggedPart>();
            rigParts = rig.Parts.ToArray(); actorId = actor.ID; rigId = rig.ID;
            repaired = rig.GetPart<RepairablePart>()?.Repaired == true; appliedPenalty = grip?.AppliedPenalty ?? 0;
        }

        internal static RigRelocation Prepare(Entity actor, Zone source, TransitionDirection direction,
            int x, int y, ZoneManager zones)
        {
            var rig = DragSystem.GetDragged(actor);
            if (actor == null || source == null || zones == null || !FieldWorkRigRules.Payload(rig)
                || WorldMap.FromZoneID(source.ZoneID).z != 0) return null;
            int dx = direction == TransitionDirection.East ? 1 : direction == TransitionDirection.West ? -1 : 0;
            int dy = direction == TransitionDirection.South ? 1 : direction == TransitionDirection.North ? -1 : 0;
            if (Math.Abs(dx) + Math.Abs(dy) != 1 || !source.InBounds(x, y) || source.InBounds(x + dx, y + dy)) return null;
            var sourceId = source.ZoneID;
            var adjacent = WorldMap.GetAdjacentZoneID(sourceId, dx, dy);
            if (adjacent == null) return null;
            var destination = zones.GetZone(adjacent);
            if (destination == null || ReferenceEquals(source, destination)) return null;
            var at = ZoneTransitionSystem.GetArrivalPosition(direction, x, y);
            var plan = new RigRelocation(actor, rig, source, destination, zones, at.x, at.y, dx, dy, sourceId, adjacent);
            return plan.sourceActor?.X == x && plan.sourceActor?.Y == y && plan.Current() ? plan : null;
        }

        bool Current()
        {
            if (committed || Actor.ID != actorId || Rig.ID != rigId || Actor == Rig || actorId == rigId
                || Actor.HasPart<SpatialFootprintPart>() || Actor.HasPart<LiquidPoolPart>()
                || Actor.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(Actor)
                || Actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || Actor.SpatialZone != Source || Rig.SpatialZone != Source
                || sourceActor == null || sourceRig == null || receivingActor == null || receivingRig == null
                || Source.GetEntityCell(Actor) != sourceActor || Source.GetEntityCell(Rig) != sourceRig
                || sourceRig.X != sourceActor.X - dx || sourceRig.Y != sourceActor.Y - dy
                || Source.ZoneID != sourceId || Destination.ZoneID != destinationId
                || !manager.CachedZones.TryGetValue(sourceId, out var s) || s != Source
                || !manager.CachedZones.TryGetValue(destinationId, out var d) || d != Destination
                || !FieldWorkRigRules.Payload(Rig) || !Rig.Parts.SequenceEqual(rigParts)
                || Rig.GetPart<RepairablePart>().Repaired != repaired || DragRules.CanDrag(Actor, Rig) != DragVerdict.Ok
                || !OwnedOnce(Actor, grip) || !OwnedOnce(Rig, inverse) || grip.Dragged != Rig || inverse.Dragger != Actor
                || grip.LoadGripX != 0 || grip.LoadGripY != 0 || grip.HaulerGripX != 0 || grip.HaulerGripY != 0
                || grip.SpeedPenalty != DragSystem.PenaltyFor(Rig) || grip.AppliedPenalty != appliedPenalty)
                return false;
            return Plain(sourceActor, Actor) && Plain(sourceRig, Rig) && Plain(receivingActor) && Plain(receivingRig);
        }

        static bool OwnedOnce(Entity owner, Part part) => part != null && part.ParentEntity == owner
            && owner.Parts.Count(p => p != null && p.GetType() == part.GetType()) == 1
            && owner.Parts.Contains(part);

        static bool Plain(Cell cell, Entity movingOwner = null)
        {
            var zone = cell?.ParentZone;
            if (zone == null || zone.GetCell(cell.X, cell.Y) != cell || zone.TileState.Get(cell.X, cell.Y)?.IsEmpty == false) return false;
            Entity ground = null; int movingCount = 0;
            foreach (var occupant in cell.Occupants)
            {
                if (occupant == movingOwner && movingOwner != null) { movingCount++; continue; }
                if (ground != null || occupant == null) return false;
                ground = occupant;
            }
            if (movingCount != (movingOwner == null ? 0 : 1) || ground == null
                || ground.SpatialZone != zone || zone.GetEntityCell(ground) != cell || !cell.Objects.Contains(ground)
                || !ground.HasTag("Terrain") || ground.HasTag("ExcludeZoneArrival") || ground.HasTag("Solid")
                || (ground.BlueprintName != "Grass" && ground.BlueprintName != "Floor" && ground.BlueprintName != "StoneFloor")
                || ground.Parts.Count != 3) return false;
            var physics = ground.GetPart<PhysicsPart>();
            return OwnedOnce(ground, physics) && OwnedOnce(ground, ground.GetPart<RenderPart>())
                && OwnedOnce(ground, ground.GetPart<ExaminablePart>()) && !physics.Solid && !physics.Takeable
                && physics.InInventory == null && physics.Equipped == null;
        }

        /// <summary>Permission hooks may mutate the world. Every hook is followed
        /// by the same physical validation; Rooted is a movement veto, not an action veto.</summary>
        internal bool CheckPermission()
        {
            if (!Current()) return false;
            var e = GameEvent.New("BeforeMove");
            e.SetParameter("Actor", (object)Actor); e.SetParameter("SourceCell", (object)sourceActor);
            e.SetParameter("TargetCell", (object)receivingActor); e.SetParameter("DX", dx); e.SetParameter("DY", dy);
            bool allowed;
            try { allowed = Actor.FireEvent(e); } finally { e.Release(); }
            return allowed && Current();
        }

        internal ZoneTransitionResult Commit(Func<bool> contextCurrent = null)
        {
            if (!CheckPermission() || (contextCurrent != null && !contextCurrent())) return Failed();
            // No external callback occurs between this last check and coherent publication.
            committed = true;
            if (!Source.TryTransferHaulingPairTo(Actor, Rig, Destination, receivingActor, receivingRig)) return Failed();
            // A committed crossing makes the rig the player's relocated machine: the marker keeps its zone from being regenerated out from under it and lists it with the
            // things the player keeps standing (Docs/MACHINE-INSTALLATIONS.md, spec S19). Stamped once; an authored or earlier marker is left alone.
            if (Rig.GetPart<InstallationMarkerPart>() == null) Rig.AddPart(new InstallationMarkerPart { InstallationId = "relocated:" + Rig.ID, Origin = "Relocated" });
            Noise.Mark(Actor);
            var partyArrival = CurrentActorCell();
            if (partyArrival?.ParentZone == Destination && Actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(Actor))
            {
                try { ZoneTransitionSystem.TransitPartyMembers(Actor, Source, Destination, partyArrival.X, partyArrival.Y); }
                catch (Exception ex) { UnityEngine.Debug.LogError($"[Rig] Companion observer failed after committed placement: {ex}"); }
            }
            // Observers may move/remove the actor or release the rig. Publish
            // that real outcome, never restore the original arrival over it.
            var actual = CurrentActorCell(); var actualZone = actual?.ParentZone;
            if (Actor.GetPart<DragPart>() == grip && (actualZone == null || Rig.SpatialZone != actualZone
                || Actor.GetStatValue("Hitpoints") <= 0 || SpatialQuery.Distance(actualZone, Actor, Rig) > DragSystem.GrabReach))
                DragSystem.Release(Actor);
            return new ZoneTransitionResult { Success = true, NewZone = actualZone,
                NewPlayerX = actualZone == null ? -1 : actual.X, NewPlayerY = actualZone == null ? -1 : actual.Y };
        }

        Cell CurrentActorCell()
        {
            var zone = Actor.SpatialZone; var cell = zone?.GetEntityCell(Actor);
            return cell != null && cell.ParentZone == zone && zone.GetCell(cell.X,cell.Y) == cell && cell.Objects.Contains(Actor)
                ? cell : null;
        }

        internal static ZoneTransitionResult Failed() => new ZoneTransitionResult { ErrorReason = Refusal };
    }
}
