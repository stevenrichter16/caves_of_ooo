var records=(System.Collections.Generic.List<Unity.Profiling.ProfilerRecorder>)System.AppDomain.CurrentDomain.GetData("SoddenOrdinary.Recorders");
var names=(string[])System.AppDomain.CurrentDomain.GetData("SoddenOrdinary.Names");
var start=(System.DateTime)System.AppDomain.CurrentDomain.GetData("SoddenOrdinary.Start");
var rows=new System.Collections.Generic.List<object>();
for(int i=0;i<records.Count;i++){
 var rec=records[i];rec.Stop(); var samples=new System.Collections.Generic.List<Unity.Profiling.ProfilerRecorderSample>();rec.CopyTo(samples);
 var values=new System.Collections.Generic.List<long>();long total=0;foreach(var s in samples){values.Add(s.Value);total+=s.Value;}values.Sort();
 rows.Add(new{name=names[i],valid=rec.Valid,count=values.Count,capacity=rec.Capacity,wrapped=rec.WrappedAround,unit=rec.UnitType.ToString(),mean=values.Count==0?0:(double)total/values.Count,max=values.Count==0?0:values[values.Count-1],p99=values.Count==0?0:values[(int)((values.Count-1)*.99)]});rec.Dispose();
}
System.AppDomain.CurrentDomain.SetData("SoddenOrdinary.Recorders",null);
var r=UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.ZoneRenderer>();var c=r.CurrentZone.GetEntityCell(r.PlayerEntity);
var result=new{label="sodden-ordinary-profile",start,end=System.DateTime.UtcNow,elapsedSeconds=(System.DateTime.UtcNow-start).TotalSeconds,frames=UnityEngine.Time.frameCount-(int)System.AppDomain.CurrentDomain.GetData("SoddenOrdinary.Frame"),zone=r.CurrentZone.ZoneID,player=new[]{c.X,c.Y},hp=r.PlayerEntity.GetStatValue("Hitpoints"),rows,bounds="Editor ordinary Sodden six-formation world travel and terrain inspection, with idle/tool time. No controlled baseline, hardware Deck, standalone or isolated-renderer attribution."};
System.IO.File.WriteAllText("Docs/Verification/Live16x24/sodden-ordinary-profile.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
