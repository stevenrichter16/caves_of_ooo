using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The parcels the player has entrusted to the Concord's carriage (<see cref="ConcordCarriage"/>). It lives on the player so it rides the save graph with the pack: each
    /// parcel is the real goods entity (every stack count, flask volume and name intact) held here and nowhere else, so nothing is copied and nothing can be in two places. The
    /// parallel lists are one record per parcel: where it was taken in, where it is going (zone ids), the tick it was sent, the tick it will have arrived and the fee that was paid.
    /// Arrival is a comparison against the clock when the player stands at the destination post, never a timer on a zone that is not loaded.
    /// </summary>
    public sealed class ConcordCarriagePart : Part
    {
        public override string Name => "ConcordCarriage";

        public List<Entity> Goods = new List<Entity>();
        public List<string> From = new List<string>(), To = new List<string>();
        public List<int> SentTick = new List<int>(), ReadyTick = new List<int>(), Fee = new List<int>();

        public int Count => Goods.Count;

        /// <summary>The records that are intact. Only these count toward the cart's limit; a torn record (a corrupted save) is left alone, never delivered and never counted.</summary>
        public int WholeCount { get { int n = 0; for (int i = 0; i < Goods.Count; i++) if (Whole(i)) n++; return n; } }

        /// <summary>A record is whole only when every list has an entry for it, the goods survived the load and both ends are named; a torn record is never delivered or counted.</summary>
        public bool Whole(int i) => i >= 0 && i < Goods.Count && Goods[i] != null && i < From.Count && i < To.Count && i < SentTick.Count && i < ReadyTick.Count && i < Fee.Count
            && !string.IsNullOrEmpty(From[i]) && !string.IsNullOrEmpty(To[i]);

        /// <summary>Appends one record. If a corrupted save left the lists at different lengths, the short ones are padded first (with empty ends, so the torn records stay torn) so the new record
        /// lands at the same index in every list.</summary>
        public void Add(Entity goods, string from, string to, int sent, int ready, int fee)
        {
            int n = Goods.Count;
            while (From.Count < n) From.Add(""); while (To.Count < n) To.Add(""); while (SentTick.Count < n) SentTick.Add(0); while (ReadyTick.Count < n) ReadyTick.Add(0); while (Fee.Count < n) Fee.Add(0);
            if (From.Count > n) From.RemoveRange(n, From.Count - n); if (To.Count > n) To.RemoveRange(n, To.Count - n);
            if (SentTick.Count > n) SentTick.RemoveRange(n, SentTick.Count - n); if (ReadyTick.Count > n) ReadyTick.RemoveRange(n, ReadyTick.Count - n); if (Fee.Count > n) Fee.RemoveRange(n, Fee.Count - n);
            Goods.Add(goods); From.Add(from); To.Add(to); SentTick.Add(sent); ReadyTick.Add(ready); Fee.Add(fee);
        }

        public void RemoveAt(int i)
        {
            if (i < 0 || i >= Goods.Count) return;
            Goods.RemoveAt(i);
            if (i < From.Count) From.RemoveAt(i);
            if (i < To.Count) To.RemoveAt(i);
            if (i < SentTick.Count) SentTick.RemoveAt(i);
            if (i < ReadyTick.Count) ReadyTick.RemoveAt(i);
            if (i < Fee.Count) Fee.RemoveAt(i);
        }
    }
}
