using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// PALIMPSEST P2 — lays fuel.
    ///
    /// <para><b>Why this skill exists.</b> The spec-vs-shipped sweep
    /// found `oil` already fully defined as a liquid (Combustibility 90,
    /// FlameTemperature 250, slippery) with `OilSlick` and `OilSeep`
    /// blueprints — and <b>nothing in the game that writes it</b>. A
    /// coating no ability can apply may as well not exist. Oilmark is
    /// the smallest thing that makes the most flammable substance in the
    /// game reachable by a player.</para>
    ///
    /// <para><b>Why Pyromancy and not Hydromancy.</b> Oil's only
    /// interesting property in this system is that it burns, so the tree
    /// that burns things should be the one that lays the fuel. It gives
    /// Pyromancy a genuine in-tree combo — Oilmark, then Ember Spit —
    /// which is different from Galvanism's cross-tree soak-then-shock
    /// and does not undermine any conditional bonus the way a
    /// self-supplied Wet would.</para>
    ///
    /// <para><b>It deals no damage at all.</b> Oil is setup. The damage
    /// belongs to whatever ignites it, which is the whole grammar this
    /// prototype is testing.</para>
    /// </summary>
    public class Pyromancy_Oilmark : BaseSkillPart
    {
        public override string Name => nameof(Pyromancy_Oilmark);

        public const int COOLDOWN = 18;
        public const int OILMARK_RANGE = 4;

        /// <summary>Reuses the shipped `oil` LiquidDefinition id.</summary>
        public const string OilLiquid = "oil";

        /// <summary>Longer than water's 6: oil does not evaporate, and
        /// the design gives it 8 turns on terrain.</summary>
        public const int OilTurns = 8;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Oilmark",
                Command = "CommandOilmark",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = OILMARK_RANGE,
                Cooldown = COOLDOWN,
            };
        }

        public override bool OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null) return false;
            var actor = ctx.Attacker;
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return false; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0) { EmitSkillRejectedDiag(ctx, "no_direction"); return false; }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0) { EmitSkillRejectedDiag(ctx, "actor_not_in_zone"); return false; }

            List<Point> cells = SkillLine.CollectCells(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, OILMARK_RANGE);

            if (cells.Count == 0)
            {
                // Walled in immediately — nowhere to pour.
                EmitSkillRejectedDiag(ctx, "line_blocked");
                MessageLog.Add(actor.GetDisplayName() + " has nowhere to pour.");
                return false;
            }

            for (int i = 0; i < cells.Count; i++)
                ZoneTileStateSystem.WriteCoating(ctx.Zone, cells[i].X, cells[i].Y,
                    OilLiquid, OilTurns, actor, Name);

            // Anything standing in the slick is coated too — the ground
            // and the creature on it should not disagree.
            bool blockedByWall;
            var caught = SkillLine.Collect(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, OILMARK_RANGE,
                out blockedByWall);
            for (int i = 0; i < caught.Count; i++)
                caught[i].ApplyEffect(new LiquidCoveredEffect(OilLiquid, 1), actor, ctx.Zone);

            // P3 — resolve what the writes just created, now rather than
            // at end of turn: lightning into a puddle must electrify it
            // on the cast, or the rule reads as a bug.
            ZoneTileStateSystem.ResolveAfterAbility(ctx.Zone, actor);

            MessageLog.Add(actor.GetDisplayName() + " lays a slick of oil across "
                + cells.Count + " pace" + (cells.Count == 1 ? "" : "s") + ".");
        
            return true;
        }
    }
}
