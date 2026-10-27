// Unsubscribe first, then export once to this observer session's unique path.
var d=System.AppDomain.CurrentDomain;
var completed=d.GetData("G2Action.EventFinishResult");if(completed!=null)return completed;
var a=(CavesOfOoo.Core.EntityAttackVisualHandler)d.GetData("G2Action.AttackObserver");
var h=(CavesOfOoo.Core.EntityDamageVisualHandler)d.GetData("G2Action.DamageObserver");
var c=(CavesOfOoo.Core.EntityCastVisualHandler)d.GetData("G2Action.CastObserver");
if(a!=null)CavesOfOoo.Core.EntityVisualHooks.AttackCallback-=a;
if(h!=null)CavesOfOoo.Core.EntityVisualHooks.DamageCallback-=h;
if(c!=null)CavesOfOoo.Core.EntityVisualHooks.CastCallback-=c;
d.SetData("G2Action.AttackObserver",null);d.SetData("G2Action.DamageObserver",null);d.SetData("G2Action.CastObserver",null);d.SetData("G2Action.CastSample",null);
var events=(System.Collections.Generic.List<object>)d.GetData("G2Action.Events");
if(events==null)return new{observing=false,observersRemoved=true,reason="No event observer is armed."};
string path=(string)d.GetData("G2Action.EventOutput");
// An observer armed by the earlier helper has no path yet; still avoid its fixed filename.
if(string.IsNullOrEmpty(path))path=System.IO.Path.GetFullPath("Docs/Verification/Live16x24/G2ActionEvents-"+System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+System.Guid.NewGuid().ToString("N")+".json");
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
if(System.IO.File.Exists(path))throw new System.IO.IOException("Existing event session preserved: "+path);
System.IO.File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(events,Newtonsoft.Json.Formatting.Indented));
var result=new{events=events.Count,observersRemoved=true,path};
d.SetData("G2Action.EventFinishResult",result);events.Clear();d.SetData("G2Action.Events",null);
return result;
