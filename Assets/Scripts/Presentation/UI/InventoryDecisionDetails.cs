using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>Explicit inspection snapshots. These queries never execute item actions,
    /// apply effects, advance a clock or predict callback approval. Callers revalidate on use.</summary>
    public static class InventoryDecisionDetails
    {
        internal static bool Owned(Entity actor, Entity item)
        {
            var pack = actor?.GetPart<InventoryPart>(); var p = item?.GetPart<PhysicsPart>();
            if (pack == null || pack.ParentEntity != actor || p == null || p.ParentEntity != item
                || item.SpatialZone != null || (item.GetPart<StackerPart>()?.StackCount ?? 1) <= 0) return false;
            return (pack.Objects.Contains(item) && p.InInventory == actor && p.Equipped == null)
                || (pack.EquippedItems.Values.Contains(item) && p.Equipped == actor && p.InInventory == null);
        }
        internal static bool VisibleOwner(Zone zone, Entity item)
        {
            if (item == null || zone == null) return false;
            var cell = zone.GetEntityCell(item); var p = item?.GetPart<PhysicsPart>(); var r = item?.GetPart<RenderPart>();
            return cell != null && cell.ParentZone == zone && cell.IsVisible && cell.Explored && cell.Objects.Contains(item)
                && item.SpatialZone == zone && p != null && p.ParentEntity == item && p.InInventory == null && p.Equipped == null
                && r != null && r.ParentEntity == item && r.Visible && (item.GetPart<StackerPart>()?.StackCount ?? 1) > 0;
        }
        static bool ActorHere(Entity actor, Zone zone) => actor != null && zone != null && actor.SpatialZone == zone
            && zone.GetEntityCell(actor)?.Objects.Contains(actor) == true;
        static bool Near(Entity actor, Zone zone, Entity item)
        {
            if (!ActorHere(actor, zone) || !VisibleOwner(zone, item)) return false;
            var a = zone.GetEntityCell(actor); var b = zone.GetEntityCell(item);
            return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)) <= 1;
        }
        static string Units(Entity item) { int count = item?.GetPart<StackerPart>()?.StackCount ?? 1; return count + (count == 1 ? " unit" : " units"); }
        static string Weight(Entity item) => Units(item) + ". Weight per unit: " + HandlingService.GetWeight(item)
            + ". Total weight: " + InventoryPart.GetItemWeight(item) + ".";
        static string Signed(int value) => (value >= 0 ? "+" : "") + value;
        static string BaseItem(Entity item)
        {
            var examine = item.GetPart<ExaminablePart>();
            if (examine != null) return examine.BuildExamineLine();
            string text = item.GetDisplayName();
            if (ItemExamineService.TryDescribeDetails(item, out string details)) text += "\n\n" + details;
            return text;
        }
        public static string Item(Entity actor, Entity item)
        {
            if (!Owned(actor, item)) return null;
            var lines = new List<string> { BaseItem(item), "", Handling(actor, item) };
            if (item.HasPart<FoodPart>()) lines.Add(FoodContext(actor, item.GetPart<FoodPart>()));
            if (item.HasPart<TinkerItemPart>() || item.HasPart<MeleeWeaponPart>()) lines.Add(Disassembly(actor, item));
            return string.Join("\n", lines.Where(s => !string.IsNullOrEmpty(s)));
        }
        public static string Loot(Entity actor, Zone zone, Entity item, Entity sourceContainer)
        {
            if (!ActorHere(actor, zone) || item == null) return null;
            if (sourceContainer == null)
            {
                if (!VisibleOwner(zone, item) || zone.GetEntityCell(actor) != zone.GetEntityCell(item)) return null;
            }
            else
            {
                var container = sourceContainer.GetPart<ContainerPart>(); var physics = item.GetPart<PhysicsPart>();
                if (!Near(actor, zone, sourceContainer) || container == null || container.ParentEntity != sourceContainer || container.IsLocked
                    || !container.Contents.Contains(item) || physics?.InInventory != sourceContainer || physics.Equipped != null
                    || item.SpatialZone != null || item.GetPart<RenderPart>()?.Visible != true || (item.GetPart<StackerPart>()?.StackCount ?? 1) <= 0) return null;
            }
            return BaseItem(item) + "\n\n" + Weight(item) + "\n" + HandlingFacts(actor, item);
        }
        public static string Handling(Entity actor, Entity item)
        {
            if (!Owned(actor, item) && !VisibleOwner(actor?.SpatialZone, item)) return null;
            return Weight(item) + "\n" + HandlingFacts(actor, item);
        }
        static string HandlingFacts(Entity actor, Entity item)
        {
            var lines = new List<string>();
            bool lift = HandlingService.CanLift(actor, item, out var liftReason);
            lines.Add("Lift Strength: " + HandlingService.GetLiftStrengthRequirement(item) + "; yours: " + actor.GetStatValue("Strength") + ".");
            lines.Add(lift ? "You can carry this item." : "This item cannot be carried now. " + liftReason);
            lines.Add("Grip: " + (HandlingService.GetGripType(item) == GripType.TwoHand ? "two hands." : "one hand."));
            if (HandlingService.CanThrow(actor, item, out var throwReason))
                lines.Add("Throw range: " + HandlingService.GetThrowRange(actor, item) + " cells. Throw Strength: " + HandlingService.GetThrowStrengthRequirement(item) + ".");
            else lines.Add(throwReason);
            if (DragRules.CanDrag(actor, item) == DragVerdict.Ok) lines.Add("You can haul this load from beside it; use its interaction menu.");
            var pack = actor.GetPart<InventoryPart>();
            if (pack != null)
            {
                long after = (long)pack.GetCarriedWeight() + (Owned(actor, item) ? 0 : InventoryPart.GetItemWeight(item));
                int burden = pack.GetMaxCarryWeight();
                lines.Add("Pack weight: " + pack.GetCarriedWeight() + "; with this item: " + after + ". Burden threshold: " + (burden < 0 ? "unlimited" : burden.ToString()) + ".");
                if (burden >= 0 && after > burden) lines.Add("This weight would leave you overburdened.");
                if (pack.MaxWeight >= 0 && after > pack.MaxWeight) lines.Add("Above this inventory's hard capacity (" + pack.MaxWeight + ").");
            }
            return string.Join("\n", lines.Where(s => !string.IsNullOrEmpty(s)));
        }
        public static string Container(Entity actor, Zone zone, Entity containerOwner, Entity item)
        {
            if (!Owned(actor, item) || !Near(actor, zone, containerOwner)) return null;
            var box = containerOwner.GetPart<ContainerPart>();
            if (box == null || box.ParentEntity != containerOwner) return null;
            if (box.IsLocked) return containerOwner.GetDisplayName() + " is locked. Contents are unavailable.";
            string header = containerOwner.GetDisplayName() + " - entries: " + box.Contents.Count + "/" + (box.MaxItems < 0 ? "unlimited" : box.MaxItems.ToString()) + ".\n" + Weight(item);
            bool full = box.MaxItems >= 0 && box.Contents.Count >= box.MaxItems;
            if (!full) return header + "\nThere is room for this item. Transfer rechecks current state.";
            var stack = item.GetPart<StackerPart>(); long remaining = stack?.StackCount ?? 1;
            if (stack != null)
                foreach (var current in box.Contents)
                {
                    var existing = current?.GetPart<StackerPart>();
                    if (existing != null && existing.CanStackWith(item)) remaining -= Math.Max(0, existing.MaxStack - existing.StackCount);
                }
            return header + (remaining <= 0 ? "\nThe entire stack can merge without a new entry." : "\nContainer is full; this stack needs a new entry.");
        }
        public static string Trade(Entity actor, Entity trader, Entity item, bool buying)
        {
            if (actor == null || trader == null || !Owned(buying ? trader : actor, item)) return null;
            double perf = TradeSystem.GetTradePerformance(actor);
            int price = buying ? TradeSystem.GetBuyPrice(item, perf, trader) : TradeSystem.GetSellPrice(item, perf, trader);
            long after = (long)TradeSystem.GetDrams(actor) + (buying ? -price : price);
            var pack = actor.GetPart<InventoryPart>();
            long weight = (pack?.GetCarriedWeight() ?? 0L) + (buying ? 1L : -1L) * InventoryPart.GetItemWeight(item);
            return BaseItem(item) + "\n\n" + (buying ? "Buy " : "Sell ") + "the entire stack: " + Units(item) + ".\n"
                + Weight(item) + "\nPrice: " + price + " drams.\nDrams now: " + TradeSystem.GetDrams(actor) + ". Drams after: " + after + ".\n"
                + "Pack weight after: " + weight + "."
                + (after < 0 ? "\nYou cannot afford this purchase." : "")
                + (pack != null && pack.GetMaxCarryWeight() >= 0 && weight > pack.GetMaxCarryWeight() ? "\nThis leaves you overburdened." : "")
                + (pack != null && pack.MaxWeight >= 0 && weight > pack.MaxWeight ? "\nAbove your inventory's hard capacity." : "")
                + "\nCurrent quote only; the trade rechecks price, stock and permission.";
        }
        public static string Haul(Entity actor, Zone zone)
        {
            var grip = actor?.GetPart<DragPart>(); var load = grip?.Dragged;
            if (!ActorHere(actor, zone) || grip?.ParentEntity != actor || !VisibleOwner(zone, load)
                || load.GetPart<DraggedPart>()?.Dragger != actor || load.GetPart<DraggedPart>()?.ParentEntity != load) return null;
            return "Hauling " + load.GetDisplayName() + " (Speed -" + grip.AppliedPenalty + "). C: load menu / let go.";
        }
        public static string Status(Entity actor)
        {
            if (actor == null) return null;
            var effects = actor.GetPart<StatusEffectsPart>()?.GetAllEffects();
            return "Current effects\n\n" + (effects == null || effects.Count == 0 ? "No current effects." : string.Join("\n\n", effects.Where(e => e != null).Select(EffectDescriber.Describe)));
        }
        public static string Surface(Zone zone, Cell cell)
        {
            if (zone == null || cell == null || cell.ParentZone != zone || zone.GetCell(cell.X, cell.Y) != cell || !cell.IsVisible || !cell.Explored) return null;
            var state = zone.TileState.Get(cell.X, cell.Y); if (state == null) return null;
            var lines = new List<string>(); string ground = CellStatusReadout.GroundLine(zone, cell); if (ground != null) lines.Add(ground);
            var seen = new HashSet<string>();
            foreach (var layer in state.Coatings.Concat(state.Residues))
            {
                if (!seen.Add(layer.Id)) continue;
                var def = LiquidRegistry.IsInitialized ? LiquidRegistry.Get(layer.Id) : null; if (def == null) continue;
                var facts = new List<string>();
                if (def.Slippery) facts.Add(Math.Max(0, Math.Min(100, def.SlipChance)) + "% slip chance on a step; a clear neighboring landing is still required");
                if (def.Conductivity >= LiquidCoveredEffect.CONDUCTIVITY_AMPLIFY_THRESHOLD) facts.Add("conducts and amplifies electrical damage while coated");
                if (def.Combustibility >= LiquidCoveredEffect.COMBUSTIBLE_AMPLIFY_THRESHOLD) facts.Add("makes fire more dangerous while coated");
                if (def.FireDampen > 0) facts.Add("dampens fire damage by " + def.FireDampen + "% while coated");
                if (def.PerTurnDamage?.Amount > 0) facts.Add(def.PerTurnDamage.Amount + " " + def.PerTurnDamage.Type + " damage per turn while the coat persists");
                if (def.Sticky) facts.Add("sticky on contact");
                foreach (var mod in (def.StatModifiers ?? new List<LiquidStatMod>()).Concat(def.ResistanceModifiers ?? new List<LiquidStatMod>()))
                    if (mod.Delta != 0) facts.Add(mod.Stat + " " + Signed(mod.Delta) + " while coated");
                if (!string.IsNullOrEmpty(def.ImmuneElement)) facts.Add("blocks " + def.ImmuneElement + " damage while coated");
                if (facts.Count > 0) lines.Add((def.DisplayName ?? def.Id) + ": " + string.Join("; ", facts) + ".");
            }
            return lines.Count == 0 ? null : string.Join("\n\n", lines);
        }
        public static string Forge(ForgePreview preview)
        {
            if (!preview.IsComplete) return "Forge result\n\nPick " + preview.Missing + " to finish the weapon.";
            var lines = new List<string> { preview.DisplayName, "Family: " + preview.FamilyDisplayName, "Damage: " + preview.BaseDamage + " per penetration.",
                "Penetration: " + Signed(preview.PenBonus), "Hit: " + Signed(preview.HitBonus), "Strength cap: " + (preview.MaxStrengthBonus < 0 ? "legacy uncapped" : preview.MaxStrengthBonus.ToString()), "Attributes: " + preview.Attributes };
            foreach (var spec in OnHitEffectSpec.Parse(preview.OnHitEffectsRaw))
            {
                var effect = OnHitEffectFactory.Create(spec, null, new System.Random(0));
                lines.Add("On damaging hit (target may resist), " + Math.Max(0, Math.Min(100, spec.ChancePercent)) + "%: "
                    + (effect == null ? "unknown effect " + spec.EffectName : EffectDescriber.Describe(effect)));
            }
            return string.Join("\n", lines);
        }
        public static string Brew(BrewPreview preview)
        {
            if (!preview.IsValid) return "Brew result\n\n" + preview.Reason;
            var lines = new List<string> { preview.DisplayName, "Form: " + preview.Form };
            foreach (var value in preview.Effects ?? Array.Empty<BrewPropertyAmount>())
            {
                if (string.Equals(value.Property, BrewingService.HealingEffectName, StringComparison.OrdinalIgnoreCase))
                    lines.Add("Heals " + Math.Max(1, value.Potency) + "d4 HP immediately.");
                else
                {
                    var effect = TonicEffectFactory.Create(value.Property, 0, "", value.Potency, null);
                    lines.Add(effect == null ? "Unknown effect: " + value.Property : EffectDescriber.Describe(effect));
                }
            }
            lines.Add(preview.Form == "Throwable" ? "Shatters over the target and neighboring cells; companions can be affected."
                : preview.Form == "Coating" ? "Quench a weapon at a forge." : preview.Form == "Food" ? "Eaten when used." : "Used as a tonic; the produced item's details describe delivery.");
            return string.Join("\n", lines);
        }
        public static string Modification(Entity item, TinkerRecipe recipe)
        {
            if (item == null || recipe == null) return null;
            return item.GetDisplayName() + "\n\n" + recipe.Description + "\nCost: " + (recipe.Cost ?? "-") + "."
                + (string.IsNullOrWhiteSpace(recipe.Ingredient) ? "" : " Ingredient: " + recipe.Ingredient + ".")
                + "\n" + TinkerModificationPreview.Describe(item, recipe.Blueprint);
        }
        public static string Disassembly(Entity actor, Entity item)
        {
            if (!Owned(actor, item) || actor.GetPart<BitLockerPart>() == null) return "Disassembly unavailable: no current owned item or tinkering access.";
            return TinkeringService.TryDescribeDisassemblyYield(item, out string bits, out string reason)
                ? "Disassemble one unit for <" + bits + ">. The item is destroyed; recovery is deliberately partial."
                : "Disassembly unavailable: " + reason;
        }
        public static string Food(Entity actor, Entity item)
        {
            var food = item?.GetPart<FoodPart>(); if (food == null || !Owned(actor, item)) return null;
            return item.GetDisplayName() + "\nEat one unit."
                + (string.IsNullOrWhiteSpace(food.Healing) ? "" : " Heals " + food.Healing + " HP.")
                + "\n" + FoodPart.DescribeMeal(food) + "\n" + FoodContext(actor, food);
        }
        static string FoodContext(Entity actor, FoodPart food)
        {
            var hp = actor.GetStat("Hitpoints"); string text = hp == null ? "" : "HP missing: " + Math.Max(0, hp.Max - hp.Value) + ". Healing rolls are capped at your maximum HP.";
            if (!PreparedMealEffect.Valid(food.MealStat, food.MealBonus, food.MealDuration)) return text;
            var current = actor.GetPart<StatusEffectsPart>()?.GetAllEffects().OfType<PreparedMealEffect>().FirstOrDefault();
            if (current != null) text += "\nCurrent meal: " + current.Describe() + "\n"
                + (current.StatName == food.MealStat && current.Bonus == food.MealBonus ? "This refreshes that meal's duration." : "This replaces that meal's benefit.");
            return text;
        }
    }

    public static class InventoryBrowseQuery
    {
        /// <summary>Literal display-name filter. Input order and owner state remain unchanged.</summary>
        public static List<Entity> Filter(IEnumerable<Entity> items, string query)
        {
            var result = new List<Entity>(); if (items == null) return result;
            foreach (var item in items) if (item != null && (string.IsNullOrEmpty(query)
                || item.GetDisplayName().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)) result.Add(item);
            return result;
        }
    }
}
