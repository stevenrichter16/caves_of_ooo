using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders free-floating ember particles that rise from campfires.
    /// Unlike tilemap-based FX, these move smoothly in world space
    /// with sinusoidal horizontal drift, independent of the grid.
    /// </summary>
    public class CampfireEmberRenderer : MonoBehaviour
    {
        private const float SpawnInterval = 0.12f;
        private const float EmberLifetime = 1.2f;
        private const float RiseSpeed = 1.8f;      // units per second upward
        private const float DriftAmplitude = 0.4f;  // horizontal wobble range
        private const float DriftFrequency = 3.0f;  // wobble cycles per second
        private const int MaxEmbers = 30;
        private const int SortingOrder = 1;

        private static readonly Color[] EmberColors =
        {
            new Color(1f, 0.3f, 0.1f, 1f),   // red-orange
            new Color(1f, 0.6f, 0.1f, 1f),   // orange
            new Color(1f, 0.85f, 0.2f, 1f),  // yellow
            new Color(1f, 1f, 0.8f, 1f),     // white-hot
        };

        private readonly List<Ember> _embers = new List<Ember>();
        private readonly List<EmberAnchor> _anchors = new List<EmberAnchor>();
        private float _spawnTimer;

        private Sprite _dotSprite;
        private Zone _zone;

        private class Ember
        {
            public GameObject Go;
            public SpriteRenderer Sr;
            public float Age;
            public float Lifetime;
            public float StartX;
            public float StartY;
            public float DriftPhase;
            public Color BaseColor;
        }

        private class EmberAnchor
        {
            public Entity Entity;
            public float WorldX;
            public float WorldY;
        }

        public void SetZone(Zone zone)
        {
            _zone = zone;
            ClearAnchors();
            ClearEmbers();
        }

        public void RegisterCampfire(Entity entity, int cellX, int cellY)
        {
            // Convert game coords to world coords, centering on the cell
            float worldX = cellX + 0.5f;
            float worldY = Zone.Height - 1 - cellY + 0.5f;

            _anchors.Add(new EmberAnchor
            {
                Entity = entity,
                WorldX = worldX,
                WorldY = worldY
            });
        }

        public void ClearAnchors()
        {
            _anchors.Clear();
        }

        private void Awake()
        {
            // Get the dot sprite from the CP437 tileset
            Tile dotTile = CP437TilesetGenerator.GetTile('.');
            if (dotTile != null)
                _dotSprite = dotTile.sprite;
        }

        private void LateUpdate()
        {
            if (_anchors.Count == 0 || _dotSprite == null)
                return;

            float dt = Time.deltaTime;

            // Spawn new embers
            _spawnTimer += dt;
            while (_spawnTimer >= SpawnInterval && _embers.Count < MaxEmbers)
            {
                _spawnTimer -= SpawnInterval;
                SpawnEmber();
            }

            // Update existing embers
            for (int i = _embers.Count - 1; i >= 0; i--)
            {
                Ember ember = _embers[i];
                ember.Age += dt;

                if (ember.Age >= ember.Lifetime)
                {
                    Destroy(ember.Go);
                    _embers.RemoveAt(i);
                    continue;
                }

                float t = ember.Age / ember.Lifetime;

                // Rise upward
                float y = ember.StartY + ember.Age * RiseSpeed;

                // Sinusoidal horizontal drift
                float x = ember.StartX + Mathf.Sin(ember.Age * DriftFrequency * Mathf.PI * 2f + ember.DriftPhase) * DriftAmplitude;

                ember.Go.transform.localPosition = new Vector3(x, y, 0f);

                // Fade out over lifetime: full opacity first half, fade in second half
                float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
                Color c = ember.BaseColor;
                c.a = Mathf.Clamp01(alpha);
                ember.Sr.color = c;

                // Shift color from base toward white-hot as it rises (hotter at base, cooler at top)
                // Actually invert: start hot, cool to red as it rises
                if (t > 0.3f)
                {
                    float coolFactor = (t - 0.3f) / 0.7f;
                    c.g *= (1f - coolFactor * 0.5f);
                    c.b *= (1f - coolFactor * 0.8f);
                    c.a = Mathf.Clamp01(alpha);
                    ember.Sr.color = c;
                }
            }
        }

        private void SpawnEmber()
        {
            if (_anchors.Count == 0)
                return;

            // Pick a random campfire anchor
            EmberAnchor anchor = _anchors[Random.Range(0, _anchors.Count)];

            // Random offset near the fire center
            float offsetX = Random.Range(-0.3f, 0.3f);
            float offsetY = Random.Range(-0.1f, 0.2f);

            var go = new GameObject("Ember");
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _dotSprite;
            sr.sortingOrder = SortingOrder;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            Color baseColor = EmberColors[Random.Range(0, EmberColors.Length)];

            var ember = new Ember
            {
                Go = go,
                Sr = sr,
                Age = 0f,
                Lifetime = EmberLifetime * Random.Range(0.7f, 1.3f),
                StartX = anchor.WorldX + offsetX,
                StartY = anchor.WorldY + offsetY,
                DriftPhase = Random.Range(0f, Mathf.PI * 2f),
                BaseColor = baseColor
            };

            go.transform.localPosition = new Vector3(ember.StartX, ember.StartY, 0f);
            sr.color = baseColor;

            _embers.Add(ember);
        }

        private void ClearEmbers()
        {
            for (int i = 0; i < _embers.Count; i++)
            {
                if (_embers[i].Go != null)
                    Destroy(_embers[i].Go);
            }
            _embers.Clear();
            _spawnTimer = 0f;
        }

        private void OnDestroy()
        {
            ClearEmbers();
        }
    }
}
