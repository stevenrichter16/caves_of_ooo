using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// ShortBlades-class active ability: rapid-fire
    /// <see cref="FLURRY_STRIKE_COUNT"/> swings against a single adjacent
    /// creature in one activation. Each strike rolls hit + damage + on-
    /// hit procs (Bloodletter, Jab, Hobble) independently, so a Flurry
    /// can pile up multiple Bleeding/Confused/Hobbled stacks from one
    /// activation.
    ///
    /// <para><b>Mechanic (CoO):</b> requires a Piercing-attribute weapon
    /// equipped (Dagger / Spear / ChoirSpine / TemporalShard). Adjacent
    /// target lookup mirrors Shank's 8-dir scan. Calls
    /// <see cref="CombatSystem.PerformSingleAttack"/> in a loop with
    /// <see cref="FLURRY_STRIKE_COUNT"/> iterations, marker tag
    /// <c>(Flurry)</c>. Loop short-circuits if the target's HP hits 0
    /// mid-flurry — no point swinging at a corpse and the message log
    /// would otherwise add a confusing "missed" line for a dead
    /// defender.</para>
    ///
    /// <para>Per the WSP8.2 active-ability brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md</c> §ShortBlades_Flurry):
    /// "the only ability that fires the attacker's full combat pipeline
    /// multiple times in one activation. Whirlwind hits multiple
    /// TARGETS once; Flurry hits ONE target multiple times."</para>
    ///
    /// <para>Classification: <b>CoO-original Extension</b> per CLAUDE.md
    /// §4.2 — Qud has a "Flurry" feel via Berserk's bonus swings, but
    /// no per-target N-strike active. The mechanic uses CoO's existing
    /// PerformSingleAttack contract directly.</para>
    /// </summary>
    public class ShortBlades_Flurry : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Flurry);

        public const int COOLDOWN = 35;
        public const int FLURRY_STRIKE_COUNT = 3;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Flurry",
                Command = "CommandFlurry",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            // Determinism: bail on null Rng — mirrors Shank's pattern.
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;

            // Require a Piercing-class weapon equipped (mirrors Shank's
            // gate — Flurry is Shank's high-damage cousin).
            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Piercing");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs a piercing-class weapon to flurry.");
                EmitSkillRejectedDiag(ctx, "no_weapon");
                return;
            }

            if (ctx.Zone == null)
            {
                EmitSkillRejectedDiag(ctx, "no_zone");
                return;
            }
            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0)
            {
                EmitSkillRejectedDiag(ctx, "actor_not_in_zone");
                return;
            }

            // Find adjacent target (mirrors Shank's 8-dir lookup).
            Entity target = null;
            for (int dir = 0; dir < 8 && target == null; dir++)
            {
                var cell = ctx.Zone.GetCellInDirection(actorPos.x, actorPos.y, dir);
                if (cell == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == actor) continue;
                    if (!e.Tags.ContainsKey("Creature")) continue;
                    target = e;
                    break;
                }
            }

            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " has nothing to flurry.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            // Strike the target FLURRY_STRIKE_COUNT times. Skip strikes
            // after target HP hits 0 — no point swinging at a corpse and
            // the message log would otherwise log "missed" for a dead
            // defender (PerformSingleAttack short-circuits internally on
            // HP≤0 but the log would still get a stale entry).
            for (int strike = 0; strike < FLURRY_STRIKE_COUNT; strike++)
            {
                int hp = target.GetStatValue("Hitpoints");
                if (hp <= 0) break;
                CombatSystem.PerformSingleAttack(
                    attacker: actor, defender: target,
                    weapon: weapon, isPrimary: true,
                    zone: ctx.Zone, rng: ctx.Rng,
                    attackSourceDesc: "(Flurry)");
            }
        }
    }
}
