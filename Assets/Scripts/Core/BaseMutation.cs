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
        private string _rapidKey;

        /// <summary>
        /// The base level of this mutation (without bonuses).
        /// </summary>
        public int BaseLevel = 1;

        /// <summary>
        /// Last synced effective level. Updated during ChangeLevel.
        /// </summary>
        public int LastLevel;

        /// <summary>
        /// Optional level-cap override. -1 means no override.
        /// </summary>
        public int CapOverride = -1;

        /// <summary>
        /// The effective level of this mutation (base + modifiers + cap).
        /// </summary>
        public virtual int Level => CalcLevel();

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
        /// Whether this mutation changes body part layout/anatomy.
        /// Phase-7 seam for future body-system parity.
        /// </summary>
        public virtual bool AffectsBodyParts => false;

        /// <summary>
        /// Whether this mutation creates body-linked default equipment.
        /// Phase-7 seam for future body-system parity.
        /// </summary>
        public virtual bool GeneratesEquipment => false;

        /// <summary>
        /// Called when this mutation is granted to an entity.
        /// Override to register activated abilities, apply passive bonuses, etc.
        /// </summary>
        public virtual void Mutate(Entity entity, int level)
        {
            BaseLevel = level;
            ChangeLevel(Level);
        }

        /// <summary>
        /// Called when this mutation is removed from an entity.
        /// Override to clean up abilities, remove bonuses, etc.
        /// </summary>
        public virtual void Unmutate(Entity entity)
        {
            LastLevel = 0;
        }

        /// <summary>
        /// Level-change hook. Concrete mutations can override to apply effects that scale with rank.
        /// </summary>
        public virtual bool ChangeLevel(int newLevel)
        {
            LastLevel = newLevel;
            return true;
        }

        /// <summary>
        /// Lifecycle seam invoked before mutation body rebuild/remutate cycle.
        /// </summary>
        public virtual void OnBeforeBodyRebuild(Entity entity, string reason) { }

        /// <summary>
        /// Lifecycle seam invoked after mutation body rebuild/remutate cycle.
        /// </summary>
        public virtual void OnAfterBodyRebuild(Entity entity, string reason) { }

        /// <summary>
        /// Whether this mutation can be advanced by spending mutation points.
        /// </summary>
        public virtual bool CanLevel()
        {
            return true;
        }

        /// <summary>
        /// Default max rank. If metadata exists in MutationRegistry, uses that value.
        /// </summary>
        public virtual int GetMaxLevel()
        {
            return MutationRegistry.GetMaxLevelForClass(GetType().Name, 10);
        }

        public bool CanIncreaseLevel()
        {
            if (!CanLevel())
                return false;
            if (BaseLevel >= GetMaxLevel())
                return false;
            int cap = GetMutationCap();
            if (cap == -1)
                return true;
            return Level < cap;
        }

        public static int GetMutationCapForLevel(int level)
        {
            return level / 2 + 1;
        }

        /// <summary>
        /// Returns the mutation cap for this entity, or -1 when uncapped.
        /// </summary>
        public virtual int GetMutationCap()
        {
            if (ParentEntity == null)
                return CapOverride;
            if (ParentEntity.GetStat("Level") == null)
                return CapOverride;

            int level = ParentEntity.GetStatValue("Level", 1);
            int defaultCap = GetMutationCapForLevel(level);
            return Math.Max(CapOverride, defaultCap);
        }

        /// <summary>
        /// Effective level calculation used for gameplay and UI.
        /// Phase-1 parity: base rank + rapid rank + mutation mods, then cap and floor.
        /// </summary>
        public virtual int CalcLevel()
        {
            if (!CanLevel() || ParentEntity == null)
                return BaseLevel;

            int level = BaseLevel;
            level += GetRapidLevelAmount();

            var mutations = ParentEntity.GetPart<MutationsPart>();
            if (mutations != null)
                level += mutations.GetLevelAdjustmentsForMutation(GetType().Name);

            if (level < 1)
                level = 1;

            int cap = GetMutationCap();
            if (cap != -1 && level > cap)
                level = cap;

            return level;
        }

        public int GetRapidLevelAmount()
        {
            if (_rapidKey == null)
                _rapidKey = "RapidLevel_" + GetType().Name;
            return ParentEntity?.GetIntProperty(_rapidKey, 0) ?? 0;
        }

        public void SetRapidLevelAmount(int amount)
        {
            if (_rapidKey == null)
                _rapidKey = "RapidLevel_" + GetType().Name;
            if (ParentEntity == null)
                return;

            ParentEntity.SetIntProperty(_rapidKey, amount, removeIfZero: true);
            ChangeLevel(Level);
        }

        public virtual void RapidLevel(int amount)
        {
            if (_rapidKey == null)
                _rapidKey = "RapidLevel_" + GetType().Name;
            if (ParentEntity == null || amount == 0)
                return;

            ParentEntity.ModIntProperty(_rapidKey, amount, removeIfZero: true);
            ChangeLevel(Level);
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

        /// <summary>
        /// Register mutation-generated equipment so MutationsPart can auto-clean/remutate it.
        /// </summary>
        protected Guid RegisterGeneratedEquipment(
            Entity item,
            bool autoEquip = true,
            bool autoRemoveOnMutationLoss = true)
        {
            var mutations = ParentEntity?.GetPart<MutationsPart>();
            if (mutations == null)
                return Guid.Empty;

            return mutations.RegisterMutationGeneratedEquipment(
                this,
                item,
                autoEquip,
                autoRemoveOnMutationLoss);
        }

        /// <summary>
        /// Remove mutation-generated equipment entries/items for this mutation.
        /// </summary>
        protected int CleanupGeneratedEquipment(bool force = false)
        {
            var mutations = ParentEntity?.GetPart<MutationsPart>();
            if (mutations == null)
                return 0;

            return mutations.CleanupMutationGeneratedEquipment(this, force);
        }
    }
}
