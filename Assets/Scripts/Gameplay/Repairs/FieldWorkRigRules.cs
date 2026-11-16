using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>One authored recoverable service, using existing saved repair,
    /// harvest and hauling state. This is not a general machine simulation.</summary>
    internal static class FieldWorkRigRules
    {
        internal const string Blueprint = "AbandonedFieldWorkRig", Recipe = "clay-field-work-rig", Tag = "RecoverableWorkRig";
        internal static bool IsRig(Entity e) => e != null && (e.BlueprintName == Blueprint || e.HasTag(Tag));
        internal static bool IsWorkshopCommand(string command) => command == "CraftKit" || command == "ForgeWeapon"
            || command == "ForgeWeaponBatch" || command == "ReforgeWeapon" || command == "QuenchWeapon";
        internal static bool Payload(Entity e, bool allowSpent = false)
        {
            if (e == null || e.BlueprintName != Blueprint || !e.HasTag(Tag) || e.HasTag("Creature") || e.HasTag("Terrain")
                || e.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP <= 0)
                || e.HasPart<SpatialFootprintPart>() || e.HasPart<StackerPart>() || e.HasPart<ContainerPart>() || e.HasPart<InventoryPart>()) return false;
            foreach (var type in new[] { typeof(PhysicsPart), typeof(ForgePart), typeof(RepairablePart), typeof(CompositionPart), typeof(HarvestablePart), typeof(HandlingPart) })
                if (e.Parts.Count(p => p != null && p.GetType() == type && p.ParentEntity == e) != 1
                    || e.Parts.Count(p => p != null && p.GetType() == type) != 1) return false;
            var p = e.GetPart<PhysicsPart>(); var h = e.GetPart<HandlingPart>(); var r = e.GetPart<RepairablePart>(); var salvage = e.GetPart<HarvestablePart>();
            return !p.Takeable && p.Solid && p.Weight == 120 && p.InInventory == null && p.Equipped == null
                && h.Weight == 120 && !h.Carryable && !h.Throwable && h.MinLiftStrength == 0 && h.BulkClass == "Massive"
                && r.RecipeId == Recipe && !r.PortableEquipment && RepairRecipeRegistry.Get(Recipe) != null
                && e.GetPart<CompositionPart>().Contains("Masonry") && e.GetPart<CompositionPart>().Contains("Wood")
                && salvage.YieldBlueprint == "SalvagedTimber" && salvage.YieldMin == 2 && salvage.YieldMax == 2 && salvage.YieldChance == 100
                && (allowSpent || !salvage.Harvested);
        }
        internal static bool Available(Entity actor, Entity rig, Zone zone, bool allowSpent = false, bool allowHauling = false)
        {
            if (actor == null || rig == null || zone == null) return false;
            var a = zone?.GetEntityCell(actor); var t = zone?.GetEntityCell(rig);
            return Payload(rig, allowSpent) && ForgeWorkReceipt.Current(zone) && actor != null && actor != rig && actor.ID != rig.ID
                && actor.SpatialZone == zone && rig.SpatialZone == zone && a != null && t != null
                && a.ParentZone == zone && t.ParentZone == zone && zone.GetCell(a.X,a.Y) == a && zone.GetCell(t.X,t.Y) == t
                && a.Objects.Contains(actor) && t.Objects.Contains(rig) && SpatialQuery.Distance(zone, actor, rig) <= 1
                && !CombatSystem.IsDeathHandled(actor) && actor.GetStatValue("Hitpoints") > 0
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true && (allowHauling || !rig.HasPart<DraggedPart>());
        }
        // Repair and dismantling both retain the exact authored source through
        // outer inventory callbacks. Dismantling expects its source to be detached.
        internal static bool BeginChange(Entity actor, Entity rig, Zone zone, InventoryTransaction tx, bool dismantle)
        {
            if (tx == null || !Available(actor, rig, zone)) return false;
            var source = zone.GetEntityCell(rig); var actorCell = zone.GetEntityCell(actor);
            var parts = rig.Parts.ToArray(); string id = rig.ID, actorId = actor.ID;
            var recipe = RepairRecipeRegistry.Get(Recipe);
            tx.BeforeCommit(() => rig.ID == id && actor.ID == actorId && rig.Parts.SequenceEqual(parts)
                && Payload(rig, dismantle) && RepairRecipeRegistry.Get(Recipe) == recipe && ForgeWorkReceipt.Current(zone)
                && zone.GetEntityCell(actor) == actorCell && actor.SpatialZone == zone && zone.GetCell(actorCell.X,actorCell.Y) == actorCell
                && !CombatSystem.IsDeathHandled(actor) && actor.GetStatValue("Hitpoints") > 0
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true && !rig.HasPart<DraggedPart>()
                && (dismantle ? rig.GetPart<HarvestablePart>().Harvested && rig.SpatialZone == null
                    && zone.GetEntityCell(rig) == null && !zone.GetReadOnlyEntities().Any(e => e.ID == id)
                    : rig.GetPart<RepairablePart>().Repaired && zone.GetEntityCell(rig) == source && Available(actor, rig, zone)));
            return true;
        }
        internal static bool ContentAvailable(EntityFactory factory)
        {
            if (factory == null || !factory.Blueprints.TryGetValue(Blueprint, out var bp) || bp == null
                || !bp.Tags.ContainsKey(Tag) || !bp.Parts.ContainsKey("Forge") || !bp.Parts.ContainsKey("Examinable")
                || !bp.Parts.ContainsKey("Render") || new[] { "Container", "Inventory", "Stacker", "SpatialFootprint" }.Any(bp.Parts.ContainsKey)
                || bp.Tags.ContainsKey("Creature") || bp.Tags.ContainsKey("Terrain") || RepairRecipeRegistry.Get(Recipe) is not RepairRecipe recipe
                || recipe.MaterialBlueprint != "FireClay" || recipe.Quantity != 2 || recipe.Composition != "Masonry") return false;
            bool Field(string part, string key, string value) => bp.Parts.TryGetValue(part, out var fields)
                && fields.TryGetValue(key, out string actual) && string.Equals(actual, value, StringComparison.OrdinalIgnoreCase);
            bool FalseOrMissing(string part,string key) => !bp.Parts.TryGetValue(part,out var f) || !f.TryGetValue(key,out var v) || string.Equals(v,"false",StringComparison.OrdinalIgnoreCase);
            return FalseOrMissing("Repairable","Repaired") && FalseOrMissing("Repairable","PortableEquipment") && FalseOrMissing("Harvestable","Harvested")
                && Field("Physics", "Solid", "true") && Field("Physics", "Takeable", "false") && Field("Physics", "Weight", "120")
                && Field("Handling", "Weight", "120") && Field("Handling", "Carryable", "false") && Field("Handling", "Throwable", "false")
                && Field("Handling", "MinLiftStrength", "0") && Field("Handling", "BulkClass", "Massive")
                && Field("Repairable", "RecipeId", Recipe) && Field("Composition", "MaterialsRaw", "Masonry,Wood")
                && Field("Harvestable", "YieldBlueprint", "SalvagedTimber") && Field("Harvestable", "YieldMin", "2")
                && Field("Harvestable", "YieldMax", "2") && Field("Harvestable", "YieldChance", "100");
        }
        internal static bool Workshop(Entity actor, Entity rig, Zone zone, string action, InventoryTransaction tx)
        {
            if (!Available(actor, rig, zone) || !rig.GetPart<RepairablePart>().Repaired || ForgePart.Factory == null
                || !ForgeWorkReceipt.Begin(actor, zone, tx, action, rig)) return Fail("The rig must be sound, released and within reach.");
            var marks = CraftingMarkPart.CollectMarked(actor); var context = new InventoryContext(actor, zone);
            bool Run(IInventoryCommand command)
            {
                var valid = command.Validate(context); if (!valid.IsValid) return Fail(valid.ErrorMessage);
                var result = command.Execute(context, tx); return result.Success || Fail(result.ErrorMessage);
            }
            bool Reforge() => marks.Weapons.Count == 1 && marks.Components.Count == 1
                ? Run(new ReforgeWeaponCommand(marks.Weapons[0], marks.Components[0], ForgePart.Factory, rig))
                : Fail("Pick one forged weapon and one replacement component.");
            bool Temper(Entity weapon) => marks.Coatings.Count == 1
                ? Run(new TemperWeaponCommand(weapon, marks.Coatings[0], rig)) : Fail("Pick one effectful brewed quench.");
            if (action == "ReforgeWeapon") return Reforge();
            if (action == "QuenchWeapon") return marks.Weapons.Count == 1 ? Temper(marks.Weapons[0]) : Fail("Pick one weapon to temper.");
            if (action == "CraftKit" && marks.Weapons.Count == 1 && marks.Components.Count == 1) return Reforge();
            if (action == "CraftKit" && marks.Weapons.Count == 1 && marks.Coatings.Count == 1) return Temper(marks.Weapons[0]);
            var blades = marks.Components.Where(e => e.GetPart<WeaponComponentPart>()?.Slot == "Blade").ToArray();
            var hafts = marks.Components.Where(e => e.GetPart<WeaponComponentPart>()?.Slot == "Haft").ToArray();
            var bindings = marks.Components.Where(e => e.GetPart<WeaponComponentPart>()?.Slot == "Binding").ToArray();
            if (marks.Components.Count != 3 || blades.Length != 1 || hafts.Length != 1 || bindings.Length != 1 || marks.Coatings.Count > 1)
                return Fail("Pick one head, one haft and one binding; optionally add one brewed quench.");
            int count = action == "ForgeWeaponBatch" ? Math.Max(1, WeaponForgingService.GetMaxBatchCount(blades[0], hafts[0], bindings[0])) : 1;
            var forge = new ForgeWeaponCommand(blades[0], hafts[0], bindings[0], ForgePart.Factory, count, rig);
            if (!Run(forge)) return false;
            return action != "CraftKit" || marks.Coatings.Count == 0 || Temper(forge.ForgedWeapons.Single());
        }
        static bool Fail(string message) { if (!string.IsNullOrEmpty(message)) MessageLog.Add(message); return false; }
    }
}
