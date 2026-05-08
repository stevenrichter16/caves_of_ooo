using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// ShortBlades active ability: pure mobility — move
    /// <see cref="DISENGAGE_DISTANCE"/> cells in a chosen direction
    /// without attacking. Distinct from Tumble (cell swap), Vault
    /// (over obstacle), and ChargingStrike (move-then-strike) —
    /// Disengage is the only ability that grants STRAIGHT-LINE
    /// MOBILITY in open cells.
    ///
    /// <para><b>Mechanic:</b> requires a Piercing-class weapon
    /// equipped (the brainstorm gates Disengage on the same gate as
    /// Shank/Flurry/Backstab so the entire ShortBlades active suite
    /// shares a "wield it or you can't" criterion). Reads direction
    /// from ctx, walks one cell at a time up to DISENGAGE_DISTANCE.
    /// Stops on first solid or first creature. The actor doesn't
    /// attack — Disengage is intentionally non-combat (the brainstorm
    /// notes "when OpAttacks ship, this becomes the canonical bypass").</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §ShortBlades_Disengage</c>):
    /// "the only ability that grants pure mobility WITHOUT attacking."</para>
    /// </summary>
    public class ShortBlades_Disengage : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Disengage);

        public const int COOLDOWN = 25;
        public const int DISENGAGE_DISTANCE = 3;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Disengage",
                Command = "CommandDisengage",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = DISENGAGE_DISTANCE,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;

            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Piercing");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs a piercing-class weapon to disengage.");
                EmitSkillRejectedDiag(ctx, "no_weapon");
                return;
            }
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return; }

            int x = actorPos.x, y = actorPos.y;
            int cellsMoved = 0;
            for (int step = 0; step < DISENGAGE_DISTANCE; step++)
            {
                int nx = x + dx, ny = y + dy;
                if (!ctx.Zone.InBounds(nx, ny)) break;
                var cell = ctx.Zone.GetCell(nx, ny);
                if (cell == null || cell.IsSolid()) break;

                // Creature in destination? Disengage stops there (no
                // attack, no swap — it's a pure walk).
                bool creatureBlocks = false;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == actor) continue;
                    if (e.Tags.ContainsKey("Creature")) { creatureBlocks = true; break; }
                }
                if (creatureBlocks) break;

                ctx.Zone.MoveEntity(actor, nx, ny);
                x = nx; y = ny;
                cellsMoved++;
            }

            MessageLog.Add(actor.GetDisplayName() + " disengages " + cellsMoved + " cell"
                + (cellsMoved == 1 ? "" : "s") + ".");
        }
    }
}
