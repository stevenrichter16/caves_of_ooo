using CavesOfOoo.Skills;

namespace CavesOfOoo.Core.Inventory.Commands
{
    internal static class EquipBonusUtility
    {
        public static void ApplyEquipBonuses(Entity actor, EquippablePart equippable, bool apply,
            string restoredBonuses = null, string restoredSkillBonuses = null)
        {
            if (actor == null || equippable == null)
                return;

            var item = equippable.ParentEntity;

            var oldSkills = SkillModifiers.Parse(equippable.AppliedEquipSkillBonuses);
            var nextSkills = apply ? SkillModifiers.Parse(restoredSkillBonuses ?? equippable.EquipSkillBonuses) : null;
            var appliedSkills = SkillModifiers.Replace(actor, oldSkills, nextSkills);
            try
            {
                if (apply)
                {
                    var applied = StatModifiers.Replace(actor, StatModifiers.Parse(equippable.AppliedEquipBonuses),
                        StatModifiers.Parse(restoredBonuses ?? equippable.EquipBonuses), createMissingElementalResistances: true);
                    equippable.AppliedEquipBonuses = StatModifiers.Format(applied);
                }
                else
                {
                    // Legacy stat gear predates receipts; skill bonuses never did.
                    StatModifiers.Remove(actor, StatModifiers.Parse(equippable.AppliedEquipBonuses ?? equippable.EquipBonuses));
                    equippable.AppliedEquipBonuses = "";
                }
                equippable.AppliedEquipSkillBonuses = SkillModifiers.Format(appliedSkills);
            }
            catch
            {
                SkillModifiers.Replace(actor, appliedSkills, oldSkills);
                throw;
            }

            if (item == null)
                return;

            var armor = item.GetPart<ArmorPart>();
            if (armor == null || armor.SpeedPenalty == 0)
                return;

            var speed = actor.GetStat("Speed");
            if (speed == null)
                return;

            if (apply)
                speed.Penalty += armor.SpeedPenalty;
            else
                speed.Penalty -= armor.SpeedPenalty;
        }
    }
}
