using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Axe-class active ability: a spinning sweep that strikes ALL 8
    /// adjacent creatures with a normal weapon swing. Each hit fires
    /// the attacker's full combat pipeline (Cleave, Dismember, on-hit
    /// procs etc. — same as a bump attack). The actor doesn't move.
    ///
    /// <para><b>Mechanic (CoO):</b> requires an Axe-attribute weapon
    /// equipped. Iterates the 8 directions in deterministic
    /// N→NE→E→SE→S→SW→W→NW order (mirrors Slam/Shank's adjacent-target
    /// scan), collects every Creature found, then loops the strike list
    /// calling <see cref="CombatSystem.PerformSingleAttack"/> on each.
    /// Cooldown <see cref="COOLDOWN"/> turns. Marker tag
    /// <c>(Whirlwind)</c> in the message log so the player + tests can
    /// see when the ability fired.</para>
    ///
    /// <para>Per the WSP8.2 active-ability brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md</c> §Axe_Whirlwind): "the
    /// only self-AOE multi-target FULL-DAMAGE attack." GroundPound
    /// (proposed Cudgel) is reduced damage + knockback; Pyroclasm
    /// consumes stacks. Whirlwind is "I get N free swings."</para>
    ///
    /// <para>Classification: <b>CoO-original Extension</b> per CLAUDE.md
    /// §4.2 — Qud has a similar <c>SpinningStrike</c> mutation but no
    /// equivalent on the Axe skill tree itself. The mechanic
    /// (8-direction sweep + per-target full PerformSingleAttack) follows
    /// established CoO patterns rather than Qud parity.</para>
    /// </summary>
    public class Axe_Whirlwind : BaseSkillPart
    {
        public override string Name => nameof(Axe_Whirlwind);

        public const int COOLDOWN = 50;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Whirlwind",
                Command = "CommandWhirlwind",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.SelfCentered,
                Range = 0,
                Cooldown = COOLDOWN,
            };
        }

        public override bool OnCommand(SkillEventContext ctx)
        {
            // Determinism: bail on null Rng instead of falling back to a
            // wall-clock-seeded one — mirrors Slam/Shank/Lunge.
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return false;
            var actor = ctx.Attacker;

            // Require an Axe-class weapon equipped (mirrors HookAndDrag's
            // gate). The substring match catches "Cutting Axe" or
            // "Cutting Glaive" composite attributes per the weapon-
            // attribute backfill.
            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Axe");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs an axe equipped to whirlwind.");
                EmitSkillRejectedDiag(ctx, "no_weapon");
                return false;
            }

            if (ctx.Zone == null)
            {
                EmitSkillRejectedDiag(ctx, "no_zone");
                return false;
            }
            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0)
            {
                EmitSkillRejectedDiag(ctx, "actor_not_in_zone");
                return false;
            }

            // One strike per physical owner on the caster's perimeter.
            // Snapshot before damage, push or reactive effects change occupancy.
            var targets = MultiCellAbilityQueries.AdjacentCreatures(ctx.Zone, actor);

            if (targets.Count == 0)
            {
                MessageLog.Add(actor.GetDisplayName() + "'s whirlwind hits nothing.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return false;
            }

            // Revalidate presence and life before each strike: earlier
            // damage hooks may remove either participant. Each valid strike
            // still runs the normal on-hit pipeline (including Cleave and
            // Dismember independently for each selected owner).
            for (int i = 0; i < targets.Count; i++)
            {
                if (ctx.Zone.GetEntityCell(actor) == null || actor.GetStatValue("Hitpoints") <= 0) break;
                if (ctx.Zone.GetEntityCell(targets[i]) == null || targets[i].GetStatValue("Hitpoints") <= 0) continue;
                CombatSystem.PerformSingleAttack(
                    attacker: actor, defender: targets[i],
                    weapon: weapon, isPrimary: true,
                    zone: ctx.Zone, rng: ctx.Rng,
                    attackSourceDesc: "(Whirlwind)");
            }
        
            return true;
        }
    }
}
