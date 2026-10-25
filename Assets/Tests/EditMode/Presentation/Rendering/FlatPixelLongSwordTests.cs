using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Two initial behavioral RED cases. No not-yet-created production API is used.
    public sealed class FlatPixelLongSwordTests
    {
        const string ResourcePath = "Sprites/Live16x24/long-sword";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        EntityFactory factory;
        [OneTimeSetUp] public void Content() => factory = DensityReferenceGladeTests.Factory();

        [Test] public void CurrentLooseLongSwordUsesNativeArtBeforeResourceIsAsserted()
        {
            using (var f = new Fixture())
            {
                var blade = factory.CreateEntity("LongSword");
                Assert.NotNull(blade); Assert.True(f.Zone.AddEntity(blade, 10, 10));
                Assert.True(f.Catalog.TryResolvePresentation(f.Zone, 10, 10, out var world));
                Assert.AreSame(blade, world.Owner); Assert.NotNull(world.ForegroundTile);
                Assert.True(world.NativeForeground,
                    "A real current LongSword still renders its slash fallback; this must fail before the asset assertion.");
                AssertFrame(world.ForegroundTile.sprite.texture, world.ForegroundTile.sprite.rect);
                Assert.AreEqual(Color.white, world.ForegroundColor);
                Assert.True(f.Catalog.TryGetPresentationSubjectSprite(f.Zone, blade, 10, 10, out var subject, out var tint));
                Assert.AreSame(world.ForegroundTile.sprite, subject); Assert.AreEqual(Color.white, tint);
            }
        }

        [Test] public void AuthorizedEquippedLongSwordReaderStopsUsingCanonicalGlyphBeforeResourceIsAsserted()
        {
            using (var f = new Fixture())
            {
                var blade = factory.CreateEntity("LongSword");
                var inventory = f.Viewer.GetPart<InventoryPart>();
                Assert.True(inventory.AddObject(blade)); Assert.True(inventory.Equip(blade, "Hand"));
                f.Presentation.Paused = true;
                var document = ExamineDocumentBuilder.BuildInventory(blade, f.Viewer);
                Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                // A known unsupported identity obtains this same catalog's actual
                // canonical glyph atlas, without adding a second world occupant.
                var other = factory.CreateEntity("Dagger");
                Assert.True(f.Catalog.TryGetPresentationSubjectSprite(f.Zone, other, 9, 10, out var fallback, out _));
                Assert.NotNull(fallback);
                Assert.True(f.Presentation.TryGetFlatExaminePortrait(document, out var portrait));
                Assert.NotNull(portrait.Texture);
                Assert.AreNotSame(fallback.texture, portrait.Texture,
                    "The authorized equipped blade reader still borrows canonical glyph art; prove this before loading the new asset.");
                var resource = Imported(); Assert.AreSame(resource, portrait.Texture);
                Assert.AreEqual(new Rect(0, 0, 1, 1), portrait.Uv);
                Assert.AreEqual(Color.white, portrait.Tint); Assert.False(portrait.IsModel);
                Assert.AreSame(f.Viewer, blade.GetPart<PhysicsPart>().Equipped);
                Assert.IsNull(f.Zone.GetEntityCell(blade));
            }
        }

        static Texture2D Imported()
        {
            var texture = Resources.Load<Texture2D>(ResourcePath);
            Assert.NotNull(texture); Assert.AreEqual(16, texture.width); Assert.AreEqual(24, texture.height);
            Assert.AreEqual(FilterMode.Point, texture.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode);
            Assert.AreEqual(1, texture.mipmapCount); return texture;
        }
        static void AssertFrame(Texture texture, Rect rect)
        { Assert.AreSame(Imported(), texture); Assert.AreEqual(new Rect(0, 0, 16, 24), rect); }

        sealed class Fixture : IDisposable
        {
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly GameObject root;
            public readonly Zone Zone = new Zone(ReferenceGladePlan.ZoneID);
            public readonly FlatPixelGreyboxCatalog Catalog;
            public readonly Entity Viewer;
            public readonly ZoneRenderer Presentation;
            public Fixture()
            {
                try
                {
                    ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
                    Catalog = new FlatPixelGreyboxCatalog();
                    Viewer = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "LongSwordReaderViewer" };
                    Viewer.SetTag("Player"); Viewer.AddPart(new PhysicsPart()); Viewer.AddPart(new InventoryPart());
                    Viewer.AddPart(new RenderPart { RenderString = "@", Visible = true });
                    Viewer.AddPart(new ExaminablePart()); Assert.True(Zone.AddEntity(Viewer, 9, 10));
                    for (int y = 9; y <= 11; y++) for (int x = 9; x <= 12; x++)
                        Zone.GetCell(x, y).Explored = Zone.GetCell(x, y).IsVisible = true;
                    root = new GameObject("LongSword exact-subject fixture"); root.SetActive(false);
                    Presentation = root.AddComponent<ZoneRenderer>();
                    var environment = root.AddComponent<EnvironmentSpriteRenderer>();
                    typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", I).SetValue(environment, Catalog);
                    environment.FlatPixelGreyboxEnabled = true;
                    typeof(ZoneRenderer).GetField("_envSpriteRenderer", I).SetValue(Presentation, environment);
                    typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(Presentation, true);
                    typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, Zone);
                    Assert.True(Presentation.FlatPixelModeActive);
                }
                catch { Dispose(); throw; }
            }
            public void Dispose()
            {
                try { if (root != null) Object.DestroyImmediate(root); Catalog?.Dispose(); }
                finally { ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; }
            }
        }
    }
}
