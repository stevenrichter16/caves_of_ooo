using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class CombatSystemTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ========================
        // DiceRoller
        // ========================

        [Test]
        public void DiceRoller_Parse_1d6()
        {
            var (count, sides, mod) = DiceRoller.Parse("1d6");
            Assert.AreEqual(1, count);
            Assert.AreEqual(6, sides);
            Assert.AreEqual(0, mod);
        }

        [Test]
        public void DiceRoller_Parse_2d8Plus3()
        {
            var (count, sides, mod) = DiceRoller.Parse("2d8+3");
            Assert.AreEqual(2, count);
            Assert.AreEqual(8, sides);
            Assert.AreEqual(3, mod);
        }

        [Test]
        public void DiceRoller_Parse_1d4Minus1()
        {
            var (count, sides, mod) = DiceRoller.Parse("1d4-1");
            Assert.AreEqual(1, count);
            Assert.AreEqual(4, sides);
            Assert.AreEqual(-1, mod);
        }

        [Test]
        public void DiceRoller_Roll_InRange()
        {
            var rng = new Random(42);
            for (int i = 0; i < 100; i++)
            {
                int result = DiceRoller.Roll("2d6", rng);
                Assert.GreaterOrEqual(result, 2);
                Assert.LessOrEqual(result, 12);
            }
        }

        [Test]
        public void DiceRoller_Roll_WithModifier()
        {
            var rng = new Random(42);
            for (int i = 0; i < 100; i++)
            {
                int result = DiceRoller.Roll("1d4+3", rng);
                Assert.GreaterOrEqual(result, 4);
                Assert.LessOrEqual(result, 7);
            }
        }

        [Test]
        public void DiceRoller_Roll_Deterministic()
        {
            var rng1 = new Random(99);
            var rng2 = new Random(99);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(DiceRoller.Roll("3d6", rng1), DiceRoller.Roll("3d6", rng2));
            }
        }

        [Test]
        public void DiceRoller_Parse_Invalid_ReturnsZero()
        {
            var (count, sides, mod) = DiceRoller.Parse("bad");
            Assert.AreEqual(0, count);
            Assert.AreEqual(0, sides);
            Assert.AreEqual(0, mod);
        }

        // ========================
        // StatUtils
        // ========================

        [Test]
        public void StatUtils_Modifier_16_Is_Zero()
        {
            Assert.AreEqual(0, StatUtils.GetModifier(16));
        }

        [Test]
        public void StatUtils_Modifier_18_Is_Plus1()
        {
            Assert.AreEqual(1, StatUtils.GetModifier(18));
        }

        [Test]
        public void StatUtils_Modifier_14_Is_Minus1()
        {
            Assert.AreEqual(-1, StatUtils.GetModifier(14));
        }

        [Test]
        public void StatUtils_Modifier_20_Is_Plus2()
        {
            Assert.AreEqual(2, StatUtils.GetModifier(20));
        }

        [Test]
        public void StatUtils_Modifier_10_Is_Minus3()
        {
            Assert.AreEqual(-3, StatUtils.GetModifier(10));
        }

        [Test]
        public void StatUtils_Modifier_17_Is_Zero()
        {
            // Floor((17-16)/2) = Floor(0.5) = 0
            Assert.AreEqual(0, StatUtils.GetModifier(17));
        }

        // ========================
        // MessageLog
        // ========================

        [Test]
        public void MessageLog_AddAndGetLast()
        {
            MessageLog.Add("Hello");
            MessageLog.Add("World");
            Assert.AreEqual("World", MessageLog.GetLast());
            Assert.AreEqual(2, MessageLog.Count);
        }

        [Test]
        public void MessageLog_Clear_EmptiesLog()
        {
            MessageLog.Add("test");
            MessageLog.Clear();
            Assert.AreEqual(0, MessageLog.Count);
            Assert.IsNull(MessageLog.GetLast());
        }

        // ========================
        // Combat Parts
        // ========================

        [Test]
        public void MeleeWeaponPart_DefaultValues()
        {
            var weapon = new MeleeWeaponPart();
            Assert.AreEqual("1d2", weapon.BaseDamage);
            Assert.AreEqual(0, weapon.PenBonus);
            Assert.AreEqual(0, weapon.HitBonus);
            Assert.AreEqual(-1, weapon.MaxStrengthBonus);
            Assert.AreEqual("Strength", weapon.Stat);
            Assert.AreEqual("MeleeWeapon", weapon.Name);
        }

        [Test]
        public void ArmorPart_DefaultValues()
        {
            var armor = new ArmorPart();
            Assert.AreEqual(0, armor.AV);
            Assert.AreEqual(0, armor.DV);
            Assert.AreEqual(0, armor.SpeedPenalty);
            Assert.AreEqual("Armor", armor.Name);
        }

        // ========================
        // DV and AV Calculation
        // ========================

        [Test]
        public void GetDV_BaseCreature_NoArmor()
        {
            // Agility 16 → mod 0, base DV 6
            var entity = CreateCreature(16, 16);
            Assert.AreEqual(6, CombatSystem.GetDV(entity));
        }

        [Test]
        public void GetDV_WithArmorDV()
        {
            // Agility 16 → mod 0, armor DV 3, total 9
            var entity = CreateCreature(16, 16);
            entity.GetPart<ArmorPart>().DV = 3;
            Assert.AreEqual(9, CombatSystem.GetDV(entity));
        }

        [Test]
        public void GetDV_WithHighAgility()
        {
            // Agility 20 → mod +2, base DV 6, total 8
            var entity = CreateCreature(16, 20);
            Assert.AreEqual(8, CombatSystem.GetDV(entity));
        }

        [Test]
        public void GetAV_NoArmor_ReturnsZero()
        {
            var entity = new Entity();
            Assert.AreEqual(0, CombatSystem.GetAV(entity));
        }

        [Test]
        public void GetAV_WithArmor()
        {
            var entity = CreateCreature(16, 16);
            entity.GetPart<ArmorPart>().AV = 4;
            Assert.AreEqual(4, CombatSystem.GetAV(entity));
        }

        // ========================
        // Penetration Rolls
        // ========================

        [Test]
        public void Penetrations_HighPV_AlwaysPenetrates()
        {
            // PV 20 vs AV 0: every 1d8 + 20 > 0, so at least 3 penetrations
            var rng = new Random(42);
            int pens = CombatSystem.RollPenetrations(20, 0, rng);
            Assert.GreaterOrEqual(pens, 3);
        }

        [Test]
        public void Penetrations_ZeroPV_LowAV_SomePenetrations()
        {
            // PV 0 vs AV 2: 1d8+0 > 2 means rolls 3-8 succeed (75% chance)
            var rng = new Random(42);
            int totalPens = 0;
            for (int i = 0; i < 50; i++)
                totalPens += CombatSystem.RollPenetrations(0, 2, rng);
            Assert.Greater(totalPens, 0, "Should get some penetrations with PV0 vs AV2");
        }

        // ========================
        // Full Melee Attack
        // ========================

        [Test]
        public void MeleeAttack_DealsDamage()
        {
            var zone = new Zone();
            var attacker = CreateCreature(20, 20); // Str 20 (mod +2), Agi 20 (mod +2)
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 5;
            zone.AddEntity(attacker, 5, 5);

            var defender = CreateCreature(10, 10); // Low stats
            defender.GetPart<ArmorPart>().AV = 0;
            defender.GetPart<ArmorPart>().DV = 0;
            zone.AddEntity(defender, 6, 5);

            int initialHP = defender.GetStatValue("Hitpoints");

            // Run many attacks with a fixed seed — at least one should deal damage
            bool dealtDamage = false;
            for (int i = 0; i < 20; i++)
            {
                var rng = new Random(i);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, rng);
                if (defender.GetStatValue("Hitpoints") < initialHP)
                {
                    dealtDamage = true;
                    break;
                }
            }
            Assert.IsTrue(dealtDamage, "Attack with high stats vs low stats should deal damage");
        }

        [Test]
        public void MeleeAttack_CanMiss()
        {
            var zone = new Zone();
            var attacker = CreateCreature(10, 10); // Agi 10 → mod -3
            zone.AddEntity(attacker, 5, 5);

            var defender = CreateCreature(16, 20); // Agi 20 → mod +2, DV = 6 + 2 = 8
            defender.GetPart<ArmorPart>().DV = 5; // total DV = 6 + 5 + 2 = 13
            zone.AddEntity(defender, 6, 5);

            // With attacker Agi mod -3 and defender DV 13, many rolls should miss
            bool missed = false;
            for (int i = 0; i < 30; i++)
            {
                var rng = new Random(i);
                MessageLog.Clear();
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, rng);
                string msg = MessageLog.GetLast();
                if (msg != null && msg.Contains("misses"))
                {
                    missed = true;
                    break;
                }
            }
            Assert.IsTrue(missed, "Should get at least one miss with low attacker Agi vs high defender DV");
        }

        [Test]
        public void MeleeAttack_KillsTarget()
        {
            var zone = new Zone();
            var attacker = CreateCreature(20, 20);
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "10d6"; // Massive damage
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 20;
            attacker.GetPart<MeleeWeaponPart>().HitBonus = 20;
            zone.AddEntity(attacker, 5, 5);

            var defender = CreateCreature(10, 10);
            defender.SetStatValue("Hitpoints", 1); // 1 HP
            zone.AddEntity(defender, 6, 5);

            // Attack until dead
            bool killed = false;
            for (int i = 0; i < 20; i++)
            {
                var rng = new Random(i);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, rng);
                if (defender.GetStatValue("Hitpoints") <= 0)
                {
                    killed = true;
                    break;
                }
            }
            Assert.IsTrue(killed, "Massive damage should kill 1HP target");

            // Verify removed from zone
            Assert.IsNull(zone.GetEntityCell(defender), "Dead entity should be removed from zone");
        }

        [Test]
        public void MeleeAttack_DeathMessage()
        {
            var zone = new Zone();
            var attacker = CreateCreature(20, 20);
            attacker.AddPart(new RenderPart { DisplayName = "hero" });
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "100d6";
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 50;
            attacker.GetPart<MeleeWeaponPart>().HitBonus = 50;
            zone.AddEntity(attacker, 5, 5);

            var defender = CreateCreature(10, 10);
            defender.AddPart(new RenderPart { DisplayName = "goblin" });
            defender.SetStatValue("Hitpoints", 1);
            zone.AddEntity(defender, 6, 5);

            var rng = new Random(42);
            CombatSystem.PerformMeleeAttack(attacker, defender, zone, rng);

            // Check for death message
            bool hasDeathMsg = false;
            foreach (var msg in MessageLog.GetRecent(10))
            {
                if (msg.Contains("killed"))
                {
                    hasDeathMsg = true;
                    break;
                }
            }
            Assert.IsTrue(hasDeathMsg, "Death should produce a 'killed' message");
        }

        [Test]
        public void MeleeAttack_BeforeMeleeAttack_CanCancel()
        {
            var zone = new Zone();
            var attacker = CreateCreature(20, 20);
            // Add a part that cancels melee attacks
            attacker.AddPart(new CancelAttackPart());
            zone.AddEntity(attacker, 5, 5);

            var defender = CreateCreature(16, 16);
            zone.AddEntity(defender, 6, 5);

            var rng = new Random(42);
            bool result = CombatSystem.PerformMeleeAttack(attacker, defender, zone, rng);
            Assert.IsFalse(result, "Attack should be cancelled by BeforeMeleeAttack handler");
        }

        [Test]
        public void MeleeAttack_TryMoveEx_ReturnsBlockedBy()
        {
            var zone = new Zone();
            var mover = CreateCreature(16, 16);
            zone.AddEntity(mover, 5, 5);

            var blocker = CreateCreature(16, 16);
            zone.AddEntity(blocker, 6, 5);

            var (moved, blockedBy) = MovementSystem.TryMoveEx(mover, zone, 1, 0);
            Assert.IsFalse(moved);
            Assert.AreEqual(blocker, blockedBy, "Should return the blocking entity");
        }

        [Test]
        public void MeleeAttack_TryMoveEx_MovesWhenClear()
        {
            var zone = new Zone();
            var mover = CreateCreature(16, 16);
            zone.AddEntity(mover, 5, 5);

            var (moved, blockedBy) = MovementSystem.TryMoveEx(mover, zone, 1, 0);
            Assert.IsTrue(moved);
            Assert.IsNull(blockedBy);
            var cell = zone.GetEntityCell(mover);
            Assert.AreEqual(6, cell.X);
        }

        [Test]
        public void BlueprintFactory_CreatesWeaponAndArmor()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(GetTestJson());

            var snapjaw = factory.CreateEntity("Snapjaw");
            Assert.IsNotNull(snapjaw);

            var weapon = snapjaw.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(weapon, "Snapjaw should have MeleeWeaponPart");
            Assert.AreEqual("1d4", weapon.BaseDamage);
            Assert.AreEqual(1, weapon.PenBonus);

            var armor = snapjaw.GetPart<ArmorPart>();
            Assert.IsNotNull(armor, "Snapjaw should have ArmorPart");
            Assert.AreEqual(2, armor.AV);
            Assert.AreEqual(1, armor.DV);
        }

        // ========================
        // Death / Loot Drops
        // ========================

        [Test]
        public void HandleDeath_DropsEquippedWeapon()
        {
            var zone = new Zone();
            var creature = CreateCreature(16, 16, 30);
            creature.AddPart(new InventoryPart { MaxWeight = 150 });
            zone.AddEntity(creature, 5, 5);

            var sword = new Entity();
            sword.BlueprintName = "Sword";
            sword.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            sword.AddPart(new MeleeWeaponPart { BaseDamage = "1d8" });
            sword.AddPart(new EquippablePart { Slot = "Hand" });

            var inv = creature.GetPart<InventoryPart>();
            inv.AddObject(sword);
            InventorySystem.Equip(creature, sword);

            CombatSystem.HandleDeath(creature, null, zone);

            // Creature should be removed from zone
            Assert.IsNull(zone.GetEntityCell(creature));

            // Sword should be on the ground at creature's old position
            var cell = zone.GetCell(5, 5);
            Assert.IsTrue(cell.Objects.Contains(sword), "Equipped weapon should drop on death");
        }

        [Test]
        public void HandleDeath_DropsCarriedItems()
        {
            var zone = new Zone();
            var creature = CreateCreature(16, 16, 30);
            creature.AddPart(new InventoryPart { MaxWeight = 150 });
            zone.AddEntity(creature, 5, 5);

            var potion = new Entity();
            potion.BlueprintName = "Potion";
            potion.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            creature.GetPart<InventoryPart>().AddObject(potion);

            CombatSystem.HandleDeath(creature, null, zone);

            var cell = zone.GetCell(5, 5);
            Assert.IsTrue(cell.Objects.Contains(potion), "Carried item should drop on death");
        }

        [Test]
        public void HandleDeath_DropsEquipmentAndInventory()
        {
            var zone = new Zone();
            var creature = CreateCreature(16, 16, 30);
            creature.AddPart(new InventoryPart { MaxWeight = 150 });
            zone.AddEntity(creature, 5, 5);

            // Equip a weapon
            var sword = new Entity();
            sword.BlueprintName = "Sword";
            sword.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            sword.AddPart(new MeleeWeaponPart { BaseDamage = "1d8" });
            sword.AddPart(new EquippablePart { Slot = "Hand" });
            creature.GetPart<InventoryPart>().AddObject(sword);
            InventorySystem.Equip(creature, sword);

            // Carry a potion
            var potion = new Entity();
            potion.BlueprintName = "Potion";
            potion.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            creature.GetPart<InventoryPart>().AddObject(potion);

            CombatSystem.HandleDeath(creature, null, zone);

            var cell = zone.GetCell(5, 5);
            Assert.IsTrue(cell.Objects.Contains(sword), "Equipped weapon should drop");
            Assert.IsTrue(cell.Objects.Contains(potion), "Carried item should drop");
            Assert.IsFalse(cell.Objects.Contains(creature), "Creature should be removed");
        }

        [Test]
        public void HandleDeath_NoZone_NoCrash()
        {
            var creature = CreateCreature(16, 16, 30);
            creature.AddPart(new InventoryPart { MaxWeight = 150 });

            // Should not crash with null zone
            Assert.DoesNotThrow(() => CombatSystem.HandleDeath(creature, null, null));
        }

        [Test]
        public void HandleDeath_EmptyInventory_NoCrash()
        {
            var zone = new Zone();
            var creature = CreateCreature(16, 16, 30);
            creature.AddPart(new InventoryPart { MaxWeight = 150 });
            zone.AddEntity(creature, 5, 5);

            Assert.DoesNotThrow(() => CombatSystem.HandleDeath(creature, null, zone));
            Assert.IsNull(zone.GetEntityCell(creature));
        }

        // ========================
        // Helpers
        // ========================

        private Entity CreateCreature(int strength, int agility, int hp = 30)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestCreature";
            entity.Tags["Creature"] = "";

            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = agility, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };

            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart());
            entity.AddPart(new ArmorPart());

            return entity;
        }

        private string GetTestJson()
        {
            return @"{
                ""Objects"": [
                    {
                        ""Name"": ""PhysicalObject"",
                        ""Parts"": [
                            { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""?"" }] },
                            { ""Name"": ""Physics"", ""Params"": [] }
                        ],
                        ""Stats"": [],
                        ""Tags"": []
                    },
                    {
                        ""Name"": ""Creature"",
                        ""Inherits"": ""PhysicalObject"",
                        ""Parts"": [
                            { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] },
                            { ""Name"": ""MeleeWeapon"", ""Params"": [{ ""Key"": ""BaseDamage"", ""Value"": ""1d2"" }] },
                            { ""Name"": ""Armor"", ""Params"": [] }
                        ],
                        ""Stats"": [
                            { ""Name"": ""Hitpoints"", ""Value"": 15, ""Min"": 0, ""Max"": 15 },
                            { ""Name"": ""Strength"", ""Value"": 16, ""Min"": 1, ""Max"": 50 },
                            { ""Name"": ""Agility"", ""Value"": 16, ""Min"": 1, ""Max"": 50 },
                            { ""Name"": ""Speed"", ""Value"": 100, ""Min"": 25, ""Max"": 200 }
                        ],
                        ""Tags"": [
                            { ""Key"": ""Creature"", ""Value"": """" }
                        ]
                    },
                    {
                        ""Name"": ""Snapjaw"",
                        ""Inherits"": ""Creature"",
                        ""Parts"": [
                            { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""DisplayName"", ""Value"": ""snapjaw"" }] },
                            { ""Name"": ""MeleeWeapon"", ""Params"": [{ ""Key"": ""BaseDamage"", ""Value"": ""1d4"" }, { ""Key"": ""PenBonus"", ""Value"": ""1"" }] },
                            { ""Name"": ""Armor"", ""Params"": [{ ""Key"": ""AV"", ""Value"": ""2"" }, { ""Key"": ""DV"", ""Value"": ""1"" }] }
                        ],
                        ""Stats"": [
                            { ""Name"": ""Hitpoints"", ""Value"": 15, ""Min"": 0, ""Max"": 15 },
                            { ""Name"": ""Strength"", ""Value"": 16 }
                        ],
                        ""Tags"": [
                            { ""Key"": ""Faction"", ""Value"": ""Snapjaws"" }
                        ]
                    }
                ]
            }";
        }
    }

    /// <summary>
    /// Test part that cancels BeforeMeleeAttack events.
    /// </summary>
    public class CancelAttackPart : Part
    {
        public override string Name => "CancelAttack";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "BeforeMeleeAttack")
                return false; // Cancel attack
            return true;
        }
    }
}