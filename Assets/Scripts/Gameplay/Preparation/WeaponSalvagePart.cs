using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    public sealed class WeaponSalvagePart : Part
    {
        public override string Name => "WeaponSalvage";
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var item = ParentEntity; var zone = PreparationActions.ZoneFor(e, actor);
            if (e.ID == "GetInventoryActions")
            {
                if (PreparationActions.Carried(actor, item) && item.HasPart<WeaponAssemblyPart>())
                    e.GetParameter<InventoryActionList>("Actions")?.AddAction("SalvageForgedWeapon", "forge: head+haft; lose binding/mods", "SalvageForgedWeapon", '\0', 17);
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != "SalvageForgedWeapon") return true;
            if (e.GetParameter<bool>("PreparationAttempted")) return true; e.SetParameter("PreparationAttempted", true);
            if (!Salvage(actor, zone, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true; return false;
        }
        bool Salvage(Entity actor, Zone zone, InventoryTransaction tx)
        {
            const string command = "SalvageForgedWeapon";
            var weapon = ParentEntity; var assembly = weapon.GetPart<WeaponAssemblyPart>(); var factory = ForgePart.Factory;
            if (weapon.BlueprintName != WeaponForgingService.ForgedWeaponBlueprintName || assembly == null || factory == null
                || !weapon.HasPart<MeleeWeaponPart>() || !ForgePart.IsNearForge(actor, zone)) return PreparationActions.Reject(actor, weapon, command, "weapon_or_station");
            string blade = assembly.BladeBlueprint, haft = assembly.HaftBlueprint, binding = assembly.BindingBlueprint;
            // Recorded component recipes are provenance, never arbitrary inventory output names.
            if (!ComponentBlueprint(factory, blade, "Blade") || !ComponentBlueprint(factory, haft, "Haft") || !ComponentBlueprint(factory, binding, "Binding"))
                return PreparationActions.Reject(actor, weapon, command, "invalid_assembly");
            return PreparationActions.Transform(actor, weapon, zone, factory, tx, command, 1, new[] { blade, haft },
                () => weapon.GetPart<WeaponSalvagePart>() == this && weapon.GetPart<WeaponAssemblyPart>() == assembly
                    && assembly.BladeBlueprint == blade && assembly.HaftBlueprint == haft && assembly.BindingBlueprint == binding
                    && ForgePart.Factory == factory && ForgePart.IsNearForge(actor, zone),
                (output, index) => output.GetPart<WeaponComponentPart>()?.Slot == (index == 0 ? "Blade" : "Haft"),
                "You reclaim the head and haft. The binding and improvements are spent.");
        }
        static bool ComponentBlueprint(CavesOfOoo.Data.EntityFactory factory, string blueprint, string slot)
        {
            if (string.IsNullOrEmpty(blueprint) || !factory.Blueprints.ContainsKey(blueprint)) return false;
            // Final physical products are checked again after all initializers; binding is discarded.
            var objectBlueprint = factory.Blueprints[blueprint];
            return objectBlueprint.Parts.TryGetValue("WeaponComponent", out var part)
                && part.TryGetValue("Slot", out string actualSlot) && actualSlot == slot;
        }
    }
}
