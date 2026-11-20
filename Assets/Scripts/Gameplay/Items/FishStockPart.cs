using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The fish a pond holds (Docs/MACHINE-INVENTED-USES.md 3n), kept on the pond's anchor pool owner and shared by every trap on that water. A pond starts full at a capacity set by where it is
    /// (<see cref="MinCapacity"/> to <see cref="MaxCapacity"/>, stable for a given zone and cell) and refills one fish every <see cref="RegenTicks"/>, up to its capacity. Taking fish is the only way it
    /// falls. It is created the first time a trap reconciles beside the water, full, because nothing could have been taken before then. Time is reconciled when a trap is read or emptied, never by a
    /// timer on a zone that is not loaded.
    /// </summary>
    public sealed class FishStockPart : Part
    {
        public override string Name => "FishStock";
        public const int MinCapacity = 4, MaxCapacity = 8, RegenTicks = WorldClock.DayLengthTicks;

        public int Capacity, Available, LastTick;

        /// <summary>The stock on an anchor, created full on first use. Null when there is no anchor.</summary>
        public static FishStockPart For(Zone zone, Entity anchor, int now)
        {
            if (zone == null || anchor == null) return null;
            var existing = anchor.GetPart<FishStockPart>(); if (existing != null && existing.ParentEntity == anchor) return existing;
            var at = zone.GetEntityCell(anchor);
            int span = MaxCapacity - MinCapacity + 1;
            int capacity = MinCapacity + FormationSelector.StableIndex(zone.ZoneID + ":fish:" + (at?.X ?? 0) + "," + (at?.Y ?? 0), span);
            var made = new FishStockPart { Capacity = capacity, Available = capacity, LastTick = now };
            anchor.AddPart(made);
            return made;
        }

        /// <summary>What the stock would be at <paramref name="now"/>, without changing it.</summary>
        public int AvailableAt(int now)
        {
            if (Available >= Capacity) return Capacity;
            int regen = now > LastTick ? (now - LastTick) / RegenTicks : 0;
            return Math.Min(Capacity, Available + Math.Max(0, regen));
        }

        /// <summary>Brings the stock up to <paramref name="now"/>: one fish per elapsed <see cref="RegenTicks"/>, and the clock restarts when it is full.</summary>
        public void Reconcile(int now)
        {
            if (Available >= Capacity) { Available = Capacity; LastTick = now; return; }
            if (now <= LastTick) return;
            int regen = (now - LastTick) / RegenTicks;
            if (regen <= 0) return;
            Available = Math.Min(Capacity, Available + regen);
            LastTick = Available >= Capacity ? now : LastTick + regen * RegenTicks;
        }

        /// <summary>How the water looks to someone standing beside it: a hint, never a count.</summary>
        public static string Look(int available, int capacity)
            => available <= 0 ? "fished out for now" : available * 3 <= capacity ? "thin" : available >= capacity ? "full of fish" : "well fished but not empty";
    }
}
