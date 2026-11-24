using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WeaponMerchantConsignmentTests : FiftyWorldFixture
    {
        static readonly string[] Choices={"SplitHaftTokenTrinket","Mace","Hatchet","LongSword","Battleaxe","Warhammer","SteelBladeComponent","SerratedEdgeComponent","StunGasGrenade"};
        CavesOfOoo.Data.EntityFactory oldTrader,oldRestock;
        [SetUp] public void PrepareWeapons()
        {
            oldTrader=TraderPart.Factory;oldRestock=TraderRestockSystem.Factory;
            TraderPart.Factory=null;TraderRestockSystem.Factory=Factory;Zone.ZoneID="Overworld.14.9.0";
            Pack.MaxWeight=1000;TradeSystem.SetDrams(Actor,10000);
        }
        [TearDown] public void RestoreWeapons()
        {TraderPart.Factory=oldTrader;TraderRestockSystem.Factory=oldRestock;QuillholdFactoryProbe.CreatedCallback=null;QuillholdFactoryProbe.Callback=null;}
        Entity Smith(string bp="Weaponsmith")
        {var e=Place(bp);e.ID="general-weapon-supply";e.GetPart<InventoryPart>().MaxWeight=1000;TradeSystem.SetDrams(e,10000);return e;}
        static int Units(Entity e,string bp)=>e.GetPart<InventoryPart>().Objects.Where(x=>x.BlueprintName==bp).Sum(BarterDeal.Units);
        static string Key(Entity e,Entity item)=>(string)typeof(MerchantStockService).GetMethod("Key",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{e,item});
        Entity Saved(Entity e)
        {
            using(var stream=new MemoryStream())
            {var w=new SaveWriter(stream);w.WriteEntityReference(e);w.WriteQueuedEntityBodies();stream.Position=0;var r=new SaveReader(stream,Factory);var copy=r.ReadEntityReference();r.ReadEntityBodies();return copy;}
        }
        MerchantStockPart Start(Entity e,int tick=10)
        {Assert.True(MerchantStockService.Reconcile(e,Zone,tick));var s=e.GetPart<MerchantStockPart>();Assert.NotNull(s);return s;}
        MerchantStockPart Legacy(Entity e,int stapleTick=10)
        {
            var goods=new[]{Factory.CreateEntity("ShortSword"),Factory.CreateEntity("Spear")};
            foreach(var item in goods)Assert.True(e.GetPart<InventoryPart>().AddObject(item));
            var s=new MerchantStockPart{ProfileId="WeaponsmithStock",Generation=0,LastStapleTick=stapleTick,LastSpecialTick=10,
                Specials=goods.ToList(),SpecialKeys=goods.Select(x=>Key(e,x)).ToList(),SelectionRaw="ShortSword|Spear",BonusApplied=false};
            Assert.True(s.SpecialKeys.All(x=>!string.IsNullOrEmpty(x)));e.AddPart(s);return s;
        }
        [TestCase("Overworld.14.9.0")] [TestCase("Overworld.10.10.0")] [TestCase("Overworld.6.6.1")]
        public void GenuineWeaponMerchantsOutsideCinderholdJoinTheSavedStockPolicy(string zone)
        {Zone.ZoneID=zone;var e=Smith();var s=Start(e);Assert.AreEqual("WeaponsmithStock",s.ProfileId);Assert.AreEqual(2,s.Specials.Count);}
        [TestCase("Dagger")] [TestCase("ShortSword")] [TestCase("Spear")]
        [TestCase("OakHaftComponent")] [TestCase("LeatherBindingComponent")]
        public void WeaponStandardsRemainAvailableWithoutWaitingForASpecial(string bp)
        {var e=Smith();Start(e);Assert.AreEqual(1,Units(e,bp));Assert.False(e.GetPart<MerchantStockPart>().Specials.Any(x=>x.BlueprintName==bp));}
        [Test] public void CinderholdKeepsItsExtraSpikeFloorAlongsideNewStandardWeapons()
        {Zone.ZoneID="Overworld.6.6.0";var e=Smith();Start(e);Assert.AreEqual(2,Units(e,"IronSpikeComponent"));Assert.AreEqual(1,Units(e,"ShortSword"));Assert.AreEqual(1,Units(e,"Spear"));}
        [Test] public void GeneralSmithDoesNotInventCinderholdRegionalSpikeGrant()
        {var e=Smith();Start(e);Assert.Zero(Units(e,"IronSpikeComponent"));}
        [Test] public void NineRemainingOriginalOptionalKindsCycleWithoutCoreDuplicates()
        {
            var e=Smith();var s=Start(e);var seen=new HashSet<string>();
            for(int n=0;n<Choices.Length;n++)
            {if(n>0)Assert.True(MerchantStockService.Reconcile(e,Zone,10+n*1200));Assert.AreEqual(2,s.Specials.Count);foreach(var x in s.Specials){seen.Add(x.BlueprintName);Assert.AreEqual(1,BarterDeal.Units(x));}}
            CollectionAssert.AreEquivalent(Choices,seen);Assert.AreEqual(1,Units(e,"ShortSword"));Assert.AreEqual(1,Units(e,"Spear"));
        }
        [TestCase("both")] [TestCase("staples")] [TestCase("specials")]
        public void SavedPromotedSpecialsSurviveBothIndependentClockBoundaries(string clock)
        {
            var original=Smith();Legacy(original,clock=="specials"?1000:10);var e=Saved(original);
            Assert.True(Zone.RemoveEntity(original));Assert.True(Zone.AddEntity(e,11,10));var s=e.GetPart<MerchantStockPart>();var old=s.Specials.ToArray();var ids=old.Select(x=>x.ID).ToArray();
            int tick=clock=="staples"?311:1210;Assert.True(MerchantStockService.Reconcile(e,Zone,tick));
            foreach(var x in old){Assert.Contains(x,e.GetPart<InventoryPart>().Objects);Assert.AreEqual(1,Units(e,x.BlueprintName));}
            if(clock=="staples"){CollectionAssert.AreEqual(ids,s.Specials.Select(x=>x.ID));Assert.Zero(s.Generation);Assert.AreEqual(10,s.LastSpecialTick);}
            else {Assert.AreEqual(1,s.Generation);Assert.False(s.Specials.Any(old.Contains));Assert.True(s.Specials.All(x=>Choices.Contains(x.BlueprintName)));}
            var copy=Saved(e);Assert.AreEqual(s.SelectionRaw,copy.GetPart<MerchantStockPart>().SelectionRaw);Assert.AreEqual(1,Units(copy,"ShortSword"));Assert.AreEqual(1,Units(copy,"Spear"));
        }
        [TestCase(false)] [TestCase(true)]
        public void ModifiedOrEscapedPromotedGoodsStayRealAndDoNotHideCanonicalDeficits(bool escaped)
        {
            var e=Smith();var s=Legacy(e);var old=s.Specials[0];
            if(escaped){Assert.True(e.GetPart<InventoryPart>().RemoveObject(old));Assert.True(Pack.AddObject(old));}
            else old.GetPart<MeleeWeaponPart>().BaseDamage="99d9";
            Assert.True(MerchantStockService.Reconcile(e,Zone,1210));Assert.False(MerchantStockService.IsSpecial(e,old));
            Assert.AreSame(escaped?Actor:e,old.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(escaped?1:2,Units(e,"ShortSword"));
            Assert.AreEqual(1,e.GetPart<InventoryPart>().Objects.Count(x=>x.BlueprintName=="ShortSword"&&x.GetPart<MeleeWeaponPart>().BaseDamage=="1d6"));
        }
        [TestCase("Tinker")] [TestCase("Quartermaster")] [TestCase("Arcanist")]
        public void NonWeaponVendorsKeepTheirExistingStockPolicy(string bp)
        {var e=Smith(bp);Assert.False(MerchantStockService.Reconcile(e,Zone,10));Assert.IsNull(e.GetPart<MerchantStockPart>());}
        [TestCase(false)] [TestCase(true)]
        public void BothAuthenticStockDeclarationsRemainRequired(bool property)
        {var e=Smith();if(property)e.Properties[TraderRestockSystem.ShopStockTableProp]="MerchantStock";else e.GetPart<TraderPart>().StockTable="MerchantStock";Assert.False(MerchantStockService.Reconcile(e,Zone,10));}
        [Test] public void ActiveViewAndPendingTradeKeepAnOldCurrentLotIntact()
        {
            var e=Smith();var s=Legacy(e);var old=s.Specials.ToArray();using(MerchantStockService.BeginView(e))Assert.False(MerchantStockService.Reconcile(e,Zone,1210));
            var deal=new BarterDeal(Actor,e);Assert.True(deal.SetTake(old[0],1));Assert.False(MerchantStockService.Reconcile(e,Zone,1210));MerchantStockService.ForgetOffer(deal);
            CollectionAssert.AreEqual(old,s.Specials);Assert.True(MerchantStockService.Reconcile(e,Zone,1210));
        }
        static IEnumerable<TestCaseData> WeaponFields()
        {foreach(string bp in new[]{"Dagger","ShortSword","Spear"})foreach(string field in new[]{"BaseDamage","PenBonus","HitBonus","MaxStrengthBonus","Stat","Attributes","OnHitEffectsRaw","EmitGasOnHitRaw"})yield return new TestCaseData(bp,field);}
        [TestCaseSource(nameof(WeaponFields))]
        public void FreshStandardWeaponsCannotBeRedefinedByFactoryCallbacks(string bp,string fieldName)
        {
            var e=Smith();Factory.RegisterPartType<QuillholdFactoryProbe>();Factory.Blueprints[bp].Parts[nameof(QuillholdFactoryProbe)]=new Dictionary<string,string>();bool fired=false;
            QuillholdFactoryProbe.CreatedCallback=item=>{fired=true;var field=typeof(MeleeWeaponPart).GetField(fieldName);field.SetValue(item.GetPart<MeleeWeaponPart>(),field.FieldType==typeof(string)?(object)"changed":99);};
            Assert.False(MerchantStockService.Reconcile(e,Zone,10));Assert.True(fired);Assert.IsNull(e.GetPart<MerchantStockPart>());Assert.IsEmpty(e.GetPart<InventoryPart>().Objects);
        }
        [TestCase("Dagger")] [TestCase("ShortSword")] [TestCase("Spear")]
        public void AStandardWeaponRequiresItsActualOwnedWeaponPart(string bp)
        {
            var e=Smith();Factory.RegisterPartType<QuillholdFactoryProbe>();Factory.Blueprints[bp].Parts[nameof(QuillholdFactoryProbe)]=new Dictionary<string,string>();bool fired=false;
            QuillholdFactoryProbe.CreatedCallback=item=>{fired=true;item.RemovePart(item.GetPart<MeleeWeaponPart>());};
            Assert.False(MerchantStockService.Reconcile(e,Zone,10));Assert.True(fired);Assert.IsNull(e.GetPart<MerchantStockPart>());Assert.IsEmpty(e.GetPart<InventoryPart>().Objects);
        }
    }
}
