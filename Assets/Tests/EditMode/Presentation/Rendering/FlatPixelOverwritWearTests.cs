using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed partial class FlatPixelOverwritMaterialsTests
    {
        const string WearAtlas = "Sprites/Live16x24/overwrit-wear";
        const int WX=4, WY=2;
        Entity WearScene(string id="Overworld.4.12.0")
        { zone=Attach(id); Assert.True(Supported(zone)); return Add(Floor,WX,WY); }
        Tile WearTile()
        { Assert.True(Ground(out var tile,out _,WX,WY)); return tile; }
        [TestCase("Overworld.4.12.0")] [TestCase("Overworld.0.10.0")]
        public void WearPilotUsesCoherentNativeGroundAndCanonicalReader(string id)
        {
            var owner=WearScene(id); int version=zone.EntityVersion; var tile=WearTile();
            Assert.AreSame(Resources.Load<Texture2D>(WearAtlas),tile.sprite.texture);
            Assert.AreEqual(new Vector2(16,24),tile.sprite.rect.size);
            Assert.AreEqual(Tile.ColliderType.None,tile.colliderType);
            Assert.False(View(WX,WY).BlocksMovement);
            Add(Floor,WX+1,WY); var next=View(WX+1,WY).GroundTile;
            Assert.AreSame(tile.sprite.texture,next.sprite.texture);
            Assert.AreEqual(tile.sprite.rect.x+16,next.sprite.rect.x,"Adjacent native fragments remain one coherent motif.");
            Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,WX,WY,out var portrait,out _));
            Assert.AreSame(Resources.Load<Texture2D>(Atlas),portrait.texture);
            Assert.AreEqual(0,portrait.rect.x,"Reader stays the quiet canonical surface.");
            Assert.AreEqual(version+1,zone.EntityVersion); Assert.False(owner.GetPart<PhysicsPart>().Solid);
        }
        [TestCase("wet")] [TestCase("heat")] [TestCase("hidden")] [TestCase("remembered")]
        [TestCase("unexplored")] [TestCase("custom")] [TestCase("duplicate")] [TestCase("removed")]
        public void WearPilotRetainsExistingOwnerAndDisclosureGuards(string fault)
        {
            var owner=WearScene(); Assert.AreSame(Resources.Load<Texture2D>(WearAtlas),WearTile().sprite.texture);
            switch(fault)
            {
                case "wet":zone.TileState.WriteCoating(WX,WY,"water",4);break;
                case "heat":zone.TileState.AddHeat(WX,WY,1);break;
                case "hidden":owner.GetPart<RenderPart>().Visible=false;break;
                case "remembered":zone.GetCell(WX,WY).IsVisible=false;break;
                case "unexplored":zone.GetCell(WX,WY).Explored=false;break;
                case "custom":owner.GetPart<RenderPart>().VisualVariant="unmapped";break;
                case "duplicate":Add(Floor,WX,WY);break;
                case "removed":zone.RemoveEntity(owner);break;
            }
            Assert.False(Ground(out _,out _,WX,WY));
        }
        [Test] public void WearPatternDoesNotReadHiddenNeighborsOrExpandScope()
        {
            WearScene(); var tile=WearTile();
            var neighbor=Add(Marker,WX,WY+1);neighbor.GetPart<RenderPart>().Visible=false;
            Assert.AreSame(tile,WearTile());
            zone=Attach(Home);Add(Floor,WX,WY);NativeTile(WearTile());
            manager.SetActiveZone(new Zone(Home)); Assert.False(Supported(zone));
        }
        [Test] public void WearMissingOrMalformedOptionalResourceKeepsApprovedGroundAndOwnership()
        {
            WearScene();var tile=WearTile();var sprite=tile.sprite;
            Inject(Resources.Load<Texture2D>(Atlas));NativeTile(WearTile());
            Assert.True(tile==null&&sprite==null);Assert.NotNull(Resources.Load<Texture2D>(WearAtlas));
            var bad=new Texture2D(511,24);var selector=new FlatPixelFallbackCatalog();
            try
            {
                using(var adapter=(IDisposable)Activator.CreateInstance(AdapterType(),new object[]{Resources.Load<Texture2D>(Atlas),selector,bad}))
                {
                    object[] args={zone,WX,WY,null,null};Assert.True((bool)AdapterType().GetMethod("TryResolveGround").Invoke(adapter,args));NativeTile((Tile)args[3]);
                }
                Assert.True(bad!=null);
            }
            finally{selector.Dispose();Object.DestroyImmediate(bad);}
        }
        [Test] public void WearGroundIncludesQuietSpaceAndDeterministicFragments()
        {
            WearScene();int quiet=0,wear=0;var first=WearTile();
            var motifs=new System.Collections.Generic.HashSet<int>();
            for(int y=0;y<21;y++)for(int x=0;x<52;x++)
            {
                if(x!=WX||y!=WY)Add(Floor,x,y);
                Assert.True(Ground(out var tile,out _,x,y));
                if(tile.sprite.texture==Resources.Load<Texture2D>(WearAtlas)){wear++;motifs.Add((int)tile.sprite.rect.x/(16*32));}else{quiet++;NativeTile(tile);Assert.AreEqual(0,tile.sprite.rect.x);}
            }
            Assert.That(quiet,Is.GreaterThan(wear));Assert.That(wear,Is.GreaterThan(60));Assert.That(motifs.Count,Is.GreaterThanOrEqualTo(2));Assert.AreSame(first,WearTile());
        }
    }
}
