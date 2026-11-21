using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Furniture part marking an alchemy still - the brewing station.
    /// Per the M1 design lockdown (Docs/CRAFTING-ALCHEMY-SYSTEM.md §6.2),
    /// tonics/coatings/throwables require a still; only simple Foods may be
    /// field-brewed. Same furniture shape as ChairPart/BedPart: a marker
    /// part on a PhysicalObject blueprint.
    ///
    /// M3-L3: the still is also the brew VERB - it declares "Brew" rows on
    /// the look-mode world-action menu and, on selection, resolves the
    /// player's set-aside reagents (<see cref="CraftingMarkPart"/>) into a
    /// <see cref="BrewReagentsCommand"/> execution. All brew rules (still
    /// gating, mishap damage, batching) stay in the command; this handler
    /// only collects the selection and reports selection problems legibly.
    /// </summary>
    public class AlchemyStillPart : Part, IStationCapability, IMenuStatus, IDescribeUse, IDescribeUseRows
    {
        public override string Name => "AlchemyStill";

        // ── station capability (Docs/MACHINE-STATIONS.md): the still keeps its older presence-only rule ──
        static readonly string[] StationKinds = { "still" };
        public System.Collections.Generic.IReadOnlyList<string> Kinds => StationKinds;
        public int Reach => 1;
        public ReadinessMode Readiness => ReadinessMode.PresenceOnly;

        /// <summary>
        /// EntityFactory for brew output creation. Wired at bootstrap
        /// (mirrors CorpsePart.Factory); when unwired the brew rows degrade
        /// to a message instead of crashing mid-event.
        /// </summary>
        public static EntityFactory Factory;

        public int Order => 10;

        /// <summary>How to brew at the still (the owner, 2026-11-21). The "right now" line is built from the same brew and pick rows the menu shows.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            if (ParentEntity?.GetPart<PhysicsPart>()?.InInventory != null || PackableFixturePart.IsInertKit(ParentEntity)) { guide.How.Add("Once it is set down, press C beside it to brew from the reagents you carry, settle bog-mire or bake it into fire clay."); return; }
            guide.How.Add("Press C beside it, pick reagents from your pack with the pick rows (the menu shows the mix and what it would make), then choose brew the mix, or brew a full batch.");
            guide.Needs.Add("to brew: reagents in your pack that make something together");
            guide.Limits.Add("Brewing uses up the picked reagents.");
            guide.WhyNot = "you are not beside it";
        }

        /// <summary>The brew rows are always in the menu but only work with reagents picked, so describing states them that way (and says what to do first).</summary>
        public void AddUseRows(Entity actor, Zone zone, InventoryActionList rows)
        {
            if (rows == null || actor == null || ParentEntity?.GetPart<PhysicsPart>()?.InInventory != null || PackableFixturePart.IsInertKit(ParentEntity)) return;
            var picks = CraftingMarkPart.CollectMarked(actor).Reagents;
            if (picks.Count == 0) { rows.AddDisabled("Brew", "brew the mix", "no-picks", "Pick the reagents to brew from your pack first."); return; }
            int batch = BrewingService.GetMaxBatchCount(picks);
            rows.AddAction("Brew", "brew the mix", "BrewMix");
            rows.AddAction("BrewBatch", batch > 1 ? "brew a full batch (up to " + batch + ")" : "brew a full batch", "BrewMixBatch");
        }

        /// <summary>The still's menu says what the mix is and what it would make, as much as the pack's Crafting panel does (see <see cref="BrewMenuReadout"/>). A carried or taken-up still is a kit and says nothing.</summary>
        public void AppendMenuStatus(DescribeContext context, System.Collections.Generic.List<string> lines)
        {
            if (ParentEntity?.GetPart<PhysicsPart>()?.InInventory != null || PackableFixturePart.IsInertKit(ParentEntity)) return;
            lines.AddRange(BrewMenuReadout.Lines(context.Viewer));
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                // A still that is carried, dropped or taken up is a kit, not a station: its brew rows belong to the set-down still.
                if (ParentEntity.GetPart<PhysicsPart>()?.InInventory != null || PackableFixturePart.IsInertKit(ParentEntity)) return true;
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                {
                    var picks = CraftingMarkPart.CollectMarked(e.GetParameter<Entity>("Actor")).Reagents;
                    int batch = picks.Count > 0 ? BrewingService.GetMaxBatchCount(picks) : 0;
                    actions.AddActionWithHint("Brew", "brew the mix", "BrewMix",
                        picks.Count == 0 ? "Brews one of the mix and uses up the picked reagents. Pick reagents below first." : "Brews one of the mix shown above and uses up the picked reagents.", 'b', 20);
                    actions.AddActionWithHint("BrewBatch", batch > 1 ? "brew a full batch (up to " + batch + ")" : "brew a full batch", "BrewMixBatch",
                        batch > 1 ? "Brews as many as the picked reagents allow, up to " + batch + ", in one go." : "Brews as many as the picked reagents allow, in one go.", 'B', 19);

                    // Actor-aware picker rows (live-playtest finding): build
                    // the mix right in this menu - one toggle per carried
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
                MessageLog.Add("The mix is empty - pick reagents from this menu (or set them aside in your pack) first.");
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
        public static bool IsNearStill(Entity actor, Zone zone) => StationQuery.NearPresence(actor, zone, "still");
    }
}
