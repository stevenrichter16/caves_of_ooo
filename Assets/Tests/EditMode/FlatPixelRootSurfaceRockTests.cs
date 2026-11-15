using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelRootSurfaceRockTests
    {
        sealed class ExtraRockPart : Part { }
        const int X = 20, Y = 10;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        Entity rock, floor;
        FlatPixelFallbackCatalog selector;
        FlatPixelBeatingMaterialsCatalog art;
        FlatPixelGreyboxCatalog world;
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 704860946, true);
            zone = new Zone("Overworld.3.3.0"); manager.SetActiveZone(zone);
            foreach (var cell in zone.Cells) cell.Explored = cell.IsVisible = true;
            floor = Add("TepuiStone"); rock = Add("Rock");
            selector = new FlatPixelFallbackCatalog();
            art = new FlatPixelBeatingMaterialsCatalog(selector); Admit(art);
            world = new FlatPixelGreyboxCatalog();
        }
        static void Admit(FlatPixelBeatingMaterialsCatalog value)
        {
            // Before the implementation exists, world/native assertions supply the RED premise.
            typeof(FlatPixelBeatingMaterialsCatalog).GetMethod("AdmittingRootSurfaceRocks",
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(value, null);
        }
        Entity Add(string name) { var e = content.Factory.CreateEntity(name); Assert.NotNull(e); Assert.True(zone.AddEntity(e, X, Y)); return e; }
        [TearDown] public void Cleanup() { world?.Dispose(); art?.Dispose(); selector?.Dispose(); content?.Dispose(); }
        bool Body() => art.TryResolveForeground(zone, X, Y, rock, out _);
        void Native()
        {
            Assert.True(world.TryResolvePresentation(zone, X, Y, out var view));
            Assert.True(view.NativeForeground, "canonical Root rock must have a native body");
            Assert.True(view.LocalGround); Assert.True(view.BlocksMovement); Assert.AreSame(rock, view.Owner);
            Assert.True(Body());
            var atlas = Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath);
            Assert.AreSame(atlas, view.ForegroundTile.sprite.texture);
            Assert.That(view.ForegroundTile.sprite.rect.x / 16f, Is.InRange(18f, 19f));
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath), view.GroundTile.sprite.texture);
        }
        [Test] public void CanonicalBodyAndReaderKeepRealGroundAndOwners()
        {
            int version = zone.EntityVersion; string state = zone.TileState.ToSaveString();
            string before = string.Join("|", rock.Parts.Select(p => JsonUtility.ToJson(p)));
            Native(); Assert.True(world.TryGetPresentationSubjectSprite(zone, rock, X, Y, out var sprite, out _));
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath), sprite.texture);
            Assert.AreEqual(before, string.Join("|", rock.Parts.Select(p => JsonUtility.ToJson(p))));
            Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(state, zone.TileState.ToSaveString());
            Assert.AreEqual(2, zone.GetCell(X, Y).Objects.Count); Assert.IsNull(rock.GetPart<DestructiblePart>());
        }
        [Test] public void GeneratedRootRocksUseOnlyApprovedBodiesOverTheirOwnStone()
        {
            manager.CachedZones.Remove(zone.ZoneID); zone = manager.GetZone("Overworld.3.3.0"); manager.SetActiveZone(zone);
            foreach (var cell in zone.Cells) cell.Explored = cell.IsVisible = true;
            var rocks = zone.GetAllEntities().Where(e => e.BlueprintName == "Rock").ToArray(); Assert.AreEqual(92, rocks.Length);
            foreach (var e in rocks)
            {
                var c = zone.GetEntityCell(e); Assert.True(c.Objects.Any(v => v.BlueprintName == "TepuiStone"));
                Assert.True(art.TryResolveForeground(zone, c.X, c.Y, e, out var tile), e.ID);
                Assert.AreSame(Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath), tile.sprite.texture);
            }
        }
        [TestCase("hidden")][TestCase("memory")][TestCase("unknown")][TestCase("moved")]
        [TestCase("custody")][TestCase("solid")][TestCase("material")][TestCase("temperature")]
        [TestCase("color")][TestCase("background")][TestCase("detail")][TestCase("tilecolor")]
        [TestCase("glyph")][TestCase("layer")][TestCase("variant")][TestCase("tile")][TestCase("visual")]
        [TestCase("extraTag")][TestCase("nullTags")][TestCase("charge")][TestCase("coating")]
        [TestCase("rootFace")][TestCase("hearth")][TestCase("seventh")][TestCase("plume")]
        [TestCase("restable")][TestCase("destructible")][TestCase("foreignParent")][TestCase("missingExam")]
        public void ChangedOrUndisclosedRocksKeepFallback(string fault)
        {
            Native(); var r = rock.GetPart<RenderPart>();
            switch (fault)
            {
                case "hidden": r.Visible = false; break;
                case "memory": zone.GetCell(X,Y).IsVisible = false; break;
                case "unknown": zone.GetCell(X,Y).IsVisible = zone.GetCell(X,Y).Explored = false; break;
                case "moved": zone.RemoveEntity(rock); break;
                case "custody": rock.GetPart<PhysicsPart>().InInventory = new Entity(); break;
                case "solid": rock.GetPart<PhysicsPart>().Solid = false; break;
                case "material": rock.GetPart<MaterialPart>().MaterialID = "Wood"; break;
                case "temperature": rock.GetPart<ThermalPart>().Temperature = rock.GetPart<ThermalPart>().FlameTemperature; break;
                case "color": r.ColorString = "&R"; break;
                case "background": r.BackgroundColor = "^r"; break;
                case "detail": r.DetailColor = "&R"; break;
                case "tilecolor": r.TileColor = "&R"; break;
                case "glyph": r.RenderString = "!"; break;
                case "layer": r.RenderLayer = 2; break;
                case "variant": r.GlyphVariants = "oO"; break;
                case "tile": r.Tile = "other"; break;
                case "visual": r.VisualID = "other"; break;
                case "extraTag": rock.Tags["Restable"] = ""; break;
                case "nullTags": rock.Tags = null; break;
                case "charge": zone.TileState.AddCharge(X,Y,1); break;
                case "coating": zone.TileState.WriteCoating(X,Y,"oil",5); break;
                case "rootFace": rock.AddPart(new RootFacePart()); break;
                case "hearth": rock.AddPart(new HearthPatchPart()); break;
                case "seventh": rock.AddPart(new SeventhPositionPart()); break;
                case "plume": rock.AddPart(new FoundingPlumePart()); break;
                case "restable": rock.AddPart(new ExtraRockPart()); break;
                case "destructible": rock.AddPart(new DestructiblePart()); break;
                case "foreignParent": r.ParentEntity = new Entity(); break;
                case "missingExam": rock.RemovePart(rock.GetPart<ExaminablePart>()); break;
            }
            string state = zone.TileState.ToSaveString(); Assert.False(Body());
            Assert.False(art.TryGetSubjectSprite(zone, rock, X, Y, out _));
            Assert.AreEqual(state, zone.TileState.ToSaveString());
        }
        [TestCase("name")][TestCase("type")][TestCase("profile")][TestCase("biome")]
        [TestCase("cache")][TestCase("map")][TestCase("shortMap")][TestCase("poi")]
        [TestCase("alias")][TestCase("depth")][TestCase("lair")][TestCase("world")]
        public void WarmAuthorityChangesRevokeTheRockBody(string fault)
        {
            Native(); var map = manager.WorldMap; var p = map.GetPOI(3,3);
            switch (fault)
            {
                case "name": p.Name = "other"; break;
                case "type": p.Type = POIType.MerchantCamp; break;
                case "profile": p.Profile = "other"; break;
                case "biome": map.Tiles[3,3] = BiomeType.Beating; break;
                case "cache": manager.CachedZones[zone.ZoneID] = new Zone(zone.ZoneID); break;
                case "map": map.POIs = null; break;
                case "shortMap": map.POIs = new PointOfInterest[1,1]; break;
                case "poi": map.SetPOI(3,3,null); break;
                case "alias": zone.ZoneID = "Overworld.03.3.0"; manager.SetActiveZone(zone); break;
                case "depth": zone.ZoneID = "Overworld.3.3.1"; manager.SetActiveZone(zone); break;
                case "lair": map.SetPOI(3,3,new PointOfInterest(POIType.Lair,"retained lair",null,1,"DesertProwler")); LairStacks.Restore(manager,null); Assert.NotNull(LairStacks.Inspect(manager,zone.ZoneID)); map.SetPOI(3,3,p); break;
                case "world": manager = OverworldZoneManager.CreateDetached(content.Factory,65,true); manager.SetActiveZone(zone); manager.WorldMap.GetPOI(3,3).Profile = "other"; break;
            }
            Assert.False(Body()); Assert.False(art.TryGetSubjectSprite(zone,rock,X,Y,out _));
        }
        [TestCase("Sand")][TestCase("SandstoneFloor")][TestCase("SandstoneWall")][TestCase("StoneWall")]
        [TestCase("BrinePool")][TestCase("TarSeep")][TestCase("DryBrush")][TestCase("Tree")]
        [TestCase("TheRoot")][TestCase("TheRooted")][TestCase("FoundingPlume")][TestCase("TepuiStone")]
        public void RootRockInstanceNeverAdmitsOtherFamilies(string name)
        {
            zone.RemoveEntity(rock); zone.RemoveEntity(floor); var e = Add(name);
            Assert.False(art.TryResolveForeground(zone,X,Y,e,out _));
            Assert.False(art.TryGetSubjectSprite(zone,e,X,Y,out _));
            Assert.False(art.TryResolveGround(zone,X,Y,out _,out _));
            Assert.False(art.TryResolveRemembered(zone,X,Y,e,out _,out _));
        }
        [TestCase(false)][TestCase(true)] public void MissingOrMalformedAtlasKeepsFallback(bool malformed)
        {
            var texture = malformed ? new Texture2D(1,1) : null;
            try { using(var missing = new FlatPixelBeatingMaterialsCatalog(texture,null,null,selector))
            { Admit(missing); Assert.False(missing.TryResolveForeground(zone,X,Y,rock,out _)); Assert.False(missing.TryGetSubjectSprite(zone,rock,X,Y,out _)); } }
            finally { if(texture != null) Object.DestroyImmediate(texture); }
        }
        [Test] public void RemovingRockWithdrawsBodyWithoutInventingOrRemovingGround()
        { Native(); Assert.True(zone.RemoveEntity(rock)); Assert.False(Body()); Assert.True(world.TryResolvePresentation(zone,X,Y,out var v)); Assert.AreSame(floor,v.Owner); Assert.True(v.LocalGround); }
        [Test] public void DisposalLeavesBorrowedAtlasAndSelectorAlive()
        { Native(); var texture = Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath); art.Dispose(); Assert.True(texture!=null); Assert.True(selector.TryResolve(zone,X,Y,out _)); Assert.False(Body()); }
    }
}
