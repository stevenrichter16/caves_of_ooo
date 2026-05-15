using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// World-map traversal showcase (WM.8.3). Demonstrates the
    /// player-exercisable world map shipped across WM.2–WM.8:
    ///
    ///   1. The game boots the player into the surface ground zone
    ///      "Overworld.10.10.0" (GameBootstrap.cs:163).
    ///   2. Press <b>&lt;</b> (Shift+Comma) with no stairs underfoot →
    ///      the player ascends to the world-map zone, arriving on the
    ///      embedded cell for parasang (10,10) — the Kyakukya village
    ///      marker ('!').
    ///   3. Walk cell-to-cell with normal movement. Each step burns
    ///      10 game ticks (WorldMapTravelCostPart, auto-attached on
    ///      first ascend). Biome glyphs + POI markers are visible;
    ///      unvisited cells are dimmed (fog of war).
    ///   4. Press <b>&gt;</b> (Shift+Period) on any world-map cell →
    ///      descend into that parasang's ground zone. Descending onto
    ///      the cell you ascended from restores your exact prior
    ///      position (LastLocationOnSurface).
    ///
    /// Diag substrate (WM.7): every ascend / descend / step records a
    /// <c>worldmap/Ascended</c>, <c>worldmap/Descended</c>, or
    /// <c>worldmap/Stepped</c> entry, queryable via the
    /// <c>diag_query</c> MCP tool — useful for verifying the
    /// from/to coordinates and the usedSavedLocation flag.
    ///
    /// <para>The scenario itself is minimal — it just gives the player
    /// a sturdy loadout and prints the keybindings. The actual world
    /// map is constructed on demand by OverworldZoneManager the first
    /// time the player presses <c>&lt;</c>.</para>
    /// </summary>
    [Scenario(
        name: "World Map Showcase",
        category: "World",
        description: "Press < to ascend to the world map, walk cell-to-cell (10 ticks/step), press > to descend. Demonstrates WM.2–WM.8: worldmap zone, ascend/descend, travel cost, fog-of-war, POI markers, worldmap/* diag.")]
    public class WorldMapShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Sturdy loadout so the player can wander without dying —
            // the worldmap has no combat but ground zones do.
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 20);

            MessageLog.Add("World Map Showcase:");
            MessageLog.Add("  Press < (Shift+Comma) to ascend to the world map.");
            MessageLog.Add("  Walk around — each step is 10 game ticks.");
            MessageLog.Add("  ! = village, & = lair, $ = merchant, ~ = river.");
            MessageLog.Add("  Press > (Shift+Period) on a cell to descend into it.");
            MessageLog.Add("  Descending where you came from returns you exactly.");
        }
    }
}
