using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelRimCanopyTests
    {
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        Entity tree;
        const string Atlas = "Sprites/Live16x24/rim-canopy";
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope(); manager = OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone = new Zone("Overworld.2.3.0"); manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            world = new FlatPixelGreyboxCatalog(); Add("TepuiStone",10); tree=Add("Tree",10);
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        Entity Add(string name,int x){var e=content.Factory.CreateEntity(name);Assert.True(zone.AddEntity(e,x,10));return e;}
        Sprite Body(int x=10){return world.TryResolvePresentation(zone,x,10,out var v) ? v.ForegroundTile?.sprite : null;}
        [Test] public void RegionalCrownUsesNativeCellAndSameCachedPortraitWithoutMutation()
        {
            var atlas=Resources.Load<Texture2D>(Atlas);Assert.NotNull(atlas);int version=zone.EntityVersion;
            var sprite=Body();Assert.AreSame(atlas,sprite.texture);Assert.AreEqual(new Vector2(16,24),sprite.rect.size);
            Assert.AreEqual(new Vector2(8,12),sprite.pivot);Assert.AreEqual(16,sprite.pixelsPerUnit);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,tree,10,10,out var portrait,out var tint));
            Assert.AreSame(sprite,portrait);Assert.AreSame(sprite,Body());Assert.AreEqual(Color.white,tint);
            Assert.AreEqual(version,zone.EntityVersion);Assert.True(zone.GetCell(10,10).BlocksMovement());
            var variants=new HashSet<float>{sprite.rect.x};
            for(int x=11;x<20;x++){Add("TepuiStone",x);Add("Tree",x);variants.Add(Body(x).rect.x);}
            Assert.AreEqual(3,variants.Count,"Regional trees should use all three cached native crown variants");
        }
        [TestCase("memory")] [TestCase("source-hidden")] [TestCase("reskinned")] [TestCase("dead")]
        [TestCase("poi")] [TestCase("graph")] [TestCase("tint")]
        public void ChangedMeaningOrAuthorityNeverKeepsRegionalCanopy(string change)
        {
            Assert.AreSame(Resources.Load<Texture2D>(Atlas),Body().texture);
            switch(change)
            {
                case "memory":zone.GetCell(10,10).IsVisible=false;break;
                case "source-hidden":tree.GetPart<RenderPart>().Visible=false;break;
                case "reskinned":tree.GetPart<RenderPart>().VisualVariant="foreign";break;
                case "dead":tree.GetPart<DestructiblePart>().HP=0;break;
                case "poi":manager.WorldMap.SetPOI(2,3,new PointOfInterest(POIType.MerchantCamp,"camp"));break;
                case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "tint":tree.GetPart<RenderPart>().ColorString="&R";break;
            }
            Assert.AreNotSame(Resources.Load<Texture2D>(Atlas),Body()?.texture);
        }
        [Test] public void MissingOptionalCanopyFallsBackToApprovedOriginalAndRestores()
        {
            Assert.AreSame(Resources.Load<Texture2D>(Atlas),Body().texture);
            var catalog=typeof(FlatPixelGreyboxCatalog).GetField("_stumpMaterialsArt",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
            var field=catalog.GetType().GetField("rimCanopy",BindingFlags.Instance|BindingFlags.NonPublic);Assert.NotNull(field);
            var tiles=field.GetValue(catalog);
            try{field.SetValue(catalog,null);Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.VegetationResourcePath),Body().texture);}
            finally{field.SetValue(catalog,tiles);}
            Assert.AreSame(Resources.Load<Texture2D>(Atlas),Body().texture);
        }
        [Test] public void NeighborStumpPreservesApprovedOriginal()
        {
            Assert.AreSame(Resources.Load<Texture2D>(Atlas),Body().texture);
            zone=new Zone("Overworld.2.5.0");manager.SetActiveZone(zone);foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            Add("TepuiStone",10);tree=Add("Tree",10);
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.VegetationResourcePath),Body().texture);
        }
    }
}
