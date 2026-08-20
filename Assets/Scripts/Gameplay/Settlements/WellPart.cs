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

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                actions?.AddAction("Draw water", "draw water", "DrawWaterAtWell", 'w', 20);
                return true;
            }
            if (e.ID == "InventoryAction")
            {
                if (e.GetStringParameter("Command") != "DrawWaterAtWell") return true;
                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                bool wasParched = actor.HasEffect<ParchedEffect>();
                if (wasParched)
                {
                    actor.GetPart<StatusEffectsPart>()?.RemoveEffect<ParchedEffect>();
                    MessageLog.Add("You draw cool water and drink until the parch lets go.");
                }
                else
                {
                    MessageLog.Add("You drink. The well water is cool.");
                }

                if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("furniture"))
                {
                    CavesOfOoo.Diagnostics.Diag.Record(
                        category: "furniture", kind: "WaterDrawn",
                        actor: actor, target: ParentEntity,
                        payload: new { curedParched = wasParched });
                }
                e.Handled = true;
                return false;
            }
            return true;
        }
    }
}
