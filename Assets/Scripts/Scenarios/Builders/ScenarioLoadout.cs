using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Scenarios.Builders
{
    /// <summary>
    /// Gives a hand-play fixture's promised loadout to the real pack, and says so loudly when any of it did not arrive.
    /// The shipped player's pack has a hard cap (<c>MaxWeight</c> 150, from the Creature blueprint) that sits below the Strength-based capacity the sidebar shows (Strength × 15, a soft overburden limit),
    /// and <see cref="PlayerBuilder.GiveItem"/> answers a full pack with a console warning only. A long loadout therefore arrived partly and silently: the first hand-play of the machines fixture (2026-11-21)
    /// found no striker, torch, flasks or repair parts, because tests had always raised the cap first. Room is made for a fixture's own loadout here, never by changing the shipped cap.
    /// </summary>
    public static class ScenarioLoadout
    {
        /// <summary>The weight of one unit of each blueprint times its count. A blueprint that does not exist weighs nothing here (giving it reports it).</summary>
        public static int WeightOf(EntityFactory factory, IEnumerable<(string Blueprint, int Count)> loadout)
        {
            int total = 0;
            if (factory == null || loadout == null) return 0;
            foreach (var (blueprint, count) in loadout)
            {
                if (count <= 0 || !factory.Blueprints.ContainsKey(blueprint)) continue;
                var unit = factory.CreateEntity(blueprint);
                if (unit != null) total += InventoryPart.GetItemWeight(unit) * count;
            }
            return total;
        }

        /// <summary>
        /// Lifts the player's hard carry cap, where one is set, to what the sidebar says they can carry or to what is already carried plus this loadout, whichever is more.
        /// Never lowers a cap, and leaves a pack with no hard cap (negative) uncapped. The room above the loadout is for what the fixture's own steps produce (clay, flakes, fish).
        /// </summary>
        public static void MakeRoom(ScenarioContext ctx, IEnumerable<(string Blueprint, int Count)> loadout)
        {
            var pack = ctx?.PlayerEntity?.GetPart<InventoryPart>();
            if (pack == null || pack.MaxWeight < 0 || loadout == null || !loadout.Any(l => l.Count > 0)) return;   // nothing to give means nothing to make room for
            int room = System.Math.Max(pack.GetMaxCarryWeight(), pack.GetCarriedWeight() + WeightOf(ctx.Factory, loadout));
            if (room > pack.MaxWeight) pack.MaxWeight = room;
        }

        /// <summary>
        /// Makes room, gives every item, then checks that each blueprint arrived in full (counted against what the pack held before, so something already carried cannot hide a shortfall).
        /// Anything missing is said in the in-game message log under <paramref name="tag"/> and returned; an empty list means it all arrived.
        /// </summary>
        public static List<string> Give(ScenarioContext ctx, IEnumerable<(string Blueprint, int Count)> loadout, string tag)
        {
            var wanted = (loadout ?? Enumerable.Empty<(string Blueprint, int Count)>()).Where(l => l.Count > 0).ToArray();
            var pack = ctx.PlayerEntity.GetPart<InventoryPart>();
            var wantedBy = wanted.GroupBy(l => l.Blueprint).ToDictionary(g => g.Key, g => g.Sum(l => l.Count));
            var before = wantedBy.Keys.ToDictionary(k => k, k => Held(pack, k));
            MakeRoom(ctx, wanted);
            foreach (var (blueprint, count) in wanted) ctx.Player.GiveItem(blueprint, count);
            var missing = new List<string>();
            foreach (var pair in wantedBy)
            {
                int got = Held(pack, pair.Key) - before[pair.Key];
                if (got < pair.Value) missing.Add(pair.Key + " (" + got + " of " + pair.Value + ")");
            }
            if (missing.Count > 0)
                ctx.Log((tag ?? "") + " The pack did not receive all of this fixture's loadout, so some steps cannot be tried: " + string.Join(", ", missing) + ".");
            return missing;
        }

        static int Held(InventoryPart pack, string blueprint)
            => pack == null ? 0 : pack.Objects.Where(o => o != null && o.BlueprintName == blueprint).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
    }
}
