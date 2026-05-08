using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Pyromancy active ability: sacrifice
    /// <see cref="HP_SACRIFICE_PERCENT"/>% of current HP to charge up
    /// <see cref="BUFF_CHARGES"/> fire-spell amplifications. Each of
    /// the next <see cref="BUFF_CHARGES"/> Heat-element spells cast
    /// within <see cref="BUFF_DURATION"/> turns deals
    /// <see cref="DAMAGE_BONUS_PERCENT"/>% bonus damage. Distinct from
    /// LeyTap (universal, single charge) — HeartFlame is fire-specific
    /// and multi-charge.
    ///
    /// <para><b>Mechanic:</b> SelfCentered, no targeting, no weapon
    /// gate. Drains HP from the actor (rounded to integer, clamped
    /// at 1 minimum), then sets internal charge state. Implements
    /// <see cref="BaseSkillPart.OnGetSpellDamageModifier"/>: when an
    /// active spell fires AND the buff is live AND the element matches
    /// "Heat", the modifier returns a damage bonus computed from the
    /// active spell's base damage × DAMAGE_BONUS_PERCENT/100. Each
    /// successful trigger decrements the charge counter; charges
    /// expire after BUFF_DURATION turns regardless of usage.</para>
    ///
    /// <para>Buff state lives ON THIS SKILL'S INSTANCE rather than as
    /// a separate Effect class — the SkillEventDispatcher already
    /// invokes OnGetSpellDamageModifier per skill, so consolidating
    /// the buffer + the modifier reader on the same class keeps the
    /// state-passing minimal. Tradeoff: the buff doesn't show up in
    /// StatusEffectsPart UI (the effect-list view), but
    /// <see cref="MessageLog"/> entries surface the activation.</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Pyromancy_HeartFlame</c>):
    /// "trades HP but for fire-specific bonus; LeyTap is universal."</para>
    /// </summary>
    public class Pyromancy_HeartFlame : BaseSkillPart
    {
        public override string Name => nameof(Pyromancy_HeartFlame);

        public const int COOLDOWN = 100;
        public const int HP_SACRIFICE_PERCENT = 50;
        public const int BUFF_CHARGES = 3;
        public const int BUFF_DURATION = 5;
        public const int DAMAGE_BONUS_PERCENT = 100; // ×2 = +100% bonus

        // Buff state. NonSerialized so save/load doesn't preserve a
        // mid-buff window — same convention as Shank's _activePenBonus.
        [System.NonSerialized]
        private int _chargesRemaining = 0;
        [System.NonSerialized]
        private int _expiresAtTurn = -1;

        public int ChargesRemaining => _chargesRemaining;
        public int ExpiresAtTurn => _expiresAtTurn;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Heart Flame",
                Command = "CommandHeartFlame",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = 0,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return;
            var actor = ctx.Attacker;
            var hp = actor.GetStat("Hitpoints");
            if (hp == null) { EmitSkillRejectedDiag(ctx, "no_hitpoints"); return; }

            int sacrifice = (hp.BaseValue * HP_SACRIFICE_PERCENT) / 100;
            if (sacrifice < 1) sacrifice = 1;
            // Clamp so the actor doesn't suicide on HeartFlame.
            if (sacrifice >= hp.BaseValue) sacrifice = hp.BaseValue - 1;
            if (sacrifice < 1)
            {
                EmitSkillRejectedDiag(ctx, "insufficient_hp");
                return;
            }
            hp.BaseValue -= sacrifice;

            _chargesRemaining = BUFF_CHARGES;
            _expiresAtTurn = (TurnManager.Active?.TickCount ?? 0) + BUFF_DURATION;

            MessageLog.Add(actor.GetDisplayName() + " burns own heart for power! "
                + sacrifice + " HP sacrificed; next " + BUFF_CHARGES
                + " fire spells deal +" + DAMAGE_BONUS_PERCENT + "% damage.");
        }

        public override int OnGetSpellDamageModifier(Entity attacker, Entity defender,
            string elementAttribute, int baseDamage)
        {
            if (_chargesRemaining <= 0) return 0;
            if (string.IsNullOrEmpty(elementAttribute)) return 0;
            // Match Heat or Fire element flavor.
            if (elementAttribute != "Heat" && elementAttribute != "Fire") return 0;
            int currentTurn = TurnManager.Active?.TickCount ?? 0;
            if (currentTurn > _expiresAtTurn)
            {
                // Expired window — drain charges so the diag reflects "spent".
                _chargesRemaining = 0;
                return 0;
            }

            // Consume one charge + return the bonus.
            _chargesRemaining--;
            int bonus = (baseDamage * DAMAGE_BONUS_PERCENT) / 100;
            return bonus;
        }
    }
}
