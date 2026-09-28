using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace CavesOfOoo.Rendering
{
    /// <summary>One quiet cell cue on the existing XY world/composite layer.
    /// No collider, world icon grid, shadow, animation, camera or native model change.</summary>
    public sealed class WorldAffordanceRenderer : IDisposable
    {
        private GameObject root;
        private readonly LineRenderer left,right;
        private readonly Tilemap tilemap;
        private Material material;
        public bool IsVisible => root!=null&&root.activeSelf&&left!=null&&right!=null&&left.enabled&&right.enabled;
        public Vector2Int CurrentCell {get;private set;}=new Vector2Int(-1,-1);
        public WorldAffordanceRenderer(Transform parent,Tilemap tilemap,int renderLayer)
        {
            this.tilemap=tilemap;
            var shader=Shader.Find("Sprites/Default");
            if(parent==null||shader==null)return;
            root=new GameObject("World action cue"){hideFlags=HideFlags.DontSave};root.transform.SetParent(parent,false);root.layer=renderLayer;
            material=new Material(shader){name="Owned world action cue",hideFlags=HideFlags.DontSave};
            left=Make("lower corner",renderLayer);right=Make("upper corner",renderLayer);Clear();
        }
        private LineRenderer Make(string name,int layer)
        {
            var go=new GameObject(name){hideFlags=HideFlags.DontSave};go.transform.SetParent(root.transform,false);go.layer=layer;
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.positionCount=3;
            line.useWorldSpace=true;line.loop=false;line.widthMultiplier=.08f;line.numCapVertices=0;line.numCornerVertices=0;line.sortingOrder=7;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
            line.startColor=line.endColor=new Color(.48f,.65f,.60f,.85f);return line;
        }
        public void Refresh(Entity actor,Zone zone,WorldAffordance? cue)
        {
            if(root==null||left==null||right==null||!cue.HasValue||!WorldAffordanceQuery.Current(actor,zone,cue.Value)){Clear();return;}
            var cell=cue.Value.Cell;CurrentCell=new Vector2Int(cell.X,cell.Y);
            var tile=new Vector3Int(cell.X,Zone.Height-1-cell.Y,0);
            var min=tilemap!=null?tilemap.CellToWorld(tile):new Vector3(tile.x,tile.y,0);
            var max=tilemap!=null?tilemap.CellToWorld(tile+new Vector3Int(1,1,0)):min+Vector3.one;
            float x0=Mathf.Lerp(min.x,max.x,.12f),x1=Mathf.Lerp(min.x,max.x,.88f);
            float y0=Mathf.Lerp(min.y,max.y,.12f),y1=Mathf.Lerp(min.y,max.y,.88f);
            float dx=(max.x-min.x)*.19f,dy=(max.y-min.y)*.19f;
            left.SetPosition(0,new Vector3(x0,y0+dy,-.11f));left.SetPosition(1,new Vector3(x0,y0,-.11f));left.SetPosition(2,new Vector3(x0+dx,y0,-.11f));
            right.SetPosition(0,new Vector3(x1-dx,y1,-.11f));right.SetPosition(1,new Vector3(x1,y1,-.11f));right.SetPosition(2,new Vector3(x1,y1-dy,-.11f));
            root.SetActive(true);left.enabled=right.enabled=true;
        }
        public void Clear(){CurrentCell=new Vector2Int(-1,-1);if(root!=null)root.SetActive(false);if(left!=null)left.enabled=false;if(right!=null)right.enabled=false;}
        private static void DestroyOwned(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        public void Dispose(){Clear();DestroyOwned(root);DestroyOwned(material);root=null;material=null;}
    }
}
