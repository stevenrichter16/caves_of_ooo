using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Controlled acceptance setup, never a naturally generated encounter.
    /// The isolated native launcher applies this once; all subsequent actions use keys.</summary>
    [Scenario(name: "Fair Retreat Door Fixture", category: "AI Behavior",
        description: "Controlled wounded-enemy and door setup. Use the isolated Fair Retreat native audit launcher; not ordinary exploration.")]
    public sealed class FairRetreatDoorScenario : IScenario
    {
        public Entity Actor { get; private set; }
        public Entity Door { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int RemovedOwners { get; private set; }

        public void Apply(ScenarioContext ctx)
        {
            if (Actor != null || string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("This controlled fixture requires a fresh isolated native audit.");
            var at = ctx.Zone.GetEntityPosition(ctx.PlayerEntity); X = at.x; Y = at.y;
            if (!ctx.Zone.InBounds(X - 12, Y - 6) || !ctx.Zone.InBounds(X + 3, Y + 6))
                throw new InvalidOperationException("The controlled lane does not fit around the current player.");
            // Disclosed fixture mutation: remove all overlapping owners, including
            // terrain, in one enclosed rectangle. Preserve the original player.
            // ClearCell alone intentionally preserves terrain and cannot ensure this lane.
            var remove = new HashSet<Entity>();
            for (int y = Y - 6; y <= Y + 6; y++)
                for (int x = X - 12; x <= X + 3; x++)
                    foreach (var owner in ctx.Zone.GetCell(x, y).Occupants)
                        if (owner != ctx.PlayerEntity) remove.Add(owner);
            foreach (var owner in remove)
            {
                ctx.Zone.RemoveEntity(owner);
                if (owner.HasTag("Creature")) ctx.Turns.RemoveEntity(owner);
            }
            RemovedOwners = remove.Count;
            for (int y = Y - 6; y <= Y + 6; y++)
                for (int x = X - 12; x <= X + 3; x++)
                {
                    ctx.Zone.TileState.Clear(x, y);
                    if (ctx.World.PlaceObject("Grass").At(x, y) == null)
                        throw new InvalidOperationException("Controlled lane ground missing.");
                    bool perimeter = x == X - 12 || x == X + 3 || y == Y - 6 || y == Y + 6;
                    bool divider = x == X - 1 && y != Y;
                    if ((perimeter || divider) && ctx.World.PlaceObject("StoneWall").At(x, y) == null)
                        throw new InvalidOperationException("Controlled sight barrier missing.");
                }
            Door = ctx.World.PlaceObject("VillageDoor").At(X - 1, Y);
            Actor = ctx.Spawn("MarlbackScrabbler").WithHp(.2f).At(X - 6, Y);
            if (Door?.GetPart<DoorPart>()?.IsClosed != false || Actor == null
                || Actor.GetStatValue("Hitpoints") != 3 || Actor.GetStat("Hitpoints")?.Max != 15
                || Actor.GetPart<BrainPart>() == null || !FactionManager.IsHostile(Actor, ctx.PlayerEntity))
                throw new InvalidOperationException("Original factory actor/door differs from the controlled fixture contract.");
            ctx.Zone.MarkDoorOcclusionChanged();
            ZoneRenderHooks.MarkFullDirty("ControlledFairRetreat.Setup");
            ctx.Log("CONTROLLED FIXTURE: enclosed lane, open door and one wounded original marlback. Setup changed this isolated zone; subsequent retreat, door actions and save/load use native input.");
        }
    }
}
