using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// PlayMode QA scenario for the weapon-rental system.
    /// Per Docs/WEAPON-RENTAL-SYSTEM.md M3.
    ///
    /// Layout: a Quartermaster two cells east of the player, stocked
    /// with one of each rental weapon. Player has 200 HP (survives
    /// experimentation) and 250 Ink — enough to rent any single
    /// weapon outright with comfortable change.
    ///
    /// What the player should observe:
    ///
    ///   1. Bumping the quartermaster opens dialogue.
    ///   2. "Show me what's on the rack." → "The loaner dagger." rents
    ///      it. Inventory now contains it; Ink dropped by ~14.
    ///   3. The rental can be equipped + used in combat normally, but
    ///      attempting to sell it at any other merchant is refused
    ///      ("You can't trade loaner dagger.") — the M1.1 anti-exploit
    ///      veto via RentalPart.HandleEvent.
    ///   4. "I'm here to return a weapon." → "Here you are." returns
    ///      every rental from this Quartermaster, refunding 50 % of
    ///      the Ink paid.
    ///
    /// Diag substrate: every rent / return records `trade/Rented` or
    /// `trade/Returned` (RentalSystem.cs:Diag.Record). Observable via
    /// `diag_query category=trade kind=Rented`.
    /// </summary>
    [Scenario(
        name: "Rental Test Bench",
        category: "Combat",
        description: "Quartermaster east of player + 250 Ink. Bump + 'Show me what's on the rack' rents a weapon; 'I'm here to return a weapon' returns it. Demonstrates WRS.M1+M2+M3.")]
    public class RentalTestBench : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // === Player loadout ===
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 18)
                .SetStat("Ego", 16);

            // Player starts with 250 ink — enough to rent any single
            // weapon (longsword is the priciest at ~54 ink) and have
            // change for a second rental.
            RentalSystem.SetInk(ctx.PlayerEntity, 250);

            // === Quartermaster 2 cells east ===
            for (int dx = 1; dx <= 4; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            var qm = ctx.Spawn("Quartermaster").At(p.x + 2, p.y);

            // Stock the quartermaster with one of each rental weapon.
            // Mirror MerchantShopShowcase's manual-stock pattern —
            // blueprints don't auto-populate NPC inventories.
            var inv = qm.GetPart<InventoryPart>();
            if (inv == null)
            {
                inv = new InventoryPart();
                qm.AddPart(inv);
            }
            inv.AddObject(ctx.Factory.CreateEntity("LoanerDagger"));
            inv.AddObject(ctx.Factory.CreateEntity("LoanerSpear"));
            inv.AddObject(ctx.Factory.CreateEntity("LoanerLongsword"));

            ctx.Log("=== Rental Test Bench ===");
            ctx.Log("You have 250 ink. Bump the quartermaster.");
            ctx.Log("Rent a weapon. Try to sell it elsewhere (you can't).");
            ctx.Log("Then come back and 'I'm here to return a weapon' for a refund.");
            ctx.Log("Diag: diag_query category=trade kind=Rented");
        }
    }
}
