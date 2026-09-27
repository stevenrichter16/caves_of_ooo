using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Persistent UV-only copies of thirteen existing visitor bodies.
    /// Original native model IDs, prefabs, rigs, clips and gameplay stay native.
    /// This library is used only by an actual receiving-Spread presenter bind.</summary>
    public sealed class SpreadVisitorPaintLibrary : ScriptableObject
    {
        public const string ResourcePath="SpreadVisitorPaint3D/Library";
        public static IReadOnlyList<string> ModelIds { get; }=Array.AsReadOnly(new[]{
            "ring-choir-tendril","ring-mosshulk","ring-rotling","ring-maw-toad","ring-sari-snake",
            "ring-wardline","ring-cascade-father","ring-glasspane-frog","ring-yellowfoot-wayfarer",
            "ring-shambler","ring-grove-lantern-moth","ring-sky-sari","ring-helmwood-frog"});
        [Serializable] public sealed class Entry { public string ModelId;public GameObject SourcePrefab;public Mesh Source,Painted; }
        public Entry[] Entries;public Material Material;
        private Dictionary<string,Entry> index;
        private HashSet<Mesh> meshes;
        public static SpreadVisitorPaintLibrary Load()=>Resources.Load<SpreadVisitorPaintLibrary>(ResourcePath);
        private void OnValidate(){index=null;meshes=null;}
        public void Validate()
        {
            index=null;meshes=null;
            var glade=ReferenceGladeVoxelLibrary.Load();var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var voxel=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
            if(glade==null||ring==null||voxel==null||Material!=glade.Material||Entries==null||Entries.Length!=ModelIds.Count)
                throw new InvalidOperationException("Complete thirteen-rig scoped palette library required.");
            var remaining=new HashSet<string>(ModelIds,StringComparer.Ordinal);var next=new Dictionary<string,Entry>(StringComparer.Ordinal);var owned=new HashSet<Mesh>();
            foreach(var e in Entries)
            {
                if(e==null||e.ModelId==null||!remaining.Remove(e.ModelId)||e.SourcePrefab==null||ring.FindModel(e.ModelId)!=e.SourcePrefab)
                    throw new InvalidOperationException("Unknown or substituted visitor source identity.");
                ValidateRigHierarchy(e.SourcePrefab);
                var skins=e.SourcePrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if(skins.Length!=1||skins[0].sharedMesh==null||voxel.Resolve(skins[0].sharedMesh)!=e.Source||!owned.Add(e.Painted))
                    throw new InvalidOperationException("Visitor paint must derive from the actual adopted native skin.");
                ValidatePaintPair(e.Source,e.Painted);next.Add(e.ModelId,e);
            }
            if(remaining.Count!=0)throw new InvalidOperationException("Missing visitor palette identity.");
            index=next;meshes=owned;
        }
        /// <summary>Reject geometry/rig changes disguised as a palette copy.</summary>
        public static void ValidatePaintPair(Mesh source,Mesh painted)
        {
            if(source==null||painted==null||source==painted||!source.isReadable||!painted.isReadable||source.vertexCount==0
                ||source.vertexCount!=painted.vertexCount||source.indexFormat!=painted.indexFormat||source.bounds!=painted.bounds
                ||source.subMeshCount!=painted.subMeshCount||source.blendShapeCount!=0||painted.blendShapeCount!=0
                ||!source.vertices.SequenceEqual(painted.vertices)||!source.normals.SequenceEqual(painted.normals)
                ||!source.tangents.SequenceEqual(painted.tangents)||!source.colors32.SequenceEqual(painted.colors32)
                ||!source.boneWeights.SequenceEqual(painted.boneWeights)||!source.bindposes.SequenceEqual(painted.bindposes))
                throw new InvalidOperationException("Visitor palette copy altered its borrowed body or rig.");
            for(int n=0;n<source.subMeshCount;n++)
                if(source.GetTopology(n)!=painted.GetTopology(n)||!source.GetIndices(n).SequenceEqual(painted.GetIndices(n)))
                    throw new InvalidOperationException("Visitor palette copy altered native topology.");
            for(int channel=1;channel<8;channel++)
            {
                var before=new List<Vector4>();var after=new List<Vector4>();source.GetUVs(channel,before);painted.GetUVs(channel,after);
                if(!before.SequenceEqual(after))throw new InvalidOperationException("Visitor palette copy altered a nonprimary UV channel.");
            }
            var uv=painted.uv;if(uv.Length!=painted.vertexCount)throw new InvalidOperationException("Visitor palette UV count is incomplete.");
            foreach(var at in uv)
            {
                float swatch=at.x*24-.5f;
                if(float.IsNaN(swatch)||float.IsInfinity(swatch)||at.y!=.5f||swatch<-.00001f||swatch>23.00001f||Math.Abs(swatch-Math.Round(swatch))>.00001)
                    throw new InvalidOperationException("Visitor palette UV is outside the approved swatches.");
            }
        }
        /// <summary>Require every native skin bone, including its root, to belong
        /// to the same persistent prefab or owned instantiated hierarchy.</summary>
        public static void ValidateRigHierarchy(GameObject root)
        {
            if(root==null)throw new InvalidOperationException("Native visitor rig root missing.");
            var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(skins.Length!=1||skins[0].sharedMesh==null||skins[0].bones.Length==0
                ||skins[0].bones.Length!=skins[0].sharedMesh.bindposeCount||skins[0].rootBone==null
                ||!skins[0].rootBone.IsChildOf(root.transform)||skins[0].bones.Any(b=>b==null||!b.IsChildOf(root.transform)))
                throw new InvalidOperationException("Native visitor rig contains foreign or incomplete bones.");
        }
        private static string BonePath(Transform bone,Transform root)
        {if(bone==root)return string.Empty;var path=bone.name;while(bone.parent!=root){bone=bone.parent;if(bone==null)throw new InvalidOperationException("Foreign visitor bone.");path=bone.name+"/"+path;}return path;}
        public Entry Find(string modelId)
        {if(modelId==null)return null;if(index==null)Validate();return index.TryGetValue(modelId,out var e)?e:null;}
        public bool ContainsMesh(Mesh mesh)
        {if(mesh==null)return false;if(meshes==null)Validate();return meshes.Contains(mesh);}
        /// <summary>Called only on a presenter-owned exact native model instance
        /// after normal voxel adoption. Equipment calls omit the model identity.
        /// No runtime mesh, rig, material or clip asset is copied or mutated.</summary>
        internal void Apply(GameObject instance,string modelId)
        {
            if(instance==null)throw new ArgumentNullException(nameof(instance));
            var e=Find(modelId);if(e==null)return;
            if(ReferenceEquals(instance,e.SourcePrefab)||!instance.scene.IsValid()||!instance.scene.isLoaded)
                throw new InvalidOperationException("Visitor palette cannot mutate a borrowed prefab asset.");
            var skins=instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(skins.Length!=1||skins[0].sharedMesh!=e.Source)
                throw new InvalidOperationException("Exact owned visitor skin changed before palette adoption.");
            ValidateRigHierarchy(instance);
            var skin=skins[0];var native=e.SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if(BonePath(skin.rootBone,instance.transform)!=BonePath(native.rootBone,e.SourcePrefab.transform)
                ||!skin.bones.Select(b=>BonePath(b,instance.transform)).SequenceEqual(native.bones.Select(b=>BonePath(b,e.SourcePrefab.transform))))
                throw new InvalidOperationException("Owned visitor rig bone order differs from its native source.");
            var bounds=skin.localBounds;
            skin.sharedMesh=e.Painted;skin.sharedMaterial=Material;skin.localBounds=bounds;
        }
    }
}
