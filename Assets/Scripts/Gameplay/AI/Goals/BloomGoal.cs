namespace CavesOfOoo.Core
{
    /// <summary>
    /// W4.4 — the Bloom's compulsion, as a goal. Pushed each turn by
    /// <see cref="BloomedEffect.OnTurnStart"/> (HasGoal-guarded) and
    /// popped by its OnRemove; belt-and-braces, <see cref="Finished"/>
    /// reports done the moment the effect is gone, so a cured bearer
    /// sheds the goal on the next cleanup pass even if the pop misses.
    ///
    /// <para>Priorities, in order: (1) whatever stands ADJACENT gets
    /// attacked — faction is not consulted; the Bloom does not know the
    /// bearer's friends (target SELECTION is where IsHostile lives;
    /// KillGoal-style EXECUTION has no faction gate, verified at
    /// CombatSystem.PerformMeleeAttack). (2) Company within
    /// <see cref="NoticeRadius"/> — step away from the nearest.
    /// (3) Alone — drift toward open ground and stand in it.</para>
    /// </summary>
    public class BloomGoal : GoalHandler
    {
        /// <summary>Creatures inside this Chebyshev radius count as
        /// "the others" the bearer is driven away from.</summary>
        public const int NoticeRadius = 5;

        public override bool Finished()
        {
            return ParentEntity == null
                || !ParentEntity.HasEffect<BloomedEffect>();
        }

        public override string GetDetails() => "worn by the Bloom";

        public override void TakeAction()
        {
            var zone = CurrentZone;
            var me = ParentEntity;
            if (zone == null || me == null) return;
            var pos = zone.GetEntityPosition(me);
            if (pos.x < 0) return;

            // 1. Whatever stands adjacent gets attacked.
            var adjacent = NearestCreature(zone, me, pos.x, pos.y, 1);
            if (adjacent != null)
            {
                // The victim defends itself: CombatSystem's damage-landed
                // retaliation hook is v1-scoped to PLAYER-sourced damage
                // (CombatSystem.cs:1066-1070, "NPC-on-NPC incidental
                // damage is deliberately not covered yet"), so without
                // this a Bloomed guard would beat its own ward and the
                // ward would stand there. The Bloom provokes explicitly,
                // pre-swing (InputHandler's melee precedent — even a
                // miss provokes). PersonalEnemies is permanent: the scar
                // outlives the cure, by design.
                adjacent.GetPart<BrainPart>()?.SetPersonallyHostile(me);
                Think("the Bloom swings");
                CombatSystem.PerformMeleeAttack(me, adjacent, zone, Rng);
                return;
            }

            // 2. Company in notice range — leave it.
            var near = NearestCreature(zone, me, pos.x, pos.y, NoticeRadius);
            if (near != null)
            {
                var theirs = zone.GetEntityPosition(near);
                Think("away from the others");
                AIHelpers.TryStepAway(me, zone, pos.x, pos.y, theirs.x, theirs.y);
                return;
            }

            // 3. Alone — drift toward open ground and stand in it. "Open"
            // = a floor cell none of whose 8 neighbors block movement.
            var open = AIHelpers.FindNearestCellWhere(zone, pos.x, pos.y,
                c => IsOpenGround(zone, c), maxRadius: 12);
            if (open.HasValue && (open.Value.x != pos.x || open.Value.y != pos.y))
            {
                Think("toward the open ground");
                AIHelpers.TryApproachWithPathfinding(me, zone,
                    pos.x, pos.y, open.Value.x, open.Value.y);
            }
            // Already standing in it: the Bloom is satisfied. Sway.
        }

        private static Entity NearestCreature(Zone zone, Entity self, int x, int y, int radius)
        {
            Entity best = null;
            int bestDist = int.MaxValue;
            foreach (var e in zone.GetAllEntities())
            {
                if (e == self || !e.Tags.ContainsKey("Creature")) continue;
                if (e.GetStatValue("Hitpoints", 0) <= 0) continue;
                var p = zone.GetEntityPosition(e);
                if (p.x < 0) continue;
                int d = System.Math.Max(System.Math.Abs(p.x - x), System.Math.Abs(p.y - y));
                if (d <= radius && d < bestDist) { bestDist = d; best = e; }
            }
            return best;
        }

        private static bool IsOpenGround(Zone zone, Cell cell)
        {
            if (cell == null || !cell.IsPassable()) return false;
            for (int ox = -1; ox <= 1; ox++)
                for (int oy = -1; oy <= 1; oy++)
                {
                    if (ox == 0 && oy == 0) continue;
                    var n = zone.GetCell(cell.X + ox, cell.Y + oy);
                    if (n == null || !n.IsPassable()) return false;
                }
            return true;
        }
    }
}
