using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadHuntSaveTests
 {
  [TestCase("untouched")][TestCase("searching")][TestCase("escaped")][TestCase("prey-absent")][TestCase("hunter-absent")]
  public void ActualGeneratedPairStateOrAbsenceSurvivesCacheReturnAndFullGraphReload(string stateName)
  {
   var oldActive=SettlementRuntime.ActiveZone;
   try{using(var scope=new HaulingContentScope())
   {
    string content=Environment.GetEnvironmentVariable("COO_HUNT_OBJECTS");if(!string.IsNullOrEmpty(content))scope.Factory.LoadBlueprints(File.ReadAllText(content));Assert.True(scope.Factory.Blueprints.ContainsKey("Furrowstalker"));
    var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var ids=manager.Exploration.Entries.Where(e=>e.Family.ToString()=="HuntThroughCover").Select(e=>e.ZoneID).ToArray();Assert.IsNotEmpty(ids);Zone z=null;Entity hunter=null;
    foreach(string id in ids){scope.Seed(unchecked(64^FormationSelector.StableIndex(id,int.MaxValue)));var candidate=manager.GetZone(id);if(candidate!=null&&manager.Exploration.DispositionFor(id)==2){z=candidate;hunter=z.GetReadOnlyEntities().Single(e=>e.Parts.Any(p=>p.Name=="SpreadPredator"));break;}}
    Assert.NotNull(z,"Declared corpus may honestly fail on unavailable sources; never inject a pair.");var role=hunter.Parts.Single(p=>p.Name=="SpreadPredator");var type=role.GetType();var prey=(Entity)type.GetField("Prey").GetValue(role);var grazer=prey.Parts.Single(p=>p.Name=="SpreadGrazer");var preyField=type.GetField("Prey");
    if(stateName=="searching"){type.GetField("Phase").SetValue(role,Enum.Parse(type.GetField("Phase").FieldType,"Searching"));type.GetField("HasLastSeen").SetValue(role,true);var at=z.GetEntityPosition(prey);type.GetField("LastSeenX").SetValue(role,at.x);type.GetField("LastSeenY").SetValue(role,at.y);type.GetField("PursuitRemaining").SetValue(role,11);type.GetField("SearchRemaining").SetValue(role,3);}
    if(stateName=="escaped"){type.GetField("Phase").SetValue(role,Enum.Parse(type.GetField("Phase").FieldType,"Escaped"));preyField.SetValue(role,null);grazer.GetType().GetField("Hunter").SetValue(grazer,null);}
    if(stateName=="prey-absent")Assert.True(z.RemoveEntity(prey));if(stateName=="hunter-absent")Assert.True(z.RemoveEntity(hunter));
    var player=scope.Factory.CreateEntity("Player");Cell cell=null;z.ForEachCell((c,x,y)=>{if(cell==null&&!c.BlocksMovement()&&z.CanPlaceFootprint(player,x,y))cell=c;});Assert.NotNull(cell);Assert.True(z.AddEntity(player,cell.X,cell.Y));manager.SetActiveZone(z);SettlementRuntime.ActiveZone=z;
    var expected=z.GetReadOnlyEntities().ToDictionary(e=>e.ID,e=>z.GetEntityPosition(e));string zoneID=z.ZoneID,hunterID=hunter.ID,preyID=prey.ID;
    var away=manager.GetZone(ReferenceGladePlan.ZoneID);Cell dest=null;away.ForEachCell((c,x,y)=>{if(dest==null&&!c.BlocksMovement()&&away.CanPlaceFootprint(player,x,y))dest=c;});Assert.NotNull(dest);Assert.True(z.RemoveEntity(player));expected.Remove(player.ID);Assert.True(away.AddEntity(player,dest.X,dest.Y));manager.SetActiveZone(away);SettlementRuntime.ActiveZone=away;
    manager.UnloadZone(zoneID);Assert.AreSame(z,manager.GetZone(zoneID));var state=GameSessionState.Capture("hunt",stateName,manager,null,player);GameSessionState restored;
    using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;restored=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
    var loaded=(OverworldZoneManager)restored.ZoneManager;Assert.AreEqual(manager.Exploration.Version,loaded.Exploration.Version);Assert.AreEqual(2,loaded.Exploration.DispositionFor(zoneID));var returned=loaded.GetZone(zoneID);Assert.AreNotSame(z,returned);Assert.AreEqual(expected.Count,returned.EntityCount);
    foreach(var e in returned.GetReadOnlyEntities()){Assert.True(expected.ContainsKey(e.ID));Assert.AreEqual(expected[e.ID],returned.GetEntityPosition(e));}
    var nextHunter=returned.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==hunterID);var nextPrey=returned.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==preyID);Assert.AreEqual(stateName!="hunter-absent",nextHunter!=null);Assert.AreEqual(stateName!="prey-absent",nextPrey!=null);
    if(nextHunter!=null){Assert.AreNotSame(hunter,nextHunter);var next=nextHunter.Parts.Single(p=>p.Name=="SpreadPredator");Assert.AreEqual(type.GetField("Phase").GetValue(role),type.GetField("Phase").GetValue(next));Assert.AreEqual(type.GetField("PursuitRemaining").GetValue(role),type.GetField("PursuitRemaining").GetValue(next));Assert.AreEqual(type.GetField("SearchRemaining").GetValue(role),type.GetField("SearchRemaining").GetValue(next));if(stateName=="escaped")Assert.IsNull(preyField.GetValue(next));else if(nextPrey!=null)Assert.AreSame(nextPrey,preyField.GetValue(next));}
    if(nextHunter!=null&&nextPrey!=null&&stateName!="escaped"){var nextGrazer=nextPrey.Parts.Single(p=>p.Name=="SpreadGrazer");Assert.AreSame(nextHunter,nextGrazer.GetType().GetField("Hunter").GetValue(nextGrazer));}
    loaded.UnloadZone(zoneID);Assert.AreSame(returned,loaded.GetZone(zoneID));Assert.AreEqual(expected.Count,returned.EntityCount);
   }}finally{SettlementRuntime.ActiveZone=oldActive;}
  }
 }
}
