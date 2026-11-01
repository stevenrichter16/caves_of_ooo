using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// P5 B3 — the stoop. Canon (FELLING-WORLD-DESIGN §3.4): "Sky-Sari shadow events (an
    /// aerial stoop: a shadow crosses your tile two turns before the hit)"; the bestiary:
    /// "avoidable by tracking sky-cells; punishing for players who don't".
    ///
    /// <para>When a hostile it can see is within <see cref="Range"/> and not adjacent, the
    /// bird rises and marks that cell (<see cref="ShadowTurns"/> turns). While aloft it does
    /// nothing else. Then it drops: if the quarry still stands in the shadow the strike lands
    /// at <see cref="DamagePercent"/>; otherwise the talons close on empty ground. Either way
    /// it lands beside the mark and rests <see cref="CooldownTurns"/> turns before it can rise
    /// again. Fields are public for the reflected save codec.</para>
    ///
    /// <para>Diag (ai): StoopMarked, StoopHit, StoopMissed.</para>
    /// </summary>
    public sealed class StoopPart : Part, ICombatBehavior, IPendingBehavior
    {
        public override string Name => "Stoop";

        public int Range = 8;
        public int ShadowTurns = 2;
        public int DamagePercent = 200;
        public int CooldownTurns = 4;

        public int Countdown;
        public int Cooldown;
        public int MarkX = -1, MarkY = -1;
        public string MarkZone;
        public Entity Quarry;

        public bool IsAloft => Countdown > 0;

        /// <summary>Is this cell under a shadow right now? (The cue layer reads it.)</summary>
        public bool Shadows(Zone zone, int x, int y)
            => IsAloft && zone != null && zone.ZoneID == MarkZone && x == MarkX && y == MarkY;

        public bool TryAct(Entity target, Zone zone, Random rng, bool targetVisible)
        {
            if (IsAloft) return true;
            if (Cooldown > 0) { Cooldown--; return false; }
            var self = ParentEntity;
            if (!targetVisible || target == null || zone == null || self?.SpatialZone != zone) return false;
            int d = SpatialQuery.Distance(zone, self, target);
            if (d <= 1 || d > Range) return false;
            var cell = zone.GetEntityCell(target);
            if (cell == null) return false;
            MarkX = cell.X; MarkY = cell.Y; MarkZone = zone.ZoneID; Quarry = target;
            Countdown = Math.Max(1, ShadowTurns);
            MessageLog.Add(target.HasTag("Player") ? "A shadow crosses you." : "A shadow crosses " + target.GetDisplayName() + ".");
            Record("StoopMarked", target, null);
            ZoneRenderHooks.MarkCellDirty(MarkX, MarkY, "stoop");
            return true;
        }

        public bool AdvancePending(Zone zone, Random rng)
        {
            if (!IsAloft) return false;
            var self = ParentEntity;
            if (self == null || zone == null || zone.ZoneID != MarkZone || self.SpatialZone != zone)
            { Clear(); return false; }
            if (--Countdown > 0) return true;
            Strike(zone, rng);
            return true;
        }

        void Strike(Zone zone, Random rng)
        {
            var self = ParentEntity;
            var victim = Quarry;
            bool there = victim != null && victim.SpatialZone == zone && zone.GetEntityCell(victim) is Cell c
                && c.X == MarkX && c.Y == MarkY && victim.GetStatValue("Hitpoints") > 0;
            if (LandBeside(zone, MarkX, MarkY) && there)
            {
                Record("StoopHit", victim, null);
                MessageLog.Add(self.GetDisplayName() + " drops out of the sky onto " + victim.GetDisplayName() + "!");
                CombatSystem.PerformCommittedMeleeAttack(self, victim, zone, rng ?? new Random(), DamagePercent);
            }
            else
            {
                Record("StoopMissed", victim, there ? "no-landing" : "moved");
                MessageLog.Add(self.GetDisplayName() + " drops out of the sky and its talons close on empty ground.");
            }
            ZoneRenderHooks.MarkCellDirty(MarkX, MarkY, "stoop");
            Clear();
            Cooldown = CooldownTurns;
        }

        // The dive ends beside the mark; nearest open cell to it.
        bool LandBeside(Zone zone, int x, int y)
        {
            var self = ParentEntity;
            var here = zone.GetEntityCell(self);
            if (here != null && Math.Max(Math.Abs(here.X - x), Math.Abs(here.Y - y)) == 1) return true;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var cell = zone.GetCell(x + dx, y + dy);
                    if (cell == null || cell.BlocksMovement()) continue;
                    bool free = true;
                    foreach (var o in cell.Occupants) if (o.HasTag("Creature")) { free = false; break; }
                    if (free && MovementSystem.ForceMoveTo(self, zone, x + dx, y + dy)) return true;
                }
            return false;
        }

        void Clear() { Countdown = 0; MarkX = MarkY = -1; MarkZone = null; Quarry = null; }

        void Record(string kind, Entity target, string reason)
        {
            if (Diag.IsChannelEnabled("ai"))
                Diag.Record("ai", kind, ParentEntity, target, new { x = MarkX, y = MarkY, reason, damagePercent = DamagePercent });
        }
    }
}
