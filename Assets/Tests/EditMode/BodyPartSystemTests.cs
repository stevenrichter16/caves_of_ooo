using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class BodyPartSystemTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ========================
        // AnatomyFactory - Humanoid
        // ========================

        [Test]
        public void CreateHumanoid_HasExpectedPartTypes()
        {
            var root = AnatomyFactory.CreateHumanoid();

            Assert.AreEqual("Body", root.Type);
            Assert.IsTrue(root.Mortal);

            var allParts = root.GetParts();
            Assert.IsNotNull(root.GetPartByType("Head"));
            Assert.IsNotNull(root.GetPartByType("Face"));
            Assert.IsNotNull(root.GetPartByType("Back"));
            Assert.AreEqual(2, root.CountParts("Arm"));
            Assert.AreEqual(2, root.CountParts("Hand"));
            Assert.AreEqual(2, root.CountParts("Hands"));
            Assert.IsNotNull(root.GetPartByType("Feet"));
            Assert.IsNotNull(root.GetPartByType("Thrown Weapon"));
            Assert.IsNotNull(root.GetPartByType("Floating Nearby"));
        }

        [Test]
        public void CreateHumanoid_HandsHaveLaterality()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var hands = root.GetPartsByType("Hand");

            Assert.AreEqual(2, hands.Count);

            bool hasLeft = false, hasRight = false;
            for (int i = 0; i < hands.Count; i++)
            {
                if (Laterality.Match(hands[i].GetLaterality(), Laterality.LEFT))
                    hasLeft = true;
                if (Laterality.Match(hands[i].GetLaterality(), Laterality.RIGHT))
                    hasRight = true;
            }
            Assert.IsTrue(hasLeft, "Should have a left hand");
            Assert.IsTrue(hasRight, "Should have a right hand");
        }

        [Test]
        public void CreateHumanoid_HasDefaultPrimary()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var hands = root.GetPartsByType("Hand");

            bool anyPrimary = false;
            for (int i = 0; i < hands.Count; i++)
            {
                if (hands[i].DefaultPrimary)
                    anyPrimary = true;
            }
            Assert.IsTrue(anyPrimary, "At least one hand should be DefaultPrimary");
        }

        [Test]
        public void CreateHumanoid_AllPartsAreNative()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var allParts = root.GetParts();

            for (int i = 0; i < allParts.Count; i++)
                Assert.IsTrue(allParts[i].Native, $"Part {allParts[i].Type} should be Native");
        }

        [Test]
        public void CreateHumanoid_HeadIsMortal()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var head = root.GetPartByType("Head");
            Assert.IsTrue(head.Mortal);
        }

        // ========================
        // AnatomyFactory - Other Templates
        // ========================

        [Test]
        public void CreateQuadruped_HasFourFeet()
        {
            var root = AnatomyFactory.CreateQuadruped();
            Assert.AreEqual(4, root.CountParts("Feet"));
        }

        [Test]
        public void CreateInsectoid_HasSixFeet()
        {
            var root = AnatomyFactory.CreateInsectoid();
            Assert.AreEqual(6, root.CountParts("Feet"));
            Assert.AreEqual(BodyPartCategory.ARTHROPOD, root.Category);
        }

        [Test]
        public void CreateSimple_HasOnlyBody()
        {
            var root = AnatomyFactory.CreateSimple();
            Assert.AreEqual("Body", root.Type);
            Assert.AreEqual(1, root.GetParts().Count);
        }

        // ========================
        // Body Part - Tree Operations
        // ========================

        [Test]
        public void AddPart_SetsParent()
        {
            var root = new BodyPart { Type = "Body", Name = "body" };
            var arm = new BodyPart { Type = "Arm", Name = "arm" };
            root.AddPart(arm);

            Assert.AreEqual(root, arm.ParentPart);
            Assert.AreEqual(1, root.Parts.Count);
        }

        [Test]
        public void RemovePart_ClearsParent()
        {
            var root = new BodyPart { Type = "Body", Name = "body" };
            var arm = new BodyPart { Type = "Arm", Name = "arm" };
            root.AddPart(arm);
            Assert.IsTrue(root.RemovePart(arm));
            Assert.IsNull(arm.ParentPart);
            Assert.AreEqual(0, root.Parts.Count);
        }

        [Test]
        public void GetParts_ReturnsAllDescendants()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var allParts = root.GetParts();

            // Humanoid: Body, Head, Face, Back, LAr, LHand, LHands, RAr, RHand, RHands, Feet, Thrown, Float = 13
            Assert.AreEqual(13, allParts.Count);
        }

        [Test]
        public void GetPartByType_ReturnsFirstMatch()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var arm = root.GetPartByType("Arm");
            Assert.IsNotNull(arm);
            Assert.AreEqual("Arm", arm.Type);
        }

        [Test]
        public void GetPartsByType_ReturnsAllMatches()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var arms = root.GetPartsByType("Arm");
            Assert.AreEqual(2, arms.Count);
        }

        // ========================
        // Body Part - Laterality
        // ========================

        [Test]
        public void Laterality_GetAdjective_ReturnsCorrectString()
        {
            Assert.AreEqual("left", Laterality.GetAdjective(Laterality.LEFT));
            Assert.AreEqual("right", Laterality.GetAdjective(Laterality.RIGHT));
            Assert.AreEqual("upper", Laterality.GetAdjective(Laterality.UPPER));
            Assert.AreEqual("lower", Laterality.GetAdjective(Laterality.LOWER));
        }

        [Test]
        public void Laterality_Match_HandlesCompoundLaterality()
        {
            int foreLeft = Laterality.FORE | Laterality.LEFT;
            Assert.IsTrue(Laterality.Match(foreLeft, Laterality.LEFT));
            Assert.IsTrue(Laterality.Match(foreLeft, Laterality.FORE));
            Assert.IsFalse(Laterality.Match(foreLeft, Laterality.RIGHT));
        }

        [Test]
        public void Laterality_ANY_MatchesEverything()
        {
            Assert.IsTrue(Laterality.Match(Laterality.LEFT, Laterality.ANY));
            Assert.IsTrue(Laterality.Match(0, Laterality.ANY));
        }

        [Test]
        public void GetDisplayName_IncludesLaterality()
        {
            var part = new BodyPart { Type = "Hand", Name = "hand" };
            part.SetLaterality(Laterality.LEFT);
            Assert.AreEqual("left hand", part.GetDisplayName());
        }

        // ========================
        // Body Part - Flags
        // ========================

        [Test]
        public void Flags_SetAndClear()
        {
            var part = new BodyPart();
            Assert.IsFalse(part.Appendage);
            part.Appendage = true;
            Assert.IsTrue(part.Appendage);
            part.Appendage = false;
            Assert.IsFalse(part.Appendage);
        }

        [Test]
        public void Flags_MultipleFlagsCoexist()
        {
            var part = new BodyPart();
            part.Appendage = true;
            part.Mortal = true;
            part.Primary = true;
            Assert.IsTrue(part.Appendage);
            Assert.IsTrue(part.Mortal);
            Assert.IsTrue(part.Primary);

            part.Mortal = false;
            Assert.IsTrue(part.Appendage);
            Assert.IsFalse(part.Mortal);
            Assert.IsTrue(part.Primary);
        }

        // ========================
        // Body Part - Severability
        // ========================

        [Test]
        public void IsSeverable_AppendageNonIntegral_True()
        {
            var part = new BodyPart { Type = "Arm", Name = "arm" };
            part.Appendage = true;
            Assert.IsTrue(part.IsSeverable());
        }

        [Test]
        public void IsSeverable_Abstract_False()
        {
            var part = new BodyPart { Type = "Thrown Weapon", Name = "thrown weapon" };
            part.Abstract = true;
            part.Appendage = true;
            Assert.IsFalse(part.IsSeverable());
        }

        [Test]
        public void IsSeverable_Integral_False()
        {
            var part = new BodyPart { Type = "Arm", Name = "arm" };
            part.Appendage = true;
            part.Integral = true;
            Assert.IsFalse(part.IsSeverable());
        }

        [Test]
        public void IsSeverable_WithDependsOn_False()
        {
            var part = new BodyPart { Type = "Hands", Name = "hands" };
            part.Appendage = true;
            part.DependsOn = "left hand";
            Assert.IsFalse(part.IsSeverable());
        }

        // ========================
        // Body Part - Equipment
        // ========================

        [Test]
        public void SetEquipped_SetsFirstSlotFlag()
        {
            var part = new BodyPart { Type = "Hand", Name = "hand" };
            var item = new Entity { BlueprintName = "Sword" };
            part.SetEquipped(item);

            Assert.AreEqual(item, part.Equipped);
            Assert.IsTrue(part.FirstSlotForEquipped);
        }

        [Test]
        public void ClearEquipped_ClearsItem()
        {
            var part = new BodyPart { Type = "Hand", Name = "hand" };
            var item = new Entity { BlueprintName = "Sword" };
            part.SetEquipped(item);
            part.ClearEquipped();

            Assert.IsNull(part.Equipped);
            Assert.IsFalse(part.FirstSlotForEquipped);
        }

        [Test]
        public void FindFreeSlot_ReturnsUnoccupied()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var freeHand = root.FindFreeSlot("Hand");
            Assert.IsNotNull(freeHand);
            Assert.AreEqual("Hand", freeHand.Type);
        }

        [Test]
        public void FindFreeSlot_SkipsOccupied()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var hands = root.GetPartsByType("Hand");
            var item = new Entity { BlueprintName = "Sword" };
            hands[0].SetEquipped(item);

            var freeHand = root.FindFreeSlot("Hand");
            Assert.IsNotNull(freeHand);
            Assert.AreNotEqual(hands[0], freeHand);
        }

        [Test]
        public void FindFreeSlot_WithLaterality()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var freeLeft = root.FindFreeSlot("Hand", Laterality.LEFT);
            Assert.IsNotNull(freeLeft);
            Assert.IsTrue(Laterality.Match(freeLeft.GetLaterality(), Laterality.LEFT));
        }

        [Test]
        public void GetEquippableSlots_ReturnsAllMatchingType()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var handSlots = root.GetEquippableSlots("Hand");
            Assert.AreEqual(2, handSlots.Count);
        }

        [Test]
        public void ForeachEquippedObject_OnlyCallsForFirstSlot()
        {
            var root = AnatomyFactory.CreateHumanoid();
            var hands = root.GetPartsByType("Hand");
            var item = new Entity { BlueprintName = "TwoHandedSword" };

            // Simulate multi-slot equip: same item on both hands
            hands[0]._Equipped = item;
            hands[0].FirstSlotForEquipped = true;
            hands[1]._Equipped = item;
            hands[1].FirstSlotForEquipped = false;

            int callCount = 0;
            root.ForeachEquippedObject((e, bp) => callCount++);
            Assert.AreEqual(1, callCount, "Should only call once for multi-slot item");
        }

        // ========================
        // Body Part - Clone
        // ========================

        [Test]
        public void Clone_CopiesFieldsButNotChildren()
        {
            var original = new BodyPart
            {
                Type = "Arm",
                Name = "left arm",
                Mobility = 1
            };
            original.SetLaterality(Laterality.LEFT);
            original.Appendage = true;
            original.AddPart(new BodyPart { Type = "Hand", Name = "hand" });

            var clone = original.Clone();
            Assert.AreEqual("Arm", clone.Type);
            Assert.AreEqual("left arm", clone.Name);
            Assert.AreEqual(Laterality.LEFT, clone.GetLaterality());
            Assert.IsTrue(clone.Appendage);
            Assert.IsNull(clone.Parts); // Children not copied
        }

        // ========================
        // Body Part - Manager Operations
        // ========================

        [Test]
        public void RemovePartsByManager_RemovesTaggedParts()
        {
            var root = AnatomyFactory.CreateHumanoid();
            int countBefore = root.GetParts().Count;

            // Add parts with a manager
            var arm = new BodyPart { Type = "Arm", Name = "extra arm", Manager = "TestMutation" };
            root.AddPart(arm);
            var hand = new BodyPart { Type = "Hand", Name = "extra hand", Manager = "TestMutation" };
            arm.AddPart(hand);

            Assert.AreEqual(countBefore + 2, root.GetParts().Count);

            int removed = root.RemovePartsByManager("TestMutation");
            Assert.AreEqual(2, removed);
            Assert.AreEqual(countBefore, root.GetParts().Count);
        }

        [Test]
        public void RemovePartsByManager_IgnoresOtherParts()
        {
            var root = AnatomyFactory.CreateHumanoid();
            int removed = root.RemovePartsByManager("NonExistentManager");
            Assert.AreEqual(0, removed);
        }

        // ========================
        // BodyPartCategory
        // ========================

        [Test]
        public void BodyPartCategory_Constants()
        {
            Assert.AreEqual(1, BodyPartCategory.ANIMAL);
            Assert.AreEqual("Animal", BodyPartCategory.GetName(BodyPartCategory.ANIMAL));
            Assert.AreEqual(BodyPartCategory.ANIMAL, BodyPartCategory.GetCode("Animal"));
        }

        [Test]
        public void BodyPartCategory_IsLiveCategory()
        {
            Assert.IsTrue(BodyPartCategory.IsLiveCategory(BodyPartCategory.ANIMAL));
            Assert.IsFalse(BodyPartCategory.IsLiveCategory(BodyPartCategory.CYBERNETIC));
        }

        // ========================
        // BodyPartType - ApplyTo
        // ========================

        [Test]
        public void BodyPartType_ApplyTo_SetsFields()
        {
            var bpt = new BodyPartType("Head")
            {
                Mortal = true,
                Appendage = true,
                Mobility = 1,
            };

            var part = new BodyPart();
            bpt.ApplyTo(part);

            Assert.AreEqual("Head", part.Type);
            Assert.AreEqual("head", part.Name);
            Assert.IsTrue(part.Mortal);
            Assert.IsTrue(part.Appendage);
            Assert.AreEqual(1, part.Mobility);
        }

        [Test]
        public void BodyPartType_ApplyTo_PreservesUnsetFields()
        {
            var bpt = new BodyPartType("Hand");
            // Don't set Mortal, so it should remain default

            var part = new BodyPart();
            part.Mortal = true; // Set before apply
            bpt.ApplyTo(part);

            // Mortal was not set on the type, so it should remain true
            Assert.IsTrue(part.Mortal);
        }

        // ========================
        // Body (Entity Part) - Core
        // ========================

        [Test]
        public void Body_SetBody_PropagatesReferences()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var root = body.GetBody();

            Assert.IsNotNull(root);
            Assert.AreEqual(body, root.ParentBody);

            var allParts = body.GetParts();
            for (int i = 0; i < allParts.Count; i++)
            {
                Assert.AreEqual(body, allParts[i].ParentBody,
                    $"Part {allParts[i].Type} should reference parent Body");
            }
        }

        [Test]
        public void Body_CountParts_ReturnsCorrectCount()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();

            Assert.AreEqual(2, body.CountParts("Hand"));
            Assert.AreEqual(2, body.CountParts("Arm"));
            Assert.AreEqual(1, body.CountParts("Head"));
        }

        // ========================
        // Body - Dismemberment
        // ========================

        [Test]
        public void Dismember_RemovesPartFromTree()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var arm = body.GetPartByType("Arm");

            Assert.IsTrue(body.Dismember(arm));
            Assert.IsNull(arm.ParentPart);
            Assert.AreEqual(1, body.CountParts("Arm"), "Should have 1 arm remaining");
        }

        [Test]
        public void Dismember_TracksDismemberedPart()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var arm = body.GetPartByType("Arm");

            body.Dismember(arm);
            Assert.AreEqual(1, body.DismemberedParts.Count);
            Assert.AreEqual(arm, body.DismemberedParts[0].Part);
        }

        [Test]
        public void Dismember_CannotDismemberRoot()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var root = body.GetBody();

            Assert.IsFalse(body.Dismember(root));
        }

        [Test]
        public void Dismember_UnequipsSubtree()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var inv = entity.GetPart<InventoryPart>();

            // Equip a weapon to a hand
            var weapon = CreateWeapon();
            inv.AddObject(weapon);
            var leftHand = body.FindFreeSlot("Hand", Laterality.LEFT);
            inv.EquipToBodyPart(weapon, leftHand);

            Assert.IsNotNull(leftHand._Equipped);

            // Find the left arm (parent of left hand)
            var arms = body.GetPartsByType("Arm");
            BodyPart leftArm = null;
            for (int i = 0; i < arms.Count; i++)
            {
                if (Laterality.Match(arms[i].GetLaterality(), Laterality.LEFT))
                {
                    leftArm = arms[i];
                    break;
                }
            }

            // Dismember the arm (should unequip weapon from hand)
            body.Dismember(leftArm);

            // Weapon should be back in carried inventory
            Assert.IsTrue(inv.Objects.Contains(weapon));
        }

        [Test]
        public void Dismember_LogsMessage()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var arm = body.GetPartByType("Arm");

            body.Dismember(arm);

            var msg = MessageLog.GetLast();
            Assert.IsNotNull(msg);
            Assert.IsTrue(msg.Contains("severed"), $"Expected 'severed' in: {msg}");
        }

        // ========================
        // Body - Regeneration
        // ========================

        [Test]
        public void RegenerateLimb_ReattachesDismemberedPart()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var arm = body.GetPartByType("Arm");

            body.Dismember(arm);
            Assert.AreEqual(1, body.CountParts("Arm"));

            Assert.IsTrue(body.RegenerateLimb());
            Assert.AreEqual(2, body.CountParts("Arm"));
            Assert.AreEqual(0, body.DismemberedParts.Count);
        }

        [Test]
        public void RegenerateLimb_PreferredType()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();

            // Dismember both arm and head
            var arm = body.GetPartByType("Arm");
            body.Dismember(arm);
            var feet = body.GetPartByType("Feet");
            body.Dismember(feet);

            Assert.AreEqual(2, body.DismemberedParts.Count);

            // Regenerate specifically "Feet"
            Assert.IsTrue(body.RegenerateLimb("Feet"));
            Assert.AreEqual(1, body.DismemberedParts.Count);
            Assert.IsNotNull(body.GetPartByType("Feet"));
        }

        [Test]
        public void HasRegenerableLimbs_TrueAfterDismember()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();

            Assert.IsFalse(body.HasRegenerableLimbs());

            var arm = body.GetPartByType("Arm");
            body.Dismember(arm);

            Assert.IsTrue(body.HasRegenerableLimbs());
        }

        // ========================
        // Body - Manager Operations
        // ========================

        [Test]
        public void AddPartByManager_SetsManagerAndFlags()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var root = body.GetBody();

            var extra = new BodyPart { Type = "Arm", Name = "extra arm" };
            body.AddPartByManager("TestMut", root, extra);

            Assert.AreEqual("TestMut", extra.Manager);
            Assert.IsTrue(extra.Dynamic);
            Assert.IsTrue(extra.Extrinsic);
        }

        [Test]
        public void RemovePartsByManager_RemovesFromBody()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var root = body.GetBody();

            body.AddPartByManager("TestMut", root, new BodyPart { Type = "Arm", Name = "extra arm" });
            Assert.AreEqual(3, body.CountParts("Arm"));

            int removed = body.RemovePartsByManager("TestMut");
            Assert.AreEqual(1, removed);
            Assert.AreEqual(2, body.CountParts("Arm"));
        }

        // ========================
        // Body - Dependency Cascade
        // ========================

        [Test]
        public void CheckUnsupportedPartLoss_CascadesDependents()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();

            // Hands depend on Hand (concrete dependency: DependsOn="left hand"/"right hand").
            // Get a hand and its parent "Hands" abstract part.
            var leftHand = body.FindFreeSlot("Hand", Laterality.LEFT);
            Assert.IsNotNull(leftHand);

            // Dismember the left hand
            body.Dismember(leftHand);

            // The left Hands (abstract slot with DependsOn="left hand") should also be lost
            var remainingHands = body.GetPartsByType("Hands");
            // Only right Hands should remain
            bool leftHandsRemain = false;
            for (int i = 0; i < remainingHands.Count; i++)
            {
                if (Laterality.Match(remainingHands[i].GetLaterality(), Laterality.LEFT))
                    leftHandsRemain = true;
            }
            Assert.IsFalse(leftHandsRemain, "Left Hands should be lost when left Hand is dismembered");
        }

        // ========================
        // Body - Mobility Penalty
        // ========================

        [Test]
        public void MobilityPenalty_ZeroWhenIntact()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();

            Assert.AreEqual(0, body.CalculateMobilityPenalty());
        }

        [Test]
        public void MobilityPenalty_IncreasesWhenFeetDismembered()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var feet = body.GetPartByType("Feet");

            body.Dismember(feet);

            int penalty = body.CalculateMobilityPenalty();
            Assert.Greater(penalty, 0, "Should have mobility penalty after losing feet");
        }

        // ========================
        // Body - Equipment Integration
        // ========================

        [Test]
        public void EquipToBodyPart_PlacesItemOnPart()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var inv = entity.GetPart<InventoryPart>();

            var weapon = CreateWeapon();
            inv.AddObject(weapon);

            var hand = body.FindFreeSlot("Hand");
            Assert.IsNotNull(hand);

            inv.EquipToBodyPart(weapon, hand);

            Assert.AreEqual(weapon, hand._Equipped);
            Assert.IsTrue(hand.FirstSlotForEquipped);
            Assert.IsFalse(inv.Objects.Contains(weapon), "Should remove from carried list");
        }

        [Test]
        public void EquipToBodyParts_MultiSlot()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var inv = entity.GetPart<InventoryPart>();

            var twoHander = CreateTwoHandedWeapon();
            inv.AddObject(twoHander);

            var hands = body.GetEquippableSlots("Hand");
            Assert.GreaterOrEqual(hands.Count, 2);

            var slotsToUse = new List<BodyPart> { hands[0], hands[1] };
            inv.EquipToBodyParts(twoHander, slotsToUse);

            Assert.AreEqual(twoHander, hands[0]._Equipped);
            Assert.AreEqual(twoHander, hands[1]._Equipped);
            Assert.IsTrue(hands[0].FirstSlotForEquipped);
            Assert.IsFalse(hands[1].FirstSlotForEquipped);
        }

        [Test]
        public void UnequipFromBodyPart_ClearsAllSlots()
        {
            var entity = CreateCreatureWithBody();
            var body = entity.GetPart<Body>();
            var inv = entity.GetPart<InventoryPart>();

            var twoHander = CreateTwoHandedWeapon();
            inv.AddObject(twoHander);

            var hands = body.GetEquippableSlots("Hand");
            var slotsToUse = new List<BodyPart> { hands[0], hands[1] };
            inv.EquipToBodyParts(twoHander, slotsToUse);

            // Unequip from the first body part
            inv.UnequipFromBodyPart(hands[0]);

            Assert.IsNull(hands[0]._Equipped);
            Assert.IsNull(hands[1]._Equipped);
            Assert.IsTrue(inv.Objects.Contains(twoHander));
        }

        // ========================
        // InventorySystem - Body-Part-Aware Equip
        // ========================

        [Test]
        public void InventorySystem_Equip_UsesBodyParts()
        {
            var entity = CreateCreatureWithBody();
            var inv = entity.GetPart<InventoryPart>();
            var body = entity.GetPart<Body>();

            var weapon = CreateWeapon();
            inv.AddObject(weapon);

            Assert.IsTrue(InventorySystem.Equip(entity, weapon));

            // Should be on a Hand body part
            var hand = inv.FindEquippedBodyPart(weapon);
            Assert.IsNotNull(hand, "Weapon should be equipped on a Hand body part");
            Assert.AreEqual("Hand", hand.Type);
        }

        [Test]
        public void InventorySystem_Equip_TwoWeapons_SeparateHands()
        {
            var entity = CreateCreatureWithBody();
            var inv = entity.GetPart<InventoryPart>();
            var body = entity.GetPart<Body>();

            var weapon1 = CreateWeapon();
            var weapon2 = CreateWeapon();
            inv.AddObject(weapon1);
            inv.AddObject(weapon2);

            Assert.IsTrue(InventorySystem.Equip(entity, weapon1));
            Assert.IsTrue(InventorySystem.Equip(entity, weapon2));

            var bp1 = inv.FindEquippedBodyPart(weapon1);
            var bp2 = inv.FindEquippedBodyPart(weapon2);
            Assert.IsNotNull(bp1);
            Assert.IsNotNull(bp2);
            Assert.AreNotEqual(bp1, bp2, "Two weapons should be on different hands");
        }

        [Test]
        public void InventorySystem_Equip_BodyArmor()
        {
            var entity = CreateCreatureWithBody();
            var inv = entity.GetPart<InventoryPart>();
            var body = entity.GetPart<Body>();

            var armor = CreateArmor();
            inv.AddObject(armor);

            Assert.IsTrue(InventorySystem.Equip(entity, armor));

            var bp = inv.FindEquippedBodyPart(armor);
            Assert.IsNotNull(bp);
            Assert.AreEqual("Body", bp.Type);
        }

        [Test]
        public void InventorySystem_UnequipItem_ReturnsToInventory()
        {
            var entity = CreateCreatureWithBody();
            var inv = entity.GetPart<InventoryPart>();

            var weapon = CreateWeapon();
            inv.AddObject(weapon);
            InventorySystem.Equip(entity, weapon);

            Assert.IsTrue(InventorySystem.UnequipItem(entity, weapon));
            Assert.IsTrue(inv.Objects.Contains(weapon));
        }

        // ========================
        // CombatSystem - Multi-Weapon
        // ========================

        [Test]
        public void CombatSystem_GetDV_BodyPartAware()
        {
            var entity = CreateCreatureWithBody();
            var inv = entity.GetPart<InventoryPart>();

            int baseDV = CombatSystem.GetDV(entity);

            // Equip armor with DV bonus
            var armor = CreateArmor(dv: 2);
            inv.AddObject(armor);
            InventorySystem.Equip(entity, armor);

            int armoredDV = CombatSystem.GetDV(entity);
            Assert.AreEqual(baseDV + 2, armoredDV);
        }

        [Test]
        public void CombatSystem_GetAV_BodyPartAware()
        {
            var entity = CreateCreatureWithBody();
            var inv = entity.GetPart<InventoryPart>();

            // Equip armor with AV
            var armor = CreateArmor(av: 3);
            inv.AddObject(armor);
            InventorySystem.Equip(entity, armor);

            int av = CombatSystem.GetAV(entity);
            Assert.GreaterOrEqual(av, 3, "AV should include equipped armor");
        }

        // ========================
        // EntityFactory - Anatomy Initialization
        // ========================

        [Test]
        public void EntityFactory_CreatesBodyFromBlueprint()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(GetTestBlueprintJson());

            var creature = factory.CreateEntity("TestCreature");
            var body = creature.GetPart<Body>();

            Assert.IsNotNull(body, "Creature with Body part should have Body initialized");
            Assert.IsNotNull(body.GetBody(), "Body tree should be initialized");

            var root = body.GetBody();
            Assert.AreEqual("Body", root.Type);
            Assert.AreEqual(2, body.CountParts("Hand"), "Humanoid should have 2 hands");
        }

        [Test]
        public void EntityFactory_QuadrupedAnatomy()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(GetTestBlueprintJson());

            var creature = factory.CreateEntity("TestQuadruped");
            var body = creature.GetPart<Body>();

            Assert.IsNotNull(body);
            Assert.AreEqual(4, body.CountParts("Feet"), "Quadruped should have 4 feet");
        }

        [Test]
        public void EntityFactory_NoBody_LegacyStillWorks()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(GetTestBlueprintJson());

            var item = factory.CreateEntity("TestItem");
            Assert.IsNull(item.GetPart<Body>(), "Item should not have Body part");
        }

        // ========================
        // Helpers
        // ========================

        private Entity CreateCreatureWithBody()
        {
            var entity = new Entity();
            entity.BlueprintName = "TestCreature";
            entity.Tags["Creature"] = "";

            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Owner = entity, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Owner = entity, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };

            entity.AddPart(new RenderPart { DisplayName = "test creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });

            var body = new Body();
            entity.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());

            return entity;
        }

        private Entity CreateWeapon()
        {
            var entity = new Entity();
            entity.BlueprintName = "TestWeapon";
            entity.Tags["Item"] = "";
            entity.AddPart(new RenderPart { DisplayName = "test weapon" });
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d6", PenBonus = 1 });
            entity.AddPart(new EquippablePart { Slot = "Hand" });
            return entity;
        }

        private Entity CreateTwoHandedWeapon()
        {
            var entity = new Entity();
            entity.BlueprintName = "TestTwoHander";
            entity.Tags["Item"] = "";
            entity.AddPart(new RenderPart { DisplayName = "test two-hander" });
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 12 });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "2d6", PenBonus = 2 });
            entity.AddPart(new EquippablePart { Slot = "Hand", UsesSlots = "Hand,Hand" });
            return entity;
        }

        private Entity CreateArmor(int av = 3, int dv = 0)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestArmor";
            entity.Tags["Item"] = "";
            entity.AddPart(new RenderPart { DisplayName = "test armor" });
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 15 });
            entity.AddPart(new ArmorPart { AV = av, DV = dv });
            entity.AddPart(new EquippablePart { Slot = "Body" });
            return entity;
        }

        private string GetTestBlueprintJson()
        {
            return @"{
                ""Objects"": [
                    {
                        ""Name"": ""PhysicalObject"",
                        ""Parts"": [
                            { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""?"" }, { ""Key"": ""ColorString"", ""Value"": ""&y"" }] },
                            { ""Name"": ""Physics"", ""Params"": [] }
                        ],
                        ""Stats"": [],
                        ""Tags"": []
                    },
                    {
                        ""Name"": ""TestItem"",
                        ""Inherits"": ""PhysicalObject"",
                        ""Parts"": [
                            { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Takeable"", ""Value"": ""true"" }] }
                        ],
                        ""Stats"": [],
                        ""Tags"": [{ ""Key"": ""Item"", ""Value"": """" }]
                    },
                    {
                        ""Name"": ""TestCreature"",
                        ""Inherits"": ""PhysicalObject"",
                        ""Parts"": [
                            { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""DisplayName"", ""Value"": ""test creature"" }] },
                            { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] },
                            { ""Name"": ""Body"", ""Params"": [] },
                            { ""Name"": ""Inventory"", ""Params"": [{ ""Key"": ""MaxWeight"", ""Value"": ""150"" }] }
                        ],
                        ""Stats"": [
                            { ""Name"": ""Hitpoints"", ""Value"": 20, ""Min"": 0, ""Max"": 20 },
                            { ""Name"": ""Strength"", ""Value"": 16, ""Min"": 1, ""Max"": 50 },
                            { ""Name"": ""Agility"", ""Value"": 16, ""Min"": 1, ""Max"": 50 },
                            { ""Name"": ""Speed"", ""Value"": 100, ""Min"": 25, ""Max"": 200 }
                        ],
                        ""Tags"": [{ ""Key"": ""Creature"", ""Value"": """" }]
                    },
                    {
                        ""Name"": ""TestQuadruped"",
                        ""Inherits"": ""PhysicalObject"",
                        ""Parts"": [
                            { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""DisplayName"", ""Value"": ""test quadruped"" }] },
                            { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] },
                            { ""Name"": ""Body"", ""Params"": [] }
                        ],
                        ""Stats"": [
                            { ""Name"": ""Hitpoints"", ""Value"": 15, ""Min"": 0, ""Max"": 15 }
                        ],
                        ""Tags"": [{ ""Key"": ""Creature"", ""Value"": """" }],
                        ""Props"": [{ ""Key"": ""Anatomy"", ""Value"": ""Quadruped"" }]
                    }
                ]
            }";
        }
    }
}
