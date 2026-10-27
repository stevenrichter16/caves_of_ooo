// Additive read-only hooks. Arm capture first; finish with coo-east-finish-events.cs.
var d=System.AppDomain.CurrentDomain;
if(d.GetData("G2Action.AttackObserver")!=null || d.GetData("G2Action.DamageObserver")!=null || d.GetData("G2Action.CastObserver")!=null)throw new System.InvalidOperationException("Already observing.");
var world=UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.ZoneRenderer>();
var input=UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.InputHandler>();
if(!UnityEditor.EditorApplication.isPlaying || !world || !input || !world.FlatPixelModeActive || d.GetData("G2Action.Capture")==null)throw new System.InvalidOperationException("Arm the ordinary flat capture first.");
var currentGraph=typeof(CavesOfOoo.Rendering.FlatPixelFallbackCatalog).GetMethod("IsCurrentGraph",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
var selectedOwner=typeof(CavesOfOoo.Rendering.FlatPixelGreyboxCatalog).GetMethod("VisibleOwner",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
var spatialZoneField=typeof(CavesOfOoo.Core.Entity).GetField("SpatialZone",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
if(spatialZoneField==null)throw new System.InvalidOperationException("Expected internal Entity.SpatialZone field absent.");
if(currentGraph==null || selectedOwner==null)throw new System.InvalidOperationException("Expected read-only selector seam absent.");
System.Func<CavesOfOoo.Core.Zone,bool> current=z=>world && z!=null && object.ReferenceEquals(world.CurrentZone,z) && (bool)currentGraph.Invoke(null,new object[]{z});
System.Func<CavesOfOoo.Core.Zone,int,int,bool> disclosed=(z,x,y)=>{if(!current(z))return false;var c=z.GetCell(x,y);return c!=null && c.Explored && c.IsVisible && object.ReferenceEquals(c.ParentZone,z) && object.ReferenceEquals(z.GetCell(c.X,c.Y),c);};
System.Func<CavesOfOoo.Core.Entity,CavesOfOoo.Core.Zone,bool> visible=(e,z)=>{
 if(e==null || !current(z) || !object.ReferenceEquals(spatialZoneField.GetValue(e),z))return false;
 var c=z.GetEntityCell(e);var r=e.GetPart<CavesOfOoo.Core.RenderPart>();var p=e.GetPart<CavesOfOoo.Core.PhysicsPart>();
 return c!=null && disclosed(z,c.X,c.Y) && c.Objects.Contains(e) && r!=null && r.ParentEntity==e && r.Visible
  && p!=null && p.ParentEntity==e && p.InInventory==null && p.Equipped==null
  && object.ReferenceEquals(selectedOwner.Invoke(null,new object[]{z,c}),e);
};
var events=new System.Collections.Generic.List<object>();
string captureSession=(string)d.GetData("G2Action.Output");
string eventDir=System.IO.Path.GetFullPath("Docs/Verification/Live16x24/"+captureSession);
string eventPath=System.IO.Path.Combine(eventDir,"events-"+System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+System.Guid.NewGuid().ToString("N")+".json");
d.SetData("G2Action.EventOutput",eventPath);d.SetData("G2Action.EventFinishResult",null);
CavesOfOoo.Core.EntityAttackVisualHandler attack=(a,b,z)=>{try{
 bool av=visible(a,z),bv=visible(b,z);if(events.Count<128 && (av||bv))events.Add(new{kind="attack",frame=UnityEngine.Time.frameCount,time=UnityEngine.Time.unscaledTime,at=System.DateTime.UtcNow.ToString("O"),tick=input.TurnManager.TickCount,actor=av?a.ID:null,target=bv?b.ID:null,actorDisclosed=av,targetDisclosed=bv,zone=z.ZoneID});
}catch{}};
CavesOfOoo.Core.EntityDamageVisualHandler damage=(target,source,z,amount,lethal)=>{try{
 bool sv=visible(source,z),tv=visible(target,z);if(events.Count<128 && (sv||tv))events.Add(new{kind="damage",frame=UnityEngine.Time.frameCount,time=UnityEngine.Time.unscaledTime,at=System.DateTime.UtcNow.ToString("O"),tick=input.TurnManager.TickCount,actor=sv?source.ID:null,target=tv?target.ID:null,actorDisclosed=sv,targetDisclosed=tv,amount,hp=tv?(int?)target.GetStatValue("Hitpoints"):null,lethal,zone=z.ZoneID});
}catch{}};
int castSequence=0;
CavesOfOoo.Core.EntityCastVisualHandler cast=(caster,z,spellID,sx,sy,tx,ty,duration)=>{try{
 bool sourceVisible=visible(caster,z) && disclosed(z,sx,sy);
 var sourceCell=sourceVisible?z.GetEntityCell(caster):null;
 bool exactSource=sourceCell!=null && sourceCell.X==sx && sourceCell.Y==sy;
 bool targetVisible=disclosed(z,tx,ty);
 // A visible target involvement is retained even if its genuine source is hidden. Redact undisclosed source metadata.
 if(events.Count>=128 || (!sourceVisible && !targetVisible))return;
 bool sampling=sourceVisible && exactSource && caster.BlueprintName=="MarlbackCindercaller" && d.GetData("G2Action.Capture")!=null && !object.Equals(d.GetData("G2Action.Stop"),true);
 float now=UnityEngine.Time.unscaledTime;
 events.Add(new{kind="cast",frame=UnityEngine.Time.frameCount,time=now,at=System.DateTime.UtcNow.ToString("O"),tick=input.TurnManager.TickCount,actor=sourceVisible?caster.ID:null,blueprint=sourceVisible?caster.BlueprintName:null,spell=spellID,source=sourceVisible?new[]{sx,sy}:null,target=targetVisible?new[]{tx,ty}:null,sourceDisclosed=sourceVisible,targetDisclosed=targetVisible,exactCurrentSource=exactSource,duration,zone=z.ZoneID,samplingTriggered=sampling});
 if(sampling){castSequence++;d.SetData("G2Action.CastSample",new double[]{castSequence,now,now+0.8});}
}catch{}};
CavesOfOoo.Core.EntityVisualHooks.AttackCallback+=attack;CavesOfOoo.Core.EntityVisualHooks.DamageCallback+=damage;CavesOfOoo.Core.EntityVisualHooks.CastCallback+=cast;
d.SetData("G2Action.AttackObserver",attack);d.SetData("G2Action.DamageObserver",damage);d.SetData("G2Action.CastObserver",cast);d.SetData("G2Action.Events",events);
return new{observing=true,maximumEvents=128,path=eventPath,bounds="Only disclosed visible involvement is recorded. A selected current visible Cindercaller source arms up to0.8s of50ms signature sampling. No event dispatch or timing/world mutation."};
