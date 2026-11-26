using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Why a process row is not offered (Docs/MACHINE-MULTI-STACK-CLAIMS.md, spec S12a, shaped by the independent critique). A row that cannot run now vanished silently; this explains the
    /// near-miss: the source is carried and permitted and exactly ONE requirement group fails. It is read-only, creates no quote or token, never runs on a menu for a ground or
    /// shop item, and yields at most one reason per (source, process), so a common item never lists every recipe it could ever join.
    /// </summary>
    internal static class MaterialProcessMisses
    {
        internal const string Short = "short", StacksIncompatible = "stacks-incompatible", NoTool = "no-tool", NoStation = "no-station";
        internal const int RowPriority = 4;

        internal readonly struct Miss
        {
            internal readonly string Code, Reason;
            internal Miss(string code, string reason) { Code = code; Reason = reason; }
        }

        static int Count(Entity e) => e.GetPart<StackerPart>()?.StackCount ?? 1;
        static string Label(EntityFactory factory, string blueprint)
            => factory.Blueprints.TryGetValue(blueprint, out var bp) && bp.Parts.TryGetValue("Render", out var p) && p.TryGetValue("DisplayName", out var label) ? label : blueprint;

        /// <summary>The single reason a carried, permitted source cannot run this process now, or null when it is not a near-miss (nothing is missing, several things are, or it is not eligible).</summary>
        internal static Miss? Explain(Entity actor, Entity source, string id, Zone zone, EntityFactory factory)
        {
            var d = MaterialProcessRegistry.Get(id);
            if (d == null || factory == null || ForgePart.Factory != factory || actor == null || source == null || zone == null
                || !PreparationActions.CurrentActor(actor, zone) || !PreparationActions.Carried(actor, source)) return null;
            var misses = new List<Miss>();
            if (Count(source) < d.InputCount) misses.Add(new Miss(Short, "You need " + d.InputCount + " " + Label(factory, d.SourceBlueprint) + " for this."));
            // The work provider.
            if (d.WorkKind == "HandCutting")
            {
                if (MaterialCuttingWorkReceipt.Resolve(actor, source, zone) == null) misses.Add(new Miss(NoTool, "You need a usable cutting tool in your pack."));
            }
            else if (d.WorkKind == "Screening")
            {
                if (MaterialScreeningWorkReceipt.Resolve(actor, source, zone) == null) misses.Add(new Miss(NoTool, "You need a sound hand sieve carried in your pack."));
            }
            else if (d.WorkKind == SeedFramePart.WorkKind)
            {
                if (SeedFramePart.IsKnotflaxWork(d))
                {
                    if (MaterialProcessPlan.VisibleSeedFrame(actor, zone) == null
                        && MaterialScreeningWorkReceipt.Resolve(actor, source, zone) == null)
                        misses.Add(new Miss(NoTool, "Carry a sound hand sieve or stand beside an intact public seed frame."));
                }
                else if (StationQuery.Resolve(actor, zone, SeedFramePart.Kind) == null)
                    misses.Add(new Miss(NoStation, "Stand beside an intact public seed frame."));
            }
            else if (ForgeWorkReceipt.Resolve(actor, zone) == null)
            {
                // An unready rig or forge within reach already says why in its own menu; only a place with none at all is a miss here.
                var candidates = new List<StationCandidate>(); StationQuery.Collect(actor, zone, ForgeWorkReceipt.Kind, candidates);
                if (candidates.Count == 0) misses.Add(new Miss(NoStation, "You need to be beside a forge to do this."));
                else return null;
            }
            // Extra inputs.
            var pack = actor.GetPart<InventoryPart>();
            for (int i = 0; i < d.AdditionalInputBlueprints.Count; i++)
            {
                string bp = d.AdditionalInputBlueprints[i]; int need = d.AdditionalInputCounts[i];
                if (MaterialProcessIngredient.Resolve(actor, source, null, bp, need, factory) != null) continue;
                int have = pack.Objects.Where(e => e?.BlueprintName == bp && e != source && PreparationActions.Carried(actor, e)).Sum(Count);
                misses.Add(have < need
                    ? new Miss(Short, "You need " + need + " " + Label(factory, bp) + " for this, and have " + have + ".")
                    : new Miss(StacksIncompatible, "You carry enough " + Label(factory, bp) + ", but they are not alike enough to be used together."));
            }
            return misses.Count == 1 ? misses[0] : (Miss?)null;
        }
    }
}
