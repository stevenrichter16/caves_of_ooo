using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// M3.2 — AI behavior part that makes the wearer actively seek out
    /// items with a configurable tag and pick them up. A magpie scans for
    /// "Shiny" items; a mimic-vault might scan for "Treasure"; a compost
    /// worm might scan for "Organic". Pushes <see cref="GoFetchGoal"/>
    /// on a bored tick when a matching item is in the zone.
    ///
    /// Wires Phase 6's GoFetchGoal-with-returnHome-true path.
    ///
    /// Mirrors <see cref="AIWellVisitorPart"/> / <see cref="AIPetterPart"/>
    /// shape: AIBoredEvent trigger, probability gate via
    /// <see cref="BrainPart.Rng"/>, idempotency via
    /// <see cref="BrainPart.HasGoal(string)"/>, event consumed on successful
    /// push so sibling behavior parts don't also fire this turn.
    ///
    /// Scan cost: O(zone entities) per bored tick when the chance gate
    /// passes. For a flock-scale scenario (many Magpies), consider a
    /// spatial index — out of scope for M3.2 per the verification sweep.
    ///
    /// Blueprint attachment:
    ///   { "Name": "AIHoarder", "Params": [
    ///       { "Key": "TargetTag", "Value": "Shiny" },
    ///       { "Key": "Chance", "Value": "15" }
    ///   ]}
    /// </summary>
    public class AIHoarderPart : AIBehaviorPart
    {
        public override string Name => "AIHoarder";

        /// <summary>Tag name on the target item (e.g. "Shiny", "Treasure").</summary>
        public string TargetTag = "Shiny";

        /// <summary>Percent chance per bored tick to scan + push GoFetchGoal (0-100).</summary>
        public int Chance = 15;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == AIBoredEvent.ID)
            {
                bool result = HandleBored();
                if (!result) e.Handled = true;
                return result;
            }
            return true;
        }

        private bool HandleBored()
        {
            var brain = ParentEntity.GetPart<BrainPart>();
            if (brain?.Rng == null || brain.CurrentZone == null)
                return true;

            // Idempotency — don't stack a second GoFetchGoal if one is
            // already on the stack from this or any other source.
            if (brain.HasGoal("GoFetchGoal"))
                return true;

            // Probability gate.
            if (brain.Rng.Next(100) >= Chance)
                return true;

            // Need a place to fetch TO. Without a StartingCell, ReturnHome
            // would silently no-op. For a Hoarder specifically, the whole
            // point is "bring it home" — if there's no home, skip the scan.
            if (!brain.HasStartingCell)
                return true;

            // Optional: need an InventoryPart to hold the picked-up item.
            // GoFetchGoal fails its DoPickup phase silently on no-inventory,
            // so let's not bother scanning if we can't stow the loot.
            if (ParentEntity.GetPart<InventoryPart>() == null)
                return true;

            Entity target = FindNearestTaggedItem(brain.CurrentZone);
            if (target == null)
                return true;

            brain.PushGoal(new GoFetchGoal(target, returnHome: true));
            return false; // consumed
        }

        /// <summary>
        /// Scans the zone for the nearest entity carrying <see cref="TargetTag"/>
        /// that is also takeable (has <see cref="PhysicsPart.Takeable"/>).
        /// Returns null if none found.
        ///
        /// Uses <c>GetReadOnlyEntities</c> here (not the allocating
        /// <c>GetAllEntities</c>) because this loop does ZERO zone
        /// mutations — only reads tag presence, Parts, and cell positions.
        /// (Methodology Template §7.2 snapshot-discipline note.)
        /// </summary>
        private Entity FindNearestTaggedItem(Zone zone)
        {
            var myCell = zone.GetEntityCell(ParentEntity);
            if (myCell == null) return null;

            Entity nearest = null;
            int nearestDist = int.MaxValue;

            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity == ParentEntity) continue;
                if (!entity.HasTag(TargetTag)) continue;

                // Must be takeable — otherwise GoFetchGoal will walk up
                // to it and fail at the Pickup phase.
                var physics = entity.GetPart<PhysicsPart>();
                if (physics == null || !physics.Takeable) continue;

                var itemCell = zone.GetEntityCell(entity);
                if (itemCell == null) continue;
                // Skip items already carried (ItemCell still on ground only
                // if Zone tracks it; PhysicsPart.InInventory != null means
                // someone else already has it).
                if (physics.InInventory != null) continue;

                int dist = AIHelpers.ChebyshevDistance(myCell.X, myCell.Y, itemCell.X, itemCell.Y);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = entity;
                }
            }

            return nearest;
        }
    }
}
