using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GameAuditQuantityRefreshAdversarialTests
    {
        private EntityFactory _factory;
        private Entity _actor;
        private InventoryPart Inv => _actor.GetPart<InventoryPart>();
        [SetUp] public void Setup()
        {
            _factory = new EntityFactory(); _factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            FactionManager.Initialize(); Diag.ResetAll(); TinkerRecipeRegistry.ResetForTests(); TinkerRecipeRegistry.EnsureInitialized();
            _actor = Item("Player", 1, 0); _actor.GetStat("Speed").BaseValue = 100; _actor.GetStat("Speed").Penalty = 7;
            Inv.MaxWeight = -1;
        }
        [TearDown] public void Cleanup() { FactionManager.Reset(); Diag.ResetAll(); TinkerRecipeRegistry.ResetForTests(); MessageLog.Clear(); }
        private Entity Item(string bp, int count = 3, int penalty = 4)
        {
            var item = _factory.CreateEntity(bp); Assert.NotNull(item);
            var stack = item.GetPart<StackerPart>(); if (stack != null) stack.StackCount = count;
            var handling = item.GetPart<HandlingPart>(); if (handling == null) { handling = new HandlingPart(); item.AddPart(handling); }
            handling.CarryMovePenalty = penalty; return item;
        }
        private Entity Give(string bp = "PaleSalt", int count = 3, int penalty = 4)
        { var item = Item(bp, count, penalty); Assert.IsTrue(Inv.AddObject(item)); return item; }
        private static int Quantity(Entity item) => item.GetPart<StackerPart>().StackCount;
        private void Penalty(int value)
        { Assert.AreEqual(value, _actor.GetStat("Speed").Penalty); Inv.RefreshHandlingCarryPenalty(); Assert.AreEqual(value, _actor.GetStat("Speed").Penalty); }
        [TestCase("carried", false)] [TestCase("carried", true)]
        [TestCase("ground", false)] [TestCase("ground", true)]
        [TestCase("container", false)] [TestCase("container", true)]
        [TestCase("equipped", false)] [TestCase("equipped", true)]
        [TestCase("forged_owner", false)] [TestCase("forged_owner", true)]
        [TestCase("missing_physics", false)] [TestCase("missing_physics", true)]
        public void PrimitiveRefreshRequiresActualCarriedMembership(string context, bool removeOne)
        {
            var pending = Give(); var source = Item("Dagger");
            switch (context)
            {
                case "carried": Assert.IsTrue(Inv.AddObject(source)); break;
                case "ground": Assert.IsTrue(new Zone("Ground").AddEntity(source, 10, 10)); break;
                case "container": Assert.IsTrue(Item("Sack", 1, 0).GetPart<ContainerPart>().AddItem(source)); break;
                case "equipped": Inv.EquippedItems["Hand"] = source; source.GetPart<PhysicsPart>().Equipped = _actor; source.GetPart<PhysicsPart>().InInventory = _actor; break;
                case "forged_owner": source.GetPart<PhysicsPart>().InInventory = _actor; break;
                case "missing_physics": source.RemovePart(source.GetPart<PhysicsPart>()); break;
            }
            int before = _actor.GetStat("Speed").Penalty;
            pending.GetPart<StackerPart>().StackCount = 2; // Deliberately stale unrelated quantity detects a spurious owner refresh.
            var detached = removeOne ? source.GetPart<StackerPart>().RemoveOne() : source.GetPart<StackerPart>().SplitStack(1);
            Assert.AreEqual(2, Quantity(source)); Assert.AreEqual(1, Quantity(detached));
            Assert.AreEqual(context == "carried" ? 23 : before, _actor.GetStat("Speed").Penalty);
            Assert.IsNull(detached.GetPart<PhysicsPart>()?.InInventory);
            Inv.RefreshHandlingCarryPenalty(); Assert.AreEqual(context == "carried" ? 23 : 15, _actor.GetStat("Speed").Penalty);
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(3)] [TestCase(4)]
        public void InvalidSplitDoesNotRefreshUnchangedSource(int amount)
        {
            var source = Give("Dagger"); var pending = Give(); pending.GetPart<StackerPart>().StackCount = 2;
            Assert.IsNull(source.GetPart<StackerPart>().SplitStack(amount)); Assert.AreEqual(3, Quantity(source));
            Assert.AreEqual(31, _actor.GetStat("Speed").Penalty); Inv.RefreshHandlingCarryPenalty(); Penalty(27);
        }
        [TestCase(false)] [TestCase(true)] public void NoopRemovalOrFullMergeKeepsPendingTracker(bool merge)
        {
            var source = Give("Dagger", 1); var pending = Give(); pending.GetPart<StackerPart>().StackCount = 2;
            if (merge) { source.GetPart<StackerPart>().MaxStack = 1; Assert.AreEqual(0, source.GetPart<StackerPart>().MergeFrom(Item("Dagger"))); }
            else Assert.AreSame(source, source.GetPart<StackerPart>().RemoveOne());
            Assert.AreEqual(23, _actor.GetStat("Speed").Penalty); Inv.RefreshHandlingCarryPenalty(); Penalty(19);
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void MergeDiagnosticsIdentifyBothActualContributions(bool sameOwner, bool partial)
        {
            var a = Give(count: 2); a.GetPart<StackerPart>().MaxStack = 2;
            var otherActor = sameOwner ? _actor : Item("Player", 1, 0);
            if (!sameOwner) otherActor.GetStat("Speed").Penalty = 7;
            var b = Item("PaleSalt"); Assert.IsTrue(otherActor.GetPart<InventoryPart>().AddObject(b));
            a.GetPart<StackerPart>().MaxStack = partial ? 3 : 99;
            Diag.ResetAll(); Diag.SetChannel("event", true);
            Assert.IsTrue(a.GetPart<StackerPart>().CanStackWith(b)); Assert.AreEqual(partial ? 1 : 3, a.GetPart<StackerPart>().MergeFrom(b));
            var records = DiagQuery.Apply(new DiagQuery.Filter { Kind = "CarryPenaltyRefreshed" }).Records;
            Assert.AreEqual(sameOwner ? 0 : 2, records.Count);
            if (!sameOwner)
            {
                StringAssert.Contains("\"delta\":" + (partial ? 4 : 12), records.Single(r => r.ActorId == _actor.ID).PayloadJson);
                StringAssert.Contains("\"delta\":" + (partial ? -4 : -12), records.Single(r => r.ActorId == otherActor.ID).PayloadJson);
            }
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void SavedOrFreshSplitAndRemergePreservesUnrelatedPenalty(bool saved, bool removeOne)
        {
            Give(); if (saved) _actor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(_actor);
            var source = Inv.Objects.Single(); Penalty(19);
            var detached = removeOne ? source.GetPart<StackerPart>().RemoveOne() : source.GetPart<StackerPart>().SplitStack(1); Penalty(15);
            Assert.IsTrue(Inv.AddObject(detached)); Assert.AreEqual(3, Quantity(source)); Assert.AreEqual(0, Quantity(detached)); Penalty(19);
            Assert.NotNull(source.GetPart<StackerPart>().SplitStack(1)); Penalty(15);
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void QuantityChangeAndDragReleasePreserveBothContributions(bool dragging, bool removeOne)
        {
            var source = Give(); var zone = new Zone("HaulQuantity"); var load = Item("HaulBarrel", 1, 0);
            Assert.IsTrue(zone.AddEntity(_actor, 10, 10)); Assert.IsTrue(zone.AddEntity(load, 11, 10));
            if (dragging) Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(_actor, load, zone));
            int drag = _actor.GetPart<DragPart>()?.AppliedPenalty ?? 0;
            Assert.AreEqual(dragging, drag > 0);
            var detached = removeOne ? source.GetPart<StackerPart>().RemoveOne() : source.GetPart<StackerPart>().SplitStack(1);
            Assert.NotNull(detached); Penalty(15 + drag); DragSystem.Release(_actor); Penalty(15);
            DragSystem.Release(_actor); Penalty(15);
        }
        [TestCase(false)] [TestCase(true)] public void EquipCallbackSeesRefreshedCountAndVetoRestoresIt(bool veto)
        {
            var source = Give("Dagger"); var probe = new EquipProbe { Source = source, Veto = veto }; _actor.AddPart(probe);
            Assert.AreEqual(!veto, InventorySystem.Equip(_actor, source));
            Assert.AreEqual(1, probe.Calls); Assert.AreEqual(2, probe.SeenCount); Assert.AreEqual(15, probe.SeenPenalty);
            Assert.AreEqual(veto ? 3 : 2, Quantity(source)); Penalty(veto ? 19 : 15);
            Assert.NotNull(source.GetPart<StackerPart>().SplitStack(1)); Penalty(veto ? 15 : 11);
        }
        [TestCase(false)] [TestCase(true)] public void MissingOutputRestoresTinkerStackIngredientPenalty(bool missing)
        {
            var salt = Give(); TinkerRecipeRegistry.TryGetRecipe("craft_dagger", out var recipe); Assert.NotNull(recipe);
            recipe.Ingredient = "PaleSalt";
            if (_actor.GetPart<BitLockerPart>() == null) _actor.AddPart(new BitLockerPart());
            var bits = _actor.GetPart<BitLockerPart>(); bits.LearnRecipe(recipe.ID); bits.AddBits(recipe.Cost);
            if (missing) { _factory.Blueprints.Remove("Dagger"); UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "EntityFactory: unknown blueprint 'Dagger'"); }
            Assert.AreEqual(!missing, TinkeringService.TryCraft(_actor, _factory, recipe.ID, out var made, out _));
            Assert.AreEqual(missing ? 0 : 1, made.Count); Assert.AreEqual(missing ? 3 : 2, Quantity(salt));
            Assert.AreEqual(missing, bits.HasBits(recipe.Cost)); Penalty(missing ? 19 : 15);
        }
        [TestCase(false)] [TestCase(true)] public void IngredientFreePartialOutputRollbackRefreshesMergedQuantity(bool tooSmall)
        {
            var stack = Give("Dagger", 2); TinkerRecipeRegistry.TryGetRecipe("craft_dagger", out var recipe); Assert.NotNull(recipe);
            recipe.NumberMade = 2; Assert.IsTrue(string.IsNullOrEmpty(recipe.Ingredient));
            if (_actor.GetPart<BitLockerPart>() == null) _actor.AddPart(new BitLockerPart());
            var bits = _actor.GetPart<BitLockerPart>(); bits.LearnRecipe(recipe.ID); bits.AddBits(recipe.Cost);
            Inv.MaxWeight = tooSmall ? 12 : 16;
            Diag.ResetAll(); Diag.SetChannel("event", true);
            Assert.AreEqual(!tooSmall, TinkeringService.TryCraft(_actor, _factory, recipe.ID, out var made, out _));
            Assert.AreEqual(tooSmall ? 0 : 2, made.Count); Assert.AreEqual(tooSmall ? 2 : 4, Quantity(stack));
            Assert.AreEqual(tooSmall, bits.HasBits(recipe.Cost)); Penalty(tooSmall ? 15 : 23);
            var changes = DiagQuery.Apply(new DiagQuery.Filter { Kind = "CarryPenaltyRefreshed", Actor = _actor.ID }).Records;
            Assert.AreEqual(2, changes.Count, "First output must merge before second output succeeds or rollback occurs.");
            Assert.AreEqual(tooSmall ? 1 : 2, changes.Count(r => r.PayloadJson.Contains("\"delta\":4")));
            Assert.AreEqual(tooSmall ? 1 : 0, changes.Count(r => r.PayloadJson.Contains("\"delta\":-4")));
        }
        [TestCase(false)] [TestCase(true)] public void MissingSpeedAndDisabledDiagnosticsRemainSafe(bool hasSpeed)
        {
            if (!hasSpeed) _actor.Statistics.Remove("Speed");
            var source = Give(); Diag.ResetAll(); Diag.SetChannel("event", true);
            Assert.NotNull(source.GetPart<StackerPart>().SplitStack(1)); Inv.RefreshHandlingCarryPenalty();
            Assert.AreEqual(hasSpeed ? 1 : 0, DiagQuery.Count(new DiagQuery.Filter { Kind = "CarryPenaltyRefreshed" }).Count);
            if (hasSpeed) Penalty(15);
        }
        [TestCase(false)] [TestCase(true)] public void UnownedDestinationRefreshesCarriedSourceOnly(bool partial)
        {
            var source = Give(); var destination = Item("PaleSalt", 2);
            destination.GetPart<StackerPart>().MaxStack = partial ? 3 : 99;
            Assert.IsNull(destination.GetPart<PhysicsPart>().InInventory);
            Assert.IsTrue(destination.GetPart<StackerPart>().CanStackWith(source));
            Assert.AreEqual(partial ? 1 : 3, destination.GetPart<StackerPart>().MergeFrom(source));
            Assert.AreEqual(partial ? 2 : 0, Quantity(source)); Penalty(partial ? 15 : 7);
            Assert.IsTrue(Inv.Objects.Contains(source)); Assert.IsFalse(Inv.Contains(destination));
        }
        private sealed class EquipProbe : Part
        {
            public override string Name => "QuantityEquipProbe";
            public Entity Source; public bool Veto; public int Calls, SeenCount, SeenPenalty;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "BeforeEquip") return true;
                Calls++; SeenCount = Quantity(Source); SeenPenalty = ParentEntity.GetStat("Speed").Penalty; return !Veto;
            }
        }
    }
}
