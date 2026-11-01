using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Glyph → pixel, props (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md): fixed props draw their 16x24
    /// frame from the generated atlas (ArtSource/Live16x24/prop-icons) outside opening zones. A
    /// crop shows its current growth stage and a door its open or closed state, each from its
    /// own frame; recoloured, takeable, wrecked or out-of-state props keep their glyph.
    /// </summary>
    public sealed class FlatPixelPropIconsTests
    {
        static EntityFactory factory;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;
        Zone zone;

        [System.Serializable] sealed class Row { public string Blueprint; public string State; public int Frame; public int Variants; public string Glyph; public string Color; public string Background; public string Family; }
        [System.Serializable] sealed class FileData { public int Columns; public int FrameHeight; public Row[] Icons; }
        static FileData Index() => JsonUtility.FromJson<FileData>(Resources.Load<TextAsset>(FlatPixelWildFaunaCatalog.PropIndexResourcePath).text);
        static Row RowOf(string blueprint, string state = "") => Index().Icons.Single(r => r.Blueprint == blueprint && (r.State ?? "") == state);

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
        }
        [SetUp] public void Setup()
        {
            manager = OverworldZoneManager.CreateDetached(factory, 64); world = new FlatPixelGreyboxCatalog();
            zone = manager.GetZone("Overworld.11.6.0"); manager.SetActiveZone(zone);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) { var c = zone.GetCell(x, y); c.Explored = true; c.IsVisible = true; }
        }
        [TearDown] public void Cleanup() { world?.Dispose(); }

        Entity Place(string blueprint)
        {
            var e = factory.CreateEntity(blueprint);
            for (int y = 3; y < Zone.Height - 3; y++) for (int x = 3; x < Zone.Width - 3; x++)
            {
                var c = zone.GetCell(x, y);
                if (c.BlocksMovement() || c.Objects.Any(o => !o.HasTag("Terrain"))) continue;
                if (zone.AddEntity(e, x, y)) return e;
            }
            Assert.Fail("no room for " + blueprint); return null;
        }
        bool Native(Entity e, out FlatPixelGreyboxCell v)
        {
            var (x, y) = zone.GetEntityPosition(e);
            return world.TryResolvePresentation(zone, x, y, out v) && ReferenceEquals(v.Owner, e) && v.NativeForeground;
        }
        void AssertFrame(Entity e, Row row)
        {
            Assert.True(Native(e, out var v), e.BlueprintName + " " + row.State + " draws natively");
            var atlas = Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.PropAtlasResourcePath);
            Assert.AreSame(atlas, v.ForegroundTile.sprite.texture);
            int columns = Index().Columns, h = Index().FrameHeight, rows = atlas.height / h;
            var own = Enumerable.Range(row.Frame, System.Math.Max(1, row.Variants))
                .Select(f => new Rect(f % columns * 16, (rows - 1 - f / columns) * h, 16, h)).ToList();
            CollectionAssert.Contains(own, v.ForegroundTile.sprite.rect, "one of its own frames");
        }
        static void Paint(Entity crop, int stage)
        {
            // CropSystem.PaintStage's exact rule: the stage CSVs drive the glyph and colour.
            var part = crop.GetPart<CropPart>(); part.GrowthStage = stage;
            var render = crop.GetPart<RenderPart>();
            render.RenderString = part.GlyphForStage(stage).ToString(); render.ColorString = part.ColorForStage(stage);
        }

        // ════════════════ stateless props ════════════════

        [TestCase("Bookshelf")] [TestCase("SmithAnvil")] [TestCase("GantryRegistryDesk")] [TestCase("WatchLantern")]
        [TestCase("CinderholdNoticeBoard")] [TestCase("BearTrap")] [TestCase("IceWall")] [TestCase("Graveyard")]
        [TestCase("CurationQuarantineRail")] [TestCase("GantryTimberWall")] [TestCase("RepairRopeWell")] [TestCase("BoatFrame")]
        public void AFixedPropDrawsItsFrame(string blueprint)
        {
            var e = Place(blueprint);
            AssertFrame(e, RowOf(blueprint));
            var (x, y) = zone.GetEntityPosition(e);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, e, x, y, out var portrait, out _));
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.PropAtlasResourcePath), portrait.texture, "the reader shows the same art");
        }

        [Test] public void ARecolouredPropKeepsItsGlyph()
        {
            var e = Place("Bookshelf"); Assume.That(Native(e, out _));
            e.GetPart<RenderPart>().ColorString = "&R";
            Assert.False(Native(e, out _), "counter-check: not the canonical owner");
        }

        [Test] public void AWallWithoutItsCanonicalBackgroundKeepsItsGlyph()
        {
            var e = Place("IceWall"); Assume.That(Native(e, out _));
            e.GetPart<RenderPart>().BackgroundColor = "";
            Assert.False(Native(e, out _), "the background is part of the canon");
        }

        [Test] public void APropMadeTakeableKeepsItsGlyph()
        {
            var e = Place("SmithAnvil"); Assume.That(Native(e, out _));
            e.GetPart<PhysicsPart>().Takeable = true;
            Assert.False(Native(e, out _), "props stand; takeable things are items");
        }

        [Test] public void AWreckedPropKeepsItsGlyph()
        {
            var e = Place("LockedDoor"); Assume.That(Native(e, out _));
            e.GetPart<DestructiblePart>().HP = 0;
            Assert.False(Native(e, out _), "a broken door is not drawn whole");
        }

        [Test] public void APlankWallRunVariesItsBoardsAndEachCellKeepsItsOwn()
        {
            var row = RowOf("GantryTimberWall"); Assume.That(row.Variants, Is.GreaterThan(1));
            var rects = new HashSet<Rect>(); var walls = new List<Entity>();
            for (int x = 20; x < 28; x++) { var w = factory.CreateEntity("GantryTimberWall"); Assert.True(zone.AddEntity(w, x, 12)); walls.Add(w); }
            foreach (var w in walls) { AssertFrame(w, row); Native(w, out var v); rects.Add(v.ForegroundTile.sprite.rect); }
            Assert.Greater(rects.Count, 1, "eight boards in a row are not one stamped frame");
            Native(walls[3], out var first); Native(walls[3], out var again);
            Assert.AreEqual(first.ForegroundTile.sprite.rect, again.ForegroundTile.sprite.rect, "counter-check: a cell never flickers between variants");
        }

        // ════════════════ crops: one frame per stage ════════════════

        [TestCase("AbsentmintCrop")] [TestCase("HearthbulbCrop")] [TestCase("KnotflaxCrop")] [TestCase("SeamleafCrop")]
        public void ACropShowsItsCurrentStage(string blueprint)
        {
            var e = Place(blueprint);
            var seen = new HashSet<Rect>();
            for (int stage = 0; stage <= 2; stage++)
            {
                Paint(e, stage);
                AssertFrame(e, RowOf(blueprint, "stage" + stage));
                Native(e, out var v); seen.Add(v.ForegroundTile.sprite.rect);
            }
            Assert.AreEqual(3, seen.Count, "sown, sprout and mature are three pictures");
        }

        [Test] public void ACropWhoseGlyphDisagreesWithItsStageKeepsItsGlyph()
        {
            var e = Place("AbsentmintCrop"); Paint(e, 0); Assume.That(Native(e, out _));
            e.GetPart<CropPart>().GrowthStage = 2;
            Assert.False(Native(e, out _), "counter-check: the stage-0 glyph is not the mature crop");
        }

        [Test] public void ACropWithAStageOutsideTheIndexKeepsItsGlyph()
        {
            var e = Place("AbsentmintCrop");
            e.GetPart<CropPart>().GrowthStage = 3;
            Assert.False(Native(e, out _));
        }

        // ════════════════ doors: open and closed ════════════════

        [TestCase("SpreadFieldGate")] [TestCase("CurationGalleryGate")] [TestCase("RepairWoodenGate")]
        public void AGateShowsWhetherItIsOpen(string blueprint)
        {
            var e = Place(blueprint); var door = e.GetPart<DoorPart>();
            door.IsOpen = false; door.Initialize();
            AssertFrame(e, RowOf(blueprint, "closed"));
            Native(e, out var shut);
            door.IsOpen = true; door.Initialize();
            AssertFrame(e, RowOf(blueprint, "open"));
            Native(e, out var open);
            Assert.AreNotEqual(shut.ForegroundTile.sprite.rect, open.ForegroundTile.sprite.rect, "open is not shut");
        }

        [Test] public void AnOpenGlyphOnAClosedGateKeepsItsGlyph()
        {
            var e = Place("SpreadFieldGate"); var door = e.GetPart<DoorPart>();
            door.IsOpen = false; door.Initialize(); Assume.That(Native(e, out _));
            e.GetPart<RenderPart>().RenderString = "/";
            Assert.False(Native(e, out _), "counter-check: the picture follows the state, not the other way round");
        }

        // ════════════════ the painted places' scene owners ════════════════

        [Test] public void EveryFurnishingOfMorrowfastDrawsItsKindAndDoorsShowTheirState()
        {
            var town = manager.GetZone(MorrowfastSceneRuntime.ZoneID); manager.SetActiveZone(town);
            foreach (var c in town.Cells) { c.Explored = true; c.IsVisible = true; }
            Assume.That(MorrowfastSceneRuntime.IsActive(town));
            int drawn = 0, owners = 0; var glyphs = new List<string>();
            foreach (var e in town.GetAllEntities().Where(e => e.BlueprintName == "MorrowfastSceneProp").ToList())
            {
                var id = e.GetPart<MorrowfastPropPart>().ComponentId; var kind = MorrowfastSceneDefinition.Load().FindOwner(id)?.kind;
                if (kind == "building-shell" || kind == "roof") continue;
                owners++;
                var (x, y) = town.GetEntityPosition(e);
                Assert.True(world.TryResolvePresentation(town, x, y, out var v));
                if (!ReferenceEquals(v.Owner, e)) { owners--; continue; }   // covered: the owner on top is what is drawn here
                if (v.NativeForeground) drawn++; else glyphs.Add(id + " (" + kind + ")");
            }
            CollectionAssert.IsEmpty(glyphs, "furnishings still drawn as glyphs");
            Assert.Greater(owners, 40); Assert.AreEqual(owners, drawn);
            var door = town.GetAllEntities().First(e => e.GetPart<MorrowfastDoorPart>() != null);
            var (dx, dy) = town.GetEntityPosition(door);
            Assert.True(world.TryResolvePresentation(town, dx, dy, out var shut) && shut.NativeForeground);
            door.GetPart<MorrowfastDoorPart>().IsOpen = true; door.GetPart<RenderPart>().RenderString = "/";
            Assert.True(world.TryResolvePresentation(town, dx, dy, out var open) && open.NativeForeground);
            Assert.AreNotEqual(shut.ForegroundTile.sprite.rect, open.ForegroundTile.sprite.rect, "the open door is drawn open");
            door.GetPart<RenderPart>().RenderString = "+";
            Assert.False(world.TryResolvePresentation(town, dx, dy, out var lie) && ReferenceEquals(lie.Owner, door) && lie.NativeForeground,
                "counter-check: a closed glyph on an open door is not the canonical state");
        }

        [Test] public void EveryLayerOfTheFellingSiteDrawsItsKind()
        {
            var place = manager.GetZone(FellingSiteBuilder.ZoneID); manager.SetActiveZone(place);
            foreach (var c in place.Cells) { c.Explored = true; c.IsVisible = true; }
            var layers = place.GetAllEntities().Where(e => e.BlueprintName == "FellingSceneProp").ToList();
            Assume.That(layers.Count, Is.GreaterThan(40), "the Felling scene is installed");
            foreach (var e in layers)
            {
                var (x, y) = place.GetEntityPosition(e);
                Assert.True(world.TryResolvePresentation(place, x, y, out var v) && ReferenceEquals(v.Owner, e) && v.NativeForeground,
                    e.GetPart<FellingScenePropPart>().ComponentId + " still draws its glyph");
            }
            var growth = layers.First(e => e.GetPart<FellingScenePropPart>().Mutable);
            growth.GetPart<FellingScenePropPart>().ComponentId = "not-a-layer";
            var (gx, gy) = place.GetEntityPosition(growth);
            Assert.False(world.TryResolvePresentation(place, gx, gy, out var foreign) && ReferenceEquals(foreign.Owner, growth) && foreign.NativeForeground,
                "counter-check: an unknown component keeps its glyph");
        }

        // ════════════════ the index ════════════════

        [Test] public void EveryIndexedPropIsARealFixedPropInsideTheAtlas()
        {
            var index = Index(); var atlas = Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.PropAtlasResourcePath);
            Assert.NotNull(atlas); Assert.AreEqual(24, index.FrameHeight); Assert.Greater(index.Icons.Length, 150);
            int frames = (atlas.width / 16) * (atlas.height / index.FrameHeight);
            var used = index.Icons.SelectMany(r => Enumerable.Range(r.Frame, System.Math.Max(1, r.Variants))).ToList();
            Assert.AreEqual(used.Count, used.Distinct().Count(), "every frame, variants included, belongs to one row");
            Assert.AreEqual(index.Icons.Length, index.Icons.Select(r => r.Blueprint + "|" + r.State).Distinct().Count(), "one row per state");
            var town = MorrowfastSceneDefinition.Load(); var site = FellingSceneDefinition.Load();
            foreach (var r in index.Icons)
            {
                if (r.Blueprint == "MorrowfastSceneProp" || r.Blueprint == "FellingSceneProp")
                {
                    // Scene owners are built in code from the place's definition, keyed by component.
                    Assert.Less(r.Frame, frames, r.State);
                    if (r.Blueprint == "FellingSceneProp")
                    {
                        var layer = site.layers.SingleOrDefault(l => l.id == r.State); Assert.NotNull(layer, r.State);
                        Assert.AreEqual(layer.mutable ? "*" : "#", r.Glyph, r.State);
                    }
                    else
                    {
                        string id = r.State.Split('|')[0]; var owner = town.FindOwner(id); Assert.NotNull(owner, r.State);
                        Assert.AreEqual(owner.kind == "door" ? (r.State.EndsWith("|open") ? "/" : "+") : "*", r.Glyph, r.State);
                    }
                    Assert.AreEqual("&y", r.Color, r.State);
                    continue;
                }
                Assert.True(factory.Blueprints.ContainsKey(r.Blueprint), r.Blueprint);
                Assert.Less(r.Frame + System.Math.Max(1, r.Variants) - 1, frames, r.Blueprint);
                var e = factory.CreateEntity(r.Blueprint); var render = e.GetPart<RenderPart>();
                Assert.False(e.GetPart<PhysicsPart>()?.Takeable ?? true, r.Blueprint + " is fixed");
                Assert.False(e.HasTag("Creature") || e.HasTag("Item"), r.Blueprint);
                Assert.AreEqual(render.BackgroundColor ?? "", r.Background ?? "", r.Blueprint + " background");
                var crop = e.GetPart<CropPart>(); var door = e.GetPart<DoorPart>();
                if (crop != null)
                {
                    int stage = int.Parse(r.State.Substring("stage".Length));
                    Assert.AreEqual(crop.GlyphForStage(stage).ToString(), r.Glyph, r.Blueprint + " " + r.State);
                    Assert.AreEqual(crop.ColorForStage(stage), r.Color, r.Blueprint + " " + r.State);
                }
                else if (door != null)
                {
                    Assert.AreEqual(r.State == "closed" ? "+" : "/", r.Glyph, r.Blueprint + " " + r.State);
                    Assert.AreEqual(render.ColorString, r.Color, r.Blueprint);
                }
                else
                {
                    Assert.AreEqual("", r.State ?? "", r.Blueprint);
                    Assert.AreEqual(render.RenderString, r.Glyph, r.Blueprint); Assert.AreEqual(render.ColorString, r.Color, r.Blueprint);
                }
            }
        }

        [Test] public void PropsWithApprovedArtOrAnItemIconAreNeverIndexed()
        {
            string dir = Path.Combine(Application.dataPath, "Scripts/Presentation/Rendering");
            var named = Directory.GetFiles(dir, "FlatPixel*.cs").Where(f => !f.EndsWith("FlatPixelWildFaunaCatalog.cs"))
                .SelectMany(f => Regex.Matches(File.ReadAllText(f), "\"([A-Z][A-Za-z0-9]+)\"").Cast<Match>().Select(m => m.Groups[1].Value)).ToHashSet();
            var items = JsonUtility.FromJson<FileData>(Resources.Load<TextAsset>(FlatPixelWildFaunaCatalog.ItemIndexResourcePath).text).Icons.Select(r => r.Blueprint).ToHashSet();
            var props = Index().Icons.Select(r => r.Blueprint).Distinct().ToList();
            CollectionAssert.IsEmpty(props.Where(named.Contains).ToList(), "approved art keeps its owners");
            CollectionAssert.IsEmpty(props.Where(items.Contains).ToList(), "an item is never also a prop");
        }
    }
}
