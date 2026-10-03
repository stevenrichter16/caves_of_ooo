using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class EquipmentDiscoveryForgingAdversarialTests : EquipmentDiscoveryForgeFixture
    {
        [TestCase(false)] [TestCase(true)]
        public void OldAuthoredSteelComponentGainsOnlyMissingFamilyAndLoadIsIdempotent(bool carried)
        {
            var item = Item("SteelBladeComponent"); var part = item.GetPart<WeaponComponentPart>();
            part.Attributes = "Cutting"; part.BaseDamage = "2d3+7"; part.PenBonus = 9; part.HitBonus = -4;
            part.OnHitEffectSpec = "Burning,37,,6,2"; item.GetPart<RenderPart>().DisplayName = "my old steel";
            var actor = Actor(); if (carried) Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(item));
            var loaded = RoundTrip(carried ? actor : item);
            for (int pass = 0; pass < 2; pass++)
            {
                item = carried ? loaded.GetPart<InventoryPart>().Objects.Single() : loaded;
                part = item.GetPart<WeaponComponentPart>(); Assert.AreEqual("Cutting LongBlades", part.Attributes);
                Assert.AreEqual("2d3+7", part.BaseDamage); Assert.AreEqual(9, part.PenBonus); Assert.AreEqual(-4, part.HitBonus);
                Assert.AreEqual("Burning,37,,6,2", part.OnHitEffectSpec); Assert.AreEqual("my old steel", item.GetPart<RenderPart>().DisplayName);
                loaded = RoundTrip(loaded);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void OldSteelAssemblyGainsFamilyWithoutRebuildingPaidTemperOrEquipment(bool equipped)
        {
            var actor = Actor(); var weapon = Forge(actor, "SteelBladeComponent");
            if (equipped) Assert.IsTrue(InventorySystem.Equip(actor, weapon));
            var melee = weapon.GetPart<MeleeWeaponPart>(); melee.Attributes = "Cutting Heat";
            melee.BaseDamage = "2d6+3"; melee.PenBonus = 11; melee.HitBonus = -3; melee.MaxStrengthBonus = 8;
            melee.OnHitEffectsRaw = "Burning,60,,5,2";
            weapon.AddPart(new WeaponTemperPart { TemperCount = 2, HpPenaltyTotal = 4, AppliedSpecsRaw = melee.OnHitEffectsRaw });
            weapon.SetIntProperty("ModificationCount", 1); weapon.Tags["Sharp"] = "custom-marker";
            weapon.GetPart<RenderPart>().DisplayName = "my quenched heirloom";
            string identity = weapon.ID;
            actor = RoundTrip(actor);
            weapon = equipped ? actor.GetPart<InventoryPart>().GetAllEquipped().Single() : actor.GetPart<InventoryPart>().Objects.Single();
            melee = weapon.GetPart<MeleeWeaponPart>();
            Assert.AreEqual(identity, weapon.ID); Assert.AreEqual("Cutting Heat LongBlades", melee.Attributes);
            Assert.AreEqual("2d6+3", melee.BaseDamage); Assert.AreEqual(11, melee.PenBonus); Assert.AreEqual(-3, melee.HitBonus);
            Assert.AreEqual(8, melee.MaxStrengthBonus); Assert.AreEqual("Burning,60,,5,2", melee.OnHitEffectsRaw);
            Assert.AreEqual(2, weapon.GetPart<WeaponTemperPart>().TemperCount); Assert.AreEqual(4, weapon.GetPart<WeaponTemperPart>().HpPenaltyTotal);
            Assert.AreEqual(melee.OnHitEffectsRaw, weapon.GetPart<WeaponTemperPart>().AppliedSpecsRaw);
            Assert.AreEqual("my quenched heirloom", weapon.GetPart<RenderPart>().DisplayName);
            Assert.AreEqual(1, weapon.GetIntProperty("ModificationCount")); Assert.AreEqual("custom-marker", weapon.Tags["Sharp"]);
            Assert.AreEqual(equipped, SkillCombatHelpers.FindEquippedWeaponOfClass(actor, "LongBlades") == melee);
        }

        [TestCase("foreign-component")] [TestCase("foreign-weapon")] [TestCase("no-assembly")]
        [TestCase("other-head")] [TestCase("wrong-slot")] [TestCase("no-cutting")]
        [TestCase("conflicting-family")] [TestCase("lookalike-token")] [TestCase("already-classified")]
        public void SaveRepairLeavesAmbiguousOrAlreadyClassifiedContentLiteral(string condition)
        {
            bool component = condition == "foreign-component" || condition == "wrong-slot";
            var item = component ? Item("SteelBladeComponent") : Forge(Actor(), "SteelBladeComponent");
            string attributes = condition == "no-cutting" ? "Piercing" : condition == "conflicting-family" ? "Cutting Axe"
                : condition == "lookalike-token" ? "NotCutting" : condition == "already-classified" ? "Cutting longblades" : "Cutting";
            if (component) item.GetPart<WeaponComponentPart>().Attributes = attributes;
            else item.GetPart<MeleeWeaponPart>().Attributes = attributes;
            if (condition == "foreign-component" || condition == "foreign-weapon") item.BlueprintName = "CustomSteel";
            if (condition == "no-assembly") item.RemovePart(item.GetPart<WeaponAssemblyPart>());
            if (condition == "other-head") item.GetPart<WeaponAssemblyPart>().BladeBlueprint = "IronSpikeComponent";
            if (condition == "wrong-slot") item.GetPart<WeaponComponentPart>().Slot = "Binding";
            item = RoundTrip(item);
            Assert.AreEqual(attributes, component ? item.GetPart<WeaponComponentPart>().Attributes : item.GetPart<MeleeWeaponPart>().Attributes);
        }

        [TestCase(false)] [TestCase(true)]
        public void ReforgeOuterRollbackRestoresFamilyAssemblyPaymentAndEquippedIdentity(bool rollback)
        {
            var actor = Actor(); var weapon = Forge(actor, "SteelBladeComponent"); Assert.IsTrue(InventorySystem.Equip(actor, weapon));
            var replacement = Give(actor, "PeatMalletHeadComponent"); var melee = weapon.GetPart<MeleeWeaponPart>();
            string original = melee.Attributes; var inventory = actor.GetPart<InventoryPart>(); var contents = inventory.Objects.ToArray();
            var tx = new InventoryTransaction();
            var command = new ReforgeWeaponCommand(weapon, replacement, Factory);
            Assert.IsTrue(command.Execute(new InventoryContext(actor), tx).Success);
            Assert.AreEqual("Bludgeoning Cudgel", melee.Attributes);
            if (rollback) tx.Rollback(); else tx.Commit();
            Assert.AreSame(melee, weapon.GetPart<MeleeWeaponPart>());
            Assert.AreSame(weapon, inventory.GetAllEquipped().Single());
            Assert.AreEqual(rollback ? original : "Bludgeoning Cudgel", melee.Attributes);
            Assert.AreEqual(rollback ? "SteelBladeComponent" : "PeatMalletHeadComponent", weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint);
            Assert.AreEqual(rollback, inventory.Contains(replacement));
            if (rollback) CollectionAssert.AreEqual(contents, inventory.Objects);
        }

        [TestCase(false)] [TestCase(true)]
        public void CounterweightTradeoffChangesNativeHitAndPenetrationNumbers(bool counterweight)
        {
            var actor = Actor(); var weapon = Forge(actor, counterweight ? "CounterweightLongBladeComponent" : "SteelBladeComponent");
            var target = Actor(); var zone = new Zone("equipment-hit"); zone.AddEntity(actor, 10, 10); zone.AddEntity(target, 11, 10);
            CavesOfOoo.Diagnostics.Diag.SetChannel("damage", true);
            CombatSystem.PerformSingleAttack(actor, target, weapon.GetPart<MeleeWeaponPart>(), true, zone, new HitRandom());
            var result = CavesOfOoo.Diagnostics.DiagQuery.Apply(new CavesOfOoo.Diagnostics.DiagQuery.Filter
                { Kind = "Penetration", Actor = actor.ID, Target = target.ID });
            Assert.AreEqual(1, result.Records.Count, "A real landed hit must consume the authored penetration penalty.");
            StringAssert.Contains("\"weaponPenBonus\":" + (counterweight ? -1 : 0), result.Records[0].PayloadJson);
            var hit = CavesOfOoo.Diagnostics.DiagQuery.Apply(new CavesOfOoo.Diagnostics.DiagQuery.Filter
                { Kind = "HitRoll", Actor = actor.ID, Target = target.ID }).Records.Single();
            StringAssert.Contains("\"weaponHitBonus\":" + (counterweight ? 2 : 1), hit.PayloadJson);
        }

        [TestCase("Cutting", "Unclassified")]
        [TestCase("NotLongBlades", "Unclassified")]
        [TestCase("Piercing", "Short blades")]
        [TestCase("Cutting LongBlades Axe", "Mixed")]
        [TestCase("Bludgeoning Cudgel Cudgel", "Cudgel")]
        public void PreviewFamilyLabelUsesActualAttributeTokensWithoutInferringFromDamageOrNames(string attributes, string family)
        {
            var head = new WeaponComponentPart { Slot = "Blade", BaseDamage = "1d6", Attributes = attributes, NameFragment = "long blade" };
            var preview = WeaponForgingService.PreviewForge(head, new WeaponComponentPart(), new WeaponComponentPart());
            Assert.AreEqual(family, preview.FamilyDisplayName);
            Assert.AreEqual("long blade", preview.DisplayName, "Name is flavor, never a skill-family source.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void IncompleteSelectionCannotAdvertiseAUsableFamily(int missing)
        {
            var parts = new[] { new WeaponComponentPart { Attributes = "Cutting LongBlades" },
                new WeaponComponentPart(), new WeaponComponentPart() };
            parts[missing] = null;
            var preview = WeaponForgingService.PreviewForge(parts[0], parts[1], parts[2]);
            Assert.IsFalse(preview.IsComplete); Assert.IsEmpty(preview.FamilyDisplayName);
        }
    }
}
