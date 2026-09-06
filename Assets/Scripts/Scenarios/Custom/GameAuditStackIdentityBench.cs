using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable native crafting audit with actual reagents and unmodified ground weapons.</summary>
    [Scenario(name: "Crafted Stack Identity Audit", category: "Items",
        description: "Brew, infuse, transfer and drink actual items without replacing their payload.")]
    public sealed class GameAuditStackIdentityBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity Glimmer, Spark, Mendleaf, Ember, Candy, Salt, DaggerA, DaggerB, DaggerC, Blocker;
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "AlchemyStill", "Dagger", "Warhammer", "StoneFloor", "BrewedTonic", "GlimmerBrine", "SparkRoot", "MendleafSprig", "EmberFruit", "CandyHeartRoot", "PaleSalt" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Stack identity audit missing blueprint: " + bp);
            if (ctx.PlayerEntity.GetPart<InventoryPart>() == null || ctx.PlayerEntity.GetPart<Body>() == null)
                throw new InvalidOperationException("Stack identity audit needs player inventory and body.");
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
                Place(ctx.Factory.CreateEntity("StoneFloor"), x, y);
            ctx.Zone.RemoveEntity(ctx.PlayerEntity); Place(ctx.PlayerEntity, 20, 12);
            ctx.PlayerEntity.RemoveEffect<ElectrifiedEffect>(); ctx.PlayerEntity.RemoveEffect<WetEffect>(); ctx.PlayerEntity.RemoveEffect<LiquidCoveredEffect>();
            var hp = ctx.PlayerEntity.GetStat("Hitpoints"); hp.Max = 100; hp.BaseValue = 20;
            Glimmer = Give("GlimmerBrine", 3); Spark = Give("SparkRoot", 2); Mendleaf = Give("MendleafSprig", 2);
            Ember = Give("EmberFruit", 1); Candy = Give("CandyHeartRoot", 2); Salt = Give("PaleSalt", 4);
            Blocker = Give("Warhammer", 1);
            if (!InventorySystem.Equip(ctx.PlayerEntity, Blocker)) throw new InvalidOperationException("Audit hand blocker refused.");
            DaggerA = ctx.Factory.CreateEntity("Dagger"); Place(DaggerA, 20, 12);
            DaggerB = ctx.Factory.CreateEntity("Dagger"); Place(DaggerB, 20, 13);
            DaggerC = ctx.Factory.CreateEntity("Dagger"); Place(DaggerC, 20, 14);
            Place(ctx.Factory.CreateEntity("AlchemyStill"), 21, 12);
            if (ctx.PlayerEntity.GetPart<BitLockerPart>() == null) ctx.PlayerEntity.AddPart(new BitLockerPart());
            ctx.PlayerEntity.GetPart<BitLockerPart>().LearnRecipe("mod_palesalt_infuse");
            BrewRuleRegistry.EnsureInitialized(); TinkerRecipeRegistry.EnsureInitialized(); EnhancementFactory.ForceReinitialize(); EnhancementFactory.EnsureInitialized();
            AlchemyStillPart.Factory = ctx.Factory;
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn(); MessageLog.Clear();
            ZoneRenderHooks.MarkFullDirty("GameAuditStackIdentityBench");
            if (Application.isPlaying) new GameObject("Stack Identity Native Audit").AddComponent<GameAuditStackIdentityBenchPlayer>().Initialize(ctx, this);
            Entity Give(string blueprint, int quantity)
            {
                var item = ctx.Factory.CreateEntity(blueprint); item.GetPart<StackerPart>().StackCount = quantity;
                if (!inventory.AddObject(item)) throw new InvalidOperationException("Audit reagent insertion refused: " + blueprint); return item;
            }
            void Place(Entity entity, int x, int y)
            { if (!ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Stack identity arena placement refused."); }
        }
        /// <summary>Record actual observations without changing diagnostic preferences.</summary>
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++; Audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool enabled = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "StackIdentityNativeAudit", payload: new { runId = RunId, name, passed }); }
            finally { Diag.SetChannel("scenario", enabled); }
            if (!passed) throw new InvalidOperationException("Stack identity native audit failed: " + name);
        }
    }
}
