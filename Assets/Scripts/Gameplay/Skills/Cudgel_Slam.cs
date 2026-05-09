using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cudgel-class active ability: slam an adjacent creature in a
    /// straight line, knocking them back up to <see cref="SLAM_DISTANCE"/>
    /// cells. Cells blocked by solid terrain (wall, creature, closed
    /// door) stop the push and count as a "wall hit." The target takes
    /// a damage roll per wall hit (rolled from the cudgel's
    /// <c>BaseDamage</c>) and is Stunned for a duration that scales with
    /// cells crossed + wall hits.
    ///
    /// <para>Per Qud's <c>Cudgel_Slam</c> mechanic — Qud's version can
    /// also slam THROUGH walls if Strength × 5 ≥ wall AV (destroying the
    /// wall) and chains the slam through other creatures. CoO v1
    /// simplifies: walls AND creatures both stop the push (a "wall hit"
    /// in the simplified model is "any solid"), and the obstacle isn't
    /// damaged. Future v2 can add wall-destruction once the wall-AV
    /// system is ported. Documented as Match (mechanic family) +
    /// Divergent (no chain / no wall-destroy) per CLAUDE.md §4.2.</para>
    ///
    /// <para><b>Mechanic (CoO):</b> requires a Cudgel-attribute weapon
    /// equipped and an adjacent Creature. Iterates 8 directions in
    /// N→NE→E→...→NW order (mirrors
    /// <see cref="SkillCombatHelpers.FindAdjacentCleaveTarget"/>) for
    /// determinism — first creature found becomes target, the iteration
    /// direction becomes slam direction. Per cell pushed:
    /// <c>Cell.IsSolid()</c> check; if solid OR off-map, push stops and
    /// wallHits++; if clear, <c>Zone.MoveEntity</c> succeeds and
    /// cellsPushed++. After pushing: target takes
    /// <c>weaponBaseDamage × wallHits</c> damage rolled via
    /// <see cref="DiceRoller"/>. Apply
    /// <see cref="StunnedEffect"/> for
    /// <c>min(MAX_STUN_DURATION, max(1, cellsPushed + wallHits))</c>
    /// turns. Cooldown <see cref="COOLDOWN"/> turns.</para>
    /// </summary>
    public class Cudgel_Slam : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_Slam);

        public const int COOLDOWN = 50;
        public const int SLAM_DISTANCE = 3;
        public const int MAX_STUN_DURATION = 4;

        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)
        {
            return new ActivatedAbilitySpec
            {
                DisplayName = "Slam",
                Command = "CommandSlam",
                Class = "Skills",
                TargetingMode = AbilityTargetingMode.AdjacentCell,
                Range = 1,
                Cooldown = COOLDOWN,
            };
        }

        public override void OnCommand(SkillEventContext ctx)
        {
            // Determinism: bail on null Rng instead of falling back to a
            // wall-clock-seeded one — mirrors Cudgel_Conk's WSP4.4 fix.
            if (ctx == null || ctx.Attacker == null || ctx.Rng == null) return;
            var actor = ctx.Attacker;

            // Require a Cudgel-class weapon equipped (mirrors Conk).
            var weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "Cudgel");
            if (weapon == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " needs a cudgel-class weapon to slam.");
                EmitSkillRejectedDiag(ctx, "no_weapon");
                return;
            }

            // Slam needs Zone for adjacency lookup + push movement.
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

            // Find adjacent target + remember which direction to slam in.
            // Iterate the same 8-dir order as FindAdjacentCleaveTarget for
            // determinism. The slam direction is the same direction we
            // found the target — pushing them AWAY from the attacker.
            Entity target = null;
            int slamDir = -1;
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
                    slamDir = dir;
                    break;
                }
            }

            if (target == null)
            {
                MessageLog.Add(actor.GetDisplayName() + " has nothing to slam.");
                EmitSkillRejectedDiag(ctx, "no_target");
                return;
            }

            // Push target up to SLAM_DISTANCE cells in slamDir.
            // Stop early if blocked by solid terrain or another creature.
            int cellsPushed = 0;
            int wallHits = 0;
            for (int step = 0; step < SLAM_DISTANCE; step++)
            {
                var targetPos = ctx.Zone.GetEntityPosition(target);
                if (targetPos.x < 0) break; // target left the zone (defense-in-depth)
                var nextCell = ctx.Zone.GetCellInDirection(targetPos.x, targetPos.y, slamDir);
                if (nextCell == null || nextCell.IsSolid() || CellHasOtherCreature(nextCell, target))
                {
                    // Blocked by edge-of-map, by Solid-tagged terrain
                    // (wall/closed door), or by another creature in the
                    // path → counts as a wall hit, push stops. The
                    // creature check is needed because creatures use
                    // PhysicsPart.Solid (not the "Solid" tag) — same
                    // discrepancy that PhysicsPart.cs:69-71 papers over
                    // for normal movement.
                    wallHits++;
                    break;
                }
                if (!ctx.Zone.MoveEntity(target, nextCell.X, nextCell.Y))
                {
                    // MoveEntity refused for some other reason. Treat as
                    // a wall hit (defense-in-depth — IsSolid() may not
                    // catch every move-blocking condition).
                    wallHits++;
                    break;
                }
                cellsPushed++;
            }

            // Bonus damage per wall hit, rolled from weapon BaseDamage.
            // Rolling fresh per wall keeps RNG-seeded tests deterministic
            // and lets a "two-wall slam" actually feel different from a
            // single-wall slam.
            if (wallHits >= 1 && !string.IsNullOrEmpty(weapon.BaseDamage))
            {
                int dmgRoll = 0;
                for (int w = 0; w < wallHits; w++)
                {
                    dmgRoll += DiceRoller.Roll(weapon.BaseDamage, ctx.Rng);
                }
                if (dmgRoll > 0)
                    CombatSystem.ApplyDamage(target, dmgRoll, actor, ctx.Zone);
            }

            // Stun duration: floor 1, ceiling MAX_STUN_DURATION.
            // Mirrors Qud's `Math.Min(4, (num5 == 1) ? 1 : (num5 + 1))`
            // shape — at least 1 turn even on a "no movement" slam,
            // capped at 4 turns even on a chain-of-walls slam.
            int stunDuration = System.Math.Max(1,
                System.Math.Min(MAX_STUN_DURATION, cellsPushed + wallHits));
            target.ApplyEffect(new StunnedEffect(stunDuration), actor, ctx.Zone);

            // Visible message — players need to see the slam happened,
            // and the wall-hit count is what drives the bonus damage so
            // surfacing it makes the mechanic legible.
            string msg = actor.GetDisplayName() + " slams " + target.GetDisplayName();
            if (wallHits >= 1) msg += " into a wall (×" + wallHits + ")";
            else if (cellsPushed >= 1) msg += " back " + cellsPushed + " cell" + (cellsPushed == 1 ? "" : "s");
            else msg += " (immobile)";
            msg += "!";
            MessageLog.Add(msg);
        }

        /// <summary>
        /// Returns true if the cell contains a Creature-tagged entity
        /// other than <paramref name="exclude"/>. Used by the slam push
        /// loop to detect creature obstacles that <see cref="Cell.IsSolid"/>
        /// misses (creatures live with <c>PhysicsPart.Solid = true</c>
        /// but no Tag["Solid"]; the regular movement system checks both
        /// at PhysicsPart.cs:69-71, but Cell.IsSolid only checks the tag).
        /// </summary>
        private static bool CellHasOtherCreature(Cell cell, Entity exclude)
        {
            if (cell == null) return false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var e = cell.Objects[i];
                if (e == null || e == exclude) continue;
                if (e.Tags.ContainsKey("Creature")) return true;
            }
            return false;
        }
    }
}
