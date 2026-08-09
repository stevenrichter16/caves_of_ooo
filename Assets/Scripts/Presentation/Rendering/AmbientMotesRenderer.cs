using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// PASS 15 round 5 — ambient world motion beyond the campfires:
    /// free-floating motes in world space, one component with a KIND
    /// per anchor. Clone of the CampfireEmberRenderer motion model
    /// (smooth world-space particles over the grid, sinusoidal drift).
    ///
    /// Kinds: Drip (cave water falling from stalactites), Spore
    /// (fungal motes rising off mushroom rings), Leaf (slow falling
    /// leaves near trees), Bee (tiny orbiters around beehives).
    /// Anchors register at zone load in ZoneRenderer.SetZone's scan —
    /// static positions only (fixtures, not movers).
    /// </summary>
    public class AmbientMotesRenderer : MonoBehaviour
    {
        public enum MoteKind { Drip, Spore, Leaf, Bee }

        private const int MaxMotes = 40;
        private const int SortingOrder = 1;

        private class Mote
        {
            public GameObject Go;
            public SpriteRenderer Sr;
            public MoteKind Kind;
            public float Age, Lifetime;
            public float StartX, StartY;
            public float DriftPhase;
            public Color BaseColor;
        }

        private class Anchor
        {
            public MoteKind Kind;
            public float WorldX, WorldY;
        }

        private readonly List<Mote> _motes = new List<Mote>();
        private readonly List<Anchor> _anchors = new List<Anchor>();
        private float _spawnTimer;
        private Sprite _dotSprite;

        // Per-kind tuning: (spawnWeight, lifetime, color pool)
        private static readonly Color[] DripColors =
        {
            new Color(0.55f, 0.75f, 0.95f, 1f),
            new Color(0.75f, 0.9f, 1f, 1f),
        };
        private static readonly Color[] SporeColors =
        {
            new Color(0.55f, 0.85f, 0.5f, 1f),
            new Color(0.75f, 0.9f, 0.55f, 1f),
            new Color(0.45f, 0.7f, 0.45f, 1f),
        };
        private static readonly Color[] LeafColors =
        {
            new Color(0.45f, 0.6f, 0.3f, 1f),
            new Color(0.65f, 0.6f, 0.25f, 1f),
            new Color(0.55f, 0.7f, 0.35f, 1f),
        };
        private static readonly Color[] BeeColors =
        {
            new Color(0.9f, 0.75f, 0.2f, 1f),
            new Color(0.25f, 0.22f, 0.15f, 1f),
        };

        public void SetZone(Zone zone)
        {
            _anchors.Clear();
            ClearMotes();
        }

        public void RegisterAnchor(MoteKind kind, int cellX, int cellY)
        {
            _anchors.Add(new Anchor
            {
                Kind = kind,
                WorldX = cellX + 0.5f,
                WorldY = Zone.Height - 1 - cellY + 0.5f,
            });
        }

        public int AnchorCount => _anchors.Count;

        private void Awake()
        {
            Tile dotTile = CP437TilesetGenerator.GetTile('.');
            if (dotTile != null) _dotSprite = dotTile.sprite;
        }

        private void LateUpdate()
        {
            if (_anchors.Count == 0 || _dotSprite == null) return;
            float dt = Time.deltaTime;

            // Spawn rate scales gently with anchor count, capped.
            float interval = Mathf.Max(0.10f, 0.5f / Mathf.Max(1, _anchors.Count / 3));
            _spawnTimer += dt;
            while (_spawnTimer >= interval && _motes.Count < MaxMotes)
            {
                _spawnTimer -= interval;
                SpawnMote();
            }

            for (int i = _motes.Count - 1; i >= 0; i--)
            {
                var m = _motes[i];
                m.Age += dt;
                if (m.Age >= m.Lifetime)
                {
                    Destroy(m.Go);
                    _motes.RemoveAt(i);
                    continue;
                }

                float t = m.Age / m.Lifetime;
                float x = m.StartX, y = m.StartY;
                switch (m.Kind)
                {
                    case MoteKind.Drip:
                        // Fast fall from the ceiling tile, no drift.
                        y = m.StartY - m.Age * 3.2f;
                        break;
                    case MoteKind.Spore:
                        // Slow rise with lazy wide drift.
                        y = m.StartY + m.Age * 0.35f;
                        x = m.StartX + Mathf.Sin(m.Age * 1.2f * Mathf.PI + m.DriftPhase) * 0.5f;
                        break;
                    case MoteKind.Leaf:
                        // Slow fall with wide sway.
                        y = m.StartY - m.Age * 0.55f;
                        x = m.StartX + Mathf.Sin(m.Age * 1.6f * Mathf.PI + m.DriftPhase) * 0.6f;
                        break;
                    case MoteKind.Bee:
                        // Tight orbit wobble around the hive.
                        x = m.StartX + Mathf.Cos(m.Age * 4f + m.DriftPhase) * 0.45f;
                        y = m.StartY + Mathf.Sin(m.Age * 5.3f + m.DriftPhase) * 0.3f;
                        break;
                }
                m.Go.transform.localPosition = new Vector3(x, y, 0f);

                // Fade in briefly, fade out over the tail.
                float alpha = t < 0.12f ? t / 0.12f : (t > 0.6f ? 1f - (t - 0.6f) / 0.4f : 1f);
                var c = m.BaseColor;
                c.a = Mathf.Clamp01(alpha) * 0.85f;
                m.Sr.color = c;
            }
        }

        private void SpawnMote()
        {
            var anchor = _anchors[Random.Range(0, _anchors.Count)];
            Color[] pool;
            float life, scale;
            switch (anchor.Kind)
            {
                case MoteKind.Drip:  pool = DripColors;  life = 0.5f; scale = 0.35f; break;
                case MoteKind.Spore: pool = SporeColors; life = 2.6f; scale = 0.3f; break;
                case MoteKind.Leaf:  pool = LeafColors;  life = 2.2f; scale = 0.45f; break;
                default:             pool = BeeColors;   life = 1.8f; scale = 0.3f; break;
            }

            var go = new GameObject("Mote");
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _dotSprite;
            sr.sortingOrder = SortingOrder;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var mote = new Mote
            {
                Go = go,
                Sr = sr,
                Kind = anchor.Kind,
                Age = 0f,
                Lifetime = life * Random.Range(0.75f, 1.3f),
                StartX = anchor.WorldX + Random.Range(-0.3f, 0.3f),
                StartY = anchor.WorldY + (anchor.Kind == MoteKind.Leaf ? Random.Range(0.2f, 0.6f) : Random.Range(-0.1f, 0.2f)),
                DriftPhase = Random.Range(0f, Mathf.PI * 2f),
                BaseColor = pool[Random.Range(0, pool.Length)],
            };
            go.transform.localPosition = new Vector3(mote.StartX, mote.StartY, 0f);
            sr.color = mote.BaseColor;
            _motes.Add(mote);
        }

        private void ClearMotes()
        {
            for (int i = 0; i < _motes.Count; i++)
                if (_motes[i].Go != null) Destroy(_motes[i].Go);
            _motes.Clear();
            _spawnTimer = 0f;
        }

        private void OnDestroy() => ClearMotes();
    }
}
