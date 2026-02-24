namespace CavesOfOoo.Core
{
    /// <summary>
    /// Abstract base for all status effects.
    /// Effects are lightweight objects managed by StatusEffectsPart.
    /// They hook into the turn/combat event system via virtual callbacks.
    /// Duration tracks turns remaining; -1 = indefinite (e.g. save-based recovery).
    /// </summary>
    public abstract class Effect
    {
        // Qud-aligned effect type flags.
        public const int TYPE_GENERAL = 1;
        public const int TYPE_MENTAL = 2;
        public const int TYPE_METABOLIC = 4;
        public const int TYPE_RESPIRATORY = 8;
        public const int TYPE_CIRCULATORY = 16;
        public const int TYPE_CONTACT = 32;
        public const int TYPE_FIELD = 64;
        public const int TYPE_ACTIVITY = 128;
        public const int TYPE_DIMENSIONAL = 256;
        public const int TYPE_CHEMICAL = 512;
        public const int TYPE_STRUCTURAL = 1024;
        public const int TYPE_SONIC = 2048;
        public const int TYPE_TEMPORAL = 4096;
        public const int TYPE_NEUROLOGICAL = 8192;
        public const int TYPE_DISEASE = 16384;
        public const int TYPE_PSIONIC = 32768;
        public const int TYPE_POISON = 65536;
        public const int TYPE_EQUIPMENT = 131072;
        public const int TYPE_MINOR = 16777216;
        public const int TYPE_NEGATIVE = 33554432;
        public const int TYPE_REMOVABLE = 67108864;
        public const int TYPE_VOLUNTARY = 134217728;

        public const int DURATION_INDEFINITE = -1;

        /// <summary>
        /// The entity this effect is currently on.
        /// Set by StatusEffectsPart when applied.
        /// </summary>
        public Entity Owner;

        /// <summary>
        /// Turns remaining. Decremented by default in OnTurnEnd.
        /// 0 = expired (will be cleaned up). -1 = indefinite.
        /// </summary>
        public int Duration;

        /// <summary>
        /// Human-readable name for UI display (e.g. "poisoned", "stunned").
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Cached effect class name (Qud-style semantic key).
        /// </summary>
        public string ClassName => GetType().Name;

        /// <summary>
        /// Qud-style effect type mask. Defaults to general.
        /// </summary>
        public virtual int GetEffectType() => TYPE_GENERAL;

        public bool IsOfType(int mask) => (GetEffectType() & mask) != 0;

        public bool IsOfTypes(int mask) => (GetEffectType() & mask) == mask;

        /// <summary>
        /// Check whether this effect can be applied to the target.
        /// Return false to block application entirely.
        /// </summary>
        public virtual bool CanApply(Entity target) => true;

        /// <summary>
        /// Qud-style apply gate alias.
        /// </summary>
        public virtual bool CanBeAppliedTo(Entity target) => CanApply(target);

        /// <summary>
        /// Qud-style apply lifecycle step.
        /// Return false to reject application after pre-checks.
        /// </summary>
        public virtual bool Apply(Entity target)
        {
            OnApply(target);
            return true;
        }

        /// <summary>
        /// Qud-style post-apply lifecycle hook.
        /// </summary>
        public virtual void Applied(Entity target) { }

        /// <summary>
        /// Qud-style remove lifecycle step.
        /// </summary>
        public virtual void Remove(Entity target)
        {
            OnRemove(target);
        }

        /// <summary>
        /// Called when the effect is first applied.
        /// Use for stat modifications, message log, etc.
        /// </summary>
        public virtual void OnApply(Entity target) { }

        /// <summary>
        /// Called when the effect is removed (expired or manually).
        /// Use to restore stat modifications.
        /// </summary>
        public virtual void OnRemove(Entity target) { }

        /// <summary>
        /// Called at the start of the entity's turn, before action.
        /// Use for periodic damage (poison, bleed, burn).
        /// </summary>
        public virtual void OnTurnStart(Entity target) { }

        /// <summary>
        /// Turn-start callback with event context (zone/source/metadata).
        /// </summary>
        public virtual void OnTurnStart(Entity target, GameEvent context)
        {
            OnTurnStart(target);
        }

        /// <summary>
        /// Called at the end of the entity's turn, after action.
        /// Default behavior: decrement Duration if > 0.
        /// Override for save-based recovery (bleeding).
        /// </summary>
        public virtual void OnTurnEnd(Entity target)
        {
            if (Duration > 0)
                Duration--;
        }

        /// <summary>
        /// Turn-end callback with event context (zone/source/metadata).
        /// </summary>
        public virtual void OnTurnEnd(Entity target, GameEvent context)
        {
            OnTurnEnd(target);
        }

        /// <summary>
        /// Called when the entity takes damage.
        /// </summary>
        public virtual void OnTakeDamage(Entity target, GameEvent e) { }

        /// <summary>
        /// Return false to prevent the entity from acting this turn (stun, paralysis).
        /// </summary>
        public virtual bool AllowAction(Entity target) => true;

        /// <summary>
        /// Return a Qud color code to override the entity's render color while this
        /// effect is active. Return null for no override.
        /// </summary>
        public virtual string GetRenderColorOverride() => null;

        /// <summary>
        /// Called when another effect of the same type is being applied.
        /// Return true if stacking was handled (extend duration, upgrade, etc.)
        /// and the new effect should NOT be added as a duplicate.
        /// Return false to allow the new effect to be added alongside this one.
        /// </summary>
        public virtual bool OnStack(Effect incoming) => false;

        /// <summary>
        /// Qud-style render hook. Return false to abort rendering.
        /// </summary>
        public virtual bool Render(GameEvent e) => true;
    }
}
