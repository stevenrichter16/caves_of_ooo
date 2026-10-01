using System;
using System.Collections.Generic;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
 /// <summary>Read-only adopted-asset proof for one actual current owner. A batch
 /// reports its exact committed source fragment and the owned submitted mesh;
 /// it does not pretend the combined mesh is the persistent source asset.</summary>
 public readonly struct SpreadBiomeStylePieceEvidence
 {
  public readonly Mesh ExpectedMesh,SubmittedMesh;public readonly Material ExpectedMaterial,SubmittedMaterial;public readonly int SourceSubmesh,SubmittedSubmesh;
  internal SpreadBiomeStylePieceEvidence(Mesh expected,Material source,Mesh submitted,Material material,int sourceSubmesh,int submittedSubmesh)
  {ExpectedMesh=expected;ExpectedMaterial=source;SubmittedMesh=submitted;SubmittedMaterial=material;SourceSubmesh=sourceSubmesh;SubmittedSubmesh=submittedSubmesh;}
 }
 public readonly struct SpreadBiomeStyleEvidence
 {
  public readonly string ModelId,Failure;
  public readonly Mesh ExpectedMesh,SubmittedMesh;
  public readonly Material ExpectedMaterial,SubmittedMaterial;
  public readonly bool Batched;
  private readonly SpreadBiomeStylePieceEvidence[] pieces;
  public int PieceCount=>pieces?.Length??(ExpectedMesh!=null?1:0);
  public SpreadBiomeStylePieceEvidence GetPiece(int index){if(index<0||index>=PieceCount)throw new ArgumentOutOfRangeException(nameof(index));return pieces!=null?pieces[index]:new SpreadBiomeStylePieceEvidence(ExpectedMesh,ExpectedMaterial,SubmittedMesh,SubmittedMaterial,0,0);}
  internal SpreadBiomeStyleEvidence(string model,string failure,bool batched,Mesh expected=null,Material source=null,Mesh submitted=null,Material material=null,SpreadBiomeStylePieceEvidence[] pieces=null)
  {ModelId=model;Failure=failure;Batched=batched;ExpectedMesh=expected;ExpectedMaterial=source;SubmittedMesh=submitted;SubmittedMaterial=material;this.pieces=pieces;}
 }
 /// <summary>Prepared once per owned bind from explicit adopted libraries. No
 /// arbitrary material family, asset name or copied mesh can enter this table.</summary>
 internal sealed class SpreadBiomeStyleCatalog
 {
  internal sealed class Contract
  {
   internal readonly Mesh Mesh;internal readonly Material Material;internal readonly Material[] Materials;
   internal Contract(Mesh mesh,Material material,Material[] materials){Mesh=mesh;Material=material;Materials=materials??new[]{material};}
  }
  private static readonly int BaseColor=Shader.PropertyToID("_BaseColor"),BaseMap=Shader.PropertyToID("_BaseMap");
  private readonly Dictionary<string,Contract> models=new Dictionary<string,Contract>(StringComparer.Ordinal);
  internal SpreadBiomeStyleCatalog(ReferenceGladeVoxelLibrary glade,SpreadBiomeActorLibrary animals,SpreadBiomeHumanoidLibrary people,SpreadPortable3DLibrary portable,PouredLiquid3DLibrary poured,SpreadScenery3DLibrary scenery,SpreadCreature3DLibrary creatures,SpreadEnvironment3DLibrary environment,SpreadVisitorPaintLibrary visitorPaints,SpreadNativeStyle3DLibrary nativeStyles,SpreadVisitorCreatureLibrary newVisitors)
  {
   if(glade==null||animals==null||people==null||portable==null||poured==null||scenery==null||creatures==null||environment==null||visitorPaints==null||nativeStyles==null||newVisitors==null)throw new ArgumentException("Complete current approved style libraries required.");
   foreach(var e in glade.Entries)Add(e.Id,e.Mesh,glade.Material);
   foreach(var p in glade.ActorPaints)Add(p.ModelId,p.Painted,glade.Material);
   foreach(var e in animals.Entries)Add(e.Id,e.Mesh,glade.Material);
   foreach(var e in creatures.Entries)Add(e.Id,e.Mesh,glade.Material);
   foreach(var e in newVisitors.Entries)Add(e.Id,e.Mesh,newVisitors.Material);
   foreach(var e in people.Entries)Add(e.Id,e.Mesh,people.Material);
   foreach(var e in portable.Entries)Add(e.Id,e.Mesh,portable.Material);
   foreach(var e in scenery.Entries)Add(e.Id,e.Mesh,scenery.Material);
   foreach(var e in environment.Entries)Add(e.Id,e.Mesh,environment.Material);
   foreach(var e in nativeStyles.Entries)Add(e.Id,e.Mesh,e.Material,e.Materials);
   foreach(var e in visitorPaints.Entries)Add(e.ModelId,e.Painted,visitorPaints.Material);
   foreach(var e in poured.Entries)Add(e.Spec.id,e.Prefab.GetComponent<MeshFilter>().sharedMesh,e.Material);
   var rareMarlbacks = SpreadRareMarlbackLibrary.Load();
   if (rareMarlbacks != null) { rareMarlbacks.Validate(); foreach (var e in rareMarlbacks.Entries) Add(e.Id, e.Mesh, rareMarlbacks.Material); }
   var latchcoil = SpreadLatchcoilLibrary.Load();
   if (latchcoil != null) { latchcoil.Validate(); foreach (var e in latchcoil.Entries) Add(e.Id,e.Mesh,latchcoil.Material); }
   var collectors=SpreadCollectorArtLibrary.Load();
   if(collectors!=null){collectors.Validate();foreach(var e in collectors.Entries)Add(e.Id,e.Mesh,e.Materials[0],e.Materials);}
   var cultivation=RepairCultivation3DLibrary.Load();
   if(cultivation!=null){cultivation.Validate();foreach(var e in cultivation.Entries)Add(e.Id,e.Mesh,cultivation.Material);}
   var curation=CurationYard3DLibrary.Load();
   if(curation!=null){curation.Validate();foreach(var e in curation.Entries)Add(e.Id,e.Mesh,curation.Material);}
   var fieldGates=SpreadFieldGate3DLibrary.Load();
   if(fieldGates!=null){fieldGates.Validate();foreach(var e in fieldGates.Entries)Add(e.Id,e.Mesh,e.Material);}
   var cooking=SpreadCooking3DLibrary.Load();
   if(cooking!=null){cooking.Validate();foreach(var e in cooking.Entries)Add(e.Id,e.Mesh,e.Material);}
   var coals=SpreadCookingCoalsLibrary.Load();
   if(coals!=null){coals.Validate();foreach(var e in coals.Entries)Add(e.Id,e.Mesh,e.Material);}
   var hunters=FurrowstalkerLibrary.Load();
   if(hunters!=null){hunters.Validate();foreach(var e in hunters.Entries)Add(e.Id,e.Mesh,e.Materials[0],e.Materials);}
   var exploration = QuestFreeSpreadArtLibrary.Load();
   if (exploration != null) { exploration.Validate(); foreach (var e in exploration.Entries) Add(e.Id,e.Mesh,e.Materials[0],e.Materials); }
  }
  private void Add(string model,Mesh mesh,Material material,Material[] materials=null)
  {if(model==null||mesh==null||material==null||models.ContainsKey(model))throw new InvalidOperationException("Invalid exact style model contract.");models.Add(model,new Contract(mesh,material,materials));}
  internal bool TryGet(string model,out Contract contract){contract=null;return model!=null&&models.TryGetValue(model,out contract);}
  internal static bool PaletteMatches(Renderer renderer,Material expectedOwned,Material borrowed,MaterialPropertyBlock scratch,List<Material> materials,out Material actual,int slot=0,int slotCount=1)
  {
   actual=null;if(renderer==null||expectedOwned==null||borrowed==null)return false;
   materials.Clear();renderer.GetSharedMaterials(materials);if(materials.Count!=slotCount||slot<0||slot>=materials.Count)return false;actual=materials[slot];
   if(actual!=expectedOwned||actual.shader!=borrowed.shader||actual.GetTexture("_BaseMap")!=borrowed.GetTexture("_BaseMap")||actual.GetColor("_BaseColor")!=borrowed.GetColor("_BaseColor"))return false;
   renderer.GetPropertyBlock(scratch);bool valid=OverridesMatch(scratch,borrowed);scratch.Clear();if(!valid)return false;
   renderer.GetPropertyBlock(scratch,slot);valid=OverridesMatch(scratch,borrowed);scratch.Clear();return valid;
  }
  private static bool OverridesMatch(MaterialPropertyBlock properties,Material source)
  {
   return (!properties.HasProperty(BaseColor)||properties.GetColor(BaseColor)==source.GetColor(BaseColor))
       &&(!properties.HasProperty(BaseMap)||properties.GetTexture(BaseMap)==source.GetTexture(BaseMap));
  }
 }
}
