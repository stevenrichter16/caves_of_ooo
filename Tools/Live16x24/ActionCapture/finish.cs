// Stop observation before PNG encoding. Repeat calls return the first result.
// Captured pixels are not edited; previously exported files are never replaced.
var domain=System.AppDomain.CurrentDomain;
var completed=domain.GetData("G2Action.FinishResult");
if(completed!=null)return completed;
domain.SetData("G2Action.Stop",true);
var renderer=(CavesOfOoo.Rendering.AnimatedEntityRenderer)domain.GetData("G2Action.Renderer");
var coroutine=(UnityEngine.Coroutine)domain.GetData("G2Action.Coroutine");
if(renderer&&coroutine!=null)renderer.StopCoroutine(coroutine);
domain.SetData("G2Action.Capture",null);domain.SetData("G2Action.Coroutine",null);
var frames=(System.Collections.Generic.List<object>)domain.GetData("G2Action.Frames");
var pixels=(System.Collections.Generic.Dictionary<int,UnityEngine.Texture2D>)domain.GetData("G2Action.Pixels");
var errors=(System.Collections.Generic.List<string>)domain.GetData("G2Action.Errors");
string output=(string)domain.GetData("G2Action.Output");
if(frames==null||pixels==null||errors==null||string.IsNullOrEmpty(output))return new{finished=false,reason="No capture is armed."};
string dir=System.IO.Path.GetFullPath("Docs/Verification/Live16x24/"+output);
string indexPath=System.IO.Path.Combine(dir,"index.json");
// Also protect an export made by the earlier helper before FinishResult existed.
if(frames.Count==0&&System.IO.File.Exists(indexPath)){
 var preserved=new{finished=true,alreadyExported=true,directory=dir,indexPreserved=true};
 domain.SetData("G2Action.FinishResult",preserved);return preserved;
}
int count=frames.Count,saved=0;bool indexSaved=false;
try{
 System.IO.Directory.CreateDirectory(dir);
 if(System.IO.File.Exists(indexPath))throw new System.IO.IOException("Existing capture index preserved: "+indexPath);
 foreach(var frame in frames){
  var row=Newtonsoft.Json.Linq.JObject.FromObject(frame);int i=(int)row["sequence"];UnityEngine.Texture2D tex;
  if(!pixels.TryGetValue(i,out tex)||!tex)continue;
  row["captureWidth"]=tex.width;row["captureHeight"]=tex.height;
  string png=System.IO.Path.Combine(dir,"frame-"+i.ToString("D2")+".png");
  string json=System.IO.Path.Combine(dir,"frame-"+i.ToString("D2")+".json");
  if(System.IO.File.Exists(png)||System.IO.File.Exists(json))throw new System.IO.IOException("Existing capture frame preserved: "+png);
  System.IO.File.WriteAllBytes(png,tex.EncodeToPNG());
  System.IO.File.WriteAllText(json,row.ToString());saved++;
 }
 System.IO.File.WriteAllText(indexPath,Newtonsoft.Json.JsonConvert.SerializeObject(new{frames,errors,bounds="Actual WaitForEndOfFrame screenshots from ordinary Play. Selective pose sampling and synchronous readback overhead: not continuous video or timing/performance proof."},Newtonsoft.Json.Formatting.Indented));
 indexSaved=true;
}catch(System.Exception ex){errors.Add(ex.ToString());}
finally{foreach(var tex in pixels.Values)if(tex)UnityEngine.Object.Destroy(tex);frames.Clear();pixels.Clear();}
var result=new{finished=true,captured=count,saved,indexSaved,errors=errors.ToArray(),directory=dir};
domain.SetData("G2Action.FinishResult",result);
return result;
