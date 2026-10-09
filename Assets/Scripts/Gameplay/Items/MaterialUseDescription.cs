using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Read-only, carried-item guidance for supported repair materials
    /// and improvised combat supplies. Built on explicit inspection, never world
    /// generation or a repair dry run. False means the item is outside this bounded help surface
    /// or no longer has a usable unit in this actor's carried inventory.</summary>
    public static class MaterialUseDescription
    {
        public static bool TryDescribe(Entity actor, Entity item, EntityFactory factory, out string text)
        {
            text = null;
            var inventory = actor?.GetPart<InventoryPart>();
            if (item == null || inventory == null || !inventory.CanConsumeOne(item)) return false;
            if (item.HasPart<WaterskinPart>() || item.HasPart<LiquidVesselPart>())
            {
                var skin = item.GetPart<WaterskinPart>();
                var vessel = item.GetPart<LiquidVesselPart>();
                if ((skin == null) == (vessel == null)) return false;
                text = item.GetDisplayName() + "\n\n" + (skin != null
                    ? "Carries fresh water for drinking, crops, transfers and dousing burning scenery."
                    : "Collects and pours one kind of liquid at a time. Water can also supply crops, transfers and douse burning scenery; this flask has no drinking action.")
                    + "\n\nWith water inside, spend one unit to drench yourself or an adjacent creature. This puts out current flames and leaves the target wet."
                    + " Wetness helps resist heat ignition but strengthens a later electrical charge; it does not make fire attacks harmless."
                    + " Soaking a non-burning outsider provokes it. Fire clay can smother current flames without adding wetness."
                    + " The vessel remains after use; refill it from a suitable clean-water source.";
                return true;
            }
            if (item.BlueprintName == "FrogOil")
            {
                text = item.GetDisplayName() + "\n\nSpread one gourd underfoot or onto nearby ground to leave grease for eight turns."
                    + " Creatures crossing it may slip sideways, including you. The grease can catch fire."
                    + " Choose a direction from this item's actions; a successful use spends the gourd. Silver sand gives firm footing over grease.";
                return true;
            }
            if (item.BlueprintName == "VeilpuffBladder")
            {
                var gas = item.GetPart<GasGrenadePart>();
                // Saved bladders can retain their pre-veil examine text. Match
                // the exact legacy fallback or normal authored payload rather
                // than advertising cover for an explicit opt-out or other gas.
                if (gas == null || gas.GasId != "cryo-mist"
                    || !((string.IsNullOrEmpty(gas.SightCloud) && gas.SightCloudTurns == 0)
                        || (gas.SightCloud == "veil-mist" && gas.SightCloudTurns == 4))) return false;
                text = item.GetDisplayName() + "\n\nThrow the bladder to release freezing mist and a stationary four-turn veil that blocks distant sight."
                    + " Put the veil between you and an enemy, then move out of sight; pursuers search where they last saw you."
                    + " Bodies and projectiles can cross the veil, and nearby enemies remain dangerous."
                    + " The cold mist can drift and harm allies too. Throwing spends the bladder.";
                return true;
            }
            if (item.BlueprintName == "SalvagedTimber" || item.BlueprintName == "KnotflaxCord")
            {
                text = item.GetDisplayName() + "\n\n" + (item.BlueprintName == "SalvagedTimber"
                    ? "Two lengths brace a damaged wooden gate. One length makes a permanent wooden jam in a visible spike trap, bear trap, fire trap or pressure plate that offers the action. Stand beside that trap and choose Jam mechanism; a successful jam spends one length and cannot be removed or reclaimed. Recover timber from finite pallets or fallen frame salvage, or ask a mender."
                    : "One coil replaces a snapped well line. Gather a dry knotflax bundle or harvest ripe knotflax from a tilled bed. One coil also lays a visible, single-use snare on adjacent empty ground. The first creature crossing it cannot move for two of its turns but can still attack or cast. You and your companions can be caught too; the spent cord cannot be recovered.")
                    + " For repairs, stand beside the damaged object, examine it, then choose its repair action. Only successful work spends supplies.";
                return true;
            }
            string guide, fallback, use, resident;
            switch (item.BlueprintName)
            {
                case SettlementRepairDefinitions.FireClayBlueprint:
                    guide = SettlementRepairDefinitions.OvenBuildersGuideBlueprint;
                    fallback = "oven builder's guide"; use = "rebuild a damaged settlement oven"; resident = "farmer";
                    break;
                case SettlementRepairDefinitions.SilverSandBlueprint:
                    guide = SettlementRepairDefinitions.WellMaintenanceManualBlueprint;
                    fallback = "well maintenance manual"; use = "repair a damaged settlement well's filtration ring"; resident = "keeper";
                    break;
                case SettlementRepairDefinitions.WardOilBlueprint:
                    guide = SettlementRepairDefinitions.LanternOilRecipeBlueprint;
                    fallback = "lantern oil recipe"; use = "reforge a settlement watch lantern"; resident = "warden";
                    break;
                default: return false;
            }
            string guideName = fallback;
            if (factory != null && factory.Blueprints.TryGetValue(guide, out var blueprint)
                && blueprint.Parts.TryGetValue("Render", out var render)
                && render.TryGetValue("DisplayName", out var name) && !string.IsNullOrWhiteSpace(name)) guideName = name;
            // Match SettlementManager's exact blueprint and CanConsumeOne gate.
            bool carried = false;
            foreach (var candidate in inventory.Objects)
                if (candidate != null && candidate.BlueprintName == guide && inventory.CanConsumeOne(candidate))
                { carried = true; break; }
            text = item.GetDisplayName()+"\n\nOne measure can "+use+". Speak to its "+resident
                +" where this repair is offered. The material is spent; the guide is kept.\n\nRequired guide: "+guideName
                +(carried?" (carried).":" (missing). Ask the settlement elder about the damaged site for its guide.");
            if (item.BlueprintName == SettlementRepairDefinitions.FireClayBlueprint)
                text += "\n\nAnother use: Morrowfast's bell. Ask Nemm about it and inspect the reserve cord first."
                    +" After diagnosis, one fire clay makes a quiet clapper; the bare, loud setting costs no material.";
            if (item.BlueprintName == SettlementRepairDefinitions.FireClayBlueprint)
                text += "\n\nStructural repair: two measures seal a split clay-lined catch well. Stand beside it and choose Repair; this repair needs no guide. Exposed fire-clay seams provide finite supplies."
                    + "\n\nEmergency use: smother flames on yourself or an adjacent burning creature or object with one measure. This adds no wetness, useful around electrical threats. Existing wetness and other ailments remain. It provides no lasting fire protection; leave the source of heat.";
            if (item.BlueprintName == SettlementRepairDefinitions.SilverSandBlueprint)
                text += "\n\nScatter one measure as grit underfoot or onto nearby ground for twelve turns of firm footing over oil or ice."
                    + " Enemies can use that footing too. It does not put out fire or make dangerous liquids harmless.";
            return true;
        }
    }
}
