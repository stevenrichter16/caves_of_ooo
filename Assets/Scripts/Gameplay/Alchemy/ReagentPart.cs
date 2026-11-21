using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marks an item as an alchemy reagent and carries its brew properties.
    /// Blueprint-authorable: <c>{ "Name": "Reagent", "Params": [{ "Key":
    /// "PropertiesRaw", "Value": "heat:2, volatile:1" }] }</c> — EntityFactory
    /// resolves "Reagent" → ReagentPart and reflects PropertiesRaw in.
    ///
    /// Raw-string + parsed-cache mirrors MeleeWeaponPart's
    /// OnHitEffectsRaw pattern: the string is the blueprint/save
    /// representation, the parse is cached and invalidated when the raw
    /// string changes.
    ///
    /// FlavorText is the §6.1 "hinted discovery" surface: examine text that
    /// names the properties in-fiction so the player can reason toward
    /// combos instead of brute-forcing them.
    /// </summary>
    public class ReagentPart : Part
    {
        public override string Name => "Reagent";

        /// <summary>E.g. "heat:2, volatile:1". Potency defaults to 1 when omitted.</summary>
        public string PropertiesRaw = "";

        /// <summary>Examine hint naming the properties in-fiction (§6.1 hinted discovery).</summary>
        public string FlavorText = "";

        private List<BrewPropertyAmount> _cachedProperties;
        private string _cachedRawSnapshot;

        /// <summary>The reagent's own verb: press it into a flask as the proofing of the element it carries (<see cref="ProofingPressActions"/>).</summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                ProofingPressActions.AddActions(actor, ParentEntity, e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone, e.GetParameter<InventoryActionList>("Actions"));
                return true;
            }
            if (e.ID == "InventoryAction" && ProofingPressActions.IsCommand(e.GetStringParameter("Command")))
            {
                var actor = e.GetParameter<Entity>("Actor");
                if (ProofingPressActions.TryAct(actor, ParentEntity, e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone, e.GetStringParameter("Command"), e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction")))
                { e.Handled = true; return false; }
            }
            return true;
        }

        public IReadOnlyList<BrewPropertyAmount> GetProperties()
        {
            if (_cachedProperties == null || !string.Equals(_cachedRawSnapshot, PropertiesRaw))
            {
                _cachedProperties = BrewPropertyAmount.ParseList(PropertiesRaw, lowerCaseNames: true);
                _cachedRawSnapshot = PropertiesRaw;
            }

            return _cachedProperties;
        }
    }
}
