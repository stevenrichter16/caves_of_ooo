using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class FellingSceneClaimTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void AuthoredClaimsSuppressOnlyOwnedSceneryAndRetainRuntimeGlyphs(bool owned)
        {
            var root = new GameObject("Felling claim test");
            var glyph = ScriptableObject.CreateInstance<Tile>(); glyph.name = "CP437_41";
            try
            {
                root.AddComponent<Grid>();
                var mainObject = new GameObject("main"); mainObject.transform.SetParent(root.transform);
                var main = mainObject.AddComponent<Tilemap>(); mainObject.AddComponent<TilemapRenderer>();
                var env = root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(root.transform, main);
                env.SetAuthoredScenePredicates((x, y) => x == 40 && y == 20, e => owned);
                var zone = new Zone("Felling claim");
                var actor = new Entity { BlueprintName = "UnknownRuntimeThing" };
                actor.AddPart(new RenderPart { DisplayName = "thing", RenderString = "A", RenderLayer = 5 });
                zone.AddEntity(actor, 40, 20);
                zone.GetCell(40, 20).Explored = zone.GetCell(40, 20).IsVisible = true;
                var pos = new Vector3Int(40, 4, 0);
                main.SetTile(pos, glyph);
                env.PostRender(zone, Zone.Width, Zone.Height);
                var overlay = root.GetComponentsInChildren<Tilemap>()[1];
                Assert.IsNull(main.GetTile(pos));
                if (owned) Assert.IsNull(overlay.GetTile(pos));
                else Assert.AreSame(glyph, overlay.GetTile(pos), "Unmapped runtime entity stays above the native ground.");
                env.RenderingEnabled = false;
                env.PostRender(zone, Zone.Width, Zone.Height);
                Assert.AreSame(glyph, main.GetTile(pos), "Disabling sprite mode restores glyph ownership.");
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(glyph); }
        }
    }
}
