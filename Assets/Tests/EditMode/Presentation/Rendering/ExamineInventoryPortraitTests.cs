using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class ExamineInventoryPortraitTests
    {
        const string Spread = "Overworld.12.10.0";
        // Real content, current manager/zone and real model assets, without a
        // whole-zone presenter bind for every one-object portrait assertion.
        sealed class InventoryPortraitFixture : IDisposable
        {
            readonly EntityEquipmentContentFixture content = new EntityEquipmentContentFixture();
            readonly bool oldEnabled = Village3DSettings.Enabled;
            readonly EntityFactory oldHarvest = HarvestablePart.Factory;
            public EntityFactory Factory => content.Factory;
            public readonly OverworldZoneManager Manager;
            public readonly Zone Zone;
            public readonly Entity Player;
            public InventoryPortraitFixture(string zoneId)
            {
                Village3DSettings.Enabled = true; HarvestablePart.Factory = Factory;
                Manager = new OverworldZoneManager(Factory, 729490642); Zone = Manager.GetZone(zoneId); Manager.SetActiveZone(Zone);
                Player = Factory.CreateEntity("Player"); var cell = FreeCell(); Assert.True(Zone.AddEntity(Player, cell.X, cell.Y));
                foreach (var c in Zone.Cells) c.IsVisible = c.Explored = true;
            }
            Cell FreeCell()
            {
                foreach (var cell in Zone.Cells)
                    if (cell.X > 0 && cell.X < 79 && cell.Y > 0 && cell.Y < 24
                        && !cell.BlocksMovement(Player) && !cell.Objects.Contains(Player)) return cell;
                Assert.Fail("No open cell in the real portrait fixture."); return null;
            }
            public Entity Add(string blueprint)
            { var item = Factory.CreateEntity(blueprint); Assert.NotNull(item); var cell = FreeCell(); Assert.True(Zone.AddEntity(item, cell.X, cell.Y)); return item; }
            public Entity Equip(Entity actor, string blueprint)
            { var item = Factory.CreateEntity(blueprint); Assert.True(actor.GetPart<InventoryPart>().AddObject(item)); Assert.True(InventorySystem.Equip(actor, item)); return item; }
            public void Dispose()
            { HarvestablePart.Factory = oldHarvest; Village3DSettings.Enabled = oldEnabled; content.Dispose(); }
        }
        static ExaminePortraitRenderer Probe()
            => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { (Action<Camera, RenderTexture>)((camera, target) => { }) }, null);
        static T Read<T>(ExaminePortraitRenderer renderer, string property)
            => (T)typeof(ExaminePortraitRenderer).GetProperty(property, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(renderer);
        static ExaminePortrait Portrait(ExaminePortraitRenderer renderer, AnnouncementDocument document)
        {
            var method = typeof(ExaminePortraitRenderer).GetMethod("GetPortrait", new[] { typeof(AnnouncementDocument) });
            Assert.NotNull(method, "The document-authorized portrait route must support carried native art without moving the item into a zone.");
            try { return (ExaminePortrait)method.Invoke(renderer, new object[] { document }); }
            catch (TargetInvocationException error) { throw error.InnerException ?? error; }
        }
        static Entity Carry(InventoryPortraitFixture fixture, string blueprint)
        {
            var item = fixture.Factory.CreateEntity(blueprint); Assert.NotNull(item);
            Assert.True(fixture.Player.GetPart<InventoryPart>().AddObject(item)); return item;
        }

        [TestCase("FireClay", "repair-cultivation-fire-clay")]
        [TestCase("SteelBladeComponent", "spread-portable-steelbladecomponent")]
        [TestCase("KnotflaxCord", "repair-cultivation-knotflax-cord")]
        [TestCase("HearthbulbSeed", "repair-cultivation-hearthbulb-seed")]
        [TestCase("SeamleafSprig", "repair-cultivation-seamleaf-sprig")]
        [TestCase("ClaspbeanSeed", "biome-crop-claspbean-seed")]
        [TestCase("ClaspbeanPulp", "biome-crop-claspbean-harvest")]
        public void CarriedItemUsesItsExactCurrentFloorModelWithoutChangingTheWorld(string blueprint, string model)
        {
            using (var f = new InventoryPortraitFixture(Spread))
            using (var renderer = Probe())
            {
                var item = f.Add(blueprint);
                var world = renderer.GetPortrait(f.Zone, item); Assert.True(world.IsModel);
                Assert.AreEqual(model, renderer.CurrentModelId);
                var source = Read<GameObject>(renderer, "CurrentSourcePrefab");
                f.Zone.RemoveEntity(item); Assert.True(f.Player.GetPart<InventoryPart>().AddObject(item));
                var document = ExamineDocumentBuilder.BuildInventory(item, f.Player); Assert.NotNull(document);
                string tileState = SpawnRing3DIntegrationFixture.Tiles(f.Zone), prose = document.ToPlainText();
                int cacheCount = f.Manager.CachedZones.Count, inventoryCount = f.Player.GetPart<InventoryPart>().Objects.Count;
                var result = Portrait(renderer, document);
                Assert.True(result.IsModel); Assert.AreEqual(model, renderer.CurrentModelId);
                Assert.AreSame(source, Read<GameObject>(renderer, "CurrentSourcePrefab"));
                Assert.AreSame(world.Texture, result.Texture); Assert.AreEqual(1, Read<int>(renderer, "CaptureCount"));
                Assert.Null(typeof(Entity).GetField("SpatialZone", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item));
                Assert.Null(f.Zone.GetEntityCell(item));
                Assert.AreSame(f.Player, item.GetPart<PhysicsPart>().InInventory);
                Assert.AreEqual(inventoryCount, f.Player.GetPart<InventoryPart>().Objects.Count);
                Assert.AreEqual(cacheCount, f.Manager.CachedZones.Count);
                Assert.AreEqual(tileState, SpawnRing3DIntegrationFixture.Tiles(f.Zone)); Assert.AreEqual(prose, document.ToPlainText());
            }
        }

        [Test] public void EquippedWeaponCanUseTheSameNativePortableModel()
        {
            using (var f = new InventoryPortraitFixture(Spread))
            using (var renderer = Probe())
            {
                var item = f.Equip(f.Player, "LongSword"); var document = ExamineDocumentBuilder.BuildInventory(item, f.Player);
                Assert.NotNull(document); Assert.True(Portrait(renderer, document).IsModel);
                Assert.AreEqual("spread-portable-longsword", renderer.CurrentModelId);
                Assert.AreSame(f.Player, item.GetPart<PhysicsPart>().Equipped); Assert.Null(item.GetPart<PhysicsPart>().InInventory);
            }
        }

        [Test] public void CurrentBrewFormChangesTheCarriedModelWhileReusingTheSmallRenderTarget()
        {
            using (var f = new InventoryPortraitFixture(Spread))
            using (var renderer = Probe())
            {
                var item = Carry(f, "BrewedTonic");
                var brew = new BrewItemPart { Form = "Coating" }; item.AddPart(brew);
                var document = ExamineDocumentBuilder.BuildInventory(item, f.Player); Assert.NotNull(document);
                var first = Portrait(renderer, document); Assert.True(first.IsModel);
                Assert.AreEqual("spread-portable-brew-coating", renderer.CurrentModelId);
                brew.Form = "Throwable";
                var second = Portrait(renderer, document); Assert.True(second.IsModel);
                Assert.AreEqual("spread-portable-brew-throwable", renderer.CurrentModelId);
                Assert.AreSame(first.Texture, second.Texture); Assert.AreEqual(2, Read<int>(renderer, "CaptureCount"));
                Assert.AreEqual("Throwable", brew.Form); Assert.AreSame(f.Player, item.GetPart<PhysicsPart>().InInventory);
            }
        }

        [TestCase("lost-owner")] [TestCase("hidden")] [TestCase("empty-stack")]
        public void StaleInventoryDocumentCannotKeepItsPreviousNativePortrait(string change)
        {
            using (var f = new InventoryPortraitFixture(Spread))
            using (var renderer = Probe())
            {
                var item = Carry(f, "FireClay"); var document = ExamineDocumentBuilder.BuildInventory(item, f.Player);
                Assert.True(Portrait(renderer, document).IsModel);
                if (change == "lost-owner") f.Player.GetPart<InventoryPart>().Objects.Remove(item);
                if (change == "hidden") item.GetPart<RenderPart>().Visible = false;
                if (change == "empty-stack") item.GetPart<StackerPart>().StackCount = 0;
                Assert.Null(Portrait(renderer, document).Texture);
                Assert.Null(renderer.CurrentModelId); Assert.Null(Read<GameObject>(renderer, "PreviewRoot"));
            }
        }

        [Test] public void DetachedViewerKeepsOnlyTheOwnedItemsHonestGlyph()
        {
            using (var f = new InventoryPortraitFixture(Spread))
            using (var renderer = Probe())
            {
                var item = Carry(f, "FireClay"); var document = ExamineDocumentBuilder.BuildInventory(item, f.Player);
                Assert.True(Portrait(renderer, document).IsModel); f.Zone.RemoveEntity(f.Player);
                var result = Portrait(renderer, document);
                Assert.NotNull(result.Texture); Assert.False(result.IsModel); Assert.Null(renderer.CurrentModelId);
                Assert.AreEqual(item.GetPart<RenderPart>().ColorString, document.GlyphColor);
            }
        }

        [TestCase("visual-override")] [TestCase("footprint")] [TestCase("invalid-seed")]
        public void UnsupportedCurrentItemAppearanceFallsBackWithoutInventingAWorldModel(string change)
        {
            using (var f = new InventoryPortraitFixture(Spread))
            using (var renderer = Probe())
            {
                var item = Carry(f, change == "invalid-seed" ? "ClaspbeanSeed" : "FireClay");
                if (change == "visual-override") item.GetPart<RenderPart>().VisualID = "unmapped-ledger-probe";
                if (change == "footprint") item.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
                if (change == "invalid-seed") item.GetPart<SeedPart>().CropBlueprint = "HearthbulbCrop";
                var document = ExamineDocumentBuilder.BuildInventory(item, f.Player); Assert.NotNull(document);
                var result = Portrait(renderer, document); Assert.NotNull(result.Texture); Assert.False(result.IsModel);
                Assert.Null(Read<RenderTexture>(renderer, "OwnedTarget"));
            }
        }

        [Test] public void PlainDocumentAndAnUnownedBareEntityHaveNoPortraitAuthority()
        {
            using (var renderer = Probe())
            {
                Assert.Null(Portrait(renderer, null).Texture);
                Assert.Null(Portrait(renderer, new AnnouncementDocument("Announcement")).Texture);
                Assert.Null(Portrait(renderer, new AnnouncementDocument("Unowned", subject: new Entity())).Texture);
            }
        }
    }
}
