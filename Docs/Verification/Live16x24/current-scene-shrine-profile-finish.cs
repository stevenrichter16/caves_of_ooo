var records=(System.Collections.Generic.List<Unity.Profiling.ProfilerRecorder>)System.AppDomain.CurrentDomain.GetData("RiverShrine.Recorders");
var names=(string[])System.AppDomain.CurrentDomain.GetData("RiverShrine.Names");
var start=(System.DateTime)System.AppDomain.CurrentDomain.GetData("RiverShrine.Start");
var rows=new System.Collections.Generic.List<object>();
for(int i=0;i<records.Count;i++){
 var rec=records[i];rec.Stop(); var samples=new System.Collections.Generic.List<Unity.Profiling.ProfilerRecorderSample>();rec.CopyTo(samples);
 var values=new System.Collections.Generic.List<long>();long total=0;foreach(var s in samples){values.Add(s.Value);total+=s.Value;}values.Sort();
 rows.Add(new{name=names[i],valid=rec.Valid,count=values.Count,capacity=rec.Capacity,wrapped=rec.WrappedAround,unit=rec.UnitType.ToString(),mean=values.Count==0?0:(double)total/values.Count,max=values.Count==0?0:values[values.Count-1],p99=values.Count==0?0:values[(int)((values.Count-1)*.99)]});rec.Dispose();
}
System.AppDomain.CurrentDomain.SetData("RiverShrine.Recorders",null);
var r=UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.ZoneRenderer>();var c=r.CurrentZone.GetEntityCell(r.PlayerEntity);
var result=new{label="current-scene-shrine-profile",start,end=System.DateTime.UtcNow,elapsedSeconds=(System.DateTime.UtcNow-start).TotalSeconds,frames=UnityEngine.Time.frameCount-(int)System.AppDomain.CurrentDomain.GetData("RiverShrine.Frame"),zone=r.CurrentZone.ZoneID,player=new[]{c.X,c.Y},hp=r.PlayerEntity.GetStatValue("Hitpoints"),rows,bounds="Editor ordinary North shrine walking/zoom/save/reload, with idle/tool time. No controlled baseline, hardware Deck, standalone or isolated-renderer attribution."};
System.IO.File.WriteAllText("Docs/Verification/Live16x24/current-scene-shrine-profile.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
