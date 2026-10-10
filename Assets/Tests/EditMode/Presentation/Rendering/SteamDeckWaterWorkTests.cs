using System.Collections;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public class SteamDeckWaterWorkTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [TestCase(false)] [TestCase(true)]
        public void NativeWaterHasNoLegacyAnimationWorkAndFallbackRestoresIt(bool cachedBeforeBind)
        {
            using (var f = new SpawnRing3DIntegrationFixture(SpawnRing3DIntegrationFixture.Fen, legacyFen: true))
            {
                var root = new GameObject("Owned Deck water probe"); root.SetActive(false); root.AddComponent<Grid>();
                try
                {
                    Tilemap Map(string name) { var go = new GameObject(name, typeof(Tilemap)); go.transform.SetParent(root.transform); return go.GetComponent<Tilemap>(); }
                    var tiles = Map("tiles"); var fine = Map("fine"); var bg = Map("background");
                    var renderer = tiles.gameObject.AddComponent<ZoneRenderer>();
                    void Set(string name, object value) => typeof(ZoneRenderer).GetField(name, Private).SetValue(renderer, value);
                    void Call(string name, params object[] args) => typeof(ZoneRenderer).GetMethod(name, Private).Invoke(renderer, args);
                    typeof(ZoneRenderer).GetProperty("CurrentZone").SetValue(renderer, f.Zone);
                    Set("_tilemap", tiles); Set("_fineWaterTilemap", fine); Set("_bgTilemap", bg);
                    var cell = f.FreeCell(); var water = f.Add("WaterPuddle", cell.x, cell.y); water.SetTag("FlowsEast", "0.250"); f.Refresh();
                    if (cachedBeforeBind) { Call("RefreshWaterCache"); Call("UpdateAmbientAnimations", .4f); Assert.Greater(fine.GetUsedTilesCount(), 0); }
                    Set("_spawnRing3DPresenter", f.Presenter);
                    Call("RefreshWaterCache");
                    Assert.Zero(((IList)typeof(ZoneRenderer).GetField("_waterTilePositions", Private).GetValue(renderer)).Count);
                    Assert.Zero(fine.GetUsedTilesCount(), "Previously painted fine water must be retired when native ownership begins.");
                    var at = new Vector3Int(cell.x, Zone.Height - 1 - cell.y, 0);
                    var tile = ScriptableObject.CreateInstance<Tile>();
                    try
                    {
                        tiles.SetTile(at, tile); tiles.SetTileFlags(at, TileFlags.None); tiles.SetColor(at, Color.magenta);
                        Call("UpdateAmbientAnimations", .4f); Assert.AreEqual(Color.magenta, tiles.GetColor(at));
                        Assert.Zero(fine.GetUsedTilesCount());
                        f.Call("SetPresentationVisible", false); Call("RefreshWaterCache"); Call("UpdateAmbientAnimations", .4f);
                        Assert.Greater(((IList)typeof(ZoneRenderer).GetField("_waterTilePositions", Private).GetValue(renderer)).Count, 0);
                        Assert.Greater(fine.GetUsedTilesCount(), 0, "ASCII fallback must still animate real water.");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(tile); }
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }
    }
}
