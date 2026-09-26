#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;
namespace CavesOfOoo.Editor
{
    /// <summary>Explicit additive import of the deterministic cuboid source kit.
    /// Owns only ReferenceGlade3D; never edits a loaded scene or shared material.</summary>
    public static class ReferenceGladeVoxelKitBuilder
    {
        const string Folder="Assets/Resources/ReferenceGlade3D",Source="ArtSource/ReferenceGlade3D/kit.json";
        [Serializable]sealed class Box{public Vector3 center,size;public int color;}
        [Serializable]sealed class Model{public string id,family;public int variant;public Box[] boxes;}
        [Serializable]sealed class Kit{public int schemaVersion;public string[] palette;public Model[] models;}
        [Serializable]sealed class Report{public string status,sourceSha256,error;public int models,boxes,triangles;public string[] palette,actorPaintAssets;}
        public static void Run(string reportPath="Docs/Verification/DensityCompletion/ReferenceGlade/Art/native-kit-import.json")
        {
            var report=new Report();GameObject primitive=null;
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Wait for clean Edit mode before importing the glade kit.");
                string json=File.ReadAllText(Source);var kit=JsonUtility.FromJson<Kit>(json);
                using(var sha=SHA256.Create())report.sourceSha256=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json))).Replace("-","").ToLowerInvariant();
                if(kit==null||kit.schemaVersion!=1||kit.palette==null||kit.palette.Length!=24||kit.models==null||kit.models.Length!=40)throw new InvalidOperationException("Incomplete reference glade source kit.");
                var colors=new Color[24];for(int i=0;i<24;i++)if(!ColorUtility.TryParseHtmlString(kit.palette[i],out colors[i]))throw new InvalidOperationException("Invalid palette color.");
                var expected=new HashSet<string>(StringComparer.Ordinal);
                foreach(string family in ReferenceGladeVoxelLibrary.Families)for(int v=0;v<4;v++)expected.Add(ReferenceGladeVoxelLibrary.ModelId(family,v));
                foreach(var model in kit.models)
                {
                    if(model==null||model.id!=ReferenceGladeVoxelLibrary.ModelId(model.family,model.variant)||!expected.Remove(model.id)||model.boxes==null||model.boxes.Length==0)throw new InvalidOperationException("Invalid model identity.");
                    foreach(var box in model.boxes)if(box==null||box.color<0||box.color>=24||!Finite(box.center)||!Finite(box.size)||box.size.x<=0||box.size.y<=0||box.size.z<=0||Mathf.Abs(box.center.x)+box.size.x*.5f>.501f||Mathf.Abs(box.center.z)+box.size.z*.5f>.501f)throw new InvalidOperationException("Invalid glade geometry.");
                }
                if(expected.Count!=0)throw new InvalidOperationException("Missing glade families.");
                var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);if(ring==null||ring.WorldMaterial==null)throw new InvalidOperationException("Native ring shader material required.");
                Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
                string palettePath=Folder+"/Palette.asset";var palette=AssetDatabase.LoadAssetAtPath<Texture2D>(palettePath);
                if(palette==null){RefuseWrongType(palettePath);palette=new Texture2D(24,1,TextureFormat.RGBA32,false){name="Reference glade palette"};AssetDatabase.CreateAsset(palette,palettePath);}
                if(palette.height!=1 || (palette.width!=16 && palette.width!=24))throw new InvalidOperationException("Existing glade palette dimensions changed.");
                if(palette.width!=24 && !palette.Reinitialize(24,1,TextureFormat.RGBA32,false))throw new InvalidOperationException("Cannot resize owned glade palette.");
                palette.filterMode=FilterMode.Point;palette.wrapMode=TextureWrapMode.Clamp;palette.SetPixels(colors);palette.Apply(false,false);EditorUtility.SetDirty(palette);
                string materialPath=Folder+"/Palette.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(material==null){RefuseWrongType(materialPath);material=new Material(ring.WorldMaterial){name="Reference glade native palette"};AssetDatabase.CreateAsset(material,materialPath);}
                material.SetTexture("_BaseMap",palette);material.SetColor("_BaseColor",Color.white);material.enableInstancing=true;EditorUtility.SetDirty(material);
                primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=primitive.GetComponent<MeshFilter>().sharedMesh;var cv=cube.vertices;var ct=cube.triangles;
                var entries=new List<ReferenceGladeVoxelLibrary.Entry>(40);
                foreach(var model in kit.models)
                {
                    var vertices=new List<Vector3>(model.boxes.Length*24);var uv=new List<Vector2>(model.boxes.Length*24);var triangles=new List<int>(model.boxes.Length*36);
                    foreach(var box in model.boxes){int offset=vertices.Count;foreach(var v in cv){vertices.Add(box.center+Vector3.Scale(v,box.size));uv.Add(new Vector2((box.color+.5f)/24f,.5f));}foreach(int t in ct)triangles.Add(offset+t);}
                    string meshPath=Folder+"/"+model.id+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(mesh==null){RefuseWrongType(meshPath);mesh=new Mesh{name=model.id};AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();
                    mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                    string prefabPath=Folder+"/"+model.id+".prefab";var root=new GameObject(model.id);GameObject prefab;
                    try{root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=material;prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);if(prefab==null)throw new InvalidOperationException("Glade prefab save failed.");}
                    finally{UnityEngine.Object.DestroyImmediate(root);}
                    entries.Add(new ReferenceGladeVoxelLibrary.Entry{Id=model.id,Prefab=prefab,Mesh=mesh,Spec=new SpawnRing3DCatalog.Model{id=model.id,path=prefabPath,kind=model.family=="ground"?"ground":"entity",materialFamily="reference-glade-palette",rigFamily="none",boundsCenter=mesh.bounds.center,boundsSize=mesh.bounds.size,triangles=triangles.Count/3,clips=Array.Empty<string>(),sockets=Array.Empty<string>()}});
                    report.boxes+=model.boxes.Length;report.triangles+=triangles.Count/3;
                }
                var actorPaints=BuildActorPaints(ring);
                report.actorPaintAssets=actorPaints.Select(p=>AssetDatabase.GetAssetPath(p.Painted)).ToArray();
                string libraryPath=Folder+"/Library.asset";var library=AssetDatabase.LoadAssetAtPath<ReferenceGladeVoxelLibrary>(libraryPath);
                if(library==null){RefuseWrongType(libraryPath);library=ScriptableObject.CreateInstance<ReferenceGladeVoxelLibrary>();AssetDatabase.CreateAsset(library,libraryPath);}
                library.Entries=entries.ToArray();library.Material=material;library.ActorPaints=actorPaints;library.Validate();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();report.status="passed";report.models=entries.Count;report.palette=kit.palette;
            }
            catch(Exception error){report.status="failed";report.error=error.ToString();throw;}
            finally{if(primitive!=null)UnityEngine.Object.DestroyImmediate(primitive);Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));File.WriteAllText(reportPath,JsonUtility.ToJson(report,true));}
        }
        static ReferenceGladeVoxelLibrary.ActorPaint[] BuildActorPaints(SpawnRing3DLibrary ring)
        {
            var catalog=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
            if(catalog==null)throw new InvalidOperationException("Adopted native voxel rigs required for scoped paint.");
            catalog.Validate();var plans=new List<(string id,Mesh source,Vector2[] uv)>();
            // Complete every source/bone/color preflight before writing paint assets.
            foreach(string id in ReferenceGladeVoxelLibrary.ActorModelIds)
            {
                var prefab=ring.FindModel(id);var skins=prefab==null?Array.Empty<SkinnedMeshRenderer>():prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if(skins.Length!=1||skins[0].sharedMaterials.Length!=1)throw new InvalidOperationException("Expected one original native rig: "+id);
                var skin=skins[0];var binding=catalog.Bindings.SingleOrDefault(b=>b.Source==skin.sharedMesh);
                var source=binding?.Voxel;if(source==null||!source.isReadable||source.bindposeCount==0)throw new InvalidOperationException("Missing adopted voxel rig: "+id);
                var uv=source.uv;var original=source.uv;
                if(uv.Length!=source.vertexCount)throw new InvalidOperationException("Incomplete adopted rig paint: "+id);
                bool humanoid=id=="ring-player"||id=="ring-sien"||id=="ring-nam";
                if(humanoid)
                {
                    var weights=source.boneWeights;int pale=0,body=0;
                    if(weights.Length!=uv.Length)throw new InvalidOperationException("Incomplete actor weights: "+id);
                    for(int i=0;i<uv.Length;i++)
                    {
                        var w=weights[i];int bone=w.boneIndex0;float max=w.weight0;
                        if(w.weight1>max){bone=w.boneIndex1;max=w.weight1;}if(w.weight2>max){bone=w.boneIndex2;max=w.weight2;}if(w.weight3>max){bone=w.boneIndex3;max=w.weight3;}
                        if(bone<0||bone>=skin.bones.Length||skin.bones[bone]==null)throw new InvalidOperationException("Unknown adopted actor bone: "+id);
                        string name=skin.bones[bone].name;bool accent=name=="Head"||name.StartsWith("Hand.",StringComparison.Ordinal);
                        int slot=accent?17:id=="ring-player"?16:11;
                        if(accent)pale++;else body++;uv[i]=new Vector2((slot+.5f)/24f,.5f);
                    }
                    if(pale==0||body==0)throw new InvalidOperationException("Actor paint must retain head/hands and body: "+id);
                }
                else
                {
                    var colors=original.GroupBy(value=>value).OrderByDescending(g=>g.Count()).ThenBy(g=>g.Key.x).ThenBy(g=>g.Key.y).Select(g=>g.Key).ToArray();
                    if(colors.Length!=2)throw new InvalidOperationException("Original Marlback plate/body colors required: "+id);
                    for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(((original[i]==colors[0]?18:21)+.5f)/24f,.5f);
                }
                plans.Add((id,source,uv));
            }
            string folder=Folder+"/ActorPaint";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var result=new List<ReferenceGladeVoxelLibrary.ActorPaint>();
            foreach(var plan in plans)
            {
                string path=folder+"/"+plan.id+".asset";var painted=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(painted==null){RefuseWrongType(path);painted=new Mesh();AssetDatabase.CreateAsset(painted,path);}
                EditorUtility.CopySerialized(plan.source,painted);painted.name="reference-glade-painted-"+plan.id;painted.uv=plan.uv;EditorUtility.SetDirty(painted);
                result.Add(new ReferenceGladeVoxelLibrary.ActorPaint{ModelId=plan.id,Source=plan.source,Painted=painted});
            }
            return result.ToArray();
        }
        static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.z);
        static void RefuseWrongType(string path){if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new InvalidOperationException("Refusing unexpected existing asset at "+path);}
    }
}
#endif
