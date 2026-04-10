namespace CavesOfOoo.Core
{
    /// <summary>
    /// Steam: a thin vapor field that cools adjacent entities slightly and
    /// lightly wets them as it touches. Dissipates over time. Primarily used
    /// as a reaction product — spawned by water_plus_fire and the vapor crossing
    /// in ThermalPart.
    /// </summary>
    public class SteamEffect : Effect
    {
        public override string DisplayName => "steam";

        /// <summary>0..1. Density decays each turn until dissipated.</summary>
        public float Density;

        public SteamEffect(float density = 1.0f)
        {
            Density = density > 1.0f ? 1.0f : (density < 0f ? 0f : density);
            Duration = DURATION_INDEFINITE;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is wreathed in steam.");
        }

        public override void OnRemove(Entity target)
        {
            // Silent — vapor just fades.
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            Zone zone = context?.GetParameter<Zone>("Zone");
            if (zone == null || Density <= 0f)
                return;

            // Cool adjacent entities (negative radiant heat) and lightly wet them.
            MaterialSimSystem.EmitHeatToAdjacent(target, zone, -20f * Density);

            var sourceCell = zone.GetEntityCell(target);
            if (sourceCell == null)
                return;

            for (int dir = 0; dir < 8; dir++)
            {
                var cell = zone.GetCellInDirection(sourceCell.X, sourceCell.Y, dir);
                if (cell == null)
                    continue;

                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var neighbor = cell.Objects[i];
                    if (neighbor == target)
                        continue;
                    neighbor.ApplyEffect(new WetEffect(moisture: 0.1f * Density), target, zone);
                }
            }
        }

        public override void OnTurnEnd(Entity target)
        {
            Density = System.Math.Max(0f, Density - 0.1f);
            if (Density <= 0f)
                Duration = 0;
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is SteamEffect steam)
            {
                Density += steam.Density * 0.5f;
                if (Density > 1.0f)
                    Density = 1.0f;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&b";
    }
}
