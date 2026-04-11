namespace CavesOfOoo.Core
{
    /// <summary>
    /// Persistent warmth aura used by Hearthwarm.
    /// Lives on the caster and emits gentle radiant heat into a chosen cell
    /// at turn start for a short duration.
    /// </summary>
    public class HearthAuraEffect : Effect
    {
        public override string DisplayName => "hearthwarm";

        public int TargetX;
        public int TargetY;
        public float JoulesPerPulse = 80f;

        public HearthAuraEffect()
        {
            Duration = 3;
        }

        public HearthAuraEffect(int targetX, int targetY, int duration = 3, float joulesPerPulse = 80f)
        {
            TargetX = targetX;
            TargetY = targetY;
            JoulesPerPulse = joulesPerPulse;
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " kindles a gentle hearth aura.");
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            Zone zone = context?.GetParameter<Zone>("Zone");
            if (zone == null)
                return;

            Cell cell = zone.GetCell(TargetX, TargetY);
            if (cell == null)
                return;

            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                Entity entity = cell.Objects[i];
                if (!entity.HasPart<ThermalPart>())
                    continue;

                var heatEvent = GameEvent.New("ApplyHeat");
                heatEvent.SetParameter("Joules", (object)JoulesPerPulse);
                heatEvent.SetParameter("Radiant", (object)true);
                heatEvent.SetParameter("Source", (object)target);
                heatEvent.SetParameter("Zone", (object)zone);
                entity.FireEvent(heatEvent);
                heatEvent.Release();
            }
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is HearthAuraEffect aura)
            {
                TargetX = aura.TargetX;
                TargetY = aura.TargetY;
                JoulesPerPulse = aura.JoulesPerPulse;
                Duration = aura.Duration > Duration ? aura.Duration : Duration;
                return true;
            }

            return false;
        }

        public override string GetRenderColorOverride() => "&y";
    }
}
