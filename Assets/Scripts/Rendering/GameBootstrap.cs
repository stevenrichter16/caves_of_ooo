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
        private ZoneManager _zoneManager;
        private Zone _zone;
        private TurnManager _turnManager;
        private Entity _player;

        private void Start()
        {
            // Load blueprints
            _factory = new EntityFactory();
            TextAsset blueprintAsset = Resources.Load<TextAsset>("Blueprints/Objects");
            if (blueprintAsset != null)
            {
                _factory.LoadBlueprints(blueprintAsset.text);
            }
            else
            {
                Debug.LogError("GameBootstrap: Could not load Blueprints/Objects.json");
                return;
            }

            // Create zone manager and generate a cave zone
            _zoneManager = new ZoneManager(_factory);
            _zone = _zoneManager.GetZone("CaveLevel_1");
            _zoneManager.SetActiveZone(_zone);

            if (_zone == null)
            {
                Debug.LogError("GameBootstrap: Failed to generate zone");
                return;
            }

            // Place player in a passable cell near center
            _player = _factory.CreateEntity("Player");
            PlacePlayerInOpenCell();

            // Set up turn manager
            _turnManager = new TurnManager();
            RegisterCreaturesForTurns();

            // Wire up the renderer
            if (ZoneRenderer != null)
            {
                ZoneRenderer.SetZone(_zone);
            }
            else
            {
                Debug.LogError("GameBootstrap: ZoneRenderer not assigned!");
            }

            // Wire up input handler
            var inputHandler = GetComponent<InputHandler>();
            if (inputHandler == null)
                inputHandler = gameObject.AddComponent<InputHandler>();
            inputHandler.PlayerEntity = _player;
            inputHandler.CurrentZone = _zone;
            inputHandler.TurnManager = _turnManager;
            inputHandler.ZoneRenderer = ZoneRenderer;

            // Start the turn loop
            _turnManager.ProcessUntilPlayerTurn();

            Debug.Log($"GameBootstrap: Zone created with {_zone.EntityCount} entities. Use WASD/arrows to move.");
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
            }
            Debug.Log($"GameBootstrap: Registered {creatures.Count} creatures for turns");
        }
    }
}
