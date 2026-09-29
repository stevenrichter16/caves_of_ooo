using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Additive original glade art. Native entities alone own collision,
    /// harvesting, light and visibility; these assets contain no game behaviours.</summary>
    public sealed class ReferenceGladeVoxelLibrary : ScriptableObject
    {
        public const string ResourcePath="ReferenceGlade3D/Library";
        public const int VariantCount=4;
        public static IReadOnlyList<string> Families{get;}=Array.AsReadOnly(new[]{"ground","pale-reeds","green-grass","dark-ruin","low-wall","lit-wall","gravel","chest","barrel","mushroom-ring","cooking-fire","cooled-fire","fallen-beam"});
        [Serializable]public sealed class Entry{public string Id;public GameObject Prefab;public Mesh Mesh;public SpawnRing3DCatalog.Model Spec;}
        public Entry[] Entries;
        public static IReadOnlyList<string> ActorModelIds{get;}=Array.AsReadOnly(new[]{"ring-player","ring-sien","ring-nam","ring-snapjaw","ring-marlback-gleaner","ring-marlback-tunnelguard","ring-marlback-wallkeeper","ring-snapjaw-warlord"});
        [Serializable]public sealed class ActorPaint{public string ModelId;public Mesh Source,Painted;public bool AuthoredGeometry;}
        public ActorPaint[] ActorPaints;
        private Dictionary<Mesh,ActorPaint> actorPaints;
        public Material Material;
        private Dictionary<string,Entry> index;
        private static readonly string[] Ids=MakeIds();
        private static string[] MakeIds(){var ids=new string[Families.Count*VariantCount];int n=0;foreach(string family in Families)for(int v=0;v<4;v++)ids[n++]="reference-glade-"+family+"-"+v;return ids;}
        public static ReferenceGladeVoxelLibrary Load()=>Resources.Load<ReferenceGladeVoxelLibrary>(ResourcePath);
        private void OnValidate(){index=null;actorPaints=null;}
        public void Validate()
        {
            index=null;actorPaints=null;
            if(Material==null||Material.shader==null||!Material.HasProperty("_FogLight")||!Material.HasProperty("_Transient")||Material.GetTexture("_BaseMap")==null||Entries==null||Entries.Length!=Ids.Length)
                throw new InvalidOperationException("Reference glade kit requires52 models and its native fog-aware palette.");
            var next=new Dictionary<string,Entry>(StringComparer.Ordinal);
            foreach(var e in Entries)
            {
                if(e==null||e.Id==null||e.Mesh==null||!e.Mesh.isReadable||e.Mesh.vertexCount==0||e.Prefab==null||e.Spec==null||next.ContainsKey(e.Id))throw new InvalidOperationException("Invalid reference glade model.");
                var filter=e.Prefab.GetComponent<MeshFilter>();var renderer=e.Prefab.GetComponent<MeshRenderer>();var s=e.Spec;
                if(filter==null||filter.sharedMesh!=e.Mesh||renderer==null||renderer.sharedMaterials.Length!=1||renderer.sharedMaterial!=Material
                    ||e.Prefab.GetComponentsInChildren<Renderer>(true).Length!=1||e.Prefab.GetComponentsInChildren<Collider>(true).Length!=0||e.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length!=0
                    ||e.Prefab.transform.localPosition!=Vector3.zero||e.Prefab.transform.localRotation!=Quaternion.identity||e.Prefab.transform.localScale!=Vector3.one
                    ||s.id!=e.Id||s.path!="Assets/Resources/ReferenceGlade3D/"+e.Id+".prefab"||s.materialFamily!="reference-glade-palette"||s.rigged||s.rigFamily!="none"
                    ||s.kind!=(e.Id.StartsWith("reference-glade-ground-",StringComparison.Ordinal)?"ground":"entity")||s.boundsCenter!=e.Mesh.bounds.center||s.boundsSize!=e.Mesh.bounds.size
                    ||s.triangles!=(int)e.Mesh.GetIndexCount(0)/3||s.clips==null||s.clips.Length!=0||s.sockets==null||s.sockets.Length!=0)
                    throw new InvalidOperationException("Reference glade prefab/mesh/metadata mismatch: "+e.Id);
                next.Add(e.Id,e);
            }
            foreach(string id in Ids)if(!next.ContainsKey(id))throw new InvalidOperationException("Missing reference glade model: "+id);
            if(ActorPaints==null||ActorPaints.Length!=ActorModelIds.Count)throw new InvalidOperationException("Reference glade requires eight scoped actor paint assets.");
            var remaining=new HashSet<string>(ActorModelIds,StringComparer.Ordinal);var paints=new Dictionary<Mesh,ActorPaint>();
            foreach(var paint in ActorPaints)
            {
                if(paint==null||!remaining.Remove(paint.ModelId)||paint.Source==null||paint.Painted==null||paint.Source==paint.Painted
                    ||!paint.Source.isReadable||!paint.Painted.isReadable||paint.Painted.vertexCount==0
                    ||paint.Source.subMeshCount!=paint.Painted.subMeshCount||paint.Source.bindposeCount!=paint.Painted.bindposeCount
                    ||paint.Painted.uv.Length!=paint.Painted.vertexCount||paints.ContainsKey(paint.Source))
                    throw new InvalidOperationException("Invalid scoped reference glade actor paint.");
                bool humanoid=paint.ModelId=="ring-player"||paint.ModelId=="ring-sien"||paint.ModelId=="ring-nam";
                if(paint.AuthoredGeometry&&!humanoid)throw new InvalidOperationException("Only the three exact humanoids have local body geometry.");
                if(!paint.AuthoredGeometry&&(paint.Source.vertexCount!=paint.Painted.vertexCount||paint.Source.bounds!=paint.Painted.bounds))
                    throw new InvalidOperationException("UV-only actor paint must preserve source geometry.");
                var sourceBindposes=paint.Source.bindposes;var bindposes=paint.Painted.bindposes;
                for(int i=0;i<bindposes.Length;i++)if(sourceBindposes[i]!=bindposes[i])throw new InvalidOperationException("Scoped body changed its source bindposes.");
                if(paint.AuthoredGeometry)
                {
                    var weights=paint.Painted.boneWeights;
                    if(bindposes.Length==0||weights.Length!=paint.Painted.vertexCount)throw new InvalidOperationException("Incomplete local humanoid weights.");
                    foreach(var weight in weights)if(weight.weight0!=1||weight.weight1!=0||weight.weight2!=0||weight.weight3!=0||weight.boneIndex0<0||weight.boneIndex0>=bindposes.Length)
                        throw new InvalidOperationException("Local humanoids require valid rigid source bone weights.");
                }
                paints.Add(paint.Source,paint);
            }
            if(remaining.Count!=0)throw new InvalidOperationException("Missing scoped reference glade actor paint.");
            index=next;actorPaints=paints;
        }
        /// <summary>Swaps only borrowed adopted-mesh references and material on an
        /// owned model instance. Geometry/rigs remain persistent; there are no
        /// runtime mesh copies. Unknown actors and equipment keep their own paint.</summary>
        internal void ApplyActorPaint(GameObject ownedInstance)
        {
            if(ownedInstance==null)throw new ArgumentNullException(nameof(ownedInstance));
            if(actorPaints==null)Validate();
            foreach(var skin in ownedInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if(skin.sharedMesh!=null&&actorPaints.TryGetValue(skin.sharedMesh,out var paint))
                {
                    var envelope=skin.localBounds;
                    skin.sharedMesh=paint.Painted;skin.sharedMaterial=Material;
                    // Retain the imported animation envelope as well as the new
                    // bind-pose shape. Bounds are never tightened around one frame.
                    if(paint.AuthoredGeometry){envelope.Encapsulate(paint.Painted.bounds.min);envelope.Encapsulate(paint.Painted.bounds.max);skin.localBounds=envelope;}
                }
        }
        internal bool IsAuthoredHumanoidMesh(Mesh mesh)
        {
            if(mesh==null)return false;if(actorPaints==null)Validate();
            foreach(var paint in ActorPaints)if(paint.AuthoredGeometry&&paint.Painted==mesh)return true;
            return false;
        }
        public Entry Find(string id){if(id==null||!id.StartsWith("reference-glade-",StringComparison.Ordinal))return null;if(index==null)Validate();return index.TryGetValue(id,out var entry)?entry:null;}
        public static string ModelId(string family,int variant)
        {
            if(variant<0||variant>=4)throw new ArgumentOutOfRangeException(nameof(variant));
            for(int i=0;i<Families.Count;i++)if(Families[i]==family)return Ids[i*4+variant];
            throw new ArgumentException("Unknown reference glade family.",nameof(family));
        }
        /// <summary>Saved visual choice must agree with current native identity.
        /// Authority to use this profile is separately checked by the zone recipe.</summary>
        public static string Family(Entity entity)
        {
            string bp=entity?.BlueprintName,visual=entity?.GetPart<RenderPart>()?.VisualID;
            switch(visual)
            {
                case "reference-glade-ground":return bp=="Grass"?"ground":null;
                case "reference-glade-pale-reeds":return bp=="Reeds"?"pale-reeds":null;
                case "reference-glade-green-grass":return bp=="Bush"?"green-grass":null;
                case "reference-glade-dark-ruin":return bp=="Wall"||bp=="BrokenColumn"?"dark-ruin":null;
                case "reference-glade-low-wall":return bp=="Wall"?"low-wall":null;
                case "reference-glade-lit-wall":return bp=="Wall"||bp=="GlowQuartzVein"?"lit-wall":null;
                case "reference-glade-gravel":return bp=="Rubble"?"gravel":null;
                default:return null;
            }
        }
    }
}
