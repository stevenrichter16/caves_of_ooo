using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using System.Text;
using System.Globalization;
namespace CavesOfOoo.Tests
{
 public sealed class DensityUndergroundLayoutSnapshotTests
 {
  [Test]public void CaptureFiveSeedTerrainAndSourceBaseline()
  {
   using(var scope=new DensityLootTestScope())
   {
    var rows=new List<string>();
    foreach(int seed in new[]{1,64,1729,2026,729490642})foreach(int depth in new[]{1,3,7,11})
    {
     scope.Seed(seed);var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed);var xy=FindColumn(manager);string id=WorldMap.ToZoneID(xy.x,xy.y,depth);var zone=manager.GetZone(id);Assert.NotNull(zone);
     var counts=zone.GetReadOnlyEntities().GroupBy(e=>e.BlueprintName).OrderBy(g=>g.Key).ToDictionary(g=>g.Key,g=>g.Count());
     string terrain=string.Join("|",zone.GetReadOnlyEntities().Where(e=>e.HasTag("Terrain")||e.HasTag("Wall")).Select(e=>{var p=zone.GetEntityCell(e);return e.BlueprintName+":"+p.X+","+p.Y;}).OrderBy(x=>x));
     uint hash=2166136261;unchecked{foreach(char c in terrain)hash=(hash^c)*16777619;}
     rows.Add("{\"seed\":"+seed.ToString(CultureInfo.InvariantCulture)+",\"zone\":"+Quote(id)+",\"terrainHash\":"+Quote(hash.ToString("x8"))+",\"owners\":"+zone.EntityCount.ToString(CultureInfo.InvariantCulture)+",\"creatures\":"+zone.GetEntitiesWithTag("Creature").Count.ToString(CultureInfo.InvariantCulture)+",\"containers\":"+zone.GetReadOnlyEntities().Count(e=>e.HasPart<ContainerPart>()).ToString(CultureInfo.InvariantCulture)+",\"blueprints\":{"+string.Join(",",counts.Select(c=>Quote(c.Key)+":"+c.Value.ToString(CultureInfo.InvariantCulture)))+"}}");
    }
    string path=Environment.GetEnvironmentVariable("COO_UNDERGROUND_SNAPSHOT_OUTPUT");if(!string.IsNullOrEmpty(path))File.WriteAllText(path,"{\"runtime\":\"See execution receipt; standalone runner uses deterministic hash adapter\",\"scope\":\"20 structural snapshots; not opened-loot economy\",\"rows\":["+string.Join(",",rows)+"]}");
   }
  }
  private static string Quote(string value)
  {
   var s=new StringBuilder("\"");
   foreach(char c in value??"")
   {
    if(c=='"'||c=='\\')s.Append('\\').Append(c);
    else if(c<32)s.Append("\\u").Append(((int)c).ToString("x4"));
    else s.Append(c);
   }
   return s.Append('"').ToString();
  }
  public static (int x,int y) FindColumn(OverworldZoneManager manager){for(int x=0;x<WorldMap.Width;x++)for(int y=0;y<WorldMap.Height;y++)if(manager.WorldMap.GetBiome(x,y)==BiomeType.Spread&&manager.WorldMap.GetPOI(x,y)==null&&WorldMap.ToZoneID(x,y,0)!=ReferenceGladePlan.ZoneID)return(x,y);throw new Exception("missing ordinary Spread column");}
 }
}
