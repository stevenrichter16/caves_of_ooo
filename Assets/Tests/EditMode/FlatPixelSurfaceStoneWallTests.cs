using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelSurfaceStoneWallTests
    {
        const int X=20,Y=12;
        static readonly string[] Zones={"Overworld.10.11.0","Overworld.1.2.0"};
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone; FlatPixelGreyboxCatalog world;
        static Texture2D Art(string path)=>Resources.Load<Texture2D>(path);
        static Texture2D Cellar=>Art(FlatPixelEastFixtureCatalog.CellarResourcePath);
        [SetUp] public void Setup(){content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,64,true);world=new FlatPixelGreyboxCatalog();}
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        Entity Wall(string id){zone=new Zone(id);manager.SetActiveZone(zone);foreach(var c in zone.Cells)c.IsVisible=c.Explored=true;var e=content.Factory.CreateEntity("Wall");Assert.True(zone.AddEntity(e,X,Y));return e;}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        void Native(Entity e){var v=View();Assert.AreSame(e,v.Owner);Assert.True(v.NativeForeground,"canonical Wall remains ASCII");Assert.AreSame(Cellar,v.ForegroundTile.sprite.texture);Assert.AreEqual(new Rect(128,0,16,24),v.ForegroundTile.sprite.rect);Assert.AreEqual(Tile.ColliderType.None,v.ForegroundTile.colliderType);}
        void Fallback(){var v=View();Assert.IsFalse(v.NativeForeground);if(v.ForegroundTile!=null)Assert.AreNotSame(Cellar,v.ForegroundTile.sprite.texture);}
        [TestCase("Overworld.10.11.0")][TestCase("Overworld.1.2.0")]
        public void CanonicalWorldReaderAndDamageReuseExactApprovedFrame(string id)
        {
            var e=Wall(id);int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();Native(e);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var sprite,out _));Assert.AreSame(View().ForegroundTile.sprite,sprite);
            e.GetPart<DestructiblePart>().HP=39;Native(e);Assert.True(e.GetPart<PhysicsPart>().Solid);
            Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
            e.GetPart<DestructiblePart>().HP=0;Fallback();e.GetPart<DestructiblePart>().HP=40;Native(e);
            zone.GetCell(X,Y).IsVisible=false;Fallback();zone.GetCell(X,Y).Explored=false;Assert.False(world.TryResolvePresentation(zone,X,Y,out _));
        }
        [TestCase("detail")][TestCase("tile")][TestCase("visual")][TestCase("glyph")][TestCase("color")][TestCase("background")][TestCase("layer")]
        [TestCase("foreign")][TestCase("duplicate")][TestCase("custody")][TestCase("nonSolid")][TestCase("footprint")]
        [TestCase("material")][TestCase("wreckage")][TestCase("maxHP")][TestCase("hot")][TestCase("frozen")][TestCase("nan")][TestCase("service")]
        public void AlteredWallCannotBorrowOrdinaryMasonry(string fault)
        {
            foreach(var id in Zones)
            {
                var e=Wall(id);Native(e);var r=e.GetPart<RenderPart>();var t=e.GetPart<ThermalPart>();var d=e.GetPart<DestructiblePart>();
                switch(fault){case "detail":r.DetailColor="&R";break;case "tile":r.TileColor="&R";break;case "visual":r.VisualID="reference-glade-dark-ruin";break;case "glyph":r.RenderString="!";break;case "color":r.ColorString="&R";break;
                case "background":r.BackgroundColor="^R";break;case "layer":r.RenderLayer=4;break;case "foreign":r.ParentEntity=new Entity();break;
                case "duplicate":e.Parts.Add(new RenderPart{ParentEntity=e});break;case "custody":e.GetPart<PhysicsPart>().InInventory=new Entity();break;
                case "nonSolid":e.GetPart<PhysicsPart>().Solid=false;break;case "footprint":e.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});break;
                case "material":e.GetPart<MaterialPart>().MaterialID="Copper";break;case "wreckage":d.WreckageBlueprint="AshPile";break;
                case "maxHP":d.MaxHP=100;break;case "hot":t.Temperature=t.FlameTemperature;break;case "frozen":t.Temperature=t.FreezeTemperature;break;
                case "nan":t.Temperature=float.NaN;break;case "service":e.AddPart(new DoorPart());break;}
                // Foreign/custodied objects can be withdrawn entirely by the authoritative selector.
                if(world.TryResolvePresentation(zone,X,Y,out var v))Assert.AreNotSame(Cellar,v.ForegroundTile?.sprite?.texture,fault+id);
            }
        }
        [TestCase("biome")][TestCase("poi")][TestCase("replacement")][TestCase("map-shape")]
        public void CurrentWildernessAuthorityIsRequired(string fault)
        {
            foreach(var id in Zones)
            {
                var e=Wall(id);Native(e);var p=WorldMap.FromZoneID(id);var map=manager.WorldMap;var tiles=map.Tiles;var old=tiles[p.x,p.y];
                try {if(fault=="biome")tiles[p.x,p.y]=BiomeType.Sodden;if(fault=="poi")map.SetPOI(p.x,p.y,new PointOfInterest(POIType.MerchantCamp,"changed site"));
                if(fault=="replacement")manager.CachedZones[id]=new Zone(id);if(fault=="map-shape")map.Tiles=new BiomeType[1,1];
                if(world.TryResolvePresentation(zone,X,Y,out var v))Assert.AreNotSame(Cellar,v.ForegroundTile?.sprite?.texture);}
                finally{map.Tiles=tiles;tiles[p.x,p.y]=old;map.SetPOI(p.x,p.y,null);manager.CachedZones[id]=zone;}
                Native(e);
            }
        }
        [TestCase("Overworld.2.4.0")][TestCase("Overworld.1.2.1")][TestCase("Overworld.3.5.0")]
        public void SpecialSiteDepthAndFellingDoNotInheritWildernessWall(string id)
        {Wall(Zones[1]);Native(zone.GetCell(X,Y).Objects[0]);Wall(id);if(world.TryResolvePresentation(zone,X,Y,out var v))Assert.AreNotSame(Cellar,v.ForegroundTile?.sprite?.texture);}
        [TestCase("Overworld.10.11.0")][TestCase("Overworld.1.2.0")]
        public void SavedLairColumnStillRefusesAfterLabelRemoval(string id)
        {
            var p=WorldMap.FromZoneID(id);manager.WorldMap.SetPOI(p.x,p.y,new PointOfInterest(POIType.Lair,"saved",null,1,"DesertProwler"));
            LairStacks.Restore(manager,null);manager.GetZone(id);Assert.NotNull(LairStacks.Inspect(manager,id));manager.WorldMap.SetPOI(p.x,p.y,null);Wall(id);
            if(world.TryResolvePresentation(zone,X,Y,out var v))Assert.AreNotSame(Cellar,v.ForegroundTile?.sprite?.texture);
        }
        [TestCase(false)][TestCase(true)]
        public void ExplicitMissingWallResourceKeepsOtherMaterials(bool stump)
        {
            var owner=Wall(Zones[stump?1:0]);Native(owner);world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);
            using(var selector=new FlatPixelFallbackCatalog())
            {
                IDisposable adapter=stump?(IDisposable)new FlatPixelStumpMaterialsCatalog(Art(FlatPixelStumpMaterialsCatalog.ResourcePath),null,null,selector)
                    :new FlatPixelSpreadEnvironmentCatalog(Art("Sprites/Live16x24/sill-environment"),Art("Sprites/Live16x24/opening-ground"),null,null,null,selector);
                typeof(FlatPixelGreyboxCatalog).GetField(stump?"_stumpMaterialsArt":"_spreadEnvironmentArt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,adapter);
                Fallback();Assert.True(zone.RemoveEntity(owner));var floor=content.Factory.CreateEntity(stump?"TepuiStone":"Grass");zone.AddEntity(floor,X,Y);
                Assert.True(View().LocalGround,"missing wall group must not withdraw existing ground");
            }
        }
    }
}
