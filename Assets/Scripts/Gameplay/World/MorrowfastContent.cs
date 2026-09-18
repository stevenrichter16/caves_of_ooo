using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Native authored cast. Registration is idempotent and never restocks a living owner.</summary>
    public static class MorrowfastContent
    {
        public readonly struct ResidentPlacement
        {
            public readonly string Id; public readonly int X, Y; public readonly string SpriteBlueprint;
            public ResidentPlacement(string id, int x, int y, string sprite) { Id = id; X = x; Y = y; SpriteBlueprint = sprite; }
        }
        public static readonly string[] ResidentIds = { "north-guard-west", "north-guard-east", "east-robed-resident", "western-shopkeeper", "southwest-craftsperson", "southern-food-vendor", "farra-sprig", "edden-brack" };
        public static readonly ResidentPlacement[] AdditionalResidents = {
            new ResidentPlacement("farra-sprig", 37, 14, "Villager"), new ResidentPlacement("edden-brack", 38, 22, "Villager") };
        private static readonly string[] Names = { "Nemm Hushbell", "Hesta Brack", "Vennit Rusk", "Pell Drowse", "Orrit Coilstitch", "Sella Kettle", "Farra Sprig", "Edden Brack" };
        private static readonly string[] Descriptions = {
            "The west watchkeeper has wound his bell cord in cloth. He listens between frog calls before speaking.",
            "The east watchkeeper carries a return slate with more erasures than entries. Her position leaves the archway clear.",
            "A careful clerk of the Recension. Vennit writes who said a thing beside the thing itself, leaving uncertain accounts uncertain.",
            "The keeper of the Dry Hem hangs damp boots toe-down. Neither of the two guest beds bears a reservation slate.",
            "A rope mender with weathered fingers. Orrit tests a knot, unties it, and tests it again before trusting it with anyone's weight.",
            "Sella tends the Second Bowl, keeping the common table clear beside a modest stock of provisions.",
            "A Bower-taught gardener who asks permission before moving even an empty chair. She measures a welcome in room left for someone else.",
            "An adult traveler, tired from the northern path. Edden has returned safely and wants a quiet afternoon before explaining himself." };

        public static void EnsureRegistered()
        {
            ConversationActions.EnsureInitialized(); ConversationPredicates.EnsureInitialized();
            ConversationActions.Register("MorrowfastAction", (speaker, player, command) => {
                if (!MorrowfastQuests.TryConversation(speaker, player, command)) MessageLog.Add("That conversation cannot change anything now.");
            });
            ConversationPredicates.Register("IfMorrowfastCan", MorrowfastQuests.CanConversation);
            ConversationActions.RegisterRequired("MorrowfastExpedition", (speaker, player, command) =>
                MorrowfastExpedition.TryConversation(speaker, player, command) ? null : "supply_errand_unavailable");
            ConversationPredicates.Register("IfMorrowfastExpeditionCan", MorrowfastExpedition.CanConversation);
            FactionManager.RegisterFaction("Stillcord");
            // Get initializes all shipped content before loading our additive definitions; never clear another registry.
            if (ConversationLoader.Get("Morrowfast_Hesta") == null) LoadConversations();
            if (StoryletRegistry.Get(MorrowfastQuests.ReturnQuestId) == null
                || StoryletRegistry.Get(MorrowfastExpedition.QuestId) == null) LoadQuests();
        }
        private static void LoadConversations() { var a = Resources.Load<TextAsset>("Content/Conversations/Morrowfast"); if (a != null) ConversationLoader.LoadFromJson(a.text, a.name); }
        private static void LoadQuests() { var a = Resources.Load<TextAsset>("Content/Data/Storylets/Morrowfast"); if (a != null) StoryletRegistry.LoadFromJson(a.text, a.name); }

        public static Entity CreateResident(string componentId, EntityFactory factory)
        {
            int index = Array.IndexOf(ResidentIds, componentId);
            if (factory == null || index < 0 || !factory.Blueprints.TryGetValue("Creature", out var creature)) return null;
            EnsureRegistered();
            string first = Names[index].Split(' ')[0], blueprintName = "Morrowfast" + first;
            if (!factory.Blueprints.ContainsKey(blueprintName))
            {
                // BlueprintLoader resolves inheritance inside a single file. Copy the already baked native parent explicitly.
                var bp = Clone(creature, blueprintName);
                bp.Parts["Render"]["DisplayName"] = Names[index]; bp.Parts["Render"]["RenderString"] = "@";
                bp.Parts["Render"]["ColorString"] = index < 2 ? "&y" : index == 2 ? "&C" : "&g";
                SetPart(bp, "Brain", "Wanders", "false", "WandersRandomly", "false", "Passive", "true", "Staying", "true");
                SetPart(bp, "Conversation", "ConversationID", "Morrowfast_" + first);
                SetPart(bp, "MorrowfastResidentPart", "ResidentId", componentId);
                SetPart(bp, "Examinable", "Text", Descriptions[index]);
                bp.Tags["Faction"] = "Stillcord"; bp.Tags["Tier"] = "1";
                SetStat(bp, "Hitpoints", index < 2 ? 40 : 24, index < 2 ? 40 : 24);
                SetStat(bp, "Strength", index < 2 ? 18 : 12, 999); SetStat(bp, "Agility", 14, 999);
                SetStat(bp, "Toughness", 14, 999); SetStat(bp, "Intelligence", index == 2 ? 18 : 12, 999);
                SetStat(bp, "Ego", 14, 999); SetStat(bp, "XPValue", 20, 999);
                if (index < 2) SetPart(bp, "Loadout", "Equip", "Spear;LeatherArmor");
                if (index == 4 || index == 5) SetPart(bp, "Trader", "StockTable", index == 4 ? "MorrowfastMenderStock" : "MorrowfastProvisionerStock", "Drams", index == 4 ? "300" : "200");
                if (index == 0) SetPart(bp, "QuestBeacon", "Quest", MorrowfastQuests.BellQuestId);
                if (index == 1) SetPart(bp, "QuestBeacon", "Quest", MorrowfastQuests.ReturnQuestId);
                if (index == 6) SetPart(bp, "QuestBeacon", "Quest", "MorrowfastDryGoods");
                factory.Blueprints[blueprintName] = bp;
            }
            var actor = factory.CreateEntity(blueprintName);
            if (index == 4) Stock(actor, factory, new[] { "Torch", "Dagger", "Spear", "LeatherArmor", "Tepuibone", "FireClay" }, new[] { 4, 2, 1, 1, 3, 4 });
            if (index == 5) Stock(actor, factory, new[] { "Mushroom", "DriedMeat", "HealingTonic", "BurnSalve", "WaterTonic" }, new[] { 8, 4, 2, 2, 3 });
            return actor;
        }
        private static void Stock(Entity actor, EntityFactory factory, string[] items, int[] counts)
        {
            var inventory = actor.GetPart<InventoryPart>();
            // TraderPart owns the normal opening roll. Keep the original
            // factory-less construction fallback without doubling a live shelf.
            if (inventory.Objects.Count > 0) return;
            for (int i = 0; i < items.Length; i++) for (int j = 0; j < counts[i]; j++) inventory.AddObject(factory.CreateEntity(items[i]));
        }
        private static Blueprint Clone(Blueprint source, string name)
        {
            var bp = new Blueprint { Name = name, Inherits = source.Name, Parent = source, Baked = true };
            foreach (var kv in source.Parts) bp.Parts[kv.Key] = new Dictionary<string, string>(kv.Value);
            foreach (var kv in source.Stats) { var s = kv.Value; bp.Stats[kv.Key] = new StatBlueprint { Name = s.Name, Value = s.Value, Min = s.Min, Max = s.Max, Boost = s.Boost, sValue = s.sValue }; }
            foreach (var kv in source.Tags) bp.Tags[kv.Key] = kv.Value;
            foreach (var kv in source.Props) bp.Props[kv.Key] = kv.Value;
            foreach (var kv in source.IntProps) bp.IntProps[kv.Key] = kv.Value;
            return bp;
        }
        private static void SetPart(Blueprint bp, string name, params string[] pairs)
        { if (!bp.Parts.TryGetValue(name, out var p)) bp.Parts[name] = p = new Dictionary<string, string>(); for (int i = 0; i < pairs.Length; i += 2) p[pairs[i]] = pairs[i + 1]; }
        private static void SetStat(Blueprint bp, string name, int value, int max)
        { bp.Stats[name] = new StatBlueprint { Name = name, Value = value, Min = 0, Max = max }; }
    }
}
