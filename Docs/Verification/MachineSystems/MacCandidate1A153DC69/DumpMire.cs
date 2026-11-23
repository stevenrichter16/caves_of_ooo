using System;
using System.Linq;
using Mono.Cecil;
class DumpMire {
 static string Body(MethodDefinition m) { return string.Join("\n",m.Body.Instructions.Select(i=>i.ToString()).ToArray()); }
 static string[] Words(MethodDefinition m) { return m.Body.Instructions.Select(i=>i.Operand as string).Where(x=>x!=null).ToArray(); }
 static void Need(bool ok,string message) {if(!ok)throw new Exception(message);Console.WriteLine("VERIFIED "+message);}
 static void Main(string[] args) {
  using(var a=AssemblyDefinition.ReadAssembly(args[0]))using(var p=AssemblyDefinition.ReadAssembly(args[1])) {
   var types=a.MainModule.Types;var old=p.MainModule.Types;
   var body=types.Single(t=>t.FullName=="CavesOfOoo.Core.BodyCoatingCleanupActions");var before=old.Single(t=>t.FullName==body.FullName);
   var recipient=body.Methods.Single(m=>m.Name=="Recipient");
   Need(Words(recipient).Contains("bog-mire")&&!Words(before.Methods.Single(m=>m.Name=="Recipient")).Contains("bog-mire"),"new body mire admission");
   Need(body.Fields.Single(f=>f.Name=="MaximumAmount").Constant.Equals(20),"capacity remains20");
   foreach(var n in new[]{"TryAct","AddActions","Source","Reject"})Need(Body(body.Methods.Single(m=>m.Name==n))==Body(before.Methods.Single(m=>m.Name==n)),"unchanged body contract "+n);
   foreach(var check in new[]{new[]{"CavesOfOoo.Core.MaterialUseDescription","TryDescribe"},new[]{"CavesOfOoo.Core.ItemTacticalUseDescription","Describe"}}) {
    var m=types.Single(t=>t.FullName==check[0]).Methods.Single(x=>x.Name==check[1]);Need(Words(m).Any(x=>x.Contains("bog mire")),"public mire guide "+check[0]);Console.WriteLine(Body(m));
   }
   foreach(var n in new[]{"CavesOfOoo.Core.MerchantStockPart","CavesOfOoo.Core.LiquidCoveredEffect"}) {
    var t=types.Single(x=>x.FullName==n);var o=old.Single(x=>x.FullName==n);
    Need(t.Fields.Select(f=>f.FullName).SequenceEqual(o.Fields.Select(f=>f.FullName)),"unchanged fields "+n);
   }
   var stock=types.Single(t=>t.FullName=="CavesOfOoo.Core.MerchantStockService");var oldStock=old.Single(t=>t.FullName==stock.FullName);
   foreach(var n in new[]{"Reconcile","ProfileFor","ProjectBonus","ValidFreshPayload",".cctor"})Need(Body(stock.Methods.Single(m=>m.Name==n))==Body(oldStock.Methods.Single(m=>m.Name==n)),"unchanged merchant contract "+n);
   Console.WriteLine(Body(recipient));
  }
 }
}
