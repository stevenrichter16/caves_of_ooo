using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Merchant shop showcase. Demonstrates the trade system end-to-end:
    ///
    ///   1. A Merchant 2 cells east of the player, stocked with three
    ///      tonics + one weapon (FlamingSword) + the player's IronKey
    ///      (now NoTrade-tagged, so SP.2 verifies it can't be sold).
    ///   2. Player has 200 HP, Strength 24, Ego 18 (good trade
    ///      performance), 250 drams in their pocket.
    ///   3. Bumping the merchant + selecting "[Let's trade.]" opens
    ///      the existing TradeUI.
    ///
    /// What the player should observe:
    ///
    ///     "you buy frost tonic for 24 drams."
    ///     "you sell short sword for 6 drams."
    ///     "You can't trade iron key."   (SP.2 NoTrade veto)
    ///
    /// Diag substrate (SP.4): every Buy / Sell records a
    /// `trade/Bought` or `trade/Sold` entry observable via
    /// `diag_query` MCP tool. Includes price, post-trade dram balance,
    /// and the buyer's perf — useful for "did the faction-modifier
    /// work?" debugging.
    /// </summary>
    [Scenario(
        name: "Merchant Shop Showcase",
        category: "Combat",
        description: "Bump merchant + [Let's trade.] to open TradeUI. Demonstrates SP.2 (NoTrade veto on IronKey), SP.3 (trader-state), SP.4 (trade/Bought + trade/Sold diag records).")]
    public class MerchantShopShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout ===
            // Strength 24 + Ego 18 = ~95% trade performance (cheap buys, lush sells).
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .SetStat("Ego", 18)
                .GiveItem("ShortSword", 1)
                .GiveItem("HealingTonic", 3)
                .GiveItem("IronKey", 1);  // NoTrade-tagged — can't be sold

            // Player starts with 250 drams.
            TradeSystem.SetDrams(ctx.PlayerEntity, 250);

            // === Merchant 2 cells east ===
            // ClearCell so a stray default item doesn't squat the merchant cell.
            for (int dx = 1; dx <= 4; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            var merchant = ctx.Spawn("Merchant").At(p.x + 2, p.y);

            // Stock the merchant: three tonics + one weapon. Total
            // value ~80 drams; player can buy any one with 250 drams.
            var inv = merchant.GetPart<InventoryPart>();
            if (inv == null)
            {
                inv = new InventoryPart();
                merchant.AddPart(inv);
            }
            inv.AddObject(ctx.Factory.CreateEntity("FrostTonic"));
            inv.AddObject(ctx.Factory.CreateEntity("FireTonic"));
            inv.AddObject(ctx.Factory.CreateEntity("AcidTonic"));
            inv.AddObject(ctx.Factory.CreateEntity("FlamingSword"));

            // Give the merchant 100 drams so they can afford to buy
            // the player's items.
            TradeSystem.SetDrams(merchant, 100);

            MessageLog.Add("Merchant Shop Showcase: bump the merchant, [Let's trade.]");
            MessageLog.Add("Try buying a frost tonic, selling your short sword.");
            MessageLog.Add("Then try to sell your iron key — SP.2 NoTrade refuses it.");
        }
    }
}
