namespace CavesOfOoo.Core
{
    /// <summary>
    /// Physical passive mutation: Regeneration.
    /// Heals Level HP per turn on EndTurn, capped at max HP.
    /// Also regrows severed limbs: every RegenInterval turns (20 / Level, min 5),
    /// one dismembered part is regenerated.
    /// </summary>
    public class RegenerationMutation : BaseMutation
    {
        public override string Name => "Regeneration";
        public override string MutationType => "Physical";
        public override string DisplayName => "Regeneration";

        private int _regenCounter;

        /// <summary>
        /// Turns between limb regeneration attempts. Scales with level.
        /// </summary>
        public int RegenInterval => System.Math.Max(5, 20 / System.Math.Max(1, Level));

        public override void Mutate(Entity entity, int level)
        {
            base.Mutate(entity, level);
            _regenCounter = 0;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "EndTurn")
            {
                RegenerateHP();
                RegenerateLimbs();
            }
            return true;
        }

        private void RegenerateHP()
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

        private void RegenerateLimbs()
        {
            if (ParentEntity == null) return;

            var body = ParentEntity.GetPart<Body>();
            if (body == null) return;
            if (!body.HasRegenerableLimbs()) return;

            _regenCounter++;
            if (_regenCounter >= RegenInterval)
            {
                _regenCounter = 0;
                body.RegenerateLimb();
                body.UpdateBodyParts();
            }
        }
    }
}
