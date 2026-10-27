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
// Read the bound renderer's already-painted ownership receipt; never resolve/paint it again.
var flags = System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var env = (CavesOfOoo.Rendering.EnvironmentSpriteRenderer)typeof(CavesOfOoo.Rendering.ZoneRenderer).GetField("_envSpriteRenderer", flags).GetValue(world);
if (!env) throw new System.InvalidOperationException("Bound environment renderer required.");
var paintField = env.GetType().GetField("_flatPaintReceipts", flags);
var overlay = (UnityEngine.Tilemaps.Tilemap)env.GetType().GetField("_overlayTilemap", flags).GetValue(env);
var currentGraph = typeof(CavesOfOoo.Rendering.FlatPixelFallbackCatalog).GetMethod("IsCurrentGraph", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
var selectedOwner = typeof(CavesOfOoo.Rendering.FlatPixelGreyboxCatalog).GetMethod("VisibleOwner", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
var spatialZoneField=typeof(CavesOfOoo.Core.Entity).GetField("SpatialZone",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
if(spatialZoneField==null)throw new System.InvalidOperationException("Expected internal Entity.SpatialZone field absent.");
if (paintField == null || !overlay || currentGraph == null || selectedOwner == null) throw new System.InvalidOperationException("Expected saved read-only capture seams absent.");
domain.SetData("G2Action.CastSample", null);
string previous = null;
string previousCastSignature = null;
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
 var animatedIDs=new System.Collections.Generic.HashSet<string>();
 foreach(System.Collections.DictionaryEntry pair in views){
  var entity=(CavesOfOoo.Core.Entity)pair.Key;var view=pair.Value;var t=view.GetType();
  var body=(UnityEngine.SpriteRenderer)t.GetField("Body").GetValue(view); if(!body || !body.enabled || !body.sprite) continue;
  var root=(UnityEngine.Transform)t.GetField("RootTransform").GetValue(view);var s=body.sprite;
  animatedIDs.Add(entity.ID);
  string key=entity.ID+":"+s.name+":"+body.flipX;keys.Add(key);
  var p=UnityEngine.Camera.main.WorldToScreenPoint(root.position);
  rows.Add(new {id=entity.ID, blueprint=entity.BlueprintName, hp=entity.GetStatValue("Hitpoints"), sprite=s.name, atlas=s.texture.name, rect=new[]{s.rect.x,s.rect.y,s.rect.width,s.rect.height}, state=t.GetField("State").GetValue(view).ToString(), stateTime=(float)t.GetField("StateTime").GetValue(view), pending=(bool)t.GetField("HasPendingReaction").GetValue(view), pendingState=t.GetField("PendingReaction").GetValue(view).ToString(), dying=(bool)t.GetField("Dying").GetValue(view), cell=new[]{(int)t.GetField("CellX").GetValue(view),(int)t.GetField("CellY").GetValue(view)}, world=new[]{root.position.x,root.position.y}, screen=new[]{p.x,p.y},flipX=body.flipX,alpha=body.color.a});
 }
 // Static identities have a separate collection; an animated owner ID is never duplicated here.
 var staticActors=new System.Collections.Generic.List<object>();
 var zone=world.CurrentZone;
 if(zone!=null && (bool)currentGraph.Invoke(null,new object[]{zone}) && env.RenderingEnabled && env.FlatPixelGreyboxEnabled){
  var receipts=(System.Array)paintField.GetValue(env);
  foreach(var entity in zone.GetReadOnlyEntities()){
   if(entity==null || entity.BlueprintName!="MarlbackCindercaller" || animatedIDs.Contains(entity.ID))continue;
   var cell=zone.GetEntityCell(entity);
   if(cell==null || !cell.Explored || !cell.IsVisible || !object.ReferenceEquals(cell.ParentZone,zone)
      || !object.ReferenceEquals(zone.GetCell(cell.X,cell.Y),cell) || !object.ReferenceEquals(spatialZoneField.GetValue(entity),zone)
      || !cell.Objects.Contains(entity) || !object.ReferenceEquals(selectedOwner.Invoke(null,new object[]{zone,cell}),entity))continue;
   var render=entity.GetPart<CavesOfOoo.Core.RenderPart>();var physics=entity.GetPart<CavesOfOoo.Core.PhysicsPart>();
   if(render==null || !render.Visible || render.ParentEntity!=entity || physics==null || physics.ParentEntity!=entity || physics.InInventory!=null || physics.Equipped!=null)continue;
   var receipt=receipts.GetValue(cell.Y*CavesOfOoo.Core.Zone.Width+cell.X);var rt=receipt.GetType();
   if(!object.ReferenceEquals(rt.GetField("Zone",flags).GetValue(receipt),zone)
      || !object.ReferenceEquals(rt.GetField("Cell",flags).GetValue(receipt),cell)
      || !object.ReferenceEquals(rt.GetField("Owner",flags).GetValue(receipt),entity)
      || !object.ReferenceEquals(rt.GetField("Render",flags).GetValue(receipt),render))continue;
   var pos=new UnityEngine.Vector3Int(cell.X,CavesOfOoo.Core.Zone.Height-1-cell.Y,0);
   var sprite=overlay.GetSprite(pos);if(!sprite)continue;
   var center=overlay.GetCellCenterWorld(pos);var screen=UnityEngine.Camera.main.WorldToScreenPoint(center);
   keys.Add("static:"+entity.ID+":"+cell.X+":"+cell.Y+":"+entity.GetStatValue("Hitpoints")+":"+sprite.name+":"+render.VisualFacing);
   staticActors.Add(new{id=entity.ID,blueprint=entity.BlueprintName,hp=entity.GetStatValue("Hitpoints"),cell=new[]{cell.X,cell.Y},sprite=sprite.name,atlas=sprite.texture.name,rect=new[]{sprite.rect.x,sprite.rect.y,sprite.rect.width,sprite.rect.height},world=new[]{center.x,center.y},screen=new[]{screen.x,screen.y},facing=render.VisualFacing.ToString(),presentation="static painted tile",visible=true,explored=true});
  }
 }
 // Only a genuine disclosed current caster hook arms this finite time-based signature.
 // Quantization requests at most ~20fps; actual EOF/readback can be slower and is not a timing benchmark.
 var castSample=domain.GetData("G2Action.CastSample") as double[];
 int castSampleBucket=-1;
 string castSignature=null;
 if(castSample!=null && castSample.Length==3 && UnityEngine.Time.unscaledTime>=castSample[1] && UnityEngine.Time.unscaledTime<castSample[2]){
  castSampleBucket=(int)System.Math.Floor((UnityEngine.Time.unscaledTime-castSample[1])/0.05);
  castSignature="cast-sample:"+castSample[0]+":"+castSampleBucket;
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
 bool newCastBucket=castSignature!=null && castSignature!=previousCastSignature;
 if((signature==previous && !newCastBucket) || (rows.Count==0 && staticActors.Count==0))return; previous=signature;
 if(castSignature!=null)previousCastSignature=castSignature;
 int sequence=frames.Count;var playerCell=world.CurrentZone.GetEntityCell(world.PlayerEntity);
 frames.Add(new {sequence,frame=UnityEngine.Time.frameCount,at=System.DateTime.UtcNow.ToString("O"),unscaledTime=UnityEngine.Time.unscaledTime,deltaTime=UnityEngine.Time.unscaledDeltaTime,zone=world.CurrentZone.ZoneID,tick=input.TurnManager.TickCount,player=new[]{playerCell.X,playerCell.Y},hp=world.PlayerEntity.GetStatValue("Hitpoints"),width,height,actors=rows,staticActors,castSampleBucket,readouts});
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
return new {armed=true,width,height,maxFrames=24,castSamplingSeconds=0.8,castSamplingBucketSeconds=0.05,bounds="Actual render-frame observation; Synchronous screenshot/metadata overhead prevents performance inference. No pixel resampling or gameplay changes."};
