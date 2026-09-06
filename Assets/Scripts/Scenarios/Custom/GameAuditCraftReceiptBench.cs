using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable repeated crafting, exact recipient and capacity retry audit.</summary>
    [Scenario(name: "Craft Receipt Audit", category: "Items",
        description: "Brew and craft into existing stacks, refuse for capacity, free space and retry.")]
    public sealed class GameAuditCraftReceiptBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity Dagger, Good;
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "Dagger", "StoneFloor", "GlimmerBrine", "BrewedTonic" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Craft receipt audit missing blueprint: " + bp);
            TinkerRecipeRegistry.EnsureInitialized();
            if (!TinkerRecipeRegistry.TryGetRecipe("craft_dagger", out var recipe) || recipe.Cost != "BC" || recipe.NumberMade != 1 || !string.IsNullOrEmpty(recipe.Ingredient))
                throw new InvalidOperationException("Craft receipt audit requires the actual one-dagger BC build recipe.");
            if (ctx.PlayerEntity.GetPart<InventoryPart>() == null || ctx.PlayerEntity.GetPart<Body>() == null)
                throw new InvalidOperationException("Craft receipt audit needs player inventory and body.");
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            ConversationManager.EndConversation();
            ctx.PlayerEntity.GetPart<Body>().DropAllEquipment(ctx.Zone);
            var inventory = ctx.PlayerEntity.GetPart<InventoryPart>();
            foreach (var item in inventory.Objects.ToArray()) inventory.RemoveObject(item);
            inventory.EquippedItems.Clear(); inventory.MaxWeight = 16;
            foreach (var entity in ctx.Zone.GetAllEntities().ToArray())
                if (entity != ctx.PlayerEntity) { ctx.Turns.RemoveEntity(entity); ctx.Zone.RemoveEntity(entity); }
            ctx.Zone.GenReservedCells.Clear();
            for (int x = 1; x < Zone.Width - 1; x++) for (int y = 1; y < Zone.Height - 1; y++)
                Place(ctx.Factory.CreateEntity("StoneFloor"), x, y);
            ctx.Zone.RemoveEntity(ctx.PlayerEntity); Place(ctx.PlayerEntity, 20, 12);
            ctx.PlayerEntity.RemoveEffect<ElectrifiedEffect>(); ctx.PlayerEntity.RemoveEffect<WetEffect>(); ctx.PlayerEntity.RemoveEffect<LiquidCoveredEffect>();
            var hp = ctx.PlayerEntity.GetStat("Hitpoints"); hp.Max = 100; hp.BaseValue = 100;
            Dagger = Give("Dagger", 1);
            Good = Give("GlimmerBrine", 2); CraftingMarkPart.Toggle(Good);
            if (InventoryPart.GetItemWeight(Dagger) != 4 || InventoryPart.GetItemWeight(Good) != 2)
                throw new InvalidOperationException("Craft receipt audit needs the authored dagger/brine weights.");
            BrewRuleRegistry.EnsureInitialized();
            ctx.PlayerEntity.GetPart<BrewKnowledgePart>()?.RestoreDiscoveredRules(Array.Empty<string>());
            var bits = ctx.PlayerEntity.GetPart<BitLockerPart>();
            if (bits == null) { bits = new BitLockerPart(); ctx.PlayerEntity.AddPart(bits); }
            bits.RestoreBitsAndRecipes(new Dictionary<char, int> { ['B'] = 3, ['C'] = 3 }, new[] { "craft_dagger" });
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn(); MessageLog.Clear();
            ZoneRenderHooks.MarkFullDirty("GameAuditCraftReceiptBench");
            if (Application.isPlaying) new GameObject("Craft Receipt Native Audit").AddComponent<GameAuditCraftReceiptBenchPlayer>().Initialize(ctx, this);
            Entity Give(string blueprint, int quantity)
            {
                var item = ctx.Factory.CreateEntity(blueprint); item.GetPart<StackerPart>().StackCount = quantity;
                if (!inventory.AddObject(item)) throw new InvalidOperationException("Audit reagent insertion refused: " + blueprint); return item;
            }
            void Place(Entity entity, int x, int y)
            { if (!ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Craft receipt arena placement refused."); }
        }
        /// <summary>Record actual observations without changing diagnostic preferences.</summary>
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++; Audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool enabled = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "CraftReceiptNativeAudit", payload: new { runId = RunId, name, passed }); }
            finally { Diag.SetChannel("scenario", enabled); }
            if (!passed) throw new InvalidOperationException("Craft receipt native audit failed: " + name);
        }
    }
}
