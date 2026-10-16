using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Real command faults at both sides of launch. No scheduler simulation: native
    // input separately verifies that the returned committed outcome pays one turn.
    public sealed class ThrowLifecycleTests : FiftyWorldFixture
    {
        [SetUp] public void SetUpThrows()
        {
            Actor.GetStat("Strength").BaseValue = 16;
            Actor.GetStat("Agility").BaseValue = 16;
            ThrowCloneFaultPart.Callback = null;
            AsciiFxBus.Clear();
        }
        [TearDown] public void ClearThrows() { ThrowCloneFaultPart.Callback = null; AsciiFxBus.Clear(); }
        Entity Target()
        {
            var target = new Entity { BlueprintName = "ThrowLifecycleTarget" };
            target.SetTag("Creature"); target.AddPart(new PhysicsPart());
            target.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 200, Max = 200 };
            target.Statistics["AV"] = new Stat { Name = "AV", BaseValue = 0 };
            Assert.True(Zone.AddEntity(target, 16, 10)); return target;
        }
        InventoryCommandResult Throw(Entity item) => InventorySystem.ExecuteCommand(
            new ThrowItemCommand(item, 16, 10, new Random(123)), Actor, Zone);
        void Equip(Entity item)
        { Assert.True(InventorySystem.ExecuteCommand(new EquipCommand(item), Actor, Zone).Success); Assert.AreSame(Actor, item.GetPart<PhysicsPart>().Equipped); }
        void OneGroundUnit(Entity item)
        {
            Assert.False(Pack.Objects.Contains(item)); Assert.NotNull(Zone.GetEntityCell(item));
            Assert.Null(item.GetPart<PhysicsPart>().InInventory); Assert.Null(item.GetPart<PhysicsPart>().Equipped);
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(1, Zone.GetReadOnlyEntities().Count(e => ReferenceEquals(e, item)));
        }

        [Test] public void DamageObserverFailureCannotRefundOrDuplicateTheLaunchedDagger()
        {
            var target = Target(); var dagger = Carry("Dagger");
            Actor.AddPart(new ThrowCallbackPart { Event = "DamageDealt", Callback = () => throw new InvalidOperationException("after damage") });
            var result = Throw(dagger);
            Assert.Less(target.GetStatValue("Hitpoints"), 200, "The irreversible effect actually happened.");
            Assert.True(result.Success, "A committed partial throw must still tell legacy input to pay.");
            Assert.True(result.ActionCommitted); Assert.AreEqual(InventoryCommandState.CommittedPartial, result.State);
            OneGroundUnit(dagger);
        }

        [Test] public void LandingObserverFailureCannotRefundAndDoesNotSuppressLaterReceipts()
        {
            var first = Place("Signpost", 11, 11); var second = Place("Signpost", 12, 11); int later = 0;
            first.AddPart(new ThrowCallbackPart { Event = ItemLandedEvent.ID, Callback = () => throw new InvalidOperationException("receipt") });
            second.AddPart(new ThrowCallbackPart { Event = ItemLandedEvent.ID, Callback = () => later++ });
            var dagger = Carry("Dagger"); var result = Throw(dagger);
            Assert.True(result.Success); OneGroundUnit(dagger); Assert.AreEqual(1, later);
        }

        [Test] public void OuterFailureAfterInnerLaunchStillReturnsPaidSuccessAndKeepsOneUnit()
        {
            var target = Target(); var dagger = Carry("Dagger");
            var result = InventorySystem.ExecuteCommand(new FailAfterThrow(dagger), Actor, Zone);
            Assert.Less(target.GetStatValue("Hitpoints"), 200);
            Assert.True(result.Success, "The outer executor must respect the shared launch boundary.");
            Assert.True(result.ActionCommitted); Assert.AreEqual(InventoryCommandState.CommittedPartial, result.State);
            OneGroundUnit(dagger);
        }

        [TestCase("BeforeUnequip")] [TestCase("AfterUnequip")]
        public void UnequipCallbackFailureBeforeLaunchRestoresExactEquippedUnit(string eventName)
        {
            var dagger = Carry("Dagger"); Equip(dagger);
            Actor.AddPart(new ThrowCallbackPart { Event = eventName, Callback = () => throw new InvalidOperationException("unequip") });
            var result = Throw(dagger);
            Assert.False(result.Success); Assert.AreSame(Actor, dagger.GetPart<PhysicsPart>().Equipped);
            Assert.False(Pack.Objects.Contains(dagger)); Assert.Null(Zone.GetEntityCell(dagger));
        }

        [Test] public void ThrowCancelledDuringUnequipRetainsExactUnitAndDoesNotEmitFlight()
        {
            var dagger = Carry("Dagger"); Equip(dagger);
            Actor.AddPart(new ThrowCallbackPart { Event = "BeforeUnequip", Refuse = true });
            Assert.False(Throw(dagger).Success); Assert.AreSame(Actor, dagger.GetPart<PhysicsPart>().Equipped);
            var fx = AsciiFxBus.Drain();
            try { Assert.False(fx.Any(r => r.Type == AsciiFxRequestType.Projectile)); }
            finally { foreach (var request in fx) AsciiFxBus.Release(request); }
        }

        [Test] public void CloneInitializationFailureDoesNotLoseAStackUnit()
        {
            var dagger = Carry("Dagger"); dagger.GetPart<StackerPart>().StackCount = 3;
            dagger.AddPart(new ThrowCloneFaultPart());
            ThrowCloneFaultPart.Callback = clone => { if (clone != dagger) throw new InvalidOperationException("clone"); };
            Assert.False(Throw(dagger).Success);
            Assert.AreEqual(3, dagger.GetPart<StackerPart>().StackCount); Assert.True(Pack.Objects.Contains(dagger));
            Assert.IsEmpty(Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "Dagger"));
        }

        [TestCase(0)] [TestCase(-1)] public void EmptyOrNegativeStackCannotLaunch(int count)
        {
            var dagger = Carry("Dagger"); dagger.GetPart<StackerPart>().StackCount = count;
            Assert.False(Throw(dagger).Success); Assert.True(Pack.Objects.Contains(dagger)); Assert.Null(Zone.GetEntityCell(dagger));
        }

        [Test] public void StaleInventoryBackReferenceCannotLaunchSomeoneElsesItem()
        {
            var dagger = Carry("Dagger"); var foreign = new Entity(); foreign.AddPart(new InventoryPart());
            dagger.GetPart<PhysicsPart>().InInventory = foreign;
            Assert.False(Throw(dagger).Success); Assert.True(Pack.Objects.Contains(dagger)); Assert.Null(Zone.GetEntityCell(dagger));
        }

        [Test] public void ActorMovedByUnequipCallbackCannotLaunchFromCapturedOldCell()
        {
            var dagger = Carry("Dagger"); Equip(dagger);
            Actor.AddPart(new ThrowCallbackPart { Event = "AfterUnequip", Callback = () => { Zone.RemoveEntity(Actor); Zone.AddEntity(Actor, 30, 10); } });
            Assert.False(Throw(dagger).Success); Assert.AreSame(Actor, dagger.GetPart<PhysicsPart>().Equipped);
            Assert.Null(Zone.GetEntityCell(dagger)); Assert.AreEqual(30, Zone.GetEntityCell(Actor).X);
        }

        [TestCase(false)] [TestCase(true)]
        public void RemovedOrReplacedBodyDuringUnequipKeepsRefusedUnitInLiveInventory(bool replace)
        {
            var dagger = Carry("Dagger"); Equip(dagger); var oldBody = Actor.GetPart<Body>();
            Actor.AddPart(new ThrowCallbackPart { Event = "AfterUnequip", Callback = () =>
            {
                Actor.RemovePart(oldBody);
                if (replace)
                {
                    var body = new Body(); body.SetBody(CavesOfOoo.Core.Anatomy.AnatomyFactory.CreateHumanoid()); Actor.AddPart(body);
                }
            } });
            var result = Throw(dagger);
            Assert.False(result.Success); Assert.False(result.ActionCommitted);
            Assert.Null(dagger.GetPart<PhysicsPart>().Equipped, "An orphan body is not an equipment destination.");
            Assert.AreSame(Actor,dagger.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(1,Pack.Objects.Count(e => e == dagger)); Assert.Null(Zone.GetEntityCell(dagger));
            Assert.False(oldBody.GetParts().Any(p => p._Equipped == dagger));
            Assert.False(Pack.EquippedItems.ContainsValue(dagger));
        }

        [TestCase(false)] [TestCase(true)]
        public void RemovedOrReplacedInventoryDuringUnequipPreservesTheRefusedExactUnit(bool replace)
        {
            var dagger = Carry("Dagger"); Equip(dagger); var oldPack = Pack;
            Actor.AddPart(new ThrowCallbackPart { Event = "AfterUnequip", Callback = () =>
            {
                Actor.RemovePart(oldPack);
                if (replace) Actor.AddPart(new InventoryPart { MaxWeight = 200 });
            } });
            var result = Throw(dagger);
            Assert.False(result.Success); Assert.False(result.ActionCommitted);
            Assert.Null(dagger.GetPart<PhysicsPart>().Equipped);
            Assert.False(oldPack.Objects.Contains(dagger)); Assert.False(oldPack.EquippedItems.ContainsValue(dagger));
            if (replace)
            {
                Assert.AreSame(Actor,dagger.GetPart<PhysicsPart>().InInventory);
                Assert.AreEqual(1,Pack.Objects.Count(e => e == dagger)); Assert.Null(Zone.GetEntityCell(dagger));
            }
            else
            {
                Assert.Null(dagger.GetPart<PhysicsPart>().InInventory);
                Assert.AreSame(Zone.GetEntityCell(Actor),Zone.GetEntityCell(dagger),"With no surviving inventory, retain the unit at the thrower's feet.");
            }
            Assert.AreEqual(1,dagger.GetPart<StackerPart>().StackCount);
        }

        [Test] public void NestedThrowDuringSplitCannotTakeAnotherUnitOfClaimedSource()
        {
            var dagger = Carry("Dagger"); dagger.GetPart<StackerPart>().StackCount = 3;
            dagger.AddPart(new ThrowCloneFaultPart()); InventoryCommandResult nested = null;
            ThrowCloneFaultPart.Callback = clone =>
            {
                if (clone == dagger) return;
                ThrowCloneFaultPart.Callback = null;
                nested = Throw(dagger);
            };
            Assert.True(Throw(dagger).Success); Assert.NotNull(nested); Assert.False(nested.Success);
            Assert.AreEqual(2, dagger.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(1, Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "Dagger"));
        }

        [Test] public void SaveRequestedDuringUnequipDoesNotCaptureTransientCustody()
        {
            using (var saves = new NewGameSaveFixture())
            {
                var dagger = Carry("Dagger"); Equip(dagger); int capturesDuring = -1;
                Actor.AddPart(new ThrowCallbackPart { Event = "AfterUnequip", Callback = () =>
                { SaveGameService.QuickSave(); capturesDuring = saves.Captures; } });
                Assert.True(Throw(dagger).Success); Assert.AreEqual(0, capturesDuring);
                typeof(SaveGameService).GetMethod("FlushStableRequests", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                Assert.AreEqual(1, saves.Captures, "The deferred request is captured only after stable disposition.");
            }
        }

        [Test] public void PlainThrowControlKeepsDeterministicDamageAndExactSingleUnit()
        {
            var target = Target(); var dagger = Carry("Dagger"); var result = Throw(dagger); Assert.True(result.Success);
            Assert.True(result.ActionCommitted); Assert.AreEqual(InventoryCommandState.Completed, result.State);
            Assert.Greater(200 - target.GetStatValue("Hitpoints"), 0); OneGroundUnit(dagger);
        }

        [Test] public void PostCommitRetrieverKeepsExactCustodyEvenWhenItsReceiptThrows()
        {
            var receiver = Factory.CreateEntity("Player"); Assert.True(Zone.AddEntity(receiver,16,10));
            var dagger = Carry("Dagger"); bool retrieved = false;
            receiver.AddPart(new ThrowCallbackPart { Event = ItemLandedEvent.ID, Callback = () =>
            {
                retrieved = InventorySystem.ExecuteCommand(new PickupCommand(dagger,autoEquip:false),receiver,Zone).Success;
                throw new InvalidOperationException("after retrieval");
            } });
            var result = Throw(dagger);
            Assert.True(result.Success); Assert.True(result.ActionCommitted); Assert.True(retrieved);
            Assert.False(Pack.Objects.Contains(dagger)); Assert.Null(Zone.GetEntityCell(dagger));
            Assert.AreSame(receiver,dagger.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(1,receiver.GetPart<InventoryPart>().Objects.Count(e => e == dagger));
        }

        [Test] public void ScopeAndPendingNativeDebtBothBlockCaptureAndLoadUntilExplicitSettlement()
        {
            using (var saves = new NewGameSaveFixture())
            {
                Assert.True(SaveGameService.QuickSave()); int before = saves.Captures;
                try
                {
                    using (ActionResolutionGuard.Enter())
                    {
                        Assert.True(SaveGameService.RequestQuickSave()); Assert.False(SaveGameService.QuickLoad());
                        Assert.AreEqual(before,saves.Captures);
                        ActionResolutionGuard.HoldPendingAction(Actor);
                    }
                    Assert.True(ActionResolutionGuard.IsUnstable); Assert.False(SaveGameService.FlushStableRequests());
                    Assert.AreEqual(before,saves.Captures); Assert.False(SaveGameService.QuickLoad());
                    ActionResolutionGuard.SettlePendingAction(Actor);
                    Assert.True(SaveGameService.FlushStableRequests()); SaveGameService.FlushPendingSaves();
                    Assert.AreEqual(before+1,saves.Captures); Assert.True(SaveGameService.QuickLoad());
                }
                finally { ActionResolutionGuard.SettlePendingAction(Actor); SaveGameService.FlushStableRequests(); SaveGameService.FlushPendingSaves(); }
            }
        }

        [Test] public void OrdinaryInventorySuccessDoesNotInventAWorldActionDebt()
        {
            var dagger = Place("Dagger",10,10);
            var result = InventorySystem.ExecuteCommand(new PickupCommand(dagger,autoEquip:false),Actor,Zone);
            Assert.True(result.Success); Assert.False(result.ActionCommitted);
            Assert.False(ActionResolutionGuard.IsUnstable); Assert.True(Pack.Objects.Contains(dagger));
        }

        [Test] public void DuplicateCarriedIdentityIsRejectedBeforeItCanGainGroundCustody()
        {
            var dagger = Carry("Dagger"); Pack.Objects.Add(dagger);
            var result = Throw(dagger);
            Assert.False(result.Success); Assert.False(result.ActionCommitted);
            Assert.Null(Zone.GetEntityCell(dagger)); Assert.AreEqual(2,Pack.Objects.Count(e => e == dagger));
            Assert.AreSame(Actor,dagger.GetPart<PhysicsPart>().InInventory);
        }

        [TestCase("BeforeUnequip")] [TestCase("DamageDealt")] [TestCase(ItemLandedEvent.ID)]
        public void NestedSameActorThrowCannotSpendASecondItemDuringOneAction(string phase)
        {
            Target(); var first = Carry("Dagger"); var second = Carry("ShortSword");
            if (phase == "BeforeUnequip") Equip(first);
            InventoryCommandResult nested = null; bool entered = false;
            var receiver = phase == ItemLandedEvent.ID ? Place("Signpost", 11, 11) : Actor;
            receiver.AddPart(new ThrowCallbackPart { Event = phase, Callback = () =>
            { if (entered) return; entered = true; nested = Throw(second); } });
            var result = Throw(first);
            Assert.True(entered, "The exact callback phase must be reached."); Assert.NotNull(nested);
            Assert.False(nested.Success); Assert.False(nested.ActionCommitted);
            Assert.True(result.Success); Assert.True(result.ActionCommitted); OneGroundUnit(first);
            Assert.True(Pack.Objects.Contains(second)); Assert.Null(Zone.GetEntityCell(second));
            Assert.AreSame(Actor, second.GetPart<PhysicsPart>().InInventory); Assert.AreEqual(1, second.GetPart<StackerPart>().StackCount);
            Assert.False(ActionResolutionGuard.IsUnstable);
        }

        [Test] public void NestedSameActorPreparationCannotCreateAnotherPaidAction()
        {
            var target = Target(); Zone.GetCell(16, 10).IsVisible = Zone.GetCell(16, 10).Explored = true;
            Actor.GetPart<SkillsPart>().AddSkill(new CavesOfOoo.Skills.Throwing_MeasuredThrow());
            var first = Carry("Dagger"); var second = Carry("ShortSword");
            Assert.True(ThrowingRules.ValidateMode(Actor, second, Zone, target, Zone.GetCell(16, 10), ThrowMode.Measured, false, out _));
            bool reached = false, prepared = false;
            Actor.AddPart(new ThrowCallbackPart { Event = "DamageDealt", Callback = () =>
            { reached = true; prepared = ThrowingRules.Prepare(Actor, second, Zone, target, Zone.GetCell(16, 10), out _); } });
            var result = Throw(first);
            Assert.True(reached); Assert.False(prepared); Assert.True(result.ActionCommitted);
            Assert.False(Actor.GetPart<ThrowingStatePart>().HasPreparation);
            Assert.Zero(ThrowingRules.CooldownRemaining(Actor, ThrowingRules.Measured)); Assert.True(Pack.Objects.Contains(second));
            Assert.False(ActionResolutionGuard.IsUnstable);
        }

        [Test] public void NestedSameActorRecoveryCannotMoveAndCollectDuringAThrow()
        {
            Actor.GetPart<SkillsPart>().AddSkill(new CavesOfOoo.Skills.Throwing_RecoveringStep());
            var recover = Carry("ShortSword");
            Assert.True(InventorySystem.ExecuteCommand(new ThrowItemCommand(recover, ThrowIntent.Ground(11, 10), new Random(123)), Actor, Zone).ActionCommitted);
            Assert.True(ThrowingRules.CanRecover(Actor, recover, Zone, out _), "Exact earlier throw creates a real eligible recovery mark.");
            Target(); var first = Carry("Dagger"); bool reached = false; ThrowRecoveryResult nested = default;
            Actor.AddPart(new ThrowCallbackPart { Event = "DamageDealt", Callback = () =>
            { reached = true; nested = ThrowingRules.Recover(Actor, recover, Zone); } });
            var result = Throw(first);
            Assert.True(reached); Assert.False(nested.Committed); Assert.False(nested.Collected); Assert.True(result.ActionCommitted);
            Assert.AreSame(Zone.GetCell(10, 10), Zone.GetEntityCell(Actor)); Assert.AreSame(Zone.GetCell(11, 10), Zone.GetEntityCell(recover));
            Assert.Zero(ThrowingRules.CooldownRemaining(Actor, ThrowingRules.Recovery)); Assert.False(ActionResolutionGuard.IsUnstable);
        }

        [Test] public void IndependentPickupInsideThrowDamageCallbackStillWorks()
        {
            Target(); var first = Carry("Dagger"); var supply = Place("ShortSword", 10, 11); bool reached = false, picked = false;
            Actor.AddPart(new ThrowCallbackPart { Event = "DamageDealt", Callback = () =>
            { reached = true; picked = InventorySystem.ExecuteCommand(new PickupCommand(supply, autoEquip: false), Actor, Zone).Success; } });
            Assert.True(Throw(first).ActionCommitted); Assert.True(reached); Assert.True(picked);
            Assert.AreSame(Actor, supply.GetPart<PhysicsPart>().InInventory); Assert.Null(Zone.GetEntityCell(supply));
            Assert.False(ActionResolutionGuard.IsUnstable);
        }

        [TestCase(false)] [TestCase(true)]
        public void CommittedPreparationStillReportsPaidWhenItsInformationalLogThrows(bool logThrows)
        {
            var target = Target(); Zone.GetCell(16, 10).IsVisible = Zone.GetCell(16, 10).Explored = true;
            Actor.GetPart<SkillsPart>().AddSkill(new Throwing_MeasuredThrow()); var item = Carry("Dagger");
            var previous = MessageLog.OnMessage; int observed = 0; bool prepared = false;
            try
            {
                MessageLog.OnMessage = text =>
                {
                    if (!text.Contains("prepares")) return;
                    observed++; if (logThrows) throw new InvalidOperationException("preparation receipt");
                };
                Assert.DoesNotThrow(() => prepared = ThrowingRules.Prepare(Actor, item, Zone, target, Zone.GetCell(16, 10), out _));
            }
            finally { MessageLog.OnMessage = previous; }
            Assert.AreEqual(1, observed); Assert.True(prepared, "Committed preparation must tell native input to charge.");
            Assert.True(Actor.GetPart<ThrowingStatePart>().Prepared);
            Assert.AreEqual(6, ThrowingRules.CooldownRemaining(Actor, ThrowingRules.Measured));
            Assert.True(Pack.Objects.Contains(item)); Assert.False(ActionResolutionGuard.IsUnstable);
        }

        sealed class FailAfterThrow : IInventoryCommand
        {
            readonly ThrowItemCommand inner;
            public FailAfterThrow(Entity item) { inner = new ThrowItemCommand(item, 16, 10, new Random(123)); }
            public string Name => "FailAfterThrow";
            public InventoryValidationResult Validate(InventoryContext context) => inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            { var result = inner.Execute(context, transaction); return result.Success ? InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "outer") : result; }
        }
    }
    public sealed class ThrowCallbackPart : Part
    {
        public string Event; public Action Callback; public bool Refuse;
        public override bool HandleEvent(GameEvent e) { if (e.ID != Event) return true; Callback?.Invoke(); return !Refuse; }
    }
    public sealed class ThrowCloneFaultPart : Part
    { public static Action<Entity> Callback; public override void Initialize() => Callback?.Invoke(ParentEntity); }
}
