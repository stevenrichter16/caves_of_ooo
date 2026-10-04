using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
 /// <summary>Original intake scenery and portable tools. Pure current-owner
 /// selection; the saved intake/door/held-item authorities stay native.</summary>
 public sealed class CurationYard3DLibrary:ScriptableObject
 {
  public const string Folder="Assets/Resources/CurationYard3D",ResourcePath="CurationYard3D/Library";
  public const string ReviewedSourceSha256="d70059d55a1a491e42bf7e2eb51f65e5bbc495c22659fe611f5f75479f2126ea";
  [Serializable]public sealed class Entry{public string Id;public Mesh Mesh;public GameObject Prefab;public SpawnRing3DCatalog.Model Spec;}
  public string SourceSha256;public Material Material;public Entry[] Entries;
  Dictionary<string,Entry> index;
  public static CurationYard3DLibrary Load()=>Resources.Load<CurationYard3DLibrary>(ResourcePath);
  void OnValidate(){index=null;}
  public Entry Find(string id){if(!CurationYardSource.IsModelId(id))return null;if(index==null)Validate();return index.TryGetValue(id,out var entry)?entry:null;}
  public void Validate()
  {
   index=null;if(SourceSha256!=ReviewedSourceSha256||Material==null||Material!=ReferenceGladeVoxelLibrary.Load()?.Material||Entries==null||Entries.Length!=CurationYardSource.ModelIds.Length)throw new InvalidOperationException("Reviewed complete Curation pack required.");
   var next=new Dictionary<string,Entry>(StringComparer.Ordinal);var meshes=new HashSet<Mesh>();
   foreach(var e in Entries)
   {
    if(e==null||!CurationYardSource.IsModelId(e.Id)||next.ContainsKey(e.Id)||e.Mesh==null||!e.Mesh.isReadable||e.Mesh.vertexCount<72||e.Mesh.vertexCount>2400||e.Mesh.subMeshCount!=1||e.Mesh.GetIndexCount(0)==0||!meshes.Add(e.Mesh)||e.Prefab==null||e.Spec==null||e.Spec.id!=e.Id||e.Spec.path!=Folder+"/"+e.Id+".prefab"||e.Spec.kind!="entity"||e.Spec.rigFamily!="none"||e.Spec.materialFamily!="reference-glade-palette")throw new InvalidOperationException("Invalid Curation model.");
    var filters=e.Prefab.GetComponentsInChildren<MeshFilter>(true);var renderers=e.Prefab.GetComponentsInChildren<Renderer>(true);var t=e.Prefab.transform;
    if(filters.Length!=1||filters[0].sharedMesh!=e.Mesh||renderers.Length!=1||renderers[0].sharedMaterials.Length!=1||renderers[0].sharedMaterial!=Material||t.localPosition!=Vector3.zero||t.localRotation!=Quaternion.identity||t.localScale!=Vector3.one||e.Prefab.GetComponentsInChildren<Collider>(true).Length!=0||e.Prefab.GetComponentsInChildren<Rigidbody>(true).Length!=0||e.Prefab.GetComponentsInChildren<Animator>(true).Length!=0||e.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)throw new InvalidOperationException("Curation art must be inert and source-owned.");
    next.Add(e.Id,e);
   }
   index=next;
  }
  public static bool IsPortable(string bp)=>bp=="CurationSaltRake"||bp=="CurationCounterfoil"||bp=="CurationInspectionKey"||bp=="CurationTransferDocket"||bp=="CurationDiscrepancyReport";
  public static bool Handles(string bp)=>IsPortable(bp)||SpreadBiomeHumanoidSource.IsCuration(bp)||bp=="SaltCuredBody"||bp=="CurationIntakeIndex"||bp=="CurationReceivingBay"||bp=="CurationToolCabinet"||bp=="CurationSaltBench"||bp=="CurationQuarantineGate"||bp=="CurationQuarantineRail"||IsAnnexGate(bp)||bp=="CurationRecoveryCabinet"||bp=="CurationConservationCase"||bp=="CurationMaintenanceRack"||bp=="CurationInspectionSlab"||bp=="CurationAnnexPlacard";
  static bool IsAnnexGate(string bp)=>bp=="CurationTransferGate"||bp=="CurationGalleryGate"||bp=="CurationServiceGate";
  static bool Appearance(Entity e,string glyph,string color)
  {
   var r=e?.GetPart<RenderPart>();return r!=null&&r.ParentEntity==e&&r.Visible&&r.RenderString==glyph&&r.ColorString==color&&string.IsNullOrEmpty(r.VisualID)&&string.IsNullOrEmpty(r.VisualVariant)&&string.IsNullOrEmpty(r.GlyphVariants);
  }
  public static string PortableModel(Entity e)
  {
   if(!IsPortable(e?.BlueprintName)||e.HasTag("Creature")||e.HasTag("Natural"))return null;
   var p=e.GetPart<PhysicsPart>();if(p==null||p.ParentEntity!=e||!p.Takeable||(e.GetPart<StackerPart>()?.StackCount??1)<=0)return null;
   string glyph="?",color="&W",model=null;
   switch(e.BlueprintName)
   {
    case "CurationSaltRake":glyph=")";color="&w";model="curation-yard-salt-rake";if(e.GetPart<MeleeWeaponPart>()?.ParentEntity!=e||e.GetPart<EquippablePart>()?.ParentEntity!=e)return null;break;
    case "CurationCounterfoil":model="curation-yard-counterfoil";if(e.GetPart<KeyPart>()?.ParentEntity!=e)return null;break;
    case "CurationInspectionKey":glyph=";";color="&y";model="curation-yard-inspection-key";if(e.GetPart<KeyPart>()?.ParentEntity!=e)return null;break;
    case "CurationTransferDocket":color="&w";model="curation-yard-transfer-docket";break;
    case "CurationDiscrepancyReport":model="curation-yard-discrepancy-report";break;
   }
   return Appearance(e,glyph,color)?model:null;
  }
  public static string ResolveModel(Zone zone,Entity e)
  {
   if(e==null||!Handles(e.BlueprintName)||!SpreadPresentationScope.IsActive(zone))return null;
   var c=zone.GetEntityCell(e);var p=e.GetPart<PhysicsPart>();
   if(c==null||e.SpatialZone!=zone||c.ParentZone!=zone||!c.Objects.Contains(e)||p==null||p.ParentEntity!=e||p.InInventory!=null||p.Equipped!=null||e.HasPart<SpatialFootprintPart>()||e.HasPart<MultiCellPilotPropPart>())return null;
   if(IsPortable(e.BlueprintName))return PortableModel(e);
   if(p.Takeable)return null;
   if(SpreadBiomeHumanoidSource.IsCuration(e.BlueprintName))
   {
    if(!e.HasTag("Creature")||e.HasTag("Item")||e.GetPart<BrainPart>()?.ParentEntity!=e||e.GetPart<Body>()?.ParentEntity!=e)return null;
    string glyph=e.BlueprintName=="CurationHalfSet"?"h":"@",color=e.BlueprintName=="CurationIntakeFiler"?"&W":e.BlueprintName=="CurationJuniorIndexer"?"&w":"&g";
    return Appearance(e,glyph,color)?SpreadBiomeHumanoidSource.ModelId(e.BlueprintName):null;
   }
   if(!MarrowstyeCompositionPlan.IsSupportedZone(zone.ZoneID)||e.HasTag("Creature")||e.HasTag("Item"))return null;
   switch(e.BlueprintName)
   {
    case "SaltCuredBody":var body=e.GetPart<CurationReceivingBodyPart>();return Appearance(e,"&","&W")&&body?.ParentEntity==e&&body.IsCurrent(zone)?(body.CaseNumber==1?"curation-yard-salt-cured-body-1":"curation-yard-salt-cured-body-2"):null;
    case "CurationReceivingBay":var bay=e.GetPart<CurationReceivingBayPart>();return Appearance(e,"_","&w")&&bay?.ParentEntity==e&&bay.IsCurrent(zone)?(bay.CaseNumber==1?"curation-yard-receiving-bay-1":"curation-yard-receiving-bay-2"):null;
    case "CurationIntakeIndex":return Appearance(e,"¶","&W")&&e.GetPart("CurationIntake")?.ParentEntity==e?"curation-yard-intake-index":null;
    case "CurationToolCabinet":return Appearance(e,"]","&W")&&e.GetPart<ContainerPart>()?.ParentEntity==e&&e.GetPart<LockPart>()?.ParentEntity==e?"curation-yard-tool-cabinet":null;
    case "CurationSaltBench":return Appearance(e,"=","&w")?"curation-yard-salt-bench":null;
    case "CurationQuarantineRail":return Appearance(e,"#","&w")?"curation-yard-quarantine-rail":null;
    case "CurationConservationCase":
    case "CurationRecoveryCabinet":return Appearance(e,"]","&W")&&e.GetPart<ContainerPart>()?.ParentEntity==e?"curation-yard-recovery-cabinet":null;
    case "CurationMaintenanceRack":return Appearance(e,"=","&w")&&e.GetPart<ContainerPart>()?.ParentEntity==e?"curation-yard-maintenance-rack":null;
    case "CurationInspectionSlab":return Appearance(e,"=","&w")?"curation-yard-inspection-slab":null;
    case "CurationAnnexPlacard":return Appearance(e,"?","&W")?"curation-yard-annex-placard":null;
    case "CurationTransferGate":case "CurationGalleryGate":case "CurationServiceGate":return AnnexGateModel(e);
    case "CurationQuarantineGate":var door=e.GetPart<DoorPart>();return door?.ParentEntity==e&&door.QuarterTurns>=0&&door.QuarterTurns<4&&Appearance(e,door.IsClosed?"+":"/","&W")?(door.IsClosed?"curation-yard-quarantine-gate-closed":"curation-yard-quarantine-gate-open"):null;
   }
   return null;
  }
  // Art observes the existing fault/door authorities; it never repairs, closes,
  // unlocks or changes collision. A malformed fault cannot borrow a healthy gate.
  static string AnnexGateModel(Entity e)
  {
   var door=e.GetPart<DoorPart>();var physics=e.GetPart<PhysicsPart>();
   if(door?.ParentEntity!=e||door.QuarterTurns<0||door.QuarterTurns>3||physics.Solid||e.HasTag("Solid")
    ||!Appearance(e,door.IsClosed?"+":"/","&W"))return null;
   if(e.BlueprintName=="CurationServiceGate")
   {
    var repair=e.GetPart<RepairablePart>();var composition=e.GetPart<CompositionPart>();int faults=0;
    foreach(var part in e.Parts)if(part is RepairablePart)faults++;
    if(faults!=1||repair?.ParentEntity!=e||repair.RecipeId!="timber-gate-frame"||composition?.ParentEntity!=e||!composition.Contains("Wood"))return null;
    if(!repair.Repaired)return door.IsClosed?null:"curation-yard-service-gate-broken";
   }
   return door.IsClosed?"curation-yard-quarantine-gate-closed":"curation-yard-quarantine-gate-open";
  }
  internal static SpawnRing3DRecipe Refine(Zone zone,Entity e,SpawnRing3DRecipe native)
  {
   if(e==null||native.Owner!=e||native.Failure!=null&&native.Failure!="unmodeled-native-blueprint")return native;
   string model=ResolveModel(zone,e);return model==null?native:Recipe(zone,e,model);
  }
  internal static SpawnRing3DRecipe Recipe(Zone zone,Entity e,string model)
  {
   var c=zone.GetEntityCell(e);bool actor=SpreadBiomeHumanoidSource.IsCuration(e.BlueprintName),portable=IsPortable(e.BlueprintName);var door=e.GetPart<DoorPart>();
   return new SpawnRing3DRecipe(e,model,null,Village3DProjection.CellCentre(c.X,c.Y),actor||portable,!actor&&!portable&&door==null,quarterTurns:door?.QuarterTurns??0);
  }
 }
}
