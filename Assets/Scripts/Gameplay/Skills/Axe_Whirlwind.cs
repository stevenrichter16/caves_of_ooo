using System.Collections.Generic;
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

        public override void OnCommand(SkillEventContext ctx)
        {
            // Determinism: bail on null Rng instead of falling back to a
            // wall-clock-seeded one — mirrors Slam/Shank/Lunge.
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;

            // Require an Axe-class weapon equipped (mirrors HookAndDrag's
            // gate). The substring match catches "Cutting Axe" or
            // "Cutting Glaive" composite attributes per the weapon-
            // attribute backfill.
            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Axe");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs an axe equipped to whirlwind.");
                return;
            }

            if (ctx.Zone == null) return;
            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) return;

            // Snapshot adjacent creatures BEFORE swinging. PerformSingleAttack
            // can move/kill targets mid-loop (dismember, push effects,
            // reflexive counter-moves) — iterating the live cells would
            // be unstable. The snapshot freezes the target list at "8-dir
            // adjacency at activation time" which matches the player's
            // mental model: "I spin once and hit whoever's standing
            // around me NOW".
            var targets = new List<Entity>(8);
            for (int dir = 0; dir < 8; dir++)
            {
                var cell = ctx.Zone.GetCellInDirection(actorPos.x, actorPos.y, dir);
                if (cell == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == actor) continue;
                    if (!e.Tags.ContainsKey("Creature")) continue;
                    targets.Add(e);
                    break; // one creature per cell — first hit
                }
            }

            if (targets.Count == 0)
            {
                MessageLog.Add(actor.GetDisplayName() + "'s whirlwind hits nothing.");
                return;
            }

            // Strike each snapshot target. PerformSingleAttack handles
            // dead defenders (HP ≤ 0 short-circuits without crashing),
            // so a target killed by an earlier strike's dismember-burst
            // — or one already dying when Whirlwind fired — won't crash
            // the loop. Each strike fires the attacker's normal on-hit
            // hooks (Cleave can chain off ANY of the 8 strikes; Dismember
            // can roll independently per strike).
            for (int i = 0; i < targets.Count; i++)
            {
                CombatSystem.PerformSingleAttack(
                    attacker: actor, defender: targets[i],
                    weapon: weapon, isPrimary: true,
                    zone: ctx.Zone, rng: ctx.Rng,
                    attackSourceDesc: "(Whirlwind)");
            }
        }
    }
}
