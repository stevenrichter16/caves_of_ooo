using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A single, saved enhancement opportunity on the ordinary T4 piece
    /// in a deep reliquary. This does not enable player tinkering or change the
    /// enhancement API, old loot tables, item prices, or locked access.</summary>
    public static class FoundEquipmentEnhancements
    {
        public const int ChancePercent = 25;
        public const string RollMarker = "DeepFindEnhancementRolled";
        public const string SourceTable = "DeepReliquaryT4";
        private static readonly Type[] Allowed =
        {
            typeof(EnhancementSerrated), typeof(EnhancementPaleSalt),
            typeof(EnhancementChoirIron), typeof(EnhancementLacquered),
            typeof(EnhancementGlowQuartz)
        };

        /// <summary>Decorates only an exact item retained by its actual native
        /// locked-chest owner after stocking. Rejected contexts spend no RNG.
        /// A valid opportunity is marked before its chance roll, including a miss,
        /// and survives save/load. The optional percentage supports authored
        /// zero/full-rate controls without modifying global RNG or registry state.</summary>
        public static bool TryApply(Entity container, string tableName, Entity item,
            Random rng, int chancePercent = ChancePercent)
        {
            if (chancePercent < 0 || chancePercent > 100)
            {
                Reject(container, tableName, item, chancePercent, "invalid_chance");
                throw new ArgumentOutOfRangeException(nameof(chancePercent));
            }
            string refusal = ContextRefusal(container, tableName, item, rng);
            if (refusal != null) return Reject(container, tableName, item, chancePercent, refusal);

            EnhancementFactory.EnsureInitialized();
            var candidates = new List<string>(Allowed.Length);
            foreach (var type in Allowed)
            {
                if (!EnhancementFactory.TryGet(type.Name, out var registered) || registered != type) continue;
                var enhancement = EnhancementFactory.Create(type.Name, 1);
                if (enhancement != null && enhancement.Applicable(item)) candidates.Add(type.Name);
            }
            if (candidates.Count == 0) return Reject(container, tableName, item, chancePercent, "no_candidates");

            item.Properties[RollMarker] = "1";
            // Observe only the roll the gameplay already consumes. Zero/full
            // chance retain their original zero chance-roll behavior.
            int? roll = chancePercent > 0 && chancePercent < 100 ? rng.Next(100) : (int?)null;
            if (chancePercent == 0 || roll.HasValue && roll.Value >= chancePercent)
            {
                Record("FoundEquipmentEnhancementMissed", container, tableName, item, chancePercent,
                    chancePercent == 0 ? "zero_chance" : "chance_roll", roll);
                return false;
            }
            string selected = candidates[rng.Next(candidates.Count)];
            bool applied = ItemEnhancing.Apply(item, selected, 1);
            Record(applied ? "FoundEquipmentEnhanced" : "FoundEquipmentEnhancementRejected",
                container, tableName, item, chancePercent, applied ? "applied" : "apply_refused", roll, selected);
            return applied;
        }

        private static string ContextRefusal(Entity container, string tableName, Entity item, Random rng)
        {
            if (rng == null) return "random_required";
            if (tableName != SourceTable) return "wrong_table";
            if (container == null) return "owner_required";
            if (container.BlueprintName != "LockedChest") return "owner_blueprint";
            if (container.GetPart<LockPart>() == null) return "owner_lock_required";
            if (item == null) return "item_required";
            var ownerPhysics = container.GetPart<PhysicsPart>();
            if (ownerPhysics == null) return "owner_physics_required";
            if (ownerPhysics.Takeable) return "owner_takeable";
            if (ownerPhysics.InInventory != null) return "owner_carried";
            if (ownerPhysics.Equipped != null) return "owner_equipped";
            var contents = container.GetPart<ContainerPart>();
            var physics = item.GetPart<PhysicsPart>();
            if (contents == null) return "container_required";
            if (!contents.Contents.Contains(item)) return "item_not_retained";
            if (physics == null) return "item_physics_required";
            if (!ReferenceEquals(physics.InInventory, container)) return "item_owner_mismatch";
            if (physics.Equipped != null) return "item_equipped";
            if (!item.HasTag("Item")) return "item_tag_required";
            if (item.GetTag("Tier") != "4") return "item_tier";
            if (item.HasTag("Unique")) return "item_unique";
            if (item.HasTag("QuestItem")) return "item_quest";
            if (item.HasTag("Crafted")) return "item_crafted";
            if (item.HasTag("Rentable")) return "item_rentable";
            if (item.GetPart<RentalPart>() != null) return "item_rented";
            if (item.GetPart<EquippablePart>() == null) return "item_not_equippable";
            if (item.Properties.ContainsKey(RollMarker)) return "already_rolled";
            if (ItemEnhancing.CountEnhancements(item) != 0) return "already_enhanced";
            var stack = item.GetPart<StackerPart>();
            if (stack != null && stack.StackCount != 1) return "item_stack";
            bool weapon = item.BlueprintName == "TemperedLongSword" || item.BlueprintName == "CounterweightMaul";
            bool armor = item.BlueprintName == "FineRingMail" || item.BlueprintName == "RivetedPlate";
            if (!weapon && !armor) return "item_blueprint";
            if (weapon && (item.GetPart<MeleeWeaponPart>() == null || item.GetPart<ArmorPart>() != null)) return "weapon_shape";
            if (armor && (item.GetPart<ArmorPart>() == null || item.GetPart<MeleeWeaponPart>() != null)) return "armor_shape";
            return null;
        }

        private static bool Reject(Entity owner, string table, Entity item, int chance, string reason)
        {
            Record("FoundEquipmentEnhancementRejected", owner, table, item, chance, reason);
            return false;
        }

        private static void Record(string kind, Entity owner, string table, Entity item,
            int chancePercent, string reason, int? roll = null, string enhancement = null)
        {
            if (!Diag.IsChannelEnabled("loot")) return;
            Diag.Record("loot", kind, actor: owner, target: item,
                payload: new { table, reason, enhancement, tier = 1, chancePercent, roll });
        }
    }
}
