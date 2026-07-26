using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Crops SM7b — renderer-side fix for the stale wet-soil background
    /// (farming audit findings F6/F7, 2026-07-25).
    ///
    /// RenderCellCore only ever WRITES background tiles; nothing in the
    /// incremental dirty-cell path erased them. The bg tilemap was only
    /// cleared by RenderZone's full ClearAllTiles — which fires on player
    /// movement/UI close, NOT on waiting in place. So a crop that dried
    /// out (BackgroundColor cleared + MarkCellDirty) or matured (entity
    /// removed while wet) kept its dark wet-earth block painted
    /// indefinitely for a stationary player — the feature's primary
    /// "watch it dry" feedback showed the opposite of the state. The
    /// same root cause left stale gas-tint blocks after gas dissipation.
    ///
    /// Fix: RenderDirtyCells erases each dirty cell's bg tile before
    /// repainting; RenderCellCore re-writes it when the top entity still
    /// has a BackgroundColor.
    /// </summary>
    [TestFixture]
    public class ZoneRendererStaleBgTests
    {
        private GameObject _gridGo;
        private ZoneRenderer _renderer;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _gridGo = new GameObject("Grid");
            _gridGo.AddComponent<Grid>();

            var tilemapGo = new GameObject("ZoneTilemap");
            tilemapGo.transform.SetParent(_gridGo.transform, false);
            tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();
            _renderer = tilemapGo.AddComponent<ZoneRenderer>();
            // EditMode AddComponent does not reliably run Awake — mirror
            // the InputHandlerLookModeTests fixture.
            FieldInfo probe = typeof(ZoneRenderer).GetField("_worldCursorRenderer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (probe.GetValue(_renderer) == null)
                InvokeNonPublic(_renderer, "Awake");
        }

        [TearDown]
        public void TearDown()
        {
            // Destroy everything the renderer spawned (BgTilemap etc. are
            // siblings under the grid parent).
            if (_gridGo != null)
                Object.DestroyImmediate(_gridGo);
        }

        // ── Helpers ──────────────────────────────────────────────

        private static object InvokeNonPublic(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return method.Invoke(instance, args);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(instance, value);
        }

        private static Entity MakeBgEntity(string bg)
        {
            var e = new Entity { ID = "crop", BlueprintName = "TestCrop" };
            e.AddPart(new RenderPart
            {
                DisplayName = "test crop",
                RenderString = "t",
                ColorString = "&g",
                BackgroundColor = bg,
                RenderLayer = 5
            });
            return e;
        }

        private Zone MakeExploredZone()
        {
            var zone = new Zone("z");
            for (int y = 0; y < Zone.Height; y++)
                for (int x = 0; x < Zone.Width; x++)
                {
                    var cell = zone.GetCell(x, y);
                    cell.Explored = true;
                    cell.IsVisible = true;
                }
            return zone;
        }

        /// <summary>Full redraw, then emulate LateUpdate's contract of
        /// clearing the full-dirty flag so MarkCellDirty queues cells.</summary>
        private void RenderZoneAndSettle()
        {
            _renderer.RenderZone();
            SetPrivateField(_renderer, "_fullDirty", false);
        }

        private static Vector3Int TilePos(int x, int y)
            => new Vector3Int(x, Zone.Height - 1 - y, 0);

        // ════════════════════════════════════════════════════════════
        //   SM7b — stale bg tile on the incremental repaint path
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Sm7b_BgCleared_DirtyRepaint_ErasesStaleBgTile()
        {
            var zone = MakeExploredZone();
            var crop = MakeBgEntity(CropPart.WET_SOIL_BG);
            zone.AddEntity(crop, 3, 4);
            _renderer.SetZone(zone);
            RenderZoneAndSettle();

            Assert.IsNotNull(_renderer.BgTilemap.GetTile(TilePos(3, 4)),
                "sanity: full render painted the wet-soil bg block");

            // The dry-out transition: state cleared + cell marked dirty
            // (exactly what CropPart.OnDriedOut does), player never moves.
            crop.GetPart<RenderPart>().BackgroundColor = "";
            _renderer.MarkCellDirty(3, 4, "SoilDried");
            InvokeNonPublic(_renderer, "RenderDirtyCells");

            Assert.IsNull(_renderer.BgTilemap.GetTile(TilePos(3, 4)),
                "dirty repaint must erase the stale wet-soil block — a stationary player " +
                "otherwise sees wet earth forever after dry-out");
        }

        [Test]
        public void Sm7b_EntityRemoved_DirtyRepaint_ErasesStaleBgTile()
        {
            // The maturity path: the still-wet crop is removed from the
            // zone (produce replaces it) and the cell is marked dirty.
            var zone = MakeExploredZone();
            var crop = MakeBgEntity(CropPart.WET_SOIL_BG);
            zone.AddEntity(crop, 6, 7);
            _renderer.SetZone(zone);
            RenderZoneAndSettle();

            Assert.IsNotNull(_renderer.BgTilemap.GetTile(TilePos(6, 7)), "sanity: bg painted");

            zone.RemoveEntity(crop);
            _renderer.MarkCellDirty(6, 7, "CropMatured");
            InvokeNonPublic(_renderer, "RenderDirtyCells");

            Assert.IsNull(_renderer.BgTilemap.GetTile(TilePos(6, 7)),
                "removing the wet entity must not leave its bg block behind");
        }

        [Test]
        public void Sm7b_BgStillSet_DirtyRepaint_KeepsBgTile()
        {
            // Counter-check: a cell whose top entity STILL has a bg keeps
            // its block through an incremental repaint (re-watered crop,
            // ordinary dirty marks on wet cells).
            var zone = MakeExploredZone();
            var crop = MakeBgEntity(CropPart.WET_SOIL_BG);
            zone.AddEntity(crop, 9, 2);
            _renderer.SetZone(zone);
            RenderZoneAndSettle();

            _renderer.MarkCellDirty(9, 2, "CropWatered");
            InvokeNonPublic(_renderer, "RenderDirtyCells");

            Assert.IsNotNull(_renderer.BgTilemap.GetTile(TilePos(9, 2)),
                "a still-wet cell must keep its bg block through a dirty repaint");
        }
    }
}
