using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Supported authoring/API fixture, not shipped loadout content. Uses
    /// real item blueprints and factory ObjectCreated, with isolated actor copies.</summary>
    [Scenario(name: "Loadout Lifecycle API Audit", category: "Items",
        description: "Verify fixture-authored loadout hooks, refusals, cleanup and complete saved graphs through APIs.")]
    public sealed class GameAuditLoadoutLifecycleBench : IScenario
    {
        public string RunId { get; private set; }
        public Entity EquippedActor, CarriedActor, VetoActor, NoBodyActor, OccupiedActor;
        public EntityFactory OriginalLoadoutFactory;
        public System.Random OriginalLoadoutRng;
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit = new List<string>();
        public void Apply(ScenarioContext ctx)
        {
            if (Application.isPlaying && string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Launch this audit through its isolated editor batch.");
            RunId = Guid.NewGuid().ToString("N"); Cases = Failures = 0; Audit.Clear();
            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            foreach (string bp in new[] { "Player", "Buckler", "Dagger", "IronshodBoots", "StoneFloor" })
                if (!factory.Blueprints.ContainsKey(bp)) throw new InvalidOperationException("Loadout audit missing actual blueprint: " + bp);
            factory.RegisterPartType<GameAuditLoadoutProbePart>(); factory.RegisterPartType<GameAuditLoadoutModSeedPart>();
            factory.Blueprints["Buckler"].Parts[nameof(GameAuditLoadoutModSeedPart)] = new Dictionary<string, string> { ["Duelist"] = "true" };
            factory.Blueprints["Dagger"].Parts[nameof(GameAuditLoadoutModSeedPart)] = new Dictionary<string, string> { ["Glow"] = "true" };
            OriginalLoadoutFactory = LoadoutPart.Factory; OriginalLoadoutRng = LoadoutPart.Rng;
            try
            {
                LoadoutPart.Factory = factory; LoadoutPart.Rng = new System.Random(7307);
                EquippedActor = CreateActor("Equipped", "IronshodBoots;Buckler;Dagger", "", false, false, false);
                CarriedActor = CreateActor("Carried", "", "IronshodBoots;Buckler;Dagger", false, false, false);
                VetoActor = CreateActor("Vetoed", "IronshodBoots;Buckler;Dagger", "", true, false, false);
                NoBodyActor = CreateActor("NoBody", "IronshodBoots;Buckler;Dagger", "", false, true, false);
                OccupiedActor = CreateActor("Occupied", "IronshodBoots", "", false, false, true);
            }
            finally { LoadoutPart.Factory = OriginalLoadoutFactory; LoadoutPart.Rng = OriginalLoadoutRng; }
            // The private factory has its own numeric ID counter. Give only owned
            // fixture entities unique opaque IDs before mixing them into the real graph.
            var owned = new HashSet<Entity>();
            foreach (var actor in Actors()) { owned.Add(actor); foreach (var item in OwnedItems(actor)) owned.Add(item); }
            foreach (var entity in owned) entity.ID = "GA03g-" + Guid.NewGuid().ToString("N");
            ConversationManager.EndConversation(); DragSystem.Release(ctx.PlayerEntity);
            foreach (var entity in ctx.Zone.GetAllEntities().ToArray())
                if (entity != ctx.PlayerEntity) { ctx.Turns.RemoveEntity(entity); ctx.Zone.RemoveEntity(entity); }
            ctx.Zone.GenReservedCells.Clear();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            {
                ctx.Zone.TileState.Clear(x, y);
                if (x > 0 && x < Zone.Width - 1 && y > 0 && y < Zone.Height - 1) Place(ctx.Factory.CreateEntity("StoneFloor"), x, y);
            }
            ctx.Zone.RemoveEntity(ctx.PlayerEntity); Place(ctx.PlayerEntity, 20, 12);
            int actorX = 24;
            foreach (var actor in Actors()) { Place(actor, actorX++, 10); ctx.Turns.AddEntity(actor); }
            ctx.Turns.ProcessUntilPlayerTurn(); ZoneRenderHooks.MarkFullDirty("GameAuditLoadoutLifecycleBench");
            if (Application.isPlaying) new GameObject("Loadout Lifecycle API Native Audit").AddComponent<GameAuditLoadoutLifecycleBenchPlayer>().Initialize(ctx, this);

            Entity CreateActor(string label, string equip, string carry, bool veto, bool noBody, bool occupied)
            {
                var blueprint = Copy(factory.Blueprints["Player"], "GA03g" + label);
                // Inert actor copies: keep real storage/anatomy/physics/render templates,
                // remove player/AI responsibilities, and supply explicit fixture stats.
                var keep = new HashSet<string> { "Physics", "Render", "Body", "Inventory" };
                blueprint.Parts = blueprint.Parts.Where(p => keep.Contains(p.Key)).ToDictionary(p => p.Key, p => p.Value);
                blueprint.Tags.Remove("Player"); blueprint.Props["Anatomy"] = "Humanoid";
                blueprint.Parts["Inventory"]["MaxWeight"] = "150";
                blueprint.Parts["Render"]["DisplayName"] = "loadout API " + label.ToLowerInvariant();
                blueprint.Stats["Agility"] = new StatBlueprint { Name = "Agility", Value = 16, Min = 0, Max = 40 };
                blueprint.Stats["Speed"] = new StatBlueprint { Name = "Speed", Value = 100, Min = 0, Max = 100 };
                blueprint.Stats["Hitpoints"] = new StatBlueprint { Name = "Hitpoints", Value = 100, Min = 0, Max = 100 };
                if (noBody) blueprint.Parts.Remove("Body");
                blueprint.Parts[nameof(GameAuditLoadoutProbePart)] = new Dictionary<string, string> { ["VetoEquip"] = veto.ToString(), ["PreEquipBoots"] = occupied.ToString() };
                blueprint.Parts["Loadout"] = new Dictionary<string, string> { ["Equip"] = equip, ["Carry"] = carry, ["Pick"] = "" };
                factory.Blueprints[blueprint.Name] = blueprint;
                var actor = factory.CreateEntity(blueprint.Name); if (actor == null) throw new InvalidOperationException("Fixture actor creation failed.");
                actor.SetIntProperty("GA03gSavedMarker", 37); return actor;
            }
            void Place(Entity entity, int x, int y)
            { if (entity == null || !ctx.Zone.AddEntity(entity, x, y)) throw new InvalidOperationException("Loadout audit placement refused."); }
        }
        public Entity[] Actors() => new[] { EquippedActor, CarriedActor, VetoActor, NoBodyActor, OccupiedActor };
        public static Entity[] OwnedItems(Entity actor)
        { var inventory = actor.GetPart<InventoryPart>(); return inventory.Objects.Concat(inventory.EquippedItems.Values).Distinct().ToArray(); }
        private static Blueprint Copy(Blueprint source, string name)
        {
            return new Blueprint { Name = name, Inherits = source.Inherits, Baked = true,
                Parts = source.Parts.ToDictionary(p => p.Key, p => new Dictionary<string, string>(p.Value)),
                Stats = source.Stats.ToDictionary(p => p.Key, p => new StatBlueprint { Name = p.Value.Name, Value = p.Value.Value, Min = p.Value.Min, Max = p.Value.Max, Boost = p.Value.Boost, sValue = p.Value.sValue }),
                Tags = new Dictionary<string, string>(source.Tags), Props = new Dictionary<string, string>(source.Props), IntProps = new Dictionary<string, int>(source.IntProps) };
        }
        public void Check(string name, bool passed)
        { Cases++; if (!passed) Failures++; Audit.Add((passed ? "PASS " : "FAIL ") + name); if (!passed) throw new InvalidOperationException("Loadout API audit failed: " + name); }
    }

    /// <summary>Fixture item creation uses real modification APIs; loading must not replay it.</summary>
    public sealed class GameAuditLoadoutModSeedPart : Part
    {
        public override string Name => nameof(GameAuditLoadoutModSeedPart);
        public bool Duelist, Glow;
        public int CreatedCount;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "ObjectCreated") return true;
            CreatedCount++;
            if (Duelist && !new DuelistCutTinkerModification().Apply(ParentEntity, out var reason)) throw new InvalidOperationException("Duelist fixture refused: " + reason);
            if (Glow && !new GlowQuartzTinkerModification().Apply(ParentEntity, out var glowReason)) throw new InvalidOperationException("Glow fixture refused: " + glowReason);
            return true;
        }
    }

    /// <summary>Saved public counters prove creation/equip is not replayed on load.</summary>
    public sealed class GameAuditLoadoutProbePart : Part
    {
        public override string Name => nameof(GameAuditLoadoutProbePart);
        public bool VetoEquip, PreEquipBoots;
        public int CreatedCount, BeforeEquipCount, AfterEquipCount, AfterUnequipCount;
        public Entity InitialEquipment;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "ObjectCreated")
            {
                CreatedCount++;
                if (PreEquipBoots)
                {
                    InitialEquipment = LoadoutPart.Factory.CreateEntity("IronshodBoots");
                    if (InitialEquipment == null || !ParentEntity.GetPart<InventoryPart>().AddObject(InitialEquipment) || !InventorySystem.Equip(ParentEntity, InitialEquipment))
                        throw new InvalidOperationException("Occupied-slot fixture could not equip its initial boots.");
                }
            }
            if (e.ID == "BeforeEquip") { BeforeEquipCount++; return !VetoEquip; }
            if (e.ID == "AfterEquip") AfterEquipCount++;
            if (e.ID == "AfterUnequip") AfterUnequipCount++;
            return true;
        }
    }
}
