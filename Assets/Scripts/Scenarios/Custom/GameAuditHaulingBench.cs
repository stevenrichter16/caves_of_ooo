using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable native hauling audit. Setup edits are explicit fixtures;
    /// all haul/release, walking and saving/loading actions use queued keyboard input.</summary>
    [Scenario(name: "Hauling Lifecycle Audit", category: "Items",
        description: "Observe real barrel hauling, stale-save recovery and removal over a measured workload.")]
    public sealed class GameAuditHaulingBench : IScenario
    {
        public bool VerifyFixes { get; set; } = true;
        public string RunId { get; private set; }
        public Entity Barrel;
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "HaulBarrel", "StoneFloor" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Hauling audit missing blueprint: " + bp);
            var actor = ctx.PlayerEntity; var inventory = actor.GetPart<InventoryPart>();
            if (inventory == null || actor.GetPart<Body>() == null || actor.GetStat("Speed") == null || actor.GetStat("Strength") == null)
                throw new InvalidOperationException("Hauling audit requires the real player body, inventory, Speed and Strength.");
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            ConversationManager.EndConversation(); DragSystem.Release(actor);
            actor.GetPart<Body>().DropAllEquipment(ctx.Zone);
            foreach (var item in inventory.Objects.ToArray()) inventory.RemoveObject(item);
            inventory.EquippedItems.Clear(); inventory.MaxWeight = 150; inventory.RefreshHandlingCarryPenalty();
            actor.GetPart<StatusEffectsPart>()?.RemoveAllEffects();
            foreach (var entity in ctx.Zone.GetAllEntities().ToArray())
                if (entity != actor) { ctx.Turns.RemoveEntity(entity); ctx.Zone.RemoveEntity(entity); }
            ctx.Zone.GenReservedCells.Clear();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            {
                ctx.Zone.TileState.Clear(x, y);
                if (x > 0 && x < Zone.Width - 1 && y > 0 && y < Zone.Height - 1)
                    Place(ctx.Factory.CreateEntity("StoneFloor"), x, y);
            }
            ctx.Zone.RemoveEntity(actor); Place(actor, 20, 12);
            var strength = actor.GetStat("Strength"); strength.BaseValue = 16; strength.Bonus = strength.Penalty = strength.Boost = 0; strength.Min = 0; strength.Max = Math.Max(40, strength.Max);
            var speed = actor.GetStat("Speed"); speed.BaseValue = 100; speed.Bonus = speed.Penalty = speed.Boost = 0; speed.Min = 0; speed.Max = Math.Max(100, speed.Max);
            var hp = actor.GetStat("Hitpoints"); if (hp != null) { hp.Max = hp.BaseValue = 100; hp.Bonus = hp.Penalty = hp.Boost = 0; }
            Barrel = ctx.Factory.CreateEntity("HaulBarrel"); Place(Barrel, 21, 12);
            ctx.Turns.AddEntity(actor); ctx.Turns.ProcessUntilPlayerTurn(); MessageLog.Clear();
            ZoneRenderHooks.MarkFullDirty("GameAuditHaulingBench");
            if (Application.isPlaying) new GameObject("Hauling Lifecycle Native Audit").AddComponent<GameAuditHaulingBenchPlayer>().Initialize(ctx, this);
            void Place(Entity entity, int x, int y)
            { if (entity == null || !ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Hauling arena placement refused."); }
        }
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++; Audit.Add((passed ? "PASS " : "FAIL ") + name);
            if (!passed) throw new InvalidOperationException("Hauling native audit failed: " + name);
        }
    }
}
