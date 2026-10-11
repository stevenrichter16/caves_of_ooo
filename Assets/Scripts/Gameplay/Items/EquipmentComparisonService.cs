using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Inspection-only equipment facts and real planner choices. Never executes
    /// an equip command, invokes item events or projects final combat statistics.
    /// The returned text is a snapshot; actual equip must validate again.
    /// </summary>
    public static class EquipmentComparisonService
    {
        public static bool TryDescribe(Entity actor, Entity candidate, out string text, out string reason)
        {
            text = null;
            reason = "Equipment comparison is unavailable.";
            if (actor == null || CombatSystem.IsDeathHandled(actor)
                || (actor.GetStat("Hitpoints") != null && actor.GetStatValue("Hitpoints") <= 0))
                return false;
            var inventory = actor.GetPart<InventoryPart>();
            var body = actor.GetPart<Body>();
            if (inventory == null || inventory.ParentEntity != actor || inventory.Objects == null
                || inventory.EquippedItems == null || (body != null && body.ParentEntity != actor))
                return false;
            var parts = body == null ? new List<BodyPart>() : body.GetParts();
            if (candidate?.GetPart<EquippablePart>() == null || !Owned(actor, inventory, parts, candidate))
            {
                reason = "That equipment is no longer available in your inventory.";
                return false;
            }
            if (!CurrentEquipment(actor, inventory, body, parts))
            {
                reason = "Current equipment ownership is inconsistent; comparison is unavailable.";
                return false;
            }

            var equipmentBefore = inventory.EquippedItems.ToArray();
            var slotsBefore = parts.Select(p => new { Part = p, Item = p._Equipped,
                Natural = p._DefaultBehavior, First = p.FirstSlotForEquipped,
                FirstNatural = p.FirstSlotForDefaultBehavior, p.Abstract, p.TargetWeight }).ToArray();
            int quantityBefore = candidate.GetPart<StackerPart>()?.StackCount ?? 1;
            var lines = new List<string> { "Equipment comparison", "", "Candidate: " + candidate.GetDisplayName() };
            AddFacts(candidate, lines);
            lines.Add("");
            var facts = new List<Entity>();
            if (candidate.GetPart<PhysicsPart>().Equipped == actor)
            {
                var equippedParts = parts.Where(p => p._Equipped == candidate).ToList();
                lines.Add("Currently equipped: " + (equippedParts.Count > 0 ? PartNames(equippedParts)
                    : string.Join(", ", inventory.EquippedItems.Where(p => p.Value == candidate).Select(p => p.Key))) + ".");
                AddWornArmorCoverage(candidate, equippedParts, lines);
            }
            else
            {
                var planner = new EquipPlanner();
                var automatic = planner.Build(actor, candidate);
                if (!automatic.IsValid)
                    lines.Add((body == null ? "Slot comparison unavailable: " : "Unavailable: ") + automatic.FailureReason);
                else
                {
                    var choices = new HashSet<string>();
                    AddChoice("Auto equip", automatic, choices, facts, lines);
                    foreach (var part in parts)
                    {
                        if (part.Abstract || !automatic.SlotTypes.Contains(part.Type)) continue;
                        var targeted = planner.Build(actor, candidate, part);
                        if (targeted.IsValid)
                            AddChoice("Manual equip", targeted, choices, facts, lines);
                    }
                }
            }
            foreach (var current in facts)
            {
                lines.Add("");
                lines.Add("Current: " + current.GetDisplayName());
                AddFacts(current, lines);
            }
            lines.Add("");
            lines.Add("Item contributions only. Other effects are not totaled; targets, skills and conditions can change results.");
            lines.Add("Inspection snapshot. Equipping rechecks current slots and restrictions.");
            // Enhancement descriptions are extensible callbacks. Do not publish
            // an ownership/slot snapshot they made stale while formatting it.
            var currentParts = body == null ? new List<BodyPart>() : body.GetParts();
            if (actor.GetPart<InventoryPart>() != inventory || actor.GetPart<Body>() != body
                || inventory.ParentEntity != actor || inventory.Objects == null || inventory.EquippedItems == null
                || (body != null && body.ParentEntity != actor) || candidate.GetPart<EquippablePart>() == null
                || CombatSystem.IsDeathHandled(actor)
                || (actor.GetStat("Hitpoints") != null && actor.GetStatValue("Hitpoints") <= 0)
                || !Owned(actor, inventory, currentParts, candidate)
                || !CurrentEquipment(actor, inventory, body, currentParts)
                || (candidate.GetPart<StackerPart>()?.StackCount ?? 1) != quantityBefore
                || !equipmentBefore.SequenceEqual(inventory.EquippedItems)
                || !slotsBefore.SequenceEqual(currentParts.Select(p => new { Part = p, Item = p._Equipped,
                    Natural = p._DefaultBehavior, First = p.FirstSlotForEquipped,
                    FirstNatural = p.FirstSlotForDefaultBehavior, p.Abstract, p.TargetWeight })))
            {
                reason = "Equipment changed while reading. Inspect it again.";
                return false;
            }
            text = string.Join("\n", lines);
            reason = null;
            return true;
        }

        private static bool ValidParts(Entity item)
        {
            if (item == null || CombatSystem.IsDeathHandled(item)) return false;
            foreach (var part in item.Parts)
                if (part == null || part.ParentEntity != item) return false;
            return item.GetPart<StackerPart>() == null || item.GetPart<StackerPart>().StackCount > 0;
        }

        private static bool Owned(Entity actor, InventoryPart inventory, List<BodyPart> parts, Entity item)
        {
            if (!ValidParts(item)) return false;
            var physics = item.GetPart<PhysicsPart>();
            if (physics == null || item.SpatialZone != null) return false;
            bool carried = inventory.Objects.Contains(item);
            bool cached = inventory.EquippedItems.Values.Contains(item);
            bool onBody = parts.Any(p => p._Equipped == item);
            if (carried)
                return !cached && !onBody && physics.InInventory == actor && physics.Equipped == null;
            if (!cached || physics.Equipped != actor || physics.InInventory != null) return false;
            return actor.GetPart<Body>() == null || onBody;
        }

        private static bool CurrentEquipment(Entity actor, InventoryPart inventory, Body body, List<BodyPart> parts)
        {
            foreach (var part in parts)
            {
                if (part == null || part.ParentBody != body) return false;
                if (part._Equipped != null)
                {
                    if (!Owned(actor, inventory, parts, part._Equipped)
                        || !inventory.EquippedItems.TryGetValue(part.ID.ToString(), out var cached)
                        || cached != part._Equipped) return false;
                }
                var natural = ActiveNaturalAttack(part);
                if (natural != null && (!ValidParts(natural)
                    || natural.SpatialZone != null || inventory.Contains(natural)
                    || natural.GetPart<PhysicsPart>()?.InInventory != null
                    || natural.GetPart<PhysicsPart>()?.Equipped != null)) return false;
            }
            foreach (var pair in inventory.EquippedItems)
            {
                if (!Owned(actor, inventory, parts, pair.Value)) return false;
                if (body != null && !parts.Any(p => p.ID.ToString() == pair.Key && p._Equipped == pair.Value))
                    return false;
            }
            return true;
        }

        // Mirrors only the current natural-hand admission in GatherMeleeWeapons.
        // A supporting hand of a two-slot item cannot supply a natural attack.
        private static Entity ActiveNaturalAttack(BodyPart part)
        {
            if (part.Type != "Hand" || !part.FirstSlotForDefaultBehavior
                || part._DefaultBehavior?.GetPart<MeleeWeaponPart>() == null) return null;
            if (part._Equipped != null && (!part.FirstSlotForEquipped
                || part._Equipped.GetPart<MeleeWeaponPart>() != null)) return null;
            return part._DefaultBehavior;
        }

        private static void AddChoice(string label, EquipPlan plan, HashSet<string> choices,
            List<Entity> facts, List<string> lines)
        {
            // Order of claimed parts matters for a multi-hand weapon's first attack slot.
            string key = string.Join(",", plan.ClaimedParts.Select(p => p.ID.ToString()));
            if (!choices.Add(key)) return;
            lines.Add(label + ": " + PartNames(plan.ClaimedParts) + ".");
            foreach (var part in plan.ClaimedParts)
            {
                string current = part._Equipped == null ? "empty" : part._Equipped.GetDisplayName();
                var natural = ActiveNaturalAttack(part);
                if (natural != null)
                {
                    current += "; natural attack: " + natural.GetDisplayName();
                    AddDistinct(facts, natural);
                }
                lines.Add("  " + part.GetDisplayName() + " now: " + current + ".");
                if (part._Equipped != null) AddDistinct(facts, part._Equipped);
            }
            foreach (var group in plan.Displacements.GroupBy(d => d.Item))
            {
                lines.Add("  Displaced: " + group.Key.GetDisplayName() + " ("
                    + PartNames(group.Select(d => d.BodyPart)) + ").");
                AddDistinct(facts, group.Key);
            }
            if (plan.Displacements.Count == 0) lines.Add("  No equipped items displaced.");
            AddArmorChanges(plan, lines);
            var deltas = new SortedDictionary<string, long>(StringComparer.Ordinal);
            AddContributions(plan.Actor, plan.Item, 1, deltas);
            foreach (var old in plan.Displacements.Select(d => d.Item).Distinct()) AddContributions(plan.Actor, old, -1, deltas);
            lines.Add("  Equipment contributions (other effects not totaled):");
            bool changed = false;
            foreach (var delta in deltas) if (delta.Value != 0)
            { lines.Add("  " + delta.Key + " change: " + (delta.Value >= 0 ? "+" : "") + delta.Value); changed = true; }
            if (!changed) lines.Add("  No numeric contribution change.");
        }

        private static bool Targetable(BodyPart part) => !part.Abstract && part.TargetWeight > 0;

        private static void AddWornArmorCoverage(Entity item, IEnumerable<BodyPart> parts, List<string> lines)
        {
            var armor = item.GetPart<ArmorPart>();
            if (armor == null || armor.AV == 0) return;
            foreach (var part in parts)
                lines.Add("  " + part.GetDisplayName() + (Targetable(part)
                    ? ": worn AV " + Signed(armor.AV) + " applies only when this part is struck."
                    : ": not a normal hit location; listed item AV does not protect other parts."));
        }

        private static void AddArmorChanges(EquipPlan plan, List<string> lines)
        {
            // A displaced multi-slot item also leaves any released slots that
            // the new item does not claim. AV belongs to each physical location;
            // it is neither an item-total nor a projection of natural armor.
            foreach (var part in plan.ClaimedParts.Concat(plan.Displacements.Select(d => d.BodyPart)).Distinct())
            {
                long before = part._Equipped?.GetPart<ArmorPart>()?.AV ?? 0;
                long after = plan.ClaimedParts.Contains(part) ? plan.Item.GetPart<ArmorPart>()?.AV ?? 0 : 0;
                if (before == 0 && after == 0) continue;
                if (!Targetable(part))
                    lines.Add("  " + part.GetDisplayName() + ": not a normal hit location; listed item AV does not protect other parts.");
                else
                    lines.Add("  " + part.GetDisplayName() + ": AV " + before + " -> " + after
                        + " (change: " + Signed(after - before) + "). Equipment armor at this hit location only.");
            }
        }

        private static string Signed(long value) => (value >= 0 ? "+" : "") + value.ToString(CultureInfo.InvariantCulture);

        private static void AddContributions(Entity actor, Entity item, int sign, IDictionary<string, long> values)
        {
            void Add(string stat, long amount) { values.TryGetValue(stat, out long prior); values[stat] = prior + sign * amount; }
            var armor = item.GetPart<ArmorPart>();
            if (armor != null) { Add("DV", armor.DV); if (actor.GetStat("Speed") != null) Add("Speed", -(long)armor.SpeedPenalty); }
            string raw = item.GetPart<EquippablePart>()?.EquipBonuses;
            foreach (string pair in (raw ?? "").Split(','))
            {
                string trimmed = pair.Trim(); int colon = trimmed.IndexOf(':');
                if (colon < 0 || !int.TryParse(trimmed.Substring(colon + 1), out int amount)) continue;
                string stat = trimmed.Substring(0, colon);
                if (actor.GetStat(stat) != null || stat == "HeatResistance" || stat == "ColdResistance" || stat == "ElectricResistance" || stat == "AcidResistance") Add(stat, amount);
            }
        }

        private static string PartNames(IEnumerable<BodyPart> parts)
        {
            return string.Join(", ", parts.Select(p => p.GetDisplayName()));
        }

        private static void AddDistinct(List<Entity> items, Entity item)
        {
            if (!items.Contains(item)) items.Add(item);
        }

        private static void AddFacts(Entity item, List<string> lines)
        {
            if (ItemExamineService.TryDescribeEquipmentDetails(item, out string details)) lines.Add(details);
            lines.Add("Weight per item: " + HandlingService.GetWeight(item).ToString(CultureInfo.InvariantCulture) + ".");
            foreach (var part in item.Parts)
                if (part is IItemEnhancement enhancement)
                    lines.Add("Conditional enhancement: " + enhancement.GetEffectDescription());
        }
    }
}
