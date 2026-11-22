namespace CavesOfOoo.Core
{
    /// <summary>
    /// W2.3 — a well you can actually drink from. Wells were inert props
    /// (Render + Physics) even though canon makes them the wasteland's
    /// nodes of power ("water is the truest wealth and is given, never
    /// sold, to a guest" — Lore/History/08_MaterialCulture.md:161).
    /// Ordinary drawing clears <see cref="ParchedEffect"/> entirely.
    /// Explicit finite metering instead prices one stack per water unit.
    ///
    /// <para>Mirrors <see cref="SanctuaryPart"/>'s action idiom exactly
    /// (declare on GetInventoryActions, act on InventoryAction).</para>
    /// </summary>
    public class WellPart : Part
    {
        public override string Name => "Well";
        /// <summary>Ordinary wells work; an authored structural fault disables both drinking and filling.</summary>
        public bool IsUsable => !RepairablePart.BlocksFunction(ParentEntity);

        /// <summary>Units of water left. -1 (the default) is an endless well or spring. A finite well
        /// (a waysite rain cistern, Docs/CHUNK-ACTIVITY.md) spends one unit per drink and one per
        /// waterskin charge, and offers nothing once dry. A well with its own LiquidPool counts that
        /// pool instead. Plain public: it round-trips through the reflection save path.</summary>
        public int Stock = -1;

        /// <summary>Opt-in source tariff: finite water relieves one Parched
        /// stack per unit in one paid batch. Defaults false for old saves and
        /// ordinary sources. An endless well retains its free full recovery.</summary>
        public bool MeteredHydration;

        /// <summary>False only for a finite well that has run dry.</summary>
        public bool HasWater => Stock != 0;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var queryingActor = e.GetParameter<Entity>("Actor");
                WellDrinkingService.AddAction(queryingActor, this,
                    e.GetParameter<Zone>("Zone") ?? queryingActor?.SpatialZone ?? SettlementRuntime.ActiveZone,
                    e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (!WellDrinkingService.IsDrinkCommand(command)) return true;
                var actor = e.GetParameter<Entity>("Actor");
                if (!WellDrinkingService.TryDrink(actor, this, e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone,
                    e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction"), command)) return true;
                e.Handled = true;
                return false;
            }
            return true;
        }
    }
}
