using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Presentation.Effects
{
    /// <summary>
    /// Pass 8 §8E.1 — scans the EnvironmentSpriteRenderer overlay tilemap
    /// for light-source sprites (campfire `*`, shrine `_`) and attaches
    /// a URP <see cref="Light2D"/> Point light at each cell's world
    /// position. Lights are pooled / re-used across frames; cells that
    /// no longer contain a recognized light-source tile have their
    /// child light retired.
    ///
    /// <para>Each light flickers via <c>Update()</c> using sin + Perlin
    /// noise so campfires breathe and shrines pulse subtly.</para>
    ///
    /// <para>Also responsible for the biome ambient drop — when a
    /// dungeon zone is active (Pass 8 heuristic: zone has any
    /// stone-floor tiles & is below ground), the Global Light 2D's
    /// intensity drops to <see cref="DungeonAmbientIntensity"/> so
    /// the spawned point lights actually create contrast.</para>
    ///
    /// <para>Plan: <c>Docs/GRAPHICS-PASS8.md</c></para>
    /// </summary>
    public class LightSourceSpriteHook : MonoBehaviour
    {
        // ── Tunables ───────────────────────────────────────────────

        [Header("Toggle")]
        public bool LightingEnabled = true;

        [Header("Campfire (`*` glyph)")]
        public float CampfireBaseIntensity = 1.6f;
        public Color CampfireColor = new Color(1.0f, 0.55f, 0.18f, 1f);
        public float CampfireOuterRadius = 4.5f;
        public float CampfireInnerRadius = 0.6f;
        public float CampfireFlickerSpeed = 7.0f;
        public float CampfireFlickerAmount = 0.25f;

        [Header("Shrine (`_` glyph)")]
        public float ShrineBaseIntensity = 1.1f;
        public Color ShrineColor = new Color(0.45f, 0.78f, 1.0f, 1f);
        public float ShrineOuterRadius = 3.5f;
        public float ShrineInnerRadius = 0.4f;
        public float ShrinePulseSpeed = 1.6f;
        public float ShrinePulseAmount = 0.15f;

        [Header("Lantern (Pass 10 — `!` glyph + Lantern blueprint)")]
        public float LanternBaseIntensity = 1.0f;
        public Color LanternColor = new Color(1.0f, 0.85f, 0.45f, 1f);
        public float LanternOuterRadius = 3.0f;
        public float LanternInnerRadius = 0.3f;
        public float LanternFlickerSpeed = 4.0f;
        public float LanternFlickerAmount = 0.10f;

        [Header("Ambient")]
        [Tooltip("Global Light 2D intensity when a dungeon zone is active.")]
        public float DungeonAmbientIntensity = 0.45f;
        [Tooltip("Global Light 2D intensity when an outdoor zone is active.")]
        public float OutdoorAmbientIntensity = 1.0f;

        [Header("Player torch (Pass 9 §9A)")]
        [Tooltip("If true, spawn a persistent Light2D child that follows "
            + "the player around the dungeon zone — a held-torch effect "
            + "so dim zones become walkable without total darkness.")]
        public bool PlayerTorchEnabled = true;
        public float PlayerTorchIntensity = 1.4f;
        public Color PlayerTorchColor = new Color(1.0f, 0.78f, 0.42f, 1f);
        public float PlayerTorchOuterRadius = 6.5f;
        public float PlayerTorchInnerRadius = 0.5f;
        public float PlayerTorchFlickerSpeed = 8.0f;
        public float PlayerTorchFlickerAmount = 0.12f;

        // ── Wiring ─────────────────────────────────────────────────

        private Transform _gridParent;
        private Tilemap _overlayTilemap;
        private Light2D _globalLight;

        private readonly Dictionary<Vector3Int, SpawnedLight> _spawned = new();
        private readonly List<Vector3Int> _toRemove = new();

        // Pass 9 — player torch state
        private GameObject _playerTorchGo;
        private Light2D _playerTorchLight;
        // Player + zone are pushed in by ZoneRenderer each PostRender so
        // we can position the torch at the player's current cell.
        private Entity _playerEntity;
        private Zone _activeZone;

        public bool IsInitialized { get; private set; }

        private struct SpawnedLight
        {
            public GameObject Go;
            public Light2D Light;
            public LightKind Kind;
            public float Seed; // for per-light Perlin offset
        }

        private enum LightKind { Campfire, Shrine, Lantern }

        // ── Init ───────────────────────────────────────────────────

        public void Init(Transform gridParent, Tilemap overlayTilemap, Light2D globalLight)
        {
            _gridParent = gridParent;
            _overlayTilemap = overlayTilemap;
            _globalLight = globalLight;
            IsInitialized = true;
        }

        /// <summary>
        /// Pass 9 §9B — push the current player + zone references in
        /// each PostRender so the torch can reposition to the player's
        /// cell. Cheap; both refs are POCOs.
        /// </summary>
        public void SetPlayerContext(Entity player, Zone zone)
        {
            _playerEntity = player;
            _activeZone = zone;
        }

        // ── Per-frame scan (called from ZoneRenderer.PostRender after
        //    EnvironmentSpriteRenderer has painted the overlay) ─────

        public void PostRender(int width, int height, bool isDungeon)
        {
            if (!IsInitialized) return;

            // Update global ambient
            if (_globalLight != null)
            {
                float target = isDungeon ? DungeonAmbientIntensity : OutdoorAmbientIntensity;
                _globalLight.intensity = LightingEnabled ? target : OutdoorAmbientIntensity;
            }

            // If lighting disabled, retire all spawned lights
            if (!LightingEnabled || _overlayTilemap == null)
            {
                RetireAll();
                RetirePlayerTorch();
                return;
            }

            // Pass 9 §9A — player-held torch updates BEFORE we scan
            // for stationary lights so they all use the same frame's
            // position state.
            UpdatePlayerTorch(isDungeon);

            // Track which cells currently host light-sources
            var seenThisFrame = new HashSet<Vector3Int>(_spawned.Count);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    var t = _overlayTilemap.GetTile(pos);
                    if (t == null) continue;
                    LightKind kind;
                    if (t.name == "Campfire") kind = LightKind.Campfire;
                    else if (t.name == "Shrine") kind = LightKind.Shrine;
                    else if (t.name == "Lantern") kind = LightKind.Lantern;
                    else continue;

                    seenThisFrame.Add(pos);
                    if (!_spawned.ContainsKey(pos))
                        SpawnLight(pos, kind);
                }
            }

            // Retire lights whose cell no longer holds a recognized tile
            _toRemove.Clear();
            foreach (var kvp in _spawned)
                if (!seenThisFrame.Contains(kvp.Key))
                    _toRemove.Add(kvp.Key);
            for (int i = 0; i < _toRemove.Count; i++)
                RetireAt(_toRemove[i]);
        }

        // ── Spawning + retirement ──────────────────────────────────

        private void SpawnLight(Vector3Int cellPos, LightKind kind)
        {
            var go = new GameObject($"Light_{kind}_{cellPos.x}_{cellPos.y}");
            go.transform.SetParent(_gridParent != null ? _gridParent : transform, false);
            // Cells are 1-unit; offset to cell center.
            go.transform.localPosition = new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f, 0f);

            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            switch (kind)
            {
                case LightKind.Campfire:
                    light.color = CampfireColor;
                    light.intensity = CampfireBaseIntensity;
                    light.pointLightInnerRadius = CampfireInnerRadius;
                    light.pointLightOuterRadius = CampfireOuterRadius;
                    light.falloffIntensity = 0.65f;
                    break;
                case LightKind.Shrine:
                    light.color = ShrineColor;
                    light.intensity = ShrineBaseIntensity;
                    light.pointLightInnerRadius = ShrineInnerRadius;
                    light.pointLightOuterRadius = ShrineOuterRadius;
                    light.falloffIntensity = 0.5f;
                    break;
                case LightKind.Lantern:
                    light.color = LanternColor;
                    light.intensity = LanternBaseIntensity;
                    light.pointLightInnerRadius = LanternInnerRadius;
                    light.pointLightOuterRadius = LanternOuterRadius;
                    light.falloffIntensity = 0.7f;
                    break;
            }

            _spawned[cellPos] = new SpawnedLight
            {
                Go = go,
                Light = light,
                Kind = kind,
                Seed = (cellPos.x * 1.31f + cellPos.y * 2.17f) % 7f,
            };
        }

        private void RetireAt(Vector3Int cellPos)
        {
            if (!_spawned.TryGetValue(cellPos, out var sl)) return;
            if (sl.Go != null)
            {
                if (Application.isPlaying) Destroy(sl.Go);
                else DestroyImmediate(sl.Go);
            }
            _spawned.Remove(cellPos);
        }

        // ── Player torch (Pass 9) ────────────────────────────────

        private void UpdatePlayerTorch(bool isDungeon)
        {
            // Spawn / retire torch based on dungeon state + toggle
            bool wantTorch = PlayerTorchEnabled && isDungeon
                && _playerEntity != null && _activeZone != null;
            if (!wantTorch)
            {
                RetirePlayerTorch();
                return;
            }

            if (_playerTorchGo == null)
            {
                _playerTorchGo = new GameObject("PlayerTorchLight");
                _playerTorchGo.transform.SetParent(_gridParent != null ? _gridParent : transform, false);
                _playerTorchLight = _playerTorchGo.AddComponent<Light2D>();
                _playerTorchLight.lightType = Light2D.LightType.Point;
                _playerTorchLight.color = PlayerTorchColor;
                _playerTorchLight.intensity = PlayerTorchIntensity;
                _playerTorchLight.pointLightInnerRadius = PlayerTorchInnerRadius;
                _playerTorchLight.pointLightOuterRadius = PlayerTorchOuterRadius;
                _playerTorchLight.falloffIntensity = 0.55f;
            }

            // Reposition to player's current cell (cell-center offset)
            var (px, py) = _activeZone.GetEntityPosition(_playerEntity);
            if (px >= 0 && py >= 0)
            {
                _playerTorchGo.transform.localPosition =
                    new Vector3(px + 0.5f, py + 0.5f, 0f);
            }
        }

        private void RetirePlayerTorch()
        {
            if (_playerTorchGo == null) return;
            if (Application.isPlaying) Destroy(_playerTorchGo);
            else DestroyImmediate(_playerTorchGo);
            _playerTorchGo = null;
            _playerTorchLight = null;
        }

        private void RetireAll()
        {
            foreach (var kvp in _spawned)
            {
                if (kvp.Value.Go != null)
                {
                    if (Application.isPlaying) Destroy(kvp.Value.Go);
                    else DestroyImmediate(kvp.Value.Go);
                }
            }
            _spawned.Clear();
        }

        // ── Flicker ───────────────────────────────────────────────

        private void Update()
        {
            if (!LightingEnabled) return;
            float t = Time.time;

            // Player torch flicker — same sin+Perlin recipe as
            // campfires, slightly tighter amplitude so the player's
            // own light feels steadier than environmental fires.
            if (_playerTorchLight != null)
            {
                float sinComp = Mathf.Sin(t * PlayerTorchFlickerSpeed);
                float perlin = Mathf.PerlinNoise(t * 1.7f, 0.31f) * 2f - 1f;
                float flicker = (sinComp * 0.4f + perlin * 0.6f) * PlayerTorchFlickerAmount;
                _playerTorchLight.intensity =
                    PlayerTorchIntensity + PlayerTorchIntensity * flicker;
            }

            foreach (var kvp in _spawned)
            {
                var sl = kvp.Value;
                if (sl.Light == null) continue;
                switch (sl.Kind)
                {
                    case LightKind.Campfire:
                    {
                        // Sin wave + Perlin noise → believable fire flicker
                        float sinComp = Mathf.Sin(t * CampfireFlickerSpeed + sl.Seed * 6.28f);
                        float perlin = Mathf.PerlinNoise(t * 2.0f + sl.Seed, sl.Seed) * 2f - 1f;
                        float flicker = (sinComp * 0.4f + perlin * 0.6f) * CampfireFlickerAmount;
                        sl.Light.intensity = CampfireBaseIntensity + CampfireBaseIntensity * flicker;
                        break;
                    }
                    case LightKind.Shrine:
                    {
                        // Slow sine pulse → mystical breathing
                        float pulse = Mathf.Sin(t * ShrinePulseSpeed + sl.Seed) * ShrinePulseAmount;
                        sl.Light.intensity = ShrineBaseIntensity + ShrineBaseIntensity * pulse;
                        break;
                    }
                    case LightKind.Lantern:
                    {
                        // Steady but with subtle wick variation — gentler
                        // than campfire, more alive than shrine.
                        float jitter = Mathf.PerlinNoise(t * LanternFlickerSpeed + sl.Seed, sl.Seed * 0.7f) * 2f - 1f;
                        sl.Light.intensity =
                            LanternBaseIntensity + LanternBaseIntensity * jitter * LanternFlickerAmount;
                        break;
                    }
                }
            }
        }

        // ── Test seam ─────────────────────────────────────────────

        public int TestOnly_SpawnedCount => _spawned.Count;
        public bool TestOnly_IsLightingEnabled => LightingEnabled;
        public void TestOnly_SetEnabled(bool v) => LightingEnabled = v;
    }
}
