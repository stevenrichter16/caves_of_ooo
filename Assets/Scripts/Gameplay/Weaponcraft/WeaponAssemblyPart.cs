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
    public class WeaponAssemblyPart : Part, IInventoryActionReceiptOwner, IDescribeUse, IDescribeUseRows
    {
        public override string Name => "WeaponAssembly";

        public string BladeBlueprint = "";
        public string HaftBlueprint = "";
        public string BindingBlueprint = "";
        public bool OwnsInventoryReceipt(string command)=>ParentEntity?.GetPart<WeaponAssemblyPart>()==this && FieldBindingActions.IsCommand(command);
        public override bool HandleEvent(GameEvent e)=>ParentEntity?.GetPart<WeaponAssemblyPart>()!=this || FieldBindingActions.Handle(ParentEntity,e);
        public int Order=>40;
        public void AddUseRows(Entity actor,Zone zone,InventoryActionList rows)=>FieldBindingActions.AddUseRows(ParentEntity,actor,zone,rows);
        public void DescribeUse(DescribeContext context,UseGuide guide)
        {
            guide.How.Add("In your pack, replace an assembled weapon's binding with a carried binding and a sound cutter.");
            guide.Limits.Add("One action; old binding destroyed. Carry a single untempered weapon, unworn. Head and haft stay. A forge refit returns its displaced component and can replace other slots.");
        }

        public override void OnAfterLoad(SaveReader reader)
        {
            SteelBladeFamilyCompatibility.RestoreAssembly(this);
        }

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
