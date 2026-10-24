// Execute as a method body through Unity MCP during an isolated ordinary Play session.
// Observes only; never emits gameplay events, changes timing or renders a second camera.
var domain = System.AppDomain.CurrentDomain;
if (domain.GetData("G2Action.Capture") != null) throw new System.InvalidOperationException("Capture already armed.");
var world = UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.ZoneRenderer>();
var renderer = world == null ? null : (CavesOfOoo.Rendering.AnimatedEntityRenderer)typeof(CavesOfOoo.Rendering.ZoneRenderer).GetField("_entityVisualRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(world);
var input = UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.InputHandler>();
if (!UnityEditor.EditorApplication.isPlaying || renderer == null || world == null || input == null || !world.FlatPixelModeActive) throw new System.InvalidOperationException("Ordinary flat Play required.");
var field = renderer.GetType().GetField("_views", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var fx = typeof(CavesOfOoo.Rendering.ZoneRenderer).GetField("_asciiFxRenderer", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(world);
var readoutField = fx.GetType().GetField("_eventTexts", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
var readoutRow = fx.GetType().GetMethod("EventTextRow", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
string previous = null;
var frames = new System.Collections.Generic.List<object>();
var pixels = new System.Collections.Generic.Dictionary<int, UnityEngine.Texture2D>();
var errors = new System.Collections.Generic.List<string>();

var size = new UnityEngine.Vector2(1280,800);
var gvType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
var gameViews = UnityEngine.Resources.FindObjectsOfTypeAll(gvType);
if (gameViews.Length > 0) size=(UnityEngine.Vector2)gvType.GetProperty("targetSize", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(gameViews[0],null);
int width=(int)size.x, height=(int)size.y, lastFrame=-1;
if(width!=1280||height!=800) throw new System.InvalidOperationException("Expected actual1280x800 Game target, got "+size);
System.Action capture=null;
capture=()=>{
 if(!UnityEditor.EditorApplication.isPlaying || !renderer || !world || frames.Count>=24 || UnityEngine.Time.frameCount==lastFrame) return;
 lastFrame=UnityEngine.Time.frameCount;
 try {
 var views=(System.Collections.IDictionary)field.GetValue(renderer);
 var rows=new System.Collections.Generic.List<object>();
 var keys=new System.Collections.Generic.List<string>();
 foreach(System.Collections.DictionaryEntry pair in views){
  var entity=(CavesOfOoo.Core.Entity)pair.Key;var view=pair.Value;var t=view.GetType();
  var body=(UnityEngine.SpriteRenderer)t.GetField("Body").GetValue(view); if(!body || !body.enabled || !body.sprite) continue;
  var root=(UnityEngine.Transform)t.GetField("RootTransform").GetValue(view);var s=body.sprite;
  string key=entity.ID+":"+s.name+":"+body.flipX;keys.Add(key);
  var p=UnityEngine.Camera.main.WorldToScreenPoint(root.position);
  rows.Add(new {id=entity.ID, blueprint=entity.BlueprintName, hp=entity.GetStatValue("Hitpoints"), sprite=s.name, atlas=s.texture.name, rect=new[]{s.rect.x,s.rect.y,s.rect.width,s.rect.height}, state=t.GetField("State").GetValue(view).ToString(), stateTime=(float)t.GetField("StateTime").GetValue(view), pending=(bool)t.GetField("HasPendingReaction").GetValue(view), pendingState=t.GetField("PendingReaction").GetValue(view).ToString(), dying=(bool)t.GetField("Dying").GetValue(view), cell=new[]{(int)t.GetField("CellX").GetValue(view),(int)t.GetField("CellY").GetValue(view)}, world=new[]{root.position.x,root.position.y}, screen=new[]{p.x,p.y},flipX=body.flipX,alpha=body.color.a});
 }
 var readouts=new System.Collections.Generic.List<object>();
 foreach(var label in (System.Collections.IEnumerable)readoutField.GetValue(fx)){
  var t=label.GetType();if(!(bool)t.GetField("Placed").GetValue(label)||(float)t.GetField("DelayRemaining").GetValue(label)>0f)continue;
  var owner=(CavesOfOoo.Core.Entity)t.GetField("Anchor").GetValue(label);
  string text=(string)t.GetField("Text").GetValue(label);int x=(int)t.GetField("StartX").GetValue(label),y=(int)readoutRow.Invoke(null,new[]{label});
  keys.Add("readout:"+(owner==null?"origin":owner.ID)+":"+text+":"+x+":"+y);
  readouts.Add(new{owner=owner==null?null:owner.ID,text,number=(int)t.GetField("Number").GetValue(label),numeric=(bool)t.GetField("Numeric").GetValue(label),x,y,width=text.Length,lane=(int)t.GetField("Lane").GetValue(label),remaining=(float)t.GetField("Remaining").GetValue(label),originOnly=(bool)t.GetField("AllowOriginOnly").GetValue(label)});
 }
 keys.Sort();string signature=string.Join("|",keys.ToArray());
 if(signature==previous || rows.Count==0)return; previous=signature;
 int sequence=frames.Count;var cell=world.CurrentZone.GetEntityCell(world.PlayerEntity);
 frames.Add(new {sequence,frame=UnityEngine.Time.frameCount,at=System.DateTime.UtcNow.ToString("O"),unscaledTime=UnityEngine.Time.unscaledTime,deltaTime=UnityEngine.Time.unscaledDeltaTime,zone=world.CurrentZone.ZoneID,tick=input.TurnManager.TickCount,player=new[]{cell.X,cell.Y},hp=world.PlayerEntity.GetStatValue("Hitpoints"),width,height,actors=rows,readouts});
 pixels[sequence]=UnityEngine.ScreenCapture.CaptureScreenshotAsTexture();
 }catch(System.Exception ex){ errors.Add(ex.ToString());domain.SetData("G2Action.Stop",true); }
};
domain.SetData("G2Action.FinishResult",null);
domain.SetData("G2Action.Output", "G2Action-"+System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
domain.SetData("G2Action.Capture",capture);domain.SetData("G2Action.Frames",frames);domain.SetData("G2Action.Pixels",pixels);domain.SetData("G2Action.Errors",errors);domain.SetData("G2Action.Stop",false);
var wait = new UnityEngine.WaitForEndOfFrame(); bool first=true;
var started=UnityEngine.Time.realtimeSinceStartup;
var enumerator=System.Linq.Enumerable.Range(0,int.MaxValue).TakeWhile(i=>!object.Equals(domain.GetData("G2Action.Stop"),true)&&UnityEditor.EditorApplication.isPlaying&&frames.Count<24&&UnityEngine.Time.realtimeSinceStartup-started<300f).Select(i=>{if(!first)capture();first=false;return wait;}).GetEnumerator();
var routine=renderer.StartCoroutine(enumerator);domain.SetData("G2Action.Coroutine",routine);domain.SetData("G2Action.Renderer",renderer);
return new {armed=true,width,height,maxFrames=24,bounds="Actual render-frame observation; Synchronous screenshot/metadata overhead prevents performance inference. No pixel resampling or gameplay changes."};
