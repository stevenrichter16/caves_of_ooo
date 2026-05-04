using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Trap furniture showcase — demonstrates four Tier-2 traps:
    /// SpikeTrap, FireTrap, BearTrap (all single-use, consumed on
    /// trigger), plus PressurePlate (rearmable, persists in zone after
    /// firing — lets the player step ON-OFF-ON to see it re-fire).
    ///
    /// Setup (player at left edge of corridor):
    ///
    ///   [Player] . [SpikeTrap] . [FireTrap] . [BearTrap] . [PressurePlate]
    ///
    /// Player walks east. Each one-shot trap fires once and disappears;
    /// the PressurePlate at the far end fires every time the player
    /// steps onto it — step ON, step OFF (one cell back), step ON
    /// again to observe the re-fire (the asymmetry vs SpikeTrap which
    /// is gone after the first hit).
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
    ///   --- Step east onto PressurePlate ---
    ///   "you treads on the pressure plate." (-8 HP)
    ///   --- Step WEST one cell, then EAST back onto plate ---
    ///   "you treads on the pressure plate." (-8 HP again — rearmable)
    ///
    /// Player loadout: HP 200, Strength 24, 5 HealingTonics. Generous
    /// so the player can step on all four traps and still re-trigger
    /// the plate a few times without dying.
    /// </summary>
    [Scenario(
        name: "Trap Furniture Showcase",
        category: "Combat",
        description: "Four mechanical floor traps in a corridor — SpikeTrap (piercing one-shot), FireTrap (fire + burn one-shot), BearTrap (piercing + stun + bleed one-shot), PressurePlate (Bludgeoning rearmable). Step east; the plate re-fires on re-stepping.")]
    public class TrapFurnitureShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Player loadout — beefy HP so all four traps can be triggered
            // (and the rearmable plate re-triggered ~5x) without the player
            // dying. 5 HealingTonics for the player to recover between
            // observations.
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
            // else. Cleared cells: every step from p.x+1..p.x+9 at p.y
            // (extended one further than the original 7 to fit the new
            // PressurePlate at p.x+8).
            for (int dx = 1; dx <= 9; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);

            // Place trap variants in a line east of the player. The empty
            // cells between (offsets 1, 3, 5, 7, 9) give the player
            // breathing room to read the message log between triggers.
            // PressurePlate is at the far end so the player ends the
            // walkthrough on the rearmable trap and can play with re-stepping.
            var spikeTrap = ctx.Spawn("SpikeTrap").At(p.x + 2, p.y);
            var fireTrap = ctx.Spawn("FireTrap").At(p.x + 4, p.y);
            var bearTrap = ctx.Spawn("BearTrap").At(p.x + 6, p.y);
            var plate = ctx.Spawn("PressurePlate").At(p.x + 8, p.y);

            MessageLog.Add("Trap Furniture Showcase: walk east through the corridor.");
            MessageLog.Add("Four traps wait — spike (^&w one-shot), fire (^&R one-shot),");
            MessageLog.Add("bear (^&y one-shot), and pressure plate (^&K rearmable — step OFF and back ON to see it re-fire).");
        }
    }
}
