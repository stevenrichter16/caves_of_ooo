using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests {
 public class GrazerRuntimeDiagnosticTests {
 sealed class Probe:IZoneBuilder{readonly Action<Zone> a;public Probe(Action<Zone> action){a=action;}public string Name=>"GrazerReceiptProbe";public int Priority=>4299;public bool BuildZone(Zone z,EntityFactory f,Random r){a(z);return true;}}
 sealed class Manager:OverworldZoneManager {
 public SpreadExplorationBuilder Composer;public SpreadCompositionBuilder Terrain;
 public Manager(EntityFactory f):base(f,64){}
 protected override ZoneGenerationPipeline GetPipelineForZone(string id){var p=base.GetPipelineForZone(id);Composer=p.Builders.OfType<SpreadExplorationBuilder>().SingleOrDefault();Terrain=p.Builders.OfType<SpreadCompositionBuilder>().SingleOrDefault();var pop=p.Builders.OfType<PopulationBuilder>().SingleOrDefault();p.AddBuilder(new Probe(z=>Describe(pop?.AmbientSourceReceipt)));return p;}
 }
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static object F(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
 static void Describe(SpreadGenerationReceipt r){if(r==null||r.IsCurrent)return;TestContext.WriteLine("RECEIPT "+r.Zone.ZoneID+" complete="+F(r,"complete")+" owners="+string.Join(",",r.Owners.Select(e=>e.BlueprintName)));foreach(var snap in (IEnumerable)F(r,"snapshots")){var owner=(Entity)F(snap,"owner");bool matches=(bool)snap.GetType().GetMethod("Matches",Flags).Invoke(snap,new object[]{true});if(matches)continue;TestContext.WriteLine(" OWNER "+owner.BlueprintName+" valid="+F(snap,"valid"));foreach(var entity in (IEnumerable)F(snap,"graph")){bool ok=(bool)entity.GetType().GetMethod("Matches",Flags).Invoke(entity,null);if(ok)continue;var actual=(Entity)F(entity,"entity");TestContext.WriteLine(" ENTITY "+actual.BlueprintName+" id="+actual.ID+" partsold="+string.Join(",",((Part[])F(entity,"parts")).Select(v=>v.Name))+" partsnew="+string.Join(",",actual.Parts.Select(v=>v.Name))+" propsold="+string.Join(",",((System.Collections.Generic.KeyValuePair<string,string>[])F(entity,"properties")).Select(v=>v.Key+"="+v.Value))+" propsnew="+string.Join(",",actual.Properties.Select(v=>v.Key+"="+v.Value))+" goalsold="+string.Join(",",((GoalHandler[])F(entity,"goals")??Array.Empty<GoalHandler>()).Select(v=>v.GetType().Name))+" goalsnew="+string.Join(",",actual.GetPart<BrainPart>()?.GetGoalsSnapshot().Select(v=>v.GetType().Name)??Enumerable.Empty<string>()));TestContext.WriteLine(" CLAUSES parents="+actual.Parts.All(x=>x.ParentEntity==actual)+" parts="+actual.Parts.SequenceEqual((Part[])F(entity,"parts"))+" body="+(actual.GetPart<Body>()?.GetParts().SequenceEqual((CavesOfOoo.Core.Anatomy.BodyPart[])F(entity,"slots")))+" oldenemies="+string.Join(",",((Entity[])F(entity,"enemies")??Array.Empty<Entity>()).Select(x=>x.ID))+" nowenemies="+string.Join(",",actual.GetPart<BrainPart>()?.PersonalEnemies.Select(x=>x.ID)??Enumerable.Empty<string>())+" bodyfieldbad="+string.Join(",",((IEnumerable)F(entity,"slotValues")).Cast<object>().Where(x=>!(bool)x.GetType().GetMethod("Matches",Flags).Invoke(x,null)).Select(x=>F(x,"owner").ToString())));
 foreach(var slot in ((IEnumerable)F(entity,"slotValues")).Cast<object>().Where(x=>!(bool)x.GetType().GetMethod("Matches",Flags).Invoke(x,null))){foreach(var pair in (IEnumerable)F(slot,"fields")){var info=(FieldInfo)F(pair,"Item1");var value=F(pair,"Item2");var now=info.GetValue(F(slot,"owner"));if(!Equals(value,now))TestContext.WriteLine(" SLOT "+info.Name+" old="+value+" new="+now);}}
 foreach(var field in (IEnumerable)F(entity,"values")){bool fm=(bool)field.GetType().GetMethod("Matches",Flags).Invoke(field,null);if(fm)continue;var instance=F(field,"owner");foreach(var pair in (IEnumerable)F(field,"fields")){var info=(FieldInfo)F(pair,"Item1");var value=F(pair,"Item2");var now=info.GetValue(instance);if(!Equals(value,now))TestContext.WriteLine(" FIELD "+instance.GetType().Name+"."+info.Name+" was="+value+" now="+now);}}}}}
 [Test]public void ActualSelectedFieldSourcesAndRefusalStages(){using(var s=new DensityLootTestScope()){
 var m=new Manager(s.Factory);int pass=0;
 foreach(var entry in m.Exploration.Entries.Where(e=>e.Family==SpreadExplorationFamily.LastGleanings)){
 s.Seed(unchecked(64^FormationSelector.StableIndex(entry.ZoneID,int.MaxValue)));
 var z=m.GetZone(entry.ZoneID);Assert.NotNull(z);var roles=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="ReedbackGrazer").ToArray();var rows=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="RipeCropRow").ToArray();
 TestContext.WriteLine(entry.ZoneID+" condition="+m.Terrain.Plan.Condition+" result="+m.Composer.LastResult+" rows="+string.Join(";",rows.Select(z.GetEntityPosition))+" grazers="+string.Join(";",roles.Select(e=>z.GetEntityPosition(e)+":"+e.GetPart<SpreadGrazerPart>().Configured)));
 if(m.Exploration.DispositionFor(entry.ZoneID)==2)pass++;
 }
 Assert.Greater(pass,0);
 }}
 }
}
