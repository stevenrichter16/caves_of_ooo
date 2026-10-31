using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Flat 16x24 quest cues: a small original marker one pixel above the head of any
    /// visible creature that has something to ask ("!", QuestCueState.Available) or whose
    /// work the player is doing ("...", Active). The same <see cref="QuestCueStateQuery"/>
    /// drives the 3D presenters' cues, so quest beacons, regional requests and place
    /// concerns are all marked, however the creature's body is drawn (static resident
    /// sprite or animated actor). Pooled sprites; re-evaluated every
    /// <see cref="CadenceFrames"/> frames, because concern predicates parse their arguments.
    /// Art and its editable source: ArtSource/Live16x24/quest-cue.
    /// </summary>
    public sealed class FlatQuestCueLayer : MonoBehaviour
    {
        public const int CadenceFrames = 15;
        public const int SortingOrder = 7; // above actor bodies (6)
        const float GapUnits = 1f / 16f, BodyHeightUnits = 1.5f;

        readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        Tilemap world;
        Func<Zone> zoneSource;
        Func<bool> activeSource;
        Sprite available, active;
        Material material;
        int frame;
        int shown;

        public int ShownCount => shown;

        public void Init(Tilemap worldTilemap, Func<Zone> zone, Func<bool> isActive)
        {
            world = worldTilemap; zoneSource = zone; activeSource = isActive;
            available = Resources.Load<Sprite>("Sprites/Actors/quest_cue_available");
            active = Resources.Load<Sprite>("Sprites/Actors/quest_cue_active");
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            if (shader != null) material = new Material(shader) { name = "Flat quest cues (Runtime)", hideFlags = HideFlags.DontSave };
        }

        void LateUpdate() => Sync(false);

        /// <summary>Re-evaluate now (<paramref name="force"/>) or on the cadence.</summary>
        public void Sync(bool force)
        {
            if (!force && ++frame < CadenceFrames) return;
            frame = 0;
            var zone = zoneSource?.Invoke();
            bool on = zone != null && (activeSource?.Invoke() ?? false) && available != null && active != null;
            shown = 0;
            if (on)
            {
                Entity player = null;
                foreach (var e in zone.GetReadOnlyEntities()) if (e != null && e.HasTag("Player")) { player = e; break; }
                if (player != null)
                    foreach (var e in zone.GetReadOnlyEntities())
                    {
                        if (e == null || ReferenceEquals(e, player) || !e.HasTag("Creature")) continue;
                        var cell = zone.GetEntityCell(e);
                        if (cell == null || !cell.IsVisible) continue;
                        var state = QuestCueStateQuery.Evaluate(e, zone, player);
                        if (state == QuestCueState.None) continue;
                        Place(shown++, state == QuestCueState.Available ? available : active, cell.X, cell.Y);
                    }
            }
            for (int i = shown; i < pool.Count; i++) if (pool[i].enabled) pool[i].enabled = false;
        }

        void Place(int index, Sprite sprite, int x, int zoneY)
        {
            while (pool.Count <= index)
            {
                var go = new GameObject("QuestCue") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.layer = gameObject.layer;
                var r = go.AddComponent<SpriteRenderer>();
                r.sortingOrder = SortingOrder;
                if (material != null) r.sharedMaterial = material;
                pool.Add(r);
            }
            var cue = pool[index];
            if (!ReferenceEquals(cue.sprite, sprite)) cue.sprite = sprite;
            cue.transform.position = Anchor(x, zoneY) + new Vector3(0f, CellHeight(x, zoneY) + sprite.bounds.extents.y + GapUnits, 0f);
            if (!cue.enabled) cue.enabled = true;
        }

        // A 16x24 body fills its cell, so its head is the cell's top edge.
        float CellHeight(int x, int zoneY)
        {
            if (world == null) return BodyHeightUnits;
            var cell = new Vector3Int(x, Zone.Height - 1 - zoneY, 0);
            return (world.CellToWorld(cell + Vector3Int.up) - world.CellToWorld(cell)).y;
        }

        // Bottom-centre of the cell, the same projection actor feet use (AnimatedEntityRenderer.CellAnchor).
        Vector3 Anchor(int x, int zoneY)
        {
            int tileY = Zone.Height - 1 - zoneY;
            if (world == null) return new Vector3(x + 0.5f, tileY, -zoneY * 0.001f - 0.5f);
            var cell = new Vector3Int(x, tileY, 0);
            Vector3 origin = world.CellToWorld(cell);
            Vector3 anchor = origin + (world.CellToWorld(cell + Vector3Int.right) - origin) * 0.5f;
            anchor.z = -zoneY * 0.001f - 0.5f;
            return anchor;
        }

        void OnDestroy()
        {
            foreach (var r in pool) if (r != null) DestroyImmediate(r.gameObject);
            pool.Clear();
            if (material != null) DestroyImmediate(material);
        }
    }
}
