using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public abstract class EquipmentDiscoveryForgeFixture
    {
        protected EntityFactory Factory;
        protected static readonly string[] Heads = { "SteelBladeComponent", "IronSpikeComponent",
            "PeatMalletHeadComponent", "CinderhookAxeHeadComponent", "CounterweightLongBladeComponent" };

        [SetUp] public void Setup()
        {
            Factory = new EntityFactory();
            Factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            MessageLog.Clear(); Diag.ResetAll(); SkillRegistry.ResetForTests();
        }
        [TearDown] public void Cleanup() { MessageLog.Clear(); Diag.ResetAll(); SkillRegistry.ResetForTests(); }
        protected Entity Item(string blueprint)
        { var item = Factory.CreateEntity(blueprint); Assert.NotNull(item, blueprint); return item; }
        protected Entity Actor()
        {
            var actor = new Entity { BlueprintName = "EquipmentDiscoveryActor", ID = Guid.NewGuid().ToString("N") };
            actor.Tags["Creature"] = "";
            actor.AddPart(new RenderPart { DisplayName = "smith" });
            actor.AddPart(new PhysicsPart { Solid = true });
            actor.AddPart(new InventoryPart { MaxWeight = -1 }); actor.AddPart(new ArmorPart());
            actor.AddPart(new StatusEffectsPart()); actor.AddPart(new ActivatedAbilitiesPart());
            actor.AddPart(new SkillsPart()); var body = new Body(); actor.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            foreach (var stat in new[] { "Strength", "Agility", "Toughness", "Speed", "DV", "Hitpoints" })
                actor.Statistics[stat] = new Stat { Owner = actor, Name = stat,
                    BaseValue = stat == "DV" ? 0 : stat == "Speed" ? 100 : stat == "Hitpoints" ? 1000 : 18,
                    Min = -100, Max = 1000 };
            return actor;
        }
        protected Entity Give(Entity actor, string blueprint)
        {
            var made = Item(blueprint); Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(made));
            return actor.GetPart<InventoryPart>().Objects.Contains(made) ? made
                : actor.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == blueprint);
        }
        protected Entity Forge(Entity actor, string head, string binding = "LeatherBindingComponent")
        {
            var blade = Give(actor, head); var haft = Give(actor, "OakHaftComponent"); var bind = Give(actor, binding);
            Assert.IsTrue(WeaponForgingService.TryForge(actor, Factory, blade, haft, bind, out var weapon, out var why), why);
            return weapon;
        }
        protected Entity RoundTrip(Entity entity)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(entity); writer.WriteQueuedEntityBodies();
                stream.Position = 0; var reader = new SaveReader(stream, Factory);
                var loaded = reader.ReadEntityReference(); reader.ReadEntityBodies(); return loaded;
            }
        }
        protected static string[] Tokens(string attributes) => (attributes ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        protected sealed class HitRandom : Random
        {
            // A normal d20 hit and a nonexploding d10 penetration roll.
            // Returning the minimum d10 gives 1 - 2 + StrengthMod <= AV,
            // a valid hit that cannot deal damage even against bare skin.
            public override int Next(int min, int max) => max == 21 ? 15 : max == 11 ? 6 : min;
            public override int Next(int max) => max - 1;
        }
        protected bool Skill(Entity actor, Entity target, string command, int range = 1)
        {
            var zone = new Zone("equipment-discovery-combat");
            Assert.IsTrue(zone.AddEntity(actor, 10, 10)); Assert.IsTrue(zone.AddEntity(target, 10 + range, 10));
            return actor.GetPart<SkillsPart>().TryRouteSkillCommand(command, zone, new HitRandom(), 1, 0);
        }
    }

    public class EquipmentDiscoveryForgingTests : EquipmentDiscoveryForgeFixture
    {
        [TestCase("SteelBladeComponent", "1d6", 0, 1, "Cutting LongBlades")]
        [TestCase("IronSpikeComponent", "1d4", 1, 1, "Piercing")]
        [TestCase("PeatMalletHeadComponent", "1d4", -1, 1, "Bludgeoning Cudgel")]
        [TestCase("CinderhookAxeHeadComponent", "1d8", 0, -1, "Cutting Axe")]
        [TestCase("CounterweightLongBladeComponent", "1d6", -1, 2, "Cutting LongBlades")]
        public void AuthoredHeadPreviewAndActualPaidWeaponHaveOneRoleAndRealDrawback(string head, string dice, int pen, int hit, string attributes)
        {
            var preview = WeaponForgingService.PreviewForge(Item(head).GetPart<WeaponComponentPart>(),
                Item("OakHaftComponent").GetPart<WeaponComponentPart>(), Item("LeatherBindingComponent").GetPart<WeaponComponentPart>());
            Assert.IsTrue(preview.IsComplete); Assert.AreEqual(dice, preview.BaseDamage);
            Assert.AreEqual(pen, preview.PenBonus); Assert.AreEqual(hit, preview.HitBonus); Assert.AreEqual(attributes, preview.Attributes);
            var actor = Actor(); var weapon = Forge(actor, head); var melee = weapon.GetPart<MeleeWeaponPart>();
            Assert.AreEqual(preview.BaseDamage, melee.BaseDamage); Assert.AreEqual(preview.PenBonus, melee.PenBonus);
            Assert.AreEqual(preview.HitBonus, melee.HitBonus); Assert.AreEqual(preview.Attributes, melee.Attributes);
            Assert.IsEmpty(melee.OnHitEffectsRaw, "The mallet uses native Bludgeoning control, with no second stacking stun proc.");
            Assert.AreEqual(1, actor.GetPart<InventoryPart>().Objects.Count); Assert.AreSame(weapon, actor.GetPart<InventoryPart>().Objects[0]);
        }

        [TestCase("SteelBladeComponent", true)] [TestCase("CounterweightLongBladeComponent", true)]
        [TestCase("IronSpikeComponent", false)] [TestCase("PeatMalletHeadComponent", false)]
        [TestCase("CinderhookAxeHeadComponent", false)]
        public void LearnedLungeUsesActualForgedLongBladeAtRangeTwoAndRejectsOtherFamilies(string head, bool expected)
        {
            var actor = Actor(); var weapon = Forge(actor, head); Assert.IsTrue(InventorySystem.Equip(actor, weapon));
            Assert.IsTrue(actor.GetPart<SkillsPart>().AddSkill(new LongBlades_Lunge()));
            var target = Actor(); int hp = target.GetStatValue("Hitpoints");
            Diag.SetChannel("damage", true);
            Assert.AreEqual(expected, Skill(actor, target, "CommandLunge", 2));
            Assert.AreEqual(expected, target.GetStatValue("Hitpoints") < hp, "The actual skill attack must land, not merely pass a family helper.");
            var hits = DiagQuery.Apply(new DiagQuery.Filter { Kind = "HitRoll", Actor = actor.ID, Target = target.ID }).Records;
            Assert.AreEqual(expected ? 1 : 0, hits.Count, "Wrong-family refusal must never enter the attack pipeline.");
            if (expected) StringAssert.Contains("\"landed\":true", hits[0].PayloadJson);
        }

        [TestCase("PeatMalletHeadComponent", "CommandConk", true)]
        [TestCase("SteelBladeComponent", "CommandConk", false)]
        [TestCase("CinderhookAxeHeadComponent", "CommandRendArmor", true)]
        [TestCase("SteelBladeComponent", "CommandRendArmor", false)]
        public void NewHeadsEnableTheirLearnedControlOrArmorSkillOnly(string head, string command, bool expected)
        {
            var actor = Actor(); Assert.IsTrue(InventorySystem.Equip(actor, Forge(actor, head)));
            actor.GetPart<SkillsPart>().AddSkill(command == "CommandConk" ? (BaseSkillPart)new Cudgel_Conk() : new Axe_RendArmor());
            var target = Actor(); Assert.AreEqual(expected, Skill(actor, target, command));
            Assert.AreEqual(expected, command == "CommandConk" ? target.HasEffect<StunnedEffect>() : target.HasEffect<ShatterArmorEffect>());
        }

        [TestCase("SteelBladeComponent", "CommandLunge")] [TestCase("PeatMalletHeadComponent", "CommandConk")]
        [TestCase("CinderhookAxeHeadComponent", "CommandRendArmor")]
        public void FindingAndEquippingHeadDoesNotTeachSkill(string head, string command)
        {
            var actor = Actor(); Assert.IsTrue(InventorySystem.Equip(actor, Forge(actor, head)));
            Assert.IsEmpty(actor.GetPart<SkillsPart>().SkillList); Assert.IsFalse(Skill(actor, Actor(), command));
        }

        [TestCase("PeatMalletHeadComponent", "Cudgel")] [TestCase("CinderhookAxeHeadComponent", "Axe")]
        [TestCase("CounterweightLongBladeComponent", "LongBlades")]
        public void ReforgeEquippedSteelChangesFamilyImmediatelyAndReturnsPaidHead(string replacement, string family)
        {
            var actor = Actor(); var weapon = Forge(actor, "SteelBladeComponent"); Assert.IsTrue(InventorySystem.Equip(actor, weapon));
            var part = Give(actor, replacement);
            Assert.IsTrue(WeaponForgingService.TryReforge(actor, Factory, weapon, part, out var affected, out var returned, out var why), why);
            Assert.AreSame(weapon, affected); Assert.AreEqual("SteelBladeComponent", returned.BlueprintName);
            Assert.AreEqual(replacement, weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint);
            Assert.AreSame(weapon.GetPart<MeleeWeaponPart>(), SkillCombatHelpers.FindEquippedWeaponOfClass(actor, family));
            foreach (var other in new[] { "Cudgel", "Axe", "LongBlades" }.Where(f => f != family))
                Assert.IsNull(SkillCombatHelpers.FindEquippedWeaponOfClass(actor, other));
            Assert.IsFalse(actor.GetPart<InventoryPart>().Contains(part));
            Assert.IsTrue(actor.GetPart<InventoryPart>().Contains(returned));
        }

        [TestCase("LeatherBindingComponent")] [TestCase("SerratedEdgeComponent")]
        public void RepeatedHeadSwapsRecomputeRatherThanAccumulateFamiliesAndRetainBinding(string binding)
        {
            var actor = Actor(); var weapon = Forge(actor, "SteelBladeComponent", binding);
            foreach (var head in Heads.Concat(new[] { "SteelBladeComponent" }))
            {
                Assert.IsTrue(WeaponForgingService.TryReforge(actor, Factory, weapon, Give(actor, head), out _, out var why), why);
                var expected = WeaponForgingService.PreviewForge(Item(head).GetPart<WeaponComponentPart>(),
                    Item("OakHaftComponent").GetPart<WeaponComponentPart>(), Item(binding).GetPart<WeaponComponentPart>());
                var actual = weapon.GetPart<MeleeWeaponPart>(); Assert.AreEqual(expected.Attributes, actual.Attributes);
                Assert.AreEqual(expected.OnHitEffectsRaw, actual.OnHitEffectsRaw); Assert.AreEqual(expected.PenBonus, actual.PenBonus);
                Assert.AreEqual(expected.HitBonus, actual.HitBonus); Assert.AreEqual(expected.BaseDamage, actual.BaseDamage);
                Assert.AreEqual(binding, weapon.GetPart<WeaponAssemblyPart>().BindingBlueprint);
            }
        }
    }
}
