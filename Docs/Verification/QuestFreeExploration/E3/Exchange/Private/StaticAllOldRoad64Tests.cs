using System;using System.Linq;using System.IO;using System.Reflection;using System.Collections.Generic;using CavesOfOoo.Core;using CavesOfOoo.Data;using NUnit.Framework;
namespace CavesOfOoo.Tests { public class ExchangeAllOldRoad64Tests {
[Test] public void FrozenSourceAvailability() { var rows=new List<object>();foreach(int seed in Enumerable.Range(1,64)) {
var m=OverworldZoneManager.CreateDetached(new EntityFactory(),seed,true);
var assigned=m.Exploration.Entries.Where(e=>e.PlacementEligible && FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.OldRoad).ToArray();
var entries=new List<object>();foreach(var e in assigned) {var at=WorldMap.FromZoneID(e.ZoneID);object[] args={m,at.x,at.y,null,null};bool route=(bool)typeof(WorldTravellers).GetMethod("FindRoute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
uint sample=WorldRemarks.Hash(seed.ToString(System.Globalization.CultureInfo.InvariantCulture)+":traveller:"+e.ZoneID);
entries.Add(new {zone=e.ZoneID,family=e.Family.ToString(),tier=WorldMapAuthoring.TierAt(at.x,at.y),roll=sample%8,route,origin=args[3],destination=args[4],eligible=sample%8==0 && WorldMapAuthoring.TierAt(at.x,at.y)<=3 && m.WorldMap.GetPOI(at.x,at.y)==null && route});}
Assert.Zero(m.CachedZoneCount);rows.Add(new {seed,assigned=assigned.Length,entries});}
File.WriteAllText("/tmp/coo-questfree-implementation/exchange/static-all-oldroad-64seeds.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
}}
}