using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class GameAuditLoadoutAdversarialTests
    {
        static List<Entity> Items(Entity actor) => actor.GetPart<InventoryPart>().Objects.Concat(actor.GetPart<InventoryPart>().EquippedItems.Values).Distinct().ToList();
        static GameSessionState RoundTrip(Entity actor)
        {
            var zone = new Zone("Overworld.10.10.0"); Assert.IsTrue(zone.AddEntity(actor, 5, 5));
            var manager = new OverworldZoneManager(null, 333);
            manager.ReplaceLoadedState(new Dictionary<string,Zone>{{zone.ZoneID,zone}},zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());
            var turns = new TurnManager(); turns.RestoreSavedState(17,true,actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=actor,Energy=1000}});
            return HotbarSaveFixture.RoundTrip(GameSessionState.Capture(Guid.NewGuid().ToString("N"),"loadout audit",manager,turns,actor));
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualDuelistProducerRunsThroughEquipAndPick(bool pick)
        {
            using(var f=new LoadoutLifecycleFixture(equip:pick?"":"Buckler",pick:pick?"1;Buckler":""))
            {f.Mod("Buckler","Duelist");var actor=f.Create();var item=LoadoutLifecycleFixture.OnlyItem(actor);Assert.IsTrue(item.HasTag("ModDuelistCut"));LoadoutLifecycleFixture.Equipped(actor,item);Assert.AreEqual(16,actor.GetStatValue("Agility"));Assert.AreEqual(1,actor.GetPart<LoadoutAuditProbe>().AfterCount);Assert.IsTrue(InventorySystem.UnequipItem(actor,item));Assert.AreEqual(14,actor.GetStatValue("Agility"));}
        }
        [Test]
        public void MissingBonusStatDoesNotManufactureOrSubtractOne()
        {
            using(var f=new LoadoutLifecycleFixture("Buckler"))
            {f.Mod("Buckler","Duelist");f.ActorBlueprint.Stats.Remove("Agility");var actor=f.Create();var item=LoadoutLifecycleFixture.OnlyItem(actor);LoadoutLifecycleFixture.Equipped(actor,item);Assert.IsNull(actor.GetStat("Agility"));Assert.IsTrue(InventorySystem.UnequipItem(actor,item));Assert.IsNull(actor.GetStat("Agility"));Assert.AreEqual(100,actor.GetStatValue("Speed"));}
        }
        [TestCase(-2)] [TestCase(0)] [TestCase(2)]
        public void ConfiguredSignedBonusAppliesAndReverses(int amount)
        {
            using(var f=new LoadoutLifecycleFixture("Dagger"))
            {f.Factory.Blueprints["Dagger"].Parts["Equippable"]["EquipBonuses"]="Agility:"+amount;var actor=f.Create();var item=LoadoutLifecycleFixture.OnlyItem(actor);Assert.AreEqual(14+amount,actor.GetStatValue("Agility"));Assert.IsTrue(InventorySystem.UnequipItem(actor,item));Assert.AreEqual(14,actor.GetStatValue("Agility"));}
        }
        [TestCase("Dagger",1)] [TestCase("Greatsword",2)]
        public void GlowAddsOnceAcrossSlotsAndPreservesIntrinsicRadius(string blueprint,int slots)
        {
            using(var f=new LoadoutLifecycleFixture(blueprint))
            {f.Mod(blueprint,"Glow");f.Factory.Blueprints[blueprint].Parts["LightSource"]=new Dictionary<string,string>{{"Radius","3"}};var actor=f.Create();var item=LoadoutLifecycleFixture.OnlyItem(actor);LoadoutLifecycleFixture.Equipped(actor,item,slots);Assert.AreEqual(5,item.GetPart<LightSourcePart>().Radius);Assert.AreEqual(1,actor.GetPart<LoadoutAuditProbe>().AfterCount);Assert.IsTrue(InventorySystem.UnequipItem(actor,item));Assert.AreEqual(3,item.GetPart<LightSourcePart>().Radius);Assert.IsFalse(item.GetPart<EnhancementGlowQuartz>().AppliedBonus);}
        }
        [TestCase(false)] [TestCase(true)]
        public void ConfiguredLacqueredPayloadActivatesOnlyWhenEquipped(bool carry)
        {
            using(var f=new LoadoutLifecycleFixture(equip:carry?"":"Buckler",carry:carry?"Buckler":""))
            {f.Factory.Blueprints["Buckler"].Parts[nameof(EnhancementLacquered)]=new Dictionary<string,string>{{"Tier","2"},{"AvBonus","2"}};var actor=f.Create();var item=LoadoutLifecycleFixture.OnlyItem(actor);Assert.AreEqual(carry?1:3,item.GetPart<ArmorPart>().AV);Assert.AreEqual(!carry,item.GetPart<EnhancementLacquered>().AppliedBonus);if(!carry)Assert.IsTrue(InventorySystem.UnequipItem(actor,item));Assert.AreEqual(1,item.GetPart<ArmorPart>().AV);}
        }
        [TestCase("inventory")] [TestCase("factory")]
        public void MissingGrantDependencyRemainsQuiet(string missing)
        {
            using(var f=new LoadoutLifecycleFixture("IronshodBoots"))
            {if(missing=="inventory")f.ActorBlueprint.Parts.Remove("Inventory");else LoadoutPart.Factory=null;var actor=f.Create();Assert.AreEqual(0,actor.GetPart<LoadoutAuditProbe>().AfterCount);Assert.AreEqual(0,actor.GetStat("Speed").Penalty);if(missing=="factory")Assert.AreEqual(0,Items(actor).Count);}
        }
        [Test]
        public void EmptyLoadoutDoesNotNotifyEquipmentOrEmitHooks()
        {using(var f=new LoadoutLifecycleFixture()){int version=EquipmentChangeBus.GlobalVersion;var actor=f.Create();Assert.AreEqual(0,Items(actor).Count);Assert.AreEqual(0,actor.GetPart<LoadoutAuditProbe>().BeforeCount);Assert.AreEqual(version,EquipmentChangeBus.GlobalVersion);}}
        [TestCase("BeforeEquip")] [TestCase("AfterEquip")]
        public void ThrowingEquipHookRetainsItsGrantAndLaterIndependentSuccess(string phase)
        {
            using(var f=new LoadoutLifecycleFixture("IronshodBoots;Dagger"))
            {f.Mod("Dagger","Glow");LoadoutAuditProbe.AuditHook=(probe,e)=>{if(e.ID==phase&&e.GetParameter<Entity>("Item")?.BlueprintName=="IronshodBoots")throw new InvalidOperationException("audit hook");return true;};var actor=f.Create();var boots=Items(actor).Single(e=>e.BlueprintName=="IronshodBoots");var dagger=Items(actor).Single(e=>e.BlueprintName=="Dagger");LoadoutLifecycleFixture.Carried(actor,boots);LoadoutLifecycleFixture.Equipped(actor,dagger);Assert.AreEqual(0,actor.GetStat("Speed").Penalty);Assert.IsTrue(dagger.GetPart<EnhancementGlowQuartz>().AppliedBonus);}
        }
        // Hypothesis: a pre-BeforeEquip plan is stale if its sole Feet node
        // becomes occupied or detached by an independently committed callback.
        [TestCase("unchanged")] [TestCase("occupied")] [TestCase("detached")]
        public void AutoEquipRevalidatesDestinationAfterBeforeHook(string change)
        {
            using(var f=new LoadoutLifecycleFixture("IronshodBoots"))
            {
                bool entered=false,injectedEquipped=false,detachedSuccess=false;Entity injected=null;CavesOfOoo.Core.Anatomy.BodyPart feet=null;
                LoadoutAuditProbe.AuditHook=(probe,e)=>{
                    if(e.ID!="BeforeEquip"||entered)return true;entered=true;var actor=probe.ParentEntity;feet=actor.GetPart<Body>().GetPartsByType("Feet").Single();
                    if(change=="occupied"){injected=f.Factory.CreateEntity("LeatherBoots");Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(injected));injectedEquipped=InventorySystem.Equip(actor,injected,feet);}
                    if(change=="detached")detachedSuccess=actor.GetPart<Body>().Dismember(feet);
                    return true;
                };
                var result=f.Create();Assert.IsTrue(entered);if(change=="occupied")Assert.IsTrue(injectedEquipped,"Independent different-blueprint boots must equip before evaluating the outer grant.");
                if(change=="detached"){Assert.IsTrue(detachedSuccess,"The callback must actually remove Feet before evaluating the outer grant.");Assert.IsNull(feet.ParentPart);Assert.IsFalse(result.GetPart<Body>().GetParts().Contains(feet));}
                var granted=Items(result).Single(e=>!ReferenceEquals(e,injected));
                if(change=="unchanged"){LoadoutLifecycleFixture.Equipped(result,granted);Assert.AreEqual(5,result.GetStat("Speed").Penalty);}
                else{LoadoutLifecycleFixture.Carried(result,granted);if(change=="occupied"){LoadoutLifecycleFixture.Equipped(result,injected);Assert.AreEqual(0,injected.GetPart<ArmorPart>().SpeedPenalty);Assert.AreEqual(0,result.GetStat("Speed").Penalty);}else{Assert.IsNull(feet._Equipped);Assert.AreEqual(result.GetIntProperty("MobilityPenalty"),result.GetStat("Speed").Penalty);}}
            }
        }
        [Test]
        public void RefusedHeavyMiddleGrantPreservesEarlierAndLaterEquipment()
        {
            using(var f=new LoadoutLifecycleFixture("Buckler;IronshodBoots;Dagger"))
            {f.Mod("Buckler","Duelist");f.Factory.Blueprints["IronshodBoots"].Parts["Physics"]["Weight"]="999";var actor=f.Create();Assert.AreEqual(2,Items(actor).Count);Assert.IsFalse(Items(actor).Any(e=>e.BlueprintName=="IronshodBoots"));Assert.AreEqual(16,actor.GetStatValue("Agility"));Assert.AreEqual(2,actor.GetPart<LoadoutAuditProbe>().AfterCount);foreach(var item in Items(actor))LoadoutLifecycleFixture.Equipped(actor,item);}
        }
        [Test]
        public void FullyMergedIncomingGrantCannotBecomeGhostEquipment()
        {
            using(var f=new LoadoutLifecycleFixture("Dagger"))
            {Entity resident=null;LoadoutAuditProbe.AuditHook=(probe,e)=>{if(e.ID=="ObjectCreated"&&resident==null){resident=f.Factory.CreateEntity("Dagger");resident.GetPart<StackerPart>().StackCount=2;Assert.IsTrue(probe.ParentEntity.GetPart<InventoryPart>().AddObject(resident));}return true;};var actor=f.Create();Assert.AreSame(resident,LoadoutLifecycleFixture.OnlyItem(actor));Assert.AreEqual(3,resident.GetPart<StackerPart>().StackCount);LoadoutLifecycleFixture.Carried(actor,resident);Assert.AreEqual(0,actor.GetPart<LoadoutAuditProbe>().AfterCount);}
        }
        [TestCase(1)] [TestCase(2)]
        public void AuthoredMultiUnitStackUsesNormalAutoEquipRefusal(int count)
        {
            using(var f=new LoadoutLifecycleFixture("Dagger"))
            {f.Factory.Blueprints["Dagger"].Parts["Stacker"]["StackCount"]=count.ToString();var actor=f.Create();var item=LoadoutLifecycleFixture.OnlyItem(actor);Assert.AreEqual(count,item.GetPart<StackerPart>().StackCount);if(count==1)LoadoutLifecycleFixture.Equipped(actor,item);else LoadoutLifecycleFixture.Carried(actor,item);Assert.AreEqual(count==1?1:0,actor.GetPart<LoadoutAuditProbe>().AfterCount);}
        }
        [TestCase(0)] [TestCase(100)]
        public void ExplicitEquipChanceRetainsExistingSourceContract(int chance)
        {using(var f=new LoadoutLifecycleFixture("IronshodBoots:"+chance)){var actor=f.Create();Assert.AreEqual(chance==0?0:1,Items(actor).Count);Assert.AreEqual(chance==0?0:5,actor.GetStat("Speed").Penalty);}}
        [Test]
        public void SavedFactoryLoadoutRestoresAliasesWithoutRegrantAndUnequipsOnce()
        {
            using(var f=new LoadoutLifecycleFixture("IronshodBoots;Buckler;Dagger"))
            {f.Mod("Buckler","Duelist");f.Mod("Dagger","Glow");var actor=f.Create();var ids=Items(actor).Select(e=>e.ID).OrderBy(id=>id).ToArray();var loaded=RoundTrip(actor).Player;Assert.AreNotSame(actor,loaded);CollectionAssert.AreEqual(ids,Items(loaded).Select(e=>e.ID).OrderBy(id=>id).ToArray());Assert.AreEqual(3,loaded.GetPart<LoadoutAuditProbe>().AfterCount);Assert.AreEqual(16,loaded.GetStatValue("Agility"));Assert.AreEqual(5,loaded.GetStat("Speed").Penalty);foreach(var item in Items(loaded)){LoadoutLifecycleFixture.Equipped(loaded,item);Assert.IsTrue(InventorySystem.UnequipItem(loaded,item));}Assert.AreEqual(14,loaded.GetStatValue("Agility"));Assert.AreEqual(0,loaded.GetStat("Speed").Penalty);Assert.IsFalse(Items(loaded).Single(e=>e.BlueprintName=="Dagger").GetPart<EnhancementGlowQuartz>().AppliedBonus);}
        }
        [TestCase(false)] [TestCase(true)]
        public void LoadoutBootsComposeWithForcedInjuryAndRealDeath(bool death)
        {
            using(var f=new LoadoutLifecycleFixture("IronshodBoots"))
            {var actor=f.Create();var item=LoadoutLifecycleFixture.OnlyItem(actor);Assert.AreEqual(5,actor.GetStat("Speed").Penalty);var zone=new Zone("audit");Assert.IsTrue(zone.AddEntity(actor,5,5));if(death)CombatSystem.ApplyDamage(actor,20,null,zone);else Assert.IsTrue(actor.GetPart<Body>().Dismember(actor.GetPart<Body>().GetPartsByType("Feet").Single(),zone));Assert.AreEqual((5,5),zone.GetEntityPosition(item));Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);Assert.AreEqual(actor.GetIntProperty("MobilityPenalty"),actor.GetStat("Speed").Penalty);Assert.AreEqual(0,actor.GetPart<InventoryPart>().EquippedItems.Count);}
        }
        [Test]
        public void ReplayedCreationRetainsExistingGrantAgainCompatibility()
        {using(var f=new LoadoutLifecycleFixture("IronshodBoots")){var actor=f.Create();var first=LoadoutLifecycleFixture.OnlyItem(actor);actor.FireEventAndRelease(GameEvent.New("ObjectCreated"));Assert.AreEqual(2,Items(actor).Count);LoadoutLifecycleFixture.Equipped(actor,first);Assert.AreEqual(5,actor.GetStat("Speed").Penalty);Assert.AreEqual(1,actor.GetPart<InventoryPart>().Objects.Count);}}
        [Test]
        public void UnavailableHandKeepsTwoHandGrantCarriedWithoutDisplacement()
        {using(var f=new LoadoutLifecycleFixture("Dagger;Greatsword")){var actor=f.Create();LoadoutLifecycleFixture.Equipped(actor,Items(actor).Single(e=>e.BlueprintName=="Dagger"));LoadoutLifecycleFixture.Carried(actor,Items(actor).Single(e=>e.BlueprintName=="Greatsword"));Assert.AreEqual(1,actor.GetPart<LoadoutAuditProbe>().AfterCount);}}
        [TestCase("Humanoid")] [TestCase("Quadruped")]
        public void EntityAnatomyPropertyControlsCompatibility(string anatomy)
        {using(var f=new LoadoutLifecycleFixture("Dagger")){f.ActorBlueprint.Props["Anatomy"]=anatomy;var actor=f.Create();var item=LoadoutLifecycleFixture.OnlyItem(actor);if(anatomy=="Humanoid")LoadoutLifecycleFixture.Equipped(actor,item);else LoadoutLifecycleFixture.Carried(actor,item);}}
        [TestCase(false)] [TestCase(true)]
        public void AfterEquipForcedRemovalCannotReactivateEnhancementOrReverseBonusTwice(bool remove)
        {
            using(var f=new LoadoutLifecycleFixture("Dagger"))
            {
                f.Mod("Dagger","Glow");
                f.Factory.Blueprints["Dagger"].Parts["Equippable"]["EquipBonuses"]="Agility:2";
                bool removed=false;
                LoadoutAuditProbe.AuditHook=(probe,e)=>{
                    if(remove&&e.ID=="AfterEquip")
                    {
                        var item=e.GetParameter<Entity>("Item");var body=probe.ParentEntity.GetPart<Body>();
                        removed=body.Dismember(body.GetParts().Single(p=>ReferenceEquals(p._Equipped,item)));
                    }
                    return true;
                };
                var actor=f.Create();var granted=LoadoutLifecycleFixture.OnlyItem(actor);
                Assert.AreEqual(remove,removed);
                Assert.AreEqual(remove?14:16,actor.GetStatValue("Agility"));
                if(remove)LoadoutLifecycleFixture.Carried(actor,granted);else LoadoutLifecycleFixture.Equipped(actor,granted);
                Assert.AreEqual(!remove,granted.GetPart<EnhancementGlowQuartz>().AppliedBonus);
                Assert.AreEqual(remove?0:2,granted.GetPart<LightSourcePart>()?.Radius??0);
            }
        }
        [Test]
        public void ChangedPlanUsesAnotherFreeHandWithoutDisplacingIndependentGear()
        {
            using(var f=new LoadoutLifecycleFixture("Dagger"))
            {
                f.Mod("Buckler","Duelist");Entity buckler=null;bool entered=false,independentEquipped=false;
                LoadoutAuditProbe.AuditHook=(probe,e)=>{
                    if(e.ID!="BeforeEquip"||entered)return true;
                    entered=true;buckler=f.Factory.CreateEntity("Buckler");
                    Assert.IsTrue(probe.ParentEntity.GetPart<InventoryPart>().AddObject(buckler));
                    independentEquipped=InventorySystem.Equip(probe.ParentEntity,buckler);
                    return true;
                };
                var actor=f.Create();Assert.IsTrue(independentEquipped);
                LoadoutLifecycleFixture.Equipped(actor,buckler);
                LoadoutLifecycleFixture.Equipped(actor,Items(actor).Single(e=>e.BlueprintName=="Dagger"));
                Assert.AreEqual(16,actor.GetStatValue("Agility"));Assert.AreEqual(0,actor.GetPart<InventoryPart>().Objects.Count);
            }
        }
        [Test]
        public void CarryOnlyDoesNotEmitAnEquipAttemptReceipt()
        {
            using(var f=new LoadoutLifecycleFixture(carry:"IronshodBoots"))
            {
                bool old=Diag.IsChannelEnabled("event");Diag.SetChannel("event",true);
                try
                {
                    LoadoutAuditProbe.AuditHook=(probe,e)=>{if(e.ID=="ObjectCreated")probe.ParentEntity.ID="GA03g-"+Guid.NewGuid().ToString("N");return true;};
                    var actor=f.Create();LoadoutLifecycleFixture.Carried(actor,LoadoutLifecycleFixture.OnlyItem(actor));
                    Assert.AreEqual(0,DiagQuery.Count(new DiagQuery.Filter{Category="event",Kind="LoadoutEquipResult",Actor=actor.ID}).Count);
                }
                finally{Diag.SetChannel("event",old);}
            }
        }
        [TestCase("equipped")] [TestCase("auto_equip_refused")] [TestCase("missing_body")] [TestCase("removed_during_equip")]
        public void LoadoutResultDiagnosticRecordsExactOutcome(string reason)
        {
            using(var f=new LoadoutLifecycleFixture("IronshodBoots",body:reason!="missing_body",mode:reason=="auto_equip_refused"?"Veto":""))
            {
                bool old=Diag.IsChannelEnabled("event");Diag.SetChannel("event",true);
                try{LoadoutAuditProbe.AuditHook=(probe,e)=>{if(e.ID=="ObjectCreated")probe.ParentEntity.ID="GA03g-"+Guid.NewGuid().ToString("N");if(e.ID=="AfterEquip"&&reason=="removed_during_equip")Assert.IsTrue(probe.ParentEntity.GetPart<Body>().Dismember(probe.ParentEntity.GetPart<Body>().GetPartsByType("Feet").Single()));return true;};var actor=f.Create();var item=LoadoutLifecycleFixture.OnlyItem(actor);var query=DiagQuery.Apply(new DiagQuery.Filter{Category="event",Kind="LoadoutEquipResult",Actor=actor.ID,Target=item.ID,Limit=10});Assert.AreEqual(1,query.Records.Count);StringAssert.Contains(reason,query.Records[0].PayloadJson);StringAssert.Contains("IronshodBoots",query.Records[0].PayloadJson);StringAssert.Contains(reason=="equipped"?"\"equipped\":true":"\"equipped\":false",query.Records[0].PayloadJson);}
                finally{Diag.SetChannel("event",old);}
            }
        }
    }
}
