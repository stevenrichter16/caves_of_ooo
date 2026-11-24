using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
class VerifyPackage {
 static void Need(bool ok,string label) {if(!ok)throw new Exception(label);Console.WriteLine("VERIFIED "+label);}
 static string Body(MethodDefinition m) {
  string s=string.Join("\n",m.Body.Instructions.Select(i=>i.ToString()).ToArray());
  s=System.Text.RegularExpressions.Regex.Replace(s,@"(MerchantStockService/<>c__DisplayClass)\d+(_)","${1}METHOD${2}");
  s=System.Text.RegularExpressions.Regex.Replace(s,@"(MerchantStockService/<>c::<>9__)\d+(_)","${1}METHOD${2}");
  return s;
 }
 static IEnumerable<TypeDefinition> Types(TypeDefinition t){yield return t;foreach(var n in t.NestedTypes)foreach(var x in Types(n))yield return x;}
 static IEnumerable<string> Strings(TypeDefinition t){return Types(t).SelectMany(x=>x.Methods).Where(m=>m.HasBody).SelectMany(m=>m.Body.Instructions).Select(i=>i.Operand as string).Where(x=>x!=null);}
 static void Main(string[] args) {
  using(var a=AssemblyDefinition.ReadAssembly(args[0]))using(var p=AssemblyDefinition.ReadAssembly(args[1])) {
   var types=a.MainModule.Types;var old=p.MainModule.Types;
   var stock=types.Single(t=>t.FullName=="CavesOfOoo.Core.MerchantStockService");var before=old.Single(t=>t.FullName==stock.FullName);
   Need(stock.Fields.Any(f=>f.Name=="Weaponsmith")&&!before.Fields.Any(f=>f.Name=="Weaponsmith"),"shared Weaponsmith profile added");
   Need(stock.Fields.Any(f=>f.Name=="CinderholdSmith"),"regional Cinderhold variant retained");
   var initial=stock.Methods.Single(m=>m.Name==".cctor");var words=initial.Body.Instructions.Select(i=>i.Operand as string).Where(x=>x!=null).ToArray();
   foreach(var s in new[]{"Cinderhold","WeaponsmithStock","Dagger","ShortSword","Spear","IronSpikeComponent","OakHaftComponent","LeatherBindingComponent","SplitHaftTokenTrinket","Mace","Hatchet","LongSword","Battleaxe","Warhammer","SteelBladeComponent","SerratedEdgeComponent","StunGasGrenade"})Need(words.Contains(s),"catalogue "+s);
   var profile=stock.Methods.Single(m=>m.Name=="ProfileFor");var dispatch=Body(profile);
   Need(dispatch.Contains("::Weaponsmith")&&dispatch.Contains("::CinderholdSmith")&&dispatch.Contains("PlaceSiteCatalog::PlaceAt"),"genuine stock dispatch and regional variant");
   var fresh=stock.Methods.Single(m=>m.Name=="ValidFreshPayload");var fields=fresh.Body.Instructions.Select(i=>i.Operand as FieldReference).Where(f=>f!=null&&f.DeclaringType.FullName=="CavesOfOoo.Core.MeleeWeaponPart").Select(f=>f.Name).Distinct().ToArray();
   foreach(var f in new[]{"BaseDamage","PenBonus","HitBonus","MaxStrengthBonus","Stat","Attributes","OnHitEffectsRaw","EmitGasOnHitRaw"})Need(fields.Contains(f),"fresh standard melee field "+f);
   var reconcile=stock.Methods.Single(m=>m.Name=="Reconcile");var body=Body(reconcile);
   File.WriteAllText(args[2]+"/reconcile-new.txt",body);File.WriteAllText(args[2]+"/reconcile-old.txt",Body(before.Methods.Single(m=>m.Name=="Reconcile")));
   Need(body!=Body(before.Methods.Single(m=>m.Name=="Reconcile")),"Reconcile intentionally changes for promoted-special retention");
   Need(body.Contains("::Staples")&&body.Contains("CavesOfOoo.Core.Entity::BlueprintName")&&body.Contains("System.Linq.Enumerable::Contains<System.String>"),"promoted staple guard is present in packaged Reconcile");
   foreach(var m in new[]{"ProjectBonus","ReleaseForTransfer","EnterTrade"})Need(Body(stock.Methods.Single(x=>x.Name==m))==Body(before.Methods.Single(x=>x.Name==m)),"unchanged merchant core "+m);
   foreach(var n in new[]{"CavesOfOoo.Core.MerchantStockPart","CavesOfOoo.Core.LiquidCoveredEffect"})Need(types.Single(t=>t.FullName==n).Fields.Select(f=>f.FullName).SequenceEqual(old.Single(t=>t.FullName==n).Fields.Select(f=>f.FullName)),"unchanged saved fields "+n);
   foreach(var pair in new[]{new[]{"CavesOfOoo.Core.BodyCoatingCleanupActions","Recipient"},new[]{"CavesOfOoo.Core.InventoryItemGroups",".cctor"}})Need(Body(types.Single(t=>t.FullName==pair[0]).Methods.Single(m=>m.Name==pair[1]))==Body(old.Single(t=>t.FullName==pair[0]).Methods.Single(m=>m.Name==pair[1])),"unchanged "+pair[0]+"."+pair[1]);
   var ui=types.Single(t=>t.FullName=="CavesOfOoo.Rendering.InventoryUI");var prevui=old.Single(t=>t.FullName==ui.FullName);
   foreach(var n in new[]{"OpenCategoryChooser","OpenSortChooser","OpenBrowseCommands","RenderBrowseHeader"})Need(ui.Methods.Any(m=>m.Name==n)&&!prevui.Methods.Any(m=>m.Name==n),"new inventory method "+n);
   var uistr=Strings(ui).ToArray();foreach(var s in new[]{"CATEGORIES","SORT ITEMS","Heaviest first (group total)","Name A-Z","All categories"})Need(uistr.Any(w=>w.Contains(s)),"inventory text "+s);
   var cat=types.Single(t=>t.FullName=="CavesOfOoo.Core.ItemCategory");foreach(var s in new[]{"Seeds","Reagents","Components","Materials","Containers","CampEquipment","Documents"})Need(cat.Fields.Any(f=>f.Name==s&&f.HasConstant),"new category "+s);
   var reader=types.Single(t=>t.FullName=="CavesOfOoo.Core.ItemExamineService");var readerwords=Strings(reader).ToArray();
   Need(!readerwords.Any(s=>s.Contains("AV is armor value")||s.Contains("These are this item's contributions")),"removed explanatory armor paragraph absent");
   foreach(var s in new[]{"Armor value (AV): ","Dodge value (DV): "})Need(readerwords.Contains(s),"separate equipment line "+s);
   foreach(var m in new[]{initial,profile,fresh,reconcile})File.WriteAllText(args[2]+"/"+m.Name.Replace('.','_')+"-packaged-il.txt",Body(m));
  }
 }
}
