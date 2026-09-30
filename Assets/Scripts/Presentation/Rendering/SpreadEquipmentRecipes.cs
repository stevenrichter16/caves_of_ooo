using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
namespace CavesOfOoo.Rendering
{
    /// <summary>Read-only exact equipped-item identity. The caller separately
    /// validates the current receiving biome, live actor and supported rig.</summary>
    public sealed class SpreadEquipmentRecipe
    {
        public string ModelId { get; }
        public string Slot { get; }
        public int Pieces { get; }
        public string AttachmentKey { get; }
        internal SpreadEquipmentRecipe(string modelId, string slot, string attachmentKey)
        { ModelId = modelId; Slot = slot; Pieces = slot == "Feet" || slot == "Handwear" ? 2 : 1; AttachmentKey = attachmentKey; }
    }
    public static class SpreadEquipmentRecipes
    {
        private static readonly Dictionary<string, string> Slots = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "Dagger", "Hand" },
            { "CurationSaltRake", "Hand" },
            { "ForgedWeapon", "Hand" },
            { "LeatherArmor", "Body" },
            { "ChainMail", "Body" },
            { "LongSword", "Hand" },
            { "Battleaxe", "Hand" },
            { "Greatsword", "Hand" },
            { "ShortSword", "Hand" },
            { "Mace", "Hand" },
            { "Spear", "Hand" },
            { "Hatchet", "Hand" },
            { "Claymore", "Hand" },
            { "Cudgel", "Hand" },
            { "Buckler", "Hand" },
            { "IronHelmet", "Head" },
            { "LeatherBoots", "Feet" },
            { "LeatherGloves", "Handwear" },
            { "LeatherCap", "Head" },
            { "IronshodBoots", "Feet" },
            { "WardedCloak", "Back" },
            { "IronBuckler", "Hand" },
            { "PlateArmor", "Body" },
            { "Cloak", "Back" },
            { "LoanerDagger", "Hand" },
            { "LoanerSpear", "Hand" },
            { "LoanerLongsword", "Hand" },
            { "Warhammer", "Hand" },
            { "ChoirSpine", "Hand" },
            { "OldWorldPipe", "Hand" },
            { "Sporeblade", "Hand" },
            { "FlamingSword", "Hand" },
            { "IceSword", "Hand" },
            { "CryoLance", "Hand" },
            { "EmberSpear", "Hand" },
            { "AcidicDagger", "Hand" },
            { "VenomDagger", "Hand" },
            { "ThunderHammer", "Hand" },
            { "EchoKnife", "Hand" },
            { "TemporalShard", "Hand" },
            { "SeveranceEdge", "Hand" },
            { "GlassblownStiletto", "Hand" },
            { "DissolutionMaul", "Hand" },
            { "FirstRootGlaive", "Hand" },
            { "PalimpsestBlade", "Hand" },
            { "BreacherCleaver", "Hand" },
            { "Torch", "Hand" },
            { "TemperedLongSword", "Hand" },
            { "CounterweightMaul", "Hand" },
            { "FineRingMail", "Body" },
            { "RivetedPlate", "Body" },
        };
        public static bool TryRecipe(Entity actor, Entity item, out SpreadEquipmentRecipe recipe)
        {
            recipe = null;
            if (actor == null || item == null || item.BlueprintName == null
                || !Slots.TryGetValue(item.BlueprintName, out string slot)
                || !SpreadPortableRecipes.TryRecipe(item, out string portable)) return false;
            var inventory = actor.GetPart<InventoryPart>();
            var body = actor.GetPart<Body>();
            var physics = item.GetPart<PhysicsPart>();
            var equippable = item.GetPart<EquippablePart>();
            var weapon = item.GetPart<MeleeWeaponPart>();
            var armor = item.GetPart<ArmorPart>();
            if (inventory == null || !ReferenceEquals(inventory.ParentEntity, actor)
                || body == null || !ReferenceEquals(body.ParentEntity, actor)
                || physics == null || !ReferenceEquals(physics.Equipped, actor) || physics.InInventory != null
                || inventory.Objects?.Contains(item) == true || inventory.EquippedItems == null
                || equippable == null || !ReferenceEquals(equippable.ParentEntity, item) || equippable.Slot != slot
                || (weapon != null && !ReferenceEquals(weapon.ParentEntity, item))
                || (armor != null && !ReferenceEquals(armor.ParentEntity, item))
                || (item.GetPart<StackerPart>()?.StackCount ?? 1) <= 0) return false;
            bool cached = false;
            foreach (var pair in inventory.EquippedItems)
                if (ReferenceEquals(pair.Value, item)) { cached = true; break; }
            if (!cached) return false;
            var first = inventory.FindEquippedBodyPart(item);
            if (first == null || first.Type != slot || !first.FirstSlotForEquipped
                || !ReferenceEquals(first.ParentBody, body) || !ReferenceEquals(first._Equipped, item)) return false;
            var parts = body.GetParts();
            if (slot == "Feet" || slot == "Handwear")
            {
                // One abstract root pair differs from several individually lateral
                // feet. A familiar imported rig cannot establish native anatomy.
                int matching = 0;
                foreach (var part in parts) if (part.Type == slot) matching++;
                if (matching != 1 || first.GetLaterality() != Laterality.NONE || !ReferenceEquals(first.ParentPart,body.GetBody())) return false;
            }
            string key = slot + ":" + first.GetLaterality();
            if (slot == "Handwear")
            {
                bool left = false, right = false;
                foreach (var part in parts)
                    if (part.Type == "Hand") { int side = part.GetLaterality(); left |= (side & Laterality.LEFT) != 0; right |= (side & Laterality.RIGHT) != 0; }
                key += ":" + (left ? "L" : "") + (right ? "R" : "");
            }
            recipe = new SpreadEquipmentRecipe(slot == "Hand" ? portable : "spread-worn-" + item.BlueprintName.ToLowerInvariant(), slot, key);
            return true;
        }
    }
}
