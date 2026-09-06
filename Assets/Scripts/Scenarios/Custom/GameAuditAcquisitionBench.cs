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
    [Scenario(name: "Acquisition Conservation Audit", category: "Items",
        description: "Native gold pickup, capacity-boundary looting and purchase refusal/retry.")]
    public sealed class GameAuditAcquisitionBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity Sack { get; private set; }
        public Entity Merchant { get; private set; }
        public Entity StartApples { get; private set; }
        public Entity SackLarge { get; private set; }
        public Entity SackSmall { get; private set; }
        public Entity StockLarge { get; private set; }
        public Entity StockSmall { get; private set; }
        public Entity Gold { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "Sack", "Merchant", "GoldCoin", "Starapple", "StoneFloor" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Acquisition audit missing blueprint: " + bp);
            if (ctx.PlayerEntity.GetPart<InventoryPart>() == null || ConversationLoader.Get("Merchant_1") == null)
                throw new InvalidOperationException("Acquisition audit needs inventory and the authored merchant conversation.");
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
            StartApples = Stack("Starapple", 52);
            if (!inventory.AddObject(StartApples)) throw new InvalidOperationException("Player boundary stock refused.");
            Gold = Stack("GoldCoin", 3); Place(Gold, 20, 12);
            Sack = ctx.Factory.CreateEntity("Sack"); Place(Sack, 20, 13);
            SackLarge = Stack("Starapple", 99); SackSmall = Stack("Starapple", 1);
            var contents = Sack.GetPart<ContainerPart>();
            if (!contents.AddItem(SackLarge) || !contents.AddItem(SackSmall)) throw new InvalidOperationException("Sack boundary stock refused.");
            Merchant = ctx.Factory.CreateEntity("Merchant"); Place(Merchant, 21, 12);
            var shelf = Merchant.GetPart<InventoryPart>();
            foreach (var item in shelf.Objects.ToArray()) shelf.RemoveObject(item);
            StockLarge = Stack("Starapple", 99); StockSmall = Stack("Starapple", 1);
            if (!shelf.AddObject(StockLarge) || !shelf.AddObject(StockSmall)) throw new InvalidOperationException("Merchant boundary stock refused.");
            TradeSystem.SetDrams(ctx.PlayerEntity, 10000); TradeSystem.SetDrams(Merchant, 10000); PlayerReputation.Set("Villagers", 0);
            ConversationActions.Factory = ctx.Factory;
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn();
            ZoneRenderHooks.MarkFullDirty("GameAuditAcquisitionBench");
            if (Application.isPlaying)
                new GameObject("Acquisition Native Audit").AddComponent<GameAuditAcquisitionBenchPlayer>().Initialize(ctx, this);
            Entity Stack(string blueprint, int quantity)
            { var item = ctx.Factory.CreateEntity(blueprint); item.GetPart<StackerPart>().StackCount = quantity; return item; }
            void Place(Entity entity, int x, int y)
            { if (!ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Acquisition arena placement refused."); }
        }
        /// <summary>Record actual native observations without changing diagnostic preferences.</summary>
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++; Audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool enabled = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "AcquisitionNativeAudit", payload: new { runId = RunId, name, passed }); }
            finally { Diag.SetChannel("scenario", enabled); }
            if (!passed) throw new InvalidOperationException("Acquisition native audit failed: " + name);
        }
    }
}
