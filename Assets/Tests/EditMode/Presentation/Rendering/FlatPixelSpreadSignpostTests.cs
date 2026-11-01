using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Glyph → pixel, rank 2b (Docs/GLYPH-TO-PIXEL-REPLACEMENT.md): a signpost in ordinary Spread
    /// country (a boundary-stone waysite, Docs/CHUNK-ACTIVITY.md) reuses the opening's approved
    /// signpost, opening-anchors frame 8, on the real grass beneath it. Only the canonical body is
    /// admitted: the weathered 'I' &amp;y post that carries carved directions and nothing else.
    /// </summary>
    public sealed class FlatPixelSpreadSignpostTests
    {
        const string Home = "Overworld.10.11.0";
        const int X = 10, Y = 10;
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone; FlatPixelGreyboxCatalog world;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope(); manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = new Zone(Home); manager.SetActiveZone(zone); foreach (var c in zone.Cells) c.IsVisible = c.Explored = true;
            Assume.That(manager.WorldMap.GetPOI(10, 11), Is.Null, "ordinary Spread country");
            world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup() { world?.Dispose(); content?.Dispose(); }

        Entity OnGrass(string name)
        {
            Assert.True(zone.AddEntity(content.Factory.CreateEntity("Grass"), X, Y));
            var e = content.Factory.CreateEntity(name); Assert.NotNull(e, name); Assert.True(zone.AddEntity(e, X, Y)); return e;
        }
        bool Native(Entity e, out FlatPixelGreyboxCell v)
            => world.TryResolvePresentation(zone, X, Y, out v) && ReferenceEquals(v.Owner, e) && v.NativeForeground;

        [Test] public void ASignpostInOrdinarySpreadCountryDrawsTheOpeningsApprovedSignpost()
        {
            var post = OnGrass("Signpost");
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone), "not an opening zone");
            Assert.True(Native(post, out var v), "the signpost is no longer an 'I' glyph");
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath), v.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(128, 0, 16, 24), v.ForegroundTile.sprite.rect, "frame 8 of 12, the opening's signpost");
            Assert.True(v.LocalGround, "it stands on the real grass");
            Assert.True(world.TryGetPresentationSubjectSprite(zone, post, X, Y, out var portrait, out _));
            Assert.AreSame(v.ForegroundTile.sprite, portrait, "the reader shows the same post");
        }

        [TestCase("recoloured")] [TestCase("reglyphed")] [TestCase("relayered")] [TestCase("passable")] [TestCase("blank")]
        [TestCase("container")] [TestCase("burnable")] [TestCase("carried")] [TestCase("visual")]
        public void OnlyTheCanonicalSignpostIsAdmitted(string mutation)
        {
            var post = OnGrass("Signpost");
            Assume.That(Native(post, out _));
            var render = post.GetPart<RenderPart>();
            switch (mutation)
            {
                case "recoloured": render.ColorString = "&R"; break;
                case "reglyphed": render.RenderString = "|"; break;
                case "relayered": render.RenderLayer = 0; break;
                case "passable": post.GetPart<PhysicsPart>().Solid = false; break;
                case "blank": post.RemovePart(post.GetPart<RegionalSignpostPart>()); break;
                case "container": post.AddPart(new ContainerPart()); break;
                case "burnable": post.AddPart(new MaterialPart { MaterialID = "Wood" }); break;
                case "carried": post.GetPart<PhysicsPart>().InInventory = new Entity(); break;
                case "visual": render.VisualID = "elsewhere"; break;
            }
            Assert.False(Native(post, out _), mutation + ": counter-check, not the canonical signpost");
        }

        [Test] public void TheBeatingDrawsTheSameApprovedSignpost()
        {
            // Verification-sweep correction: the Beating already borrows opening-anchors frame 8, so a
            // signpost reads the same in both countries. Pinned so the two cannot drift apart.
            var beating = new Zone("Overworld.0.14.0"); manager.SetActiveZone(beating);
            foreach (var c in beating.Cells) c.IsVisible = c.Explored = true;
            beating.AddEntity(content.Factory.CreateEntity("Sand"), X, Y);
            var post = content.Factory.CreateEntity("Signpost"); beating.AddEntity(post, X, Y);
            Assert.True(world.TryResolvePresentation(beating, X, Y, out var v)); Assert.AreSame(post, v.Owner);
            Assert.True(v.NativeForeground);
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath), v.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(128, 0, 16, 24), v.ForegroundTile.sprite.rect);
        }
    }
}
