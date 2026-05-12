namespace CavesOfOoo.Core
{
    /// <summary>
    /// Item Enhancements E.1.2 — abstract base for item enhancement
    /// Parts ("infusions," "sigils," "engravings"). An enhancement
    /// is a <see cref="Part"/> attached to an item (weapon, armor,
    /// accessory) that modifies the item's behavior via event hooks
    /// (<see cref="Part.HandleEvent"/>) — on-hit damage adjustments,
    /// on-equip stat bonuses, on-turn ticking effects, etc.
    ///
    /// <para><b>Qud parity:</b> mirrors
    /// <c>/Users/steven/qud-decompiled-project/XRL.World.Parts/IModification.cs</c>.
    /// The lifecycle (Configure → ApplyTier → Applicable → Apply →
    /// Remove) and the Tier-scaled-numbers pattern are direct ports.
    /// CoO defers Qud's <c>Examiner.Difficulty</c>/<c>Examiner.Complexity</c>
    /// scaling helpers (no Examiner Part in CoO yet — see E.5+ polish queue).</para>
    ///
    /// <para><b>Lifecycle (Qud-parity):</b></para>
    /// <list type="number">
    ///   <item><see cref="Configure"/> — runs once at construction. Set
    ///         ctor-time defaults (chance %, damage dice strings, etc.)
    ///         that do NOT depend on tier.</item>
    ///   <item><see cref="ApplyTier"/> — sets <see cref="Tier"/> and
    ///         calls <see cref="TierConfigure"/>. Called by content
    ///         creation paths that want tier-scaled enhancements
    ///         (E.1.4's <c>ItemEnhancing.Apply</c> helper).</item>
    ///   <item><see cref="TierConfigure"/> — runs every <see cref="ApplyTier"/>.
    ///         Scale tier-dependent numbers (chance% by tier, duration
    ///         by tier). Idempotent in the sense that re-running it for
    ///         the same tier produces the same numbers.</item>
    ///   <item><see cref="Applicable"/> — gate before adding the
    ///         enhancement. Returns <c>false</c> to reject (wrong item
    ///         type, slot full, etc.). Default <c>true</c> = opt-out.</item>
    ///   <item><see cref="Apply"/> — runs when the enhancement is added
    ///         to an item. Mutate the item's stats / add Parts / set
    ///         Tags here. <b>Atomicity:</b> per E.1 Lockdown #3, if Apply
    ///         can fail partway, set the "applied" flag EAGERLY (before
    ///         the failure-prone work) so a re-entry from a retry
    ///         doesn't double-apply the successful portion. Lesson
    ///         carried over from F.3.4 GrantsRepAsFollowerPart audit
    ///         (Finding #8).</item>
    ///   <item><see cref="Remove"/> — runs when the enhancement is
    ///         removed. Reverse the <see cref="Apply"/> mutations.</item>
    /// </list>
    ///
    /// <para><b>Save/load (SL.6 reflection contract):</b> public fields
    /// with simple types (int, string, bool, Entity) round-trip
    /// automatically via <c>SaveSystem.WritePublicFields</c>. No
    /// hidden non-serializable refs. Per-enhancement round-trip tests
    /// pin the contract (mirror F.3.5 pattern).</para>
    /// </summary>
    public abstract class IItemEnhancement : Part
    {
        /// <summary>Tier 1-4 (Qud parity). Tier 1 is the baseline;
        /// higher tiers scale the enhancement's primary number(s) via
        /// <see cref="TierConfigure"/>. Round-trips via reflection.</summary>
        public int Tier = 1;

        /// <summary>Ctor runs <see cref="Configure"/> automatically so
        /// every instance has its tier-independent defaults set.</summary>
        protected IItemEnhancement()
        {
            Configure();
        }

        /// <summary>Set the enhancement's tier and re-run
        /// <see cref="TierConfigure"/>. Content creation paths call this
        /// at install time. Re-callable — re-tiering at runtime is rare
        /// but supported.</summary>
        public void ApplyTier(int tier)
        {
            Tier = tier;
            TierConfigure();
        }

        /// <summary>Override to set ctor-time defaults that do not depend
        /// on tier. Runs once during construction. Default no-op.</summary>
        public virtual void Configure() { }

        /// <summary>Override to scale tier-dependent numbers. Runs every
        /// time <see cref="ApplyTier"/> is called. Default no-op.</summary>
        public virtual void TierConfigure() { }

        /// <summary>Gate: should this enhancement be allowed on this
        /// item? Override to filter by item type (e.g. melee-weapon-only,
        /// armor-only), to enforce slot caps, etc. Default <c>true</c>
        /// — enhancements opt OUT, not in. Null item rejected.</summary>
        public virtual bool Applicable(Entity item)
        {
            return item != null;
        }

        /// <summary>Runs when the enhancement is added to the item.
        /// Override to mutate item state (add Parts, modify stats,
        /// set Tags). Default no-op. Null-safe.</summary>
        public virtual void Apply(Entity item) { }

        /// <summary>Runs when the enhancement is removed. Override to
        /// reverse <see cref="Apply"/>. Default no-op. Null-safe.</summary>
        public virtual void Remove(Entity item) { }

        /// <summary>Display name shown to the player (e.g. "Serrated",
        /// "Lacquered", "Engraved with the Spiral"). Override to provide
        /// content-readable text. Default returns the type name.</summary>
        public virtual string GetDisplayName()
        {
            return GetType().Name;
        }

        // ── Content hooks (E.2 — concrete enhancements override) ───

        /// <summary>Called when the parent item is the weapon used in a
        /// successful melee hit. Override to react to combat (apply
        /// status effects to the defender, modify damage, etc.).
        /// Fired by <see cref="ItemEnhancementDispatch.DispatchOnHit"/>
        /// from <c>CombatSystem.PerformSingleAttack</c>. Default no-op.
        /// </summary>
        public virtual void OnAttackerHit(
            Entity defender, Entity attacker, Damage damage,
            int actualDamage, Zone zone, System.Random rng)
        { }

        /// <summary>Called when the parent item is equipped by an actor.
        /// Override to apply on-equip bonuses (stat bonuses, faction
        /// rep, ongoing effects). Fired by
        /// <see cref="ItemEnhancementDispatch.DispatchOnEquip"/> from
        /// <c>EquipCommand</c> after the equip transaction commits.
        /// Default no-op.</summary>
        public virtual void OnEquipped(Entity actor, Entity item) { }

        /// <summary>Called when the parent item is unequipped. Override
        /// to reverse <see cref="OnEquipped"/> bonuses. Fired by
        /// <see cref="ItemEnhancementDispatch.DispatchOnUnequip"/>
        /// from <c>UnequipCommand</c>. Default no-op.</summary>
        public virtual void OnUnequipped(Entity actor, Entity item) { }
    }
}
