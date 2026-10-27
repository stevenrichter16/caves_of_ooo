using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>One optional native cue for disclosed live steam in East.
    /// This observes tile state, not vent identity or damaging SteamEffect.
    /// The atlas is borrowed; only the cached sprite and tile are owned.</summary>
    public sealed class FlatPixelEastSteamCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/east-steam";
        readonly Texture2D atlas;
        readonly Sprite sprite;
        readonly Tile tile;
        bool disposed;

        public FlatPixelEastSteamCatalog() : this(Resources.Load<Texture2D>(ResourcePath)) { }

        /// <summary>Missing or malformed injection never loads a substitute.</summary>
        public FlatPixelEastSteamCatalog(Texture2D borrowedAtlas)
        {
            if (borrowedAtlas == null || borrowedAtlas.width != 16 || borrowedAtlas.height != 24) return;
            atlas = borrowedAtlas;
            sprite = Sprite.Create(atlas, new Rect(0, 0, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 East live steam"; sprite.hideFlags = HideFlags.HideAndDontSave;
            tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
            tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
        }

        // Used before the lazy resource load as well as at resolution. Higher
        // priority marks remain the painter's decision, including native water
        // fallthrough. There need not be any underlying entity at all.
        internal static bool HasVisibleSteam(Zone zone, Cell cell)
        {
            if (zone?.ZoneID != "Overworld.12.10.0" || !FlatPixelGreyboxCatalog.IsAdmitted(zone)
                || !FlatPixelFallbackCatalog.IsCurrentGraph(zone) || cell == null
                || !ReferenceEquals(cell.ParentZone, zone) || !ReferenceEquals(zone.GetCell(cell.X, cell.Y), cell)
                || !cell.Explored || !cell.IsVisible) return false;
            var state = zone.TileState.Get(cell.X, cell.Y);
            return state != null && state.Cloud == "steam" && state.CloudTurns > 0;
        }

        public bool TryResolve(Zone zone, Cell cell, out Tile result)
        {
            result = null;
            if (disposed || atlas == null || sprite == null || tile == null || !HasVisibleSteam(zone, cell)) return false;
            result = tile; return true;
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyOwned(tile); DestroyOwned(sprite);
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
