namespace CavesOfOoo.Core
{
    /// <summary>
    /// Physical passive mutation: Regeneration.
    /// Heals Level HP per turn on EndTurn, capped at max HP.
    /// Mirrors Qud's Regeneration (simplified: flat heal per turn).
    /// </summary>
    public class RegenerationMutation : BaseMutation
    {
        public override string Name => "Regeneration";
        public override string MutationType => "Physical";
        public override string DisplayName => "Regeneration";

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "EndTurn")
            {
                Regenerate();
            }
            return true;
        }

        private void Regenerate()
        {
            if (ParentEntity == null) return;

            var hpStat = ParentEntity.GetStat("Hitpoints");
            if (hpStat == null) return;

            // Only heal if below max
            if (hpStat.BaseValue >= hpStat.Max) return;

            int healAmount = Level;
            hpStat.BaseValue += healAmount;

            // Clamp to max
            if (hpStat.BaseValue > hpStat.Max)
                hpStat.BaseValue = hpStat.Max;
        }
    }
}
