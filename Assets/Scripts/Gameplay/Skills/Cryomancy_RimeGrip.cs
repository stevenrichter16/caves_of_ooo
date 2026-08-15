using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cryomancy active: seizes one target at range and locks it in ice.
    ///
    /// <para>SPELLCRAFT SM6 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §6.4) —
    /// the cold branch of the same rule that makes soaked targets
    /// conduct. A wet target freezes deeper, because
    /// <see cref="FrozenEffect"/> now amplifies on moisture exactly as
    /// <see cref="ElectrifiedEffect"/> does. The power gets that for
    /// free by applying the effect normally.</para>
    ///
    /// <para>Single target on purpose: <see cref="FrozenEffect"/> blocks
    /// ALL action while present, so a version that froze a whole line
    /// would end fights outright rather than shape them.</para>
    /// </summary>
    public class Cryomancy_RimeGrip : BaseSkillPart
    {
        public override string Name => nameof(Cryomancy_RimeGrip);

        public const int COOLDOWN = 35;
        public const int GRIP_RANGE = 5;
        public const int GRIP_DAMAGE = 4;

        /// <summary>Base freeze. Modest, because a wet target multiplies
        /// it and because any positive Cold already blocks action.</summary>
        public const float GRIP_COLD = 0.5f;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Rime Grip",
                Command = "CommandRimeGrip",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = GRIP_RANGE,
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

            bool blockedByWall;
            List<Entity> line = SkillLine.Collect(
                ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, GRIP_RANGE,
                out blockedByWall);

            if (line.Count == 0)
            {
                // COLD-TILE BRIDGE (Docs/COLD-TILE-BRIDGE.md): no entity to
                // grip — but the ground can still be gripped. Jet Blast's
                // water is a TILE coating, not an entity, so this branch
                // used to refuse ("closes on nothing") over a visibly wet
                // tile. Mirror Jet Blast's own rule ("a miss is not a
                // nothing"): if any cell in the line holds WATER, cold it,
                // resolve (freeze_water: water + cold → ice), and consume
                // the cast. A truly dry line stays a FREE refusal — Rime
                // Grip freezes water; it does not freeze bare rock, and it
                // does not chill dry ground on the way (cold is written
                // only where water was, so a whiff leaves no residue).
                var cells = SkillLine.CollectCells(
                    ctx.Zone, actor, actorPos.x, actorPos.y, dx, dy, GRIP_RANGE);
                var wet = new List<Point>();
                for (int i = 0; i < cells.Count; i++)
                    if (ctx.Zone.TileState.HasCoating(cells[i].X, cells[i].Y, "water"))
                        wet.Add(cells[i]);
                if (wet.Count > 0)
                {
                    ZoneTileStateSystem.ApplyColdToTiles(ctx.Zone, wet, actor, Name);
                    MessageLog.Add(actor.GetDisplayName() + "'s rime grip freezes the ground.");
                    return true; // the water froze — a real cast
                }

                EmitSkillRejectedDiag(ctx, blockedByWall ? "line_blocked" : "no_target");
                MessageLog.Add(actor.GetDisplayName() + "'s rime grip closes on nothing.");
                return false;
            }

            var target = line[0];   // nearest — the grip takes ONE
            // Capture the ground BEFORE damage: a shattered target may be
            // gone from the zone by the time we ice its cell (live find:
            // Rime Grip at a chest on water shattered the chest and the
            // water stayed water, because the ground pass ran after an
            // early return on a position that no longer resolved).
            var groundPos = ctx.Zone.GetEntityPosition(target);

            var dmg = new Damage(GRIP_DAMAGE);
            dmg.AddAttribute("Cold");
            // RouteDamage, not ApplyDamage: scenery keeps its hitpoints on a
                // DestructiblePart, and ApplyDamage deliberately early-returns
                // on anything with no Hitpoints stat — so elemental damage aimed
                // at a tree or a barrel was silently discarded.
                DestructionSystem.RouteDamage(target, dmg, actor, ctx.Zone);

            if (target.GetStatValue("Hitpoints") <= 0)
            {
                MessageLog.Add(actor.GetDisplayName() + "'s rime grip shatters "
                    + target.GetDisplayName() + "!");
                IceTheGround(ctx, actor, groundPos);
                return true; // shattered the target — a real cast
            }

            target.ApplyEffect(new FrozenEffect(GRIP_COLD), actor, ctx.Zone);
            MessageLog.Add(actor.GetDisplayName() + "'s rime grip seizes "
                + target.GetDisplayName() + "!");
            IceTheGround(ctx, actor, groundPos);
            return true;
        }

        /// <summary>The grip seizes the thing AND ices the ground under it
        /// — on EVERY hit branch, shatter included. If that ground is
        /// water, freeze_water turns it to ice and lands its own Frozen on
        /// whoever stands there: a wet target freezes deeper, which is Rime
        /// Grip's stated identity (SM6). Docs/COLD-TILE-BRIDGE.md.</summary>
        private void IceTheGround(SkillEventContext ctx, Entity actor, (int x, int y) at)
        {
            if (at.x < 0) return;
            ZoneTileStateSystem.ApplyColdToTile(ctx.Zone, at.x, at.y, actor, Name);
            ZoneTileStateSystem.ResolveAfterAbility(ctx.Zone, actor);
        }
    }
}
