using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Furniture part marking an alchemy still — the brewing station.
    /// Per the M1 design lockdown (Docs/CRAFTING-ALCHEMY-SYSTEM.md §6.2),
    /// tonics/coatings/throwables require a still; only simple Foods may be
    /// field-brewed. Same furniture shape as ChairPart/BedPart: a marker
    /// part on a PhysicalObject blueprint.
    ///
    /// M3-L3: the still is also the brew VERB — it declares "Brew" rows on
    /// the look-mode world-action menu and, on selection, resolves the
    /// player's set-aside reagents (<see cref="CraftingMarkPart"/>) into a
    /// <see cref="BrewReagentsCommand"/> execution. All brew rules (still
    /// gating, mishap damage, batching) stay in the command; this handler
    /// only collects the selection and reports selection problems legibly.
    /// </summary>
    public class AlchemyStillPart : Part
    {
        public override string Name => "AlchemyStill";

        /// <summary>
        /// EntityFactory for brew output creation. Wired at bootstrap
        /// (mirrors CorpsePart.Factory); when unwired the brew rows degrade
        /// to a message instead of crashing mid-event.
        /// </summary>
        public static EntityFactory Factory;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                {
                    actions.AddAction("Brew", "brew the mix", "BrewMix", 'b', 20);
                    actions.AddAction("BrewBatch", "brew a full batch", "BrewMixBatch", 'B', 19);

                    // Actor-aware picker rows (live-playtest finding): build
                    // the mix right in this menu — one toggle per carried
                    // reagent. Actor is absent on actor-less gathers.
                    CraftingMarkPart.AddToggleRows(actions, e.GetParameter<Entity>("Actor"),
                        item => item.HasPart<ReagentPart>(), basePriority: 10);
                }
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "BrewMix" && command != "BrewMixBatch")
                    return true;

                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null)
                    return true;

                e.Handled = true;
                HandleBrew(actor, e.GetParameter<Zone>("Zone"), batch: command == "BrewMixBatch");
                return false;
            }

            return true;
        }

        private static void HandleBrew(Entity actor, Zone zone, bool batch)
        {
            if (Factory == null)
            {
                MessageLog.Add("The still gurgles, but nothing comes of it. (Brewing is not wired to a factory.)");
                return;
            }

            var marked = CraftingMarkPart.CollectMarked(actor);
            if (marked.Reagents.Count == 0)
            {
                MessageLog.Add("The mix is empty — pick reagents from this menu (or set them aside in your pack) first.");
                return;
            }

            int count = 1;
            if (batch)
            {
                count = BrewingService.GetMaxBatchCount(marked.Reagents);
                if (count < 1)
                    count = 1;
            }

            var result = InventorySystem.ExecuteCommand(
                new BrewReagentsCommand(marked.Reagents, Factory, count), actor, zone);

            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                MessageLog.Add(result.ErrorMessage);
        }

        /// <summary>
        /// True when <paramref name="actor"/> stands on or orthogonally/
        /// diagonally adjacent to (3×3 box) a cell containing an entity
        /// with an AlchemyStillPart. False when the zone is null or the
        /// actor isn't placed in it.
        /// </summary>
        public static bool IsNearStill(Entity actor, Zone zone)
        {
            if (actor == null || zone == null)
                return false;

            (int x, int y) = zone.GetEntityPosition(actor);
            if (x < 0 || y < 0)
                return false;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Cell cell = zone.GetCell(x + dx, y + dy);
                    if (cell == null)
                        continue;

                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        Entity obj = cell.Objects[i];
                        if (obj != null && obj.HasPart<AlchemyStillPart>())
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
