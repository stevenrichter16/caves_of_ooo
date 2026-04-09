namespace CavesOfOoo.Core
{
    /// <summary>
    /// Smoldering: post-fire residual heat. Emits low heat to adjacent cells.
    /// Lasts 5 turns then fades.
    /// </summary>
    public class SmolderingEffect : Effect
    {
        public override string DisplayName => "smoldering";

        public SmolderingEffect(int duration = 5)
        {
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " smolders.");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " stops smoldering.");
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            // Emit low heat to adjacent cells
            Zone zone = context?.GetParameter<Zone>("Zone");
            if (zone != null)
                MaterialSimSystem.EmitHeatToAdjacent(target, zone, 10f);
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is SmolderingEffect smolder)
            {
                if (smolder.Duration > Duration)
                    Duration = smolder.Duration;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&W";
    }
}
