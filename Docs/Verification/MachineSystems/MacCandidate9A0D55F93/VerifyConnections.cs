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
  Need(Calls(type(core+"Inventory.Commands.PerformInventoryActionCommand"),core+"IInventoryActionReceiptOwner","OwnsInventoryReceipt"),"attached Part receipt hook replaces the intentional direct kindling case");
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
   Need(np.Keys.OrderBy(x=>x).SequenceEqual(pp.Keys.OrderBy(x=>x)),"Part names unchanged against immediate prior088, count="+np.Count);
   Need(np.All(x=>Fields(x.Value).SequenceEqual(Fields(pp[x.Key]))),"Part instance fields unchanged against immediate prior088");
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

  var fuel=type(core+"ReagentFireFuel");var receipt=type(core+"IInventoryActionReceiptOwner");
  Need(receipt.IsInterface&&receipt.Methods.Any(m=>m.Name=="OwnsInventoryReceipt"&&m.Parameters.Count==1),"small Part receipt contract present");
  Need(!Part(fuel,a)&&fuel.IsAbstract&&fuel.IsSealed,"new finite fuel helper is static, not a saved Part");
  Need(fuel.Fields.Single(f=>f.Name=="FuelPerTier").Constant.ToString()=="5","explicit five fuel per supported tier");
  Need(Words(fuel).Contains("FeedReagentFuel|"),"fuel has separate explicit quoted command");
  foreach(var name in new[]{"CampfirePart","TorchLightPart"}) {
   var owner=type(core+name);
   Need(owner.Interfaces.Any(i=>i.InterfaceType.FullName==receipt.FullName),name+" supplies attached receipt ownership");
   Need(Calls(owner,k.FullName,"OwnsReceipt"),name+" retains its existing kindling receipt");
  }
  foreach(var method in new[]{"AddActions","TryAct"})Need(Calls(type(core+"CampfirePart"),fuel.FullName,method),"fire target dispatches finite reagent fuel "+method);
  Need(type(core+"ReagentPart").Interfaces.Any(i=>i.InterfaceType.FullName==core+"IDescribeUse")&&Calls(type(core+"ReagentPart"),fuel.FullName,"DescribeUse"),"generic reagent reader explains fuel from the shared rule");
  Need(References(fuel).Any(m=>m.DeclaringType.FullName=="System.Int32"&&m.Name=="TryParse"&&m.Parameters.Count==4)&&!Calls(fuel,core+"BrewPropertyAmount","ParseList"),"fuel uses strict full property parsing rather than dropping malformed brew entries");
  foreach(var method in new[]{"BeforeCommit","TryClaim","AfterCommit"})Need(Calls(fuel,core+"Inventory.InventoryTransaction",method),"fuel transaction wiring "+method);
  Need(Calls(fuel,core+"InventoryPart","TryConsumeOne"),"fuel spends one carried unit");
  Need(Words(fuel).Contains("ReagentFuelRefundIdentityChanged")&&Words(fuel).Contains("unrepresentable-gain"),"fuel identity and precision refusal diagnostics present");
  Need(!References(fuel).Any(m=>m.DeclaringType.FullName=="CavesOfOoo.Data.EntityFactory"&&m.Name=="CreateEntity"),"fuel helper creates no new material supply");

  foreach(var helper in new[]{"BrineDistillation","AcidRinseActions","SpringTrapRecovery"}) {
   var ht=type(core+helper);Need(!Part(ht,a)&&ht.IsAbstract&&ht.IsSealed,helper+" is static, not a saved Part");
   Need(Calls(ht,core+"Inventory.InventoryTransaction","BeforeCommit"),helper+" revalidates before commitment");
   Need(Calls(ht,core+"Inventory.InventoryTransaction","TryClaim"),helper+" participates in resource claims");
  }
  var distill=type(core+"BrineDistillation");
  foreach(var pair in new[]{new[]{"Feed","4"},new[]{"Water","2"},new[]{"Fuel","5"}})Need(distill.Fields.Single(f=>f.Name==pair[0]).Constant.ToString()==pair[1],"distillation constant "+pair[0]);
  Need(Words(distill).Contains("DistillBrine|"),"distillation has its own quoted command");
  Need(Calls(type(core+"LiquidSettlingPart"),distill.FullName,"AddRows")&&Calls(type(core+"LiquidSettlingPart"),distill.FullName,"TryAct"),"standing still dispatches distillation");
  foreach(var ownerName in new[]{"LiquidSettlingPart","LiquidVesselPart","WaterskinPart","SpringTrapPart"})
   Need(type(core+ownerName).Interfaces.Any(i=>i.InterfaceType.FullName==core+"IInventoryActionReceiptOwner"),ownerName+" owns attached inventory receipts");
  var rinse=type(core+"AcidRinseActions");Need(rinse.Fields.Single(f=>f.Name=="WaterCost").Constant.ToString()=="2"&&rinse.Fields.Single(f=>f.Name=="MaximumAmount").Constant.ToString()=="20","rinse costs two water, removes up to20");
  Need(Calls(type(core+"CombatUtilityActions"),rinse.FullName,"AddActions"),"shared utility producer offers acid rinse");
  foreach(var n in new[]{"LiquidVesselPart","WaterskinPart"})Need(Calls(type(core+n),rinse.FullName,"get_UseText"),n+" describes the shared rinse capability");
  Need(Calls(type(core+"CombatUtilityActions"),rinse.FullName,"TryAct"),"shared utility execution dispatches rinse");
  var recovery=type(core+"SpringTrapRecovery");var spring=type(core+"SpringTrapPart");
  Need(Words(recovery).Contains("RecoverSpringTrap|")&&Calls(spring,recovery.FullName,"AddRows")&&Calls(spring,recovery.FullName,"TryAct"),"spring trap dispatches recovery");
  Need(Calls(type(core+"LaidDevicePart"),recovery.FullName,"Supported"),"laid reader preserves supported recovery path");
  Need(type(core+"FishPond").Fields.Single(f=>f.Name=="MaxCells").Constant.ToString()=="2000","pond traversal covers bounded2000cell zone");
  var trigger=spring.Methods.Single(m=>m.Name=="OnTrigger");
  Need(trigger.Body.Instructions.Any(i=>(i.Operand as FieldReference)?.Name=="Armed"&&i.OpCode.Code==Mono.Cecil.Cil.Code.Stfld),"trigger marks cartridge spent");
  using(var immediate=AssemblyDefinition.ReadAssembly(args[3])) {
   var pp=Types(immediate).Where(t=>Part(t,immediate)).ToDictionary(t=>t.FullName);
   Need(np.Keys.OrderBy(x=>x).SequenceEqual(pp.Keys.OrderBy(x=>x)),"Part names unchanged against immediate238, count="+np.Count);
   Need(np.All(x=>Fields(x.Value).SequenceEqual(Fields(pp[x.Key]))),"Part instance fields unchanged against immediate238");
  }


  var input=type("CavesOfOoo.Rendering.InputHandler");
  var bump=Methods(input).Single(m=>m.Body.Instructions.Any(i=>(i.Operand as string)=="CampFixtureBumpBlocked"));
  Need(bump!=null,"one production movement method emits camp-fixture refusal");
  Need(bump.Body.Instructions.Select(i=>i.Operand as MethodReference).Where(m=>m!=null).Any(m=>m.DeclaringType.FullName==core+"HeldFixtureActions"&&m.Name=="IsPlayerHeld"),"actual movement branch checks existing installed-player authority");
  Need(bump.Body.Instructions.Select(i=>i.Operand as string).Where(s=>s!=null).Any(s=>s.Contains("deliberately break")),"refusal explains the deliberate alternative");
  Need(bump.Body.Instructions.Select(i=>i.Operand as MethodReference).Where(m=>m!=null).Any(m=>m.DeclaringType.FullName==core+"DestructionSystem"&&m.Name=="StrikeStructure"),"ordinary bump damage path retained");
  Need(Methods(input).Where(m=>m!=bump).Any(m=>m.Body.Instructions.Any(i=>(i.Operand as string)=="Break")&&m.Body.Instructions.Select(i=>i.Operand as MethodReference).Where(v=>v!=null).Any(v=>v.DeclaringType.FullName==core+"DestructionSystem"&&v.Name=="StrikeStructure")),"explicit Break retains separate structural damage path");
  using(var immediate=AssemblyDefinition.ReadAssembly(args[4])) {
   var previousInput=Types(immediate).Single(t=>t.FullName==input.FullName);
   Need(Fields(input).SequenceEqual(Fields(previousInput)),"InputHandler instance fields unchanged against immediate7a");
  }


  foreach(var helper in new[]{"CampBoxWork","MaterialProcessStock","MaterialProcessPayment","FieldBindingActions"})
   Need(!Part(type(core+helper),a),helper+" is not a saved Part");
  var armor=type(core+"ArmorPart");var assembly=type(core+"WeaponAssemblyPart");var box=type(core+"CampBoxWork");var worksiteStock=type(core+"MaterialProcessStock");var payment=type(core+"MaterialProcessPayment");var field=type(core+"FieldBindingActions");
  foreach(var n in new[]{"IMaterialReclaimPermission","IInventoryActionReceiptOwner","IDescribeUseRows"})Need(armor.Interfaces.Any(i=>i.InterfaceType.FullName==core+n),"armor supplies "+n);
  Need(Calls(armor,core+"MaterialProcessRegistry","ForReclamation")&&Calls(armor,core+"MaterialProcessActions","Handle"),"armor delegates explicit reclamation to shared process actions");
  var armorRows=armor.Methods.Single(m=>m.Name=="AddUseRows");
  Need(!armorRows.Body.Instructions.Select(i=>i.Operand as MethodReference).Where(m=>m!=null).Any(m=>m.DeclaringType.FullName==core+"MaterialProcessActions"&&m.Name=="Handle"),"armor guide avoids executable menu mutation");
  foreach(var n in new[]{"IInventoryActionReceiptOwner","IDescribeUseRows"})Need(assembly.Interfaces.Any(i=>i.InterfaceType.FullName==core+n),"assembled weapon supplies "+n);
  Need(Calls(assembly,field.FullName,"Handle")&&Calls(assembly,field.FullName,"AddUseRows"),"assembled weapon delegates field work and pure rows");
  var container=type(core+"ContainerPart");Need(container.Interfaces.Any(i=>i.InterfaceType.FullName==core+"IInventoryActionReceiptOwner")&&Calls(container,box.FullName,"Handle"),"existing container delegates owned box work and receipts");
  Need(Words(box).Contains("CampBoxWork|")&&Words(field).Contains("FieldBinding|"),"separate quoted box and field command families");
  foreach(var method in new[]{"TryQuote","TryExecute"})Need(Calls(box,core+"MaterialProcessService",method),"box uses shared material engine "+method);
  Need(Calls(box,core+"MaterialProcessService/Quote","Current"),"box retains only current quoted promises");
  Need(Calls(worksiteStock,core+"HeldFixtureActions","IsPlayerHeld"),"stock uses actual player installation authority");
  Need(Methods(worksiteStock).Any(m=>m.Body.Instructions.Any(i=>(i.Operand as FieldReference)?.Name=="Gone")),"stock rejects logically destroyed fixtures");
  Need(Calls(worksiteStock,payment.FullName,"Spend")&&Calls(field,payment.FullName,"Spend"),"box and field share scoped unit payment");
  Need(Calls(payment,core+"MaterialProcessService","OwnsConsumption"),"payment requires owning resource reservation");
  Need(Words(payment).Contains("changed_input_parts")&&Words(payment).Contains("changed_input_payload")&&Words(payment).Contains("changed_count_bounds"),"refund validates part identity payload and bounds");
  foreach(var method in new[]{"Resolve","Begin"})Need(Calls(field,core+"MaterialCuttingWorkReceipt",method),"field uses shared cutting receipt "+method);
  Need(Calls(field,core+"WeaponForgingService","ApplyComponentStats"),"field uses existing component stat math");
  Need(!References(field).Any(m=>m.DeclaringType.FullName=="CavesOfOoo.Data.EntityFactory"&&m.Name=="CreateEntity"),"field creates no displaced or transient entities");
  Need(!Calls(field,core+"WeaponTemperingService","ClearTemper"),"field does not clear temper");
  Need(!Methods(field).Any(m=>m.Body.Instructions.Any(i=>i.OpCode.Code==Mono.Cecil.Cil.Code.Stfld&&(i.Operand as FieldReference)?.DeclaringType.FullName==core+"Stat")),"field does not write statistic fields");
  Need(Calls(field,core+"Inventory.InventoryTransaction","BeforeCommit")&&Calls(field,core+"Inventory.InventoryTransaction","TryClaimAction"),"field claims action and revalidates at commit");
  Need(type(core+"MaterialProcessPlan").Methods.Any(m=>m.Name=="TryCreateFromStock"),"shared planner accepts an explicit stock context");
  Need(Words(type(core+"PackableFixturePart")).Any(w=>w.Contains("direct contents")),"box instructions describe direct stock boundary");
  using(var immediate=AssemblyDefinition.ReadAssembly(args[5])) {
   var pp=Types(immediate).Where(t=>Part(t,immediate)).ToDictionary(t=>t.FullName);
   Need(np.Keys.OrderBy(x=>x).SequenceEqual(pp.Keys.OrderBy(x=>x)),"Part names unchanged against immediate6b, count="+np.Count);
   Need(np.All(x=>Fields(x.Value).SequenceEqual(Fields(pp[x.Key]))),"Part instance fields unchanged against immediate6b");
  }

  Console.WriteLine("CHECKS "+count);
 }}
}
