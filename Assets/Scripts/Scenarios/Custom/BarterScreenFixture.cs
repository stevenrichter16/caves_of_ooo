using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// EC6c manual fixture (Docs/ECONOMY-SYSTEM.md §12.3): opens the barter screen in the live game against a
    /// salt-walker standing in the deep wasteland (a scratch zone; the player's own zone is untouched), with
    /// salt and goods on both sides and one line marked, so the screen's layout, deal line and keys can be
    /// seen and tried. Esc closes it as usual. Opens through the real <c>InputHandler.OpenTrade</c> path.
    /// </summary>
    [Scenario(
        name: "Barter Screen Fixture",
        category: "Economy",
        description: "Opens the barter screen against a deep-wasteland salt-walker, with salt and goods on both sides, for a look at the screen and its keys.")]
    public class BarterScreenFixture : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var factory = ctx.Factory;
            (int x, int y) deep = (-1, -1);
            for (int y = 0; y < WorldMap.Height && deep.x < 0; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                    if (Tender.IsDeepWasteland(WorldMap.ToZoneID(x, y, 0))) { deep = (x, y); break; }
            if (deep.x < 0) { ctx.Log("[BarterFixture] FAILED: no deep wasteland on the map."); return; }

            var wasteland = new Zone(WorldMap.ToZoneID(deep.x, deep.y, 0));
            var walker = factory.CreateEntity("DesertHermit"); wasteland.AddEntity(walker, 10, 10);
            var shelf = walker.GetPart<InventoryPart>();
            shelf.Objects.Clear();
            foreach (var (bp, n) in new[] { ("PaleSalt", 18), ("HealingTonic", 2), ("MendleafSprig", 3), ("LongSword", 1) })
            {
                var item = factory.CreateEntity(bp); var s = item.GetPart<StackerPart>(); if (s != null) s.StackCount = n; shelf.AddObject(item);
            }
            var pack = ctx.PlayerEntity.GetPart<InventoryPart>();
            foreach (var (bp, n) in new[] { ("PaleSalt", 9), ("Dagger", 1) })
            {
                var item = factory.CreateEntity(bp); var s = item.GetPart<StackerPart>(); if (s != null) s.StackCount = n; pack.AddObject(item);
            }

            var input = Object.FindFirstObjectByType<InputHandler>();
            var open = typeof(InputHandler).GetMethod("OpenTrade", BindingFlags.Instance | BindingFlags.NonPublic);
            if (input == null || open == null) { ctx.Log("[BarterFixture] FAILED: no InputHandler.OpenTrade."); return; }
            open.Invoke(input, new object[] { walker });
            input.TradeUI.DealKey(KeyCode.Return); // take one healing tonic
            ctx.Log($"[BarterFixture] barter screen open: deal mode={input.TradeUI.IsDealMode}, tender={Tender.For(walker)}.");
        }
    }
}
