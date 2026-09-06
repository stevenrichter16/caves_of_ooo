using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable native shortcut and75second input audit.</summary>
    [Scenario(name: "Shortcut Audit", category: "Items",
        description: "Exercise visible keys, reserved controls and held-key handoffs across pickup, containers, world actions and dialogue.")]
    public sealed class GameAuditShortcutBench : IScenario
    {
        public string RunId { get; private set; }
        public readonly List<Entity> Ground = new List<Entity>();
        public readonly List<Entity> Sacks = new List<Entity>();
        public Entity Glimmer, Still, Chest, Speaker;
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "Sack", "Dagger", "SilverSand", "FireClay", "WardOil", "HealingTonic", "CandyCarrotSeed", "SteelBladeComponent", "AlchemyStill", "BrewedTonic", "StoneFloor", "GlimmerBrine", "Chest", "Villager" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Shortcut audit missing blueprint: " + bp);
            if (ctx.PlayerEntity.GetPart<InventoryPart>() == null || ctx.PlayerEntity.GetPart<Body>() == null)
                throw new InvalidOperationException("Shortcut audit needs player inventory and body.");
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
            Ground.Clear(); Sacks.Clear();
            string[] blueprints = { "Dagger", "SilverSand", "FireClay", "WardOil", "HealingTonic", "CandyCarrotSeed", "SteelBladeComponent" };
            foreach (string bp in blueprints)
            { var item=Give(bp,1); if(!InventorySystem.Drop(ctx.PlayerEntity,item,ctx.Zone))throw new InvalidOperationException("Actual ground drop refused."); Ground.Add(item); }
            foreach (string bp in blueprints)
            { var sack=ctx.Factory.CreateEntity("Sack"); Place(sack,24,12); if(!sack.GetPart<ContainerPart>().AddItem(ctx.Factory.CreateEntity(bp)))throw new InvalidOperationException("Crowded container content refused."); Sacks.Add(sack); }
            Glimmer=Give("GlimmerBrine",3); Still=ctx.Factory.CreateEntity("AlchemyStill"); Place(Still,21,12);
            Chest=ctx.Factory.CreateEntity("Chest"); Place(Chest,19,12);
            Speaker=ctx.Factory.CreateEntity("Villager"); Speaker.RemovePart(Speaker.GetPart<InventoryPart>()); Speaker.RemovePart(Speaker.GetPart<TraderPart>());
            Speaker.GetPart<ConversationPart>().ConversationID="ShortcutBenchDialogue"; Place(Speaker,20,11);
            var conversation=new CavesOfOoo.Data.ConversationData{ID="ShortcutBenchDialogue"};
            conversation.Nodes.Add(new CavesOfOoo.Data.NodeData{ID="Start",Text="This staged conversation has nine ordinary choices plus the automatic attack choice. Let the letters finish or reveal them with a choice key.",Choices=Enumerable.Range(0,9).Select(i=>new CavesOfOoo.Data.ChoiceData{Text="Choice "+i,Target="End"}).ToList()});
            CavesOfOoo.Data.ConversationLoader.Register(conversation);
            BrewRuleRegistry.ResetForTests(); BrewRuleRegistry.EnsureInitialized(); AlchemyStillPart.Factory=ctx.Factory;
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn(); MessageLog.Clear();
            ZoneRenderHooks.MarkFullDirty("GameAuditShortcutBench");
            if (Application.isPlaying) new GameObject("Shortcut Native Audit").AddComponent<GameAuditShortcutBenchPlayer>().Initialize(ctx, this);
            Entity Give(string blueprint, int quantity)
            {
                var item = ctx.Factory.CreateEntity(blueprint); item.GetPart<StackerPart>().StackCount = quantity;
                if (!inventory.AddObject(item)) throw new InvalidOperationException("Audit reagent insertion refused: " + blueprint); return item;
            }
            void Place(Entity entity, int x, int y)
            { if (!ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Shortcut arena placement refused."); }
        }
        /// <summary>Record actual observations without changing diagnostic preferences.</summary>
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++; Audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool enabled = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "ShortcutNativeAudit", payload: new { runId = RunId, name, passed }); }
            finally { Diag.SetChannel("scenario", enabled); }
            if (!passed) throw new InvalidOperationException("Shortcut native audit failed: " + name);
        }
    }
}
