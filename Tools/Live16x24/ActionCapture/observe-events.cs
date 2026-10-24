// Additive read-only observer; remove with finish-events.cs before leaving Play.
var d=System.AppDomain.CurrentDomain;if(d.GetData("G2Action.AttackObserver")!=null)throw new System.InvalidOperationException("Already observing.");
var events=new System.Collections.Generic.List<object>();
string captureSession=d.GetData("G2Action.Capture")!=null?(string)d.GetData("G2Action.Output"):null;
string eventDir=System.IO.Path.GetFullPath("Docs/Verification/Live16x24/"+(captureSession??""));
string eventPath=System.IO.Path.Combine(eventDir,"events-"+System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+System.Guid.NewGuid().ToString("N")+".json");
d.SetData("G2Action.EventOutput",eventPath);d.SetData("G2Action.EventFinishResult",null);
CavesOfOoo.Core.EntityAttackVisualHandler attack=(a,b,z)=>{try{if(events.Count<128)events.Add(new{kind="attack",frame=UnityEngine.Time.frameCount,time=UnityEngine.Time.unscaledTime,actor=a.ID,target=b.ID,zone=z.ZoneID});}catch{}};
CavesOfOoo.Core.EntityDamageVisualHandler damage=(target,source,z,amount,lethal)=>{try{if(events.Count<128)events.Add(new{kind="damage",frame=UnityEngine.Time.frameCount,time=UnityEngine.Time.unscaledTime,actor=source==null?null:source.ID,target=target.ID,amount,hp=target.GetStatValue("Hitpoints"),lethal,zone=z.ZoneID});}catch{}};
CavesOfOoo.Core.EntityVisualHooks.AttackCallback+=attack;CavesOfOoo.Core.EntityVisualHooks.DamageCallback+=damage;
d.SetData("G2Action.AttackObserver",attack);d.SetData("G2Action.DamageObserver",damage);d.SetData("G2Action.Events",events);
return new{observing=true,maximumEvents=128,path=eventPath};
