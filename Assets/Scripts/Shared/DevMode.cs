namespace CavesOfOoo.Core
{
    /// <summary>
    /// Master gate for developer conveniences in NORMAL play
    /// (ALPHA-READINESS item 4, Docs/ALPHA-READINESS.md). When false —
    /// the shipping default — the game starts with the small designed
    /// <see cref="NewGameLoadout"/> and none of the following exist:
    ///   - the 8 free showcase attack mutations,
    ///   - the debug weapon/NPC spawns at the player's feet,
    ///   - full tinkering unlocks (all recipes + 5 of every bit),
    ///   - the 19-stack crafting kit + one-of-each tonic grant,
    ///   - the F7/F8/F9/P debug keys (F8 permanently dismembered the
    ///     player's limb with no confirmation),
    ///   - village barrel-demo layouts + the material sandbox.
    ///
    /// Flip to true (in code, or from a dev scenario / test) to get the
    /// full sandbox back. Mirrors the GraphicsPolish master-gate idiom,
    /// but is a FIELD, not a const, so tests and dev tooling can toggle
    /// it at runtime (always restore in a finally).
    /// </summary>
    public static class DevMode
    {
        public static bool Enabled = false;
    }
}
