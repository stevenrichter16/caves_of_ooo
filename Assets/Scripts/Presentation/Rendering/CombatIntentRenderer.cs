using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>Persistent amber direction chevrons on the existing XY composite.
    /// A fixed pool is allocated once; idle actors create no cues. If unusually
    /// crowded, the nearest commitments win with stable owner-ID ordering.</summary>
    public sealed class CombatIntentRenderer : IDisposable
    {
        public const int Capacity = 64;
        private struct Cue
        {
            public Entity Actor;
            public Cell Cell;
            public int Dx, Dy, Distance, Step;
            public bool Same(Cue other) => Actor == other.Actor && Cell == other.Cell && Dx == other.Dx && Dy == other.Dy;
        }
        private readonly LineRenderer[] lines = new LineRenderer[Capacity];
        private readonly Cue[] pending = new Cue[Capacity], displayed = new Cue[Capacity];
        private readonly Tilemap tilemap;
        private GameObject root;
        private Material material;
        private Vector3 lastOrigin, lastX, lastY;
        private bool hasTransform;
        public int VisibleCount { get; private set; }

        public CombatIntentRenderer(Transform parent, Tilemap tilemap, int renderLayer)
        {
            this.tilemap = tilemap;
            var shader = Shader.Find("Sprites/Default");
            if (parent == null || shader == null) return;
            root = new GameObject("Combat intent cues") { hideFlags = HideFlags.DontSave, layer = renderLayer };
            root.transform.SetParent(parent, false);
            material = new Material(shader) { name = "Owned combat intent cue", hideFlags = HideFlags.DontSave };
            for (int i = 0; i < Capacity; i++)
            {
                var child = new GameObject("tendril direction") { hideFlags = HideFlags.DontSave, layer = renderLayer };
                child.transform.SetParent(root.transform, false);
                var line = child.AddComponent<LineRenderer>(); lines[i] = line;
                line.sharedMaterial = material; line.positionCount = 3; line.useWorldSpace = true;
                line.loop = false; line.widthMultiplier = .085f; line.numCapVertices = line.numCornerVertices = 0;
                line.sortingOrder = 8; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false; line.startColor = line.endColor = new Color(.94f, .64f, .20f, .88f);
                line.enabled = false;
            }
            Clear();
        }

        /// <summary>Derive current cues without moving actors, advancing time,
        /// dispatching actions, or allocating objects during a frame.</summary>
        public void Refresh(Entity player, Zone zone, bool allowed)
        {
            if (root == null || !allowed || player?.HasTag("Player") != true
                || !CombatIntentReadout.IsVisibleActor(player, zone)) { Clear(); return; }
            int count = 0;
            var playerCell = zone.GetEntityCell(player);
            foreach (var actor in zone.GetReadOnlyEntities())
            {
                var intent = actor.GetPart<CommittedMeleePart>();
                if (intent == null || !intent.IsWindingUp || !CombatIntentReadout.IsVisibleActor(actor, zone)) continue;
                var source = zone.GetEntityCell(actor);
                int distance = Math.Max(Math.Abs(source.X - playerCell.X), Math.Abs(source.Y - playerCell.Y));
                for (int step = 1; step <= 2; step++)
                {
                    var cell = zone.GetCell(intent.OriginX + intent.DirectionX * step, intent.OriginY + intent.DirectionY * step);
                    if (!CombatIntentReadout.ThreatensVisibleCell(actor, zone, cell)) break;
                    Insert(new Cue { Actor = actor, Cell = cell, Dx = intent.DirectionX, Dy = intent.DirectionY,
                        Distance = distance, Step = step }, ref count);
                }
            }
            var origin = World(Vector3Int.zero); var xBasis = World(Vector3Int.right); var yBasis = World(Vector3Int.up);
            bool same = hasTransform && origin == lastOrigin && xBasis == lastX && yBasis == lastY && count == VisibleCount;
            if (same) for (int i = 0; i < count; i++) if (!pending[i].Same(displayed[i])) { same = false; break; }
            if (same) { ReleaseScratch(count); return; }
            for (int i = 0; i < count; i++)
            {
                Draw(lines[i], pending[i]); displayed[i] = pending[i]; lines[i].enabled = true;
            }
            for (int i = count; i < VisibleCount; i++) { lines[i].enabled = false; displayed[i] = default; }
            VisibleCount = count; lastOrigin = origin; lastX = xBasis; lastY = yBasis; hasTransform = true;
            root.SetActive(count > 0); ReleaseScratch(count);
        }

        private void Insert(Cue cue, ref int count)
        {
            int at = 0;
            while (at < count && Compare(pending[at], cue) <= 0) at++;
            if (at >= Capacity) return;
            int end = Math.Min(count, Capacity - 1);
            for (int i = end; i > at; i--) pending[i] = pending[i - 1];
            pending[at] = cue; if (count < Capacity) count++;
        }
        private static int Compare(Cue a, Cue b)
        {
            int compare = a.Distance.CompareTo(b.Distance); if (compare != 0) return compare;
            compare = string.Compare(a.Actor.ID, b.Actor.ID, StringComparison.Ordinal); if (compare != 0) return compare;
            return a.Step.CompareTo(b.Step);
        }
        private void ReleaseScratch(int count) { for (int i = 0; i < count; i++) pending[i] = default; }
        private Vector3 World(Vector3Int cell) => tilemap != null ? tilemap.CellToWorld(cell) : (Vector3)cell;
        private void Draw(LineRenderer line, Cue cue)
        {
            var tile = new Vector3Int(cue.Cell.X, Zone.Height - 1 - cue.Cell.Y, 0);
            var min = World(tile); var max = World(tile + new Vector3Int(1, 1, 0));
            var center = (min + max) * .5f; center.z = -.12f;
            var direction = new Vector3(cue.Dx, -cue.Dy, 0).normalized;
            var side = new Vector3(-direction.y, direction.x, 0);
            float size = Mathf.Min(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
            line.SetPosition(0, center - direction * (.15f * size) + side * (.15f * size));
            line.SetPosition(1, center + direction * (.18f * size));
            line.SetPosition(2, center - direction * (.15f * size) - side * (.15f * size));
        }
        public void Clear()
        {
            for (int i = 0; i < VisibleCount; i++) { if (lines[i] != null) lines[i].enabled = false; displayed[i] = default; }
            VisibleCount = 0; hasTransform = false; if (root != null) root.SetActive(false);
        }
        private static void DestroyOwned(UnityEngine.Object value)
        { if (value == null) return; if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
        public void Dispose() { Clear(); DestroyOwned(root); DestroyOwned(material); root = null; material = null; }
    }
}
