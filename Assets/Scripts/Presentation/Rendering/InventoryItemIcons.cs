using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>Icons for item rows whose owners have already been disclosed by the menu.</summary>
    public static class InventoryItemIcons
    {
        internal const string AtlasPath = "Sprites/Live16x24/inventory-item-icons";
        internal const string IndexPath = "Content/Data/InventoryItemIcons";
        private const string Fallback = "unknown";

        [Serializable]
        private sealed class Index
        {
            public int Columns = 16;
            public int FrameHeight = 16;
            public Entry[] Icons;
        }

        [Serializable]
        private sealed class Entry
        {
            public string Blueprint;
            public int Frame;
        }

        private static readonly Dictionary<string, int> Frames = new(StringComparer.Ordinal);
        private static readonly Dictionary<int, Sprite> Sprites = new();
        private static readonly Dictionary<Sprite, Tile> Tiles = new();
        private static Texture2D _atlas;
        private static int _columns, _size;
        private static bool _loaded;

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            var asset = Resources.Load<TextAsset>(IndexPath);
            _atlas = Resources.Load<Texture2D>(AtlasPath);
            if (asset == null || _atlas == null) return;
            var index = JsonUtility.FromJson<Index>(asset.text);
            if (index?.Icons == null || index.Columns <= 0 || index.FrameHeight <= 0
                || (long)index.Columns * index.FrameHeight > _atlas.width || index.FrameHeight > _atlas.height)
                return;
            _columns = index.Columns;
            _size = index.FrameHeight;
            int capacity = _columns * (_atlas.height / _size);
            foreach (var row in index.Icons)
                if (row != null && !string.IsNullOrEmpty(row.Blueprint) && row.Frame >= 0 && row.Frame < capacity)
                    Frames.TryAdd(row.Blueprint, row.Frame);
        }

        public static Sprite For(Entity item)
        {
            if (!InventoryIconSettings.Enabled || item == null) return null;
            Load();
            if (!Frames.TryGetValue(item.BlueprintName ?? "", out int frame) && !Frames.TryGetValue(Fallback, out frame))
                return null;
            if (Sprites.TryGetValue(frame, out var sprite)) return sprite;
            int x = frame % _columns * _size, y = _atlas.height - (frame / _columns + 1) * _size;
            sprite = Sprite.Create(_atlas, new Rect(x, y, _size, _size), new Vector2(.5f, .5f), _size, 0, SpriteMeshType.FullRect);
            sprite.name = "Inventory item " + frame;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            Sprites.Add(frame, sprite);
            return sprite;
        }

        internal static void Paint(Tilemap map, Vector3Int position, Entity item)
        {
            var sprite = For(item);
            if (map == null || sprite == null) return;
            if (!Tiles.TryGetValue(sprite, out var tile))
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite;
                tile.flags = TileFlags.None;
                tile.colliderType = Tile.ColliderType.None;
                Tiles.Add(sprite, tile);
            }
            map.SetTile(position, tile);
            map.SetColor(position, Color.white);
        }
    }
}
