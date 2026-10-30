using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Settlement ground (2026-10-29). A detached seed-64 census found the fifteen ordinary authored
    /// settlements (every Place except Morrowfast's 3D scene and Sill's own adapter) drawing almost
    /// entirely glyph ground in the flat 16x24 view: e.g. Posy 89 of 2,000 native cells. Their Grass,
    /// Sand, StoneFloor and RoadStone now reuse the approved biome art the surrounding wilderness and
    /// Sill already use; anything wet, stacked, hidden or remembered keeps its fallback.
    /// </summary>
    public sealed class FlatPixelSettlementGroundTests
    {
        const string SillLawnAndFloors = "Sprites/Live16x24/sill-environment";
        const string ForestFloor = "Sprites/Live16x24/grovelands-ground";
        const string Beating = "Sprites/Live16x24/beating-materials";
        const string OpeningGround = "Sprites/Live16x24/opening-ground";
        const string TownFloor = "Sprites/Live16x24/town-floor";
        const string Vegetation = "Sprites/Live16x24/north-vegetation";
        const string OpeningBase = "Sprites/Live16x24/opening-base";
        const string SoddenTravel = "Sprites/Live16x24/sodden-travel";
        const string Keepers = "Sprites/Live16x24/settlement-keepers";
        const string OpeningStates = "Sprites/Live16x24/opening-states";
        const string Scorch = "Sprites/Live16x24/campfire-scorch";
        static EntityFactory factory;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup() { manager = OverworldZoneManager.CreateDetached(factory, 64); world = new FlatPixelGreyboxCatalog(); }
        [TearDown] public void Cleanup() { world?.Dispose(); }

        Zone Visit(string id)
        {
            var zone = manager.GetZone(id); Assert.NotNull(zone, id); manager.SetActiveZone(zone);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) { var c = zone.GetCell(x, y); c.Explored = true; c.IsVisible = true; }
            return zone;
        }
        IEnumerable<(int x, int y, FlatPixelGreyboxCell view)> Cells(Zone zone, string blueprint)
        {
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                if (world.TryResolvePresentation(zone, x, y, out var v) && v.Owner?.BlueprintName == blueprint) yield return (x, y, v);
        }
        static bool NativeGround(FlatPixelGreyboxCell v, string resource)
            => v.LocalGround && v.GroundTile != null && v.GroundTile.sprite != null
                && ReferenceEquals(v.GroundTile.sprite.texture, Resources.Load<Texture2D>(resource)) && v.ForegroundTile == null;

        // ════════════════ the settlements ════════════════

        [TestCase("Overworld.5.9.0", "Grass", SillLawnAndFloors)]       // Posy, Spread
        [TestCase("Overworld.16.11.0", "Grass", SillLawnAndFloors)]     // Slip, Spread
        [TestCase("Overworld.16.1.0", "Grass", SillLawnAndFloors)]      // the Quiet's Door, Sodden
        [TestCase("Overworld.6.6.0", "Grass", ForestFloor)]             // Cinderhold, Grovelands
        [TestCase("Overworld.8.16.0", "Sand", Beating)]                 // Wellmeet, Beating
        [TestCase("Overworld.5.17.0", "Sand", Beating)]                 // the First Tent, Beating
        [TestCase("Overworld.10.14.0", "StoneFloor", SillLawnAndFloors)] // Tally
        [TestCase("Overworld.10.14.0", "RoadStone", OpeningGround)]     // Tally, Spread road
        [TestCase("Overworld.8.16.0", "RoadStone", Beating)]            // Wellmeet, Beating road
        [TestCase("Overworld.13.7.0", "Floor", TownFloor)]              // Tine: town yard
        [TestCase("Overworld.15.6.0", "Floor", TownFloor)]              // Sumphold
        [TestCase("Overworld.17.5.0", "Floor", TownFloor)]              // the Drowned Ledger, Sodden
        [TestCase("Overworld.18.18.0", "Floor", TownFloor)]             // the Last Counter, Beating
        [TestCase("Overworld.14.9.0", "Floor", TownFloor)]              // Quillhold
        [TestCase("Overworld.10.14.0", "Floor", TownFloor)]             // Tally
        public void SettlementGroundUsesTheApprovedArtOfItsBiome(string id, string blueprint, string resource)
        {
            var zone = Visit(id); Assert.True(FlatPixelSettlementGroundCatalog.IsSupportedZone(zone), id + " is an ordinary settlement");
            var cells = Cells(zone, blueprint).ToList();
            Assert.Greater(cells.Count, 20, "precondition: the settlement has visible " + blueprint);
            int native = cells.Count(c => NativeGround(c.view, resource));
            Assert.GreaterOrEqual(native, cells.Count * 95 / 100, blueprint + " in " + id + ": " + native + " of " + cells.Count + " native");
            foreach (var c in cells.Where(c => NativeGround(c.view, resource)).Take(25))
            {
                Assert.AreEqual(new Vector2(16, 24), c.view.GroundTile.sprite.rect.size);
                Assert.AreEqual(Tile.ColliderType.None, c.view.GroundTile.colliderType);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, c.view.Owner, c.x, c.y, out var portrait, out _), "the reader shows the same ground");
                Assert.AreSame(c.view.GroundTile.sprite.texture, portrait.texture); Assert.AreEqual(c.view.GroundTile.sprite.rect, portrait.rect);
            }
        }

        [Test] public void SettlementFloorsAndLawnUseTheirOwnFrames()
        {
            // Lawn frames 0–3 and room floors 4–7 of the Sill settlement atlas; never borrowed across.
            var zone = Visit("Overworld.10.14.0");
            var floors = Cells(zone, "StoneFloor").Where(c => NativeGround(c.view, SillLawnAndFloors)).ToList();
            Assert.IsNotEmpty(floors, "precondition: native room floors");
            foreach (var c in floors) Assert.That((int)c.view.GroundTile.sprite.rect.x / 16, Is.InRange(4, 7));
            zone = Visit("Overworld.5.9.0");
            var lawn = Cells(zone, "Grass").Where(c => NativeGround(c.view, SillLawnAndFloors)).ToList();
            Assert.IsNotEmpty(lawn, "precondition: native lawn");
            foreach (var c in lawn) Assert.That((int)c.view.GroundTile.sprite.rect.x / 16, Is.InRange(0, 3));
        }

        // ════════════════ settlement water and sandstone walls ════════════════
        // Seed-64 census: 2,394 WaterPuddle cells (Posy, Slip, the Quiet's Door on Grass; Tine, Sumphold,
        // the Drowned Ledger on Floor; the Salt-Vault on Sand) and 935 SandstoneWall cells were all glyphs.

        [TestCase("Overworld.5.9.0")]   // Posy, on Grass
        [TestCase("Overworld.16.1.0")]  // the Quiet's Door, on Grass
        [TestCase("Overworld.13.7.0")]  // Tine, on Floor
        [TestCase("Overworld.17.5.0")]  // the Drowned Ledger, on Floor
        [TestCase("Overworld.15.15.0")] // the Salt-Vault, on Sand
        public void SettlementPuddlesUseTheApprovedRiverWater(string id)
        {
            var zone = Visit(id);
            var cells = Cells(zone, "WaterPuddle").ToList();
            Assert.Greater(cells.Count, 100, "precondition: visible settlement water");
            var water = Resources.Load<Texture2D>(OpeningGround);
            bool Native(FlatPixelGreyboxCell v) => v.LocalGround && v.GroundKind == FlatPixelGreyboxGround.Water && v.ForegroundTile == null
                && v.GroundTile?.sprite != null && ReferenceEquals(v.GroundTile.sprite.texture, water) && v.GroundTile.sprite.rect.x / 16 < 16;
            int native = cells.Count(c => Native(c.view));
            Assert.GreaterOrEqual(native, cells.Count * 95 / 100, "WaterPuddle in " + id + ": " + native + " of " + cells.Count + " native");
            foreach (var c in cells.Where(c => Native(c.view)).Take(25))
            {
                Assert.AreEqual(Tile.ColliderType.None, c.view.GroundTile.colliderType);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, c.view.Owner, c.x, c.y, out var portrait, out _), "the reader shows the same water");
                Assert.AreEqual(c.view.GroundTile.sprite.rect, portrait.rect);
            }
        }

        [TestCase("Overworld.6.6.0")]   // Cinderhold, on Grass
        [TestCase("Overworld.12.12.0")] // Marrowstye, on Floor and StoneFloor
        [TestCase("Overworld.18.18.0")] // the Last Counter
        [TestCase("Overworld.15.15.0")] // the Salt-Vault, on Sand
        public void SettlementSandstoneWallsUseTheApprovedBeatingWall(string id)
        {
            var zone = Visit(id);
            var cells = Cells(zone, "SandstoneWall").ToList();
            Assert.Greater(cells.Count, 40, "precondition: visible sandstone walls");
            var beating = Resources.Load<Texture2D>(Beating);
            bool Native(FlatPixelGreyboxCell v) => v.NativeForeground && v.ForegroundTile?.sprite != null
                && ReferenceEquals(v.ForegroundTile.sprite.texture, beating) && (int)v.ForegroundTile.sprite.rect.x / 16 is 16 or 17;
            int native = cells.Count(c => Native(c.view));
            Assert.GreaterOrEqual(native, cells.Count * 95 / 100, "SandstoneWall in " + id + ": " + native + " of " + cells.Count + " native");
            foreach (var c in cells.Where(c => Native(c.view)).Take(25))
            {
                Assert.True(c.view.BlocksMovement, "a wall still blocks");
                Assert.True(world.TryGetPresentationSubjectSprite(zone, c.view.Owner, c.x, c.y, out var portrait, out _), "the reader shows the same wall");
                Assert.AreEqual(c.view.ForegroundTile.sprite.rect, portrait.rect);
            }
        }

        [Test] public void MixedWaterAndAFrozenWallKeepTheirFallback()
        {
            var zone = Visit("Overworld.15.15.0");
            var puddle = Cells(zone, "WaterPuddle").First(c => c.view.LocalGround);
            zone.TileState.WriteCoating(puddle.x, puddle.y, "oil", 4);
            Assert.True(world.TryResolvePresentation(zone, puddle.x, puddle.y, out var oily));
            Assert.False(oily.LocalGround && oily.GroundKind == FlatPixelGreyboxGround.Water, "oil on the water is not plain river water");
            var wall = Cells(zone, "SandstoneWall").First(c => c.view.NativeForeground);
            var thermal = wall.view.Owner.GetPart<ThermalPart>(); Assert.NotNull(thermal, "precondition: the wall has a temperature");
            thermal.Temperature = thermal.FreezeTemperature - 10f;
            Assert.True(world.TryResolvePresentation(zone, wall.x, wall.y, out var frozen));
            Assert.False(frozen.NativeForeground, "a frozen wall keeps its truthful fallback, as in the Beating");
        }

        // ════════════════ plants and boardwalks standing on settlement ground ════════════════
        // After the town floor, these owners showed as black boxes on native ground (seed-64 census:
        // Duckboard 183, Bush 83, DryBrush 68, Reeds 71, PeatBank 65, Tree 31 glyph cells). Each now uses
        // the approved art and the exact eligibility rule of the biome that owns it; see-through ones
        // keep the real settlement ground beneath them.

        [TestCase("Overworld.6.6.0", "Bush", OpeningBase, true)]      // Cinderhold
        [TestCase("Overworld.6.6.0", "Tree", Vegetation, true)]       // Cinderhold
        [TestCase("Overworld.8.16.0", "DryBrush", Vegetation, true)]  // Wellmeet, on Sand
        [TestCase("Overworld.18.18.0", "DryBrush", Vegetation, true)] // the Last Counter, on Floor
        [TestCase("Overworld.13.7.0", "Duckboard", SoddenTravel, true)] // Tine
        [TestCase("Overworld.17.5.0", "Reeds", OpeningBase, true)]    // the Drowned Ledger
        [TestCase("Overworld.15.6.0", "PeatBank", SoddenTravel, false)] // Sumphold: an opaque bank
        public void SettlementPlantsAndBoardwalksUseApprovedArtOnTheirGround(string id, string blueprint, string resource, bool groundBeneath)
        {
            var zone = Visit(id);
            var cells = Cells(zone, blueprint).ToList();
            Assert.Greater(cells.Count, 8, "precondition: visible " + blueprint);
            var texture = Resources.Load<Texture2D>(resource);
            bool Native(FlatPixelGreyboxCell v) => v.NativeForeground && v.ForegroundTile?.sprite != null
                && ReferenceEquals(v.ForegroundTile.sprite.texture, texture) && (!groundBeneath || v.LocalGround);
            int native = cells.Count(c => Native(c.view));
            Assert.GreaterOrEqual(native, cells.Count * 90 / 100, blueprint + " in " + id + ": " + native + " of " + cells.Count + " native");
            foreach (var c in cells.Where(c => Native(c.view)).Take(20))
            {
                Assert.True(world.TryGetPresentationSubjectSprite(zone, c.view.Owner, c.x, c.y, out var portrait, out _), "the reader shows the same " + blueprint);
                Assert.AreSame(texture, portrait.texture);
            }
        }

        [Test] public void ADestroyedBushAndAFrozenBoardwalkKeepTheirFallback()
        {
            var zone = Visit("Overworld.6.6.0");
            var bush = Cells(zone, "Bush").First(c => c.view.NativeForeground);
            var structure = bush.view.Owner.GetPart<DestructiblePart>(); Assert.NotNull(structure, "precondition");
            structure.HP = 0;
            Assert.True(world.TryResolvePresentation(zone, bush.x, bush.y, out var dead));
            Assert.False(dead.NativeForeground, "a destroyed bush is not drawn as a living one");
            zone = Visit("Overworld.13.7.0");
            var board = Cells(zone, "Duckboard").First(c => c.view.NativeForeground);
            var thermal = board.view.Owner.GetPart<ThermalPart>(); Assert.NotNull(thermal, "precondition");
            thermal.Temperature = thermal.FreezeTemperature - 10f;
            Assert.True(world.TryResolvePresentation(zone, board.x, board.y, out var frozen));
            Assert.False(frozen.NativeForeground, "a frozen boardwalk keeps its truthful fallback, as in the Sodden");
        }

        // ════════════════ the settlement keepers ════════════════
        // Every settlement's Innkeeper, Quartermaster and Scribe was still a coloured '@' while the shared
        // Villager, Merchant, Tinker, Elder and Child beside them had native bodies. Three original
        // cast-style bodies (settlement-keepers.png) now draw them, by the shared residents' exact rule.

        [TestCase("Overworld.5.9.0", "Innkeeper", 0)] [TestCase("Overworld.13.7.0", "Innkeeper", 0)]
        [TestCase("Overworld.5.9.0", "Quartermaster", 1)] [TestCase("Overworld.8.16.0", "Quartermaster", 1)]
        [TestCase("Overworld.5.9.0", "Scribe", 2)] [TestCase("Overworld.10.14.0", "Scribe", 2)]
        public void SettlementKeepersHaveTheirOwnNativeBodies(string id, string role, int frame)
        {
            var zone = Visit(id);
            var keeper = zone.GetAllEntities().FirstOrDefault(e => e.BlueprintName == role);
            Assert.NotNull(keeper, "precondition: " + id + " has its " + role);
            var (x, y) = zone.GetEntityPosition(keeper);
            Assert.True(world.TryResolvePresentation(zone, x, y, out var view));
            Assert.AreSame(keeper, view.Owner, "precondition: the keeper is the cell's subject");
            var atlas = Resources.Load<Texture2D>(Keepers);
            Assert.True(view.NativeForeground, role + " has a native body");
            Assert.AreSame(atlas, view.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(frame * 16, 0, 16, 24), view.ForegroundTile.sprite.rect, role + " uses its own frame");
            Assert.True(world.TryGetPresentationSubjectSprite(zone, keeper, x, y, out var portrait, out _), "the reader shows the same body");
            Assert.AreEqual(view.ForegroundTile.sprite.rect, portrait.rect);
        }

        [Test] public void AKeeperWhoseLookOrRoleChangedKeepsTheirGlyph()
        {
            var zone = Visit("Overworld.5.9.0");
            var inn = zone.GetAllEntities().First(e => e.BlueprintName == "Innkeeper");
            var (x, y) = zone.GetEntityPosition(inn);
            inn.GetPart<RenderPart>().ColorString = "&R";
            Assert.True(world.TryResolvePresentation(zone, x, y, out var recoloured));
            Assert.False(recoloured.NativeForeground, "a recoloured innkeeper is not the canonical one");
            var scribe = zone.GetAllEntities().First(e => e.BlueprintName == "Scribe");
            var (sx, sy) = zone.GetEntityPosition(scribe);
            scribe.RemovePart(scribe.GetPart<TraderPart>());
            Assert.True(world.TryResolvePresentation(zone, sx, sy, out var noTrade));
            Assert.False(noTrade.NativeForeground, "a scribe without their trade is not the canonical one");
        }

        // ════════════════ static memory ════════════════
        // Actual play in Marrowstye (2026-10-30) showed explored settlement ground and walls falling back
        // to dark glyph speckle once out of sight. Like Sill, remembered plain ground and sandstone walls
        // keep their approved static material under the single memory dim, with no live state, no
        // adjacency and no reader authority.

        [TestCase("Overworld.13.7.0", "Floor", TownFloor, false)]          // Tine town floor
        [TestCase("Overworld.5.9.0", "Grass", SillLawnAndFloors, false)]   // Posy lawn
        [TestCase("Overworld.10.14.0", "RoadStone", OpeningGround, false)] // Tally road
        [TestCase("Overworld.18.18.0", "SandstoneWall", Beating, true)]    // the Last Counter wall
        public void RememberedSettlementMaterialKeepsItsArtUnderTheMemoryDim(string id, string blueprint, string resource, bool wall)
        {
            var zone = Visit(id);
            var live = Cells(zone, blueprint).First(c => wall ? c.view.NativeForeground : NativeGround(c.view, resource));
            var liveRect = (wall ? live.view.ForegroundTile : live.view.GroundTile).sprite.rect;
            zone.GetCell(live.x, live.y).IsVisible = false;
            Assert.True(world.TryResolvePresentation(zone, live.x, live.y, out var memory));
            Assert.True(memory.Remembered, "precondition: explored but out of sight");
            var tile = wall ? memory.ForegroundTile : memory.GroundTile;
            Assert.NotNull(tile, blueprint + " keeps its material in memory");
            Assert.AreSame(Resources.Load<Texture2D>(resource), tile.sprite.texture);
            Assert.AreEqual(liveRect, tile.sprite.rect, "the same static frame as when seen");
            var tint = wall ? memory.ForegroundColor : memory.GroundColor;
            Assert.Less(tint.r, .5f, "drawn under the memory dim");
            if (!wall) Assert.IsNull(memory.ForegroundTile, "no glyph speckle over remembered ground");
            bool portrait = world.TryGetPresentationSubjectSprite(zone, live.view.Owner, live.x, live.y, out var memoryPortrait, out _);
            Assert.False(portrait && ReferenceEquals(memoryPortrait.texture, Resources.Load<Texture2D>(resource)),
                "memory grants no native reader portrait");
        }

        [Test] public void UnexploredAndRememberedWaterKeepTheirFallback()
        {
            var zone = Visit("Overworld.13.7.0");
            var floor = Cells(zone, "Floor").First(c => NativeGround(c.view, TownFloor));
            var cell = zone.GetCell(floor.x, floor.y); cell.IsVisible = false; cell.Explored = false;
            Assert.True(world.TryResolvePresentation(zone, floor.x, floor.y, out var unknown) == false || unknown.GroundTile == null
                || !ReferenceEquals(unknown.GroundTile.sprite?.texture, Resources.Load<Texture2D>(TownFloor)), "never-seen ground stays undisclosed");
            var water = Cells(zone, "WaterPuddle").First(c => c.view.LocalGround);
            zone.GetCell(water.x, water.y).IsVisible = false;
            Assert.True(world.TryResolvePresentation(zone, water.x, water.y, out var rememberedWater));
            Assert.False(rememberedWater.GroundTile?.sprite != null
                && ReferenceEquals(rememberedWater.GroundTile.sprite.texture, Resources.Load<Texture2D>(OpeningGround))
                && rememberedWater.GroundKind == FlatPixelGreyboxGround.Water, "live water state is not kept in static memory");
        }

        // ════════════════ the settlement hearth ════════════════
        // Every settlement centre showed a red '*' and four red '.' markers on black boxes. An ordinary
        // (not finite-cooking) campfire is a permanent lit hearth whose glyph flickers, so it uses the
        // approved burning campfire (opening-states frame 5); its scorched-earth markers use the original
        // campfire-scorch stain over the real settlement ground.

        [TestCase("Overworld.10.14.0")] [TestCase("Overworld.8.16.0")] [TestCase("Overworld.12.12.0")]
        public void SettlementHearthsBurnOnScorchedRealGround(string id)
        {
            var zone = Visit(id);
            var fires = Cells(zone, "Campfire").ToList(); var marks = Cells(zone, "CampfireGroundMarker").ToList();
            Assert.IsNotEmpty(fires, "precondition: a hearth"); Assert.GreaterOrEqual(marks.Count, 4, "precondition: its scorched ring");
            var states = Resources.Load<Texture2D>(OpeningStates); var scorch = Resources.Load<Texture2D>(Scorch);
            foreach (var f in fires)
            {
                Assert.True(f.view.NativeForeground, "the hearth has a native body");
                Assert.AreSame(states, f.view.ForegroundTile.sprite.texture); Assert.AreEqual(new Rect(80, 0, 16, 24), f.view.ForegroundTile.sprite.rect, "the burning frame");
                Assert.True(f.view.LocalGround, "real ground beneath the hearth");
                Assert.True(world.TryGetPresentationSubjectSprite(zone, f.view.Owner, f.x, f.y, out var portrait, out _)); Assert.AreSame(states, portrait.texture);
            }
            int scorched = marks.Count(m => m.view.NativeForeground && ReferenceEquals(m.view.ForegroundTile.sprite.texture, scorch) && m.view.LocalGround);
            Assert.GreaterOrEqual(scorched, marks.Count * 80 / 100, "scorched earth on real ground: " + scorched + " of " + marks.Count);
        }

        [Test] public void AFiniteCookingFireAndAForeignMarkerKeepTheirFallback()
        {
            var zone = Visit("Overworld.10.14.0");
            var fire = Cells(zone, "Campfire").First();
            fire.view.Owner.GetPart<CampfirePart>().FiniteCooking = true;
            Assert.True(world.TryResolvePresentation(zone, fire.x, fire.y, out var finite));
            Assert.False(finite.NativeForeground && ReferenceEquals(finite.ForegroundTile.sprite.texture, Resources.Load<Texture2D>(OpeningStates)),
                "a finite-cooking fire has real lit/cold states this adapter does not read");
            var mark = Cells(zone, "CampfireGroundMarker").First(m => m.view.NativeForeground);
            mark.view.Owner.GetPart<RenderPart>().ColorString = "&B";
            Assert.True(world.TryResolvePresentation(zone, mark.x, mark.y, out var odd));
            Assert.False(odd.NativeForeground, "a recoloured marker is not plain scorched earth");
        }

        // ════════════════ counter-checks ════════════════

        [TestCase("Overworld.10.10.0")] [TestCase("Overworld.3.6.0")] [TestCase("Overworld.5.11.0")]
        public void SillMorrowfastAndWildernessAreNotThisAdaptersToDraw(string id)
            => Assert.False(FlatPixelSettlementGroundCatalog.IsSupportedZone(Visit(id)), id);

        [Test] public void WetHiddenAndStackedGroundKeepTheirFallback()
        {
            var zone = Visit("Overworld.5.9.0");
            var cells = Cells(zone, "Grass").Where(c => NativeGround(c.view, SillLawnAndFloors)).Take(3).ToList();
            Assert.AreEqual(3, cells.Count, "precondition");
            var (wx, wy, _) = cells[0]; zone.TileState.WriteCoating(wx, wy, "water", 4);
            Assert.True(world.TryResolvePresentation(zone, wx, wy, out var wet)); Assert.False(NativeGround(wet, SillLawnAndFloors), "a wet cell does not read as dry lawn");
            var (hx, hy, _) = cells[1]; zone.GetCell(hx, hy).IsVisible = false;
            Assert.True(world.TryResolvePresentation(zone, hx, hy, out var hidden));
            // Revised with settlement memory (2026-10-30): remembered ground keeps its static material
            // under the memory dim, but is never redrawn as live ground.
            Assert.True(hidden.Remembered); Assert.Less(hidden.GroundColor.r, .5f, "remembered ground is not redrawn live");
            var (sx, sy, _) = cells[2]; Assert.True(zone.AddEntity(factory.CreateEntity("Sand"), sx, sy));
            Assert.True(world.TryResolvePresentation(zone, sx, sy, out var stacked)); Assert.False(NativeGround(stacked, SillLawnAndFloors), "two ground owners are ambiguous");
        }

        [Test] public void TheWildernessAroundAPlaceIsUnchanged()
        {
            // Counter-check: an ordinary Spread wilderness zone keeps its own adapter's turf.
            var zone = Visit("Overworld.5.11.0");
            var cells = Cells(zone, "Grass").ToList(); Assert.Greater(cells.Count, 100);
            Assert.GreaterOrEqual(cells.Count(c => c.view.LocalGround), cells.Count * 95 / 100);
        }
    }
}
