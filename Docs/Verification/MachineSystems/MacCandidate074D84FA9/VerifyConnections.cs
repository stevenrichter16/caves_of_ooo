using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
class VerifyConnections {
 static int count;
 static void Need(bool ok,string label){if(!ok)throw new Exception(label);count++;Console.WriteLine("VERIFIED "+label);}
 static IEnumerable<TypeDefinition> All(TypeDefinition t){yield return t;foreach(var n in t.NestedTypes)foreach(var v in All(n))yield return v;}
 static IEnumerable<MethodDefinition> Methods(TypeDefinition t){return All(t).SelectMany(x=>x.Methods).Where(m=>m.HasBody);}
 static IEnumerable<string> Words(TypeDefinition t){return Methods(t).SelectMany(m=>m.Body.Instructions).Select(i=>i.Operand as string).Where(s=>s!=null);}
 static bool Calls(TypeDefinition t,string declaring,string method){return Methods(t).SelectMany(m=>m.Body.Instructions).Select(i=>i.Operand as MethodReference).Any(m=>m!=null&&m.DeclaringType.FullName==declaring&&m.Name==method);}
 static string[] Fields(TypeDefinition t){return t.Fields.Where(f=>!f.IsStatic).Select(f=>f.Name+":"+f.FieldType.FullName).OrderBy(s=>s).ToArray();}
 static void Main(string[] args){using(var a=AssemblyDefinition.ReadAssembly(args[0]))using(var old=AssemblyDefinition.ReadAssembly(args[1])){
  Func<string,TypeDefinition> type=n=>a.MainModule.Types.Single(t=>t.FullName==n);
  string core="CavesOfOoo.Core.";
  foreach(string n in new[]{"RegionalWeather","RainCatchmentPart","RainCollectorPart","ShowerCollectionSite","VesselTorchRefueling"})Need(type(core+n)!=null&&!old.MainModule.Types.Any(t=>t.FullName==core+n),"new packaged capability "+n);
  Need(Fields(type(core+"RainCatchmentPart")).SequenceEqual(new[]{"ActivatedAt:System.Int32","Ground:CavesOfOoo.Core.Entity","LastWindow:System.Int32","PlotKey:System.String","PolicyVersion:System.Int32","Public:System.Boolean","Spent:System.Int32","WorldKey:System.String"}.OrderBy(s=>s)),"exact catchment saved fields");
  Need(Fields(type(core+"RainCollectorPart")).SequenceEqual(new[]{"Anchor:CavesOfOoo.Core.Entity","Clean:System.Boolean","Ground:CavesOfOoo.Core.Entity"}.OrderBy(s=>s)),"exact collector saved fields");
  foreach(var n in new[]{"LiquidVesselPart","FuelPart","TorchLightPart","MerchantStockPart"})Need(Fields(type(core+n)).SequenceEqual(Fields(old.MainModule.Types.Single(t=>t.FullName==core+n))),"unchanged saved fields "+n);
  var weather=type(core+"RegionalWeather");
  foreach(var pair in new[]{new[]{"PolicyVersion","1"},new[]{"WindowTicks","600"},new[]{"CycleWindows","64"}})Need(weather.Fields.Single(f=>f.Name==pair[0]).Constant.ToString()==pair[1],"weather constant "+pair[0]);
  var vessel=type(core+"LiquidVesselPart");var refill=type(core+"VesselTorchRefueling");
  foreach(var n in new[]{"AddActions","TryAct"})Need(Calls(vessel,refill.FullName,n),"vessel routes "+n);
  var command=type("CavesOfOoo.Core.Inventory.Commands.PerformInventoryActionCommand");
  foreach(var n in new[]{"RainCollectorPart","VesselTorchRefueling"})Need(Calls(command,core+n,"OwnsReceipt"),"exact delta receipt "+n);
  Need(Calls(type(core+"OverworldZoneManager"),core+"ShowerCollectionSite","TryInstall"),"fresh allotment installs collection site");
  Need(refill.Fields.Single(f=>f.Name=="FuelPerOil").Constant.ToString()=="25","same25fuel tariff");
  Need(refill.Fields.Single(f=>f.Name=="Prefix").Constant.ToString()=="RefuelVesselTorch|","quoted refuel command");
  Need(Words(refill).Contains("G5")&&Words(refill).Contains("R"),"readable display and exact quote precision");
  foreach(var n in new[]{"BeforeCommit","TryClaim","AfterCommit"})Need(Calls(refill,core+"Inventory.InventoryTransaction",n),"refill transaction "+n);
  Need(Calls(refill,core+"WaterQuantityUndo","Restore"),"bounded quantity inverse");
  Need(Words(type(core+"ItemExamineService")).Any(w=>w.Contains("oil-filled flask")),"torch reader names vessel fuel");
  Console.WriteLine("CHECKS "+count);
 }}
}
