namespace CavesOfOoo.Rendering
{
    public partial class ZoneRenderer
    {
        /// <summary>Apply a preference to this renderer only. Flat mode borrows
        /// sprite rendering; returning to previous graphics restores the supplied
        /// legacy sprite choice. Neither operation changes saved preferences or quality.</summary>
        public void ApplyWorldPresentation(WorldPresentationMode mode, bool legacySpritesEnabled)
        {
            bool flat = mode != WorldPresentationMode.Previous;
            bool sprites = flat || legacySpritesEnabled;
            if (_flatPixelModeEnabled != flat
                || (_envSpriteRenderer != null && _envSpriteRenderer.RenderingEnabled != sprites))
                _fullDirty = true;
            _flatPixelModeEnabled = flat;
            if (_envSpriteRenderer != null) _envSpriteRenderer.RenderingEnabled = sprites;
            // This also synchronizes projection when only sprite visibility changed.
            SyncVillagePresentation(!Paused && _envSpriteRenderer != null && sprites);
        }
    }
}
