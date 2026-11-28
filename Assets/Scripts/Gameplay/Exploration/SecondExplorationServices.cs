using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core
{
    public sealed class ScribeCopyServicePart : SecondExplorationServicePart
    {
        public override string Name => "ScribeCopyService";
        protected override string[] Verbs => new[] { "CopyVolume" };
        static bool Original(Entity actor, Entity book) => SecondExplorationActions.Carried(actor, book)
            && !SecondExplorationActions.Special(book) && book.BlueprintName != "GrimoireCopy" && book.HasTag("Grimoire")
            && book.GetPart<GrimoirePart>() is GrimoirePart g && (!string.IsNullOrEmpty(g.SkillClassName) || !string.IsNullOrEmpty(g.KnowledgeProperty));
        bool FreshCopy(Entity copy, Entity actor, Zone zone)
        {
            if (!SecondExplorationActions.Fresh(copy, "GrimoireCopy") || SecondExplorationActions.Units(copy) != 1
                || copy.GetPart<GrimoirePart>() == null || copy.GetPart<RenderPart>() == null) return false;
            // Save identity must be fresh throughout the current physical owner graph,
            // including carried/equipped goods and nested containers. Command-only bound.
            var pending = new Stack<Entity>(zone.GetReadOnlyEntities()); pending.Push(actor); pending.Push(ParentEntity);
            var seen = new HashSet<Entity>();
            while (pending.Count > 0)
            {
                var owner = pending.Pop(); if (owner == null || !seen.Add(owner)) continue;
                if (seen.Count > 8192 || owner.ID == copy.ID) return false;
                var inventory = owner.GetPart<InventoryPart>();
                if (inventory != null) foreach (var item in inventory.Objects.Concat(inventory.EquippedItems.Values)) pending.Push(item);
                var container = owner.GetPart<ContainerPart>(); if (container != null) foreach (var item in container.Contents) pending.Push(item);
            }
            return true;
        }
        protected override void Choices(Entity actor, Zone zone, InventoryActionList actions)
        { foreach (var book in actor.GetPart<InventoryPart>().Objects.Where(e => Original(actor, e))) Offer(actions, "copy [5dr + 1 ink vial]: " + SecondExplorationActions.BookTitle(book), SecondExplorationActions.Choice("CopyVolume", book)); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var book = SecondExplorationActions.Selected(actor, command); if (!Original(actor, book) || !tx.TryClaim(book, actor, command)) return false;
            var g = book.GetPart<GrimoirePart>(); string skill = g.SkillClassName, knowledge = g.KnowledgeProperty, learn = g.LearnMessage, known = g.AlreadyKnownMessage;
            var copy = SecondExplorationActions.Content(zone)?.CreateEntity("GrimoireCopy");
            if (!FreshCopy(copy, actor, zone)) return false;
            var cg = copy.GetPart<GrimoirePart>(); cg.SkillClassName = skill; cg.KnowledgeProperty = knowledge; cg.LearnMessage = learn; cg.AlreadyKnownMessage = known;
            copy.GetPart<RenderPart>().DisplayName = "copy of " + book.GetDisplayName(); if (copy.GetPart<StackerPart>() is StackerPart s) s.MaxStack = 1;
            if (!SecondExplorationActions.Pay(tx, actor, ParentEntity, 5) || !SecondExplorationActions.Spend(tx, actor, "InkVial", 1, command)
                || !Current(actor, zone) || !Original(actor, book) || book.GetPart<GrimoirePart>() != g || g.SkillClassName != skill || g.KnowledgeProperty != knowledge
                || !FreshCopy(copy, actor, zone)) return false;
            return SecondExplorationActions.Give(tx, actor, copy, command);
        }
    }
    public sealed class ArtisanRepairServicePart : SecondExplorationServicePart
    {
        public override string Name => "ArtisanRepairService";
        protected override string[] Verbs => new[] { "ArtisanRepair" };
        static bool Eligible(Entity actor, Entity item) => SecondExplorationActions.Carried(actor, item, true) && !SecondExplorationActions.Special(item)
            && !item.HasPart<RentalPart>() && item.GetPart<RepairablePart>()?.PortableEquipment == true && item.HasEffect<BrokenEffect>();
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a)
        { foreach (var item in actor.GetPart<InventoryPart>().Objects.Where(e => Eligible(actor, e))) Offer(a, "(8dr) repair " + item.GetDisplayName(), SecondExplorationActions.Choice("ArtisanRepair", item)); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var item = SecondExplorationActions.Selected(actor, command); if (!Eligible(actor, item) || !tx.TryClaim(item, actor, command)) return false;
            var fault = item.GetPart<RepairablePart>(); var recipe = RepairRecipeRegistry.Get(fault.RecipeId);
            if (recipe == null || item.GetPart<CompositionPart>()?.Contains(recipe.Composition) != true
                || !SecondExplorationActions.Pay(tx, actor, ParentEntity, 8)
                || !SecondExplorationActions.Spend(tx, ParentEntity, recipe.MaterialBlueprint, recipe.Quantity, command)
                || !Current(actor, zone) || !Eligible(actor, item) || item.GetPart<RepairablePart>() != fault || RepairRecipeRegistry.Get(fault.RecipeId) != recipe) return false;
            fault.RemoveBrokenWithUndo(tx); return !item.HasEffect<BrokenEffect>();
        }
    }
    public sealed class RentalDeskPart : SecondExplorationServicePart
    {
        public override string Name => "RentalDesk";
        protected override string[] Verbs => new[] { "ReturnRental", "BuyRental" };
        protected override bool Civilian => ParentEntity?.HasTag("Creature") == true;
        bool Loan(Entity actor, Entity item) => SecondExplorationActions.OwnedExact(actor, item) && SecondExplorationActions.Units(item) == 1
            && item.GetPart<RentalPart>() is RentalPart loan && loan.InkPaid >= 0 && loan.LessorBlueprintName == ParentEntity.BlueprintName;
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a)
        {
            foreach (var item in SecondExplorationActions.Owned(actor).Where(e => Loan(actor, e)))
            {
                Offer(a, "(+" + (int)Math.Floor(item.GetPart<RentalPart>().InkPaid * RentalSystem.REFUND_FRACTION) + " Ink) return " + item.GetDisplayName(), SecondExplorationActions.Choice("ReturnRental", item));
                int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(actor), ParentEntity);
                if (price > 0) Offer(a, "(" + price + "dr; no Ink refund) buy " + item.GetDisplayName(), SecondExplorationActions.Choice("BuyRental", item));
            }
        }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var item = SecondExplorationActions.Selected(actor, command); if (!Loan(actor, item) || !tx.TryClaim(item, actor, command)) return false;
            var loan = item.GetPart<RentalPart>();
            if (command.StartsWith("BuyRental|", StringComparison.Ordinal))
            {
                int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(actor), ParentEntity);
                if (!SecondExplorationActions.Pay(tx, actor, ParentEntity, price)) return false;
            }
            else
            {
                if (item.GetPart<PhysicsPart>().Equipped == actor)
                { var context = new InventoryContext(actor, zone); var unequip = new UnequipCommand(item); if (!unequip.Validate(context).IsValid || !unequip.Execute(context, tx).Success) return false; }
                if (!Current(actor, zone) || !Loan(actor, item) || !SecondExplorationActions.Transfer(tx, actor, actor, ParentEntity, item, command)) return false;
                int old = RentalSystem.GetInk(actor), refund = (int)Math.Floor(loan.InkPaid * RentalSystem.REFUND_FRACTION);
                if (old > int.MaxValue - refund) return false;
                tx.Do(() => RentalSystem.SetInk(actor, old + refund), () => RentalSystem.SetInk(actor, RentalSystem.GetInk(actor) - refund));
            }
            tx.Do(() => item.RemovePart(loan), () => { if (!item.Parts.Contains(loan)) item.AddPart(loan); }); return true;
        }
    }
    public sealed class GuestLockerPart : SecondExplorationServicePart
    {
        public override string Name => "GuestLocker";
        public Entity ClaimedBy;
        protected override string[] Verbs => new[] { "ClaimGuestLocker" };
        protected override bool Civilian => false;
        bool CanClaim(Entity actor) => ClaimedBy == null && ParentEntity.GetPart<ContainerPart>() is ContainerPart c && c.Locked
            && actor.GetEffect<UnderTheClothEffect>() is UnderTheClothEffect oath && !oath.Broken && oath.ExpiryTick > WorldClock.CurrentTick;
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a) { if (CanClaim(actor)) Offer(a, "claim locker (guest-right)", "ClaimGuestLocker"); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            if (command != "ClaimGuestLocker" || !CanClaim(actor)) return false;
            var c = ParentEntity.GetPart<ContainerPart>(); tx.Do(() => { c.Locked = false; ClaimedBy = actor; }, () => { c.Locked = true; ClaimedBy = null; }); return true;
        }
    }
    public sealed class LocksmithServicePart : SecondExplorationServicePart
    {
        public override string Name => "LocksmithService";
        protected override string[] Verbs => new[] { "LocksmithOpen" };
        bool Lock(Entity chest, Zone zone) => WorldResourceActions.Ground(chest, zone) && SecondExplorationActions.Live(chest)
            && chest.GetPart<DestructiblePart>()?.Gone != true
            && SpatialQuery.Distance(zone, ParentEntity, chest) <= 2 && !chest.HasTag("Creature") && !SecondExplorationActions.Special(chest)
            && !chest.HasPart<DoorPart>() && chest.BlueprintName != "CurationCounterfoilCabinet" && !chest.BlueprintName.StartsWith("Curation", StringComparison.Ordinal)
            && !chest.BlueprintName.StartsWith("Stillleaf", StringComparison.Ordinal) && chest.GetPart<ContainerPart>()?.IsLocked == true
            && chest.GetPart<LockPart>()?.IsLocked == true;
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a)
        { foreach (var chest in zone.GetReadOnlyEntities().Where(e => Lock(e, zone))) Offer(a, "(6dr) unlock " + chest.GetDisplayName(), SecondExplorationActions.Choice("LocksmithOpen", chest)); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var chest = SecondExplorationActions.GroundSelected(zone, command); if (!Lock(chest, zone) || !tx.TryClaim(chest, actor, command)
                || !SecondExplorationActions.Pay(tx, actor, ParentEntity, 6)) return false;
            var locked = chest.GetPart<LockPart>(); var c = chest.GetPart<ContainerPart>(); bool old = c.Locked;
            tx.Do(() => { locked.IsLocked = false; c.Locked = false; }, () => { locked.IsLocked = true; c.Locked = old; }); return true;
        }
    }
    public sealed class CivilianCourtesyPart : SecondExplorationServicePart
    {
        public override string Name => "CivilianCourtesy";
        protected override string[] Verbs => new[] { "StepAside" };
        bool Candidate(Entity actor, Zone zone, out int dx, out int dy)
        {
            dx = dy = 0; var at = zone.GetEntityCell(ParentEntity); var a = zone.GetEntityCell(actor);
            if (ParentEntity.HasPart<SpatialFootprintPart>() || DragSystem.IsDragging(ParentEntity)) return false;
            int best = -1;
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0) continue; var c = zone.GetCell(at.X + x, at.Y + y);
                if (c == null || c.BlocksMovement(ParentEntity) || c.Objects.Any(e => e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>() || e.HasPart<TriggerOnStepPart>())
                    || zone.TileState.Get(c.X, c.Y)?.IsEmpty == false) continue;
                int score = Math.Max(Math.Abs(c.X - a.X), Math.Abs(c.Y - a.Y));
                if (score > best) { best = score; dx = x; dy = y; }
            }
            return best >= 0;
        }
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a) { if (Candidate(actor, zone, out _, out _)) Offer(a, "ask to step aside", "StepAside"); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            if (command != "StepAside" || !Candidate(actor, zone, out int dx, out int dy)) return false;
            var original = zone.GetEntityPosition(ParentEntity); var owner = ParentEntity;
            tx.Do(null, () => { if (owner.SpatialZone == zone) zone.MoveEntity(owner, original.x, original.y); });
            return MovementSystem.TryMove(owner, zone, dx, dy);
        }
    }
    public sealed class CivilianAidPart : SecondExplorationServicePart, IDescribeState, IInventoryActionReceiptOwner
    {
        const string Treat = "TreatPatient";
        const int ThreatRange = 3;
        public override string Name => "CivilianAid";
        protected override string[] Verbs => new[] { Treat };
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 45;

        internal static void BindSillResident(Zone zone, OverworldZoneManager manager)
        {
            if (zone == null || !SillCourtComposition.MatchesSite(manager, zone.ZoneID)) return;
            var residents = zone.GetReadOnlyEntitiesWithPart<ConversationPart>().Where(owner =>
                owner.BlueprintName == "Scribe" && WorldResourceActions.Ground(owner, zone)
                && owner.HasTag("Creature") && owner.HasPart<BrainPart>()
                && FactionManager.GetFaction(owner) == "Villagers" && owner.GetProperty("SettlementId") == zone.ZoneID
                && owner.GetProperty("LocalCulture") == "Villagers" && owner.GetProperty("LocalRole") == "scribe"
                && !string.IsNullOrWhiteSpace(owner.GetProperty("LocalPersonalName"))
                && owner.GetPart<ConversationPart>()?.ConversationID == "Scribe_1").Take(2).ToArray();
            if (residents.Length == 1 && SecondExplorationActions.Live(residents[0]) && !residents[0].HasPart<CivilianAidPart>())
                residents[0].AddPart(new CivilianAidPart());
        }

        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<CivilianAidPart>() == this
            && command?.StartsWith(Treat + "|", StringComparison.Ordinal) == true;

        public string DescribeState(DescribeContext context) => SecondExplorationActions.Live(ParentEntity)
            && ParentEntity.GetStat("Hitpoints") is Stat hp && hp.Value < hp.Max ? "Looks wounded." : null;

        protected override bool Current(Entity actor, Zone zone) => base.Current(actor, zone) && !UnderAttack(zone);

        bool UnderAttack(Zone zone)
        {
            var patient = ParentEntity;
            foreach (var hunter in zone.GetReadOnlyEntitiesWithPart<BrainPart>())
            {
                var brain = hunter.GetPart<BrainPart>();
                if (brain == null || brain.GoalCount == 0 || SpatialQuery.Distance(zone, hunter, patient) > ThreatRange
                    || !SecondExplorationActions.Live(hunter) || !WorldResourceActions.Ground(hunter, zone)) continue;
                for (int i = brain.GoalCount - 1; i >= 0; i--)
                {
                    var goal = brain.PeekGoalAt(i);
                    if (goal is KillGoal hunt && !hunt.Finished())
                    {
                        if (hunt.Target == patient) return true;
                        break;
                    }
                    if (goal?.CanFight() == false) break;
                }
            }
            return false;
        }

        bool Needed(Entity item)
        {
            if (item == null || SecondExplorationActions.Special(item) || item.HasPart<RentalPart>()
                || item.Parts.Any(part => part.ParentEntity != item)) return false;
            var tonic = item.GetPart<TonicPart>();
            bool simpleTonic = tonic != null && string.IsNullOrEmpty(tonic.StatBoost)
                && !item.HasPart<StatusTonicPart>() && !item.HasPart<BrewItemPart>();
            if (item.BlueprintName == "HealingTonic") return simpleTonic && tonic.Healing == "4d6+4"
                && ParentEntity.GetStat("Hitpoints") is Stat hp && hp.Value < hp.Max && hp.BaseValue < hp.Max && !item.HasPart<CureTonicPart>();
            if (item.BlueprintName == "Antidote") return simpleTonic && item.GetPart<CureTonicPart>()?.CureEffect == nameof(PoisonedEffect)
                && (ParentEntity.GetEffect<PoisonedEffect>() is { Duration: not 0 } || ParentEntity.GetEffect<PoisonedByGasEffect>() is { Duration: not 0 });
            return item.BlueprintName == "SoddenFieldDressing" && item.HasPart<SoddenDressingPart>()
                && ParentEntity.GetPart<StatusEffectsPart>()?.GetAllEffects().Any(SoddenDressingPart.TreatsActiveEffect) == true;
        }
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a)
        {
            var pack = actor.GetPart<InventoryPart>();
            if (pack == null) return;
            foreach (var item in pack.Objects.Where(e => SecondExplorationActions.Carried(actor, e) && Needed(e) && pack.CanConsumeOne(e)))
                Offer(a, "treat with " + InventoryPart.GetUnitDisplayName(item) + " (1)", SecondExplorationActions.Choice(Treat, item));
        }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var medicine = SecondExplorationActions.Selected(actor, command);
            if (!SecondExplorationActions.Carried(actor, medicine) || !Needed(medicine) || !tx.TryClaim(medicine, actor, command)) return false;
            var patient = ParentEntity;
            var pack = actor.GetPart<InventoryPart>();
            var physics = medicine.GetPart<PhysicsPart>();
            var stack = medicine.GetPart<StackerPart>();
            var origin = zone.GetEntityCell(actor);
            var target = zone.GetEntityCell(patient);
            int units = SecondExplorationActions.Units(medicine);
            string patientId = patient.ID, medicineId = medicine.ID, blueprint = medicine.BlueprintName;
            var acceptedParts = medicine.Parts.ToArray();
            var tonic = medicine.GetPart<TonicPart>(); var cure = medicine.GetPart<CureTonicPart>();
            var payload = (tonic?.Healing, tonic?.StatBoost, tonic?.Effect, tonic?.Duration, tonic?.Drink, cure?.CureEffect);

            bool SameMedicine() => medicine.ID == medicineId && medicine.GetPart<PhysicsPart>() == physics
                && medicine.GetPart<StackerPart>() == stack;
            var receipt = InventoryTransferSnapshot.Capture(pack, medicine);
            tx.Do(null, () =>
            {
                bool moved = medicine.SpatialZone != null || physics.Equipped != null
                    || (physics.InInventory != null && physics.InInventory != actor);
                if (SameMedicine() && !moved && !pack.Objects.Any(e => e != medicine && e?.ID == medicineId))
                {
                    receipt.Restore();
                    return;
                }
                // Independent relocation keeps its custody. Undo only our
                // staged unit delta when the exact remainder is still intact.
                if (SameMedicine() && moved && units > 1 && stack.StackCount == units - 1)
                {
                    stack.StackCount = units;
                    (physics.InInventory ?? physics.Equipped)?.GetPart<InventoryPart>()?.RefreshHandlingCarryPenalty();
                }
                receipt.RestoreExcept(new HashSet<Entity> { medicine });
            });
            if (!receipt.Apply(() => pack.TryStageConsumeOne(medicine, tx)) || !receipt.ClaimChanges(tx, actor, command)) return false;
            receipt.WatchChangedOwners(tx);

            bool Paid() => ReferenceEquals(ParentEntity, patient) && patient.ID == patientId && Current(actor, zone)
                && zone.GetEntityCell(actor) == origin && zone.GetEntityCell(patient) == target
                && actor.GetPart<InventoryPart>() == pack && pack.ParentEntity == actor
                && SameMedicine() && medicine.BlueprintName == blueprint
                && medicine.Parts.SequenceEqual(acceptedParts) && Needed(medicine)
                && (tonic?.Healing, tonic?.StatBoost, tonic?.Effect, tonic?.Duration, tonic?.Drink, cure?.CureEffect) == payload
                && (units > 1 ? SecondExplorationActions.Carried(actor, medicine) && stack.StackCount == units - 1
                    : medicine.SpatialZone == null && physics.InInventory == null && physics.Equipped == null
                        && !pack.Objects.Any(e => e == medicine || e?.ID == medicineId) && !pack.EquippedItems.ContainsValue(medicine)
                        && pack.FindEquippedBodyPart(medicine) == null);
            if (!Paid()) return false;
            tx.BeforeCommit(Paid);
            tx.AfterCommit(() =>
            {
                if (actor.HasTag("Player")) MessageLog.Add($"You treat {patient.GetDisplayName()} with {InventoryPart.GetUnitDisplayName(medicine)}.");
                if (blueprint == "SoddenFieldDressing")
                { patient.RemoveEffect(typeof(PoisonedEffect)); patient.RemoveEffect(typeof(BleedingEffect)); }
                else tonic.ApplyTo(patient, actor, zone, consumeItem: false, showUseMessage: false);
            }); return true;
        }
    }
    public sealed class CivilianEquipmentGiftPart : SecondExplorationServicePart
    {
        public override string Name => "CivilianEquipmentGift";
        protected override string[] Verbs => new[] { "DonateEquipment" };
        bool Eligible(Entity actor, Entity item) => SecondExplorationActions.Carried(actor, item, true) && !SecondExplorationActions.Special(item)
            && !item.HasPart<RentalPart>() && item.HasPart<EquippablePart>() && (item.HasPart<MeleeWeaponPart>() || item.HasPart<ArmorPart>())
            && ParentEntity.GetPart<Body>() != null;
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a)
        { foreach (var item in actor.GetPart<InventoryPart>().Objects.Where(e => Eligible(actor, e))) Offer(a, "give (free slot): " + item.GetDisplayName(), SecondExplorationActions.Choice("DonateEquipment", item)); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var item = SecondExplorationActions.Selected(actor, command);
            if (!Eligible(actor, item) || !SecondExplorationActions.Transfer(tx, actor, actor, ParentEntity, item, command) || !Current(actor, zone)) return false;
            return EquipCommand.ExecuteInternal(new InventoryContext(ParentEntity, zone), tx, item, null, false, true).Success;
        }
    }
    public sealed class BurialPart : SecondExplorationServicePart
    {
        public override string Name => "Burial";
        protected override string[] Verbs => new[] { "InterCorpse" };
        protected override bool Civilian => false;
        bool Corpse(Entity actor, Entity item) => ParentEntity.HasTag("Graveyard") && ParentEntity.GetPart<ContainerPart>()?.IsLocked == false
            && SecondExplorationActions.Carried(actor, item, true) && item.HasTag("Corpse") && !item.HasTag("Creature") && !SecondExplorationActions.Special(item);
        protected override void Choices(Entity actor, Zone zone, InventoryActionList a)
        { foreach (var item in actor.GetPart<InventoryPart>().Objects.Where(e => Corpse(actor, e))) Offer(a, "inter " + item.GetDisplayName(), SecondExplorationActions.Choice("InterCorpse", item)); }
        protected override bool Apply(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var corpse = SecondExplorationActions.Selected(actor, command); if (!Corpse(actor, corpse)) return false;
            var context = new InventoryContext(actor, zone); var put = new PutInContainerCommand(ParentEntity, corpse);
            return put.Validate(context).IsValid && put.Execute(context, tx).Success;
        }
    }
}
