using System;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Paint = CavesOfOoo.Tests.FlatPixelBeatingWallShadowTests.Paint;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelStillleafWallShadowTests
    {
        const int X = 10, Y = 10;
        const string Site = "Overworld.2.4.1";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.NonPublic;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        Entity wall, floor;
        EntityFactory priorFactory;

        [SetUp] public void Setup()
        {
            priorFactory = DestructionSystem.EntityFactoryRef;
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 704860946, true);
            manager.GetZone(Site); // Establish the real authored library authority.
            zone = new Zone(Site); manager.SetActiveZone(zone);
            foreach (var c in zone.Cells) c.Explored = c.IsVisible = true;
            wall = Add("SandstoneWall", X, Y); floor = Add("SandstoneFloor", X, Y + 1);
            world = new FlatPixelGreyboxCatalog(); DestructionSystem.EntityFactoryRef = content.Factory;
        }
        [TearDown] public void Cleanup()
        { try { world?.Dispose(); } finally { try { content?.Dispose(); } finally { DestructionSystem.EntityFactoryRef = priorFactory; } } }
        Entity Add(string name, int x, int y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e); Assert.True(zone.AddEntity(e, x, y)); return e; }
        void Present(Paint p)
        {
            p.Step(); var tile = p.Shade.GetTile<Tile>(Paint.Position(X, Y));
            Assert.NotNull(tile, "Disclosed Stillleaf wall over empty native sandstone floor needs bounded depth.");
            Assert.AreSame(p.At(X, Y), tile);
            Assert.AreEqual(new Color(0, 0, 0, .16f), p.Shade.GetColor(Paint.Position(X, Y)));
            Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y + 1)));
        }
        void Absent(Paint p) { p.Step(); Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y))); }
        void Parity(Paint p)
        {
            var tile = p.Shade.GetTile(Paint.Position(X, Y));
            p.Renderer.MarkDirty("Stillleaf full paint counter-case"); p.Step();
            Assert.AreSame(tile, p.Shade.GetTile(Paint.Position(X, Y)));
        }

        [Test] public void SubmittedShadeIsTwoPixelsSouthUsingOpaqueBorrowedWallAndPreservesWorld()
        {
            using (var p = new Paint(world, zone))
            {
                int version = zone.EntityVersion, hp = wall.GetPart<DestructiblePart>().HP;
                string state = zone.TileState.ToSaveString(); Present(p);
                var tile = p.Shade.GetTile<Tile>(Paint.Position(X, Y));
                Assert.AreSame(Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath), tile.sprite.texture);
                Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
                Assert.IsNull(p.Shade.GetComponent<TilemapCollider2D>());
                Assert.AreEqual(0, p.Shade.GetComponent<TilemapRenderer>().sortingOrder);
                Assert.That(Vector3.Distance(new Vector3(0, -2f / 16, 0), p.ShadeBounds(X, Y).center - p.BodyBounds(X, Y).center), Is.LessThan(.0001f));
                Assert.True(zone.GetCell(X, Y).BlocksMovement()); Assert.False(zone.GetCell(X, Y + 1).BlocksMovement());
                Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(hp, wall.GetPart<DestructiblePart>().HP);
                Assert.AreEqual(state, zone.TileState.ToSaveString());
                var pixels = new Texture2D(2, 2);
                try
                {
                    Assert.True(pixels.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,"Resources/Sprites/Live16x24/shallow-caves.png"))));
                    foreach (var color in pixels.GetPixels()) Assert.AreEqual(1f, color.a);
                }
                finally { UnityEngine.Object.DestroyImmediate(pixels); }
            }
        }

        [TestCase("DescentLedge")] [TestCase("Player")] [TestCase("Dagger")]
        [TestCase("Rubble")] [TestCase("WaterPuddle")] [TestCase("SandstoneFloor")]
        public void ReceiverOnlyDirtyOccupancyWithdrawsShadeWithoutChangingWallsOrLedges(string name)
        {
            using (var p = new Paint(world, zone))
            {
                Present(p); var body = p.At(X, Y); var other = Add(name, X, Y + 1);
                p.Dirty(X, Y + 1); Assert.False(p.FullDirty); Absent(p); Parity(p);
                Assert.AreSame(body, p.At(X, Y));
                if (name == "DescentLedge")
                {
                    Assert.False(other.GetPart<PhysicsPart>().Solid);
                    Assert.True(world.TryResolvePresentation(zone, X, Y + 1, out var v));
                    Assert.AreSame(other, v.Owner); Assert.True(v.NativeForeground); Assert.True(v.LocalGround);
                }
                if (name == "Dagger")
                {
                    other.GetPart<RenderPart>().Visible = false; p.Dirty(X, Y + 1); Present(p);
                    other.GetPart<RenderPart>().Visible = true; p.Dirty(X, Y + 1); Absent(p);
                }
                zone.RemoveEntity(other); p.Dirty(X, Y + 1); Present(p); Parity(p);
            }
        }

        [TestCase(false)] [TestCase(true)] public void SourceAndReceiverMustRemainVisibleExploredAndFreeOfTileState(bool source)
        {
            using (var p = new Paint(world, zone))
            {
                Present(p); int y = source ? Y : Y + 1; var c = zone.GetCell(X, y);
                c.IsVisible = false; p.Dirty(X, y); Absent(p);
                c.IsVisible = true; p.Dirty(X, y); Present(p);
                c.Explored = false; p.Dirty(X, y); Absent(p);
                c.Explored = true; p.Dirty(X, y); Present(p);
                zone.TileState.WriteCoating(X, y, "oil", 5); Absent(p); Parity(p);
                zone.TileState.RemoveCoating(X, y, "oil"); Present(p); Parity(p);
            }
        }

        [Test] public void NoFloorOrChangedOwnerCannotBorrowAmbientBackingAndDestructionClearsShade()
        {
            using (var p = new Paint(world, zone))
            {
                Present(p); zone.RemoveEntity(floor); p.Dirty(X, Y + 1); Absent(p);
                zone.AddEntity(floor, X, Y + 1); p.Dirty(X, Y + 1); Present(p);
                floor.GetPart<RenderPart>().ColorString = "&R"; p.Dirty(X, Y + 1); Absent(p);
                floor.GetPart<RenderPart>().ColorString = "&W"; p.Dirty(X, Y + 1); Present(p);
                wall.GetPart<RenderPart>().Visible = false; p.Dirty(X, Y); Absent(p);
                wall.GetPart<RenderPart>().Visible = true; p.Dirty(X, Y); Present(p);
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(wall, 999, null, zone)); Absent(p); Parity(p);
                Assert.AreSame(floor, zone.GetCell(X, Y + 1).GetTopVisibleObject());
            }
        }

        [Test] public void OptionAndLegacyExitClearShadeWithoutChangingBodies()
        {
            using (var p = new Paint(world, zone))
            {
                Present(p); var body = p.At(X, Y); var ground = p.GroundAt(X, Y + 1);
                p.Environment.FlatWallShadowEnabled = false; Absent(p);
                p.Environment.FlatWallShadowEnabled = true; Present(p);
                p.Renderer.FlatPixelModeEnabled = false; Absent(p);
                p.Renderer.FlatPixelModeEnabled = true; Present(p);
                Assert.AreSame(body, p.At(X, Y)); Assert.AreSame(ground, p.GroundAt(X, Y + 1));
            }
        }

        [TestCase("graph")] [TestCase("profile")] [TestCase("poi")]
        [TestCase("tiles")] [TestCase("pois")] [TestCase("lair")]
        public void WarmLibraryAuthorityRevokesAndValidStoryRenameDoesNot(string change)
        {
            using (var p = new Paint(world, zone))
            {
                Present(p); var poi = manager.WorldMap.GetPOI(2, 4);
                poi.Name = "renamed by story"; p.Dirty(X, Y); Present(p);
                switch (change)
                {
                    case "graph": manager.CachedZones[Site] = new Zone(Site); break;
                    case "profile": poi.Profile = SinkholeSites.FoundingVillageProfile; break;
                    case "poi": manager.WorldMap.SetPOI(2, 4, null); break;
                    case "tiles": manager.WorldMap.Tiles = new BiomeType[1,1]; break;
                    case "pois": manager.WorldMap.POIs = new PointOfInterest[1,1]; break;
                    case "lair": manager.WorldMap.SetPOI(2,4,new PointOfInterest(POIType.Lair,"retained lair",null,1,"DesertProwler")); LairStacks.Restore(manager,null); manager.WorldMap.SetPOI(2,4,poi); break;
                }
                p.Dirty(X, Y); Absent(p); Parity(p);
            }
        }

        [TestCase("Overworld.2.4.0")] [TestCase("Overworld.2.4.2")]
        [TestCase("Overworld.2.7.1")] [TestCase("Overworld.02.4.1")]
        public void OtherDepthsGinmereAndAliasNeverBorrowStillleafShade(string id)
        {
            using (var p = new Paint(world, zone)) Present(p);
            zone = new Zone(id); manager.SetActiveZone(zone);
            foreach (var c in zone.Cells) c.Explored = c.IsVisible = true;
            Add("SandstoneWall", X, Y); Add("SandstoneFloor", X, Y + 1);
            using (var p = new Paint(world, zone)) Absent(p);
        }

        [TestCase(true)] [TestCase(false)] public void MissingWallOrFloorArtCannotBorrowAmbientOrFallbackForShade(bool wallMissing)
        {
            using (var p = new Paint(world, zone))
            {
                Present(p);
                var field = typeof(FlatPixelGreyboxCatalog).GetField("_stillleafDescentArt", I); Assert.NotNull(field);
                var original = field.GetValue(world);
                using (var selector = new FlatPixelFallbackCatalog())
                using (var missing = new FlatPixelShallowCavesCatalog(
                    wallMissing ? null : Resources.Load<Texture2D>(FlatPixelShallowCavesCatalog.ResourcePath),
                    wallMissing ? Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath) : null,
                    null, null, null, null, selector))
                {
                    typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingStillleafDescent", I).Invoke(missing, null);
                    try { field.SetValue(world, missing); p.Renderer.MarkDirty("missing optional art"); Absent(p); }
                    finally { field.SetValue(world, original); }
                }
                p.Renderer.MarkDirty("restored optional art"); Present(p);
            }
        }
    }
}
