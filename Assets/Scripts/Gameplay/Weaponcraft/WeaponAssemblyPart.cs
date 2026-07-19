namespace CavesOfOoo.Core
{
    /// <summary>
    /// Records which components a forged weapon was assembled from, so the
    /// weapon can be RE-FORGED later — swap one component, keep the others,
    /// recompute stats. This is the RPG-identity heart of §7.1 Layer 1: a
    /// favorite weapon evolves with the character instead of being replaced.
    ///
    /// Stores blueprint NAMES (components are stateless items), so the save
    /// layer round-trips this via plain public string fields and a reforge
    /// can re-create the displaced component from its blueprint.
    /// </summary>
    public class WeaponAssemblyPart : Part
    {
        public override string Name => "WeaponAssembly";

        public string BladeBlueprint = "";
        public string HaftBlueprint = "";
        public string BindingBlueprint = "";

        public string GetBlueprintForSlot(string slot)
        {
            if (WeaponForgingService.IsBladeSlot(slot)) return BladeBlueprint;
            if (WeaponForgingService.IsHaftSlot(slot)) return HaftBlueprint;
            if (WeaponForgingService.IsBindingSlot(slot)) return BindingBlueprint;
            return "";
        }

        public void SetBlueprintForSlot(string slot, string blueprintName)
        {
            if (WeaponForgingService.IsBladeSlot(slot)) BladeBlueprint = blueprintName;
            else if (WeaponForgingService.IsHaftSlot(slot)) HaftBlueprint = blueprintName;
            else if (WeaponForgingService.IsBindingSlot(slot)) BindingBlueprint = blueprintName;
        }
    }
}
