using CavesOfOoo.Core;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Locked-door + Iron-key showcase. Demonstrates the Lock & Key v1
    /// (Docs/LOCK-AND-KEY.md) bump-to-unlock contract:
    ///
    ///   1. Player has an IronKey in inventory.
    ///   2. Bumping the LockedDoor north of the player → "you unlocks
    ///      the locked door." Door's IsLocked flips to false; Solid
    ///      drops; door tile yields next turn.
    ///   3. Walking through to the LockedChest (also keyed "iron")
    ///      uses the same key — master-key model, no consumption.
    ///   4. There's also a spare IronKey on the floor 2 cells south
    ///      of the player to demonstrate pickup mid-scenario.
    ///
    /// What the player should observe in the message log:
    ///
    ///     "you unlocks the locked door."
    ///     (next turn) [walk through]
    ///     "you unlocks the locked chest."
    ///
    /// And via diag (execute_code): every bump produces a single
    /// `furniture/UnlockAttempted` record with succeeded=true /
    /// keyId=iron / keyEntityId=&lt;the IronKey instance ID&gt;.
    ///
    /// IMPORTANT — what's NOT visible without dropping the key:
    ///
    /// To exercise the "no key" path, the player drops the IronKey
    /// (inventory → Drop), then bumps the door — message log says
    /// "the locked door is locked." and the diag record has
    /// succeeded=false / keyEntityId=null.
    /// </summary>
    [Scenario(
        name: "Locked Door Showcase",
        category: "Combat",
        description: "Bump-to-unlock with IronKey — walk into locked door + locked chest. Demonstrates LK.3 + LK.4 (furniture/UnlockAttempted diag).")]
    public class LockedDoorShowcase : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);

            // Player loadout — beefy enough to walk around comfortably.
            ctx.Player
                .SetStatMax("Hitpoints", 200)
                .SetHp(200)
                .SetStatMax("Strength", 30)
                .SetStat("Strength", 24)
                .GiveItem("IronKey", 1);

            // Clear the corridor east of the player so the door + chest
            // sit on otherwise-empty cells (matching the TrapFurniture
            // showcase's pattern — without ClearCell, default zone
            // furniture might block placement).
            for (int dx = 1; dx <= 6; dx++)
                ctx.World.ClearCell(p.x + dx, p.y);
            for (int dy = 1; dy <= 2; dy++)
                ctx.World.ClearCell(p.x, p.y + dy);

            // Locked door 2 cells east of player.
            ctx.Spawn("LockedDoor").At(p.x + 2, p.y);

            // Locked chest 2 cells past the door — same iron key opens both.
            ctx.Spawn("LockedChest").At(p.x + 4, p.y);

            // Spare IronKey on the floor 2 cells south of the player —
            // demonstrates pickup as a separate action from inventory-
            // resident keys.
            ctx.Spawn("IronKey").At(p.x, p.y + 2);

            MessageLog.Add("Locked Door Showcase: you have an iron key.");
            MessageLog.Add("Bump the locked door east, then walk through to the locked chest.");
            MessageLog.Add("A spare iron key is on the floor south of you for pickup demo.");
        }
    }
}
