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
        public void EveryPortableBlueprintAndStorageOwnerHasAnIcon()
        {
            bool oldChoice = InventoryIconSettings.Enabled;
            InventoryIconSettings.Enabled = true;
            try
            {
                var factory = new EntityFactory();
                factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
                var templates = new HashSet<string>
                {
                    "Item", "ArmorItem", "FoodItem", "ReagentItem", "TonicItem", "TrinketItem", "WeaponComponentItem"
                };
                // Normal village quests and dismemberment create these items directly.
                var runtimeItems = new[] { "DetectiveNotebook", "CrunchyLocket", "SeveredLimb" };
                var names = factory.Blueprints.Values.Where(blueprint => !templates.Contains(blueprint.Name)
                    && (blueprint.Parts.ContainsKey("Container") || !blueprint.Tags.ContainsKey("Creature")
                        && blueprint.Parts.TryGetValue("Physics", out var physics)
                        && physics.TryGetValue("Takeable", out var takeable)
                        && bool.TryParse(takeable, out bool canTake) && canTake))
                    .Select(blueprint => blueprint.Name).Concat(runtimeItems).OrderBy(name => name).ToArray();
                Assert.That(names.Length, Is.GreaterThan(380));
                var fallback = InventoryItemIcons.For(new Entity { BlueprintName = "unknown" });
                Assert.NotNull(fallback);
                var missing = names.Where(name =>
                {
                    var icon = InventoryItemIcons.For(new Entity { BlueprintName = name });
                    return icon == null || icon == fallback;
                });
                Assert.That(missing, Is.Empty);
            }
            finally
            {
                InventoryIconSettings.Enabled = oldChoice;
            }
        }

        [TestCase("Sporeblade", "VenomGland", false)]
        [TestCase("CurationInspectionKey", "SealbarkSlat", false)]
        [TestCase("AbsentmintLeaf", "LiquidFlask", false)]
        [TestCase("Chest", "LockedChest", true)]
        [TestCase("Chest", "MimicChest", true)]
        public void InventoryArtDistinguishesDifferentBodiesAndPreservesSharedVariants(
            string firstType, string secondType, bool identicalArt)
        {
            // Separate frame indices do not prove a blade, key or leaf has its own body shape.
            // Chest variants deliberately share pixels so the icon does not reveal a mimic.
            bool oldChoice = InventoryIconSettings.Enabled;
            var image = new Texture2D(2, 2);
            InventoryIconSettings.Enabled = true;
            try
            {
                Assert.True(image.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,
                    "Resources", InventoryItemAtlas + ".png"))));
                var pixels = image.GetPixels32();
                Color32[] first = ReadIcon(firstType), second = ReadIcon(secondType);
                Assert.True(first.Any(pixel => pixel.a != 0), firstType);
                Assert.True(second.Any(pixel => pixel.a != 0), secondType);
                if (identicalArt)
                    CollectionAssert.AreEqual(first, second, "Disclosure-equivalent chest bodies must keep shared art.");
                else
                    Assert.False(first.Select(pixel => pixel.a != 0).SequenceEqual(
                        second.Select(pixel => pixel.a != 0)),
                        firstType + " must not borrow " + secondType + "'s silhouette.");

                Color32[] ReadIcon(string blueprint)
                {
                    var sprite = InventoryItemIcons.For(new Entity { BlueprintName = blueprint });
                    Assert.NotNull(sprite, blueprint);
                    Assert.AreEqual(new Vector2(16, 16), sprite.rect.size, blueprint);
                    var result = new Color32[16 * 16];
                    for (int y = 0; y < 16; y++)
                        for (int x = 0; x < 16; x++)
                            result[y * 16 + x] = pixels[((int)sprite.rect.y + y) * image.width
                                + (int)sprite.rect.x + x];
                    return result;
                }
            }
            finally
            {
                InventoryIconSettings.Enabled = oldChoice;
                Object.DestroyImmediate(image);
            }
        }

        [Test]
        public void StowShowsTheExactItemsIconInsideTheChoiceAndHonorsTheSetting()
        {
            bool oldChoice = InventoryIconSettings.Enabled;
            InventoryIconSettings.Enabled = true;
            try
            {
                using var r = new Rig();
                var vessel = r.Factory.CreateEntity("FieldSatchel");
                Assert.True(r.Pack.AddObject(vessel));
                var item = r.Carry("HealingTonic");
                var sprite = InventoryItemIcons.For(item);
                Assert.NotNull(sprite);
                r.Open();
                r.Tab(1);
                Call(r.UI, "OpenCarriedStorageStow", vessel, vessel.GetPart<ContainerPart>());
                object choice = r.FindHit("PopupRow", 0);
                Assert.True(HasChoiceIcon());

                InventoryIconSettings.Enabled = false;
                Call(r.UI, "Render");
                Assert.False(HasChoiceIcon());
                r.ActivatePainted(r.FindHit("PopupRow", 0));
                Assert.AreSame(vessel, item.GetPart<PhysicsPart>().InInventory);
                Assert.False(r.Pack.Contains(item));

                bool HasChoiceIcon()
                {
                    var content = (RectTransform)Get(r.UI, "_ledgerContent");
                    return r.UI.GetComponentsInChildren<Image>(true).Any(image =>
                    {
                        if (!image.isActiveAndEnabled || image.sprite != sprite || image.raycastTarget) return false;
                        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, image.rectTransform);
                        float x = bounds.min.x - content.rect.xMin;
                        float y = content.rect.yMax - bounds.max.y;
                        var row = Bounds(choice);
                        return x >= row.xMin + 8 && x <= row.xMin + 12 && y >= row.yMin && y < row.yMax;
                    });
                }
            }
            finally
            {
                InventoryIconSettings.Enabled = oldChoice;
            }
        }

        [Test]
        public void DepositChoiceShowsTheStorageOwnerIconWithoutRevealingItsContents()
        {
            bool oldChoice = InventoryIconSettings.Enabled;
            InventoryIconSettings.Enabled = true;
            try
            {
                using var r = new Rig();
                var item = r.Carry("HealingTonic");
                var basket = r.Factory.CreateEntity("WovenBasket");
                var concealed = r.Factory.CreateEntity("HandPump");
                Assert.True(basket.GetPart<ContainerPart>().AddItem(concealed));
                Assert.True(r.UI.CurrentZone.AddEntity(basket, 10, 10));
                var storageIcon = InventoryItemIcons.For(basket);
                var concealedIcon = InventoryItemIcons.For(concealed);
                Assert.NotNull(storageIcon);
                Assert.NotNull(concealedIcon);
                r.Open();
                Assert.True(r.UI.ReopenItemActionPopupFor(item));
                int index = ActionIndex(r, "put_container");
                var popup = Get(r.UI, "_itemActionPopup");
                popup.GetType().GetField("CursorIndex", Members).SetValue(popup, index);
                Call(r.UI, "Render");
                Assert.True(r.Labels().Any(label => label.Content.Contains("Put in " + basket.GetDisplayName())));
                Assert.True(HasIcon(storageIcon));
                Assert.False(HasIcon(concealedIcon));

                InventoryIconSettings.Enabled = false;
                Call(r.UI, "Render");
                Assert.False(HasIcon(storageIcon));
                r.ActivatePainted(r.FindHit("PopupRow", index));
                Assert.AreSame(basket, item.GetPart<PhysicsPart>().InInventory);
                Assert.AreSame(basket, concealed.GetPart<PhysicsPart>().InInventory);

                bool HasIcon(Sprite sprite) => r.UI.GetComponentsInChildren<Image>(true)
                    .Any(image => image.isActiveAndEnabled && image.sprite == sprite);
            }
            finally
            {
                InventoryIconSettings.Enabled = oldChoice;
            }
        }

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
                var item = r.Factory.CreateEntity(containerPicker ? "WovenBasket" : "HandPump");
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
