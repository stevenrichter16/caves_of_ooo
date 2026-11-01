using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// P5 B3 — flat 16x24 threat cues on the ground: the Sky-Sari's shadow on the cell it
    /// has marked (<see cref="StoopPart"/>), drawn under actor bodies so the player standing
    /// in it reads as standing in a shadow. Only visible cells. Pooled sprites; re-evaluated
    /// every <see cref="CadenceFrames"/> frames. Art and its editable source:
    /// ArtSource/Live16x24/threat-cue.
    /// </summary>
    public sealed class FlatThreatCueLayer : MonoBehaviour
    {
        public const int CadenceFrames = 4;
        public const int SortingOrder = 5; // above ground, below actor bodies (6)

        readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        Tilemap world;
        Func<Zone> zoneSource;
        Func<bool> activeSource;
        Sprite shadow;
        Material material;
        int frame;
        int shown;

        public int ShownCount => shown;

        public void Init(Tilemap worldTilemap, Func<Zone> zone, Func<bool> isActive)
        {
            world = worldTilemap; zoneSource = zone; activeSource = isActive;
            shadow = Resources.Load<Sprite>("Sprites/Actors/threat_stoop_shadow");
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            if (shader != null) material = new Material(shader) { name = "Flat threat cues (Runtime)", hideFlags = HideFlags.DontSave };
        }

        void LateUpdate() => Sync(false);

        public void Sync(bool force)
        {
            if (!force && ++frame < CadenceFrames) return;
            frame = 0;
            var zone = zoneSource?.Invoke();
            shown = 0;
            if (zone != null && shadow != null && (activeSource?.Invoke() ?? false))
                foreach (var e in zone.GetReadOnlyEntities())
                {
                    var stoop = e?.GetPart<StoopPart>();
                    if (stoop == null || !stoop.IsAloft || stoop.MarkZone != zone.ZoneID) continue;
                    var cell = zone.GetCell(stoop.MarkX, stoop.MarkY);
                    if (cell == null || !cell.IsVisible) continue;
                    Place(shown++, stoop.MarkX, stoop.MarkY);
                }
            for (int i = shown; i < pool.Count; i++) if (pool[i].enabled) pool[i].enabled = false;
        }

        void Place(int index, int x, int zoneY)
        {
            while (pool.Count <= index)
            {
                var go = new GameObject("ThreatCue") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                go.layer = gameObject.layer;
                var r = go.AddComponent<SpriteRenderer>();
                r.sortingOrder = SortingOrder;
                if (material != null) r.sharedMaterial = material;
                pool.Add(r);
            }
            var cue = pool[index];
            if (!ReferenceEquals(cue.sprite, shadow)) cue.sprite = shadow;
            cue.transform.position = Centre(x, zoneY);
            if (!cue.enabled) cue.enabled = true;
        }

        Vector3 Centre(int x, int zoneY)
        {
            int tileY = Zone.Height - 1 - zoneY;
            if (world == null) return new Vector3(x + 0.5f, tileY + 0.75f, -zoneY * 0.001f - 0.4f);
            var cell = new Vector3Int(x, tileY, 0);
            Vector3 origin = world.CellToWorld(cell);
            Vector3 centre = origin + (world.CellToWorld(cell + new Vector3Int(1, 1, 0)) - origin) * 0.5f;
            centre.z = -zoneY * 0.001f - 0.4f;
            return centre;
        }

        void OnDestroy()
        {
            foreach (var r in pool) if (r != null) DestroyImmediate(r.gameObject);
            pool.Clear();
            if (material != null) DestroyImmediate(material);
        }
    }
}
