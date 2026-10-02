using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SeveredLimbIdentityTests
    {
        [Test]
        public void FactoryCreatesDistinctIdentitiesBeforeAnyWorldOrSaveAdmission()
        {
            var part = new BodyPart { Type = "Arm", Name = "arm", Category = BodyPartCategory.ANIMAL };
            var first = SeveredLimbFactory.Create(part);
            var second = SeveredLimbFactory.Create(part);

            Assert.IsFalse(string.IsNullOrEmpty(first.ID), "A portable limb needs an identity when created, before saving.");
            Assert.IsFalse(string.IsNullOrEmpty(second.ID));
            Assert.AreNotEqual(first.ID, second.ID, "Two cuts cannot share an owner identity.");
            Assert.AreEqual("SeveredLimb", first.BlueprintName);
            Assert.IsTrue(first.GetPart<PhysicsPart>().Takeable);
            Assert.AreEqual("Arm", first.GetPart<SeveredLimbPart>().PartType);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FreshLimbAppearsInNativeTargetPickerUnlessIdentityIsRemoved(bool removeIdentity)
        {
            var limb = SeveredLimbFactory.Create(new BodyPart { Type = "Arm", Name = "arm", Category = BodyPartCategory.ANIMAL });
            if (removeIdentity) limb.ID = null;
            var zone = new Zone("SeveredLimbPicker");
            Assert.IsTrue(zone.AddEntity(limb, 5, 5));
            var cell = zone.GetCell(5, 5);

            var actions = WorldInteractionSystem.BuildTargetPickerActions(cell);

            Assert.AreEqual(removeIdentity ? 0 : 1, actions.Count);
            if (removeIdentity)
            {
                Assert.IsNull(WorldInteractionSystem.FindInCell(cell, limb.ID));
                return;
            }
            Assert.AreEqual(WorldInteractionSystem.PickTargetCommandPrefix + limb.ID, actions.Single().Command);
            Assert.AreSame(limb, WorldInteractionSystem.FindInCell(cell, limb.ID));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DismemberPublishesIdentifiedLimbBeforeObserversUnlessVetoed(bool veto)
        {
            var state = HotbarSaveFixture.MakeState(0);
            var actor = state.Player;
            var body = AddBody(actor);
            var zone = state.ZoneManager.ActiveZone;
            var observer = new SeveredLimbIdentityObserver { Veto = veto };
            actor.AddPart(observer);

            Assert.AreEqual(!veto, body.Dismember(body.GetPartByType("Arm"), zone));
            var limbs = zone.GetAllEntities().Where(e => e.HasTag("SeveredLimb")).ToArray();
            Assert.AreEqual(veto ? 0 : 1, limbs.Length);
            Assert.AreEqual(veto ? 0 : 1, observer.AfterCount);
            if (veto)
            {
                Assert.IsNull(observer.Limb);
                Assert.AreEqual(2, body.CountParts("Arm"));
                return;
            }

            Assert.AreSame(limbs[0], observer.Limb);
            Assert.IsFalse(string.IsNullOrEmpty(observer.IdentityAtPublication));
            Assert.AreEqual(limbs[0].ID, observer.IdentityAtPublication);
            Assert.AreEqual(zone.GetEntityPosition(actor), zone.GetEntityPosition(limbs[0]));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActualDismemberedOwnerKeepsIdentityAcrossGroundOrInventorySave(bool carried)
        {
            var state = HotbarSaveFixture.MakeState(0);
            var actor = state.Player;
            actor.AddPart(new InventoryPart());
            var body = AddBody(actor);
            var zone = state.ZoneManager.ActiveZone;
            Assert.IsTrue(body.Dismember(body.GetPartByType("Arm"), zone));
            var limb = zone.GetAllEntities().Single(e => e.HasTag("SeveredLimb"));
            string identityBeforeSave = limb.ID;
            if (carried)
            {
                zone.RemoveEntity(limb);
                Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(limb));
            }

            var loaded = HotbarSaveFixture.RoundTrip(state);
            var loadedZone = loaded.ZoneManager.ActiveZone;
            var ground = loadedZone.GetAllEntities().Where(e => e.HasTag("SeveredLimb")).ToArray();
            var inventory = loaded.Player.GetPart<InventoryPart>().Objects.Where(e => e.HasTag("SeveredLimb")).ToArray();
            Assert.AreEqual(carried ? 0 : 1, ground.Length);
            Assert.AreEqual(carried ? 1 : 0, inventory.Length);
            var restored = carried ? inventory.Single() : ground.Single();
            Assert.AreEqual(identityBeforeSave, restored.ID, "Loading must retain a new limb's already published identity.");
            Assert.IsFalse(string.IsNullOrEmpty(identityBeforeSave));
            Assert.AreEqual("Arm", restored.GetPart<SeveredLimbPart>().PartType);
            Assert.AreEqual(BodyPartCategory.ANIMAL, restored.GetPart<SeveredLimbPart>().Category);
            Assert.AreEqual("severed left arm", restored.GetDisplayName());
            Assert.IsTrue(restored.GetPart<PhysicsPart>().Takeable);
            if (carried)
                Assert.AreSame(loaded.Player, restored.GetPart<PhysicsPart>().InInventory);
            else
                Assert.AreEqual(loadedZone.GetEntityPosition(loaded.Player), loadedZone.GetEntityPosition(restored));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("older-limb-opaque-id")]
        public void LegacyLoadStillRepairsMissingIdentityAndPreservesExistingIdentity(string savedIdentity)
        {
            var limb = SeveredLimbFactory.Create(new BodyPart { Type = "Hand", Name = "hand", Category = BodyPartCategory.ANIMAL });
            limb.ID = savedIdentity;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(limb);

            Assert.IsFalse(string.IsNullOrEmpty(loaded.ID));
            if (!string.IsNullOrEmpty(savedIdentity)) Assert.AreEqual(savedIdentity, loaded.ID);
            var loadedAgain = PartRoundTripHelper.RoundTripEntityViaTokenGraph(loaded);
            Assert.AreEqual(loaded.ID, loadedAgain.ID);
            Assert.AreEqual("Hand", loadedAgain.GetPart<SeveredLimbPart>().PartType);
        }

        private static Body AddBody(Entity actor)
        {
            var body = new Body();
            actor.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return body;
        }
    }

    public sealed class SeveredLimbIdentityObserver : Part
    {
        public bool Veto;
        public int AfterCount;
        public Entity Limb;
        public string IdentityAtPublication;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeDismember") return !Veto;
            if (e.ID == "AfterDismember")
            {
                AfterCount++;
                Limb = e.GetParameter<Entity>("SeveredLimbEntity");
                IdentityAtPublication = Limb?.ID;
            }
            return true;
        }
    }
}
