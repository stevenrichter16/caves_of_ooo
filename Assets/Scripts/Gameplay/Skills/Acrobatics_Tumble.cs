using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Acrobatics active ability: swap positions with an adjacent
    /// creature. Works on enemies AND allies. On hostile targets, applies
    /// <see cref="ConfusedEffect"/> for <see cref="CONFUSED_DURATION"/>
    /// turns (the disorientation of being shoved past in a single
    /// motion). On allies, the swap is a clean position-trade with no
    /// debuff — useful for combat repositioning ("trade places with the
    /// scribe so he can reach the well").
    ///
    /// <para><b>Mechanic (CoO):</b> no weapon class required — Acrobatics
    /// is martial-arts-y by design (Dodge already shipped with no weapon
    /// gate). The chosen adjacent physical cell identifies the target. The swap
    /// validates and exchanges both complete bodies atomically, then runs
    /// each participant's landing events through <see cref="MovementSystem.TrySwap"/>.</para>
    ///
    /// <para>Only actual hostility applies confusion. FactionManager
    /// includes party alignment, personal grudges and temporary truces;
    /// a neutral creature or recruited companion trades places cleanly.</para>
    ///
    /// <para>Per the WSP8.2 active-ability brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md</c> §Acrobatics_Tumble): "the
    /// only ability that EXCHANGES cells with another creature." Vault
    /// (proposed) crosses obstacles; Disengage (proposed) moves through
    /// open cells; Tumble swaps.</para>
    ///
    /// <para>Classification: <b>Match (mechanic family)</b> per
    /// CLAUDE.md §4.2 — Qud has <c>Acrobatics_Tumble</c> with similar
    /// swap-and-confuse semantics. Magnitude (1-turn confusion, 20T
    /// cooldown) per the CoO brainstorm tuning.</para>
    /// </summary>
    public class Acrobatics_Tumble : BaseSkillPart
    {
        public override string Name => nameof(Acrobatics_Tumble);

        public const int COOLDOWN = 20;
        public const int CONFUSED_DURATION = 1;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Tumble",
                Command = "CommandTumble",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override bool OnCommand(SkillEventContext ctx)
        {
            // Tumble doesn't roll dice for the swap (it's deterministic
            // movement) — but ConfusedEffect's CanApply path doesn't need
            // an Rng either, and the determinism rule is universal: a
            // null Rng signals a misconfigured caller. Bail like the
            // other actives.
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return false;
            var actor = ctx.Attacker;

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

            var target = SkillCombatHelpers.FindAdjacentSkillTarget(actor, ctx.Zone, ctx.TargetCell, out _);

            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " has no one to tumble with.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return false;
            }

            var targetPos = ctx.Zone.GetEntityPosition(target);
            if (targetPos.x < 0)
            {
                EmitSkillRejectedDiag(ctx, "target_not_in_zone");
                return false;
            }

            if (TouchesClosedArchive(ctx.Zone, actor, targetPos.x, targetPos.y)
                || TouchesClosedArchive(ctx.Zone, target, actorPos.x, actorPos.y))
            {
                EmitSkillRejectedDiag(ctx, "sealed_barrier");
                return false;
            }

            // Permission query only: the occupied swap destination is validated
            // atomically by TrySwap, rather than ordinary walking collision.
            var permission = GameEvent.New("BeforeMove");
            permission.SetParameter("Actor", (object)actor);
            permission.SetParameter("SourceCell", (object)ctx.Zone.GetEntityCell(actor));
            permission.SetParameter("DX", targetPos.x - actorPos.x);
            permission.SetParameter("DY", targetPos.y - actorPos.y);
            bool canMove;
            try { canMove = actor.FireEvent(permission); }
            finally { permission.Release(); }
            if (!canMove) { EmitSkillRejectedDiag(ctx, "movement_blocked"); return false; }

            if(!MovementSystem.TrySwap(actor,target,ctx.Zone))
            {
                EmitSkillRejectedDiag(ctx,"body_swap_blocked");
                return false;
            }

            // Use the same actual relationship as combat, including companions.
            if (FactionManager.IsHostile(target, actor) && ctx.Zone.GetEntityCell(target) != null
                && target.GetStatValue("Hitpoints", 1) > 0)
            {
                target.ApplyEffect(new ConfusedEffect(CONFUSED_DURATION),
                    actor, ctx.Zone);
            }

            MessageLog.Add(actor.GetDisplayName() + " tumbles past "
                + target.GetDisplayName() + ".");
        
            return true;
        }

        private static bool TouchesClosedArchive(Zone zone, Entity owner, int x, int y)
        {
            foreach (var cell in zone.GetOccupiedCells(owner, x, y))
                if (cell?.HasClosedArchiveBarrier() == true) return true;
            return false;
        }
    }
}
