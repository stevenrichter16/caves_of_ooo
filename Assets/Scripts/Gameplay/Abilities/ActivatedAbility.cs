using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Plain data class representing a single activated ability.
    /// Mirrors Qud's ActivatedAbilityEntry: ID, display name, command string,
    /// category class, and cooldown tracking.
    /// </summary>
    public class ActivatedAbility
    {
        /// <summary>
        /// Unique identifier for this ability instance.
        /// </summary>
        public Guid ID;

        /// <summary>
        /// Name shown in the UI / ability bar.
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// The command event ID fired when the player activates this ability.
        /// e.g. "CommandFlamingHands"
        /// </summary>
        public string Command;

        /// <summary>
        /// Category for grouping (e.g. "Physical Mutations", "Mental Mutations").
        /// </summary>
        public string Class;

        /// <summary>
        /// How the player targets this ability from input.
        /// </summary>
        public AbilityTargetingMode TargetingMode = AbilityTargetingMode.AdjacentCell;

        /// <summary>
        /// Maximum input-targeting range for this ability.
        /// </summary>
        public int Range = 1;

        /// <summary>
        /// Turns remaining on cooldown. 0 = ready.
        /// </summary>
        public int CooldownRemaining;

        /// <summary>
        /// Maximum cooldown when ability is used.
        /// </summary>
        public int MaxCooldown;

        /// <summary>
        /// Whether the ability is currently usable (off cooldown).
        /// </summary>
        public bool IsUsable => CooldownRemaining <= 0;
    }
}
