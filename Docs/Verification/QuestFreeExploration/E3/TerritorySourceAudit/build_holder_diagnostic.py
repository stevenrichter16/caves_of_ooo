from pathlib import Path
import shutil
p=Path('/tmp/coo-questfree-implementation/actors/collector-placement');s=(p/'OccupiedBankAvailabilityCensus.cs').read_text().replace('OccupiedBankAvailabilityCensus','OccupiedBankPostDiagnostic')
s=s.replace('public object[] Owners;','public object Layout;public object[] Owners;')
s=s.replace('ReceiptCurrent=receipt?.IsCurrent==true;','ReceiptCurrent=receipt?.IsCurrent==true; if(Exploration.Find(id)?.Family==SpreadExplorationFamily.OccupiedBank && receipt?.Owners.Count==1)Layout=Inspect(z,receipt.Owners[0]);')
s=s.replace('owners=m.Owners,holders=','owners=m.Owners,layout=m.Layout,holders=')
s=s.replace('[Test]public void Capture()',r'''static object Inspect(Zone z,Entity actor){
var flags=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;var type=typeof(SpreadExplorationActorPlacement);var post=type.GetMethod("Post",flags);var geom=type.GetNestedType("Geometry",flags);var g=Activator.CreateInstance(geom,flags,null,new object[]{z,actor},null);var origin=z.GetEntityPosition(actor);
var all=z.GetReadOnlyEntities().Where(e=>(bool)post.Invoke(null,new object[]{z,e})).OrderBy(e=>z.GetEntityPosition(e).y).ThenBy(e=>z.GetEntityPosition(e).x).ToArray();
bool Interior(Entity e){var a=z.GetEntityPosition(e);return a.x-3>=2&&a.y-2>=2&&a.x+3<Zone.Width-2&&a.y+2<Zone.Height-2;}
bool Near(Entity e){var a=z.GetEntityPosition(e);return Math.Max(Math.Abs(a.x-origin.x),Math.Abs(a.y-origin.y))<=12;}
object Evaluate(IEnumerable<Entity> sources){int trials=0,bare=0,preserved=0;foreach(var e in sources){var a=z.GetEntityPosition(e);for(int y=a.y-1;y<=a.y+1;y++)for(int x=a.x-1;x<=a.x+1;x++){if(++trials>256)return new{success=false,trials,bare,preserved,post=(object)null,destination=(object)null};if(!(bool)geom.GetMethod("Place",flags).Invoke(g,new object[]{x,y}))continue;bare++;if(!(bool)geom.GetMethod("PreservesRoutes",flags).Invoke(g,new object[]{(x,y)}))continue;preserved++;Func<int,int,bool> avoid=(xx,yy)=>xx>=a.x-3&&xx<=a.x+3&&yy>=a.y-2&&yy<=a.y+2;if((bool)geom.GetMethod("HasBypass",flags).Invoke(g,new object[]{(x,y),avoid}))return new{success=true,trials,bare,preserved,post=(object)new {e.BlueprintName,position=a},destination=(object)(x,y)};}}return new{success=false,trials,bare,preserved,post=(object)null,destination=(object)null};}
return new {origin,allPosts=all.Length,first32=all.Take(32).Select(e=>new{e.BlueprintName,position=z.GetEntityPosition(e),interior=Interior(e),near=Near(e)}).ToArray(),interiorPosts=all.Count(Interior),nearInteriorPosts=all.Count(e=>Interior(e)&&Near(e)),original=Evaluate(all.Take(32).Where(e=>Interior(e)&&Near(e))),filterThenCap=Evaluate(all.Where(e=>Interior(e)&&Near(e)).Take(32)),coldRelocationAndFilter=Evaluate(all.Where(Interior).Take(32))};}
[Test]public void Capture()''')
(p/'OccupiedBankPostDiagnostic.cs').write_text(s)
d=p/'holder-diagnostic-runner';shutil.copytree(p/'census-v4-runner',d,ignore=shutil.ignore_patterns('bin','obj','Patched','*.log'),dirs_exist_ok=True)
f=d/'tests.props';f.write_text(f.read_text().replace('OccupiedBankAvailabilityCensus.cs','OccupiedBankPostDiagnostic.cs'))
