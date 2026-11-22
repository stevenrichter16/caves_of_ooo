using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Builders;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Hand-play fixture for the Concord's carriage (Docs/MACHINE-INVENTED-USES.md, 3k). It gives a purse and things worth sending, and puts one parcel already waiting on the cart at
    /// Cinderhold (sent from the Last Counter) so "collect" can be tried at the first post without waiting three days. It grants items and one cart record only; every verb afterwards is the
    /// ordinary conversation with the Concord's keepers. Cinderhold is at map (6,6) and the Last Counter at (18,18): open the world map with < to travel.
    /// </summary>
    [Scenario(
        name: "Concord Carriage",
        category: "Home",
        description: "Gives drams, timber and other goods, and one parcel already waiting at Cinderhold, for hand-testing the Concord's cart between Cinderhold and the Last Counter.")]
    public class ConcordCarriageShowcase : IScenario
    {
        public const int Drams = 60;
        public static readonly (string Blueprint, int Count)[] Loadout = { ("SalvagedTimber", 8), ("Dagger", 1), ("FireClay", 2), ("LiquidFlask", 1), ("BlastcapSpore", 1) };
        /// <summary>The parcel already on the cart, bound for Cinderhold: the Last Counter sent it a day ago.</summary>
        public const string WaitingBlueprint = "IronSpikeComponent"; public const int WaitingCount = 2;

        public void Apply(ScenarioContext ctx)
        {
            var player = ctx.PlayerEntity;
            TradeSystem.SetDrams(player, Drams);
            ScenarioLoadout.Give(ctx, Loadout, "[Carriage]");
            var waiting = ctx.Factory.CreateEntity(WaitingBlueprint);
            if (WaitingCount > 1) waiting.GetPart<StackerPart>().StackCount = WaitingCount;
            var cart = player.GetPart<ConcordCarriagePart>() ?? new ConcordCarriagePart();
            if (player.GetPart<ConcordCarriagePart>() == null) player.AddPart(cart);
            ConcordCarriage.TryPost(WorldMap.ToZoneID(6, 6, 0), out var cinderhold); ConcordCarriage.TryPost(WorldMap.ToZoneID(18, 18, 0), out var counter);
            int now = ctx.Turns?.TickCount ?? 0;
            cart.Add(waiting, counter.ZoneId, cinderhold.ZoneId, now - WorldClock.DayLengthTicks, now, ConcordCarriage.BaseFee);
            ctx.Log("[Carriage] 60 drams, 8 timber, a dagger, 2 fire clay, a flask, a blastcap spore, and one parcel (2 iron spikes) already waiting for you at Cinderhold.");
            ctx.Log("[Carriage] 1. Open the world map with < and travel to Cinderhold (6,6); find the Concord Factor in the pruning post and talk to him. 2. The greeting lists the parcel in your name and offers 'Collect what has arrived (1)': take it. 3. Choose 'Send a parcel to the Last Counter': the list shows each stack with its fee (the spore is not offered: clause five); pick the timber and read the bargain (all, half or one, each with its fee), then send. 4. The greeting now reads the parcel as due in 3 days. 5. Travel to the Last Counter (18,18), wait the days out (rest at a fire), and talk to the Envoy: 'Collect what has arrived'. 6. Open the world map at any time: one line says how many parcels ride and where any have arrived.");
        }
    }
}
