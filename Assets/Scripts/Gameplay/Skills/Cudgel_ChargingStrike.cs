using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cudgel-class active ability: charge up to
    /// <see cref="CHARGE_DISTANCE"/> cells in a chosen direction, then
    /// strike the first creature in the path with
    /// <see cref="CHARGE_DAMAGE_BONUS_PERCENT"/>% bonus damage on top
    /// of the swing's natural damage. Distinct from Slam (single-
    /// adjacent push), Conk (single-adjacent stun) — ChargingStrike
    /// is the only ability that MOVES THE ACTOR before attacking.
    ///
    /// <para><b>Mechanic:</b> requires a Cudgel-class weapon equipped.
    /// Reads ctx.DirectionX/DirectionY, walks open cells one at a
    /// time up to CHARGE_DISTANCE cells. If a creature is encountered
    /// (in path or at endpoint), the actor stops one cell SHORT of
    /// the creature and swings into them with bonus damage. If only
    /// open cells were traversed (no creature found), the actor ends
    /// up at the max-distance cell and the cooldown is consumed
    /// without a strike. If a wall blocks the path, the actor stops
    /// at the last open cell and the cooldown is consumed.</para>
    ///
    /// <para>Per the WSP8.2 brainstorm
    /// (<c>Docs/SKILL-ACTIVES-BRAINSTORM.md §Cudgel_ChargingStrike</c>):
    /// mirrors Qud's <c>Cudgel_ChargingStrike</c>. The "charge then
    /// swing" is distinct from Slam (push at distance 1) and Lunge
    /// (extend reach without moving).</para>
    /// </summary>
    public class Cudgel_ChargingStrike : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_ChargingStrike);

        public const int COOLDOWN = 30;
        public const int CHARGE_DISTANCE = 3;
        public const int CHARGE_DAMAGE_BONUS_PERCENT = 50;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Charging Strike",
                Command = "CommandChargingStrike",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.DirectionLine,
                Range = CHARGE_DISTANCE,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;

            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Cudgel");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs a cudgel-class weapon to charge.");
                EmitSkillRejectedDiag(ctx, "no_weapon");
                return;
            }
            if (ctx.Zone == null) { EmitSkillRejectedDiag(ctx, "no_zone"); return; }

            int dx = ctx.DirectionX, dy = ctx.DirectionY;
            if (dx == 0 && dy == 0)
            {
                EmitSkillRejectedDiag(ctx, "no_direction");
                return;
            }

            var actorPos = ctx.Zone.GetEntityPosition(actor);
            if (actorPos.x < 0)
            {
                EmitSkillRejectedDiag(ctx, "actor_not_in_zone");
                return;
            }

            // Walk up to CHARGE_DISTANCE cells. Stop on first creature
            // encountered (one cell SHORT of them, so we can swing).
            // Stop on first solid wall (one cell short, so we don't
            // walk into it). Otherwise advance.
            int x = actorPos.x;
            int y = actorPos.y;
            Entity hitTarget = null;
            for (int step = 0; step < CHARGE_DISTANCE; step++)
            {
                int nx = x + dx, ny = y + dy;
                if (!ctx.Zone.InBounds(nx, ny)) break;
                var cell = ctx.Zone.GetCell(nx, ny);
                if (cell == null) break;

                // Creature in path? Stop one short, set as target.
                Entity creature = null;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == actor) continue;
                    if (e.Tags.ContainsKey("Creature")) { creature = e; break; }
                }
                if (creature != null) { hitTarget = creature; break; }

                if (cell.IsSolid()) break; // wall — actor stops here

                // Open cell — advance.
                ctx.Zone.MoveEntity(actor, nx, ny);
                x = nx; y = ny;
            }

            if (hitTarget == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " charges into empty space.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            // Swing with bonus damage. Snapshot HP, fire normal attack,
            // measure damage dealt, apply bonus = +50% of dealt damage.
            int hpBefore = hitTarget.GetStatValue("Hitpoints");
            CombatSystem.PerformSingleAttack(
                attacker: actor, defender: hitTarget,
                weapon: weapon, isPrimary: true,
                zone: ctx.Zone, rng: ctx.Rng,
                attackSourceDesc: "(ChargingStrike)");

            if (hitTarget.GetStatValue("Hitpoints") > 0)
            {
                int dmg = hpBefore - hitTarget.GetStatValue("Hitpoints");
                int bonus = (dmg * CHARGE_DAMAGE_BONUS_PERCENT) / 100;
                if (bonus >= 1)
                {
                    CombatSystem.ApplyDamage(hitTarget, bonus, actor, ctx.Zone);
                    MessageLog.Add(actor.GetDisplayName() + "'s charge adds +" + bonus + " momentum damage!");
                }
            }
        }
    }
}
