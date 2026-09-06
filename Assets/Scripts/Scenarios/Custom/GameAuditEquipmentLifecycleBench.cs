using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disposable actual-content fixture. Mod application, HP and veto flags
    /// are explicit setup. Equipment, F8 injury, saves and trap walking use native keys.</summary>
    [Scenario(name: "Equipment Lifecycle Audit", category: "Items",
        description: "Equip real modified gear, veto and accept developer injury, save/load, and step on an actual trap.")]
    public sealed class GameAuditEquipmentLifecycleBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity Buckler, Dagger, Boots, Trap;
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            foreach (string bp in new[] { "Buckler", "Dagger", "IronshodBoots", "SpikeTrap", "StoneFloor" })
                if (!ctx.Factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Equipment audit missing blueprint: " + bp);
            var actor = ctx.PlayerEntity; var inventory = actor.GetPart<InventoryPart>(); var body = actor.GetPart<Body>();
            if (inventory == null || body == null || new[] { "Speed", "Strength", "Agility", "Hitpoints" }.Any(s => actor.GetStat(s) == null))
                throw new InvalidOperationException("Equipment audit requires the real player body, inventory and stats.");
            if (body.DismemberedParts.Count != 0 || !body.GetParts().Any(p => p.Type == "Hand" && p.GetLaterality() == Laterality.LEFT)
                || !body.GetParts().Any(p => p.Type == "Hand" && p.GetLaterality() == Laterality.RIGHT))
                throw new InvalidOperationException("Equipment audit requires a fresh standard Humanoid.");
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            ConversationManager.EndConversation(); DragSystem.Release(actor); body.DropAllEquipment(ctx.Zone);
            foreach (var item in inventory.Objects.ToArray()) inventory.RemoveObject(item);
            inventory.EquippedItems.Clear(); inventory.MaxWeight = 150; inventory.RefreshHandlingCarryPenalty();
            actor.GetPart<StatusEffectsPart>()?.RemoveAllEffects();
            foreach (var entity in ctx.Zone.GetAllEntities().ToArray())
                if (entity != actor) { ctx.Turns.RemoveEntity(entity); ctx.Zone.RemoveEntity(entity); }
            ctx.Zone.GenReservedCells.Clear();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            {
                ctx.Zone.TileState.Clear(x, y);
                if (x > 0 && x < Zone.Width - 1 && y > 0 && y < Zone.Height - 1) Place(ctx.Factory.CreateEntity("StoneFloor"), x, y);
            }
            ctx.Zone.RemoveEntity(actor); Place(actor, 20, 12);
            ResetStat("Strength", 16, 40); ResetStat("Agility", 16, 40); ResetStat("Speed", 100, 100); ResetStat("Hitpoints", 100, 100);
            actor.GetStat("Speed").Penalty = 7; // Unrelated fixture penalty must survive equipment cleanup.
            actor.SetIntProperty("MobilityPenalty", 0, removeIfZero: true);
            Buckler = ctx.Factory.CreateEntity("Buckler"); Dagger = ctx.Factory.CreateEntity("Dagger"); Boots = ctx.Factory.CreateEntity("IronshodBoots");
            Check("fixture_actual_singletons", new[] { Buckler, Dagger, Boots }.All(e => e != null && (e.GetPart<StackerPart>()?.StackCount ?? 1) == 1));
            Check("fixture_real_duelist_mod", new DuelistCutTinkerModification().Apply(Buckler, out var reason));
            Check("fixture_real_glowquartz_mod", new GlowQuartzTinkerModification().Apply(Dagger, out reason));
            foreach (var item in new[] { Buckler, Dagger, Boots }) Check("fixture_carried_" + item.BlueprintName, inventory.AddObject(item) && inventory.Objects.Contains(item));
            Trap = ctx.Factory.CreateEntity("SpikeTrap"); Place(Trap, 21, 12);
            ctx.Turns.AddEntity(actor); ctx.Turns.ProcessUntilPlayerTurn(); MessageLog.Clear();
            ZoneRenderHooks.MarkFullDirty("GameAuditEquipmentLifecycleBench");
            if (Application.isPlaying) new GameObject("Equipment Lifecycle Native Audit").AddComponent<GameAuditEquipmentLifecycleBenchPlayer>().Initialize(ctx, this);
            void Place(Entity entity, int x, int y)
            { if (entity == null || !ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Equipment arena placement refused."); }
            void ResetStat(string name, int value, int maximum)
            { var s = actor.GetStat(name); s.BaseValue = value; s.Bonus = s.Penalty = s.Boost = 0; s.Min = 0; s.Max = maximum; }
        }
        public void Check(string name, bool passed)
        {
            Cases++; if (!passed) Failures++; Audit.Add((passed ? "PASS " : "FAIL ") + name);
            if (!passed) throw new InvalidOperationException("Equipment native audit failed: " + name);
        }
    }

    /// <summary>Explicit scenario probe. Removed before every checkpoint; no custom save contract.</summary>
    public sealed class GameAuditEquipmentVetoPart : Part
    {
        public override string Name => nameof(GameAuditEquipmentVetoPart);
        public bool VetoDismember;
        public int BeforeCount, LastPartID, AfterCount;
        public bool ObservedCleared = true;
        private readonly List<Entity> _unequipped = new List<Entity>();
        public int CountFor(Entity item) => _unequipped.Count(e => ReferenceEquals(e, item));
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeDismember")
            { BeforeCount++; LastPartID = e.GetParameter<BodyPart>("Part")?.ID ?? -1; return !VetoDismember; }
            if (e.ID == "AfterUnequip")
            {
                AfterCount++; var item = e.GetParameter<Entity>("Item"); _unequipped.Add(item);
                var inventory = ParentEntity.GetPart<InventoryPart>(); var body = ParentEntity.GetPart<Body>();
                ObservedCleared &= item != null && !inventory.EquippedItems.Values.Any(value => ReferenceEquals(value, item))
                    && !body.GetParts().Any(part => ReferenceEquals(part._Equipped, item)) && item.GetPart<PhysicsPart>()?.Equipped == null;
            }
            return true;
        }
    }
}
