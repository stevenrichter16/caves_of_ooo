using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Optional exact original prepared grain. Gameplay owns food quantity,
    /// cooking and movement; this library only observes the current owner.</summary>
    public sealed class SpreadCooking3DLibrary : ScriptableObject
    {
        public const string ResourcePath="SpreadCooking3D/Library";
        public const string Toasted="spread-toasted-emberwheat";
        public const string ReviewedCatalogSha256="80daba812e6aca05aa958054f1c50b2e4ff1609b50c87cf13bd3c42d8a3b527f";
        [Serializable] public sealed class Entry
        { public string Id; public GameObject Prefab; public Mesh Mesh; public Material Material; public SpawnRing3DCatalog.Model Spec; }
        public string SourceSha256;
        public Entry[] Entries;
        Dictionary<string,Entry> index;
        public static SpreadCooking3DLibrary Load()=>Resources.Load<SpreadCooking3DLibrary>(ResourcePath);
        public static bool IsModelId(string id)=>id==Toasted;
        void OnValidate(){index=null;}
        public Entry Find(string id)
        {if(!IsModelId(id))return null;if(index==null)Validate();return index.TryGetValue(id,out var e)?e:null;}
        public void Validate()
        {
            index=null;var palette=ReferenceGladeVoxelLibrary.Load()?.Material;
            if(palette==null||SourceSha256!=ReviewedCatalogSha256||Entries==null||Entries.Length!=1)
                throw new InvalidOperationException("One reviewed original prepared-grain form and approved palette required.");
            var next=new Dictionary<string,Entry>(StringComparer.Ordinal);var meshes=new HashSet<Mesh>();var prefabs=new HashSet<GameObject>();
            foreach(var e in Entries)
            {
                if(e==null||!IsModelId(e.Id)||next.ContainsKey(e.Id)||e.Prefab==null||!prefabs.Add(e.Prefab)
                    ||e.Mesh==null||!meshes.Add(e.Mesh)||!e.Mesh.isReadable||e.Material!=palette||e.Spec==null)
                    throw new InvalidOperationException("Invalid exact original prepared-grain asset identity.");
                var root=e.Prefab;var mesh=e.Mesh;var renderer=root.GetComponent<MeshRenderer>();
                if(!root.activeSelf||root.transform.childCount!=0||root.GetComponents<Component>().Length!=3
                    ||root.transform.localPosition!=Vector3.zero||root.transform.localRotation!=Quaternion.identity||root.transform.localScale!=Vector3.one
                    ||root.GetComponent<MeshFilter>()?.sharedMesh!=mesh||renderer==null||!renderer.enabled||renderer.forceRenderingOff
                    ||renderer.sharedMaterials.Length!=1||renderer.sharedMaterial!=palette
                    ||renderer.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.On||!renderer.receiveShadows
                    ||mesh.vertexCount<100||mesh.vertexCount>4096||mesh.subMeshCount!=1||mesh.GetTopology(0)!=MeshTopology.Triangles
                    ||mesh.GetIndexCount(0)!=480||mesh.bindposeCount!=0||mesh.boneWeights.Length!=0||mesh.blendShapeCount!=0)
                    throw new InvalidOperationException("Prepared-grain output must be one complete inert static renderer.");
                var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;
                if(normals.Length!=vertices.Length||uv.Length!=vertices.Length)throw new InvalidOperationException("Incomplete prepared-grain geometry buffers.");
                var bounds=new Bounds(vertices[0],Vector3.zero);
                for(int i=0;i<vertices.Length;i++)
                {
                    var v=vertices[i];var n=normals[i];var t=uv[i];
                    if(!Finite(v.x)||!Finite(v.y)||!Finite(v.z)||Mathf.Abs(v.x)>.501f||Mathf.Abs(v.z)>.501f||v.y<-.001f||v.y>.101f
                        ||!Finite(n.x)||!Finite(n.y)||!Finite(n.z)||n.sqrMagnitude<.9f||n.sqrMagnitude>1.1f
                        ||!Finite(t.x)||!Finite(t.y)||t.x<0||t.x>1||Mathf.Abs(t.y-.5f)>.00001f)
                        throw new InvalidOperationException("Invalid prepared-grain palette, normals or one-cell envelope.");
                    float swatch=t.x*24-.5f;if(Mathf.Abs(swatch-Mathf.Round(swatch))>.0001f)throw new InvalidOperationException("Prepared-grain UV misses an approved palette centre.");
                    bounds.Encapsulate(v);
                }
                if(bounds.max.y<.055f||bounds.size.x<.4f||bounds.size.x>.62f||bounds.size.z<.25f||bounds.size.z>.5f
                    ||(bounds.center-mesh.bounds.center).sqrMagnitude>.000001f||(bounds.size-mesh.bounds.size).sqrMagnitude>.000001f)
                    throw new InvalidOperationException("Original low prepared-grain shape or actual culling bounds differ.");
                var spec=e.Spec;
                if(spec.id!=e.Id||spec.sourceBlueprint!="ToastedEmberwheat"||spec.path!="Assets/Resources/SpreadCooking3D/"+e.Id+".prefab"
                    ||spec.kind!="entity"||spec.rigged||spec.rigFamily!=""||spec.materialFamily!="reference-glade-palette"||spec.triangles!=160
                    ||spec.clips==null||spec.clips.Length!=0||spec.sockets==null||spec.sockets.Length!=0
                    ||(spec.boundsCenter-bounds.center).sqrMagnitude>.000001f||(spec.boundsSize-bounds.size).sqrMagnitude>.000001f)
                    throw new InvalidOperationException("Original prepared-grain metadata differs from actual source geometry.");
                next.Add(e.Id,e);
            }
            index=next;
        }
        static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        internal static SpawnRing3DRecipe Refine(Zone zone,Entity owner,SpawnRing3DRecipe native)
        {
            if(owner?.BlueprintName!="ToastedEmberwheat"||!SpreadPresentationScope.IsActive(zone)||!ReferenceEquals(native.Owner,owner)
                ||native.Failure!="unmodeled-native-blueprint")return native;
            var cell=zone.GetEntityCell(owner);var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();
            var food=owner.GetPart<FoodPart>();var stack=owner.GetPart<StackerPart>();
            if(cell==null||!cell.Objects.Contains(owner)||render==null||!ReferenceEquals(render.ParentEntity,owner)||!render.Visible
                ||physics==null||!ReferenceEquals(physics.ParentEntity,owner)||!physics.Takeable||physics.Solid||physics.InInventory!=null||physics.Equipped!=null
                ||food==null||!ReferenceEquals(food.ParentEntity,owner)||stack==null||!ReferenceEquals(stack.ParentEntity,owner)
                ||stack.StackCount<=0||stack.StackCount>stack.MaxStack
                ||!owner.HasTag("Item")||owner.HasTag("Creature")||owner.HasTag("Player")||owner.HasTag("Natural")
                ||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()||owner.HasPart<FellingScenePropPart>()
                ||render.ColorString!="&y"||render.RenderString!="%"
                ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants))return native;
            if(Load()?.Find(Toasted)==null)return native;
            return new SpawnRing3DRecipe(owner,Toasted,native.ComponentId,Village3DProjection.CellCentre(cell.X,cell.Y),true,false);
        }
    }
}
