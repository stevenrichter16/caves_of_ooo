using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    // Cross-owner and replacement-state controls, not extra feature counts.
    public sealed class SecondExplorationAdversarialTests : SecondExplorationFixture
    {
        [TearDown] public void ClearCallbacks() => FiftyWorldCreationProbe.Created = null;
        void Callback(string blueprint, Action<Entity> callback)
        {
            Factory.RegisterPartType<FiftyWorldCreationProbe>();
            Factory.Blueprints[blueprint].Parts["FiftyWorldCreationProbe"] = new Dictionary<string,string>();
            FiftyWorldCreationProbe.Created = callback;
        }
        void ValidZone(string id = "Overworld.10.10.0")
        { Zone.RemoveEntity(Actor); Zone = new Zone(id); SettlementRuntime.ActiveZone = Zone; Assert.True(Zone.AddEntity(Actor,10,10)); }
        OverworldZoneManager Manager(params Zone[] others)
        {
            var m = OverworldZoneManager.CreateDetached(Factory,64);
            var zones = others.Concat(new[]{Zone}).ToDictionary(z=>z.ZoneID);
            m.ReplaceLoadedState(zones,Zone.ZoneID,new Dictionary<string,List<ZoneConnection>>()); return m;
        }
        GameSessionState Save(OverworldZoneManager manager)
        {
            Clock.RestoreSavedState(0,true,Actor,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Actor,Energy=1000}});
            return HotbarSaveFixture.RoundTrip(GameSessionState.Capture("second-exploration","controlled exact-owner aftermath",manager,Clock,Actor));
        }
        [TestCase(false)] [TestCase(true)] public void BlockedActorCannotPayForCopyOrRecoverLamp(bool lamp)
        {
            Actor.ApplyEffect(new StunnedEffect(2)); Assert.True(Actor.GetPart<StatusEffectsPart>().IsActionBlocked());
            var source=lamp?Place("BeetleJar"):Provider("ScribeCopyServicePart","Scribe"); if(lamp)source.AddPart(new RecoverableLampPart());
            var book=Carry("WardGleamGrimoire");Carry("InkVial");Assert.False(Act(source,lamp?"RecoverLamp":"CopyVolume|"+Id(book)));
            Assert.AreEqual(100,TradeSystem.GetDrams(Actor));Assert.AreEqual(1,Count("InkVial"));Assert.AreEqual(0,Count("GrimoireCopy"));if(lamp)Assert.NotNull(Zone.GetEntityCell(source));
        }
        [TestCase(false)] [TestCase(true)] public void MalformedCopyQuantityOrAliasedIdentityCannotMintExtraGoods(bool identity)
        {
            var s=Provider("ScribeCopyServicePart","Scribe");var book=Carry("WardGleamGrimoire");Carry("InkVial");
            Callback("GrimoireCopy",made=>{if(identity)made.ID=book.ID;else made.GetPart<StackerPart>().StackCount=3;});
            Assert.False(Act(s,"CopyVolume|"+Id(book)));Assert.AreEqual(0,Count("GrimoireCopy"));Assert.AreEqual(1,Count("InkVial"));Assert.AreEqual(100,TradeSystem.GetDrams(Actor));
        }
        [Test] public void RemoteScribeHasNoPaidServiceOrCurrencyChange()
        { var s=Provider("ScribeCopyServicePart","Scribe"); Zone.MoveEntity(s,15,10); var book=Carry("WardGleamGrimoire"); Carry("InkVial"); Assert.False(Act(s,"CopyVolume|"+Id(book))); Assert.AreEqual(100,TradeSystem.GetDrams(Actor)); Assert.AreEqual(1,Count("InkVial")); }
        [Test] public void DuplicateSelectedBookIdentityCannotChooseAnotherVolume()
        { var s=Provider("ScribeCopyServicePart","Scribe"); var a=Carry("WardGleamGrimoire"); var b=Carry("DryingBreezeGrimoire"); b.ID=a.ID; Carry("InkVial"); Assert.False(Act(s,"CopyVolume|"+Id(a))); Assert.AreEqual(0,Count("GrimoireCopy")); Assert.AreEqual(1,Count("InkVial")); }
        [Test] public void ForgedCarriedBookBacklinkDoesNotBuyACopy()
        { var s=Provider("ScribeCopyServicePart","Scribe"); var a=Carry("WardGleamGrimoire"); a.GetPart<PhysicsPart>().InInventory=s; Carry("InkVial"); Assert.False(Act(s,"CopyVolume|"+Id(a))); Assert.AreEqual(100,TradeSystem.GetDrams(Actor)); }
        [Test] public void FullCopyPackRestoresPaidInkAndPurse()
        { var s=Provider("ScribeCopyServicePart","Scribe"); var a=Carry("WardGleamGrimoire"); Carry("InkVial"); Pack.MaxWeight=0; Assert.False(Act(s,"CopyVolume|"+Id(a))); Assert.AreEqual(1,Count("InkVial")); Assert.AreEqual(100,TradeSystem.GetDrams(Actor)); }
        [Test] public void CopyFactoryCannotReenterTheSameClaimedOriginal()
        {
            var s=Provider("ScribeCopyServicePart","Scribe"); var a=Carry("WardGleamGrimoire"); Carry("InkVial"); bool? nested=null;
            Callback("GrimoireCopy",_=>{if(nested==null){nested=false;nested=Act(s,"CopyVolume|"+Id(a));}});
            Assert.True(Act(s,"CopyVolume|"+Id(a))); Assert.AreEqual(false,nested); Assert.AreEqual(1,Count("GrimoireCopy")); Assert.AreEqual(95,TradeSystem.GetDrams(Actor));
        }
        [Test] public void OuterRepairFailureRestoresSameBrokenEffectAndSmithMaterial()
        {
            var smith=Provider("ArtisanRepairServicePart","Weaponsmith"); var gear=Carry("Dagger"); gear.ApplyEffect(new BrokenEffect()); var broken=gear.GetEffect<BrokenEffect>();
            var r=RepairRecipeRegistry.Get(gear.GetPart<RepairablePart>().RecipeId); var supply=Factory.CreateEntity(r.MaterialBlueprint); Assert.True(smith.GetPart<InventoryPart>().AddObject(supply));
            FailAfter(); Assert.False(Act(smith,"ArtisanRepair|"+Id(gear))); Assert.AreSame(broken,gear.GetEffect<BrokenEffect>()); Assert.True(smith.GetPart<InventoryPart>().Objects.Contains(supply)); Assert.AreEqual(100,TradeSystem.GetDrams(Actor));
        }
        [Test] public void OuterRentalReturnFailureRestoresWornBindingAndInk()
        { var desk=Provider("RentalDeskPart","Quartermaster"); var item=Rental(desk,"Dagger",true); var slot=Pack.FindEquippedBodyPart(item); var loan=item.GetPart<RentalPart>(); FailAfter(); Assert.False(Act(desk,"ReturnRental|"+Id(item))); Assert.AreSame(slot,Pack.FindEquippedBodyPart(item)); Assert.AreSame(loan,item.GetPart<RentalPart>()); Assert.AreEqual(100,RentalSystem.GetInk(Actor)); }
        [Test] public void OuterBuyoutFailureRestoresExactRentalAndPurse()
        { var desk=Provider("RentalDeskPart","Quartermaster"); var item=Rental(desk); var loan=item.GetPart<RentalPart>(); FailAfter(); Assert.False(Act(desk,"BuyRental|"+Id(item))); Assert.AreSame(loan,item.GetPart<RentalPart>()); Assert.AreEqual(100,TradeSystem.GetDrams(Actor)); }
        [Test] public void GuestClaimOuterFailureRestoresLockedUnclaimedOwner()
        { var chest=Place("WellmeetGuestLocker"); Actor.ApplyEffect(new UnderTheClothEffect{ExpiryTick=100}); FailAfter(); Assert.False(Act(chest,"ClaimGuestLocker")); Assert.True(chest.GetPart<ContainerPart>().Locked); Assert.Null(chest.GetPart<GuestLockerPart>().ClaimedBy); }
        [Test] public void HostilePatientCannotTakeBeneficialMedicine()
        { var patient=Provider("CivilianAidPart"); patient.GetStat("Hitpoints").BaseValue=1; patient.GetPart<BrainPart>().Target=Actor; var tonic=Carry("HealingTonic"); Assert.False(Act(patient,"TreatPatient|"+Id(tonic))); Assert.True(Pack.Objects.Contains(tonic)); Assert.AreEqual(1,patient.GetStatValue("Hitpoints")); }
        [Test] public void RefusedOuterAidDoesNotHealOrConsume()
        { var patient=Provider("CivilianAidPart"); patient.GetStat("Hitpoints").BaseValue=1; var tonic=Carry("HealingTonic"); FailAfter(); Assert.False(Act(patient,"TreatPatient|"+Id(tonic))); Assert.AreEqual(1,patient.GetStatValue("Hitpoints")); Assert.True(Pack.Objects.Contains(tonic)); }
        [Test] public void AcceptedHealingCannotAcquireHarmfulStatusBeforeCommit()
        { var patient=Provider("CivilianAidPart"); patient.GetStat("Hitpoints").BaseValue=1; var tonic=Carry("HealingTonic"); Actor.AddPart(new SecondExplorationAfterPart{Callback=()=>tonic.AddPart(new StatusTonicPart{EffectName="Poisoned",EffectDuration=4,EffectDamageDice="1d2"})}); Assert.True(Act(patient,"TreatPatient|"+Id(tonic))); Assert.False(patient.HasEffect<PoisonedEffect>()); Assert.AreEqual(1,patient.GetStatValue("Hitpoints"),"Changed treatment skips its benefit after committed payment."); Assert.False(Pack.Objects.Contains(tonic)); }
        [Test] public void DeathBetweenAidAndCommitPaysFiniteStockWithoutResurrection()
        { var patient=Provider("CivilianAidPart"); patient.GetStat("Hitpoints").BaseValue=1; var tonic=Carry("HealingTonic"); Actor.AddPart(new SecondExplorationAfterPart{Callback=()=>patient.GetStat("Hitpoints").BaseValue=0}); Assert.True(Act(patient,"TreatPatient|"+Id(tonic))); Assert.AreEqual(0,patient.GetStatValue("Hitpoints")); Assert.False(Pack.Objects.Contains(tonic)); }
        [Test] public void GiftOuterFailureRestoresExactDonorItemAndEmptyNpcHand()
        { var patient=Provider("CivilianEquipmentGiftPart"); var item=Carry("Dagger"); FailAfter(); Assert.False(Act(patient,"DonateEquipment|"+Id(item))); Assert.True(Pack.Objects.Contains(item)); Assert.Null(patient.GetPart<InventoryPart>().FindEquippedBodyPart(item)); Assert.AreSame(Actor,item.GetPart<PhysicsPart>().InInventory); }
        [Test] public void CourtesyOuterFailureRestoresTheActualOriginalCell()
        { var patient=Provider("CivilianCourtesyPart"); var before=Zone.GetEntityPosition(patient); FailAfter(); Assert.False(Act(patient,"StepAside")); Assert.AreEqual(before,Zone.GetEntityPosition(patient)); }
        [Test] public void OuterJarRecoveryFailureRestoresSameSeatAndOriginalLight()
        { var jar=Place("BeetleJar"); jar.AddPart(new RecoverableLampPart()); var light=jar.GetPart<LightSourcePart>(); FailAfter(); Assert.False(Act(jar,"RecoverLamp")); Assert.AreSame(light,jar.GetPart<LightSourcePart>()); Assert.AreEqual((11,10),Zone.GetEntityPosition(jar)); Assert.False(jar.GetPart<PhysicsPart>().Takeable); Assert.False(jar.HasPart<EquippablePart>()); Assert.False(Pack.Objects.Contains(jar)); }
        [Test] public void SalvageCreationCallbackCannotDoubleDismantle()
        { var trap=Place("SpikeTrap"); trap.AddPart(new TrapSalvagePart()); trap.GetPart<TrapJammingPart>().Jammed=true; bool? nested=null; Callback("IronSpikeComponent",_=>{if(nested==null){nested=false;nested=Act(trap,"SalvageJammedTrap");}}); Assert.True(Act(trap,"SalvageJammedTrap")); Assert.AreEqual(false,nested); Assert.AreEqual(1,Count("IronSpikeComponent")); }
        [TestCase(false)] [TestCase(true)] public void CargoRejectsDuplicateIdentityOrBrokenBacklink(bool backlink)
        { var c=Place("Chest"); c.AddPart(new HandlingPart{Weight=40,Carryable=false}); c.AddPart(new ContainerLoadPart()); var a=Factory.CreateEntity("Dagger"); var b=Factory.CreateEntity("LeatherArmor"); Assert.True(c.GetPart<ContainerPart>().AddItem(a)); Assert.True(c.GetPart<ContainerPart>().AddItem(b)); if(backlink)b.GetPart<PhysicsPart>().InInventory=Actor;else b.ID=a.ID; Assert.AreEqual(int.MaxValue,DragRules.WeightOf(c)); }
        [Test] public void CargoCycleCannotBecomeAFreeOrRecursiveLoad()
        { var c=Place("Chest"); c.AddPart(new HandlingPart{Weight=40,Carryable=false}); c.AddPart(new ContainerLoadPart()); c.GetPart<ContainerPart>().Contents.Add(c); Assert.AreEqual(int.MaxValue,DragRules.WeightOf(c)); }
        [Test] public void ReplacementSaveRetainsClaimedStorageAndRemovedCover()
        {
            ValidZone();var chest=Place("WellmeetGuestLocker");Actor.ApplyEffect(new UnderTheClothEffect{ExpiryTick=100});Assert.True(Act(chest,"ClaimGuestLocker"));
            var screen=Place("FrontierClothScreen",10,11);Assert.True(Act(screen,"StripClothScreen"));Actor.RemoveEffect(typeof(UnderTheClothEffect));
            var loaded=Save(Manager());var z=loaded.ZoneManager.ActiveZone;var replacement=z.GetReadOnlyEntities().Single(e=>e.ID==chest.ID);
            Assert.AreNotSame(chest,replacement);Assert.False(replacement.GetPart<ContainerPart>().IsLocked);Assert.AreSame(loaded.Player,replacement.GetPart<GuestLockerPart>().ClaimedBy);
            Assert.False(z.GetReadOnlyEntities().Any(e=>e.ID==screen.ID));Assert.AreEqual(2,loaded.Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="KnotflaxCord").Sum(Units));
        }
        [TestCase(false)] [TestCase(true)] public void ReciprocalRopeIsAtomicAcrossBothCachedGraphs(bool refusal)
        {
            ValidZone("Overworld.2.7.1");var other=new Zone("Overworld.2.7.2");var a=Place("RopeAnchor");var b=Factory.CreateEntity("RopeAnchor");Assert.True(other.AddEntity(b,11,10));
            a.AddPart(new RopeShortcutPart{ZoneID=Zone.ZoneID,OtherZoneID=other.ZoneID,X=11,Y=10,OtherX=11,OtherY=10,Down=true});
            b.AddPart(new RopeShortcutPart{ZoneID=other.ZoneID,OtherZoneID=Zone.ZoneID,X=11,Y=10,OtherX=11,OtherY=10,Down=false});var manager=Manager(other);Carry("KnotflaxCord");Carry("KnotflaxCord");if(refusal)FailAfter();
            Assert.AreEqual(!refusal,Act(a,"RigRopeShortcut"));Assert.AreEqual(!refusal,a.HasPart<StairsDownPart>());Assert.AreEqual(!refusal,b.HasPart<StairsUpPart>());Assert.AreEqual(!refusal,a.HasTag("StairsDown"));Assert.AreEqual(!refusal,b.HasTag("StairsUp"));Assert.AreEqual(refusal?2:0,Count("KnotflaxCord"));
            Assert.AreEqual(refusal?0:2,manager.GetConnections(Zone.ZoneID).Count);Assert.AreEqual(refusal?0:2,manager.GetConnections(other.ZoneID).Count);
            Assert.AreEqual(refusal?0:1,manager.GetConnections(Zone.ZoneID).Count(c=>c.SourceZoneID==Zone.ZoneID));Assert.AreEqual(refusal?0:1,manager.GetConnections(other.ZoneID).Count(c=>c.SourceZoneID==other.ZoneID));
            if(!refusal){var loaded=Save(manager);Assert.AreEqual(2,loaded.ZoneManager.GetConnections(Zone.ZoneID).Count);Assert.AreEqual(1,loaded.ZoneManager.GetConnections(Zone.ZoneID).Count(c=>c.SourceZoneID==Zone.ZoneID));Assert.True(loaded.ZoneManager.GetZone(other.ZoneID).GetReadOnlyEntities().Single(e=>e.ID==b.ID).GetPart<RopeShortcutPart>().Installed);}
        }
        [TestCase(false)] [TestCase(true)] public void PassagePermissionEndsAtActualExitOrLostPhysicalPost(bool movedPost)
        {
            var guard=Provider("LocalPassagePermitPart");var post=Place("SoddenPassagePost",12,9);var role=new SpreadTerritoryPart();guard.AddPart(role);Assert.True(role.Configure(Zone,post,11,9,14,11,2));
            Assert.True(Act(guard,"PermitPassage"));var permit=guard.GetPart<LocalPassagePermitPart>();Assert.True(permit.Allows(Actor,Zone));
            if(movedPost){Zone.MoveEntity(post,13,9);Assert.False(permit.Allows(Actor,Zone));}
            else {Assert.True(MovementSystem.TryMove(Actor,Zone,1,1));Assert.True(Actor.GetPart<LocalPassageTokenPart>().Entered);Assert.True(MovementSystem.TryMove(Actor,Zone,-1,0));Assert.True(Actor.GetPart<LocalPassageTokenPart>().Spent);Assert.False(permit.Allows(Actor,Zone));}
            Assert.AreEqual(96,TradeSystem.GetDrams(Actor));
        }
    }
    public sealed class SecondExplorationAfterPart:Part
    { public Action Callback; public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Callback?.Invoke();return true;} }
}
