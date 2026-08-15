using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Conjure Water — a short conjured stream that becomes a puddle.
    /// Port of <c>ConjureWaterMutation</c>; taught by
    /// ConjureWaterGrimoire, buyable in Hydromancy.
    ///
    /// <para>Verbatim: directional, range 2, cooldown 4. The stream walks
    /// the line and lands preferentially on a cell holding something
    /// BURNING (fire-brigade semantics); otherwise the furthest passable
    /// cell. A WaterPuddle spawns there, everything in the cell is soaked
    /// (Wet 0.5), and burning occupants get their reactions re-evaluated
    /// so the douse actually extinguishes.</para>
    ///
    /// <para>Refusals are free: no direction, no landable cell, or a
    /// missing WaterPuddle blueprint mean no cooldown and no turn. (The
    /// mutation read its direction with <c>GetParameter&lt;int&gt;</c>,
    /// which never sees the int-dictionary the input path writes — from a
    /// keybind the spell always refused. The ctx fields come through the
    /// dispatcher's proper lift, so the port fixes that bug by
    /// construction — audit F17.)</para>
    /// </summary>
    public class Hydromancy_ConjureWater : BaseSkillPart
    {
        public override string Name => nameof(Hydromancy_ConjureWater);

        public const int RANGE = 2;
        public const int COOLDOWN = 4;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Conjure Water",
                Command = "CommandConjureWater",
                Class = "Hydromancy",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = RANGE,
                Cooldown = COOLDOWN,
            };
        }

        public override bool OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }
            var zone = ctx.Zone;

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return false; }

            var actorPos = zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return false; }

            int range = ctx.Range > 0 ? ctx.Range : RANGE;

            // Walk the line: a cell holding something burning wins
            // immediately; otherwise the furthest passable cell.
            Cell targetCell = null;
            for (int step = 1; step <= range; step++)
            {
                int tx = actorPos.x + dx * step;
                int ty = actorPos.y + dy * step;
                if (!zone.InBounds(tx, ty))
                    break;
                Cell candidate = zone.GetCell(tx, ty);
                if (candidate == null)
                    break;

                bool holdsBurning = false;
                for (int i = 0; i < candidate.Objects.Count; i++)
                {
                    if (candidate.Objects[i].HasEffect<BurningEffect>())
                    {
                        holdsBurning = true;
                        break;
                    }
                }
                if (holdsBurning)
                {
                    targetCell = candidate;
                    break;
                }

                if (!candidate.IsPassable())
                    break;
                targetCell = candidate;
            }

            if (targetCell == null) { EmitSkillRejectedDiag(ctx, "no_landable_cell"); return false; }

            var factory = MaterialReactionResolver.Factory;
            Entity puddle = factory?.CreateEntity("WaterPuddle");
            if (puddle == null)
            {
                UnityEngine.Debug.LogError("[ConjureWater] Factory returned null for WaterPuddle blueprint.");
                EmitSkillRejectedDiag(ctx, "no_puddle_blueprint");
                return false;
            }
            zone.AddEntity(puddle, targetCell.X, targetCell.Y);

            // Soak the cell: everything gets Wet, and burning occupants
            // get their reactions re-run so the douse extinguishes.
            for (int i = targetCell.Objects.Count - 1; i >= 0; i--)
            {
                Entity entity = targetCell.Objects[i];
                if (entity == puddle)
                    continue;
                entity.ApplyEffect(new WetEffect(0.5f), actor, zone);
                if (entity.HasEffect<BurningEffect>())
                    MaterialReactionResolver.EvaluateReactions(entity, zone, entity.GetEffect<BurningEffect>());
            }

            return true;
        }
    }
}
