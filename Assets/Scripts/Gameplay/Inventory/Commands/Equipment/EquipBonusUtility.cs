namespace CavesOfOoo.Core.Inventory.Commands
{
    internal static class EquipBonusUtility
    {
        public static void ApplyEquipBonuses(Entity actor, EquippablePart equippable, bool apply)
        {
            if (actor == null || equippable == null)
                return;

            var item = equippable.ParentEntity;

            if (!string.IsNullOrEmpty(equippable.EquipBonuses))
            {
                string[] pairs = equippable.EquipBonuses.Split(',');
                for (int i = 0; i < pairs.Length; i++)
                {
                    string trimmed = pairs[i].Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    int colon = trimmed.IndexOf(':');
                    if (colon < 0)
                        continue;

                    string statName = trimmed.Substring(0, colon);
                    if (!int.TryParse(trimmed.Substring(colon + 1), out int amount))
                        continue;

                    var stat = actor.GetStat(statName);
                    if (stat == null)
                        continue;

                    if (apply)
                        stat.Bonus += amount;
                    else
                        stat.Bonus -= amount;
                }
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
