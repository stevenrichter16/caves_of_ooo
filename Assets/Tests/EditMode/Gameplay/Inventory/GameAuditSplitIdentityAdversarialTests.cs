using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SplitIdentityLaterReferenceProbe : Part
    {
        public override string Name => "SplitIdentityLaterReferenceProbe";
        public Entity Later;
        public string AfterID, FinalID;
        public override void OnAfterLoad(SaveReader reader) => AfterID = Later.ID;
        public override void FinalizeLoad(SaveReader reader) => FinalID = Later.ID;
    }
    /// <summary>Identity boundaries, exact rollback, opaque saved values and alias preservation.</summary>
    public class GameAuditSplitIdentityAdversarialTests : SplitIdentityFixture
    {
        [TestCase(2)] [TestCase(9)] [TestCase(99)]
        public void RepeatedSplitsRemainDistinctAndConserveEveryUnit(int count)
        {
            var source = Item("Dagger"); source.GetPart<StackerPart>().StackCount = count;
            string original = source.ID; var units = new List<Entity> { source };
            while (Quantity(source) > 1) units.Add(source.GetPart<StackerPart>().SplitStack(1));
            Assert.AreEqual(count, units.Sum(Quantity)); Assert.AreEqual(count, units.Count);
            Assert.AreEqual(count, units.Select(e => e.ID).Distinct().Count());
            foreach (var unit in units.Skip(1)) Fresh(unit, source);
            Assert.AreEqual(original, source.ID);
        }

        [TestCase(false)] [TestCase(true)]
        public void CraftPreparationDoesNotReplaceIdentitySeenByInitialize(bool reforge)
        {
            var actor = Crafter(); var source = reforge ? PaidStock(actor, 2) : Units(actor, "Dagger", 2);
            source.AddPart(new SplitIdentityProbePart()); string original = source.ID;
            var payment = reforge ? Units(actor, "IronSpikeComponent", 1) : Quench(actor);
            Entity changed;
            Assert.IsTrue(reforge
                ? WeaponForgingService.TryReforge(actor, Factory, source, payment, out changed, out _, out _)
                : WeaponTemperingService.TryTemper(actor, source, payment, out changed, out _));
            Fresh(changed, source);
            Assert.AreEqual(changed.ID, changed.GetPart<SplitIdentityProbePart>().InitializedID);
            Assert.AreEqual(original, source.ID); Assert.AreEqual(1, Quantity(source));
        }

        [TestCase("equip", false)] [TestCase("equip", true)]
        [TestCase("drop", false)] [TestCase("drop", true)]
        [TestCase("throw", false)] [TestCase("throw", true)]
        public void OuterFailureRestoresExactSourceWithoutLeavingReachableClone(string action, bool rollback)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 3);
            var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray(); string id = source.ID;
            var zone = new Zone("IdentityRollback"); Assert.IsTrue(zone.AddEntity(actor, 10, 10));
            IInventoryCommand command = action == "equip" ? new EquipCommand(source)
                : action == "drop" ? new DropPartialCommand(source, 1)
                : new ThrowItemCommand(source, 12, 10, new Random(1));
            if (rollback) command = new FailAfter(command);
            var result = InventorySystem.ExecuteCommand(command, actor, zone);
            Assert.AreEqual(!rollback, result.Success); Assert.AreEqual(id, source.ID);
            Assert.AreEqual(rollback ? 3 : 2, Quantity(source));
            Assert.AreSame(actor, source.GetPart<PhysicsPart>().InInventory);
            var published = inv.GetAllEquipped().Concat(zone.GetAllEntities().Where(e => e.BlueprintName == "Dagger")).ToArray();
            Assert.AreEqual(rollback ? 0 : 1, published.Length);
            if (rollback) CollectionAssert.AreEqual(before, inv.Objects);
            else Fresh(published.Single(), source);
        }

        [TestCase(false)] [TestCase(true)]
        public void SyntheticVegetationPlacementRefusalDoesNotPublishSplit(bool barren)
        {
            var actor = Crafter(); var zone = new Zone("IdentityPlacement"); Assert.IsTrue(zone.AddEntity(actor, 10, 10));
            if (barren) Assert.IsTrue(zone.AddEntity(Item("FellingBarePosition"), 10, 10));
            var source = Item("FlowerField"); source.AddPart(new StackerPart { StackCount = 3 });
            Assert.IsFalse(source.GetPart<PhysicsPart>().Takeable, "synthetic API fixture, not ordinary pickup");
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(source)); string id = source.ID;
            Assert.AreEqual(!barren, InventorySystem.DropPartial(actor, source, 1, zone));
            Assert.AreEqual(barren ? 3 : 2, Quantity(source)); Assert.AreEqual(id, source.ID);
            var ground = zone.GetAllEntities().Where(e => e.BlueprintName == "FlowerField").ToArray();
            Assert.AreEqual(barren ? 0 : 1, ground.Length); if (!barren) Fresh(ground.Single(), source);
        }

        [TestCase(false)] [TestCase(true)]
        public void SameBlueprintWorldRowsResolveExactSplitRegardlessOfOrder(bool sourceFirst)
        {
            var source = Item("Dagger"); source.GetPart<StackerPart>().StackCount = 2;
            var split = source.GetPart<StackerPart>().SplitStack(1); var zone = new Zone("IdentityPile");
            foreach (var item in sourceFirst ? new[] { source, split } : new[] { split, source })
                Assert.IsTrue(zone.AddEntity(item, 10, 10));
            WorldPick(zone, source); WorldPick(zone, split);
            Assert.IsNull(WorldInteractionSystem.FindInCell(zone.GetCell(10, 10), "missing"));
        }

        [TestCase(false)] [TestCase(true)]
        public void MergeKeepsResidentIdentityWhileUnmergedTransferKeepsSplitIdentity(bool resident)
        {
            var donor = Crafter(); var source = Units(donor, "Dagger", 2);
            var split = source.GetPart<StackerPart>().SplitStack(1); string splitId = split.ID;
            var receiver = Crafter(); var prior = resident ? Units(receiver, "Dagger", 1) : null;
            string priorId = prior?.ID; var inv = receiver.GetPart<InventoryPart>();
            Assert.IsTrue(inv.AddObject(split)); Assert.AreEqual(1, inv.Objects.Count);
            var actual = inv.Objects.Single(); Assert.AreSame(resident ? prior : split, actual);
            Assert.AreEqual(resident ? priorId : splitId, actual.ID); Assert.AreEqual(resident ? 2 : 1, Quantity(actual));
            Assert.AreSame(receiver, actual.GetPart<PhysicsPart>().InInventory);
            Assert.AreSame(donor, source.GetPart<PhysicsPart>().InInventory); Assert.AreEqual(1, Quantity(source));
            Assert.AreEqual(splitId, split.ID);
        }

        [TestCase("  ")] [TestCase("\t")] [TestCase("Case:Sensitive")]
        [TestCase("-1")] [TestCase("00000000000000000000000000000000")] [TestCase("x/y:雪")]
        public void OpaqueNonemptyIDIsPreservedThroughBodyAndWorldResolution(string id)
        {
            var source = Item("Dagger"); source.ID = id;
            var loaded = LoadGraph(SaveGraph(source)); Assert.AreEqual(id, loaded.ID);
            var zone = new Zone("OpaqueIdentity"); Assert.IsTrue(zone.AddEntity(loaded, 10, 10)); WorldPick(zone, loaded);
        }

        [TestCase(null)] [TestCase("")]
        public void IndependentlyLoadingLegacyBytesAllocatesButResavingRepairStabilizes(string missing)
        {
            var source = Item("Dagger"); source.ID = missing; var bytes = SaveGraph(source);
            var first = LoadGraph(bytes); var second = LoadGraph(bytes);
            Fresh(first, source); Fresh(second, source); Assert.AreNotEqual(first.ID, second.ID);
            var savedRepair = SaveGraph(first); Assert.AreEqual(first.ID, LoadGraph(savedRepair).ID);
            CollectionAssert.AreEqual(bytes, SaveGraph(source));
        }

        [TestCase(null)] [TestCase("")]
        public void RepeatedRootTokensShareOneRepairAndNullReferenceRemainsNull(string missing)
        {
            var source = Item("Dagger"); source.ID = missing;
            using var stream = new MemoryStream(); var writer = new SaveWriter(stream);
            writer.WriteEntityReference(source); writer.WriteEntityReference(source); writer.WriteEntityReference(null); writer.WriteQueuedEntityBodies();
            stream.Position = 0; var reader = new SaveReader(stream, null);
            var first = reader.ReadEntityReference(); var alias = reader.ReadEntityReference(); var absent = reader.ReadEntityReference();
            Assert.AreSame(first, alias); Assert.IsNull(first.ID, "unread body placeholder is not repaired"); Assert.IsNull(absent);
            reader.ReadEntityBodies(); Fresh(first, source); Assert.AreSame(first, alias); Assert.AreEqual(first.ID, alias.ID);
        }

        [TestCase(false)] [TestCase(true)]
        public void ExistingDuplicateOpaqueIDsAreNotSilentlyRewritten(bool missing)
        {
            // Duplicate nonempty IDs are separate debt; tokens must still retain separate objects.
            var actor = Crafter(); var first = Units(actor, "Dagger", 1); var second = Units(actor, "SilverSand", 1);
            first.ID = second.ID = missing ? "" : "legacy-duplicate";
            var loaded = LoadGraph(SaveGraph(actor)).GetPart<InventoryPart>().Objects;
            Assert.AreEqual(2, loaded.Count); Assert.AreNotSame(loaded[0], loaded[1]);
            Assert.AreEqual(missing ? 2 : 1, loaded.Select(e => e.ID).Distinct().Count());
            if (!missing) Assert.IsTrue(loaded.All(e => e.ID == "legacy-duplicate"));
            else Assert.IsTrue(loaded.All(e => !string.IsNullOrEmpty(e.ID)));
        }

        [TestCase(false)] [TestCase(true)]
        public void PaidEnhancementSplitStillHasIndependentPartsAndCanMergeBack(bool enhanced)
        {
            var source = enhanced ? Infused(1) : Item("Dagger"); source.GetPart<StackerPart>().StackCount = 2;
            var split = source.GetPart<StackerPart>().SplitStack(1); Fresh(split, source); Compatible(source, split, true);
            if (enhanced) Assert.AreNotSame(source.GetPart<EnhancementPaleSalt>(), split.GetPart<EnhancementPaleSalt>());
            var inv = Crafter().GetPart<InventoryPart>(); string id = source.ID;
            Assert.IsTrue(inv.AddObject(source)); Assert.IsTrue(inv.AddObject(split));
            Assert.AreSame(source, inv.Objects.Single()); Assert.AreEqual(id, source.ID); Assert.AreEqual(2, Quantity(source));
        }

        [TestCase(null)] [TestCase("")]
        public void FullSessionRepairPreservesCellBrainEquipmentAndTurnReferences(string missing)
        {
            var previousTurns = TurnManager.Active;
            try
            {
                var actor = Crafter(); var source = Units(actor, "Dagger", 2);
                Assert.IsTrue(InventorySystem.Equip(actor, source)); var weapon = actor.GetPart<InventoryPart>().GetAllEquipped().Single();
                var npc = new Entity { ID = "audit-npc", BlueprintName = "AuditNpc" };
                npc.AddPart(new BrainPart { Target = actor });
                var zone = new Zone("Overworld.10.10.0"); Assert.IsTrue(zone.AddEntity(actor, 1, 2)); Assert.IsTrue(zone.AddEntity(npc, 4, 5));
                npc.GetPart<BrainPart>().CurrentZone = zone;
                var manager = new OverworldZoneManager(null, 12345);
                manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } }, zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
                var turns = new TurnManager();
                turns.RestoreSavedState(2, true, actor, new List<TurnManager.SavedTurnEntry> {
                    new TurnManager.SavedTurnEntry { Entity = actor, Energy = 200 },
                    new TurnManager.SavedTurnEntry { Entity = npc, Energy = 150 } });
                actor.ID = source.ID = weapon.ID = npc.ID = missing;
                var state = GameSessionState.Capture("split-identity-test", "test", manager, turns, actor);
                using var stream = new MemoryStream(); state.Save(new SaveWriter(stream)); stream.Position = 0;
                var loaded = GameSessionState.Load(new SaveReader(stream, null));
                var player = loaded.Player; var active = loaded.ZoneManager.ActiveZone;
                Assert.AreSame(player, active.GetCell(1, 2).Objects.Single());
                var other = active.GetCell(4, 5).Objects.Single(); Assert.AreSame(player, other.GetPart<BrainPart>().Target);
                var inv = player.GetPart<InventoryPart>(); Assert.AreEqual(1, inv.Objects.Count);
                var equipped = inv.GetAllEquipped().Single();
                Assert.IsTrue(player.GetPart<Body>().GetParts().Any(part => ReferenceEquals(part._Equipped, equipped)));
                Assert.AreSame(player, equipped.GetPart<PhysicsPart>().Equipped);
                Assert.AreSame(player, inv.Objects.Single().GetPart<PhysicsPart>().InInventory);
                var all = new[] { player, other, equipped, inv.Objects.Single() };
                Assert.IsTrue(all.All(e => !string.IsNullOrEmpty(e.ID))); Assert.AreEqual(4, all.Select(e => e.ID).Distinct().Count());
                Assert.AreSame(player, loaded.TurnManager.CurrentActor); Assert.IsTrue(loaded.TurnManager.WaitingForInput);
                Assert.AreEqual(2, loaded.TurnManager.TickCount); Assert.AreEqual(200, loaded.TurnManager.GetEnergy(player));
                Assert.AreEqual(150, loaded.TurnManager.GetEnergy(other)); Assert.AreEqual(missing, actor.ID);
            }
            finally { typeof(TurnManager).GetProperty("Active", BindingFlags.Public | BindingFlags.Static).SetMethod.Invoke(null, new object[] { previousTurns }); }
        }

        [Test] public void EarlierLoadHooksSeeLaterReferencedBodyAlreadyRepaired()
        {
            var first = Item("Dagger"); var later = Item("SilverSand"); later.ID = "";
            first.AddPart(new SplitIdentityLaterReferenceProbe { Later = later });
            var probe = LoadGraph(SaveGraph(first)).GetPart<SplitIdentityLaterReferenceProbe>();
            Fresh(probe.Later, later); Assert.AreEqual(probe.Later.ID, probe.AfterID); Assert.AreEqual(probe.Later.ID, probe.FinalID);
        }

        [Test] public void BareBodyRepairsPrimaryButLeavesUnreadOwnerPlaceholder()
        {
            var actor = Crafter(); var item = Units(actor, "Dagger", 1); item.ID = "";
            var loaded = PartRoundTripHelper.RoundTripEntity(item); Fresh(loaded, item);
            var owner = loaded.GetPart<PhysicsPart>().InInventory; Assert.NotNull(owner);
            Assert.IsNull(owner.ID); Assert.IsEmpty(owner.Parts); Assert.AreEqual("", item.ID);
        }

        private sealed class FailAfter : IInventoryCommand
        {
            private readonly IInventoryCommand _inner;
            public FailAfter(IInventoryCommand inner) { _inner = inner; }
            public string Name => "SplitIdentityOuterRefusal";
            public InventoryValidationResult Validate(InventoryContext context) => _inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                var result = _inner.Execute(context, transaction);
                return result.Success ? InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Injected outer refusal.") : result;
            }
        }
    }
}
