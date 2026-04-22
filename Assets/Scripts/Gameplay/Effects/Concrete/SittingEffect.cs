namespace CavesOfOoo.Core
{
    /// <summary>
    /// Activity effect: the entity is sitting in a chair or resting in a bed.
    /// Duration is indefinite — removed by AI when hostile detected or NPC decides to stand.
    /// Mirrors Qud's Sitting effect.
    /// </summary>
    public class SittingEffect : Effect
    {
        public override string DisplayName => "sitting";

        /// <summary>The furniture entity being used (chair/bed).</summary>
        public Entity Furniture;

        public SittingEffect(Entity furniture = null)
        {
            Duration = DURATION_INDEFINITE;
            Furniture = furniture;
        }

        public override int GetEffectType() => TYPE_ACTIVITY | TYPE_VOLUNTARY;

        public override void OnRemove(Entity target)
        {
            // Mark furniture as unoccupied
            if (Furniture != null)
            {
                var chair = Furniture.GetPart<ChairPart>();
                if (chair != null) chair.Occupied = false;

                var bed = Furniture.GetPart<BedPart>();
                if (bed != null) bed.Occupied = false;
            }
        }

        public override void OnTurnEnd(Entity target)
        {
            // Do NOT decrement Duration — indefinite until removed by AI
        }
    }
}
