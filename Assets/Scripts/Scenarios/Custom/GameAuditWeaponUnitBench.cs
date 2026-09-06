using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable native reforge audit with actual components and brewed quench inputs.</summary>
    [Scenario(name: "Weapon Unit Crafting Audit", category: "Items",
        description: "Brew, forge a batch, and change one weapon through real crafting selections.")]
    public sealed class GameAuditWeaponUnitBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity Blade, Haft, Binding, Replacement, Glimmer, Spark, Station;
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "TinkersForge", "AlchemyStill", "BrewedTonic", "StoneFloor", "ForgedWeapon", "SteelBladeComponent", "OakHaftComponent", "LeatherBindingComponent", "IronSpikeComponent", "GlimmerBrine", "SparkRoot" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Weapon unit crafting audit missing blueprint: " + bp);
            if (ctx.PlayerEntity.GetPart<InventoryPart>() == null || ctx.PlayerEntity.GetPart<Body>() == null)
                throw new InvalidOperationException("Weapon unit crafting audit needs player inventory and body.");
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
            var hp = ctx.PlayerEntity.GetStat("Hitpoints"); hp.Max = 100; hp.BaseValue = 100;
            Blade = Give("SteelBladeComponent", 3); Haft = Give("OakHaftComponent", 3); Binding = Give("LeatherBindingComponent", 3);
            Replacement = Give("IronSpikeComponent", 1); Glimmer = Give("GlimmerBrine", 3); Spark = Give("SparkRoot", 3);
            Station = ctx.Factory.CreateEntity("TinkersForge"); Place(Station, 21, 12);
            Place(ctx.Factory.CreateEntity("AlchemyStill"), 19, 12);
            BrewRuleRegistry.ResetForTests(); BrewRuleRegistry.EnsureInitialized();
            ForgePart.Factory = ctx.Factory; AlchemyStillPart.Factory = ctx.Factory;
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn(); MessageLog.Clear();
            ZoneRenderHooks.MarkFullDirty("GameAuditWeaponUnitBench");
            if (Application.isPlaying) new GameObject("Weapon Unit Crafting Native Audit").AddComponent<GameAuditWeaponUnitBenchPlayer>().Initialize(ctx, this);
            Entity Give(string blueprint, int quantity)
            {
                var item = ctx.Factory.CreateEntity(blueprint); item.GetPart<StackerPart>().StackCount = quantity;
                if (!inventory.AddObject(item)) throw new InvalidOperationException("Audit reagent insertion refused: " + blueprint); return item;
            }
            void Place(Entity entity, int x, int y)
            { if (!ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Weapon unit crafting arena placement refused."); }
        }
        /// <summary>Record actual observations without changing diagnostic preferences.</summary>
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++; Audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool enabled = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "WeaponUnitNativeAudit", payload: new { runId = RunId, name, passed }); }
            finally { Diag.SetChannel("scenario", enabled); }
            if (!passed) throw new InvalidOperationException("Weapon unit crafting native audit failed: " + name);
        }
    }
}
