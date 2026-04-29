using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Trap furniture showcase — demonstrates the three Tier-2 traps
    /// (SpikeTrap, FireTrap, BearTrap) trigger on step.
    ///
    /// Setup (player at left edge of corridor):
    ///
    ///   [Player] . [SpikeTrap] . [FireTrap] . [BearTrap] .
    ///
    /// Player walks east. Each trap fires once: spike pierces, fire
    /// burns + ignites, bear trap pierces + stuns + bleeds.
    ///
    /// What the player should observe in the message log:
    ///
    ///   "you springs the spike trap! Spikes pierce flesh." (-12 HP)
    ///
    ///   --- Step east ---
    ///   "you steps on the fire trap! Flames erupt." (-8 HP)
    ///   "you catches fire!"
    ///   (next turns: "you takes 1 fire damage." per tick)
    ///
    ///   --- Step east, after burning subsides ---
    ///   "you springs the bear trap! Iron jaws clamp shut." (-15 HP)
    ///   "you is bleeding!"
    ///   "you is stunned and cannot act!"
    ///   (next turn skipped — stun blocks action)
    ///
    /// Player loadout: HP 200, Strength 24. Generous so the player
    /// can step on all three traps without dying.
    /// </summary>
    [Scenario(
        name: "Trap Furniture Showcase",
        category: "Combat",
        description: "Three mechanical floor traps in a corridor — SpikeTrap (piercing), FireTrap (fire + burn), BearTrap (piercing + stun + bleed). Step east through the corridor to trigger each in turn.")]
    public class TrapFurnitureShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Player loadout — beefy HP so all three traps can be triggered
            // for observation without the player dying.
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .GiveItem("HealingTonic", 5);

            // Clear the corridor first. The starting zone often spawns chests,
            // items, or NPCs along the walkable path; without this, a chest
            // squatting on the trap cell would block the player from stepping
            // on the trap (chest is Solid, trap is Physics.Solid=false).
            // ClearCell preserves the player and terrain, removes everything
            // else. Cleared cells: every step from p.x+1..p.x+7 at p.y.
            for (int dx = 1; dx <= 7; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            // Place all three trap variants in a line east of the player.
            // The empty cells between (offsets 1, 3, 5, 7) give the player
            // breathing room to read the message log between triggers.
            var spikeTrap = ctx.Spawn("SpikeTrap").At(p.x + 2, p.y);
            var fireTrap = ctx.Spawn("FireTrap").At(p.x + 4, p.y);
            var bearTrap = ctx.Spawn("BearTrap").At(p.x + 6, p.y);

            MessageLog.Add("Trap Furniture Showcase: walk east through the corridor.");
            MessageLog.Add("Three single-use trap variants wait — spike (^&w), fire (^&R), bear (^&y).");
        }
    }
}
