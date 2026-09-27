using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Original authored cuboid bodies, borrowed persistently by owned
    /// receiving-biome views. This initial library covers three animal species.</summary>
    public sealed class SpreadBiomeActorLibrary:ScriptableObject
    {
        public const string ResourcePath="SpreadBiome3D/ActorLibrary";
        [Serializable] public sealed class Entry
        {public string Id;public GameObject Prefab;public Mesh Mesh;public SpawnRing3DCatalog.Model Spec;}
        public Entry[] Entries;
        private Dictionary<string,Entry> index;
        private static readonly string[] Blueprints={"Magpie","PetDog","Viper"};
        private static readonly string[] ClipNames={"Idle","Walk","Interact","Attack","Hit"};
        public static SpreadBiomeActorLibrary Load()=>Resources.Load<SpreadBiomeActorLibrary>(ResourcePath);
        private void OnValidate()=>index=null;
        public void Validate()
        {
            if(Entries==null||Entries.Length!=3)throw new InvalidOperationException("Three original Spread animal bodies required.");
            var next=new Dictionary<string,Entry>(StringComparer.Ordinal);
            var palette=ReferenceGladeVoxelLibrary.Load()?.Material;
            if(palette==null)throw new InvalidOperationException("Approved native palette is unavailable.");
            foreach(var e in Entries)
            {
                if(e==null||e.Spec==null||e.Id!=ModelId(e.Spec.sourceBlueprint)||e.Id!=e.Spec.id||next.ContainsKey(e.Id)
                    ||e.Mesh==null||!e.Mesh.isReadable||e.Mesh.vertexCount<100||e.Prefab==null||!e.Spec.rigged||e.Spec.kind!="actor")
                    throw new InvalidOperationException("Invalid original animal entry.");
                var skins=e.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);var animators=e.Prefab.GetComponentsInChildren<Animator>(true);
                if(skins.Length!=1||skins[0].sharedMesh!=e.Mesh||skins[0].sharedMaterials.Length!=1||skins[0].sharedMaterial!=palette
                    ||animators.Length!=1||animators[0].avatar==null||!animators[0].avatar.isValid||animators[0].applyRootMotion
                    ||animators[0].runtimeAnimatorController==null||e.Prefab.GetComponentsInChildren<Collider>(true).Length!=0
                    ||e.Prefab.GetComponentsInChildren<Rigidbody>(true).Length!=0)
                    throw new InvalidOperationException("Original animal rig/material/ownership contract failed: "+e.Id);
                var clips=animators[0].runtimeAnimatorController.animationClips;
                if(clips.Length!=ClipNames.Length)throw new InvalidOperationException("Five real native clips required.");
                foreach(var name in ClipNames)
                {int count=0;foreach(var clip in clips)if(clip!=null&&clip.name==name&&clip.length>0)count++;if(count!=1)throw new InvalidOperationException("Missing original animal clip: "+name);}
                var bones=skins[0].bones;var weights=e.Mesh.boneWeights;
                if(bones.Length==0||e.Mesh.bindposeCount!=bones.Length||weights.Length!=e.Mesh.vertexCount)
                    throw new InvalidOperationException("Original animal bone buffers incomplete.");
                foreach(var bone in bones)if(bone==null)throw new InvalidOperationException("Missing original animal bone.");
                foreach(var w in weights)if(w.weight0!=1||w.weight1!=0||w.weight2!=0||w.weight3!=0||w.boneIndex0<0||w.boneIndex0>=bones.Length)
                    throw new InvalidOperationException("Authored cuboids require exact rigid bone ownership.");
                next.Add(e.Id,e);
            }
            foreach(var bp in Blueprints)if(!next.ContainsKey(ModelId(bp)))throw new InvalidOperationException("Missing original animal: "+bp);
            index=next;
        }
        public Entry Find(string id)
        {if(id==null||!id.StartsWith("spread-biome-",StringComparison.Ordinal))return null;if(index==null)Validate();return index.TryGetValue(id,out var e)?e:null;}
        public static string ModelId(string blueprint)
        {switch(blueprint){case "Magpie":return "spread-biome-magpie";case "PetDog":return "spread-biome-pet-dog";case "Viper":return "spread-biome-viper";default:return null;}}
        public static string ResolveOwner(Entity owner)
        {
            string id=ModelId(owner?.BlueprintName);if(id==null)return null;
            var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();var brain=owner.GetPart<BrainPart>();
            string glyph=owner.BlueprintName=="Magpie"?"b":owner.BlueprintName=="PetDog"?"d":"~";
            if(render==null||!render.Visible||!ReferenceEquals(render.ParentEntity,owner)||render.RenderString!=glyph
                ||physics==null||!ReferenceEquals(physics.ParentEntity,owner)||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
                ||brain==null||!ReferenceEquals(brain.ParentEntity,owner)||!owner.HasTag("Creature")||owner.HasTag("Item")
                ||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()
                ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants))return null;
            return id;
        }
    }
}
