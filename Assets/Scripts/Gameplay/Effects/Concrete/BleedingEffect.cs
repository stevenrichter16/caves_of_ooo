namespace CavesOfOoo.Core
{
    /// <summary>
    /// Bleeding: indefinite duration, save-based recovery.
    /// Deals damage each turn. Each EndTurn, rolls Toughness save vs SaveTarget.
    /// SaveTarget decreases by 1 each turn, making recovery progressively easier.
    /// Stacking upgrades damage/save of existing bleed.
    /// </summary>
    public class BleedingEffect : Effect
    {
        public override string DisplayName => "bleeding";

        public string DamageDice;
        public int SaveTarget;
        public System.Random Rng;

        public BleedingEffect(int saveTarget = 15, string damageDice = "1d2", System.Random rng = null)
        {
            Duration = DURATION_INDEFINITE;
            SaveTarget = saveTarget;
            DamageDice = damageDice;
            Rng = rng ?? new System.Random();
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is bleeding!");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " stops bleeding.");
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            int damage = DiceRoller.Roll(DamageDice, Rng);
            if (damage > 0)
            {
                Zone zone = context?.GetParameter<Zone>("Zone");
                CombatSystem.ApplyDamage(target, damage, null, zone);
                MessageLog.Add(target.GetDisplayName() + " takes " + damage + " bleed damage.");
            }
        }

        public override void OnTurnEnd(Entity target)
        {
            // Save-based recovery: roll 1d20 + ToughnessMod vs SaveTarget
            int toughMod = StatUtils.GetModifier(target, "Toughness");
            int roll = DiceRoller.Roll(20, Rng) + toughMod;
            if (roll >= SaveTarget)
            {
                Duration = 0; // will be cleaned up
                return;
            }

            // SaveTarget decreases each turn (gets easier to recover)
            if (SaveTarget > 1)
                SaveTarget--;
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is BleedingEffect bleed)
            {
                // Upgrade to the worse of the two
                if (bleed.SaveTarget > SaveTarget)
                    SaveTarget = bleed.SaveTarget;
                if (string.Compare(bleed.DamageDice, DamageDice, System.StringComparison.Ordinal) > 0)
                    DamageDice = bleed.DamageDice;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&r";
    }
}
