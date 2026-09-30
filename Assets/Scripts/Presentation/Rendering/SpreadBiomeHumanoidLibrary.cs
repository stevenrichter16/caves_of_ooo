using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
 /// <summary>Persistent approved humanoid forms with real native rigs. Refinement
 /// follows existing quest/state recipes and never manufactures native owners.</summary>
 public sealed class SpreadBiomeHumanoidLibrary:ScriptableObject
 {
  public const string ResourcePath="SpreadBiome3D/HumanoidLibrary";
  [Serializable]public sealed class Entry{public string Id,Blueprint;public GameObject Prefab;public Mesh Mesh;public SpawnRing3DCatalog.Model Spec;}
  public Entry[] Entries;public Material Material;
  private Dictionary<string,Entry> index;private HashSet<Mesh> meshes;
  public static SpreadBiomeHumanoidLibrary Load()=>Resources.Load<SpreadBiomeHumanoidLibrary>(ResourcePath);
  public static string ModelId(string blueprint)=>SpreadBiomeHumanoidSource.ModelId(blueprint);
  private void OnValidate(){index=null;meshes=null;}
  public void Validate()
  {
   if(Entries==null||(Entries.Length!=52&&Entries.Length!=54&&Entries.Length!=56&&Entries.Length!=59)||Material==null||Material!=ReferenceGladeVoxelLibrary.Load()?.Material)throw new InvalidOperationException("Exact approved humanoid roster/palette required.");
   var next=new Dictionary<string,Entry>(StringComparer.Ordinal);var owned=new HashSet<Mesh>();
   foreach(var e in Entries)
   {
    if(e==null||e.Id==null||Entries.Length==52&&SpreadBiomeHumanoidSource.IsCaster(e.Blueprint)||Entries.Length<56&&SpreadBiomeHumanoidSource.IsFieldResident(e.Blueprint)||Entries.Length<59&&SpreadBiomeHumanoidSource.IsCuration(e.Blueprint)||ModelId(e.Blueprint)!=e.Id||next.ContainsKey(e.Id)||e.Mesh==null||!e.Mesh.isReadable||e.Mesh.vertexCount<384||e.Mesh.subMeshCount!=1||e.Prefab==null||!ValidScale(e.Prefab.transform.localScale)||e.Spec==null||e.Spec.id!=e.Id||e.Spec.sourceBlueprint!=e.Blueprint||e.Spec.rigFamily!="humanoid"||!e.Spec.rigged||e.Spec.kind!="actor"||!owned.Add(e.Mesh))throw new InvalidOperationException("Invalid scoped humanoid body.");
    var skins=e.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);var animators=e.Prefab.GetComponentsInChildren<Animator>(true);
    if(skins.Length!=1||skins[0].sharedMesh!=e.Mesh||skins[0].sharedMaterials.Length!=1||skins[0].sharedMaterial!=Material||animators.Length!=1||animators[0].avatar==null||!animators[0].avatar.isValid||animators[0].applyRootMotion||animators[0].runtimeAnimatorController==null||e.Prefab.GetComponentsInChildren<Collider>(true).Length!=0||e.Prefab.GetComponentsInChildren<Rigidbody>(true).Length!=0)throw new InvalidOperationException("Invalid scoped humanoid native rig.");
    var clips=animators[0].runtimeAnimatorController.animationClips;if(clips.Length!=5)throw new InvalidOperationException("Five native humanoid clips required.");
    foreach(string name in new[]{"Idle","Walk","Interact","Attack","Hit"}){int count=0;foreach(var clip in clips)if(clip!=null&&clip.name==name&&clip.length>0)count++;if(count!=1)throw new InvalidOperationException("Missing native humanoid motion: "+name);}
    var bones=skins[0].bones;var weights=e.Mesh.boneWeights;if(bones.Length!=9||e.Mesh.bindposeCount!=bones.Length||weights.Length!=e.Mesh.vertexCount)throw new InvalidOperationException("Incomplete approved humanoid bone buffers.");
    foreach(var bone in bones)if(bone==null)throw new InvalidOperationException("Missing native humanoid bone.");
    foreach(var w in weights)if(w.weight0!=1||w.weight1!=0||w.weight2!=0||w.weight3!=0||w.boneIndex0<0||w.boneIndex0>=bones.Length)throw new InvalidOperationException("Invalid rigid humanoid vertex ownership.");
    next.Add(e.Id,e);
   }
   index=next;meshes=owned;
  }
  private static bool ValidScale(Vector3 scale)=>!float.IsNaN(scale.x)&&!float.IsInfinity(scale.x)&&scale.x>=.65f&&scale.x<=1&&scale.x==scale.y&&scale.y==scale.z;
  public Entry Find(string id){if(id==null||!id.StartsWith("spread-person-",StringComparison.Ordinal))return null;if(index==null)Validate();return index.TryGetValue(id,out var e)?e:null;}
  /// <summary>Exact persistent membership for visible-bounds and coverage checks;
  /// unknown or merely similarly named meshes never acquire approval.</summary>
  public bool ContainsMesh(Mesh mesh){if(mesh==null)return false;if(meshes==null)Validate();return meshes.Contains(mesh);}
  // These are actual Villager quest owners authored by VillagePopulationBuilder.
  // The full live appearance/quest pairing permits their noncanonical glyphs;
  // a display name or isolated quest marker cannot authorize a substitute body.
  private static bool IsAuthoredSillQuestAppearance(Entity owner,RenderPart render)
  {
   if(owner.BlueprintName!="Villager")return false;
   var conversation=owner.GetPart<ConversationPart>();var beacon=owner.GetPart<QuestBeaconPart>();
   if(conversation==null||beacon==null||!ReferenceEquals(conversation.ParentEntity,owner)||!ReferenceEquals(beacon.ParentEntity,owner))return false;
   return render.RenderString=="r"&&render.ColorString=="&r"&&conversation.ConversationID=="RootBeerGuy_Quest"&&beacon.Quest=="RootBeerGuyCase"
    ||render.RenderString=="b"&&render.ColorString=="&c"&&conversation.ConversationID=="BMO_Quest"&&beacon.Quest=="BmoCartridge";
  }

  // Posy's message quest keeps its actual home metadata while an owner travels.
  // The recipient has no beacon; the giver requires its exact native beacon.
  private static bool IsAuthoredPosyQuestAppearance(Entity owner,RenderPart render)
  {
   if(owner.BlueprintName!="Villager"||owner.GetProperty("SettlementId")!="Overworld.5.9.0")return false;
   var conversation=owner.GetPart<ConversationPart>();
   if(conversation==null||!ReferenceEquals(conversation.ParentEntity,owner))return false;
   var beacon=owner.GetPart<QuestBeaconPart>();
   return render.RenderString=="b"&&render.ColorString=="&w"&&conversation.ConversationID=="Baker_Quest"
     &&beacon!=null&&ReferenceEquals(beacon.ParentEntity,owner)&&beacon.Quest=="MessageForHermit"
    ||render.RenderString=="h"&&render.ColorString=="&K"&&conversation.ConversationID=="Hermit_Quest"&&beacon==null;
  }

  // Exact new spellcaster identities use the unchanged real humanoid skeleton;
  // no item, equipment or active spell state is manufactured by presentation.
  internal static string ResolveCasterOwner(Zone zone,Entity owner)=>ResolveScopedOwner(zone,owner,false);
  internal static string ResolveFieldResidentOwner(Zone zone,Entity owner)=>ResolveScopedOwner(zone,owner,true);
  private static string ResolveScopedOwner(Zone zone,Entity owner,bool resident)
  {
   if(!SpreadPresentationScope.IsActive(zone)||(resident?!SpreadBiomeHumanoidSource.IsFieldResident(owner?.BlueprintName):!SpreadBiomeHumanoidSource.IsCaster(owner?.BlueprintName)))return null;
   var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();var brain=owner.GetPart<BrainPart>();var body=owner.GetPart<Body>();var cell=zone.GetEntityCell(owner);
   if(cell==null||!ReferenceEquals(owner.SpatialZone,zone)||!ReferenceEquals(cell.ParentZone,zone)||!cell.Objects.Contains(owner)
      ||render==null||render.ParentEntity!=owner||!render.Visible||render.RenderString!=(resident?"@":"g")||render.ColorString!=(resident?(owner.BlueprintName=="SpreadSeedKeeper"?"&y":"&w"):(owner.BlueprintName=="MarlbackCindercaller"?"&R":"&G"))
      ||physics==null||physics.ParentEntity!=owner||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
      ||brain==null||brain.ParentEntity!=owner||body==null||body.ParentEntity!=owner||!owner.HasTag("Creature")||owner.HasTag("Item")
      ||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants))return null;
   return ModelId(owner.BlueprintName);
  }
  internal static SpawnRing3DRecipe Refine(Zone zone,Entity owner,SpawnRing3DRecipe native)
  {
   if(!SpreadPresentationScope.IsActive(zone)||ModelId(owner?.BlueprintName)==null)return native;
   if(SpreadBiomeHumanoidSource.IsCuration(owner.BlueprintName)&&CurationYard3DLibrary.ResolveModel(zone,owner)==null)return native;
   if(SpreadBiomeHumanoidSource.IsCaster(owner.BlueprintName)&&ResolveCasterOwner(zone,owner)==null)return native;
   if(SpreadBiomeHumanoidSource.IsFieldResident(owner.BlueprintName)&&ResolveFieldResidentOwner(zone,owner)==null)return native;
   if(native.ModelId=="ring-player"||native.ModelId=="ring-sien"||native.ModelId=="ring-nam")return native;
   // Never rescue a refused regional quest, footprint, scene or disguise contract.
   if(native.Failure!=null&&native.Failure!="unmodeled-native-blueprint")return native;
   var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();var brain=owner.GetPart<BrainPart>();var cell=zone.GetEntityCell(owner);
   if(cell==null||!cell.Objects.Contains(owner)||render==null||!render.Visible||!ReferenceEquals(render.ParentEntity,owner)
     ||physics==null||!ReferenceEquals(physics.ParentEntity,owner)||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
     ||brain==null||!ReferenceEquals(brain.ParentEntity,owner)||!owner.HasTag("Creature")||owner.HasTag("Item")
     ||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants)
     ||native.Failure!=null&&render.RenderString!=SpreadBiomeHumanoidSource.CanonicalGlyph(owner.BlueprintName)&&!IsAuthoredSillQuestAppearance(owner,render)&&!IsAuthoredPosyQuestAppearance(owner,render))return native;
   return new SpawnRing3DRecipe(owner,ModelId(owner.BlueprintName),native.ComponentId,Village3DProjection.CellCentre(cell.X,cell.Y),true,false,quarterTurns:native.QuarterTurns);
  }
 }
}
