using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Saved identity for one exceptional keeper and its existing real
    /// reward. Item mechanics remain on the item; no load hook recreates gear.</summary>
    public sealed class LegendaryIdentityPart : Part
    {
        public override string Name => "LegendaryIdentity";
        public int Version = 1;
        public string PersonalName, Epithet, TemplateID, SurfaceID, RewardID, EnhancementName;

        /// <summary>Describe only an item the owner still actually carries.
        /// Pure: stale, disarmed, transferred or missing gear yields no claim.</summary>
        public string DescribeCarriedReward()
        {
            var inventory = ParentEntity?.GetPart<InventoryPart>();
            if (inventory == null || string.IsNullOrEmpty(RewardID)) return null;
            var matches = inventory.Objects.Concat(inventory.EquippedItems.Values).Distinct()
                .Where(e => e != null && e.ID == RewardID).ToArray();
            if (matches.Length != 1) return null;
            var item = matches[0];
            var physical = item.GetPart<PhysicsPart>();
            bool equipped = physical?.Equipped == ParentEntity
                && inventory.EquippedItems.Values.Contains(item)
                && ParentEntity.GetPart<Body>()?.GetParts().Any(p => p.Equipped == item) == true;
            bool carried = physical?.InInventory == ParentEntity && physical.Equipped == null
                && inventory.Objects.Contains(item);
            if (item.SpatialZone != null || (!equipped && !carried)) return null;
            var enhancement = item.Parts.OfType<IItemEnhancement>().FirstOrDefault(p => p.Name == EnhancementName);
            return enhancement == null ? null
                : (equipped ? "Equipped: " : "Carried: ") + item.GetDisplayName() + ". " + enhancement.GetEffectDescription() + ".";
        }
    }

    /// <summary>Cold, opt-in decoration of a fresh final-floor lair keeper.
    /// Runs after generation callbacks, before C10.2 publishes the native graph.
    /// No new creature/item, combat RNG, turn hook or global uniqueness ledger.</summary>
    public static class LegendaryLairEncounters
    {
        public const int DefaultChance = 35;
        private const string KeeperBlueprint = "MarlbackWallkeeper";
        private const string OrdinaryName = "marlback wallkeeper";
        private static readonly string[] Names = { "Rusk", "Drel", "Marn", "Tav", "Vek", "Borr", "Kerr", "Venn" };
        private static readonly string[] EdgeEpithets = { "the Notchback", "the Split Plate", "the Ridge-Scarred", "the Root-Cutter" };
        private static readonly string[] ArmorEpithets = { "the Black Harness", "the Amber Harness", "the Shell-Polished", "the Rain-Slick" };

        /// <summary>Upgrade one real equipped reward at a selected fresh source.
        /// Returns false for an ordinary/refused source; never replaces missing
        /// gear or modifies a cached/legacy/named actor. Chance is bounded0–100.</summary>
        public static bool TryApplyGeneratedFinal(Zone zone, OverworldZoneManager manager, int chancePercent = DefaultChance)
        {
            if (zone == null || manager == null || chancePercent < 0 || chancePercent > 100
                || !WorldMap.IsOverworldZoneID(zone.ZoneID)) return false;
            var at = WorldMap.FromZoneID(zone.ZoneID);
            if (!WorldMapAuthoring.InBounds(at.x, at.y) || at.z < 1 || zone.ZoneID != WorldMap.ToZoneID(at.x, at.y, at.z)
                || manager.CachedZones.ContainsKey(zone.ZoneID)
                || !LairStacks.TryPlan(manager, zone.ZoneID, out var plan)) return false;
            if (plan.Legacy || plan.BossBlueprint != KeeperBlueprint || plan.Tier < 2 || plan.Tier > 3
                || (plan.Biome != BiomeType.Spread && plan.Biome != BiomeType.Sodden)
                || at.z != plan.FinalDepth || (plan.GeneratedMask & (1 << at.z)) != 0) return false;
            uint source = Hash(manager.WorldSeed, plan.SurfaceID, "legendary-roll-v1");
            int roll = (int)(source % 100);
            if (roll >= chancePercent)
            {
                Record("LegendaryLairRoll", null, new { plan.SurfaceID, roll, chancePercent, selected = false });
                return false;
            }
            if (!LairStacks.TryGetValidatedFinalBoss(zone, manager, out var boss)
                || boss.BlueprintName != KeeperBlueprint || !boss.HasTag("Boss"))
                return false;
            var render = boss.GetPart<RenderPart>();
            if (string.IsNullOrEmpty(boss.ID) || zone.GetReadOnlyEntities().Count(e => e.ID == boss.ID) != 1
                || boss.SpatialZone != zone || zone.GetEntityCell(boss) == null
                || !boss.HasTag("Creature") || boss.HasTag("Player") || boss.HasTag("Unique")
                || boss.HasTag("Temporary") || boss.HasTag("NoDropOnDeath") || boss.HasPart("Conversation")
                || boss.HasPart("Trader") || boss.HasPart<LegendaryIdentityPart>()
                || CombatSystem.IsDeathHandled(boss) || boss.GetStatValue("Hitpoints") <= 0
                || boss.GetTag("Faction") != "OutlandRaiders" || render?.DisplayName != OrdinaryName)
                return Reject(boss, "changed-or-protected-owner");
            var cells = zone.GetOccupiedCells(boss);
            if (cells.Count == 0 || cells.Any(c => c == null || !c.Occupants.Contains(boss)))
                return Reject(boss, "missing-native-body");
            bool edge = Hash(manager.WorldSeed, plan.SurfaceID, "legendary-template-v1") % 2 == 0;
            string blueprint = edge ? "LongSword" : "LeatherArmor";
            string originalItemName = edge ? "long sword" : "leather armor";
            string enhancementName = edge ? nameof(EnhancementSerrated) : nameof(EnhancementLacquered);
            var inventory = boss.GetPart<InventoryPart>();
            var rewards = inventory?.EquippedItems.Values.Distinct().Where(e => e != null && e.BlueprintName == blueprint).ToArray();
            if (rewards == null || rewards.Length != 1) return Reject(boss, "missing-real-equipped-reward");
            var reward = rewards[0];
            var physical = reward.GetPart<PhysicsPart>();
            var itemRender = reward.GetPart<RenderPart>();
            if (reward.SpatialZone != null || physical?.Equipped != boss || physical.InInventory != null
                || boss.GetPart<Body>()?.GetParts().Any(p => p.Equipped == reward) != true
                || string.IsNullOrEmpty(reward.ID)
                || inventory.Objects.Concat(inventory.EquippedItems.Values).Distinct().Count(e => e != null && e.ID == reward.ID) != 1
                || (reward.GetPart<StackerPart>()?.StackCount ?? 1) != 1
                || ItemEnhancing.CountEnhancements(reward) != 0 || itemRender?.DisplayName != originalItemName
                || reward.HasTag("Temporary") || reward.HasTag("NoDropOnDeath") || reward.HasPart("Rental"))
                return Reject(boss, "changed-or-foreign-reward");
            EnhancementFactory.EnsureInitialized();
            Type expected = edge ? typeof(EnhancementSerrated) : typeof(EnhancementLacquered);
            if (!EnhancementFactory.TryGet(enhancementName, out var registered) || registered != expected
                || !ItemEnhancing.Apply(reward, enhancementName, 1, boss)) return Reject(boss, "enhancement-refused");
            string personalName = Names[Hash(manager.WorldSeed, plan.SurfaceID, "legendary-name-v1") % Names.Length];
            var epithets = edge ? EdgeEpithets : ArmorEpithets;
            string epithet = epithets[Hash(manager.WorldSeed, plan.SurfaceID, "legendary-epithet-v1") % epithets.Length];
            itemRender.DisplayName = (edge ? "serrated " : "lacquered ") + originalItemName;
            render.DisplayName = personalName + ", " + epithet + " (" + OrdinaryName + ")";
            boss.AddPart(new LegendaryIdentityPart
            {
                PersonalName = personalName, Epithet = epithet, TemplateID = edge ? "notched-edge" : "lacquered-harness",
                SurfaceID = plan.SurfaceID, RewardID = reward.ID, EnhancementName = enhancementName
            });
            Record("LegendaryLairRoll", boss, new { plan.SurfaceID, roll, chancePercent, selected = true,
                template = edge ? "notched-edge" : "lacquered-harness", reward = reward.ID, enhancementName });
            return true;
        }

        private static uint Hash(int seed, string surface, string domain)
        {
            unchecked
            {
                uint value = 2166136261;
                foreach (char c in seed.ToString(CultureInfo.InvariantCulture) + "|" + surface + "|" + domain)
                    value = (value ^ c) * 16777619;
                // Small name/profile pools must not share FNV's low-bit patterns.
                // Mix all source bits before modulo; this remains stable and local.
                value ^= value >> 16;
                value *= 0x7feb352d;
                value ^= value >> 15;
                value *= 0x846ca68b;
                value ^= value >> 16;
                return value;
            }
        }
        private static bool Reject(Entity actor, string reason)
        {
            Record("LegendaryLairRejected", actor, new { reason });
            return false;
        }
        private static void Record(string kind, Entity actor, object payload)
        {
            if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", kind, actor: actor, payload: payload);
        }
    }
}
