#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Editor
{
    /// <summary>Explicit UV-only adoption. All source preflight precedes writes;
    /// original prefabs, mesh/rig buffers, clips, materials and importers stay borrowed.</summary>
    public static class SpreadVisitorPaintBuilder
    {
        private const string Folder="Assets/Resources/SpreadVisitorPaint3D";
        [Serializable] public sealed class Report
        {public string status,error;public Row[] rows;public string[] written;public bool borrowedBytesUnchanged;}
        [Serializable] public sealed class Row
        {public string model,sourcePrefab,sourceMesh,sourceHash,texture;public int vertices,triangles,bones,sourceColors;public int[] swatchVertexCounts;}
        private sealed class Plan
        {public string Id;public GameObject Prefab;public Mesh Source;public Vector2[] Uv;public Row Row;}
        public static Report Run()=>Build("Docs/Verification/DensityCompletion/SpreadBiome/Visitors/ExistingRigs/native-import.json");
        public static Report Build(string reportPath)
        {
            var report=new Report();var written=new List<string>();var snapshots=new Dictionary<Texture,Texture2D>();
            var borrowed=new Dictionary<string,string>(StringComparer.Ordinal);
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                    throw new InvalidOperationException("Wait for idle native editor before visitor palette adoption.");
                var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
                var voxel=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
                var glade=ReferenceGladeVoxelLibrary.Load();
                if(ring==null||voxel==null||glade==null)throw new InvalidOperationException("Existing native source and approved palette libraries required.");
                ring.Validate();voxel.Validate();glade.Validate();
                var colors=ApprovedColors();CaptureDependencies(glade.Material,borrowed);
                Preflight<SpreadVisitorPaintLibrary>(Folder+"/Library.asset");
                foreach(string id in SpreadVisitorPaintLibrary.ModelIds)Preflight<Mesh>(Folder+"/"+id+".asset");
                var plans=new List<Plan>();
                foreach(string id in SpreadVisitorPaintLibrary.ModelIds)
                {
                    var prefab=ring.FindModel(id);if(prefab==null)throw new InvalidOperationException("Missing existing native body: "+id);
                    CaptureDependencies(prefab,borrowed);SpreadVisitorPaintLibrary.ValidateRigHierarchy(prefab);
                    var skins=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);var animators=prefab.GetComponentsInChildren<Animator>(true);
                    if(skins.Length!=1||animators.Length!=1||skins[0].sharedMaterials.Length!=1||skins[0].sharedMaterial==null
                        ||animators[0].avatar==null||!animators[0].avatar.isValid||animators[0].runtimeAnimatorController==null)
                        throw new InvalidOperationException("Expected one original complete native rig: "+id);
                    var clips=animators[0].runtimeAnimatorController.animationClips;
                    if(clips.Length!=5||!new[]{"Idle","Walk","Interact","Attack","Hit"}.All(n=>clips.Count(c=>c!=null&&c.name==n&&c.length>0)==1))
                        throw new InvalidOperationException("Five existing native clips required: "+id);
                    var source=voxel.Resolve(skins[0].sharedMesh);CaptureDependencies(source,borrowed);
                    if(source==null||!source.isReadable||source.vertexCount==0||source.blendShapeCount!=0||source.bindposeCount!=skins[0].bones.Length
                        ||source.boneWeights.Length!=source.vertexCount||skins[0].bones.Any(b=>b==null)||source.uv.Length!=source.vertexCount)
                        throw new InvalidOperationException("Existing adopted voxel body is incomplete: "+id);
                    var material=skins[0].sharedMaterial;var original=source.uv;var uv=new Vector2[original.Length];var used=new HashSet<Color>();var counts=new int[24];
                    for(int i=0;i<uv.Length;i++)
                    {
                        if(!Finite(original[i].x)||!Finite(original[i].y))throw new InvalidOperationException("Nonfinite original palette coordinate: "+id);
                        Color sample=Sample(material,original[i],snapshots);if(!Finite(sample.r)||!Finite(sample.g)||!Finite(sample.b)||!Finite(sample.a))throw new InvalidOperationException("Nonfinite source color.");
                        used.Add(sample);int swatch=Nearest(sample,colors);counts[swatch]++;uv[i]=new Vector2((swatch+.5f)/24f,.5f);
                    }
                    var row=new Row{model=id,sourcePrefab=AssetDatabase.GetAssetPath(prefab),sourceMesh=AssetDatabase.GetAssetPath(source),
                        sourceHash=Hash(AssetDatabase.GetAssetPath(source)),texture=AssetDatabase.GetAssetPath(Texture(material)),vertices=source.vertexCount,
                        triangles=Enumerable.Range(0,source.subMeshCount).Sum(n=>(int)source.GetIndexCount(n)/3),bones=source.bindposeCount,sourceColors=used.Count,swatchVertexCounts=counts};
                    plans.Add(new Plan{Id=id,Prefab=prefab,Source=source,Uv=uv,Row=row});
                }
                // Nothing above changes source assets, importer settings or scene.
                AssertUnchanged(borrowed);EnsureFolder(Folder);var entries=new List<SpreadVisitorPaintLibrary.Entry>();
                foreach(var plan in plans)
                {
                    string path=Folder+"/"+plan.Id+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
                    EditorUtility.CopySerialized(plan.Source,mesh);mesh.name="spread-visitor-painted-"+plan.Id;mesh.uv=plan.Uv;
                    SpreadVisitorPaintLibrary.ValidatePaintPair(plan.Source,mesh);EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);written.Add(path);
                    entries.Add(new SpreadVisitorPaintLibrary.Entry{ModelId=plan.Id,SourcePrefab=plan.Prefab,Source=plan.Source,Painted=mesh});
                }
                string libraryPath=Folder+"/Library.asset";var library=AssetDatabase.LoadAssetAtPath<SpreadVisitorPaintLibrary>(libraryPath);
                if(library==null){library=ScriptableObject.CreateInstance<SpreadVisitorPaintLibrary>();AssetDatabase.CreateAsset(library,libraryPath);}
                library.Material=glade.Material;library.Entries=entries.ToArray();library.Validate();EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);written.Add(libraryPath);
                AssertUnchanged(borrowed);report.borrowedBytesUnchanged=true;report.rows=plans.Select(p=>p.Row).ToArray();report.status="passed";
            }
            catch(Exception ex){report.status="failed";report.error=ex.ToString();throw;}
            finally
            {
                foreach(var image in snapshots.Values)if(image!=null)Object.DestroyImmediate(image);
                report.written=written.ToArray();string full=Path.GetFullPath(reportPath);Directory.CreateDirectory(Path.GetDirectoryName(full));File.WriteAllText(full,JsonUtility.ToJson(report,true));
            }
            return report;
        }
        private static Texture Texture(Material material)=>material.HasProperty("_BaseMap")?material.GetTexture("_BaseMap"):material.mainTexture;
        private static Color Sample(Material material,Vector2 uv,Dictionary<Texture,Texture2D> snapshots)
        {
            var borrowed=Texture(material);Color tint=material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):material.HasProperty("_Color")?material.GetColor("_Color"):Color.white;
            if(borrowed==null)return tint;
            if(!snapshots.TryGetValue(borrowed,out var texture))
            {
                string path=AssetDatabase.GetAssetPath(borrowed);
                if(!path.EndsWith(".png",StringComparison.OrdinalIgnoreCase)||!File.Exists(path))throw new InvalidOperationException("Existing palette must have explicit PNG source bytes.");
                texture=new Texture2D(2,2,TextureFormat.RGBA32,false,false){hideFlags=HideFlags.HideAndDontSave};
                try{if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path),false))throw new InvalidOperationException("Could not decode original palette.");snapshots.Add(borrowed,texture);}
                catch{Object.DestroyImmediate(texture);throw;}
            }
            float u=borrowed.wrapMode==TextureWrapMode.Repeat?Mathf.Repeat(uv.x,1):Mathf.Clamp01(uv.x);
            float v=borrowed.wrapMode==TextureWrapMode.Repeat?Mathf.Repeat(uv.y,1):Mathf.Clamp01(uv.y);
            return texture.GetPixel(Mathf.Min((int)(u*texture.width),texture.width-1),Mathf.Min((int)(v*texture.height),texture.height-1))*tint;
        }
        private static Color[] ApprovedColors()
        {var colors=new Color[24];for(int i=0;i<24;i++)if(!ColorUtility.TryParseHtmlString(SpreadEnvironmentSource.ApprovedPalette[i],out colors[i]))throw new InvalidOperationException("Invalid approved palette.");return colors;}
        private static int Nearest(Color source,Color[] colors)
        {int best=0;float distance=float.PositiveInfinity;for(int i=0;i<colors.Length;i++){var d=source-colors[i];float q=2*d.r*d.r+4*d.g*d.g+3*d.b*d.b;if(q<distance){distance=q;best=i;}}return best;}
        private static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        private static void CaptureDependencies(Object value,Dictionary<string,string> hashes)
        {
            string path=AssetDatabase.GetAssetPath(value);if(string.IsNullOrEmpty(path)||!File.Exists(path))throw new InvalidOperationException("Persistent source asset required.");
            foreach(string dependency in AssetDatabase.GetDependencies(path,true).Concat(new[]{path}))
                foreach(string file in new[]{dependency,dependency+".meta"})if(File.Exists(file)&&!hashes.ContainsKey(file))hashes.Add(file,Hash(file));
        }
        private static void AssertUnchanged(Dictionary<string,string> hashes)
        {foreach(var pair in hashes)if(!File.Exists(pair.Key)||Hash(pair.Key)!=pair.Value)throw new InvalidOperationException("Borrowed source bytes changed: "+pair.Key);}
        private static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        private static void Preflight<T>(string path)where T:Object
        {if((File.Exists(path)||AssetDatabase.LoadMainAssetAtPath(path)!=null)&&AssetDatabase.LoadAssetAtPath<T>(path)==null)throw new InvalidOperationException("Refusing foreign output type: "+path);}
        private static void EnsureFolder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
    }
}
#endif
