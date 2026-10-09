using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A visible single-use physical foot snare. Actual occupied body
    /// contacts trigger it, including forced landings and allied creatures.
    /// Holding prevents voluntary movement only; it grants no extra duration
    /// to an existing root and cannot distinguish the person who laid it.</summary>
    public sealed class CordSnarePart : Part
    {
        public override string Name => "CordSnare";
        public const int HoldTurns = 2;
        /// <summary>Saved spent-state also keeps an uncommitted placement inert.</summary>
        public bool Spent;

        /// <summary>True only for an intact authored trap currently on the ground.</summary>
        public static bool IsArmed(Entity owner)
        {
            var part = owner?.GetPart<CordSnarePart>(); var physics = owner?.GetPart<PhysicsPart>();
            return owner?.BlueprintName == CordSnareActions.Blueprint && part?.ParentEntity == owner && !part.Spent
                && physics?.ParentEntity == owner && !physics.Solid && !physics.Takeable
                && WorldResourceActions.Ground(owner, owner.SpatialZone);
        }
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "EntityEnteredCell" || ParentEntity?.GetPart<CordSnarePart>() != this || !IsArmed(ParentEntity)) return true;
            var actor = e.GetParameter<Entity>("Actor"); var cell = e.GetParameter<Cell>("Cell"); var zone = ParentEntity.SpatialZone;
            if (actor == null || actor == ParentEntity || !actor.HasTag("Creature") || CombatSystem.IsDeathHandled(actor)
                || actor.GetStat("Hitpoints") is Stat hp && hp.Value <= 0 || cell?.ParentZone != zone
                || !cell.Occupants.Contains(ParentEntity) || actor.SpatialZone != zone) return true;
            bool contact = false;
            foreach (var foot in zone.GetOccupiedCells(actor)) if (foot == cell) { contact = true; break; }
            if (!contact) return true;
            // Invalidate before effect dispatch: listeners may move the target,
            // replay the contact, or remove this trap while ApplyEffect runs.
            Spent = true; zone.RemoveEntity(ParentEntity);
            var existing = actor.GetEffect<RootedEffect>();
            bool held = existing != null && existing.Duration != 0;
            if (!held) held = actor.ApplyEffect(new RootedEffect(HoldTurns), ParentEntity, zone);
            Diag.Record("event", "CordSnareTriggered", ParentEntity, actor, new { x = cell.X, y = cell.Y, held, holdTurns = existing?.Duration ?? HoldTurns });
            MessageLog.Add(held ? actor.GetDisplayName() + " catches in the cord loop. The spent cord tears away."
                : actor.GetDisplayName() + " breaks through the cord loop without being held.");
            return true;
        }
    }
}
