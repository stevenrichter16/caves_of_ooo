using System;
using System.Linq;
using NUnit.Framework;
using NUnitLite;
using CavesOfOoo.Experiments;
public static class Program { public static int Main(string[] args)=>new AutoRun().Execute(args); }
public sealed class QuestFreeCohortTests
{
 bool[,] Open(){var b=new bool[20,20];for(int y=0;y<20;y++)for(int x=0;x<20;x++)b[x,y]=true;return b;}
 [Test]public void FreezeIsDeterministicConnectedBoundedAndLeavesInputUntouched(){var map=Open();var before=(bool[,])map.Clone();var a=QuestFreeCohort.Freeze(64,11,10,map,20);var b=QuestFreeCohort.Freeze(64,11,10,map,20);Assert.AreEqual(20,a.nodes.Count);CollectionAssert.AreEqual(a.nodes.Select(n=>n.id+":"+n.parent),b.nodes.Select(n=>n.id+":"+n.parent));CollectionAssert.AreEqual(before,map);Assert.AreEqual(20,a.nodes.Select(n=>n.id).Distinct().Count());foreach(var n in a.nodes.Skip(1)){var p=a.nodes.Single(x=>x.id==n.parent);Assert.AreEqual(1,Math.Abs(n.x-p.x)+Math.Abs(n.y-p.y));Assert.Less(a.nodes.IndexOf(p),a.nodes.IndexOf(n));}}
 [Test]public void DisconnectedRichIslandDoesNotEnterCohort(){var b=new bool[20,20];b[1,1]=true;b[1,2]=true;for(int y=8;y<16;y++)for(int x=8;x<16;x++)b[x,y]=true;var a=QuestFreeCohort.Freeze(64,1,1,b,20);Assert.AreEqual(2,a.nodes.Count);Assert.False(a.complete);Assert.That(a.nodes.All(n=>n.x==1));}
 [Test]public void TreeWalkUsesRealCardinalEdgesAndCountsRevisitsSeparately(){var a=QuestFreeCohort.Freeze(1,11,10,Open(),20);Assert.AreEqual(39,a.walk.Count);Assert.AreEqual(a.nodes[0].id,a.walk[0]);Assert.AreEqual(a.walk[0],a.walk.Last());for(int i=1;i<a.walk.Count;i++){var x=a.nodes.Single(n=>n.id==a.walk[i-1]);var y=a.nodes.Single(n=>n.id==a.walk[i]);Assert.AreEqual(1,Math.Abs(x.x-y.x)+Math.Abs(x.y-y.y));}}
 [Test]public void SeedBreaksTiesWithoutChangingBreadthFirstDepth(){var a=QuestFreeCohort.Freeze(1,11,10,Open(),20);var b=QuestFreeCohort.Freeze(1729,11,10,Open(),20);Assert.AreNotEqual(string.Join(",",a.nodes.Select(n=>n.id)),string.Join(",",b.nodes.Select(n=>n.id)));CollectionAssert.IsOrdered(a.nodes.Select(n=>n.depth).ToArray());}
 [TestCase(0)][TestCase(401)]public void InvalidBudgetRefuses(int n){Assert.Throws<ArgumentOutOfRangeException>(()=>QuestFreeCohort.Freeze(1,11,10,Open(),n));}
 [Test]public void ForeignStartRefusesInsteadOfMovingIt(){var b=Open();b[11,10]=false;Assert.Throws<ArgumentException>(()=>QuestFreeCohort.Freeze(1,11,10,b,20));}
 [TestCase("cargo","refused:no-existing-geometry",true,"refused")]
 [TestCase("shelter","",true,"selected-not-committed")]
 [TestCase("cargo","cargo",true,"committed")]
 [TestCase("cargo","cargo",false,"generation-refused")]
 [TestCase("","",true,"not-selected")]
 [TestCase("gleanings","",true,"composition-only")]
 [TestCase("shelter","cargo",true,"selected-not-committed")]
 public void SelectedIsNotCommitted(string selected,string result,bool accepted,string expected){Assert.AreEqual(expected,QuestFreeCohort.Disposition(selected,result,accepted));}
}
