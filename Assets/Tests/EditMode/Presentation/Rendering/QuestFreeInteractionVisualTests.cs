using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class QuestFreeInteractionVisualTests
 {
  const BindingFlags Static=BindingFlags.Public|BindingFlags.Static,Private=BindingFlags.NonPublic|BindingFlags.Instance;
  static Entity observedActor,observedTarget;static Zone observedZone;static int calls;
  static void Capture(Entity actor,Entity target,Zone zone){calls++;observedActor=actor;observedTarget=target;observedZone=zone;}
  static PropertyInfo Hook(){var p=typeof(EntityVisualHooks).GetProperty("InteractionCallback",Static);Assert.NotNull(p,"resolved interaction visual hook is missing");return p;}
  static void Emit(Entity actor,Entity target,Zone zone){var m=typeof(EntityVisualHooks).GetMethod("EmitInteraction",Static);Assert.NotNull(m,"resolved interaction emitter is missing");m.Invoke(null,new object[]{actor,target,zone});}
  sealed class Hooks:IDisposable
  {
   readonly Dictionary<PropertyInfo,object> saved=typeof(EntityVisualHooks).GetProperties(Static).Where(p=>p.CanRead&&p.CanWrite).ToDictionary(p=>p,p=>p.GetValue(null));
   public Hooks(){calls=0;observedActor=null;observedTarget=null;observedZone=null;}
   public void Dispose(){foreach(var p in saved)p.Key.SetValue(null,p.Value);calls=0;observedActor=null;observedTarget=null;observedZone=null;}
  }
  static Entity Owner(Zone zone,int x,int y){var e=new Entity{ID=Guid.NewGuid().ToString("N")};e.AddPart(new PhysicsPart());e.AddPart(new RenderPart());zone.AddEntity(e,x,y);return e;}
  [Test]
  public void ResolvedInteractionUsesExactCurrentPairFacingWithoutManufacturedAttackOrDamage()
  {
   using(var scope=new Hooks())
   {var z=new Zone("visual-probe");var actor=Owner(z,3,3);var target=Owner(z,4,3);var p=Hook();p.SetValue(null,Delegate.CreateDelegate(p.PropertyType,typeof(QuestFreeInteractionVisualTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static)));int attacks=0,damage=0;EntityVisualHooks.AttackCallback=(a,b,c)=>attacks++;EntityVisualHooks.DamageCallback=(a,b,c,d,e)=>damage++;int version=z.EntityVersion;
    Emit(actor,target,z);Assert.AreEqual(1,calls);Assert.AreSame(actor,observedActor);Assert.AreSame(target,observedTarget);Assert.AreSame(z,observedZone);Assert.AreEqual(EntityVisualFacing.East,actor.GetPart<RenderPart>().VisualFacing);Assert.AreEqual(0,attacks);Assert.AreEqual(0,damage);Assert.AreEqual(version,z.EntityVersion);}
  }
  [TestCase("actor-removed")][TestCase("target-removed")][TestCase("foreign-zone")]
  public void StaleOrForeignPhysicalPairDoesNotPublishInteraction(string change)
  {
   using(var scope=new Hooks())
   {var z=new Zone("visual-probe");var actor=Owner(z,3,3);var target=Owner(z,4,3);var p=Hook();p.SetValue(null,Delegate.CreateDelegate(p.PropertyType,typeof(QuestFreeInteractionVisualTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static)));if(change=="actor-removed")z.RemoveEntity(actor);if(change=="target-removed")z.RemoveEntity(target);Emit(actor,target,change=="foreign-zone"?new Zone("same-shape"):z);Assert.AreEqual(0,calls);}
  }
  [Test]
  public void ResetClearsNewHookAndAbsentPresentationIsSafe()
  {using(var scope=new Hooks()){var p=Hook();p.SetValue(null,Delegate.CreateDelegate(p.PropertyType,typeof(QuestFreeInteractionVisualTests).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static)));EntityVisualHooks.Reset();Assert.IsNull(p.GetValue(null));var z=new Zone("visual-probe");Emit(Owner(z,3,3),Owner(z,4,3),z);Assert.AreEqual(0,calls);}}
  [Test]
  public void PresenterUsesExistingInteractPoseOnlyForVisibleCurrentPair()
  {
   using(var scope=new Hooks())using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {
    var target=f.Add("WaterPuddle");f.Refresh();var presenter=(SpawnRing3DPresenter)f.Presenter;var field=typeof(SpawnRing3DPresenter).GetField("views",Private);var views=(System.Collections.IDictionary)field.GetValue(presenter);var view=views[f.Player];Assert.NotNull(view);var until=view.GetType().GetField("ActionUntil",BindingFlags.Instance|BindingFlags.Public);until.SetValue(view,0f);
    Emit(f.Player,target,f.Zone);var state=view.GetType().GetField("ActionState",BindingFlags.Instance|BindingFlags.Public);Assert.NotNull(state,"Pending gesture must expose the exact state passed to the existing animator dispatch.");Assert.AreEqual("Interact",state.GetValue(view));Assert.Greater((float)until.GetValue(view),UnityEngine.Time.unscaledTime+.4f,"Feed pose survives long enough to reach the head-down midpoint.");
    until.SetValue(view,0f);state.SetValue(view,"Idle");f.Zone.GetEntityCell(f.Player).IsVisible=false;f.Refresh();Emit(f.Player,target,f.Zone);Assert.AreEqual(0f,until.GetValue(view));Assert.AreEqual("Idle",state.GetValue(view));f.Zone.GetEntityCell(f.Player).IsVisible=true;f.Refresh();f.Zone.RemoveEntity(target);Emit(f.Player,target,f.Zone);Assert.AreEqual(0f,until.GetValue(view));Assert.AreEqual("Idle",state.GetValue(view));
   }
  }
 }
}
