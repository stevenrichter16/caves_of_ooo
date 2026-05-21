using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.6 — per-type gas immunity Part. Direct port of Qud's
    /// <c>XRL.World.Parts.GasImmunity</c> (GasImmunity.cs:1-41).
    ///
    /// <para><b>How it works.</b> Listens to <c>CheckGasCanAffect</c>
    /// (the event the filter pipeline in
    /// <see cref="IObjectGasBehaviorPart.CheckCanAffect"/> fires).
    /// When the event's <c>GasType</c> param matches this Part's
    /// <see cref="GasType"/>, <c>HandleEvent</c> returns false — which
    /// in CoO's event system causes <c>FireEvent</c> to return false →
    /// the gas treats it as vetoed (Entity.cs:255-265).</para>
    ///
    /// <para><b>Multi-type immunity.</b> A creature can carry multiple
    /// <c>GasImmunityPart</c> instances, one per immune type. They're
    /// independent — a creature with two GasImmunityParts (one for
    /// "Poison", one for "Cryo") is immune to both but still
    /// vulnerable to "Stun"/"Sleep"/etc. Empty <see cref="GasType"/>
    /// matches nothing (defensive — opt-out of "" being a blanket
    /// match).</para>
    /// </summary>
    public class GasImmunityPart : Part
    {
        public override string Name => "GasImmunity";

        /// <summary>The <see cref="GasPoolPart.GasType"/> this Part
        /// makes the wearer immune to. Case-sensitive: "Poison" ≠
        /// "poison" (mirroring the broader engine convention; see
        /// LA-followup for the related ImmuneElement case-insensitivity
        /// fix on liquids — gas-attribute strings are not user-facing
        /// content the same way, so leave case-sensitive for now).</summary>
        public string GasType = "";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "CheckGasCanAffect") return true;
            if (string.IsNullOrEmpty(GasType)) return true; // empty = no match
            string eventType = e.GetParameter<string>("GasType");
            if (eventType == GasType)
            {
                Diag.Record("gas", "ImmunityVeto", ParentEntity, null,
                    new { immuneTo = GasType });
                return false; // veto: target is immune
            }
            return true;
        }
    }
}
