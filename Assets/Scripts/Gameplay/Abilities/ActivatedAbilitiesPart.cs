using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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
        /// Number of number-key slots (1-9, 0) available for ability binding.
        /// </summary>
        public const int SlotCount = 10;

        /// <summary>
        /// All abilities indexed by their Guid.
        /// </summary>
        public Dictionary<Guid, ActivatedAbility> AbilityByGuid = new Dictionary<Guid, ActivatedAbility>();

        /// <summary>
        /// Ordered list of abilities. Registration order; used by UI enumeration
        /// and as the source for the legacy save migration in <see cref="OnDeserialized"/>.
        /// </summary>
        public List<ActivatedAbility> AbilityList = new List<ActivatedAbility>();

        /// <summary>
        /// Per-slot ability binding. Each entry is a Guid that keys into
        /// <see cref="AbilityByGuid"/>. <see cref="Guid.Empty"/> means the slot is empty.
        /// Index 0..8 correspond to number keys 1-9, index 9 corresponds to 0.
        /// </summary>
        public Guid[] SlotAssignments = new Guid[SlotCount];

        /// <summary>
        /// Register a new activated ability. Returns its Guid for later reference.
        /// The new ability is auto-bound to the first empty <see cref="SlotAssignments"/>
        /// entry so freshly-read grimoires immediately map to a number key. If every
        /// slot is occupied, the ability still exists in the list/dictionary but is
        /// unbound until the player assigns it via <see cref="AssignAbilityToSlot"/>.
        /// </summary>
        public Guid AddAbility(
            string displayName,
            string command,
            string abilityClass,
            AbilityTargetingMode targetingMode = AbilityTargetingMode.AdjacentCell,
            int range = 1,
            string sourceMutationClass = "")
        {
            var ability = new ActivatedAbility
            {
                ID = Guid.NewGuid(),
                DisplayName = displayName,
                Command = command,
                Class = abilityClass,
                TargetingMode = targetingMode,
                Range = range < 1 ? 1 : range,
                CooldownRemaining = 0,
                MaxCooldown = 0,
                SourceMutationClass = sourceMutationClass ?? ""
            };
            AbilityByGuid[ability.ID] = ability;
            AbilityList.Add(ability);

            // Auto-bind to first empty slot so the number key works immediately.
            for (int i = 0; i < SlotCount; i++)
            {
                if (SlotAssignments[i] == Guid.Empty)
                {
                    SlotAssignments[i] = ability.ID;
                    break;
                }
            }

            return ability.ID;
        }

        /// <summary>
        /// Remove an ability by its Guid. Also clears any slot it occupied.
        /// </summary>
        public bool RemoveAbility(Guid id)
        {
            if (!AbilityByGuid.TryGetValue(id, out var ability))
                return false;
            AbilityByGuid.Remove(id);
            AbilityList.Remove(ability);

            for (int i = 0; i < SlotCount; i++)
            {
                if (SlotAssignments[i] == id)
                    SlotAssignments[i] = Guid.Empty;
            }
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
        /// Get an ability by its slot index (0-based, for keys 1-9 and 0).
        /// Returns null if the slot is out of range or empty.
        /// </summary>
        public ActivatedAbility GetAbilityBySlot(int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                return null;
            Guid id = SlotAssignments[slot];
            if (id == Guid.Empty)
                return null;
            AbilityByGuid.TryGetValue(id, out var ability);
            return ability;
        }

        /// <summary>
        /// Assign an ability to a specific slot. If the ability is already bound to
        /// another slot, that other slot is cleared first so there are never duplicate
        /// bindings. Passing <see cref="Guid.Empty"/> clears the target slot.
        /// </summary>
        public void AssignAbilityToSlot(Guid id, int slot)
        {
            if (slot < 0 || slot >= SlotCount)
                return;

            if (id != Guid.Empty)
            {
                if (!AbilityByGuid.ContainsKey(id))
                    return;

                // Clear the ability from any other slot it currently occupies.
                for (int i = 0; i < SlotCount; i++)
                {
                    if (SlotAssignments[i] == id)
                        SlotAssignments[i] = Guid.Empty;
                }
            }

            SlotAssignments[slot] = id;
        }

        /// <summary>
        /// Returns the slot index an ability is bound to, or -1 if unbound.
        /// </summary>
        public int GetSlotForAbility(Guid id)
        {
            if (id == Guid.Empty)
                return -1;
            for (int i = 0; i < SlotCount; i++)
            {
                if (SlotAssignments[i] == id)
                    return i;
            }
            return -1;
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

        /// <summary>
        /// Post-deserialize hook that backfills <see cref="SlotAssignments"/> from
        /// <see cref="AbilityList"/> for legacy saves made before per-slot bindings
        /// existed. Invoked by any serializer that honors
        /// <see cref="OnDeserializedAttribute"/> (Newtonsoft, System.Text.Json with
        /// DataContract, BinaryFormatter). Callers that deserialize via a different
        /// path can invoke <see cref="MigrateLegacyAssignments"/> directly.
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            MigrateLegacyAssignments();
        }

        /// <summary>
        /// Ensure <see cref="SlotAssignments"/> is the correct length and, if the
        /// array is entirely empty but <see cref="AbilityList"/> has entries,
        /// backfill from registration order. This preserves the old "first ability
        /// read goes on key 1" behavior for characters saved before this feature.
        /// </summary>
        public void MigrateLegacyAssignments()
        {
            if (SlotAssignments == null || SlotAssignments.Length != SlotCount)
            {
                var newArr = new Guid[SlotCount];
                if (SlotAssignments != null)
                {
                    int n = Math.Min(SlotAssignments.Length, SlotCount);
                    for (int i = 0; i < n; i++)
                        newArr[i] = SlotAssignments[i];
                }
                SlotAssignments = newArr;
            }

            bool allEmpty = true;
            for (int i = 0; i < SlotCount; i++)
            {
                if (SlotAssignments[i] != Guid.Empty)
                {
                    allEmpty = false;
                    break;
                }
            }

            if (allEmpty && AbilityList != null && AbilityList.Count > 0)
            {
                int n = Math.Min(SlotCount, AbilityList.Count);
                for (int i = 0; i < n; i++)
                    SlotAssignments[i] = AbilityList[i].ID;
            }
        }
    }
}
