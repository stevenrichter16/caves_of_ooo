using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;
namespace CavesOfOoo.Editor
{
    /// <summary>Repeatable offline authoring of coarse, two-color plant silhouettes.
    /// No scene edits and no per-cube GameObjects in the resulting prefabs.</summary>
    public static class SpreadVoxelKitBuilder
    {
        private const string Folder="Assets/Resources/SpreadVoxel3D";
        private static readonly List<Vector3> vertices=new List<Vector3>();
        private static readonly List<Vector2> uvs=new List<Vector2>();
        private static readonly List<Color> colors=new List<Color>();
        private static readonly List<int> triangles=new List<int>();
        private static Mesh cube;
        public static void Run()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            if(ring==null)throw new Exception("Native material library missing.");
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=primitive.GetComponent<MeshFilter>().sharedMesh;
            var entries=new List<SpreadVoxelLibrary.Entry>();
            try
            {
                foreach(string family in new[]{"hedge","barley","flowers","reeds"})for(int variant=0;variant<4;variant++)
                {
                    string id="spread-"+family+"-"+variant;vertices.Clear();uvs.Clear();colors.Clear();triangles.Clear();
                    if(family=="hedge")
                    {
                        Box(new Vector3(0,.22f,0),new Vector3(.72f,.44f,.48f),18);
                        Box(new Vector3(0,.66f,0),new Vector3(.92f,.48f,.64f),16);
                        Box(new Vector3((variant-1.5f)*.08f,.95f,0),new Vector3(.64f,.18f,.48f),16);
                    }
                    else for(int i=0;i<3;i++)
                    {
                        float x=(i-1)*.24f,z=((variant+i)%3-1)*.19f;
                        float h=family=="reeds"?.72f+(i+variant)%3*.12f:family=="barley"?.38f+(i+variant)%3*.07f:.25f+(i+variant)%2*.08f;
                        if(variant==3)h+=.08f;
                        int stem=family=="barley"?14:16,head=family=="flowers"?27:family=="reeds"?8:21;
                        Box(new Vector3(x,h*.5f,z),new Vector3(.09f,h,.09f),stem);
                        if(family=="flowers")
                        {
                            Box(new Vector3(x,h,z),new Vector3(.28f,.10f,.15f),head);
                            Box(new Vector3(x,h,z),new Vector3(.15f,.10f,.28f),head);
                        }
                        else Box(new Vector3(x,h+.06f,z),new Vector3(.15f,family=="reeds"?.26f:.20f,.15f),head);
                    }
                    string meshPath=Folder+"/"+id+".asset";
                    var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(mesh==null){mesh=new Mesh{name=id};AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();
                    mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                    var root=new GameObject(id);root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=ring.WorldMaterial;
                    GameObject prefab;
                    try{prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+id+".prefab");}finally{UnityEngine.Object.DestroyImmediate(root);}
                    entries.Add(new SpreadVoxelLibrary.Entry{Id=id,Prefab=prefab,Mesh=mesh,
                        Spec=new SpawnRing3DCatalog.Model{id=id,path=Folder+"/"+id+".prefab",kind="entity",materialFamily="ring-palette",rigFamily="none",
                            boundsCenter=mesh.bounds.center,boundsSize=mesh.bounds.size,triangles=triangles.Count/3,clips=Array.Empty<string>(),sockets=Array.Empty<string>()}});
                }
                string path=Folder+"/Library.asset";var library=AssetDatabase.LoadAssetAtPath<SpreadVoxelLibrary>(path);
                if(library==null){library=ScriptableObject.CreateInstance<SpreadVoxelLibrary>();AssetDatabase.CreateAsset(library,path);}
                library.Entries=entries.ToArray();library.Validate();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
            }
            finally{UnityEngine.Object.DestroyImmediate(primitive);}
        }
        private static void Box(Vector3 center,Vector3 size,int palette)
        {
            int offset=vertices.Count;var uv=new Vector2((palette%16+.5f)/16f,(palette/16+.5f)/8f);
            // Palette UV is constant on every face: exactly one swatch per block.
            foreach(var v in cube.vertices){vertices.Add(center+Vector3.Scale(v,size));uvs.Add(uv);colors.Add(Color.white);}
            foreach(int t in cube.triangles)triangles.Add(offset+t);
        }
    }
}
