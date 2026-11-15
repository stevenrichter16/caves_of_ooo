using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelRootChamberRockTests
    {
        const int X=20,Y=10;
        DensityLootTestScope content;OverworldZoneManager manager;Zone zone;
        FlatPixelFallbackCatalog selector;FlatPixelShallowCavesCatalog art;FlatPixelGreyboxCatalog world;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,704860946,true);
            zone=new Zone("Overworld.3.3.1");manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            selector=new FlatPixelFallbackCatalog();art=new FlatPixelShallowCavesCatalog(selector);Admit(art);world=new FlatPixelGreyboxCatalog();
        }
        static void Admit(FlatPixelShallowCavesCatalog a)
        { typeof(FlatPixelShallowCavesCatalog).GetMethod("AdmittingRootChamber",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(a,null); }
        [TearDown] public void Cleanup(){world?.Dispose();art?.Dispose();selector?.Dispose();content?.Dispose();}
        Entity Add(string name) {var e=content.Factory.CreateEntity(name);Assert.NotNull(e);Assert.True(zone.AddEntity(e,X,Y));return e;}
        bool Back()=>art.TryResolveCellBackdrop(zone,X,Y,out _,out _);
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,X,Y,out var v));return v;}
        [TestCase("Rubble",false)] [TestCase("Stalagmite",true)]
        public void RealFloorlessRockKeepsBodyReaderGeometryAndNoInventedFloor(string name,bool solid)
        {
            var e=Add(name);int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();var view=View();
            Assert.True(view.NativeForeground);Assert.True(view.AmbientBackdrop);Assert.False(view.LocalGround);
            Assert.AreSame(e,view.Owner);Assert.AreEqual(solid,view.BlocksMovement);Assert.AreEqual(FlatPixelGreyboxGround.Ground,view.GroundKind);
            Assert.AreSame(Resources.Load<Texture2D>("Sprites/Environment/"+name.ToLowerInvariant()),view.ForegroundTile.sprite.texture);
            Assert.AreEqual(Tile.ColliderType.None,view.GroundTile.colliderType);
            Assert.AreEqual((Color)new Color32(128,106,76,255),view.GroundColor);
            Assert.False(art.TryResolveGround(zone,X,Y,out _,out _));
            Assert.True(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var s,out _));Assert.AreSame(view.ForegroundTile.sprite,s);
            Assert.AreEqual(1,zone.GetCell(X,Y).Objects.Count);Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [Test] public void ActorCoverRetainsRubbleBackingReaderAndRemovalWithdrawsIt()
        {
            var e=Add("Rubble");Assert.True(Back());var player=Add("Player");var view=View();Assert.AreSame(player,view.Owner);Assert.True(view.AmbientBackdrop);Assert.False(view.LocalGround);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,e,X,Y,out var s,out _));Assert.AreSame(Resources.Load<Texture2D>("Sprites/Environment/rubble"),s.texture);
            Assert.True(zone.RemoveEntity(e));Assert.False(Back());Assert.False(View().AmbientBackdrop);
        }
        [TestCase("empty")][TestCase("Player")][TestCase("DescentLedge")][TestCase("HearthPatch")][TestCase("NicheHome")][TestCase("PlaqueWall")][TestCase("TheRoot")][TestCase("TheRooted")]
        public void OtherCellsNeverAcquireRockBacking(string name)
        {if(name!="empty")Add(name);Assert.False(Back());Assert.False(View().AmbientBackdrop);}
        [TestCase("hidden")][TestCase("memory")][TestCase("moved")][TestCase("custody")][TestCase("changed")][TestCase("duplicate")]
        public void InvalidRockCannotAuthorizeBacking(string fault)
        {
            var e=Add("Rubble");Assert.True(Back());
            if(fault=="hidden")e.GetPart<RenderPart>().Visible=false;
            if(fault=="memory")zone.GetCell(X,Y).IsVisible=false;
            if(fault=="moved")zone.RemoveEntity(e);
            if(fault=="custody")e.GetPart<PhysicsPart>().InInventory=new Entity();
            if(fault=="changed")e.GetPart<RenderPart>().ColorString="&R";
            if(fault=="duplicate")Add("Rubble");
            Assert.False(Back());
        }
        [TestCase("Rubble")][TestCase("Stalagmite")]
        public void MalformedVisibleRockCannotHideBesideCanonicalOne(string name)
        {Add("Rubble");Assert.True(Back());var e=Add(name);e.GetPart<RenderPart>().ColorString="&R";Assert.False(Back());e.GetPart<RenderPart>().Visible=false;Assert.True(Back());}
        [TestCase("water")][TestCase("charge")][TestCase("background")][TestCase("hearth")]
        public void HazardsAndAuthoredMeaningKeepPriority(string fault)
        {
            var e=Add("Rubble");Assert.True(Back());
            if(fault=="water")zone.TileState.WriteCoating(X,Y,"water",3);
            if(fault=="charge")zone.TileState.AddCharge(X,Y,1);
            if(fault=="background")e.GetPart<RenderPart>().BackgroundColor="^r";
            if(fault=="hearth")Add("HearthPatch");
            string state=zone.TileState.ToSaveString();Assert.False(Back());Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [Test] public void RealFloorWinsEvenWhenItsTextureIsMissing()
        {
            Add("Rubble");Assert.True(Back());Add("SandstoneFloor");Assert.False(Back());Assert.True(View().LocalGround);
            using(var missing=new FlatPixelShallowCavesCatalog(null,null,Resources.Load<Texture2D>("Sprites/Environment/rubble"),null,null,null,selector))
            {Admit(missing);Assert.False(missing.TryResolveGround(zone,X,Y,out _,out _));Assert.False(missing.TryResolveCellBackdrop(zone,X,Y,out _,out _));}
        }
        [Test] public void HiddenForeignGroundDoesNotLeakIntoBackdropDecision()
        {Add("Rubble");Assert.True(Back());var e=Add("Grass");e.GetPart<RenderPart>().Visible=false;Assert.True(Back());e.GetPart<RenderPart>().Visible=true;Assert.False(Back());}
        [TestCase(false)][TestCase(true)] public void MissingOrMalformedRockArtCannotAuthorizeBackdrop(bool malformed)
        {
            var e=Add("Rubble");var bad=malformed?new Texture2D(1,1):null;
            try {using(var missing=new FlatPixelShallowCavesCatalog(null,null,bad,Resources.Load<Texture2D>("Sprites/Environment/stalagmite"),null,null,selector))
              {Admit(missing);Assert.False(missing.TryResolveForeground(zone,X,Y,e,out _));Assert.False(missing.TryResolveCellBackdrop(zone,X,Y,out _,out _));}}
            finally {if(bad!=null)Object.DestroyImmediate(bad);}
        }
        [Test] public void WarmAuthorityRevokesAndRestoresWithoutCachedPermission()
        {Add("Rubble");Assert.True(Back());var p=manager.WorldMap.GetPOI(3,3);p.Name="other";Assert.False(Back());p.Name=RootSiteBuilder.SiteName;Assert.True(Back());manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);Assert.False(Back());}
        [TestCase(false)][TestCase(true)] public void EndingFaceNeverBorrowsRockBacking(bool colocated)
        {if(colocated)Add("Rubble");Add("TheRoot");Assert.False(Back());Assert.False(View().AmbientBackdrop);}
        [Test] public void DisposalPreservesBorrowedBackdropTextureAndSelector()
        {Add("Rubble");Assert.True(art.TryResolveCellBackdrop(zone,X,Y,out var tile,out _));var texture=Resources.Load<Texture2D>("Sprites/Environment/rubble");art.Dispose();Assert.True(tile!=null);Assert.True(texture!=null);Assert.True(selector.TryResolve(zone,X,Y,out _));Assert.False(Back());}
    }
}
