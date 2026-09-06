using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable quantity audit using actual content with explicitly configured handling penalties.</summary>
    [Scenario(name: "Carried Quantity Audit", category: "Items",
        description: "Plant, infuse, disassemble and brew while carrying penalties track units.")]
    public sealed class GameAuditQuantityRefreshBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity Seed, Salt, Dagger, Moss, Weapon;
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "AlchemyStill", "Dagger", "Warhammer", "Grass", "CandyCarrotSeed", "CandyCarrotCrop", "FireMoss", "PaleSalt" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Carried quantity audit missing blueprint: " + bp);
            if (ctx.PlayerEntity.GetPart<InventoryPart>() == null || ctx.PlayerEntity.GetPart<Body>() == null)
                throw new InvalidOperationException("Carried quantity audit needs player inventory and body.");
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            ConversationManager.EndConversation();
            ctx.PlayerEntity.GetPart<Body>().DropAllEquipment(ctx.Zone);
            var inventory = ctx.PlayerEntity.GetPart<InventoryPart>();
            foreach (var item in inventory.Objects.ToArray()) inventory.RemoveObject(item);
            inventory.EquippedItems.Clear(); inventory.MaxWeight = 150;
            foreach (var entity in ctx.Zone.GetAllEntities().ToArray())
                if (entity != ctx.PlayerEntity) { ctx.Turns.RemoveEntity(entity); ctx.Zone.RemoveEntity(entity); }
            ctx.Zone.GenReservedCells.Clear();
            for (int x = 1; x < Zone.Width - 1; x++) for (int y = 1; y < Zone.Height - 1; y++)
                Place(ctx.Factory.CreateEntity("Grass"), x, y);
            ctx.Zone.RemoveEntity(ctx.PlayerEntity); Place(ctx.PlayerEntity, 20, 12);
            ctx.PlayerEntity.RemoveEffect<ElectrifiedEffect>(); ctx.PlayerEntity.RemoveEffect<WetEffect>(); ctx.PlayerEntity.RemoveEffect<LiquidCoveredEffect>();
            var hp = ctx.PlayerEntity.GetStat("Hitpoints"); hp.Max = 100; hp.BaseValue = 100;
            var speed = ctx.PlayerEntity.GetStat("Speed"); speed.BaseValue = 100; speed.Penalty = 7;
            Seed = Give("CandyCarrotSeed", 3, 4); Salt = Give("PaleSalt", 3, 4);
            Dagger = Give("Dagger", 3, 4); Moss = Give("FireMoss", 3, 4); Weapon = Give("Warhammer", 1, 0);
            Place(ctx.Factory.CreateEntity("AlchemyStill"), 21, 12);
            if (ctx.PlayerEntity.GetPart<BitLockerPart>() == null) ctx.PlayerEntity.AddPart(new BitLockerPart());
            ctx.PlayerEntity.GetPart<BitLockerPart>().LearnRecipe("mod_palesalt_infuse");
            BrewRuleRegistry.EnsureInitialized(); TinkerRecipeRegistry.EnsureInitialized(); EnhancementFactory.ForceReinitialize(); EnhancementFactory.EnsureInitialized();
            AlchemyStillPart.Factory = ctx.Factory; SeedPart.Factory = ctx.Factory;
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn(); MessageLog.Clear();
            ZoneRenderHooks.MarkFullDirty("GameAuditQuantityRefreshBench");
            if (Application.isPlaying) new GameObject("Carried Quantity Native Audit").AddComponent<GameAuditQuantityRefreshBenchPlayer>().Initialize(ctx, this);
            Entity Give(string blueprint, int quantity, int penalty)
            {
                var item = ctx.Factory.CreateEntity(blueprint); item.GetPart<StackerPart>().StackCount = quantity;
                var handling = item.GetPart<HandlingPart>();
                if (handling == null) { handling = new HandlingPart(); item.AddPart(handling); }
                handling.CarryMovePenalty = penalty;
                if (!inventory.AddObject(item)) throw new InvalidOperationException("Audit reagent insertion refused: " + blueprint); return item;
            }
            void Place(Entity entity, int x, int y)
            { if (!ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Carried quantity arena placement refused."); }
        }
        /// <summary>Record actual observations without changing diagnostic preferences.</summary>
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++; Audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool enabled = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "QuantityRefreshNativeAudit", payload: new { runId = RunId, name, passed }); }
            finally { Diag.SetChannel("scenario", enabled); }
            if (!passed) throw new InvalidOperationException("Carried quantity native audit failed: " + name);
        }
    }
}
