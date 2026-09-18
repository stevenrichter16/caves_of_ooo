#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Explicit offline conversion of the native model libraries. Source
    /// FBXs, prefabs, materials, rigs and import settings are never rewritten.</summary>
    public static class VoxelWorldMeshBuilder
    {
        public const string ArtRoot = "Assets/Art3D/VoxelWorld";
        public const string CatalogPath = "Assets/Resources/VoxelWorld/Library.asset";
        [Serializable] public sealed class MeshReport
        {
            public string sourceKey, sourcePath, sourceName, outputPath, outputGuid;
            public int usages, sourceVertices, voxelVertices, sourceTriangles, voxelTriangles, submeshes, bones;
            public int simplifiedPaintVertices;
            public int objectPaintColors, objectPaintChangedVertices;
            public float maxPrefabScale, localVoxelSize, worldVoxelSize;
            public Vector3 sourceBoundsSize, voxelBoundsSize;
        }
        [Serializable] public sealed class Report
        {
            public string startedUtc, finishedUtc, status, error, catalogAsset;
            public bool borrowedAssetsUnchanged, existingGuidsPreserved;
            public int prefabCount, meshCount, skinnedMeshCount;
            public long sourceTriangles, voxelTriangles;
            public MeshReport[] meshes;
            public string[] borrowedPaths;
        }
        sealed class Source
        {
            public Mesh Mesh;
            public string Key, Path;
            public float Scale;
            public int Usages;
            public bool Skinned;
            public int PaintColumns;
            public string PaintTexture;
            public string ObjectTexture;
            public bool SolidTint;
        }
        sealed class Prepared
        {
            public Source Source;
            public Mesh Mesh;
            public MeshReport Report;
        }
        [MenuItem("Tools/Caves of Ooo/Voxel World/Bake native mesh catalog")]
        public static void BuildMenu()
        { Build(); MorrowfastCoarseVoxelBuilder.Build(); }
        public static void BuildFromCommandLine()
        {
            string path=null;var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-voxelWorldMeshReport")path=args[i+1];
            Build(path);
            MorrowfastCoarseVoxelBuilder.Build();
        }
        /// <summary>Base source-mesh stage used by ToolkitImporter, which follows
        /// it with recipe import and the Morrowfast coarse overlay. This method
        /// alone is not the final gameplay art build; use either public command
        /// wrapper or ToolkitImporter.BuildAllFromCommandLine for published art.</summary>
        public static Report Build(string reportPath=null)
        {
            var report=new Report {startedUtc=DateTime.UtcNow.ToString("O"),status="running",catalogAsset=CatalogPath};
            string destination=Path.GetFullPath(string.IsNullOrEmpty(reportPath)?"Docs/Verification/VoxelWorld/mesh-build.json":reportPath);
            var prepared=new List<Prepared>();var borrowed=new Dictionary<string,string>(StringComparer.Ordinal);
            var existingGuids=new Dictionary<string,string>(StringComparer.Ordinal);
            var paintPalettes=new Dictionary<string,Vector2[]>(StringComparer.Ordinal);
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                    throw new InvalidOperationException("Run the explicit voxel mesh builder outside play mode and compilation.");
                var village=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
                var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
                var pilot=Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath);
                if(village==null||ring==null||pilot==null)throw new InvalidOperationException("All three native 3D libraries must be imported before baking voxels.");
                village.Validate();ring.Validate();pilot.Validate();
                var prefabs=village.Models.Select(m=>m.Prefab).Concat(ring.Models.Select(m=>m.Prefab))
                    .Concat(pilot.Models.Select(m=>m.Prefab)).Concat(ring.EquipmentLibrary.Models.Select(m=>m.Prefab)).Distinct().ToArray();
                report.prefabCount=prefabs.Length;
                foreach(var library in new Object[]{village,ring,pilot,ring.EquipmentLibrary})TrackBorrowed(AssetDatabase.GetAssetPath(library),borrowed);
                var sources=Collect(prefabs,borrowed);
                if(sources.Count==0)throw new InvalidOperationException("No native model meshes were found.");
                // Validate and bake every mesh in memory before touching destinations.
                // Asset publication below keeps existing GUIDs; the catalog table is
                // replaced last, only after complete source coverage is verified.
                foreach(var source in sources.OrderBy(s=>s.Key,StringComparer.Ordinal))
                {
                    bool equipment=Path.GetFileNameWithoutExtension(source.Path).StartsWith("equipment-",StringComparison.Ordinal);
                    float worldPitch=VoxelWorldDensity.SelectWorldPitch(source.Mesh.bounds.size,source.Scale,source.Skinned,equipment);
                    float localPitch=worldPitch/source.Scale;
                    var baked=VoxelWorldMeshBaker.Bake(source.Mesh,localPitch);
                    // Own the native allocation before palette IO/validation can
                    // throw. Failed editor retries must release it in finally too.
                    var pending=new Prepared {Source=source,Mesh=baked,Report=new MeshReport {sourceKey=source.Key,sourcePath=source.Path}};
                    prepared.Add(pending);
                    int simplified=0;
                    if(source.PaintColumns>0)
                    {
                        if(!paintPalettes.TryGetValue(source.PaintTexture,out var palette))
                        {palette=VoxelWorldPaintSimplifier.LoadRepresentativeUvs(source.PaintTexture,source.PaintColumns);paintPalettes.Add(source.PaintTexture,palette);}
                        var before=baked.uv;var after=VoxelWorldPaintSimplifier.Consolidate(before,palette,source.PaintColumns);
                        for(int i=0;i<before.Length;i++)if(before[i]!=after[i])simplified++;
                        baked.uv=after;
                    }
                    string path=ArtRoot+"/Meshes/"+source.Key+".asset";
                    var row=new MeshReport {sourceKey=source.Key,sourcePath=source.Path,sourceName=source.Mesh.name,outputPath=path,
                        usages=source.Usages,sourceVertices=source.Mesh.vertexCount,voxelVertices=baked.vertexCount,
                        sourceTriangles=Triangles(source.Mesh),voxelTriangles=Triangles(baked),submeshes=source.Mesh.subMeshCount,
                        bones=source.Mesh.bindposes.Length,maxPrefabScale=source.Scale,localVoxelSize=localPitch,worldVoxelSize=worldPitch,
                        sourceBoundsSize=source.Mesh.bounds.size,voxelBoundsSize=baked.bounds.size,simplifiedPaintVertices=simplified};
                    pending.Report=row;
                    ValidateBaked(source,baked);
                    TrackGuid(path,existingGuids);
                }
                TrackGuid(CatalogPath,existingGuids);
                ApplyObjectPalettes(prepared);
                CheckBorrowed(borrowed);
                EnsureFolder(ArtRoot+"/Meshes");EnsureFolder("Assets/Resources/VoxelWorld");
                var bindings=new List<VoxelWorldMeshCatalog.Binding>(prepared.Count);
                foreach(var entry in prepared)
                {
                    string path=entry.Report.outputPath;var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(mesh==null)
                    {
                        RefuseWrongType(path);mesh=entry.Mesh;AssetDatabase.CreateAsset(mesh,path);entry.Mesh=null;
                    }
                    else {ReplaceGeneratedGeometry(entry.Mesh,mesh);EditorUtility.SetDirty(mesh);}
                    bindings.Add(new VoxelWorldMeshCatalog.Binding {Source=entry.Source.Mesh,Voxel=mesh,SourceKey=entry.Source.Key,
                        VoxelSize=entry.Report.localVoxelSize,WorldVoxelSize=entry.Report.worldVoxelSize,SourceBindposes=entry.Source.Mesh.bindposes});
                    entry.Report.outputGuid=AssetDatabase.AssetPathToGUID(path);
                }
                var catalog=AssetDatabase.LoadAssetAtPath<VoxelWorldMeshCatalog>(CatalogPath);
                if(catalog==null)
                {RefuseWrongType(CatalogPath);catalog=ScriptableObject.CreateInstance<VoxelWorldMeshCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
                var candidate=ScriptableObject.CreateInstance<VoxelWorldMeshCatalog>();
                try {candidate.Bindings=bindings.ToArray();candidate.Validate();catalog.Bindings=candidate.Bindings;catalog.InvalidateCaches();catalog.Validate();}
                finally {Object.DestroyImmediate(candidate);}
                EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
                foreach(var old in existingGuids)if(AssetDatabase.AssetPathToGUID(old.Key)!=old.Value)throw new InvalidOperationException("Existing voxel asset GUID changed: "+old.Key);
                CheckBorrowed(borrowed);
                report.meshCount=prepared.Count;report.skinnedMeshCount=prepared.Count(p=>p.Source.Skinned);
                report.sourceTriangles=prepared.Sum(p=>(long)p.Report.sourceTriangles);report.voxelTriangles=prepared.Sum(p=>(long)p.Report.voxelTriangles);
                report.existingGuidsPreserved=true;report.borrowedAssetsUnchanged=true;report.status="passed";
                Debug.Log("Voxel mesh catalog baked: "+report.meshCount+" meshes, "+report.skinnedMeshCount+" skinned; borrowed assets unchanged.");
                return report;
            }
            catch(Exception error){report.status="failed";report.error=error.ToString();throw;}
            finally
            {
                report.finishedUtc=DateTime.UtcNow.ToString("O");report.meshes=prepared.Select(p=>p.Report).ToArray();report.borrowedPaths=borrowed.Keys.OrderBy(p=>p,StringComparer.Ordinal).ToArray();
                string folder=Path.GetDirectoryName(destination);if(!string.IsNullOrEmpty(folder))Directory.CreateDirectory(folder);
                File.WriteAllText(destination,JsonUtility.ToJson(report,true));
                foreach(var entry in prepared)if(entry.Mesh!=null)Object.DestroyImmediate(entry.Mesh);
            }
        }
        // CopySerialized can retain a persistent skinned mesh's old vertex
        // stream while replacing its indices. Write the generated channels
        // through Mesh instead, retaining the destination asset and its GUID.
        // Both voxel exporters emit only positions, normals, UV0 and optional
        // rigid skin weights/bindposes; no source FBX is passed as destination.
        internal static void ReplaceGeneratedGeometry(Mesh source,Mesh destination)
        {
            if(source==null||destination==null||source==destination)
                throw new ArgumentException("Distinct generated source and destination meshes are required.");
            destination.Clear(false);
            destination.indexFormat=source.indexFormat;
            destination.vertices=source.vertices;
            destination.normals=source.normals;
            destination.uv=source.uv;
            destination.bindposes=source.bindposes;
            destination.boneWeights=source.boneWeights;
            destination.subMeshCount=source.subMeshCount;
            for(int sub=0;sub<source.subMeshCount;sub++)
                destination.SetTriangles(source.GetTriangles(sub),sub,false);
            destination.bounds=source.bounds;
        }
        static void ApplyObjectPalettes(List<Prepared> prepared)
        {
            var images=new Dictionary<string,VoxelWorldObjectPalette.ImagePixels>(StringComparer.Ordinal);
            // Every native model is an FBX prefab. Its water and opaque child
            // share one object budget rather than each getting two colors.
            foreach(var group in prepared.GroupBy(p=>p.Source.Path,StringComparer.Ordinal))
            {
                var opaque=group.Where(p=>p.Source.ObjectTexture!=null).ToArray();
                if(opaque.Length==0||group.Any(p=>p.Source.ObjectTexture==null&&!p.Source.SolidTint))continue;
                string texture=opaque[0].Source.ObjectTexture;
                if(opaque.Any(p=>p.Source.ObjectTexture!=texture))
                    throw new InvalidOperationException("A native object has incompatible child palettes: "+group.Key);
                int reserved=group.Count(p=>p.Source.SolidTint);
                if(reserved>1)throw new InvalidOperationException("Native object exceeds its solid-color budget: "+group.Key);
                if(!images.TryGetValue(texture,out var image))
                {image=VoxelWorldObjectPalette.Load(texture);images.Add(texture,image);}
                var input=opaque.SelectMany(p=>p.Mesh.uv).ToArray();
                var output=reserved==0
                    ?VoxelWorldObjectPalette.ReduceNativeObject(input,image.Pixels,image.Width,image.Height,group.Key,texture)
                    :VoxelWorldObjectPalette.ReduceToBudget(input,image.Pixels,image.Width,image.Height,1);
                int offset=0;
                foreach(var entry in opaque)
                {
                    int length=entry.Mesh.vertexCount;var uv=new Vector2[length];
                    Array.Copy(output,offset,uv,0,length);
                    for(int i=0;i<length;i++)if(input[offset+i]!=uv[i])entry.Report.objectPaintChangedVertices++;
                    entry.Mesh.uv=uv;entry.Report.objectPaintColors=uv.Distinct().Count();offset+=length;
                }
            }
        }
        static List<Source> Collect(GameObject[] prefabs,Dictionary<string,string> borrowed)
        {
            var found=new Dictionary<Mesh,Source>();
            foreach(var prefab in prefabs)
            {
                if(prefab==null)throw new InvalidOperationException("A native prefab is missing.");
                foreach(string dependency in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(prefab),true))TrackBorrowed(dependency,borrowed);
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer=filter.GetComponent<MeshRenderer>();
                    if(renderer!=null)Add(filter.sharedMesh,renderer,false,found);
                }
                foreach(var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))Add(renderer.sharedMesh,renderer,true,found);
            }
            return found.Values.ToList();
        }
        static void Add(Mesh mesh,Renderer renderer,bool skinned,Dictionary<Mesh,Source> sources)
        {
            if(mesh==null||mesh.vertexCount==0)throw new InvalidOperationException("Native renderer has an empty source mesh: "+renderer.name);
            if(renderer.sharedMaterials.Length!=mesh.subMeshCount)throw new InvalidOperationException("Native renderer material slots differ from mesh submeshes: "+renderer.name);
            foreach(var material in renderer.sharedMaterials)
                if(material==null||!material.HasProperty("_FogLight")||!material.HasProperty("_Transient"))
                    throw new InvalidOperationException("Native renderer does not use a fog-compatible material: "+renderer.name);
            var matrix=renderer.transform.localToWorldMatrix;
            float scale=Mathf.Max(((Vector3)matrix.GetColumn(0)).magnitude,Mathf.Max(((Vector3)matrix.GetColumn(1)).magnitude,((Vector3)matrix.GetColumn(2)).magnitude));
            if(!VoxelWorldMeshBaker.IsFinite(scale)||scale<.000001f)throw new InvalidOperationException("Native prefab transform scale is invalid.");
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long localId)||string.IsNullOrEmpty(guid))
                throw new InvalidOperationException("Native source mesh has no persistent asset identity.");
            if(!sources.TryGetValue(mesh,out var source))
            {
                source=new Source {Mesh=mesh,Key=guid+"_"+localId.ToString(System.Globalization.CultureInfo.InvariantCulture).Replace('-','n'),Path=AssetDatabase.GetAssetPath(mesh)};
                sources.Add(mesh,source);
            }
            var paintMaterial=renderer.sharedMaterials.Length==1?renderer.sharedMaterial:null;
            string texture=paintMaterial!=null&&paintMaterial.HasProperty("_BaseMap")?AssetDatabase.GetAssetPath(paintMaterial.GetTexture("_BaseMap")):null;
            bool knownPalette=texture=="Assets/Art3D/Village/Textures/VillagePalette.png"
                ||texture=="Assets/Art3D/SpawnRing/Textures/SpawnRingPalette.png"
                ||texture=="Assets/Art3D/MultiCellPilot/Textures/PilotPalette.png";
            string objectTexture=paintMaterial!=null&&paintMaterial.shader.name=="CavesOfOoo/Village3D/Palette"
                &&knownPalette&&paintMaterial.GetTextureScale("_BaseMap")==Vector2.one
                &&paintMaterial.GetColor("_BaseColor")==Color.white
                &&paintMaterial.GetTextureOffset("_BaseMap")==Vector2.zero?texture:null;
            // Both offline paint passes require the same untransformed, untinted
            // atlas contract. A foreign shared usage must veto S1 as well as P1.
            int columns=objectTexture!=null?VoxelWorldPaintSimplifier.AtlasColumns(source.Path,texture,skinned):0;
            bool solidTint=paintMaterial!=null&&paintMaterial.shader.name=="CavesOfOoo/Village3D/Water"
                &&paintMaterial.GetTexture("_BaseMap")==null;
            if(source.Usages==0){source.ObjectTexture=objectTexture;source.SolidTint=solidTint;}
            else
            {if(source.ObjectTexture!=objectTexture)source.ObjectTexture=null;source.SolidTint&=solidTint;}
            if(source.Usages==0){source.PaintColumns=columns;source.PaintTexture=texture;}
            // Any excluded or differently textured usage vetoes simplification of
            // this shared mesh; the first encountered renderer never grants ownership.
            else if(columns!=source.PaintColumns||texture!=source.PaintTexture)source.PaintColumns=0;
            source.Usages++;source.Scale=Mathf.Max(source.Scale,scale);source.Skinned|=skinned;
        }
        static void ValidateBaked(Source source,Mesh mesh)
        {
            if(mesh==null||mesh.vertexCount==0||mesh.subMeshCount!=source.Mesh.subMeshCount||!mesh.isReadable)
                throw new InvalidOperationException("Baked mesh does not preserve the native static mesh contract: "+source.Path);
            for(int sub=0;sub<mesh.subMeshCount;sub++)
                if(source.Mesh.GetIndexCount(sub)>0&&mesh.GetIndexCount(sub)==0)
                    throw new InvalidOperationException("Voxel pitch erased a source material submesh: "+source.Path+" submesh "+sub);
            if(source.Skinned&&(mesh.bindposes.Length!=source.Mesh.bindposes.Length||mesh.boneWeights.Length!=mesh.vertexCount))
                throw new InvalidOperationException("Baked skin lost weights or bindposes: "+source.Path);
        }
        static int Triangles(Mesh mesh)
        {long count=0;for(int i=0;i<mesh.subMeshCount;i++)count+=(long)mesh.GetIndexCount(i)/3;return checked((int)count);}
        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        static void RefuseWrongType(string path)
        {if(AssetDatabase.LoadMainAssetAtPath(path)!=null||File.Exists(path))throw new InvalidOperationException("Refusing to overwrite a different asset type: "+path);}
        static void TrackGuid(string path,Dictionary<string,string> guids)
        {string guid=AssetDatabase.AssetPathToGUID(path);if(!string.IsNullOrEmpty(guid))guids[path]=guid;}
        static void TrackBorrowed(string path,Dictionary<string,string> hashes)
        {
            if(string.IsNullOrEmpty(path)||!File.Exists(path)||hashes.ContainsKey(path))return;
            hashes.Add(path,Hash(path));if(File.Exists(path+".meta"))hashes.Add(path+".meta",Hash(path+".meta"));
        }
        static void CheckBorrowed(Dictionary<string,string> hashes)
        {foreach(var item in hashes)if(!File.Exists(item.Key)||Hash(item.Key)!=item.Value)throw new InvalidOperationException("Borrowed asset changed during voxel bake: "+item.Key);}
        static string Hash(string path)
        {using(var hash=SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(hash.ComputeHash(file)).Replace("-","").ToLowerInvariant();}
    }
}
#endif
