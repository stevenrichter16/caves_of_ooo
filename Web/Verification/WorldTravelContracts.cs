using System.Reflection;
using System.Text.Json.Nodes;
using CavesOfOoo.Core;
using CavesOfOoo.Web;

internal static partial class Program
{
    private static void RunWorldTravelContracts()
    {
        Run("world travel exposes actual coordinates while reading the map stays free", WorldMapPanelIsFree);
        Run("surface to world map and back pays one turn each and retains the exact square", WorldTravelRoundTrip);
        Run("one world-map step pays the native surcharge and reaches persistent Sill", WorldTravelToSill);
        Run("stairs away from an entrance use native world-map vertical fallback", WorldTravelStairsFallback);
        Run("underground travel cannot skip local stairs or spend a refused turn", UndergroundWorldTravelIsRefused);
        Run("stale and inapplicable travel cannot change location or spend a turn", StaleWorldTravelIsRefused);
        Run("all four world-map borders refuse movement without damage or a turn", WorldMapBordersAreFree);
        Run("saving on the world map preserves coordinates and the return location", WorldMapCheckpoint);
        Run("world-map cells are bounded and expose native live names and visited flags", WorldMapCellSnapshot);
        Run("unvisited sinkhole names remain hidden until native discovery", WorldMapHiddenSinkholes);
    }

    private static JsonObject NewTravelGame() => Parse(Game.NewGame("classic", 8128, "World travel contract"));
    private static JsonObject Travel(string destination, int? revision = null) => Send("travel", new() { ["value"] = destination }, revision);
    private static OverworldZoneManager TravelZones() => typeof(BrowserGame).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(field => field.FieldType == typeof(OverworldZoneManager)).GetValue(Game) as OverworldZoneManager
        ?? throw new Exception("The travel contract needs the actual overworld manager.");
    private static void TravelFlags(JsonObject state, bool enter, bool leave)
    {
        var travel = state["travel"] as JsonObject ?? throw new Exception("Missing travel capabilities.");
        Check(Boolean(travel, "canEnter") == enter && Boolean(travel, "canLeave") == leave, "Travel capabilities disagree with the active location.");
    }
    private static void AtWorld(JsonObject state, int x, int y, bool onMap)
    {
        Check(Boolean(state, "isWorldMap") == onMap, "The view reports the wrong map mode.");
        Check(Number(state, "worldX") == x && Number(state, "worldY") == y, "The view reports incorrect logical world coordinates.");
        Check(!string.IsNullOrWhiteSpace(Text(state, "biome")), "The current native biome must be exposed.");
        Number(state, "depth");
        Check(WorldMap.IsWorldMapZoneID(CurrentZone(Player()).ZoneID) == onMap, "The view disagrees with the actual active zone.");
    }
    private static JsonObject EnterWorldMap()
    {
        var before = State(); var mapped = Travel("world"); RequireSuccess(mapped, "Entering the world map");
        AtWorld(mapped, 11, 10, true);
        Check(Number(mapped, "x") == 41 && Number(mapped, "y") == 13, "Canonical glade must enter embedded world cell41,13.");
        Check(Number(mapped, "tick") == Number(before, "tick") + 10, "Entering the world map must pay exactly one native turn.");
        TravelFlags(mapped, false, true); return mapped;
    }
    private static void WorldMapPanelIsFree()
    {
        var before = NewTravelGame(); AtWorld(before, 11, 10, false);
        Check(Number(before, "depth") == 0, "The canonical glade is on the surface.");
        TravelFlags(before, true, false);
        var panel = Send("panel", new() { ["value"] = "map" }); RequireSuccess(panel, "Reading the map");
        SamePlayerState(before, panel, "Reading the map"); AtWorld(panel, 11, 10, false);
        Check(Text(panel, "panel") == "map", "The map reader must remain a free panel.");
        SamePlayerState(before, Send("close"), "Closing the map");
    }
    private static void WorldTravelRoundTrip()
    {
        var original = NewTravelGame(); var surface = CurrentZone(Player());
        var inactiveCreatures = surface.GetEntitiesWithTag("Creature").Where(e => e != Player()).ToArray();
        var mapped = EnterWorldMap();
        Check(inactiveCreatures.All(e => !TurnManager.Active.IsRegistered(e)), "Surface NPCs must leave the active scheduler while traveling on the map.");
        var memory = Player().GetPart<WorldMapPart>();
        Check(memory != null && memory.LastZoneIDOnSurface == Text(original, "zone")
            && memory.LastZoneX == Number(original, "x") && memory.LastZoneY == Number(original, "y"), "Native surface return memory was not retained.");
        var returned = Travel("local"); RequireSuccess(returned, "Descending to the same surface"); AtWorld(returned, 11, 10, false);
        Check(Text(returned, "zone") == Text(original, "zone") && Number(returned, "x") == Number(original, "x")
            && Number(returned, "y") == Number(original, "y"), "Descending to the same chunk lost the exact previous square.");
        Check(Number(returned, "tick") == Number(mapped, "tick") + 10, "Descending must pay exactly one turn.");
        Check(ReferenceEquals(surface, CurrentZone(Player())), "World travel replaced the retained surface instance.");
        Check(TurnManager.Active.WaitingForInput, "World travel did not settle at player input."); TravelFlags(returned, true, false);
    }
    private static void WorldTravelToSill()
    {
        NewTravelGame(); var mapped = EnterWorldMap();
        var west = Send("step", new() { ["x"] = -1, ["y"] = 0 }); RequireSuccess(west, "Walking west on the world map"); AtWorld(west, 10, 10, true);
        Check(Number(west, "x") == 40 && Number(west, "y") == 13, "One westward world step reached the wrong embedded cell.");
        Check(Number(west, "tick") == Number(mapped, "tick") + 20, "A world step must include the native extra10ticks plus its normal10tick turn.");
        var sill = Travel("local"); RequireSuccess(sill, "Entering Sill"); AtWorld(sill, 10, 10, false);
        Check(Text(sill, "zone") == "Overworld.10.10.0" && Text(sill, "zoneName").Contains("Sill", StringComparison.Ordinal), "The westward route did not reach the actual named settlement.");
        Check(Number(sill, "tick") == Number(west, "tick") + 10 && Number(sill, "depth") == 0, "Entering Sill must pay one surface-arrival turn.");
        var identity = InventoryIdentity(); var checkpoint = Game.Save(); Send("wait");
        var restored = Parse(Game.Load(checkpoint)); SamePlayerState(sill, restored, "Restoring Sill"); AtWorld(restored, 10, 10, false);
        Check(InventoryIdentity() == identity, "Sill save/load changed carried identities.");
    }
    private static void WorldTravelStairsFallback()
    {
        var original = NewTravelGame();
        var mapped = Send("stairs", new() { ["value"] = "up" }); RequireSuccess(mapped, "Ascending from surface away from stairs"); AtWorld(mapped, 11, 10, true);
        Check(Number(mapped, "tick") == Number(original, "tick") + 10, "The up shortcut must charge one world-travel turn.");
        var local = Send("stairs", new() { ["value"] = "down" }); RequireSuccess(local, "Descending from the map"); AtWorld(local, 11, 10, false);
        Check(Number(local, "tick") == Number(mapped, "tick") + 10 && Number(local, "x") == Number(original, "x")
            && Number(local, "y") == Number(original, "y"), "The down shortcut lost the saved square or native timing.");
    }
    private static void UndergroundWorldTravelIsRefused()
    {
        NewTravelGame(); WalkTo(GleanersDistrict.EntranceX, GleanersDistrict.EntranceY);
        var cellar = Send("stairs", new() { ["value"] = "down" }); RequireSuccess(cellar, "Entering the real cellar");
        AtWorld(cellar, 11, 10, false); Check(Number(cellar, "depth") == 1, "The cellar must expose depth1."); TravelFlags(cellar, false, false);
        foreach (var destination in new[] { "world", "local" })
        {
            var refused = Travel(destination); SamePlayerState(cellar, refused, "Refused underground travel");
            Check(!string.IsNullOrWhiteSpace(Text(refused, "error")), "Underground travel must explain why local stairs are required.");
            AtWorld(refused, 11, 10, false);
        }
    }
    private static void StaleWorldTravelIsRefused()
    {
        var surface = NewTravelGame();
        var stale = Travel("world", Number(surface, "revision") - 1); SamePlayerState(surface, stale, "Stale world ascent");
        Check(!string.IsNullOrWhiteSpace(Text(stale, "error")), "Stale travel must be rejected.");
        var local = Travel("local"); SamePlayerState(surface, local, "Descending while already on the surface");
        Check(!string.IsNullOrWhiteSpace(Text(local, "error")), "Inapplicable descent must be rejected.");
        var mapped = EnterWorldMap();
        var staleLocal = Travel("local", Number(mapped, "revision") - 1); SamePlayerState(mapped, staleLocal, "Stale map descent"); AtWorld(staleLocal, 11, 10, true);
        Check(!string.IsNullOrWhiteSpace(Text(staleLocal, "error")), "Stale descent must be rejected.");
        var repeated = Travel("world"); SamePlayerState(mapped, repeated, "Ascending while already on the world map");
        Check(!string.IsNullOrWhiteSpace(Text(repeated, "error")), "Repeated ascent must be rejected.");
    }
    private static void WorldMapBordersAreFree()
    {
        NewTravelGame(); EnterWorldMap();
        foreach (var edge in new[] { (x: 0, y: 10, dx: -1, dy: 0), (x: 0, y: 0, dx: 0, dy: -1),
            (x: 19, y: 0, dx: 1, dy: 0), (x: 19, y: 19, dx: 0, dy: 1) })
        {
            WalkWorldTo(edge.x, edge.y); var before = State(); var zone = CurrentZone(Player());
            var wallCell = zone.GetCell(Number(before, "x") + edge.dx, Number(before, "y") + edge.dy);
            var walls = wallCell.Occupants.Where(e => e.HasTag("Wall")).Select(e => e.ID).ToArray();
            Check(walls.Length > 0, "The boundary fixture must be the actual generated wall.");
            var after = Send("step", new() { ["x"] = edge.dx, ["y"] = edge.dy });
            SamePlayerState(before, after, "World-map boundary bump"); AtWorld(after, edge.x, edge.y, true);
            Check(wallCell.Occupants.Where(e => e.HasTag("Wall")).Select(e => e.ID).SequenceEqual(walls), "A world border was destroyed by a refused step.");
        }
    }
    private static void WalkWorldTo(int x, int y)
    {
        for (var step = 0; step < 40; step++)
        {
            var before = State(); var wx = Number(before, "worldX"); var wy = Number(before, "worldY");
            if (wx == x && wy == y) return;
            var after = Send("step", new() { ["x"] = Math.Sign(x - wx), ["y"] = Math.Sign(y - wy) });
            RequireSuccess(after, "Following a bounded world-map route");
            Check(Number(after, "tick") == Number(before, "tick") + 20, "A world-route step did not preserve native travel timing.");
        }
        throw new Exception("The bounded world-map route did not reach its destination.");
    }
    private static void WorldMapCheckpoint()
    {
        var surface = NewTravelGame(); var mapped = EnterWorldMap(); var identity = InventoryIdentity(); var checkpoint = Game.Save();
        Send("step", new() { ["x"] = -1, ["y"] = 0 }); RequireSuccess(Travel("local"), "Changing the active zone after the checkpoint");
        var restored = Parse(Game.Load(checkpoint)); SamePlayerState(mapped, restored, "Restoring a map checkpoint"); AtWorld(restored, 11, 10, true);
        TravelFlags(restored, false, true); Check(InventoryIdentity() == identity, "Map save/load changed carried identities.");
        var returned = Travel("local"); RequireSuccess(returned, "Descending after restoring the map"); AtWorld(returned, 11, 10, false);
        Check(Text(returned, "zone") == Text(surface, "zone") && Number(returned, "x") == Number(surface, "x")
            && Number(returned, "y") == Number(surface, "y"), "Map save/load lost the exact native surface return memory.");
        Check(Number(returned, "tick") == Number(mapped, "tick") + 10, "Restored map descent must pay exactly one turn.");
    }
    private static void WorldMapCellSnapshot()
    {
        NewTravelGame(); var mapped = EnterWorldMap(); var zone = CurrentZone(Player()); var map = TravelZones().WorldMap;
        var cells = mapped["cells"] as JsonArray ?? throw new Exception("The map omitted its cells.");
        Check(cells.Count == WorldMap.Width * WorldMap.Height, "Only the400 embedded world cells should reach the browser renderer.");
        var coordinates = new HashSet<(int, int)>();
        foreach (var node in cells)
        {
            var cell = node as JsonObject ?? throw new Exception("Malformed map cell."); var x = Number(cell, "x"); var y = Number(cell, "y");
            Check(x >= 30 && x <= 49 && y >= 3 && y <= 22 && coordinates.Add((x, y)), "Map snapshot leaked a border cell or duplicated a coordinate.");
            var terrain = zone.GetCell(x, y).Occupants.Single(e => e.GetPart<WorldMapCellPart>() != null);
            Check(Text(cell, "name") == terrain.GetPart<RenderPart>().DisplayName, "Cell name must come from the native live map appearance.");
            Check(Boolean(cell, "visited") == map.IsVisited(x - 30, y - 3), "Cell discovery disagrees with the native visited bitmap.");
        }
    }
    private static void WorldMapHiddenSinkholes()
    {
        NewTravelGame(); EnterWorldMap(); var map = TravelZones().WorldMap; var zone = CurrentZone(Player());
        var hidden = Enumerable.Range(0, WorldMap.Width).SelectMany(x => Enumerable.Range(0, WorldMap.Height).Select(y => (x, y)))
            .Where(p => map.GetPOI(p.x, p.y)?.Type == POIType.Sinkhole && !map.IsVisited(p.x, p.y)).ToArray();
        Check(hidden.Length > 0, "Shipped seed8128 needs undiscovered sinkholes for this disclosure contract.");
        var point = hidden[0]; var poi = map.GetPOI(point.x, point.y); var biome = WorldMapZoneBuilder.GetBiomeRender(map.GetBiome(point.x, point.y));
        JsonObject CellView() => (State()["cells"] as JsonArray)!.OfType<JsonObject>()
            .Single(c => Number(c, "x") == point.x + 30 && Number(c, "y") == point.y + 3);
        var unseen = CellView(); Check(!Boolean(unseen, "visited") && Text(unseen, "name") == biome.displayName
            && Text(unseen, "g") == biome.glyph && Text(unseen, "c") == biome.color
            && Text(unseen, "name") != poi.Name, "An undiscovered sinkhole leaked its identity, marker or color.");
        var beforeReading = State();
        var looked = Send("look", new() { ["x"] = point.x + 30, ["y"] = point.y + 3 });
        RequireSuccess(looked, "Inspecting an undiscovered map cell"); SamePlayerState(beforeReading, looked, "Reading a world-map cell");
        Check(looked["look"] != null && !looked["look"]!.ToJsonString().Contains(poi.Name, StringComparison.Ordinal), "World-map inspection leaked the hidden entrance name.");
        var reader = Send("panel", new() { ["value"] = "map" }); SamePlayerState(beforeReading, reader, "Reading the strategic journey");
        Check(reader["journey"] != null && !reader["journey"]!.ToJsonString().Contains(poi.Name, StringComparison.Ordinal), "The journey reader leaked the hidden entrance name.");
        Send("close");
        WalkWorldTo(point.x, point.y);
        Check(!map.IsVisited(point.x, point.y) && !Boolean(CellView(), "visited"), "Crossing a strategic map tile must not discover its hidden ground entrance.");
        WalkWorldTo(11, 10);
        var crossed = CellView(); Check(Text(crossed, "name") == biome.displayName && Text(crossed, "g") == biome.glyph
            && Text(crossed, "c") == biome.color, "Strategic travel revealed a sinkhole without a ground visit.");
        // Owned discovery fixture: change only this fresh run's native bitmap,
        // then refresh with the real builder. Restore both in finally.
        try
        {
            map.MarkVisited(point.x, point.y); new WorldMapZoneBuilder(map).RefreshAppearance(zone);
            var discovered = CellView();
            Check(Boolean(discovered, "visited") && Text(discovered, "name") == poi.Name && Text(discovered, "g") == "o"
                && Text(discovered, "c") == "&K", "Native discovery did not update the browser's live marker, color and name.");
        }
        finally { map.Visited[point.x, point.y] = false; new WorldMapZoneBuilder(map).RefreshAppearance(zone); }
    }
}
