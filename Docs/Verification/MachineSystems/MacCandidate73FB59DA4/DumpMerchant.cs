using System;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
class DumpMerchant {
 static IEnumerable<TypeDefinition> Types(TypeDefinition t) {yield return t;foreach(var n in t.NestedTypes)foreach(var c in Types(n))yield return c;}
 static void Main(string[] args) {
  using(var a=AssemblyDefinition.ReadAssembly(args[0])) using(var p=AssemblyDefinition.ReadAssembly(args[1])) {
   var types=a.MainModule.Types.SelectMany(Types).ToArray();
   var stock=types.Single(t=>t.FullName=="CavesOfOoo.Core.MerchantStockService");
   if(p.MainModule.Types.Any(t=>t.FullName==stock.FullName))throw new Exception("Previous package unexpectedly has merchant pilot");
   foreach(var check in new[]{new[]{"CavesOfOoo.Core.TraderRestockSystem","RestockZone","MerchantStockService::Reconcile"},new[]{"CavesOfOoo.Rendering.TradeUI","Open","MerchantStockService::Reconcile"},new[]{"CavesOfOoo.Core.InventoryPart","AddObjectCore","MerchantStockService::ProtectsMerge"},new[]{"CavesOfOoo.Core.TradeSystem/BuyTransferCommand","Execute","MerchantStockService::ReleaseForTransfer"},new[]{"CavesOfOoo.Core.Barter/DealCommand","Move","MerchantStockService::ReleaseForTransfer"}}) {
    var method=types.Single(t=>t.FullName==check[0]).Methods.Single(m=>m.Name==check[1]);
    if(!method.Body.Instructions.Any(i=>i.Operand!=null&&i.Operand.ToString().Contains(check[2])))throw new Exception("Missing packaged call "+check[2]);
    Console.WriteLine("VERIFIED "+method.FullName+" -> "+check[2]);
   }
   var state=types.Single(t=>t.FullName=="CavesOfOoo.Core.MerchantStockPart");foreach(var f in state.Fields)Console.WriteLine("SAVED "+f.FullName);
   foreach(var method in stock.Methods.Where(m=>m.Name==".cctor"||m.Name=="Reconcile"||m.Name=="ProjectBenefit")) {
    Console.WriteLine(method.FullName);
    foreach(var i in method.Body.Instructions)if(i.Operand is string||i.OpCode.Code==Mono.Cecil.Cil.Code.Ldc_I4||i.Operand is MethodReference)Console.WriteLine(i);
   }
  }
 }
}
