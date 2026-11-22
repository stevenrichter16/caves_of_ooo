using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Builders;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Controlled fresh-game water audition. Only the two new trial
    /// owners opt in; no existing source, clock, NPC or exposure rule changes.</summary>
    [Scenario("Finite Water Trial", category: "Home",
        description: "Starts parched beside trial cisterns of three and one water units, with an empty waterskin. Compare a direct batch with carrying the same water away.")]
    public sealed class MeteredHydrationShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var at = ctx.Zone.GetEntityCell(ctx.PlayerEntity);
            var candidates = new List<Cell>();
            foreach (var offset in new[] { (1,0), (-1,0), (0,1), (0,-1), (1,1), (-1,1), (1,-1), (-1,-1) })
            {
                var cell = at == null ? null : ctx.Zone.GetCell(at.X + offset.Item1, at.Y + offset.Item2);
                if (cell != null && cell.IsPassable() && !ctx.Zone.TileState.HasCoating(cell.X, cell.Y, "water")) candidates.Add(cell);
            }
            if (candidates.Count < 2 || ctx.PlayerEntity.HasEffect<ParchedEffect>())
            { ctx.Log("[Water trial] This start cannot hold both trial cisterns or already has parch. Nothing was supplied or changed."); return; }
            var full = ctx.Factory.CreateEntity("Cistern"); var small = ctx.Factory.CreateEntity("Cistern");
            if (full == null || small == null) { ctx.Log("[Water trial] Cistern content is unavailable; nothing was changed."); return; }
            full.GetPart<WellPart>().MeteredHydration = small.GetPart<WellPart>().MeteredHydration = true;
            full.GetPart<WellPart>().Stock = 3; small.GetPart<WellPart>().Stock = 1;
            full.GetPart<RenderPart>().DisplayName = "trial cistern"; small.GetPart<RenderPart>().DisplayName = "small trial cistern";
            foreach (var source in new[] { full, small })
                source.GetPart<ExaminablePart>().Description += "\n\nThis trial source uses one water unit per level of parch. Drawing batches the available needed units in one action; an empty source stays empty.";
            if (!ctx.Zone.AddEntity(full, candidates[0].X, candidates[0].Y)) return;
            if (!ctx.Zone.AddEntity(small, candidates[1].X, candidates[1].Y)) { ctx.Zone.RemoveEntity(full); return; }
            var before = new HashSet<string>(ctx.PlayerEntity.GetPart<InventoryPart>().Objects.Select(e => e.ID));
            var missing = ScenarioLoadout.Give(ctx, new[] { ("Waterskin", 1) }, "[Water trial]");
            var skin = ctx.PlayerEntity.GetPart<InventoryPart>().Objects.FirstOrDefault(e => !before.Contains(e.ID) && e.HasPart<WaterskinPart>());
            if (missing.Count != 0 || skin == null) { ctx.Log("[Water trial] The skin was not supplied; the comparison is incomplete."); return; }
            ctx.PlayerEntity.ApplyEffect(new ParchedEffect { Stacks = 3 });
            ctx.Log("[Water trial] Controlled starting condition: three levels of parch, an empty three-drink skin, and two finite trial cisterns. This does not enable NPC thirst.");
            ctx.Log("[Water trial] Trial cistern: " + candidates[0].X + "," + candidates[0].Y + " (three units). Small trial cistern: " + candidates[1].X + "," + candidates[1].Y + " (one unit). Read the C-menu price before drinking.");
            ctx.Log("[Water trial] Save before acting. Compare separate copies of that start: draw three units for full relief, or fill the skin and drink it after walking away. The small cistern offers only partial relief. No source refills on its own.");
        }
    }
}
