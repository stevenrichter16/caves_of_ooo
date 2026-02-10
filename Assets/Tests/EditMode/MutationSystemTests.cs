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
