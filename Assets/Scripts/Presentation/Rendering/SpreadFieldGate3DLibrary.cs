using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Optional exact two-state field timber gate. DoorPart owns state,
    /// collision and saved axis; this library only observes the current owner.</summary>
    public sealed class SpreadFieldGate3DLibrary : ScriptableObject
    {
        public const string ResourcePath="SpreadFieldGate3D/Library";
        public const string Closed="spread-field-gate-closed", Open="spread-field-gate-open";
        public const string ReviewedCatalogSha256="c413dd9390a4ec23347c9042f8f2127e9723300c9899abfa22fa6d96135171b3";
        [Serializable] public sealed class Entry
        { public string Id; public GameObject Prefab; public Mesh Mesh; public Material Material; public SpawnRing3DCatalog.Model Spec; }
        public string SourceSha256;
        public Entry[] Entries;
        Dictionary<string,Entry> index;
        public static SpreadFieldGate3DLibrary Load()=>Resources.Load<SpreadFieldGate3DLibrary>(ResourcePath);
        public static bool IsModelId(string id)=>id==Closed||id==Open;
        void OnValidate(){index=null;}
        public Entry Find(string id)
        {if(!IsModelId(id))return null;if(index==null)Validate();return index.TryGetValue(id,out var e)?e:null;}
        public void Validate()
        {
            index=null;var palette=ReferenceGladeVoxelLibrary.Load()?.Material;
            if(palette==null||SourceSha256!=ReviewedCatalogSha256||Entries==null||Entries.Length!=2)
                throw new InvalidOperationException("Two reviewed original field-gate states and approved palette required.");
            var next=new Dictionary<string,Entry>(StringComparer.Ordinal);var meshes=new HashSet<Mesh>();var prefabs=new HashSet<GameObject>();
            foreach(var e in Entries)
            {
                if(e==null||!IsModelId(e.Id)||next.ContainsKey(e.Id)||e.Prefab==null||!prefabs.Add(e.Prefab)
                    ||e.Mesh==null||!meshes.Add(e.Mesh)||!e.Mesh.isReadable||e.Material!=palette||e.Spec==null)
                    throw new InvalidOperationException("Invalid exact original field-gate asset identity.");
                var root=e.Prefab;var mesh=e.Mesh;var renderer=root.GetComponent<MeshRenderer>();
                if(!root.activeSelf||root.transform.childCount!=0||root.GetComponents<Component>().Length!=3
                    ||root.transform.localPosition!=Vector3.zero||root.transform.localRotation!=Quaternion.identity||root.transform.localScale!=Vector3.one
                    ||root.GetComponent<MeshFilter>()?.sharedMesh!=mesh||renderer==null||!renderer.enabled||renderer.forceRenderingOff
                    ||renderer.sharedMaterials.Length!=1||renderer.sharedMaterial!=palette
                    ||renderer.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.On||!renderer.receiveShadows
                    ||mesh.vertexCount<100||mesh.vertexCount>4096||mesh.subMeshCount!=1||mesh.GetTopology(0)!=MeshTopology.Triangles
                    ||mesh.GetIndexCount(0)!=468||mesh.bindposeCount!=0||mesh.boneWeights.Length!=0||mesh.blendShapeCount!=0)
                    throw new InvalidOperationException("Field-gate output must be one complete inert static renderer.");
                var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;
                if(normals.Length!=vertices.Length||uv.Length!=vertices.Length)throw new InvalidOperationException("Incomplete timber geometry buffers.");
                var bounds=new Bounds(vertices[0],Vector3.zero);
                for(int i=0;i<vertices.Length;i++)
                {
                    var v=vertices[i];var n=normals[i];var t=uv[i];
                    if(!Finite(v.x)||!Finite(v.y)||!Finite(v.z)||Mathf.Abs(v.x)>.491f||Mathf.Abs(v.z)>.491f||v.y<-.001f||v.y>.851f
                        ||!Finite(n.x)||!Finite(n.y)||!Finite(n.z)||n.sqrMagnitude<.9f||n.sqrMagnitude>1.1f
                        ||!Finite(t.x)||!Finite(t.y)||t.x<0||t.x>1||Mathf.Abs(t.y-.5f)>.00001f)
                        throw new InvalidOperationException("Invalid timber palette, normals or one-cell envelope.");
                    float swatch=t.x*24-.5f;if(Mathf.Abs(swatch-Mathf.Round(swatch))>.0001f)throw new InvalidOperationException("Timber UV misses an approved palette centre.");
                    bounds.Encapsulate(v);
                }
                if(bounds.max.y<.7f||(bounds.center-mesh.bounds.center).sqrMagnitude>.000001f||(bounds.size-mesh.bounds.size).sqrMagnitude>.000001f
                    ||e.Id==Open&&bounds.size.z<=.65f||e.Id==Closed&&bounds.size.z>=.25f)
                    throw new InvalidOperationException("Original open/closed timber shape or actual culling bounds differ.");
                var spec=e.Spec;
                if(spec.id!=e.Id||spec.sourceBlueprint!="SpreadFieldGate"||spec.path!="Assets/Resources/SpreadFieldGate3D/"+e.Id+".prefab"
                    ||spec.kind!="entity"||spec.rigged||spec.rigFamily!=""||spec.materialFamily!="reference-glade-palette"||spec.triangles!=156
                    ||spec.clips==null||spec.clips.Length!=0||spec.sockets==null||spec.sockets.Length!=0
                    ||(spec.boundsCenter-bounds.center).sqrMagnitude>.000001f||(spec.boundsSize-bounds.size).sqrMagnitude>.000001f)
                    throw new InvalidOperationException("Original field-gate metadata differs from actual source geometry.");
                next.Add(e.Id,e);
            }
            index=next;
        }
        static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        internal static SpawnRing3DRecipe Refine(Zone zone,Entity owner,SpawnRing3DRecipe native)
        {
            if(owner?.BlueprintName!="SpreadFieldGate"||!SpreadPresentationScope.IsActive(zone)||!ReferenceEquals(native.Owner,owner)
                ||native.Failure!="unmodeled-native-blueprint")return native;
            var cell=zone.GetEntityCell(owner);var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();var door=owner.GetPart<DoorPart>();
            var structural=owner.GetPart<DestructiblePart>();
            if(cell==null||!cell.Objects.Contains(owner)||render==null||!ReferenceEquals(render.ParentEntity,owner)||!render.Visible
                ||physics==null||!ReferenceEquals(physics.ParentEntity,owner)||physics.Takeable||physics.Solid||physics.InInventory!=null||physics.Equipped!=null
                ||door==null||!ReferenceEquals(door.ParentEntity,owner)||door.QuarterTurns<0||door.QuarterTurns>3
                ||structural==null||!ReferenceEquals(structural.ParentEntity,owner)||structural.Gone
                ||owner.HasTag("Creature")||owner.HasTag("Player")||owner.HasTag("Item")||owner.HasTag("Solid")
                ||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()||owner.HasPart<FellingScenePropPart>()
                ||owner.HasPart<MorrowfastDoorPart>()||owner.HasPart<SealedLibraryBarrierPart>()
                ||render.ColorString!="&w"||render.RenderString!=(door.IsClosed?"+":"/")
                ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants))return native;
            string id=door.IsClosed?Closed:Open;if(Load()?.Find(id)==null)return native;
            return new SpawnRing3DRecipe(owner,id,native.ComponentId,Village3DProjection.CellCentre(cell.X,cell.Y),false,false,quarterTurns:door.QuarterTurns);
        }
    }
}
