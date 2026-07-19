using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Data definition for one brew-resolution rule (loaded from
    /// Content/Data/Alchemy/BrewRules.json). A rule fires when the combined
    /// reagent property profile contains ALL of <see cref="RequireAll"/> and
    /// NONE of <see cref="ForbidAny"/>; when it fires it contributes one
    /// effect to the brew. Multiple rules can fire on one mix — that is how
    /// emergent combos arise (e.g. corrosive+conductive fires both the acid
    /// rule and the shock rule → a galvanic draught).
    ///
    /// This is the *legible rule table* that keeps the system from being
    /// opaque randomness (Docs/CRAFTING-ALCHEMY-SYSTEM.md §2.2): once a
    /// player learns "heat + combustible = fire," it holds for ANY heat
    /// reagent + ANY combustible reagent. Knowledge transfers; that is the
    /// depth.
    /// </summary>
    [Serializable]
    public class BrewRule
    {
        public string ID;

        /// <summary>
        /// Space/comma-delimited property ids that must ALL be present for
        /// this rule to fire. A rule with an empty RequireAll never fires
        /// (so it can't match every mix vacuously).
        /// </summary>
        public string RequireAll;

        /// <summary>
        /// Space/comma-delimited property ids that, if ANY is present, veto
        /// this rule (e.g. a frost rule forbids "heat"). Optional.
        /// </summary>
        public string ForbidAny;

        /// <summary>
        /// The effect name this rule contributes. Fed (downstream, in the
        /// M1.2 BrewingService) into the same name→Effect dispatch the
        /// existing tonics use (StatusTonicPart.CreateEffect), so M1.1 rules
        /// intentionally name only effects that dispatch already supports.
        /// </summary>
        public string Effect;

        /// <summary>
        /// Output form hint: "Tonic" | "Coating" | "Throwable" | "Food".
        /// When multiple rules fire, the highest-<see cref="Priority"/> rule's
        /// form wins. A "volatile" property in the mix overrides the form to
        /// "Throwable" regardless (a volatile brew wants to be thrown).
        /// </summary>
        public string Form;

        /// <summary>Form tiebreak when several rules fire. Higher wins.</summary>
        public int Priority;

        /// <summary>
        /// Optional multiplier on the derived effect potency. Defaults to 1.
        /// Potency itself is the MAX potency among this rule's required
        /// properties in the mix (§6.4), then scaled by this and floored at 1.
        /// </summary>
        public float MagnitudeScale = 1f;

        /// <summary>Player-facing one-liner shown on discovery / in the brew log.</summary>
        public string Description;
    }
}
