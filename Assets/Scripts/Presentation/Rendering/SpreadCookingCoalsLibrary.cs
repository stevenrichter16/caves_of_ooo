using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Optional original residual coals. Gameplay owns heat and fuel; this
    /// library only observes the exact current source and never changes it.</summary>
    public sealed class SpreadCookingCoalsLibrary : ScriptableObject
    {
        public const string ResourcePath="SpreadCookingCoals3D/Library";
        public const string Hot="spread-cooking-coals-hot",Cooled="spread-cooking-coals-cooled";
        public const string ReviewedCatalogSha256="db0d575f932d26995e99efddc4718a95500425accc76c3764e190587a68fb950";
        [Serializable] public sealed class Entry
        { public string Id; public GameObject Prefab; public Mesh Mesh; public Material Material; public SpawnRing3DCatalog.Model Spec; }
        public string SourceSha256;
        public Entry[] Entries;
        Dictionary<string,Entry> index;
        public static SpreadCookingCoalsLibrary Load()=>Resources.Load<SpreadCookingCoalsLibrary>(ResourcePath);
        public static bool IsModelId(string id)=>id==Hot||id==Cooled;
        void OnValidate(){index=null;}
        public Entry Find(string id)
        {if(!IsModelId(id))return null;if(index==null)Validate();return index.TryGetValue(id,out var e)?e:null;}
        public void Validate()
        {
            index=null;var palette=ReferenceGladeVoxelLibrary.Load()?.Material;
            if(palette==null||SourceSha256!=ReviewedCatalogSha256||Entries==null||Entries.Length!=2)
                throw new InvalidOperationException("Two reviewed original residual-coal forms and approved palette required.");
            var next=new Dictionary<string,Entry>(StringComparer.Ordinal);var meshes=new HashSet<Mesh>();var prefabs=new HashSet<GameObject>();
            foreach(var e in Entries)
            {
                if(e==null||!IsModelId(e.Id)||next.ContainsKey(e.Id)||e.Prefab==null||!prefabs.Add(e.Prefab)
                    ||e.Mesh==null||!meshes.Add(e.Mesh)||!e.Mesh.isReadable||e.Material!=palette||e.Spec==null)
                    throw new InvalidOperationException("Invalid exact original residual-coal asset identity.");
                var root=e.Prefab;var mesh=e.Mesh;var renderer=root.GetComponent<MeshRenderer>();
                if(!root.activeSelf||root.transform.childCount!=0||root.GetComponents<Component>().Length!=3
                    ||root.transform.localPosition!=Vector3.zero||root.transform.localRotation!=Quaternion.identity||root.transform.localScale!=Vector3.one
                    ||root.GetComponent<MeshFilter>()?.sharedMesh!=mesh||renderer==null||!renderer.enabled||renderer.forceRenderingOff
                    ||renderer.sharedMaterials.Length!=1||renderer.sharedMaterial!=palette
                    ||renderer.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.On||!renderer.receiveShadows
                    ||mesh.vertexCount<100||mesh.vertexCount>4096||mesh.subMeshCount!=1||mesh.GetTopology(0)!=MeshTopology.Triangles
                    ||mesh.GetIndexCount(0)!=780||mesh.bindposeCount!=0||mesh.boneWeights.Length!=0||mesh.blendShapeCount!=0)
                    throw new InvalidOperationException("Residual-coal output must be one complete inert static renderer.");
                var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;
                if(normals.Length!=vertices.Length||uv.Length!=vertices.Length)throw new InvalidOperationException("Incomplete residual-coal geometry buffers.");
                var bounds=new Bounds(vertices[0],Vector3.zero);
                for(int i=0;i<vertices.Length;i++)
                {
                    var v=vertices[i];var n=normals[i];var t=uv[i];
                    if(!Finite(v.x)||!Finite(v.y)||!Finite(v.z)||Mathf.Abs(v.x)>.46f||Mathf.Abs(v.z)>.46f||v.y<-.00001f||v.y>.16f
                        ||!Finite(n.x)||!Finite(n.y)||!Finite(n.z)||n.sqrMagnitude<.9f||n.sqrMagnitude>1.1f
                        ||!Finite(t.x)||!Finite(t.y)||t.x<0||t.x>1||Mathf.Abs(t.y-.5f)>.00001f)
                        throw new InvalidOperationException("Invalid residual-coal palette, normals or one-cell envelope.");
                    float swatch=t.x*24-.5f;if(Mathf.Abs(swatch-Mathf.Round(swatch))>.0001f)throw new InvalidOperationException("Residual-coal UV misses an approved palette centre.");
                    bounds.Encapsulate(v);
                }
                if(bounds.max.y<.1f||bounds.size.x<.8f||bounds.size.x>.9f||bounds.size.z<.68f||bounds.size.z>.76f
                    ||(bounds.center-mesh.bounds.center).sqrMagnitude>.000001f||(bounds.size-mesh.bounds.size).sqrMagnitude>.000001f)
                    throw new InvalidOperationException("Original low residual-coal shape or actual culling bounds differ.");
                var spec=e.Spec;
                if(spec.id!=e.Id||spec.sourceBlueprint!="SpreadCookingCoals"||spec.path!="Assets/Resources/SpreadCookingCoals3D/"+e.Id+".prefab"
                    ||spec.kind!="entity"||spec.rigged||spec.rigFamily!=""||spec.materialFamily!="reference-glade-palette"||spec.triangles!=260
                    ||spec.clips==null||spec.clips.Length!=0||spec.sockets==null||spec.sockets.Length!=0
                    ||(spec.boundsCenter-bounds.center).sqrMagnitude>.000001f||(spec.boundsSize-bounds.size).sqrMagnitude>.000001f)
                    throw new InvalidOperationException("Original residual-coal metadata differs from actual source geometry.");
                next.Add(e.Id,e);
            }
            var hot=next[Hot].Mesh;var cooled=next[Cooled].Mesh;
            if(!hot.vertices.SequenceEqual(cooled.vertices)||!hot.triangles.SequenceEqual(cooled.triangles)
                ||!hot.normals.SequenceEqual(cooled.normals)||hot.bounds!=cooled.bounds||hot.uv.SequenceEqual(cooled.uv))
                throw new InvalidOperationException("Thermal pair must keep exact common geometry with distinct face palette.");
            index=next;
        }
        static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        internal static SpawnRing3DRecipe Refine(Zone zone,Entity owner,SpawnRing3DRecipe native)
        {
            if(owner?.BlueprintName!="SpreadCookingCoals"||!SpreadPresentationScope.IsActive(zone)||!ReferenceEquals(native.Owner,owner)
                ||native.Failure!="unmodeled-native-blueprint")return native;
            var cell=zone.GetEntityCell(owner);var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();
            var campfire=owner.GetPart<CampfirePart>();var thermal=owner.GetPart<ThermalPart>();var fuel=owner.GetPart<FuelPart>();
            if(!ReferenceEquals(owner.SpatialZone,zone)||cell==null||!ReferenceEquals(cell.ParentZone,zone)||!cell.Objects.Contains(owner)||render==null||!ReferenceEquals(render.ParentEntity,owner)||!render.Visible
                ||physics==null||!ReferenceEquals(physics.ParentEntity,owner)||physics.Takeable||physics.Solid||physics.InInventory!=null||physics.Equipped!=null
                ||campfire==null||!ReferenceEquals(campfire.ParentEntity,owner)||!campfire.FiniteCooking||campfire.AllowRest
                ||thermal==null||!ReferenceEquals(thermal.ParentEntity,owner)||!Finite(thermal.Temperature)
                ||fuel==null||!ReferenceEquals(fuel.ParentEntity,owner)||!Finite(fuel.FuelMass)||fuel.FuelMass<0
                ||owner.HasTag("Item")||owner.HasTag("Creature")||owner.HasTag("Player")||owner.HasTag("Natural")
                ||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()||owner.HasPart<FellingScenePropPart>()
                ||render.ColorString!="&K"||render.RenderString!="*"
                ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants))return native;
            // Fuel exhaustion does not erase physical heat. CookingService and
            // the focused reader independently decide whether cooking is usable.
            string model=thermal.Temperature>=CookingService.MinimumFiniteCookingTemperature?Hot:Cooled;
            if(Load()?.Find(model)==null)return native;
            return new SpawnRing3DRecipe(owner,model,native.ComponentId,Village3DProjection.CellCentre(cell.X,cell.Y),true,false);
        }
    }
}
