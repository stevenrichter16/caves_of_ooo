using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Acrobatics active ability: leap 2 cells in a chosen direction,
    /// SKIPPING a single solid cell at distance 1 if the cell at
    /// distance 2 is open. Useful for crossing pits, walls, or doors
    /// the actor can't normally walk through. Distinct from Disengage
    /// (open cells only) and Tumble (cell swap with creature) —
    /// Vault is the only ability that BYPASSES A WALL CELL.
    ///
    /// <para><b>Mechanic:</b> SelfCentered with direction (DirectionLine
    /// targeting). Reads ctx.DirectionX/Y. Looks at the cell at distance
    /// 2 — if it's open + non-creature, the actor moves there
    /// regardless of what's at distance 1 (open or wall, they're
    /// jumped over). If the cell at distance 2 is solid or occupied,
    /// the leap fails and the cooldown is consumed.</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Acrobatics_Vault</c>):
    /// "the only ability that bypasses a wall/solid cell." Disengage
    /// moves through open cells; Vault crosses blockers.</para>
    /// </summary>
    public class Acrobatics_Vault : BaseSkillPart
    {
        public override string Name => nameof(Acrobatics_Vault);

        public const int COOLDOWN = 15;
        public const int VAULT_DISTANCE = 2;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Vault",
                Command = "CommandVault",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = VAULT_DISTANCE,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return; }

            int landX = actorPos.x + dx * VAULT_DISTANCE;
            int landY = actorPos.y + dy * VAULT_DISTANCE;
            if (!ctx.Zone.InBounds(landX, landY))
            {
                EmitSkillRejectedDiag(ctx, "out_of_bounds");
                MessageLog.Add(actor.GetDisplayName() + "'s vault would land off the map.");
                return;
            }
            var landCell = ctx.Zone.GetCell(landX, landY);
            if (landCell == null || landCell.IsSolid())
            {
                EmitSkillRejectedDiag(ctx, "landing_blocked");
                MessageLog.Add(actor.GetDisplayName() + "'s vault has no clear landing.");
                return;
            }
            // Creature at landing cell? Vault doesn't displace.
            for (int i = 0; i < landCell.Objects.Count; i++)
            {
                var e = landCell.Objects[i];
                if (e == null || e == actor) continue;
                if (e.Tags.ContainsKey("Creature"))
                {
                    EmitSkillRejectedDiag(ctx, "landing_occupied");
                    MessageLog.Add(actor.GetDisplayName() + "'s vault is blocked by " + e.GetDisplayName() + ".");
                    return;
                }
            }

            // Move actor directly to landing — skipping whatever was at
            // distance 1 (which is the whole point of Vault).
            ctx.Zone.MoveEntity(actor, landX, landY);
            MessageLog.Add(actor.GetDisplayName() + " vaults forward!");
        }
    }
}
