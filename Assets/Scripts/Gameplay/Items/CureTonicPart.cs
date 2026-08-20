namespace CavesOfOoo.Core
{
    /// <summary>
    /// Extension part for tonic items that cure status effects.
    /// Sits alongside TonicPart on the same entity and handles the
    /// ApplyTonic event to remove effects by class name.
    /// CureEffect = "PoisonedEffect", "BurningEffect", or "All".
    /// </summary>
    public class CureTonicPart : Part
    {
        public override string Name => "CureTonic";

        public string CureEffect = "";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "ApplyTonic")
            {
                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                var effects = actor.GetPart<StatusEffectsPart>();
                if (effects == null) return true;

                if (CureEffect == "All")
                {
                    // W2 close-out: "all AILMENTS", literally. A blanket
                    // RemoveAllEffects also stripped covenants like
                    // UnderTheClothEffect (and printed its calm expiry
                    // line mid-oath). A cure removes what is WRONG with
                    // you — the TYPE_NEGATIVE effects (WSP6.16 backfill)
                    // — and leaves oaths and boons alone.
                    bool any = false;
                    while (effects.RemoveEffect(eff =>
                        (eff.GetEffectType() & Effect.TYPE_NEGATIVE) != 0))
                        any = true;
                    if (any)
                        MessageLog.Add($"{actor.GetDisplayName()} is cured of all ailments!");
                }
                else
                {
                    bool removed = effects.RemoveEffect(eff => eff.ClassName == CureEffect);
                    if (removed)
                    {
                        string name = CureEffect.Replace("Effect", "").ToLower();
                        MessageLog.Add($"{actor.GetDisplayName()} is no longer {name}!");
                    }
                }
            }
            return true;
        }
    }
}
