using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable arena for an actual keyboard pickup-and-consume audit.
    /// Staging does not count as verification; the native driver records each observed result.</summary>
    [Scenario(name: "Consumable Ownership Audit", category: "Items",
        description: "Native world-menu, pickup and inventory consumption controls.")]
    public sealed class GameAuditConsumablesBench : IScenario
    {
        /// <summary>Native fixture control: dismiss the isolated boot modal before auditing ordinary gameplay.</summary>
        public bool PreDismissBootMenu { get; set; }
        public string RunId { get; private set; }
        public Entity Tonic { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();

        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "HealingTonic", "StoneFloor" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp))
                    throw new InvalidOperationException("Consumable audit missing blueprint: " + bp);
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            ctx.PlayerEntity.GetPart<Body>()?.DropAllEquipment(ctx.Zone);
            var inventory = ctx.PlayerEntity.GetPart<InventoryPart>();
            if (inventory == null) throw new InvalidOperationException("Consumable audit requires an inventory.");
            foreach (var item in inventory.Objects.ToArray()) inventory.RemoveObject(item);
            inventory.EquippedItems.Clear();
            foreach (var entity in ctx.Zone.GetAllEntities().ToArray())
                if (entity != ctx.PlayerEntity) { ctx.Turns.RemoveEntity(entity); ctx.Zone.RemoveEntity(entity); }
            ctx.Zone.GenReservedCells.Clear();
            for (int x = 1; x < Zone.Width - 1; x++) for (int y = 1; y < Zone.Height - 1; y++)
                ctx.Zone.AddEntity(ctx.Factory.CreateEntity("StoneFloor"), x, y);
            ctx.Zone.RemoveEntity(ctx.PlayerEntity); ctx.Zone.AddEntity(ctx.PlayerEntity, 20, 12);
            var hp = ctx.PlayerEntity.GetStat("Hitpoints");
            hp.Max = 100; hp.BaseValue = 10; hp.Bonus = hp.Penalty = hp.Boost = 0;
            Tonic = ctx.Factory.CreateEntity("HealingTonic");
            if (!ctx.Zone.AddEntity(Tonic, 21, 12)) throw new InvalidOperationException("Tonic placement refused.");
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn();
            ZoneRenderHooks.MarkFullDirty("GameAuditConsumablesBench");
            if (Application.isPlaying)
                new GameObject("Consumable Native Audit").AddComponent<GameAuditConsumablesBenchPlayer>().Initialize(ctx, this);
        }

        /// <summary>Record a native observation; a failed precondition aborts dependent steps.</summary>
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++;
            Audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool wasEnabled = Diag.IsChannelEnabled("scenario");
            Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "ConsumableNativeAudit", payload: new { runId = RunId, name, passed }); }
            finally { Diag.SetChannel("scenario", wasEnabled); }
            if (!passed) throw new InvalidOperationException("Consumable native audit failed: " + name);
        }
    }
}
