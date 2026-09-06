using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SplitIdentityProbePart : Part
    {
        public override string Name=>"SplitIdentityProbe";
        public string InitializedID, LoadedID;
        public override void Initialize()=>InitializedID=ParentEntity.ID;
        public override void OnAfterLoad(SaveReader reader)=>LoadedID=ParentEntity.ID;
    }
    public abstract class SplitIdentityFixture : WeaponUnitFixture
    {
        protected static void Fresh(Entity clone,Entity source)
        {Assert.IsFalse(string.IsNullOrEmpty(clone.ID),"new item needs a nonempty ID");Assert.IsTrue(Guid.TryParseExact(clone.ID,"N",out _),"new clone/repair uses existing GUID-N identity shape");Assert.AreNotEqual(source.ID,clone.ID);}
        protected Entity PaidStock(Entity actor,int count)
        {var parts=Components(actor,count);Assert.IsTrue(WeaponForgingService.TryForgeBatch(actor,Factory,parts[0],parts[1],parts[2],count,out var made,out var units,out var reason),reason);Assert.AreEqual(count,units);var stock=made.Distinct().Single();Assert.AreEqual(count,Quantity(stock));return stock;}
        protected static Entity UnequippedUnit(Entity actor,Entity stock)
        {Assert.IsTrue(InventorySystem.Equip(actor,stock));var unit=actor.GetPart<InventoryPart>().GetAllEquipped().Single();Assert.IsTrue(InventorySystem.UnequipItem(actor,unit));Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(unit));return unit;}
        protected static void StationPick(Entity actor,Entity station,Zone zone,Entity item)
        {
            var actions=WorldInteractionSystem.GatherActions(station,actor);var action=actions.SingleOrDefault(a=>a.Command==CraftingMarkPart.ToggleCommandPrefix+item.ID);Assert.NotNull(action,"actual split item must have a station picker row");
            var go=new GameObject("Split identity input");var uiGo=new GameObject("Split identity menu");
            try{var input=go.AddComponent<InputHandler>();input.PlayerEntity=actor;input.CurrentZone=zone;input.WorldActionMenuUI=uiGo.AddComponent<WorldActionMenuUI>();typeof(InputHandler).GetMethod("ExecuteWorldActionSelection",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,new object[]{action,station,zone.GetEntityCell(station),false});Assert.IsTrue(CraftingMarkPart.IsMarked(item));}
            finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(uiGo);}
        }
        protected static void WorldPick(Zone zone,Entity item)
        {var cell=zone.GetEntityCell(item);Assert.NotNull(cell);var row=WorldInteractionSystem.BuildTargetPickerActions(cell).SingleOrDefault(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+item.ID);Assert.NotNull(row,"exact dropped entity must be listed");Assert.AreSame(item,WorldInteractionSystem.FindInCell(cell,row.Command.Substring(WorldInteractionSystem.PickTargetCommandPrefix.Length)));}
        protected static byte[] SaveGraph(Entity item)
        {using var stream=new MemoryStream();var writer=new SaveWriter(stream);writer.WriteEntityReference(item);writer.WriteQueuedEntityBodies();return stream.ToArray();}
        protected static Entity LoadGraph(byte[] bytes)
        {using var stream=new MemoryStream(bytes);var reader=new SaveReader(stream,null);var entity=reader.ReadEntityReference();reader.ReadEntityBodies();return entity;}
    }
    public class GameAuditSplitIdentityTests : SplitIdentityFixture
    {
        [TestCase(null)] [TestCase("")] [TestCase("42")] [TestCase("custom:item/1")] [TestCase("f72c8cc150b44a28a15bb5c56d40d806")] [TestCase(" ")]
        public void CloneHasIndependentIdentityBeforePartInitialization(string sourceId)
        {var source=Item("Dagger");source.ID=sourceId;source.AddPart(new SplitIdentityProbePart());var first=source.CloneForStack();var second=source.CloneForStack();Fresh(first,source);Fresh(second,source);Assert.AreNotEqual(first.ID,second.ID);Assert.AreEqual(first.ID,first.GetPart<SplitIdentityProbePart>().InitializedID);Assert.AreEqual(sourceId,source.ID);Assert.AreEqual(sourceId,source.GetPart<SplitIdentityProbePart>().InitializedID);}
        [TestCase(1)] [TestCase(2)] public void PositiveSplitGetsIDAndPreservesUnitsAndSourceOwner(int count)
        {var actor=Crafter();var source=Units(actor,"Dagger",3);string id=source.ID;var split=source.GetPart<StackerPart>().SplitStack(count);Fresh(split,source);Assert.AreEqual(3-count,Quantity(source));Assert.AreEqual(count,Quantity(split));Assert.AreEqual(id,source.ID);Assert.AreSame(actor,source.GetPart<PhysicsPart>().InInventory);Assert.IsNull(split.GetPart<PhysicsPart>().InInventory);Assert.IsNull(split.GetPart<PhysicsPart>().Equipped);}
        [TestCase(-1)] [TestCase(0)] [TestCase(3)] [TestCase(4)] public void InvalidSplitDoesNotAllocateOrChangeSource(int count)
        {var source=Item("Dagger");source.GetPart<StackerPart>().StackCount=3;string id=source.ID;Assert.IsNull(source.GetPart<StackerPart>().SplitStack(count));Assert.AreEqual(3,Quantity(source));Assert.AreEqual(id,source.ID);}
        [TestCase(1)] [TestCase(2)] public void RemoveOnePreservesSingletonIdentityOrCreatesFreshSplit(int quantity)
        {var source=Item("Dagger");source.GetPart<StackerPart>().StackCount=quantity;string id=source.ID;var one=source.GetPart<StackerPart>().RemoveOne();Assert.AreEqual(1,Quantity(one));Assert.AreEqual(quantity==1,ReferenceEquals(source,one));if(quantity>1)Fresh(one,source);Assert.AreEqual(id,source.ID);}
        [TestCase(1)] [TestCase(2)] public void ActualDaggerEquipUnequipRemainsStationSelectableAndTempersExactUnit(int count)
        {var actor=Crafter();var source=Units(actor,"Dagger",count);var forge=Item("TinkersForge");var zone=ForgeZone(actor,forge);var unit=UnequippedUnit(actor,source);Assert.IsFalse(string.IsNullOrEmpty(unit.ID),"split item must remain addressable");Assert.AreEqual(count==1,ReferenceEquals(source,unit));if(count>1)Fresh(unit,source);StationPick(actor,forge,zone,unit);if(count>1)Assert.IsFalse(CraftingMarkPart.IsMarked(source));var quench=Quench(actor);Assert.IsTrue(WeaponTemperingService.TryTemper(actor,unit,quench,out var reason),reason);Assert.AreEqual(1,TemperCount(unit));if(count>1)Assert.AreEqual(0,TemperCount(source));Assert.IsTrue(InventorySystem.Drop(actor,unit,zone));WorldPick(zone,unit);}
        [TestCase(1)] [TestCase(2)] public void PaidForgedStackEquipUnequipRemainsSelectableAndReforgesExactUnit(int count)
        {var actor=Crafter();var source=PaidStock(actor,count);var forge=Item("TinkersForge");var zone=ForgeZone(actor,forge);var unit=UnequippedUnit(actor,source);Assert.IsFalse(string.IsNullOrEmpty(unit.ID),"split item must remain addressable");StationPick(actor,forge,zone,unit);var replacement=Units(actor,"IronSpikeComponent",1);Assert.IsTrue(WeaponForgingService.TryReforge(actor,Factory,unit,replacement,out var result,out var returned,out var reason),reason);Assert.AreSame(unit,result);Assert.AreEqual("SteelBladeComponent",returned.BlueprintName);Assert.AreEqual("IronSpikeComponent",unit.GetPart<WeaponAssemblyPart>().BladeBlueprint);if(count>1){Assert.AreEqual("SteelBladeComponent",source.GetPart<WeaponAssemblyPart>().BladeBlueprint);Assert.IsFalse(CraftingMarkPart.IsMarked(source));}Assert.IsTrue(InventorySystem.Drop(actor,unit,zone));WorldPick(zone,unit);}
        [TestCase(1)] [TestCase(2)] public void PartialAndWholeDropHaveResolvableIdentity(int count)
        {var actor=Crafter();var source=Units(actor,"Dagger",2);var zone=new Zone("SplitDrop");Assert.IsTrue(zone.AddEntity(actor,10,10));string id=source.ID;Assert.IsTrue(InventorySystem.DropPartial(actor,source,count,zone));var ground=zone.GetCell(10,10).Objects.Single(e=>e.BlueprintName=="Dagger");Assert.AreEqual(count,Quantity(ground));Assert.AreEqual(count==2,ReferenceEquals(source,ground));if(count==1)Fresh(ground,source);Assert.AreEqual(id,source.ID);WorldPick(zone,ground);}
        [TestCase(1)] [TestCase(2)] public void StackAndSingletonThrowLandAsOneAddressableUnit(int quantity)
        {var actor=Crafter();var source=Units(actor,"Dagger",quantity);var zone=new Zone("SplitThrow");Assert.IsTrue(zone.AddEntity(actor,10,10));var result=InventorySystem.ExecuteCommand(new ThrowItemCommand(source,12,10,new System.Random(1)),actor,zone);Assert.IsTrue(result.Success,result.ErrorMessage);var landed=zone.GetCell(12,10).Objects.Single(e=>e.BlueprintName=="Dagger");Assert.AreEqual(1,Quantity(landed));Assert.AreEqual(quantity==1,ReferenceEquals(source,landed));if(quantity>1){Fresh(landed,source);Assert.AreEqual(1,Quantity(source));}WorldPick(zone,landed);}
        [TestCase(null)] [TestCase("")] [TestCase("42")] [TestCase("custom:item/1")] [TestCase("f72c8cc150b44a28a15bb5c56d40d806")] [TestCase(" ")]
        public void SupportedBodyLoadRepairsOnlyMissingIDsBeforeHooksAndIsStable(string id)
        {var item=Item("Dagger");item.ID=id;item.AddPart(new SplitIdentityProbePart());byte[] before=SaveGraph(item);var loaded=LoadGraph(before);if(string.IsNullOrEmpty(id))Fresh(loaded,item);else Assert.AreEqual(id,loaded.ID);Assert.AreEqual(loaded.ID,loaded.GetPart<SplitIdentityProbePart>().LoadedID);Assert.AreEqual(id,item.ID,"writer does not repair/mutate source");CollectionAssert.AreEqual(before,SaveGraph(item));var twice=PartRoundTripHelper.RoundTripEntityViaTokenGraph(loaded);Assert.AreEqual(loaded.ID,twice.ID);Assert.AreEqual(7,SaveWriter.FormatVersion);}
        [TestCase(null)] [TestCase("")] public void MissingOwnerAndItemIDsRepairWithoutBreakingCyclesAndEquipmentAliases(string id)
        {var actor=Crafter();var carried=Units(actor,"SilverSand",2);var source=Units(actor,"Dagger",2);Assert.IsTrue(InventorySystem.Equip(actor,source));var equipped=actor.GetPart<InventoryPart>().GetAllEquipped().Single();actor.ID=carried.ID=source.ID=equipped.ID=id;var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);var inv=loaded.GetPart<InventoryPart>();Assert.AreEqual(2,inv.Objects.Count);Assert.AreEqual(1,inv.GetAllEquipped().Count());var all=inv.Objects.Concat(inv.GetAllEquipped()).Distinct().ToArray();Assert.AreEqual(3,all.Length);Assert.IsTrue(all.All(e=>!string.IsNullOrEmpty(e.ID)));Assert.AreEqual(4,all.Select(e=>e.ID).Append(loaded.ID).Distinct().Count());foreach(var item in inv.Objects)Assert.AreSame(loaded,item.GetPart<PhysicsPart>().InInventory);var weapon=inv.GetAllEquipped().Single();Assert.AreSame(loaded,weapon.GetPart<PhysicsPart>().Equipped);Assert.IsTrue(loaded.GetPart<Body>().GetParts().Any(p=>ReferenceEquals(p._Equipped,weapon)));Assert.AreEqual(id,actor.ID);Assert.AreEqual(id,equipped.ID);}
        [TestCase(false)] [TestCase(true)] public void EquipVetoPreservesOriginalIdentityAndQuantity(bool veto)
        {var actor=Crafter();var source=Units(actor,"Dagger",2);string id=source.ID;if(veto)actor.AddPart(new CancelEventPart("BeforeEquip"));Assert.AreEqual(!veto,InventorySystem.Equip(actor,source));Assert.AreEqual(id,source.ID);Assert.AreEqual(veto?2:1,Quantity(source));var equipped=actor.GetPart<InventoryPart>().GetAllEquipped().ToArray();Assert.AreEqual(veto?0:1,equipped.Length);if(!veto)Fresh(equipped[0],source);}
    }
}
