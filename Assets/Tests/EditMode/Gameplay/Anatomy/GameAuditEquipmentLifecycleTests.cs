using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    internal sealed class EquipmentLifecycleFixture : IDisposable
    {
        readonly HotbarSaveFixture _scope=new HotbarSaveFixture(false,false);
        readonly Action<string> _oldMessage=MessageLog.OnMessage;
        readonly int _equipmentVersion=EquipmentChangeBus.GlobalVersion;
        readonly List<(FieldInfo field,object value)> _hooks=new List<(FieldInfo,object)>();
        static EntityFactory _factory;
        public readonly Entity Actor;
        public readonly Body Body;
        public readonly InventoryPart Inventory;
        public readonly Zone Zone=new Zone("Overworld.10.10.0");
        public BodyPart LeftHand=>Body.FindFreeSlot("Hand",Laterality.LEFT)??Body.GetPartsByType("Hand").First(p=>Laterality.Match(p.GetLaterality(),Laterality.LEFT));
        public BodyPart LeftArm=>Body.GetPartsByType("Arm").First(p=>Laterality.Match(p.GetLaterality(),Laterality.LEFT));
        public BodyPart RightArm=>Body.GetPartsByType("Arm").First(p=>Laterality.Match(p.GetLaterality(),Laterality.RIGHT));
        public EquipmentLifecycleFixture()
        {
            try
            {
                MessageLog.OnMessage=null;LootDropSystem.Factory=null;CorpsePart.Factory=null;
                foreach(var type in new[]{typeof(EntityVisualHooks),typeof(ZoneRenderHooks)})foreach(var field in type.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static))if(!field.IsInitOnly&&typeof(Delegate).IsAssignableFrom(field.FieldType)){_hooks.Add((field,field.GetValue(null)));field.SetValue(null,null);}
                if(_factory==null){_factory=new EntityFactory();_factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));}
                Actor=HaulLifecycleFixture.MakeActor("equipment audit");Actor.Tags["Player"]="";Actor.Statistics["Agility"]=new Stat{Name="Agility",Owner=Actor,BaseValue=14,Min=0,Max=100};
                Body=new Body();Body.SetBody(AnatomyFactory.CreateHumanoid());Actor.AddPart(Body);Inventory=new InventoryPart{MaxWeight=200};Actor.AddPart(Inventory);Assert.IsTrue(Zone.AddEntity(Actor,5,5));
            }
            catch{Dispose();throw;}
        }
        public Entity Item(string blueprint)=>_factory.CreateEntity(blueprint);
        public Entity EquipDuelistBuckler()
        {var item=Item("Buckler");Assert.IsTrue(new DuelistCutTinkerModification().Apply(item,out string reason),reason);Assert.IsTrue(Inventory.AddObject(item));Assert.IsTrue(InventorySystem.Equip(Actor,item,LeftHand));Assert.AreEqual(16,Actor.GetStatValue("Agility"));Assert.AreSame(Actor,item.GetPart<PhysicsPart>().Equipped);Assert.IsTrue(Inventory.EquippedItems.Values.Any(e=>ReferenceEquals(e,item)));return item;}
        public void Detached(Entity item)
        {Assert.IsFalse(Body.GetBody().GetParts().Any(p=>ReferenceEquals(p._Equipped,item)));Assert.IsFalse(Inventory.EquippedItems.Values.Any(e=>ReferenceEquals(e,item)));Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);}
        public void Ground(Entity item)
        {Detached(item);Assert.AreEqual((5,5),Zone.GetEntityPosition(item));Assert.IsFalse(Inventory.Objects.Contains(item));Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);}
        public GameSessionState RoundTrip()
        {var manager=new OverworldZoneManager(null,333);manager.ReplaceLoadedState(new Dictionary<string,Zone>{{Zone.ZoneID,Zone}},Zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());var turns=new TurnManager();turns.RestoreSavedState(17,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=1000}});return HotbarSaveFixture.RoundTrip(GameSessionState.Capture(Guid.NewGuid().ToString("N"),"equipment-audit",manager,turns,Actor));}
        public void Dispose()
        {for(int i=_hooks.Count-1;i>=0;i--)_hooks[i].field.SetValue(null,_hooks[i].value);MessageLog.OnMessage=_oldMessage;typeof(EquipmentChangeBus).GetProperty("GlobalVersion").GetSetMethod(true).Invoke(null,new object[]{_equipmentVersion});_scope.Dispose();}
    }
    public sealed class EquipmentLifecycleObserver : Part
    {
        public string VetoEvent;
        public Action<Entity> After;
        public int AfterCount;
        public override bool HandleEvent(GameEvent e){if(e.ID==VetoEvent)return false;if(e.ID=="AfterUnequip"){AfterCount++;After?.Invoke(e.GetParameter<Entity>("Item"));}return true;}
    }
    public class GameAuditEquipmentLifecycleTests
    {
        [TestCase(false)] [TestCase(true)]
        public void AcceptedInjuryRemovesBonusesAndCacheWhileDismemberVetoPreservesThem(bool veto)
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();f.Actor.AddPart(new EquipmentLifecycleObserver{VetoEvent=veto?"BeforeDismember":null});int version=EquipmentChangeBus.GlobalVersion;Assert.AreEqual(!veto,f.Body.Dismember(f.LeftArm,f.Zone));Assert.AreEqual(veto?16:14,f.Actor.GetStatValue("Agility"));if(veto){Assert.AreSame(item,f.LeftHand._Equipped);Assert.AreSame(f.Actor,item.GetPart<PhysicsPart>().Equipped);Assert.AreEqual(version,EquipmentChangeBus.GlobalVersion);}else{f.Ground(item);Assert.Greater(EquipmentChangeBus.GlobalVersion,version);}}}
        [Test]
        public void LosingOtherArmPreservesTheEquippedBonusAndExactLeftSlot()
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();Assert.IsTrue(f.Body.Dismember(f.RightArm,f.Zone));Assert.AreEqual(16,f.Actor.GetStatValue("Agility"));Assert.AreSame(item,f.LeftHand._Equipped);Assert.AreSame(f.Actor,item.GetPart<PhysicsPart>().Equipped);Assert.IsNull(f.Zone.GetEntityCell(item));}}
        [Test]
        public void NormalUnequipRemainsThePositiveLifecycleControl()
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();Assert.IsTrue(InventorySystem.UnequipItem(f.Actor,item));f.Detached(item);Assert.AreEqual(14,f.Actor.GetStatValue("Agility"));Assert.IsTrue(f.Inventory.Objects.Contains(item));Assert.IsNull(f.Zone.GetEntityCell(item));}}
        [Test]
        public void NoZoneDismemberReturnsExactSingletonToInventoryWithoutGhostCache()
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();Assert.IsTrue(f.Body.Dismember(f.LeftArm));f.Detached(item);Assert.AreEqual(14,f.Actor.GetStatValue("Agility"));Assert.AreEqual(1,f.Inventory.Objects.Count(e=>ReferenceEquals(e,item)));Assert.AreSame(f.Actor,item.GetPart<PhysicsPart>().InInventory);}}
        [TestCase(false)] [TestCase(true)]
        public void InvalidGroundDestinationRetainsExactItemCarried(bool unplaced)
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();if(unplaced)Assert.IsTrue(f.Zone.RemoveEntity(f.Actor));var zone=unplaced?f.Zone:new Zone("wrong-zone");Assert.IsTrue(f.Body.Dismember(f.LeftArm,zone));f.Detached(item);Assert.AreEqual(1,f.Inventory.Objects.Count(e=>ReferenceEquals(e,item)));Assert.AreSame(f.Actor,item.GetPart<PhysicsPart>().InInventory);Assert.IsNull(zone.GetEntityCell(item));Assert.AreEqual(14,f.Actor.GetStatValue("Agility"));}}
        [Test]
        public void IronshodBootsLoseArmorPenaltyButRetainLegitimateMobilityAndOtherPenalty()
        {using(var f=new EquipmentLifecycleFixture()){var boots=f.Item("IronshodBoots");Assert.AreEqual(5,boots.GetPart<ArmorPart>().SpeedPenalty);f.Actor.GetStat("Speed").Penalty=7;Assert.IsTrue(f.Inventory.AddObject(boots));var feet=f.Body.GetPartsByType("Feet")[0];Assert.IsTrue(InventorySystem.Equip(f.Actor,boots,feet));Assert.AreEqual(12,f.Actor.GetStat("Speed").Penalty);Assert.IsTrue(f.Body.Dismember(feet,f.Zone));f.Ground(boots);Assert.Greater(f.Actor.GetIntProperty("MobilityPenalty"),0);Assert.AreEqual(7+f.Actor.GetIntProperty("MobilityPenalty"),f.Actor.GetStat("Speed").Penalty);}}
        [Test]
        public void GlowQuartzUnequipsAfterInjuryAndDoesNotRemainOnGroundItem()
        {using(var f=new EquipmentLifecycleFixture()){var item=f.Item("Buckler");var glow=new EnhancementGlowQuartz();glow.ApplyTier(2);item.AddPart(glow);Assert.IsTrue(f.Inventory.AddObject(item));Assert.IsTrue(InventorySystem.Equip(f.Actor,item,f.LeftHand));Assert.IsTrue(glow.AppliedBonus);Assert.AreEqual(2,item.GetPart<LightSourcePart>().Radius);Assert.IsTrue(f.Body.Dismember(f.LeftArm,f.Zone));Assert.IsFalse(glow.AppliedBonus);Assert.AreEqual(0,item.GetPart<LightSourcePart>().Radius);f.Ground(item);}}
        [Test]
        public void ForcedUnequipPublishesSettledStateExactlyOnceAndRepeatedCleanupIsQuiet()
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();var observer=new EquipmentLifecycleObserver{After=e=>{Assert.AreSame(item,e);f.Ground(item);Assert.AreEqual(14,f.Actor.GetStatValue("Agility"));}};f.Actor.AddPart(observer);Assert.IsTrue(f.Body.Dismember(f.LeftArm,f.Zone));Assert.AreEqual(1,observer.AfterCount);int version=EquipmentChangeBus.GlobalVersion;f.Body.DropAllEquipment(f.Zone);Assert.AreEqual(1,observer.AfterCount);Assert.AreEqual(version,EquipmentChangeBus.GlobalVersion);}}
        [Test]
        public void OrdinaryUnequipVetoCannotPreventForcedLimbCleanup()
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();f.Actor.AddPart(new EquipmentLifecycleObserver{VetoEvent="BeforeUnequip"});Assert.IsFalse(InventorySystem.UnequipItem(f.Actor,item));Assert.AreEqual(16,f.Actor.GetStatValue("Agility"));Assert.IsTrue(f.Body.Dismember(f.LeftArm,f.Zone));f.Ground(item);Assert.AreEqual(14,f.Actor.GetStatValue("Agility"));}}
        [Test]
        public void DropAllEquipmentIncludesBodyAndLegacyCacheUnionWithoutLosingEither()
        {using(var f=new EquipmentLifecycleFixture()){var bodyItem=f.EquipDuelistBuckler();var legacy=f.Item("Dagger");Assert.IsTrue(f.Inventory.AddObject(legacy));Assert.IsTrue(f.Inventory.Equip(legacy,"legacy-audit"));Assert.AreEqual(2,f.Inventory.GetAllEquipped().Count);f.Body.DropAllEquipment(f.Zone);f.Ground(bodyItem);f.Ground(legacy);Assert.AreEqual(0,f.Inventory.EquippedItems.Count);Assert.AreEqual(14,f.Actor.GetStatValue("Agility"));}}
        [Test]
        public void SavedDroppedEquipmentCannotRegainOldEquippedOwner()
        {using(var f=new EquipmentLifecycleFixture()){var item=f.EquipDuelistBuckler();Assert.IsTrue(f.Body.Dismember(f.LeftArm,f.Zone));var loaded=f.RoundTrip();var saved=loaded.ZoneManager.ActiveZone.GetReadOnlyEntities().Single(e=>e.ID==item.ID);Assert.IsNull(saved.GetPart<PhysicsPart>().Equipped);Assert.IsNull(saved.GetPart<PhysicsPart>().InInventory);Assert.IsFalse(loaded.Player.GetPart<InventoryPart>().EquippedItems.Values.Contains(saved));Assert.AreEqual(14,loaded.Player.GetStatValue("Agility"));}}
    }
}
