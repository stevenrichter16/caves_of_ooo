using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable native container/trade audit, using real items and hard capacities.</summary>
    [Scenario(name: "Transfer Conservation Audit", category: "Items",
        description: "Native full Sack refusal/retry and overloaded trader sale refusal/retry.")]
    public sealed class GameAuditTransferBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity Sack { get; private set; }
        public Entity Merchant { get; private set; }
        public Entity CarriedDagger { get; private set; }
        public Entity EquippedDagger { get; private set; }
        public Entity Filler { get; private set; }
        public Entity SmallStock { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "Sack", "Merchant", "Dagger", "Starapple", "Torch", "SilverSand", "FireClay", "WardOil", "HealingTonic", "OvenBuildersGuide", "StoneFloor" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Transfer audit missing blueprint: " + bp);
            if (ctx.PlayerEntity.GetPart<InventoryPart>() == null || ConversationLoader.Get("Merchant_1") == null)
                throw new InvalidOperationException("Transfer audit needs inventory and the authored merchant conversation.");
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            ConversationManager.EndConversation(); MessageLog.Clear();
            ctx.PlayerEntity.GetPart<Body>()?.DropAllEquipment(ctx.Zone);
            var inventory = ctx.PlayerEntity.GetPart<InventoryPart>();
            foreach (var item in inventory.Objects.ToArray()) inventory.RemoveObject(item);
            inventory.EquippedItems.Clear(); inventory.MaxWeight = 150;
            foreach (var entity in ctx.Zone.GetAllEntities().ToArray())
                if (entity != ctx.PlayerEntity) { ctx.Turns.RemoveEntity(entity); ctx.Zone.RemoveEntity(entity); }
            ctx.Zone.GenReservedCells.Clear();
            for (int x = 1; x < Zone.Width - 1; x++) for (int y = 1; y < Zone.Height - 1; y++)
                ctx.Zone.AddEntity(ctx.Factory.CreateEntity("StoneFloor"), x, y);
            ctx.Zone.RemoveEntity(ctx.PlayerEntity); Place(ctx.PlayerEntity, 20, 12);
            Sack = ctx.Factory.CreateEntity("Sack"); Place(Sack, 20, 12);
            foreach (string bp in new[] { "Torch", "SilverSand", "FireClay", "WardOil", "HealingTonic", "OvenBuildersGuide" })
            {
                var item = ctx.Factory.CreateEntity(bp);
                if (!Sack.GetPart<ContainerPart>().AddItem(item)) throw new InvalidOperationException("Sack fixture refused " + bp);
                if (bp == "SilverSand") Filler = item;
            }
            CarriedDagger = ctx.Factory.CreateEntity("Dagger"); CarriedDagger.GetPart<StackerPart>().StackCount = 3;
            if (!inventory.AddObject(CarriedDagger) || !InventorySystem.Equip(ctx.PlayerEntity, CarriedDagger))
                throw new InvalidOperationException("Dagger fixture could not equip one unit.");
            EquippedDagger = inventory.EquippedItems.Values.Distinct().Single();
            Merchant = ctx.Factory.CreateEntity("Merchant"); Place(Merchant, 21, 12);
            var shelf = Merchant.GetPart<InventoryPart>();
            foreach (var item in shelf.Objects.ToArray()) shelf.RemoveObject(item);
            foreach (int count in new[] { 99, 51 })
            {
                var apples = ctx.Factory.CreateEntity("Starapple"); apples.GetPart<StackerPart>().StackCount = count;
                if (!shelf.AddObject(apples)) throw new InvalidOperationException("Merchant fixture refused stock.");
                if (count == 51) SmallStock = apples;
            }
            TradeSystem.SetDrams(ctx.PlayerEntity, 10000); TradeSystem.SetDrams(Merchant, 10000); PlayerReputation.Set("Villagers", 0);
            ConversationActions.Factory = ctx.Factory;
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn();
            ZoneRenderHooks.MarkFullDirty("GameAuditTransferBench");
            if (Application.isPlaying)
                new GameObject("Transfer Native Audit").AddComponent<GameAuditTransferBenchPlayer>().Initialize(ctx, this);
            void Place(Entity entity, int x, int y)
            { if (!ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Transfer arena placement refused."); }
        }
        /// <summary>Record actual native observations without changing diagnostic preferences.</summary>
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++; Audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool enabled = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "TransferNativeAudit", payload: new { runId = RunId, name, passed }); }
            finally { Diag.SetChannel("scenario", enabled); }
            if (!passed) throw new InvalidOperationException("Transfer native audit failed: " + name);
        }
    }
}
