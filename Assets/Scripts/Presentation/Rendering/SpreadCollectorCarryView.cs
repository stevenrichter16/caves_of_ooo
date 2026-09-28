using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
 /// <summary>One exact carried owner under an owned bird bill. This observes
 /// inventory; it never equips, transfers or creates a native item.</summary>
 internal sealed class SpreadCollectorCarryView : IDisposable
 {
  readonly Entity actor;readonly GameObject body;readonly Transform bill,head;readonly Quaternion bindRotation;
  readonly SpreadPortable3DLibrary library;readonly Action<GameObject> prepare;
  SpreadCollectorPart previousRole;SpreadCollectorPhase previousPhase;Entity previousTarget,previousCarried,previousHome;
  Vector3 committedPosition,committedScale;Quaternion committedRotation;
  bool observed,previousVisible;Entity item;SpreadPortable3DLibrary.Entry entry;GameObject root;MeshRenderer renderer;MeshFilter filter;
  internal SpreadCollectorCarryView(Entity actor,GameObject body,SpreadPortable3DLibrary library,Action<GameObject> prepare)
  {
   this.actor=actor;this.body=body;this.library=library;this.prepare=prepare;
   foreach(var t in body.GetComponentsInChildren<Transform>(true))if(t.name==SpreadCollectorArtLibrary.BillSocket){if(bill!=null)throw new InvalidOperationException("Ambiguous collector bill socket.");bill=t;}
   var skin=body.GetComponentInChildren<SkinnedMeshRenderer>(true);
   if(skin!=null)foreach(var bone in skin.bones)if(bone!=null&&bone.name=="Head")head=bone;
   if(bill==null||head==null||!head.IsChildOf(body.transform)||!bill.IsChildOf(head))throw new InvalidOperationException("Owned collector bill/head socket missing.");
   bindRotation=Quaternion.Inverse(bill.rotation)*body.transform.rotation;
  }
  internal void Sync(Zone zone,bool shown,out string gesture)
  {
   gesture=null;var role=actor.GetPart<SpreadCollectorPart>();bool current=CurrentActor(zone,role);bool visible=current&&shown;
   Entity actual=current?role.CurrentCarriedItem:null;
   bool same=observed&&ReferenceEquals(previousRole,role)&&previousVisible&&visible;
   if(same&&previousPhase==SpreadCollectorPhase.Seeking&&role.Phase==SpreadCollectorPhase.Carrying
       &&actual!=null&&ReferenceEquals(previousTarget,actual))gesture="Pickup";
   if(same&&previousPhase==SpreadCollectorPhase.Carrying&&role.Phase==SpreadCollectorPhase.Deposited
       &&previousCarried!=null&&ReferenceEquals(previousTarget,role.Target)&&ReferenceEquals(previousCarried,role.Target)
       &&ReferenceEquals(previousHome,role.Home)&&DepositedIntoCurrentHome(zone,role.Home,previousCarried))gesture="Deposit";
   if(!visible||actual==null||!Source(actual,out var expected,out var grip,out var rotation))Clear();
   else if(root==null||!ReferenceEquals(item,actual)||entry!=expected||!GripMatches())
   {
    Clear();item=actual;entry=expected;
    root=new GameObject("CollectorCarry-"+actual.ID){hideFlags=HideFlags.DontSave};root.transform.SetParent(bill,false);
    root.transform.localScale=Vector3.one*.68f;root.transform.localRotation=bindRotation*rotation;
    root.transform.localPosition=-(root.transform.localRotation*(grip*.68f));
    committedPosition=root.transform.localPosition;committedRotation=root.transform.localRotation;committedScale=root.transform.localScale;
    filter=root.AddComponent<MeshFilter>();filter.sharedMesh=entry.Mesh;renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=library.Material;
    try{prepare(root);}catch{Clear();throw;}
   }
   observed=true;previousRole=role;previousPhase=role?.Phase??SpreadCollectorPhase.Stopped;previousTarget=role?.Target;previousHome=role?.Home;previousCarried=actual;previousVisible=visible;
  }
  // The saved phase transition is the role command's committed result. A full
  // native stack merge consumes the incoming owner instead of retaining its ID
  // in Contents; require its exact exhausted aftermath and a real compatible
  // resident in the same observed/current home, never mere cargo disappearance.
  bool DepositedIntoCurrentHome(Zone zone,Entity home,Entity incoming)
  {
   var cell=home==null?null:zone.GetEntityCell(home);var container=home?.GetPart<ContainerPart>();var physics=incoming?.GetPart<PhysicsPart>();
   if(home==null||home.SpatialZone!=zone||cell==null||!cell.Objects.Contains(home)||container==null||container.ParentEntity!=home
      ||incoming==null||physics==null||physics.ParentEntity!=incoming||incoming.SpatialZone!=null||physics.Equipped!=null
      ||actor.GetPart<InventoryPart>()?.Objects.Contains(incoming)==true)return false;
   var stack=incoming.GetPart<StackerPart>();
   if(container.Contents.Contains(incoming))return physics.InInventory==home&&(stack?.StackCount??1)>0;
   if(stack==null||stack.ParentEntity!=incoming||stack.StackCount!=0||physics.InInventory!=null)return false;
   foreach(var resident in container.Contents)
   {
    var residentStack=resident?.GetPart<StackerPart>();var residentPhysics=resident?.GetPart<PhysicsPart>();
    if(residentStack!=null&&residentStack.ParentEntity==resident&&residentStack.StackCount>0&&residentPhysics!=null
       &&residentPhysics.ParentEntity==resident&&residentPhysics.InInventory==home&&residentPhysics.Equipped==null
       &&resident.SpatialZone==null&&residentStack.CanStackWith(incoming))return true;
   }
   return false;
  }
  bool CurrentActor(Zone zone,SpreadCollectorPart role)
  {
   if(body==null||bill==null||head==null||!head.IsChildOf(body.transform)||!bill.IsChildOf(head)
      ||zone==null||actor==null||actor.BlueprintName!="Tatterjay"||actor.SpatialZone!=zone||CombatSystem.IsDeathHandled(actor)
      ||!actor.HasTag("Creature")||actor.HasTag("Item")||actor.GetStatValue("Hitpoints",0)<=0||actor.HasPart<SpatialFootprintPart>()||actor.HasPart<MultiCellPilotPropPart>())return false;
   var cell=zone.GetEntityCell(actor);var r=actor.GetPart<RenderPart>();var p=actor.GetPart<PhysicsPart>();var brain=actor.GetPart<BrainPart>();var anatomy=actor.GetPart<Body>();
   return cell!=null&&cell.Objects.Contains(actor)&&role!=null&&role.ParentEntity==actor
    &&r!=null&&r.ParentEntity==actor&&r.Visible&&r.RenderString=="j"&&r.ColorString=="&c"&&string.IsNullOrEmpty(r.VisualID)&&string.IsNullOrEmpty(r.VisualVariant)&&string.IsNullOrEmpty(r.GlyphVariants)
    &&p!=null&&p.ParentEntity==actor&&!p.Takeable&&p.InInventory==null&&p.Equipped==null
    &&brain!=null&&brain.ParentEntity==actor&&anatomy!=null&&anatomy.ParentEntity==actor;
  }
  bool Source(Entity actual,out SpreadPortable3DLibrary.Entry expected,out Vector3 grip,out Quaternion rotation)
  {
   expected=null;grip=default;rotation=Quaternion.identity;
   if(library==null||actual==null||!SpreadPortable3DLibrary.TryRecipe(actual,out var id))return false;
   // These are the three actual finite allocation forms. Unknown changed art is
   // honestly unrepresented rather than a generic scrap or wrong tool alias.
   switch(actual.BlueprintName)
   {case "Hatchet":grip=new Vector3(0,.059f,-.10f);rotation=Quaternion.Euler(0,90,0);break;
    case "Cudgel":grip=new Vector3(0,.06f,-.16f);rotation=Quaternion.Euler(0,90,0);break;
    case "LeatherBoots":grip=new Vector3(-.1f,.27f,0);break;default:return false;}
   expected=library.Find(id);return expected?.Mesh!=null;
  }
  // The owned grip is part of the submitted contract. An arbitrary descendant
  // of the bill or altered transform cannot pass exact mesh/material proof.
  bool GripMatches()=>root!=null&&root.transform.parent==bill&&root.transform.localPosition.Equals(committedPosition)
      &&root.transform.localRotation.Equals(committedRotation)&&root.transform.localScale.Equals(committedScale);
  internal bool HasCurrentCarry(Zone zone,bool shown)=>TryGet(zone,shown,item,out _);
  internal bool TryGet(Zone zone,bool shown,Entity requested,out GameObject value)
  {
   value=null;var role=actor.GetPart<SpreadCollectorPart>();
   if(!shown||!CurrentActor(zone,role)||root==null||!root.activeInHierarchy||body==null||!body.activeInHierarchy
      ||requested==null||!ReferenceEquals(requested,item)||!ReferenceEquals(role.CurrentCarriedItem,item)
      ||!Source(item,out var expected,out _,out _)||entry!=expected||!GripMatches()
      ||renderer==null||!renderer.enabled||renderer.forceRenderingOff||!renderer.gameObject.activeInHierarchy
      ||filter==null||filter.sharedMesh!=entry.Mesh||entry.Mesh.vertexCount==0||entry.Mesh.subMeshCount!=1||entry.Mesh.GetIndexCount(0)==0)return false;
   value=root;return true;
  }
  internal bool TryStyle(Zone zone,bool shown,Entity requested,NativeZone3DRenderSurface surface,MaterialPropertyBlock scratch,List<Material> materials,out SpreadBiomeStyleEvidence proof)
  {
   proof=new SpreadBiomeStyleEvidence(null,"outside-current-collector-carry",false);
   if(!TryGet(zone,shown,requested,out _))return false;
   if(!SpreadBiomeStyleCatalog.PaletteMatches(renderer,surface.MaterialFor(library.Material),library.Material,scratch,materials,out var material))
   {proof=new SpreadBiomeStyleEvidence(entry.Id,"submitted-palette-mismatch",false,entry.Mesh,library.Material,filter.sharedMesh);return false;}
   proof=new SpreadBiomeStyleEvidence(entry.Id,null,false,entry.Mesh,library.Material,filter.sharedMesh,material);return true;
  }
  void Clear()
  {if(root!=null){root.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(root);else UnityEngine.Object.DestroyImmediate(root);}root=null;renderer=null;filter=null;item=null;entry=null;}
  public void Dispose(){Clear();observed=false;previousVisible=false;previousRole=null;previousTarget=null;previousHome=null;previousCarried=null;}
 }
}
