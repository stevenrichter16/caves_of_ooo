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
   var previous = p.MainModule.Types.SelectMany(Types).ToArray();
   var oldStock=previous.Single(t=>t.FullName==stock.FullName);
   if(oldStock.Fields.Any(f=>f.Name=="Scribe"||f.Name=="Provisioner"))throw new Exception("Previous package already has expansion");
   if(!stock.Fields.Any(f=>f.Name=="Scribe")||!stock.Fields.Any(f=>f.Name=="Provisioner"))throw new Exception("Missing new profiles");
   foreach(var check in new[]{new[]{"CavesOfOoo.Core.MerchantStockService","ProjectBonus","SettlementManager::HasExistingCondition"},new[]{"CavesOfOoo.Core.MerchantStockService","ProfileFor","PlaceSiteCatalog::PlaceAt"}}) {
    var method=types.Single(t=>t.FullName==check[0]).Methods.Single(m=>m.Name==check[1]);
    if(!method.Body.Instructions.Any(i=>i.Operand!=null&&i.Operand.ToString().Contains(check[2])))throw new Exception("Missing expansion call "+check[2]);
    Console.WriteLine("VERIFIED "+method.FullName+" -> "+check[2]);
   }
   var pure=types.Single(t=>t.FullName=="CavesOfOoo.Core.SettlementManager").Methods.Single(m=>m.Name=="HasExistingCondition");
   if(pure.Body.Instructions.Any(i=>i.Operand!=null&&i.Operand.ToString().Contains("GetOrCreateSettlement")))throw new Exception("Condition reader backfills");
   if(previous.Single(t=>t.FullName=="CavesOfOoo.Core.SettlementManager").Methods.Any(m=>m.Name=="HasExistingCondition"))throw new Exception("Previous already has condition reader");
   var constants=stock.Methods.Single(m=>m.Name==".cctor").Body.Instructions.Select(i=>i.Operand as string).Where(v=>v!=null).ToArray();
   foreach(var value in new[]{"ScribeStock","ProvisionerStock","LampOil","DryingBreezeGrimoire","Quillhold.DesksLit","CampHearth","PoisonGasGrenade","Codex04"})
    if(!constants.Contains(value))throw new Exception("Missing catalogue value "+value);
   Console.WriteLine("VERIFIED new profile constants and pure saved-condition call");
   var grouping=types.Single(t=>t.FullName=="CavesOfOoo.Core.InventoryItemGroups");
   var audited=grouping.Methods.Single(m=>m.Name==".cctor").Body.Instructions.Select(i=>i.Operand as TypeReference).Where(v=>v!=null).Select(v=>v.Name).ToArray();
   foreach(var name in new[]{"ReagentPart","LightSourceFlickerPart","CookablePart","CampfirePart","HearthGlowPart","HearthRestPart","PackableFixturePart","GasGrenadePart"})
    if(!audited.Contains(name))throw new Exception("Missing payload audit "+name);
   Console.WriteLine("VERIFIED eight exact merchant payload audit types");
   foreach(var check in new[]{new[]{"CavesOfOoo.Core.TraderRestockSystem","RestockZone","MerchantStockService::Reconcile"},new[]{"CavesOfOoo.Rendering.TradeUI","Open","MerchantStockService::Reconcile"},new[]{"CavesOfOoo.Core.InventoryPart","AddObjectCore","MerchantStockService::ProtectsMerge"},new[]{"CavesOfOoo.Core.TradeSystem/BuyTransferCommand","Execute","MerchantStockService::ReleaseForTransfer"},new[]{"CavesOfOoo.Core.Barter/DealCommand","Move","MerchantStockService::ReleaseForTransfer"}}) {
    var method=types.Single(t=>t.FullName==check[0]).Methods.Single(m=>m.Name==check[1]);
    if(!method.Body.Instructions.Any(i=>i.Operand!=null&&i.Operand.ToString().Contains(check[2])))throw new Exception("Missing packaged call "+check[2]);
    Console.WriteLine("VERIFIED "+method.FullName+" -> "+check[2]);
   }
   var state=types.Single(t=>t.FullName=="CavesOfOoo.Core.MerchantStockPart");if(state.Fields.Count!=8)throw new Exception("Saved schema changed");foreach(var f in state.Fields)Console.WriteLine("SAVED "+f.FullName);
   foreach(var method in stock.Methods.Where(m=>m.Name==".cctor"||m.Name=="Reconcile"||m.Name=="ProjectBenefit"||m.Name=="ValidFreshPayload"||m.Name=="ProjectBonus")) {
    Console.WriteLine(method.FullName);
    foreach(var i in method.Body.Instructions)if(i.Operand is string||i.OpCode.Code==Mono.Cecil.Cil.Code.Ldc_I4||i.Operand is MethodReference)Console.WriteLine(i);
   }
  }
 }
}
