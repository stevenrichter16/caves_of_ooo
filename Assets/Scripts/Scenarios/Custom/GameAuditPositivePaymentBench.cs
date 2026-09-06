using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable native planting and mineral dialogue audit with actual content.</summary>
    [Scenario(name: "Planting and Mineral Payment Audit", category: "Items",
        description: "Planting refusal and success, mineral sale, and one-time Founding offering.")]
    public sealed class GameAuditPositivePaymentBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity Seed, Salt, Stone, SaltMaster, Tender;
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "StoneFloor", "Grass", "CandyCarrotSeed", "CandyCarrotCrop", "PaleSalt", "Tepuibone", "SaltMaster", "FoundingPlaqueTender" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Positive payment audit missing blueprint: " + bp);
            if (ctx.PlayerEntity.GetPart<InventoryPart>() == null || ctx.PlayerEntity.GetPart<Body>() == null
                || NarrativeStatePart.Current == null || ConversationLoader.Get("SaltMaster_1") == null || ConversationLoader.Get("FoundingPlaqueTender_1") == null)
                throw new InvalidOperationException("Positive payment audit requires a player session and authored conversations.");
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
                Place(ctx.Factory.CreateEntity(x == 20 && y == 13 ? "Grass" : "StoneFloor"), x, y);
            ctx.Zone.RemoveEntity(ctx.PlayerEntity); Place(ctx.PlayerEntity, 20, 12);
            ctx.PlayerEntity.RemoveEffect<ElectrifiedEffect>(); ctx.PlayerEntity.RemoveEffect<WetEffect>(); ctx.PlayerEntity.RemoveEffect<LiquidCoveredEffect>();
            var hp = ctx.PlayerEntity.GetStat("Hitpoints"); hp.Max = 100; hp.BaseValue = 100;
            Seed = Give("CandyCarrotSeed", 2); Salt = Give("PaleSalt", 1); Stone = Give("Tepuibone", 2);
            SaltMaster = ctx.Factory.CreateEntity("SaltMaster"); Place(SaltMaster, 21, 13);
            Tender = ctx.Factory.CreateEntity("FoundingPlaqueTender"); Place(Tender, 19, 13);
            // UI subjects deliberately receive no turns, keeping native dialogue targets adjacent.
            PlayerReputation.Set("TentRight", 0); PlayerReputation.Set("CatacombFolk", 0);
            NarrativeStatePart.Current.SetFact(FoundingTrustService.OfferedFact, 0);
            SeedPart.Factory = ctx.Factory; ConversationActions.Factory = ctx.Factory; SettlementRuntime.ActiveZone = ctx.Zone;
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn(); MessageLog.Clear();
            ZoneRenderHooks.MarkFullDirty("GameAuditPositivePaymentBench");
            if (Application.isPlaying) new GameObject("Planting and Mineral Payment Native Audit").AddComponent<GameAuditPositivePaymentBenchPlayer>().Initialize(ctx, this);
            Entity Give(string blueprint, int quantity)
            {
                var item = ctx.Factory.CreateEntity(blueprint); item.GetPart<StackerPart>().StackCount = quantity;
                if (!inventory.AddObject(item)) throw new InvalidOperationException("Audit reagent insertion refused: " + blueprint); return item;
            }
            void Place(Entity entity, int x, int y)
            { if (!ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Positive payment arena placement refused."); }
        }
        /// <summary>Record actual observations without changing diagnostic preferences.</summary>
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++; Audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool enabled = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "PositivePaymentNativeAudit", payload: new { runId = RunId, name, passed }); }
            finally { Diag.SetChannel("scenario", enabled); }
            if (!passed) throw new InvalidOperationException("Positive payment native audit failed: " + name);
        }
    }
}
