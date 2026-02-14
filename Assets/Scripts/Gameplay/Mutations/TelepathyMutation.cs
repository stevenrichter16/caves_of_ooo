namespace CavesOfOoo.Core
{
    /// <summary>
    /// Mental passive mutation: Telepathy.
    /// Grants +Level/2 (min 1) to Ego stat as a bonus on Mutate.
    /// Mirrors Qud's Telepathy (simplified: passive stat bonus only, no conversation initiation).
    /// </summary>
    public class TelepathyMutation : BaseMutation
    {
        public override string Name => "Telepathy";
        public override string MutationType => "Mental";
        public override string DisplayName => "Telepathy";

        private int _appliedBonus;

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            ApplyBonus(entity);
        }

        public override void Unmutate(Entity entity)
        {
            RemoveBonus(entity);
            base.Unmutate(entity);
        }

        private void ApplyBonus(Entity entity)
        {
            int bonus = Level / 2;
            if (bonus < 1) bonus = 1;

            var ego = entity.GetStat("Ego");
            if (ego != null)
            {
                ego.Bonus += bonus;
                _appliedBonus = bonus;
            }
        }

        private void RemoveBonus(Entity entity)
        {
            if (_appliedBonus > 0)
            {
                var ego = entity.GetStat("Ego");
                if (ego != null)
                    ego.Bonus -= _appliedBonus;
                _appliedBonus = 0;
            }
        }
    }
}
