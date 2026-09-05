using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable native dialogue audit. Staging alone records no passed cases.</summary>
    [Scenario(name: "Handover and Repair Audit", category: "Items",
        description: "Native full-pack book copy and successful/refused oven repair controls.")]
    public sealed class GameAuditHandoverBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity Scribe { get; private set; }
        public Entity Farmer { get; private set; }
        public Entity Original { get; private set; }
        public Entity Guide { get; private set; }
        public Entity Clay { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();

        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "Scribe", "Farmer", "MendingRiteGrimoire", "OvenBuildersGuide", "FireClay", "GrimoireCopy", "StoneFloor" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Handover audit missing blueprint: " + bp);
            if (ctx.Zone.ZoneID != WorldMap.StartingZoneID || ctx.PlayerEntity.GetPart<InventoryPart>() == null
                || ConversationLoader.Get("Scribe_1") == null || ConversationLoader.Get("Farmer_1") == null)
                throw new InvalidOperationException("Handover audit requires the starting village, player inventory and authored conversations.");
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            ConversationManager.EndConversation(); MessageLog.Clear();
            ctx.PlayerEntity.GetPart<Body>()?.DropAllEquipment(ctx.Zone);
            var inventory = ctx.PlayerEntity.GetPart<InventoryPart>();
            foreach (var item in inventory.Objects.ToArray()) inventory.RemoveObject(item);
            inventory.EquippedItems.Clear(); inventory.MaxWeight = 1000;
            foreach (var entity in ctx.Zone.GetAllEntities().ToArray())
                if (entity != ctx.PlayerEntity) { ctx.Turns.RemoveEntity(entity); ctx.Zone.RemoveEntity(entity); }
            ctx.Zone.GenReservedCells.Clear();
            for (int x = 1; x < Zone.Width - 1; x++) for (int y = 1; y < Zone.Height - 1; y++)
                ctx.Zone.AddEntity(ctx.Factory.CreateEntity("StoneFloor"), x, y);
            ctx.Zone.RemoveEntity(ctx.PlayerEntity);
            if (!ctx.Zone.AddEntity(ctx.PlayerEntity, 20, 12)) throw new InvalidOperationException("Player placement refused.");
            Scribe = Place("Scribe", 21, 12); Farmer = Place("Farmer", 20, 13);
            Original = Carry("MendingRiteGrimoire"); Guide = Carry("OvenBuildersGuide"); Clay = Carry("FireClay");
            Clay.GetPart<StackerPart>().StackCount = 3; inventory.MaxWeight = inventory.GetCarriedWeight();
            inventory.RefreshHandlingCarryPenalty(); PlayerReputation.Set("Villagers", 0);
            ConversationActions.Factory = ctx.Factory; SettlementRuntime.ActiveZone = ctx.Zone;
            var manager = SettlementManager.Current ?? new SettlementManager();
            var settlement = manager.GetOrCreateSettlement(ctx.Zone.ZoneID, new PointOfInterest(POIType.Village, "Sill", "Villagers", 1));
            var oven = settlement.GetSite("VillageOven");
            if (oven == null) throw new InvalidOperationException("Native audit oven site missing.");
            oven.Stage = RepairStage.Fouled;
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn();
            ZoneRenderHooks.MarkFullDirty("GameAuditHandoverBench");
            if (Application.isPlaying)
                new GameObject("Handover Native Audit").AddComponent<GameAuditHandoverBenchPlayer>().Initialize(ctx, this);

            Entity Place(string bp, int x, int y)
            {
                var entity = ctx.Factory.CreateEntity(bp); entity.Properties["SettlementId"] = ctx.Zone.ZoneID;
                if (!ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException(bp + " placement refused.");
                return entity; // Deliberately not registered for turns: native UI subjects stay adjacent.
            }
            Entity Carry(string bp)
            {
                var item = ctx.Factory.CreateEntity(bp);
                if (!inventory.AddObject(item)) throw new InvalidOperationException(bp + " inventory placement refused.");
                return item;
            }
        }

        /// <summary>Record an observed native result, preserving the user's diagnostic channel setting.</summary>
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++;
            Audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool enabled = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "HandoverNativeAudit", payload: new { runId = RunId, name, passed }); }
            finally { Diag.SetChannel("scenario", enabled); }
            if (!passed) throw new InvalidOperationException("Handover native audit failed: " + name);
        }
    }
}
