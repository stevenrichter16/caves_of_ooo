using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Exact persistent original visitor bodies. Gameplay membership,
    /// visibility and receiving-zone authority are checked before refinement.</summary>
    public sealed class SpreadVisitorCreatureLibrary : ScriptableObject
    {
        public const string ResourcePath="SpreadVisitorCreatures/Library";
        [Serializable] public sealed class Entry
        { public string Id; public GameObject Prefab; public Mesh Mesh; public SpawnRing3DCatalog.Model Spec; }
        public Entry[] Entries;
        public Material Material;
        public Texture2D Palette;
        private Dictionary<string,Entry> index;
        private HashSet<Mesh> meshes;
        public static SpreadVisitorCreatureLibrary Load()=>Resources.Load<SpreadVisitorCreatureLibrary>(ResourcePath);
        private void OnValidate(){index=null;meshes=null;}
        public Entry Find(string id)
        { if(SpreadVisitorCreatureSource.Find(id)==null)return null;if(index==null)Validate();return index.TryGetValue(id,out var e)?e:null; }
        public bool ContainsMesh(Mesh mesh)
        { if(mesh==null)return false;if(meshes==null)Validate();return meshes.Contains(mesh); }
        public void Validate()
        {
            index=null;meshes=null;
            var glade=ReferenceGladeVoxelLibrary.Load();
            if(Entries==null||Entries.Length!=29||glade==null||Material==null||Material==glade.Material
                ||Material.shader!=glade.Material.shader||Palette==null||!Palette.isReadable||Palette.width!=30||Palette.height!=1
                ||Material.GetTexture("_BaseMap")!=Palette||Material.GetColor("_BaseColor")!=Color.white)
                throw new InvalidOperationException("Complete original visitor library and scoped semantic palette required.");
            for(int i=0;i<30;i++)
                if(!ColorUtility.TryParseHtmlString(SpreadVisitorCreatureSource.Palette[i],out var c)||Vector4.Distance(c,Palette.GetPixel(i,0))>.00001f)
                    throw new InvalidOperationException("Visitor palette differs from approved source swatches.");
            var next=new Dictionary<string,Entry>(StringComparer.Ordinal);var used=new HashSet<Mesh>();var prefabs=new HashSet<GameObject>();
            foreach(var e in Entries)
            {
                var authored=SpreadVisitorCreatureSource.Find(e?.Id);
                if(authored==null||e.Spec==null||e.Id!=e.Spec.id||next.ContainsKey(e.Id)||e.Spec.sourceBlueprint!=authored.Blueprint
                    ||e.Spec.rigFamily!=authored.RigFamily||e.Spec.kind!="actor"||!e.Spec.rigged||e.Prefab==null||!prefabs.Add(e.Prefab)
                    ||e.Mesh==null||!used.Add(e.Mesh)||!e.Mesh.isReadable||e.Mesh.vertexCount<100||e.Mesh.vertexCount>24000
                    ||e.Mesh.subMeshCount!=1||e.Mesh.GetTopology(0)!=MeshTopology.Triangles||e.Mesh.GetIndexCount(0)==0)
                    throw new InvalidOperationException("Invalid original visitor identity or geometry.");
                var skins=e.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);var animators=e.Prefab.GetComponentsInChildren<Animator>(true);
                if(skins.Length!=1||skins[0].sharedMesh!=e.Mesh||skins[0].sharedMaterials.Length!=1||skins[0].sharedMaterial!=Material
                    ||animators.Length!=1||animators[0].avatar==null||!animators[0].avatar.isValid||animators[0].applyRootMotion
                    ||animators[0].runtimeAnimatorController==null||e.Prefab.GetComponentsInChildren<Collider>(true).Length!=0
                    ||e.Prefab.GetComponentsInChildren<Rigidbody>(true).Length!=0||e.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)
                    throw new InvalidOperationException("Original visitor rig/material/component contract failed: "+e.Id);
                var skin=skins[0];var bones=skin.bones;var weights=e.Mesh.boneWeights;
                if(skin.rootBone==null||!skin.rootBone.IsChildOf(e.Prefab.transform)||bones.Length!=authored.Bones.Length
                    ||e.Mesh.bindposeCount!=bones.Length||weights.Length!=e.Mesh.vertexCount)
                    throw new InvalidOperationException("Incomplete original visitor skin.");
                var expected=new HashSet<string>(authored.Bones,StringComparer.Ordinal);
                foreach(var bone in bones)
                    if(bone==null||!bone.IsChildOf(e.Prefab.transform)||!expected.Remove(bone.name))
                        throw new InvalidOperationException("Foreign, duplicate or missing visitor anatomy bone.");
                if(expected.Count!=0)throw new InvalidOperationException("Missing visitor anatomy.");
                foreach(var weight in weights)
                    if(weight.weight0!=1||weight.weight1!=0||weight.weight2!=0||weight.weight3!=0||weight.boneIndex0<0||weight.boneIndex0>=bones.Length)
                        throw new InvalidOperationException("Original cuboids require one actual owning bone.");
                var uv=e.Mesh.uv;
                if(uv.Length!=e.Mesh.vertexCount)throw new InvalidOperationException("Missing visitor palette coordinates.");
                foreach(var at in uv)
                {
                    if(!SpreadVisitorCreatureSource.IsPaletteCoordinate(at.x,at.y))
                        throw new InvalidOperationException("Visitor UV does not select an exact authored swatch.");
                }
                foreach(string name in authored.Sockets)
                {
                    int count=0;
                    foreach(var t in e.Prefab.GetComponentsInChildren<Transform>(true))
                        if(t.name==name){count++;if(!t.IsChildOf(skin.rootBone))throw new InvalidOperationException("Equipment socket must belong to the real rig.");}
                    if(count!=1)throw new InvalidOperationException("Required original equipment socket absent or repeated.");
                }
                var clips=animators[0].runtimeAnimatorController.animationClips;
                if(clips.Length!=5)throw new InvalidOperationException("Five real visitor clips required.");
                foreach(string name in SpreadVisitorCreatureSource.Clips)
                {int count=0;foreach(var clip in clips)if(clip!=null&&clip.name==name&&clip.length>0)count++;if(count!=1)throw new InvalidOperationException("Missing actual visitor motion clip.");}
                next.Add(e.Id,e);
            }
            index=next;meshes=used;
        }
        internal static SpawnRing3DRecipe Refine(Zone zone,Entity owner,SpawnRing3DRecipe native)
        {
            // The southern crossing retains the same original native Bandfrog
            // body. This narrow reuse admits no other visitor or new population.
            bool districtBandfrog=zone!=null&&SoddenDistrictPlan.IsSupportedZone(zone.ZoneID)&&owner?.BlueprintName=="Bandfrog";
            if((!SpreadPresentationScope.IsActive(zone)&&!districtBandfrog)||owner==null||!ReferenceEquals(native.Owner,owner)
                ||native.Failure!=null&&native.Failure!="unmodeled-native-blueprint")return native;
            var spec=SpreadVisitorCreatureSource.ForBlueprint(owner.BlueprintName);
            if(spec==null)return native;
            var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();var brain=owner.GetPart<BrainPart>();var cell=zone.GetEntityCell(owner);
            if(cell==null||!cell.Objects.Contains(owner)||render==null||!render.Visible||!ReferenceEquals(render.ParentEntity,owner)||render.RenderString!=spec.Glyph
                ||physics==null||!ReferenceEquals(physics.ParentEntity,owner)||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
                ||brain==null||!ReferenceEquals(brain.ParentEntity,owner)||!owner.HasTag("Creature")||owner.HasTag("Item")
                ||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()
                ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants))return native;
            if(districtBandfrog&&(owner.SpatialZone!=zone||cell.ParentZone!=zone||!physics.Solid||render.ColorString!="&R"
                ||owner.GetPart<CausticSkinPart>()?.ParentEntity!=owner))return native;
            // The source appearance is retained through native effects, dormancy
            // and faction changes. This read does not advance or replace goals.
            return new SpawnRing3DRecipe(owner,spec.Id,native.ComponentId,Village3DProjection.CellCentre(cell.X,cell.Y),true,false,quarterTurns:native.QuarterTurns);
        }
    }
}
