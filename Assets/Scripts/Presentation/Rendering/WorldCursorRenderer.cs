using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Presentation-only world cursor outline. Input and gameplay state stay elsewhere.
    /// </summary>
    public sealed class WorldCursorRenderer
    {
        private readonly LineRenderer _lineRenderer;
        private readonly Tilemap _tilemap;
        private Zone _currentZone;

        public bool IsVisible => _lineRenderer != null && _lineRenderer.enabled;
        public Color CurrentColor { get; private set; } = QudColorParser.Gray;
        public Vector2Int CurrentCell { get; private set; } = new Vector2Int(-1, -1);

        public WorldCursorRenderer(Transform parent, Tilemap tilemap, int renderLayer)
        {
            _tilemap = tilemap;
            var go = new GameObject("WorldCursor");
            go.transform.SetParent(parent, false);
            go.layer = renderLayer;

            _lineRenderer = go.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 4;
            _lineRenderer.loop = true;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.widthMultiplier = 0.08f;
            _lineRenderer.numCapVertices = 0;
            _lineRenderer.numCornerVertices = 0;
            _lineRenderer.sortingOrder = 4;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.enabled = false;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                _lineRenderer.material = new Material(shader);
        }

        public void SetZone(Zone zone)
        {
            _currentZone = zone;
            Clear();
        }

        public void SetCursor(WorldCursorState state, Entity player)
        {
            if (_lineRenderer == null ||
                state == null ||
                !state.Active ||
                state.Zone == null ||
                state.Zone != _currentZone ||
                !_currentZone.InBounds(state.X, state.Y))
            {
                Clear();
                return;
            }

            Cell cell = _currentZone.GetCell(state.X, state.Y);
            CurrentCell = new Vector2Int(state.X, state.Y);
            CurrentColor = GetCursorColor(cell, player);

            _lineRenderer.startColor = CurrentColor;
            _lineRenderer.endColor = CurrentColor;
            _lineRenderer.enabled = true;

            Vector3Int tileCell = new Vector3Int(state.X, Zone.Height - 1 - state.Y, 0);
            Vector3 worldMin = _tilemap != null
                ? _tilemap.CellToWorld(tileCell)
                : new Vector3(state.X, Zone.Height - 1 - state.Y, 0f);
            Vector3 worldMax = _tilemap != null
                ? _tilemap.CellToWorld(tileCell + new Vector3Int(1, 1, 0))
                : worldMin + Vector3.one;

            float minX = worldMin.x;
            float maxX = worldMax.x;
            float minY = worldMin.y;
            float maxY = worldMax.y;

            _lineRenderer.SetPosition(0, new Vector3(minX, minY, -0.1f));
            _lineRenderer.SetPosition(1, new Vector3(maxX, minY, -0.1f));
            _lineRenderer.SetPosition(2, new Vector3(maxX, maxY, -0.1f));
            _lineRenderer.SetPosition(3, new Vector3(minX, maxY, -0.1f));
        }

        public void Clear()
        {
            CurrentCell = new Vector2Int(-1, -1);
            CurrentColor = QudColorParser.Gray;
            if (_lineRenderer != null)
                _lineRenderer.enabled = false;
        }

        public static Color GetCursorColor(Cell cell, Entity player)
        {
            if (cell == null)
                return QudColorParser.Gray;

            bool hasInteractable = false;
            bool hasTakeable = false;
            bool hasVisible = false;

            for (int i = 0; i < cell.Objects.Count; i++)
            {
                Entity entity = cell.Objects[i];
                if (entity == null)
                    continue;

                RenderPart render = entity.GetPart<RenderPart>();
                if (render != null && render.Visible)
                    hasVisible = true;

                if (entity.HasTag("Creature") && entity != player && player != null && FactionManager.IsHostile(player, entity))
                    return QudColorParser.BrightRed;

                if (entity.GetPart<ContainerPart>() != null ||
                    entity.GetPart<StairsDownPart>() != null ||
                    entity.GetPart<StairsUpPart>() != null)
                {
                    hasInteractable = true;
                }

                PhysicsPart physics = entity.GetPart<PhysicsPart>();
                if (physics != null && physics.Takeable)
                    hasTakeable = true;
            }

            if (hasInteractable)
                return QudColorParser.BrightCyan;
            if (hasTakeable)
                return QudColorParser.BrightYellow;
            if (!hasVisible || cell.IsSolid())
                return QudColorParser.Gray;

            return QudColorParser.White;
        }
    }
}
