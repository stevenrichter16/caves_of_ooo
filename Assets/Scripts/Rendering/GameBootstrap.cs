using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo
{
    /// <summary>
    /// Bootstraps the game: loads blueprints, generates a cave zone via ZoneManager,
    /// wires up the renderer, turn manager, and input handler.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        public ZoneRenderer ZoneRenderer;

        private EntityFactory _factory;
        private OverworldZoneManager _zoneManager;
        private Zone _zone;
        private TurnManager _turnManager;
        private Entity _player;

        private void Awake()
        {
            Debug.Log("[Bootstrap] Awake called");
        }

        private void Start()
        {
            Debug.Log("[Bootstrap] Start called");
            try
            {
                DoStart();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bootstrap] FATAL exception during Start:\n{ex}");
            }
        }

        private void DoStart()
        {
            Debug.Log("[Bootstrap] Step 1/9: Initializing factions...");
            FactionManager.Initialize();

            Debug.Log("[Bootstrap] Step 2/9: Initializing mutations...");
            MutationRegistry.EnsureInitialized();

            // Wire combat messages to Unity console
            MessageLog.OnMessage = msg => Debug.Log($"[Combat] {msg}");

            Debug.Log("[Bootstrap] Step 3/9: Creating EntityFactory...");
            _factory = new EntityFactory();

            Debug.Log("[Bootstrap] Step 4/9: Loading blueprints...");
            TextAsset blueprintAsset = Resources.Load<TextAsset>("Blueprints/Objects");
            if (blueprintAsset == null)
            {
                Debug.LogError("[Bootstrap] FAILED: Could not load Blueprints/Objects.json from Resources");
                return;
            }
            _factory.LoadBlueprints(blueprintAsset.text);
            Debug.Log($"[Bootstrap] Loaded {_factory.Blueprints.Count} blueprints");

            Debug.Log("[Bootstrap] Step 5/9: Generating starting zone...");
            _zoneManager = new OverworldZoneManager(_factory);
            _zone = _zoneManager.GetZone("Overworld.5.5");
            _zoneManager.SetActiveZone(_zone);
            if (_zone == null)
            {
                Debug.LogError("[Bootstrap] FAILED: Zone generation returned null");
                return;
            }
            Debug.Log($"[Bootstrap] Zone generated: {_zone.EntityCount} entities");

            Debug.Log("[Bootstrap] Step 6/9: Creating player...");
            _player = _factory.CreateEntity("Player");
            if (_player == null)
            {
                Debug.LogError("[Bootstrap] FAILED: Player entity creation returned null");
                return;
            }
            var playerBody = _player.GetPart<Body>();
            Debug.Log($"[Bootstrap] Player created. Has Body part: {playerBody != null}, Body initialized: {playerBody?.GetBody() != null}");
            PlacePlayerInOpenCell();
            SpawnDebugWeaponNearPlayer();

            Debug.Log("[Bootstrap] Step 7/9: Setting up turns...");
            _turnManager = new TurnManager();
            RegisterCreaturesForTurns();

            Debug.Log("[Bootstrap] Step 8/9: Wiring renderer...");
            if (ZoneRenderer != null)
            {
                ZoneRenderer.SetZone(_zone);
                Debug.Log("[Bootstrap] ZoneRenderer wired successfully");
            }
            else
            {
                Debug.LogError("[Bootstrap] FAILED: ZoneRenderer not assigned in Inspector!");
            }

            // Wire up camera follow on the main camera
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[Bootstrap] FAILED: Camera.main is null (no camera tagged MainCamera)");
            }
            else
            {
                var cameraFollow = cam.GetComponent<CameraFollow>();
                if (cameraFollow == null)
                    cameraFollow = cam.gameObject.AddComponent<CameraFollow>();
                cameraFollow.Player = _player;
                cameraFollow.CurrentZone = _zone;
                cameraFollow.SnapToPlayer();

                // Wire up input handler
                Debug.Log("[Bootstrap] Step 9/9: Wiring input...");
                var inputHandler = GetComponent<InputHandler>();
                if (inputHandler == null)
                    inputHandler = gameObject.AddComponent<InputHandler>();
                inputHandler.PlayerEntity = _player;
                inputHandler.CurrentZone = _zone;
                inputHandler.TurnManager = _turnManager;
                inputHandler.ZoneRenderer = ZoneRenderer;
                inputHandler.ZoneManager = _zoneManager;
                inputHandler.WorldMap = _zoneManager.WorldMap;
                inputHandler.CameraFollow = cameraFollow;

                // Wire inventory UI (shares tilemap with zone renderer)
                var inventoryUI = GetComponent<InventoryUI>();
                if (inventoryUI == null)
                    inventoryUI = gameObject.AddComponent<InventoryUI>();
                if (ZoneRenderer != null)
                    inventoryUI.Tilemap = ZoneRenderer.GetComponent<Tilemap>();
                inputHandler.InventoryUI = inventoryUI;
            }

            // Start the turn loop
            _turnManager.ProcessUntilPlayerTurn();

            Debug.Log($"[Bootstrap] DONE. Zone has {_zone.EntityCount} entities. WASD/arrows to move.");
        }

        /// <summary>
        /// Debug: spawn a weapon next to the player so equipment-drop-on-dismember can be tested.
        /// </summary>
        private void SpawnDebugWeaponNearPlayer()
        {
            var pos = _zone.GetEntityPosition(_player);
            if (pos.x < 0) return;

            // Try adjacent cells for an open spot
            int[] dx = { 1, -1, 0, 0, 1, -1, 1, -1 };
            int[] dy = { 0, 0, 1, -1, 1, -1, -1, 1 };
            for (int i = 0; i < dx.Length; i++)
            {
                int nx = pos.x + dx[i];
                int ny = pos.y + dy[i];
                if (!_zone.InBounds(nx, ny)) continue;
                var cell = _zone.GetCell(nx, ny);
                if (cell != null && cell.IsPassable())
                {
                    var weapon = _factory.CreateEntity("Dagger");
                    if (weapon != null)
                    {
                        _zone.AddEntity(weapon, nx, ny);
                        Debug.Log($"[Bootstrap] Debug: Spawned {weapon.GetDisplayName()} at ({nx},{ny}) near player");
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Find an open cell near the center of the zone to place the player.
        /// Searches outward from center in a spiral.
        /// </summary>
        private void PlacePlayerInOpenCell()
        {
            int cx = Zone.Width / 2;
            int cy = Zone.Height / 2;

            for (int radius = 0; radius < Math.Max(Zone.Width, Zone.Height); radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                        int x = cx + dx;
                        int y = cy + dy;
                        if (!_zone.InBounds(x, y)) continue;
                        var cell = _zone.GetCell(x, y);
                        if (cell != null && cell.IsPassable())
                        {
                            _zone.AddEntity(_player, x, y);
                            return;
                        }
                    }
                }
            }

            // Fallback: place at center regardless
            _zone.AddEntity(_player, cx, cy);
        }

        /// <summary>
        /// Register all creatures in the zone with the turn manager.
        /// </summary>
        private void RegisterCreaturesForTurns()
        {
            var creatures = _zone.GetEntitiesWithTag("Creature");
            foreach (var creature in creatures)
            {
                _turnManager.AddEntity(creature);

                // Wire BrainPart with zone and RNG so AI can function
                var brain = creature.GetPart<BrainPart>();
                if (brain != null)
                {
                    brain.CurrentZone = _zone;
                    brain.Rng = new System.Random();
                }
            }
            Debug.Log($"GameBootstrap: Registered {creatures.Count} creatures for turns");
        }
    }
}
