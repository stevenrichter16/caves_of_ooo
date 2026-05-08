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

        // ---- Removal-cause constants (read from the EffectRemoved event by listeners) ----
        // Effects that end themselves (e.g. via a save-based recovery in OnTurnEnd)
        // overwrite LastRemovalCause before setting Duration = 0 so the removal event
        // carries an accurate cause string. The default — duration tick to zero — is
        // CAUSE_DURATION_EXPIRED. External callers (cure spells, dispel mutations)
        // that invoke any RemoveEffect overload get CAUSE_EXTERNAL automatically.
        public const string CAUSE_DURATION_EXPIRED = "duration_expired";
        public const string CAUSE_SAVE_SUCCEEDED = "save_succeeded";
        public const string CAUSE_EXTERNAL = "external";

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
        /// True for one cycle if this effect was added to its owner's
        /// <see cref="StatusEffectsPart"/> while the owner was mid-action
        /// (between <c>BeginTakeAction</c> and <c>EndTurn</c>). The owner's
        /// next <c>HandleEndTurn</c> tick skips this effect and clears the
        /// flag — so an effect applied mid-action survives the apply turn
        /// rather than evaporating in the very <c>EndTurn</c> that follows.
        ///
        /// Concretely: a player who steps onto <c>BearTrap</c> with
        /// <c>StunnedEffect(1)</c> would otherwise tick 1 → 0 in the
        /// EndTurn of the move action and never actually get stunned.
        /// With this flag, the first EndTurn skips, the next EndTurn
        /// ticks normally, and the stun blocks exactly one turn of
        /// action — matching its <c>Duration</c> contract.
        ///
        /// This flag is NEVER serialized; on load it's always false. The
        /// rare case of "saved between apply and EndTurn" reloads with
        /// the apply turn already considered consumed, which is a small
        /// acceptable edge.
        /// </summary>
        public bool JustApplied;

        /// <summary>
        /// Why this effect ended. Defaults to <see cref="CAUSE_DURATION_EXPIRED"/>;
        /// effects with custom recovery (e.g. <see cref="BleedingEffect"/>'s
        /// Toughness save) overwrite this in their <c>OnTurnEnd</c> before
        /// setting <c>Duration = 0</c>. Public <c>RemoveEffect</c> calls in
        /// <see cref="StatusEffectsPart"/> set this to <see cref="CAUSE_EXTERNAL"/>
        /// before cleanup. The value is included in the <c>EffectRemoved</c>
        /// event's <c>Cause</c> parameter.
        /// </summary>
        public string LastRemovalCause = CAUSE_DURATION_EXPIRED;

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
        /// Called BEFORE damage applies (after the dead-target guard, before
        /// resistance and HP decrement). Listeners can mutate the
        /// <see cref="Damage"/> object via <c>e.GetParameter("Damage")</c>
        /// to add/remove attributes or reduce <c>Amount</c>. Mirrors the
        /// Phase F <c>BeforeTakeDamage</c> event hook on
        /// <see cref="CombatSystem.ApplyDamage"/>; this is the
        /// Effect-system surface for that hook.
        ///
        /// To fully block damage, mutate <c>damage.Amount</c> to 0 — the
        /// setter clamps to ≥ 0 so over-reduction is safe. The CombatSystem's
        /// resistance code path then surfaces the attempt as
        /// <c>DamageFullyResisted</c> instead of firing <c>TakeDamage</c>.
        /// </summary>
        public virtual void OnBeforeTakeDamage(Entity target, GameEvent e) { }

        /// <summary>
        /// Return false to prevent the entity from acting this turn (stun, paralysis).
        /// </summary>
        public virtual bool AllowAction(Entity target) => true;

        /// <summary>
        /// Return false to prevent the entity from moving (separate from
        /// <see cref="AllowAction"/>, so a "rooted" effect can keep the
        /// actor able to attack/cast in place but unable to move). Default
        /// returns <see cref="AllowAction"/> so existing AllowAction-blocking
        /// effects (Stunned, Frozen, Paralyzed) continue to block movement
        /// without modification — the override is only meaningful for
        /// effects that want to permit some actions while denying movement
        /// (RootedEffect being the canonical case).
        /// </summary>
        public virtual bool AllowMovement(Entity target) => AllowAction(target);

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

        public virtual void OnBeforeSave(SaveWriter writer) { }

        public virtual void OnAfterSave(SaveWriter writer) { }

        public virtual void OnAfterLoad(SaveReader reader) { }

        public virtual void FinalizeLoad(SaveReader reader) { }
    }
}
