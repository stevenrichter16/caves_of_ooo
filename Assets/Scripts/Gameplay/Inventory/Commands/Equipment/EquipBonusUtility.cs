using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core.Inventory.Commands
{
    /// <summary>
    /// Applies / removes stat bonuses + speed penalty when an item is
    /// equipped or unequipped. Each individual stat-bonus change emits
    /// one diag record under <c>category="equipment"</c> so a future
    /// "why did my Strength jump 4 points?" debug starts with a query
    /// (<c>diag_query category=equipment kind=StatBonusApplied
    /// target=&lt;stat-name&gt;</c>) rather than a code-grep.
    /// </summary>
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

                    int bonusBefore = stat.Bonus;
                    if (apply)
                        stat.Bonus += amount;
                    else
                        stat.Bonus -= amount;

                    if (Diag.IsChannelEnabled("equipment"))
                    {
                        Diag.Record(
                            category: "equipment",
                            kind: apply ? "StatBonusApplied" : "StatBonusRemoved",
                            actor: actor,
                            target: item,
                            payload: new
                            {
                                statName,
                                delta = apply ? amount : -amount,
                                bonusBefore,
                                bonusAfter = stat.Bonus,
                                item = item?.GetDisplayName(),
                                itemBlueprint = item?.BlueprintName,
                            });
                    }
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

            int penaltyBefore = speed.Penalty;
            if (apply)
                speed.Penalty += armor.SpeedPenalty;
            else
                speed.Penalty -= armor.SpeedPenalty;

            if (Diag.IsChannelEnabled("equipment"))
            {
                Diag.Record(
                    category: "equipment",
                    kind: apply ? "SpeedPenaltyApplied" : "SpeedPenaltyRemoved",
                    actor: actor,
                    target: item,
                    payload: new
                    {
                        delta = apply ? armor.SpeedPenalty : -armor.SpeedPenalty,
                        penaltyBefore,
                        penaltyAfter = speed.Penalty,
                        item = item.GetDisplayName(),
                        itemBlueprint = item.BlueprintName,
                    });
            }
        }
    }
}
