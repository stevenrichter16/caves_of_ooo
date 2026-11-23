using System;
using System.Linq;
using Mono.Cecil;
class DumpSmith {
 static string Body(MethodDefinition m) {
  string s=string.Join("\n",m.Body.Instructions.Select(i=>i.ToString()).ToArray());
  s=System.Text.RegularExpressions.Regex.Replace(s,@"(MerchantStockService/<>c__DisplayClass)\d+(_)","${1}METHOD${2}");
  s=System.Text.RegularExpressions.Regex.Replace(s,@"(MerchantStockService/<>c::<>9__)\d+(_)","${1}METHOD${2}");
  s=System.Text.RegularExpressions.Regex.Replace(s,@"(<Reconcile>b__)\d+(_)","${1}METHOD${2}");
  return System.Text.RegularExpressions.Regex.Replace(s,@"(<Reconcile>g__[^|]+\|)\d+(_)","${1}METHOD${2}");
 }
 static void Need(bool ok,string label) {if(!ok)throw new Exception(label);Console.WriteLine("VERIFIED "+label);}
 static void Main(string[] args) {
  using(var a=AssemblyDefinition.ReadAssembly(args[0]))using(var p=AssemblyDefinition.ReadAssembly(args[1])) {
   var types=a.MainModule.Types;var old=p.MainModule.Types;
   var stock=types.Single(t=>t.FullName=="CavesOfOoo.Core.MerchantStockService");var before=old.Single(t=>t.FullName==stock.FullName);
   Need(stock.Fields.Any(f=>f.Name=="CinderholdSmith")&&!before.Fields.Any(f=>f.Name=="CinderholdSmith"),"new local smith profile");
   var initial=stock.Methods.Single(m=>m.Name==".cctor");var words=initial.Body.Instructions.Select(i=>i.Operand as string).Where(x=>x!=null).ToArray();
   foreach(var s in new[]{"Cinderhold","WeaponsmithStock","IronSpikeComponent","OakHaftComponent","LeatherBindingComponent","SplitHaftTokenTrinket","ShortSword","Mace","Spear","Hatchet","LongSword","Battleaxe","Warhammer","SteelBladeComponent","SerratedEdgeComponent","StunGasGrenade"})Need(words.Contains(s),"catalogue "+s);
   var profile=stock.Methods.Single(m=>m.Name=="ProfileFor");Need(profile.Body.Instructions.Any(i=>i.Operand!=null&&i.Operand.ToString().Contains("::CinderholdSmith")),"profile dispatch includes smith");
   Need(profile.Body.Instructions.Any(i=>i.Operand!=null&&i.Operand.ToString().Contains("PlaceSiteCatalog::PlaceAt")),"profile dispatch retains place gate");
   var fresh=stock.Methods.Single(m=>m.Name=="ValidFreshPayload");var fields=fresh.Body.Instructions.Select(i=>i.Operand as FieldReference).Where(f=>f!=null&&f.DeclaringType.FullName=="CavesOfOoo.Core.WeaponComponentPart").Select(f=>f.Name).Distinct().ToArray();
   foreach(var f in new[]{"Slot","BaseDamage","PenBonus","HitBonus","MaxStrengthBonus","Attributes","OnHitEffectSpec","NameFragment"})Need(fields.Contains(f),"fresh component field "+f);
   Need(fresh.Body.Instructions.Any(i=>(i.Operand as string)=="stun-vapor"),"fresh stun gas identity");
   foreach(var m in new[]{"Reconcile","ProjectBonus","ReleaseForTransfer","EnterTrade"})Need(Body(stock.Methods.Single(x=>x.Name==m))==Body(before.Methods.Single(x=>x.Name==m)),"unchanged merchant core "+m);
   foreach(var n in new[]{"CavesOfOoo.Core.MerchantStockPart","CavesOfOoo.Core.LiquidCoveredEffect"})Need(types.Single(t=>t.FullName==n).Fields.Select(f=>f.FullName).SequenceEqual(old.Single(t=>t.FullName==n).Fields.Select(f=>f.FullName)),"unchanged saved fields "+n);
   foreach(var pair in new[]{new[]{"CavesOfOoo.Core.BodyCoatingCleanupActions","Recipient"},new[]{"CavesOfOoo.Core.InventoryItemGroups",".cctor"}})Need(Body(types.Single(t=>t.FullName==pair[0]).Methods.Single(m=>m.Name==pair[1]))==Body(old.Single(t=>t.FullName==pair[0]).Methods.Single(m=>m.Name==pair[1])),"unchanged "+pair[0]+"."+pair[1]);
   foreach(var m in new[]{initial,profile,fresh})Console.WriteLine(m.FullName+"\n"+Body(m));
  }
 }
}
