using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Disclosed controlled setup for the isolated native item/AI audit.
    /// Never an ordinary generated encounter. All later actions use native keys.</summary>
    [Scenario(name: "Danger Aware Approach Fixture", category: "AI Behavior",
        description: "Controlled clear-ground pursuit with one finite frog oil. Use the isolated Danger Movement native audit launcher.")]
    public sealed class DangerAwareApproachScenario : IScenario
    {
        public Entity Actor { get; private set; }
        public Entity Oil { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int RemovedOwners { get; private set; }
        public void Apply(ScenarioContext ctx)
        {
            if (Actor != null || Oil != null || string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("This controlled fixture requires a fresh isolated native audit.");
            var at = ctx.Zone.GetEntityPosition(ctx.PlayerEntity); X = at.x; Y = at.y;
            if (!ctx.Zone.InBounds(X - 5, Y - 4) || !ctx.Zone.InBounds(X + 5, Y + 4))
                throw new InvalidOperationException("The controlled pursuit rectangle does not fit around the player.");
            var remove = new HashSet<Entity>();
            for (int y = Y - 4; y <= Y + 4; y++)
                for (int x = X - 5; x <= X + 5; x++)
                    foreach (var owner in ctx.Zone.GetCell(x, y).Occupants)
                        if (owner != ctx.PlayerEntity) remove.Add(owner);
            foreach (var owner in remove)
            {
                ctx.Zone.RemoveEntity(owner);
                if (owner.HasTag("Creature")) ctx.Turns.RemoveEntity(owner);
            }
            RemovedOwners = remove.Count;
            for (int y = Y - 4; y <= Y + 4; y++)
                for (int x = X - 5; x <= X + 5; x++)
                {
                    ctx.Zone.TileState.Clear(x, y);
                    if (ctx.World.PlaceObject("Grass").At(x, y) == null)
                        throw new InvalidOperationException("Controlled pursuit ground missing.");
                    if ((x == X - 5 || x == X + 5 || y == Y - 4 || y == Y + 4)
                        && ctx.World.PlaceObject("StoneWall").At(x, y) == null)
                        throw new InvalidOperationException("Controlled pursuit boundary missing.");
                }
            // Standard scenario registration wires the factory Brain and native
            // scheduler. No goals, target, health, energy or behavior overrides.
            Actor = ctx.Spawn("MarlbackScrabbler").At(X + 2, Y);
            Oil = ctx.Factory.CreateEntity("FrogOil");
            if (Actor == null || Actor.GetStatValue("Hitpoints") != 15 || Actor.GetStat("Hitpoints")?.Max != 15
                || Actor.GetPart<BrainPart>() == null || Actor.HasPart<CombatTacticsPart>()
                || !FactionManager.IsHostile(Actor, ctx.PlayerEntity) || Oil == null
                || !ctx.PlayerEntity.GetPart<InventoryPart>().AddObject(Oil))
                throw new InvalidOperationException("Original enemy and finite oil differ from the controlled fixture contract.");
            ctx.Zone.MarkDoorOcclusionChanged();
            ZoneRenderHooks.MarkFullDirty("ControlledDangerMovement.Setup");
            ctx.Log("CONTROLLED FIXTURE: enclosed clear ground, one full-health original marlback and one finite frog oil. Setup changed this isolated zone and added that one item; subsequent movement, oil use and save/load use native input.");
        }
    }
}
