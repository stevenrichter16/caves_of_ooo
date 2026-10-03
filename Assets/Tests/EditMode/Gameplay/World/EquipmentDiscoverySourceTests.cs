using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Regional equipment is acquired from actual native owners, never from a quest reward or a synthetic grant.</summary>
    public class EquipmentDiscoverySourceTests
    {
        internal static readonly string[] Special={"PeatMalletHeadComponent","GroundwireScreen","CinderhookAxeHeadComponent","KilnfeltApron","CounterweightLongBladeComponent"};
        internal static string Address(string source)=>source=="sodden"?SoddenDistrictPlan.WorksZoneID:source=="cinderhold"?CinderholdCompositionPlan.ZoneID:LastCounterCompositionPlan.ZoneID;
        internal static string[] Kit(string source)=>source=="sodden"?new[]{Special[0],Special[1],"OakHaftComponent","LeatherBindingComponent"}:source=="cinderhold"?new[]{Special[2],Special[3],"OakHaftComponent","LeatherBindingComponent"}:new[]{Special[4],"OakHaftComponent","LeatherBindingComponent"};
        internal static Entity Build(string source,EntityFactory factory,int seed,out Zone zone,out Func<bool> replay)
        {
            zone=new Zone(Address(source));var ownedZone=zone;
            if(source=="sodden")
            {
                var builder=new SoddenDistrictBuilder(seed);Assert.IsTrue(builder.BuildZone(zone,factory,new Random(seed)),"Sodden source must build.");
                Assert.IsTrue(builder.ValidateFinal(zone));replay=()=>builder.BuildZone(ownedZone,factory,new Random(seed));
                return zone.GetAllEntities().Single(e=>e.BlueprintName=="SoddenWorksLocker");
            }
            if(source=="cinderhold")
            {
                var builder=new CinderholdCompositionBuilder(seed);Assert.IsTrue(builder.BuildZone(zone,factory,new Random(seed)));
                var profile=new CinderholdProfileBuilder(builder);Assert.IsTrue(profile.BuildZone(zone,factory,new Random(seed)));replay=()=>profile.BuildZone(ownedZone,factory,new Random(seed));
                return zone.GetAllEntities().Single(e=>e.BlueprintName=="Weaponsmith");
            }
            var last=new LastCounterCompositionBuilder(seed);Assert.IsTrue(last.BuildZone(zone,factory,new Random(seed)));
            var late=new LastCounterProfileBuilder(last);Assert.IsTrue(late.BuildZone(zone,factory,new Random(seed)));replay=()=>late.BuildZone(ownedZone,factory,new Random(seed));
            return zone.GetAllEntities().Single(e=>e.BlueprintName=="Chest");
        }
        internal static Entity[] Items(Entity owner)=>owner.GetPart<ContainerPart>()?.Contents.ToArray()??owner.GetPart<InventoryPart>().Objects.ToArray();
        internal static string Text(Zone zone)=>string.Join("\n",zone.GetAllEntities().Select(e=>e.GetPart<ExaminablePart>()?.Text??""));

        [TestCase("sodden",1)][TestCase("sodden",64)][TestCase("sodden",1729)]
        [TestCase("cinderhold",1)][TestCase("cinderhold",64)][TestCase("cinderhold",1729)]
        [TestCase("counter",1)][TestCase("counter",64)][TestCase("counter",1729)]
        public void EachGeographyGuaranteesItsOwnCompleteUsableKit(string source,int seed)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(seed);var owner=Build(source,scope.Factory,seed,out var zone,out _);var items=Items(owner);
                foreach(string id in Kit(source))Assert.IsTrue(items.Any(e=>e.BlueprintName==id),source+" missing "+id);
                foreach(string id in Special.Except(Kit(source)))Assert.IsFalse(items.Any(e=>e.BlueprintName==id),"Another region's specialty leaked into this source.");
                foreach(var item in items)
                {Assert.AreSame(owner,item.GetPart<PhysicsPart>().InInventory);Assert.IsNull(zone.GetEntityCell(item));Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);Assert.IsTrue(item.GetPart<PhysicsPart>().Takeable);}
                foreach(string id in Kit(source).Where(Special.Contains))Assert.AreEqual(1,items.Count(e=>e.BlueprintName==id));
                Assert.AreEqual(items.Length,items.Select(e=>e.ID).Distinct().Count());
            }
        }
        [TestCase("sodden",false)][TestCase("sodden",true)][TestCase("counter",false)][TestCase("counter",true)]
        public void RegionalHeadUsesNativeContainerTransferAndRejectsALockedSource(string source,bool locked)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var owner=Build(source,scope.Factory,64,out var zone,out _);var box=owner.GetPart<ContainerPart>();
                var item=Items(owner).SingleOrDefault(e=>e.BlueprintName==Kit(source)[0]);Assert.NotNull(item);box.Locked=locked;
                var actor=scope.Factory.CreateEntity("Player");actor.GetPart<InventoryPart>().MaxWeight=10000;
                var at=zone.GetEntityPosition(owner);Assert.IsTrue(zone.AddEntity(actor,at.x+1,at.y));
                Assert.AreEqual(!locked,InventorySystem.ExecuteCommand(new TakeFromContainerCommand(owner,item),actor,zone).Success);
                Assert.AreEqual(locked,box.Contents.Contains(item));Assert.AreEqual(!locked,actor.GetPart<InventoryPart>().Objects.Contains(item));
            }
        }
        [TestCase(false)][TestCase(true)] public void CinderholdSpecialtyRequiresOrdinaryTradePayment(bool funded)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var owner=Build("cinderhold",scope.Factory,64,out _,out _);var item=Items(owner).SingleOrDefault(e=>e.BlueprintName==Special[2]);Assert.NotNull(item);
                var actor=scope.Factory.CreateEntity("Player");actor.GetPart<InventoryPart>().MaxWeight=10000;TradeSystem.SetDrams(actor,funded?10000:0);
                int purse=TradeSystem.GetDrams(actor);Assert.AreEqual(funded,TradeSystem.BuyFromTrader(actor,owner,item));
                Assert.AreEqual(funded,actor.GetPart<InventoryPart>().Objects.Contains(item));Assert.AreEqual(!funded,Items(owner).Contains(item));
                Assert.AreEqual(funded,TradeSystem.GetDrams(actor)<purse);
            }
        }
        [TestCase("sodden")][TestCase("cinderhold")][TestCase("counter")]
        public void LocalEvidenceNamesTheSpecialtyItsTradeoffAndAssemblyInputs(string source)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);Build(source,scope.Factory,64,out var zone,out _);string text=Text(zone).ToLowerInvariant();
                StringAssert.Contains(source=="sodden"?"mallet":source=="cinderhold"?"cinderhook":"counterweight",text);
                StringAssert.Contains("haft",text);StringAssert.Contains("binding",text);
                StringAssert.Contains(source=="sodden"?"armor":source=="cinderhold"?"inaccurate":"penetration",text);
                if(source=="sodden"){StringAssert.Contains("electric",text);StringAssert.Contains("hand",text);}
                if(source=="cinderhold"){StringAssert.Contains("heat",text);StringAssert.Contains("slow",text);}
            }
        }
        [TestCase("sodden")][TestCase("cinderhold")][TestCase("counter")]
        public void AcquiredSpecialtiesStayAbsentAfterNativeSaveAndRevisit(string source)
        {
            using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);var zone=manager.GetZone(Address(source));Assert.NotNull(zone);
                var owner=zone.GetAllEntities().Single(e=>source=="sodden"?e.BlueprintName=="SoddenWorksLocker":source=="cinderhold"?e.BlueprintName=="Weaponsmith":e.BlueprintName=="Chest"&&e.GetProperty("SettlementId")==Address(source));
                var item=Items(owner).SingleOrDefault(e=>e.BlueprintName==Kit(source)[0]);Assert.NotNull(item);string id=owner.ID;
                if(source=="cinderhold")Assert.IsTrue(owner.GetPart<InventoryPart>().RemoveObject(item));else Assert.IsTrue(owner.GetPart<ContainerPart>().RemoveItem(item));
                var state=GameSessionState.Capture("equipment-source-test","test",manager,null,null);GameSessionState loaded;
                using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
                var restored=loaded.ZoneManager.GetZone(Address(source)).GetAllEntities().Single(e=>e.ID==id);
                Assert.IsFalse(Items(restored).Any(e=>e.BlueprintName==Kit(source)[0]),"Loading must not re-supply a consumed discovery.");
            }
        }
    }
}
