namespace CavesOfOoo.Core
{
    /// <summary>
    /// BIOME-OVERHAUL A6 — a "use" consumable that refills the actor's
    /// Ink wallet (the weapon-rental currency). Before this existed the
    /// starting 50 Ink was a character's LIFETIME supply: the GiveInk
    /// conversation action was registered but called by zero content
    /// files, so the Quartermaster's rental economy went permanently
    /// inert once spent. Scribes stock these (they copy grimoires for a
    /// living); the vial also circulates in the general trade pool.
    /// Action/consume shape mirrors <see cref="TonicPart"/>.
    /// </summary>
    public class InkVialPart : Part
    {
        public override string Name => "InkVial";

        /// <summary>Ink granted per use.</summary>
        public int InkAmount = 25;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                actions?.AddAction("Use", "use", "UseInkVial", 'u', 20);
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                if (e.GetStringParameter("Command") != "UseInkVial") return true;
                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                RentalSystem.AddInk(actor, InkAmount);
                int total = RentalSystem.GetInk(actor);
                MessageLog.Add($"You decant the ink. (+{InkAmount} ink, {total} total)");

                if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("trade"))
                {
                    CavesOfOoo.Diagnostics.Diag.Record(
                        category: "trade", kind: "InkRefilled",
                        actor: actor,
                        payload: new { amount = InkAmount, total });
                }

                ConsumeOne(actor);
                e.Handled = true;
                return false;
            }

            return true;
        }

        private void ConsumeOne(Entity actor)
        {
            var stacker = ParentEntity.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
                stacker.StackCount--;
            else
                actor.GetPart<InventoryPart>()?.RemoveObject(ParentEntity);
        }
    }
}
