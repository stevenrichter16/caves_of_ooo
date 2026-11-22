namespace CavesOfOoo.Core
{
    /// <summary>
    /// Short-lived entities (steam clouds, ash drifts, scorch marks, temporary
    /// conjured puddles) carry a LifespanPart that counts down to zero each
    /// EndTurn and then removes the host entity from its zone. Reuses the
    /// MaterialSimSystem passive tick so reaction products don't need their own
    /// driver. Zero-or-negative TurnsRemaining at construction means "persist
    /// indefinitely" — use for fallbacks only.
    /// </summary>
    public class LifespanPart : Part
    {
        public override string Name => "Lifespan";

        /// <summary>Turns remaining before the host entity is removed from the zone.</summary>
        public int TurnsRemaining = 3;
        /// <summary>Optional shallow aftermath on natural expiry only; never inventory liquid.</summary>
        public string ExpiryCoating = "";
        public int ExpiryCoatingTurns;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "EndTurn")
                return HandleEndTurn(e);
            return true;
        }

        private bool HandleEndTurn(GameEvent e)
        {
            if (TurnsRemaining <= 0)
                return true;

            TurnsRemaining--;
            if (TurnsRemaining > 0)
                return true;

            if (ParentEntity == null)
                return true;

            var zone = e.GetParameter<Zone>("Zone");
            var cell = zone?.GetEntityCell(ParentEntity);
            bool removedLight = ParentEntity.GetPart<LightSourcePart>()?.Enabled == true;
            if (cell == null || !zone.RemoveEntity(ParentEntity))
                return true;

            if (!string.IsNullOrEmpty(ExpiryCoating) && ExpiryCoatingTurns > 0)
                ZoneTileStateSystem.WriteCoating(zone, cell.X, cell.Y, ExpiryCoating,
                    ExpiryCoatingTurns, ParentEntity, "natural-expiry");

            // Ordinary single-cell removal does not invalidate its tile. A light
            // also changes neighboring illumination: the cell-only render path
            // deliberately reuses its lightmap, so refresh that once on expiry.
            if (removedLight) ZoneRenderHooks.MarkFullDirty("LifespanExpiredLight");
            else ZoneRenderHooks.MarkCellDirty(cell, "LifespanExpired");

            return true;
        }
    }
}
