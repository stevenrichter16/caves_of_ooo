namespace CavesOfOoo.Core
{
    /// <summary>
    /// Display authority for tile-layer ids — the string vocabulary the
    /// tile universe speaks ("water", "oil", "ice", "embers", "smoke").
    /// Step 2 of the status-system plan
    /// (Docs/STATUS-EFFECTS-STUDY-2026-08.md §6 Option 1): before this,
    /// each surface answered "what is this id called?" on its own, which
    /// in practice meant most surfaces never answered at all.
    ///
    /// <para>Coatings resolve through <see cref="LiquidRegistry"/>
    /// (liquids already author a DisplayName); residues, clouds, and any
    /// unregistered id fall back to the id itself. The contract that
    /// matters: <see cref="DisplayName"/> NEVER returns null or empty
    /// for a non-empty id — a brand-new tile id ships with a readable
    /// (if plain) name instead of shipping pre-broken.</para>
    /// </summary>
    public static class TileStateCatalog
    {
        public static string DisplayName(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "";

            if (LiquidRegistry.IsInitialized)
            {
                var def = LiquidRegistry.Get(id);
                if (def != null && !string.IsNullOrEmpty(def.DisplayName))
                    return def.DisplayName;
            }

            // Residues ("embers"), clouds ("smoke", "steam"), and any id
            // without a liquid row read as themselves.
            return id;
        }
    }
}
