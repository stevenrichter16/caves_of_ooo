using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // New Parts are resolved reflectively so the initial receipt is assertion RED,
    // while every completed test exercises the native inventory command boundary.
    public abstract class SecondExplorationFixture : FiftyWorldFixture
    {
        object priorFactory; FieldInfo factoryField;
        [SetUp] public void SetUpExploration()
        {
            var type = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.SecondExplorationActions");
            factoryField = type?.GetField("Factory", BindingFlags.Static | BindingFlags.Public);
            if (factoryField != null) { priorFactory = factoryField.GetValue(null); factoryField.SetValue(null, Factory); }
            TradeSystem.SetDrams(Actor, 100); RentalSystem.SetInk(Actor, 100);
        }
        [TearDown] public void TearDownExploration() { factoryField?.SetValue(null, priorFactory); }
        protected static Part Add(Entity entity, string typeName)
        {
            var type = typeof(Entity).Assembly.GetType("CavesOfOoo.Core." + typeName);
            Assert.NotNull(type, "Missing exploration behavior " + typeName);
            var existing = entity.Parts.FirstOrDefault(p => p.GetType() == type);
            if (existing != null) return existing;
            var part = (Part)Activator.CreateInstance(type); entity.AddPart(part); return part;
        }
        protected Entity Provider(string part, string blueprint = "PeatCutter")
        {
            var e = Place(blueprint); if (!e.HasPart<InventoryPart>()) e.AddPart(new InventoryPart());
            e.GetPart<InventoryPart>().MaxWeight = 500;
            var brain = e.GetPart<BrainPart>(); brain.Target = null; brain.Passive = true; brain.Staying = true;
            Add(e, part); Assert.False(FactionManager.IsHostile(e, Actor), "Willing-provider precondition"); return e;
        }
        protected static int Units(Entity e) => e.GetPart<StackerPart>()?.StackCount ?? 1;
        protected static void Set(Part part, string name, object value)
        { var field = part.GetType().GetField(name); Assert.NotNull(field, "Persisted field " + name); field.SetValue(part, value); }
        protected static object Get(Part part, string name) => part.GetType().GetField(name).GetValue(part);
        protected string Selected(Entity provider, string prefix, Entity item) => Choice(provider, prefix, a => a.Command.EndsWith("|" + Id(item), StringComparison.Ordinal));
        protected Entity Rental(Entity provider, string blueprint = "ShortSword", bool equipped = false)
        {
            var item = Carry(blueprint); item.GetPart<StackerPart>().MaxStack = 1;
            item.AddPart(new RentalPart { InkPaid = 12, LessorBlueprintName = provider.BlueprintName });
            if (equipped) Assert.True(InventorySystem.Equip(Actor, item)); return item;
        }
    }

    public sealed class SecondExplorationServicesTests : SecondExplorationFixture
    {
        [TestCase(false)] [TestCase(true)]
        public void CopyTheSelectedSecondVolumeOrRefuseWithoutItsInk(bool noInk)
        {
            var scribe = Provider("ScribeCopyServicePart", "Scribe");
            var first = Carry("WardGleamGrimoire"); var second = Carry("DryingBreezeGrimoire");
            if (!noInk) Carry("InkVial");
            int purse = TradeSystem.GetDrams(Actor), payee = TradeSystem.GetDrams(scribe);
            bool done = Act(scribe, "CopyVolume|" + Id(second));
            Assert.AreEqual(!noInk, done);
            Assert.True(Pack.Objects.Contains(first)); Assert.True(Pack.Objects.Contains(second));
            var copies = Pack.Objects.Where(e => e.BlueprintName == "GrimoireCopy").ToArray();
            Assert.AreEqual(noInk ? 0 : 1, copies.Length);
            if (!noInk) { Assert.AreEqual(second.GetPart<GrimoirePart>().SkillClassName, copies[0].GetPart<GrimoirePart>().SkillClassName); Assert.AreEqual(0, Count("InkVial")); }
            Assert.AreEqual(purse - (noInk ? 0 : 5), TradeSystem.GetDrams(Actor));
            Assert.AreEqual(payee + (noInk ? 0 : 5), TradeSystem.GetDrams(scribe));
        }
        [TestCase(false)] [TestCase(true)]
        public void ArtisanSuppliesRealMaterialsOrRefusesEmptyStock(bool emptyStock)
        {
            var smith = Provider("ArtisanRepairServicePart", "Weaponsmith"); var item = Carry("Dagger");
            var repair = item.GetPart<RepairablePart>(); Assert.NotNull(repair); Assert.True(repair.PortableEquipment);
            Assert.True(item.ApplyEffect(new BrokenEffect())); var broken = item.GetEffect<BrokenEffect>();
            var recipe = RepairRecipeRegistry.Get(repair.RecipeId); Assert.NotNull(recipe);
            var stock = smith.GetPart<InventoryPart>(); stock.Objects.Clear();
            if (!emptyStock) for (int i = 0; i < recipe.Quantity; i++) Assert.True(stock.AddObject(Factory.CreateEntity(recipe.MaterialBlueprint)));
            int purse = TradeSystem.GetDrams(Actor); bool done = Act(smith, "ArtisanRepair|" + Id(item));
            Assert.AreEqual(!emptyStock, done); Assert.AreEqual(emptyStock, item.HasEffect<BrokenEffect>());
            Assert.AreEqual(purse - (emptyStock ? 0 : 8), TradeSystem.GetDrams(Actor));
            Assert.True(Pack.Objects.Contains(item)); Assert.False(Pack.Objects.Any(e => e.BlueprintName == recipe.MaterialBlueprint));
            if (emptyStock) Assert.AreSame(broken, item.GetEffect<BrokenEffect>());
            else Assert.False(stock.Objects.Any(e => e.BlueprintName == recipe.MaterialBlueprint));
        }
        [TestCase(false)] [TestCase(true)]
        public void ReturnOneRentalKeepsOtherLoanAndItsBodyBinding(bool wrongLessor)
        {
            var desk = Provider("RentalDeskPart", "Quartermaster"); var selected = Rental(desk); var kept = Rental(desk, "Dagger", true);
            if (wrongLessor) selected.GetPart<RentalPart>().LessorBlueprintName = "SomeOtherLessor";
            int ink = RentalSystem.GetInk(Actor); var slot = Pack.FindEquippedBodyPart(kept); Assert.NotNull(slot);
            Assert.AreEqual(!wrongLessor, Act(desk, "ReturnRental|" + Id(selected)));
            Assert.AreEqual(ink + (wrongLessor ? 0 : 6), RentalSystem.GetInk(Actor));
            Assert.AreSame(slot, Pack.FindEquippedBodyPart(kept)); Assert.NotNull(kept.GetPart<RentalPart>());
            Assert.AreEqual(wrongLessor, Pack.Objects.Contains(selected));
            Assert.AreEqual(!wrongLessor, desk.GetPart<InventoryPart>().Objects.Contains(selected));
        }
        [TestCase(false)] [TestCase(true)]
        public void BuyOutExactWornLoanPreservesEquipmentAndInk(bool insufficient)
        {
            var desk = Provider("RentalDeskPart", "Quartermaster"); var selected = Rental(desk, "ShortSword", true);
            int price = TradeSystem.GetBuyPrice(selected, TradeSystem.GetTradePerformance(Actor), desk); Assert.Greater(price, 0);
            TradeSystem.SetDrams(Actor, insufficient ? price - 1 : price); int payee = TradeSystem.GetDrams(desk);
            var binding = Pack.FindEquippedBodyPart(selected); int ink = RentalSystem.GetInk(Actor);
            Assert.AreEqual(!insufficient, Act(desk, "BuyRental|" + Id(selected)));
            Assert.AreEqual(insufficient, selected.HasPart<RentalPart>()); Assert.AreSame(binding, Pack.FindEquippedBodyPart(selected));
            Assert.AreEqual(ink, RentalSystem.GetInk(Actor)); Assert.AreEqual(payee + (insufficient ? 0 : price), TradeSystem.GetDrams(desk));
        }
        [TestCase(false)] [TestCase(true)]
        public void ClaimGuestLockerOnlyWithUnexpiredCloth(bool expired)
        {
            var chest = Place("Chest"); chest.GetPart<ContainerPart>().Locked = true; Add(chest, "GuestLockerPart");
            Assert.True(Actor.ApplyEffect(new UnderTheClothEffect { ExpiryTick = WorldClock.CurrentTick + (expired ? -1 : 50) }));
            Assert.AreEqual(!expired, Act(chest, "ClaimGuestLocker")); Assert.AreEqual(expired, chest.GetPart<ContainerPart>().IsLocked);
            Assert.AreEqual(0, chest.GetPart<ContainerPart>().Contents.Count);
            if (!expired) { Actor.RemoveEffect(typeof(UnderTheClothEffect)); Assert.False(chest.GetPart<ContainerPart>().IsLocked, "Expiry cannot strand deposited possessions."); Assert.False(Act(chest, "ClaimGuestLocker")); }
        }
        [TestCase(false)] [TestCase(true)]
        public void LocksmithOpensOneNearbyOrdinaryChestButNotUniqueLock(bool unique)
        {
            var worker = Provider("LocksmithServicePart", "Weaponsmith"); var chest = Place("Chest", 12, 10); var other = Place("Chest", 12, 11);
            chest.AddPart(new LockPart { KeyId = "fixture", IsLocked = true }); other.AddPart(new LockPart { KeyId = "other", IsLocked = true });
            if (unique) chest.SetTag("Unique"); int purse = TradeSystem.GetDrams(Actor);
            Assert.AreEqual(!unique, Act(worker, "LocksmithOpen|" + Id(chest)));
            Assert.AreEqual(unique, chest.GetPart<LockPart>().IsLocked); Assert.True(other.GetPart<LockPart>().IsLocked);
            Assert.AreEqual(purse - (unique ? 0 : 6), TradeSystem.GetDrams(Actor));
        }
        [TestCase(2, true)] [TestCase(3, false)]
        public void LocksmithHonorsItsAdvertisedTwoCellServiceRange(int distance, bool expected)
        {
            var worker = Provider("LocksmithServicePart", "Weaponsmith"); var chest = Place("Chest", 11 + distance, 10);
            chest.AddPart(new LockPart { KeyId = "range-fixture", IsLocked = true }); int purse = TradeSystem.GetDrams(Actor);
            Assert.AreEqual(expected, Act(worker, "LocksmithOpen|" + Id(chest)));
            Assert.AreEqual(!expected, chest.GetPart<LockPart>().IsLocked);
            Assert.AreEqual(purse - (expected ? 6 : 0), TradeSystem.GetDrams(Actor));
        }
        [TestCase(false)] [TestCase(true)]
        public void CourteousNpcMovesOneRealStepUnlessAllDestinationsAreBlocked(bool enclosed)
        {
            var npc = Provider("CivilianCourtesyPart"); var before = Zone.GetEntityPosition(npc);
            if (enclosed) for (int y = 9; y <= 11; y++) for (int x = 10; x <= 12; x++)
                if ((x != 11 || y != 10) && (x != 10 || y != 10)) Place("StoneWall", x, y);
            Assert.AreEqual(!enclosed, Act(npc, "StepAside")); var after = Zone.GetEntityPosition(npc);
            if (enclosed) Assert.AreEqual(before, after);
            else { Assert.AreNotEqual(before, after); Assert.AreEqual(1, Math.Max(Math.Abs(before.x - after.x), Math.Abs(before.y - after.y))); }
            Assert.AreEqual((10,10), Zone.GetEntityPosition(Actor));
        }
        [TestCase(false)] [TestCase(true)]
        public void OwnHealingTonicTreatsOnlyInjuredWillingPatient(bool healthy)
        {
            var patient = Provider("CivilianAidPart"); var hp = patient.GetStat("Hitpoints"); hp.BaseValue = healthy ? hp.Max : hp.Max - 10;
            var medicine = Carry("HealingTonic"); int before = hp.Value, ownHp = Actor.GetStatValue("Hitpoints");
            Assert.AreEqual(!healthy, Act(patient, "TreatPatient|" + Id(medicine)));
            if (healthy) { Assert.AreEqual(before, hp.Value); Assert.True(Pack.Objects.Contains(medicine)); }
            else { Assert.Greater(hp.Value, before); Assert.False(Pack.Objects.Contains(medicine)); }
            Assert.AreEqual(ownHp, Actor.GetStatValue("Hitpoints"));
        }
        [TestCase(false)] [TestCase(true)]
        public void EquipmentGiftActuallyBindsTheSameItemUnlessItIsRented(bool rented)
        {
            var patient = Provider("CivilianEquipmentGiftPart"); var item = Carry("Dagger");
            if (rented) item.AddPart(new RentalPart { InkPaid = 8, LessorBlueprintName = "Quartermaster" });
            Assert.NotNull(patient.GetPart<Body>()); Assert.IsNull(Pack.FindEquippedBodyPart(item));
            Assert.AreEqual(!rented, Act(patient, "DonateEquipment|" + Id(item)));
            Assert.AreEqual(rented, Pack.Objects.Contains(item));
            var binding = patient.GetPart<InventoryPart>().FindEquippedBodyPart(item);
            if (rented) Assert.IsNull(binding); else { Assert.NotNull(binding); Assert.AreSame(patient, item.GetPart<PhysicsPart>().Equipped); }
        }
        [TestCase(false)] [TestCase(true)]
        public void BurialMovesExactCorpseIntoActualGraveyardUnlessLocked(bool locked)
        {
            var grave = Place("Graveyard"); Add(grave, "BurialPart"); grave.GetPart<ContainerPart>().Locked = locked;
            var corpse = Carry("ReedbackGrazerCorpse"); Assert.True(corpse.HasTag("Corpse")); int purse = TradeSystem.GetDrams(Actor);
            Assert.AreEqual(!locked, Act(grave, "InterCorpse|" + Id(corpse)));
            Assert.AreEqual(locked, Pack.Objects.Contains(corpse)); Assert.AreEqual(!locked, grave.GetPart<ContainerPart>().Contents.Contains(corpse));
            Assert.AreEqual(purse, TradeSystem.GetDrams(Actor));
        }
        [Test] public void SuccessfulServiceMenuNamesExactBookAndPriceWithoutMutation()
        {
            var scribe = Provider("ScribeCopyServicePart", "Scribe"); Carry("InkVial"); var book = Carry("DryingBreezeGrimoire");
            int count = Pack.Objects.Count, purse = TradeSystem.GetDrams(Actor);
            var action = Actions(scribe).Single(a => a.Command == "CopyVolume|" + Id(book));
            StringAssert.Contains("Drying Breeze", action.Display); StringAssert.Contains("5dr", action.Display); StringAssert.Contains("1 ink vial", action.Display);
            Assert.AreEqual("Grimoire of Drying Breeze", book.GetDisplayName(), "Only the menu prefix is shortened; the real original title is unchanged.");
            Assert.AreEqual(count, Pack.Objects.Count); Assert.AreEqual(purse, TradeSystem.GetDrams(Actor));
        }
        [Test] public void FailedOuterCopyRestoresExactInkPurseAndPack()
        {
            var scribe = Provider("ScribeCopyServicePart", "Scribe"); var ink = Carry("InkVial"); var book = Carry("WardGleamGrimoire");
            int purse = TradeSystem.GetDrams(Actor), payee = TradeSystem.GetDrams(scribe); FailAfter();
            Assert.False(Act(scribe, "CopyVolume|" + Id(book)));
            Assert.True(Pack.Objects.Contains(ink)); Assert.AreEqual(1, Units(ink)); Assert.AreEqual(0, Count("GrimoireCopy"));
            Assert.AreEqual(purse, TradeSystem.GetDrams(Actor)); Assert.AreEqual(payee, TradeSystem.GetDrams(scribe));
        }
    }
}
