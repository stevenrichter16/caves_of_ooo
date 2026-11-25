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
  string[] routeParts={core+"DirectedAirPart",core+"TrailMarkingPart",core+"TrailMarkPart",core+"LodgedSpikePart"};
  string[] foundationParts={core+"CargoSledPart",core+"SledLoadPart",core+"ScreeningToolPart",core+"DirectLiquidPumpPart"};
  var addedParts=new[]{core+"FieldWorkToolPart"}.Concat(routeParts).Concat(foundationParts).ToArray();
  var k=type(core+"PortableKindling");var knife=type(core+"CuttingToolPart");
  Need(!Types(old).Any(t=>t.FullName==k.FullName)&&!Part(k,a),"new ignition adapter is not a saved Part");
  var np=Types(a).Where(t=>Part(t,a)).ToDictionary(t=>t.FullName);var op=Types(old).Where(t=>Part(t,old)).ToDictionary(t=>t.FullName);
  Need(np.Keys.OrderBy(x=>x).SequenceEqual(op.Keys.Concat(addedParts).OrderBy(x=>x)),"all prior Part names plus explicit field/route Parts, count="+np.Count);
  Need(op.All(x=>Fields(np[x.Key]).SequenceEqual(Fields(x.Value))),"all existing Part instance-field shapes unchanged");
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
   Need(np.Keys.OrderBy(x=>x).SequenceEqual(pp.Keys.Concat(addedParts).OrderBy(x=>x)),"prior Part names plus explicit field/route tools against immediate prior088, count="+np.Count);
   Need(pp.All(x=>Fields(np[x.Key]).SequenceEqual(Fields(x.Value))),"Part instance fields unchanged against immediate prior088");
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
   Need(np.Keys.OrderBy(x=>x).SequenceEqual(pp.Keys.Concat(addedParts).OrderBy(x=>x)),"prior Part names plus explicit field/route tools against immediate238, count="+np.Count);
   Need(pp.All(x=>Fields(np[x.Key]).SequenceEqual(Fields(x.Value))),"Part instance fields unchanged against immediate238");
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
   Need(np.Keys.OrderBy(x=>x).SequenceEqual(pp.Keys.Concat(addedParts).OrderBy(x=>x)),"prior Part names plus explicit field/route tools against immediate6b, count="+np.Count);
   Need(pp.All(x=>Fields(np[x.Key]).SequenceEqual(Fields(x.Value))),"Part instance fields unchanged against immediate6b");
  }


  var fieldTool=type(core+"FieldWorkToolPart");
  Need(Part(fieldTool,a),"field tool remains a saved Part");
  Need(Fields(fieldTool).SequenceEqual(new[]{"Charge:System.Int32","Kind:System.String"}),"new tool saves only exact Kind and Charge fields");
  Need(fieldTool.Fields.Single(f=>f.Name=="HeatFuel").Constant.ToString()=="5","aimed heat tariff is five actual fuel");
  foreach(var n in new[]{"IDescribeUse","IDescribeUseRows","IDescribeState","IInventoryActionReceiptOwner"})
   Need(fieldTool.Interfaces.Any(i=>i.InterfaceType.FullName==core+n),"field tool supplies "+n);
  foreach(var n in new[]{"DirectHearthHeat|","LoadDischargeRod|","DischargeRod|","CondenseSteam|"})Need(Words(fieldTool).Contains(n),"explicit field command "+n);
  foreach(var n in new[]{"BeforeCommit","AfterCommit","TryClaim","TryClaimAction"})Need(Calls(fieldTool,core+"Inventory.InventoryTransaction",n),"field transaction "+n);
  foreach(var n in new[]{"ApplyFireToTile","AddCharge","ResolveAfterAbility"})Need(Calls(fieldTool,core+"ZoneTileStateSystem",n),"field delegates shared tile rule "+n);
  foreach(var n in new[]{"StockHolder","ElementOf"})Need(Calls(fieldTool,core+"ProofingSources",n),"field uses shared source "+n);
  foreach(var n in new[]{"For","CapacityFor","AvailableAt","Reconcile"})Need(Calls(fieldTool,core+"SourceStockPart",n),"field uses existing finite budget "+n);
  Need(Calls(fieldTool,core+"LiquidSourcePhase","CanDrawWater"),"condenser retains source phase refusal");
  Need(Calls(fieldTool,core+"Cell","BlocksMovement"),"rod line uses shared movement door/archive/footprint blocking");
  Need(Calls(fieldTool,core+"MaterialProcessPayment","Spend"),"root loading uses existing scoped material payment");
  Need(Calls(fieldTool,core+"HeldFixtureActions","IsPlayerHeld")&&Words(fieldTool).Contains("Player"),"nozzle checks own installation and Player actor boundary");
  Need(Calls(fieldTool,core+"InventoryPart","FindEquippedBodyPart"),"removed last root is checked against body custody");
  Need(Words(fieldTool).Contains("FieldWorkPaidContextChanged"),"late paid context failure has its own diagnostic");
  Need(!References(fieldTool).Any(m=>m.DeclaringType.FullName=="CavesOfOoo.Data.EntityFactory"&&m.Name=="CreateEntity"),"field actions create no roots tools or refund entities");
  Need(!References(fieldTool).Any(m=>m.DeclaringType.FullName.Contains("Random")||m.DeclaringType.FullName.Contains("Rng")),"field resource work does not draw world randomness");
  Need(!Methods(fieldTool).Any(m=>m.Body.Instructions.Any(i=>i.OpCode.Code==Mono.Cecil.Cil.Code.Stfld&&(i.Operand as FieldReference)?.DeclaringType.FullName==core+"Stat")),"field tool does not write actor statistics");
  var processPermission=type(core+"IMaterialProcessPermission");Need(processPermission.IsInterface,"general explicit material process permission exists");
  Need(type(core+"IMaterialReclaimPermission").Interfaces.Any(i=>i.InterfaceType.FullName==processPermission.FullName),"existing reclaim marker retains inherited permission contract");
  var componentTool=type(core+"WeaponComponentPart");
  foreach(var n in new[]{"IMaterialProcessPermission","IInventoryActionReceiptOwner","IDescribeUseRows"})Need(componentTool.Interfaces.Any(i=>i.InterfaceType.FullName==core+n),"component adapter supplies "+n);
  Need(Calls(componentTool,core+"MaterialProcessActions","Handle"),"component delegates to shared process commands");
  Need(Calls(type(core+"MaterialProcessActions"),core+"MaterialProcessService/Quote","Current"),"shared process menus retain only current quotes");
  Need(Words(type(core+"MaterialProcessPayment")).Contains("input_already_listed"),"shared refund refuses a preexisting raw input alias");
  Need(References(type(core+"ItemCategory")).Any(m=>m.Name=="HasPart"&&m is GenericInstanceMethod&&((GenericInstanceMethod)m).GenericArguments.Any(t=>t.FullName==fieldTool.FullName)),"new tool capability classifies under Tools");
  Need(Methods(type(core+"MaterialProcessPayloadPolicies")).Any(m=>m.Body.Instructions.Any(i=>(i.Operand as TypeReference)?.FullName==fieldTool.FullName)),"new scalar Part has explicit payload policy registration");
  using(var priorWorksite=AssemblyDefinition.ReadAssembly(args[6])) {
   var priorParts=Types(priorWorksite).Where(t=>Part(t,priorWorksite)).ToDictionary(t=>t.FullName);
   Need(np.Keys.OrderBy(x=>x).SequenceEqual(priorParts.Keys.Concat(addedParts).OrderBy(x=>x)),"exact explicit field and route Part additions versus worksite9a");
   Need(priorParts.All(x=>Fields(np[x.Key]).SequenceEqual(Fields(x.Value))),"all428 prior worksite Part instance-field shapes retained");
  }

  var air=type(core+"DirectedAirPart");var marking=type(core+"TrailMarkingPart");var mark=type(core+"TrailMarkPart");var lodged=type(core+"LodgedSpikePart");
  Need(Fields(air).Length==0&&Fields(marking).Length==0,"air and marking capability Parts have no saved instance fields");
  Need(Fields(mark).SequenceEqual(new[]{"Note:System.String"}),"trail mark saves exactly its player Note");
  Need(Fields(lodged).SequenceEqual(new[]{"Door:"+core+"Entity"}),"lodged spike saves exactly its actual Door reference");
  Need(air.Fields.Single(f=>f.Name=="MaximumDensity").Constant.ToString()=="40","bellows whole-cloud density admission capped at40");
  Need(Calls(air,core+"Zone","MoveEntity"),"bellows moves the existing gas owner");
  Need(!References(air).Any(m=>m.Name=="CreateEntity"||m.Name=="SpawnGas"||m.Name=="set_Density"),"bellows does not create gas or write density");
  Need(Calls(air,core+"Cell","BlocksMovement")&&Calls(air,core+"ZoneRenderHooks","MarkCellDirty"),"bellows reuses physical blockers and dirties actual cells");
  Need(Words(air).Contains("PushGas|")&&Words(air).Contains("DirectedCloudMoved"),"directed air command and committed conservation diagnostic retained");
  foreach(var n in new[]{"BeforeCommit","AfterCommit","TryClaimAction"})Need(Calls(type(core+"RouteWorkMenu"),core+"Inventory.InventoryTransaction",n),"route action promise uses transaction "+n);
  Need(!Part(type(core+"RouteWorkMenu"),a),"local route promises are not saved simulation state");
  Need(Calls(marking,core+"MaterialProcessTemplate","Capture")&&Calls(marking,core+"MaterialProcessTemplate","Snapshot"),"sand and fresh marker use explicit audited payload admission");
  Need(Calls(marking,core+"MaterialProcessPayment","Spend"),"marking spends an actual reserved sand unit");
  foreach(var note in new[]{"north","east","south","west","exit","water","danger"})Need(Words(marking).Contains(note),"player mark preset "+note);
  Need(Words(marking).Contains("MakeTrailMark|")&&Words(mark).Contains("ClearTrailMark|"),"mark and clear use separate explicit action families");
  Need(Calls(mark,core+"Zone","RemoveEntity"),"brushing removes the actual mark");
  Need(Calls(type(core+"DoorPart"),core+"DoorBraceActions","Blocked"),"ordinary player and opening-AI permission consult the shared brace rule");
  Need(Calls(type(core+"DoorBraceActions"),core+"InventoryPart","AddRetrievedObject"),"brace recovery retains the exact spike rather than merging its identity");
  Need(!References(type(core+"DoorBraceActions")).Any(m=>m.Name=="CreateEntity"),"bracing never reconstructs a spike");
  Need(Calls(type(core+"WeaponComponentPart"),core+"DoorBraceActions","Handle")&&Calls(type(core+"WeaponComponentPart"),core+"DoorBraceActions","AddUseRows"),"existing component supplies explicit brace and guide adapters");
  using(var immediateRoute=AssemblyDefinition.ReadAssembly(args[7])) {
   var priorParts=Types(immediateRoute).Where(t=>Part(t,immediateRoute)).ToDictionary(t=>t.FullName);
   Need(np.Keys.OrderBy(x=>x).SequenceEqual(priorParts.Keys.Concat(routeParts).Concat(foundationParts).OrderBy(x=>x)),"explicit route and transport/processing Part additions versus152b");
   Need(priorParts.All(x=>Fields(np[x.Key]).SequenceEqual(Fields(x.Value))),"all429 immediate152b runtime Part instance-field shapes retained");
  }

  var sled=type(core+"CargoSledPart");var load=type(core+"SledLoadPart");var screen=type(core+"ScreeningToolPart");var pump=type(core+"DirectLiquidPumpPart");
  Need(Fields(sled).SequenceEqual(new[]{"Load:"+core+"Entity"}),"sled saves actual Load reference only");
  Need(Fields(load).SequenceEqual(new[]{"Sled:"+core+"Entity"}),"supported load saves actual Sled reference only");
  Need(Fields(screen).Length==0&&Fields(pump).Length==0,"sieve and pump capabilities add no saved fields");
  Need(Calls(type(core+"HandlingService"),core+"CargoSledSupport","Weight"),"shared handling includes physical sled weight");
  foreach(var method in new[]{"Assists","Follow"})Need(Calls(type(core+"DragSystem"),core+"CargoSledSupport",method),"ordinary dragging delegates sled "+method);
  Need(Calls(type(core+"TurnManager"),core+"DragSystem","ValidateLink"),"turn scheduling validates hauling link before energy accrual");
  var speed=type(core+"TurnManager").Methods.Single(m=>m.Name=="GetSpeed");
  Need(!speed.Body.Instructions.Select(i=>i.Operand as MethodReference).Where(m=>m!=null).Any(m=>m.DeclaringType.FullName==core+"DragSystem"&&m.Name=="ValidateLink"),"GetSpeed remains a pure query");
  Need(Calls(sled,core+"InventoryPart","AddRetrievedObject"),"same sled retrieval uses exact-owner add");
  var work=type(core+"IMaterialWorkReceipt");Need(work.IsInterface&&work.Methods.Count==3,"internal work receipt has only Tool Current Begin");
  Need(type(core+"MaterialCuttingWorkReceipt").Interfaces.Any(i=>i.InterfaceType.FullName==work.FullName),"existing cutting implements shared work receipt");
  var screening=type(core+"MaterialScreeningWorkReceipt");Need(screening.Interfaces.Any(i=>i.InterfaceType.FullName==work.FullName),"screening implements same receipt");
  Need(Calls(screening,core+"WorldResourceActions","Carried"),"screening checks actual carried tool");
  foreach(var method in new[]{"TryClaim","BeforeCommit"})Need(Calls(screening,core+"Inventory.InventoryTransaction",method),"screening transaction "+method);
  Need(Calls(type(core+"MaterialProcessPlan"),screening.FullName,"Resolve")&&Words(type(core+"MaterialProcessPlan")).Contains("Screening"),"plan explicitly selects screening work");
  Need(type(core+"MaterialProcessInputPart").Interfaces.Any(i=>i.InterfaceType.FullName==core+"IInventoryActionReceiptOwner"),"material input owns rollback receipt instead of actor snapshot");
  Need(Words(type(core+"MaterialProcessService")).Contains("Screening")&&Words(type(core+"MaterialProcessService")).Contains("Tool"),"screening event emits actual tool metadata");
  Need(pump.Fields.Single(f=>f.Name=="StrokeVolume").Constant.ToString()=="20","pump stroke is bounded to20 real units");
  foreach(var method in new[]{"AddEntity","RemoveEntity"})Need(Calls(pump,core+"Zone",method),"pump uses normal ground-owner lifecycle "+method);
  Need(Calls(pump,core+"LiquidSourcePhase","CanDrawWater"),"pump retains existing aqueous phase policy");
  Need(Calls(pump,core+"LiquidSourceSafety","IsUnmixedCell"),"pump retains shared mixing refusal");
  Need(Calls(pump,core+"Inventory.InventoryTransaction","TryClaim"),"pump claims actual source recipient and tool");
  foreach(var method in new[]{"Capture","Snapshot"})Need(Calls(pump,core+"MaterialProcessTemplate",method),"pump audits output construction "+method);
  Need(Calls(pump,core+"WaterQuantityUndo","Restore")&&Words(pump).Contains("PumpUndoCreditRetainedOrConsumed"),"pump bounded paired undo and partial-credit diagnosis present");
  Need(References(type(core+"ItemCategory")).Any(m=>m.Name=="HasPart"&&m is GenericInstanceMethod&&((GenericInstanceMethod)m).GenericArguments.Any(t=>t.FullName==pump.FullName)),"pump capability falls under Tools without rewriting saved Physics");
  foreach(var name in new[]{screen.FullName,pump.FullName,core+"LiquidPoolPart"})Need(Methods(type(core+"MaterialProcessPayloadPolicies")).Any(m=>m.Body.Instructions.Any(i=>(i.Operand as TypeReference)?.FullName==name)),"audited exact scalar policy present "+name);
  using(var immediateFoundation=AssemblyDefinition.ReadAssembly(args[8])){
   var priorParts=Types(immediateFoundation).Where(t=>Part(t,immediateFoundation)).ToDictionary(t=>t.FullName);
   Need(priorParts.Count==433&&np.Keys.OrderBy(x=>x).SequenceEqual(priorParts.Keys.Concat(foundationParts).OrderBy(x=>x)),"all433 priorA023 Part names plus exactly four explicit transport/processing Parts");
   Need(priorParts.All(x=>Fields(np[x.Key]).SequenceEqual(Fields(x.Value))),"all433 priorA023 Part instance-field shapes unchanged");
  }
  Console.WriteLine("CHECKS "+count);
 }}
}
