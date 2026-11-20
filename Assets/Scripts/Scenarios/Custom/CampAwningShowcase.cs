using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Hand-play fixture for the shade awning (Docs/MACHINE-INVENTED-USES.md 3o): the fresh game starts on ordinary wild ground in the Beating (requested through
    /// <c>ScenarioRunner.LaunchWild(BiomeType.Beating)</c>) with the clock at the start of the Height band and nothing on the head, so the glare is on from the first turn. It grants items
    /// and moves the clock forward only: every verb afterwards is the ordinary one.
    /// </summary>
    [Scenario(
        name: "Camp Awning (the Beating at noon)",
        category: "Home",
        description: "Starts on wild ground in the Beating at the start of the Height band, bareheaded, with the shade awning: stand in the glare to be parched, then set it down and stand beneath it.")]
    public class CampAwningShowcase : IScenario
    {
        /// <summary>What the pack receives. The awning alone is the machine; a torch is the only other gift, so nothing else covers the head or answers the sun.</summary>
        public static readonly (string Blueprint, int Count)[] Loadout = { ("CampAwning", 1), ("Torch", 1) };

        /// <summary>The tick the scenario moves the clock to: the first tick of the Height band, or the current tick when it is already Height (the clock never runs backwards, so a game past this day's Height goes to the next day's).</summary>
        public static int HeightStart(int tick)
        {
            if (WorldClock.GetBand(tick) == DayBand.Height) return tick;
            int dayStart = tick - tick % WorldClock.DayLengthTicks;
            int target = dayStart + (int)DayBand.Height * WorldClock.BandLengthTicks;
            return target >= tick ? target : target + WorldClock.DayLengthTicks;
        }

        public void Apply(ScenarioContext ctx)
        {
            foreach (var (blueprint, count) in Loadout) ctx.Player.GiveItem(blueprint, count);
            if (ctx.Turns != null) ctx.Turns.AdvanceClock(HeightStart(ctx.Turns.TickCount) - ctx.Turns.TickCount);
            ctx.Log("[Awning] It is the Height of the day on the open pan of the Beating, and nothing should be on your head (take off any hat). The glare is on from now.");
            ctx.Log("[Awning] 1. Stand still (rest or wait) for ten turns in the open: the sidebar parches you, and again every ten turns after. 2. Open the pack, choose the awning's 'set down' toward an open cell, and stand on it or in the ring of cells around it: the count starts over and no stacks come. 3. Step out of the ring and wait ten more: the glare finds you again. 4. Take the awning up and the shade goes with it. The Height band lasts 300 ticks; the sidebar shows the time of day.");
        }
    }
}
