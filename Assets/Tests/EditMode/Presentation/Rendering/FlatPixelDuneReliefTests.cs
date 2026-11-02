using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class FlatPixelBeatingMaterialsTests
    {
        const string DunePilot = "Overworld.19.14.0";
        const string DuneRelief = "Sprites/Live16x24/dune-relief";
        Entity DuneScene()
        {
            zone = Attach(DunePilot);
            var crest = Add("DuneCrest");
            Add("Sand", X, Y + 1);
            return crest;
        }
        void Relief(bool expected)
        {
            var view = View(); Assert.True(view.NativeForeground); Assert.True(view.BlocksMovement);
            NativeTile(view.ForegroundTile, expected ? DuneRelief : Atlas);
        }
        [Test] public void DuneReliefExposedSouthKeepsIdentityAndCollision()
        {
            var crest = DuneScene(); var version = zone.EntityVersion; var state = zone.TileState.ToSaveString();
            Relief(true);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, crest, X, Y, out var sprite, out _));
            Assert.AreSame(Resources.Load<Texture2D>(Atlas), sprite.texture, "Reader remains the canonical quiet crest.");
            Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(state, zone.TileState.ToSaveString());
            Assert.True(crest.GetPart<PhysicsPart>().Solid);
        }
        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("wet")] [TestCase("heat")]
        [TestCase("duplicate")] [TestCase("custom")] [TestCase("solid")] [TestCase("missing")]
        public void DuneReliefOnlyUsesDisclosedUnambiguousDrySand(string fault)
        {
            DuneScene(); Relief(true); var cell = zone.GetCell(X, Y + 1); var sand = cell.Occupants[0];
            switch (fault)
            {
                case "hidden": cell.IsVisible = false; break;
                case "unexplored": cell.Explored = false; break;
                case "wet": zone.TileState.WriteCoating(X, Y + 1, "water", 4); break;
                case "heat": zone.TileState.AddHeat(X, Y + 1, 1); break;
                case "duplicate": Add("Sand", X, Y + 1); break;
                case "custom": sand.GetPart<RenderPart>().VisualVariant = "unmapped"; break;
                case "solid": Add("DuneCrest", X, Y + 1); break;
                case "missing": zone.RemoveEntity(sand); break;
            }
            Relief(false);
        }
        [Test] public void DuneReliefDoesNotExpandRegionOrReadHiddenOwners()
        {
            DuneScene(); Relief(true);
            var hidden = Add("DuneCrest", X, Y + 1); hidden.GetPart<RenderPart>().Visible = false;
            Relief(true); // no hidden-owner side channel
            zone = Attach(Home); Add("DuneCrest"); Add("Sand", X, Y + 1); Relief(false);
        }
        [Test] public void DuneReliefMissingOptionalAtlasKeepsApprovedBodyAndDisposal()
        {
            DuneScene(); Relief(true); var tile = View().ForegroundTile; var sprite = tile.sprite;
            Inject(Resources.Load<Texture2D>(Atlas)); Relief(false);
            Assert.True(tile == null && sprite == null);
            Assert.NotNull(Resources.Load<Texture2D>(DuneRelief));
        }
        [Test] public void DuneReliefMalformedOptionalAtlasCannotReplaceApprovedBody()
        {
            DuneScene(); Relief(true);
            var selector = new FlatPixelFallbackCatalog(); var bad = new Texture2D(63,24);
            try
            {
                using (var adapter = new FlatPixelBeatingMaterialsCatalog(Resources.Load<Texture2D>(Atlas),
                    null,null,null,null,bad,selector))
                {
                    var crest=zone.GetCell(X,Y).Occupants[0];
                    Assert.True(adapter.TryResolveForeground(zone,X,Y,crest,out var tile)); NativeTile(tile,Atlas);
                }
                Assert.True(bad!=null, "Borrowed malformed resource remains owned by caller.");
            }
            finally { selector.Dispose(); Object.DestroyImmediate(bad); }
        }
        [Test] public void DuneReliefNeighborOnlyDirtyMatchesFullRepaint()
        {
            DuneScene();
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); NativeTile(paint.At(X,Y), DuneRelief);
                var neighbor = Add("DuneCrest", X, Y + 1); paint.Dirty(X,Y+1); paint.Step();
                NativeTile(paint.At(X,Y), Atlas);
                zone.RemoveEntity(neighbor); paint.Dirty(X,Y+1); paint.Step(); var partial=paint.At(X,Y);
                NativeTile(partial,DuneRelief); paint.Renderer.MarkDirty("dune full counter"); paint.Step();
                Assert.AreSame(partial,paint.At(X,Y));
                zone.GetCell(X,Y).IsVisible=false; paint.Dirty(X,Y); paint.Step();
                Assert.False(View().NativeForeground); Assert.AreSame(View().ForegroundTile,paint.At(X,Y));
            }
        }
    }
}
