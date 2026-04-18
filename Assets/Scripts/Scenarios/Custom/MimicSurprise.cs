namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>
    /// Places a real Chest and a MimicChest (a Creature that looks like a chest)
    /// side by side, with a HealingTonic on the floor as a lure. Attack the wrong
    /// one and it attacks back.
    ///
    /// Phase 2d showcase: uses <c>ctx.World.PlaceObject</c> for the non-creature
    /// Chest and lure, and <c>ctx.Spawn</c> for the creature Mimic (which needs
    /// full brain/turn wiring). The two builders naturally complement each other.
    ///
    /// Note: the plan originally called for a GoldPile lure, but that blueprint
    /// doesn't exist — swapping in HealingTonic per the plan's risk note.
    /// </summary>
    [Scenario(
        name: "Mimic Surprise",
        category: "Content Demo",
        description: "A real chest and a mimic next to each other. One is lying.")]
    public class MimicSurprise : IScenario
    {
        public void Apply(ScenarioContext ctx)
        {
            // Real chest (decoy) 3 cells east of player.
            ctx.World.PlaceObject("Chest").AtPlayerOffset(3, 0);

            // Mimic disguised as a chest, 5 cells east. Tag:Creature already on
            // the MimicChest blueprint via Creature inheritance; AIAmbush makes it
            // wake on adjacent damage.
            ctx.Spawn("MimicChest").AtPlayerOffset(5, 0);

            // Lure item — HealingTonic on the floor between them.
            ctx.World.PlaceObject("HealingTonic").AtPlayerOffset(4, 0);

            ctx.Log("Mimic Surprise applied. One of the chests is lying to you.");
        }
    }
}
