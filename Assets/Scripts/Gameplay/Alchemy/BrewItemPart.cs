using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Carries a brewed item's resolved effects and applies ALL of them when
    /// the item is consumed. Listens for the same "ApplyTonic" event
    /// StatusTonicPart does (fired by TonicPart.ApplyTo), so a brewed item
    /// flows through the entire existing drink/apply/throw pipeline with no
    /// new plumbing — the difference is that a brew can carry SEVERAL effects
    /// (a galvanic draught is Acidic + Electrified on one flask), where
    /// StatusTonic carries exactly one.
    ///
    /// EffectsRaw uses the shared "Name:Potency;Name:Potency" grammar
    /// (BrewPropertyAmount.ParseList, case-preserved). Effect names dispatch
    /// through TonicEffectFactory — the same canonical table tonics use — so
    /// every name a BrewRule can emit is applyable here by construction.
    ///
    /// Potency mapping (M1.2): potency flows into the factory's magnitude
    /// slot; duration/dice use per-effect defaults. Duration-style effects
    /// (Poisoned, Stoneskin) therefore get default durations regardless of
    /// potency — a documented M1 simplification; per-effect potency→duration
    /// mapping is an M1.3 refinement (Docs/CRAFTING-ALCHEMY-SYSTEM.md §9).
    /// </summary>
    public class BrewItemPart : Part
    {
        public override string Name => "BrewItem";

        /// <summary>E.g. "Burning:2" or "Acidic:2;Electrified:1".</summary>
        public string EffectsRaw = "";

        /// <summary>Resolved brew form: "Tonic" | "Coating" | "Throwable" | "Food".</summary>
        public string Form = "Tonic";

        private List<BrewPropertyAmount> _cachedEffects;
        private string _cachedRawSnapshot;

        public IReadOnlyList<BrewPropertyAmount> GetEffects()
        {
            if (_cachedEffects == null || !string.Equals(_cachedRawSnapshot, EffectsRaw))
            {
                _cachedEffects = BrewPropertyAmount.ParseList(EffectsRaw, lowerCaseNames: false);
                _cachedRawSnapshot = EffectsRaw;
            }

            return _cachedEffects;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "ApplyTonic")
                return true;

            var target = e.GetParameter<Entity>("Actor");
            if (target == null)
                return true;

            Zone zone = e.GetParameter<Zone>("Zone");
            Entity source = e.GetParameter<Entity>("Source");

            IReadOnlyList<BrewPropertyAmount> effects = GetEffects();
            for (int i = 0; i < effects.Count; i++)
            {
                BrewPropertyAmount entry = effects[i];
                Effect effect = TonicEffectFactory.Create(
                    entry.Property,
                    effectDuration: 0,
                    effectDamageDice: "",
                    effectMagnitude: entry.Potency,
                    source: source);

                if (effect != null)
                    target.ApplyEffect(effect, source, zone);
            }

            return true;
        }
    }
}
