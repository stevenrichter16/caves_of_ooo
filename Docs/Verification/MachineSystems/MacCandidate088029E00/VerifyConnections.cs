using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
class VerifyConnections {
 static int count;
 static void Need(bool ok,string label){if(!ok)throw new Exception(label);count++;Console.WriteLine("VERIFIED "+label);}
 static IEnumerable<TypeDefinition> All(TypeDefinition t){yield return t;foreach(var n in t.NestedTypes)foreach(var v in All(n))yield return v;}
 static IEnumerable<TypeDefinition> Types(AssemblyDefinition a){return a.MainModule.Types.SelectMany(All);}
 static IEnumerable<MethodDefinition> Methods(TypeDefinition t){return All(t).SelectMany(x=>x.Methods).Where(m=>m.HasBody);}
 static IEnumerable<MethodReference> References(TypeDefinition t){return Methods(t).SelectMany(m=>m.Body.Instructions).Select(i=>i.Operand as MethodReference).Where(m=>m!=null);}
 static IEnumerable<string> Words(TypeDefinition t){return Methods(t).SelectMany(m=>m.Body.Instructions).Select(i=>i.Operand as string).Where(s=>s!=null);}
 static bool Calls(TypeDefinition t,string declaring,string method){return References(t).Any(m=>m.DeclaringType.FullName==declaring&&m.Name==method);}
 static string[] Fields(TypeDefinition t){return t.Fields.Where(f=>!f.IsStatic).Select(f=>f.Name+":"+f.FieldType.FullName).OrderBy(s=>s).ToArray();}
 static bool Part(TypeDefinition t,AssemblyDefinition a){while(t!=null&&t.BaseType!=null){if(t.BaseType.FullName=="CavesOfOoo.Core.Part")return true;var n=t.BaseType.FullName;t=Types(a).FirstOrDefault(x=>x.FullName==n);}return false;}
 static void Main(string[] args){using(var a=AssemblyDefinition.ReadAssembly(args[0]))using(var old=AssemblyDefinition.ReadAssembly(args[1])){
  Func<string,TypeDefinition> type=n=>Types(a).Single(t=>t.FullName==n);
  string core="CavesOfOoo.Core.";
  var k=type(core+"PortableKindling");var knife=type(core+"CuttingToolPart");
  Need(!Types(old).Any(t=>t.FullName==k.FullName)&&!Part(k,a),"new ignition adapter is not a saved Part");
  var np=Types(a).Where(t=>Part(t,a)).ToDictionary(t=>t.FullName);var op=Types(old).Where(t=>Part(t,old)).ToDictionary(t=>t.FullName);
  Need(np.Keys.OrderBy(x=>x).SequenceEqual(op.Keys.OrderBy(x=>x)),"all runtime Part type names unchanged, count="+np.Count);
  Need(np.All(x=>Fields(x.Value).SequenceEqual(Fields(op[x.Key]))),"all existing Part instance-field shapes unchanged");
  Need(knife.Interfaces.Any(i=>i.InterfaceType.FullName==core+"IDescribeUse"),"knife participates in shared use description");
  Need(Words(knife).Contains(" -> ")&&!Words(knife).Any(w=>w.Contains("→")),"knife examples use supported ASCII arrows");
  foreach(var n in new[]{"Validate","ForWorkKind"})Need(Calls(knife,core+"MaterialProcessRegistry",n),"knife reads live catalogue "+n);
  Need(Calls(knife,core+"MaterialCuttingWorkReceipt","Usable"),"knife uses exact tool predicate");
  Need(!References(knife).Any(m=>m.DeclaringType.FullName=="CavesOfOoo.Data.EntityFactory"&&m.Name=="CreateEntity"),"knife reader creates no example entities");
  Need(Calls(type(core+"TorchLightPart"),k.FullName,"AddActions"),"torch target event offers kindling");
  Need(Calls(type(core+"CampfirePart"),k.FullName,"AddActions"),"hearth target event offers kindling");
  var utility=type(core+"CombatUtilityActions");
  foreach(var n in new[]{"IsCommand","TryAct"})Need(Calls(utility,k.FullName,n),"native adapter routes kindling "+n);
  Need(!Calls(utility,k.FullName,"AddActions"),"no duplicate global kindling row producer");
  Need(Calls(type(core+"Inventory.Commands.PerformInventoryActionCommand"),k.FullName,"OwnsReceipt"),"kindling owns narrow inverse instead of actor snapshot");
  foreach(var n in new[]{"BeforeCommit","TryClaim","AfterCommit"})Need(Calls(k,core+"Inventory.InventoryTransaction",n),"kindling transaction "+n);
  foreach(var n in new[]{"Capture","Apply","ClaimChanges","Restore"})Need(Calls(k,core+"Inventory.InventoryTransferSnapshot",n),"immediate payment receipt "+n);
  Need(Calls(k,core+"InventoryPart","TryConsumeOne"),"one carried unit pays ignition");
  Need(Calls(k,core+"IgnitionSources","Find"),"normal flame and flint checked first");
  Need(Words(k).Contains("KindleFuel|"),"explicit quoted ignition command");
  Need(Words(k).Contains("KindlingRefundIdentityChanged"),"unsupported refund identity diagnosed");
  Need(References(k).Any(m=>m.Name=="Contains"&&m.DeclaringType.FullName.StartsWith("System.Collections.Generic.List")),"exact physical membership is checked");
  Need(!References(k).Any(m=>m.DeclaringType.FullName=="CavesOfOoo.Data.EntityFactory"&&m.Name=="CreateEntity"),"kindling creates no new supply");
  Need(!References(k).Any(m=>m.DeclaringType.FullName.Contains("Random")||m.DeclaringType.FullName.Contains("Rng")),"kindling selection does not draw randomness");
  Need(!Calls(type(core+"IgnitionSources"),k.FullName,"TryAct")&&!Words(type(core+"IgnitionSources")).Any(w=>w.Contains("FireMoss")),"legacy ignition has no silent moss fallback");
  Need(Words(type(core+"ItemTacticalUseDescription")).Any(w=>w.Contains("moss supplies no ongoing fuel")),"moss reader explains cost and finite-fuel boundary");
  foreach(var n in new[]{"RegionalWeather","RainCatchmentPart","RainCollectorPart","ShowerCollectionSite","VesselTorchRefueling"})Need(type(core+n)!=null&&Types(old).Any(t=>t.FullName==core+n),"retained packaged connection "+n);
  var refill=type(core+"VesselTorchRefueling");Need(refill.Fields.Single(f=>f.Name=="FuelPerOil").Constant.ToString()=="25","retained25fuel oil tariff");
  Need(Words(type(core+"ItemExamineService")).Any(w=>w.Contains("oil-filled flask")),"retained torch vessel-fuel reader");
  using(var previous=AssemblyDefinition.ReadAssembly(args[2])) {
   var pp=Types(previous).Where(t=>Part(t,previous)).ToDictionary(t=>t.FullName);
   Need(np.Keys.OrderBy(x=>x).SequenceEqual(pp.Keys.OrderBy(x=>x)),"Part names unchanged against immediate prior632, count="+np.Count);
   Need(np.All(x=>Fields(x.Value).SequenceEqual(Fields(pp[x.Key]))),"Part instance fields unchanged against immediate prior632");
  }
  var stock=type(core+"MerchantStockService");
  Need(Words(stock).Contains("SoundingRod")&&Words(stock).Contains("Cinderhold.Sealed")&&Words(stock).Contains("Pump"),"Cinderhold durable project and rod supply literals");
  Need(Words(stock).Any(w=>w.Contains("Repair the pump or seal the seep"))&&Words(stock).Any(w=>w.Contains("extra sounding rod")),"both lasting project approaches explained");
  var bonus=stock.Methods.Single(m=>m.Name=="ProjectBonus");
  var bonusRefs=bonus.Body.Instructions.Select(i=>i.Operand as MethodReference).Where(m=>m!=null).ToArray();
  foreach(var n in new[]{"HasExistingCondition","TryGetExistingSite"})Need(bonusRefs.Any(m=>m.DeclaringType.FullName==core+"SettlementManager"&&m.Name==n),"project bonus only existing-state query "+n);
  Need(!bonusRefs.Any(m=>m.Name.Contains("GetOrCreate")),"project qualification does not create settlements");
  Need(Calls(stock,core+"Inventory.InventoryTransaction","BeforeCommit"),"stock keeps transaction precommit validation");
  var fresh=stock.Methods.Single(m=>m.Name=="ValidFreshPayload");
  Need(fresh.Body.Instructions.Any(i=>(i.Operand as FieldReference)?.DeclaringType.FullName==core+"SoundingRodPart"&&(i.Operand as FieldReference)?.Name=="LastTick"),"fresh supply validates rod notebook state");
  var groups=type(core+"InventoryItemGroups");var rodIdentity=groups.Methods.Single(m=>m.Name=="RodIdentity");
  Need(rodIdentity.Body.Instructions.Any(i=>i.Operand is int&&(int)i.Operand==16384),"merchant rod identity bounded to16K");
  Need(rodIdentity.Body.Instructions.Any(i=>(i.Operand as FieldReference)?.Name=="Log"),"merchant identity reads actual rod log");
  Need(!rodIdentity.Body.Instructions.Select(i=>i.Operand as MethodReference).Where(m=>m!=null).Any(m=>m.DeclaringType.FullName==core+"SoundingRodPart"),"merchant identity invokes no rod reader/sounding");
  var skin=type(core+"WaterskinPart");var handle=skin.Methods.Single(m=>m.Name=="HandleEvent");
  var refs=handle.Body.Instructions.Select(i=>i.Operand as MethodReference).Where(m=>m!=null).ToArray();
  Need(refs.Count(m=>m.DeclaringType.FullName==skin.FullName&&m.Name=="AddUseRows")==1,"real water menu calls shared producer exactly once");
  Need(!refs.Any(m=>m.DeclaringType.FullName==core+"InventoryActionList"&&m.Name=="AddAction"),"water event has no unconditional drink/fill rows");
  var use=skin.Methods.Single(m=>m.Name=="AddUseRows");var ur=use.Body.Instructions.Select(i=>i.Operand as MethodReference).Where(m=>m!=null).ToArray();
  Need(ur.Any(m=>m.DeclaringType.FullName==core+"WaterVesselService"&&m.Name=="FreshSourceBeside")&&ur.Any(m=>m.Name=="AddDisabled"),"shared water menu checks actual reachable fresh supply");
  foreach(var n in new[]{"WaterTransferActions","DouseWorldActions"})Need(ur.Count(m=>m.DeclaringType.FullName==core+n&&m.Name=="AddActions")==1,"shared water menu retains single "+n);
  Need(Words(skin).Contains("The waterskin is empty.")&&Words(skin).Contains("The waterskin is full.")&&Words(skin).Contains("There is no fresh water beside you."),"water availability reasons present");
  Need(refs.Any(m=>m.DeclaringType.FullName==core+"WaterVesselService"&&m.Name=="TryAct"),"water execution retains guarded service");
  Console.WriteLine("CHECKS "+count);
 }}
}
