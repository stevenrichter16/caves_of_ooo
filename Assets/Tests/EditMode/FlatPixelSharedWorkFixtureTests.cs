using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    /// <summary>Ordinary work fixtures use neutral approved bodies without granting services.
    /// Public-path RED pins world/reader identity; semantic countercases protect altered owners.</summary>
    public sealed class FlatPixelSharedWorkFixtureTests
    {
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        const int X = 20, Y = 12;
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = new Zone("Overworld.12.12.0"); manager.SetActiveZone(zone);
            foreach (var c in zone.Cells) c.IsVisible = c.Explored = true;
            world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup() { world?.Dispose(); content?.Dispose(); }
        Entity Add(string name, int x = X, int y = Y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e, name); Assert.True(zone.AddEntity(e,x,y)); return e; }
        static Texture2D Atlas => Resources.Load<Texture2D>(FlatPixelEastKitchenCatalog.ResourcePath);
        static Rect RectFor(string name) => new Rect(name == "Oven" ? 16 : 272,0,16,24);
        static void Native(Tile tile, string name)
        { Assert.NotNull(tile); Assert.AreSame(Atlas,tile.sprite.texture); Assert.AreEqual(RectFor(name),tile.sprite.rect); Assert.AreEqual(Tile.ColliderType.None,tile.colliderType); }
        [TestCase("Oven")] [TestCase("MillStone")]
        public void CurrentWorldAndReaderShareNeutralBodyOnRealGround(string name)
        {
            var floor=Add("Floor"); var owner=Add(name); var parts=owner.Parts.Count;
            Assert.True(world.TryResolvePresentation(zone,X,Y,out var view));
            Assert.AreSame(owner,view.Owner); Assert.True(view.NativeForeground);
            Native(view.ForegroundTile,name); Assert.True(view.LocalGround); Assert.NotNull(view.GroundTile);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var portrait,out _));
            Assert.AreSame(view.ForegroundTile.sprite,portrait);
            Assert.True(owner.GetPart<PhysicsPart>().Solid); Assert.AreEqual(parts,owner.Parts.Count);
            Assert.False(owner.HasPart<CampfirePart>()); Assert.False(owner.HasPart<OvenSitePart>());
            zone.GetCell(X,Y).IsVisible=false;
            Assert.True(world.TryResolvePresentation(zone,X,Y,out var memory)); Assert.False(memory.NativeForeground);
            zone.GetCell(X,Y).IsVisible=true;
            Assert.True(world.TryResolvePresentation(zone,X,Y,out var returned)); Native(returned.ForegroundTile,name);
        }
        static ExaminePortraitRenderer Portraits() => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new object[] { (Action<Camera, RenderTexture>)((camera,target)=>{}) }, null);
        [TestCase("Oven")] [TestCase("MillStone")]
        public void SelectedAndCurrentPlacementRemainsAuthority(string name)
        {
            var owner=Add(name); var document=ExamineDocumentBuilder.BuildWorld(owner,zone,zone.GetCell(X,Y),null);
            using(var paint=new FlatPixelSharedFurnitureTests.Paint(world,zone)) using(var portraits=Portraits())
            {
                paint.Step(); Native(paint.At(X,Y),name);
                Assert.AreSame(Atlas,portraits.GetPortrait(document,paint.Renderer).Texture);
                zone.GetCell(X,Y).IsVisible=false; paint.Dirty(X,Y); paint.Step();
                Assert.IsNull(portraits.GetPortrait(document,paint.Renderer).Texture);
                zone.GetCell(X,Y).IsVisible=true;
                var cover=Add("Player"); paint.Dirty(X,Y); paint.Step();
                Assert.True(world.TryResolvePresentation(zone,X,Y,out var covered)); Assert.AreSame(cover,covered.Owner);
                Assert.True(zone.RemoveEntity(cover)); paint.Dirty(X,Y); paint.Step(); Native(paint.At(X,Y),name);
                Assert.True(zone.MoveEntity(owner,X+1,Y)); Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document));
                document=ExamineDocumentBuilder.BuildWorld(owner,zone,zone.GetCell(X+1,Y),null);
                Assert.True(zone.RemoveEntity(owner)); paint.Dirty(X+1,Y); paint.Step();
                Assert.IsNull(portraits.GetPortrait(document,paint.Renderer).Texture);
            }
        }
        [TestCase("Oven")] [TestCase("MillStone")]
        public void ResourceFailureAndDisposalLeaveBorrowedArtIntact(string name)
        {
            var e=Add(name);
            using(var missing=new FlatPixelSharedFurnitureCatalog(null)) Assert.False(missing.TryResolve(e,out _));
            var malformed=new Texture2D(16,24);
            try { using(var wrong=new FlatPixelSharedFurnitureCatalog(malformed)) Assert.False(wrong.TryResolve(e,out _)); }
            finally { UnityEngine.Object.DestroyImmediate(malformed); }
            var art=new FlatPixelSharedFurnitureCatalog(Atlas);
            Assert.True(art.TryResolve(e,out var body)); Native(body,name);
            Assert.True(art.TryResolve(e,out var repeated)); Assert.AreSame(body,repeated);
            art.Dispose(); Assert.False(art.TryResolve(e,out _)); Assert.NotNull(Atlas); art.Dispose();
        }
        [TestCase("Oven","custody")] [TestCase("MillStone","custody")]
        [TestCase("Oven","hidden")] [TestCase("MillStone","hidden")]
        [TestCase("Oven","glyph")] [TestCase("MillStone","glyph")]
        [TestCase("Oven","visual")] [TestCase("MillStone","visual")]
        [TestCase("Oven","layer")] [TestCase("MillStone","layer")]
        [TestCase("Oven","nonSolid")] [TestCase("MillStone","nonSolid")]
        [TestCase("Oven","service")] [TestCase("MillStone","service")]
        [TestCase("Oven","foreignPart")] [TestCase("MillStone","foreignPart")]
        public void AlteredIdentityCannotBorrowNeutralBody(string name,string change)
        {
            var e=Add(name);
            using(var art=new FlatPixelSharedFurnitureCatalog(Atlas))
            {
                Assert.True(art.TryResolve(e,out _),"positive precondition");
                var r=e.GetPart<RenderPart>(); var p=e.GetPart<PhysicsPart>();
                switch(change)
                {
                    case "custody": p.InInventory=Add("Player",X-1,Y); break;
                    case "hidden": r.Visible=false; break;
                    case "glyph": r.RenderString="!"; break;
                    case "visual": r.VisualID="foreign-workshop"; break;
                    case "layer": r.RenderLayer=4; break;
                    case "nonSolid": p.Solid=false; break;
                    case "service": e.AddPart(new CampfirePart()); break;
                    case "foreignPart": e.GetPart<ExaminablePart>().ParentEntity=Add("Chair",X-1,Y); break;
                }
                Assert.False(art.TryResolve(e,out _));
            }
        }

        [Test] public void MissingSharedArtCannotBorrowOldEastButAuthoredCookingOvenSurvives()
        {
            world.Dispose(); world=new FlatPixelGreyboxCatalog((Texture2D)null);
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(FlatPixelGreyboxCatalog).GetField("_eastKitchenArt",flags).SetValue(world,new FlatPixelEastKitchenCatalog(Atlas,null));
            typeof(FlatPixelGreyboxCatalog).GetField("_sharedFurnitureArt",flags).SetValue(world,new FlatPixelSharedFurnitureCatalog(null));
            zone=new Zone("Overworld.12.11.0");manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.IsVisible=c.Explored=true;
            var mill=Add("MillStone"); var oven=Add("Oven",X+1,Y); oven.AddPart(new CampfirePart());
            Assert.True(world.TryResolvePresentation(zone,X,Y,out var missing)); Assert.False(missing.NativeForeground);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,mill,X,Y,out var missingPortrait,out _)); Assert.AreNotSame(Atlas,missingPortrait.texture);
            Assert.True(world.TryResolvePresentation(zone,X+1,Y,out var cooking)); Native(cooking.ForegroundTile,"Oven");
            Assert.True(world.TryGetPresentationSubjectSprite(zone,oven,X+1,Y,out var ovenPortrait,out _)); Assert.AreSame(Atlas,ovenPortrait.texture);
        }
        [TestCase("throw")] [TestCase("weight")] [TestCase("bulk")] [TestCase("foreign")]
        public void MillHandlingChangesWithdrawBody(string change)
        {
            var mill=Add("MillStone"); using(var art=new FlatPixelSharedFurnitureCatalog(Atlas))
            {
                Assert.True(art.TryResolve(mill,out _)); var h=mill.GetPart<HandlingPart>();
                if(change=="throw")h.Throwable=true; else if(change=="weight")h.Weight=1;
                else if(change=="bulk")h.BulkClass="Light"; else h.ParentEntity=Add("Chair",X-1,Y);
                Assert.False(art.TryResolve(mill,out _));
            }
        }
        [TestCase("Oven")] [TestCase("MillStone")]
        public void MulticellDoesNotStretchSingleBody(string name)
        {
            var owner=Add(name);using(var art=new FlatPixelSharedFurnitureCatalog(Atlas))
            {
                Assert.True(art.TryResolve(owner,out _));owner.AddPart(new SpatialFootprintPart{CellsRaw="0,0"});
                Assert.True(zone.TryChangeFootprint(owner,"0,0;1,0"));Assert.False(art.TryResolve(owner,out _));
                Assert.True(zone.TryChangeFootprint(owner,"0,0"));Assert.True(art.TryResolve(owner,out _));
            }
        }
        [Test] public void RepairOvenAndPortableMillRemainSeparate()
        {
            var oven=Add("Oven"); var mill=Add("MillStone",X+1,Y);
            using(var art=new FlatPixelSharedFurnitureCatalog(Atlas))
            {
                Assert.True(art.TryResolve(oven,out _)); Assert.True(art.TryResolve(mill,out _));
                oven.AddPart(new OvenSitePart()); mill.GetPart<HandlingPart>().Carryable=true;
                Assert.False(art.TryResolve(oven,out _)); Assert.False(art.TryResolve(mill,out _));
            }
        }
    }
}
