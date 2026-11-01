using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// P5 B5 — hunting as a pack. When a pack-mate is already beside the quarry, this
    /// one does not queue behind it: it circles to the free side farthest from its mates
    /// (the flank). A strike on a quarry that already has a pack-mate beside it gains
    /// <see cref="FlankBonus"/> to hit (<see cref="FlankHitBonus"/>, read by the hit roll).
    /// The pack is every creature of the same faction carrying this part. Calling the
    /// pack on first sight is the existing <c>CombatTacticsPart.AssistAllies</c>.
    /// Diag (ai): Flanked, Circled.
    /// </summary>
    public sealed class PackHunterPart : Part, ICombatBehavior
    {
        public override string Name => "PackHunter";

        public int FlankBonus = 2;

        public bool TryAct(Entity target, Zone zone, Random rng, bool targetVisible)
        {
            var self = ParentEntity;
            if (!targetVisible || target == null || zone == null || self?.SpatialZone != zone) return false;
            if (SpatialQuery.Distance(zone, self, target) <= 1) return false;
            var mate = MateBeside(self, target, zone);
            if (mate == null) return false;
            var t = zone.GetEntityCell(target); var m = zone.GetEntityCell(mate); var me = zone.GetEntityCell(self);
            if (t == null || m == null || me == null) return false;
            int bestX = -1, bestY = -1, best = -1;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var c = zone.GetCell(t.X + dx, t.Y + dy);
                    if (c == null || c.BlocksMovement() || HasCreature(c)) continue;
                    int away = Math.Max(Math.Abs(c.X - m.X), Math.Abs(c.Y - m.Y));
                    if (away > best) { best = away; bestX = c.X; bestY = c.Y; }
                }
            if (best < 2) return false;
            if (!AIHelpers.TryApproachWithPathfinding(self, zone, me.X, me.Y, bestX, bestY)) return false;
            if (Diag.IsChannelEnabled("ai")) Diag.Record("ai", "Circled", self, target, new { x = bestX, y = bestY });
            return true;
        }

        /// <summary>The to-hit bonus for <paramref name="attacker"/> striking
        /// <paramref name="defender"/>: <see cref="FlankBonus"/> if a pack-mate is beside the
        /// defender, else 0. Zero cost for creatures without the part.</summary>
        public static int FlankHitBonus(Entity attacker, Entity defender, Zone zone)
        {
            var pack = attacker?.GetPart<PackHunterPart>();
            if (pack == null || defender == null || zone == null) return 0;
            if (MateBeside(attacker, defender, zone) == null) return 0;
            if (Diag.IsChannelEnabled("ai")) Diag.Record("ai", "Flanked", attacker, defender, new { bonus = pack.FlankBonus });
            return pack.FlankBonus;
        }

        static Entity MateBeside(Entity self, Entity target, Zone zone)
        {
            string faction = FactionManager.GetFaction(self);
            var t = zone.GetEntityCell(target);
            if (t == null) return null;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var c = zone.GetCell(t.X + dx, t.Y + dy);
                    if (c == null) continue;
                    foreach (var o in c.Objects)
                        if (o != self && o != target && o.GetPart<PackHunterPart>() != null && FactionManager.GetFaction(o) == faction
                            && o.GetStatValue("Hitpoints") > 0) return o;
                }
            return null;
        }

        static bool HasCreature(Cell c)
        {
            foreach (var o in c.Objects) if (o.HasTag("Creature")) return true;
            return false;
        }
    }
}
