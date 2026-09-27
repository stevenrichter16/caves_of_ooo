using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Original authored cuboid bodies, borrowed persistently by owned
    /// receiving-biome views. This initial library covers four creature species in five exact current-state forms.</summary>
    public sealed class SpreadCreature3DLibrary:ScriptableObject
    {
        public const string ResourcePath="SpreadCreature3D/Library";
        [Serializable] public sealed class Entry
        {public string Id;public GameObject Prefab;public Mesh Mesh;public SpawnRing3DCatalog.Model Spec;}
        public Entry[] Entries;
        private Dictionary<string,Entry> index;
        private static readonly string[] Ids={"spread-creature-giant-spider","spread-creature-jungle-ape","spread-creature-mimic-closed","spread-creature-mimic-awake","spread-creature-glowmaw"};
        public bool ContainsMesh(Mesh mesh){if(mesh==null)return false;if(index==null)Validate();foreach(var e in Entries)if(e.Mesh==mesh)return true;return false;}
        private static readonly string[] ClipNames={"Idle","Walk","Interact","Attack","Hit"};
        public static SpreadCreature3DLibrary Load()=>Resources.Load<SpreadCreature3DLibrary>(ResourcePath);
        private void OnValidate()=>index=null;
        public void Validate()
        {
            if(Entries==null||Entries.Length!=5)throw new InvalidOperationException("Five original Spread creature forms required.");
            var next=new Dictionary<string,Entry>(StringComparer.Ordinal);
            var palette=ReferenceGladeVoxelLibrary.Load()?.Material;
            if(palette==null)throw new InvalidOperationException("Approved native palette is unavailable.");
            foreach(var e in Entries)
            {
                if(e==null||e.Spec==null||Array.IndexOf(Ids,e.Id)<0||e.Spec.sourceBlueprint!=Blueprint(e.Id)||e.Spec.rigFamily!=RigFamily(e.Id)||e.Id!=e.Spec.id||next.ContainsKey(e.Id)
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
                var expectedBones=new HashSet<string>(BoneNames(e.Id),StringComparer.Ordinal);
                foreach (var bone in bones)
                {
                    if (bone == null || !bone.IsChildOf(e.Prefab.transform) || !expectedBones.Remove(bone.name))
                        throw new InvalidOperationException("Missing, foreign or repeated original creature bone.");
                }
                if(expectedBones.Count!=0)throw new InvalidOperationException("Incomplete original creature anatomy.");
                foreach(var w in weights)if(w.weight0!=1||w.weight1!=0||w.weight2!=0||w.weight3!=0||w.boneIndex0<0||w.boneIndex0>=bones.Length)
                    throw new InvalidOperationException("Authored cuboids require exact rigid bone ownership.");
                next.Add(e.Id,e);
            }
            foreach(var id in Ids)if(!next.ContainsKey(id))throw new InvalidOperationException("Missing original creature form: "+id);
            index=next;
        }
        public Entry Find(string id)
        {if(id==null||!id.StartsWith("spread-creature-",StringComparison.Ordinal))return null;if(index==null)Validate();return index.TryGetValue(id,out var e)?e:null;}
        public static string Blueprint(string id)
        {switch(id){case "spread-creature-giant-spider":return "GiantSpider";case "spread-creature-jungle-ape":return "JungleApe";case "spread-creature-mimic-closed":case "spread-creature-mimic-awake":return "MimicChest";case "spread-creature-glowmaw":return "Glowmaw";default:return null;}}
        public static string RigFamily(string id)
        {switch(Blueprint(id)){case "GiantSpider":return "arachnid";case "JungleApe":return "ape";case "MimicChest":return "mimic";case "Glowmaw":return "grasping";default:return null;}}
        public static string[] BoneNames(string id)
        {
            switch(id){
            case "spread-creature-giant-spider":return new[]{"Root","Body","Head","Leg.L0","Leg.L1","Leg.L2","Leg.L3","Leg.R0","Leg.R1","Leg.R2","Leg.R3"};
            case "spread-creature-jungle-ape":return new[]{"Root","Body","Head","Arm.L","Hand.L","Leg.L","Arm.R","Hand.R","Leg.R"};
            case "spread-creature-mimic-closed":return new[]{"Root","Body","Head"};
            case "spread-creature-mimic-awake":return new[]{"Root","Body","Head","Arm.L","Arm.R"};
            case "spread-creature-glowmaw":return new[]{"Root","Body","Head","Arm.L0","Arm.L1","Arm.R0","Arm.R1"};
            default:return null;}
        }
        internal static SpawnRing3DRecipe Refine(Zone zone,Entity owner,SpawnRing3DRecipe native)
        {
            if(!SpreadPresentationScope.IsActive(zone)||owner==null||!ReferenceEquals(native.Owner,owner)
                ||native.Failure!=null&&native.Failure!="unmodeled-native-blueprint")return native;
            string bp=owner.BlueprintName;
            if(bp!="GiantSpider"&&bp!="JungleApe"&&bp!="MimicChest"&&bp!="Glowmaw")return native;
            var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();var brain=owner.GetPart<BrainPart>();var cell=zone.GetEntityCell(owner);
            string glyph=bp=="GiantSpider"?"S":bp=="JungleApe"?"A":bp=="MimicChest"?"=":"O";
            if(cell==null||!cell.Objects.Contains(owner)||render==null||!render.Visible||!ReferenceEquals(render.ParentEntity,owner)||render.RenderString!=glyph
                ||physics==null||!ReferenceEquals(physics.ParentEntity,owner)||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
                ||brain==null||!ReferenceEquals(brain.ParentEntity,owner)||!owner.HasTag("Creature")||owner.HasTag("Item")
                ||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()
                ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants))return native;
            string id;
            if(bp=="MimicChest")
            {
                var ambush=owner.GetPart<AIAmbushPart>();var container=owner.GetPart<ContainerPart>();
                if(ambush==null||!ReferenceEquals(ambush.ParentEntity,owner)||container==null||!ReferenceEquals(container.ParentEntity,owner))return native;
                bool dormant=false;for(int i=0;i<brain.GoalCount;i++)if(brain.PeekGoalAt(i)is DormantGoal goal&&!goal.Finished()){dormant=true;break;}
                id=dormant?"spread-creature-mimic-closed":"spread-creature-mimic-awake";
            }
            else if(bp=="Glowmaw")
            {
                var ambush=owner.GetPart<GlowmawAmbushPart>();if(ambush==null||!ReferenceEquals(ambush.ParentEntity,owner)||!ambush.HasDropped)return native;
                id="spread-creature-glowmaw";
            }
            else id=bp=="GiantSpider"?"spread-creature-giant-spider":"spread-creature-jungle-ape";
            return new SpawnRing3DRecipe(owner,id,native.ComponentId,Village3DProjection.CellCentre(cell.X,cell.Y),true,false,quarterTurns:native.QuarterTurns);
        }
    }
}
