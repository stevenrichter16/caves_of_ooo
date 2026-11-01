using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// P5 B4 — seize and hold. A grappler that strikes an adjacent target and draws blood
    /// may take hold (<see cref="Chance"/>%), putting a <see cref="GrappledEffect"/> on it.
    /// While it holds:
    /// <list type="bullet">
    /// <item>a <b>drag</b> grappler (the Riverwarden) hauls its quarry one step toward the
    /// nearest standing water each turn, and in water the quarry takes
    /// <see cref="DrownDamage"/> a turn on top of the bite — canon: "drags player into
    /// water for damage-over-time (drowning + bite)";</item>
    /// <item>a <b>constrictor</b> (the Canopy Strangler) squeezes for
    /// <see cref="SqueezeDamage"/> each of the quarry's turns and keeps striking.</item>
    /// </list>
    /// Diag (ai): Grappled, Dragged, Drowned.
    /// </summary>
    public sealed class GrapplePart : Part, ICombatBehavior
    {
        public override string Name => "Grapple";

        public int Chance = 50;
        public bool Drag;
        public int SqueezeDamage;
        public int DrownDamage = 4;
        public int WaterRadius = 10;

        public bool TryAct(Entity target, Zone zone, Random rng, bool targetVisible)
        {
            var self = ParentEntity;
            if (target == null || zone == null || self?.SpatialZone != zone || SpatialQuery.Distance(zone, self, target) != 1) return false;
            var held = target.GetEffect<GrappledEffect>();
            if (held != null && held.Broken) return false; // torn free; the grip is gone this turn
            if (held != null && held.GrapplerId == self.ID)
            {
                if (Drag && DragToward(zone, target)) return true;
                if (Drag && InWater(zone, zone.GetEntityCell(target)))
                {
                    Record("Drowned", target, DrownDamage);
                    MessageLog.Add(target.HasTag("Player") ? "You are held under the water." : target.GetDisplayName() + " is held under the water.");
                    CombatSystem.ApplyDamage(target, DrownDamage, self, zone);
                }
                return false; // and strike as usual
            }
            if (held != null || (rng ?? new Random()).Next(100) >= Chance) return false;
            int before = target.GetStatValue("Hitpoints");
            CombatSystem.PerformMeleeAttack(self, target, zone, rng ?? new Random());
            if (target.GetStatValue("Hitpoints") < before && target.GetStatValue("Hitpoints") > 0 && !target.HasEffect<GrappledEffect>())
            {
                target.ApplyEffect(new GrappledEffect(self, SqueezeDamage), self, zone);
                Record("Grappled", target, 0);
            }
            return true;
        }

        // Haul the quarry one step toward the water nearest to it, and follow.
        bool DragToward(Zone zone, Entity target)
        {
            var self = ParentEntity;
            var me = zone.GetEntityCell(self); var them = zone.GetEntityCell(target);
            if (me == null || them == null || InWater(zone, them)) return false;
            if (!NearestWater(zone, them.X, them.Y, out int wx, out int wy, free: true)) return false;
            var next = zone.GetCell(them.X + Math.Sign(wx - them.X), them.Y + Math.Sign(wy - them.Y));
            if (next == null || next.BlocksMovement()) return false;
            int tx = them.X, ty = them.Y, mx = me.X, my = me.Y;
            if (next.X == mx && next.Y == my)
            {
                // The grappler is in the way: it backs toward the water first and pulls
                // the quarry into the cell it left.
                if (!NearestWater(zone, mx, my, out int gx, out int gy)) return false;
                var back = zone.GetCell(mx + Math.Sign(gx - mx), my + Math.Sign(gy - my));
                if (back == null || back.BlocksMovement() || Occupied(back) || !MovementSystem.TryMoveTo(self, zone, back.X, back.Y)) return false;
                if (!MovementSystem.ForceMoveTo(target, zone, mx, my)) return true;
            }
            else
            {
                if (Occupied(next) || !MovementSystem.ForceMoveTo(target, zone, next.X, next.Y)) return false;
                if (SpatialQuery.Distance(zone, self, target) > 1) MovementSystem.TryMoveTo(self, zone, tx, ty);
            }
            Record("Dragged", target, 0);
            MessageLog.Add(self.GetDisplayName() + (target.HasTag("Player") ? " drags you toward the water!" : " drags " + target.GetDisplayName() + " toward the water!"));
            return true;
        }

        // free: skip water already occupied (the grappler lying in it is not a place to drag to).
        bool NearestWater(Zone zone, int x, int y, out int wx, out int wy, bool free = false)
        {
            for (int r = 0; r <= WaterRadius; r++)
                for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    var c = zone.GetCell(x + dx, y + dy);
                    if (c != null && InWater(zone, c) && !(free && Occupied(c))) { wx = c.X; wy = c.Y; return true; }
                }
            wx = wy = -1; return false;
        }

        static bool InWater(Zone zone, Cell cell)
        {
            if (cell == null) return false;
            foreach (var o in cell.Objects)
                if (o.GetPart<LiquidPoolPart>() is LiquidPoolPart pool && pool.Volume > 0) return true; // water, mire, spray: all drown
            return false;
        }

        static bool Occupied(Cell cell)
        {
            foreach (var o in cell.Objects) if (o.HasTag("Creature")) return true;
            return false;
        }

        void Record(string kind, Entity target, int amount)
        {
            if (Diag.IsChannelEnabled("ai")) Diag.Record("ai", kind, ParentEntity, target, new { drag = Drag, amount });
        }
    }
}
