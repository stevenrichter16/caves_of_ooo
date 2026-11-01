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
    /// Glyph → pixel, items (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md): items lying on the ground draw
    /// with their 16x16 icon from the generated atlas (ArtSource/Live16x24/item-icons), outside
    /// opening zones; carried, worn or recoloured items keep their glyph; items another flat
    /// catalog already draws are never indexed.
    /// </summary>
    public sealed class FlatPixelItemIconsTests
    {
        static EntityFactory factory;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;
        Zone zone;

        [System.Serializable] sealed class Row { public string Blueprint; public int Frame; public string Glyph; public string Color; }
        [System.Serializable] sealed class FileData { public int Columns; public int FrameHeight; public Row[] Icons; }
        static FileData Index() => JsonUtility.FromJson<FileData>(Resources.Load<TextAsset>(FlatPixelWildFaunaCatalog.ItemIndexResourcePath).text);

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

        Entity Drop(string blueprint)
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

        [TestCase("Antidote")] [TestCase("Battleaxe")] [TestCase("Codex01")] [TestCase("ChainMail")]
        [TestCase("AbsentmintSeed")] [TestCase("IceLanceGrimoire")] [TestCase("RawMeat")] [TestCase("SealedLetter")]
        public void AnItemOnTheGroundDrawsItsIcon(string blueprint)
        {
            var row = Index().Icons.Single(r => r.Blueprint == blueprint);
            var e = Drop(blueprint);
            Assert.True(Native(e, out var v), blueprint + " has an icon");
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.ItemAtlasResourcePath), v.ForegroundTile.sprite.texture);
            var atlas = v.ForegroundTile.sprite.texture; int columns = Index().Columns, h = Index().FrameHeight, rows = atlas.height / h;
            Assert.AreEqual(new Rect(row.Frame % columns * 16, (rows - 1 - row.Frame / columns) * h, 16, h), v.ForegroundTile.sprite.rect, "its own frame");
            var (x, y) = zone.GetEntityPosition(e);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, e, x, y, out var portrait, out _));
            Assert.AreSame(atlas, portrait.texture, "the reader shows the same icon");
        }

        [Test] public void ARecolouredItemKeepsItsGlyph()
        {
            var e = Drop("Antidote"); e.GetPart<RenderPart>().ColorString = "&R";
            Assert.False(Native(e, out _), "counter-check: not the canonical owner");
        }

        // ════════════════ in a pack, or worn ════════════════

        Entity Holder()
        {
            var holder = factory.CreateEntity("Apothecary");
            for (int y = 3; y < Zone.Height - 3; y++) for (int x = 3; x < Zone.Width - 3; x++)
            {
                var c = zone.GetCell(x, y);
                if (c.BlocksMovement() || c.Objects.Any(o => !o.HasTag("Terrain"))) continue;
                if (zone.AddEntity(holder, x, y)) return holder;
            }
            Assert.Fail("no room"); return null;
        }
        bool ReaderIcon(Entity holder, Entity item, out Sprite portrait)
        {
            var (x, y) = zone.GetEntityPosition(holder);
            return world.TryGetPresentationSubjectSprite(zone, item, x, y, out portrait, out _) && portrait != null
                && portrait.texture == Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.ItemAtlasResourcePath);
        }

        [Test] public void AnItemInAPackShowsItsIconInTheReader()
        {
            var holder = Holder(); var item = factory.CreateEntity("Antidote");
            Assert.True(holder.GetPart<InventoryPart>().AddObject(item));
            Assume.That(item.GetPart<PhysicsPart>().InInventory, Is.SameAs(holder));
            Assert.True(ReaderIcon(holder, item, out var portrait), "the ledger portrait is the ground icon, not the glyph");
            var row = Index().Icons.Single(r => r.Blueprint == "Antidote"); var atlas = portrait.texture;
            int h = Index().FrameHeight;
            Assert.AreEqual(new Rect(row.Frame % Index().Columns * 16, (atlas.height / h - 1 - row.Frame / Index().Columns) * h, 16, h), portrait.rect);
        }

        [Test] public void WornArmourShowsItsIconInTheReader()
        {
            var holder = Holder(); var mail = factory.CreateEntity("ChainMail");
            var pack = holder.GetPart<InventoryPart>();
            Assert.True(pack.AddObject(mail));
            var body = holder.GetPart<Body>().GetParts().First(p => p.Type == "Body");
            Assert.True(pack.EquipToBodyPart(mail, body));
            Assume.That(mail.GetPart<PhysicsPart>().Equipped, Is.SameAs(holder));
            Assert.True(ReaderIcon(holder, mail, out _));
        }

        [Test] public void ARecolouredCarriedItemKeepsItsGlyph()
        {
            var holder = Holder(); var item = factory.CreateEntity("Antidote");
            Assert.True(holder.GetPart<InventoryPart>().AddObject(item));
            item.GetPart<RenderPart>().ColorString = "&R";
            Assert.False(ReaderIcon(holder, item, out _), "counter-check: not the canonical item");
        }

        [Test] public void AnItemLyingInAZoneNeverTakesThePackRoute()
        {
            var e = Drop("Antidote");
            using (var wild = new FlatPixelWildFaunaCatalog())
                Assert.False(wild.TryResolvePortableItem(e, out _), "the floor's reader follows the floor's cell, even in opening zones");
        }

        [Test] public void EveryIndexedItemIsARealItemInsideTheAtlas()
        {
            var index = Index(); var atlas = Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.ItemAtlasResourcePath);
            Assert.NotNull(atlas); Assert.Greater(index.Icons.Length, 200);
            Assert.AreEqual(24, index.FrameHeight, "items stand in the game's 16x24 cell, resting on the floor");
            int frames = (atlas.width / 16) * (atlas.height / index.FrameHeight);
            Assert.AreEqual(index.Icons.Length, index.Icons.Select(r => r.Frame).Distinct().Count(), "one frame each");
            foreach (var r in index.Icons)
            {
                Assert.True(factory.Blueprints.ContainsKey(r.Blueprint), r.Blueprint);
                Assert.Less(r.Frame, frames, r.Blueprint);
                var render = factory.CreateEntity(r.Blueprint).GetPart<RenderPart>();
                Assert.AreEqual(r.Glyph, render.RenderString, r.Blueprint); Assert.AreEqual(r.Color, render.ColorString, r.Blueprint);
            }
        }

        [Test] public void ItemsWithApprovedArtAreNeverIndexed()
        {
            string dir = Path.Combine(Application.dataPath, "Scripts/Presentation/Rendering");
            var named = Directory.GetFiles(dir, "FlatPixel*.cs").Where(f => !f.EndsWith("FlatPixelWildFaunaCatalog.cs"))
                .SelectMany(f => Regex.Matches(File.ReadAllText(f), "\"([A-Z][A-Za-z0-9]+)\"").Cast<Match>().Select(m => m.Groups[1].Value)).ToHashSet();
            var overlap = Index().Icons.Select(r => r.Blueprint).Where(named.Contains).ToList();
            CollectionAssert.IsEmpty(overlap, "approved art keeps its owners");
        }
    }
}
