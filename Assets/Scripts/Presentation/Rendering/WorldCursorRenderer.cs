using CavesOfOoo.Core;
using CavesOfOoo.Skills;
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
        private readonly LineRenderer _landingRenderer;
        public Vector2Int LandingCell { get; private set; } = new Vector2Int(-1,-1);
        int _vaultVersion=-1,_vaultTick=-1;Entity _vaultActor;bool _vaultVisible;
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
            _lineRenderer.sortingOrder = 7;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;
            _lineRenderer.enabled = false;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                _lineRenderer.material = new Material(shader);
            var landingGo=new GameObject("VaultLanding");landingGo.transform.SetParent(parent,false);landingGo.layer=renderLayer;
            _landingRenderer=landingGo.AddComponent<LineRenderer>();
            _landingRenderer.positionCount=4;_landingRenderer.loop=true;_landingRenderer.useWorldSpace=true;
            _landingRenderer.widthMultiplier=0.12f;_landingRenderer.sortingOrder=8;
            _landingRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;_landingRenderer.receiveShadows=false;
            _landingRenderer.sharedMaterial=_lineRenderer.sharedMaterial;_landingRenderer.enabled=false;
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
            if(state.Mode==WorldCursorMode.AbilityTarget)
            {
                _lineRenderer.widthMultiplier = 0.12f;
                int tick=TurnManager.Active?.TickCount??0;
                if(_vaultActor==player&&CurrentCell.x==state.X&&CurrentCell.y==state.Y&&_vaultVersion==_currentZone.EntityVersion
                    &&_vaultTick==tick&&_vaultVisible==cell.IsVisible)return;
                _vaultActor=player;_vaultVersion=_currentZone.EntityVersion;_vaultTick=tick;_vaultVisible=cell.IsVisible;
                CurrentCell=new Vector2Int(state.X,state.Y);
                Cell landing=null;Entity target=null;string reason=null;
                bool valid=cell.IsVisible&&VaultTargeting.TryPlan(player,_currentZone,state.X,state.Y,out landing,out target,out reason);
                CurrentColor=valid?(target==null?QudColorParser.BrightYellow:QudColorParser.BrightRed):QudColorParser.Gray;
                DrawOutline(_lineRenderer,state.X,state.Y,CurrentColor);
                LandingCell=valid?new Vector2Int(landing.X,landing.Y):new Vector2Int(-1,-1);
                _landingRenderer.enabled=valid&&target!=null;
                if(_landingRenderer.enabled)DrawOutline(_landingRenderer,landing.X,landing.Y,QudColorParser.BrightYellow);
                return;
            }
            _lineRenderer.widthMultiplier = 0.08f;
            _vaultActor=null;_landingRenderer.enabled=false;LandingCell=new Vector2Int(-1,-1);
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

        private void DrawOutline(LineRenderer line,int x,int y,Color color)
        {
            var cell=new Vector3Int(x,Zone.Height-1-y,0);
            var min=_tilemap!=null?_tilemap.CellToWorld(cell):new Vector3(x,Zone.Height-1-y,0);
            var max=_tilemap!=null?_tilemap.CellToWorld(cell+new Vector3Int(1,1,0)):min+Vector3.one;
            line.startColor=color;line.endColor=color;line.enabled=true;
            line.SetPosition(0,new Vector3(min.x,min.y,-.15f));line.SetPosition(1,new Vector3(max.x,min.y,-.15f));
            line.SetPosition(2,new Vector3(max.x,max.y,-.15f));line.SetPosition(3,new Vector3(min.x,max.y,-.15f));
        }

        public void Clear()
        {
            _vaultActor=null;_vaultVersion=-1;LandingCell=new Vector2Int(-1,-1);
            if(_landingRenderer!=null)_landingRenderer.enabled=false;
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
