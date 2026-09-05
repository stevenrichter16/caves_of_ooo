using System;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    [Scenario(name: "Olderdeep Founding Encounter", category: "World",
        description: "Native keyboard audit of one-time trust, underfoot sleep, remembered dream, cancellation and reach.")]
    public sealed class FoundingVillageBench : IScenario
    {
        public string RunId { get; private set; }
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "TheRooted", "FoundingPlume", "FoundingListener", "FoundingPlaqueTender", "Tepuibone" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Founding audit missing " + bp);
            RunId = Guid.NewGuid().ToString("N");
            // Disposable scenario arena. Never a migration of a saved floor.
            foreach (var e in ctx.Zone.GetAllEntities().ToArray())
            { ctx.Turns.RemoveEntity(e); ctx.Zone.RemoveEntity(e); }
            ctx.Zone.GenReservedCells.Clear();
            if (!new FoundingVillageBuilder().BuildZone(ctx.Zone, ctx.Factory, ctx.Rng))
                throw new InvalidOperationException("Founding audit chamber failed to author");
            NarrativeStatePart.Current = new NarrativeStatePart();
            PlayerReputation.Set("CatacombFolk", 0);
            var player = ctx.PlayerEntity;
            var hp = player.GetStat("Hitpoints"); hp.Max = 40; hp.BaseValue = 5; hp.Penalty = 0;
            if (player.GetStat("Speed") == null) player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Max = 200 };
            var inventory = player.GetPart<InventoryPart>();
            if (inventory == null) { inventory = new InventoryPart(); player.AddPart(inventory); }
            foreach (var e in inventory.Objects.ToArray()) inventory.RemoveObject(e);
            inventory.MaxWeight = 100;
            var stone = ctx.Factory.CreateEntity("Tepuibone"); stone.GetPart<StackerPart>().StackCount = 2; inventory.AddObject(stone);
            ctx.Zone.AddEntity(player, 57, 12); ctx.Turns.AddEntity(player); ctx.Turns.ProcessUntilPlayerTurn();
            foreach (var npc in ctx.Zone.GetAllEntities().Where(e => e.HasPart<BrainPart>()))
            { npc.GetPart<BrainPart>().CurrentZone = ctx.Zone; npc.GetPart<BrainPart>().Rng = ctx.Rng; ctx.Turns.AddEntity(npc); }
            ZoneRenderHooks.MarkFullDirty("FoundingVillageBench");
            if (Application.isPlaying) new GameObject("Founding Native Audit").AddComponent<FoundingVillageBenchPlayer>().Initialize(ctx, this);
        }
    }
}
