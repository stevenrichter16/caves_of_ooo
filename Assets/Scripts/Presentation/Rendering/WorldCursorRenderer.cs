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
        private readonly LineRenderer _throwPath;
        private readonly LineRenderer[] _throwAreas=new LineRenderer[11];
        private ThrowPreview _drawnThrowPreview;
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
            _throwPath=MakeThrowLine(parent,renderLayer,"ThrowRoute",false);
            for(int i=0;i<_throwAreas.Length;i++)_throwAreas[i]=MakeThrowLine(parent,renderLayer,"ThrowArea"+i,true);
        }

        LineRenderer MakeThrowLine(Transform parent,int layer,string name,bool loop)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.layer=layer;
            var line=go.AddComponent<LineRenderer>();line.loop=loop;line.useWorldSpace=true;line.positionCount=loop?4:0;
            line.widthMultiplier=loop?.045f:.035f;line.sortingOrder=6;line.sharedMaterial=_lineRenderer.sharedMaterial;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;line.enabled=false;return line;
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
            if(state.Mode==WorldCursorMode.ThrowTarget && state.ThrowPreview!=null)
            {
                DrawThrowPreview(state,player);return;
            }
            HideThrowPreview();
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

        private void DrawOutline(LineRenderer line,int x,int y,Color color,float inset=0)
        {
            var cell=new Vector3Int(x,Zone.Height-1-y,0);
            var min=_tilemap!=null?_tilemap.CellToWorld(cell):new Vector3(x,Zone.Height-1-y,0);
            var max=_tilemap!=null?_tilemap.CellToWorld(cell+new Vector3Int(1,1,0)):min+Vector3.one;
            var boundsMin=min;min=Vector3.Lerp(boundsMin,max,inset);max=Vector3.Lerp(max,boundsMin,inset);
            line.startColor=color;line.endColor=color;line.enabled=true;
            line.SetPosition(0,new Vector3(min.x,min.y,-.15f));line.SetPosition(1,new Vector3(max.x,min.y,-.15f));
            line.SetPosition(2,new Vector3(max.x,max.y,-.15f));line.SetPosition(3,new Vector3(min.x,max.y,-.15f));
        }

        public void Clear()
        {
            HideThrowPreview();
            _vaultActor=null;_vaultVersion=-1;LandingCell=new Vector2Int(-1,-1);
            if(_landingRenderer!=null)_landingRenderer.enabled=false;
            CurrentCell = new Vector2Int(-1, -1);
            CurrentColor = QudColorParser.Gray;
            if (_lineRenderer != null)
                _lineRenderer.enabled = false;
        }

        public static Color GetCursorColor(Cell cell, Entity player)
        {
            if (cell == null || !cell.IsVisible || !cell.Explored)
                return QudColorParser.Gray;

            bool hasInteractable = false;
            bool hasTakeable = false;
            bool hasVisible = false;
            bool visibleSolid=false;

            for (int i = 0; i < cell.Objects.Count; i++)
            {
                Entity entity = cell.Objects[i];
                if (entity == null || entity.GetPart<RenderPart>()?.Visible!=true)
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
                if(entity.HasTag("Solid") || physics?.Solid==true || entity.GetPart<DoorPart>()?.IsClosed==true
                    || entity.GetPart<SealedLibraryBarrierPart>()?.IsClosed==true)visibleSolid=true;
                if (physics != null && physics.Takeable)
                    hasTakeable = true;
            }

            if (hasInteractable)
                return QudColorParser.BrightCyan;
            if (hasTakeable)
                return QudColorParser.BrightYellow;
            if (!hasVisible || visibleSolid)
                return QudColorParser.Gray;

            return QudColorParser.White;
        }

        void HideThrowPreview()
        {
            _drawnThrowPreview=null;if(_throwPath!=null)_throwPath.enabled=false;
            foreach(var line in _throwAreas)if(line!=null)line.enabled=false;
        }
        Vector3 Center(int x,int y)
        {
            var point=new Vector3Int(x,Zone.Height-1-y,0);
            var min=_tilemap!=null?_tilemap.CellToWorld(point):new Vector3(point.x,point.y,0);
            var max=_tilemap!=null?_tilemap.CellToWorld(point+new Vector3Int(1,1,0)):min+Vector3.one;
            return new Vector3((min.x+max.x)*.5f,(min.y+max.y)*.5f,-.11f);
        }
        void DrawThrowPreview(WorldCursorState state,Entity player)
        {
            var preview=state.ThrowPreview;if(_drawnThrowPreview==preview)return;
            HideThrowPreview();_drawnThrowPreview=preview;_vaultActor=null;
            var trace=preview.Trace;CurrentCell=new Vector2Int(state.X,state.Y);
            CurrentColor=state.Zone.GetCell(state.X,state.Y).IsVisible?GetCursorColor(state.Zone.GetCell(state.X,state.Y),player):QudColorParser.Gray;
            if(state.ThrowGroundAim && state.Zone.GetCell(state.X,state.Y).IsVisible)CurrentColor=QudColorParser.BrightYellow;
            DrawOutline(_lineRenderer,state.X,state.Y,CurrentColor);
            _throwPath.positionCount=trace.Path.Count+1;_throwPath.SetPosition(0,Center(state.AnchorX,state.AnchorY));
            for(int i=0;i<trace.Path.Count;i++)_throwPath.SetPosition(i+1,Center(trace.Path[i].X,trace.Path[i].Y));
            _throwPath.startColor=_throwPath.endColor=trace.Uncertain?QudColorParser.Gray:QudColorParser.BrightCyan;
            _throwPath.enabled=trace.Path.Count>0;
            _landingRenderer.enabled=trace.RestCell!=null && !trace.Uncertain;
            LandingCell=_landingRenderer.enabled?new Vector2Int(trace.RestCell.X,trace.RestCell.Y):new Vector2Int(-1,-1);
            if(_landingRenderer.enabled)DrawOutline(_landingRenderer,trace.RestCell.X,trace.RestCell.Y,QudColorParser.BrightYellow);
            int index=0;
            foreach(var cell in preview.Splash)DrawOutline(_throwAreas[index++],cell.X,cell.Y,cell.IsVisible && cell.Explored?QudColorParser.BrightCyan:QudColorParser.Gray,.22f);
            if(!trace.Uncertain && trace.ImpactCell!=null && (trace.HitEntity!=null || trace.BlockedBySolid || trace.AmbiguousContact))DrawOutline(_throwAreas[9],trace.ImpactCell.X,trace.ImpactCell.Y,QudColorParser.BrightRed,.12f);
            if(preview.ConditionalPush!=null)DrawOutline(_throwAreas[10],preview.ConditionalPush.X,preview.ConditionalPush.Y,QudColorParser.BrightMagenta);
        }
    }
}
