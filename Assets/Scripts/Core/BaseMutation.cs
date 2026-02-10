using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Abstract base class for all mutations.
    /// Mirrors Qud's BaseMutation: a Part with level, mutation type,
    /// Mutate/Unmutate lifecycle, and helpers for activated ability management.
    /// Each concrete mutation extends this and overrides Mutate/Unmutate/HandleEvent.
    /// </summary>
    public abstract class BaseMutation : Part
    {
        /// <summary>
        /// The base level of this mutation (without bonuses).
        /// </summary>
        public int BaseLevel = 1;

        /// <summary>
        /// The effective level of this mutation. Can be overridden for bonus calculation.
        /// </summary>
        public virtual int Level => BaseLevel;

        /// <summary>
        /// Most mutations have an activated ability; this stores its ID.
        /// </summary>
        public Guid ActivatedAbilityID;

        /// <summary>
        /// The type of mutation: "Physical" or "Mental".
        /// </summary>
        public abstract string MutationType { get; }

        /// <summary>
        /// Display name for the mutation (shown in UI).
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Called when this mutation is granted to an entity.
        /// Override to register activated abilities, apply passive bonuses, etc.
        /// </summary>
        public virtual void Mutate(Entity entity, int level)
        {
            BaseLevel = level;
        }

        /// <summary>
        /// Called when this mutation is removed from an entity.
        /// Override to clean up abilities, remove bonuses, etc.
        /// </summary>
        public virtual void Unmutate(Entity entity)
        {
        }

        // --- Activated Ability Helpers ---

        /// <summary>
        /// Register an activated ability on the parent entity.
        /// Returns the ability's Guid for later reference.
        /// Mirrors Qud's AddMyActivatedAbility.
        /// </summary>
        protected Guid AddMyActivatedAbility(string displayName, string command, string abilityClass)
        {
            var abilities = ParentEntity?.GetPart<ActivatedAbilitiesPart>();
            if (abilities == null)
                return Guid.Empty;
            return abilities.AddAbility(displayName, command, abilityClass);
        }

        /// <summary>
        /// Remove a previously registered activated ability.
        /// </summary>
        protected void RemoveMyActivatedAbility(Guid id)
        {
            if (id == Guid.Empty) return;
            var abilities = ParentEntity?.GetPart<ActivatedAbilitiesPart>();
            abilities?.RemoveAbility(id);
        }

        /// <summary>
        /// Put the mutation's activated ability on cooldown.
        /// </summary>
        protected void CooldownMyActivatedAbility(Guid id, int turns)
        {
            if (id == Guid.Empty) return;
            var abilities = ParentEntity?.GetPart<ActivatedAbilitiesPart>();
            abilities?.CooldownAbility(id, turns);
        }

        /// <summary>
        /// Check if the mutation's activated ability is usable (off cooldown).
        /// </summary>
        protected bool IsMyActivatedAbilityUsable()
        {
            if (ActivatedAbilityID == Guid.Empty) return false;
            var abilities = ParentEntity?.GetPart<ActivatedAbilitiesPart>();
            var ability = abilities?.GetAbility(ActivatedAbilityID);
            return ability?.IsUsable ?? false;
        }
    }
}
