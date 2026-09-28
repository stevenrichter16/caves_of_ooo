using System;
using System.Collections.Generic;
using System.Linq;
namespace CavesOfOoo.Experiments
{
 // Audit-only topology helper: no gameplay types, zone factory, RNG, actors or loot.
 public static class QuestFreeCohort
 {
  public const string Version="QuestFreeExploration.BFS20.v1";
  [Serializable]public sealed class Node{public string id,parent;public int x,y,depth;}
  [Serializable]public sealed class Plan{public string protocol=Version;public int seed,requested;public bool complete;public List<Node> nodes=new List<Node>();public List<string> walk=new List<string>();}
  static string ID(int x,int y)=>"Overworld."+x+"."+y+".0";
  static uint Rank(int seed,string id){unchecked{uint h=2166136261^(uint)seed;foreach(char c in Version+"|"+id)h=(h^c)*16777619;h^=h>>16;h*=0x7feb352d;h^=h>>15;h*=0x846ca68b;return h^(h>>16);}}
  public static Plan Freeze(int seed,int startX,int startY,bool[,] spread,int limit)
  {
   if(limit<1||limit>400)throw new ArgumentOutOfRangeException(nameof(limit));
   if(spread==null||spread.GetLength(0)!=20||spread.GetLength(1)!=20||startX<0||startX>=20||startY<0||startY>=20||!spread[startX,startY])throw new ArgumentException("Actual metadata start must be a Spread surface cell in the20x20 map.");
   var p=new Plan{seed=seed,requested=limit};var seen=new HashSet<string>(StringComparer.Ordinal);var first=new Node{id=ID(startX,startY),x=startX,y=startY,depth=0,parent=""};p.nodes.Add(first);seen.Add(first.id);var frontier=new List<Node>{first};
   int[] dx={-1,1,0,0},dy={0,0,-1,1};
   while(p.nodes.Count<limit&&frontier.Count>0)
   {
    var candidates=new Dictionary<string,Node>(StringComparer.Ordinal);
    foreach(var parent in frontier)for(int d=0;d<4;d++)
    {
     int x=parent.x+dx[d],y=parent.y+dy[d];if(x<0||x>=20||y<0||y>=20||!spread[x,y])continue;string id=ID(x,y);if(seen.Contains(id))continue;
     if(!candidates.TryGetValue(id,out var prior)||string.CompareOrdinal(parent.id,prior.parent)<0)candidates[id]=new Node{id=id,x=x,y=y,parent=parent.id,depth=parent.depth+1};
    }
    frontier=candidates.Values.OrderBy(n=>Rank(seed,n.id)).ThenBy(n=>n.id,StringComparer.Ordinal).Take(limit-p.nodes.Count).ToList();
    foreach(var n in frontier){seen.Add(n.id);p.nodes.Add(n);}
   }
   p.complete=p.nodes.Count==limit;Walk(first.id,p);return p;
  }
  static void Walk(string id,Plan p){p.walk.Add(id);foreach(var child in p.nodes.Where(n=>n.parent==id)){Walk(child.id,p);p.walk.Add(id);}}
  public static string Disposition(string selected,string result,bool accepted)
  {if(!accepted)return "generation-refused";if(string.IsNullOrEmpty(selected))return "not-selected";if(selected=="gleanings")return "composition-only";if(result==selected)return "committed";return result!=null&&result.StartsWith("refused:",StringComparison.Ordinal)?"refused":"selected-not-committed";}
 }
}
