using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed partial class InventoryLedgerInteractionTests
    {
        const string InventoryItemAtlas = "Sprites/Live16x24/inventory-item-icons";

        [Test]
        public void CurrentToolsAndStorageUseDistinctInventoryFramesAndOutlines()
        {
            bool oldChoice = InventoryIconSettings.Enabled;
            var image = new Texture2D(2, 2);
            try
            {
                InventoryIconSettings.Enabled = true;
                var atlas = Resources.Load<Texture2D>(InventoryItemAtlas);
                Assert.NotNull(atlas);
                Assert.True(image.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,
                    "Resources", InventoryItemAtlas + ".png"))));
                Assert.AreEqual(atlas.width, image.width);
                Assert.AreEqual(atlas.height, image.height);
                var pixels = image.GetPixels32();
                var rects = new HashSet<Rect>();
                var silhouettes = new HashSet<string>();
                var packTools = new[]
                {
                    "HandSieve", "HandPump", "SteamCondenser", "Scraper", "CargoSled", "CampBox",
                    "FieldSatchel", "LiquidFlask", "DriedMeat", "PreparedLiningClay"
                };
                var factory = new EntityFactory();
                factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
                var unknown = new Entity { BlueprintName = "UncataloguedInventoryExample" };
                var fallback = InventoryItemIcons.For(unknown);
                Assert.NotNull(fallback);
                Assert.AreSame(fallback, InventoryItemIcons.For(new Entity { BlueprintName = "unknown" }));
                Assert.AreSame(atlas, fallback.texture);
                Assert.IsNull(InventoryItemIcons.For(null));
                rects.Add(fallback.rect);
                silhouettes.Add(Outline(fallback.rect));

                foreach (string blueprint in packTools)
                {
                    var item = factory.CreateEntity(blueprint);
                    Assert.NotNull(item, blueprint);
                    var icon = InventoryItemIcons.For(item);
                    Assert.NotNull(icon, blueprint);
                    Assert.AreNotSame(fallback, icon, blueprint);
                    Assert.AreSame(atlas, icon.texture, blueprint);
                    Assert.AreSame(icon, InventoryItemIcons.For(item), blueprint);
                    Assert.AreEqual(new Vector2(16, 16), icon.rect.size, blueprint);
                    Assert.IsTrue(rects.Add(icon.rect), blueprint + " shares another checked frame.");
                    string outline = Outline(icon.rect);
                    StringAssert.Contains("#", outline, blueprint);
                    StringAssert.Contains(".", outline, blueprint);
                    Assert.IsTrue(silhouettes.Add(outline), blueprint + " shares another checked silhouette.");
                }

                InventoryIconSettings.Enabled = false;
                Assert.IsNull(InventoryItemIcons.For(factory.CreateEntity("HandPump")));
                Assert.IsNull(InventoryItemIcons.For(unknown));

                string Outline(Rect rect)
                {
                    var outline = new char[(int)rect.width * (int)rect.height];
                    int at = 0;
                    for (int y = (int)rect.yMin; y < (int)rect.yMax; y++)
                        for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
                            outline[at++] = pixels[y * image.width + x].a == 0 ? '.' : '#';
                    return new string(outline);
                }
            }
            finally
            {
                InventoryIconSettings.Enabled = oldChoice;
                Object.DestroyImmediate(image);
            }
        }

        [Test]
        public void DensePackPaintsItsVesselAndOnlyTheExpandedChildWithClickableIcons()
        {
            bool oldChoice = InventoryIconSettings.Enabled;
            InventoryIconSettings.Enabled = true;
            try
            {
                using var r = new Rig();
                var first = r.Factory.CreateEntity("FieldSatchel");
                var child = r.Factory.CreateEntity("HandSieve");
                var second = r.Factory.CreateEntity("FieldSatchel");
                var hidden = r.Factory.CreateEntity("SteamCondenser");
                Assert.True(first.GetPart<ContainerPart>().AddItem(child));
                Assert.True(second.GetPart<ContainerPart>().AddItem(hidden));
                Assert.True(r.Pack.AddObject(first));
                Assert.True(r.Pack.AddObject(second));
                var vesselSprite = InventoryItemIcons.For(first);
                var childSprite = InventoryItemIcons.For(child);
                var hiddenSprite = InventoryItemIcons.For(hidden);
                Assert.NotNull(vesselSprite);
                Assert.NotNull(childSprite);
                Assert.NotNull(hiddenSprite);

                r.Open();
                r.Tab(1);
                Assert.False(Images().Any(icon => icon.sprite == childSprite || icon.sprite == hiddenSprite));
                int firstRow = r.InventoryRow(first);
                var firstHit = r.FindHit("Item", firstRow);
                var vesselIcon = FindIcon(vesselSprite, firstHit);
                r.ActivatePainted(firstHit);
                int childRow = r.InventoryRow(child);
                Assert.AreEqual(firstRow + 1, childRow);
                var childIcon = FindIcon(childSprite, r.FindHit("Item", childRow));
                Assert.Greater(childIcon.xMin, vesselIcon.xMin + 5f);
                Assert.False(Images().Any(icon => icon.sprite == hiddenSprite));
                Assert.AreSame(first, child.GetPart<PhysicsPart>().InInventory);

                InventoryIconSettings.Enabled = false;
                Call(r.UI, "Render");
                Assert.False(Images().Any(), "Turning off icons must clear reused images as well as new rows.");
                Assert.AreEqual(childRow, r.InventoryRow(child));
                Assert.NotNull(r.FindHit("Item", childRow));
                Assert.False(r.UI.ConsumePendingEverydayTurn());

                IEnumerable<Image> Images() => r.UI.GetComponentsInChildren<Image>(true)
                    .Where(icon => icon.isActiveAndEnabled && icon.sprite != null && icon.sprite.texture == vesselSprite.texture);

                Rect FindIcon(Sprite sprite, object row)
                {
                    var content = (RectTransform)Get(r.UI, "_ledgerContent");
                    var hit = Bounds(row);
                    foreach (var icon in Images().Where(icon => icon.sprite == sprite))
                    {
                        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, icon.rectTransform);
                        var rect = new Rect(bounds.min.x - content.rect.xMin, content.rect.yMax - bounds.max.y, bounds.size.x, bounds.size.y);
                        if (!rect.Overlaps(hit)) continue;
                        Assert.False(icon.raycastTarget);
                        int index = r.HitAtScreen(r.ToScreen(rect.center));
                        Assert.GreaterOrEqual(index, 0);
                        Assert.AreEqual(Index(row), Index(r.Hits()[index]));
                        return rect;
                    }
                    Assert.Fail("Expected shared sprite in the actual item-row bounds.");
                    return default;
                }
            }
            finally
            {
                InventoryIconSettings.Enabled = oldChoice;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PickupAndContainerPickerPaintTheSameSpriteAndClearItWhenDisabled(bool containerPicker)
        {
            bool oldChoice = InventoryIconSettings.Enabled;
            InventoryIconSettings.Enabled = true;
            try
            {
                using var r = new Rig();
                var map = r.UI.Tilemap;
                var item = r.Factory.CreateEntity(containerPicker ? "Crate" : "HandPump");
                var sprite = InventoryItemIcons.For(item);
                Assert.NotNull(sprite);
                object menu;
                if (containerPicker)
                {
                    var picker = r.UI.gameObject.AddComponent<ContainerPickerUI>();
                    picker.Tilemap = map;
                    picker.PopupCamera = r.Camera;
                    picker.Open(new List<Entity> { item });
                    menu = picker;
                }
                else
                {
                    var picker = r.UI.gameObject.AddComponent<PickupUI>();
                    picker.Tilemap = map;
                    picker.PopupCamera = r.Camera;
                    picker.PlayerEntity = r.Player;
                    picker.CurrentZone = r.UI.CurrentZone;
                    Assert.True(picker.CurrentZone.AddEntity(item, 10, 10));
                    picker.Open(new List<Entity> { item });
                    menu = picker;
                }
                var cell = new Vector3Int((int)Get(menu, "_worldOriginX") + 5, (int)Get(menu, "_worldTopY") - 3, 0);
                Assert.AreSame(sprite, map.GetSprite(cell));
                InventoryIconSettings.Enabled = false;
                Call(menu, "Render");
                Assert.AreNotSame(sprite, map.GetSprite(cell));
            }
            finally
            {
                InventoryIconSettings.Enabled = oldChoice;
            }
        }
    }
}
