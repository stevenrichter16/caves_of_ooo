using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Spellcraft active ability: trade
    /// <see cref="HP_DRAIN_PERCENT"/>% of current HP for a SINGLE
    /// universal spell-damage buff. The next spell cast within
    /// <see cref="BUFF_DURATION"/> turns deals
    /// <see cref="DAMAGE_BONUS_MULTIPLIER"/>× the HP cost as bonus
    /// damage. Distinct from HeartFlame (fire-specific, multi-charge)
    /// — LeyTap is UNIVERSAL and SINGLE-CHARGE.
    ///
    /// <para><b>Mechanic:</b> SelfCentered, no targeting, no weapon
    /// gate. Drains HP, stores the (cost × multiplier) bonus + an
    /// expiry turn on this instance. The next time
    /// <see cref="BaseSkillPart.OnGetSpellDamageModifier"/> fires
    /// while the buff is live (any element), the bonus is returned
    /// and the buff is consumed. Charge expires after BUFF_DURATION
    /// turns even if unused.</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Spellcraft_LeyTap</c>):
    /// "the only ability that trades own HP for buff. HeartFlame
    /// also trades HP but for fire-specific bonus; LeyTap is
    /// universal."</para>
    /// </summary>
    public class Spellcraft_LeyTap : BaseSkillPart
    {
        public override string Name => nameof(Spellcraft_LeyTap);

        public const int COOLDOWN = 40;
        public const int HP_DRAIN_PERCENT = 15;
        public const int BUFF_DURATION = 3;
        public const int DAMAGE_BONUS_MULTIPLIER = 2;

        [System.NonSerialized]
        private int _pendingBonus = 0;
        [System.NonSerialized]
        private int _expiresAtTurn = -1;

        public int PendingBonus => _pendingBonus;
        public int ExpiresAtTurn => _expiresAtTurn;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Ley Tap",
                Command = "CommandLeyTap",
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

            int drain = (hp.BaseValue * HP_DRAIN_PERCENT) / 100;
            if (drain < 1) drain = 1;
            if (drain >= hp.BaseValue) drain = hp.BaseValue - 1;
            if (drain < 1)
            {
                EmitSkillRejectedDiag(ctx, "insufficient_hp");
                return;
            }
            hp.BaseValue -= drain;

            _pendingBonus = drain * DAMAGE_BONUS_MULTIPLIER;
            _expiresAtTurn = (TurnManager.Active?.TickCount ?? 0) + BUFF_DURATION;

            MessageLog.Add(actor.GetDisplayName() + " taps the leylines! "
                + drain + " HP drained; next spell deals +" + _pendingBonus + " bonus damage.");
        }

        public override int OnGetSpellDamageModifier(Entity attacker, Entity defender,
            string elementAttribute, int baseDamage)
        {
            if (_pendingBonus <= 0) return 0;
            int currentTurn = TurnManager.Active?.TickCount ?? 0;
            if (currentTurn > _expiresAtTurn)
            {
                _pendingBonus = 0;
                return 0;
            }
            // Consume the buff (single-charge) + return the stored bonus.
            int bonus = _pendingBonus;
            _pendingBonus = 0;
            return bonus;
        }
    }
}
