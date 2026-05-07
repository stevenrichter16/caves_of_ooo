namespace CavesOfOoo.Core
{
    /// <summary>
    /// Electrified: short-duration charge that stuns creatures on apply,
    /// chains along conductive materials on each turn start, and is
    /// amplified by moisture. Drives lightning_plus_conductor reactions.
    /// </summary>
    public class ElectrifiedEffect : Effect
    {
        public override string DisplayName => "electrified";

        // WSP6.16 — TYPE_NEGATIVE backfill (see AcidicEffect.cs).
        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        /// <summary>Joules-equivalent charge. Higher = longer chain and stronger zap.</summary>
        public float Charge;

        public ElectrifiedEffect(float charge = 1.0f)
        {
            Charge = charge < 0f ? 0f : charge;
            Duration = 2;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is electrified!");

            // Wet targets receive amplified charge (and the effect lingers longer).
            var wet = target.GetEffect<WetEffect>();
            if (wet != null && wet.Moisture > 0.2f)
            {
                Charge *= 2f;
                Duration += 1;
            }

            // Stun creatures briefly on contact. Non-creatures (props) ignore the stun
            // but still carry the Electrified state for propagation.
            if (target.HasTag("Creature"))
                target.ApplyEffect(new StunnedEffect(duration: 1), null, null);
        }

        /// <summary>
        /// Per-turn lightning damage tick. Damage =
        /// <c>1 + floor(Charge × 1.5)</c>, attributed as <c>Lightning</c>
        /// so it routes through <c>ElectricResistance</c> in
        /// <see cref="CombatSystem.ApplyResistances"/>. Mirrors
        /// <see cref="BurningEffect.OnTurnStart"/>'s damage tick to give
        /// <see cref="ElectrifiedEffect"/> parity with Fire/Acid as a
        /// damage-over-time effect — pre-fix it was a stun-only control
        /// effect, asymmetric with the rest of the elemental row.
        ///
        /// <para>Skipped if charge is 0 (degenerate input — e.g. a
        /// fully-decayed effect that hasn't been removed yet).</para>
        /// </summary>
        public override void OnTurnStart(Entity target, GameEvent context)
        {
            if (Charge <= 0f) return;
            if (target == null) return;
            if (target.GetStatValue("Hitpoints", 0) <= 0) return;

            int amount = 1 + (int)System.Math.Floor(Charge * 1.5f);
            if (amount <= 0) return;

            var damage = new Damage(amount);
            damage.AddAttribute("Lightning");

            // Source is null — the original electrifier (player who threw a
            // tonic, ThunderHammer wielder) isn't threaded through to the
            // Effect today. Killing-blow attribution falls back to "killed by
            // the electrified status" rather than the player. Tracked as a
            // 🔵 follow-up; mirrors BurningEffect's IgnitionSource pattern
            // when we wire StatusTonicPart / OnHitEffectFactory to thread
            // the original source.
            Zone zone = context?.GetParameter<Zone>("Zone");
            // damage is already a typed Damage with the "Lightning"
            // attribute set above (line 65) — uses the typed overload of
            // ApplyDamage which fires ApplyResistances. Pre-WSP7.4 this
            // was already correct (just verified during the WSP7.4 audit).
            CombatSystem.ApplyDamage(target, damage, source: null, zone);
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is no longer electrified.");
        }

        public override void OnTurnEnd(Entity target, GameEvent context)
        {
            // Propagate along conductors during the sim tick so chains resolve
            // during the turn, not at application time. Fire this from OnTurnEnd
            // rather than OnTurnStart so passive electrified props (which only
            // receive EndTurn via MaterialSimSystem's passive loop, never
            // BeginTakeAction) still chain. Burning electrified entities also
            // receive EndTurn via the burning-list pass, so this single hook
            // covers both cases without double-firing.
            var chain = GameEvent.New("TryChainElectricity");
            chain.SetParameter("Charge", (object)Charge);
            chain.SetParameter("Zone", context?.GetParameter<Zone>("Zone"));
            chain.SetParameter("Source", (object)target);
            target.FireEvent(chain);
            chain.Release();

            // Default duration decrement — preserved from base Effect.OnTurnEnd.
            if (Duration > 0)
                Duration--;
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is ElectrifiedEffect zap)
            {
                Charge = System.Math.Max(Charge, zap.Charge);
                Duration = System.Math.Max(Duration, zap.Duration);
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&Y";
    }
}
