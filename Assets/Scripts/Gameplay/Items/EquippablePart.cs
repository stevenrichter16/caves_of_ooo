namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marks an item as equippable and defines which body part slot(s) it uses.
    /// Mirrors Qud's equipment slot system: items declare which body part types
    /// they can equip to via UsesSlots (comma-separated body part type names).
    ///
    /// When an entity has a Body part, equipment routes through body part nodes.
    /// When no Body part exists, falls back to legacy string-keyed slot system.
    /// </summary>
    public class EquippablePart : Part
    {
        public override string Name => "Equippable";

        /// <summary>
        /// Primary slot type this item equips to (e.g. "Hand", "Body", "Head", "Feet").
        /// Used for both legacy string-slot and body-part-aware equip.
        /// </summary>
        public string Slot = "Hand";

        /// <summary>
        /// Comma-separated list of body part types this item occupies.
        /// Mirrors Qud's Physics.UsesSlots.
        ///
        /// Examples:
        /// - "Hand" = one-handed weapon/shield (occupies 1 Hand)
        /// - "Hand,Hand" = two-handed weapon (occupies 2 Hands)
        /// - "Body" = body armor
        /// - "Head" = helmet
        /// - "Back" = cloak
        /// - "Feet" = boots
        /// - "Floating Nearby" = floating item
        ///
        /// If null or empty, falls back to Slot field for a single slot.
        /// </summary>
        public string UsesSlots;

        /// <summary>
        /// Stat bonuses applied when equipped, as comma-separated "StatName:Amount" pairs.
        /// Example: "Strength:2,Agility:-1" means +2 Str, -1 Agi when equipped.
        /// Parsed at runtime by InventorySystem.
        /// </summary>
        public string EquipBonuses = "";

        /// <summary>
        /// Exact signed bonuses currently contributed by this equipped item. Saved
        /// alongside the wearer's already-modified stats; never reapplied on load.
        /// Null identifies legacy equipment without a receipt, while an empty string
        /// means no recorded contribution. Fresh stack clones clear this field.
        /// </summary>
        public string AppliedEquipBonuses;

        /// <summary>Positive temporary ranks, as "SkillClass:Amount" pairs.</summary>
        public string EquipSkillBonuses = "";

        /// <summary>Exact saved temporary skill contribution; not replayed on load.</summary>
        public string AppliedEquipSkillBonuses = "";

        public override void FinalizeLoad(SaveReader reader)
        {
            // The first two prototype trinkets shipped with attribute tradeoffs.
            // Upgrade just those old instances after all ownership/skill caches load.
            // The saved receipt removes their exact old contribution, even if the
            // definition or actor's learned/base values changed since equipping.
            string replacement = ParentEntity?.BlueprintName == "QuarryToothTrinket" ? "CudgelSkill:1"
                : ParentEntity?.BlueprintName == "TonguelessBellTrinket" ? "Persuasion_Recruit:1,Persuasion_Dismiss:1" : null;
            if (replacement == null || !string.IsNullOrEmpty(EquipSkillBonuses)
                || string.IsNullOrEmpty(EquipBonuses)) return;
            var wearer = ParentEntity.GetPart<PhysicsPart>()?.Equipped;
            if (wearer != null)
                StatModifiers.Remove(wearer, StatModifiers.Parse(AppliedEquipBonuses ?? EquipBonuses));
            EquipBonuses = "";
            AppliedEquipBonuses = "";
            EquipSkillBonuses = replacement;
            if (wearer != null)
                AppliedEquipSkillBonuses = CavesOfOoo.Skills.SkillModifiers.Format(
                    CavesOfOoo.Skills.SkillModifiers.Replace(wearer,
                        CavesOfOoo.Skills.SkillModifiers.Parse(AppliedEquipSkillBonuses),
                        CavesOfOoo.Skills.SkillModifiers.Parse(replacement)));
        }

        /// <summary>
        /// Get the effective slots list. Returns UsesSlots if set, otherwise Slot.
        /// </summary>
        public string GetEffectiveSlots()
        {
            if (!string.IsNullOrWhiteSpace(UsesSlots))
                return UsesSlots;

            string derivedSlots = GetDerivedHandlingSlots();
            if (!string.IsNullOrEmpty(derivedSlots))
                return derivedSlots;

            return Slot;
        }

        /// <summary>
        /// Get the slot types as an array. Handles comma-separated values.
        /// </summary>
        public string[] GetSlotArray()
        {
            string slots = GetEffectiveSlots();
            if (string.IsNullOrEmpty(slots))
                return new string[] { "Hand" };
            return slots.Split(',');
        }

        /// <summary>
        /// How many slots of the primary type does this item require?
        /// (e.g. "Hand,Hand" = 2 Hand slots)
        /// </summary>
        public int GetSlotsRequired(string type)
        {
            string[] arr = GetSlotArray();
            int count = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].Trim() == type)
                    count++;
            }
            return count;
        }

        private string GetDerivedHandlingSlots()
        {
            if (ParentEntity == null)
                return null;

            if (ParentEntity.GetPart<HandlingPart>() == null)
                return null;

            string[] derived = HandlingService.GetDefaultSlots(ParentEntity);
            if (derived == null || derived.Length == 0)
                return null;

            return string.Join(",", derived);
        }
    }
}
