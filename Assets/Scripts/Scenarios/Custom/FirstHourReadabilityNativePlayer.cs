using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Scenarios.Custom
{
 // Staged UI acceptance only. No ordinary acquisition, encounter, or thermal balance claim.
 public sealed class FirstHourReadabilityNativePlayer:MonoBehaviour
 {
  const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  public string RunId{get;}=Guid.NewGuid().ToString("N"); public bool Finished{get;private set;}
  public int Failures=>failures+unexpected; public string ReportPath{get;private set;}
  ScenarioContext context;InputHandler input;Keyboard keyboard,oldKeyboard;InputSettings settings,oldSettings;
  bool oldBackground,cleaned,errorsFinalized;int failures,unexpected;string fatal,root,baseline;Entity target,item;DemoAction action;
  System.Diagnostics.Stopwatch clock;readonly List<string> captures=new List<string>();readonly List<Check> checks=new List<Check>();readonly List<KeyRow> keys=new List<KeyRow>();readonly List<Page> pages=new List<Page>();BoundsRow bounds;
  string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/SpreadFirstHour/M2/Native",RunId));
  public void Initialize(ScenarioContext value)
  {
   if(string.IsNullOrEmpty(SaveGameService.SaveRootOverride))throw new InvalidOperationException("Owned save isolation is required.");
   context=value;root=SaveGameService.SaveRootOverride;clock=System.Diagnostics.Stopwatch.StartNew();oldKeyboard=Keyboard.current;oldSettings=InputSystem.settings;
   settings=Instantiate(oldSettings);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
   settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
   InputSystem.settings=settings;oldBackground=Application.runInBackground;Application.runInBackground=true;keyboard=InputSystem.AddDevice<Keyboard>();StartCoroutine(Safe(Run()));
  }
  IEnumerator Run()
  {
   yield return new WaitForSecondsRealtime(.8f);input=FindFirstObjectByType<InputHandler>();Require(input!=null,"ordinary bootstrap");
   var boot=(BootMenuController)Field(input,"_bootMenuController");Require(boot!=null&&boot.IsActive,"isolated boot marker");yield return Tap(Key.N);yield return Settled();
   Require(!boot.IsActive&&!DevMode.Enabled&&SaveGameService.SaveRootOverride==root,"native N and owned save root");
   Stage();yield return null;baseline=Signature();yield return Capture("01-staged-current-world");
   yield return OwnerMenu();var camera=CameraState();yield return MenuAction("Examine");Require(State()=="AnnouncementOpen"&&input.AnnouncementUI.IsOpen,"actual world Examine reader");
   string first=string.Join("\n",input.AnnouncementUI.VisibleLines);Require(first.Contains("Hot steam")&&first.Contains("On the ground:"),"warning and ground are first-page visible");
   yield return ReadPages("02-world",true);yield return Tap(Key.Escape);Require(State()=="LookMode"&&!input.AnnouncementUI.IsOpen,"reader returns to prior Look");Require(CameraState()==camera,"reader preserves gameplay camera");Free("world_reader_free");
   yield return Tap(Key.Enter);yield return SelectOwner();Require(ReferenceEquals(input.WorldActionMenuUI.SelectedTarget,target),"reopen exact source");
   yield return MoveMenu("ReadabilityDemo");yield return Tap(Key.F1);Require(State()=="AnnouncementOpen","native F1 recovery");yield return ReadPages("03-action",false);
   Require(action.Executed==0,"reading does not execute fixture action");yield return Tap(Key.Escape);Require(State()=="WorldActionMenuOpen"&&input.WorldActionMenuUI.IsOpen,"F1 returns actual menu");
   var current=(List<InventoryAction>)Field(input.WorldActionMenuUI,"_actions");Require(current[(int)Field(input.WorldActionMenuUI,"_cursorIndex")].Command=="ReadabilityDemo","restored selected current action");
   yield return Capture("03-action-menu-return");yield return Tap(Key.Escape);Require(State()=="LookMode","menu returns Look");yield return Tap(Key.Escape);yield return Settled();Free("action_details_free");
   // An explicit diagnostic state change while paused; no claim of natural cooling here.
   target.GetPart<ThermalPart>().Temperature=25;baseline=Signature();yield return OwnerMenu();yield return MenuAction("Examine");
   Require(string.Join("\n",input.AnnouncementUI.VisibleLines).Contains("Steam - cools")&&!string.Join("\n",input.AnnouncementUI.VisibleLines).Contains("Hot steam"),"reopening reads current cool source");
   yield return Capture("04-current-cooled-description");yield return Tap(Key.Escape);yield return Tap(Key.Escape);yield return Settled();Free("reopened_current_reader_free");
   yield return InventoryCompare();Require(State()=="AnnouncementOpen","actual comparison through inventory action");
   Require(string.Join("\n",input.AnnouncementUI.VisibleLines).Contains(item.GetDisplayName()),"comparison names exact carried candidate");yield return ReadPages("05-equipment",false);
   yield return Tap(Key.Escape);Require(State()=="InventoryOpen"&&input.InventoryUI.IsOpen,"comparison returns current inventory");yield return Capture("05-inventory-return");yield return Tap(Key.I);yield return Settled();Free("comparison_free");
   MeasureSidebar();Require(bounds.glyphs>15&&bounds.outside==0&&bounds.top>=13.49f&&bounds.bottom>=13.49f,"actual 1920 glyph fit and vertical inset");yield return Capture("06-sidebar-bounds");
   CheckResult("all_native_ui_routes_complete",true);
  }
  void Stage()
  {
   var player=input.PlayerEntity;Require(player.GetStat("Hitpoints").Max==40,"ordinary character maxHP");var at=input.CurrentZone.GetEntityPosition(player);Cell cell=null;
   foreach(var d in new[]{(1,0),(0,1),(-1,0),(0,-1)}){var c=input.CurrentZone.GetCell(at.x+d.Item1,at.y+d.Item2);if(c!=null&&c.Explored&&c.IsVisible&&!c.BlocksMovement()&&!c.Occupants.Any(e=>e.HasTag("Creature"))){cell=c;break;}}
   Require(cell!=null,"existing visible adjacent clear fixture cell; no world clearing");target=context.Factory.CreateEntity("OilSeep");Require(target!=null&&input.CurrentZone.AddEntity(target,cell.X,cell.Y),"actual source identity");
   target.GetPart<ExaminablePart>().Text="READABILITY-FLAVOR-BEGIN\n"+string.Join("\n",Enumerable.Range(0,80).Select(i=>"Page line "+i+" keeps this authored paragraph."))+"\nREADABILITY-FLAVOR-END";
   target.ApplyEffect(new SteamEffect(.7f));target.GetPart<ThermalPart>().Temperature=700;input.CurrentZone.TileState.AddHeat(cell.X,cell.Y,1);
   action=new DemoAction();target.AddPart(action);item=context.Factory.CreateEntity("ShortSword");Require(input.PlayerEntity.GetPart<InventoryPart>().AddObject(item),"explicit carried comparison candidate");ZoneRenderHooks.MarkFullDirty("StagedFirstHourReadability");
  }
  IEnumerator OwnerMenu()
  {
   Require(State()=="Normal","owner route starts Normal");yield return Tap(Key.L);Require(State()=="LookMode","native Look");var cursor=(WorldCursorState)Field(input,"_worldCursorState");var at=input.CurrentZone.GetEntityPosition(target);
   for(int i=0;cursor.X!=at.x||cursor.Y!=at.y;i++){Require(i<12,"bounded adjacent cursor");yield return Tap(cursor.X<at.x?Key.D:cursor.X>at.x?Key.A:cursor.Y<at.y?Key.S:Key.W);}
   yield return Tap(Key.Enter);yield return SelectOwner();
  }
  IEnumerator SelectOwner()
  {
   Require(State()=="WorldActionMenuOpen","current world menu");string choose=WorldInteractionSystem.PickTargetCommandPrefix+target.ID;var actions=(List<InventoryAction>)Field(input.WorldActionMenuUI,"_actions");
   if(!ReferenceEquals(input.WorldActionMenuUI.SelectedTarget,target)||input.WorldActionMenuUI.SelectedCellIsPile)
   {if(!actions.Any(a=>a.Command==choose)&&actions.Any(a=>a.Command==WorldInteractionSystem.PickCellCommand))yield return MenuAction(WorldInteractionSystem.PickCellCommand);}
   actions=(List<InventoryAction>)Field(input.WorldActionMenuUI,"_actions");if(actions.Any(a=>a.Command==choose))yield return MenuAction(choose);
   Require(ReferenceEquals(input.WorldActionMenuUI.SelectedTarget,target)&&!input.WorldActionMenuUI.SelectedCellIsPile,"exact source via existing picker");
  }
  IEnumerator MoveMenu(string command)
  {var actions=(List<InventoryAction>)Field(input.WorldActionMenuUI,"_actions");int index=actions.FindIndex(a=>a.Command==command);Require(index>=0,"actual offered world command "+command);for(int n=0;(int)Field(input.WorldActionMenuUI,"_cursorIndex")!=index;n++){Require(n<40,"bounded world selection");yield return Tap((int)Field(input.WorldActionMenuUI,"_cursorIndex")<index?Key.DownArrow:Key.UpArrow);}}
  IEnumerator MenuAction(string command){yield return MoveMenu(command);yield return Tap(Key.Enter);}
  IEnumerator ReadPages(string label,bool requireFlavor)
  {
   var all=new List<string>();int count=input.AnnouncementUI.PageCount;Require(count>0&&count<20,"finite page count");yield return Capture(label+"-first");
   for(int i=0;i<count;i++){int observed=(int)Field(input.AnnouncementUI,"_pageIndex");Require(observed==i,"actual reader page advance to "+i+"; observed "+observed);var text=string.Join("\n",input.AnnouncementUI.VisibleLines);all.Add(text);pages.Add(new Page{label=label,index=i,observedIndex=observed,count=count,text=text});if(i+1<count)yield return Tap(Key.RightArrow);}
   if(requireFlavor){Require(string.Join("\n",all).Contains("READABILITY-FLAVOR-BEGIN")&&string.Join("\n",all).Contains("READABILITY-FLAVOR-END"),"complete world description first and last");Require(count>1,"long actual reader paginates");}
   if(label=="03-action")Require(string.Join("\n",all).Contains("ACTION-DETAIL-BEGIN")&&string.Join("\n",all).Contains("ACTION-DETAIL-END"),"full selected label recovery");
   yield return Capture(label+"-last");
  }
  IEnumerator InventoryCompare()
  {
   yield return Tap(Key.I);Require(State()=="InventoryOpen","native inventory");yield return Tap(Key.Tab);var rows=(IList)Field(input.InventoryUI,"_rows");int row=-1;
   for(int i=0;i<rows.Count;i++)if(ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i],"Item"))?.Item,item)){row=i;break;}
   Require(row>=0,"current candidate row");for(int i=0;(int)Field(input.InventoryUI,"_cursorIndex")!=row;i++){Require(i<80,"bounded inventory row");yield return Tap((int)Field(input.InventoryUI,"_cursorIndex")<row?Key.DownArrow:Key.UpArrow);}
   yield return Tap(Key.Enter);var popup=Field(input.InventoryUI,"_itemActionPopup");Require(popup!=null,"actual item actions");var actions=(IList)Field(popup,"Actions");int index=-1;
   for(int i=0;i<actions.Count;i++)if((string)Field(actions[i],"Command")=="compare_equipment"){index=i;break;}Require(index>=0,"offered comparison action");
   for(int i=0;(int)Field(popup,"CursorIndex")!=index;i++){Require(i<40,"bounded item action");yield return Tap((int)Field(popup,"CursorIndex")<index?Key.DownArrow:Key.UpArrow);}yield return Tap(Key.Enter);
  }
  string Signature()
  {
   var p=input.PlayerEntity;var inv=p.GetPart<InventoryPart>();return input.CurrentZone.ZoneID+"|"+input.TurnManager.TickCount+"|"+input.TurnManager.GetEnergy(p)+"|"+p.GetStatValue("Hitpoints")+"|"+target.GetEffect<SteamEffect>().Density+"|"+target.GetPart<ThermalPart>().Temperature+"|"+
    string.Join(";",input.CurrentZone.GetReadOnlyEntities().OrderBy(e=>e.ID).Select(e=>e.ID+":"+input.CurrentZone.GetEntityPosition(e)+":"+e.GetStatValue("Hitpoints")))+"|"+string.Join(";",inv.Objects.Select(e=>e.ID))+"|"+string.Join(";",inv.EquippedItems.OrderBy(x=>x.Key).Select(x=>x.Key+":"+x.Value?.ID));
  }
  void Free(string label){Require(Signature()==baseline,"no paid turn, HP, entity position/identity, carried/equipped ownership or source changes: "+label);CheckResult(label,true);}
  string CameraState(){var c=input.CameraFollow.GetComponent<Camera>();return c.transform.position.ToString("R")+c.rect.ToString("R")+c.orthographicSize.ToString("R");}
  void MeasureSidebar()
  {
   var map=input.ZoneRenderer.SidebarTilemap;var cam=input.ZoneRenderer.SidebarCamera;var rect=cam.pixelRect;Require(Mathf.Abs(rect.height-1080)<.1f&&Mathf.Abs(rect.xMax-1920)<.1f,"recorded baseline 1920x1080 camera output");
   bounds=new BoundsRow{cameraWidth=rect.width,cameraHeight=rect.height,screenWidth=Screen.width,screenHeight=Screen.height,left=float.MaxValue,right=float.MaxValue,top=float.MaxValue,bottom=float.MaxValue};
   foreach(var cell in map.cellBounds.allPositionsWithin){var sprite=map.GetSprite(cell);if(sprite==null||!sprite.name.StartsWith("Text_"))continue;int code=Convert.ToInt32(sprite.name.Substring(5),16);if(code<=32||code>=127)continue;bounds.glyphs++;var b=sprite.bounds;var center=map.GetCellCenterLocal(cell);var matrix=map.orientationMatrix*map.GetTransformMatrix(cell);bool outside=false;
    for(int i=0;i<4;i++){var p=new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,0);var q=cam.WorldToScreenPoint(map.transform.TransformPoint(center+matrix.MultiplyPoint3x4(p)));bounds.left=Mathf.Min(bounds.left,q.x-rect.xMin);bounds.right=Mathf.Min(bounds.right,rect.xMax-q.x);bounds.bottom=Mathf.Min(bounds.bottom,q.y-rect.yMin);bounds.top=Mathf.Min(bounds.top,rect.yMax-q.y);if(q.x<rect.xMin-.01f||q.x>rect.xMax+.01f||q.y<rect.yMin-.01f||q.y>rect.yMax+.01f)outside=true;}if(outside)bounds.outside++;}
   WriteReport();
  }
  IEnumerator Settled(){double start=Time.realtimeSinceStartupAsDouble;while(State()!="Normal"||input.ZoneRenderer.WorldFx?.HasBlockingFx==true){Require(Time.realtimeSinceStartupAsDouble-start<8,"normal input/FX");yield return null;}yield return null;}
  string State()=>Field(input,"_inputState").ToString();
  static object Field(object owner,string name){Require(owner!=null,"observed owner "+name);var type=owner.GetType();var f=type.GetField(name,Flags);if(f!=null)return f.GetValue(owner);var p=type.GetProperty(name,Flags);if(p!=null)return p.GetValue(owner);throw new InvalidOperationException("Missing observed member "+name);}
  IEnumerator Tap(params Key[] pressed)
  {
   Require(clock.Elapsed.TotalSeconds<180,"finite UI audit deadline");double start=Time.realtimeSinceStartupAsDouble;
   while(input!=null&&Time.time-(float)Field(input,"_lastMoveTime")<input.MoveRepeatDelay){Require(Time.realtimeSinceStartupAsDouble-start<3,"input repeat gate");yield return null;}
   var row=new KeyRow{keys=string.Join("+",pressed),before=input==null?null:State()};keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(pressed));yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);row.after=input==null?null:State();keys.Add(row);WriteReport();
  }
  IEnumerator Capture(string label){yield return new WaitForSecondsRealtime(.12f);yield return new WaitForEndOfFrame();Directory.CreateDirectory(DirectoryPath);string path=Path.Combine(DirectoryPath,label+".png");DensityNativeScreenshot.CaptureToFile(path);Require(File.Exists(path)&&new FileInfo(path).Length>0,"native capture");captures.Add(path);WriteReport();}
  static void Require(bool condition,string reason){if(!condition)throw new InvalidOperationException("First-hour UI precondition: "+reason);}
  void CheckResult(string name,bool pass){checks.Add(new Check{name=name,passed=pass});if(!pass)failures++;}
  IEnumerator Safe(IEnumerator steps){var stack=new Stack<IEnumerator>();stack.Push(steps);while(stack.Count>0){bool moved=false;object value=null;Exception error=null;try{moved=stack.Peek().MoveNext();if(moved)value=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){fatal=error.ToString();CheckResult("native_route_failed",false);Debug.LogError(fatal);break;}if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator next){stack.Push(next);continue;}yield return value;}while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish();}
  public void SetUnexpectedErrors(int count){unexpected=count;errorsFinalized=true;WriteReport();}
  public void Abort(string reason){if(Finished)return;StopAllCoroutines();fatal=reason;CheckResult("interrupted",false);Finish();}
  void Finish(){Cleanup();Finished=true;WriteReport();}
  void Cleanup(){if(cleaned)return;cleaned=true;if(keyboard!=null){InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.RemoveDevice(keyboard);}if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldSettings!=null)InputSystem.settings=oldSettings;if(settings!=null)Destroy(settings);if(clock!=null)Application.runInBackground=oldBackground;}
  void OnDestroy(){if(!Finished&&clock!=null){fatal="Play ended before acceptance completed.";CheckResult("interrupted",false);Finish();}Cleanup();}
  void WriteReport(){if(clock==null)return;Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report{runId=RunId,finished=Finished,complete=Finished&&errorsFinalized&&Failures==0&&checks.Count==5,failures=Failures,seconds=clock.Elapsed.TotalSeconds,fatal=fatal,checks=checks.ToArray(),keys=keys.ToArray(),pages=pages.ToArray(),captures=captures.ToArray(),bounds=bounds,boundary="Explicit staged OilSeep temperature/SteamEffect/ground and long text/action plus carried ShortSword. No natural discovery/acquisition/scald balance claim. Real N, Look/picker/Examine/F1/pages/Compare/return keys. No gameplay action is executed during measured UI route. Graph signature covers current zone owner IDs/positions/HP, inventory/equipped IDs, player clock/energy/HP and source temperature/density; not all world fields."},true));}
  sealed class DemoAction:Part{public int Executed;public override bool HandleEvent(GameEvent e){if(e.ID=="GetInventoryActions")e.GetParameter<InventoryActionList>("Actions")?.AddAction("ReadabilityDemo","ACTION-DETAIL-BEGIN "+new string('w',1600)+" ACTION-DETAIL-END","ReadabilityDemo",'z',999);if(e.ID=="InventoryAction"&&e.GetStringParameter("Command")=="ReadabilityDemo")Executed++;return true;}}
  [Serializable]public sealed class Check{public string name;public bool passed;}
  [Serializable]public sealed class KeyRow{public string keys,before,after;}
  [Serializable]public sealed class Page{public string label,text;public int index,observedIndex,count;}
  [Serializable]public sealed class BoundsRow{public int glyphs,outside,screenWidth,screenHeight;public float cameraWidth,cameraHeight,left,right,top,bottom;}
  [Serializable]sealed class Report{public string runId,fatal,boundary;public bool finished,complete;public int failures;public double seconds;public Check[] checks;public KeyRow[] keys;public Page[] pages;public string[] captures;public BoundsRow bounds;}
 }
}
