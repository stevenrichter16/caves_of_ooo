using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W3 re-review — the dead attacker who kept fighting. TakeDamage
    /// fires BEFORE the HP decrement, so retaliation (CausticSkinPart,
    /// ScaldingVeil) can kill the attacker MID-SWING via a nested
    /// ApplyDamage that runs full HandleDeath (corpse, equipment drop,
    /// zone removal). Pre-fix, control then returned to the outer attack
    /// flow: the hit line printed after "you die", on-hit dispatchers
    /// fired from the corpse, and the dual-wield swing loop — which only
    /// re-checked the DEFENDER's HP — let the zone-removed corpse swing
    /// again with its off-hand. Two guards now stop the sequence the
    /// moment the attacker stops being alive.
    ///
    /// <para>The exposure predates W3 (ScaldingVeil), but W3.4 made
    /// attacker-killing retaliation a routine wilderness hazard: the
    /// Bandfrog sits in every Sodden tier and the lair guards.</para>
    /// </summary>
    public class DeadAttackerGuardTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        [TearDown]
        public void TearDown() => SettlementRuntime.ActiveZone = null;

        private static Entity CreatureWithBody(string id, int hp)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat { Owner = e, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat { Owner = e, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new ArmorPart());
            e.AddPart(new InventoryPart { MaxWeight = 150 });
            var body = new Body();
            e.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return e;
        }

        private static Entity Weapon(string name)
        {
            var w = new Entity { BlueprintName = name };
            w.Tags["Item"] = "";
            w.AddPart(new RenderPart { DisplayName = name });
            w.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            w.AddPart(new MeleeWeaponPart { BaseDamage = "1d2", HitBonus = 100, PenBonus = 20 });
            w.AddPart(new EquippablePart { Slot = "Hand" });
            return w;
        }

        private static List<BodyPart> Hands(Entity e)
        {
            var hands = new List<BodyPart>();
            foreach (var p in e.GetPart<Body>().GetParts())
                if (p.Type == "Hand") hands.Add(p);
            return hands;
        }

        private static int HitLines(string attackerName)
        {
            int n = 0;
            foreach (var line in MessageLog.GetRecent(60))
                if (line.Contains(attackerName) && line.Contains("hits")) n++;
            return n;
        }

        [Test]
        public void AReflectKilledAttacker_DoesNotNarrateTheSwing()
        {
            // RED pre-fix: the reflect kills the attacker during the
            // frog's TakeDamage dispatch; the outer flow then printed
            // "attacker hits bandfrog for N!" AFTER the death line and
            // fired the on-hit dispatchers from the corpse.
            var zone = new Zone("Z");
            SettlementRuntime.ActiveZone = zone;
            var attacker = CreatureWithBody("doomed", hp: 1);
            var knife = Weapon("only-knife");   // HitBonus 100: the swing must land
            attacker.GetPart<InventoryPart>().AddObject(knife);
            attacker.GetPart<InventoryPart>().EquipToBodyPart(knife, Hands(attacker)[0]);
            zone.AddEntity(attacker, 10, 10);
            var frog = _factory.CreateEntity("Bandfrog");
            zone.AddEntity(frog, 11, 10);
            int frogHp = frog.GetStatValue("Hitpoints", 0);

            CombatSystem.PerformMeleeAttack(attacker, frog, zone, new Random(3));

            Assert.LessOrEqual(attacker.GetStatValue("Hitpoints", 0), 0, "the skin finished it");
            Assert.Less(frog.GetStatValue("Hitpoints", 0), frogHp,
                "the landed blow still lands — announcement precedes decrement by contract");
            Assert.AreEqual(0, HitLines("doomed"),
                "the dead do not narrate their swings — no hit line after the death line");
        }

        [Test]
        public void AReflectKilledAttacker_DoesNotSwingTheOffHand()
        {
            // RED pre-fix: the swing loop re-checked only the DEFENDER's
            // HP between swings, so the corpse rolled its off-hand and
            // attacked again from outside the zone.
            var zone = new Zone("Z");
            SettlementRuntime.ActiveZone = zone;
            var attacker = CreatureWithBody("doomed", hp: 2);
            // Guarantee the off-hand always swings (pre-fix).
            attacker.Statistics["MultiWeaponSkillBonus"] = new Stat
            {
                Owner = attacker, Name = "MultiWeaponSkillBonus",
                BaseValue = 100 - CombatSystem.BASE_SECONDARY_ATTACK_CHANCE_PERCENT,
                Min = -1000, Max = 1000,
            };
            var hands = Hands(attacker);
            var inv = attacker.GetPart<InventoryPart>();
            var w1 = Weapon("first-knife");
            var w2 = Weapon("second-knife");
            inv.AddObject(w1); inv.AddObject(w2);
            inv.EquipToBodyPart(w1, hands[0]);
            inv.EquipToBodyPart(w2, hands[1]);
            zone.AddEntity(attacker, 10, 10);
            var frog = _factory.CreateEntity("Bandfrog");
            zone.AddEntity(frog, 11, 10);

            CombatSystem.PerformMeleeAttack(attacker, frog, zone, new Random(3));

            Assert.LessOrEqual(attacker.GetStatValue("Hitpoints", 0), 0,
                "the first contact killed the attacker (reflect 2 vs 2 HP)");
            Assert.AreEqual(0, HitLines("doomed"),
                "no swing is narrated post-death, and the off-hand never swings at all");
        }
    }
}
