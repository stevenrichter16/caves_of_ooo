namespace CavesOfOoo.Core
{
    /// <summary>
    /// W2.3 — a well you can actually drink from. Wells were inert props
    /// (Render + Physics) even though canon makes them the wasteland's
    /// nodes of power ("water is the truest wealth and is given, never
    /// sold, to a guest" — Lore/History/08_MaterialCulture.md:161).
    /// Drawing water clears <see cref="ParchedEffect"/> entirely — the
    /// well is the cure the pan is the cause of.
    ///
    /// <para>Mirrors <see cref="SanctuaryPart"/>'s action idiom exactly
    /// (declare on GetInventoryActions, act on InventoryAction).</para>
    /// </summary>
    public class WellPart : Part
    {
        public override string Name => "Well";
        /// <summary>Ordinary wells work; an authored structural fault disables both drinking and filling.</summary>
        public bool IsUsable => !RepairablePart.BlocksFunction(ParentEntity);

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var queryingActor = e.GetParameter<Entity>("Actor");
                if (!WellDrinkingService.Available(queryingActor, this, e.GetParameter<Zone>("Zone") ?? queryingActor?.SpatialZone ?? SettlementRuntime.ActiveZone)) return true;
                var actions = e.GetParameter<InventoryActionList>("Actions");
                actions?.AddAction("Draw water", "draw water", "DrawWaterAtWell", 'w', 20);
                return true;
            }
            if (e.ID == "InventoryAction")
            {
                if (e.GetStringParameter("Command") != "DrawWaterAtWell") return true;
                var actor = e.GetParameter<Entity>("Actor");
                if (!WellDrinkingService.TryDrink(actor, this, e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone,
                    e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction"))) return true;
                e.Handled = true;
                return false;
            }
            return true;
        }
    }
}
