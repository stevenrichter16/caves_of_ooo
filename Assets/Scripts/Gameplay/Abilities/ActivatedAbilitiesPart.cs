using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Part that manages an entity's activated abilities.
    /// Mirrors Qud's ActivatedAbilities part: maintains an ordered list of abilities,
    /// handles cooldown ticking on EndTurn, and provides lookup by ID or slot index.
    /// </summary>
    public class ActivatedAbilitiesPart : Part
    {
        public override string Name => "ActivatedAbilities";

        /// <summary>
        /// All abilities indexed by their Guid.
        /// </summary>
        public Dictionary<Guid, ActivatedAbility> AbilityByGuid = new Dictionary<Guid, ActivatedAbility>();

        /// <summary>
        /// Ordered list of abilities for slot-based access (keys 1-5).
        /// </summary>
        public List<ActivatedAbility> AbilityList = new List<ActivatedAbility>();

        /// <summary>
        /// Register a new activated ability. Returns its Guid for later reference.
        /// </summary>
        public Guid AddAbility(string displayName, string command, string abilityClass)
        {
            var ability = new ActivatedAbility
            {
                ID = Guid.NewGuid(),
                DisplayName = displayName,
                Command = command,
                Class = abilityClass,
                CooldownRemaining = 0,
                MaxCooldown = 0
            };
            AbilityByGuid[ability.ID] = ability;
            AbilityList.Add(ability);
            return ability.ID;
        }

        /// <summary>
        /// Remove an ability by its Guid.
        /// </summary>
        public bool RemoveAbility(Guid id)
        {
            if (!AbilityByGuid.TryGetValue(id, out var ability))
                return false;
            AbilityByGuid.Remove(id);
            AbilityList.Remove(ability);
            return true;
        }

        /// <summary>
        /// Get an ability by its Guid.
        /// </summary>
        public ActivatedAbility GetAbility(Guid id)
        {
            AbilityByGuid.TryGetValue(id, out var ability);
            return ability;
        }

        /// <summary>
        /// Get an ability by its slot index (0-based, for keys 1-5).
        /// Returns null if slot is out of range.
        /// </summary>
        public ActivatedAbility GetAbilityBySlot(int slot)
        {
            if (slot < 0 || slot >= AbilityList.Count)
                return null;
            return AbilityList[slot];
        }

        /// <summary>
        /// Put an ability on cooldown for the specified number of turns.
        /// </summary>
        public void CooldownAbility(Guid id, int turns)
        {
            if (AbilityByGuid.TryGetValue(id, out var ability))
            {
                ability.CooldownRemaining = turns;
                ability.MaxCooldown = turns;
            }
        }

        /// <summary>
        /// Tick all cooldowns down by 1 (called each turn).
        /// </summary>
        public void TickCooldowns()
        {
            for (int i = 0; i < AbilityList.Count; i++)
            {
                if (AbilityList[i].CooldownRemaining > 0)
                    AbilityList[i].CooldownRemaining--;
            }
        }

        /// <summary>
        /// Handle EndTurn event to tick cooldowns.
        /// </summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "EndTurn")
            {
                TickCooldowns();
            }
            return true;
        }
    }
}
