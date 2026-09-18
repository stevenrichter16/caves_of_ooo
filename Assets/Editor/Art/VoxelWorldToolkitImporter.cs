#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Imports the actual Blender toolkit's neutral voxel recipes into
    /// native model bindings. Layout, colliders, source prefabs and materials are
    /// unchanged. A uniform fit preserves each recipe's aspect ratio.</summary>
    public static class VoxelWorldToolkitImporter
    {
        const string SourcePath = "ArtSource/VoxelTown/Output/native-region/assets-coarse.json";
        const string BindingPath = "ArtSource/VoxelTown/native-bindings.json";
        const string OutputRoot = "Assets/Art3D/VoxelWorld/Toolkit";
        const string Ownership = "CavesOfOoo.VoxelWorld.Toolkit/1";
        static readonly Matrix4x4 NativeAxes = new Matrix4x4(new Vector4(1,0,0,0),
            new Vector4(0,0,1,0), new Vector4(0,1,0,0), new Vector4(0,0,0,1));
        [Serializable] public sealed class Row
        {
            public string library, model, recipe, sourceKey, meshAsset;
            public float uniformFit, worldVoxelSize, localVoxelSize;
            public int vertices, triangles;
        }
        [Serializable] public sealed class Report
        {
            public string status, error, sourceSha256, bindingsSha256, finishedUtc;
            public int imported; public Row[] rows;
        }
        sealed class Pending
        { public VoxelWorldMeshCatalog.Binding Binding; public Mesh Mesh; public Row Row; }

        public static void BuildAllFromCommandLine()
        {
            VoxelWorldMeshBuilder.Build(Argument("-voxelWorldMeshReport"));
            Build(Argument("-voxelWorldToolkitReport"));
        }
        public static void BuildFromCommandLine() => Build(Argument("-voxelWorldToolkitReport"));
        static string Argument(string name)
        { var args=Environment.GetCommandLineArgs(); for(int i=0;i<args.Length-1;i++)if(args[i]==name)return args[i+1]; return null; }

        /// <summary>Pure geometry import entry for tests and offline exporters.
        /// Per-face vertices retain hard voxel normals and palette ownership.
        /// Reflection transforms reverse winding exactly once.</summary>
        public static Mesh CreateMesh(string json, string recipeId, Matrix4x4 transform, Vector2[] materialUvs)
        {
            if (string.IsNullOrEmpty(json) || json.Length > 16000000 || materialUvs == null || materialUvs.Length == 0)
                throw new ArgumentException("A bounded recipe document and material palette are required.");
            for(int i=0;i<16;i++)if(!Finite(transform[i]))throw new ArgumentException("Non-finite recipe transform.");
            if(transform.m30!=0||transform.m31!=0||transform.m32!=0||transform.m33!=1)
                throw new ArgumentException("Recipe transform must be affine.");
            if(Mathf.Abs(transform.determinant)<1e-12f)throw new ArgumentException("Recipe transform is singular.");
            var document=JObject.Parse(json);
            if((int?)document["schemaVersion"]!=1)throw new ArgumentException("Unsupported toolkit schema.");
            var recipes=document["assets"] as JArray;
            var matches=recipes?.OfType<JObject>().Where(a=>(string)a["id"]==recipeId).ToArray();
            if(matches==null||matches.Length!=1)throw new ArgumentException("Missing or ambiguous toolkit recipe: "+recipeId);
            var recipe=matches[0]; var source=recipe["vertices"] as JArray; var faces=recipe["quads"] as JArray;
            var paint=recipe["quadMaterials"] as JArray;
            if(source==null||faces==null||paint==null||source.Count==0||source.Count>1000000||faces.Count==0||faces.Count>250000||paint.Count!=faces.Count)
                throw new ArgumentException("Recipe has invalid bounded geometry.");
            var points=new Vector3[source.Count];
            for(int i=0;i<source.Count;i++)
            {
                var p=source[i] as JArray;if(p==null||p.Count!=3)throw new ArgumentException("Invalid recipe vertex.");
                for(int c=0;c<3;c++)if(p[c].Type!=JTokenType.Integer&&p[c].Type!=JTokenType.Float)
                    throw new ArgumentException("Vertex coordinates require numeric tokens.");
                var value=new Vector3((float)p[0],(float)p[1],(float)p[2]);
                points[i]=transform.MultiplyPoint3x4(value);
                if(!Finite(points[i].x)||!Finite(points[i].y)||!Finite(points[i].z))throw new ArgumentException("Non-finite recipe vertex.");
            }
            bool flip=transform.determinant<0;
            var vertices=new List<Vector3>(faces.Count*4);var uvs=new List<Vector2>(faces.Count*4);
            var triangles=new List<int>(faces.Count*6);
            for(int i=0;i<faces.Count;i++)
            {
                var face=faces[i] as JArray; int material=Index(paint[i]);
                if(face==null||face.Count!=4||material<0||material>=materialUvs.Length)throw new ArgumentException("Invalid face or material index.");
                var uv=materialUvs[material];if(!Finite(uv.x)||!Finite(uv.y))throw new ArgumentException("Non-finite palette coordinate.");
                int start=vertices.Count;
                for(int c=0;c<4;c++)
                {
                    int index=Index(face[c]);if(index<0||index>=points.Length)throw new ArgumentException("Face index outside recipe vertices.");
                    vertices.Add(points[index]);uvs.Add(uv);
                }
                var first=Vector3.Cross(vertices[start+1]-vertices[start],vertices[start+2]-vertices[start]);
                var second=Vector3.Cross(vertices[start+2]-vertices[start],vertices[start+3]-vertices[start]);
                if(first.sqrMagnitude<1e-18f||second.sqrMagnitude<1e-18f||Vector3.Dot(first.normalized,second.normalized)<.99999f)
                    throw new ArgumentException("Degenerate, folded or non-planar recipe face.");
                triangles.Add(start);triangles.Add(start+(flip?2:1));triangles.Add(start+(flip?1:2));
                triangles.Add(start);triangles.Add(start+(flip?3:2));triangles.Add(start+(flip?2:3));
            }
            var mesh=new Mesh {name="VX_Toolkit_"+recipeId,indexFormat=IndexFormat.UInt32};
            try {mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
            catch {Object.DestroyImmediate(mesh);throw;}
        }
        static bool Finite(float value) => !float.IsNaN(value)&&!float.IsInfinity(value);
        static int Index(JToken token)
        {
            if(token==null||token.Type!=JTokenType.Integer)throw new ArgumentException("Indices require integer tokens.");
            long value=(long)token;if(value<int.MinValue||value>int.MaxValue)throw new ArgumentException("Index exceeds integer bounds.");
            return (int)value;
        }
        static float ConvertedLocalPitch(float local,float world,float next)
        {
            if(!Finite(local)||!Finite(world)||!Finite(next)||local<=0||world<=0||next<=0)
                throw new ArgumentException("Native and recipe voxel pitches must be finite and positive.");
            float converted=(float)((double)local*next/world);
            if(!Finite(converted)||converted<=0)throw new ArgumentException("Converted local voxel pitch exceeds finite bounds.");
            return converted;
        }

        public static Report Build(string reportPath=null)
        {
            var report=new Report {status="running"};var pending=new List<Pending>();
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                    throw new InvalidOperationException("Import toolkit recipes outside Play and compilation.");
                var catalog=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
                if(catalog==null)throw new InvalidOperationException("Bake the native voxel catalogue first.");catalog.Validate();
                string json=File.ReadAllText(SourcePath),bindingJson=File.ReadAllText(BindingPath);
                report.sourceSha256=Hash(SourcePath);report.bindingsSha256=Hash(BindingPath);
                var document=JObject.Parse(json);var recipes=((JArray)document["assets"]).OfType<JObject>().ToDictionary(r=>(string)r["id"],StringComparer.Ordinal);
                var village=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
                var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
                village.Validate();ring.Validate();
                var seen=new HashSet<Mesh>();
                var specification=JObject.Parse(bindingJson);
                if((int?)specification["schemaVersion"]!=1)throw new ArgumentException("Unsupported native binding schema.");
                foreach(var binding in (JArray)specification["bindings"])
                {
                    string family=(string)binding["library"],model=(string)binding["model"],recipeId=(string)binding["recipe"];
                    var prefab=family=="Village"?village.FindModel(model):family=="SpawnRing"?ring.FindModel(model):null;
                    if(prefab==null||!recipes.TryGetValue(recipeId,out var recipe))throw new ArgumentException("Unknown native/toolkit binding: "+model+" / "+recipeId);
                    var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
                    if(filters.Length!=1||prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length!=0)
                        throw new ArgumentException("Toolkit replacement requires exactly one rigid native mesh: "+model);
                    var filter=filters[0];var mesh=filter.sharedMesh;var renderer=filter.GetComponent<MeshRenderer>();
                    if(mesh==null||renderer==null||mesh.subMeshCount!=1||renderer.sharedMaterials.Length!=1||!seen.Add(mesh))
                        throw new ArgumentException("Ambiguous toolkit native source: "+model);
                    var existing=catalog.Bindings.SingleOrDefault(r=>r.Source==mesh);
                    if(existing==null)throw new ArgumentException("Native source missing from baked catalogue: "+model);
                    Matrix4x4 localToNative=prefab.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                    var bounds=TransformBounds(mesh.bounds,localToNative);
                    var size=(JArray)recipe["bounds"]["size"];
                    var nativeSize=new Vector3((float)size[0],(float)size[2],(float)size[1]);
                    float fit=Mathf.Min(bounds.size.x/nativeSize.x,Mathf.Min(bounds.size.y/nativeSize.y,bounds.size.z/nativeSize.z));
                    if(!Finite(fit)||fit<=0)throw new ArgumentException("Cannot uniformly fit native recipe: "+model);
                    float worldPitch=(float)recipe["voxelSize"]*fit;
                    float localPitch=ConvertedLocalPitch(existing.VoxelSize,existing.WorldVoxelSize,worldPitch);
                    var pivot=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                    var conversion=localToNative.inverse*Matrix4x4.TRS(pivot,Quaternion.identity,Vector3.one*fit)*NativeAxes;
                    var palette=PaletteUvs(document,renderer.sharedMaterial);
                    var replacement=CreateMesh(json,recipeId,conversion,palette);
                    string path=OutputRoot+"/"+existing.SourceKey+".asset";
                    var row=new Row {library=family,model=model,recipe=recipeId,sourceKey=existing.SourceKey,meshAsset=path,
                        uniformFit=fit,worldVoxelSize=worldPitch,localVoxelSize=localPitch,vertices=replacement.vertexCount,triangles=replacement.triangles.Length/3};
                    pending.Add(new Pending {Binding=existing,Mesh=replacement,Row=row});
                }
                if(pending.Count==0)throw new ArgumentException("No native toolkit models requested.");
                // Preflight every destination before updating any existing mesh or
                // catalogue row. A late foreign path must not partially publish.
                foreach(var entry in pending) ValidateDestination(entry.Row.meshAsset);
                EnsureFolder(OutputRoot);
                var updated=catalog.Bindings.Select(row=>new VoxelWorldMeshCatalog.Binding
                {Source=row.Source,Voxel=row.Voxel,SourceKey=row.SourceKey,VoxelSize=row.VoxelSize,
                    WorldVoxelSize=row.WorldVoxelSize,SourceBindposes=row.SourceBindposes}).ToArray();
                foreach(var entry in pending)
                {
                    var existing=AssetDatabase.LoadAssetAtPath<Mesh>(entry.Row.meshAsset);
                    if(existing==null)
                    {
                        if(File.Exists(entry.Row.meshAsset))throw new InvalidOperationException("Refusing foreign toolkit destination.");
                        existing=entry.Mesh;AssetDatabase.CreateAsset(existing,entry.Row.meshAsset);entry.Mesh=null;
                        // CreateAsset normalizes Mesh.name to its filename. Use
                        // explicit importer ownership, not a mutable display name.
                        var importer=AssetImporter.GetAtPath(entry.Row.meshAsset);
                        importer.userData=Ownership;importer.SaveAndReimport();
                    }
                    else {VoxelWorldMeshBuilder.ReplaceGeneratedGeometry(entry.Mesh,existing);EditorUtility.SetDirty(existing);}
                    var row=updated.Single(r=>r.Source==entry.Binding.Source);
                    row.Voxel=existing;
                    row.VoxelSize=entry.Row.localVoxelSize;
                    row.WorldVoxelSize=entry.Row.worldVoxelSize;
                }
                catalog.Bindings=updated;
                catalog.InvalidateCaches();catalog.Validate();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
                // The native-coordinate town overlay is the final art layer.
                // Reimporting shared recipes must not restore the old detailed
                // village scenery or overwrite its deliberate coarse silhouettes.
                MorrowfastCoarseVoxelBuilder.Build();
                report.imported=pending.Count;report.status="passed";
                Debug.Log("Imported "+pending.Count+" native models from procedural voxel recipes.");return report;
            }
            catch(Exception error){report.status="failed";report.error=error.ToString();throw;}
            finally
            {
                report.finishedUtc=DateTime.UtcNow.ToString("O");report.rows=pending.Select(p=>p.Row).ToArray();
                string destination=Path.GetFullPath(reportPath??"Docs/Verification/VoxelWorld/toolkit-import.json");Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllText(destination,JsonUtility.ToJson(report,true));
                foreach(var entry in pending)if(entry.Mesh!=null)Object.DestroyImmediate(entry.Mesh);
            }
        }
        static Vector2[] PaletteUvs(JObject document,Material material)
        {
            var texture=material.GetTexture("_BaseMap") as Texture2D;
            string path=AssetDatabase.GetAssetPath(texture);
            if(texture==null||string.IsNullOrEmpty(path)||!File.Exists(path))throw new ArgumentException("Native material needs an authored palette texture.");
            var copy=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                if(!ImageConversion.LoadImage(copy,File.ReadAllBytes(path),false))throw new ArgumentException("Cannot read native palette.");
                var pixels=copy.GetPixels32();var palette=(JArray)document["palette"];var result=new Vector2[palette.Count];
                for(int m=0;m<palette.Count;m++)
                {
                    if(!ColorUtility.TryParseHtmlString("#"+(string)palette[m]["hex"],out var target))throw new ArgumentException("Invalid toolkit palette hex.");
                    int best=-1;float distance=float.PositiveInfinity;
                    for(int i=0;i<pixels.Length;i++)
                    {
                        if(pixels[i].a<250)continue;Color color=pixels[i];
                        float d=(color.r-target.r)*(color.r-target.r)+(color.g-target.g)*(color.g-target.g)+(color.b-target.b)*(color.b-target.b);
                        if(d<distance){distance=d;best=i;}
                    }
                    if(best<0)throw new ArgumentException("Native palette is transparent.");
                    result[m]=new Vector2((best%copy.width+.5f)/copy.width,(best/copy.width+.5f)/copy.height);
                }
                return result;
            }
            finally {Object.DestroyImmediate(copy);}
        }
        static Bounds TransformBounds(Bounds bounds,Matrix4x4 transform)
        {
            var result=new Bounds(transform.MultiplyPoint3x4(bounds.min),Vector3.zero);
            for(int i=0;i<8;i++)result.Encapsulate(transform.MultiplyPoint3x4(new Vector3((i&1)==0?bounds.min.x:bounds.max.x,(i&2)==0?bounds.min.y:bounds.max.y,(i&4)==0?bounds.min.z:bounds.max.z)));
            return result;
        }
        static void ValidateDestination(string path)
        {
            string root=Path.GetFullPath(OutputRoot)+Path.DirectorySeparatorChar;
            if(!Path.GetFullPath(path).StartsWith(root,StringComparison.Ordinal)||Path.GetDirectoryName(path).Replace('\\','/')!=OutputRoot)
                throw new ArgumentException("Toolkit destination escapes its owned folder.");
            var existing=AssetDatabase.LoadMainAssetAtPath(path);
            if(existing!=null&&(!(existing is Mesh)||AssetImporter.GetAtPath(path)?.userData!=Ownership))
                throw new InvalidOperationException("Refusing foreign toolkit destination: "+path);
            if(existing==null&&(File.Exists(path)||Directory.Exists(path)))
                throw new InvalidOperationException("Refusing unrecognized toolkit destination: "+path);
        }
        static void EnsureFolder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
        static string Hash(string path)
        {using(var hash=SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(file)).Replace("-","").ToLowerInvariant();}
    }
}
#endif
