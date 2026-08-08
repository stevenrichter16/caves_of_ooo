using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// PASS 15 round 2 — regression pin for the ghost-decay
    /// iterator bug. The decay loop wrote <c>_ghosts[pos] = ghost</c>
    /// INSIDE its foreach, invalidating the Dictionary enumerator:
    /// an InvalidOperationException every frame that any ghost was
    /// fading. Latent for the entire life of the feature because the
    /// GraphicsPolish gate shipped OFF; the round-2 live sweep (save
    /// loaded into a jungle full of movers) surfaced it within
    /// seconds. Found by screenshot + read_console, pinned here.
    /// </summary>
    [TestFixture]
    public class GlyphGhostRendererDecayTests
    {
        private GameObject _gridGo;
        private Tilemap _mainTilemap;
        private GlyphGhostRenderer _ghosts;

        [SetUp]
        public void Setup()
        {
            _gridGo = new GameObject("GhostTestGrid");
            _gridGo.AddComponent<Grid>();
            var mainGo = new GameObject("Main");
            mainGo.transform.SetParent(_gridGo.transform, false);
            _mainTilemap = mainGo.AddComponent<Tilemap>();
            mainGo.AddComponent<TilemapRenderer>();

            _ghosts = _gridGo.AddComponent<GlyphGhostRenderer>();
            _ghosts.Init(_gridGo.transform);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_gridGo);

        private static Entity Mover(string id)
        {
            var e = new Entity { ID = id, BlueprintName = "Snapjaw" };
            e.AddPart(new RenderPart { DisplayName = id, RenderString = "s", RenderLayer = 5 });
            return e;
        }

        /// <summary>SpawnGhost sources the ghost's tile from the MAIN
        /// tilemap at the mover's previous cell — paint a glyph there
        /// the way ZoneRenderer does (zone row y → tile row H-1-y) or
        /// no ghost spawns.</summary>
        private Tile PaintGlyphAtZoneCell(int x, int zoneY)
        {
            var t = ScriptableObject.CreateInstance<Tile>();
            t.name = "CP437_73"; // 's'
            _mainTilemap.SetTile(new Vector3Int(x, Zone.Height - 1 - zoneY, 0), t);
            return t;
        }

        [Test]
        public void DecayLoop_SurvivesFullGhostLifetime()
        {
            // A mover spawns a ghost, then the decay loop runs a full
            // lifetime. Pre-fix, PostRender #3 (first decay tick over
            // a live ghost) threw InvalidOperationException from the
            // foreach whose body wrote back into the dictionary.
            var zone = new Zone("G");
            var mover = Mover("m1");
            zone.AddEntity(mover, 5, 5);
            PaintGlyphAtZoneCell(5, 5);
            _ghosts.SetZone(zone);

            _ghosts.PostRender(_mainTilemap);           // record last-known
            zone.MoveEntity(mover, 6, 5);               // move → ghost at (5,5)
            _ghosts.PostRender(_mainTilemap);
            Assert.Greater(_ghosts.TestOnly_ActiveGhostCount, 0,
                "precondition: the move spawned a ghost");

            // Mirror pin (same bug class as sprite-pass R2): the ghost
            // paints at TILE row Height-1-zoneY, not raw zone y.
            Tilemap ghostTm = null;
            foreach (Transform child in _gridGo.transform)
                if (child.name == "GlyphGhostTilemap")
                    ghostTm = child.GetComponent<Tilemap>();
            Assert.IsNotNull(ghostTm, "ghost tilemap exists");
            Assert.IsNotNull(ghostTm.GetTile(new Vector3Int(5, Zone.Height - 1 - 5, 0)),
                "the ghost sits on the mover's previous cell's TILE row — " +
                "pre-fix it spawned on the vertically mirrored row");

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 12; i++) _ghosts.PostRender(_mainTilemap);
            }, "decaying ghosts must never invalidate the decay loop's enumeration");

            Assert.AreEqual(0, _ghosts.TestOnly_ActiveGhostCount,
                "counter-check: after a full lifetime every ghost expired and was removed");
        }

        [Test]
        public void Ghost_IsTheMoversGlyph_NotTheRepaintedTerrain()
        {
            // AUDIT 🟡 — SpawnGhost used to sample the previous cell
            // AFTER the repaint: the ghost duplicated the terrain '.'
            // instead of the mover. The tile is now CAPTURED before the
            // move.
            var zone = new Zone("G");
            var mover = Mover("m1");
            zone.AddEntity(mover, 5, 5);
            var moverTile = PaintGlyphAtZoneCell(5, 5); // 's' seen here
            _ghosts.SetZone(zone);
            _ghosts.PostRender(_mainTilemap);           // capture 's'

            zone.MoveEntity(mover, 6, 5);
            // The dirty repaint replaces the old cell with FLOOR:
            var floorTile = ScriptableObject.CreateInstance<Tile>();
            floorTile.name = "CP437_2E";
            _mainTilemap.SetTile(new Vector3Int(5, Zone.Height - 1 - 5, 0), floorTile);
            _ghosts.PostRender(_mainTilemap);           // spawn from capture

            Tilemap ghostTm = null;
            foreach (Transform child in _gridGo.transform)
                if (child.name == "GlyphGhostTilemap")
                    ghostTm = child.GetComponent<Tilemap>();
            var pos = new Vector3Int(5, Zone.Height - 1 - 5, 0);
            Assert.AreEqual("CP437_73", ghostTm.GetTile(pos)?.name,
                "the ghost is the MOVER's 's' — not the '.' the repaint left behind");
            Assert.AreEqual(moverTile.name, "CP437_73", "fixture sanity");
            Object.DestroyImmediate(floorTile);
        }

        [Test]
        public void Ghost_ActuallyFades()
        {
            // AUDIT 🟡 — the ghost tilemap never cleared TileFlags, so
            // every SetColor (spawn AND decay fade) was a LockColor
            // no-op: ghosts rendered full-bright then popped out. Same
            // root cause round 2 found in MakeTile.
            var zone = new Zone("G");
            var mover = Mover("m1");
            zone.AddEntity(mover, 5, 5);
            PaintGlyphAtZoneCell(5, 5);
            _ghosts.SetZone(zone);
            _ghosts.PostRender(_mainTilemap);
            zone.MoveEntity(mover, 6, 5);
            _ghosts.PostRender(_mainTilemap);           // spawn

            Tilemap ghostTm = null;
            foreach (Transform child in _gridGo.transform)
                if (child.name == "GlyphGhostTilemap")
                    ghostTm = child.GetComponent<Tilemap>();
            var pos = new Vector3Int(5, Zone.Height - 1 - 5, 0);
            float a0 = ghostTm.GetColor(pos).a;
            _ghosts.PostRender(_mainTilemap);           // decay tick
            _ghosts.PostRender(_mainTilemap);           // decay tick
            float a2 = ghostTm.GetColor(pos).a;

            Assert.Less(a2, a0,
                "alpha falls across decay ticks — LockColor no longer eats the fade");
        }

        [Test]
        public void LastKnown_PrunesEntitiesThatLeftTheZone()
        {
            // AUDIT ⚪ — dead/removed entities stayed in _lastKnown
            // forever, pinning their object graphs until zone change.
            var zone = new Zone("G");
            var mover = Mover("m1");
            zone.AddEntity(mover, 5, 5);
            PaintGlyphAtZoneCell(5, 5);
            _ghosts.SetZone(zone);
            _ghosts.PostRender(_mainTilemap);
            Assert.IsTrue(_ghosts.TestOnly_HasLastKnown(mover), "tracked while present");

            zone.RemoveEntity(mover);
            _ghosts.PostRender(_mainTilemap);

            Assert.IsFalse(_ghosts.TestOnly_HasLastKnown(mover),
                "gone from the zone → gone from tracking");
        }

        [Test]
        public void DecayLoop_ManySimultaneousGhosts_AllExpire()
        {
            // The live failure had a jungle full of movers — many
            // ghosts decaying in one frame. Pin the many-ghost shape.
            var zone = new Zone("G");
            var movers = new Entity[6];
            for (int i = 0; i < movers.Length; i++)
            {
                movers[i] = Mover("m" + i);
                zone.AddEntity(movers[i], 10 + i, 8);
                PaintGlyphAtZoneCell(10 + i, 8);
            }
            _ghosts.SetZone(zone);
            _ghosts.PostRender(_mainTilemap);
            for (int i = 0; i < movers.Length; i++)
                zone.MoveEntity(movers[i], 10 + i, 9);
            _ghosts.PostRender(_mainTilemap);
            Assert.GreaterOrEqual(_ghosts.TestOnly_ActiveGhostCount, movers.Length,
                "precondition: every mover left a ghost");

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 12; i++) _ghosts.PostRender(_mainTilemap);
            });
            Assert.AreEqual(0, _ghosts.TestOnly_ActiveGhostCount);
        }
    }
}
