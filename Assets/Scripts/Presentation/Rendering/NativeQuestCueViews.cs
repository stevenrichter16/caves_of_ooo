using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using UnityEngine;
using UnityEngine.Rendering;

namespace CavesOfOoo.Rendering
{
    /// <summary>Two shared, coarse meshes on the presenter's native palette and
    /// fog material. Each handle is owned by an existing actor view: no native
    /// owners, colliders, event subscriptions or per-frame world enumeration.</summary>
    internal sealed class NativeQuestCueViews : IDisposable
    {
        internal sealed class Handle
        {
            public GameObject Root;
            public MeshFilter Filter;
            public QuestCueState State;
            public float Height;
        }
        private readonly NativeZone3DRenderSurface surface;
        private readonly Material palette;
        private readonly int paletteColumns;
        private Mesh available, active;

        public NativeQuestCueViews(NativeZone3DRenderSurface surface, Material palette, int paletteColumns)
        { this.surface=surface; this.palette=palette; this.paletteColumns=paletteColumns; }

        public void Sync(ref Handle handle, Entity owner, GameObject body, Renderer[] bodyRenderers,
            Zone zone, Entity player, bool drawn, bool fullReveal, Camera camera)
        {
            var cell=owner==null?null:zone?.GetEntityCell(owner);
            var state=drawn && cell!=null && (fullReveal || cell.IsVisible)
                ? QuestCueStateQuery.Evaluate(owner,zone,player) : QuestCueState.None;
            if(state==QuestCueState.None)
            {
                if(handle!=null){handle.State=state;if(handle.Root!=null && handle.Root.activeSelf)handle.Root.SetActive(false);}
                return;
            }
            if(handle==null || handle.Root==null)
            {
                EnsureMeshes();
                float top=body.transform.position.y;
                foreach(var renderer in bodyRenderers) if(renderer!=null)top=Mathf.Max(top,renderer.bounds.max.y);
                var root=new GameObject("Native quest cue"){hideFlags=HideFlags.DontSave};
                root.transform.SetParent(body.transform,false);
                var filter=root.AddComponent<MeshFilter>();var rendererComponent=root.AddComponent<MeshRenderer>();
                rendererComponent.sharedMaterial=palette;
                surface.PrepareModel(root,true);
                rendererComponent.shadowCastingMode=ShadowCastingMode.Off;rendererComponent.receiveShadows=false;
                handle=new Handle{Root=root,Filter=filter,Height=top-body.transform.position.y+.38f};
            }
            if(handle.State!=state || handle.Filter.sharedMesh==null)
            {handle.Filter.sharedMesh=state==QuestCueState.Available?available:active;handle.State=state;}
            if(!handle.Root.activeSelf)handle.Root.SetActive(true);
            // Position in world space so native actor facing/scale cannot tip
            // the sign into a body or make it wider than a gameplay cell.
            var inherited=body.transform.lossyScale;
            handle.Root.transform.localScale=new Vector3(Reciprocal(inherited.x),Reciprocal(inherited.y),Reciprocal(inherited.z));
            handle.Root.transform.position=body.transform.position+Vector3.up*handle.Height;
            if(camera!=null)handle.Root.transform.rotation=Quaternion.LookRotation(-camera.transform.forward,camera.transform.up);
        }
        private static float Reciprocal(float value)=>Mathf.Abs(value)>.00001f?1f/value:0f;

        public static bool TryGet(Handle handle, bool presentationVisible, out GameObject root, out string state)
        {
            root=handle?.Root;state=handle==null?"None":handle.State.ToString();
            return presentationVisible && root!=null && root.activeInHierarchy && handle.State!=QuestCueState.None;
        }
        private void EnsureMeshes()
        {
            if(available!=null)return;
            available=Build(false);active=Build(true);
        }
        private Mesh Build(bool ongoing)
        {
            var vertices=new List<Vector3>(96);var uvs=new List<Vector2>(96);var triangles=new List<int>(144);
            if(!ongoing)
            {
                // Camera captures exposed a low-contrast available cue. The
                // existing pale/black swatches form one coarse outlined !,
                // without another material, light or per-owner source mesh.
                Box(new Vector3(0,.125f,-.015f),new Vector3(.28f,.51f,.08f),0,59,vertices,uvs,triangles);
                Box(new Vector3(0,-.265f,-.015f),new Vector3(.26f,.22f,.08f),0,59,vertices,uvs,triangles);
                Box(new Vector3(0,.125f,.035f),new Vector3(.20f,.43f,.07f),0,26,vertices,uvs,triangles);
                Box(new Vector3(0,-.265f,.035f),new Vector3(.18f,.14f,.07f),0,26,vertices,uvs,triangles);
            }
            else
            {
                Box(new Vector3(.125f,.125f,0),new Vector3(.35f,.065f,.075f),-45,13,vertices,uvs,triangles);
                Box(new Vector3(-.125f,.125f,0),new Vector3(.35f,.065f,.075f),45,13,vertices,uvs,triangles);
                Box(new Vector3(.125f,-.125f,0),new Vector3(.35f,.065f,.075f),45,13,vertices,uvs,triangles);
                Box(new Vector3(-.125f,-.125f,0),new Vector3(.35f,.065f,.075f),-45,13,vertices,uvs,triangles);
            }
            var mesh=new Mesh{name=ongoing?"Native quest active diamond":"Native quest available exclamation",hideFlags=HideFlags.DontSave};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private void Box(Vector3 center,Vector3 size,float angle,int swatch,List<Vector3> vertices,List<Vector2> uvs,List<int> triangles)
        {
            Vector3[] corners={new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),
                new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};
            int[] faces={4,5,6,7, 1,0,3,2, 5,1,2,6, 0,4,7,3, 7,6,2,3, 0,1,5,4};
            // Village and regional palettes share these first 64 named colors,
            // but their atlases are 8x8 and 16x8 respectively.
            var rotation=Quaternion.Euler(0,0,angle);var uv=new Vector2((swatch%paletteColumns+.5f)/paletteColumns,(swatch/paletteColumns+.5f)/8f);
            for(int face=0;face<6;face++)
            {
                int start=vertices.Count;
                for(int i=0;i<4;i++){vertices.Add(center+rotation*Vector3.Scale(corners[faces[face*4+i]],size*.5f));uvs.Add(uv);}
                triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);triangles.Add(start);triangles.Add(start+2);triangles.Add(start+3);
            }
        }
        public void Dispose(){Destroy(available);Destroy(active);available=null;active=null;}
        private static void Destroy(UnityEngine.Object value)
        {if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
    }
}
