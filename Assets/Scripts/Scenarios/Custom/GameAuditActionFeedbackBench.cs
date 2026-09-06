using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable crafting controls and75second UI performance audit.</summary>
    [Scenario(name: "Action Feedback Audit", category: "Items",
        description: "Refuse, repair and retry container, tinker, forge and brew actions through native controls.")]
    public sealed class GameAuditActionFeedbackBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity Blade, Haft, Binding, Replacement, Glimmer, Spark, Station;
        public Entity Sack, CarriedDagger, EquippedDagger, Filler, Moss;
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "Sack", "Dagger", "FireMoss", "Torch", "SilverSand", "FireClay", "WardOil", "HealingTonic", "OvenBuildersGuide", "TinkersForge", "AlchemyStill", "BrewedTonic", "StoneFloor", "ForgedWeapon", "SteelBladeComponent", "OakHaftComponent", "LeatherBindingComponent", "IronSpikeComponent", "GlimmerBrine", "SparkRoot" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Crafting flow audit missing blueprint: " + bp);
            if (ctx.PlayerEntity.GetPart<InventoryPart>() == null || ctx.PlayerEntity.GetPart<Body>() == null)
                throw new InvalidOperationException("Crafting flow audit needs player inventory and body.");
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
            Moss = Give("FireMoss", 1);
            Sack = ctx.Factory.CreateEntity("Sack"); Place(Sack, 20, 12);
            foreach (string blueprint in new[] { "Torch", "SilverSand", "FireClay", "WardOil", "HealingTonic", "OvenBuildersGuide" })
            {
                var item = ctx.Factory.CreateEntity(blueprint);
                if (!Sack.GetPart<ContainerPart>().AddItem(item)) throw new InvalidOperationException("Sack staging refused.");
                if (blueprint == "SilverSand") Filler = item;
            }
            CarriedDagger = Give("Dagger", 3);
            if (!InventorySystem.Equip(ctx.PlayerEntity, CarriedDagger)) throw new InvalidOperationException("Dagger staging could not equip one unit.");
            EquippedDagger = inventory.GetAllEquipped().Distinct().Single();
            var bits = ctx.PlayerEntity.GetPart<BitLockerPart>();
            if (bits == null) { bits = new BitLockerPart(); ctx.PlayerEntity.AddPart(bits); }
            TinkerRecipeRegistry.ResetForTests(); TinkerRecipeRegistry.EnsureInitialized();
            bits.RestoreBitsAndRecipes(new Dictionary<char, int> { ['C'] = 1 }, new[] { "craft_dagger" });
            Station = ctx.Factory.CreateEntity("TinkersForge"); Place(Station, 21, 12);
            Place(ctx.Factory.CreateEntity("AlchemyStill"), 19, 12);
            BrewRuleRegistry.ResetForTests(); BrewRuleRegistry.EnsureInitialized();
            ForgePart.Factory = ctx.Factory; AlchemyStillPart.Factory = ctx.Factory;
            ctx.Turns.AddEntity(ctx.PlayerEntity); ctx.Turns.ProcessUntilPlayerTurn(); MessageLog.Clear();
            ZoneRenderHooks.MarkFullDirty("GameAuditActionFeedbackBench");
            if (Application.isPlaying) new GameObject("Action Feedback Native Audit").AddComponent<GameAuditActionFeedbackBenchPlayer>().Initialize(ctx, this);
            Entity Give(string blueprint, int quantity)
            {
                var item = ctx.Factory.CreateEntity(blueprint); item.GetPart<StackerPart>().StackCount = quantity;
                if (!inventory.AddObject(item)) throw new InvalidOperationException("Audit reagent insertion refused: " + blueprint); return item;
            }
            void Place(Entity entity, int x, int y)
            { if (!ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Crafting flow arena placement refused."); }
        }
        /// <summary>Record actual observations without changing diagnostic preferences.</summary>
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++; Audit.Add(name + ":" + (passed ? "PASS" : "FAIL"));
            bool enabled = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            try { Diag.Record("scenario", "ActionFeedbackNativeAudit", payload: new { runId = RunId, name, passed }); }
            finally { Diag.SetChannel("scenario", enabled); }
            if (!passed) throw new InvalidOperationException("Crafting flow native audit failed: " + name);
        }
    }
}
