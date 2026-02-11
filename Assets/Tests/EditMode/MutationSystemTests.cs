using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class MutationSystemTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            MutationRegistry.ResetForTests();
        }

        // ========================
        // Helper Methods
        // ========================

        private Entity CreateCreature(int hp = 20, int strength = 18, int agility = 18, int ego = 10)
        {
            var entity = new Entity { BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = agility, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = ego, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "test creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d2" });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            return entity;
        }

        private Entity CreateCreatureWithMutationSupport(int hp = 20, int ego = 10)
        {
            var entity = CreateCreature(hp: hp, ego: ego);
            entity.AddPart(new ActivatedAbilitiesPart());
            entity.AddPart(new MutationsPart());
            return entity;
        }

        private Zone CreateSimpleZone()
        {
            return new Zone("TestZone");
        }

        // ========================
        // ActivatedAbility (Data Class)
        // ========================

        [Test]
        public void ActivatedAbility_IsUsable_WhenNoCooldown()
        {
            var ability = new ActivatedAbility
            {
                ID = Guid.NewGuid(),
                DisplayName = "Test",
                Command = "CommandTest",
                CooldownRemaining = 0
            };
            Assert.IsTrue(ability.IsUsable);
        }

        [Test]
        public void ActivatedAbility_NotUsable_WhenOnCooldown()
        {
            var ability = new ActivatedAbility
            {
                ID = Guid.NewGuid(),
                DisplayName = "Test",
                Command = "CommandTest",
                CooldownRemaining = 5
            };
            Assert.IsFalse(ability.IsUsable);
        }

        // ========================
        // ActivatedAbilitiesPart
        // ========================

        [Test]
        public void ActivatedAbilitiesPart_AddAbility_ReturnsGuid()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Flaming Hands", "CommandFlamingHands", "Physical Mutations");
            Assert.AreNotEqual(Guid.Empty, id);
            Assert.AreEqual(1, part.AbilityList.Count);
        }

        [Test]
        public void ActivatedAbilitiesPart_GetAbility_ReturnsByGuid()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "CommandTest", "Test");
            var ability = part.GetAbility(id);
            Assert.IsNotNull(ability);
            Assert.AreEqual("Test", ability.DisplayName);
            Assert.AreEqual("CommandTest", ability.Command);
        }

        [Test]
        public void ActivatedAbilitiesPart_GetAbilityBySlot_ReturnsCorrectOrder()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            part.AddAbility("First", "Cmd1", "Class");
            part.AddAbility("Second", "Cmd2", "Class");

            Assert.AreEqual("First", part.GetAbilityBySlot(0).DisplayName);
            Assert.AreEqual("Second", part.GetAbilityBySlot(1).DisplayName);
            Assert.IsNull(part.GetAbilityBySlot(2));
            Assert.IsNull(part.GetAbilityBySlot(-1));
        }

        [Test]
        public void ActivatedAbilitiesPart_RemoveAbility_RemovesFromBoth()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "Cmd", "Class");
            Assert.IsTrue(part.RemoveAbility(id));
            Assert.AreEqual(0, part.AbilityList.Count);
            Assert.IsNull(part.GetAbility(id));
        }

        [Test]
        public void ActivatedAbilitiesPart_RemoveAbility_ReturnsFalseForUnknown()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            Assert.IsFalse(part.RemoveAbility(Guid.NewGuid()));
        }

        [Test]
        public void ActivatedAbilitiesPart_CooldownAbility_SetsCooldown()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "Cmd", "Class");
            part.CooldownAbility(id, 10);

            var ability = part.GetAbility(id);
            Assert.AreEqual(10, ability.CooldownRemaining);
            Assert.AreEqual(10, ability.MaxCooldown);
            Assert.IsFalse(ability.IsUsable);
        }

        [Test]
        public void ActivatedAbilitiesPart_TickCooldowns_ReducesByOne()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "Cmd", "Class");
            part.CooldownAbility(id, 3);

            part.TickCooldowns();
            Assert.AreEqual(2, part.GetAbility(id).CooldownRemaining);

            part.TickCooldowns();
            Assert.AreEqual(1, part.GetAbility(id).CooldownRemaining);

            part.TickCooldowns();
            Assert.AreEqual(0, part.GetAbility(id).CooldownRemaining);
            Assert.IsTrue(part.GetAbility(id).IsUsable);
        }

        [Test]
        public void ActivatedAbilitiesPart_TickCooldowns_DoesNotGoNegative()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "Cmd", "Class");
            // Already at 0

            part.TickCooldowns();
            Assert.AreEqual(0, part.GetAbility(id).CooldownRemaining);
        }

        [Test]
        public void ActivatedAbilitiesPart_EndTurnEvent_TicksCooldowns()
        {
            var part = new ActivatedAbilitiesPart();
            var entity = new Entity();
            entity.AddPart(part);

            var id = part.AddAbility("Test", "Cmd", "Class");
            part.CooldownAbility(id, 5);

            entity.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(4, part.GetAbility(id).CooldownRemaining);
        }

        // ========================
        // BaseMutation
        // ========================

        // Concrete test mutation for testing base class behavior
        private class TestMutation : BaseMutation
        {
            public override string MutationType => "Physical";
            public override string DisplayName => "Test Mutation";
            public bool MutateCalled;
            public bool UnmutateCalled;

            public override void Mutate(Entity entity, int level)
            {
                base.Mutate(entity, level);
                MutateCalled = true;
            }

            public override void Unmutate(Entity entity)
            {
                UnmutateCalled = true;
                base.Unmutate(entity);
            }
        }

        private class RankedTestMutation : BaseMutation, IRankedMutation
        {
            public override string MutationType => "Physical";
            public override string DisplayName => "Ranked Test Mutation";
            public int Rank = 1;

            public int GetRank()
            {
                return Rank;
            }

            public int AdjustRank(int amount)
            {
                Rank += amount;
                if (Rank < 1) Rank = 1;
                return Rank;
            }
        }

        private class TrackingMutation : BaseMutation
        {
            public override string MutationType => "Physical";
            public override string DisplayName => "Tracking Mutation";
            public bool IsLevelable = true;
            public int MutateCalls;
            public int UnmutateCalls;
            public int ChangeLevelCalls;

            public override bool CanLevel()
            {
                return IsLevelable;
            }

            public override void Mutate(Entity entity, int level)
            {
                MutateCalls++;
                base.Mutate(entity, level);
            }

            public override void Unmutate(Entity entity)
            {
                UnmutateCalls++;
                base.Unmutate(entity);
            }

            public override bool ChangeLevel(int newLevel)
            {
                ChangeLevelCalls++;
                return base.ChangeLevel(newLevel);
            }
        }

        private class MPEventProbePart : Part
        {
            public int GainedCalls;
            public int UsedCalls;
            public int LastGainedAmount;
            public int LastUsedAmount;
            public string LastContext;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "GainedMP")
                {
                    GainedCalls++;
                    LastGainedAmount = e.GetIntParameter("Amount");
                }
                else if (e.ID == "UsedMP")
                {
                    UsedCalls++;
                    LastUsedAmount = e.GetIntParameter("Amount");
                    LastContext = e.GetStringParameter("Context");
                }
                return true;
            }
        }

        private class RandomBuyOverridePart : Part
        {
            public int? MutationCountOverride;
            public int? ChimericRollOverride;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "GetRandomBuyMutationCount" && MutationCountOverride.HasValue)
                {
                    e.SetParameter("Amount", MutationCountOverride.Value);
                }
                else if (e.ID == "GetRandomBuyChimericBodyPartRolls" && ChimericRollOverride.HasValue)
                {
                    e.SetParameter("Amount", ChimericRollOverride.Value);
                }

                return true;
            }
        }

        private class ChimericHookProbePart : Part
        {
            public int Calls;
            public string LastMutationClass;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "RandomBuyChimericBodyPartGranted")
                {
                    Calls++;
                    LastMutationClass = e.GetStringParameter("MutationClassName");
                }
                return true;
            }
        }

        private class BodyRebuildProbePart : Part
        {
            public int Calls;
            public int LastMutationCount;
            public string LastReason;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "MutationBodyRebuild")
                {
                    Calls++;
                    LastMutationCount = e.GetIntParameter("MutationCount");
                    LastReason = e.GetStringParameter("Reason");
                }

                return true;
            }
        }

        private class BodyAffectingTestMutation : BaseMutation
        {
            public override string MutationType => "Physical";
            public override string DisplayName => "Body-Affecting Test Mutation";
            public override bool AffectsBodyParts => true;
            public override bool GeneratesEquipment => true;

            public int MutateCalls;
            public int UnmutateCalls;
            public int BeforeBodyRebuildCalls;
            public int AfterBodyRebuildCalls;
            public bool AutoRemoveOnMutationLoss = true;
            public Entity GeneratedItem;

            public override void Mutate(Entity entity, int level)
            {
                MutateCalls++;
                base.Mutate(entity, level);

                if (GeneratedItem == null)
                    GeneratedItem = CreateGeneratedItem();

                RegisterGeneratedEquipment(
                    GeneratedItem,
                    autoEquip: true,
                    autoRemoveOnMutationLoss: AutoRemoveOnMutationLoss);
            }

            public override void Unmutate(Entity entity)
            {
                UnmutateCalls++;
                base.Unmutate(entity);
            }

            public override void OnBeforeBodyRebuild(Entity entity, string reason)
            {
                BeforeBodyRebuildCalls++;
            }

            public override void OnAfterBodyRebuild(Entity entity, string reason)
            {
                AfterBodyRebuildCalls++;
            }

            private static Entity CreateGeneratedItem()
            {
                var item = new Entity { BlueprintName = "MutationGear" };
                item.AddPart(new RenderPart { DisplayName = "mutation gear" });
                item.AddPart(new PhysicsPart { Takeable = true, Weight = 0 });
                item.AddPart(new EquippablePart { Slot = "Hand" });
                return item;
            }
        }

        [Test]
        public void BaseMutation_Mutate_SetsBaseLevel()
        {
            var mutation = new TestMutation();
            var entity = CreateCreatureWithMutationSupport();

            entity.AddPart(mutation);
            mutation.Mutate(entity, 3);

            Assert.AreEqual(3, mutation.BaseLevel);
            Assert.AreEqual(3, mutation.Level);
            Assert.IsTrue(mutation.MutateCalled);
        }

        [Test]
        public void BaseMutation_DefaultLevel_IsOne()
        {
            var mutation = new TestMutation();
            Assert.AreEqual(1, mutation.BaseLevel);
            Assert.AreEqual(1, mutation.Level);
        }

        [Test]
        public void BaseMutation_Phase7Defaults_AreNoOp()
        {
            var mutation = new TestMutation();
            var entity = CreateCreatureWithMutationSupport();

            Assert.IsFalse(mutation.AffectsBodyParts);
            Assert.IsFalse(mutation.GeneratesEquipment);

            mutation.OnBeforeBodyRebuild(entity, "BodyChanged");
            mutation.OnAfterBodyRebuild(entity, "BodyChanged");
        }

        [Test]
        public void BaseMutation_GetMutationCapForLevel_UsesQudFormula()
        {
            Assert.AreEqual(1, BaseMutation.GetMutationCapForLevel(1));
            Assert.AreEqual(2, BaseMutation.GetMutationCapForLevel(2));
            Assert.AreEqual(3, BaseMutation.GetMutationCapForLevel(5));
            Assert.AreEqual(6, BaseMutation.GetMutationCapForLevel(10));
        }

        [Test]
        public void BaseMutation_CalcLevel_RespectsLevelCap_WhenLevelStatExists()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 2, Min = 1, Max = 99 };

            var mutation = new TestMutation();
            var mutations = entity.GetPart<MutationsPart>();
            mutations.AddMutation(mutation, 5);

            // L2 cap is 2/2 + 1 = 2.
            Assert.AreEqual(2, mutation.Level);
        }

        [Test]
        public void BaseMutation_RapidLevel_IncreasesEffectiveLevel()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 20, Min = 1, Max = 99 };

            var mutation = new TestMutation();
            var mutations = entity.GetPart<MutationsPart>();
            mutations.AddMutation(mutation, 1);

            mutation.RapidLevel(2);

            Assert.AreEqual(2, mutation.GetRapidLevelAmount());
            Assert.AreEqual(3, mutation.Level);
        }

        [Test]
        public void BaseMutation_CanIncreaseLevel_StopsAtMaxLevel()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 30, Min = 1, Max = 99 };

            var mutation = new TestMutation();
            var mutations = entity.GetPart<MutationsPart>();
            mutations.AddMutation(mutation, 10);

            Assert.IsFalse(mutation.CanIncreaseLevel());
        }

        [Test]
        public void MutationRegistry_InitializeFromJson_LoadsDefinitions()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [{ ""Name"": ""Physical"", ""DisplayName"": ""Physical"" }],
              ""Mutations"": [{
                ""Name"": ""Test"",
                ""ClassName"": ""TestMutation"",
                ""Category"": ""Physical"",
                ""MaxLevel"": 7
              }]
            }");

            Assert.IsTrue(MutationRegistry.TryGetByClassName("TestMutation", out var definition));
            Assert.AreEqual(7, definition.MaxLevel);
            Assert.AreEqual(7, MutationRegistry.GetMaxLevelForClass("TestMutation", 10));
        }

        // ========================
        // MutationsPart
        // ========================

        [Test]
        public void MutationsPart_AddMutation_AddsToList()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            var mutation = new TestMutation();
            Assert.IsTrue(mutations.AddMutation(mutation, 2));
            Assert.AreEqual(1, mutations.MutationList.Count);
            Assert.AreEqual(2, mutation.BaseLevel);
            Assert.IsTrue(mutation.MutateCalled);
        }

        [Test]
        public void MutationsPart_AddMutation_PreventsDuplicates()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            Assert.IsTrue(mutations.AddMutation(new TestMutation(), 1));
            Assert.IsFalse(mutations.AddMutation(new TestMutation(), 1));
            Assert.AreEqual(1, mutations.MutationList.Count);
        }

        [Test]
        public void MutationsPart_RemoveMutation_RemovesAndCallsUnmutate()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            var mutation = new TestMutation();
            mutations.AddMutation(mutation, 1);
            Assert.IsTrue(mutations.RemoveMutation(mutation));
            Assert.AreEqual(0, mutations.MutationList.Count);
            Assert.IsTrue(mutation.UnmutateCalled);
        }

        [Test]
        public void MutationsPart_RemoveMutation_ReturnsFalseIfNotPresent()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            Assert.IsFalse(mutations.RemoveMutation(new TestMutation()));
        }

        [Test]
        public void MutationsPart_GetMutation_FindsByType()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            mutations.AddMutation(new TestMutation(), 1);
            Assert.IsNotNull(mutations.GetMutation<TestMutation>());
        }

        [Test]
        public void MutationsPart_HasMutation_ReturnsCorrectly()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            Assert.IsFalse(mutations.HasMutation<TestMutation>());
            mutations.AddMutation(new TestMutation(), 1);
            Assert.IsTrue(mutations.HasMutation<TestMutation>());
        }

        [Test]
        public void MutationsPart_AddMutation_AddsPartToEntity()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            mutations.AddMutation(new TestMutation(), 1);
            Assert.IsTrue(entity.HasPart<TestMutation>());
        }

        [Test]
        public void MutationsPart_RemoveMutation_RemovesPartFromEntity()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            var mutation = new TestMutation();
            mutations.AddMutation(mutation, 1);
            mutations.RemoveMutation(mutation);
            Assert.IsFalse(entity.HasPart<TestMutation>());
        }

        [Test]
        public void MutationsPart_MutationGeneratedEquipment_IsTrackedAndAutoEquipped()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var inventory = entity.GetPart<InventoryPart>();
            var mutation = new BodyAffectingTestMutation();

            Assert.IsTrue(mutations.AddMutation(mutation, 1));
            Assert.AreEqual(1, mutations.MutationGeneratedEquipment.Count);
            Assert.IsNotNull(mutation.GeneratedItem);
            Assert.AreEqual(mutation.GeneratedItem, inventory.GetEquipped("Hand"));
            Assert.AreEqual(entity, mutation.GeneratedItem.GetPart<PhysicsPart>()?.Equipped);
        }

        [Test]
        public void MutationsPart_RemoveMutation_CleansGeneratedEquipment_ByDefault()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var inventory = entity.GetPart<InventoryPart>();
            var mutation = new BodyAffectingTestMutation();

            Assert.IsTrue(mutations.AddMutation(mutation, 1));
            Entity item = mutation.GeneratedItem;
            Assert.IsNotNull(item);
            Assert.IsTrue(inventory.Contains(item));

            Assert.IsTrue(mutations.RemoveMutation(mutation));
            Assert.AreEqual(0, mutations.MutationGeneratedEquipment.Count);
            Assert.IsFalse(inventory.Contains(item));
            Assert.IsNull(item.GetPart<PhysicsPart>()?.InInventory);
            Assert.IsNull(item.GetPart<PhysicsPart>()?.Equipped);
        }

        [Test]
        public void MutationsPart_RemoveMutation_CanLeaveGeneratedEquipment_WhenConfigured()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var inventory = entity.GetPart<InventoryPart>();
            var mutation = new BodyAffectingTestMutation { AutoRemoveOnMutationLoss = false };

            Assert.IsTrue(mutations.AddMutation(mutation, 1));
            Entity item = mutation.GeneratedItem;
            Assert.IsNotNull(item);

            Assert.IsTrue(mutations.RemoveMutation(mutation));
            Assert.AreEqual(0, mutations.MutationGeneratedEquipment.Count);
            Assert.IsTrue(inventory.Contains(item));
        }

        [Test]
        public void MutationsPart_BodyChanged_RebuildsOnlyBodyAffectingMutations()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var probe = new BodyRebuildProbePart();
            entity.AddPart(probe);

            var bodyMutation = new BodyAffectingTestMutation();
            var regularMutation = new TrackingMutation();
            Assert.IsTrue(mutations.AddMutation(bodyMutation, 1));
            Assert.IsTrue(mutations.AddMutation(regularMutation, 1));

            int regularMutateCalls = regularMutation.MutateCalls;
            int regularUnmutateCalls = regularMutation.UnmutateCalls;

            var bodyChanged = GameEvent.New("BodyChanged");
            bodyChanged.SetParameter("Reason", "Phase7Test");
            entity.FireEvent(bodyChanged);

            Assert.AreEqual(1, probe.Calls);
            Assert.AreEqual("Phase7Test", probe.LastReason);
            Assert.AreEqual(1, probe.LastMutationCount);
            Assert.AreEqual(1, bodyMutation.BeforeBodyRebuildCalls);
            Assert.AreEqual(1, bodyMutation.AfterBodyRebuildCalls);
            Assert.GreaterOrEqual(bodyMutation.UnmutateCalls, 1);
            Assert.GreaterOrEqual(bodyMutation.MutateCalls, 2);
            Assert.AreEqual(regularMutateCalls, regularMutation.MutateCalls);
            Assert.AreEqual(regularUnmutateCalls, regularMutation.UnmutateCalls);
            Assert.AreEqual(1, mutations.MutationGeneratedEquipment.Count);
        }

        [Test]
        public void ExtraArmPrototypeMutation_Mutate_EquipsExtraLimbAndAppliesStrengthBonus()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var inventory = entity.GetPart<InventoryPart>();
            var strength = entity.GetStat("Strength");

            Assert.AreEqual(0, strength.Bonus);
            Assert.IsTrue(mutations.AddMutation(new ExtraArmPrototypeMutation(), 1));

            Entity equipped = inventory.GetEquipped(ExtraArmPrototypeMutation.EXTRA_LIMB_SLOT);
            Assert.IsNotNull(equipped);
            Assert.AreEqual("mutant extra arm", equipped.GetDisplayName());
            Assert.AreEqual(entity, equipped.GetPart<PhysicsPart>()?.Equipped);
            Assert.AreEqual(ExtraArmPrototypeMutation.STRENGTH_BONUS, equipped.GetPart<EquippablePart>()?.EquipBonuses);
            Assert.AreEqual(1, strength.Bonus);
            Assert.AreEqual(1, mutations.MutationGeneratedEquipment.Count);
        }

        [Test]
        public void ExtraArmPrototypeMutation_RemoveMutation_CleansEquipmentAndBonus()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var inventory = entity.GetPart<InventoryPart>();
            var strength = entity.GetStat("Strength");

            var mutation = new ExtraArmPrototypeMutation();
            Assert.IsTrue(mutations.AddMutation(mutation, 1));
            Entity equipped = inventory.GetEquipped(ExtraArmPrototypeMutation.EXTRA_LIMB_SLOT);
            Assert.IsNotNull(equipped);
            Assert.AreEqual(1, strength.Bonus);

            Assert.IsTrue(mutations.RemoveMutation(mutation));
            Assert.IsNull(inventory.GetEquipped(ExtraArmPrototypeMutation.EXTRA_LIMB_SLOT));
            Assert.IsFalse(inventory.Contains(equipped));
            Assert.AreEqual(0, strength.Bonus);
            Assert.AreEqual(0, mutations.MutationGeneratedEquipment.Count);
        }

        [Test]
        public void ExtraArmPrototypeMutation_BodyChanged_RebuildsWithoutEquipmentDuplication()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var inventory = entity.GetPart<InventoryPart>();
            var strength = entity.GetStat("Strength");

            var mutation = new ExtraArmPrototypeMutation();
            Assert.IsTrue(mutations.AddMutation(mutation, 1));
            Entity firstEquip = inventory.GetEquipped(ExtraArmPrototypeMutation.EXTRA_LIMB_SLOT);
            Assert.IsNotNull(firstEquip);
            Assert.AreEqual(1, strength.Bonus);
            Assert.AreEqual(1, mutations.MutationGeneratedEquipment.Count);

            var changed = GameEvent.New("BodyChanged");
            changed.SetParameter("Reason", "ExtraArmPrototypeTest");
            entity.FireEvent(changed);

            Entity rebuiltEquip = inventory.GetEquipped(ExtraArmPrototypeMutation.EXTRA_LIMB_SLOT);
            Assert.IsNotNull(rebuiltEquip);
            Assert.AreSame(firstEquip, rebuiltEquip);
            Assert.AreEqual(1, strength.Bonus);
            Assert.AreEqual(1, mutations.MutationGeneratedEquipment.Count);
        }

        [Test]
        public void MutationsPart_LevelMutation_UpdatesBaseAndEffectiveLevel()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var mutation = new TestMutation();

            mutations.AddMutation(mutation, 1);
            Assert.IsTrue(mutations.LevelMutation(mutation, 4));
            Assert.AreEqual(4, mutation.BaseLevel);
            Assert.AreEqual(4, mutation.Level);
        }

        [Test]
        public void Entity_GainMP_UsesBaseValue_AndFiresEvent()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["MP"] = new Stat { Name = "MP", BaseValue = 3, Min = 0, Max = 999 };
            var probe = new MPEventProbePart();
            entity.AddPart(probe);

            Assert.IsTrue(entity.GainMP(2));
            var mp = entity.GetStat("MP");
            Assert.AreEqual(5, mp.BaseValue);
            Assert.AreEqual(5, mp.Value);
            Assert.AreEqual(1, probe.GainedCalls);
            Assert.AreEqual(2, probe.LastGainedAmount);
        }

        [Test]
        public void Entity_UseMP_UsesPenalty_AndFiresContextEvent()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["MP"] = new Stat { Name = "MP", BaseValue = 5, Min = 0, Max = 999 };
            var probe = new MPEventProbePart();
            entity.AddPart(probe);

            Assert.IsTrue(entity.UseMP(2, "RankUp"));
            var mp = entity.GetStat("MP");
            Assert.AreEqual(5, mp.BaseValue);
            Assert.AreEqual(2, mp.Penalty);
            Assert.AreEqual(3, mp.Value);
            Assert.AreEqual(1, probe.UsedCalls);
            Assert.AreEqual(2, probe.LastUsedAmount);
            Assert.AreEqual("RankUp", probe.LastContext);
        }

        [Test]
        public void Entity_MPMethods_ReturnFalse_WhenMPStatMissing()
        {
            var entity = CreateCreatureWithMutationSupport();
            Assert.IsFalse(entity.CanGainMP());
            Assert.IsFalse(entity.GainMP(1));
            Assert.IsFalse(entity.UseMP(1));
        }

        [Test]
        public void MutationsPart_SpendMPToIncreaseMutation_ConsumesOneMP_AndLevelsUp()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["MP"] = new Stat { Name = "MP", BaseValue = 3, Min = 0, Max = 999 };
            var probe = new MPEventProbePart();
            entity.AddPart(probe);

            var mutations = entity.GetPart<MutationsPart>();
            var mutation = new TestMutation();
            mutations.AddMutation(mutation, 1);

            Assert.IsTrue(mutations.SpendMPToIncreaseMutation(mutation, "RankUp"));
            Assert.AreEqual(2, mutation.BaseLevel);
            Assert.AreEqual(2, mutation.Level);
            Assert.AreEqual(3, entity.GetStat("MP").BaseValue);
            Assert.AreEqual(1, entity.GetStat("MP").Penalty);
            Assert.AreEqual(2, entity.GetStatValue("MP"));
            Assert.AreEqual(1, probe.UsedCalls);
            Assert.AreEqual("RankUp", probe.LastContext);
        }

        [Test]
        public void MutationsPart_SpendMPToIncreaseMutation_FailsWithoutEnoughMP()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["MP"] = new Stat { Name = "MP", BaseValue = 0, Min = 0, Max = 999 };
            var probe = new MPEventProbePart();
            entity.AddPart(probe);

            var mutations = entity.GetPart<MutationsPart>();
            var mutation = new TestMutation();
            mutations.AddMutation(mutation, 1);

            Assert.IsFalse(mutations.SpendMPToIncreaseMutation(mutation, "RankUp"));
            Assert.AreEqual(1, mutation.BaseLevel);
            Assert.AreEqual(1, mutation.Level);
            Assert.AreEqual(0, entity.GetStat("MP").Penalty);
            Assert.AreEqual(0, probe.UsedCalls);
        }

        [Test]
        public void MutationsPart_GetRandomBuyMutationOptions_UsesSelectionCountOverrideEvent()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [{ ""Name"": ""Physical"", ""DisplayName"": ""Physical"" }],
              ""Mutations"": [
                { ""Name"": ""M1"", ""ClassName"": ""MutationA"", ""Category"": ""Physical"", ""Cost"": 4 },
                { ""Name"": ""M2"", ""ClassName"": ""MutationB"", ""Category"": ""Physical"", ""Cost"": 4 },
                { ""Name"": ""M3"", ""ClassName"": ""MutationC"", ""Category"": ""Physical"", ""Cost"": 4 }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            entity.AddPart(new RandomBuyOverridePart { MutationCountOverride = 1 });
            var mutations = entity.GetPart<MutationsPart>();

            var options = mutations.GetRandomBuyMutationOptions(baseSelectionCount: 3, rng: new Random(42));
            Assert.AreEqual(1, options.Count);
        }

        [Test]
        public void MutationsPart_GetRandomBuyMutationOptions_PrefersCostTwoOrHigher_WhenTrimming()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [{ ""Name"": ""Physical"", ""DisplayName"": ""Physical"" }],
              ""Mutations"": [
                { ""Name"": ""LowA"",  ""ClassName"": ""LowA"",  ""Category"": ""Physical"", ""Cost"": 1 },
                { ""Name"": ""LowB"",  ""ClassName"": ""LowB"",  ""Category"": ""Physical"", ""Cost"": 1 },
                { ""Name"": ""HighA"", ""ClassName"": ""HighA"", ""Category"": ""Physical"", ""Cost"": 4 },
                { ""Name"": ""HighB"", ""ClassName"": ""HighB"", ""Category"": ""Physical"", ""Cost"": 3 },
                { ""Name"": ""HighC"", ""ClassName"": ""HighC"", ""Category"": ""Physical"", ""Cost"": 2 }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            var options = mutations.GetRandomBuyMutationOptions(baseSelectionCount: 3, rng: new Random(7));
            Assert.AreEqual(3, options.Count);
            Assert.IsTrue(options.TrueForAll(o => o.Mutation.Cost >= 2));
        }

        [Test]
        public void MutationsPart_GetRandomBuyMutationOptions_RespectsEsperMorphotype()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [
                { ""Name"": ""Physical"", ""DisplayName"": ""Physical"" },
                { ""Name"": ""Mental"", ""DisplayName"": ""Mental"" }
              ],
              ""Mutations"": [
                { ""Name"": ""BodyMut"", ""ClassName"": ""BodyMut"", ""Category"": ""Physical"", ""Cost"": 4 },
                { ""Name"": ""MindMut"", ""ClassName"": ""MindMut"", ""Category"": ""Mental"", ""Cost"": 4 }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            entity.SetTag("Esper");
            var mutations = entity.GetPart<MutationsPart>();

            var options = mutations.GetRandomBuyMutationOptions(baseSelectionCount: 3, rng: new Random(11));
            Assert.AreEqual(1, options.Count);
            Assert.AreEqual("Mental", options[0].Mutation.Category);
        }

        [Test]
        public void MutationsPart_GetRandomBuyMutationOptions_ChimeraAnnotatesExtraLimbRolls()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [{ ""Name"": ""Physical"", ""DisplayName"": ""Physical"" }],
              ""Mutations"": [
                { ""Name"": ""ArmMutA"", ""ClassName"": ""ArmMutA"", ""Category"": ""Physical"", ""Cost"": 4 },
                { ""Name"": ""ArmMutB"", ""ClassName"": ""ArmMutB"", ""Category"": ""Physical"", ""Cost"": 4 }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            entity.SetTag("Chimera");
            entity.AddPart(new RandomBuyOverridePart { ChimericRollOverride = 2 });
            var mutations = entity.GetPart<MutationsPart>();

            var options = mutations.GetRandomBuyMutationOptions(baseSelectionCount: 2, rng: new Random(9));
            Assert.AreEqual(2, options.Count);
            Assert.IsTrue(options.TrueForAll(o => o.Mutation.Category == "Physical"));
            Assert.IsTrue(options.Exists(o => o.GrantsChimericBodyPart));
        }

        [Test]
        public void MutationsPart_BuyRandomMutationOption_AddsMutationAndSpendsMP()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [{ ""Name"": ""Physical"", ""DisplayName"": ""Physical"" }],
              ""Mutations"": [
                { ""Name"": ""Regeneration"", ""ClassName"": ""RegenerationMutation"", ""Category"": ""Physical"", ""Cost"": 4 }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["MP"] = new Stat { Name = "MP", BaseValue = 4, Min = 0, Max = 999 };
            var mpProbe = new MPEventProbePart();
            entity.AddPart(mpProbe);
            var chimericProbe = new ChimericHookProbePart();
            entity.AddPart(chimericProbe);
            var mutations = entity.GetPart<MutationsPart>();

            var options = mutations.GetRandomBuyMutationOptions(baseSelectionCount: 3, rng: new Random(3));
            Assert.AreEqual(1, options.Count);

            Assert.IsTrue(mutations.BuyRandomMutationOption(options[0], cost: 4, spendContext: "BuyNew"));
            Assert.IsTrue(mutations.HasMutation<RegenerationMutation>());
            Assert.AreEqual(4, entity.GetStat("MP").Penalty);
            Assert.AreEqual(1, mpProbe.UsedCalls);
            Assert.AreEqual("BuyNew", mpProbe.LastContext);
            Assert.AreEqual(0, chimericProbe.Calls);
        }

        [Test]
        public void MutationsPart_BuyRandomMutationOption_FailsWhenMPInsufficient()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [{ ""Name"": ""Physical"", ""DisplayName"": ""Physical"" }],
              ""Mutations"": [
                { ""Name"": ""Regeneration"", ""ClassName"": ""RegenerationMutation"", ""Category"": ""Physical"", ""Cost"": 4 }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["MP"] = new Stat { Name = "MP", BaseValue = 3, Min = 0, Max = 999 };
            var mutations = entity.GetPart<MutationsPart>();

            var options = mutations.GetRandomBuyMutationOptions(baseSelectionCount: 3, rng: new Random(3));
            Assert.AreEqual(1, options.Count);

            Assert.IsFalse(mutations.BuyRandomMutationOption(options[0], cost: 4, spendContext: "BuyNew"));
            Assert.IsFalse(mutations.HasMutation<RegenerationMutation>());
            Assert.AreEqual(0, entity.GetStat("MP").Penalty);
        }

        [Test]
        public void EsperMutation_Mutate_SetsMorphotypeMarkers_AndFiltersToMental()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [
                { ""Name"": ""Physical"", ""DisplayName"": ""Physical"" },
                { ""Name"": ""Mental"", ""DisplayName"": ""Mental"" },
                { ""Name"": ""Morphotypes"", ""DisplayName"": ""Morphotypes"" }
              ],
              ""Mutations"": [
                { ""Name"": ""BodyMut"", ""ClassName"": ""BodyMut"", ""Category"": ""Physical"", ""Cost"": 4 },
                { ""Name"": ""MindMut"", ""ClassName"": ""MindMut"", ""Category"": ""Mental"", ""Cost"": 4 },
                { ""Name"": ""Esper"", ""ClassName"": ""EsperMutation"", ""Category"": ""Morphotypes"", ""ExcludeFromPool"": true }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var esper = new EsperMutation();
            Assert.IsTrue(mutations.AddMutation(esper, 1));

            Assert.IsTrue(entity.HasTag("Esper"));
            Assert.AreEqual("Esper", entity.GetProperty("MutationLevel"));
            Assert.IsFalse(esper.CanLevel());

            var options = mutations.GetRandomBuyMutationOptions(baseSelectionCount: 3, rng: new Random(12));
            Assert.AreEqual(1, options.Count);
            Assert.AreEqual("Mental", options[0].Mutation.Category);

            Assert.IsTrue(mutations.RemoveMutation(esper));
            Assert.IsFalse(entity.HasTag("Esper"));
            Assert.IsNull(entity.GetProperty("MutationLevel"));
        }

        [Test]
        public void ChimeraMutation_Mutate_SetsMorphotypeMarkers_AndFiltersToPhysical()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [
                { ""Name"": ""Physical"", ""DisplayName"": ""Physical"" },
                { ""Name"": ""Mental"", ""DisplayName"": ""Mental"" },
                { ""Name"": ""Morphotypes"", ""DisplayName"": ""Morphotypes"" }
              ],
              ""Mutations"": [
                { ""Name"": ""BodyMut"", ""ClassName"": ""BodyMut"", ""Category"": ""Physical"", ""Cost"": 4 },
                { ""Name"": ""MindMut"", ""ClassName"": ""MindMut"", ""Category"": ""Mental"", ""Cost"": 4 },
                { ""Name"": ""Chimera"", ""ClassName"": ""ChimeraMutation"", ""Category"": ""Morphotypes"", ""ExcludeFromPool"": true }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var chimera = new ChimeraMutation();
            Assert.IsTrue(mutations.AddMutation(chimera, 1));

            Assert.IsTrue(entity.HasTag("Chimera"));
            Assert.AreEqual("Chimera", entity.GetProperty("MutationLevel"));
            Assert.IsFalse(chimera.CanLevel());

            var options = mutations.GetRandomBuyMutationOptions(baseSelectionCount: 3, rng: new Random(12));
            Assert.AreEqual(1, options.Count);
            Assert.AreEqual("Physical", options[0].Mutation.Category);

            Assert.IsTrue(mutations.RemoveMutation(chimera));
            Assert.IsFalse(entity.HasTag("Chimera"));
            Assert.IsNull(entity.GetProperty("MutationLevel"));
        }

        [Test]
        public void IrritableGenome_TracksUsedMPMemory_ExcludingBuyNew()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [{ ""Name"": ""MentalDefects"", ""DisplayName"": ""Mental Defects"" }],
              ""Mutations"": []
            }");

            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["MP"] = new Stat { Name = "MP", BaseValue = 12, Min = 0, Max = 999 };
            var mutations = entity.GetPart<MutationsPart>();
            var irritable = new IrritableGenomeMutation();
            mutations.AddMutation(irritable, 1);

            Assert.IsTrue(entity.UseMP(2, "RankUp"));
            Assert.AreEqual(2, irritable.MPSpentMemory);

            Assert.IsTrue(entity.UseMP(4, "BuyNew"));
            Assert.AreEqual(2, irritable.MPSpentMemory, "BuyNew should not increase memory");
        }

        [Test]
        public void IrritableGenome_GainedMP_AutoSpendsRememberedMP()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [{ ""Name"": ""Physical"", ""DisplayName"": ""Physical"" }],
              ""Mutations"": [
                { ""Name"": ""Track"", ""ClassName"": ""TrackingMutation"", ""Category"": ""Physical"", ""Cost"": 4 }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["MP"] = new Stat { Name = "MP", BaseValue = 3, Min = 0, Max = 999 };
            entity.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 20, Min = 1, Max = 99 };
            var mutations = entity.GetPart<MutationsPart>();

            var tracked = new TrackingMutation();
            mutations.AddMutation(tracked, 1);

            var irritable = new IrritableGenomeMutation { Rng = new Random(1), MPSpentMemory = 2 };
            mutations.AddMutation(irritable, 1);

            Assert.IsTrue(entity.GainMP(1)); // triggers GainedMP => irritable spending

            Assert.AreEqual(0, irritable.MPSpentMemory);
            Assert.AreEqual(3, tracked.BaseLevel, "should have spent 2 MP on random mutation rank increases");
            Assert.AreEqual(2, entity.GetStat("MP").Penalty);
        }

        [Test]
        public void UnstableGenome_LevelGain_ManifestsMutationAndConsumesCharge()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [
                { ""Name"": ""Physical"", ""DisplayName"": ""Physical"" },
                { ""Name"": ""Mental"", ""DisplayName"": ""Mental"" }
              ],
              ""Mutations"": [
                { ""Name"": ""Regeneration"", ""ClassName"": ""RegenerationMutation"", ""Category"": ""Physical"", ""Cost"": 4 },
                { ""Name"": ""Telepathy"", ""ClassName"": ""TelepathyMutation"", ""Category"": ""Mental"", ""Cost"": 4 },
                { ""Name"": ""UnstableGenome"", ""ClassName"": ""UnstableGenomeMutation"", ""Category"": ""Mental"", ""Ranked"": true, ""ExcludeFromPool"": true }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 1, Min = 1, Max = 99 };
            var mutations = entity.GetPart<MutationsPart>();

            var unstable = new UnstableGenomeMutation
            {
                ProcChancePercent = 100,
                Rng = new Random(7)
            };

            mutations.AddMutation(unstable, 2);
            Assert.IsTrue(mutations.HasMutation<UnstableGenomeMutation>());

            entity.SetStatValue("Level", 2);
            Assert.AreEqual(1, unstable.BaseLevel);

            entity.SetStatValue("Level", 3);
            Assert.IsFalse(mutations.HasMutation<UnstableGenomeMutation>(), "last unstable charge should be consumed");

            int manifested = 0;
            if (mutations.HasMutation<RegenerationMutation>()) manifested++;
            if (mutations.HasMutation<TelepathyMutation>()) manifested++;
            Assert.AreEqual(2, manifested, "both level gains should manifest one mutation each");
        }

        [Test]
        public void UnstableGenome_LevelGain_NoManifestWhenChanceZero()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [
                { ""Name"": ""Physical"", ""DisplayName"": ""Physical"" },
                { ""Name"": ""Mental"", ""DisplayName"": ""Mental"" }
              ],
              ""Mutations"": [
                { ""Name"": ""Regeneration"", ""ClassName"": ""RegenerationMutation"", ""Category"": ""Physical"", ""Cost"": 4 },
                { ""Name"": ""UnstableGenome"", ""ClassName"": ""UnstableGenomeMutation"", ""Category"": ""Mental"", ""Ranked"": true, ""ExcludeFromPool"": true }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 1, Min = 1, Max = 99 };
            var mutations = entity.GetPart<MutationsPart>();

            var unstable = new UnstableGenomeMutation
            {
                ProcChancePercent = 0,
                Rng = new Random(19)
            };

            mutations.AddMutation(unstable, 1);
            entity.SetStatValue("Level", 2);

            Assert.IsTrue(mutations.HasMutation<UnstableGenomeMutation>());
            Assert.AreEqual(1, unstable.BaseLevel);
            Assert.IsFalse(mutations.HasMutation<RegenerationMutation>());
        }

        [Test]
        public void MutationsPart_AddMutationMod_ContributesToEffectiveLevel()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var mutation = new TestMutation();

            mutations.AddMutation(mutation, 2);
            Guid id = mutations.AddMutationMod(typeof(TestMutation).Name, 3, MutationSourceType.Equipment, "test harness");

            Assert.AreEqual(5, mutation.Level);

            mutations.RemoveMutationMod(id);
            Assert.AreEqual(2, mutation.Level);
        }

        [Test]
        public void MutationsPart_AddMutationMod_CreatesTemporaryMutationPart_WhenMissing()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            Guid id = mutations.AddMutationMod("RegenerationMutation", 2, MutationSourceType.Equipment, "test");

            Assert.AreNotEqual(Guid.Empty, id);
            Assert.IsTrue(entity.HasPart<RegenerationMutation>());
            Assert.AreEqual(0, mutations.MutationList.Count, "temporary mod mutation should not be inherent");
        }

        [Test]
        public void MutationsPart_RemoveMutationMod_RemovesTemporaryMutationPart_WhenNoModsRemain()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            Guid id = mutations.AddMutationMod("RegenerationMutation", 2, MutationSourceType.Equipment, "test");
            Assert.IsTrue(entity.HasPart<RegenerationMutation>());

            mutations.RemoveMutationMod(id);

            Assert.IsFalse(entity.HasPart<RegenerationMutation>());
        }

        [Test]
        public void MutationsPart_AddMutation_DuplicateRanked_AdjustsRank()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [{ ""Name"": ""Physical"", ""DisplayName"": ""Physical"" }],
              ""Mutations"": [{
                ""Name"": ""Ranked"",
                ""ClassName"": ""RankedTestMutation"",
                ""Category"": ""Physical"",
                ""Ranked"": true
              }]
            }");

            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            var first = new RankedTestMutation();
            var second = new RankedTestMutation();

            Assert.IsTrue(mutations.AddMutation(first, 1));
            Assert.IsTrue(mutations.AddMutation(second, 1), "ranked duplicate should adjust rank, not fail");
            Assert.AreEqual(1, mutations.MutationList.Count);
            Assert.AreEqual(2, first.GetRank());
        }

        [Test]
        public void MutationsPart_GetMutatePool_RespectsExclusionsAndDefectLimit()
        {
            MutationRegistry.InitializeFromJson(@"
            {
              ""Categories"": [
                { ""Name"": ""Physical"", ""DisplayName"": ""Physical"" },
                { ""Name"": ""MentalDefects"", ""DisplayName"": ""Mental Defects"" }
              ],
              ""Mutations"": [
                { ""Name"": ""Alpha"", ""ClassName"": ""RegenerationMutation"", ""Category"": ""Physical"", ""Exclusions"": [""Beta""] },
                { ""Name"": ""Beta"", ""ClassName"": ""TelepathyMutation"", ""Category"": ""Physical"" },
                { ""Name"": ""DefA"", ""ClassName"": ""FlamingHandsMutation"", ""Category"": ""MentalDefects"", ""Defect"": true },
                { ""Name"": ""DefB"", ""ClassName"": ""TestMutation"", ""Category"": ""MentalDefects"", ""Defect"": true }
              ]
            }");

            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();

            Assert.IsTrue(mutations.AddMutation(new RegenerationMutation(), 1)); // Alpha
            Assert.IsTrue(mutations.AddMutation(new FlamingHandsMutation(), 1)); // DefA

            var pool = mutations.GetMutatePool();

            // Beta excluded by Alpha's exclusion list.
            Assert.IsFalse(pool.Exists(d => d.Name == "Beta"));

            // DefB blocked by single-defect limit (DefA already owned).
            Assert.IsFalse(pool.Exists(d => d.Name == "DefB"));
        }

        [Test]
        public void MutationsPart_SyncMutationLevels_EventTriggersChangeLevel()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 20, Min = 1, Max = 99 };
            var mutations = entity.GetPart<MutationsPart>();
            var mutation = new TrackingMutation();
            mutations.AddMutation(mutation, 1);

            int initialChanges = mutation.ChangeLevelCalls;
            mutation.BaseLevel = 3; // direct change, no lifecycle call yet
            entity.FireEvent(GameEvent.New("SyncMutationLevels"));

            Assert.Greater(mutation.ChangeLevelCalls, initialChanges);
            Assert.AreEqual(3, mutation.LastLevel);
        }

        [Test]
        public void MutationsPart_StatChanged_TriggersSyncAndRecalculation()
        {
            var entity = CreateCreatureWithMutationSupport();
            entity.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 2, Min = 1, Max = 99 };
            var mutations = entity.GetPart<MutationsPart>();
            var mutation = new TrackingMutation();
            mutations.AddMutation(mutation, 5); // capped to 2 initially
            int initialChanges = mutation.ChangeLevelCalls;
            Assert.AreEqual(2, mutation.Level);

            entity.SetStatValue("Level", 10); // should fire StatChanged => sync

            Assert.Greater(mutation.ChangeLevelCalls, initialChanges);
            Assert.AreEqual(5, mutation.Level);
        }

        [Test]
        public void MutationsPart_SyncMutationLevels_HandlesZeroCrossingForNonLevelable()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var mutation = new TrackingMutation { IsLevelable = false };
            mutations.AddMutation(mutation, 1);

            // Drive level to 0 without lifecycle hook first.
            mutation.BaseLevel = 0;
            entity.FireEvent(GameEvent.New("SyncMutationLevels"));
            Assert.AreEqual(1, mutation.UnmutateCalls);
            Assert.AreEqual(0, mutation.LastLevel);

            // Raise back above zero and sync, should remutate.
            mutation.BaseLevel = 2;
            entity.FireEvent(GameEvent.New("SyncMutationLevels"));
            Assert.GreaterOrEqual(mutation.MutateCalls, 2);
            Assert.AreEqual(2, mutation.LastLevel);
        }

        // ========================
        // FlamingHandsMutation
        // ========================

        [Test]
        public void FlamingHands_Mutate_RegistersAbility()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var abilities = entity.GetPart<ActivatedAbilitiesPart>();

            mutations.AddMutation(new FlamingHandsMutation(), 1);

            Assert.AreEqual(1, abilities.AbilityList.Count);
            Assert.AreEqual("Flaming Hands", abilities.AbilityList[0].DisplayName);
            Assert.AreEqual(FlamingHandsMutation.COMMAND_NAME, abilities.AbilityList[0].Command);
        }

        [Test]
        public void FlamingHands_Unmutate_RemovesAbility()
        {
            var entity = CreateCreatureWithMutationSupport();
            var mutations = entity.GetPart<MutationsPart>();
            var abilities = entity.GetPart<ActivatedAbilitiesPart>();

            var flamingHands = new FlamingHandsMutation();
            mutations.AddMutation(flamingHands, 1);
            mutations.RemoveMutation(flamingHands);

            Assert.AreEqual(0, abilities.AbilityList.Count);
        }

        [Test]
        public void FlamingHands_Cast_DamagesCreaturesInCell()
        {
            var zone = CreateSimpleZone();
            var attacker = CreateCreatureWithMutationSupport();
            attacker.GetPart<RenderPart>().DisplayName = "you";
            zone.AddEntity(attacker, 5, 5);

            var target = CreateCreature(hp: 20);
            target.GetPart<RenderPart>().DisplayName = "snapjaw";
            zone.AddEntity(target, 6, 5);

            var mutations = attacker.GetPart<MutationsPart>();
            mutations.AddMutation(new FlamingHandsMutation(), 1);

            var flamingHands = mutations.GetMutation<FlamingHandsMutation>();
            var rng = new Random(42);
            var targetCell = zone.GetCell(6, 5);

            flamingHands.Cast(targetCell, zone, rng);

            // Should have dealt damage (1d4 at level 1, so 1-4 damage)
            int remainingHP = target.GetStatValue("Hitpoints");
            Assert.Less(remainingHP, 20);
        }

        [Test]
        public void FlamingHands_Cast_PutsOnCooldown()
        {
            var zone = CreateSimpleZone();
            var attacker = CreateCreatureWithMutationSupport();
            zone.AddEntity(attacker, 5, 5);

            var mutations = attacker.GetPart<MutationsPart>();
            mutations.AddMutation(new FlamingHandsMutation(), 1);

            var flamingHands = mutations.GetMutation<FlamingHandsMutation>();
            var abilities = attacker.GetPart<ActivatedAbilitiesPart>();
            var rng = new Random(42);
            var targetCell = zone.GetCell(6, 5);

            flamingHands.Cast(targetCell, zone, rng);

            var ability = abilities.GetAbility(flamingHands.ActivatedAbilityID);
            Assert.AreEqual(FlamingHandsMutation.COOLDOWN, ability.CooldownRemaining);
            Assert.IsFalse(ability.IsUsable);
        }

        [Test]
        public void FlamingHands_Cast_HigherLevelMoreDamage()
        {
            // Level 3 should deal 3d4 (3-12), level 1 deals 1d4 (1-4)
            // Use deterministic RNG to verify more damage at higher level
            var zone = CreateSimpleZone();

            // Level 1 test
            var attacker1 = CreateCreatureWithMutationSupport(hp: 20);
            zone.AddEntity(attacker1, 0, 0);
            var target1 = CreateCreature(hp: 100);
            zone.AddEntity(target1, 1, 0);

            var mutations1 = attacker1.GetPart<MutationsPart>();
            mutations1.AddMutation(new FlamingHandsMutation(), 1);
            var fh1 = mutations1.GetMutation<FlamingHandsMutation>();

            var rng1 = new Random(42);
            fh1.Cast(zone.GetCell(1, 0), zone, rng1);
            int damage1 = 100 - target1.GetStatValue("Hitpoints");

            // Level 3 test
            var attacker3 = CreateCreatureWithMutationSupport(hp: 20);
            zone.AddEntity(attacker3, 0, 1);
            var target3 = CreateCreature(hp: 100);
            zone.AddEntity(target3, 1, 1);

            var mutations3 = attacker3.GetPart<MutationsPart>();
            mutations3.AddMutation(new FlamingHandsMutation(), 3);
            var fh3 = mutations3.GetMutation<FlamingHandsMutation>();

            var rng3 = new Random(42);
            fh3.Cast(zone.GetCell(1, 1), zone, rng3);
            int damage3 = 100 - target3.GetStatValue("Hitpoints");

            Assert.Greater(damage3, damage1, "Level 3 should deal more damage than level 1");
        }

        [Test]
        public void FlamingHands_Cast_EmptyCell_LogsMessage()
        {
            var zone = CreateSimpleZone();
            var attacker = CreateCreatureWithMutationSupport();
            attacker.GetPart<RenderPart>().DisplayName = "you";
            zone.AddEntity(attacker, 5, 5);

            var mutations = attacker.GetPart<MutationsPart>();
            mutations.AddMutation(new FlamingHandsMutation(), 1);

            var flamingHands = mutations.GetMutation<FlamingHandsMutation>();
            var rng = new Random(42);
            var emptyCell = zone.GetCell(6, 5); // no creature

            flamingHands.Cast(emptyCell, zone, rng);

            Assert.IsTrue(MessageLog.GetLast().Contains("flames"));
        }

        [Test]
        public void FlamingHands_CommandEvent_TriggersCast()
        {
            var zone = CreateSimpleZone();
            var attacker = CreateCreatureWithMutationSupport();
            zone.AddEntity(attacker, 5, 5);

            var target = CreateCreature(hp: 20);
            zone.AddEntity(target, 6, 5);

            var mutations = attacker.GetPart<MutationsPart>();
            mutations.AddMutation(new FlamingHandsMutation(), 1);

            // Fire the command event
            var cmd = GameEvent.New(FlamingHandsMutation.COMMAND_NAME);
            cmd.SetParameter("TargetCell", (object)zone.GetCell(6, 5));
            cmd.SetParameter("Zone", (object)zone);
            cmd.SetParameter("RNG", (object)new Random(42));
            attacker.FireEvent(cmd);

            Assert.Less(target.GetStatValue("Hitpoints"), 20);
        }

        [Test]
        public void FlamingHands_Cast_CanKillCreature()
        {
            var zone = CreateSimpleZone();
            var attacker = CreateCreatureWithMutationSupport();
            zone.AddEntity(attacker, 5, 5);

            var target = CreateCreature(hp: 1); // 1 HP, will die
            zone.AddEntity(target, 6, 5);

            var mutations = attacker.GetPart<MutationsPart>();
            mutations.AddMutation(new FlamingHandsMutation(), 5); // Level 5 for big damage

            var flamingHands = mutations.GetMutation<FlamingHandsMutation>();
            flamingHands.Cast(zone.GetCell(6, 5), zone, new Random(42));

            // Target should be removed from zone (dead)
            Assert.IsNull(zone.GetEntityCell(target));
        }

        // ========================
        // TelepathyMutation
        // ========================

        [Test]
        public void Telepathy_Mutate_AppliesEgoBonus()
        {
            var entity = CreateCreatureWithMutationSupport(ego: 10);
            var mutations = entity.GetPart<MutationsPart>();

            mutations.AddMutation(new TelepathyMutation(), 1);

            // Level 1: bonus = max(1, 1/2) = 1
            var ego = entity.GetStat("Ego");
            Assert.AreEqual(1, ego.Bonus);
            Assert.AreEqual(11, ego.Value);
        }

        [Test]
        public void Telepathy_Mutate_Level4_AppliesBonus2()
        {
            var entity = CreateCreatureWithMutationSupport(ego: 10);
            var mutations = entity.GetPart<MutationsPart>();

            mutations.AddMutation(new TelepathyMutation(), 4);

            // Level 4: bonus = 4/2 = 2
            var ego = entity.GetStat("Ego");
            Assert.AreEqual(2, ego.Bonus);
            Assert.AreEqual(12, ego.Value);
        }

        [Test]
        public void Telepathy_Unmutate_RemovesEgoBonus()
        {
            var entity = CreateCreatureWithMutationSupport(ego: 10);
            var mutations = entity.GetPart<MutationsPart>();

            var telepathy = new TelepathyMutation();
            mutations.AddMutation(telepathy, 4);
            mutations.RemoveMutation(telepathy);

            var ego = entity.GetStat("Ego");
            Assert.AreEqual(0, ego.Bonus);
            Assert.AreEqual(10, ego.Value);
        }

        [Test]
        public void Telepathy_Properties()
        {
            var telepathy = new TelepathyMutation();
            Assert.AreEqual("Mental", telepathy.MutationType);
            Assert.AreEqual("Telepathy", telepathy.DisplayName);
        }

        // ========================
        // RegenerationMutation
        // ========================

        [Test]
        public void Regeneration_EndTurn_HealsLevelHP()
        {
            var entity = CreateCreatureWithMutationSupport(hp: 20);
            var mutations = entity.GetPart<MutationsPart>();

            mutations.AddMutation(new RegenerationMutation(), 2);

            // Damage the entity
            var hpStat = entity.GetStat("Hitpoints");
            hpStat.BaseValue = 10;

            // Fire EndTurn — should heal 2 HP (Level=2)
            entity.FireEvent(GameEvent.New("EndTurn"));

            Assert.AreEqual(12, hpStat.BaseValue);
        }

        [Test]
        public void Regeneration_EndTurn_CapsAtMaxHP()
        {
            var entity = CreateCreatureWithMutationSupport(hp: 20);
            var mutations = entity.GetPart<MutationsPart>();

            mutations.AddMutation(new RegenerationMutation(), 5);

            // Damage slightly
            var hpStat = entity.GetStat("Hitpoints");
            hpStat.BaseValue = 18;

            // Fire EndTurn — should heal to 20 (max), not 23
            entity.FireEvent(GameEvent.New("EndTurn"));

            Assert.AreEqual(20, hpStat.BaseValue);
        }

        [Test]
        public void Regeneration_EndTurn_DoesNotHealAtMaxHP()
        {
            var entity = CreateCreatureWithMutationSupport(hp: 20);
            var mutations = entity.GetPart<MutationsPart>();

            mutations.AddMutation(new RegenerationMutation(), 3);

            // Already at max
            entity.FireEvent(GameEvent.New("EndTurn"));

            Assert.AreEqual(20, entity.GetStat("Hitpoints").BaseValue);
        }

        [Test]
        public void Regeneration_Properties()
        {
            var regen = new RegenerationMutation();
            Assert.AreEqual("Physical", regen.MutationType);
            Assert.AreEqual("Regeneration", regen.DisplayName);
        }

        // ========================
        // TurnManager EndTurn Event
        // ========================

        [Test]
        public void TurnManager_EndTurn_FiresEndTurnEvent()
        {
            var entity = CreateCreatureWithMutationSupport();
            var abilities = entity.GetPart<ActivatedAbilitiesPart>();
            var id = abilities.AddAbility("Test", "Cmd", "Class");
            abilities.CooldownAbility(id, 5);

            var tm = new TurnManager();
            tm.AddEntity(entity);

            // Give entity enough energy to take a turn
            tm.Tick();
            tm.EndTurn(entity);

            // Cooldown should have ticked from 5 to 4
            Assert.AreEqual(4, abilities.GetAbility(id).CooldownRemaining);
        }

        [Test]
        public void TurnManager_EndTurn_TicksRegeneration()
        {
            var entity = CreateCreatureWithMutationSupport(hp: 20);
            var mutations = entity.GetPart<MutationsPart>();
            mutations.AddMutation(new RegenerationMutation(), 1);

            // Damage
            entity.GetStat("Hitpoints").BaseValue = 15;

            var tm = new TurnManager();
            tm.AddEntity(entity);
            tm.Tick();
            tm.EndTurn(entity);

            // Should have healed 1 HP
            Assert.AreEqual(16, entity.GetStat("Hitpoints").BaseValue);
        }

        // ========================
        // MutationsPart StartingMutations Parsing
        // ========================

        [Test]
        public void MutationsPart_StartingMutations_ParsesSingle()
        {
            var entity = CreateCreature();
            entity.AddPart(new ActivatedAbilitiesPart());

            var mutationsPart = new MutationsPart();
            mutationsPart.StartingMutations = "RegenerationMutation:2";
            entity.AddPart(mutationsPart);

            Assert.AreEqual(1, mutationsPart.MutationList.Count);
            Assert.IsTrue(mutationsPart.HasMutation<RegenerationMutation>());
            Assert.AreEqual(2, mutationsPart.GetMutation<RegenerationMutation>().BaseLevel);
        }

        [Test]
        public void MutationsPart_StartingMutations_ParsesMultiple()
        {
            var entity = CreateCreature();
            entity.AddPart(new ActivatedAbilitiesPart());

            var mutationsPart = new MutationsPart();
            mutationsPart.StartingMutations = "RegenerationMutation:1,TelepathyMutation:3";
            entity.AddPart(mutationsPart);

            Assert.AreEqual(2, mutationsPart.MutationList.Count);
            Assert.IsTrue(mutationsPart.HasMutation<RegenerationMutation>());
            Assert.IsTrue(mutationsPart.HasMutation<TelepathyMutation>());
            Assert.AreEqual(3, mutationsPart.GetMutation<TelepathyMutation>().BaseLevel);
        }

        [Test]
        public void MutationsPart_StartingMutations_DefaultLevelOne()
        {
            var entity = CreateCreature();
            entity.AddPart(new ActivatedAbilitiesPart());

            var mutationsPart = new MutationsPart();
            mutationsPart.StartingMutations = "RegenerationMutation";
            entity.AddPart(mutationsPart);

            Assert.AreEqual(1, mutationsPart.GetMutation<RegenerationMutation>().BaseLevel);
        }

        [Test]
        public void MutationsPart_StartingMutations_IgnoresInvalidNames()
        {
            var entity = CreateCreature();
            entity.AddPart(new ActivatedAbilitiesPart());

            var mutationsPart = new MutationsPart();
            mutationsPart.StartingMutations = "NonexistentMutation:1,RegenerationMutation:1";
            entity.AddPart(mutationsPart);

            // Only RegenerationMutation should be added
            Assert.AreEqual(1, mutationsPart.MutationList.Count);
            Assert.IsTrue(mutationsPart.HasMutation<RegenerationMutation>());
        }

        // ========================
        // Integration: Cooldown + Turn Flow
        // ========================

        [Test]
        public void Integration_FlamingHands_CooldownTicksOverTurns()
        {
            var zone = CreateSimpleZone();
            var entity = CreateCreatureWithMutationSupport();
            entity.Tags["Player"] = "";
            zone.AddEntity(entity, 5, 5);

            var mutations = entity.GetPart<MutationsPart>();
            mutations.AddMutation(new FlamingHandsMutation(), 1);

            var flamingHands = mutations.GetMutation<FlamingHandsMutation>();
            var abilities = entity.GetPart<ActivatedAbilitiesPart>();

            // Cast to put on cooldown
            flamingHands.Cast(zone.GetCell(6, 5), zone, new Random(42));
            Assert.AreEqual(FlamingHandsMutation.COOLDOWN, abilities.GetAbility(flamingHands.ActivatedAbilityID).CooldownRemaining);

            // Simulate 10 EndTurn events
            var tm = new TurnManager();
            tm.AddEntity(entity);

            for (int i = 0; i < FlamingHandsMutation.COOLDOWN; i++)
            {
                tm.Tick();
                tm.EndTurn(entity);
            }

            Assert.AreEqual(0, abilities.GetAbility(flamingHands.ActivatedAbilityID).CooldownRemaining);
            Assert.IsTrue(abilities.GetAbility(flamingHands.ActivatedAbilityID).IsUsable);
        }
    }
}
